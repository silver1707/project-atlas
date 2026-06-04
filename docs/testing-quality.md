# Testes e Qualidade

## Camadas

- Unitarios de dominio.
- Integracao com PostgreSQL real via Testcontainers.
- Contratos de providers fiscais.
- API tests.
- E2E com Playwright.
- Lint e build frontend.
- Analise estatica CodeQL.

## Comandos

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

E2E:

```bash
cd tests/Atlas.Web.E2E
npm ci
npm test
```

## Cenarios Criticos

- venda de balcao com reserva;
- venda sem saldo;
- compra sugerida por giro;
- recebimento e transferencia;
- devolucao;
- garantia;
- emissao fiscal;
- cancelamento fiscal;
- contingencia;
- importacao de XML;
- fechamento de caixa;
- permissao sem MFA;
- tenant isolation.

## Cobertura Relevante

O objetivo nao e cobertura numerica artificial. Cobrir:

- regras de negocio;
- fluxos transacionais;
- seguranca;
- contratos externos;
- migracoes;
- bugs regressivos.

## Quality Gates

- build backend verde;
- testes verdes;
- lint frontend verde;
- build PWA verde;
- CodeQL sem alerta critico;
- imagens Docker geradas;
- migrations revisadas;
- documentacao atualizada.
