'use client';
import { Button } from 'primereact/button';
import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { Dialog } from 'primereact/dialog';
import { ProgressSpinner } from 'primereact/progressspinner';
import { Tag } from 'primereact/tag';
import { Toast } from 'primereact/toast';
import { useRouter } from 'next/navigation';
import React, { useEffect, useRef, useState } from 'react';
import { PatientService } from '../../../../service/PatientService';

interface Patient {
    id: number;
    fullName: string;
    email: string;
    phone: string;
    city: string;
    age?: number;
    isActive?: boolean;
}

interface Measurement {
    date: string;
    weight: number;
    bmi: number;
    bodyFat: number;
    note: string;
}

// Dummy ölçüm geçmişi — her danışan için aynı şablon
const makeMeasurements = (name: string): Measurement[] => [
    { date: '01 Ocak 2026',  weight: 82.0, bmi: 27.1, bodyFat: 28.4, note: 'İlk ölçüm' },
    { date: '01 Şubat 2026', weight: 80.5, bmi: 26.6, bodyFat: 27.8, note: 'İlerleme iyi' },
    { date: '01 Mart 2026',  weight: 78.2, bmi: 25.8, bodyFat: 26.9, note: `${name} motivasyonu yüksek` },
    { date: '01 Nisan 2026', weight: 75.8, bmi: 25.0, bodyFat: 25.5, note: 'Hedef yaklaşıyor' },
];

const DietitianPatientsPage = () => {
    const router = useRouter();
    const [patients, setPatients]                 = useState<Patient[]>([]);
    const [loading, setLoading]                   = useState(true);
    const [selectedPatient, setSelectedPatient]   = useState<Patient | null>(null);
    const [measurements, setMeasurements]         = useState<Measurement[]>([]);
    const [dialogVisible, setDialogVisible]       = useState(false);
    const [globalFilter, setGlobalFilter]         = useState('');
    const toast = useRef<Toast>(null);

    useEffect(() => {
        PatientService.getAllPatients()
            .then((d: Patient[]) => setPatients(d))
            .finally(() => setLoading(false));
    }, []);

    const openMeasurements = (patient: Patient) => {
        setSelectedPatient(patient);
        setMeasurements(makeMeasurements(patient.fullName));
        setDialogVisible(true);
    };

    const statusBody = (row: Patient) => {
        if (row.isActive === undefined) return null;
        return <Tag value={row.isActive ? 'Aktif' : 'Pasif'} severity={row.isActive ? 'success' : 'warning'} />;
    };

    const actionBody = (row: Patient) => (
        <div className="flex gap-1">
            <Button
                icon="pi pi-folder-open"
                rounded text
                style={{ color: '#2f4860' }}
                tooltip="Detay / Diyet Planı / Ölçümler" tooltipOptions={{ position: 'top' }}
                onClick={() => router.push(`/dietitian/patients/${row.id}`)}
            />
            <Button
                icon="pi pi-chart-line"
                rounded text severity="info"
                tooltip="Hızlı Ölçüm Geçmişi" tooltipOptions={{ position: 'top' }}
                onClick={() => openMeasurements(row)}
            />
        </div>
    );

    const nameBody = (row: Patient) => (
        <span
            className="text-primary font-medium cursor-pointer hover:underline"
            onClick={() => router.push(`/dietitian/patients/${row.id}`)}
        >
            {row.fullName}
        </span>
    );

    const tableHeader = (
        <div className="flex justify-content-between align-items-center">
            <span className="p-input-icon-left">
                <i className="pi pi-search" />
                <input
                    className="p-inputtext p-component"
                    value={globalFilter}
                    onChange={e => setGlobalFilter(e.target.value)}
                    placeholder="Danışan ara..."
                    style={{ paddingLeft: '2rem' }}
                />
            </span>
        </div>
    );

    return (
        <>
            <Toast ref={toast} />
            <div className="grid">
                <div className="col-12">
                    <div className="card">
                        <h5>Danışanlarım</h5>
                        {loading ? (
                            <div className="flex justify-content-center" style={{ height: '200px', alignItems: 'center' }}>
                                <ProgressSpinner style={{ width: '50px', height: '50px' }} strokeWidth="4" />
                            </div>
                        ) : (
                            <DataTable
                                value={patients}
                                paginator rows={10}
                                stripedRows dataKey="id"
                                header={tableHeader}
                                globalFilter={globalFilter}
                                emptyMessage="Danışan bulunamadı."
                            >
                                <Column field="fullName" header="Ad Soyad"  body={nameBody} sortable filter filterPlaceholder="Ara..." />
                                <Column field="email"    header="E-posta"   sortable />
                                <Column field="phone"    header="Telefon" />
                                <Column field="city"     header="Şehir" />
                                <Column field="age"      header="Yaş"       style={{ width: '70px' }} />
                                <Column header="Durum"   body={statusBody}  style={{ width: '90px' }} />
                                <Column header="İşlemler" body={actionBody} style={{ width: '110px' }} />
                            </DataTable>
                        )}
                    </div>
                </div>
            </div>

            {/* ── Ölçüm Geçmişi Dialog ──────────────────────────────── */}
            <Dialog
                header={`Ölçüm Geçmişi — ${selectedPatient?.fullName ?? ''}`}
                visible={dialogVisible}
                style={{ width: '640px' }}
                modal
                onHide={() => setDialogVisible(false)}
                footer={<Button label="Kapat" icon="pi pi-times" text onClick={() => setDialogVisible(false)} />}
            >
                <DataTable value={measurements} stripedRows dataKey="date">
                    <Column field="date"    header="Tarih" />
                    <Column field="weight"  header="Kilo (kg)"   body={(r: Measurement) => `${r.weight} kg`} />
                    <Column field="bmi"     header="BMI"         body={(r: Measurement) => r.bmi.toFixed(1)} />
                    <Column field="bodyFat" header="Yağ (%)"     body={(r: Measurement) => `%${r.bodyFat}`} />
                    <Column field="note"    header="Not" />
                </DataTable>
            </Dialog>
        </>
    );
};

export default DietitianPatientsPage;
