# Manual Funcional

## Papeis

- Administrador: configura empresas, filiais, perfis, parametros locais e adapters.
- Vendedor de balcao: busca produtos, monta venda, consulta estoque e fecha pedido.
- Estoquista: recebe, transfere, reserva, confere e separa mercadorias.
- Comprador: cadastra fornecedor, emite pedido e usa compra sugerida.
- Fiscal: parametriza regras, emite, consulta, cancela, inutiliza e importa XML.
- Financeiro: gerencia contas a pagar, receber, caixa e conciliacao.
- CRM: cadastra clientes, frota e interacoes.

## Padrao de Operacao

Cada operacao usa contexto de empresa e filial. No desktop, esse contexto vem da sessao local e das escolhas do usuario. Na API, o contexto vem das claims do JWT local e dos headers `X-Company-Id`/`X-Branch-Id` quando necessario.

## Catalogo

O usuario pesquisa por:

- SKU interno.
- Codigo do fabricante.
- Codigo OE.
- GTIN/EAN.
- Equivalente ou similar.
- Descricao.
- Marca/modelo/ano/motor/combustivel.
- Chassi quando houver faixa configurada.
- VIN quando houver adapter externo.

Resultado esperado:

- dados comerciais;
- aplicacoes;
- equivalentes;
- preco sugerido;
- codigos tecnicos;
- saldo por local.

## Vendas

Venda de balcao:

1. Buscar produto.
2. Conferir aplicacao no veiculo.
3. Conferir saldo.
4. Adicionar ao carrinho.
5. Validar desconto/autorizacao.
6. Fechar venda.
7. Sistema cria pedido, reservas e financeiro.
8. Pedido fica pronto para fiscal.

Se alguma reserva falhar, a transacao e revertida.

## Estoque

Saldo e dividido em:

- fisico;
- reservado;
- disponivel;
- minimo;
- maximo.

Movimentos suportados:

- recebimento;
- venda;
- transferencia;
- devolucao;
- garantia;
- ajuste.

## Fiscal

Fluxos:

- emitir documento;
- autorizar via provider;
- consultar situacao;
- cancelar;
- inutilizar numeracao;
- distribuir DF-e por NSU;
- manifestar destinatario;
- importar XML de entrada.

XMLs sao armazenados por hash em pasta local/compartilhada e tratados como imutaveis.

## Financeiro

Fluxos:

- criar titulo a pagar;
- criar titulo a receber;
- registrar lancamento de caixa;
- conciliar extrato;
- acompanhar dashboard de vencidos e abertos.

## Servicos Locais

Administrador acompanha no desktop:

- API local;
- PostgreSQL;
- caminho fiscal;
- fila/outbox;
- backup;
- versao instalada;
- logs locais.

## Auditoria

Toda alteracao persistida gera registro com:

- entidade;
- id;
- operacao;
- antes;
- depois;
- usuario;
- IP;
- empresa;
- filial;
- data/hora.
