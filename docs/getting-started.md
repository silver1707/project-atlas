# Comecando

## Requisitos

- Windows, Linux ou macOS.
- Docker Desktop ou Docker Engine.
- .NET SDK 10.
- Node.js 24.
- Git.

## Subir Tudo com Docker

```bash
docker compose up --build
```

Servicos:

- `postgres`: banco principal.
- `redis`: cache distribuido.
- `rabbitmq`: mensageria.
- `keycloak`: provedor OIDC local.
- `api`: backend.
- `web`: frontend PWA.
- `otel-collector`, `prometheus`, `grafana`: observabilidade.

## Rodar Backend Fora do Docker

```bash
dotnet restore Atlas.slnx
dotnet run --project src/Atlas.Api/Atlas.Api.csproj
```

Configure variaveis:

```bash
ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=atlas;Username=atlas;Password=atlas_dev_password"
ConnectionStrings__Redis="localhost:6379"
RabbitMq__Host="localhost"
Authentication__Authority="http://localhost:8080/realms/atlas"
```

## Rodar Frontend Fora do Docker

```bash
cd web
npm ci
npm run dev
```

Variaveis comuns:

```bash
VITE_API_BASE_URL=http://localhost:5000
VITE_OIDC_AUTHORITY=http://localhost:8080/realms/atlas
VITE_OIDC_CLIENT_ID=atlas-web
VITE_COMPANY_ID=11111111-1111-1111-1111-111111111111
VITE_BRANCH_ID=22222222-2222-2222-2222-222222222222
```

## Dados de Homologacao

As migrations SQL criam:

- empresa e filial padrao;
- permissoes iniciais;
- produtos reais de exemplo: pastilha, disco de freio e filtro;
- aplicacoes por veiculo;
- equivalentes/OE;
- fornecedor;
- locais de estoque;
- saldos;
- regras fiscais de homologacao.

## Primeiros Testes Manuais

1. Abrir `http://localhost:5173`.
2. Entrar via Keycloak configurado.
3. Buscar produto por `NKF1234`, `04465-0K290`, `Corolla` ou `7890000001234`.
4. Conferir estoque em `Estoque`.
5. Fazer venda de balcao.
6. Emitir documento fiscal em sandbox.
7. Verificar Swagger em `http://localhost:5000/swagger`.
