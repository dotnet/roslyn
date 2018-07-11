// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReferenceHighlighting : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpReferenceHighlighting()
            : base(nameof(CSharpReferenceHighlighting))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task HighlightingAsync()
        {
            var markup = @"
class {|definition:C|}
{
    void M<T>({|reference:C|} c) where T : {|reference:C|}
    {
        {|reference:C|} c = new {|reference:C|}();
    }
}";
            MarkupTestFile.GetSpans(markup, out var text, out IDictionary<string, ImmutableArray<TextSpan>> spans);
            await VisualStudio.Editor.SetTextAsync(text);
            await VerifyAsync("C", spans);

            // Verify tags disappear
            await VerifyNoneAsync("void");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task WrittenReferenceAsync()
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
            MarkupTestFile.GetSpans(markup, out var text, out IDictionary<string, ImmutableArray<TextSpan>> spans);
            await VisualStudio.Editor.SetTextAsync(text);
            await VerifyAsync("x", spans);

            // Verify tags disappear
            await VerifyNoneAsync("void");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NavigationAsync()
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
            await VisualStudio.Editor.SetTextAsync(text);
            await VisualStudio.Editor.PlaceCaretAsync("x");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_NextHighlightedReference);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("x$$ = 3;", assertCaretPosition: true, trimWhitespace: true);
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

            AssertEx.SetEqual(spans["definition"], await VisualStudio.Editor.GetTagSpansAsync(DefinitionHighlightTag.TagId), message: "Testing 'definition'\r\n");

            if (spans.ContainsKey("reference"))
            {
                AssertEx.SetEqual(spans["reference"], await VisualStudio.Editor.GetTagSpansAsync(ReferenceHighlightTag.TagId), message: "Testing 'reference'\r\n");
            }

            if (spans.ContainsKey("writtenreference"))
            {
                AssertEx.SetEqual(spans["writtenreference"], await VisualStudio.Editor.GetTagSpansAsync(WrittenReferenceHighlightTag.TagId), message: "Testing 'writtenreference'\r\n");
            }
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
            Assert.Empty(await VisualStudio.Editor.GetTagSpansAsync(DefinitionHighlightTag.TagId));
        }
    }
}
