// ServiceWorkerRegistrar.tsx — useEffect dallarını kapsar:
// - serviceWorker yok → erken dönüş
// - production → register('/sw.js')
// - dev/test → getRegistrations().unregister()

import React from 'react';
import { render, act } from '@testing-library/react';

jest.mock('../app/i18n/config.js', () => ({}));

import ServiceWorkerRegistrar from '../app/ServiceWorkerRegistrar';

const mockUnregister = jest.fn().mockResolvedValue(true);
const mockGetRegistrations = jest
    .fn()
    .mockResolvedValue([{ unregister: mockUnregister }]);
const mockRegister = jest.fn().mockResolvedValue({});

function addServiceWorkerMock() {
    Object.defineProperty(navigator, 'serviceWorker', {
        value: { register: mockRegister, getRegistrations: mockGetRegistrations },
        writable: true,
        configurable: true,
    });
}

function removeServiceWorkerMock() {
    // Properly remove so 'serviceWorker' in navigator returns false again
    try {
        Reflect.deleteProperty(navigator, 'serviceWorker');
    } catch (_) {}
}

describe('ServiceWorkerRegistrar', () => {
    const savedNodeEnv = process.env.NODE_ENV;

    beforeEach(() => {
        mockRegister.mockClear();
        mockGetRegistrations.mockClear();
        mockUnregister.mockClear();
    });

    afterEach(() => {
        process.env.NODE_ENV = savedNodeEnv;
        removeServiceWorkerMock();
    });

    it('does nothing when serviceWorker is not in navigator', async () => {
        // No addServiceWorkerMock() call — navigator.serviceWorker absent in jsdom
        await act(async () => {
            render(<ServiceWorkerRegistrar />);
        });
        expect(mockRegister).not.toHaveBeenCalled();
        expect(mockGetRegistrations).not.toHaveBeenCalled();
    });

    it('registers the service worker in production', async () => {
        addServiceWorkerMock();
        process.env.NODE_ENV = 'production';
        await act(async () => {
            render(<ServiceWorkerRegistrar />);
        });
        expect(mockRegister).toHaveBeenCalledWith('/sw.js');
    });

    it('unregisters all registrations in non-production', async () => {
        addServiceWorkerMock();
        process.env.NODE_ENV = 'development';
        await act(async () => {
            render(<ServiceWorkerRegistrar />);
        });
        expect(mockGetRegistrations).toHaveBeenCalled();
        expect(mockUnregister).toHaveBeenCalled();
    });

    it('renders no DOM elements (returns null)', async () => {
        let container!: HTMLElement;
        await act(async () => {
            ({ container } = render(<ServiceWorkerRegistrar />));
        });
        expect(container.firstChild).toBeNull();
    });
});
