<#
  This script drives the Jenkins verification that our build is correct.  In particular:

    - Our build has no double writes
    - Our project.json files are consistent
    - Our build files are well structured
    - Our solution states are consistent
    - Our generated files are consistent

#>

[CmdletBinding(PositionalBinding=$false)]
param(
  [string]$configuration = "Debug",
  [switch]$enableDumps = $false,
  [switch]$help)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Print-Usage() {
  Write-Host "Usage: test-build-correctness.ps1"
  Write-Host "  -configuration            Build configuration ('Debug' or 'Release')"
}

try {
  if ($help) {
    Print-Usage
    exit 0
  }

  $ci = $true

  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  if ($enableDumps) {
    $key = "HKLM:\\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps"
    New-Item -Path $key -ErrorAction SilentlyContinue
    New-ItemProperty -Path $key -Name 'DumpType' -PropertyType 'DWord' -Value 2 -Force
    New-ItemProperty -Path $key -Name 'DumpCount' -PropertyType 'DWord' -Value 2 -Force
    New-ItemProperty -Path $key -Name 'DumpFolder' -PropertyType 'String' -Value $LogDir -Force
  }

  # Verify no PROTOTYPE marker left in main
  if ($env:SYSTEM_PULLREQUEST_TARGETBRANCH -eq "main") {
    Write-Host "Checking no PROTOTYPE markers in source"
    $prototypes = Get-ChildItem -Path src, eng, scripts -Exclude *.dll,*.exe,*.pdb,*.xlf,test-build-correctness.ps1 -Recurse | Select-String -Pattern 'PROTOTYPE' -CaseSensitive -SimpleMatch
    if ($prototypes) {
      Write-Host "Found PROTOTYPE markers in source:"
      Write-Host $prototypes
      throw "PROTOTYPE markers disallowed in compiler source"
    }
  }

  Write-Host "Building Roslyn"
  Exec-Block { & (Join-Path $PSScriptRoot "build.ps1") -restore -build -bootstrap -bootstrapConfiguration:Debug -ci:$ci -runAnalyzers:$true -configuration:$configuration -pack -binaryLog -useGlobalNuGetCache:$false -warnAsError:$true -properties "/p:RoslynEnforceCodeStyle=true"}

  # Verify the state of our various build artifacts
  Write-Host "Running BuildBoss"
  $buildBossPath = GetProjectOutputBinary "BuildBoss.exe"
  Exec-Console $buildBossPath "-r `"$RepoRoot`" -c $configuration" -p Roslyn.sln
  Write-Host ""

  # Verify the state of our generated syntax files
  Write-Host "Checking generated compiler files"
  Exec-Block { & (Join-Path $PSScriptRoot "generate-compiler-code.ps1") -test -configuration:$configuration }
  Exec-Console dotnet "format . --include-generated --include src/Compilers/CSharp/Portable/Generated/ src/Compilers/VisualBasic/Portable/Generated/ src/ExpressionEvaluator/VisualBasic/Source/ResultProvider/Generated/ --check -f"
  Write-Host ""  
  
  exit 0
}
catch [exception] {
  Write-Host $_
  Write-Host $_.Exception
  exit 1
}
finally {
  if ($enableDumps) {
    $key = "HKLM:\\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps"
    Remove-ItemProperty -Path $key -Name 'DumpType'
    Remove-ItemProperty -Path $key -Name 'DumpCount'
    Remove-ItemProperty -Path $key -Name 'DumpFolder'
  }
  Pop-Location
}
