import { describe, expect, it } from 'vitest';
import { safeReturnPath } from '@/features/auth/auth.navigation';
import { registrationSchema, totpSchema } from '@/features/auth/auth.schemas';
describe('safe navigation', () => {
  it.each(['https://evil.example', '//evil.example', '/\\evil.example', '/login', null])(
    'rejects unsafe return path %s',
    (path) => {
      expect(safeReturnPath(path)).toBe('/dashboard');
    },
  );
  it('preserves allowed internal paths', () => {
    expect(safeReturnPath('/security')).toBe('/security');
  });
});
describe('form boundaries', () => {
  it('requires account name only when creating an account', () => {
    const data = {
      name: 'Test',
      email: 'test@example.test',
      password: 'Password-123',
      confirmPassword: 'Password-123',
      accountName: '',
      createAccount: true,
    };
    expect(registrationSchema.safeParse(data).success).toBe(false);
    expect(registrationSchema.safeParse({ ...data, createAccount: false }).success).toBe(true);
  });
  it('requires a six digit code for enrollment', () => {
    expect(totpSchema.safeParse({ code: '12345' }).success).toBe(false);
    expect(totpSchema.safeParse({ code: '123456' }).success).toBe(true);
  });
});
