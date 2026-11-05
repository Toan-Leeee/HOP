$ErrorActionPreference = 'Stop'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\MeetingScheduler.Web')).Path
Set-Location -LiteralPath $root
dotnet bin/Debug/net9.0/MeetingScheduler.Web.dll --urls http://localhost:5090
