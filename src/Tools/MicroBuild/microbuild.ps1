[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$restore = $false,
    [switch]$release = $false,
    [switch]$official = $false,
    [string]$msbuildDir = "",
    [switch]$cibuild = $false,
    [string]$branchName = "master",
    [string]$nugetApiKey = "",
    [switch]$testDesktop = $false,
    [switch]$publish = $false,
    [switch]$help = $false,
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
    $args = "/nologo /nodeReuse:false /consoleloggerparameters:Verbosity=minimal /p:DeployExtension=false";

    if ($parallel) { 
        $args += " /m"
    }

    if ($official) {
        $args += " /p:OfficialBuild=true"
    }
    
    if ($logFile -ne "") {
        $args += " /filelogger /fileloggerparameters:Verbosity=normal;logFile=$logFile";
    }

    $args += " $buildArgs"
    Exec-Command $msbuild $args
}

function Run-SignTool() { 
    Push-Location $repoDir
    try {
        $signTool = Join-Path (Get-PackageDir "RoslynTools.Microsoft.SignTool") "tools\SignTool.exe"
        $signToolArgs = "-msbuildPath $msbuild"
        if (-not $official) {
            $signToolArgs += " -test"
        }
        $signToolArgs += " `"$configDir`""
        Exec-Command $signTool $signToolArgs
    }
    finally { 
        Pop-Location
    }
}

# Not all of our artifacts needed for signing are included inside Roslyn.sln. Need to 
# finish building these before we can run signing.
function Build-ExtraSignArtifacts() { 

    Push-Location $setupDir
    try {
        # Publish the CoreClr projects (CscCore and VbcCore) and dependencies for later NuGet packaging.
        Run-MSBuild "..\Compilers\CSharp\CscCore\CscCore.csproj /t:PublishWithoutBuilding"
        Run-MSBuild "..\Compilers\VisualBasic\VbcCore\VbcCore.csproj /t:PublishWithoutBuilding"

        # No need to build references here as we just built the rest of the source tree. 
        # We build these serially to work around https://github.com/dotnet/roslyn/issues/11856,
        # where building multiple projects that produce VSIXes larger than 10MB will race against each other
        Run-MSBuild "Deployment\Current\Roslyn.Deployment.Full.csproj /p:BuildProjectReferences=false" -parallel:$false
        Run-MSBuild "Deployment\Next\Roslyn.Deployment.Full.Next.csproj /p:BuildProjectReferences=false" -parallel:$false

        $dest = @(
            $configDir,
            "Templates\CSharp\Diagnostic\Analyzer",
            "Templates\VisualBasic\Diagnostic\Analyzer\tools")
        foreach ($dir in $dest) { 
            Copy-Item "PowerShell\*.ps1" $dir
        }

        Run-MSBuild "Templates\Templates.sln"
    }
    finally {
        Pop-Location
    }
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

    Exec-Block { & (Join-Path $scriptDir "build.ps1") -restore:$restore -build -official:$official -msbuildDir $msbuildDir -release:$release }
    Exec-Block { & (Join-Path $scriptDir "create-perftests.ps1") -buildDir $configDir }
    Build-ExtraSignArtifacts
    Exec-Block { & (Join-Path $PSScriptRoot "run-gitlink.ps1") -config $config }
    Run-MSBuild (Join-Path $repoDir "src\NuGet\NuGet.proj")

    $buildArgs = Join-Path $repoDir "src\Setup\SetupStep2.proj"
    if (-not $official) { 
        $buildArgs += " /p:FinalizeValidate=false /p:ManifestPublishUrl=https://vsdrop.corp.microsoft.com/file/v1/Products/DevDiv/dotnet/roslyn/master/20160729.6"
    }
    Run-MSBuild $buildArgs

    # The desktop tests need to run after signing so that tests run against fully signed 
    # assemblies.
    if ($testDesktop) {
        Exec-Block { & (Join-Path $scriptDir "build.ps1") -testDesktop -test32 }
    }

    Exec-Block { & (Join-Path $scriptDir "check-toolset-insertion.ps1") -sourcePath $repoDir -binariesPath $configDir }

    # Insertion scripts currently look for a sentinel file on the drop share to determine that the build was green
    # and ready to be inserted 
    $sentinelFile = Join-Path $configDir AllTestsPassed.sentinel
    New-Item -Force $sentinelFile -type file

    Get-Process vbcscompiler -ErrorAction SilentlyContinue | Stop-Process
    Exec-Block { & .\publish-assets.ps1 -binariesPath $configDir -branchName $branchName -apiKey $nugetApiKey -test:$(-not $official) }
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
