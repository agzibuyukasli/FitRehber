'use client';
import { Button } from 'primereact/button';
import { Column } from 'primereact/column';
import { ConfirmDialog, confirmDialog } from 'primereact/confirmdialog';
import { DataTable } from 'primereact/datatable';
import { Dialog } from 'primereact/dialog';
import { InputText } from 'primereact/inputtext';
import { ProgressSpinner } from 'primereact/progressspinner';
import { Tag } from 'primereact/tag';
import { Toast } from 'primereact/toast';
import React, { useEffect, useRef, useState } from 'react';
import { PatientService } from '../../../service/PatientService';

// ─── Tipler ──────────────────────────────────────────────────────────────────
interface Patient {
    id: number;
    fullName: string;
    email: string;
    phone: string;
    city: string;
    age?: number;
    isActive?: boolean;
}

interface PatientForm {
    firstName: string;
    lastName: string;
    tcNo: string;
    email: string;
    phone: string;
    birthDate: string;
    city: string;
}

const EMPTY_FORM: PatientForm = {
    firstName: '', lastName: '', tcNo: '',
    email: '', phone: '', birthDate: '', city: '',
};

// ─── Bileşen ──────────────────────────────────────────────────────────────────
const PatientsPage = () => {
    const [patients, setPatients]           = useState<Patient[]>([]);
    const [loading, setLoading]             = useState(true);
    const [dialogVisible, setDialogVisible] = useState(false);
    const [form, setForm]                   = useState<PatientForm>(EMPTY_FORM);
    const [saving, setSaving]               = useState(false);
    const [globalFilter, setGlobalFilter]   = useState('');
    const toast = useRef<Toast>(null);

    useEffect(() => {
        PatientService.getAllPatients()
            .then((data: Patient[]) => setPatients(data))
            .finally(() => setLoading(false));
    }, []);

    // ── Yeni hasta kaydet (local state — TODO: POST /api/Patients) ─────────
    const handleSave = () => {
        if (!form.firstName.trim() || !form.lastName.trim()) {
            toast.current?.show({
                severity: 'warn', summary: 'Uyarı',
                detail: 'Ad ve Soyad alanları zorunludur.', life: 3000,
            });
            return;
        }
        setSaving(true);
        setTimeout(() => {
            const newPatient: Patient = {
                id:       Date.now(),
                fullName: `${form.firstName.trim()} ${form.lastName.trim()}`,
                email:    form.email,
                phone:    form.phone,
                city:     form.city,
                isActive: true,
            };
            setPatients(prev => [newPatient, ...prev]);
            closeDialog();
            setSaving(false);
            toast.current?.show({
                severity: 'success', summary: 'Başarılı',
                detail: 'Hasta başarıyla kaydedildi.', life: 3000,
            });
        }, 400);
    };

    // ── Sil ───────────────────────────────────────────────────────────────
    const handleDelete = (patient: Patient) => {
        confirmDialog({
            message:         `"${patient.fullName}" hastasını silmek istediğinize emin misiniz?`,
            header:          'Silme Onayı',
            icon:            'pi pi-exclamation-triangle',
            acceptClassName: 'p-button-danger',
            acceptLabel:     'Evet, Sil',
            rejectLabel:     'İptal',
            accept: () => {
                // TODO: DELETE /api/Patients/{id}
                setPatients(prev => prev.filter(p => p.id !== patient.id));
                toast.current?.show({
                    severity: 'success', summary: 'Silindi',
                    detail: `${patient.fullName} silindi.`, life: 3000,
                });
            },
        });
    };

    const closeDialog = () => { setDialogVisible(false); setForm(EMPTY_FORM); };
    const setField = (key: keyof PatientForm) => (e: React.ChangeEvent<HTMLInputElement>) =>
        setForm(f => ({ ...f, [key]: e.target.value }));

    // ── Sütun şablonları ──────────────────────────────────────────────────
    const statusBody = (row: Patient) => {
        if (row.isActive === undefined) return null;
        return <Tag value={row.isActive ? 'Aktif' : 'Pasif'} severity={row.isActive ? 'success' : 'warning'} />;
    };

    const actionBody = (row: Patient) => (
        <div className="flex gap-2">
            <Button
                icon="pi pi-pencil"
                rounded text severity="info"
                tooltip="Düzenle" tooltipOptions={{ position: 'top' }}
                onClick={() => toast.current?.show({
                    severity: 'info', summary: 'Yakında',
                    detail: 'Düzenleme özelliği yakında eklenecek.', life: 2000,
                })}
            />
            <Button
                icon="pi pi-trash"
                rounded text severity="danger"
                tooltip="Sil" tooltipOptions={{ position: 'top' }}
                onClick={() => handleDelete(row)}
            />
        </div>
    );

    // ── Dialog footer ─────────────────────────────────────────────────────
    const dialogFooter = (
        <div>
            <Button label="İptal"  icon="pi pi-times" text    onClick={closeDialog} />
            <Button label="Kaydet" icon="pi pi-check" loading={saving} onClick={handleSave} />
        </div>
    );

    // ── Header arama ─────────────────────────────────────────────────────
    const tableHeader = (
        <div className="flex justify-content-between align-items-center">
            <span className="p-input-icon-left">
                <i className="pi pi-search" />
                <InputText
                    value={globalFilter}
                    onChange={e => setGlobalFilter(e.target.value)}
                    placeholder="Hasta ara..."
                />
            </span>
            <Button
                label="Yeni Hasta Ekle"
                icon="pi pi-plus"
                onClick={() => setDialogVisible(true)}
            />
        </div>
    );

    return (
        <>
            <Toast ref={toast} position="top-right" />
            <ConfirmDialog />

            <div className="grid">
                <div className="col-12">
                    <div className="card">
                        <h5>Hasta Yönetimi</h5>

                        {loading ? (
                            <div className="flex justify-content-center align-items-center" style={{ height: '220px' }}>
                                <ProgressSpinner style={{ width: '50px', height: '50px' }} strokeWidth="4" />
                            </div>
                        ) : (
                            <DataTable
                                value={patients}
                                paginator
                                rows={10}
                                rowsPerPageOptions={[5, 10, 25]}
                                stripedRows
                                dataKey="id"
                                header={tableHeader}
                                globalFilter={globalFilter}
                                emptyMessage="Kayıtlı hasta bulunamadı."
                            >
                                <Column field="fullName" header="Ad Soyad"  sortable filter filterPlaceholder="Ara..." />
                                <Column field="email"    header="E-posta"   sortable />
                                <Column field="phone"    header="Telefon" />
                                <Column field="city"     header="Şehir"     sortable filter filterPlaceholder="Şehir..." />
                                <Column field="age"      header="Yaş"       style={{ width: '70px' }} />
                                <Column header="Durum"    body={statusBody}  style={{ width: '90px' }} />
                                <Column header="İşlemler" body={actionBody}  style={{ width: '110px' }} />
                            </DataTable>
                        )}
                    </div>
                </div>
            </div>

            {/* ── Yeni Hasta Dialog ──────────────────────────────────────── */}
            <Dialog
                header="Yeni Hasta Kaydı"
                visible={dialogVisible}
                style={{ width: '520px' }}
                modal
                footer={dialogFooter}
                onHide={closeDialog}
            >
                <div className="p-fluid grid formgrid mt-2">
                    <div className="field col-12 md:col-6">
                        <label htmlFor="firstName">Ad *</label>
                        <InputText id="firstName" value={form.firstName} onChange={setField('firstName')} autoFocus />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label htmlFor="lastName">Soyad *</label>
                        <InputText id="lastName" value={form.lastName} onChange={setField('lastName')} />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label htmlFor="tcNo">TC Kimlik No</label>
                        <InputText id="tcNo" value={form.tcNo} onChange={setField('tcNo')} maxLength={11} />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label htmlFor="birthDate">Doğum Tarihi</label>
                        <InputText id="birthDate" type="date" value={form.birthDate} onChange={setField('birthDate')} />
                    </div>
                    <div className="field col-12">
                        <label htmlFor="patEmail">E-posta</label>
                        <InputText id="patEmail" type="email" value={form.email} onChange={setField('email')} />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label htmlFor="phone">Telefon</label>
                        <InputText id="phone" value={form.phone} onChange={setField('phone')} />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label htmlFor="city">Şehir</label>
                        <InputText id="city" value={form.city} onChange={setField('city')} />
                    </div>
                </div>
            </Dialog>
        </>
    );
};

export default PatientsPage;
