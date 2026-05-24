// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class NullConditionalAwaitBindingTests : CSharpTestBase
{
    // Most tests here compile a single `var v = await? <operand>;` line inside an async
    // method and then ask the semantic model for the inferred type of `v`. That is the
    // user-visible way to observe what the result-type rule produced and avoids poking at
    // internal bound-tree state. NRT is enabled at the compilation level for every test that
    // cares about reference-type `?` annotations; tests that observe NRT output also assert
    // the NRT-disabled case to show the annotation isn't leaking through.

    private static string InAsyncMethod(string statement, string parameterList = "") => $$"""
        using System.Threading.Tasks;

        public class C
        {
            public async Task M({{parameterList}})
            {
                {{statement}}
            }
        }
        """;

    private CSharpCompilation CreateWithNullableReferenceTypesEnabled(
        string source,
        CSharpParseOptions? parseOptions = null,
        CSharpCompilationOptions? options = null,
        MetadataReference[]? references = null,
        TargetFramework targetFramework = TargetFramework.NetCoreApp)
    {
        return CreateCompilation(
            source,
            options: (options ?? TestOptions.ReleaseDll).WithNullableContextOptions(NullableContextOptions.Enable),
            parseOptions: parseOptions,
            references: references,
            targetFramework: targetFramework);
    }

    private CSharpCompilation CreateWithNullableReferenceTypesDisabled(
        string source,
        CSharpParseOptions? parseOptions = null,
        CSharpCompilationOptions? options = null,
        MetadataReference[]? references = null,
        TargetFramework targetFramework = TargetFramework.NetCoreApp)
    {
        return CreateCompilation(
            source,
            options: (options ?? TestOptions.ReleaseDll).WithNullableContextOptions(NullableContextOptions.Disable),
            parseOptions: parseOptions,
            references: references,
            targetFramework: targetFramework);
    }

    private static string TypeOfLocalSymbol(CSharpCompilation comp, string localName)
    {
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var declarator = tree.GetRoot().DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Single(d => d.Identifier.ValueText == localName);
        var local = (ILocalSymbol)model.GetDeclaredSymbol(declarator)!;
        return local.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
    }

    private static ITypeSymbol TypeOfAwaitExpression(CSharpCompilation comp)
    {
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var awaitExpr = tree.GetRoot().DescendantNodes().OfType<AwaitExpressionSyntax>().Single();
        return model.GetTypeInfo(awaitExpr).Type!;
    }

    #region Feature availability

    [Fact]
    public void FeatureAvailability_Preview_NoDiagnostic()
    {
        // `Task<int>` (not annotated) — the operand-nullability rule allows reference types
        // regardless of NRT annotation; the `?` still does the runtime null check.
        var source = InAsyncMethod("_ = await? t;", "Task<int> t");
        var comp = CreateWithNullableReferenceTypesEnabled(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void FeatureAvailability_CSharp14_Diagnostic()
    {
        // null-conditional-await is preview-only, so CS8652 (FeatureInPreview) fires for any
        // non-preview target language version.
        var source = InAsyncMethod("_ = await? t;", "Task<int> t");
        var comp = CreateWithNullableReferenceTypesEnabled(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp14));
        comp.VerifyDiagnostics(
            // (7,18): error CS8652: The feature 'null conditional await' is currently in Preview and *unsupported*.
            //         _ = await? t;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("null conditional await").WithLocation(7, 18));
    }

    #endregion

    #region Operand-nullability errors (non-nullable value-type operand)

    [Theory]
    [InlineData("ValueTask", "System.Threading.Tasks.ValueTask")]
    [InlineData("ValueTask<int>", "System.Threading.Tasks.ValueTask<int>")]
    // The generic argument being nullable doesn't change the outer operand: ValueTask<_> is
    // still a non-nullable value type, so the operand-nullability rule still rejects it.
    [InlineData("ValueTask<int?>", "System.Threading.Tasks.ValueTask<int?>")]
    public void OperandError_ConcreteValueTask(string type, string displayName)
    {
        var source = InAsyncMethod("await? t;", $"{type} t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,14): error CS9388: 'await?' cannot be applied to an operand of non-nullable value type '<type>'.
            //         await? t;
            Diagnostic(ErrorCode.ERR_AwaitConditionalNonNullableValueType, "?").WithArguments(displayName).WithLocation(7, 14));
    }

    [Fact]
    public void OperandError_UserStruct()
    {
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            public struct MyAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public int GetResult() => 0;
                public void OnCompleted(Action continuation) { }
            }

            public struct MyAwaitable
            {
                public MyAwaiter GetAwaiter() => default;
            }

            public class C
            {
                public async Task M(MyAwaitable t)
                {
                    await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (21,14): error CS9388: 'await?' cannot be applied to an operand of non-nullable value type 'MyAwaitable'.
            //         await? t;
            Diagnostic(ErrorCode.ERR_AwaitConditionalNonNullableValueType, "?").WithArguments("MyAwaitable").WithLocation(21, 14));
    }

    [Fact]
    public void OperandError_TypeParameterStructConstraint()
    {
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M<T>(T t) where T : struct
                {
                    await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (6,14): error CS9388: 'await?' cannot be applied to an operand of non-nullable value type 'T'.
            //         await? t;
            Diagnostic(ErrorCode.ERR_AwaitConditionalNonNullableValueType, "?").WithArguments("T").WithLocation(6, 14));
    }

    [Fact]
    public void OperandError_TypeParameterUnmanagedConstraint()
    {
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M<T>(T t) where T : unmanaged
                {
                    await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (6,14): error CS9388: 'await?' cannot be applied to an operand of non-nullable value type 'T'.
            //         await? t;
            Diagnostic(ErrorCode.ERR_AwaitConditionalNonNullableValueType, "?").WithArguments("T").WithLocation(6, 14));
    }

    #endregion

    #region Operand-nullability successes (result type via inferred local)

    [Fact]
    public void Operand_ReferenceTypeTask_ResultIsNothing()
    {
        // `await? (Task)t` has GetResult() -> void, classified as *nothing*. A `var v = ...`
        // declaration with void initializer is an error; test the statement-only form.
        var source = InAsyncMethod("await? t;", "Task t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    // Non-annotated Task operand — walker sees t as non-null; no NRT warning.
    [Fact] public void Operand_TaskOfInt_ResultIsNullableInt() => AssertTaskOfIntResultIsNullableInt("Task<int>");
    [Fact] public void Operand_TaskOfNullableInt_ResultIsNullableInt() => AssertTaskOfIntResultIsNullableInt("Task<int?>");

    private void AssertTaskOfIntResultIsNullableInt(string operandType)
    {
        // Operand-nullability rule accepts the (non-annotated) reference-type operand;
        // the result-type rule lifts non-nullable value-type R to Nullable<R> and leaves
        // already-nullable Nullable<V> unchanged. Both permutations produce int? for v,
        // in both NRT-on and NRT-off contexts.
        var source = InAsyncMethod("var v = await? t;", $"{operandType} t");

        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(compNrtOn, "v"));

        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    // NRT-annotated Task operand. In NRT-off mode the outer `?` annotation on the parameter
    // type is invalid outside an NRT context, so CS8632 fires; in NRT-on mode no warning
    // fires because the whole point of `await?` is to accept a null receiver. Either way
    // the result type is int?.
    [Fact]
    public void Operand_NrtAnnotatedTaskOfInt_ResultIsNullableInt()
    {
        var source = InAsyncMethod("var v = await? t;", "Task<int>? t");

        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(compNrtOn, "v"));

        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics(
            // (5,34): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(5, 34));
        Assert.Equal("int?", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    [Fact]
    public void Operand_NrtAnnotatedTaskOfNullableInt_ResultIsNullableInt()
    {
        var source = InAsyncMethod("var v = await? t;", "Task<int?>? t");

        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(compNrtOn, "v"));

        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics(
            // (5,35): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(5, 35));
        Assert.Equal("int?", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    // Reference-type R = string. The result-type rule leaves R unchanged; in NRT-on mode
    // the short-circuit flow state (MaybeDefault) surfaces as the NRT `?` annotation on
    // the local's type. In NRT-off mode annotations are oblivious, so v is just `string`.

    [Fact]
    public void Operand_TaskOfString_ResultIsAnnotatedString()
    {
        var source = InAsyncMethod("var v = await? t;", "Task<string> t");

        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        Assert.Equal("string?", TypeOfLocalSymbol(compNrtOn, "v"));
        Assert.Equal(CodeAnalysis.NullableAnnotation.Annotated, TypeOfAwaitExpression(compNrtOn).NullableAnnotation);

        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics();
        Assert.Equal("string", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    [Fact]
    public void Operand_TaskOfNullableString_ResultIsAnnotatedString()
    {
        var source = InAsyncMethod("var v = await? t;", "Task<string?> t");

        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        Assert.Equal("string?", TypeOfLocalSymbol(compNrtOn, "v"));

        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics(
            // (5,36): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(5, 36));
        Assert.Equal("string", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    [Fact]
    public void Operand_NrtAnnotatedTaskOfString_ResultIsAnnotatedString()
    {
        var source = InAsyncMethod("var v = await? t;", "Task<string>? t");

        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        Assert.Equal("string?", TypeOfLocalSymbol(compNrtOn, "v"));

        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics(
            // (5,37): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(5, 37));
        Assert.Equal("string", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    [Fact]
    public void Operand_NrtAnnotatedTaskOfNullableString_ResultIsAnnotatedString()
    {
        var source = InAsyncMethod("var v = await? t;", "Task<string?>? t");

        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        Assert.Equal("string?", TypeOfLocalSymbol(compNrtOn, "v"));

        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics(
            // (5,36): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(5, 36),
            // (5,38): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(5, 38));
        Assert.Equal("string", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    [Fact]
    public void Operand_NullableOfValueTask_ResultIsNothing()
    {
        // Nullable<ValueTask>: GetResult() -> void; classification is *nothing* in statement position.
        var source = InAsyncMethod("await? t;", "ValueTask? t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Operand_NullableOfValueTaskOfInt_ResultIsNullableInt()
    {
        // Nullable<ValueTask<int>>: GetResult() -> int; lifted to Nullable<int> regardless of NRT.
        var source = InAsyncMethod("var v = await? t;", "ValueTask<int>? t");

        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(compNrtOn, "v"));

        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    [Fact]
    public void Operand_TypeParameter_ClassConstraint_AwaitableInstance()
    {
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M<T>(T t) where T : Task<int>
                {
                    var v = await? t;
                }
            }
            """;

        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        // R is int (from Task<int>.GetAwaiter().GetResult()), lifted to int?.
        Assert.Equal("int?", TypeOfLocalSymbol(compNrtOn, "v"));

        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    [Fact]
    public void Operand_TypeParameter_Unconstrained_NoGetAwaiter()
    {
        // An unconstrained type parameter operand is accepted by the applicability rule.
        // Whether the awaitable pattern resolves on `T` depends on whether T has a GetAwaiter,
        // which unconstrained T does not. Expect the standard awaitable-pattern error on the
        // operand, not an operand-nullability error on `?`.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M<T>(T t)
                {
                    await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (6,16): error CS1061: 'T' does not contain a definition for 'GetAwaiter'...
            //         await? t;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "await? t").WithArguments("T", "GetAwaiter").WithLocation(6, 9));
    }

    [Fact]
    public void Operand_Dynamic()
    {
        // NetCoreApp's reference set already contains Microsoft.CSharp, which provides the
        // runtime binder for dynamic await; no extra reference is needed.
        var source = InAsyncMethod("var v = await? t;", "dynamic t");

        // The result-type rule leaves `dynamic` unchanged. The null-conditional short-circuit
        // means the NRT flow-state is "may-default", which surfaces as `dynamic?` on the local
        // in NRT-enabled mode.
        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        Assert.Equal("dynamic?", TypeOfLocalSymbol(compNrtOn, "v"));

        // In NRT-disabled mode the annotation is suppressed and `v` is just `dynamic`.
        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics();
        Assert.Equal("dynamic", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    #endregion

    #region Result-type rule

    [Fact]
    public void ResultType_UnconstrainedTypeParameterResult_ValuePosition_CannotBeMadeNullable()
    {
        // Here the OPERAND is Task<T> (a reference type — allowed), but the AWAITER's
        // GetResult returns T (an unconstrained type parameter). That result type cannot
        // be made nullable, so using the await in value position is an error.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M<T>(Task<T> t)
                {
                    var v = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (6,22): error CS8978: 'T' cannot be made nullable.
            //         var v = await? t;
            Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, "?").WithArguments("T").WithLocation(6, 22));
    }

    [Fact]
    public void ResultType_UnconstrainedTypeParameterResult_StatementPosition_OK()
    {
        // Same operand (Task<T>) but used at statement position. Per the spec, unused
        // unconditional-T results degrade to void in statement position instead of
        // reporting CS8978.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M<T>(Task<T> t)
                {
                    await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void ResultType_TypeParameterStructConstraintResult_Lifted()
    {
        // Result R = T where T : struct. Lifted to Nullable<T>. Value-type lifting is
        // independent of NRT.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M<T>(Task<T> t) where T : struct
                {
                    var v = await? t;
                }
            }
            """;

        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        Assert.Equal("T?", TypeOfLocalSymbol(compNrtOn, "v"));

        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics();
        Assert.Equal("T?", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    [Fact]
    public void ResultType_TypeParameterClassConstraintResult_AnnotatedReference()
    {
        // Result R = T where T : class. Reference-type R stays as T; the short-circuit
        // nullability surfaces as NRT annotation T? only when NRT is enabled.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M<T>(Task<T> t) where T : class
                {
                    var v = await? t;
                }
            }
            """;

        var compNrtOn = CreateWithNullableReferenceTypesEnabled(source);
        compNrtOn.VerifyDiagnostics();
        Assert.Equal("T?", TypeOfLocalSymbol(compNrtOn, "v"));
        Assert.Equal(CodeAnalysis.NullableAnnotation.Annotated, TypeOfAwaitExpression(compNrtOn).NullableAnnotation);

        // With NRT disabled, no annotation: v is just T.
        var compNrtOff = CreateWithNullableReferenceTypesDisabled(source);
        compNrtOff.VerifyDiagnostics();
        Assert.Equal("T", TypeOfLocalSymbol(compNrtOff, "v"));
    }

    [Fact]
    public void ResultType_VoidResult_ValuePosition_IsStillError()
    {
        // Task's GetResult returns void. `await? task` therefore has void classification
        // (the result-type rule treats a void R as "no representable null" which degrades to
        // void in statement position). Using such a void-typed expression as the initializer
        // for `int x` is the usual void-to-int conversion error.
        var source = InAsyncMethod("int x = await? t;", "Task t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,17): error CS0029: Cannot implicitly convert type 'void' to 'int'
            //         int x = await? t;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "await? t").WithArguments("void", "int").WithLocation(7, 17));
    }

    #endregion

    #region Existing await-context restrictions still fire

    [Fact]
    public void ContextError_OutsideAsync_ParsesAsTernary()
    {
        // In a non-async method `await` is an identifier, and `await? t;` parses as a ternary
        // `await ? t : <missing>`. This is a deliberate parser choice so pre-existing code that
        // uses `await` as an identifier near `?` continues to parse the same way. The binder
        // therefore never sees an AwaitExpression here, so the usual "can only be used in async"
        // diagnostic doesn't apply; instead we get the ternary-shaped syntax errors plus the
        // lookup failure for the `await` identifier.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public void M(Task<int> t)
                {
                    var v = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (6,17): error CS0103: The name 'await' does not exist in the current context
            Diagnostic(ErrorCode.ERR_NameNotInContext, "await").WithArguments("await").WithLocation(6, 17),
            // (6,25): error CS1003: Syntax error, ':' expected
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(6, 25),
            // (6,25): error CS1525: Invalid expression term ';'
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 25));
    }

    [Fact]
    public void ContextError_InLock()
    {
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M(Task<int> t)
                {
                    object gate = new();
                    lock (gate)
                    {
                        var v = await? t;
                    }
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (9,21): error CS1996: Cannot await in the body of a lock statement
            //             var v = await? t;
            Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await? t").WithLocation(9, 21));
    }

    [Fact]
    public void ContextError_InUnsafe()
    {
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M(Task<int> t)
                {
                    unsafe
                    {
                        var v = await? t;
                    }
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source, options: TestOptions.UnsafeReleaseDll);
        comp.VerifyDiagnostics(
            // (8,21): error CS4004: Cannot await in an unsafe context
            //             var v = await? t;
            Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await? t").WithLocation(8, 21));
    }

    [Fact]
    public void ContextError_OperandDoesNotSatisfyAwaitablePattern()
    {
        // `?` is fine on a reference type operand, but the operand must satisfy the
        // awaitable pattern on its underlying type. A random class with no GetAwaiter
        // produces the standard missing-member diagnostic (CS1061).
        var source = """
            using System.Threading.Tasks;
            public class NotAwaitable { }
            public class C
            {
                public async Task M(NotAwaitable t)
                {
                    var v = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,17): error CS1061: 'NotAwaitable' does not contain a definition for 'GetAwaiter'...
            //         var v = await? t;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "await? t").WithArguments("NotAwaitable", "GetAwaiter").WithLocation(7, 17));
    }

    #endregion

    #region Interaction

    [Fact]
    public void Interaction_ExtensionGetAwaiterOnUnderlyingType()
    {
        // GetAwaiter is an extension method on MyStruct. The operand is MyStruct? (Nullable<MyStruct>).
        // The awaitable pattern resolves against MyStruct (the underlying type U) and picks up the extension.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            public struct MyStruct { }

            public struct MyAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public int GetResult() => 42;
                public void OnCompleted(Action continuation) { }
            }

            public static class Ext
            {
                public static MyAwaiter GetAwaiter(this MyStruct s) => default;
            }

            public class C
            {
                public async Task M(MyStruct? t)
                {
                    var v = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(comp, "v"));

        // Confirm the GetAwaiter binding resolved to the extension method.
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var awaitExpr = tree.GetRoot().DescendantNodes().OfType<AwaitExpressionSyntax>().Single();
        var info = model.GetAwaitExpressionInfo(awaitExpr);
        Assert.Equal("GetAwaiter", info.GetAwaiterMethod!.Name);
        Assert.True(info.GetAwaiterMethod.IsExtensionMethod);
    }

    [Fact]
    public void Interaction_ConfigureAwaitOnBareTask_IsError()
    {
        // `task.ConfigureAwait(false)` returns a non-nullable struct (ConfiguredTaskAwaitable),
        // which fails the operand-nullability rule. The user's intended spelling is
        // `await? task?.ConfigureAwait(false)` (covered below).
        var source = InAsyncMethod("await? t.ConfigureAwait(false);", "Task t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,14): error CS9388: 'await?' cannot be applied to an operand of non-nullable value type 'ConfiguredTaskAwaitable'.
            //         await? t.ConfigureAwait(false);
            Diagnostic(ErrorCode.ERR_AwaitConditionalNonNullableValueType, "?").WithArguments("System.Runtime.CompilerServices.ConfiguredTaskAwaitable").WithLocation(7, 14));
    }

    [Fact]
    public void Interaction_ConfigureAwaitOnNullableTaskViaNullConditional_IsOK()
    {
        // `task?.ConfigureAwait(false)` returns `Nullable<ConfiguredTaskAwaitable>`, which
        // passes the operand-nullability rule and binds correctly.
        var source = InAsyncMethod("await? t?.ConfigureAwait(false);", "Task t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    #endregion

    #region Conversions applied to the await? result

    [Fact]
    public void Conversion_TaskOfInt_ToObject_Succeeds()
    {
        // `await? Task<int>` has static type `int?`. Assignment to `object` (non-nullable)
        // is an implicit reference conversion via boxing — it succeeds at the conversion
        // level but triggers the standard NRT "converting possibly null to non-nullable"
        // warning because the short-circuit can produce null. Assigning to `object?` avoids
        // the warning (covered in the next test).
        var source = InAsyncMethod("object o = await? t;", "Task<int> t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,20): warning CS8600: Converting null literal or possible null value to non-nullable type.
            //         object o = await? t;
            Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "await? t").WithLocation(7, 20));
    }

    [Fact]
    public void Conversion_TaskOfInt_ToNullableObject_Succeeds()
    {
        // Same as above but the target is `object?`, matching the null-conditional semantics.
        var source = InAsyncMethod("object? o = await? t;", "Task<int> t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Conversion_TaskOfInt_ToNullableLong_Succeeds()
    {
        // `int?` has an implicit conversion to `long?` (lifted numeric widening). The
        // conversion takes place on the overall `await?` result type.
        var source = InAsyncMethod("long? x = await? t;", "Task<int> t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Conversion_TaskOfInt_ToLong_FailsWithoutNullableTarget()
    {
        // The result is `int?`; there is no implicit conversion from `int?` to plain `long`.
        // This is the same error you'd get on any `long x = someNullableInt;` expression.
        var source = InAsyncMethod("long x = await? t;", "Task<int> t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,18): error CS0266: Cannot implicitly convert type 'int?' to 'long'. An explicit conversion exists (are you missing a cast?)
            //         long x = await? t;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "await? t").WithArguments("int?", "long").WithLocation(7, 18),
            // (7,18): warning CS8629: Nullable value type may be null.
            //         long x = await? t;
            Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "await? t").WithLocation(7, 18));
    }

    [Fact]
    public void Conversion_TaskOfString_ToObject_Succeeds()
    {
        // The result static type is `string` (reference-type R stays unchanged). Conversion
        // to `object` is an implicit reference conversion. The NRT short-circuit annotation
        // triggers the standard "converting possibly null to non-nullable" warning; assigning
        // to `object?` avoids it.
        var source = InAsyncMethod("object o = await? t;", "Task<string> t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,20): warning CS8600: Converting null literal or possible null value to non-nullable type.
            //         object o = await? t;
            Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "await? t").WithLocation(7, 20));
    }

    [Fact]
    public void Conversion_TaskOfString_ToNullableObject_Succeeds()
    {
        var source = InAsyncMethod("object? o = await? t;", "Task<string> t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    #endregion

    #region Lambdas and expression-bodied members

    [Fact]
    public void InAsyncLambda_ExplicitReturnType()
    {
        // Async lambda with explicit return type `Task<int?>`. The lambda body's `await?`
        // result (int?) matches the declared element return type.
        var source = """
            using System;
            using System.Threading.Tasks;
            public class C
            {
                public void M(Task<int> t)
                {
                    Func<Task<int?>> f = async Task<int?> () => await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void InAsyncLambda_InferredReturnType()
    {
        // No explicit return type on the lambda — the compiler infers the element return type
        // from the body. `await? Task<int>` returns `int?`, so the lambda should be inferable
        // as `Func<Task<int?>>`.
        var source = """
            using System;
            using System.Threading.Tasks;
            public class C
            {
                public void M(Task<int> t)
                {
                    Func<Task<int?>> f = async () => await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void InExpressionBodiedAsyncMethod()
    {
        // Expression-bodied async method returning `Task<int?>`. The expression body is the
        // method's return value, so the `await?` result is used (classified as int?).
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task<int?> M(Task<int> t) => await? t;
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void InExpressionBodiedAsyncLocalFunction()
    {
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public void M(Task<int> t)
                {
                    async Task<int?> Local() => await? t;
                    _ = Local();
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void InTernaryBranch()
    {
        // Both ternary branches use `await?`; the branches' types unify to `int?`.
        var source = InAsyncMethod("var v = flag ? await? a : await? b;", "bool flag, Task<int> a, Task<int> b");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(comp, "v"));
    }

    [Fact]
    public void Nested_AwaitQuestion_AwaitQuestion()
    {
        // Two stacked `await?` applications. Inner awaits Task<Task<int>> producing Task<int>
        // (reference type, short-circuitable to null per the inner `?`); outer then does its
        // own short-circuit on that possibly-null Task<int> and lifts the final int → int?.
        // No warning fires on either `await?` because each one accepts a null receiver.
        var source = InAsyncMethod("var v = await? await? outer;", "Task<Task<int>> outer");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(comp, "v"));
    }

    [Fact]
    public void NullableAnalysis_OperandIsStillAnalyzed()
    {
        // The `await?` null-receiver suppression must not leak outward and silence nullable
        // analysis INSIDE the operand expression. A `null` literal passed to a non-nullable
        // reference-type parameter of the operand must still produce the usual warning.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public Task<int> Something(string s) => Task.FromResult(s.Length);
                public async Task M()
                {
                    var v = await? Something(null);
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,34): warning CS8625: Cannot convert null literal to non-nullable reference type.
            //         var v = await? Something(null);
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(7, 34));
        Assert.Equal("int?", TypeOfLocalSymbol(comp, "v"));
    }

    #endregion

    #region Type-parameter constraint variants

    [Fact]
    public void Operand_TypeParameter_NotNullConstraint_RefTypeInstance()
    {
        // `where T : notnull` does NOT imply `T` is a reference type — it could be a struct.
        // Per the operand-nullability rule, only a *known* non-nullable value type is
        // rejected with CS9388; a type parameter that isn't known to be a value type passes,
        // and the missing-GetAwaiter diagnostic is what surfaces.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M<T>(T t) where T : notnull
                {
                    await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (6,9): error CS1061: 'T' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
            //         await? t;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "await? t").WithArguments("T", "GetAwaiter").WithLocation(6, 9));
    }

    [Fact]
    public void Operand_TypeParameter_InterfaceOnlyConstraint()
    {
        // Table A row: type parameter constrained by an interface only (no `struct`). Treated
        // as a reference-type-compatible operand — not rejected by the operand-nullability
        // rule. An interface-only T on its own still lacks a GetAwaiter, so the standard
        // missing-member diagnostic is what surfaces.
        var source = """
            using System.Threading.Tasks;
            public interface IMarker { }
            public class C
            {
                public async Task M<T>(T t) where T : IMarker
                {
                    await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,9): error CS1061: 'T' does not contain a definition for 'GetAwaiter'...
            //         await? t;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "await? t").WithArguments("T", "GetAwaiter").WithLocation(7, 9));
    }

    [Fact]
    public void ResultType_TypeParameter_NotNullConstraint_StillCannotBeMadeNullable()
    {
        // Result R = T with `T : notnull`. `notnull` doesn't prove T is a reference type or a
        // non-nullable value type (it could be either). Same classification bucket as an
        // unconstrained T — CS8978 fires in value position.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M<T>(Task<T> t) where T : notnull
                {
                    var v = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (6,22): error CS8978: 'T' cannot be made nullable.
            //         var v = await? t;
            Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, "?").WithArguments("T").WithLocation(6, 22));
    }

    #endregion

    #region Other operand / result shapes

    [Fact]
    public void Operand_TaskOfDynamic_ResultIsDynamic()
    {
        // `R = dynamic` via a generic Task. The result-type rule leaves dynamic unchanged; the
        // short-circuit surfaces as `dynamic?` on the inferred local in NRT mode (same pattern
        // as Operand_Dynamic, but R comes from the generic arg not from a dynamic operand).
        var source = InAsyncMethod("var v = await? t;", "Task<dynamic> t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
        Assert.Equal("dynamic?", TypeOfLocalSymbol(comp, "v"));
    }

    [Fact]
    public void ExtensionGetAwaiter_OnReferenceTypeUnderlying()
    {
        // GetAwaiter is an extension on a reference-type awaitable wrapper. Nothing about the
        // operand is Nullable<V>, so no underlying-type stripping happens — this confirms the
        // awaitable pattern resolves via extensions on the plain reference operand.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            public class MyTask { }

            public struct MyAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public int GetResult() => 0;
                public void OnCompleted(Action continuation) { }
            }

            public static class Ext
            {
                public static MyAwaiter GetAwaiter(this MyTask t) => default;
            }

            public class C
            {
                public async Task M(MyTask t)
                {
                    var v = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(comp, "v"));
    }

    #endregion

    #region Additional await-context restrictions

    [Fact]
    public void ContextError_InCatchFilter()
    {
        // `await` inside a catch filter (exception-filter expression) is always illegal,
        // regardless of async mode. `await?` inherits the same rule.
        var source = """
            using System;
            using System.Threading.Tasks;
            public class C
            {
                public async Task M(Task<bool> t)
                {
                    try { }
                    catch (Exception e) when (await? t) { }
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (8,26): warning CS0168: The variable 'e' is declared but never used
            Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(8, 26),
            // (8,35): error CS7094: Cannot await in the filter expression of a catch clause
            //         catch (Exception e) when (await? t) { }
            Diagnostic(ErrorCode.ERR_BadAwaitInCatchFilter, "await? t").WithLocation(8, 35));
    }

    #endregion

    #region Semantic model API for ordinary (non-extension) GetAwaiter

    [Fact]
    public void GetAwaitExpressionInfo_OrdinaryInstance_TaskOfInt()
    {
        // For a plain Task<int> operand, GetAwaitExpressionInfo should surface the instance
        // Task<int>.GetAwaiter method (not an extension).
        var source = InAsyncMethod("var v = await? t;", "Task<int> t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var awaitExpr = tree.GetRoot().DescendantNodes().OfType<AwaitExpressionSyntax>().Single();
        var info = model.GetAwaitExpressionInfo(awaitExpr);
        Assert.Equal("GetAwaiter", info.GetAwaiterMethod!.Name);
        Assert.False(info.GetAwaiterMethod.IsExtensionMethod);
        Assert.Equal("System.Threading.Tasks.Task<int>", info.GetAwaiterMethod.ContainingType.ToDisplayString());
    }

    [Fact]
    public void GetAwaitExpressionInfo_Dynamic_IsDynamic()
    {
        // A dynamic operand goes through the dynamic await path — GetAwaiterMethod is not
        // resolved to a concrete symbol and IsDynamic is true.
        var source = InAsyncMethod("var v = await? t;", "dynamic t");
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var awaitExpr = tree.GetRoot().DescendantNodes().OfType<AwaitExpressionSyntax>().Single();
        var info = model.GetAwaitExpressionInfo(awaitExpr);
        Assert.True(info.IsDynamic);
    }

    #endregion

    #region Pointer and function-pointer result types

    // Pointer (and function-pointer) result types are left unchanged by the result-type
    // rule — they already represent null as the zero value, so no `Nullable<int*>` wrapping
    // happens. These tests validate the bind and also pin the interaction with the normal
    // "await cannot appear in unsafe context" restriction.

    private const string PointerAwaitableSource = """
        using System;
        using System.Runtime.CompilerServices;
        using System.Threading.Tasks;

        public struct PointerAwaiter : INotifyCompletion
        {
            public bool IsCompleted => true;
            public unsafe int* GetResult() => null;
            public void OnCompleted(Action continuation) { }
        }

        public class PointerAwaitable
        {
            public PointerAwaiter GetAwaiter() => default;
        }
        """;

    private const string FunctionPointerAwaitableSource = """
        using System;
        using System.Runtime.CompilerServices;
        using System.Threading.Tasks;

        public struct FPtrAwaiter : INotifyCompletion
        {
            public bool IsCompleted => true;
            public unsafe delegate*<int> GetResult() => default;
            public void OnCompleted(Action continuation) { }
        }

        public class FPtrAwaitable
        {
            public FPtrAwaiter GetAwaiter() => default;
        }
        """;

    [Fact]
    public void ResultType_Pointer_InUnsafeAsyncMethod_AwaitStillForbidden()
    {
        // Combining `await` (of any form) with a pointer-returning awaitable runs headlong
        // into ERR_AwaitInUnsafeContext because the whole await has to happen somewhere
        // `GetResult()` can be legally invoked. The `?` doesn't rescue it.
        var source = PointerAwaitableSource + """

            public class C
            {
                public async Task M(PointerAwaitable t)
                {
                    unsafe
                    {
                        var v = await? t;
                    }
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source, options: TestOptions.UnsafeReleaseDll);
        comp.VerifyDiagnostics(
            // (22,21): error CS4004: Cannot await in an unsafe context
            //             var v = await? t;
            Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await? t").WithLocation(22, 21));
        // Despite the error the binder still assigns a type to `v`: pointers are left
        // unchanged (no Nullable<int*> wrapping).
        Assert.Equal("int*", TypeOfLocalSymbol(comp, "v"));
    }

    [Fact]
    public void ResultType_Pointer_InSafeAsyncMethod_PointerUsageIsUnsafe()
    {
        // Under preview unsafe rules, naming `int*` as a result type no longer requires an
        // unsafe context; the await on a pointer-returning awaitable binds cleanly.
        var source = PointerAwaitableSource + """

            public class C
            {
                public async Task M(PointerAwaitable t)
                {
                    var v = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source, options: TestOptions.UnsafeReleaseDll);
        comp.VerifyDiagnostics();
        Assert.Equal("int*", TypeOfLocalSymbol(comp, "v"));
    }

    [Fact]
    public void ResultType_FunctionPointer_InUnsafeAsyncMethod_AwaitStillForbidden()
    {
        var source = FunctionPointerAwaitableSource + """

            public class C
            {
                public async Task M(FPtrAwaitable t)
                {
                    unsafe
                    {
                        var v = await? t;
                    }
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source, options: TestOptions.UnsafeReleaseDll);
        comp.VerifyDiagnostics(
            // (22,21): error CS4004: Cannot await in an unsafe context
            //             var v = await? t;
            Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await? t").WithLocation(22, 21));
        Assert.Equal("delegate*<int>", TypeOfLocalSymbol(comp, "v"));
    }

    [Fact]
    public void ResultType_FunctionPointer_InSafeAsyncMethod_PointerUsageIsUnsafe()
    {
        // Under preview unsafe rules, naming a function-pointer type as a result no longer
        // requires an unsafe context; the await binds cleanly.
        var source = FunctionPointerAwaitableSource + """

            public class C
            {
                public async Task M(FPtrAwaitable t)
                {
                    var v = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source, options: TestOptions.UnsafeReleaseDll);
        comp.VerifyDiagnostics();
        Assert.Equal("delegate*<int>", TypeOfLocalSymbol(comp, "v"));
    }

    [Fact]
    public void ResultType_RefStructR_CannotBeMadeNullable()
    {
        // Ref structs (e.g. `Span<int>`) cannot be stored as a `Nullable<>` generic
        // argument. Per the spec, `await?` of a ref-struct R behaves like the unconstrained-T
        // case: CS8978 in value position (pinned here), degrades to void in statement
        // position (pinned in the next test). Note the CS8978 anchors on `?`, which means
        // it fires ahead of the usual "ref struct can't cross an await boundary" error.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            public struct SpanAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public Span<int> GetResult() => default;
                public void OnCompleted(Action continuation) { }
            }

            public class SpanAwaitable
            {
                public SpanAwaiter GetAwaiter() => default;
            }

            public class C
            {
                public async Task M(SpanAwaitable t)
                {
                    var v = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (21,22): error CS8978: 'Span<int>' cannot be made nullable.
            //         var v = await? t;
            Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, "?").WithArguments("System.Span<int>").WithLocation(21, 22));
    }

    [Fact]
    public void ResultType_RefStructR_StatementPosition_DegradesToVoid()
    {
        // Parallel to ResultType_UnconstrainedTypeParameterResult_StatementPosition_OK:
        // in statement position the unused result degrades to void instead of reporting
        // CS8978.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            public struct SpanAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public Span<int> GetResult() => default;
                public void OnCompleted(Action continuation) { }
            }

            public class SpanAwaitable
            {
                public SpanAwaiter GetAwaiter() => default;
            }

            public class C
            {
                public async Task M(SpanAwaitable t)
                {
                    await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    #endregion

    #region Type inference and overload resolution

    [Fact]
    public void TypeInference_GenericMethodArg_InfersLiftedResult()
    {
        // `M<T>(T x)` called with `await? taskOfInt` — T should be inferred from the
        // `await?` result type (int?), not the awaiter's R (int).
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public static T Identity<T>(T x) => x;
                public async Task M(Task<int> t)
                {
                    var v = Identity(await? t);
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
        Assert.Equal("int?", TypeOfLocalSymbol(comp, "v"));
    }

    [Fact]
    public void TypeInference_GenericMethodArg_ReferenceResult_InfersString()
    {
        // Reference-type result: T should be inferred as string (with NRT annotation).
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public static T Identity<T>(T x) => x;
                public async Task M(Task<string> t)
                {
                    var v = Identity(await? t);
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
        Assert.Equal("string?", TypeOfLocalSymbol(comp, "v"));
    }

    [Fact]
    public void OverloadResolution_IntVsNullableInt_PrefersNullableInt()
    {
        // `F(int) / F(int?)` called with `await? taskOfInt` (result type int?).
        // Exact-match overload is F(int?).
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public static string F(int x) => "int";
                public static string F(int? x) => "int?";
                public async Task M(Task<int> t)
                {
                    var v = F(await? t);
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
        // `var` inference under NRT annotates the local as `string?` because the result
        // of F flows from a MaybeDefault argument.
        Assert.Equal("string?", TypeOfLocalSymbol(comp, "v"));

        // Pin which overload was picked via the semantic model.
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(n => n.Expression is IdentifierNameSyntax { Identifier.Text: "F" });
        var resolved = model.GetSymbolInfo(call).Symbol;
        Assert.Equal("System.String C.F(System.Int32? x)", resolved!.ToTestDisplayString());
    }

    [Fact]
    public void OverloadResolution_ObjectVsNullableInt_PrefersNullableInt()
    {
        // `F(object) / F(int?)` called with `await? taskOfInt` (result type int?).
        // Better overload is F(int?) — exact match.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public static string F(object x) => "object";
                public static string F(int? x) => "int?";
                public async Task M(Task<int> t)
                {
                    _ = F(await? t);
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(n => n.Expression is IdentifierNameSyntax { Identifier.Text: "F" });
        Assert.Equal("System.String C.F(System.Int32? x)", model.GetSymbolInfo(call).Symbol!.ToTestDisplayString());
    }

    [Fact]
    public void OverloadResolution_OnlyIntAvailable_LiftedResultIsCompileTimeError()
    {
        // If `F(int)` is the ONLY available overload, `F(await? taskOfInt)` is an error
        // because int? → int is not an implicit conversion. This pins that the caller
        // actually sees the lifted type at overload resolution time, not the awaiter's R.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public static string F(int x) => "int";
                public async Task M(Task<int> t)
                {
                    _ = F(await? t);
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,15): error CS1503: Argument 1: cannot convert from 'int?' to 'int'
            //         _ = F(await? t);
            Diagnostic(ErrorCode.ERR_BadArgType, "await? t").WithArguments("1", "int?", "int").WithLocation(7, 15));
    }

    [Fact]
    public void OverloadResolution_StringVsNullableString_ResolvesOnNRTAnnotation()
    {
        // Reference-type result: plain `F(string)` wins (result type is still `string` at
        // the symbol level; NRT is an annotation, not a different symbol). Pin the symbol
        // picked so future changes surface here.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public static string F(string x) => "string";
                public async Task M(Task<string> t)
                {
                    _ = F(await? t);
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,15): warning CS8604: Possible null reference argument for parameter 'x' of 'string C.F(string x)'.
            //         _ = F(await? t);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "await? t").WithArguments("x", "string C.F(string x)").WithLocation(7, 15));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(n => n.Expression is IdentifierNameSyntax { Identifier.Text: "F" });
        Assert.Equal("System.String C.F(System.String x)", model.GetSymbolInfo(call).Symbol!.ToTestDisplayString());
    }

    #endregion

    #region Pattern matching and deconstruction on the await? result

    [Fact]
    public void PatternMatching_IsConstantPattern_OnLiftedResult()
    {
        // `(await? t) is 42` on a Task<int> operand — the pattern is matched against int?.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task<bool> M(Task<int> t) => (await? t) is 42;
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void PatternMatching_IsTypePattern_OnLiftedNullableResult()
    {
        // `(await? t) is int value` — declaration pattern narrows int? to int.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task<int> M(Task<int> t)
                {
                    return (await? t) is int value ? value : -1;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void PatternMatching_IsNullPattern_OnReferenceResult()
    {
        // `(await? t) is null` on a Task<string> operand — pattern matches when the
        // short-circuit produced null or when GetResult returned null.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task<bool> M(Task<string> t) => (await? t) is null;
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void PatternMatching_IsNotNullPattern_PromotesToNonNullable()
    {
        // After `is not null`, flow analysis should narrow string? → string in the true
        // branch. Passing the narrowed value into a non-nullable parameter must not warn.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public static void F(string s) { }
                public async Task M(Task<string> t)
                {
                    var v = await? t;
                    if (v is not null)
                        F(v);
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void PatternMatching_PropertyPattern_OnLiftedResult()
    {
        // Property pattern `{ Length: > 0 }` on a Task<string> operand — pattern walks
        // through the possibly-null wrapper.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task<bool> M(Task<string> t) => (await? t) is { Length: > 0 };
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void PatternMatching_SwitchExpression_OnLiftedResult()
    {
        // Switch on `await? t` with explicit `null` arm and a declaration arm.
        // Already covered for Spill in emit tests; this pins the binding-level typing.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task<string> M(Task<int> t)
                {
                    return (await? t) switch
                    {
                        null => "null",
                        0 => "zero",
                        var x => x.ToString()!,
                    };
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Deconstruction_TupleResult_OfNullableTuple()
    {
        // `Task<(int, int)>` — result type of `await?` is `(int, int)?`.
        // Deconstructing a Nullable<ValueTuple> directly is not supported by the language:
        // there's no implicit Deconstruct on Nullable<T>. Pin the expected error so we
        // notice if the rules ever change.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M(Task<(int, int)> t)
                {
                    var (a, b) = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (6,14): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'a'.
            Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "a").WithArguments("a").WithLocation(6, 14),
            // (6,17): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'b'.
            Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "b").WithArguments("b").WithLocation(6, 17),
            // (6,22): error CS1061: '(int, int)?' does not contain a definition for 'Deconstruct' ...
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "await? t").WithArguments("(int, int)?", "Deconstruct").WithLocation(6, 22),
            // (6,22): error CS8129: No suitable 'Deconstruct' instance or extension method was found ...
            Diagnostic(ErrorCode.ERR_MissingDeconstruct, "await? t").WithArguments("(int, int)?", "2").WithLocation(6, 22));
    }

    [Fact]
    public void NullForgiving_OnReferenceResult_AllowsMemberAccess()
    {
        // `(await? t)!.Length` on Task<string>. The null-forgiving `!` narrows the result
        // from `string?` to `string` for the member access, so no CS8602 fires.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task<int> M(Task<string> t) => (await? t)!.Length;
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void NullForgiving_OnLiftedValueResult_AllowsValueAccess()
    {
        // `(await? t)!.Value` on Task<int>. The result is int?; `!` narrows the flow state
        // but the static type stays int? (there is no NRT equivalent for value types), so
        // `.Value` is the correct member.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task<int> M(Task<int> t) => (await? t)!.Value;
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void NullForgiving_OnReferenceResult_NoWarningOnDereference()
    {
        // Without `!`, dereferencing `(await? t).Length` warns about possibly null.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task<int> M(Task<string> t) => (await? t).Length;
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (4,50): warning CS8602: Dereference of a possibly null reference.
            //     public async Task<int> M(Task<string> t) => (await? t).Length;
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "await? t").WithLocation(4, 50));
    }

    [Fact]
    public void OutVar_InsideAwaitQuestionOperand_DefiniteAssignment()
    {
        // `await? GetTask(out var x)` — `out var x` introduces `x` as definitely assigned
        // on the non-null branch. On the null branch GetTask isn't called, so `x` is NOT
        // definitely assigned after the statement. Using `x` unconditionally below should
        // error.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public static Task<int> GetTask(out int x) { x = 1; return Task.FromResult(0); }
                public async Task M()
                {
                    var v = await? GetTask(out var x);
                    System.Console.WriteLine(x);
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        // Method reference is never null, so GetTask always runs and `x` is definitely
        // assigned; this should compile without errors.
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void OutVar_InsideAwaitQuestionOperand_DefiniteAssignment_ViaNullableReceiver()
    {
        // Now the operand of `await?` can actually be null: `t?.Method(out var x)`.
        // When `t` is null the whole operand short-circuits to null, `Method` is not
        // called, and `x` is NOT definitely assigned. Using `x` after the statement
        // should produce CS0165 (use of unassigned local variable).
        var source = """
            using System.Threading.Tasks;
            public class Holder
            {
                public Task<int> M(out int x) { x = 1; return Task.FromResult(0); }
            }
            public class C
            {
                public async Task M(Holder h)
                {
                    var v = await? h?.M(out var x);
                    System.Console.WriteLine(x);
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (10,34): error CS0165: Use of unassigned local variable 'x'
            //         System.Console.WriteLine(x);
            Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(11, 34));
    }

    [Fact]
    public void ForbiddenContext_DefaultParameterValue()
    {
        // `await` is forbidden as a default parameter value. `await?` inherits. The
        // ternary-vs-await-expression disambiguation at parse time yields a ternary here
        // (non-async context); the binder then reports the usual identifier-lookup cascade.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public void M(int x = await? Task.FromResult(1)) { }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (4,27): error CS0103: The name 'await' does not exist in the current context
            //     public void M(int x = await? Task.FromResult(1)) { }
            Diagnostic(ErrorCode.ERR_NameNotInContext, "await").WithArguments("await").WithLocation(4, 27),
            // (4,52): error CS1003: Syntax error, ':' expected
            //     public void M(int x = await? Task.FromResult(1)) { }
            Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":").WithLocation(4, 52),
            // (4,52): error CS1525: Invalid expression term ')'
            //     public void M(int x = await? Task.FromResult(1)) { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(4, 52));
    }

    [Fact]
    public void ForbiddenContext_FieldInitializer()
    {
        // Field initializers don't run in an async function. `await` is illegal; `await?`
        // inherits. Ternary-disambiguation in this non-async context makes the parser see
        // a ternary; the binder then reports a cascade from the unresolved `await` identifier.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public int F = await? Task.FromResult(1);
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (4,20): error CS0103: The name 'await' does not exist in the current context
            //     public int F = await? Task.FromResult(1);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "await").WithArguments("await").WithLocation(4, 20),
            // (4,45): error CS1003: Syntax error, ':' expected
            //     public int F = await? Task.FromResult(1);
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(4, 45),
            // (4,45): error CS1525: Invalid expression term ';'
            //     public int F = await? Task.FromResult(1);
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(4, 45));
    }

    [Fact]
    public void ForbiddenContext_NonAsyncConstructor()
    {
        // Constructors cannot be `async`; `await?` in a ctor body is rejected the same
        // way as `await` — same ternary-in-non-async-context cascade as the other
        // non-async forbidden contexts.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public C(Task<int> t)
                {
                    var v = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (6,17): error CS0103: The name 'await' does not exist in the current context
            //         var v = await? t;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "await").WithArguments("await").WithLocation(6, 17),
            // (6,25): error CS1003: Syntax error, ':' expected
            //         var v = await? t;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(6, 25),
            // (6,25): error CS1525: Invalid expression term ';'
            //         var v = await? t;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 25));
    }

    [Fact]
    public void ForbiddenContext_Finalizer()
    {
        // Finalizers cannot be async; `await?` is rejected.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                ~C()
                {
                    Task<int> t = Task.FromResult(0);
                    _ = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (7,13): error CS0103: The name 'await' does not exist in the current context
            //         _ = await? t;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "await").WithArguments("await").WithLocation(7, 13),
            // (7,21): error CS1003: Syntax error, ':' expected
            //         _ = await? t;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(7, 21),
            // (7,21): error CS1525: Invalid expression term ';'
            //         _ = await? t;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(7, 21));
    }

    [Fact]
    public void ExpressionTree_AsyncLambda_WithAwaitQuestion_IsRejected()
    {
        // `async`-lambda-to-expression-tree conversion is already illegal; `await?` in the
        // body doesn't change the outcome but is worth pinning so a parser/binder
        // regression that accidentally let it through would be caught here.
        var source = """
            using System;
            using System.Linq.Expressions;
            using System.Threading.Tasks;
            public class C
            {
                public void M(Task<int> t)
                {
                    Expression<Func<Task<int?>>> e = async () => await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (8,42): error CS1989: Async lambda expressions cannot be converted to expression trees
            //         Expression<Func<Task<int?>>> e = async () => await? t;
            Diagnostic(ErrorCode.ERR_BadAsyncExpressionTree, "async () => await? t").WithLocation(8, 42));
    }

    [Fact]
    public void AwaitablePattern_MissingGetResult_SameErrorAsAwait()
    {
        // If the awaiter type lacks a `GetResult` method, plain `await` reports
        // ERR_BadAwaiterPattern / ERR_BadAwaitArg; `await?` must surface the same error.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            public struct MyAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public void OnCompleted(Action continuation) { }
                // GetResult is intentionally missing.
            }
            public class MyTask { public MyAwaiter GetAwaiter() => default; }
            public class C
            {
                public async Task M(MyTask t)
                {
                    await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (15,9): error CS0117: 'MyAwaiter' does not contain a definition for 'GetResult'
            //         await? t;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "await? t").WithArguments("MyAwaiter", "GetResult").WithLocation(15, 9));
    }

    [Fact]
    public void AwaitablePattern_NotINotifyCompletion_SameErrorAsAwait()
    {
        // Awaiter lacks INotifyCompletion / ICriticalNotifyCompletion.
        var source = """
            using System.Threading.Tasks;
            public struct MyAwaiter
            {
                public bool IsCompleted => true;
                public int GetResult() => 0;
            }
            public class MyTask { public MyAwaiter GetAwaiter() => default; }
            public class C
            {
                public async Task M(MyTask t)
                {
                    _ = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (12,13): error CS4027: 'MyAwaiter' does not implement 'INotifyCompletion'
            //         _ = await? t;
            Diagnostic(ErrorCode.ERR_DoesntImplementAwaitInterface, "await? t").WithArguments("MyAwaiter", "System.Runtime.CompilerServices.INotifyCompletion").WithLocation(12, 13));
    }

    [Fact]
    public void AwaitablePattern_GetResultTakesArguments_SameErrorAsAwait()
    {
        // GetResult with parameters — awaitable pattern requires parameterless GetResult.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            public struct MyAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public int GetResult(int badParam) => 0;
                public void OnCompleted(Action continuation) { }
            }
            public class MyTask { public MyAwaiter GetAwaiter() => default; }
            public class C
            {
                public async Task M(MyTask t)
                {
                    _ = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (15,13): error CS7036: There is no argument given that corresponds to the required parameter 'badParam' of 'MyAwaiter.GetResult(int)'
            //         _ = await? t;
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "await? t").WithArguments("badParam", "MyAwaiter.GetResult(int)").WithLocation(15, 13));
    }

    [Fact]
    public void AwaitablePattern_StaticGetAwaiter_SameErrorAsAwait()
    {
        // Static GetAwaiter on the awaitable class — not part of the pattern.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            public struct MyAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public int GetResult() => 0;
                public void OnCompleted(Action continuation) { }
            }
            public class MyTask
            {
                public static MyAwaiter GetAwaiter() => default;
            }
            public class C
            {
                public async Task M(MyTask t)
                {
                    _ = await? t;
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics(
            // (18,13): error CS1986: 'await' requires that the type MyTask have a suitable 'GetAwaiter' method
            //         _ = await? t;
            Diagnostic(ErrorCode.ERR_BadAwaitArg, "await? t").WithArguments("MyTask").WithLocation(18, 13));
    }

    [Fact]
    public void Deconstruction_TupleResult_ViaGetValueOrDefault_Works()
    {
        // Same operand as above, but use `.GetValueOrDefault()` to get the tuple value.
        // This is the workaround and should compile cleanly.
        var source = """
            using System.Threading.Tasks;
            public class C
            {
                public async Task M(Task<(int, int)> t)
                {
                    var (a, b) = (await? t).GetValueOrDefault();
                }
            }
            """;
        var comp = CreateWithNullableReferenceTypesEnabled(source);
        comp.VerifyDiagnostics();
    }

    #endregion
}
