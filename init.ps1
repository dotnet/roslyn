<#
.SYNOPSIS
Installs dependencies required to build and test the projects in this repository.
.DESCRIPTION
This does not require elevation, as the dependencies are installed in per-user locations.
.PARAMETER InstallLocality
A value indicating whether dependencies should be installed locally to the repo or at a per-user location.
Per-user allows sharing the installed dependencies across repositories and allows use of a shared expanded package cache.
Per-repo allows for high isolation, allowing for a more precise recreation of the environment within an Azure Pipelines build.
When using 'repo', environment variables are set to cause the locally installed dotnet SDK to be used.
Per-repo can lead to file locking issues when dotnet.exe is left running as a build server and can be mitigated by running `dotnet build-server shutdown`.
.PARAMETER NoPrerequisites
Skips the installation of prerequisite software (e.g. SDKs, tools).
.PARAMETER NoRestore
Skips the package restore step.
.PARAMETER AccessToken
An optional access token for authenticating to Azure Artifacts authenticated feeds.
#>
[CmdletBinding(SupportsShouldProcess=$true)]
Param (
    [ValidateSet('repo','user')]
    [string]$InstallLocality='user',
    [Parameter()]
    [switch]$NoPrerequisites,
    [Parameter()]
    [switch]$NoRestore,
    [Parameter()]
    [string]$AccessToken
)

if (!$NoPrerequisites) {
    & "$PSScriptRoot\tools\Install-NuGetCredProvider.ps1" -AccessToken $AccessToken
    & "$PSScriptRoot\tools\Install-DotNetSdk.ps1" -InstallLocality $InstallLocality
}

Push-Location $PSScriptRoot
try {
    $HeaderColor = 'Green'

    if (!$NoRestore) {
        Write-Host "Restoring NuGet packages" -ForegroundColor $HeaderColor
        dotnet restore src
        if ($lastexitcode -ne 0) {
            throw "Failure while restoring packages."
        }
    }
}
catch {
    Write-Error $error[0]
    exit $lastexitcode
}
finally {
    Pop-Location
}
