Param(
    [string]$version = "26014.00",
    [string]$branch = "d15rel",
    [string]$outPath = $null,
    [string]$fakeSign = $null
)

set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try {
    if ($outPath -eq "") {
        write-host "Need an -outPath value"
        exit 1
    }

    if ($fakeSign -eq "") {
        write-host "Need a -fakeSign value"
        exit 1
    }

    $list = gc (join-path $PSScriptRoot "files.txt")
    $dropPath = "\\cpvsbuild\drops\VS\$branch\raw\$version\binaries.x86ret\bin\i386"
    $nuget = join-path $PSScriptRoot "..\..\..\nuget.exe"

    $baseNuspecPath = join-path $PSScriptRoot "base.nuspec"
    $shortVersion = $version.Substring(0, $version.IndexOf('.'))
    $packageVersion = "15.0.$shortVersion-alpha"
    $dllPath = join-path $outPath "Dlls"
    $packagePath = join-path $outPath "Packages"

    write-host "Drop path is $dropPath"
    write-host "Package version $packageVersion"
    write-host "Out path is $outPath"

    mkdir $outPath -ErrorAction SilentlyContinue | out-null
    mkdir $dllPath -ErrorAction SilentlyContinue | out-null
    mkdir $packagePath -ErrorAction SilentlyContinue | out-null
    pushd $outPath
    try {

        foreach ($item in $list) {
            $name = split-path -leaf $item
            $simpleName = [IO.Path]::GetFileNameWithoutExtension($name) 
            write-host "Packing $simpleName"
            $sourceFilePath = join-path $dropPath $item
            $filePath = join-path $dllPath $name
            if (-not (test-path $sourceFilePath)) {
                write-host "Could not locate $sourceFilePath"
                continue;
            }

            cp $sourceFilePath $filePath
            & $fakeSign -f $filePath
            & $nuget pack $baseNuspecPath -OutputDirectory $packagePath -Properties name=$simpleName`;version=$packageVersion`;filePath=$filePath
        }
    }
    finally {
        popd
    }
}
catch [exception] {
    write-host $_.Exception
    exit -1
}
