$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$taskSource = Join-Path $taskRoot 'docs\Nhom7_Xac_dinh_thuc_the_DataLayer.docx'
$taskOut = Join-Path $taskRoot 'docs\qa\entity_submission'
New-Item -ItemType Directory -Path $taskOut -Force | Out-Null
$taskPdf = Join-Path $taskOut 'word-render.pdf'
$taskTemp = Join-Path $env:TEMP ('meeting-entity-qa-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskTemp | Out-Null
$taskTempDoc = Join-Path $taskTemp 'source.docx'
$taskTempPdf = Join-Path $taskTemp 'render.pdf'
Copy-Item -LiteralPath $taskSource -Destination $taskTempDoc
$taskWord = New-Object -ComObject Word.Application
$taskWord.Visible = $true
$taskWord.WindowState = 2
$taskWord.DisplayAlerts = 0
$taskDocument = $null
try {
    $taskDocument = $taskWord.Documents.Open($taskTempDoc, $false, $true)
    $taskDocument.Repaginate()
    Write-Output ('Word pages: ' + $taskDocument.ComputeStatistics(2))
    $taskDocument.ExportAsFixedFormat($taskTempPdf, 17, $false, 1, 0, 1, 4, 0, $false, $false, 0, $false, $false, $false)
} finally {
    if ($taskDocument) {
        $taskDocument.Close($false)
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskDocument)
    }
    $taskWord.Quit()
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskWord)
}
Copy-Item -LiteralPath $taskTempPdf -Destination $taskPdf -Force
& 'C:\Users\acer\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\poppler\Library\bin\pdftoppm.exe' -png -r 120 $taskPdf (Join-Path $taskOut 'page')
if ($LASTEXITCODE -ne 0) { throw 'PDF rasterization failed.' }
Get-ChildItem -LiteralPath $taskOut -Filter 'page-*.png' | Select-Object Name,Length
