[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$clean = $false, 
    [switch]$fast = $false,
    [string]$msbuildDir = "")

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Run-Restore([string]$name, [string]$fileName) {
    Write-Host "Restoring $name"
    $nugetConfig = Join-Path $repoDir "nuget.config"
    $filePath = Join-Path $repoDir $fileName
    Exec { & $nuget restore -verbosity quiet -configfile $nugetConfig -MSBuildPath $msbuildDir -Project2ProjectTimeOut 1200 $filePath }
}

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")

    $nuget = Ensure-NuGet
    if ($msbuildDir -eq "") {
        $msbuildDir = Get-MSBuildDir
    }

    Write-Host "Restore using MSBuild at $msbuildDir"

    if ($clean) {
        Write-Host "Clearing the NuGet caches"
        Exec { & $nuget locals all -clear }
    }

    if (-not $fast) { 
        Write-Host "Deleting project.lock.json files"
        Get-ChildItem $repoDir -re -in project.lock.json | Remove-Item
    }

    Run-Restore "Toolsets" "build\ToolsetPackages\project.json"
    Run-Restore "Toolsets (Dev14 VS SDK build tools)" "build\ToolsetPackages\dev14.project.json"
    Run-Restore "Toolsets (Dev15 VS SDK RC build tools)" "build\ToolsetPackages\dev15rc.project.json" 
    Run-Restore "Samples" "src\Samples\Samples.sln" 
    Run-Restore "Templates" "src\Setup\Templates\Templates.sln"
    Run-Restore "Toolsets Compiler" "build\Toolset\Toolset.csproj"
    Run-Restore "Roslyn" "Roslyn.sln"
    Run-Restore "DevDivInsertionFiles" "src\Setup\DevDivInsertionFiles\DevDivInsertionFiles.sln"
    Run-Restore "DevDiv Roslyn Packages" "src\Setup\DevDivPackages\Roslyn\project.json" 
    Run-Restore "DevDiv Debugger Packages" "src\Setup\DevDivPackages\Debugger\project.json" 
}
catch {
  Write-Host $_
  exit 1
}
