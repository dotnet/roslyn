# This script scrapes AzDo data and writes out a CSV of build runtimes
# after running this script you can check 'ci-times.csv' and paste into 'all-ci-times.xlsx' to graph the data

$roslynPipelineId = "15"
# number of recent CI runs to scrape data from.
# adjust as needed to get a varying picture of the data.
$runCount = 200

$baseURL = "https://dev.azure.com/dnceng/public/_apis/"
$runsURL = "$baseURL/pipelines/$roslynPipelineId/runs?api-version=6.0-preview.1"
$buildsURL = "$baseURL/build/builds/"

$wantedRecords = @(
    "Build_Windows_Debug",
    "Build_Windows_Release",
    "Build_Unix_Debug",

    "Correctness_Determinism",
    "Correctness_Build",
    "Correctness_SourceBuild",

    "Test_Windows_Desktop_Debug_32",
    "Test_Windows_Desktop_Spanish_Debug_32",
    "Test_Windows_Desktop_Debug_64",
    "Test_Windows_CoreClr_Debug",
    "Test_Windows_Desktop_Release_32",
    "Test_Windows_Desktop_Release_64",
    "Test_Windows_CoreClr_Release",
    "Test_Linux_Debug",
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
    Correctness_SourceBuild = 7;

    Test_Windows_Desktop_Debug_32 = 2;
    Test_Windows_Desktop_Spanish_Debug_32 = 2;
    Test_Windows_Desktop_Debug_64 = 2;
    Test_Windows_CoreClr_Debug = 2;
    Test_Windows_Desktop_Release_32 = 4;
    Test_Windows_Desktop_Release_64 = 4;
    Test_Windows_CoreClr_Release = 4;
    Test_Linux_Debug = 6;
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


function initialPass() {
    $runs = (Invoke-WebRequest -uri $runsURL | ConvertFrom-Json)
    $allJobs = [System.Collections.Generic.List[Job]]::new()
    foreach ($run in $runs.value[0..$runCount]) {
        if ($run.result -eq "succeeded") {
            $runDetails = (Invoke-WebRequest -uri $run._links.self.href | ConvertFrom-Json)
            $refName = $runDetails.resources.repositories.self.refName

            # uncomment the desired condition to filter the builds we measure
            if (
                # distrust all PR/feature/release branch builds and only get master CI builds
                # $refName -ne "refs/heads/master"

                # ignore specific PRs which change CI config and thus don't have valid data points
                $refName -eq "refs/pulls/50046/merge" -or $refName -eq "refs/pulls/49626/merge"

                # specifically gather data on experimental azure vmImage builds
                # $refName -ne "refs/pulls/50046/merge"
            ) {
                continue
            }
            $createdJob = [Job]::new()
            $createdJob.runId = $run.id
            $createdJob.attempt = 0
            $createdJob.name = "0_Run_Queued"
            $createdJob.relativeStart = [TimeSpan]0
            $createdJob.duration = [DateTime]$run.finishedDate - [DateTime]$run.createdDate
            $createdJob.startDelay = [TimeSpan]0
            $allJobs.Add($createdJob) | Out-Null

            $runStartTime = [DateTime]$run.createdDate

            $timeline = (Invoke-WebRequest -uri "$buildsURL/$($run.id)/timeline" | ConvertFrom-Json)
            foreach ($record in $timeline.records) {
                # not sure yet how to deal with multiple attempts. but data from succeeded in jobs in attempts that ultimately fail is probably still helpful.
                if ($record.attempt -eq 1 -and $record.type -eq "job" -and $record.result -eq "succeeded" -and $wantedRecords.Contains($record.name)) {
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
    }
    return $allJobs
}

# there might be rest API that makes it unnecessary to reverse engineer this stuff
# but for now this is more convenient.
$prerequisites = @{
    "1_Build_Windows_Debug"                   = $null;
    "3_Build_Windows_Release"                 = $null;
    "5_Build_Unix_Debug"                      = $null;

    "7_Correctness_Determinism"               = $null;
    "7_Correctness_Build"                     = $null;
    "7_Correctness_SourceBuild"               = $null;

    "2_Test_Windows_Desktop_Debug_32"         = "1_Build_Windows_Debug";
    "2_Test_Windows_Desktop_Spanish_Debug_32" = "1_Build_Windows_Debug";
    "2_Test_Windows_Desktop_Debug_64"         = "1_Build_Windows_Debug";
    "2_Test_Windows_CoreClr_Debug"            = "1_Build_Windows_Debug";
    "4_Test_Windows_Desktop_Release_32"       = "3_Build_Windows_Release";
    "4_Test_Windows_Desktop_Release_64"       = "3_Build_Windows_Release";
    "4_Test_Windows_CoreClr_Release"          = "3_Build_Windows_Release";
    "6_Test_Linux_Debug"                      = "5_Build_Unix_Debug";
    "6_Test_macOS_Debug"                      = "5_Build_Unix_Debug";
}

function findPrereq([Job]$job) {
    $prerequisiteName = $prerequisites[$job.name]
    if (-not $prerequisiteName) {
        return $null
    }
    foreach ($candidate in $allJobs) {
        if ($candidate.name -eq $prerequisiteName -and $candidate.runId -eq $job.runId) {
            return $candidate
            break # if we don't break, remarkably, we end up returning an Object[] of all the things we returned in here. what?
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
        return [TimeSpan]::new(($matchingJobs | Select-Object -ExpandProperty $propName | Measure-Object -Property Ticks -Average).Average)
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

$averageJobs + $allJobs | Sort-Object -Property runId,name | Export-Csv -Path "ci-times.csv"