<#
.SYNOPSIS
Installs the .NET SDK specified in the global.json file at the root of this repository,
along with supporting .NET Core runtimes used for testing.
#>
[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='Medium')]
Param (
)

$DotNetInstallScriptRoot = Resolve-Path $PSScriptRoot\..\obj
$sdkVersion = & "$PSScriptRoot\..\azure-pipelines\variables\DotNetSdkVersion.ps1"

if ($IsMacOS -or $IsLinux) {
    $DownloadUri = "https://dot.net/v1/dotnet-install.sh"
    $DotNetInstallScriptPath = "$DotNetInstallScriptRoot/dotnet-install.sh"
} else {
    $DownloadUri = "https://dot.net/v1/dotnet-install.ps1"
    $DotNetInstallScriptPath = "$DotNetInstallScriptRoot\dotnet-install.ps1"
}

if (-not (Test-Path $DotNetInstallScriptPath)) {
    Invoke-WebRequest -Uri $DownloadUri -OutFile $DotNetInstallScriptPath
    chmod +x $DotNetInstallScriptPath
}

if ($PSCmdlet.ShouldProcess(".NET Core SDK $sdkVersion", "Install")) {
    & $DotNetInstallScriptPath -Version $sdkVersion -Architecture x64
} else {
    & $DotNetInstallScriptPath -Version $sdkVersion -Architecture x64 -DryRun
}

# Search for all .NET Core runtime versions referenced from MSBuild projects and arrange to install them.
$runtimeVersions = @()
Get-ChildItem "$PSScriptRoot\..\src\*.*proj" -Recurse |% {
    $projXml = [xml](Get-Content -Path $_)
    $targetFrameworks = $projXml.Project.PropertyGroup.TargetFramework
    if (!$targetFrameworks) {
        $targetFrameworks = $projXml.Project.PropertyGroup.TargetFrameworks
        if ($targetFrameworks) {
            $targetFrameworks = $targetFrameworks.Split(';')
        }
    }
    $targetFrameworks |? { $_ -match 'netcoreapp(\d+\.\d+)' } |% {
        $runtimeVersions += $Matches[1]
    }
}

$runtimeVersions | Get-Unique |% {
    if ($PSCmdlet.ShouldProcess(".NET Core runtime $_", "Install")) {
        & $DotNetInstallScriptPath -Channel $_ -Runtime dotnet -Architecture x64
    } else {
        & $DotNetInstallScriptPath -Channel $_ -Runtime dotnet -Architecture x64 -DryRun
    }
}
