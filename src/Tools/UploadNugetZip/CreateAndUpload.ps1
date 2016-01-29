$ScriptDir = $PSScriptRoot
$AzCopyLoc = Join-Path ${env:ProgramFiles(x86)} "Microsoft SDKs\Azure\AzCopy\AzCopy.exe"

If (-Not (Test-Path $AzCopyLoc)) {
    echo "Azure Copy could not be found.  Download and install here:"
    echo "http://aka.ms/downloadazcopy"
    exit 1
}

$Branch = Read-Host 'which branch is this for? [Master|Future]'
$Version = Read-Host 'which version of the zip file is this?'
$AzureKey = Read-Host 'what is the azure key?'

$MaybeFuture = ""
If ($Branch -eq "future") {
    $MaybeFuture = ".future"
}

$NugetZipName = "nuget$MaybeFuture.$Version.zip"

echo "==============================================="
echo "=           Clearing nuget caches"
echo "==============================================="
echo ""

 & $ScriptDir\..\..\..\nuget.exe locals all -clear

echo ""
echo "==============================================="
echo "=       Restoring nuget to fill cache"
echo "==============================================="
echo ""

 & $ScriptDir\..\..\..\Restore.cmd

echo ""
echo "==============================================="
echo "=       Zipping $HOME/.nuget into $ScriptDir/$NugetZipName "
echo "==============================================="
echo ""

 Add-Type -Assembly "System.IO.Compression.FileSystem";
 [System.IO.Compression.ZipFile]::CreateFromDirectory("$HOME/.nuget", "$ScriptDir/$NugetZipName");

echo "Done"

echo ""
echo "==============================================="
echo "=       Uploading $ScriptDir/$NugetZipName"
echo "==============================================="
echo ""

& $AzCopyLoc /Source:$ScriptDir /Dest:https://dotnetci.blob.core.windows.net/roslyn /DestKey:$AzureKey /Pattern:$NugetZipName

