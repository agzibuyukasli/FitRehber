'use client';
import { Button } from 'primereact/button';
import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { InputNumber } from 'primereact/inputnumber';
import { InputText } from 'primereact/inputtext';
import { Tag } from 'primereact/tag';
import { Toast } from 'primereact/toast';
import React, { useRef, useState } from 'react';

interface Food {
    id: number;
    name: string;
    category: string;
    protein: number; // per 100g
    carbs: number;
    fat: number;
    calories: number;
}

interface PlanItem extends Food {
    planId: number;
    amount: number; // gram
}

const FOOD_DB: Food[] = [
    { id: 1,  name: 'Tavuk Göğsü',       category: 'Et & Protein',  protein: 31,  carbs: 0,   fat: 3.6,  calories: 165 },
    { id: 2,  name: 'Yumurta',            category: 'Et & Protein',  protein: 13,  carbs: 1.1, fat: 11,   calories: 155 },
    { id: 3,  name: 'Pirinç (pişmiş)',    category: 'Tahıllar',      protein: 2.7, carbs: 28,  fat: 0.3,  calories: 130 },
    { id: 4,  name: 'Elma',               category: 'Meyve',         protein: 0.3, carbs: 14,  fat: 0.2,  calories: 52  },
    { id: 5,  name: 'Ispanak',            category: 'Sebze',         protein: 2.9, carbs: 3.6, fat: 0.4,  calories: 23  },
    { id: 6,  name: 'Tam Buğday Ekmeği', category: 'Tahıllar',      protein: 9,   carbs: 43,  fat: 3,    calories: 247 },
    { id: 7,  name: 'Süt (%2 yağlı)',    category: 'Süt Ürünleri',  protein: 3.4, carbs: 5,   fat: 2,    calories: 50  },
    { id: 8,  name: 'Fındık',            category: 'Kuruyemiş',     protein: 15,  carbs: 17,  fat: 61,   calories: 628 },
    { id: 9,  name: 'Somon',             category: 'Balık',         protein: 25,  carbs: 0,   fat: 13,   calories: 208 },
    { id: 10, name: 'Yoğurt (sade)',     category: 'Süt Ürünleri',  protein: 10,  carbs: 3.6, fat: 0.4,  calories: 59  },
    { id: 11, name: 'Mercimek (pişmiş)', category: 'Baklagiller',   protein: 9,   carbs: 20,  fat: 0.4,  calories: 116 },
    { id: 12, name: 'Avokado',           category: 'Meyve',         protein: 2,   carbs: 9,   fat: 15,   calories: 160 },
];

const macros = (item: PlanItem) => {
    const r = item.amount / 100;
    return {
        protein:  +(item.protein  * r).toFixed(1),
        carbs:    +(item.carbs    * r).toFixed(1),
        fat:      +(item.fat      * r).toFixed(1),
        calories: +(item.calories * r).toFixed(0),
    };
};

const MealPlansPage = () => {
    const [filter, setFilter] = useState('');
    const [plan, setPlan]     = useState<PlanItem[]>([]);
    const toast               = useRef<Toast>(null);

    const filtered = FOOD_DB.filter(f =>
        f.name.toLowerCase().includes(filter.toLowerCase()) ||
        f.category.toLowerCase().includes(filter.toLowerCase())
    );

    const addFood = (food: Food) => {
        if (plan.find(p => p.id === food.id)) {
            toast.current?.show({ severity: 'warn', summary: 'Zaten Eklendi', detail: `${food.name} listede mevcut.`, life: 2000 });
            return;
        }
        setPlan(prev => [...prev, { ...food, planId: Date.now(), amount: 100 }]);
    };

    const removeItem = (planId: number) =>
        setPlan(prev => prev.filter(p => p.planId !== planId));

    const updateAmount = (planId: number, value: number) =>
        setPlan(prev => prev.map(p => p.planId === planId ? { ...p, amount: value } : p));

    const totals = plan.reduce((acc, item) => {
        const m = macros(item);
        return { protein: acc.protein + m.protein, carbs: acc.carbs + m.carbs, fat: acc.fat + m.fat, calories: acc.calories + m.calories };
    }, { protein: 0, carbs: 0, fat: 0, calories: 0 });

    const addBody = (row: Food) => (
        <Button icon="pi pi-plus" rounded text severity="success"
            tooltip="Plana Ekle" tooltipOptions={{ position: 'top' }}
            onClick={() => addFood(row)} />
    );

    const planActionBody = (row: PlanItem) => (
        <Button icon="pi pi-trash" rounded text severity="danger" onClick={() => removeItem(row.planId)} />
    );

    const amountBody = (row: PlanItem) => (
        <InputNumber
            value={row.amount}
            onValueChange={e => updateAmount(row.planId, e.value ?? 100)}
            min={1} max={2000} suffix=" g"
            inputClassName="w-5rem text-center"
        />
    );

    return (
        <>
            <Toast ref={toast} />
            <div className="grid">

                {/* ── Sol: Besin Veritabanı ──────────────────────────── */}
                <div className="col-12 xl:col-5">
                    <div className="card" style={{ height: '100%' }}>
                        <h5>Besin Veritabanı</h5>
                        <span className="p-input-icon-left w-full mb-3" style={{ display: 'block' }}>
                            <i className="pi pi-search" />
                            <InputText
                                value={filter}
                                onChange={e => setFilter(e.target.value)}
                                placeholder="Besin veya kategori ara..."
                                className="w-full"
                                style={{ paddingLeft: '2rem' }}
                            />
                        </span>
                        <DataTable value={filtered} scrollable scrollHeight="420px" dataKey="id" stripedRows emptyMessage="Besin bulunamadı.">
                            <Column field="name"     header="Besin"     sortable style={{ minWidth: '140px' }} />
                            <Column field="category" header="Kategori"  style={{ minWidth: '120px' }} />
                            <Column field="calories" header="kcal/100g" style={{ width: '90px' }} />
                            <Column header=""        body={addBody}     style={{ width: '60px' }} />
                        </DataTable>
                    </div>
                </div>

                {/* ── Sağ: Hazırlanan Plan ───────────────────────────── */}
                <div className="col-12 xl:col-7">
                    <div className="card">
                        <div className="flex justify-content-between align-items-center mb-3">
                            <h5 className="m-0">Hazırlanan Diyet Planı</h5>
                            <Button
                                label="Planı Kaydet"
                                icon="pi pi-check"
                                disabled={plan.length === 0}
                                onClick={() => toast.current?.show({ severity: 'success', summary: 'Kaydedildi', detail: 'Plan kaydedildi (yakında API bağlanacak).', life: 3000 })}
                            />
                        </div>

                        {plan.length === 0 ? (
                            <div className="text-center text-500 py-5">
                                <i className="pi pi-arrow-left text-3xl mb-3 block" />
                                Sol taraftan besin ekleyerek plan oluşturun.
                            </div>
                        ) : (
                            <>
                                <DataTable value={plan} dataKey="planId" stripedRows>
                                    <Column field="name"     header="Besin"     sortable />
                                    <Column header="Miktar"  body={amountBody}  style={{ width: '130px' }} />
                                    <Column header="Protein" body={(r:PlanItem) => `${macros(r).protein}g`}  style={{ width: '80px' }} />
                                    <Column header="Karb"    body={(r:PlanItem) => `${macros(r).carbs}g`}    style={{ width: '75px' }} />
                                    <Column header="Yağ"     body={(r:PlanItem) => `${macros(r).fat}g`}      style={{ width: '70px' }} />
                                    <Column header="kcal"    body={(r:PlanItem) => macros(r).calories}       style={{ width: '70px' }} />
                                    <Column header=""        body={planActionBody} style={{ width: '55px' }} />
                                </DataTable>

                                {/* Toplam satırı */}
                                <div className="flex gap-3 mt-3 flex-wrap">
                                    {[
                                        { label: 'Protein', value: `${totals.protein.toFixed(1)} g`, color: 'info' },
                                        { label: 'Karbonhidrat', value: `${totals.carbs.toFixed(1)} g`, color: 'warning' },
                                        { label: 'Yağ', value: `${totals.fat.toFixed(1)} g`, color: 'danger' },
                                        { label: 'Kalori', value: `${totals.calories.toFixed(0)} kcal`, color: 'success' },
                                    ].map(item => (
                                        <Tag key={item.label} severity={item.color as 'info'|'warning'|'danger'|'success'}
                                            value={`${item.label}: ${item.value}`} className="text-base px-3 py-2" />
                                    ))}
                                </div>
                            </>
                        )}
                    </div>
                </div>

            </div>
        </>
    );
};

export default MealPlansPage;
