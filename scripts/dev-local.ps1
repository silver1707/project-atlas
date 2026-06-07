param(
  [switch]$Advanced
)

$ErrorActionPreference = "Stop"
$composeArgs = @("compose", "up", "-d", "postgres", "api")
if ($Advanced) {
  $composeArgs = @("compose", "--profile", "advanced", "up", "-d")
}

docker @composeArgs
Push-Location "src/desktop/AutoPartsErp.Desktop"
try {
  npm run desktop:dev
}
finally {
  Pop-Location
}
