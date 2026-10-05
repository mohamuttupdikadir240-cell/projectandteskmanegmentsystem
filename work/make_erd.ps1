Add-Type -AssemblyName System.Drawing

$W = 1400
$H = 460
$bmp = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
$g.Clear([System.Drawing.Color]::White)

$fontName = "Calibri"
$boxFont   = New-Object System.Drawing.Font($fontName, 12, [System.Drawing.FontStyle]::Bold)
$labelFont = New-Object System.Drawing.Font($fontName, 10, [System.Drawing.FontStyle]::Regular)

$borderPen   = New-Object System.Drawing.Pen([System.Drawing.ColorTranslator]::FromHtml("#2E4A66"), 2)
$centralFill = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml("#FCE8B2"))
$normalFill  = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml("#DCE6F5"))
$textBrush   = [System.Drawing.Brushes]::Black
$labelBrush  = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml("#B03A2E"))
$lineColor   = [System.Drawing.ColorTranslator]::FromHtml("#3B5C7A")
$linePen     = New-Object System.Drawing.Pen($lineColor, 1.8)
$linePenArrow = New-Object System.Drawing.Pen($lineColor, 1.8)
$linePenArrow.CustomEndCap = New-Object System.Drawing.Drawing2D.AdjustableArrowCap(5, 7)

$sf = New-Object System.Drawing.StringFormat
$sf.Alignment = [System.Drawing.StringAlignment]::Center
$sf.LineAlignment = [System.Drawing.StringAlignment]::Center

function New-Box($x, $y, $w, $h) {
    [PSCustomObject]@{ X = $x; Y = $y; W = $w; H = $h }
}

$boxes = [ordered]@{
    "ApplicationUser" = New-Box 520 30 280 60
    "Project"         = New-Box 20  190 190 55
    "ProjectMember"   = New-Box 290 190 190 55
    "ProjectTask"     = New-Box 610 190 190 55
    "Notification"    = New-Box 980 190 190 55
    "AuditLog"        = New-Box 1190 190 190 55
    "TaskComment"     = New-Box 430 350 190 55
    "TaskAttachment"  = New-Box 640 350 190 55
    "TaskHistory"     = New-Box 850 350 190 55
}

function Draw-Box($name, $box, $isCentral) {
    $rect = New-Object System.Drawing.Rectangle([int]$box.X, [int]$box.Y, [int]$box.W, [int]$box.H)
    $fill = if ($isCentral) { $centralFill } else { $normalFill }
    $g.FillRectangle($fill, $rect)
    $g.DrawRectangle($borderPen, $rect)
    $g.DrawString($name, $boxFont, $textBrush, [System.Drawing.RectangleF]$rect, $sf)
}

function Get-EdgePoint($box, $tx, $ty) {
    $cx = $box.X + $box.W / 2.0
    $cy = $box.Y + $box.H / 2.0
    $dx = $tx - $cx
    $dy = $ty - $cy
    if ($dx -eq 0 -and $dy -eq 0) { return New-Object System.Drawing.PointF($cx, $cy) }
    $halfW = $box.W / 2.0
    $halfH = $box.H / 2.0
    $scaleX = if ($dx -ne 0) { $halfW / [Math]::Abs($dx) } else { [double]::PositiveInfinity }
    $scaleY = if ($dy -ne 0) { $halfH / [Math]::Abs($dy) } else { [double]::PositiveInfinity }
    $scale = [Math]::Min($scaleX, $scaleY)
    return New-Object System.Drawing.PointF(($cx + $dx * $scale), ($cy + $dy * $scale))
}

function Draw-Label($mx, $my, $label) {
    $size = $g.MeasureString($label, $labelFont)
    $bgRect = New-Object System.Drawing.RectangleF(($mx - $size.Width/2 - 3), ($my - $size.Height/2 - 1), ($size.Width + 6), ($size.Height + 2))
    $g.FillRectangle([System.Drawing.Brushes]::White, $bgRect)
    $lsf = New-Object System.Drawing.StringFormat
    $lsf.Alignment = [System.Drawing.StringAlignment]::Center
    $lsf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString($label, $labelFont, $labelBrush, [System.Drawing.RectangleF]$bgRect, $lsf)
}

function Connect-Under($fromName, $toName, $label, $dropY, $labelFrac = 0.5) {
    $a = $boxes[$fromName]
    $b = $boxes[$toName]
    $fx = $a.X + $a.W / 2.0
    $fy = $a.Y + $a.H
    $tx = $b.X + $b.W / 2.0
    $ty = $b.Y + $b.H
    $p0 = New-Object System.Drawing.PointF($fx, $fy)
    $p1 = New-Object System.Drawing.PointF($fx, $dropY)
    $p2 = New-Object System.Drawing.PointF($tx, $dropY)
    $p3 = New-Object System.Drawing.PointF($tx, $ty)
    $g.DrawLine($linePen, $p0, $p1)
    $g.DrawLine($linePen, $p1, $p2)
    $g.DrawLine($linePenArrow, $p2, $p3)
    $lx = $p1.X + ($p2.X - $p1.X) * $labelFrac
    Draw-Label $lx $dropY $label
}

function Connect($fromName, $toName, $label) {
    $a = $boxes[$fromName]
    $b = $boxes[$toName]
    $acx = $a.X + $a.W / 2.0
    $acy = $a.Y + $a.H / 2.0
    $bcx = $b.X + $b.W / 2.0
    $bcy = $b.Y + $b.H / 2.0
    $p1 = Get-EdgePoint $a $bcx $bcy
    $p2 = Get-EdgePoint $b $acx $acy
    $g.DrawLine($linePenArrow, $p1, $p2)
    Draw-Label (($p1.X + $p2.X) / 2.0) (($p1.Y + $p2.Y) / 2.0) $label
}

# Draw connections first (so boxes sit on top of line ends)
Connect "ApplicationUser" "Project"        "1-M"
Connect "ApplicationUser" "ProjectMember"  "1-M"
Connect "ApplicationUser" "ProjectTask"    "1-M"
Connect "ApplicationUser" "Notification"   "1-M"
Connect "ApplicationUser" "AuditLog"       "1-M"
Connect "Project"         "ProjectMember"  "1-M"
Connect-Under "Project"   "ProjectTask"    "1-M" 280 0.2
Connect "ProjectTask"     "TaskComment"    "1-M"
Connect "ProjectTask"     "TaskAttachment" "1-M"
Connect "ProjectTask"     "TaskHistory"    "1-M"

foreach ($key in $boxes.Keys) {
    Draw-Box $key $boxes[$key] ($key -eq "ApplicationUser")
}

# Legend
$legendFont = New-Object System.Drawing.Font($fontName, 10, [System.Drawing.FontStyle]::Italic)
$g.DrawString("Legend: boxes = entities (tables); lines = 1-to-Many (1-M) relationships; shaded box = central entity", $legendFont, [System.Drawing.Brushes]::DimGray, 20, 420)

$g.Save()

$outPath = "work\unpacked\word\media\erd_diagram.png"
$bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Save("work\erd_diagram_preview.png", [System.Drawing.Imaging.ImageFormat]::Png)

$g.Dispose()
$bmp.Dispose()
Write-Output "Saved: $outPath"
