$BinPath = [System.IO.Path]::GetFullPath("$PSScriptRoot/../../bin")
$symbolfiles = & "$PSScriptRoot/../Get-SymbolFiles.ps1" -Path $BinPath | Get-Unique

@{
    "$BinPath" = $SymbolFiles;
}
