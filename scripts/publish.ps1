param([ValidateSet('win-x64', 'win-arm64')][string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
& dotnet run --project "$root/tests/BlackScreen.Core.Tests" -c Release
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
& dotnet publish "$root/src/BlackScreen/BlackScreen.csproj" -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o "$root/artifacts/$Runtime"
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
Copy-Item "$root/README.md" "$root/artifacts/$Runtime/使用说明.md"
Set-Content -Path "$root/artifacts/$Runtime/立即黑屏.cmd" -Encoding ascii -Value '@start "" "%~dp0BlackScreen.exe" --black'
Compress-Archive -Path "$root/artifacts/$Runtime/*" -DestinationPath "$root/artifacts/MuHei-$Runtime.zip" -Force
