param (
  $verbosity = 'minimal'
)

. $PSScriptRoot\common\tools.ps1

function Invoke-DotNet {
  param (
    [string] $dotnet,
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $arguments
  )

  & "$dotnet" @arguments
  if ($LASTEXITCODE -ne 0) {
    throw "'$dotnet $($arguments -join ' ')' exited with code $LASTEXITCODE."
  }
}

function InstallRoslynTools {
  $packageName = 'Microsoft.RoslynTools'
  $packageSource = 'https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json'

  $dotnetRoot = InitializeDotNetCli -install:$true
  $dotnet = "$dotnetRoot\dotnet.exe"
  $toolList = & "$dotnet" tool list --global
  if ($LASTEXITCODE -ne 0) {
    throw "'$dotnet tool list --global' exited with code $LASTEXITCODE."
  }

  if ($toolList -like "*$packageName*") {
    Invoke-DotNet $dotnet tool uninstall $packageName --global
  }

  Write-Host 'Installing the latest prerelease RoslynTools...'
  Write-Host 'You may need to restart your command window if this is the first dotnet tool you have installed.'
  Invoke-DotNet $dotnet tool install $packageName --prerelease --source $packageSource --verbosity $verbosity --global
}

try {
  InstallRoslynTools
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'RoslynTools' -Message $_
  ExitWithExitCode 1
}
