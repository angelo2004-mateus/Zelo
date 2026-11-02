import { request } from '@/lib/http/client';
import type {
  AuthResult,
  Registration,
  RegistrationResult,
  Session,
  TotpSetup,
  RecoveryResult,
} from './models';
export const authService = {
  async login(email: string, password: string) {
    const result = await request<AuthResult>('auth/login', { email, password });
    return { ...result, challengeExpires: Date.now() + 5 * 60 * 1000 };
  },
  verify(challengeToken: string, code: string) {
    return request<AuthResult>('auth/totp/verify', { challengeToken, code });
  },
  session() {
    return request<Session>('auth/session', undefined, 'GET');
  },
  logout() {
    return request<void>('auth/logout', {});
  },
  register(data: Registration) {
    return request<RegistrationResult>(
      data.accountName ? 'auth/register' : 'auth/register-user',
      data,
    );
  },
  recover(email: string) {
    return request<void>('auth/forgot-password', { email });
  },
  reset(token: string, password: string) {
    return request<void>('auth/reset-password', { token, password });
  },
  confirm(token: string) {
    return request<void>('auth/confirm-email', { token });
  },
  resend(email: string) {
    return request<void>('auth/resend-confirmation', { email });
  },
  setupTotp(password: string) {
    return request<TotpSetup>('auth/totp/setup', { password });
  },
  enableTotp(code: string) {
    return request<RecoveryResult>('auth/totp/enable', { code });
  },
  disableTotp(password: string, code: string) {
    return request<void>('auth/totp/disable', { password, code });
  },
};
