# The original of this file is in the PostSharp.Engineering repo.
# You can generate this file using `./Build.ps1 generate-scripts`.

[CmdletBinding(PositionalBinding = $false)]
param(
    [switch]$Interactive, # Opens an interactive PowerShell session
    [switch]$StartVsmon, # Enable the remote debugger.
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$BuildArgs   # Arguments passed to `Build.ps1` within the container.
)

####
# These settings are replaced by the generate-scripts command.
$EngPath = 'eng-Metalama'
$ProductName = 'MetalamaCompiler'
####

if ($StartVsmon)
{
    $vsmonport = 4024
    Write-Host "Starting Visual Studio Remote Debugger, listening at port $vsmonport." -ForegroundColor Cyan
    $vsmonProcess = Start-Process -FilePath "C:\msvsmon\msvsmon.exe" `
        -ArgumentList "/noauth","/anyuser","/silent","/port:$vsmonport","/timeout:2147483647" `
        -NoNewWindow -PassThru
}

# Change the prompt and window title in Docker.
if ($env:RUNNING_IN_DOCKER)
{
    function global:prompt
    {
        $host.UI.RawUI.WindowTitle = "[docker] " + (Get-Location).Path
        "[docker] $( Get-Location )> "
    }
}


if (-not $Interactive -or $BuildArgs)
{
    # Change the working directory so we can use a global.json that is specific to eng.
    $previousLocation = Get-Location

    Set-Location $PSScriptRoot\$EngPath\src

    try
    {

        # Run the project.
        & dotnet run --project "$PSScriptRoot\$EngPath\src\Build$ProductName.csproj" -- $BuildArgs

        if ($StartVsmon)
        {
            Write-Host ""
            Write-Host "Killing vsmon.exe."
            $vsmonProcess.Kill()
        }
    }
    finally
    {
        Set-Location $previousLocation
    }
}
