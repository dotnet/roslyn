// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpSpecialBlockTest() : ParserTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void NonKeywordStatementInCodeBlockIsHandledCorrectly()
    {
        ParseDocumentTest(
@"@{
    List<dynamic> photos = gallery.Photo.ToList();
}");
    }

    [Fact]
    public void BalancesBracesOutsideStringsIfFirstCharIsBraceAndReturnsSpanOfTypeCode()
    {
        // ParseBlockBalancesBracesOutsideStringsIfFirstCharacterIsBraceAndReturnsSpanOfTypeCode
        ParseDocumentTest("@{foo\"b}ar\" if(condition) { string.Format(\"{0}\"); } }");
    }

    [Fact]
    public void BalancesParensOutsideStringsIfFirstCharIsParenAndReturnsSpanOfTypeExpr()
    {
        // ParseBlockBalancesParensOutsideStringsIfFirstCharacterIsParenAndReturnsSpanOfTypeExpression
        ParseDocumentTest("@(foo\"b)ar\" if(condition) { string.Format(\"{0}\"); } )");
    }

    [Fact]
    public void ParseBlockIgnoresSingleSlashAtStart()
    {
        ParseDocumentTest("@/ foo");
    }

    [Fact]
    public void ParseBlockTerminatesSingleLineCommentAtEndOfLine()
    {
        ParseDocumentTest("""
            @if(!false) {
                // Foo
            	<p>A real tag!</p>
            }
            """);
    }
}
