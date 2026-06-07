param(
  [int]$ApiPort = 5000,
  [int]$PostgresPort = 5432
)

$ErrorActionPreference = "Stop"
New-NetFirewallRule -DisplayName "AutoParts ERP API Local" -Direction Inbound -Protocol TCP -LocalPort $ApiPort -Action Allow -Profile Private -ErrorAction SilentlyContinue
New-NetFirewallRule -DisplayName "AutoParts ERP PostgreSQL LAN" -Direction Inbound -Protocol TCP -LocalPort $PostgresPort -Action Allow -Profile Private -ErrorAction SilentlyContinue
