if ($env:AGENT_TEMPDIRECTORY) {
    $path = "$env:AGENT_TEMPDIRECTORY\$env:BUILD_BUILDID"
} elseif ($env:localappdata) {
    $path = "$env:localappdata\gitrepos\tools"
} else {
    $path = "$PSScriptRoot\..\obj\tools"
}

if (!(Test-Path $path)) {
    New-Item -ItemType Directory -Path $Path | Out-Null
}

(Resolve-Path $path).Path
