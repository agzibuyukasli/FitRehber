'use client';
import React, { useEffect, useState } from 'react';
import AppMenuitem from './AppMenuitem';
import { MenuProvider } from './context/menucontext';
import { AppMenuItem } from '@/types';
import { AuthService } from '../service/AuthService';

// ─── Menü Konfigürasyonları ───────────────────────────────────────────────────
const ADMIN_MENU: AppMenuItem[] = [
    {
        label: 'Yönetim Paneli',
        items: [
            { label: 'Dashboard',        icon: 'pi pi-fw pi-home',         to: '/' },
            { label: 'Hasta Yönetimi',   icon: 'pi pi-fw pi-users',        to: '/patients' },
            { label: 'Randevu Takvimi',  icon: 'pi pi-fw pi-calendar',     to: '/appointments' },
            { label: 'Besin Veritabanı', icon: 'pi pi-fw pi-database',     to: '/nutrition' },
            { label: 'Raporlar',         icon: 'pi pi-fw pi-chart-bar',    to: '/reports' },
            { label: 'Mesajlar',         icon: 'pi pi-fw pi-comments',     to: '/messages' },
            { label: 'Ayarlar',          icon: 'pi pi-fw pi-cog',          to: '/settings' },
        ],
    },
];

const DIETITIAN_MENU: AppMenuItem[] = [
    {
        label: 'Diyetisyen Paneli',
        items: [
            { label: 'Dashboard',        icon: 'pi pi-fw pi-home',          to: '/dietitian' },
            { label: 'Danışanlarım',     icon: 'pi pi-fw pi-users',         to: '/dietitian/patients' },
            { label: 'Diyet Programı',   icon: 'pi pi-fw pi-list',          to: '/dietitian/meal-plans' },
            { label: 'Randevu Takvimi',  icon: 'pi pi-fw pi-calendar',      to: '/dietitian/calendar' },
            { label: 'Mesajlar',         icon: 'pi pi-fw pi-comments',      to: '/messages' },
        ],
    },
];

const PATIENT_MENU: AppMenuItem[] = [
    {
        label: 'Danışan Paneli',
        items: [
            { label: 'Dashboard',    icon: 'pi pi-fw pi-home',       to: '/patient' },
            { label: 'Gelişimim',    icon: 'pi pi-fw pi-chart-line', to: '/patient/progress' },
            { label: 'Diyet Listem', icon: 'pi pi-fw pi-list',       to: '/patient/diet' },
            { label: 'Su & Notlar',  icon: 'pi pi-fw pi-pencil',     to: '/patient/tracking' },
            { label: 'Mesajlar',     icon: 'pi pi-fw pi-comments',   to: '/messages' },
        ],
    },
];

// ─── Bileşen ──────────────────────────────────────────────────────────────────
const AppMenu = () => {
    const [menuModel, setMenuModel] = useState<AppMenuItem[]>([]);

    useEffect(() => {
        const role = AuthService.getRole();
        if (role === 'Dietitian') setMenuModel(DIETITIAN_MENU);
        else if (role === 'Patient') setMenuModel(PATIENT_MENU);
        else setMenuModel(ADMIN_MENU); // Admin veya bilinmeyen
    }, []);

    return (
        <MenuProvider>
            <ul className="layout-menu">
                {menuModel.map((item, i) =>
                    !item?.seperator
                        ? <AppMenuitem item={item} root={true} index={i} key={item.label} />
                        : <li className="menu-separator" key={i} />
                )}
            </ul>
        </MenuProvider>
    );
};

export default AppMenu;
