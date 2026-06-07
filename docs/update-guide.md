# Guia de Atualizacao

## Antes de Atualizar

1. Registrar versao atual.
2. Fazer backup do banco.
3. Fazer backup dos XMLs fiscais.
4. Conferir espaco em disco.
5. Ler notas da versao.
6. Agendar janela fora do horario de pico.

## Atualizacao Simples

1. Parar app desktop nos terminais.
2. Parar API local.
3. Aplicar migrations, se houver.
4. Instalar nova versao da API.
5. Instalar novo desktop no servidor.
6. Atualizar terminais cliente.
7. Rodar smoke test.

## Atualizacao Avancada

Adicionar:

- verificar RabbitMQ/Redis;
- pausar workers separados, se existirem;
- confirmar compatibilidade de eventos;
- validar dashboards e alertas.

## Smoke Test

- health ready;
- login local;
- busca catalogo;
- venda sandbox;
- estoque;
- financeiro;
- fiscal sandbox;
- backup pos-atualizacao;
- terminal cliente.

## Rollback

- reinstalar versao anterior;
- restaurar banco se migration nao for reversivel;
- restaurar XML somente se houver corrompimento comprovado;
- registrar causa e evidencia.
