import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Field } from '@/components/ui/Field';
import { Button } from '@/components/ui/Button';
import { Notice } from '@/components/ui/Notice';
import { errorMessage } from '@/lib/http/client';
import { disableTotpSchema } from '../auth.schemas';
import { authService } from '../auth.service';
export function TotpDisableForm({ onDisabled }: { onDisabled: () => void }) {
  const [error, setError] = useState('');
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof disableTotpSchema>>({ resolver: zodResolver(disableTotpSchema) });
  async function submit(values: z.infer<typeof disableTotpSchema>) {
    setError('');
    try {
      await authService.disableTotp(values.password, values.code);
      onDisabled();
    } catch (error) {
      setError(errorMessage(error));
    }
  }
  return (
    <form noValidate onSubmit={handleSubmit(submit)}>
      {error ? <Notice>{error}</Notice> : null}
      <fieldset disabled={isSubmitting}>
        <Field
          label="Sua senha atual"
          type="password"
          autoComplete="current-password"
          {...register('password')}
          error={errors.password?.message}
        />
        <Field
          label="Código do autenticador ou de reserva"
          autoComplete="one-time-code"
          {...register('code')}
          error={errors.code?.message}
        />
        <p className="field-hint">
          A desativação invalida os códigos de reserva e encerra as sessões abertas.
        </p>
        <Button type="submit" busy={isSubmitting}>
          Desativar e encerrar sessões
        </Button>
      </fieldset>
    </form>
  );
}
