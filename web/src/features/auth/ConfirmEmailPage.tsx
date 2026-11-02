import { useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Field } from '@/components/ui/Field';
import { Button } from '@/components/ui/Button';
import { Notice } from '@/components/ui/Notice';
import { errorMessage } from '@/lib/http/client';
import { AuthPageLayout } from './AuthPageLayout';
import { tokenSchema, emailSchema } from './auth.schemas';
import { authService } from './auth.service';
export function ConfirmEmailPage() {
  const [params, setParams] = useSearchParams();
  const [done, setDone] = useState(false);
  const [error, setError] = useState('');
  const [resent, setResent] = useState(false);
  const confirmation = useForm<z.infer<typeof tokenSchema>>({
    resolver: zodResolver(tokenSchema),
    defaultValues: { token: params.get('token') ?? '' },
  });
  const resend = useForm<z.infer<typeof emailSchema>>({
    resolver: zodResolver(emailSchema),
    defaultValues: { email: params.get('email') ?? '' },
  });
  async function confirm({ token }: z.infer<typeof tokenSchema>) {
    setError('');
    try {
      await authService.confirm(token);
      setParams({}, { replace: true });
      setDone(true);
    } catch (error) {
      setError(errorMessage(error));
    }
  }
  async function send({ email }: z.infer<typeof emailSchema>) {
    setError('');
    setResent(false);
    try {
      await authService.resend(email);
      setResent(true);
    } catch (error) {
      setError(errorMessage(error));
    }
  }
  return (
    <AuthPageLayout
      title="Confirme seu email."
      description="Conclua a confirmação para acessar sua conta."
    >
      {error ? <Notice>{error}</Notice> : null}
      {done ? (
        <Notice tone="success">
          Email confirmado. <Link to="/login">Entrar na minha conta</Link>
        </Notice>
      ) : (
        <>
          <form noValidate onSubmit={confirmation.handleSubmit(confirm)}>
            <Field
              label="Token de confirmação"
              autoComplete="off"
              spellCheck={false}
              {...confirmation.register('token')}
              error={confirmation.formState.errors.token?.message}
            />
            <p className="field-hint">
              Use o token recebido por email. Cada token funciona uma única vez.
            </p>
            <Button
              busy={confirmation.formState.isSubmitting || resend.formState.isSubmitting}
              type="submit"
            >
              Confirmar email
            </Button>
          </form>
          <section className="form-section">
            <h3>Não recebeu ou o token expirou?</h3>
            {resent ? (
              <Notice tone="success">
                Se seu cadastro precisar de confirmação, enviaremos um novo email. Use o token mais
                recente.
              </Notice>
            ) : null}
            <form noValidate onSubmit={resend.handleSubmit(send)}>
              <Field
                label="Email do cadastro"
                type="email"
                autoComplete="email"
                {...resend.register('email')}
                error={resend.formState.errors.email?.message}
              />
              <Button
                busy={resend.formState.isSubmitting || confirmation.formState.isSubmitting}
                type="submit"
              >
                Reenviar confirmação
              </Button>
            </form>
          </section>
        </>
      )}
    </AuthPageLayout>
  );
}
