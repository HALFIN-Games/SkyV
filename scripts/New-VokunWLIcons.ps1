param(
  [string]$OutputDir = "$(Join-Path $PSScriptRoot '..\src\SkyV.Launcher\Packaging\Assets')",
  [string]$SvgPath = "$(Join-Path $PSScriptRoot '..\assets\Vokun_logo.svg')"
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$bg = [System.Drawing.ColorTranslator]::FromHtml("#1A1D1C")
$accent = [System.Drawing.ColorTranslator]::FromHtml("#0C2D24")
$fg = [System.Drawing.ColorTranslator]::FromHtml("#F4F1EA")

$sizes = @(16,20,24,30,32,36,40,44,48,60,64,72,96,128,150,256)

function Generate-WithMagick([string]$magickExe) {
  foreach ($s in $sizes) {
    $pad = [int]([Math]::Max(1.0, [Math]::Floor([double]$s * 0.08)))
    $inner = [int]($s - (2 * $pad))
    $radius = [int]([Math]::Max(1.0, [Math]::Floor([double]$s * 0.10)))

    $x0 = $pad
    $y0 = $pad
    $x1 = $s - $pad - 1
    $y1 = $s - $pad - 1

    $path = Join-Path $OutputDir ("Square{0}x{0}Logo.png" -f $s)
    & $magickExe `
      "-size" "${s}x${s}" "xc:#1A1D1C" `
      "-fill" "#0C2D24" "-draw" "roundrectangle $x0,$y0 $x1,$y1 $radius,$radius" `
      "(" $SvgPath "-resize" "${inner}x${inner}" ")" `
      "-gravity" "center" "-composite" `
      $path | Out-Null
  }
}

function Generate-WithSystemDrawing {
  Add-Type -AssemblyName System.Drawing

  foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear($bg)

    $pad = [int]([Math]::Max(1.0, [Math]::Floor([double]$s * 0.08)))
    $inner = [int]($s - (2 * $pad))
    $rect = New-Object System.Drawing.Rectangle($pad, $pad, $inner, $inner)
    $brush = New-Object System.Drawing.SolidBrush($accent)
    $g.FillRectangle($brush, $rect)

    $fontSize = [Math]::Max(6, [Math]::Floor($s * 0.55))
    $font = New-Object System.Drawing.Font("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $textBrush = New-Object System.Drawing.SolidBrush($fg)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString("V", $font, $textBrush, (New-Object System.Drawing.RectangleF(0,0,$s,$s)), $sf)

    $path = Join-Path $OutputDir ("Square{0}x{0}Logo.png" -f $s)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)

    $g.Dispose()
    $bmp.Dispose()
  }
}

$magick = Get-Command magick -ErrorAction SilentlyContinue
if ($magick -and (Test-Path $SvgPath)) {
  Generate-WithMagick $magick.Path
} else {
  Generate-WithSystemDrawing
}

Write-Host "Generated icons in:"
Write-Host $OutputDir
