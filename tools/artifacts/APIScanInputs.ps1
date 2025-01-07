$inputs = & "$PSScriptRoot/symbols.ps1"

if (!$inputs) { return }

# Filter out specific files that target OS's that are not subject to APIScan.
# Files that are subject but are not supported must be scanned and an SEL exception filed.
$outputs = @{}
$forbiddenSubPaths = @(
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
