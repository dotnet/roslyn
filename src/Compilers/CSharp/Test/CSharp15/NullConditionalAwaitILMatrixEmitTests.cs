// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class NullConditionalAwaitILMatrixEmitTests : CSharpTestBase
{
    // Exhaustive IL-shape matrix for `await? e`. Each row is a distinct (receiver-type-kind,
    // result-type-kind) pair and pins both:
    //
    //   * State-machine MoveNext IL (traditional async lowering).
    //   * Runtime-async body IL (AsyncHelpers.Await path).
    //
    // The two "kinds" that drive all shape differences are:
    //
    //   Receiver kind:
    //     A. Reference type        — null-check via `brfalse` on the reference.
    //     B. Nullable<V>           — null-check via `Nullable<V>.get_HasValue`
    //                                + `GetValueOrDefault()` on the non-null branch.
    //
    //   Result R kind (from GetAwaiter().GetResult()):
    //     1. void                  — statement-only; null branch contributes nothing.
    //     2. non-nullable value    — lifted to Nullable<R>; null branch emits `initobj R?`,
    //                                non-null branch emits `newobj R?..ctor(R)`.
    //     3. already-nullable V?   — kept as V?; no `new Nullable<...>` wrapper on either branch.
    //     4. reference             — kept as T; null branch emits `ldnull`.
    //
    // The matrix is A × {1,2,3,4} + B × {1,2,3,4} = 8 cells. Bodies are reduced to a single
    // `await? t` expression so only the null-check + await + lift shapes appear in IL.

    private static readonly CSharpParseOptions s_preview = TestOptions.RegularPreview;

    /// <summary>
    /// Compiles <paramref name="source"/> twice (state-machine and runtime-async) and asserts the
    /// IL of each mode against the expected snapshots. Both assertions run even if one fails so
    /// a mismatch in either mode is visible.
    /// </summary>
    private void VerifyMatrixIL(string source, string stateMachineMoveNextIL, string runtimeAsyncIL)
    {
        var smVerifier = CompileAndVerify(
            source,
            parseOptions: s_preview,
            targetFramework: TargetFramework.NetCoreApp,
            options: TestOptions.ReleaseDll);

        var raComp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseDll);
        var raVerifier = CompileAndVerify(raComp, verify: Verification.Skipped);
        raVerifier.VerifyDiagnostics();

        System.Exception? smFailure = null;
        System.Exception? raFailure = null;
        try { smVerifier.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", stateMachineMoveNextIL); }
        catch (System.Exception ex) { smFailure = ex; }
        try { raVerifier.VerifyIL("C.F", runtimeAsyncIL); }
        catch (System.Exception ex) { raFailure = ex; }
        if (smFailure is not null || raFailure is not null)
        {
            var parts = new[]
            {
                smFailure is not null ? "SM: " + smFailure.Message : null,
                raFailure is not null ? "RA: " + raFailure.Message : null,
            }.Where(m => m is not null);
            throw new System.Exception(string.Join("\n\n==========\n\n", parts));
        }
    }

    // ==========================================================================================
    // A. Reference-type receiver: null-check via `brfalse` on the reference.
    // ==========================================================================================

    [Fact]
    public void Matrix_ReferenceReceiver_VoidR_TaskFamily()
    {
        // Receiver: Task (reference, void R). Statement-position body.
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task F(Task t) { await? t; }
            }
            """;
        VerifyMatrixIL(source, stateMachineMoveNextIL: """
            {
              // Code size      148 (0x94)
              .maxstack  3
              .locals init (int V_0,
                            System.Threading.Tasks.Task V_1,
                            System.Runtime.CompilerServices.TaskAwaiter V_2,
                            System.Exception V_3)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0044
                IL_000a:  ldarg.0
                IL_000b:  ldfld      "System.Threading.Tasks.Task C.<F>d__0.t"
                IL_0010:  stloc.1
                IL_0011:  ldloc.1
                IL_0012:  brfalse.s  IL_0067
                IL_0014:  ldloc.1
                IL_0015:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
                IL_001a:  stloc.2
                IL_001b:  ldloca.s   V_2
                IL_001d:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
                IL_0022:  brtrue.s   IL_0060
                IL_0024:  ldarg.0
                IL_0025:  ldc.i4.0
                IL_0026:  dup
                IL_0027:  stloc.0
                IL_0028:  stfld      "int C.<F>d__0.<>1__state"
                IL_002d:  ldarg.0
                IL_002e:  ldloc.2
                IL_002f:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1"
                IL_0034:  ldarg.0
                IL_0035:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
                IL_003a:  ldloca.s   V_2
                IL_003c:  ldarg.0
                IL_003d:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__0)"
                IL_0042:  leave.s    IL_0093
                IL_0044:  ldarg.0
                IL_0045:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1"
                IL_004a:  stloc.2
                IL_004b:  ldarg.0
                IL_004c:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1"
                IL_0051:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
                IL_0057:  ldarg.0
                IL_0058:  ldc.i4.m1
                IL_0059:  dup
                IL_005a:  stloc.0
                IL_005b:  stfld      "int C.<F>d__0.<>1__state"
                IL_0060:  ldloca.s   V_2
                IL_0062:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
                IL_0067:  leave.s    IL_0080
              }
              catch System.Exception
              {
                IL_0069:  stloc.3
                IL_006a:  ldarg.0
                IL_006b:  ldc.i4.s   -2
                IL_006d:  stfld      "int C.<F>d__0.<>1__state"
                IL_0072:  ldarg.0
                IL_0073:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
                IL_0078:  ldloc.3
                IL_0079:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_007e:  leave.s    IL_0093
              }
              IL_0080:  ldarg.0
              IL_0081:  ldc.i4.s   -2
              IL_0083:  stfld      "int C.<F>d__0.<>1__state"
              IL_0088:  ldarg.0
              IL_0089:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
              IL_008e:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_0093:  ret
            }
            """,
            runtimeAsyncIL: """
            {
              // Code size       12 (0xc)
              .maxstack  1
              .locals init (System.Threading.Tasks.Task V_0)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloc.0
              IL_0003:  brfalse.s  IL_000b
              IL_0005:  ldloc.0
              IL_0006:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void Matrix_ReferenceReceiver_ValueR_LiftedToNullable()
    {
        // Receiver: Task<int> (reference, non-null value R=int). Result lifted to int?.
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task<int?> F(Task<int> t) => await? t;
            }
            """;
        VerifyMatrixIL(source, stateMachineMoveNextIL: """
            {
              // Code size      176 (0xb0)
              .maxstack  3
              .locals init (int V_0,
                            int? V_1,
                            System.Threading.Tasks.Task<int> V_2,
                            int? V_3,
                            int V_4,
                            System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
                            System.Exception V_6)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0046
                IL_000a:  ldarg.0
                IL_000b:  ldfld      "System.Threading.Tasks.Task<int> C.<F>d__0.t"
                IL_0010:  stloc.2
                IL_0011:  ldloc.2
                IL_0012:  brfalse.s  IL_0076
                IL_0014:  ldloc.2
                IL_0015:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                IL_001a:  stloc.s    V_5
                IL_001c:  ldloca.s   V_5
                IL_001e:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                IL_0023:  brtrue.s   IL_0063
                IL_0025:  ldarg.0
                IL_0026:  ldc.i4.0
                IL_0027:  dup
                IL_0028:  stloc.0
                IL_0029:  stfld      "int C.<F>d__0.<>1__state"
                IL_002e:  ldarg.0
                IL_002f:  ldloc.s    V_5
                IL_0031:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_0036:  ldarg.0
                IL_0037:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
                IL_003c:  ldloca.s   V_5
                IL_003e:  ldarg.0
                IL_003f:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)"
                IL_0044:  leave.s    IL_00af
                IL_0046:  ldarg.0
                IL_0047:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_004c:  stloc.s    V_5
                IL_004e:  ldarg.0
                IL_004f:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_0054:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                IL_005a:  ldarg.0
                IL_005b:  ldc.i4.m1
                IL_005c:  dup
                IL_005d:  stloc.0
                IL_005e:  stfld      "int C.<F>d__0.<>1__state"
                IL_0063:  ldloca.s   V_5
                IL_0065:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                IL_006a:  stloc.s    V_4
                IL_006c:  ldloc.s    V_4
                IL_006e:  newobj     "int?..ctor(int)"
                IL_0073:  stloc.3
                IL_0074:  br.s       IL_007e
                IL_0076:  ldloca.s   V_3
                IL_0078:  initobj    "int?"
                IL_007e:  ldloc.3
                IL_007f:  stloc.1
                IL_0080:  leave.s    IL_009b
              }
              catch System.Exception
              {
                IL_0082:  stloc.s    V_6
                IL_0084:  ldarg.0
                IL_0085:  ldc.i4.s   -2
                IL_0087:  stfld      "int C.<F>d__0.<>1__state"
                IL_008c:  ldarg.0
                IL_008d:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
                IL_0092:  ldloc.s    V_6
                IL_0094:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.SetException(System.Exception)"
                IL_0099:  leave.s    IL_00af
              }
              IL_009b:  ldarg.0
              IL_009c:  ldc.i4.s   -2
              IL_009e:  stfld      "int C.<F>d__0.<>1__state"
              IL_00a3:  ldarg.0
              IL_00a4:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
              IL_00a9:  ldloc.1
              IL_00aa:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.SetResult(int?)"
              IL_00af:  ret
            }
            """,
            runtimeAsyncIL: """
            {
              // Code size       32 (0x20)
              .maxstack  2
              .locals init (System.Threading.Tasks.Task<int> V_0,
                            int? V_1,
                            int V_2)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloc.0
              IL_0003:  brfalse.s  IL_0016
              IL_0005:  ldloc.0
              IL_0006:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
              IL_000b:  stloc.2
              IL_000c:  ldloca.s   V_1
              IL_000e:  ldloc.2
              IL_000f:  call       "int?..ctor(int)"
              IL_0014:  br.s       IL_001e
              IL_0016:  ldloca.s   V_1
              IL_0018:  initobj    "int?"
              IL_001e:  ldloc.1
              IL_001f:  ret
            }
            """);
    }

    [Fact]
    public void Matrix_ReferenceReceiver_NullableR_NotDoubleWrapped()
    {
        // Receiver: Task<int?> (reference, already-nullable R=int?). Result kept as int?.
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task<int?> F(Task<int?> t) => await? t;
            }
            """;
        VerifyMatrixIL(source, stateMachineMoveNextIL: """
            {
              // Code size      167 (0xa7)
              .maxstack  3
              .locals init (int V_0,
                            int? V_1,
                            System.Threading.Tasks.Task<int?> V_2,
                            int? V_3,
                            System.Runtime.CompilerServices.TaskAwaiter<int?> V_4,
                            System.Exception V_5)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0046
                IL_000a:  ldarg.0
                IL_000b:  ldfld      "System.Threading.Tasks.Task<int?> C.<F>d__0.t"
                IL_0010:  stloc.2
                IL_0011:  ldloc.2
                IL_0012:  brfalse.s  IL_006d
                IL_0014:  ldloc.2
                IL_0015:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int?> System.Threading.Tasks.Task<int?>.GetAwaiter()"
                IL_001a:  stloc.s    V_4
                IL_001c:  ldloca.s   V_4
                IL_001e:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int?>.IsCompleted.get"
                IL_0023:  brtrue.s   IL_0063
                IL_0025:  ldarg.0
                IL_0026:  ldc.i4.0
                IL_0027:  dup
                IL_0028:  stloc.0
                IL_0029:  stfld      "int C.<F>d__0.<>1__state"
                IL_002e:  ldarg.0
                IL_002f:  ldloc.s    V_4
                IL_0031:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int?> C.<F>d__0.<>u__1"
                IL_0036:  ldarg.0
                IL_0037:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
                IL_003c:  ldloca.s   V_4
                IL_003e:  ldarg.0
                IL_003f:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int?>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int?>, ref C.<F>d__0)"
                IL_0044:  leave.s    IL_00a6
                IL_0046:  ldarg.0
                IL_0047:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int?> C.<F>d__0.<>u__1"
                IL_004c:  stloc.s    V_4
                IL_004e:  ldarg.0
                IL_004f:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int?> C.<F>d__0.<>u__1"
                IL_0054:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int?>"
                IL_005a:  ldarg.0
                IL_005b:  ldc.i4.m1
                IL_005c:  dup
                IL_005d:  stloc.0
                IL_005e:  stfld      "int C.<F>d__0.<>1__state"
                IL_0063:  ldloca.s   V_4
                IL_0065:  call       "int? System.Runtime.CompilerServices.TaskAwaiter<int?>.GetResult()"
                IL_006a:  stloc.3
                IL_006b:  br.s       IL_0075
                IL_006d:  ldloca.s   V_3
                IL_006f:  initobj    "int?"
                IL_0075:  ldloc.3
                IL_0076:  stloc.1
                IL_0077:  leave.s    IL_0092
              }
              catch System.Exception
              {
                IL_0079:  stloc.s    V_5
                IL_007b:  ldarg.0
                IL_007c:  ldc.i4.s   -2
                IL_007e:  stfld      "int C.<F>d__0.<>1__state"
                IL_0083:  ldarg.0
                IL_0084:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
                IL_0089:  ldloc.s    V_5
                IL_008b:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.SetException(System.Exception)"
                IL_0090:  leave.s    IL_00a6
              }
              IL_0092:  ldarg.0
              IL_0093:  ldc.i4.s   -2
              IL_0095:  stfld      "int C.<F>d__0.<>1__state"
              IL_009a:  ldarg.0
              IL_009b:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
              IL_00a0:  ldloc.1
              IL_00a1:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.SetResult(int?)"
              IL_00a6:  ret
            }
            """,
            runtimeAsyncIL: """
            {
              // Code size       24 (0x18)
              .maxstack  1
              .locals init (System.Threading.Tasks.Task<int?> V_0,
                            int? V_1)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloc.0
              IL_0003:  brfalse.s  IL_000e
              IL_0005:  ldloc.0
              IL_0006:  call       "int? System.Runtime.CompilerServices.AsyncHelpers.Await<int?>(System.Threading.Tasks.Task<int?>)"
              IL_000b:  stloc.1
              IL_000c:  br.s       IL_0016
              IL_000e:  ldloca.s   V_1
              IL_0010:  initobj    "int?"
              IL_0016:  ldloc.1
              IL_0017:  ret
            }
            """);
    }

    [Fact]
    public void Matrix_ReferenceReceiver_ReferenceR_UnchangedType()
    {
        // Receiver: Task<string> (reference, reference R=string). Result kept as string.
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task<string> F(Task<string> t) => await? t;
            }
            """;
        VerifyMatrixIL(source, stateMachineMoveNextIL: """
            {
              // Code size      161 (0xa1)
              .maxstack  3
              .locals init (int V_0,
                            string V_1,
                            System.Threading.Tasks.Task<string> V_2,
                            string V_3,
                            System.Runtime.CompilerServices.TaskAwaiter<string> V_4,
                            System.Exception V_5)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0046
                IL_000a:  ldarg.0
                IL_000b:  ldfld      "System.Threading.Tasks.Task<string> C.<F>d__0.t"
                IL_0010:  stloc.2
                IL_0011:  ldloc.2
                IL_0012:  brfalse.s  IL_006d
                IL_0014:  ldloc.2
                IL_0015:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<string> System.Threading.Tasks.Task<string>.GetAwaiter()"
                IL_001a:  stloc.s    V_4
                IL_001c:  ldloca.s   V_4
                IL_001e:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<string>.IsCompleted.get"
                IL_0023:  brtrue.s   IL_0063
                IL_0025:  ldarg.0
                IL_0026:  ldc.i4.0
                IL_0027:  dup
                IL_0028:  stloc.0
                IL_0029:  stfld      "int C.<F>d__0.<>1__state"
                IL_002e:  ldarg.0
                IL_002f:  ldloc.s    V_4
                IL_0031:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<string> C.<F>d__0.<>u__1"
                IL_0036:  ldarg.0
                IL_0037:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> C.<F>d__0.<>t__builder"
                IL_003c:  ldloca.s   V_4
                IL_003e:  ldarg.0
                IL_003f:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<string>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<string>, ref C.<F>d__0)"
                IL_0044:  leave.s    IL_00a0
                IL_0046:  ldarg.0
                IL_0047:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<string> C.<F>d__0.<>u__1"
                IL_004c:  stloc.s    V_4
                IL_004e:  ldarg.0
                IL_004f:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<string> C.<F>d__0.<>u__1"
                IL_0054:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<string>"
                IL_005a:  ldarg.0
                IL_005b:  ldc.i4.m1
                IL_005c:  dup
                IL_005d:  stloc.0
                IL_005e:  stfld      "int C.<F>d__0.<>1__state"
                IL_0063:  ldloca.s   V_4
                IL_0065:  call       "string System.Runtime.CompilerServices.TaskAwaiter<string>.GetResult()"
                IL_006a:  stloc.3
                IL_006b:  br.s       IL_006f
                IL_006d:  ldnull
                IL_006e:  stloc.3
                IL_006f:  ldloc.3
                IL_0070:  stloc.1
                IL_0071:  leave.s    IL_008c
              }
              catch System.Exception
              {
                IL_0073:  stloc.s    V_5
                IL_0075:  ldarg.0
                IL_0076:  ldc.i4.s   -2
                IL_0078:  stfld      "int C.<F>d__0.<>1__state"
                IL_007d:  ldarg.0
                IL_007e:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> C.<F>d__0.<>t__builder"
                IL_0083:  ldloc.s    V_5
                IL_0085:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetException(System.Exception)"
                IL_008a:  leave.s    IL_00a0
              }
              IL_008c:  ldarg.0
              IL_008d:  ldc.i4.s   -2
              IL_008f:  stfld      "int C.<F>d__0.<>1__state"
              IL_0094:  ldarg.0
              IL_0095:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> C.<F>d__0.<>t__builder"
              IL_009a:  ldloc.1
              IL_009b:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetResult(string)"
              IL_00a0:  ret
            }
            """,
            runtimeAsyncIL: """
            {
              // Code size       18 (0x12)
              .maxstack  1
              .locals init (System.Threading.Tasks.Task<string> V_0,
                            string V_1)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloc.0
              IL_0003:  brfalse.s  IL_000e
              IL_0005:  ldloc.0
              IL_0006:  call       "string System.Runtime.CompilerServices.AsyncHelpers.Await<string>(System.Threading.Tasks.Task<string>)"
              IL_000b:  stloc.1
              IL_000c:  br.s       IL_0010
              IL_000e:  ldnull
              IL_000f:  stloc.1
              IL_0010:  ldloc.1
              IL_0011:  ret
            }
            """);
    }

    // ==========================================================================================
    // B. Nullable<V> receiver: null-check via Nullable<V>.get_HasValue, value via GetValueOrDefault.
    // ==========================================================================================

    [Fact]
    public void Matrix_NullableReceiver_VoidR_NullableValueTask()
    {
        // Receiver: Nullable<ValueTask> (nullable value, void R). Statement-position body.
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task F(ValueTask? t) { await? t; }
            }
            """;
        VerifyMatrixIL(source, stateMachineMoveNextIL: """
            {
              // Code size      165 (0xa5)
              .maxstack  3
              .locals init (int V_0,
                            System.Threading.Tasks.ValueTask? V_1,
                            System.Runtime.CompilerServices.ValueTaskAwaiter V_2,
                            System.Threading.Tasks.ValueTask V_3,
                            System.Exception V_4)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0053
                IL_000a:  ldarg.0
                IL_000b:  ldfld      "System.Threading.Tasks.ValueTask? C.<F>d__0.t"
                IL_0010:  stloc.1
                IL_0011:  ldloca.s   V_1
                IL_0013:  call       "readonly bool System.Threading.Tasks.ValueTask?.HasValue.get"
                IL_0018:  brfalse.s  IL_0076
                IL_001a:  ldloca.s   V_1
                IL_001c:  call       "readonly System.Threading.Tasks.ValueTask System.Threading.Tasks.ValueTask?.GetValueOrDefault()"
                IL_0021:  stloc.3
                IL_0022:  ldloca.s   V_3
                IL_0024:  call       "System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()"
                IL_0029:  stloc.2
                IL_002a:  ldloca.s   V_2
                IL_002c:  call       "bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get"
                IL_0031:  brtrue.s   IL_006f
                IL_0033:  ldarg.0
                IL_0034:  ldc.i4.0
                IL_0035:  dup
                IL_0036:  stloc.0
                IL_0037:  stfld      "int C.<F>d__0.<>1__state"
                IL_003c:  ldarg.0
                IL_003d:  ldloc.2
                IL_003e:  stfld      "System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__1"
                IL_0043:  ldarg.0
                IL_0044:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
                IL_0049:  ldloca.s   V_2
                IL_004b:  ldarg.0
                IL_004c:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<F>d__0)"
                IL_0051:  leave.s    IL_00a4
                IL_0053:  ldarg.0
                IL_0054:  ldfld      "System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__1"
                IL_0059:  stloc.2
                IL_005a:  ldarg.0
                IL_005b:  ldflda     "System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__1"
                IL_0060:  initobj    "System.Runtime.CompilerServices.ValueTaskAwaiter"
                IL_0066:  ldarg.0
                IL_0067:  ldc.i4.m1
                IL_0068:  dup
                IL_0069:  stloc.0
                IL_006a:  stfld      "int C.<F>d__0.<>1__state"
                IL_006f:  ldloca.s   V_2
                IL_0071:  call       "void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()"
                IL_0076:  leave.s    IL_0091
              }
              catch System.Exception
              {
                IL_0078:  stloc.s    V_4
                IL_007a:  ldarg.0
                IL_007b:  ldc.i4.s   -2
                IL_007d:  stfld      "int C.<F>d__0.<>1__state"
                IL_0082:  ldarg.0
                IL_0083:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
                IL_0088:  ldloc.s    V_4
                IL_008a:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_008f:  leave.s    IL_00a4
              }
              IL_0091:  ldarg.0
              IL_0092:  ldc.i4.s   -2
              IL_0094:  stfld      "int C.<F>d__0.<>1__state"
              IL_0099:  ldarg.0
              IL_009a:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
              IL_009f:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_00a4:  ret
            }
            """,
            runtimeAsyncIL: """
            {
              // Code size       24 (0x18)
              .maxstack  1
              .locals init (System.Threading.Tasks.ValueTask? V_0)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "readonly bool System.Threading.Tasks.ValueTask?.HasValue.get"
              IL_0009:  brfalse.s  IL_0017
              IL_000b:  ldloca.s   V_0
              IL_000d:  call       "readonly System.Threading.Tasks.ValueTask System.Threading.Tasks.ValueTask?.GetValueOrDefault()"
              IL_0012:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
              IL_0017:  ret
            }
            """);
    }

    [Fact]
    public void Matrix_NullableReceiver_ValueR_LiftedToNullable()
    {
        // Receiver: Nullable<ValueTask<int>> (nullable value, non-null value R=int). Lifted to int?.
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task<int?> F(ValueTask<int>? t) => await? t;
            }
            """;
        VerifyMatrixIL(source, stateMachineMoveNextIL: """
            {
              // Code size      192 (0xc0)
              .maxstack  3
              .locals init (int V_0,
                            int? V_1,
                            System.Threading.Tasks.ValueTask<int>? V_2,
                            int? V_3,
                            int V_4,
                            System.Runtime.CompilerServices.ValueTaskAwaiter<int> V_5,
                            System.Threading.Tasks.ValueTask<int> V_6,
                            System.Exception V_7)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0056
                IL_000a:  ldarg.0
                IL_000b:  ldfld      "System.Threading.Tasks.ValueTask<int>? C.<F>d__0.t"
                IL_0010:  stloc.2
                IL_0011:  ldloca.s   V_2
                IL_0013:  call       "readonly bool System.Threading.Tasks.ValueTask<int>?.HasValue.get"
                IL_0018:  brfalse.s  IL_0086
                IL_001a:  ldloca.s   V_2
                IL_001c:  call       "readonly System.Threading.Tasks.ValueTask<int> System.Threading.Tasks.ValueTask<int>?.GetValueOrDefault()"
                IL_0021:  stloc.s    V_6
                IL_0023:  ldloca.s   V_6
                IL_0025:  call       "System.Runtime.CompilerServices.ValueTaskAwaiter<int> System.Threading.Tasks.ValueTask<int>.GetAwaiter()"
                IL_002a:  stloc.s    V_5
                IL_002c:  ldloca.s   V_5
                IL_002e:  call       "bool System.Runtime.CompilerServices.ValueTaskAwaiter<int>.IsCompleted.get"
                IL_0033:  brtrue.s   IL_0073
                IL_0035:  ldarg.0
                IL_0036:  ldc.i4.0
                IL_0037:  dup
                IL_0038:  stloc.0
                IL_0039:  stfld      "int C.<F>d__0.<>1__state"
                IL_003e:  ldarg.0
                IL_003f:  ldloc.s    V_5
                IL_0041:  stfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_0046:  ldarg.0
                IL_0047:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
                IL_004c:  ldloca.s   V_5
                IL_004e:  ldarg.0
                IL_004f:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter<int>, ref C.<F>d__0)"
                IL_0054:  leave.s    IL_00bf
                IL_0056:  ldarg.0
                IL_0057:  ldfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_005c:  stloc.s    V_5
                IL_005e:  ldarg.0
                IL_005f:  ldflda     "System.Runtime.CompilerServices.ValueTaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_0064:  initobj    "System.Runtime.CompilerServices.ValueTaskAwaiter<int>"
                IL_006a:  ldarg.0
                IL_006b:  ldc.i4.m1
                IL_006c:  dup
                IL_006d:  stloc.0
                IL_006e:  stfld      "int C.<F>d__0.<>1__state"
                IL_0073:  ldloca.s   V_5
                IL_0075:  call       "int System.Runtime.CompilerServices.ValueTaskAwaiter<int>.GetResult()"
                IL_007a:  stloc.s    V_4
                IL_007c:  ldloc.s    V_4
                IL_007e:  newobj     "int?..ctor(int)"
                IL_0083:  stloc.3
                IL_0084:  br.s       IL_008e
                IL_0086:  ldloca.s   V_3
                IL_0088:  initobj    "int?"
                IL_008e:  ldloc.3
                IL_008f:  stloc.1
                IL_0090:  leave.s    IL_00ab
              }
              catch System.Exception
              {
                IL_0092:  stloc.s    V_7
                IL_0094:  ldarg.0
                IL_0095:  ldc.i4.s   -2
                IL_0097:  stfld      "int C.<F>d__0.<>1__state"
                IL_009c:  ldarg.0
                IL_009d:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
                IL_00a2:  ldloc.s    V_7
                IL_00a4:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.SetException(System.Exception)"
                IL_00a9:  leave.s    IL_00bf
              }
              IL_00ab:  ldarg.0
              IL_00ac:  ldc.i4.s   -2
              IL_00ae:  stfld      "int C.<F>d__0.<>1__state"
              IL_00b3:  ldarg.0
              IL_00b4:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
              IL_00b9:  ldloc.1
              IL_00ba:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.SetResult(int?)"
              IL_00bf:  ret
            }
            """,
            runtimeAsyncIL: """
            {
              // Code size       44 (0x2c)
              .maxstack  2
              .locals init (System.Threading.Tasks.ValueTask<int>? V_0,
                            int? V_1,
                            int V_2)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "readonly bool System.Threading.Tasks.ValueTask<int>?.HasValue.get"
              IL_0009:  brfalse.s  IL_0022
              IL_000b:  ldloca.s   V_0
              IL_000d:  call       "readonly System.Threading.Tasks.ValueTask<int> System.Threading.Tasks.ValueTask<int>?.GetValueOrDefault()"
              IL_0012:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.ValueTask<int>)"
              IL_0017:  stloc.2
              IL_0018:  ldloca.s   V_1
              IL_001a:  ldloc.2
              IL_001b:  call       "int?..ctor(int)"
              IL_0020:  br.s       IL_002a
              IL_0022:  ldloca.s   V_1
              IL_0024:  initobj    "int?"
              IL_002a:  ldloc.1
              IL_002b:  ret
            }
            """);
    }

    [Fact]
    public void Matrix_NullableReceiver_NullableR_NotDoubleWrapped()
    {
        // Receiver: Nullable<ValueTask<int?>> (nullable value, already-nullable R=int?).
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task<int?> F(ValueTask<int?>? t) => await? t;
            }
            """;
        VerifyMatrixIL(source, stateMachineMoveNextIL: """
            {
              // Code size      183 (0xb7)
              .maxstack  3
              .locals init (int V_0,
                            int? V_1,
                            System.Threading.Tasks.ValueTask<int?>? V_2,
                            int? V_3,
                            System.Runtime.CompilerServices.ValueTaskAwaiter<int?> V_4,
                            System.Threading.Tasks.ValueTask<int?> V_5,
                            System.Exception V_6)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0056
                IL_000a:  ldarg.0
                IL_000b:  ldfld      "System.Threading.Tasks.ValueTask<int?>? C.<F>d__0.t"
                IL_0010:  stloc.2
                IL_0011:  ldloca.s   V_2
                IL_0013:  call       "readonly bool System.Threading.Tasks.ValueTask<int?>?.HasValue.get"
                IL_0018:  brfalse.s  IL_007d
                IL_001a:  ldloca.s   V_2
                IL_001c:  call       "readonly System.Threading.Tasks.ValueTask<int?> System.Threading.Tasks.ValueTask<int?>?.GetValueOrDefault()"
                IL_0021:  stloc.s    V_5
                IL_0023:  ldloca.s   V_5
                IL_0025:  call       "System.Runtime.CompilerServices.ValueTaskAwaiter<int?> System.Threading.Tasks.ValueTask<int?>.GetAwaiter()"
                IL_002a:  stloc.s    V_4
                IL_002c:  ldloca.s   V_4
                IL_002e:  call       "bool System.Runtime.CompilerServices.ValueTaskAwaiter<int?>.IsCompleted.get"
                IL_0033:  brtrue.s   IL_0073
                IL_0035:  ldarg.0
                IL_0036:  ldc.i4.0
                IL_0037:  dup
                IL_0038:  stloc.0
                IL_0039:  stfld      "int C.<F>d__0.<>1__state"
                IL_003e:  ldarg.0
                IL_003f:  ldloc.s    V_4
                IL_0041:  stfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<int?> C.<F>d__0.<>u__1"
                IL_0046:  ldarg.0
                IL_0047:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
                IL_004c:  ldloca.s   V_4
                IL_004e:  ldarg.0
                IL_004f:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter<int?>, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter<int?>, ref C.<F>d__0)"
                IL_0054:  leave.s    IL_00b6
                IL_0056:  ldarg.0
                IL_0057:  ldfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<int?> C.<F>d__0.<>u__1"
                IL_005c:  stloc.s    V_4
                IL_005e:  ldarg.0
                IL_005f:  ldflda     "System.Runtime.CompilerServices.ValueTaskAwaiter<int?> C.<F>d__0.<>u__1"
                IL_0064:  initobj    "System.Runtime.CompilerServices.ValueTaskAwaiter<int?>"
                IL_006a:  ldarg.0
                IL_006b:  ldc.i4.m1
                IL_006c:  dup
                IL_006d:  stloc.0
                IL_006e:  stfld      "int C.<F>d__0.<>1__state"
                IL_0073:  ldloca.s   V_4
                IL_0075:  call       "int? System.Runtime.CompilerServices.ValueTaskAwaiter<int?>.GetResult()"
                IL_007a:  stloc.3
                IL_007b:  br.s       IL_0085
                IL_007d:  ldloca.s   V_3
                IL_007f:  initobj    "int?"
                IL_0085:  ldloc.3
                IL_0086:  stloc.1
                IL_0087:  leave.s    IL_00a2
              }
              catch System.Exception
              {
                IL_0089:  stloc.s    V_6
                IL_008b:  ldarg.0
                IL_008c:  ldc.i4.s   -2
                IL_008e:  stfld      "int C.<F>d__0.<>1__state"
                IL_0093:  ldarg.0
                IL_0094:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
                IL_0099:  ldloc.s    V_6
                IL_009b:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.SetException(System.Exception)"
                IL_00a0:  leave.s    IL_00b6
              }
              IL_00a2:  ldarg.0
              IL_00a3:  ldc.i4.s   -2
              IL_00a5:  stfld      "int C.<F>d__0.<>1__state"
              IL_00aa:  ldarg.0
              IL_00ab:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?> C.<F>d__0.<>t__builder"
              IL_00b0:  ldloc.1
              IL_00b1:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int?>.SetResult(int?)"
              IL_00b6:  ret
            }
            """,
            runtimeAsyncIL: """
            {
              // Code size       36 (0x24)
              .maxstack  1
              .locals init (System.Threading.Tasks.ValueTask<int?>? V_0,
                            int? V_1)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "readonly bool System.Threading.Tasks.ValueTask<int?>?.HasValue.get"
              IL_0009:  brfalse.s  IL_001a
              IL_000b:  ldloca.s   V_0
              IL_000d:  call       "readonly System.Threading.Tasks.ValueTask<int?> System.Threading.Tasks.ValueTask<int?>?.GetValueOrDefault()"
              IL_0012:  call       "int? System.Runtime.CompilerServices.AsyncHelpers.Await<int?>(System.Threading.Tasks.ValueTask<int?>)"
              IL_0017:  stloc.1
              IL_0018:  br.s       IL_0022
              IL_001a:  ldloca.s   V_1
              IL_001c:  initobj    "int?"
              IL_0022:  ldloc.1
              IL_0023:  ret
            }
            """);
    }

    [Fact]
    public void Matrix_NullableReceiver_ReferenceR_UnchangedType()
    {
        // Receiver: Nullable<ValueTask<string>> (nullable value, reference R=string).
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task<string> F(ValueTask<string>? t) => await? t;
            }
            """;
        VerifyMatrixIL(source, stateMachineMoveNextIL: """
            {
              // Code size      177 (0xb1)
              .maxstack  3
              .locals init (int V_0,
                            string V_1,
                            System.Threading.Tasks.ValueTask<string>? V_2,
                            string V_3,
                            System.Runtime.CompilerServices.ValueTaskAwaiter<string> V_4,
                            System.Threading.Tasks.ValueTask<string> V_5,
                            System.Exception V_6)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0056
                IL_000a:  ldarg.0
                IL_000b:  ldfld      "System.Threading.Tasks.ValueTask<string>? C.<F>d__0.t"
                IL_0010:  stloc.2
                IL_0011:  ldloca.s   V_2
                IL_0013:  call       "readonly bool System.Threading.Tasks.ValueTask<string>?.HasValue.get"
                IL_0018:  brfalse.s  IL_007d
                IL_001a:  ldloca.s   V_2
                IL_001c:  call       "readonly System.Threading.Tasks.ValueTask<string> System.Threading.Tasks.ValueTask<string>?.GetValueOrDefault()"
                IL_0021:  stloc.s    V_5
                IL_0023:  ldloca.s   V_5
                IL_0025:  call       "System.Runtime.CompilerServices.ValueTaskAwaiter<string> System.Threading.Tasks.ValueTask<string>.GetAwaiter()"
                IL_002a:  stloc.s    V_4
                IL_002c:  ldloca.s   V_4
                IL_002e:  call       "bool System.Runtime.CompilerServices.ValueTaskAwaiter<string>.IsCompleted.get"
                IL_0033:  brtrue.s   IL_0073
                IL_0035:  ldarg.0
                IL_0036:  ldc.i4.0
                IL_0037:  dup
                IL_0038:  stloc.0
                IL_0039:  stfld      "int C.<F>d__0.<>1__state"
                IL_003e:  ldarg.0
                IL_003f:  ldloc.s    V_4
                IL_0041:  stfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<string> C.<F>d__0.<>u__1"
                IL_0046:  ldarg.0
                IL_0047:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> C.<F>d__0.<>t__builder"
                IL_004c:  ldloca.s   V_4
                IL_004e:  ldarg.0
                IL_004f:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter<string>, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter<string>, ref C.<F>d__0)"
                IL_0054:  leave.s    IL_00b0
                IL_0056:  ldarg.0
                IL_0057:  ldfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<string> C.<F>d__0.<>u__1"
                IL_005c:  stloc.s    V_4
                IL_005e:  ldarg.0
                IL_005f:  ldflda     "System.Runtime.CompilerServices.ValueTaskAwaiter<string> C.<F>d__0.<>u__1"
                IL_0064:  initobj    "System.Runtime.CompilerServices.ValueTaskAwaiter<string>"
                IL_006a:  ldarg.0
                IL_006b:  ldc.i4.m1
                IL_006c:  dup
                IL_006d:  stloc.0
                IL_006e:  stfld      "int C.<F>d__0.<>1__state"
                IL_0073:  ldloca.s   V_4
                IL_0075:  call       "string System.Runtime.CompilerServices.ValueTaskAwaiter<string>.GetResult()"
                IL_007a:  stloc.3
                IL_007b:  br.s       IL_007f
                IL_007d:  ldnull
                IL_007e:  stloc.3
                IL_007f:  ldloc.3
                IL_0080:  stloc.1
                IL_0081:  leave.s    IL_009c
              }
              catch System.Exception
              {
                IL_0083:  stloc.s    V_6
                IL_0085:  ldarg.0
                IL_0086:  ldc.i4.s   -2
                IL_0088:  stfld      "int C.<F>d__0.<>1__state"
                IL_008d:  ldarg.0
                IL_008e:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> C.<F>d__0.<>t__builder"
                IL_0093:  ldloc.s    V_6
                IL_0095:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetException(System.Exception)"
                IL_009a:  leave.s    IL_00b0
              }
              IL_009c:  ldarg.0
              IL_009d:  ldc.i4.s   -2
              IL_009f:  stfld      "int C.<F>d__0.<>1__state"
              IL_00a4:  ldarg.0
              IL_00a5:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> C.<F>d__0.<>t__builder"
              IL_00aa:  ldloc.1
              IL_00ab:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetResult(string)"
              IL_00b0:  ret
            }
            """,
            runtimeAsyncIL: """
            {
              // Code size       30 (0x1e)
              .maxstack  1
              .locals init (System.Threading.Tasks.ValueTask<string>? V_0,
                            string V_1)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  call       "readonly bool System.Threading.Tasks.ValueTask<string>?.HasValue.get"
              IL_0009:  brfalse.s  IL_001a
              IL_000b:  ldloca.s   V_0
              IL_000d:  call       "readonly System.Threading.Tasks.ValueTask<string> System.Threading.Tasks.ValueTask<string>?.GetValueOrDefault()"
              IL_0012:  call       "string System.Runtime.CompilerServices.AsyncHelpers.Await<string>(System.Threading.Tasks.ValueTask<string>)"
              IL_0017:  stloc.1
              IL_0018:  br.s       IL_001c
              IL_001a:  ldnull
              IL_001b:  stloc.1
              IL_001c:  ldloc.1
              IL_001d:  ret
            }
            """);
    }

    // ==========================================================================================
    // Spilling: `await?` inside a complex expression forces the spiller to emit the null-check/
    // await as statement-level operations interleaved with surrounding spill temps.
    // ==========================================================================================

    [Fact]
    public void Matrix_Spill_ArgumentPosition()
    {
        // `a + await? t` — `a` must be spilled across the null-check-and-await. This pins
        // the spill shape that surfaces in both modes: the integer `a` is routed through a
        // temp (into the state-machine frame, or into a stack spill for runtime-async) and
        // re-read after the null-conditional await short-circuit.
        var source = """
            using System.Threading.Tasks;

            public class C
            {
                public static async Task<int> F(int a, Task<int> t) => a + (await? t).GetValueOrDefault();
            }
            """;
        VerifyMatrixIL(source, stateMachineMoveNextIL: """
            {
              // Code size      201 (0xc9)
              .maxstack  3
              .locals init (int V_0,
                            int V_1,
                            System.Threading.Tasks.Task<int> V_2,
                            int? V_3,
                            int V_4,
                            System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
                            System.Exception V_6)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0052
                IL_000a:  ldarg.0
                IL_000b:  ldarg.0
                IL_000c:  ldfld      "int C.<F>d__0.a"
                IL_0011:  stfld      "int C.<F>d__0.<>7__wrap1"
                IL_0016:  ldarg.0
                IL_0017:  ldfld      "System.Threading.Tasks.Task<int> C.<F>d__0.t"
                IL_001c:  stloc.2
                IL_001d:  ldloc.2
                IL_001e:  brfalse.s  IL_0082
                IL_0020:  ldloc.2
                IL_0021:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                IL_0026:  stloc.s    V_5
                IL_0028:  ldloca.s   V_5
                IL_002a:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                IL_002f:  brtrue.s   IL_006f
                IL_0031:  ldarg.0
                IL_0032:  ldc.i4.0
                IL_0033:  dup
                IL_0034:  stloc.0
                IL_0035:  stfld      "int C.<F>d__0.<>1__state"
                IL_003a:  ldarg.0
                IL_003b:  ldloc.s    V_5
                IL_003d:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_0042:  ldarg.0
                IL_0043:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder"
                IL_0048:  ldloca.s   V_5
                IL_004a:  ldarg.0
                IL_004b:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)"
                IL_0050:  leave.s    IL_00c8
                IL_0052:  ldarg.0
                IL_0053:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_0058:  stloc.s    V_5
                IL_005a:  ldarg.0
                IL_005b:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_0060:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                IL_0066:  ldarg.0
                IL_0067:  ldc.i4.m1
                IL_0068:  dup
                IL_0069:  stloc.0
                IL_006a:  stfld      "int C.<F>d__0.<>1__state"
                IL_006f:  ldloca.s   V_5
                IL_0071:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                IL_0076:  stloc.s    V_4
                IL_0078:  ldloc.s    V_4
                IL_007a:  newobj     "int?..ctor(int)"
                IL_007f:  stloc.3
                IL_0080:  br.s       IL_008a
                IL_0082:  ldloca.s   V_3
                IL_0084:  initobj    "int?"
                IL_008a:  ldarg.0
                IL_008b:  ldfld      "int C.<F>d__0.<>7__wrap1"
                IL_0090:  ldloca.s   V_3
                IL_0092:  call       "readonly int int?.GetValueOrDefault()"
                IL_0097:  add
                IL_0098:  stloc.1
                IL_0099:  leave.s    IL_00b4
              }
              catch System.Exception
              {
                IL_009b:  stloc.s    V_6
                IL_009d:  ldarg.0
                IL_009e:  ldc.i4.s   -2
                IL_00a0:  stfld      "int C.<F>d__0.<>1__state"
                IL_00a5:  ldarg.0
                IL_00a6:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder"
                IL_00ab:  ldloc.s    V_6
                IL_00ad:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)"
                IL_00b2:  leave.s    IL_00c8
              }
              IL_00b4:  ldarg.0
              IL_00b5:  ldc.i4.s   -2
              IL_00b7:  stfld      "int C.<F>d__0.<>1__state"
              IL_00bc:  ldarg.0
              IL_00bd:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder"
              IL_00c2:  ldloc.1
              IL_00c3:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)"
              IL_00c8:  ret
            }
            """,
            runtimeAsyncIL: """
            {
              // Code size       40 (0x28)
              .maxstack  3
              .locals init (System.Threading.Tasks.Task<int> V_0,
                            int? V_1,
                            int V_2)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  stloc.0
              IL_0003:  ldloc.0
              IL_0004:  brfalse.s  IL_0017
              IL_0006:  ldloc.0
              IL_0007:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
              IL_000c:  stloc.2
              IL_000d:  ldloca.s   V_1
              IL_000f:  ldloc.2
              IL_0010:  call       "int?..ctor(int)"
              IL_0015:  br.s       IL_001f
              IL_0017:  ldloca.s   V_1
              IL_0019:  initobj    "int?"
              IL_001f:  ldloca.s   V_1
              IL_0021:  call       "readonly int int?.GetValueOrDefault()"
              IL_0026:  add
              IL_0027:  ret
            }
            """);
    }
}
