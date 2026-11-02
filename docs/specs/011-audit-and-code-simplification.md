# Auditoria centralizada e simplificação

Status: **implementada**.

## Decisões

- A sessão NHibernate recebe um interceptor por escopo. Ao inserir qualquer `Entity`, ele preenche `CreationTime`; ao atualizar uma entidade comum, preenche `LastModifiedTime`. Em `AuditedAggregateRoot`, preenche `CreatorId` e o par `LastModifiedTime`/`LastModifierId` com `ICurrentUser` quando houver usuário autenticado. Sem usuário autenticado, não altera esse par para respeitar a restrição do banco. Os valores também são escritos no estado entregue ao NHibernate, para que a gravação corresponda ao objeto.
- Criação inicial de conta e membership durante o cadastro não possui usuário autenticado; esses fluxos continuam informando explicitamente o ID do criador. `JoinTime`, `ExpirationTime`, `EmailConfirmationTime` e outros tempos de negócio não são auditoria.
- Escritas SQL diretas não passam pelo interceptor. Seus próprios comandos devem continuar atualizando `LastModifiedTime` quando aplicável.
- Repositórios e app services deixam de atribuir campos de auditoria. O CRUD genérico elimina `RejectIfNeeded`, a exceção privada de rejeição e os snapshots repetidos de campos que o AutoMapper já ignora. Ele mantém autorização, exigência de tenant, 404 e proteção de soft delete.
- A validação de formato da empresa permanece em um único ponto e é convertida em HTTP 400 por um handler existente de exceções de aplicação. Verificações de segurança do login, convites, permissões e último Owner permanecem, pois impedem acesso indevido e estados inválidos.
- Declarações de classes genéricas, assinaturas e construtores ficam em uma linha quando legíveis, sem quebras artificiais por parâmetro.

## Validação

- Build da solução sem avisos.
- Sete testes unitários aprovados, incluindo verificação do estado NHibernate produzido pelo interceptor.
- Smoke completo em PostgreSQL aprovado: cadastro, autenticação, convite, permissões, 2FA e isolamento entre contas. Consultas diretas ao banco confirmam a auditoria na criação de empresa e na alteração de um membership.
