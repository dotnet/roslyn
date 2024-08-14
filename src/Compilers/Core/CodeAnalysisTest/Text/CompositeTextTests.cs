// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Text;

public sealed class CompositeTextTests
{
    [Theory]
    [InlineData("abcdefghijkl")]
    [InlineData(["\r\r\r\r\r\r\r\r\r\r\r\r"])]
    [InlineData(["\n\n\n\n\n\n\n\n\n\n\n\n"])]
    [InlineData(["\r\n\r\n\r\n\r\n\r\n\r\n"])]
    [InlineData(["\n\r\n\r\n\r\n\r\n\r\n\r"])]
    [InlineData(["a\r\nb\r\nc\r\nd\r\n"])]
    [InlineData(["\ra\n\rb\n\rc\n\rd\n"])]
    [InlineData(["\na\r\nb\r\nc\r\nd\r"])]
    [InlineData(["ab\r\ncd\r\nef\r\n"])]
    [InlineData(["ab\r\r\ncd\r\r\nef"])]
    [InlineData(["ab\n\n\rcd\n\n\ref"])]
    public void CompositeTextLinesEqualSourceTextLinesPermutations(string contents)
    {
        // Please try to limit the inputs to this method to around 12 chars or less, as much longer than that
        // will blow up the number of potential permutations.
        foreach (var (sourceText, compositeText) in CreateSourceAndCompositeTexts(contents))
        {
            var sourceLinesText = GetLinesTexts(sourceText.Lines);
            var compositeLinesText = GetLinesTexts(compositeText.Lines);

            Assert.True(sourceLinesText.SequenceEqual(compositeLinesText));

            for (var i = 0; i < sourceText.Length; i++)
            {
                Assert.Equal(sourceText.Lines.IndexOf(i), compositeText.Lines.IndexOf(i));
            }
        }
    }

    private static IEnumerable<string> GetLinesTexts(TextLineCollection textLines)
    {
        return textLines.Select(l => l.Text!.ToString(l.SpanIncludingLineBreak));
    }

    // Returns all possible permutations of contents into SourceText arrays of length between minSourceTextCount and maxSourceTextCount
    private static IEnumerable<(SourceText, CompositeText)> CreateSourceAndCompositeTexts(string contents, int minSourceTextCount = 2, int maxSourceTextCount = 4)
    {
        var sourceText = SourceText.From(contents);

        for (var sourceTextCount = minSourceTextCount; sourceTextCount <= Math.Min(maxSourceTextCount, contents.Length); sourceTextCount++)
        {
            foreach (var sourceTexts in CreateSourceTextPermutations(contents, sourceTextCount))
            {
                var sourceTextsBuilder = ArrayBuilder<SourceText>.GetInstance();
                sourceTextsBuilder.AddRange(sourceTexts);

                var compositeText = (CompositeText)CompositeText.ToSourceText(sourceTextsBuilder, sourceText, adjustSegments: false);
                yield return (sourceText, compositeText);
            }
        }
    }

    private static IEnumerable<SourceText[]> CreateSourceTextPermutations(string contents, int requestedSourceTextCount)
    {
        if (requestedSourceTextCount == 1)
        {
            yield return [SourceText.From(contents)];
        }
        else
        {
            var maximalSourceTextLength = (contents.Length - requestedSourceTextCount) + 1;
            for (int i = 1; i <= maximalSourceTextLength; i++)
            {
                var sourceText = SourceText.From(contents[..i]);
                foreach (var otherSourceTexts in CreateSourceTextPermutations(contents.Substring(i), requestedSourceTextCount - 1))
                {
                    yield return [sourceText, .. otherSourceTexts];
                }
            }
        }
    }
}
