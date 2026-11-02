import type { NextApiRequest, NextApiResponse } from 'next';
import type { User } from '@/features/auth/models';
import type { BackendAuthResult } from './BackendAuthResult';
function serialize(name: string, value: string, maxAge: number) {
  return `${name}=${encodeURIComponent(value)}; Path=/; HttpOnly; SameSite=Lax; Max-Age=${maxAge}${process.env.NODE_ENV === 'production' ? '; Secure' : ''}`;
}
function lifetime(value: string | undefined, fallback: number) {
  const expiry = value ? Date.parse(value) : NaN;
  return Number.isFinite(expiry) ? Math.max(0, Math.floor((expiry - Date.now()) / 1000)) : fallback;
}
export function clearAuthCookies(res: NextApiResponse) {
  res.setHeader(
    'Set-Cookie',
    ['zelo_access', 'zelo_refresh', 'zelo_profile'].map((name) => serialize(name, '', 0)),
  );
}
export function readProfile(req: NextApiRequest): User | null {
  try {
    const data = JSON.parse(req.cookies.zelo_profile ?? 'null');
    const user = data?.user ?? data;
    if (
      typeof user?.id === 'string' &&
      typeof user.name === 'string' &&
      typeof user.email === 'string'
    ) {
      return user;
    }
    return null;
  } catch {
    return null;
  }
}
export function saveAuthCookies(
  res: NextApiResponse,
  data: BackendAuthResult,
  previousUser?: User | null,
) {
  if (!data.accessToken || !data.refreshToken) {
    throw new Error('Missing backend session tokens');
  }
  const user = data.user ?? previousUser;
  if (!user) {
    throw new Error('Missing session profile');
  }
  const refreshLifetime = lifetime(data.refreshExpirationTime, 30 * 24 * 60 * 60);
  res.setHeader('Set-Cookie', [
    serialize('zelo_access', data.accessToken, lifetime(data.expirationTime, 15 * 60)),
    serialize('zelo_refresh', data.refreshToken, refreshLifetime),
    serialize('zelo_profile', JSON.stringify(user), refreshLifetime),
  ]);
}
