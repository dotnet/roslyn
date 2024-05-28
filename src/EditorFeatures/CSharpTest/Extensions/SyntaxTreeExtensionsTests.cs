// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Extensions;

public class SyntaxTreeExtensionsTests
{
    private static void VerifyWholeLineIsActive(SyntaxTree tree, int lineNumber)
    {
        var line = tree.GetText().Lines[lineNumber];
        for (var pos = line.Start; pos < line.EndIncludingLineBreak; pos++)
        {
            Assert.False(tree.IsInInactiveRegion(pos, CancellationToken.None));
        }
    }

    private static void VerifyWholeLineIsInactive(SyntaxTree tree, int lineNumber)
    {
        var line = tree.GetText().Lines[lineNumber];
        for (var pos = line.Start; pos < line.EndIncludingLineBreak; pos++)
        {
            Assert.True(tree.IsInInactiveRegion(pos, CancellationToken.None));
        }
    }

    [Fact]
    public void SimpleInactive()
    {
        var code = """
            #if false
            This is inactive
            #else
            // This is active
            #endif
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        VerifyWholeLineIsActive(tree, 0);
        VerifyWholeLineIsInactive(tree, 1);
        VerifyWholeLineIsActive(tree, 2);
        VerifyWholeLineIsActive(tree, 3);
        VerifyWholeLineIsActive(tree, 4);
    }

    [Fact]
    public void InactiveEof()
    {
        var code = """
            #if false
            This is inactive
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        VerifyWholeLineIsActive(tree, 0);
        VerifyWholeLineIsInactive(tree, 1);
    }

    [Fact]
    public void InactiveEof2()
    {
        var code = """
            #if false
            This is inactive
            #endif
            // This is active
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        VerifyWholeLineIsActive(tree, 0);
        VerifyWholeLineIsInactive(tree, 1);
        VerifyWholeLineIsActive(tree, 2);
        VerifyWholeLineIsActive(tree, 3);
    }
}
