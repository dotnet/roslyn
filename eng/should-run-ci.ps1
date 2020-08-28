. (Join-Path $PSScriptRoot "build-utils.ps1")

Write-Host "Yes this script really exists and is being run"
ShouldRunCI -AsOutput
$str = if ($_ShouldRunCI) { "true" } else { "false" }
Write-Host "##vso[task.setvariable variable=ShouldRunCI]$str"
