// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic;

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
        await TestServices.Editor.SetTextAsync("""
            Module Module1

                Sub Main()
                    Console.WriteLine("Hello, World!")
                End Sub

            End Module
            """, HangMitigatingCancellationToken);

        var succeed = await TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(HangMitigatingCancellationToken);
        Assert.True(succeed);

        await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);

        var errors = await TestServices.ErrorList.GetBuildErrorsAsync(HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(string.Empty, string.Join(Environment.NewLine, errors));
    }
}
