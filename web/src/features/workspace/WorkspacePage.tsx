import { useState } from 'react';
import { Link } from 'react-router';
import { LayoutDashboard, LogOut, Building2, ShieldCheck } from 'lucide-react';
import { Brand } from '@/components/ui/Brand';
import { Button } from '@/components/ui/Button';
import { useAuth } from '@/features/auth/AuthProvider';
export function WorkspacePage() {
  const { session, logout } = useAuth();
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  async function exit() {
    setBusy(true);
    try {
      await logout();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <main className="workspace">
      <aside>
        <Brand />
        <div className="nav-active">
          <LayoutDashboard size={19} /> Visão geral
        </div>
        <Link className="nav-link" to="/security">
          <ShieldCheck size={19} /> Segurança da conta
        </Link>
        <Button busy={busy} onClick={exit}>
          <LogOut size={17} /> Sair da conta
        </Button>
      </aside>
      <section>
        <span className="eyebrow">SEU ESPAÇO DE GESTÃO</span>
        <h1>Olá, {session?.user.name.split(' ')[0]}.</h1>
        <p className="form-description">Seu acesso está pronto. Vamos cuidar do seu negócio.</p>
        {error ? (
          <div role="alert" className="notice error">
            {error}
          </div>
        ) : null}
        <div className="workspace-card">
          <ShieldCheck size={28} />
          <h2>Você está conectado.</h2>
          <p>A estrutura está preparada para receber os módulos financeiros.</p>
          <div className="account-list">
            {session?.accounts
              .filter((a) => a.isActive)
              .map((account) => (
                <div key={account.accountId}>
                  <Building2 size={20} />
                  <span>{account.accountName}</span>
                  <small>{account.roleName ?? 'Acesso à conta'}</small>
                </div>
              ))}
          </div>
        </div>
      </section>
    </main>
  );
}
