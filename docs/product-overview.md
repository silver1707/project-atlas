# Visao do Produto

## Objetivo

Atlas ERP e um sistema operacional para autopecas e distribuidores no Brasil. O foco e vender a peca correta, com estoque confiavel, precificacao consistente, emissao fiscal controlada, financeiro rastreavel e integracoes desacopladas.

## Publico

- Loja de autopecas com venda de balcao.
- Distribuidor de pecas automotivas.
- Distribuidor de pecas para caminhoes e utilitarios.
- Rede multiempresa/multifilial com estoque por local.

## Principios de Produto

- Busca de peca e compatibilidade sao fluxos centrais, nao cadastros auxiliares.
- Produto mestre une dados tecnicos, comerciais, fiscais e logisticos.
- Operacoes criticas deixam trilha de auditoria.
- Fiscal e modulo critico, provider-based e parametrizavel.
- Banco relacional e a fonte de verdade.
- Integracoes externas entram por adapters.
- O frontend e web-first e instalavel como PWA.

## Modulos

- Catalogo e compatibilidade.
- Compras.
- Estoque.
- Vendas.
- Fiscal.
- Financeiro.
- CRM.
- Autenticacao e administracao.
- Relatorios.
- Integracoes.

## Fluxos Principais

1. Cadastrar produto tecnico com codigos, GTIN, NCM, CEST, atributos, fornecedores e aplicacoes.
2. Buscar por codigo, OE, equivalente, GTIN, descricao ou compatibilidade veicular.
3. Criar venda de balcao com reserva de estoque.
4. Gerar documento fiscal via provider.
5. Registrar recebimento financeiro e fechamento de caixa.
6. Receber XML de entrada e conciliar compra.
7. Rodar compra sugerida por giro e estoque minimo.
8. Auditar alteracoes e monitorar operacao.

## Nao Objetivos do MVP de Homologacao

- Nao inclui provider fiscal real embutido.
- Nao inclui catalogo TecDoc/TecAlliance real embutido.
- Nao substitui responsabilidade contabil por parametrizacao fiscal validada.
- Nao inclui BI externo, mas prepara logical replication e relatorios operacionais.
