$base = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)

# Встроенный каталог - снимок живого на момент сборки, третья ступень фолбэка в утилите.
Copy-Item (Join-Path $base "..\..\catalog.json") (Join-Path $base "catalog.json") -Force

dotnet build (Join-Path $base "AbrCivilSetup.csproj") -c Release
if ($LASTEXITCODE -ne 0) { throw "Сборка AbrCivilSetup провалилась" }

$exe = Join-Path $base "bin\Release\net48\AbrCivilSetup.exe"
Copy-Item $exe (Join-Path $base "AbrCivilSetup.exe") -Force

Write-Host "Готово: $(Join-Path $base 'AbrCivilSetup.exe')"
