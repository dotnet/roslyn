$ScriptDir = $PSScriptRoot
$AzCopyLoc = Join-Path ${env:ProgramFiles(x86)} "Microsoft SDKs\Azure\AzCopy\AzCopy.exe"
$RestoreScriptLoc = Join-Path $ScriptDir \..\..\..\..\Restore.cmd
$NugetExeLoc = Join-Path $ScriptDir \..\..\..\nuget.exe

If (-Not (Test-Path $AzCopyLoc)) {
    echo "Azure Copy could not be found.  Download and install here:"
    echo "http://aka.ms/downloadazcopy"
    exit 1
}

if (-Not (test-path $RestoreScriptLoc)) {
    echo "You must run this script from inside of roslyn-internal"
    echo "looking for $RestoreScriptLoc"
    exit 2
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

& $NugetExeLoc locals all -clear
if ($error.count -eq 0) {
    exit 3
}

echo ""
echo "==============================================="
echo "=       Restoring nuget to fill cache"
echo "==============================================="
echo ""

& $RestoreScriptLoc 
if ($error.count -eq 0) {
    exit 3
}

echo ""
echo "==============================================="
echo "=       Zipping $HOME/.nuget into $ScriptDir/$NugetZipName "
echo "==============================================="
echo ""

Add-Type -Assembly "System.IO.Compression.FileSystem";
[System.IO.Compression.ZipFile]::CreateFromDirectory("$HOME/.nuget", "$ScriptDir/$NugetZipName", "Fastest", $true);
if ($error.count -eq 0) {
    exit 4
}

echo "Done"

echo ""
echo "==============================================="
echo "=       Uploading $ScriptDir/$NugetZipName"
echo "==============================================="
echo ""

& $AzCopyLoc /Source:$ScriptDir /Dest:https://dotnetci.blob.core.windows.net/roslyn /DestKey:$AzureKey /Pattern:$NugetZipName
