# ADR 0003: PostgreSQL, RLS e Outbox

## Status

Aceito

## Contexto

O sistema precisa de fonte primaria relacional, auditoria, tenant isolation e integracoes confiaveis.

## Decisao

Usar PostgreSQL como fonte de verdade, RLS por empresa, particoes para tabelas volumosas, logical replication para analitico e outbox transacional para mensageria.

## Consequencias

- Forte consistencia no core.
- Integracoes assicronas resilientes.
- Aplicacao precisa configurar variaveis de sessao de tenant em cada conexao.
