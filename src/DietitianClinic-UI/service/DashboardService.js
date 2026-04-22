import axios from 'axios';

const BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

const api = axios.create({ baseURL: BASE, timeout: 15_000 });

const authHeader = () => {
    if (typeof window === 'undefined') return {};
    try {
        const user = JSON.parse(localStorage.getItem('user') || 'null');
        return user?.token ? { Authorization: `Bearer ${user.token}` } : {};
    } catch { return {}; }
};

const cfg = () => ({ headers: { 'Content-Type': 'application/json', ...authHeader() } });

export const DashboardService = {
    /** Özet istatistikler: hasta, randevu, diyet planı sayıları */
    getSummary: async () => {
        const res = await api.get('/Dashboard/summary', cfg());
        return res.data;
    },

    /** Grafik verileri: aylık hasta/randevu, diyetisyen dağılımı */
    getAnalytics: async () => {
        const res = await api.get('/Dashboard/analytics', cfg());
        return res.data;
    },

    /** Raporlama tablosu: hasta bazlı detaylı özet */
    getReports: async () => {
        const res = await api.get('/Dashboard/reports', cfg());
        return res.data;
    },
};
