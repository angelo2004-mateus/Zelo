import type { User } from './User';
import type { AccountAccess } from './AccountAccess';
export interface Session {
  user: User;
  accounts: AccountAccess[];
}
