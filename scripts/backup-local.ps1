param(
  [string]$BackupRoot = "C:\AutoPartsErp\Backups",
  [string]$HostName = "localhost",
  [int]$Port = 5432,
  [string]$Database = "autoparts_erp",
  [string]$User = "autoparts",
  [string]$Password = "",
  [string]$FiscalXmlPath = "C:\AutoPartsErp\FiscalXml"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command pg_dump -ErrorAction SilentlyContinue)) {
  throw "pg_dump not found. Install PostgreSQL client tools and add them to PATH."
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$target = Join-Path $BackupRoot $timestamp
New-Item -ItemType Directory -Force -Path $target | Out-Null

$previousPassword = $env:PGPASSWORD
if ($Password) { $env:PGPASSWORD = $Password }
try {
  $dumpFile = Join-Path $target "$Database.dump"
  pg_dump `
    --host=$HostName `
    --port=$Port `
    --username=$User `
    --dbname=$Database `
    --format=custom `
    --file=$dumpFile

  if (Test-Path -LiteralPath $FiscalXmlPath) {
    Compress-Archive -LiteralPath $FiscalXmlPath -DestinationPath (Join-Path $target "FiscalXml.zip") -Force
  }
}
finally {
  $env:PGPASSWORD = $previousPassword
}

Write-Host "Backup created in $target"
