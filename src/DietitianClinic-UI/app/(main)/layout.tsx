import { Metadata } from 'next';
import Layout from '../../layout/layout';
import AuthGuard from '../../layout/AuthGuard';

interface AppLayoutProps {
    children: React.ReactNode;
}

export const metadata: Metadata = {
    title: 'FitRehber',
    description: 'Diyetisyen Klinik Yönetim Sistemi',
    robots: { index: false, follow: false },
    viewport: { initialScale: 1, width: 'device-width' },
    icons: {
        icon: '/favicon.ico'
    }
};

export default function AppLayout({ children }: AppLayoutProps) {
    return (
        <AuthGuard>
            <Layout>{children}</Layout>
        </AuthGuard>
    );
}
