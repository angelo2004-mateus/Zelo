import { ApiError } from './ApiError';
export { ApiError } from './ApiError';
export const SESSION_EXPIRED_EVENT = 'zelo:session-expired';
export function errorMessage(error: unknown) {
  return error instanceof Error ? error.message : 'Não foi possível concluir. Tente novamente.';
}
export async function request<T>(path: string, body?: unknown, method = 'POST'): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`/api/${path}`, {
      method,
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(0, 'Não foi possível conectar. Confira sua conexão e tente novamente.');
  }
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    const protectedRequest =
      path === 'auth/session' ||
      ['auth/totp/setup', 'auth/totp/enable', 'auth/totp/disable'].includes(path);
    if (response.status === 401 && protectedRequest) {
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT));
    }
    const retryAfter = Number(response.headers.get('Retry-After')) || undefined;
    const messages: Record<number, string> = {
      400: 'Confira os dados informados. O token ou código pode ter expirado ou já ter sido usado.',
      401: protectedRequest
        ? 'Sua sessão expirou. Entre novamente para continuar.'
        : 'Confira o email, a senha ou o código. Para entrar, seu email também precisa estar confirmado.',
      409: 'Este email já está cadastrado. Entre na conta ou recupere sua senha.',
      429: retryAfter
        ? `Muitas tentativas. Tente novamente em ${retryAfter} segundos.`
        : 'Muitas tentativas. Aguarde um pouco e tente novamente.',
      502: 'O serviço está indisponível. Tente novamente em instantes.',
      503: 'O serviço está indisponível. Tente novamente em instantes.',
    };
    throw new ApiError(
      response.status,
      messages[response.status] ?? 'Não foi possível concluir. Tente novamente.',
      retryAfter,
    );
  }
  return data as T;
}
