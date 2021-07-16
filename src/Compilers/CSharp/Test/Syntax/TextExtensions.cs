// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public static class TextExtensions
    {
        public static void ShouldBe<T>(this T actual, T expected)
        {
            Assert.Equal(expected, actual);
        }

        public static SourceText WithReplace(this SourceText text, int offset, int length, string newText)
        {
            var oldFullText = text.ToString();
            var span = new TextSpan(offset, length);
            var newFullText = oldFullText.Substring(0, offset) + newText + oldFullText.Substring(span.End);
            return SourceText.From(newFullText);
        }

        public static SourceText WithReplaceFirst(this SourceText text, string oldText, string newText)
        {
            var oldFullText = text.ToString();
            int offset = oldFullText.IndexOf(oldText, StringComparison.Ordinal);
            int length = oldText.Length;
            var span = new TextSpan(offset, length);
            var newFullText = oldFullText.Substring(0, offset) + newText + oldFullText.Substring(span.End);
            return SourceText.From(newFullText);
        }

        public static SourceText WithReplace(this SourceText text, int startIndex, string oldText, string newText)
        {
            var oldFullText = text.ToString();
            int offset = oldFullText.IndexOf(oldText, startIndex, StringComparison.Ordinal); // Use an offset to find the first element to replace at
            int length = oldText.Length;
            var span = new TextSpan(offset, length);
            var newFullText = oldFullText.Substring(0, offset) + newText + oldFullText.Substring(span.End);
            return SourceText.From(newFullText);
        }

        public static SourceText WithInsertAt(this SourceText text, int offset, string newText)
        {
            return WithReplace(text, offset, 0, newText);
        }

        public static SourceText WithInsertBefore(this SourceText text, string existingText, string newText)
        {
            var oldFullText = text.ToString();
            int offset = oldFullText.IndexOf(existingText, StringComparison.Ordinal);
            var span = new TextSpan(offset, 0);
            var newFullText = oldFullText.Substring(0, offset) + newText + oldFullText.Substring(offset);
            return SourceText.From(newFullText);
        }

        public static SourceText WithRemoveAt(this SourceText text, int offset, int length)
        {
            return WithReplace(text, offset, length, string.Empty);
        }

        public static SourceText WithRemoveFirst(this SourceText text, string oldText)
        {
            return WithReplaceFirst(text, oldText, string.Empty);
        }
    }
}
