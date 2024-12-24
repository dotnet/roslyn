$BinPath = [System.IO.Path]::GetFullPath("$PSScriptRoot/../../bin")
if (!(Test-Path $BinPath)) { return }
$symbolfiles = & "$PSScriptRoot/../Get-SymbolFiles.ps1" -Path $BinPath -Tests | Get-Unique

@{
    "$BinPath" = $SymbolFiles;
}
