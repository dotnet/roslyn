﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.Build)]
    public class CSharpBuild : AbstractIntegrationTest
    {
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CSharpBuild), HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task BuildProject()
        {
            var editorText = @"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(""Hello, World!"");
    }
}";

            await TestServices.Editor.SetTextAsync(editorText, HangMitigatingCancellationToken);

            var buildSummary = await TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(HangMitigatingCancellationToken);
            Assert.Equal("========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========", buildSummary);

            await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);

            var errors = await TestServices.ErrorList.GetBuildErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(string.Empty, string.Join(Environment.NewLine, errors));
        }

        [IdeFact]
        public async Task BuildWithCommandLine()
        {
            await TestServices.SolutionExplorer.SaveAllAsync(HangMitigatingCancellationToken);

            var pathToDevenv = Process.GetCurrentProcess().MainModule.FileName;
            Assert.Equal("devenv.exe", Path.GetFileName(pathToDevenv));
            var (_, pathToSolution, _) = await TestServices.SolutionExplorer.GetSolutionInfoAsync(HangMitigatingCancellationToken);
            var logFileName = pathToSolution + ".log";

            File.Delete(logFileName);

            var commandLine = $"\"{pathToSolution}\" /Rebuild Debug /Out \"{logFileName}\" /rootsuffix RoslynDev /log";

            var process = Process.Start(pathToDevenv, commandLine);
            Assert.Equal(0, await process.WaitForExitAsync(HangMitigatingCancellationToken));

            Assert.Contains("Rebuild All: 1 succeeded, 0 failed, 0 skipped", File.ReadAllText(logFileName));

            Assert.Equal(0, process.ExitCode);
        }
    }
}
