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
    [string]$configuration = "Debug",

    # Configuration
    [switch]$restore = $false,
    [switch]$official = $false,
    [switch]$cibuild = $false,
    [switch]$build = $false,
    [switch]$buildCoreClr = $false,
    [switch]$bootstrap = $false,
    [switch]$sign = $false,
    [switch]$pack = $false,
    [switch]$binaryLog = $false,
    [switch]$deployExtensions = $false,
    [switch]$launch = $false,
    [switch]$procdump = $false,
    [switch]$skipAnalyzers = $false,
    [switch]$checkLoc = $false,

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

    [parameter(ValueFromRemainingArguments=$true)][string[]]$properties)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
    Write-Host "Usage: build.ps1"
    Write-Host "  -configuration            Build configuration ('Debug' or 'Release')"
    Write-Host "  -restore                  Restore packages"
    Write-Host "  -build                    Build Roslyn.sln"
    Write-Host "  -official                 Perform an official build"
    Write-Host "  -bootstrap                Build using a bootstrap Roslyn"
    Write-Host "  -sign                     Sign our binaries"
    Write-Host "  -pack                     Build NuGet packages, VS insertion manifests and installer"
    Write-Host "  -deployExtensions         Deploy built vsixes"
    Write-Host "  -binaryLog                Create binary log for every MSBuild invocation"
    Write-Host "  -procdump                 Monitor test runs with procdump"
    Write-Host "  -skipAnalyzers            Do not run analyzers during build operations"
    Write-Host "  -checkLoc                 Check that all resources are localized"
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
    Write-Host "  -testDeterminism          Run determinism tests"
}

# Process the command line arguments and establish defaults for the values which are not
# specified.
#
# In this function it's okay to use two arguments to extend the effect of another. For
# example it's okay to look at $buildCoreClr and infer $build. It's not okay though to infer
# $build based on say $testDesktop. It's possible the developer wanted only for testing
# to execute, not any build.
function Process-Arguments() {
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

    if ($cibuild -and -not $official -and $anyVsi) {
        # Avoid spending time in analyzers when requested, and also in the slowest integration test builds
        $script:skipAnalyzers = $true
    }

    if ($testDeterminism -and ($anyUnit -or $anyVsi)) {
        Write-Host "Cannot combine special testing with any other action"
        exit 1
    }

    if ($build -and $launch -and -not $deployExtensions) {
        Write-Host -ForegroundColor Red "Cannot combine -build and -launch without -deployExtensions"
        exit 1
    }

    if ($buildCoreClr) {
        $script:build = $true
    }

    $script:test32 = -not $test64
}

function Run-MSBuild([string]$projectFilePath, [string]$buildArgs = "", [string]$logFileName = "", [switch]$parallel = $true, [switch]$useDotnetBuild = $false, [switch]$summary = $true, [switch]$warnAsError = $true) {
    # Because we override the C#/VB toolset to build against our LKG package, it is important
    # that we do not reuse MSBuild nodes from other jobs/builds on the machine. Otherwise,
    # we'll run into issues such as https://github.com/dotnet/roslyn/issues/6211.
    # MSBuildAdditionalCommandLineArgs=
    $args = "/p:TreatWarningsAsErrors=true /nologo /nodeReuse:false /p:Configuration=$configuration ";

    if ($warnAsError) {
        $args += " /warnaserror"
    }

    if ($summary) {
        $args += " /consoleloggerparameters:Verbosity=minimal;summary"
    } else {        
        $args += " /consoleloggerparameters:Verbosity=minimal"
    }

    if ($parallel) {
        $args += " /m"
    }

    if ($skipAnalyzers) {
        $args += " /p:UseRoslynAnalyzers=false"
    }

    if ($binaryLog) {
        if ($logFileName -eq "") {
            $logFileName = [IO.Path]::GetFileNameWithoutExtension($projectFilePath)
        }
        $logFileName = [IO.Path]::ChangeExtension($logFileName, ".binlog")
        $logFilePath = Join-Path $logsDir $logFileName
        $args += " /bl:$logFilePath"
    }

    if ($official) {
        $args += " /p:OfficialBuildId=" + $env:BUILD_BUILDNUMBER
    }

    if ($cibuild) {
        $args += " /p:ContinuousIntegrationBuild=true"
    }

    if ($bootstrapDir -ne "") {
        $args += " /p:BootstrapBuildPath=$bootstrapDir"
    }

    $args += " $buildArgs"
    $args += " $projectFilePath"
    $args += " $properties"

    if ($useDotnetBuild) {
        $args = " msbuild $args"
        Exec-Console $dotnet $args
    }
    else {
        Exec-Console $msbuild $args
    }
}

# Restore all of the projects that the repo consumes
function Restore-Packages() {
    Write-Host "Restore using dotnet at $dotnet"

    Write-Host "Restoring Roslyn Toolset"
    $logFilePath = if ($binaryLog) { Join-Path $logsDir "Restore-RoslynToolset.binlog" } else { "" }
    Restore-Project "build\ToolsetPackages\RoslynToolset.csproj" $logFilePath

    Write-Host "Restoring RepoToolset"
    $logFilePath = if ($binaryLog) { Join-Path $logsDir "Restore-RepoToolset.binlog" } else { "" }
    Run-MSBuild "build\Targets\RepoToolset\Build.proj" "/p:Restore=true /bl:$logFilePath" -summary:$false

    Write-Host "Restoring Roslyn"
    $logFilePath = if ($binaryLog) { Join-Path $logsDir "Restore-Roslyn.binlog" } else { "" }
    Restore-Project "Roslyn.sln" $logFilePath
}

# Create a bootstrap build of the compiler.  Returns the directory where the bootstrap build
# is located.
#
# Important to not set $script:bootstrapDir here yet as we're actually in the process of
# building the bootstrap.
function Make-BootstrapBuild() {
    Write-Host "Building bootstrap compiler"

    $dir = Join-Path $binariesDir "Bootstrap"
    Remove-Item -re $dir -ErrorAction SilentlyContinue
    Create-Directory $dir

    $packageName = if ($buildCoreClr) { "Microsoft.NETCore.Compilers" } else { "Microsoft.Net.Compilers" }
    $projectPath = "src\NuGet\$packageName\$packageName.Package.csproj"

    Run-MSBuild $projectPath "/t:Pack /p:DotNetUseShippingVersions=true /p:InitialDefineConstants=BOOTSTRAP /p:PackageOutputPath=$dir" -logFileName "Bootstrap" -useDotnetBuild:$buildCoreClr
    $packageFile = Get-ChildItem -Path $dir -Filter "$packageName.*.nupkg"    
    Unzip-File "$dir\$packageFile" $dir

    Write-Host "Cleaning Bootstrap compiler artifacts"
    Run-MSBuild $projectPath "/t:Clean" -logFileName "BootstrapClean"

    return $dir
}

function Build-Artifacts() {
    $args = "/t:Build"
    if ($pack) { $args += " /t:Pack" }    
    if (-not $deployExtensions) { $args += " /p:DeployExtension=false" }

    if ($buildCoreClr) {
        Run-MSBuild "Compilers.sln" $args -useDotnetBuild
    }
    elseif ($build) {
        Run-MSBuild "Roslyn.sln" $args
    }

    if ($pack) {
        Run-MSBuild "build\Targets\RepoToolset\AfterSolutionBuild.proj" "/t:Pack"
    }

    if ($sign) {
        Run-MSBuild "build\Targets\RepoToolset\Sign.proj"
    }

    if ($pack) {
        Run-MSBuild "build\Targets\RepoToolset\AfterSigning.proj" "/t:Pack"
    }

    if ($pack -and $cibuild) {
        Run-MSBuild "Roslyn.sln" "/t:DeployToSymStore" -logFileName "RoslynDeployToSymStore"
    }

    if ($build -and $pack -and (-not $buildCoreClr)) {
        if ($official){
            Build-OptProfData
        }
    }

    if ($cibuild) {
        # Symbol Uploader currently reports a warning for some files (https://github.com/dotnet/symstore/issues/76)
        Run-MSBuild "build\Targets\RepoToolset\Publish.proj" "/t:Publish" -warnAsError:$false
    }
}

function Build-OptProfData() {
    $optProfToolDir = Get-PackageDir "RoslynTools.OptProf"
    $optProfToolExe = Join-Path $optProfToolDir "tools\roslyn.optprof.exe"
    $configFile = Join-Path $RepoRoot "build\config\optprof.json"
    $insertionFolder = Join-Path $vsSetupDir "Insertion"
    $outputFolder = Join-Path $configDir "DevDivInsertionFiles\OptProf"
    Write-Host "Generating optprof data using '$configFile' into '$outputFolder'"
    $optProfArgs = "--configFile $configFile --insertionFolder $insertionFolder --outputFolder $outputFolder"
    Exec-Console $optProfToolExe $optProfArgs

    # Write Out Branch we are inserting into
    $vsBranchFolder = Join-Path $configDir "DevDivInsertionFiles\BranchInfo"
    New-Item -ItemType Directory -Force -Path $vsBranchFolder
    $vsBranchText = Join-Path $vsBranchFolder "vsbranch.txt"
    # InsertTargetBranchFullName is defined in .vsts-ci.yml
    $vsBranch = $Env:InsertTargetBranchFullName
    $vsBranch >> $vsBranchText
}

function Build-CheckLocStatus() {
    Run-MSBuild "Roslyn.sln" "/t:CheckLocStatus" -logFileName "RoslynCheckLocStatus"
}

# These are tests that don't follow our standard restore, build, test pattern. They customize
# the processes in order to test specific elements of our build and hence are handled
# separately from our other tests
function Test-Determinism() {
    $bootstrapDir = Make-BootstrapBuild
    Exec-Block { & ".\build\scripts\test-determinism.ps1" -bootstrapDir $bootstrapDir } | Out-Host
}

function Test-XUnitCoreClr() {
    $unitDir = Join-Path $configDir "UnitTests"
    $tf = "netcoreapp2.1"
    $xunitResultDir = Join-Path $unitDir "xUnitResults"
    Create-Directory $xunitResultDir
    $xunitConsole = Join-Path (Get-PackageDir "xunit.runner.console") "tools\netcoreapp2.0\xunit.console.dll"
    $runtimeVersion = Get-ToolVersion "dotnetRuntime"

    $dlls = @()
    $allGood = $true
    foreach ($dir in Get-ChildItem $unitDir) {
        $testDir = Join-Path $unitDir (Join-Path $dir $tf)
        if (Test-Path $testDir) {
            $dllName = Get-ChildItem -name "*.UnitTests.dll" -path $testDir
            $dllPath = Join-Path $testDir $dllName

            $args = "exec"
            $args += " --fx-version $runtimeVersion"
            $args += " --depsfile " + [IO.Path]::ChangeExtension($dllPath, ".deps.json")
            $args += " --runtimeconfig " + [IO.Path]::ChangeExtension($dllPath, ".runtimeconfig.json")
            $args += " $xunitConsole"
            $args += " $dllPath"
            $args += " -xml " + (Join-Path $xunitResultDir ([IO.Path]::ChangeExtension($dllName, ".xml")))

            # https://github.com/dotnet/roslyn/issues/25049
            # Disable parallel runs everywhere until we get assembly specific settings working again
            $args += " -parallel none"

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

    # Used by tests to locate dotnet CLI
    $env:DOTNET_INSTALL_DIR = Split-Path $dotnet -Parent

    if ($testCoreClr) {
        Test-XUnitCoreClr
        return
    }

    if ($testVsi -or $testVsiNetCore) {
        Deploy-VsixViaTool
    }

    if ($testIOperation) {
        $env:ROSLYN_TEST_IOPERATION = "true"
    }

    $unitDir = Join-Path $configDir "UnitTests"
    $runTests = Join-Path $configDir "Exes\RunTests\RunTests.exe"
    $xunitDir = Join-Path (Get-PackageDir "xunit.runner.console") "tools\net472"
    $args = "$xunitDir"
    $args += " -logpath:$logsDir"
    $args += " -nocache"

    if ($testDesktop -or $testIOperation) {
        if ($test32) {
            $dlls = Get-ChildItem -re -in "*.UnitTests.dll" $unitDir
        }
        else {
            $dlls = Get-ChildItem -re -in "*.UnitTests.dll" -ex "*InteractiveHost*" $unitDir
        }
    }
    elseif ($testVsi) {
        # Since they require Visual Studio to be installed, ensure that the MSBuildWorkspace tests run along with our VS
        # integration tests in CI.
        if ($cibuild) {
            $dlls += @(Get-Item (Join-Path $unitDir "Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests\Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests.dll"))
        }

        $dlls += @(Get-ChildItem -re -in "*.IntegrationTests.dll" $unitDir)
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
        # Use a 75 minute timeout on CI
        $args += " -xml -timeout:75"
    }

    if ($procdump) {
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
        if ($testIOperation) {
            Remove-Item env:\ROSLYN_TEST_IOPERATION
        }
    }
}

# Deploy our core VSIX libraries to Visual Studio via the Roslyn VSIX tool.  This is an alternative to
# deploying at build time.
function Deploy-VsixViaTool() { 
    $vsixDir = Get-PackageDir "RoslynTools.VSIXExpInstaller"
    $vsixExe = Join-Path $vsixDir "tools\VsixExpInstaller.exe"
    $both = Get-VisualStudioDirAndId
    $vsDir = $both[0].Trim("\")
    $vsId = $both[1]
    $hive = "RoslynDev"
    Write-Host "Using VS Instance $vsId at `"$vsDir`""
    $baseArgs = "/rootSuffix:$hive /vsInstallDir:`"$vsDir`""

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

    # VSIX files need to be installed in this specific order:
    $orderedVsixFileNames = @(	
        "Roslyn.Compilers.Extension.vsix",
        "Roslyn.VisualStudio.Setup.vsix",
        "Roslyn.VisualStudio.Setup.Dependencies.vsix",
        "Roslyn.VisualStudio.InteractiveComponents.vsix",
        "ExpressionEvaluatorPackage.vsix",
        "Roslyn.VisualStudio.DiagnosticsWindow.vsix",
        "Microsoft.VisualStudio.IntegrationTest.Setup.vsix")

    foreach ($vsixFileName in $orderedVsixFileNames) {
        $vsixFile = Join-Path $vsSetupDir $vsixFileName
        $fullArg = "$baseArgs $vsixFile"
        Write-Host "`tInstalling $vsixFileName"
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
    Copy-Item (Join-Path $RepoRoot "src\Workspaces\CoreTestUtilities\Resources\.editorconfig") $temp
    Copy-Item (Join-Path $RepoRoot "src\Workspaces\CoreTestUtilities\Resources\Directory.Build.props") $temp
    Copy-Item (Join-Path $RepoRoot "src\Workspaces\CoreTestUtilities\Resources\Directory.Build.targets") $temp
    Copy-Item (Join-Path $RepoRoot "src\Workspaces\CoreTestUtilities\Resources\Directory.Build.rsp") $temp
    Copy-Item (Join-Path $RepoRoot "src\Workspaces\CoreTestUtilities\Resources\NuGet.Config") $temp
    ${env:TEMP} = $temp
    ${env:TMP} = $temp
}

function List-BuildProcesses() {
    Write-Host "Listing running build processes..."
    Get-Process -Name "msbuild" -ErrorAction SilentlyContinue | Out-Host
    Get-Process -Name "vbcscompiler" -ErrorAction SilentlyContinue | Out-Host
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | where { $_.Modules | select { $_.ModuleName -eq "VBCSCompiler.dll" } } | Out-Host
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
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | where { $_.Modules | select { $_.ModuleName -eq "VBCSCompiler.dll" } } | Stop-Process
}

# Kill any instances of devenv.exe to ensure VSIX install/uninstall works in future runs and to ensure
# that any locked files don't prevent future CI runs from failing.
# Also call Stop-BuildProcesses
function Stop-VSProcesses() {
    Write-Host "Killing running vs processes..."
    Get-Process -Name "devenv" -ErrorAction SilentlyContinue | Stop-Process
}

try {
    Process-Arguments

   . (Join-Path $PSScriptRoot "build-utils.ps1")

    Push-Location $RepoRoot

    Write-Host "Repo Dir $RepoRoot"
    Write-Host "Binaries Dir $binariesDir"

    $msbuild = Ensure-MSBuild
    $dotnet = Ensure-DotnetSdk
    $configDir = Join-Path $binariesDir $configuration
    $vsSetupDir = Join-Path $binariesDir (Join-Path "VSSetup" $configuration)
    $logsDir = Join-Path $configDir "Logs"
    $bootstrapDir = ""

    # Ensure the main output directories exist as a number of tools will fail when they don't exist.
    Create-Directory $binariesDir
    Create-Directory $configDir
    Create-Directory $logsDir

    if ($cibuild) {
        List-VSProcesses
        List-BuildProcesses
        Redirect-Temp
    }

    if ($restore) {
        Write-Host "Running restore"
        Restore-Packages
    }

    if ($testDeterminism) {
        Test-Determinism
        exit 0
    }

    if ($bootstrap) {
        $bootstrapDir = Make-BootstrapBuild
    }

    if ($build -or $pack) {
        Build-Artifacts
    }

    if ($checkLoc) {
        Build-CheckLocStatus
    }

    if ($testDesktop -or $testCoreClr -or $testVsi -or $testVsiNetCore -or $testIOperation) {
        Test-XUnit
    }

    if ($launch) {
        $devenvExe = Get-VisualStudioDir
        $devenvExe = Join-Path $devenvExe 'Common7\IDE\devenv.exe'
        &$devenvExe /rootSuffix RoslynDev
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
    if ($cibuild -and -not $official) {
        Stop-VSProcesses
        Stop-BuildProcesses
    }
}