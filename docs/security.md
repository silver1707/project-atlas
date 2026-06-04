# Seguranca

## Objetivo

O sistema nasce com controles para proteger dados fiscais, financeiros e operacionais. As regras abaixo sao obrigatorias para homologacao e producao.

## Autenticacao

- OIDC/OAuth2 com Keycloak local.
- JWT Bearer na API.
- PKCE no frontend.
- MFA obrigatorio para perfis sensiveis.
- Sessao e politicas de senha controladas pelo IdP.

## Autorizacao

Policies:

- `admin:tenant`: administracao.
- `sensitive:fiscal`: fiscal.
- `sensitive:finance`: financeiro.

RBAC por:

- empresa;
- filial;
- role;
- modulo;
- permissao.

## Segregacao de Funcoes

Recomendado:

- fiscal cadastra regras, mas aprovacao de excecoes exige outro perfil;
- financeiro baixa titulos, mas nao altera regra fiscal;
- vendedor fecha venda, mas nao cancela documento fiscal sem autorizacao;
- admin tecnico nao deve acessar XML fiscal sem necessidade.

## Tenant Isolation

Camadas:

- headers/claims de tenant;
- filtro EF por empresa/filial;
- variaveis de sessao PostgreSQL;
- RLS no banco.

## OWASP ASVS

Checklist aplicado:

- autenticacao centralizada;
- MFA para acoes sensiveis;
- controle de acesso server-side;
- validacao de entrada;
- tratamento padronizado de erro;
- logs sem segredos;
- rate limiting;
- segredos fora do codigo;
- auditoria before/after;
- headers e CORS restritos;
- health checks separados.

## Dados Sensiveis

Nao logar:

- tokens;
- senhas;
- certificados;
- private keys;
- XML completo com dados pessoais quando desnecessario;
- credenciais de providers.

## Certificados Digitais

Para producao:

- armazenar fora do repositorio;
- preferir vault/HSM ou provider de assinatura;
- controlar acesso por perfil;
- auditar uso;
- rotacionar antes do vencimento;
- testar assinatura em homologacao.

## Rate Limiting

API usa limite global por usuario/IP. Providers externos devem ter limites proprios por adapter.

## Incidentes

Ver [Runbooks](runbooks.md).
