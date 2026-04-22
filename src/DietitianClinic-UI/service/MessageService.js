import axios from 'axios';

const BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

// Özel axios instance
const api = axios.create({ baseURL: BASE, timeout: 10_000 });

const authHeader = () => {
    if (typeof window === 'undefined') return {};
    try {
        const user = JSON.parse(localStorage.getItem('user') || 'null');
        return user?.token ? { Authorization: `Bearer ${user.token}` } : {};
    } catch { return {}; }
};

const cfg = () => ({ headers: { 'Content-Type': 'application/json', ...authHeader() } });

export const MessageService = {
    /** GET /api/Messages/{otherUserId} – konuşma geçmişini getirir */
    getConversation: async (otherUserId) => {
        const res = await api.get(`/Messages/${otherUserId}`, cfg());
        return res.data;
    },

    /** POST /api/Messages – yeni mesaj gönderir */
    sendMessage: async ({ receiverId, content, attachmentUrl = null, attachmentName = null, attachmentType = null }) => {
        const res = await api.post('/Messages', { receiverId, content, attachmentUrl, attachmentName, attachmentType }, cfg());
        return res.data;
    },

    /** GET /api/Messages/unread-count – toplam okunmamış mesaj sayısı */
    getUnreadCount: async () => {
        const res = await api.get('/Messages/unread-count', cfg());
        return res.data?.count ?? 0;
    },

    /** GET /api/Messages/unread-by-sender – gönderici bazlı okunmamış sayılar */
    getUnreadBySender: async () => {
        const res = await api.get('/Messages/unread-by-sender', cfg());
        return res.data;
    },

    /** POST /api/Messages/upload – dosya yükler */
    uploadFile: async (file) => {
        const form = new FormData();
        form.append('file', file);
        const res = await api.post('/Messages/upload', form, { headers: { ...authHeader() }, timeout: 30_000 });
        return res.data;
    },
};
