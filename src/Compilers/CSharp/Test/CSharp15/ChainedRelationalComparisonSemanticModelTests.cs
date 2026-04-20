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
/// Tests the SemanticModel / IOperation / flow-analysis shape of a chained
/// relational comparison (spec §11.11.13, preview feature) — the behaviour IDE
/// features and analyzers observe. Binding and emit correctness are covered by
/// <c>ChainedRelationalComparisonTests</c>, <c>ChainedRelationalComparisonEmitTests</c>,
/// <c>ChainedRelationalComparisonConstantFoldingTests</c>,
/// <c>ChainedRelationalComparisonNullableAnalysisTests</c>, and
/// <c>ChainedRelationalComparisonControlFlowTests</c>; this file focuses on the
/// public <see cref="SemanticModel"/> APIs those earlier files don't already
/// exercise (or don't exhaustively pin).
///
/// Key invariant pinned across the file: for a chained node <c>A op B</c> the
/// symbol reported by <see cref="SemanticModel.GetSymbolInfo(SyntaxNode, System.Threading.CancellationToken)"/>
/// is the <em>outer-link</em> operator <c>Y op B</c>, signed by the outer link's
/// left-operand type (stored in <c>BoundBinaryOperator.ChainedRelationalLeftConvertedType</c>),
/// NOT by <c>Left.Type</c> (which is always <see langword="bool"/> — the inner
/// link's result type). A regression to the old behaviour would surface here
/// as a synthesized <c>bool op_LessThan(bool, ...)</c> symbol.
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

    /// <summary>
    /// Return the chain's <see cref="BinaryExpressionSyntax"/> nodes in
    /// innermost-to-outermost order. For <c>a &lt; b &lt; c</c> returns
    /// <c>[a&lt;b, a&lt;b&lt;c]</c>; for <c>a &lt; b &lt; c &lt; d</c> returns
    /// <c>[a&lt;b, a&lt;b&lt;c, a&lt;b&lt;c&lt;d]</c>.
    /// </summary>
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
        // The canonical case: both links resolve to the built-in `int < int`
        // operator. A regression to the old behaviour (outer mapped off
        // BoundBinaryOperator.Left.Type) would surface here as
        // `bool <(bool, int)` on the outer link - a symbol the runtime has
        // no implementation for.
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
        // The outer link's overload resolution sees the shared middle (Y,
        // aka `b`) as the inner link classified it. For `int < short < long`
        // the inner resolves to `int < int` (short widens to int). The outer
        // then resolves against Y=int vs Right=long, selecting `long < long`
        // and adding a second widening on Y. Pin both signatures; previously
        // the outer would have reported `<(bool, long)` via Left.Type.
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
        // Same as above with a different permutation. The inner link's
        // signature ends up `int < int` (both operands widen from short/int
        // to int); the outer ends up `long < long`.
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
        // `a <= b < c` - each link carries its own op name. Both links are
        // `(int, int)` here, but the Name differs per link.
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
        // 4-operand chain `int < short < int < long` - each BinaryExpression
        // in the spine has its own signature. Inner link: int<int. Middle
        // link: int<int. Outer link: long<long. A per-level outer-vs-Left
        // confusion would flip any of the three.
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
        // Lifted `int? < int? < int?`. Roslyn's synthesized built-in
        // operator symbol reports the STRIPPED (non-nullable) operand
        // types - matching GetSymbolInfo for a non-chained `int? < int?` -
        // and surfaces the lifting via IBinaryOperation.IsLifted at the
        // operation level. Both links look the same here; the previous
        // buggy outer-link synthesis would have used Left.Type (bool) and
        // produced a non-existent `bool op_LessThan(bool, int?)`.
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

            // Lifting is tracked on the operation itself.
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
        // C# 11 static-abstract-in-interfaces: T is constrained to an
        // interface that declares `<`. Both links bind to the constrained
        // interface method, with ContainingType pointing at the interface.
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
        // The chain interpretation requires `Y op B` to resolve to a
        // bool-returning operator; when it cannot, we report
        // ERR_NoChainedRelationalComparison and the outer binary has no
        // bound operator. GetSymbolInfo on the outer link should reflect
        // that cleanly (no symbol) rather than leaking a stale intrinsic.
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

        // Inner link still resolves successfully: int < int.
        var inner = (IMethodSymbol?)model.GetSymbolInfo(spine[0]).Symbol;
        Assert.NotNull(inner);
        Assert.Equal("op_LessThan", inner.Name);

        // Outer link has no usable operator. Symbol is null, CandidateReason
        // identifies the failure shape.
        var outer = model.GetSymbolInfo(spine[1]);
        Assert.Null(outer.Symbol);
    }

    [Fact]
    public void GetSymbolInfo_ClassicallyBoundAlternative_DoesNotChain()
    {
        // When ordinary binary operator overload resolution succeeds at the
        // outer level the chain interpretation is NOT applied (spec §11.11.13
        // is a fallback). A user-defined `<` whose Left accepts bool keeps
        // its classical meaning, so GetSymbolInfo on the outer link reports
        // the user-defined operator, not a chained Y-op-B signature.
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
        // Left param is bool - proof we took the classical `(bool, S)`
        // overload, not the chain interpretation (which would have picked
        // `(S, S)` via Y-op-B).
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
        // Lifted relational operators propagate null by returning false,
        // not by returning bool?. The chain's overall type must be bool.
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
        // The chain's result is bool, but the surrounding context converts
        // it to bool?; GetTypeInfo's Type vs ConvertedType reflects that.
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
        // In `int < short < long`, the middle `b` is a short parameter.
        // Syntactically `b` appears once (as the Right of the inner
        // binary); the chain's conversion stack applies at the operator,
        // not on the identifier. GetTypeInfo(b) reports Type=short (the
        // declared type). ConvertedType is the inner-link classification
        // (int) because that's what the inner `<` converted `b` to.
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
        // The outer syntax's IOperation is a chained IBinaryOperation
        // whose internal IsChainedRelationalComparison flag is set. The
        // Left operand is the inner IBinaryOperation (not chained).
        var source = """
            class P { static bool M(int a, int b, int c) => a < b < c; }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var spine = GetChainSpineInnermostToOutermost(comp);

        var outerOp = (IBinaryOperation?)model.GetOperation(spine[1]);
        Assert.NotNull(outerOp);
        // Outer's LeftOperand IS the inner link's IBinaryOperation.
        Assert.IsAssignableFrom<IBinaryOperation>(outerOp.LeftOperand);
        // Outer's Type is bool.
        Assert.Equal("System.Boolean", outerOp.Type!.ToTestDisplayString());

        var innerOp = (IBinaryOperation)outerOp.LeftOperand;
        // The inner link produced from model.GetOperation on the inner
        // syntax is the SAME bound node that lives as outer.LeftOperand.
        // (Bound tree reuse - no duplication.)
        Assert.Same(innerOp, model.GetOperation(spine[0]));
        // Inner's Left is the parameter `a`, not another IBinaryOperation.
        Assert.IsAssignableFrom<IParameterReferenceOperation>(innerOp.LeftOperand);
    }

    [Fact]
    public void GetOperation_SharedMiddleOperand_IsInnerOperationsRightOperand()
    {
        // The shared middle operand's IOperation is reachable both as the
        // inner link's RightOperand AND (equivalently) through the chain's
        // pointer (BoundBinaryOperator.ChainedRelationalLeftOperand, which
        // the public IOperation model surfaces via the standard tree
        // because it's the same node). Pin this by checking they're the
        // same instance.
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

        // The inner RightOperand is `b`.
        Assert.Equal("b", ((IParameterReferenceOperation)innerOp.RightOperand).Parameter.Name);

        // Asking GetOperation on `b`'s identifier syntax returns the same node.
        var bIdentifier = tree.GetRoot().DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .First(n => n.Identifier.ValueText == "b");
        Assert.Same(innerOp.RightOperand, model.GetOperation(bIdentifier));
    }

    [Fact]
    public void GetOperation_IdenticalTreeShape_ToNonChainedBinaryPlusShortCircuit()
    {
        // Pin that at the public-IOperation surface a chained node reads
        // just like any other IBinaryOperation - it does NOT expose a
        // different OperationKind. IDE tools that pattern-match on
        // OperationKind.Binary will continue to see a binary op; only
        // consumers that care about the short-circuit nuance check the
        // (internal) IsChainedRelationalComparison flag.
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
        // Pull the statement matching the /*<bind>*/.../*</bind>*/ markers.
        // Mirrors the CompilationUtils pattern but invoked per-test since
        // we often want more precise narrowing than that helper provides.
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
        // The chain reads all three operands and writes only the target
        // local; nothing else should appear in ReadInside / WrittenInside.
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
        // A `b = expr` inside the middle operand is observable as an inside
        // write: the chain evaluates the middle exactly once, and if control
        // reaches the outer link (i.e. the inner was true) the assignment
        // definitively ran. DefiniteAssignment elsewhere pins the full
        // behaviour; this test pins only the raw read/write sets.
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
        // An `if (chain) { ... }` treats the chain the same way it would
        // treat any other bool condition - no chain-specific special casing
        // at the data-flow API level.
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
        // Control flow over `if (a<b<c) { return 1; }` has the standard
        // one-entry / one-return-exit shape of any if-with-return; the
        // chain's internal short-circuit is modelled inside the condition
        // expression, not as extra control flow on the statement.
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
        // Even when one of the operands has observable side effects that
        // may or may not run (short-circuit), the statement-level control
        // flow still looks the same; the short-circuit is internal to the
        // expression, not a statement-level branch.
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
        // Start with a non-chain expression, ask the speculative semantic
        // model about a fresh `a < b < c` at the same position. The
        // speculative result must match what the non-speculative API
        // returns for the corresponding syntax at that position.
        var source = """
            class P { static bool M(int a, int b, int c) { return a < b; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var original = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

        var speculative = (BinaryExpressionSyntax)SyntaxFactory.ParseExpression("a < b < c");
        var speculativeOuter = speculative; // `a < b < c` is its own outer

        // Speculative symbol info on the outer link reports the outer
        // operator using ChainedRelationalLeftConvertedType, not the
        // inner BoundBinaryOperator.Left.Type.
        var info = model.GetSpeculativeSymbolInfo(original.SpanStart, speculativeOuter, SpeculativeBindingOption.BindAsExpression);
        var method = (IMethodSymbol?)info.Symbol;
        Assert.NotNull(method);
        Assert.Equal("op_LessThan", method.Name);
        Assert.Equal(["System.Int32", "System.Int32"], method.Parameters.Select(p => p.Type.ToTestDisplayString()).ToArray());
    }

    [Fact]
    public void Speculative_GetTypeInfo_OnNewChain_ReportsBool()
    {
        // Same setup: at a position where the original compilation had
        // some other expression, ask what the type of a chain would be.
        // Result is bool, like every other chain in this file.
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
