// sentry.server.config.ts (1 satır), sentry.edge.config.ts (1 satır),
// sentry.client.config.ts (2 koşul: isProd=true dalı) kapsar

const mockInit = jest.fn();
const mockReplayIntegration = jest.fn(() => ({ name: 'replay' }));

jest.mock('@sentry/nextjs', () => ({
    init: mockInit,
    replayIntegration: mockReplayIntegration,
}));

beforeEach(() => {
    mockInit.mockClear();
    mockReplayIntegration.mockClear();
});

describe('sentry.server.config', () => {
    it('calls Sentry.init on import', () => {
        jest.isolateModules(() => {
            require('../sentry.server.config');
            expect(mockInit).toHaveBeenCalledTimes(1);
        });
    });
});

describe('sentry.edge.config', () => {
    it('calls Sentry.init on import', () => {
        jest.isolateModules(() => {
            require('../sentry.edge.config');
            expect(mockInit).toHaveBeenCalledTimes(1);
        });
    });
});

describe('sentry.client.config', () => {
    it('calls Sentry.init in non-production (NODE_ENV=test)', () => {
        jest.isolateModules(() => {
            require('../sentry.client.config');
            expect(mockInit).toHaveBeenCalledTimes(1);
            expect(mockInit).toHaveBeenCalledWith(
                expect.objectContaining({
                    replaysOnErrorSampleRate: 0,
                    replaysSessionSampleRate: 0,
                })
            );
        });
    });

    it('uses production replay settings when NODE_ENV is production', () => {
        jest.isolateModules(() => {
            const saved = process.env.NODE_ENV;
            process.env.NODE_ENV = 'production';
            try {
                require('../sentry.client.config');
                expect(mockInit).toHaveBeenCalledTimes(1);
                expect(mockInit).toHaveBeenCalledWith(
                    expect.objectContaining({
                        replaysOnErrorSampleRate: 1.0,
                        replaysSessionSampleRate: 0.1,
                    })
                );
                expect(mockReplayIntegration).toHaveBeenCalled();
            } finally {
                process.env.NODE_ENV = saved;
            }
        });
    });
});
