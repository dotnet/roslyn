# Examine an MSBuild log and issue an error if any MSBulid warnings are discovered.

param ([string]$logFile = $(throw "Need a build log file"))
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    $exitCode = 0
    $pattern = "\bwarning\b\s+\bMSB(\d+):(.*)"
    $ignoreCase = [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
    $reader = new-object "System.IO.StreamReader" -argumentList $logFile
    while ($true) {
        $line = $reader.ReadLine()
        if ($line -eq $null) {
            break;
        }

        if (-not $line.Contains("warning")) {
            continue;
        }

        $m = [regex]::Match($line, $pattern, $ignoreCase)
        if ($m.Success) {
            $num = $m.Groups[1].Value
            $text = $m.Groups[2].Value
            $location = $line.Substring(0, $m.Index)

            # MSBuild race conditions.  Doesn't break build so don't promote to error for now
            #
            # https://github.com/dotnet/roslyn/issues/10116
            if ($num -eq 3491 -and [regex]::Match($text, ".*Could not write lines to file.*Portable", $ignoreCase)) {
                continue
            }

            if ($num -eq 3026) {
                continue
            }

            write-host ("error MSB{0}: {1} -> {2}" -f $num, $text, $location)
            write-host "Original MSB warning line below"
            write-host $line
            $exitCode = 1
        }
    }

    $reader.Close()
    exit $exitCode
}
catch
{
    write-host "Error: $($_.Exception.Message)"
    exit 1
}

