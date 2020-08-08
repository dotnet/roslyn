param (
    [string]$CPCLocation = "C:/CPC",
    [switch]$ShouldArchive = $false
)

set-variable -name LastExitCode 0
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

# If the test runner crashes and doesn't shut down CPC, CPC could fill
# the entire disk with ETL traces.

function KillAndIgnore {
    try {
        taskkill /F /IM $args[0] 2>&1 | Out-Null
    }
    catch {}
}

KillAndIgnore Cpc.exe
KillAndIgnore msbuild.exe
KillAndIgnore csc.exe
KillAndIgnore vbc.exe
KillAndIgnore VBCSCompiler.exe

try {
    & $CPCLocation/cpc.exe /Stop /SkipClean 2>&1 | Out-Null
}
catch {}

if (Test-Path ToArchive) {
    Remove-Item -Recurse -Force ToArchive
}

if ($ShouldArchive) {
    # Move all etl files to the a folder for archiving
    echo "creating ToArchive directory"
    mkdir ToArchive
    if (Test-Path $CPCLocation) {
        try
        {
            echo "moving $CPCLocation/DataBackup* to ToArchive"
            mv $CPCLocation/DataBackup* ToArchive
            echo "moving $CPCLocation/consumptionTempResults.xml to ToArchive"
            mv $CPCLocation/consumptionTempResults.xml ToArchive
        }
        catch
        {
            echo "Copying CPC data failed"
            $ExitCode = 1
        }
    }
    if (Test-Path C:\PerfLogs)
    {
        try
        {    
            mkdir ToArchive/PerfLogs
            xcopy /S C:\PerfLogs ToArchive\PerfLogs
        }
        catch
        {
            echo "Copying PerfLogs failed"
            $ExitCode = 1
        }
    }

    ls ToArchive
}

# Clean CPC and related directories out of the machine
$ExitCode = 0
if (Test-Path $CPCLocation) {
    try {
        echo "removing $CPCLocation ..."
        Remove-Item -Recurse -Force $CPCLocation
        echo "done."
    }
    catch {
        echo "Removing CPC failed, restarting the machine.  THIS SHOULD NOT HAPPEN.  Please email mlinfraswat@microsoft.com"
        shutdown /r /t 5
        $ExitCode = 1
    }
}

if (Test-Path C:/CPCTraces) {
    try {
        echo "removing C:/CPCTraces"
        Remove-Item -Recurse -Force C:/CPCTraces
        echo "done."
    }
    catch {
        $ExitCode = 1
    }
}

if (Test-Path C:/PerfLogs) {
    try {  
        echo "removing C:/PerfLogs"
        Remove-Item -Recurse -Force C:/PerfLogs
        echo "done."
    }
    catch {
        $ExitCode = 1
    }
}

if (Test-Path C:/PerfTemp) {
    try {
        echo "removing C:/PerfTemp"
        Remove-Item -Recurse -Force C:/PerfTemp
        echo "done."
    }
    catch {
        $ExitCode = 1
    }
}

exit $ExitCode
