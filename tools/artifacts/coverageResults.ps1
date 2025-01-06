$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")

$coverageFiles = @(Get-ChildItem "$RepoRoot/test/*.cobertura.xml" -Recurse | Where {$_.FullName -notlike "*/In/*"  -and $_.FullName -notlike "*\In\*" })

# Prepare code coverage reports for merging on another machine
$repoRoot = $env:SYSTEM_DEFAULTWORKINGDIRECTORY
if (!$repoRoot) { $repoRoot = $env:GITHUB_WORKSPACE }
if ($repoRoot) {
    Write-Host "Substituting $repoRoot with `"{reporoot}`""
    $coverageFiles |% {
        $content = Get-Content -LiteralPath $_ |% { $_ -Replace [regex]::Escape($repoRoot), "{reporoot}" }
        Set-Content -LiteralPath $_ -Value $content -Encoding UTF8
    }
} else {
    Write-Warning "coverageResults: Cloud build not detected. Machine-neutral token replacement skipped."
}

if (!((Test-Path $RepoRoot\bin) -and (Test-Path $RepoRoot\obj))) { return }

@{
    $RepoRoot = (
        $coverageFiles +
        (Get-ChildItem "$RepoRoot\obj\*.cs" -Recurse)
    );
}
