# Vendas e Balcao

## Escopo

Modulo de orcamento, pedido, venda de balcao, romaneio, comissao, devolucao, garantia e caixa.

## Entidades

- `SalesQuote`: orcamento.
- `SalesOrder`: pedido.
- `SalesOrderLine`: item com snapshots.
- `CashSession`: sessao de caixa.

## Snapshots

Cada item de venda guarda:

- SKU;
- descricao;
- NCM;
- CEST;
- preco;
- desconto;
- quantidade.

Isso impede que mudancas de cadastro alterem documento historico.

## Fluxos

### Orcamento

```http
POST /api/sales/quotes
```

### Pedido

```http
POST /api/sales/orders
POST /api/sales/orders/{id}/approve
```

### Venda de Balcao

```http
POST /api/sales/counter-sales
```

Fluxo atomico:

1. cria pedido;
2. adiciona itens;
3. recalcula total;
4. salva;
5. reserva estoque;
6. confirma transacao.

Se uma reserva falha, a transacao e revertida.

### Devolucao e Garantia

```http
POST /api/sales/orders/{id}/return
```

O pedido fica como `returned` ou `warranty`.

### Caixa

```http
POST /api/sales/cash/open
POST /api/sales/cash/{id}/close
```

## Eventos

- `SalesOrderCreated`.
- `SalesOrderApproved`.
- `SalesReturnRegistered`.

## Homologacao

- abrir caixa;
- buscar produto;
- vender item com saldo;
- tentar vender item sem saldo;
- aprovar pedido;
- registrar devolucao;
- registrar garantia;
- fechar caixa com diferenca;
- conferir auditoria.
