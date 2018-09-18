# See https://raw.githubusercontent.com/Microsoft/vsts-tasks/master/Tasks/PublishBuildArtifactsV1/Invoke-Robocopy.ps1

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$source,

    [Parameter(Mandatory = $true)]
    [string]$target,

    [Parameter(Mandatory = $true)]
    [int]$parallelCount,

    [Parameter(Mandatory = $false)]
    [string]$file,
    
    [Parameter(Mandatory = $false)]
    [string[]]$exclude)

$ErrorActionPreference = 'Stop'
Set-StrictMode -version 2.0

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")

    if (!$File) {
        $File = "*";
    }

    $commandLine = "/E /COPY:DA /NP /R:3 "
    if ($parallelCount -gt 1) {
        $commandLine += "/MT:$parallelCount "
    }

    $commandLine += "$source $target $file "

    if (($null -ne $exclude) -and ($exclude.Length -gt 0)) {
        $commandLine += "/XD "
        foreach ($e in $exclude) {
            $commandLine += "$e "
        }
    }

    Write-Host "robocopy $commandLine"
    $exitCode = Exec-Process "robocopy" $commandLine
    if ($exitCode -gt 8) {
        Write-Host "robocopy failed $exitCode"
    }

    exit 0
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
