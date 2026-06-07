# Testes e Qualidade

## Camadas

- Unitarios de dominio.
- Aplicacao/use cases.
- Integracao com PostgreSQL real via Testcontainers.
- Contratos de providers fiscais.
- API tests.
- E2E do renderer/desktop com Playwright.
- Lint e build do desktop renderer.
- Build de instalador Tauri em Windows.
- Analise estatica CodeQL.

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

E2E:

```powershell
Set-Location tests\AutoPartsErp.Desktop.E2E
npm ci
npm test
```

Instalador:

```powershell
.\scripts\package-windows.ps1
```

## Cenarios Criticos

- login local e refresh token;
- permissao sem acesso indevido;
- venda de balcao com reserva;
- venda sem saldo;
- compra sugerida por giro;
- recebimento e transferencia;
- devolucao;
- garantia;
- emissao fiscal sandbox/provider;
- cancelamento fiscal;
- contingencia;
- importacao de XML;
- fechamento de caixa;
- auditoria;
- tenant isolation;
- terminal cliente apontando para servidor LAN.

## Quality Gates

- build backend verde;
- testes backend verdes;
- lint desktop verde;
- build renderer verde;
- instalador Tauri gerado em runner Windows;
- CodeQL sem alerta critico;
- migrations revisadas;
- smoke test local documentado;
- documentacao atualizada.
