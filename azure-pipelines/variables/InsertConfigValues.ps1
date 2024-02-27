$InsertedPkgs = (& "$PSScriptRoot\..\artifacts\VSInsertion.ps1")

$icv=@()
foreach ($kvp in $InsertedPkgs.GetEnumerator()) {
    $kvp.Value |% {
        # Skip VSInsertionMetadata packages for default.config world, which doesn't use it any more.
        if (($_.Name -match "^(.*?)\.(\d+\.\d+\.\d+(?:\.\d+)?(?:-.*?)?)(?:\.symbols)?\.nupkg$") -and $_.Name -notmatch 'VSInsertionMetadata') {
            $id = $Matches[1]
            $version = $Matches[2]
            $icv += "$id=$version"
        }
    }
}

Write-Output ([string]::join(',',$icv))
