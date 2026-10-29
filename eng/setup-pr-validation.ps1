[CmdletBinding(PositionalBinding=$false)]
param (
  [string]$prNumber,
  [string]$commitSHA,
  [boolean]$enforceLatestCommit)

try {
    # name and email are only used for merge commit, it doesn't really matter what we put in there.
    git config user.name "RoslynValidation"
    git config user.email "validation@roslyn.net"

    if ($commitSHA -notmatch '\A[a-fA-F0-9]{40}\z') {
      Write-Host "##vso[task.LogIssue type=error;]The PR Commit SHA must contain exactly 40 hexadecimal characters."
      exit 1
    }
    
    git remote add gh https://github.com/dotnet/roslyn.git

    Write-Host "Getting the hash of refs/pull/$prNumber/head..."
    $remoteRef = git ls-remote gh refs/pull/$prNumber/head
    Write-Host ($remoteRef | Out-String)

    $prHeadSHA = $remoteRef.Split()[0]

    if ($enforceLatestCommit) {
      Write-Host "Validating the PR head matches the specified commit SHA ($commitSHA)..."
      if ($prHeadSHA -ne $commitSHA) {
        Write-Host "##vso[task.LogIssue type=error;]The PR's Head SHA ($prHeadSHA) does not match the specified commit SHA ($commitSHA). Unreviewed changes may have been pushed to the PR."
        exit 1
      }
    }

    Write-Host "Setting up the build for PR validation by fetching refs/pull/$prNumber/merge..."
    git fetch gh refs/pull/$prNumber/merge
    if (!$?) {
      Write-Host "##vso[task.LogIssue type=error;]Fetching ref refs/pull/$prNumber/merge failed."
      exit 1
    }

    git checkout FETCH_HEAD
    if (!$?) {
      Write-Host "##vso[task.LogIssue type=error;]Checking out FETCH_HEAD for refs/pull/$prNumber/merge failed."
      exit 1
    }

    $builtPrHead = git rev-parse 'HEAD^2'
    if ($builtPrHead -ne $commitSHA) {
      if ($enforceLatestCommit) {
        Write-Host "##vso[task.LogIssue type=error;]The fetched merge's PR head SHA ($builtPrHead) does not match the specified commit SHA ($commitSHA). Unreviewed changes may have been pushed to the PR."
        exit 1
      }

      Write-Host "Checking out the specified commit SHA ($commitSHA)..."
      git fetch gh $commitSHA
      if (!$?) {
        Write-Host "##vso[task.LogIssue type=error;]Fetching commit SHA $commitSHA failed."
        exit 1
      }
      git checkout $commitSHA
      if (!$?) {
        Write-Host "##vso[task.LogIssue type=error;]Checking out commit SHA $commitSHA failed."
        exit 1
      }
    }
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  exit 1
}
