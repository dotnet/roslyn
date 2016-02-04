param ([string]$buildDir = $(throw "Need a directory containing a compiler build to test with"))

$rootDir = split-path -parent (split-path -parent $PSScriptRoot)
$debugDir = resolve-path (join-path $rootDir "Binaries\Debug")
$sln = join-path $rootDir "Compilers.sln"

pushd $rootDir

$allGood = $true
$map = @{}
$i = 0;
while ($i -lt 3 -and $allGood) {

    # Clean out the previous run
    write-host "Cleaning the Binaries"
    rm -re -fo "Binaries\Debug"
    rm -re -fo "Binaries\Obj"
    msbuild /nologo /v:m /t:clean $sln

    write-host "Building the Solution"
    msbuild /nologo /v:m /m $sln

    pushd $debugDir

    write-host "Testing the binaries"
    foreach ($dll in gci -re -in Microsoft.CodeAnalysis.*dll,Roslyn.*dll,cs*exe,vb*exe) {
        $dllFullName = $dll.FullName
        $dllName = split-path -leaf $dllFullName
        $dllHash = get-md5 $dll
        if ($i -eq 0) {
            write-host "`tRecording $dllName = $dllHash"
            $data = @{}
            $data["Hash"] = $dllHash
            $data["Content"] = [IO.File]::ReadAllBytes($dllFullName)
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

if (-not $allGood) {
    exit 1
}

exit 0
