# Generates assets\app.ico : an amber isometric "Horadric cube" icon.
# Each size is rendered natively (no downscaling) so the 16px tray icon stays crisp.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root "assets"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$outIco = Join-Path $outDir "app.ico"

function New-P([double]$x, [double]$y, [int]$s, [double]$pad) {
    $span = $s - 2 * $pad
    return New-Object System.Drawing.PointF ([float]($pad + $x * $span)), ([float]($pad + $y * $span))
}

function Render-Cube([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $pad = [Math]::Max(0.5, $s * 0.03)

    # Isometric hexagon vertices (unit space)
    $T  = New-P 0.50 0.02 $s $pad
    $UR = New-P 0.95 0.26 $s $pad
    $LR = New-P 0.95 0.74 $s $pad
    $B  = New-P 0.50 0.98 $s $pad
    $LL = New-P 0.05 0.74 $s $pad
    $UL = New-P 0.05 0.26 $s $pad
    $C  = New-P 0.50 0.50 $s $pad

    $topFace   = [System.Drawing.PointF[]]@($T, $UR, $C, $UL)
    $leftFace  = [System.Drawing.PointF[]]@($UL, $C, $B, $LL)
    $rightFace = [System.Drawing.PointF[]]@($C, $UR, $LR, $B)
    $hex       = [System.Drawing.PointF[]]@($T, $UR, $LR, $B, $LL, $UL)

    $cTopA   = [System.Drawing.Color]::FromArgb(255, 255, 224, 130)
    $cTopB   = [System.Drawing.Color]::FromArgb(255, 255, 190, 60)
    $cLeftA  = [System.Drawing.Color]::FromArgb(255, 248, 165, 30)
    $cLeftB  = [System.Drawing.Color]::FromArgb(255, 222, 128, 10)
    $cRightA = [System.Drawing.Color]::FromArgb(255, 196, 98, 8)
    $cRightB = [System.Drawing.Color]::FromArgb(255, 140, 62, 4)

    if ($s -ge 32) {
        $bTop   = New-Object System.Drawing.Drawing2D.LinearGradientBrush $T, $C, $cTopA, $cTopB
        $bLeft  = New-Object System.Drawing.Drawing2D.LinearGradientBrush $UL, $B, $cLeftA, $cLeftB
        $bRight = New-Object System.Drawing.Drawing2D.LinearGradientBrush $UR, $B, $cRightA, $cRightB
    } else {
        $bTop   = New-Object System.Drawing.SolidBrush $cTopB
        $bLeft  = New-Object System.Drawing.SolidBrush $cLeftA
        $bRight = New-Object System.Drawing.SolidBrush $cRightA
    }

    $g.FillPolygon($bTop, $topFace)
    $g.FillPolygon($bLeft, $leftFace)
    $g.FillPolygon($bRight, $rightFace)

    # Inner edges (light highlight lines between faces)
    $edgeW = [Math]::Max(1.0, $s / 28.0)
    $edgePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(230, 255, 245, 210)), ([float]$edgeW)
    $g.DrawLine($edgePen, $C, $UL)
    $g.DrawLine($edgePen, $C, $UR)
    $g.DrawLine($edgePen, $C, $B)

    # Rune details on larger sizes: glowing grid lines on side faces + sparkle on top
    if ($s -ge 48) {
        $runePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(110, 255, 240, 200)), ([float][Math]::Max(1.0, $s / 64.0))
        foreach ($t in @(0.333, 0.667)) {
            # left face: lines parallel to UL->C, offset towards LL->B
            $a = New-Object System.Drawing.PointF ($UL.X + ($LL.X - $UL.X) * $t), ($UL.Y + ($LL.Y - $UL.Y) * $t)
            $b2 = New-Object System.Drawing.PointF ($C.X + ($B.X - $C.X) * $t), ($C.Y + ($B.Y - $C.Y) * $t)
            $g.DrawLine($runePen, $a, $b2)
            # right face
            $a = New-Object System.Drawing.PointF ($UR.X + ($LR.X - $UR.X) * $t), ($UR.Y + ($LR.Y - $UR.Y) * $t)
            $g.DrawLine($runePen, $a, $b2)
            # top face
            $a = New-Object System.Drawing.PointF ($UL.X + ($T.X - $UL.X) * $t), ($UL.Y + ($T.Y - $UL.Y) * $t)
            $b2 = New-Object System.Drawing.PointF ($C.X + ($UR.X - $C.X) * $t), ($C.Y + ($UR.Y - $C.Y) * $t)
            $g.DrawLine($runePen, $a, $b2)
            $a = New-Object System.Drawing.PointF ($UR.X + ($T.X - $UR.X) * $t), ($UR.Y + ($T.Y - $UR.Y) * $t)
            $b2 = New-Object System.Drawing.PointF ($C.X + ($UL.X - $C.X) * $t), ($C.Y + ($UL.Y - $C.Y) * $t)
            $g.DrawLine($runePen, $a, $b2)
        }
        $runePen.Dispose()
    }

    # Dark outline so it reads on both light and dark taskbars
    $outW = [Math]::Max(1.0, $s / 16.0)
    $outPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 48, 24, 2)), ([float]$outW)
    $outPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPolygon($outPen, $hex)

    if ($s -ge 32) {
        # Sparkle on the top face
        $cx = ($T.X + $C.X) / 2.0; $cy = ($T.Y + $C.Y) / 2.0
        $r = $s * 0.11; $w = $s * 0.025
        $star = [System.Drawing.PointF[]]@(
            (New-Object System.Drawing.PointF ([float]$cx), ([float]($cy - $r))),
            (New-Object System.Drawing.PointF ([float]($cx + $w)), ([float]($cy - $w))),
            (New-Object System.Drawing.PointF ([float]($cx + $r)), ([float]$cy)),
            (New-Object System.Drawing.PointF ([float]($cx + $w)), ([float]($cy + $w))),
            (New-Object System.Drawing.PointF ([float]$cx), ([float]($cy + $r))),
            (New-Object System.Drawing.PointF ([float]($cx - $w)), ([float]($cy + $w))),
            (New-Object System.Drawing.PointF ([float]($cx - $r)), ([float]$cy)),
            (New-Object System.Drawing.PointF ([float]($cx - $w)), ([float]($cy - $w)))
        )
        $sb = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(245, 255, 255, 240))
        $g.FillPolygon($sb, $star)
        $sb.Dispose()
    }

    $edgePen.Dispose(); $outPen.Dispose()
    $bTop.Dispose(); $bLeft.Dispose(); $bRight.Dispose()
    $g.Dispose()
    return $bmp
}

function Get-DibBytes([System.Drawing.Bitmap]$bmp) {
    $s = $bmp.Width
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter $ms
    $maskRow = [int]([Math]::Floor(($s + 31) / 32) * 4)
    # BITMAPINFOHEADER
    $bw.Write([int]40); $bw.Write([int]$s); $bw.Write([int]($s * 2))
    $bw.Write([int16]1); $bw.Write([int16]32); $bw.Write([int]0)
    $bw.Write([int]($s * $s * 4 + $maskRow * $s))
    $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0)
    # XOR (BGRA, bottom-up)
    for ($y = $s - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $s; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $bw.Write([byte]$c.B); $bw.Write([byte]$c.G); $bw.Write([byte]$c.R); $bw.Write([byte]$c.A)
        }
    }
    # AND mask (1 = transparent), bottom-up
    for ($y = $s - 1; $y -ge 0; $y--) {
        $row = New-Object byte[] $maskRow
        for ($x = 0; $x -lt $s; $x++) {
            if ($bmp.GetPixel($x, $y).A -lt 128) { $row[[int][Math]::Floor($x / 8)] = $row[[int][Math]::Floor($x / 8)] -bor (0x80 -shr ($x % 8)) }
        }
        $bw.Write($row)
    }
    $bw.Flush()
    return ,$ms.ToArray()
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = @()
foreach ($s in $sizes) {
    $bmp = Render-Cube $s
    if ($s -ge 256) {
        $pms = New-Object System.IO.MemoryStream
        $bmp.Save($pms, [System.Drawing.Imaging.ImageFormat]::Png)
        $images += ,@($s, $pms.ToArray())
    } else {
        $images += ,@($s, (Get-DibBytes $bmp))
    }
    if ($s -eq 256) { $bmp.Save((Join-Path $outDir "app_preview.png"), [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose()
}

$fs = [System.IO.File]::Create($outIco)
$w = New-Object System.IO.BinaryWriter $fs
$w.Write([int16]0); $w.Write([int16]1); $w.Write([int16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($img in $images) {
    $s = $img[0]; $data = $img[1]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([int16]1); $w.Write([int16]32)
    $w.Write([int]$data.Length); $w.Write([int]$offset)
    $offset += $data.Length
}
foreach ($img in $images) { $w.Write([byte[]]$img[1]) }
$w.Flush(); $fs.Close()

Write-Host "Icon written: $outIco" -ForegroundColor Green
