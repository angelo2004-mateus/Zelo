import { useState } from 'react';
import { Link } from 'react-router';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ArrowRight } from 'lucide-react';
import { Field } from '@/components/ui/Field';
import { Button } from '@/components/ui/Button';
import { Notice } from '@/components/ui/Notice';
import { errorMessage } from '@/lib/http/client';
import { authService } from './auth.service';
import { registrationSchema } from './auth.schemas';
import { AuthPageLayout } from './AuthPageLayout';
export function RegisterPage() {
  const [error, setError] = useState('');
  const [result, setResult] = useState<{ email: string; delivered: boolean } | null>(null);
  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof registrationSchema>>({
    resolver: zodResolver(registrationSchema),
    defaultValues: { createAccount: true, accountName: '' },
  });
  const createAccount = useWatch({ control, name: 'createAccount' });
  async function submit(values: z.infer<typeof registrationSchema>) {
    setError('');
    try {
      const response = await authService.register({
        name: values.name,
        email: values.email,
        password: values.password,
        ...(values.createAccount ? { accountName: values.accountName } : {}),
      });
      setResult({ email: values.email, delivered: response.confirmationEmailSent });
    } catch (error) {
      setError(errorMessage(error));
    }
  }
  return (
    <AuthPageLayout
      title="Seu próximo passo começa aqui."
      description="Crie seu acesso para cuidar das finanças com Zelo."
    >
      {error ? <Notice>{error}</Notice> : null}
      {result ? (
        <>
          <Notice tone={result.delivered ? 'success' : 'info'}>
            {result.delivered
              ? 'Conta criada. Enviamos a confirmação para'
              : 'Seu cadastro foi salvo, mas o email de confirmação não pôde ser enviado para'}{' '}
            <strong>{result.email}</strong>.{' '}
            {result.delivered
              ? 'Confira também a caixa de spam.'
              : 'Solicite um novo envio para concluir seu acesso.'}
          </Notice>
          <Link className="button" to={`/confirm-email?email=${encodeURIComponent(result.email)}`}>
            Confirmar email ou reenviar
          </Link>
        </>
      ) : (
        <form onSubmit={handleSubmit(submit)} noValidate>
          <fieldset disabled={isSubmitting}>
            <Field
              label="Seu nome"
              autoComplete="name"
              {...register('name')}
              error={errors.name?.message}
            />
            <Field
              label="Email"
              type="email"
              autoComplete="email"
              placeholder="voce@empresa.com.br"
              {...register('email')}
              error={errors.email?.message}
            />
            <label className="checkbox-row">
              <input type="checkbox" {...register('createAccount')} /> Criar uma conta para minha
              empresa
            </label>
            <p className="field-hint">Se você recebeu um convite, pode criar somente seu acesso.</p>
            {createAccount ? (
              <Field
                label="Nome da conta / empresa"
                autoComplete="organization"
                {...register('accountName')}
                error={errors.accountName?.message}
              />
            ) : null}
            <Field
              label="Senha"
              type="password"
              autoComplete="new-password"
              placeholder="Pelo menos 8 caracteres"
              {...register('password')}
              error={errors.password?.message}
            />
            <Field
              label="Confirmar senha"
              type="password"
              autoComplete="new-password"
              {...register('confirmPassword')}
              error={errors.confirmPassword?.message}
            />
            <Button type="submit" busy={isSubmitting}>
              Criar meu acesso <ArrowRight size={18} />
            </Button>
          </fieldset>
        </form>
      )}
    </AuthPageLayout>
  );
}
