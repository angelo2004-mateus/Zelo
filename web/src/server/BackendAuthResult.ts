import type { AccountAccess, User } from '@/features/auth/models';
export interface BackendAuthResult {
  accessToken?: string;
  refreshToken?: string;
  expirationTime?: string;
  refreshExpirationTime?: string;
  user?: User;
  accounts?: AccountAccess[];
  succeeded?: boolean;
  requiresTwoFactor?: boolean;
  challengeToken?: string;
}
