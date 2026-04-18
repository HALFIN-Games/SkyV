param(
  [string]$Version = "0.0.0.0",
  [string]$SourceDataDir = "",
  [string]$OutputDir = "",
  [string]$SkympRepoDir = "",
  [string]$ServerIp = "16.171.229.12",
  [int]$ServerPort = 7777
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$OutputDir = if ([string]::IsNullOrWhiteSpace($OutputDir)) { Join-Path $repoRoot "artifacts\pack" } else { $OutputDir }
$SkympRepoDir = if ([string]::IsNullOrWhiteSpace($SkympRepoDir)) { Join-Path $repoRoot "..\skymp-SkyV" } else { $SkympRepoDir }

if ([string]::IsNullOrWhiteSpace($SourceDataDir)) {
  $candidate = Join-Path $repoRoot "..\FiveE\staging\skymp-client\Data"
  if (Test-Path $candidate) { $SourceDataDir = $candidate }
}

if ([string]::IsNullOrWhiteSpace($SourceDataDir) -or -not (Test-Path $SourceDataDir)) {
  throw "Pack source Data folder not found. Pass -SourceDataDir pointing at a folder containing 'Platform' and 'SKSE'."
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$temp = Join-Path $OutputDir "tmp"
if (Test-Path $temp) { Remove-Item -Force -Recurse $temp }
New-Item -ItemType Directory -Force -Path $temp | Out-Null

$dataOut = Join-Path $temp "Data"
Copy-Item -Force -Recurse $SourceDataDir $dataOut

$clientPlugin = Join-Path $SkympRepoDir "build\dist\client\Data\Platform\Plugins\skymp5-client.js"
if (-not (Test-Path $clientPlugin)) {
  throw "skymp5-client.js not found at: $clientPlugin. Build skymp-SkyV\\skymp5-client first (yarn build)."
}
$dst = Join-Path $dataOut "Platform\Plugins\skymp5-client.js"
Copy-Item -Force $clientPlugin $dst

$settingsPath = Join-Path $dataOut "Platform\Plugins\skymp5-client-settings.txt"
$settingsJson = @"
{
  "server-ip": "$ServerIp",
  "server-port": $ServerPort,
  "server-info-ignore": true,
  "skyv-join-ui": true
}
"@
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($settingsPath, $settingsJson, $utf8NoBom)

New-Item -ItemType Directory -Force -Path (Join-Path $dataOut "Platform\PluginsDev") | Out-Null

$zipVersioned = Join-Path $OutputDir ("VokunPack_{0}.zip" -f $Version)
if (Test-Path $zipVersioned) { Remove-Item -Force $zipVersioned }

Compress-Archive -Path (Join-Path $temp "*") -DestinationPath $zipVersioned

Write-Host "Done:"
Write-Host $zipVersioned
