import type { BackendAuthResult } from './BackendAuthResult';
export async function backendRequest(
  path: string,
  body?: unknown,
  access?: string,
  method = 'POST',
) {
  const base = (process.env.API_BASE_URL ?? 'http://localhost:5271').replace(/\/$/, '');
  return fetch(`${base}/api/${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(access ? { Authorization: `Bearer ${access}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
    signal: AbortSignal.timeout(15000),
    cache: 'no-store',
  });
}
// Coalesce rotating refresh requests from simultaneous tabs in this server process.
const refreshes = new Map<string, Promise<BackendAuthResult | null>>();
export async function refreshSession(token: string): Promise<BackendAuthResult | null> {
  const pending = refreshes.get(token);
  if (pending) {
    return pending;
  }
  const operation = backendRequest('auth/refresh', { refreshToken: token }).then(
    async (response) => {
      if (response.status === 401) {
        return null;
      }
      if (!response.ok) {
        throw new Error('Refresh service unavailable');
      }
      return response.json() as Promise<BackendAuthResult>;
    },
  );
  refreshes.set(token, operation);
  try {
    return await operation;
  } finally {
    const cleanup = setTimeout(() => refreshes.delete(token), 5000);
    cleanup.unref();
  }
}
