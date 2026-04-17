param(
  [Parameter(Mandatory = $true)]
  [string]$Version,
  [string]$ArtifactsDir = "",
  [string]$OutputDir = ""
)

$ErrorActionPreference = 'Stop'

$ArtifactsDir = if ([string]::IsNullOrWhiteSpace($ArtifactsDir)) { Join-Path $PSScriptRoot "..\artifacts" } else { $ArtifactsDir }
$OutputDir = if ([string]::IsNullOrWhiteSpace($OutputDir)) { Join-Path $PSScriptRoot "..\artifacts\release" } else { $OutputDir }

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

$msix = Join-Path $ArtifactsDir "msix\VokunWL_$Version.msix"
$cer = Join-Path $ArtifactsDir "signing\VokunWL_TestCert.cer"
$installer = Join-Path $repoRoot "scripts\Install-VokunWLTester.ps1"
$installerExe = Join-Path $ArtifactsDir "installer\VokunWLInstaller_$Version.exe"
$packZip = Join-Path $ArtifactsDir "pack\VokunPack_$Version.zip"

if (-not (Test-Path $msix)) {
  & (Join-Path $repoRoot "scripts\Build-VokunWLMSIX.ps1") -Version $Version
}
if (-not (Test-Path $installerExe)) {
  & (Join-Path $repoRoot "scripts\Build-VokunWLInstaller.ps1") -Version $Version
}
if (-not (Test-Path $packZip)) {
  & (Join-Path $repoRoot "scripts\Build-VokunPack.ps1") -Version $Version
}
if (-not (Test-Path $msix)) { throw "MSIX not found: $msix" }
if (-not (Test-Path $cer)) { throw "CER not found: $cer" }
if (-not (Test-Path $installerExe)) { throw "Installer EXE not found: $installerExe" }
if (-not (Test-Path $packZip)) { throw "Pack zip not found: $packZip" }

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$dest = Join-Path $OutputDir ("VokunWL_{0}" -f $Version)
if (Test-Path $dest) { Remove-Item -Force -Recurse $dest }
New-Item -ItemType Directory -Force -Path $dest | Out-Null

$outMsixVer = Join-Path $dest ("VokunWL_{0}.msix" -f $Version)
$outMsixStable = Join-Path $dest "VokunWL.msix"
$outCer = Join-Path $dest "VokunWL_TestCert.cer"
$outPs1 = Join-Path $dest "Install-VokunWLTester.ps1"
$outExeVer = Join-Path $dest ("VokunWLInstaller_{0}.exe" -f $Version)
$outExeStable = Join-Path $dest "VokunWLInstaller.exe"
$outPackVer = Join-Path $dest ("VokunPack_{0}.zip" -f $Version)
$outPackStable = Join-Path $dest "VokunPack.zip"

Copy-Item -Force $msix $outMsixVer
Copy-Item -Force $msix $outMsixStable
Copy-Item -Force $cer $outCer
Copy-Item -Force $installer $outPs1
Copy-Item -Force $installerExe $outExeVer
Copy-Item -Force $installerExe $outExeStable
Copy-Item -Force $packZip $outPackVer
Copy-Item -Force $packZip $outPackStable

foreach ($p in @($outMsixVer,$outMsixStable,$outCer,$outPs1,$outExeVer,$outExeStable,$outPackVer,$outPackStable)) {
  if (-not (Test-Path $p)) { throw "Release file missing after copy: $p" }
  if ((Get-Item $p).Length -le 0) { throw "Release file is empty (0 bytes): $p" }
}

$hashMsix = Get-FileHash -Algorithm SHA256 (Join-Path $dest ("VokunWL_{0}.msix" -f $Version))
$hashExe = Get-FileHash -Algorithm SHA256 (Join-Path $dest ("VokunWLInstaller_{0}.exe" -f $Version))
$hashPack = Get-FileHash -Algorithm SHA256 (Join-Path $dest ("VokunPack_{0}.zip" -f $Version))
Set-Content -Encoding utf8 (Join-Path $dest "SHA256SUMS.txt") @(
  ("{0}  {1}" -f $hashMsix.Hash.ToLowerInvariant(), $hashMsix.Path.Split('\')[-1]),
  ("{0}  {1}" -f $hashExe.Hash.ToLowerInvariant(), $hashExe.Path.Split('\')[-1]),
  ("{0}  {1}" -f $hashPack.Hash.ToLowerInvariant(), $hashPack.Path.Split('\')[-1])
)

Write-Host "Release folder:"
Write-Host $dest
