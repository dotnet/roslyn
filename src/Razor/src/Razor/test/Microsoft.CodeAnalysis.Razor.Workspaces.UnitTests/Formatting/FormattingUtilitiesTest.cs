// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

public class FormattingUtilitiesTest
{
    [Theory]
    [InlineData(1, 4, 0)]
    [InlineData(2, 4, 0)]
    [InlineData(3, 4, 0)]
    [InlineData(1, 4, 1)]
    [InlineData(2, 4, 2)]
    [InlineData(3, 4, 3)]
    [InlineData(4, 8, 6)]
    public void GetIndentationLevel_Spaces(int level, int tabSize, int additional)
    {
        var input = new string(' ', level * tabSize + additional);
        var text = SourceText.From(input);

        var actual = FormattingUtilities.GetIndentationLevel(text.Lines[0], text.Length, insertSpaces: true, tabSize, out var additionalIndentation);

        Assert.Equal(level, actual);
        Assert.Equal(additional, additionalIndentation);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 6)]
    public void GetIndentationLevel_Tabs(int level, int additional)
    {
        var input = new string('\t', level) + new string(' ', additional);
        var text = SourceText.From(input);

        var actual = FormattingUtilities.GetIndentationLevel(text.Lines[0], text.Length, insertSpaces: false, tabSize: 4, out var additionalIndentation);

        Assert.Equal(level, actual);
        Assert.Equal(additional, additionalIndentation);
    }

    [Theory]
    [InlineData(0, true, 4, "")]
    [InlineData(4, true, 4, "    ")]
    [InlineData(8, true, 4, "        ")]
    [InlineData(0, false, 4, "")]
    [InlineData(4, false, 4, "\t")]
    [InlineData(8, false, 4, "\t\t")]
    [InlineData(6, false, 4, "\t  ")]
    [InlineData(5, false, 4, "\t ")]
    [InlineData(3, false, 4, "   ")]
    public void GetIndentationString_ReturnsExpectedString(int indentation, bool insertSpaces, int tabSize, string expected)
    {
        var actual = FormattingUtilities.GetIndentationString(indentation, insertSpaces, tabSize);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(4, true, 4)]
    [InlineData(4, false, 4)]
    [InlineData(6, false, 4)]
    [InlineData(3, false, 10)]
    public void GetIndentationString_CachedValuesReturnSameInstance(int indentation, bool insertSpaces, int tabSize)
    {
        var result1 = FormattingUtilities.GetIndentationString(indentation, insertSpaces, tabSize);
        var result2 = FormattingUtilities.GetIndentationString(indentation, insertSpaces, tabSize);

        Assert.Same(result1, result2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FormattingIndentCache_GetIndentString_InvalidTabSize_Throws(int tabSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IndentCache.GetIndentString(size: 4, insertSpaces: false, tabSize));
    }

    [Theory]
    [InlineData("0123456789")]
    [InlineData("0 1 2 3 4  56789")]
    [InlineData("01234\r\n    56789")]
    [InlineData("012345\n    6789")]
    [InlineData("\t\t\t012345\r\n\t\t\t6789       ")]
    public void CountNonWhitespaceCharacters(string input)
    {
        var text = SourceText.From(input);
        Assert.Equal(10, FormattingUtilities.CountNonWhitespaceChars(text, 0, text.Lines[^1].End));
    }

    [Fact]
    public void ContentEqualIgnoringWhitespace()
    {
        TestCode input1 = """
            public class C
            {
                [|public void M() { }|]
            }
            """;

        TestCode input2 = """
            public class C
            {
                [|public void M()
                {
                }|]
            }
            """;

        Assert.True(SourceText.From(input1.Text).NonWhitespaceContentEquals(SourceText.From(input2.Text),
            input1.Span.Start, input1.Span.End,
            input2.Span.Start, input2.Span.End));
    }

    [Fact]
    public void ContentEqualIgnoringWhitespace_ChangedCode()
    {
        TestCode input1 = """
            public class C
            {
                [|public void M() { }|]
            }
            """;

        TestCode input2 = """
            public class C
            {
                [|public void M()|]
            }
            """;

        Assert.False(SourceText.From(input1.Text).NonWhitespaceContentEquals(SourceText.From(input2.Text),
            input1.Span.Start, input1.Span.End,
            input2.Span.Start, input2.Span.End));
    }
}
