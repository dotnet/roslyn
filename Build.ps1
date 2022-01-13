if ( $env:VisualStudioVersion -eq $null ) {
    Import-Module "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\Tools\Microsoft.VisualStudio.DevShell.dll"
    Enter-VsDevShell -VsInstallPath "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\" -StartInPath $(Get-Location)
}

(& dotnet nuget locals http-cache -c) | Out-Null
& dotnet run --project "$PSScriptRoot\eng-Metalama\src\BuildMetalamaCompiler.csproj" -- $args
exit $LASTEXITCODE

