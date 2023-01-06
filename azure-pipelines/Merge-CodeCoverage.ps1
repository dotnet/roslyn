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
    [string]$OutputFile=("$PSScriptRoot/../coveragereport/merged.cobertura.xml")
)

$RepoRoot = [string](Resolve-Path $PSScriptRoot/..)

if (!(Test-Path $RepoRoot/obj/dotnet-coverage*)) {
    dotnet tool install --tool-path $RepoRoot/obj dotnet-coverage --version 17.4.3 --configfile $PSScriptRoot/justnugetorg.nuget.config
}

Write-Verbose "Searching $Path for *.cobertura.xml files"
$reports = Get-ChildItem -Recurse $Path -Filter *.cobertura.xml

if ($reports) {
    $reports |% { $_.FullName } |% {
        # In addition to replacing {reporoot}, we also normalize on one kind of slash so that the report aggregates data for a file whether data was collected on Windows or not.
        $xml = [xml](Get-Content -Path $_)
        $xml.coverage.packages.package.classes.class |? { $_.filename} |% {
            $_.filename = $_.filename.Replace('{reporoot}', $RepoRoot).Replace([IO.Path]::AltDirectorySeparatorChar, [IO.Path]::DirectorySeparatorChar)
        }

        $xml.Save($_)
    }

    $Inputs = $reports |% { Resolve-Path -relative $_.FullName }

    if ((Split-Path $OutputFile) -and -not (Test-Path (Split-Path $OutputFile))) {
        New-Item -Type Directory -Path (Split-Path $OutputFile) | Out-Null
    }

    & "$RepoRoot/obj/dotnet-coverage" merge $Inputs -o $OutputFile -f cobertura
} else {
    Write-Error "No reports found to merge."
}
