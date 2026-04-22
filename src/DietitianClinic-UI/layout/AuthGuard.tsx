'use client';
import { useEffect } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import { AuthService } from '../service/AuthService';

export default function AuthGuard({ children }: { children: React.ReactNode }) {
    const router   = useRouter();
    const pathname = usePathname();

    useEffect(() => {
        const user = AuthService.getCurrentUser();

        // Token yok → login
        if (!user?.token) {
            router.replace('/auth/login');
            return;
        }

        // Token süresi dolmuş → login
        if (AuthService.isTokenExpired(user.token)) {
            AuthService.logout();
            router.replace('/auth/login');
            return;
        }

        const role = AuthService.getUserRole(user.token);
        if (!role) return;

        // Tüm roller için ortak sayfalar
        const sharedPaths = ['/messages', '/settings', '/reports'];
        if (sharedPaths.some(p => pathname.startsWith(p))) return;

        // Dietitian yalnızca /dietitian/* erişebilir
        if (role === 'Dietitian' && !pathname.startsWith('/dietitian')) {
            router.replace('/dietitian');
            return;
        }

        // Patient yalnızca /patient/* erişebilir
        if (role === 'Patient' && !pathname.startsWith('/patient')) {
            router.replace('/patient');
            return;
        }
    // pathname her değişimde kontrol edilsin; dep-array kasıtlı
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [pathname]);

    return <>{children}</>;
}
