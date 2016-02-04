param ([string]$buildDir = $(throw "Need a directory containing a compiler build to test with"))

if (-not ([IO.Path]::IsPathRooted($buildDir))) {
    write-error "The build path must be absolute"
    exit 1
}

$rootDir = resolve-path (split-path -parent (split-path -parent $PSScriptRoot))
$sln = join-path $rootDir "Compilers.sln"
$debugDir = join-path $rootDir "Binaries\Debug"

# Create directories that may or may not exist to make the script execution below 
# clean in either case.
mkdir $debugDir -errorAction SilentlyContinue | out-null
mkdir (join-path $rootDir "Binaries\Obj") -errorAction SilentlyContinue | out-null

pushd $rootDir

$skipList = @(
    "Microsoft.CodeAnalysis.Test.Resources.Proprietary.dll",

    # https://github.com/dotnet/roslyn/issues/7595
    "Roslyn.Compilers.CSharp.Emit.UnitTests.dll",
    "Roslyn.Compilers.CSharp.Symbol.UnitTests.dll",

    # TODO: file a bug for this
    "Roslyn.Compilers.CSharp.WinRT.UnitTests.dll"
)

$allGood = $true
$map = @{}
$i = 0;
while ($i -lt 3 -and $allGood) {

    # Clean out the previous run
    write-host "Cleaning the Binaries"
    rm -re -fo "Binaries\Debug" 
    rm -re -fo "Binaries\Obj"
    & msbuild /nologo /v:m /t:clean $sln

    write-host "Building the Solution"
    & msbuild /nologo /v:m /m /p:DebugDeterminism=true /p:BootstrapBuildPath=$buildDir /p:Features=debug-determinism /p:UseRoslynAnalyzers=false $sln

    pushd $debugDir

    write-host "Testing the binaries"
    foreach ($dll in gci Microsoft.CodeAnalysis.*dll,Roslyn.*dll,cs*exe,vb*exe) {
        $dllFullName = $dll.FullName
        $dllName = split-path -leaf $dllFullName
        $dllHash = get-md5 $dll
        $dllKeyName = $dllFullName + ".key"

        if ($skipList.Contains($dllName)) {
            continue;
        }

        if ($i -eq 0) {
            write-host "`tRecording $dllName = $dllHash"
            $data = @{}
            $data["Hash"] = $dllHash
            $data["Content"] = [IO.File]::ReadAllBytes($dllFullName)
            $data["Key"] = [IO.File]::ReadAllBytes($dllFullName + ".key")
            $map[$dllFullName] = $data
        }
        else {
            $data = $map[$dllFullName]
            $oldHash = $data.Hash
            if ($oldHash -eq $dllHash) {
                write-host "`tVerified $dllName"
            }
            else {
                write-host "`tERROR! $dllName changed ($dllFullName)"
                [IO.File]::WriteAllBytes($dllFullName + ".baseline", $data.Content)
                [IO.File]::WriteAllBytes($dllFullName + ".baseline.key", $data.Key)
                $allGood = $false
            }
        }
    }

    popd

    # Sanity check to ensure we didn't return a false positive because we failed
    # to examine any binaries.
    if ($map.Count -lt 10) {
        write-host "Found no binaries to process"
        $allGood = $false
    }

    $i++
}

popd

& $buildDir\VBCSCompiler.exe -shutdown

if (-not $allGood) {
    exit 1
}

exit 0
