[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $projects = "",
  [string][Alias('v')]$verbosity = "minimal",
  [string] $msbuildEngine = $null,
  [bool] $warnAsError = $true,
  [switch][Alias('bl')]$binaryLog,
  [switch][Alias('r')]$restore,
  [switch] $ci,
  [switch] $prepareMachine,
  [switch] $help,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

. $PSScriptRoot\tools.ps1

function Print-Usage() {
    Write-Host "Common settings:"
    Write-Host "  -v[erbosity] <value>    Msbuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]"
    Write-Host "  -[bl|binaryLog]         Output binary log (short: -bl)"
    Write-Host "  -help                   Print help and exit"
    Write-Host ""

    Write-Host "Advanced settings:"
    Write-Host "  -restore                Restore dependencies (short: -r)"
    Write-Host "  -projects <value>       Semi-colon delimited list of sln/proj's from the Arcade sdk to build. Globbing is supported (*.sln)"
    Write-Host "  -ci                     Set when running on CI server"
    Write-Host "  -prepareMachine         Prepare machine for CI run"
    Write-Host "  -msbuildEngine <value>  Msbuild engine to use to run build ('dotnet', 'vs', or unspecified)."
    Write-Host ""
    Write-Host "Command line arguments not listed above are passed thru to msbuild."
    Write-Host "The above arguments can be shortened as much as to be unambiguous (e.g. -co for configuration, -t for test, etc.)."
}

function Build {
  $toolsetBuildProj = InitializeToolset

  $toolsetBuildProj = Join-Path (Split-Path $toolsetBuildProj -Parent) "SdkTasks\SdkTask.proj"
  $bl = if ($binaryLog) { "/bl:" + (Join-Path $LogDir "SdkTask.binlog") } else { "" }
  MSBuild $toolsetBuildProj `
    $bl `
    /p:Projects=$projects `
    /p:Restore=$restore `
    /p:RepoRoot=$RepoRoot `
    /p:ContinuousIntegrationBuild=$ci `
    @properties
}

try {
  if ($help -or (($null -ne $properties) -and ($properties.Contains("/help") -or $properties.Contains("/?")))) {
    Print-Usage
    exit 0
  }

  if ($projects -eq "") {
    Write-Error "Missing required parameter '-projects <value>'"
    Print-Usage
    ExitWithExitCode 1
  }

  if ($ci) {
    $binaryLog = $true
  }

  Build
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}

ExitWithExitCode 0
