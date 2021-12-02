$Path = "$PSScriptRoot\..\..\bin"
if (Test-Path $Path) {
    [string]::join(';', (& "$PSScriptRoot\..\Get-SymbolFiles.ps1" -ConvertToWindowsPDBs:$false -Path $Path))
}
