// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class CompoundAssignmentInitializerSemanticModelTests : CSharpTestBase
{
    // SemanticModel / IOperation surface for compound member initializers (`new C { P += 1 }`,
    // `r with { P += 1 }`, `new C { P ??= "x" }`, `new C { E += h }`, etc.). Binding/lowering/emit
    // correctness lives in the other CompoundAssignmentInitializer* test files; this file pins the
    // public SemanticModel APIs IDE features and analyzers depend on. Full IOperation tree pins
    // live in the canonical IOperationTests_IObjectCreationExpression.cs.

    private static readonly string IsExternalInitPolyfill = IsExternalInitTypeDefinition;

    private static AssignmentExpressionSyntax GetMemberAssignment(CSharpCompilation comp)
    {
        var tree = comp.SyntaxTrees.Single();
        // The "member assignment" is the one directly inside an `{ ... }` initializer body — the
        // helper is robust to other AssignmentExpressions in the source (e.g. constructor bodies
        // or method bodies setting fields).
        return tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>()
            .Single(a => a.Parent is InitializerExpressionSyntax);
    }

    private static IdentifierNameSyntax GetLhsIdentifier(CSharpCompilation comp, string name)
    {
        var tree = comp.SyntaxTrees.Single();
        return tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
            .Single(n => n.Identifier.ValueText == name
                && n.Parent is AssignmentExpressionSyntax { Left: var left } && left == n);
    }

    #region GetOperation — operation kind dispatch

    [Fact]
    public void GetOperation_PropertyCompound_IsCompoundAssignmentOperation()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var compound = Assert.IsAssignableFrom<ICompoundAssignmentOperation>(
            model.GetOperation(GetMemberAssignment(comp))!);
        Assert.Equal(Operations.BinaryOperatorKind.Add, compound.OperatorKind);
        Assert.Equal("P", Assert.IsAssignableFrom<IPropertyReferenceOperation>(compound.Target).Property.Name);
        Assert.Equal(5, Assert.IsAssignableFrom<ILiteralOperation>(compound.Value).ConstantValue.Value);
    }

    [Fact]
    public void GetOperation_IndexerCompound_TargetIsIndexerPropertyReference()
    {
        var source = """
            class C
            {
                public int this[int i] { get => 0; set { } }
                public static C Make() => new C { [0] += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var compound = Assert.IsAssignableFrom<ICompoundAssignmentOperation>(
            model.GetOperation(GetMemberAssignment(comp))!);
        var target = Assert.IsAssignableFrom<IPropertyReferenceOperation>(compound.Target);
        Assert.True(target.Property.IsIndexer);
        Assert.Single(target.Arguments);
        Assert.Equal(0, Assert.IsAssignableFrom<ILiteralOperation>(target.Arguments[0].Value).ConstantValue.Value);
    }

    [Fact]
    public void GetOperation_EventPlusEquals_IsEventAssignmentOperation()
    {
        // `+=` on an event projects as IEventAssignmentOperation (Adds=true), not a flattened
        // ICompoundAssignmentOperation.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler h) => new C { E += h };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31));
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var eventAssignment = Assert.IsAssignableFrom<IEventAssignmentOperation>(
            model.GetOperation(GetMemberAssignment(comp))!);
        Assert.True(eventAssignment.Adds);
        Assert.Equal("E", Assert.IsAssignableFrom<IEventReferenceOperation>(eventAssignment.EventReference).Event.Name);
    }

    [Fact]
    public void GetOperation_EventMinusEquals_AddsIsFalse()
    {
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler h) => new C { E -= h };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31));
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var eventAssignment = Assert.IsAssignableFrom<IEventAssignmentOperation>(
            model.GetOperation(GetMemberAssignment(comp))!);
        Assert.False(eventAssignment.Adds);
    }

    [Fact]
    public void GetOperation_PropertyCoalesceEquals_IsCoalesceAssignmentOperation()
    {
        // `??=` projects as ICoalesceAssignmentOperation (distinct OperationKind from compound).
        var source = """
            class C
            {
                public string P { get; set; }
                public static C Make() => new C { P ??= "x" };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var coalesce = Assert.IsAssignableFrom<ICoalesceAssignmentOperation>(
            model.GetOperation(GetMemberAssignment(comp))!);
        Assert.Equal("P", Assert.IsAssignableFrom<IPropertyReferenceOperation>(coalesce.Target).Property.Name);
        Assert.Equal("x", Assert.IsAssignableFrom<ILiteralOperation>(coalesce.Value).ConstantValue.Value);
    }

    [Fact]
    public void GetOperation_WithExpressionCompound_ProjectsViaIWithOperation()
    {
        // `with`-expression initializer members still project as ICompoundAssignmentOperation under
        // IWithOperation (parity with `new`).
        var source = """
            record R(int P)
            {
                public static R Make(R r) => r with { P += 5 };
            }
            """;
        var comp = CreateCompilation([source, IsExternalInitPolyfill]);
        comp.VerifyDiagnostics();
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var withExpr = tree.GetRoot().DescendantNodes().OfType<WithExpressionSyntax>().Single();
        var withOp = Assert.IsAssignableFrom<IWithOperation>(model.GetOperation(withExpr)!);
        Assert.NotNull(withOp.Initializer);
        var compound = Assert.IsAssignableFrom<ICompoundAssignmentOperation>(withOp.Initializer.Initializers.Single());
        Assert.Equal("P", Assert.IsAssignableFrom<IPropertyReferenceOperation>(compound.Target).Property.Name);
    }

    #endregion

    #region GetSymbolInfo

    [Fact]
    public void GetSymbolInfo_OnBuiltInPropertyCompound_IsBuiltInOperator()
    {
        // `P += 5` on `int P` uses the built-in `+` operator; `GetSymbolInfo` on the assignment
        // returns the synthetic `int.operator +(int, int)` method symbol.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var info = model.GetSymbolInfo(GetMemberAssignment(comp));
        Assert.Equal("int.operator +(int, int)", info.Symbol!.ToString());
    }

    [Fact]
    public void GetSymbolInfo_OnUserDefinedPlusEquals_IsTheUserDefinedOperator()
    {
        // For a user-defined `operator +`, `GetSymbolInfo` on the compound returns that operator
        // method.
        var source = """
            struct V
            {
                public int X;
                public V(int x) => X = x;
                public static V operator +(V a, V b) => new V(a.X + b.X);
            }
            class C
            {
                public V P { get; set; }
                public static C Make() => new C { P += new V(1) };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var info = model.GetSymbolInfo(GetMemberAssignment(comp));
        var op = Assert.IsAssignableFrom<IMethodSymbol>(info.Symbol);
        Assert.Equal("op_Addition", op.Name);
        Assert.Equal("V", op.ContainingType.Name);
    }

    [Fact]
    public void GetSymbolInfo_OnLhsIdentifier_ResolvesProperty()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var info = model.GetSymbolInfo(GetLhsIdentifier(comp, "P"));
        Assert.Equal("P", info.Symbol!.Name);
        Assert.Equal(SymbolKind.Property, info.Symbol.Kind);
    }

    [Fact]
    public void GetSymbolInfo_OnLhsIdentifier_OfEventCompound_ResolvesEvent()
    {
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler h) => new C { E += h };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31));
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var info = model.GetSymbolInfo(GetLhsIdentifier(comp, "E"));
        Assert.Equal("E", info.Symbol!.Name);
        Assert.Equal(SymbolKind.Event, info.Symbol.Kind);
    }

    [Fact]
    public void GetSymbolInfo_OnRhs_ResolvesNormally()
    {
        // The compound's RHS binds in the enclosing context — the initializer placeholder receiver
        // is *only* available on the LHS. Pin that the RHS sees method-local context (the parameter
        // `h`, here) cleanly.
        var source = """
            using System;
            class C
            {
                public event EventHandler E;
                public static C Make(EventHandler h) => new C { E += h };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (4,31): warning CS0067: The event 'C.E' is never used
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31));
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var rhs = (IdentifierNameSyntax)GetMemberAssignment(comp).Right;
        var info = model.GetSymbolInfo(rhs);
        Assert.Equal("h", info.Symbol!.Name);
        Assert.Equal(SymbolKind.Parameter, info.Symbol.Kind);
    }

    [Fact]
    public void GetSymbolInfo_OnDynamicCompound_IsLateBoundOperator()
    {
        // Mirror of `CompoundAssignment` (SemanticModelGetSemanticInfoTests_LateBound.cs:808).
        // `P += d` with dynamic on both sides surfaces the late-bound
        // `dynamic.operator +(dynamic, dynamic)` symbol with `CandidateReason.LateBound`.
        var source = """
            class C
            {
                public dynamic P { get; set; }
                public static C Make(dynamic d) => new C { P += d };
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var info = model.GetSymbolInfo(GetMemberAssignment(comp));
        Assert.Equal("dynamic.operator +(dynamic, dynamic)", info.Symbol!.ToString());
        Assert.Equal(CandidateReason.LateBound, info.CandidateReason);
        Assert.Empty(info.CandidateSymbols);
    }

    #endregion

    #region GetTypeInfo

    [Fact]
    public void GetTypeInfo_OnPropertyCompound_IsTargetType()
    {
        // The compound's Type is the target's type (compound is a value-producing expression
        // whose value is the post-write value).
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var info = model.GetTypeInfo(GetMemberAssignment(comp));
        Assert.Equal(SpecialType.System_Int32, info.Type!.SpecialType);
        Assert.Equal(SpecialType.System_Int32, info.ConvertedType!.SpecialType);
    }

    [Fact]
    public void GetTypeInfo_OnLhsIdentifier_IsPropertyType()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var info = model.GetTypeInfo(GetLhsIdentifier(comp, "P"));
        Assert.Equal(SpecialType.System_Int32, info.Type!.SpecialType);
    }

    [Fact]
    public void GetTypeInfo_OnRhsLiteral_IsLiteralType()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var rhs = GetMemberAssignment(comp).Right;
        var info = model.GetTypeInfo(rhs);
        Assert.Equal(SpecialType.System_Int32, info.Type!.SpecialType);
    }

    [Fact]
    public void GetTypeInfo_OnCoalesceCompound_IsTargetType()
    {
        // `??=` on a string property — Type/ConvertedType are both `string`.
        var source = """
            class C
            {
                public string P { get; set; }
                public static C Make() => new C { P ??= "x" };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var info = model.GetTypeInfo(GetMemberAssignment(comp));
        Assert.Equal(SpecialType.System_String, info.Type!.SpecialType);
        Assert.Equal(SpecialType.System_String, info.ConvertedType!.SpecialType);
    }

    [Fact]
    public void GetTypeInfo_OnDynamicCompound_IsDynamic()
    {
        var source = """
            class C
            {
                public dynamic P { get; set; }
                public static C Make(dynamic d) => new C { P += d };
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var info = model.GetTypeInfo(GetMemberAssignment(comp));
        Assert.True(info.Type!.IsDynamic());
    }

    #endregion

    #region GetMemberGroup / LookupSymbols

    [Fact]
    public void GetMemberGroup_OnPropertyCompound_IsEmpty()
    {
        // Compound assignment isn't a method invocation; `GetMemberGroup` returns nothing.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        Assert.Empty(model.GetMemberGroup(GetMemberAssignment(comp)));
    }

    [Fact]
    public void LookupSymbols_AtLhsPosition_SeesContainerMembers()
    {
        // The LHS identifier is bound against the placeholder receiver (the implicit `this` of the
        // type being initialized). LookupSymbols at that position should see container members.
        var source = """
            class C
            {
                public int P { get; set; }
                public int Other { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var lhs = GetLhsIdentifier(comp, "P");
        var symbols = model.LookupSymbols(lhs.SpanStart, name: "Other");
        Assert.Single(symbols);
        Assert.Equal("Other", symbols[0].Name);
    }

    #endregion

    #region GetEnclosingSymbol / GetDeclaredSymbol

    [Fact]
    public void GetEnclosingSymbol_AtCompoundAssignment_ReturnsContainingMethod()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var enclosing = model.GetEnclosingSymbol(GetMemberAssignment(comp).SpanStart);
        Assert.Equal("Make", enclosing!.Name);
        Assert.Equal(SymbolKind.Method, enclosing.Kind);
    }

    [Fact]
    public void GetDeclaredSymbol_OnObjectCreation_IsNull()
    {
        // The wrapping `new C { ... }` doesn't *declare* a symbol; `GetDeclaredSymbol` returns null
        // (in contrast to `var c = new C { ... }` where the local declarator does declare one).
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var objectCreation = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
        Assert.Null(model.GetDeclaredSymbol(objectCreation));
    }

    #endregion

    #region GetConversion / ClassifyConversion

    [Fact]
    public void GetConversion_OnRhsImplicitConversion_IsImplicitNumeric()
    {
        // `byte += int`-typed RHS goes through an implicit numeric conversion. `GetConversion` on
        // the RHS expression reflects the conversion the compound applies.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make(short s) => new C { P += s };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var rhs = GetMemberAssignment(comp).Right;
        var conv = model.GetConversion(rhs);
        Assert.True(conv.IsImplicit);
        Assert.True(conv.IsNumeric);
    }

    [Fact]
    public void ClassifyConversion_AtRhsPosition_FromStringToInt_IsNoConversion()
    {
        // ClassifyConversion at the RHS position confirms `string` is not implicitly convertible
        // to `int`, even though the *actual* RHS in the source is convertible.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var rhs = GetMemberAssignment(comp).Right;
        ITypeSymbol intType = ((Compilation)comp).GetSpecialType(SpecialType.System_Int32);
        var conv = model.ClassifyConversion(rhs.SpanStart, SyntaxFactory.ParseExpression("\"abc\""), intType);
        Assert.False(conv.IsImplicit);
        var conv2 = model.ClassifyConversion(rhs.SpanStart, SyntaxFactory.ParseExpression("0"), intType);
        Assert.True(conv2.IsImplicit);
        Assert.True(conv2.IsIdentity);
    }

    #endregion

    #region AnalyzeDataFlow / AnalyzeControlFlow

    [Fact]
    public void AnalyzeDataFlow_OverInitializerWithCompound_SeesRhsParameterRead()
    {
        // The RHS `s` of the compound initializer is read inside the initializer expression. Pin
        // that AnalyzeDataFlow on the wrapping `var c = ...;` statement reports that read.
        var source = """
            class C
            {
                public int P { get; set; }
                public static void M(int s)
                {
                    var c = new C { P += s };
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var declStmt = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
        var flow = model.AnalyzeDataFlow(declStmt);
        Assert.Contains(flow.ReadInside, s => s.Name == "s");
        Assert.Contains(flow.WrittenInside, s => s.Name == "c");
    }

    [Fact]
    public void AnalyzeDataFlow_OverWithExpressionCompound_SeesReceiverRead()
    {
        var source = """
            record R(int P)
            {
                public static void M(R r)
                {
                    var r2 = r with { P += 5 };
                }
            }
            """;
        var comp = CreateCompilation([source, IsExternalInitPolyfill]);
        comp.VerifyDiagnostics();
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var declStmt = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
        var flow = model.AnalyzeDataFlow(declStmt);
        Assert.Contains(flow.ReadInside, s => s.Name == "r");
        Assert.Contains(flow.WrittenInside, s => s.Name == "r2");
    }

    [Fact]
    public void AnalyzeControlFlow_OverInitializerBlock_NoBranches()
    {
        // The initializer's compound members are sequential statement-expressions in the lowering;
        // the wrapping block has a single linear flow with no branches.
        var source = """
            class C
            {
                public int P { get; set; }
                public static void M()
                {
                    {
                        var c = new C { P += 5, P *= 2 };
                    }
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var innerBlock = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().Last();
        var flow = model.AnalyzeControlFlow(innerBlock);
        Assert.True(flow.Succeeded);
        Assert.Empty(flow.EntryPoints);
        Assert.Empty(flow.ExitPoints);
        Assert.Empty(flow.ReturnStatements);
    }

    [Fact]
    public void AnalyzeDataFlow_BadShape_DoesNotThrow()
    {
        // `P += { 1, 2 }` is invalid (nested-initializer RHS on compound). Public flow APIs must
        // not NRE on the bad-shape bound tree.
        var source = """
            class C
            {
                public int P { get; set; }
                public static void M()
                {
                    var c = new C { P += { 1, 2 } };
                }
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var declStmt = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
        // Just must not throw; no behavioral assertion.
        _ = model.AnalyzeDataFlow(declStmt);
    }

    #endregion

    #region Speculative binding

    [Fact]
    public void GetSpeculativeSymbolInfo_ReplacedRhs_ResolvesInOriginalContext()
    {
        // Speculate replacing the `5` in `new C { P += 5 }` with `Other`; the speculative bind
        // should see container member `Other` from the enclosing context.
        var source = """
            class C
            {
                public int P { get; set; }
                public int Other => 1;
                public C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var rhs = GetMemberAssignment(comp).Right;
        var newRhs = SyntaxFactory.ParseExpression("Other");
        var info = model.GetSpeculativeSymbolInfo(rhs.SpanStart, newRhs, SpeculativeBindingOption.BindAsExpression);
        Assert.Equal("Other", info.Symbol!.Name);
        Assert.Equal(SymbolKind.Property, info.Symbol.Kind);
    }

    [Fact]
    public void GetSpeculativeTypeInfo_ReplacedRhs_ReportsTypeOfReplacement()
    {
        var source = """
            class C
            {
                public int P { get; set; }
                public C Make() => new C { P += 5 };
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var rhs = GetMemberAssignment(comp).Right;
        var newRhs = SyntaxFactory.ParseExpression("\"abc\"");
        var info = model.GetSpeculativeTypeInfo(rhs.SpanStart, newRhs, SpeculativeBindingOption.BindAsExpression);
        Assert.Equal(SpecialType.System_String, info.Type!.SpecialType);
    }

    #endregion

    #region Bad-shape robustness

    [Fact]
    public void SemanticModelApis_OnBadShape_DoNotThrow()
    {
        // `P += { 1, 2 }` produces a bad-bound tree. All public SemanticModel calls on the
        // assignment and its operands must complete without exception.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += { 1, 2 } };
            }
            """;
        var comp = CreateCompilation(source);
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var assignment = GetMemberAssignment(comp);
        _ = model.GetOperation(assignment);
        _ = model.GetSymbolInfo(assignment);
        _ = model.GetTypeInfo(assignment);
        _ = model.GetMemberGroup(assignment);
        _ = model.GetSymbolInfo(assignment.Left);
        _ = model.GetSymbolInfo(assignment.Right);
        _ = model.GetTypeInfo(assignment.Left);
        _ = model.GetTypeInfo(assignment.Right);
    }

    #endregion
}
