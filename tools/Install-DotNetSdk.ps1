<#
.SYNOPSIS
Installs the .NET SDK specified in the global.json file at the root of this repository,
along with supporting .NET Core runtimes used for testing.
.DESCRIPTION
This does not require elevation, as the SDK and runtimes are installed locally to this repo location.
.PARAMETER InstallLocality
A value indicating whether dependencies should be installed locally to the repo or at a per-user location.
Per-user allows sharing the installed dependencies across repositories and allows use of a shared expanded package cache.
Per-repo allows for high isolation, allowing for a more precise recreation of the environment within an Azure Pipelines build.
When using 'repo', environment variables are set to cause the locally installed dotnet SDK to be used.
Per-repo can lead to file locking issues when dotnet.exe is left running as a build server and can be mitigated by running `dotnet build-server shutdown`.
#>
[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='Medium')]
Param (
    [ValidateSet('repo','user')]
    [string]$InstallLocality='user'
)

$DotNetInstallScriptRoot = "$PSScriptRoot/../obj"
if (!(Test-Path $DotNetInstallScriptRoot)) { mkdir $DotNetInstallScriptRoot | Out-Null }

$switches = @(
    '-Architecture','x64'
)
$envVars = @{
    # For locally installed dotnet, skip first time experience which takes a long time
    'DOTNET_SKIP_FIRST_TIME_EXPERIENCE' = 'true';
}

$DotNetInstallScriptRoot = Resolve-Path $DotNetInstallScriptRoot
if ($InstallLocality -eq 'repo') {
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
$sdkVersion = & "$PSScriptRoot/../azure-pipelines/variables/DotNetSdkVersion.ps1"

if ($IsMacOS -or $IsLinux) {
    $DownloadUri = "https://dot.net/v1/dotnet-install.sh"
    $DotNetInstallScriptPath = "$DotNetInstallScriptRoot/dotnet-install.sh"
} else {
    $DownloadUri = "https://dot.net/v1/dotnet-install.ps1"
    $DotNetInstallScriptPath = "$DotNetInstallScriptRoot/dotnet-install.ps1"
}

if (-not (Test-Path $DotNetInstallScriptPath)) {
    Invoke-WebRequest -Uri $DownloadUri -OutFile $DotNetInstallScriptPath
    if ($IsMacOS -or $IsLinux) {
        chmod +x $DotNetInstallScriptPath
    }
}

if ($PSCmdlet.ShouldProcess(".NET Core SDK $sdkVersion", "Install")) {
    Invoke-Expression -Command "$DotNetInstallScriptPath -Version $sdkVersion $switches"
} else {
    Invoke-Expression -Command "$DotNetInstallScriptPath -Version $sdkVersion $switches -DryRun"
}

# Search for all .NET Core runtime versions referenced from MSBuild projects and arrange to install them.
$runtimeVersions = @()
Get-ChildItem "$PSScriptRoot\..\src\*.*proj" -Recurse |% {
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

$switches += '-Runtime','dotnet'

$runtimeVersions | Get-Unique |% {
    if ($PSCmdlet.ShouldProcess(".NET Core runtime $_", "Install")) {
        Invoke-Expression -Command "$DotNetInstallScriptPath -Channel $_ $switches"
    } else {
        Invoke-Expression -Command "$DotNetInstallScriptPath -Channel $_ $switches -DryRun"
    }
}

if ($PSCmdlet.ShouldProcess("Set DOTNET environment variables to discover these installed runtimes?")) {
    if ($env:TF_BUILD) {
        Write-Host "Azure Pipelines detected. Logging commands will be used to propagate environment variables and prepend path."
    }

    if ($IsMacOS -or $IsLinux) {
        $envVars['PATH'] = "${DotNetInstallDir}:$env:PATH"
    } else {
        $envVars['PATH'] = "$DotNetInstallDir;$env:PATH"
    }

    $envVars.GetEnumerator() |% {
        Set-Item -Path env:$($_.Key) -Value $_.Value

        # If we're running in Azure Pipelines, set these environment variables
        if ($env:TF_BUILD) {
            Write-Host "##vso[task.setvariable variable=$($_.Key);]$($_.Value)"
        }
    }

    if ($env:TF_BUILD) {
        Write-Host "##vso[task.prependpath]$DotNetInstallDir"
    }
}

if ($env:PS1UnderCmd -eq '1') {
    Write-Warning "Environment variable changes will be lost because you're running under cmd.exe. Run these commands manually:"
    $envVars.GetEnumerator() |% {
        if ($_.Key -eq 'PATH') {
            # Special case this one for readability
            Write-Host "SET PATH=$DotNetInstallDir;%PATH%"
        } else {
            Write-Host "SET $($_.Key)=$($_.Value)"
        }
    }
} else {
    Write-Host "Environment variables set:" -ForegroundColor Blue
    $envVars
}
