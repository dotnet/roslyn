// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [TestClass]
    public class BasicKeywordHighlighting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicKeywordHighlighting() : base(nameof(BasicKeywordHighlighting))
        {
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
        public void NavigationBetweenKeywords()
        {
            VisualStudioInstance.Editor.SetText(@"
Class C
    Sub Main()
        For a = 0 To 1 Step 1
            For b = 0 To 2
        Next b, a
    End Sub
End Class");

            KeywordHighlightTagCount("To", 3);
            VisualStudioInstance.ExecuteCommand("Edit.NextHighlightedReference");
            VisualStudioInstance.Editor.Verify.CurrentLineText("For a = 0 To 1 Step$$ 1", assertCaretPosition: true, trimWhitespace: true);
        }

        private void KeywordHighlightTagCount(string marker, int expectedCount)
        {
            VisualStudioInstance.Editor.PlaceCaret(marker, charsOffset: -1);
            VisualStudioInstance.Workspace.WaitForAllAsyncOperations(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.KeywordHighlighting);

            // Assert.AreEqual(expectedCount, VisualStudio.Editor.GetKeywordHighlightTagCount());
            // TODO
        }
    }
}
