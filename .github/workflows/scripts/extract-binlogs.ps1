#!/usr/bin/env pwsh
# Extract *.binlog entries from one Azure DevOps build-log artifact.
#
# The archive is produced by a PR-triggered build, so its entry paths, metadata
# and contents are untrusted. Two properties keep extraction safe:
#
#   * destination names are generated here, never taken from the archive, so a
#     traversal or absolute path cannot choose where bytes land;
#   * writing stops as soon as the caller's remaining byte budget is exceeded,
#     so a zip bomb cannot fill the runner disk.
#
# Entry paths and types are still validated up front - an archive containing a
# traversal path or a link/device entry is hostile rather than merely odd, so
# the whole artifact is rejected instead of partially extracted.
#
# Usage: extract-binlogs.ps1 <archive> <destination> <prefix> <budget-bytes> [label]
# Prints "<extracted-count> <written-bytes>".

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Archive,
    [Parameter(Mandatory = $true)][string]$Destination,
    [Parameter(Mandatory = $true)][string]$Prefix,
    [Parameter(Mandatory = $true)][long]$BudgetBytes,
    [Parameter(Mandatory = $false)][string]$Label = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ChunkSize = 1024 * 1024

# S_IFMT mask and the only entry types a log artifact may legitimately contain.
$FileTypeMask = 0xF000
$RegularFile = 0x8000
$DirectoryType = 0x4000

# Artifact names are untrusted build metadata, so re-sanitize here rather than
# trusting the caller: only the destination name generated in this process may
# decide where bytes land.
function Get-SafeLabel([string]$Value) {
    $chars = $Value.ToCharArray() | ForEach-Object {
        if (($_ -ge 'a' -and $_ -le 'z') -or ($_ -ge 'A' -and $_ -le 'Z') -or
            ($_ -ge '0' -and $_ -le '9') -or $_ -eq '.' -or $_ -eq '_' -or $_ -eq '-') { $_ } else { '_' }
    }
    $result = (-join $chars).Trim('.', '_', '-')
    if ($result.Length -gt 80) { $result.Substring(0, 80) } else { $result }
}

function Test-UnsafePath([string]$Name) {
    if ($Name.Contains([char]0)) { return $true }

    $normalized = $Name.Replace('\', '/')
    if ($normalized.StartsWith('/')) { return $true }

    $parts = $normalized.Split('/')
    if ($parts -contains '..') { return $true }

    # A Windows drive spec such as "c:/foo" or "c:foo" is not rooted by POSIX
    # rules but is still an attempt to escape the destination.
    $first = if ($parts.Length -gt 0) { $parts[0] } else { '' }
    return ($first.Length -ge 2 -and [char]::IsAsciiLetter($first[0]) -and $first[1] -eq ':')
}

function Test-UnsupportedType($Entry) {
    # Entries written on Windows carry no Unix mode; 0 means "unspecified",
    # which is not evidence of a hostile type.
    $mode = ($Entry.ExternalAttributes -shr 16) -band 0xFFFF
    $fileType = $mode -band $FileTypeMask
    return ($fileType -ne 0 -and $fileType -ne $RegularFile -and $fileType -ne $DirectoryType)
}

function Test-DirectoryEntry($Entry) {
    return ($Entry.FullName.EndsWith('/') -or $Entry.Name.Length -eq 0)
}

$safeLabel = if ($Label) { Get-SafeLabel $Label } else { '' }
$zip = $null

try {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($Archive)

    # Validate every entry before reading any payload.
    for ($i = 0; $i -lt $zip.Entries.Count; $i++) {
        $entry = $zip.Entries[$i]
        if (Test-UnsafePath $entry.FullName) {
            [Console]::Error.WriteLine("archive entry $i has an unsafe path")
            exit 1
        }
        if (Test-UnsupportedType $entry) {
            [Console]::Error.WriteLine("archive entry $i has an unsupported type")
            exit 1
        }
    }

    $selected = @($zip.Entries | Where-Object {
        -not (Test-DirectoryEntry $_) -and $_.FullName.EndsWith('.binlog', 'OrdinalIgnoreCase')
    })

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    $written = [long]0
    $buffer = [byte[]]::new($ChunkSize)

    for ($index = 0; $index -lt $selected.Count; $index++) {
        $stem = if ($safeLabel) { "${Prefix}_${index}_${safeLabel}" } else { "${Prefix}_${index}" }
        $target = Join-Path $Destination "$stem.binlog"

        $source = $selected[$index].Open()
        try {
            # CreateNew, so a name that somehow already exists is an error rather
            # than a silent overwrite of a previous artifact's binlog.
            $output = [System.IO.FileStream]::new($target, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write)
            try {
                while (($read = $source.Read($buffer, 0, $buffer.Length)) -gt 0) {
                    $written += $read
                    if ($written -gt $BudgetBytes) {
                        [Console]::Error.WriteLine('extracted binlogs exceed the remaining budget')
                        exit 1
                    }
                    $output.Write($buffer, 0, $read)
                }
            }
            finally { $output.Dispose() }
        }
        finally { $source.Dispose() }
    }

    [Console]::Out.WriteLine("$($selected.Count) $written")
    exit 0
}
catch [System.IO.InvalidDataException] {
    [Console]::Error.WriteLine("archive could not be read: $($_.Exception.Message)")
    exit 1
}
finally {
    if ($null -ne $zip) { $zip.Dispose() }
}
