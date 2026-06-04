# API e Contratos

## Padroes

- Base local: `http://localhost:5000`.
- Swagger: `/swagger`.
- Health: `/health/live` e `/health/ready`.
- Autenticacao: Bearer JWT OIDC.
- Tenant: headers `X-Company-Id` e `X-Branch-Id` quando claims nao existirem.
- Erros: `application/problem+json` com `traceId`.

## Headers

```http
Authorization: Bearer <token>
X-Company-Id: 11111111-1111-1111-1111-111111111111
X-Branch-Id: 22222222-2222-2222-2222-222222222222
Content-Type: application/json
```

## Administracao

```http
GET /api/admin/companies
POST /api/admin/companies
POST /api/admin/branches
```

## Identidade

```http
GET /api/identity/me
POST /api/identity/profiles
```

## Catalogo

```http
GET /api/catalog/products/search
GET /api/catalog/products/{id}
POST /api/catalog/products
POST /api/catalog/products/{id}/applications
POST /api/catalog/products/{id}/equivalents
POST /api/catalog/products/{id}/price
```

Exemplo de busca:

```http
GET /api/catalog/products/search?term=04465-0K290&make=Toyota&model=Corolla&year=2018&limit=50
```

## Compras

```http
POST /api/purchasing/suppliers
POST /api/purchasing/orders
GET /api/purchasing/orders/{id}
POST /api/purchasing/orders/{id}/approve
GET /api/purchasing/suggestions
```

## Estoque

```http
GET /api/inventory/balances
POST /api/inventory/locations
POST /api/inventory/receipts
POST /api/inventory/reservations
POST /api/inventory/transfers
POST /api/inventory/picking
POST /api/inventory/picking/{id}/confirm
```

## Vendas

```http
POST /api/sales/quotes
POST /api/sales/orders
POST /api/sales/counter-sales
POST /api/sales/orders/{id}/approve
POST /api/sales/orders/{id}/return
POST /api/sales/cash/open
POST /api/sales/cash/{id}/close
```

## Fiscal

```http
POST /api/fiscal/rules
POST /api/fiscal/documents/issue
POST /api/fiscal/documents/{id}/cancel
POST /api/fiscal/documents/{id}/query
POST /api/fiscal/documents/inutilization
POST /api/fiscal/dfe/distribution
POST /api/fiscal/dfe/manifestation
POST /api/fiscal/imports/xml
GET /api/fiscal/documents
```

## Financeiro

```http
POST /api/finance/payables
POST /api/finance/receivables
POST /api/finance/cash-ledger
POST /api/finance/reconciliation
GET /api/finance/dashboard
```

## CRM

```http
POST /api/crm/customers
GET /api/crm/customers
POST /api/crm/customers/{id}/interactions
```

## Relatorios

```http
GET /api/reports/operations-dashboard
GET /api/reports/sales-by-day
```

## Integracoes

```http
GET /api/integrations/adapters
POST /api/integrations/adapters
POST /api/integrations/aftermarket/catalog/search
```

## Versionamento

A versao atual e `v1` no Swagger. Mudancas breaking devem gerar nova versao de rota ou contrato. Eventos de integracao carregam `schemaVersion` nos headers do outbox.
