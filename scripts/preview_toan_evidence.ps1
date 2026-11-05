$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$source = Join-Path $root 'Nop_ca_nhan_Le_Xuan_Toan\23A1001D0361_Le_Xuan_Toan_DataFlow_Room_Co_Minh_Chung.docx'
$local = Join-Path $env:TEMP 'toan_evidence_preview.docx'
$pdf = Join-Path $env:TEMP 'toan_evidence_preview.pdf'
Copy-Item -LiteralPath $source -Destination $local -Force
$word = New-Object -ComObject Word.Application
Write-Output ('Word PID window: ' + $word.Hwnd)
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $document = $word.Documents.Open($local)
    Write-Output 'Opened'
    $document.SaveAs2($pdf, 17)
    Write-Output 'PDF saved'
    $document.Close(0)
} finally {
    $word.Quit()
}
New-Item -ItemType Directory -Path (Join-Path $root 'docs\qa\toan_evidence_word') -Force | Out-Null
Copy-Item -LiteralPath $pdf -Destination (Join-Path $root 'docs\qa\toan_evidence_word\preview.pdf') -Force
