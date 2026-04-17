param(
  [Parameter(Mandatory = $true)]
  [string]$MsixPath,

  [Parameter(Mandatory = $true)]
  [string]$CerPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $MsixPath)) { throw "MSIX not found: $MsixPath" }
if (-not (Test-Path $CerPath)) { throw "CER not found: $CerPath" }

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
  throw "Run this script from an elevated PowerShell (Run as Administrator)."
}

Write-Host "Removing old dev protocol registration (current user)..."
cmd /c 'reg delete "HKCU\Software\Classes\skyv" /f >nul 2>&1' | Out-Null

Write-Host "Installing certificate into LocalMachine\\TrustedPeople..."
Import-Certificate -FilePath $CerPath -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null

Write-Host "Removing previous installs (if any)..."
$existingUser = Get-AppxPackage -Name "HALFIN.VokunWL" -ErrorAction SilentlyContinue
if ($existingUser) {
  Remove-AppxPackage -Package $existingUser.PackageFullName -ErrorAction SilentlyContinue | Out-Null
}

$existingProv = Get-AppxProvisionedPackage -Online | Where-Object { $_.PackageName -like "HALFIN.VokunWL_*" }
foreach ($p in $existingProv) {
  Remove-AppxProvisionedPackage -Online -PackageName $p.PackageName | Out-Null
}

Write-Host "Provisioning package for all users (admin required)..."
Add-AppxProvisionedPackage -Online -PackagePath $MsixPath -SkipLicense | Out-Null

Write-Host "Installing package for current user..."
Add-AppxPackage -Path $MsixPath -ForceApplicationShutdown | Out-Null

Write-Host "Installed."
