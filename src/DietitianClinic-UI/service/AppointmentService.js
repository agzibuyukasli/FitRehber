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

/** Axios hatasını kullanıcı dostu bir nesneye dönüştürür ve fırlatır. */
const handleError = (error) => {
    const status  = error?.response?.status;
    const message = error?.response?.data?.message || error?.response?.data?.title || error?.message;

    if (status === 401) throw { type: 'auth',       message: 'Oturum süresi doldu. Lütfen tekrar giriş yapın.' };
    if (status === 403) throw { type: 'forbidden',  message: 'Bu işlem için yetkiniz yok.' };
    if (status === 400) throw { type: 'validation', message: message || 'Geçersiz veri.' };
    if (status === 404) throw { type: 'notfound',   message: 'Kayıt bulunamadı.' };
    if (error?.code === 'ECONNREFUSED' || error?.code === 'ERR_NETWORK' || error?.code === 'ECONNRESET') {
        throw { type: 'network', message: `Backend bağlantısı kurulamadı (${API_URL})` };
    }
    throw { type: 'server', message: message || 'Sunucu hatası. Lütfen tekrar deneyin.' };
};

const cfg = (extra = {}) => ({
    headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
    timeout: 10_000,
    ...extra,
});

export const AppointmentService = {
    /** Admin veya Diyetisyen: tüm/kendi randevularını getirir. */
    getAppointments: async () => {
        try {
            const res = await axios.get(`${API_URL}/Appointments`, cfg());
            return Array.isArray(res.data) ? res.data : [];
        } catch (e) { handleError(e); }
    },

    /** Giriş yapan kullanıcının randevularını getirir (Diyetisyen veya Hasta). */
    getMyAppointments: async () => {
        try {
            const res = await axios.get(`${API_URL}/Appointments/my-appointments`, cfg());
            return Array.isArray(res.data) ? res.data : [];
        } catch (e) { handleError(e); }
    },

    /**
     * Diyetisyen tarafından randevu oluşturur.
     * @param {{ patientId, appointmentDate (ISO), durationInMinutes, status, reason }} data
     */
    createAppointment: async (data) => {
        try {
            const res = await axios.post(`${API_URL}/Appointments`, data, cfg());
            return res.data;
        } catch (e) { handleError(e); }
    },

    /**
     * Hasta tarafından randevu talebi gönderir.
     * @param {{ appointmentDate (ISO), durationInMinutes, reason }} data
     */
    requestAppointment: async (data) => {
        try {
            const res = await axios.post(`${API_URL}/Appointments/request`, data, cfg());
            return res.data;
        } catch (e) { handleError(e); }
    },

    /** Talep edilen randevuyu onaylar (Diyetisyen). */
    approveAppointment: async (id) => {
        try {
            const res = await axios.put(`${API_URL}/Appointments/${id}/approve`, {}, cfg());
            return res.data;
        } catch (e) { handleError(e); }
    },

    /** Randevuyu günceller (Diyetisyen). */
    updateAppointment: async (id, data) => {
        try {
            const res = await axios.put(`${API_URL}/Appointments/${id}`, data, cfg());
            return res.data;
        } catch (e) { handleError(e); }
    },

    /** Randevuyu siler/iptal eder. */
    deleteAppointment: async (id) => {
        try {
            await axios.delete(`${API_URL}/Appointments/${id}`, cfg());
        } catch (e) { handleError(e); }
    },
};
