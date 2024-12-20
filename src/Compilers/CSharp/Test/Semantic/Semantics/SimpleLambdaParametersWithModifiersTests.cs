// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics;

public sealed class SimpleLambdaParametersWithModifiersTests : SemanticModelTestBase
{
    [Fact]
    public void TestOneParameterWithRef()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = (ref x) => { };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(RefKind.Ref, symbol.Parameters.Single().RefKind);
        Assert.Equal(SpecialType.System_Int32, symbol.Parameters.Single().Type.SpecialType);
    }

    [Fact]
    public void TestTwoParametersWithRef()
    {
        var compilation = CreateCompilation("""
            delegate void D(string s, ref int x);

            class C
            {
                void M()
                {
                    D d = (s, ref x) => { };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Type.SpecialType: SpecialType.System_String, RefKind: RefKind.None }, { Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.Ref }]);
    }

    [Fact]
    public void TestTwoParametersWithRefAndOptionalValue()
    {
        var compilation = CreateCompilation("""
            delegate void D(string s, ref int x);

            class C
            {
                void M()
                {
                    D d = (s, ref x = 1) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,19): error CS1741: A ref or out parameter cannot have a default value
                //         D d = (s, ref x = 1) => { };
                Diagnostic(ErrorCode.ERR_RefOutDefaultValue, "ref").WithLocation(7, 19),
                // (7,23): error CS9098: Implicitly typed lambda parameter 'x' cannot have a default value.
                //         D d = (s, ref x = 1) => { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedDefaultParameter, "x").WithArguments("x").WithLocation(7, 23));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [
        { Type.SpecialType: SpecialType.System_String, RefKind: RefKind.None },
        { Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.Ref, IsOptional: false }]);
    }

    [Fact]
    public void TestOneParameterWithAnAttribute()
    {
        var compilation = CreateCompilation("""
            using System;

            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = ([CLSCompliant(false)] ref x) => { };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(
            symbol.Parameters.Single().GetAttributes().Single().AttributeClass,
            compilation.GetTypeByMetadataName(typeof(CLSCompliantAttribute).FullName).GetPublicSymbol());
    }

    [Fact]
    public void TestOneParameterWithMultipleAttribute()
    {
        var compilation = CreateCompilation("""
            using System;

            [AttributeUsage(AttributeTargets.Parameter)]
            class MyAttribute : Attribute;
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = ([CLSCompliant(false), My] ref x) => { };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(
            symbol.Parameters.Single().GetAttributes().Any(a => a.AttributeClass!.Equals(
                compilation.GetTypeByMetadataName(typeof(CLSCompliantAttribute).FullName).GetPublicSymbol())));
        Assert.True(
            symbol.Parameters.Single().GetAttributes().Any(a => a.AttributeClass!.Equals(
                compilation.GetTypeByMetadataName("MyAttribute").GetPublicSymbol())));
    }
}
