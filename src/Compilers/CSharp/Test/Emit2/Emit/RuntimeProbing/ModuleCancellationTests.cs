// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class ModuleCancellationTests : CSharpTestBase
{
    private static readonly EmitOptions s_emitOptions = EmitOptions.Default.WithInstrumentationKinds([InstrumentationKind.ModuleCancellation]);

    private CompilationVerifier CompileAndVerify(string source, string? expectedOutput = null, CSharpCompilationOptions? options = null, Verification? verification = null)
        => CompileAndVerify(
            source,
            options: options ?? (expectedOutput != null ? TestOptions.UnsafeDebugExe : TestOptions.UnsafeDebugDll),
            emitOptions: s_emitOptions,
            verify: verification ?? Verification.Passes,
            targetFramework: TargetFramework.NetLatest,
            expectedOutput: expectedOutput);

    private static void AssertNotInstrumented(CompilationVerifier verifier, string qualifiedMethodName)
        => AssertNotInstrumented(verifier, qualifiedMethodName, "<PrivateImplementationDetails>.ModuleCancellationToken");

    private static void AssertNotInstrumentedWithTokenLoad(CompilationVerifier verifier, string qualifiedMethodName)
        => AssertNotInstrumented(verifier, qualifiedMethodName, @"ldsfld ""System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken""");

    private static void AssertNotInstrumented(CompilationVerifier verifier, string qualifiedMethodName, string instrumentationIndicator)
    {
        var il = verifier.VisualizeIL(qualifiedMethodName);
        var isInstrumented = string.Join(" ", il.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Contains(instrumentationIndicator);

        Assert.False(isInstrumented,
            $"Method '{qualifiedMethodName}' should not be instrumented with '{instrumentationIndicator}'. Actual IL:{Environment.NewLine}{il}");
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
              // Code size       51 (0x33)
              .maxstack  3
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              IL_000b:  nop
              // sequence point: F(() => 1);
              IL_000c:  ldarg.0
              IL_000d:  ldsfld     "System.Func<int> C.<>c.<>9__0_1"
              IL_0012:  dup
              IL_0013:  brtrue.s   IL_002c
              IL_0015:  pop
              IL_0016:  ldsfld     "C.<>c C.<>c.<>9"
              IL_001b:  ldftn      "int C.<>c.<F>b__0_1()"
              IL_0021:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
              IL_0026:  dup
              IL_0027:  stsfld     "System.Func<int> C.<>c.<>9__0_1"
              IL_002c:  call       "void C.F(System.Func<int>)"
              IL_0031:  nop
              // sequence point: }
              IL_0032:  ret
            }
            """);

        verifier.VerifyMethodBody("C.<F>g__L|0_0", """
            {
              // Code size       18 (0x12)
              .maxstack  1
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: L();
              IL_000b:  call       "void C.<F>g__L|0_0()"
              IL_0010:  nop
              // sequence point: }
              IL_0011:  ret
            }
            """);

        verifier.VerifyMethodBody("C.<>c.<F>b__0_1", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: 1
              IL_000a:  ldc.i4.1
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void LambdaAndLocalFunction_TopLevel()
    {
        var source = """
            using System;

            var b = true;
            for(;b;) F();
            
            void F()
            {
                while(b) { }
            }
            """;

        var verifier = CompileAndVerify(source, options: TestOptions.DebugExe);

        verifier.VerifyMethodBody("<top-level-statements-entry-point>", """
            {
              // Code size       60 (0x3c)
              .maxstack  2
              .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                            bool V_1)
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: var b = true;
              IL_000a:  ldloca.s   V_0
              IL_000c:  ldc.i4.1
              IL_000d:  stfld      "bool Program.<>c__DisplayClass0_0.b"
              // sequence point: <hidden>
              IL_0012:  br.s       IL_001c
              // sequence point: F();
              IL_0014:  ldloca.s   V_0
              IL_0016:  call       "void Program.<<Main>$>g__F|0_0(ref Program.<>c__DisplayClass0_0)"
              IL_001b:  nop
              // sequence point: b
              IL_001c:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0021:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_0026:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_002b:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_0030:  ldloc.0
              IL_0031:  ldfld      "bool Program.<>c__DisplayClass0_0.b"
              IL_0036:  stloc.1
              // sequence point: <hidden>
              IL_0037:  ldloc.1
              IL_0038:  brtrue.s   IL_0014
              IL_003a:  nop
              IL_003b:  ret
            }
            """);

        verifier.VerifyMethodBody("Program.<<Main>$>g__F|0_0", """
            {
              // Code size       36 (0x24)
              .maxstack  1
              .locals init (bool V_0)
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: <hidden>
              IL_000b:  br.s       IL_000f
              // sequence point: {
              IL_000d:  nop
              // sequence point: }
              IL_000e:  nop
              // sequence point: while(b)
              IL_000f:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0014:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_0019:  ldarg.0
              IL_001a:  ldfld      "bool Program.<>c__DisplayClass0_0.b"
              IL_001f:  stloc.0
              // sequence point: <hidden>
              IL_0020:  ldloc.0
              IL_0021:  brtrue.s   IL_000d
              // sequence point: }
              IL_0023:  ret
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
                static bool b = true;

                static C()
                {
                    while (b)
                    {
                    }
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        // no preamble but while loop is instrumented:
        verifier.VerifyMethodBody("C..cctor", """
            {
              // Code size       31 (0x1f)
              .maxstack  1
              .locals init (bool V_0)
              // sequence point: {
              IL_0000:  nop
              // sequence point: static bool b = true;
              IL_0001:  ldc.i4.1
              IL_0002:  stsfld     "bool C.b"
              // sequence point: <hidden>
              IL_0007:  br.s       IL_000b
              // sequence point: {
              IL_0009:  nop
              // sequence point: }
              IL_000a:  nop
              // sequence point: while (b)
              IL_000b:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0010:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_0015:  ldsfld     "bool C.b"
              IL_001a:  stloc.0
              // sequence point: <hidden>
              IL_001b:  ldloc.0
              IL_001c:  brtrue.s   IL_0009
              // sequence point: }
              IL_001e:  ret
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
                static Action<bool> s_f = b => { while (b) {} };
            }
            """;

        var verifier = CompileAndVerify(source);

        // no probe since static constructor can only be invoked once:
        AssertNotInstrumented(verifier, "C..cctor");

        // lambda should be instrumented
        verifier.VerifyMethodBody("C.<>c.<.cctor>b__2_0", """
            {
              // Code size       31 (0x1f)
              .maxstack  1
              .locals init (bool V_0)
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: <hidden>
              IL_000b:  br.s       IL_000f
              // sequence point: {
              IL_000d:  nop
              // sequence point: }
              IL_000e:  nop
              // sequence point: while (b)
              IL_000f:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0014:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_0019:  ldarg.1
              IL_001a:  stloc.0
              // sequence point: <hidden>
              IL_001b:  ldloc.0
              IL_001c:  brtrue.s   IL_000d
              // sequence point: }
              IL_001e:  ret
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
              // Code size       29 (0x1d)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: C C = new();
              IL_000a:  ldarg.0
              IL_000b:  newobj     "C..ctor()"
              IL_0010:  stfld      "C B.C"
              IL_0015:  ldarg.0
              IL_0016:  call       "object..ctor()"
              IL_001b:  nop
              IL_001c:  ret
            }
            """);

        verifier.VerifyMethodBody("C..ctor", """
            {
              // Code size       18 (0x12)
              .maxstack  1
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_000a:  ldarg.0
              IL_000b:  call       "B..ctor()"
              IL_0010:  nop
              IL_0011:  ret
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
              // Code size       18 (0x12)
              .maxstack  1
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: B(int a)
              IL_000a:  ldarg.0
              IL_000b:  call       "object..ctor()"
              IL_0010:  nop
              IL_0011:  ret
            }
            """);

        verifier.VerifyMethodBody("C..ctor", """
            {
              // Code size       31 (0x1f)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: C c = new C(1);
              IL_000a:  ldarg.0
              IL_000b:  ldc.i4.1
              IL_000c:  newobj     "C..ctor(int)"
              IL_0011:  stfld      "C C.c"
              // sequence point: B(a)
              IL_0016:  ldarg.0
              IL_0017:  ldarg.1
              IL_0018:  call       "B..ctor(int)"
              IL_001d:  nop
              IL_001e:  ret
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
              // Code size       22 (0x16)
              .maxstack  1
              .locals init (int V_0)
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: return P;
              IL_000b:  ldarg.0
              IL_000c:  call       "int C.P.get"
              IL_0011:  stloc.0
              IL_0012:  br.s       IL_0014
              // sequence point: }
              IL_0014:  ldloc.0
              IL_0015:  ret
            }
            """);

        verifier.VerifyMethodBody("C.P.set", """
            {
              // Code size       20 (0x14)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: P = value;
              IL_000b:  ldarg.0
              IL_000c:  ldarg.1
              IL_000d:  call       "void C.P.set"
              IL_0012:  nop
              // sequence point: }
              IL_0013:  ret
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
              // Code size       12 (0xc)
              .maxstack  1
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: }
              IL_000b:  ret
            }
            """);

        verifier.VerifyMethodBody("C.E.remove", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: }
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void EventAccessors_Explicit()
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
    public void StateMachine_Iterator()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                public IEnumerable<int> F()
                {
                    while (true)
                    {
                        yield return 1;
                    }
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
              // Code size       86 (0x56)
              .maxstack  2
              .locals init (int V_0,
                            bool V_1)
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: <hidden>
              IL_000a:  ldarg.0
              IL_000b:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0010:  stloc.0
              IL_0011:  ldloc.0
              IL_0012:  brfalse.s  IL_001c
              IL_0014:  br.s       IL_0016
              IL_0016:  ldloc.0
              IL_0017:  ldc.i4.1
              IL_0018:  beq.s      IL_001e
              IL_001a:  br.s       IL_0020
              IL_001c:  br.s       IL_0022
              IL_001e:  br.s       IL_003d
              IL_0020:  ldc.i4.0
              IL_0021:  ret
              IL_0022:  ldarg.0
              IL_0023:  ldc.i4.m1
              IL_0024:  stfld      "int C.<F>d__0.<>1__state"
              // sequence point: {
              IL_0029:  nop
              // sequence point: <hidden>
              IL_002a:  br.s       IL_0045
              // sequence point: {
              IL_002c:  nop
              // sequence point: yield return 1;
              IL_002d:  ldarg.0
              IL_002e:  ldc.i4.1
              IL_002f:  stfld      "int C.<F>d__0.<>2__current"
              IL_0034:  ldarg.0
              IL_0035:  ldc.i4.1
              IL_0036:  stfld      "int C.<F>d__0.<>1__state"
              IL_003b:  ldc.i4.1
              IL_003c:  ret
              // sequence point: <hidden>
              IL_003d:  ldarg.0
              IL_003e:  ldc.i4.m1
              IL_003f:  stfld      "int C.<F>d__0.<>1__state"
              // sequence point: }
              IL_0044:  nop
              // sequence point: while (true)
              IL_0045:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_004a:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_004f:  ldc.i4.1
              IL_0050:  stloc.1
              // sequence point: <hidden>
              IL_0051:  ldloc.1
              IL_0052:  brtrue.s   IL_002c
              IL_0054:  ldc.i4.0
              IL_0055:  ret
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
                    while (true)
                    {
                        await Task.FromResult(2);
                    }
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumented(verifier, "C.F");
        AssertNotInstrumented(verifier, "C.<F>d__0..ctor");
        AssertNotInstrumented(verifier, "C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine");

        verifier.VerifyMethodBody("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      186 (0xba)
              .maxstack  3
              .locals init (int V_0,
                            System.Runtime.CompilerServices.TaskAwaiter<int> V_1,
                            C.<F>d__0 V_2,
                            bool V_3,
                            System.Exception V_4)
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: <hidden>
              IL_000a:  ldarg.0
              IL_000b:  ldfld      "int C.<F>d__0.<>1__state"
              IL_0010:  stloc.0
              .try
              {
                // sequence point: <hidden>
                IL_0011:  ldloc.0
                IL_0012:  brfalse.s  IL_0016
                IL_0014:  br.s       IL_0018
                IL_0016:  br.s       IL_0055
                // sequence point: {
                IL_0018:  nop
                // sequence point: <hidden>
                IL_0019:  br.s       IL_007a
                // sequence point: {
                IL_001b:  nop
                // sequence point: await Task.FromResult(2);
                IL_001c:  ldc.i4.2
                IL_001d:  call       "System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)"
                IL_0022:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                IL_0027:  stloc.1
                // sequence point: <hidden>
                IL_0028:  ldloca.s   V_1
                IL_002a:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                IL_002f:  brtrue.s   IL_0071
                IL_0031:  ldarg.0
                IL_0032:  ldc.i4.0
                IL_0033:  dup
                IL_0034:  stloc.0
                IL_0035:  stfld      "int C.<F>d__0.<>1__state"
                // async: yield
                IL_003a:  ldarg.0
                IL_003b:  ldloc.1
                IL_003c:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_0041:  ldarg.0
                IL_0042:  stloc.2
                IL_0043:  ldarg.0
                IL_0044:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
                IL_0049:  ldloca.s   V_1
                IL_004b:  ldloca.s   V_2
                IL_004d:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)"
                IL_0052:  nop
                IL_0053:  leave.s    IL_00b9
                // async: resume
                IL_0055:  ldarg.0
                IL_0056:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_005b:  stloc.1
                IL_005c:  ldarg.0
                IL_005d:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                IL_0068:  ldarg.0
                IL_0069:  ldc.i4.m1
                IL_006a:  dup
                IL_006b:  stloc.0
                IL_006c:  stfld      "int C.<F>d__0.<>1__state"
                IL_0071:  ldloca.s   V_1
                IL_0073:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                IL_0078:  pop
                // sequence point: }
                IL_0079:  nop
                // sequence point: while (true)
                IL_007a:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
                IL_007f:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
                IL_0084:  ldc.i4.1
                IL_0085:  stloc.3
                // sequence point: <hidden>
                IL_0086:  ldloc.3
                IL_0087:  brtrue.s   IL_001b
                IL_0089:  leave.s    IL_00a5
              }
              catch System.Exception
              {
                // sequence point: <hidden>
                IL_008b:  stloc.s    V_4
                IL_008d:  ldarg.0
                IL_008e:  ldc.i4.s   -2
                IL_0090:  stfld      "int C.<F>d__0.<>1__state"
                IL_0095:  ldarg.0
                IL_0096:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
                IL_009b:  ldloc.s    V_4
                IL_009d:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_00a2:  nop
                IL_00a3:  leave.s    IL_00b9
              }
              // sequence point: }
              IL_00a5:  ldarg.0
              IL_00a6:  ldc.i4.s   -2
              IL_00a8:  stfld      "int C.<F>d__0.<>1__state"
              // sequence point: <hidden>
              IL_00ad:  ldarg.0
              IL_00ae:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder"
              IL_00b3:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_00b8:  nop
              IL_00b9:  ret
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
              // Code size       29 (0x1d)
              .maxstack  2
              .locals init (bool V_0)
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: builder.Append("x");
              IL_000b:  ldarg.1
              IL_000c:  ldstr      "x"
              IL_0011:  callvirt   "System.Text.StringBuilder System.Text.StringBuilder.Append(string)"
              IL_0016:  pop
              // sequence point: return true;
              IL_0017:  ldc.i4.1
              IL_0018:  stloc.0
              IL_0019:  br.s       IL_001b
              // sequence point: }
              IL_001b:  ldloc.0
              IL_001c:  ret
            }
            """);

        // We instrument the copy constructor for simplicity, even though it does not contain any user code.
        verifier.VerifyMethodBody("R..ctor(R)", """
            {
              // Code size       31 (0x1f)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_000a:  ldarg.0
              IL_000b:  call       "object..ctor()"
              IL_0010:  nop
              IL_0011:  ldarg.0
              IL_0012:  ldarg.1
              IL_0013:  ldfld      "int R.<P>k__BackingField"
              IL_0018:  stfld      "int R.<P>k__BackingField"
              // sequence point: R
              IL_001d:  nop
              IL_001e:  ret
            }
            """);

        // We instrument the primary constructor for simplicity, even though it does not contain any user code.
        verifier.VerifyMethodBody("R..ctor(int)", """
            {
              // Code size       25 (0x19)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: <hidden>
              IL_000a:  ldarg.0
              IL_000b:  ldarg.1
              IL_000c:  stfld      "int R.<P>k__BackingField"
              // sequence point: R(int P)
              IL_0011:  ldarg.0
              IL_0012:  call       "object..ctor()"
              IL_0017:  nop
              IL_0018:  ret
            }
            """);
    }

    [Fact]
    public void While()
    {
        var source = """
            using System;

            class C
            {
                void F(bool b)
                {
                    while(b) { Console.WriteLine(); }
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       37 (0x25)
              .maxstack  1
              .locals init (bool V_0)
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: <hidden>
              IL_000b:  br.s       IL_0015
              // sequence point: {
              IL_000d:  nop
              // sequence point: Console.WriteLine();
              IL_000e:  call       "void System.Console.WriteLine()"
              IL_0013:  nop
              // sequence point: }
              IL_0014:  nop
              // sequence point: while(b)
              IL_0015:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_001a:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_001f:  ldarg.1
              IL_0020:  stloc.0
              // sequence point: <hidden>
              IL_0021:  ldloc.0
              IL_0022:  brtrue.s   IL_000d
              // sequence point: }
              IL_0024:  ret
            }
            """);
    }

    [Fact]
    public void DoWhile()
    {
        var source = """
            using System;

            class C
            {
                void F(bool b)
                {
                    do { Console.WriteLine(); } while (b);
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       35 (0x23)
              .maxstack  1
              .locals init (bool V_0)
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: {
              IL_000b:  nop
              // sequence point: Console.WriteLine();
              IL_000c:  call       "void System.Console.WriteLine()"
              IL_0011:  nop
              // sequence point: }
              IL_0012:  nop
              // sequence point: while (b);
              IL_0013:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0018:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_001d:  ldarg.1
              IL_001e:  stloc.0
              // sequence point: <hidden>
              IL_001f:  ldloc.0
              IL_0020:  brtrue.s   IL_000b
              // sequence point: }
              IL_0022:  ret
            }
            """);
    }

    [Fact]
    public void For()
    {
        var source = """
            using System;

            class C
            {
                void F()
                {
                    for (;;) { Console.WriteLine(); }
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       33 (0x21)
              .maxstack  1
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: <hidden>
              IL_000b:  br.s       IL_0015
              // sequence point: {
              IL_000d:  nop
              // sequence point: Console.WriteLine();
              IL_000e:  call       "void System.Console.WriteLine()"
              IL_0013:  nop
              // sequence point: }
              IL_0014:  nop
              // sequence point: <hidden>
              IL_0015:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_001a:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_001f:  br.s       IL_000d
            }
            """);
    }

    [Fact]
    public void ForEach_Array()
    {
        var source = """
            using System;

            class C
            {
                void F(int[] items)
                {
                    foreach (var item in items) { Console.WriteLine(item); }
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       52 (0x34)
              .maxstack  2
              .locals init (int[] V_0,
                            int V_1,
                            int V_2) //item
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: foreach
              IL_000b:  nop
              // sequence point: items
              IL_000c:  ldarg.1
              IL_000d:  stloc.0
              IL_000e:  ldc.i4.0
              IL_000f:  stloc.1
              // sequence point: <hidden>
              IL_0010:  br.s       IL_0023
              // sequence point: var item
              IL_0012:  ldloc.0
              IL_0013:  ldloc.1
              IL_0014:  ldelem.i4
              IL_0015:  stloc.2
              // sequence point: {
              IL_0016:  nop
              // sequence point: Console.WriteLine(item);
              IL_0017:  ldloc.2
              IL_0018:  call       "void System.Console.WriteLine(int)"
              IL_001d:  nop
              // sequence point: }
              IL_001e:  nop
              // sequence point: <hidden>
              IL_001f:  ldloc.1
              IL_0020:  ldc.i4.1
              IL_0021:  add
              IL_0022:  stloc.1
              // sequence point: in
              IL_0023:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0028:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_002d:  ldloc.1
              IL_002e:  ldloc.0
              IL_002f:  ldlen
              IL_0030:  conv.i4
              IL_0031:  blt.s      IL_0012
              // sequence point: }
              IL_0033:  ret
            }
            """);
    }

    [Fact]
    public void ForEach_Enumerable()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                void F(IEnumerable<int> items)
                {
                    foreach (var item in items) { Console.WriteLine(item); }
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       69 (0x45)
              .maxstack  1
              .locals init (System.Collections.Generic.IEnumerator<int> V_0,
                            int V_1) //item
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: foreach
              IL_000b:  nop
              // sequence point: items
              IL_000c:  ldarg.1
              IL_000d:  callvirt   "System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()"
              IL_0012:  stloc.0
              .try
              {
                // sequence point: <hidden>
                IL_0013:  br.s       IL_0025
                // sequence point: var item
                IL_0015:  ldloc.0
                IL_0016:  callvirt   "int System.Collections.Generic.IEnumerator<int>.Current.get"
                IL_001b:  stloc.1
                // sequence point: {
                IL_001c:  nop
                // sequence point: Console.WriteLine(item);
                IL_001d:  ldloc.1
                IL_001e:  call       "void System.Console.WriteLine(int)"
                IL_0023:  nop
                // sequence point: }
                IL_0024:  nop
                // sequence point: in
                IL_0025:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
                IL_002a:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
                IL_002f:  ldloc.0
                IL_0030:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                IL_0035:  brtrue.s   IL_0015
                IL_0037:  leave.s    IL_0044
              }
              finally
              {
                // sequence point: <hidden>
                IL_0039:  ldloc.0
                IL_003a:  brfalse.s  IL_0043
                IL_003c:  ldloc.0
                IL_003d:  callvirt   "void System.IDisposable.Dispose()"
                IL_0042:  nop
                // sequence point: <hidden>
                IL_0043:  endfinally
              }
              // sequence point: }
              IL_0044:  ret
            }
            """);
    }

    [Fact]
    public void GoTo()
    {
        var source = """
            using System;

            class C
            {
                void F()
                {
                    label:
                    Console.WriteLine();
                    goto label;
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        // TODO: odd sequence point // sequence point: { ...     }
        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       30 (0x1e)
              .maxstack  1
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: label:
              IL_000b:  nop
              // sequence point: Console.WriteLine();
              IL_000c:  call       "void System.Console.WriteLine()"
              IL_0011:  nop
              // sequence point: { ...     }
              IL_0012:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0017:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_001c:  br.s       IL_000b
            }
            """);
    }

    [Fact]
    public void GoToCase()
    {
        var source = """
            using System;

            class C
            {
                void F(int x)
                {
                    switch (x)
                    {
                        case 1: Console.WriteLine(1); goto case 2;
                        case 2: Console.WriteLine(2); goto case 1;
                    }
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        // TODO: odd sequence points: // sequence point: case 1: Console.WriteLine(1); goto case 2;
        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       66 (0x42)
              .maxstack  2
              .locals init (int V_0,
                            int V_1)
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: switch (x)
              IL_000b:  ldarg.1
              IL_000c:  stloc.1
              // sequence point: <hidden>
              IL_000d:  ldloc.1
              IL_000e:  stloc.0
              // sequence point: <hidden>
              IL_000f:  ldloc.0
              IL_0010:  ldc.i4.1
              IL_0011:  beq.s      IL_001b
              IL_0013:  br.s       IL_0015
              IL_0015:  ldloc.0
              IL_0016:  ldc.i4.2
              IL_0017:  beq.s      IL_002e
              IL_0019:  br.s       IL_0041
              // sequence point: Console.WriteLine(1);
              IL_001b:  ldc.i4.1
              IL_001c:  call       "void System.Console.WriteLine(int)"
              IL_0021:  nop
              // sequence point: case 1: Console.WriteLine(1); goto case 2;
              IL_0022:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0027:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_002c:  br.s       IL_002e
              // sequence point: Console.WriteLine(2);
              IL_002e:  ldc.i4.2
              IL_002f:  call       "void System.Console.WriteLine(int)"
              IL_0034:  nop
              // sequence point: case 2: Console.WriteLine(2); goto case 1;
              IL_0035:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_003a:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              IL_003f:  br.s       IL_001b
              // sequence point: }
              IL_0041:  ret
            }
            """);
    }

    [Fact]
    public void ArgumentReplacement_SourceVsLibrary()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G(token);                        
                    var _ = Task.FromCanceled(token);
                }

                void G(CancellationToken token)
                {
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        // Replace token regardless of whether the target method is from source or defined externally.
        // Allows us to be consistent when the target method is invoked indirectly via a delegate.
        // In that case we wouldn't know where it's defined.
        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       35 (0x23)
              .maxstack  2
              .locals init (System.Threading.Tasks.Task V_0) //_
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: G(token);
              IL_000b:  ldarg.0
              IL_000c:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0011:  call       "void C.G(System.Threading.CancellationToken)"
              IL_0016:  nop
              // sequence point: var _ = Task.FromCanceled(token);
              IL_0017:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_001c:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.FromCanceled(System.Threading.CancellationToken)"
              IL_0021:  stloc.0
              // sequence point: }
              IL_0022:  ret
            }
            """);
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("out")]
    [InlineData("in")]
    public void ArgumentReplacement_RefKinds(string refKind)
    {
        var source = $$"""
            using System.Threading;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G({{refKind}} token);                        
                }

                void G({{refKind}} CancellationToken token)
                {
                    throw null;
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        // Doesn't force module-level cancellation token to ref/in/out parameters as these are not commonly used in cancellable APIs.
        verifier.VerifyMethodBody("C.F", $$"""
            {
              // Code size       21 (0x15)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: G({{refKind}} token);
              IL_000b:  ldarg.0
              IL_000c:  ldarga.s   V_1
              IL_000e:  call       "void C.G({{refKind}} System.Threading.CancellationToken)"
              IL_0013:  nop
              // sequence point: }
              IL_0014:  ret
            }
            """);
    }

    [Fact]
    public void ArgumentReplacement_Named()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G(token: token, a: 1);
                }

                void G(int a, CancellationToken token)
                {
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       25 (0x19)
              .maxstack  3
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: G(token: token, a: 1);
              IL_000b:  ldarg.0
              IL_000c:  ldc.i4.1
              IL_000d:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0012:  call       "void C.G(int, System.Threading.CancellationToken)"
              IL_0017:  nop
              // sequence point: }
              IL_0018:  ret
            }
            """);
    }

    [Fact]
    public void ArgumentReplacement_Optional()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G(1);
                }

                void G(int a, CancellationToken token = default)
                {
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
             {
              // Code size       25 (0x19)
              .maxstack  3
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: G(1);
              IL_000b:  ldarg.0
              IL_000c:  ldc.i4.1
              IL_000d:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0012:  call       "void C.G(int, System.Threading.CancellationToken)"
              IL_0017:  nop
              // sequence point: }
              IL_0018:  ret
            }
            """);
    }

    [Fact]
    public void ArgumentReplacement_IndirectCall_Positional()
    {
        var source = """
            using System;
            using System.Threading;
            
            class C
            {
                void F(CancellationToken token)
                {
                    var g = new Action<CancellationToken>(G);
                    g(token);                        
                }

                void G(CancellationToken token)
                {
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", $$"""
            {
              // Code size       37 (0x25)
              .maxstack  2
              .locals init (System.Action<System.Threading.CancellationToken> V_0) //g
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: var g = new Action<CancellationToken>(G);
              IL_000b:  ldarg.0
              IL_000c:  ldftn      "void C.G(System.Threading.CancellationToken)"
              IL_0012:  newobj     "System.Action<System.Threading.CancellationToken>..ctor(object, System.IntPtr)"
              IL_0017:  stloc.0
              // sequence point: g(token);
              IL_0018:  ldloc.0
              IL_0019:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_001e:  callvirt   "void System.Action<System.Threading.CancellationToken>.Invoke(System.Threading.CancellationToken)"
              IL_0023:  nop
              // sequence point: }
              IL_0024:  ret
            }
            """);
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("out")]
    [InlineData("in")]
    public void ArgumentReplacement_IndirectCall_RefKinds(string refKind)
    {
        var source = $$"""
            using System.Threading;
            
            delegate void D({{refKind}} CancellationToken token);

            class C
            {
                void F(CancellationToken token)
                {
                    var g = new D(G);
                    g({{refKind}} token);                        
                }

                void G({{refKind}} CancellationToken token)
                {
                    throw null;
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        // Doesn't force module-level cancellation token to ref/in/out parameters as these are not commonly used in cancellable APIs.
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void ArgumentReplacement_IndirectCall_Named()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            delegate void D(int a, CancellationToken token);
            
            class C
            {
                void F(CancellationToken token)
                {
                    var g = new D(G);
                    g(token: token, a: 1);
                }

                void G(int a, CancellationToken token)
                {
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", $$"""
            {
              // Code size       38 (0x26)
              .maxstack  3
              .locals init (D V_0) //g
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: var g = new D(G);
              IL_000b:  ldarg.0
              IL_000c:  ldftn      "void C.G(int, System.Threading.CancellationToken)"
              IL_0012:  newobj     "D..ctor(object, System.IntPtr)"
              IL_0017:  stloc.0
              // sequence point: g(token: token, a: 1);
              IL_0018:  ldloc.0
              IL_0019:  ldc.i4.1
              IL_001a:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_001f:  callvirt   "void D.Invoke(int, System.Threading.CancellationToken)"
              IL_0024:  nop
              // sequence point: }
              IL_0025:  ret
            }
            """);
    }

    [Fact]
    public void ArgumentReplacement_IndirectCall_Optional()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;

            delegate void D(int a, CancellationToken token = default);
            
            class C
            {
                void F(CancellationToken token)
                {
                    var g = new D(G);
                    g(1);
                }

                void G(int a, CancellationToken token)
                {
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", $$"""
             {
              // Code size       38 (0x26)
              .maxstack  3
              .locals init (D V_0) //g
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: var g = new D(G);
              IL_000b:  ldarg.0
              IL_000c:  ldftn      "void C.G(int, System.Threading.CancellationToken)"
              IL_0012:  newobj     "D..ctor(object, System.IntPtr)"
              IL_0017:  stloc.0
              // sequence point: g(1);
              IL_0018:  ldloc.0
              IL_0019:  ldc.i4.1
              IL_001a:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_001f:  callvirt   "void D.Invoke(int, System.Threading.CancellationToken)"
              IL_0024:  nop
              // sequence point: }
              IL_0025:  ret
            }
            """);
    }

    [Fact]
    public void ArgumentReplacement_DynamicCall()
    {
        var source = """
            using System.Threading;
            
            class C
            {
                void F(CancellationToken token)
                {
                    dynamic d = new C();
                    d.G("str");
                }

                void G(string s) => throw null;
                void G(int a, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_Matching()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G(1);
                }

                void G(int a)
                {
                }
            
                void G(out int a, CancellationToken token) => throw null;
                void G(long a, CancellationToken token) => throw null;
                void G(CancellationToken token) => throw null;
                void G(int a, CancellationToken token) => throw null;
                void G(CancellationToken token, int a) => throw null;
                void G(int a, int b, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
             {
              // Code size       25 (0x19)
              .maxstack  3
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: G(1);
              IL_000b:  ldarg.0
              IL_000c:  ldc.i4.1
              IL_000d:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0012:  call       "void C.G(int, System.Threading.CancellationToken)"
              IL_0017:  nop
              // sequence point: }
              IL_0018:  ret
            }
            """);
    }

    [Fact]
    public void CancellableOverload_ObjectVsDynamic_Arg()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G(1);
                }

                void G(object a) => throw null;
                void G(dynamic a, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_ObjectToDynamic_Return()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    var x = G(1);
                    var y = x.ToString();
                }

                object G(int a) => throw null;
                dynamic G(int a, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_DynamicToObject_Return()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    var x = G(1);
                    var y = x.ToString();
                }

                dynamic G(int a) => throw null;
                object G(int a, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_ArgumentNames()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G((1, 1));
                }

                (int u, int v) G((int x, int y) a) => throw null;
                (int u, int w) G((int z, int y) b, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       31 (0x1f)
              .maxstack  3
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: G((1, 1));
              IL_000b:  ldarg.0
              IL_000c:  ldc.i4.1
              IL_000d:  ldc.i4.1
              IL_000e:  newobj     "System.ValueTuple<int, int>..ctor(int, int)"
              IL_0013:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0018:  call       "System.ValueTuple<int, int> C.G(System.ValueTuple<int, int>, System.Threading.CancellationToken)"
              IL_001d:  pop
              // sequence point: }
              IL_001e:  ret
            }
            """);
    }

    [Fact]
    public void CancellableOverload_InstanceConstructor_BaseCall()
    {
        var source = """
            using System.Threading;

            class B
            {
                public B(int a) {}
                public B(int a, CancellationToken token) {}
            }

            class C() : B(1)
            {
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C..ctor", """
            {
              // Code size       24 (0x18)
              .maxstack  3
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: B(1)
              IL_000a:  ldarg.0
              IL_000b:  ldc.i4.1
              IL_000c:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0011:  call       "B..ctor(int, System.Threading.CancellationToken)"
              IL_0016:  nop
              IL_0017:  ret
            }
            """);
    }

    [Fact]
    public void CancellableOverload_InstanceConstructor_ThisCall()
    {
        var source = """
            using System.Threading;

            class C
            {
                public C() : this(1) {}
                public C(int a) {}
                public C(int a, CancellationToken token) {}
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C..ctor()", """
            {
              // Code size       25 (0x19)
              .maxstack  3
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: this(1)
              IL_000a:  ldarg.0
              IL_000b:  ldc.i4.1
              IL_000c:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0011:  call       "C..ctor(int, System.Threading.CancellationToken)"
              IL_0016:  nop
              // sequence point: {
              IL_0017:  nop
              // sequence point: }
              IL_0018:  ret
            }
            """);
    }

    [Fact]
    public void CancellableOverload_InstanceConstructor_ObjectCreation()
    {
        var source = """
            using System.Threading;

            class C
            {
                C(int a) {}
                C(int a, CancellationToken token) {}

                void F()
                {
                    var c = new C(1);
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       24 (0x18)
              .maxstack  2
              .locals init (C V_0) //c
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: var c = new C(1);
              IL_000b:  ldc.i4.1
              IL_000c:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0011:  newobj     "C..ctor(int, System.Threading.CancellationToken)"
              IL_0016:  stloc.0
              // sequence point: }
              IL_0017:  ret
            }
            """);
    }

    [Fact]
    public void CancellableOverload_InstanceConstructor_CollectionInitializers()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;
            using System.Threading;

            class C : IEnumerable<int>
            {
                void F()
                {
                    var c = new C()
                    {
                        1, 2
                    };
                }

                public void Add(int x) {}
                public void Add(int x, CancellationToken token) {}

                public IEnumerator<int> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       44 (0x2c)
              .maxstack  4
              .locals init (C V_0) //c
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: var c = new  ...         };
              IL_000b:  newobj     "C..ctor()"
              IL_0010:  dup
              IL_0011:  ldc.i4.1
              IL_0012:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0017:  callvirt   "void C.Add(int, System.Threading.CancellationToken)"
              IL_001c:  nop
              IL_001d:  dup
              IL_001e:  ldc.i4.2
              IL_001f:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0024:  callvirt   "void C.Add(int, System.Threading.CancellationToken)"
              IL_0029:  nop
              IL_002a:  stloc.0
              // sequence point: }
              IL_002b:  ret
            }
            """);
    }

    [Fact]
    public void CancellableOverload_InstanceConstructor_SetsRequiredMembers_NotCompatible()
    {
        var source = """
            using System.Threading;
            using System.Diagnostics.CodeAnalysis;

            class B
            {
                [SetsRequiredMembers]
                public B(int a) {}

                public B(int a, CancellationToken token) {}
            }

            [method: SetsRequiredMembers]
            class C() : B(1)
            {
            }
            """ + SetsRequiredMembersAttribute;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C..ctor");
    }

    [Theory]
    [InlineData("")]
    [InlineData("[SetsRequiredMembers]")]
    public void CancellableOverload_InstanceConstructor_SetsRequiredMembers_Compatible(string attributes)
    {
        var source = $$"""
            using System.Threading;
            using System.Diagnostics.CodeAnalysis;

            class B
            {
                {{attributes}}
                public B(int a) {}

                [SetsRequiredMembers]
                public B(int a, CancellationToken token) {}
            }

            [method: SetsRequiredMembers]
            class C() : B(1)
            {
            }
            """ + SetsRequiredMembersAttribute;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C..ctor", """
            {
              // Code size       24 (0x18)
              .maxstack  3
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: B(1)
              IL_000a:  ldarg.0
              IL_000b:  ldc.i4.1
              IL_000c:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0011:  call       "B..ctor(int, System.Threading.CancellationToken)"
              IL_0016:  nop
              IL_0017:  ret
            }
            """);
    }

    [Fact]
    public void CancellableOverload_ExplicitInterfaceImpl()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            interface I
            {
                void G(int a);
                void G(int a, CancellationToken token);
            }

            class C : I
            {
                void F(CancellationToken token)
                {
                    ((I)this).G(1, token);
                    ((I)this).G(1);
                }

                void I.G(int a) => throw null;
                void I.G(int a, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       38 (0x26)
              .maxstack  3
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: ((I)this).G(1, token);
              IL_000b:  ldarg.0
              IL_000c:  ldc.i4.1
              IL_000d:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0012:  callvirt   "void I.G(int, System.Threading.CancellationToken)"
              IL_0017:  nop
              // sequence point: ((I)this).G(1);
              IL_0018:  ldarg.0
              IL_0019:  ldc.i4.1
              IL_001a:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_001f:  callvirt   "void I.G(int, System.Threading.CancellationToken)"
              IL_0024:  nop
              // sequence point: }
              IL_0025:  ret
            }
            """);
    }

    [Fact]
    public void CancellableOverload_SpecializedCancellable_SameArity()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G<int>(0, 1);
                }

                void G<T>(T a, T b) => throw null;
                void G<T>(int a, T b, CancellationToken token) => throw null;
                void G<T>(bool a, T b, CancellationToken token) => throw null;
            }    
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_SpecializedCancellable_DifferentArity()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G<int, bool>(1, true);
                }

                void G<S, T>(S a, T b) => throw null;
                void G<T>(int a, T b, CancellationToken token) => throw null;
                void G<T>(bool a, T b, CancellationToken token) => throw null;
            }    
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_GenericCancellable_SameArity()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G<int>(0, 1);
                }

                void G<T>(int a, T b) => throw null;
                void G<T>(T a, T b, CancellationToken token) => throw null;
            }    
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_GenericCancellable_DifferentArity()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G<bool>(1, true);
                }
            
                void G<T>(int a, T b) => throw null;
                void G<S, T>(S a, T b, CancellationToken token) => throw null;
            }    
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_GenericContainingType()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C<T>
            {
                void F(CancellationToken token)
                {
                    G(default);
                    G(default, token);
                }

                void G(T a)
                {
                }
            
                void G(T a, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C<T>.F", """
            {
              // Code size       54 (0x36)
              .maxstack  3
              .locals init (T V_0)
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: G(default);
              IL_000b:  ldarg.0
              IL_000c:  ldloca.s   V_0
              IL_000e:  initobj    "T"
              IL_0014:  ldloc.0
              IL_0015:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_001a:  call       "void C<T>.G(T, System.Threading.CancellationToken)"
              IL_001f:  nop
              // sequence point: G(default, token);
              IL_0020:  ldarg.0
              IL_0021:  ldloca.s   V_0
              IL_0023:  initobj    "T"
              IL_0029:  ldloc.0
              IL_002a:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_002f:  call       "void C<T>.G(T, System.Threading.CancellationToken)"
              IL_0034:  nop
              // sequence point: }
              IL_0035:  ret
            }
            """);
    }

    [Fact]
    public void CancellableOverload_GenericContainingType2()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C<S, T>
            {
                void F()
                {
                    G(default);
                }

                void G(S a)
                {
                }
            
                void G(T a, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumentedWithTokenLoad(verifier, "C<S, T>.F");
    }

    [Fact]
    public void CancellableOverload_GenericContainingType_Specialized()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class D<T>
            {
                public static void G(T a) {}
                public static void G(int a, CancellationToken token) {}
            }

            class C
            {
                void F(CancellationToken token)
                {
                    D<int>.G(1);
                }
            }
                
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_GenericContainingType_Specialized_Generic()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class D<T>
            {
                public static void G<S>(T a) {}
                public static void G<S>(S a, CancellationToken token) {}
            }

            class C
            {
                void F(CancellationToken token)
                {
                    D<int>.G<int>(1);
                }
            }
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_GenericContainingType_Generalized()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class D<T>
            {
                public static void G(int a) {}
                public static void G(T a, CancellationToken token) {}
            }

            class C
            {
                void F(CancellationToken token)
                {
                    D<int>.G(1);
                }
            }
                
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_Generic_MatchingConstraint()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G<int>(1);
                }

                void G<T>(T a) where T : struct => throw null;
                void G<T>(T a, CancellationToken token) where T : struct => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       25 (0x19)
              .maxstack  3
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: G<int>(1);
              IL_000b:  ldarg.0
              IL_000c:  ldc.i4.1
              IL_000d:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0012:  call       "void C.G<int>(int, System.Threading.CancellationToken)"
              IL_0017:  nop
              // sequence point: }
              IL_0018:  ret
            }
            """);
    }

    [Fact]
    public void CancellableOverload_Generic_MatchingConstraint_TypeParameterNamesDiffer()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G<int, bool>(1, true);
                }

                void G<S, T>(S a, T b) where T : struct => throw null;
                void G<T, S>(T a, S b, CancellationToken token) where S : struct => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        verifier.VerifyMethodBody("C.F", """
            {
              // Code size       26 (0x1a)
              .maxstack  4
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: G<int, bool>(1, true);
              IL_000b:  ldarg.0
              IL_000c:  ldc.i4.1
              IL_000d:  ldc.i4.1
              IL_000e:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0013:  call       "void C.G<int, bool>(int, bool, System.Threading.CancellationToken)"
              IL_0018:  nop
              // sequence point: }
              IL_0019:  ret
            }
            """);
    }

    [Fact]
    public void CancellableOverload_Generic_CompatibleButNotSameConstraint()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G<int>(1);
                }

                void G<T>(int a) => throw null;            
                void G<T>(int a, CancellationToken token) where T : struct => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_Generic_IncompatibleConstraint()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G<int>(1);
                }

                void G<T>(int a) => throw null;            
                void G<T>(int a, CancellationToken token) where T : class => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_NoMatching()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G(1);
                }

                void G(int a)
                {
                }
            
                int G(int a, CancellationToken token) => throw null;
                void G(out int a, CancellationToken token) => throw null;
                void G(long a, CancellationToken token) => throw null;
                void G(CancellationToken token) => throw null;
                void G(CancellationToken token, int a) => throw null;
                void G(int a, int b, CancellationToken token) => throw null;
                void G<T>(int a, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Theory]
    [InlineData("ref", "")]
    [InlineData("", "ref")]
    [InlineData("ref readonly", "")]
    [InlineData("", "ref readonly")]
    [InlineData("ref", "ref readonly")]
    [InlineData("ref readonly", "ref")]
    public void CancellableOverload_ReturnType(string modifiers1, string modifiers2)
    {
        var source = $$"""
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G(1);
                }

                {{modifiers1}} int G(int a) => throw null;
                {{modifiers2}} int G(int a, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_InstanceToStatic()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G(1);
                }

                void G(int a)
                {
                }
            
                static void G(int a, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_StaticToInstance()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            
            class C
            {
                void F(CancellationToken token)
                {
                    G(1);
                }

                static void G(int a)
                {
                }
            
                void G(int a, CancellationToken token) => throw null;
            }
            """;

        var verifier = CompileAndVerify(source);

        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Theory]
    [CombinatorialData]
    public void CancellableOverload_NonMatchingVisibility(
        [CombinatorialValues("protected", "internal", "private protected", "internal protected")] string modifier1,
        [CombinatorialValues("private", "protected", "internal", "private protected", "internal protected")] string modifier2)
    {
        var source = $$"""
            using System.Threading;
            using System.Threading.Tasks;
            
            class B
            {
                {{modifier1}} static void G(int a) => throw null;
                {{modifier2}} static void G(int a, CancellationToken token) => throw null;
            }

            class C : B
            {
                void F(CancellationToken token)
                {
                    G(1);
                }
            }
            """;

        var verifier = CompileAndVerify(source);

        if (modifier1 == modifier2)
        {
            verifier.VerifyMethodBody("C.F", $$"""
            {
              // Code size       24 (0x18)
              .maxstack  2
              // sequence point: <hidden>
              IL_0000:  ldsflda    "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0005:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
              // sequence point: {
              IL_000a:  nop
              // sequence point: G(1);
              IL_000b:  ldc.i4.1
              IL_000c:  ldsfld     "System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken"
              IL_0011:  call       "void B.G(int, System.Threading.CancellationToken)"
              IL_0016:  nop
              // sequence point: }
              IL_0017:  ret
            }
            """);
        }
        else
        {
            AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
        }
    }

    [Fact]
    public void CancellableOverload_Indexer()
    {
        var source = """
            using System.Threading;
            
            class C
            {
                int this[int x] => 0;
                int this[int x, CancellationToken token] => 0;

                void F()
                {
                    var _ = this[1];
                }
            }
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_IndexerAndMethod()
    {
        var source = """
            using System.Threading;
            
            class C
            {
                int get_Item() => 0;
                int this[CancellationToken token] => 0;

                void F()
                {
                    var _ = get_Item();
                }
            }
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_PropertyAndMethod()
    {
        var source = """
            using System.Threading;
            
            class C
            {
                void set_P() {}
                CancellationToken P { get; set; }

                void F()
                {
                    set_P();
                }
            }
            """;

        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_RecordPropertyAndMethod()
    {
        var source = """
            using System.Threading;
            
            record C(CancellationToken P)
            {
                void set_P() {}

                void F()
                {
                    set_P();
                }
            }
            """ + IsExternalInitTypeDefinition;

        var verifier = CompileAndVerify(source, verification: Verification.FailsPEVerify);
        AssertNotInstrumentedWithTokenLoad(verifier, "C.F");
    }

    [Fact]
    public void CancellableOverload_InfiniteRecursionAvoidance()
    {
        var source = """
            using System.Threading;

            class C<T>
            {
                void G<S>(T a, S b, CancellationToken token) => G<int>(default, default);
                void G<S>(T a, S b) {}
            }
            """;

        // Avoid instrumentation that would trivially cause infinite recursion.
        var verifier = CompileAndVerify(source);
        AssertNotInstrumentedWithTokenLoad(verifier, "C<T>.G<S>(T, S, System.Threading.CancellationToken)");
    }

    [Fact]
    public void FlowPass()
    {
        var source = """
            using System.Threading;
            using System.Collections.Generic;

            class C
            {
                IEnumerable<int> F()
                {
                    var x = G() as string;
                    yield return (x != null) ? 1 : 0;
                }

                object G(CancellationToken token = default)
                    => "";
            }
            """;

        // definite assignment flow pass doesn't fail
        CompileAndVerify(source).VerifyDiagnostics();
    }
}
