// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
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
        Assert.False(symbol.IsForEach()); // VB extension method works only for VB locals
    }

    [WorkItem(59709, "https://github.com/dotnet/roslyn/issues/59709")]
    [Fact]
    public void ForEachVisualBasic()
    {
        var sourceCode = @"
Module M1
    Class C
        Sub S() As Object
            Dim a() As Integer = {1, 2, 3}
            For Each x As Integer In a
                Dim y = x   'BIND:""x""
            Next
        End Sub
    End Class
End Module
";
        var compilation = CreateVisualBasicCompilation(sourceCode);
        var model = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
        var semanticInfo = VisualBasic.UnitTests.CompilationUtils.FindBindingText<VisualBasic.Syntax.IdentifierNameSyntax>(compilation);
        var local = (ILocalSymbol)model.GetSymbolInfo(semanticInfo).Symbol!;
        Assert.False(local.IsUsing);
        Assert.True(local.IsForEach);
        Assert.True(local.IsForEach());
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
