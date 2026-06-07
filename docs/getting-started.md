# Comecando

## Requisitos

- Windows 10/11 para desktop e instalador.
- .NET SDK 10 para desenvolvimento backend.
- Node.js 24 para o renderer React.
- Rust toolchain para `tauri build`.
- Docker Desktop para Postgres de desenvolvimento e homologacao.
- Git.

## Subir Ambiente de Desenvolvimento

```powershell
Copy-Item .env.example .env
.\scripts\dev-local.ps1
```

Esse script sobe `postgres` e `api` via Docker Compose e abre o app Tauri em modo desenvolvimento.

Para perfil avancado:

```powershell
.\scripts\dev-local.ps1 -Advanced
```

## Rodar Backend Fora do Docker

```powershell
dotnet restore AutoPartsErp.slnx
dotnet run --project src\backend\AutoPartsErp.Api\AutoPartsErp.Api.csproj
```

Variaveis comuns:

```powershell
$env:ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=autoparts_erp;Username=autoparts;Password=autoparts_dev_password"
$env:Atlas__InstallationProfile="development"
$env:Atlas__EnableRedis="false"
$env:Atlas__EnableRabbitMq="false"
$env:LocalAuthentication__SigningKey="development-only-change-this-local-erp-signing-key-64-bytes"
```

## Rodar Desktop

```powershell
Set-Location src\desktop\AutoPartsErp.Desktop
npm ci
npm run desktop:dev
```

Renderer isolado, util para depurar UI:

```powershell
npm run dev
```

## Dados de Homologacao

As migrations SQL criam:

- empresa e filial padrao;
- permissoes iniciais;
- usuario local `admin`;
- produtos reais de exemplo;
- aplicacoes por veiculo;
- equivalentes/OE;
- fornecedor;
- locais de estoque;
- saldos;
- regras fiscais sandbox.

Credencial:

- Usuario: `admin`
- Senha: `Admin@123456`

## Primeiros Testes Manuais

1. Abrir o aplicativo desktop.
2. Conferir o painel de status da API local.
3. Entrar com `admin` e `Admin@123456`.
4. Buscar produto por `NKF1234`, `04465-0K290`, `Corolla` ou `7890000001234`.
5. Conferir saldo em estoque.
6. Fazer venda de balcao.
7. Emitir documento fiscal no provider sandbox.
8. Verificar Swagger em `http://localhost:5000/swagger`.
