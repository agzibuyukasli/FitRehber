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

export const MeasurementService = {
    /** Diyetisyen: hastanın tüm ölçümlerini getirir. */
    getPatientMeasurements: async (patientId) => {
        try {
            const res = await axios.get(`${API_URL}/Patients/${patientId}/measurements`, cfg());
            return Array.isArray(res.data) ? res.data : [];
        } catch (e) { handleError(e); }
    },

    /** Diyetisyen: hastaya yeni ölçüm ekler. */
    addMeasurement: async (patientId, data) => {
        try {
            const res = await axios.post(`${API_URL}/Patients/${patientId}/measurements`, data, cfg());
            return res.data;
        } catch (e) { handleError(e); }
    },

    /**
     * Hasta: kendi profilini getirir (ölçümler dahil).
     * Returns: { measurements: [...], latestMeasurement, ... }
     */
    getMyProfile: async () => {
        try {
            const res = await axios.get(`${API_URL}/Patients/profile`, cfg());
            return res.data;
        } catch (e) { handleError(e); }
    },
};
