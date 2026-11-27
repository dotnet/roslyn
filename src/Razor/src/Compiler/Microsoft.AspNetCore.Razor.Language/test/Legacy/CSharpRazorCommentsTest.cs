// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpRazorCommentsTest() : ParserTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void UnterminatedRazorComment()
    {
        ParseDocumentTest("@*");
    }

    [Fact]
    public void EmptyRazorComment()
    {
        ParseDocumentTest("@**@");
    }

    [Fact]
    public void RazorCommentInImplicitExpressionMethodCall()
    {
        ParseDocumentTest("""
            @foo(
            @**@

            """);
    }

    [Fact]
    public void UnterminatedRazorCommentInImplicitExpressionMethodCall()
    {
        ParseDocumentTest("@foo(@*");
    }

    [Fact]
    public void RazorMultilineCommentInBlock()
    {
        ParseDocumentTest(@"
@{
    @*
This is a comment
    *@
}
");
    }

    [Fact]
    public void RazorCommentInVerbatimBlock()
    {
        ParseDocumentTest("""
            @{
                <text
                @**@
            }
            """);
    }

    [Fact]
    public void RazorCommentInOpeningTagBlock()
    {
        ParseDocumentTest("<text @* razor comment *@></text>");
    }

    [Fact]
    public void RazorCommentInClosingTagBlock()
    {
        ParseDocumentTest("<text></text @* razor comment *@>");
    }

    [Fact]
    public void UnterminatedRazorCommentInVerbatimBlock()
    {
        ParseDocumentTest("@{@*");
    }

    [Fact]
    public void RazorCommentInMarkup()
    {
        ParseDocumentTest("""
            <p>
            @**@
            </p>
            """);
    }

    [Fact]
    public void MultipleRazorCommentInMarkup()
    {
        ParseDocumentTest("""
            <p>
              @**@  
            @**@
            </p>
            """);
    }

    [Fact]
    public void MultipleRazorCommentsInSameLineInMarkup()
    {
        ParseDocumentTest("""
            <p>
            @**@  @**@
            </p>
            """);
    }

    [Fact]
    public void RazorCommentsSurroundingMarkup()
    {
        ParseDocumentTest("""
            <p>
            @* hello *@ content @* world *@
            </p>
            """);
    }

    [Fact]
    public void RazorCommentBetweenCodeBlockAndMarkup()
    {
        ParseDocumentTest("""
            @{ }
            @* Hello World *@
            <div>Foo</div>
            """        );
    }

    [Fact]
    public void RazorCommentWithExtraNewLineInMarkup()
    {
        ParseDocumentTest("""
            <p>

            @* content *@
            @*
            content
            *@

            </p>
            """);
    }
}
