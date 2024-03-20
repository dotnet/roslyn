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
            """);

        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        AssertToken(SyntaxKind.ClassKeyword, SyntaxKind.None, """
            // Hello world
            class 
            """, parser.ParseNextToken());

        AssertToken(SyntaxKind.IdentifierToken, SyntaxKind.None, """
            C

            """, parser.ParseNextToken());

        AssertToken(SyntaxKind.OpenBraceToken, SyntaxKind.None, """
            {

            """, parser.ParseNextToken());

        AssertToken(SyntaxKind.CloseBraceToken, SyntaxKind.None, """

            }
            """, parser.ParseNextToken());

        AssertToken(SyntaxKind.EndOfFileToken, SyntaxKind.None, "", parser.ParseNextToken());
        AssertToken(SyntaxKind.EndOfFileToken, SyntaxKind.None, "", parser.ParseNextToken());
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
            """);
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        AssertToken(SyntaxKind.ClassKeyword, SyntaxKind.None, """
            #if true
            class 
            """, parser.ParseNextToken());

        AssertToken(SyntaxKind.IdentifierToken, SyntaxKind.None, """
            C

            """, parser.ParseNextToken());

        AssertToken(SyntaxKind.OpenBraceToken, SyntaxKind.None, """
            {

            """, parser.ParseNextToken());

        AssertToken(SyntaxKind.EndOfFileToken, SyntaxKind.None, """

            #else
            }
            #endif
            """, parser.ParseNextToken());

        AssertToken(SyntaxKind.EndOfFileToken, SyntaxKind.None, "", parser.ParseNextToken());
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
            """);
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        parser.SkipForwardTo(16);

        AssertToken(SyntaxKind.ClassKeyword, SyntaxKind.None, """
            // Hello world
            class 
            """, parser.ParseNextToken());

        parser.SkipForwardTo(39);

        AssertToken(SyntaxKind.OpenBraceToken, SyntaxKind.None, """
            {

            """, parser.ParseNextToken());

        AssertToken(SyntaxKind.CloseBraceToken, SyntaxKind.None, """

            }
            """, parser.ParseNextToken());
    }

    [Fact]
    public void SkipForwardTo2()
    {
        var sourceText = SourceText.From("""class""");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);
        parser.SkipForwardTo(1);

        AssertToken(SyntaxKind.IdentifierToken, SyntaxKind.None, """lass""", parser.ParseNextToken());
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
            """);
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        AssertToken(SyntaxKind.ClassKeyword, SyntaxKind.None, """
            #if true
            class 
            """, parser.ParseNextToken());

        SyntaxTokenParser.Result cTokenResult = parser.ParseNextToken();
        AssertToken(SyntaxKind.IdentifierToken, SyntaxKind.None, """
            C

            """, cTokenResult);

        verifyAfterC(parser);

        parser.ResetTo(cTokenResult);

        Assert.Equal(cTokenResult, parser.ParseNextToken());

        verifyAfterC(parser);

        static void verifyAfterC(SyntaxTokenParser parser)
        {
            AssertToken(SyntaxKind.OpenBraceToken, SyntaxKind.None, """
                {

                """, parser.ParseNextToken());

            AssertToken(SyntaxKind.EndOfFileToken, SyntaxKind.None, """

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

        AssertToken(SyntaxKind.IdentifierToken, SyntaxKind.WhenKeyword, "when ", parser.ParseNextToken());
        AssertToken(SyntaxKind.IdentifierToken, SyntaxKind.None, "identifier ", parser.ParseNextToken());
        AssertToken(SyntaxKind.ClassKeyword, SyntaxKind.None, "class", parser.ParseNextToken());
    }

    private static void AssertToken(SyntaxKind expectedKind, SyntaxKind expectedContextualKind, string expectedText, SyntaxTokenParser.Result result)
    {
        Assert.Equal(expectedKind, result.Token.Kind());
        Assert.Equal(expectedContextualKind, result.ContextualKind);
        AssertEx.Equal(expectedText, result.Token.ToFullString());
    }
}
