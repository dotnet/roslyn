<#
.SYNOPSIS
    Installs a NuGet package.
.PARAMETER PackageID
    The Package ID to install.
.PARAMETER Version
    The version of the package to install. If unspecified, the latest stable release is installed.
.PARAMETER Source
    The package source feed to find the package to install from.
.PARAMETER PackagesDir
    The directory to install the package to. By default, it uses the Packages folder at the root of the repo.
.PARAMETER ConfigFile
    The nuget.config file to use. By default, it uses :/nuget.config.
.OUTPUTS
    System.String. The path to the installed package.
#>
[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='Low')]
Param(
    [Parameter(Position=1,Mandatory=$true)]
    [string]$PackageId,
    [Parameter()]
    [string]$Version,
    [Parameter()]
    [string]$Source,
    [Parameter()]
    [switch]$Prerelease,
    [Parameter()]
    [string]$PackagesDir="$PSScriptRoot\..\packages",
    [Parameter()]
    [string]$ConfigFile="$PSScriptRoot\..\nuget.config",
    [Parameter()]
    [ValidateSet('Quiet','Normal','Detailed')]
    [string]$Verbosity='normal'
)

$nugetPath = & "$PSScriptRoot\..\tools\Get-NuGetTool.ps1"

try {
    Write-Verbose "Installing $PackageId..."
    $nugetArgs = "Install",$PackageId,"-OutputDirectory",$PackagesDir,'-ConfigFile',$ConfigFile
    if ($Version) { $nugetArgs += "-Version",$Version }
    if ($Source) { $nugetArgs += "-FallbackSource",$Source }
    if ($Prerelease) { $nugetArgs += "-Prerelease" }
    $nugetArgs += '-Verbosity',$Verbosity

    if ($PSCmdlet.ShouldProcess($PackageId, 'nuget install')) {
        $p = Start-Process $nugetPath $nugetArgs -NoNewWindow -Wait -PassThru
        if ($null -ne $p.ExitCode -and $p.ExitCode -ne 0) { throw }
    }

    # Provide the path to the installed package directory to our caller.
    Write-Output (Get-ChildItem "$PackagesDir\$PackageId.*")[0].FullName
} finally {
    Pop-Location
}
