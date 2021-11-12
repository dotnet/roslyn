$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")

# Prepare code coverage reports for merging on another machine
if ($env:SYSTEM_DEFAULTWORKINGDIRECTORY) {
    Write-Host "Substituting $env:SYSTEM_DEFAULTWORKINGDIRECTORY with `"{reporoot}`""
    $reports = Get-ChildItem "$RepoRoot/bin/coverage.*cobertura.xml" -Recurse
    $reports |% {
        $content = Get-Content -Path $_ |% { $_ -Replace [regex]::Escape($env:SYSTEM_DEFAULTWORKINGDIRECTORY), "{reporoot}" }
        Set-Content -Path $_ -Value $content -Encoding UTF8
    }
} else {
    Write-Warning "coverageResults: Azure Pipelines not detected. Machine-neutral token replacement skipped."
}

if (!((Test-Path $RepoRoot\bin) -and (Test-Path $RepoRoot\obj))) { return }

@{
    $RepoRoot = (
        @(Get-ChildItem "$RepoRoot\bin\coverage.*cobertura.xml" -Recurse) +
        (Get-ChildItem "$RepoRoot\obj\*.cs" -Recurse)
    );
}
