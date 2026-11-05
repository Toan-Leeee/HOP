$ErrorActionPreference='Stop'
$word=[Runtime.InteropServices.Marshal]::GetActiveObject('Word.Application')
$document=$null
foreach($item in $word.Documents){
    if($item.Name -eq '23A1001D0361_Le_Xuan_Toan_AJAX_Room.docx'){$document=$item;break}
}
if($null -eq $document){throw 'Report is not open in Word.'}
$xml=$document.Content.WordOpenXML
$target=Join-Path (Join-Path $PSScriptRoot '..\docs\qa\ajax_word') 'current_report.xml'
[System.IO.File]::WriteAllText($target,$xml,[System.Text.UTF8Encoding]::new($false))
Write-Output $target
