# Runbooks

## API Local Indisponivel

Sintomas:

- `/health/live` falha.
- desktop mostra erro de conexao.

Acoes:

1. Verificar se o servico/processo da API esta ativo.
2. Verificar logs da API.
3. Verificar PostgreSQL.
4. Verificar variaveis de ambiente.
5. Verificar firewall/porta em terminais.
6. Reiniciar servico se necessario.
7. Registrar incidente.

## Banco Indisponivel

Sintomas:

- ready check falha;
- erros Npgsql;
- timeouts em endpoints.

Acoes:

1. Verificar servico PostgreSQL.
2. Verificar disco.
3. Verificar conexoes e locks longos.
4. Verificar WAL/backup em perfil avancado.
5. Se perda de dados, iniciar restore.

## Terminal Cliente Nao Conecta

Acoes:

1. Confirmar IP/hostname do servidor da loja.
2. Testar `http://<servidor>:5000/health/ready`.
3. Verificar firewall no servidor.
4. Verificar se API escuta no endereco correto.
5. Conferir rede LAN/VPN.
6. Atualizar URL do servidor no desktop.

## Outbox Parado

Sintomas:

- mensagens sem `ProcessedAt`;
- `Attempts` aumentando;
- fila RabbitMQ crescendo quando habilitado.

Acoes:

1. Verificar logs `OutboxDispatcher`.
2. Em perfil avancado, verificar RabbitMQ.
3. Conferir credenciais.
4. Conferir contrato do evento que falhou.
5. Corrigir causa.
6. Reiniciar dispatcher/API se necessario.

## Documento Fiscal Rejeitado

Sintomas:

- status `rejected`;
- evento de autorizacao com mensagem do provider.

Acoes:

1. Ler evento fiscal.
2. Conferir regra fiscal aplicada.
3. Conferir cadastro produto: NCM, CEST, GTIN, origem.
4. Conferir cliente/empresa.
5. Corrigir dados.
6. Reemitir ou inutilizar conforme caso fiscal.

## Provider Fiscal ou Internet Indisponivel

Acoes:

1. Confirmar status do provider/SEFAZ.
2. Conferir certificado.
3. Conferir rede.
4. Ativar contingencia se permitido e aplicavel.
5. Registrar justificativa.
6. Reprocessar quando servico voltar.

## Venda Sem Reserva

Sintomas:

- erro de estoque insuficiente.

Acoes:

1. Conferir saldo fisico, reservado e disponivel.
2. Conferir local selecionado.
3. Verificar reservas expiradas.
4. Transferir estoque se necessario.
5. Reprocessar venda.

## Login/MFA Falhando

Acoes:

1. Verificar usuario em `identity.local_users`.
2. Verificar lockout por tentativas.
3. Verificar roles e permissoes.
4. Verificar configuracao de MFA.
5. Revisar policy na API.
6. Auditar `identity.authentication_logs`.

## Backup/Restore Local

Acoes resumidas:

1. Isolar ambiente.
2. Escolher backup.
3. Parar API se necessario.
4. Executar `scripts/restore-local.ps1`.
5. Iniciar PostgreSQL/API.
6. Validar consistencia.
7. Registrar RTO/RPO real.
