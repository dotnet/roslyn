// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private static readonly MetadataReference[] s_asyncRefs = new[] { MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929 };

        public CodeGenAsyncEHTests()
        {
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput = null, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions options = null)
        {
            references = (references != null) ? references.Concat(s_asyncRefs) : s_asyncRefs;
            return base.CompileAndVerify(source, expectedOutput: expectedOutput, additionalRefs: references, options: options);
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
  // Code size      867 (0x363)
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
    IL_000d:  stfld      ""int Test.<G>d__0.<x>5__1""
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
        IL_00e8,
        IL_015b,
        IL_01ce,
        IL_0241,
        IL_02b4)
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
          IL_0070:  leave      IL_0362
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
          IL_0098:  ldloca.s   V_2
          IL_009a:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_00a0:  ldarg.0
          IL_00a1:  ldarg.0
          IL_00a2:  ldfld      ""int Test.<G>d__0.<x>5__1""
          IL_00a7:  ldc.i4.1
          IL_00a8:  add
          IL_00a9:  stfld      ""int Test.<G>d__0.<x>5__1""
          IL_00ae:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
          IL_00b3:  stloc.3
          IL_00b4:  ldloca.s   V_3
          IL_00b6:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
          IL_00bb:  stloc.2
          IL_00bc:  ldloca.s   V_2
          IL_00be:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
          IL_00c3:  brtrue.s   IL_0104
          IL_00c5:  ldarg.0
          IL_00c6:  ldc.i4.1
          IL_00c7:  dup
          IL_00c8:  stloc.0
          IL_00c9:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_00ce:  ldarg.0
          IL_00cf:  ldloc.2
          IL_00d0:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_00d5:  ldarg.0
          IL_00d6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
          IL_00db:  ldloca.s   V_2
          IL_00dd:  ldarg.0
          IL_00de:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<G>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<G>d__0)""
          IL_00e3:  leave      IL_0362
          IL_00e8:  ldarg.0
          IL_00e9:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_00ee:  stloc.2
          IL_00ef:  ldarg.0
          IL_00f0:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_00f5:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_00fb:  ldarg.0
          IL_00fc:  ldc.i4.m1
          IL_00fd:  dup
          IL_00fe:  stloc.0
          IL_00ff:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_0104:  ldloca.s   V_2
          IL_0106:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
          IL_010b:  ldloca.s   V_2
          IL_010d:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_0113:  ldarg.0
          IL_0114:  ldarg.0
          IL_0115:  ldfld      ""int Test.<G>d__0.<x>5__1""
          IL_011a:  ldc.i4.1
          IL_011b:  add
          IL_011c:  stfld      ""int Test.<G>d__0.<x>5__1""
          IL_0121:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
          IL_0126:  stloc.3
          IL_0127:  ldloca.s   V_3
          IL_0129:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
          IL_012e:  stloc.2
          IL_012f:  ldloca.s   V_2
          IL_0131:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
          IL_0136:  brtrue.s   IL_0177
          IL_0138:  ldarg.0
          IL_0139:  ldc.i4.2
          IL_013a:  dup
          IL_013b:  stloc.0
          IL_013c:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_0141:  ldarg.0
          IL_0142:  ldloc.2
          IL_0143:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0148:  ldarg.0
          IL_0149:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
          IL_014e:  ldloca.s   V_2
          IL_0150:  ldarg.0
          IL_0151:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<G>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<G>d__0)""
          IL_0156:  leave      IL_0362
          IL_015b:  ldarg.0
          IL_015c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0161:  stloc.2
          IL_0162:  ldarg.0
          IL_0163:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0168:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_016e:  ldarg.0
          IL_016f:  ldc.i4.m1
          IL_0170:  dup
          IL_0171:  stloc.0
          IL_0172:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_0177:  ldloca.s   V_2
          IL_0179:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
          IL_017e:  ldloca.s   V_2
          IL_0180:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_0186:  ldarg.0
          IL_0187:  ldarg.0
          IL_0188:  ldfld      ""int Test.<G>d__0.<x>5__1""
          IL_018d:  ldc.i4.1
          IL_018e:  add
          IL_018f:  stfld      ""int Test.<G>d__0.<x>5__1""
          IL_0194:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
          IL_0199:  stloc.3
          IL_019a:  ldloca.s   V_3
          IL_019c:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
          IL_01a1:  stloc.2
          IL_01a2:  ldloca.s   V_2
          IL_01a4:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
          IL_01a9:  brtrue.s   IL_01ea
          IL_01ab:  ldarg.0
          IL_01ac:  ldc.i4.3
          IL_01ad:  dup
          IL_01ae:  stloc.0
          IL_01af:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_01b4:  ldarg.0
          IL_01b5:  ldloc.2
          IL_01b6:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_01bb:  ldarg.0
          IL_01bc:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
          IL_01c1:  ldloca.s   V_2
          IL_01c3:  ldarg.0
          IL_01c4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<G>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<G>d__0)""
          IL_01c9:  leave      IL_0362
          IL_01ce:  ldarg.0
          IL_01cf:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_01d4:  stloc.2
          IL_01d5:  ldarg.0
          IL_01d6:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_01db:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_01e1:  ldarg.0
          IL_01e2:  ldc.i4.m1
          IL_01e3:  dup
          IL_01e4:  stloc.0
          IL_01e5:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_01ea:  ldloca.s   V_2
          IL_01ec:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
          IL_01f1:  ldloca.s   V_2
          IL_01f3:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_01f9:  ldarg.0
          IL_01fa:  ldarg.0
          IL_01fb:  ldfld      ""int Test.<G>d__0.<x>5__1""
          IL_0200:  ldc.i4.1
          IL_0201:  add
          IL_0202:  stfld      ""int Test.<G>d__0.<x>5__1""
          IL_0207:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
          IL_020c:  stloc.3
          IL_020d:  ldloca.s   V_3
          IL_020f:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
          IL_0214:  stloc.2
          IL_0215:  ldloca.s   V_2
          IL_0217:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
          IL_021c:  brtrue.s   IL_025d
          IL_021e:  ldarg.0
          IL_021f:  ldc.i4.4
          IL_0220:  dup
          IL_0221:  stloc.0
          IL_0222:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_0227:  ldarg.0
          IL_0228:  ldloc.2
          IL_0229:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_022e:  ldarg.0
          IL_022f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
          IL_0234:  ldloca.s   V_2
          IL_0236:  ldarg.0
          IL_0237:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<G>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<G>d__0)""
          IL_023c:  leave      IL_0362
          IL_0241:  ldarg.0
          IL_0242:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_0247:  stloc.2
          IL_0248:  ldarg.0
          IL_0249:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_024e:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_0254:  ldarg.0
          IL_0255:  ldc.i4.m1
          IL_0256:  dup
          IL_0257:  stloc.0
          IL_0258:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_025d:  ldloca.s   V_2
          IL_025f:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
          IL_0264:  ldloca.s   V_2
          IL_0266:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_026c:  ldarg.0
          IL_026d:  ldarg.0
          IL_026e:  ldfld      ""int Test.<G>d__0.<x>5__1""
          IL_0273:  ldc.i4.1
          IL_0274:  add
          IL_0275:  stfld      ""int Test.<G>d__0.<x>5__1""
          IL_027a:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
          IL_027f:  stloc.3
          IL_0280:  ldloca.s   V_3
          IL_0282:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
          IL_0287:  stloc.2
          IL_0288:  ldloca.s   V_2
          IL_028a:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
          IL_028f:  brtrue.s   IL_02d0
          IL_0291:  ldarg.0
          IL_0292:  ldc.i4.5
          IL_0293:  dup
          IL_0294:  stloc.0
          IL_0295:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_029a:  ldarg.0
          IL_029b:  ldloc.2
          IL_029c:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_02a1:  ldarg.0
          IL_02a2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
          IL_02a7:  ldloca.s   V_2
          IL_02a9:  ldarg.0
          IL_02aa:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<G>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<G>d__0)""
          IL_02af:  leave      IL_0362
          IL_02b4:  ldarg.0
          IL_02b5:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_02ba:  stloc.2
          IL_02bb:  ldarg.0
          IL_02bc:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<G>d__0.<>u__1""
          IL_02c1:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_02c7:  ldarg.0
          IL_02c8:  ldc.i4.m1
          IL_02c9:  dup
          IL_02ca:  stloc.0
          IL_02cb:  stfld      ""int Test.<G>d__0.<>1__state""
          IL_02d0:  ldloca.s   V_2
          IL_02d2:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
          IL_02d7:  ldloca.s   V_2
          IL_02d9:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
          IL_02df:  ldarg.0
          IL_02e0:  ldarg.0
          IL_02e1:  ldfld      ""int Test.<G>d__0.<x>5__1""
          IL_02e6:  ldc.i4.1
          IL_02e7:  add
          IL_02e8:  stfld      ""int Test.<G>d__0.<x>5__1""
          IL_02ed:  leave.s    IL_0302
        }
        finally
        {
          IL_02ef:  ldloc.0
          IL_02f0:  ldc.i4.0
          IL_02f1:  bge.s      IL_0301
          IL_02f3:  ldarg.0
          IL_02f4:  ldarg.0
          IL_02f5:  ldfld      ""int Test.<G>d__0.<x>5__1""
          IL_02fa:  ldc.i4.1
          IL_02fb:  add
          IL_02fc:  stfld      ""int Test.<G>d__0.<x>5__1""
          IL_0301:  endfinally
        }
        IL_0302:  leave.s    IL_0317
      }
      finally
      {
        IL_0304:  ldloc.0
        IL_0305:  ldc.i4.0
        IL_0306:  bge.s      IL_0316
        IL_0308:  ldarg.0
        IL_0309:  ldarg.0
        IL_030a:  ldfld      ""int Test.<G>d__0.<x>5__1""
        IL_030f:  ldc.i4.1
        IL_0310:  add
        IL_0311:  stfld      ""int Test.<G>d__0.<x>5__1""
        IL_0316:  endfinally
      }
      IL_0317:  leave.s    IL_032c
    }
    finally
    {
      IL_0319:  ldloc.0
      IL_031a:  ldc.i4.0
      IL_031b:  bge.s      IL_032b
      IL_031d:  ldarg.0
      IL_031e:  ldarg.0
      IL_031f:  ldfld      ""int Test.<G>d__0.<x>5__1""
      IL_0324:  ldc.i4.1
      IL_0325:  add
      IL_0326:  stfld      ""int Test.<G>d__0.<x>5__1""
      IL_032b:  endfinally
    }
    IL_032c:  ldarg.0
    IL_032d:  ldfld      ""int Test.<G>d__0.<x>5__1""
    IL_0332:  stloc.1
    IL_0333:  leave.s    IL_034e
  }
  catch System.Exception
  {
    IL_0335:  stloc.s    V_4
    IL_0337:  ldarg.0
    IL_0338:  ldc.i4.s   -2
    IL_033a:  stfld      ""int Test.<G>d__0.<>1__state""
    IL_033f:  ldarg.0
    IL_0340:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
    IL_0345:  ldloc.s    V_4
    IL_0347:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_034c:  leave.s    IL_0362
  }
  IL_034e:  ldarg.0
  IL_034f:  ldc.i4.s   -2
  IL_0351:  stfld      ""int Test.<G>d__0.<>1__state""
  IL_0356:  ldarg.0
  IL_0357:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__0.<>t__builder""
  IL_035c:  ldloc.1
  IL_035d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_0362:  ret
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
    IL_0043:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_004e:  ldloca.s   V_3
    IL_0050:  ldarg.0
    IL_0051:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
    IL_0056:  leave      IL_00dc
    IL_005b:  ldarg.0
    IL_005c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_0061:  stloc.3
    IL_0062:  ldarg.0
    IL_0063:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
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
            var v = CompileAndVerify(source, s_asyncRefs, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), expectedOutput: expected, symbolValidator: module =>
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

            v.VerifyIL("Test.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      481 (0x1e1)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                int V_3,
                Test.<G>d__1 V_4,
                object V_5,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_6,
                System.Exception V_7)
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
    IL_0014:  br         IL_0121
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
      IL_0035:  br.s       IL_0074
     -IL_0037:  nop
     -IL_0038:  call       ""System.Threading.Tasks.Task<int> Test.F()""
      IL_003d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0042:  stloc.2
     ~IL_0043:  ldloca.s   V_2
      IL_0045:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_004a:  brtrue.s   IL_0090
      IL_004c:  ldarg.0
      IL_004d:  ldc.i4.0
      IL_004e:  dup
      IL_004f:  stloc.0
      IL_0050:  stfld      ""int Test.<G>d__1.<>1__state""
     <IL_0055:  ldarg.0
      IL_0056:  ldloc.2
      IL_0057:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
      IL_005c:  ldarg.0
      IL_005d:  stloc.s    V_4
      IL_005f:  ldarg.0
      IL_0060:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
      IL_0065:  ldloca.s   V_2
      IL_0067:  ldloca.s   V_4
      IL_0069:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
      IL_006e:  nop
      IL_006f:  leave      IL_01e0
     >IL_0074:  ldarg.0
      IL_0075:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
      IL_007a:  stloc.2
      IL_007b:  ldarg.0
      IL_007c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
      IL_0081:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_0087:  ldarg.0
      IL_0088:  ldc.i4.m1
      IL_0089:  dup
      IL_008a:  stloc.0
      IL_008b:  stfld      ""int Test.<G>d__1.<>1__state""
      IL_0090:  ldloca.s   V_2
      IL_0092:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_0097:  stloc.3
      IL_0098:  ldloca.s   V_2
      IL_009a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00a0:  ldarg.0
      IL_00a1:  ldloc.3
      IL_00a2:  stfld      ""int Test.<G>d__1.<>s__5""
      IL_00a7:  ldarg.0
      IL_00a8:  ldarg.0
      IL_00a9:  ldfld      ""int Test.<G>d__1.<>s__5""
      IL_00ae:  stfld      ""int Test.<G>d__1.<x>5__1""
     -IL_00b3:  ldarg.0
      IL_00b4:  ldarg.0
      IL_00b5:  ldfld      ""int Test.<G>d__1.<x>5__1""
      IL_00ba:  stfld      ""int Test.<G>d__1.<>s__4""
      IL_00bf:  br.s       IL_00c1
      IL_00c1:  ldarg.0
      IL_00c2:  ldc.i4.1
      IL_00c3:  stfld      ""int Test.<G>d__1.<>s__3""
      IL_00c8:  leave.s    IL_00d6
    }
    catch object
    {
     ~IL_00ca:  stloc.s    V_5
      IL_00cc:  ldarg.0
      IL_00cd:  ldloc.s    V_5
      IL_00cf:  stfld      ""object Test.<G>d__1.<>s__2""
      IL_00d4:  leave.s    IL_00d6
    }
   -IL_00d6:  nop
   -IL_00d7:  ldarg.0
    IL_00d8:  ldarg.0
    IL_00d9:  ldfld      ""int Test.<G>d__1.<x>5__1""
    IL_00de:  stfld      ""int Test.<G>d__1.<>s__6""
    IL_00e3:  call       ""System.Threading.Tasks.Task<int> Test.F()""
    IL_00e8:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00ed:  stloc.s    V_6
   ~IL_00ef:  ldloca.s   V_6
    IL_00f1:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00f6:  brtrue.s   IL_013e
    IL_00f8:  ldarg.0
    IL_00f9:  ldc.i4.1
    IL_00fa:  dup
    IL_00fb:  stloc.0
    IL_00fc:  stfld      ""int Test.<G>d__1.<>1__state""
   <IL_0101:  ldarg.0
    IL_0102:  ldloc.s    V_6
    IL_0104:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_0109:  ldarg.0
    IL_010a:  stloc.s    V_4
    IL_010c:  ldarg.0
    IL_010d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_0112:  ldloca.s   V_6
    IL_0114:  ldloca.s   V_4
    IL_0116:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
    IL_011b:  nop
    IL_011c:  leave      IL_01e0
   >IL_0121:  ldarg.0
    IL_0122:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_0127:  stloc.s    V_6
    IL_0129:  ldarg.0
    IL_012a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_012f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0135:  ldarg.0
    IL_0136:  ldc.i4.m1
    IL_0137:  dup
    IL_0138:  stloc.0
    IL_0139:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_013e:  ldloca.s   V_6
    IL_0140:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0145:  stloc.3
    IL_0146:  ldloca.s   V_6
    IL_0148:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_014e:  ldarg.0
    IL_014f:  ldloc.3
    IL_0150:  stfld      ""int Test.<G>d__1.<>s__7""
    IL_0155:  ldarg.0
    IL_0156:  ldarg.0
    IL_0157:  ldfld      ""int Test.<G>d__1.<>s__6""
    IL_015c:  ldarg.0
    IL_015d:  ldfld      ""int Test.<G>d__1.<>s__7""
    IL_0162:  add
    IL_0163:  stfld      ""int Test.<G>d__1.<x>5__1""
   -IL_0168:  nop
   ~IL_0169:  ldarg.0
    IL_016a:  ldfld      ""object Test.<G>d__1.<>s__2""
    IL_016f:  stloc.s    V_5
    IL_0171:  ldloc.s    V_5
    IL_0173:  brfalse.s  IL_0192
    IL_0175:  ldloc.s    V_5
    IL_0177:  isinst     ""System.Exception""
    IL_017c:  stloc.s    V_7
    IL_017e:  ldloc.s    V_7
    IL_0180:  brtrue.s   IL_0185
    IL_0182:  ldloc.s    V_5
    IL_0184:  throw
    IL_0185:  ldloc.s    V_7
    IL_0187:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_018c:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_0191:  nop
    IL_0192:  ldarg.0
    IL_0193:  ldfld      ""int Test.<G>d__1.<>s__3""
    IL_0198:  stloc.3
    IL_0199:  ldloc.3
    IL_019a:  ldc.i4.1
    IL_019b:  beq.s      IL_019f
    IL_019d:  br.s       IL_01a8
    IL_019f:  ldarg.0
    IL_01a0:  ldfld      ""int Test.<G>d__1.<>s__4""
    IL_01a5:  stloc.1
    IL_01a6:  leave.s    IL_01cb
    IL_01a8:  ldarg.0
    IL_01a9:  ldnull
    IL_01aa:  stfld      ""object Test.<G>d__1.<>s__2""
    IL_01af:  leave.s    IL_01cb
  }
  catch System.Exception
  {
   ~IL_01b1:  stloc.s    V_7
    IL_01b3:  ldarg.0
    IL_01b4:  ldc.i4.s   -2
    IL_01b6:  stfld      ""int Test.<G>d__1.<>1__state""
    IL_01bb:  ldarg.0
    IL_01bc:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_01c1:  ldloc.s    V_7
    IL_01c3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_01c8:  nop
    IL_01c9:  leave.s    IL_01e0
  }
 -IL_01cb:  ldarg.0
  IL_01cc:  ldc.i4.s   -2
  IL_01ce:  stfld      ""int Test.<G>d__1.<>1__state""
 ~IL_01d3:  ldarg.0
  IL_01d4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_01d9:  ldloc.1
  IL_01da:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_01df:  nop
  IL_01e0:  ret
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
    IL_003e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1""
    IL_0043:  ldarg.0
    IL_0044:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
    IL_0049:  ldloca.s   V_4
    IL_004b:  ldarg.0
    IL_004c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
    IL_0051:  leave.s    IL_00b1
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
