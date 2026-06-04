# Identidade e Administracao

## Escopo

Empresas, filiais, perfis, permissoes, MFA e contexto operacional.

## Identidade

O sistema usa OIDC/OAuth2. O ambiente local usa Keycloak.

Claims esperadas:

- `sub`;
- `preferred_username`;
- `roles`;
- `company_id`;
- `branch_id`;
- `mfa`.

Se claims de empresa/filial nao existirem, a API aceita headers:

- `X-Company-Id`;
- `X-Branch-Id`.

## Policies

- `admin:tenant`: administracao, requer MFA.
- `sensitive:fiscal`: fiscal, requer MFA.
- `sensitive:finance`: financeiro, requer MFA.

## Administracao

Endpoints:

```http
GET /api/admin/companies
POST /api/admin/companies
POST /api/admin/branches
GET /api/identity/me
POST /api/identity/profiles
```

## RBAC

Permissoes ficam em `identity.module_permissions` por:

- empresa;
- filial;
- role;
- modulo;
- permissao;
- flag de MFA.

## Homologacao

- usuario sem MFA nao acessa fiscal/financeiro/admin;
- usuario com role correta acessa modulo;
- filial A nao enxerga dados da filial B;
- auditoria registra usuario e IP.
