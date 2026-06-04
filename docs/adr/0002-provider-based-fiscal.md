# ADR 0002: Fiscal Provider-Based

## Status

Aceito

## Contexto

NF-e, NFC-e e NFS-e mudam por NT, UF, municipio, provider, assinatura e contingencia. Acoplar dominio a um fornecedor criaria risco de lock-in.

## Decisao

O dominio cria documentos fiscais e eventos. Providers executam autorizacao, assinatura, consulta, cancelamento, inutilizacao, DF-e e manifestacao.

## Consequencias

- Troca de provider sem reescrever agregados.
- Mais contratos de teste.
- Renderizadores de DANFE/DANFCE/DANFSE podem evoluir isoladamente.
