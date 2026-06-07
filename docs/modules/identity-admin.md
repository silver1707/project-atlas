# Identidade e Administracao

## Escopo

Empresas, filiais, usuarios locais, perfis, permissoes, MFA, sessoes e contexto operacional.

## Identidade Local

O sistema usa autenticacao local por padrao:

- usuario e senha;
- hash forte com salt;
- JWT local;
- refresh token armazenado por hash;
- lockout por tentativas;
- log de autenticacao;
- RBAC por modulo/empresa/filial.

Claims esperadas no token:

- `sub`;
- `preferred_username`;
- `roles`;
- `company_id`;
- `branch_id`;
- `mfa`.

Se claims de empresa/filial nao existirem em chamadas internas controladas, a API aceita headers:

- `X-Company-Id`;
- `X-Branch-Id`.

## Policies

- `admin:tenant`: administracao, requer MFA quando configurado.
- `sensitive:fiscal`: fiscal, requer MFA quando configurado.
- `sensitive:finance`: financeiro, requer MFA quando configurado.

## Administracao

Endpoints:

```http
GET /api/auth/bootstrap/status
POST /api/auth/bootstrap/admin
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
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

- usuario sem role nao acessa modulo;
- usuario sensivel exige MFA quando configurado;
- filial A nao enxerga dados da filial B;
- auditoria registra usuario e IP;
- logout revoga refresh token;
- lockout ocorre apos tentativas invalidas.
