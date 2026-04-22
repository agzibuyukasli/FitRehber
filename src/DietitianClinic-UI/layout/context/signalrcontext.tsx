'use client';
import { Toast }    from 'primereact/toast';
import React, {
    createContext, useCallback, useContext,
    useEffect, useMemo, useRef, useState,
} from 'react';
import { AuthService }    from '../../service/AuthService';
import { SignalRService } from '../../service/SignalRService';

// ─── Tip ─────────────────────────────────────────────────────────────────────
interface SignalRContextValue {
    connected:  boolean;
    /** ReceiveMessage olayına dinleyici ekler */
    onMessage:  (cb: (msg: ChatMessage) => void) => void;
    /** ReceiveMessage dinleyicisini kaldırır */
    offMessage: (cb: (msg: ChatMessage) => void) => void;
    /** UserTyping olayına dinleyici ekler */
    onTyping:   (cb: (senderId: string) => void) => void;
    offTyping:  (cb: (senderId: string) => void) => void;
    /** Karşı tarafa yazıyor sinyali gönderir */
    sendTyping: (receiverId: number) => void;
}

export interface ChatMessage {
    id:             number;
    senderId:       number;
    receiverId:     number;
    content:        string;
    isRead:         boolean;
    attachmentUrl:  string | null;
    attachmentName: string | null;
    attachmentType: string | null;
    createdDate:    string;
}

const SignalRContext = createContext<SignalRContextValue>({
    connected:  false,
    onMessage:  () => {},
    offMessage: () => {},
    onTyping:   () => {},
    offTyping:  () => {},
    sendTyping: () => {},
});

export const useSignalR = () => useContext(SignalRContext);

// ─── Provider ─────────────────────────────────────────────────────────────────
export const SignalRProvider = ({ children }: { children: React.ReactNode }) => {
    const [connected, setConnected] = useState(false);
    const notifToast = useRef<Toast>(null);

    useEffect(() => {
        const user = AuthService.getCurrentUser();
        if (!user?.token) return;

        let active = true;

        (async () => {
            try {
                const conn = await SignalRService.connect();
                if (!conn || !active) return;

                setConnected(true);

                conn.onreconnected(() => { if (active) setConnected(true); });
                conn.onreconnecting(() => { if (active) setConnected(false); });
                conn.onclose(()      => { if (active) setConnected(false); });

                SignalRService.onReceiveNotification((notif: { title?: string; message?: string }) => {
                    if (!active) return;
                    notifToast.current?.show({
                        severity: 'info',
                        summary:  notif?.title   || 'Yeni Bildirim',
                        detail:   notif?.message || 'Yeni bir bildiriminiz var!',
                        life: 6000,
                    });
                });
            } catch {
                /* Bağlantı kurulamazsa sessizce geç — uygulama çalışmaya devam eder */
            }
        })();

        return () => {
            active = false;
            SignalRService.offReceiveNotification(() => {});
        };
    }, []);

    const onMessage  = useCallback((cb: (msg: ChatMessage) => void) => SignalRService.onReceiveMessage(cb),  []);
    const offMessage = useCallback((cb: (msg: ChatMessage) => void) => SignalRService.offReceiveMessage(cb), []);
    const onTyping   = useCallback((cb: (id: string) => void)       => SignalRService.onUserTyping(cb),       []);
    const offTyping  = useCallback((cb: (id: string) => void)       => SignalRService.offUserTyping(cb),      []);
    const sendTyping = useCallback((rid: number)                     => SignalRService.sendTyping(rid),        []);

    const contextValue = useMemo(
        () => ({ connected, onMessage, offMessage, onTyping, offTyping, sendTyping }),
        [connected, onMessage, offMessage, onTyping, offTyping, sendTyping]
    );

    return (
        <SignalRContext.Provider value={contextValue}>
            {/* Uygulama genelinde bildirim toast'u */}
            <Toast ref={notifToast} position="top-right" style={{ zIndex: 9999 }} />
            {children}
        </SignalRContext.Provider>
    );
};
