// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.DevLoop;

[IdeSettings(MinVersion = VisualStudioVersion.VS2022, RootSuffix = "RoslynDev", MaxAttempts = 1)]
public class RoslynSelfBuildTests : AbstractIntegrationTest
{
    private readonly ITestOutputHelper output;

    public RoslynSelfBuildTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    // TODO: Debug the test discover why test can't be find
    // [ConditionalIdeFact(typeof(DartLabCIOnly), Reason = "We want to monitor the health of F5 deployment")]
    [IdeFact]
    public async Task Test()
    {
        // https://github.com/microsoft/vs-extension-testing/issues/172
        Environment.SetEnvironmentVariable("RoslynSelfBuildTest", "true");
        Environment.SetEnvironmentVariable("MSBUILDTERMINALLOGGER ", "auto");
        var solutionDir = @"D:\repo\roslyn\roslyn.sln";
        await this.TestServices.SolutionExplorer.OpenSolutionAsync(solutionDir, HangMitigatingCancellationToken);
        var result = await this.TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(HangMitigatingCancellationToken);
        var outputResult = await this.TestServices.SolutionExplorer.GetBuildOutputContentAsync(HangMitigatingCancellationToken);
        output.WriteLine(outputResult);
        Assert.Contains("0 failed", result);
        await this.TestServices.Shell.ExecuteCommandAsync("Debug.StartWithoutDebugging", HangMitigatingCancellationToken);
    }
}
