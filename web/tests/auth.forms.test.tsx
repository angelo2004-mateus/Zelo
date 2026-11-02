import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { RegisterPage } from '@/features/auth/RegisterPage';
import { ResetPasswordPage } from '@/features/auth/ResetPasswordPage';
import { RecoveryCodesPage } from '@/features/auth/security/RecoveryCodesPage';
import { authService } from '@/features/auth/auth.service';
const context = vi.hoisted(() => ({
  clearSession: vi.fn(),
  clearRecoveryCodes: vi.fn(),
  recoveryCodes: ['abcd-1234', 'efgh-5678'] as string[],
}));
vi.mock('@/features/auth/AuthProvider', () => ({ useAuth: () => context }));
vi.mock('@/features/auth/auth.service', () => ({
  authService: { register: vi.fn(), reset: vi.fn() },
}));
afterEach(() => {
  vi.clearAllMocks();
});
function renderPage(page: React.ReactNode, route = '/') {
  return render(<MemoryRouter initialEntries={[route]}>{page}</MemoryRouter>);
}
describe('registration', () => {
  it('rejects different passwords before contacting the backend', async () => {
    renderPage(<RegisterPage />);
    fireEvent.change(screen.getByLabelText('Seu nome'), { target: { value: 'Auth Test' } });
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'test@example.test' } });
    fireEvent.change(screen.getByLabelText('Nome da conta / empresa'), {
      target: { value: 'Test Account' },
    });
    fireEvent.change(screen.getByLabelText('Senha', { exact: true }), {
      target: { value: 'Password-1234' },
    });
    fireEvent.change(screen.getByLabelText('Confirmar senha'), {
      target: { value: 'Different-1234' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Criar meu acesso' }));
    expect(await screen.findByText('As senhas precisam ser iguais.')).toBeInTheDocument();
    expect(authService.register).not.toHaveBeenCalled();
  });
  it('allows user-only registration and offers resend when delivery fails', async () => {
    vi.mocked(authService.register).mockResolvedValue({
      succeeded: true,
      confirmationEmailSent: false,
    });
    renderPage(<RegisterPage />);
    await userEvent.click(screen.getByLabelText('Criar uma conta para minha empresa'));
    expect(screen.queryByLabelText('Nome da conta / empresa')).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Seu nome'), { target: { value: 'Auth Test' } });
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'test@example.test' } });
    fireEvent.change(screen.getByLabelText('Senha', { exact: true }), {
      target: { value: 'Password-1234' },
    });
    fireEvent.change(screen.getByLabelText('Confirmar senha'), {
      target: { value: 'Password-1234' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Criar meu acesso' }));
    expect(await screen.findByText(/cadastro foi salvo/)).toBeInTheDocument();
    expect(authService.register).toHaveBeenCalledWith({
      name: 'Auth Test',
      email: 'test@example.test',
      password: 'Password-1234',
    });
    expect(screen.getByRole('link', { name: 'Confirmar email ou reenviar' })).toHaveAttribute(
      'href',
      '/confirm-email?email=test%40example.test',
    );
  });
});
describe('password reset', () => {
  it('reads the email token and clears session after a successful reset', async () => {
    vi.mocked(authService.reset).mockResolvedValue(undefined);
    renderPage(<ResetPasswordPage />, '/reset-password?token=one-use-token');
    expect(screen.getByLabelText('Token de recuperação')).toHaveValue('one-use-token');
    fireEvent.change(screen.getByLabelText('Nova senha', { exact: true }), {
      target: { value: 'New-Password-1234' },
    });
    fireEvent.change(screen.getByLabelText('Confirmar nova senha'), {
      target: { value: 'New-Password-1234' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Salvar nova senha' }));
    expect(await screen.findByText(/Senha atualizada/)).toBeInTheDocument();
    expect(authService.reset).toHaveBeenCalledWith('one-use-token', 'New-Password-1234');
    expect(context.clearSession).toHaveBeenCalledOnce();
  });
  it('offers a new link when the token is expired', async () => {
    vi.mocked(authService.reset).mockRejectedValue(new Error('Token expirado.'));
    renderPage(<ResetPasswordPage />, '/reset-password?token=expired');
    fireEvent.change(screen.getByLabelText('Nova senha', { exact: true }), {
      target: { value: 'New-Password-1234' },
    });
    fireEvent.change(screen.getByLabelText('Confirmar nova senha'), {
      target: { value: 'New-Password-1234' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Salvar nova senha' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Token expirado.');
    expect(screen.getByRole('link', { name: 'Solicitar novas instruções' })).toHaveAttribute(
      'href',
      '/forgot-password',
    );
  });
});
describe('one-time recovery codes', () => {
  it('erases recovery codes when leaving the page', async () => {
    const page = renderPage(<RecoveryCodesPage />);
    page.unmount();
    await waitFor(() => expect(context.clearRecoveryCodes).toHaveBeenCalled());
  });
  it('requires acknowledgement before clearing codes', async () => {
    renderPage(<RecoveryCodesPage />);
    const finish = screen.getByRole('button', { name: 'Concluir e entrar novamente' });
    expect(finish).toBeDisabled();
    expect(screen.getByText('abcd-1234')).toBeInTheDocument();
    await userEvent.click(screen.getByLabelText('Guardei meus códigos em um local seguro'));
    await userEvent.click(finish);
    await waitFor(() => expect(context.clearRecoveryCodes).toHaveBeenCalledOnce());
  });
});
