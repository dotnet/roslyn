// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class StackOverflowProbingTests : CSharpTestBase
{
    private static readonly EmitOptions s_emitOptions = EmitOptions.Default.WithInstrumentationKinds([InstrumentationKind.StackOverflowProbing]);

    private CompilationVerifier CompileAndVerify(string source, string? expectedOutput = null, CSharpCompilationOptions? options = null, Verification? verification = null)
        => CompileAndVerify(
            source,
            options: options ?? (expectedOutput != null ? TestOptions.UnsafeDebugExe : TestOptions.UnsafeDebugDll),
            emitOptions: s_emitOptions,
            verify: verification ?? Verification.Passes,
            targetFramework: TargetFramework.NetLatest,
            expectedOutput: expectedOutput);

    private static void AssertNotInstrumented(CompilationVerifier verifier, string qualifiedMethodName)
    {
        var il = verifier.VisualizeIL(qualifiedMethodName);
        var isInstrumented = il.Contains("System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack");

        Assert.False(isInstrumented,
            $"Method '{qualifiedMethodName}' should not be instrumented. Actual IL:{Environment.NewLine}{il}");
    }

    [Fact]
    public void LambdaAndLocalFunction()
    {
        var source = """
            using System;

            class C
            {
                public void F(Func<int> a)
                {
                    void L()
                    {
                        L();
                    }

                    F(() => 1);
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", $$"""
            {
              // Code size       47 (0x2f)
              .maxstack  3
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: {
              IL_0006:  nop
              IL_0007:  nop
              // sequence point: F(() => 1);
              IL_0008:  ldarg.0
              IL_0009:  ldsfld     "System.Func<int> C.<>c.<>9__0_1"
              IL_000e:  dup
              IL_000f:  brtrue.s   IL_0028
              IL_0011:  pop
              IL_0012:  ldsfld     "C.<>c C.<>c.<>9"
              IL_0017:  ldftn      "int C.<>c.<F>b__0_1()"
              IL_001d:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
              IL_0022:  dup
              IL_0023:  stsfld     "System.Func<int> C.<>c.<>9__0_1"
              IL_0028:  call       "void C.F(System.Func<int>)"
              IL_002d:  nop
              // sequence point: }
              IL_002e:  ret
            }
            """);

        verifier.VerifyMethodBody("C.<F>g__L|0_0", """
            {
              // Code size       14 (0xe)
              .maxstack  0
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: {
              IL_0006:  nop
              // sequence point: L();
              IL_0007:  call       "void C.<F>g__L|0_0()"
              IL_000c:  nop
              // sequence point: }
              IL_000d:  ret
            }
            """);

        verifier.VerifyMethodBody("C.<>c.<F>b__0_1", """
            {
              // Code size        8 (0x8)
              .maxstack  1
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: 1
              IL_0006:  ldc.i4.1
              IL_0007:  ret
            }
            """);
    }

    [Fact]
    public void LambdaAndLocalFunction_TopLevel()
    {
        var source = """
            F();
            
            void F()
            {
                F();
            }
            """;

        var verifier = CompileAndVerify(source, options: TestOptions.DebugExe);

        verifier.VerifyMethodBody("<top-level-statements-entry-point>", """
            {
              // Code size       14 (0xe)
              .maxstack  0
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: F();
              IL_0006:  call       "void Program.<<Main>$>g__F|0_0()"
              IL_000b:  nop
              IL_000c:  nop
              IL_000d:  ret
            }
            """);

        verifier.VerifyMethodBody("Program.<<Main>$>g__F|0_0", """
            {
              // Code size       14 (0xe)
              .maxstack  0
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: {
              IL_0006:  nop
              // sequence point: F();
              IL_0007:  call       "void Program.<<Main>$>g__F|0_0()"
              IL_000c:  nop
              // sequence point: }
              IL_000d:  ret
            }
            """);
    }

    [Fact]
    public void StaticConstructor_Explicit()
    {
        var source = """
            using System;

            class C
            {
                static Func<int> s_f = () => s_f();

                static C()
                {
                    Console.WriteLine(123);
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        // no probe since static constructor can only be invoked once:
        AssertNotInstrumented(verifier, "C..cctor");

        // lambda should be instrumented
        verifier.VerifyMethodBody("C.<>c.<.cctor>b__1_0", """
            {
              // Code size       17 (0x11)
              .maxstack  1
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: s_f()
              IL_0006:  ldsfld     "System.Func<int> C.s_f"
              IL_000b:  callvirt   "int System.Func<int>.Invoke()"
              IL_0010:  ret
            }
            """);
    }

    [Fact]
    public void StaticConstructor_Implicit()
    {
        var source = """
            using System;

            class C
            {
                static Func<int> s_f = () => s_f();
            }
            """;

        var verifier = CompileAndVerify(source);

        // no probe since static constructor can only be invoked once:
        AssertNotInstrumented(verifier, "C..cctor");

        // lambda should be instrumented
        verifier.VerifyMethodBody("C.<>c.<.cctor>b__2_0", """
            {
              // Code size       17 (0x11)
              .maxstack  1
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: s_f()
              IL_0006:  ldsfld     "System.Func<int> C.s_f"
              IL_000b:  callvirt   "int System.Func<int>.Invoke()"
              IL_0010:  ret
            }
            """);
    }

    [Fact]
    public void InstanceConstructor_Implicit()
    {
        var source = """
            class B
            {
                C C = new();
            }

            class C : B
            {
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("B..ctor", """
            {
              // Code size       25 (0x19)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: C C = new();
              IL_0006:  ldarg.0
              IL_0007:  newobj     "C..ctor()"
              IL_000c:  stfld      "C B.C"
              IL_0011:  ldarg.0
              IL_0012:  call       "object..ctor()"
              IL_0017:  nop
              IL_0018:  ret
            }
            """);

        verifier.VerifyMethodBody("C..ctor", """
            {
              // Code size       14 (0xe)
              .maxstack  1
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              IL_0006:  ldarg.0
              IL_0007:  call       "B..ctor()"
              IL_000c:  nop
              IL_000d:  ret
            }
            """);
    }

    [Fact]
    public void InstanceConstructors_Explicit()
    {
        var source = """
            class B(int a)
            {
            }

            class C(int a) : B(a)
            {
                C c = new C(1);
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("B..ctor", """
            {
              // Code size       14 (0xe)
              .maxstack  1
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: B(int a)
              IL_0006:  ldarg.0
              IL_0007:  call       "object..ctor()"
              IL_000c:  nop
              IL_000d:  ret
            }
            """);

        verifier.VerifyMethodBody("C..ctor", """
            {
              // Code size       27 (0x1b)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: C c = new C(1);
              IL_0006:  ldarg.0
              IL_0007:  ldc.i4.1
              IL_0008:  newobj     "C..ctor(int)"
              IL_000d:  stfld      "C C.c"
              // sequence point: B(a)
              IL_0012:  ldarg.0
              IL_0013:  ldarg.1
              IL_0014:  call       "B..ctor(int)"
              IL_0019:  nop
              IL_001a:  ret
            }
            """);
    }

    [Fact]
    public void PropertyAccessors_Auto()
    {
        var source = """
            class C
            {
                int P { get; set; }
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumented(verifier, "C.P.get");
        AssertNotInstrumented(verifier, "C.P.set");
    }

    [Fact]
    public void PropertyAccessors_Explicit()
    {
        var source = """
            class C
            {
                int P { get { return P; } set { P = value; } }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.P.get", """
            {
              // Code size       18 (0x12)
              .maxstack  1
              .locals init (int V_0)
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: {
              IL_0006:  nop
              // sequence point: return P;
              IL_0007:  ldarg.0
              IL_0008:  call       "int C.P.get"
              IL_000d:  stloc.0
              IL_000e:  br.s       IL_0010
              // sequence point: }
              IL_0010:  ldloc.0
              IL_0011:  ret
            }
            """);

        verifier.VerifyMethodBody("C.P.set", """
            {
              // Code size       16 (0x10)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: {
              IL_0006:  nop
              // sequence point: P = value;
              IL_0007:  ldarg.0
              IL_0008:  ldarg.1
              IL_0009:  call       "void C.P.set"
              IL_000e:  nop
              // sequence point: }
              IL_000f:  ret
            }
            """);
    }

    [Fact]
    public void EventAccessors_Field()
    {
        var source = """
            using System;
            class C
            {
                event Action E;
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumented(verifier, "C.E.add");
        AssertNotInstrumented(verifier, "C.E.remove");
    }

    [Fact]
    public void EventAccessors_Explicit()
    {
        var source = """
            using System;
            class C
            {
                event Action E
                {
                    add { }
                    remove { }
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.E.add", """
            {
              // Code size        8 (0x8)
              .maxstack  0
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: {
              IL_0006:  nop
              // sequence point: }
              IL_0007:  ret
            }
            """);

        verifier.VerifyMethodBody("C.E.remove", """
             {
              // Code size        8 (0x8)
              .maxstack  0
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: {
              IL_0006:  nop
              // sequence point: }
              IL_0007:  ret
            }
            """);
    }

    [Fact]
    public void StateMachine_Iterator()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                public IEnumerable<int> F()
                {
                    yield return 1;
                    yield return 2;
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumented(verifier, "C.F");
        AssertNotInstrumented(verifier, "C.<F>d__0..ctor");
        AssertNotInstrumented(verifier, "C.<F>d__0.System.IDisposable.Dispose");
        AssertNotInstrumented(verifier, "C.<F>d__0.System.Collections.IEnumerator.get_Current");
        AssertNotInstrumented(verifier, "C.<F>d__0.System.Collections.IEnumerator.Reset");
        AssertNotInstrumented(verifier, "C.<F>d__0.System.Collections.IEnumerable.GetEnumerator");
        AssertNotInstrumented(verifier, "C.<F>d__0.System.Collections.Generic.IEnumerator<int>.get_Current");
        AssertNotInstrumented(verifier, "C.<F>d__0.System.Collections.Generic.IEnumerable<int>.GetEnumerator");

        verifier.VerifyMethodBody("C.<F>d__0.System.Collections.IEnumerator.MoveNext", """
            {
              // Code size       97 (0x61)
              .maxstack  2
              .locals init (int V_0)
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: <hidden>
              IL_0006:  ldarg.0
              IL_0007:  ldfld      "int C.<F>d__0.<>1__state"
              IL_000c:  stloc.0
              IL_000d:  ldloc.0
              IL_000e:  switch    (
                    IL_0021,
                    IL_0023,
                    IL_0025)
              IL_001f:  br.s       IL_0027
              IL_0021:  br.s       IL_0029
              IL_0023:  br.s       IL_0041
              IL_0025:  br.s       IL_0058
              IL_0027:  ldc.i4.0
              IL_0028:  ret
              IL_0029:  ldarg.0
              IL_002a:  ldc.i4.m1
              IL_002b:  stfld      "int C.<F>d__0.<>1__state"
              // sequence point: {
              IL_0030:  nop
              // sequence point: yield return 1;
              IL_0031:  ldarg.0
              IL_0032:  ldc.i4.1
              IL_0033:  stfld      "int C.<F>d__0.<>2__current"
              IL_0038:  ldarg.0
              IL_0039:  ldc.i4.1
              IL_003a:  stfld      "int C.<F>d__0.<>1__state"
              IL_003f:  ldc.i4.1
              IL_0040:  ret
              // sequence point: <hidden>
              IL_0041:  ldarg.0
              IL_0042:  ldc.i4.m1
              IL_0043:  stfld      "int C.<F>d__0.<>1__state"
              // sequence point: yield return 2;
              IL_0048:  ldarg.0
              IL_0049:  ldc.i4.2
              IL_004a:  stfld      "int C.<F>d__0.<>2__current"
              IL_004f:  ldarg.0
              IL_0050:  ldc.i4.2
              IL_0051:  stfld      "int C.<F>d__0.<>1__state"
              IL_0056:  ldc.i4.1
              IL_0057:  ret
              // sequence point: <hidden>
              IL_0058:  ldarg.0
              IL_0059:  ldc.i4.m1
              IL_005a:  stfld      "int C.<F>d__0.<>1__state"
              // sequence point: }
              IL_005f:  ldc.i4.0
              IL_0060:  ret
            }
            """);
    }

    [Fact]
    public void StateMachine_Async()
    {
        var source = """
            using System.Threading.Tasks;
            
            class C
            {
                static async Task F()
                {
                    await F();
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumented(verifier, "C.F");
        AssertNotInstrumented(verifier, "C.<F>d__0..ctor");
        AssertNotInstrumented(verifier, "C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine");

        verifier.VerifyMethodBody("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      160 (0xa0)
              .maxstack  3
              .locals init (int V_0,
                            System.Runtime.CompilerServices.TaskAwaiter V_1,
                            C.<F>d__0 V_2,
                            System.Exception V_3)
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: <hidden>
              IL_0006:  ldarg.0
              IL_0007:  ldfld      "int C.<F>d__0.<>1__state"
              IL_000c:  stloc.0
              .try
              {
                // sequence point: <hidden>
                IL_000d:  ldloc.0
                IL_000e:  brfalse.s  IL_0012
                IL_0010:  br.s       IL_0014
                IL_0012:  br.s       IL_004d
                // sequence point: {
                IL_0014:  nop
                // sequence point: await F();
                IL_0015:  call       "System.Threading.Tasks.Task C.F()"
                IL_001a:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
                IL_001f:  stloc.1
                // sequence point: <hidden>
                IL_0020:  ldloca.s   V_1
                IL_0022:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
                IL_0027:  brtrue.s   IL_0069
                IL_0029:  ldarg.0
                IL_002a:  ldc.i4.0
                IL_002b:  dup
                IL_002c:  stloc.0
                IL_002d:  stfld      "int C.<F>d__0.<>1__state"
                // async: yield
                IL_0032:  ldarg.0
                IL_0033:  ldloc.1
                IL_0034:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1"
                IL_0039:  ldarg.0
                IL_003a:  stloc.2
                IL_003b:  ldarg.0
                IL_003c:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
                IL_0041:  ldloca.s   V_1
                IL_0043:  ldloca.s   V_2
                IL_0045:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__0)"
                IL_004a:  nop
                IL_004b:  leave.s    IL_009f
                // async: resume
                IL_004d:  ldarg.0
                IL_004e:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1"
                IL_0053:  stloc.1
                IL_0054:  ldarg.0
                IL_0055:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1"
                IL_005a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
                IL_0060:  ldarg.0
                IL_0061:  ldc.i4.m1
                IL_0062:  dup
                IL_0063:  stloc.0
                IL_0064:  stfld      "int C.<F>d__0.<>1__state"
                IL_0069:  ldloca.s   V_1
                IL_006b:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
                IL_0070:  nop
                IL_0071:  leave.s    IL_008b
              }
              catch System.Exception
              {
                // sequence point: <hidden>
                IL_0073:  stloc.3
                IL_0074:  ldarg.0
                IL_0075:  ldc.i4.s   -2
                IL_0077:  stfld      "int C.<F>d__0.<>1__state"
                IL_007c:  ldarg.0
                IL_007d:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
                IL_0082:  ldloc.3
                IL_0083:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_0088:  nop
                IL_0089:  leave.s    IL_009f
              }
              // sequence point: }
              IL_008b:  ldarg.0
              IL_008c:  ldc.i4.s   -2
              IL_008e:  stfld      "int C.<F>d__0.<>1__state"
              // sequence point: <hidden>
              IL_0093:  ldarg.0
              IL_0094:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
              IL_0099:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_009e:  nop
              IL_009f:  ret
            }
            """);
    }

    [Fact]
    public void Records()
    {
        var source = """
            using System.Text;
            record class R(int P)
            {
                protected virtual bool PrintMembers(StringBuilder builder)
                {
                    builder.Append("x");
                    return true;
                }
            }
            """ + IsExternalInitTypeDefinition;

        var verifier = CompileAndVerify(source, verification: Verification.FailsPEVerify);

        AssertNotInstrumented(verifier, "R.P.get");
        AssertNotInstrumented(verifier, "R.P.init");
        AssertNotInstrumented(verifier, "R.<Clone>$()");
        AssertNotInstrumented(verifier, "R.Deconstruct(out int)");
        AssertNotInstrumented(verifier, "R.Equals(object)");
        AssertNotInstrumented(verifier, "R.Equals(R)");
        AssertNotInstrumented(verifier, "R.GetHashCode()");
        AssertNotInstrumented(verifier, "R.EqualityContract.get");
        AssertNotInstrumented(verifier, "R.ToString()");
        AssertNotInstrumented(verifier, "bool R.op_Equality(R, R)");
        AssertNotInstrumented(verifier, "bool R.op_Inequality(R, R)");

        verifier.VerifyMethodBody("R.PrintMembers(System.Text.StringBuilder)", """
            {
              // Code size       25 (0x19)
              .maxstack  2
              .locals init (bool V_0)
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: {
              IL_0006:  nop
              // sequence point: builder.Append("x");
              IL_0007:  ldarg.1
              IL_0008:  ldstr      "x"
              IL_000d:  callvirt   "System.Text.StringBuilder System.Text.StringBuilder.Append(string)"
              IL_0012:  pop
              // sequence point: return true;
              IL_0013:  ldc.i4.1
              IL_0014:  stloc.0
              IL_0015:  br.s       IL_0017
              // sequence point: }
              IL_0017:  ldloc.0
              IL_0018:  ret
            }
            """);

        // We instrument the copy constructor for simplicity, even though it does not contain any user code.
        verifier.VerifyMethodBody("R..ctor(R)", """
            {
              // Code size       27 (0x1b)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              IL_0006:  ldarg.0
              IL_0007:  call       "object..ctor()"
              IL_000c:  nop
              IL_000d:  ldarg.0
              IL_000e:  ldarg.1
              IL_000f:  ldfld      "int R.<P>k__BackingField"
              IL_0014:  stfld      "int R.<P>k__BackingField"
              // sequence point: R
              IL_0019:  nop
              IL_001a:  ret
            }
            """);

        // We instrument the primary constructor for simplicity, even though it does not contain any user code.
        verifier.VerifyMethodBody("R..ctor(int)", """
             {
              // Code size       21 (0x15)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()"
              IL_0005:  nop
              // sequence point: <hidden>
              IL_0006:  ldarg.0
              IL_0007:  ldarg.1
              IL_0008:  stfld      "int R.<P>k__BackingField"
              // sequence point: R(int P)
              IL_000d:  ldarg.0
              IL_000e:  call       "object..ctor()"
              IL_0013:  nop
              IL_0014:  ret
            }
            """);
    }
}
