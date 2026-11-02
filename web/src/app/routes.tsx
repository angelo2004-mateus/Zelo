import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router';
import { useAuth } from '@/features/auth/AuthProvider';
import { LoginPage } from '@/features/auth/LoginPage';
import { RegisterPage } from '@/features/auth/RegisterPage';
import { ForgotPasswordPage } from '@/features/auth/ForgotPasswordPage';
import { ResetPasswordPage } from '@/features/auth/ResetPasswordPage';
import { ConfirmEmailPage } from '@/features/auth/ConfirmEmailPage';
import { SecurityPage } from '@/features/auth/security/SecurityPage';
import { RecoveryCodesPage } from '@/features/auth/security/RecoveryCodesPage';
import { WorkspacePage } from '@/features/workspace/WorkspacePage';
import { Button } from '@/components/ui/Button';
import { Notice } from '@/components/ui/Notice';
export const paths = {
  login: '/login',
  register: '/register',
  forgotPassword: '/forgot-password',
  resetPassword: '/reset-password',
  confirmEmail: '/confirm-email',
  dashboard: '/dashboard',
  security: '/security',
  recoveryCodes: '/recovery-codes',
} as const;
function ProtectedRoute() {
  const { loading, session, sessionError, reload } = useAuth();
  const location = useLocation();
  if (loading) {
    return (
      <div className="loading-screen" role="status">
        Preparando seu espaço…
      </div>
    );
  }
  if (sessionError) {
    return (
      <div className="loading-screen">
        <Notice>{sessionError}</Notice>
        <Button
          onClick={() => {
            void reload().catch(() => {});
          }}
        >
          Tentar novamente
        </Button>
      </div>
    );
  }
  const next = encodeURIComponent(location.pathname + location.search);
  return session ? <Outlet /> : <Navigate to={`${paths.login}?expired=1&next=${next}`} replace />;
}
function GuestRoute() {
  const { session, loading } = useAuth();
  if (loading) {
    return (
      <div className="loading-screen" role="status">
        Preparando seu acesso…
      </div>
    );
  }
  return session ? <Navigate to={paths.dashboard} replace /> : <Outlet />;
}
export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to={paths.dashboard} replace />} />
      <Route element={<GuestRoute />}>
        <Route path={paths.login} element={<LoginPage />} />
        <Route path={paths.register} element={<RegisterPage />} />
      </Route>
      <Route path={paths.forgotPassword} element={<ForgotPasswordPage />} />
      <Route path={paths.resetPassword} element={<ResetPasswordPage />} />
      <Route path={paths.confirmEmail} element={<ConfirmEmailPage />} />
      <Route path={paths.recoveryCodes} element={<RecoveryCodesPage />} />
      <Route element={<ProtectedRoute />}>
        <Route path={paths.dashboard} element={<WorkspacePage />} />
        <Route path={paths.security} element={<SecurityPage />} />
      </Route>
      <Route
        path="*"
        element={
          <div className="loading-screen">
            <h1>Página não encontrada</h1>
            <a href={paths.login}>Voltar ao início</a>
          </div>
        }
      />
    </Routes>
  );
}
