#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Installs the .NET SDK specified in the global.json file at the root of this repository,
    along with supporting .NET runtimes used for testing.
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
.PARAMETER SdkOnly
    Skips installing the runtime.
.PARAMETER IncludeX86
    Installs a x86 SDK and runtimes in addition to the x64 ones. Only supported on Windows. Ignored on others.
.PARAMETER IncludeAspNetCore
    Installs the ASP.NET Core runtime along with the .NET runtime.
#>
[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='Medium')]
Param (
    [ValidateSet('repo','user','machine')]
    [string]$InstallLocality='user',
    [switch]$SdkOnly,
    [switch]$IncludeX86,
    [switch]$IncludeAspNetCore
)

$DotNetInstallScriptRoot = "$PSScriptRoot/../obj/tools"
if (!(Test-Path $DotNetInstallScriptRoot)) { New-Item -ItemType Directory -Path $DotNetInstallScriptRoot -WhatIf:$false | Out-Null }
$DotNetInstallScriptRoot = Resolve-Path $DotNetInstallScriptRoot

# Look up actual required .NET SDK version from global.json
$sdkVersion = & "$PSScriptRoot/../azure-pipelines/variables/DotNetSdkVersion.ps1"

If ($IncludeX86 -and ($IsMacOS -or $IsLinux)) {
    Write-Verbose "Ignoring -IncludeX86 switch because 32-bit runtimes are only supported on Windows."
    $IncludeX86 = $false
}

$arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
if (!$arch) { # Windows Powershell leaves this blank
    $arch = 'x64'
    if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { $arch = 'ARM64' }
    if (${env:ProgramFiles(Arm)}) { $arch = 'ARM64' }
}

# Search for all .NET runtime versions referenced from MSBuild projects and arrange to install them.
$runtimeVersions = @()
$windowsDesktopRuntimeVersions = @()
$aspnetRuntimeVersions = @()
if (!$SdkOnly) {
    Get-ChildItem "$PSScriptRoot\..\src\*.*proj","$PSScriptRoot\..\test\*.*proj","$PSScriptRoot\..\Directory.Build.props" -Recurse |% {
        $projXml = [xml](Get-Content -Path $_)
        $pg = $projXml.Project.PropertyGroup
        if ($pg) {
            $targetFrameworks = @()
            $tf = $pg.TargetFramework
            $targetFrameworks += $tf
            $tfs = $pg.TargetFrameworks
            if ($tfs) {
                $targetFrameworks = $tfs -Split ';'
            }
        }
        $targetFrameworks |? { $_ -match 'net(?:coreapp)?(\d+\.\d+)' } |% {
            $v = $Matches[1]
            $runtimeVersions += $v
            $aspnetRuntimeVersions += $v
            if ($v -ge '3.0' -and -not ($IsMacOS -or $IsLinux)) {
                $windowsDesktopRuntimeVersions += $v
            }
        }

        # Add target frameworks of the form: netXX
        $targetFrameworks |? { $_ -match 'net(\d+\.\d+)' } |% {
            $v = $Matches[1]
            $runtimeVersions += $v
            $aspnetRuntimeVersions += $v
            if (-not ($IsMacOS -or $IsLinux)) {
                $windowsDesktopRuntimeVersions += $v
            }
        }
    }
}

if (!$IncludeAspNetCore) {
    $aspnetRuntimeVersions = @()
}

Function Get-FileFromWeb([Uri]$Uri, $OutDir) {
    $OutFile = Join-Path $OutDir $Uri.Segments[-1]
    if (!(Test-Path $OutFile)) {
        Write-Verbose "Downloading $Uri..."
        if (!(Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }
        try {
            (New-Object System.Net.WebClient).DownloadFile($Uri, $OutFile)
        } finally {
            # This try/finally causes the script to abort
        }
    }

    $OutFile
}

Function Get-InstallerExe(
    $Version,
    $Architecture,
    [ValidateSet('Sdk','Runtime','WindowsDesktop')]
    [string]$sku
) {
    # Get the latest/actual version for the specified one
    $TypedVersion = $null
    if (![Version]::TryParse($Version, [ref] $TypedVersion)) {
        Write-Error "Unable to parse $Version into an a.b.c.d version. This version cannot be installed machine-wide."
        exit 1
    }

    if ($TypedVersion.Build -eq -1) {
        $versionInfo = -Split (Invoke-WebRequest -Uri "https://dotnetcli.blob.core.windows.net/dotnet/$sku/$Version/latest.version" -UseBasicParsing)
        $Version = $versionInfo[-1]
    }

    $majorMinor = "$($TypedVersion.Major).$($TypedVersion.Minor)"
    $ReleasesFile = Join-Path $DotNetInstallScriptRoot "$majorMinor\releases.json"
    if (!(Test-Path $ReleasesFile)) {
        Get-FileFromWeb -Uri "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/$majorMinor/releases.json" -OutDir (Split-Path $ReleasesFile) | Out-Null
    }

    $releases = Get-Content $ReleasesFile | ConvertFrom-Json
    $url = $null
    foreach ($release in $releases.releases) {
        $filesElement = $null
        if ($release.$sku.version -eq $Version) {
            $filesElement = $release.$sku.files
        }
        if (!$filesElement -and ($sku -eq 'sdk') -and $release.sdks) {
            foreach ($sdk in $release.sdks) {
                if ($sdk.version -eq $Version) {
                    $filesElement = $sdk.files
                    break
                }
            }
        }

        if ($filesElement) {
            foreach ($file in $filesElement) {
                if ($file.rid -eq "win-$Architecture") {
                    $url = $file.url
                    Break
                }
            }

            if ($url) {
                Break
            }
        }
    }

    if ($url) {
        Get-FileFromWeb -Uri $url -OutDir $DotNetInstallScriptRoot
    } else {
        throw "Unable to find release of $sku v$Version"
    }
}

Function Install-DotNet($Version, $Architecture, [ValidateSet('Sdk','Runtime','WindowsDesktop','AspNetCore')][string]$sku = 'Sdk') {
    Write-Host "Downloading .NET $sku $Version..."
    $Installer = Get-InstallerExe -Version $Version -Architecture $Architecture -sku $sku
    Write-Host "Installing .NET $sku $Version..."
    cmd /c start /wait $Installer /install /passive /norestart
    if ($LASTEXITCODE -eq 3010) {
        Write-Verbose "Restart required"
    } elseif ($LASTEXITCODE -ne 0) {
        throw "Failure to install .NET SDK"
    }
}

$switches = @()
$envVars = @{
    # For locally installed dotnet, skip first time experience which takes a long time
    'DOTNET_SKIP_FIRST_TIME_EXPERIENCE' = 'true';
}

if ($InstallLocality -eq 'machine') {
    if ($IsMacOS -or $IsLinux) {
        $DotNetInstallDir = '/usr/share/dotnet'
    } else {
        $restartRequired = $false
        if ($PSCmdlet.ShouldProcess(".NET SDK $sdkVersion", "Install")) {
            Install-DotNet -Version $sdkVersion -Architecture $arch
            $restartRequired = $restartRequired -or ($LASTEXITCODE -eq 3010)

            if ($IncludeX86) {
                Install-DotNet -Version $sdkVersion -Architecture x86
                $restartRequired = $restartRequired -or ($LASTEXITCODE -eq 3010)
            }
        }

        $runtimeVersions | Sort-Object | Get-Unique |% {
            if ($PSCmdlet.ShouldProcess(".NET runtime $_", "Install")) {
                Install-DotNet -Version $_ -sku Runtime -Architecture $arch
                $restartRequired = $restartRequired -or ($LASTEXITCODE -eq 3010)

                if ($IncludeX86) {
                    Install-DotNet -Version $_ -sku Runtime -Architecture x86
                    $restartRequired = $restartRequired -or ($LASTEXITCODE -eq 3010)
                }
            }
        }

        $windowsDesktopRuntimeVersions | Sort-Object | Get-Unique |% {
            if ($PSCmdlet.ShouldProcess(".NET Windows Desktop $_", "Install")) {
                Install-DotNet -Version $_ -sku WindowsDesktop -Architecture $arch
                $restartRequired = $restartRequired -or ($LASTEXITCODE -eq 3010)

                if ($IncludeX86) {
                    Install-DotNet -Version $_ -sku WindowsDesktop -Architecture x86
                    $restartRequired = $restartRequired -or ($LASTEXITCODE -eq 3010)
                }
            }
        }

        $aspnetRuntimeVersions | Sort-Object | Get-Unique |% {
            if ($PSCmdlet.ShouldProcess("ASP.NET Core $_", "Install")) {
                Install-DotNet -Version $_ -sku AspNetCore -Architecture $arch
                $restartRequired = $restartRequired -or ($LASTEXITCODE -eq 3010)

                if ($IncludeX86) {
                    Install-DotNet -Version $_ -sku AspNetCore -Architecture x86
                    $restartRequired = $restartRequired -or ($LASTEXITCODE -eq 3010)
                }
            }
        }
        if ($restartRequired) {
            Write-Host -ForegroundColor Yellow "System restart required"
            Exit 3010
        }

        return
    }
} elseif ($InstallLocality -eq 'repo') {
    $DotNetInstallDir = "$DotNetInstallScriptRoot/.dotnet"
    $DotNetX86InstallDir = "$DotNetInstallScriptRoot/x86/.dotnet"
} elseif ($env:AGENT_TOOLSDIRECTORY) {
    $DotNetInstallDir = "$env:AGENT_TOOLSDIRECTORY/dotnet"
    $DotNetX86InstallDir = "$env:AGENT_TOOLSDIRECTORY/x86/dotnet"
} else {
    $DotNetInstallDir = Join-Path $HOME .dotnet
}

if ($DotNetInstallDir) {
    if (!(Test-Path $DotNetInstallDir)) { New-Item -ItemType Directory -Path $DotNetInstallDir }
    $DotNetInstallDir = Resolve-Path $DotNetInstallDir
    Write-Host "Installing .NET SDK and runtimes to $DotNetInstallDir" -ForegroundColor Blue
    $envVars['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    $envVars['DOTNET_ROOT'] = $DotNetInstallDir
}

if ($IncludeX86) {
    if ($DotNetX86InstallDir) {
        if (!(Test-Path $DotNetX86InstallDir)) { New-Item -ItemType Directory -Path $DotNetX86InstallDir }
        $DotNetX86InstallDir = Resolve-Path $DotNetX86InstallDir
        Write-Host "Installing x86 .NET SDK and runtimes to $DotNetX86InstallDir" -ForegroundColor Blue
    } else {
        # Only machine-wide or repo-wide installations can handle two unique dotnet.exe architectures.
        Write-Error "The installation location or OS isn't supported for x86 installation. Try a different -InstallLocality value."
        return 1
    }
}

if ($IsMacOS -or $IsLinux) {
    $DownloadUri = "https://raw.githubusercontent.com/dotnet/install-scripts/0b09de9bc136cacb5f849a6957ebd4062173c148/src/dotnet-install.sh"
    $DotNetInstallScriptPath = "$DotNetInstallScriptRoot/dotnet-install.sh"
} else {
    $DownloadUri = "https://raw.githubusercontent.com/dotnet/install-scripts/0b09de9bc136cacb5f849a6957ebd4062173c148/src/dotnet-install.ps1"
    $DotNetInstallScriptPath = "$DotNetInstallScriptRoot/dotnet-install.ps1"
}

if (-not (Test-Path $DotNetInstallScriptPath)) {
    Invoke-WebRequest -Uri $DownloadUri -OutFile $DotNetInstallScriptPath -UseBasicParsing
    if ($IsMacOS -or $IsLinux) {
        chmod +x $DotNetInstallScriptPath
    }
}

# In case the script we invoke is in a directory with spaces, wrap it with single quotes.
# In case the path includes single quotes, escape them.
$DotNetInstallScriptPathExpression = $DotNetInstallScriptPath.Replace("'", "''")
$DotNetInstallScriptPathExpression = "& '$DotNetInstallScriptPathExpression'"

$anythingInstalled = $false
$global:LASTEXITCODE = 0

if ($PSCmdlet.ShouldProcess(".NET SDK $sdkVersion", "Install")) {
    $anythingInstalled = $true
    Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Version $sdkVersion -Architecture $arch -InstallDir $DotNetInstallDir $switches"

    if ($LASTEXITCODE -ne 0) {
        Write-Error ".NET SDK installation failure: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
} else {
    Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Version $sdkVersion -Architecture $arch -InstallDir $DotNetInstallDir $switches -DryRun"
}

if ($IncludeX86) {
    if ($PSCmdlet.ShouldProcess(".NET x86 SDK $sdkVersion", "Install")) {
        $anythingInstalled = $true
        Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Version $sdkVersion -Architecture x86 -InstallDir $DotNetX86InstallDir $switches"

        if ($LASTEXITCODE -ne 0) {
            Write-Error ".NET x86 SDK installation failure: $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    } else {
        Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Version $sdkVersion -Architecture x86 -InstallDir $DotNetX86InstallDir $switches -DryRun"
    }
}

$dotnetRuntimeSwitches = $switches + '-Runtime','dotnet'

$runtimeVersions | Sort-Object -Unique |% {
    if ($PSCmdlet.ShouldProcess(".NET $Arch runtime $_", "Install")) {
        $anythingInstalled = $true
        Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture $arch -InstallDir $DotNetInstallDir $dotnetRuntimeSwitches"

        if ($LASTEXITCODE -ne 0) {
            Write-Error ".NET SDK installation failure: $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    } else {
        Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture $arch -InstallDir $DotNetInstallDir $dotnetRuntimeSwitches -DryRun"
    }

    if ($IncludeX86) {
        if ($PSCmdlet.ShouldProcess(".NET x86 runtime $_", "Install")) {
            $anythingInstalled = $true
            Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture x86 -InstallDir $DotNetX86InstallDir $dotnetRuntimeSwitches"

            if ($LASTEXITCODE -ne 0) {
                Write-Error ".NET SDK installation failure: $LASTEXITCODE"
                exit $LASTEXITCODE
            }
        } else {
            Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture x86 -InstallDir $DotNetX86InstallDir $dotnetRuntimeSwitches -DryRun"
        }
    }
}

$windowsDesktopRuntimeSwitches = $switches + '-Runtime','windowsdesktop'

$windowsDesktopRuntimeVersions | Sort-Object -Unique |% {
    if ($PSCmdlet.ShouldProcess(".NET WindowsDesktop $arch runtime $_", "Install")) {
        $anythingInstalled = $true
        Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture $arch -InstallDir $DotNetInstallDir $windowsDesktopRuntimeSwitches"

        if ($LASTEXITCODE -ne 0) {
            Write-Error ".NET SDK installation failure: $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    } else {
        Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture $arch -InstallDir $DotNetInstallDir $windowsDesktopRuntimeSwitches -DryRun"
    }

    if ($IncludeX86) {
        if ($PSCmdlet.ShouldProcess(".NET WindowsDesktop x86 runtime $_", "Install")) {
            $anythingInstalled = $true
            Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture x86 -InstallDir $DotNetX86InstallDir $windowsDesktopRuntimeSwitches"

            if ($LASTEXITCODE -ne 0) {
                Write-Error ".NET SDK installation failure: $LASTEXITCODE"
                exit $LASTEXITCODE
            }
        } else {
            Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture x86 -InstallDir $DotNetX86InstallDir $windowsDesktopRuntimeSwitches -DryRun"
        }
    }
}

$aspnetRuntimeSwitches = $switches + '-Runtime','aspnetcore'

$aspnetRuntimeVersions | Sort-Object -Unique |% {
    if ($PSCmdlet.ShouldProcess(".NET ASP.NET Core $arch runtime $_", "Install")) {
        $anythingInstalled = $true
        Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture $arch -InstallDir $DotNetInstallDir $aspnetRuntimeSwitches"

        if ($LASTEXITCODE -ne 0) {
            Write-Error ".NET SDK installation failure: $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    } else {
        Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture $arch -InstallDir $DotNetInstallDir $aspnetRuntimeSwitches -DryRun"
    }

    if ($IncludeX86) {
        if ($PSCmdlet.ShouldProcess(".NET ASP.NET Core x86 runtime $_", "Install")) {
            $anythingInstalled = $true
            Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture x86 -InstallDir $DotNetX86InstallDir $aspnetRuntimeSwitches"

            if ($LASTEXITCODE -ne 0) {
                Write-Error ".NET SDK installation failure: $LASTEXITCODE"
                exit $LASTEXITCODE
            }
        } else {
            Invoke-Expression -Command "$DotNetInstallScriptPathExpression -Channel $_ -Architecture x86 -InstallDir $DotNetX86InstallDir $aspnetRuntimeSwitches -DryRun"
        }
    }
}

if ($PSCmdlet.ShouldProcess("Set DOTNET environment variables to discover these installed runtimes?")) {
    & "$PSScriptRoot/Set-EnvVars.ps1" -Variables $envVars -PrependPath $DotNetInstallDir | Out-Null
}

if ($anythingInstalled -and ($InstallLocality -ne 'machine') -and !$env:TF_BUILD -and !$env:GITHUB_ACTIONS) {
    Write-Warning ".NET runtimes or SDKs were installed to a non-machine location. Perform your builds or open Visual Studio from this same environment in order for tools to discover the location of these dependencies."
}
