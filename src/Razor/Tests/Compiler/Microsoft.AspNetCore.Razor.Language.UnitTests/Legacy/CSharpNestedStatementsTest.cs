// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpNestedStatementsTest() : ParserTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void NestedSimpleStatement()
    {
        ParseDocumentTest("@while(true) { foo(); }");
    }

    [Fact]
    public void NestedKeywordStatement()
    {
        ParseDocumentTest("@while(true) { for(int i = 0; i < 10; i++) { foo(); } }");
    }

    [Fact]
    public void NestedCodeBlock()
    {
        ParseDocumentTest("@while(true) { { { { foo(); } } } }");
    }

    [Fact]
    public void NestedImplicitExpression()
    {
        ParseDocumentTest("@while(true) { @foo }");
    }

    [Fact]
    public void NestedExplicitExpression()
    {
        ParseDocumentTest("@while(true) { @(foo) }");
    }

    [Fact]
    public void NestedMarkupBlock()
    {
        ParseDocumentTest("@while(true) { <p>Hello</p> }");
    }
}
