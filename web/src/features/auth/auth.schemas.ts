import { z } from 'zod';
const email = z.string().trim().pipe(z.email('Informe um email válido.'));
const password = z
  .string()
  .min(8, 'Use pelo menos 8 caracteres.')
  .max(128, 'Use até 128 caracteres.');
const confirmPassword = z.string().min(1, 'Confirme sua senha.');
export const loginSchema = z.object({ email, password: z.string().min(1, 'Informe sua senha.') });
export const emailSchema = z.object({ email });
export const registrationSchema = z
  .object({
    name: z.string().trim().min(1, 'Informe seu nome.').max(200, 'Use até 200 caracteres.'),
    email,
    password,
    confirmPassword,
    accountName: z.string().trim().max(200, 'Use até 200 caracteres.'),
    createAccount: z.boolean(),
  })
  .superRefine((data, context) => {
    if (data.password !== data.confirmPassword) {
      context.addIssue({
        code: 'custom',
        path: ['confirmPassword'],
        message: 'As senhas precisam ser iguais.',
      });
    }
    if (data.createAccount && !data.accountName) {
      context.addIssue({
        code: 'custom',
        path: ['accountName'],
        message: 'Informe o nome da conta.',
      });
    }
  });
export const resetSchema = z
  .object({
    token: z.string().trim().min(1, 'Informe o token recebido por email.'),
    password,
    confirmPassword,
  })
  .refine((data) => data.password === data.confirmPassword, {
    path: ['confirmPassword'],
    message: 'As senhas precisam ser iguais.',
  });
export const tokenSchema = z.object({
  token: z.string().trim().min(1, 'Informe o token recebido por email.'),
});
export const totpSchema = z.object({
  code: z
    .string()
    .trim()
    .regex(/^\d{6}$/, 'Informe os 6 dígitos do autenticador.'),
});
export const challengeSchema = z.object({
  code: z.string().trim().min(1, 'Informe um código do autenticador ou de reserva.').max(128),
});
export const currentPasswordSchema = z.object({
  password: z.string().min(1, 'Informe sua senha atual.'),
});
export const disableTotpSchema = currentPasswordSchema.extend({ code: challengeSchema.shape.code });
