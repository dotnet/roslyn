param (
    [Parameter(Mandatory=$true)][string]$filePath,
    [Parameter(Mandatory=$true)][string]$framework,
    [Parameter(Mandatory=$false)][string]$filter
)

Set-StrictMode -version 3.0
$ErrorActionPreference="Stop"

. (Join-Path $PSScriptRoot "../eng/build-utils.ps1")

Push-Location

$originalCwd = Get-Location
$fileInfo = Get-ItemProperty $filePath
Set-Location $fileInfo.Directory

try {
    while ($true) {
        # search up from the current file for a folder containing a csproj
        $files = Get-ChildItem *.csproj
        if ($files) {
            Set-Location $originalCwd
            $dotnetPath = Resolve-Path (Ensure-DotNetSdk) -Relative

            $projectFileInfo = $files[0]
            $projectDir = Resolve-Path $projectFileInfo.Directory -Relative

            $filterArg = if ($filter) {" --filter $filter"} else {""}
            $logFileName = if ($filter) {$fileInfo.Name} else {$projectFileInfo.Name}

            $resultsPath = Join-Path (Resolve-Path "$PSScriptRoot/.." -Relative) "/artifacts/TestResults"
            New-Item -ItemType Directory -Force -Path $resultsPath | Out-Null

            $invocation = "$dotnetPath test $projectDir$filterArg --framework $framework --logger `"html;LogFileName=$logfileName.html`" --results-directory $resultsPath"
            Write-Output "> $invocation"
            Invoke-Expression $invocation

            exit 0
        }
        else {
            $location = Get-Location
            Set-Location ..
            if ((Get-Location).Path -eq $location.Path) {
                # our location didn't change. We must be at the drive root, so give up
                Write-Host "Failed to run tests. $fileInfo is not part of a C# project."

                exit 1
            }
        }
    }
}
finally {
    Pop-Location
}
