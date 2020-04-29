[CmdletBinding(PositionalBinding=$false)]
param (
  [string]$sourceBranchName,
  [string]$prNumber)

try {
    if ($sourceBranchName -notlike '*-vs-deps') {
      Write-Host  "##vso[task.LogIssue type=warning;]The base branch for insertion validation is $sourceBranchName, which is not a vs-deps branch."
    }
    Write-Host "Setting up the build for PR validation by merging refs/pull/$prNumber/merge into $sourceBranchName..."
    git pull origin refs/pull/$prNumber/merge
    if (!$?) {
      Write-Host "##vso[task.LogIssue type=error;]Merging branch refs/pull/$prNumber/merge failed."
      exit 1
    }
    Write-Host "Getting the hash of refs/pull/$prNumber/head..."
    Write-Host ((git ls-remote origin refs/pull/$prNumber/head) | Out-String)
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  exit 1
}
