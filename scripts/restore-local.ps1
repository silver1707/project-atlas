param(
  [Parameter(Mandatory = $true)][string]$DumpFile,
  [string]$HostName = "localhost",
  [int]$Port = 5432,
  [string]$Database = "autoparts_erp",
  [string]$User = "autoparts",
  [string]$Password = "",
  [string]$FiscalXmlZip,
  [string]$FiscalXmlPath = "C:\AutoPartsErp\FiscalXml"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $DumpFile)) { throw "Dump file not found: $DumpFile" }
if (-not (Get-Command pg_restore -ErrorAction SilentlyContinue)) {
  throw "pg_restore not found. Install PostgreSQL client tools and add them to PATH."
}

$previousPassword = $env:PGPASSWORD
if ($Password) { $env:PGPASSWORD = $Password }
try {
  pg_restore `
    --host=$HostName `
    --port=$Port `
    --username=$User `
    --dbname=$Database `
    --clean `
    --if-exists `
    --no-owner `
    --no-privileges `
    $DumpFile
}
finally {
  $env:PGPASSWORD = $previousPassword
}

if ($FiscalXmlZip) {
  if (-not (Test-Path -LiteralPath $FiscalXmlZip)) { throw "Fiscal XML zip not found: $FiscalXmlZip" }
  New-Item -ItemType Directory -Force -Path $FiscalXmlPath | Out-Null
  Expand-Archive -LiteralPath $FiscalXmlZip -DestinationPath $FiscalXmlPath -Force
}

Write-Host "Restore completed for database $Database"
