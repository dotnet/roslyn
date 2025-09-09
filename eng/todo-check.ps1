<#
  Checks that TODO and PROTOTYPE comments are not present.
#>

# Verify no PROTOTYPE marker left in main
Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"
if ($env:SYSTEM_PULLREQUEST_TARGETBRANCH -eq "main") {
  Write-Host "Checking no PROTOTYPE markers in source"
  $prototypes = Get-ChildItem -Path src, eng, scripts, docs\compilers -Exclude *.dll,*.exe,*.pdb,*.xlf,todo-check.ps1 -Recurse | Select-String -Pattern 'PROTOTYPE' -CaseSensitive -SimpleMatch
  if ($prototypes) {
    Write-Host "Found PROTOTYPE markers in source:"
    Write-Host $prototypes
    throw "PROTOTYPE markers disallowed in compiler source"
  }
}

# Verify no TODO2 marker left
$prototypes = Get-ChildItem -Path src, eng, scripts, docs\compilers -Exclude *.dll,*.exe,*.pdb,*.xlf,todo-check.ps1 -Recurse | Select-String -Pattern 'TODO2' -CaseSensitive -SimpleMatch
if ($prototypes) {
  Write-Host "Found TODO2 markers in source:"
  Write-Host $prototypes
  throw "TODO2 markers disallowed in compiler source"
}
  
