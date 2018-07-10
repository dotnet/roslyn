// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicReferenceHighlighting : AbstractIdeEditorTest
    {
        public BasicReferenceHighlighting()
            : base(nameof(BasicReferenceHighlighting))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task HighlightingAsync()
        {
            var markup = @"
Class C
    Dim {|definition:Goo|} as Int32
    Function M()
        Console.WriteLine({|reference:Goo|})
        {|writtenReference:Goo|} = 4
    End Function
End Class";
            MarkupTestFile.GetSpans(markup, out var text, out IDictionary<string, ImmutableArray<TextSpan>> spans);
            await VisualStudio.Editor.SetTextAsync(text);
            await VerifyAsync("Goo", spans);

            // Verify tags disappear
            await VerifyNoneAsync("4");
        }

        private async Task VerifyAsync(string marker, IDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            await VisualStudio.Editor.PlaceCaretAsync(marker, charsOffset: -1);
            await VisualStudio.Workspace.WaitForAllAsyncOperationsAsync(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.ReferenceHighlighting);

            AssertEx.SetEqual(spans["reference"], await VisualStudio.Editor.GetTagSpansAsync(ReferenceHighlightTag.TagId), message: "Testing 'reference'\r\n");
            AssertEx.SetEqual(spans["writtenReference"], await VisualStudio.Editor.GetTagSpansAsync(WrittenReferenceHighlightTag.TagId), message: "Testing 'writtenReference'\r\n");
            AssertEx.SetEqual(spans["definition"], await VisualStudio.Editor.GetTagSpansAsync(DefinitionHighlightTag.TagId), message: "Testing 'definition'\r\n");
        }

        private async Task VerifyNoneAsync(string marker)
        {
            await VisualStudio.Editor.PlaceCaretAsync(marker, charsOffset: -1);
            await VisualStudio.Workspace.WaitForAllAsyncOperationsAsync(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.ReferenceHighlighting);

            Assert.Empty(await VisualStudio.Editor.GetTagSpansAsync(ReferenceHighlightTag.TagId));
            Assert.Empty(await VisualStudio.Editor.GetTagSpansAsync(WrittenReferenceHighlightTag.TagId));
            Assert.Empty(await VisualStudio.Editor.GetTagSpansAsync(DefinitionHighlightTag.TagId));
        }
    }
}
