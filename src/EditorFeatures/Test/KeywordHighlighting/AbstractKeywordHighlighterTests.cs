// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting
{
    public abstract class AbstractKeywordHighlighterTests
    {
        internal abstract IHighlighter CreateHighlighter();
        protected abstract IEnumerable<ParseOptions> GetOptions();
        protected abstract Task<TestWorkspace> CreateWorkspaceFromFileAsync(string code, ParseOptions options);

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
            using (var workspace = await CreateWorkspaceFromFileAsync(markup, options))
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

                var root = document.GetSyntaxRootAsync().Result;

                for (int i = 0; i <= cursorSpan.Length; i++)
                {
                    var position = cursorSpan.Start + i;
                    var highlightSpans = highlighter.GetHighlights(root, position, CancellationToken.None).ToList();
                    highlightSpans = Sort(highlightSpans);

                    CheckSpans(expectedHighlightSpans, highlightSpans);
                }
            }
        }

        private static void CheckSpans(IList<TextSpan> expectedHighlightSpans, List<TextSpan> highlightSpans)
        {
            for (int j = 0; j < Math.Max(highlightSpans.Count, expectedHighlightSpans.Count); j++)
            {
                if (j >= expectedHighlightSpans.Count)
                {
                    Assert.False(true, "Unexpected highlight: " + highlightSpans[j].ToString());
                }
                else if (j >= highlightSpans.Count)
                {
                    Assert.False(true, "Missing highlight for: " + expectedHighlightSpans[j].ToString());
                }

                var expected = expectedHighlightSpans[j];
                var actual = highlightSpans[j];

                Assert.Equal(expected, actual);
            }
        }

        private List<TextSpan> Sort(IEnumerable<TextSpan> spans)
        {
            return spans.OrderBy((s1, s2) => s1.Start - s2.Start).ToList();
        }
    }
}
