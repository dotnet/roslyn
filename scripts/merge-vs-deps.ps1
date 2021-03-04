[CmdletBinding(PositionalBinding=$false)]
Param([string]$accessToken = "")

# name and email are only used for merge commit, it doesn't really matter what we put in there.    
git config user.name "RoslynValidation"
git config user.email "validation@roslyn.net"   

if ($accessToken -eq "") {
    git pull origin main-vs-deps
}
else {
    $base64Pat = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(":$accessToken"))
    git -c http.extraheader="AUTHORIZATION: Basic $base64Pat" pull origin main-vs-deps
}

if (-not $?)
{
    Write-Host "##vso[task.logissue type=error]Failed to merge main-vs-deps into source branch"
    exit 1
}