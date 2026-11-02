import { LoaderCircle } from 'lucide-react';
import type { ButtonHTMLAttributes } from 'react';
export function Button({
  busy,
  children,
  disabled,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { busy?: boolean }) {
  return (
    <button {...props} className="button" disabled={busy || disabled} aria-busy={busy || undefined}>
      {busy ? <LoaderCircle size={18} className="spin" /> : null}
      {children}
    </button>
  );
}
