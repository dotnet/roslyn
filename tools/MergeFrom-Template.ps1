
<#
.SYNOPSIS
    Merges the latest changes from Library.Template into HEAD of this repo.
.PARAMETER LocalBranch
    The name of the local branch to create at HEAD and use to merge into from Library.Template.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
Param(
    [string]$LocalBranch = "dev/$($env:USERNAME)/libtemplateUpdate"
)

Function Spawn-Tool($command, $commandArgs, $workingDirectory, $allowFailures) {
    if ($workingDirectory) {
        Push-Location $workingDirectory
    }
    try {
        if ($env:TF_BUILD) {
            Write-Host "$pwd >"
            Write-Host "##[command]$command $commandArgs"
        }
        else {
            Write-Host "$command $commandArgs" -ForegroundColor Yellow
        }
        if ($commandArgs) {
            & $command @commandArgs
        } else {
            Invoke-Expression $command
        }
        if ((!$allowFailures) -and ($LASTEXITCODE -ne 0)) { exit $LASTEXITCODE }
    }
    finally {
        if ($workingDirectory) {
            Pop-Location
        }
    }
}

$remoteBranch = & $PSScriptRoot\Get-LibTemplateBasis.ps1 -ErrorIfNotRelated
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$LibTemplateUrl = 'https://github.com/aarnott/Library.Template'
Spawn-Tool 'git' ('fetch', $LibTemplateUrl, $remoteBranch)
$SourceCommit = Spawn-Tool 'git' ('rev-parse', 'FETCH_HEAD')
$BaseBranch = Spawn-Tool 'git' ('branch', '--show-current')
$SourceCommitUrl = "$LibTemplateUrl/commit/$SourceCommit"

# To reduce the odds of merge conflicts at this stage, we always move HEAD to the last successful merge.
$basis = Spawn-Tool 'git' ('rev-parse', 'HEAD') # TODO: consider improving this later

Write-Host "Merging the $remoteBranch branch of Library.Template ($SourceCommit) into local repo $basis" -ForegroundColor Green

Spawn-Tool 'git' ('checkout', '-b', $LocalBranch, $basis) $null $true
if ($LASTEXITCODE -eq 128) {
    Spawn-Tool 'git' ('checkout', $LocalBranch)
    Spawn-Tool 'git' ('merge', $basis)
}

Spawn-Tool 'git' ('merge', 'FETCH_HEAD', '--no-ff', '-m', "Merge the $remoteBranch branch from $LibTemplateUrl`n`nSpecifically, this merges [$SourceCommit from that repo]($SourceCommitUrl).")
if ($LASTEXITCODE -eq 1) {
    Write-Error "Merge conflict detected. Manual resolution required."
    exit 1
}
elseif ($LASTEXITCODE -ne 0) {
    Write-Error "Merge failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

$result = New-Object PSObject -Property @{
    BaseBranch   = $BaseBranch   # The original branch that was checked out when the script ran.
    LocalBranch  = $LocalBranch  # The name of the local branch that was created before the merge.
    SourceCommit = $SourceCommit # The commit from Library.Template that was merged in.
    SourceBranch = $remoteBranch # The branch from Library.Template that was merged in.
}

Write-Host $result
Write-Output $result
