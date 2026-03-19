# This script scrapes AzDo data and writes out a CSV of build runtimes
# after running this script you can open 'roslyn/artifacts/ci-times.csv' and paste into 'roslyn/scripts/all-ci-times.xlsx' to graph the data

. (Join-Path $PSScriptRoot ".." "eng" "build-utils.ps1")

$roslynPipelineId = "15"
$minDate = [DateTime]"2022-07-20"
$maxDate = [DateTime]"2022-08-03"

$baseURL = "https://dev.azure.com/dnceng/public/_apis/"
$runsURL = "$baseURL/pipelines/$roslynPipelineId/runs?api-version=6.0-preview.1"
$buildsURL = "$baseURL/build/builds/"

$wantedRecords = @(
    "Build_Windows_Debug",
    "Build_Windows_Release",
    "Build_Unix_Debug",

    "Correctness_Determinism",
    "Correctness_Build",
    "Correctness_TodoCheck",
    "Correctness_Rebuild",
    "Correctness_Analyzers",
    "Correctness_Bootstrap_Build",
    "Correctness_Code_Analysis",
    "Correctness_Build_Artifacts",

    "Test_Windows_Desktop_Debug_32",
    "Test_Windows_Desktop_Debug_64",
    "Test_Windows_CoreClr_Debug",
    "Test_Windows_CoreClr_Debug_Single_Machine",
    "Test_Windows_CoreClr_IOperation_Debug",
    "Test_Windows_CoreClr_UsedAssemblies_Debug",

    "Test_Windows_Desktop_Release_32",
    "Test_Windows_Desktop_Spanish_Release_64",
    "Test_Windows_Desktop_Release_64",
    "Test_Windows_CoreClr_Release",

    "Test_Linux_Debug",
    "Test_Linux_Debug_Single_Machine",
    "Test_macOS_Debug"
)

# this determines how the jobs will sort.
# this helps us put artifact producers next to the consumers in the chart
$priorities = @{
    Build_Windows_Debug = 1;
    Build_Windows_Release = 3;
    Build_Unix_Debug = 5;

    Correctness_Determinism = 7;
    Correctness_Build = 7;
    Correctness_TodoCheck = 7;
    Correctness_Rebuild = 7;
    Correctness_Analyzers = 7;
    Correctness_Bootstrap_Build = 7;
    Correctness_Code_Analysis = 7;
    Correctness_Build_Artifacts = 7;

    Test_Windows_Desktop_Debug_32 = 2;
    Test_Windows_Desktop_Debug_64 = 2;
    Test_Windows_CoreClr_Debug = 2;
    Test_Windows_CoreClr_Debug_Single_Machine = 2;
    Test_Windows_CoreClr_IOperation_Debug = 2;
    Test_Windows_CoreClr_UsedAssemblies_Debug = 2;

    Test_Windows_Desktop_Release_32 = 4;
    Test_Windows_Desktop_Spanish_Release_64 = 4;
    Test_Windows_Desktop_Release_64 = 4;
    Test_Windows_CoreClr_Release = 4;

    Test_Linux_Debug = 6;
    Test_Linux_Debug_Single_Machine = 6;
    Test_macOS_Debug = 6
}

class Job {
    [int] $runId
    [int] $attempt
    [string] $name
    [TimeSpan] $relativeStart
    [Nullable[TimeSpan]] $prereqFinish
    [Nullable[TimeSpan]] $startDelay
    [TimeSpan] $duration
}

function Test-Any() {
    begin {
        $any = $false
    }
    process {
        $any = $true
    }
    end {
        $any
    }
}

function requestWithRetry($uri) {
    $i = 0
    while ($i -lt 5) {
        try {
            return Invoke-RestMethod -uri $uri
            break
        } catch {
            Write-Error "Error in request to $uri"
            Write-Error "HTTP $($_.Exception.Response.StatusCode.value__): $($_.Exception.Response.StatusDescription)"
            Write-Error "Sleeping for 5 seconds before retrying..."
            Start-Sleep -Seconds 5.0
        }
        $i++
    }
}

function initialPass() {
    $runs = requestWithRetry $runsURL
    $allJobs = [System.Collections.Generic.List[Job]]::new()

    foreach ($run in $runs.value) {
        if ($run.createdDate -lt $minDate) {
            continue
        }

        if ($run.createdDate -gt $maxDate) {
            continue
        }

        if ($run.state -ne "completed") {
            continue
        }

        if ($run.result -ne "succeeded") {
            continue
        }

        $runDetails = requestWithRetry $run._links.self.href
        $refName = $runDetails.resources.repositories.self.refName

        # uncomment the desired condition to filter the builds we measure
        if (
            # use builds from any branch
            $false

            # distrust all PR/feature/release branch builds and only get main CI builds
            # $refName -ne "refs/heads/main"

            # ignore specific PRs which modify infra and thus don't measure the "production" behavior
            # $refName -eq "refs/pulls/50046/merge" -or $refName -eq "refs/pulls/49626/merge"

            # look for specific PRs which modify infra and thus don't measure the "production" behavior
            # $refName -ne "refs/pull/62797/merge"

            # specifically gather data on experimental builds
            # $refName -ne "refs/heads/dev/rigibson/no-windows-vmImage"
        ) {
            continue
        }

        $timeline = requestWithRetry "$buildsURL/$($run.id)/timeline"
        if ($timeline.records | Where-Object { $_.attempt -gt 1 } | Test-Any) {
            # not yet sure how to properly handle jobs with multiple attempts so will just skip them for now.
            continue
        }

        Write-Host "Measuring run $($run.id) created at $($run.createdDate) - $($run._links.web.href)"

        $createdJob = [Job]::new()
        $createdJob.runId = $run.id
        $createdJob.attempt = 0
        $createdJob.name = "0_Run_Queued"
        $createdJob.relativeStart = [TimeSpan]0
        $createdJob.duration = [DateTime]$run.finishedDate - [DateTime]$run.createdDate
        $createdJob.startDelay = [TimeSpan]0
        $allJobs.Add($createdJob) | Out-Null

        $runStartTime = [DateTime]$run.createdDate

        foreach ($record in $timeline.records) {
            if ($record.type -eq "job" -and $record.result -eq "succeeded" -and $wantedRecords.Contains($record.name)) {
                $job = [Job]::new()
                $job.runId = $run.id;
                $job.attempt = $record.attempt;
                $job.name = "$($priorities[$record.name])_$($record.name)";
                $job.relativeStart = [DateTime]$record.startTime - $runStartTime;
                $job.duration = [DateTime]$record.finishTime - [DateTime]$record.startTime;
                $allJobs.Add($job) | Out-Null
            }
        }
    }
    return $allJobs
}

# there might be rest API that makes it unnecessary to reverse engineer this stuff
# but for now this is more convenient.
$prerequisites = @{
    "1_Build_Windows_Debug"                       = $null;
    "3_Build_Windows_Release"                     = $null;
    "5_Build_Unix_Debug"                          = $null;

    "7_Correctness_Determinism"                   = $null;
    "7_Correctness_Build"                         = $null;
    "7_Correctness_TodoCheck"                     = $null;
    "7_Correctness_Rebuild"                       = $null;
    "7_Correctness_Analyzers"                     = $null;
    "7_Correctness_Bootstrap_Build"               = $null;
    "7_Correctness_Code_Analysis"                 = $null;
    "7_Correctness_Build_Artifacts"               = $null;

    "2_Test_Windows_Desktop_Debug_32"             = "1_Build_Windows_Debug";
    "2_Test_Windows_Desktop_Debug_64"             = "1_Build_Windows_Debug";
    "2_Test_Windows_CoreClr_Debug"                = "1_Build_Windows_Debug";
    "2_Test_Windows_CoreClr_Debug_Single_Machine" = "1_Build_Windows_Debug";
    "2_Test_Windows_CoreClr_IOperation_Debug"     = "1_Build_Windows_Debug";
    "2_Test_Windows_CoreClr_UsedAssemblies_Debug" = "1_Build_Windows_Debug";

    "4_Test_Windows_Desktop_Release_32"           = "3_Build_Windows_Release";
    "4_Test_Windows_Desktop_Spanish_Release_64"   = "3_Build_Windows_Release";
    "4_Test_Windows_Desktop_Release_64"           = "3_Build_Windows_Release";
    "4_Test_Windows_CoreClr_Release"              = "3_Build_Windows_Release";

    "6_Test_Linux_Debug"                          = "5_Build_Unix_Debug";
    "6_Test_Linux_Debug_Single_Machine"           = "5_Build_Unix_Debug";
    "6_Test_macOS_Debug"                          = "5_Build_Unix_Debug";
}

function findPrereq([Job]$job) {
    $prerequisiteName = $prerequisites[$job.name]
    if (-not $prerequisiteName) {
        return $null
    }
    foreach ($candidate in $allJobs) {
        if ($candidate.name -eq $prerequisiteName -and $candidate.runId -eq $job.runId) {
            return $candidate
            break
        }
    }
}

function fillPrereqTimes($allJobs) {
    foreach ($job in $allJobs) {
        [Job]$prereq = findPrereq($job)
        if ($prereq) {
            $job.prereqFinish = $prereq.relativeStart + $prereq.duration
            $job.startDelay = $job.relativeStart - $job.prereqFinish
            if ($job.startDelay -lt [TimeSpan]::Zero) {
                throw "error in $($job.runId) $($job.name): relativeStart $($job.relativeStart) is before prereq $($prereq.name) finish $($job.prereqFinish)"
            }
        }
        else {
            # this is the Run_Created "job"
            # we don't use the duration of Run_Created here to determine the job's start delay because
            # the Run_Created duration represents the entire run time of the job
            $job.prereqFinish = 0
            $job.startDelay = $job.relativeStart
        }
    }
}

function getAverageTimes([string]$jobName) {
    $matchingJobs = $allJobs | Where-Object { $_.name -eq $jobName }
    function getAverageTime($propName) {
        $measurement = $matchingJobs | Select-Object -ExpandProperty $propName | Measure-Object -Property Ticks -Average
        if ($null -ne $measurement) {
            return [TimeSpan]::new($measurement.Average)
        } else {
            return [TimeSpan]::Zero
        }
    }

    $averageJob = [Job]::new()
    $averageJob.runId = 0
    $averageJob.attempt = 0
    $averageJob.name = $jobName
    $averageJob.relativeStart = getAverageTime("relativeStart")
    $averageJob.prereqFinish = getAverageTime("prereqFinish")
    $averageJob.startDelay = getAverageTime("startDelay")
    $averageJob.duration = getAverageTime("duration")
    return $averageJob
}

$allJobs = [System.Collections.Generic.List[Job]](initialPass)
fillPrereqTimes($allJobs)
$averageJobs = [System.Collections.Generic.List[Job]]::new()
foreach ($jobName in $prerequisites.Keys) {
    $averageJobs.Add((getAverageTimes($jobName)))
}
$averageJobs.Add((getAverageTimes("0_Run_Created")))

$outDir = Join-Path $ArtifactsDir "ci-times.csv"
$averageJobs + $allJobs | Sort-Object -Property runId,name | Export-Csv -Path $outDir
Write-Host "Exported CSV to $outDir"
