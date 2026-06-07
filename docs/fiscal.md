# Fiscal Tecnico

## Status da Consulta

Fontes oficiais consultadas em 04/06/2026:

- [Portal Nacional da NF-e](https://www.nfe.fazenda.gov.br/portal/principal.aspx/consulta.aspx).
- [Notas Tecnicas NF-e/NFC-e](https://www.nfe.fazenda.gov.br/portal/consulta.aspx/listaConteudo.aspx?tipoConteudo=04BIflQt1aY%3D).
- [Ministerio da Fazenda - NFS-e Nacional obrigatoria para optantes do Simples](https://www.gov.br/fazenda/pt-br/assuntos/noticias/2026/abril/nota-fiscal-de-servico-eletronica-de-padrao-nacional-sera-obrigatoria-para-optantes-do-simples-nacional/).
- [Receita Federal - CNPJ alfanumerico](https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/perguntas-e-respostas/cnpj/cnpj-alfanumerico.pdf/@@download/file).

NTs consideradas na arquitetura:

- NT 2025.002 v1.40: Reforma Tributaria do Consumo, IBS e CBS em NF-e/NFC-e.
- NT 2026.004 v1.00: adequacoes para CNPJ alfanumerico.
- NT 2020.001 v1.60: manifestacao do destinatario e PAA 2026.
- NT 2014.002 v1.30: distribuicao DF-e por NSU.
- NT 2021.003 v1.40: validacao GTIN.

Antes de homologacao fiscal real, revisar as versoes vigentes novamente.

## Principios

- Nenhum imposto hardcoded.
- Documento fiscal e agregado proprio.
- XML e eventos sao imutaveis por hash e historico.
- Providers assinam, autorizam, consultam, cancelam, inutilizam, distribuem e manifestam.
- Dominio nao depende de SEFAZ direta, prefeitura, NFS-e Nacional ou provedor privado.

## Operacao Local

No produto desktop local, o fiscal deve operar mesmo com oscilacao de internet, respeitando limites legais e tecnicos de cada documento:

- XMLs, eventos e protocolos ficam em pasta local/compartilhada protegida.
- Certificado A1/A3 e senha ficam fora do codigo, com acesso restrito.
- Emissao, consulta, cancelamento, inutilizacao, contingencia e reprocessamento entram em fila local/outbox.
- Falha de internet deve registrar tentativa, payload, erro, usuario, empresa, filial e horario.
- Reprocessamento deve ser idempotente por chave/documento/evento.
- DANFE/DANFCE/DANFSE devem ser gerados para visualizacao/impressao a partir do desktop, sem acoplar dominio a driver local.

## Escopo

Contratos preparados para:

- NF-e modelo 55.
- NFC-e modelo 65.
- NFS-e Nacional quando houver servicos.
- Autorizacao.
- Consulta.
- Cancelamento.
- Inutilizacao.
- Contingencia.
- DANFE, DANFCE e DANFSE via provider/renderizador.
- Armazenamento imutavel de XML.
- Historico de protocolos.
- Download/reprocessamento.
- Distribuicao DF-e por NSU.
- Manifestacao do destinatario.
- Importacao de XML de entrada.
- Conciliacao de compras por XML.

## Motor Fiscal

Tabela `fiscal.fiscal_rules`.

Parametros:

- UF origem;
- UF destino;
- vigencia;
- NCM;
- CEST;
- CFOP;
- CST;
- CSOSN;
- regime tributario;
- origem da mercadoria;
- tipo de operacao;
- beneficios;
- excecoes;
- base legal;
- aliquotas ICMS, ICMS-ST, PIS, COFINS, IBS e CBS.

Processo:

1. Montar `FiscalOperationContext`.
2. Buscar regras ativas por tenant e vigencia.
3. Filtrar por UF, regime e operacao.
4. Escolher regra mais especifica por NCM, CEST e excecao.
5. Calcular tributos.
6. Persistir `TaxSnapshot`.

Os seeds usam regra de homologacao com aliquotas zeradas para fluxo tecnico. Producao exige importacao e aprovacao fiscal/contabil.

## Provider

Contrato:

```csharp
public interface IFiscalDocumentProvider
{
    Task<ProviderOperationResult> AuthorizeAsync(FiscalDocument document, string xml, CancellationToken ct);
    Task<ProviderOperationResult> CancelAsync(FiscalDocument document, CancelFiscalDocumentRequest request, CancellationToken ct);
    Task<ProviderOperationResult> QueryAsync(FiscalDocument document, CancellationToken ct);
    Task<ProviderOperationResult> InutilizeAsync(InutilizeNumberRequest request, CancellationToken ct);
    Task<DfeDistributionResult> DistributeDfeAsync(DfeDistributionRequest request, CancellationToken ct);
    Task<ProviderOperationResult> ManifestRecipientAsync(RecipientManifestationRequest request, CancellationToken ct);
}
```

Provider real deve implementar:

- certificado A1/A3 ou assinatura remota;
- assinatura XML;
- validacao XSD;
- comunicacao SOAP/REST;
- QR Code NFC-e;
- DANFE/DANFCE/DANFSE;
- contingencia;
- retries idempotentes;
- armazenamento de retorno bruto;
- monitoramento de latencia e rejeicoes.

## XML Imutavel

`ImmutableXmlStore`:

- calcula SHA-256 do XML;
- cria caminho por empresa/modelo/ano/mes;
- grava arquivo se nao existir;
- marca como somente leitura;
- salva hash e URI no documento fiscal.

## CNPJ Alfanumerico

Com entrada prevista para julho de 2026, CNPJ deve ser tratado como string em todo o sistema. Validadores e mascaras devem aceitar formato alfanumerico quando aplicavel. O sistema ja evita armazenamento numerico de CNPJ.

## IBS e CBS

O motor fiscal possui campos `IbsRate` e `CbsRate` e grava valores no snapshot. A geracao XML final deve acompanhar layout vigente da NT aplicavel no provider fiscal.

## NFS-e Nacional

Segundo publicacao oficial do Ministerio da Fazenda, a emissao da NFS-e de padrao nacional para ME/EPP optantes do Simples Nacional sera obrigatoria a partir de 1 de setembro de 2026, por emissor web ou API. O sistema modela NFS-e como documento provider-based, permitindo adapter para API nacional.

## Distribuicao DF-e por NSU

O cursor `fiscal.dfe_distribution_cursors` guarda:

- CNPJ;
- ambiente;
- ultimo NSU;
- max NSU;
- ultima execucao.

O provider executa a chamada real e retorna `DfeDistributionResult`.

## Manifestacao do Destinatario

Contrato `RecipientManifestationRequest` inclui:

- provider;
- chave;
- tipo de evento;
- justificativa;
- ambiente.

Eventos devem ser persistidos no historico fiscal.

## Homologacao Fiscal Real

Checklist:

- certificado digital valido;
- ambiente de homologacao SEFAZ/NFS-e;
- schemas atualizados;
- validacao GTIN;
- CNPJ alfanumerico;
- IBS/CBS;
- cancelamento;
- inutilizacao;
- contingencia;
- DFe por NSU;
- manifestacao;
- DANFE/DANFCE/DANFSE;
- armazenamento imutavel;
- retencao de XML;
- conciliacao de XML de entrada;
- parecer contabil das regras.
