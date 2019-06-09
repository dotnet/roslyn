// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicReferenceHighlighting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicReferenceHighlighting(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(BasicReferenceHighlighting))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
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
            VisualStudio.Editor.SetText(text);
            Verify("Goo", spans);

            // Verify tags disappear
            VerifyNone("4");
        }

        private void Verify(string marker, IDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            VisualStudio.Editor.PlaceCaret(marker, charsOffset: -1);
            VisualStudio.Workspace.WaitForAllAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.ReferenceHighlighting);

            AssertEx.SetEqual(spans["reference"], VisualStudio.Editor.GetTagSpans(ReferenceHighlightTag.TagId), message: "Testing 'reference'\r\n");
            AssertEx.SetEqual(spans["writtenReference"], VisualStudio.Editor.GetTagSpans(WrittenReferenceHighlightTag.TagId), message: "Testing 'writtenReference'\r\n");
            AssertEx.SetEqual(spans["definition"], VisualStudio.Editor.GetTagSpans(DefinitionHighlightTag.TagId), message: "Testing 'definition'\r\n");
        }

        private void VerifyNone(string marker)
        {
            VisualStudio.Editor.PlaceCaret(marker, charsOffset: -1);
            VisualStudio.Workspace.WaitForAllAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.ReferenceHighlighting);

            Assert.Empty(VisualStudio.Editor.GetTagSpans(ReferenceHighlightTag.TagId));
            Assert.Empty(VisualStudio.Editor.GetTagSpans(WrittenReferenceHighlightTag.TagId));
            Assert.Empty(VisualStudio.Editor.GetTagSpans(DefinitionHighlightTag.TagId));
        }
    }
}
