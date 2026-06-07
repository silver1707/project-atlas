# Instalacao em Servidor Local

## Objetivo

Instalar a maquina principal da loja, responsavel por PostgreSQL, API local, arquivos fiscais, backups e, opcionalmente, Redis/RabbitMQ.

## Pre-Requisitos

- Windows 10/11 Pro ou Windows Server.
- Usuario administrador para instalacao.
- Disco com espaco para banco, XML fiscal e backups.
- PostgreSQL instalado.
- Porta da API definida, padrao `5000`.
- Rede LAN estavel se houver terminais.

## Passos

1. Instalar PostgreSQL.
2. Criar usuario `autoparts` e banco `autoparts_erp`.
3. Copiar `.env.example` ou `deploy/local/store-server.env.example` para um arquivo local seguro.
4. Ajustar connection string, chave JWT local, caminhos fiscais e perfil de instalacao.
5. Rodar migrations:

```powershell
.\scripts\migrate-database.ps1
```

6. Publicar API:

```powershell
.\scripts\build-backend.ps1
```

7. Registrar API como servico Windows usando o modelo em `deploy/local/autoparts-erp-api.service.example.xml` ou ferramenta equivalente.
8. Liberar firewall, se terminais acessarem a API:

```powershell
.\deploy\local\windows-firewall.ps1 -Port 5000
```

9. Instalar o app desktop no servidor.
10. Abrir o desktop, testar conexao e executar configuracao inicial.

## Validacao

- `http://localhost:5000/health/ready` retorna 200.
- Swagger abre localmente.
- Desktop entra com usuario admin.
- Busca de produto funciona.
- Backup local executa.
- Pasta fiscal aceita gravacao.
