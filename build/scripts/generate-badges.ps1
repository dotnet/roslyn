# Script for generating our badges line in the README.md

[CmdletBinding(PositionalBinding=$false)]
param ()

$branchNames = @(
    'master',
    'master-vs-deps',
    'dev16.0-preview2',
    'dev16.0-preview2-vs-deps')

function Get-AzureBadge($branchName, $jobName, $configName) {
    $template = "[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?label=build&branchname=$branchName&jobname=$jobName&configuration=$configName)]"
    $template += "(https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=$branchName)"
    return $template
}

function Get-AzureLine($branchName, $jobNames) {
    $line = "**$branchName**|"
    foreach ($jobName in $jobNames) {
        $i = $jobName.IndexOf('#')
        $configName = $jobName.SubString($i + 1)
        $jobName = $jobName.Substring(0, $i)

        $line += Get-AzureBadge $branchName $jobName $configName
        $line += "|"
    }

    return $line + [Environment]::NewLine
}

function Get-JenkinsBadge($branchName, $jobName) {
    $template = "[![Build Status](https://ci.dot.net/buildStatus/icon?job=dotnet_roslyn/$branchName/$jobName)]"
    $template += "(https://ci.dot.net/job/dotnet_roslyn/job/$branchName/job/$jobName/)"
    return $template
}

function Get-JenkinsLine($branchName, $jobNames) {
    $line = "**$branchName**|"
    foreach ($jobName in $jobNames) {
        $line += Get-JenkinsBadge $branchName $jobName
        $line += "|"
    }

    return $line + [Environment]::NewLine
}

function Get-DesktopTable() {
    $jobNames = @(
        'Windows_Desktop_Unit_Tests#debug_32'
        'Windows_Desktop_Unit_Tests#debug_64'
        'Windows_Desktop_Unit_Tests#release_32'
        'Windows_Desktop_Unit_Tests#release_64'
        'Windows_Desktop_Spanish_Unit_Tests#',
        'Windows_Determinism_Test#',
        'Windows_Correctness_Test#'
    )

    $table = @'
### Desktop Unit Tests
|Branch|Debug x86|Debug x64|Release x86|Release x64|Spanish|
|:--:|:--:|:--:|:--:|:--:|:--:|

'@
    foreach ($branchName in $branchNames) {
        $table += Get-AzureLine $branchName $jobNames
    }
    return $table
}

function Get-CoreClrTable() {
    $jobNames = @(
        'Windows_CoreClr_Unit_Tests#debug',
        'Windows_CoreClr_Unit_Tests#release',
        'Linux_Test#coreclr'
    )

    $table = @'
### CoreClr Unit Tests
|Branch|Windows Debug|Windows Release|Linux|
|:--:|:--:|:--:|:--:|

'@

    foreach ($branchName in $branchNames) {
        $table += Get-AzureLine $branchName $jobNames
    }
    return $table
}

function Get-IntegrationTable() {
    $jobNames = @(
        'windows_debug_vs-integration',
        'windows_release_vs-integration'
    )

    $table = @'
### Integration Tests
|Branch|Debug|Release|
|:--:|:--:|:--:|

'@

    foreach ($branchName in $branchNames) {
        $table += Get-JenkinsLine $branchName $jobNames
    }
    return $table

}

function Get-MiscTable() {
    $jobNames = @(
        'Windows_Determinism_Test#',
        'Windows_Correctness_Test#',
        'Linux_Test#mono'
    )

    $table = @'
### Misc Tests
|Branch|Determinism|Build Correctness|Mono|
|:--:|:--:|:--:|:--:|

'@

    foreach ($branchName in $branchNames) {
        $table += Get-AzureLine $branchName $jobNames
    }
    return $table
}


Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"
try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    

    Get-DesktopTable | Write-Output
    Get-CoreClrTable | Write-Output
    Get-IntegrationTable | Write-Output
    Get-MiscTable | Write-Output

    exit 0
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
