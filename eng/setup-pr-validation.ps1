[CmdletBinding(PositionalBinding=$false)]
param (
  [string]$sourceBranchName,
  [string]$prNumber,
  [string]$commitSHA)

try {
    # name and email are only used for merge commit, it doesn't really matter what we put in there.
    git config user.name "RoslynValidation"
    git config user.email "validation@roslyn.net"

    if ($sourceBranchName -notlike '*-vs-deps') {
      Write-Host  "##vso[task.LogIssue type=warning;]The base branch for insertion validation is $sourceBranchName, which is not a vs-deps branch."
    }

    Write-Host "Validating the PR head matches the specified commit SHA ($commitSHA)..."
    if ($commitSHA.Length -lt 7) {
      Write-Host "##vso[task.LogIssue type=error;]The PR Commit SHA must be at least 7 characters long."
      exit 1
    }

    Write-Host "Getting the hash of refs/pull/$prNumber/head..."
    $remoteRef = git ls-remote origin refs/pull/$prNumber/head
    Write-Host ($remoteRef | Out-String)

    Write-Host "Setting up the build for PR validation by merging refs/pull/$prNumber/merge into $sourceBranchName..."
    git pull origin refs/pull/$prNumber/merge
    if (!$?) {
      Write-Host "##vso[task.LogIssue type=error;]Merging branch refs/pull/$prNumber/merge failed."
      exit 1
    }

    Write-Host "Checking out the specified commit SHA ($commitSHA)..."
    git checkout $commitSHA
    if (!$?) {
      Write-Host "##vso[task.LogIssue type=error;]Checking out commit SHA $commitSHA failed."
      exit 1
    }
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  exit 1
}
