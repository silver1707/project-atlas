# AutoParts ERP Desktop

ERP local desktop para Windows, voltado a lojas de autopecas, distribuidores automotivos e pecas de caminhao no Brasil.

Este repositorio agora entrega uma arquitetura desktop-first e local-first: interface React/TypeScript empacotada com Tauri, API local em ASP.NET Core, PostgreSQL como fonte primaria de verdade, Redis/RabbitMQ opcionais para instalacoes avancadas, migrations, seeds, testes, scripts de operacao local e documentacao de instalacao em servidor de loja e terminais de rede.

## Inicio Rapido

Requisitos de desenvolvimento:

- Windows 10/11.
- .NET SDK 10.
- Node.js 24.
- Rust toolchain para empacotar o Tauri.
- Docker Desktop apenas para desenvolvimento/homologacao com Postgres e servicos auxiliares.

Subir ambiente local de desenvolvimento:

```powershell
Copy-Item .env.example .env
.\scripts\dev-local.ps1
```

Servicos e acessos comuns:

- App desktop em modo dev: janela Tauri.
- Renderer dev, quando executado isolado: `http://127.0.0.1:5173`.
- API local/Swagger: `http://localhost:5000/swagger`.
- Health API: `http://localhost:5000/health/ready`.
- PostgreSQL dev: `localhost:5432`, banco `autoparts_erp`.
- RabbitMQ/Redis/Grafana: apenas com perfil avancado.

Credencial seed de homologacao:

- Usuario: `admin`
- Senha: `Admin@123456`

## Stack

- Desktop: Tauri 2, React, TypeScript, Vite.
- Backend local: C#, ASP.NET Core `net10.0`, Minimal APIs, EF Core, OpenAPI/Swagger.
- Banco: PostgreSQL local ou servidor LAN, modelagem relacional, RLS, particionamento preparado, WAL/PITR em instalacao avancada.
- Cache: Redis opcional.
- Mensageria: RabbitMQ opcional; em instalacao simples, jobs rodam no proprio backend com outbox local.
- Identidade: autenticacao local propria com senha forte, refresh token, JWT local e RBAC por empresa/filial; OIDC externo fica como extensao futura.
- Fiscal: provider-based, com provider sandbox/mock, armazenamento local imutavel de XML e preparacao para certificados A1/A3, SEFAZ ou provedores especializados.

## Estrutura

```text
src/
  desktop/AutoPartsErp.Desktop/   React + Tauri + TypeScript
  backend/AutoPartsErp.Api/       API local ASP.NET Core
database/migrations/              migrations SQL e seeds
deploy/local/                     exemplos para servidor Windows/local
docs/                             documentacao tecnica e funcional
scripts/                          build, migration, backup, restore e instalador
tests/                            testes backend e e2e desktop
```

## Comandos

Backend:

```powershell
dotnet restore AutoPartsErp.slnx
dotnet build AutoPartsErp.slnx -c Release
dotnet test AutoPartsErp.slnx -c Release --collect:"XPlat Code Coverage"
```

Desktop renderer:

```powershell
Set-Location src\desktop\AutoPartsErp.Desktop
npm ci
npm run lint
npm run build
```

App desktop Tauri:

```powershell
Set-Location src\desktop\AutoPartsErp.Desktop
npm run desktop:dev
npm run package:windows
```

## Documentacao

A documentacao completa esta em [docs/index.md](docs/index.md).

Entradas principais:

- [Visao do produto](docs/product-overview.md)
- [Comecando](docs/getting-started.md)
- [Arquitetura](docs/architecture.md)
- [Instalacao servidor local](docs/local-server-installation.md)
- [Instalacao terminal cliente](docs/client-terminal-installation.md)
- [Configuracao inicial](docs/initial-setup.md)
- [Fiscal](docs/fiscal.md)
- [Backup e restore](docs/backup-continuity.md)
- [Atualizacao](docs/update-guide.md)
- [Mudanca de webapp para desktop](docs/migration-from-webapp.md)
- [ADRs](docs/adr)

## Aviso Fiscal

O provider fiscal incluido e `sandbox`. A producao fiscal real depende de certificado digital, parametrizacao por contador, validacao em homologacao SEFAZ/NFS-e, schemas vigentes, regras por UF/NCM/CEST/CFOP/CST/CSOSN/regime e autorizacao conforme legislacao aplicavel. O sistema foi preparado para essa homologacao, mas nao afirma emissao fiscal real sem essas etapas.
