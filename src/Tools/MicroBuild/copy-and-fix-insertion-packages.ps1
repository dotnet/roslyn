Param(
    [string]$binariesPath = $null,
    [string]$modifyVsixToolPath = $null
)

set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    $items = @(
        "Roslyn.VisualStudio.Setup.vsix",
        "Roslyn.VisualStudio.Setup.Next.vsix")
    foreach ($item in $items) {
        $baseFileName = [System.IO.Path]::GetFileNameWithoutExtension($item)
        $baseExtension = [System.IO.Path]::GetExtension($item)
        $newInsertionName = $baseFileName + ".Insertion" + $baseExtension
        $sourceVsix = join-path $binariesPath $item
        $destinationVsix = join-path $binariesPath $newInsertionName
        copy $sourceVsix $destinationVsix
        & $modifyVsixToolPath --vsix=$destinationVsix --remove=//x:PackageManifest/x:Installation/@Experimental --add-attribute=//x:PackageManifest/x:Installation`;InstalledByMSI`;true
    }

    exit 0
}
catch [exception]
{
    write-host $_.Exception
    exit -1
}
