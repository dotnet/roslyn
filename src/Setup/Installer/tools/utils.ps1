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


# Get the directory and instance ID of the first Visual Studio version which
# meets our minimal requirements for the Roslyn repo.
function Get-VisualStudioDirAndId() {
  $vswhere = Join-Path $PSScriptRoot "vswhere\vswhere.exe"
  $output = Exec-Command $vswhere "-prerelease -requires Microsoft.VisualStudio.Component.Roslyn.Compiler -version [15.5,15.9) -format json" | Out-String
  $j = ConvertFrom-Json $output
  $foundVsInstall = $false
  foreach ($obj in $j) {
    # Need to be using at least Visual Studio 15.5 in order to have the appropriate
    # set of SDK fixes. Parsing the installationName is the only place where this is
    # recorded in that form.
    $name = $obj.installationName
    if ($name -match "VisualStudio(Preview)?/([\d.]+)(\+|-).*") {
      $minVersion = New-Object System.Version "15.5.0"
      $maxVersion = New-Object System.Version "15.9.0"
      $version = New-Object System.Version $matches[2]
      if ($version -ge $minVersion -and $version -lt $maxVersion) {
        Write-Output $obj.installationPath
        Write-Output $obj.instanceId
        $foundVsInstall = $true;
      }
    }
  }

  if (-not $foundVsInstall) {
    throw "Could not find a suitable Visual Studio Version"
  }
}

function Test-Process([string]$processName) {
  $all = Get-Process $processName -ErrorAction SilentlyContinue
  return $all -ne $null
}

function Install-VsixViaTool([string]$vsDir, [string]$vsId, [string]$hive) {
  $baseArgs = "/rootSuffix:$hive /vsInstallDir:`"$vsDir`""
  $vsixes = @("vsix\RoslynDeployment.vsix")
  Use-VsixTool -vsDir $vsDir -vsId $vsId -baseArgs $baseArgs -hive $hive -vsixes $vsixes
}

function Uninstall-VsixViaTool([string]$vsDir, [string]$vsId, [string]$hive) {
  $baseArgs = "/rootSuffix:$hive /u /vsInstallDir:`"$vsDir`""

  $attempt = 3
  $success = $false
  while ($attempt -gt 0 -and -not $success) {
    try {
      $vsixes = @("vsix\RoslynDeployment.vsix")
      Use-VsixTool -vsDir $vsDir -vsId $vsId -baseArgs $baseArgs -hive $hive -vsixes $vsixes
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

function Use-VsixTool([string]$vsDir, [string]$vsId, [string]$baseArgs, [string]$hive, [string[]]$vsixes) {
  $vsixExe = Join-Path $PSScriptRoot "vsixexpinstaller\VsixExpInstaller.exe"
  $vsixExe = "`"$vsixExe`""
  Write-Host "Using VS Instance $vsId at `"$vsDir`"" -ForegroundColor Gray

  foreach ($e in $vsixes) {
    $name = $e
    $filePath = "`"$((Resolve-Path $e).Path)`""
    $fullArg = "$baseArgs $filePath"
    Exec-Console $vsixExe $fullArg
  }
}
