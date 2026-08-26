<#
  This script validates that every Roslyn SDK sample project is included in the
  standalone sample solution and that the solution builds successfully.
#>

[CmdletBinding(PositionalBinding=$false)]
param(
  [string]$configuration = "Release",
  [switch]$ci = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

$repoDir = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$samplesDir = Join-Path $repoDir "src\RoslynSdk\Samples"
$solutionPath = Join-Path $samplesDir "RoslynSdkSamples.slnx"

[xml]$solution = Get-Content $solutionPath -Raw
$solutionProjects = @(
  $solution.SelectNodes("//Project") |
    ForEach-Object { $_.Path.Replace("\", "/") }
)
$diskProjects = @(
  Get-ChildItem $samplesDir -Recurse -File |
    Where-Object { $_.Extension -in ".csproj", ".vbproj" } |
    ForEach-Object { $_.FullName.Substring($samplesDir.Length + 1).Replace("\", "/") }
)

$duplicateProjects = @(
  $solutionProjects |
    Group-Object |
    Where-Object { $_.Count -gt 1 } |
    ForEach-Object { $_.Name }
)
$missingFromSolution = @($diskProjects | Where-Object { $_ -notin $solutionProjects })
$missingFromDisk = @($solutionProjects | Where-Object { $_ -notin $diskProjects })

if ($duplicateProjects.Count -gt 0 -or $missingFromSolution.Count -gt 0 -or $missingFromDisk.Count -gt 0) {
  Write-Host "RoslynSdkSamples.slnx does not match the sample projects on disk." -ForegroundColor Red

  if ($duplicateProjects.Count -gt 0) {
    Write-Host "Duplicate solution entries:" -ForegroundColor Red
    $duplicateProjects | Sort-Object | ForEach-Object { Write-Host "  - $_" }
  }

  if ($missingFromSolution.Count -gt 0) {
    Write-Host "Projects missing from the solution:" -ForegroundColor Red
    $missingFromSolution | Sort-Object | ForEach-Object { Write-Host "  - $_" }
  }

  if ($missingFromDisk.Count -gt 0) {
    Write-Host "Solution entries missing on disk:" -ForegroundColor Red
    $missingFromDisk | Sort-Object | ForEach-Object { Write-Host "  - $_" }
  }

  exit 1
}

$buildArgs = @(
  "build"
  $solutionPath
  "-c", $configuration
  "--no-incremental"
)

if ($ci) {
  $logDir = Join-Path $repoDir "artifacts\log\$configuration"
  New-Item -ItemType Directory -Path $logDir -Force | Out-Null

  $buildArgs += "--disable-build-servers"
  $buildArgs += "-bl:$(Join-Path $logDir "RoslynSdkSamples.binlog")"
}

Write-Host "dotnet $($buildArgs -join ' ')"
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}
