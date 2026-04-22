'use client';
import { Button } from 'primereact/button';
import { Divider } from 'primereact/divider';
import { InputText } from 'primereact/inputtext';
import { Toast } from 'primereact/toast';
import React, { useRef, useState } from 'react';

const SettingsPage = () => {
    const [clinicName, setClinicName]   = useState('FitRehber Klinik');
    const [email, setEmail]             = useState('admin@fitrehber.com');
    const [phone, setPhone]             = useState('0555 000 00 01');
    const [address, setAddress]         = useState('');
    const toast = useRef<Toast>(null);

    const handleSave = () => {
        toast.current?.show({
            severity: 'success', summary: 'Kaydedildi',
            detail: 'Ayarlar başarıyla güncellendi.', life: 3000,
        });
    };

    return (
        <>
            <Toast ref={toast} position="top-right" />
            <div className="grid">
                <div className="col-12 lg:col-8">
                    <div className="card">
                        <h5>Klinik Bilgileri</h5>
                        <div className="p-fluid grid formgrid">
                            <div className="field col-12">
                                <label htmlFor="clinicName">Klinik Adı</label>
                                <InputText id="clinicName" value={clinicName} onChange={e => setClinicName(e.target.value)} />
                            </div>
                            <div className="field col-12 md:col-6">
                                <label htmlFor="settEmail">E-posta</label>
                                <InputText id="settEmail" type="email" value={email} onChange={e => setEmail(e.target.value)} />
                            </div>
                            <div className="field col-12 md:col-6">
                                <label htmlFor="settPhone">Telefon</label>
                                <InputText id="settPhone" value={phone} onChange={e => setPhone(e.target.value)} />
                            </div>
                            <div className="field col-12">
                                <label htmlFor="address">Adres</label>
                                <InputText id="address" value={address} onChange={e => setAddress(e.target.value)} placeholder="Klinik adresi..." />
                            </div>
                        </div>

                        <Divider />

                        <div className="flex justify-content-end">
                            <Button label="Değişiklikleri Kaydet" icon="pi pi-check" onClick={handleSave} />
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
};

export default SettingsPage;
