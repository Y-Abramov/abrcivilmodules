# Генератор иконок ленты для AbrCivilModules («Библиотека модулей»).
# По образцу civil3d/lisp/icons/make-icons.ps1.
#
# Конвенция AutoCAD: PNG с альфой, 16x16 и 32x32, отдельные наборы под тёмную
# и светлую тему. Поля вокруг глифа обязательны - без них рисунок упирается
# в край кнопки и выглядит обрезанным.
#
# Рисуем с восьмикратным запасом и уменьшаем бикубикой: сглаживание получается
# ровным на обоих размерах без ручной подгонки по пикселям.

Add-Type -AssemblyName System.Drawing

$OutDir = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)
$Scale = 8

$Themes = @{
    light = @{ Neutral = '#FF414141'; Accent = '#FF0E7C93' }
    dark  = @{ Neutral = '#FFD4D9DC'; Accent = '#FF3FC7E3' }
}

function Get-Color([string]$argb) {
    return [System.Drawing.ColorTranslator]::FromHtml('#' + $argb.Substring(3))
}

function New-Pen($color, [double]$widthPx) {
    $pen = New-Object System.Drawing.Pen($color, [float]($widthPx * $Scale))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    return $pen
}

function Draw-AbrAbout($g, [int]$S, $neutral, $accent, [double]$stroke) {
    # "i" в кружке - стандартный глиф "о модуле" по всей линейке.
    $penNeutral = New-Pen $neutral $stroke
    $rect = New-Object System.Drawing.RectangleF(
        [float]($S * 0.14), [float]($S * 0.14), [float]($S * 0.72), [float]($S * 0.72))
    $g.DrawEllipse($penNeutral, $rect)

    $penAccent = New-Pen $accent $stroke
    $x = [float]($S * 0.5)
    $g.DrawLine($penAccent, $x, [float]($S * 0.46), $x, [float]($S * 0.70))

    $dot = [float]($stroke * $Scale * 0.62)
    $brush = New-Object System.Drawing.SolidBrush($accent)
    $g.FillEllipse($brush, $x - $dot / 2, [float]($S * 0.30) - $dot / 2, $dot, $dot)

    $brush.Dispose(); $penAccent.Dispose(); $penNeutral.Dispose()
}

function Draw-AbrStore($g, [int]$S, $neutral, $accent, [double]$stroke) {
    # Коробка со стрелкой внутрь - библиотека модулей, откуда их устанавливают.
    $penNeutral = New-Pen $neutral $stroke
    $penAccent = New-Pen $accent $stroke

    # Корпус коробки.
    $g.DrawLine($penNeutral, [float]($S * 0.16), [float]($S * 0.34), [float]($S * 0.50), [float]($S * 0.18))
    $g.DrawLine($penNeutral, [float]($S * 0.50), [float]($S * 0.18), [float]($S * 0.84), [float]($S * 0.34))
    $g.DrawLine($penNeutral, [float]($S * 0.16), [float]($S * 0.34), [float]($S * 0.16), [float]($S * 0.70))
    $g.DrawLine($penNeutral, [float]($S * 0.84), [float]($S * 0.34), [float]($S * 0.84), [float]($S * 0.70))
    $g.DrawLine($penNeutral, [float]($S * 0.16), [float]($S * 0.70), [float]($S * 0.50), [float]($S * 0.86))
    $g.DrawLine($penNeutral, [float]($S * 0.84), [float]($S * 0.70), [float]($S * 0.50), [float]($S * 0.86))
    $g.DrawLine($penNeutral, [float]($S * 0.50), [float]($S * 0.86), [float]($S * 0.50), [float]($S * 0.50))
    $g.DrawLine($penNeutral, [float]($S * 0.16), [float]($S * 0.34), [float]($S * 0.50), [float]($S * 0.50))
    $g.DrawLine($penNeutral, [float]($S * 0.84), [float]($S * 0.34), [float]($S * 0.50), [float]($S * 0.50))

    # Стрелка вниз внутрь коробки - установка.
    $cap = New-Object System.Drawing.Drawing2D.AdjustableArrowCap([float]2.0, [float]2.0, $true)
    $penAccent.CustomEndCap = $cap
    $x = [float]($S * 0.5)
    $g.DrawLine($penAccent, $x, [float]($S * 0.02), $x, [float]($S * 0.30))
    $cap.Dispose()

    $penAccent.Dispose(); $penNeutral.Dispose()
}

function Draw-AbrWebsite($g, [int]$S, $neutral, $accent, [double]$stroke) {
    # Глобус - переход на сайт разработчика.
    $penNeutral = New-Pen $neutral $stroke
    $rect = New-Object System.Drawing.RectangleF(
        [float]($S * 0.14), [float]($S * 0.14), [float]($S * 0.72), [float]($S * 0.72))
    $g.DrawEllipse($penNeutral, $rect)

    $penAccent = New-Pen $accent ($stroke * 0.85)
    $cx = [float]($S * 0.5)
    $cy = [float]($S * 0.5)
    $rx = [float]($S * 0.20)
    $ry = [float]($S * 0.36)
    $g.DrawEllipse($penAccent, $cx - $rx, $cy - $ry, $rx * 2, $ry * 2)
    $g.DrawLine($penAccent, [float]($S * 0.16), $cy, [float]($S * 0.84), $cy)

    $penAccent.Dispose(); $penNeutral.Dispose()
}

function Write-Icon([string]$name, [int]$size, [string]$theme) {
    $S = $size * $Scale
    $big = New-Object System.Drawing.Bitmap($S, $S,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    $g = [System.Drawing.Graphics]::FromImage($big)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $neutral = Get-Color $Themes[$theme].Neutral
    $accent = Get-Color $Themes[$theme].Accent
    $stroke = if ($size -le 16) { 1.7 } else { 2.3 }

    switch ($name) {
        'abr_about'   { Draw-AbrAbout   $g $S $neutral $accent $stroke }
        'abr_store'   { Draw-AbrStore   $g $S $neutral $accent $stroke }
        'abr_website' { Draw-AbrWebsite $g $S $neutral $accent $stroke }
        default        { throw "Неизвестная иконка: $name" }
    }
    $g.Dispose()

    $small = New-Object System.Drawing.Bitmap($size, $size,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $gs = [System.Drawing.Graphics]::FromImage($small)
    $gs.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $gs.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $gs.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $gs.Clear([System.Drawing.Color]::Transparent)
    $gs.DrawImage($big, 0, 0, $size, $size)
    $gs.Dispose()

    $path = Join-Path $OutDir ("{0}_{1}_{2}.png" -f $name, $size, $theme)
    $small.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)

    $small.Dispose(); $big.Dispose()
    Write-Host ("  {0}" -f [System.IO.Path]::GetFileName($path))
}

Write-Host "Генерация иконок в $OutDir"
foreach ($name in @('abr_about', 'abr_store', 'abr_website')) {
    foreach ($size in @(16, 32)) {
        foreach ($theme in @('light', 'dark')) {
            Write-Icon $name $size $theme
        }
    }
}
Write-Host "Готово."
