'use client';

import { Column }    from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { Skeleton }  from 'primereact/skeleton';
import { Tag }       from 'primereact/tag';
import React, { useEffect, useState } from 'react';
import { AuthService }       from '../../../service/AuthService';
import { DashboardService }  from '../../../service/DashboardService';
import { MessageService }    from '../../../service/MessageService';

// ─── Palette ──────────────────────────────────────────────────────────────────
const NAVY = '#2f4860';
const ROSE = '#C9908C';

// ─── Tipler ───────────────────────────────────────────────────────────────────
const STATUS_MAP: Record<number, string>                                 = { 0:'Planlandı', 1:'Tamamlandı', 2:'İptal', 3:'Gelmedi', 4:'Ertelendi', 5:'Talep' };
const SEVERITY_MAP: Record<number, 'info'|'success'|'danger'|'warning'> = { 0:'info', 1:'success', 2:'danger', 3:'warning', 4:'warning', 5:'warning' };

const fmtTime = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`;
};

// ─── Skeleton Kart ────────────────────────────────────────────────────────────
const StatSkeleton = () => (
    <div className="card mb-0">
        <div className="flex justify-content-between mb-3">
            <div style={{ width: '60%' }}>
                <Skeleton width="80%" height="1rem" className="mb-2" />
                <Skeleton width="40%" height="1.5rem" />
            </div>
            <Skeleton shape="circle" size="2.5rem" />
        </div>
        <Skeleton width="60%" height="0.85rem" />
    </div>
);

// ─── Bileşen ──────────────────────────────────────────────────────────────────
const DietitianDashboard = () => {
    const [dietitianName, setDietitianName] = useState('');
    const [summary,       setSummary]       = useState<any>(null);
    const [unreadCount,   setUnreadCount]   = useState<number>(0);
    const [loading,       setLoading]       = useState(true);

    useEffect(() => {
        const user = AuthService.getCurrentUser();
        if (user?.fullName) setDietitianName(user.fullName);

        Promise.all([
            DashboardService.getSummary().catch(() => null),
            MessageService.getUnreadCount().catch(() => 0),
        ]).then(([s, unread]) => {
            setSummary(s);
            setUnreadCount(unread as number);
        }).finally(() => setLoading(false));
    }, []);

    const todaySchedule: any[] = summary?.todaySchedule ?? [];
    const pendingCount          = summary?.pendingRequests ?? 0;

    const statusBody = (row: any) => (
        <Tag value={STATUS_MAP[row.status] ?? '—'} severity={SEVERITY_MAP[row.status] ?? 'info'} />
    );

    return (
        <div className="grid">

            {/* ── Hoş Geldin Kartı ─────────────────────────────────── */}
            <div className="col-12">
                <div className="card" style={{ background: `linear-gradient(135deg, ${NAVY} 0%, #4a6785 100%)`, border: 'none' }}>
                    <div className="text-white">
                        <div className="text-lg font-medium mb-1 opacity-80">Hoş Geldiniz,</div>
                        <div className="text-3xl font-bold mb-2">{dietitianName || 'Diyetisyen'}</div>
                        <div className="opacity-80 text-sm">
                            {loading
                                ? 'Yükleniyor…'
                                : `Bugün ${summary?.todayAppointments ?? 0} seans · ${pendingCount} bekleyen talep`}
                        </div>
                    </div>
                </div>
            </div>

            {/* ── Stat Kartları ─────────────────────────────────────── */}
            {loading ? (
                [0,1,2,3].map(i => (
                    <div key={i} className="col-12 lg:col-6 xl:col-3"><StatSkeleton /></div>
                ))
            ) : (
                <>
                    <div className="col-12 lg:col-6 xl:col-3">
                        <div className="card mb-0" style={{ borderTop: `3px solid ${NAVY}` }}>
                            <div className="flex justify-content-between mb-3">
                                <div>
                                    <span className="block text-500 font-medium mb-3">Toplam Danışan</span>
                                    <div className="text-900 font-bold text-3xl">{summary?.totalPatients ?? 0}</div>
                                </div>
                                <div className="flex align-items-center justify-content-center border-round" style={{ width:'2.5rem',height:'2.5rem',background:'#e0e7f0' }}>
                                    <i className="pi pi-users text-xl" style={{ color: NAVY }} />
                                </div>
                            </div>
                            <span className="text-500">Aktif danışanlar</span>
                        </div>
                    </div>

                    <div className="col-12 lg:col-6 xl:col-3">
                        <div className="card mb-0" style={{ borderTop: `3px solid ${ROSE}` }}>
                            <div className="flex justify-content-between mb-3">
                                <div>
                                    <span className="block text-500 font-medium mb-3">Bugünkü Seanslar</span>
                                    <div className="text-900 font-bold text-3xl">{summary?.todayAppointments ?? 0}</div>
                                </div>
                                <div className="flex align-items-center justify-content-center border-round" style={{ width:'2.5rem',height:'2.5rem',background:'#fde8d8' }}>
                                    <i className="pi pi-calendar text-xl" style={{ color: ROSE }} />
                                </div>
                            </div>
                            <span className="font-medium text-orange-500">{pendingCount} talep </span>
                            <span className="text-500">bekliyor</span>
                        </div>
                    </div>

                    <div className="col-12 lg:col-6 xl:col-3">
                        <div className="card mb-0" style={{ borderTop: `3px solid #4a6785` }}>
                            <div className="flex justify-content-between mb-3">
                                <div>
                                    <span className="block text-500 font-medium mb-3">Aktif Planlar</span>
                                    <div className="text-900 font-bold text-3xl">{summary?.activeMealPlans ?? 0}</div>
                                </div>
                                <div className="flex align-items-center justify-content-center border-round" style={{ width:'2.5rem',height:'2.5rem',background:'#e8f0f7' }}>
                                    <i className="pi pi-list text-xl" style={{ color: '#4a6785' }} />
                                </div>
                            </div>
                            <span className="text-500">Güncel diyet programları</span>
                        </div>
                    </div>

                    <div className="col-12 lg:col-6 xl:col-3">
                        <div className="card mb-0" style={{ borderTop: `3px solid #9b59b6` }}>
                            <div className="flex justify-content-between mb-3">
                                <div>
                                    <span className="block text-500 font-medium mb-3">Mesajlar</span>
                                    <div className="text-900 font-bold text-3xl">{unreadCount}</div>
                                </div>
                                <div className="flex align-items-center justify-content-center bg-purple-100 border-round" style={{ width:'2.5rem',height:'2.5rem' }}>
                                    <i className="pi pi-comments text-purple-500 text-xl" />
                                </div>
                            </div>
                            <span className="text-500">okunmamış mesaj</span>
                        </div>
                    </div>
                </>
            )}

            {/* ── Bugünün Randevuları ───────────────────────────────── */}
            <div className="col-12">
                <div className="card">
                    <div className="flex align-items-center justify-content-between mb-3">
                        <h5 className="m-0">Bugünün Randevuları</h5>
                        <Tag
                            value={`${summary?.todayAppointments ?? 0} seans`}
                            style={{ background: NAVY, color: '#fff' }}
                        />
                    </div>
                    {loading ? (
                        <div className="flex flex-column gap-2">
                            {[0,1,2,3].map(i => <Skeleton key={i} width="100%" height="2.5rem" />)}
                        </div>
                    ) : (
                        <DataTable
                            value={todaySchedule}
                            stripedRows
                            dataKey="id"
                            emptyMessage="Bugün için randevu bulunmuyor."
                            showGridlines
                        >
                            <Column field="patientName"       header="Danışan" sortable />
                            <Column header="Saat"             body={(r: any) => fmtTime(r.appointmentDate)} style={{ width: '80px' }} />
                            <Column field="durationInMinutes" header="Süre (dk)"                            style={{ width: '90px' }} />
                            <Column header="Durum"            body={statusBody}                             style={{ width: '120px' }} />
                        </DataTable>
                    )}
                </div>
            </div>

        </div>
    );
};

export default DietitianDashboard;
