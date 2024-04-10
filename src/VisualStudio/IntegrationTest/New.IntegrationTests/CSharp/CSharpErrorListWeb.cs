// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.ErrorList)]
    public class CSharpErrorListWeb : AbstractWebApplicationTest
    {
        public CSharpErrorListWeb()
            : base(nameof(CSharpErrorListWeb))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/70627")]
        public async Task ClosedRazorFile()
        {
            var source = """
@page
@{
    var x = "Hello"
}
""";
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, @"Pages\\Index.razor", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(source, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.SaveAllAsync(HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.CloseActiveWindow(HangMitigatingCancellationToken);

            await TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList], HangMitigatingCancellationToken);

            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);

            var actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);

            string[] expectedContents =
            [
                "(Compiler) Index.razor(3, 20): error CS1002: ; expected",
                "(Compiler) Index.razor(3, 9): warning CS0219: The variable 'x' is assigned but its value is never used",
                "(Csc) Index.razor(1, 6): error RZ1016: The 'page' directive expects a string surrounded by double quotes.",
            ];

            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));
        }
    }
}
