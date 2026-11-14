[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$package = Get-AppxPackage -Name 'NeteaseGameBarWidget' -ErrorAction SilentlyContinue
if (-not $package) {
    Write-Host 'NeteaseGameBarWidget is not installed.'
    return
}

$package | Remove-AppxPackage
Write-Host 'NeteaseGameBarWidget has been uninstalled.'
