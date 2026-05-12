const CACHE_NAME = 'fitrehber-v1';

// Uygulama açılışında önbelleğe alınacak temel URL'ler
const PRECACHE_URLS = ['/', '/manifest.json'];

// --- Install: temel kaynakları önbelleğe al ---
self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => cache.addAll(PRECACHE_URLS))
    );
    self.skipWaiting();
});

// --- Activate: eski cache sürümlerini temizle ---
self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(
                keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))
            )
        )
    );
    self.clients.claim();
});

// --- Fetch stratejisi ---
self.addEventListener('fetch', (event) => {
    const { request } = event;
    const url = new URL(request.url);

    // Yalnızca GET ve aynı origin istekleri işle
    if (request.method !== 'GET' || url.origin !== self.location.origin) return;

    // API istekleri → Network-first (önce ağ, başarısızsa cache)
    if (url.pathname.startsWith('/api/')) {
        event.respondWith(
            fetch(request)
                .then((res) => {
                    const clone = res.clone();
                    caches.open(CACHE_NAME).then((c) => c.put(request, clone));
                    return res;
                })
                .catch(() => caches.match(request))
        );
        return;
    }

    // _next/static ve diğer statik kaynaklar → Cache-first
    event.respondWith(
        caches.match(request).then(
            (cached) =>
                cached ||
                fetch(request)
                    .then((res) => {
                        if (res.ok) {
                            const clone = res.clone();
                            caches.open(CACHE_NAME).then((c) => c.put(request, clone));
                        }
                        return res;
                    })
                    .catch(() => cached || new Response('Offline', { status: 503 }))
        )
    );
});
