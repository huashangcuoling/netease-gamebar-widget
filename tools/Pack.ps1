[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [ValidateSet('x64', 'ARM64')]
    [string]$Platform = 'x64',

    [string]$PfxPath = '',
    [string]$PfxPassword = 'dsh-local'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PfxPath)) {
    $PfxPath = Join-Path $root 'build\NeteaseGameBarWidget.pfx'
}

if (-not (Test-Path $PfxPath)) {
    throw "PFX not found: $PfxPath. Run tools\New-DevCert.ps1 first."
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = $null
foreach ($workload in @(
        'Microsoft.VisualStudio.Workload.UniversalBuildTools',
        'Microsoft.VisualStudio.Workload.Universal')) {
    $msbuild = & $vswhere -latest -products * -requires $workload -find 'MSBuild\**\Bin\MSBuild.exe' |
        Select-Object -First 1
    if ($msbuild) {
        break
    }
}

if (-not $msbuild) {
    throw 'No Visual Studio installation with Windows/UWP build tools was found. Install "WinUI application development build tools" (Build Tools) or "WinUI application development" (full VS), then import .vsconfig.'
}

$project = Join-Path $root 'NeteaseGameBarWidget.csproj'

& $msbuild $project `
    /restore `
    /t:Build `
    /m `
    /verbosity:minimal `
    "/p:Configuration=$Configuration" `
    "/p:Platform=$Platform" `
    "/p:AppxBundle=Never" `
    "/p:AppxPackageSigningEnabled=true" `
    "/p:PackageCertificateKeyFile=$PfxPath" `
    "/p:PackageCertificatePassword=$PfxPassword"

if ($LASTEXITCODE -ne 0) {
    throw "Packaging failed with exit code $LASTEXITCODE."
}

Write-Host ''
Write-Host "Signed package output: $root\AppPackages"
