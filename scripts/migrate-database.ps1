param(
  [string]$HostName = "localhost",
  [int]$Port = 5432,
  [string]$Database = "autoparts_erp",
  [string]$User = "autoparts",
  [string]$Password = "autoparts_dev_password",
  [string]$MigrationDir = "database/migrations"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
  throw "psql not found. Install PostgreSQL client tools and add them to PATH."
}

$resolvedMigrationDir = Resolve-Path -LiteralPath $MigrationDir
$files = Get-ChildItem -LiteralPath $resolvedMigrationDir -Filter "*.sql" | Sort-Object Name
if ($files.Count -eq 0) {
  throw "No SQL migrations found in $resolvedMigrationDir"
}

$previousPassword = $env:PGPASSWORD
$env:PGPASSWORD = $Password
try {
  foreach ($file in $files) {
    Write-Host "Applying migration $($file.Name)"
    psql `
      --host=$HostName `
      --port=$Port `
      --username=$User `
      --dbname=$Database `
      --set=ON_ERROR_STOP=1 `
      --file=$file.FullName
  }
}
finally {
  $env:PGPASSWORD = $previousPassword
}
