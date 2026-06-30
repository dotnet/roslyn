<#
.SYNOPSIS
Downloads and inventories a Visual Studio feedback work item.

.DESCRIPTION
Fetches Azure DevOps work item data, comments, optional history, Developer Community
conversation, VS Feedback diagnostics, and attachment files. It mirrors the
TypeScript FeedbackHandler flow: normal work-item APIs use an Azure DevOps AAD
bearer token, while the VS Feedback diagnostics API requires a VSS app token from
the feedback work item's Diagnostics iframe.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [int]$WorkItemId,

    [string]$OutputDirectory,

    [string]$OrganizationUrl = 'https://devdiv.visualstudio.com',

    [string]$Project = 'DevDiv',

    [string]$AzdoBearerToken,

    [string]$VssToken,

    [string]$DevComCookie,

    [string]$DevComAccessToken,

    [switch]$AcquireVssToken,

    [switch]$IncludeHistory,

    [switch]$SkipAttachmentDownloads,

    [switch]$ExtractArchives
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Utf8NoBom {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Content
    )

    $directory = [System.IO.Path]::GetDirectoryName($Path)
    if (-not [string]::IsNullOrWhiteSpace($directory))
    {
        [System.IO.Directory]::CreateDirectory($directory) | Out-Null
    }

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [AllowNull()]
        $Value
    )

    Write-Utf8NoBom -Path $Path -Content (ConvertTo-Json -InputObject $Value -Depth 100)
}

function Get-PropertyValue {
    param(
        [Parameter(Mandatory = $true)]
        [AllowNull()]
        $Object,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ($null -eq $Object)
    {
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property)
    {
        return $null
    }

    return $property.Value
}

function Get-FieldValue {
    param(
        [Parameter(Mandatory = $true)]
        $Fields,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    return Get-PropertyValue -Object $Fields -Name $Name
}

function ConvertFrom-HtmlText {
    param([AllowNull()][string]$Html)

    if ([string]::IsNullOrWhiteSpace($Html))
    {
        return ''
    }

    $text = [regex]::Replace($Html, '<\s*br\s*/?\s*>', "`n", 'IgnoreCase')
    $text = [regex]::Replace($text, '</\s*p\s*>', "`n`n", 'IgnoreCase')
    $text = [regex]::Replace($text, '<[^>]+>', '')
    return [System.Net.WebUtility]::HtmlDecode($text).Trim()
}

function ConvertTo-SafeFileName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $safe = [regex]::Replace($Name, '[\\/:*?"<>|]+', '_')
    $safe = [regex]::Replace($safe, '\s+', ' ').Trim()
    if ([string]::IsNullOrWhiteSpace($safe))
    {
        $safe = 'attachment'
    }

    if ($safe.Length -gt 180)
    {
        $extension = [System.IO.Path]::GetExtension($safe)
        $stem = [System.IO.Path]::GetFileNameWithoutExtension($safe)
        $maxStemLength = [Math]::Max(1, 180 - $extension.Length)
        $safe = $stem.Substring(0, [Math]::Min($stem.Length, $maxStemLength)) + $extension
    }

    return $safe
}

function Get-UniqueFilePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Directory,

        [Parameter(Mandatory = $true)]
        [string]$FileName
    )

    [System.IO.Directory]::CreateDirectory($Directory) | Out-Null
    $candidate = Join-Path $Directory $FileName
    if (-not (Test-Path -LiteralPath $candidate))
    {
        return $candidate
    }

    $extension = [System.IO.Path]::GetExtension($FileName)
    $stem = [System.IO.Path]::GetFileNameWithoutExtension($FileName)
    for ($i = 2; ; $i++)
    {
        $candidate = Join-Path $Directory "$stem ($i)$extension"
        if (-not (Test-Path -LiteralPath $candidate))
        {
            return $candidate
        }
    }
}

function Get-AzdoToken {
    param([string]$ExplicitToken)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitToken))
    {
        return $ExplicitToken
    }

    if (-not [string]::IsNullOrWhiteSpace($env:AZDO_BEARER_TOKEN))
    {
        return $env:AZDO_BEARER_TOKEN
    }

    $az = Get-Command az -ErrorAction SilentlyContinue
    if ($null -ne $az)
    {
        # Azure DevOps' fixed Microsoft Entra resource ID; not user- or tenant-specific.
        $tokenJson = & az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --output json 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($tokenJson))
        {
            $token = ($tokenJson | ConvertFrom-Json).accessToken
            if (-not [string]::IsNullOrWhiteSpace($token))
            {
                return $token
            }
        }
    }

    throw "Azure DevOps token unavailable. Pass -AzdoBearerToken, set AZDO_BEARER_TOKEN, or sign in with Azure CLI."
}

function Get-CachedVssToken {
    param([string]$ExplicitToken)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitToken))
    {
        return $ExplicitToken
    }

    if (-not [string]::IsNullOrWhiteSpace($env:VSS_TOKEN))
    {
        return $env:VSS_TOKEN
    }

    $cachePath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'FeedbackHandler\vss_token_cache.txt'
    if (Test-Path -LiteralPath $cachePath)
    {
        $cachedToken = [System.IO.File]::ReadAllText($cachePath).Trim()
        if (-not [string]::IsNullOrWhiteSpace($cachedToken))
        {
            return $cachedToken
        }
    }

    return $null
}

function Get-CachedDevComCookie {
    param([string]$ExplicitCookie)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitCookie))
    {
        return $ExplicitCookie
    }

    if (-not [string]::IsNullOrWhiteSpace($env:DEVCOM_COOKIE))
    {
        return $env:DEVCOM_COOKIE
    }

    $cachePath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'FeedbackHandler\devcom_cookie_cache.txt'
    if (Test-Path -LiteralPath $cachePath)
    {
        $cachedCookie = [System.IO.File]::ReadAllText($cachePath).Trim()
        if (-not [string]::IsNullOrWhiteSpace($cachedCookie))
        {
            return $cachedCookie
        }
    }

    return $null
}

function Get-CachedDevComAccessToken {
    param([string]$ExplicitToken)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitToken))
    {
        return $ExplicitToken
    }

    if (-not [string]::IsNullOrWhiteSpace($env:DEVCOM_ACCESS_TOKEN))
    {
        return $env:DEVCOM_ACCESS_TOKEN
    }

    $cachePath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'FeedbackHandler\devcom_access_token_cache.txt'
    if (Test-Path -LiteralPath $cachePath)
    {
        $cachedToken = [System.IO.File]::ReadAllText($cachePath).Trim()
        if (-not [string]::IsNullOrWhiteSpace($cachedToken))
        {
            return $cachedToken
        }
    }

    return $null
}

function Invoke-FeedbackBrowserAuth {
    param(
        [Parameter(Mandatory = $true)]
        [int]$WorkItemId,

        [Parameter(Mandatory = $true)]
        [string]$OrganizationUrl,

        [Parameter(Mandatory = $true)]
        [string]$Project
    )

    $authApp = Join-Path $PSScriptRoot 'Get-VSFeedbackAuth.cs'
    if (-not (Test-Path -LiteralPath $authApp))
    {
        throw "Feedback browser auth helper not found: $authApp"
    }

    dotnet $authApp /p:ImportDirectoryBuildProps=false /p:ImportDirectoryBuildTargets=false /p:ManagePackageVersionsCentrally=false -- -WorkItemId $WorkItemId -OrganizationUrl $OrganizationUrl -Project $Project
    if ($LASTEXITCODE -ne 0)
    {
        throw "Feedback browser auth helper failed with exit code $LASTEXITCODE."
    }
}

function Invoke-JsonGet {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter(Mandatory = $true)]
        [hashtable]$Headers
    )

    return Invoke-RestMethod -Method Get -Uri $Uri -Headers $Headers
}

function Format-DevComChild {
    param(
        [Parameter(Mandatory = $true)]
        $Item,

        [int]$Depth = 0
    )

    $indent = if ($Depth -gt 0) { '  ' * $Depth } else { '' }
    $author = Get-PropertyValue -Object (Get-PropertyValue -Object $Item -Name 'author') -Name 'name'
    if ([string]::IsNullOrWhiteSpace($author))
    {
        $author = Get-PropertyValue -Object (Get-PropertyValue -Object $Item -Name 'author') -Name 'displayName'
    }
    if ([string]::IsNullOrWhiteSpace($author))
    {
        $author = 'Unknown'
    }

    $createdDate = Get-PropertyValue -Object $Item -Name 'createdDate'
    $text = ConvertFrom-HtmlText (Get-PropertyValue -Object $Item -Name 'text')
    $lines = @("$indent[$createdDate] ${author}:", "$indent$text", '')
    $children = Get-PropertyValue -Object $Item -Name 'children'
    if ($null -ne $children)
    {
        foreach ($child in @($children))
        {
            $lines += Format-DevComChild -Item $child -Depth ($Depth + 1)
        }
    }

    return $lines
}

function Format-DevComConversation {
    param([Parameter(Mandatory = $true)]$Conversation)

    $title = Get-PropertyValue -Object $Conversation -Name 'title'
    $createdDate = Get-PropertyValue -Object $Conversation -Name 'createdDate'
    $author = Get-PropertyValue -Object (Get-PropertyValue -Object $Conversation -Name 'author') -Name 'name'
    if ([string]::IsNullOrWhiteSpace($author))
    {
        $author = Get-PropertyValue -Object (Get-PropertyValue -Object $Conversation -Name 'author') -Name 'displayName'
    }
    if ([string]::IsNullOrWhiteSpace($author))
    {
        $author = 'Unknown'
    }

    $lines = @(
        '# Developer Community Conversation',
        '',
        "Title: $title",
        '',
        "[$createdDate] $author (Original Post):",
        (ConvertFrom-HtmlText (Get-PropertyValue -Object $Conversation -Name 'text')),
        ''
    )

    $children = Get-PropertyValue -Object $Conversation -Name 'children'
    if ($null -ne $children)
    {
        foreach ($child in @($children))
        {
            $lines += Format-DevComChild -Item $child
        }
    }

    return ($lines -join [Environment]::NewLine)
}

function Get-DevComId {
    param([AllowNull()][string]$DeveloperCommunityLink)

    if ([string]::IsNullOrWhiteSpace($DeveloperCommunityLink))
    {
        return $null
    }

    $match = [regex]::Match($DeveloperCommunityLink, '/t/[^/]+/(\d+)')
    if (-not $match.Success)
    {
        return $null
    }

    return $match.Groups[1].Value
}

function Get-DownloadHeaders {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url,

        [Parameter(Mandatory = $true)]
        [string]$AzdoToken,

        [AllowNull()]
        [string]$VssToken,

        [AllowNull()]
        [string]$DevComCookie,

        [AllowNull()]
        [string]$DevComAccessToken
    )

    $headers = @{}
    $uri = [Uri]$Url
    $isAzdoAttachment = (($uri.Host.EndsWith('visualstudio.com', [StringComparison]::OrdinalIgnoreCase) -and
            -not $uri.Host.StartsWith('developercommunity', [StringComparison]::OrdinalIgnoreCase) -and
            $Url.IndexOf('/_apis/wit/attachments/', [StringComparison]::OrdinalIgnoreCase) -ge 0) -or
        $uri.Host.EndsWith('dev.azure.com', [StringComparison]::OrdinalIgnoreCase))

    if ($isAzdoAttachment)
    {
        $headers['Authorization'] = "Bearer $AzdoToken"
    }
    elseif ($uri.Host.Equals('vsfeedback.azurewebsites.net', [StringComparison]::OrdinalIgnoreCase) -and
        $uri.AbsolutePath.StartsWith('/api/', [StringComparison]::OrdinalIgnoreCase) -and
        -not [string]::IsNullOrWhiteSpace($VssToken))
    {
        $headers['Authorization'] = "Bearer $VssToken"
    }
    elseif ($uri.Host.Equals('developercommunity.visualstudio.com', [StringComparison]::OrdinalIgnoreCase) -and
        (-not [string]::IsNullOrWhiteSpace($DevComAccessToken) -or -not [string]::IsNullOrWhiteSpace($DevComCookie)))
    {
        if (-not [string]::IsNullOrWhiteSpace($DevComAccessToken))
        {
            $headers['Authorization'] = "Bearer $DevComAccessToken"
        }

        if (-not [string]::IsNullOrWhiteSpace($DevComCookie))
        {
            $headers['Cookie'] = $DevComCookie
        }
    }
    elseif (-not [string]::IsNullOrWhiteSpace($DevComCookie))
    {
        $headers['Cookie'] = $DevComCookie
    }

    return $headers
}

function Test-IsZipFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path))
    {
        return $false
    }

    $stream = [System.IO.File]::OpenRead($Path)
    try
    {
        if ($stream.Length -lt 4)
        {
            return $false
        }

        $bytes = [byte[]]::new(4)
        [void]$stream.Read($bytes, 0, $bytes.Length)
        return $bytes[0] -eq 0x50 -and $bytes[1] -eq 0x4B
    }
    finally
    {
        $stream.Dispose()
    }
}

function Test-IsHtmlAuthResponse {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path))
    {
        return $false
    }

    $stream = [System.IO.File]::OpenRead($Path)
    try
    {
        $length = [Math]::Min(4096, [int]$stream.Length)
        if ($length -eq 0)
        {
            return $false
        }

        $bytes = [byte[]]::new($length)
        [void]$stream.Read($bytes, 0, $bytes.Length)
        $text = [System.Text.Encoding]::UTF8.GetString($bytes).TrimStart()
        return $text.StartsWith('<!DOCTYPE html', [StringComparison]::OrdinalIgnoreCase) -or
            $text.StartsWith('<html', [StringComparison]::OrdinalIgnoreCase) -or
            $text.IndexOf('<title>Sign in to your account</title>', [StringComparison]::OrdinalIgnoreCase) -ge 0
    }
    finally
    {
        $stream.Dispose()
    }
}

function Test-IsTextArtifact {
    param([Parameter(Mandatory = $true)][string]$Path)

    $name = [System.IO.Path]::GetFileName($Path)
    $extension = [System.IO.Path]::GetExtension($Path).ToLowerInvariant()
    if ($name -match 'ActivityLog\.xml|ServiceHub.*\.log|\.sarif$')
    {
        return $true
    }

    return $extension -in @('.txt', '.log', '.err', '.out', '.xml', '.json', '.config', '.csproj', '.vbproj', '.fsproj', '.sln', '.slnx', '.props', '.targets', '.rsp', '.md')
}

function Get-ArtifactCategory {
    param([Parameter(Mandatory = $true)][string]$Path)

    $name = [System.IO.Path]::GetFileName($Path)
    $extension = [System.IO.Path]::GetExtension($Path).ToLowerInvariant()

    if ($extension -in @('.dmp', '.mdmp', '.dump')) { return 'dump' }
    if ($extension -in @('.etl', '.etlzip')) { return 'etl' }
    if ($extension -in @('.binlog')) { return 'binlog' }
    if ($extension -in @('.zip', '.7z', '.rar', '.tar', '.gz')) { return 'archive' }
    if ($extension -in @('.png', '.jpg', '.jpeg', '.gif', '.bmp')) { return 'image' }
    if ($name -match 'ActivityLog\.xml|ServiceHub.*\.log') { return 'vs-log' }
    if (Test-IsTextArtifact -Path $Path) { return 'text' }
    return 'other'
}

function Write-ArtifactAnalysis {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,

        [Parameter(Mandatory = $true)]
        [string]$AnalysisDirectory
    )

    [System.IO.Directory]::CreateDirectory($AnalysisDirectory) | Out-Null
    $files = @(Get-ChildItem -LiteralPath $Root -Recurse -File -ErrorAction SilentlyContinue)
    $inventory = foreach ($file in $files)
    {
        [pscustomobject]@{
            path = $file.FullName
            relativePath = [System.IO.Path]::GetRelativePath($Root, $file.FullName)
            size = $file.Length
            category = Get-ArtifactCategory -Path $file.FullName
        }
    }

    $inventoryLines = @('# Artifact inventory', '')
    foreach ($group in $inventory | Group-Object category | Sort-Object Name)
    {
        $inventoryLines += "## $($group.Name) ($($group.Count))"
        foreach ($item in $group.Group | Sort-Object relativePath)
        {
            $inventoryLines += '- `' + $item.relativePath + '` (' + $item.size + ' bytes)'
        }
        $inventoryLines += ''
    }

    Write-Utf8NoBom -Path (Join-Path $AnalysisDirectory 'artifact-inventory.md') -Content ($inventoryLines -join [Environment]::NewLine)

    $patterns = @(
        'exception',
        'error',
        'fatal',
        'fail',
        'crash',
        'hang',
        'timeout',
        'Microsoft.CodeAnalysis',
        'Roslyn',
        'ServiceHub',
        'LanguageServer',
        'ActivityLog',
        'Watson',
        'StackTrace',
        'OutOfMemory',
        'AccessViolation'
    )

    $findingLines = @('# Log findings', '')
    $textFiles = @($files | Where-Object { (Test-IsTextArtifact -Path $_.FullName) -and $_.Length -le 25MB })
    $totalMatches = 0

    foreach ($file in $textFiles)
    {
        $matches = @(Select-String -LiteralPath $file.FullName -Pattern $patterns -SimpleMatch -ErrorAction SilentlyContinue | Select-Object -First 25)
        if ($matches.Count -eq 0)
        {
            continue
        }

        $relativePath = [System.IO.Path]::GetRelativePath($Root, $file.FullName)
        $findingLines += '## `' + $relativePath + '`'
        foreach ($match in $matches)
        {
            $line = $match.Line.Trim()
            if ($line.Length -gt 500)
            {
                $line = $line.Substring(0, 500) + '...'
            }
            $findingLines += "- L$($match.LineNumber): $line"
            $totalMatches++
        }
        $findingLines += ''
    }

    if ($totalMatches -eq 0)
    {
        $findingLines += 'No high-signal log terms were found in text artifacts under 25 MB.'
    }

    Write-Utf8NoBom -Path (Join-Path $AnalysisDirectory 'log-findings.md') -Content ($findingLines -join [Environment]::NewLine)
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory))
{
    $OutputDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("VSFeedback\{0}-{1:yyyyMMdd-HHmmss}" -f $WorkItemId, (Get-Date))
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$attachmentsDirectory = Join-Path $OutputDirectory 'attachments'
$analysisDirectory = Join-Path $OutputDirectory 'analysis'
$extractedDirectory = Join-Path $OutputDirectory 'extracted'
[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
[System.IO.Directory]::CreateDirectory($attachmentsDirectory) | Out-Null

$AzdoBearerToken = Get-AzdoToken -ExplicitToken $AzdoBearerToken
$VssToken = Get-CachedVssToken -ExplicitToken $VssToken
$DevComCookie = Get-CachedDevComCookie -ExplicitCookie $DevComCookie
$DevComAccessToken = Get-CachedDevComAccessToken -ExplicitToken $DevComAccessToken
if ($AcquireVssToken -and
    ([string]::IsNullOrWhiteSpace($VssToken) -or [string]::IsNullOrWhiteSpace($DevComAccessToken)))
{
    Invoke-FeedbackBrowserAuth -WorkItemId $WorkItemId -OrganizationUrl $OrganizationUrl -Project $Project
    $VssToken = Get-CachedVssToken -ExplicitToken $null
    $DevComCookie = Get-CachedDevComCookie -ExplicitCookie $null
    $DevComAccessToken = Get-CachedDevComAccessToken -ExplicitToken $null
}

$azdoHeaders = @{ Authorization = "Bearer $AzdoBearerToken" }
$baseWorkItemApi = "$OrganizationUrl/$Project/_apis/wit"

Write-Host "Fetching work item $WorkItemId..."
$workItemUrl = "$baseWorkItemApi/workitems/${WorkItemId}?`$expand=all&api-version=7.1"
$workItem = Invoke-JsonGet -Uri $workItemUrl -Headers $azdoHeaders
Write-JsonFile -Path (Join-Path $OutputDirectory 'work-item.json') -Value $workItem

Write-Host "Fetching comments..."
$commentsUrl = "$baseWorkItemApi/workItems/$WorkItemId/comments?api-version=7.1-preview.4"
$comments = Invoke-JsonGet -Uri $commentsUrl -Headers $azdoHeaders
Write-JsonFile -Path (Join-Path $OutputDirectory 'comments.json') -Value $comments

if ($IncludeHistory)
{
    Write-Host "Fetching history..."
    $historyUrl = "$baseWorkItemApi/workitems/$WorkItemId/updates?api-version=7.1"
    $history = Invoke-JsonGet -Uri $historyUrl -Headers $azdoHeaders
    Write-JsonFile -Path (Join-Path $OutputDirectory 'history.json') -Value $history
}

$fields = Get-PropertyValue -Object $workItem -Name 'fields'
$title = Get-FieldValue -Fields $fields -Name 'System.Title'
$description = ConvertFrom-HtmlText (Get-FieldValue -Fields $fields -Name 'System.Description')
$reproSteps = ConvertFrom-HtmlText (Get-FieldValue -Fields $fields -Name 'Microsoft.VSTS.TCM.ReproSteps')
$areaPath = Get-FieldValue -Fields $fields -Name 'System.AreaPath'
$developerCommunityLink = Get-FieldValue -Fields $fields -Name 'Microsoft.DevDiv.DeveloperCommunityLink'
$devComId = Get-DevComId -DeveloperCommunityLink $developerCommunityLink

$allAttachments = @()
$relations = Get-PropertyValue -Object $workItem -Name 'relations'
foreach ($relation in @($relations))
{
    if ($null -eq $relation)
    {
        continue
    }

    $relationType = Get-PropertyValue -Object $relation -Name 'rel'
    if ($relationType -ne 'AttachedFile')
    {
        continue
    }

    $attributes = Get-PropertyValue -Object $relation -Name 'attributes'
    $name = Get-PropertyValue -Object $attributes -Name 'name'
    if ([string]::IsNullOrWhiteSpace($name))
    {
        $name = "work-item-attachment-$($allAttachments.Count + 1)"
    }

    $allAttachments += [pscustomobject]@{
        source = 'work-item'
        groupName = 'Azure DevOps work item attachments'
        fileName = $name
        url = Get-PropertyValue -Object $relation -Name 'url'
        status = 'pending'
        localPath = $null
        error = $null
    }
}

if (-not [string]::IsNullOrWhiteSpace($devComId))
{
    Write-Host "Fetching Developer Community conversation $devComId..."
    $devComHeaders = @{}
    if (-not [string]::IsNullOrWhiteSpace($VssToken))
    {
        $devComHeaders['Authorization'] = "Bearer $VssToken"
    }

    try
    {
        $devComApiUrl = "https://sendvsfeedback2.azurewebsites.net/api/detailsV3/rootPost?id=$devComId"
        $devComConversation = Invoke-JsonGet -Uri $devComApiUrl -Headers $devComHeaders
        Write-JsonFile -Path (Join-Path $OutputDirectory 'devcom-conversation.json') -Value $devComConversation
        Write-Utf8NoBom -Path (Join-Path $OutputDirectory 'devcom-conversation.md') -Content (Format-DevComConversation -Conversation $devComConversation)
    }
    catch
    {
        Write-Utf8NoBom -Path (Join-Path $OutputDirectory 'devcom-conversation.error.txt') -Content $_.Exception.Message
    }

    if (-not [string]::IsNullOrWhiteSpace($VssToken))
    {
        Write-Host "Fetching VS Feedback diagnostics..."
        $diagnosticsUrl = "https://vsfeedback.azurewebsites.net/api/diagnostics?areaPath=$([Uri]::EscapeDataString($areaPath))&developerCommunityId=$devComId&developerCommunityUrl=$([Uri]::EscapeDataString($developerCommunityLink))"
        try
        {
            $diagnostics = Invoke-JsonGet -Uri $diagnosticsUrl -Headers @{ Authorization = "Bearer $VssToken" }
            Write-JsonFile -Path (Join-Path $OutputDirectory 'diagnostics.json') -Value $diagnostics

            $index = 0
            $diagnosticItems = Get-PropertyValue -Object $diagnostics -Name 'items'
            foreach ($item in @($diagnosticItems))
            {
                if ($null -eq $item)
                {
                    continue
                }

                $index++
                $fileName = Get-PropertyValue -Object $item -Name 'friendlyName'
                if ([string]::IsNullOrWhiteSpace($fileName))
                {
                    $fileName = Get-PropertyValue -Object $item -Name 'fileName'
                }
                if ([string]::IsNullOrWhiteSpace($fileName))
                {
                    $fileName = "diagnostics-item-$index"
                }

                $allAttachments += [pscustomobject]@{
                    source = 'diagnostics'
                    groupName = Get-PropertyValue -Object $item -Name 'groupName'
                    fileName = $fileName
                    url = Get-PropertyValue -Object $item -Name 'url'
                    status = 'pending'
                    localPath = $null
                    error = $null
                }
            }
        }
        catch
        {
            Write-Utf8NoBom -Path (Join-Path $OutputDirectory 'diagnostics.error.txt') -Content $_.Exception.Message
        }
    }
    else
    {
        Write-Utf8NoBom -Path (Join-Path $OutputDirectory 'diagnostics.error.txt') -Content 'No VSS token was available. Pass -VssToken, set VSS_TOKEN, use -AcquireVssToken, or populate %LOCALAPPDATA%\FeedbackHandler\vss_token_cache.txt.'
    }
}

foreach ($attachment in $allAttachments)
{
    $url = $attachment.url
    if ([string]::IsNullOrWhiteSpace($url))
    {
        $attachment.status = 'skipped'
        $attachment.error = 'No download URL'
        continue
    }

    if ($url -match 'prism\.vsdata\.io/|dataexplorer\.azure\.com/')
    {
        $attachment.status = 'telemetry-link'
        continue
    }

    if ($SkipAttachmentDownloads)
    {
        $attachment.status = 'not-downloaded'
        continue
    }

    try
    {
        $groupName = $attachment.groupName
        if ([string]::IsNullOrWhiteSpace($groupName))
        {
            $groupName = 'ungrouped'
        }

        $safeGroup = ConvertTo-SafeFileName -Name $groupName
        $safeFileName = ConvertTo-SafeFileName -Name $attachment.fileName
        $destinationDirectory = Join-Path $attachmentsDirectory $safeGroup
        $destinationPath = Get-UniqueFilePath -Directory $destinationDirectory -FileName $safeFileName
        $headers = Get-DownloadHeaders -Url $url -AzdoToken $AzdoBearerToken -VssToken $VssToken -DevComCookie $DevComCookie -DevComAccessToken $DevComAccessToken

        Write-Host "Downloading $($attachment.fileName)..."
        Invoke-WebRequest -Method Get -Uri $url -Headers $headers -OutFile $destinationPath

        if (Test-IsHtmlAuthResponse -Path $destinationPath)
        {
            Remove-Item -LiteralPath $destinationPath -Force -ErrorAction SilentlyContinue
            throw 'Downloaded response was an HTML sign-in page. Developer Community authentication is required or expired.'
        }

        $isZipArchive = Test-IsZipFile -Path $destinationPath
        if ([System.IO.Path]::GetExtension($destinationPath).Equals('.zip', [StringComparison]::OrdinalIgnoreCase) -and -not $isZipArchive)
        {
            Remove-Item -LiteralPath $destinationPath -Force -ErrorAction SilentlyContinue
            throw 'Downloaded response was not a ZIP archive.'
        }

        $attachment.status = 'downloaded'
        $attachment.localPath = $destinationPath

        if ($ExtractArchives -and $isZipArchive)
        {
            $extractGroupDirectory = Join-Path $extractedDirectory $safeGroup
            $extractTarget = Join-Path $extractGroupDirectory ([System.IO.Path]::GetFileNameWithoutExtension($destinationPath))
            [System.IO.Directory]::CreateDirectory($extractTarget) | Out-Null
            Expand-Archive -LiteralPath $destinationPath -DestinationPath $extractTarget -Force | Out-Null
        }
    }
    catch
    {
        $attachment.status = 'failed'
        $attachment.error = $_.Exception.Message
    }
}

Write-JsonFile -Path (Join-Path $OutputDirectory 'attachments-index.json') -Value $allAttachments

Write-ArtifactAnalysis -Root $OutputDirectory -AnalysisDirectory $analysisDirectory

$telemetrySessionIds = @()
foreach ($attachment in $allAttachments)
{
    if ($attachment.url -match 'prism\.vsdata\.io/session/\?.*[&?]id=([0-9a-fA-F-]+)')
    {
        $telemetrySessionIds += $Matches[1]
    }
}
$telemetrySessionIds = @($telemetrySessionIds | Select-Object -Unique)

$downloadedCount = @($allAttachments | Where-Object { $_.status -eq 'downloaded' }).Count
$failedCount = @($allAttachments | Where-Object { $_.status -eq 'failed' }).Count
$diagnosticsErrorPath = Join-Path $OutputDirectory 'diagnostics.error.txt'
$diagnosticsErrorSummary = if (Test-Path -LiteralPath $diagnosticsErrorPath) { [System.IO.File]::ReadAllText($diagnosticsErrorPath).Trim() } else { $null }
$summaryLines = @(
    "# VS Feedback $WorkItemId",
    '',
    "Title: $title",
    '',
    "Work item: $OrganizationUrl/$Project/_workitems/edit/$WorkItemId",
    "Developer Community: $developerCommunityLink",
    "Area path: $areaPath",
    "Output directory: $OutputDirectory",
    '',
    "Attachments: $downloadedCount downloaded, $failedCount failed, $($allAttachments.Count) discovered.",
    ''
)

if (-not [string]::IsNullOrWhiteSpace($diagnosticsErrorSummary))
{
    $summaryLines += '## Diagnostics error'
    $summaryLines += $diagnosticsErrorSummary
    $summaryLines += ''
}

if ($telemetrySessionIds.Count -gt 0)
{
    $summaryLines += '## Telemetry sessions'
    foreach ($sessionId in $telemetrySessionIds)
    {
        $summaryLines += "- $sessionId"
    }
    $summaryLines += ''
}

if (-not [string]::IsNullOrWhiteSpace($description))
{
    $summaryLines += '## Description'
    $summaryLines += $description
    $summaryLines += ''
}

if (-not [string]::IsNullOrWhiteSpace($reproSteps))
{
    $summaryLines += '## Repro steps'
    $summaryLines += $reproSteps
    $summaryLines += ''
}

$summaryLines += '## Attachments'
foreach ($attachment in $allAttachments)
{
    $summaryLines += '- [' + $attachment.status + '] ' + $attachment.groupName + ': `' + $attachment.fileName + '`'
    if (-not [string]::IsNullOrWhiteSpace($attachment.error))
    {
        $summaryLines += "  Error: $($attachment.error)"
    }
}

Write-Utf8NoBom -Path (Join-Path $OutputDirectory 'summary.md') -Content ($summaryLines -join [Environment]::NewLine)

$summary = [pscustomobject]@{
    workItemId = $WorkItemId
    title = $title
    outputDirectory = $OutputDirectory
    developerCommunityLink = $developerCommunityLink
    telemetrySessionIds = $telemetrySessionIds
    discoveredAttachments = $allAttachments.Count
    downloadedAttachments = $downloadedCount
    failedAttachments = $failedCount
    diagnosticsError = $diagnosticsErrorSummary
}

Write-Host "Downloaded feedback artifacts to: $OutputDirectory"
Write-Host "[VS_FEEDBACK_SUMMARY] $(($summary | ConvertTo-Json -Compress -Depth 10))"
