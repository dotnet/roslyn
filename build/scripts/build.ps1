#
# This script controls the Roslyn build process. This encompasess everything from build, testing to
# publishing of NuGet packages. The intent is to structure it to allow for a simple flow of logic 
# between the following phases:
#
#   - restore
#   - build
#   - sign
#   - pack
#   - test
#   - publish
#
# Each of these phases has a separate command which can be executed independently. For instance 
# it's fine to call `build.ps1 -build -testDesktop` followed by repeated calls to 
# `.\build.ps1 -testDesktop`. 

[CmdletBinding(PositionalBinding=$false)]
param (
    # Configuration
    [switch]$restore = $false,
    [switch]$release = $false,
    [switch]$official = $false,
    [switch]$cibuild = $false,
    [switch]$build = $false,
    [switch]$buildAll = $false,
    [switch]$bootstrap = $false,
    [switch]$sign = $false,
    [switch]$pack = $false,
    [switch]$binaryLog = $false,
    [string]$msbuildDir = "",
    [string]$signType = "",

    # Test options 
    [switch]$test32 = $false,
    [switch]$test64 = $false,
    [switch]$testVsi = $false,
    [switch]$testVsiNetCore = $false,
    [switch]$testDesktop = $false,
    [switch]$testCoreClr = $false,
    [switch]$testIOperation = $false,

    # Special test options
    [switch]$testDeterminism = $false,
    [switch]$testBuildCorrectness = $false,
    [switch]$testPerfCorrectness = $false,
    [switch]$testPerfRun = $false,

    [parameter(ValueFromRemainingArguments=$true)] $badArgs)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
    Write-Host "Usage: build.ps1"
    Write-Host "  -release                  Perform release build (default is debug)"
    Write-Host "  -restore                  Restore packages"
    Write-Host "  -build                    Build Roslyn.sln"
    Write-Host "  -buildAll                 Build all Roslyn source items"
    Write-Host "  -official                 Perform an official build"
    Write-Host "  -bootstrap                Build using a bootstrap Roslyn"
    Write-Host "  -sign                     Sign our binaries"
    Write-Host "  -signType                 Type of sign: real, test, verify"
    Write-Host "  -pack                     Create our NuGet packages"
    Write-Host "  -msbuildDir               MSBuild to use for operations"
    Write-Host "  -binaryLog                Create binary log for every MSBuild invocation"
    Write-Host "" 
    Write-Host "Test options" 
    Write-Host "  -test32                   Run unit tests in the 32-bit runner"
    Write-Host "  -test64                   Run units tests in the 64-bit runner"
    Write-Host "  -testDesktop              Run desktop unit tests"
    Write-Host "  -testCoreClr              Run CoreClr unit tests"
    Write-Host "  -testVsi                  Run all integration tests"
    Write-Host "  -testVsiNetCore           Run just dotnet core integration tests"
    Write-Host "  -testIOperation           Run extra checks to validate IOperations"
    Write-Host ""
    Write-Host "Special Test options" 
    Write-Host "  -testBuildCorrectness     Run build correctness tests"
    Write-Host "  -testDeterminism          Run determinism tests"
    Write-Host "  -testPerfCorrectness      Run perf correctness tests"
    Write-Host "  -testPerfCorrectness      Run perf tests"
}

# Process the command line arguments and establish defaults for the values which are not 
# specified.
#
# In this function it's okay to use two arguments to extend the effect of another. For 
# example it's okay to look at $buildAll and infer $build. It's not okay though to infer 
# $build based on say $testDesktop. It's possible the developer wanted only for testing 
# to execute, not any build.
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

    $script:isAnyTestSpecial = $testBuildCorrectness -or $testDeterminism -or $testPerfCorrectness -or $testPerfRun
    if ($isAnyTestSpecial -and ($anyUnit -or $anyVsi)) {
        Write-Host "Cannot combine special testing with any other action"
        exit 1
    }

    if ($buildAll) {
        $script:build = $true
    }

    $script:test32 = -not $test64
    $script:debug = -not $release
}

function Run-MSBuild([string]$projectFilePath, [string]$buildArgs = "", [string]$logFileName = "", [switch]$parallel = $true) {
    # Because we override the C#/VB toolset to build against our LKG package, it is important
    # that we do not reuse MSBuild nodes from other jobs/builds on the machine. Otherwise,
    # we'll run into issues such as https://github.com/dotnet/roslyn/issues/6211.
    # MSBuildAdditionalCommandLineArgs=
    $args = "/p:TreatWarningsAsErrors=true /warnaserror /nologo /nodeReuse:false /consoleloggerparameters:Verbosity=minimal;summary /p:Configuration=$buildConfiguration";

    if ($parallel) {
        $args += " /m"
    }
    
    if ($binaryLog) {
        if ($logFileName -eq "") { 
            $logFileName = [IO.Path]::GetFileNameWithoutExtension($projectFilePath)
        }
        $logFileName = [IO.Path]::ChangeExtension($logFileName, ".binlog")
        $logDir = Join-Path $binariesDir "Logs"
        Create-Directory $logDir
        $logFilePath = Join-Path $logDir $logFileName
        $args += " /bl:$logFilePath"
    }

    if ($official) {
        $args += " /p:OfficialBuild=true"
    }

    if ($bootstrapDir -ne "") {
        $args += " /p:BootstrapBuildPath=$bootstrapDir"
    }

    if ($testIOperation)
    {
        $args += " /p:TestIOperationInterface=true"
    }

    $args += " $buildArgs"
    $args += " $projectFilePath"
    Exec-Console $msbuild $args
}

# Create a bootstrap build of the compiler.  Returns the directory where the bootstrap buil 
# is located. 
#
# Important to not set $script:bootstrapDir here yet as we're actually in the process of 
# building the bootstrap.
function Make-BootstrapBuild() {

    Write-Host "Building Bootstrap compiler"
    Run-MSBuild "build\Toolset\Toolset.csproj" "/p:UseShippingAssemblyVersion=true /p:InitialDefineConstants=BOOTSTRAP" -logFileName "Bootstrap"
    $dir = Join-Path $binariesDir "Bootstrap"
    Remove-Item -re $dir -ErrorAction SilentlyContinue
    Create-Directory $dir
    Move-Item "$configDir\Exes\Toolset\*" $dir

    Write-Host "Cleaning Bootstrap compiler artifacts"
    Run-MSBuild "build\Toolset\Toolset.csproj" "/t:Clean" -logFileName "BootstrapClean"
    Stop-BuildProcesses
    return $dir
}

function Build-Artifacts() { 
    Run-MSBuild "Roslyn.sln" "/p:DeployExtension=false"

    if ($testDesktop) { 
        Run-MSBuild "src\Samples\Samples.sln" "/p:DeployExtension=false"
    }

    if ($buildAll) {
        Build-ExtraSignArtifacts
    }

    if ($pack) {
        Build-NuGetPackages
    }

    if ($sign) {
        Run-SignTool
    }

    if ($pack -and ($cibuild -or $official)) { 
        Build-DeployToSymStore
    }

    if ($buildAll) {
        Build-InsertionItems
    }
}

# Not all of our artifacts needed for signing are included inside Roslyn.sln. Need to 
# finish building these before we can run signing.
function Build-ExtraSignArtifacts() { 

    Push-Location (Join-Path $repoDir "src\Setup")
    try {
        # Publish the CoreClr projects (CscCore and VbcCore) and dependencies for later NuGet packaging.
        Write-Host "Publishing csc"
        Run-MSBuild "..\Compilers\CSharp\csc\csc.csproj" "/p:TargetFramework=netcoreapp2.0 /t:PublishWithoutBuilding"
        Write-Host "Publishing csc"
        Run-MSBuild "..\Compilers\VisualBasic\vbc\vbc.csproj" "/p:TargetFramework=netcoreapp2.0 /t:PublishWithoutBuilding"

        # No need to build references here as we just built the rest of the source tree. 
        # We build these serially to work around https://github.com/dotnet/roslyn/issues/11856,
        # where building multiple projects that produce VSIXes larger than 10MB will race against each other
        Run-MSBuild "Deployment\Current\Roslyn.Deployment.Full.csproj" "/p:BuildProjectReferences=false" -parallel:$false
        Run-MSBuild "Deployment\Next\Roslyn.Deployment.Full.Next.csproj" "/p:BuildProjectReferences=false" -parallel:$false

        $dest = @(
            $configDir,
            "Templates\CSharp\Diagnostic\Analyzer",
            "Templates\VisualBasic\Diagnostic\Analyzer\tools")
        foreach ($dir in $dest) { 
            Copy-Item "PowerShell\*.ps1" $dir
        }

        Run-MSBuild "Templates\Templates.sln" "/p:VersionType=Release"
        Run-MSBuild "DevDivInsertionFiles\DevDivInsertionFiles.sln" -buildArgs ""
        Copy-Item -Force "Vsix\myget_org-extensions.config" $configDir
    }
    finally {
        Pop-Location
    }
}

function Build-InsertionItems() { 

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

    $setupDir = Join-Path $repoDir "src\Setup"
    Push-Location $setupDir
    try { 
        Create-PerfTests
        Exec-Console (Join-Path $configDir "Exes\DevDivInsertionFiles\Roslyn.BuildDevDivInsertionFiles.exe") "$configDir $setupDir $(Get-PackagesDir)"
        
        # In non-official builds need to supply values for a few MSBuild properties. The actual value doesn't
        # matter, just that it's provided some value.
        $extraArgs = ""
        if (-not $official) { 
            $extraArgs = " /p:FinalizeValidate=false /p:ManifestPublishUrl=https://vsdrop.corp.microsoft.com/file/v1/Products/DevDiv/dotnet/roslyn/master/20160729.6"
        }

        Run-MSBuild "DevDivPackages\Roslyn.proj" -logFileName "RoslynPackagesProj"
        Run-MSBuild "DevDivVsix\PortableFacades\PortableFacades.vsmanproj" -buildArgs $extraArgs
        Run-MSBuild "DevDivVsix\CompilersPackage\Microsoft.CodeAnalysis.Compilers.vsmanproj" -buildArgs $extraArgs
        Run-MSBuild "DevDivVsix\MicrosoftCodeAnalysisLanguageServices\Microsoft.CodeAnalysis.LanguageServices.vsmanproj" -buildArgs "$extraArgs"
        Run-MSBuild "..\Dependencies\Microsoft.NetFX20\Microsoft.NetFX20.nuget.proj"
    }
    finally {
        Pop-Location
    }
}

function Build-NuGetPackages() {
    $buildArgs = ""
    if (-not $official) {
        $buildArgs = '/p:SkipReleaseVersion=true /p:SkipPreReleaseVersion=true'
    }

    Run-MSBuild "src\NuGet\NuGet.proj" $buildArgs
}

function Build-DeployToSymStore() {
    Run-MSBuild "Roslyn.sln" "/t:DeployToSymStore" -logFileName "RoslynDeployToSymStore"
}

# These are tests that don't follow our standard restore, build, test pattern. They customize 
# the processes in order to test specific elements of our build and hence are handled 
# separately from our other tests
function Test-Special() {

    if ($restore) { 
        Write-Host "Running restore"
        Restore-All -msbuildDir $msbuildDir 
    }

    if ($testBuildCorrectness) {
        Exec-Block { & ".\build\scripts\test-build-correctness.ps1" -config $buildConfiguration } | Out-Host
    }
    elseif ($testDeterminism) {
        $bootstrapDir = Make-BootstrapBuild
        Exec-Block { & ".\build\scripts\test-determinism.ps1" -bootstrapDir $bootstrapDir } | Out-Host
    } 
    elseif ($testPerfCorrectness) {
        Test-PerfCorrectness
    }
    elseif ($testPerfRun) {
        Test-PerfRun
    }
    else {
        throw "Not a special test"
    }
}

function Test-PerfCorrectness() {
    Run-MSBuild "Roslyn.sln" "/p:DeployExtension=false" -logFileName "RoslynPerfCorrectness"
    Exec-Block { & ".\Binaries\$buildConfiguration\Exes\Perf.Runner\Roslyn.Test.Performance.Runner.exe" --ci-test } | Out-Host
}

function Test-PerfRun() { 
    Run-MSBuild "Roslyn.sln" "/p:DeployExtension=false" -logFileName "RoslynPerfRun"

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

    $unitDir = Join-Path $configDir "UnitTests"
    $runtimeIdentifier = "win-x64"
    $tf = "netcoreapp2.0"
    $logDir = Join-Path $unitDir "xUnitResults"
    Create-Directory $logDir 
    $xunitConsole = Join-Path (Get-PackageDir "dotnet-xunit") "tools\$tf\xunit.console.dll"

    # A number of our tests need to be published before they can be executed in order to get some 
    # runtime assets.
    $needPublish = @(
        "src\Compilers\CSharp\Test\Symbol\CSharpCompilerSymbolTest.csproj"
    )

    foreach ($file in $needPublish) {
        $name = Split-Path -leaf $file
        Write-Host "Publishing $name"
        $filePath = Join-Path $repoDir $file
        Run-MSBuild "$filePath" "/m /v:m /t:Publish /p:TargetFramework=$tf /p:RuntimeIdentifier=$runtimeIdentifier /p:SelfContained=true"
    }

    $dlls = @()
    $allGood = $true
    foreach ($dir in Get-ChildItem $unitDir) {
        $testDir = Join-Path $unitDir (Join-Path $dir $tf)
        if (Test-Path $testDir) { 
            $publishDir = Join-Path $testDir "$runtimeIdentifier\publish"
            if (Test-path $publishDir) {
                $testDir = $publishDir
            }   
            
            $dllName = Get-ChildItem -name "*.UnitTests.dll" -path $testDir
            $dllPath = Join-Path $testDir $dllName

            $args = "exec"
            $args += " --depsfile " + [IO.Path]::ChangeExtension($dllPath, ".deps.json")
            $args += " --runtimeconfig " + [IO.Path]::ChangeExtension($dllPath, ".runtimeconfig.json")
            $args += " $xunitConsole"
            $args += " $dllPath"
            $args += " -xml " + (Join-Path $logDir ([IO.Path]::ChangeExtension($dllName, ".xml")))

            try {
                Write-Host "Running $dllName"
                Exec-Console $dotnet $args
            }
            catch {
                Write-Host "Failed"
                $allGood = $false
            }
        }
    }

    if (-not $allGood) { 
        throw "Unit tests failed"
    }
}

# Core function for running our unit / integration tests tests
function Test-XUnit() { 

    if ($testCoreClr) {
        Test-XUnitCoreClr
        return
    }

    if ($testVsi -or $testVsiNetCore) {
        Deploy-VsixViaTool
    }

    $logFilePath = Join-Path $configDir "runtests.log"
    $unitDir = Join-Path $configDir "UnitTests"
    $runTests = Join-Path $configDir "Exes\RunTests\RunTests.exe"
    $xunitDir = Join-Path (Get-PackageDir "xunit.runner.console") "tools"
    $args = "$xunitDir"
    $args += " -log:$logFilePath"
    $args += " -nocache"

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

    # Exclude out the multi-targetted netcore app projects
    $dlls = $dlls | ?{ -not ($_.FullName -match ".*netcoreapp.*") }

    # Exclude out the ref assemblies
    $dlls = $dlls | ?{ -not ($_.FullName -match ".*\\ref\\.*") }
    $dlls = $dlls | ?{ -not ($_.FullName -match ".*/ref/.*") }

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

# Sign all of our binaries that need to be signed
function Run-SignTool() { 
    Push-Location $repoDir
    try {
        $signTool = Join-Path (Get-PackageDir "RoslynTools.Microsoft.SignTool") "tools\SignTool.exe"
        $signToolArgs = "-msbuildPath `"$msbuild`""
        switch ($signType) {
            "real" { break; }
            "test" { $signToolArgs += " -testSign"; break; }
            default { $signToolArgs += " -test"; break; }
        }

        $signToolArgs += " `"$configDir`""
        Exec-Console $signTool $signToolArgs
    }
    finally { 
        Pop-Location
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
        Invoke-WebRequest "https://download.sysinternals.com/files/Procdump.zip" -UseBasicParsing -outfile $zipFilePath | Out-Null
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

    $msbuild, $msbuildDir = Ensure-MSBuildAndDir -msbuildDir $msbuildDir
    $dotnet, $sdkDir = Ensure-SdkInPathAndData
    $buildConfiguration = if ($release) { "Release" } else { "Debug" }
    $configDir = Join-Path $binariesDir $buildConfiguration
    $bootstrapDir = ""

    # Ensure the main output directories exist as a number of tools will fail when they don't exist. 
    Create-Directory $binariesDir
    Create-Directory $configDir 

    if ($cibuild) { 
        List-VSProcesses
        List-BuildProcesses
        Redirect-Temp
    }

    if ($isAnyTestSpecial) {
        Test-Special
        exit 0
    }

    if ($restore) { 
        Write-Host "Running restore"
        Restore-All -msbuildDir $msbuildDir 
    }

    if ($bootstrap) {
        $bootstrapDir = Make-BootstrapBuild
    }

    if ($build) {
        Build-Artifacts
    }

    if ($testDesktop -or $testCoreClr -or $testVsi -or $testVsiNetCore) {
        Test-XUnit
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
