[CmdletBinding(PositionalBinding=$false)]
param (
    # Configuration
    [switch]$restore = $false,
    [switch]$release = $false,
    [switch]$official = $false,
    [switch]$cibuild = $false,
    [switch]$build = $false,
    [switch]$bootstrap = $false,
    [string]$msbuildDir = "",

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
    Write-Host "  -official                 Perform an official build"
    Write-Host "  -bootstrap                Build using a bootstrap Roslyn"
    Write-Host "  -msbuildDir               MSBuild to use for operations"
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

    $script:test32 = -not $test64
    $script:debug = -not $release

    if ($testDeterminism) {
        $script:bootstrap = $true
    }
}

function Run-MSBuild([string]$buildArgs = "", [string]$logFile = "") {
    # Because we override the C#/VB toolset to build against our LKG package, it is important
    # that we do not reuse MSBuild nodes from other jobs/builds on the machine. Otherwise,
    # we'll run into issues such as https://github.com/dotnet/roslyn/issues/6211.
    # MSBuildAdditionalCommandLineArgs=
    $args = "/p:TreatWarningsAsErrors=true /warnaserror /nologo /m /nodeReuse:false /consoleloggerparameters:Verbosity=minimal";
    
    if ($logFile -ne "") {
        $args += " /filelogger /fileloggerparameters:Verbosity=normal;logFile=$logFile";
    }

    if ($cibuild) { 
        $args += " /p:PathMap=`"$($repoDir)=q:\roslyn`" /p:Feature=pdb-path-determinism" 
    }

    if ($official) {
        $args += " /p:OfficialBuild=true"
    }

    $args += " $buildArgs"
    Exec-Console $msbuild $args
}

# Create a bootstrap build of the compiler.  Returns the directory where the bootstrap buil 
# is located. 
#
# Important to not set $script:bootstrapDir here yet as we're actually in the process of 
# building the bootstrap.
function Make-BootstrapBuild() {

    $bootstrapLog = Join-Path $binariesDir "Bootstrap.log"
    Write-Host "Building Bootstrap compiler"
    Run-MSBuild "/p:UseShippingAssemblyVersion=true /p:InitialDefineConstants=BOOTSTRAP build\Toolset\Toolset.csproj /p:Configuration=$buildConfiguration" -logFile $bootstrapLog 
    $dir = Join-Path $binariesDir "Bootstrap"
    Remove-Item -re $dir -ErrorAction SilentlyContinue
    Create-Directory $dir
    Move-Item "$configDir\Exes\Toolset\*" $dir

    Write-Host "Cleaning Bootstrap compiler artifacts"
    Run-MSBuild "/t:Clean build\Toolset\Toolset.csproj /p:Configuration=$buildConfiguration"
    Stop-BuildProcesses
    return $dir
}

function Test-PerfCorrectness() {
    Run-MSBuild "Roslyn.sln /p:Configuration=$buildConfiguration /p:DeployExtension=false"
    Exec-Block { & ".\Binaries\$buildConfiguration\Exes\Perf.Runner\Roslyn.Test.Performance.Runner.exe" --ci-test } | Out-Host
}

function Test-PerfRun() { 
    Run-MSBuild "Roslyn.sln /p:Configuration=$buildConfiguration /p:DeployExtension=false"

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

function Test-XUnitCoreClr() { 

    $unitDir = Join-Path $binariesDir "CoreClrTest"
    $logDir = Join-Path $unitDir "xUnitResults"
    $logFile = Join-Path $logDir "TestResults.xml"
    Create-Directory $logDir 

    Write-Host "Publishing CoreClr tests"
    Run-MSBuild "src\Test\DeployCoreClrTestRuntime\DeployCoreClrTestRuntime.csproj /m /v:m /t:Publish /p:RuntimeIdentifier=win7-x64 /p:PublishDir=$unitDir"

    $corerun = Join-Path $unitDir "CoreRun.exe"
    $args = Join-Path $unitDir "xunit.console.netcore.exe"
    foreach ($dll in Get-ChildItem -re -in "*.UnitTests.dll" $unitDir) {
        $args += " $dll";
    }

    $args += " -parallel all"
    $args += " -xml $logFile"

    Write-Host "Running CoreClr tests"
    Exec-Console $corerun $args
}

# Core function for running our unit / integration tests tests
function Test-XUnit() { 

    if ($testCoreClr) {
        Test-XUnitCoreClr
        return
    }

    # To help the VS SDK team track down their issues around install via build temporarily 
    # re-enabling the build based deployment
    # 
    # https://github.com/dotnet/roslyn/issues/17456
    $deployExtensionViaBuild = $false

    if ($build) {
        $deployArg = if ($deployExtensionViaBuild) { "true" } else { "false" }
        Run-MSBuild "Roslyn.sln /p:Configuration=$buildConfiguration /p:DeployExtension=$deployArg"
        
        if ($testDesktop) { 
            Run-MSBuild "src\Samples\Samples.sln /p:Configuration=$buildConfiguration /p:DeployExtension=false"
        }

        Stop-BuildProcesses
    }

    $anyVsi = $testVsi -or $TestVsiNetCore
    if ($anyVsi -and (-not $deployExtensionViaBuild)) {
        Deploy-VsixViaTool
    }

    $logFilePath = Join-Path $configDir "runtests.log"
    $unitDir = Join-Path $configDir "UnitTests"
    $runTests = Join-Path $configDir "Exes\RunTests\RunTests.exe"
    $xunitDir = Join-Path (Get-PackageDir "xunit.runner.console") "tools"
    $args = "$xunitDir"
    $args += " -log:$logFilePath"

    if ($testDesktop) {
        if ($test32) {
            $dlls = Get-ChildItem -re -in "*.UnitTests.dll" $unitDir
        }
        else {
            $dlls = Get-ChildItem -re -in "*.UnitTests.dll" -ex "*Roslyn.Interactive*" $unitDir 
        }
    }
    elseif ($testVsi) {
        $dlls = Get-ChildItem -re -in "*.IntegrationTests.dll" $unitDir
    }
    else {
        $dlls = Get-ChildItem -re -in "*.IntegrationTests.dll" $unitDir
        $args += " -trait:Feature=NetCore"
    }

    if ($cibuild) {
        # Use a 50 minute timeout on CI
        $args += " -xml -timeout:50"

        $procdumpPath = Ensure-ProcDump
        $args += " -procdumppath:$procDumpPath"
    }

    if ($test64) {
        $args += " -test64"
    }

    foreach ($dll in $dlls) { 
        $args += " $dll"
    }
    
    try {
        Exec-Console $runTests $args
    }
    finally {
        Get-Process "xunit*" -ErrorAction SilentlyContinue | Stop-Process    
    }
}

# Deploy our core VSIX libraries to Visual Studio via the Roslyn VSIX tool.  This is an alternative to 
# deploying at build time.
function Deploy-VsixViaTool() { 
    $vsixDir = Get-PackageDir "roslyntools.microsoft.vsixexpinstaller"
    $vsixExe = Join-Path $vsixDir "tools\VsixExpInstaller.exe"
    $both = Get-VisualStudioDirAndId
    $vsDir = $both[0].Trim("\")
    $vsId = $both[1]
    $hive = "RoslynDev"
    Write-Host "Using VS Instance $vsId at `"$vsDir`""
    $baseArgs = "/rootSuffix:$hive /vsInstallDir:`"$vsDir`""
    $all = @(
        "Vsix\CompilerExtension\Roslyn.Compilers.Extension.vsix",
        "Vsix\VisualStudioSetup\Roslyn.VisualStudio.Setup.vsix",
        "Vsix\VisualStudioSetup.Next\Roslyn.VisualStudio.Setup.Next.vsix",
        "Vsix\VisualStudioInteractiveComponents\Roslyn.VisualStudio.InteractiveComponents.vsix",
        "Vsix\ExpressionEvaluatorPackage\ExpressionEvaluatorPackage.vsix",
        "Vsix\VisualStudioDiagnosticsWindow\Roslyn.VisualStudio.DiagnosticsWindow.vsix",
        "Vsix\VisualStudioIntegrationTestSetup\Microsoft.VisualStudio.IntegrationTest.Setup.vsix")

    Write-Host "Uninstalling old Roslyn VSIX"

    # Actual uninstall is failing at the moment using the uninstall options. Temporarily using 
    # wildfire to uninstall our VSIX extensions
    $extDir = Join-Path ${env:USERPROFILE} "AppData\Local\Microsoft\VisualStudio\15.0_$($vsid)$($hive)"
    if (Test-Path $extDir) {
        foreach ($dir in Get-ChildItem -Directory $extDir) {
            $name = Split-Path -leaf $dir
            Write-Host "`tUninstalling $name"
        }
        Remove-Item -re -fo $extDir
    }

    Write-Host "Installing all Roslyn VSIX"
    foreach ($e in $all) {
        $name = Split-Path -leaf $e
        $filePath = Join-Path $configDir $e
        $fullArg = "$baseArgs $filePath"
        Write-Host "`tInstalling $name"
        Exec-Console $vsixExe $fullArg
    }
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

function List-BuildProcesses() {
    Write-Host "Listing running build processes..."
    Get-Process -Name "msbuild" -ErrorAction SilentlyContinue | Out-Host
    Get-Process -Name "vbcscompiler" -ErrorAction SilentlyContinue | Out-Host
}

function List-VSProcesses() {
    Write-Host "Listing running vs processes..."
    Get-Process -Name "devenv" -ErrorAction SilentlyContinue | Out-Host
}

# Kill any instances VBCSCompiler.exe to release locked files, ignoring stderr if process is not open
# This prevents future CI runs from failing while trying to delete those files.
# Kill any instances of msbuild.exe to ensure that we never reuse nodes (e.g. if a non-roslyn CI run
# left some floating around).
function Stop-BuildProcesses() {
    Write-Host "Killing running build processes..."
    Get-Process -Name "msbuild" -ErrorAction SilentlyContinue | Stop-Process
    Get-Process -Name "vbcscompiler" -ErrorAction SilentlyContinue | Stop-Process
}

# Kill any instances of devenv.exe to ensure VSIX install/uninstall works in future runs and to ensure
# that any locked files don't prevent future CI runs from failing.
# Also call Stop-BuildProcesses
function Stop-VSProcesses() {
    Write-Host "Killing running vs processes..."
    Get-Process -Name "devenv" -ErrorAction SilentlyContinue | Stop-Process
}

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $repoDir

    Write-Host "Repo Dir $repoDir"
    Write-Host "Binaries Dir $binariesDir"

    Process-Arguments

    if ($msbuildDir -eq "") {
        $msbuild = Ensure-MSBuild
        $msbuildDir = Split-Path -parent $msbuild
    }
    else {
        $msbuild = Join-Path $msbuildDir "msbuild.exe"
    }

    $buildConfiguration = if ($release) { "Release" } else { "Debug" }
    $configDir = Join-Path $binariesDIr $buildConfiguration
    $bootstrapDir = ""

    # Ensure the main output directories exist as a number of tools will fail when they don't exist. 
    Create-Directory $binariesDir
    Create-Directory $configDir 

    if ($cibuild) { 
        List-VSProcesses
        List-BuildProcesses
        Redirect-Temp
    }

    if ($restore) { 
        Write-Host "Running restore"
        Restore-All -msbuildDir $msbuildDir 
    }

    if ($testBuildCorrectness) {
        Exec-Block { & ".\build\scripts\test-build-correctness.ps1" -config $buildConfiguration } | Out-Host
        exit 0
    }

    if ($bootstrap) {
        Write-Host "Making bootstrap"
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
        Run-MSBuild "Roslyn.sln /p:Configuration=$buildConfiguration /p:DeployExtension=false"
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
        Stop-VSProcesses
        Stop-BuildProcesses
    }
}
