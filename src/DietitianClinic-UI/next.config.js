/** @type {import('next').NextConfig} */
const nextConfig = {
    // Build sırasında ESLint hatalarını yoksay
    eslint: {
        ignoreDuringBuilds: true,
    },
    // Build sırasında TypeScript hatalarını da yoksay
    typescript: {
        ignoreBuildErrors: true,
    },
    webpack: (config, { isServer }) => {
        if (!isServer) {
            config.resolve.fallback = { ...config.resolve.fallback, fs: false };
        }
        return config;
    },
};

module.exports = nextConfig;
