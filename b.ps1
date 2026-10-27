$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Get-Process -Name "Scroll-V", "Scroll-V-Setup" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 250

$pubDir = Join-Path $PSScriptRoot "bin\publish"
if (Test-Path $pubDir) { Remove-Item $pubDir -Recurse -Force }

Write-Host "Building Release..." -ForegroundColor Cyan
dotnet publish .\ScrollV.csproj -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $pubDir -nologo -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exePath = Join-Path $PSScriptRoot "Scroll-V.exe"
$setupPath = Join-Path $PSScriptRoot "Scroll-V-Setup.exe"

Copy-Item -Path (Join-Path $pubDir "Scroll-V.exe") -Destination $exePath -Force
Copy-Item -Path (Join-Path $pubDir "Scroll-V.exe") -Destination $setupPath -Force

$installedExe = Join-Path $env:LOCALAPPDATA "Programs\Scroll-V\Scroll-V.exe"
if (Test-Path $installedExe) {
    Copy-Item -Path $exePath -Destination $installedExe -Force
    Write-Host "Updated installed binary: $installedExe" -ForegroundColor Yellow
}

# Re-restore default project.assets.json so ./r.ps1 works immediately after ./b.ps1
dotnet restore .\ScrollV.csproj -nologo -v q

Write-Host "Portable : $exePath" -ForegroundColor Green
Write-Host "Installer: $setupPath" -ForegroundColor Green
