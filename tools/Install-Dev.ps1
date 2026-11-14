[CmdletBinding()]
param(
    [switch]$Reinstall
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$bin = Join-Path $root 'bin'

if (-not (Test-Path $bin)) {
    throw 'No bin directory. Run tools\Build.ps1 or build the solution in Visual Studio first.'
}

$manifest = Get-ChildItem $bin -Recurse -Filter 'AppxManifest.xml' |
    Where-Object { $_.FullName -match '\\AppX\\AppxManifest.xml$' } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $manifest) {
    throw @'
AppX\AppxManifest.xml not found.

Use one of these paths:
  1. Open NeteaseGameBarWidget.sln in Visual Studio, choose Debug | x64, press F5.
  2. Build and sign a package with tools\New-DevCert.ps1 + tools\Pack.ps1, then install the MSIX.
'@
}

$devKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock'
$devMode = (Get-ItemProperty $devKey -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense
if ($devMode -ne 1) {
    Write-Warning 'Windows Developer Mode is off. Add-AppxPackage -Register will usually fail.'
    Write-Warning 'Enable Settings > System > For developers > Developer Mode, or use a signed MSIX package.'
}

# The VS deployment output may carry the UWP framework dependencies separately.
$dependencies = Join-Path $root 'AppPackages'
if (Test-Path $dependencies) {
    $frameworkPackages = Get-ChildItem $dependencies -Recurse -Filter '*.appx' |
        Where-Object { $_.FullName -match '\\Dependencies\\x64\\' -and $_.Name -match 'Microsoft\.(NET\.Core|VCLibs)' }
    foreach ($framework in $frameworkPackages) {
        Write-Host "Installing framework: $($framework.Name)"
        try {
            Add-AppxPackage -Path $framework.FullName
        }
        catch {
            Write-Warning "Framework may already be installed: $($framework.Name)"
        }
    }
}

if ($Reinstall) {
    Get-AppxPackage -Name 'NeteaseGameBarWidget' -ErrorAction SilentlyContinue | Remove-AppxPackage
}

Write-Host "Registering: $($manifest.FullName)"
Add-AppxPackage -Register $manifest.FullName

Write-Host ''
Write-Host 'Installed. Open Game Bar with Win+G, then add "网易云迷你播放器" from the widget menu.'
Write-Host 'If it does not appear, fully close Game Bar (GameBar.exe) and open it again.'
