# Seguranca

## Objetivo

O ERP local protege dados fiscais, financeiros e operacionais mesmo rodando em servidor de loja e rede LAN. Nenhum endpoint deve confiar apenas no desktop; autorizacao e tenant sao validados na API.

## Autenticacao

- Autenticacao local propria por usuario e senha.
- Hash PBKDF2-SHA256 com salt.
- JWT local de curta duracao.
- Refresh token persistido por hash.
- Lockout por tentativas falhas.
- Logs de autenticacao em `identity.authentication_logs`.
- MFA obrigatorio para perfis sensiveis quando configurado.
- OIDC/Keycloak fica como provider externo opcional futuro, nao requisito de instalacao local.

## Autorizacao

Policies:

- `admin:tenant`: administracao.
- `sensitive:fiscal`: fiscal.
- `sensitive:finance`: financeiro.

RBAC por:

- empresa;
- filial;
- role;
- modulo;
- permissao;
- caixa/operacao quando aplicavel.

## Segregacao de Funcoes

Recomendado:

- fiscal cadastra regras, mas aprovacao de excecoes exige outro perfil;
- financeiro baixa titulos, mas nao altera regra fiscal;
- vendedor fecha venda, mas nao cancela documento fiscal sem autorizacao;
- admin tecnico nao deve acessar XML fiscal sem necessidade;
- operador de caixa nao deve alterar preco/custo sem permissao.

## Tenant Isolation

Camadas:

- claims do JWT local;
- headers `X-Company-Id` e `X-Branch-Id` em chamadas internas controladas;
- filtro EF por empresa/filial;
- variaveis de sessao PostgreSQL;
- RLS no banco.

## Rede Local

- expor API apenas na LAN quando houver terminais;
- liberar firewall somente na porta configurada;
- preferir usuario Windows sem privilegio administrativo para o servico;
- usar HTTPS local quando houver infraestrutura/certificado interno;
- registrar IP de origem na auditoria.

## OWASP ASVS

Checklist aplicado:

- autenticacao server-side;
- MFA para acoes sensiveis;
- controle de acesso server-side;
- validacao de entrada;
- tratamento padronizado de erro;
- logs sem segredos;
- rate limiting;
- segredos fora do codigo;
- auditoria before/after;
- CORS restrito ao desktop/dev local;
- health checks separados.

## Dados Sensiveis

Nao logar:

- tokens;
- senhas;
- certificados;
- private keys;
- XML completo com dados pessoais quando desnecessario;
- credenciais de providers.

## Certificados Digitais

Para producao fiscal:

- armazenar fora do repositorio;
- proteger por ACL e senha;
- preferir provider de assinatura ou armazenamento seguro quando possivel;
- controlar acesso por perfil;
- auditar uso;
- rotacionar antes do vencimento;
- testar assinatura em homologacao.

## Incidentes

Ver [Runbooks](runbooks.md).
