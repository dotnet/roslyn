git pull origin master-vs-deps
if (-not $?)
{
    Write-Host "##vso[task.logissue type=error]Failed to merge master-vs-deps into source branch"
    exit 1
}