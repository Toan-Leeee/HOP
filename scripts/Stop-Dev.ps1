$ErrorActionPreference = 'Stop'
$taskWebRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\MeetingScheduler.Web')).Path
$taskBinRoot = $taskWebRoot + '\bin\'
$taskRunning = Get-CimInstance Win32_Process | Where-Object {
    $_.Name -eq 'MeetingScheduler.Web.exe' -and $_.ExecutablePath -and
    $_.ExecutablePath.StartsWith($taskBinRoot, [System.StringComparison]::OrdinalIgnoreCase)
}
foreach ($taskProcess in $taskRunning) {
    Stop-Process -Id $taskProcess.ProcessId
    Write-Output "Stopped MeetingScheduler.Web (PID $($taskProcess.ProcessId))."
}
if (!$taskRunning) { Write-Output 'MeetingScheduler.Web is not running from this project.' }
