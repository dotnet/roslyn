﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenAsyncEHTests : EmitMetadataTestBase
    {
        private static readonly MetadataReference[] s_asyncRefs = new[] { MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929 };

        public CodeGenAsyncEHTests()
        {
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput = null, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions options = null)
        {
            references = (references != null) ? references.Concat(s_asyncRefs) : s_asyncRefs;
            return base.CompileAndVerify(source, targetFramework: TargetFramework.Empty, expectedOutput: expectedOutput, references: references, options: options);
        }

        [Fact]
        [WorkItem(624970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624970")]
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

        [WorkItem(14878, "https://github.com/dotnet/roslyn/issues/14878")]
        [Fact]
        public void AsyncWithEHCodeQuality()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> G()
    {
        int x = 1;

        try
        {
            try
            {
                try
                {
                    await Task.Yield();
                    x += 1;
                    await Task.Yield();
                    x += 1;
                    await Task.Yield();
                    x += 1;
                    await Task.Yield();
                    x += 1;
                    await Task.Yield();
                    x += 1;
                    await Task.Yield();
                    x += 1;
                }
                finally
                {
                    x += 1;
                }
            }
            finally
            {
                x += 1;
            }
        }
        finally
        {
            x += 1;
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
10
";
            CompileAndVerify(source, expectedOutput: expected).
VerifyIL("Test.<G>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      819 (0x333)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<G>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.5
    IL_0009:  ble.un.s   IL_0012
    IL_000b:  ldarg.0
    IL_000c:  ldc.i4.1
    IL_000d:  stfld      ""int Test.<G>d__0.<x>5__2""
    IL_0012:  nop
    .try
    {
      IL_0013:  ldloc.0
      IL_0014:  ldc.i4.5
      IL_0015:  pop
      IL_0016:  pop
      IL_0017:  nop
      .try
      {
        IL_0018:  ldloc.0
        IL_0019:  ldc.i4.5
        IL_001a:  pop
        IL_001b:  pop
        IL_001c:  nop
        .try
        {
          IL_001d:  ldloc.0
          IL_001e:  switch    (
        IL_0075,
        IL_00e0,
        IL_014b,
        IL_01b6,
        IL_0221,
        IL_028c)
          IL_003b:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
          IL_0040:  stloc.3
          IL_0041:  ldloca.s   V_3
          IL_0043:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
          IL_0048:  stloc.2
          IL_0049:  ldloca.s   V_2
          IL_004b:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
          IL_0050:  brtrue.s   IL_0091
          IL_0052:  ldarg.0
          IL_0053:  ldc.i4.0
          IL_0054:  dup
          IL_0055:  stloc.0
          IL_0056:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_005b:  ldarg.0
          IL_005c:  ldloc.2
          IL_005d:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0062:  ldarg.0
          IL_0063:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
          IL_0068:  ldloca.s   V_2
          IL_006a:  ldarg.0
          IL_006b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<G>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<G>d__0)""
          IL_0070:  leave      IL_0332
          IL_0075:  ldarg.0
          IL_0076:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_007b:  stloc.2
          IL_007c:  ldarg.0
          IL_007d:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0082:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_0088:  ldarg.0
          IL_0089:  ldc.i4.m1
          IL_008a:  dup
          IL_008b:  stloc.0
          IL_008c:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_0091:  ldloca.s   V_2
          IL_0093:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
          IL_0098:  ldarg.0
          IL_0099:  ldarg.0
          IL_009a:  ldfld      ""int Test.<G>d__0.<x>5__2""
          IL_009f:  ldc.i4.1
          IL_00a0:  add
          IL_00a1:  stfld      ""int Test.<G>d__0.<x>5__2""
          IL_00a6:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
          IL_00ab:  stloc.3
          IL_00ac:  ldloca.s   V_3
          IL_00ae:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
          IL_00b3:  stloc.2
          IL_00b4:  ldloca.s   V_2
          IL_00b6:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
          IL_00bb:  brtrue.s   IL_00fc
          IL_00bd:  ldarg.0
          IL_00be:  ldc.i4.1
          IL_00bf:  dup
          IL_00c0:  stloc.0
          IL_00c1:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_00c6:  ldarg.0
          IL_00c7:  ldloc.2
          IL_00c8:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_00cd:  ldarg.0
          IL_00ce:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
          IL_00d3:  ldloca.s   V_2
          IL_00d5:  ldarg.0
          IL_00d6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<G>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<G>d__0)""
          IL_00db:  leave      IL_0332
          IL_00e0:  ldarg.0
          IL_00e1:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_00e6:  stloc.2
          IL_00e7:  ldarg.0
          IL_00e8:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_00ed:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_00f3:  ldarg.0
          IL_00f4:  ldc.i4.m1
          IL_00f5:  dup
          IL_00f6:  stloc.0
          IL_00f7:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_00fc:  ldloca.s   V_2
          IL_00fe:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
          IL_0103:  ldarg.0
          IL_0104:  ldarg.0
          IL_0105:  ldfld      ""int Test.<G>d__0.<x>5__2""
          IL_010a:  ldc.i4.1
          IL_010b:  add
          IL_010c:  stfld      ""int Test.<G>d__0.<x>5__2""
          IL_0111:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
          IL_0116:  stloc.3
          IL_0117:  ldloca.s   V_3
          IL_0119:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
          IL_011e:  stloc.2
          IL_011f:  ldloca.s   V_2
          IL_0121:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
          IL_0126:  brtrue.s   IL_0167
          IL_0128:  ldarg.0
          IL_0129:  ldc.i4.2
          IL_012a:  dup
          IL_012b:  stloc.0
          IL_012c:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_0131:  ldarg.0
          IL_0132:  ldloc.2
          IL_0133:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0138:  ldarg.0
          IL_0139:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
          IL_013e:  ldloca.s   V_2
          IL_0140:  ldarg.0
          IL_0141:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<G>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<G>d__0)""
          IL_0146:  leave      IL_0332
          IL_014b:  ldarg.0
          IL_014c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0151:  stloc.2
          IL_0152:  ldarg.0
          IL_0153:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0158:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_015e:  ldarg.0
          IL_015f:  ldc.i4.m1
          IL_0160:  dup
          IL_0161:  stloc.0
          IL_0162:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_0167:  ldloca.s   V_2
          IL_0169:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
          IL_016e:  ldarg.0
          IL_016f:  ldarg.0
          IL_0170:  ldfld      ""int Test.<G>d__0.<x>5__2""
          IL_0175:  ldc.i4.1
          IL_0176:  add
          IL_0177:  stfld      ""int Test.<G>d__0.<x>5__2""
          IL_017c:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
          IL_0181:  stloc.3
          IL_0182:  ldloca.s   V_3
          IL_0184:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
          IL_0189:  stloc.2
          IL_018a:  ldloca.s   V_2
          IL_018c:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
          IL_0191:  brtrue.s   IL_01d2
          IL_0193:  ldarg.0
          IL_0194:  ldc.i4.3
          IL_0195:  dup
          IL_0196:  stloc.0
          IL_0197:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_019c:  ldarg.0
          IL_019d:  ldloc.2
          IL_019e:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_01a3:  ldarg.0
          IL_01a4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
          IL_01a9:  ldloca.s   V_2
          IL_01ab:  ldarg.0
          IL_01ac:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<G>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<G>d__0)""
          IL_01b1:  leave      IL_0332
          IL_01b6:  ldarg.0
          IL_01b7:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_01bc:  stloc.2
          IL_01bd:  ldarg.0
          IL_01be:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_01c3:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_01c9:  ldarg.0
          IL_01ca:  ldc.i4.m1
          IL_01cb:  dup
          IL_01cc:  stloc.0
          IL_01cd:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_01d2:  ldloca.s   V_2
          IL_01d4:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
          IL_01d9:  ldarg.0
          IL_01da:  ldarg.0
          IL_01db:  ldfld      ""int Test.<G>d__0.<x>5__2""
          IL_01e0:  ldc.i4.1
          IL_01e1:  add
          IL_01e2:  stfld      ""int Test.<G>d__0.<x>5__2""
          IL_01e7:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
          IL_01ec:  stloc.3
          IL_01ed:  ldloca.s   V_3
          IL_01ef:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
          IL_01f4:  stloc.2
          IL_01f5:  ldloca.s   V_2
          IL_01f7:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
          IL_01fc:  brtrue.s   IL_023d
          IL_01fe:  ldarg.0
          IL_01ff:  ldc.i4.4
          IL_0200:  dup
          IL_0201:  stloc.0
          IL_0202:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_0207:  ldarg.0
          IL_0208:  ldloc.2
          IL_0209:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_020e:  ldarg.0
          IL_020f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
          IL_0214:  ldloca.s   V_2
          IL_0216:  ldarg.0
          IL_0217:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<G>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<G>d__0)""
          IL_021c:  leave      IL_0332
          IL_0221:  ldarg.0
          IL_0222:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0227:  stloc.2
          IL_0228:  ldarg.0
          IL_0229:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_022e:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_0234:  ldarg.0
          IL_0235:  ldc.i4.m1
          IL_0236:  dup
          IL_0237:  stloc.0
          IL_0238:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_023d:  ldloca.s   V_2
          IL_023f:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
          IL_0244:  ldarg.0
          IL_0245:  ldarg.0
          IL_0246:  ldfld      ""int Test.<G>d__0.<x>5__2""
          IL_024b:  ldc.i4.1
          IL_024c:  add
          IL_024d:  stfld      ""int Test.<G>d__0.<x>5__2""
          IL_0252:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
          IL_0257:  stloc.3
          IL_0258:  ldloca.s   V_3
          IL_025a:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
          IL_025f:  stloc.2
          IL_0260:  ldloca.s   V_2
          IL_0262:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
          IL_0267:  brtrue.s   IL_02a8
          IL_0269:  ldarg.0
          IL_026a:  ldc.i4.5
          IL_026b:  dup
          IL_026c:  stloc.0
          IL_026d:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_0272:  ldarg.0
          IL_0273:  ldloc.2
          IL_0274:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0279:  ldarg.0
          IL_027a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
          IL_027f:  ldloca.s   V_2
          IL_0281:  ldarg.0
          IL_0282:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<G>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<G>d__0)""
          IL_0287:  leave      IL_0332
          IL_028c:  ldarg.0
          IL_028d:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0292:  stloc.2
          IL_0293:  ldarg.0
          IL_0294:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0299:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_029f:  ldarg.0
          IL_02a0:  ldc.i4.m1
          IL_02a1:  dup
          IL_02a2:  stloc.0
          IL_02a3:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_02a8:  ldloca.s   V_2
          IL_02aa:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
          IL_02af:  ldarg.0
          IL_02b0:  ldarg.0
          IL_02b1:  ldfld      ""int Test.<G>d__0.<x>5__2""
          IL_02b6:  ldc.i4.1
          IL_02b7:  add
          IL_02b8:  stfld      ""int Test.<G>d__0.<x>5__2""
          IL_02bd:  leave.s    IL_02d2
        }
        finally
        {
          IL_02bf:  ldloc.0
          IL_02c0:  ldc.i4.0
          IL_02c1:  bge.s      IL_02d1
          IL_02c3:  ldarg.0
          IL_02c4:  ldarg.0
          IL_02c5:  ldfld      ""int Test.<G>d__0.<x>5__2""
          IL_02ca:  ldc.i4.1
          IL_02cb:  add
          IL_02cc:  stfld      ""int Test.<G>d__0.<x>5__2""
          IL_02d1:  endfinally
        }
        IL_02d2:  leave.s    IL_02e7
      }
      finally
      {
        IL_02d4:  ldloc.0
        IL_02d5:  ldc.i4.0
        IL_02d6:  bge.s      IL_02e6
        IL_02d8:  ldarg.0
        IL_02d9:  ldarg.0
        IL_02da:  ldfld      ""int Test.<G>d__0.<x>5__2""
        IL_02df:  ldc.i4.1
        IL_02e0:  add
        IL_02e1:  stfld      ""int Test.<G>d__0.<x>5__2""
        IL_02e6:  endfinally
      }
      IL_02e7:  leave.s    IL_02fc
    }
    finally
    {
      IL_02e9:  ldloc.0
      IL_02ea:  ldc.i4.0
      IL_02eb:  bge.s      IL_02fb
      IL_02ed:  ldarg.0
      IL_02ee:  ldarg.0
      IL_02ef:  ldfld      ""int Test.<G>d__0.<x>5__2""
      IL_02f4:  ldc.i4.1
      IL_02f5:  add
      IL_02f6:  stfld      ""int Test.<G>d__0.<x>5__2""
      IL_02fb:  endfinally
    }
    IL_02fc:  ldarg.0
    IL_02fd:  ldfld      ""int Test.<G>d__0.<x>5__2""
    IL_0302:  stloc.1
    IL_0303:  leave.s    IL_031e
  }
  catch System.Exception
  {
    IL_0305:  stloc.s    V_4
    IL_0307:  ldarg.0
    IL_0308:  ldc.i4.s   -2
    IL_030a:  stfld      ""int Test.<G>d__0.<>1__state""
    IL_030f:  ldarg.0
    IL_0310:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
    IL_0315:  ldloc.s    V_4
    IL_0317:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_031c:  leave.s    IL_0332
  }
  IL_031e:  ldarg.0
  IL_031f:  ldc.i4.s   -2
  IL_0321:  stfld      ""int Test.<G>d__0.<>1__state""
  IL_0326:  ldarg.0
  IL_0327:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
  IL_032c:  ldloc.1
  IL_032d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_0332:  ret
}
");
        }

        [Fact, WorkItem(855080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/855080")]
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
            NotImplementedException ex = await Goo<NotImplementedException>();
            return 3;
        }
        public static async Task<T> Goo<T>() where T : Exception
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
  // Code size      210 (0xd2)
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
    IL_0008:  brfalse.s  IL_0058
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
    IL_0036:  brtrue.s   IL_0074
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.0
    IL_003c:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_0041:  ldarg.0
    IL_0042:  ldloc.3
    IL_0043:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_004e:  ldloca.s   V_3
    IL_0050:  ldarg.0
    IL_0051:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
    IL_0056:  leave.s    IL_00d1
    IL_0058:  ldarg.0
    IL_0059:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_005e:  stloc.3
    IL_005f:  ldarg.0
    IL_0060:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_0065:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_006b:  ldarg.0
    IL_006c:  ldc.i4.m1
    IL_006d:  dup
    IL_006e:  stloc.0
    IL_006f:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_0074:  ldloca.s   V_3
    IL_0076:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_007b:  ldarg.0
    IL_007c:  ldfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_0081:  stloc.2
    IL_0082:  ldloc.2
    IL_0083:  brfalse.s  IL_009a
    IL_0085:  ldloc.2
    IL_0086:  isinst     ""System.Exception""
    IL_008b:  dup
    IL_008c:  brtrue.s   IL_0090
    IL_008e:  ldloc.2
    IL_008f:  throw
    IL_0090:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_0095:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_009a:  ldarg.0
    IL_009b:  ldnull
    IL_009c:  stfld      ""object Test.<G>d__1.<>7__wrap1""
    IL_00a1:  stloc.1
    IL_00a2:  leave.s    IL_00bd
  }
  catch System.Exception
  {
    IL_00a4:  stloc.s    V_4
    IL_00a6:  ldarg.0
    IL_00a7:  ldc.i4.s   -2
    IL_00a9:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_00ae:  ldarg.0
    IL_00af:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_00b4:  ldloc.s    V_4
    IL_00b6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00bb:  leave.s    IL_00d1
  }
  IL_00bd:  ldarg.0
  IL_00be:  ldc.i4.s   -2
  IL_00c0:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_00c5:  ldarg.0
  IL_00c6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_00cb:  ldloc.1
  IL_00cc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00d1:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
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
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
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
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}";
            var expected = @"FOne or more errors occurred.
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
            var v = CompileAndVerify(source, s_asyncRefs, targetFramework: TargetFramework.Empty, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), expectedOutput: expected, symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<x>5__1",
                    "<>s__2", // pending exception
                    "<>s__3", // pending branch
                    "<>s__4", // return value
                    "<>s__5", // spill
                    "<>s__6", // spill
                    "<>s__7", // spill
                    "<>u__1", // awaiter
                }, module.GetFieldNames("Test.<G>d__1"));
            });

            v.VerifyPdb("Test.G", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""Test"" methodName=""Main"" />
  <methods>
    <method containingType=""Test"" name=""G"">
      <customDebugInfo>
        <forwardIterator name=""&lt;G&gt;d__1"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""22"" offset=""33"" />
          <slot kind=""23"" offset=""33"" />
          <slot kind=""20"" offset=""33"" />
          <slot kind=""28"" offset=""65"" />
          <slot kind=""28"" offset=""156"" />
          <slot kind=""28"" offset=""156"" ordinal=""1"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");

            v.VerifyIL("Test.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"{
  // Code size      461 (0x1cd)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                Test.<G>d__1 V_3,
                object V_4,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
                System.Exception V_6,
                int V_7)
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
    IL_0014:  br         IL_0115
   -IL_0019:  nop
   -IL_001a:  ldarg.0
    IL_001b:  ldc.i4.0
    IL_001c:  stfld      ""int Test.<G>d__1.<x>5__1""
   ~IL_0021:  ldarg.0
    IL_0022:  ldnull
    IL_0023:  stfld      ""object Test.<G>d__1.<>s__2""
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  stfld      ""int Test.<G>d__1.<>s__3""
   ~IL_002f:  nop
    .try
    {
     ~IL_0030:  ldloc.0
      IL_0031:  brfalse.s  IL_0035
      IL_0033:  br.s       IL_0037
      IL_0035:  br.s       IL_0073
     -IL_0037:  nop
     -IL_0038:  call       ""System.Threading.Tasks.Task<int> Test.F()""
      IL_003d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0042:  stloc.2
     ~IL_0043:  ldloca.s   V_2
      IL_0045:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_004a:  brtrue.s   IL_008f
      IL_004c:  ldarg.0
      IL_004d:  ldc.i4.0
      IL_004e:  dup
      IL_004f:  stloc.0
      IL_0050:  stfld      ""int Test.<G>d__1.<>1__state""
     <IL_0055:  ldarg.0
      IL_0056:  ldloc.2
      IL_0057:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
      IL_005c:  ldarg.0
      IL_005d:  stloc.3
      IL_005e:  ldarg.0
      IL_005f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
      IL_0064:  ldloca.s   V_2
      IL_0066:  ldloca.s   V_3
      IL_0068:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
      IL_006d:  nop
      IL_006e:  leave      IL_01cc
     >IL_0073:  ldarg.0
      IL_0074:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
      IL_0079:  stloc.2
      IL_007a:  ldarg.0
      IL_007b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
      IL_0080:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_0086:  ldarg.0
      IL_0087:  ldc.i4.m1
      IL_0088:  dup
      IL_0089:  stloc.0
      IL_008a:  stfld      ""int Test.<G>d__1.<>1__state""
      IL_008f:  ldarg.0
      IL_0090:  ldloca.s   V_2
      IL_0092:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_0097:  stfld      ""int Test.<G>d__1.<>s__5""
      IL_009c:  ldarg.0
      IL_009d:  ldarg.0
      IL_009e:  ldfld      ""int Test.<G>d__1.<>s__5""
      IL_00a3:  stfld      ""int Test.<G>d__1.<x>5__1""
     -IL_00a8:  ldarg.0
      IL_00a9:  ldarg.0
      IL_00aa:  ldfld      ""int Test.<G>d__1.<x>5__1""
      IL_00af:  stfld      ""int Test.<G>d__1.<>s__4""
      IL_00b4:  br.s       IL_00b6
      IL_00b6:  ldarg.0
      IL_00b7:  ldc.i4.1
      IL_00b8:  stfld      ""int Test.<G>d__1.<>s__3""
      IL_00bd:  leave.s    IL_00cb
    }
    catch object
    {
     ~IL_00bf:  stloc.s    V_4
      IL_00c1:  ldarg.0
      IL_00c2:  ldloc.s    V_4
      IL_00c4:  stfld      ""object Test.<G>d__1.<>s__2""
      IL_00c9:  leave.s    IL_00cb
    }
   -IL_00cb:  nop
   -IL_00cc:  ldarg.0
    IL_00cd:  ldarg.0
    IL_00ce:  ldfld      ""int Test.<G>d__1.<x>5__1""
    IL_00d3:  stfld      ""int Test.<G>d__1.<>s__6""
    IL_00d8:  call       ""System.Threading.Tasks.Task<int> Test.F()""
    IL_00dd:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00e2:  stloc.s    V_5
   ~IL_00e4:  ldloca.s   V_5
    IL_00e6:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00eb:  brtrue.s   IL_0132
    IL_00ed:  ldarg.0
    IL_00ee:  ldc.i4.1
    IL_00ef:  dup
    IL_00f0:  stloc.0
    IL_00f1:  stfld      ""int Test.<G>d__1.<>1__state""
   <IL_00f6:  ldarg.0
    IL_00f7:  ldloc.s    V_5
    IL_00f9:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_00fe:  ldarg.0
    IL_00ff:  stloc.3
    IL_0100:  ldarg.0
    IL_0101:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_0106:  ldloca.s   V_5
    IL_0108:  ldloca.s   V_3
    IL_010a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
    IL_010f:  nop
    IL_0110:  leave      IL_01cc
   >IL_0115:  ldarg.0
    IL_0116:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_011b:  stloc.s    V_5
    IL_011d:  ldarg.0
    IL_011e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_0123:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0129:  ldarg.0
    IL_012a:  ldc.i4.m1
    IL_012b:  dup
    IL_012c:  stloc.0
    IL_012d:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_0132:  ldarg.0
    IL_0133:  ldloca.s   V_5
    IL_0135:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_013a:  stfld      ""int Test.<G>d__1.<>s__7""
    IL_013f:  ldarg.0
    IL_0140:  ldarg.0
    IL_0141:  ldfld      ""int Test.<G>d__1.<>s__6""
    IL_0146:  ldarg.0
    IL_0147:  ldfld      ""int Test.<G>d__1.<>s__7""
    IL_014c:  add
    IL_014d:  stfld      ""int Test.<G>d__1.<x>5__1""
   -IL_0152:  nop
   ~IL_0153:  ldarg.0
    IL_0154:  ldfld      ""object Test.<G>d__1.<>s__2""
    IL_0159:  stloc.s    V_4
    IL_015b:  ldloc.s    V_4
    IL_015d:  brfalse.s  IL_017c
    IL_015f:  ldloc.s    V_4
    IL_0161:  isinst     ""System.Exception""
    IL_0166:  stloc.s    V_6
    IL_0168:  ldloc.s    V_6
    IL_016a:  brtrue.s   IL_016f
    IL_016c:  ldloc.s    V_4
    IL_016e:  throw
    IL_016f:  ldloc.s    V_6
    IL_0171:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_0176:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_017b:  nop
    IL_017c:  ldarg.0
    IL_017d:  ldfld      ""int Test.<G>d__1.<>s__3""
    IL_0182:  stloc.s    V_7
    IL_0184:  ldloc.s    V_7
    IL_0186:  ldc.i4.1
    IL_0187:  beq.s      IL_018b
    IL_0189:  br.s       IL_0194
    IL_018b:  ldarg.0
    IL_018c:  ldfld      ""int Test.<G>d__1.<>s__4""
    IL_0191:  stloc.1
    IL_0192:  leave.s    IL_01b7
    IL_0194:  ldarg.0
    IL_0195:  ldnull
    IL_0196:  stfld      ""object Test.<G>d__1.<>s__2""
    IL_019b:  leave.s    IL_01b7
  }
  catch System.Exception
  {
   ~IL_019d:  stloc.s    V_6
    IL_019f:  ldarg.0
    IL_01a0:  ldc.i4.s   -2
    IL_01a2:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_01a7:  ldarg.0
    IL_01a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_01ad:  ldloc.s    V_6
    IL_01af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_01b4:  nop
    IL_01b5:  leave.s    IL_01cc
  }
 -IL_01b7:  ldarg.0
  IL_01b8:  ldc.i4.s   -2
  IL_01ba:  stfld      ""int Test.<G>d__1.<>1__state""
 ~IL_01bf:  ldarg.0
  IL_01c0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_01c5:  ldloc.1
  IL_01c6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_01cb:  nop
  IL_01cc:  ret
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
  // Code size      170 (0xaa)
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
      IL_000f:  ldloc.2
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
    IL_001b:  bne.un.s   IL_0078
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
    IL_003e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_0043:  ldarg.0
    IL_0044:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_0049:  ldloca.s   V_4
    IL_004b:  ldarg.0
    IL_004c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
    IL_0051:  leave.s    IL_00a9
    IL_0053:  ldarg.0
    IL_0054:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_0059:  stloc.s    V_4
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_0061:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0067:  ldarg.0
    IL_0068:  ldc.i4.m1
    IL_0069:  dup
    IL_006a:  stloc.0
    IL_006b:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_0070:  ldloca.s   V_4
    IL_0072:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0077:  stloc.2
    IL_0078:  ldloc.2
    IL_0079:  stloc.1
    IL_007a:  leave.s    IL_0095
  }
  catch System.Exception
  {
    IL_007c:  stloc.s    V_5
    IL_007e:  ldarg.0
    IL_007f:  ldc.i4.s   -2
    IL_0081:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_0086:  ldarg.0
    IL_0087:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_008c:  ldloc.s    V_5
    IL_008e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0093:  leave.s    IL_00a9
  }
  IL_0095:  ldarg.0
  IL_0096:  ldc.i4.s   -2
  IL_0098:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_009d:  ldarg.0
  IL_009e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_00a3:  ldloc.1
  IL_00a4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00a9:  ret
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
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            Task<int> t2 = G();
            t2.Wait(1000 * 60);
            Console.WriteLine(t2.Result);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}";
            var expected = @"
Attempted to divide by zero.
2
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [WorkItem(74, "https://github.com/dotnet/roslyn/issues/1334")]
        [Fact]
        public void AsyncInCatchRethrow01()
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
            catch (ArgumentNullException)
            {
                x = await F();
            }
            catch
            {
                Console.WriteLine(""rethrowing"");
                throw;
            }
        }
        catch(DivideByZeroException ex)
        {
            x += await F();
            System.Console.WriteLine(ex.Message);
        }

        return x;
    }

    public static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            Task<int> t2 = G();
            t2.Wait(1000 * 60);
            Console.WriteLine(t2.Result);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}";
            var expected = @"rethrowing
Attempted to divide by zero.
2
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [WorkItem(74, "https://github.com/dotnet/roslyn/issues/1334")]
        [Fact]
        public void AsyncInCatchRethrow02()
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
            catch (ArgumentNullException)
            {
                Console.WriteLine(""should not get here"");
            }
            catch (Exception ex) when (ex == null)
            {
                Console.WriteLine(""should not get here"");
            }
            catch
            {
                x = await F();
                Console.WriteLine(""rethrowing"");
                throw;
            }
        }
        catch(DivideByZeroException ex)
        {
            x += await F();
            System.Console.WriteLine(ex.Message);
        }

        return x;
    }

    public static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            Task<int> t2 = G();
            t2.Wait(1000 * 60);
            Console.WriteLine(t2.Result);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}";
            var expected = @"rethrowing
Attempted to divide by zero.
4
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
            catch when(x != 0)
            {
                x = await F();
                throw;
            }
        }
        catch(Exception ex) when(x == 0 && ((ex = new Exception(""hello"")) != null))
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
        catch(Exception ex) when(T(()=>ex.Message == null, ref ex))
        {
            x = await F();
            System.Console.WriteLine(ex.Message);
        }
        catch(Exception ex) when(T(()=>ex.Message != null, ref ex))
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
                        catch (DivideByZeroException) when (i < 3)
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
                            catch (DivideByZeroException) when (i < 3)
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
