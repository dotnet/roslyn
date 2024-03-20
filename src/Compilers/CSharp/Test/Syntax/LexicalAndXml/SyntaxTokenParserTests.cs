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
        AssertEx.Equal("""
            // Hello world
            class 
            """, parser.ParseNextToken().Token.ToFullString());
        AssertEx.Equal("""
            C

            """, parser.ParseNextToken().Token.ToFullString());

        AssertEx.Equal("""
            {

            """, parser.ParseNextToken().Token.ToFullString());

        AssertEx.Equal("""

            }
            """, parser.ParseNextToken().Token.ToFullString());

        var eofToken = parser.ParseNextToken().Token;
        Assert.Equal(SyntaxKind.EndOfFileToken, eofToken.Kind());
        Assert.Equal(0, eofToken.FullWidth);

        Assert.Equal(eofToken, parser.ParseNextToken().Token);
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
        AssertEx.Equal("""
            #if true
            class 
            """, parser.ParseNextToken().Token.ToFullString());
        AssertEx.Equal("""
            C

            """, parser.ParseNextToken().Token.ToFullString());
        AssertEx.Equal("""
            {

            """, parser.ParseNextToken().Token.ToFullString());

        var eofToken = parser.ParseNextToken().Token;
        Assert.Equal(SyntaxKind.EndOfFileToken, eofToken.Kind());
        AssertEx.Equal("""

            #else
            }
            #endif
            """, eofToken.ToFullString());
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

        AssertEx.Equal("""
            // Hello world
            class 
            """, parser.ParseNextToken().Token.ToFullString());

        parser.SkipForwardTo(23);

        AssertEx.Equal("""
            {

            """, parser.ParseNextToken().Token.ToFullString());

        AssertEx.Equal("""

            }
            """, parser.ParseNextToken().Token.ToFullString());
    }

    [Fact]
    public void SkipForwardTo2()
    {
        var sourceText = SourceText.From("""class""");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);
        parser.SkipForwardTo(1);

        AssertEx.Equal("""lass""", parser.ParseNextToken().Token.ToFullString());
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
        AssertEx.Equal("""
            #if true
            class 
            """, parser.ParseNextToken().Token.ToFullString());

        SyntaxTokenParser.Result cTokenResult = parser.ParseNextToken();
        AssertEx.Equal("""
            C

            """, cTokenResult.Token.ToFullString());

        verifyAfterC(parser);
        parser.ResetTo(cTokenResult);
        Assert.Equal(cTokenResult, parser.ParseNextToken());

        verifyAfterC(parser);

        static void verifyAfterC(SyntaxTokenParser parser)
        {
            AssertEx.Equal("""
            {

            """, parser.ParseNextToken().Token.ToFullString());

            var eofToken = parser.ParseNextToken().Token;
            Assert.Equal(SyntaxKind.EndOfFileToken, eofToken.Kind());
            AssertEx.Equal("""

            #else
            }
            #endif
            """, eofToken.ToFullString());
        }
    }

    [Fact]
    public void ResultContextualKind()
    {
        var sourceText = SourceText.From("when identifier class");
        var parser = SyntaxFactory.CreateTokenParser(sourceText, TestOptions.Regular);

        validateKinds(parsedKind: SyntaxKind.IdentifierToken, contextualKind: SyntaxKind.WhenKeyword);
        validateKinds(parsedKind: SyntaxKind.IdentifierToken, contextualKind: SyntaxKind.None);
        validateKinds(parsedKind: SyntaxKind.ClassKeyword, contextualKind: SyntaxKind.None);

        void validateKinds(SyntaxKind parsedKind, SyntaxKind contextualKind)
        {
            var result = parser.ParseNextToken();
            Assert.Equal(parsedKind, result.Token.Kind());
            Assert.Equal(contextualKind, result.ContextualKind);
        }
    }
}
