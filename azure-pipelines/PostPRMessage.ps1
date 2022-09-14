[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory=$true)]
    $AccessToken,
    [Parameter(Mandatory=$true)]
    $Markdown,
    [ValidateSet('Active','ByDesign','Closed','Fixed','Pending','Unknown','WontFix')]
    $CommentState='Active'
)

# See https://docs.microsoft.com/en-us/dotnet/api/microsoft.teamfoundation.sourcecontrol.webapi.commentthreadstatus?view=azure-devops-dotnet
if ($CommentState -eq 'Active') {
    $StatusCode = 1
} elseif ($CommentState -eq 'ByDesign') {
    $StatusCode = 5
} elseif ($CommentState -eq 'Closed') {
    $StatusCode = 4
} elseif ($CommentState -eq 'Fixed') {
    $StatusCode = 2
} elseif ($CommentState -eq 'Pending') {
    $StatusCode = 6
} elseif ($CommentState -eq 'Unknown') {
    $StatusCode = 0
} elseif ($CommentState -eq 'WontFix') {
    $StatusCode = 3
}

# Build the JSON body up
$body = ConvertTo-Json @{
    comments = @(@{
        parentCommentId = 0
        content = $Markdown
        commentType = 1
    })
    status = $StatusCode
}

Write-Verbose "Posting JSON payload: `n$Body"

# Post the message to the Pull Request
# https://docs.microsoft.com/en-us/rest/api/azure/devops/git/pull%20request%20threads?view=azure-devops-rest-5.1
$url = "$($env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI)$env:SYSTEM_TEAMPROJECTID/_apis/git/repositories/$($env:BUILD_REPOSITORY_NAME)/pullRequests/$($env:SYSTEM_PULLREQUEST_PULLREQUESTID)/threads?api-version=5.1"
if ($PSCmdlet.ShouldProcess($url, 'Post comment via REST call')) {
    try {
        if (!$env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI) {
            Write-Error "Posting to the pull request requires that the script is running in an Azure Pipelines context."
            exit 1
        }
        Write-Host "Posting PR comment to: $url"
        Invoke-RestMethod -Uri $url -Method POST -Headers @{Authorization = "Bearer $AccessToken"} -Body $Body -ContentType application/json
    }
    catch {
        Write-Error $_
        Write-Error $_.Exception.Message
        exit 2
    }
}
