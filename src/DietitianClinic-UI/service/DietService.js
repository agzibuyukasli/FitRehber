import axios from 'axios';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

const getAuthHeader = () => {
    if (typeof window === 'undefined') return {};
    try {
        const user = JSON.parse(localStorage.getItem('user') || 'null');
        return user?.token ? { Authorization: `Bearer ${user.token}` } : {};
    } catch {
        return {};
    }
};

const handleError = (error) => {
    const status  = error?.response?.status;
    const message = error?.response?.data?.message || error?.response?.data?.title || error?.message;
    if (status === 401) throw { type: 'auth',       message: 'Oturum süresi doldu. Lütfen tekrar giriş yapın.' };
    if (status === 403) throw { type: 'forbidden',  message: 'Bu işlem için yetkiniz yok.' };
    if (status === 400) throw { type: 'validation', message: message || 'Geçersiz veri.' };
    if (status === 404) throw { type: 'notfound',   message: 'Kayıt bulunamadı.' };
    throw { type: 'server', message: message || 'Sunucu hatası.' };
};

const cfg = () => ({
    headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
    timeout: 10_000,
});

export const DietService = {
    /** Tüm diyet planlarını getirir (rol bazlı filtre backend'de yapılır). */
    getMealPlans: async () => {
        try {
            const res = await axios.get(`${API_URL}/MealPlans`, cfg());
            return Array.isArray(res.data) ? res.data : [];
        } catch (e) { handleError(e); }
    },

    /** Belirli bir hasta için diyet planlarını getirir. */
    getMealPlansByPatient: async (patientId) => {
        try {
            const res = await axios.get(`${API_URL}/MealPlans`, cfg());
            const all = Array.isArray(res.data) ? res.data : [];
            return all.filter(p => p.patientId === patientId);
        } catch (e) { handleError(e); }
    },

    /** Tek bir diyet planını getirir. */
    getMealPlan: async (id) => {
        try {
            const res = await axios.get(`${API_URL}/MealPlans/${id}`, cfg());
            return res.data;
        } catch (e) { handleError(e); }
    },

    /**
     * Yeni diyet planı oluşturur.
     * @param {{ patientId, title, startDate, endDate?, targetCalories, targetProtein, targetCarbs, targetFat,
     *           restrictions?, isActive, breakfastContent?, lunchContent?, snackContent?, dinnerContent? }} data
     */
    createMealPlan: async (data) => {
        try {
            const res = await axios.post(`${API_URL}/MealPlans`, data, cfg());
            return res.data;
        } catch (e) { handleError(e); }
    },

    /** Diyet planını günceller. */
    updateMealPlan: async (id, data) => {
        try {
            const res = await axios.put(`${API_URL}/MealPlans/${id}`, data, cfg());
            return res.data;
        } catch (e) { handleError(e); }
    },

    /** Diyet planını siler. */
    deleteMealPlan: async (id) => {
        try {
            await axios.delete(`${API_URL}/MealPlans/${id}`, cfg());
        } catch (e) { handleError(e); }
    },
};
