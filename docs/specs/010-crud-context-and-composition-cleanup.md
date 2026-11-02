# Limpeza do CRUD, contexto da requisição e composição

Status: **implementada**.

## Decisões

- Remover WeatherForecast e a pasta `Zelo.Domain` gerada indevidamente dentro do projeto da API.
- `ICurrentUser`, `ICurrentTenant` e `ICurrentCompany` fornecem os IDs da requisição. `actorId` designava o usuário autenticado, enquanto `id` designa a entidade solicitada; só o segundo permanece como argumento dos métodos CRUD. `TenantId` vem da rota, nunca do corpo. Em comandos sem tenant na rota, serviços especializados continuam recebendo explicitamente o tenant do caso de uso.
- O CRUD público recebe apenas ID e DTO quando aplicável. Leituras genéricas não expõem `CancellationToken`; comandos e fluxos especializados podem continuar usando cancelamento onde necessário. A base de aplicação consulta os contextos e autoriza antes de chamar o repositório.
- O repositório genérico consulta por ID e usa um filtro NHibernate de tenant para entidades `IMultiTenant`. O filtro é habilitado na sessão quando a rota possui tenant; o repositório genérico rejeita consultas a entidades tenant-owned sem tenant corrente. Consultas especializadas fora de uma rota de tenant continuam exigindo escopo explícito. O filtro de banco não substitui a autorização de membership.
- `CrudApiController`, `ICrudAppService` e `CrudAppService` mantêm uma forma completa e variações por aridade. A variação usada por `Company` emprega `CompanyCreateRequestDto` tanto na criação quanto na edição, e `CompanyDto` somente na resposta. `Company` usa diretamente os cinco métodos da base, sem aliases `ListAsync` ou overloads que só chamam a base.
- `Program.cs` concentra opções e decisões de infraestrutura HTTP, JWT, email e mapeamento. `AddZeloInfrastructure` configura a persistência e componentes compartilhados, e `AddModules<ZeloModule>()` adiciona os serviços específicos do projeto por registros simples. Não manter uma segunda classe de composição com a mesma responsabilidade.
- Manter os contratos HTTP existentes de autenticação, convites, memberships e contas. Não criar rotas públicas de edição/exclusão de Company nesta limpeza. Validar build, testes e smoke de autenticação, incluindo isolamento entre tenants.

## Estado final

Os controllers de contas, empresas, roles, membros e convites não extraem nem repassam o ID do usuário autenticado. Roles, membros e convites consultam os contextos nos app services; `AccountAppService.GetAsync` usa uma consulta de acesso por ID no repositório, enquanto a listagem acessível permanece distinta para login. O filtro NHibernate `tenant` está mapeado para `Company`, `UserTenant`, `Role`, `RolePermission` e `Invitation`; a sessão o habilita a partir da rota. `Repository.GetAsync` usa uma consulta filtrável por ID, evitando `Session.Get` para entidades tenant-owned. O smoke test cria empresas em duas contas e verifica que um membro de ambas só recebe as empresas da conta solicitada.
