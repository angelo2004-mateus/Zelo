import { Sprout } from 'lucide-react';
export function Brand() {
  return (
    <a className="brand" href="/login" aria-label="Zelo, início">
      <span className="brand-icon">
        <Sprout size={23} />
      </span>
      zelo<span className="brand-dot">.</span>
    </a>
  );
}
