$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$source = Join-Path $root 'Nop_ca_nhan_Le_Xuan_Toan\Ajax_Room\23A1001D0361_Le_Xuan_Toan_AJAX_Room.docx'
$tempFile = Join-Path $env:TEMP 'toan_ajax_room_preview.docx'
Copy-Item -LiteralPath $source -Destination $tempFile -Force
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $document = $word.Documents.Open($tempFile, $false, $true)
    Write-Output 'Document opened'
    $document.ExportAsFixedFormat((Join-Path $root 'docs\qa\ajax_word\preview.pdf'), 17)
    Write-Output 'PDF exported'
    $document.Close(0)
} finally { $word.Quit() }
