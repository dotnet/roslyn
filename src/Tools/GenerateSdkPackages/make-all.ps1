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

# The debugger DLLs have a more complex structure and it's easier to special case
# copying them over.
function copy-debugger() { 
    $refRootPath = [IO.Path]::GetFullPath((join-path $dropPath "..\..\Debugger\ReferenceDLL"))
    $debuggerDllPath = join-path $dllPath "debugger"
    $net20Path = join-path $debuggerDllPath "net20"
    $net45Path = join-path $debuggerDllPath "net45"
    $portablePath = join-path $debuggerDllPath "portable"

    mkdir $debuggerDllPath -ErrorAction SilentlyContinue | out-null
    mkdir $net20Path -ErrorAction SilentlyContinue | out-null
    mkdir $net45Path -ErrorAction SilentlyContinue | out-null
    mkdir $portablePath -ErrorAction SilentlyContinue | out-null

    pushd $debuggerDllPath
    try {
        $d = join-path $dropPath "..\..\Debugger"
        cp (join-path $d "RemoteDebugger\Microsoft.VisualStudio.Debugger.Engine.dll") $net20Path
        cp (join-path $d "IDE\Microsoft.VisualStudio.Debugger.Engine.dll") $net45Path
        cp (join-path $d "x-plat\coreclr.windows\mcg\Microsoft.VisualStudio.Debugger.Engine.dll") $portablePath
        cp (join-path $dropPath "Microsoft.VisualStudio.Debugger.Metadata.dll") $net20Path
        cp (join-path $dropPath "Microsoft.VisualStudio.Debugger.Metadata.dll") $portablePath
        gci -re -in *.dll | %{ & $fakeSign -f $_ }
    }
    finally {
        popd
    }
}

# Used to package debugger nugets
function package-debugger() {
    param( [string]$kind )
    $debuggerPath = join-path $dllPath "debugger"
    $nuspecPath = join-path $PSScriptRoot "$kind.nuspec"
    & $nuget pack $nuspecPath -OutputDirectory $packagePath -Properties version=$packageVersion`;debuggerPath=$debuggerPath
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
        copy-debugger

        foreach ($item in $list) {
            $name = split-path -leaf $item
            $simpleName = [IO.Path]::GetFileNameWithoutExtension($name) 
            write-host "Packing $simpleName"
            switch ($simpleName) {
                "Microsoft.VisualStudio.Debugger.Engine" { package-debugger "engine" }
                "Microsoft.VisualStudio.Debugger.Metadata" { package-debugger "metadata" }
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
