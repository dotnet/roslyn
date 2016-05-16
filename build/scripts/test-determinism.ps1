param (
        [string]$script:buildDir = $(throw "Need a directory containing a compiler build to test with"), 
        [bool]$debugDeterminism = $false)
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

### Variables available to the entire script.

# List of binary names that should be skipped because they have a known issue that
# makes them non-deterministic.  
$script:skipList = @(

    # https://github.com/dotnet/roslyn/issues/8739
    "Microsoft.VisualStudio.ProjectSystem.Managed.dll"
)

# Holds the determinism data checked on every build.
$script:dataMap = @{}

# Location that deterministic error information should be written to. 
[string]$script:errorDir = ""
[string]$script:errorDirLeft = ""
[string]$script:errorDirRight = ""

function Run-Build()
{
    param ( [string]$rootDir = $(throw "Need a root directory to build"),
            [string]$pathMapBuildOption = "")

    $sln = join-path $rootDir "Roslyn.sln"
    $debugDir = join-path $rootDir "Binaries\Debug"
    $objDir = join-path $rootDir "Binaries\Obj"

    # Create directories that may or may not exist to make the script execution below 
    # clean in either case.
    mkdir $debugDir -errorAction SilentlyContinue | out-null
    mkdir $objDir -errorAction SilentlyContinue | out-null

    pushd $rootDir

    # Clean out the previous run
    write-host "Cleaning the Binaries"
    rm -re -fo $debugDir
    rm -re -fo $objDir
    & msbuild /nologo /v:m /nodeReuse:false /t:clean $sln

    write-host "Building the Solution"
    & msbuild /nologo /v:m /nodeReuse:false /m /p:DebugDeterminism=true /p:BootstrapBuildPath=$script:buildDir '/p:Features="debug-determinism;pdb-path-determinism"' /p:UseRoslynAnalyzers=false $pathMapBuildOption $sln

    popd
}

function Run-Analysis()
{
    param ( [string]$rootDir = $(throw "Need a root directory to build"),
            [bool]$buildMap = $(throw "Whether to build the map or analyze it"),
            [string]$pathMapBuildOption = "")
            
    $debugDir = join-path $rootDir "Binaries\Debug"
    $errorList = @()
    $allGood = $true

    Run-Build $rootDir $pathMapBuildOption

    pushd $debugDir

    write-host "Testing the binaries"
    foreach ($dll in gci -re -in *.dll,*.exe) {
        $dllFullName = $dll.FullName
        $dllId = $dllFullName.Substring($debugDir.Length)
        $dllName = split-path -leaf $dllFullName
        $dllHash = (get-filehash $dll -algorithm MD5).Hash
        $keyFullName = $dllFullName + ".key"
        $keyName = split-path -leaf $keyFullName

        # Do not process binaries that have been explicitly skipped or do not have a key
        # file.  The lack of a key file means it's a binary that wasn't specifically 
        # built for that directory (dependency).  Only need to check the binaries we are
        # building. 
        if ($script:skipList.Contains($dllName) -or -not (test-path $keyFullName)) {
            continue;
        }

        if ($buildMap) {
            write-host "`tRecording $dllName = $dllHash"
            $data = @{}
            $data["Hash"] = $dllHash
            $data["Content"] = [IO.File]::ReadAllBytes($dllFullName)
            $data["Key"] = [IO.File]::ReadAllBytes($dllFullName + ".key")
            $script:dataMap[$dllId] = $data
        }
        elseif (-not $script:dataMap.Contains($dllId)) {
            write-host "Missing entry in map $dllId->$dllFullName"
            $allGood = $false
        }
        else {
            $data = $script:dataMap[$dllId]
            $oldHash = $data.Hash
            if ($oldHash -eq $dllHash) {
                write-host "`tVerified $dllName"
            }
            else {
                write-host "`tERROR! $dllName"
                $allGood = $false
                $errorList += $dllName

                # Save out the original and baseline so Jenkins will archive them for investigation
                [IO.File]::WriteAllBytes((join-path $script:errorDirLeft $dllName), $data.Content)
                [IO.File]::WriteAllBytes((join-path $script:errorDirLeft $keyName), $data.Key)
                cp $dllFullName (join-path $script:errorDirRight $dllName)
                cp $keyFullName (join-path $script:errorDirRight $keyName)
            }
        }
    }

    popd

    # During determinism debugging shutdown the compiler after every pass so we get a unique
    # log directory.
    if ($debugDeterminism) {
        gps VBCSCompiler -ErrorAction SilentlyContinue | kill
    }

    # Sanity check to ensure we didn't return a false positive because we failed
    # to examine any binaries.
    if ($script:dataMap.Count -lt 10) {
        write-host "Found no binaries to process"
        $allGood = $false
    }

    if (-not $allGood) {
        write-host "Determinism failed for the following binaries:"
        foreach ($name in $errorList) {
            write-host "`t$name"
        }

        write-host "Archiving failure information"
        $zipFile = join-path $rootDir "Binaries\determinism.zip"
        Add-Type -Assembly "System.IO.Compression.FileSystem";
        [System.IO.Compression.ZipFile]::CreateFromDirectory($script:errorDir, $zipFile, "Fastest", $true);

        write-host "Please send $zipFile to compiler team for analysis"
        exit 1
    }
}

function Run-Test()
{
    $origRootDir = resolve-path (split-path -parent (split-path -parent $PSScriptRoot))

    # Ensure the error directory is written for all analysis to use.
    $script:errorDir = join-path $origRootDir "Binaries\Determinism"
    $script:errorDirLeft = join-path $script:errorDir "Left"
    $script:errorDirRight = join-path $script:errorDir "Right"
    mkdir $script:errorDir -errorAction SilentlyContinue | out-null
    mkdir $script:errorDirLeft -errorAction SilentlyContinue | out-null
    mkdir $script:errorDirRight -errorAction SilentlyContinue | out-null

    # Run initial build to populate all of the expected data.
    Run-Analysis -rootDir $origRootDir -buildMap $true

    # Run another build in same place and verify the build is identical.
    Run-Analysis -rootDir $origRootDir -buildMap $false

    # Run another build in a different source location and verify that path mapping 
    # allows the build to be identical.  To do this we'll copy the entire source 
    # tree under the Binaries\q directory and run a build from there.
    $origBinDir = join-path $origRootDir "Binaries"
    $altRootDir = join-path $origBinDir "q"
    & robocopy $origRootDir $altRootDir /E /XD $origBinDir /XD ".git" /njh /njs /ndl /nc /ns /np /nfl
    $pathMapBuildOption = "/p:PathMap=`"$altRootDir=$origRootDir`""
    Run-Analysis -rootDir $altRootDir -buildMap $false -pathMapBuildOption $pathMapBuildOption
    rm -re -fo $altRootDir
}

try
{
    if (-not ([IO.Path]::IsPathRooted($script:buildDir))) {
        write-error "The build path must be absolute"
        exit 1
    }

    Run-Test
    exit 0
}
catch
{
    write-host "Error: $($_.Exception.Message)"
    exit 1
}
finally
{
    write-host "Stopping VBCSCompiler"
    gps VBCSCompiler -ErrorAction SilentlyContinue | kill
    write-host "Stopped VBCSCompiler"
}

