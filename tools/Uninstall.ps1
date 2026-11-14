[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

Write-Host ''
Write-Host '网易云迷你播放器 · 卸载'
Write-Host '----------------------------------------'

$package = Get-AppxPackage -Name 'NeteaseGameBarWidget' -ErrorAction SilentlyContinue
if (-not $package) {
    Write-Host '没有检测到已安装的版本，无需卸载。'
    exit 0
}

$package | Remove-AppxPackage
Write-Host '已卸载。' -ForegroundColor Green
