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


# Create the Insertion folder. This is where the insertion tool pulls all of its 
# binaries from. 
function Copy-InsertionItems() {
    $insertionDir = Join-Path $binariesdir "Insertion"
    Create-Directory $insertionDir

    $items = @(
        "Vsix\ExpressionEvaluatorPackage\Microsoft.CodeAnalysis.ExpressionEvaluator.json",
        "Vsix\ExpressionEvaluatorPackage\ExpressionEvaluatorPackage.vsix",
        "Vsix\VisualStudioInteractiveComponents\Microsoft.CodeAnalysis.VisualStudio.InteractiveComponents.json",
        "Vsix\VisualStudioInteractiveComponents\Roslyn.VisualStudio.InteractiveComponents.vsix",
        "Vsix\VisualStudioSetup\Microsoft.CodeAnalysis.VisualStudio.Setup.json",
        "Vsix\VisualStudioSetup\Roslyn.VisualStudio.Setup.vsix",
        "Vsix\VisualStudioSetup.Next\Microsoft.CodeAnalysis.VisualStudio.Setup.Next.json",
        "Vsix\VisualStudioSetup.Next\Roslyn.VisualStudio.Setup.Next.vsix",
        "Vsix\CodeAnalysisLanguageServices\Microsoft.CodeAnalysis.LanguageServices.vsman",
        "Vsix\PortableFacades\PortableFacades.vsix",
        "Vsix\PortableFacades\PortableFacades.vsman",
        "Vsix\PortableFacades\PortableFacades.vsmand",
        "Vsix\PortableFacades\PortableFacades.json",
        "Vsix\CodeAnalysisCompilers\Microsoft.CodeAnalysis.Compilers.vsix",
        "Vsix\CodeAnalysisCompilers\Microsoft.CodeAnalysis.Compilers.vsman",
        "Vsix\CodeAnalysisCompilers\Microsoft.CodeAnalysis.Compilers.vsmand",
        "Vsix\CodeAnalysisCompilers\Microsoft.CodeAnalysis.Compilers.json")


    foreach ($item in $items) { 
        $itemPath = Join-Path $configDir $item
        Copy-Item $itemPath $insertionDir
    }

    Copy-Item (Join-Path $binariesDir "DevDivPackages\Roslyn\*.nupkg") $insertionDir
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

    Exec-Block { & (Join-Path $scriptDir "build.ps1") -restore:$restore -buildAll -cibuild:$cibuild -official:$official -msbuildDir $msbuildDir -release:$release -sign -pack -testDesktop:$testDesktop -assemblyVersion:$assemblyVersion }
    Exec-Block { & (Join-Path $scriptDir "check-toolset-insertion.ps1") -sourcePath $repoDir -binariesPath $configDir }
    Copy-InsertionItems

    # Insertion scripts currently look for a sentinel file on the drop share to determine that the build was green
    # and ready to be inserted 
    $sentinelFile = Join-Path $configDir AllTestsPassed.sentinel
    New-Item -Force $sentinelFile -type file

    Get-Process vbcscompiler -ErrorAction SilentlyContinue | Stop-Process

    if ($publish) { 
        Exec-Block { & .\publish-assets.ps1 -configDir $configDir -branchName $branchName -mygetApiKey $mygetApiKey -nugetApiKey $nugetApiKey -gitHubUserName $githubUserName -gitHubToken $gitHubToken -gitHubEmail $gitHubEmail -test:$(-not $official) }
    }

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
