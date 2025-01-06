<#
.SYNOPSIS
    Uploads code coverage to codecov.io
.PARAMETER CodeCovToken
    Code coverage token to use
.PARAMETER PathToCodeCoverage
    Path to root of code coverage files
.PARAMETER Name
    Name to upload with codecoverge
.PARAMETER Flags
    Flags to upload with codecoverge
#>
[CmdletBinding()]
Param (
    [Parameter(Mandatory=$true)]
    [string]$CodeCovToken,
    [Parameter(Mandatory=$true)]
    [string]$PathToCodeCoverage,
    [string]$Name,
    [string]$Flags
)

$RepoRoot = (Resolve-Path "$PSScriptRoot/..").Path

Get-ChildItem -Recurse -LiteralPath $PathToCodeCoverage -Filter "*.cobertura.xml" | % {
    $relativeFilePath = Resolve-Path -relative $_.FullName

    Write-Host "Uploading: $relativeFilePath" -ForegroundColor Yellow
    & (& "$PSScriptRoot/Get-CodeCovTool.ps1") -t $CodeCovToken -f $relativeFilePath -R $RepoRoot -F $Flags -n $Name
}
