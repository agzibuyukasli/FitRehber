'use client';
import { Button } from 'primereact/button';
import { Column } from 'primereact/column';
import { ConfirmDialog, confirmDialog } from 'primereact/confirmdialog';
import { DataTable } from 'primereact/datatable';
import { Dialog } from 'primereact/dialog';
import { Dropdown } from 'primereact/dropdown';
import { InputNumber } from 'primereact/inputnumber';
import { InputText } from 'primereact/inputtext';
import { Toast } from 'primereact/toast';
import React, { useRef, useState } from 'react';

// ─── Tipler ──────────────────────────────────────────────────────────────────
interface Food {
    id: number;
    name: string;
    category: string;
    protein: number;
    carbs: number;
    fat: number;
    calories: number;
}

interface FoodForm {
    name: string;
    category: string;
    protein: number;
    carbs: number;
    fat: number;
    calories: number;
}

const EMPTY_FORM: FoodForm = {
    name: '', category: '', protein: 0, carbs: 0, fat: 0, calories: 0,
};

const CATEGORIES = [
    { label: 'Et & Protein',   value: 'Et & Protein' },
    { label: 'Tahıllar',       value: 'Tahıllar' },
    { label: 'Meyve',          value: 'Meyve' },
    { label: 'Sebze',          value: 'Sebze' },
    { label: 'Süt Ürünleri',   value: 'Süt Ürünleri' },
    { label: 'Kuruyemiş',      value: 'Kuruyemiş' },
    { label: 'Balık',          value: 'Balık' },
    { label: 'Baklagiller',    value: 'Baklagiller' },
    { label: 'İçecekler',      value: 'İçecekler' },
    { label: 'Diğer',          value: 'Diğer' },
];

// ─── Dummy veriler ────────────────────────────────────────────────────────────
const INITIAL_FOODS: Food[] = [
    { id: 1,  name: 'Tavuk Göğsü',         category: 'Et & Protein',  protein: 31,   carbs: 0,    fat: 3.6,  calories: 165 },
    { id: 2,  name: 'Yumurta',              category: 'Et & Protein',  protein: 13,   carbs: 1.1,  fat: 11,   calories: 155 },
    { id: 3,  name: 'Pirinç (pişmiş)',      category: 'Tahıllar',      protein: 2.7,  carbs: 28,   fat: 0.3,  calories: 130 },
    { id: 4,  name: 'Elma',                 category: 'Meyve',         protein: 0.3,  carbs: 14,   fat: 0.2,  calories: 52  },
    { id: 5,  name: 'Ispanak',              category: 'Sebze',         protein: 2.9,  carbs: 3.6,  fat: 0.4,  calories: 23  },
    { id: 6,  name: 'Tam Buğday Ekmeği',    category: 'Tahıllar',      protein: 9,    carbs: 43,   fat: 3,    calories: 247 },
    { id: 7,  name: 'Süt (%2 yağlı)',       category: 'Süt Ürünleri',  protein: 3.4,  carbs: 5,    fat: 2,    calories: 50  },
    { id: 8,  name: 'Fındık',               category: 'Kuruyemiş',     protein: 15,   carbs: 17,   fat: 61,   calories: 628 },
    { id: 9,  name: 'Somon',                category: 'Balık',         protein: 25,   carbs: 0,    fat: 13,   calories: 208 },
    { id: 10, name: 'Yoğurt (sade)',        category: 'Süt Ürünleri',  protein: 10,   carbs: 3.6,  fat: 0.4,  calories: 59  },
    { id: 11, name: 'Mercimek (pişmiş)',    category: 'Baklagiller',   protein: 9,    carbs: 20,   fat: 0.4,  calories: 116 },
    { id: 12, name: 'Avokado',              category: 'Meyve',         protein: 2,    carbs: 9,    fat: 15,   calories: 160 },
];

// ─── Bileşen ──────────────────────────────────────────────────────────────────
const NutritionPage = () => {
    const [foods, setFoods]                 = useState<Food[]>(INITIAL_FOODS);
    const [dialogVisible, setDialogVisible] = useState(false);
    const [form, setForm]                   = useState<FoodForm>(EMPTY_FORM);
    const [saving, setSaving]               = useState(false);
    const [globalFilter, setGlobalFilter]   = useState('');
    const toast = useRef<Toast>(null);

    // ── Kaydet ────────────────────────────────────────────────────────────
    const handleSave = () => {
        if (!form.name.trim() || !form.category) {
            toast.current?.show({
                severity: 'warn', summary: 'Uyarı',
                detail: 'Besin adı ve kategori zorunludur.', life: 3000,
            });
            return;
        }
        setSaving(true);
        setTimeout(() => {
            setFoods(prev => [
                { id: Date.now(), ...form },
                ...prev,
            ]);
            closeDialog();
            setSaving(false);
            toast.current?.show({
                severity: 'success', summary: 'Eklendi',
                detail: `"${form.name}" besin veritabanına eklendi.`, life: 3000,
            });
        }, 300);
    };

    // ── Sil ───────────────────────────────────────────────────────────────
    const handleDelete = (food: Food) => {
        confirmDialog({
            message:         `"${food.name}" besinini silmek istediğinize emin misiniz?`,
            header:          'Silme Onayı',
            icon:            'pi pi-exclamation-triangle',
            acceptClassName: 'p-button-danger',
            acceptLabel:     'Evet, Sil',
            rejectLabel:     'İptal',
            accept: () => {
                setFoods(prev => prev.filter(f => f.id !== food.id));
                toast.current?.show({
                    severity: 'success', summary: 'Silindi',
                    detail: `${food.name} silindi.`, life: 3000,
                });
            },
        });
    };

    const closeDialog = () => { setDialogVisible(false); setForm(EMPTY_FORM); };

    // ── Sütun şablonları ──────────────────────────────────────────────────
    const numBody = (field: keyof Food, unit: string) => (row: Food) =>
        `${(row[field] as number).toFixed(1)} ${unit}`;

    const actionBody = (row: Food) => (
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

    const tableHeader = (
        <div className="flex justify-content-between align-items-center">
            <span className="p-input-icon-left">
                <i className="pi pi-search" />
                <InputText
                    value={globalFilter}
                    onChange={e => setGlobalFilter(e.target.value)}
                    placeholder="Besin ara..."
                />
            </span>
            <Button
                label="Yeni Besin Ekle"
                icon="pi pi-plus"
                onClick={() => setDialogVisible(true)}
            />
        </div>
    );

    const dialogFooter = (
        <div>
            <Button label="İptal"  icon="pi pi-times" text    onClick={closeDialog} />
            <Button label="Ekle"   icon="pi pi-check" loading={saving} onClick={handleSave} />
        </div>
    );

    return (
        <>
            <Toast ref={toast} position="top-right" />
            <ConfirmDialog />

            <div className="grid">
                <div className="col-12">
                    <div className="card">
                        <h5>Besin Veritabanı Yönetimi</h5>
                        <DataTable
                            value={foods}
                            paginator
                            rows={10}
                            rowsPerPageOptions={[5, 10, 25]}
                            stripedRows
                            dataKey="id"
                            header={tableHeader}
                            globalFilter={globalFilter}
                            emptyMessage="Besin bulunamadı."
                        >
                            <Column field="name"     header="Besin Adı"     sortable filter filterPlaceholder="Ara..." style={{ minWidth: '180px' }} />
                            <Column field="category" header="Kategori"      sortable filter filterPlaceholder="Kategori..." style={{ minWidth: '140px' }} />
                            <Column field="protein"  header="Protein (g)"   body={numBody('protein', 'g')}  sortable style={{ width: '110px' }} />
                            <Column field="carbs"    header="Karbonhidrat (g)" body={numBody('carbs', 'g')} sortable style={{ width: '130px' }} />
                            <Column field="fat"      header="Yağ (g)"       body={numBody('fat', 'g')}      sortable style={{ width: '90px' }} />
                            <Column field="calories" header="Kalori (kcal)" body={numBody('calories', 'kcal')} sortable style={{ width: '120px' }} />
                            <Column header="İşlemler" body={actionBody} style={{ width: '110px' }} />
                        </DataTable>
                    </div>
                </div>
            </div>

            {/* ── Yeni Besin Dialog ──────────────────────────────────────── */}
            <Dialog
                header="Yeni Besin Ekle"
                visible={dialogVisible}
                style={{ width: '480px' }}
                modal
                footer={dialogFooter}
                onHide={closeDialog}
            >
                <div className="p-fluid grid formgrid mt-2">
                    <div className="field col-12">
                        <label htmlFor="foodName">Besin Adı *</label>
                        <InputText
                            id="foodName"
                            value={form.name}
                            onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                            autoFocus
                        />
                    </div>
                    <div className="field col-12">
                        <label htmlFor="category">Kategori *</label>
                        <Dropdown
                            id="category"
                            value={form.category}
                            options={CATEGORIES}
                            onChange={e => setForm(f => ({ ...f, category: e.value }))}
                            placeholder="Kategori seçin"
                        />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label htmlFor="protein">Protein (g)</label>
                        <InputNumber
                            id="protein"
                            value={form.protein}
                            onValueChange={e => setForm(f => ({ ...f, protein: e.value ?? 0 }))}
                            minFractionDigits={1} maxFractionDigits={1} min={0}
                        />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label htmlFor="carbs">Karbonhidrat (g)</label>
                        <InputNumber
                            id="carbs"
                            value={form.carbs}
                            onValueChange={e => setForm(f => ({ ...f, carbs: e.value ?? 0 }))}
                            minFractionDigits={1} maxFractionDigits={1} min={0}
                        />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label htmlFor="fat">Yağ (g)</label>
                        <InputNumber
                            id="fat"
                            value={form.fat}
                            onValueChange={e => setForm(f => ({ ...f, fat: e.value ?? 0 }))}
                            minFractionDigits={1} maxFractionDigits={1} min={0}
                        />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label htmlFor="calories">Kalori (kcal)</label>
                        <InputNumber
                            id="calories"
                            value={form.calories}
                            onValueChange={e => setForm(f => ({ ...f, calories: e.value ?? 0 }))}
                            minFractionDigits={0} maxFractionDigits={0} min={0}
                        />
                    </div>
                </div>
            </Dialog>
        </>
    );
};

export default NutritionPage;
