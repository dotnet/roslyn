set-variable -name LastExitCode 0
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

# If the test runner crashes and doesn't shut down CPC, CPC could fill
# the entire disk with ETL traces.
try {
    taskkill /F /IM CPC.exe 2>&1 | Out-Null
}
catch {}
try {
    taskkill /F /IM msbuild.exe 2>&1 | Out-Null
}
catch {}
try {
    taskkill /F /IM VBCSCompiler.exe 2>&1 | Out-Null
}
catch {}

# Move all etl files to the a folder for archiving
echo "creating ToArchive directory"
mkdir ToArchive
echo "moving C:/CPC/DataBackup* to ToArchive"
mv C:/CPC/DataBackup* ToArchive
ls ToArchive

# Clean CPC and related directories out of the machine
If (Test-Path C:/CPC) {
    try {
        echo "removing C:/CPC ..."
        Remove-Item -Recurse -Force C:/CPC
        echo "done."
    }
    catch { }
}

If (Test-Path C:/CPCTraces) {
    try {
        echo "removing C:/CPCTraces"
        Remove-Item -Recurse -Force C:/CPCTraces
        echo "done."
    }
    catch { }
}

If (Test-Path C:/PerfLogs) {
    try {  
        echo "removing C:/PerfLogs"
        Remove-Item -Recurse -Force C:/PerfLogs
        echo "done."
    }
    catch { }
}

If (Test-Path C:/PerfTemp) {
    try {
        echo "removing C:/PerfTemp"
        Remove-Item -Recurse -Force C:/PerfTemp
        echo "done."
    }
    catch { }
}
exit 0
