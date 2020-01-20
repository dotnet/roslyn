Param(
  [string] $Repository,
  [string] $BranchName='master',
  [string] $GdnFolder,
  [string] $AzureDevOpsAccessToken,
  [string] $PushReason
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$disableConfigureToolsetImport = $true
$LASTEXITCODE = 0

try {
  . $PSScriptRoot\..\tools.ps1

  # We create the temp directory where we'll store the sdl-config repository
  $sdlDir = Join-Path $env:TEMP 'sdl'
  if (Test-Path $sdlDir) {
    Remove-Item -Force -Recurse $sdlDir
  }

  Write-Host "git clone https://dnceng:`$AzureDevOpsAccessToken@dev.azure.com/dnceng/internal/_git/sdl-tool-cfg $sdlDir"
  git clone https://dnceng:$AzureDevOpsAccessToken@dev.azure.com/dnceng/internal/_git/sdl-tool-cfg $sdlDir
  if ($LASTEXITCODE -ne 0) {
    Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Git clone failed with exit code $LASTEXITCODE."
    ExitWithExitCode $LASTEXITCODE
  }
  # We copy the .gdn folder from our local run into the git repository so it can be committed
  $sdlRepositoryFolder = Join-Path (Join-Path (Join-Path $sdlDir $Repository) $BranchName) '.gdn'
  if (Get-Command Robocopy) {
    Robocopy /S $GdnFolder $sdlRepositoryFolder
  } else {
    rsync -r $GdnFolder $sdlRepositoryFolder
  }
  # cd to the sdl-config directory so we can run git there
  Push-Location $sdlDir
  # git add . --> git commit --> git push
  Write-Host 'git add .'
  git add .
  if ($LASTEXITCODE -ne 0) {
    Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Git add failed with exit code $LASTEXITCODE."
    ExitWithExitCode $LASTEXITCODE
  }
  Write-Host "git -c user.email=`"dn-bot@microsoft.com`" -c user.name=`"Dotnet Bot`" commit -m `"$PushReason for $Repository/$BranchName`""
  git -c user.email="dn-bot@microsoft.com" -c user.name="Dotnet Bot" commit -m "$PushReason for $Repository/$BranchName"
  if ($LASTEXITCODE -ne 0) {
    Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Git commit failed with exit code $LASTEXITCODE."
    ExitWithExitCode $LASTEXITCODE
  }
  Write-Host 'git push'
  git push
  if ($LASTEXITCODE -ne 0) {
    Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Git push failed with exit code $LASTEXITCODE."
    ExitWithExitCode $LASTEXITCODE
  }

  # Return to the original directory
  Pop-Location
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'Sdl' -Message $_
  ExitWithExitCode 1
}