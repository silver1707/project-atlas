# Configuracao Inicial

## Sequencia Recomendada

1. Testar conexao com API local.
2. Testar conexao com PostgreSQL.
3. Criar usuario administrador inicial, se ainda nao existir.
4. Configurar empresa.
5. Configurar filial.
6. Configurar locais de estoque.
7. Configurar caixa e formas de pagamento.
8. Configurar parametros fiscais.
9. Configurar caminhos de XML, DANFE/DANFCE e backup.
10. Configurar provider fiscal sandbox/homologacao.
11. Cadastrar usuarios, roles e permissoes.
12. Importar ou cadastrar produtos.
13. Rodar smoke test de venda, estoque e fiscal sandbox.

## Empresa e Filial

Campos sensiveis:

- CNPJ como string;
- inscricao estadual como string;
- regime tributario;
- UF fiscal;
- serie fiscal;
- parametros de caixa;
- configuracoes de estoque negativo;
- politica de desconto.

## Fiscal

Antes de producao real:

- obter certificado digital;
- validar parametros com contador;
- carregar regras fiscais por vigencia;
- validar ambiente de homologacao;
- testar cancelamento, inutilizacao, contingencia e reprocessamento;
- conferir DANFE/DANFCE/DANFSE;
- validar armazenamento e backup de XML.

## Impressao e Arquivos

Configurar:

- pasta de XML autorizado;
- pasta de eventos fiscais;
- pasta de contingencia;
- pasta de DANFE/DANFCE/DANFSE;
- pasta de backup;
- impressoras/visualizadores usados pelos operadores.

## Usuarios

Criar perfis:

- administrador;
- gerente;
- vendedor;
- estoquista;
- financeiro;
- fiscal.

Perfis sensiveis devem usar MFA quando o recurso estiver habilitado.
