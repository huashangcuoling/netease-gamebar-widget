[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [ValidateSet('x64', 'ARM64')]
    [string]$Platform = 'x64'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'NeteaseGameBarWidget.csproj'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'

if (-not (Test-Path $vswhere)) {
    throw 'vswhere.exe not found. Install Visual Studio with the UWP workload first.'
}

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

Write-Host "MSBuild: $msbuild"
Write-Host "Building $Configuration|$Platform ..."

& $msbuild $project `
    /restore `
    /t:Build `
    /m `
    /verbosity:minimal `
    "/p:Configuration=$Configuration" `
    "/p:Platform=$Platform" `
    "/p:AppxPackageSigningEnabled=false" `
    "/p:AppxBundle=Never"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE. Make sure the Visual Studio UWP workload and Windows SDK are installed."
}

Write-Host ''
Write-Host "Build output: $root\bin\$Platform\$Configuration"
Write-Host 'Next: .\tools\Install-Dev.ps1'
