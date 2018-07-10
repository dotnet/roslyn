// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicKeywordHighlighting : AbstractIdeEditorTest
    {
        public BasicKeywordHighlighting()
            : base(nameof(BasicKeywordHighlighting))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NavigationBetweenKeywordsAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
Class C
    Sub Main()
        For a = 0 To 1 Step 1
            For b = 0 To 2
        Next b, a
    End Sub
End Class");

            await VerifyAsync("To", 3);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_NextHighlightedReference);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("For a = 0 To 1 Step$$ 1", assertCaretPosition: true, trimWhitespace: true);
        }

        private async Task VerifyAsync(string marker, int expectedCount)
        {
            await VisualStudio.Editor.PlaceCaretAsync(marker, charsOffset: -1);
            await VisualStudio.Workspace.WaitForAllAsyncOperationsAsync(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.KeywordHighlighting);

            // Assert.Equal(expectedCount, VisualStudio.Editor.GetKeywordHighlightTagCount());
        }
    }
}
