import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowRight, LockKeyhole } from 'lucide-react';
import { Field } from '@/components/ui/Field';
import { Button } from '@/components/ui/Button';
import { Notice } from '@/components/ui/Notice';
import { AuthLayout } from '@/components/layout/AuthLayout';
import { errorMessage } from '@/lib/http/client';
import { authService } from './auth.service';
import { useAuth } from './AuthProvider';
import { loginSchema } from './auth.schemas';
import { safeReturnPath } from './auth.navigation';
import { TwoFactorChallengeForm } from './TwoFactorChallengeForm';
export function LoginPage() {
  const {
    register,
    resetField,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof loginSchema>>({ resolver: zodResolver(loginSchema) });
  const [error, setError] = useState('');
  const [challenge, setChallenge] = useState<{ token: string; expires: number } | null>(null);
  const { reload } = useAuth();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  async function complete() {
    const session = await reload();
    if (!session) {
      throw new Error('Não foi possível iniciar sua sessão. Entre novamente.');
    }
    navigate(safeReturnPath(params.get('next')), { replace: true });
  }
  async function submit(data: z.infer<typeof loginSchema>) {
    setError('');
    try {
      const result = await authService.login(data.email, data.password);
      resetField('password');
      if (result.requiresTwoFactor && result.challengeToken) {
        setChallenge({ token: result.challengeToken, expires: result.challengeExpires });
      } else {
        await complete();
      }
    } catch (error) {
      setError(errorMessage(error));
    }
  }
  return (
    <AuthLayout>
      <div className="welcome-icon">
        <LockKeyhole size={23} />
      </div>
      <span className="form-eyebrow">BEM-VINDO AO ZELO</span>
      <h2>{challenge ? 'Uma camada extra de cuidado' : 'Bom ter você de volta.'}</h2>
      <p className="form-description">
        {challenge
          ? 'Use seu autenticador ou um código de reserva para concluir o acesso.'
          : 'Entre na sua conta e acompanhe o que importa.'}
      </p>
      {params.get('expired') ? (
        <Notice tone="info">Sua sessão terminou. Entre novamente para continuar.</Notice>
      ) : null}
      {params.get('security') === 'enabled' ? (
        <Notice tone="success">Autenticador ativado. Entre com sua senha e seu código.</Notice>
      ) : null}
      {params.get('security') === 'disabled' ? (
        <Notice tone="success">Autenticador desativado. Entre novamente com sua senha.</Notice>
      ) : null}
      {error ? (
        <Notice>
          {error}
          <Link to="/confirm-email">Confirmar email ou solicitar outro envio</Link>
        </Notice>
      ) : null}
      {challenge ? (
        <TwoFactorChallengeForm
          token={challenge.token}
          expires={challenge.expires}
          onComplete={complete}
          onBack={() => {
            setChallenge(null);
            setError('');
          }}
        />
      ) : (
        <form onSubmit={handleSubmit(submit)} noValidate>
          <fieldset disabled={isSubmitting}>
            <Field
              label="Email"
              type="email"
              placeholder="voce@empresa.com.br"
              autoComplete="email"
              {...register('email')}
              error={errors.email?.message}
            />
            <Field
              label="Senha"
              type="password"
              placeholder="Digite sua senha"
              autoComplete="current-password"
              {...register('password')}
              error={errors.password?.message}
            />
            <div className="form-options">
              <span>
                <LockKeyhole size={14} /> Acesso seguro
              </span>
              <Link to="/forgot-password">Esqueci minha senha</Link>
            </div>
            <Button type="submit" busy={isSubmitting}>
              Entrar na minha conta <ArrowRight size={18} />
            </Button>
          </fieldset>
        </form>
      )}
      <div className="signup-line">
        Ainda não tem uma conta?{' '}
        <Link to="/register">
          Comece agora <ArrowRight size={14} />
        </Link>
      </div>
      <div className="form-bottom">
        <span className="tiny-dot" /> Uma gestão mais leve começa aqui.
      </div>
    </AuthLayout>
  );
}
