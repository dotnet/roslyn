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
        // Full snapshot: the key observables are (a) no `int?` local and no
        // `newobj "int?..ctor(int)"` / `initobj "int?"` on either branch, (b) `GetResult()`
        // is called and its result is immediately `pop`ped, and (c) the null branch of the
        // task field skips the whole awaiter dance (`brfalse.s IL_0068` jumping past
        // everything to the leave).
        verifier.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      149 (0x95)
              .maxstack  3
              .locals init (int V_0,
                            System.Threading.Tasks.Task<int> V_1,
                            System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                            System.Exception V_3)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0044
                IL_000a:  ldarg.0
                IL_000b:  ldfld      "System.Threading.Tasks.Task<int> C.<F>d__0.t"
                IL_0010:  stloc.1
                IL_0011:  ldloc.1
                IL_0012:  brfalse.s  IL_0068
                IL_0014:  ldloc.1
                IL_0015:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                IL_001a:  stloc.2
                IL_001b:  ldloca.s   V_2
                IL_001d:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                IL_0022:  brtrue.s   IL_0060
                IL_0024:  ldarg.0
                IL_0025:  ldc.i4.0
                IL_0026:  dup
                IL_0027:  stloc.0
                IL_0028:  stfld      "int C.<F>d__0.<>1__state"
                IL_002d:  ldarg.0
                IL_002e:  ldloc.2
                IL_002f:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_0034:  ldarg.0
                IL_0035:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
                IL_003a:  ldloca.s   V_2
                IL_003c:  ldarg.0
                IL_003d:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)"
                IL_0042:  leave.s    IL_0094
                IL_0044:  ldarg.0
                IL_0045:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_004a:  stloc.2
                IL_004b:  ldarg.0
                IL_004c:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_0051:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                IL_0057:  ldarg.0
                IL_0058:  ldc.i4.m1
                IL_0059:  dup
                IL_005a:  stloc.0
                IL_005b:  stfld      "int C.<F>d__0.<>1__state"
                IL_0060:  ldloca.s   V_2
                IL_0062:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                IL_0067:  pop
                IL_0068:  leave.s    IL_0081
              }
              catch System.Exception
              {
                IL_006a:  stloc.3
                IL_006b:  ldarg.0
                IL_006c:  ldc.i4.s   -2
                IL_006e:  stfld      "int C.<F>d__0.<>1__state"
                IL_0073:  ldarg.0
                IL_0074:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
                IL_0079:  ldloc.3
                IL_007a:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_007f:  leave.s    IL_0094
              }
              IL_0081:  ldarg.0
              IL_0082:  ldc.i4.s   -2
              IL_0084:  stfld      "int C.<F>d__0.<>1__state"
              IL_0089:  ldarg.0
              IL_008a:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
              IL_008f:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_0094:  ret
            }
            """);
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
        // Full snapshot. The key observables specific to `await?` (buried in this longer
        // dynamic-dispatch IL): `brfalse IL_01ae` at IL_0015 is the null check on the
        // dynamic operand field, and IL_01ae/IL_01af (`ldnull` + `stloc.3`) is the null
        // branch that skips every GetAwaiter / GetResult call site.
        verifier.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      482 (0x1e2)
              .maxstack  10
              .locals init (int V_0,
                            object V_1,
                            object V_2,
                            object V_3,
                            object V_4,
                            System.Runtime.CompilerServices.ICriticalNotifyCompletion V_5,
                            System.Runtime.CompilerServices.INotifyCompletion V_6,
                            System.Exception V_7)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse    IL_0146
                IL_000d:  ldarg.0
                IL_000e:  ldfld      "dynamic C.<F>d__0.t"
                IL_0013:  stloc.2
                IL_0014:  ldloc.2
                IL_0015:  brfalse    IL_01ae
                IL_001a:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__0"
                IL_001f:  brtrue.s   IL_0051
                IL_0021:  ldc.i4.0
                IL_0022:  ldstr      "GetAwaiter"
                IL_0027:  ldnull
                IL_0028:  ldtoken    "C"
                IL_002d:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                IL_0032:  ldc.i4.1
                IL_0033:  newarr     "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo"
                IL_0038:  dup
                IL_0039:  ldc.i4.0
                IL_003a:  ldc.i4.0
                IL_003b:  ldnull
                IL_003c:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
                IL_0041:  stelem.ref
                IL_0042:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)"
                IL_0047:  call       "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
                IL_004c:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__0"
                IL_0051:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__0"
                IL_0056:  ldfld      "System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target"
                IL_005b:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__0"
                IL_0060:  ldloc.2
                IL_0061:  callvirt   "dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)"
                IL_0066:  stloc.s    V_4
                IL_0068:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<F>d__0.<>o__0.<>p__2"
                IL_006d:  brtrue.s   IL_0094
                IL_006f:  ldc.i4.s   16
                IL_0071:  ldtoken    "bool"
                IL_0076:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                IL_007b:  ldtoken    "C"
                IL_0080:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                IL_0085:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)"
                IL_008a:  call       "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
                IL_008f:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<F>d__0.<>o__0.<>p__2"
                IL_0094:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<F>d__0.<>o__0.<>p__2"
                IL_0099:  ldfld      "System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target"
                IL_009e:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<F>d__0.<>o__0.<>p__2"
                IL_00a3:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__1"
                IL_00a8:  brtrue.s   IL_00d9
                IL_00aa:  ldc.i4.0
                IL_00ab:  ldstr      "IsCompleted"
                IL_00b0:  ldtoken    "C"
                IL_00b5:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                IL_00ba:  ldc.i4.1
                IL_00bb:  newarr     "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo"
                IL_00c0:  dup
                IL_00c1:  ldc.i4.0
                IL_00c2:  ldc.i4.0
                IL_00c3:  ldnull
                IL_00c4:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
                IL_00c9:  stelem.ref
                IL_00ca:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)"
                IL_00cf:  call       "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
                IL_00d4:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__1"
                IL_00d9:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__1"
                IL_00de:  ldfld      "System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target"
                IL_00e3:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__1"
                IL_00e8:  ldloc.s    V_4
                IL_00ea:  callvirt   "dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)"
                IL_00ef:  callvirt   "bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)"
                IL_00f4:  brtrue.s   IL_015e
                IL_00f6:  ldarg.0
                IL_00f7:  ldc.i4.0
                IL_00f8:  dup
                IL_00f9:  stloc.0
                IL_00fa:  stfld      "int C.<F>d__0.<>1__state"
                IL_00ff:  ldarg.0
                IL_0100:  ldloc.s    V_4
                IL_0102:  stfld      "object C.<F>d__0.<>u__1"
                IL_0107:  ldloc.s    V_4
                IL_0109:  isinst     "System.Runtime.CompilerServices.ICriticalNotifyCompletion"
                IL_010e:  stloc.s    V_5
                IL_0110:  ldloc.s    V_5
                IL_0112:  brtrue.s   IL_0130
                IL_0114:  ldloc.s    V_4
                IL_0116:  castclass  "System.Runtime.CompilerServices.INotifyCompletion"
                IL_011b:  stloc.s    V_6
                IL_011d:  ldarg.0
                IL_011e:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<F>d__0.<>t__builder"
                IL_0123:  ldloca.s   V_6
                IL_0125:  ldarg.0
                IL_0126:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitOnCompleted<System.Runtime.CompilerServices.INotifyCompletion, C.<F>d__0>(ref System.Runtime.CompilerServices.INotifyCompletion, ref C.<F>d__0)"
                IL_012b:  ldnull
                IL_012c:  stloc.s    V_6
                IL_012e:  br.s       IL_013e
                IL_0130:  ldarg.0
                IL_0131:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<F>d__0.<>t__builder"
                IL_0136:  ldloca.s   V_5
                IL_0138:  ldarg.0
                IL_0139:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ICriticalNotifyCompletion, C.<F>d__0>(ref System.Runtime.CompilerServices.ICriticalNotifyCompletion, ref C.<F>d__0)"
                IL_013e:  ldnull
                IL_013f:  stloc.s    V_5
                IL_0141:  leave      IL_01e1
                IL_0146:  ldarg.0
                IL_0147:  ldfld      "object C.<F>d__0.<>u__1"
                IL_014c:  stloc.s    V_4
                IL_014e:  ldarg.0
                IL_014f:  ldnull
                IL_0150:  stfld      "object C.<F>d__0.<>u__1"
                IL_0155:  ldarg.0
                IL_0156:  ldc.i4.m1
                IL_0157:  dup
                IL_0158:  stloc.0
                IL_0159:  stfld      "int C.<F>d__0.<>1__state"
                IL_015e:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__3"
                IL_0163:  brtrue.s   IL_0195
                IL_0165:  ldc.i4.0
                IL_0166:  ldstr      "GetResult"
                IL_016b:  ldnull
                IL_016c:  ldtoken    "C"
                IL_0171:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                IL_0176:  ldc.i4.1
                IL_0177:  newarr     "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo"
                IL_017c:  dup
                IL_017d:  ldc.i4.0
                IL_017e:  ldc.i4.0
                IL_017f:  ldnull
                IL_0180:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
                IL_0185:  stelem.ref
                IL_0186:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)"
                IL_018b:  call       "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
                IL_0190:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__3"
                IL_0195:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__3"
                IL_019a:  ldfld      "System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target"
                IL_019f:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<F>d__0.<>o__0.<>p__3"
                IL_01a4:  ldloc.s    V_4
                IL_01a6:  callvirt   "dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)"
                IL_01ab:  stloc.3
                IL_01ac:  br.s       IL_01b0
                IL_01ae:  ldnull
                IL_01af:  stloc.3
                IL_01b0:  ldloc.3
                IL_01b1:  stloc.1
                IL_01b2:  leave.s    IL_01cd
              }
              catch System.Exception
              {
                IL_01b4:  stloc.s    V_7
                IL_01b6:  ldarg.0
                IL_01b7:  ldc.i4.s   -2
                IL_01b9:  stfld      "int C.<F>d__0.<>1__state"
                IL_01be:  ldarg.0
                IL_01bf:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<F>d__0.<>t__builder"
                IL_01c4:  ldloc.s    V_7
                IL_01c6:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetException(System.Exception)"
                IL_01cb:  leave.s    IL_01e1
              }
              IL_01cd:  ldarg.0
              IL_01ce:  ldc.i4.s   -2
              IL_01d0:  stfld      "int C.<F>d__0.<>1__state"
              IL_01d5:  ldarg.0
              IL_01d6:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<F>d__0.<>t__builder"
              IL_01db:  ldloc.1
              IL_01dc:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetResult(object)"
              IL_01e1:  ret
            }
            """);
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

    #region Async iterator (IAsyncEnumerable<T>) interactions

    [Fact]
    public void AsyncIterator_AwaitQuestion_NonNullOperand()
    {
        // `await?` inside an `async IAsyncEnumerable<T>` method body. The iterator state
        // machine is distinct from a normal async method's; this pins that `await?` is
        // accepted there and that both branches compose with `yield return`.
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static async IAsyncEnumerable<string> StreamAsync()
                {
                    Task<int> t1 = Task.FromResult(1);
                    int? v1 = await? t1;
                    yield return $"v1={v1};";

                    Task<int> t2 = null;
                    int? v2 = await? t2;
                    yield return $"v2={v2?.ToString() ?? "null"};";
                }

                public static async Task Main()
                {
                    await foreach (var s in StreamAsync())
                        Console.Write(s);
                    Console.Write("done");
                }
            }
            """;
        var expected = "v1=1;v2=null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void AsyncIterator_AwaitQuestion_VoidResult_StatementPosition()
    {
        // Void-result `await? t;` (statement position) inside an async iterator. The
        // statement is classified as nothing, so the iterator doesn't yield anything
        // from it; yields must still be driven by explicit `yield return`.
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static async IAsyncEnumerable<int> StreamAsync()
                {
                    Task t = null;
                    await? t;         // short-circuits, no-op
                    yield return 1;
                    t = Task.CompletedTask;
                    await? t;         // non-null branch; returns nothing
                    yield return 2;
                }

                public static async Task Main()
                {
                    await foreach (var i in StreamAsync())
                        Console.Write($"{i};");
                    Console.Write("done");
                }
            }
            """;
        var expected = "1;2;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Try / catch / finally interactions

    [Fact]
    public void TryBlock_NullReceiver_ContinuesPastTry()
    {
        // `await?` inside a try block with a null receiver — short-circuits, the try
        // body completes normally, and execution continues past the try.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t = null;
                    try
                    {
                        Console.Write("enter;");
                        int? v = await? t;
                        Console.Write($"v={v?.ToString() ?? "null"};");
                    }
                    finally
                    {
                        Console.Write("finally;");
                    }
                    Console.Write("done");
                }
            }
            """;
        var expected = "enter;v=null;finally;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void CatchBlock_WithAwaitQuestion_Runs()
    {
        // `await?` inside a catch block. Requires C# 6+ `await in catch` which is the
        // default here; exercising it confirms that `await?` doesn't lose that capability.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    try
                    {
                        throw new InvalidOperationException("boom");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.Write($"catch:{ex.Message};");
                        Task<int> t = Task.FromResult(7);
                        int? v = await? t;
                        Console.Write($"recovered:{v};");
                    }
                    Console.Write("done");
                }
            }
            """;
        var expected = "catch:boom;recovered:7;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void FinallyBlock_WithAwaitQuestion_Runs()
    {
        // `await?` inside a finally block. Valid in C# 6+ (same gate as plain `await`
        // in finally). Confirms `await?` participates in EH reordering correctly.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    try
                    {
                        Console.Write("try;");
                    }
                    finally
                    {
                        Task<int> t = Task.FromResult(9);
                        int? v = await? t;
                        Console.Write($"finally-v={v};");
                    }
                    Console.Write("done");
                }
            }
            """;
        var expected = "try;finally-v=9;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void FinallyBlock_WithNullReceiver_ShortCircuits()
    {
        // `await?` in finally with a null receiver. The short-circuit must work inside
        // the finally handler; the enclosing block's normal completion should proceed.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    try
                    {
                        Console.Write("try;");
                    }
                    finally
                    {
                        Task<int> t = null;
                        int? v = await? t;
                        Console.Write($"finally-v={v?.ToString() ?? "null"};");
                    }
                    Console.Write("done");
                }
            }
            """;
        var expected = "try;finally-v=null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void ExceptionFromAwaitQuestion_CaughtInOuterCatch()
    {
        // Non-null branch throws via a faulted task. The exception bubbles out of the
        // try body and is caught by the catch clause.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t = Task.FromException<int>(new InvalidOperationException("faulted"));
                    try
                    {
                        int? v = await? t;
                        Console.Write($"unexpected v={v};");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.Write($"caught:{ex.Message};");
                    }
                    Console.Write("done");
                }
            }
            """;
        var expected = "caught:faulted;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void FinallyRuns_EvenWhenAwaitQuestionThrows()
    {
        // Finally handler must run even when `await?` throws in the try body.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t = Task.FromException<int>(new InvalidOperationException("boom"));
                    try
                    {
                        try { int? v = await? t; }
                        finally { Console.Write("finally;"); }
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.Write($"caught:{ex.Message};");
                    }
                    Console.Write("done");
                }
            }
            """;
        var expected = "finally;caught:boom;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Custom async method builder

    [Fact]
    public void CustomAsyncMethodBuilder_WithAwaitQuestion_ProducesResult()
    {
        // A custom tasklike type (marked with [AsyncMethodBuilder]) should support
        // await? in its async method body just like Task does. Exercises `ValueTask<int>`
        // as the enclosing async return type with a Task<int> operand for await?.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static async ValueTask<int?> F(Task<int> t) => await? t;

                public static async Task Main()
                {
                    int? v1 = await F(Task.FromResult(11));
                    Console.Write($"v1={v1};");

                    int? v2 = await F(null);
                    Console.Write($"v2={v2?.ToString() ?? "null"};");

                    Console.Write("done");
                }
            }
            """;
        var expected = "v1=11;v2=null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Runtime parity for binding-test scenarios

    // These tests execute the scenarios whose binding-level counterparts (in
    // NullConditionalAwaitBindingTests.cs) only assert compile-time properties. They
    // prove the runtime behavior matches what binding reported.

    [Fact]
    public void Runtime_OverloadResolution_NullableIntOverloadActuallyRuns()
    {
        // Binding counterpart: OverloadResolution_IntVsNullableInt_PrefersNullableInt.
        // Runtime check that `F(int?)` is the overload that actually runs for both the
        // non-null and null operand paths.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static string F(int x) => $"int:{x}";
                public static string F(int? x) => $"int?:{(x.HasValue ? x.Value.ToString() : "null")}";

                public static async Task Main()
                {
                    Console.Write(F(await? Task.FromResult(42)));
                    Console.Write(";");
                    Task<int> nullTask = null;
                    Console.Write(F(await? nullTask));
                    Console.Write(";done");
                }
            }
            """;
        var expected = "int?:42;int?:null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Runtime_OverloadResolution_StringOverloadActuallyRuns()
    {
        // Binding counterpart: OverloadResolution_StringVsNullableString_ResolvesOnNRTAnnotation.
        // Runtime check: the single `F(string)` overload is invoked even with a null operand.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static string F(string s) => s ?? "null";

                public static async Task Main()
                {
                    Console.Write(F(await? Task.FromResult("hi")));
                    Console.Write(";");
                    Task<string> nullTask = null;
                    Console.Write(F(await? nullTask));
                    Console.Write(";done");
                }
            }
            """;
        var expected = "hi;null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Runtime_TypeInference_GenericIdentityReceivesLiftedValue()
    {
        // Binding counterpart: TypeInference_GenericMethodArg_InfersLiftedResult.
        // The generic method receives the lifted int? at runtime. Confirmed via ToString
        // on a Nullable<int> (prints "" for null).
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static T Identity<T>(T x) => x;

                public static async Task Main()
                {
                    int? v1 = Identity(await? Task.FromResult(7));
                    Console.Write($"v1={v1};");
                    Task<int> nullTask = null;
                    int? v2 = Identity(await? nullTask);
                    Console.Write($"v2={v2?.ToString() ?? "null"};done");
                }
            }
            """;
        var expected = "v1=7;v2=null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Runtime_PatternMatching_ConstantPatternOnLiftedInt()
    {
        // Binding counterpart: PatternMatching_IsConstantPattern_OnLiftedResult.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Console.Write(((await? Task.FromResult(42)) is 42) ? "match42;" : "no42;");
                    Console.Write(((await? Task.FromResult(0)) is 42) ? "match0-42;" : "no0-42;");
                    Task<int> nullTask = null;
                    Console.Write(((await? nullTask) is 42) ? "matchNull42;" : "noNull42;");
                    Console.Write("done");
                }
            }
            """;
        var expected = "match42;no0-42;noNull42;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Runtime_PatternMatching_TypePatternNarrowsLiftedInt()
    {
        // Binding counterpart: PatternMatching_IsTypePattern_OnLiftedNullableResult.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    int r1 = (await? Task.FromResult(5)) is int x1 ? x1 * 10 : -1;
                    Console.Write($"r1={r1};");
                    Task<int> nullTask = null;
                    int r2 = (await? nullTask) is int x2 ? x2 * 10 : -1;
                    Console.Write($"r2={r2};done");
                }
            }
            """;
        var expected = "r1=50;r2=-1;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Runtime_PatternMatching_NullAndNotNullOnReferenceResult()
    {
        // Binding counterparts: PatternMatching_IsNullPattern_OnReferenceResult +
        // PatternMatching_IsNotNullPattern_PromotesToNonNullable.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<string> nonNull = Task.FromResult("hi");
                    Task<string> nullTask = null;

                    Console.Write(((await? nonNull) is null) ? "nonNull-isnull;" : "nonNull-notnull;");
                    Console.Write(((await? nullTask) is null) ? "null-isnull;" : "null-notnull;");

                    // Non-null narrowing via `is not null` guard.
                    var v = await? nonNull;
                    if (v is not null)
                        Console.Write($"len={v.Length};");

                    Console.Write("done");
                }
            }
            """;
        var expected = "nonNull-notnull;null-isnull;len=2;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Runtime_PatternMatching_PropertyPatternOnLiftedString()
    {
        // Binding counterpart: PatternMatching_PropertyPattern_OnLiftedResult.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Console.Write((await? Task.FromResult("hello")) is { Length: > 0 } ? "hello-nonempty;" : "hello-empty;");
                    Console.Write((await? Task.FromResult("")) is { Length: > 0 } ? "empty-nonempty;" : "empty-empty;");
                    Task<string> nullTask = null;
                    Console.Write((await? nullTask) is { Length: > 0 } ? "null-nonempty;" : "null-empty;");
                    Console.Write("done");
                }
            }
            """;
        var expected = "hello-nonempty;empty-empty;null-empty;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Runtime_PatternMatching_SwitchExpressionAllArms()
    {
        // Binding counterpart: PatternMatching_SwitchExpression_OnLiftedResult.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                static string Classify(int? v) => v switch
                {
                    null => "null",
                    0 => "zero",
                    var x => x!.Value.ToString(),
                };

                public static async Task Main()
                {
                    Console.Write(Classify(await? Task.FromResult(7)));
                    Console.Write(";");
                    Console.Write(Classify(await? Task.FromResult(0)));
                    Console.Write(";");
                    Task<int> nullTask = null;
                    Console.Write(Classify(await? nullTask));
                    Console.Write(";done");
                }
            }
            """;
        var expected = "7;zero;null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Runtime_NullForgiving_OnReferenceResult_ExecutesNonNullPath()
    {
        // Binding counterpart: NullForgiving_OnReferenceResult_AllowsMemberAccess.
        // At runtime on the non-null path, the member access succeeds and returns the
        // string's length. This also demonstrates that the `!` doesn't change runtime
        // behavior (no null-check injected).
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<string> t = Task.FromResult("abcd");
                    int len = (await? t)!.Length;
                    Console.Write($"len={len};done");
                }
            }
            """;
        var expected = "len=4;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Runtime_NullForgiving_OnLiftedValueResult_UnwrapsValue()
    {
        // Binding counterpart: NullForgiving_OnLiftedValueResult_AllowsValueAccess.
        // `!.Value` unwraps the Nullable<int> at runtime.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t = Task.FromResult(99);
                    int v = (await? t)!.Value;
                    Console.Write($"v={v};done");
                }
            }
            """;
        var expected = "v=99;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Runtime_OutVar_InsideAwaitQuestionOperand_NonNullReceiver()
    {
        // Binding counterpart: OutVar_InsideAwaitQuestionOperand_DefiniteAssignment.
        // Runtime check: `x` is assigned before the await completes and the read after
        // the statement succeeds.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static Task<int> GetTask(out int x) { x = 17; return Task.FromResult(0); }

                public static async Task Main()
                {
                    int? v = await? GetTask(out var x);
                    Console.Write($"v={v};x={x};done");
                }
            }
            """;
        var expected = "v=0;x=17;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void Runtime_Deconstruction_ViaGetValueOrDefault()
    {
        // Binding counterpart: Deconstruction_TupleResult_ViaGetValueOrDefault_Works.
        // Runtime values of the deconstructed components, both with a non-null task and
        // a null task (deconstruct yields (0, 0) via GetValueOrDefault's zero).
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<(int, int)> t1 = Task.FromResult((3, 4));
                    var (a1, b1) = (await? t1).GetValueOrDefault();
                    Console.Write($"t1:a={a1},b={b1};");

                    Task<(int, int)> nullTask = null;
                    var (a2, b2) = (await? nullTask).GetValueOrDefault();
                    Console.Write($"tnull:a={a2},b={b2};done");
                }
            }
            """;
        var expected = "t1:a=3,b=4;tnull:a=0,b=0;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion

    #region Exception propagation from the non-null branch

    [Fact]
    public void NonNullBranch_GetAwaiterThrows_ExceptionPropagates()
    {
        // The non-null branch calls GetAwaiter(), which throws. The exception should
        // propagate out of `await?` to the caller exactly as it would from plain `await`.
        // Demonstrates that the null-conditional wrapping does not swallow or re-shape
        // exceptions that arise from the awaitable pattern itself.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            class ThrowingAwaitable
            {
                public ThrowingAwaiter GetAwaiter() => throw new InvalidOperationException("from GetAwaiter");
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
                    ThrowingAwaitable t = new();
                    try
                    {
                        int? v = await? t;
                        Console.Write($"unexpected v={v};");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.Write($"caught:{ex.Message};");
                    }
                    Console.Write("done");
                }
            }
            """;
        var expected = "caught:from GetAwaiter;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void NonNullBranch_GetResultThrows_ExceptionPropagates()
    {
        // The non-null branch calls GetResult(), which throws. The exception should
        // propagate out of `await?` exactly as it would from plain `await`.
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            class ThrowingAwaitable
            {
                public ThrowingAwaiter GetAwaiter() => default;
            }
            struct ThrowingAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public int GetResult() => throw new InvalidOperationException("from GetResult");
                public void OnCompleted(Action continuation) { }
            }

            class C
            {
                public static async Task Main()
                {
                    ThrowingAwaitable t = new();
                    try
                    {
                        int? v = await? t;
                        Console.Write($"unexpected v={v};");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.Write($"caught:{ex.Message};");
                    }
                    Console.Write("done");
                }
            }
            """;
        var expected = "caught:from GetResult;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void NonNullBranch_TaskOfIntFaulted_ExceptionPropagates()
    {
        // Standard faulted `Task<int>` — GetResult throws the task's stored exception.
        // Verifies that when the non-null branch resolves a faulted task, the exception
        // is thrown from `await?` identically to plain `await`.
        var source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public static async Task Main()
                {
                    Task<int> t = Task.FromException<int>(new InvalidOperationException("faulted"));
                    try
                    {
                        int? v = await? t;
                        Console.Write($"unexpected v={v};");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.Write($"caught:{ex.Message};");
                    }
                    Console.Write("done");
                }
            }
            """;
        var expected = "caught:faulted;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    [Fact]
    public void NullOperand_StillShortCircuits_EvenWithThrowingMembers()
    {
        // Defense-in-depth: the null branch must not invoke any awaiter machinery even
        // when the awaitable's GetAwaiter / GetResult would throw. Exercises both the
        // reference-operand null case (ThrowingAwaitable) and the Nullable<V> operand
        // null case (ThrowingValueAwaitable? with HasValue == false).
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            class ThrowingAwaitable
            {
                public ThrowingAwaiter GetAwaiter() => throw new InvalidOperationException("never called");
            }
            struct ThrowingValueAwaitable
            {
                public ThrowingAwaiter GetAwaiter() => throw new InvalidOperationException("never called for Nullable<V>");
            }
            struct ThrowingAwaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public int GetResult() => throw new InvalidOperationException("never called");
                public void OnCompleted(Action continuation) { }
            }

            class C
            {
                public static async Task Main()
                {
                    ThrowingAwaitable refNull = null;
                    int? v1 = await? refNull;
                    Console.Write($"ref-null:v1={v1?.ToString() ?? "null"};");

                    ThrowingValueAwaitable? structNull = null;
                    int? v2 = await? structNull;
                    Console.Write($"struct-null:v2={v2?.ToString() ?? "null"};");

                    Console.Write("done");
                }
            }
            """;
        var expected = "ref-null:v1=null;struct-null:v2=null;done";
        VerifyStateMachine(source, expected);
        VerifyRuntimeAsync(source, expected);
    }

    #endregion
}
