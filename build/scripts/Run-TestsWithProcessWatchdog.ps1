<#
.SYNOPSIS
Run the unit tests under the control of the process watchdog.

.DESCRIPTION
This script runs the unit tests under the control of the process watchdog.
The process watchdog allows the unit tests a certain amount of time to
run. If they don't complete in the allotted time, then the watchdog
obtains memory dumps from the test process and all its descendant processes,
takes a screen shot, and kills the processes.

.PARAMETER ProcessWatchdogExe
The path to the executable ProcessWatchdog.exe, which sets time limits on
the unit tests.

.PARAMETER ProcessWatchdogOutputDirectory
The directory into which ProcessWatchdog.exe should write memory dumps and screen shots.

.PARAMETER ProcDumpExe
The path to the SysInternals executable ProcDump.exe, which produces memory dumps.

.PARAMETER CoreRunArgs
The arguments to be passed to CoreRun.exe.

.PARAMETER CoreRunExe
The path to the executable CoreRun.exe, which runs the Core CLR unit tests.

.PARAMETER CoreRunArgs
The arguments to be passed to CoreRun.exe.

.PARAMETER RunTestsExe
The path to the executable RunTests.exe, which runs the desktop unit tests.

.PARAMETER RunTestsArgs
The arguments to be passed to RunTests.exe.

.PARAMETER BuildStartTime
The time the Jenkins build started, in the ISO 8601-compatible format YYYY-MM-DDThh:mm:ss.
Ignored if $RunProcessWatchdog is not $true.

.PARAMETER BuildTimeLimit
The total time allowed for the Jenkins build, measured in minutes.
Ignored if $RunProcessWatchdog is not $true.

.PARAMETER BufferTime
The time reserved for the process watchdog to kill all test processes and obtain crash
dumps from them, measure in seconds. 
Ignored if $RunProcessWatchdog is not $true.
#>

[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)] [string] $ProcessWatchdogExe,
    [Parameter(Mandatory=$true)] [string] $ProcessWatchdogOutputDirectory,
    [Parameter(Mandatory=$true)] [string] $ProcDumpExe,
    [Parameter(Mandatory=$true)] [string] $CoreRunExe,
    [Parameter(Mandatory=$true)] [string] $CoreRunArgs,
    [Parameter(Mandatory=$true)] [string] $RunTestsExe,
    [Parameter(Mandatory=$true)] [string] $RunTestsArgs,
    [Parameter(Mandatory=$true)] [string] $BuildStartTime,
    [Parameter(Mandatory=$true)] [int] $BuildTimeLimit,
    [Parameter(Mandatory=$true)] [int] $BufferTime
)

function Check-TimeRemaining($testGroupName)
{
    $timeRemaining = Get-TimeRemaining
    if ($timeRemaining -gt 0) {
        Write-Host "$timeRemaining seconds remain to run the $testGroupName unit tests."
        $timeRemaining
    } else {
        Write-Host "There is no time remaining to run the $testGroupName unit tests."
        exit 1;
    }
}

function Get-TimeRemaining() {
    
    $secondsSinceStart = ([DateTime]::Now - [DateTime]::Parse($BuildStartTime)).TotalSeconds
    [Math]::Truncate($BuildTimeLimit * 60 - $secondsSinceStart - $BufferTime)
}

$timeRemaining = Check-TimeRemaining "Core CLR"

& $ProcessWatchdogExe --executable $CoreRunExe --arguments "$CoreRunArgs" --time-limit $timeRemaining --output-folder "$ProcessWatchdogOutputDirectory" --screenshot --procdump-path "$ProcDumpExe"

$timeRemaining = Check-TimeRemaining "desktop"

& $ProcessWatchdogExe --executable $RunTestsExe --arguments "$RunTestsArgs" --time-limit $timeRemaining --output-folder "$ProcessWatchdogOutputDirectory" --screenshot --procdump-path "$ProcDumpExe"