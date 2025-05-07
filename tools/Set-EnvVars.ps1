<#
.SYNOPSIS
    Set environment variables in the environment.
    Azure Pipeline and CMD environments are considered.
.PARAMETER Variables
    A hashtable of variables to be set.
.PARAMETER PrependPath
    A set of paths to prepend to the PATH environment variable.
.OUTPUTS
    A boolean indicating whether the environment variables can be expected to propagate to the caller's environment.
.DESCRIPTION
    The CmdEnvScriptPath environment variable may be optionally set to a path to a cmd shell script to be created (or appended to if it already exists) that will set the environment variables in cmd.exe that are set within the PowerShell environment.
    This is used by init.cmd in order to reapply any new environment variables to the parent cmd.exe process that were set in the powershell child process.
#>
[CmdletBinding(SupportsShouldProcess=$true)]
Param(
    [Parameter(Mandatory=$true, Position=1)]
    $Variables,
    [string[]]$PrependPath
)

if ($Variables.Count -eq 0) {
    return $true
}

$cmdInstructions = !$env:TF_BUILD -and !$env:GITHUB_ACTIONS -and !$env:CmdEnvScriptPath -and ($env:PS1UnderCmd -eq '1')
if ($cmdInstructions) {
    Write-Warning "Environment variables have been set that will be lost because you're running under cmd.exe"
    Write-Host "Environment variables that must be set manually:" -ForegroundColor Blue
} else {
    Write-Host "Environment variables set:" -ForegroundColor Blue
    Write-Host ($Variables | Out-String)
    if ($PrependPath) {
        Write-Host "Paths prepended to PATH: $PrependPath"
    }
}

if ($env:TF_BUILD) {
    Write-Host "Azure Pipelines detected. Logging commands will be used to propagate environment variables and prepend path."
}

if ($env:GITHUB_ACTIONS) {
    Write-Host "GitHub Actions detected. Logging commands will be used to propagate environment variables and prepend path."
}

$CmdEnvScript = ''
$Variables.GetEnumerator() |% {
    Set-Item -LiteralPath env:$($_.Key) -Value $_.Value

    # If we're running in a cloud CI, set these environment variables so they propagate.
    if ($env:TF_BUILD) {
        Write-Host "##vso[task.setvariable variable=$($_.Key);]$($_.Value)"
    }
    if ($env:GITHUB_ACTIONS) {
        Add-Content -LiteralPath $env:GITHUB_ENV -Value "$($_.Key)=$($_.Value)"
    }

    if ($cmdInstructions) {
        Write-Host "SET $($_.Key)=$($_.Value)"
    }

    $CmdEnvScript += "SET $($_.Key)=$($_.Value)`r`n"
}

$pathDelimiter = ';'
if ($IsMacOS -or $IsLinux) {
    $pathDelimiter = ':'
}

if ($PrependPath) {
    $PrependPath |% {
        $newPathValue = "$_$pathDelimiter$env:PATH"
        Set-Item -LiteralPath env:PATH -Value $newPathValue
        if ($cmdInstructions) {
            Write-Host "SET PATH=$newPathValue"
        }

        if ($env:TF_BUILD) {
            Write-Host "##vso[task.prependpath]$_"
        }
        if ($env:GITHUB_ACTIONS) {
            Add-Content -LiteralPath $env:GITHUB_PATH -Value $_
        }

        $CmdEnvScript += "SET PATH=$_$pathDelimiter%PATH%"
    }
}

if ($env:CmdEnvScriptPath) {
    if (Test-Path $env:CmdEnvScriptPath) {
        $CmdEnvScript = (Get-Content -LiteralPath $env:CmdEnvScriptPath) + $CmdEnvScript
    }

    Set-Content -LiteralPath $env:CmdEnvScriptPath -Value $CmdEnvScript
}

return !$cmdInstructions
