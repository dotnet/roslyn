# This doesn't work off Windows, nor do we need to convert symbols on multiple OS agents
if ($IsMacOS -or $IsLinux) {
    return;
}

$BinPath = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\bin")
if (!(Test-Path $BinPath)) { return }
$symbolfiles = & "$PSScriptRoot\..\Get-SymbolFiles.ps1" -Path $BinPath -Tests | Get-Unique

@{
    "$BinPath" = $SymbolFiles;
}
