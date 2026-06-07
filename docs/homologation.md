# Homologacao

## Objetivo

Validar que o ERP desktop local esta pronto para operacao real em loja antes de producao.

## Pre-Requisitos

- ambiente de homologacao isolado;
- PostgreSQL inicializado;
- API local instalada ou em execucao;
- app desktop instalado;
- usuarios, roles e permissoes;
- provider fiscal sandbox ou homologacao real;
- regras fiscais carregadas;
- seeds ou dados migrados;
- backup configurado;
- caminhos de XML/DANFE/backup definidos;
- firewall/porta testados para terminais.

## Checklist Tecnico

- API sobe.
- App desktop abre.
- Terminal cliente conecta na API do servidor.
- Swagger acessivel na rede autorizada.
- Login local funciona.
- MFA exigido quando configurado para fiscal/financeiro/admin.
- RLS isola empresas.
- Outbox processa eventos no perfil simples.
- RabbitMQ/Redis funcionam no perfil avancado.
- PostgreSQL com backup local.
- WAL/PITR testado quando configurado.
- Logs locais sao acessiveis ao administrador.

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
- emitir documento fiscal sandbox;
- cancelar documento fiscal sandbox;
- importar XML de entrada;
- criar titulo financeiro;
- conciliar extrato;
- fechar caixa.

## Checklist Fiscal

- regras fiscais reais carregadas e aprovadas;
- GTIN validado conforme regra vigente;
- CNPJ/CPF tratados como string;
- campos preparados para IBS/CBS;
- NFS-e Nacional avaliada quando houver servico;
- cancelamento e inutilizacao testados;
- contingencia testada;
- manifestacao do destinatario testada;
- DF-e por NSU testado;
- XML armazenado e recuperavel;
- DANFE/DANFCE/DANFSE conferidos;
- reprocessamento apos queda de internet testado.

## Evidencias

Salvar:

- versao do instalador;
- commit;
- data/hora dos testes;
- usuario executor;
- prints ou exports de resultados;
- protocolos fiscais;
- hashes de XML;
- relatorio de testes;
- parecer fiscal/contabil.

## Criterio de Aprovacao

Homologacao aprovada quando:

- todos os cenarios criticos passam;
- sem bug bloqueante;
- seguranca validada;
- fiscal aprovado pelo responsavel;
- backup/restore validado;
- instalacao servidor e terminal testadas;
- runbooks conhecidos pela operacao.
