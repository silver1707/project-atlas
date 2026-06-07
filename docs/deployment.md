# Implantacao Local

## Artefatos

- API local: `src/backend/AutoPartsErp.Api`.
- Desktop Windows: `src/desktop/AutoPartsErp.Desktop`.
- Banco: `database/migrations`.
- Servidor local: `deploy/local`.
- Homologacao/dev: `docker-compose.yml` e `docker-compose.hml.yml`.
- Pipeline: `.github/workflows/ci.yml`.

## Instalacao Simples

Componentes:

- AutoParts ERP Desktop instalado no servidor da loja.
- ASP.NET Core API como processo local ou servico Windows.
- PostgreSQL instalado na mesma maquina ou servidor local.
- Jobs internos no backend.

Uso indicado: uma loja pequena, um caixa principal ou instalacao inicial de homologacao.

## Instalacao Avancada

Componentes adicionais:

- Redis.
- RabbitMQ.
- workers separados, quando criados.
- OpenTelemetry, Prometheus e Grafana.
- WAL archive/PITR e backup externo.

Uso indicado: rede com varios terminais, alto volume de integracoes fiscais/catalogo ou exigencia operacional mais forte.

## Servidor de Loja

1. Instalar PostgreSQL.
2. Criar banco e usuario.
3. Rodar migrations e seeds.
4. Instalar/publicar API.
5. Configurar `store-server.env`.
6. Liberar porta da API no firewall, se houver terminais.
7. Configurar caminhos de XML, DANFE/DANFCE e backup.
8. Instalar desktop no servidor.
9. Executar setup inicial da empresa.

## Terminais Cliente

Cada terminal instala apenas o app desktop e aponta a URL da API para o servidor da loja, por exemplo:

```text
http://192.168.0.10:5000
```

O terminal nao deve ter banco proprio nem gravar dados oficiais localmente.

## Pipeline

Etapas:

1. restore .NET;
2. build backend;
3. testes backend;
4. install desktop renderer;
5. lint desktop renderer;
6. build desktop renderer;
7. build instalador Tauri em runner Windows;
8. build imagem da API para homologacao;
9. analise CodeQL.

## Smoke Test Pos-Instalacao

1. `/health/live` retorna 200.
2. `/health/ready` retorna 200.
3. Swagger abre.
4. App desktop conecta na API.
5. Login local funciona.
6. Busca catalogo retorna dados seedados.
7. Venda de balcao cria pedido/reserva.
8. Provider fiscal sandbox responde.
9. Backup local executa.
10. Logs locais sao encontrados pelo administrador.

## Rollback

- manter instalador anterior;
- fazer backup antes de migrations;
- migrations destrutivas exigem plano de retorno por restore;
- registrar versao instalada, commit, data, operador e resultado do smoke test.
