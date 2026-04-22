'use client';
import { Button } from 'primereact/button';
import { InputNumber } from 'primereact/inputnumber';
import { InputTextarea } from 'primereact/inputtextarea';
import { Knob } from 'primereact/knob';
import { Toast } from 'primereact/toast';
import React, { useRef, useState } from 'react';

const PatientTrackingPage = () => {
    const [water, setWater]       = useState<number>(1.4);
    const [steps, setSteps]       = useState<number | null>(null);
    const [weight, setWeight]     = useState<number | null>(null);
    const [note, setNote]         = useState('');
    const [saving, setSaving]     = useState(false);
    const toast = useRef<Toast>(null);

    const WATER_GOAL = 2.5;
    const waterPct   = Math.min(Math.round((water / WATER_GOAL) * 100), 100);

    const handleSave = () => {
        setSaving(true);
        setTimeout(() => {
            setSaving(false);
            toast.current?.show({
                severity: 'success', summary: 'Kaydedildi',
                detail: 'Günlük verileriniz kaydedildi.', life: 3000,
            });
        }, 500);
    };

    return (
        <>
            <Toast ref={toast} position="top-right" />
            <div className="grid">

                {/* ── Su Takibi ─────────────────────────────────────── */}
                <div className="col-12 md:col-6 lg:col-4">
                    <div className="card text-center">
                        <h5>Su Takibi</h5>
                        <Knob
                            value={waterPct}
                            readOnly
                            size={150}
                            valueColor="#00bb7e"
                            rangeColor="#e5e7eb"
                            valueTemplate="{value}%"
                        />
                        <div className="text-500 mb-3">{water.toFixed(1)} / {WATER_GOAL} litre</div>
                        <div className="p-fluid">
                            <label className="block mb-2 text-900 font-medium">Bugün içtiğiniz su (litre)</label>
                            <InputNumber
                                value={water}
                                onValueChange={e => setWater(e.value ?? 0)}
                                min={0} max={5}
                                minFractionDigits={1} maxFractionDigits={1}
                                suffix=" L"
                                className="w-full"
                            />
                        </div>
                        <div className="flex justify-content-center gap-2 mt-3">
                            {[0.25, 0.5, 1].map(v => (
                                <Button
                                    key={v}
                                    label={`+${v}L`}
                                    className="p-button-sm p-button-outlined"
                                    onClick={() => setWater(w => Math.min(+(w + v).toFixed(2), 5))}
                                />
                            ))}
                        </div>
                    </div>
                </div>

                {/* ── Adım & Kilo ───────────────────────────────────── */}
                <div className="col-12 md:col-6 lg:col-4">
                    <div className="card">
                        <h5>Günlük Veriler</h5>
                        <div className="p-fluid">
                            <div className="field">
                                <label className="block mb-2 text-900 font-medium">Günlük Adım Sayısı</label>
                                <InputNumber
                                    value={steps}
                                    onValueChange={e => setSteps(e.value ?? null)}
                                    placeholder="Örn: 8000"
                                    min={0} max={100000}
                                    suffix=" adım"
                                    className="w-full"
                                />
                            </div>
                            <div className="field">
                                <label className="block mb-2 text-900 font-medium">Sabah Kilosu (kg)</label>
                                <InputNumber
                                    value={weight}
                                    onValueChange={e => setWeight(e.value ?? null)}
                                    placeholder="Örn: 75.5"
                                    min={30} max={300}
                                    minFractionDigits={1} maxFractionDigits={1}
                                    suffix=" kg"
                                    className="w-full"
                                />
                            </div>
                        </div>

                        {/* İlerleme çubuğu — günlük adım hedefi 10.000 */}
                        {steps !== null && (
                            <div className="mt-2">
                                <div className="flex justify-content-between mb-1">
                                    <span className="text-500 text-sm">Adım hedefi</span>
                                    <span className="text-500 text-sm">{Math.min(Math.round((steps / 10000) * 100), 100)}%</span>
                                </div>
                                <div className="surface-200 border-round" style={{ height: '8px' }}>
                                    <div
                                        className="bg-green-500 border-round"
                                        style={{ height: '8px', width: `${Math.min((steps / 10000) * 100, 100)}%`, transition: 'width .3s' }}
                                    />
                                </div>
                            </div>
                        )}
                    </div>
                </div>

                {/* ── Notlar ────────────────────────────────────────── */}
                <div className="col-12 lg:col-4">
                    <div className="card" style={{ height: '100%' }}>
                        <h5>Günlük Notlar</h5>
                        <div className="p-fluid">
                            <InputTextarea
                                value={note}
                                onChange={e => setNote(e.target.value)}
                                placeholder="Bugün nasıl hissettiniz? Uyku düzeniniz, enerjiniz, diyet uyumunuz hakkında not bırakın..."
                                rows={8}
                                className="w-full"
                                autoResize
                            />
                        </div>
                        <div className="text-right text-500 text-sm mt-1">{note.length} karakter</div>
                    </div>
                </div>

                {/* ── Kaydet butonu ─────────────────────────────────── */}
                <div className="col-12">
                    <div className="card">
                        <Button
                            label="Günlük Verileri Kaydet"
                            icon="pi pi-check"
                            loading={saving}
                            onClick={handleSave}
                            className="w-full md:w-auto"
                        />
                    </div>
                </div>

            </div>
        </>
    );
};

export default PatientTrackingPage;
