# Observabilidade

## Componentes

- Serilog para logs estruturados.
- OpenTelemetry para traces e metricas.
- OTLP Collector.
- Prometheus para coleta.
- Grafana para dashboards.
- Health checks da API.

## Health Checks

```http
GET /health/live
GET /health/ready
```

`live` indica processo vivo. `ready` deve ser usado pelo orquestrador para receber trafego.

## Logs

Campos esperados:

- timestamp;
- level;
- trace id;
- usuario;
- empresa;
- filial;
- rota;
- status;
- duracao;
- erro.

## Metricas Recomendadas

- requisicoes por rota;
- latencia p95/p99;
- erros 4xx/5xx;
- outbox pendente;
- outbox falha;
- tempo de autorizacao fiscal;
- rejeicoes fiscais por codigo;
- reservas de estoque negadas;
- titulos vencidos;
- uso de CPU/memoria;
- conexoes PostgreSQL;
- filas RabbitMQ.

## Alertas Minimos

- API indisponivel.
- Ready check falhando por mais de 5 minutos.
- Outbox com falhas crescentes.
- Provider fiscal com erro acima de limiar.
- Rejeicao fiscal acima do normal.
- Banco sem WAL archive.
- Redis indisponivel.
- RabbitMQ sem consumidor.

## Trace

Cada requisicao HTTP deve propagar trace para:

- consultas HTTP externas;
- provider fiscal;
- outbox;
- jobs.

## Dashboards

Dashboards recomendados:

- Operacao do ERP.
- Fiscal.
- Mensageria.
- Banco.
- Frontend/PWA.
- Seguranca e autenticacao.
