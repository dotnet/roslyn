. (Join-Path $PSScriptRoot "build-utils.ps1")

Write-Host "Yes this script really exists and is being run"
ShouldRunCI -AsOutput
$str = $_ShouldRunCI ? "true" : "false"
Write-Host "##vso[task.setvariable variable=ShouldRunCI]$str"
