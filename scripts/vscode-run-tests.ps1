param (
    [Parameter(Mandatory=$true)][string]$filePath,
    [Parameter(Mandatory=$true)][string]$framework,
    [Parameter(Mandatory=$false)][string]$filter
)

Set-StrictMode -version 3.0
$ErrorActionPreference="Stop"

Push-Location

$fileInfo = Get-ItemProperty $filePath
Set-Location $fileInfo.Directory

try
{
    while ($true)
    {
        # search up from the current file for a folder containing a csproj
        $files = Get-ChildItem *.csproj
        if ($files)
        {
            Pop-Location
            $dotnetPath = Resolve-Path "$PSScriptRoot/../.dotnet/dotnet.exe" -Relative

            $projectFileInfo = $files[0]
            $projectDir = Resolve-Path $projectFileInfo.Directory -Relative

            $filterArg = if ($filter) {" --filter $filter"} else {""}
            $logFileName = if ($filter) {$fileInfo.Name} else {$projectFileInfo.Name}

            $resultsPath = Resolve-Path "$PSScriptRoot/../artifacts/TestResults" -Relative

            $invocation = "$dotnetPath test $projectDir$filterArg --framework $framework --logger `"html;LogFileName=$logfileName.html`" --results-directory $resultsPath"
            Write-Output "> $invocation"
            Invoke-Expression $invocation

            break
        }
        else
        {
            $location = Get-Location
            Set-Location ..
            if ((Get-Location).Path -eq $location.Path)
            {
                # our location didn't change. We must be at the drive root, so give up
                Write-Host "Failed to run tests. $fileInfo is not part of a C# project."
                break
            }
        }
    }
}
finally
{
    Pop-Location
}
