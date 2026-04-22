'use client';

import { Button }    from 'primereact/button';
import { Column }    from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { InputText } from 'primereact/inputtext';
import { Skeleton }  from 'primereact/skeleton';
import { Tag }       from 'primereact/tag';
import { Toast }     from 'primereact/toast';
import { Toolbar }   from 'primereact/toolbar';
import React, { useEffect, useRef, useState } from 'react';
import { DashboardService } from '../../../service/DashboardService';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

// ─── Palette ──────────────────────────────────────────────────────────────────
const NAVY = '#2f4860';
const ROSE = '#C9908C';

// ─── Tipler ───────────────────────────────────────────────────────────────────
interface ReportRow {
    patientName:           string;
    email:                 string;
    dietitianName:         string;
    totalAppointments:     number;
    completedAppointments: number;
    measurementCount:      number;
    latestWeight:          number | null;
    latestBmi:             number | null;
    registeredDate:        string;
    isActive:              boolean;
}

const MONTHS = ['Oca','Şub','Mar','Nis','May','Haz','Tem','Ağu','Eyl','Eki','Kas','Ara'];
const fmtDate = (iso: string) => {
    const d = new Date(iso);
    return `${d.getDate()} ${MONTHS[d.getMonth()]} ${d.getFullYear()}`;
};

// ─── Bileşen ──────────────────────────────────────────────────────────────────
const ReportsPage = () => {
    const toast    = useRef<Toast>(null);
    const dtRef    = useRef<DataTable<ReportRow[]>>(null);
    const [rows,     setRows]     = useState<ReportRow[]>([]);
    const [loading,  setLoading]  = useState(true);
    const [filter,   setFilter]   = useState('');

    useEffect(() => {
        DashboardService.getReports()
            .then((data: ReportRow[]) => setRows(data ?? []))
            .catch(() => {
                toast.current?.show({ severity: 'error', summary: 'Hata', detail: 'Rapor verileri alınamadı.', life: 4000 });
            })
            .finally(() => setLoading(false));
    }, []);

    // ── CSV dışa aktarma ──────────────────────────────────────────────────────
    const exportCSV = () => {
        dtRef.current?.exportCSV();
    };

    // ── PDF dışa aktarma ──────────────────────────────────────────────────────
    const exportPDF = () => {
        const doc = new jsPDF({ orientation: 'landscape' });

        // Başlık bandı
        doc.setFillColor(47, 72, 96);
        doc.rect(0, 0, doc.internal.pageSize.width, 18, 'F');
        doc.setFontSize(13);
        doc.setTextColor(255, 255, 255);
        doc.text('FitRehber — Klinik Raporu', 14, 12);
        doc.setFontSize(9);
        doc.text(`Oluşturma: ${new Date().toLocaleDateString('tr-TR')}`, doc.internal.pageSize.width - 14, 12, { align: 'right' });

        autoTable(doc, {
            startY: 22,
            head: [[
                'Hasta',
                'E-posta',
                'Diyetisyen',
                'Top. Randevu',
                'Tamamlanan',
                'Ölçüm Sayısı',
                'Son Kilo (kg)',
                'Son BMI',
                'Kayıt Tarihi',
                'Durum',
            ]],
            body: rows.map(r => [
                r.patientName,
                r.email,
                r.dietitianName,
                r.totalAppointments,
                r.completedAppointments,
                r.measurementCount,
                r.latestWeight != null ? r.latestWeight.toFixed(1) : '—',
                r.latestBmi    != null ? r.latestBmi.toFixed(1)    : '—',
                fmtDate(r.registeredDate),
                r.isActive ? 'Aktif' : 'Pasif',
            ]),
            headStyles: { fillColor: [47, 72, 96], textColor: 255, fontStyle: 'bold', fontSize: 8 },
            bodyStyles: { fontSize: 8 },
            alternateRowStyles: { fillColor: [248, 240, 240] },
            columnStyles: { 0: { cellWidth: 32 }, 1: { cellWidth: 38 } },
            didDrawPage: (data: any) => {
                const pageCount = (doc as any).internal.getNumberOfPages();
                doc.setFontSize(8);
                doc.setTextColor(150);
                doc.text(`Sayfa ${data.pageNumber} / ${pageCount}`, doc.internal.pageSize.width / 2, doc.internal.pageSize.height - 6, { align: 'center' });
            },
        });

        doc.save(`FitRehber_Rapor_${new Date().toLocaleDateString('tr-TR').replace(/\//g, '-')}.pdf`);
    };

    // ── Toolbar içerikleri ────────────────────────────────────────────────────
    const leftContent = (
        <div className="flex align-items-center gap-3">
            <div className="flex align-items-center justify-content-center border-round" style={{ width:'2.5rem',height:'2.5rem',background:NAVY }}>
                <i className="pi pi-chart-line text-white" />
            </div>
            <div>
                <div className="font-semibold text-900">Klinik Raporları</div>
                <div className="text-500 text-sm">{loading ? '...' : `${rows.length} hasta kaydı`}</div>
            </div>
        </div>
    );

    const rightContent = (
        <div className="flex gap-2">
            <Button
                label="CSV"
                icon="pi pi-file"
                severity="secondary"
                outlined
                onClick={exportCSV}
                disabled={loading || rows.length === 0}
                tooltip="Excel ile açılabilir CSV dosyası"
                tooltipOptions={{ position: 'top' }}
            />
            <Button
                label="PDF"
                icon="pi pi-file-pdf"
                onClick={exportPDF}
                disabled={loading || rows.length === 0}
                style={{ background: ROSE, border: 'none' }}
                tooltip="PDF raporu indir"
                tooltipOptions={{ position: 'top' }}
            />
        </div>
    );

    // ── Kolon renderers ───────────────────────────────────────────────────────
    const activeBody = (row: ReportRow) => (
        <Tag
            value={row.isActive ? 'Aktif' : 'Pasif'}
            severity={row.isActive ? 'success' : 'warning'}
        />
    );

    const weightBody = (row: ReportRow) =>
        row.latestWeight != null ? `${row.latestWeight.toFixed(1)} kg` : <span className="text-400">—</span>;

    const bmiBody = (row: ReportRow) => {
        if (row.latestBmi == null) return <span className="text-400">—</span>;
        const bmi = row.latestBmi;
        const color = bmi < 18.5 ? '#3b82f6' : bmi < 25 ? '#22c55e' : bmi < 30 ? '#f59e0b' : '#ef4444';
        return <span style={{ color, fontWeight: 600 }}>{bmi.toFixed(1)}</span>;
    };

    const completionBody = (row: ReportRow) => {
        if (row.totalAppointments === 0) return <span className="text-400">0%</span>;
        const pct = Math.round((row.completedAppointments / row.totalAppointments) * 100);
        return (
            <div className="flex align-items-center gap-2">
                <div className="flex-1 border-round overflow-hidden" style={{ height: '6px', background: '#e2e8f0' }}>
                    <div style={{ width: `${pct}%`, height: '100%', background: pct >= 70 ? '#22c55e' : pct >= 40 ? '#f59e0b' : '#ef4444', borderRadius: '3px' }} />
                </div>
                <span className="text-sm text-500">{pct}%</span>
            </div>
        );
    };

    const dateBody = (row: ReportRow) => <span className="text-500 text-sm">{fmtDate(row.registeredDate)}</span>;

    // ─────────────────────────────────────────────────────────────────────────
    return (
        <>
            <Toast ref={toast} />
            <div className="grid">
                <div className="col-12">
                    <div className="card p-0 overflow-hidden">

                        {/* Toolbar */}
                        <div className="px-4 pt-4 pb-3">
                            <Toolbar start={leftContent} end={rightContent} style={{ border: 'none', padding: 0, background: 'transparent' }} />
                        </div>

                        {/* Arama */}
                        <div className="px-4 pb-3">
                            <span className="p-input-icon-left w-full md:w-20rem">
                                <i className="pi pi-search" />
                                <InputText
                                    placeholder="Hasta, diyetisyen veya e-posta ara..."
                                    value={filter}
                                    onChange={e => setFilter(e.target.value)}
                                    className="w-full"
                                />
                            </span>
                        </div>

                        {/* Tablo */}
                        {loading ? (
                            <div className="px-4 pb-4 flex flex-column gap-2">
                                {Array.from({ length: 8 }).map((_, i) => (
                                    <Skeleton key={i} width="100%" height="2.75rem" />
                                ))}
                            </div>
                        ) : (
                            <DataTable
                                ref={dtRef}
                                value={rows}
                                globalFilter={filter}
                                paginator
                                rows={15}
                                rowsPerPageOptions={[10, 15, 25, 50]}
                                stripedRows
                                showGridlines
                                dataKey="email"
                                emptyMessage="Kayıt bulunamadı."
                                exportFilename="FitRehber_Rapor"
                                className="p-datatable-sm"
                                style={{ fontSize: '0.875rem' }}
                            >
                                <Column field="patientName"           header="Hasta"           sortable style={{ minWidth: '140px' }} />
                                <Column field="email"                 header="E-posta"         sortable style={{ minWidth: '180px' }} className="hidden md:table-cell" />
                                <Column field="dietitianName"         header="Diyetisyen"      sortable style={{ minWidth: '130px' }} />
                                <Column field="totalAppointments"     header="Top. Randevu"    sortable style={{ width: '110px' }} />
                                <Column header="Tamamlanma %" body={completionBody}            style={{ minWidth: '130px' }} />
                                <Column field="measurementCount"      header="Ölçüm"           sortable style={{ width: '80px' }} />
                                <Column header="Son Kilo"  body={weightBody}                   style={{ width: '100px' }} />
                                <Column header="BMI"       body={bmiBody}                      style={{ width: '80px' }} />
                                <Column header="Kayıt"     body={dateBody}  field="registeredDate" sortable style={{ width: '110px' }} />
                                <Column header="Durum"     body={activeBody}                   style={{ width: '80px' }} />
                            </DataTable>
                        )}
                    </div>
                </div>
            </div>
        </>
    );
};

export default ReportsPage;
