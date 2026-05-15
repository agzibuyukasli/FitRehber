// next.config.js'deki production dalını (withSentryConfig) kapsar

const mockWithSentryConfig = jest.fn((config) => ({ ...config, __wrapped: true }));

jest.mock('@sentry/nextjs', () => ({
    withSentryConfig: mockWithSentryConfig,
}));

describe('next.config.js', () => {
    beforeEach(() => {
        jest.resetModules();
        mockWithSentryConfig.mockClear();
    });

    it('exports plain nextConfig when NODE_ENV is not production', () => {
        process.env.NODE_ENV = 'test';
        const config = require('../next.config.js');
        expect(config.transpilePackages).toBeDefined();
        expect(config.__wrapped).toBeUndefined();
        expect(mockWithSentryConfig).not.toHaveBeenCalled();
    });

    it('wraps config with withSentryConfig when NODE_ENV is production', () => {
        process.env.NODE_ENV = 'production';
        try {
            const config = require('../next.config.js');
            expect(mockWithSentryConfig).toHaveBeenCalledTimes(1);
            expect(config.__wrapped).toBe(true);
        } finally {
            process.env.NODE_ENV = 'test';
        }
    });
});
