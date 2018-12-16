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

try {
    if ($outPath -eq "") {
        Write-Host "Need an -outPath value"
        exit 1
    }

    . (Join-Path $PSScriptRoot "..\..\eng\build-utils.ps1")

    $list = Get-Content (Join-Path $PSScriptRoot "files.txt")
    $dropPath = "\\cpvsbuild\drops\VS\$branch\raw\$version\binaries.x86ret\bin\i386"
    $nuget = Join-Path $PSScriptRoot "..\..\..\Binaries\Tools\nuget.exe"
    $fakeSign = Join-Path (Get-PackageDir "FakeSign") "Tools\FakeSign.exe"

    $shortVersion = $version.Substring(0, $version.IndexOf('.'))
    $packageVersion = "15.8.$shortVersion-$versionSuffix"
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
        foreach ($item in $list) {
            $name = Split-Path -leaf $item
            $simpleName = [IO.Path]::GetFileNameWithoutExtension($name) 
            Write-Host "Packing $simpleName"

            if ($simpleName.StartsWith("Microsoft.VisualStudio.Debugger")) {
              Package-Debugger $simpleName     
            } else {
              Package-Normal
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
