// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TextManager.Interop;
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

    [IdeFact(EnvironmentVariables = ["RoslynSelfBuildTest='true'"])]
    public async Task Test()
    {
        Assert.True(System.Environment.GetEnvironmentVariable("RoslynSelfBuildTest") == "true");
        var solutionDir = @"D:\Sample\roslyn\roslyn.sln";
        await this.TestServices.SolutionExplorer.OpenSolutionAsync(solutionDir, HangMitigatingCancellationToken);
        var result = await this.TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(HangMitigatingCancellationToken);
        var outputResult = await this.TestServices.SolutionExplorer.GetBuildOutputContentAsync(HangMitigatingCancellationToken);
        output.WriteLine(outputResult);
        Assert.Contains("0 failed", result);
    }
}
