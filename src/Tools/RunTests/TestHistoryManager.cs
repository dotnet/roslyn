// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests;

internal class TestHistoryManager
{
    /// <summary>
    /// Azure devops limits the number of tests returned per request to 10000.
    /// </summary>
    private const int MaxTestsReturnedPerRequest = 10_000;

    /// <summary>
    /// Looks up the last passing test run for the current build and stage to estimate execution times for each
    /// tests. The dictionary is indexed by test full name and contains the body duration and theory instance count.
    /// The theory instance count is sourced from the AzDO <c>subResultsCount</c> field which represents individual
    /// theory invocations reported under a grouped test result.
    ///
    /// Note: the duration returned is the sum of body execution times (DurationInMs) as reported by xUnit.
    /// In xUnit v2, DurationInMs does NOT include IAsyncLifetime.InitializeAsync or DisposeAsync time.
    /// The caller is responsible for adjusting the duration based on the HasAsyncLifetime flag from test discovery.
    /// </summary>
    public static async Task<Dictionary<string, (TimeSpan Duration, int TestTheoryInstances)>?> GetTestHistoryAsync(Options options, CancellationToken cancellationToken)
    {
        // Access token that has permissions to lookup test history.  This typically comes from the pipeline.
        var accessToken = options.AccessToken ?? GetEnvironmentVariable("SYSTEM_ACCESSTOKEN");

        // ADO project that the build pipeline is located in.
        var projectUri = options.ProjectUri ?? GetEnvironmentVariable("SYSTEM_COLLECTIONURI");

        // Id of the pipeline to get test history from.
        var pipelineDefinitionIdStr = options.PipelineDefinitionId ?? GetEnvironmentVariable("SYSTEM_DEFINITIONID");

        // The phase name is used to filter the tests on the last passing build to only those that apply to the currently running phase.
        //   Note here that 'phaseName' corresponds to the 'jobName' defined in our pipeline yaml file and the job name env var is not correct.
        //   See https://developercommunity.visualstudio.com/t/systemjobname-seems-to-be-incorrectly-assigned-and/1209736
        var phaseName = options.PhaseName ?? GetEnvironmentVariable("SYSTEM_PHASENAME");

        // We use the target branch of the current build to lookup the last successful build for the same branch.
        // For PR builds, SYSTEM_PULLREQUEST_TARGETBRANCH gives us the target (e.g. "main").
        // For CI builds, we need the full branch ref. BUILD_SOURCEBRANCH gives us the full ref
        // (e.g. "refs/heads/features/unions") while BUILD_SOURCEBRANCHNAME only gives the last
        // segment (e.g. "unions"), which breaks history lookup for nested branch names.
        var targetBranch = options.TargetBranchName ?? GetEnvironmentVariable("SYSTEM_PULLREQUEST_TARGETBRANCH") ?? GetEnvironmentVariable("BUILD_SOURCEBRANCH");
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(projectUri) || string.IsNullOrEmpty(phaseName) || string.IsNullOrEmpty(targetBranch) || !int.TryParse(pipelineDefinitionIdStr, out var pipelineDefinitionId))
        {
            ConsoleUtil.Warning($"Missing required options to lookup test history, projectUri={projectUri}, phaseName={phaseName}, targetBranchName={targetBranch}, pipelineDefinitionId={pipelineDefinitionIdStr}");
            return null;
        }

        using var azdoClient = AzdoClient.Create(projectUri, accessToken);

        ConsoleUtil.WriteLine($"Getting last successful build for branch {targetBranch}");
        var adoBranch = targetBranch.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase)
            ? targetBranch
            : $"refs/heads/{targetBranch}";
        var lastSuccessfulBuild = await GetLastSuccessfulBuildAsync(azdoClient, pipelineDefinitionId, adoBranch, cancellationToken);
        if (lastSuccessfulBuild == null && adoBranch != "refs/heads/main")
        {
            // If this is a new branch or has no successful builds, fall back to main history
            // to avoid the expensive method-count fallback that creates hundreds of work items.
            ConsoleUtil.Warning($"Unable to get the last successful build for branch {adoBranch}, falling back to refs/heads/main");
            lastSuccessfulBuild = await GetLastSuccessfulBuildAsync(azdoClient, pipelineDefinitionId, "refs/heads/main", cancellationToken);
        }

        if (lastSuccessfulBuild == null)
        {
            // If this is a new branch we may not have any historical data for it.
            ConsoleUtil.Warning($"Unable to get the last successful build for definition {pipelineDefinitionId} in {projectUri} and branch {targetBranch}");
            return null;
        }

        var runForThisStage = await GetRunForStageAsync(azdoClient, lastSuccessfulBuild, phaseName, cancellationToken);
        if (runForThisStage == null)
        {
            // If this is a new stage, historical runs will not have any data for it.
            ConsoleUtil.Warning($"Unable to get a run with name {phaseName} from build {lastSuccessfulBuild.Url}.");
            return null;
        }

        ConsoleUtil.WriteLine($"Looking up test execution data for build {lastSuccessfulBuild.Id} on branch {targetBranch} and stage {phaseName}");

        var totalTests = runForThisStage.TotalTests;

        Dictionary<string, (TimeSpan Duration, int TestTheoryInstances)> testInfos = new();
        var duplicateCount = 0;

        // Get runtimes for all tests.
        var timer = new Stopwatch();
        timer.Start();
        for (var i = 0; i < totalTests; i += MaxTestsReturnedPerRequest)
        {
            var testResults = await GetTestResultsAsync(azdoClient, runForThisStage, i, MaxTestsReturnedPerRequest, cancellationToken);
            foreach (var testResult in testResults)
            {
                // Helix outputs results for the whole dll work item suffixed with WorkItemExecution which we should ignore.
                if (testResult.AutomatedTestName.Contains("WorkItemExecution"))
                {
                    Logger.Log($"Skipping overall result for work item {testResult.AutomatedTestName}");
                    continue;
                }

                var testName = CleanTestName(testResult.AutomatedTestName);

                if (testInfos.TryGetValue(testName, out var existing))
                {
                    // We can get duplicate tests if a test file is included in multiple assemblies (e.g. analyzer codestyle tests).
                    // This is fine, we'll just use capture one of the run times since it is the same test being run in both cases and unlikely to have different run times.
                    //
                    // Another case that can happen is if a test is incorrectly authored to have the same name and namespace as a test in another assembly.  For example
                    // a test that applies to both VB and C#, but the tests in both the C# and VB assembly accidentally use the C# namespace.
                    // It may have a different run time, but ADO does not let us differentiate by assembly name, so we just have to pick one.
                    //
                    // Keep tracking the count of theory instances so we can apply async lifetime adjustment.
                    testInfos[testName] = (existing.Duration, existing.TestTheoryInstances + testResult.SubResultsCount);
                    duplicateCount++;
                }
                else
                {
                    testInfos[testName] = (TimeSpan.FromMilliseconds(testResult.DurationInMs), testResult.SubResultsCount);
                }
            }
        }

        timer.Stop();

        if (duplicateCount > 0)
        {
            Logger.Log($"Found {duplicateCount} duplicate tests in run {runForThisStage.Name}.");
        }

        if (testInfos.Count == 0)
        {
            ConsoleUtil.Warning($"Retrieved zero test results from build {lastSuccessfulBuild.Id} and stage {phaseName}, falling back to count based scheduling");
            return null;
        }

        var totalTestRuntime = TimeSpan.FromMilliseconds(testInfos.Values.Sum(t => t.Duration.TotalMilliseconds));
        ConsoleUtil.WriteLine($"Retrieved {testInfos.Keys.Count} tests from AzureDevops in {timer.Elapsed}.  Total runtime of all tests is {totalTestRuntime}");
        return testInfos;
    }

    private static string? GetEnvironmentVariable(string envVarName)
    {
        var envVar = Environment.GetEnvironmentVariable(envVarName);
        if (string.IsNullOrEmpty(envVar))
        {
            ConsoleUtil.WriteLine($"Missing environment variable {envVarName}");
        }

        return envVar;
    }

    private static string CleanTestName(string fullyQualifiedTestName)
    {
        // Some test names contain test arguments, so take everything before the first paren (since they are not valid in the fully qualified test name).
        var beforeMethodArgs = fullyQualifiedTestName.Split('(')[0];
        return beforeMethodArgs;
    }

    private static async Task<AzdoBuild?> GetLastSuccessfulBuildAsync(AzdoClient azdoClient, int definitionId, string branchName, CancellationToken cancellationToken)
    {
        try
        {
            return await azdoClient.GetLastSuccessfulBuildAsync("public", definitionId, branchName, cancellationToken);
        }
        catch (Exception ex)
        {
            // We will fallback to test count partitioning if we fail to query ADO.
            ConsoleUtil.WriteLine($"Caught exception querying ADO for passing build: {ex}");
            return null;
        }
    }

    private static async Task<AzdoTestRun?> GetRunForStageAsync(AzdoClient azdoClient, AzdoBuild build, string phaseName, CancellationToken cancellationToken)
    {
        try
        {
            return await azdoClient.GetRunForStageAsync("public", build, phaseName, cancellationToken);
        }
        catch (Exception ex)
        {
            // We will fallback to test count partitioning if we fail to query ADO.
            ConsoleUtil.WriteLine($"Caught exception querying ADO for test runs: {ex}");
            return null;
        }
    }

    private static async Task<List<AzdoTestResult>> GetTestResultsAsync(AzdoClient azdoClient, AzdoTestRun testRun, int skip, int top, CancellationToken cancellationToken)
    {
        try
        {
            return await azdoClient.GetTestResultsAsync("public", testRun.Id, skip, top, includeSubResults: true, cancellationToken);
        }
        catch (Exception ex)
        {
            // We will fallback to test count partitioning if we fail to query ADO.
            ConsoleUtil.WriteLine($"Caught exception querying ADO for test runs: {ex}");
            return new List<AzdoTestResult>();
        }
    }
}
