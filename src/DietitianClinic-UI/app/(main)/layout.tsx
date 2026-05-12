import { Metadata } from 'next';
import { Suspense } from 'react';
import Layout from '../../layout/layout';
import AuthGuard from '../../layout/AuthGuard';

interface AppLayoutProps {
    children: React.ReactNode;
}

export const metadata: Metadata = {
    title: 'FitRehber - Diyetisyen Klinik Yönetim Sistemi',
    description: 'FitRehber; randevu, diyet planı ve hasta takibini tek ekrandan yönetmenizi sağlayan diyetisyen klinik otomasyon sistemi.',
    robots: { index: true, follow: true },
    viewport: { initialScale: 1, width: 'device-width' },
    icons: {
        icon: '/favicon.ico'
    }
};

export default function AppLayout({ children }: AppLayoutProps) {
    return (
        <AuthGuard>
            <Suspense fallback={null}>
                <Layout>{children}</Layout>
            </Suspense>
        </AuthGuard>
    );
}
