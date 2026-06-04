# Rastreabilidade de Requisitos

| Requisito | Implementacao | Documentacao |
| --- | --- | --- |
| Web-first responsivo e PWA | `web`, Vite PWA, CSS responsivo | `README.md`, `getting-started.md` |
| Backend C# ASP.NET Core | `src/Atlas.Api` | `architecture.md` |
| PostgreSQL fonte primaria | `db/migrations`, `AtlasDbContext` | `data-model.md` |
| Redis cache | `docker-compose.yml`, DI cache | `configuration.md` |
| RabbitMQ mensageria | MassTransit, `OutboxDispatcher` | `integrations.md` |
| Modular monolith | namespaces e schemas por contexto | `architecture.md` |
| Catalogo autopecas | `CatalogModule` | `modules/catalog.md` |
| Compatibilidade por veiculo/chassi/VIN-ready | `ProductApplication` | `modules/catalog.md` |
| Produto mestre comercial/fiscal/logistico | `AutoPartProduct` | `modules/catalog.md` |
| Compras e compra sugerida | `PurchasingModule` | `modules/purchasing.md` |
| Estoque por local/reserva/picking | `InventoryModule` | `modules/inventory.md` |
| Vendas, balcao, romaneio, caixa | `SalesModule` | `modules/sales.md` |
| Fiscal provider-based | `FiscalModule`, `IFiscalDocumentProvider` | `fiscal.md` |
| Motor fiscal parametrizavel | `FiscalRule`, `TaxEngine` | `fiscal.md` |
| DF-e por NSU e manifestacao | contratos fiscais | `fiscal.md` |
| Importacao XML entrada | `ImportInboundXmlAsync` | `modules/fiscal-user.md` |
| Financeiro | `FinanceModule` | `modules/finance.md` |
| CRM | `CrmModule` | `modules/crm.md` |
| OIDC/OAuth2 Keycloak | JWT Bearer, config | `security.md` |
| MFA sensivel | policies | `security.md` |
| Auditoria before/after | `AuditRecord`, `SaveChangesAsync` | `data-model.md` |
| RLS tenant | SQL e interceptor | `data-model.md` |
| Particoes | SQL inicial | `data-model.md` |
| Backup WAL/PITR | scripts | `backup-continuity.md` |
| OpenAPI/Swagger | SwaggerGen | `api.md` |
| Observabilidade | Serilog, OTLP | `observability.md` |
| Testcontainers/API/E2E | tests | `testing-quality.md` |
| CI/CD | GitHub Actions | `deployment.md` |
| ADRs | `docs/adr` | `adr` |
