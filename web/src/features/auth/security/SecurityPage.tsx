import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { ArrowLeft, ShieldCheck } from 'lucide-react';
import { useAuth } from '../AuthProvider';
import { TotpSetupForm } from './TotpSetupForm';
import { TotpDisableForm } from './TotpDisableForm';
export function SecurityPage() {
  const [mode, setMode] = useState<'setup' | 'disable'>('setup');
  const { session, showRecoveryCodes, clearSession } = useAuth();
  const navigate = useNavigate();
  function enabled(codes: string[]) {
    showRecoveryCodes(codes);
    navigate('/recovery-codes', { replace: true });
  }
  function disabled() {
    clearSession();
    navigate('/login?security=disabled', { replace: true });
  }
  return (
    <main className="security-shell">
      <Link className="back-link" to="/dashboard">
        <ArrowLeft size={16} /> Voltar à visão geral
      </Link>
      <div className="workspace-card security-card">
        <div className="welcome-icon">
          <ShieldCheck size={24} />
        </div>
        <h1>Segurança da conta</h1>
        <p className="form-description">
          Proteja seu acesso, {session?.user.name}. Ao ativar ou desativar o autenticador, você
          precisará entrar novamente.
        </p>
        <div className="segmented" role="group" aria-label="Configuração de duas etapas">
          <button type="button" aria-pressed={mode === 'setup'} onClick={() => setMode('setup')}>
            Configurar 2FA
          </button>
          <button
            type="button"
            aria-pressed={mode === 'disable'}
            onClick={() => setMode('disable')}
          >
            Desativar 2FA
          </button>
        </div>
        {mode === 'setup' ? (
          <TotpSetupForm onEnabled={enabled} />
        ) : (
          <TotpDisableForm onDisabled={disabled} />
        )}
        <Link className="back-link spaced" to="/forgot-password">
          Quero redefinir minha senha
        </Link>
      </div>
    </main>
  );
}
