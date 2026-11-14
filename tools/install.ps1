[CmdletBinding()]
param(
    [string]$PackagePath = '',
    [string]$CertificatePath = ''
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) { Write-Host "  $Message" }
function Fail([string]$Message, [string]$Hint) {
    Write-Host ''
    Write-Host $Message -ForegroundColor Red
    if ($Hint) { Write-Host $Hint -ForegroundColor Yellow }
    exit 1
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $PackagePath) {
    $PackagePath = Get-ChildItem $root -Filter '*.msix' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

if (-not $PackagePath -or -not (Test-Path $PackagePath)) {
    Fail '找不到 .msix 安装包。' '请把 install.cmd、install.ps1 和 .msix 放在同一个文件夹里。'
}

if (-not $CertificatePath) {
    $CertificatePath = Get-ChildItem $root -Filter '*.cer' |
        Select-Object -First 1 -ExpandProperty FullName
}

Write-Host ''
Write-Host '网易云迷你播放器 · 安装程序'
Write-Host '----------------------------------------'

$build = [Environment]::OSVersion.Version.Build
if ($build -lt 19041) {
    Fail "Windows 版本过低（Build $build）。" '需要 Windows 10 2004（Build 19041）或更新版本。'
}

Write-Step "系统：Windows build $build / $env:PROCESSOR_ARCHITECTURE"
Write-Step "安装包：$(Split-Path -Leaf $PackagePath)"

if ($CertificatePath -and (Test-Path $CertificatePath)) {
    $certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $CertificatePath
    $thumbprint = $certificate.Thumbprint

    $store = if ($isAdmin) { 'Cert:\LocalMachine\TrustedPeople' } else { 'Cert:\CurrentUser\TrustedPeople' }

    $trusted = Get-ChildItem $store | Where-Object { $_.Thumbprint -eq $thumbprint }
    if ($trusted) {
        Write-Step '证书：已信任'
    }
    else {
        Write-Step "证书：导入到 $store"
        Import-Certificate -FilePath $CertificatePath -CertStoreLocation $store | Out-Null
    }
}

$dependencyFolder = Join-Path $root 'Dependencies'
if (Test-Path $dependencyFolder) {
    foreach ($appx in Get-ChildItem $dependencyFolder -Filter '*.appx') {
        try {
            Add-AppxPackage -Path $appx.FullName -ErrorAction Stop
            Write-Step ("框架：" + $appx.Name)
        }
        catch {
            Write-Step ("框架已存在：" + $appx.Name)
        }
    }
}

$existing = Get-AppxPackage -Name 'NeteaseGameBarWidget' -ErrorAction SilentlyContinue
if ($existing) {
    Write-Step '检测到已安装的旧版本，先卸载…'
    $existing | Remove-AppxPackage -ErrorAction SilentlyContinue
}

Write-Step '正在安装…'
try {
    Add-AppxPackage -Path $PackagePath -ForceApplicationShutdown
}
catch {
    $message = $_.Exception.Message
    if ($message -match '0x800B0109') {
        if (-not $isAdmin) {
            Write-Host ''
            Write-Host '需要一次管理员授权把签名证书装进本机受信任存储…' -ForegroundColor Yellow
            Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList @(
                '-NoProfile',
                '-ExecutionPolicy', 'Bypass',
                '-NoExit',
                '-File', $PSCommandPath,
                '-PackagePath', $PackagePath,
                '-CertificatePath', $CertificatePath
            )
            exit 0
        }

        Fail '证书已装进本机受信任存储，但系统仍拒绝。' '请确认安装包没有被修改，或重启后重试。'
    }

    Fail "安装失败：$message" '如果提示需要开发者模式，请在 设置 → 系统 → 开发者选项 中打开“开发人员模式”后重试。'
}

$installed = Get-AppxPackage -Name 'NeteaseGameBarWidget' -ErrorAction SilentlyContinue
if (-not $installed) {
    Fail '安装未完成。' '请重启后重试。'
}

Write-Host ''
Write-Host ("安装完成：版本 {0}" -f $installed.Version) -ForegroundColor Green
Write-Host ''
Write-Host '接下来：'
Write-Host '  1. 打开网易云音乐并播放一首歌'
Write-Host '  2. 按 Win+G 打开 Game Bar'
Write-Host '  3. 在小组件菜单里添加“网易云迷你播放器”'
Write-Host '  4. 点小组件标题栏的图钉，把它固定到屏幕'
Write-Host ''
Write-Host '如果小组件菜单里找不到，完全退出 GameBar.exe（任务管理器）后重新打开 Game Bar。'
