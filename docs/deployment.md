# Deploy

## Artefatos

- API: `src/Atlas.Api/Dockerfile`.
- Web: `web/Dockerfile`.
- Local: `docker-compose.yml`.
- Homologacao: `docker-compose.hml.yml`.
- Kubernetes: `deploy/k8s`.
- CI: `.github/workflows/ci.yml`.

## Pipeline

Etapas:

1. checkout;
2. restore .NET;
3. build backend;
4. testes backend;
5. install frontend;
6. lint frontend;
7. build frontend;
8. build imagens;
9. analise CodeQL.

## Homologacao

Usar imagens imutaveis. Nao aplicar migration automaticamente sem controle. Executar scripts de banco em janela de deploy e manter backup antes da mudanca.

## Kubernetes

Recursos:

- `api.yaml`: deployment e service da API.
- `web.yaml`: deployment e service do frontend.
- `ingress.yaml`: roteamento TLS.
- `postgres-backup-cronjob.yaml`: base backup.

Antes de aplicar:

- trocar imagens por tags/digests do registry;
- criar `atlas-api-secrets`;
- criar `atlas-api-config`;
- criar segredo TLS;
- configurar ingress controller;
- configurar volumes.

## Variaveis de Producao

Obrigatorias:

- connection string PostgreSQL;
- Redis;
- RabbitMQ;
- authority OIDC HTTPS;
- audience;
- OTLP endpoint;
- caminho de XML imutavel;
- provider fiscal default.

## Smoke Test Pos-Deploy

1. `/health/live` retorna 200.
2. `/health/ready` retorna 200.
3. Swagger abre.
4. Login OIDC funciona.
5. Busca catalogo retorna seed ou dados migrados.
6. Outbox publica evento.
7. Provider fiscal sandbox responde.
8. Logs chegam ao collector.

## Rollback

- rollback de imagem e simples se migration for backward compatible;
- migration destrutiva exige plano de retorno por backup/PITR;
- nunca rodar `git reset` ou comandos destrutivos em producao;
- manter changelog de banco.
