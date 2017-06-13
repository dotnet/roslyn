[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$restore = $false,
    [switch]$release = $false,
    [switch]$cibuild = $false,
    [switch]$build = $false,
    [switch]$bootstrap = $false,

    # Test options 
    [switch]$test32 = $false,
    [switch]$test64 = $false,
    [switch]$testDeterminism = $false,
    [switch]$testBuildCorrectness = $false,
    [switch]$testPerfCorrectness = $false,
    [switch]$testPerfRun = $false,
    [switch]$testVsi = $false,
    [switch]$testVsiNetCore = $false,
    [switch]$testDesktop = $false,
    [switch]$testCoreClr = $false,
    [parameter(ValueFromRemainingArguments=$true)] $badArgs)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
    Write-Host "Usage: build.ps1"
    Write-Host "  -release                  Perform release build (default is debug)"
    Write-Host "  -restore                  Restore packages"
    Write-Host "  -build                    Build the Roslyn source"
    Write-Host "  -bootstrap                Build using a bootstrap Roslyn"
    Write-Host "" 
    Write-Host "Test options" 
    Write-Host "  -test32                   Run unit tests in the 32-bit runner"
    Write-Host "  -test64                   Run units tests in the 64-bit runner"
    Write-Host "  -testDesktop              Run desktop unit tests"
    Write-Host "  -testCoreClr              Run CoreClr unit tests"
    Write-Host "  -testVsi                  Run all integration tests"
    Write-Host "  -testVsiNetCore           Run just dotnet core integration tests"
    Write-Host "  -testBuildCorrectness     Run build correctness tests"
    Write-Host "  -testPerfCorrectness      Run perf correctness tests"
}

# Process the command line arguments and establish defaults for the values which
# are not specified.
function Process-Arguments() {
    if ($badArgs -ne $null) {
        Write-Host "Unsupported argument $badArgs"
        Print-Usage
        exit 1
    }

    if ($test32 -and $test64) {
        Write-Host "Cannot combine -test32 and -test64"
        exit 1
    }

    $anyVsi = $testVsi -or $testVsiNetCore
    $anyUnit = $testDesktop -or $testCoreClr
    if ($anyUnit -and $anyVsi) {
        Write-Host "Cannot combine unit and VSI testing"
        exit 1
    }

    $test32 = -not $test64

    if ($cibuild) {
        $bootstrap = $true
        $restore = $true
        $build = $true
    }

    if ($testDeterminism) {
        $bootstrap = $true
    }
}

# TODO need to think about delpoyextensions default and ci, local, etc ... 

function Run-MSBuild() {
    # TODO: Use everything we have in BuildAndTest.proj
    # /p:PathMap="$($repoDir)=q:\roslyn" /p:Feature=pdb-path-determinism 
    # /p:TreatWarningsAsErrors=true
    # /p:RoslynRuntimeIdentifier=win7-x64 

    # Because we override the C#/VB toolset to build against our LKG package, it is important
    # that we do not reuse MSBuild nodes from other jobs/builds on the machine. Otherwise,
    # we'll run into issues such as https://github.com/dotnet/roslyn/issues/6211.
    # MSBuildAdditionalCommandLineArgs=
    $buildArgs = "/warnaserror /nologo /m /nodeReuse:false /consoleloggerparameters:Verbosity=minimal /filelogger /fileloggerparameters:Verbosity=normal"
    foreach ($arg in $args) { 
        $buildArgs += " $arg"
    }
    
    Exec-Command $msbuild $buildArgs
}

# Create a bootstrap build of the compiler.  Returns the directory where the bootstrap buil 
# is located. 
#
# Important to not set $script:bootstrapDir here yet as we're actually in the process of 
# building the bootstrap.
function Make-BootstrapBuild() {
    $bootstrapLog = Join-Path $binariesDir "Bootstrap.log"
    Run-MSBuild /p:UseShippingAssemblyVersion=true /p:InitialDefineConstants=BOOTSTRAP "build\Toolset\Toolset.csproj" /p:Configuration=$buildConfiguration /fileloggerparameters:LogFile=$($bootstrapLog)
    $dir = Join-Path $binariesDir "Bootstrap"
    Remove-Item -re $dir -ErrorAction SilentlyContinue
    Create-Directory $dir
    Move-Item "$configDir\Exes\Toolset\*" $dir
    Run-MSBuild /t:Clean "build\Toolset\Toolset.csproj" /p:Configuration=$buildConfiguration
    Stop-BuildProcesses
    return $dir
}

function Test-PerfCorrectness() {
    Run-MSBuild Roslyn.sln /p:Configuration=$buildConfiguration /p:DeployExtension=false
    Exec-Block { & ".\Binaries\$buildConfiguration\Exes\Perf.Runner\Roslyn.Test.Performance.Runner.exe" --ci-test } | Out-Host
}

function Test-PerfRun() { 
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
        Exec-Block { & ".\build\scripts\install_benchview_tools.cmd" ".\Binaries\$buildConfiguration\tools\" } | Out-Host
    }

    Stop-BuildProcesses
    & ".\Binaries\$buildConfiguration\Exes\Perf.Runner\Roslyn.Test.Performance.Runner.exe"  $extraArgs --search-directory=".\\Binaries\\$buildConfiguration\\Dlls\\" --no-trace-upload
    if (-not $?) { 
        throw "Perf run failed"
    }
}

# Core function for running our unit / integration tests tests
function Test-XUnit() { 

    # To help the VS SDK team track down their issues around install via build temporarily 
    # re-enabling the build based deployment
    # 
    # https://github.com/dotnet/roslyn/issues/17456
    $deployExtensionViaBuild = $false

    if ($build) {
        $deployArg = if ($deployExtensionViaBuild) { "true" } else { "false" }
        Run-MSBuild Roslyn.sln /p:Configuration=$buildConfiguration /p:DeployExtension=$deployArg
        
        if ($testDesktop) { 
            Run-MSBuild src\Samples\Samples.sln /p:Configuration=$buildConfiguration /p:DeployExtension=false
        }

        Stop-BuildProcesses
    }

    $anyVsi = $testVsi -or $TestVsiNetCore
    if ($anyVsi -and (-not $deployExtensionViaBuild)) {
        Delpoy-VsixViaTool
    }

    $unitDir = Join-Path $configDir "UnitTests"
    if ($testCoreClr) {
        # TODO FIX THIs
        exit 1
    }
    else { 
        $runTests = Join-Path $configDir "Exes\RunTests\RunTests.exe"
        $xunitDir = Join-Path (Get-PackageDir "xunit.runner.console") "tools"
        $dlls = Get-ChildItem -re -in "*.UnitTests.dll" $unitDir
        $args = "$xunitDir"

        foreach ($dll in $dlls) { 
            $args += " $dll"
        }
        
        try {
            Exec-Command $runTests $args | Out-Host 
        }
        finally {
            Get-Process "xunit*" -ErrorAction SilentlyContinue | Stop-Process    
        }
    } 
}

# Deploy our core VSIX libraries to Visual Studio via the Roslyn VSIX tool.  This is an alternative to 
# deploying at build time.
function Deploy-VsixViaTool() { 

    $vsixDir = Get-PackageDir "roslyntools.microsoft.vsixexpinstaller"
    $vsixExe = Join-Path $vsixDir "tools\VsixExpInstaller.exe"
    $vsDir = [IO.Path]::GetFullPath("$msbuildDir\..\..\..\")
    $baseArgs = "-rootSuffix:RoslynDev -vsInstallDir:`"$vsDir`""
    $all = @(
        "Vsix\CompilerExtension\Roslyn.Compilers.Extension.vsix",
        "Vsix\VisualStudioSetup\Roslyn.VisualStudio.Setup.vsix",
        "Vsix\VisualStudioSetup.Next\Roslyn.VisualStudio.Setup.Next.vsix",
        "Vsix\VisualStudioInteractiveComponents\Roslyn.VisualStudio.InteractiveComponents.vsix",
        "Vsix\ExpressionEvaluatorPackage\ExpressionEvaluatorPackage.vsix",
        "Vsix\VisualStudioDiagnosticsWindow\Roslyn.VisualStudio.DiagnosticsWindow.vsix",
        "Vsix\VisualStudioIntegrationTestSetup\Microsoft.VisualStudio.IntegrationTest.Setup.vsix")

    Write-Host "Installing all Roslyn VSIX"
    foreach ($e in $all) {
        $name = Split-Path -leaf $e
        $filePath = Join-Path $configDir $e
        $fullArg = "$baseArgs $filePath"
        Write-Host "`tInstalling $name"
        Exec-Command $vsix $fullArg | Out-Host
    }
}

# Temporary code to help track down a NuGet cache corruption bug.
# https://github.com/dotnet/roslyn/issues/19882
function Test-NuGetCache([string]$place) {
    Write-Host "Testing NuGet cache: $place"
    Exec-Block { & ".\build\scripts\test-nuget-cache.ps1" }
}

# Ensure that procdump is available on the machine.  Returns the path to the directory that contains 
# the procdump binaries (both 32 and 64 bit)
function Ensure-ProcDump() {

    # Jenkins images default to having procdump installed in the root.  Use that if available to avoid
    # an unnecessary download.
    if (Test-Path "c:\SysInternals\procdump.exe") {
        return "c:\SysInternals";
    }    

    $toolsDir = Join-Path $binariesDir "Tools"
    $outDir = Join-Path $toolsDir "ProcDump"
    $filePath = Join-Path $outDir "procdump.exe"
    if (-not (Test-Path $filePath)) { 
        Remove-Item -Re $filePath -ErrorAction SilentlyContinue
        Create-Directory $outDir 
        $zipFilePath = Join-Path $toolsDir "procdump.zip"
        Invoke-WebRequest "https://download.sysinternals.com/files/Procdump.zip" -outfile $zipFilePath | Out-Null
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::ExtractToDirectory($zipFilePath, $outDir)
    }

    return $outDir
}

# The Jenkins images used to execute our tests can live for a very long time.  Over the course
# of hundreds of runs this can cause the %TEMP% folder to fill up.  To avoid this we redirect
# %TEMP% into the binaries folder which is deleted at the end of every run as a part of cleaning
# up the workspace.
function Redirect-Temp() {
    $temp = Join-Path $binariesDir "Temp"
    Create-Directory $temp
    ${env:TEMP} = $temp
    ${env:TMP} = $temp
}

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $repoDir

    Process-Arguments

    $debug = -not $release
    $buildConfiguration = if ($release) { "Release" } else { "Debug" }
    $msbuild = Ensure-MSBuild
    $msbuildDir = Split-Path -parent $msbuild
    $configDir = Join-Path $binariesDIr $buildConfiguration
    $bootstrapDir = ""

    # Ensure the main output directories exist as a number of tools will fail when they don't exist. 
    Create-Directory $binariesDir
    Create-Directory $configDir 

    if ($cibuild) { 
        Redirect-Temp
        Test-NuGetCache "start of CI"
        ${env:NUGET_SHOW_STACK}="true"
    }

    if ($restore) { 
        Write-Host "Running restore"
        Restore-All -msbuildDir $msbuildDir 

        if ($cibuild) {
            Test-NuGetCache "after restore"
        }
    }

    if ($testBuildCorrectness) {
        Exec-Block { & ".\build\scripts\test-build-correctness.ps1" -config $buildConfiguration } | Out-Host
        exit 0
    }

    if ($bootstrap) {
        $bootstrapDir = Make-BootstrapBuild
    }

    if ($testDeterminism) {
        Exec-Block { & ".\build\scripts\test-determinism.ps1" -bootstrapDir $bootstrapDir } | Out-Host
        exit 0
    }

    if ($testPerfCorrectness) {
        Test-PerfCorrectness
        exit 0
    }

    if ($testPerfRun) {
        Test-PerfRun
        exit 0
    }

    if ($testDesktop -or $testCoreClr -or $testVsi -or $testVsiNetCore) {
        Test-XUnit
        exit 0
    } 

    if ($build) {
        Run-MSBuild Roslyn.sln /p:Configuration=$buildConfiguration /p:DeployExtension=false
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
        Stop-BuildProcesses
    }
}
