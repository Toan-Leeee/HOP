$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$inputFile = Join-Path $root 'Nop_ca_nhan_Le_Xuan_Toan\23A1001D0361_Le_Xuan_Toan_DataFlow_Room.docx'
$outputFile = Join-Path $root 'docs\qa\toan_word\preview.pdf'
$localInput = Join-Path $env:TEMP 'toan_dataflow_render.docx'
Copy-Item -LiteralPath $inputFile -Destination $localInput -Force
$word = New-Object -ComObject Word.Application
Write-Output 'Word initialized'
$word.Visible = $false
$word.DisplayAlerts = 0
$word.AutomationSecurity = 3
try {
    $document = $word.Documents.Open($localInput, $false, $true)
    Write-Output 'Document opened'
    $word.Options.Pagination = $false
    Write-Output 'Exporting PDF'
    $document.ExportAsFixedFormat($outputFile, 17)
    $document.Close(0)
} finally {
    $word.Quit()
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($word)
}
Write-Output $outputFile
