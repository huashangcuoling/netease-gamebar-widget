[CmdletBinding()]
param(
    [int]$Height = 0,
    [switch]$Reset
)

$ErrorActionPreference = 'Stop'

$package = Get-AppxPackage -Name 'NeteaseGameBarWidget' -ErrorAction SilentlyContinue
if (-not $package) {
    Write-Host '没有检测到已安装的网易云迷你播放器，请先运行 install.cmd。' -ForegroundColor Red
    exit 1
}

$folder = Join-Path $env:LOCALAPPDATA "Packages/$($package.PackageFamilyName)/LocalState"
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$file = Join-Path $folder 'settings.json'

if ($Reset) {
    if (Test-Path $file) {
        Remove-Item $file -Force
    }

    Write-Host '已恢复自动检测任务栏高度。重启小组件后生效。' -ForegroundColor Green
    exit 0
}

if ($Height -lt 24 -or $Height -gt 96) {
    Write-Host '用法：' -ForegroundColor Yellow
    Write-Host '  set-taskbar-height.cmd 44        指定高度（DIP，24-96）'
    Write-Host '  set-taskbar-height.cmd -Reset    恢复自动检测'
    Write-Host ''
    Write-Host '常见值：Windows 11 普通任务栏 48；Windows 10 普通任务栏 40；小任务栏按钮 32。'
    exit 1
}

$json = '{"taskbarHeight": ' + $Height + '}'
[System.IO.File]::WriteAllText($file, $json, (New-Object System.Text.UTF8Encoding($false)))

Write-Host ("已设置任务栏高度：{0} DIP。重启小组件后生效。" -f $Height) -ForegroundColor Green
