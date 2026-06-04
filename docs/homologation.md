# Homologacao

## Objetivo

Validar que o sistema esta pronto para uso operacional e fiscal antes de producao.

## Pre-Requisitos

- ambiente de homologacao isolado;
- banco inicializado;
- Keycloak configurado;
- usuarios e roles;
- provider fiscal sandbox ou homologacao real;
- regras fiscais carregadas;
- seeds ou dados migrados;
- backup configurado;
- observabilidade ativa.

## Checklist Tecnico

- API sobe.
- Web PWA instala.
- Swagger acessivel.
- Login OIDC funciona.
- MFA exigido para fiscal/financeiro/admin.
- RLS isola empresas.
- Outbox publica no RabbitMQ.
- Redis acessivel.
- PostgreSQL com WAL/logical replication.
- Backup base executado.
- Restore testado.

## Checklist Funcional

- cadastrar produto;
- adicionar aplicacao;
- adicionar equivalente;
- buscar por codigo;
- buscar por compatibilidade;
- cadastrar fornecedor;
- criar pedido de compra;
- receber estoque;
- transferir estoque;
- vender no balcao;
- reservar estoque;
- emitir documento fiscal;
- cancelar documento fiscal;
- importar XML de entrada;
- criar titulo financeiro;
- conciliar extrato;
- fechar caixa.

## Checklist Fiscal

- regras fiscais reais carregadas e aprovadas;
- GTIN validado conforme regra vigente;
- CNPJ alfanumerico testado;
- IBS/CBS avaliados conforme NT vigente;
- NFS-e Nacional avaliada quando houver servico;
- cancelamento e inutilizacao testados;
- contingencia testada;
- manifestacao do destinatario testada;
- DF-e por NSU testado;
- XML armazenado e recuperavel;
- DANFE/DANFCE/DANFSE conferidos.

## Evidencias

Salvar:

- versao da imagem;
- commit;
- data/hora dos testes;
- usuario executor;
- prints ou exports de resultados;
- protocolos fiscais;
- hashes de XML;
- relatorio de testes;
- parecer fiscal.

## Criterio de Aprovacao

Homologacao aprovada quando:

- todos os cenarios criticos passam;
- sem bug bloqueante;
- seguranca validada;
- fiscal assinado por responsavel;
- backup/restore validado;
- runbooks conhecidos pela operacao.
