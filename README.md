# Atlas ERP

ERP web-first para lojas de autopecas, distribuidores automotivos e pecas de caminhao no Brasil.

Este repositorio contem uma aplicacao completa para homologacao tecnica: backend ASP.NET Core, frontend React PWA, PostgreSQL, Redis, RabbitMQ, Keycloak, observabilidade, testes, pipeline, scripts de banco e documentacao.

## Inicio Rapido

Requisitos locais:

- Docker Desktop ou Docker Engine com Compose.
- .NET SDK 10.
- Node.js 24.

Suba a stack:

```bash
docker compose up --build
```

URLs locais:

- Web PWA: `http://localhost:5173`
- API/Swagger: `http://localhost:5000/swagger`
- Keycloak: `http://localhost:8080`
- RabbitMQ: `http://localhost:15672`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`

## Stack

- Backend: C#, ASP.NET Core `net10.0`, Minimal APIs, EF Core, OpenAPI/Swagger.
- Frontend: React, TypeScript, Vite, PWA instalavel.
- Banco: PostgreSQL como fonte primaria, modelagem relacional, RLS, particoes, WAL/logical replication-ready.
- Cache: Redis.
- Mensageria: RabbitMQ com outbox transacional.
- Identidade: OIDC/OAuth2 com Keycloak.
- Observabilidade: Serilog, OpenTelemetry, Prometheus e Grafana.

## Documentacao

A documentacao completa esta em [docs/index.md](docs/index.md).

Entradas principais:

- [Visao do produto](docs/product-overview.md)
- [Comecando](docs/getting-started.md)
- [Arquitetura](docs/architecture.md)
- [Manual funcional](docs/functional-manual.md)
- [Modelo de dados](docs/data-model.md)
- [API e contratos](docs/api.md)
- [Fiscal](docs/fiscal.md)
- [Seguranca](docs/security.md)
- [Operacao](docs/operations.md)
- [Deploy](docs/deployment.md)
- [Testes e qualidade](docs/testing-quality.md)
- [Runbooks](docs/runbooks.md)
- [ADRs](docs/adr)

## Validacao Local

Backend:

```bash
dotnet restore Atlas.slnx
dotnet build Atlas.slnx -c Release
dotnet test Atlas.slnx -c Release --collect:"XPlat Code Coverage"
```

Frontend:

```bash
cd web
npm ci
npm run lint
npm run build
```

## Aviso Fiscal

O provider fiscal incluido e `sandbox`. Para producao, configure provider fiscal homologado ou implementacao direta SEFAZ/NFS-e com certificado, assinatura, schemas, contingencia, DANFE/DANFCE/DANFSE e monitoramento. O motor fiscal nao substitui validacao contabil/fiscal humana das regras por UF, NCM, CEST, CFOP, CST, CSOSN, regime, beneficios e vigencias.
