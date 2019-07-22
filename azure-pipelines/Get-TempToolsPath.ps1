if ($env:AGENT_TOOLSDIRECTORY) {
    $path = "$env:AGENT_TOOLSDIRECTORY\vs-platform\tools"
} elseif ($env:localappdata) {
    $path = "$env:localappdata\vs-platform\tools"
} else {
    $path = "$PSScriptRoot\..\obj\tools"
}

if (!(Test-Path $path)) {
    New-Item -ItemType Directory -Path $Path | Out-Null
}

(Resolve-Path $path).Path
