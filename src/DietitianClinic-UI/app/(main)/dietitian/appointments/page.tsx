'use client';
import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { Tag } from 'primereact/tag';
import React, { useState } from 'react';
import { Button } from 'primereact/button';
import { Toast } from 'primereact/toast';
import { useRef } from 'react';

type Status = 'Planlandı' | 'Tamamlandı' | 'Bekliyor' | 'İptal';

interface Appointment {
    id: number;
    patientName: string;
    date: string;
    time: string;
    duration: string;
    type: string;
    status: Status;
}

const DUMMY: Appointment[] = [
    { id: 1, patientName: 'Aleyna Coruk',    date: '15 Nisan 2026', time: '10:00', duration: '45 dk', type: 'İlk Görüşme',      status: 'Tamamlandı' },
    { id: 2, patientName: 'Aslı Ağzıbüyük', date: '15 Nisan 2026', time: '11:30', duration: '60 dk', type: 'Takip Seansı',     status: 'Tamamlandı' },
    { id: 3, patientName: 'Sudem Demircan',  date: '16 Nisan 2026', time: '14:00', duration: '45 dk', type: 'Ölçüm Seansı',    status: 'Planlandı'  },
    { id: 4, patientName: 'Mehmet Yılmaz',   date: '17 Nisan 2026', time: '09:00', duration: '30 dk', type: 'Takip Seansı',    status: 'Bekliyor'   },
    { id: 5, patientName: 'Zeynep Kaya',     date: '18 Nisan 2026', time: '15:30', duration: '45 dk', type: 'İlk Görüşme',     status: 'İptal'      },
    { id: 6, patientName: 'Aleyna Coruk',    date: '22 Nisan 2026', time: '10:00', duration: '45 dk', type: 'Plan Güncellemesi',status: 'Planlandı'  },
    { id: 7, patientName: 'Sudem Demircan',  date: '23 Nisan 2026', time: '14:00', duration: '45 dk', type: 'Takip Seansı',    status: 'Planlandı'  },
];

const SEV: Record<Status,'success'|'info'|'warning'|'danger'> = {
    Planlandı: 'info', Tamamlandı: 'success', Bekliyor: 'warning', İptal: 'danger',
};

const DietitianAppointmentsPage = () => {
    const [appointments] = useState<Appointment[]>(DUMMY);
    const toast = useRef<Toast>(null);

    const statusBody  = (r: Appointment) => <Tag value={r.status} severity={SEV[r.status]} />;
    const actionBody  = (r: Appointment) => (
        <Button icon="pi pi-eye" rounded text severity="info"
            tooltip="Detay" tooltipOptions={{ position: 'top' }}
            onClick={() => toast.current?.show({ severity: 'info', summary: r.patientName, detail: `${r.date} ${r.time} — ${r.type}`, life: 3000 })} />
    );

    const upcoming = appointments.filter(a => a.status === 'Planlandı' || a.status === 'Bekliyor');
    const past     = appointments.filter(a => a.status === 'Tamamlandı' || a.status === 'İptal');

    return (
        <>
            <Toast ref={toast} />
            <div className="grid">

                <div className="col-12 lg:col-6">
                    <div className="card">
                        <h5>Yaklaşan Randevular</h5>
                        <DataTable value={upcoming} stripedRows dataKey="id" emptyMessage="Yaklaşan randevu yok.">
                            <Column field="patientName" header="Danışan"  sortable />
                            <Column field="date"        header="Tarih" />
                            <Column field="time"        header="Saat"  style={{ width: '70px' }} />
                            <Column field="type"        header="Tür" />
                            <Column header="Durum"  body={statusBody}  style={{ width: '100px' }} />
                            <Column header=""       body={actionBody}  style={{ width: '60px' }} />
                        </DataTable>
                    </div>
                </div>

                <div className="col-12 lg:col-6">
                    <div className="card">
                        <h5>Geçmiş Randevular</h5>
                        <DataTable value={past} stripedRows dataKey="id" emptyMessage="Geçmiş randevu yok.">
                            <Column field="patientName" header="Danışan"  sortable />
                            <Column field="date"        header="Tarih" />
                            <Column field="time"        header="Saat"  style={{ width: '70px' }} />
                            <Column field="type"        header="Tür" />
                            <Column header="Durum"  body={statusBody}  style={{ width: '100px' }} />
                        </DataTable>
                    </div>
                </div>

            </div>
        </>
    );
};

export default DietitianAppointmentsPage;
