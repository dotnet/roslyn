// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class LocalTests : SemanticModelTestBase
{
    [WorkItem(59709, "https://github.com/dotnet/roslyn/issues/59709")]
    [Fact]
    public void ForEach()
    {
        var sourceCode = @"
class C
{
    void M()
    {
        var a = new int[] { 1, 2, 3 };
        foreach (var x in a)
        {
            var y = /*<bind>*/x/*</bind>*/;
        }
    }
}
";
        var compilation = CreateCompilation(sourceCode);
        var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(compilation);

        var symbol = (ILocalSymbol)semanticInfo.Symbol;
        Assert.False(symbol.IsUsing);
        Assert.True(symbol.IsForEach);
    }

    [WorkItem(59709, "https://github.com/dotnet/roslyn/issues/59709")]
    [Fact]
    public void ForEachAwait()
    {
        var sourceCode = @"
class C
{
    void M()
    {
        var a = new int[] { 1, 2, 3 };
        await foreach (var x in a)
        {
            var y = /*<bind>*/x/*</bind>*/;
        }
    }
}
";
        var compilation = CreateCompilation(sourceCode);
        var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(compilation);

        var symbol = (ILocalSymbol)semanticInfo.Symbol;
        Assert.False(symbol.IsUsing);
        Assert.True(symbol.IsForEach);
    }

    [WorkItem(59709, "https://github.com/dotnet/roslyn/issues/59709")]
    [Fact]
    public void UsingBlock()
    {
        var sourceCode = @"
using System.IO;
class C
{
    void M()
    {
        using (var x = new StreamReader(""""))
        {
            /*<bind>*/x/*</bind>*/.Read();
        }
    }
}
";
        var compilation = CreateCompilation(sourceCode);
        var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(compilation);

        var symbol = (ILocalSymbol)semanticInfo.Symbol;
        Assert.False(symbol.IsForEach);
        Assert.True(symbol.IsUsing);
    }

    [WorkItem(59709, "https://github.com/dotnet/roslyn/issues/59709")]
    [Fact]
    public void UsingBlockAwait()
    {
        var sourceCode = @"
using System.IO;
class C
{
    void M()
    {
        await using (var x = new StreamReader(""""))
        {
            /*<bind>*/x/*</bind>*/.Read();
        }
    }
}
";
        var compilation = CreateCompilation(sourceCode);
        var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(compilation);

        var symbol = (ILocalSymbol)semanticInfo.Symbol;
        Assert.False(symbol.IsForEach);
        Assert.True(symbol.IsUsing);
    }

    [WorkItem(59709, "https://github.com/dotnet/roslyn/issues/59709")]
    [Fact]
    public void UsingDeclaration()
    {
        var sourceCode = @"
using System.IO;
class C
{
    void M()
    {
        using var x = new StreamReader("""");
        /*<bind>*/x/*</bind>*/.Read();
    }
}
";
        var compilation = CreateCompilation(sourceCode);
        var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(compilation);

        var symbol = (ILocalSymbol)semanticInfo.Symbol;
        Assert.False(symbol.IsForEach);
        Assert.True(symbol.IsUsing);
    }
}
