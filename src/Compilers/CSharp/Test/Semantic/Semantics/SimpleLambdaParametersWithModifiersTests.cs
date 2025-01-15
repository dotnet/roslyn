// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
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
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

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
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

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
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

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
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(
            symbol.Parameters.Single().GetAttributes().Single().AttributeClass,
            compilation.GetTypeByMetadataName(typeof(CLSCompliantAttribute).FullName).GetPublicSymbol());
    }

    [Fact]
    public void TestOneParameterWithAnAttributeAndDefaultValue()
    {
        var compilation = CreateCompilation("""
            using System;

            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = ([CLSCompliant(false)] ref x = 0) => { };
                }
            }
            """).VerifyDiagnostics(
                // (9,38): error CS1741: A ref or out parameter cannot have a default value
                //         D d = ([CLSCompliant(false)] ref x = 0) => { };
                Diagnostic(ErrorCode.ERR_RefOutDefaultValue, "ref").WithLocation(9, 38),
                // (9,42): error CS9098: Implicitly typed lambda parameter 'x' cannot have a default value.
                //         D d = ([CLSCompliant(false)] ref x = 0) => { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedDefaultParameter, "x").WithArguments("x").WithLocation(9, 42));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(
            symbol.Parameters.Single().GetAttributes().Single().AttributeClass,
            compilation.GetTypeByMetadataName(typeof(CLSCompliantAttribute).FullName).GetPublicSymbol());
        Assert.True(symbol.Parameters is [{ Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.Ref, IsOptional: false }]);
    }

    [Theory]
    [InlineData("[CLSCompliant(false), My]")]
    [InlineData("[CLSCompliant(false)][My]")]
    public void TestOneParameterWithMultipleAttribute(string attributeForm)
    {
        var compilation = CreateCompilation($$"""
            using System;

            [AttributeUsage(AttributeTargets.Parameter)]
            class MyAttribute : Attribute;
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = ({{attributeForm}} ref x) => { };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

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

    [Fact]
    public void TestOneParameterWithScoped()
    {
        var compilation = CreateCompilationWithSpan("""
            using System;
            delegate void D(scoped ReadOnlySpan<int> x);

            class C
            {
                void M()
                {
                    D d = (scoped x) => { };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(ScopedKind.ScopedValue, symbol.Parameters.Single().ScopedKind);
        Assert.Equal(compilation.GetTypeByMetadataName(typeof(ReadOnlySpan<>).FullName).GetPublicSymbol(), symbol.Parameters.Single().Type.OriginalDefinition);
    }

    [Fact]
    public void TestTwoParametersWithScopedAndRef1()
    {
        var compilation = CreateCompilationWithSpan("""
            using System;
            delegate void D(scoped ReadOnlySpan<int> a, ref ReadOnlySpan<int> b);

            class C
            {
                void M()
                {
                    D d = (scoped a, ref b) => { };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(ScopedKind.ScopedValue, symbol.Parameters.First().ScopedKind);
        Assert.Equal(compilation.GetTypeByMetadataName(typeof(ReadOnlySpan<>).FullName).GetPublicSymbol(), symbol.Parameters.First().Type.OriginalDefinition);
    }

    [Fact]
    public void TestTwoParametersWithScopedAndRef2()
    {
        var compilation = CreateCompilationWithSpan("""
            using System;
            delegate void D(scoped ReadOnlySpan<int> a, ref ReadOnlySpan<int> b);

            class C
            {
                void M()
                {
                    D d = (a, ref b) => { };
                }
            }
            """).VerifyDiagnostics(
                // (8,15): error CS8986: The 'scoped' modifier of parameter 'a' doesn't match target 'D'.
                //         D d = (a, ref b) => { };
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfTarget, "(a, ref b) => { }").WithArguments("a", "D").WithLocation(8, 15));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(ScopedKind.None, symbol.Parameters.First().ScopedKind);
        Assert.Equal(compilation.GetTypeByMetadataName(typeof(ReadOnlySpan<>).FullName).GetPublicSymbol(), symbol.Parameters.First().Type.OriginalDefinition);
    }

    [Fact]
    public void TestOneParameterWithScopedAndOptionalValue()
    {
        var compilation = CreateCompilationWithSpan("""
            using System;
            delegate void D(scoped ReadOnlySpan<int> x);

            class C
            {
                void M()
                {
                    D d = (scoped x = default) => { };
                }
            }
            """).VerifyDiagnostics(
                // (8,23): error CS9098: Implicitly typed lambda parameter 'x' cannot have a default value.
                //         D d = (scoped x = default) => { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedDefaultParameter, "x").WithArguments("x").WithLocation(8, 23),
                // (8,23): warning CS9099: Parameter 1 has default value 'null' in lambda but '<missing>' in the target delegate type.
                //         D d = (scoped x = default) => { };
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "null", "<missing>").WithLocation(8, 23));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(ScopedKind.ScopedValue, symbol.Parameters.Single().ScopedKind);
        Assert.True(symbol.Parameters.Single().IsOptional);
        Assert.Equal(compilation.GetTypeByMetadataName(typeof(ReadOnlySpan<>).FullName).GetPublicSymbol(), symbol.Parameters.Single().Type.OriginalDefinition);
    }

    [Theory, CombinatorialData]
    public void TestOneParameterWithScopedAsParameterName(bool escaped)
    {
        var compilation = CreateCompilationWithSpan($$"""
            using System;
            delegate void D(scoped ReadOnlySpan<int> x);

            class C
            {
                void M()
                {
                    D d = (scoped {{(escaped ? "@" : "")}}scoped) => { };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(ScopedKind.ScopedValue, symbol.Parameters.Single().ScopedKind);
        Assert.Equal("scoped", symbol.Parameters.Single().Name);
        Assert.Equal(compilation.GetTypeByMetadataName(typeof(ReadOnlySpan<>).FullName).GetPublicSymbol(), symbol.Parameters.Single().Type.OriginalDefinition);
    }

    [Fact]
    public void TestInconsistentUseOfTypes()
    {
        var compilation = CreateCompilation("""
            delegate void D(string s, ref int x);

            class C
            {
                void M()
                {
                    D d = (string s, ref x) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,30): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                //         D d = (string s, ref x) => { };
                Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "x").WithLocation(7, 30));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [
        { Type.SpecialType: SpecialType.System_String, RefKind: RefKind.None },
        { Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.Ref }]);
    }

    [Fact]
    public void TestOneParameterWithNoModifiersAndOptionalValue()
    {
        var compilation = CreateCompilation("""
            delegate void D(int x);

            class C
            {
                void M()
                {
                    D d = (x = 1) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,16): error CS9098: Implicitly typed lambda parameter 'x' cannot have a default value.
                //         D d = (x = 1) => { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedDefaultParameter, "x").WithArguments("x").WithLocation(7, 16),
                // (7,16): warning CS9099: Parameter 1 has default value '1' in lambda but '<missing>' in the target delegate type.
                //         D d = (x = 1) => { };
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "x").WithArguments("1", "1", "<missing>").WithLocation(7, 16));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.None, IsOptional: true }]);
    }

    [Fact]
    public void TestNonParenthesizedLambdaPrecededByRef()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = ref x => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,11): error CS8171: Cannot initialize a by-value variable with a reference
                //         D d = ref x => { };
                Diagnostic(ErrorCode.ERR_InitializeByValueVariableWithReference, "d = ref x => { }").WithLocation(7, 11),
                // (7,19): error CS1676: Parameter 1 must be declared with the 'ref' keyword
                //         D d = ref x => { };
                Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "ref").WithLocation(7, 19));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Type: IErrorTypeSymbol, RefKind: RefKind.None, IsOptional: false }]);
    }

    [Fact]
    public void TestRefParameterMissingName()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = (ref) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,19): error CS1001: Identifier expected
                //         D d = (ref) => { };
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(7, 19));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "", Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.Ref, IsOptional: false }]);
    }

    [Fact]
    public void TestAnonymousMethodWithRefParameter()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = delegate (ref x) { };
                }
            }
            """).VerifyDiagnostics(
                // (7,29): error CS0246: The type or namespace name 'x' could not be found (are you missing a using directive or an assembly reference?)
                //         D d = delegate (ref x) { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "x").WithArguments("x").WithLocation(7, 29),
                // (7,30): error CS1001: Identifier expected
                //         D d = delegate (ref x) { };
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(7, 30));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<AnonymousMethodExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "", Type: IErrorTypeSymbol { Name: "x" }, RefKind: RefKind.Ref, IsOptional: false }]);
    }

    [Fact]
    public void TestLocalFunctionWithRefParameter()
    {
        var compilation = CreateCompilation("""
            class C
            {
                void M()
                {
                    void LocalFunc(ref x) { };
                }
            }
            """).VerifyDiagnostics(
                // (5,14): warning CS8321: The local function 'LocalFunc' is declared but never used
                //         void LocalFunc(ref x) { };
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "LocalFunc").WithArguments("LocalFunc").WithLocation(5, 14),
                // (5,28): error CS0246: The type or namespace name 'x' could not be found (are you missing a using directive or an assembly reference?)
                //         void LocalFunc(ref x) { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "x").WithArguments("x").WithLocation(5, 28),
                // (5,29): error CS1001: Identifier expected
                //         void LocalFunc(ref x) { };
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(5, 29));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = semanticModel.GetDeclaredSymbol(lambda)!;

        Assert.Equal(MethodKind.LocalFunction, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "", Type: IErrorTypeSymbol { Name: "x" }, RefKind: RefKind.Ref, IsOptional: false }]);
    }

    [Fact]
    public void TestOverloadResolution1()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int x);
            delegate void E(ref int x);

            class C
            {
                void M()
                {
                    M1((ref x) => { });
                }

                void M1(D d) { }
                void M1(E e) { }
            }
            """).VerifyDiagnostics(
                // (8,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(D)' and 'C.M1(E)'
                //         M1((ref x) => { });
                Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(D)", "C.M1(E)").WithLocation(8, 9));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.Ref, IsOptional: false }]);
    }

    [Theory, CombinatorialData]
    public void TestOverloadResolution2(bool passByRef)
    {
        var compilation = CompileAndVerify($$"""
            using System;

            delegate void D(ref int x);
            delegate void E(int x);

            class C
            {
                static void Main()
                {
                    M1(({{(passByRef ? "ref" : "")}} x) => { });
                }

                static void M1(D d) { Console.WriteLine(0); }
                static void M1(E e) { Console.WriteLine(1); }
            }
            """,
            expectedOutput: passByRef ? "0" : "1").VerifyDiagnostics().Compilation;

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.SpecialType: SpecialType.System_Int32, IsOptional: false } parameter] &&
            parameter.RefKind == (passByRef ? RefKind.Ref : RefKind.None));
    }

    [Fact]
    public void TestTypeInference()
    {
        var compilation = CreateCompilation("""
            delegate void D<T>(ref T x);

            class C
            {
                void M()
                {
                    M1((ref x) => { }, "");
                }

                void M1<T>(D<T> d, T value) { }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.SpecialType: SpecialType.System_String, RefKind: RefKind.Ref, IsOptional: false }]);
    }

    [Fact]
    public void TestInModifier()
    {
        var compilation = CreateCompilation("""
            delegate void D(in int x);

            class C
            {
                void M()
                {
                    D d = (in x) =>
                    {
                    };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.In, IsOptional: false }]);
    }

    [Fact]
    public void TestInModifierWriteWithinLambda()
    {
        var compilation = CreateCompilation("""
            delegate void D(in int x);

            class C
            {
                void M()
                {
                    D d = (in x) =>
                    {
                        x = 1;
                    };
                }
            }
            """).VerifyDiagnostics(
                // (9,13): error CS8331: Cannot assign to variable 'x' or use it as the right hand side of a ref assignment because it is a readonly variable
                //             x = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "x").WithArguments("variable", "x").WithLocation(9, 13));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.In, IsOptional: false }]);
    }

    [Fact]
    public void TestOutModifier()
    {
        var compilation = CreateCompilation("""
            delegate void D(out int x);

            class C
            {
                void M()
                {
                    D d = (out x) =>
                    {
                        x = 1;
                    };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.Out, IsOptional: false }]);
    }

    [Fact]
    public void TestOutModifierMustBeWrittenWithinLambda()
    {
        var compilation = CreateCompilation("""
            delegate void D(out int x);

            class C
            {
                void M()
                {
                    D d = (out x) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,26): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //         D d = (out x) => { };
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "{ }").WithArguments("x").WithLocation(7, 26));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.Out, IsOptional: false }]);
    }

    [Theory, CombinatorialData]
    public void TestExpressionTree(bool explicitType)
    {
        var compilation = CreateCompilation($$"""
            using System.Linq.Expressions;

            delegate int D(ref int x);

            class C
            {
                void M()
                {
                    Expression<D> e = (ref {{(explicitType ? "int " : "")}}x) => 0;
                }
            }
            """).VerifyDiagnostics(
                // (9,33): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
                //         Expression<D> e = (ref x) => 0;
                Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "x"));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.Ref, IsOptional: false }]);
    }

    [Fact]
    public void TestNullable1()
    {
        var compilation = CreateCompilation($$"""
            #nullable enable

            delegate void D(ref string? x);

            class C
            {
                void M()
                {
                    D d = (ref x) =>
                    {
                        string y = x;
                    };
                }
            }
            """).VerifyDiagnostics(
                // (11,24): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //             string y = x;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "x").WithLocation(11, 24));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.SpecialType: SpecialType.System_String, Type.NullableAnnotation: CodeAnalysis.NullableAnnotation.Annotated, RefKind: RefKind.Ref, IsOptional: false }]);
    }

    [Fact]
    public void TestNullable2()
    {
        var compilation = CreateCompilation($$"""
            #nullable enable

            delegate void D(ref string x);

            class C
            {
                void M()
                {
                    D d = (ref x) =>
                    {
                        x = null;
                    };
                }
            }
            """).VerifyDiagnostics(
                // (11,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //             x = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(11, 17));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.SpecialType: SpecialType.System_String, Type.NullableAnnotation: CodeAnalysis.NullableAnnotation.NotAnnotated, RefKind: RefKind.Ref, IsOptional: false }]);
    }

    [Fact]
    public void TestDynamic()
    {
        var compilation = CreateCompilation($$"""
            delegate void D(ref dynamic x);

            class C
            {
                void M()
                {
                    D d = (ref x) =>
                    {
                        x = null;
                        x = 1;
                        x = "";
                        x.NonExistent();
                    };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.TypeKind: TypeKind.Dynamic, RefKind: RefKind.Ref, IsOptional: false }]);
    }

    [Theory, CombinatorialData]
    public void TestParams1(bool delegateIsParams)
    {
        var compilation = CreateCompilation($$"""
            delegate void D({{(delegateIsParams ? "params" : "")}} int[] x);

            class C
            {
                void M()
                {
                    D d = (params x) =>
                    {
                    };
                }
            }
            """);

        if (delegateIsParams)
        {
            compilation.VerifyDiagnostics(
                // (7,16): error CS9272: Implicitly typed lambda parameter 'x' cannot have the 'params' modifier.
                //         D d = (params x) =>
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedParamsParameter, "params").WithArguments("x").WithLocation(7, 16));
        }
        else
        {
            compilation.VerifyDiagnostics(
                // (7,16): error CS9272: Implicitly typed lambda parameter 'x' cannot have the 'params' modifier.
                //         D d = (params x) =>
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedParamsParameter, "params").WithArguments("x").WithLocation(7, 16),
                // (7,23): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                //         D d = (params x) =>
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "x").WithArguments("1").WithLocation(7, 23));
        }

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type: IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Int32 }, IsParams: true }]);
    }

    [Fact]
    public void TestParams2()
    {
        var compilation = CreateCompilation($$"""
            delegate void D(params int[] x);

            class C
            {
                void M()
                {
                    D d = (x) =>
                    {
                    };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type: IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Int32 }, IsParams: false }]);
    }
    [Fact]
    public void TestIOperation()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = (ref x) => { return; };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var operation = (IAnonymousFunctionOperation)semanticModel.GetOperation(lambda)!;
        Assert.NotNull(operation);

        compilation.VerifyOperationTree(lambda, """
            IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '(ref x) => { return; }')
              IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ return; }')
                IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return;')
                  ReturnedValue:
                    null
            """);

        var symbol = operation.Symbol;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(RefKind.Ref, symbol.Parameters.Single().RefKind);
        Assert.Equal(SpecialType.System_Int32, symbol.Parameters.Single().Type.SpecialType);

        Assert.NotNull(operation.Body);
        Assert.Single(operation.Body.Operations);
        Assert.True(operation.Body.Operations.Single() is IReturnOperation { ReturnedValue: null });
    }

    [Fact]
    public void TestParamsRef()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int[] x);

            class C
            {
                void M()
                {
                    D d = (params ref x) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,16): error CS9272: Implicitly typed lambda parameter 'x' cannot have the 'params' modifier.
                //         D d = (params ref x) => { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedParamsParameter, "params").WithArguments("x").WithLocation(7, 16),
                // (7,23): error CS1611: The params parameter cannot be declared as ref
                //         D d = (params ref x) => { };
                Diagnostic(ErrorCode.ERR_ParamsCantBeWithModifier, "ref").WithArguments("ref").WithLocation(7, 23),
                // (7,27): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                //         D d = (params ref x) => { };
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "x").WithArguments("1").WithLocation(7, 27));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type: IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Int32 }, RefKind: RefKind.Ref, IsParams: true }]);
    }

    [Fact]
    public void TestRefParams()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int[] x);

            class C
            {
                void M()
                {
                    D d = (ref params x) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,20): error CS8328:  The parameter modifier 'params' cannot be used with 'ref'
                //         D d = (ref params x) => { };
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "ref").WithLocation(7, 20),
                // (7,20): error CS9272: Implicitly typed lambda parameter 'x' cannot have the 'params' modifier.
                //         D d = (ref params x) => { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedParamsParameter, "params").WithArguments("x").WithLocation(7, 20),
                // (7,27): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                //         D d = (ref params x) => { };
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "x").WithArguments("1").WithLocation(7, 27));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type: IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Int32 }, RefKind: RefKind.Ref, IsParams: true }]);
    }

    [Fact]
    public void TestRefToOut()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = (out x) => { x = 1; };
                }
            }
            """).VerifyDiagnostics(
                // (7,20): error CS1676: Parameter 1 must be declared with the 'ref' keyword
                //         D d = (out x) => { x = 1; };
                Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "ref").WithLocation(7, 20));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type: IErrorTypeSymbol, RefKind: RefKind.Out, IsParams: false }]);
    }

    [Fact]
    public void TestOutToRef()
    {
        var compilation = CreateCompilation("""
            delegate void D(out int x);

            class C
            {
                void M()
                {
                    D d = (ref x) => { x = 1; };
                }
            }
            """).VerifyDiagnostics(
                // (7,20): error CS1676: Parameter 1 must be declared with the 'out' keyword
                //         D d = (ref x) => { x = 1; };
                Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "out").WithLocation(7, 20));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type: IErrorTypeSymbol, RefKind: RefKind.Ref, IsParams: false }]);
    }

    [Theory, CombinatorialData]
    public void TestScopedOnDelegateNotOnLambda(bool includeType)
    {
        var compilation = CreateCompilationWithSpan($$"""
            using System;
            delegate void D(scoped ReadOnlySpan<int> x);

            class C
            {
                void M()
                {
                    D d = ({{(includeType ? "ReadOnlySpan<int> " : "")}}x) => { };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(ScopedKind.None, symbol.Parameters.Single().ScopedKind);
        Assert.Equal("x", symbol.Parameters.Single().Name);
        Assert.Equal(compilation.GetTypeByMetadataName(typeof(ReadOnlySpan<>).FullName).GetPublicSymbol(), symbol.Parameters.Single().Type.OriginalDefinition);
    }

    [Theory, CombinatorialData]
    public void TestScopedOnLambdaNotOnDelegate(bool includeType)
    {
        var compilation = CreateCompilationWithSpan($$"""
            using System;
            delegate void D(ReadOnlySpan<int> x);

            class C
            {
                void M()
                {
                    D d = (scoped {{(includeType ? "ReadOnlySpan<int> " : "")}}x) => { };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(ScopedKind.ScopedValue, symbol.Parameters.Single().ScopedKind);
        Assert.Equal("x", symbol.Parameters.Single().Name);
        Assert.Equal(compilation.GetTypeByMetadataName(typeof(ReadOnlySpan<>).FullName).GetPublicSymbol(), symbol.Parameters.Single().Type.OriginalDefinition);
    }

    [Fact]
    public void TestParamsOnNonParamsType()
    {
        var compilation = CreateCompilation("""
            delegate void D(int x);

            class C
            {
                void M()
                {
                    D d = (params x) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,16): error CS9272: Implicitly typed lambda parameter 'x' cannot have the 'params' modifier.
                //         D d = (params x) => { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedParamsParameter, "params").WithArguments("x").WithLocation(7, 16),
                // (7,16): error CS0225: The params parameter must have a valid collection type
                //         D d = (params x) => { };
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(7, 16));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.True(symbol.Parameters is [{ Name: "x", Type.SpecialType: SpecialType.System_Int32, IsParams: true }]);
    }

    [Fact]
    public void TestAccessibilityModifier1()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = (public ref x) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,16): error CS1525: Invalid expression term 'public'
                //         D d = (public ref x) => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "public").WithArguments("public").WithLocation(7, 16),
                // (7,16): error CS1026: ) expected
                //         D d = (public ref x) => { };
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "public").WithLocation(7, 16),
                // (7,16): error CS1002: ; expected
                //         D d = (public ref x) => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "public").WithLocation(7, 16),
                // (7,16): error CS1513: } expected
                //         D d = (public ref x) => { };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "public").WithLocation(7, 16),
                // (7,28): error CS1519: Invalid token ')' in class, record, struct, or interface member declaration
                //         D d = (public ref x) => { };
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ")").WithArguments(")").WithLocation(7, 28),
                // (7,28): error CS1519: Invalid token ')' in class, record, struct, or interface member declaration
                //         D d = (public ref x) => { };
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ")").WithArguments(")").WithLocation(7, 28),
                // (9,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(9, 1));
    }

    [Fact]
    public void TestAccessibilityModifier2()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = (ref public x) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,16): error CS1525: Invalid expression term 'ref'
                //         D d = (ref public x) => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref ").WithArguments("ref").WithLocation(7, 16),
                // (7,16): error CS1073: Unexpected token 'ref'
                //         D d = (ref public x) => { };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(7, 16),
                // (7,20): error CS1525: Invalid expression term 'public'
                //         D d = (ref public x) => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "public").WithArguments("public").WithLocation(7, 20),
                // (7,20): error CS1026: ) expected
                //         D d = (ref public x) => { };
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "public").WithLocation(7, 20),
                // (7,20): error CS1002: ; expected
                //         D d = (ref public x) => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "public").WithLocation(7, 20),
                // (7,20): error CS1513: } expected
                //         D d = (ref public x) => { };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "public").WithLocation(7, 20),
                // (7,28): error CS1519: Invalid token ')' in class, record, struct, or interface member declaration
                //         D d = (ref public x) => { };
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ")").WithArguments(")").WithLocation(7, 28),
                // (7,28): error CS1519: Invalid token ')' in class, record, struct, or interface member declaration
                //         D d = (ref public x) => { };
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ")").WithArguments(")").WithLocation(7, 28),
                // (9,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(9, 1));
    }

    [Fact]
    public void TestOneParameterWithReadonlyRef()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = (readonly ref x) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,16): error CS9190: 'readonly' modifier must be specified after 'ref'.
                //         D d = (readonly ref x) => { };
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(7, 16));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(RefKind.Ref, symbol.Parameters.Single().RefKind);
        Assert.Equal(SpecialType.System_Int32, symbol.Parameters.Single().Type.SpecialType);
    }

    [Fact]
    public void TestOneParameterWithRefReadonly1()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref int x);

            class C
            {
                void M()
                {
                    D d = (ref readonly x) => { };
                }
            }
            """).VerifyDiagnostics(
                // (7,15): warning CS9198: Reference kind modifier of parameter 'ref readonly int x' doesn't match the corresponding parameter 'ref int x' in target.
                //         D d = (ref readonly x) => { };
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "(ref readonly x) => { }").WithArguments("ref readonly int x", "ref int x").WithLocation(7, 15));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(RefKind.RefReadOnlyParameter, symbol.Parameters.Single().RefKind);
        Assert.Equal(SpecialType.System_Int32, symbol.Parameters.Single().Type.SpecialType);
    }

    [Fact]
    public void TestOneParameterWithRefReadonly2()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref readonly int x);

            class C
            {
                void M()
                {
                    D d = (ref readonly x) => { };
                }
            }
            """).VerifyDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(RefKind.RefReadOnlyParameter, symbol.Parameters.Single().RefKind);
        Assert.Equal(SpecialType.System_Int32, symbol.Parameters.Single().Type.SpecialType);
    }

    [Fact]
    public void TestOneParameterWithRefReadonly3()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref readonly int x);

            class C
            {
                void M()
                {
                    D d = (ref readonly x) =>
                    {
                        x = 0;
                    };
                }
            }
            """).VerifyDiagnostics(
                // (9,13): error CS8331: Cannot assign to variable 'x' or use it as the right hand side of a ref assignment because it is a readonly variable
                //             x = 0;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "x").WithArguments("variable", "x").WithLocation(9, 13));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(RefKind.RefReadOnlyParameter, symbol.Parameters.Single().RefKind);
        Assert.Equal(SpecialType.System_Int32, symbol.Parameters.Single().Type.SpecialType);
    }

    [Fact]
    public void TestOneParameterWithRefReadonly4()
    {
        var compilation = CreateCompilation("""
            delegate void D(ref readonly int x);

            class C
            {
                void M()
                {
                    D d = (ref x) =>
                    {
                        x = 0;
                    };
                }
            }
            """).VerifyDiagnostics(
                // (7,20): error CS1676: Parameter 1 must be declared with the 'ref readonly' keyword
                //         D d = (ref x) =>
                Diagnostic(ErrorCode.ERR_BadParamRef, "x").WithArguments("1", "ref readonly").WithLocation(7, 20));

        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

        var semanticModel = compilation.GetSemanticModel(tree);
        var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(lambda).Symbol!;

        Assert.Equal(MethodKind.LambdaMethod, symbol.MethodKind);
        Assert.Equal(RefKind.Ref, symbol.Parameters.Single().RefKind);
        Assert.Equal(SpecialType.None, symbol.Parameters.Single().Type.SpecialType);
    }
}
