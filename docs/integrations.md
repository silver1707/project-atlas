# Integracoes e Adapters

## Principio

Fornecedor externo nunca deve entrar no dominio. O dominio conhece contratos internos. Implementacoes externas ficam em adapters.

## Tipos

- `aftermarket_catalog`: catalogos como TecDoc/TecAlliance ou outros.
- `fiscal_provider`: SEFAZ direta, NFS-e Nacional, prefeitura ou provedor fiscal.
- `importer`: tabelas fiscais, produtos, fornecedores.
- `webhook`: notificacoes externas.
- `analytics`: replicacao ou exportacao para BI.

## Registro

```http
POST /api/integrations/adapters
```

Campos:

- `type`;
- `name`;
- `enabled`;
- `configurationJson`.

Segredos nao devem ficar em `configurationJson`; use secret manager e referencia por chave.

## Catalogo Aftermarket

Interface:

```csharp
public interface IAftermarketCatalogAdapter
{
    string Name { get; }
    Task<ExternalCatalogSearchResult> SearchAsync(ExternalCatalogSearchRequest request, CancellationToken ct);
}
```

Responsabilidades do adapter:

- autenticar no fornecedor;
- converter VIN em criterios;
- traduzir fitment externo;
- retornar resultados sem poluir o produto mestre;
- lidar com rate limit e retries.

## Fiscal Provider

Ver [Fiscal tecnico](fiscal.md).

## Outbox

Tabela:

```sql
integrations.outbox_messages
```

Campos:

- id;
- empresa;
- filial;
- tipo de evento;
- payload;
- headers;
- data de ocorrencia;
- data processada;
- falha;
- tentativas.

## Padrao de Novo Adapter

1. Criar interface se nao existir.
2. Implementar adapter no modulo `Integrations` ou pacote futuro.
3. Registrar DI no `Program.cs`.
4. Adicionar contrato de teste.
5. Documentar configuracao.
6. Garantir timeouts e circuit breaker quando houver provider real.
