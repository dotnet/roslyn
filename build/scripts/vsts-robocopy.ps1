# See https://raw.githubusercontent.com/Microsoft/vsts-tasks/master/Tasks/PublishBuildArtifactsV1/Invoke-Robocopy.ps1

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Source,

    [Parameter(Mandatory = $true)]
    [string]$Target,

    [Parameter(Mandatory = $true)]
    [int]$ParallelCount,

    [Parameter(Mandatory = $false)]
    [string]$File,
    
    [Parameter(Mandatory = $false)]
    [string[]]$Exclude)

# This script translates the output from robocopy into UTF8. Node has limited
# built-in support for encodings.
#
# Robocopy uses the system default code page. The system default code page varies
# depending on the locale configuration. On an en-US box, the system default code
# page is Windows-1252.
#
# Note, on a typical en-US box, testing with the 'ç' character is a good way to
# determine whether data is passed correctly between processes. This is because
# the 'ç' character has a different code point across each of the common encodings
# on a typical en-US box, i.e.
#   1) the default console-output code page (IBM437)
#   2) the system default code page (i.e. CP_ACP) (Windows-1252)
#   3) UTF8

$ErrorActionPreference = 'Stop'

# Redefine the wrapper over STDOUT to use UTF8. Node expects UTF8 by default.
$stdout = [System.Console]::OpenStandardOutput()
$utf8 = New-Object System.Text.UTF8Encoding($false) # do not emit BOM
$writer = New-Object System.IO.StreamWriter($stdout, $utf8)
[System.Console]::SetOut($writer)

# All subsequent output must be written using [System.Console]::WriteLine(). In
# PowerShell 4, Write-Host and Out-Default do not consider the updated stream writer.

if (!$File) {
    $File = "*";
}

# Print the ##command. The /MT parameter is only supported on 2008 R2 and higher.
if ($ParallelCount -gt 1) {
    [System.Console]::WriteLine("##[command]robocopy.exe /E /COPY:DA /NP /R:3 /MT:$ParallelCount `"$Source`" `"$Target`" `"$File`"")
}
else {
    [System.Console]::WriteLine("##[command]robocopy.exe /E /COPY:DA /NP /R:3 `"$Source`" `"$Target`" `"$File`"")
}

# The $OutputEncoding variable instructs PowerShell how to interpret the output
# from the external command.
$OutputEncoding = [System.Text.Encoding]::Default

$ExcludeArg = ""
if (($null -ne $Exclude) -and ($Exclude.Length -gt 0)) {
    $ExcludeArg = "/XD "
    foreach ($e in $Exclude) {
        $ExcludeArg += "$e "
    }
}

#             Usage :: ROBOCOPY source destination [file [file]...] [options]
#            source :: Source Directory (drive:\path or \\server\share\path).
#       destination :: Destination Dir  (drive:\path or \\server\share\path).
#              file :: File(s) to copy  (names/wildcards: default is "*.*").
#                /E :: copy subdirectories, including Empty ones.
# /COPY:copyflag[s] :: what to COPY for files (default is /COPY:DAT).
#                      (copyflags : D=Data, A=Attributes, T=Timestamps).
#                      (S=Security=NTFS ACLs, O=Owner info, U=aUditing info).
#               /NP :: No Progress - don't display percentage copied.
#			/MT[:n] :: Do multi-threaded copies with n threads (default 8).
#                       n must be at least 1 and not greater than 128.
#                       This option is incompatible with the /IPG and /EFSRAW options.
#                      Redirect output using /LOG option for better performance.
#              /R:n :: number of Retries on failed copies: default 1 million.
#
# Note, the output from robocopy needs to be iterated over. Otherwise PowerShell.exe
# will launch the external command in such a way that it inherits the streams.
#
# Note, the /MT parameter is only supported on 2008 R2 and higher.
if ($ParallelCount -gt 1) {
    & robocopy.exe /E /COPY:DA /NP /R:3 /MT:$ParallelCount $Source $Target $File $ExcludeArg 2>&1 |
        ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) {
            [System.Console]::WriteLine($_.Exception.Message)
        }
        else {
            [System.Console]::WriteLine($_)
        }
    }
}
else {
    & robocopy.exe /E /COPY:DA /NP /R:3 $Source $Target $File $ExcludeArg 2>&1 |
        ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) {
            [System.Console]::WriteLine($_.Exception.Message)
        }
        else {
            [System.Console]::WriteLine($_)
        }
    }
}

[System.Console]::WriteLine("##[debug]robocopy exit code '$LASTEXITCODE'")
[System.Console]::Out.Flush()
if ($LASTEXITCODE -ge 8) {
    exit $LASTEXITCODE
}

exit 0
