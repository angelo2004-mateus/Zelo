import { useState } from 'react';
import Image from 'next/image';
import QRCode from 'qrcode';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Field } from '@/components/ui/Field';
import { Button } from '@/components/ui/Button';
import { Notice } from '@/components/ui/Notice';
import { errorMessage } from '@/lib/http/client';
import { currentPasswordSchema, totpSchema } from '../auth.schemas';
import { authService } from '../auth.service';
import type { TotpSetup } from '../models';
export function TotpSetupForm({ onEnabled }: { onEnabled: (codes: string[]) => void }) {
  const [setup, setSetup] = useState<TotpSetup | null>(null);
  const [qr, setQr] = useState('');
  const [error, setError] = useState('');
  const passwordForm = useForm<z.infer<typeof currentPasswordSchema>>({
    resolver: zodResolver(currentPasswordSchema),
  });
  const codeForm = useForm<z.infer<typeof totpSchema>>({ resolver: zodResolver(totpSchema) });
  async function start({ password }: z.infer<typeof currentPasswordSchema>) {
    setError('');
    try {
      const data = await authService.setupTotp(password);
      const image = await QRCode.toDataURL(data.authenticatorUri, {
        width: 220,
        margin: 2,
        errorCorrectionLevel: 'M',
      });
      setSetup(data);
      setQr(image);
      passwordForm.reset();
    } catch (error) {
      setError(errorMessage(error));
    }
  }
  async function enable({ code }: z.infer<typeof totpSchema>) {
    setError('');
    try {
      const result = await authService.enableTotp(code);
      setSetup(null);
      setQr('');
      onEnabled(result.recoveryCodes);
    } catch (error) {
      setError(errorMessage(error));
    }
  }
  return (
    <>
      {error ? <Notice>{error}</Notice> : null}
      {setup ? (
        <>
          <ol className="setup-steps">
            <li>Abra seu aplicativo autenticador e adicione uma conta.</li>
            <li>Escaneie o QR Code ou digite a chave abaixo.</li>
            <li>Informe o código de 6 dígitos para ativar.</li>
          </ol>
          <div className="qr-panel">
            <Image
              src={qr}
              width={220}
              height={220}
              alt="QR Code para cadastrar Zelo no seu autenticador"
              unoptimized
            />
            <span>Chave para configuração manual</span>
            <code className="secret-key">{setup.secret}</code>
          </div>
          <form noValidate onSubmit={codeForm.handleSubmit(enable)}>
            <Field
              label="Código de 6 dígitos"
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={6}
              {...codeForm.register('code')}
              error={codeForm.formState.errors.code?.message}
            />
            <Button type="submit" busy={codeForm.formState.isSubmitting}>
              Ativar autenticação em duas etapas
            </Button>
            <button
              className="text-button"
              type="button"
              disabled={codeForm.formState.isSubmitting}
              onClick={() => {
                setSetup(null);
                setQr('');
                setError('');
                codeForm.reset();
              }}
            >
              Cancelar configuração
            </button>
          </form>
        </>
      ) : (
        <form noValidate onSubmit={passwordForm.handleSubmit(start)}>
          <Field
            label="Confirme sua senha atual"
            type="password"
            autoComplete="current-password"
            {...passwordForm.register('password')}
            error={passwordForm.formState.errors.password?.message}
          />
          <Button type="submit" busy={passwordForm.formState.isSubmitting}>
            Configurar autenticador
          </Button>
        </form>
      )}
    </>
  );
}
