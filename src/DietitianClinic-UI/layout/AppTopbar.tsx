/* eslint-disable @next/next/no-img-element */
'use client';

import Link from 'next/link';
import { useRouter, usePathname } from 'next/navigation';
import { classNames } from 'primereact/utils';
import React, { forwardRef, useCallback, useContext, useEffect, useImperativeHandle, useRef, useState } from 'react';
import { AppTopbarRef } from '@/types';
import { LayoutContext } from './context/layoutcontext';
import { AuthService } from '../service/AuthService';
import { MessageService } from '../service/MessageService';
import { useSignalR } from './context/signalrcontext';

const ROLE_LABEL: Record<string, string> = {
    Admin:     'Admin',
    Dietitian: 'Diyetisyen',
    Patient:   'Danışan',
};

const AppTopbar = forwardRef<AppTopbarRef>((_props, ref) => {
    const { layoutConfig, layoutState, onMenuToggle, showProfileSidebar } = useContext(LayoutContext);
    const menubuttonRef       = useRef(null);
    const topbarmenuRef       = useRef(null);
    const topbarmenubuttonRef = useRef(null);
    const router              = useRouter();
    const pathname            = usePathname();
    const signalr             = useSignalR();

    const [userName,    setUserName]    = useState('');
    const [role,        setRole]        = useState('');
    const [unreadCount, setUnreadCount] = useState(0);

    // Kullanıcı bilgilerini yükle
    useEffect(() => {
        const user = AuthService.getCurrentUser();
        if (user?.fullName) setUserName(user.fullName);
        const r = AuthService.getRole();
        if (r) setRole(r);
    }, []);

    // Sayfa değişince okunmamış sayısını güncelle
    useEffect(() => {
        const user = AuthService.getCurrentUser();
        if (!user?.token) return;
        if (pathname?.startsWith('/messages')) {
            setUnreadCount(0);
            return;
        }
        MessageService.getUnreadCount()
            .then((count: number) => setUnreadCount(count))
            .catch(() => {/* backend kapalıysa sessizce geç */});
    }, [pathname]);

    // SignalR: yeni mesaj gelince badge artır
    // useCallback ile sabit referans — SignalR handler leak'i önler
    const handleNewMessage = useCallback((msg: any) => {
        const currentUserId = AuthService.getCurrentUser()?.userId ?? 0;
        if (msg.receiverId !== currentUserId) return;
        if (window.location.pathname.startsWith('/messages')) return;
        setUnreadCount(prev => prev + 1);
    }, []);

    useEffect(() => {
        signalr.onMessage(handleNewMessage);
        return () => { signalr.offMessage(handleNewMessage); };
    }, [handleNewMessage]); // signalr değil, sabit callback'e bağlı

    useImperativeHandle(ref, () => ({
        menubutton:       menubuttonRef.current,
        topbarmenu:       topbarmenuRef.current,
        topbarmenubutton: topbarmenubuttonRef.current,
    }));

    const handleLogout = () => {
        AuthService.logout();
        router.push('/auth/login');
    };

    return (
        <div className="layout-topbar">
            <Link href="/" className="layout-topbar-logo">
                <img
                    src={`/layout/images/logo-${layoutConfig.colorScheme !== 'light' ? 'white' : 'dark'}.svg`}
                    width="47.22px" height="35px" alt="FitRehber logo"
                />
                <span>FitRehber</span>
            </Link>

            <button ref={menubuttonRef} type="button" className="p-link layout-menu-button layout-topbar-button" onClick={onMenuToggle}>
                <i className="pi pi-bars" />
            </button>

            <button ref={topbarmenubuttonRef} type="button" className="p-link layout-topbar-menu-button layout-topbar-button" onClick={showProfileSidebar}>
                <i className="pi pi-ellipsis-v" />
            </button>

            <div ref={topbarmenuRef} className={classNames('layout-topbar-menu', { 'layout-topbar-menu-mobile-active': layoutState.profileSidebarVisible })}>

                {/* Kullanıcı adı + rol */}
                {userName && (
                    <div className="flex align-items-center gap-2 mr-2" style={{ lineHeight: 1.2 }}>
                        <div
                            className="flex align-items-center justify-content-center bg-primary border-circle text-white font-bold"
                            style={{ width: '2rem', height: '2rem', fontSize: '0.85rem', flexShrink: 0 }}
                        >
                            {userName.charAt(0).toUpperCase()}
                        </div>
                        <div className="hidden md:block">
                            <div className="text-900 font-medium text-sm">{userName}</div>
                            {role && <div className="text-500 text-xs">{ROLE_LABEL[role] ?? role}</div>}
                        </div>
                    </div>
                )}

                {/* Mesajlar — Link ile navigasyon (router.push yerine güvenilir) */}
                <Link href="/messages" className="p-link layout-topbar-button" title="Mesajlar" style={{ position: 'relative', display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}>
                    <i className="pi pi-comments" />
                    {unreadCount > 0 && (
                        <span style={{
                            position: 'absolute',
                            top: '4px', right: '4px',
                            background: 'var(--red-500)',
                            color: '#fff',
                            borderRadius: '50%',
                            minWidth: '1rem', height: '1rem',
                            fontSize: '0.6rem',
                            display: 'flex', alignItems: 'center', justifyContent: 'center',
                            fontWeight: 700,
                            padding: '0 2px',
                        }}>
                            {unreadCount > 9 ? '9+' : unreadCount}
                        </span>
                    )}
                    <span>Mesajlar</span>
                </Link>

                {/* Çıkış Yap */}
                <button
                    type="button"
                    className="p-link layout-topbar-button"
                    onClick={handleLogout}
                    title="Çıkış Yap"
                >
                    <i className="pi pi-sign-out" />
                    <span>Çıkış Yap</span>
                </button>
            </div>
        </div>
    );
});

AppTopbar.displayName = 'AppTopbar';

export default AppTopbar;
