'use client';
import { useEffect } from 'react';
import './i18n/config.js';

export default function ServiceWorkerRegistrar() {
    useEffect(() => {
        if (!('serviceWorker' in navigator)) return;

        if (process.env.NODE_ENV === 'production') {
            navigator.serviceWorker
                .register('/sw.js')
                .catch((err) => console.error('SW registration failed:', err));
        } else {
            // Dev modunda SW'yi devre dışı bırak — HMR ile çakışır ve sonsuz döngüye girer
            navigator.serviceWorker.getRegistrations().then((registrations) => {
                for (const registration of registrations) {
                    registration.unregister();
                }
            });
        }
    }, []);

    return null;
}
