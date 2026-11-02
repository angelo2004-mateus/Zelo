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
import { resetSchema } from './auth.schemas';
import { authService } from './auth.service';
import { useAuth } from './AuthProvider';
export function ResetPasswordPage() {
  const [params, setParams] = useSearchParams();
  const [done, setDone] = useState(false);
  const [error, setError] = useState('');
  const { clearSession } = useAuth();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof resetSchema>>({
    resolver: zodResolver(resetSchema),
    defaultValues: { token: params.get('token') ?? '' },
  });
  async function submit(values: z.infer<typeof resetSchema>) {
    setError('');
    try {
      await authService.reset(values.token, values.password);
      clearSession();
      setParams({}, { replace: true });
      setDone(true);
    } catch (error) {
      setError(errorMessage(error));
    }
  }
  return (
    <AuthPageLayout
      title="Crie sua nova senha."
      description="Use pelo menos 8 caracteres. Após a alteração, entre novamente nos seus dispositivos."
    >
      {error ? (
        <Notice>
          {error}
          <Link to="/forgot-password">Solicitar novas instruções</Link>
        </Notice>
      ) : null}
      {done ? (
        <Notice tone="success">
          Senha atualizada. <Link to="/login">Entrar com a nova senha</Link>
        </Notice>
      ) : (
        <form noValidate onSubmit={handleSubmit(submit)}>
          <fieldset disabled={isSubmitting}>
            <Field
              label="Token de recuperação"
              autoComplete="off"
              spellCheck={false}
              {...register('token')}
              error={errors.token?.message}
            />
            <p className="field-hint">Preenchido pelo link ou copiado do email que você recebeu.</p>
            <Field
              label="Nova senha"
              type="password"
              autoComplete="new-password"
              {...register('password')}
              error={errors.password?.message}
            />
            <Field
              label="Confirmar nova senha"
              type="password"
              autoComplete="new-password"
              {...register('confirmPassword')}
              error={errors.confirmPassword?.message}
            />
            <Button type="submit" busy={isSubmitting}>
              Salvar nova senha
            </Button>
          </fieldset>
        </form>
      )}
    </AuthPageLayout>
  );
}
