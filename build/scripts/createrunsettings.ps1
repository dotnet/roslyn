[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$release = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $repoDir
    
    Write-Host "Repo Dir $repoDir"
    Write-Host "Binaries Dir $binariesDir"
    
    $buildConfiguration = if ($release) { "Release" } else { "Debug" }
    $configDir = Join-Path (Join-Path $binariesDir "VSSetup") $buildConfiguration
    
    $optProfToolDir = Get-PackageDir "Roslyn.OptProf.RunSettings.Generator"
    $optProfToolExe = Join-Path $optProfToolDir "tools\roslyn.optprof.runsettings.generator.exe"
    $configFile = Join-Path $repoDir "build\config\optprof.json"
    $outputFolder = Join-Path $configDir "Insertion\RunSettings"
    $optProfArgs = "--configFile $configFile --outputFolder $outputFolder --buildNumber 28218.3001 "
    
    Exec-Console $optProfToolExe $optProfArgs
    exit 0
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
finally {
    Pop-Location
}