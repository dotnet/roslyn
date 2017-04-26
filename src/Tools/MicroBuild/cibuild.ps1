
[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

# Kill any instances VBCSCompiler.exe to release locked files, ignoring stderr if process is not open
# This prevents future CI runs from failing while trying to delete those files.
# Kill any instances of msbuild.exe to ensure that we never reuse nodes (e.g. if a non-roslyn CI run
# left some floating around).
function Terminate-BuildProcesses() {
    Get-Process msbuild -ErrorAction SilentlyContinue | kill 
    Get-Process vbcscompiler -ErrorAction SilentlyContinue | kill
}

try {
    Write-Host "${env:Userprofile}"
    . (Join-Path $PSScriptRoot "..\..\..\build\scripts\build-utils.ps1")
    Push-Location $PSScriptRoot

    $nuget = Ensure-NuGet
    Exec-Block { & $nuget locals all -clear } | Out-Host

    $msbuild = Ensure-MSBuild

    # The /nowarn exception can be removed once we fix https://github.com/dotnet/roslyn/issues/17325
    Exec-Block { & $msbuild /nodereuse:false /p:Configuration=Release /p:SkipTest=true Build.proj /warnaserror /nowarn:MSB3277 } | Out-Host

    exit 0
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
finally {
    Pop-Location
}
