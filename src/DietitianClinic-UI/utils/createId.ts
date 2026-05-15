const CHARS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';

export function createId(): string {
    const bytes = new Uint8Array(5);
    window.crypto.getRandomValues(bytes);
    return Array.from(bytes, (b) => CHARS[b % CHARS.length]).join('');
}
