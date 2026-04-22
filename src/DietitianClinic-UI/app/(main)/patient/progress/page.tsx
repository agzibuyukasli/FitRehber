'use client';
import { Chart }           from 'primereact/chart';
import { ProgressSpinner } from 'primereact/progressspinner';
import { Toast }           from 'primereact/toast';
import React, { useContext, useEffect, useRef, useState } from 'react';
import { LayoutContext }       from '../../../../layout/context/layoutcontext';
import { MeasurementService }  from '../../../../service/MeasurementService';

interface Measurement {
    id:                  number;
    measurementDate:     string;
    weight:              number;
    height:              number;
    bmi:                 number | null;
    bodyFatPercentage:   number | null;
}

const MONTHS = ['Oca','Şub','Mar','Nis','May','Haz','Tem','Ağu','Eyl','Eki','Kas','Ara'];
const fmtLabel = (iso: string) => {
    const d = new Date(iso);
    return `${d.getDate()} ${MONTHS[d.getMonth()]}`;
};

const PatientProgressPage = () => {
    const toast = useRef<Toast>(null);
    const { layoutConfig } = useContext(LayoutContext);
    const isDark = layoutConfig.colorScheme === 'dark';

    const [measurements, setMeasurements] = useState<Measurement[]>([]);
    const [loading,      setLoading]      = useState(true);

    useEffect(() => {
        MeasurementService.getMyProfile()
            .then((profile: { measurements?: Measurement[] } | undefined) => {
                if (!profile) return;
                const sorted = (profile.measurements ?? [])
                    .slice()
                    .sort((a: Measurement, b: Measurement) =>
                        new Date(a.measurementDate).getTime() - new Date(b.measurementDate).getTime());
                setMeasurements(sorted);
            })
            .catch(() => {
                toast.current?.show({ severity: 'warn', summary: 'Bağlantı', detail: 'Ölçüm verileri alınamadı.', life: 4000 });
            })
            .finally(() => setLoading(false));
    }, []);

    if (loading) return (
        <div className="flex justify-content-center align-items-center" style={{ height: '60vh' }}>
            <ProgressSpinner style={{ width: 50, height: 50 }} strokeWidth="4" />
        </div>
    );

    if (measurements.length === 0) return (
        <>
            <Toast ref={toast} position="top-right" />
            <div className="grid">
                <div className="col-12">
                    <div className="card text-center py-6">
                        <i className="pi pi-chart-line text-500 text-6xl mb-3 block" />
                        <h5 className="text-500">Henüz ölçüm verisi bulunmuyor.</h5>
                        <p className="text-500 text-sm">Diyetisyeniniz ölçümlerinizi sisteme girdiğinde burada görünecek.</p>
                    </div>
                </div>
            </div>
        </>
    );

    const labels  = measurements.map(m => fmtLabel(m.measurementDate));
    const first   = measurements[0];
    const last    = measurements[measurements.length - 1];
    const wDiff   = last.weight - first.weight;
    const bmiDiff = (last.bmi ?? 0) - (first.bmi ?? 0);
    const fatDiff = (last.bodyFatPercentage ?? 0) - (first.bodyFatPercentage ?? 0);

    const tickColor = isDark ? '#ebedef' : '#495057';
    const gridColor = isDark ? 'rgba(160,167,181,.3)' : '#ebedef';
    const chartOpts = (unit: string) => ({
        plugins: { legend: { labels: { color: tickColor } } },
        scales: {
            x: { ticks: { color: tickColor }, grid: { color: gridColor } },
            y: { ticks: { color: tickColor, callback: (v: number) => `${v}${unit}` }, grid: { color: gridColor } },
        },
    });

    const weightData = {
        labels,
        datasets: [{
            label: 'Kilo (kg)',
            data: measurements.map(m => m.weight),
            fill: true,
            backgroundColor: 'rgba(201,144,140,0.15)',
            borderColor: '#C9908C',
            tension: 0.4,
            pointRadius: 5,
            pointBackgroundColor: '#C9908C',
        }],
    };
    const bmiData = {
        labels,
        datasets: [{
            label: 'BMI',
            data: measurements.map(m => m.bmi),
            fill: false,
            borderColor: '#2f4860',
            tension: 0.4,
            pointRadius: 5,
            pointBackgroundColor: '#2f4860',
        }],
    };
    const fatData = {
        labels,
        datasets: [{
            label: 'Vücut Yağ Oranı (%)',
            data: measurements.map(m => m.bodyFatPercentage),
            fill: true,
            backgroundColor: 'rgba(249,115,22,0.12)',
            borderColor: '#F97316',
            tension: 0.4,
            pointRadius: 5,
            pointBackgroundColor: '#F97316',
        }],
    };

    return (
        <>
            <Toast ref={toast} position="top-right" />
            <div className="grid">

                {/* ── Özet Kartları ─────────────────────────────────────── */}
                <div className="col-12 md:col-4">
                    <div className="card mb-0 text-center">
                        <div className="text-500 font-medium mb-2">Kilo Değişimi</div>
                        <div className={`text-4xl font-bold ${wDiff <= 0 ? 'text-green-500' : 'text-red-400'}`}>
                            {wDiff <= 0 ? '−' : '+'}{Math.abs(wDiff).toFixed(1)} kg
                        </div>
                        <div className="text-500 mt-1">{first.weight} kg → {last.weight} kg</div>
                    </div>
                </div>
                <div className="col-12 md:col-4">
                    <div className="card mb-0 text-center">
                        <div className="text-500 font-medium mb-2">BMI Değişimi</div>
                        <div className={`text-4xl font-bold ${bmiDiff <= 0 ? 'text-blue-500' : 'text-orange-500'}`}>
                            {bmiDiff <= 0 ? '−' : '+'}{Math.abs(bmiDiff).toFixed(1)}
                        </div>
                        <div className="text-500 mt-1">{first.bmi?.toFixed(1) ?? '—'} → {last.bmi?.toFixed(1) ?? '—'}</div>
                    </div>
                </div>
                <div className="col-12 md:col-4">
                    <div className="card mb-0 text-center">
                        <div className="text-500 font-medium mb-2">Yağ Oranı Değişimi</div>
                        <div className={`text-4xl font-bold ${fatDiff <= 0 ? 'text-orange-500' : 'text-red-400'}`}>
                            {fatDiff <= 0 ? '−' : '+'}{Math.abs(fatDiff).toFixed(1)}%
                        </div>
                        <div className="text-500 mt-1">
                            {first.bodyFatPercentage ? `%${first.bodyFatPercentage.toFixed(1)}` : '—'}
                            {' → '}
                            {last.bodyFatPercentage  ? `%${last.bodyFatPercentage.toFixed(1)}`  : '—'}
                        </div>
                    </div>
                </div>

                {/* ── Grafikler ─────────────────────────────────────────── */}
                <div className="col-12 lg:col-6">
                    <div className="card">
                        <h5>Kilo Takibi</h5>
                        <Chart type="line" data={weightData} options={chartOpts(' kg')} />
                    </div>
                </div>

                <div className="col-12 lg:col-6">
                    <div className="card">
                        <h5>BMI Takibi</h5>
                        <Chart type="line" data={bmiData} options={chartOpts('')} />
                    </div>
                </div>

                {measurements.some(m => m.bodyFatPercentage != null) && (
                    <div className="col-12 lg:col-6">
                        <div className="card">
                            <h5>Vücut Yağ Oranı Takibi</h5>
                            <Chart type="line" data={fatData} options={chartOpts('%')} />
                        </div>
                    </div>
                )}

                {/* ── Ölçüm Tablosu ─────────────────────────────────────── */}
                <div className="col-12 lg:col-6">
                    <div className="card">
                        <h5>Ölçüm Geçmişi</h5>
                        <table className="w-full" style={{ borderCollapse: 'collapse' }}>
                            <thead>
                                <tr className="surface-100">
                                    <th className="p-2 text-left text-600 text-sm">Tarih</th>
                                    <th className="p-2 text-center text-600 text-sm">Kilo</th>
                                    <th className="p-2 text-center text-600 text-sm">BMI</th>
                                    <th className="p-2 text-center text-600 text-sm">Yağ %</th>
                                </tr>
                            </thead>
                            <tbody>
                                {[...measurements].reverse().map((m, i) => (
                                    <tr key={i} className="border-bottom-1 surface-border">
                                        <td className="p-2 text-700 text-sm">{fmtLabel(m.measurementDate)}</td>
                                        <td className="p-2 text-center text-sm">{m.weight} kg</td>
                                        <td className="p-2 text-center text-sm">{m.bmi?.toFixed(1) ?? '—'}</td>
                                        <td className="p-2 text-center text-sm">
                                            {m.bodyFatPercentage ? `%${m.bodyFatPercentage.toFixed(1)}` : '—'}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>

            </div>
        </>
    );
};

export default PatientProgressPage;
