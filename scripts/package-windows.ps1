$ErrorActionPreference = "Stop"
Push-Location "src/desktop/AutoPartsErp.Desktop"
try {
  npm ci
  npm run package:windows
}
finally {
  Pop-Location
}
