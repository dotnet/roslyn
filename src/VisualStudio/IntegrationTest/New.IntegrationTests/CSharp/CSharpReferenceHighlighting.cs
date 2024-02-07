// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.Classification)]
    public class CSharpReferenceHighlighting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpReferenceHighlighting()
            : base(nameof(CSharpReferenceHighlighting))
        {
        }

        [IdeFact]
        public async Task Highlighting()
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
            await TestServices.Editor.SetTextAsync(text, HangMitigatingCancellationToken);
            await VerifyAsync("C", spans, HangMitigatingCancellationToken);

            // Verify tags disappear
            await VerifyNoneAsync("void", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task WrittenReference()
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
            await TestServices.Editor.SetTextAsync(text, HangMitigatingCancellationToken);
            await VerifyAsync("x", spans, HangMitigatingCancellationToken);

            // Verify tags disappear
            await VerifyNoneAsync("void", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task Navigation()
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
            await TestServices.Editor.SetTextAsync(text, HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("x", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.NextHighlightedReference, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        x$$ = 3;", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [WorkItem("https://github.com/dotnet/roslyn/pull/52041")]
        [IdeFact]
        public async Task HighlightBasedOnSelection()
        {
            var text = @"
class C
{
   void M()
    {
        int x = 0;
        x++;
        x = 3;
    }
}";
            await TestServices.Editor.SetTextAsync(text, HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("x", charsOffset: 0, HangMitigatingCancellationToken);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.NextHighlightedReference, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        x$$++;", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.NextHighlightedReference, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        x$$ = 3;", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        private async Task VerifyAsync(string marker, IDictionary<string, ImmutableArray<TextSpan>> spans, CancellationToken cancellationToken)
        {
            await TestServices.Editor.PlaceCaretAsync(marker, charsOffset: -1, cancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                [
                    FeatureAttribute.Workspace,
                    FeatureAttribute.SolutionCrawlerLegacy,
                    FeatureAttribute.DiagnosticService,
                    FeatureAttribute.Classification,
                    FeatureAttribute.ReferenceHighlighting,
                ],
                cancellationToken);

            var tags = await TestServices.Editor.GetTagsAsync<ITextMarkerTag>(cancellationToken);
            var definitionTagSpans = tags.SelectAsArray(tag => tag.Tag.Type == DefinitionHighlightTag.TagId, tag => tag.Span.Span.ToTextSpan());
            AssertEx.SetEqual(spans["definition"], definitionTagSpans, message: "Testing 'definition'\r\n");

            if (spans.TryGetValue("reference", out var referenceSpans))
            {
                var referenceTagSpans = tags.SelectAsArray(tag => tag.Tag.Type == ReferenceHighlightTag.TagId, tag => tag.Span.Span.ToTextSpan());
                AssertEx.SetEqual(referenceSpans, referenceTagSpans, message: "Testing 'reference'\r\n");
            }

            if (spans.TryGetValue("writtenreference", out var writtenReferenceSpans))
            {
                var writtenReferenceTagSpans = tags.SelectAsArray(tag => tag.Tag.Type == WrittenReferenceHighlightTag.TagId, tag => tag.Span.Span.ToTextSpan());
                AssertEx.SetEqual(writtenReferenceSpans, writtenReferenceSpans, message: "Testing 'writtenreference'\r\n");
            }
        }

        private async Task VerifyNoneAsync(string marker, CancellationToken cancellationToken)
        {
            await TestServices.Editor.PlaceCaretAsync(marker, charsOffset: -1, cancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                [
                    FeatureAttribute.Workspace,
                    FeatureAttribute.SolutionCrawlerLegacy,
                    FeatureAttribute.DiagnosticService,
                    FeatureAttribute.Classification,
                    FeatureAttribute.ReferenceHighlighting,
                ],
                cancellationToken);

            var tags = await TestServices.Editor.GetTagsAsync<ITextMarkerTag>(cancellationToken);
            Assert.Empty(tags.Where(tag => tag.Tag.Type == ReferenceHighlightTag.TagId));
            Assert.Empty(tags.Where(tag => tag.Tag.Type == DefinitionHighlightTag.TagId));
        }
    }
}
