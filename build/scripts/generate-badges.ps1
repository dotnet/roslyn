[CmdletBinding(PositionalBinding=$false)]
param ([string]$branch = "master")

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"
try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    
    $jobNames = @(
        'Windows_Desktop_Unit_Tests debug_32',
        'Windows_Desktop_Unit_Tests debug_64'
    )

    foreach ($jobName in $jobNames) {
        $jobName = $jobName.Replace(" ", "%20")
        $template = "[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchName=$branch&jobname=$jobName)]"
        $template += "(https://dev.azure.com/dnceng/public/_build/latest?definitionId=15)"
        Write-Host $template
    }


    exit 0
}
catch {
    Write-Host $_
    exit 1
}
