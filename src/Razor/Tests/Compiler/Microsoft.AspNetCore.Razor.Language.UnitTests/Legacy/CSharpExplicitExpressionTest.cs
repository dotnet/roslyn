// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpExplicitExpressionTest() : ParserTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void ShouldOutputZeroLengthCodeSpanIfExplicitExpressionIsEmpty()
    {
        ParseDocumentTest("@()");
    }

    [Fact]
    public void ShouldOutputZeroLengthCodeSpanIfEOFOccursAfterStartOfExplicitExpr()
    {
        // ParseBlockShouldOutputZeroLengthCodeSpanIfEOFOccursAfterStartOfExplicitExpression
        ParseDocumentTest("@(");
    }

    [Fact]
    public void ShouldAcceptEscapedQuoteInNonVerbatimStrings()
    {
        ParseDocumentTest("@(\"\\\"\")");
    }

    [Fact]
    public void ShouldAcceptEscapedQuoteInVerbatimStrings()
    {
        ParseDocumentTest("@(@\"\"\"\")");
    }

    [Fact]
    public void ShouldAcceptMultipleRepeatedEscapedQuoteInVerbatimStrings()
    {
        ParseDocumentTest("@(@\"\"\"\"\"\")");
    }

    [Fact]
    public void ShouldAcceptMultiLineVerbatimStrings()
    {
        ParseDocumentTest("""
            @(@"
            Foo
            Bar
            Baz
            ")
            """);
    }

    [Fact]
    public void ShouldAcceptMultipleEscapedQuotesInNonVerbatimStrings()
    {
        ParseDocumentTest("@(\"\\\"hello, world\\\"\")");
    }

    [Fact]
    public void ShouldAcceptMultipleEscapedQuotesInVerbatimStrings()
    {
        ParseDocumentTest("@(@\"\"\"hello, world\"\"\")");
    }

    [Fact]
    public void ShouldAcceptConsecutiveEscapedQuotesInNonVerbatimStrings()
    {
        ParseDocumentTest("@(\"\\\"\\\"\")");
    }

    [Fact]
    public void ShouldAcceptConsecutiveEscapedQuotesInVerbatimStrings()
    {
        ParseDocumentTest("@(@\"\"\"\"\"\")");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7230")]
    public void SwitchExpression()
    {
        ParseDocumentTest("""
            <span>@(value switch{
                10 => "ten",
                _ => "other"
            })</span>

            @code{
                public int value = 10;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7230")]
    public void SwitchExpression_WithLessThan()
    {
        ParseDocumentTest("""
            <span>@(value switch{
                < 10 => "less than",
                10 => "ten",
                _ => "other"
            })</span>

            @code{
                public int value = 10;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7230")]
    public void SwitchExpression_WithHtml()
    {
        ParseDocumentTest("""
            <span>@(value switch{
                10 => <span>ten</span>,
                _ => "other"
            })</span>

            @code{
                public int value = 10;
            }
            """);
    }
}
