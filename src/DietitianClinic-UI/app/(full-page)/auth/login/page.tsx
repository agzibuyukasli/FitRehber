'use client';
import { useTranslation } from 'react-i18next';
import { useRouter } from 'next/navigation';
import React, { useContext, useEffect, useRef, useState } from 'react';
import { AuthService } from '@/service/AuthService';
import { Button } from 'primereact/button';
import { Checkbox } from 'primereact/checkbox';
import { InputText } from 'primereact/inputtext';
import { Password } from 'primereact/password';
import { Toast } from 'primereact/toast';
import { LayoutContext } from '../../../../layout/context/layoutcontext';
import { classNames } from 'primereact/utils';

const ROLE_DASHBOARD: Record<string, string> = {
    Admin:      '/',
    Dietitian:  '/dietitian',
    Patient:    '/patient',
};

const LoginPage = () => {
    const { t, i18n }       = useTranslation();
    const [mounted, setMounted]   = useState(false);
    const [email, setEmail]       = useState('');
    const [password, setPassword] = useState('');
    const [checked, setChecked]   = useState(false);
    const [loading, setLoading]   = useState(false);

    const { layoutConfig } = useContext(LayoutContext);
    const router           = useRouter();
    const toast            = useRef<Toast>(null);

    // Hydration guard — çeviriler yalnızca client mount sonrası render edilir
    useEffect(() => { setMounted(true); }, []);

    const handleLogin = async () => {
        if (!email.trim() || !password.trim()) {
            toast.current?.show({
                severity: 'warn', summary: 'Uyarı',
                detail: 'E-posta ve şifre alanları zorunludur.', life: 3000,
            });
            return;
        }
        setLoading(true);
        try {
            const data = await AuthService.login(email, password);
            if (data?.token) {
                const role    = AuthService.getUserRole(data.token);
                const target  = (role && ROLE_DASHBOARD[role]) ?? '/';
                router.push(target);
            } else {
                toast.current?.show({
                    severity: 'error', summary: 'Giriş Başarısız',
                    detail: 'Geçersiz e-posta veya şifre.', life: 4000,
                });
            }
        } catch (err: unknown) {
            const status = (err as { response?: { status?: number } })?.response?.status;
            toast.current?.show({
                severity: 'error',
                summary: 'Giriş Başarısız',
                detail: status === 401
                    ? 'E-posta veya şifre hatalı.'
                    : 'Sunucuya bağlanılamadı. Backend çalışıyor mu?',
                life: 4000,
            });
        } finally {
            setLoading(false);
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') handleLogin();
    };

    const containerClass = classNames(
        'surface-ground flex align-items-center justify-content-center min-h-screen min-w-screen overflow-hidden',
        { 'p-input-filled': layoutConfig.inputStyle === 'filled' }
    );

    return (
        <>
            <Toast ref={toast} position="top-center" />
            <div className={containerClass}>
                <div className="flex flex-column align-items-center justify-content-center">
                    <img
                        src={`/layout/images/logo-${layoutConfig.colorScheme === 'light' ? 'dark' : 'white'}.svg`}
                        alt="FitRehber logo"
                        className="mb-5 w-6rem flex-shrink-0"
                    />
                    <div
                        style={{
                            borderRadius: '56px',
                            padding: '0.3rem',
                            background: 'linear-gradient(180deg, var(--primary-color) 10%, rgba(33,150,243,0) 30%)',
                        }}
                    >
                        <div className="w-full surface-card py-8 px-5 sm:px-8" style={{ borderRadius: '53px' }}>
                            <div className="text-center mb-5">
                                <img src="/demo/images/login/avatar.png" alt="avatar" height="50" className="mb-3" />
                                <div className="text-900 text-3xl font-medium mb-3">
                                    {mounted ? t('login.welcome') : '\u00A0'}
                                </div>
                                <span className="text-600 font-medium">
                                    {mounted ? t('login.signin') : '\u00A0'}
                                </span>
                            </div>

                            {/* Dil seçimi */}
                            <div className="flex justify-content-center gap-3 mb-5">
                                <Button label="TR" className="p-button-rounded p-button-text" onClick={() => i18n.changeLanguage('tr')} />
                                <Button label="EN" className="p-button-rounded p-button-text" onClick={() => i18n.changeLanguage('en')} />
                            </div>

                            <div onKeyDown={handleKeyDown}>
                                <label htmlFor="email1" className="block text-900 text-xl font-medium mb-2">
                                    {mounted ? t('login.email') : '\u00A0'}
                                </label>
                                <InputText
                                    id="email1"
                                    type="email"
                                    value={email}
                                    onChange={e => setEmail(e.target.value)}
                                    placeholder="Email adresi"
                                    className="w-full md:w-30rem mb-5"
                                    style={{ padding: '1rem' }}
                                />

                                <label htmlFor="password1" className="block text-900 font-medium text-xl mb-2">
                                    {mounted ? t('login.password') : '\u00A0'}
                                </label>
                                <Password
                                    inputId="password1"
                                    value={password}
                                    onChange={e => setPassword(e.target.value)}
                                    placeholder="Şifre"
                                    toggleMask
                                    feedback={false}
                                    className="w-full mb-5"
                                    inputClassName="w-full p-3 md:w-30rem"
                                />

                                <div className="flex align-items-center justify-content-between mb-5 gap-5">
                                    <div className="flex align-items-center">
                                        <Checkbox
                                            inputId="rememberme1"
                                            checked={checked}
                                            onChange={e => setChecked(e.checked ?? false)}
                                            className="mr-2"
                                        />
                                        <label htmlFor="rememberme1">Beni Hatırla</label>
                                    </div>
                                    <a className="font-medium no-underline cursor-pointer" style={{ color: 'var(--primary-color)' }}>
                                        Şifremi Unuttum
                                    </a>
                                </div>

                                <Button
                                    label="Giriş Yap"
                                    loading={loading}
                                    className="w-full p-3 text-xl"
                                    onClick={handleLogin}
                                />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
};

export default LoginPage;
