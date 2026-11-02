import type { User } from './User';
import type { AccountAccess } from './AccountAccess';
export interface AuthResult {
  succeeded: boolean;
  user?: User;
  accounts?: AccountAccess[];
  requiresTwoFactor: boolean;
  challengeToken?: string;
}
