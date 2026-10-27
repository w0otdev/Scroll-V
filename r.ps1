$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Get-Process -Name "Scroll-V", "Scroll-V-Setup" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

dotnet build .\ScrollV.csproj -c Debug --no-incremental -nologo -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exe = Join-Path $PSScriptRoot "bin\Debug\net8.0-windows\Scroll-V.exe"
Start-Process -FilePath $exe -WorkingDirectory $PSScriptRoot -ArgumentList "--show"
Write-Host "Running: $exe --show" -ForegroundColor Cyan
