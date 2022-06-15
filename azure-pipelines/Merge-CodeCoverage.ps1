#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Merges code coverage reports.
.PARAMETER Path
    The path(s) to search for Cobertura code coverage reports.
.PARAMETER Format
    The format for the merged result. The default is Cobertura
.PARAMETER OutputDir
    The directory the merged result will be written to. The default is `coveragereport` in the root of this repo.
#>
[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)]
    [string[]]$Path,
    [ValidateSet('Badges', 'Clover', 'Cobertura', 'CsvSummary', 'Html', 'Html_Dark', 'Html_Light', 'HtmlChart', 'HtmlInline', 'HtmlInline_AzurePipelines', 'HtmlInline_AzurePipelines_Dark', 'HtmlInline_AzurePipelines_Light', 'HtmlSummary', 'JsonSummary', 'Latex', 'LatexSummary', 'lcov', 'MarkdownSummary', 'MHtml', 'PngChart', 'SonarQube', 'TeamCitySummary', 'TextSummary', 'Xml', 'XmlSummary')]
    [string]$Format='Cobertura',
    [string]$OutputDir=("$PSScriptRoot/../coveragereport")
)

$RepoRoot = [string](Resolve-Path $PSScriptRoot/..)

if (!(Test-Path $RepoRoot/obj/reportgenerator*)) {
    dotnet tool install --tool-path $RepoRoot/obj dotnet-reportgenerator-globaltool --version 5.1.9 --configfile $PSScriptRoot/justnugetorg.nuget.config
}

Write-Verbose "Searching $Path for *.cobertura.xml files"
$reports = Get-ChildItem -Recurse $Path -Filter *.cobertura.xml

if ($reports) {
    $reports |% { $_.FullName } |% {
        # In addition to replacing {reporoot}, we also normalize on one kind of slash so that the report aggregates data for a file whether data was collected on Windows or not.
        $content = Get-Content -Path $_ |% { [Regex]::Replace($_, '{reporoot}([^"]+)', { $RepoRoot + $args[0].groups[1].value.replace([IO.Path]::AltDirectorySeparatorChar, [IO.Path]::DirectorySeparatorChar) }) }
        Set-Content -Path $_ -Value $content -Encoding UTF8
    }

    $Inputs = [string]::join(';', ($reports |% { Resolve-Path -relative $_.FullName }))
    & "$RepoRoot/obj/reportgenerator" -reports:"$Inputs" -targetdir:$OutputDir -reporttypes:$Format
} else {
    Write-Error "No reports found to merge."
}
