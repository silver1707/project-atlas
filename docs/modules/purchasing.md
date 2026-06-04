# Compras

## Escopo

Modulo para fornecedores, pedidos de compra e compra sugerida por giro.

## Entidades

- `Supplier`: fornecedor.
- `PurchaseOrder`: pedido de compra.
- `PurchaseOrderLine`: item de compra.

## Fluxos

### Cadastro de Fornecedor

```http
POST /api/purchasing/suppliers
```

Campos:

- razao social;
- nome fantasia;
- documento como string;
- inscricao estadual;
- email;
- telefone.

### Pedido de Compra

```http
POST /api/purchasing/orders
POST /api/purchasing/orders/{id}/approve
```

O pedido armazena snapshot de fornecedor, NCM e CFOP informado para compra. Isso preserva o historico mesmo que cadastro mude depois.

### Compra Sugerida

```http
GET /api/purchasing/suggestions
```

Formula atual:

- vendas dos ultimos 90 dias;
- media diaria;
- alvo de 30 dias ou estoque minimo, o que for maior;
- quantidade sugerida = alvo - disponivel.

## Regras

- Apenas pedido `draft` pode ser aprovado.
- Criacao dispara `PurchaseOrderCreated`.
- Aprovacao dispara `PurchaseOrderApproved`.
- Compra sugerida nao grava pedido automaticamente.

## Homologacao

- cadastrar fornecedor;
- criar pedido com mais de um item;
- aprovar pedido;
- tentar aprovar pedido ja aprovado;
- simular vendas e conferir sugestao de compra;
- auditar criacao e aprovacao.
