Param(
    [string]$version = "26014.00",
    [string]$branch = "d15rel",
    [string]$outPath = $null,
    [string]$fakeSign = $null
)

set-strictmode -version 2.0
$ErrorActionPreference="Stop"

# Package a normal DLL into a nuget.  Default used for packages that have a simple 1-1
# relationship between DLL and NuGet for only Net46.
function package-normal() {
    $baseNuspecPath = join-path $PSScriptRoot "base.nuspec"
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

# Used to package Microsoft.VisualStudio.Debugger.Engine
function package-engine() {
    $refRootPath = [IO.Path]::GetFullPath((join-path $dropPath "..\..\Debugger\ReferenceDLL"))
    $engineDllPath = join-path $dllPath "engine"
    $engineNespecPath = join-path $PSScriptRoot "engine.nuspec"
    mkdir $engineDllPath -ErrorAction SilentlyContinue | out-null
    cp -fo -re $refRootPath $engineDllPath
    pushd $engineDllPath
    try {
        gci -re -in *.dll | %{ & $fakeSign -f $_ }
        & $nuget pack $engineNespecPath -OutputDirectory $packagePath -Properties version=$packageVersion`;enginePath=$engineDllPath
    }
    finally {
        popd
    }
}

# Used to package Microsoft.VisualStudio.Debugger.Metadata
function package-metadata() {
    $refRootPath = [IO.Path]::GetFullPath((join-path $dropPath "..\..\Debugger\ReferenceDLL"))
    $metadataDllPath = join-path $dllPath "metadata"
    $metadataNespecPath = join-path $PSScriptRoot "metadata.nuspec"
    mkdir $metadataDllPath -ErrorAction SilentlyContinue | out-null
    cp -fo -re $refRootPath $metadataDllPath
    pushd $metadataDllPath
    try {
        gci -re -in *.dll | %{ & $fakeSign -f $_ }
        & $nuget pack $metadataNespecPath -OutputDirectory $packagePath -Properties version=$packageVersion`;metadataPath=$metadataDllPath
    }
    finally {
        popd
    }
}

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
            switch ($simpleName) {
                "Microsoft.VisualStudio.Debugger.Engine" { package-engine }
                "Microsoft.VisualStudio.Debugger.Metadata" { package-metadata }
                default { package-normal }
            }
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
