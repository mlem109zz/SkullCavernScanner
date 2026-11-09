$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root 'SkullCavernScanner/SkullCavernScanner.csproj'
$Build = Join-Path $Root 'SkullCavernScanner/bin/Release/net6.0'
$Dist = Join-Path $Root 'dist/SkullCavernScanner'

dotnet build $Project -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build thất bại. Hãy kiểm tra .NET 6 SDK và đường dẫn cài Stardew Valley.' }

New-Item -ItemType Directory -Force -Path $Dist | Out-Null
Copy-Item (Join-Path $Build 'SkullCavernScanner.dll') $Dist -Force
Copy-Item (Join-Path $Root 'SkullCavernScanner/manifest.json') $Dist -Force
Copy-Item (Join-Path $Root 'SkullCavernScanner/config.json') $Dist -Force
Write-Host "Build thành công. Thư mục mod: $Dist" -ForegroundColor Green
