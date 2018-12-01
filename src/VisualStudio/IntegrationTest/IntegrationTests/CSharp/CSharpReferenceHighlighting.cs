// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpReferenceHighlighting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpReferenceHighlighting( )
            : base( nameof(CSharpReferenceHighlighting))
        {
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
        public void Highlighting()
        {
            var markup = @"
class {|definition:C|}
{
    void M<T>({|reference:C|} c) where T : {|reference:C|}
    {
        {|reference:C|} c = new {|reference:C|}();
    }
}";
            Test.Utilities.MarkupTestFile.GetSpans(markup, out var text, out IDictionary<string, ImmutableArray<TextSpan>> spans);
            VisualStudioInstance.Editor.SetText(text);
            VerifyTagSpans("C", spans);

            // Verify tags disappear
            VerifyNone("void");
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
        public void WrittenReference()
        {
            var markup = @"
class C
{
    void M()
    {
        int {|definition:x|};
        {|writtenreference:x|} = 3;
    }
}";
            Test.Utilities.MarkupTestFile.GetSpans(markup, out var text, out IDictionary<string, ImmutableArray<TextSpan>> spans);
            VisualStudioInstance.Editor.SetText(text);
            VerifyTagSpans("x", spans);

            // Verify tags disappear
            VerifyNone("void");
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
        public void Navigation()
        {
            var text = @"
class C
{
   void M()
    {
        int x;
        x = 3;
    }
}";
            VisualStudioInstance.Editor.SetText(text);
            VisualStudioInstance.Editor.PlaceCaret("x");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudioInstance.ExecuteCommand("Edit.NextHighlightedReference");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudioInstance.Editor.Verify.CurrentLineText("x$$ = 3;", assertCaretPosition: true, trimWhitespace: true);
        }

        private void VerifyTagSpans(string marker, IDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            VisualStudioInstance.Editor.PlaceCaret(marker, charsOffset: -1);
            VisualStudioInstance.Workspace.WaitForAllAsyncOperations(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.ReferenceHighlighting);

            AssertEx.SetEqual(spans["definition"], VisualStudioInstance.Editor.GetTagSpans(DefinitionHighlightTag.TagId), message: "Testing 'definition'\r\n");

            if (spans.ContainsKey("reference"))
            {
                AssertEx.SetEqual(spans["reference"], VisualStudioInstance.Editor.GetTagSpans(ReferenceHighlightTag.TagId), message: "Testing 'reference'\r\n");
            }

            if (spans.ContainsKey("writtenreference"))
            {
                AssertEx.SetEqual(spans["writtenreference"], VisualStudioInstance.Editor.GetTagSpans(WrittenReferenceHighlightTag.TagId), message: "Testing 'writtenreference'\r\n");
            }
        }

        private void VerifyNone(string marker)
        {
            VisualStudioInstance.Editor.PlaceCaret(marker, charsOffset: -1);
            VisualStudioInstance.Workspace.WaitForAllAsyncOperations(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.ReferenceHighlighting);

            ExtendedAssert.Empty(VisualStudioInstance.Editor.GetTagSpans(ReferenceHighlightTag.TagId));
            ExtendedAssert.Empty(VisualStudioInstance.Editor.GetTagSpans(DefinitionHighlightTag.TagId));
        }
    }
}
