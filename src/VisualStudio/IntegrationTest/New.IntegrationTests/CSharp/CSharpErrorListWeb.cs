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

        [IdeFact]
        public async Task ClosedRazorFile_NoErrors()
        {
            var source = @"
@page
@model IndexModel
@{
    var x = ""Hello"";
}
";
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, @"Pages\\Index.razor", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(source, HangMitigatingCancellationToken);

            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            var expectedContents = new string[] { };
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            var actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join<string>(Environment.NewLine, expectedContents),
                string.Join<string>(Environment.NewLine, actualContents));
        }

        [IdeFact]
        public async Task ClosedRazorFile_Errors()
        {
            var source = @"
@page ""/""
@{
    var x = ""Hello""
}
";
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, @"Pages\\Index.razor", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(source, HangMitigatingCancellationToken);

            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            var expectedContents = new string[] { };
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            var actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join<string>(Environment.NewLine, expectedContents),
                string.Join<string>(Environment.NewLine, actualContents));
        }
    }
}
