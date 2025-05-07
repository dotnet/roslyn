<#
.SYNOPSIS
Downloads 32-bit and 64-bit procdump executables and returns the path to where they were installed.
#>
$version = '0.0.1'
$baseDir = "$PSScriptRoot\..\obj\tools"
$procDumpToolPath = "$baseDir\procdump.$version\bin"
if (-not (Test-Path $procDumpToolPath)) {
    if (-not (Test-Path $baseDir)) { New-Item -Type Directory -Path $baseDir | Out-Null }
    $baseDir = (Resolve-Path $baseDir).Path # Normalize it
    & (& $PSScriptRoot\Get-NuGetTool.ps1) install procdump -version $version -PackageSaveMode nuspec -OutputDirectory $baseDir -Source https://api.nuget.org/v3/index.json | Out-Null
}

(Resolve-Path $procDumpToolPath).Path
