// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.LexicalAndXml;

public class SyntaxTokenParserTests
{
    [Fact]
    public void TestDispose()
    {
        var sourceText = SourceText.From("class C { }");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);
        parser.Dispose();
        Assert.Throws<NullReferenceException>(() => parser.ParseNextToken());
        parser.Dispose();
    }

    [Fact]
    public void TestParseNext()
    {
        var sourceText = SourceText.From("""
            // Hello world
            class C
            {

            }
            """.NormalizeLineEndings());

        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 22), """
            // Hello world
            class 
            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.IdentifierToken, expectedContextualKind: SyntaxKind.None, new TextSpan(22, 3), """
            C

            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.OpenBraceToken, expectedContextualKind: SyntaxKind.None, new TextSpan(25, 3), """
            {

            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.CloseBraceToken, expectedContextualKind: SyntaxKind.None, new TextSpan(28, 3), """

            }
            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.EndOfFileToken, expectedContextualKind: SyntaxKind.None, new TextSpan(31, 0), "", parser.ParseNextToken());
        AssertToken(expectedKind: SyntaxKind.EndOfFileToken, expectedContextualKind: SyntaxKind.None, new TextSpan(31, 0), "", parser.ParseNextToken());
    }

    [Fact]
    public void DocumentCommentTriviaIsHandled()
    {
        var sourceText = SourceText.From("""
            /// <summary>
            /// Hello world
            /// </summary>
            class C
            {

            }
            """.NormalizeLineEndings());
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        var result = parser.ParseNextToken();
        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 54), """
            /// <summary>
            /// Hello world
            /// </summary>
            class 
            """, result);

        var docCommentTrivia = result.Token.GetLeadingTrivia()[0];
        Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, docCommentTrivia.Kind());
        Assert.NotNull(docCommentTrivia.GetStructure());

        AssertToken(expectedKind: SyntaxKind.IdentifierToken, expectedContextualKind: SyntaxKind.None, new TextSpan(54, 3), """
            C

            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.OpenBraceToken, expectedContextualKind: SyntaxKind.None, new TextSpan(57, 3), """
            {

            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.CloseBraceToken, expectedContextualKind: SyntaxKind.None, new TextSpan(60, 3), """

            }
            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.EndOfFileToken, expectedContextualKind: SyntaxKind.None, new TextSpan(63, 0), "", parser.ParseNextToken());
    }

    [Fact]
    public void DirectiveContextIsPreservedAcrossParseNextToken()
    {
        var sourceText = SourceText.From("""
            #if true
            class C
            {

            #else
            }
            #endif
            """.NormalizeLineEndings());
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 16), """
            #if true
            class 
            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.IdentifierToken, expectedContextualKind: SyntaxKind.None, new TextSpan(16, 3), """
            C

            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.OpenBraceToken, expectedContextualKind: SyntaxKind.None, new TextSpan(19, 3), """
            {

            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.EndOfFileToken, expectedContextualKind: SyntaxKind.None, new TextSpan(22, 18), """

            #else
            }
            #endif
            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.EndOfFileToken, expectedContextualKind: SyntaxKind.None, new TextSpan(40, 0), "", parser.ParseNextToken());
    }

    [Fact]
    public void SkipForwardTo()
    {
        var sourceText = SourceText.From("""
            This is not C#

            // Hello world
            class C
            {

            }
            """.NormalizeLineEndings());
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        parser.SkipForwardTo(18);

        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(18, 22), """
            // Hello world
            class 
            """, parser.ParseNextToken());

        parser.SkipForwardTo(43);

        AssertToken(expectedKind: SyntaxKind.OpenBraceToken, expectedContextualKind: SyntaxKind.None, new TextSpan(43, 3), """
            {

            """, parser.ParseNextToken());

        AssertToken(expectedKind: SyntaxKind.CloseBraceToken, expectedContextualKind: SyntaxKind.None, new TextSpan(46, 3), """

            }
            """, parser.ParseNextToken());
    }

    [Fact]
    public void SkipForwardTo2()
    {
        var sourceText = SourceText.From("""class""");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);
        parser.SkipForwardTo(1);

        AssertToken(expectedKind: SyntaxKind.IdentifierToken, expectedContextualKind: SyntaxKind.None, new TextSpan(1, 4), """lass""", parser.ParseNextToken());
    }

    [Fact]
    public void SkipForwardTo_PastDocumentEnd()
    {
        var sourceText = SourceText.From("class C { }");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);
        parser.SkipForwardTo(100);
        AssertToken(expectedKind: SyntaxKind.EndOfFileToken, expectedContextualKind: SyntaxKind.None, new TextSpan(11, 0), "", parser.ParseNextToken());
    }

    [Fact]
    public void SkipForwardTo_CannotSkipBack()
    {
        var sourceText = SourceText.From("class C { }");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);
        parser.SkipForwardTo(0);
        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 6), "class ", parser.ParseNextToken());
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.SkipForwardTo(0));
    }

    [Fact]
    public void ResetToPreservedDirectiveContext()
    {
        var sourceText = SourceText.From("""
            #if true
            class C
            {

            #else
            }
            #endif
            """.NormalizeLineEndings());
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 16), """
            #if true
            class 
            """, parser.ParseNextToken());

        SyntaxTokenParser.Result cTokenResult = parser.ParseNextToken();
        AssertToken(expectedKind: SyntaxKind.IdentifierToken, expectedContextualKind: SyntaxKind.None, new TextSpan(16, 3), """
            C

            """, cTokenResult);

        verifyAfterC(parser);

        parser.ResetTo(cTokenResult);

        Assert.Equal(cTokenResult, parser.ParseNextToken());

        verifyAfterC(parser);

        static void verifyAfterC(SyntaxTokenParser parser)
        {
            AssertToken(expectedKind: SyntaxKind.OpenBraceToken, expectedContextualKind: SyntaxKind.None, new TextSpan(19, 3), """
                {

                """, parser.ParseNextToken());

            AssertToken(expectedKind: SyntaxKind.EndOfFileToken, expectedContextualKind: SyntaxKind.None, new TextSpan(22, 18), """

                #else
                }
                #endif
                """, parser.ParseNextToken());
        }
    }

    [Fact]
    public void ResultContextualKind()
    {
        var sourceText = SourceText.From("when identifier class");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        AssertToken(expectedKind: SyntaxKind.IdentifierToken, expectedContextualKind: SyntaxKind.WhenKeyword, new TextSpan(0, 5), "when ", parser.ParseNextToken());
        AssertToken(expectedKind: SyntaxKind.IdentifierToken, expectedContextualKind: SyntaxKind.None, new TextSpan(5, 11), "identifier ", parser.ParseNextToken());
        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(16, 5), "class", parser.ParseNextToken());
    }

    [Fact]
    public void ParseLeadingTrivia_Empty()
    {
        var sourceText = SourceText.From("class C { }");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        var result = parser.ParseLeadingTrivia();
        AssertToken(expectedKind: SyntaxKind.None, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 0), "", result);
        Assert.Empty(result.Token.LeadingTrivia);
        Assert.Empty(result.Token.TrailingTrivia);

        result = parser.ParseTrailingTrivia();
        AssertToken(expectedKind: SyntaxKind.None, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 0), "", result);
        Assert.Empty(result.Token.LeadingTrivia);
        Assert.Empty(result.Token.TrailingTrivia);

        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 6), "class ", parser.ParseNextToken());
    }

    [Fact]
    public void ParseLeadingTrivia_SameLine()
    {
        var sourceText = SourceText.From("/* test */ class C { }");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        var result = parser.ParseLeadingTrivia();
        AssertToken(expectedKind: SyntaxKind.None, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 11), "/* test */ ", result);
        AssertTrivia(result.Token.LeadingTrivia,
            (SyntaxKind.MultiLineCommentTrivia, "/* test */"),
            (SyntaxKind.WhitespaceTrivia, " "));
        Assert.Empty(result.Token.TrailingTrivia);

        var intermediateResult = parser.ParseLeadingTrivia();
        AssertToken(expectedKind: SyntaxKind.None, expectedContextualKind: SyntaxKind.None, new TextSpan(11, 0), "", intermediateResult);
        Assert.Empty(intermediateResult.Token.LeadingTrivia);
        Assert.Empty(intermediateResult.Token.TrailingTrivia);

        intermediateResult = parser.ParseTrailingTrivia();
        AssertToken(expectedKind: SyntaxKind.None, expectedContextualKind: SyntaxKind.None, new TextSpan(11, 0), "", intermediateResult);
        Assert.Empty(intermediateResult.Token.LeadingTrivia);
        Assert.Empty(intermediateResult.Token.TrailingTrivia);

        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(11, 6), "class ", parser.ParseNextToken());

        parser.ResetTo(result);
        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 17), "/* test */ class ", parser.ParseNextToken());
    }

    [Fact]
    public void ParseLeadingTrivia_MultiLine()
    {
        var sourceText = SourceText.From("""
            /* test */

            class C { }
            """.NormalizeLineEndings());
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        var result = parser.ParseLeadingTrivia();
        AssertToken(expectedKind: SyntaxKind.None, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 14), $"/* test */\r\n\r\n", result);
        AssertTrivia(result.Token.LeadingTrivia,
            (SyntaxKind.MultiLineCommentTrivia, "/* test */"),
            (SyntaxKind.EndOfLineTrivia, "\r\n"),
            (SyntaxKind.EndOfLineTrivia, "\r\n"));
        Assert.Empty(result.Token.TrailingTrivia);

        var intermediateResult = parser.ParseLeadingTrivia();
        AssertToken(expectedKind: SyntaxKind.None, expectedContextualKind: SyntaxKind.None, new TextSpan(14, 0), "", intermediateResult);
        Assert.Empty(intermediateResult.Token.LeadingTrivia);
        Assert.Empty(intermediateResult.Token.TrailingTrivia);

        intermediateResult = parser.ParseTrailingTrivia();
        AssertToken(expectedKind: SyntaxKind.None, expectedContextualKind: SyntaxKind.None, new TextSpan(14, 0), "", intermediateResult);
        Assert.Empty(intermediateResult.Token.LeadingTrivia);
        Assert.Empty(intermediateResult.Token.TrailingTrivia);

        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(14, 6), "class ", parser.ParseNextToken());

        parser.ResetTo(result);
        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 20), "/* test */\r\n\r\nclass ", parser.ParseNextToken());
    }

    [Fact]
    public void ParseTrailingTrivia_Empty()
    {
        var sourceText = SourceText.From("class C { }");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        var result = parser.ParseTrailingTrivia();
        AssertToken(expectedKind: SyntaxKind.None, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 0), "", result);
        Assert.Empty(result.Token.LeadingTrivia);
        Assert.Empty(result.Token.TrailingTrivia);

        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 6), "class ", parser.ParseNextToken());
    }

    [Fact]
    public void ParseTrailingTrivia_SameLine()
    {
        var sourceText = SourceText.From("/* test */ class C { }");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        var result = parser.ParseTrailingTrivia();
        AssertToken(expectedKind: SyntaxKind.None, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 11), "/* test */ ", result);
        Assert.Empty(result.Token.LeadingTrivia);
        AssertTrivia(result.Token.TrailingTrivia,
            (SyntaxKind.MultiLineCommentTrivia, "/* test */"),
            (SyntaxKind.WhitespaceTrivia, " "));

        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(11, 6), "class ", parser.ParseNextToken());

        parser.ResetTo(result);
        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 17), "/* test */ class ", parser.ParseNextToken());
    }

    [Fact]
    public void ParseTrailingTrivia_MultiLine()
    {
        var sourceText = SourceText.From("""
            /* test */

            class C { }
            """.NormalizeLineEndings());
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        var result = parser.ParseTrailingTrivia();
        AssertToken(expectedKind: SyntaxKind.None, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 12), $"/* test */\r\n", result);
        Assert.Empty(result.Token.LeadingTrivia);
        AssertTrivia(result.Token.TrailingTrivia,
            (SyntaxKind.MultiLineCommentTrivia, "/* test */"),
            (SyntaxKind.EndOfLineTrivia, "\r\n"));

        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(12, 8), "\r\nclass ", parser.ParseNextToken());

        parser.ResetTo(result);
        AssertToken(expectedKind: SyntaxKind.ClassKeyword, expectedContextualKind: SyntaxKind.None, new TextSpan(0, 20), "/* test */\r\n\r\nclass ", parser.ParseNextToken());
    }

    private static void AssertToken(SyntaxKind expectedKind, SyntaxKind expectedContextualKind, TextSpan expectedFullSpan, string expectedText, SyntaxTokenParser.Result result)
    {
        Assert.Equal(expectedKind, result.Token.Kind());
        Assert.Equal(expectedContextualKind, result.ContextualKind);
        AssertEx.Equal(expectedText.NormalizeLineEndings(), result.Token.ToFullString());
        Assert.Null(result.Token.Parent);
        Assert.Equal(expectedFullSpan, result.Token.FullSpan);
    }

    private static void AssertTrivia(SyntaxTriviaList leadingTrivia, params (SyntaxKind kind, string text)[] expectedTrivia)
    {
        Assert.Equal(expectedTrivia.Length, leadingTrivia.Count);
        for (int i = 0; i < expectedTrivia.Length; i++)
        {
            var (kind, text) = expectedTrivia[i];
            Assert.Equal(kind, leadingTrivia[i].Kind());
            Assert.Equal(text, leadingTrivia[i].ToFullString());
        }
    }
}
