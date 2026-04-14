param(
  [Parameter(Mandatory = $true)]
  [string]$KitFolder,

  [string]$Tag,

  [string]$Title,

  [switch]$Clobber
)

$ErrorActionPreference = 'Stop'

function Require-Command([string]$name) {
  $cmd = Get-Command $name -ErrorAction SilentlyContinue
  if (-not $cmd) {
    throw "Required command not found in PATH: $name"
  }
}

function Resolve-GhExe() {
  $cmd = Get-Command gh -ErrorAction SilentlyContinue
  if ($cmd) {
    return $cmd.Source
  }

  $candidates = @(
    'C:\\Program Files\\GitHub CLI\\gh.exe',
    'C:\\Program Files (x86)\\GitHub CLI\\gh.exe'
  )

  foreach ($p in $candidates) {
    if (Test-Path $p) {
      return $p
    }
  }

  throw "Required command not found: gh (install GitHub CLI)"
}

Require-Command git

$ghExe = Resolve-GhExe

$repoRoot = (git rev-parse --show-toplevel).Trim()
$kitPath = $KitFolder
if (-not [System.IO.Path]::IsPathRooted($kitPath)) {
  $kitPath = Join-Path $repoRoot $kitPath
}

if (-not (Test-Path $kitPath)) {
  throw "Kit folder not found: $kitPath"
}

$kitFolderName = Split-Path -Leaf $kitPath
if (-not $Tag) {
  $Tag = "kit-$kitFolderName"
}
if (-not $Title) {
  $Title = "Tester kit: $kitFolderName"
}

$zipPath = Join-Path (Join-Path $repoRoot 'testing-kits') "$kitFolderName.zip"
$shaPath = "$zipPath.sha256"

if ((Test-Path $zipPath) -and (-not $Clobber)) {
  throw "Zip already exists: $zipPath (use -Clobber to overwrite)"
}

if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
if (Test-Path $shaPath) { Remove-Item -Force $shaPath }

Compress-Archive -Path (Join-Path $kitPath '*') -DestinationPath $zipPath -Force

$hash = (Get-FileHash -Algorithm SHA256 -Path $zipPath).Hash.ToLowerInvariant()
"$hash  $(Split-Path -Leaf $zipPath)" | Out-File -Encoding ascii -NoNewline -FilePath $shaPath

$notes = @(
  "Kit folder: $kitFolderName",
  "SHA256: $hash"
) -join "`n"

$releaseExists = $true
try {
  & $ghExe release view $Tag --repo . | Out-Null
} catch {
  $releaseExists = $false
}

if (-not $releaseExists) {
  & $ghExe release create $Tag $zipPath $shaPath --repo . --title $Title --notes $notes
} else {
  & $ghExe release edit $Tag --repo . --title $Title --notes $notes | Out-Null
  & $ghExe release upload $Tag $zipPath $shaPath --repo . --clobber | Out-Null
}

Write-Host "Published release $Tag with assets:" 
Write-Host "- $zipPath"
Write-Host "- $shaPath"
