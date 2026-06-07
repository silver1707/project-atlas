# Operacao Local

## Rotina Diaria

Equipe operacional deve acompanhar:

- status do servico da API local;
- conexao com PostgreSQL;
- outbox pendente/falha;
- filas RabbitMQ quando habilitado;
- rejeicoes fiscais;
- documentos em contingencia;
- internet e provider fiscal;
- estoque abaixo do minimo;
- vendas pendentes de fiscal;
- caixa aberto;
- titulos vencidos;
- backups locais e WAL archive quando configurado.

## Abertura do Dia

1. Abrir o app desktop no servidor.
2. Verificar painel de status dos servicos locais.
3. Verificar `/health/ready`.
4. Conferir provider fiscal ou internet, quando houver emissao no dia.
5. Conferir erros da noite.
6. Conferir ultimo backup.
7. Abrir caixa.

## Fechamento do Dia

1. Fechar caixas.
2. Conferir documentos fiscais pendentes/rejeitados.
3. Conferir outbox sem falha.
4. Conferir conciliacao financeira.
5. Executar ou validar backup.
6. Exportar relatorios exigidos internamente.

## Monitoramento Fiscal

Alertas:

- documento rejeitado;
- timeout de autorizacao;
- contingencia ativa;
- NSU parado;
- manifestacao falhando;
- XML nao armazenado;
- provider indisponivel;
- pasta fiscal sem permissao de escrita.

## Monitoramento de Estoque

Alertas:

- disponibilidade negativa;
- reservado maior que fisico;
- item abaixo do minimo;
- picking parado;
- transferencia sem confirmacao.

## Monitoramento Financeiro

Alertas:

- caixa aberto fora de horario;
- diferenca de caixa;
- titulo vencido;
- conciliacao com alto volume sem match;
- usuario sensivel sem MFA.

## Logs

Logs devem ser consultados por:

- trace id;
- usuario;
- IP/terminal;
- rota;
- empresa;
- filial;
- documento fiscal;
- pedido;
- produto.

## Manutencao

- criar particoes futuras antes da virada de ano para auditoria, movimentos e documentos fiscais;
- testar restore mensalmente;
- revisar espaco em disco para banco, XML e backups;
- revisar certificados digitais antes do vencimento;
- arquivar XML e logs conforme politica legal/contratual.
