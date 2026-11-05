param([string]$RelativeFile='Models\Room.cs',[int]$Line=1)
$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\MeetingScheduler.Web')).Path
$taskFile=(Resolve-Path -LiteralPath (Join-Path $taskRoot $RelativeFile)).Path
if (!$taskFile.StartsWith($taskRoot+'\')) { throw 'Outside project.' }
$taskDte=[Runtime.InteropServices.Marshal]::GetActiveObject('VisualStudio.DTE.17.0')
$taskDte.MainWindow.WindowState=1
$taskWindow=$taskDte.ItemOperations.OpenFile($taskFile)
$taskWindow.Activate()
$taskDte.ActiveDocument.Selection.GotoLine($Line,$false)
$taskDte.ExecuteCommand('Edit.DocumentStart')
Write-Output $taskDte.ActiveDocument.FullName
