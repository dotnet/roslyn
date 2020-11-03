[CmdletBinding(PositionalBinding=$false)]
param (
  [string]$mode = "",
  [string]$repoDirectory = "",
  [string]$transportDirectory = "")

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Pack() {
  . (Join-Path $repoDirectory "eng\build-utils.ps1")
  $dotnet = Ensure-DotNetSdk

  # First prepare the tests
  Write-Host "Preparing unit tests"
  $unitTestDirectory = Join-Path $transportDirectory "testPayload"
  Exec-Console $dotnet "run --project src\Tools\PrepareTests\PrepareTests.csproj $repoDirectory\artifacts\bin $unitTestDirectory"

  # Next prepare the eng directory
  Write-Host "Copying eng directory"
  Copy-Item -Recurse -Force "$repoDirectory\eng\*" (Join-Path $transportDirectory "eng")

  # Next record the global.json file
  Write-Host "Copying global.json"
  Copy-Item "$repoDirectory\global.json" $transportDirectory
}

function Unpack() {
  # In this mode there is no build-utils.ps1 environment. The job of this script is to unpack
  # the palyoad created by pack to form a coherent unit test environment
  Write-Host "Copying global.json"
  Copy-Item (Join-Path $transportDirectory "global.json") $repoDirectory

  Write-Host "Copying eng"
  Copy-Item -Recurse -Force (Join-Path $transportDirectory "eng") (Join-Path $repoDirectory "eng")

  Write-Host "Copying test payload"
  Create-Directory "artifacts\bin" | Out-Null
  Move-Item "$transportDirectory\testPayload\*" (Join-Path $repoDirectory "artifacts\bin")

  # Now that the environment is restored, lets use it to finish rebuilding
  . (Join-Path $repoDirectory "eng\build-utils.ps1")

  Push-Location "artifacts\bin"
  & .\rehydrate.cmd
  Pop-Location

  Ensure-DotNetSdk
}

try {
  if ($repoDirectory -eq "") {
    Write-Host "Need to specify the path to the repository"
    exit 1
  }

  if ($transportDirectory -eq "") {
    Write-Host "Need to specify -transportDirectory pointing to the transport payload directory"
    exit 1
  }

  if (!(Test-Path $repoDirectory)) {
    New-Item -path $repoDirectory -force -itemType "Directory" | Out-Null
  }

  Push-Location $repoDirectory

  if ($mode -eq "pack")
  {
    Pack
  }
  elseif ($mode -eq "unpack")
  {
    Unpack
  }
  else
  {
    Write-Host "Unknown mode $mode. Must be 'pack' or 'unpack'"
    exit 1
  }

  exit 0
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
  exit 1
}
finally {
  Pop-Location
}
