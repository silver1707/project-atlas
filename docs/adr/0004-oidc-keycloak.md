# ADR 0004: OIDC com Keycloak

## Status

Aceito

## Contexto

ERP fiscal/financeiro exige MFA, SSO, auditoria e segregacao de funcoes.

## Decisao

Usar OIDC/OAuth2 com provider compativel, Keycloak no ambiente local/homologacao e RBAC local por modulo/filial.

## Consequencias

- MFA e politicas de senha ficam no IdP.
- API valida JWT e claims.
- Permissoes operacionais continuam no dominio administrativo.
