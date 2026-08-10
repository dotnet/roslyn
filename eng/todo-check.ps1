<#
  Checks that TODO2, PROTOTYPE, and merge conflict markers are not present.
#>

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Get-GitGrepMatches([string] $pattern, [string[]] $pathspecs, [switch] $extendedRegex) {
  $arguments = @(
    'grep',
    '-n', # Include line numbers in reported matches.
    '-I'  # Skip binary files.
  )
  if ($extendedRegex) {
    $arguments += '-E' # Treat the pattern as an extended regular expression.
  } else {
    $arguments += '-F' # Treat the pattern as a fixed string.
  }

  $arguments += @('-e', $pattern, '--')
  $arguments += ($pathspecs | ForEach-Object { $_.Replace('\', '/') })

  # A git grep exit code of 1 means no matches, not failure. Handle it below instead of letting PowerShell throw.
  $hasNativeCommandUseErrorActionPreference = Test-Path variable:PSNativeCommandUseErrorActionPreference
  if ($hasNativeCommandUseErrorActionPreference) {
    $previousNativeCommandUseErrorActionPreference = $PSNativeCommandUseErrorActionPreference
    $PSNativeCommandUseErrorActionPreference = $false
  }
  try {
    $result = & git @arguments
    $exitCode = $LASTEXITCODE
  } finally {
    if ($hasNativeCommandUseErrorActionPreference) {
      $PSNativeCommandUseErrorActionPreference = $previousNativeCommandUseErrorActionPreference
    }
  }

  if ($exitCode -eq 0) {
    return $result
  } elseif ($exitCode -eq 1) {
    $global:LASTEXITCODE = 0
    return @()
  }

  throw "git grep failed with exit code $exitCode"
}

# Verify no PROTOTYPE marker left in main
$prototypePathSpecs = @(
  '.',
  ':!eng/todo-check.ps1',
  ':!AGENTS.md',
  ':!azure-pipelines.yml',
  ':!docs/wiki/Compiler-release-process.md',
  ':!.github/copilot-instructions.md',
  ':!.github/memory/CONVENTIONS.md',
  ':!.github/memory/KNOWN_ISSUES.md'
)

if ($env:SYSTEM_PULLREQUEST_TARGETBRANCH -eq "main") {
  Write-Host "Checking no PROTOTYPE markers in checked-in files"
  $prototypes = Get-GitGrepMatches 'PROTOTYPE' $prototypePathSpecs
  if ($prototypes) {
    Write-Host "Found PROTOTYPE markers in checked-in files:"
    Write-Host $prototypes
    throw "PROTOTYPE markers disallowed in checked-in files"
  }
}

# Verify no TODO2 marker left
$todo2PathSpecs = @(
  '.',
  ':!eng/todo-check.ps1',
  ':!AGENTS.md',
  ':!.github/copilot-instructions.md',
  ':!.github/memory/CONVENTIONS.md',
  ':!.github/memory/KNOWN_ISSUES.md'
)

$todo2s = Get-GitGrepMatches 'TODO2' $todo2PathSpecs
if ($todo2s) {
  Write-Host "Found TODO2 markers in checked-in files:"
  Write-Host $todo2s
  throw "TODO2 markers disallowed in checked-in files"
}

# Verify no merge markers left
$mergeMarkerPathSpecs = @(
  '.',
  ':!eng/todo-check.ps1',
  ':!src/Analyzers/CSharp/Tests/ConflictMarkerResolution/ConflictMarkerResolutionTests.cs',
  ':!src/Analyzers/VisualBasic/Tests/ConflictMarkerResolution/ConflictMarkerResolutionTests.vb',
  ':!src/Compilers/CSharp/Test/Syntax/LexicalAndXml/LexicalTests.cs',
  ':!src/EditorFeatures/Test2/Rename/RenameTagProducerTests.vb',
  ':!src/EditorFeatures/VisualBasicTest/Classification/SyntacticClassifierTests.vb'
)

$mergeMarkers = Get-GitGrepMatches '^(<<<<<<<|\|\|\|\|\|\||>>>>>>>)' $mergeMarkerPathSpecs -extendedRegex
if ($mergeMarkers) {
  Write-Host "Found merge markers in checked-in files:"
  Write-Host $mergeMarkers
  throw "Merge markers disallowed in checked-in files"
}
