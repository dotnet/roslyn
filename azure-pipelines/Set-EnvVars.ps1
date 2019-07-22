<#
.SYNOPSIS
    Set environment variables in the environment.
    Azure Pipeline and CMD environments are considered.
.PARAMETER Variables
    A hashtable of variables to be set.
.OUTPUTS
    A boolean indicating whether the environment variables can be expected to propagate to the caller's environment.
#>
[CmdletBinding(SupportsShouldProcess=$true)]
Param(
    [Parameter(Mandatory=$true, Position=1)]
    $Variables
)

if ($Variables.Count -eq 0) {
    return $true
}

$cmdInstructions = !$env:TF_BUILD -and $env:PS1UnderCmd -eq '1'
if ($cmdInstructions) {
    Write-Warning "Environment variables have been set that will be lost because you're running under cmd.exe"
    Write-Host "Environment variables that must be set manually:" -ForegroundColor Blue
}

$Variables.GetEnumerator() |% {
    Set-Item -Path env:$($_.Key) -Value $_.Value

    # If we're running in Azure Pipelines, set these environment variables
    if ($env:TF_BUILD) {
        Write-Host "##vso[task.setvariable variable=$($_.Key);]$($_.Value)"
    }

    if ($cmdInstructions) {
        Write-Host "SET $($_.Key)=$($_.Value)"
    }
}

return !$cmdInstructions
