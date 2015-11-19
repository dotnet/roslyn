param(
    [Parameter(Mandatory=$true)]
    $BinariesPath
)

$repoRoot = Resolve-Path "$PSScriptRoot\..\..\..\"
$nugetDir = Resolve-Path "$repoRoot"
$packagesDir = Resolve-Path "$repoRoot\packages"

[xml]$configFile = Get-Content "$nugetDir\NuGet.Config"

$sources = $configFile.configuration.packageSources.add | %{ '-s', $_.value }

$args = @(
    "restore",
    "$PSScriptRoot\project.json",
    "--packages",
    "$packagesDir"
) + $sources

& "$packagesDir\dnx-coreclr-win-x86.1.0.0-beta5-12101\bin\dnu.cmd" $args

$consoleHostPath = "$packagesDir\Microsoft.NETCore.Runtime.CoreCLR.ConsoleHost-x86\" +
                   "1.0.0-beta-22914\runtimes\win7-x86\native\CoreConsole.exe"

function ReplaceExeWithConsoleHost([string]$exeName)
{
    $exePath = "$BinariesPath/$exeName"
    $dllPath = [System.IO.Path]::ChangeExtension($exePath, "dll")
    if (Test-Path "$exePath")
    {
        Move-Item -Force -Path $exePath -Destination "$dllPath"
        Copy-Item -Path "$consoleHostPath" -Destination "$exePath"
    }
}

ReplaceExeWithConsoleHost "csc.exe"
ReplaceExeWithConsoleHost "vbc.exe"

$runtimePkgDir = "$packagesDir\Microsoft.NETCore.Runtime.CoreCLR-x86\1.0.0-beta-23019\runtimes\win7-x86"

Copy-Item -Path "$runtimePkgDir\lib\dotnet\*" `
                -Destination "$BinariesPath"
Copy-Item -Path "$runtimePkgDir\native\*" `
                -Destination "$BinariesPath"
