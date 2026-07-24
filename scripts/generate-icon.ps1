# generate-icon.ps1 — Generates AppIcon.ico from SVG source using GDI+
# All sizes: 16,20,24,32,40,48,64,128,256
# No external tools required — pure .NET System.Drawing

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$SvgPath = "$RepoRoot\assets\icon\app-icon.svg"
$IcoOutApp = "$RepoRoot\src\AjazzBattery.App\Resources\AppIcon.ico"
$IcoOutInstaller = "$RepoRoot\installer\assets\AppIcon.ico"

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)

# ── Draw icon programmatically (no SVG renderer needed) ──────────────────
function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    # ── Scale factor ──────────────────────────────────────────────────────
    $s = $size / 64.0

    # ── Background ────────────────────────────────────────────────────────
    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 30, 30, 46))
    $radius  = [int]([Math]::Round(14 * $s))
    $bgRect  = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    # Rounded rectangle via GraphicsPath
    $gp = New-Object System.Drawing.Drawing2D.GraphicsPath
    $gp.AddArc($bgRect.X, $bgRect.Y, $radius*2, $radius*2, 180, 90)
    $gp.AddArc($bgRect.Right - $radius*2, $bgRect.Y, $radius*2, $radius*2, 270, 90)
    $gp.AddArc($bgRect.Right - $radius*2, $bgRect.Bottom - $radius*2, $radius*2, $radius*2, 0, 90)
    $gp.AddArc($bgRect.X, $bgRect.Bottom - $radius*2, $radius*2, $radius*2, 90, 90)
    $gp.CloseFigure()
    $g.FillPath($bgBrush, $gp)
    $bgBrush.Dispose()
    $gp.Dispose()

    # ── Mouse body outline ────────────────────────────────────────────────
    $mouseColor = [System.Drawing.Color]::FromArgb(255, 208, 208, 232)
    $mousePen   = New-Object System.Drawing.Pen($mouseColor, [float]([Math]::Max(1.5, 2.5 * $s)))

    $mX = [int]([Math]::Round(21 * $s))
    $mY = [int]([Math]::Round(12 * $s))
    $mW = [int]([Math]::Round(22 * $s))
    $mH = [int]([Math]::Round(30 * $s))
    $mR = [int]([Math]::Round(10 * $s))

    $mp = New-Object System.Drawing.Drawing2D.GraphicsPath
    $mp.AddArc($mX, $mY, $mR*2, $mR*2, 180, 90)
    $mp.AddArc($mX + $mW - $mR*2, $mY, $mR*2, $mR*2, 270, 90)
    $mp.AddArc($mX + $mW - $mR*2, $mY + $mH - $mR*2, $mR*2, $mR*2, 0, 90)
    $mp.AddArc($mX, $mY + $mH - $mR*2, $mR*2, $mR*2, 90, 90)
    $mp.CloseFigure()
    $g.DrawPath($mousePen, $mp)
    $mp.Dispose()

    # ── Center divider line ────────────────────────────────────────────────
    $cx = [int]([Math]::Round(32 * $s))
    $divY1 = [int]([Math]::Round(12 * $s))
    $divY2 = [int]([Math]::Round(26 * $s))
    $g.DrawLine($mousePen, $cx, $divY1, $cx, $divY2)
    $mousePen.Dispose()

    # ── Scroll wheel ──────────────────────────────────────────────────────
    if ($size -ge 24) {
        $wheelColor = [System.Drawing.Color]::FromArgb(255, 160, 160, 192)
        $wheelBrush = New-Object System.Drawing.SolidBrush($wheelColor)
        $wX = [int]([Math]::Round(29.5 * $s))
        $wY = [int]([Math]::Round(15 * $s))
        $wW = [int]([Math]::Round(5 * $s))
        $wH = [int]([Math]::Round(9 * $s))
        $wR = [int]([Math]::Round(2.5 * $s))
        if ($wW -gt 0 -and $wH -gt 0) {
            $wp = New-Object System.Drawing.Drawing2D.GraphicsPath
            $wp.AddArc($wX, $wY, $wR*2, $wR*2, 180, 90)
            $wp.AddArc($wX + $wW - $wR*2, $wY, $wR*2, $wR*2, 270, 90)
            $wp.AddArc($wX + $wW - $wR*2, $wY + $wH - $wR*2, $wR*2, $wR*2, 0, 90)
            $wp.AddArc($wX, $wY + $wH - $wR*2, $wR*2, $wR*2, 90, 90)
            $wp.CloseFigure()
            $g.FillPath($wheelBrush, $wp)
            $wp.Dispose()
        }
        $wheelBrush.Dispose()
    }

    # ── Battery indicator ─────────────────────────────────────────────────
    $green = [System.Drawing.Color]::FromArgb(255, 76, 175, 80)
    $greenBrush = New-Object System.Drawing.SolidBrush($green)
    $greenPen   = New-Object System.Drawing.Pen($green, [float]([Math]::Max(1.2, 1.8 * $s)))

    $bX = [int]([Math]::Round(25 * $s))
    $bY = [int]([Math]::Round(33 * $s))
    $bW = [int]([Math]::Round(14 * $s))
    $bH = [int]([Math]::Round(7  * $s))

    if ($bW -gt 3 -and $bH -gt 2) {
        # Outer battery rect
        $g.DrawRectangle($greenPen, $bX, $bY, $bW, $bH)

        # Positive terminal
        $tW = [int]([Math]::Max(1, [Math]::Round(2 * $s)))
        $tH = [int]([Math]::Max(1, [Math]::Round(2 * $s)))
        $tY = $bY + ($bH - $tH) / 2
        $g.FillRectangle($greenBrush, $bX + $bW + 1, $tY, $tW, $tH)

        # Battery fill (75%)
        $fillW = [int]([Math]::Round($bW * 0.75 - 2))
        if ($fillW -gt 0) {
            $g.FillRectangle($greenBrush, $bX + 1, $bY + 1, $fillW, $bH - 2)
        }
    }
    $greenBrush.Dispose()
    $greenPen.Dispose()

    # ── Lightning bolt badge ──────────────────────────────────────────────
    if ($size -ge 20) {
        $yellow = [System.Drawing.Color]::FromArgb(230, 255, 224, 102)
        $yellowBrush = New-Object System.Drawing.SolidBrush($yellow)

        # Bolt polygon scaled from (translate 42,10): points "6,0 2,7 5,7 0,14 8,14 5,7 8,7"
        # World coords: add (42,10) to each point
        $boltPts = @(
            [System.Drawing.PointF]::new((42+6)*$s, (10+0)*$s),
            [System.Drawing.PointF]::new((42+2)*$s, (10+7)*$s),
            [System.Drawing.PointF]::new((42+5)*$s, (10+7)*$s),
            [System.Drawing.PointF]::new((42+0)*$s, (10+14)*$s),
            [System.Drawing.PointF]::new((42+8)*$s, (10+14)*$s),
            [System.Drawing.PointF]::new((42+5)*$s, (10+7)*$s),
            [System.Drawing.PointF]::new((42+8)*$s, (10+7)*$s)
        )
        $g.FillPolygon($yellowBrush, $boltPts)
        $yellowBrush.Dispose()
    }

    $g.Dispose()
    return $bmp
}

# ── Write ICO file ────────────────────────────────────────────────────────
function Write-IcoFile([string]$outPath, [int[]]$sizes) {
    $bitmaps = @{}
    foreach ($sz in $sizes) {
        $bitmaps[$sz] = New-IconBitmap $sz
    }

    $stream = [System.IO.File]::Open($outPath, [System.IO.FileMode]::Create)
    $writer = New-Object System.IO.BinaryWriter($stream)

    # ICO header: reserved=0, type=1 (icon), count=N
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)

    # Compute image data
    $imageStreams = @{}
    foreach ($sz in $sizes) {
        $ms = New-Object System.IO.MemoryStream
        $bitmaps[$sz].Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $imageStreams[$sz] = $ms
    }

    # Directory entries: 6 + 16*count bytes = header size
    $headerSize = 6 + 16 * $sizes.Count
    $offset = $headerSize

    foreach ($sz in $sizes) {
        $imgData = $imageStreams[$sz].ToArray()
        $w = if ($sz -ge 256) { 0 } else { $sz }
        $h = if ($sz -ge 256) { 0 } else { $sz }
        $writer.Write([byte]$w)          # width
        $writer.Write([byte]$h)          # height
        $writer.Write([byte]0)           # color count
        $writer.Write([byte]0)           # reserved
        $writer.Write([uint16]1)         # planes
        $writer.Write([uint16]32)        # bit count
        $writer.Write([uint32]$imgData.Length)
        $writer.Write([uint32]$offset)
        $offset += $imgData.Length
    }

    # Image data
    foreach ($sz in $sizes) {
        $writer.Write($imageStreams[$sz].ToArray())
    }

    $writer.Flush()
    $stream.Close()
    $writer.Dispose()

    foreach ($sz in $sizes) {
        $bitmaps[$sz].Dispose()
        $imageStreams[$sz].Dispose()
    }

    Write-Output "[+] ICO written: $outPath ($($sizes.Count) sizes: $($sizes -join ', '))"
}

Write-Output "[+] Generating AppIcon.ico from programmatic GDI+ renderer..."
Write-IcoFile $IcoOutApp $sizes
Copy-Item $IcoOutApp $IcoOutInstaller -Force
Write-Output "[+] Copied to installer assets: $IcoOutInstaller"
Write-Output "[+] Done. Icon generation complete."
