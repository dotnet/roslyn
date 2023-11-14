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

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    public class BasicReferenceHighlighting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicReferenceHighlighting()
            : base(nameof(BasicReferenceHighlighting))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task Highlighting()
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
            await TestServices.Editor.SetTextAsync(text, HangMitigatingCancellationToken);
            await VerifyAsync("Goo", spans, HangMitigatingCancellationToken);

            // Verify tags disappear
            await VerifyNoneAsync("4", HangMitigatingCancellationToken);
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
            AssertEx.SetEqual(spans["reference"], tags.SelectAsArray(tag => tag.Tag.Type == ReferenceHighlightTag.TagId, tag => tag.Span.Span.ToTextSpan()), message: "Testing 'reference'\r\n");
            AssertEx.SetEqual(spans["writtenReference"], tags.SelectAsArray(tag => tag.Tag.Type == WrittenReferenceHighlightTag.TagId, tag => tag.Span.Span.ToTextSpan()), message: "Testing 'writtenReference'\r\n");
            AssertEx.SetEqual(spans["definition"], tags.SelectAsArray(tag => tag.Tag.Type == DefinitionHighlightTag.TagId, tag => tag.Span.Span.ToTextSpan()), message: "Testing 'definition'\r\n");
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
            Assert.Empty(tags.Where(tag => tag.Tag.Type == WrittenReferenceHighlightTag.TagId));
            Assert.Empty(tags.Where(tag => tag.Tag.Type == DefinitionHighlightTag.TagId));
        }
    }
}
