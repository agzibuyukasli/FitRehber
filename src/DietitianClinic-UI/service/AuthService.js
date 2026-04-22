import axios from 'axios';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

// .NET ClaimTypes.Role → JWT'de hem "role" hem uzun URL olabilir
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

/** JWT payload'ını decode eder (imza doğrulaması yapılmaz – sadece client-side okuma). */
const parseJwt = (token) => {
    try {
        const parts = token.split('.');
        if (parts.length !== 3) return null;
        const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
        const padded  = base64 + '=='.slice(0, (4 - (base64.length % 4)) % 4);
        return JSON.parse(atob(padded));
    } catch {
        return null;
    }
};

export const AuthService = {
    /** Login yapar, başarılıysa user objesini localStorage'a kaydeder ve döner. */
    login: async (email, password) => {
        const response = await axios.post(`${API_URL}/Users/login`, { email, password });
        if (response.data?.token) {
            localStorage.setItem('user', JSON.stringify(response.data));
        }
        return response.data;
    },

    logout: () => {
        localStorage.removeItem('user');
    },

    /** localStorage'daki user objesini döner. SSR-safe. */
    getCurrentUser: () => {
        if (typeof window === 'undefined') return null;
        try {
            return JSON.parse(localStorage.getItem('user') || 'null');
        } catch {
            return null;
        }
    },

    /**
     * Token'dan kullanıcı rolünü çıkarır.
     * Backend ClaimTypes.Role kullandığı için .NET short-claim map'i ile "role" key'i tercih edilir,
     * ancak uzun URL form da kontrol edilir.
     * @returns {string|null} "Admin" | "Dietitian" | "Patient" | null
     */
    getUserRole: (token) => {
        const payload = parseJwt(token);
        if (!payload) return null;
        return payload['role'] ?? payload[ROLE_CLAIM] ?? null;
    },

    /** Hem getCurrentUser hem getUserRole'u birleştirir. */
    getRole: () => {
        const user = AuthService.getCurrentUser();
        if (!user?.token) return null;
        return AuthService.getUserRole(user.token);
    },

    /** Token'ın süresi dolmuş mu? */
    isTokenExpired: (token) => {
        const payload = parseJwt(token);
        if (!payload?.exp) return true;
        return Date.now() / 1000 > payload.exp;
    },
};
