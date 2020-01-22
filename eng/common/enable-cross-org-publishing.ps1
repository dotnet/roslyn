param(
  [string] $token
)

# Work around https://github.com/dotnet/arcade/issues/4660

Write-Host "##vso[task.setvariable variable=VSS_NUGET_ACCESSTOKEN]$token"
Write-Host "##vso[task.setvariable variable=VSS_NUGET_URI_PREFIXES]https://dnceng.pkgs.visualstudio.com/;https://pkgs.dev.azure.com/dnceng/;https://devdiv.pkgs.visualstudio.com/;https://pkgs.dev.azure.com/devdiv/"
