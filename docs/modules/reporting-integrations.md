# Relatorios e Integracoes

## Relatorios

Endpoints:

```http
GET /api/reports/operations-dashboard
GET /api/reports/sales-by-day?from=2026-06-01&to=2026-06-30
```

Dashboard operacional retorna:

- quantidade de vendas hoje;
- valor vendido hoje;
- documentos fiscais autorizados;
- rejeicoes fiscais;
- itens abaixo do minimo;
- pickings pendentes.

## Integracoes

Integracoes sao adapters. O dominio nao conhece fornecedor externo.

Tipos previstos:

- catalogo aftermarket;
- provider fiscal;
- jobs;
- BI/leitura analitica;
- importadores de tabelas fiscais;
- webhooks.

## Aftermarket Catalog

Contrato:

```http
POST /api/integrations/aftermarket/catalog/search
```

Entrada:

- adapter;
- codigo;
- VIN;
- marca;
- modelo;
- ano;
- motor.

Saida:

- id externo;
- codigo;
- marca;
- descricao;
- fitment JSON.

## Outbox

Eventos de dominio sao gravados em `integrations.outbox_messages` e publicados no RabbitMQ por `OutboxDispatcher`.

## Homologacao

- registrar adapter;
- executar busca com adapter `null`;
- gerar evento de produto/venda/fiscal;
- conferir outbox pendente e processada;
- desligar RabbitMQ e validar retries.
