# Operacao

## Rotina Diaria

Equipe operacional deve acompanhar:

- health checks da API;
- filas RabbitMQ;
- outbox pendente/falha;
- rejeicoes fiscais;
- documentos em contingencia;
- estoque abaixo do minimo;
- vendas pendentes de fiscal;
- caixa aberto;
- titulos vencidos;
- backups e WAL archive.

## Abertura do Dia

1. Verificar `/health/ready`.
2. Verificar login OIDC.
3. Conferir dashboard operacional.
4. Conferir provider fiscal em homologacao/producao.
5. Conferir RabbitMQ.
6. Conferir se WAL archive esta ativo.
7. Conferir erros da noite.

## Fechamento do Dia

1. Fechar caixas.
2. Conferir documentos fiscais pendentes/rejeitados.
3. Conferir outbox sem falha.
4. Conferir conciliacao financeira.
5. Conferir backup agendado.
6. Exportar relatorios exigidos internamente.

## Monitoramento Fiscal

Alertas:

- documento rejeitado;
- timeout de autorizacao;
- contingencia ativa;
- NSU parado;
- manifestacao falhando;
- XML nao armazenado;
- provider indisponivel.

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
- rota;
- empresa;
- filial;
- documento fiscal;
- pedido;
- produto.

## Manutencao de Particoes

Criar particoes futuras antes da virada de ano para:

- auditoria;
- movimentos de estoque;
- documentos fiscais.

## Retencao

Definir politica por area:

- XML fiscal conforme obrigacao legal;
- auditoria conforme compliance;
- logs de aplicacao conforme seguranca;
- backups conforme RPO/RTO;
- eventos de outbox processados conforme volume.
