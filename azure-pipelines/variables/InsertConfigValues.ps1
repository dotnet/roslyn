$BinPath = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\bin\Packages\$env:BUILDCONFIGURATION")

$icv=@()
if (Test-Path "$BinPath") {
    Get-ChildItem -Path "$BinPath\*.nupkg" -rec |% {
        if ($_.Name -match "^(.*)\.(\d+\.\d+\.\d+(?:-.*?)?)(?:\.symbols)?\.nupkg$") {
            $id = $Matches[1]
            $version = $Matches[2]
            $icv += "$id=$version"
        }
    }
}

Write-Output ([string]::join(',',$icv))
