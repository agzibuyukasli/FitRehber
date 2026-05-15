const nextJest = require('next/jest');

const createJestConfig = nextJest({ dir: './' });

/** @type {import('jest').Config} */
const customConfig = {
    testEnvironment: 'jest-environment-jsdom',
    setupFilesAfterEnv: ['<rootDir>/jest.setup.js'],
    moduleNameMapper: {
        '^@/(.*)$': '<rootDir>/$1',
    },
    testMatch: ['<rootDir>/__tests__/**/*.{ts,tsx,js}'],
    collectCoverageFrom: [
        'instrumentation.ts',
        'sentry.*.config.ts',
        'next.config.js',
        'app/ServiceWorkerRegistrar.tsx',
        'public/sw.js',
    ],
};

module.exports = createJestConfig(customConfig);
