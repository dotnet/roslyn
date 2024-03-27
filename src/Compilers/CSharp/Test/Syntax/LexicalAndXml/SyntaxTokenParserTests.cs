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
        AssertToken(expectedKind: SyntaxKind.EndOfFileToken, expectedContextualKind: SyntaxKind.None, new TextSpan(100, 0), "", parser.ParseNextToken());
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

    private static void AssertToken(SyntaxKind expectedKind, SyntaxKind expectedContextualKind, TextSpan expectedFullSpan, string expectedText, SyntaxTokenParser.Result result)
    {
        Assert.Equal(expectedKind, result.Token.Kind());
        Assert.Equal(expectedContextualKind, result.ContextualKind);
        AssertEx.Equal(expectedText.NormalizeLineEndings(), result.Token.ToFullString());
        Assert.Null(result.Token.Parent);
        Assert.Equal(expectedFullSpan, result.Token.FullSpan);
    }
}
