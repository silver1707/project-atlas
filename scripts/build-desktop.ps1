$ErrorActionPreference = "Stop"
Push-Location "src/desktop/AutoPartsErp.Desktop"
try {
  npm ci
  npm run lint
  npm run build
}
finally {
  Pop-Location
}
