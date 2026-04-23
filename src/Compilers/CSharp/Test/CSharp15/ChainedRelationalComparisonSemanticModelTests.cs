// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

/// <summary>
/// SemanticModel / IOperation / flow-analysis tests for "chained relational
/// comparison" (C# preview feature; spec §11.11.13). The core invariant is that
/// <see cref="SemanticModel.GetSymbolInfo(SyntaxNode, System.Threading.CancellationToken)"/>
/// on a chained node reports the outer-link <c>Y op B</c> operator, not a
/// synthesized <c>bool op_LessThan(bool, ...)</c> built off the inner link's
/// bool result type.
/// </summary>
public sealed class ChainedRelationalComparisonSemanticModelTests : CSharpTestBase
{
    private static CSharpCompilation CreateCompilationUnderTest(string source, TargetFramework targetFramework = TargetFramework.Standard)
    {
        return CreateCompilation(
            source,
            parseOptions: TestOptions.RegularPreview,
            targetFramework: targetFramework);
    }

    // Return the chain's BinaryExpressionSyntax nodes innermost-to-outermost.
    private static BinaryExpressionSyntax[] GetChainSpineInnermostToOutermost(CSharpCompilation comp)
    {
        var tree = comp.SyntaxTrees.Single();
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<BinaryExpressionSyntax>()
            .Where(b => SyntaxFacts.IsChainableRelationalExpression(b.Kind()))
            .OrderBy(b => b.SpanStart)
            .ThenBy(b => b.Span.Length)
            .ToArray();
    }

    #region GetSymbolInfo

    [Fact]
    public void GetSymbolInfo_SameTypeIntrinsicInt_InnerAndOuterBothMapToIntLessThan()
    {
        // Both links resolve to built-in `int < int`.
        var source = """
            class P { static bool M(int a, int b, int c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);
        Assert.Equal(2, spine.Length);

        foreach (var binary in spine)
        {
            var info = model.GetSymbolInfo(binary);
            var method = (IMethodSymbol?)info.Symbol;
            Assert.NotNull(method);
            Assert.Equal(MethodKind.BuiltinOperator, method.MethodKind);
            Assert.Equal("op_LessThan", method.Name);
            Assert.Equal("System.Boolean", method.ReturnType.ToTestDisplayString());
            Assert.Equal(["System.Int32", "System.Int32"], method.Parameters.Select(p => p.Type.ToTestDisplayString()).ToArray());
            Assert.Equal("System.Int32", method.ContainingType.ToTestDisplayString());
        }
    }

    [Fact]
    public void GetSymbolInfo_AsymmetricIntShortLong_InnerIsIntIntOuterIsLongLong()
    {
        // Inner resolves as `int < int`; outer resolves as `long < long`.
        var source = """
            class P { static bool M(int a, short b, long c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        var inner = (IMethodSymbol?)model.GetSymbolInfo(spine[0]).Symbol;
        Assert.NotNull(inner);
        Assert.Equal(["System.Int32", "System.Int32"], inner.Parameters.Select(p => p.Type.ToTestDisplayString()).ToArray());
        Assert.Equal("System.Int32", inner.ContainingType.ToTestDisplayString());

        var outer = (IMethodSymbol?)model.GetSymbolInfo(spine[1]).Symbol;
        Assert.NotNull(outer);
        Assert.Equal(["System.Int64", "System.Int64"], outer.Parameters.Select(p => p.Type.ToTestDisplayString()).ToArray());
        Assert.Equal("System.Int64", outer.ContainingType.ToTestDisplayString());
    }

    [Fact]
    public void GetSymbolInfo_AsymmetricShortIntLong_InnerIsIntIntOuterIsLongLong()
    {
        // Different permutation: inner `int < int`, outer `long < long`.
        var source = """
            class P { static bool M(short a, int b, long c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        var inner = (IMethodSymbol?)model.GetSymbolInfo(spine[0]).Symbol;
        Assert.NotNull(inner);
        Assert.Equal(["System.Int32", "System.Int32"], inner.Parameters.Select(p => p.Type.ToTestDisplayString()).ToArray());

        var outer = (IMethodSymbol?)model.GetSymbolInfo(spine[1]).Symbol;
        Assert.NotNull(outer);
        Assert.Equal(["System.Int64", "System.Int64"], outer.Parameters.Select(p => p.Type.ToTestDisplayString()).ToArray());
    }

    [Fact]
    public void GetSymbolInfo_MixedOperators_EachLinkKeepsItsOwnOperatorName()
    {
        // Each link carries its own operator name.
        var source = """
            class P { static bool M(int a, int b, int c) => a <= b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        Assert.Equal("op_LessThanOrEqual", ((IMethodSymbol?)model.GetSymbolInfo(spine[0]).Symbol)!.Name);
        Assert.Equal("op_LessThan", ((IMethodSymbol?)model.GetSymbolInfo(spine[1]).Symbol)!.Name);
    }

    [Fact]
    public void GetSymbolInfo_NAryChain_EachLevelReportsItsOwnSignature()
    {
        // 4-operand chain: each spine node has its own signature.
        var source = """
            class P { static bool M(int a, short b, int c, long d) => a < b < c < d; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);
        Assert.Equal(3, spine.Length);

        string[] expectedParams = ["System.Int32", "System.Int32", "System.Int64"];
        for (int i = 0; i < spine.Length; i++)
        {
            var method = (IMethodSymbol?)model.GetSymbolInfo(spine[i]).Symbol;
            Assert.NotNull(method);
            Assert.Equal("op_LessThan", method.Name);
            Assert.Equal(expectedParams[i], method.Parameters[0].Type.ToTestDisplayString());
            Assert.Equal(expectedParams[i], method.Parameters[1].Type.ToTestDisplayString());
        }
    }

    [Fact]
    public void GetSymbolInfo_LiftedNullableIntrinsic_ReportsUnderlyingParametersWithIsLiftedOnOperation()
    {
        // Lifted chain: symbol's params are stripped to non-nullable; IsLifted lives on the operation.
        var source = """
            class P { static bool M(int? a, int? b, int? c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        foreach (var binary in spine)
        {
            var method = (IMethodSymbol?)model.GetSymbolInfo(binary).Symbol;
            Assert.NotNull(method);
            Assert.Equal(MethodKind.BuiltinOperator, method.MethodKind);
            Assert.Equal("op_LessThan", method.Name);
            Assert.Equal("System.Boolean", method.ReturnType.ToTestDisplayString());
            Assert.Equal(["System.Int32", "System.Int32"], method.Parameters.Select(p => p.Type.ToTestDisplayString()).ToArray());

            var op = (IBinaryOperation?)model.GetOperation(binary);
            Assert.NotNull(op);
            Assert.True(op.IsLifted);
        }
    }

    [Fact]
    public void GetSymbolInfo_UserDefinedOperator_BothLinksResolveToSameUserMethod()
    {
        var source = """
            struct S
            {
                public static bool operator <(S a, S b) => false;
                public static bool operator >(S a, S b) => false;
            }
            class P { static bool M(S a, S b, S c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        foreach (var binary in spine)
        {
            var method = (IMethodSymbol?)model.GetSymbolInfo(binary).Symbol;
            Assert.NotNull(method);
            Assert.Equal(MethodKind.UserDefinedOperator, method.MethodKind);
            Assert.Equal("op_LessThan", method.Name);
            Assert.Equal("S", method.ContainingType.ToTestDisplayString());
        }
    }

    [Fact]
    public void GetSymbolInfo_GenericConstrainedOperator_BothLinksResolveToConstrainedInterfaceMethod()
    {
        // Constrained-to-interface-with-static-abstract-`<`: both links bind to the interface method.
        var source = """
            interface ILt<TSelf> where TSelf : ILt<TSelf>
            {
                static abstract bool operator <(TSelf x, TSelf y);
                static abstract bool operator >(TSelf x, TSelf y);
            }
            class P
            {
                static bool M<T>(T a, T b, T c) where T : ILt<T> => a < b < c;
            }
            """;
        var comp = CreateCompilationUnderTest(source, TargetFramework.NetCoreApp);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        foreach (var binary in spine)
        {
            var method = (IMethodSymbol?)model.GetSymbolInfo(binary).Symbol;
            Assert.NotNull(method);
            Assert.Equal(MethodKind.UserDefinedOperator, method.MethodKind);
            Assert.Equal("op_LessThan", method.Name);
            Assert.Equal("ILt<T>", method.ContainingType.ToTestDisplayString());
        }
    }

    [Fact]
    public void GetSymbolInfo_OnChainFallbackThatFailsResolution_ReportsNoSymbol()
    {
        // Outer link has no bool-returning operator -> GetSymbolInfo yields no symbol.
        var source = """
            class P { static bool M(int a, int b, string c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics(
            // (1,58): error CS9380: Operator '<' cannot be chained from 'int' to 'string'.
            // class P { static bool M(int a, int b, string c) => a < b < c; }
            Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "int", "string").WithLocation(1, 58));

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        var inner = (IMethodSymbol?)model.GetSymbolInfo(spine[0]).Symbol;
        Assert.NotNull(inner);
        Assert.Equal("op_LessThan", inner.Name);

        var outer = model.GetSymbolInfo(spine[1]);
        Assert.Null(outer.Symbol);
    }

    [Fact]
    public void GetSymbolInfo_ClassicallyBoundAlternative_DoesNotChain()
    {
        // When classical binding succeeds, the chain interpretation is not applied.
        var source = """
            struct S
            {
                public static bool operator <(bool a, S b) => false;
                public static bool operator >(bool a, S b) => false;
                public static bool operator <(S a, S b) => false;
                public static bool operator >(S a, S b) => false;
            }
            class P { static bool M(S a, S b, S c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        var outer = (IMethodSymbol?)model.GetSymbolInfo(spine[1]).Symbol;
        Assert.NotNull(outer);
        // Left param `bool` -> classical `(bool, S)` overload, not the chain's `(S, S)`.
        Assert.Equal("System.Boolean", outer.Parameters[0].Type.ToTestDisplayString());
        Assert.Equal("S", outer.Parameters[1].Type.ToTestDisplayString());
    }

    #endregion GetSymbolInfo

    #region GetTypeInfo

    [Fact]
    public void GetTypeInfo_IntrinsicInt_ChainTypeIsBool()
    {
        var source = """
            class P { static bool M(int a, int b, int c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        // Both links produce bool.
        foreach (var binary in spine)
        {
            var info = model.GetTypeInfo(binary);
            Assert.Equal("System.Boolean", info.Type.ToTestDisplayString());
            Assert.Equal("System.Boolean", info.ConvertedType.ToTestDisplayString());
        }
    }

    [Fact]
    public void GetTypeInfo_LiftedNullable_ChainTypeIsStillBoolNotNullableBool()
    {
        // Lifted relational operators return bool (not bool?).
        var source = """
            class P { static bool M(int? a, int? b, int? c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        foreach (var binary in spine)
        {
            var info = model.GetTypeInfo(binary);
            Assert.Equal("System.Boolean", info.Type.ToTestDisplayString());
        }
    }

    [Fact]
    public void GetTypeInfo_OnChainInNullableBoolContext_ConvertedTypeIsNullableBool()
    {
        // Chain's Type is bool; ConvertedType is bool? in a nullable-bool context.
        var source = """
            class P { static bool? M(int a, int b, int c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        var outerInfo = model.GetTypeInfo(spine[1]);
        Assert.Equal("System.Boolean", outerInfo.Type.ToTestDisplayString());
        Assert.Equal("System.Boolean?", outerInfo.ConvertedType.ToTestDisplayString());
    }

    [Fact]
    public void GetTypeInfo_SharedMiddleOperand_TypeIsItsDeclaredType_ConvertedTypeIsInnerLinkClassification()
    {
        // Middle `b`'s Type is its declared `short`; ConvertedType is the inner link's `int`.
        var source = """
            class P { static bool M(int a, short b, long c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var middleRef = tree.GetRoot().DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Single(n => n.Identifier.ValueText == "b");

        var info = model.GetTypeInfo(middleRef);
        Assert.Equal("System.Int16", info.Type.ToTestDisplayString());
        Assert.Equal("System.Int32", info.ConvertedType.ToTestDisplayString());
    }

    #endregion GetTypeInfo

    #region GetOperation (IOperation)

    [Fact]
    public void GetOperation_OnOuterSyntax_ReturnsChainedIBinaryOperation()
    {
        // Outer is an IBinaryOperation whose LeftOperand is the inner IBinaryOperation.
        var source = """
            class P { static bool M(int a, int b, int c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        var outerOp = (IBinaryOperation?)model.GetOperation(spine[1]);
        Assert.NotNull(outerOp);
        Assert.IsAssignableFrom<IBinaryOperation>(outerOp.LeftOperand);
        Assert.Equal("System.Boolean", outerOp.Type!.ToTestDisplayString());

        var innerOp = (IBinaryOperation)outerOp.LeftOperand;
        // Same instance regardless of which syntax we query - no duplicate bound nodes.
        Assert.Same(innerOp, model.GetOperation(spine[0]));
        Assert.IsAssignableFrom<IParameterReferenceOperation>(innerOp.LeftOperand);
    }

    [Fact]
    public void GetOperation_SharedMiddleOperand_IsInnerOperationsRightOperand()
    {
        // Middle operand's IOperation is the same instance whether reached via inner.RightOperand or GetOperation(b).
        var source = """
            class P { static bool M(int a, int b, int c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var spine = GetChainSpineInnermostToOutermost(comp);

        var outerOp = (IBinaryOperation)model.GetOperation(spine[1])!;
        var innerOp = (IBinaryOperation)outerOp.LeftOperand;

        Assert.Equal("b", ((IParameterReferenceOperation)innerOp.RightOperand).Parameter.Name);

        var bIdentifier = tree.GetRoot().DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .First(n => n.Identifier.ValueText == "b");
        Assert.Same(innerOp.RightOperand, model.GetOperation(bIdentifier));
    }

    [Fact]
    public void GetOperation_IdenticalTreeShape_ToNonChainedBinaryPlusShortCircuit()
    {
        // Chained nodes still report `OperationKind.Binary` on the public surface.
        var source = """
            class P { static bool M(int a, int b, int c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        foreach (var binary in spine)
        {
            var op = model.GetOperation(binary);
            Assert.NotNull(op);
            Assert.Equal(OperationKind.Binary, op.Kind);
        }
    }

    #endregion GetOperation (IOperation)

    #region AnalyzeDataFlow

    private static T GetStatementWithBindMarker<T>(CSharpCompilation comp) where T : StatementSyntax
    {
        // Extract the statement bracketed by /*<bind>*/.../*</bind>*/ markers.
        var tree = comp.SyntaxTrees.Single();
        var source = tree.GetText().ToString();
        int start = source.IndexOf("/*<bind>*/", System.StringComparison.Ordinal) + "/*<bind>*/".Length;
        int end = source.IndexOf("/*</bind>*/", System.StringComparison.Ordinal);
        var statement = tree.GetRoot().DescendantNodes()
            .OfType<T>()
            .First(n => n.SpanStart >= start && n.Span.End <= end);
        return statement;
    }

    [Fact]
    public void AnalyzeDataFlow_ChainAssignedToLocal_ReadsAllOperandsWritesOnlyLocal()
    {
        var source = """
            class P
            {
                static void M(int a, int b, int c)
                {
                    /*<bind>*/bool r = a < b < c;/*</bind>*/
                }
            }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var stmt = GetStatementWithBindMarker<LocalDeclarationStatementSyntax>(comp);
        var analysis = model.AnalyzeDataFlow(stmt);

        Assert.Equal("a, b, c", string.Join(", ", analysis.ReadInside.Select(s => s.Name)));
        Assert.Equal("r", string.Join(", ", analysis.WrittenInside.Select(s => s.Name)));
        Assert.Equal("r", string.Join(", ", analysis.VariablesDeclared.Select(s => s.Name)));
    }

    [Fact]
    public void AnalyzeDataFlow_ChainWithAssignmentInMiddleOperand_AssignmentIsCaptured()
    {
        // Assignment inside the middle operand shows up as an inside write.
        var source = """
            class P
            {
                static void M(int a, int c)
                {
                    int b;
                    /*<bind>*/bool r = a < (b = 5) < c;/*</bind>*/
                    _ = b;
                }
            }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var stmt = GetStatementWithBindMarker<LocalDeclarationStatementSyntax>(comp);
        var analysis = model.AnalyzeDataFlow(stmt);

        var writes = analysis.WrittenInside.Select(s => s.Name).ToArray();
        Assert.Contains("b", writes);
        Assert.Contains("r", writes);
        Assert.Equal("a, c", string.Join(", ", analysis.ReadInside.Select(s => s.Name)));
    }

    [Fact]
    public void AnalyzeDataFlow_ChainInsideIf_TrackedLikeAnyBoolCondition()
    {
        var source = """
            class P
            {
                static int M(int a, int b, int c)
                {
                    /*<bind>*/if (a < b < c) { return 1; }/*</bind>*/
                    return 0;
                }
            }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var stmt = GetStatementWithBindMarker<IfStatementSyntax>(comp);
        var analysis = model.AnalyzeDataFlow(stmt);

        Assert.Equal("a, b, c", string.Join(", ", analysis.ReadInside.Select(s => s.Name)));
        Assert.Empty(analysis.WrittenInside);
    }

    #endregion AnalyzeDataFlow

    #region AnalyzeControlFlow

    [Fact]
    public void AnalyzeControlFlow_ChainAsIfCondition_HasReturnExit()
    {
        // Chain's short-circuit is internal to the expression; statement-level flow is unchanged.
        var source = """
            class P
            {
                static int M(int a, int b, int c)
                {
                    /*<bind>*/if (a < b < c) { return 1; }/*</bind>*/
                    return 0;
                }
            }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var stmt = GetStatementWithBindMarker<IfStatementSyntax>(comp);
        var analysis = model.AnalyzeControlFlow(stmt);

        Assert.Empty(analysis.EntryPoints);
        Assert.Single(analysis.ExitPoints);
        Assert.True(analysis.StartPointIsReachable);
        Assert.True(analysis.EndPointIsReachable);
    }

    [Fact]
    public void AnalyzeControlFlow_ChainWithShortCircuitingSideEffectInOperand_StillSingleStatementFlow()
    {
        var source = """
            class P
            {
                static int Side() => 5;
                static int M(int a, int c)
                {
                    /*<bind>*/if (a < Side() < c) { return 1; }/*</bind>*/
                    return 0;
                }
            }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var stmt = GetStatementWithBindMarker<IfStatementSyntax>(comp);
        var analysis = model.AnalyzeControlFlow(stmt);

        Assert.Empty(analysis.EntryPoints);
        Assert.Single(analysis.ExitPoints);
        Assert.True(analysis.EndPointIsReachable);
    }

    #endregion AnalyzeControlFlow

    #region Speculative binding

    [Fact]
    public void Speculative_GetSymbolInfo_OnNewChainReplacingOldExpression_ReportsOuterOperator()
    {
        // Speculatively bind a fresh chain at a non-chain position.
        var source = """
            class P { static bool M(int a, int b, int c) { return a < b; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var original = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

        var speculative = (BinaryExpressionSyntax)SyntaxFactory.ParseExpression("a < b < c");
        var info = model.GetSpeculativeSymbolInfo(original.SpanStart, speculative, SpeculativeBindingOption.BindAsExpression);
        var method = (IMethodSymbol?)info.Symbol;
        Assert.NotNull(method);
        Assert.Equal("op_LessThan", method.Name);
        Assert.Equal(["System.Int32", "System.Int32"], method.Parameters.Select(p => p.Type.ToTestDisplayString()).ToArray());
    }

    [Fact]
    public void Speculative_GetTypeInfo_OnNewChain_ReportsBool()
    {
        var source = """
            class P { static bool M(int a, int b, int c) { return a < b; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var original = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

        var speculative = (BinaryExpressionSyntax)SyntaxFactory.ParseExpression("a < b < c");
        var info = model.GetSpeculativeTypeInfo(original.SpanStart, speculative, SpeculativeBindingOption.BindAsExpression);

        Assert.Equal("System.Boolean", info.Type.ToTestDisplayString());
    }

    #endregion Speculative binding
}
