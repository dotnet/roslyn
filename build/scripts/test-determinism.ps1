[CmdletBinding(PositionalBinding=$false)]
param ( [string]$bootstrapDir = "",
        [switch]$debugDeterminism = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

### Variables available to the entire script.

# List of binary names that should be skipped because they have a known issue that
# makes them non-deterministic.  
$script:skipList = @()

# Location that deterministic error information should be written to. 
[string]$script:errorDir = ""
[string]$script:errorDirLeft = ""
[string]$script:errorDirRight = ""

function Run-Build([string]$rootDir, [string]$pathMapBuildOption, [switch]$restore = $false) {
    Push-Location $rootDir
    try {

        # Clean out the previous run
        Write-Host "Cleaning the Binaries"
        Exec-Console $msbuild "/nologo /v:m /nodeReuse:false /t:clean Roslyn.sln" 

        if ($restore) {
            Write-Host "Restoring the packages"
            Restore-Project -fileName "Roslyn.sln" -nuget (Ensure-NuGet) -msbuildDir (Split-Path -parent $msbuild)
        }

        Write-Host "Building the Solution"
        Exec-Console $msbuild "/nologo /v:m /nodeReuse:false /m /p:DebugDeterminism=true /p:BootstrapBuildPath=$script:bootstrapDir /p:Features=`"debug-determinism`" /p:UseRoslynAnalyzers=false $pathMapBuildOption Roslyn.sln"
    }
    finally {
        Pop-Location
    }
}

function Get-ObjDir([string]$rootDir) { 
    return Join-Path $rootDir "Binaries\Obj"
}

# Return all of the files that need to be processed for determinism under the given
# directory.
function Get-FilesToProcess([string]$rootDir) {
    $objDir = Get-ObjDir $rootDir
    foreach ($item in Get-ChildItem -re -in *.dll,*.exe $objDir) {
        $fileFullName = $item.FullName 
        $fileName = Split-Path -leaf $fileFullName

        if ($skipList.Contains($fileName)) {
            continue;
        }

        $fileId = $fileFullName.Substring($objDir.Length).Replace("\", ".")
        $fileHash = (Get-FileHash $fileFullName -algorithm MD5).Hash

        $data = @{}
        $data.Hash = $fileHash
        $data.Content = [IO.File]::ReadAllBytes($fileFullName)
        $data.FileId = $fileId
        $data.FileName = $fileName
        $data.FileFullName = $fileFullName
        Write-Output $data
    }
}

# This will build up the map of all of the binaries and their respective hashes.
function Record-Binaries([string]$rootDir) {
    Write-Host "Recording file hashes"

    $map = @{ }
    foreach ($fileData in Get-FilesToProcess $rootDir) { 
        Write-Host "`t$($fileData.FileName) = $($fileData.Hash)"
        $map[$fileData.FileId] = $fileData
    }
    return $map
}

# This is a sanity check to ensure that we're actually putting the right entries into
# the core data map. Essentially to ensure things like if we change our directory layout 
# that this test fails beacuse we didn't record the binaries we intended to record. 
function Test-MapContents($dataMap) { 

    # Sanity check to ensure we didn't return a false positive because we failed
    # to examine any binaries.
    if ($dataMap.Count -lt 40) {
        throw "Didn't find the expected count of binaries"
    }

    # Test for some well known binaries
    $list = @(
        "Microsoft.CodeAnalysis.dll",
        "Microsoft.CodeAnalysis.CSharp.dll",
        "Microsoft.CodeAnalysis.Workspaces.dll",
        "Microsoft.VisualStudio.LanguageServices.Implementation.dll")

    foreach ($fileName in $list) { 
        $found = $false
        foreach ($value in $dataMap.Values) { 
            if ($value.FileName -eq $fileName) { 
                $found = $true
                break;
            }
        }

        if (-not $found) { 
            throw "Did not find the expected binary $fileName"
        }
    }
}

function Test-Build([string]$rootDir, $dataMap, [string]$pathMapBuildOption, [switch]$restore = $false) {
    Run-Build $rootDir $pathMapBuildOption -restore:$restore

    $errorList = @()
    $allGood = $true

    Write-Host "Testing the binaries"
    foreach ($fileData in Get-FilesToProcess $rootDir) {
        $fileId = $fileData.FileId
        $fileName = $fileData.FileName
        $fileFullName = $fileData.FileFullName

        if (-not $dataMap.Contains($fileId)) {
            Write-Host "ERROR! Missing entry in map $fileId->$fileFullName"
            $allGood = $false
            continue
        }

        $oldfileData = $datamap[$fileId]
        if ($fileData.Hash -ne $oldFileData.Hash) { 
            Write-Host "`tERROR! $fileName contents don't match"
            $allGood = $false
            $errorList += $fileName

            # Save out the original and baseline so Jenkins will archive them for investigation
            [IO.File]::WriteAllBytes((Join-Path $script:errorDirLeft $fileName), $oldFileData.Content)
            Copy-Item $fileFullName (Join-Path $script:errorDirRight $fileName)
            continue
        }

        Write-Host "`tVerified $fileName"
    }

    if (-not $allGood) {
        Write-Host "Determinism failed for the following binaries:"
        foreach ($name in $errorList) {
            Write-Host "`t$name"
        }

        Write-Host "Archiving failure information"
        $zipFile = Join-Path $repoDir "Binaries\determinism.zip"
        Add-Type -Assembly "System.IO.Compression.FileSystem";
        [System.IO.Compression.ZipFile]::CreateFromDirectory($script:errorDir, $zipFile, "Fastest", $true);

        Write-Host "Please send $zipFile to compiler team for analysis"
        exit 1
    }
}

function Run-Test() {
    $rootDir = $repoDir

    # Ensure the error directory is written for all analysis to use.
    $script:errorDir = Join-Path $repoDir "Binaries\Determinism"
    $script:errorDirLeft = Join-Path $script:errorDir "Left"
    $script:errorDirRight = Join-Path $script:errorDir "Right"
    Create-Directory $script:errorDir
    Create-Directory $script:errorDirLeft
    Create-Directory $script:errorDirRight

    # Run the initial build so that we can populate the maps
    Run-Build $repoDir 
    $dataMap = Record-Binaries $repoDir
    Test-MapContents $dataMap

    # Run a test against the source in the same directory location
    Test-Build -rootDir $repoDir -dataMap $dataMap

    # Run another build in a different source location and verify that path mapping 
    # allows the build to be identical.  To do this we'll copy the entire source 
    # tree under the Binaries\q directory and run a build from there.
    $altRootDir = Join-Path "$repoDir\Binaries" "q"
    Remove-Item -re -fo $altRootDir -ErrorAction SilentlyContinue
    & robocopy $repoDir $altRootDir /E /XD $binariesDir /XD ".git" /njh /njs /ndl /nc /ns /np /nfl
    $pathMapBuildOption = "/p:PathMap=`"$altRootDir=$repoDir`""
    Test-Build -rootDir $altRootDir -dataMap $dataMap -pathMapBuildOption $pathMapBuildOption -restore
}

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")

    $msbuild = Ensure-MSBuild
    if (($bootstrapDir -eq "") -or (-not ([IO.Path]::IsPathRooted($script:bootstrapDir)))) {
        Write-Host "The bootstrap build path must be absolute"
        exit 1
    }

    Run-Test
    exit 0
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
finally {
    Write-Host "Stopping VBCSCompiler"
    Get-Process VBCSCompiler -ErrorAction SilentlyContinue | Stop-Process
    Write-Host "Stopped VBCSCompiler"
}

