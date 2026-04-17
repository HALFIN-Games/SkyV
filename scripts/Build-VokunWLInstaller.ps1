param(
  [string]$Configuration = "Release",
  [string]$Version = "0.0.0.0",
  [string]$OutputDir = "$(Join-Path $PSScriptRoot '..\artifacts\installer')"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$proj = Join-Path $repoRoot "src\SkyV.Installer\SkyV.Installer.csproj"

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$publishDir = Join-Path $OutputDir "publish"
if (Test-Path $publishDir) { Remove-Item -Force -Recurse $publishDir }

dotnet publish $proj -c $Configuration -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publishDir

$srcExe = Join-Path $publishDir "SkyV.Installer.exe"
if (-not (Test-Path $srcExe)) { throw "Expected output missing: $srcExe" }

$outExe = Join-Path $OutputDir ("VokunWLInstaller_{0}.exe" -f $Version)
Copy-Item -Force $srcExe $outExe

Write-Host "Done:"
Write-Host $outExe

