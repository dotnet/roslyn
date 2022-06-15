<#
.SYNOPSIS
    Uploads code coverage to codecov.io
.PARAMETER CodeCovToken
    Code coverage token to use
.PARAMETER PathToCodeCoverage
    Path to root of code coverage files
.PARAMETER Name
    Optional name to upload with codecoverge
.PARAMETER Flags
    Optional flags to upload with codecoverge
#>
[CmdletBinding()]
Param (
    [string]$CodeCovToken,
    [string]$PathToCodeCoverage,
    [string]$Name="",
    [string]$Flags=""
)

$RepoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$CodeCoveragePathWildcard = (Join-Path $PathToCodeCoverage "*.cobertura.xml")

Write-Host "RepoRoot: $RepoRoot" -ForegroundColor Yellow
Write-Host "CodeCoveragePathWildcard: $CodeCoveragePathWildcard" -ForegroundColor Yellow

Get-ChildItem -Recurse -Path $CodeCoveragePathWildcard | % {

    if ($IsMacOS -or $IsLinux)
    {
        $relativeFilePath = Resolve-Path -relative $_
    }
    else
    {
        $relativeFilePath = Resolve-Path -relative (Get-ChildItem $_ | Select-Object -ExpandProperty Target)
    }

    Write-Host "Uploading: $relativeFilePath" -ForegroundColor Yellow
    Write-Host "Flags: $Flags$TargetFrameworkFlag$TestTypeFlag$OSTypeFlag" -ForegroundColor Yellow

    & (& "$PSScriptRoot/Get-CodeCovTool.ps1") -t "$CodeCovToken" -f "$relativeFilePath" -R "$RepoRoot" -F "$Flags" -n "$Name"
}
