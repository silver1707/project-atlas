# Configuracao

## Arquivos

- `src/Atlas.Api/appsettings.json`: padroes.
- `src/Atlas.Api/appsettings.Development.json`: desenvolvimento.
- `.env.example`: variaveis para Compose.
- `docker-compose.yml`: stack local.
- `docker-compose.hml.yml`: homologacao.

## Atlas

```json
{
  "Atlas": {
    "ApplyMigrationsOnStartup": false,
    "ImmutableXmlStoragePath": "/var/lib/atlas/fiscal-xml",
    "DefaultFiscalProvider": "sandbox",
    "DefaultCompanyId": "11111111-1111-1111-1111-111111111111",
    "DefaultBranchId": "22222222-2222-2222-2222-222222222222"
  }
}
```

## Connection Strings

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=postgres;Port=5432;Database=atlas;Username=atlas;Password=...",
    "Redis": "redis:6379"
  }
}
```

## OIDC

```json
{
  "Authentication": {
    "Authority": "http://keycloak:8080/realms/atlas",
    "Audience": "atlas-api",
    "RequireHttpsMetadata": false
  }
}
```

Em producao:

- `RequireHttpsMetadata=true`;
- issuer HTTPS;
- client separado para web e API;
- MFA configurado no IdP.

## RabbitMQ

```json
{
  "RabbitMq": {
    "Host": "rabbitmq",
    "VirtualHost": "/",
    "Username": "atlas",
    "Password": "..."
  }
}
```

## Frontend

```bash
VITE_API_BASE_URL=http://localhost:5000
VITE_OIDC_AUTHORITY=http://localhost:8080/realms/atlas
VITE_OIDC_CLIENT_ID=atlas-web
VITE_COMPANY_ID=11111111-1111-1111-1111-111111111111
VITE_BRANCH_ID=22222222-2222-2222-2222-222222222222
```

## Segredos

Nunca versionar:

- senha de banco;
- senha RabbitMQ;
- certificado digital;
- private key;
- token de provider fiscal;
- credencial de catalogo externo.

Use:

- variaveis de ambiente;
- Kubernetes Secrets;
- vault corporativo;
- secret manager do provedor cloud.
