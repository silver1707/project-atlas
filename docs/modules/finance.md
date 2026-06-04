# Financeiro

## Escopo

Contas a pagar, contas a receber, caixa, ledger e conciliacao.

## Entidades

- `AccountPayable`: titulo a pagar.
- `AccountReceivable`: titulo a receber.
- `CashLedgerEntry`: lancamento de caixa/razao.

## Fluxos

### Contas a Pagar

```http
POST /api/finance/payables
```

Campos:

- fornecedor;
- documento;
- vencimento;
- valor;
- status.

### Contas a Receber

```http
POST /api/finance/receivables
```

Campos:

- cliente;
- documento;
- vencimento;
- valor;
- status.

### Ledger

```http
POST /api/finance/cash-ledger
```

### Conciliacao

```http
POST /api/finance/reconciliation
```

A conciliacao compara valor, data proxima e documento.

### Dashboard

```http
GET /api/finance/dashboard
```

Retorna:

- vencidos a pagar;
- vencidos a receber;
- abertos a pagar;
- abertos a receber.

## Homologacao

- criar titulo a pagar;
- criar titulo a receber;
- criar lancamento;
- conciliar extrato;
- verificar dashboard;
- validar MFA obrigatorio para acesso.
