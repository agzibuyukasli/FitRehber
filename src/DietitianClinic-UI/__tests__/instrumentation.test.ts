// instrumentation.ts'deki register() fonksiyonunun dallarını kapsar
// NEXT_RUNTIME=nodejs → sentry.server.config import
// NEXT_RUNTIME=edge   → sentry.edge.config import

jest.mock('../sentry.server.config', () => ({}));
jest.mock('../sentry.edge.config', () => ({}));
jest.mock('@sentry/nextjs', () => ({
    init: jest.fn(),
    replayIntegration: jest.fn(() => ({})),
}));

describe('instrumentation register()', () => {
    const originalRuntime = process.env.NEXT_RUNTIME;

    afterEach(() => {
        process.env.NEXT_RUNTIME = originalRuntime;
    });

    it('imports sentry.server.config when NEXT_RUNTIME is nodejs', async () => {
        process.env.NEXT_RUNTIME = 'nodejs';
        const { register } = await import('../instrumentation');
        await expect(register()).resolves.toBeUndefined();
    });

    it('imports sentry.edge.config when NEXT_RUNTIME is edge', async () => {
        process.env.NEXT_RUNTIME = 'edge';
        const { register } = await import('../instrumentation');
        await expect(register()).resolves.toBeUndefined();
    });

    it('does nothing when NEXT_RUNTIME is not set', async () => {
        delete process.env.NEXT_RUNTIME;
        const { register } = await import('../instrumentation');
        await expect(register()).resolves.toBeUndefined();
    });
});
