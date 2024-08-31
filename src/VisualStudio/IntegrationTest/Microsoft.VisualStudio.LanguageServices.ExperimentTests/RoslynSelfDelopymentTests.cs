// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;
using Xunit.Abstractions;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using static Microsoft.VisualStudio.Extensibility.Testing.SolutionExplorerInProcess;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Experiment.IntegrationTests;

[IdeSettings(MinVersion = VisualStudioVersion.VS2022, RootSuffix = "Exp", MaxAttempts = 2)]
public class RoslynSelfBuildTests(ITestOutputHelper output) : AbstractIdeIntegrationTest
{
    private readonly CancellationTokenSource _longTimeTestExecutionCancellationTokenSource = new(TimeSpan.FromMinutes(30));

    [IdeFact]
    public async Task SelfBuildAndDeploy()
    {
        // https://github.com/microsoft/vs-extension-testing/issues/172
        Environment.SetEnvironmentVariable("runExperimentTest", "true");
        Environment.SetEnvironmentVariable("MSBUILDTERMINALLOGGER ", "auto");
        // Will cause msbuild lock dlls...
        // Environment.SetEnvironmentVariable("MSBuildDebugEngine", "1");
        Environment.SetEnvironmentVariable("experimentTestAssetsDir", @"D:\Sample\roslyn");
        var testAssetDirectory = Environment.GetEnvironmentVariable("experimentTestAssetsDir");
        Assert.NotNull(testAssetDirectory);
        var solutionDir = Path.Combine(testAssetDirectory, "Roslyn.sln");
        Assert.True(File.Exists(solutionDir));
        await this.TestServices.SolutionExplorer.OpenSolutionAsync(solutionDir, _longTimeTestExecutionCancellationTokenSource.Token);

        await this.TestOperationAndReportIfFailedAsync(
            () => this.TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(_longTimeTestExecutionCancellationTokenSource.Token), _longTimeTestExecutionCancellationTokenSource.Token);
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

    public override void Dispose()
    {
        _longTimeTestExecutionCancellationTokenSource.Dispose();
        base.Dispose();
    }
}
