$ErrorActionPreference='Stop'
$word=[Runtime.InteropServices.Marshal]::GetActiveObject('Word.Application')
$document=$null
foreach($item in $word.Documents){
    if($item.Name -eq '23A1001D0361_Le_Xuan_Toan_AJAX_Du_Anh_Code_Va_Chay.docx'){$document=$item;break}
}
if($null -eq $document){throw 'Updated report is not open in Word.'}
$target=Join-Path $PSScriptRoot '..\docs\qa\ajax_code_word\preview.pdf'
New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
$document.ExportAsFixedFormat([System.IO.Path]::GetFullPath($target),17)
Write-Output $target
