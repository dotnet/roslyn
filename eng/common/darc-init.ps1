param (
    $darcVersion = $null,
    $versionEndpoint = 'https://maestro.dot.net/api/assets/darc-version?api-version=2019-01-16',
    $verbosity = 'minimal',
    $toolpath = $null
)

. $PSScriptRoot\tools.ps1

function InstallDarcCli ($darcVersion, $toolpath) {
  # pin darc to the last version targeting net6.0
  $darcVersion = "1.1.0-beta.25522.2"
  $darcCliPackageName = 'microsoft.dotnet.darc'

  $dotnetRoot = InitializeDotNetCli -install:$true
  $dotnet = "$dotnetRoot\dotnet.exe"
  $toolList = & "$dotnet" tool list -g

  if ($toolList -like "*$darcCliPackageName*") {
    & "$dotnet" tool uninstall $darcCliPackageName -g
  }

  $arcadeServicesSource = 'https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json'

  Write-Host "Installing Darc CLI version $darcVersion..."
  Write-Host 'You may need to restart your command window if this is the first dotnet tool you have installed.'
  if (-not $toolpath) {
    Write-Host "'$dotnet' tool install $darcCliPackageName --version $darcVersion --add-source '$arcadeServicesSource' -v $verbosity -g"
    & "$dotnet" tool install $darcCliPackageName --version $darcVersion --add-source "$arcadeServicesSource" -v $verbosity -g
  }else {
    Write-Host "'$dotnet' tool install $darcCliPackageName --version $darcVersion --add-source '$arcadeServicesSource' -v $verbosity --tool-path '$toolpath'"
    & "$dotnet" tool install $darcCliPackageName --version $darcVersion --add-source "$arcadeServicesSource" -v $verbosity --tool-path "$toolpath"
  }
}

try {
  InstallDarcCli $darcVersion $toolpath
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'Darc' -Message $_
  ExitWithExitCode 1
}