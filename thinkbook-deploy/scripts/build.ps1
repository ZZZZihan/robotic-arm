$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$msbuild = 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
if (!(Test-Path $msbuild)) { throw 'Visual Studio 2019 MSBuild is missing.' }
& $msbuild "$root\source\EtherCATDemo\CSharpDemo.csproj" /t:Rebuild /p:Configuration=Release /p:Platform=x64 "/p:OutputPath=$root\app\" /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'The x64 demo build failed.' }
Get-FileHash "$root\app\CSharpDemo.exe" -Algorithm SHA256 | ConvertTo-Json
