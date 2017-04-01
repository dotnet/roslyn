[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$test64 = $false,
    [switch]$testDeterminism = $false,
    [switch]$testBuildCorrectness = $false,
    [switch]$testPerfCorrectness = $false,
    [switch]$testPerfRun = $false,
    [switch]$testVsi = $false,
    [switch]$skipTest = $false,
    [switch]$skipRestore = $false,
    [switch]$skipCommitPrinting = $false,
    [switch]$release = $false,
    [parameter(ValueFromRemainingArguments=$true)] $badArgs)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
    Write-Host "Usage: cibuild.cmd [-debug^|-release] [-test32^|-test64] [-restore]"
    Write-Host "  -debug   Perform debug build.  This is the default."
    Write-Host "  -release Perform release build."
    Write-Host "  -test32  Run unit tests in the 32-bit runner.  This is the default."
    Write-Host "  -test64  Run units tests in the 64-bit runner."
}

function Run-MSBuild() {
    # Because we override the C#/VB toolset to build against our LKG package, it is important
    # that we do not reuse MSBuild nodes from other jobs/builds on the machine. Otherwise,
    # we'll run into issues such as https://github.com/dotnet/roslyn/issues/6211.
    # MSBuildAdditionalCommandLineArgs=
    & $msbuild /warnaserror /nologo /m /nodeReuse:false /consoleloggerparameters:Verbosity=minimal /filelogger /fileloggerparameters:Verbosity=normal @args
    if (-not $?) {
        throw "Build failed"
    }
}

# Kill any instances VBCSCompiler.exe to release locked files, ignoring stderr if process is not open
# This prevents future CI runs from failing while trying to delete those files.
# Kill any instances of msbuild.exe to ensure that we never reuse nodes (e.g. if a non-roslyn CI run
# left some floating around).
function Terminate-BuildProcesses() {
    Get-Process msbuild -ErrorAction SilentlyContinue | kill 
    Get-Process vbcscompiler -ErrorAction SilentlyContinue | kill
}

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $repoDir

    if ($badArgs -ne $null) {
        Print-Usage
        exit 1
    }

    Write-Host "Parameters:"
    foreach ($k in $PSBoundParameters.Keys)  {
        $v = $PSBoundParameters[$k]
        Write-Host "`t$k=$v"
    }

    $buildConfiguration = if ($release) { "Release" } else { "Debug" }
    $msbuild = Ensure-MSBuild
    $msbuildDir = Split-Path -parent $msbuild
    $configDir = Join-Path $binariesDIr $buildConfiguration

    if (-not $skipRestore) { 
        Write-Host "Running restore"
        Restore-All -msbuildDir $msbuildDir 
    }

    # Ensure the binaries directory exists because msbuild can fail when part of the path to LogFile isn't present.
    Create-Directory $binariesDir

    if ($testBuildCorrectness) {
        Exec { & ".\build\scripts\test-build-correctness.ps1" $repoDir $configDir }
        exit 0
    }

    # Output the commit that we're building, for reference in Jenkins logs
    if (-not $skipCommitPrinting) {
        Write-Host "Building this commit:"
        Exec { & git show --no-patch --pretty=raw HEAD }
    }

    # Build with the real assembly version, since that's what's contained in the bootstrap compiler redirects
    $bootstrapLog = Join-Path $binariesDir "Bootstrap.log"
    Run-MSBuild /p:UseShippingAssemblyVersion=true /p:InitialDefineConstants=BOOTSTRAP "build\Toolset\Toolset.csproj" /p:Configuration=$buildConfiguration /fileloggerparameters:LogFile=$($bootstrapLog)
    $bootstrapDir = Join-Path $binariesDir "Bootstrap"
    Remove-Item -re $bootstrapDir -ErrorAction SilentlyContinue
    Create-Directory $bootstrapDir
    Move-Item "$configDir\Exes\Toolset\*" $bootstrapDir
    Run-MSBuild /t:Clean "build\Toolset\Toolset.csproj" /p:Configuration=$buildConfiguration
    Terminate-BuildProcesses

    if ($testDeterminism) {
        Exec { & ".\build\scripts\test-determinism.ps1" $bootstrapDir }
        Terminate-BuildProcesses
        exit 0
    }

    if ($testPerfCorrectness) {
        Run-MSBuild Roslyn.sln /p:Configuration=$buildConfiguration /p:DeployExtension=false
        Exec { & ".\Binaries\$buildConfiguration\Exes\Perf.Runner\Roslyn.Test.Performance.Runner.exe" --ci-test }
        exit 0
    }

    if ($testPerfRun) {
        Run-MSBuild Roslyn.sln /p:Configuration=$buildConfiguration /p:DeployExtension=false

        # Check if we have credentials to upload to benchview
        $extraArgs = @()
        if ((Test-Path env:\GIT_BRANCH) -and (Test-Path env:\BV_UPLOAD_SAS_TOKEN)) {
            $extraArgs += "--report-benchview"
            $extraArgs += "--branch=$env:GIT_BRANCH"

            # Check if we are in a PR or this is a rolling submission
            if (Test-Path env:\ghprbPullTitle) {
                $submissionName = $env:ghprbPullTitle.Replace(" ", "_")
                $extraArgs += "--benchview-submission-name=""$submissionName"""
                $extraArgs += "--benchview-submission-type=private"
            } 
            else {
                $extraArgs += "--benchview-submission-type=rolling"
            }

            Create-Directory ".\Binaries\$buildConfiguration\tools\"
            # Get the benchview tools - Place alongside Roslyn.Test.Performance.Runner.exe
            Exec { & ".\build\scripts\install_benchview_tools.cmd" ".\Binaries\$buildConfiguration\tools\" }
        }

        Terminate-BuildProcesses
        & ".\Binaries\$buildConfiguration\Exes\Perf.Runner\Roslyn.Test.Performance.Runner.exe"  $extraArgs --search-directory=".\\Binaries\\$buildConfiguration\\Dlls\\" --no-trace-upload
        if (-not $?) { 
            throw "Perf run failed"
        }
        exit 0
    }

    $target = if ($skipTest) { "Build" } else { "BuildAndTest" }
    $test64Arg = if ($test64) { "true" } else { "false" }
    $testVsiArg = if ($testVsi) { "true" } else { "false" }
    $buildLog = Join-Path $binariesdir "Build.log"

    Run-MSBuild /p:BootstrapBuildPath="$bootstrapDir" BuildAndTest.proj /t:$target /p:Configuration=$buildConfiguration /p:Test64=$test64Arg /p:TestVsi=$testVsiArg /p:PathMap="$($repoDir)=q:\roslyn" /p:Feature=pdb-path-determinism /fileloggerparameters:LogFile="$buildLog"`;verbosity=diagnostic /p:DeployExtension=false
    exit 0
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
finally {
    Pop-Location
}
