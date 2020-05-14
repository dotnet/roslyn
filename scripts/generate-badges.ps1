# Script for generating our badges line in the README.md

[CmdletBinding(PositionalBinding=$false)]
param ()

$branchNames = @(
    'master',
    'master-vs-deps')

function Get-AzureBadge($branchName, $jobName, $configName, [switch]$integration = $false) {
    $name = if ($integration) { "roslyn-integration-CI" } else { "roslyn-CI" }
    $id = if ($integration) { 245 } else { 15 }
    $template = "[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/$($name)?branchname=$branchName&jobname=$jobName&configuration=$jobName$configName&label=build)]"
    $template += "(https://dev.azure.com/dnceng/public/_build/latest?definitionId=$($id)&branchname=$branchName&view=logs)"
    return $template
}

function Get-AzureLine($branchName, $jobNames, [switch]$integration = $false) {
    $line = "**$branchName**|"
    foreach ($jobName in $jobNames) {

        $configName = ""
        $i = $jobName.IndexOf('#')
        if ($i -ge 0)
        {
            $configName = "%20$($jobName.SubString($i + 1))"
            $jobName = $jobName.Substring(0, $i)
        }

        $line += Get-AzureBadge $branchName $jobName $configName -integration:$integration
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
    )

    $table = @'
### Desktop Unit Tests
|Branch|Debug x86|Debug x64|Release x86|Release x64|
|:--:|:--:|:--:|:--:|:--:|

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
        'VS_Integration#debug_async',
        'VS_Integration#release_async',
        'VS_Integration#debug_legacy',
        'VS_Integration#release_legacy'
    )

    $table = @'
### Integration Tests
|Branch|Debug|Release|Debug (Legacy completion)|Release (Legacy completion)
|:--:|:--:|:--:|:--:|:--:|

'@

    foreach ($branchName in $branchNames) {
        $table += Get-AzureLine $branchName $jobNames -integration:$true
    }
    return $table

}

function Get-MiscTable() {
    $jobNames = @(
        'Windows_Determinism_Test',
        'Windows_Correctness_Test',
        'Windows_Desktop_Spanish_Unit_Tests',
        'Linux_Test#mono'
    )

    $table = @'
### Misc Tests
|Branch|Determinism|Build Correctness|Mono|Spanish|
|:--:|:--:|:--:|:--:|:--:|

'@

    foreach ($branchName in $branchNames) {
        $table += Get-AzureLine $branchName $jobNames
    }
    return $table
}


Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"
try {

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
