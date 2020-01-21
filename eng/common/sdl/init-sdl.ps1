Param(
  [string] $GuardianCliLocation,
  [string] $Repository,
  [string] $BranchName='master',
  [string] $WorkingDirectory,
  [string] $AzureDevOpsAccessToken,
  [string] $GuardianLoggerLevel='Standard'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$disableConfigureToolsetImport = $true
$LASTEXITCODE = 0

. $PSScriptRoot\..\tools.ps1

# Don't display the console progress UI - it's a huge perf hit
$ProgressPreference = 'SilentlyContinue'

# Construct basic auth from AzDO access token; construct URI to the repository's gdn folder stored in that repository; construct location of zip file
$encodedPat = [Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes(":$AzureDevOpsAccessToken"))
$escapedRepository = [Uri]::EscapeDataString("/$Repository/$BranchName/.gdn")
$uri = "https://dev.azure.com/dnceng/internal/_apis/git/repositories/sdl-tool-cfg/Items?path=$escapedRepository&versionDescriptor[versionOptions]=0&`$format=zip&api-version=5.0-preview.1"
$zipFile = "$WorkingDirectory/gdn.zip"

Add-Type -AssemblyName System.IO.Compression.FileSystem
$gdnFolder = (Join-Path $WorkingDirectory '.gdn')
try {
  # We try to download the zip; if the request fails (e.g. the file doesn't exist), we catch it and init guardian instead
  Write-Host 'Downloading gdn folder from internal config repostiory...'
  Invoke-WebRequest -Headers @{ "Accept"="application/zip"; "Authorization"="Basic $encodedPat" } -Uri $uri -OutFile $zipFile
  if (Test-Path $gdnFolder) {
    # Remove the gdn folder if it exists (it shouldn't unless there's too much caching; this is just in case)
    Remove-Item -Force -Recurse $gdnFolder
  }
  [System.IO.Compression.ZipFile]::ExtractToDirectory($zipFile, $WorkingDirectory)
  Write-Host $gdnFolder
  ExitWithExitCode 0
} catch [System.Net.WebException] { } # Catch and ignore webexception
try {
  # if the folder does not exist, we'll do a guardian init and push it to the remote repository
  Write-Host 'Initializing Guardian...'
  Write-Host "$GuardianCliLocation init --working-directory $WorkingDirectory --logger-level $GuardianLoggerLevel"
  & $GuardianCliLocation init --working-directory $WorkingDirectory --logger-level $GuardianLoggerLevel
  if ($LASTEXITCODE -ne 0) {
    Write-PipelineTelemetryError -Force -Category 'Build' -Message "Guardian init failed with exit code $LASTEXITCODE."
    ExitWithExitCode $LASTEXITCODE
  }
  # We create the mainbaseline so it can be edited later
  Write-Host "$GuardianCliLocation baseline --working-directory $WorkingDirectory --name mainbaseline"
  & $GuardianCliLocation baseline --working-directory $WorkingDirectory --name mainbaseline
  if ($LASTEXITCODE -ne 0) {
    Write-PipelineTelemetryError -Force -Category 'Build' -Message "Guardian baseline failed with exit code $LASTEXITCODE."
    ExitWithExitCode $LASTEXITCODE
  }
  & $(Join-Path $PSScriptRoot 'push-gdn.ps1') -Repository $Repository -BranchName $BranchName -GdnFolder $gdnFolder -AzureDevOpsAccessToken $AzureDevOpsAccessToken -PushReason 'Initialize gdn folder'
  ExitWithExitCode 0
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'Sdl' -Message $_
  ExitWithExitCode 1
}
