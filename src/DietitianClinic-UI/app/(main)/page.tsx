'use client';

import { Chart }     from 'primereact/chart';
import { Column }    from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { Skeleton }  from 'primereact/skeleton';
import { Tag }       from 'primereact/tag';
import { Toast }     from 'primereact/toast';
import React, { useContext, useEffect, useRef, useState } from 'react';
import { LayoutContext } from '../../layout/context/layoutcontext';
import { DashboardService } from '../../service/DashboardService';

// ─── Palette ─────────────────────────────────────────────────────────────────
const NAVY  = '#2f4860';
const ROSE  = '#C9908C';
const NAVY2 = '#4a6785';
const ROSE2 = '#e8d5d4';

// ─── Sabitler ─────────────────────────────────────────────────────────────────
const MONTHS = ['Oca','Şub','Mar','Nis','May','Haz','Tem','Ağu','Eyl','Eki','Kas','Ara'];

const STATUS_MAP: Record<number,string>                            = { 0:'Planlandı',1:'Tamamlandı',2:'İptal',3:'Gelmedi',4:'Ertelendi',5:'Talep' };
const SEVERITY_MAP: Record<number,'info'|'success'|'danger'|'warning'> = { 0:'info',1:'success',2:'danger',3:'warning',4:'warning',5:'warning' };

const fmtTime = (iso: string) => { const d = new Date(iso); return `${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`; };

// Son 12 ay etiketleri
const last12Labels = () => {
    const now = new Date();
    return Array.from({ length: 12 }, (_, i) => {
        const d = new Date(now.getFullYear(), now.getMonth() - 11 + i, 1);
        return { label: MONTHS[d.getMonth()], year: d.getFullYear(), month: d.getMonth() + 1 };
    });
};

// ─── Skeleton kartı ───────────────────────────────────────────────────────────
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
const Dashboard = () => {
    const { layoutConfig } = useContext(LayoutContext);
    const toast = useRef<Toast>(null);
    const isDark = layoutConfig.colorScheme === 'dark';

    const [summary,   setSummary]   = useState<any>(null);
    const [analytics, setAnalytics] = useState<any>(null);
    const [loading,   setLoading]   = useState(true);

    useEffect(() => {
        Promise.all([
            DashboardService.getSummary().catch(() => null),
            DashboardService.getAnalytics().catch(() => null),
        ]).then(([s, a]) => {
            setSummary(s);
            setAnalytics(a);
        }).finally(() => setLoading(false));
    }, []);

    // ── Grafik verisi: aylık line chart ──────────────────────────────────────
    const lineData = (() => {
        const labels = last12Labels();
        const patients = labels.map(l =>
            analytics?.monthlyPatients?.find((p: any) => p.year === l.year && p.month === l.month)?.count ?? 0
        );
        const appointments = labels.map(l =>
            analytics?.monthlyAppointments?.find((a: any) => a.year === l.year && a.month === l.month)?.count ?? 0
        );
        return {
            labels: labels.map(l => l.label),
            datasets: [
                {
                    label: 'Yeni Hastalar',
                    data: patients,
                    fill: true,
                    borderColor: NAVY,
                    backgroundColor: 'rgba(47,72,96,0.1)',
                    tension: 0.4,
                    pointBackgroundColor: NAVY,
                    pointRadius: 4,
                },
                {
                    label: 'Randevular',
                    data: appointments,
                    fill: true,
                    borderColor: ROSE,
                    backgroundColor: 'rgba(201,144,140,0.1)',
                    tension: 0.4,
                    pointBackgroundColor: ROSE,
                    pointRadius: 4,
                },
            ],
        };
    })();

    // ── Grafik verisi: diyetisyen pie chart ──────────────────────────────────
    const pieData = (() => {
        const dist: { name: string; count: number }[] = analytics?.dietitianDist ?? [];
        const bgColors = [NAVY, ROSE, NAVY2, ROSE2, '#7e9bbf', '#d4a5a2', '#3d5a74', '#b87c78'];
        return {
            labels: dist.length > 0 ? dist.map(d => d.name) : ['Veri yok'],
            datasets: [{
                data: dist.length > 0 ? dist.map(d => d.count) : [1],
                backgroundColor: bgColors.slice(0, Math.max(dist.length, 1)),
                borderWidth: 2,
                borderColor: isDark ? '#1e1e2e' : '#ffffff',
            }],
        };
    })();

    // ── Chart options ─────────────────────────────────────────────────────────
    const textColor   = isDark ? '#ebedef' : '#495057';
    const gridColor   = isDark ? 'rgba(160,167,181,.2)' : '#ebedef';

    const lineOptions = {
        maintainAspectRatio: false,
        plugins: { legend: { labels: { color: textColor, usePointStyle: true } } },
        scales: {
            x: { ticks: { color: textColor }, grid: { color: gridColor } },
            y: { ticks: { color: textColor }, grid: { color: gridColor }, beginAtZero: true },
        },
    };

    const pieOptions = {
        maintainAspectRatio: false,
        plugins: {
            legend: { labels: { color: textColor, usePointStyle: true }, position: 'bottom' as const },
        },
    };

    // ── Bugünkü program ───────────────────────────────────────────────────────
    const todaySchedule = summary?.todaySchedule ?? [];

    return (
        <>
            <Toast ref={toast} position="top-right" />
            <div className="grid">

                {/* ── Sayfa başlığı ──────────────────────────────────── */}
                <div className="col-12">
                    <div className="flex align-items-center gap-3 mb-2">
                        <div
                            className="flex align-items-center justify-content-center border-round"
                            style={{ width: '3rem', height: '3rem', background: NAVY }}
                        >
                            <i className="pi pi-chart-bar text-white text-xl" />
                        </div>
                        <div>
                            <h4 className="m-0 text-900">Klinik Yönetim Merkezi</h4>
                            <span className="text-500 text-sm">Gerçek zamanlı klinik özeti</span>
                        </div>
                    </div>
                </div>

                {/* ── Stat Kartları ──────────────────────────────────── */}
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
                                        <span className="block text-500 font-medium mb-3">Toplam Hasta</span>
                                        <div className="text-900 font-bold text-3xl">{summary?.totalPatients ?? 0}</div>
                                    </div>
                                    <div className="flex align-items-center justify-content-center border-round" style={{ width:'2.5rem',height:'2.5rem',background:ROSE2 }}>
                                        <i className="pi pi-users text-xl" style={{ color: NAVY }} />
                                    </div>
                                </div>
                                <span className="font-medium" style={{ color: NAVY }}>{summary?.totalDietitians ?? 0} diyetisyen </span>
                                <span className="text-500">klinikte aktif</span>
                            </div>
                        </div>

                        <div className="col-12 lg:col-6 xl:col-3">
                            <div className="card mb-0" style={{ borderTop: `3px solid ${ROSE}` }}>
                                <div className="flex justify-content-between mb-3">
                                    <div>
                                        <span className="block text-500 font-medium mb-3">Toplam Randevu</span>
                                        <div className="text-900 font-bold text-3xl">{summary?.totalAppointments ?? 0}</div>
                                    </div>
                                    <div className="flex align-items-center justify-content-center border-round" style={{ width:'2.5rem',height:'2.5rem',background:'#fde8d8' }}>
                                        <i className="pi pi-calendar text-xl" style={{ color: ROSE }} />
                                    </div>
                                </div>
                                <span className="font-medium text-green-500">{summary?.upcomingAppointments ?? 0} yaklaşan </span>
                                <span className="text-500">randevu</span>
                            </div>
                        </div>

                        <div className="col-12 lg:col-6 xl:col-3">
                            <div className="card mb-0" style={{ borderTop: `3px solid ${NAVY2}` }}>
                                <div className="flex justify-content-between mb-3">
                                    <div>
                                        <span className="block text-500 font-medium mb-3">Aktif Diyet Planı</span>
                                        <div className="text-900 font-bold text-3xl">{summary?.activeMealPlans ?? 0}</div>
                                    </div>
                                    <div className="flex align-items-center justify-content-center border-round" style={{ width:'2.5rem',height:'2.5rem',background:'#e0e7f0' }}>
                                        <i className="pi pi-list text-xl" style={{ color: NAVY2 }} />
                                    </div>
                                </div>
                                <span className="font-medium text-green-500">Güncel </span>
                                <span className="text-500">diyet programları</span>
                            </div>
                        </div>

                        <div className="col-12 lg:col-6 xl:col-3">
                            <div className="card mb-0" style={{ borderTop: `3px solid #e67e22` }}>
                                <div className="flex justify-content-between mb-3">
                                    <div>
                                        <span className="block text-500 font-medium mb-3">Bugünkü Seanslar</span>
                                        <div className="text-900 font-bold text-3xl">{summary?.todayAppointments ?? 0}</div>
                                    </div>
                                    <div className="flex align-items-center justify-content-center border-round" style={{ width:'2.5rem',height:'2.5rem',background:'#fef3e2' }}>
                                        <i className="pi pi-clock text-xl text-orange-500" />
                                    </div>
                                </div>
                                <span className="font-medium text-orange-500">{summary?.pendingRequests ?? 0} talep </span>
                                <span className="text-500">onay bekliyor</span>
                            </div>
                        </div>
                    </>
                )}

                {/* ── Line Chart: Aylık Kayıtlar ─────────────────────── */}
                <div className="col-12 xl:col-7">
                    <div className="card h-full">
                        <div className="flex align-items-center justify-content-between mb-3">
                            <h5 className="m-0">Aylık Kayıt Trendi</h5>
                            <span className="text-500 text-sm">Son 12 ay</span>
                        </div>
                        {loading ? (
                            <Skeleton width="100%" height="280px" />
                        ) : (
                            <div style={{ height: '280px' }}>
                                <Chart type="line" data={lineData} options={lineOptions} style={{ height: '100%' }} />
                            </div>
                        )}
                    </div>
                </div>

                {/* ── Pie Chart: Diyetisyen Dağılımı ────────────────── */}
                <div className="col-12 xl:col-5">
                    <div className="card h-full">
                        <div className="flex align-items-center justify-content-between mb-3">
                            <h5 className="m-0">Diyetisyen Dağılımı</h5>
                            <span className="text-500 text-sm">Hasta sayısı</span>
                        </div>
                        {loading ? (
                            <div className="flex justify-content-center align-items-center" style={{ height: '280px' }}>
                                <Skeleton shape="circle" size="220px" />
                            </div>
                        ) : (
                            <div style={{ height: '280px' }}>
                                <Chart type="pie" data={pieData} options={pieOptions} style={{ height: '100%' }} />
                            </div>
                        )}
                    </div>
                </div>

                {/* ── Bugünkü Program ────────────────────────────────── */}
                <div className="col-12">
                    <div className="card">
                        <div className="flex align-items-center justify-content-between mb-3">
                            <h5 className="m-0">Bugünün Programı</h5>
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
                                emptyMessage="Bugün için randevu bulunmuyor."
                                stripedRows
                                dataKey="id"
                                showGridlines
                            >
                                <Column field="patientName" header="Hasta" sortable />
                                <Column header="Saat"    body={(r: any) => fmtTime(r.appointmentDate)} style={{ width: '80px' }} />
                                <Column field="durationInMinutes" header="Süre (dk)" style={{ width: '100px' }} />
                                <Column
                                    header="Durum"
                                    body={(r: any) => <Tag value={STATUS_MAP[r.status] ?? '—'} severity={SEVERITY_MAP[r.status] ?? 'info'} />}
                                    style={{ width: '120px' }}
                                />
                            </DataTable>
                        )}
                    </div>
                </div>

            </div>
        </>
    );
};

export default Dashboard;
