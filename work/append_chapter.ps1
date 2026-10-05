param(
    [string]$SourceDocx,
    [string]$JsonPath,
    [string]$OutDocx
)

$ErrorActionPreference = "Stop"

$items = Get-Content -Raw -Path $JsonPath | ConvertFrom-Json

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
$doc = $word.Documents.Open($SourceDocx, [ref]$false, [ref]$false)

$wdCollapseEnd = 0
$wdStory = 6
$wdAlignParagraphLeft = 0
$wdAlignParagraphCenter = 1
$wdAlignParagraphJustify = 3
$wdLineStyleSingle = 1

$sel = $word.Selection
$sel.EndKey($wdStory)

foreach ($item in $items) {
    switch ($item.type) {
        "h1" {
            $sel.TypeParagraph()
            $sel.Style = $doc.Styles.Item("Heading 1")
            $sel.Font.Name = "Times New Roman"
            $sel.TypeText($item.text)
        }
        "h2" {
            $sel.TypeParagraph()
            $sel.Style = $doc.Styles.Item("Heading 2")
            $sel.Font.Name = "Times New Roman"
            $sel.TypeText($item.text)
        }
        "h3" {
            $sel.TypeParagraph()
            $sel.Style = $doc.Styles.Item("Heading 3")
            $sel.Font.Name = "Times New Roman"
            $sel.TypeText($item.text)
        }
        "p" {
            $sel.TypeParagraph()
            $sel.Style = $doc.Styles.Item("Normal")
            $sel.ParagraphFormat.Alignment = $wdAlignParagraphJustify
            $sel.Font.Name = "Times New Roman"
            $sel.Font.Size = 12
            $sel.Font.Bold = 0
            $sel.Font.Italic = 0
            $sel.TypeText($item.text)
        }
        "caption" {
            $sel.TypeParagraph()
            $sel.Style = $doc.Styles.Item("Normal")
            $sel.ParagraphFormat.Alignment = $wdAlignParagraphCenter
            $sel.Font.Name = "Times New Roman"
            $sel.Font.Size = 12
            $sel.Font.Bold = 0
            $sel.Font.Italic = -1
            $sel.TypeText($item.text)
            $sel.Font.Italic = 0
        }
        "bullet" {
            $sel.TypeParagraph()
            $sel.Style = $doc.Styles.Item("List Paragraph")
            $sel.ParagraphFormat.Alignment = $wdAlignParagraphJustify
            $sel.Font.Name = "Times New Roman"
            $sel.Font.Size = 12
            $sel.Font.Bold = 0
            $sel.Font.Italic = 0
            $sel.Range.ListFormat.ApplyBulletDefault()
            $sel.TypeText($item.text)
        }
        "table" {
            $sel.TypeParagraph()
            $headers = $item.headers
            $rows = $item.rows
            $nCols = $headers.Count
            $nRows = $rows.Count + 1
            $insertRange = $sel.Range
            $insertRange.Collapse($wdCollapseEnd)
            $tbl = $doc.Tables.Add($insertRange, $nRows, $nCols)
            $tbl.Borders.InsideLineStyle = $wdLineStyleSingle
            $tbl.Borders.OutsideLineStyle = $wdLineStyleSingle
            $tbl.PreferredWidthType = 2   # wdPreferredWidthPercent
            $tbl.PreferredWidth = 100
            $tbl.AutoFitBehavior(1)       # wdAutoFitWindow

            for ($c = 0; $c -lt $nCols; $c++) {
                $cell = $tbl.Cell(1, $c + 1)
                $cr = $cell.Range
                $cr.Text = [string]$headers[$c]
                $cr.Font.Name = "Times New Roman"
                $cr.Font.Size = 12
                $cr.Font.Bold = -1
                $cell.Shading.BackgroundPatternColor = 15983321
            }
            for ($r = 0; $r -lt $rows.Count; $r++) {
                $rowData = $rows[$r]
                for ($c = 0; $c -lt $nCols; $c++) {
                    $cell = $tbl.Cell($r + 2, $c + 1)
                    $cr = $cell.Range
                    $cr.Text = [string]$rowData[$c]
                    $cr.Font.Name = "Times New Roman"
                    $cr.Font.Size = 12
                    $cr.Font.Bold = 0
                }
            }
            $sel.EndKey($wdStory)
        }
    }
}

$sel.EndKey($wdStory)

$doc.SaveAs([ref]$OutDocx, [ref]16)   # 16 = wdFormatXMLDocument (.docx)
$doc.Close([ref]$false)
$word.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
Write-Output "DONE -> $OutDocx"
