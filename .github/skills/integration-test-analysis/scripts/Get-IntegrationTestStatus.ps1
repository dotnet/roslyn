<#
.SYNOPSIS
    Analyzes Visual Studio integration test failures from Azure DevOps builds.

.DESCRIPTION
    Fetches build timeline, job logs, and published test artifacts from the
    roslyn-integration-CI pipeline. Parses exception Activity XMLs, screenshots,
    MEF errors, and VSIX installer logs to identify root causes of integration
    test failures and timeouts.

.PARAMETER BuildId
    Azure DevOps build ID to analyze.

.PARAMETER DownloadArtifacts
    Download and analyze published test artifacts (exception XMLs, screenshots, etc.).

.PARAMETER MaxArtifactSizeMB
    Maximum artifact size in MB to download. Artifacts larger than this are skipped.

.PARAMETER Organization
    Azure DevOps organization. Default: dnceng-public.

.PARAMETER Project
    Azure DevOps project. Default: public.

.EXAMPLE
    ./Get-IntegrationTestStatus.ps1 -BuildId 1354185
    Analyze build timeline and job logs.

.EXAMPLE
    ./Get-IntegrationTestStatus.ps1 -BuildId 1354185 -DownloadArtifacts
    Analyze build timeline, job logs, and download/parse test artifacts.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [int]$BuildId,

    [Parameter()]
    [switch]$DownloadArtifacts,

    [Parameter()]
    [int]$MaxArtifactSizeMB = 200,

    [Parameter()]
    [string]$Organization = "dnceng-public",

    [Parameter()]
    [string]$Project = "public"
)

$ErrorActionPreference = "Stop"

#region Utility Functions

function Write-Header([string]$text) {
    Write-Host ""
    Write-Host "=== $text ===" -ForegroundColor Cyan
}

function Write-Detail([string]$label, [string]$value) {
    Write-Host "  ${label}: " -NoNewline -ForegroundColor Gray
    Write-Host $value
}

function Write-ErrorLine([string]$text) {
    Write-Host "  [ERROR] $text" -ForegroundColor Red
}

function Write-WarningLine([string]$text) {
    Write-Host "  [WARNING] $text" -ForegroundColor Yellow
}

function Write-SuccessLine([string]$text) {
    Write-Host "  [OK] $text" -ForegroundColor Green
}

#endregion

#region Azure DevOps API Functions

function Get-AzDOBuild {
    param([int]$BuildId)
    $uri = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/${BuildId}?api-version=7.0"
    Invoke-RestMethod -Uri $uri -Method Get -Headers @{ 'Accept' = 'application/json' }
}

function Get-AzDOTimeline {
    param([int]$BuildId)
    $uri = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/${BuildId}/timeline?api-version=7.0"
    Invoke-RestMethod -Uri $uri -Method Get -Headers @{ 'Accept' = 'application/json' }
}

function Get-AzDOLog {
    param([int]$BuildId, [int]$LogId)
    $uri = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/${BuildId}/logs/${LogId}?api-version=7.0"
    try {
        $response = Invoke-WebRequest -Uri $uri -Method Get -Headers @{ 'Accept' = 'text/plain' }
        return $response.Content
    }
    catch {
        Write-WarningLine "Failed to fetch log $LogId : $($_.Exception.Message)"
        return $null
    }
}

function Get-AzDOArtifacts {
    param([int]$BuildId)
    $uri = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/${BuildId}/artifacts?api-version=7.0"
    $response = Invoke-RestMethod -Uri $uri -Method Get -Headers @{ 'Accept' = 'application/json' }
    return $response.value
}

function Get-AzDOArtifactZip {
    param([int]$BuildId, [string]$ArtifactName, [string]$OutputPath)
    $encodedName = [uri]::EscapeDataString($ArtifactName)
    $uri = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/${BuildId}/artifacts?artifactName=${encodedName}&api-version=7.0&`$format=zip"
    Invoke-WebRequest -Uri $uri -Method Get -OutFile $OutputPath
}

#endregion

#region Log Parsing Functions

function Parse-TestRunnerLog {
    <#
    .SYNOPSIS
        Parses the "Run Integration Tests" step log to extract test runner status.
    #>
    param([string]$LogContent)

    $result = @{
        vsVersion = $null
        vsixInstallSuccess = $true
        vsixInstallErrors = @()
        testRunnerCommandLine = $null
        testRunnerStatus = $null
        testRunnerStartTime = $null
        cancelTime = $null
        dotnetSdkVersion = $null
        queueName = $null
    }

    $lines = $LogContent -split "`n"

    foreach ($line in $lines) {
        # VS version detection
        if ($line -match 'Using VS Instance\s+\w+\s+\(([^)]+)\)\s+at\s+"([^"]+)"') {
            $result.vsVersion = $matches[1]
        }

        # VSIX install error detection
        if ($line -match 'VSIX installer failed with exit code:\s*(\d+)') {
            $result.vsixInstallSuccess = $false
            $result.vsixInstallErrors += "VSIX installer failed with exit code: $($matches[1])"
        }
        if ($line -match 'Install Error\s*:') {
            $result.vsixInstallSuccess = $false
            $result.vsixInstallErrors += ($line -replace '^\d{4}-\d{2}-\d{2}T[\d:.]+Z\s*', '').Trim()
        }

        # .NET SDK version
        if ($line -match 'dotnet-install: Installed version is (.+)') {
            $result.dotnetSdkVersion = $matches[1].Trim()
        }

        # Test runner command line
        if ($line -match 'RunTests\.dll\s+(.+)') {
            $result.testRunnerCommandLine = $matches[0].Trim()
        }

        # Test runner status line (e.g., "   1 running,  3 queued,  0 completed")
        if ($line -match '(\d+)\s+running,\s*(\d+)\s+queued,\s*(\d+)\s+completed') {
            $result.testRunnerStatus = "$($matches[1]) running, $($matches[2]) queued, $($matches[3]) completed"
        }

        # Test runner start time
        if ($line -match '^(\d{4}-\d{2}-\d{2}T[\d:.]+Z).*RunTests\.dll') {
            $result.testRunnerStartTime = $matches[1]
        }

        # Cancellation
        if ($line -match '^(\d{4}-\d{2}-\d{2}T[\d:.]+Z).*The operation was canceled') {
            $result.cancelTime = $matches[1]
        }
    }

    return $result
}

function Parse-ExceptionActivityXml {
    <#
    .SYNOPSIS
        Parses an integration test Activity.xml exception log for error details.
    #>
    param([string]$FilePath)

    $result = @{
        fileName = [System.IO.Path]::GetFileName($FilePath)
        testName = $null
        exceptionType = $null
        vsVersion = $null
        totalErrors = 0
        errorSources = @{}
        affectedFeatures = @()
        primaryError = $null
        rootCausePattern = $null
    }

    # Parse filename: {timestamp}-{TestClass}.{TestMethod}-{ExceptionType}.Activity.xml
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension([System.IO.Path]::GetFileNameWithoutExtension($FilePath))
    if ($baseName -match '^[\d.]+\-(.+)\-([^-]+)$') {
        $result.testName = $matches[1]
        $result.exceptionType = $matches[2]
    }

    try {
        $content = Get-Content $FilePath -Raw -ErrorAction Stop

        # Extract VS version
        if ($content -match 'Microsoft Visual Studio \d+ version:\s*([\d.]+)') {
            $result.vsVersion = $matches[1]
        }

        # Count error entries
        $errorMatches = [regex]::Matches($content, '<type>Error</type>')
        $result.totalErrors = $errorMatches.Count

        # Extract affected features
        $featureMatches = [regex]::Matches($content, "Feature &apos;([^&]+)&apos;")
        $result.affectedFeatures = @($featureMatches | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)

        # Extract error sources
        $sourceMatches = [regex]::Matches($content, '<type>Error</type>\s*\r?\n\s*<source>([^<]+)</source>')
        foreach ($m in $sourceMatches) {
            $source = $m.Groups[1].Value
            if ($result.errorSources.ContainsKey($source)) {
                $result.errorSources[$source]++
            }
            else {
                $result.errorSources[$source] = 1
            }
        }

        # Detect root cause patterns
        if ($content -match "Could not load file or assembly &apos;([^&]+)&apos;") {
            $result.rootCausePattern = "assembly-load-failure"
            $result.primaryError = "Could not load file or assembly '$($matches[1])'"
        }
        elseif ($content -match 'ServiceActivationFailedException') {
            $result.rootCausePattern = "service-activation-failure"
            if ($content -match "Activating the &quot;([^&]+)&quot; service failed") {
                $result.primaryError = "Service activation failed: $($matches[1])"
            }
        }
        elseif ($content -match 'ObjectDisposedException') {
            $result.rootCausePattern = "object-disposed"
            $result.primaryError = "ObjectDisposedException in VS process"
        }
        elseif ($content -match 'TimeoutException') {
            $result.rootCausePattern = "timeout"
            $result.primaryError = "Operation timed out during test execution"
        }
    }
    catch {
        Write-WarningLine "Failed to parse $FilePath : $($_.Exception.Message)"
    }

    return $result
}

function Parse-MefErrors {
    <#
    .SYNOPSIS
        Parses MEF error text files for composition failures.
    #>
    param([string]$FilePath)

    $result = @{
        fileName = [System.IO.Path]::GetFileName($FilePath)
        errorCount = 0
        errors = @()
    }

    try {
        $content = Get-Content $FilePath -Raw -ErrorAction Stop
        $lines = $content -split "`n" | Where-Object { $_.Trim() -ne '' }
        $result.errorCount = $lines.Count

        # Extract first few error summaries
        $result.errors = @($lines | Select-Object -First 10 | ForEach-Object { $_.Trim().Substring(0, [Math]::Min(200, $_.Trim().Length)) })
    }
    catch {
        Write-WarningLine "Failed to parse MEF errors from $FilePath : $($_.Exception.Message)"
    }

    return $result
}

function Parse-VsixInstallerLog {
    <#
    .SYNOPSIS
        Parses VSIX installer log for installation errors.
    #>
    param([string]$FilePath)

    $result = @{
        fileName = [System.IO.Path]::GetFileName($FilePath)
        success = $true
        errors = @()
        installedExtensions = @()
    }

    try {
        $content = Get-Content $FilePath -ErrorAction Stop

        foreach ($line in $content) {
            if ($line -match 'Install Error') {
                $result.success = $false
                $result.errors += $line.Trim().Substring(0, [Math]::Min(300, $line.Trim().Length))
            }
            if ($line -match 'Successfully installed') {
                $result.installedExtensions += $line.Trim()
            }
            if ($line -match 'failed with exit code') {
                $result.success = $false
                $result.errors += $line.Trim()
            }
        }
    }
    catch {
        Write-WarningLine "Failed to parse VSIX log $FilePath : $($_.Exception.Message)"
    }

    return $result
}

#endregion

#region Artifact Download & Analysis

function Invoke-ArtifactAnalysis {
    <#
    .SYNOPSIS
        Downloads and analyzes published test artifacts for a given build.
    #>
    param(
        [int]$BuildId,
        [array]$Artifacts,
        [array]$FailedJobConfigs,
        [int]$MaxSizeMB
    )

    $analysisResult = @{
        downloaded = $false
        exceptionFiles = @()
        screenshotFiles = @()
        mefErrorFiles = @()
        vsixLogFiles = @()
        serviceHubLogFiles = @()
        parsedExceptions = @()
        parsedMefErrors = @()
        parsedVsixLogs = @()
        skippedArtifacts = @()
    }

    # Find log artifacts matching failing configurations
    $logArtifacts = @()
    foreach ($artifact in $Artifacts) {
        $name = $artifact.name
        if ($name -notlike '*Logs*' -or $name -like '*Build*') {
            continue
        }

        # Check if this artifact matches a failing configuration
        $isRelevant = $false
        foreach ($config in $FailedJobConfigs) {
            if ($name -match $config) {
                $isRelevant = $true
                break
            }
        }
        # If no specific configs, include all log artifacts
        if ($FailedJobConfigs.Count -eq 0) {
            $isRelevant = $true
        }

        if ($isRelevant) {
            $sizeMB = [math]::Round([long]$artifact.resource.properties.artifactsize / 1MB, 1)
            if ($sizeMB -gt $MaxSizeMB) {
                Write-WarningLine "Artifact '$name' is ${sizeMB}MB (limit: ${MaxSizeMB}MB) — skipping download"
                $analysisResult.skippedArtifacts += @{
                    name  = $name
                    sizeMB = $sizeMB
                    reason = "Exceeds size limit of ${MaxSizeMB}MB"
                }
                continue
            }
            $logArtifacts += @{ artifact = $artifact; sizeMB = $sizeMB }
        }
    }

    if ($logArtifacts.Count -eq 0) {
        Write-WarningLine "No downloadable log artifacts found"
        return $analysisResult
    }

    $tempBase = Join-Path ([System.IO.Path]::GetTempPath()) "roslyn-inttest-$BuildId"
    if (Test-Path $tempBase) { Remove-Item -Recurse -Force $tempBase }
    New-Item -ItemType Directory -Path $tempBase -Force | Out-Null

    foreach ($entry in $logArtifacts) {
        $artifact = $entry.artifact
        $name = $artifact.name
        $sizeMB = $entry.sizeMB
        Write-Host "  Downloading '$name' (${sizeMB}MB)..." -ForegroundColor Gray

        $zipPath = Join-Path $tempBase "$($name -replace '[^\w.-]', '_').zip"
        $extractPath = Join-Path $tempBase ($name -replace '[^\w.-]', '_')

        try {
            Get-AzDOArtifactZip -BuildId $BuildId -ArtifactName $name -OutputPath $zipPath
            Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force
            Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
            $analysisResult.downloaded = $true

            # Scan extracted files
            $allFiles = Get-ChildItem $extractPath -Recurse -File

            # Exception Activity XMLs
            $exceptionXmls = $allFiles | Where-Object { $_.Name -like '*.Activity.xml' }
            foreach ($xml in $exceptionXmls) {
                $analysisResult.exceptionFiles += $xml.Name
                Write-Host "    Found exception log: $($xml.Name)" -ForegroundColor Yellow
                $parsed = Parse-ExceptionActivityXml -FilePath $xml.FullName
                $analysisResult.parsedExceptions += $parsed
            }

            # Screenshots
            $screenshots = $allFiles | Where-Object { $_.Extension -in '.png', '.jpg', '.jpeg', '.bmp' }
            foreach ($ss in $screenshots) {
                $relPath = $ss.FullName.Substring($extractPath.Length + 1)
                $analysisResult.screenshotFiles += $relPath
                $inScreenshotsDir = $relPath -match '[\\/]Screenshots[\\/]'
                if (-not $inScreenshotsDir) {
                    Write-Host "    Found root-level screenshot (likely timeout): $($ss.Name)" -ForegroundColor Yellow
                }
            }

            # MEF errors
            $mefFiles = $allFiles | Where-Object { $_.Name -like 'MEFErrors*' }
            foreach ($mef in $mefFiles) {
                $analysisResult.mefErrorFiles += $mef.Name
                Write-Host "    Found MEF error file: $($mef.Name)" -ForegroundColor Yellow
                $parsed = Parse-MefErrors -FilePath $mef.FullName
                $analysisResult.parsedMefErrors += $parsed
            }

            # VSIX installer logs
            $vsixLogs = $allFiles | Where-Object { $_.Name -like 'VSIXInstaller*' }
            foreach ($vl in $vsixLogs) {
                $analysisResult.vsixLogFiles += $vl.Name
                $parsed = Parse-VsixInstallerLog -FilePath $vl.FullName
                $analysisResult.parsedVsixLogs += $parsed
                if (-not $parsed.success) {
                    Write-Host "    Found VSIX installer errors in: $($vl.Name)" -ForegroundColor Red
                }
            }

            # ServiceHub logs
            $shLogs = $allFiles | Where-Object { $_.Name -like 'ServiceHub*' -or $_.Name -like '*servicehub*' }
            foreach ($sh in $shLogs) {
                $analysisResult.serviceHubLogFiles += $sh.Name
            }
        }
        catch {
            Write-WarningLine "Failed to download/extract '$name': $($_.Exception.Message)"
            $analysisResult.skippedArtifacts += @{
                name   = $name
                sizeMB = $sizeMB
                reason = "Download/extract failed: $($_.Exception.Message)"
            }
        }
    }

    # Cleanup
    try { Remove-Item -Recurse -Force $tempBase -ErrorAction SilentlyContinue } catch { }

    return $analysisResult
}

#endregion

#region Main Execution

try {
    $pipelineTimeoutMinutes = 150

    Write-Header "Integration Test Analysis — Build $BuildId"
    $buildUrl = "https://dev.azure.com/$Organization/$Project/_build/results?buildId=$BuildId&view=results"
    Write-Detail "URL" $buildUrl

    # Step 1: Fetch build status
    Write-Header "Build Status"
    $build = Get-AzDOBuild -BuildId $BuildId
    Write-Detail "Status" "$($build.status) ($($build.result))"
    Write-Detail "Pipeline" $build.definition.name
    Write-Detail "Source Branch" $build.sourceBranch
    Write-Detail "Start Time" $build.startTime
    Write-Detail "Finish Time" $build.finishTime

    if ($build.triggerInfo -and $build.triggerInfo.'pr.number') {
        Write-Detail "PR" "#$($build.triggerInfo.'pr.number')"
    }

    # Step 2: Fetch timeline and analyze jobs
    Write-Header "Job Timeline"
    $timeline = Get-AzDOTimeline -BuildId $BuildId

    $jobs = $timeline.records | Where-Object { $_.type -eq 'Job' }
    $integrationJobs = @()
    $buildJobs = @()

    foreach ($job in $jobs) {
        $durationMin = 'N/A'
        if ($job.startTime -and $job.finishTime) {
            $durationMin = [math]::Round(([datetime]$job.finishTime - [datetime]$job.startTime).TotalMinutes, 1)
        }

        $jobInfo = @{
            name        = $job.name
            result      = $job.result
            durationMin = $durationMin
            id          = $job.id
            timedOut    = $false
        }

        if ($job.name -match 'VS_Integration') {
            # Check if this was a timeout (canceled after running close to the pipeline timeout)
            if ($job.result -eq 'canceled' -and $durationMin -ne 'N/A' -and $durationMin -gt ($pipelineTimeoutMinutes - 10)) {
                $jobInfo.timedOut = $true
            }
            $integrationJobs += $jobInfo
        }
        else {
            $buildJobs += $jobInfo
        }
    }

    # Display build jobs
    foreach ($j in $buildJobs) {
        $color = if ($j.result -eq 'succeeded') { 'Green' } elseif ($j.result -eq 'failed') { 'Red' } else { 'Yellow' }
        Write-Host "  $($j.name): " -NoNewline
        Write-Host "$($j.result)" -ForegroundColor $color -NoNewline
        Write-Host " ($($j.durationMin) min)"
    }

    # Display integration test jobs
    Write-Host ""
    foreach ($j in $integrationJobs) {
        $color = if ($j.result -eq 'succeeded') { 'Green' } elseif ($j.result -eq 'failed') { 'Red' } else { 'Yellow' }
        $suffix = if ($j.timedOut) { ' [TIMED OUT]' } else { '' }
        Write-Host "  $($j.name): " -NoNewline
        Write-Host "$($j.result)${suffix}" -ForegroundColor $color -NoNewline
        Write-Host " ($($j.durationMin) min)"
    }

    # Step 3: Analyze logs for failed/canceled integration jobs
    $failedOrCanceled = $integrationJobs | Where-Object { $_.result -ne 'succeeded' }
    $jobAnalyses = @()

    if ($failedOrCanceled.Count -gt 0) {
        Write-Header "Integration Test Job Logs"

        foreach ($job in $failedOrCanceled) {
            Write-Host "`n  --- $($job.name) ---" -ForegroundColor Cyan

            # Find "Run Integration Tests" step
            $steps = $timeline.records | Where-Object { $_.parentId -eq $job.id }
            $testStep = $steps | Where-Object { $_.name -eq 'Run Integration Tests' }

            $jobAnalysis = @{
                name              = $job.name
                result            = $job.result
                durationMinutes   = $job.durationMin
                timedOut          = $job.timedOut
                configuration     = if ($job.name -match 'Debug') { 'Debug' } else { 'Release' }
                testRunnerStatus  = $null
                vsixInstallSuccess = $null
                vsVersion         = $null
                dotnetSdkVersion  = $null
                lastTestActivity  = $null
            }

            if ($testStep -and $testStep.log) {
                $logContent = Get-AzDOLog -BuildId $BuildId -LogId $testStep.log.id
                if ($logContent) {
                    $parsed = Parse-TestRunnerLog -LogContent $logContent
                    $jobAnalysis.testRunnerStatus = $parsed.testRunnerStatus
                    $jobAnalysis.vsixInstallSuccess = $parsed.vsixInstallSuccess
                    $jobAnalysis.vsVersion = $parsed.vsVersion
                    $jobAnalysis.dotnetSdkVersion = $parsed.dotnetSdkVersion
                    $jobAnalysis.lastTestActivity = $parsed.testRunnerStartTime

                    Write-Detail "VS Version" ($parsed.vsVersion ?? "unknown")
                    Write-Detail ".NET SDK" ($parsed.dotnetSdkVersion ?? "unknown")
                    Write-Detail "VSIX Install" $(if ($parsed.vsixInstallSuccess) { "OK" } else { "FAILED" })
                    if ($parsed.vsixInstallErrors.Count -gt 0) {
                        foreach ($err in $parsed.vsixInstallErrors) {
                            Write-ErrorLine $err
                        }
                    }
                    Write-Detail "Test Runner Status" ($parsed.testRunnerStatus ?? "no status line found")

                    if ($parsed.testRunnerStatus -and $parsed.testRunnerStatus -match '(\d+) running.*0 completed' -and $job.timedOut) {
                        Write-ErrorLine "Tests HUNG: first test assembly never completed (ran for $($job.durationMin) min)"
                    }

                    if ($parsed.cancelTime) {
                        Write-Detail "Canceled At" $parsed.cancelTime
                    }
                }
            }
            else {
                Write-WarningLine "Could not find 'Run Integration Tests' step log"

                # Check for other step errors
                $failedSteps = $steps | Where-Object { $_.result -eq 'failed' }
                foreach ($fs in $failedSteps) {
                    Write-ErrorLine "Failed step: $($fs.name)"
                    if ($fs.issues) {
                        foreach ($issue in $fs.issues) {
                            Write-ErrorLine "  $($issue.message)"
                        }
                    }
                }
            }

            $jobAnalyses += $jobAnalysis
        }
    }

    # Step 4: Artifact analysis
    $artifactAnalysis = @{
        downloaded        = $false
        exceptionFiles    = @()
        screenshotFiles   = @()
        mefErrorFiles     = @()
        vsixLogFiles      = @()
        parsedExceptions  = @()
        parsedMefErrors   = @()
        parsedVsixLogs    = @()
        skippedArtifacts  = @()
    }

    $artifacts = Get-AzDOArtifacts -BuildId $BuildId

    if ($DownloadArtifacts -and $failedOrCanceled.Count -gt 0) {
        Write-Header "Published Artifact Analysis"

        # Determine which configurations failed
        $failedConfigs = @()
        foreach ($job in $failedOrCanceled) {
            if ($job.name -match 'Debug') { $failedConfigs += 'Debug' }
            if ($job.name -match 'Release') { $failedConfigs += 'Release' }
        }
        $failedConfigs = $failedConfigs | Sort-Object -Unique

        Write-Host "  Looking for artifacts matching: $($failedConfigs -join ', ')"

        $artifactAnalysis = Invoke-ArtifactAnalysis `
            -BuildId $BuildId `
            -Artifacts $artifacts `
            -FailedJobConfigs $failedConfigs `
            -MaxSizeMB $MaxArtifactSizeMB
    }
    elseif (-not $DownloadArtifacts -and $failedOrCanceled.Count -gt 0) {
        Write-Header "Published Artifacts (not downloaded — use -DownloadArtifacts to analyze)"

        $logArtifacts = $artifacts | Where-Object { $_.name -like '*Logs*' -and $_.name -notlike '*Build*' }
        foreach ($a in $logArtifacts) {
            $sizeMB = [math]::Round([long]$a.resource.properties.artifactsize / 1MB, 1)
            Write-Host "  $($a.name) — ${sizeMB}MB"
        }
    }

    # Step 5: Emit parsed artifact details
    if ($artifactAnalysis.parsedExceptions.Count -gt 0) {
        Write-Header "Exception Details"
        foreach ($exc in $artifactAnalysis.parsedExceptions) {
            Write-Host "  File: $($exc.fileName)" -ForegroundColor Yellow
            if ($exc.testName) { Write-Detail "Test" $exc.testName }
            if ($exc.exceptionType) { Write-Detail "Exception Type" $exc.exceptionType }
            if ($exc.vsVersion) { Write-Detail "VS Version" $exc.vsVersion }
            Write-Detail "Error Count" $exc.totalErrors
            if ($exc.primaryError) { Write-Detail "Primary Error" $exc.primaryError }
            if ($exc.affectedFeatures.Count -gt 0) {
                Write-Detail "Affected Features" ($exc.affectedFeatures -join ', ')
            }
            if ($exc.errorSources.Count -gt 0) {
                Write-Host "    Error Sources:"
                foreach ($kvp in $exc.errorSources.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 5) {
                    Write-Host "      $($kvp.Key): $($kvp.Value) errors"
                }
            }
            Write-Host ""
        }
    }

    # Step 6: Emit structured summary
    $summary = [ordered]@{
        buildId          = $BuildId
        buildUrl         = $buildUrl
        buildResult      = $build.result
        pipeline         = $build.definition.name
        sourceBranch     = $build.sourceBranch
        pipelineTimeout  = $pipelineTimeoutMinutes
        jobs             = @($jobAnalyses | ForEach-Object {
                [ordered]@{
                    name              = $_.name
                    result            = $_.result
                    durationMinutes   = $_.durationMinutes
                    timedOut          = $_.timedOut
                    configuration     = $_.configuration
                    testRunnerStatus  = $_.testRunnerStatus
                    vsixInstallSuccess = $_.vsixInstallSuccess
                    vsVersion         = $_.vsVersion
                    dotnetSdkVersion  = $_.dotnetSdkVersion
                    lastTestActivity  = $_.lastTestActivity
                }
            })
        artifacts = [ordered]@{
            downloaded       = $artifactAnalysis.downloaded
            exceptionFiles   = $artifactAnalysis.exceptionFiles
            screenshotFiles  = $artifactAnalysis.screenshotFiles
            mefErrorFiles    = $artifactAnalysis.mefErrorFiles
            vsixLogFiles     = $artifactAnalysis.vsixLogFiles
            skippedArtifacts = $artifactAnalysis.skippedArtifacts
            parsedExceptions = @($artifactAnalysis.parsedExceptions | ForEach-Object {
                    [ordered]@{
                        fileName         = $_.fileName
                        testName         = $_.testName
                        exceptionType    = $_.exceptionType
                        rootCausePattern = $_.rootCausePattern
                        primaryError     = $_.primaryError
                        totalErrors      = $_.totalErrors
                        affectedFeatures = $_.affectedFeatures
                        errorSources     = $_.errorSources
                        vsVersion        = $_.vsVersion
                    }
                })
            parsedMefErrors  = @($artifactAnalysis.parsedMefErrors | ForEach-Object {
                    [ordered]@{
                        fileName   = $_.fileName
                        errorCount = $_.errorCount
                        errors     = $_.errors
                    }
                })
            parsedVsixLogs   = @($artifactAnalysis.parsedVsixLogs | ForEach-Object {
                    [ordered]@{
                        fileName = $_.fileName
                        success  = $_.success
                        errors   = $_.errors
                    }
                })
        }
    }

    Write-Host ""
    Write-Host "[INTEGRATION_TEST_SUMMARY]"
    Write-Host ($summary | ConvertTo-Json -Depth 5)
    Write-Host "[/INTEGRATION_TEST_SUMMARY]"
}
catch {
    Write-Host "FATAL ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
    exit 1
}

#endregion
