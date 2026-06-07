# ADR 0004: OIDC com Keycloak

## Status

Superseded por [0006: Autenticacao Local por Padrao](0006-local-authentication-default.md)

## Contexto

A versao anterior do produto foi desenhada como webapp, com OIDC/OAuth2 e Keycloak no ambiente local/homologacao.

## Decisao Original

Usar OIDC/OAuth2 com provider compativel, Keycloak no ambiente local/homologacao e RBAC local por modulo/filial.

## Motivo da Substituicao

O produto agora e desktop-first e local-first. Exigir Keycloak para uma loja pequena aumenta complexidade de instalacao, suporte e operacao. OIDC continua relevante para clientes maiores, mas como provider externo opcional.
