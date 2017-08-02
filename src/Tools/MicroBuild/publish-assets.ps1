# Publishes our build assets to nuget, myget, dotnet/versions, etc ..
#
# The publish operation is best visioned as an optional yet repeatable post build operation. It can be 
# run anytime after build or automatically as a post build step. But it is an operation that focuses on 
# build outputs and hence can't rely on source code from the build being available
#
# Repeatable is important here because we have to assume that publishes can and will fail with some 
# degree of regularity. 
[CmdletBinding(PositionalBinding=$false)]
Param(
    # Standard options
    [string]$configDir = "",
    [string]$branchName = "",
    [string]$releaseName = "",
    [switch]$test,

    # Credentials 
    [string]$gitHubUserName = "",
    [string]$gitHubToken = "",
    [string]$gitHubEmail = "",
    [string]$nugetApiKey = "",
    [string]$myGetApiKey = ""
)
Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Get-PublishKey([string]$uploadUrl) {
    $url = New-Object Uri $uploadUrl
    switch ($url.Host) {
        "dotnet.myget.org" { return $myGetApiKey }
        "api.nuget.org" { return $nugetApiKey }
        default { throw "Cannot determine publish key for $uploadUrl" }
    }
}

# Publish the NuGet packages to the specified URL
function Publish-NuGet([string]$packageDir, [string]$uploadUrl) {
    Push-Location $packageDir
    try {
        Write-Host "Publishing $(Split-Path -leaf $packageDir) to $uploadUrl"
        $packages = [xml](Get-Content "$packageDir\myget_org-packages.config")
        $apiKey = Get-PublishKey $uploadUrl
        foreach ($package in $packages.packages.package) {
            $nupkg = $package.id + "." + $package.version + ".nupkg"
            Write-Host "  Publishing $nupkg"
            if (-not (Test-Path $nupkg)) {
                throw "$nupkg does not exist"
            }

            if (-not $test) {
                Exec-Console $nuget "push $nupkg -Source $uploadUrl -ApiKey $apiKey -NonInteractive -Verbosity quiet"
            }
        }
    } 
    finally {
        Pop-Location
    }
}

function Publish-Vsix([string]$uploadUrl) {
    Push-Location $configDir
    try { 
        Write-Host "Publishing VSIX to $uploadUrl"
        $apiKey = Get-PublishKey $uploadUrl
        $extensions = [xml](Get-Content (Join-Path $configDir "myget_org-extensions.config"))
        foreach ($extension in $extensions.extensions.extension) {
            $vsix = Join-Path $extension.path ($extension.id + ".vsix")
            if (-not (Test-Path $vsix)) {
                throw "VSIX $vsix does not exist"
            }

            Write-Host "  Publishing '$vsix'"
            if (-not $test) { 
                $response = Invoke-WebRequest -Uri $requestUrl -Headers @{"X-NuGet-ApiKey"=$apiKey} -ContentType 'multipart/form-data' -InFile $vsix -Method Post -UseBasicParsing
                if ($response.StatusCode -ne 201) {
                    throw "Failed to upload VSIX extension: $vsix. Upload failed with Status code: $response.StatusCode"
                }
            }
        }
    }
    finally {
        Pop-Location
    }
}

function Publish-Channel([string]$packageDir, [string]$name) {
    $publish = Join-Path $configDir "Exes\RoslynPublish.exe"
    $args = "-nugetdir $packageDir -channel $name -gu $gitHubUserName -gt $gitHubToken -ge $githubEmail"
    Write-Host "Publishing $packageDir to channel $name"
    if (-not $test) { 
        Exec-Console $publish $args
    }
}

# Do basic verification on the values provided in the publish configuration
function Test-Entry($publishData, [switch]$isBranch) { 
    if ($isBranch) { 
        if ($publishData.nuget -ne $null) { 
            $kind = $publishData.nugetKind;
            if ($kind -ne "PerBuildPreRelease") {
                throw "Branches are only allowed to publish PerBuildPreRelease"
            }
        }
    }
}

# Publish a given entry: branch or release. 
function Publish-Entry($publishData, [switch]$isBranch) { 
    Test-Entry $publishData -isBranch:$isBranch
    $packageDir = Join-Path $nugetDir $publishData.nugetKind


    # First publish the NuGet packages to the specified feeds
    foreach ($url in $publishData.nuget) { 
        Publish-NuGet $packageDir $url
    }

    # Next publish the VSIX to the specified feeds
    $vsixData = $publishData.vsix
    if ($vsixData -ne $null) { 
        Publish-Vsix $vsixData
    }

    # Finally get our channels uploaded to versions
    foreach ($channel in $publishData.channels) {
        Publish-Channel $packageDir $channel
    }

    exit 0
}

try {
    . (Join-Path $PSScriptRoot "..\..\..\build\scripts\build-utils.ps1")
    $nuget = Ensure-NuGet
    $nugetDir = Join-Path $configDir "NuGet"

    if ($configDir -eq "") {
        Write-Host "Must provide the build output with -configDir"
        exit 1
    }

    # TODO: use the live file 
    $publishFilePath = Join-Path $repoDir "build\config\PublishData.json"
    $data = ConvertFrom-Json (Get-Content -raw $publishFilePath)

    if ($branchName -ne "" -and $releaseName -ne "") {
        Write-Host "Can only specify -branchName or -releaseName, not both"
        exit 1
    }
    elseif ($branchName -ne "") {
        $branchData = $data.branches.$branchName
        if ($branchData -eq $null) {
            Write-Host "$branchName is not listed for publishing"
            exit 0
        }

        Publish-Entry $branchData -isBranch:$true
    }
    elseif ($releaseName -ne "") { 
        $releaseData = $data.releases.$releaseName
        if ($releaseData -eq $null) {
           Write-Host "$releaseName is not a valid release"
           exit 1
        }

        Publish-Entry $releaseName -isBranch:$false
    }
    else {
        Write-Host "Need to specify -branchName or -releaseName"
        exit 1
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
