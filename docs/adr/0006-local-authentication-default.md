# ADR 0006: Autenticacao Local por Padrao

## Status

Aceito

## Contexto

O ERP local deve instalar em lojas pequenas sem exigir IdP externo. Ainda precisa de seguranca, trilha de auditoria, segregacao de funcoes e possibilidade futura de integracao corporativa.

## Decisao

Implementar autenticacao local propria por padrao, com senha forte, hash PBKDF2-SHA256, JWT local, refresh token armazenado por hash, logs de autenticacao, lockout e RBAC por empresa/filial/modulo. OIDC/Keycloak fica como provider externo opcional futuro.

## Consequencias

- Instalacao local simples fica viavel.
- API continua validando todas as permissoes server-side.
- MFA deve ser ativado para perfis sensiveis quando configurado.
- Clientes maiores ainda podem integrar OIDC no futuro sem reescrever dominio.
