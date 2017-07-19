[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$restore = $false,
    [switch]$release = $false,
    [switch]$official = $false,
    [string]$msbuildDir = "",
    [switch]$cibuild = $false,
    [string]$branchName = "master",
    [string]$assemblyVersion = "42.42.42.4242",
    [switch]$testDesktop = $false,
    [switch]$publish = $false,
    [switch]$help = $false,

    # Credentials
    [string]$myGetApiKey = "",
    [string]$nugetApiKey = "",
    [string]$gitHubUserName = "",
    [string]$gitHubToken = "",
    [string]$gitHubEmail = "",
    [parameter(ValueFromRemainingArguments=$true)] $badArgs)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
    Write-Host "Usage: build.ps1"
    Write-Host "  -release                  Perform release build (default is debug)"
    Write-Host "  -restore                  Restore packages"
    Write-Host "  -official                 Perform an official build"
    Write-Host "  -msbuildDir               MSBuild to use for operations"
    Write-Host "  -cibuild                  Run CI specific operations"
    Write-Host "  -testDesktop              Run unit tests"
    Write-Host "  -publish                  Run the pubish step"
    Write-Host "  -branchName               Branch being built"
    Write-Host "  -nugetApiKey              Key for NuGet publishing"
    Write-Host "  -help                     Print this message"
}

function Run-MSBuild([string]$buildArgs = "", [string]$logFile = "", [switch]$parallel = $true) {
    $args = "/nologo /nodeReuse:false /consoleloggerparameters:Verbosity=minimal /p:DeployExtension=false /p:Configuration=$config";

    if ($parallel) { 
        $args += " /m"
    }

    if ($official) {
        $args += " /p:OfficialBuild=true"
    }
    
    if ($logFile -ne "") {
        $args += " /filelogger /fileloggerparameters:Verbosity=normal;logFile=$logFile";
    }

    if ($release) { 
        $args += " /p:Configuration=Release"
    }

    $args += " $buildArgs"
    Exec-Console $msbuild $args
}

function Build-InsertionItems() { 
    Push-Location $setupDir
    try { 
        Create-PerfTests
        Exec-Command (Join-Path $configDir "Exes\DevDivInsertionFiles\Roslyn.BuildDevDivInsertionFiles.exe") "$configDir $setupDir $(Get-PackagesDir) `"$assemblyVersion`"" | Out-Host
        
        # In non-official builds need to supply values for a few MSBuild properties. The actual value doesn't
        # matter, just that it's provided some value.
        $extraArgs = ""
        if (-not $official) { 
            $extraArgs = " /p:FinalizeValidate=false /p:ManifestPublishUrl=https://vsdrop.corp.microsoft.com/file/v1/Products/DevDiv/dotnet/roslyn/master/20160729.6"
        }

        Run-MSBuild "DevDivPackages\Roslyn.proj"
        Run-MSBuild "DevDivVsix\PortableFacades\PortableFacades.vsmanproj $extraArgs"
        Run-MSBuild "DevDivVsix\CompilersPackage\Microsoft.CodeAnalysis.Compilers.vsmanproj $extraArgs"
        Run-MSBuild "DevDivVsix\MicrosoftCodeAnalysisLanguageServices\Microsoft.CodeAnalysis.LanguageServices.vsmanproj $extraArgs"
        Run-MSBuild "..\Dependencies\Microsoft.NetFX20\Microsoft.NetFX20.nuget.proj"
    }
    finally {
        Pop-Location
    }
}

# Create the PerfTests directory under Binaries\$(Configuration).  There are still a number
# of tools (in roslyn and roslyn-internal) that depend on this combined directory.
function Create-PerfTests() {
    $target = Join-Path $configDir "PerfTests"
    Write-Host "PerfTests: $target"
    Create-Directory $target

    Push-Location $configDir
    foreach ($subDir in @("Dlls", "UnitTests")) {
        Push-Location $subDir
        foreach ($path in Get-ChildItem -re -in "PerfTests") {
            Write-Host "`tcopying $path"
            Copy-Item -force -recurse "$path\*" $target
        }
        Pop-Location
    }
    Pop-Location
}

Push-Location $PSScriptRoot
try {
    . (Join-Path $PSScriptRoot "..\..\..\build\scripts\build-utils.ps1")
    if ($badArgs -ne $null) {
        Write-Host "Unsupported argument $badArgs"
        Print-Usage
        exit 1
    }

    if ($help) { 
        Print-Usage
        exit 1
    }

    # On Jenkins runs we deliberately run microbuild with a clean NuGet cache. This means at least 
    # one job runs with a clean cache and assures all packages we depend on are restored during 
    # the restore phase. As opposed to getting lucky based on a NuGet being available in the cache.
    if ($cibuild) {
        $nuget = Ensure-NuGet
        Exec-Block { & $nuget locals all -clear } | Out-Host
    }

    $msbuild, $msbuildDir = Ensure-MSBuildAndDir -msbuildDir $msbuildDir
    $scriptDir = Join-Path $repoDir "build\scripts"
    $config = if ($release) { "Release" } else { "Debug" }
    $configDir = Join-Path $binariesDir $config
    $setupDir = Join-Path $repoDir "src\Setup"

    Exec-Block { & (Join-Path $scriptDir "build.ps1") -restore:$restore -buildAll -official:$official -msbuildDir $msbuildDir -release:$release -sign -pack -testDesktop:$testDesktop }

    Exec-Block { & (Join-Path $PSScriptRoot "run-gitlink.ps1") -config $config }
    Build-InsertionItems
    Exec-Block { & (Join-Path $scriptDir "check-toolset-insertion.ps1") -sourcePath $repoDir -binariesPath $configDir }

    # Insertion scripts currently look for a sentinel file on the drop share to determine that the build was green
    # and ready to be inserted 
    $sentinelFile = Join-Path $configDir AllTestsPassed.sentinel
    New-Item -Force $sentinelFile -type file

    Get-Process vbcscompiler -ErrorAction SilentlyContinue | Stop-Process

    if ($publish) { 
        Exec-Block { & .\publish-assets.ps1 -configDir $configDir -branchName $branchName -mygetApiKey $mygetApiKey -nugetApiKey $nugetApiKey -gitHubUserName $githubUserName -gitHubToken $gitHubToken -gitHubEmail $gitHubEmail -test:$(-not $official) }
    }

    Exec-Block { & .\copy-insertion-items.ps1 -binariesPath $configDir -test:$(-not $official) }

    exit 0
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
finally {
    Pop-Location
    if ($cibuild) {
        Get-Process msbuild -ErrorAction SilentlyContinue | Stop-Process
        Get-Process vbcscompiler -ErrorAction SilentlyContinue | Stop-Process
    }
}
