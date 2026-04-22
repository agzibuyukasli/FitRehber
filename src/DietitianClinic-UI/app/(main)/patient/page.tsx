'use client';
import { Button } from 'primereact/button';
import { Tag } from 'primereact/tag';
import { Toast } from 'primereact/toast';
import Link from 'next/link';
import React, { useEffect, useRef, useState } from 'react';
import { AuthService } from '../../../service/AuthService';
import { AppointmentService } from '../../../service/AppointmentService';

interface Appointment {
    id: number;
    patientName: string;
    dietitianName: string;
    appointmentDate: string;
    durationInMinutes: number;
    status: number;
    reason: string;
}

const MONTHS = ['Ocak','Şubat','Mart','Nisan','Mayıs','Haziran','Temmuz','Ağustos','Eylül','Ekim','Kasım','Aralık'];

const fmtDateTime = (iso: string) => {
    const d = new Date(iso);
    return {
        date: `${d.getDate()} ${MONTHS[d.getMonth()]} ${d.getFullYear()}`,
        time: `${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`,
        weekday: ['Pazar','Pazartesi','Salı','Çarşamba','Perşembe','Cuma','Cumartesi'][d.getDay()],
    };
};

const PatientDashboard = () => {
    const [patientName, setPatientName]       = useState('');
    const [nextAppt, setNextAppt]             = useState<Appointment | null | 'loading'>('loading');
    const [recentAppts, setRecentAppts]       = useState<Appointment[]>([]);
    const toast = useRef<Toast>(null);

    useEffect(() => {
        const user = AuthService.getCurrentUser();
        if (user?.fullName) setPatientName(user.fullName);
    }, []);

    useEffect(() => {
        AppointmentService.getMyAppointments()
            .then((data: Appointment[] | undefined) => {
                if (!data) return;
                const now = new Date();
                const upcoming = data
                    .filter(a => new Date(a.appointmentDate) > now && (a.status === 0 || a.status === 5))
                    .sort((a, b) => new Date(a.appointmentDate).getTime() - new Date(b.appointmentDate).getTime());

                setNextAppt(upcoming[0] ?? null);

                const recent = data
                    .filter(a => a.status === 1)
                    .sort((a, b) => new Date(b.appointmentDate).getTime() - new Date(a.appointmentDate).getTime())
                    .slice(0, 3);
                setRecentAppts(recent);
            })
            .catch(() => {
                setNextAppt(null);
                toast.current?.show({
                    severity: 'warn', summary: 'Bağlantı',
                    detail: 'Randevu bilgileri alınamadı.', life: 4000,
                });
            });
    }, []);

    const nextDt = nextAppt && nextAppt !== 'loading' ? fmtDateTime(nextAppt.appointmentDate) : null;

    return (
        <>
            <Toast ref={toast} position="top-right" />
            <div className="grid">

                {/* ── Hoş Geldin ──────────────────────────────────── */}
                <div className="col-12">
                    <div className="card" style={{ background: 'linear-gradient(135deg,#00bb7e 0%,#2f4860 100%)' }}>
                        <div className="text-white">
                            <div className="text-xl font-medium mb-1">Hoş Geldiniz,</div>
                            <div className="text-3xl font-bold mb-2">{patientName || 'Danışan'}</div>
                            <div className="text-green-100">Sağlıklı bir gün geçirmeniz dileğiyle!</div>
                        </div>
                    </div>
                </div>

                {/* ── Sonraki Randevum ─────────────────────────────── */}
                <div className="col-12 md:col-6">
                    <div className="card" style={{ minHeight: '140px' }}>
                        <h5 className="mb-3">Bir Sonraki Randevum</h5>

                        {nextAppt === 'loading' && (
                            <p className="text-500">Yükleniyor...</p>
                        )}

                        {nextAppt === null && (
                            <div className="flex align-items-center gap-3">
                                <div className="flex align-items-center justify-content-center bg-gray-100 border-circle" style={{ width:'3rem', height:'3rem' }}>
                                    <i className="pi pi-calendar text-gray-400 text-xl" />
                                </div>
                                <div>
                                    <div className="text-700 font-medium">Henüz planlanmış randevunuz yok.</div>
                                    <div className="text-500 text-sm mt-1">Diyetisyeninizden randevu talep edebilirsiniz.</div>
                                </div>
                            </div>
                        )}

                        {nextAppt && nextAppt !== 'loading' && nextDt && (
                            <div className="flex align-items-start gap-3">
                                <div
                                    className="flex align-items-center justify-content-center border-circle flex-shrink-0"
                                    style={{ width:'3.5rem', height:'3.5rem', background:'linear-gradient(135deg,#00bb7e,#2f4860)' }}
                                >
                                    <i className="pi pi-calendar text-white text-xl" />
                                </div>
                                <div className="flex-1">
                                    <div className="text-900 font-bold text-xl mb-1">{nextDt.time}</div>
                                    <div className="text-700 font-medium mb-1">
                                        {nextDt.weekday}, {nextDt.date}
                                    </div>
                                    <div className="text-500 text-sm mb-2">
                                        Diyetisyen: <span className="font-medium text-700">{nextAppt.dietitianName}</span>
                                        &nbsp;·&nbsp;{nextAppt.durationInMinutes} dakika
                                    </div>
                                    {nextAppt.reason && (
                                        <div className="text-500 text-sm">{nextAppt.reason}</div>
                                    )}
                                    <Tag
                                        className="mt-2"
                                        value={nextAppt.status === 5 ? 'Onay Bekleniyor' : 'Planlandı'}
                                        severity={nextAppt.status === 5 ? 'warning' : 'info'}
                                    />
                                </div>
                            </div>
                        )}
                    </div>
                </div>

                {/* ── Özet Kartlar ─────────────────────────────────── */}
                <div className="col-12 md:col-6">
                    <div className="grid m-0">
                        <div className="col-6 p-1">
                            <div className="card mb-0 text-center">
                                <div className="text-500 text-sm mb-1">Güncel Kilo</div>
                                <div className="text-2xl font-bold text-900">75.8 kg</div>
                                <div className="text-green-500 text-xs mt-1">−6.2 kg</div>
                            </div>
                        </div>
                        <div className="col-6 p-1">
                            <div className="card mb-0 text-center">
                                <div className="text-500 text-sm mb-1">Su Tüketimi</div>
                                <div className="text-2xl font-bold text-900">1.4 L</div>
                                <div className="text-blue-500 text-xs mt-1">Hedef: 2.5 L</div>
                            </div>
                        </div>
                        <div className="col-6 p-1">
                            <div className="card mb-0 text-center">
                                <div className="text-500 text-sm mb-1">Diyet Uyumu</div>
                                <div className="text-2xl font-bold text-900">%82</div>
                                <div className="text-green-500 text-xs mt-1">Bu hafta</div>
                            </div>
                        </div>
                        <div className="col-6 p-1">
                            <div className="card mb-0 text-center">
                                <div className="text-500 text-sm mb-1">Geçmiş Seans</div>
                                <div className="text-2xl font-bold text-900">{recentAppts.length}</div>
                                <div className="text-500 text-xs mt-1">Tamamlandı</div>
                            </div>
                        </div>
                    </div>
                </div>

                {/* ── Hızlı Erişim ─────────────────────────────────── */}
                <div className="col-12">
                    <div className="card">
                        <h5>Hızlı Erişim</h5>
                        <div className="flex flex-wrap gap-3">
                            <Link href="/patient/diet">
                                <Button label="Bugünkü Diyet Listem" icon="pi pi-list" className="p-button-outlined" />
                            </Link>
                            <Link href="/patient/progress">
                                <Button label="Gelişim Grafiğim" icon="pi pi-chart-line" className="p-button-outlined p-button-success" />
                            </Link>
                            <Link href="/patient/tracking">
                                <Button label="Su & Not Ekle" icon="pi pi-pencil" className="p-button-outlined p-button-info" />
                            </Link>
                        </div>
                    </div>
                </div>

                {/* ── Diyetisyen Notları ────────────────────────────── */}
                <div className="col-12 md:col-6">
                    <div className="card">
                        <h5>Diyetisyen Notları</h5>
                        <ul className="list-none p-0 m-0">
                            {['Su tüketiminizi artırın, günde en az 2.5 litre hedefleyin.',
                              'Akşam yemeklerinde karbonhidrat miktarını azaltın.',
                              'Haftada 3 gün 30 dakika yürüyüş yapmayı deneyin.'].map((note, i) => (
                                <li key={i} className="flex align-items-start py-2 border-bottom-1 surface-border">
                                    <i className="pi pi-comment text-primary mr-2 mt-1 flex-shrink-0" />
                                    <span className="text-700 text-sm">{note}</span>
                                </li>
                            ))}
                        </ul>
                    </div>
                </div>

                {/* ── Geçmiş Seanslar ───────────────────────────────── */}
                <div className="col-12 md:col-6">
                    <div className="card">
                        <h5>Geçmiş Seanslar</h5>
                        {recentAppts.length === 0 ? (
                            <p className="text-500 text-sm">Tamamlanmış seans bulunamadı.</p>
                        ) : (
                            <div className="flex flex-column gap-2">
                                {recentAppts.map(a => {
                                    const dt = fmtDateTime(a.appointmentDate);
                                    return (
                                        <div key={a.id} className="flex justify-content-between align-items-center p-2 surface-100 border-round">
                                            <div>
                                                <div className="font-medium text-900 text-sm">{dt.date} — {dt.time}</div>
                                                <div className="text-500 text-xs">{a.reason}</div>
                                            </div>
                                            <Tag value="Tamamlandı" severity="success" />
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                    </div>
                </div>

            </div>
        </>
    );
};

export default PatientDashboard;
