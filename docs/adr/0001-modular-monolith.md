# ADR 0001: Modular Monolith

## Status

Aceito

## Contexto

O ERP precisa de transacoes fortes entre catalogo, estoque, vendas, fiscal e financeiro. Extrair servicos cedo aumentaria complexidade de consistencia e deploy.

## Decisao

Usar modular monolith com schemas, namespaces, endpoints e contratos separados por bounded context. Eventos de dominio viram integration events por outbox.

## Consequencias

- Menos custo operacional no inicio.
- Consistencia transacional mais simples.
- Extracao futura possivel por modulo, desde que contratos e outbox sejam preservados.
