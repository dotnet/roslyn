// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenAsyncEHTests : EmitMetadataTestBase
    {
        private static readonly MetadataReference[] AsyncRefs = new[] { MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929 };

        public CodeGenAsyncEHTests()
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput = null, IEnumerable<MetadataReference> references = null, TestEmitters emitOptions = TestEmitters.All, CSharpCompilationOptions options = null)
        {
            references = (references != null) ? references.Concat(AsyncRefs) : AsyncRefs;
            return base.CompileAndVerify(source, expectedOutput: expectedOutput, additionalRefs: references, options: options, emitOptions: emitOptions);
        }

        [Fact]
        [WorkItem(624970, "DevDiv")]
        public void AsyncWithEH()
        {
            var source = @"
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Test
{
    static int awaitCount = 0;
    static int finallyCount = 0;

    static void LogAwait()
    {
        Interlocked.Increment(ref awaitCount);
    }

    static void LogException()
    {
        Interlocked.Increment(ref finallyCount);
    }

    public static async void F(AutoResetEvent handle)
    {
        try
        {
            await Task.Factory.StartNew(LogAwait);
            try
            {
                await Task.Factory.StartNew(LogAwait);
                try
                {
                    await Task.Factory.StartNew(LogAwait);
                    try
                    {
                        await Task.Factory.StartNew(LogAwait);
                        throw new Exception();
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        LogException();
                    }
                    await Task.Factory.StartNew(LogAwait);
                    throw new Exception();
                }
                catch (Exception)
                {
                }
                finally
                {
                    LogException();
                }
                await Task.Factory.StartNew(LogAwait);
                throw new Exception();
            }
            catch (Exception)
            {
            }
            finally
            {
                LogException();
            }
            await Task.Factory.StartNew(LogAwait);
        }
        finally
        {
            handle.Set();
        }
    }

    public static void Main2(int i)
    {
        try
        {
            awaitCount = 0;
            finallyCount = 0;
            var handle = new AutoResetEvent(false);
            F(handle);
            var completed = handle.WaitOne(1000 * 60);
            if (completed)
            {
                if (awaitCount != 7 || finallyCount != 3)
                {
                    throw new Exception(""failed at i="" + i);
                }
            }
            else
            {
                Console.WriteLine(""Test did not complete in time."");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(""unexpected exception thrown:"");
            Console.WriteLine(ex.ToString());
        }
    }

    public static void Main()
    {
        for (int i = 0; i < 1500; i++)
        {
            Main2(i);
        }
    }
}";
            var expected = @"";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact, WorkItem(855080, "DevDiv")]
        public void GenericCatchVariableInAsyncMethod()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Bar().Result);
        }
        static async Task<int> Bar()
        {
            NotImplementedException ex = await Foo<NotImplementedException>();
            return 3;
        }
        public static async Task<T> Foo<T>() where T : Exception
        {
            Task<int> task = null;
            if (task != null) await task;
            T result = null;
            try
            {
            }
            catch (T ex)
            {
                result = ex;
            }
            return result;
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "3");
        }

        [Fact]
        public void AsyncWithException1()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        throw new Exception();
    }

    static async Task<int> G()
    {
        try
        {
            return await F();
        }
        catch(Exception)
        {
            return -1;
        }
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
-1
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithException2()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        throw new Exception();
    }

    static async Task<int> H()
    {
        return await F();
    }

    public static void Main()
    {
        Task<int> t1 = H();
        try
        {
            t1.Wait(1000 * 60);
        }
        catch (AggregateException)
        {
            Console.WriteLine(""exception"");
        }
    }
}";
            var expected = @"
exception
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInFinally001()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        return 2;
    }

    static async Task<int> G()
    {
        int x = 42;

        try
        {
        }
        finally
        {
            x = await F();
        }

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
2
";
            CompileAndVerify(source, expectedOutput: expected).
VerifyIL("Test.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      221 (0xdd)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                object V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<G>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005b
    IL_000a:  ldarg.0
    IL_000b:  ldnull
    IL_000c:  stfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_0011:  ldarg.0
    IL_0012:  ldc.i4.0
    IL_0013:  stfld      ""int Test.<G>d__1.<>7__wrap2""
    .try
    {
      IL_0018:  leave.s    IL_0024
    }
    catch object
    {
      IL_001a:  stloc.2
      IL_001b:  ldarg.0
      IL_001c:  ldloc.2
      IL_001d:  stfld      ""object Test.<G>d__1.<>7__wrap1""
      IL_0022:  leave.s    IL_0024
    }
    IL_0024:  call       ""System.Threading.Tasks.Task<int> Test.F()""
    IL_0029:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_002e:  stloc.3
    IL_002f:  ldloca.s   V_3
    IL_0031:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0036:  brtrue.s   IL_0077
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.0
    IL_003c:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_0041:  ldarg.0
    IL_0042:  ldloc.3
    IL_0043:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_004e:  ldloca.s   V_3
    IL_0050:  ldarg.0
    IL_0051:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
    IL_0056:  leave      IL_00dc
    IL_005b:  ldarg.0
    IL_005c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_0061:  stloc.3
    IL_0062:  ldarg.0
    IL_0063:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_0068:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_006e:  ldarg.0
    IL_006f:  ldc.i4.m1
    IL_0070:  dup
    IL_0071:  stloc.0
    IL_0072:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_0077:  ldloca.s   V_3
    IL_0079:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_007e:  ldloca.s   V_3
    IL_0080:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0086:  ldarg.0
    IL_0087:  ldfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_008c:  stloc.2
    IL_008d:  ldloc.2
    IL_008e:  brfalse.s  IL_00a5
    IL_0090:  ldloc.2
    IL_0091:  isinst     ""System.Exception""
    IL_0096:  dup
    IL_0097:  brtrue.s   IL_009b
    IL_0099:  ldloc.2
    IL_009a:  throw
    IL_009b:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00a0:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00a5:  ldarg.0
    IL_00a6:  ldnull
    IL_00a7:  stfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_00ac:  stloc.1
    IL_00ad:  leave.s    IL_00c8
  }
  catch System.Exception
  {
    IL_00af:  stloc.s    V_4
    IL_00b1:  ldarg.0
    IL_00b2:  ldc.i4.s   -2
    IL_00b4:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_00b9:  ldarg.0
    IL_00ba:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_00bf:  ldloc.s    V_4
    IL_00c1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00c6:  leave.s    IL_00dc
  }
  IL_00c8:  ldarg.0
  IL_00c9:  ldc.i4.s   -2
  IL_00cb:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_00d0:  ldarg.0
  IL_00d1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_00d6:  ldloc.1
  IL_00d7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00dc:  ret
}
");
        }

        [Fact]
        public void AsyncInFinally002()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        System.Console.Write(""F"");
        return 2;
    }

    static async Task G()
    {
        int x = 0;

        try
        {
            throw new Exception(""hello"");
        }
        finally
        {
            x += await F();
        }
    }

    public static void Main()
    {
        Task t2 = G();
        try
        {
            t2.Wait(1000 * 60);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}";
            var expected = @"FOne or more errors occurred.
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInFinally003()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        return 2;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            x = await F();
            return x;
        }
        finally
        {
            x += await F();
        }
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
2
";
            var v = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: expected);

            v.VerifyIL("Test.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      467 (0x1d3)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                int V_4,
                Test.<G>d__1 V_5,
                object V_6,
                int V_7,
                System.Exception V_8)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<G>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_002f
    IL_0014:  br         IL_0117
   -IL_0019:  nop
   -IL_001a:  ldarg.0
    IL_001b:  ldc.i4.0
    IL_001c:  stfld      ""int Test.<G>d__1.<x>5__1""
   ~IL_0021:  ldarg.0
    IL_0022:  ldnull
    IL_0023:  stfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  stfld      ""int Test.<G>d__1.<>7__wrap2""
   ~IL_002f:  nop
    .try
    {
     ~IL_0030:  ldloc.0
      IL_0031:  brfalse.s  IL_0035
      IL_0033:  br.s       IL_0037
      IL_0035:  br.s       IL_0074
     -IL_0037:  nop
     -IL_0038:  call       ""System.Threading.Tasks.Task<int> Test.F()""
      IL_003d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0042:  stloc.3
      IL_0043:  ldloca.s   V_3
      IL_0045:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_004a:  brtrue.s   IL_0090
      IL_004c:  ldarg.0
      IL_004d:  ldc.i4.0
      IL_004e:  dup
      IL_004f:  stloc.0
      IL_0050:  stfld      ""int Test.<G>d__1.<>1__state""
      IL_0055:  ldarg.0
      IL_0056:  ldloc.3
      IL_0057:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
      IL_005c:  ldarg.0
      IL_005d:  stloc.s    V_5
      IL_005f:  ldarg.0
      IL_0060:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
      IL_0065:  ldloca.s   V_3
      IL_0067:  ldloca.s   V_5
      IL_0069:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
      IL_006e:  nop
      IL_006f:  leave      IL_01d2
      IL_0074:  ldarg.0
      IL_0075:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
      IL_007a:  stloc.3
      IL_007b:  ldarg.0
      IL_007c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
      IL_0081:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_0087:  ldarg.0
      IL_0088:  ldc.i4.m1
      IL_0089:  dup
      IL_008a:  stloc.0
      IL_008b:  stfld      ""int Test.<G>d__1.<>1__state""
      IL_0090:  ldloca.s   V_3
      IL_0092:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_0097:  stloc.s    V_4
      IL_0099:  ldloca.s   V_3
      IL_009b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00a1:  ldloc.s    V_4
      IL_00a3:  stloc.2
      IL_00a4:  ldarg.0
      IL_00a5:  ldloc.2
      IL_00a6:  stfld      ""int Test.<G>d__1.<x>5__1""
     -IL_00ab:  ldarg.0
      IL_00ac:  ldarg.0
      IL_00ad:  ldfld      ""int Test.<G>d__1.<x>5__1""
      IL_00b2:  stfld      ""int Test.<G>d__1.<>7__wrap3""
      IL_00b7:  br.s       IL_00b9
      IL_00b9:  ldarg.0
      IL_00ba:  ldc.i4.1
      IL_00bb:  stfld      ""int Test.<G>d__1.<>7__wrap2""
      IL_00c0:  leave.s    IL_00ce
    }
    catch object
    {
     ~IL_00c2:  stloc.s    V_6
      IL_00c4:  ldarg.0
      IL_00c5:  ldloc.s    V_6
      IL_00c7:  stfld      ""object Test.<G>d__1.<>7__wrap1""
      IL_00cc:  leave.s    IL_00ce
    }
   -IL_00ce:  nop
   -IL_00cf:  ldarg.0
    IL_00d0:  ldarg.0
    IL_00d1:  ldfld      ""int Test.<G>d__1.<x>5__1""
    IL_00d6:  stfld      ""int Test.<G>d__1.<>7__wrap4""
    IL_00db:  call       ""System.Threading.Tasks.Task<int> Test.F()""
    IL_00e0:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00e5:  stloc.3
    IL_00e6:  ldloca.s   V_3
    IL_00e8:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00ed:  brtrue.s   IL_0133
    IL_00ef:  ldarg.0
    IL_00f0:  ldc.i4.1
    IL_00f1:  dup
    IL_00f2:  stloc.0
    IL_00f3:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_00f8:  ldarg.0
    IL_00f9:  ldloc.3
    IL_00fa:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_00ff:  ldarg.0
    IL_0100:  stloc.s    V_5
    IL_0102:  ldarg.0
    IL_0103:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_0108:  ldloca.s   V_3
    IL_010a:  ldloca.s   V_5
    IL_010c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
    IL_0111:  nop
    IL_0112:  leave      IL_01d2
    IL_0117:  ldarg.0
    IL_0118:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_011d:  stloc.3
    IL_011e:  ldarg.0
    IL_011f:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_0124:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_012a:  ldarg.0
    IL_012b:  ldc.i4.m1
    IL_012c:  dup
    IL_012d:  stloc.0
    IL_012e:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_0133:  ldloca.s   V_3
    IL_0135:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_013a:  stloc.s    V_4
    IL_013c:  ldloca.s   V_3
    IL_013e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0144:  ldloc.s    V_4
    IL_0146:  stloc.s    V_7
    IL_0148:  ldarg.0
    IL_0149:  ldarg.0
    IL_014a:  ldfld      ""int Test.<G>d__1.<>7__wrap4""
    IL_014f:  ldloc.s    V_7
    IL_0151:  add
    IL_0152:  stfld      ""int Test.<G>d__1.<x>5__1""
   -IL_0157:  nop
   ~IL_0158:  ldarg.0
    IL_0159:  ldfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_015e:  stloc.s    V_6
    IL_0160:  ldloc.s    V_6
    IL_0162:  brfalse.s  IL_0181
    IL_0164:  ldloc.s    V_6
    IL_0166:  isinst     ""System.Exception""
    IL_016b:  stloc.s    V_8
    IL_016d:  ldloc.s    V_8
    IL_016f:  brtrue.s   IL_0174
    IL_0171:  ldloc.s    V_6
    IL_0173:  throw
    IL_0174:  ldloc.s    V_8
    IL_0176:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_017b:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_0180:  nop
    IL_0181:  ldarg.0
    IL_0182:  ldfld      ""int Test.<G>d__1.<>7__wrap2""
    IL_0187:  stloc.s    V_4
    IL_0189:  ldloc.s    V_4
    IL_018b:  ldc.i4.1
    IL_018c:  beq.s      IL_0190
    IL_018e:  br.s       IL_0199
    IL_0190:  ldarg.0
    IL_0191:  ldfld      ""int Test.<G>d__1.<>7__wrap3""
    IL_0196:  stloc.1
    IL_0197:  leave.s    IL_01bd
    IL_0199:  ldarg.0
    IL_019a:  ldnull
    IL_019b:  stfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_01a0:  leave.s    IL_01bd
  }
  catch System.Exception
  {
   ~IL_01a2:  stloc.s    V_8
    IL_01a4:  nop
    IL_01a5:  ldarg.0
    IL_01a6:  ldc.i4.s   -2
    IL_01a8:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_01ad:  ldarg.0
    IL_01ae:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_01b3:  ldloc.s    V_8
    IL_01b5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_01ba:  nop
    IL_01bb:  leave.s    IL_01d2
  }
 -IL_01bd:  ldarg.0
  IL_01be:  ldc.i4.s   -2
  IL_01c0:  stfld      ""int Test.<G>d__1.<>1__state""
 ~IL_01c5:  ldarg.0
  IL_01c6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_01cb:  ldloc.1
  IL_01cc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_01d1:  nop
  IL_01d2:  ret
}", sequencePoints: "Test+<G>d__1.MoveNext");
        }

        [Fact]
        public void AsyncInFinally004()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        return 2;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                throw new Exception();
            }
            finally
            {
                x += await F();
            }
        }
        catch
        {
            return x;
        }
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
2
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInFinallyNested001()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F(int a)
    {
        await Task.Yield();
        return a;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                x = await F(1);
                goto L1;
                System.Console.WriteLine(""FAIL"");
            }
            finally
            {
                x += await F(2);
            }
        }
        finally
        {
            try
            {
                x += await F(4);
            }
            finally
            {
                x += await F(8);
            }
        }

        System.Console.WriteLine(""FAIL"");

        L1:
        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"15";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInFinallyNested002()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F(int a)
    {
        await Task.Yield();
        return a;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                try
                {
                    x = await F(1);
                    throw new Exception(""hello"");
                    System.Console.WriteLine(""FAIL"");
                }
                finally
                {
                    x += await F(2);
                }

                System.Console.WriteLine(""FAIL"");
            }
            finally
            {
                try
                {
                    x += await F(4);
                }
                finally
                {
                    x += await F(8);
                }
            }

            System.Console.WriteLine(""FAIL"");
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex.Message);
        }      

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"hello
15";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInFinallyNested003()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F(int a)
    {
        await Task.Yield();
        return a;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                try
                {
                    x = await F(1);
                    throw new Exception(""hello"");
                    System.Console.WriteLine(""FAIL"");
                }
                finally
                {
                    x += await F(2);
                }

                System.Console.WriteLine(""FAIL"");
            }
            finally
            {
                try
                {
                    x += await F(4);
                }
                finally
                {
                    x += await F(8);
                    throw new Exception(""bye"");
                }
            }

            System.Console.WriteLine(""FAIL"");
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex.Message);
        }      

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"bye
15";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInCatch001()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        return 2;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            x = x / x;
        }
        catch
        {
            x = await F();
        }

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
2
";
            CompileAndVerify(source, expectedOutput: expected).
VerifyIL("Test.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      178 (0xb2)
  .maxstack  3
  .locals init (int V_0,
  int V_1, 
  int V_2, //x
  int V_3,
  System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
  System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<G>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.2
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.3
  .try
{
      IL_000e:  ldloc.2
      IL_000f:  dup
      IL_0010:  div
      IL_0011:  stloc.2
      IL_0012:  leave.s    IL_0019
}
  catch object
{
      IL_0014:  pop
      IL_0015:  ldc.i4.1
      IL_0016:  stloc.3
      IL_0017:  leave.s    IL_0019
}
    IL_0019:  ldloc.3
    IL_001a:  ldc.i4.1
    IL_001b:  bne.un.s   IL_0080
    IL_001d:  call       ""System.Threading.Tasks.Task<int> Test.F()""
    IL_0022:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0027:  stloc.s    V_4
    IL_0029:  ldloca.s   V_4
    IL_002b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0030:  brtrue.s   IL_0070
    IL_0032:  ldarg.0
    IL_0033:  ldc.i4.0
    IL_0034:  dup
    IL_0035:  stloc.0
    IL_0036:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_003b:  ldarg.0
    IL_003c:  ldloc.s    V_4
    IL_003e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_0043:  ldarg.0
    IL_0044:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_0049:  ldloca.s   V_4
    IL_004b:  ldarg.0
    IL_004c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
    IL_0051:  leave.s    IL_00b1
    IL_0053:  ldarg.0
    IL_0054:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_0059:  stloc.s    V_4
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_0061:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0067:  ldarg.0
    IL_0068:  ldc.i4.m1
    IL_0069:  dup
    IL_006a:  stloc.0
    IL_006b:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_0070:  ldloca.s   V_4
    IL_0072:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0077:  ldloca.s   V_4
    IL_0079:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_007f:  stloc.2
    IL_0080:  ldloc.2
    IL_0081:  stloc.1
    IL_0082:  leave.s    IL_009d
}
  catch System.Exception
{
    IL_0084:  stloc.s    V_5
    IL_0086:  ldarg.0
    IL_0087:  ldc.i4.s   -2
    IL_0089:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_008e:  ldarg.0
    IL_008f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_0094:  ldloc.s    V_5
    IL_0096:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_009b:  leave.s    IL_00b1
}
  IL_009d:  ldarg.0
  IL_009e:  ldc.i4.s   -2
  IL_00a0:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_00a5:  ldarg.0
  IL_00a6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_00ab:  ldloc.1
  IL_00ac:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b1:  ret
}
");
        }

        [Fact]
        public void AsyncInCatchRethrow()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        await Task.Yield();
        return 2;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                x = x / x;
            }
            catch
            {
                x = await F();
                throw;
            }
        }
        catch(DivideByZeroException ex)
        {
            x = await F();
            System.Console.WriteLine(ex.Message);
        }

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
Attempted to divide by zero.
2
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInCatchFilter()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        await Task.Yield();
        return 2;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                x = x / x;
            }
            catch if(x != 0)
            {
                x = await F();
                throw;
            }
        }
        catch(Exception ex) if(x == 0 && ((ex = new Exception(""hello"")) != null))
        {
            x = await F();
            System.Console.WriteLine(ex.Message);
        }

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
hello
2
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInCatchFilterLifted()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        await Task.Yield();
        return 2;
    }

    static bool T(Func<bool> f, ref Exception ex)
    {
        var result = f();
        ex = new Exception(result.ToString());
        return result;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            x = x / x;
        }
        catch(Exception ex) if(T(()=>ex.Message == null, ref ex))
        {
            x = await F();
            System.Console.WriteLine(ex.Message);
        }
        catch(Exception ex) if(T(()=>ex.Message != null, ref ex))
        {
            x = await F();
            System.Console.WriteLine(ex.Message);
        }
        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"True
2
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInCatchFinallyMixed()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F(int x)
    {
        await Task.Yield();
        return x;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        try
                        {
                            x = x / await F(0);
                        }
                        catch (DivideByZeroException) if (i < 3)
                        {
                            await Task.Yield();
                            continue;
                        }
                        catch (DivideByZeroException)
                        {
                            x = 2 + await F(x);
                            throw;
                        }
                        System.Console.WriteLine(""FAIL"");
                    }
                    finally
                    {
                        x = await F(x) + 3;
                        if (i >= 3)
                        {
                            throw new Exception(""hello"");
                        }
                    }
                }
            }
            finally
            {
                x = 11 + await F(x);
            }
        }
        catch (Exception ex)
        {
            x = await F(x) + 17;
            System.Console.WriteLine(ex.Message);
        }

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
hello
42
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInCatchFinallyMixed_InAsyncLambda()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F(int x)
    {
        await Task.Yield();
        return x;
    }

    static Func<Task<int>> G()
    {
        int x = 0;

        return async () =>
        {
            try
            {
                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            try
                            {
                                x = x / await F(0);
                            }
                            catch (DivideByZeroException) if (i < 3)
                            {
                                await Task.Yield();
                                continue;
                            }
                            catch (DivideByZeroException)
                            {
                                x = 2 + await F(x);
                                throw;
                            }
                            System.Console.WriteLine(""FAIL"");
                        }
                        finally
                        {
                            x = await F(x) + 3;
                            if (i >= 3)
                            {
                                throw new Exception(""hello"");
                            }
                        }
                    }
                }
                finally
                {
                    x = 11 + await F(x);
                }
            }
            catch (Exception ex)
            {
                x = await F(x) + 17;
                System.Console.WriteLine(ex.Message);
            }

            return x;
        };
    }

    public static void Main()
    {
        Task<int> t2 = G()();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
hello
42
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void DoFinallyBodies()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Driver
{
    public static int finally_count = 0;

    static async Task F()
    {
        try
        {
            await Task.Factory.StartNew(() => { });
        }
        finally
        {
            Driver.finally_count++;
        }
    }
    
    static void Main()
    {
        var t = F();
        t.Wait();
        Console.WriteLine(Driver.finally_count);
    }
}";
            var expected = @"
1
";
            CompileAndVerify(source, expected);
        }

    }
}
