$inputs = & "$PSScriptRoot/symbols.ps1"

if (!$inputs) { return }

# Filter out specific files that APIScan does not support.
# Specifically, APIScan doesn't support Windows ARM64 binaries, nor linux/OSX binaries.
$outputs = @{}
$forbiddenSubPaths = @(
    , 'arm64'
    , 'win-arm64'
    , 'linux-*'
    , 'osx*'
)

$inputs.GetEnumerator() | % {
    $list = $_.Value | ? {
        $path = $_.Replace('\', '/')
        return !($forbiddenSubPaths | ? { $path -like "*/$_/*" })
    }
    $outputs[$_.Key] = $list
}


$outputs
