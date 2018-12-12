[CmdletBinding(PositionalBinding=$false)]
param ([string]$configuration = "Debug")

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $RepoRoot

    $optProfToolDir = Get-PackageDir "Roslyn.OptProf.RunSettings.Generator"
    $optProfToolExe = Join-Path $optProfToolDir "tools\roslyn.optprof.runsettings.generator.exe"
    $configFile = Join-Path $EngRoot "config\OptProf.json"
    $outputFolder = Join-Path $VSSetupDir "Insertion\RunSettings"
    $optProfArgs = "--configFile $configFile --outputFolder $outputFolder"
    
    # https://github.com/dotnet/roslyn/issues/31486
    $dest = Join-Path $RepoRoot ".vsts-ci.yml"
    try {
        Copy-Item (Join-Path $RepoRoot "azure-pipelines-official.yml") $dest
        Exec-Console $optProfToolExe $optProfArgs
    }
    finally {
        Remove-Item $dest
    }

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
