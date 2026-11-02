import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { ArrowUpRight, Check, ShieldCheck, TrendingUp, Wallet, CircleHelp } from 'lucide-react';
import { Brand } from '@/components/ui/Brand';
export function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <main className="auth-shell">
      <section className="story">
        <Brand />
        <div className="story-content">
          <span className="eyebrow">
            <span /> MAIS CLAREZA. MAIS CONTROLE.
          </span>
          <h1>
            Seu financeiro,
            <br />
            em boas mãos<span>.</span>
          </h1>
          <p>
            Uma visão clara das suas despesas.
            <br />
            Mais espaço para cuidar do seu negócio.
          </p>
          <div className="finance-art" aria-hidden="true">
            <div className="art-orbit" />
            <div className="art-card">
              <div className="art-header">
                <span>
                  <Wallet size={17} /> Visão financeira
                </span>
                <span className="art-badge">Este mês</span>
              </div>
              <div className="art-value">
                Tudo sob controle <ArrowUpRight size={24} />
              </div>
              <div className="art-chart">
                {[29, 45, 37, 65, 54, 82, 100].map((height, i) => (
                  <i key={i} style={{ height: `${height}%` }} />
                ))}
              </div>
              <div className="art-footer">
                <span>Organize. Acompanhe. Evolua.</span>
                <TrendingUp size={16} />
              </div>
            </div>
            <div className="floating-check">
              <span>
                <Check size={18} />
              </span>{' '}
              Sua gestão, mais simples
            </div>
          </div>
          <div className="story-proof">
            <ShieldCheck size={18} />
            <span>Controle para sua empresa. Tranquilidade para você.</span>
          </div>
        </div>
        <div className="story-bottom">
          Feito para empresas brasileiras <span>●</span>
        </div>
      </section>
      <section className="auth-main">
        <div className="top-help">
          <CircleHelp size={16} />
          <Link to="/forgot-password">Precisa de ajuda?</Link>
        </div>
        <div className="mobile-brand">
          <Brand />
        </div>
        <div className="form-container">{children}</div>
        <footer>
          <ShieldCheck size={15} />
          <span>Seu acesso é protegido e seus dados são privados.</span>
        </footer>
      </section>
    </main>
  );
}
