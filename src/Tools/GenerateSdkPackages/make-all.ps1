[CmdletBinding(PositionalBinding=$false)]
Param( [string]$version = "26014.00",
    [string]$branch = "d15rel",
    [string]$outPath = $null,
    [string]$fakeSign = $null
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Create-Directory([string]$name) {
    [IO.Directory]::CreateDirectory($name)
}

# Package a normal DLL into a nuget.  Default used for packages that have a simple 1-1
# relationship between DLL and NuGet for only Net46.
function package-normal() {
    $baseNuspecPath = Join-Path $PSScriptRoot "base.nuspec"
    $sourceFilePath = Join-Path $dropPath $item
    $filePath = Join-Path $dllPath $name
    if (-not (Test-Path $sourceFilePath)) {
        Write-Host "Could not locate $sourceFilePath"
        continue;
    }

    Copy-Item $sourceFilePath $filePath
    & $fakeSign -f $filePath
    & $nuget pack $baseNuspecPath -OutputDirectory $packagePath -Properties name=$simpleName`;version=$packageVersion`;filePath=$filePath
}

# The debugger DLLs have a more complex structure and it's easier to special case
# copying them over.
function copy-debugger() { 
    $refRootPath = [IO.Path]::GetFullPath((Join-Path $dropPath "..\..\Debugger\ReferenceDLL"))
    $debuggerDllPath = Join-Path $dllPath "debugger"
    $net20Path = Join-Path $debuggerDllPath "net20"
    $net45Path = Join-Path $debuggerDllPath "net45"
    $portablePath = Join-Path $debuggerDllPath "portable"

    Create-Directory $debuggerDllPath
    Create-Directory $net20Path
    Create-Directory $net45Path
    Create-Directory $portablePath

    Push-Location $debuggerDllPath
    try {
        $d = Join-Path $dropPath "..\..\Debugger"
        Copy-Item (Join-Path $d "RemoteDebugger\Microsoft.VisualStudio.Debugger.Engine.dll") $net20Path
        Copy-Item (Join-Path $d "IDE\Microsoft.VisualStudio.Debugger.Engine.dll") $net45Path
        Copy-Item (Join-Path $d "x-plat\coreclr.windows\mcg\Microsoft.VisualStudio.Debugger.Engine.dll") $portablePath
        Copy-Item (Join-Path $dropPath "Microsoft.VisualStudio.Debugger.Metadata.dll") $net20Path
        Copy-Item (Join-Path $dropPath "Microsoft.VisualStudio.Debugger.Metadata.dll") $portablePath
        Get-ChildItem -re -in *.dll | %{ & $fakeSign -f $_ }
    }
    finally {
        Pop-Location
    }
}

# Used to package debugger nugets
function package-debugger() {
    param( [string]$kind )
    $debuggerPath = Join-Path $dllPath "debugger"
    $nuspecPath = Join-Path $PSScriptRoot "$kind.nuspec"
    & $nuget pack $nuspecPath -OutputDirectory $packagePath -Properties version=$packageVersion`;debuggerPath=$debuggerPath
}

try {
    if ($outPath -eq "") {
        Write-Host "Need an -outPath value"
        exit 1
    }

    if ($fakeSign -eq "") {
        Write-Host "Need a -fakeSign value"
        exit 1
    }

    $list = Get-Content (Join-Path $PSScriptRoot "files.txt")
    $dropPath = "\\cpvsbuild\drops\VS\$branch\raw\$version\binaries.x86ret\bin\i386"
    $nuget = Join-Path $PSScriptRoot "..\..\..\nuget.exe"

    $shortVersion = $version.Substring(0, $version.IndexOf('.'))
    $packageVersion = "15.0.$shortVersion-alpha"
    $dllPath = Join-Path $outPath "Dlls"
    $packagePath = Join-Path $outPath "Packages"

    Write-Host "Drop path is $dropPath"
    Write-Host "Package version $packageVersion"
    Write-Host "Out path is $outPath"

    Create-Directory $outPath
    Create-Directory $dllPath
    Create-Directory $packagePath
    Push-Location $outPath
    try {
        copy-debugger

        foreach ($item in $list) {
            $name = Split-Path -leaf $item
            $simpleName = [IO.Path]::GetFileNameWithoutExtension($name) 
            Write-Host "Packing $simpleName"
            switch ($simpleName) {
                "Microsoft.VisualStudio.Debugger.Engine" { package-debugger "engine" }
                "Microsoft.VisualStudio.Debugger.Metadata" { package-debugger "metadata" }
                default { package-normal }
            }
        }
    }
    finally {
        Pop-Location
    }
}
catch {
    Write-Host $_
    exit 1
}
