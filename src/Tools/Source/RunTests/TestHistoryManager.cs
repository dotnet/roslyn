// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Azure devops limits the number of tests returned per request to 1000.
    /// </summary>
    private const int MaxTestsReturnedPerRequest = 10000;

    /// <summary>
    /// The pipeline id for roslyn-ci, see https://dev.azure.com/dnceng/public/_build?definitionId=15
    /// </summary>
    private const int RoslynCiBuildDefinitionId = 15;

    /// <summary>
    /// The Azure devops project that the build pipeline is located in.
    /// </summary>
    private static readonly Uri s_projectUri = new(@"https://dev.azure.com/dnceng");

    /// <summary>
    /// Todo - build definition comes from System.DefinitionId
    /// Todo - stageName comes from System.StageName to identify which test runs to get from the build.
    /// </summary>
    public static async Task<ImmutableDictionary<string, TimeSpan>> GetTestHistoryAsync(/*int buildDefinitionId, string stageName, string branchName*/CancellationToken cancellationToken)
    {
        var pat = Environment.GetEnvironmentVariable("TEST_PAT");
        var credentials = new Microsoft.VisualStudio.Services.Common.VssBasicCredential(string.Empty, pat);

        var connection = new VssConnection(s_projectUri, credentials);

        var stageName = "Test_Windows_Desktop_Release_64";
        var targetBranchName = "main";

        using var buildClient = connection.GetClient<BuildHttpClient>();

        // Tries to get the latest succeeded build from the input build definition (pipeline) from dnceng/public with the specified branch.
        Logger.Log($"Branch name: {targetBranchName}");
        Logger.Log($"Stage name: {stageName}");

        var adoBranchName = $"refs/heads/{targetBranchName}";
        var builds = await buildClient.GetBuildsAsync2(project: "public", new int[] { RoslynCiBuildDefinitionId }, resultFilter: BuildResult.Succeeded, queryOrder: BuildQueryOrder.FinishTimeDescending, maxBuildsPerDefinition: 1, reasonFilter: BuildReason.IndividualCI, branchName: adoBranchName, cancellationToken: cancellationToken);
        var lastSuccessfulBuild = builds?.FirstOrDefault();
        if (lastSuccessfulBuild == null)
        {
            // If this is a new branch we may not have any historical data for it.
            ConsoleUtil.WriteLine($"Unable to get the last successful build for definition {RoslynCiBuildDefinitionId} and branch {adoBranchName}");
            return ImmutableDictionary<string, TimeSpan>.Empty;
        }

        using var testClient = connection.GetClient<TestResultsHttpClient>();

        // API requires us to pass a time range to query runs for.  So just pass the times from the build.
        var minTime = lastSuccessfulBuild.QueueTime!.Value;
        var maxTime = lastSuccessfulBuild.FinishTime!.Value;
        var runsInBuild = await testClient.QueryTestRunsAsync2("public", minTime, maxTime, buildIds: new int[] { lastSuccessfulBuild.Id }, cancellationToken: cancellationToken);

        var runForThisStage = runsInBuild.SingleOrDefault(r => r.Name.Contains(stageName));
        if (runForThisStage == null)
        {
            // If this is a new stage, historical runs will not have any data for it.
            ConsoleUtil.WriteLine($"Unable to get a run with name {stageName} from build {lastSuccessfulBuild.Url}.");
            return ImmutableDictionary<string, TimeSpan>.Empty;
        }

        ConsoleUtil.WriteLine($"Looking up test execution data from last successful build {lastSuccessfulBuild.Id} on branch {targetBranchName} and stage {stageName}");

        var totalTests = runForThisStage.TotalTests;

        Dictionary<string, TimeSpan> testInfos = new();
        var duplicateCount = 0;

        // Get runtimes for all tests.
        var timer = new Stopwatch();
        timer.Start();
        for (var i = 0; i < totalTests; i += MaxTestsReturnedPerRequest)
        {
            var testResults = await testClient.GetTestResultsAsync("public", runForThisStage.Id, skip: i, top: MaxTestsReturnedPerRequest, cancellationToken: cancellationToken);
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

    private static string CleanTestName(string fullyQualifiedTestName)
    {
        // Some test names contain test arguments, so take everything before the first paren (since they are not valid in the fully qualified test name).
        var beforeMethodArgs = fullyQualifiedTestName.Split('(')[0];
        return beforeMethodArgs;
    }

}
