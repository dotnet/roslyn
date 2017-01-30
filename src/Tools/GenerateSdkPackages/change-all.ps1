Param(
    [string]$version = ""
)

set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try {
    if ($version -eq "") { 
        write-host "Need a -version"
        exit 1
    }

    $rootPath = resolve-path (join-path $PSScriptRoot "..\..\..\")
    $repoUtil = join-path $rootPath "Binaries\Debug\Exes\RepoUtil\RepoUtil.exe"
    if (-not (test-path $repoUtil)) { 
        write-host "RepoUtil not found $repoUtil"
        exit 1
    }

    $fileList = gc (join-path $PSScriptRoot "files.txt")
    $shortVersion = $version.Substring(0, $version.IndexOf('.'))
    $packageVersion = "15.0.$shortVersion-alpha"
    $changeList = @()

    write-host "Moving version to $packageVersion"
    foreach ($item in $fileList) { 
        $name = split-path -leaf $item
        $simpleName = [IO.Path]::GetFileNameWithoutExtension($name) 
        $changeList += "$simpleName $packageVersion"
    }

    $changeFilePath = [IO.Path]::GetTempFileName()
    $changeList | out-file $changeFilePath

    $fullSln = join-path $rootPath "..\Roslyn.sln"
    if (test-path $fullSln) {
        # Running as a part of the full enlisment.  Need to add some extra paramteers
        $sourcesPath = resolve-path (join-path $rootPath "..")
        $generatePath = $rootPath
        $configPath = join-path $rootPath "build\config\RepoUtilData.json"
        & $repoUtil -sourcesPath $sourcesPath -generatePath $generatePath -config $configPath change -version $changeFilePath
    }
    else {
        # Just open so run there.
        & $repoUtil change -version $changeFilePath
    }

}
catch [exception] {
    write-host $_.Exception
    exit -1
}
