# Instalacao em Terminal Cliente

## Objetivo

Instalar um terminal de balcao, estoque, compras ou financeiro que acessa o servidor da loja pela rede local.

## Pre-Requisitos

- Windows 10/11.
- App desktop instalado.
- IP ou hostname do servidor local.
- Firewall liberado no servidor.
- Usuario cadastrado no ERP.

## Passos

1. Instalar o app desktop.
2. Abrir a tela de conexao/servicos.
3. Configurar URL da API:

```text
http://192.168.0.10:5000
```

4. Testar conexao.
5. Fazer login com usuario autorizado.
6. Conferir empresa/filial/caixa.
7. Executar busca de produto e health check.

## Regras

- O terminal nao deve ter banco oficial proprio.
- Dados oficiais nao devem ser exportados para arquivo local como fonte de verdade.
- Impressao deve usar PDF/servico compativel com navegador/app desktop, nao acesso direto acoplado a driver no dominio.
- Logs de terminal devem ajudar diagnostico, mas auditoria oficial fica no backend.

## Troubleshooting

- Se nao conectar, testar `http://<servidor>:5000/health/ready`.
- Conferir firewall do Windows no servidor.
- Conferir se servidor e terminal estao na mesma rede/VPN.
- Conferir URL e porta configuradas.
