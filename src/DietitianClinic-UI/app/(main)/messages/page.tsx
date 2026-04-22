'use client';

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { Button }      from 'primereact/button';
import { InputText }   from 'primereact/inputtext';
import { ProgressSpinner } from 'primereact/progressspinner';
import { classNames }  from 'primereact/utils';
import { AuthService } from '../../../service/AuthService';
import { PatientService } from '../../../service/PatientService';
import { MeasurementService } from '../../../service/MeasurementService';
import { MessageService } from '../../../service/MessageService';
import { useSignalR, ChatMessage } from '../../../layout/context/signalrcontext';

// ─── Tipler ──────────────────────────────────────────────────────────────────

interface Contact {
    userId:   number;
    name:     string;
    subtitle: string;
}

// ─── Yardımcı ────────────────────────────────────────────────────────────────

const formatTime = (iso: string) => {
    const d = new Date(iso);
    const now = new Date();
    const diffDays = Math.floor((now.getTime() - d.getTime()) / 86_400_000);
    if (diffDays === 0) return d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    if (diffDays === 1) return 'Dün';
    if (diffDays < 7)  return d.toLocaleDateString('tr-TR', { weekday: 'long' });
    return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit' });
};

const initials = (name: string) =>
    name.split(' ').map(w => w[0]?.toUpperCase() ?? '').slice(0, 2).join('');

// ─── Bileşen ─────────────────────────────────────────────────────────────────

const MessagesPage = () => {
    const signalr = useSignalR();

    // Auth
    const currentUser   = AuthService.getCurrentUser();
    const currentUserId: number = currentUser?.userId ?? currentUser?.UserId ?? 0;
    const role          = AuthService.getRole();
    const isDietitian   = role === 'Dietitian' || role === '1';

    // Kontaklar & seçili kontakt
    const [contacts,         setContacts]         = useState<Contact[]>([]);
    const [selectedContact,  setSelectedContact]  = useState<Contact | null>(null);
    const [contactsLoading,  setContactsLoading]  = useState(true);

    // Mesajlar
    const [messages,     setMessages]     = useState<ChatMessage[]>([]);
    const [msgLoading,   setMsgLoading]   = useState(false);

    // Giriş & gönderme
    const [inputText,    setInputText]    = useState('');
    const [sending,      setSending]      = useState(false);

    // Yazıyor göstergesi
    const [otherTyping,  setOtherTyping]  = useState(false);
    const typingTimer                     = useRef<ReturnType<typeof setTimeout> | null>(null);

    // Okunmamış sayıları
    const [unreadMap,    setUnreadMap]    = useState<Record<number, number>>({});

    // Scroll
    const bottomRef = useRef<HTMLDivElement>(null);

    // Typing debounce
    const typingDebounce = useRef<ReturnType<typeof setTimeout> | null>(null);

    // ── Kontakları yükle ─────────────────────────────────────────────────────

    useEffect(() => {
        (async () => {
            try {
                if (isDietitian) {
                    const patients = await PatientService.getAllPatients();
                    const list: Contact[] = patients
                        .filter((p: any) => p.patientUserId)
                        .map((p: any) => ({
                            userId:   p.patientUserId as number,
                            name:     p.fullName,
                            subtitle: p.email || '',
                        }));
                    setContacts(list);
                } else {
                    // Hasta: profilden diyetisyen bilgisi al
                    const profile = await MeasurementService.getMyProfile();
                    const dietUserId = profile?.userId ?? profile?.UserId ?? null;
                    const dietName   = profile?.dietitianName ?? profile?.DietitianName ?? 'Diyetisyenim';
                    const dietEmail  = profile?.dietitianEmail ?? profile?.DietitianEmail ?? '';
                    if (dietUserId) {
                        setContacts([{ userId: dietUserId, name: dietName, subtitle: dietEmail }]);
                    }
                }

                // Okunmamış sayıları yükle
                const unread = await MessageService.getUnreadBySender();
                const map: Record<number, number> = {};
                (unread as { senderId: number; count: number }[]).forEach(u => { map[u.senderId] = u.count; });
                setUnreadMap(map);
            } catch (e) {
                console.error('[Messages] Kontaklar yüklenemedi:', e);
            } finally {
                setContactsLoading(false);
            }
        })();
    }, [isDietitian]);

    // ── Konuşmayı yükle ──────────────────────────────────────────────────────

    useEffect(() => {
        if (!selectedContact) { setMessages([]); return; }
        setMsgLoading(true);
        (async () => {
            try {
                const data = await MessageService.getConversation(selectedContact.userId);
                setMessages(data ?? []);
                // Okunmamış sayısını sıfırla
                setUnreadMap(prev => ({ ...prev, [selectedContact.userId]: 0 }));
            } catch (e) {
                console.error('[Messages] Konuşma yüklenemedi:', e);
            } finally {
                setMsgLoading(false);
            }
        })();
    }, [selectedContact]);

    // ── Otomatik scroll ───────────────────────────────────────────────────────

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, otherTyping]);

    // ── SignalR: yeni mesaj ───────────────────────────────────────────────────

    const handleIncoming = useCallback((msg: ChatMessage) => {
        const otherId = selectedContact?.userId;
        const relevant =
            (msg.senderId === currentUserId && msg.receiverId === otherId) ||
            (msg.receiverId === currentUserId && msg.senderId === otherId);

        if (relevant) {
            setMessages(prev => {
                // Mükerrer önle
                if (prev.some(m => m.id === msg.id)) return prev;
                return [...prev, msg];
            });
            // Okunmamış sayısını sıfırla (konuşma açık)
            setUnreadMap(prev => ({ ...prev, [msg.senderId]: 0 }));
        } else if (msg.receiverId === currentUserId) {
            // Başka bir konuşma – badge artır
            setUnreadMap(prev => ({ ...prev, [msg.senderId]: (prev[msg.senderId] ?? 0) + 1 }));
        }
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentUserId, selectedContact]);

    useEffect(() => {
        signalr.onMessage(handleIncoming);
        return () => signalr.offMessage(handleIncoming);
    }, [signalr, handleIncoming]);

    // ── SignalR: yazıyor ───────────────────────────────────────────────────────

    const handleTyping = useCallback((senderId: string) => {
        if (Number(senderId) !== selectedContact?.userId) return;
        setOtherTyping(true);
        if (typingTimer.current) clearTimeout(typingTimer.current);
        typingTimer.current = setTimeout(() => setOtherTyping(false), 3000);
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedContact]);

    useEffect(() => {
        signalr.onTyping(handleTyping);
        return () => signalr.offTyping(handleTyping);
    }, [signalr, handleTyping]);

    // ── Gönder ───────────────────────────────────────────────────────────────

    const handleSend = async () => {
        const text = inputText.trim();
        if (!text || !selectedContact || sending) return;
        setSending(true);
        setInputText('');
        try {
            await MessageService.sendMessage({ receiverId: selectedContact.userId, content: text });
            // Backend SignalR'dan echo gelecek, optimistic ekleme gerekmez
        } catch (e) {
            console.error('[Messages] Gönderilemedi:', e);
            setInputText(text); // geri koy
        } finally {
            setSending(false);
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend(); }
    };

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setInputText(e.target.value);
        if (!selectedContact) return;
        if (typingDebounce.current) clearTimeout(typingDebounce.current);
        typingDebounce.current = setTimeout(() => signalr.sendTyping(selectedContact.userId), 400);
    };

    // ─────────────────────────────────────────────────────────────────────────

    return (
        <div className="grid" style={{ height: 'calc(100vh - 9rem)' }}>
            <div className="col-12">
                <div className="card p-0 overflow-hidden" style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                    <div className="flex" style={{ flex: 1, minHeight: 0 }}>

                        {/* ── Sol: Kontaklar ── */}
                        <div
                            className="border-right-1 surface-border flex flex-column"
                            style={{ width: '300px', minWidth: '240px', flexShrink: 0 }}
                        >
                            {/* Başlık */}
                            <div className="p-3 border-bottom-1 surface-border">
                                <span className="font-semibold text-lg text-900">Mesajlar</span>
                                {!signalr.connected && (
                                    <span className="ml-2 text-xs text-orange-500">
                                        <i className="pi pi-circle-fill mr-1" style={{ fontSize: '0.5rem' }} />
                                        Bağlanıyor…
                                    </span>
                                )}
                                {signalr.connected && (
                                    <span className="ml-2 text-xs text-green-500">
                                        <i className="pi pi-circle-fill mr-1" style={{ fontSize: '0.5rem' }} />
                                        Canlı
                                    </span>
                                )}
                            </div>

                            {/* Liste */}
                            <div className="overflow-y-auto" style={{ flex: 1 }}>
                                {contactsLoading ? (
                                    <div className="flex justify-content-center align-items-center p-4">
                                        <ProgressSpinner style={{ width: '2rem', height: '2rem' }} />
                                    </div>
                                ) : contacts.length === 0 ? (
                                    <div className="p-4 text-center text-500 text-sm">
                                        {isDietitian ? 'Henüz danışanınız yok.' : 'Diyetisyen bilgisi bulunamadı.'}
                                    </div>
                                ) : contacts.map(c => {
                                    const unread = unreadMap[c.userId] ?? 0;
                                    const active = selectedContact?.userId === c.userId;
                                    return (
                                        <div
                                            key={c.userId}
                                            onClick={() => setSelectedContact(c)}
                                            className={classNames(
                                                'flex align-items-center gap-3 px-3 py-3 cursor-pointer transition-colors transition-duration-150',
                                                active
                                                    ? 'surface-100'
                                                    : 'hover:surface-50'
                                            )}
                                            style={{ borderLeft: active ? '3px solid var(--primary-color)' : '3px solid transparent' }}
                                        >
                                            {/* Avatar */}
                                            <div
                                                className="flex align-items-center justify-content-center border-circle font-bold text-white flex-shrink-0"
                                                style={{
                                                    width: '2.5rem', height: '2.5rem',
                                                    background: active ? 'var(--primary-color)' : '#6366f1',
                                                    fontSize: '0.85rem',
                                                }}
                                            >
                                                {initials(c.name)}
                                            </div>

                                            <div className="flex-1 min-w-0">
                                                <div className="flex justify-content-between align-items-center">
                                                    <span className={classNames('text-900', unread > 0 && 'font-semibold')}
                                                          style={{ fontSize: '0.9rem' }}>
                                                        {c.name}
                                                    </span>
                                                    {unread > 0 && (
                                                        <span
                                                            className="flex align-items-center justify-content-center border-circle bg-primary text-white font-bold"
                                                            style={{ width: '1.3rem', height: '1.3rem', fontSize: '0.7rem' }}
                                                        >
                                                            {unread > 9 ? '9+' : unread}
                                                        </span>
                                                    )}
                                                </div>
                                                <div className="text-500 text-xs white-space-nowrap overflow-hidden text-overflow-ellipsis">
                                                    {c.subtitle}
                                                </div>
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        </div>

                        {/* ── Sağ: Mesaj alanı ── */}
                        <div className="flex flex-column" style={{ flex: 1, minWidth: 0 }}>
                            {!selectedContact ? (
                                <div className="flex flex-column align-items-center justify-content-center h-full text-center gap-3 text-500">
                                    <i className="pi pi-comments" style={{ fontSize: '3rem', opacity: 0.3 }} />
                                    <span>Mesajlaşmaya başlamak için bir kişi seçin</span>
                                </div>
                            ) : (
                                <>
                                    {/* Başlık */}
                                    <div className="flex align-items-center gap-3 px-4 py-3 border-bottom-1 surface-border">
                                        <div
                                            className="flex align-items-center justify-content-center border-circle font-bold text-white flex-shrink-0"
                                            style={{ width: '2.5rem', height: '2.5rem', background: 'var(--primary-color)', fontSize: '0.85rem' }}
                                        >
                                            {initials(selectedContact.name)}
                                        </div>
                                        <div>
                                            <div className="font-semibold text-900">{selectedContact.name}</div>
                                            <div className="text-500 text-xs">{selectedContact.subtitle}</div>
                                        </div>
                                    </div>

                                    {/* Mesajlar */}
                                    <div className="overflow-y-auto px-4 py-3 flex flex-column gap-2" style={{ flex: 1 }}>
                                        {msgLoading ? (
                                            <div className="flex justify-content-center align-items-center h-full">
                                                <ProgressSpinner style={{ width: '2rem', height: '2rem' }} />
                                            </div>
                                        ) : messages.length === 0 ? (
                                            <div className="flex flex-column align-items-center justify-content-center h-full text-500 gap-2">
                                                <i className="pi pi-send" style={{ fontSize: '2rem', opacity: 0.3 }} />
                                                <span className="text-sm">Henüz mesaj yok. İlk mesajı siz gönderin!</span>
                                            </div>
                                        ) : (
                                            messages.map(msg => {
                                                const mine = msg.senderId === currentUserId;
                                                return (
                                                    <div
                                                        key={msg.id}
                                                        className={classNames('flex', mine ? 'justify-content-end' : 'justify-content-start')}
                                                    >
                                                        <div
                                                            className={classNames(
                                                                'px-3 py-2 border-round-xl',
                                                                mine
                                                                    ? 'bg-primary text-white'
                                                                    : 'surface-100 text-900'
                                                            )}
                                                            style={{ maxWidth: '65%', wordBreak: 'break-word' }}
                                                        >
                                                            <div style={{ fontSize: '0.9rem', lineHeight: '1.4' }}>
                                                                {msg.content}
                                                            </div>
                                                            <div
                                                                className={classNames(
                                                                    'text-right mt-1',
                                                                    mine ? 'text-primary-100' : 'text-400'
                                                                )}
                                                                style={{ fontSize: '0.7rem' }}
                                                            >
                                                                {formatTime(msg.createdDate)}
                                                                {mine && (
                                                                    <i
                                                                        className={classNames(
                                                                            'pi ml-1',
                                                                            msg.isRead ? 'pi-check-circle' : 'pi-check'
                                                                        )}
                                                                        style={{ fontSize: '0.65rem' }}
                                                                    />
                                                                )}
                                                            </div>
                                                        </div>
                                                    </div>
                                                );
                                            })
                                        )}

                                        {/* Yazıyor göstergesi */}
                                        {otherTyping && (
                                            <div className="flex justify-content-start">
                                                <div className="surface-100 px-3 py-2 border-round-xl text-500 text-sm flex align-items-center gap-2">
                                                    <span className="flex gap-1">
                                                        {[0, 1, 2].map(i => (
                                                            <span
                                                                key={i}
                                                                style={{
                                                                    display: 'inline-block',
                                                                    width: '6px', height: '6px',
                                                                    background: '#94a3b8',
                                                                    borderRadius: '50%',
                                                                    animation: `bounce 1.2s ${i * 0.2}s infinite`,
                                                                }}
                                                            />
                                                        ))}
                                                    </span>
                                                    <span>Yazıyor…</span>
                                                </div>
                                            </div>
                                        )}
                                        <div ref={bottomRef} />
                                    </div>

                                    {/* Giriş alanı */}
                                    <div className="flex align-items-center gap-2 px-4 py-3 border-top-1 surface-border">
                                        <InputText
                                            className="flex-1"
                                            placeholder="Mesajınızı yazın..."
                                            value={inputText}
                                            onChange={handleInputChange}
                                            onKeyDown={handleKeyDown}
                                            disabled={sending}
                                        />
                                        <Button
                                            icon={sending ? 'pi pi-spin pi-spinner' : 'pi pi-send'}
                                            rounded
                                            disabled={!inputText.trim() || sending}
                                            onClick={handleSend}
                                            tooltip="Gönder"
                                            tooltipOptions={{ position: 'top' }}
                                        />
                                    </div>
                                </>
                            )}
                        </div>

                    </div>
                </div>
            </div>

            {/* Yazıyor animasyonu */}
            <style>{`
                @keyframes bounce {
                    0%, 80%, 100% { transform: translateY(0); }
                    40%           { transform: translateY(-5px); }
                }
            `}</style>
        </div>
    );
};

export default MessagesPage;
