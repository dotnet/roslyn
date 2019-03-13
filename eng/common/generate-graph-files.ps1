Param(
  [Parameter(Mandatory=$true)][string] $barToken,       # Token generated at https://maestro-prod.westus2.cloudapp.azure.com/Account/Tokens
  [Parameter(Mandatory=$true)][string] $gitHubPat,      # GitHub personal access token from https://github.com/settings/tokens (no auth scopes needed)
  [Parameter(Mandatory=$true)][string] $azdoPat,        # Azure Dev Ops tokens from https://dev.azure.com/dnceng/_details/security/tokens (code read scope needed)
  [Parameter(Mandatory=$true)][string] $outputFolder,   # Where the graphviz.txt file will be created
  [string] $darcVersion = '1.1.0-beta.19156.4',         # darc's version
  [switch] $includeToolset                              # Whether the graph should include toolset dependencies or not. i.e. arcade, optimization. For more about
                                                        # toolset dependencies see https://github.com/dotnet/arcade/blob/master/Documentation/Darc.md#toolset-vs-product-dependencies
)

$ErrorActionPreference = "Stop"
. $PSScriptRoot\tools.ps1

function CheckExitCode ([string]$stage)
{
  $exitCode = $LASTEXITCODE
  if ($exitCode  -ne 0) {
    Write-Host "Something failed in stage: '$stage'. Check for errors above. Exiting now..."
    ExitWithExitCode $exitCode
  }
}

try {
  Push-Location $PSScriptRoot
    
  Write-Host "Installing darc..."
  . .\darc-init.ps1 -darcVersion $darcVersion
  CheckExitCode "Running darc-init"

  $darcExe = "$env:USERPROFILE\.dotnet\tools"
  $darcExe = Resolve-Path "$darcExe\darc.exe"
  
  Create-Directory $outputFolder
  
  $graphVizFilePath = "$outputFolder\graphviz.txt"
  $graphFilePath = "$outputFolder\graph.txt"
  $options = "get-dependency-graph --graphviz '$graphVizFilePath' --github-pat $gitHubPat --azdev-pat $azdoPat --password $barToken --output-file $graphFilePath"
  
  if ($includeToolset) {
    Write-Host "Toolsets will be included in the graph..."
    $options += " --include-toolset"
  }

  Write-Host "Generating dependency graph..."
  Invoke-Expression "& `"$darcExe`" $options"
  CheckExitCode "Generating dependency graph"
  
  $graph = Get-Content $graphVizFilePath
  Set-Content $graphVizFilePath -Value "Paste the following digraph object in http://www.webgraphviz.com `r`n", $graph
  Write-Host "'$graphVizFilePath' and '$graphFilePath' created!"
}
catch {
  if (!$includeToolset) {
    Write-Host "This might be a toolset repo which includes only toolset dependencies. " -NoNewline -ForegroundColor Yellow
    Write-Host "Since -includeToolset is not set there is no graph to create. Include -includeToolset and try again..." -ForegroundColor Yellow
  }
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}