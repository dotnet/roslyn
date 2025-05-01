$InsertedPkgs = (& "$PSScriptRoot\..\artifacts\VSInsertion.ps1")

$icv=@()
foreach ($kvp in $InsertedPkgs.GetEnumerator()) {
    $kvp.Value |% {
        if ($_.Name -match "^(.*?)\.(\d+\.\d+\.\d+(?:\.\d+)?(?:-.*?)?)(?:\.symbols)?\.nupkg$") {
            $id = $Matches[1]
            $version = $Matches[2]
            $icv += "$id=$version"
        }
    }
}

Write-Output ([string]::join(',',$icv))
