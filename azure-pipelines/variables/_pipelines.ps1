# This script translates the variables returned by the _all.ps1 script
# into commands that instruct Azure Pipelines to actually set those variables for other pipeline tasks to consume.

# The build or release definition may have set these variables to override
# what the build would do. So only set them if they have not already been set.

(& "$PSScriptRoot\_all.ps1").GetEnumerator() |% {
    if (Test-Path -Path "env:$($_.Key.ToUpper())") {
        Write-Host "Skipping setting $($_.Key) because variable is already set." -ForegroundColor Cyan
    } else {
        Write-Host "$($_.Key)=$($_.Value)" -ForegroundColor Yellow
        Write-Host "##vso[task.setvariable variable=$($_.Key);]$($_.Value)"
        Set-Item -Path "env:$($_.Key)" -Value $_.Value
    }
}
