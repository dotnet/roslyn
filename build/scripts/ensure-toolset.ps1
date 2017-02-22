param ()
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

$nugetVersion = "3.6.0-beta1"
$msbuildToolsetVersion = "0.1.6-alpha"
$repoDir = [IO.Path]::GetFullPath((join-path $PSScriptroot "..\.."))
$toolsDir = join-path $repoDir "Binaries\Toolset"

# Download the specified tool.  The download name needs to be a version name that
# will be different when we change the version strings above.  That way the script
# does work when the strings change vs. using the already downloaded and cached
# values.
function download-tool() {
    param ([string]$url, [string]$fileName, [string]$downloadName)

    $scratchDir = join-path $toolsDir "Scratch"
    mkdir $scratchDir -ErrorAction SilentlyContinue | out-null
    
    $filePath = join-path $toolsDir $fileName
    $downloadPath = join-path $scratchDir $downloadName

    if ((test-path $filePath) -and (test-path $downloadPath)) {
        return $false
    }
        
    write-host "Downloading $fileName"
    $webClient = New-Object -TypeName "System.Net.WebClient"
    $webClient.DownloadFile($url, $downloadPath)
    cp $downloadPath $filePath
    return $true
}

try {
    mkdir $toolsDir -ErrorAction silent | out-null
    pushd $toolsDir
    
    $nugetUrl = "https://dist.nuget.org/win-x86-commandline/v$nugetVersion/NuGet.exe"
    if (download-tool $nugetUrl "NuGet.exe" "NuGet-$nugetVersion.exe") {
        cp "NuGet.exe" $repoDir
    }

    $msbuildUrl = "https://jdashstorage.blob.core.windows.net/msbuild/msbuild-$msbuildToolsetVersion.zip" 
    if (download-tool $msbuildUrl "msbuild.zip" "msbuild-$msbuildToolsetVersion.zip") { 
        rm -re (join-path $toolsDir "msbuild") -ErrorAction SilentlyContinue
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::ExtractToDirectory((join-path $toolsDir "msbuild.zip"), $toolsDir)
    }

    exit 0
}
catch [exception] {
    write-host $_.Exception
    exit -1
}
finally {
    popd
}
