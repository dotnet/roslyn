// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServices.Experiment.IntegrationTests;

[IdeSettings(MinVersion = VisualStudioVersion.VS2022, RootSuffix = "RoslynDev", MaxAttempts = 1)]
public class RoslynSelfBuildTests(ITestOutputHelper output) : AbstractIntegrationTest
{
    [ConditionalIdeFact(typeof(WindowsOnly), Reason = "We want to monitor the health of F5 deployment")]
    public async Task SelfBuildAndDeploy()
    {
        // https://github.com/microsoft/vs-extension-testing/issues/172
        Environment.SetEnvironmentVariable("RoslynSelfBuildTest", "true");
        Environment.SetEnvironmentVariable("MSBUILDTERMINALLOGGER ", "auto");
        Environment.SetEnvironmentVariable("MSBuildDebugEngine", "1");
        Environment.SetEnvironmentVariable("RoslynSelfBuildTestAsset", @"C:\Users\shech\Workspace\Sample\roslyn");
        var testAssetDirectory = Environment.GetEnvironmentVariable("RoslynSelfBuildTestAsset");
        Assert.NotNull(testAssetDirectory);
        var solutionDir = Path.Combine(testAssetDirectory, "roslyn.sln");
        Assert.True(File.Exists(solutionDir));
        await this.TestServices.SolutionExplorer.OpenSolutionAsync(solutionDir, HangMitigatingCancellationToken);

        await this.TestOperationAndReportIfFailedAsync(
            () => this.TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(HangMitigatingCancellationToken), HangMitigatingCancellationToken);

        await this.TestOperationAndReportIfFailedAsync(
            () => this.TestServices.SolutionExplorer.DeploySolutionAsync(attachingDebugger: false, HangMitigatingCancellationToken), HangMitigatingCancellationToken);
    }

    private async Task TestOperationAndReportIfFailedAsync(Func<Task<bool>> operation, CancellationToken cancellationToken)
    {
        var result = await operation();
        if (!result)
        {
            var buildOutput = await this.TestServices.SolutionExplorer.GetBuildOutputContentAsync(cancellationToken);
            output.WriteLine(buildOutput);
        }
        Assert.True(result);
    }
}
