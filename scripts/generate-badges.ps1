# Script for generating our badges line in the README.md

[CmdletBinding(PositionalBinding=$false)]
param ()

$branchNames = @(
    'main',
    'main-vs-deps')

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

function Get-BuildsTable() {
    $jobNames = @(
        'Build_Windows_Debug'
        'Build_Windows_Release'
        'Build_Unix_Debug'
    )

    $table = @'
#### Builds

|Branch|Windows Debug|Windows Release|Unix Debug|
|:--:|:--:|:--:|:--:|

'@
    foreach ($branchName in $branchNames) {
        $table += Get-AzureLine $branchName $jobNames
    }
    return $table
}

function Get-DesktopTable() {
    $jobNames = @(
        'Test_Windows_Desktop_Debug_32'
        'Test_Windows_Desktop_Debug_64'
        'Test_Windows_Desktop_Release_32'
        'Test_Windows_Desktop_Release_64'
    )

    $table = @'
#### Desktop Unit Tests

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
        'Test_Windows_CoreClr_Debug',
        'Test_Windows_CoreClr_Release',
        'Test_Linux_Debug'
    )

    $table = @'
#### CoreClr Unit Tests

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
        'VS_Integration#debug_32',
        'VS_Integration#debug_64',
        'VS_Integration#release_32',
        'VS_Integration#release_64'
    )

    $table = @'
#### Integration Tests

|Branch|Debug x86|Debug x64|Release x86|Release x64
|:--:|:--:|:--:|:--:|:--:|

'@

    foreach ($branchName in $branchNames) {
        $table += Get-AzureLine $branchName $jobNames -integration:$true
    }
    return $table

}

function Get-MiscTable() {
    $jobNames = @(
        'Correctness_Determinism',
        'Correctness_Build',
        'Correctness_SourceBuild',
        'Test_Windows_Desktop_Spanish_Release_32',
        'Test_macOS_Debug'
    )

    $table = @'
#### Misc Tests

|Branch|Determinism|Build Correctness|Source build|Spanish|MacOS|
|:--:|:--:|:--|:--:|:--:|:--:|

'@

    foreach ($branchName in $branchNames) {
        $table += Get-AzureLine $branchName $jobNames
    }
    return $table
}


Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"
try {
    Get-BuildsTable | Write-Output
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
