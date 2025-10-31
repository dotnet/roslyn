[CmdletBinding(PositionalBinding=$false)]
param([string]$configuration = "Debug",
      [string]$msbuildEngine = "vs",
      [string]$altRootDrive = "q:",
      [string]$bootstrapDir = "",
      [switch]$ci = $false,
      [switch]$help)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
  Write-Host "Usage: test-determinism.ps1"
  Write-Host "  -configuration <value>    Build configuration ('Debug' or 'Release')"
  Write-Host "  -msbuildEngine <value>    Msbuild engine to use to run build ('dotnet', 'vs', or unspecified)."
  Write-Host "  -bootstrapDir             Directory containing the bootstrap compiler"
  Write-Host "  -altRootDrive             The drive we build on (via subst) for verifying pathmap implementation"
}

if ($help) {
  Print-Usage
  exit 0
}

# List of binary names that should be skipped because they have a known issue that
# makes them non-deterministic.
$script:skipList = @(
  # Added to work around https://github.com/dotnet/roslyn/issues/48417
  "Microsoft.CodeAnalysis.EditorFeatures2.UnitTests.dll",

  # Work around XLF issues https://github.com/dotnet/roslyn/issues/58840
  "Roslyn.VisualStudio.DiagnosticsWindow.dll.key",

  # Work around resx issues https://github.com/dotnet/roslyn/issues/77544
  "Text.Analyzers.dll.key"
)

function Run-Build([string]$rootDir, [string]$logFileName) {

  # Clean out the previous run
  Write-Host "Cleaning binaries"
  $stopWatch = [System.Diagnostics.StopWatch]::StartNew()
  Remove-Item -Recurse (Get-BinDir $rootDir) -ErrorAction SilentlyContinue
  Remove-Item -Recurse (Get-ObjDir $rootDir) -ErrorAction SilentlyContinue
  $stopWatch.Stop()
  Write-Host "Cleaning took $($stopWatch.Elapsed)"

  $solution = Join-Path $rootDir "Roslyn.sln"

  $toolsetBuildProj = InitializeToolset

  if ($logFileName -eq "") {
    $logFileName = [IO.Path]::GetFileNameWithoutExtension($projectFilePath)
  }
  $logFileName = [IO.Path]::ChangeExtension($logFileName, ".binlog")
  $logFilePath = Join-Path $LogDir $logFileName

  Stop-Processes

  Write-Host "Building $solution using $bootstrapDir"
  MSBuild $toolsetBuildProj `
     /p:Projects=$solution `
     /p:Restore=true `
     /p:Build=true `
     /p:DebugDeterminism=true `
     /p:Features="debug-determinism" `
     /p:DeployExtension=false `
     /p:RepoRoot=$rootDir `
     /p:TreatWarningsAsErrors=true `
     /p:BootstrapBuildPath=$bootstrapDir `
     /p:DeterministicSourcePaths=true `
     /p:RunAnalyzers=false `
     /p:RunAnalyzersDuringBuild=false `
     /bl:$logFilePath

  Stop-Processes
}

function Get-ObjDir([string]$rootDir) {
  return Join-Path $rootDir "artifacts\obj"
}

function Get-BinDir([string]$rootDir) {
  return Join-Path $rootDir "artifacts\bin"
}

# Return all of the files that need to be processed for determinism under the given
# directory.
function Get-FilesToProcess([string]$rootDir) {
  $objDir = Get-ObjDir $rootDir
  foreach ($item in Get-ChildItem -re -in *.dll,*.exe,*.pdb,*.sourcelink.json,*.key $objDir) {
    $filePath = $item.FullName
    $fileName = Split-Path -leaf $filePath
    $relativeDirectory = Split-Path -parent $filePath
    $relativeDirectory = $relativeDirectory.Substring($objDir.Length)
    $relativeDirectory = $relativeDirectory.TrimStart("\")

    if ($skipList.Contains($fileName)) {
      continue;
    }

    $fileId = $filePath.Substring($objDir.Length).Replace("\", ".").TrimStart(".")
    $fileHash = (Get-FileHash $filePath -algorithm MD5).Hash

    $data = @{}
    $data.Hash = $fileHash
    $data.Content = [IO.File]::ReadAllBytes($filePath)
    $data.FileId = $fileId
    $data.FileName = $fileName
    $data.FilePath = $filePath
    $data.RelativeDirectory = $relativeDirectory

    $keyFilePath = $filePath + ".key"
    $keyFileName = Split-Path -leaf $keyFilePath
    if (Test-Path $keyFilePath) {
      $data.KeyFileName = $keyFileName
      $data.KeyFilePath = $keyFilePath
      $data.KeyFileContent = [IO.File]::ReadAllBytes($keyFilePath)
    }
    else {
      $data.KeyFileName = ""
      $data.KeyFilePath = ""
      $data.KeyFileContent = $null
    }

    Write-Output $data
  }
}

# This will build up the map of all of the binaries and their respective hashes.
function Record-Binaries([string]$rootDir) {
  $stopWatch = [System.Diagnostics.StopWatch]::StartNew()
  Write-Host "Recording file hashes"

  $map = @{ }
  foreach ($fileData in Get-FilesToProcess $rootDir) {
    Write-Host "`t$($fileData.FileId) = $($fileData.Hash)"
    $map[$fileData.FileId] = $fileData
  }
  $stopWatch.Stop()
  Write-Host "Recording took $($stopWatch.Elapsed)"
  return $map
}

# This is a sanity check to ensure that we're actually putting the right entries into
# the core data map. Essentially to ensure things like if we change our directory layout
# that this test fails beacuse we didn't record the binaries we intended to record.
function Test-MapContents($dataMap) {

  # Sanity check to ensure we didn't return a false positive because we failed
  # to examine any binaries.
  if ($dataMap.Count -lt 40) {
    throw "Didn't find the expected count of binaries"
  }

  # Test for some well known binaries
  $list = @(
    "Microsoft.CodeAnalysis.dll",
    "Microsoft.CodeAnalysis.CSharp.dll",
    "Microsoft.CodeAnalysis.Workspaces.dll",
    "Microsoft.VisualStudio.LanguageServices.Implementation.dll")

  foreach ($fileName in $list) {
    $found = $false
    foreach ($value in $dataMap.Values) {
      if ($value.FileName -eq $fileName) {
        $found = $true
        break;
      }
    }

    if (-not $found) {
      throw "Did not find the expected binary $fileName"
    }
  }
}

function Test-Build([string]$rootDir, $dataMap, [string]$logFileName) {
  Run-Build $rootDir -logFile $logFileName

  $errorList = @()
  $allGood = $true

  Write-Host "Testing the binaries"
  $stopWatch = [System.Diagnostics.StopWatch]::StartNew()
  foreach ($fileData in Get-FilesToProcess $rootDir) {
    $fileId = $fileData.FileId
    $fileName = $fileData.FileName
    $filePath = $fileData.FilePath
    $relativeDir = $fileData.RelativeDirectory

    if (-not $dataMap.Contains($fileId)) {
      Write-Host "ERROR! Missing entry in map $fileId->$filePath"
      $allGood = $false
      continue
    }

    $oldfileData = $datamap[$fileId]
    if ($fileData.Hash -ne $oldFileData.Hash) {
      Write-Host "`tERROR! $relativeDir\$fileName contents don't match"
      $allGood = $false
      $errorList += $fileName

      $errorCurrentDirLeft = Join-Path $errorDirLeft $relativeDir
      Create-Directory $errorCurrentDirLeft
      $errorCurrentDirRight = Join-Path $errorDirRight $relativeDir
      Create-Directory $errorCurrentDirRight

      # Save out the original and baseline for investigation
      [IO.File]::WriteAllBytes((Join-Path $errorCurrentDirLeft $fileName), $oldFileData.Content)
      Copy-Item $filePath (Join-Path $errorCurrentDirRight $fileName)

      # Copy the key files if available too
      $keyFileName = $oldFileData.KeyFileName
      if ($keyFileName -ne "") {
        [IO.File]::WriteAllBytes((Join-Path $errorCurrentDirLeft $keyFileName), $oldFileData.KeyFileContent)
        Copy-Item $fileData.KeyFilePath (Join-Path $errorCurrentDirRight $keyFileName)
      }

      continue
    }

    Write-Host "`tVerified $relativeDir\$fileName"
  }

  if ($allGood) {
    Write-Host "Determinism check succeeded"
  }
  else {
    Write-Host "Determinism failed for the following binaries:"
    foreach ($name in $errorList) {
      Write-Host "`t$name"
    }

    Write-Host "Archiving failure information"
    $zipFile = Join-Path $LogDir "determinism.zip"
    Add-Type -Assembly "System.IO.Compression.FileSystem";
    [System.IO.Compression.ZipFile]::CreateFromDirectory($script:errorDir, $zipFile, "Fastest", $true);

    Write-Host "Please send $zipFile to compiler team for analysis"
    exit 1
  }

  $stopWatch.Stop()
  Write-Host "Testing took $($stopWatch.Elapsed)"
}

function Run-Test() {
  # Run the initial build so that we can populate the maps
  Run-Build $RepoRoot -logFileName "Initial" -useBootstrap
  $dataMap = Record-Binaries $RepoRoot
  Test-MapContents $dataMap

  # Run a test against the source in the same directory location
  Test-Build -rootDir $RepoRoot -dataMap $dataMap -logFileName "test1"

  # Run another build in a different source location and verify that path mapping
  # allows the build to be identical.  To do this we'll copy the entire source
  # tree under the artifacts\q directory and run a build from there.
  Write-Host "Building in a different directory"
  Exec-Command "subst" "$altRootDrive $(Split-Path -parent $RepoRoot)"
  try {
    $altRootDir = Join-Path (Join-Path "$($altRootDrive)\" (Split-Path -leaf $RepoRoot)) "\"
    Test-Build -rootDir $altRootDir -dataMap $dataMap -logFileName "test2"
  }
  finally {
    Exec-Command "subst" "$altRootDrive /d"
  }
}

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot
  $prepareMachine = $ci

  # We need to make sure DOTNET_HOST_PATH points to that SDK as a workaround for older MSBuild
  # which will not set it correctly.  Removal is tracked by https://github.com/dotnet/roslyn/issues/80742
  if (-not $env:DOTNET_HOST_PATH) {
    $env:DOTNET_HOST_PATH = Ensure-DotnetSdk
    Write-Host "Setting DOTNET_HOST_PATH to $env:DOTNET_HOST_PATH"
  }

  # Create all of the logging directories
  $errorDir = Join-Path $LogDir "DeterminismFailures"
  $errorDirLeft = Join-Path $errorDir "Left"
  $errorDirRight = Join-Path $errorDir "Right"

  Create-Directory $LogDir
  Create-Directory $errorDirLeft
  Create-Directory $errorDirRight

  $runAnalyzers = $false
  $binaryLog = $true
  $officialBuildId = ""
  $nodeReuse = $false
  $properties = @()

  if ($bootstrapDir -eq "") {
    Write-Host "Building bootstrap compiler"
    $bootstrapDir = Join-Path $ArtifactsDir (Join-Path "bootstrap" "determinism")
    & eng/make-bootstrap.ps1 -output $bootstrapDir -ci:$ci -force
    Test-LastExitCode
  }

  Run-Test
  ExitWithExitCode 0
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
finally {
  Pop-Location
}

