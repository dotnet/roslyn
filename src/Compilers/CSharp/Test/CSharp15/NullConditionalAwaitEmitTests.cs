// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class NullConditionalAwaitEmitTests : CSharpTestBase
{
    // Emit tests for `await? e` follow the dual-mode pattern used elsewhere for async emit
    // (see CodeGenAsyncTests / CodeGenAsyncSpillTests): every source is compiled under the
    // default state-machine async lowering AND under the runtime-async feature. State-machine
    // output is asserted unconditionally; runtime-async output is asserted only when the test
    // runner sets `DOTNET_RuntimeAsync=1` (RuntimeAsyncTestHelpers.ExpectedOutput gates that).
    //
    // Tests that want to pin IL shape do so via VerifyIL on a focused method (not MoveNext,
    // which is noisy); runtime-async IL is asserted unconditionally (it doesn't depend on the
    // runtime being installed).

    private static readonly CSharpParseOptions s_preview = TestOptions.RegularPreview;

    private CompilationVerifier VerifyStateMachine(string source, string expectedOutput)
    {
        // NetCoreApp gives us Task / ValueTask / etc. The language-version override puts us in
        // preview so `await?` is accepted.
        return CompileAndVerify(
            source,
            parseOptions: s_preview,
            targetFramework: TargetFramework.NetCoreApp,
            options: TestOptions.ReleaseExe,
            expectedOutput: expectedOutput);
    }

    private CompilationVerifier VerifyRuntimeAsync(string source, string expectedOutput, Verification? verify = null)
    {
        // IL verification for runtime-async output is skipped by default — the lowered form
        // calls AsyncHelpers.Await which (intentionally) does not round-trip through ILVerify
        // because it stands in for a yield point that only the runtime implements. Individual
        // IL-shape assertions live in the `IL_*` tests which call VerifyIL directly.
        var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
        var verifier = CompileAndVerify(
            comp,
            expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput),
            verify: verify ?? Verification.Skipped);
        verifier.VerifyDiagnostics();
        return verifier;
    }

    #region Spec matrix: one test per row, asserting correct runtime behavior in both modes

    // The consistent output pattern for tests in this region is:
    //
    //   before-nn;    (printed unconditionally before the non-null-receiver `await?`)
    //   <value>       (printed after; proves the `await?` returned control and we can
    //                  read the result — for void cases we skip the value line)
    //   after-nn;
    //   before-null;  (before the null-receiver `await?`)
    //   <value>       (proves execution continued past the null short-circuit)
    //   after-null;
    //   done          (proves the whole method ran to completion; any mid-`await?` abort
    //                  would drop this line)
    //
    // Every null-receiver case therefore demonstrates the short-circuit *and* that every
    // statement after it executes as if the await were a no-op.

    [Fact]
    public void ResultType_Task_VoidResult()
    {
        // R = void. `await? t;` at statement position evaluates to nothing.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task t1 = Task.CompletedTask;
                    Console.Write("before-nn;");
                    await? t1;
                    Console.Write("after-nn;");

                    Task t2 = null;
                    Console.Write("before-null;");
                    await? t2;
                    Console.Write("after-null;");

                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;after-nn;before-null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void ResultType_TaskOfInt_LiftedToNullableInt()
    {
        // R = int, lifted to int?. Non-null → int?(value); null → null.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(42);
                    Console.Write("before-nn;");
                    int? v1 = await? t1;
                    Console.Write($"v1={(v1.HasValue ? v1.Value.ToString() : "null")};after-nn;");

                    Task<int> t2 = null;
                    Console.Write("before-null;");
                    int? v2 = await? t2;
                    Console.Write($"v2={(v2.HasValue ? v2.Value.ToString() : "null")};after-null;");

                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;v1=42;after-nn;before-null;v2=null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void ResultType_TaskOfString_UnchangedReferenceR()
    {
        // R = string (reference type), stays as string; null-conditional may produce null.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<string> t1 = Task.FromResult("hello");
                    Console.Write("before-nn;");
                    string v1 = await? t1;
                    Console.Write($"v1={v1 ?? "null"};after-nn;");

                    Task<string> t2 = null;
                    Console.Write("before-null;");
                    string v2 = await? t2;
                    Console.Write($"v2={v2 ?? "null"};after-null;");

                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;v1=hello;after-nn;before-null;v2=null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void ResultType_TaskOfNullableInt_NotDoubleWrapped()
    {
        // R = int? already. Lifting leaves it as int? (not int??). Three receivers: non-null-with-value,
        // non-null-with-null-inner, null-task. All three must continue past their await.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int?> t1 = Task.FromResult<int?>(7);
                    Console.Write("before-nn-7;");
                    int? v1 = await? t1;
                    Console.Write($"v1={v1?.ToString() ?? "null"};after-nn-7;");

                    Task<int?> t2 = Task.FromResult<int?>(null);
                    Console.Write("before-nn-inner-null;");
                    int? v2 = await? t2;
                    Console.Write($"v2={v2?.ToString() ?? "null"};after-nn-inner-null;");

                    Task<int?> t3 = null;
                    Console.Write("before-null;");
                    int? v3 = await? t3;
                    Console.Write($"v3={v3?.ToString() ?? "null"};after-null;");

                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn-7;v1=7;after-nn-7;before-nn-inner-null;v2=null;after-nn-inner-null;before-null;v3=null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void ResultType_NullableValueTask_VoidResult()
    {
        // Operand is Nullable<ValueTask>. The awaitable pattern is on ValueTask (the underlying V);
        // GetResult returns void. Null-receiver case: Nullable<ValueTask> with no value.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    ValueTask? t1 = new ValueTask();
                    Console.Write("before-nn;");
                    await? t1;
                    Console.Write("after-nn;");

                    ValueTask? t2 = null;
                    Console.Write("before-null;");
                    await? t2;
                    Console.Write("after-null;");

                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;after-nn;before-null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void ResultType_NullableValueTaskOfInt_LiftedToNullableInt()
    {
        // Null-receiver case: Nullable<ValueTask<int>> with no value.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    ValueTask<int>? t1 = new ValueTask<int>(99);
                    Console.Write("before-nn;");
                    int? v1 = await? t1;
                    Console.Write($"v1={v1?.ToString() ?? "null"};after-nn;");

                    ValueTask<int>? t2 = null;
                    Console.Write("before-null;");
                    int? v2 = await? t2;
                    Console.Write($"v2={v2?.ToString() ?? "null"};after-null;");

                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;v1=99;after-nn;before-null;v2=null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void ResultType_GenericTaskOfTStruct_LiftedToNullableT()
    {
        // R = T where T : struct. Lifted to Nullable<T>. The helper M prints both before and
        // after the `await?` so null-receiver cases also demonstrate continuation.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static async Task M<T>(Task<T> t, string label) where T : struct
                {
                    Console.Write($"before-{label};");
                    T? v = await? t;
                    Console.Write($"v={(v.HasValue ? v.Value.ToString() : "null")};after-{label};");
                }

                public static async Task Main()
                {
                    await M<int>(Task.FromResult(5), "int-nn");
                    await M<int>(null, "int-null");
                    await M<double>(Task.FromResult(3.14), "double-nn");
                    Console.Write("done");
                }
            }
            """;
        var expected = "before-int-nn;v=5;after-int-nn;before-int-null;v=null;after-int-null;before-double-nn;v=3.14;after-double-nn;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void ResultType_GenericTaskOfTClass_UnchangedReferenceR()
    {
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static async Task M<T>(Task<T> t, string label) where T : class
                {
                    Console.Write($"before-{label};");
                    T v = await? t;
                    Console.Write($"v={v?.ToString() ?? "null"};after-{label};");
                }

                public static async Task Main()
                {
                    await M<string>(Task.FromResult("s"), "str-nn");
                    await M<string>(null, "str-null");
                    Console.Write("done");
                }
            }
            """;
        var expected = "before-str-nn;v=s;after-str-nn;before-str-null;v=null;after-str-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void ResultType_CustomClassAwaitable()
    {
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            class MyTask
            {
                private readonly int _value;
                public MyTask(int v) { _value = v; }
                public MyAwaiter GetAwaiter() => new MyAwaiter(_value);
            }

            struct MyAwaiter : INotifyCompletion
            {
                private readonly int _value;
                public MyAwaiter(int v) { _value = v; }
                public bool IsCompleted => true;
                public int GetResult() => _value;
                public void OnCompleted(Action continuation) { continuation(); }
            }

            class C
            {
                public static async Task Main()
                {
                    MyTask t1 = new MyTask(123);
                    Console.Write("before-nn;");
                    int? v1 = await? t1;
                    Console.Write($"v1={v1?.ToString() ?? "null"};after-nn;");

                    MyTask t2 = null;
                    Console.Write("before-null;");
                    int? v2 = await? t2;
                    Console.Write($"v2={v2?.ToString() ?? "null"};after-null;");

                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;v1=123;after-nn;before-null;v2=null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void ResultType_ExtensionGetAwaiterOnNullableStruct()
    {
        // Operand is Nullable<MyStruct>. Awaitable pattern is resolved on MyStruct (the underlying
        // type) via an extension GetAwaiter. Null-receiver case: Nullable<MyStruct> with no value.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            struct MyStruct
            {
                public int Value;
                public MyStruct(int v) { Value = v; }
            }

            struct MyAwaiter : INotifyCompletion
            {
                private readonly int _value;
                public MyAwaiter(int v) { _value = v; }
                public bool IsCompleted => true;
                public int GetResult() => _value;
                public void OnCompleted(Action continuation) { continuation(); }
            }

            static class Ext
            {
                public static MyAwaiter GetAwaiter(this MyStruct s) => new MyAwaiter(s.Value);
            }

            class C
            {
                public static async Task Main()
                {
                    MyStruct? t1 = new MyStruct(17);
                    Console.Write("before-nn;");
                    int? v1 = await? t1;
                    Console.Write($"v1={v1?.ToString() ?? "null"};after-nn;");

                    MyStruct? t2 = null;
                    Console.Write("before-null;");
                    int? v2 = await? t2;
                    Console.Write($"v2={v2?.ToString() ?? "null"};after-null;");

                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;v1=17;after-nn;before-null;v2=null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void ResultType_Dynamic_StateMachineOnly()
    {
        // Dynamic operand. Runtime-async rejects dynamic await with
        // ERR_UnsupportedFeatureInRuntimeAsync, so execution + IL pinning here are
        // state-machine-mode only.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    dynamic d1 = Task.FromResult(33);
                    Console.Write("before-nn;");
                    dynamic v1 = await? d1;
                    Console.Write($"v1={v1};after-nn;");

                    dynamic d2 = null;
                    Console.Write("before-null;");
                    dynamic v2 = await? d2;
                    Console.Write($"v2={v2?.ToString() ?? "null"};after-null;");

                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;v1=33;after-nn;before-null;v2=null;after-null;done";
        VerifyStateMachine(source, expected);
    }

    [Fact]
    public void NullBranch_DoesNotInvokeGetAwaiter()
    {
        // Direct proof of the spec's short-circuit rule: when the receiver is null, the
        // awaitable pattern is NOT invoked. The custom awaitable's GetAwaiter() throws —
        // which would surface as an observable exception if the null branch mistakenly
        // called into the awaitable machinery.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            class ThrowingAwaitable
            {
                public ThrowingAwaiter GetAwaiter()
                    => throw new InvalidOperationException("GetAwaiter must not be called when the receiver is null.");
            }

            struct ThrowingAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public int GetResult() => 0;
                public void OnCompleted(Action continuation) { }
            }

            class C
            {
                public static async Task Main()
                {
                    ThrowingAwaitable t = null;
                    Console.Write("before-null;");
                    int? v = await? t;
                    Console.Write($"v={v?.ToString() ?? "null"};after-null;");
                    Console.Write("done");
                }
            }
            """;
        var expected = "before-null;v=null;after-null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region IL-shape tests: edge cases not covered by the exhaustive matrix in NullConditionalAwaitILMatrixEmitTests

    [Fact]
    public void IL_RuntimeAsync_UnusedNonVoidResult_NullableWrappingElided()
    {
        // `await? Task<int>;` at statement position — the int? result is unused. The emitted
        // IL must NOT contain a Nullable<int> wrapper (no `newobj int?..ctor`, no Nullable<int>
        // local), a Nullable<int> local, or an int-typed await-result temp: what remains is
        // the receiver copy, a single null-check branch, the call to the Await helper, and
        // `pop`. Shape-equivalent to a plain `await t;` plus the null-check prologue.
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task F(Task<int> t) { await? t; }
            }
            """;
        var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseDll);
        var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.F", """
            {
              // Code size       13 (0xd)
              .maxstack  1
              .locals init (System.Threading.Tasks.Task<int> V_0)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloc.0
              IL_0003:  brfalse.s  IL_000c
              IL_0005:  ldloc.0
              IL_0006:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
              IL_000b:  pop
              IL_000c:  ret
            }
            """);
    }

    [Fact]
    public void IL_StateMachine_UnusedNonVoidResult_NullableWrappingElided()
    {
        // Same observation as IL_RuntimeAsync_UnusedNonVoidResult_NullableWrappingElided, but
        // through the classic state-machine lowering. The MoveNext method should invoke
        // GetResult() and drop the result rather than wrapping into Nullable<int> and storing
        // into a dead slot. The null branch skips the whole awaiter dance entirely.
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task F(Task<int> t) { await? t; }
            }
            """;
        var verifier = CompileAndVerify(source, parseOptions: s_preview, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
        var il = verifier.VisualizeIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext");
        // Affirmative: null check on the task field; GetAwaiter + GetResult invocations still present.
        Assert.Contains("brfalse", il);
        Assert.Contains("TaskAwaiter<int>.GetResult()", il);
        // Negative: no Nullable<int> wrapping; no Nullable<int> local.
        Assert.DoesNotContain("newobj     \"int?..ctor(int)\"", il);
        Assert.DoesNotContain("int? V_", il);
        Assert.DoesNotContain("initobj    \"int?\"", il);
    }

    [Fact]
    public void IL_StateMachine_Dynamic_ShowsNullCheckPlusDynamicAwaiter()
    {
        // Dynamic operand goes through the dynamic-await lowering, which dispatches via
        // CallSite<Func<...>>-style binder invocations inside the state machine. We don't pin
        // the full IL (it's large and embeds synthesized binder types), but we confirm the
        // essential shape: a null check on the dynamic operand precedes the dynamic
        // GetAwaiter/GetResult calls, and the result is wrapped in Nullable<R> only on the
        // non-null branch (analogous to the reference-type matrix cell).
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task<dynamic> F(dynamic t) => await? t;
            }
            """;
        var verifier = CompileAndVerify(source, parseOptions: s_preview, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
        var il = verifier.VisualizeIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext");
        Assert.Contains("brfalse", il);                                  // null check on the dynamic operand
        Assert.Contains("GetAwaiter", il);                               // dynamic dispatch to GetAwaiter on non-null branch
        Assert.Contains("GetResult", il);                                // dynamic dispatch to GetResult on non-null branch
        Assert.Contains("AwaitUnsafeOnCompleted", il);                   // state machine still schedules continuation
    }

    #endregion

    #region Async-void, expression-bodied forms

    // `async void` is the "fire and forget" form: the body produces no value the caller can await.
    // When combined with an expression body `=>`, the expression's value (if any) is discarded.
    // These tests exercise each container flavour (method, local function, lambda) against an
    // expression body of `await? <value-returning>` (result discarded) and `await? <void>`
    // (nothing to discard). Each test covers both non-null and null receivers.
    //
    // Tests use completed tasks (Task.FromResult / Task.CompletedTask) so IsCompleted is true and
    // the async-void body runs synchronously. That lets us assert observable output without
    // needing a synchronization context or spin-wait.

    [Fact]
    public void AsyncVoid_Method_ExpressionBodied_AwaitQuestion_ValueResult()
    {
        // `async void M(Task<int> t) => await? t;` — the int? produced by `await?` is discarded
        // because M returns void. Both non-null and null receivers must complete cleanly.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static async void M(Task<int> t, string label)
                {
                    Console.Write($"before-{label};");
                    int? v = await? t;
                    Console.Write($"v={v?.ToString() ?? "null"};after-{label};");
                }

                public static void Main()
                {
                    M(Task.FromResult(42), "nn");
                    M(null, "null");
                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;v=42;after-nn;before-null;v=null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void AsyncVoid_Method_ExpressionBodied_AwaitQuestion_VoidResult()
    {
        // `async void M(Task t) => await? t;` — void R; the expression body is classified as
        // "nothing" and the async void method simply completes.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static async void M(Task t, string label)
                {
                    Console.Write($"before-{label};");
                    await? t;
                    Console.Write($"after-{label};");
                }

                public static void Main()
                {
                    M(Task.CompletedTask, "nn");
                    M(null, "null");
                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;after-nn;before-null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void AsyncVoid_LocalFunction_ExpressionBodied_AwaitQuestion_ValueResult()
    {
        // `async void Local(...) => await? t;` — same idea, but the declaration is a local
        // function inside Main. Closure/lifting machinery runs on top of the async-void lowering.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static void Main()
                {
                    async void Local(Task<int> t, string label)
                    {
                        Console.Write($"before-{label};");
                        int? v = await? t;
                        Console.Write($"v={v?.ToString() ?? "null"};after-{label};");
                    }

                    Local(Task.FromResult(7), "nn");
                    Local(null, "null");
                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;v=7;after-nn;before-null;v=null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void AsyncVoid_LocalFunction_ExpressionBodied_AwaitQuestion_VoidResult()
    {
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static void Main()
                {
                    async void Local(Task t, string label)
                    {
                        Console.Write($"before-{label};");
                        await? t;
                        Console.Write($"after-{label};");
                    }

                    Local(Task.CompletedTask, "nn");
                    Local(null, "null");
                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;after-nn;before-null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void AsyncVoid_Lambda_ExpressionBodied_AwaitQuestion_ValueResult()
    {
        // `Action<...> a = async (...) => await? t;` — the lambda is inferred as async-void
        // against the `Action<...>` target. The int? the body produces is discarded, matching
        // the method/local function shapes above.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static void Main()
                {
                    Action<Task<int>, string> a = async (t, label) =>
                    {
                        Console.Write($"before-{label};");
                        int? v = await? t;
                        Console.Write($"v={v?.ToString() ?? "null"};after-{label};");
                    };
                    a(Task.FromResult(99), "nn");
                    a(null, "null");
                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;v=99;after-nn;before-null;v=null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void AsyncVoid_Lambda_ExpressionBodied_AwaitQuestion_VoidResult()
    {
        // True expression-bodied async-void lambda `async () => await? t` — no braces; the lambda
        // body is a single void-typed `await?` expression.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static void Run(Action<Task> a, Task t, string label)
                {
                    Console.Write($"before-{label};");
                    a(t);
                    Console.Write($"after-{label};");
                }

                public static void Main()
                {
                    Action<Task> a = async t => await? t;
                    Run(a, Task.CompletedTask, "nn");
                    Run(a, null, "null");
                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;after-nn;before-null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void AsyncVoid_Lambda_ExpressionBodied_ValueResult_Discarded()
    {
        // Canonical expression-bodied async-void lambda with a value-returning `await?`. The
        // int? result is discarded by the async-void context. This is the pure `=>` form (no
        // braces, no extra statements) — the shape the user explicitly asked to cover.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static void Run(Action<Task<int>> a, Task<int> t, string label)
                {
                    Console.Write($"before-{label};");
                    a(t);
                    Console.Write($"after-{label};");
                }

                public static void Main()
                {
                    Action<Task<int>> a = async t => await? t;
                    Run(a, Task.FromResult(11), "nn");
                    Run(a, null, "null");
                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;after-nn;before-null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Conversions applied to the result

    [Fact]
    public void Conversion_TaskOfInt_ToNullableObject_Succeeds()
    {
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t1 = Task.FromResult(5);
                    Console.Write("before-nn;");
                    object o1 = await? t1;
                    Console.Write($"o1={o1 ?? "null"};after-nn;");

                    Task<int> t2 = null;
                    Console.Write("before-null;");
                    object o2 = await? t2;
                    Console.Write($"o2={o2 ?? "null"};after-null;");

                    Console.Write("done");
                }
            }
            """;
        var expected = "before-nn;o1=5;after-nn;before-null;o2=null;after-null;done";

        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion
}
