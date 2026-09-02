<#
.SYNOPSIS
  Extract authoritative CoreCompile evidence from an MSBuild binary log.

.DESCRIPTION
  Replays the binlog through MSBuild's event API and records targets that ran
  completely or skipped because their outputs were up to date. This avoids
  expanding the binlog to a multi-gigabyte diagnostic text log.
#>
param(
    [Parameter(Mandatory)][string]$Binlog,
    [Parameter(Mandatory)][string]$DotNetPath,
    [Parameter(Mandatory)][string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$Binlog = (Resolve-Path $Binlog).Path
$DotNetPath = (Resolve-Path $DotNetPath).Path

$parent = Split-Path -Parent $OutputPath
if ($parent) {
    New-Item -ItemType Directory -Force $parent | Out-Null
}

$project = Join-Path $PSScriptRoot 'CoreCompileBinlogAnalyzer\CoreCompileBinlogAnalyzer.csproj'
& $DotNetPath run --project $project --configuration Release -- $Binlog $OutputPath
if ($LASTEXITCODE -ne 0) {
    throw "CoreCompile binlog analyzer failed ($LASTEXITCODE)."
}
