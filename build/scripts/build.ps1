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
    [string]$msbuildEngine = "vs",

    # Configuration
    [switch]$restore,
    [switch]$official,
    [switch]$cibuild,
    [switch]$build,
    [switch]$bootstrap,
    [switch]$sign,
    [switch]$pack,
    [switch]$binaryLog,
    [switch]$deployExtensions,
    [switch]$launch,
    [switch]$procdump,
    [switch]$skipAnalyzers,

    # Test options
    [switch]$test32,
    [switch]$test64,
    [switch]$testVsi,
    [switch]$testVsiNetCore,
    [switch]$testDesktop,
    [switch]$testCoreClr,
    [switch]$testIOperation,

    [switch]$help,
    [parameter(ValueFromRemainingArguments=$true)][string[]]$properties)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
    Write-Host "Usage: build.ps1"
    Write-Host "  -configuration <value>    Build configuration ('Debug' or 'Release')"
    Write-Host "  -msbuildEngine <value>    Msbuild engine to use to run build ('dotnet', 'vs', or unspecified)."
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
    Write-Host ""
    Write-Host "Test options"
    Write-Host "  -test32                   Run unit tests in the 32-bit runner"
    Write-Host "  -test64                   Run units tests in the 64-bit runner"
    Write-Host "  -testDesktop              Run desktop unit tests"
    Write-Host "  -testCoreClr              Run CoreClr unit tests"
    Write-Host "  -testVsi                  Run all integration tests"
    Write-Host "  -testVsiNetCore           Run just dotnet core integration tests"
    Write-Host "  -testIOperation           Run extra checks to validate IOperations"
}

if ($help -or (($properties -ne $null) -and ($properties.Contains("/help") -or $properties.Contains("/?")))) {
  Print-Usage
  exit 0
}

# Process the command line arguments and establish defaults for the values which are not
# specified.
#
# In this function it's okay to use two arguments to extend the effect of another. For
# example it's okay to look at $anyVsi and infer $skipAnalyzers. It's not okay though to infer
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

    if ($anyVsi) {
        # Avoid spending time in analyzers when requested, and also in the slowest integration test builds
        $script:skipAnalyzers = $true
    }

    if ($build -and $launch -and -not $deployExtensions) {
        Write-Host -ForegroundColor Red "Cannot combine -build and -launch without -deployExtensions"
        exit 1
    }

    $script:test32 = -not $test64
}

# Restore all of the projects that the repo consumes
function Restore-Packages() {
    Write-Host "Restoring Roslyn Toolset"
    $logFilePath = if ($binaryLog) { Join-Path $LogDir "Restore-RoslynToolset.binlog" } else { "" }
    Restore-Project "build\ToolsetPackages\RoslynToolset.csproj" $logFilePath

    Write-Host "Restoring RepoToolset"
    $logFilePath = if ($binaryLog) { Join-Path $LogDir "Restore-RepoToolset.binlog" } else { "" }
    Run-MSBuild "build\Targets\RepoToolset\Build.proj" "/p:Restore=true /bl:$logFilePath" -summary:$false

    Write-Host "Restoring Roslyn"
    $logFilePath = if ($binaryLog) { Join-Path $LogDir "Restore-Roslyn.binlog" } else { "" }
    Restore-Project "Roslyn.sln" $logFilePath
}

function Build-Artifacts() {
    $args = "/t:Build"
    if ($pack) { $args += " /t:Pack" }    
    if (-not $deployExtensions) { $args += " /p:DeployExtension=false" }

    # Roslyn.sln can't be built with dotnet due to WPF and VSIX build task dependencies
    $solution = if ($msbuildEngine -eq 'dotnet') { "Compilers.sln" } else { "Roslyn.sln" }

    if ($build) {
        Run-MSBuild $solution $args
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
        Run-MSBuild $solution "/t:DeployToSymStore" -logFileName "RoslynDeployToSymStore"
    }

    if ($build -and $pack -and $official) {
        Build-OptProfData
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
    $insertionFolder = Join-Path $VSSetupDir "Insertion"
    $outputFolder = Join-Path $BinariesConfigDir "DevDivInsertionFiles\OptProf"
    Write-Host "Generating optprof data using '$configFile' into '$outputFolder'"
    $optProfArgs = "--configFile $configFile --insertionFolder $insertionFolder --outputFolder $outputFolder"
    Exec-Console $optProfToolExe $optProfArgs

    # Write Out Branch we are inserting into
    $vsBranchFolder = Join-Path $BinariesConfigDir "DevDivInsertionFiles\BranchInfo"
    New-Item -ItemType Directory -Force -Path $vsBranchFolder
    $vsBranchText = Join-Path $vsBranchFolder "vsbranch.txt"
    # InsertTargetBranchFullName is defined in .vsts-ci.yml
    $vsBranch = $Env:InsertTargetBranchFullName
    $vsBranch >> $vsBranchText
}

function Test-XUnitCoreClr() {
    $unitDir = Join-Path $BinariesConfigDir "UnitTests"
    $tf = "netcoreapp2.1"
    $xunitResultDir = Join-Path $unitDir "xUnitResults"
    Create-Directory $xunitResultDir
    $xunitConsole = Join-Path (Get-PackageDir "xunit.runner.console") "tools\netcoreapp2.0\xunit.console.dll"
    $runtimeVersion = Get-ToolVersion "dotnetRuntime"
    $dotnet = Ensure-DotnetSdk

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

    # Tests need to locate .NET Core SDK
    InitializeDotNetCli

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

    $unitDir = Join-Path $BinariesConfigDir "UnitTests"
    $runTests = Join-Path $BinariesConfigDir "Exes\RunTests\RunTests.exe"
    $xunitDir = Join-Path (Get-PackageDir "xunit.runner.console") "tools\net472"
    $args = "$xunitDir"
    $args += " -logpath:$LogDir"
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
    
    $vsInfo = LocateVisualStudio
    if ($vsInfo -eq $null) {
        throw "Unable to locate required Visual Studio installation"
    }

    $vsDir = $vsInfo.installationPath.TrimEnd("\")
    $vsId = $vsInfo.instanceId
    $vsMajorVersion = $vsInfo.installationVersion.Split('.')[0]

    $hive = "RoslynDev"
    Write-Host "Using VS Instance $vsId at `"$vsDir`""
    $baseArgs = "/rootSuffix:$hive /vsInstallDir:`"$vsDir`""

    Write-Host "Uninstalling old Roslyn VSIX"

    # Actual uninstall is failing at the moment using the uninstall options. Temporarily using
    # wildfire to uninstall our VSIX extensions
    $extDir = Join-Path ${env:USERPROFILE} "AppData\Local\Microsoft\VisualStudio\$vsMajorVersion.0_$vsid$hive"
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
        $vsixFile = Join-Path $VSSetupDir $vsixFileName
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
    if (Test-Path "C:\SysInternals\procdump.exe") {
        return "C:\SysInternals";
    }

    $outDir = Join-Path $ToolsDir "ProcDump"
    $filePath = Join-Path $outDir "procdump.exe"
    if (-not (Test-Path $filePath)) {
        Remove-Item -Re $filePath -ErrorAction SilentlyContinue
        Create-Directory $outDir
        $zipFilePath = Join-Path $toolsDir "procdump.zip"
        Invoke-WebRequest "https://download.sysinternals.com/files/Procdump.zip" -UseBasicParsing -outfile $zipFilePath | Out-Null
        Unzip $zipFilePath $outDir
    }

    return $outDir
}

# The Jenkins images used to execute our tests can live for a very long time.  Over the course
# of hundreds of runs this can cause the %TEMP% folder to fill up.  To avoid this we redirect
# %TEMP% into the binaries folder which is deleted at the end of every run as a part of cleaning
# up the workspace.
function Redirect-Temp() {
    Create-Directory $TempDir
    Copy-Item (Join-Path $RepoRoot "src\Workspaces\CoreTestUtilities\Resources\.editorconfig") $TempDir
    Copy-Item (Join-Path $RepoRoot "src\Workspaces\CoreTestUtilities\Resources\Directory.Build.props") $TempDir
    Copy-Item (Join-Path $RepoRoot "src\Workspaces\CoreTestUtilities\Resources\Directory.Build.targets") $TempDir
    Copy-Item (Join-Path $RepoRoot "src\Workspaces\CoreTestUtilities\Resources\Directory.Build.rsp") $TempDir
    Copy-Item (Join-Path $RepoRoot "src\Workspaces\CoreTestUtilities\Resources\NuGet.Config") $TempDir
    ${env:TEMP} = $TempDir
    ${env:TMP} = $TempDir
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

    Create-Directory $ArtifactsDir
    Create-Directory $BinariesConfigDir
    Create-Directory $LogDir

    if ($cibuild) {
        List-VSProcesses
        List-BuildProcesses
        Redirect-Temp
    }

    if ($restore) {
        Write-Host "Running restore"
        Restore-Packages
    }

    if ($bootstrap) {
        $bootstrapDir = Make-BootstrapBuild
    }

    if ($build -or $pack) {
        Build-Artifacts
    }

    if ($testDesktop -or $testCoreClr -or $testVsi -or $testVsiNetCore -or $testIOperation) {
        Test-XUnit
    }

    if ($launch) {
        $devenvExe = Join-Path $env:VSINSTALLDIR 'Common7\IDE\devenv.exe'
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