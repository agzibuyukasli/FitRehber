'use client';
import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { Tag } from 'primereact/tag';
import React from 'react';

type ApptStatus = 'Planlandı' | 'Tamamlandı' | 'İptal' | 'Bekliyor';

interface Appointment {
    id: number;
    patientName: string;
    date: string;
    time: string;
    duration: string;
    status: ApptStatus;
}

const DUMMY: Appointment[] = [
    { id: 1, patientName: 'Aleyna Coruk',    date: '15 Nisan 2026', time: '10:00', duration: '45 dk', status: 'Planlandı'  },
    { id: 2, patientName: 'Aslı Ağzıbüyük', date: '15 Nisan 2026', time: '11:30', duration: '60 dk', status: 'Tamamlandı' },
    { id: 3, patientName: 'Sudem Demircan',  date: '16 Nisan 2026', time: '14:00', duration: '45 dk', status: 'Planlandı'  },
    { id: 4, patientName: 'Mehmet Yılmaz',   date: '17 Nisan 2026', time: '09:00', duration: '30 dk', status: 'Bekliyor'   },
    { id: 5, patientName: 'Zeynep Kaya',     date: '18 Nisan 2026', time: '15:30', duration: '45 dk', status: 'İptal'      },
];

const SEVERITY: Record<ApptStatus, 'success' | 'info' | 'danger' | 'warning'> = {
    Planlandı: 'info', Tamamlandı: 'success', İptal: 'danger', Bekliyor: 'warning',
};

const AppointmentsPage = () => (
    <div className="grid">
        <div className="col-12">
            <div className="card">
                <h5>Randevu Takvimi</h5>
                <DataTable value={DUMMY} stripedRows paginator rows={10} dataKey="id" emptyMessage="Randevu bulunamadı.">
                    <Column field="patientName" header="Hasta Adı"  sortable />
                    <Column field="date"        header="Tarih" />
                    <Column field="time"        header="Saat"       style={{ width: '80px' }} />
                    <Column field="duration"    header="Süre"       style={{ width: '90px' }} />
                    <Column
                        field="status" header="Durum"
                        body={(row: Appointment) => <Tag value={row.status} severity={SEVERITY[row.status]} />}
                        style={{ width: '110px' }}
                    />
                </DataTable>
            </div>
        </div>
    </div>
);

export default AppointmentsPage;
