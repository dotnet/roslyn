[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$restore = $false,
    [switch]$release = $false,
    [switch]$official = $false,
    [switch]$cibuild = $false,
    [string]$branchName = "master",
    [switch]$testDesktop = $false,
    [string]$publishType = "",
    [switch]$help = $false,
    [string]$signType = "",

    # Credentials
    [string]$myGetApiKey = "",
    [string]$nugetApiKey = "",
    [string]$gitHubUserName = "",
    [string]$gitHubToken = "",
    [string]$gitHubEmail = "",
    [string]$blobFeedUrl = "",
    [string]$blobFeedKey = "",
    [parameter(ValueFromRemainingArguments=$true)] $badArgs)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
    Write-Host "Usage: build.ps1"
    Write-Host "  -release                  Perform release build (default is debug)"
    Write-Host "  -restore                  Restore packages"
    Write-Host "  -official                 Perform an official build"
    Write-Host "  -cibuild                  Run CI specific operations"
    Write-Host "  -testDesktop              Run unit tests"
    Write-Host "  -publishType              Publish to run: vsts, blob or none (default is none)"
    Write-Host "  -branchName               Branch being built"
    Write-Host "  -nugetApiKey              Key for NuGet publishing"
    Write-Host "  -signType                 Signing type: real, test or public (default is public)"
    Write-Host "  -help                     Print this message"
}

# Create the Insertion folder. This is where the insertion tool pulls all of its 
# binaries from. 
function Copy-InsertionItems() {
    $insertionDir = Join-Path $configDir "Insertion"
    Create-Directory $insertionDir

    $items = @(
        "Vsix\ExpressionEvaluatorPackage\Microsoft.CodeAnalysis.ExpressionEvaluator.json",
        "Vsix\ExpressionEvaluatorPackage\ExpressionEvaluatorPackage.vsix",
        "Vsix\Roslyn.VisualStudio.InteractiveComponents\Microsoft.CodeAnalysis.VisualStudio.InteractiveComponents.json",
        "Vsix\Roslyn.VisualStudio.InteractiveComponents\Roslyn.VisualStudio.InteractiveComponents.vsix",
        "Vsix\Roslyn.VisualStudio.Setup\Microsoft.CodeAnalysis.VisualStudio.Setup.json",
        "Vsix\Roslyn.VisualStudio.Setup\Roslyn.VisualStudio.Setup.vsix",
        "Vsix\CodeAnalysisLanguageServices\Microsoft.CodeAnalysis.LanguageServices.vsman",
        "Vsix\PortableFacades\PortableFacades.vsix",
        "Vsix\PortableFacades\PortableFacades.vsman",
        "Vsix\PortableFacades\PortableFacades.vsmand",
        "Vsix\PortableFacades\PortableFacades.json",
        "Vsix\CodeAnalysisCompilers\Microsoft.CodeAnalysis.Compilers.vsix",
        "Vsix\CodeAnalysisCompilers\Microsoft.CodeAnalysis.Compilers.vsman",
        "Vsix\CodeAnalysisCompilers\Microsoft.CodeAnalysis.Compilers.vsmand",
        "Vsix\CodeAnalysisCompilers\Microsoft.CodeAnalysis.Compilers.json")

    $vsLanguages = "chs", "cht", "csy", "fra", "deu", "ita", "jpn", "kor", "plk", "ptb", "rus", "esn", "trk"
    foreach ($language in $vsLanguages) {
        $items += "Vsix\CodeAnalysisCompilers\Microsoft.CodeAnalysis.Compilers.Resources.$($language).vsix"
        $items += "Vsix\CodeAnalysisCompilers\Microsoft.CodeAnalysis.Compilers.Resources.$($language).vsmand"
        $items += "Vsix\CodeAnalysisCompilers\Microsoft.CodeAnalysis.Compilers.Resources.$($language).json"
    }

    foreach ($item in $items) { 
        $itemPath = Join-Path $configDir $item
        Copy-Item $itemPath $insertionDir
    }

    Copy-Item (Join-Path $configDir "DevDivPackages\Roslyn\*.nupkg") $insertionDir
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
        Clear-PackageCache
    }

    $scriptDir = Join-Path $repoDir "build\scripts"
    $config = if ($release) { "Release" } else { "Debug" }
    $configDir = Join-Path $binariesDir $config
    $setupDir = Join-Path $repoDir "src\Setup"

    Exec-Block { & (Join-Path $scriptDir "build.ps1") -restore:$restore -build -cibuild:$cibuild -official:$official -release:$release -sign -signType $signType -pack -testDesktop:$testDesktop -binaryLog -procdump }
    Copy-InsertionItems

    # Insertion scripts currently look for a sentinel file on the drop share to determine that the build was green
    # and ready to be inserted 
    $sentinelFile = Join-Path $configDir AllTestsPassed.sentinel
    New-Item -Force $sentinelFile -type file

    Get-Process vbcscompiler -ErrorAction SilentlyContinue | Stop-Process

    switch ($publishType) {
        "vsts" {
            Exec-Block { & .\publish-assets.ps1 -configDir $configDir -branchName $branchName -mygetApiKey $mygetApiKey -nugetApiKey $nugetApiKey -gitHubUserName $githubUserName -gitHubToken $gitHubToken -gitHubEmail $gitHubEmail -test:$(-not $official) }
            break;
        }
        "blob" {
            # This is handled by the Build.proj file directly
            break;
        }
        "" {
            # Explicit don't publish
            break;
        }
        default {
            throw "Unexpected publish type: $publishType"
            break;
        }
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