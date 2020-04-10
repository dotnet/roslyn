#!/usr/bin/env pwsh

<#
.SYNOPSIS
Installs the .NET SDK specified in the global.json file at the root of this repository,
along with supporting .NET Core runtimes used for testing.
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
#>
[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='Medium')]
Param (
    [ValidateSet('repo','user','machine')]
    [string]$InstallLocality='user'
)

$DotNetInstallScriptRoot = "$PSScriptRoot/../obj/tools"
if (!(Test-Path $DotNetInstallScriptRoot)) { New-Item -ItemType Directory -Path $DotNetInstallScriptRoot -WhatIf:$false | Out-Null }
$DotNetInstallScriptRoot = Resolve-Path $DotNetInstallScriptRoot

# Look up actual required .NET Core SDK version from global.json
$sdkVersion = & "$PSScriptRoot/../azure-pipelines/variables/DotNetSdkVersion.ps1"

# Search for all .NET Core runtime versions referenced from MSBuild projects and arrange to install them.
$runtimeVersions = @()
Get-ChildItem "$PSScriptRoot\..\src\*.*proj","$PSScriptRoot\..\test\*.*proj","$PSScriptRoot\..\Directory.Build.props" -Recurse |% {
    $projXml = [xml](Get-Content -Path $_)
    $targetFrameworks = $projXml.Project.PropertyGroup.TargetFramework
    if (!$targetFrameworks) {
        $targetFrameworks = $projXml.Project.PropertyGroup.TargetFrameworks
        if ($targetFrameworks) {
            $targetFrameworks = $targetFrameworks -Split ';'
        }
    }
    $targetFrameworks |? { $_ -match 'netcoreapp(\d+\.\d+)' } |% {
        $runtimeVersions += $Matches[1]
    }
}

Function Get-FileFromWeb([Uri]$Uri, $OutDir) {
    $OutFile = Join-Path $OutDir $Uri.Segments[-1]
    if (!(Test-Path $OutFile)) {
        Write-Verbose "Downloading $Uri..."
        try {
            (New-Object System.Net.WebClient).DownloadFile($Uri, $OutFile)
        } finally {
            # This try/finally causes the script to abort
        }
    }

    $OutFile
}

Function Get-InstallerExe($Version, [switch]$Runtime) {
    $sdkOrRuntime = 'Sdk'
    if ($Runtime) { $sdkOrRuntime = 'Runtime' }

    # Get the latest/actual version for the specified one
    if (([Version]$Version).Build -eq -1) {
        $versionInfo = -Split (Invoke-WebRequest -Uri "https://dotnetcli.blob.core.windows.net/dotnet/$sdkOrRuntime/$Version/latest.version" -UseBasicParsing)
        $Version = $versionInfo[-1]
    }

    Get-FileFromWeb -Uri "https://dotnetcli.blob.core.windows.net/dotnet/$sdkOrRuntime/$Version/dotnet-$($sdkOrRuntime.ToLowerInvariant())-$Version-win-x64.exe" -OutDir "$DotNetInstallScriptRoot"
}

Function Install-DotNet($Version, [switch]$Runtime) {
    if ($Runtime) { $sdkSubstring = '' } else { $sdkSubstring = 'SDK ' }
    Write-Host "Downloading .NET Core $sdkSubstring$Version..."
    $Installer = Get-InstallerExe -Version $Version -Runtime:$Runtime
    Write-Host "Installing .NET Core $sdkSubstring$Version..."
    cmd /c start /wait $Installer /install /quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Failure to install .NET Core SDK"
    }
}

$switches = @(
    '-Architecture','x64'
)
$envVars = @{
    # For locally installed dotnet, skip first time experience which takes a long time
    'DOTNET_SKIP_FIRST_TIME_EXPERIENCE' = 'true';
}

if ($InstallLocality -eq 'machine') {
    if ($IsWindows) {
        if ($PSCmdlet.ShouldProcess(".NET Core SDK $sdkVersion", "Install")) {
            Install-DotNet -Version $sdkVersion
        }

        $runtimeVersions | Get-Unique |% {
            if ($PSCmdlet.ShouldProcess(".NET Core runtime $_", "Install")) {
                Install-DotNet -Version $_ -Runtime
            }
        }

        return
    } else {
        $DotNetInstallDir = '/usr/share/dotnet'
    }
} elseif ($InstallLocality -eq 'repo') {
    $DotNetInstallDir = "$DotNetInstallScriptRoot/.dotnet"
} elseif ($env:AGENT_TOOLSDIRECTORY) {
    $DotNetInstallDir = "$env:AGENT_TOOLSDIRECTORY/dotnet"
} else {
    $DotNetInstallDir = Join-Path $HOME .dotnet
}

Write-Host "Installing .NET Core SDK and runtimes to $DotNetInstallDir" -ForegroundColor Blue

if ($DotNetInstallDir) {
    $switches += '-InstallDir',$DotNetInstallDir
    $envVars['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    $envVars['DOTNET_ROOT'] = $DotNetInstallDir
}

if ($IsMacOS -or $IsLinux) {
    $DownloadUri = "https://dot.net/v1/dotnet-install.sh"
    $DotNetInstallScriptPath = "$DotNetInstallScriptRoot/dotnet-install.sh"
} else {
    $DownloadUri = "https://dot.net/v1/dotnet-install.ps1"
    $DotNetInstallScriptPath = "$DotNetInstallScriptRoot/dotnet-install.ps1"
}

if (-not (Test-Path $DotNetInstallScriptPath)) {
    Invoke-WebRequest -Uri $DownloadUri -OutFile $DotNetInstallScriptPath -UseBasicParsing
    if ($IsMacOS -or $IsLinux) {
        chmod +x $DotNetInstallScriptPath
    }
}

if ($PSCmdlet.ShouldProcess(".NET Core SDK $sdkVersion", "Install")) {
    Invoke-Expression -Command "$DotNetInstallScriptPath -Version $sdkVersion $switches"
} else {
    Invoke-Expression -Command "$DotNetInstallScriptPath -Version $sdkVersion $switches -DryRun"
}

$switches += '-Runtime','dotnet'

$runtimeVersions | Get-Unique |% {
    if ($PSCmdlet.ShouldProcess(".NET Core runtime $_", "Install")) {
        Invoke-Expression -Command "$DotNetInstallScriptPath -Channel $_ $switches"
    } else {
        Invoke-Expression -Command "$DotNetInstallScriptPath -Channel $_ $switches -DryRun"
    }
}

if ($PSCmdlet.ShouldProcess("Set DOTNET environment variables to discover these installed runtimes?")) {
    & "$PSScriptRoot/../azure-pipelines/Set-EnvVars.ps1" -Variables $envVars -PrependPath $DotNetInstallDir | Out-Null
}
