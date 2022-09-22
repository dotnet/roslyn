// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.TestResults.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace RunTests;
internal class TestHistoryManager
{
    /// <summary>
    /// Azure devops limits the number of tests returned per request to 10000.
    /// </summary>
    private const int MaxTestsReturnedPerRequest = 10_000;

    /// <summary>
    /// Looks up the last passing test run for the current build and stage to estimate execution times for each test.
    /// </summary>
    public static async Task<ImmutableDictionary<string, TimeSpan>> GetTestHistoryAsync(Options options, CancellationToken cancellationToken)
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
        var targetBranch = options.TargetBranchName ?? GetEnvironmentVariable("SYSTEM_PULLREQUEST_TARGETBRANCH") ?? GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME");
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(projectUri) || string.IsNullOrEmpty(phaseName) || string.IsNullOrEmpty(targetBranch) || !int.TryParse(pipelineDefinitionIdStr, out var pipelineDefinitionId))
        {
            ConsoleUtil.WriteLine($"Missing required options to lookup test history, accessToken={accessToken}, projectUri={projectUri}, phaseName={phaseName}, targetBranchName={targetBranch}, pipelineDefinitionId={pipelineDefinitionIdStr}");
            return ImmutableDictionary<string, TimeSpan>.Empty;
        }

        var credentials = new Microsoft.VisualStudio.Services.Common.VssBasicCredential(string.Empty, accessToken);

        var connection = new VssConnection(new Uri(projectUri), credentials);

        using var buildClient = connection.GetClient<BuildHttpClient>();

        ConsoleUtil.WriteLine($"Getting last successful build for branch {targetBranch}");
        var adoBranch = $"refs/heads/{targetBranch}";
        var lastSuccessfulBuild = await GetLastSuccessfulBuildAsync(pipelineDefinitionId, adoBranch, buildClient, cancellationToken);
        if (lastSuccessfulBuild == null)
        {
            // If this is a new branch we may not have any historical data for it.
            ConsoleUtil.WriteLine($"Unable to get the last successful build for definition {pipelineDefinitionId} in {projectUri} and branch {targetBranch}");
            return ImmutableDictionary<string, TimeSpan>.Empty;
        }

        using var testClient = connection.GetClient<TestResultsHttpClient>();
        var runForThisStage = await GetRunForStageAsync(lastSuccessfulBuild, phaseName, testClient, cancellationToken);
        if (runForThisStage == null)
        {
            // If this is a new stage, historical runs will not have any data for it.
            ConsoleUtil.WriteLine($"Unable to get a run with name {phaseName} from build {lastSuccessfulBuild.Url}.");
            return ImmutableDictionary<string, TimeSpan>.Empty;
        }

        ConsoleUtil.WriteLine($"Looking up test execution data for build {lastSuccessfulBuild.Id} on branch {targetBranch} and stage {phaseName}");

        var totalTests = runForThisStage.TotalTests;

        Dictionary<string, TimeSpan> testInfos = new();
        var duplicateCount = 0;

        // Get runtimes for all tests.
        var timer = new Stopwatch();
        timer.Start();
        for (var i = 0; i < totalTests; i += MaxTestsReturnedPerRequest)
        {
            var testResults = await GetTestResultsAsync(runForThisStage, i, MaxTestsReturnedPerRequest, testClient, cancellationToken);
            foreach (var testResult in testResults)
            {
                // Helix outputs results for the whole dll work item suffixed with WorkItemExecution which we should ignore.
                if (testResult.AutomatedTestName.Contains("WorkItemExecution"))
                {
                    Logger.Log($"Skipping overall result for work item {testResult.AutomatedTestName}");
                    continue;
                }

                var testName = CleanTestName(testResult.AutomatedTestName);

                if (!testInfos.TryAdd(testName, TimeSpan.FromMilliseconds(testResult.DurationInMs)))
                {
                    // We can get duplicate tests if a test file is included in multiple assemblies (e.g. analyzer codestyle tests).
                    // This is fine, we'll just use capture one of the run times since it is the same test being run in both cases and unlikely to have different run times.
                    //
                    // Another case that can happen is if a test is incorrectly authored to have the same name and namespace as a test in another assembly.  For example
                    // a test that applies to both VB and C#, but the tests in both the C# and VB assembly accidentally use the C# namespace.
                    // It may have a different run time, but ADO does not let us differentiate by assembly name, so we just have to pick one.
                    duplicateCount++;
                }
            }
        }

        timer.Stop();

        if (duplicateCount > 0)
        {
            Logger.Log($"Found {duplicateCount} duplicate tests in run {runForThisStage.Name}.");
        }

        var totalTestRuntime = TimeSpan.FromMilliseconds(testInfos.Values.Sum(t => t.TotalMilliseconds));
        ConsoleUtil.WriteLine($"Retrieved {testInfos.Keys.Count} tests from AzureDevops in {timer.Elapsed}.  Total runtime of all tests is {totalTestRuntime}");
        return testInfos.ToImmutableDictionary();
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

    private static async Task<Build?> GetLastSuccessfulBuildAsync(int definitionId, string branchName, BuildHttpClient buildClient, CancellationToken cancellationToken)
    {
        try
        {
            var builds = await buildClient.GetBuildsAsync2(
                        project: "public",
                        new int[] { definitionId },
                        resultFilter: BuildResult.Succeeded,
                        queryOrder: BuildQueryOrder.FinishTimeDescending,
                        maxBuildsPerDefinition: 1,
                        reasonFilter: BuildReason.IndividualCI,
                        branchName: branchName,
                        cancellationToken: cancellationToken);
            return builds?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            // We will fallback to test count partitioning if we fail to query ADO.
            ConsoleUtil.WriteLine($"Caught exception querying ADO for passing build: {ex}");
            return null;
        }
    }

    private static async Task<TestRun?> GetRunForStageAsync(Build build, string phaseName, TestResultsHttpClient testClient, CancellationToken cancellationToken)
    {
        try
        {
            // API requires us to pass a time range to query runs for.  So just pass the times from the build.
            var minTime = build.QueueTime!.Value;
            var maxTime = build.FinishTime!.Value;
            var runsInBuild = await testClient.QueryTestRunsAsync2("public", minTime, maxTime, buildIds: new int[] { build.Id }, cancellationToken: cancellationToken);

            var runForThisStage = runsInBuild.SingleOrDefault(r => r.Name.Contains(phaseName));
            return runForThisStage;
        }
        catch (Exception ex)
        {
            // We will fallback to test count partitioning if we fail to query ADO.
            ConsoleUtil.WriteLine($"Caught exception querying ADO for test runs: {ex}");
            return null;
        }
    }

    private static async Task<List<TestCaseResult>> GetTestResultsAsync(TestRun testRun, int skip, int top, TestResultsHttpClient testClient, CancellationToken cancellationToken)
    {
        try
        {
            var testResults = await testClient.GetTestResultsAsync("public", testRun.Id, skip: skip, top: top, cancellationToken: cancellationToken);
            return testResults ?? new List<TestCaseResult>();
        }
        catch (Exception ex)
        {
            // We will fallback to test count partitioning if we fail to query ADO.
            ConsoleUtil.WriteLine($"Caught exception querying ADO for test runs: {ex}");
            return new List<TestCaseResult>();
        }
    }
}
