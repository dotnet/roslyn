param ([string]$buildDir = $(throw "Need a directory containing a compiler build to test with"))

# TODO: Use the Roslyn.sln instead of Compilers.sln
$rootDir = split-path -parent (split-path -parent $PSScriptRoot)
$debugDir = join-path $rootDir "Binaries\Debug"
$sln = join-path $rootDir "build\Toolset.sln"

pushd $rootDir

$allGood = $true
$map = @{}
$i = 0;
while ($i -lt 3 -and $allGood) {

    rm -re -fo $debugDir
    msbuild /v:m /m $sln

    pushd $debugDir

    foreach ($dll in gci -re -in Microsoft.CodeAnalysis.*dll,Roslyn.*dll) {
        $dllName = split-path -leaf $dll
        $dllHash = get-md5 $dll
        if ($i -eq 0) {
            $map[$dllName] = $dllHash
        }
        else {
            $oldHash = $map[$dllName]
            if ($oldHash -ne $dllHash) {
                write-host "$dllName changed"
                $allGood = $false
            }
        }
    }

    popd

    $i++
}

popd

if (-not $allGood) {
    exit 1
}

exit 0
