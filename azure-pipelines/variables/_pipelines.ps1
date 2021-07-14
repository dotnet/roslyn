# This script translates the variables returned by the _all.ps1 script
# into commands that instruct Azure Pipelines to actually set those variables for other pipeline tasks to consume.

# The build or release definition may have set these variables to override
# what the build would do. So only set them if they have not already been set.

(& "$PSScriptRoot\_all.ps1").GetEnumerator() |% {
    # Always use ALL CAPS for env var names since Azure Pipelines converts variable names to all caps and on non-Windows OS, env vars are case sensitive.
    $keyCaps = $_.Key.ToUpper()
    if (Test-Path -Path "env:$keyCaps") {
        Write-Host "Skipping setting $keyCaps because variable is already set to '$(Get-Content env:$keyCaps)'." -ForegroundColor Cyan
    } else {
        Write-Host "$keyCaps=$($_.Value)" -ForegroundColor Yellow
        if ($env:TF_BUILD) {
            # Create two variables: the first that can be used by its simple name and accessible only within this job.
            Write-Host "##vso[task.setvariable variable=$keyCaps]$($_.Value)"
            # and the second that works across jobs and stages but must be fully qualified when referenced.
            Write-Host "##vso[task.setvariable variable=$keyCaps;isOutput=true]$($_.Value)"
        } elseif ($env:GITHUB_ACTIONS) {
            Add-Content -Path $env:GITHUB_ENV -Value "$keyCaps=$($_.Value)"
        }
        Set-Item -Path "env:$keyCaps" -Value $_.Value
    }
}
