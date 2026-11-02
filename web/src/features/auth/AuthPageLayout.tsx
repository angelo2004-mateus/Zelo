import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { ArrowLeft, Mail } from 'lucide-react';
import { AuthLayout } from '@/components/layout/AuthLayout';
export function AuthPageLayout({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <AuthLayout>
      <Link className="back-link" to="/login">
        <ArrowLeft size={16} /> Voltar ao login
      </Link>
      <div className="welcome-icon">
        <Mail size={23} />
      </div>
      <h2>{title}</h2>
      <p className="form-description">{description}</p>
      {children}
    </AuthLayout>
  );
}
