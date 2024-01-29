// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    public class BasicKeywordHighlighting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicKeywordHighlighting()
            : base(nameof(BasicKeywordHighlighting))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task NavigationBetweenKeywords()
        {
            await TestServices.Editor.SetTextAsync(@"
Class C
    Sub Main()
        For a = 0 To 1 Step 1
            For b = 0 To 2
        Next b, a
    End Sub
End Class", HangMitigatingCancellationToken);

            await VerifyAsync("To", 4, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.NextHighlightedReference, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        For a = 0 To 1 Step$$ 1", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        private async Task VerifyAsync(string marker, int expectedCount, CancellationToken cancellationToken)
        {
            await TestServices.Editor.PlaceCaretAsync(marker, charsOffset: -1, cancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                [
                    FeatureAttribute.Workspace,
                    FeatureAttribute.SolutionCrawlerLegacy,
                    FeatureAttribute.DiagnosticService,
                    FeatureAttribute.Classification,
                    FeatureAttribute.KeywordHighlighting,
                ],
                cancellationToken);

            var tags = await TestServices.Editor.GetTagsAsync<ITextMarkerTag>(cancellationToken);
            var tagSpans = tags.SelectAsArray(tag => tag.Tag.Type == KeywordHighlightTag.TagId, tag => tag.Span.Span.ToTextSpan());
            Assert.Equal(expectedCount, tagSpans.Length);
        }
    }
}
