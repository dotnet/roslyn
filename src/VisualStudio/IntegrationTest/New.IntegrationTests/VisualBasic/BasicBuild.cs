// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Harness;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    public class BasicBuild : AbstractIntegrationTest
    {
        public BasicBuild() : base()
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(BasicBuild), HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic, HangMitigatingCancellationToken);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Build)]
        public async Task BuildProject()
        {
            var editorText = @"Module Module1

    Sub Main()
        Console.WriteLine(""Hello, World!"")
    End Sub

End Module";

            await TestServices.Editor.SetTextAsync(editorText, HangMitigatingCancellationToken);

            var buildSummary = await TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(HangMitigatingCancellationToken);
            Assert.Equal("========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========", buildSummary);

            await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);

            var errors = await TestServices.ErrorList.GetBuildErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(string.Empty, string.Join(Environment.NewLine, errors));

            DataCollectionService.RegisterCustomLogger(
                static fileName =>
                {
                    var content = string.Join(Environment.NewLine, Directory.EnumerateFiles("D:\\a\\_work\\1\\s\\artifacts\\TestResults\\Debug"));
                    File.WriteAllText(fileName, content);

                },
                logId: "customLogging",
                extension: "log");

            Assert.True(false);
        }
    }
}
