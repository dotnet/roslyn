# Examine an MSBuild log and issue an error if any MSBulid warnings are discovered.

param ([string]$logFile = $(throw "Need a build log file"))
$ErrorActionPreference="Stop"

try
{
    $exitCode = 0
    $pattern = "\bwarning\b\s+\bMSB(\d+):(.*)"
    foreach ($line in gc $logFile) {
        $m = [regex]::match($line, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($m.Success) {
            $num = $m.Groups[1].Value
            $text = $m.Groups[2].Value
            $location = $line.Substring(0, $m.Index)
            write-host ("error MSB{0}: {1} -> {2}" -f $num, $text, $location)
            write-host "Original MSB warning line below"
            write-host $line
            $exitCode = 1
        }
    }

    exit $exitCode
}
catch
{
    write-host "Error: $($_.Exception.Message)"
    exit 1
}

