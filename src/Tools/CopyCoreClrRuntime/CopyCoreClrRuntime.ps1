param(
    [Parameter(Mandatory=$true)]
    $BinariesPath
)

$repoRoot = Resolve-Path "$PSScriptRoot\..\..\..\"
$nugetDir = Resolve-Path "$repoRoot\.nuget"
$packagesDir = Resolve-Path "$repoRoot\packages"

& "$nugetDir\nuget.exe" restore "$PSScriptRoot\packages.config" `
                        -PackagesDirectory $packagesDir `
                        -ConfigFile "$nugetDir\NuGet.Config"

$consoleHostPath = "$packagesDir\Microsoft.NETCore.Runtime.CoreCLR.ConsoleHost-x86.1.0.0-beta-22713\native\win\x86\CoreConsole.exe"

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

Copy-Item -Path "$packagesDir\Microsoft.NETCore.Runtime.CoreCLR-x86.1.0.0-beta-22713\lib\netcore50~windows\x86\*" `
                -Destination "$BinariesPath"
