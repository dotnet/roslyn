// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.Instruction;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
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
        /// <summary>
        /// Enumerates `C.M()` a given number of iterations.
        /// </summary>
        private static CSharpTestSource Run(int iterations)
        {
            string _runner = @"
using static System.Console;
class D
{
    static async System.Threading.Tasks.Task Main()
    {
        var enumerator = C.M().GetAsyncEnumerator();

        try
        {
            for (int i = 0; i < ITERATIONS; i++)
            {
                if (!await enumerator.MoveNextAsync())
                {
                    Write(""END "");
                    break;
                }
                Write($""{enumerator.Current} "");
            }
        }
        catch
        {
            Write(""CAUGHT "");
        }
        finally
        {
            Write(""DISPOSAL "");

            try
            {
                await enumerator.DisposeAsync();
            }
            catch
            {
                Write(""CAUGHT2 "");
            }
        }
        Write(""DONE"");
    }
}
";
            return _runner.Replace("ITERATIONS", iterations.ToString());
        }

        private const string _enumerable = @"
using System.Threading.Tasks;
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
}
";
        private const string _enumerator = @"
using System.Threading.Tasks;
class C
{
    async System.Collections.Generic.IAsyncEnumerator<int> M() { await Task.CompletedTask; yield return 3; }
}
";
        private static void VerifyMissingMember(WellKnownMember member, params DiagnosticDescription[] expected)
        {
            foreach (var source in new[] { _enumerable, _enumerator })
            {
                VerifyMissingMember(source, member, expected);
            }
        }

        private static void VerifyMissingMember(string source, WellKnownMember member, params DiagnosticDescription[] expected)
        {
            var lib = CreateCompilationWithTasksExtensions(AsyncStreamsTypes);
            var lib_ref = lib.EmitToImageReference();
            var comp = CreateCompilationWithTasksExtensions(source, references: new[] { lib_ref });
            comp.MakeMemberMissing(member);
            comp.VerifyEmitDiagnostics(expected);
        }

        private static void VerifyMissingType(WellKnownType type, params DiagnosticDescription[] expected)
        {
            foreach (var source in new[] { _enumerable, _enumerator })
            {
                VerifyMissingType(source, type, expected);
            }
        }

        private static void VerifyMissingType(string source, WellKnownType type, params DiagnosticDescription[] expected)
        {
            var lib = CreateCompilationWithTasksExtensions(AsyncStreamsTypes);
            var lib_ref = lib.EmitToImageReference();
            var comp = CreateCompilationWithTasksExtensions(source, references: new[] { lib_ref });
            comp.MakeTypeMissing(type);
            comp.VerifyEmitDiagnostics(expected);
        }

        private CSharpCompilation CreateCompilationWithAsyncIterator(CSharpTestSource source, CSharpCompilationOptions options = null)
            => CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: options);

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
  // Code size      384 (0x180)
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
        IL_0108)
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
    IL_0041:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_0046:  ldloca.s   V_2
    IL_0048:  ldloca.s   V_3
    IL_004a:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]>, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.Task<int>[]>, ref C.<M>d__0)""
    IL_004f:  leave      IL_017f
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
    IL_0086:  br         IL_0129
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
    IL_00bc:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_00c1:  ldloca.s   V_5
    IL_00c3:  ldloca.s   V_3
    IL_00c5:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<M>d__0)""
    IL_00ca:  leave      IL_017f
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
    IL_00ff:  dup
    IL_0100:  stloc.0
    IL_0101:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0106:  leave.s    IL_0173
    IL_0108:  ldarg.0
    IL_0109:  ldc.i4.m1
    IL_010a:  dup
    IL_010b:  stloc.0
    IL_010c:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0111:  ldarg.0
    IL_0112:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_0117:  brfalse.s  IL_011b
    IL_0119:  leave.s    IL_015e
    IL_011b:  ldarg.0
    IL_011c:  ldarg.0
    IL_011d:  ldfld      ""int C.<M>d__0.<>7__wrap2""
    IL_0122:  ldc.i4.1
    IL_0123:  add
    IL_0124:  stfld      ""int C.<M>d__0.<>7__wrap2""
    IL_0129:  ldarg.0
    IL_012a:  ldfld      ""int C.<M>d__0.<>7__wrap2""
    IL_012f:  ldarg.0
    IL_0130:  ldfld      ""System.Threading.Tasks.Task<int>[] C.<M>d__0.<>7__wrap1""
    IL_0135:  ldlen
    IL_0136:  conv.i4
    IL_0137:  blt        IL_008b
    IL_013c:  ldarg.0
    IL_013d:  ldnull
    IL_013e:  stfld      ""System.Threading.Tasks.Task<int>[] C.<M>d__0.<>7__wrap1""
    IL_0143:  leave.s    IL_015e
  }
  catch System.Exception
  {
    IL_0145:  stloc.s    V_6
    IL_0147:  ldarg.0
    IL_0148:  ldc.i4.s   -2
    IL_014a:  stfld      ""int C.<M>d__0.<>1__state""
    IL_014f:  ldarg.0
    IL_0150:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_0155:  ldloc.s    V_6
    IL_0157:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_015c:  leave.s    IL_017f
  }
  IL_015e:  ldarg.0
  IL_015f:  ldc.i4.s   -2
  IL_0161:  stfld      ""int C.<M>d__0.<>1__state""
  IL_0166:  ldarg.0
  IL_0167:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_016c:  ldc.i4.0
  IL_016d:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0172:  ret
  IL_0173:  ldarg.0
  IL_0174:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0179:  ldc.i4.1
  IL_017a:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_017f:  ret
}");
        }

        [Fact]
        public void YieldReturnAwaitDynamic()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    public static async IAsyncEnumerable<int> M()
    {
        dynamic d = Task.FromResult(42);
        yield return await d;
    }
    public static async Task Main(string[] args)
    {
        await foreach (var i in M())
        {
            Console.WriteLine(i);
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, references: new[] { CSharpRef }, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"42");

            verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      627 (0x273)
  .maxstack  10
  .locals init (int V_0,
                object V_1, //d
                object V_2,
                object V_3,
                System.Runtime.CompilerServices.ICriticalNotifyCompletion V_4,
                C.<M>d__0 V_5,
                System.Runtime.CompilerServices.INotifyCompletion V_6,
                System.Exception V_7)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_018e
    IL_000d:  ldloc.0
    IL_000e:  ldc.i4.1
    IL_000f:  beq        IL_0215
    IL_0014:  ldc.i4.s   42
    IL_0016:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_001b:  stloc.1
    IL_001c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
    IL_0021:  brtrue.s   IL_0047
    IL_0023:  ldc.i4.0
    IL_0024:  ldtoken    ""int""
    IL_0029:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_002e:  ldtoken    ""C""
    IL_0033:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0038:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
    IL_003d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_0042:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
    IL_0047:  ldarg.0
    IL_0048:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
    IL_004d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Target""
    IL_0052:  stfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> C.<M>d__0.<>7__wrap1""
    IL_0057:  ldarg.0
    IL_0058:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
    IL_005d:  stfld      ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<M>d__0.<>7__wrap2""
    IL_0062:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__0""
    IL_0067:  brtrue.s   IL_0099
    IL_0069:  ldc.i4.0
    IL_006a:  ldstr      ""GetAwaiter""
    IL_006f:  ldnull
    IL_0070:  ldtoken    ""C""
    IL_0075:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_007a:  ldc.i4.1
    IL_007b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_0080:  dup
    IL_0081:  ldc.i4.0
    IL_0082:  ldc.i4.0
    IL_0083:  ldnull
    IL_0084:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_0089:  stelem.ref
    IL_008a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_008f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_0094:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__0""
    IL_0099:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__0""
    IL_009e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_00a3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__0""
    IL_00a8:  ldloc.1
    IL_00a9:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_00ae:  stloc.3
    IL_00af:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<M>d__0.<>o__0.<>p__2""
    IL_00b4:  brtrue.s   IL_00db
    IL_00b6:  ldc.i4.s   16
    IL_00b8:  ldtoken    ""bool""
    IL_00bd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_00c2:  ldtoken    ""C""
    IL_00c7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_00cc:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
    IL_00d1:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_00d6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<M>d__0.<>o__0.<>p__2""
    IL_00db:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<M>d__0.<>o__0.<>p__2""
    IL_00e0:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
    IL_00e5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<M>d__0.<>o__0.<>p__2""
    IL_00ea:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__1""
    IL_00ef:  brtrue.s   IL_0120
    IL_00f1:  ldc.i4.0
    IL_00f2:  ldstr      ""IsCompleted""
    IL_00f7:  ldtoken    ""C""
    IL_00fc:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0101:  ldc.i4.1
    IL_0102:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_0107:  dup
    IL_0108:  ldc.i4.0
    IL_0109:  ldc.i4.0
    IL_010a:  ldnull
    IL_010b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_0110:  stelem.ref
    IL_0111:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_0116:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_011b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__1""
    IL_0120:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__1""
    IL_0125:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_012a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__1""
    IL_012f:  ldloc.3
    IL_0130:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_0135:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_013a:  brtrue.s   IL_01a5
    IL_013c:  ldarg.0
    IL_013d:  ldc.i4.0
    IL_013e:  dup
    IL_013f:  stloc.0
    IL_0140:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0145:  ldarg.0
    IL_0146:  ldloc.3
    IL_0147:  stfld      ""object C.<M>d__0.<>u__1""
    IL_014c:  ldloc.3
    IL_014d:  isinst     ""System.Runtime.CompilerServices.ICriticalNotifyCompletion""
    IL_0152:  stloc.s    V_4
    IL_0154:  ldarg.0
    IL_0155:  stloc.s    V_5
    IL_0157:  ldloc.s    V_4
    IL_0159:  brtrue.s   IL_0177
    IL_015b:  ldloc.3
    IL_015c:  castclass  ""System.Runtime.CompilerServices.INotifyCompletion""
    IL_0161:  stloc.s    V_6
    IL_0163:  ldarg.0
    IL_0164:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_0169:  ldloca.s   V_6
    IL_016b:  ldloca.s   V_5
    IL_016d:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitOnCompleted<System.Runtime.CompilerServices.INotifyCompletion, C.<M>d__0>(ref System.Runtime.CompilerServices.INotifyCompletion, ref C.<M>d__0)""
    IL_0172:  ldnull
    IL_0173:  stloc.s    V_6
    IL_0175:  br.s       IL_0186
    IL_0177:  ldarg.0
    IL_0178:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_017d:  ldloca.s   V_4
    IL_017f:  ldloca.s   V_5
    IL_0181:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ICriticalNotifyCompletion, C.<M>d__0>(ref System.Runtime.CompilerServices.ICriticalNotifyCompletion, ref C.<M>d__0)""
    IL_0186:  ldnull
    IL_0187:  stloc.s    V_4
    IL_0189:  leave      IL_0272
    IL_018e:  ldarg.0
    IL_018f:  ldfld      ""object C.<M>d__0.<>u__1""
    IL_0194:  stloc.3
    IL_0195:  ldarg.0
    IL_0196:  ldnull
    IL_0197:  stfld      ""object C.<M>d__0.<>u__1""
    IL_019c:  ldarg.0
    IL_019d:  ldc.i4.m1
    IL_019e:  dup
    IL_019f:  stloc.0
    IL_01a0:  stfld      ""int C.<M>d__0.<>1__state""
    IL_01a5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__3""
    IL_01aa:  brtrue.s   IL_01dc
    IL_01ac:  ldc.i4.0
    IL_01ad:  ldstr      ""GetResult""
    IL_01b2:  ldnull
    IL_01b3:  ldtoken    ""C""
    IL_01b8:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_01bd:  ldc.i4.1
    IL_01be:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_01c3:  dup
    IL_01c4:  ldc.i4.0
    IL_01c5:  ldc.i4.0
    IL_01c6:  ldnull
    IL_01c7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_01cc:  stelem.ref
    IL_01cd:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_01d2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_01d7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__3""
    IL_01dc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__3""
    IL_01e1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_01e6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<M>d__0.<>o__0.<>p__3""
    IL_01eb:  ldloc.3
    IL_01ec:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_01f1:  stloc.2
    IL_01f2:  ldarg.0
    IL_01f3:  ldarg.0
    IL_01f4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> C.<M>d__0.<>7__wrap1""
    IL_01f9:  ldarg.0
    IL_01fa:  ldfld      ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<M>d__0.<>7__wrap2""
    IL_01ff:  ldloc.2
    IL_0200:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_0205:  stfld      ""int C.<M>d__0.<>2__current""
    IL_020a:  ldarg.0
    IL_020b:  ldc.i4.1
    IL_020c:  dup
    IL_020d:  stloc.0
    IL_020e:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0213:  leave.s    IL_0266
    IL_0215:  ldarg.0
    IL_0216:  ldc.i4.m1
    IL_0217:  dup
    IL_0218:  stloc.0
    IL_0219:  stfld      ""int C.<M>d__0.<>1__state""
    IL_021e:  ldarg.0
    IL_021f:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_0224:  brfalse.s  IL_0228
    IL_0226:  leave.s    IL_0251
    IL_0228:  ldarg.0
    IL_0229:  ldnull
    IL_022a:  stfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> C.<M>d__0.<>7__wrap1""
    IL_022f:  ldarg.0
    IL_0230:  ldnull
    IL_0231:  stfld      ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<M>d__0.<>7__wrap2""
    IL_0236:  leave.s    IL_0251
  }
  catch System.Exception
  {
    IL_0238:  stloc.s    V_7
    IL_023a:  ldarg.0
    IL_023b:  ldc.i4.s   -2
    IL_023d:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0242:  ldarg.0
    IL_0243:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_0248:  ldloc.s    V_7
    IL_024a:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_024f:  leave.s    IL_0272
  }
  IL_0251:  ldarg.0
  IL_0252:  ldc.i4.s   -2
  IL_0254:  stfld      ""int C.<M>d__0.<>1__state""
  IL_0259:  ldarg.0
  IL_025a:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_025f:  ldc.i4.0
  IL_0260:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0265:  ret
  IL_0266:  ldarg.0
  IL_0267:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_026c:  ldc.i4.1
  IL_026d:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0272:  ret
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source }, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (4,45): error CS0234: The type or namespace name 'IAsyncEnumerable<>' does not exist in the namespace 'System.Collections.Generic' (are you missing an assembly reference?)
                //     static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IAsyncEnumerable<int>").WithArguments("IAsyncEnumerable<>", "System.Collections.Generic").WithLocation(4, 45),
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics();

            var m2 = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M2");
            Assert.False(m2.IsAsync);
            Assert.False(m2.IsIterator);

            var m = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            Assert.True(m.IsAsync);
            Assert.True(m.IsIterator);
        }

        [Fact]
        public void ReturnAfterAwait()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        return null;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (7,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return null;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(7, 9)
                );
        }

        [Fact]
        public void AwaitAfterReturn()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        return null;
        await System.Threading.Tasks.Task.CompletedTask;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return null;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(6, 9),
                // (7,9): warning CS0162: Unreachable code detected
                //         await System.Threading.Tasks.Task.CompletedTask;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "await").WithLocation(7, 9)
                );

            var m = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            Assert.True(m.IsAsync);
            Assert.False(m.IsIterator);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.M");
                AssertEx.SetEqual(new[] { "AsyncStateMachineAttribute", "IteratorStateMachineAttribute" },
                    GetAttributeNames(method.GetAttributes()));
            });
        }

        [Fact]
        public void MissingTypeAndMembers_AsyncIteratorMethodBuilder()
        {
            VerifyMissingMember(WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__AwaitOnCompleted,
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitOnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.AsyncIteratorMethodBuilder", "AwaitOnCompleted").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__AwaitUnsafeOnCompleted,

                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.AsyncIteratorMethodBuilder", "AwaitUnsafeOnCompleted").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__Complete,
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.AsyncIteratorMethodBuilder", "Complete").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__Create,
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.AsyncIteratorMethodBuilder", "Create").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__MoveNext_T,
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.MoveNext'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.AsyncIteratorMethodBuilder", "MoveNext").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingTypeAndMembers_ManualResetValueTaskSourceCore()
        {
            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetResult,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.GetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "GetResult").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetStatus,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "GetStatus").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__get_Version,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.get_Version'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "get_Version").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__OnCompleted,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "OnCompleted").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__Reset,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.Reset'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "Reset").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetException,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.SetException'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "SetException").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetResult,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.SetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "SetResult").WithLocation(5, 64)
                );

            VerifyMissingType(WellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.GetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "GetResult").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "GetStatus").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.get_Version'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "get_Version").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "OnCompleted").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.Reset'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "Reset").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.SetException'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "SetException").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.SetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "SetResult").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingTypeAndMembers_IValueTaskSourceT()
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
        public void MissingTypeAndMembers_IValueTaskSource()
        {
            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__GetResult,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource.GetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource", "GetResult").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__GetStatus,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource", "GetStatus").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__OnCompleted,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource", "OnCompleted").WithLocation(5, 64)
                );

            VerifyMissingType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ValueTask..ctor'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ValueTask", ".ctor").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource.GetResult'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource", "GetResult").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource", "GetStatus").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource", "OnCompleted").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingMember_AsyncIteratorMethodBuilder()
        {
            VerifyMissingMember(WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__Create,
                // (5,64): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Runtime.CompilerServices.AsyncIteratorMethodBuilder", "Create").WithLocation(5, 64)
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
            VerifyMissingMember(_enumerable, WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator,
                // (5,64): error CS0656: Missing compiler required member 'System.Collections.Generic.IAsyncEnumerable`1.GetAsyncEnumerator'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Collections.Generic.IAsyncEnumerable`1", "GetAsyncEnumerator").WithLocation(5, 64)
                );

            VerifyMissingMember(_enumerator, WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator);

            // Since MakeTypeMissing doesn't fully simulate a type being absent (it only makes it disappear from GetWellKnownType), we specially verify missing IAsyncEnumerable<T> since it appears in source
            var comp1 = CreateCompilation(_enumerable);
            comp1.VerifyDiagnostics(
                // (5,38): error CS0234: The type or namespace name 'IAsyncEnumerable<>' does not exist in the namespace 'System.Collections.Generic' (are you missing an assembly reference?)
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IAsyncEnumerable<int>").WithArguments("IAsyncEnumerable<>", "System.Collections.Generic").WithLocation(5, 38)
                );

            // Also verify on local functions
            var comp2 = CreateCompilation(@"
using System.Threading.Tasks;
class C
{
    void M()
    {
        _ = local();
        async System.Collections.Generic.IAsyncEnumerable<int> local() { await Task.CompletedTask; yield return 3; }
    }
}
");
            comp2.VerifyDiagnostics(
                // (8,42): error CS0234: The type or namespace name 'IAsyncEnumerable<>' does not exist in the namespace 'System.Collections.Generic' (are you missing an assembly reference?)
                //         async System.Collections.Generic.IAsyncEnumerable<int> local() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IAsyncEnumerable<int>").WithArguments("IAsyncEnumerable<>", "System.Collections.Generic").WithLocation(8, 42)
                );

            // And missing IAsyncEnumerator<T>
            var comp3 = CreateCompilation(_enumerator);
            comp3.VerifyDiagnostics(
                // (5,38): error CS0234: The type or namespace name 'IAsyncEnumerator<>' does not exist in the namespace 'System.Collections.Generic' (are you missing an assembly reference?)
                //     async System.Collections.Generic.IAsyncEnumerator<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IAsyncEnumerator<int>").WithArguments("IAsyncEnumerator<>", "System.Collections.Generic").WithLocation(5, 38)
                );
        }

        [Fact]
        public void MissingType_ValueTaskSourceStatus()
        {
            VerifyMissingType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceStatus,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "GetStatus").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource`1.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource`1", "GetStatus").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource.GetStatus'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource", "GetStatus").WithLocation(5, 64)
                );
        }

        [Fact]
        public void MissingType_ValueTaskSourceOnCompletedFlags()
        {
            VerifyMissingType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1", "OnCompleted").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource`1.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource`1", "OnCompleted").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.Sources.IValueTaskSource.OnCompleted'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.Sources.IValueTaskSource", "OnCompleted").WithLocation(5, 64)
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
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IAsyncEnumerable<int>").WithArguments("IAsyncEnumerable<>", "System.Collections.Generic").WithLocation(8, 42)
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

            VerifyMissingType(_enumerator, WellKnownType.System_Collections_Generic_IAsyncEnumerator_T,
                // (5,60): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, or IAsyncEnumerable<T>
                //     async System.Collections.Generic.IAsyncEnumerator<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "M").WithLocation(5, 60),
                // (5,60): error CS1624: The body of 'C.M()' cannot be an iterator block because 'IAsyncEnumerator<int>' is not an iterator interface type
                //     async System.Collections.Generic.IAsyncEnumerator<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_BadIteratorReturn, "M").WithArguments("C.M()", "System.Collections.Generic.IAsyncEnumerator<int>").WithLocation(5, 60)
                );

            VerifyMissingType(_enumerable, WellKnownType.System_Collections_Generic_IAsyncEnumerator_T,
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
        public void MissingTypeAndMembers_ValueTaskT()
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
        public void MissingTypeAndMembers_ValueTask()
        {
            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ValueTask__ctor,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ValueTask..ctor'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ValueTask", ".ctor").WithLocation(5, 64)
                );

            VerifyMissingType(WellKnownType.System_Threading_Tasks_ValueTask,
                // (5,64): error CS0656: Missing compiler required member 'System.IAsyncDisposable.DisposeAsync'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.IAsyncDisposable", "DisposeAsync").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ValueTask..ctor'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ValueTask", ".ctor").WithLocation(5, 64)
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
            var comp = CreateCompilationWithAsyncIterator(source);
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
            var comp = CreateCompilationWithAsyncIterator(source);
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
            var comp = CreateCompilationWithAsyncIterator(source);
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
            var comp = CreateCompilationWithAsyncIterator(source);
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
            var comp = CreateCompilationWithAsyncIterator(source);
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
        [WorkItem(31057, "https://github.com/dotnet/roslyn/issues/31057")]
        public void AsyncIteratorReturningEnumerator()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerator<int> M(int value)
    {
        yield return value;
        value = 5;
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield return 3;
        Write(""4 "");
        yield return value;
    }
    static async System.Threading.Tasks.Task Main()
    {
        await using (var enumerator = M(0))
        {
            if (!await enumerator.MoveNextAsync()) throw null;
            Write($""Value:{enumerator.Current} "");
            if (!await enumerator.MoveNextAsync()) throw null;
            Write($""Value:{enumerator.Current} "");
            if (!await enumerator.MoveNextAsync()) throw null;
            Write($""Value:{enumerator.Current} "");
            if (await enumerator.MoveNextAsync()) throw null;
            if (await enumerator.MoveNextAsync()) throw null;

            Write(""Done"");
        }
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Value:0 1 2 Value:3 4 Value:5 Done", symbolValidator: verifyMembersAndInterfaces);

            void verifyMembersAndInterfaces(ModuleSymbol module)
            {
                var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C.<M>d__0");
                AssertEx.SetEqual(new[] {
                    "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder",
                    "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<System.Boolean> C.<M>d__0.<>v__promiseOfValueOrEnd",
                    "System.Int32 C.<M>d__0.value",
                    "C.<M>d__0..ctor(System.Int32 <>1__state)",
                    "void C.<M>d__0.MoveNext()",
                    "void C.<M>d__0.SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)",
                    "System.Threading.Tasks.ValueTask<System.Boolean> C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync()",
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current.get",
                    "System.Boolean C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.GetResult(System.Int16 token)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(System.Int16 token)",
                    "System.Threading.Tasks.Sources.ValueTaskSourceStatus C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.GetStatus(System.Int16 token)",
                    "System.Threading.Tasks.Sources.ValueTaskSourceStatus C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.GetStatus(System.Int16 token)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)",
                    "System.Threading.Tasks.ValueTask C.<M>d__0.System.IAsyncDisposable.DisposeAsync()",
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                    "System.Int32 C.<M>d__0.<>1__state" },
                    type.GetMembersUnordered().Select(m => m.ToTestDisplayString()));

                AssertEx.SetEqual(new[] {
                    "System.Runtime.CompilerServices.IAsyncStateMachine",
                    "System.IAsyncDisposable",
                    "System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>",
                    "System.Threading.Tasks.Sources.IValueTaskSource",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32>" },
                    type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Select(m => m.ToTestDisplayString()));
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(31057, "https://github.com/dotnet/roslyn/issues/31057")]
        public void AsyncIteratorReturningEnumerator_CSharp73()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerator<int> M()
    {
        yield return 1;
        await System.Threading.Tasks.Task.CompletedTask;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (4,60): error CS8370: Feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M").WithArguments("async streams", "8.0").WithLocation(4, 60),
                // (23,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                // #nullable disable
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(23, 2)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(31057, "https://github.com/dotnet/roslyn/issues/31057")]
        public void AsyncIteratorReturningEnumerator_WithReturnOnly()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerator<int> M()
    {
        return null;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (4,60): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 60),
                // (6,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return null;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(6, 9)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(31057, "https://github.com/dotnet/roslyn/issues/31057")]
        public void ReturningIAsyncEnumerator_WithReturn()
        {
            string source = @"
class C
{
    System.Collections.Generic.IAsyncEnumerator<int> M()
    {
        return null;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(31057, "https://github.com/dotnet/roslyn/issues/31057")]
        public void AsyncIteratorReturningEnumerator_WithReturnAndAwait()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerator<int> M(int value)
    {
        return value;
        await System.Threading.Tasks.Task.CompletedTask;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return value;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(6, 9),
                // (7,9): warning CS0162: Unreachable code detected
                //         await System.Threading.Tasks.Task.CompletedTask;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "await").WithLocation(7, 9)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(31057, "https://github.com/dotnet/roslyn/issues/31057")]
        [WorkItem(31113, "https://github.com/dotnet/roslyn/issues/31113")]
        public void AsyncIteratorReturningEnumerator_WithoutAsync()
        {
            string source = @"
class C
{
    static System.Collections.Generic.IAsyncEnumerator<int> M(int value)
    {
        yield return value;
        await System.Threading.Tasks.Task.CompletedTask;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (7,9): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<IAsyncEnumerator<int>>'.
                //         await System.Threading.Tasks.Task.CompletedTask;
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await System.Threading.Tasks.Task.CompletedTask").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>").WithLocation(7, 9)
                );

            // This error message is rather poor. Tracked by https://github.com/dotnet/roslyn/issues/31113
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(31057, "https://github.com/dotnet/roslyn/issues/31057")]
        public void AsyncIteratorReturningEnumerator_WithReturnAfterAwait()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerator<int> M(int value)
    {
        await System.Threading.Tasks.Task.CompletedTask;
        return value;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (7,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return value;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(7, 9)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(31057, "https://github.com/dotnet/roslyn/issues/31057")]
        public void AsyncIteratorReturningEnumerator_WithoutAwait()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerator<int> M(int value)
    {
        yield return value;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (4,67): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async System.Collections.Generic.IAsyncEnumerator<int> M(int value)
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 67)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(31057, "https://github.com/dotnet/roslyn/issues/31057")]
        public void AsyncIteratorReturningEnumerator_WithoutYield()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerator<int> M(int value)
    {
        await System.Threading.Tasks.Task.CompletedTask;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (4,67): error CS0161: 'C.M(int)': not all code paths return a value
                //     static async System.Collections.Generic.IAsyncEnumerator<int> M(int value)
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M(int)").WithLocation(4, 67)
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 5");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(30275, "https://github.com/dotnet/roslyn/issues/30275")]
        public void CallingGetEnumeratorTwice()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M(int value)
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield return 3;
        Write(""4 "");
        value++;
        Write($""{value} "");
    }
    static async System.Threading.Tasks.Task Main()
    {
        var enumerable = M(1);
        await using (var enumerator1 = enumerable.GetAsyncEnumerator())
        {
            await using (var enumerator2 = enumerable.GetAsyncEnumerator())
            {
                if (!await enumerator1.MoveNextAsync()) throw null;
                Write($""Stream1:{enumerator1.Current} "");
                if (!await enumerator2.MoveNextAsync()) throw null;
                Write($""Stream2:{enumerator2.Current} "");
                if (await enumerator1.MoveNextAsync()) throw null;
                if (await enumerator2.MoveNextAsync()) throw null;
                Write(""Done"");
            }
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2 Stream1:3 1 2 Stream2:3 4 2 4 2 Done", symbolValidator: verifyMembersAndInterfaces);
            // Illustrates that parameters are proxied (we save the original in the enumerable, then copy them into working fields when making an enumerator)

            void verifyMembersAndInterfaces(ModuleSymbol module)
            {
                var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C.<M>d__0");
                AssertEx.SetEqual(new[] {
                    "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder",
                    "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<System.Boolean> C.<M>d__0.<>v__promiseOfValueOrEnd",
                    "System.Int32 C.<M>d__0.<>3__value",
                    "C.<M>d__0..ctor(System.Int32 <>1__state)",
                    "void C.<M>d__0.MoveNext()",
                    "void C.<M>d__0.SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32> C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
                    "System.Threading.Tasks.ValueTask<System.Boolean> C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync()",
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current.get",
                    "System.Boolean C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.GetResult(System.Int16 token)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(System.Int16 token)",
                    "System.Threading.Tasks.Sources.ValueTaskSourceStatus C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.GetStatus(System.Int16 token)",
                    "System.Threading.Tasks.Sources.ValueTaskSourceStatus C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.GetStatus(System.Int16 token)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)",
                    "System.Threading.Tasks.ValueTask C.<M>d__0.System.IAsyncDisposable.DisposeAsync()",
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                    "System.Int32 C.<M>d__0.<>1__state" },
                    type.GetMembersUnordered().Select(m => m.ToTestDisplayString()));

                AssertEx.SetEqual(new[] {
                    "System.Runtime.CompilerServices.IAsyncStateMachine",
                    "System.IAsyncDisposable",
                    "System.Threading.Tasks.Sources.IValueTaskSource",
                    "System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>",
                    "System.Collections.Generic.IAsyncEnumerable<System.Int32>",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32>" },
                    type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Select(m => m.ToTestDisplayString()));
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(30275, "https://github.com/dotnet/roslyn/issues/30275")]
        public void CallingGetEnumeratorTwice2()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M(int value)
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield return 3;
        Write(""4 "");
        value++;
        Write($""{value} "");
    }
    static async System.Threading.Tasks.Task Main()
    {
        var enumerable = M(1);
        await using (var enumerator1 = enumerable.GetAsyncEnumerator())
        {
            await using (var enumerator2 = enumerable.GetAsyncEnumerator())
            {
                if (!await enumerator1.MoveNextAsync()) throw null;
                Write($""Stream1:{enumerator1.Current} "");
                if (await enumerator1.MoveNextAsync()) throw null;

                if (!await enumerator2.MoveNextAsync()) throw null;
                Write($""Stream2:{enumerator2.Current} "");
                if (await enumerator2.MoveNextAsync()) throw null;

                Write(""Done"");
            }
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2 Stream1:3 4 2 1 2 Stream2:3 4 2 Done");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(30275, "https://github.com/dotnet/roslyn/issues/30275")]
        public void CallingGetEnumeratorTwice3()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M(int value)
    {
        yield return 0;
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield return 3;
        Write(""4 "");
        value++;
        Write($""{value} "");
    }
    static async System.Threading.Tasks.Task Main()
    {
        var enumerable = M(1);
        var enumerator1 = enumerable.GetAsyncEnumerator();
        if (!await enumerator1.MoveNextAsync()) throw null;
        Write($""Stream1:{enumerator1.Current} "");

        var enumerator2 = enumerable.GetAsyncEnumerator();

        if (!await enumerator2.MoveNextAsync()) throw null;
        Write($""Stream2:{enumerator2.Current} "");

        if (!await enumerator1.MoveNextAsync()) throw null;
        Write($""Stream1:{enumerator1.Current} "");
        if (await enumerator1.MoveNextAsync()) throw null;
        await enumerator1.DisposeAsync();

        if (!await enumerator2.MoveNextAsync()) throw null;
        Write($""Stream2:{enumerator2.Current} "");
        if (await enumerator2.MoveNextAsync()) throw null;
        await enumerator2.DisposeAsync();

        Write(""Done"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Stream1:0 Stream2:0 1 2 Stream1:3 4 2 1 2 Stream2:3 4 2 Done");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(30275, "https://github.com/dotnet/roslyn/issues/30275")]
        public void CallingGetEnumeratorTwice4()
        {
            string source = @"
using System.Threading.Tasks;
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M(int value)
    {
        yield return 0;
        Write(""1 "");
        await Task.Delay(10);
        Write(""2 "");
        yield return 3;
        await Task.Delay(10);
        Write(""4 "");
        value++;
        await Task.Delay(10);
        Write($""{value} "");
        await Task.Delay(10);
    }
    static async Task Main()
    {
        var enumerable = M(41);
        await foreach (var item1 in enumerable)
        {
            Write($""Stream1:{item1} "");
        }
        Write(""Await "");
        await Task.Delay(10);
        await foreach (var item2 in enumerable)
        {
            Write($""Stream2:{item2} "");
        }

        Write(""Done"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Stream1:0 1 2 Stream1:3 4 42 Await Stream2:0 1 2 Stream2:3 4 42 Done");
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
                var comp = CreateCompilationWithAsyncIterator(source, options: options);
                comp.VerifyDiagnostics();
                var verifier = CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 5");

                verifier.VerifyIL("C.M", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.s   -2
  IL_0002:  newobj     ""C.<M>d__0..ctor(int)""
  IL_0007:  ret
}", sequencePoints: "C.M", source: source);

                if (options == TestOptions.DebugExe)
                {
                    verifier.VerifyIL("C.<M>d__0..ctor", @"
{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""int C.<M>d__0.<>1__state""
  IL_000e:  ldarg.0
  IL_000f:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_0014:  stfld      ""int C.<M>d__0.<>l__initialThreadId""
  IL_0019:  ldarg.0
  IL_001a:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_001f:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_0024:  ret
}", sequencePoints: "C+<M>d__0..ctor", source: source);
                }
                else
                {
                    verifier.VerifyIL("C.<M>d__0..ctor", @"
{
  // Code size       36 (0x24)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      ""int C.<M>d__0.<>1__state""
  IL_000d:  ldarg.0
  IL_000e:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_0013:  stfld      ""int C.<M>d__0.<>l__initialThreadId""
  IL_0018:  ldarg.0
  IL_0019:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_001e:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_0023:  ret
}", sequencePoints: "C+<M>d__0..ctor", source: source);
                }

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
  IL_0015:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_001a:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.Reset()""
  IL_001f:  ldarg.0
  IL_0020:  stloc.0
  IL_0021:  ldarg.0
  IL_0022:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_0027:  ldloca.s   V_0
  IL_0029:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.MoveNext<C.<M>d__0>(ref C.<M>d__0)""
  IL_002e:  ldarg.0
  IL_002f:  ldarg.0
  IL_0030:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0035:  call       ""short System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.Version.get""
  IL_003a:  newobj     ""System.Threading.Tasks.ValueTask<bool>..ctor(System.Threading.Tasks.Sources.IValueTaskSource<bool>, short)""
  IL_003f:  ret
}");
                verifier.VerifyIL("C.<M>d__0.System.IAsyncDisposable.DisposeAsync()", @"
{
  // Code size       80 (0x50)
  .maxstack  2
  .locals init (C.<M>d__0 V_0,
                System.Threading.Tasks.ValueTask V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""bool C.<M>d__0.<>w__disposeMode""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_000d:  ldc.i4.s   -2
  IL_000f:  beq.s      IL_001a
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0017:  ldc.i4.m1
  IL_0018:  bne.un.s   IL_0024
  IL_001a:  ldloca.s   V_1
  IL_001c:  initobj    ""System.Threading.Tasks.ValueTask""
  IL_0022:  ldloc.1
  IL_0023:  ret
  IL_0024:  ldarg.0
  IL_0025:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_002a:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.Reset()""
  IL_002f:  ldarg.0
  IL_0030:  stloc.0
  IL_0031:  ldarg.0
  IL_0032:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_0037:  ldloca.s   V_0
  IL_0039:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.MoveNext<C.<M>d__0>(ref C.<M>d__0)""
  IL_003e:  ldarg.0
  IL_003f:  ldarg.0
  IL_0040:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0045:  call       ""short System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.Version.get""
  IL_004a:  newobj     ""System.Threading.Tasks.ValueTask..ctor(System.Threading.Tasks.Sources.IValueTaskSource, short)""
  IL_004f:  ret
}
");
                verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator()", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (C.<M>d__0 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0022
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""int C.<M>d__0.<>l__initialThreadId""
  IL_0010:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_0015:  bne.un.s   IL_0022
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.m1
  IL_0019:  stfld      ""int C.<M>d__0.<>1__state""
  IL_001e:  ldarg.0
  IL_001f:  stloc.0
  IL_0020:  br.s       IL_0029
  IL_0022:  ldc.i4.m1
  IL_0023:  newobj     ""C.<M>d__0..ctor(int)""
  IL_0028:  stloc.0
  IL_0029:  ldloc.0
  IL_002a:  ret
}");
                verifier.VerifyIL("C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<bool>.GetResult(short)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0006:  ldarg.1
  IL_0007:  call       ""bool System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.GetResult(short)""
  IL_000c:  ret
}");
                verifier.VerifyIL("C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<bool>.GetStatus(short)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0006:  ldarg.1
  IL_0007:  call       ""System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.GetStatus(short)""
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
  IL_0001:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0006:  ldarg.1
  IL_0007:  ldarg.2
  IL_0008:  ldarg.3
  IL_0009:  ldarg.s    V_4
  IL_000b:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.OnCompleted(System.Action<object>, object, short, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags)""
  IL_0010:  ret
}");
                if (options == TestOptions.DebugExe)
                {
                    verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      253 (0xfd)
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
    IL_0014:  br         IL_00a1
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
    IL_004c:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_0051:  ldloca.s   V_1
    IL_0053:  ldloca.s   V_2
    IL_0055:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)""
    IL_005a:  nop
    IL_005b:  leave      IL_00fc
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
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      ""int C.<M>d__0.<>1__state""
    IL_009f:  leave.s    IL_00ef
    IL_00a1:  ldarg.0
    IL_00a2:  ldc.i4.m1
    IL_00a3:  dup
    IL_00a4:  stloc.0
    IL_00a5:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00aa:  ldarg.0
    IL_00ab:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_00b0:  brfalse.s  IL_00b4
    IL_00b2:  leave.s    IL_00d9
    // sequence point: Write("" 4 "");
    IL_00b4:  ldstr      "" 4 ""
    IL_00b9:  call       ""void System.Console.Write(string)""
    IL_00be:  nop
    IL_00bf:  leave.s    IL_00d9
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_00c1:  stloc.3
    IL_00c2:  ldarg.0
    IL_00c3:  ldc.i4.s   -2
    IL_00c5:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00ca:  ldarg.0
    IL_00cb:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_00d0:  ldloc.3
    IL_00d1:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_00d6:  nop
    IL_00d7:  leave.s    IL_00fc
  }
  // sequence point: }
  IL_00d9:  ldarg.0
  IL_00da:  ldc.i4.s   -2
  IL_00dc:  stfld      ""int C.<M>d__0.<>1__state""
  // sequence point: <hidden>
  IL_00e1:  ldarg.0
  IL_00e2:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_00e7:  ldc.i4.0
  IL_00e8:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_00ed:  nop
  IL_00ee:  ret
  IL_00ef:  ldarg.0
  IL_00f0:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_00f5:  ldc.i4.1
  IL_00f6:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_00fb:  nop
  IL_00fc:  ret
}", sequencePoints: "C+<M>d__0.MoveNext", source: source);
                }
                else
                {
                    verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      236 (0xec)
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
    IL_000c:  beq        IL_0094
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
    IL_0042:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_0047:  ldloca.s   V_1
    IL_0049:  ldloca.s   V_2
    IL_004b:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)""
    IL_0050:  leave      IL_00eb
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
    IL_008b:  dup
    IL_008c:  stloc.0
    IL_008d:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0092:  leave.s    IL_00df
    IL_0094:  ldarg.0
    IL_0095:  ldc.i4.m1
    IL_0096:  dup
    IL_0097:  stloc.0
    IL_0098:  stfld      ""int C.<M>d__0.<>1__state""
    IL_009d:  ldarg.0
    IL_009e:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_00a3:  brfalse.s  IL_00a7
    IL_00a5:  leave.s    IL_00ca
    // sequence point: Write("" 4 "");
    IL_00a7:  ldstr      "" 4 ""
    IL_00ac:  call       ""void System.Console.Write(string)""
    IL_00b1:  leave.s    IL_00ca
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_00b3:  stloc.3
    IL_00b4:  ldarg.0
    IL_00b5:  ldc.i4.s   -2
    IL_00b7:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00bc:  ldarg.0
    IL_00bd:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_00c2:  ldloc.3
    IL_00c3:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_00c8:  leave.s    IL_00eb
  }
  // sequence point: }
  IL_00ca:  ldarg.0
  IL_00cb:  ldc.i4.s   -2
  IL_00cd:  stfld      ""int C.<M>d__0.<>1__state""
  // sequence point: <hidden>
  IL_00d2:  ldarg.0
  IL_00d3:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_00d8:  ldc.i4.0
  IL_00d9:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_00de:  ret
  IL_00df:  ldarg.0
  IL_00e0:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_00e5:  ldc.i4.1
  IL_00e6:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_00eb:  ret
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Start f:10 f:11 Value f:12 End");
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
            var comp = CreateCompilationWithAsyncIterator(source);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 Done");
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "1 2 END DISPOSAL DONE")]
        [InlineData(10, "1 2 END DISPOSAL DONE")]
        public void AsyncIteratorWithAwaitCompletedAndYieldBreak(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.CompletedTask;
        Write(""2 "");
        yield break;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
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
                var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
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
                var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 Done");
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(2, "1 Break Throw Caught Finally END DISPOSAL DONE")]
        public void TryFinally_DisposeIAsyncEnumeratorMethod(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C : System.Collections.Generic.IAsyncEnumerable<int>
{
    public static System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        return new C();
    }
    public async System.Collections.Generic.IAsyncEnumerator<int> GetAsyncEnumerator()
    {
        yield return 1;
        await System.Threading.Tasks.Task.Delay(10);
        try
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(10);
                Write(""Break "");
                yield break;
            }
            finally
            {
                Write(""Throw "");
                throw null;
            }
        }
        catch
        {
            Write(""Caught "");
            await System.Threading.Tasks.Task.Delay(10);
            yield break;
        }
        finally
        {
            Write(""Finally "");
            await System.Threading.Tasks.Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(2, "1 Break Throw Caught Finally END DISPOSAL DONE")]
        public void TryFinally_YieldBreakInDisposeMode(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        await System.Threading.Tasks.Task.CompletedTask;
        try
        {
            try
            {
                Write(""Break "");
                yield break;
            }
            finally
            {
                Write(""Throw "");
                throw null;
            }
        }
        catch
        {
            Write(""Caught "");
            yield break;
        }
        finally
        {
            Write(""Finally "");
        }
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL DONE")]
        [InlineData(2, "1 2 DISPOSAL Dispose Finally Throw Dispose CAUGHT2 DONE")]
        [InlineData(3, "1 2 Try Dispose Finally Throw Dispose CAUGHT DISPOSAL DONE")]
        public void TryFinally_AwaitUsingInFinally(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C : System.IAsyncDisposable
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            await using (new C())
            {
                yield return 2;
                Write(""Try "");
            }
        }
        finally
        {
            await using (new C())
            {
                Write(""Finally "");
                bool b = true;
                Write(""Throw "");
                if (b) throw null;
            }
        }
        yield return 42;
        Write(""SKIPPED"");
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        Write(""Dispose "");
        await System.Threading.Tasks.Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var v = CompileAndVerify(comp);
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "Try 1 DISPOSAL Finally Item1 Item2 Throw CAUGHT2 DONE")]
        [InlineData(2, "Try 1 2 DISPOSAL Finally Item1 Item2 Throw CAUGHT2 DONE")]
        [InlineData(3, "Try 1 2 Finally Item1 Item2 Throw CAUGHT DISPOSAL DONE")]
        public void TryFinally_AwaitForeachInFinally(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            Write(""Try "");
            await foreach (var i in M2())
            {
                yield return i;
            }
        }
        finally
        {
            Write(""Finally "");
            await foreach (var j in M2())
            {
                Write($""Item{j} "");
                if (j > 1)
                {
                    Write(""Throw "");
                    throw null;
                }
            }
        }
        yield return 42;
        Write(""SKIPPED"");
    }
    public static async System.Collections.Generic.IAsyncEnumerable<int> M2()
    {
        yield return 1;
        await System.Threading.Tasks.Task.Delay(10);
        yield return 2;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(2, "1 Throw Caught Throw2 Dispose CAUGHT DISPOSAL DONE")]
        public void TryFinally_AwaitUsingInCatch(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C : System.IAsyncDisposable
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            Write(""Throw "");
            bool b = true;
            if (b) throw null;
        }
        catch
        {
            await using (new C())
            {
                await System.Threading.Tasks.Task.CompletedTask;
                Write(""Caught "");
                await System.Threading.Tasks.Task.Delay(10);
                bool b = true;
                Write(""Throw2 "");
                if (b) throw null;
            }
        }
        yield return 42;
        Write(""SKIPPED"");
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        Write(""Dispose "");
        await System.Threading.Tasks.Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "Try Item1 Item2 Throw1 Finally Item1 Item2 Throw2 CAUGHT DISPOSAL DONE")]
        public void TryFinally_AwaitForeachInCatch(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            Write(""Try "");
            await foreach (var i in M2())
            {
                await System.Threading.Tasks.Task.Delay(10);
                Write($""Item{i} "");
                if (i > 1)
                {
                    Write(""Throw1 "");
                    throw null;
                }
            }
        }
        catch
        {
            Write(""Finally "");
            await foreach (var j in M2())
            {
                await System.Threading.Tasks.Task.Delay(10);
                Write($""Item{j} "");
                if (j > 1)
                {
                    Write(""Throw2 "");
                    throw null;
                }
            }
        }
        yield return 42;
        Write(""SKIPPED"");
    }
    public static async System.Collections.Generic.IAsyncEnumerable<int> M2()
    {
        yield return 1;
        await System.Threading.Tasks.Task.Delay(10);
        yield return 2;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL DONE")]
        [InlineData(2, "1 Throw Caught Finally END DISPOSAL DONE")]
        public void TryFinally_YieldBreakInCatch(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(10);
                Write(""Throw "");
                bool b = true;
                if (b) throw null;
                Write(""SKIPPED"");
            }
            catch
            {
                Write(""Caught "");
                yield break;
            }
            yield return 42;
            Write(""SKIPPED"");
        }
        finally
        {
            Write(""Finally "");
        }
        yield return 42;
        Write(""SKIPPED"");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL DONE")]
        [InlineData(2, "1 Throw Caught Finally END DISPOSAL DONE")]
        public void TryFinally_YieldBreakInCatch_WithAwaits(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(10);
                Write(""Throw "");
                bool b = true;
                if (b) throw null;
                Write(""SKIPPED"");
            }
            catch
            {
                Write(""Caught "");
                await System.Threading.Tasks.Task.Delay(10);
                yield break;
            }
            yield return 42;
            Write(""SKIPPED"");
        }
        finally
        {
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Finally "");
        }
        yield return 42;
        Write(""SKIPPED"");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL Finally2 DONE")]
        [InlineData(2, "1 Throw Caught Break Finally Finally2 END DISPOSAL DONE")]
        public void TryFinally_YieldBreakInCatch_Nested(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            yield return 1;
            try
            {
                await System.Threading.Tasks.Task.Delay(10);
                Write(""Throw "");
                bool b = true;
                if (b) throw null;
            }
            catch
            {
                Write(""Caught "");
                try
                {
                    await System.Threading.Tasks.Task.Delay(10);
                    Write(""Break "");
                    bool b = true;
                    if (b) yield break;
                }
                finally
                {
                    Write(""Finally "");
                }
                Write(""SKIPPED"");
            }
            Write(""SKIPPED"");
        }
        finally
        {
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Finally2 "");
        }
        Write(""SKIPPED"");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "1 DISPOSAL Finally DONE")]
        [InlineData(2, "1 Break Finally END DISPOSAL DONE")]
        public void TryFinally_YieldBreak(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            yield return 1;
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Break "");
            bool b = true;
            if (b) yield break;
            Write(""SKIPPED"");
        }
        finally
        {
            Write(""Finally "");
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "1 DISPOSAL Finally DONE")]
        [InlineData(2, "1 2 DISPOSAL Finally DONE")]
        [InlineData(3, "1 2 Finally END DISPOSAL DONE")]
        public void TryFinally_WithYieldsAndAwaits(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            yield return 1;
            await System.Threading.Tasks.Task.CompletedTask;
            yield return 2;
        }
        finally
        {
            Write(""Finally "");
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }


        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "1 DISPOSAL Finally1 Finally2 Finally5 Finally6 DONE")]
        [InlineData(2, "1 2 DISPOSAL Finally1 Finally2 Finally5 Finally6 DONE")]
        [InlineData(3, "1 2 Finally1 Finally2 3 DISPOSAL Finally5 Finally6 DONE")]
        [InlineData(4, "1 2 Finally1 Finally2 3 4 DISPOSAL Finally3 Finally4 Finally5 Finally6 DONE")]
        [InlineData(5, "1 2 Finally1 Finally2 3 4 5 DISPOSAL Finally3 Finally4 Finally5 Finally6 DONE")]
        [InlineData(6, "1 2 Finally1 Finally2 3 4 5 Finally3 Finally4 6 DISPOSAL Finally5 Finally6 DONE")]
        [InlineData(7, "1 2 Finally1 Finally2 3 4 5 Finally3 Finally4 6 Finally5 Finally6 7 DISPOSAL DONE")]
        [InlineData(8, "1 2 Finally1 Finally2 3 4 5 Finally3 Finally4 6 Finally5 Finally6 7 END DISPOSAL DONE")]
        public void TryFinally_MultipleToplevelTryes(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            try
            {
                yield return 1;
                await System.Threading.Tasks.Task.CompletedTask;
                yield return 2;
            }
            finally
            {
                Write(""Finally1 "");
                await System.Threading.Tasks.Task.Delay(10);
                Write(""Finally2 "");
            }

            yield return 3;

            try
            {
                yield return 4;
                await System.Threading.Tasks.Task.CompletedTask;
                yield return 5;
            }
            finally
            {
                Write(""Finally3 "");
                await System.Threading.Tasks.Task.Delay(10);
                Write(""Finally4 "");
            }
            yield return 6;
        }
        finally
        {
            Write(""Finally5 "");
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Finally6 "");
        }

        yield return 7;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }


        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "1 DISPOSAL Finally DONE")]
        [InlineData(2, "1 Throw Finally CAUGHT DISPOSAL DONE")]
        [InlineData(3, "1 Throw Finally CAUGHT DISPOSAL DONE")]
        public void TryFinally_WithYieldsAndAwaits_WithThrow(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            await System.Threading.Tasks.Task.CompletedTask;
            yield return 1;
            bool b = true;
            Write(""Throw "");
            if (b) throw null;
            yield return 42;
        }
        finally
        {
            Write(""Finally "");
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "1 DISPOSAL Finally DONE")]
        [InlineData(2, "1 2 DISPOSAL Finally DONE")]
        [InlineData(3, "1 2 Finally END DISPOSAL DONE")]
        public void TryFinally_WithYieldsOnly(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.Delay(10);
        try
        {
            yield return 1;
            yield return 2;
        }
        finally
        {
            Write(""Finally "");
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL Finally DONE")]
        [InlineData(10, "1 Throw Finally CAUGHT DISPOSAL DONE")]
        public void TryFinally_WithYieldsOnly_WithThrow(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.Delay(10);
        try
        {
            yield return 1;
            Write(""Throw "");
            bool b = true;
            if (b) throw null;
            yield return 2;
        }
        finally
        {
            Write(""Finally "");
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL DONE")]
        [InlineData(2, "1 Try Finally 2 DISPOSAL DONE")]
        [InlineData(3, "1 Try Finally 2 END DISPOSAL DONE")]
        public void TryFinally_WithAwaitsOnly(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            await System.Threading.Tasks.Task.CompletedTask;
            Write(""Try "");
            await System.Threading.Tasks.Task.Delay(10);
        }
        finally
        {
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Finally "");
            await System.Threading.Tasks.Task.CompletedTask;
        }
        yield return 2;
    }
}
";

            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL DONE")]
        [InlineData(2, "1 Throw Finally CAUGHT DISPOSAL DONE")]
        public void TryFinally_WithAwaitsOnly_WithThrow(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Throw "");
            bool b = true;
            if (b) throw null;
            await System.Threading.Tasks.Task.Delay(10);
            Write(""SKIPPED"");
        }
        finally
        {
            Write(""Finally "");
        }
        yield return 2;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL DONE")]
        [InlineData(2, "1 Throw1 Throw2 Finally CAUGHT DISPOSAL DONE")]
        public void TryFinally_WithAwaitsOnly_WithSlowThrowInAwait(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            await System.Threading.Tasks.Task.Delay(10);
            await SlowThrowAsync();
            Write(""SKIPPED"");
            await System.Threading.Tasks.Task.Delay(10);
        }
        finally
        {
            Write(""Finally "");
        }
        yield return 2;
    }
    static async System.Threading.Tasks.Task SlowThrowAsync()
    {
        Write(""Throw1 "");
        await System.Threading.Tasks.Task.Delay(10);
        Write(""Throw2 "");
        throw null;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL DONE")]
        [InlineData(2, "1 Throw Finally CAUGHT DISPOSAL DONE")]
        public void TryFinally_WithAwaitsOnly_WithFastThrowInAwait(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            await System.Threading.Tasks.Task.Delay(10);
            await FastThrowAsync();
            Write(""SKIPPED"");
            await System.Threading.Tasks.Task.CompletedTask;
        }
        finally
        {
            Write(""Finally "");
        }
        Write(""SKIPPED"");
        yield return 2;
    }
    static async System.Threading.Tasks.Task FastThrowAsync()
    {
        Write(""Throw "");
        bool b = true;
        if (b) throw null;
        await System.Threading.Tasks.Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 Throw Finally1 Caught")]
        [InlineData(2, "1 2 Throw Finally2 Finally1 Caught")]
        [InlineData(3, "1 2 Finally2 3 Throw Finally3 Finally1 Caught")]
        [InlineData(4, "1 2 Finally2 3 Throw Finally1 Caught")]
        [InlineData(5, "1 2 Finally2 3 Finally3 Finally1 4")]
        public void TryFinally_Nested_WithYields(int position, string expectedOutput)
        {
            string template = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        try
        {
            yield return 1;
            ThrowIf(1);

            try
            {
                yield return 2;
                ThrowIf(2);
            }
            finally
            {
                Write(""Finally2 "");
            }

            try
            {
                yield return 3;
                ThrowIf(3);
            }
            finally
            {
                ThrowIf(4);
                Write(""Finally3 "");
            }
        }
        finally
        {
            Write(""Finally1 "");
        }

        yield return 4;
    }
    static void ThrowIf(int position)
    {
        if (position == POSITION)
        {
            Write(""Throw "");
            throw null;
        }
    }
    static async System.Threading.Tasks.Task Main()
    {
        try
        {
            await foreach (var item in M())
            {
                Write($""{item} "");
            }
        }
        catch
        {
            Write(""Caught"");
        }
    }
}
";
            var source = template.Replace("POSITION", position.ToString());
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "100 1 Throw Finally1 Caught")]
        [InlineData(2, "100 1 Throw Finally1 Caught")]
        [InlineData(3, "100 1 2 Throw Finally2 Finally1 Caught")]
        [InlineData(4, "100 1 2 Throw Finally2 Finally1 Caught")]
        [InlineData(5, "100 1 2 Throw Finally2 Finally1 Caught")]
        [InlineData(6, "100 1 2 Finally2 3 Throw Finally3 Finally1 Caught")]
        [InlineData(7, "100 1 2 Finally2 3 Throw Finally3 Finally1 Caught")]
        [InlineData(8, "100 1 2 Finally2 3 Throw Finally3 Finally1 Caught")]
        [InlineData(9, "100 1 2 Finally2 3 Throw Finally1 Caught")]
        [InlineData(10, "100 1 2 Finally2 3 Finally3 Finally1 101")]
        public void TryFinally_Nested_WithAwaits(int position, string expectedOutput)
        {
            string template = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 100;
        try
        {
            Write(""1 "");
            ThrowIf(1);
            await System.Threading.Tasks.Task.Delay(10);
            ThrowIf(2);

            try
            {
                Write(""2 "");
                ThrowIf(3);
                await System.Threading.Tasks.Task.Delay(10);
                ThrowIf(4);
                await System.Threading.Tasks.Task.CompletedTask;
                ThrowIf(5);
            }
            finally
            {
                Write(""Finally2 "");
            }

            try
            {
                Write(""3 "");
                ThrowIf(6);
                await System.Threading.Tasks.Task.CompletedTask;
                ThrowIf(7);
                await System.Threading.Tasks.Task.Delay(10);
                ThrowIf(8);
            }
            finally
            {
                ThrowIf(9);
                Write(""Finally3 "");
            }
        }
        finally
        {
            Write(""Finally1 "");
        }

        yield return 101;
    }
    static void ThrowIf(int position)
    {
        if (position == POSITION)
        {
            Write(""Throw "");
            throw null;
        }
    }
    static async System.Threading.Tasks.Task Main()
    {
        try
        {
            await foreach (var item in M())
            {
                Write($""{item} "");
            }
        }
        catch
        {
            Write(""Caught"");
        }
    }
}
";
            var source = template.Replace("POSITION", position.ToString());
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL DONE")]
        [InlineData(2, "1 Try Caught1 Caught2 After END DISPOSAL DONE")]
        public void TryFinally_AwaitAndCatch(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            Write(""Try "");
            await System.Threading.Tasks.Task.CompletedTask;
            bool b = true;
            if (b) throw null;
        }
        catch
        {
            await System.Threading.Tasks.Task.CompletedTask;
            Write(""Caught1 "");
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Caught2 "");
        }
        Write(""After "");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL DONE")]
        [InlineData(2, "1 Throw Caught END DISPOSAL DONE")]
        public void TryFinally_AwaitInCatch(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            Write(""Throw "");
            throw null;
        }
        catch
        {
            Write(""Caught "");
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "1 DISPOSAL DONE")]
        [InlineData(2, "1 Throw Caught END DISPOSAL DONE")]
        public void TryFinally_AwaitAndYieldBreakInCatch(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            Write(""Throw "");
            throw null;
        }
        catch
        {
            Write(""Caught "");
            await System.Threading.Tasks.Task.CompletedTask;
            yield break;
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "1 DISPOSAL Finally DONE")]
        [InlineData(2, "1 Try 2 DISPOSAL Finally DONE")]
        [InlineData(3, "1 Try 2 Finally END DISPOSAL DONE")]
        public void TryFinally_AwaitInFinally_YieldInTry(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;

public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            yield return 1;
            Write(""Try "");
            yield return 2;
        }
        finally
        {
            Write(""Finally "");
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TryFinally_NoYieldReturnInTryCatch()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        try
        {
            yield return 1;
        }
        catch
        {
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (9,13): error CS1626: Cannot yield a value in the body of a try block with a catch clause
                //             yield return 1;
                Diagnostic(ErrorCode.ERR_BadYieldInTryOfCatch, "yield").WithLocation(9, 13)
                );
        }

        [Fact]
        public void TryFinally_NoYieldReturnInTryCatch_Nested()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        try
        {
            try
            {
                yield return 1;
            }
            finally
            {
            }
        }
        catch
        {
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (11,17): error CS1626: Cannot yield a value in the body of a try block with a catch clause
                //                 yield return 1;
                Diagnostic(ErrorCode.ERR_BadYieldInTryOfCatch, "yield").WithLocation(11, 17)
                );
        }

        [Fact]
        public void TryFinally_NoYieldBreakInFinally()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        try
        {
        }
        finally
        {
            yield break;
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (12,13): error CS1625: Cannot yield in the body of a finally clause
                //             yield break;
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield").WithLocation(12, 13)
                );
        }

        [Fact]
        public void TryFinally_NoYieldBreakInFinally_Nested()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        try
        {
        }
        finally
        {
            try
            {
                yield break;
            }
            finally
            {
            }
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (14,17): error CS1625: Cannot yield in the body of a finally clause
                //                 yield break;
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield").WithLocation(14, 17)
                );
        }

        [Fact]
        public void TryFinally_NoYieldReturnInCatch()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
        }
        catch
        {
            yield return 1;
        }

        try
        {
            await System.Threading.Tasks.Task.CompletedTask;
        }
        catch
        {
            yield return 2;
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (11,13): error CS1631: Cannot yield a value in the body of a catch clause
                //             yield return 1;
                Diagnostic(ErrorCode.ERR_BadYieldInCatch, "yield").WithLocation(11, 13),
                // (20,13): error CS1631: Cannot yield a value in the body of a catch clause
                //             yield return 2;
                Diagnostic(ErrorCode.ERR_BadYieldInCatch, "yield").WithLocation(20, 13)
                );
        }

        [Fact]
        public void TryFinally_NoYieldReturnInCatch_Nested()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
        }
        catch
        {
            try
            {
                yield return 1;
            }
            finally { }
        }

        try
        {
            await System.Threading.Tasks.Task.CompletedTask;
        }
        catch
        {
            try
            {
                yield return 2;
            }
            finally { }
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (13,17): error CS1631: Cannot yield a value in the body of a catch clause
                //                 yield return 1;
                Diagnostic(ErrorCode.ERR_BadYieldInCatch, "yield").WithLocation(13, 17),
                // (26,17): error CS1631: Cannot yield a value in the body of a catch clause
                //                 yield return 2;
                Diagnostic(ErrorCode.ERR_BadYieldInCatch, "yield").WithLocation(26, 17)
                );
        }

        [Fact]
        public void TryFinally_NoYieldReturnInFinally()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
        }
        finally
        {
            yield return 1;
        }

        try
        {
            await System.Threading.Tasks.Task.CompletedTask;
        }
        finally
        {
            yield return 2;
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (11,13): error CS1625: Cannot yield in the body of a finally clause
                //             yield return 1;
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield").WithLocation(11, 13),
                // (20,13): error CS1625: Cannot yield in the body of a finally clause
                //             yield return 2;
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield").WithLocation(20, 13)
                );
        }

        [Fact]
        public void TryFinally_NoYieldInFinally_Nested()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
        }
        finally
        {
            try
            {
                yield return 1;
            }
            finally { }
        }

        try
        {
        }
        finally
        {
            await System.Threading.Tasks.Task.CompletedTask;
            try
            {
                yield return 2;
            }
            finally { }
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (13,17): error CS1625: Cannot yield in the body of a finally clause
                //                 yield return 1;
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield").WithLocation(13, 17),
                // (26,17): error CS1625: Cannot yield in the body of a finally clause
                //                 yield return 2;
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield").WithLocation(26, 17)
                );
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "0 DISPOSAL Finally1 DONE")]
        [InlineData(2, "0 Finally1 Again 2 DISPOSAL Finally3 DONE")]
        public void TryFinally_DisposingInsideLoop(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        int counter = 0;

        bool b = true;
        while (b)
        {
            try
            {
                yield return counter++;
                await System.Threading.Tasks.Task.Delay(10);
            }
            finally
            {
                Write($""Finally{counter++} "");
                await System.Threading.Tasks.Task.Delay(10);
            }

            Write($""Again "");
        }

        Write($""SKIPPED"");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "1 DISPOSAL Finally CAUGHT2 DONE")]
        [InlineData(2, "1 Finally CAUGHT DISPOSAL DONE")]
        public void TryFinally_FinallyThrows(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            yield return 1;
            await System.Threading.Tasks.Task.Delay(10);
        }
        finally
        {
            Write($""Finally "");
            await System.Threading.Tasks.Task.Delay(10);
            bool b = true;
            if (b) throw null;
        }
        Write($""SKIPPED "");
    }
}
";

            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "1 DISPOSAL Finally1 Finally2 CAUGHT2 DONE")]
        [InlineData(2, "1 Finally1 Finally2 CAUGHT DISPOSAL DONE")]
        public void TryFinally_FinallyThrows_Nested(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            try
            {
                yield return 1;
                await System.Threading.Tasks.Task.Delay(10);
            }
            finally
            {
                Write($""Finally1 "");
                await System.Threading.Tasks.Task.Delay(10);
                bool b = true;
                if (b) throw null;
            }
            Write($""SKIPPED "");
        }
        finally
        {
            Write($""Finally2 "");
        }
        Write($""SKIPPED "");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "1 DISPOSAL DONE")]
        [InlineData(2, "1 Try1 Try2 Caught Finally1 Finally2 END DISPOSAL DONE")]
        [InlineData(10, "1 Try1 Try2 Caught Finally1 Finally2 END DISPOSAL DONE")]
        public void TryFinally_AwaitsInVariousPositions_NoYieldInTry(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        try
        {
            Write(""Try1 "");
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Try2 "");
            throw new System.Exception();
        }
        catch
        {
            Write(""Caught "");
        }
        finally
        {
            Write(""Finally1 "");
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Finally2 "");
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(1, "Try1 1 DISPOSAL Finally1 Finally2 DONE")]
        [InlineData(2, "Try1 1 Throw Finally1 Finally2 CAUGHT DISPOSAL DONE")]
        [InlineData(10, "Try1 1 Throw Finally1 Finally2 CAUGHT DISPOSAL DONE")]
        public void TryFinally_AwaitsInVariousPositions_WithYieldInTry(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            Write(""Try1 "");
            yield return 1;
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Throw "");
            bool b = true;
            if (b) throw new System.Exception();
        }
        finally
        {
            Write(""Finally1 "");
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Finally2 "");
        }
        Write(""SKIPPED"");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "Try1 1 DISPOSAL Finally1 Finally2 DONE")]
        [InlineData(2, "Try1 1 Try2 Finally1 Finally2 END DISPOSAL DONE")]
        [InlineData(10, "Try1 1 Try2 Finally1 Finally2 END DISPOSAL DONE")]
        public void TryFinally_AwaitsInVariousPositions_WithYieldInTry_NoThrow(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        try
        {
            Write(""Try1 "");
            yield return 1;
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Try2 "");
        }
        finally
        {
            Write(""Finally1 "");
            await System.Threading.Tasks.Task.Delay(10);
            Write(""Finally2 "");
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
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
            var comp = CreateCompilationWithAsyncIterator(source);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithAsyncIterator(source);
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
            var comp = CreateCompilationWithAsyncIterator(source);
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

        [Fact]
        public void TestWellKnownMembers()
        {
            var comp = CreateCompilation(AsyncStreamsTypes, references: new[] { TestReferences.NetStandard20.TasksExtensionsRef }, targetFramework: TargetFramework.NetStandard20);
            comp.VerifyDiagnostics();

            verifyType(WellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T,
                "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<TResult>");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetResult,
                "TResult System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<TResult>.GetResult(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetStatus,
                "System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<TResult>.GetStatus(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__OnCompleted,
                "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<TResult>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__Reset,
                "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<TResult>.Reset()");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetException,
                "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<TResult>.SetException(System.Exception error)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetResult,
                "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<TResult>.SetResult(TResult result)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__get_Version,
                "System.Int16 System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<TResult>.Version.get");

            verifyType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceStatus,
                "System.Threading.Tasks.Sources.ValueTaskSourceStatus");

            verifyType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags,
                "System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags");

            verifyType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T,
                "System.Threading.Tasks.Sources.IValueTaskSource<out TResult>");

            verifyType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource,
                "System.Threading.Tasks.Sources.IValueTaskSource");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult,
                "TResult System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.GetResult(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetStatus,
                "System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.GetStatus(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__OnCompleted,
                "void System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__GetResult,
                "void System.Threading.Tasks.Sources.IValueTaskSource.GetResult(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__GetStatus,
                "System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.Sources.IValueTaskSource.GetStatus(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__OnCompleted,
                "void System.Threading.Tasks.Sources.IValueTaskSource.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)");

            verifyType(WellKnownType.System_Threading_Tasks_ValueTask_T,
                "System.Threading.Tasks.ValueTask<TResult>");

            verifyType(WellKnownType.System_Threading_Tasks_ValueTask,
                "System.Threading.Tasks.ValueTask");

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
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(8, 15)
                );
        }

        [Fact]
        public void TestIteratorWithBaseAccess()
        {
            // modified version of corresponding CodeGenIterators test
            var source = @"
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

class Test
{
    static async Task Main()
    {
        await foreach (string i in new Derived().Iter())
        {
            Console.Write(i);
        }
    }
}

class Base
{
    public virtual string Func()
    {
        return ""Base.Func;"";
    }
}

class Derived: Base
{
    public override string Func()
    {
        return ""Derived.Func;"";
    }

    public async IAsyncEnumerable<string> Iter()
    {
        await Task.CompletedTask;
        yield return base.Func();
        yield return this.Func();
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Base.Func;Derived.Func;");
        }

        [Fact]
        public void TestIteratorWithBaseAccessInLambda()
        {
            // modified version of corresponding CodeGenIterators test
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

static class M1
{
    class B1<T>
    {
        public virtual string F<U>(T t, U u)
        {
            return ""B1::F;"";
        }
    }

    class Outer<V>
    {
        public class B2 : B1<V>
        {
            public override string F<U>(V t, U u)
            {
                return ""B2::F;"";
            }

            public async Task Test()
            {
                Func<Task> m = async () =>
                    {
                        await foreach (string i in this.Iter())
                        {
                            Console.Write(i);
                        }
                    };
                await m();
            }

            public async IAsyncEnumerable<string> Iter()
            {
                V v = default(V);
                int i = 0;
                string s = null;

                Func<string> f = () => { Func<Func<V, int, string>> ff = () => base.F<int>; return ff()(v, i); };
                yield return f();

                f = () => { Func<Func<V, string, string>> ff = () => this.F<string>; return ff()(v, s); };
                yield return f();

                f = () => { Func<Func<V, int, string>> ff = () => { i++; return base.F<int>; }; return ff()(v, i); };
                yield return f();
                await Task.CompletedTask;
            }
        }
    }

    class D<X> : Outer<X>.B2
    {
        public override string F<U>(X t, U u)
        {
            return ""D::F;"";
        }
    }

    static async Task Main()
    {
        await (new D<int>()).Test();
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "B1::F;D::F;B1::F;");
        }
    }
}
