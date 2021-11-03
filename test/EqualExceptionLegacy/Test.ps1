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

function Get-Outcome([xml]$testResults, [string]$testName) {
    $ns = @{"n" = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    $result = ($trx | Select-Xml -Namespace $ns "/n:TestRun/n:Results/n:UnitTestResult[@testName='$testName']")
    Return $result.Node.outcome
}

function Verify-Outcome([xml]$testResults, [string]$testName, [string]$expected) {
    $outcome = Get-Outcome $testResults $testName
    if ($outcome -ne $expected) {
        Write-Host "Verifying outcome for test '$testName'"
        Write-Host "Expected '$expected', found '$outcome'"
        Return 1
    }

    Return 0
}

function Verify-Passed([xml]$testResults, [string]$testName) {
    Return Verify-Outcome $testResults $testName "Passed"
}

function Verify-Failed([xml]$testResults, [string]$testName) {
    Return Verify-Outcome $testResults $testName "Failed"
}

$errorCount = 0
[xml]$trx = Get-Content $trxPath
if ($trx.TestRun.ResultSummary.outcome -ne "Failed") {
    Write-Host "Testing /TestRun/ResultSummary/@outcome"
    Write-Host "Expected 'Failed', found '$($trx.TestRun.ResultSummary.outcome)'"
    $errorCount++
}

if ($trx.TestRun.ResultSummary.Counters.executed -ne "9") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@executed"
    Write-Host "Expected '9', found '$($trx.TestRun.ResultSummary.Counters.executed)'"
    $errorCount++
}

if ($trx.TestRun.ResultSummary.Counters.total -ne "45") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@total"
    Write-Host "Expected '45', found '$($trx.TestRun.ResultSummary.Counters.total)'"
    $errorCount++
}

if ($trx.TestRun.ResultSummary.Counters.passed -ne "2") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@passed"
    Write-Host "Expected '2', found '$($trx.TestRun.ResultSummary.Counters.passed)'"
    $errorCount++
}

if ($trx.TestRun.ResultSummary.Counters.failed -ne "7") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@failed"
    Write-Host "Expected '7', found '$($trx.TestRun.ResultSummary.Counters.failed)'"
    $errorCount++
}

if ($trx.TestRun.ResultSummary.Counters.notExecuted -ne "0") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@notExecuted"
    Write-Host "Expected '0', found '$($trx.TestRun.ResultSummary.Counters.notExecuted)'"
    $errorCount++
}

# Verify the first success
$errorCount += Verify-Passed $trx "EqualExceptionLegacy.EqualException.EqualsSucceeds (VS2019)"

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsSucceeds-*.log" | sort LastWriteTime | select -last 1
if ($logPath) {
    Write-Host "Verifying no diagnostic logs for passing test"
    Write-Host "Found unexpected file '$logPath'"
    $errorCount++
}

# Verify the second success
$errorCount += Verify-Passed $trx "EqualExceptionLegacy.EqualException.EqualsSucceedsAsync (VS2019)"

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsSucceedsAsync-*.log" | sort LastWriteTime | select -last 1
if ($logPath) {
    Write-Host "Verifying no diagnostic logs for passing test"
    Write-Host "Found unexpected file '$logPath'"
    $errorCount++
}

# Verify the first failure (xunit exception)
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualException.EqualsFailure (VS2019)"

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsFailure-EqualException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualException.EqualsFailure-EqualException.log'"
    $errorCount++
}

$pngPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsFailure-EqualException.png" | sort LastWriteTime | select -last 1
if (-not $pngPath) {
    Write-Host "Verifying diagnostic screenshot for failing test"
    Write-Host "Missing image file 'EqualException.EqualsFailure-EqualException.png'"
    $errorCount++
}

$activityPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsFailure-EqualException.Activity.xml" | sort LastWriteTime | select -last 1
if (-not $pngPath) {
    Write-Host "Verifying diagnostic in-memory activity log for failing test"
    Write-Host "Missing image file 'EqualException.EqualsFailure-EqualException.Activity.xml'"
    $errorCount++
}

# Verify the first failure (non-xunit exception)
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualException.EqualsFailureNonXunit (VS2019)"

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsFailureNonXunit-TargetInvocationException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualException.EqualsFailureNonXunit-TargetInvocationException.log'"
    $errorCount++
}

$pngPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsFailureNonXunit-TargetInvocationException.png" | sort LastWriteTime | select -last 1
if (-not $pngPath) {
    Write-Host "Verifying diagnostic screenshot for failing test"
    Write-Host "Missing image file 'EqualException.EqualsFailureNonXunit-TargetInvocationException.png'"
    $errorCount++
}

# Verify the second failure (xunit exception)
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualException.EqualsFailureAsync (VS2019)"

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsFailureAsync-EqualException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualException.EqualsFailureAsync-EqualException.log'"
    $errorCount++
}

$pngPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsFailureAsync-EqualException.png" | sort LastWriteTime | select -last 1
if (-not $pngPath) {
    Write-Host "Verifying diagnostic screenshot for failing test"
    Write-Host "Missing image file 'EqualException.EqualsFailureAsync-EqualException.png'"
    $errorCount++
}

# Verify the second failure (non-xunit exception)
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualException.EqualsFailureNonXunitAsync (VS2019)"

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsFailureNonXunitAsync-InvalidOperationException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualException.EqualsFailureNonXunitAsync-InvalidOperationException.log'"
    $errorCount++
}

$pngPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualException.EqualsFailureNonXunitAsync-InvalidOperationException.png" | sort LastWriteTime | select -last 1
if (-not $pngPath) {
    Write-Host "Verifying diagnostic screenshot for failing test"
    Write-Host "Missing image file 'EqualException.EqualsFailureNonXunitAsync-InvalidOperationException.png'"
    $errorCount++
}

# Verify failure in constructor
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualExceptionInConstructor.EqualsSucceeds (VS2019)"

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualExceptionInConstructor.EqualsSucceeds-TargetInvocationException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualExceptionInConstructor.EqualsSucceeds-TargetInvocationException.log'"
    $errorCount++
}

$pngPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualExceptionInConstructor.EqualsSucceeds-TargetInvocationException.png" | sort LastWriteTime | select -last 1
if (-not $pngPath) {
    Write-Host "Verifying diagnostic screenshot for failing test"
    Write-Host "Missing image file 'EqualExceptionInConstructor.EqualsSucceeds-TargetInvocationException.png'"
    $errorCount++
}

# Verify failure in before test attribute
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualExceptionInBeforeAfterTest.FailBeforeTest (VS2019)"

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualExceptionInBeforeAfterTest.FailBeforeTest-InvalidOperationException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualExceptionInBeforeAfterTest.FailBeforeTest-InvalidOperationException.log'"
    $errorCount++
}

$pngPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualExceptionInBeforeAfterTest.FailBeforeTest-InvalidOperationException.png" | sort LastWriteTime | select -last 1
if (-not $pngPath) {
    Write-Host "Verifying diagnostic screenshot for failing test"
    Write-Host "Missing image file 'EqualExceptionInBeforeAfterTest.FailBeforeTest-InvalidOperationException.png'"
    $errorCount++
}

# Verify failure in after test attribute
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualExceptionInBeforeAfterTest.FailAfterTest (VS2019)"

$logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualExceptionInBeforeAfterTest.FailAfterTest-InvalidOperationException.log" | sort LastWriteTime | select -last 1
if (-not $logPath) {
    Write-Host "Verifying diagnostic logs for failing test"
    Write-Host "Missing log file 'EqualExceptionInBeforeAfterTest.FailAfterTest-InvalidOperationException.log'"
    $errorCount++
}

$pngPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-EqualExceptionInBeforeAfterTest.FailAfterTest-InvalidOperationException.png" | sort LastWriteTime | select -last 1
if (-not $pngPath) {
    Write-Host "Verifying diagnostic screenshot for failing test"
    Write-Host "Missing image file 'EqualExceptionInBeforeAfterTest.FailAfterTest-InvalidOperationException.png'"
    $errorCount++
}

if ($errorCount -ne 0) {
    Write-Host "Integration test failed ($errorCount total failures)"
} else {
    Write-Host "Integration test passed (results as expected)."
}

Exit $errorCount
