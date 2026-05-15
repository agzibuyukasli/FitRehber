/** @jest-environment node */
// public/sw.js — install / activate / fetch olay işleyicilerini kapsar

'use strict';

// ── Service worker global context mock ─────────────────────────────────────
const handlers = {};
const mockSelf = {
    addEventListener: jest.fn((event, handler) => { handlers[event] = handler; }),
    skipWaiting: jest.fn(),
    location: { origin: 'http://localhost' },
    clients: { claim: jest.fn() },
};

const mockCacheStore = {};
const buildMockCache = (name) => ({
    addAll: jest.fn().mockResolvedValue(undefined),
    put: jest.fn().mockResolvedValue(undefined),
    match: jest.fn().mockResolvedValue(undefined),
    delete: jest.fn().mockResolvedValue(true),
});

const mockCaches = {
    open: jest.fn((name) => Promise.resolve(buildMockCache(name))),
    keys: jest.fn().mockResolvedValue(['fitrehber-v1', 'old-cache']),
    match: jest.fn().mockResolvedValue(undefined),
    delete: jest.fn().mockResolvedValue(true),
};

global.self = mockSelf;
global.caches = mockCaches;
global.fetch = jest.fn();

// Load the service worker — registers all event handlers
require('../public/sw.js');

// ─────────────────────────────────────────────────────────────────────────
describe('Service Worker — install event', () => {
    it('pre-caches PRECACHE_URLS and calls skipWaiting', async () => {
        const mockCache = buildMockCache('fitrehber-v1');
        mockCaches.open.mockResolvedValueOnce(mockCache);

        const event = { waitUntil: jest.fn() };
        handlers['install'](event);

        expect(event.waitUntil).toHaveBeenCalled();
        await event.waitUntil.mock.calls[0][0];

        expect(mockCache.addAll).toHaveBeenCalledWith(['/', '/manifest.json']);
        expect(mockSelf.skipWaiting).toHaveBeenCalled();
    });
});

describe('Service Worker — activate event', () => {
    it('deletes old caches and calls clients.claim', async () => {
        const event = { waitUntil: jest.fn() };
        handlers['activate'](event);

        expect(event.waitUntil).toHaveBeenCalled();
        await event.waitUntil.mock.calls[0][0];

        // 'old-cache' should be deleted, 'fitrehber-v1' should not
        expect(mockCaches.delete).toHaveBeenCalledWith('old-cache');
        expect(mockCaches.delete).not.toHaveBeenCalledWith('fitrehber-v1');
        expect(mockSelf.clients.claim).toHaveBeenCalled();
    });
});

describe('Service Worker — fetch event', () => {
    const makeRequest = (method, url) => ({
        method,
        url,
        clone: jest.fn(function () { return this; }),
    });

    const makeEvent = (request) => ({
        request,
        respondWith: jest.fn(),
    });

    it('ignores non-GET requests', () => {
        const event = makeEvent(makeRequest('POST', 'http://localhost/api/data'));
        handlers['fetch'](event);
        expect(event.respondWith).not.toHaveBeenCalled();
    });

    it('ignores cross-origin requests', () => {
        const event = makeEvent(makeRequest('GET', 'https://cdn.example.com/asset.js'));
        handlers['fetch'](event);
        expect(event.respondWith).not.toHaveBeenCalled();
    });

    it('uses network-first for /api/ routes (success path)', async () => {
        const mockRes = { clone: jest.fn().mockReturnThis(), ok: true };
        global.fetch.mockResolvedValueOnce(mockRes);
        const mockCache = buildMockCache('fitrehber-v1');
        mockCaches.open.mockResolvedValueOnce(mockCache);

        const event = makeEvent(makeRequest('GET', 'http://localhost/api/patients'));
        handlers['fetch'](event);

        expect(event.respondWith).toHaveBeenCalled();
        const result = await event.respondWith.mock.calls[0][0];
        expect(result).toBe(mockRes);
    });

    it('uses network-first for /api/ routes (network failure falls back to cache)', async () => {
        const cachedRes = { status: 200 };
        global.fetch.mockRejectedValueOnce(new Error('offline'));
        mockCaches.match.mockResolvedValueOnce(cachedRes);

        const event = makeEvent(makeRequest('GET', 'http://localhost/api/patients'));
        handlers['fetch'](event);

        const result = await event.respondWith.mock.calls[0][0];
        expect(result).toBe(cachedRes);
    });

    it('uses cache-first for static assets (cache hit)', async () => {
        const cachedRes = { status: 200 };
        mockCaches.match.mockResolvedValueOnce(cachedRes);

        const event = makeEvent(makeRequest('GET', 'http://localhost/_next/static/chunk.js'));
        handlers['fetch'](event);

        const result = await event.respondWith.mock.calls[0][0];
        expect(result).toBe(cachedRes);
    });

    it('uses cache-first for static assets (cache miss → network success)', async () => {
        mockCaches.match.mockResolvedValueOnce(undefined);
        const netRes = { ok: true, clone: jest.fn().mockReturnThis() };
        global.fetch.mockResolvedValueOnce(netRes);
        const mockCache = buildMockCache('fitrehber-v1');
        mockCaches.open.mockResolvedValueOnce(mockCache);

        const event = makeEvent(makeRequest('GET', 'http://localhost/_next/static/chunk.js'));
        handlers['fetch'](event);

        const result = await event.respondWith.mock.calls[0][0];
        expect(result).toBe(netRes);
    });

    it('returns 503 Offline response when both cache and network fail', async () => {
        mockCaches.match.mockResolvedValueOnce(undefined);
        global.fetch.mockRejectedValueOnce(new Error('offline'));

        const event = makeEvent(makeRequest('GET', 'http://localhost/some-page'));
        handlers['fetch'](event);

        const result = await event.respondWith.mock.calls[0][0];
        expect(result.status).toBe(503);
    });
});
