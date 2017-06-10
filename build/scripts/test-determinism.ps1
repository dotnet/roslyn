[CmdletBinding(PositionalBinding=$false)]
param ( [string]$bootstrapDir = $(throw "Need a directory containing a compiler build to test with"), 
        [bool]$debugDeterminism = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

### Variables available to the entire script.

# List of binary names that should be skipped because they have a known issue that
# makes them non-deterministic.  
$script:skipList = @()

# Holds the determinism data checked on every build.
$script:dataMap = @{}

# Location that deterministic error information should be written to. 
[string]$script:errorDir = ""
[string]$script:errorDirLeft = ""
[string]$script:errorDirRight = ""

function Run-Build() {
    param ( [string]$rootDir = $(throw "Need a root directory to build"),
            [string]$pathMapBuildOption = "",
            [switch]$restore = $false)

    $sln = Join-Path $rootDir "Roslyn.sln"
    $debugDir = Join-Path $rootDir "Binaries\Debug"
    $objDir = Join-Path $rootDir "Binaries\Obj"

    # Create directories that may or may not exist to make the script execution below 
    # clean in either case.
    Create-Directory $debugDir
    Create-Directory $objDir

    Push-Location $rootDir
    try {

        # Clean out the previous run
        Write-Host "Cleaning the Binaries"
        Exec-Command $msbuild "/nologo /v:m /nodeReuse:false /t:clean $sln"

        if ($restore) {
            Write-Host "Restoring the packages"
            Restore-Project -fileName $sln -nuget (Ensure-NuGet) -msbuildDir (Split-Path -parent $msbuild)
        }

        Write-Host "Building the Solution"
        Exec-Command $msbuild "/nologo /v:m /nodeReuse:false /m /p:DebugDeterminism=true /p:BootstrapBuildPath=$script:bootstrapDir /p:Features=`"debug-determinism`" /p:UseRoslynAnalyzers=false $pathMapBuildOption $sln"
    }
    finally {
        Pop-Location
    }
}

function Run-Analysis() {
    param ( [string]$rootDir = $(throw "Need a root directory to build"),
            [bool]$buildMap = $(throw "Whether to build the map or analyze it"),
            [string]$pathMapBuildOption = "",
            [switch]$restore = $false)
            
    $debugDir = Join-Path $rootDir "Binaries\Debug"
    $errorList = @()
    $allGood = $true

    Run-Build $rootDir $pathMapBuildOption -restore:$restore

    Push-Location $debugDir

    Write-Host "Testing the binaries"
    foreach ($dll in gci -re -in *.dll,*.exe) {
        $dllFullName = $dll.FullName
        $dllId = $dllFullName.Substring($debugDir.Length)
        $dllName = Split-Path -leaf $dllFullName
        $dllHash = (get-filehash $dll -algorithm MD5).Hash
        $keyFullName = $dllFullName + ".key"
        $keyName = Split-Path -leaf $keyFullName

        # Do not process binaries that have been explicitly skipped or do not have a key
        # file.  The lack of a key file means it's a binary that wasn't specifically 
        # built for that directory (dependency).  Only need to check the binaries we are
        # building. 
        if ($script:skipList.Contains($dllName) -or -not (test-path $keyFullName)) {
            continue;
        }

        if ($buildMap) {
            Write-Host "`tRecording $dllName = $dllHash"
            $data = @{}
            $data["Hash"] = $dllHash
            $data["Content"] = [IO.File]::ReadAllBytes($dllFullName)
            $data["Key"] = [IO.File]::ReadAllBytes($dllFullName + ".key")
            $script:dataMap[$dllId] = $data
        }
        elseif (-not $script:dataMap.Contains($dllId)) {
            Write-Host "Missing entry in map $dllId->$dllFullName"
            $allGood = $false
        }
        else {
            $data = $script:dataMap[$dllId]
            $oldHash = $data.Hash
            if ($oldHash -eq $dllHash) {
                Write-Host "`tVerified $dllName"
            }
            else {
                Write-Host "`tERROR! $dllName"
                $allGood = $false
                $errorList += $dllName

                # Save out the original and baseline so Jenkins will archive them for investigation
                [IO.File]::WriteAllBytes((Join-Path $script:errorDirLeft $dllName), $data.Content)
                [IO.File]::WriteAllBytes((Join-Path $script:errorDirLeft $keyName), $data.Key)
                cp $dllFullName (Join-Path $script:errorDirRight $dllName)
                cp $keyFullName (Join-Path $script:errorDirRight $keyName)
            }
        }
    }

    Pop-Location

    # During determinism debugging shutdown the compiler after every pass so we get a unique
    # log directory.
    if ($debugDeterminism) {
        Get-Process VBCSCompiler -ErrorAction SilentlyContinue | kill
    }

    # Sanity check to ensure we didn't return a false positive because we failed
    # to examine any binaries.
    if ($script:dataMap.Count -lt 10) {
        Write-Host "Found no binaries to process"
        $allGood = $false
    }

    if (-not $allGood) {
        Write-Host "Determinism failed for the following binaries:"
        foreach ($name in $errorList) {
            Write-Host "`t$name"
        }

        Write-Host "Archiving failure information"
        $zipFile = Join-Path $rootDir "Binaries\determinism.zip"
        Add-Type -Assembly "System.IO.Compression.FileSystem";
        [System.IO.Compression.ZipFile]::CreateFromDirectory($script:errorDir, $zipFile, "Fastest", $true);

        Write-Host "Please send $zipFile to compiler team for analysis"
        exit 1
    }
}

function Run-Test() {
    $origRootDir = Resolve-Path (Split-Path -parent (Split-Path -parent $PSScriptRoot))

    # Ensure the error directory is written for all analysis to use.
    $script:errorDir = Join-Path $origRootDir "Binaries\Determinism"
    $script:errorDirLeft = Join-Path $script:errorDir "Left"
    $script:errorDirRight = Join-Path $script:errorDir "Right"
    Create-Directory $script:errorDir
    Create-Directory $script:errorDirLeft
    Create-Directory $script:errorDirRight

    # Run initial build to populate all of the expected data.
    Run-Analysis -rootDir $origRootDir -buildMap $true

    # Run another build in same place and verify the build is identical.
    Run-Analysis -rootDir $origRootDir -buildMap $false

    # Run another build in a different source location and verify that path mapping 
    # allows the build to be identical.  To do this we'll copy the entire source 
    # tree under the Binaries\q directory and run a build from there.
    $origBinDir = Join-Path $origRootDir "Binaries"
    $altRootDir = Join-Path $origBinDir "q"
    & robocopy $origRootDir $altRootDir /E /XD $origBinDir /XD ".git" /njh /njs /ndl /nc /ns /np /nfl
    $pathMapBuildOption = "/p:PathMap=`"$altRootDir=$origRootDir`""
    Run-Analysis -rootDir $altRootDir -buildMap $false -pathMapBuildOption $pathMapBuildOption -restore
    Remove-Item -re -fo $altRootDir
}

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")

    $msbuild = Ensure-MSBuild
    if (-not ([IO.Path]::IsPathRooted($script:bootstrapDir))) {
        Write-Host "The bootstrap build path must be absolute"
        exit 1
    }

    Run-Test
    exit 0
}
catch {
    Write-Host "Error: $($_.Exception.Message)"
    exit 1
}
finally {
    Write-Host "Stopping VBCSCompiler"
    Get-Process VBCSCompiler -ErrorAction SilentlyContinue | kill
    Write-Host "Stopped VBCSCompiler"
}

