# CRM

## Escopo

Cadastro de clientes, documentos, contatos, placa, chassi, segmento e interacoes.

## Entidade

- `Customer`.

## Campos

- tipo: pessoa fisica, juridica ou consumidor final.
- nome;
- documento;
- inscricao estadual;
- email;
- telefone;
- placa principal;
- chassi principal;
- segmento;
- interacoes.

## Fluxos

### Criar Cliente

```http
POST /api/crm/customers
```

### Buscar Cliente

```http
GET /api/crm/customers?term=ABC1D23
```

Busca por:

- nome;
- documento;
- telefone;
- email;
- placa.

### Registrar Interacao

```http
POST /api/crm/customers/{id}/interactions
```

## Homologacao

- cadastrar cliente pessoa fisica;
- cadastrar oficina/frota;
- pesquisar por placa;
- registrar interacao com follow-up;
- auditar alteracao.
