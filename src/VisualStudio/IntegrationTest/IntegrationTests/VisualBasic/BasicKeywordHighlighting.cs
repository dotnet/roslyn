// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicKeywordHighlighting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicKeywordHighlighting(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicKeywordHighlighting))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void NavigationBetweenKeywords()
        {
            VisualStudio.Editor.SetText(@"
Class C
    Sub Main()
        For a = 0 To 1 Step 1
            For b = 0 To 2
        Next b, a
    End Sub
End Class");

            Verify("To", 3);
            VisualStudio.ExecuteCommand("Edit.NextHighlightedReference");
            VisualStudio.Editor.Verify.CurrentLineText("For a = 0 To 1 Step$$ 1", assertCaretPosition: true, trimWhitespace: true);
        }

#pragma warning disable IDE0060 // Remove unused parameter - https://github.com/dotnet/roslyn/issues/46169
        private void Verify(string marker, int expectedCount)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            VisualStudio.Editor.PlaceCaret(marker, charsOffset: -1);
            VisualStudio.Workspace.WaitForAllAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.KeywordHighlighting);

            // Assert.Equal(expectedCount, VisualStudio.Editor.GetKeywordHighlightTagCount());
        }
    }
}
