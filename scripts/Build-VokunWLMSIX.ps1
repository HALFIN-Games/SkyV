param(
  [string]$Configuration = "Release",
  [string]$Version = "0.0.0.0",
  [string]$Publisher = "CN=VokunWL Test",
  [string]$PfxPath = "$(Join-Path $PSScriptRoot '..\artifacts\signing\VokunWL_TestCert.pfx')",
  [string]$PfxPassword = "vokunwl-test",
  [string]$OutputDir = "$(Join-Path $PSScriptRoot '..\artifacts\msix')"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$proj = Join-Path $repoRoot "src\SkyV.Launcher\SkyV.Launcher.csproj"
$packagingDir = Join-Path $repoRoot "src\SkyV.Launcher\Packaging"
$assetsDir = Join-Path $packagingDir "Assets"
$manifestPath = Join-Path $packagingDir "AppxManifest.xml"
$packZip = Join-Path $repoRoot ("artifacts\pack\VokunPack_{0}.zip" -f $Version)


New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null

Write-Host "Generating icons..."
& (Join-Path $repoRoot "scripts\New-VokunWLIcons.ps1") -OutputDir $assetsDir

Write-Host "Publishing app..."
$publishDir = Join-Path $OutputDir "app"
if (Test-Path $publishDir) { Remove-Item -Force -Recurse $publishDir }
dotnet publish $proj -c $Configuration -r win-x64 -p:PublishSingleFile=false -p:SelfContained=true -o $publishDir

Write-Host "Preparing manifest..."
$manifestOut = Join-Path $publishDir "AppxManifest.xml"
$manifest = Get-Content -Raw $manifestPath
$manifest = $manifest -replace 'Version="0\.0\.0\.0"', ('Version="{0}"' -f $Version)
$manifest = $manifest -replace 'Publisher="CN=VokunWL Test"', ('Publisher="{0}"' -f $Publisher)
Set-Content -NoNewline -Encoding utf8 $manifestOut $manifest

Write-Host "Copying assets..."
Copy-Item -Force (Join-Path $assetsDir "*.png") $publishDir
New-Item -ItemType Directory -Force -Path (Join-Path $publishDir "Assets") | Out-Null
Copy-Item -Force (Join-Path $assetsDir "*.png") (Join-Path $publishDir "Assets")

if (-not (Test-Path $packZip)) { throw "Pack zip not found: $packZip. Build pack first (Build-VokunPack.ps1)." }
Copy-Item -Force $packZip (Join-Path $publishDir "VokunPack.zip")

$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter makeappx.exe -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -match '\\x64\\makeappx\.exe$' } |
  Sort-Object FullName -Descending |
  Select-Object -First 1 -ExpandProperty FullName
if (-not $makeappx) { throw "makeappx.exe not found. Install Windows 10/11 SDK." }

$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
  Sort-Object FullName -Descending |
  Select-Object -First 1 -ExpandProperty FullName
if (-not $signtool) { throw "signtool.exe not found. Install Windows 10/11 SDK." }

$msixPath = Join-Path $OutputDir ("VokunWL_{0}.msix" -f $Version)
if (Test-Path $msixPath) { Remove-Item -Force $msixPath }

Write-Host "Packing MSIX..."
& $makeappx pack /d $publishDir /p $msixPath /o | Out-Host

Write-Host "Signing MSIX..."
& $signtool sign /fd SHA256 /f $PfxPath /p $PfxPassword $msixPath | Out-Host

Write-Host "Done:"
Write-Host $msixPath
