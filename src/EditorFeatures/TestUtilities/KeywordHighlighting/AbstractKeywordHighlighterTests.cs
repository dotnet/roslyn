// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting
{
    [UseExportProvider]
    public abstract class AbstractKeywordHighlighterTests
    {
        internal abstract IHighlighter CreateHighlighter();
        protected abstract IEnumerable<ParseOptions> GetOptions();
        protected abstract TestWorkspace CreateWorkspaceFromFile(string code, ParseOptions options);

        protected async Task TestAsync(
            string code)
        {
            foreach (var option in GetOptions())
            {
                await TestAsync(code, option);
            }
        }

        private async Task TestAsync(
            string markup,
            ParseOptions options,
            bool optionIsEnabled = true)
        {
            using (var workspace = CreateWorkspaceFromFile(markup, options))
            {
                var testDocument = workspace.Documents.Single();
                var expectedHighlightSpans = testDocument.SelectedSpans ?? new List<TextSpan>();
                expectedHighlightSpans = Sort(expectedHighlightSpans);
                var cursorSpan = testDocument.AnnotatedSpans["Cursor"].Single();
                var textSnapshot = testDocument.TextBuffer.CurrentSnapshot;
                var document = workspace.CurrentSolution.GetDocument(testDocument.Id);

                // If the position being tested is immediately following the keyword, 
                // we should get the token before this position to find the appropriate 
                // ancestor node.
                var highlighter = CreateHighlighter();

                var root = await document.GetSyntaxRootAsync();

                for (var i = 0; i <= cursorSpan.Length; i++)
                {
                    var position = cursorSpan.Start + i;
                    var highlightSpans = highlighter.GetHighlights(root, position, CancellationToken.None).ToList();
                    highlightSpans = Sort(highlightSpans);

                    CheckSpans(root.SyntaxTree, expectedHighlightSpans, highlightSpans);
                }
            }
        }

        private static void CheckSpans(SyntaxTree tree, IList<TextSpan> expectedHighlightSpans, List<TextSpan> highlightSpans)
        {
            for (var j = 0; j < Math.Max(highlightSpans.Count, expectedHighlightSpans.Count); j++)
            {
                if (j >= expectedHighlightSpans.Count)
                {
                    var actualLineSpan = tree.GetLineSpan(highlightSpans[j]).Span;
                    var actualText = tree.GetText().ToString(highlightSpans[j]);
                    Assert.False(true, $"Unexpected highlight at {actualLineSpan}: '{actualText}'");
                }
                else if (j >= highlightSpans.Count)
                {
                    var expectedLineSpan = tree.GetLineSpan(expectedHighlightSpans[j]).Span;
                    var expectedText = tree.GetText().ToString(expectedHighlightSpans[j]);
                    Assert.False(true, $"Missing highlight at {expectedLineSpan}: '{expectedText}'");
                }

                var expected = expectedHighlightSpans[j];
                var actual = highlightSpans[j];

                if (actual != expected)
                    Assert.Equal(tree.GetLineSpan(expected).Span, tree.GetLineSpan(actual).Span);
            }
        }

        private List<TextSpan> Sort(IEnumerable<TextSpan> spans)
        {
            return spans.OrderBy((s1, s2) => s1.Start - s2.Start).ToList();
        }
    }
}
