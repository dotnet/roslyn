// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    using static Instruction;
    internal enum Instruction
    {
        Write,
        Yield,
        AwaitSlow,
        AwaitFast,
        YieldBreak
    }

    [CompilerTrait(CompilerFeature.AsyncStreams)]
    public class CodeGenAsyncIteratorTests : EmitMetadataTestBase
    {
        private void VerifyMissingMember(WellKnownMember member, params DiagnosticDescription[] expected)
        {
            var lib = CreateCompilationWithTasksExtensions(s_common);
            var lib_ref = lib.EmitToImageReference();

            string source = @"
using System.Threading.Tasks;
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
}
";
            var comp = CreateCompilationWithTasksExtensions(source, references: new[] { lib_ref });
            comp.MakeMemberMissing(member);
            comp.VerifyEmitDiagnostics(expected);
        }

        private void VerifyMissingType(WellKnownType type, params DiagnosticDescription[] expected)
        {
            var lib = CreateCompilationWithTasksExtensions(s_common);
            var lib_ref = lib.EmitToImageReference();

            string source = @"
using System.Threading.Tasks;
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
}
";
            var comp = CreateCompilationWithTasksExtensions(source, references: new[] { lib_ref });
            comp.MakeTypeMissing(type);
            comp.VerifyEmitDiagnostics(expected);
        }

        private CSharpCompilation CreateCompilationWithAsyncIterator(string source, CSharpCompilationOptions options = null)
            => CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: options);

        [Fact]
        [WorkItem(30566, "https://github.com/dotnet/roslyn/issues/30566")]
        public void AsyncIteratorBug30566()
        {
            var comp = CreateCompilationWithAsyncIterator(@"
using System;
class C
{
    public async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return await GetTemperatureAsync();
        yield return await GetTemperatureAsync();
    }

    private static Random CapturedRandom = new Random();

    public async System.Threading.Tasks.Task<int> GetTemperatureAsync()
    {
        await System.Threading.Tasks.Task.Delay(CapturedRandom.Next(1, 8));
        return CapturedRandom.Next(50, 100);
    }
}");
            CompileAndVerify(comp);
        }

        [Fact]
        [WorkItem(30566, "https://github.com/dotnet/roslyn/issues/30566")]
        public void YieldReturnAwait1()
        {
            var comp = CreateCompilationWithAsyncIterator(@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    public static async IAsyncEnumerable<int> M()
    {
        yield return await Task.FromResult(2);
        await Task.Delay(1);
        yield return await Task.FromResult(8);
    }
    public static async Task Main(string[] args)
    {
        await foreach (var i in M())
        {
            Console.WriteLine(i);
        }
    }
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
2
8");
        }

        [Fact]
        [WorkItem(30566, "https://github.com/dotnet/roslyn/issues/30566")]
        public void YieldReturnAwait2()
        {
            var comp = CreateCompilationWithAsyncIterator(@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    public static async IAsyncEnumerable<int> M(Task<Task<int>[]> arr)
    {
        foreach (var t in await arr)
        {
            yield return await t;
        }
    }
    public static async Task Main(string[] args)
    {
        var arr = new Task<int>[] {
            Task.FromResult(2),
            Task.FromResult(8)
        };
        await foreach (var i in M(Task.FromResult(arr)))
        {
            Console.WriteLine(i);
        }
    }
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
2
8");
            verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      362 (0x16a)
  .maxstack  3
  .locals init (int V_0,
                System.Threading.Tasks.Task<int>[] V_1,
                System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]> V_2,
                C.<M>d__0 V_3,
                int V_4,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_0054,
        IL_00cf,
        IL_0112)
    IL_0019:  ldarg.0
    IL_001a:  ldfld      ""System.Threading.Tasks.Task<System.Threading.Tasks.Task<int>[]> C.<M>d__0.arr""
    IL_001f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]> System.Threading.Tasks.Task<System.Threading.Tasks.Task<int>[]>.GetAwaiter()""
    IL_0024:  stloc.2
    IL_0025:  ldloca.s   V_2
    IL_0027:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]>.IsCompleted.get""
    IL_002c:  brtrue.s   IL_0070
    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.0
    IL_0030:  dup
    IL_0031:  stloc.0
    IL_0032:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0037:  ldarg.0
    IL_0038:  ldloc.2
    IL_0039:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]> C.<M>d__0.<>u__1""
    IL_003e:  ldarg.0
    IL_003f:  stloc.3
    IL_0040:  ldarg.0
    IL_0041:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__0.<>t__builder""
    IL_0046:  ldloca.s   V_2
    IL_0048:  ldloca.s   V_3
    IL_004a:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]>, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]>, ref C.<M>d__0)""
    IL_004f:  leave      IL_0169
    IL_0054:  ldarg.0
    IL_0055:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]> C.<M>d__0.<>u__1""
    IL_005a:  stloc.2
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]> C.<M>d__0.<>u__1""
    IL_0061:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]>""
    IL_0067:  ldarg.0
    IL_0068:  ldc.i4.m1
    IL_0069:  dup
    IL_006a:  stloc.0
    IL_006b:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0070:  ldloca.s   V_2
    IL_0072:  call       ""System.Threading.Tasks.Task<int>[] System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]>.GetResult()""
    IL_0077:  stloc.1
    IL_0078:  ldarg.0
    IL_0079:  ldloc.1
    IL_007a:  stfld      ""System.Threading.Tasks.Task<int>[] C.<M>d__0.<>7__wrap1""
    IL_007f:  ldarg.0
    IL_0080:  ldc.i4.0
    IL_0081:  stfld      ""int C.<M>d__0.<>7__wrap2""
    IL_0086:  br         IL_0120
    IL_008b:  ldarg.0
    IL_008c:  ldfld      ""System.Threading.Tasks.Task<int>[] C.<M>d__0.<>7__wrap1""
    IL_0091:  ldarg.0
    IL_0092:  ldfld      ""int C.<M>d__0.<>7__wrap2""
    IL_0097:  ldelem.ref
    IL_0098:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_009d:  stloc.s    V_5
    IL_009f:  ldloca.s   V_5
    IL_00a1:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00a6:  brtrue.s   IL_00ec
    IL_00a8:  ldarg.0
    IL_00a9:  ldc.i4.1
    IL_00aa:  dup
    IL_00ab:  stloc.0
    IL_00ac:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00b1:  ldarg.0
    IL_00b2:  ldloc.s    V_5
    IL_00b4:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<M>d__0.<>u__2""
    IL_00b9:  ldarg.0
    IL_00ba:  stloc.3
    IL_00bb:  ldarg.0
    IL_00bc:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__0.<>t__builder""
    IL_00c1:  ldloca.s   V_5
    IL_00c3:  ldloca.s   V_3
    IL_00c5:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<M>d__0)""
    IL_00ca:  leave      IL_0169
    IL_00cf:  ldarg.0
    IL_00d0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<M>d__0.<>u__2""
    IL_00d5:  stloc.s    V_5
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<M>d__0.<>u__2""
    IL_00dd:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00e3:  ldarg.0
    IL_00e4:  ldc.i4.m1
    IL_00e5:  dup
    IL_00e6:  stloc.0
    IL_00e7:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00ec:  ldloca.s   V_5
    IL_00ee:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00f3:  stloc.s    V_4
    IL_00f5:  ldarg.0
    IL_00f6:  ldloc.s    V_4
    IL_00f8:  stfld      ""int C.<M>d__0.<>2__current""
    IL_00fd:  ldarg.0
    IL_00fe:  ldc.i4.2
    IL_00ff:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0104:  ldarg.0
    IL_0105:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_010a:  ldc.i4.1
    IL_010b:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.SetResult(bool)""
    IL_0110:  leave.s    IL_0169
    IL_0112:  ldarg.0
    IL_0113:  ldarg.0
    IL_0114:  ldfld      ""int C.<M>d__0.<>7__wrap2""
    IL_0119:  ldc.i4.1
    IL_011a:  add
    IL_011b:  stfld      ""int C.<M>d__0.<>7__wrap2""
    IL_0120:  ldarg.0
    IL_0121:  ldfld      ""int C.<M>d__0.<>7__wrap2""
    IL_0126:  ldarg.0
    IL_0127:  ldfld      ""System.Threading.Tasks.Task<int>[] C.<M>d__0.<>7__wrap1""
    IL_012c:  ldlen
    IL_012d:  conv.i4
    IL_012e:  blt        IL_008b
    IL_0133:  ldarg.0
    IL_0134:  ldnull
    IL_0135:  stfld      ""System.Threading.Tasks.Task<int>[] C.<M>d__0.<>7__wrap1""
    IL_013a:  leave.s    IL_0155
  }
  catch System.Exception
  {
    IL_013c:  stloc.s    V_6
    IL_013e:  ldarg.0
    IL_013f:  ldc.i4.s   -2
    IL_0141:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0146:  ldarg.0
    IL_0147:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_014c:  ldloc.s    V_6
    IL_014e:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.SetException(System.Exception)""
    IL_0153:  leave.s    IL_0169
  }
  IL_0155:  ldarg.0
  IL_0156:  ldc.i4.s   -2
  IL_0158:  stfld      ""int C.<M>d__0.<>1__state""
  IL_015d:  ldarg.0
  IL_015e:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0163:  ldc.i4.0
  IL_0164:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.SetResult(bool)""
  IL_0169:  ret
}");
        }

        [Fact]
        public void AsyncIteratorInCSharp7_3()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 4;
        yield break;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (4,67): error CS8370: Feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M").WithArguments("async streams", "8.0").WithLocation(4, 67),
                // (4,67): error CS8370: Feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M").WithArguments("async streams", "8.0").WithLocation(4, 67)
                );
        }

        [Fact]
        public void RefStructElementType()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<S> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return new S();
    }
    static async System.Threading.Tasks.Task Main()
    {
        await foreach (var s in M())
        {
        }
    }
}
ref struct S
{
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (4,65): error CS0306: The type 'S' may not be used as a type argument
                //     static async System.Collections.Generic.IAsyncEnumerable<S> M()
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M").WithArguments("S").WithLocation(4, 65)
                );
        }

        [Fact]
        public void ReturningIAsyncEnumerable()
        {
            string source = @"
class C
{
    static System.Collections.Generic.IAsyncEnumerable<int> M2()
    {
        return M();
    }
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 42;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common });
            comp.VerifyDiagnostics();

            var m2 = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M2");
            Assert.False(m2.IsAsync);
            Assert.False(m2.IsIterator);

            var m = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            Assert.True(m.IsAsync);
            Assert.True(m.IsIterator);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AttributesSynthesized()
        {
            string source = @"
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 4;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.M");
                AssertEx.SetEqual(new[] { "AsyncStateMachineAttribute", "IteratorStateMachineAttribute" },
                    GetAttributeNames(method.GetAttributes()));
            });
        }

        [Fact]
        public void ReturnIAsyncEnumerator()
        {
            string source = @"
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerator<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 4;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common });
            comp.VerifyDiagnostics(
                // (4,74): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, or IAsyncEnumerable<T>
                //     public static async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "M").WithLocation(4, 74),
                // (4,74): error CS1624: The body of 'C.M()' cannot be an iterator block because 'IAsyncEnumerator<int>' is not an iterator interface type
                //     public static async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_BadIteratorReturn, "M").WithArguments("C.M()", "System.Collections.Generic.IAsyncEnumerator<int>").WithLocation(4, 74)
                );
        }

        [Fact]
        public void MissingTypeAndMembers_ManualResetValueTaskSourceLogic()
        {
            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__ctor,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1..ctor'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", ".ctor").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetResult,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.GetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "GetResult").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetStatus,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "GetStatus").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__get_Version,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.get_Version'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "get_Version").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__OnCompleted,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "OnCompleted").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__Reset,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.Reset'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "Reset").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetException,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.SetException'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "SetException").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetResult,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.SetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "SetResult").WithLocation(5, 64)
                );

            VerifyMissingType(WellKnownType.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1..ctor'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", ".ctor").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.GetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "GetResult").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "GetStatus").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.get_Version'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "get_Version").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "OnCompleted").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.Reset'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "Reset").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.SetException'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "SetException").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.SetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "SetResult").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingTypeAndMembers_IValueTaskSource()
        {
            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource`1.GetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource`1", "GetResult").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetStatus,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource`1.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource`1", "GetStatus").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__OnCompleted,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource`1.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource`1", "OnCompleted").WithLocation(5, 64)
                );

            VerifyMissingType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ValueTask`1..ctor'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ValueTask`1", ".ctor").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource`1.GetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource`1", "GetResult").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource`1.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource`1", "GetStatus").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource`1.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource`1", "OnCompleted").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingMember_AsyncVoidMethodBuilder()
        {
            VerifyMissingMember(WellKnownMember.System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Create,
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Create'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.AsyncVoidMethodBuilder", "Create").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingTypeAndMembers_IAsyncStateMachine()
        {
            VerifyMissingMember(WellKnownMember.System_Runtime_CompilerServices_IAsyncStateMachine_MoveNext,
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "MoveNext").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Runtime_CompilerServices_IAsyncStateMachine_SetStateMachine,
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "SetStateMachine").WithLocation(5, 64)
                );

            VerifyMissingType(WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetStateMachine'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.AsyncVoidMethodBuilder", "SetStateMachine").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "MoveNext").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "SetStateMachine").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingTypeAndMembers_IAsyncEnumerable()
        {
            VerifyMissingMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator,
                // (5,64): error CS0656: Missing compiler required member 'System.Collections.Generic.IAsyncEnumerable`1.GetAsyncEnumerator'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Collections.Generic.IAsyncEnumerable`1", "GetAsyncEnumerator").WithLocation(5, 64)
                );

            VerifyMissingType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T,
                // (5,60): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, or IAsyncEnumerable<T>
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "M").WithLocation(5, 60),
                // (5,60): error CS1624: The body of 'C.M()' cannot be an iterator block because 'IAsyncEnumerable<int>' is not an iterator interface type
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_BadIteratorReturn, "M").WithArguments("C.M()", "System.Collections.Generic.IAsyncEnumerable<int>").WithLocation(5, 60)
                );
        }

        [Fact]
        public void MissingType_ValueTaskSourceStatus()
        {
            VerifyMissingType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceStatus,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "GetStatus").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource`1.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource`1", "GetStatus").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingType_ValueTaskSourceOnCompletedFlags()
        {
            VerifyMissingType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", "OnCompleted").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource`1.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource`1", "OnCompleted").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingType_IAsyncIEnumerable_LocalFunction()
        {
            string source = @"
class C
{
    void Method()
    {
        _ = local();

        async System.Collections.Generic.IAsyncEnumerable<int> local()
        {
            await System.Threading.Tasks.Task.CompletedTask;
            yield return 3;
        }
    }
}
namespace System.Collections.Generic
{
    public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask<bool> MoveNextAsync();
        T Current { get; }
    }
}
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask DisposeAsync();
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source });
            comp.VerifyDiagnostics(
                // (8,42): error CS0234: The type or namespace name 'IAsyncEnumerable<>' does not exist in the namespace 'System.Collections.Generic' (are you missing an assembly reference?)
                //         async System.Collections.Generic.IAsyncEnumerable<int> local()
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IAsyncEnumerable<int>").WithArguments("IAsyncEnumerable<>", "System.Collections.Generic").WithLocation(8, 42),
                // (8,64): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, or IAsyncEnumerable<T>
                //         async System.Collections.Generic.IAsyncEnumerable<int> local()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "local").WithLocation(8, 64)
                );
        }

        [Fact]
        public void MissingTypeAndMembers_IAsyncEnumerator()
        {
            VerifyMissingMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync,
                // (5,64): error CS0656: Missing compiler required member 'System.Collections.Generic.IAsyncEnumerator`1.MoveNextAsync'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Collections.Generic.IAsyncEnumerator`1", "MoveNextAsync").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__get_Current,
                // (5,64): error CS0656: Missing compiler required member 'System.Collections.Generic.IAsyncEnumerator`1.get_Current'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Collections.Generic.IAsyncEnumerator`1", "get_Current").WithLocation(5, 64)
                );

            VerifyMissingType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T,
                // (5,64): error CS0656: Missing compiler required member 'System.Collections.Generic.IAsyncEnumerable`1.GetAsyncEnumerator'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Collections.Generic.IAsyncEnumerable`1", "GetAsyncEnumerator").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Collections.Generic.IAsyncEnumerator`1.MoveNextAsync'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Collections.Generic.IAsyncEnumerator`1", "MoveNextAsync").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Collections.Generic.IAsyncEnumerator`1.get_Current'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Collections.Generic.IAsyncEnumerator`1", "get_Current").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingTypeAndMembers_IAsyncDisposable()
        {
            VerifyMissingMember(WellKnownMember.System_IAsyncDisposable__DisposeAsync,
                // (5,64): error CS0656: Missing compiler required member 'System.IAsyncDisposable.DisposeAsync'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.IAsyncDisposable", "DisposeAsync").WithLocation(5, 64)
                );

            VerifyMissingType(WellKnownType.System_IAsyncDisposable,
                // (5,64): error CS0656: Missing compiler required member 'System.IAsyncDisposable.DisposeAsync'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.IAsyncDisposable", "DisposeAsync").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingTypeAndMembers_ValueTask()
        {
            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctor,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ValueTask`1..ctor'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ValueTask`1", ".ctor").WithLocation(5, 64)
                );

            VerifyMissingType(WellKnownType.System_Threading_Tasks_ValueTask_T,
                // (5,64): error CS0656: Missing compiler required member 'System.Collections.Generic.IAsyncEnumerator`1.MoveNextAsync'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Collections.Generic.IAsyncEnumerator`1", "MoveNextAsync").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ValueTask`1..ctor'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ValueTask`1", ".ctor").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingTypeAndMembers_IStrongBox()
        {
            VerifyMissingMember(WellKnownMember.System_Runtime_CompilerServices_IStrongBox_T__get_Value,
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IStrongBox`1.get_Value'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.IStrongBox`1", "get_Value").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Runtime_CompilerServices_IStrongBox_T__Value,
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IStrongBox`1.Value'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.IStrongBox`1", "Value").WithLocation(5, 64)
                );

            VerifyMissingType(WellKnownType.System_Runtime_CompilerServices_IStrongBox_T,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ManualResetValueTaskSourceLogic`1..ctor'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ManualResetValueTaskSourceLogic`1", ".ctor").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IStrongBox`1.get_Value'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.IStrongBox`1", "get_Value").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IStrongBox`1.Value'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.IStrongBox`1", "Value").WithLocation(5, 64)
                );
        }

        [Fact]
        public void AsyncIteratorWithBreak()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 3;
        break;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common });
            comp.VerifyDiagnostics(
                // (8,9): error CS0139: No enclosing loop out of which to break or continue
                //         break;
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "break;").WithLocation(8, 9)
                );
        }

        [Fact]
        public void AsyncIteratorWithReturnInt()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 3;
        return 1;
    }
    async System.Collections.Generic.IAsyncEnumerable<int> M2()
    {
        return 4;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common });
            comp.VerifyDiagnostics(
                // (8,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return 1;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(8, 9),
                // (10,60): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async System.Collections.Generic.IAsyncEnumerable<int> M2()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M2").WithLocation(10, 60),
                // (12,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return 4;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(12, 9)
                );
        }

        [Fact]
        public void AsyncIteratorWithReturnNull()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 3;
        return null;
    }
    async System.Collections.Generic.IAsyncEnumerable<int> M2()
    {
        return null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common });
            comp.VerifyDiagnostics(
                // (8,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return null;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(8, 9),
                // (10,60): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async System.Collections.Generic.IAsyncEnumerable<int> M2()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M2").WithLocation(10, 60),
                // (12,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return null;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(12, 9)
                );
        }

        [Fact]
        public void AsyncIteratorWithReturnRef()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M(ref string s)
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 3;
        return ref s;
    }
    async System.Collections.Generic.IAsyncEnumerable<int> M2(ref string s2)
    {
        return ref s2;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common });
            comp.VerifyDiagnostics(
                // (4,73): error CS1988: Async methods cannot have ref, in or out parameters
                //     async System.Collections.Generic.IAsyncEnumerable<int> M(ref string s)
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "s").WithLocation(4, 73),
                // (4,73): error CS1623: Iterators cannot have ref, in or out parameters
                //     async System.Collections.Generic.IAsyncEnumerable<int> M(ref string s)
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "s").WithLocation(4, 73),
                // (8,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return ref s;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(8, 9),
                // (10,60): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async System.Collections.Generic.IAsyncEnumerable<int> M2(ref string s2)
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M2").WithLocation(10, 60),
                // (10,74): error CS1988: Async methods cannot have ref, in or out parameters
                //     async System.Collections.Generic.IAsyncEnumerable<int> M2(ref string s2)
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "s2").WithLocation(10, 74),
                // (12,9): error CS8149: By-reference returns may only be used in methods that return by reference
                //         return ref s2;
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(12, 9)
                );
        }

        [Fact]
        public void AsyncIteratorWithReturnDefault()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 3;
        return default;
    }
    async System.Collections.Generic.IAsyncEnumerable<int> M2()
    {
        return default;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common });
            comp.VerifyDiagnostics(
                // (8,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return default;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(8, 9),
                // (10,60): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async System.Collections.Generic.IAsyncEnumerable<int> M2()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M2").WithLocation(10, 60),
                // (12,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return default;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(12, 9)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void CallingMoveNextAsyncTwice()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield return 3;
        Write(""4 "");
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write(""0 "");
        await using (var enumerator = M().GetAsyncEnumerator())
        {
            var found = await enumerator.MoveNextAsync();
            if (!found) throw null;
            var value = enumerator.Current;
            Write($""{value} "");
            found = await enumerator.MoveNextAsync();
            if (found) throw null;
            found = await enumerator.MoveNextAsync();
            if (found) throw null;
            Write(""5"");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 5");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void AsyncIteratorWithAwaitCompletedAndYield()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield return 3;
        Write("" 4 "");
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write(""0 "");
        await foreach (var i in M())
        {
            Write(i);
        }
        Write(""5"");
    }
}";
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: options);
                comp.VerifyDiagnostics();
                var verifier = CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 5");

                verifier.VerifyIL("C.M", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (C.<M>d__0 V_0)
  IL_0000:  newobj     ""C.<M>d__0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Create()""
  IL_000c:  stfld      ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__0.<>t__builder""
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.m1
  IL_0013:  stfld      ""int C.<M>d__0.<>1__state""
  IL_0018:  ldloc.0
  IL_0019:  ldloc.0
  IL_001a:  newobj     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>..ctor(System.Runtime.CompilerServices.IStrongBox<System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>>)""
  IL_001f:  stfld      ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0024:  ldloc.0
  IL_0025:  ret
}", sequencePoints: "C.M", source: source);

                verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<int>.get_Current()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>2__current""
  IL_0006:  ret
}");
                verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()", @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (C.<M>d__0 V_0,
                System.Threading.Tasks.ValueTask<bool> V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0014
  IL_000a:  ldloca.s   V_1
  IL_000c:  initobj    ""System.Threading.Tasks.ValueTask<bool>""
  IL_0012:  ldloc.1
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_001a:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.Reset()""
  IL_001f:  ldarg.0
  IL_0020:  stloc.0
  IL_0021:  ldarg.0
  IL_0022:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__0.<>t__builder""
  IL_0027:  ldloca.s   V_0
  IL_0029:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<C.<M>d__0>(ref C.<M>d__0)""
  IL_002e:  ldarg.0
  IL_002f:  ldarg.0
  IL_0030:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0035:  call       ""short System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.Version.get""
  IL_003a:  newobj     ""System.Threading.Tasks.ValueTask<bool>..ctor(System.Threading.Tasks.Sources.IValueTaskSource<bool>, short)""
  IL_003f:  ret
}");
                verifier.VerifyIL("C.<M>d__0.System.IAsyncDisposable.DisposeAsync()", @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (System.Threading.Tasks.ValueTask V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__0.<>t__builder""
  IL_0006:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_000b:  ldarg.0
  IL_000c:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0011:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.Reset()""
  IL_0016:  ldarg.0
  IL_0017:  ldc.i4.m1
  IL_0018:  stfld      ""int C.<M>d__0.<>1__state""
  IL_001d:  ldloca.s   V_0
  IL_001f:  initobj    ""System.Threading.Tasks.ValueTask""
  IL_0025:  ldloc.0
  IL_0026:  ret
}
");
                verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IStrongBox<System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>>.get_Value()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0006:  ret
}");
                verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator()", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                verifier.VerifyIL("C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<bool>.GetResult(short)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0006:  ldarg.1
  IL_0007:  call       ""bool System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.GetResult(short)""
  IL_000c:  ret
}");
                verifier.VerifyIL("C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<bool>.GetStatus(short)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0006:  ldarg.1
  IL_0007:  call       ""System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.GetStatus(short)""
  IL_000c:  ret
}");
                verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine)", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
                verifier.VerifyIL("C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<bool>.OnCompleted(System.Action<object>, object, short, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags)", @"
{
  // Code size       17 (0x11)
  .maxstack  5
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0006:  ldarg.1
  IL_0007:  ldarg.2
  IL_0008:  ldarg.3
  IL_0009:  ldarg.s    V_4
  IL_000b:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.OnCompleted(System.Action<object>, object, short, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags)""
  IL_0010:  ret
}");
                if (options == TestOptions.DebugExe)
                {
                    verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      231 (0xe7)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<M>d__0 V_2,
                System.Exception V_3)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_0060
    IL_0014:  br         IL_00ac
    // sequence point: {
    IL_0019:  nop
    // sequence point: Write(""1 "");
    IL_001a:  ldstr      ""1 ""
    IL_001f:  call       ""void System.Console.Write(string)""
    IL_0024:  nop
    // sequence point: await System.Threading.Tasks.Task.CompletedTask;
    IL_0025:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get""
    IL_002a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_002f:  stloc.1
    // sequence point: <hidden>
    IL_0030:  ldloca.s   V_1
    IL_0032:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0037:  brtrue.s   IL_007c
    IL_0039:  ldarg.0
    IL_003a:  ldc.i4.0
    IL_003b:  dup
    IL_003c:  stloc.0
    IL_003d:  stfld      ""int C.<M>d__0.<>1__state""
    // async: yield
    IL_0042:  ldarg.0
    IL_0043:  ldloc.1
    IL_0044:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0049:  ldarg.0
    IL_004a:  stloc.2
    IL_004b:  ldarg.0
    IL_004c:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__0.<>t__builder""
    IL_0051:  ldloca.s   V_1
    IL_0053:  ldloca.s   V_2
    IL_0055:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)""
    IL_005a:  nop
    IL_005b:  leave      IL_00e6
    // async: resume
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0066:  stloc.1
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_006d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0073:  ldarg.0
    IL_0074:  ldc.i4.m1
    IL_0075:  dup
    IL_0076:  stloc.0
    IL_0077:  stfld      ""int C.<M>d__0.<>1__state""
    IL_007c:  ldloca.s   V_1
    IL_007e:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0083:  nop
    // sequence point: Write(""2 "");
    IL_0084:  ldstr      ""2 ""
    IL_0089:  call       ""void System.Console.Write(string)""
    IL_008e:  nop
    // sequence point: yield return 3;
    IL_008f:  ldarg.0
    IL_0090:  ldc.i4.3
    IL_0091:  stfld      ""int C.<M>d__0.<>2__current""
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  stfld      ""int C.<M>d__0.<>1__state""
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_00a3:  ldc.i4.1
    IL_00a4:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.SetResult(bool)""
    IL_00a9:  nop
    IL_00aa:  leave.s    IL_00e6
    // sequence point: Write("" 4 "");
    IL_00ac:  ldstr      "" 4 ""
    IL_00b1:  call       ""void System.Console.Write(string)""
    IL_00b6:  nop
    IL_00b7:  leave.s    IL_00d1
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_00b9:  stloc.3
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.s   -2
    IL_00bd:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00c2:  ldarg.0
    IL_00c3:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_00c8:  ldloc.3
    IL_00c9:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.SetException(System.Exception)""
    IL_00ce:  nop
    IL_00cf:  leave.s    IL_00e6
  }
  // sequence point: }
  IL_00d1:  ldarg.0
  IL_00d2:  ldc.i4.s   -2
  IL_00d4:  stfld      ""int C.<M>d__0.<>1__state""
  // sequence point: <hidden>
  IL_00d9:  ldarg.0
  IL_00da:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_00df:  ldc.i4.0
  IL_00e0:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.SetResult(bool)""
  IL_00e5:  nop
  IL_00e6:  ret
}", sequencePoints: "C+<M>d__0.MoveNext", source: source);
                }
                else
                {
                    verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      214 (0xd6)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<M>d__0 V_2,
                System.Exception V_3)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0055
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_009e
    // sequence point: Write(""1 "");
    IL_0011:  ldstr      ""1 ""
    IL_0016:  call       ""void System.Console.Write(string)""
    // sequence point: await System.Threading.Tasks.Task.CompletedTask;
    IL_001b:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get""
    IL_0020:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0025:  stloc.1
    // sequence point: <hidden>
    IL_0026:  ldloca.s   V_1
    IL_0028:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_002d:  brtrue.s   IL_0071
    IL_002f:  ldarg.0
    IL_0030:  ldc.i4.0
    IL_0031:  dup
    IL_0032:  stloc.0
    IL_0033:  stfld      ""int C.<M>d__0.<>1__state""
    // async: yield
    IL_0038:  ldarg.0
    IL_0039:  ldloc.1
    IL_003a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_003f:  ldarg.0
    IL_0040:  stloc.2
    IL_0041:  ldarg.0
    IL_0042:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<M>d__0.<>t__builder""
    IL_0047:  ldloca.s   V_1
    IL_0049:  ldloca.s   V_2
    IL_004b:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)""
    IL_0050:  leave      IL_00d5
    // async: resume
    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_005b:  stloc.1
    IL_005c:  ldarg.0
    IL_005d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0062:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0068:  ldarg.0
    IL_0069:  ldc.i4.m1
    IL_006a:  dup
    IL_006b:  stloc.0
    IL_006c:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0071:  ldloca.s   V_1
    IL_0073:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    // sequence point: Write(""2 "");
    IL_0078:  ldstr      ""2 ""
    IL_007d:  call       ""void System.Console.Write(string)""
    // sequence point: yield return 3;
    IL_0082:  ldarg.0
    IL_0083:  ldc.i4.3
    IL_0084:  stfld      ""int C.<M>d__0.<>2__current""
    IL_0089:  ldarg.0
    IL_008a:  ldc.i4.1
    IL_008b:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0090:  ldarg.0
    IL_0091:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_0096:  ldc.i4.1
    IL_0097:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.SetResult(bool)""
    IL_009c:  leave.s    IL_00d5
    // sequence point: Write("" 4 "");
    IL_009e:  ldstr      "" 4 ""
    IL_00a3:  call       ""void System.Console.Write(string)""
    IL_00a8:  leave.s    IL_00c1
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_00aa:  stloc.3
    IL_00ab:  ldarg.0
    IL_00ac:  ldc.i4.s   -2
    IL_00ae:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00b3:  ldarg.0
    IL_00b4:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_00b9:  ldloc.3
    IL_00ba:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.SetException(System.Exception)""
    IL_00bf:  leave.s    IL_00d5
  }
  // sequence point: }
  IL_00c1:  ldarg.0
  IL_00c2:  ldc.i4.s   -2
  IL_00c4:  stfld      ""int C.<M>d__0.<>1__state""
  // sequence point: <hidden>
  IL_00c9:  ldarg.0
  IL_00ca:  ldflda     ""System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_00cf:  ldc.i4.0
  IL_00d0:  call       ""void System.Threading.Tasks.ManualResetValueTaskSourceLogic<bool>.SetResult(bool)""
  IL_00d5:  ret
}", sequencePoints: "C+<M>d__0.MoveNext", source: source);
                }
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithGenericReturn()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<T> M<T>(T value)
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield return value;
        Write("" 4 "");
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write(""0 "");
        await foreach (var i in M(3))
        {
            Write(i);
        }
        Write(""5"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 5");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithGenericReturnFromContainingType()
        {
            string source = @"
using static System.Console;
public class C<T>
{
    public static async System.Collections.Generic.IAsyncEnumerable<T> M(T value)
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield return value;
        Write("" 4 "");
    }
}
class D
{
    static async System.Threading.Tasks.Task Main()
    {
        Write(""0 "");
        await foreach (var i in C<int>.M(3))
        {
            Write(i);
        }
        Write(""5"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 5");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithParameter()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M(int parameter)
    {
        Write($""p:{parameter} "");
        parameter++;
        await System.Threading.Tasks.Task.Delay(10);
        Write($""p:{parameter} "");
        parameter++;
        yield return 42;
        Write($""p:{parameter} "");
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write(""Start "");
        await foreach (var i in M(10))
        {
            Write(""Value "");
        }
        Write(""End"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Start p:10 p:11 Value p:12 End");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithThis()
        {
            string source = @"
using static System.Console;
class C
{
    int field = 10;
    async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write($""f:{this.field} "");
        this.field++;
        await System.Threading.Tasks.Task.Delay(10);
        Write($""f:{this.field} "");
        this.field++;
        yield return 42;
        Write($""f:{this.field} "");
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write(""Start "");
        await foreach (var i in new C().M())
        {
            Write(""Value "");
        }
        Write(""End"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "Start f:10 f:11 Value f:12 End");
        }

        [Fact]
        public void AsyncIteratorWithReturn()
        {
            string source = @"
class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 3;
        return null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common });
            comp.VerifyDiagnostics(
                // (8,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return null;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(8, 9)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithAwaitCompletedAndOneYieldAndOneInvocation()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield return 3;
        Write(""4 "");
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write(""0 "");
        await foreach (var i in M())
        {
            Write($""{i} "");
        }
        Write(""Done"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 Done");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithAwaitCompletedAndTwoYields()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield return 3;
        Write(""4 "");
        yield return 5;
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write(""0 "");
        await foreach (var i in M())
        {
            Write($""{i} "");
        }
        Write(""Done"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 5 Done");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithYieldAndAwait()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write(""1 "");
        yield return 2;
        Write(""3 "");
        await System.Threading.Tasks.Task.Delay(10);
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write(""0 "");
        await foreach (var i in M())
        {
            Write($""{i} "");
        }
        Write(""Done"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 Done");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithAwaitCompletedAndYieldBreak()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield break;
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write(""0 "");
        await foreach (var i in M())
        {
            Write($""{i} "");
        }
        Write(""Done"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 Done");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithAwaitCompletedAndYieldBreakAndYieldReturn()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        goto label2;
label1:
        yield break;
label2:
        yield return 3;
        goto label1;
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write(""0 "");
        await foreach (var i in M())
        {
            Write($""{i} "");
        }
        Write(""Done"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 Done");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithCustomCode()
        {
            verify(new[] { AwaitSlow, Write, Yield, AwaitSlow });
            verify(new[] { AwaitSlow, Write, Yield, Yield });
            verify(new[] { Write, Yield, Write, AwaitFast, Yield });
            verify(new[] { Yield, Write, AwaitFast, Yield });
            verify(new[] { AwaitFast, YieldBreak });
            verify(new[] { AwaitSlow, YieldBreak });
            verify(new[] { AwaitSlow, Yield, YieldBreak });

            void verify(Instruction[] spec)
            {
                verifyMethod(spec);
                verifyLocalFunction(spec);
            }

            void verifyMethod(Instruction[] spec)
            {
                (string code, string expectation) = generateCode(spec);

                string source = $@"
using static System.Console;
class C
{{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {{
        {code}
    }}
    static async System.Threading.Tasks.Task Main()
    {{
        Write(""0 "");
        await foreach (var i in M())
        {{
            Write($""{{i}} "");
        }}
        Write(""Done"");
    }}
}}";
                var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
                comp.VerifyDiagnostics();
                var verifier = CompileAndVerify(comp, expectedOutput: expectation);
            }

            void verifyLocalFunction(Instruction[] spec)
            {
                (string code, string expectation) = generateCode(spec);

                string source = $@"
using static System.Console;
class C
{{
    static async System.Threading.Tasks.Task Main()
    {{
        Write(""0 "");
        await foreach (var i in local())
        {{
            Write($""{{i}} "");
        }}
        Write(""Done"");

        async System.Collections.Generic.IAsyncEnumerable<int> local()
        {{
            {code}
        }}
    }}
}}";
                var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
                comp.VerifyDiagnostics();
                var verifier = CompileAndVerify(comp, expectedOutput: expectation);
            }

            (string code, string expectation) generateCode(Instruction[] spec)
            {
                var builder = new StringBuilder();
                var expectationBuilder = new StringBuilder();
                int counter = 1;
                expectationBuilder.Append("0 ");

                foreach (var instruction in spec)
                {
                    switch (instruction)
                    {
                        case Write:
                            //Write(""N "");
                            builder.AppendLine($@"Write(""{counter} "");");
                            expectationBuilder.Append($"{counter} ");
                            counter++;
                            break;
                        case Yield:
                            //yield return N;
                            builder.AppendLine($@"yield return {counter};");
                            expectationBuilder.Append($"{counter} ");
                            counter++;
                            break;
                        case AwaitSlow:
                            //await System.Threading.Tasks.Task.Delay(10);
                            builder.AppendLine("await System.Threading.Tasks.Task.Delay(10);");
                            break;
                        case AwaitFast:
                            //await new System.Threading.Tasks.Task.CompletedTask;
                            builder.AppendLine("await System.Threading.Tasks.Task.CompletedTask;");
                            break;
                        case YieldBreak:
                            //yield break;
                            builder.AppendLine($@"yield break;");
                            break;
                    }
                }
                expectationBuilder.Append("Done");
                return (builder.ToString(), expectationBuilder.ToString());
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithAwaitAndYieldAndAwait()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.Delay(10);
        Write(""2 "");
        yield return 3;
        Write(""4 "");
        await System.Threading.Tasks.Task.Delay(10);
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write(""0 "");
        await foreach (var i in M())
        {
            Write($""{i} "");
        }
        Write(""Done"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 Done");
        }

        [Fact]
        public void AsyncIteratorWithAwaitOnly()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common });
            comp.VerifyDiagnostics(
                // (4,60): error CS0161: 'C.M()': not all code paths return a value
                //     async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M()").WithLocation(4, 60)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithYieldReturnOnly()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
    }
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in M())
        {
            System.Console.Write(i);
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (4,67): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 67)
                );
            CompileAndVerify(comp, expectedOutput: "1");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void AsyncIteratorWithYieldBreakOnly()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield break;
    }
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in M())
        {
            System.Console.Write(""SKIPPED"");
        }
        System.Console.Write(""none"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (4,67): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 67)
                );
            CompileAndVerify(comp, expectedOutput: "none");
        }

        [Fact]
        public void AsyncIteratorWithoutAwaitOrYield()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common });
            comp.VerifyDiagnostics(
                // (4,60): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 60),
                // (4,60): error CS0161: 'C.M()': not all code paths return a value
                //     async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M()").WithLocation(4, 60)
                );
        }

        [Fact]
        public void TestBadReturnValue()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return ""hello"";
        yield return;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_common });
            comp.VerifyDiagnostics(
                // (7,15): error CS1627: Expression expected after yield return
                //         yield return;
                Diagnostic(ErrorCode.ERR_EmptyYield, "return").WithLocation(7, 15),
                // (6,22): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         yield return "hello";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "int").WithLocation(6, 22),
                // (4,60): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 60)
                );
        }

        private static readonly string s_common = @"
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator();
    }

    public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask<bool> MoveNextAsync();
        T Current { get; }
    }
}
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask DisposeAsync();
    }
}

namespace System.Runtime.CompilerServices
{
    public interface IStrongBox<T>
    {
        ref T Value { get; }
    }
}

namespace System.Threading.Tasks
{
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks.Sources;

    public struct ManualResetValueTaskSourceLogic<TResult>
    {
        private static readonly Action<object> s_sentinel = new Action<object>(s => throw new InvalidOperationException());

        private readonly IStrongBox<ManualResetValueTaskSourceLogic<TResult>> _parent;
        private Action<object> _continuation;
        private object _continuationState;
        private object _capturedContext;
        private ExecutionContext _executionContext;
        private bool _completed;
        private TResult _result;
        private ExceptionDispatchInfo _error;
        private short _version;

        public ManualResetValueTaskSourceLogic(IStrongBox<ManualResetValueTaskSourceLogic<TResult>> parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _continuation = null;
            _continuationState = null;
            _capturedContext = null;
            _executionContext = null;
            _completed = false;
            _result = default;
            _error = null;
            _version = 0;
        }

        public short Version => _version;

        private void ValidateToken(short token)
        {
            if (token != _version)
            {
                throw new InvalidOperationException();
            }
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            ValidateToken(token);

            return
                !_completed ? ValueTaskSourceStatus.Pending :
                _error == null ? ValueTaskSourceStatus.Succeeded :
                _error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
                ValueTaskSourceStatus.Faulted;
        }

        public TResult GetResult(short token)
        {
            ValidateToken(token);

            if (!_completed)
            {
                throw new InvalidOperationException();
            }

            _error?.Throw();
            return _result;
        }

        public void Reset()
        {
            _version++;

            _completed = false;
            _continuation = null;
            _continuationState = null;
            _result = default;
            _error = null;
            _executionContext = null;
            _capturedContext = null;
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }
            ValidateToken(token);

            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                _executionContext = ExecutionContext.Capture();
            }

            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                SynchronizationContext sc = SynchronizationContext.Current;
                if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    _capturedContext = sc;
                }
                else
                {
                    TaskScheduler ts = TaskScheduler.Current;
                    if (ts != TaskScheduler.Default)
                    {
                        _capturedContext = ts;
                    }
                }
            }

            _continuationState = state;
            if (Interlocked.CompareExchange(ref _continuation, continuation, null) != null)
            {
                _executionContext = null;

                object cc = _capturedContext;
                _capturedContext = null;

                switch (cc)
                {
                    case null:
                        Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                        break;

                    case SynchronizationContext sc:
                        sc.Post(s =>
                        {
                            var tuple = (Tuple<Action<object>, object>)s;
                            tuple.Item1(tuple.Item2);
                        }, Tuple.Create(continuation, state));
                        break;

                    case TaskScheduler ts:
                        Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                        break;
                }
            }
        }

        public void SetResult(TResult result)
        {
            _result = result;
            SignalCompletion();
        }

        public void SetException(Exception error)
        {
            _error = ExceptionDispatchInfo.Capture(error);
            SignalCompletion();
        }

        private void SignalCompletion()
        {
            if (_completed)
            {
                throw new InvalidOperationException();
            }
            _completed = true;

            if (Interlocked.CompareExchange(ref _continuation, s_sentinel, null) != null)
            {
                if (_executionContext != null)
                {
                    ExecutionContext.Run(
                        _executionContext,
                        s => ((IStrongBox<ManualResetValueTaskSourceLogic<TResult>>)s).Value.InvokeContinuation(),
                        _parent ?? throw new InvalidOperationException());
                }
                else
                {
                    InvokeContinuation();
                }
            }
        }

        private void InvokeContinuation()
        {
            object cc = _capturedContext;
            _capturedContext = null;

            switch (cc)
            {
                case null:
                    _continuation(_continuationState);
                    break;

                case SynchronizationContext sc:
                    sc.Post(s =>
                    {
                        ref ManualResetValueTaskSourceLogic<TResult> logicRef = ref ((IStrongBox<ManualResetValueTaskSourceLogic<TResult>>)s).Value;
                        logicRef._continuation(logicRef._continuationState);
                    }, _parent ?? throw new InvalidOperationException());
                    break;

                case TaskScheduler ts:
                    Task.Factory.StartNew(_continuation, _continuationState, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                    break;
            }
        }
    }
}
";

        [Fact]
        public void TestWellKnownMembers()
        {
            var comp = CreateCompilation(s_common, references: new[] { TestReferences.NetStandard20.TasksExtensionsRef }, targetFramework: Roslyn.Test.Utilities.TargetFramework.NetStandard20);
            comp.VerifyDiagnostics();

            verifyType(WellKnownType.System_Runtime_CompilerServices_IStrongBox_T,
                "System.Runtime.CompilerServices.IStrongBox<T>");

            verifyMember(WellKnownMember.System_Runtime_CompilerServices_IStrongBox_T__Value,
                "ref T System.Runtime.CompilerServices.IStrongBox<T>.Value { get; }");

            verifyMember(WellKnownMember.System_Runtime_CompilerServices_IStrongBox_T__get_Value,
                "ref T System.Runtime.CompilerServices.IStrongBox<T>.Value.get");

            verifyType(WellKnownType.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T,
                "System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__ctor,
                "System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>..ctor(System.Runtime.CompilerServices.IStrongBox<System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>> parent)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetResult,
                "TResult System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.GetResult(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetStatus,
                "System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.GetStatus(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__OnCompleted,
                "void System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__Reset,
                "void System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.Reset()");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetException,
                "void System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.SetException(System.Exception error)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetResult,
                "void System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.SetResult(TResult result)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__get_Version,
                "System.Int16 System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.Version.get");

            verifyType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceStatus,
                "System.Threading.Tasks.Sources.ValueTaskSourceStatus");

            verifyType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags,
                "System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags");

            verifyType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T,
                "System.Threading.Tasks.Sources.IValueTaskSource<out TResult>");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult,
                "TResult System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.GetResult(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetStatus,
                "System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.GetStatus(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__OnCompleted,
                "void System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)");

            verifyType(WellKnownType.System_Threading_Tasks_ValueTask_T,
                "System.Threading.Tasks.ValueTask<TResult>");

            verifyMember(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctor,
                "System.Threading.Tasks.ValueTask<TResult>..ctor(System.Threading.Tasks.Sources.IValueTaskSource<TResult> source, System.Int16 token)");

            void verifyType(WellKnownType type, string expected)
            {
                var symbol = comp.GetWellKnownType(type);
                Assert.Equal(expected, symbol.ToTestDisplayString());
            }

            void verifyMember(WellKnownMember member, string expected)
            {
                var symbol = comp.GetWellKnownTypeMember(member);
                Assert.Equal(expected, symbol.ToTestDisplayString());
            }
        }

        [Fact]
        public void AsyncLocalFunctionWithUnknownReturnType()
        {
            string source = @"
class C
{
    void Method()
    {
        _ = local();

        async Unknown local()
        {
            await System.Threading.Tasks.Task.CompletedTask;
            yield return 3;
        }
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source });
            comp.VerifyDiagnostics(
                // (8,15): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //         async Unknown local()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(8, 15),
                // (8,23): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, or IAsyncEnumerable<T>
                //         async Unknown local()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "local").WithLocation(8, 23)
                );
        }
    }
}
