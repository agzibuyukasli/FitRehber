import * as Sentry from '@sentry/nextjs';

const isProd = process.env.NODE_ENV === 'production';

Sentry.init({
    dsn: process.env.NEXT_PUBLIC_SENTRY_DSN,
    tracesSampleRate: 1.0,

    // Session replay yalnızca production'da aktif — dev'de sayfa döngüsüne giriyor
    replaysOnErrorSampleRate: isProd ? 1.0 : 0,
    replaysSessionSampleRate: isProd ? 0.1 : 0,
    integrations: isProd ? [Sentry.replayIntegration()] : [],

    environment: process.env.NODE_ENV,
    enabled: !!process.env.NEXT_PUBLIC_SENTRY_DSN,
    debug: false,
});
