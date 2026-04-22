import * as signalR from '@microsoft/signalr';

const HUB_URL = `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'}/hubs/chat`;

const getToken = () => {
    if (typeof window === 'undefined') return '';
    try {
        const user = JSON.parse(localStorage.getItem('user') || 'null');
        return user?.token || '';
    } catch { return ''; }
};

// Singleton bağlantı nesnesi
let _connection = null;

const buildConnection = () =>
    new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL, {
            accessTokenFactory: () => Promise.resolve(getToken()),
            // withCredentials:false → negotiate isteğini credentials:'include' olmadan gönderir
            // Bu sayede backend AllowAnyOrigin(*) ile bile çalışabilir
            withCredentials: false,
            transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
        })
        .withAutomaticReconnect([2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.None)
        .build();

export const SignalRService = {

    /** Bağlantıyı başlatır (idempotent). */
    connect: async () => {
        if (!getToken()) return null;

        if (_connection &&
            (_connection.state === signalR.HubConnectionState.Connected ||
             _connection.state === signalR.HubConnectionState.Connecting)) {
            return _connection;
        }

        _connection = buildConnection();

        // Bağlantı kesilince unhandledRejection önlemek için onclose handler
        _connection.onclose(() => { /* sessizce geç */ });

        try {
            await _connection.start();
        } catch {
            _connection = null;
        }
        return _connection;
    },

    /** Bağlantıyı keser. */
    disconnect: async () => {
        if (_connection) {
            await _connection.stop().catch(() => {});
            _connection = null;
        }
    },

    /** Mevcut bağlantı nesnesini döner (null olabilir). */
    getConnection: () => _connection,

    /** Bağlı mı? */
    isConnected: () => _connection?.state === signalR.HubConnectionState.Connected,

    // ── Olay dinleyicileri ────────────────────────────────────────────────────

    on:  (event, cb) => _connection?.on(event, cb),
    off: (event, cb) => _connection?.off(event, cb),

    onReceiveMessage:      (cb) => _connection?.on('ReceiveMessage', cb),
    offReceiveMessage:     (cb) => _connection?.off('ReceiveMessage', cb),
    onReceiveNotification: (cb) => _connection?.on('ReceiveNotification', cb),
    offReceiveNotification:(cb) => _connection?.off('ReceiveNotification', cb),
    onUserTyping:          (cb) => _connection?.on('UserTyping', cb),
    offUserTyping:         (cb) => _connection?.off('UserTyping', cb),

    // ── Hub metodları (client → server) ──────────────────────────────────────

    /** Karşı tarafa "yazıyor" sinyali gönderir. */
    sendTyping: async (receiverId) => {
        if (SignalRService.isConnected()) {
            try { await _connection.invoke('SendTyping', receiverId); } catch { /* yoksay */ }
        }
    },
};
