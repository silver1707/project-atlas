# ADR 0005: Desktop Local-First com Tauri

## Status

Aceito

## Contexto

O produto deixou de ser um webapp/PWA publico e passou a ser um ERP local desktop para Windows. A interface React/TypeScript existente ja possuia componentes e fluxos importantes para balcao, estoque, fiscal e dashboards.

## Decisao

Usar Tauri 2 para empacotar o frontend React/TypeScript como aplicativo desktop Windows. Manter a API ASP.NET Core separada, acessada por `localhost` no servidor da loja ou por URL LAN em terminais cliente. Manter PostgreSQL como banco local ou servidor de rede.

## Consequencias

- Reaproveita a UI e reduz reescrita.
- Remove service worker, manifest e estrategia PWA.
- Permite instalador Windows NSIS/MSI.
- Mantem fronteira clara entre desktop, API e banco.
- Facilita operacao local sem dependencia de nuvem.
- Exige pipeline Windows com Rust/Tauri para gerar instalador.
