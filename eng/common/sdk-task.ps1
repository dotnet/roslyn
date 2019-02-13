[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $configuration = "Debug",
  [string] $task,
  [string] $verbosity = "minimal",
  [string] $msbuildEngine = $null,
  [switch] $restore,
  [switch] $prepareMachine,
  [switch] $help,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

$ci = $true
$binaryLog = $true
$warnAsError = $true

. $PSScriptRoot\tools.ps1

function Print-Usage() {
  Write-Host "Common settings:"
  Write-Host "  -task <value>           Name of Arcade task (name of a project in SdkTasks directory of the Arcade SDK package)"
  Write-Host "  -restore                Restore dependencies"
  Write-Host "  -verbosity <value>      Msbuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]"
  Write-Host "  -help                   Print help and exit"
  Write-Host ""

  Write-Host "Advanced settings:"
  Write-Host "  -prepareMachine         Prepare machine for CI run"
  Write-Host "  -msbuildEngine <value>  Msbuild engine to use to run build ('dotnet', 'vs', or unspecified)."
  Write-Host ""
  Write-Host "Command line arguments not listed above are passed thru to msbuild."
}

function Build([string]$target) {
  $logSuffix = if ($target -eq "Execute") { "" } else { ".$target" }
  $log = Join-Path $LogDir "$task$logSuffix.binlog"
  $outputPath = Join-Path $ToolsetDir "$task\\"

  MSBuild $taskProject `
    /bl:$log `
    /t:$target `
    /p:Configuration=$configuration `
    /p:RepoRoot=$RepoRoot `
    /p:BaseIntermediateOutputPath=$outputPath `
    @properties
}

try {
  if ($help -or (($null -ne $properties) -and ($properties.Contains("/help") -or $properties.Contains("/?")))) {
    Print-Usage
    exit 0
  }

  if ($task -eq "") {
    Write-Host "Missing required parameter '-task <value>'" -ForegroundColor Red
    Print-Usage
    ExitWithExitCode 1
  }

  $taskProject = GetSdkTaskProject $task
  if (!(Test-Path $taskProject)) {
    Write-Host "Unknown task: $task" -ForegroundColor Red
    ExitWithExitCode 1
  }

  if ($restore) {
    Build "Restore"
  }

  Build "Execute"
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}

ExitWithExitCode 0
