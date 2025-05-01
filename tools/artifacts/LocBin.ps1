# Identify LCE files and the binary files they describe
$BinRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\bin")
if (!(Test-Path $BinRoot))  { return }

$FilesToCopy = @()
$FilesToCopy += Get-ChildItem -Recurse -File -Path $BinRoot |? { $_.FullName -match '\\Localize\\' }

Get-ChildItem -rec "$BinRoot\*.lce" -File | % {
    $FilesToCopy += $_
    $FilesToCopy += $_.FullName.SubString(0, $_.FullName.Length - 4)
}

$FilesToCopy += Get-ChildItem -rec "$BinRoot\*.lcg" -File | % { [xml](Get-Content $_) } | % { $_.lcx.name }

@{
    "$BinRoot" = $FilesToCopy;
}
