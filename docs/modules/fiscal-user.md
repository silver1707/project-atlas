# Fiscal - Manual do Usuario

## Escopo

O modulo fiscal controla documentos de saida, entrada, eventos, XMLs, regras e integracoes com providers.

## Documentos

Modelos suportados por contrato:

- NF-e modelo 55.
- NFC-e modelo 65.
- NFS-e Nacional para servicos.

## Acoes

- Emitir.
- Autorizar.
- Consultar.
- Cancelar.
- Inutilizar numeracao.
- Importar XML de entrada.
- Distribuir DF-e por NSU.
- Manifestar destinatario.
- Reprocessar eventos.

## Emissao

```http
POST /api/fiscal/documents/issue
```

Campos importantes:

- pedido de venda;
- modelo;
- serie;
- numero;
- ambiente;
- provider;
- UF origem/destino;
- regime;
- tipo de operacao;
- contingencia e motivo quando aplicavel.

## Regras Fiscais

```http
POST /api/fiscal/rules
```

Parametros:

- UF origem/destino;
- vigencia;
- NCM;
- CEST;
- CFOP;
- CST;
- CSOSN;
- origem;
- regime;
- beneficios;
- excecoes;
- aliquotas ICMS, ST, PIS, COFINS, IBS, CBS.

## XML

O XML e armazenado em caminho imutavel por hash SHA-256. O sistema grava tambem eventos, protocolos e payloads de provider.

## Homologacao

- emitir NF-e sandbox;
- consultar documento;
- cancelar documento;
- inutilizar faixa;
- importar XML de entrada;
- executar manifestacao;
- executar distribuicao DF-e por NSU;
- validar auditoria.
