// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test;

public class FindTokenTests
{
    private const string PositionMarker = "$$";

    private static (RazorSyntaxTree Tree, int Position) ParseWithPosition(string textWithPosition)
    {
        var position = textWithPosition.IndexOf(PositionMarker, StringComparison.Ordinal);
        if (position == -1)
        {
            throw new ArgumentException("The text must contain a '$$' character to indicate the position to find the token at.", nameof(textWithPosition));
        }

        var text = textWithPosition.Remove(position, PositionMarker.Length);
        var tree = Parse(text);
        return (tree, position);
    }

    private static RazorSyntaxTree Parse(string text)
    {
        return RazorSyntaxTree.Parse(RazorSourceDocument.Create(text, System.Text.Encoding.Default, RazorSourceDocumentProperties.Default));
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7505")]
    public void ReturnsEofOnFileEnd()
    {
        var text = "<div></div>$$";
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""EndOfFile;[];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7505")]
    public void ReturnsEofOnFileEnd_WithTrailingTrivia()
    {
        var text = """
            <div></div>
            $$

            """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""EndOfFile;[];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/9040")]
    public void LeadingWhitespace_BeforeAnyNode()
    {
        var text = """
        $$ <Component></Component>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""OpenAngle;[<];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void ReturnsOpenAngle()
    {
        var text = "$$<div></div>";
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""OpenAngle;[<];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Theory]
    [InlineData("<$$div></div>")]
    [InlineData("<d$$iv></div>")]
    [InlineData("<di$$v></div>")]
    public void ReturnsStartDivTag(string text)
    {
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Text;[div];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void ReturnsCloseAngle()
    {
        var text = "<div$$></div>";
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""CloseAngle;[>];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7630")]
    public void ReturnsEof_AfterVoidTag()
    {
        var text = "<input>$$";
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""EndOfFile;[];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7630")]
    public void ReturnsCloseAngle_AfterVoidTagWithTrailingSpace()
    {
        var text = "<input>$$ ";
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""CloseAngle;[>];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_01()
    {
        var text = """
        $$@if (true)
        {
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Transition;[@];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_02()
    {
        var text = """
        @$$if (true)
        {
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Keyword;[if];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_03_IgnoreWhitespace()
    {
        var text = """
        @if$$ (true)
        {
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Keyword;[if];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_03_IncludeWhitespace()
    {
        var text = """
        @if$$ (true)
        {
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position, includeWhitespace: true);

        AssertEx.Equal("""Whitespace;[ ];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_04()
    {
        var text = """
        @if $$(true)
        {
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""LeftParenthesis;[(];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_05()
    {
        var text = """
        @if ($$true)
        {
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Keyword;[true];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_06()
    {
        var text = """
        @if (true$$)
        {
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RightParenthesis;[)];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_07_IgnoreWhitespace()
    {
        var text = """
        @if (true)$$
        {
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RightParenthesis;[)];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_07_IncludeWhitespace()
    {
        var text = """
        @if (true)$$
        {
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position, includeWhitespace: true);

        AssertEx.Equal("""NewLine;[LF];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_08()
    {
        var text = """
        @if (true)
        $${
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""LeftBrace;[{];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_09()
    {
        var text = """
        @if (true)
        {
            $$
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RightBrace;[}];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_10()
    {
        var text = """
        @if (true)
        {
        $$}
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RightBrace;[}];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_11()
    {
        var text = """
        <div attr=$$@value />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Transition;[@];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_12()
    {
        var text = """
        <div attr=@$$value />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Identifier;[value];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_12_IgnoreWhitespace()
    {
        var text = """
        <div attr=@value$$ />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Identifier;[value];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_12_IncludeWhitespace()
    {
        var text = """
        <div attr=@value$$ />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position, includeWhitespace: true);

        AssertEx.Equal("""Whitespace;[ ];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_13()
    {
        var text = """
        <div attr=$$@(value) />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Transition;[@];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_14()
    {
        var text = """
        <div attr=@$$(value) />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""LeftParenthesis;[(];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_15()
    {
        var text = """
        <div attr=@($$value) />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Identifier;[value];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_16()
    {
        var text = """
        <div attr=@(value$$) />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RightParenthesis;[)];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_17_IgnoreWhitespace()
    {
        var text = """
        <div attr=@(value)$$ />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RightParenthesis;[)];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void CSharpTransition_17_IncludeWhitespace()
    {
        var text = """
        <div attr=@(value)$$ />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position, includeWhitespace: true);

        AssertEx.Equal("""Whitespace;[ ];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void HtmlTransition_01()
    {
        var text = """
        @if (true)
        {
        $$    <div />
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""OpenAngle;[<];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Theory, WorkItem("https://github.com/dotnet/razor/issues/7630")]
    [InlineData("div")]
    [InlineData("div /")]
    [InlineData("input")]
    public void HtmlTransition_02(string tagContent)
    {
        var text = $$"""
        @if (true)
        {
            <{{tagContent}}>$$
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""CloseAngle;[>];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7630")]
    public void HtmlTransition_03()
    {
        var text = """
        @if (true)
        {
            <div><em>$$</div>
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""OpenAngle;[<];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7630")]
    public void HtmlTransition_04()
    {
        var text = """
        @if (true)
        {
            <div><em$$></div>
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""CloseAngle;[>];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Theory, WorkItem("https://github.com/dotnet/razor/issues/7505")]
    [CombinatorialData]
    public void HtmlTransition_05(bool withComment)
    {
        var commentText = withComment ?
        """

                <!--asdfasd-->
        """
        : "";
        var text = $$"""
        @foreach (var num in Enumerable.Range(1, 10))
        {
            <span class="skill_result btn">{{commentText}}
                $$<span style="margin-left:0px">
                    <svg>
                        <rect width="1" height="1" />
                    </svg>
                </span>
            </span>
        }
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""OpenAngle;[<];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_BeforeNewline_01()
    {
        var text = """
        <div>    $$

        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""CloseAngle;[>];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_BeforeNewline_02()
    {
        var text = """
        <div>   asdf  $$
        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Text;[asdf];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_AfterNewline()
    {
        var text = """
        <div>
        $$
        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""OpenAngle;[<];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_InsideTag_01()
    {
        var text = """
        <div $$ Attribute = "value">
        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Text;[div];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_InsideTag_02()
    {
        var text = """
        <div Attribute $$ = "value">
        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Text;[Attribute];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_InsideTag_03()
    {
        var text = """
        <div Attribute = $$ "value">
        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Equals;[=];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_InsideTag_04()
    {
        var text = """
        <div Attribute =
            $$ "value">
        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""DoubleQuote;["];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_InsideTag_05()
    {
        var text = """
        <div Attribute = " $$ value" >
        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""DoubleQuote;["];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_InsideTag_06()
    {
        var text = """
        <div Attribute = "value $$ " >
        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Text;[value];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_InsideTag_07()
    {
        var text = """
        <div Attribute = "value" $$ >
        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""DoubleQuote;["];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_InsideTag_08()
    {
        var text = """
        <div Attribute = "value" $$>
        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""CloseAngle;[>];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void IgnoreWhitespace_InsideTag_09()
    {
        var text = """
        <div Attribute = "value"
        $$ >
        </div>
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""CloseAngle;[>];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void HtmlComment_01()
    {
        var text = """
        $$<!-- Comment -->
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""OpenAngle;[<];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void HtmlComment_02()
    {
        var text = """
        <$$!-- Comment -->
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Bang;[!];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void HtmlComment_03()
    {
        var text = """
        <!$$-- Comment -->
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""DoubleHyphen;[--];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void HtmlComment_04_IgnoreWhitespace()
    {
        var text = """
        <!--$$ Comment -->
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""DoubleHyphen;[--];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void HtmlComment_04_IncludeWhitespace()
    {
        var text = """
        <!--$$ Comment -->
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position, includeWhitespace: true);

        AssertEx.Equal("""Whitespace;[ ];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void HtmlComment_05()
    {
        var text = """
        <!-- $$Comment -->
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Text;[Comment];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void HtmlComment_06()
    {
        var text = """
        <!-- Comment$$ -->
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""Text;[Comment];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void HtmlComment_07()
    {
        var text = """
        <!-- Comment $$-->
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""DoubleHyphen;[--];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void HtmlComment_08()
    {
        var text = """
        <!-- Comment --$$>
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""CloseAngle;[>];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void HtmlComment_09()
    {
        var text = """
        <!-- Comment -->$$
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""CloseAngle;[>];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void RazorComment_01()
    {
        var text = """
        $$@* Comment *@
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RazorCommentTransition;[@];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void RazorComment_02()
    {
        var text = """
        @$$* Comment *@
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RazorCommentStar;[*];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void RazorComment_03_IgnoreWhitespace()
    {
        var text = """
        @*$$ Comment *@
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RazorCommentLiteral;[ Comment ];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void RazorComment_03_IncludeWhitespace()
    {
        var text = """
        @*$$ Comment *@
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position, includeWhitespace: true);

        AssertEx.Equal("""RazorCommentLiteral;[ Comment ];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void RazorComment_04()
    {
        var text = """
        @* $$Comment *@
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RazorCommentLiteral;[ Comment ];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void RazorComment_05()
    {
        var text = """
        @* Comment$$ *@
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RazorCommentLiteral;[ Comment ];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void RazorComment_06()
    {
        var text = """
        @* Comment $$*@
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RazorCommentStar;[*];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void RazorComment_07()
    {
        var text = """
        @* Comment *$$@
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RazorCommentTransition;[@];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void RazorComment_08()
    {
        var text = """
        @* Comment *@$$
        <div />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.FindToken(position);

        AssertEx.Equal("""RazorCommentTransition;[@];""", TestSyntaxSerializer.Serialize(token).Trim());
    }

    [Fact]
    public void OutOfRange_NoRangeInDocument()
    {
        var text = """
        <div />$$
        """;
        var (tree, position) = ParseWithPosition(text);

        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => tree.Root.FindToken(-1));
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => tree.Root.FindToken(position + 1));
    }

    [Fact]
    public void OutOfRange_NoRangeInWhitespaceNode_NoNewline()
    {
        var text = """
        <div $$ />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.DescendantTokens().Single(t => t.Kind == SyntaxKind.Whitespace);
        var parent = token.Parent;
        Assert.NotNull(parent);
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => parent.FindToken(position, includeWhitespace: false));
        Assert.Equal(token, parent.FindToken(position, includeWhitespace: true));
    }

    [Fact]
    public void OutOfRange_NoRangeInWhitespaceNode_AfterNewline()
    {
        var text = """
        <div
         $$ />
        """;
        var (tree, position) = ParseWithPosition(text);

        var token = tree.Root.DescendantTokens().Last(t => t.Kind == SyntaxKind.Whitespace);
        var parent = token.Parent;
        Assert.NotNull(parent);
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => parent.FindToken(position, includeWhitespace: false));
        Assert.Equal(token, parent.FindToken(position, includeWhitespace: true));
    }
}
