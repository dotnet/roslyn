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
#>
[CmdletBinding(SupportsShouldProcess=$true)]
Param (
    [ValidateSet('repo','user')]
    [string]$InstallLocality='user'
)

& "$PSScriptRoot\tools\Install-DotNetSdk.ps1" -InstallLocality $InstallLocality
