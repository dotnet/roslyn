

$toolsDir = join-path $PSScriptRoot "..\..\..\Binaries\Temp"
mkdir $toolsDir -ErrorAction SilentlyContinue | out-null
$zipPath = join-path $toolsDir "msbuild.zip"
Invoke-WebRequest -Uri "https://jdashstorage.blob.core.windows.net/msbuild/msbuild-0.1.0-alpha.zip" -OutFile $zipPath

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $toolsDir)
