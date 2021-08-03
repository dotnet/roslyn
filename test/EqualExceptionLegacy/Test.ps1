Param (
    [string]$binlog
)

if (-not $env:GITBUILDVERSIONSIMPLE) {
    Write-Host "Missing environment variable 'GITBUILDVERSIONSIMPLE'"
    Exit 1
}

$resultDir = $env:AGENT_TEMPDIRECTORY
if (-not $env:AGENT_TEMPDIRECTORY -or -not (Test-Path $env:AGENT_TEMPDIRECTORY)) {
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
mkdir $CurrentTestResultsDir
dotnet test /bl:$binlog --logger trx --results-directory $CurrentTestResultsDir $PSScriptRoot\EqualExceptionLegacy.csproj
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

if ($trx.TestRun.ResultSummary.Counters.executed -ne "4") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@executed"
    Write-Host "Expected '4', found '$($trx.TestRun.ResultSummary.Counters.executed)'"
    Exit 1
}

if ($trx.TestRun.ResultSummary.Counters.total -ne "20") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@total"
    Write-Host "Expected '20', found '$($trx.TestRun.ResultSummary.Counters.total)'"
    Exit 1
}

if ($trx.TestRun.ResultSummary.Counters.passed -ne "2") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@passed"
    Write-Host "Expected '2', found '$($trx.TestRun.ResultSummary.Counters.passed)'"
    Exit 1
}

if ($trx.TestRun.ResultSummary.Counters.failed -ne "2") {
    Write-Host "Testing /TestRun/ResultSummary/Counters/@failed"
    Write-Host "Expected '2', found '$($trx.TestRun.ResultSummary.Counters.failed)'"
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

# Verify the second success
$pass2 = ($trx | Select-Xml -Namespace $ns "/n:TestRun/n:Results/n:UnitTestResult[@testName='EqualExceptionLegacy.EqualException.EqualsSucceedsAsync (VS2019)']")
if ($pass2.Node.outcome -ne "Passed") {
    Write-Host "Testing 'EqualsSucceedsAsync' outcome"
    Write-Host "Expected 'Passed', found '$($pass2.Node.outcome)'"
    Exit 1
}

# Verify the first failure
$failure1 = ($trx | Select-Xml -Namespace $ns "/n:TestRun/n:Results/n:UnitTestResult[@testName='EqualExceptionLegacy.EqualException.EqualsFailure (VS2019)']")
if ($failure1.Node.outcome -ne "Failed") {
    Write-Host "Testing 'EqualsFailure' outcome"
    Write-Host "Expected 'Failed', found '$($failure1.Node.outcome)'"
    Exit 1
}

# Verify the second failure
$failure2 = ($trx | Select-Xml -Namespace $ns "/n:TestRun/n:Results/n:UnitTestResult[@testName='EqualExceptionLegacy.EqualException.EqualsFailureAsync (VS2019)']")
if ($failure2.Node.outcome -ne "Failed") {
    Write-Host "Testing 'EqualsFailureAsync' outcome"
    Write-Host "Expected 'Failed', found '$($failure2.Node.outcome)'"
    Exit 1
}

Write-Host "Integration test passed (results as expected)."
Exit 0
