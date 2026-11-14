[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [ValidateSet('x64', 'ARM64')]
    [string]$Platform = 'x64'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$pfx = Join-Path $root 'build\NeteaseGameBarWidget.pfx'
$cer = Join-Path $root 'build\NeteaseGameBarWidget.cer'

if (-not (Test-Path $pfx)) {
    Write-Host '未找到签名证书，生成一张开发用证书…'
    $windowsPowerShell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    & $windowsPowerShell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'New-DevCert.ps1')
    if (-not (Test-Path $pfx)) {
        throw '证书生成失败，无法签名打包。'
    }
}

& (Join-Path $PSScriptRoot 'Pack.ps1') -Configuration $Configuration -Platform $Platform

$msix = Get-ChildItem (Join-Path $root 'AppPackages') -Recurse -Filter '*.msix' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $msix) {
    throw '没有找到打包产物 .msix。'
}

$manifestText = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Package.appxmanifest')
if ($manifestText -notmatch 'Version="([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)"') {
    throw '无法从 Package.appxmanifest 读取版本号。'
}
$version = $Matches[1]

$distRoot = Join-Path $root 'dist'
$name = "NeteaseGameBarWidget-$version-$Platform"
$dist = Join-Path $distRoot $name

if (Test-Path $dist) {
    Remove-Item $dist -Recurse -Force
}

New-Item -ItemType Directory -Path $dist -Force | Out-Null

Copy-Item $msix.FullName (Join-Path $dist $msix.Name)
Copy-Item $cer $dist
foreach ($script in @('install.ps1', 'install.cmd', 'uninstall.ps1', 'uninstall.cmd', 'set-taskbar-height.ps1', 'set-taskbar-height.cmd')) {
    Copy-Item (Join-Path $PSScriptRoot $script) $dist
}

$dependencies = Join-Path $dist 'Dependencies'
New-Item -ItemType Directory -Path $dependencies -Force | Out-Null

foreach ($framework in @(
        'Microsoft.NET.CoreRuntime.2.2.appx',
        'Microsoft.NET.CoreFramework.Debug.2.2.appx',
        'Microsoft.VCLibs.140.00.appx')) {
    $source = Get-ChildItem (Join-Path $root 'AppPackages') -Recurse -Filter $framework -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($source) {
        Copy-Item $source.FullName $dependencies
    }
}

$instructions = @'
网易云迷你播放器 · 安装说明
================================

版本：__VERSION__

一、安装
  1. 解压整个文件夹（不要只把 msix 拖出来）。
  2. 双击 install.cmd，等它输出“安装完成”。
  3. 安装时会弹出一次管理员授权（UAC），点“是”。证书需要装进本机受信任存储，
     这是旁加载应用的固定要求。
  4. 如果 UAC 被拒绝，安装会停在“证书不受信任”，重新双击 install.cmd 再点“是”即可。

二、使用
  1. 打开网易云音乐并播放歌曲。
  2. 按 Win+G 打开 Game Bar。
  3. 在小组件菜单里添加“网易云迷你播放器”。
  4. 点小组件标题栏的图钉，把它固定到屏幕。
  如果菜单里找不到：任务管理器结束 GameBar.exe，再重新打开 Game Bar。

三、任务栏高度不对时
  默认按系统版本估算：Windows 11 = 48，Windows 10 = 40（单位是 DIP）。
  如果开了“小任务栏按钮”或换了外壳，小组件会偏高/偏矮，运行：

    set-taskbar-height.cmd 32      ← 换成你实际的任务栏高度
    set-taskbar-height.cmd -Reset  ← 恢复自动检测

  改完重启小组件（关掉再重新添加，或结束 GameBar.exe）后生效。

四、卸载
  双击 uninstall.cmd；或
  设置 → 应用 → 已安装的应用 → 网易云迷你播放器 → 卸载。

五、说明
  - 需要 Windows 10 2004（Build 19041）或更高版本。
  - 安装包使用自签名证书签名，安装脚本会把证书导入当前用户的“受信任人”存储。
    这张证书只用于验证本应用，不能用它给其它软件签名，可以随时删除。
  - 源码和构建脚本见仓库。
'@

$instructions = $instructions.Replace('__VERSION__', $version)
$utf8Bom = New-Object System.Text.UTF8Encoding($true)
[System.IO.File]::WriteAllText((Join-Path $dist '安装说明.txt'), $instructions, $utf8Bom)

$zip = Join-Path $distRoot "$name.zip"
if (Test-Path $zip) {
    Remove-Item $zip -Force
}

Compress-Archive -Path $dist -DestinationPath $zip

Write-Host ''
Write-Host '发布包已生成：' -ForegroundColor Green
Write-Host "  文件夹：$dist"
Write-Host "  压缩包：$zip"
Write-Host ''
Write-Host '发给别人时只发压缩包；对方解压后双击 install.cmd 即可。'
