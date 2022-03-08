param(
  [string]$globalJsonPath,
  [string]$nugetConfigPath = $null,
  [string]$dotnetPath = "dotnet"
)

# If it's just 'dotnet', it's being resolved from PATH. Otherwise, resolve it.
if (Test-Path $dotnetPath) {
  $dotnetPath = (Resolve-Path $dotnetPath).Path
}

function New-TemporaryFilePath
{
  # Neither New-TemporaryFile nor New-Guid can be resolved when running these
  # scripts via cmd scripts from a PowerShell 7 terminal.
  return Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid())
}

function Get-WorkloadName
(
  [string]$shortName
)
{
  return "microsoft.net.sdk.$shortName"
}

function Get-InstalledWorkloads
{
  $installedWorkloadsWithVersions = @{}

  # Shows versions of workloads that would be installed if a rollback were
  # done. Includes uninstalled workloads, though, so these version numbers
  # are only useful to us if we've confirmed elsewhere that the workload is
  # actually installed.
  $workloadUpdateOutput = & $dotnetPath workload update --print-rollback

  $foundJson = $false
  $rollback = $null
  foreach ($line in $workloadUpdateOutput) {
    if ($foundJson) {
      $rollback = $line | ConvertFrom-Json
      break
    }

    if ($line -eq "==workloadRollbackDefinitionJsonOutputStart==") {
      $foundJson = $true
    }
  }

  if (-not $rollback) {
    return $installedWorkloadsWithVersions
  }

  # Shows what workloads are actually installed. Does not show installed
  # version information, unless an update happens to be available.
  # https://github.com/dotnet/sdk/issues/22882
  $workloadListOutput = & $dotnetPath workload list --machine-readable

  $foundJson = $false
  $workloadList = $null
  foreach ($line in $workloadListOutput) {
    if ($foundJson) {
      $workloadList = $line | ConvertFrom-Json
      break
    }

    if ($line -eq "==workloadListJsonOutputStart==") {
      $foundJson = $true
    }
  }

  foreach ($shortWorkloadName in $workloadList.installed) {
    $workloadName = Get-WorkloadName -shortName $shortWorkloadName
    if ($workloadName -in $rollback.PSObject.Properties.Name) {
      $installedWorkloadsWithVersions.$workloadName = $rollback.$workloadName
    }
  }

  return $installedWorkloadsWithVersions
}

function Install-Workload
(
  [string]$shortName,
  [string]$version = $null,
  [string[]]$sources = $null
)
{
  $workloadName = Get-WorkloadName -shortName $shortName

  $installedVersion = (Get-InstalledWorkloads)[$workloadName]

  if ($installedVersion) {
    Write-Host "Workload $workloadName ($shortName) $installedVersion already installed"

    if (-not $version -or $version -eq $installedVersion) {
      continue
    }
  }

  Write-Host "Preparing to install workload $workloadName ($shortName) $version..."

  # Safest to uninstall the existing version first
  if ($installedVersion) {
    Write-Host "  Uninstalling $shortName..."

    # NOTE: This requires elevated access if using a system dotnet install
    & $dotnetPath workload uninstall $shortName
  }

  $rollbackArgs = @()
  $rollbackFile = $null
  if ($version) {
    $rollbackJson = @{}
    $rollbackJson[$workloadName] = $version

    $rollbackFile = New-TemporaryFilePath
    $rollbackJson | ConvertTo-Json | Set-Content -Path $rollbackFile

    $rollbackArgs += "--from-rollback-file"
    $rollbackArgs += $rollbackFile
  }

  $sourcesArgs = @()
  foreach ($source in $sources) {
    $sourcesArgs += "--source"
    $sourcesArgs += $source
  }

  $nugetConfigArgs = @()
  if (Test-Path -Type Leaf -Path $nugetConfigPath) {
    $nugetConfigArgs += "--configfile"
    $nugetConfigArgs += $nugetConfigPath
  }

  Write-Host "  Installing $shortName..."

  # NOTE: This requires elevated access if using a system dotnet install
  & $dotnetPath workload install $shortName $rollbackArgs $sourcesArgs $nugetConfigArgs

  if ($rollbackFile) {
      Remove-Item $rollbackFile
  }
}

$GlobalJson = Get-Content $globalJsonPath | ConvertFrom-Json

if ($GlobalJson.PSObject.Properties.Name -contains "workloads") {
  foreach ($workload in $GlobalJson.workloads.PSObject.Properties) {
    $version = $null
    if ($workload.Value.PSObject.Properties.Name -contains "version") {
      $version = $workload.Value.version
    }

    $sources = $null
    if ($workload.Value.PSObject.Properties.Name -contains "sources") {
      $version = $workload.Value.sources
    }

    Install-Workload `
      -shortName $workload.Name `
      -version $version `
      -sources $sources
  }
}