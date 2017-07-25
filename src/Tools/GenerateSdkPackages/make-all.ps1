[CmdletBinding(PositionalBinding=$false)]
param(
    [string]$version = "26606.00",
    [string]$branch = "d15rel",
    [string]$outPath = $null,
    [string]$versionSuffix = "alpha"
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

# Package a normal DLL into a nuget.  Default used for packages that have a simple 1-1
# relationship between DLL and NuGet for only Net46.
function Package-Normal() {
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
function Copy-Debugger() { 
    $refRootPath = [IO.Path]::GetFullPath((Join-Path $dropPath "..\..\Debugger\ReferenceDLL"))
    $debuggerDllPath = Join-Path $dllPath "debugger"
    Create-Directory $debuggerDllPath
    Copy-Item -re -fo "$refRootPath\*" $debuggerDllPath
}

# Used to package debugger nugets
function Package-Debugger() {
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

    . (Join-Path $PSScriptRoot "..\..\..\build\scripts\build-utils.ps1")

    $list = Get-Content (Join-Path $PSScriptRoot "files.txt")
    $dropPath = "\\cpvsbuild\drops\VS\$branch\raw\$version\binaries.x86ret\bin\i386"
    $nuget = Join-Path $PSScriptRoot "..\..\..\nuget.exe"
    $fakeSign = Join-Path (Get-PackageDir "FakeSign") "Tools\FakeSign.exe"

    $shortVersion = $version.Substring(0, $version.IndexOf('.'))
    $packageVersion = "15.0.$shortVersion-$versionSuffix"
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
        Copy-Debugger

        foreach ($item in $list) {
            $name = Split-Path -leaf $item
            $simpleName = [IO.Path]::GetFileNameWithoutExtension($name) 
            Write-Host "Packing $simpleName"
            switch ($simpleName) {
                "Microsoft.VisualStudio.Debugger.Engine" { Package-Debugger "engine" }
                "Microsoft.VisualStudio.Debugger.Metadata" { Package-Debugger "metadata" }
                default { Package-Normal }
            }
        }
    }
    finally {
        Pop-Location
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Get-PSCallstack
    throw

    exit 1
}
