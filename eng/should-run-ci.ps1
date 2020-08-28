. (Join-Path $PSScriptRoot "build-utils.ps1")

ShouldRunCI -AsOutput
Write-Host "##vso[task.setvariable variable=ShouldRunCI]$_ShouldRunCI"
