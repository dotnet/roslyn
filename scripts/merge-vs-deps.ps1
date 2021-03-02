[CmdletBinding(PositionalBinding=$false)]
Param(
    [string]$userName,
    [string]$userEmail,
    [string]$accessToken = "")

git config user.name $userName
git config user.email $userEmail   

if ($accessToken -eq "") {
    git pull origin master-vs-deps
}
else {
    $base64Pat = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(":$accessToken"))
    git -c http.extraheader="AUTHORIZATION: Basic $base64Pat" pull origin master-vs-deps
}

if (-not $?)
{
    Write-Host "##vso[task.logissue type=error]Failed to merge master-vs-deps into source branch"
    exit 1
}