// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class NullConditionalAwaitSemanticModelTests : CSharpTestBase
{
    // Tests here pin the SemanticModel / IOperation / flow-analysis behavior observable by
    // IDE features and analyzers on an `await? e` expression. Binding/emit correctness is
    // covered by the other NullConditionalAwait* test files; this file focuses on the public
    // SemanticModel APIs. The key invariant is that every API that reports the type of the
    // `await?` expression (GetTypeInfo, GetOperation / IAwaitOperation.Type,
    // GetDeclaredSymbol(var)) reports the same lifted result type the spec describes.

    private CSharpCompilation CreateCompilationUnderTest(string source)
    {
        return CreateCompilation(
            source,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.ReleaseDll.WithNullableContextOptions(NullableContextOptions.Enable),
            targetFramework: TargetFramework.NetCoreApp);
    }

    private static AwaitExpressionSyntax GetAwaitExpression(CSharpCompilation comp)
    {
        var tree = comp.SyntaxTrees.Single();
        return tree.GetRoot().DescendantNodes().OfType<AwaitExpressionSyntax>().Single();
    }

    #region GetTypeInfo

    [Fact]
    public void GetTypeInfo_TaskOfInt_ReturnsLiftedNullableInt()
    {
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var await_ = GetAwaitExpression(comp);
        var info = model.GetTypeInfo(await_);

        Assert.Equal("System.Int32?", info.Type.ToTestDisplayString());
        Assert.Equal("System.Int32?", info.ConvertedType.ToTestDisplayString());
    }

    [Fact]
    public void GetTypeInfo_TaskOfString_NRTEnabled_TypeIsAnnotatedAndFlowStateMaybeNull()
    {
        // Reference-type R (String). With NRT enabled, the public Type surfaces as annotated
        // ("String?") and the flow state is MaybeNull, reflecting the short-circuit.
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<string> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetTypeInfo(GetAwaitExpression(comp));

        Assert.Equal("System.String?", info.Type.ToTestDisplayString());
        Assert.Equal(CodeAnalysis.NullableAnnotation.Annotated, info.Nullability.Annotation);
        Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, info.Nullability.FlowState);
    }

    [Fact]
    public void GetTypeInfo_TaskOfString_NRTDisabled_TypeIsStringNotAnnotated()
    {
        // With NRT disabled, the Type is plain "String" (no annotation). The nullable-flow
        // analysis doesn't run in NRT-disabled contexts, so the flow state is None as well —
        // both are pinned so neither can regress silently.
        var source = """
            using System.Threading.Tasks;
            class C { async System.Threading.Tasks.Task M(Task<string> t) { var v = await? t; } }
            """;
        var comp = CreateCompilation(
            source,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.ReleaseDll.WithNullableContextOptions(NullableContextOptions.Disable),
            targetFramework: TargetFramework.NetCoreApp);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetTypeInfo(GetAwaitExpression(comp));

        Assert.Equal("System.String", info.Type.ToTestDisplayString());
        Assert.Equal(CodeAnalysis.NullableAnnotation.None, info.Nullability.Annotation);
        Assert.Equal(CodeAnalysis.NullableFlowState.None, info.Nullability.FlowState);
    }

    [Fact]
    public void GetTypeInfo_NullableValueTaskOfInt_ReturnsLiftedNullableInt()
    {
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(ValueTask<int>? t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetTypeInfo(GetAwaitExpression(comp));
        Assert.Equal("System.Int32?", info.Type.ToTestDisplayString());
        Assert.Equal("System.Int32?", info.ConvertedType!.ToTestDisplayString());
    }

    [Fact]
    public void GetTypeInfo_Task_IsVoid()
    {
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task t) { await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        Assert.Equal("System.Void", model.GetTypeInfo(GetAwaitExpression(comp)).Type.ToTestDisplayString());
    }

    [Fact]
    public void GetTypeInfo_InnerOperand_UnchangedByOuterQuestion()
    {
        // The `?` token belongs to the outer AwaitExpressionSyntax. The operand's type info
        // must not be affected by whether the outer await is null-conditional.
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var innerOperand = GetAwaitExpression(comp).Expression;
        var info = model.GetTypeInfo(innerOperand);
        Assert.Equal("System.Threading.Tasks.Task<System.Int32>", info.Type.ToTestDisplayString());
    }

    #endregion

    #region GetAwaitExpressionInfo

    [Fact]
    public void GetAwaitExpressionInfo_TaskOfInt_ResolvesOnTaskNotOnNullable()
    {
        // Operand type is Task<int> directly — awaitable pattern is on Task<int>, GetResult
        // returns int. The `?` doesn't change which GetAwaiter / GetResult symbols resolve.
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetAwaitExpressionInfo(GetAwaitExpression(comp));

        Assert.NotNull(info.GetAwaiterMethod);
        Assert.Equal("GetAwaiter", info.GetAwaiterMethod!.Name);
        Assert.Equal("System.Threading.Tasks.Task<System.Int32>", info.GetAwaiterMethod.ContainingType.ToTestDisplayString());
        Assert.False(info.GetAwaiterMethod.IsExtensionMethod);

        Assert.NotNull(info.GetResultMethod);
        Assert.Equal("GetResult", info.GetResultMethod!.Name);
        Assert.Equal("System.Int32", info.GetResultMethod.ReturnType.ToTestDisplayString());

        Assert.NotNull(info.IsCompletedProperty);
        Assert.Equal("IsCompleted", info.IsCompletedProperty!.Name);
        Assert.False(info.IsDynamic);
    }

    [Fact]
    public void GetAwaitExpressionInfo_NullableValueTaskOfInt_ResolvesOnUnderlyingV()
    {
        // Operand is Nullable<ValueTask<int>> — the binder strips Nullable<> and resolves
        // the awaitable pattern against ValueTask<int> (the underlying V). The API surfaces
        // that resolution unchanged. Assert every member so a bug that wires up the right
        // GetAwaiter but the wrong GetResult/IsCompleted doesn't slip through.
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(ValueTask<int>? t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetAwaitExpressionInfo(GetAwaitExpression(comp));

        Assert.NotNull(info.GetAwaiterMethod);
        Assert.Equal("System.Threading.Tasks.ValueTask<System.Int32>", info.GetAwaiterMethod!.ContainingType.ToTestDisplayString());
        Assert.NotNull(info.GetResultMethod);
        Assert.Equal("System.Int32", info.GetResultMethod!.ReturnType.ToTestDisplayString());
        Assert.Equal("System.Runtime.CompilerServices.ValueTaskAwaiter<System.Int32>", info.GetResultMethod.ContainingType.ToTestDisplayString());
        Assert.NotNull(info.IsCompletedProperty);
        Assert.Equal("System.Runtime.CompilerServices.ValueTaskAwaiter<System.Int32>", info.IsCompletedProperty!.ContainingType.ToTestDisplayString());
    }

    [Fact]
    public void GetTypeInfo_TaskOfAlreadyNullableInt_ResultIsInt_NotDoubleLifted()
    {
        // Spec Table B row: if R is already Nullable<V>, `await?` keeps it unchanged (no
        // double-lift). Pin via the public Type so a regression that eagerly wraps any R in
        // another Nullable<> would fail here.
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int?> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetTypeInfo(GetAwaitExpression(comp));
        Assert.Equal("System.Int32?", info.Type.ToTestDisplayString());

        var op = model.GetOperation(GetAwaitExpression(comp));
        Assert.Equal("System.Int32?", ((IAwaitOperation)op!).Type!.ToTestDisplayString());
    }

    [Fact]
    public void GetTypeInfo_UnconstrainedT_StatementPosition_DegradesToVoid()
    {
        // Spec: in statement position where a Nullable<T> return would be required on an
        // unconstrained T, the result type degrades to void (instead of erroring).
        var source = """
            using System.Threading.Tasks;
            class C { async Task M<T>(Task<T> t) { await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetTypeInfo(GetAwaitExpression(comp));
        Assert.Equal("System.Void", info.Type.ToTestDisplayString());
    }

    [Fact]
    public void GetTypeInfo_UnconstrainedT_ValuePosition_IsErrorType_WithDiagnostic()
    {
        // Mirror of the statement case but at value position: CS8978 fires because we can't
        // form `Nullable<T>`. Unlike CS9379 (which produces IInvalidOperation), this error
        // still produces an IAwaitOperation whose Type is an error type. Pin that shape so
        // analyzers know what to expect on this path.
        var source = """
            using System.Threading.Tasks;
            class C { async Task M<T>(Task<T> t) { var v = await? t; System.Console.WriteLine(v); } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics(
            // (2,53): error CS8978: 'T' cannot be made nullable.
            // class C { async Task M<T>(Task<T> t) { var v = await? t; System.Console.WriteLine(v); } }
            Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, "?").WithArguments("T").WithLocation(2, 53));

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var op = model.GetOperation(GetAwaitExpression(comp));
        var awaitOp = Assert.IsAssignableFrom<IAwaitOperation>(op);
        Assert.Equal(TypeKind.Error, awaitOp.Type!.TypeKind);
    }

    [Fact]
    public void GetAwaitExpressionInfo_ConfigureAwaitFalseOnValueTask_IsRejected()
    {
        // `await? task.ConfigureAwait(false)` is the classic ConfigureAwait misuse when the
        // task is non-nullable. The operand is a ConfiguredTaskAwaitable (struct), which
        // triggers CS9379. The await info should be default/empty (binding produced a bad
        // expression).
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task t) { await? t.ConfigureAwait(false); } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics(
            // (2,39): error CS9379: 'await?' cannot be applied to an operand of non-nullable value type 'ConfiguredTaskAwaitable'.
            // class C { async Task M(Task t) { await? t.ConfigureAwait(false); } }
            Diagnostic(ErrorCode.ERR_AwaitConditionalNonNullableValueType, "?").WithArguments("System.Runtime.CompilerServices.ConfiguredTaskAwaitable").WithLocation(2, 39));

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetAwaitExpressionInfo(GetAwaitExpression(comp));
        Assert.Null(info.GetAwaiterMethod);
        Assert.Null(info.GetResultMethod);
        Assert.Null(info.IsCompletedProperty);
    }

    [Fact]
    public void GetAwaitExpressionInfo_Dynamic_IsDynamicFlag()
    {
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(dynamic d) { var v = await? d; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetAwaitExpressionInfo(GetAwaitExpression(comp));
        Assert.True(info.IsDynamic);
    }

    [Fact]
    public void GetAwaitExpressionInfo_ExtensionGetAwaiterOnUnderlyingV()
    {
        // Confirms the public API sees the extension method, consistent with the regular
        // Nullable<V>-operand extension case.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            struct S { }
            struct SA : INotifyCompletion
            {
                public bool IsCompleted => true;
                public int GetResult() => 0;
                public void OnCompleted(Action a) { }
            }
            static class Ext { public static SA GetAwaiter(this S s) => default; }

            class C { async Task M(S? s) { var v = await? s; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();
        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetAwaitExpressionInfo(GetAwaitExpression(comp));
        Assert.NotNull(info.GetAwaiterMethod);
        Assert.True(info.GetAwaiterMethod!.IsExtensionMethod);
        Assert.Equal("Ext", info.GetAwaiterMethod.ContainingType.ToTestDisplayString());
    }

    [Fact]
    public void GetAwaitExpressionInfo_OperandNullabilityError_IsDefault()
    {
        // `await?` on a non-nullable value type (e.g. ValueTask) is rejected by the binder
        // with CS9379 before any awaitable-pattern resolution. GetAwaitExpressionInfo returns
        // default (all members null / false).
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(ValueTask vt) { await? vt; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics(
            // (2,45): error CS9379: 'await?' cannot be applied to an operand of non-nullable value type 'ValueTask'.
            Diagnostic(ErrorCode.ERR_AwaitConditionalNonNullableValueType, "?").WithArguments("System.Threading.Tasks.ValueTask").WithLocation(2, 45));

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetAwaitExpressionInfo(GetAwaitExpression(comp));
        Assert.Null(info.GetAwaiterMethod);
        Assert.Null(info.GetResultMethod);
        Assert.Null(info.IsCompletedProperty);
    }

    #endregion

    #region GetSymbolInfo

    [Fact]
    public void GetSymbolInfo_OnAwaitExpression_IsEmpty()
    {
        // `await` expressions have no symbol associated with the overall expression; only
        // the awaitable pattern members (reported via GetAwaitExpressionInfo) do.
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetSymbolInfo(GetAwaitExpression(comp));
        Assert.Null(info.Symbol);
        Assert.Empty(info.CandidateSymbols);
    }

    [Fact]
    public void GetSymbolInfo_OnInnerOperand_ResolvesParameter()
    {
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var info = model.GetSymbolInfo(GetAwaitExpression(comp).Expression);
        Assert.NotNull(info.Symbol);
        Assert.Equal("t", info.Symbol!.Name);
        Assert.Equal(SymbolKind.Parameter, info.Symbol.Kind);
    }

    #endregion

    #region GetOperation / IAwaitOperation

    [Fact]
    public void GetOperation_ReturnsIAwaitOperationWithLiftedType()
    {
        // IAwaitOperation does not expose IsNullConditional (the IOperation API is
        // intentionally flat on this point — see IOperationTests_IAwaitExpression.cs for the
        // full shape). What the public API *does* surface is the result type, which reflects
        // the lift.
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var op = model.GetOperation(GetAwaitExpression(comp));
        var awaitOp = Assert.IsAssignableFrom<IAwaitOperation>(op);
        Assert.Equal(OperationKind.Await, awaitOp.Kind);
        Assert.Equal("System.Int32?", awaitOp.Type!.ToTestDisplayString());
        Assert.NotNull(awaitOp.Operation);
        Assert.Equal("System.Threading.Tasks.Task<System.Int32>", awaitOp.Operation.Type!.ToTestDisplayString());
    }

    [Fact]
    public void GetOperation_AwaitVsAwaitQuestion_TreeShapeIdentical_OnlyTypeDiffers()
    {
        // Intentional parity pin. Analyzers walking the IOperation tree can only distinguish
        // `await` from `await?` via `IAwaitOperation.Type` or the underlying
        // `AwaitExpressionSyntax.QuestionToken`. Exposing an `IsNullConditional` flag on
        // IAwaitOperation is a separate design question (see the matching note in
        // IOperationTests_IAwaitExpression.cs).
        var source = """
            using System.Threading.Tasks;
            class C
            {
                async Task M1(Task<int> t) { var v = await  t; }
                async Task M2(Task<int> t) { var v = await? t; }
            }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var awaits = comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<AwaitExpressionSyntax>().ToArray();

        var plainOp = (IAwaitOperation)model.GetOperation(awaits[0])!;
        var nullCondOp = (IAwaitOperation)model.GetOperation(awaits[1])!;

        Assert.Equal(plainOp.Kind, nullCondOp.Kind);
        Assert.Equal(plainOp.Operation.Kind, nullCondOp.Operation.Kind);
        // Plain await returns int; null-conditional lifts to int?.
        Assert.Equal("System.Int32", plainOp.Type!.ToTestDisplayString());
        Assert.Equal("System.Int32?", nullCondOp.Type!.ToTestDisplayString());
    }

    #endregion

    #region GetDeclaredSymbol

    [Fact]
    public void GetDeclaredSymbol_OfVarLocal_InheritsLiftedType()
    {
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var declarator = comp.SyntaxTrees.Single().GetRoot().DescendantNodes()
            .OfType<VariableDeclaratorSyntax>().Single();
        var local = (ILocalSymbol)model.GetDeclaredSymbol(declarator)!;
        Assert.Equal("System.Int32?", local.Type.ToTestDisplayString());
    }

    #endregion

    #region Conversions

    [Fact]
    public void ClassifyConversion_LiftedResultToObject_IsImplicitReference()
    {
        // The int? the `await?` produces can be assigned to `object` (boxing).
        // Use a multi-line source so the warning location doesn't shift if the test harness's
        // preceding bytes change.
        var source = """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task<int> t) { object o = await? t; }
            }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics(
            // (4,44): warning CS8600: Converting null literal or possible null value to non-nullable type.
            //     async Task M(Task<int> t) { object o = await? t; }
            Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "await? t").WithLocation(4, 44));

        var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var await_ = GetAwaitExpression(comp);
        var typeInfo = model.GetTypeInfo(await_);
        var conversion = model.GetConversion(await_);

        Assert.Equal("System.Int32?", typeInfo.Type.ToTestDisplayString());
        Assert.Equal("System.Object?", typeInfo.ConvertedType!.ToTestDisplayString());
        Assert.True(conversion.IsImplicit);
        Assert.True(conversion.IsBoxing);
    }

    #endregion

    #region Flow analysis

    [Fact]
    public void AnalyzeDataFlow_Succeeds_OverAwaitQuestion()
    {
        // Data-flow analysis over a statement containing `await? t` should succeed and see
        // `t` as read. This is a smoke test that the new expression form doesn't break the
        // flow-analysis region builder.
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { var v = await? t; System.Console.WriteLine(v); } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var block = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().First();
        var analysis = model.AnalyzeDataFlow(block);

        Assert.True(analysis.Succeeded);
        Assert.Contains(analysis.ReadInside, s => s.Name == "t");
        Assert.Contains(analysis.VariablesDeclared, s => s.Name == "v");
    }

    [Fact]
    public void AnalyzeDataFlow_AwaitQuestion_AsCallArgument_ArgumentsReadInside()
    {
        // `await?` as a call argument is a common spilling scenario. Data-flow must still
        // see both arguments as read inside the region.
        var source = """
            using System.Threading.Tasks;
            class C
            {
                static void F(int a, int? b) { }
                async Task M(Task<int> t, int a) { F(a, await? t); }
            }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single(n => ((IdentifierNameSyntax)n.Expression).Identifier.Text == "F");
        var analysis = model.AnalyzeDataFlow(call);

        Assert.True(analysis.Succeeded);
        Assert.Contains(analysis.ReadInside, s => s.Name == "a");
        Assert.Contains(analysis.ReadInside, s => s.Name == "t");
    }

    [Fact]
    public void AnalyzeDataFlow_AwaitQuestion_InTryBlock_Succeeds()
    {
        // Flow analysis through a try/catch that contains `await?` should succeed and see
        // the operand as read inside the try.
        var source = """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task<int> t)
                {
                    try
                    {
                        var v = await? t;
                        System.Console.WriteLine(v);
                    }
                    catch { }
                }
            }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var tryBlock = tree.GetRoot().DescendantNodes().OfType<TryStatementSyntax>().Single().Block;
        var analysis = model.AnalyzeDataFlow(tryBlock);
        Assert.True(analysis.Succeeded);
        Assert.Contains(analysis.ReadInside, s => s.Name == "t");
    }

    [Fact]
    public void AnalyzeControlFlow_Succeeds_OverAwaitQuestionBlock()
    {
        // Smoke test: control-flow analysis over a block containing `await?` should
        // succeed. The IOperation-level CFG doesn't expose the short-circuit as a distinct
        // branch (see the matching CFG test in IOperationTests_IAwaitExpression.cs).
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var block = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().First();
        var analysis = model.AnalyzeControlFlow(block);
        Assert.True(analysis.Succeeded);
        Assert.Empty(analysis.ExitPoints);
    }

    #endregion

    #region Position-based queries

    [Fact]
    public void GetEnclosingSymbol_AtQuestionToken_ReturnsEnclosingMethod()
    {
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var questionToken = GetAwaitExpression(comp).QuestionToken;
        Assert.Equal(SyntaxKind.QuestionToken, questionToken.Kind());

        var enclosing = model.GetEnclosingSymbol(questionToken.SpanStart);
        Assert.Equal("M", enclosing!.Name);
        Assert.Equal(SymbolKind.Method, enclosing.Kind);
    }

    [Fact]
    public void LookupSymbols_AtAwaitPosition_SeesParameterInScope()
    {
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { var v = await? t; } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var symbols = model.LookupSymbols(GetAwaitExpression(comp).SpanStart);
        Assert.Contains(symbols, s => s.Name == "t");
    }

    #endregion

    #region Speculative binding

    [Fact]
    public void GetSpeculativeTypeInfo_NullConditionalAwaitInAsyncContext()
    {
        // Speculative binding rebuilds the expression from scratch; confirm it still applies
        // the result-type rule (lifts int to int?). Note that SyntaxFactory.ParseExpression
        // doesn't parse `await? t` as an AwaitExpression (await isn't a keyword outside an
        // async context), so construct the syntax directly.
        var source = """
            using System.Threading.Tasks;
            class C { async Task M(Task<int> t) { int x = 0; System.Console.WriteLine(x); } }
            """;
        var comp = CreateCompilationUnderTest(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var xInit = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().Single();
        var speculated = SyntaxFactory.AwaitExpression(
            SyntaxFactory.Token(SyntaxKind.AwaitKeyword),
            SyntaxFactory.Token(SyntaxKind.QuestionToken),
            SyntaxFactory.IdentifierName("t"));

        var info = model.GetSpeculativeTypeInfo(xInit.Value.SpanStart, speculated, SpeculativeBindingOption.BindAsExpression);
        Assert.Equal("System.Int32?", info.Type!.ToTestDisplayString());
    }

    #endregion

    #region Debug / PDB emit

    [Fact]
    public void DebugEmit_WithAwaitQuestion_ProducesValidPdb()
    {
        // Smoke test: compile in Debug mode, emit with PDB, and verify no emit
        // diagnostics. Exercises that `AwaitExpressionSyntax.QuestionToken` does not
        // trip up sequence-point emission.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public static async Task<int?> F(Task<int> t) => await? t;
            }
            """;
        var comp = CreateCompilation(
            source,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.DebugDll,
            targetFramework: TargetFramework.NetCoreApp);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void DebugEmit_WithAwaitQuestionInTryFinally_ProducesValidPdb()
    {
        // Debug-emit smoke test with await? inside EH structure, which historically
        // stresses sequence-point emission more than straight-line code.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public static async Task F(Task<int> t)
                {
                    try
                    {
                        int? v = await? t;
                    }
                    finally
                    {
                        System.Console.WriteLine("finally");
                    }
                }
            }
            """;
        var comp = CreateCompilation(
            source,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.DebugDll,
            targetFramework: TargetFramework.NetCoreApp);
        comp.VerifyEmitDiagnostics();
    }

    #endregion

    #region NRT flow through long ?. chains interacting with await?

    [Fact]
    public void NRTFlow_LongChainBeforeAwaitQuestion_ReceiverFlowStateSurvives()
    {
        // After `a?.Next?.Next?.Next`, the walker narrows `a` to non-null on the chain's
        // continuation. When the await? then operates on the chain result, the outer
        // expression's flow state is MaybeNull (both chain short-circuit and await?
        // short-circuit contribute null). Pin that a subsequent use of `a` (outside the
        // chain) still carries the narrowing from the chain entry.
        var source = """
            using System.Threading.Tasks;
            class Node { public Node? Next; public Task<int> Work() => Task.FromResult(0); }
            class C
            {
                public static void Consume(Node a) { _ = a.Next; }
                public async Task M(Node a)
                {
                    if (a is null) return;
                    int? v = await? a?.Next?.Next?.Work();
                    // After the `if (a is null) return;` above, `a` is narrowed to non-null
                    // for the rest of the method. Passing it to a non-null parameter must
                    // not warn.
                    Consume(a);
                    System.Console.WriteLine(v);
                }
            }
            """;
        var comp = CreateCompilation(
            source,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.ReleaseDll.WithNullableContextOptions(NullableContextOptions.Enable),
            targetFramework: TargetFramework.NetCoreApp);
        comp.VerifyDiagnostics(
            // (2,27): warning CS0649: Field 'Node.Next' is never assigned to, and will always have its default value null
            // class Node { public Node? Next; public Task<int> Work() => Task.FromResult(0); }
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Next").WithArguments("Node.Next", "null").WithLocation(2, 27));
    }

    [Fact]
    public void NRTFlow_AwaitQuestionResultAnnotation_FlowsIntoCoalesce()
    {
        // `(await? taskOfString) ?? "default"` — the coalesce operator takes the MaybeNull
        // left side and narrows to a non-null string. Passing that to a non-nullable
        // parameter should produce no warning.
        var source = """
            using System.Threading.Tasks;
            class C
            {
                public static void Consume(string s) { _ = s.Length; }
                public async Task M(Task<string> t)
                {
                    string s = (await? t) ?? "default";
                    Consume(s);
                }
            }
            """;
        var comp = CreateCompilation(
            source,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.ReleaseDll.WithNullableContextOptions(NullableContextOptions.Enable),
            targetFramework: TargetFramework.NetCoreApp);
        comp.VerifyDiagnostics();
    }

    #endregion
}
