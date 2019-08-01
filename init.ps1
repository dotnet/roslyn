<#
.SYNOPSIS
Installs dependencies required to build and test the projects in this repository.
.DESCRIPTION
This MAY not require elevation, as the SDK and runtimes are installed locally to this repo location,
unless `-InstallLocality machine` is specified.
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
.PARAMETER NoRestore
Skips the package restore step.
.PARAMETER Signing
Install the MicroBuild signing plugin for building test-signed builds on desktop machines.
.PARAMETER Localization
Install the MicroBuild localization plugin for building loc builds on desktop machines.
The environment is configured to build pseudo-loc for JPN only, but may be used to build
all languages with shipping-style loc by using the `/p:loctype=full,loclanguages=vs`
when building.
.PARAMETER Setup
Install the MicroBuild setup plugin for building VSIXv3 packages.
.PARAMETER OptProf
Install the MicroBuild OptProf plugin for building optimized assemblies on desktop machines.
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
    [switch]$NoRestore,
    [Parameter()]
    [switch]$Signing,
    [Parameter()]
    [switch]$Localization,
    [Parameter()]
    [switch]$Setup,
    [Parameter()]
    [switch]$OptProf,
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

    $EnvVars = @{}
    $InstallNuGetPkgScriptPath = ".\azure-pipelines\Install-NuGetPackage.ps1"
    $nugetVerbosity = 'quiet'
    if ($Verbose) { $nugetVerbosity = 'normal' }
    $MicroBuildPackageSource = 'https://devdiv.pkgs.visualstudio.com/DefaultCollection/_packaging/MicroBuildToolset/nuget/v3/index.json'
    if ($Signing) {
        Write-Host "Installing MicroBuild signing plugin" -ForegroundColor $HeaderColor
        & $InstallNuGetPkgScriptPath MicroBuild.Plugins.Signing -source $MicroBuildPackageSource -Verbosity $nugetVerbosity
        $EnvVars['SignType'] = "Test"
    }

    if ($Setup) {
        Write-Host "Installing MicroBuild SwixBuild plugin..." -ForegroundColor $HeaderColor
        & $InstallNuGetPkgScriptPath MicroBuild.Plugins.SwixBuild -source $MicroBuildPackageSource -Verbosity $nugetVerbosity
    }

    if ($OptProf) {
        Write-Host "Installing MicroBuild OptProf plugin" -ForegroundColor $HeaderColor
        & $InstallNuGetPkgScriptPath MicroBuild.Plugins.OptProf -source $MicroBuildPackageSource -Verbosity $nugetVerbosity
        $EnvVars['OptProfEnabled'] = '1'
    }

    if ($Localization) {
        Write-Host "Installing MicroBuild localization plugin" -ForegroundColor $HeaderColor
        & $InstallNuGetPkgScriptPath MicroBuild.Plugins.Localization -source $MicroBuildPackageSource -Verbosity $nugetVerbosity
        $EnvVars['LocType'] = "Pseudo"
        $EnvVars['LocLanguages'] = "JPN"
    }

    & ".\azure-pipelines\Set-EnvVars.ps1" -Variables $EnvVars
}
catch {
    Write-Error $error[0]
    exit $lastexitcode
}
finally {
    Pop-Location
}
