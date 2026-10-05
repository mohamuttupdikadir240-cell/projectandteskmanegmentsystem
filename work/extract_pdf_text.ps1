param(
    [string]$PdfPath,
    [string]$OutPath
)

$bytes = [System.IO.File]::ReadAllBytes($PdfPath)
# Work on a Latin1 string so byte offsets == char offsets (safe for binary PDF bytes)
$enc = [System.Text.Encoding]::GetEncoding("ISO-8859-1")
$raw = $enc.GetString($bytes)

function Decompress-Flate([byte[]]$data) {
    try {
        # zlib stream = 2-byte header + deflate data + 4-byte adler32; skip header for raw DeflateStream
        $ms = New-Object System.IO.MemoryStream(,$data[2..($data.Length-1)])
        $ds = New-Object System.IO.Compression.DeflateStream($ms, [System.IO.Compression.CompressionMode]::Decompress)
        $out = New-Object System.IO.MemoryStream
        $ds.CopyTo($out)
        return $enc.GetString($out.ToArray())
    } catch {
        return ""
    }
}

# Find every "stream ... endstream" block
$streamPattern = [regex]'stream\r?\n'
$allText = New-Object System.Text.StringBuilder
$pos = 0
$objPattern = [regex]::new('(\d+)\s+0\s+obj(.*?)endobj', [System.Text.RegularExpressions.RegexOptions]::Singleline)

$count = 0
foreach ($m in $objPattern.Matches($raw)) {
    $objBody = $m.Groups[2].Value
    $sIdx = $objBody.IndexOf("stream")
    if ($sIdx -lt 0) { continue }
    if ($objBody -notmatch '/FlateDecode') { continue }
    # locate actual stream data start (after 'stream' + EOL)
    $afterStream = $objBody.Substring($sIdx + 6)
    $afterStream = $afterStream -replace '^\r?\n', ''
    $eIdx = $afterStream.IndexOf("endstream")
    if ($eIdx -lt 0) { continue }
    $streamDataStr = $afterStream.Substring(0, $eIdx)
    # convert back to bytes using Latin1 (1:1 mapping)
    $sbytes = $enc.GetBytes($streamDataStr)
    if ($sbytes.Length -lt 3) { continue }
    $decoded = Decompress-Flate $sbytes
    if ($decoded.Length -eq 0) { continue }
    $count++

    # Extract text shown via Tj and TJ operators, preserving order
    $tokenPattern = [regex]::new('\((?:\\.|[^\\()])*\)\s*Tj|\[(?:[^\[\]]*)\]\s*TJ', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    foreach ($tm in $tokenPattern.Matches($decoded)) {
        $tok = $tm.Value
        if ($tok.EndsWith("Tj")) {
            $strs = [regex]::Matches($tok, '\((?:\\.|[^\\()])*\)')
        } else {
            $strs = [regex]::Matches($tok, '\((?:\\.|[^\\()])*\)')
        }
        $lineParts = New-Object System.Collections.Generic.List[string]
        foreach ($sm in $strs) {
            $s = $sm.Value.Substring(1, $sm.Value.Length - 2)
            $s = $s -replace '\\\(', '(' -replace '\\\)', ')' -replace '\\\\', '\'
            $s = $s -replace '\\n', "`n" -replace '\\r', ''
            $lineParts.Add($s)
        }
        [void]$allText.Append([string]::Join('', $lineParts))
    }
    [void]$allText.Append("`n")
}

Write-Output "Processed $count FlateDecode streams"
[System.IO.File]::WriteAllText($OutPath, $allText.ToString(), [System.Text.Encoding]::UTF8)
Write-Output "Wrote $($allText.Length) chars to $OutPath"
