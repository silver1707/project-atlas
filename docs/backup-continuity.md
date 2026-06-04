# Backup e Continuidade

## Objetivos

- RPO definido por negocio.
- RTO validado em teste.
- Recuperacao point-in-time com WAL.
- Retencao de XML fiscal conforme obrigacoes legais.

## PostgreSQL

Estrategia:

- base backup periodico;
- WAL archive continuo;
- PITR testado;
- replica logica para analitico;
- monitoramento de slots e WAL.

Scripts:

- `scripts/backup-pitr.sh`;
- `scripts/restore-pitr.sh`.

## XML Fiscal

XML fica fora do banco, em caminho imutavel por hash. Recomendado:

- volume dedicado;
- backup incremental;
- criptografia em repouso;
- retencao legal;
- verificacao periodica de hash;
- acesso controlado.

## Redis

Redis e cache. Perda nao deve perder dado de negocio. Habilitar AOF apenas para estabilidade operacional local.

## RabbitMQ

Mensagens publicadas via outbox. Se RabbitMQ falhar:

- outbox permanece pendente;
- dispatcher tenta novamente;
- apos retorno, eventos sao publicados.

## Teste de Restore

Mensalmente:

1. restaurar base backup em ambiente isolado;
2. aplicar WAL ate data/hora alvo;
3. conferir schemas;
4. conferir produto, venda e documento fiscal;
5. conferir XML por hash;
6. registrar tempo de recuperacao.

## Continuidade

Prioridade de recuperacao:

1. PostgreSQL;
2. XML fiscal;
3. API;
4. IdP;
5. RabbitMQ;
6. Redis;
7. Web;
8. observabilidade.
