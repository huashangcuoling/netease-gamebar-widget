[CmdletBinding()]
param(
    [string]$Subject = 'CN=NeteaseGameBarWidget',
    [string]$Password = 'dsh-local'
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command New-SelfSignedCertificate -ErrorAction SilentlyContinue)) {
    throw 'New-SelfSignedCertificate is unavailable. Run this script with Windows PowerShell 5.1 (powershell.exe).'
}

$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root 'build'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Subject `
    -FriendlyName 'NeteaseGameBarWidget dev signing' `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -HashAlgorithm SHA256 `
    -KeyLength 2048

$pfxPath = Join-Path $outDir 'NeteaseGameBarWidget.pfx'
$cerPath = Join-Path $outDir 'NeteaseGameBarWidget.cer'
$securePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null
Import-Certificate -FilePath $cerPath -CertStoreLocation 'Cert:\CurrentUser\TrustedPeople' | Out-Null

Write-Host "Certificate: $($cert.Thumbprint)"
Write-Host "PFX:        $pfxPath"
Write-Host "CER:        $cerPath"
Write-Host "Thumbprint: $($cert.Thumbprint)"
Write-Host ''
Write-Host 'The CER is trusted for the current user. If Add-AppxPackage still reports an untrusted publisher,'
Write-Host 'install the CER into LocalMachine\TrustedPeople from an elevated PowerShell.'
