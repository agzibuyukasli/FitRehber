'use client';
import { ProgressSpinner } from 'primereact/progressspinner';
import { Tag }             from 'primereact/tag';
import { Toast }           from 'primereact/toast';
import React, { useEffect, useRef, useState } from 'react';
import { DietService } from '../../../../service/DietService';

// ─── Tipler ───────────────────────────────────────────────────────────────────
interface MealItem { name: string; grams: number; cal: number; p: number; c: number; f: number; }

interface MealSlotCfg {
    key:   'breakfast' | 'snack' | 'lunch' | 'dinner';
    label: string;
    icon:  string;
    color: string;
    bg:    string;
    items: MealItem[];
}

interface MealPlan {
    id:             number;
    title:          string;
    targetCalories: number;
    targetProtein:  number;
    targetCarbs:    number;
    targetFat:      number;
    restrictions:   string;
    status:         number;
    startDate:      string;
    endDate:        string | null;
    meals: {
        breakfast: string | null;
        lunch:     string | null;
        snack:     string | null;
        dinner:    string | null;
    } | null;
}

// ─── Sabitler ─────────────────────────────────────────────────────────────────
const SLOT_CFG: Omit<MealSlotCfg, 'items'>[] = [
    { key: 'breakfast', label: 'Sabah Kahvaltısı', icon: 'pi-sun',         color: '#F97316', bg: '#FFF7ED' },
    { key: 'snack',     label: 'Ara Öğün',         icon: 'pi-apple',       color: '#22C55E', bg: '#F0FDF4' },
    { key: 'lunch',     label: 'Öğle Yemeği',      icon: 'pi-circle-fill', color: '#3B82F6', bg: '#EFF6FF' },
    { key: 'dinner',    label: 'Akşam Yemeği',     icon: 'pi-moon',        color: '#8B5CF6', bg: '#F5F3FF' },
];

const parseMealItems = (raw: string | null | undefined): MealItem[] => {
    if (!raw) return [];
    try {
        const parsed = JSON.parse(raw);
        return Array.isArray(parsed) ? parsed : [];
    } catch {
        // Raw metin ise satır satır göster
        return raw.split('\n').filter(Boolean).map((line, i) => ({
            name: line.trim(), grams: 0, cal: 0, p: 0, c: 0, f: 0,
        }));
    }
};

const fmtDate = (iso: string) =>
    new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: 'long', year: 'numeric' });

// ─── Bileşen ──────────────────────────────────────────────────────────────────
const PatientDietPage = () => {
    const toast = useRef<Toast>(null);
    const [plan,    setPlan]    = useState<MealPlan | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        DietService.getMealPlans()
            .then((data: MealPlan[] | undefined) => {
                if (!data || data.length === 0) { setPlan(null); return; }
                // En son aktif plan, yoksa en son plan
                const active = data.filter(p => p.status === 1)
                    .sort((a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime());
                setPlan(active[0] ?? data.sort((a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime())[0]);
            })
            .catch(() => {
                toast.current?.show({ severity: 'warn', summary: 'Bağlantı', detail: 'Diyet planı alınamadı.', life: 4000 });
                setPlan(null);
            })
            .finally(() => setLoading(false));
    }, []);

    if (loading) return (
        <div className="flex justify-content-center align-items-center" style={{ height: '60vh' }}>
            <ProgressSpinner style={{ width: 50, height: 50 }} strokeWidth="4" />
        </div>
    );

    if (!plan) return (
        <div className="grid">
            <div className="col-12">
                <div className="card text-center py-6">
                    <i className="pi pi-clipboard text-500 text-6xl mb-3 block" />
                    <h5 className="text-500">Henüz bir diyet planı atanmadı.</h5>
                    <p className="text-500 text-sm">Diyetisyeniniz yakında planınızı hazırlayacak.</p>
                </div>
            </div>
        </div>
    );

    const slots: MealSlotCfg[] = SLOT_CFG.map(cfg => ({
        ...cfg,
        items: parseMealItems(plan.meals?.[cfg.key]),
    }));

    const totalMacros = slots.flatMap(s => s.items).reduce(
        (acc, i) => ({ cal: acc.cal + i.cal, p: acc.p + i.p, c: acc.c + i.c, f: acc.f + i.f }),
        { cal: 0, p: 0, c: 0, f: 0 }
    );
    const hasRealMacros = totalMacros.cal > 0;

    return (
        <>
            <Toast ref={toast} position="top-right" />
            <div className="grid">
                <div className="col-12">
                    <div className="card">
                        {/* Başlık */}
                        <div className="flex justify-content-between align-items-start mb-1 flex-wrap gap-2">
                            <div>
                                <h5 className="m-0 mb-1">{plan.title}</h5>
                                <div className="text-500 text-sm">
                                    {fmtDate(plan.startDate)}
                                    {plan.endDate ? ` → ${fmtDate(plan.endDate)}` : ''}
                                    {plan.restrictions ? <span className="ml-2 text-orange-600">· {plan.restrictions}</span> : null}
                                </div>
                            </div>
                            {/* Hedefler */}
                            <div className="flex gap-2 flex-wrap">
                                <Tag severity="success" value={`Hedef: ${plan.targetCalories} kcal`}   className="text-sm" />
                                <Tag severity="info"    value={`Protein: ${plan.targetProtein}g`}      className="text-sm" />
                                <Tag severity="warning" value={`Karb: ${plan.targetCarbs}g`}           className="text-sm" />
                                <Tag severity="danger"  value={`Yağ: ${plan.targetFat}g`}              className="text-sm" />
                            </div>
                        </div>

                        {/* Gerçek toplam (besin eklendiyse) */}
                        {hasRealMacros && (
                            <div className="flex gap-2 flex-wrap mt-3 mb-1 p-2 surface-100 border-round">
                                <span className="text-500 text-sm font-medium mr-1">Günlük Toplam:</span>
                                <Tag severity="success" value={`${totalMacros.cal.toFixed(0)} kcal`} />
                                <Tag severity="info"    value={`P: ${totalMacros.p.toFixed(0)}g`}    />
                                <Tag severity="warning" value={`K: ${totalMacros.c.toFixed(0)}g`}    />
                                <Tag severity="danger"  value={`Y: ${totalMacros.f.toFixed(0)}g`}    />
                            </div>
                        )}

                        {/* Öğün Kartları */}
                        <div className="grid mt-3">
                            {slots.map(slot => (
                                <div key={slot.key} className="col-12 lg:col-6">
                                    <div className="border-round overflow-hidden mb-2"
                                        style={{ border: `1px solid ${slot.color}33` }}>
                                        {/* Öğün başlığı */}
                                        <div className="flex align-items-center justify-content-between px-3 py-2"
                                            style={{ background: slot.bg }}>
                                            <div className="flex align-items-center gap-2">
                                                <div className="flex align-items-center justify-content-center border-circle"
                                                    style={{ width: '2rem', height: '2rem', background: slot.color + '22' }}>
                                                    <i className={`pi ${slot.icon}`} style={{ color: slot.color }} />
                                                </div>
                                                <span className="font-semibold text-900">{slot.label}</span>
                                            </div>
                                            {slot.items.length > 0 && (
                                                <span className="text-xs text-500">
                                                    {slot.items.reduce((s, i) => s + i.cal, 0).toFixed(0)} kcal
                                                </span>
                                            )}
                                        </div>

                                        {/* Besinler */}
                                        {slot.items.length === 0 ? (
                                            <div className="px-3 py-3 text-500 text-sm text-center">
                                                Bu öğün için henüz besin eklenmemiş.
                                            </div>
                                        ) : (
                                            <ul className="list-none p-0 m-0 px-1">
                                                {slot.items.map((item, idx) => (
                                                    <li key={idx} className="flex justify-content-between align-items-center px-2 py-2 border-bottom-1 surface-border">
                                                        <div>
                                                            <div className="text-900 text-sm font-medium">{item.name}</div>
                                                            {item.grams > 0 && (
                                                                <div className="text-500 text-xs">{item.grams}g</div>
                                                            )}
                                                        </div>
                                                        {item.cal > 0 && (
                                                            <div className="text-right">
                                                                <div className="text-900 text-sm">{item.cal} kcal</div>
                                                                <div className="text-500 text-xs">
                                                                    P:{item.p}g · K:{item.c}g · Y:{item.f}g
                                                                </div>
                                                            </div>
                                                        )}
                                                    </li>
                                                ))}
                                            </ul>
                                        )}
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
};

export default PatientDietPage;
