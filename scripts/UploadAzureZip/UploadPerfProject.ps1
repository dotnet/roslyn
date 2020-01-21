$AzCopyLoc = Join-Path ${env:ProgramFiles(x86)} "Microsoft SDKs\Azure\AzCopy\AzCopy.exe"

If (-Not (Test-Path $AzCopyLoc)) {
    echo "Azure Copy could not be found.  Download and install here:"
    echo "http://aka.ms/downloadazcopy"
    exit 1
}

$MyDir = Get-Location
$ZipName = Read-Host 'what is the name of the folder that you want to zip?'
$Version = Read-Host 'which version of the zip file is this?'
$AzureKey = Read-Host 'what is the azure key?'

$DirToZipPath = "$MyDir\$ZipName"
$ZipName = "$ZipName.$Version.zip"
$ZipPath = "$MyDir\$ZipName"

echo "Zipping $DirToZipPath into $ZipPath"

Add-Type -Assembly "System.IO.Compression.FileSystem";
[System.IO.Compression.ZipFile]::CreateFromDirectory("$DirToZipPath", "$ZipPath", "Fastest", $true);

echo "Uploading $ScriptDir/$NugetZipName"

& $AzCopyLoc /Source:"$MyDir" "/Dest:https://dotnetci.blob.core.windows.net/roslyn-perf" /DestKey:$AzureKey /Pattern:$ZipName
