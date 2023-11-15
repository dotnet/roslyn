# This is a list of AzDO account names or email addresses.
# Add your team DL and/or whoever should be notified of insertion PRs.
$contacts = ,$env:BUILD_REQUESTEDFOREMAIL

if (Test-Path "$PSScriptRoot\TeamEmail.ps1") {
    $contacts += & "$PSScriptRoot\TeamEmail.ps1"
}

$contacts = $contacts |? { $_ }
if ($contacts) {
    [string]::Join(',', $contacts)
}
