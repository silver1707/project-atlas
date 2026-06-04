# Modelo de Dados

## Principios

- PostgreSQL e fonte primaria de verdade.
- Dados centrais sao relacionais e normalizados.
- JSONB e usado apenas para payload externo, snapshot, atributos tecnicos, linhas de picking, interacoes e integracoes.
- Identificadores fiscais e tecnicos sao strings.
- Tabelas volumosas usam particoes.
- RLS isola dados por empresa.

## Identificadores como String

Armazenar como string:

- CNPJ;
- CPF;
- inscricao estadual;
- GTIN/EAN;
- chassi;
- placa;
- chave de NF-e/NFC-e;
- codigo OE;
- codigo fabricante;
- NCM;
- CEST;
- CFOP;
- CST;
- CSOSN.

Motivos:

- preserva zeros a esquerda;
- suporta CNPJ alfanumerico;
- evita overflow;
- evita formatacao fiscal incorreta;
- permite codigos com letras, barras e hifens.

## Schemas

- `administration`;
- `identity`;
- `audit`;
- `catalog`;
- `purchasing`;
- `inventory`;
- `sales`;
- `fiscal`;
- `finance`;
- `crm`;
- `integrations`.

## RLS

Todas as tabelas tenant-scoped tem colunas:

- `CompanyId`;
- `BranchId`;
- `CreatedAt`;
- `CreatedBy`;
- `UpdatedAt`;
- `UpdatedBy`;
- `RowVersion`.

Policy:

```sql
"CompanyId" = administration.current_company_id()
```

## Particoes

Particionadas por data:

- `audit.audit_records`;
- `inventory.stock_movements`;
- `fiscal.fiscal_documents`.

Criar novas particoes antes da virada de ano.

## Indices Criticos

- `catalog.products`: SKU, GTIN, codigo fabricante, OE, NCM/CEST e trigram para busca.
- `catalog.product_equivalents`: codigo equivalente.
- `catalog.product_applications`: marca/modelo/ano.
- `inventory.stock_balances`: empresa, filial, produto, local.
- `sales.sales_orders`: status e data.
- `fiscal.fiscal_rules`: UF, regime, operacao, NCM, CEST e vigencia.
- `fiscal.fiscal_documents`: chave, modelo, serie, numero e status.
- `integrations.outbox_messages`: pendentes por data.

## Auditoria

`audit.audit_records` grava:

- `EntityName`;
- `EntityId`;
- `Operation`;
- `Before`;
- `After`;
- `UserId`;
- `IpAddress`;
- `OccurredAt`.

## Backup e Replicacao

Compose local habilita:

- `wal_level=logical`;
- slots e senders para replicacao;
- PITR no compose de homologacao com archive command.

Ver [Backup e continuidade](backup-continuity.md).
