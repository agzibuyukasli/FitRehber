'use client';
import { Button }         from 'primereact/button';
import { Chart }          from 'primereact/chart';
import { Column }         from 'primereact/column';
import { ConfirmDialog, confirmDialog } from 'primereact/confirmdialog';
import { DataTable }      from 'primereact/datatable';
import { Dialog }         from 'primereact/dialog';
import { Divider }        from 'primereact/divider';
import { Dropdown }       from 'primereact/dropdown';
import { InputNumber }    from 'primereact/inputnumber';
import { InputText }      from 'primereact/inputtext';
import { InputTextarea }  from 'primereact/inputtextarea';
import { ProgressSpinner } from 'primereact/progressspinner';
import { TabPanel, TabView } from 'primereact/tabview';
import { Tag }            from 'primereact/tag';
import { Toast }          from 'primereact/toast';
import Link               from 'next/link';
import React, { useCallback, useContext, useEffect, useRef, useState } from 'react';
import { MeasurementService } from '../../../../../service/MeasurementService';
import { DietService }        from '../../../../../service/DietService';
import { PatientService }     from '../../../../../service/PatientService';
import { LayoutContext }      from '../../../../../layout/context/layoutcontext';
import { AuthService }        from '../../../../../service/AuthService';
import { exportDietPlanPDF, PdfMealSlot } from '../../../../../utils/pdfExport';

// ─── Besin Veritabanı (yerel kopya) ──────────────────────────────────────────
interface FoodDB { id: number; name: string; category: string; protein: number; carbs: number; fat: number; calories: number; }
const FOOD_DB: FoodDB[] = [
    { id:1,  name:'Tavuk Göğsü',       category:'Et & Protein',  protein:31,  carbs:0,   fat:3.6, calories:165 },
    { id:2,  name:'Yumurta',           category:'Et & Protein',  protein:13,  carbs:1.1, fat:11,  calories:155 },
    { id:3,  name:'Pirinç (pişmiş)',   category:'Tahıllar',      protein:2.7, carbs:28,  fat:0.3, calories:130 },
    { id:4,  name:'Elma',              category:'Meyve',         protein:0.3, carbs:14,  fat:0.2, calories:52  },
    { id:5,  name:'Ispanak',           category:'Sebze',         protein:2.9, carbs:3.6, fat:0.4, calories:23  },
    { id:6,  name:'Tam Buğday Ekmeği', category:'Tahıllar',      protein:9,   carbs:43,  fat:3,   calories:247 },
    { id:7,  name:'Süt (%2 yağlı)',    category:'Süt Ürünleri',  protein:3.4, carbs:5,   fat:2,   calories:50  },
    { id:8,  name:'Fındık',            category:'Kuruyemiş',     protein:15,  carbs:17,  fat:61,  calories:628 },
    { id:9,  name:'Somon',             category:'Balık',         protein:25,  carbs:0,   fat:13,  calories:208 },
    { id:10, name:'Yoğurt (sade)',     category:'Süt Ürünleri',  protein:10,  carbs:3.6, fat:0.4, calories:59  },
    { id:11, name:'Mercimek (pişmiş)', category:'Baklagiller',   protein:9,   carbs:20,  fat:0.4, calories:116 },
    { id:12, name:'Avokado',           category:'Meyve',         protein:2,   carbs:9,   fat:15,  calories:160 },
];

// ─── Tipler ───────────────────────────────────────────────────────────────────
interface Measurement {
    id:                  number;
    patientId:           number;
    measurementDate:     string;
    weight:              number;
    height:              number;
    bmi:                 number | null;
    waistCircumference:  number | null;
    hipCircumference:    number | null;
    bodyFatPercentage:   number | null;
    notes:               string;
}

interface MeasureForm {
    measurementDate:    string;
    weight:             number;
    height:             number;
    waistCircumference: number | null;
    hipCircumference:   number | null;
    bodyFatPercentage:  number | null;
    notes:              string;
}

interface MealItem { foodId: number; name: string; grams: number; cal: number; p: number; c: number; f: number; }

interface MealSlot {
    key:   'breakfast' | 'lunch' | 'snack' | 'dinner';
    label: string;
    icon:  string;
    color: string;
    bg:    string;
    items: MealItem[];
}

interface MealPlan {
    id:             number;
    title:          string;
    patientId:      number;
    startDate:      string;
    endDate:        string | null;
    targetCalories: number;
    targetProtein:  number;
    targetCarbs:    number;
    targetFat:      number;
    restrictions:   string;
    status:         number;
    meals:          { breakfast: string | null; lunch: string | null; snack: string | null; dinner: string | null; } | null;
}

interface PatientInfo { id: number; fullName: string; email: string; phone: string; city: string; age?: number; }

const EMPTY_MEASURE: MeasureForm = {
    measurementDate: new Date().toISOString().slice(0, 10),
    weight: 0, height: 0,
    waistCircumference: null, hipCircumference: null, bodyFatPercentage: null,
    notes: '',
};

const INIT_MEALS = (): MealSlot[] => [
    { key: 'breakfast', label: 'Sabah Kahvaltısı', icon: 'pi-sun',         color: '#F97316', bg: '#FFF7ED', items: [] },
    { key: 'snack',     label: 'Ara Öğün',         icon: 'pi-apple',       color: '#22C55E', bg: '#F0FDF4', items: [] },
    { key: 'lunch',     label: 'Öğle Yemeği',      icon: 'pi-circle-fill', color: '#3B82F6', bg: '#EFF6FF', items: [] },
    { key: 'dinner',    label: 'Akşam Yemeği',     icon: 'pi-moon',        color: '#8B5CF6', bg: '#F5F3FF', items: [] },
];

const PLAN_STATUS: Record<number, string> = { 0: 'Taslak', 1: 'Aktif', 2: 'Tamamlandı', 3: 'İptal' };
const PLAN_SEV:    Record<number, 'warning' | 'success' | 'info' | 'danger'> = { 0: 'warning', 1: 'success', 2: 'info', 3: 'danger' };

const fmtDate = (iso: string) => new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' });

const calcItemMacros = (food: FoodDB, grams: number): MealItem => ({
    foodId: food.id,
    name:   food.name,
    grams,
    cal: Math.round((food.calories / 100) * grams * 10) / 10,
    p:   Math.round((food.protein  / 100) * grams * 10) / 10,
    c:   Math.round((food.carbs    / 100) * grams * 10) / 10,
    f:   Math.round((food.fat      / 100) * grams * 10) / 10,
});

const serializeMeal = (items: MealItem[]): string =>
    items.length === 0 ? '' : JSON.stringify(items);

// ─── Bileşen ──────────────────────────────────────────────────────────────────
const PatientDetailPage = ({ params }: { params: { id: string } }) => {
    const patientId = parseInt(params.id, 10);
    const toast     = useRef<Toast>(null);
    const { layoutConfig } = useContext(LayoutContext);
    const isDark = layoutConfig.colorScheme === 'dark';

    const [patient,      setPatient]      = useState<PatientInfo | null>(null);
    const [activeTab,    setActiveTab]    = useState(0);

    // ── Ölçüm state ───────────────────────────────────────────────────────────
    const [measurements, setMeasurements] = useState<Measurement[]>([]);
    const [loadingM,     setLoadingM]     = useState(true);
    const [measureDlg,   setMeasureDlg]  = useState(false);
    const [mForm,        setMForm]        = useState<MeasureForm>(EMPTY_MEASURE);
    const [savingM,      setSavingM]      = useState(false);

    // ── Diyet plan state ──────────────────────────────────────────────────────
    const [plans,        setPlans]        = useState<MealPlan[]>([]);
    const [loadingP,     setLoadingP]     = useState(true);
    const [planDlg,      setPlanDlg]      = useState(false);
    const [editPlan,     setEditPlan]     = useState<MealPlan | null>(null);
    const [savingP,      setSavingP]      = useState(false);

    // Plan form
    const [planTitle,    setPlanTitle]    = useState('');
    const [planStart,    setPlanStart]    = useState(new Date().toISOString().slice(0, 10));
    const [planEnd,      setPlanEnd]      = useState('');
    const [planCal,      setPlanCal]      = useState(2000);
    const [planProt,     setPlanProt]     = useState(120);
    const [planCarbs,    setPlanCarbs]    = useState(250);
    const [planFat,      setPlanFat]      = useState(65);
    const [planRestrict, setPlanRestrict] = useState('');
    const [mealSlots,    setMealSlots]    = useState<MealSlot[]>(INIT_MEALS());

    // Besin arama
    const [foodSearch,   setFoodSearch]   = useState('');
    const [addToMeal,    setAddToMeal]    = useState<MealSlot['key']>('breakfast');
    const [addGrams,     setAddGrams]     = useState<number>(100);

    // ── Veri yükle ────────────────────────────────────────────────────────────
    const loadMeasurements = useCallback(async () => {
        setLoadingM(true);
        try {
            const data = await MeasurementService.getPatientMeasurements(patientId);
            const sorted = (data ?? []).sort((a: Measurement, b: Measurement) =>
                new Date(a.measurementDate).getTime() - new Date(b.measurementDate).getTime());
            setMeasurements(sorted);
        } catch {
            toast.current?.show({ severity: 'warn', summary: 'Uyarı', detail: 'Ölçümler alınamadı.', life: 3000 });
        } finally {
            setLoadingM(false);
        }
    }, [patientId]);

    const loadPlans = useCallback(async () => {
        setLoadingP(true);
        try {
            const data = await DietService.getMealPlansByPatient(patientId);
            setPlans(data ?? []);
        } catch {
            toast.current?.show({ severity: 'warn', summary: 'Uyarı', detail: 'Diyet planları alınamadı.', life: 3000 });
        } finally {
            setLoadingP(false);
        }
    }, [patientId]);

    useEffect(() => {
        PatientService.getPatientById(patientId)
            .then((p: PatientInfo) => setPatient(p))
            .catch(() => {});
        loadMeasurements();
        loadPlans();
    }, [patientId, loadMeasurements, loadPlans]);

    // ── Ölçüm ekle ────────────────────────────────────────────────────────────
    const handleAddMeasurement = async () => {
        if (!mForm.weight || !mForm.height) {
            toast.current?.show({ severity: 'warn', summary: 'Eksik', detail: 'Kilo ve boy zorunludur.', life: 3000 });
            return;
        }
        setSavingM(true);
        try {
            await MeasurementService.addMeasurement(patientId, {
                measurementDate:   new Date(mForm.measurementDate).toISOString(),
                weight:            mForm.weight,
                height:            mForm.height,
                waistCircumference: mForm.waistCircumference ?? undefined,
                hipCircumference:   mForm.hipCircumference ?? undefined,
                bodyFatPercentage:  mForm.bodyFatPercentage ?? undefined,
                notes:              mForm.notes,
            });
            toast.current?.show({ severity: 'success', summary: 'Kaydedildi', detail: 'Ölçüm eklendi.', life: 3000 });
            setMeasureDlg(false);
            setMForm(EMPTY_MEASURE);
            await loadMeasurements();
        } catch (e: unknown) {
            const err = e as { message?: string };
            toast.current?.show({ severity: 'error', summary: 'Hata', detail: err?.message ?? 'Ölçüm kaydedilemedi.', life: 4000 });
        } finally {
            setSavingM(false);
        }
    };

    // ── Diyet planı dialog aç/kapat ───────────────────────────────────────────
    const openNewPlan = () => {
        setEditPlan(null);
        setPlanTitle(''); setPlanStart(new Date().toISOString().slice(0, 10));
        setPlanEnd(''); setPlanCal(2000); setPlanProt(120); setPlanCarbs(250); setPlanFat(65);
        setPlanRestrict(''); setMealSlots(INIT_MEALS()); setFoodSearch('');
        setPlanDlg(true);
    };

    const openEditPlan = (plan: MealPlan) => {
        setEditPlan(plan);
        setPlanTitle(plan.title);
        setPlanStart(plan.startDate?.slice(0, 10) ?? '');
        setPlanEnd(plan.endDate?.slice(0, 10) ?? '');
        setPlanCal(plan.targetCalories);
        setPlanProt(plan.targetProtein);
        setPlanCarbs(plan.targetCarbs);
        setPlanFat(plan.targetFat);
        setPlanRestrict(plan.restrictions ?? '');
        // Öğünleri parse et
        const slots = INIT_MEALS();
        if (plan.meals) {
            const parseMeal = (raw: string | null, key: MealSlot['key']) => {
                const slot = slots.find(s => s.key === key)!;
                if (!raw) return;
                try { slot.items = JSON.parse(raw); } catch { /* raw text, ignore */ }
            };
            parseMeal(plan.meals.breakfast, 'breakfast');
            parseMeal(plan.meals.lunch,     'lunch');
            parseMeal(plan.meals.snack,     'snack');
            parseMeal(plan.meals.dinner,    'dinner');
        }
        setMealSlots(slots);
        setFoodSearch('');
        setPlanDlg(true);
    };

    // ── Besin ekle ────────────────────────────────────────────────────────────
    const filteredFoods = FOOD_DB.filter(f =>
        f.name.toLowerCase().includes(foodSearch.toLowerCase()) ||
        f.category.toLowerCase().includes(foodSearch.toLowerCase()));

    const addFoodToMeal = (food: FoodDB) => {
        const item = calcItemMacros(food, addGrams);
        setMealSlots(prev => prev.map(s =>
            s.key === addToMeal ? { ...s, items: [...s.items, item] } : s));
    };

    const removeItemFromMeal = (slotKey: MealSlot['key'], idx: number) => {
        setMealSlots(prev => prev.map(s =>
            s.key === slotKey ? { ...s, items: s.items.filter((_, i) => i !== idx) } : s));
    };

    // ── Toplam makrolar ───────────────────────────────────────────────────────
    const totalMacros = mealSlots.flatMap(s => s.items).reduce(
        (acc, i) => ({ cal: acc.cal + i.cal, p: acc.p + i.p, c: acc.c + i.c, f: acc.f + i.f }),
        { cal: 0, p: 0, c: 0, f: 0 }
    );

    // ── Plan kaydet ───────────────────────────────────────────────────────────
    const handleSavePlan = async () => {
        if (!planTitle.trim()) {
            toast.current?.show({ severity: 'warn', summary: 'Eksik', detail: 'Plan başlığı zorunludur.', life: 3000 });
            return;
        }
        setSavingP(true);
        try {
            const payload = {
                patientId,
                title:            planTitle.trim(),
                startDate:        new Date(planStart).toISOString(),
                endDate:          planEnd ? new Date(planEnd).toISOString() : undefined,
                targetCalories:   planCal,
                targetProtein:    planProt,
                targetCarbs:      planCarbs,
                targetFat:        planFat,
                restrictions:     planRestrict,
                isActive:         true,
                breakfastContent: serializeMeal(mealSlots.find(s => s.key === 'breakfast')!.items),
                snackContent:     serializeMeal(mealSlots.find(s => s.key === 'snack')!.items),
                lunchContent:     serializeMeal(mealSlots.find(s => s.key === 'lunch')!.items),
                dinnerContent:    serializeMeal(mealSlots.find(s => s.key === 'dinner')!.items),
            };
            if (editPlan) {
                await DietService.updateMealPlan(editPlan.id, payload);
                toast.current?.show({ severity: 'success', summary: 'Güncellendi', detail: 'Diyet planı güncellendi ve danışana bildirildi.', life: 4000 });
            } else {
                await DietService.createMealPlan(payload);
                toast.current?.show({ severity: 'success', summary: 'Gönderildi', detail: 'Diyet planı oluşturuldu ve danışana gönderildi.', life: 4000 });
            }
            setPlanDlg(false);
            await loadPlans();
        } catch (e: unknown) {
            const err = e as { message?: string };
            toast.current?.show({ severity: 'error', summary: 'Hata', detail: err?.message ?? 'Plan kaydedilemedi.', life: 5000 });
        } finally {
            setSavingP(false);
        }
    };

    const handleDeletePlan = (plan: MealPlan) => {
        confirmDialog({
            message:         `"${plan.title}" planını silmek istediğinize emin misiniz?`,
            header:          'Silme Onayı',
            icon:            'pi pi-exclamation-triangle',
            acceptClassName: 'p-button-danger',
            acceptLabel:     'Evet, Sil',
            rejectLabel:     'İptal',
            accept: async () => {
                try {
                    await DietService.deleteMealPlan(plan.id);
                    toast.current?.show({ severity: 'success', summary: 'Silindi', detail: 'Plan silindi.', life: 3000 });
                    await loadPlans();
                } catch {
                    toast.current?.show({ severity: 'error', summary: 'Hata', detail: 'Plan silinemedi.', life: 3000 });
                }
            },
        });
    };

    // ── PDF dışa aktar ────────────────────────────────────────────────────────
    const handleExportPDF = (plan: MealPlan) => {
        const parseSlot = (raw: string | null | undefined, label: string): PdfMealSlot => {
            let items: PdfMealSlot['items'] = [];
            if (raw) {
                try { items = JSON.parse(raw); } catch { /* ham metin */ }
            }
            return { label, items };
        };

        const slots: PdfMealSlot[] = [
            parseSlot(plan.meals?.breakfast, 'Sabah Kahvaltısı'),
            parseSlot(plan.meals?.snack,     'Ara Öğün'),
            parseSlot(plan.meals?.lunch,     'Öğle Yemeği'),
            parseSlot(plan.meals?.dinner,    'Akşam Yemeği'),
        ];

        const dietitianUser = AuthService.getCurrentUser();
        exportDietPlanPDF({
            plan: {
                title:           plan.title,
                startDate:       plan.startDate,
                endDate:         plan.endDate,
                targetCalories:  plan.targetCalories,
                targetProtein:   plan.targetProtein,
                targetCarbs:     plan.targetCarbs,
                targetFat:       plan.targetFat,
                restrictions:    plan.restrictions ?? '',
            },
            patientName:   patient?.fullName ?? '',
            dietitianName: dietitianUser?.fullName ?? '',
            mealSlots:     slots,
        });

        toast.current?.show({
            severity: 'success', summary: 'PDF İndirildi',
            detail: `${plan.title} — PDF hazırlandı.`, life: 3000,
        });
    };

    // ── Grafik verileri ───────────────────────────────────────────────────────
    const chartLabels  = measurements.map(m => fmtDate(m.measurementDate));
    const weightData = {
        labels: chartLabels,
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
        labels: chartLabels,
        datasets: [{
            label: 'BMI',
            data: measurements.map(m => m.bmi ?? null),
            fill: false,
            borderColor: '#2f4860',
            tension: 0.4,
            pointRadius: 5,
            pointBackgroundColor: '#2f4860',
        }],
    };
    const chartOpts = (unit: string) => ({
        plugins: { legend: { labels: { color: isDark ? '#ebedef' : '#495057' } } },
        scales: {
            x: { ticks: { color: isDark ? '#ebedef' : '#495057' }, grid: { color: isDark ? 'rgba(160,167,181,.3)' : '#ebedef' } },
            y: { ticks: { color: isDark ? '#ebedef' : '#495057', callback: (v: number) => `${v}${unit}` },
                 grid: { color: isDark ? 'rgba(160,167,181,.3)' : '#ebedef' } },
        },
    });

    // ── Plan DialogFooter ─────────────────────────────────────────────────────
    const planDialogFooter = (
        <div className="flex justify-content-between align-items-center w-full">
            <div className="flex gap-2">
                <Tag value={`${totalMacros.cal.toFixed(0)} kcal`}  severity="success" />
                <Tag value={`P: ${totalMacros.p.toFixed(0)}g`}      severity="info"    />
                <Tag value={`K: ${totalMacros.c.toFixed(0)}g`}      severity="warning" />
                <Tag value={`Y: ${totalMacros.f.toFixed(0)}g`}      severity="danger"  />
            </div>
            <div className="flex gap-2">
                <Button label="İptal"          icon="pi pi-times" text onClick={() => setPlanDlg(false)} />
                <Button label="Kaydet ve Gönder" icon="pi pi-send"  loading={savingP} onClick={handleSavePlan}
                    style={{ background: '#2f4860', borderColor: '#2f4860' }} />
            </div>
        </div>
    );

    return (
        <>
            <Toast ref={toast} position="top-right" />
            <ConfirmDialog />

            {/* ── Breadcrumb & Başlık ──────────────────────────────────────── */}
            <div className="card mb-3 py-3">
                <div className="flex align-items-center gap-2 text-500 text-sm mb-2">
                    <Link href="/dietitian/patients" className="text-primary no-underline hover:underline">Danışanlarım</Link>
                    <i className="pi pi-chevron-right text-xs" />
                    <span>{patient?.fullName ?? 'Yükleniyor...'}</span>
                </div>
                <div className="flex align-items-center gap-3">
                    <div className="flex align-items-center justify-content-center border-circle"
                        style={{ width: '3rem', height: '3rem', background: 'linear-gradient(135deg,#C9908C,#2f4860)', flexShrink: 0 }}>
                        <span className="text-white font-bold text-lg">
                            {patient?.fullName?.[0] ?? '?'}
                        </span>
                    </div>
                    <div>
                        <div className="text-900 text-2xl font-bold">{patient?.fullName ?? '...'}</div>
                        <div className="text-500 text-sm">{patient?.email ?? ''}{patient?.city ? ` · ${patient.city}` : ''}</div>
                    </div>
                </div>
            </div>

            {/* ── TabView ──────────────────────────────────────────────────── */}
            <div className="card p-0">
                <TabView activeIndex={activeTab} onTabChange={e => setActiveTab(e.index)}>

                    {/* ══ Tab 1: Ölçüm Geçmişi ══════════════════════════════ */}
                    <TabPanel header="Ölçüm Geçmişi" leftIcon="pi pi-chart-line mr-2">
                        <div className="p-3">
                            {/* Özet + Ekle */}
                            <div className="flex justify-content-between align-items-center mb-4">
                                <h5 className="m-0">Ölçüm Geçmişi</h5>
                                <Button label="Yeni Ölçüm Ekle" icon="pi pi-plus" size="small"
                                    style={{ background: '#C9908C', borderColor: '#C9908C' }}
                                    onClick={() => { setMForm(EMPTY_MEASURE); setMeasureDlg(true); }} />
                            </div>

                            {loadingM ? (
                                <div className="flex justify-content-center" style={{ height: 120, alignItems: 'center' }}>
                                    <ProgressSpinner style={{ width: 40, height: 40 }} strokeWidth="4" />
                                </div>
                            ) : measurements.length === 0 ? (
                                <div className="text-center py-5">
                                    <i className="pi pi-inbox text-500 text-5xl mb-3 block" />
                                    <p className="text-500">Henüz ölçüm kaydı bulunmuyor.</p>
                                </div>
                            ) : (
                                <div className="grid">
                                    {/* Özet kartlar */}
                                    {measurements.length >= 2 && (() => {
                                        const first = measurements[0];
                                        const last  = measurements[measurements.length - 1];
                                        const diff  = last.weight - first.weight;
                                        return (
                                            <>
                                                <div className="col-12 md:col-4">
                                                    <div className="surface-100 border-round p-3 text-center mb-3">
                                                        <div className="text-500 text-sm mb-1">Kilo Değişimi</div>
                                                        <div className={`text-2xl font-bold ${diff <= 0 ? 'text-green-500' : 'text-red-500'}`}>
                                                            {diff <= 0 ? '−' : '+'}{Math.abs(diff).toFixed(1)} kg
                                                        </div>
                                                        <div className="text-500 text-xs">{first.weight} → {last.weight} kg</div>
                                                    </div>
                                                </div>
                                                <div className="col-12 md:col-4">
                                                    <div className="surface-100 border-round p-3 text-center mb-3">
                                                        <div className="text-500 text-sm mb-1">Son BMI</div>
                                                        <div className="text-2xl font-bold text-blue-500">{last.bmi?.toFixed(1) ?? '—'}</div>
                                                        <div className="text-500 text-xs">Boy: {last.height} cm</div>
                                                    </div>
                                                </div>
                                                <div className="col-12 md:col-4">
                                                    <div className="surface-100 border-round p-3 text-center mb-3">
                                                        <div className="text-500 text-sm mb-1">Yağ Oranı</div>
                                                        <div className="text-2xl font-bold text-orange-500">
                                                            {last.bodyFatPercentage ? `%${last.bodyFatPercentage.toFixed(1)}` : '—'}
                                                        </div>
                                                        <div className="text-500 text-xs">Son ölçüm</div>
                                                    </div>
                                                </div>
                                            </>
                                        );
                                    })()}

                                    {/* Grafikler */}
                                    <div className="col-12 lg:col-6">
                                        <div className="card mb-3">
                                            <h6 className="mb-2">Kilo Takibi</h6>
                                            <Chart type="line" data={weightData} options={chartOpts(' kg')} />
                                        </div>
                                    </div>
                                    <div className="col-12 lg:col-6">
                                        <div className="card mb-3">
                                            <h6 className="mb-2">BMI Takibi</h6>
                                            <Chart type="line" data={bmiData} options={chartOpts('')} />
                                        </div>
                                    </div>

                                    {/* Tablo */}
                                    <div className="col-12">
                                        <DataTable
                                            value={[...measurements].reverse()}
                                            stripedRows dataKey="id"
                                            emptyMessage="Ölçüm bulunamadı."
                                        >
                                            <Column header="Tarih"      body={(r: Measurement) => fmtDate(r.measurementDate)} sortable />
                                            <Column header="Kilo (kg)"  body={(r: Measurement) => `${r.weight} kg`} />
                                            <Column header="Boy (cm)"   body={(r: Measurement) => `${r.height} cm`} />
                                            <Column header="BMI"        body={(r: Measurement) => r.bmi?.toFixed(1) ?? '—'} />
                                            <Column header="Bel (cm)"   body={(r: Measurement) => r.waistCircumference ? `${r.waistCircumference} cm` : '—'} />
                                            <Column header="Yağ %"      body={(r: Measurement) => r.bodyFatPercentage ? `%${r.bodyFatPercentage.toFixed(1)}` : '—'} />
                                            <Column header="Not"        field="notes" style={{ maxWidth: '200px' }} />
                                        </DataTable>
                                    </div>
                                </div>
                            )}
                        </div>
                    </TabPanel>

                    {/* ══ Tab 2: Diyet Programı ═════════════════════════════ */}
                    <TabPanel header="Diyet Programı" leftIcon="pi pi-list mr-2">
                        <div className="p-3">
                            <div className="flex justify-content-between align-items-center mb-4">
                                <h5 className="m-0">Diyet Planları</h5>
                                <Button label="Yeni Plan Oluştur" icon="pi pi-plus" size="small"
                                    style={{ background: '#2f4860', borderColor: '#2f4860' }}
                                    onClick={openNewPlan} />
                            </div>

                            {loadingP ? (
                                <div className="flex justify-content-center" style={{ height: 120, alignItems: 'center' }}>
                                    <ProgressSpinner style={{ width: 40, height: 40 }} strokeWidth="4" />
                                </div>
                            ) : plans.length === 0 ? (
                                <div className="text-center py-5">
                                    <i className="pi pi-clipboard text-500 text-5xl mb-3 block" />
                                    <p className="text-500">Bu danışan için henüz diyet planı oluşturulmadı.</p>
                                    <Button label="İlk Planı Oluştur" icon="pi pi-plus" className="mt-2"
                                        style={{ background: '#2f4860', borderColor: '#2f4860' }}
                                        onClick={openNewPlan} />
                                </div>
                            ) : (
                                <div className="grid">
                                    {plans.map(plan => {
                                        const mealCount = [
                                            plan.meals?.breakfast, plan.meals?.lunch,
                                            plan.meals?.snack, plan.meals?.dinner
                                        ].filter(Boolean).length;
                                        return (
                                            <div key={plan.id} className="col-12 lg:col-6">
                                                <div className="border-1 surface-border border-round p-4"
                                                    style={{ borderLeft: '4px solid #2f4860' }}>
                                                    <div className="flex justify-content-between align-items-start mb-2">
                                                        <div className="text-900 font-bold text-lg">{plan.title}</div>
                                                        <Tag value={PLAN_STATUS[plan.status] ?? '—'} severity={PLAN_SEV[plan.status] ?? 'info'} />
                                                    </div>
                                                    <div className="text-500 text-sm mb-3">
                                                        {fmtDate(plan.startDate)}
                                                        {plan.endDate ? ` → ${fmtDate(plan.endDate)}` : ''}
                                                        {plan.restrictions ? ` · ${plan.restrictions}` : ''}
                                                    </div>
                                                    <div className="flex flex-wrap gap-2 mb-3">
                                                        <Tag value={`${plan.targetCalories} kcal`} severity="success"  className="text-xs" />
                                                        <Tag value={`P: ${plan.targetProtein}g`}   severity="info"     className="text-xs" />
                                                        <Tag value={`K: ${plan.targetCarbs}g`}     severity="warning"  className="text-xs" />
                                                        <Tag value={`Y: ${plan.targetFat}g`}       severity="danger"   className="text-xs" />
                                                    </div>
                                                    <div className="text-500 text-xs mb-3">{mealCount} öğün tanımlı</div>
                                                    <div className="flex gap-2 flex-wrap mt-1">
                                                        <Button
                                                            icon="pi pi-pencil" label="Düzenle"
                                                            size="small" outlined severity="info"
                                                            onClick={() => openEditPlan(plan)}
                                                        />
                                                        <Button
                                                            icon="pi pi-file-pdf" label="PDF İndir"
                                                            size="small" outlined
                                                            style={{ color: '#C9908C', borderColor: '#C9908C' }}
                                                            onClick={() => handleExportPDF(plan)}
                                                        />
                                                        <Button
                                                            icon="pi pi-trash" label="Sil"
                                                            size="small" outlined severity="danger"
                                                            onClick={() => handleDeletePlan(plan)}
                                                        />
                                                    </div>
                                                </div>
                                            </div>
                                        );
                                    })}
                                </div>
                            )}
                        </div>
                    </TabPanel>
                </TabView>
            </div>

            {/* ══ Ölçüm Ekle Dialog ═════════════════════════════════════════ */}
            <Dialog header="Yeni Ölçüm Ekle" visible={measureDlg} style={{ width: '520px' }} modal onHide={() => setMeasureDlg(false)}
                footer={
                    <div>
                        <Button label="İptal"  icon="pi pi-times" text onClick={() => setMeasureDlg(false)} />
                        <Button label="Kaydet" icon="pi pi-check" loading={savingM} onClick={handleAddMeasurement}
                            style={{ background: '#C9908C', borderColor: '#C9908C' }} />
                    </div>
                }>
                <div className="p-fluid grid formgrid mt-2">
                    <div className="field col-12">
                        <label>Ölçüm Tarihi</label>
                        <InputText type="date" value={mForm.measurementDate}
                            onChange={e => setMForm(f => ({ ...f, measurementDate: e.target.value }))} />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label>Kilo (kg) *</label>
                        <InputNumber value={mForm.weight} min={0} minFractionDigits={1} maxFractionDigits={1}
                            onValueChange={e => setMForm(f => ({ ...f, weight: e.value ?? 0 }))} />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label>Boy (cm) *</label>
                        <InputNumber value={mForm.height} min={0} minFractionDigits={0} maxFractionDigits={1}
                            onValueChange={e => setMForm(f => ({ ...f, height: e.value ?? 0 }))} />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label>Bel Çevresi (cm)</label>
                        <InputNumber value={mForm.waistCircumference} min={0} minFractionDigits={1} maxFractionDigits={1}
                            onValueChange={e => setMForm(f => ({ ...f, waistCircumference: e.value ?? null }))} />
                    </div>
                    <div className="field col-12 md:col-6">
                        <label>Kalça Çevresi (cm)</label>
                        <InputNumber value={mForm.hipCircumference} min={0} minFractionDigits={1} maxFractionDigits={1}
                            onValueChange={e => setMForm(f => ({ ...f, hipCircumference: e.value ?? null }))} />
                    </div>
                    <div className="field col-12">
                        <label>Vücut Yağ Oranı (%)</label>
                        <InputNumber value={mForm.bodyFatPercentage} min={0} max={100} minFractionDigits={1} maxFractionDigits={1}
                            onValueChange={e => setMForm(f => ({ ...f, bodyFatPercentage: e.value ?? null }))} />
                    </div>
                    <div className="field col-12">
                        <label>Not</label>
                        <InputTextarea value={mForm.notes} rows={2}
                            onChange={e => setMForm(f => ({ ...f, notes: e.target.value }))} autoResize />
                    </div>
                </div>
            </Dialog>

            {/* ══ Diyet Planı Dialog ════════════════════════════════════════ */}
            <Dialog
                header={editPlan ? `Plan Düzenle — ${editPlan.title}` : 'Yeni Diyet Planı Oluştur'}
                visible={planDlg} style={{ width: '92vw', maxWidth: '1100px' }}
                modal maximizable onHide={() => setPlanDlg(false)}
                footer={planDialogFooter}
            >
                <div className="grid mt-1">

                    {/* Sol: Plan Detayları + Besin Arama */}
                    <div className="col-12 lg:col-5">
                        {/* Plan Bilgileri */}
                        <div className="p-fluid">
                            <div className="field">
                                <label>Plan Başlığı *</label>
                                <InputText value={planTitle} onChange={e => setPlanTitle(e.target.value)} placeholder="örn. 1500 kcal Zayıflama Diyeti" />
                            </div>
                            <div className="grid formgrid">
                                <div className="field col-6">
                                    <label>Başlangıç Tarihi</label>
                                    <InputText type="date" value={planStart} onChange={e => setPlanStart(e.target.value)} />
                                </div>
                                <div className="field col-6">
                                    <label>Bitiş Tarihi</label>
                                    <InputText type="date" value={planEnd} onChange={e => setPlanEnd(e.target.value)} />
                                </div>
                                <div className="field col-6">
                                    <label>Hedef Kalori (kcal)</label>
                                    <InputNumber value={planCal} min={0} onValueChange={e => setPlanCal(e.value ?? 0)} />
                                </div>
                                <div className="field col-6">
                                    <label>Protein (g)</label>
                                    <InputNumber value={planProt} min={0} onValueChange={e => setPlanProt(e.value ?? 0)} />
                                </div>
                                <div className="field col-6">
                                    <label>Karbonhidrat (g)</label>
                                    <InputNumber value={planCarbs} min={0} onValueChange={e => setPlanCarbs(e.value ?? 0)} />
                                </div>
                                <div className="field col-6">
                                    <label>Yağ (g)</label>
                                    <InputNumber value={planFat} min={0} onValueChange={e => setPlanFat(e.value ?? 0)} />
                                </div>
                                <div className="field col-12">
                                    <label>Kısıtlamalar / Alerji</label>
                                    <InputText value={planRestrict} onChange={e => setPlanRestrict(e.target.value)} placeholder="örn. Glüten yok, Laktoz yok" />
                                </div>
                            </div>
                        </div>

                        <Divider />

                        {/* Besin Arama */}
                        <div className="font-semibold text-900 mb-2">Besin Veritabanı</div>
                        <div className="p-fluid mb-2">
                            <span className="p-input-icon-left w-full">
                                <i className="pi pi-search" />
                                <InputText value={foodSearch} onChange={e => setFoodSearch(e.target.value)} placeholder="Besin ara..." />
                            </span>
                        </div>
                        <div className="grid formgrid mb-2">
                            <div className="field col-6">
                                <label className="text-sm">Eklenecek Öğün</label>
                                <Dropdown value={addToMeal} options={[
                                    { label: 'Sabah Kahvaltısı', value: 'breakfast' },
                                    { label: 'Ara Öğün',         value: 'snack' },
                                    { label: 'Öğle Yemeği',      value: 'lunch' },
                                    { label: 'Akşam Yemeği',     value: 'dinner' },
                                ]} onChange={e => setAddToMeal(e.value)} />
                            </div>
                            <div className="field col-6">
                                <label className="text-sm">Miktar (gram)</label>
                                <InputNumber value={addGrams} min={1} max={2000}
                                    onValueChange={e => setAddGrams(e.value ?? 100)} />
                            </div>
                        </div>

                        <div style={{ maxHeight: '260px', overflowY: 'auto' }}>
                            {filteredFoods.map(food => (
                                <div key={food.id} className="flex justify-content-between align-items-center p-2 border-bottom-1 surface-border hover:surface-100 border-round cursor-pointer"
                                    style={{ transition: 'background .15s' }}>
                                    <div>
                                        <div className="text-900 text-sm font-medium">{food.name}</div>
                                        <div className="text-500 text-xs">{food.calories} kcal/100g · P:{food.protein}g K:{food.carbs}g Y:{food.fat}g</div>
                                    </div>
                                    <Button icon="pi pi-plus" rounded text size="small"
                                        style={{ color: '#2f4860' }}
                                        tooltip={`${addGrams}g ekle`} tooltipOptions={{ position: 'left' }}
                                        onClick={() => addFoodToMeal(food)} />
                                </div>
                            ))}
                        </div>
                    </div>

                    {/* Sağ: Öğün Listesi */}
                    <div className="col-12 lg:col-7">
                        {mealSlots.map(slot => {
                            const slotTotal = slot.items.reduce((s, i) => ({ cal: s.cal + i.cal, p: s.p + i.p, c: s.c + i.c, f: s.f + i.f }), { cal: 0, p: 0, c: 0, f: 0 });
                            return (
                                <div key={slot.key} className="border-round mb-3 overflow-hidden"
                                    style={{ border: `1px solid ${slot.color}22` }}>
                                    {/* Öğün başlığı */}
                                    <div className="flex align-items-center justify-content-between px-3 py-2"
                                        style={{ background: slot.bg }}>
                                        <div className="flex align-items-center gap-2">
                                            <i className={`pi ${slot.icon}`} style={{ color: slot.color }} />
                                            <span className="font-semibold text-900 text-sm">{slot.label}</span>
                                        </div>
                                        <span className="text-xs text-500">{slotTotal.cal.toFixed(0)} kcal</span>
                                    </div>

                                    {/* Besinler */}
                                    {slot.items.length === 0 ? (
                                        <div className="px-3 py-2 text-500 text-sm text-center">
                                            Henüz besin eklenmedi. Soldan ekleyiniz.
                                        </div>
                                    ) : (
                                        <div className="px-2 py-1">
                                            {slot.items.map((item, idx) => (
                                                <div key={idx} className="flex justify-content-between align-items-center py-2 border-bottom-1 surface-border">
                                                    <div>
                                                        <span className="text-900 text-sm font-medium">{item.name}</span>
                                                        <span className="text-500 text-xs ml-2">{item.grams}g</span>
                                                    </div>
                                                    <div className="flex align-items-center gap-2">
                                                        <span className="text-500 text-xs">{item.cal} kcal</span>
                                                        <Button icon="pi pi-times" rounded text size="small" severity="danger"
                                                            onClick={() => removeItemFromMeal(slot.key, idx)} />
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                </div>
            </Dialog>
        </>
    );
};

export default PatientDetailPage;
