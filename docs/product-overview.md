# Visao do Produto

## Objetivo

AutoParts ERP Desktop e um sistema operacional local para autopecas e distribuidores no Brasil. O foco e vender a peca correta, com estoque confiavel, precificacao consistente, emissao fiscal controlada, financeiro rastreavel e operacao resiliente mesmo quando a internet cai.

## Publico

- Loja de autopecas com venda de balcao.
- Distribuidor de pecas automotivas.
- Distribuidor de pecas para caminhoes e utilitarios.
- Rede multiempresa/multifilial com estoque por local.
- Operacao com servidor de loja e terminais em rede LAN.

## Principios de Produto

- Desktop-first e local-first.
- Busca de peca e compatibilidade sao fluxos centrais.
- Produto mestre une dados tecnicos, comerciais, fiscais e logisticos.
- Banco PostgreSQL local/LAN e a fonte de verdade.
- O desktop nunca grava dado oficial fora da API.
- Operacoes criticas deixam trilha de auditoria.
- Fiscal e modulo critico, provider-based e parametrizavel.
- Integracoes externas entram por adapters.
- Redis, RabbitMQ e observabilidade completa sao opcionais por perfil.

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

1. Configurar empresa, filial, caixa, parametros fiscais e caminhos locais.
2. Cadastrar produto tecnico com codigos, GTIN, NCM, CEST, atributos, fornecedores e aplicacoes.
3. Buscar por codigo, OE, equivalente, GTIN, descricao ou compatibilidade veicular.
4. Criar venda de balcao com reserva de estoque.
5. Gerar documento fiscal via provider.
6. Registrar recebimento financeiro e fechamento de caixa.
7. Receber XML de entrada e conciliar compra.
8. Rodar compra sugerida por giro e estoque minimo.
9. Auditar alteracoes, monitorar servicos locais e executar backups.

## Nao Objetivos da Homologacao Inicial

- Nao inclui provider fiscal real embutido.
- Nao inclui catalogo TecDoc/TecAlliance real embutido.
- Nao substitui responsabilidade contabil por parametrizacao fiscal validada.
- Nao depende de nuvem para operar.
- Nao transforma o sistema em microsservicos.
