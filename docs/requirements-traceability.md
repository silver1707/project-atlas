# Rastreabilidade de Requisitos

| Requisito | Implementacao | Documentacao |
| --- | --- | --- |
| Desktop local Windows | `src/desktop/AutoPartsErp.Desktop`, Tauri | `README.md`, `architecture.md` |
| Reaproveitamento React/TypeScript | componentes e features em `src/desktop` | `migration-from-webapp.md` |
| Backend C# ASP.NET Core local | `src/backend/AutoPartsErp.Api` | `architecture.md` |
| PostgreSQL fonte primaria | `database/migrations`, `AtlasDbContext` | `data-model.md` |
| Redis opcional | config `Atlas:EnableRedis` | `configuration.md` |
| RabbitMQ opcional | `IOutboxPublisher`, MassTransit opcional | `integrations.md` |
| Modular monolith | namespaces e schemas por contexto | `architecture.md` |
| Autenticacao local | `LocalAuthenticationService` | `security.md` |
| Multiempresa/multifilial | tenant context, RLS, claims/headers | `data-model.md` |
| Catalogo autopecas | `CatalogModule` | `modules/catalog.md` |
| Compatibilidade por veiculo/chassi/VIN-ready | `ProductApplication` | `modules/catalog.md` |
| Produto mestre comercial/fiscal/logistico | `AutoPartProduct` | `modules/catalog.md` |
| Compras e compra sugerida | `PurchasingModule` | `modules/purchasing.md` |
| Estoque por local/reserva/picking | `InventoryModule` | `modules/inventory.md` |
| Vendas, balcao, romaneio, caixa | `SalesModule` | `modules/sales.md` |
| Fiscal provider-based local | `FiscalModule`, `IFiscalDocumentProvider` | `fiscal.md` |
| Motor fiscal parametrizavel | `FiscalRule`, `TaxEngine` | `fiscal.md` |
| XML fiscal local imutavel | `ImmutableXmlStore` | `fiscal.md` |
| DF-e por NSU e manifestacao | contratos fiscais | `fiscal.md` |
| Importacao XML entrada | `ImportInboundXmlAsync` | `modules/fiscal-user.md` |
| Financeiro | `FinanceModule` | `modules/finance.md` |
| CRM | `CrmModule` | `modules/crm.md` |
| Auditoria before/after | `AuditRecord`, `SaveChangesAsync` | `data-model.md` |
| RLS tenant | SQL e interceptor | `data-model.md` |
| Particoes | SQL inicial | `data-model.md` |
| Backup local/WAL/PITR | scripts | `backup-continuity.md` |
| OpenAPI/Swagger | SwaggerGen | `api.md` |
| Observabilidade local/avancada | Serilog, health, OTLP opcional | `observability.md` |
| Testcontainers/API/E2E | tests | `testing-quality.md` |
| Instalador desktop | Tauri bundles NSIS/MSI | `deployment.md` |
| ADRs | `docs/adr` | `adr` |
