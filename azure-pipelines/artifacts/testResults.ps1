[CmdletBinding()]
Param(
)

$result = @{}

$testRoot = Resolve-Path "$PSScriptRoot\..\..\test"
$result[$testRoot] = (Get-ChildItem "$testRoot\TestResults" -Recurse -Directory | Get-ChildItem -Recurse -File)

$testlogsPath = "$env:BUILD_ARTIFACTSTAGINGDIRECTORY\test_logs"
if (Test-Path $testlogsPath) {
    $result[$testlogsPath] = Get-ChildItem "$testlogsPath\*";
}

$result
