// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing;

public class InterpolatedStringExpressionTests
{
    [Fact]
    public void APIBackCompatTest1()
    {
        Assert.Equal("$\"\"", SyntaxFactory.InterpolatedStringExpression(SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken)).ToFullString());
    }

    [Fact]
    public void APIBackCompatTest2()
    {
        Assert.Equal("$\"goo\"", SyntaxFactory.InterpolatedStringExpression(
            SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken),
            SyntaxFactory.SingletonList<InterpolatedStringContentSyntax>(
                SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(
                    default, SyntaxKind.InterpolatedStringTextToken, "goo", "goo", default)))).ToFullString());
    }
}
