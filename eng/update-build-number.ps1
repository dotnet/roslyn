[CmdletBinding(PositionalBinding=$false)]
param (
  [string]$buildNumber,
  [string]$oddOrEven)

# Build number is in the format of YYYYMMDD.R
$buildNumberYYYYMMDD = $buildNumber.Substring(0, 8)
$buildNumberRev = [int]($buildNumber.Substring(9))
$updatedRev = $buildNumberRev

if ($oddOrEven -eq 'odd') {
  $updatedRev = $buildNumberRev * 2 -1
}
elseif ($oddOrEven -eq 'even') {
  $updatedRev = $buildNumberRev * 2
}

$updatedBuildNumber = $buildNumberYYYYMMDD + '.' + $updatedRev
Write-Host "Setting BuildNumber to $updatedBuildNumber"
Write-Host "##vso[build.updatebuildnumber]$updatedBuildNumber"