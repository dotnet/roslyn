#Requires -PSEdition Core -Version 7
<#
.SYNOPSIS
    Submits a source archival request for this repo.
.PARAMETER Requester
    The alias for the user requesting this backup.
.PARAMETER ManagerAlias
    The alias of the manager that owns the repo.
.PARAMETER TeamAlias
    The alias of the team that owns the repo.
.PARAMETER BusinessGroupName
    A human-readable title for your team or business group.
.PARAMETER ProductionType
.PARAMETER ReleaseType
    The type of release being backed up.
.PARAMETER ReleaseDate
    The date of the release of your software. Defaults to today.
.PARAMETER OwnerAlias
    The alias of the owner.
.PARAMETER OS
.PARAMETER ProductLanguage
    One or more languages.
.PARAMETER Notes
    Any notes to record with the backup.
.PARAMETER FileCollection
    One or more collections to archive.
.PARAMETER ProductName
    The name of the product. This will default to the repository name.
.PARAMETER RepoUrl
    The URL to the repository. This will default to the repository containing this script.
.PARAMETER BackupType
    The kind of backup to be performed.
.PARAMETER ServerPath
    The UNC path to the server to be backed up (if applicable).
.PARAMETER SourceCodeArchivalUri
    The URI to POST the source code archival request to.
    This value will typically come automatically by a variable group associated with your pipeline.
    You can also look it up at https://dpsrequestforms.azurewebsites.net/#/help -> SCA Request Help -> SCA API Help -> Description
#>
[CmdletBinding(SupportsShouldProcess = $true, PositionalBinding = $false)]
param (
    [Parameter()]
    [string]$Requester,
    [Parameter(Mandatory = $true)]
    [string]$ManagerAlias,
    [Parameter(Mandatory = $true)]
    [string]$TeamAlias,
    [Parameter(Mandatory = $true)]
    [string]$BusinessGroupName,
    [Parameter()]
    [string]$ProductionType = 'Visual Studio',
    [Parameter()]
    [string]$ReleaseType = 'RTW',
    [Parameter()]
    [DateTime]$ReleaseDate = [DateTime]::Today,
    [Parameter()]
    [string]$OwnerAlias,
    [Parameter()]
    [ValidateSet('64-Bit Win', '32-Bit Win', 'Linux', 'Mac', '64-Bit ARM', '32-Bit ARM')]
    [string[]]$OS = @('64-Bit Win'),
    [Parameter(Mandatory = $true)]
    [ValidateSet('English', 'Chinese Simplified', 'Chinese Traditional', 'Czech', 'French', 'German', 'Italian', 'Japanese', 'Korean', 'Polish', 'Portuguese', 'Russian', 'Spanish', 'Turkish')]
    [string[]]$ProductLanguage,
    [Parameter()]
    [string]$Notes = '',
    [Parameter()]
    [ValidateSet('Binaries', 'Localization', 'Source Code')]
    [string[]]$FileCollection = @('Source Code'),
    [Parameter()]
    [string]$ProductName,
    [Parameter()]
    [Uri]$RepoUrl,
    [Parameter()]
    [ValidateSet('Server Path', 'Code Repo(Git URL/AzureDevOps)', 'Git', 'Azure Storage Account')]
    [string]$BackupType = 'Code Repo(Git URL/AzureDevOps)',
    [Parameter()]
    [string]$ServerPath = '',
    [Parameter()]
    [Uri]$SourceCodeArchivalUri = $env:SOURCECODEARCHIVALURI
)

function Invoke-Git() {
    # Make sure we invoke git from within the repo.
    Push-Location $PSScriptRoot
    try {
        return (git $args)
    }
    finally {
        Pop-Location
    }
}

if (!$ProductName) {
    if ($env:BUILD_REPOSITORY_NAME) {
        Write-Verbose 'Using $env:BUILD_REPOSITORY_NAME for ProductName.' # single quotes are intentional so user sees the name of env var.
        $ProductName = $env:BUILD_REPOSITORY_NAME
    }
    else {
        $originUrl = [Uri](Invoke-Git remote get-url origin)
        if ($originUrl) {
            $lastPathSegment = $originUrl.Segments[$originUrl.Segments.Length - 1]
            if ($lastPathSegment.EndsWith('.git')) {
                $lastPathSegment = $lastPathSegment.Substring(0, $lastPathSegment.Length - '.git'.Length)
            }
            Write-Verbose 'Using origin remote URL to derive ProductName.'
            $ProductName = $lastPathSegment
        }
    }

    if (!$ProductName) {
        Write-Error "Unable to determine default value for -ProductName."
    }
}

if (!$OwnerAlias) {
    if ($env:BUILD_REQUESTEDFOREMAIL) {
        Write-Verbose 'Using $env:BUILD_REQUESTEDFOREMAIL and slicing to just the alias for OwnerAlias.'
        $OwnerAlias = ($env:BUILD_REQUESTEDFOREMAIL -split '@')[0]
    } else {
        $OwnerAlias = $TeamAlias
    }

    if (!$OwnerAlias) {
        Write-Error "Unable to determine default value for -OwnerAlias."
    }
}

if (!$Requester) {
    if ($env:BUILD_REQUESTEDFOREMAIL) {
        Write-Verbose 'Using $env:BUILD_REQUESTEDFOREMAIL and slicing to just the alias for Requester.'
        $Requester = ($env:BUILD_REQUESTEDFOREMAIL -split '@')[0]
    }
    else {
        Write-Verbose 'Using $env:USERNAME for Requester.'
        $Requester = $env:USERNAME
    }
    if (!$Requester) {
        $Requester = $OwnerAlias
    }
}

if (!$RepoUrl) {
    $RepoUrl = $env:BUILD_REPOSITORY_URI
    if (!$RepoUrl) {
        $originUrl = [Uri](Invoke-Git remote get-url origin)
        if ($originUrl) {
            Write-Verbose 'Using git origin remote url for GitURL.'
            $RepoUrl = $originUrl
        }

        if (!$RepoUrl) {
            Write-Error "Unable to determine default value for -RepoUrl."
        }
    }
}

Push-Location $PSScriptRoot
$versionsObj = dotnet nbgv get-version -f json | ConvertFrom-Json
Pop-Location

$ReleaseDateString = $ReleaseDate.ToShortDateString()
$Version = $versionsObj.Version

$BackupSize = Get-ChildItem $PSScriptRoot\..\.git -Recurse -File | Measure-Object -Property Length -Sum
$DataSizeMB = [int]($BackupSize.Sum / 1mb)
$FileCount = $BackupSize.Count

$Request = @{
    "Requester"                  = $Requester
    "Manager"                    = $ManagerAlias
    "TeamAlias"                  = $TeamAlias
    "AdditionalContacts"         = $AdditionalContacts
    "BusinessGroupName"          = $BusinessGroupName
    "ProductName"                = $ProductName
    "Version"                    = $Version
    "ProductionType"             = $ProductionType
    "ReleaseType"                = $ReleaseType
    "ReleaseDateString"          = $ReleaseDateString
    "OS"                         = [string]::Join(',', $OS)
    "ProductLanguage"            = [string]::Join(',', $ProductLanguage)
    "FileCollection"             = [string]::Join(',', $FileCollection)
    "OwnerAlias"                 = $OwnerAlias
    "Notes"                      = $Notes.Trim()
    "CustomerProvidedDataSizeMB" = $DataSizeMB
    "CustomerProvidedFileCount"  = $FileCount
    "BackupType"                 = $BackupType
    "ServerPath"                 = $ServerPath
    "AzureStorageAccount"        = $AzureStorageAccount
    "AzureStorageContainer"      = $AzureStorageContainer
    "GitURL"                     = $RepoUrl
}

$RequestJson = ConvertTo-Json $Request
Write-Host "SCA request:`n$RequestJson"

if ($PSCmdlet.ShouldProcess('source archival request', 'post')) {
    if (!$SourceCodeArchivalUri) {
        Write-Error "Unable to post request without -SourceCodeArchivalUri parameter."
        exit 1
    }

    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    $Response = Invoke-WebRequest -Uri $SourceCodeArchivalUri -Method POST -Body $RequestJson -ContentType "application/json" -UseBasicParsing -SkipHttpErrorCheck
    Write-Host "Status Code : " -NoNewline
    $responseContent = ConvertFrom-Json ($Response.Content)
    if ($Response.StatusCode -eq 200) {
        Write-Host $Response.StatusCode -ForegroundColor Green
        Write-Host "Ticket ID   : " -NoNewline
        Write-Host $responseContent
    }
    else {
        $responseContent = ConvertFrom-Json $Response.Content
        Write-Host $Response.StatusCode -ForegroundColor Red
        Write-Host "Message     : $($responseContent.message)"
    }
} elseif ($SourceCodeArchivalUri) {
    Write-Host "Would have posted to $SourceCodeArchivalUri"
}
