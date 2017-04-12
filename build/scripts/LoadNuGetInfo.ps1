set-strictmode -version 2.0
$ErrorActionPreference="Stop"

& CMD /c "$PSScriptRoot\LoadNuGetInfo.cmd && set" | .{process{
    if ($_ -match '^(NuGet[^=]+)=(.*)') {
        Set-Variable $matches[1] $matches[2]
    }
}}
