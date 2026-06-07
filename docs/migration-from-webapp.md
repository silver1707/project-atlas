# Mudanca de Webapp para Desktop Local

## Objetivo

Registrar a refatoracao arquitetural de um produto web/PWA para ERP desktop local Windows, preservando regra de negocio, entidades, banco, migrations, testes e contratos de API.

## Reaproveitado

- React/TypeScript existente como base da interface.
- Componentes de dashboard, catalogo, estoque, vendas e fiscal.
- API ASP.NET Core e Minimal APIs.
- Modular monolith por bounded contexts.
- Entidades, validacoes e services de dominio.
- PostgreSQL, RLS, migrations e seeds.
- Outbox, domain events e contratos fiscais.
- Testes backend e Playwright como base de e2e.
- Documentacao funcional de modulos de negocio.

## Removido ou Substituido

- Manifest PWA.
- Plugin Vite PWA.
- Conceito de instalacao por navegador.
- Service worker/cache PWA.
- Dockerfile/nginx do frontend como site publicado.
- Documentacao que tratava PWA como produto principal.
- Keycloak/OIDC como requisito local obrigatorio.
- Deploy Kubernetes como caminho padrao.
- Rotas/textos de webapp publico.

## Novo Desenho

- `src/desktop/AutoPartsErp.Desktop`: Tauri + React.
- `src/backend/AutoPartsErp.Api`: API local.
- `database/migrations`: banco local/LAN.
- `deploy/local`: instalacao servidor/Windows.
- `scripts`: build, empacotamento, migration, backup e restore.

## Regras de Migracao

- Dados oficiais ficam no backend/PostgreSQL.
- Desktop guarda somente sessao e preferencias.
- Autenticacao local e padrao; OIDC externo fica opcional.
- Redis/RabbitMQ sao perfil avancado.
- Docker Compose fica para desenvolvimento/homologacao.
- Fiscal continua provider-based, agora com caminhos locais, certificado e reprocessamento por falha de internet.

## Pendencias Naturais

- Implementar wizard visual completo de configuracao inicial.
- Finalizar atualizador automatico assinado.
- Homologar provider fiscal real.
- Empacotar API como servico Windows definitivo.
- Validar instalador em maquina Windows com Rust/Tauri toolchain.
