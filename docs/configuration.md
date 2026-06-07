# Configuracao

## Arquivos

- `src/backend/AutoPartsErp.Api/appsettings.json`: padroes locais.
- `src/backend/AutoPartsErp.Api/appsettings.Development.json`: desenvolvimento.
- `.env.example`: variaveis para Compose.
- `deploy/local/store-server.env.example`: exemplo para servidor da loja.
- `src/desktop/AutoPartsErp.Desktop/src-tauri/tauri.conf.json`: pacote desktop.

## Atlas

```json
{
  "Atlas": {
    "ApplyMigrationsOnStartup": false,
    "InstallationProfile": "simple",
    "EnableRedis": false,
    "EnableRabbitMq": false,
    "EnableOpenTelemetry": false,
    "ImmutableXmlStoragePath": "C:/AutoPartsErp/FiscalXml",
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
    "Postgres": "Host=localhost;Port=5432;Database=autoparts_erp;Username=autoparts;Password=...",
    "Redis": ""
  }
}
```

Redis vazio/desabilitado ativa cache em memoria local.

## Autenticacao Local

```json
{
  "LocalAuthentication": {
    "Issuer": "AutoPartsErp.Local",
    "Audience": "AutoPartsErp.Desktop",
    "SigningKey": "trocar-por-chave-local-grande-e-secreta",
    "AccessTokenMinutes": 30,
    "RefreshTokenDays": 14,
    "MaxFailedAccessAttempts": 5,
    "LockoutMinutes": 15
  }
}
```

Obrigatorio em producao local:

- trocar `SigningKey`;
- proteger arquivo de ambiente;
- criar usuario administrador no setup;
- ativar MFA quando o recurso estiver configurado para perfis sensiveis.

## RabbitMQ Opcional

```json
{
  "Atlas": {
    "EnableRabbitMq": true
  },
  "RabbitMq": {
    "Host": "localhost",
    "VirtualHost": "/",
    "Username": "autoparts",
    "Password": "..."
  }
}
```

## Desktop

Variaveis Vite usadas no desenvolvimento:

```powershell
$env:VITE_API_BASE_URL="http://localhost:5000"
```

No produto instalado, a URL do servidor deve ser configurada pela tela de setup/status do desktop. Dados oficiais nao devem ficar em `localStorage`/IndexedDB.

## Fiscal Local

Configurar fora do codigo:

- caminho imutavel de XML;
- caminho de DANFE/DANFCE/DANFSE;
- caminho de backup fiscal;
- certificado A1/A3 quando houver provider real;
- ambiente homologacao/producao;
- serie, numero, CSC/ID CSC e parametros por filial;
- provider fiscal default.

## Segredos

Nunca versionar:

- senha de banco;
- chave JWT local;
- senha RabbitMQ/Redis;
- certificado digital;
- private key;
- token de provider fiscal;
- credencial de catalogo externo.

Use variaveis de ambiente, ACL do Windows, vault local/corporativo ou segredo do provedor quando houver integracao externa.
