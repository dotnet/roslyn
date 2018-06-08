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
    [switch]$buildCoreClr = $false,
    [switch]$bootstrap = $false,
    [switch]$sign = $false,
    [switch]$pack = $false,
    [switch]$packAll = $false,
    [switch]$binaryLog = $false,
    [switch]$deployExtensions = $false,
    [switch]$launch = $false,
    [switch]$procdump = $false,
    [string]$signType = "",
    [switch]$skipBuildExtras = $false,
    [switch]$skipAnalyzers = $false,

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

    [parameter(ValueFromRemainingArguments=$true)] $badArgs)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
    Write-Host "Usage: build.ps1"
    Write-Host "  -release                  Perform release build (default is debug)"
    Write-Host "  -restore                  Restore packages"
    Write-Host "  -build                    Build Roslyn.sln"
    Write-Host "  -official                 Perform an official build"
    Write-Host "  -bootstrap                Build using a bootstrap Roslyn"
    Write-Host "  -sign                     Sign our binaries"
    Write-Host "  -signType                 Type of sign: real, test, verify"
    Write-Host "  -pack                     Create our NuGet packages"
    Write-Host "  -deployExtensions         Deploy built vsixes"
    Write-Host "  -binaryLog                Create binary log for every MSBuild invocation"
    Write-Host "  -procdump                 Monitor test runs with procdump"
    Write-Host "  -skipAnalyzers            Do not run analyzers during build operations"
    Write-Host "  -skipBuildExtras          Do not build insertion items"
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

    if (($cibuild -and $anyVsi)) {
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

    $script:pack = $pack -or $packAll
    $script:packAll = $packAll -or ($pack -and $official)

    if ($buildCoreClr) {
        $script:build = $true
    }

    $script:test32 = -not $test64
    $script:debug = -not $release
}

function Run-MSBuild([string]$projectFilePath, [string]$buildArgs = "", [string]$logFileName = "", [switch]$parallel = $true, [switch]$useDotnetBuild = $false, [switch]$summary = $true) {
    # Because we override the C#/VB toolset to build against our LKG package, it is important
    # that we do not reuse MSBuild nodes from other jobs/builds on the machine. Otherwise,
    # we'll run into issues such as https://github.com/dotnet/roslyn/issues/6211.
    # MSBuildAdditionalCommandLineArgs=
    $args = "/p:TreatWarningsAsErrors=true /warnaserror /nologo /nodeReuse:false /p:Configuration=$buildConfiguration";

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
        $args += " /p:OfficialBuild=true"
    }

    if ($bootstrapDir -ne "") {
        $args += " /p:BootstrapBuildPath=$bootstrapDir"
    }

    $args += " $buildArgs"
    $args += " $projectFilePath"

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
    Restore-Project $dotnet "build\ToolsetPackages\RoslynToolset.csproj" $logFilePath

    Write-Host "Restoring RepoToolset"
    $logFilePath = if ($binaryLog) { Join-Path $logsDir "Restore-RepoToolset.binlog" } else { "" }
    Run-MSBuild "build\Targets\RepoToolset\Build.proj" "/p:Restore=true /bl:$logFilePath" -summary:$false

    Write-Host "Restoring Roslyn"
    $logFilePath = if ($binaryLog) { Join-Path $logsDir "Restore-Roslyn.binlog" } else { "" }
    Restore-Project $dotnet "Roslyn.sln" $logFilePath
}

# Create a bootstrap build of the compiler.  Returns the directory where the bootstrap build
# is located.
#
# Important to not set $script:bootstrapDir here yet as we're actually in the process of
# building the bootstrap.
function Make-BootstrapBuild() {
    $dir = Join-Path $binariesDir "Bootstrap"
    Write-Host "Building Bootstrap compiler"
    $bootstrapArgs = "/p:UseShippingAssemblyVersion=true /p:InitialDefineConstants=BOOTSTRAP"
    Remove-Item -re $dir -ErrorAction SilentlyContinue
    Create-Directory $dir
    if ($buildCoreClr) {
        $bootstrapFramework = "netcoreapp2.0"
        $projectFiles = @(
            'src/Compilers/CSharp/csc/csc.csproj',
            'src/Compilers/VisualBasic/vbc/vbc.csproj',
            'src/Compilers/Server/VBCSCompiler/VBCSCompiler.csproj',
            'src/Compilers/Core/MSBuildTask/MSBuildTask.csproj'
        )

        foreach ($projectFilePath in $projectFiles) {
            $fileName = [IO.Path]::GetFileNameWithoutExtension((Split-Path -leaf $projectFilePath))
            $logFileName = "Bootstrap$($fileName)"
            Run-MSBuild $projectFilePath "/t:Publish /p:TargetFramework=netcoreapp2.0 $bootstrapArgs" -logFileName $logFileName -useDotnetBuild
        }

        Pack-One "Microsoft.NetCore.Compilers.nuspec" "Bootstrap" $dir
        Unzip-File "$dir\Microsoft.NETCore.Compilers.42.42.42.42-bootstrap.nupkg" "$dir\Microsoft.NETCore.Compilers\42.42.42.42"

        Write-Host "Cleaning Bootstrap compiler artifacts"
        Run-MSBuild "Compilers.sln" "/t:Clean"
        Stop-BuildProcesses
    }
    else {
        Run-MSBuild "build\Toolset\Toolset.csproj" $bootstrapArgs -logFileName "Bootstrap"
        Remove-Item -re $dir -ErrorAction SilentlyContinue
        Create-Directory $dir

        Pack-One "Microsoft.Net.Compilers.nuspec" "Bootstrap" $dir
        Unzip-File "$dir\Microsoft.Net.Compilers.42.42.42.42-bootstrap.nupkg" "$dir\Microsoft.Net.Compilers\42.42.42.42"

        Write-Host "Cleaning Bootstrap compiler artifacts"
        Run-MSBuild "build\Toolset\Toolset.csproj" "/t:Clean" -logFileName "BootstrapClean"
        Stop-BuildProcesses
    }

    return $dir
}

function Build-Artifacts() {
    if ($buildCoreClr) {
        Run-MSBuild "Compilers.sln" -useDotnetBuild
    }
    elseif ($build) {
        Run-MSBuild "Roslyn.sln" $(if (-not $deployExtensions) {"/p:DeployExtension=false"})
        if (-not $skipBuildExtras) {
            Build-ExtraSignArtifacts
        }
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

    if ($build -and (-not $skipBuildExtras) -and (-not $buildCoreClr)) {
        Build-InsertionItems
        Build-Installer
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
        Write-Host "Publishing vbc"
        Run-MSBuild "..\Compilers\VisualBasic\vbc\vbc.csproj" "/p:TargetFramework=netcoreapp2.0 /t:PublishWithoutBuilding"
        Write-Host "Publishing VBCSCompiler"
        Run-MSBuild "..\Compilers\Server\VBCSCompiler\VBCSCompiler.csproj" "/p:TargetFramework=netcoreapp2.0 /t:PublishWithoutBuilding"
        Write-Host "Publishing MSBuildTask"
        Run-MSBuild "..\Compilers\Core\MSBuildTask\MSBuildTask.csproj" "/p:TargetFramework=netcoreapp2.0 /t:PublishWithoutBuilding"
        Write-Host "Building PortableFacades Swix"
        Run-MSBuild "DevDivVsix\PortableFacades\PortableFacades.swixproj"
        Write-Host "Building CompilersCodeAnalysis Swix"
        Run-MSBuild "DevDivVsix\CompilersPackage\Microsoft.CodeAnalysis.Compilers.swixproj"

        $dest = @($configDir)
        foreach ($dir in $dest) {
            Copy-Item "PowerShell\*.ps1" $dir
        }

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
        Exec-Console (Join-Path $configDir "Exes\DevDivInsertionFiles\Roslyn.BuildDevDivInsertionFiles.exe") "$configDir $repoDir $(Get-PackagesDir)"

        # In non-official builds need to supply values for a few MSBuild properties. The actual value doesn't
        # matter, just that it's provided some value.
        $extraArgs = ""
        if (-not $official) {
            $extraArgs = " /p:FinalizeValidate=false /p:ManifestPublishUrl=https://vsdrop.corp.microsoft.com/file/v1/Products/DevDiv/dotnet/roslyn/master/20160729.6"
        }

        $insertionDir = Join-Path $configDir "DevDivInsertionFiles"
        $vsToolsDir = Join-Path $insertionDir "VS.Tools.Roslyn"
        $packageOutDir = Join-Path $configDir "DevDivPackages\Roslyn"
        $packArgs = "/p:NoPackageAnalysis=true"
        Create-Directory $packageOutDir
        Pack-One (Join-Path $insertionDir "VS.ExternalAPIs.Roslyn.nuspec") "PerBuildPreRelease" $packageOutDir $packArgs
        Pack-One (Join-Path $vsToolsDir "VS.Tools.Roslyn.nuspec") "PerBuildPreRelease" $packageOutDir $packArgs -basePath $vsToolsDir

        $netfx20Dir = Join-Path $repoDir "src\Dependencies\Microsoft.NetFX20"
        Pack-One (Join-Path $netfx20Dir "Microsoft.NetFX20.nuspec") "PerBuildPreRelease" -packageOutDir (Join-Path $configDir "NuGet\NetFX20") -basePath $netfx20Dir -extraArgs "$packArgs /p:CurrentVersion=4.3.0"

        Run-MSBuild "DevDivVsix\PortableFacades\PortableFacades.vsmanproj" -buildArgs $extraArgs
        Run-MSBuild "DevDivVsix\CompilersPackage\Microsoft.CodeAnalysis.Compilers.vsmanproj" -buildArgs $extraArgs
        Run-MSBuild "DevDivVsix\MicrosoftCodeAnalysisLanguageServices\Microsoft.CodeAnalysis.LanguageServices.vsmanproj" -buildArgs "$extraArgs"
    }
    finally {
        Pop-Location
    }
}

function Build-Installer () {
    #  Copying Artifacts
    $installerDir = Join-Path $configDir "Installer"
    Create-Directory $installerDir

    $intermidateDirectory = Join-Path $env:TEMP "InstallerTemp"
    if(Test-Path $intermidateDirectory)
    {
        Remove-Item -Path $intermidateDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $intermidateDirectory

    ## Copying VsixExpInstaller.exe
    $vsixExpInstallerDir = Get-PackageDir "RoslynTools.Microsoft.VSIXExpInstaller"
    $vsixExpInstallerExe = Join-Path $vsixExpInstallerDir "tools\*"
    $vsixExpInstallerExeDestination = Join-Path $intermidateDirectory "tools\vsixexpinstaller"
    Create-Directory $vsixExpInstallerExeDestination
    Copy-Item $vsixExpInstallerExe -Destination $vsixExpInstallerExeDestination -Recurse

    ## Copying VsWhere.exe
    $vswhere = Join-Path (Ensure-BasicTool "vswhere") "tools\*"
    $vswhereDestination = Join-Path $intermidateDirectory "tools\vswhere"
    Create-Directory $vswhereDestination
    Copy-Item $vswhere -Destination $vswhereDestination -Recurse

    ## Copying scripts
    $installerScriptsFolder = Join-Path $repoDir "src\Setup\InstallerScripts\*.bat"
    Copy-Item $installerScriptsFolder -Destination $intermidateDirectory -Recurse

    $installerScriptsFolder = Join-Path $repoDir "src\Setup\InstallerScripts\tools\*.ps1"
    $intermidatePowershellScriptsDirectory = Join-Path $intermidateDirectory "tools"
    Copy-Item $installerScriptsFolder -Destination $intermidatePowershellScriptsDirectory -Recurse

    ## Copying VSIXes
    $vsixDir = Join-Path $configDir "Vsix"
    $vsixDirDestination = Join-Path $intermidateDirectory "vsix"
    if (-not (Test-Path $vsixDirDestination)) {
        New-Item -ItemType Directory -Force -Path $vsixDirDestination
    }
    $RoslynDeploymentVsix = Join-Path $vsixDir "Roslyn\RoslynDeployment.vsix"
    Copy-Item $RoslynDeploymentVsix -Destination $vsixDirDestination

    #  Zip Folder
    $installerZip = Join-Path $installerDir "Roslyn_Preview"
    $intermidateDirectory = Join-Path $intermidateDirectory "*"
    Compress-Archive -Path $intermidateDirectory -DestinationPath $installerZip
}

function Pack-One([string]$nuspecFilePath, [string]$packageKind, [string]$packageOutDir = "", [string]$extraArgs = "", [string]$basePath = "", [switch]$useConsole = $true) {
    $nugetDir = Join-Path $repoDir "src\Nuget"
    if ($packageOutDir -eq "") {
        $packageOutDir = Join-Path $configDir "NuGet\$packageKind"
    }

    if ($basePath -eq "") {
        $basePath = $configDir
    }

    if (-not ([IO.Path]::IsPathRooted($nuspecFilePath))) {
        $nuspecFilePath = Join-Path $nugetDir $nuspecFilePath
    }

    Create-Directory $packageOutDir
    $nuspecFileName = Split-Path -leaf $nuspecFilePath
    $projectFilePath = Join-Path $nugetDir "NuGetProjectPackUtil.csproj"
    $packArgs = "pack -nologo --no-build $projectFilePath $extraArgs /p:NugetPackageKind=$packageKind /p:NuspecFile=$nuspecFilePath /p:NuspecBasePath=$basePath -o $packageOutDir"

    if ($official) {
        $packArgs = "$packArgs /p:OfficialBuild=true"
    }

    if ($useConsole) {
        Exec-Console $dotnet $packArgs
    }
    else {
        Exec-Command $dotnet $packArgs
    }
}

function Build-NuGetPackages() {

    function Pack-All([string]$packageKind, $extraArgs) {

        Write-Host "Packing for $packageKind"
        foreach ($item in Get-ChildItem *.nuspec) {
            $name = Split-Path -leaf $item
            Pack-One $name $packageKind -extraArgs $extraArgs
        }
    }

    Push-Location (Join-Path $repoDir "src\NuGet")
    try {
        $extraArgs = ""

        if ($official) {
            $extraArgs += " /p:UseRealCommit=true"
        }

        # Empty directory for packing explicit empty items in the nuspec
        $emptyDir = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName())
        Create-Directory $emptyDir
        New-Item -Path (Join-Path $emptyDir "_._") -Type File | Out-Null
        $extraArgs += " /p:EmptyDir=$emptyDir"

        Pack-All "PreRelease" $extraArgs
        if ($packAll) {
            Pack-All "Release" $extraArgs
            Pack-All "PerBuildPreRelease" $extraArgs
        }
    }
    finally {
        Pop-Location
    }
}

function Build-DeployToSymStore() {
    Run-MSBuild "Roslyn.sln" "/t:DeployToSymStore" -logFileName "RoslynDeployToSymStore"
}

# These are tests that don't follow our standard restore, build, test pattern. They customize
# the processes in order to test specific elements of our build and hence are handled
# separately from our other tests
function Test-Determinism() {
    $bootstrapDir = Make-BootstrapBuild
    Exec-Block { & ".\build\scripts\test-determinism.ps1" -bootstrapDir $bootstrapDir } | Out-Host
}

function Test-XUnitCoreClr() {
    Write-Host "Publishing ILAsm.csproj"
    $toolsDir = Join-Path $binariesDir "Tools"
    $ilasmDir = Join-Path $toolsDir "ILAsm"
    Exec-Console $dotnet "publish src\Tools\ILAsm --no-restore --runtime win-x64 --self-contained -o $ilasmDir"

    $unitDir = Join-Path $configDir "UnitTests"
    $tf = "netcoreapp2.0"
    $xunitResultDir = Join-Path $unitDir "xUnitResults"
    Create-Directory $xunitResultDir
    $xunitConsole = Join-Path (Get-PackageDir "xunit.runner.console") "tools\$tf\xunit.console.dll"

    $dlls = @()
    $allGood = $true
    foreach ($dir in Get-ChildItem $unitDir) {
        $testDir = Join-Path $unitDir (Join-Path $dir $tf)
        if (Test-Path $testDir) {
            $dllName = Get-ChildItem -name "*.UnitTests.dll" -path $testDir
            $dllPath = Join-Path $testDir $dllName

            $args = "exec"
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
    $xunitDir = Join-Path (Get-PackageDir "xunit.runner.console") "tools\net452"
    $args = "$xunitDir"
    $args += " -logpath:$logsDir"
    $args += " -nocache"

    if ($testDesktop -or $testIOperation) {
        if ($test32) {
            $dlls = Get-ChildItem -re -in "*.UnitTests.dll" $unitDir
        }
        else {
            $dlls = Get-ChildItem -re -in "*.UnitTests.dll" -ex "*Roslyn.Interactive*" $unitDir
        }
    }
    elseif ($testVsi) {
        # Since they require Visual Studio to be installed, ensure that the MSBuildWorkspace tests run along with our VS
        # integration tests in CI.
        if ($cibuild) {
            $dlls += @(Get-Item (Join-Path $unitDir "Workspaces.MSBuild.Test\Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests.dll"))
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

    if ($cibuild -or $official) {
        # Use a 50 minute timeout on CI
        $args += " -xml -timeout:50"
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
    $vsixDir = Get-PackageDir "RoslynTools.Microsoft.VSIXExpInstaller"
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
        $signTool = Join-Path (Get-PackageDir "RoslynTools.SignTool") "tools\SignTool.exe"
        $signToolArgs = "-msbuildPath `"$msbuild`""
        if ($binaryLog) {
            $signToolArgs += " -msbuildBinaryLog $logsDir\Signing.binlog"
        }
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
    Copy-Item (Join-Path $repoDir "src\Workspaces\CoreTestUtilities\Resources\.editorconfig") $temp
    Copy-Item (Join-Path $repoDir "src\Workspaces\CoreTestUtilities\Resources\Directory.Build.props") $temp
    Copy-Item (Join-Path $repoDir "src\Workspaces\CoreTestUtilities\Resources\Directory.Build.targets") $temp
    Copy-Item (Join-Path $repoDir "src\Workspaces\CoreTestUtilities\Resources\Directory.Build.rsp") $temp
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
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $repoDir

    Write-Host "Repo Dir $repoDir"
    Write-Host "Binaries Dir $binariesDir"

    Process-Arguments

    $msbuild = Ensure-MSBuild
    $dotnet = Ensure-DotnetSdk
    $buildConfiguration = if ($release) { "Release" } else { "Debug" }
    $configDir = Join-Path $binariesDir $buildConfiguration
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
    if ($cibuild) {
        Stop-VSProcesses
        Stop-BuildProcesses
    }
}
