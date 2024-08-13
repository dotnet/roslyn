// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Text
{
    public class CompositeTextTests
    {
        [Theory]
        [InlineData(["a", "b"])]
        [InlineData(["a", "b", "c", "d", "e", "f"])]
        [InlineData(["aa", "bb", "cc", "dd", "ee", "ff"])]
        [InlineData(["a\r\n", "b"])]
        [InlineData(["a", "\r\nb"])]
        [InlineData(["\r\na\r\n", "\r\nb\r\n"])]
        [InlineData(["\r\n\r\na", "b", "c", "d\r\n\r\n"])]
        [InlineData(["a\r", "\nb"])]
        [InlineData(["a\r", "\nb\r", "\nc"])]
        [InlineData(["a\n", "\nb\n", "\nc"])]
        [InlineData(["a\r", "\rb\r", "\rc"])]
        public void CompositeTextLinesEqualSourceTextLines(params string[] sourceTextsContents)
        {
            var (sourceText, compositeText) = CreateSourceAndCompositeTexts(sourceTextsContents);
            var sourceLinesText = GetLinesTexts(sourceText.Lines);
            var compositeLinesText = GetLinesTexts(compositeText.Lines);

            Assert.True(sourceLinesText.SequenceEqual(compositeLinesText));
        }

        [Theory]
        [InlineData(["a", "b"])]
        [InlineData(["a", "b", "c", "d", "e", "f"])]
        [InlineData(["aa", "bb", "cc", "dd", "ee", "ff"])]
        [InlineData(["a\r\n", "b"])]
        [InlineData(["a", "\r\nb"])]
        [InlineData(["\r\na\r\n", "\r\nb\r\n"])]
        [InlineData(["\r\n\r\na", "b", "c", "d\r\n\r\n"])]
        [InlineData(["a\r", "\nb"])]
        [InlineData(["a\r", "\nb\r", "\nc"])]
        [InlineData(["a\n", "\nb\n", "\nc"])]
        [InlineData(["a\r", "\rb\r", "\rc"])]
        public void CompositeTextIndexOfEqualSourceTextIndexOf(params string[] sourceTextsContents)
        {
            var (sourceText, compositeText) = CreateSourceAndCompositeTexts(sourceTextsContents);

            for (var i = 0; i < sourceText.Length; i++)
            {
                Assert.Equal(sourceText.Lines.IndexOf(i), compositeText.Lines.IndexOf(i));
            }
        }

        private IEnumerable<string> GetLinesTexts(TextLineCollection textLines)
        {
            return textLines.Select(l => l.Text!.ToString(l.SpanIncludingLineBreak));
        }

        private (SourceText, CompositeText) CreateSourceAndCompositeTexts(string[] contents)
        {
            var texts = ArrayBuilder<SourceText>.GetInstance();
            texts.AddRange(contents.Select(static s => SourceText.From(s)));

            var sourceText = SourceText.From(String.Join(string.Empty, contents));
            var compositeText = (CompositeText)CompositeText.ToSourceText(texts, sourceText, adjustSegments: false);

            return (sourceText, compositeText);
        }
    }
}
