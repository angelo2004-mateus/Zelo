import { useState } from 'react';
import { Link } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Field } from '@/components/ui/Field';
import { Button } from '@/components/ui/Button';
import { Notice } from '@/components/ui/Notice';
import { errorMessage } from '@/lib/http/client';
import { AuthPageLayout } from './AuthPageLayout';
import { emailSchema } from './auth.schemas';
import { authService } from './auth.service';
export function ForgotPasswordPage() {
  const [sent, setSent] = useState(false);
  const [error, setError] = useState('');
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof emailSchema>>({ resolver: zodResolver(emailSchema) });
  async function submit({ email }: z.infer<typeof emailSchema>) {
    setError('');
    try {
      await authService.recover(email);
      setSent(true);
    } catch (error) {
      setError(errorMessage(error));
    }
  }
  return (
    <AuthPageLayout
      title="Vamos recuperar seu acesso."
      description="Informe seu email para solicitar as instruções de recuperação."
    >
      {error ? <Notice>{error}</Notice> : null}
      {sent ? (
        <>
          <Notice tone="success">
            Se houver uma conta com esse email, você receberá as instruções. Confira também a caixa
            de spam.
          </Notice>
          <Link className="button" to="/reset-password">
            Já tenho o token de recuperação
          </Link>
          <button className="text-button" onClick={() => setSent(false)}>
            Solicitar novamente
          </button>
        </>
      ) : (
        <form noValidate onSubmit={handleSubmit(submit)}>
          <Field
            label="Email"
            type="email"
            autoComplete="email"
            {...register('email')}
            error={errors.email?.message}
          />
          <Button type="submit" busy={isSubmitting}>
            Enviar instruções
          </Button>
        </form>
      )}
      <Link className="back-link spaced" to="/confirm-email">
        Preciso confirmar meu email
      </Link>
    </AuthPageLayout>
  );
}
