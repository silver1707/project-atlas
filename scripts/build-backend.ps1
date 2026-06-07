$ErrorActionPreference = "Stop"
dotnet restore AutoPartsErp.slnx
dotnet build AutoPartsErp.slnx -c Release --no-restore
