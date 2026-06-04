# Manual Funcional

## Papeis

- Administrador: configura empresas, filiais, perfis, parametros e adapters.
- Vendedor de balcao: busca produtos, monta venda, consulta estoque e fecha pedido.
- Estoquista: recebe, transfere, reserva, confere e separa mercadorias.
- Comprador: cadastra fornecedor, emite pedido e usa compra sugerida.
- Fiscal: parametriza regras, emite, consulta, cancela, inutiliza e importa XML.
- Financeiro: gerencia contas a pagar, receber, caixa e conciliacao.
- CRM: cadastra clientes, frota e interacoes.

## Padrao de Operacao

Cada operacao usa contexto de empresa e filial. No frontend local, esse contexto vem de variaveis `VITE_COMPANY_ID` e `VITE_BRANCH_ID`. Na API, o contexto vem dos headers `X-Company-Id`, `X-Branch-Id` ou claims OIDC.

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
- codigos tecnicos.

## Vendas

Venda de balcao:

1. Buscar produto.
2. Adicionar ao carrinho.
3. Validar local com estoque disponivel.
4. Fechar venda.
5. Sistema cria pedido e reservas.
6. Pedido fica pronto para emissao fiscal.

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

XMLs sao armazenados por hash e tratados como imutaveis.

## Financeiro

Fluxos:

- criar titulo a pagar;
- criar titulo a receber;
- registrar lancamento de caixa;
- conciliar extrato;
- acompanhar dashboard de vencidos e abertos.

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
