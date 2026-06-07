# Backup e Continuidade

## Objetivos

- RPO definido por negocio.
- RTO validado em teste.
- Backup local automatico.
- Recuperacao point-in-time com WAL quando configurado.
- Retencao de XML fiscal conforme obrigacoes legais.

## PostgreSQL

Instalacao simples:

- backup diario com `pg_dump`;
- copia para disco externo, NAS ou pasta protegida;
- teste de restore mensal.

Instalacao avancada:

- base backup periodico;
- WAL archive continuo;
- PITR testado;
- replica logica para analitico;
- monitoramento de slots e WAL.

Scripts:

- `scripts/backup-local.ps1`;
- `scripts/restore-local.ps1`;
- `scripts/backup-pitr.sh`;
- `scripts/restore-pitr.sh`.

## XML Fiscal

XML fica fora do banco, em caminho imutavel por hash. Recomendado:

- pasta dedicada em `C:\AutoPartsErp\FiscalXml` ou compartilhamento controlado;
- backup incremental;
- criptografia em repouso quando possivel;
- retencao legal;
- verificacao periodica de hash;
- acesso controlado por administrador/fiscal.

## Desktop e API

O desktop pode ser reinstalado sem perda de dados, desde que PostgreSQL, XML fiscal e backups estejam preservados. A API deve ter suas variaveis e segredos guardados fora do repositorio.

## Redis

Redis e cache opcional. Perda nao deve perder dado de negocio. Habilitar persistencia apenas se fizer sentido operacional.

## RabbitMQ

Mensagens publicadas via outbox. Se RabbitMQ falhar:

- outbox permanece pendente;
- dispatcher tenta novamente;
- apos retorno, eventos sao publicados.

## Teste de Restore

Mensalmente:

1. restaurar backup em ambiente isolado;
2. aplicar WAL ate data/hora alvo se houver PITR;
3. conferir schemas;
4. conferir produto, venda e documento fiscal;
5. conferir XML por hash;
6. abrir app desktop apontando para o ambiente restaurado;
7. registrar tempo de recuperacao.

## Continuidade

Prioridade de recuperacao:

1. PostgreSQL;
2. XML fiscal;
3. API local;
4. desktop servidor;
5. terminais cliente;
6. RabbitMQ quando habilitado;
7. Redis quando habilitado;
8. observabilidade.
