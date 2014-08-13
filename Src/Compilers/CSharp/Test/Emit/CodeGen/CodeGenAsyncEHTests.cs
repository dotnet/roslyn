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
        private CSharpCompilation CreateCompilation(string source, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions compOptions = null)
        {
            SynchronizationContext.SetSynchronizationContext(null);

            compOptions = compOptions ?? TestOptions.ReleaseExe;

            IEnumerable<MetadataReference> asyncRefs = new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef };
            references = (references != null) ? references.Concat(asyncRefs) : asyncRefs;

            return CreateCompilationWithMscorlib45(source, options: compOptions, references: references);
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput, IEnumerable<MetadataReference> references = null, EmitOptions emitOptions = EmitOptions.All, CSharpCompilationOptions compOptions = null)
        {
            SynchronizationContext.SetSynchronizationContext(null);

            var compilation = this.CreateCompilation(source, references: references, compOptions: compOptions);
            return base.CompileAndVerify(compilation, expectedOutput: expectedOutput, emitOptions: emitOptions);
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
  // Code size      225 (0xe1)
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
    IL_0008:  brfalse.s  IL_000e
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq.s      IL_005f
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  stfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_0015:  ldarg.0
    IL_0016:  ldc.i4.0
    IL_0017:  stfld      ""int Test.<G>d__1.<>7__wrap2""
    .try
    {
      IL_001c:  leave.s    IL_0028
    }
    catch object
    {
      IL_001e:  stloc.2
      IL_001f:  ldarg.0
      IL_0020:  ldloc.2
      IL_0021:  stfld      ""object Test.<G>d__1.<>7__wrap1""
      IL_0026:  leave.s    IL_0028
    }
    IL_0028:  call       ""System.Threading.Tasks.Task<int> Test.F()""
    IL_002d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0032:  stloc.3
    IL_0033:  ldloca.s   V_3
    IL_0035:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003a:  brtrue.s   IL_007b
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.1
    IL_003e:  dup
    IL_003f:  stloc.0
    IL_0040:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_0045:  ldarg.0
    IL_0046:  ldloc.3
    IL_0047:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_004c:  ldarg.0
    IL_004d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_0052:  ldloca.s   V_3
    IL_0054:  ldarg.0
    IL_0055:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
    IL_005a:  leave      IL_00e0
    IL_005f:  ldarg.0
    IL_0060:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_0065:  stloc.3
    IL_0066:  ldarg.0
    IL_0067:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_006c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0072:  ldarg.0
    IL_0073:  ldc.i4.m1
    IL_0074:  dup
    IL_0075:  stloc.0
    IL_0076:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_007b:  ldloca.s   V_3
    IL_007d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0082:  ldloca.s   V_3
    IL_0084:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_008a:  ldarg.0
    IL_008b:  ldfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_0090:  stloc.2
    IL_0091:  ldloc.2
    IL_0092:  brfalse.s  IL_00a9
    IL_0094:  ldloc.2
    IL_0095:  isinst     ""System.Exception""
    IL_009a:  dup
    IL_009b:  brtrue.s   IL_009f
    IL_009d:  ldloc.2
    IL_009e:  throw
    IL_009f:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00a4:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00a9:  ldarg.0
    IL_00aa:  ldnull
    IL_00ab:  stfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_00b0:  stloc.1
    IL_00b1:  leave.s    IL_00cc
  }
  catch System.Exception
  {
    IL_00b3:  stloc.s    V_4
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.s   -2
    IL_00b8:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_00bd:  ldarg.0
    IL_00be:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_00c3:  ldloc.s    V_4
    IL_00c5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00ca:  leave.s    IL_00e0
  }
  IL_00cc:  ldarg.0
  IL_00cd:  ldc.i4.s   -2
  IL_00cf:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_00d4:  ldarg.0
  IL_00d5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_00da:  ldloc.1
  IL_00db:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00e0:  ret
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
            var v = CompileAndVerify(source, compOptions: TestOptions.DebugExe, expectedOutput: expected);

            v.VerifyIL("Test.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      479 (0x1df)
  .maxstack  3
  .locals init (int V_0, //CS$524$0000
                int V_1, //CS$523$0001
                int V_2, //CS$530$0002
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                int V_4,
                Test.<G>d__1 V_5,
                object V_6,
                int V_7, //CS$530$0003
                System.Exception V_8)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<G>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_001b,
        IL_001d,
        IL_001f)
    IL_0019:  br.s       IL_0024
    IL_001b:  br.s       IL_0024
    IL_001d:  br.s       IL_003a
    IL_001f:  br         IL_0123
   -IL_0024:  nop
   -IL_0025:  ldarg.0
    IL_0026:  ldc.i4.0
    IL_0027:  stfld      ""int Test.<G>d__1.<x>5__1""
   ~IL_002c:  ldarg.0
    IL_002d:  ldnull
    IL_002e:  stfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_0033:  ldarg.0
    IL_0034:  ldc.i4.0
    IL_0035:  stfld      ""int Test.<G>d__1.<>7__wrap2""
   ~IL_003a:  nop
    .try
    {
     ~IL_003b:  ldloc.0
      IL_003c:  ldc.i4.1
      IL_003d:  beq.s      IL_0041
      IL_003f:  br.s       IL_0043
      IL_0041:  br.s       IL_0080
     -IL_0043:  nop
     -IL_0044:  call       ""System.Threading.Tasks.Task<int> Test.F()""
      IL_0049:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_004e:  stloc.3
      IL_004f:  ldloca.s   V_3
      IL_0051:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_0056:  brtrue.s   IL_009c
      IL_0058:  ldarg.0
      IL_0059:  ldc.i4.1
      IL_005a:  dup
      IL_005b:  stloc.0
      IL_005c:  stfld      ""int Test.<G>d__1.<>1__state""
      IL_0061:  ldarg.0
      IL_0062:  ldloc.3
      IL_0063:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
      IL_0068:  ldarg.0
      IL_0069:  stloc.s    V_5
      IL_006b:  ldarg.0
      IL_006c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
      IL_0071:  ldloca.s   V_3
      IL_0073:  ldloca.s   V_5
      IL_0075:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
      IL_007a:  nop
      IL_007b:  leave      IL_01de
      IL_0080:  ldarg.0
      IL_0081:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
      IL_0086:  stloc.3
      IL_0087:  ldarg.0
      IL_0088:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
      IL_008d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_0093:  ldarg.0
      IL_0094:  ldc.i4.m1
      IL_0095:  dup
      IL_0096:  stloc.0
      IL_0097:  stfld      ""int Test.<G>d__1.<>1__state""
      IL_009c:  ldloca.s   V_3
      IL_009e:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00a3:  stloc.s    V_4
      IL_00a5:  ldloca.s   V_3
      IL_00a7:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00ad:  ldloc.s    V_4
      IL_00af:  stloc.2
      IL_00b0:  ldarg.0
      IL_00b1:  ldloc.2
      IL_00b2:  stfld      ""int Test.<G>d__1.<x>5__1""
     -IL_00b7:  ldarg.0
      IL_00b8:  ldarg.0
      IL_00b9:  ldfld      ""int Test.<G>d__1.<x>5__1""
      IL_00be:  stfld      ""int Test.<G>d__1.<>7__wrap3""
      IL_00c3:  br.s       IL_00c5
      IL_00c5:  ldarg.0
      IL_00c6:  ldc.i4.1
      IL_00c7:  stfld      ""int Test.<G>d__1.<>7__wrap2""
      IL_00cc:  leave.s    IL_00da
    }
    catch object
    {
     ~IL_00ce:  stloc.s    V_6
      IL_00d0:  ldarg.0
      IL_00d1:  ldloc.s    V_6
      IL_00d3:  stfld      ""object Test.<G>d__1.<>7__wrap1""
      IL_00d8:  leave.s    IL_00da
    }
   -IL_00da:  nop
   -IL_00db:  ldarg.0
    IL_00dc:  ldarg.0
    IL_00dd:  ldfld      ""int Test.<G>d__1.<x>5__1""
    IL_00e2:  stfld      ""int Test.<G>d__1.<>7__wrap4""
    IL_00e7:  call       ""System.Threading.Tasks.Task<int> Test.F()""
    IL_00ec:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00f1:  stloc.3
    IL_00f2:  ldloca.s   V_3
    IL_00f4:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00f9:  brtrue.s   IL_013f
    IL_00fb:  ldarg.0
    IL_00fc:  ldc.i4.2
    IL_00fd:  dup
    IL_00fe:  stloc.0
    IL_00ff:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_0104:  ldarg.0
    IL_0105:  ldloc.3
    IL_0106:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_010b:  ldarg.0
    IL_010c:  stloc.s    V_5
    IL_010e:  ldarg.0
    IL_010f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_0114:  ldloca.s   V_3
    IL_0116:  ldloca.s   V_5
    IL_0118:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
    IL_011d:  nop
    IL_011e:  leave      IL_01de
    IL_0123:  ldarg.0
    IL_0124:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_0129:  stloc.3
    IL_012a:  ldarg.0
    IL_012b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
    IL_0130:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0136:  ldarg.0
    IL_0137:  ldc.i4.m1
    IL_0138:  dup
    IL_0139:  stloc.0
    IL_013a:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_013f:  ldloca.s   V_3
    IL_0141:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0146:  stloc.s    V_4
    IL_0148:  ldloca.s   V_3
    IL_014a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0150:  ldloc.s    V_4
    IL_0152:  stloc.s    V_7
    IL_0154:  ldarg.0
    IL_0155:  ldarg.0
    IL_0156:  ldfld      ""int Test.<G>d__1.<>7__wrap4""
    IL_015b:  ldloc.s    V_7
    IL_015d:  add
    IL_015e:  stfld      ""int Test.<G>d__1.<x>5__1""
   -IL_0163:  nop
   ~IL_0164:  ldarg.0
    IL_0165:  ldfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_016a:  stloc.s    V_6
    IL_016c:  ldloc.s    V_6
    IL_016e:  brfalse.s  IL_018d
    IL_0170:  ldloc.s    V_6
    IL_0172:  isinst     ""System.Exception""
    IL_0177:  stloc.s    V_8
    IL_0179:  ldloc.s    V_8
    IL_017b:  brtrue.s   IL_0180
    IL_017d:  ldloc.s    V_6
    IL_017f:  throw
    IL_0180:  ldloc.s    V_8
    IL_0182:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_0187:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_018c:  nop
    IL_018d:  ldarg.0
    IL_018e:  ldfld      ""int Test.<G>d__1.<>7__wrap2""
    IL_0193:  stloc.s    V_4
    IL_0195:  ldloc.s    V_4
    IL_0197:  ldc.i4.1
    IL_0198:  beq.s      IL_019c
    IL_019a:  br.s       IL_01a5
    IL_019c:  ldarg.0
    IL_019d:  ldfld      ""int Test.<G>d__1.<>7__wrap3""
    IL_01a2:  stloc.1
    IL_01a3:  leave.s    IL_01c9
    IL_01a5:  ldarg.0
    IL_01a6:  ldnull
    IL_01a7:  stfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_01ac:  leave.s    IL_01c9
  }
  catch System.Exception
  {
   ~IL_01ae:  stloc.s    V_8
    IL_01b0:  nop
    IL_01b1:  ldarg.0
    IL_01b2:  ldc.i4.s   -2
    IL_01b4:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_01b9:  ldarg.0
    IL_01ba:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_01bf:  ldloc.s    V_8
    IL_01c1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_01c6:  nop
    IL_01c7:  leave.s    IL_01de
  }
 -IL_01c9:  ldarg.0
  IL_01ca:  ldc.i4.s   -2
  IL_01cc:  stfld      ""int Test.<G>d__1.<>1__state""
 ~IL_01d1:  ldarg.0
  IL_01d2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_01d7:  ldloc.1
  IL_01d8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_01dd:  nop
  IL_01de:  ret
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
  // Code size      182 (0xb6)
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
  IL_0008:  brfalse.s  IL_000e
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0057
  IL_000e:  ldc.i4.0
  IL_000f:  stloc.2
  IL_0010:  ldc.i4.0
  IL_0011:  stloc.3
  .try
{
  IL_0012:  ldloc.2
  IL_0013:  dup
  IL_0014:  div
  IL_0015:  stloc.2
  IL_0016:  leave.s    IL_001d
}
  catch object
{
  IL_0018:  pop
  IL_0019:  ldc.i4.1
  IL_001a:  stloc.3
  IL_001b:  leave.s    IL_001d
}
  IL_001d:  ldloc.3
  IL_001e:  ldc.i4.1
  IL_001f:  bne.un.s   IL_0084
  IL_0021:  call       ""System.Threading.Tasks.Task<int> Test.F()""
  IL_0026:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
  IL_002b:  stloc.s    V_4
  IL_002d:  ldloca.s   V_4
  IL_002f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
  IL_0034:  brtrue.s   IL_0074
  IL_0036:  ldarg.0
  IL_0037:  ldc.i4.1
  IL_0038:  dup
  IL_0039:  stloc.0
  IL_003a:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_003f:  ldarg.0
  IL_0040:  ldloc.s    V_4
  IL_0042:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
  IL_0047:  ldarg.0
  IL_0048:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_004d:  ldloca.s   V_4
  IL_004f:  ldarg.0
  IL_0050:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
  IL_0055:  leave.s    IL_00b5
  IL_0057:  ldarg.0
  IL_0058:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
  IL_005d:  stloc.s    V_4
  IL_005f:  ldarg.0
  IL_0060:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
  IL_0065:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_006b:  ldarg.0
  IL_006c:  ldc.i4.m1
  IL_006d:  dup
  IL_006e:  stloc.0
  IL_006f:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_0074:  ldloca.s   V_4
  IL_0076:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
  IL_007b:  ldloca.s   V_4
  IL_007d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_0083:  stloc.2
  IL_0084:  ldloc.2
  IL_0085:  stloc.1
  IL_0086:  leave.s    IL_00a1
}
  catch System.Exception
{
  IL_0088:  stloc.s    V_5
  IL_008a:  ldarg.0
  IL_008b:  ldc.i4.s   -2
  IL_008d:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_0092:  ldarg.0
  IL_0093:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_0098:  ldloc.s    V_5
  IL_009a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
  IL_009f:  leave.s    IL_00b5
}
  IL_00a1:  ldarg.0
  IL_00a2:  ldc.i4.s   -2
  IL_00a4:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_00a9:  ldarg.0
  IL_00aa:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_00af:  ldloc.1
  IL_00b0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b5:  ret
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
