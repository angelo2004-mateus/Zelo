import type { NextApiRequest, NextApiResponse } from 'next';
import { clearAuthCookies, readProfile, saveAuthCookies } from './auth.cookies';
import { backendRequest, refreshSession } from './auth.upstream';
const publicActions = new Set([
  'login',
  'register',
  'register-user',
  'forgot-password',
  'reset-password',
  'confirm-email',
  'resend-confirmation',
  'totp/verify',
]);
const protectedActions = new Set([
  'totp/setup',
  'totp/enable',
  'totp/disable',
  'logout',
  'session',
]);
function sameOrigin(req: NextApiRequest) {
  if (req.headers['sec-fetch-site'] === 'cross-site') {
    return false;
  }
  if (!req.headers.origin) {
    return true;
  }
  try {
    return new URL(req.headers.origin).host === req.headers.host;
  } catch {
    return false;
  }
}
export async function authBridge(req: NextApiRequest, res: NextApiResponse) {
  res.setHeader('Cache-Control', 'no-store');
  res.setHeader('Referrer-Policy', 'no-referrer');
  const parts = req.query.path;
  const path = Array.isArray(parts) ? parts.join('/') : '';
  const action = path.slice(5);
  if (!path.startsWith('auth/') || (!publicActions.has(action) && !protectedActions.has(action))) {
    res.status(404).end();
    return;
  }
  const method = action === 'session' ? 'GET' : 'POST';
  if (req.method !== method) {
    res.setHeader('Allow', method);
    res.status(405).end();
    return;
  }
  if (!sameOrigin(req)) {
    res.status(403).end();
    return;
  }
  if (method === 'POST' && !req.headers['content-type']?.startsWith('application/json')) {
    res.status(415).end();
    return;
  }
  try {
    let access = req.cookies.zelo_access;
    let user = readProfile(req);
    if (protectedActions.has(action)) {
      let validation = access ? await backendRequest('auth/me', undefined, access, 'GET') : null;
      if ((!validation || validation.status === 401) && req.cookies.zelo_refresh) {
        const tokens = await refreshSession(req.cookies.zelo_refresh);
        if (tokens) {
          access = tokens.accessToken;
          user = tokens.user ?? user;
          saveAuthCookies(res, tokens, user);
          validation = await backendRequest('auth/me', undefined, access, 'GET');
        }
      }
      if (!validation || validation.status === 401) {
        clearAuthCookies(res);
        res.status(action === 'logout' ? 204 : 401).end();
        return;
      }
      if (!validation.ok) {
        res.status(validation.status).end();
        return;
      }
      if (action === 'session') {
        const identity = await validation.json();
        if (!user || identity.userId !== user.id) {
          clearAuthCookies(res);
          res.status(401).end();
          return;
        }
        const accounts = await backendRequest('accounts', undefined, access, 'GET');
        if (!accounts.ok) {
          res.status(accounts.status).end();
          return;
        }
        res.status(200).json({ user, accounts: await accounts.json() });
        return;
      }
    }
    const result = await backendRequest(path, req.body, access);
    const retryAfter = result.headers.get('Retry-After');
    if (retryAfter) {
      res.setHeader('Retry-After', retryAfter);
    }
    const data = await result.json().catch(() => null);
    if (action === 'logout' && (result.ok || result.status === 401)) {
      clearAuthCookies(res);
      res.status(204).end();
      return;
    }
    if (result.ok && ['totp/enable', 'totp/disable', 'reset-password'].includes(action)) {
      clearAuthCookies(res);
    }
    if (result.ok && data?.accessToken) {
      saveAuthCookies(res, data);
    }
    // Always strip bearer credentials, including malformed or unsuccessful backend responses.
    if (data && typeof data === 'object') {
      delete data.accessToken;
      delete data.refreshToken;
    }
    if (data === null) {
      res.status(result.status).end();
    } else {
      res.status(result.status).json(data);
    }
  } catch {
    res.status(502).json({ title: 'Authentication service unavailable' });
  }
}
