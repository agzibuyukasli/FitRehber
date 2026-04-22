'use client';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import { Dropdown } from 'primereact/dropdown';
import { InputText } from 'primereact/inputtext';
import { ProgressSpinner } from 'primereact/progressspinner';
import { Toast } from 'primereact/toast';
import React, { useCallback, useEffect, useRef, useState } from 'react';
import { AppointmentService } from '../../../../service/AppointmentService';
import { PatientService } from '../../../../service/PatientService';

// ─── Sabitler ─────────────────────────────────────────────────────────────────
const MONTHS = ['Ocak','Şubat','Mart','Nisan','Mayıs','Haziran','Temmuz','Ağustos','Eylül','Ekim','Kasım','Aralık'];
const DAY_NAMES = ['Pzt','Sal','Çar','Per','Cum','Cmt','Paz'];

const STATUS_CFG: Record<number, { label: string; bg: string; border: string }> = {
    0: { label: 'Planlandı',  bg: '#EFF6FF', border: '#3B82F6' },
    1: { label: 'Tamamlandı', bg: '#F0FDF4', border: '#22C55E' },
    2: { label: 'İptal',      bg: '#FFF1F2', border: '#EF4444' },
    3: { label: 'Gelmedi',    bg: '#F9FAFB', border: '#9CA3AF' },
    4: { label: 'Ertelendi',  bg: '#FFF7ED', border: '#F97316' },
    5: { label: 'Talep',      bg: '#FEFCE8', border: '#EAB308' },
};

const DURATION_OPTS = [
    { label: '15 dakika', value: 15 },
    { label: '30 dakika', value: 30 },
    { label: '45 dakika', value: 45 },
    { label: '60 dakika', value: 60 },
    { label: '90 dakika', value: 90 },
];

// ─── Yardımcı fonksiyonlar ────────────────────────────────────────────────────
const getMonday = (d: Date): Date => {
    const date = new Date(d);
    const day  = date.getDay();
    date.setDate(date.getDate() - (day === 0 ? 6 : day - 1));
    date.setHours(0, 0, 0, 0);
    return date;
};

const addDays = (d: Date, n: number): Date => {
    const r = new Date(d);
    r.setDate(r.getDate() + n);
    return r;
};

const isSameDay = (a: Date, b: Date) =>
    a.getFullYear() === b.getFullYear() &&
    a.getMonth()    === b.getMonth()    &&
    a.getDate()     === b.getDate();

const toDateStr = (d: Date) =>
    `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

const fmtTime = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`;
};

// ─── Tipler ───────────────────────────────────────────────────────────────────
interface Appointment {
    id: number;
    patientId: number;
    patientName: string;
    appointmentDate: string;
    durationInMinutes: number;
    status: number;
    reason: string;
    isPast: boolean;
}
interface Patient  { id: number; fullName: string; }
interface ApptForm { patientId: number | null; date: string; time: string; duration: number; reason: string; }

const EMPTY_FORM: ApptForm = { patientId: null, date: '', time: '09:00', duration: 30, reason: '' };

// ─── Bileşen ──────────────────────────────────────────────────────────────────
const DietitianCalendarPage = () => {
    const [weekStart, setWeekStart]       = useState<Date>(() => getMonday(new Date()));
    const [appointments, setAppointments] = useState<Appointment[]>([]);
    const [patients, setPatients]         = useState<Patient[]>([]);
    const [loading, setLoading]           = useState(true);
    const [dialogVisible, setDialog]      = useState(false);
    const [form, setForm]                 = useState<ApptForm>(EMPTY_FORM);
    const [saving, setSaving]             = useState(false);
    const toast = useRef<Toast>(null);

    const weekDays = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i));

    // ── Veri yükle ────────────────────────────────────────────────────────────
    const load = useCallback(async () => {
        setLoading(true);
        try {
            const data = await AppointmentService.getAppointments();
            setAppointments(data ?? []);
        } catch (e: unknown) {
            const err = e as { type?: string; message?: string };
            if (err?.type !== 'auth') {
                toast.current?.show({
                    severity: 'warn', summary: 'Bağlantı Uyarısı',
                    detail: err?.message ?? 'Backend bağlı değil.', life: 4000,
                });
            }
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        load();
        PatientService.getAllPatients()
            .then((d: Patient[]) => setPatients(d))
            .catch(() => {});
    }, [load]);

    // ── Yeni randevu dialog aç ────────────────────────────────────────────────
    const openDialog = (day: Date) => {
        setForm({ ...EMPTY_FORM, date: toDateStr(day) });
        setDialog(true);
    };
    const closeDialog = () => { setDialog(false); setForm(EMPTY_FORM); };

    // ── Kaydet ────────────────────────────────────────────────────────────────
    const handleSave = async () => {
        if (!form.patientId || !form.date || !form.time || !form.reason.trim()) {
            toast.current?.show({ severity: 'warn', summary: 'Eksik Alan', detail: 'Tüm alanları doldurun.', life: 3000 });
            return;
        }
        setSaving(true);
        try {
            const isoDate = new Date(`${form.date}T${form.time}:00`).toISOString();
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            await AppointmentService.createAppointment({
                patientId:         form.patientId,
                appointmentDate:   isoDate as any,
                durationInMinutes: form.duration,
                status:            0,
                reason:            form.reason,
            });
            toast.current?.show({ severity: 'success', summary: 'Başarılı', detail: 'Randevu oluşturuldu.', life: 3000 });
            closeDialog();
            load();
        } catch (e: unknown) {
            const err = e as { message?: string };
            toast.current?.show({ severity: 'error', summary: 'Hata', detail: err?.message ?? 'Randevu oluşturulamadı.', life: 5000 });
        } finally {
            setSaving(false);
        }
    };

    // ── Onayla ────────────────────────────────────────────────────────────────
    const handleApprove = async (id: number) => {
        try {
            await AppointmentService.approveAppointment(id);
            toast.current?.show({ severity: 'success', summary: 'Onaylandı', detail: 'Randevu onaylandı.', life: 3000 });
            load();
        } catch (e: unknown) {
            const err = e as { message?: string };
            toast.current?.show({ severity: 'error', summary: 'Hata', detail: err?.message ?? 'Onaylama başarısız.', life: 4000 });
        }
    };

    // ── Sil ───────────────────────────────────────────────────────────────────
    const handleDelete = async (id: number, patientName: string) => {
        if (!window.confirm(`"${patientName}" randevusunu iptal etmek istiyor musunuz?`)) return;
        try {
            await AppointmentService.deleteAppointment(id);
            toast.current?.show({ severity: 'success', summary: 'İptal Edildi', detail: 'Randevu iptal edildi.', life: 3000 });
            load();
        } catch (e: unknown) {
            const err = e as { message?: string };
            toast.current?.show({ severity: 'error', summary: 'Hata', detail: err?.message ?? 'İptal başarısız.', life: 4000 });
        }
    };

    // ── Hafta etiketi ─────────────────────────────────────────────────────────
    const weekLabel = () => {
        const end = addDays(weekStart, 6);
        return weekStart.getMonth() === end.getMonth()
            ? `${weekStart.getDate()} – ${end.getDate()} ${MONTHS[weekStart.getMonth()]} ${weekStart.getFullYear()}`
            : `${weekStart.getDate()} ${MONTHS[weekStart.getMonth()]} – ${end.getDate()} ${MONTHS[end.getMonth()]} ${weekStart.getFullYear()}`;
    };

    const dialogFooter = (
        <>
            <Button label="İptal"  icon="pi pi-times" text    onClick={closeDialog} />
            <Button label="Kaydet" icon="pi pi-check" loading={saving} onClick={handleSave} />
        </>
    );

    return (
        <>
            <Toast ref={toast} position="top-right" />
            <div className="grid">
                <div className="col-12">
                    <div className="card">

                        {/* ── Başlık & Navigasyon ─────────────────────── */}
                        <div className="flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
                            <div className="flex align-items-center gap-2">
                                <Button icon="pi pi-chevron-left"  text rounded onClick={() => setWeekStart(d => addDays(d, -7))} />
                                <span className="font-semibold text-900 text-lg">{weekLabel()}</span>
                                <Button icon="pi pi-chevron-right" text rounded onClick={() => setWeekStart(d => addDays(d, 7))} />
                                <Button
                                    label="Bugün" text severity="secondary" className="text-sm"
                                    onClick={() => setWeekStart(getMonday(new Date()))}
                                />
                            </div>
                            <Button label="Yeni Randevu" icon="pi pi-plus" onClick={() => openDialog(new Date())} />
                        </div>

                        {loading ? (
                            <div className="flex justify-content-center align-items-center" style={{ minHeight: '300px' }}>
                                <ProgressSpinner style={{ width: '50px', height: '50px' }} strokeWidth="4" />
                            </div>
                        ) : (
                            <>
                                {/* ── Haftalık Takvim Izgarası ─────────── */}
                                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', gap: '4px' }}>
                                    {weekDays.map((day, i) => {
                                        const isToday  = isSameDay(day, new Date());
                                        const dayAppts = appointments
                                            .filter(a => isSameDay(new Date(a.appointmentDate), day))
                                            .sort((a, b) => new Date(a.appointmentDate).getTime() - new Date(b.appointmentDate).getTime());
                                        const hasRequested = dayAppts.some(a => a.status === 5);

                                        return (
                                            <div key={i}>
                                                {/* Gün Başlığı */}
                                                <div
                                                    className={`text-center py-2 px-1 border-round-top font-medium
                                                        ${isToday ? 'bg-primary text-white' : 'surface-100 text-700'}`}
                                                >
                                                    <div className="text-xs mb-1">{DAY_NAMES[i]}</div>
                                                    <div className="text-xl font-bold">{day.getDate()}</div>
                                                    {hasRequested && (
                                                        <div className="text-xs mt-1" style={{ color: isToday ? '#fef08a' : '#ca8a04' }}>
                                                            ● Talep
                                                        </div>
                                                    )}
                                                </div>

                                                {/* Randevular */}
                                                <div
                                                    className="border-1 surface-border border-round-bottom p-1"
                                                    style={{ minHeight: '260px' }}
                                                >
                                                    {dayAppts.length === 0 ? (
                                                        <p className="text-center text-300 text-xs mt-3 mb-0">—</p>
                                                    ) : (
                                                        dayAppts.map(appt => {
                                                            const cfg = STATUS_CFG[appt.status] ?? STATUS_CFG[0];
                                                            return (
                                                                <div
                                                                    key={appt.id}
                                                                    className="mb-1 p-2 border-round text-xs"
                                                                    style={{ background: cfg.bg, borderLeft: `3px solid ${cfg.border}` }}
                                                                >
                                                                    <div className="font-bold text-900">
                                                                        {fmtTime(appt.appointmentDate)}
                                                                    </div>
                                                                    <div
                                                                        className="text-700 my-1"
                                                                        style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
                                                                        title={appt.patientName}
                                                                    >
                                                                        {appt.patientName}
                                                                    </div>
                                                                    <div className="text-400 mb-1">{appt.durationInMinutes} dk — {cfg.label}</div>
                                                                    <div className="flex gap-1">
                                                                        {appt.status === 5 && (
                                                                            <button
                                                                                title="Onayla"
                                                                                onClick={() => handleApprove(appt.id)}
                                                                                style={{
                                                                                    background: '#22c55e', color: '#fff',
                                                                                    border: 'none', borderRadius: '4px',
                                                                                    cursor: 'pointer', fontSize: '10px', padding: '1px 5px',
                                                                                }}
                                                                            >✓ Onayla</button>
                                                                        )}
                                                                        {appt.status !== 2 && (
                                                                            <button
                                                                                title="İptal"
                                                                                onClick={() => handleDelete(appt.id, appt.patientName)}
                                                                                style={{
                                                                                    background: '#ef4444', color: '#fff',
                                                                                    border: 'none', borderRadius: '4px',
                                                                                    cursor: 'pointer', fontSize: '10px', padding: '1px 5px',
                                                                                }}
                                                                            >✕</button>
                                                                        )}
                                                                    </div>
                                                                </div>
                                                            );
                                                        })
                                                    )}
                                                    {/* Randevu Ekle */}
                                                    <button
                                                        onClick={() => openDialog(day)}
                                                        title="Randevu Ekle"
                                                        style={{
                                                            width: '100%', background: 'transparent', border: '1px dashed #cbd5e1',
                                                            borderRadius: '4px', cursor: 'pointer', color: '#94a3b8',
                                                            padding: '4px 0', marginTop: '4px', fontSize: '14px',
                                                        }}
                                                    >+</button>
                                                </div>
                                            </div>
                                        );
                                    })}
                                </div>

                                {/* ── Renk Açıklaması ─────────────────────── */}
                                <div className="flex flex-wrap gap-3 mt-3">
                                    {Object.entries(STATUS_CFG).map(([key, cfg]) => (
                                        <div key={key} className="flex align-items-center gap-1 text-xs text-600">
                                            <div style={{ width: '10px', height: '10px', borderRadius: '2px', background: cfg.border }} />
                                            {cfg.label}
                                        </div>
                                    ))}
                                </div>
                            </>
                        )}
                    </div>
                </div>
            </div>

            {/* ── Yeni Randevu Dialog ───────────────────────────────── */}
            <Dialog
                header="Yeni Randevu Ekle"
                visible={dialogVisible}
                style={{ width: '460px' }}
                modal
                footer={dialogFooter}
                onHide={closeDialog}
            >
                <div className="p-fluid grid formgrid mt-2">
                    <div className="field col-12">
                        <label>Hasta *</label>
                        <Dropdown
                            value={form.patientId}
                            options={patients.map(p => ({ label: p.fullName, value: p.id }))}
                            onChange={e => setForm(f => ({ ...f, patientId: e.value }))}
                            placeholder="Hasta seçin..."
                            filter
                            filterPlaceholder="İsim ara..."
                            emptyMessage="Hasta bulunamadı"
                        />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label>Tarih *</label>
                        <InputText
                            type="date"
                            value={form.date}
                            onChange={e => setForm(f => ({ ...f, date: e.target.value }))}
                        />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label>Saat *</label>
                        <InputText
                            type="time"
                            value={form.time}
                            onChange={e => setForm(f => ({ ...f, time: e.target.value }))}
                        />
                    </div>
                    <div className="field col-12">
                        <label>Süre</label>
                        <Dropdown
                            value={form.duration}
                            options={DURATION_OPTS}
                            onChange={e => setForm(f => ({ ...f, duration: e.value }))}
                        />
                    </div>
                    <div className="field col-12">
                        <label>Randevu Konusu *</label>
                        <InputText
                            value={form.reason}
                            onChange={e => setForm(f => ({ ...f, reason: e.target.value }))}
                            placeholder="Örn: İlk görüşme, takip seansı..."
                        />
                    </div>
                </div>
            </Dialog>
        </>
    );
};

export default DietitianCalendarPage;
