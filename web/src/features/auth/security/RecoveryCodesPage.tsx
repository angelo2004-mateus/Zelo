import { useEffect, useRef, useState } from 'react';
import { Navigate, useNavigate } from 'react-router';
import { Copy, Download } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Notice } from '@/components/ui/Notice';
import { Brand } from '@/components/ui/Brand';
import { useAuth } from '../AuthProvider';
export function RecoveryCodesPage() {
  const { recoveryCodes, clearRecoveryCodes } = useAuth();
  const [saved, setSaved] = useState(false);
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const mounted = useRef(false);
  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
      // Ignore the development StrictMode setup/cleanup cycle; erase on actual exit.
      queueMicrotask(() => {
        if (!mounted.current) {
          clearRecoveryCodes();
        }
      });
    };
  }, [clearRecoveryCodes]);
  if (!recoveryCodes.length) {
    return <Navigate to="/login" replace />;
  }
  async function copy() {
    try {
      await navigator.clipboard.writeText(recoveryCodes.join('\n'));
      setCopied(true);
      setError('');
    } catch {
      setError('Não foi possível copiar. Selecione os códigos ou baixe o arquivo.');
    }
  }
  function download() {
    const blob = new Blob(
      [
        'Zelo — códigos de reserva\nCada código funciona uma única vez. Guarde em um local seguro.\n\n',
        recoveryCodes.join('\n'),
      ],
      { type: 'text/plain;charset=utf-8' },
    );
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = 'zelo-codigos-de-reserva.txt';
    anchor.click();
    URL.revokeObjectURL(url);
  }
  function finish() {
    clearRecoveryCodes();
    navigate('/login?security=enabled', { replace: true });
  }
  return (
    <main className="security-shell">
      <Brand />
      <section className="workspace-card security-card">
        <h1>Guarde seus códigos de reserva.</h1>
        <p className="form-description">
          O 2FA está ativo. Cada código permite um acesso quando você não tiver o autenticador. Eles
          aparecem apenas agora; atualizar ou sair desta página apaga esta cópia.
        </p>
        <div className="recovery-grid">
          {recoveryCodes.map((code) => (
            <code key={code}>{code}</code>
          ))}
        </div>
        {error ? <Notice>{error}</Notice> : null}
        {copied ? (
          <Notice tone="success">Códigos copiados. Guarde em um local seguro.</Notice>
        ) : null}
        <div className="action-row">
          <Button type="button" onClick={copy}>
            <Copy size={16} /> Copiar
          </Button>
          <Button type="button" onClick={download}>
            <Download size={16} /> Baixar arquivo
          </Button>
        </div>
        <label className="checkbox-row">
          <input
            type="checkbox"
            checked={saved}
            onChange={(event) => setSaved(event.target.checked)}
          />{' '}
          Guardei meus códigos em um local seguro
        </label>
        <Button disabled={!saved} onClick={finish}>
          Concluir e entrar novamente
        </Button>
      </section>
    </main>
  );
}
