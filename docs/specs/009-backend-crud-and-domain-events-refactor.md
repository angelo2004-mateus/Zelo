# Refatoração do backend: CRUD compartilhado e eventos de domínio

Status: **eventos de domínio e base de CRUD implementados. As assinaturas de CRUD, o contexto da requisição e a composição foram posteriormente simplificados pela spec 010; esta spec registra as decisões originais. Novas rotas e aplicação de soft delete permanecem decisões específicas**.

## Objetivo e limites

Unificar as cinco operações reutilizáveis (`GetAsync`, `GetAllAsync`, `CreateAsync`, `UpdateAsync` e `DeleteAsync`) em bases genéricas para API, aplicação e repositório. Recursos concretos herdam essas bases e acrescentam somente regras próprias. O comportamento HTTP, as permissões atuais, o isolamento entre contas e os fluxos de autenticação devem permanecer equivalentes durante a refatoração.

Esta spec define a arquitetura e a sequência de migração. Ela não cria automaticamente endpoints de edição ou exclusão para recursos que hoje não os oferecem. Cadastro, login, 2FA, convites, troca de role de membro e revogação de acesso continuam comandos específicos; não são CRUD simples. O CRUD genérico será usado por todo recurso **e operação** para os quais o comportamento de negócio for realmente o CRUD descrito aqui.

## Situação anterior à refatoração

- `IRepository<TEntity, TId>` contém somente `AddAsync`; `NHibernateRepository<TEntity, TId>` implementa apenas essa gravação. Busca, listagem e atualização se repetem nos repositórios concretos.
- `CompanyAppService` e `RoleAppService` repetem autorização, transação, conversão de DTO e persistência. `RolesController` e `CompaniesController` repetem leitura do usuário e mapeamento do resultado HTTP.
- `AccountAppService` fornece uma lista de contas **acessíveis**, não uma listagem irrestrita de `Account`. `UserTenantAppService` e `InvitationAppService` executam transições de estado. Essas consultas e comandos não podem ser substituídos cegamente pelo CRUD base.
- `Role` implementa `IAggregateRoot` diretamente hoje, mas será corrigida para entidade comum do contexto de `Account`; seu nome não a torna uma raiz de agregado. `Account` e `Company` herdam `AuditedAggregateRoot`. Hoje `IAggregateRoot`/`AggregateRoot` **não** possuem `DomainEvents` nem `AddEvent`. Não existe infraestrutura de eventos de domínio nem soft delete genérico.
- O backend não usa AutoMapper atualmente. O projeto Atlas consultado usa outra biblioteca de mapeamento; a sintaxe `Map.To<T>()` não é a API nativa do AutoMapper.

## Organização por camada

Não criar `Zelo.Frame` nem outro projeto. Código reutilizável fica em `Shared` **da camada que o possui**; implementações específicas ficam agrupadas por assunto.

```text
Zelo.Domain/
  Shared/Entities/IEntity.cs             contrato atual com Guid Id
  Shared/Entities/IAggregateRoot.cs      passa a incluir eventos
  Shared/Entities/AggregateRoot.cs       implementação base dos eventos
  Shared/Entities/ISoftDeletable.cs
  Shared/Events/IDomainEvent.cs
  Shared/Repositories/IRepository.cs
  Accounts/Events/AccountCreated.cs
  Accounts/IAccountRepository.cs
  Roles/Role.cs                           entidade comum, sem eventos

Zelo.Application/
  Shared/Crud/ICrudAppService.cs
  Shared/Crud/CrudAppService.cs
  Shared/Authorization/AppServiceResult.cs  renomeia o resultado atual, sem duplicá-lo
  Shared/Events/IDomainEventDispatcher.cs
  Shared/Events/IDomainEventHandler.cs
  Shared/Mapping/                         somente auxiliares genéricos
  Mapping/Companies/CompanyMappingProfile.cs
  Accounts/Handlers/AccountCreatedHandler.cs
  Accounts/Services/AccountProvisioningService.cs
  Accounts/Services/AccountSetupService.cs
  Accounts/Services/AccountRoleService.cs
  Contract/ICompanyAppService.cs
  Services/CompanyAppService.cs
  Model/Companies/...                      DTOs específicos

Zelo.Infrastructure/
  Shared/Data/Repositories/Repository.cs
  Shared/Data/NHibernateUnitOfWork.cs
  Data/Repositories/Accounts/AccountRepository.cs  inclui operações de roles da conta
  Data/Repositories/Companies/CompanyRepository.cs
  Data/Mappings/Shared/...                 bases de mapeamento NHibernate
  Data/Mappings/Companies/CompanyMap.cs

Zelo.Web.Api/
  Shared/Controllers/CrudApiController.cs
  Shared/Authorization/AppServiceResultHttpMapper.cs  renomeia o helper HTTP atual
  Shared/DependencyInjection/ServiceCollectionExtensions.cs
  Controllers/CompaniesController.cs
  Controllers/RolesController.cs
```

O arquivo/classe `NHibernateRepository<TEntity, TId>` passa a se chamar `Repository<TEntity, TId>` e fica em `Infrastructure/Shared/Data/Repositories`. O nome público é `Repository`, mas a implementação continua NHibernate. O contrato genérico permanece no domínio, em `Domain/Shared/Repositories`; interfaces específicas, como `ICompanyRepository`, permanecem junto da entidade. `NHibernateUnitOfWork` e outros componentes de persistência que forem realmente genéricos também migram para `Infrastructure/Shared/Data`. Migrations e mapas de entidades continuam específicos em `Data/`. Eventos ficam em `Domain/<Entidade>/Events`; handlers que usam repositórios ficam em `Application/<Entidade>/Handlers`, ambos separados por entidade. A listagem é ilustrativa, não uma ordem de criação de arquivos.

## Registro central de dependências

O `Program.cs` registrava manualmente sessões NHibernate, repositórios, stores, app services, autenticação, email, rate limiter, controllers e infraestrutura HTTP. Os registros **atuais** já foram centralizados em uma classe estática de extensões no composition root da API: `Zelo.Web.Api/Shared/DependencyInjection/ServiceCollectionExtensions.cs`. Ela expõe uma única função pública estática, `AddZeloServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)`, chamada no `Program.cs` como `builder.Services.AddZeloServices(builder.Configuration, builder.Environment)`. Serviços futuros entram nessa mesma função, sem registrar componentes em lugares diferentes do projeto nem usar `BuildServiceProvider()` durante o registro.

Dentro dessa função, o código fica separado com `#region` por responsabilidade: `Configuration and Options`, `Authentication Services`, `Persistence and Unit of Work`, `Repositories and Stores`, `Application Services`, `Secrets and Email Delivery`, `Rate Limiting`, `Authentication and Authorization`, `API Infrastructure`, `Domain Event Handlers` e `Mapping`. As regiões são apenas organização visual de **uma função**, não classes/módulos extras. As interfaces e implementações concretas são registradas explicitamente, inclusive `IDomainEventHandler<AccountCreated> -> AccountCreatedHandler`; o despachante e os handlers que usam `ISession` são `Scoped`. Os profiles do AutoMapper são registrados uma vez. Os objetos de evento (`AccountCreated`) não entram no DI: são criados pelo agregado e enviados ao despachante.

As opções condicionais de desenvolvimento/produção para SMTP, outbox de arquivo, TOTP e JWT mantêm as validações atuais durante a migração. `Program.cs` conserva criação do `WebApplication`, chamada de `AddZeloServices`, construção do app e a ordem de middleware (`UseAuthentication`, `UseAuthorization` etc.); middleware **não** é um registro em `IServiceCollection`. Ao implementar, validar a construção do provider com escopos e conferir que cada app service, repositório e handler necessário resolve sem dependências ausentes. Não fazer varredura automática de assemblies que registre handlers ou serviços acidentalmente.

## Contratos do CRUD e variações de DTO

A forma completa separa DTO de detalhe, DTO de linha de lista, DTO de criação e DTO de edição:

```csharp
CrudApiController<TEntity, TId, TEntityDto, TGetAllDto,
    TCreatingDto, TEditingDto, TIAppService>

ICrudAppService<TEntity, TId, TEntityDto, TGetAllDto,
    TCreatingDto, TEditingDto>
```

As variações são heranças finas que apenas fixam tipos repetidos; **uma única base de sete parâmetros implementa os cinco métodos**:

| Controller | Tipo usado em `GetAll` | Tipo usado em `Create` | Tipo usado em `Update` |
| --- | --- | --- | --- |
| 7 tipos: `TEntity, TId, TEntityDto, TGetAllDto, TCreatingDto, TEditingDto, TIAppService` | `TGetAllDto` | `TCreatingDto` | `TEditingDto` |
| 6 tipos: `TEntity, TId, TEntityDto, TGetAllDto, TEditingDto, TIAppService` | `TGetAllDto` | `TEntityDto` | `TEditingDto` |
| 5 tipos: `TEntity, TId, TEntityDto, TEditingDto, TIAppService` | `TEntityDto` | `TEntityDto` | `TEditingDto` |
| 4 tipos: `TEntity, TId, TEntityDto, TIAppService` | `TEntityDto` | `TEntityDto` | `TEntityDto` |

Assim, fornecer apenas `TEntityDto` faz esse mesmo tipo servir também para criação, listagem e edição. Um recurso que precisa de DTO de criação diferente usa a forma completa. Interfaces e bases de app service oferecem a mesma redução de tipos, sem copiar a lógica dos cinco métodos. A restrição de `TIAppService` aponta para o contrato `ICrudAppService<...>` correspondente, não para uma classe concreta.

Quando um DTO de resposta também servir como entrada, seus campos somente de leitura não podem ser obrigatórios para criação nem editáveis por mapeamento. Se isso não puder ser expresso claramente no contrato HTTP, usar um `TCreatingDto` próprio em vez de forçar o atalho genérico.

O contrato completo expõe, conceitualmente:

```csharp
GetAsync(actorId, tenantId, id, cancellationToken) -> AppServiceResult<TEntityDto>
GetAllAsync(actorId, tenantId, cancellationToken) -> AppServiceResult<IReadOnlyList<TGetAllDto>>
CreateAsync(actorId, tenantId, creatingDto, cancellationToken) -> AppServiceResult<TEntityDto>
UpdateAsync(actorId, tenantId, id, editingDto, cancellationToken) -> AppServiceResult<TEntityDto>
DeleteAsync(actorId, tenantId, id, cancellationToken) -> AppServiceResult<bool>
```

Não criar `CrudOperationContext`: dois valores explícitos (`actorId` autenticado e `tenantId` da rota ou caso de uso) bastam. `TenantId` pode ser nulo somente para um recurso global; é obrigatório para `IMultiTenant`. O serviço nunca confia em `TenantId` recebido no corpo. O app service verifica a autorização **em cada operação** antes da consulta ou escrita; o repositório aplica o filtro do tenant como segunda barreira. Não criar `CrudResult` nem um segundo mapper HTTP. O tipo existente `TenantActionResult<T>` já representa sucesso e os erros usados pelos recursos; ao estendê-lo para recursos globais, **renomear o mesmo tipo** para `AppServiceResult<T>` (ou nome equivalente aprovado) e migrar os usos, sem manter tipos paralelos ou adaptadores. `Delete` pode usar o resultado genérico com `bool`; a API devolve `204` e descarta o valor.

O nome provisório `AppServiceResultMapper` se referia **somente** ao helper HTTP que hoje se chama `TenantActionResultMapper`: ele converte `Success`, `Forbidden`, `Invalid`, `Conflict` e `NotFound` em `IActionResult`/status HTTP. Não é o AutoMapper de DTOs, não acessa banco e não precisa de injeção de dependência. Para deixar isso claro, o nome proposto na migração é `AppServiceResultHttpMapper`, por renomeação do helper existente; `CrudApiController` e os controllers específicos reutilizam o mesmo mapeamento. Se a base do controller tornar esse helper desnecessário sem duplicar lógica, eliminá-lo em vez de conservar duas formas de tradução HTTP.

O controller base só obtém o contexto, chama o app service e traduz o resultado: `GET` retorna `200/404/403`, `POST` retorna `201`, `PUT` retorna `200/404/403/409`, `DELETE` retorna `204/404/403/409`. `CreatedAtAction` é usado apenas onde existe um `GET` por ID acessível. As rotas concretas continuam nos controllers derivados (`api/accounts/{tenantId:guid}/companies`, por exemplo). Restrições de rota de `Guid` ficam no recurso concreto, porque `TId` pode ser outro tipo. Não expor uma rota genérica global sem autenticação e autorização explícitas.

`GetAllAsync` mantém inicialmente o formato de lista atual para não quebrar clientes. Paginação e alteração do envelope de resposta exigem uma spec própria. Os métodos genéricos são `virtual` apenas onde uma especialização concreta precisa alterar comportamento; defaults sem uso não geram métodos públicos extras no recurso.

## Base de aplicação e AutoMapper

`CrudAppService` concentra somente a sequência reutilizável: autorização comum, consulta, mapeamento, chamada ao repositório e tradução do resultado. O controle de transação fica no `IUnitOfWork`. Regras de negócio e fluxos compostos ficam em serviços específicos e handlers; a base não conhece as regras de `Account`, `Role` ou `User`. O fluxo de escrita é:

1. Validar entrada e contexto; carregar a entidade existente em `Update`/`Delete` usando `TenantId` e ignorando excluídas.
2. Dentro da transação, bloquear o recurso/tenant quando a regra exigir concorrência, **reconsultar a autorização atual** e executar validações específicas.
3. Em criação, atribuir `Guid Id` pelo `IEntity` existente (com geração de `Guid` no fluxo compartilhado), atribuir `TenantId` pelo contexto e mapear somente campos editáveis. Em edição, aplicar o DTO sobre a **entidade carregada**, sem trocar ID, escopo ou referências imutáveis.
4. Chamar `OnBeforeCreateOrUpdateAsync`, persistir via repositório e chamar `OnAfterCreateOrUpdateAsync`, todos na mesma transação. Os hooks compartilhados recebem operação (`Create`/`Update`), entidade, `actorId` e `tenantId`; não recebem um `object` com DTO que obrigue cast. Regras dependentes do DTO usam hooks tipados separados (`OnBeforeCreateAsync`/`OnBeforeUpdateAsync`, e equivalentes posteriores) ou uma sobrescrita do caso de uso. Há hooks antes/depois de `Delete`. O comportamento base é no-op. Os hooks **delegam** para serviços específicos; não viram depósitos de regras de negócio.
5. Despachar os eventos de domínio pendentes antes do commit. Erro de handler provoca rollback. Depois do commit, mapear ou reler o DTO de saída se ele depender de dados gravados pelos handlers.

Hooks do app service são pontos de extensão curtos para chamar serviços de domínio/aplicação que tratam normalização, autorização adicional, validação, relação com outras entidades e registro de eventos. Hooks do repositório não assumem essas responsabilidades. Uma operação especializada pode sobrepor o método público inteiro quando a sequência genérica não a representa com clareza. Gestão de roles é um caso de uso próprio de `Account`, não um CRUD genérico de `Role`: `RoleAppService` continua como fachada dos endpoints atuais e delega ao `AccountRoleService`, que centraliza regras de `Owner`, teto de permissões e gravação de `Role`/`RolePermission` por métodos especializados de persistência.

Usar um único `AutoMapper.Profile` em `Application/Mapping/ApplicationMappingProfile.cs`, registrado uma vez no composition root (decisão atualizada na spec 012). `CreateEntityMap<TEntity, TDto>()` registra o mapa direto e reverso. Propriedades ausentes do DTO não precisam de `Ignore`; o helper protege `Id` e `TenantId` quando aparecem no DTO. DTOs de entrada não devem expor campos de auditoria ou eventos. Campos compostos, como `RoleDto.Permissions`, são montados no app service depois de uma consulta específica; o profile não acessa repositórios nem executa N+1 consultas. Validar o perfil com `AssertConfigurationIsValid`.

A API oficial do AutoMapper é `IMapper.Map<TDestination>(source)` ou `IMapper.Map(source, destination)`, não `Map.To<T>()`. Esta spec usa o método oficial. Se a sintaxe `Map.To<T>()` for indispensável, ela será apenas um adaptador fino em `Application/Shared/Mapping`, injetando `IMapper`, sem estado estático ou outra implementação de mapeamento. A seleção da versão/licença deve ocorrer antes da implementação: a documentação do AutoMapper 15 informa exigência de licença ([guia oficial](https://docs.automapper.io/en/stable/15.0-Upgrade-Guide.html)); não registrar uma chave no repositório.

## Repositório, escopo e soft delete

`IRepository<TEntity, TId>` passa a oferecer `GetAsync`, `GetAllAsync`, `CreateAsync`, `UpdateAsync` e `DeleteAsync` para **raízes de agregado** elegíveis ao CRUD. A implementação `Repository<TEntity, TId>` concentra somente operações NHibernate; repositórios concretos adicionam consultas que o contrato genérico não expressa. `Role` e `RolePermission`, como entidades internas do contexto de `Account`, não ganham endpoint nem app service CRUD genérico. O acesso persistente a elas na implantação atual será encapsulado por operações específicas de `IAccountRepository`/`AccountRoleService`; a migração remove escrita direta dessas entidades por repositórios genéricos usados pela aplicação. A escrita de outras entidades internas, como `UserPasswordCredential`, também deve ser encapsulada nas operações do repositório de sua raiz (`User`) ao migrar esse fluxo, sem quebrar o auth existente numa troca de uma vez. Não expor `IQueryable` fora de Infrastructure. **Não criar `IEntityWithId`**: o `IEntity` existente já exige `Guid Id`, e todas as entidades Zelo usam esse identificador. As bases usam `where TEntity : class, IAggregateRoot, new()` quando precisam criar um agregado. `TId` permanece nas assinaturas solicitadas de controller, app service e repositório para representar o tipo da chave nas operações; todas as instanciações deste projeto usam `Guid`. Suporte a outros tipos de ID exigiria uma decisão futura sobre `IEntity`, e não uma segunda interface agora.

Não criar `RepositoryScope`: o repositório recebe `tenantId` explicitamente nos métodos que consultam entidades `IMultiTenant` e **exige** seu valor; métodos de recursos globais não recebem esse argumento. Uma única assinatura com `Guid? tenantId` só é aceitável se a implementação rejeitar sua ausência para `IMultiTenant` antes de consultar. Para `ICompanyScoped`, consultas de uma empresa usam `companyId` explícito em métodos específicos; consultas do tenant todo continuam filtradas por `tenantId`. Busca por ID nunca faz `_session.Get<TEntity>(id)` seguida de retorno sem checar o tenant. Todas as leituras comuns ignoram entidades excluídas. Consultas administrativas que incluem excluídas exigem método específico e autorização própria. A autorização continua no app service: o filtro do repositório não a substitui.

`Repository` expõe hooks protegidos `OnCreateAsync`, `OnUpdateAsync` e `OnDeleteAsync`, chamados uma vez dentro da transação antes da respectiva gravação. Eles cuidam de detalhes persistentes compartilhados, como `CreationTime`/`CreatorId`, `LastModifiedTime`/`LastModifierId`, marcação de exclusão e registro do agregado para coleta de eventos. Overrides concretos chamam a base; regras de negócio e disparo de handlers ficam no domínio/aplicação. Nenhum método de repositório abre sua própria transação quando já existe `IUnitOfWork`.

Soft delete é **opt-in** via `ISoftDeletable` em `Domain/Shared/Entities`, com `IsDeleted`, `DeletionTime` e `DeleterId`. `DeleteAsync` carrega a entidade no escopo, usa `OnDeleteAsync` para marcar esses campos e persiste por `UpdateAsync`; chamadas repetidas não retornam a entidade como ativa. Para entidades sem esse contrato, exclusão física só ocorre em um repositório/operação que a autorize explicitamente; o CRUD base retorna operação não suportada/conflito. Isso evita apagar dados financeiros ou quebrar FKs por acidente. A introdução do contrato em uma tabela exige migration, mapeamento NHibernate, índice/consulta adaptados e regra de retenção específica. Não converter `Status=Suspended`, revogação de `UserTenant` ou expiração de convite em soft delete: são estados de negócio distintos. Role referenciada por membros não pode ser excluída enquanto houver referências, conforme a spec 003.

## Eventos de domínio síncronos

**Todas as raízes de agregado** implementam uma coleção própria de eventos de domínio. `IAggregateRoot` passa a exigir `IReadOnlyCollection<IDomainEvent> DomainEvents`, `AddEvent(IDomainEvent)` e `PullEvents()`, que devolve um snapshot e limpa os eventos pendentes. `AggregateRoot` implementa esses membros com uma lista privada e não mapeada pelo NHibernate; `AuditedAggregateRoot` herda essa implementação. `Role` passa a implementar apenas `IEntity` e `IMultiTenant`, mantendo **somente** `Id`, `TenantId` e `Name`; herdar `Entity` acrescentaria timestamps que foram removidos dela por decisão anterior. `Role` não tem lista de eventos, `AddEvent` ou eventos próprios. Não criar `DomainEventBag` nem um contrato opcional `IHasDomainEvents`. A lista dos agregados não recebe setter público, não aparece em DTOs e não cria colunas. Esses membros de eventos são a exceção explícita à regra atual de entidades somente com propriedades e construtor vazio.

As classes de evento específico (`AccountCreated` etc.) ficam em `Domain/<Entidade>/Events` e carregam somente um snapshot imutável dos IDs/dados necessários. Elas **não** usam o sufixo `DomainEvent`; a pasta `Events` e o contrato compartilhado `IDomainEvent` já deixam o papel explícito. `IDomainEventHandler<TEvent>` e o despachante síncrono ficam em `Application/Shared/Events`. Handlers que coordenam repositórios e casos de uso ficam em `Application/<Entidade>/Handlers`, ao lado dos demais componentes daquela entidade; somente handlers puramente de domínio, sem dependência de aplicação/persistência, podem ficar em `Domain/<Entidade>/Handlers`. O Domain não referencia DI, NHibernate, email ou ASP.NET. Não adicionar fila, broker nem MediatR nesta etapa.

O app service registra o evento no agregado durante o comando. O repositório registra o agregado alterado no `IUnitOfWork`. Antes do commit, o `IUnitOfWork` coleta e despacha os eventos na mesma transação, inclusive eventos produzidos por handlers, com proteção contra ciclos. Cada evento é retirado da coleção para ser processado uma vez naquela execução. Se um handler falha, a transação inteira é revertida e a requisição falha; não há publicação externa garantida ou retry assíncrono. Handlers não enviam email nem chamam serviços externos dentro dessa transação. Confirmação de email e convite continuam com entrega **após** o commit e o comportamento atual de falha/reenvio; confiabilidade de entrega exigiria um outbox numa spec futura.

Fluxo de registro com conta, dividido por responsabilidade:

1. `UserAppService.RegisterAsync` coordena o comando e a transação. Um serviço específico de registro valida os dados e cria `User` + credencial. O caso de uso chama **explicitamente** `AccountProvisioningService` para criar a conta solicitada; assim o contrato de `register` não depende de um handler oculto para devolver `AccountDto`.
2. `AccountProvisioningService` cria `Account` e registra `AccountCreated` no agregado, com `AccountId` e `CreatorUserId`. Antes do commit, `AccountCreatedHandler` chama `AccountSetupService`, que usa as operações de `AccountRoleService` para criar as quatro roles iniciais, suas permissões e o vínculo `UserTenant` como `Owner`, tudo na mesma transação. As regras puras de roles/permissões ficam em uma política de domínio ou serviço de domínio específico, sem NHibernate ou HTTP; os serviços de aplicação coordenam essa política e a persistência. `Role` e `RolePermission` não disparam eventos.
3. `register-user` não cria conta; portanto um `UserCreated` genérico **não** pode significar “criar conta”. Não criar evento de usuário sem uma reação de domínio concreta. A entrega de confirmação de email acontece **após** o commit e permanece explícita no caso de uso, preservando `ConfirmationEmailSent` no resultado.
4. Cadastro de conta por usuário já existente chama `AccountProvisioningService` diretamente e recebe as mesmas roles e vínculo iniciais. O serviço usa a conta como limite de consistência e trava/reconsulta o necessário quando alterar roles; não precisa carregar todas as roles do tenant em memória. Testes garantem que falha ao criar roles/vínculo desfaça também usuário e conta no registro combinado.

O encadeamento `UserCreated` → criar conta → `AccountCreated` é tecnicamente possível, mas neste produto confunde criação global de usuário com a escolha opcional de abrir conta. Esta spec escolhe um comando explícito para a etapa obrigatória e evento para a reação às contas criadas. Eventos são usados para **reações** a uma mudança já validada, não para esconder entradas obrigatórias ou a validação principal. O resultado de uma operação só é sucesso após os handlers transacionais terminarem. Não gerar automaticamente cinco eventos para todo CRUD; cada evento tem nome e necessidade de negócio claros.

## Migração dos recursos existentes

| Recurso | Adoção prevista | Regras que permanecem específicas |
| --- | --- | --- |
| `Company` | Primeiro piloto do CRUD base para `Get`, `GetAll` e `Create`; `Update`/`Delete` somente quando seus contratos e permissões forem aprovados. | `company.manage`, CNPJ/identificador fiscal, `TenantId` e eventual arquivamento. |
| `Role` | Não usar CRUD genérico: entidade interna administrada pelo caso de uso de `Account` e por operações específicas de persistência. | `Owner` imutável, catálogo e teto de permissões, referências de membros, lock do tenant; sem eventos próprios. |
| `Account` | Reutilizar consulta por ID onde fizer sentido; manter `ListAccessibleAsync`. | Visibilidade por membership/admin e criação composta com roles/membership. |
| `User`, `UserTenant`, `Invitation` | Usar contratos/repositórios compartilhados quando úteis, mantendo controllers e app services de comando. | Auth, confirmação, convites, suspensão, último Owner, tokens e transições de estado. |

O fluxo de `UserAppService.RegisterAsync` delega criação de usuário/credencial ao serviço de registro e criação de conta ao `AccountProvisioningService`; `AccountCreated` inicia as reações de roles/vínculo, e a transação mantém usuário + credencial + conta + Owner atômicos. Não substituir a autorização vigente por role claims do JWT. Endpoints e DTOs atuais mantêm rota, status e payload durante a migração; novas operações públicas terão spec e testes próprios.

## Estado da implementação e limites

`AddZeloServices` concentra os registros; `Repository<TEntity, TId>` contém os cinco métodos e hooks de persistência; `CrudAppService` e `CrudApiController` têm a implementação completa e as variações por aridade; `Company` usa as bases para listagem/criação. `AccountCreated` é despachado antes do commit e configura as roles iniciais e o vínculo Owner. `Role` é entidade comum sem eventos. `RoleAppService` delega à gestão específica da conta. `AppServiceResult<T>` e seu mapper HTTP substituem os nomes antigos. `UserRepository` concentra a escrita de usuário e credencial.

AutoMapper 16.2.0 foi adotado para os profiles; a chave vem de `AutoMapper:LicenseKey` ou `AUTOMAPPER_LICENSE_KEY`. O projeto da API possui `UserSecretsId` e a chave fornecida foi guardada no .NET User Secrets local, fora do repositório. Fora de desenvolvimento a configuração exige que a chave seja fornecida pelo ambiente de implantação. O contrato `ISoftDeletable` e o comportamento genérico de exclusão estão implementados, mas nenhum agregado existente recebeu soft delete ou migration: isso depende da escolha de agregados e retenção. `Update` e `Delete` genéricos existem nas bases, porém nenhuma rota nova foi exposta sem contrato de recurso aprovado. Consultas e comandos especializados de auth, convite e membership continuam específicos.

Os testes automatizados cobrem a coleção de eventos, a condição não agregada de `Role`, mapeamento protegido e operações principais da base de aplicação. O smoke test de auth cobre criação transacional de conta, roles iniciais, convites, permissões, múltiplas contas, email e 2FA; uma falha forçada na criação das roles confirma o rollback de usuário e conta. Testes de exclusão lógica em um agregado escolhido e de rotas futuras pertencem às respectivas próximas mudanças de negócio.
