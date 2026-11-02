# Mapeamento de entidades e DTOs

Status: **implementada**.

## Decisões

- Consolidar os mapas AutoMapper da aplicação em `ApplicationMappingProfile`, sem um perfil por entidade.
- `CreateEntityMap<TEntity, TDto>()` registra o mapa entidade → DTO e `ReverseMap()` para o caminho inverso.
- AutoMapper não copia propriedades que não existem no DTO. Por isso, não configurar `Ignore` para `CreationTime`, `CreatorId`, `LastModifiedTime`, `LastModifierId` ou eventos quando o DTO não contém esses membros.
- No mapa inverso, ignorar automaticamente `Id` e `TenantId` apenas quando ambos os tipos expõem a propriedade. A origem desses valores permanece o serviço de aplicação e a rota de tenant.
- O DTO de edição da empresa continua sendo `CompanyCreateRequestDto`, separado de `CompanyDto`. Não criar mapas para pares de tipos sem uso atual.
- Atualizar o registro do AutoMapper e os testes para usar somente o perfil central.

## Validação

- `AssertConfigurationIsValid` passou com o perfil único.
- O teste de mapeamento reverso confirmou que `Id` e `TenantId` permanecem intactos enquanto um campo editável é atualizado.
- Build da API sem avisos e sete testes unitários aprovados.
- Smoke completo com PostgreSQL aprovado, incluindo cadastro, conta, empresas, convites, permissões e autenticação.
