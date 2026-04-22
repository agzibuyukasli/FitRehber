import axios from 'axios';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

const getAuthHeader = () => {
    if (typeof window === 'undefined') return {};
    const userJson = localStorage.getItem('user');
    if (!userJson) return {};
    try {
        const user = JSON.parse(userJson);
        return user?.token ? { Authorization: `Bearer ${user.token}` } : {};
    } catch {
        return {};
    }
};

/**
 * Backend camelCase (ASP.NET Core default) veya PascalCase döndürebilir.
 * Her iki durumu da güvenli şekilde karşılar.
 */
const resolveFullName = (p) => {
    if (p.fullName) return p.fullName;
    if (p.FullName) return p.FullName;
    const parts = [p.firstName || p.FirstName, p.lastName || p.LastName].filter(Boolean);
    return parts.length > 0 ? parts.join(' ') : '—';
};

const normalizePatient = (p) => ({
    id:            p.id            ?? p.Id            ?? 0,
    fullName:      resolveFullName(p),
    email:         p.email         ?? p.Email         ?? '',
    phone:         p.phone         ?? p.Phone         ?? p.phoneNumber ?? p.PhoneNumber ?? '',
    city:          p.city          ?? p.City          ?? '',
    age:           p.age           ?? p.Age           ?? undefined,
    isActive:      p.isActive      ?? p.IsActive      ?? undefined,
    patientUserId: p.patientUserId ?? p.PatientUserId ?? null,
});

export const PatientService = {
    getPatientById: async (id) => {
        try {
            const response = await axios.get(`${API_URL}/Patients/${id}`, {
                headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
                timeout: 10_000,
            });
            return normalizePatient(response.data);
        } catch (error) {
            console.error('[PatientService] getPatientById hata:', error?.response?.status, error?.message);
            throw error;
        }
    },

    getAllPatients: async () => {
        try {
            const response = await axios.get(`${API_URL}/Patients`, {
                headers: {
                    'Content-Type': 'application/json',
                    ...getAuthHeader(),
                },
                timeout: 10_000,
            });

            const raw = Array.isArray(response.data) ? response.data : [];
            return raw.map(normalizePatient);

        } catch (error) {
            const status   = error?.response?.status;
            const errCode  = error?.code;

            if (status === 401) {
                console.warn('[PatientService] 401 – token geçersiz, login sayfasına yönlendiriliyor.');
                if (typeof window !== 'undefined') {
                    localStorage.removeItem('user');
                    window.location.href = '/auth/login';
                }
                return [];
            }

            if (errCode === 'ECONNREFUSED' || errCode === 'ERR_NETWORK' || errCode === 'ECONNRESET') {
                console.error(
                    `[PatientService] Backend'e bağlanılamadı (${errCode}). ` +
                    `Backend çalışıyor mu? → ${API_URL}`
                );
            } else {
                console.error('[PatientService] Hata:', status ?? errCode, error?.message);
            }

            // Bağlanamadığımızda boş liste dön — mock veri YOK
            return [];
        }
    },
};
