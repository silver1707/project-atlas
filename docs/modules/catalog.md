# Catalogo e Compatibilidade

## Escopo

Modulo central para cadastro tecnico e busca de autopecas. O catalogo une dados comerciais, fiscais, logisticos e tecnicos do produto.

## Entidades

- `AutoPartProduct`: produto mestre.
- `ProductApplication`: aplicacao por veiculo.
- `ProductEquivalent`: codigos OE, equivalentes, similares e substitutos.
- `ProductSupplierOffer`: multiplos fornecedores e codigos de compra.
- `ProductPriceHistory`: historico de custo, margem e tabelas de preco.
- `VehicleModel`: base interna de veiculos e referencia externa opcional.

## Dados do Produto

Obrigatorios ou recomendados:

- SKU.
- descricao.
- GTIN/EAN como string.
- NCM como string.
- CEST como string quando aplicavel.
- unidade comercial.
- unidade tributavel.
- marca.
- linha.
- fabricante.
- codigo do fabricante.
- codigo OE.
- origem fiscal.
- peso e dimensoes.
- atributos tecnicos em JSONB.
- fotos.
- custo, margem e preco sugerido.

## Compatibilidade

Aplicacao por veiculo inclui:

- fabricante do veiculo;
- modelo;
- ano inicial;
- ano final;
- motor;
- combustivel;
- faixa de chassi;
- expressao VIN-ready;
- observacoes.

VIN nao e validado pelo dominio interno. Quando houver integracao externa, o adapter traduz VIN para criterios de compatibilidade.

## Busca

Endpoint principal:

```http
GET /api/catalog/products/search?term=04465-0K290&make=Toyota&model=Corolla&year=2018
```

A busca considera:

- SKU;
- descricao;
- GTIN;
- codigo do fabricante;
- OE;
- NCM;
- equivalentes;
- aplicacao por veiculo;
- faixa de chassi.

## Fluxo de Cadastro

1. Criar produto mestre.
2. Adicionar aplicacoes por veiculo.
3. Adicionar OE/equivalentes.
4. Adicionar fornecedores.
5. Atualizar politica comercial.
6. Validar busca por codigo e por veiculo.

## Regras

- Identificadores tecnicos sao string.
- Produto dispara evento `ProductCreated`.
- Alteracao de compatibilidade dispara `ProductCompatibilityChanged`.
- Alteracao de preco dispara `ProductPriceChanged`.

## Homologacao

Cenarios minimos:

- buscar por SKU;
- buscar por GTIN;
- buscar por OE;
- buscar por equivalente;
- buscar por Toyota Corolla 2018;
- buscar por chassi dentro e fora da faixa;
- atualizar preco e conferir historico;
- verificar auditoria da alteracao.
