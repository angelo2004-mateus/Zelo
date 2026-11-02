# Zelo Web

Frontend Next.js + TypeScript com tema azul. As rotas da aplicação ficam em `src/app/routes.tsx`; criar uma tela não exige criar pastas de rota. `pages` contém apenas a entrada do Next e a ponte HTTP para a API.

## Executar

```sh
cd web
cp .env.example .env.local
npm install
npm run dev
```

Abra http://127.0.0.1:3000/login. Para iniciar a API real com PostgreSQL local, execute `npm run dev:api` em outro terminal dentro de `web`. O comando cria o banco dedicado `zelo_dev`, aplica apenas as migrações pendentes e mantém a chave JWT local em `.local/api.json` (ignorado pelo Git). Por padrão, a API **não envia emails por SMTP em desenvolvimento**: grava as mensagens em `.local/mail`. Não altera outros bancos. Também é possível executar a API .NET separadamente na porta 5271 ou configurar `API_BASE_URL` em `.env.local`. Essa variável é exclusiva do servidor. Não há login simulado nem credenciais padrão.

Para testar o fluxo de email numa caixa de entrada virtual, instale [Mailpit](https://mailpit.axllent.org/docs/install/) e execute `npm run dev:mail`. Em outro terminal, execute `npm run dev:api:mailpit` no lugar de `npm run dev:api`. Abra http://127.0.0.1:8025 para ler as mensagens. Essa configuração usa SMTP sem TLS apenas entre processos locais, nas portas 1025 e 8025, e não entrega emails a destinatários reais. Os emails anteriormente gravados em `.local/mail` continuam nessa pasta; apenas os novos aparecem no Mailpit.

Para testar envio real, configure `Auth__EmailDelivery=Smtp`, `Auth__Smtp__Host`, `Auth__Smtp__From` e, se necessários, `Auth__Smtp__Port` (padrão 587), `Auth__Smtp__Username`, `Auth__Smtp__Password` e `Auth__Smtp__Security` (`StartTls` por padrão ou `SslOnConnect`) no ambiente do processo da API antes de executar `npm run dev:api`. Reinicie a API após mudar essas variáveis. Não coloque credenciais no repositório. O frontend apenas chama a API; não envia emails.

## Fluxos disponíveis

| Rota               | Função                                                |
| ------------------ | ----------------------------------------------------- |
| `/register`        | Cadastro com uma nova conta ou somente usuário        |
| `/confirm-email`   | Confirmação por token e reenvio do email              |
| `/login`           | Senha, desafio TOTP ou código de reserva              |
| `/forgot-password` | Solicitar recuperação sem revelar existência do email |
| `/reset-password`  | Token, nova senha e confirmação da senha              |
| `/dashboard`       | Entrada autenticada e contas acessíveis               |
| `/security`        | Configurar ou desativar autenticação em duas etapas   |
| `/recovery-codes`  | Exibição única dos códigos após ativação de 2FA       |

Links de email podem usar `/confirm-email?token=...` e `/reset-password?token=...`. A API atual entrega o token em texto no corpo do email; por isso essas telas também permitem colá-lo manualmente. Em desenvolvimento, procure o email na pasta configurada por `Auth__DevelopmentOutbox` na API (por padrão, `zelo-auth-outbox` no diretório temporário do sistema). O frontend não lê essa pasta nem expõe tokens por HTTP.

Cadastro sem conta usa `register-user`; o usuário pode entrar com uma lista de contas vazia até receber acesso. Aceitar convites e gerenciar permissões são módulos separados, ainda sem telas.

## Estrutura para revisão

- `src/features/auth/`: páginas, schemas Zod, serviço HTTP e provider de sessão.
- `src/features/auth/security/`: configuração TOTP, desativação e códigos de reserva.
- `src/features/auth/models/`: contratos tipados, um tipo por arquivo.
- `src/components/ui/` e `src/components/layout/`: componentes reutilizáveis.
- `src/server/`: ponte HTTP, cookies, requisições à API e renovação da sessão.
- `src/lib/http/`: mensagens de falha e notificação de sessão expirada.
- `src/styles/globals.css`: tema, responsividade e estados visuais.
- `tests/`: testes de formulário, navegação segura e ponte de autenticação.
- `scripts/auth_smoke.py`: integração real com API e banco descartável.

## Sessão e 2FA

Access e refresh tokens ficam em cookies HttpOnly, SameSite=Lax e Secure em produção. A duração acompanha as expirações retornadas pela API. O navegador recebe apenas perfil e lista de contas. A sessão é validada contra `auth/me`; as contas são consultadas novamente no backend, sem armazenar permissões em cookies. Refresh ocorre quando necessário e requisições simultâneas compartilham a renovação dentro do mesmo processo do servidor.

Rotas protegidas distinguem sessão expirada de indisponibilidade e preservam um destino interno permitido para o próximo login. O retorno à janela revalida a sessão. A API continua responsável pela autorização de cada operação.

O QR Code do autenticador é gerado localmente, sem serviço externo. Ativar ou desativar 2FA revoga todas as sessões na API e limpa os cookies locais. Os oito códigos de reserva ficam somente na memória do frontend; podem ser copiados ou baixados e exigem confirmação de armazenamento antes de concluir. Atualizar a página perde essa cópia. O backend não fornece um endpoint de status de 2FA, então a tela oferece ações explícitas sem inventar o estado atual.

Em produção, use HTTPS. Em múltiplas instâncias, a coordenação de refresh e os limites de tentativas precisam ser tratados também no gateway/backend: a coalescência da ponte é local ao processo e o limitador da API vê o endereço do proxy. Não registre senhas, tokens, segredos TOTP ou códigos de reserva.

## Formatação e validação

```sh
npm run format       # Prettier: aplica formatação
npm run lint         # ESLint
npm run typecheck    # TypeScript
npm test             # Vitest
npm run build        # Build de produção
npm run validate     # Todos os checks acima, incluindo format:check
```

Para a integração real, após o build:

```sh
npm run test:auth
```

O teste exige .NET, PostgreSQL local e `createdb`, `psql`, `dropdb`. Respeita `PGHOST`, `PGPORT`, `PGUSER` e `PGPASSWORD`. Cria um banco com nome aleatório, aplica as migrações, inicia API e Next em portas livres e testa cadastro, confirmação, refresh, logout, TOTP, códigos de reserva, recuperação e proteção de origem. Usa somente emails `example.test`, encerra seus processos e remove seu próprio banco ao terminar. Não altera a API nem os dados de desenvolvimento.

## Roteiro manual

1. Cadastre um usuário com uma conta e confirme o token recebido por email.
2. Entre, confira as contas e abra **Segurança da conta**.
3. Informe sua senha, escaneie o QR Code e ative com o código atual do autenticador.
4. Guarde os códigos de reserva, confirme o armazenamento e entre novamente com senha + TOTP.
5. Saia e faça outro login com um código de reserva. A reutilização dele deve falhar.
6. Desative o 2FA com senha + código válido. Sua sessão deve ser encerrada.
7. Solicite recuperação, redefina a senha com o token mais recente e confirme que a antiga falha.
8. Teste token expirado/usado, email duplicado, campos vazios e senhas diferentes.
