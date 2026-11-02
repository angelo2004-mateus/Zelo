import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { ApiError, errorMessage, SESSION_EXPIRED_EVENT } from '@/lib/http/client';
import { authService } from './auth.service';
import type { Session } from './models';
interface AuthContextValue {
  session: Session | null;
  loading: boolean;
  sessionError: string;
  recoveryCodes: string[];
  reload: () => Promise<Session | null>;
  logout: () => Promise<void>;
  clearSession: () => void;
  showRecoveryCodes: (codes: string[]) => void;
  clearRecoveryCodes: () => void;
}
const AuthContext = createContext<AuthContextValue | null>(null);
export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(true);
  const [sessionError, setSessionError] = useState('');
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const revision = useRef(0);
  const pending = useRef<Promise<Session | null> | null>(null);
  const reload = useCallback(async () => {
    if (pending.current) {
      return pending.current;
    }
    const version = revision.current;
    const operation = authService
      .session()
      .then((value) => {
        if (version !== revision.current) {
          return null;
        }
        setSession(value);
        setSessionError('');
        return value;
      })
      .catch((error) => {
        if (version !== revision.current) {
          return null;
        }
        if (error instanceof ApiError && error.status === 401) {
          setSession(null);
          setSessionError('');
          return null;
        }
        setSessionError(errorMessage(error));
        throw error;
      })
      .finally(() => {
        setLoading(false);
        if (pending.current === operation) {
          pending.current = null;
        }
      });
    pending.current = operation;
    return operation;
  }, []);
  const clearSession = useCallback(() => {
    revision.current += 1;
    pending.current = null;
    setSession(null);
    setSessionError('');
  }, []);
  useEffect(() => {
    void reload().catch(() => {});
    function checkSession() {
      void reload().catch(() => {});
    }
    window.addEventListener(SESSION_EXPIRED_EVENT, clearSession);
    window.addEventListener('focus', checkSession);
    return () => {
      window.removeEventListener(SESSION_EXPIRED_EVENT, clearSession);
      window.removeEventListener('focus', checkSession);
    };
  }, [reload, clearSession]);
  async function logout() {
    await authService.logout();
    clearSession();
    setRecoveryCodes([]);
  }
  function showRecoveryCodes(codes: string[]) {
    setRecoveryCodes(codes);
    clearSession();
  }
  const clearRecoveryCodes = useCallback(() => {
    setRecoveryCodes([]);
  }, []);
  return (
    <AuthContext.Provider
      value={{
        session,
        loading,
        sessionError,
        recoveryCodes,
        reload,
        logout,
        clearSession,
        showRecoveryCodes,
        clearRecoveryCodes,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('AuthProvider is required');
  }
  return context;
}
