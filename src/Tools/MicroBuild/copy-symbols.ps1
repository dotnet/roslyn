# Copy our symbols to the Symbols directory.  This simplifies the logic of our achiving 
# and indexing steps.  
Param(
    [string]$binariesPath = $null
)
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    $symbolsPath = join-path $binariesPath "Symbols"
    mkdir $symbolsPath -ErrorAction SilentlyContinue | out-null

    $signToolDataPath = join-path $PSScriptRoot "..\..\..\build\config\SignToolData.json"
    $json = gc -raw $signToolDataPath | ConvertFrom-Json
    foreach ($block in $json.sign) {
        foreach ($fileName in $block.values) {
            $ext = [IO.Path]::GetExtension($fileName)
            if ($ext -eq ".dll" -or $ext -eq ".exe") {
                $filePath = join-path $binariesPath $fileName
                cp $filePath $symbolSPath

                $pdbPath = [IO.Path]::ChangeExtension($filePath, "pdb")
                cp $pdbPath $symbolsPath
            }
        }
    }

    exit 0
}
catch [exception]
{
    write-host $_.Exception
    exit -1
}
