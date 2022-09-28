(& dotnet nuget locals http-cache -c) | Out-Null
& dotnet run --project "$PSScriptRoot\eng-Metalama\src\BuildMetalamaCompiler.csproj" -- $args
exit $LASTEXITCODE

