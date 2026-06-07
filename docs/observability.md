# Observabilidade

## Componentes

- Serilog para logs estruturados.
- Health checks da API local.
- Correlation/trace id por request.
- OpenTelemetry, Prometheus e Grafana opcionais no perfil avancado.
- Logs locais acessiveis ao administrador da loja.

## Health Checks

```http
GET /health/live
GET /health/ready
```

`live` indica processo vivo. `ready` indica que a API consegue operar, incluindo dependencias essenciais como PostgreSQL.

## Logs

Campos esperados:

- timestamp;
- level;
- trace id;
- usuario;
- empresa;
- filial;
- IP/terminal;
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
- disco livre para banco/XML/backup;
- conexoes PostgreSQL;
- filas RabbitMQ quando habilitado.

## Alertas Minimos

- API indisponivel.
- Ready check falhando por mais de 5 minutos.
- Banco indisponivel.
- Pasta fiscal sem escrita.
- Backup local falhando.
- Outbox com falhas crescentes.
- Provider fiscal com erro acima de limiar.
- Rejeicao fiscal acima do normal.
- Redis/RabbitMQ indisponivel quando habilitados.

## Dashboards

Dashboards recomendados:

- Operacao da loja.
- Fiscal.
- Mensageria opcional.
- Banco.
- Desktop/terminais.
- Seguranca e autenticacao.
