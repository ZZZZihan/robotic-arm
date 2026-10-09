[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [string]$MSBuildPath
)

# Offline only: build a fresh source snapshot and run fake-adapter/UI checks.
# Never start the operator app, open a controller, or replace a running release.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$maintainedSource = Join-Path $repo 'thinkbook-deploy\payload\source\EtherCATDemo'
if (!$OutputDirectory) {
    $OutputDirectory = Join-Path $repo ('.artifacts\dashboard-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
}
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Output directory already exists; choose a fresh directory.' }
if (!$MSBuildPath) {
    $MSBuildPath = 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
    if (!(Test-Path -LiteralPath $MSBuildPath)) {
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path -LiteralPath $vswhere) {
            $MSBuildPath = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        }
    }
}
if (!$MSBuildPath -or !(Test-Path -LiteralPath $MSBuildPath)) { throw 'MSBuild not found. Supply -MSBuildPath or install the Windows .NET desktop build tools.' }
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $csc)) { throw '.NET Framework x64 C# compiler not found.' }

$manifest = Get-Content -LiteralPath (Join-Path $repo 'materials\manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$sdkEntries = @($manifest.artifacts | Where-Object { $_.kind -eq 'runtime-library' })
if ($sdkEntries.Count -ne 3) { throw 'Expected exactly three pinned SDK libraries.' }
foreach ($entry in $sdkEntries) {
    $file = Join-Path $repo $entry.path
    if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $entry.sha256) { throw "SDK checksum mismatch: $($entry.path)" }
}

$source = Join-Path $output 'source\EtherCATDemo'
$app = Join-Path $output 'app'
$tests = Join-Path $output 'verification'
New-Item -ItemType Directory -Path $source,$app,$tests | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $maintainedSource -Recurse -File) {
    $relative = $file.FullName.Substring($maintainedSource.Length + 1)
    if ($relative -match '(^|[\\/])(bin|obj)([\\/]|$)') { continue }
    $destination = Join-Path $source $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $destination) | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination
}
New-Item -ItemType Directory -Path (Join-Path $source 'bin\Debug') | Out-Null
foreach ($entry in $sdkEntries) {
    foreach ($directory in @($app, $tests, (Join-Path $source 'bin\Debug'))) {
        Copy-Item -LiteralPath (Join-Path $repo $entry.path) -Destination $directory
    }
}

& $MSBuildPath (Join-Path $source 'CSharpDemo.csproj') /t:Rebuild /p:Configuration=Release /p:Platform=x64 "/p:OutputPath=$app\" /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Dashboard build failed. This project targets .NET Framework 4.0 Client Profile and x64.' }
& $csc /nologo /platform:x64 /target:exe "/out:$tests\Axis2MotionTests.exe" (Join-Path $source 'Axis2Motion.cs') (Join-Path $source 'tests\Axis2MotionTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Behavior test compilation failed.' }
$behaviorOutput = & (Join-Path $tests 'Axis2MotionTests.exe')
if ($LASTEXITCODE -ne 0) { $behaviorOutput | Write-Output; throw 'Behavior tests failed.' }
$behaviorOutput | Write-Output
$behaviorOutput | Set-Content -LiteralPath (Join-Path $tests 'behavior-results.txt') -Encoding UTF8
& $csc /nologo /platform:x64 /target:exe "/out:$tests\UiSmoke.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Data.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "/reference:$app\MultiCardCS.dll" "/reference:$app\MultiCardCLR.dll" (Join-Path $source 'Form1.cs') (Join-Path $source 'Form1.Designer.cs') (Join-Path $source 'Form1.Axis2.cs') (Join-Path $source 'Form1.Dashboard.cs') (Join-Path $source 'DashboardWidgets.cs') (Join-Path $source 'Axis2Motion.cs') (Join-Path $source 'tests\UiSmoke.cs')
if ($LASTEXITCODE -ne 0) { throw 'UI test compilation failed.' }
$uiOutput = & (Join-Path $tests 'UiSmoke.exe') (Join-Path $tests 'ui-smoke.png')
if ($LASTEXITCODE -ne 0) { $uiOutput | Write-Output; throw 'Offline UI checks failed.' }
$uiOutput | Write-Output
$uiOutput | Set-Content -LiteralPath (Join-Path $tests 'ui-results.txt') -Encoding UTF8
$result = [ordered]@{
    createdAtUtc = [DateTime]::UtcNow.ToString('o')
    output = $output
    msbuild = $MSBuildPath
    exeSha256 = (Get-FileHash -LiteralPath (Join-Path $app 'CSharpDemo.exe')).Hash
    controllerConnected = $false
    operatorAppStarted = $false
    behaviorChecks = @($behaviorOutput | Where-Object { $_ -match '^PASS ' -and $_ -notmatch '^PASS [0-9]+ behavior tests' }).Count
    uiChecks = @($uiOutput | Where-Object { $_ -match '^PASS UI ' -and $_ -notmatch '^PASS UI verification;' }).Count
    sdk = $sdkEntries | Select-Object path,sha256
}
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'verification-result.json') -Encoding UTF8
$result | ConvertTo-Json -Depth 5
