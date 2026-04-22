import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

// ─── Tipler ───────────────────────────────────────────────────────────────────
export interface PdfMealItem {
    name:  string;
    grams: number;
    cal:   number;
    p:     number;
    c:     number;
    f:     number;
}

export interface PdfMealSlot {
    label: string;
    items: PdfMealItem[];
}

export interface ExportDietPlanOptions {
    plan: {
        title:           string;
        startDate:       string;
        endDate:         string | null;
        targetCalories:  number;
        targetProtein:   number;
        targetCarbs:     number;
        targetFat:       number;
        restrictions:    string;
    };
    patientName:   string;
    dietitianName: string;
    mealSlots:     PdfMealSlot[];
}

// ─── Renk Paleti ─────────────────────────────────────────────────────────────
type RGB = [number, number, number];
const NAVY:       RGB = [47, 72, 96];
const ROSE:       RGB = [201, 144, 140];
const NAVY_LIGHT: RGB = [232, 238, 245];
const ROSE_LIGHT: RGB = [252, 244, 243];
const ALT_ROW:    RGB = [253, 249, 249];
const WHITE:      RGB = [255, 255, 255];
const GRAY:       RGB = [130, 130, 130];
const DARK:       RGB = [35, 35, 35];
const BORDER:     RGB = [218, 218, 218];

// ─── Yardımcılar ──────────────────────────────────────────────────────────────
const fmtDate = (iso: string) =>
    new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: 'long', year: 'numeric' });

const today = () =>
    new Date().toLocaleDateString('tr-TR', { day: '2-digit', month: 'long', year: 'numeric' });

// ─── Ana Fonksiyon ────────────────────────────────────────────────────────────
export const exportDietPlanPDF = ({
    plan, patientName, dietitianName, mealSlots,
}: ExportDietPlanOptions): void => {

    const doc  = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    const W    = doc.internal.pageSize.getWidth();   // 210
    const H    = doc.internal.pageSize.getHeight();  // 297
    const mg   = 15;
    const cw   = W - mg * 2;  // content width = 180
    let y      = 0;

    // ────────────────────────────────────────────────────────────────────────
    // Sayfa başlığı — her yeni sayfada çizilir
    // ────────────────────────────────────────────────────────────────────────
    const drawPageHeader = () => {
        // Navy banner
        doc.setFillColor(...NAVY);
        doc.rect(0, 0, W, 30, 'F');

        // Logo dairesi (dusty rose)
        doc.setFillColor(...ROSE);
        doc.circle(mg + 7, 15, 7, 'F');
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(9);
        doc.setTextColor(...WHITE);
        doc.text('FR', mg + 7, 16.2, { align: 'center' });

        // Kurum adı
        doc.setFontSize(15);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(...WHITE);
        doc.text('FitRehber Diyetisyenlik ve Danışmanlık', mg + 18, 13);

        // Alt başlık
        doc.setFontSize(8.5);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(220, 205, 205);
        doc.text('Kişiselleştirilmiş Beslenme Programı', mg + 18, 20);

        // Sağ üst köşe: tarih
        doc.setFontSize(8);
        doc.setTextColor(200, 190, 190);
        doc.text(today(), W - mg, 20, { align: 'right' });

        y = 38;
    };

    // ────────────────────────────────────────────────────────────────────────
    // Sayfa altbilgisi — her sayfa için
    // ────────────────────────────────────────────────────────────────────────
    const drawFooter = (pageNum: number, total: number) => {
        doc.setFillColor(245, 245, 245);
        doc.rect(0, H - 11, W, 11, 'F');

        // Sol: marka
        doc.setFontSize(7.5);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(...GRAY);
        doc.text('FitRehber © 2026 — Tüm hakları saklıdır.', mg, H - 4.5);

        // Sağ: sayfa no
        doc.text(`Sayfa ${pageNum} / ${total}`, W - mg, H - 4.5, { align: 'right' });
    };

    // ────────────────────────────────────────────────────────────────────────
    // İlk sayfa
    // ────────────────────────────────────────────────────────────────────────
    drawPageHeader();

    // ── Hasta / Diyetisyen Bilgi Kartı ─────────────────────────────────────
    doc.setFillColor(...NAVY_LIGHT);
    doc.roundedRect(mg, y, cw, 32, 2, 2, 'F');

    // Sol sütun — Hasta
    doc.setFontSize(7.5);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(...NAVY);
    doc.text('HASTA', mg + 6, y + 8);

    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(...DARK);
    doc.text(patientName || '—', mg + 6, y + 16);

    doc.setFontSize(8.5);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(...GRAY);
    doc.text(`Başlangıç: ${fmtDate(plan.startDate)}`, mg + 6, y + 23);
    if (plan.endDate) {
        doc.text(`Bitiş: ${fmtDate(plan.endDate)}`, mg + 6, y + 29);
    }

    // Orta dikey çizgi
    doc.setDrawColor(...BORDER);
    doc.setLineWidth(0.3);
    doc.line(W / 2, y + 5, W / 2, y + 27);

    // Sağ sütun — Diyetisyen
    const rx = W / 2 + 6;
    doc.setFontSize(7.5);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(...NAVY);
    doc.text('DİYETİSYEN', rx, y + 8);

    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(...DARK);
    doc.text(dietitianName || '—', rx, y + 16);

    doc.setFontSize(8.5);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(...GRAY);
    doc.text(`Rapor Tarihi: ${today()}`, rx, y + 23);

    y += 38;

    // ── Plan Başlığı Bandı ─────────────────────────────────────────────────
    doc.setFillColor(...ROSE);
    doc.rect(mg, y, cw, 9, 'F');

    doc.setFontSize(11);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(...WHITE);
    doc.text(plan.title, mg + 5, y + 6.2);
    y += 9;

    // ── Hedef Makrolar ─────────────────────────────────────────────────────
    doc.setFillColor(...ROSE_LIGHT);
    doc.rect(mg, y, cw, 9, 'F');

    const macros = [
        { label: 'Hedef Kalori', value: `${plan.targetCalories} kcal` },
        { label: 'Protein',      value: `${plan.targetProtein} g` },
        { label: 'Karbonhidrat', value: `${plan.targetCarbs} g` },
        { label: 'Yağ',          value: `${plan.targetFat} g` },
    ];
    const cell = cw / macros.length;
    macros.forEach((m, i) => {
        const cx = mg + i * cell + cell / 2;
        doc.setFontSize(7);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(...GRAY);
        doc.text(m.label, cx, y + 3.5, { align: 'center' });

        doc.setFontSize(9);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(...NAVY);
        doc.text(m.value, cx, y + 7.5, { align: 'center' });
    });
    y += 9;

    // Kısıtlamalar
    if (plan.restrictions) {
        doc.setFillColor(255, 248, 240);
        doc.rect(mg, y, cw, 8, 'F');
        doc.setFontSize(8);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(180, 80, 60);
        doc.text('Kısıtlamalar / Alerjiler:', mg + 4, y + 5.5);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(DARK[0], DARK[1], DARK[2]);
        doc.text(plan.restrictions, mg + 44, y + 5.5);
        y += 8;
    }

    y += 6;

    // ── Öğün Tabloları ─────────────────────────────────────────────────────
    const MEAL_ICONS: Record<string, string> = {
        'Sabah Kahvaltısı': '\u2600',  // ☀
        'Ara Öğün':         '\u25C6',  // ◆
        'Öğle Yemeği':      '\u2B24',  // ⬤
        'Akşam Yemeği':     '\u263D',  // ☽
    };

    for (const slot of mealSlots) {
        if (slot.items.length === 0) continue;

        // Sayfa taşma kontrolü
        const estimatedHeight = 10 + slot.items.length * 8 + 12;
        if (y + estimatedHeight > H - 20) {
            doc.addPage();
            drawPageHeader();
        }

        const slotCal = slot.items.reduce((s, i) => s + i.cal, 0);
        const icon    = MEAL_ICONS[slot.label] ?? '\u25A0';

        // Öğün başlık bandı (navy)
        doc.setFillColor(...NAVY);
        doc.rect(mg, y, cw, 8, 'F');

        doc.setFontSize(10);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(...WHITE);
        doc.text(`${icon}  ${slot.label.toUpperCase()}`, mg + 5, y + 5.5);

        doc.setFontSize(9);
        doc.text(`${slotCal.toFixed(0)} kcal`, W - mg - 3, y + 5.5, { align: 'right' });
        y += 8;

        // Besin tablosu
        autoTable(doc, {
            startY: y,
            margin: { left: mg, right: mg },
            head: [['Besin Adı', 'Miktar', 'Kalori', 'Protein', 'Karbonhidrat', 'Yağ']],
            body: slot.items.map(item => [
                item.name,
                item.grams > 0 ? `${item.grams} g` : '—',
                `${item.cal.toFixed(0)} kcal`,
                `${item.p.toFixed(1)} g`,
                `${item.c.toFixed(1)} g`,
                `${item.f.toFixed(1)} g`,
            ]),
            headStyles: {
                fillColor:  ROSE_LIGHT,
                textColor:  NAVY,
                fontStyle:  'bold',
                fontSize:   8,
                halign:     'center',
            },
            bodyStyles: {
                fontSize:  8.5,
                textColor: DARK,
            },
            columnStyles: {
                0: { cellWidth: 58,  halign: 'left' },
                1: { cellWidth: 22,  halign: 'center' },
                2: { cellWidth: 26,  halign: 'center' },
                3: { cellWidth: 22,  halign: 'center' },
                4: { cellWidth: 30,  halign: 'center' },
                5: { cellWidth: 22,  halign: 'center' },
            },
            alternateRowStyles: { fillColor: ALT_ROW },
            tableLineColor:     BORDER,
            tableLineWidth:     0.15,
            styles: { overflow: 'linebreak', cellPadding: 2 },
            // Yeni sayfa açılırsa header'ı yeniden çiz
            didDrawPage: () => { drawPageHeader(); },
        });

        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        y = (doc as any).lastAutoTable.finalY + 7;
    }

    // ── Günlük Toplam Özet ─────────────────────────────────────────────────
    if (y + 20 > H - 20) { doc.addPage(); drawPageHeader(); }

    const totals = mealSlots
        .flatMap(s => s.items)
        .reduce(
            (acc, i) => ({ cal: acc.cal + i.cal, p: acc.p + i.p, c: acc.c + i.c, f: acc.f + i.f }),
            { cal: 0, p: 0, c: 0, f: 0 }
        );

    // Toplam başlık bandı
    doc.setFillColor(...NAVY);
    doc.rect(mg, y, cw, 8, 'F');
    doc.setFontSize(10);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(...WHITE);
    doc.text('GÜNLÜK TOPLAM', mg + 5, y + 5.5);
    y += 8;

    // Toplam değerler
    doc.setFillColor(...NAVY_LIGHT);
    doc.rect(mg, y, cw, 11, 'F');

    const totalCells = [
        { label: 'Kalori',       value: `${totals.cal.toFixed(0)} kcal` },
        { label: 'Protein',      value: `${totals.p.toFixed(1)} g`      },
        { label: 'Karbonhidrat', value: `${totals.c.toFixed(1)} g`      },
        { label: 'Yağ',          value: `${totals.f.toFixed(1)} g`      },
    ];
    totalCells.forEach((tc, i) => {
        const cx = mg + i * (cw / 4) + (cw / 4) / 2;
        doc.setFontSize(7);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(...GRAY);
        doc.text(tc.label, cx, y + 4, { align: 'center' });

        doc.setFontSize(10.5);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(...NAVY);
        doc.text(tc.value, cx, y + 9.5, { align: 'center' });
    });
    y += 18;

    // ── Not Alanı ──────────────────────────────────────────────────────────
    if (y + 18 < H - 20) {
        doc.setDrawColor(...BORDER);
        doc.setLineWidth(0.3);
        doc.roundedRect(mg, y, cw, 16, 1.5, 1.5, 'S');
        doc.setFontSize(8);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(...NAVY);
        doc.text('Diyetisyen Notları:', mg + 4, y + 6);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(...GRAY);
        doc.text('Bu program kişisel sağlık hedefleriniz doğrultusunda hazırlanmıştır. Herhangi bir sağlık sorununuzda', mg + 4, y + 11);
        doc.text('diyetisyeninizle iletişime geçiniz.', mg + 4, y + 15.5);
    }

    // ── Footer'ları çiz ────────────────────────────────────────────────────
    const totalPages = doc.getNumberOfPages();
    for (let p = 1; p <= totalPages; p++) {
        doc.setPage(p);
        drawFooter(p, totalPages);
    }

    // ── PDF'i indir ────────────────────────────────────────────────────────
    const safeName = (patientName || 'hasta').replace(/\s+/g, '_').replace(/[^a-zA-Z0-9_ğüşıöçĞÜŞİÖÇ]/g, '');
    doc.save(`FitRehber_DiyetProgrami_${safeName}.pdf`);
};
