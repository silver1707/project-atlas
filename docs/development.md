# Contribuicao Tecnica

## Padroes

- Manter bounded contexts claros.
- Evitar acoplamento direto entre modulos.
- Preferir contratos e eventos.
- Usar `Guid.CreateVersion7()` para novos ids em dominio.
- Usar strings para identificadores fiscais/tecnicos.
- JSONB apenas para snapshots, payloads externos e dados flexiveis justificados.

## Novo Endpoint

1. Identificar modulo.
2. Criar request/response DTO.
3. Implementar regra no agregado ou servico.
4. Mapear endpoint no `IEndpointModule`.
5. Adicionar teste.
6. Atualizar docs.

## Nova Entidade

1. Definir ownership do bounded context.
2. Criar entidade.
3. Configurar mapping EF.
4. Criar migration SQL ou migration EF.
5. Avaliar RLS, indices e particao.
6. Avaliar auditoria.
7. Atualizar docs de dados.

## Novo Evento

1. Criar record implementando `IDomainEvent`.
2. Disparar por metodo de agregado.
3. Garantir persistencia via outbox.
4. Criar consumer externo ou documentar evento.
5. Adicionar teste.

## Novo Provider Fiscal

1. Implementar `IFiscalDocumentProvider`.
2. Configurar secrets.
3. Registrar DI.
4. Adicionar contratos de teste.
5. Homologar em ambiente oficial.
6. Documentar rejeicoes e contingencias.

## Frontend

- Usar componentes existentes.
- Manter UI operacional e densa.
- Garantir responsividade.
- Rodar lint/build.
- Validar com Playwright quando mexer em layout.
