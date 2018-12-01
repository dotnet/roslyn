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

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [TestClass]
    public class BasicReferenceHighlighting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicReferenceHighlighting() : base(nameof(BasicReferenceHighlighting))
        {
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
        public void Highlighting()
        {
            var markup = @"
Class C
    Dim {|definition:Goo|} as Int32
    Function M()
        Console.WriteLine({|reference:Goo|})
        {|writtenReference:Goo|} = 4
    End Function
End Class";
            Test.Utilities.MarkupTestFile.GetSpans(markup, out var text, out IDictionary<string, ImmutableArray<TextSpan>> spans);
            VisualStudioInstance.Editor.SetText(text);
            VerifyTagSpans("Goo", spans);

            // Verify tags disappear
            VerifyNone("4");
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

            AssertEx.SetEqual(spans["reference"], VisualStudioInstance.Editor.GetTagSpans(ReferenceHighlightTag.TagId), message: "Testing 'reference'\r\n");
            AssertEx.SetEqual(spans["writtenReference"], VisualStudioInstance.Editor.GetTagSpans(WrittenReferenceHighlightTag.TagId), message: "Testing 'writtenReference'\r\n");
            AssertEx.SetEqual(spans["definition"], VisualStudioInstance.Editor.GetTagSpans(DefinitionHighlightTag.TagId), message: "Testing 'definition'\r\n");
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
            ExtendedAssert.Empty(VisualStudioInstance.Editor.GetTagSpans(WrittenReferenceHighlightTag.TagId));
            ExtendedAssert.Empty(VisualStudioInstance.Editor.GetTagSpans(DefinitionHighlightTag.TagId));
        }
    }
}
