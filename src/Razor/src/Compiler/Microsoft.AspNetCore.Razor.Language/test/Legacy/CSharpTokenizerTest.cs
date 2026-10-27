// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Roslyn.Test.Utilities;
using Xunit;

using CSharpParseOptions = Microsoft.CodeAnalysis.CSharp.CSharpParseOptions;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpTokenizerTest : CSharpTokenizerTestBase
{
    private new SyntaxToken IgnoreRemaining => (SyntaxToken)base.IgnoreRemaining;

    [Fact]
    public void Next_Returns_Null_When_EOF_Reached()
    {
        TestTokenizer("");
    }

    [Fact]
    public void Next_Returns_Newline_Token_For_Single_CR()
    {
        TestTokenizer(
            "\r\ra",
            SyntaxFactory.Token(SyntaxKind.NewLine, "\r"),
            SyntaxFactory.Token(SyntaxKind.NewLine, "\r"),
            IgnoreRemaining);
    }

    [Fact]
    public void Next_Returns_Newline_Token_For_Single_LF()
    {
        TestTokenizer(
            "\n\na",
            SyntaxFactory.Token(SyntaxKind.NewLine, "\n"),
            SyntaxFactory.Token(SyntaxKind.NewLine, "\n"),
            IgnoreRemaining);
    }

    [Fact]
    public void Next_Returns_Newline_Token_For_Single_NEL()
    {
        // NEL: Unicode "Next Line" U+0085
        TestTokenizer(
            "\u0085\u0085a",
            SyntaxFactory.Token(SyntaxKind.NewLine, "\u0085"),
            SyntaxFactory.Token(SyntaxKind.NewLine, "\u0085"),
            IgnoreRemaining);
    }

    [Fact]
    public void Next_Returns_Newline_Token_For_Single_Line_Separator()
    {
        // Unicode "Line Separator" U+2028
        TestTokenizer(
            "\u2028\u2028a",
            SyntaxFactory.Token(SyntaxKind.NewLine, "\u2028"),
            SyntaxFactory.Token(SyntaxKind.NewLine, "\u2028"),
            IgnoreRemaining);
    }

    [Fact]
    public void Next_Returns_Newline_Token_For_Single_Paragraph_Separator()
    {
        // Unicode "Paragraph Separator" U+2029
        TestTokenizer(
            "\u2029\u2029a",
            SyntaxFactory.Token(SyntaxKind.NewLine, "\u2029"),
            SyntaxFactory.Token(SyntaxKind.NewLine, "\u2029"),
            IgnoreRemaining);
    }

    [Fact]
    public void Next_Returns_Single_Newline_Token_For_CRLF()
    {
        TestTokenizer(
            "\r\n\r\na",
            SyntaxFactory.Token(SyntaxKind.NewLine, "\r\n"),
            SyntaxFactory.Token(SyntaxKind.NewLine, "\r\n"),
            IgnoreRemaining);
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public void Reset_After_Newline_Preserves_Line_State(string lineEnding)
    {
        using var reader = new SeekableTextReader("a" + lineEnding + "b", "test.cs");
        using var tokenizer = new RoslynCSharpTokenizer(reader, CSharpParseOptions.Default);
        tokenizer.StartingBlock();

        Assert.Equal("a", tokenizer.NextToken()?.Content);
        Assert.Equal(lineEnding, tokenizer.NextToken()?.Content);
        Assert.Equal("b", tokenizer.NextToken()?.Content);

        reader.Position = 1 + lineEnding.Length;
        tokenizer.Reset(reader.Position);

        Assert.Equal("b", tokenizer.NextToken()?.Content);
        Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void Next_Returns_Token_For_Whitespace_Characters()
    {
        TestTokenizer(
            " \f\t\u000B \n ",
            SyntaxFactory.Token(SyntaxKind.Whitespace, " \f\t\u000B "),
            SyntaxFactory.Token(SyntaxKind.NewLine, "\n"),
            SyntaxFactory.Token(SyntaxKind.Whitespace, " "));
    }

    [Fact]
    public void Transition_Is_Recognized()
    {
        TestSingleToken("@", SyntaxKind.Transition);
    }

    [Fact]
    public void Transition_Is_Recognized_As_SingleCharacter()
    {
        TestTokenizer(
            "@(",
            SyntaxFactory.Token(SyntaxKind.Transition, "@"),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis, "("));
    }
}
