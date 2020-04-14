#!/usr/bin/env pwsh

<#
.SYNOPSIS
Installs dependencies required to build and test the projects in this repository.
.DESCRIPTION
This MAY not require elevation, as the SDK and runtimes are installed to a per-user location,
unless the `-InstallLocality` switch is specified directing to a per-repo or per-machine location.
See detailed help on that switch for more information.
.PARAMETER InstallLocality
A value indicating whether dependencies should be installed locally to the repo or at a per-user location.
Per-user allows sharing the installed dependencies across repositories and allows use of a shared expanded package cache.
Visual Studio will only notice and use these SDKs/runtimes if VS is launched from the environment that runs this script.
Per-repo allows for high isolation, allowing for a more precise recreation of the environment within an Azure Pipelines build.
When using 'repo', environment variables are set to cause the locally installed dotnet SDK to be used.
Per-repo can lead to file locking issues when dotnet.exe is left running as a build server and can be mitigated by running `dotnet build-server shutdown`.
Per-machine requires elevation and will download and install all SDKs and runtimes to machine-wide locations so all applications can find it.
.PARAMETER NoPrerequisites
Skips the installation of prerequisite software (e.g. SDKs, tools).
.PARAMETER UpgradePrerequisites
Takes time to install prerequisites even if they are already present in case they need to be upgraded.
No effect if -NoPrerequisites is specified.
.PARAMETER NoRestore
Skips the package restore step.
.PARAMETER AccessToken
An optional access token for authenticating to Azure Artifacts authenticated feeds.
#>
[CmdletBinding(SupportsShouldProcess=$true)]
Param (
    [ValidateSet('repo','user','machine')]
    [string]$InstallLocality='user',
    [Parameter()]
    [switch]$NoPrerequisites,
    [Parameter()]
    [switch]$UpgradePrerequisites,
    [Parameter()]
    [switch]$NoRestore,
    [Parameter()]
    [string]$AccessToken
)

$EnvVars = @{}

if (!$NoPrerequisites) {
    & "$PSScriptRoot\tools\Install-NuGetCredProvider.ps1" -AccessToken $AccessToken -Force:$UpgradePrerequisites
    & "$PSScriptRoot\tools\Install-DotNetSdk.ps1" -InstallLocality $InstallLocality

    # The procdump tool and env var is required for dotnet test to collect hang/crash dumps of tests.
    # But it only works on Windows.
    if ($env:OS -eq 'Windows_NT') {
        $EnvVars['PROCDUMP_PATH'] = & "$PSScriptRoot\azure-pipelines\Get-ProcDump.ps1"
    }
}

# Workaround nuget credential provider bug that causes very unreliable package restores on Azure Pipelines
$env:NUGET_PLUGIN_HANDSHAKE_TIMEOUT_IN_SECONDS=20
$env:NUGET_PLUGIN_REQUEST_TIMEOUT_IN_SECONDS=20

Push-Location $PSScriptRoot
try {
    $HeaderColor = 'Green'

    if (!$NoRestore -and $PSCmdlet.ShouldProcess("NuGet packages", "Restore")) {
        Write-Host "Restoring NuGet packages" -ForegroundColor $HeaderColor
        dotnet restore
        if ($lastexitcode -ne 0) {
            throw "Failure while restoring packages."
        }
    }

    & "$PSScriptRoot\azure-pipelines\Set-EnvVars.ps1" -Variables $EnvVars | Out-Null
}
catch {
    Write-Error $error[0]
    exit $lastexitcode
}
finally {
    Pop-Location
}
