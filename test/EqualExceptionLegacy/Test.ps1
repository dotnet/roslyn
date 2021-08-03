Param (
    [string]$version = "$env:GITBUILDVERSIONSIMPLE",
    [string]$resultDir = "$env:AGENT_TEMPDIRECTORY",
    [string]$binlog
)

if (-not $version) {
    Write-Host "Missing environment variable 'GITBUILDVERSIONSIMPLE'"
    Exit 1
}

if (-not $resultDir -or -not (Test-Path $resultDir)) {
    if (-not $env:RUNNER_TEMP -or -not (Test-Path $env:RUNNER_TEMP)) {
        Write-Host "Missing environment variable 'AGENT_TEMPDIRECTORY' and/or 'RUNNER_TEMP'"
        Exit 1
    }

    $resultDir = $env:RUNNER_TEMP
}

# Make sure directories referenced by NuGet.Config exist
if (-not (Test-Path "$PSScriptRoot/../../bin/Packages/Debug/NuGet")) {
    mkdir -p "$PSScriptRoot/../../bin/Packages/Debug/NuGet"
}

if (-not (Test-Path "$PSScriptRoot/../../bin/Packages/Release/NuGet")) {
    mkdir -p "$PSScriptRoot/../../bin/Packages/Release/NuGet"
}

$CurrentTestResultsDir = "$resultDir\xUnitResults\EqualExceptionLegacy"
if (-not (Test-Path $CurrentTestResultsDir)) {
    mkdir -p $CurrentTestResultsDir
}

$env:XUNIT_LOGS = $CurrentTestResultsDir
dotnet test /bl:$binlog --logger trx --results-directory $CurrentTestResultsDir $PSScriptRoot\EqualExceptionLegacy.csproj "/p:GITBUILDVERSIONSIMPLE=$version"
if ($LASTEXITCODE -ne 1) {
    Write-Host "Expected 'dotnet test' to exit with code 1, but was $LASTEXITCODE"
    Exit 1
}

$ns = @{"n" = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
$trxPath = Get-ChildItem "$CurrentTestResultsDir\*.trx" | sort LastWriteTime | select -last 1
if (-not (Test-Path $trxPath)) {
    Write-Host "Failed to find test results"
    Exit 1
}

[xml]$trx = Get-Content $trxPath
if ($trx.TestRun.ResultSummary.outcome -ne "Failed") {
    Write-Host "Testing /TestRun/ResultSummary/@outcome"
    Write-Host "Expected 'Failed', found '$($trx.TestRun.ResultSummary.outcome)'"
    Exit 1
}

if ($trx.TestRun.ResultSummary.Counters.executed -ne "7") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@executed"
    Write-Host "Expected '7', found '$($trx.TestRun.ResultSummary.Counters.executed)'"
    Exit 1
}

if ($trx.TestRun.ResultSummary.Counters.total -ne "35") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@total"
    Write-Host "Expected '35', found '$($trx.TestRun.ResultSummary.Counters.total)'"
    Exit 1
}

if ($trx.TestRun.ResultSummary.Counters.passed -ne "2") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@passed"
    Write-Host "Expected '2', found '$($trx.TestRun.ResultSummary.Counters.passed)'"
    Exit 1
}

if ($trx.TestRun.ResultSummary.Counters.failed -ne "5") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@failed"
    Write-Host "Expected '5', found '$($trx.TestRun.ResultSummary.Counters.failed)'"
    Exit 1
}

if ($trx.TestRun.ResultSummary.Counters.notExecuted -ne "0") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@notExecuted"
    Write-Host "Expected '0', found '$($trx.TestRun.ResultSummary.Counters.notExecuted)'"
    Exit 1
}

# Verify the first success
$pass1 = ($trx | Select-Xml -Namespace $ns "/n:TestRun/n:Results/n:UnitTestResult[@testName='EqualExceptionLegacy.EqualException.EqualsSucceeds (VS2019)']")
if ($pass1.Node.outcome -ne "Passed") {
    Write-Host "Testing 'EqualsSucceeds' outcome"
    Write-Host "Expected 'Passed', found '$($pass1.Node.outcome)'"
    Exit 1
}

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsSucceeds-*.log" | sort LastWriteTime | select -last 1
if ($logPath) {
    Write-Host "Verifying no diagnostic logs for passing test"
    Write-Host "Found unexpected file '$logPath'"
    Exit 1
}

# Verify the second success
$pass2 = ($trx | Select-Xml -Namespace $ns "/n:TestRun/n:Results/n:UnitTestResult[@testName='EqualExceptionLegacy.EqualException.EqualsSucceedsAsync (VS2019)']")
if ($pass2.Node.outcome -ne "Passed") {
    Write-Host "Testing 'EqualsSucceedsAsync' outcome"
    Write-Host "Expected 'Passed', found '$($pass2.Node.outcome)'"
    Exit 1
}

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsSucceedsAsync-*.log" | sort LastWriteTime | select -last 1
if ($logPath) {
    Write-Host "Verifying no diagnostic logs for passing test"
    Write-Host "Found unexpected file '$logPath'"
    Exit 1
}

# Verify the first failure
$failure1 = ($trx | Select-Xml -Namespace $ns "/n:TestRun/n:Results/n:UnitTestResult[@testName='EqualExceptionLegacy.EqualException.EqualsFailure (VS2019)']")
if ($failure1.Node.outcome -ne "Failed") {
    Write-Host "Testing 'EqualsFailure' outcome"
    Write-Host "Expected 'Failed', found '$($failure1.Node.outcome)'"
    Exit 1
}

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsFailure-EqualException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualException.EqualsFailure-EqualException.log'"
    Exit 1
}

# Verify the second failure
$failure2 = ($trx | Select-Xml -Namespace $ns "/n:TestRun/n:Results/n:UnitTestResult[@testName='EqualExceptionLegacy.EqualException.EqualsFailureAsync (VS2019)']")
if ($failure2.Node.outcome -ne "Failed") {
    Write-Host "Testing 'EqualsFailureAsync' outcome"
    Write-Host "Expected 'Failed', found '$($failure2.Node.outcome)'"
    Exit 1
}

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsFailureAsync-EqualException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualException.EqualsFailureAsync-EqualException.log'"
    Exit 1
}

# Verify failure in constructor
$failure3 = ($trx | Select-Xml -Namespace $ns "/n:TestRun/n:Results/n:UnitTestResult[@testName='EqualExceptionLegacy.EqualExceptionInConstructor.EqualsSucceeds (VS2019)']")
if ($failure3.Node.outcome -ne "Failed") {
    Write-Host "Testing 'EqualsSucceeds' outcome"
    Write-Host "Expected 'Failed', found '$($failure3.Node.outcome)'"
    Exit 1
}

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualExceptionInConstructor.EqualsSucceeds-TargetInvocationException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualExceptionInConstructor.EqualsSucceeds-TargetInvocationException.log'"
    Exit 1
}

# Verify failure in before test attribute
$failure4 = ($trx | Select-Xml -Namespace $ns "/n:TestRun/n:Results/n:UnitTestResult[@testName='EqualExceptionLegacy.EqualExceptionInBeforeAfterTest.FailBeforeTest (VS2019)']")
if ($failure4.Node.outcome -ne "Failed") {
    Write-Host "Testing 'FailBeforeTest' outcome"
    Write-Host "Expected 'Failed', found '$($failure4.Node.outcome)'"
    Exit 1
}

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualExceptionInBeforeAfterTest.FailBeforeTest-InvalidOperationException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualExceptionInBeforeAfterTest.FailBeforeTest-InvalidOperationException.log'"
    Exit 1
}

# Verify failure in after test attribute
$failure5 = ($trx | Select-Xml -Namespace $ns "/n:TestRun/n:Results/n:UnitTestResult[@testName='EqualExceptionLegacy.EqualExceptionInBeforeAfterTest.FailAfterTest (VS2019)']")
if ($failure5.Node.outcome -ne "Failed") {
    Write-Host "Testing 'FailAfterTest' outcome"
    Write-Host "Expected 'Failed', found '$($failure5.Node.outcome)'"
    Exit 1
}

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualExceptionInBeforeAfterTest.FailAfterTest-InvalidOperationException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualExceptionInBeforeAfterTest.FailAfterTest-InvalidOperationException.log'"
    Exit 1
}

Write-Host "Integration test passed (results as expected)."
Exit 0
