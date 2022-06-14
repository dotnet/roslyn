$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")

$coverageFiles = @(Get-ChildItem "$RepoRoot/test/*.cobertura.xml" -Recurse | Where {$_.FullName -notlike "*/In/*"  -and $_.FullName -notlike "*\In\*" })

# Prepare code coverage reports for merging on another machine
if ($env:SYSTEM_DEFAULTWORKINGDIRECTORY) {
    Write-Host "Substituting $env:SYSTEM_DEFAULTWORKINGDIRECTORY with `"{reporoot}`""
    $coverageFiles |% {
        $content = Get-Content -Path $_ |% { $_ -Replace [regex]::Escape($env:SYSTEM_DEFAULTWORKINGDIRECTORY), "{reporoot}" }
        Set-Content -Path $_ -Value $content -Encoding UTF8
    }
} else {
    Write-Warning "coverageResults: Azure Pipelines not detected. Machine-neutral token replacement skipped."
}

if (!((Test-Path $RepoRoot\bin) -and (Test-Path $RepoRoot\obj))) { return }

@{
    $RepoRoot = (
        $coverageFiles +
        (Get-ChildItem "$RepoRoot\obj\*.cs" -Recurse)
    );
}
