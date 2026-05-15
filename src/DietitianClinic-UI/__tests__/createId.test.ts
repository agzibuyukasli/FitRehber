// utils/createId.ts — window.crypto.getRandomValues kullanan satırları kapsar

import { createId } from '../utils/createId';

const VALID_CHARS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';

describe('createId()', () => {
    it('returns a 5-character string using only valid characters', () => {
        const id = createId();
        expect(id).toHaveLength(5);
        for (const ch of id) {
            expect(VALID_CHARS).toContain(ch);
        }
    });

    it('calls window.crypto.getRandomValues', () => {
        const spy = jest.spyOn(window.crypto, 'getRandomValues');
        createId();
        expect(spy).toHaveBeenCalledWith(expect.any(Uint8Array));
        spy.mockRestore();
    });

    it('produces different IDs on consecutive calls', () => {
        const ids = new Set(Array.from({ length: 20 }, () => createId()));
        expect(ids.size).toBeGreaterThan(1);
    });
});
