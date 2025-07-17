// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public class CSharpKeywordHighlighting : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpKeywordHighlighting()
        : base(nameof(CSharpKeywordHighlighting))
    {
    }

    [IdeFact]
    public async Task Foreach()
    {
        MarkupTestFile.GetSpans("""
            class C
            {
                void M()
                {
                    [|foreach|](var c in "") { if(true) [|break|]; else [|continue|]; }
                }
            }
            """, out var text, out var spans);

        await TestServices.Editor.SetTextAsync(text, HangMitigatingCancellationToken);

        await VerifyAsync("foreach", spans, HangMitigatingCancellationToken);
        await VerifyAsync("break", spans, HangMitigatingCancellationToken);
        await VerifyAsync("continue", spans, HangMitigatingCancellationToken);
        await VerifyAsync("in", [], HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task PreprocessorConditionals()
    {
        MarkupTestFile.GetSpans(
            """

            #define Debug
            #undef Trace
            class PurchaseTransaction
            {
                void Commit() {
                    {|if:#if|} Debug
                        CheckConsistency();
                        {|else:#if|} Trace
                            WriteToLog(this.ToString());
                        {|else:#else|}
                            Exit();
                        {|else:#endif|}
                    {|if:#endif|}
                    CommitHelper();
                }
            }
            """,
            out var text,
            out IDictionary<string, ImmutableArray<TextSpan>> spans);

        await TestServices.Editor.SetTextAsync(text, HangMitigatingCancellationToken);

        await VerifyAsync("#if", spans["if"], HangMitigatingCancellationToken);
        await VerifyAsync("#else", spans["else"], HangMitigatingCancellationToken);
        await VerifyAsync("#endif", spans["else"], HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task PreprocessorRegions()
    {
        MarkupTestFile.GetSpans("""

            class C
            {
                [|#region|] Main
                static void Main()
                {
                }
                [|#endregion|]
            }
            """, out var text, out var spans);

        await TestServices.Editor.SetTextAsync(text, HangMitigatingCancellationToken);

        await VerifyAsync("#region", spans, HangMitigatingCancellationToken);
        await VerifyAsync("#endregion", spans, HangMitigatingCancellationToken);
    }

    private async Task VerifyAsync(string marker, ImmutableArray<TextSpan> expectedCount, CancellationToken cancellationToken)
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
        Assert.Equal(expectedCount.AsEnumerable(), tagSpans.AsEnumerable());
    }
}
