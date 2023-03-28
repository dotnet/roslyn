#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Installs dependencies required to build and test the projects in this repository.
.DESCRIPTION
    This MAY not require elevation, as the SDK and runtimes are installed to a per-user location,
    unless the `-InstallLocality` switch is specified directing to a per-repo or per-machine location.
    See detailed help on that switch for more information.

    The CmdEnvScriptPath environment variable may be optionally set to a path to a cmd shell script to be created (or appended to if it already exists) that will set the environment variables in cmd.exe that are set within the PowerShell environment.
    This is used by init.cmd in order to reapply any new environment variables to the parent cmd.exe process that were set in the powershell child process.
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
.PARAMETER NoNuGetCredProvider
    Skips the installation of the NuGet credential provider. Useful in pipelines with the `NuGetAuthenticate` task, as a workaround for https://github.com/microsoft/artifacts-credprovider/issues/244.
    This switch is ignored and installation is skipped when -NoPrerequisites is specified.
.PARAMETER UpgradePrerequisites
    Takes time to install prerequisites even if they are already present in case they need to be upgraded.
    No effect if -NoPrerequisites is specified.
.PARAMETER NoRestore
    Skips the package restore step.
.PARAMETER NoToolRestore
    Skips the dotnet tool restore step.
.PARAMETER AccessToken
    An optional access token for authenticating to Azure Artifacts authenticated feeds.
.PARAMETER Interactive
    Runs NuGet restore in interactive mode. This can turn authentication failures into authentication challenges.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
Param (
    [ValidateSet('repo', 'user', 'machine')]
    [string]$InstallLocality = 'user',
    [Parameter()]
    [switch]$NoPrerequisites,
    [Parameter()]
    [switch]$NoNuGetCredProvider,
    [Parameter()]
    [switch]$UpgradePrerequisites,
    [Parameter()]
    [switch]$NoRestore,
    [Parameter()]
    [switch]$NoToolRestore,
    [Parameter()]
    [string]$AccessToken,
    [Parameter()]
    [switch]$Interactive
)

$EnvVars = @{}
$PrependPath = @()

if (!$NoPrerequisites) {
    if (!$NoNuGetCredProvider) {
        & "$PSScriptRoot\tools\Install-NuGetCredProvider.ps1" -AccessToken $AccessToken -Force:$UpgradePrerequisites
    }

    & "$PSScriptRoot\tools\Install-DotNetSdk.ps1" -InstallLocality $InstallLocality
    if ($LASTEXITCODE -eq 3010) {
        Exit 3010
    }

    # The procdump tool and env var is required for dotnet test to collect hang/crash dumps of tests.
    # But it only works on Windows.
    if ($env:OS -eq 'Windows_NT') {
        $EnvVars['PROCDUMP_PATH'] = & "$PSScriptRoot\azure-pipelines\Get-ProcDump.ps1"
    }
}

# Workaround nuget credential provider bug that causes very unreliable package restores on Azure Pipelines
$env:NUGET_PLUGIN_HANDSHAKE_TIMEOUT_IN_SECONDS = 20
$env:NUGET_PLUGIN_REQUEST_TIMEOUT_IN_SECONDS = 20

Push-Location $PSScriptRoot
try {
    $HeaderColor = 'Green'

    if (!$NoRestore -and $PSCmdlet.ShouldProcess("NuGet packages", "Restore")) {
        $RestoreArguments = @()
        if ($Interactive)
        {
            $RestoreArguments += '--interactive'
        }

        Write-Host "Restoring NuGet packages" -ForegroundColor $HeaderColor
        dotnet restore @RestoreArguments
        if ($lastexitcode -ne 0) {
            throw "Failure while restoring packages."
        }
    }

    if (!$NoToolRestore -and $PSCmdlet.ShouldProcess("dotnet tool", "restore")) {
      dotnet tool restore @RestoreArguments
      if ($lastexitcode -ne 0) {
          throw "Failure while restoring dotnet CLI tools."
      }
    }

    & "$PSScriptRoot/tools/Set-EnvVars.ps1" -Variables $EnvVars -PrependPath $PrependPath | Out-Null
}
catch {
    Write-Error $error[0]
    exit $lastexitcode
}
finally {
    Pop-Location
}
