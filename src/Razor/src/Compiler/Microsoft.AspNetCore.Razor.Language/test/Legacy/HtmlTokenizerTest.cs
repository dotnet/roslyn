// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class HtmlTokenizerTest : HtmlTokenizerTestBase
{
    [Fact]
    public void Next_Returns_Null_When_EOF_Reached()
    {
        TestTokenizer("");
    }

    [Fact]
    public void Text_Is_Recognized()
    {
        TestTokenizer("foo-9309&smlkmb;::-3029022,.sdkq92384",
                      SyntaxFactory.Token(SyntaxKind.Text, "foo-9309&smlkmb;::-3029022,.sdkq92384"));
    }

    [Fact]
    public void Whitespace_Is_Recognized()
    {
        TestTokenizer(" \t\f ",
                      SyntaxFactory.Token(SyntaxKind.Whitespace, " \t\f "));
    }

    [Fact]
    public void Newline_Is_Recognized()
    {
        TestTokenizer("\n\r\r\n",
                      SyntaxFactory.Token(SyntaxKind.NewLine, "\n"),
                      SyntaxFactory.Token(SyntaxKind.NewLine, "\r"),
                      SyntaxFactory.Token(SyntaxKind.NewLine, "\r\n"));
    }

    [Fact]
    public void Transition_Is_Not_Recognized_Mid_Text_If_Surrounded_By_Alphanumeric_Characters()
    {
        TestSingleToken("foo@bar", SyntaxKind.Text);
    }

    [Fact]
    public void OpenAngle_Is_Recognized()
    {
        TestSingleToken("<", SyntaxKind.OpenAngle);
    }

    [Fact]
    public void Bang_Is_Recognized()
    {
        TestSingleToken("!", SyntaxKind.Bang);
    }

    [Fact]
    public void Solidus_Is_Recognized()
    {
        TestSingleToken("/", SyntaxKind.ForwardSlash);
    }

    [Fact]
    public void QuestionMark_Is_Recognized()
    {
        TestSingleToken("?", SyntaxKind.QuestionMark);
    }

    [Fact]
    public void LeftBracket_Is_Recognized()
    {
        TestSingleToken("[", SyntaxKind.LeftBracket);
    }

    [Fact]
    public void CloseAngle_Is_Recognized()
    {
        TestSingleToken(">", SyntaxKind.CloseAngle);
    }

    [Fact]
    public void RightBracket_Is_Recognized()
    {
        TestSingleToken("]", SyntaxKind.RightBracket);
    }

    [Fact]
    public void Equals_Is_Recognized()
    {
        TestSingleToken("=", SyntaxKind.Equals);
    }

    [Fact]
    public void DoubleQuote_Is_Recognized()
    {
        TestSingleToken("\"", SyntaxKind.DoubleQuote);
    }

    [Fact]
    public void SingleQuote_Is_Recognized()
    {
        TestSingleToken("'", SyntaxKind.SingleQuote);
    }

    [Fact]
    public void Transition_Is_Recognized()
    {
        TestSingleToken("@", SyntaxKind.Transition);
    }

    [Theory]
    [InlineData("@value")]
    [InlineData("@@value")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public void IgnoredRazorTransitionsRemainLiteralAfterReset(string text)
    {
        using var source = new SeekableTextReader(text, filePath: null);
        using var tokenizer = new TokenizerView<HtmlTokenizer>(new HtmlTokenizer(source) { IgnoreRazorTransitions = true });

        Assert.True(tokenizer.Next());
        Assert.Equal(SyntaxKind.Text, tokenizer.Current.Kind);
        Assert.Equal(text, tokenizer.Current.Content);
        Assert.False(tokenizer.Next());

        tokenizer.Reset(0);
        Assert.True(tokenizer.Next());
        Assert.Equal(SyntaxKind.Text, tokenizer.Current.Kind);
        Assert.Equal(text, tokenizer.Current.Content);

        tokenizer.Tokenizer.IgnoreRazorTransitions = false;
        tokenizer.Reset(0);
        Assert.True(tokenizer.Next());
        Assert.Equal("@", tokenizer.Current.Content);
        Assert.Equal(SyntaxKind.Transition, tokenizer.Current.Kind);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public void IgnoredRazorCommentsRemainLiteralAfterReset()
    {
        const string text = "@*value*@";
        using var source = new SeekableTextReader(text, filePath: null);
        using var tokenizer = new TokenizerView<HtmlTokenizer>(new HtmlTokenizer(source) { IgnoreRazorTransitions = true });

        Assert.True(tokenizer.Next());
        Assert.Equal(SyntaxKind.Text, tokenizer.Current.Kind);
        Assert.Equal(text, tokenizer.Current.Content);
        Assert.False(tokenizer.Next());

        tokenizer.Reset(0);
        Assert.True(tokenizer.Next());
        Assert.Equal(SyntaxKind.Text, tokenizer.Current.Kind);
        Assert.Equal(text, tokenizer.Current.Content);

        tokenizer.Tokenizer.IgnoreRazorTransitions = false;
        tokenizer.Reset(0);
        Assert.True(tokenizer.Next());
        Assert.Equal("@", tokenizer.Current.Content);
        Assert.Equal(SyntaxKind.RazorCommentTransition, tokenizer.Current.Kind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public void LeadingStarsAreText(bool ignoreRazorTransitions)
    {
        using var source = new SeekableTextReader("\n  *** <summary>", filePath: null);
        using var tokenizer = new HtmlTokenizer(source) { IgnoreRazorTransitions = ignoreRazorTransitions };

        Assert.Equal(SyntaxKind.NewLine, tokenizer.NextToken().Kind);
        Assert.Equal("  ", tokenizer.NextToken().Content);
        var stars = tokenizer.NextToken();
        Assert.Equal(SyntaxKind.Text, stars.Kind);
        Assert.Equal("***", stars.Content);
        Assert.Equal(" ", tokenizer.NextToken().Content);
        Assert.Equal(SyntaxKind.OpenAngle, tokenizer.NextToken().Kind);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public void StarFollowedBySlashIsText()
    {
        using var source = new SeekableTextReader("\n*/", filePath: null);
        using var tokenizer = new HtmlTokenizer(source);

        Assert.Equal(SyntaxKind.NewLine, tokenizer.NextToken().Kind);
        var star = tokenizer.NextToken();
        Assert.Equal(SyntaxKind.Text, star.Kind);
        Assert.Equal("*", star.Content);
        Assert.Equal(SyntaxKind.ForwardSlash, tokenizer.NextToken().Kind);
        Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public void IgnoringRazorTransitionsPreservesLeadingStars()
    {
        using var source = new SeekableTextReader("\n* @value", filePath: null);
        using var tokenizer = new HtmlTokenizer(source) { IgnoreRazorTransitions = true };

        Assert.Equal(SyntaxKind.NewLine, tokenizer.NextToken().Kind);
        var star = tokenizer.NextToken();
        Assert.Equal(SyntaxKind.Text, star.Kind);
        Assert.Equal("*", star.Content);
        Assert.Equal(SyntaxKind.Whitespace, tokenizer.NextToken().Kind);
        var text = tokenizer.NextToken();
        Assert.Equal(SyntaxKind.Text, text.Kind);
        Assert.Equal("@value", text.Content);
        Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void DoubleHyphen_Is_Recognized()
    {
        TestSingleToken("--", SyntaxKind.DoubleHyphen);
    }

    [Fact]
    public void SingleHyphen_Is_Not_Recognized()
    {
        TestSingleToken("-", SyntaxKind.Text);
    }

    [Fact]
    public void SingleHyphen_Mid_Text_Is_Not_Recognized_As_Separate_Token()
    {
        TestSingleToken("foo-bar", SyntaxKind.Text);
    }

    [Fact]
    public void Next_Ignores_Star_At_EOF_In_RazorComment()
    {
        TestTokenizer(
            "@* Foo * Bar * Baz *",
            SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
            SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
            SyntaxFactory.Token(SyntaxKind.RazorCommentLiteral, " Foo * Bar * Baz *"));
    }

    [Fact]
    public void Next_Ignores_Star_Without_Trailing_At()
    {
        TestTokenizer(
            "@* Foo * Bar * Baz *@",
            SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
            SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
            SyntaxFactory.Token(SyntaxKind.RazorCommentLiteral, " Foo * Bar * Baz "),
            SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
            SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"));
    }

    [Fact]
    public void Next_Returns_RazorComment_Token_For_Entire_Razor_Comment()
    {
        TestTokenizer(
            "@* Foo Bar Baz *@",
            SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
            SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
            SyntaxFactory.Token(SyntaxKind.RazorCommentLiteral, " Foo Bar Baz "),
            SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
            SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"));
    }
}
