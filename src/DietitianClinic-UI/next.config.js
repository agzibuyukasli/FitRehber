/** @type {import('next').NextConfig} */
const nextConfig = {
    transpilePackages: ['primereact', 'primeflex', 'primeicons'],
    eslint: {
        ignoreDuringBuilds: true,
    },
    typescript: {
        ignoreBuildErrors: true,
    },
    // instrumentation.ts (Sentry server-side init) için gerekli
    experimental: {
        instrumentationHook: true,
    },
    webpack: (config, { isServer }) => {
        if (!isServer) {
            config.resolve.fallback = { ...config.resolve.fallback, fs: false };
        }
        // prop-types'ın tek bir kopyadan yüklenmesini zorla (react-transition-group çakışmasını giderir)
        config.resolve.alias = {
            ...config.resolve.alias,
            'prop-types': require.resolve('prop-types'),
        };
        return config;
    },
};

// withSentryConfig webpack eklentisi dev modunda HMR döngüsüne giriyor.
// Sadece production build'de aktif olsun.
if (process.env.NODE_ENV === 'production') {
    const { withSentryConfig } = require('@sentry/nextjs');
    module.exports = withSentryConfig(nextConfig, {
        org: process.env.SENTRY_ORG || '',
        project: process.env.SENTRY_PROJECT || '',
        silent: true,
        widenClientFileUpload: true,
        hideSourceMaps: true,
        disableLogger: true,
        authToken: process.env.SENTRY_AUTH_TOKEN || undefined,
    });
} else {
    module.exports = nextConfig;
}
