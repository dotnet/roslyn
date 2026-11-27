// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpTokenizerCommentTest : CSharpTokenizerTestBase
{
    private new SyntaxToken IgnoreRemaining => (SyntaxToken)base.IgnoreRemaining;

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

    [Fact]
    public void Next_Returns_Comment_Token_For_Entire_Single_Line_Comment()
    {
        TestTokenizer("// Foo Bar Baz", SyntaxFactory.Token(SyntaxKind.CSharpComment, "// Foo Bar Baz"));
    }

    [Fact]
    public void Single_Line_Comment_Is_Terminated_By_Newline()
    {
        TestTokenizer("""
            // Foo Bar Baz
            a
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, "// Foo Bar Baz"), IgnoreRemaining);
    }

    [Fact]
    public void Multi_Line_Comment_In_Single_Line_Comment_Has_No_Effect()
    {
        TestTokenizer("""
            // Foo/*Bar*/ Baz
            a
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, "// Foo/*Bar*/ Baz"), IgnoreRemaining);
    }

    [Fact]
    public void Next_Returns_Comment_Token_For_Entire_Multi_Line_Comment()
    {
        TestTokenizer("""
            /* Foo
            Bar
            Baz */
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /* Foo
            Bar
            Baz */
            """));
    }

    [Fact]
    public void Multi_Line_Comment_Is_Terminated_By_End_Sequence()
    {
        TestTokenizer("""
            /* Foo
            Bar
            Baz */a
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /* Foo
            Bar
            Baz */
            """), IgnoreRemaining);
    }

    [Fact]
    public void Unterminated_Multi_Line_Comment_Captures_To_EOF()
    {
        TestTokenizer("""
            /* Foo
            Bar
            Baz
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /* Foo
            Bar
            Baz
            """), IgnoreRemaining);
    }

    [Fact]
    public void Nested_Multi_Line_Comments_Terminated_At_First_End_Sequence()
    {
        TestTokenizer("""
            /* Foo/*
            Bar
            Baz*/ */
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /* Foo/*
            Bar
            Baz*/
            """), IgnoreRemaining);
    }

    [Fact]
    public void Nested_Multi_Line_Comments_Terminated_At_Full_End_Sequence()
    {
        TestTokenizer("""
            /* Foo
            Bar
            Baz* */
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /* Foo
            Bar
            Baz* */
            """), IgnoreRemaining);
    }

    [Fact]
    public void Next_Returns_CSharpComment_Token_For_Single_Line_Documentation_Comment()
    {
        TestTokenizer("/// This is a single line documentation comment", SyntaxFactory.Token(SyntaxKind.CSharpComment, "/// This is a single line documentation comment"));
    }

    [Fact]
    public void Single_Line_Documentation_Comment_Is_Terminated_By_And_Owns_Newline()
    {
        TestTokenizer("""
            /// This is a single line documentation comment
            a
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /// This is a single line documentation comment

            """), IgnoreRemaining);
    }

    [Fact]
    public void Single_Line_Documentation_Comment_With_Multiple_Lines()
    {
        TestTokenizer("""
            /// This is a single line documentation comment
            /// with multiple lines
            /// in the comment

            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /// This is a single line documentation comment
            /// with multiple lines
            /// in the comment

            """), IgnoreRemaining);
    }

    [Fact]
    public void Next_Returns_CSharpComment_Token_For_Multi_Line_Documentation_Comment()
    {
        TestTokenizer("""
            /**
             * This is a
             * multi-line
             * documentation comment
             */
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /**
             * This is a
             * multi-line
             * documentation comment
             */
            """));
    }

    [Fact]
    public void Multi_Line_Documentation_Comment_Is_Terminated_By_End_Sequence()
    {
        TestTokenizer("""
            /**
             * This is a
             * multi-line
             * documentation comment
             */a
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /**
             * This is a
             * multi-line
             * documentation comment
             */
            """), IgnoreRemaining);
    }

    [Fact]
    public void Unterminated_Multi_Line_Documentation_Comment_Captures_To_EOF()
    {
        TestTokenizer("""
            /**
             * This is a
             * multi-line
             * documentation comment
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /**
             * This is a
             * multi-line
             * documentation comment
            """), IgnoreRemaining);
    }

    [Fact]
    public void Nested_Multi_Line_Documentation_Comments_Terminated_At_First_End_Sequence()
    {
        TestTokenizer("""
            /**
             * This is a
             * /*nested*/
             * documentation comment
             */
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /**
             * This is a
             * /*nested*/
            """), IgnoreRemaining);
    }

    [Fact]
    public void Nested_Multi_Line_Documentation_Comments_Terminated_At_Full_End_Sequence()
    {
        TestTokenizer("""
            /**
             * This is a
             * multi-line
             * documentation comment*
             */
            """, SyntaxFactory.Token(SyntaxKind.CSharpComment, """
            /**
             * This is a
             * multi-line
             * documentation comment*
             */
            """), IgnoreRemaining);
    }
}
