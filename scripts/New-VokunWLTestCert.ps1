param(
  [string]$OutputDir = "$(Join-Path $PSScriptRoot '..\artifacts\signing')",
  [string]$Subject = 'CN=VokunWL Test',
  [string]$PfxPassword = 'vokunwl-test'
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$cert = New-SelfSignedCertificate `
  -Type Custom `
  -Subject $Subject `
  -KeyUsage DigitalSignature `
  -KeyAlgorithm RSA `
  -KeyLength 2048 `
  -HashAlgorithm SHA256 `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")

$pfxPath = Join-Path $OutputDir "VokunWL_TestCert.pfx"
$cerPath = Join-Path $OutputDir "VokunWL_TestCert.cer"

$secure = ConvertTo-SecureString -String $PfxPassword -Force -AsPlainText

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $secure | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

Write-Host "Created:"
Write-Host $pfxPath
Write-Host $cerPath
