# The original of this file is in the PostSharp.Engineering repo.
# You can generate this file using `./Build.ps1 generate-scripts`.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SecretsPath
)

if (-not (Test-Path $SecretsPath))
{
    Write-Error "Secrets file not found: $SecretsPath"
    exit 1
}

try
{
    Write-Host "Reading secrets from: $SecretsPath" -ForegroundColor Cyan
    $secrets = Get-Content -Path $SecretsPath -Raw | ConvertFrom-Json

    foreach ($property in $secrets.PSObject.Properties)
    {
        $name = $property.Name
        $value = $property.Value

        Write-Host "Setting environment variable: $name" -ForegroundColor Green
        [Environment]::SetEnvironmentVariable($name, $value, [EnvironmentVariableTarget]::Machine)
    }

    Write-Host "Successfully set $( $secrets.PSObject.Properties.Count ) environment variables" -ForegroundColor Green
}
catch
{
    Write-Error "Failed to read or parse secrets file: $_"
    exit 1
}
