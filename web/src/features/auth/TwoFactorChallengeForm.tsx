import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ArrowRight } from 'lucide-react';
import { Field } from '@/components/ui/Field';
import { Button } from '@/components/ui/Button';
import { Notice } from '@/components/ui/Notice';
import { errorMessage } from '@/lib/http/client';
import { challengeSchema } from './auth.schemas';
import { authService } from './auth.service';
export function TwoFactorChallengeForm({
  token,
  expires,
  onComplete,
  onBack,
}: {
  token: string;
  expires: number;
  onComplete: () => Promise<void>;
  onBack: () => void;
}) {
  const [remaining, setRemaining] = useState(() =>
    Math.max(0, Math.ceil((expires - Date.now()) / 1000)),
  );
  const [error, setError] = useState('');
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof challengeSchema>>({ resolver: zodResolver(challengeSchema) });
  useEffect(() => {
    const timer = window.setInterval(
      () => setRemaining(Math.max(0, Math.ceil((expires - Date.now()) / 1000))),
      1000,
    );
    return () => window.clearInterval(timer);
  }, [expires]);
  async function submit({ code }: z.infer<typeof challengeSchema>) {
    setError('');
    try {
      await authService.verify(token, code);
      await onComplete();
    } catch (error) {
      setError(errorMessage(error));
    }
  }
  return (
    <form onSubmit={handleSubmit(submit)} noValidate>
      {error ? <Notice>{error}</Notice> : null}
      {remaining === 0 ? (
        <Notice>Este desafio expirou. Volte ao login para gerar outro.</Notice>
      ) : (
        <p className="field-hint">
          Válido por {Math.floor(remaining / 60)}:{String(remaining % 60).padStart(2, '0')}. Após
          várias tentativas incorretas, entre novamente.
        </p>
      )}
      <Field
        label="Código do autenticador ou de reserva"
        autoComplete="one-time-code"
        autoFocus
        {...register('code')}
        error={errors.code?.message}
      />
      <Button type="submit" busy={isSubmitting} disabled={remaining === 0}>
        Verificar e entrar <ArrowRight size={18} />
      </Button>
      <button className="text-button" onClick={onBack} disabled={isSubmitting} type="button">
        Voltar ao login
      </button>
    </form>
  );
}
