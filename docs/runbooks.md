# Runbooks

## API Indisponivel

Sintomas:

- `/health/live` falha.
- frontend mostra erro de conexao.

Acoes:

1. Verificar logs da API.
2. Verificar PostgreSQL.
3. Verificar variaveis de ambiente.
4. Reiniciar pod/container se necessario.
5. Conferir readiness.
6. Registrar incidente.

## Banco Indisponivel

Sintomas:

- ready check falha;
- erros Npgsql;
- timeouts em endpoints.

Acoes:

1. Verificar conexoes.
2. Verificar disco.
3. Verificar locks longos.
4. Verificar WAL.
5. Acionar DBA.
6. Se perda de dados, iniciar procedimento PITR.

## Outbox Parado

Sintomas:

- mensagens sem `ProcessedAt`;
- `Attempts` aumentando;
- RabbitMQ com fila crescendo.

Acoes:

1. Verificar RabbitMQ.
2. Verificar logs `OutboxDispatcher`.
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

## Provider Fiscal Indisponivel

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

1. Verificar Keycloak.
2. Verificar realm `atlas`.
3. Verificar client `atlas-web`.
4. Verificar roles.
5. Verificar claim `mfa`.
6. Revisar policy na API.

## Restore PITR

Acoes resumidas:

1. Isolar ambiente.
2. Escolher base backup.
3. Escolher ponto de recuperacao.
4. Executar `scripts/restore-pitr.sh`.
5. Iniciar PostgreSQL.
6. Validar consistencia.
7. Apontar aplicacao.
8. Registrar RTO/RPO real.
