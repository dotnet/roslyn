# The original of this file is in the PostSharp.Engineering repo.
# You can generate this file using `./Build.ps1 generate-scripts`.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$envFilePath
)

if (-not (Test-Path $envFilePath))
{
    Write-Error "Environment file not found: $envFilePath"
    exit 1
}

try
{
    Write-Host "Reading environment variables from: $envFilePath" -ForegroundColor Cyan
    $secrets = Get-Content -Path $envFilePath -Raw | ConvertFrom-Json

    foreach ($property in $secrets.PSObject.Properties)
    {
        $name = $property.Name
        $value = $property.Value

        Write-Host "Setting environment variable: $name" -ForegroundColor Green
        [Environment]::SetEnvironmentVariable($name, $value, [EnvironmentVariableTarget]::Machine)
    }
}
catch
{
    Write-Error "Failed to read or parse secrets file: $_"
    exit 1
}
