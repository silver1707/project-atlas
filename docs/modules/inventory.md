# Estoque

## Escopo

Controle de estoque por empresa, filial, local, saldo, reserva, movimento, picking, transferencia, devolucao e garantia.

## Entidades

- `StockLocation`: local de estoque.
- `StockBalance`: saldo por produto/local.
- `StockMovement`: movimento imutavel.
- `StockReservation`: reserva.
- `PickList`: lista de separacao.

## Saldos

O saldo e separado em:

- `OnHandQuantity`: fisico.
- `ReservedQuantity`: reservado.
- `AvailableQuantity`: disponivel.
- `MinimumQuantity`: minimo.
- `MaximumQuantity`: maximo.

## Movimentos

Tipos:

- `receipt`: entrada.
- `sale`: venda.
- `transfer_in`: entrada por transferencia.
- `transfer_out`: saida por transferencia.
- `return`: devolucao.
- `warranty`: garantia.
- `adjustment`: ajuste.

## Fluxos

### Criar Local

```http
POST /api/inventory/locations
```

### Receber Estoque

```http
POST /api/inventory/receipts
```

Atualiza fisico, disponivel, ultimo custo e cria movimento.

### Reservar

```http
POST /api/inventory/reservations
```

Reserva diminui disponivel e aumenta reservado. Se nao houver saldo, retorna erro.

### Transferir

```http
POST /api/inventory/transfers
```

Cria movimento de saida e entrada.

### Picking

```http
POST /api/inventory/picking
POST /api/inventory/picking/{id}/confirm
```

## Homologacao

- receber produto;
- reservar produto;
- tentar reservar acima do disponivel;
- transferir entre locais;
- criar e confirmar picking;
- conferir auditoria e movimentos particionados.
