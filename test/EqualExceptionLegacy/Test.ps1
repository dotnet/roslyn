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
if ((-not $trxPath) -or (-not (Test-Path $trxPath))) {
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

function Verify-Skipped([xml]$testResults, [string]$testName) {
    Return Verify-Outcome $testResults $testName "NotExecuted"
}

function Verify-NoLogs([string]$shortTestName) {
    $logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-$shortTestName-*.log" | sort LastWriteTime | select -last 1
    if ($logPath) {
        Write-Host "Verifying no diagnostic logs for passing test"
        Write-Host "Found unexpected file: $logPath"
        Return 1
    }

    Return 0
}

function Verify-HasLogs([string]$shortTestName, [string]$exceptionName) {
    $currentFailureCount = 0
    $logPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-$shortTestName-$exceptionName.log" | sort LastWriteTime | select -last 1
    if (-not $logPath) {
        Write-Host "Verifying diagnostic logs for failing test"
        Write-Host "Missing log file '$shortTestName-$exceptionName.log'"
        $currentFailureCount++
    }

    $ideLogPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-$shortTestName-$exceptionName.IDE.log" | sort LastWriteTime | select -last 1
    if (-not $ideLogPath) {
        Write-Host "Verifying IDE state logs for failing test"
        Write-Host "Missing log file '$shortTestName-$exceptionName.IDE.log'"
        $currentFailureCount++
    }

    $pngPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-$shortTestName-$exceptionName.png" | sort LastWriteTime | select -last 1
    if (-not $pngPath) {
        Write-Host "Verifying diagnostic screenshot for failing test"
        Write-Host "Missing image file '$shortTestName-$exceptionName.png'"
        $currentFailureCount++
    }

    $activityPath = Get-ChildItem "$CurrentTestResultsDir\Screenshots\??.??.??-$shortTestName-$exceptionName.Activity.xml" | sort LastWriteTime | select -last 1
    if (-not $pngPath) {
        Write-Host "Verifying diagnostic in-memory activity log for failing test"
        Write-Host "Missing activity log file '$shortTestName-$exceptionName.Activity.xml'"
        $currentFailureCount++
    }

    Return $currentFailureCount
}

$errorCount = 0
[xml]$trx = Get-Content $trxPath
if ($trx.TestRun.ResultSummary.outcome -ne "Failed") {
    Write-Host "Testing /TestRun/ResultSummary/@outcome"
    Write-Host "Expected 'Failed', found '$($trx.TestRun.ResultSummary.outcome)'"
    $errorCount++
}

if ($trx.TestRun.ResultSummary.Counters.executed -ne "16") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@executed"
    Write-Host "Expected '16', found '$($trx.TestRun.ResultSummary.Counters.executed)'"
    $errorCount++
}

if ($trx.TestRun.ResultSummary.Counters.total -ne "75") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@total"
    Write-Host "Expected '75', found '$($trx.TestRun.ResultSummary.Counters.total)'"
    $errorCount++
}

if ($trx.TestRun.ResultSummary.Counters.passed -ne "8") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@passed"
    Write-Host "Expected '8', found '$($trx.TestRun.ResultSummary.Counters.passed)'"
    $errorCount++
}

if ($trx.TestRun.ResultSummary.Counters.failed -ne "8") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@failed"
    Write-Host "Expected '8', found '$($trx.TestRun.ResultSummary.Counters.failed)'"
    $errorCount++
}

if ($trx.TestRun.ResultSummary.Counters.notExecuted -ne "0") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@notExecuted"
    Write-Host "Expected '0', found '$($trx.TestRun.ResultSummary.Counters.notExecuted)'"
    $errorCount++
}

# Verify the first success
$errorCount += Verify-Passed $trx "EqualExceptionLegacy.EqualException.EqualsSucceeds (VS2019)"
$errorCount += Verify-NoLogs "EqualException.EqualsSucceeds"

# Verify the second success
$errorCount += Verify-Passed $trx "EqualExceptionLegacy.EqualException.EqualsSucceedsAsync (VS2019)"
$errorCount += Verify-NoLogs "EqualException.EqualsSucceedsAsync"

# Verify the theory success
$errorCount += Verify-Passed $trx "EqualExceptionLegacy.EqualException.EqualsSuccessOrFailureWithParam(value: 0) (VS2019)"
# Can't verify missing logs for this case, because a different test with the same short name failed

# Verify the skipped facts
$errorCount += Verify-Skipped $trx "EqualExceptionLegacy.EqualException.EqualsSkipped (VS2019)"
$errorCount += Verify-NoLogs "EqualException.EqualsSkipped"

# Verify the skipped theories
$errorCount += Verify-Skipped $trx "EqualExceptionLegacy.EqualException.EqualsWithParamSkipped (VS2019)"
$errorCount += Verify-NoLogs "EqualException.EqualsWithParamSkipped"

$errorCount += Verify-Skipped $trx "EqualExceptionLegacy.EqualException.EqualsSuccessOrFailureWithParam(value: 4) (VS2019)"
# Can't verify missing logs for this case, because a different test with the same short name failed

# Verify the first failure (xunit exception)
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualException.EqualsFailure (VS2019)"
$errorCount += Verify-HasLogs "EqualException.EqualsFailure" "EqualException"

# Verify the first failure (non-xunit exception)
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualException.EqualsFailureNonXunit (VS2019)"
$errorCount += Verify-HasLogs "EqualException.EqualsFailureNonXunit" "TargetInvocationException"

# Verify the second failure (xunit exception)
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualException.EqualsFailureAsync (VS2019)"
$errorCount += Verify-HasLogs "EqualException.EqualsFailureAsync" "EqualException"

# Verify the second failure (non-xunit exception)
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualException.EqualsFailureNonXunitAsync (VS2019)"
$errorCount += Verify-HasLogs "EqualException.EqualsFailureNonXunitAsync" "InvalidOperationException"

# Verify failure in constructor
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualExceptionInConstructor.EqualsSucceeds (VS2019)"
$errorCount += Verify-HasLogs "EqualExceptionInConstructor.EqualsSucceeds" "TargetInvocationException"

# Verify failure in before test attribute
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualExceptionInBeforeAfterTest.FailBeforeTest (VS2019)"
$errorCount += Verify-HasLogs "EqualExceptionInBeforeAfterTest.FailBeforeTest" "InvalidOperationException"

# Verify failure in after test attribute
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualExceptionInBeforeAfterTest.FailAfterTest (VS2019)"
$errorCount += Verify-HasLogs "EqualExceptionInBeforeAfterTest.FailAfterTest" "InvalidOperationException"

# Verify the theory failures
$errorCount += Verify-Failed $trx "EqualExceptionLegacy.EqualException.EqualsSuccessOrFailureWithParam(value: 3) (VS2019)"
$errorCount += Verify-HasLogs "EqualException.EqualsSuccessOrFailureWithParam" "EqualException"

if ($errorCount -ne 0) {
    Write-Host "Integration test failed ($errorCount total failures)"
} else {
    Write-Host "Integration test passed (results as expected)."
}

Exit $errorCount
