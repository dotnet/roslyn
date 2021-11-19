#!/usr/bin/env pwsh

# This script returns all the artifacts that should be collected after a build.
#
# Each powershell artifact is expressed as an object with these properties:
#  Source          - the full path to the source file
#  ArtifactName    - the name of the artifact to upload to
#  ContainerFolder - the relative path within the artifact in which the file should appear
#
# Each artifact aggregating .ps1 script should return a hashtable:
#   Key = path to the directory from which relative paths within the artifact should be calculated
#   Value = an array of paths (absolute or relative to the BaseDirectory) to files to include in the artifact.
#           FileInfo objects are also allowed.

$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")

Function EnsureTrailingSlash($path) {
    if ($path.length -gt 0 -and !$path.EndsWith('\') -and !$path.EndsWith('/')) {
        $path = $path + [IO.Path]::DirectorySeparatorChar
    }

    $path.Replace('\', [IO.Path]::DirectorySeparatorChar)
}

Get-ChildItem "$PSScriptRoot\*.ps1" -Exclude "_*" -Recurse |% {
    $ArtifactName = $_.BaseName

    $totalFileCount = 0
    $fileGroups = & $_
    if ($fileGroups) {
        $fileGroups.GetEnumerator() | % {
            $BaseDirectory = New-Object Uri ((EnsureTrailingSlash $_.Key.ToString()), [UriKind]::Absolute)
            $_.Value | % {
                if ($_.GetType() -eq [IO.FileInfo] -or $_.GetType() -eq [IO.DirectoryInfo]) {
                    $_ = $_.FullName
                }

                $artifact = New-Object -TypeName PSObject
                Add-Member -InputObject $artifact -MemberType NoteProperty -Name ArtifactName -Value $ArtifactName

                $SourceFullPath = New-Object Uri ($BaseDirectory, $_)
                Add-Member -InputObject $artifact -MemberType NoteProperty -Name Source -Value $SourceFullPath.LocalPath

                $RelativePath = [Uri]::UnescapeDataString($BaseDirectory.MakeRelative($SourceFullPath))
                Add-Member -InputObject $artifact -MemberType NoteProperty -Name ContainerFolder -Value (Split-Path $RelativePath)

                Write-Output $artifact
                $totalFileCount += 1
            }
        }
    }

    if ($totalFileCount -eq 0) {
        Write-Warning "No files found for the `"$ArtifactName`" artifact."
    }
}
