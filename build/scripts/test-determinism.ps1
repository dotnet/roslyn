param ([string]$script:buildDir = $(throw "Need a directory containing a compiler build to test with"))
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

# The set of errors that we encountered during 

function Run-Build()
{
    param ( [string]$rootDir = $(throw "Need a root directory to build"),
            [bool]$buildMap = $(throw "Are we building the map"))
            
    $sln = join-path $rootDir "Roslyn.sln"
    $debugDir = join-path $rootDir "Binaries\Debug"
    $errorDir = join-path $rootDir "Binaries\Determinism"
    $errorList = @()
    $allGood = $true

    # Create directories that may or may not exist to make the script execution below 
    # clean in either case.
    mkdir $debugDir -errorAction SilentlyContinue | out-null
    mkdir (join-path $rootDir "Binaries\Obj") -errorAction SilentlyContinue | out-null
    mkdir $errorDir -errorAction SilentlyContinue | out-null

    pushd $rootDir

    # Clean out the previous run
    write-host "Cleaning the Binaries"
    rm -re -fo "Binaries\Debug" 
    rm -re -fo "Binaries\Obj"
    & msbuild /nologo /v:m /nodeReuse:false /t:clean $sln

    write-host "Building the Solution"
    & msbuild /nologo /v:m /nodeReuse:false /m /p:DebugDeterminism=true /p:BootstrapBuildPath=$script:buildDir /p:Features=debug-determinism /p:UseRoslynAnalyzers=false $sln

    popd

    pushd $debugDir

    write-host "Testing the binaries"
    foreach ($dll in gci -re -in *.dll,*.exe) {
        $dllFullName = $dll.FullName
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
            $script:dataMap[$dllFullName] = $data
        }
        else {
            $data = $script:dataMap[$dllFullName]
            $oldHash = $data.Hash
            if ($oldHash -eq $dllHash) {
                write-host "`tVerified $dllName"
            }
            else {
                write-host "`tERROR! $dllName"
                $allGood = $false
                $errorList += $dllName

                # Save out the original and baseline so Jenkins will archive them for investigation
                pushd $errorDir
                [IO.File]::WriteAllBytes((join-path $errorDir ($dllName + ".original")), $data.Content)
                [IO.File]::WriteAllBytes((join-path $errorDir ($keyName + ".original")), $data.Key)
                cp $dllFullName ($dllName + ".baseline")
                cp $keyFullName ($keyName + ".baseline")
                popd
            }
        }
    }

    popd

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
        [System.IO.Compression.ZipFile]::CreateFromDirectory($errorDir, $zipFile, "Fastest", $true);

        write-host "Please send $zipFile to compiler team for analysis"
        exit 1
    }
}

try
{
    if (-not ([IO.Path]::IsPathRooted($script:buildDir))) {
        write-error "The build path must be absolute"
        exit 1
    }

    $rootDir = resolve-path (split-path -parent (split-path -parent $PSScriptRoot))
    Run-Build -rootDir $rootDir -buildMap $true
    Run-Build -rootDir $rootDir -buildMap $false
    Run-Build -rootDir $rootDir -buildMap $false

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

