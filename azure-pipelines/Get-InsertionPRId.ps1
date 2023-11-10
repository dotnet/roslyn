<#
.SYNOPSIS
    Look up the pull request URL of the insertion PR.
#>
$stagingFolder = $env:BUILD_STAGINGDIRECTORY
if (!$stagingFolder) {
    $stagingFolder = $env:SYSTEM_DEFAULTWORKINGDIRECTORY
    if (!$stagingFolder) {
        Write-Error "This script must be run in an Azure Pipeline."
        exit 1
    }
}
$markdownFolder = Join-Path $stagingFolder (Join-Path 'MicroBuild' 'Output')
$markdownFile = Join-Path $markdownFolder 'PullRequestUrl.md'
if (!(Test-Path $markdownFile)) {
    Write-Error "This script should be run after the MicroBuildInsertVsPayload task."
    exit 2
}

$insertionPRUrl = Get-Content $markdownFile
if (!($insertionPRUrl -match 'https:.+?/pullrequest/(\d+)')) {
    Write-Error "Failed to parse pull request URL: $insertionPRUrl"
    exit 3
}

$Matches[1]
