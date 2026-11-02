// @vitest-environment node
import type { NextApiRequest, NextApiResponse } from 'next';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { authBridge } from '@/server/authBridge';
const user = { id: 'user-1', name: 'Auth Test', email: 'test@example.test' };
const auth = {
  succeeded: true,
  user,
  accessToken: 'secret-access',
  refreshToken: 'secret-refresh',
  expirationTime: new Date(Date.now() + 900000).toISOString(),
  refreshExpirationTime: new Date(Date.now() + 86400000).toISOString(),
};
function harness(action: string, options: Partial<NextApiRequest> = {}) {
  const req = {
    query: { path: ['auth', ...action.split('/')] },
    method: action === 'session' ? 'GET' : 'POST',
    cookies: {},
    headers: {
      host: 'localhost:3000',
      origin: 'http://localhost:3000',
      'content-type': 'application/json',
    },
    body: {},
    ...options,
  } as NextApiRequest;
  const headers: Record<string, string | string[]> = {};
  const result = { status: 0, body: null as unknown };
  const res = {
    setHeader(name: string, value: string | string[]) {
      headers[name] = value;
    },
    status(code: number) {
      result.status = code;
      return res;
    },
    json(body: unknown) {
      result.body = body;
    },
    end() {},
  } as unknown as NextApiResponse;
  return { req, res, result, headers };
}
afterEach(() => {
  vi.unstubAllGlobals();
});
describe('authentication bridge', () => {
  it('rejects cross-origin mutations without reaching the API', async () => {
    const fetch = vi.fn();
    vi.stubGlobal('fetch', fetch);
    const test = harness('login', {
      headers: {
        host: 'localhost:3000',
        origin: 'https://evil.example',
        'content-type': 'application/json',
      },
    });
    await authBridge(test.req, test.res);
    expect(test.result.status).toBe(403);
    expect(fetch).not.toHaveBeenCalled();
  });
  it('rejects non-JSON mutations', async () => {
    const test = harness('login', {
      headers: { host: 'localhost:3000', 'content-type': 'text/plain' },
    });
    await authBridge(test.req, test.res);
    expect(test.result.status).toBe(415);
  });
  it('stores tokens in HttpOnly cookies and strips them from JSON', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json(auth)));
    const test = harness('login');
    await authBridge(test.req, test.res);
    expect(test.result.status).toBe(200);
    expect(test.result.body).not.toHaveProperty('accessToken');
    expect(test.result.body).not.toHaveProperty('refreshToken');
    expect(test.headers['Set-Cookie']).toEqual(
      expect.arrayContaining([
        expect.stringContaining('zelo_access=secret-access; Path=/; HttpOnly; SameSite=Lax'),
      ]),
    );
  });
  it('does not expose tokens even when backend returns an error', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json(auth, { status: 400 })));
    const test = harness('login');
    await authBridge(test.req, test.res);
    expect(test.result.body).not.toHaveProperty('refreshToken');
    expect(test.headers['Set-Cookie']).toBeUndefined();
  });
  it('refreshes an expired session and loads current account access', async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(Response.json(auth))
      .mockResolvedValueOnce(Response.json({ userId: user.id }))
      .mockResolvedValueOnce(
        Response.json([{ accountId: 'account-1', accountName: 'Updated', isActive: true }]),
      );
    vi.stubGlobal('fetch', fetch);
    const test = harness('session', {
      cookies: {
        zelo_access: 'expired',
        zelo_refresh: 'refresh-test',
        zelo_profile: JSON.stringify(user),
      },
    });
    await authBridge(test.req, test.res);
    expect(test.result.status).toBe(200);
    expect(test.result.body).toHaveProperty('accounts.0.accountName', 'Updated');
    expect(fetch).toHaveBeenCalledTimes(4);
  });
  it('clears expired cookies on logout even without a valid session', async () => {
    const test = harness('logout');
    await authBridge(test.req, test.res);
    expect(test.result.status).toBe(204);
    expect(test.headers['Set-Cookie']).toEqual(
      expect.arrayContaining([expect.stringContaining('Max-Age=0')]),
    );
  });
  it('clears cookies immediately after 2FA activation while returning recovery codes', async () => {
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValueOnce(Response.json({ userId: user.id }))
        .mockResolvedValueOnce(Response.json({ recoveryCodes: ['once'] })),
    );
    const test = harness('totp/enable', { cookies: { zelo_access: 'access' } });
    await authBridge(test.req, test.res);
    expect(test.result.body).toEqual({ recoveryCodes: ['once'] });
    expect(test.headers['Set-Cookie']).toEqual(
      expect.arrayContaining([expect.stringContaining('Max-Age=0')]),
    );
  });
  it('keeps cookies on a transient backend failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Connection refused')));
    const test = harness('session', { cookies: { zelo_access: 'access' } });
    await authBridge(test.req, test.res);
    expect(test.result.status).toBe(502);
    expect(test.headers['Set-Cookie']).toBeUndefined();
  });
});
