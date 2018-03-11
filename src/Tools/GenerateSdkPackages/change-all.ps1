[CmdletBinding(PositionalBinding=$false)]
Param([string]$version = "")

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    if ($version -eq "") { 
        Write-Host "Need a -version"
        exit 1
    }

    $rootPath = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\")
    $repoUtil = Join-Path $rootPath "Binaries\Debug\Exes\RepoUtil\RepoUtil.exe"
    if (-not (Test-Path $repoUtil)) { 
        Write-Host "RepoUtil not found $repoUtil"
        exit 1
    }

    $fileList = Get-Content (Join-Path $PSScriptRoot "files.txt")
    $shortVersion = $version.Substring(0, $version.IndexOf('.'))
    $packageVersion = "15.0.$shortVersion-alpha"
    $changeList = @()

    Write-Host "Moving version to $packageVersion"
    foreach ($item in $fileList) { 
        $name = Split-Path -leaf $item
        $simpleName = [IO.Path]::GetFileNameWithoutExtension($name) 
        $changeList += "$simpleName $packageVersion"
    }

    $changeFilePath = [IO.Path]::GetTempFileName()
    $changeList -join [Environment]::NewLine | Out-File $changeFilePath
    Write-Host (gc -raw $changeFilePath)

    $fullSln = Join-Path $rootPath "..\Roslyn.sln"
    if (Test-Path $fullSln) {
        # Running as a part of the full enlisment.  Need to add some extra paramteers
        $sourcesPath = Resolve-Path (Join-Path $rootPath "..")
        $generatePath = $rootPath
        $configPath = Join-Path $rootPath "build\config\RepoUtilData.json"
        & $repoUtil -sourcesPath $sourcesPath -generatePath $generatePath -config $configPath change -version $changeFilePath
    }
    else {
        # Just open so run there.
        & $repoUtil change -version $changeFilePath
    }

}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
