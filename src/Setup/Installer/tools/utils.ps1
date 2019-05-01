function Exec-CommandCore([string]$command, [string]$commandArgs, [switch]$useConsole = $true) {
  $startInfo = New-Object System.Diagnostics.ProcessStartInfo
  $startInfo.FileName = $command
  $startInfo.Arguments = $commandArgs

  $startInfo.UseShellExecute = $false
  $startInfo.WorkingDirectory = Get-Location

  if (-not $useConsole) {
    $startInfo.RedirectStandardOutput = $true
    $startInfo.CreateNoWindow = $true
  }

  $process = New-Object System.Diagnostics.Process
  $process.StartInfo = $startInfo
  $process.Start() | Out-Null

  $finished = $false
  try {
    if (-not $useConsole) {
      # The OutputDataReceived event doesn't fire as events are sent by the
      # process in powershell.  Possibly due to subtlties of how Powershell
      # manages the thread pool that I'm not aware of.  Using blocking
      # reading here as an alternative which is fine since this blocks
      # on completion already.
      $out = $process.StandardOutput
      while (-not $out.EndOfStream) {
        $line = $out.ReadLine()
        Write-Output $line
      }
    }

    while (-not $process.WaitForExit(100)) {
      # Non-blocking loop done to allow ctr-c interrupts
    }

    $finished = $true
    if ($process.ExitCode -ne 0) {
      throw "Command failed to execute: $command $commandArgs"
    }
  }
  finally {
    # If we didn't finish then an error occured or the user hit ctrl-c.  Either
    # way kill the process
    if (-not $finished) {
      $process.Kill()
    }
  }
}

# Handy function for executing a windows command which needs to go through
# windows command line parsing.
#
# Use this when the command arguments are stored in a variable.  Particularly
# when the variable needs reparsing by the windows command line. Example:
#
#   $args = "/p:ManualBuild=true Test.proj"
#   Exec-Command $msbuild $args
#
function Exec-Command([string]$command, [string]$commandArgs) {
  Exec-CommandCore -command $command -commandArgs $commandargs -useConsole:$false
}

# Functions exactly like Exec-Command but lets the process re-use the current
# console. This means items like colored output will function correctly.
#
# In general this command should be used in place of
#   Exec-Command $msbuild $args | Out-Host
#
function Exec-Console([string]$command, [string]$commandArgs) {
  Exec-CommandCore -command $command -commandArgs $commandargs -useConsole:$true
}


# Returns an array of JSON objects, each item representing an installed instance of Visual Studio that 
# meets our minimal requirements for the Roslyn repo.
# Throws if there is none.
function Get-VisualStudioInstances() {
  $minVersionStr = "15.8"

  $vswhere = Join-Path $PSScriptRoot "vswhere\vswhere.exe"
  $vsInstances = Exec-Command $vswhere "-prerelease -requires Microsoft.VisualStudio.Component.Roslyn.Compiler -version $minVersionStr -format json" | ConvertFrom-Json

  if ($vsInstances.Length -eq 0) {
    throw "Could not find a suitable Visual Studio version. Minimal required version is $minVersionStr."
  }

  return $vsInstances
}

function Test-Process([string]$processName) {
  $all = Get-Process $processName -ErrorAction SilentlyContinue
  return $all -ne $null
}

function Get-VisualStudioLocalDir([string]$vsMajorVersion, [string]$vsId, [string]$rootSuffix) {
  return Join-Path $env:LOCALAPPDATA "Microsoft\VisualStudio\$vsMajorVersion.0_$vsId$rootSuffix"
}

function Get-MefCacheDir([string]$vsLocalDir) {
  return Join-Path $vsLocalDir "ComponentModelCache"
}

function Install-VsixViaTool([string]$vsDir, [string]$vsId, [string]$rootSuffix) {
  Use-VsixTool -vsDir $vsDir -vsId $vsId -rootSuffix $rootSuffix
}

function Uninstall-VsixViaTool([string]$vsDir, [string]$vsId, [string]$rootSuffix) {
  $attempt = 3
  $success = $false
  while ($attempt -gt 0 -and -not $success) {
    try {
      Use-VsixTool -vsDir $vsDir -vsId $vsId -rootSuffix $rootSuffix -additionalArgs "/u" 
      $success = $true
    } catch {
      # remember error information
      $ErrorInfo = $_
      $attempt--
    }
  }

  if (-not $success) {
    Write-Host $ErrorInfo -ForegroundColor Red
    Write-Host $ErrorInfo.Exception  -ForegroundColor Red
    Write-Host $ErrorInfo.ScriptStackTrace -ForegroundColor Red
    exit 1
  }
}

function Use-VsixTool([string]$vsDir, [string]$vsId, [string]$rootSuffix, [string]$additionalArgs = "") {
  $installerExe = Join-Path $PSScriptRoot "vsixexpinstaller\VsixExpInstaller.exe"
  Write-Host "Using VS Instance $vsId at `"$vsDir`"" -ForegroundColor Gray

  $vsixFileNames = @("RoslynDeployment.vsix")
  $rootSuffixArg = if ($rootSuffix) { "/rootSuffix:`"$rootSuffix`"" } else { "" }

  foreach ($vsixFileName in $vsixFileNames) {
    $vsixPath = Resolve-Path (Join-Path $PSScriptRoot "..\vsix\$vsixFileName")
    Exec-Console "`"$installerExe`"" "`"$vsixPath`" /vsInstallDir:`"$vsDir`" $rootSuffixArg $additionalArgs"
  }
}

function Stop-Processes([string]$vsDir, [string]$extensionDir) {
  $stopProcesses = @()
  foreach ($process in Get-Process) {
    if ($process.Path) {
      $dir = Split-Path $process.Path
      if ($dir.StartsWith($vsDir) -or $dir.StartsWith($extensionDir)) {
        $stopProcesses += $process
      }
    }
  }

  if ($stopProcesses.Length -eq 0) {
    return
  }

  Write-Host "The following processes need to be stopped before installation can continue. Proceed? [Y/N]"
  foreach ($process in $stopProcesses) {
    Write-Host "> $($process.Path)"
  }

  $input = Read-Host
  if ($input -ne "y" -and $input -ne "yes") {
    Write-Host "Installation cancelled" -ForegroundColor Yellow
    exit 1
  }

  foreach ($process in $stopProcesses) {
    Stop-Process $process.Id -Force -ErrorAction SilentlyContinue
  }
}
