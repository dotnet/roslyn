// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    [CompilerTrait(CompilerFeature.Async)]
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.FailsPEVerify);
            verifier.VerifyDiagnostics(
                // (3,1): hidden CS8019: Unnecessary using directive.
                // using System.Diagnostics;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Diagnostics;").WithLocation(3, 1)
            );
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [G]: Unexpected type on the stack. { Offset = 0x104, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem(855080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/855080")]
        public void GenericCatchVariableInAsyncMethod()
        {
            var source = @"
using System;
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("3"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Bar]: Unexpected type on the stack. { Offset = 0xc, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [Goo]: Unexpected type on the stack. { Offset = 0x15, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    """
            });
            verifier.VerifyDiagnostics();
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

            var comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(
                comp,
                expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [G]: Unexpected type on the stack. { Offset = 0x13, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [H]: Unexpected type on the stack. { Offset = 0xa, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyDiagnostics();
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x1, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0x29, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       42 (0x2a)
                  .maxstack  3
                  .locals init (object V_0)
                  IL_0000:  ldnull
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  leave.s    IL_0007
                  }
                  catch object
                  {
                    IL_0004:  stloc.0
                    IL_0005:  leave.s    IL_0007
                  }
                  IL_0007:  call       "System.Threading.Tasks.Task<int> Test.F()"
                  IL_000c:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0011:  ldloc.0
                  IL_0012:  brfalse.s  IL_0029
                  IL_0014:  ldloc.0
                  IL_0015:  isinst     "System.Exception"
                  IL_001a:  dup
                  IL_001b:  brtrue.s   IL_001f
                  IL_001d:  ldloc.0
                  IL_001e:  throw
                  IL_001f:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0024:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0029:  ret
                }
                """);
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
            var expected = ExecutionConditionUtil.IsWindowsDesktop
                ? @"FOne or more errors occurred."
                : @"FOne or more errors occurred. (hello)";

            CompileAndVerify(source, expectedOutput: expected);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0xb, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       59 (0x3b)
                  .maxstack  2
                  .locals init (int V_0, //x
                                object V_1,
                                int V_2)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  IL_0002:  ldnull
                  IL_0003:  stloc.1
                  .try
                  {
                    IL_0004:  ldstr      "hello"
                    IL_0009:  newobj     "System.Exception..ctor(string)"
                    IL_000e:  throw
                  }
                  catch object
                  {
                    IL_000f:  stloc.1
                    IL_0010:  leave.s    IL_0012
                  }
                  IL_0012:  ldloc.0
                  IL_0013:  call       "System.Threading.Tasks.Task<int> Test.F()"
                  IL_0018:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_001d:  stloc.2
                  IL_001e:  ldloc.2
                  IL_001f:  add
                  IL_0020:  stloc.0
                  IL_0021:  ldloc.1
                  IL_0022:  brfalse.s  IL_0039
                  IL_0024:  ldloc.1
                  IL_0025:  isinst     "System.Exception"
                  IL_002a:  dup
                  IL_002b:  brtrue.s   IL_002f
                  IL_002d:  ldloc.1
                  IL_002e:  throw
                  IL_002f:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0034:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0039:  ldnull
                  IL_003a:  throw
                }
                """);
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

            // Native PDBs require desktop
            if (ExecutionConditionUtil.IsWindowsDesktop)
            {
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
        <encStateMachineStateMap>
          <state number=""0"" offset=""65"" />
          <state number=""1"" offset=""156"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");
            }

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
    IL_019b:  ldnull
    IL_019c:  throw
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
}", sequencePointDisplay: SequencePointDisplayMode.Minimal);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x1, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G]: Unexpected type on the stack. { Offset = 0x48, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       75 (0x4b)
                  .maxstack  2
                  .locals init (int V_0, //x
                                object V_1,
                                int V_2,
                                int V_3,
                                int V_4)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  IL_0002:  ldnull
                  IL_0003:  stloc.1
                  IL_0004:  ldc.i4.0
                  IL_0005:  stloc.2
                  .try
                  {
                    IL_0006:  call       "System.Threading.Tasks.Task<int> Test.F()"
                    IL_000b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0010:  stloc.0
                    IL_0011:  ldloc.0
                    IL_0012:  stloc.3
                    IL_0013:  ldc.i4.1
                    IL_0014:  stloc.2
                    IL_0015:  leave.s    IL_001a
                  }
                  catch object
                  {
                    IL_0017:  stloc.1
                    IL_0018:  leave.s    IL_001a
                  }
                  IL_001a:  ldloc.0
                  IL_001b:  call       "System.Threading.Tasks.Task<int> Test.F()"
                  IL_0020:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0025:  stloc.s    V_4
                  IL_0027:  ldloc.s    V_4
                  IL_0029:  add
                  IL_002a:  stloc.0
                  IL_002b:  ldloc.1
                  IL_002c:  brfalse.s  IL_0043
                  IL_002e:  ldloc.1
                  IL_002f:  isinst     "System.Exception"
                  IL_0034:  dup
                  IL_0035:  brtrue.s   IL_0039
                  IL_0037:  ldloc.1
                  IL_0038:  throw
                  IL_0039:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_003e:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0043:  ldloc.2
                  IL_0044:  ldc.i4.1
                  IL_0045:  bne.un.s   IL_0049
                  IL_0047:  ldloc.3
                  IL_0048:  ret
                  IL_0049:  ldnull
                  IL_004a:  throw
                }
                """);
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x1, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0x3e, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       63 (0x3f)
                  .maxstack  2
                  .locals init (int V_0, //x
                                object V_1,
                                int V_2)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldnull
                    IL_0003:  stloc.1
                    .try
                    {
                      IL_0004:  newobj     "System.Exception..ctor()"
                      IL_0009:  throw
                    }
                    catch object
                    {
                      IL_000a:  stloc.1
                      IL_000b:  leave.s    IL_000d
                    }
                    IL_000d:  ldloc.0
                    IL_000e:  call       "System.Threading.Tasks.Task<int> Test.F()"
                    IL_0013:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0018:  stloc.2
                    IL_0019:  ldloc.2
                    IL_001a:  add
                    IL_001b:  stloc.0
                    IL_001c:  ldloc.1
                    IL_001d:  brfalse.s  IL_0034
                    IL_001f:  ldloc.1
                    IL_0020:  isinst     "System.Exception"
                    IL_0025:  dup
                    IL_0026:  brtrue.s   IL_002a
                    IL_0028:  ldloc.1
                    IL_0029:  throw
                    IL_002a:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_002f:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0034:  leave.s    IL_003b
                  }
                  catch object
                  {
                    IL_0036:  pop
                    IL_0037:  ldloc.0
                    IL_0038:  stloc.2
                    IL_0039:  leave.s    IL_003d
                  }
                  IL_003b:  ldnull
                  IL_003c:  throw
                  IL_003d:  ldloc.2
                  IL_003e:  ret
                }
                """);
        }

        [Fact]
        public void AsyncInFinally005()
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
            throw new Exception(x.ToString());
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
        Task<int> t2 = G();
        try
        {
            t2.Wait(1000 * 60);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}";
            var expected = "One or more errors occurred.";
            if (!ExecutionConditionUtil.IsDesktop)
            {
                expected += " (2)";
            }
            var verifier = CompileAndVerify(source, expectedOutput: expected);
            verifier.VerifyIL("Test.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
                {
                  // Code size      353 (0x161)
                  .maxstack  3
                  .locals init (int V_0,
                                int V_1,
                                int V_2,
                                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                                object V_4,
                                System.Exception V_5)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "int Test.<G>d__1.<>1__state"
                  IL_0006:  stloc.0
                  .try
                  {
                    IL_0007:  ldloc.0
                    IL_0008:  brfalse.s  IL_0026
                    IL_000a:  ldloc.0
                    IL_000b:  ldc.i4.1
                    IL_000c:  beq        IL_00e9
                    IL_0011:  ldarg.0
                    IL_0012:  ldc.i4.0
                    IL_0013:  stfld      "int Test.<G>d__1.<x>5__2"
                    IL_0018:  ldarg.0
                    IL_0019:  ldnull
                    IL_001a:  stfld      "object Test.<G>d__1.<>7__wrap2"
                    IL_001f:  ldarg.0
                    IL_0020:  ldc.i4.0
                    IL_0021:  stfld      "int Test.<G>d__1.<>7__wrap3"
                    IL_0026:  nop
                    .try
                    {
                      IL_0027:  ldloc.0
                      IL_0028:  brfalse.s  IL_0061
                      IL_002a:  call       "System.Threading.Tasks.Task<int> Test.F()"
                      IL_002f:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                      IL_0034:  stloc.3
                      IL_0035:  ldloca.s   V_3
                      IL_0037:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                      IL_003c:  brtrue.s   IL_007d
                      IL_003e:  ldarg.0
                      IL_003f:  ldc.i4.0
                      IL_0040:  dup
                      IL_0041:  stloc.0
                      IL_0042:  stfld      "int Test.<G>d__1.<>1__state"
                      IL_0047:  ldarg.0
                      IL_0048:  ldloc.3
                      IL_0049:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                      IL_004e:  ldarg.0
                      IL_004f:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder"
                      IL_0054:  ldloca.s   V_3
                      IL_0056:  ldarg.0
                      IL_0057:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)"
                      IL_005c:  leave      IL_0160
                      IL_0061:  ldarg.0
                      IL_0062:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                      IL_0067:  stloc.3
                      IL_0068:  ldarg.0
                      IL_0069:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                      IL_006e:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                      IL_0074:  ldarg.0
                      IL_0075:  ldc.i4.m1
                      IL_0076:  dup
                      IL_0077:  stloc.0
                      IL_0078:  stfld      "int Test.<G>d__1.<>1__state"
                      IL_007d:  ldloca.s   V_3
                      IL_007f:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                      IL_0084:  stloc.2
                      IL_0085:  ldarg.0
                      IL_0086:  ldloc.2
                      IL_0087:  stfld      "int Test.<G>d__1.<x>5__2"
                      IL_008c:  ldarg.0
                      IL_008d:  ldflda     "int Test.<G>d__1.<x>5__2"
                      IL_0092:  call       "string int.ToString()"
                      IL_0097:  newobj     "System.Exception..ctor(string)"
                      IL_009c:  throw
                    }
                    catch object
                    {
                      IL_009d:  stloc.s    V_4
                      IL_009f:  ldarg.0
                      IL_00a0:  ldloc.s    V_4
                      IL_00a2:  stfld      "object Test.<G>d__1.<>7__wrap2"
                      IL_00a7:  leave.s    IL_00a9
                    }
                    IL_00a9:  ldarg.0
                    IL_00aa:  ldarg.0
                    IL_00ab:  ldfld      "int Test.<G>d__1.<x>5__2"
                    IL_00b0:  stfld      "int Test.<G>d__1.<>7__wrap4"
                    IL_00b5:  call       "System.Threading.Tasks.Task<int> Test.F()"
                    IL_00ba:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                    IL_00bf:  stloc.3
                    IL_00c0:  ldloca.s   V_3
                    IL_00c2:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                    IL_00c7:  brtrue.s   IL_0105
                    IL_00c9:  ldarg.0
                    IL_00ca:  ldc.i4.1
                    IL_00cb:  dup
                    IL_00cc:  stloc.0
                    IL_00cd:  stfld      "int Test.<G>d__1.<>1__state"
                    IL_00d2:  ldarg.0
                    IL_00d3:  ldloc.3
                    IL_00d4:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                    IL_00d9:  ldarg.0
                    IL_00da:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder"
                    IL_00df:  ldloca.s   V_3
                    IL_00e1:  ldarg.0
                    IL_00e2:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)"
                    IL_00e7:  leave.s    IL_0160
                    IL_00e9:  ldarg.0
                    IL_00ea:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                    IL_00ef:  stloc.3
                    IL_00f0:  ldarg.0
                    IL_00f1:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                    IL_00f6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                    IL_00fc:  ldarg.0
                    IL_00fd:  ldc.i4.m1
                    IL_00fe:  dup
                    IL_00ff:  stloc.0
                    IL_0100:  stfld      "int Test.<G>d__1.<>1__state"
                    IL_0105:  ldloca.s   V_3
                    IL_0107:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                    IL_010c:  stloc.2
                    IL_010d:  ldarg.0
                    IL_010e:  ldarg.0
                    IL_010f:  ldfld      "int Test.<G>d__1.<>7__wrap4"
                    IL_0114:  ldloc.2
                    IL_0115:  add
                    IL_0116:  stfld      "int Test.<G>d__1.<x>5__2"
                    IL_011b:  ldarg.0
                    IL_011c:  ldfld      "object Test.<G>d__1.<>7__wrap2"
                    IL_0121:  stloc.s    V_4
                    IL_0123:  ldloc.s    V_4
                    IL_0125:  brfalse.s  IL_013e
                    IL_0127:  ldloc.s    V_4
                    IL_0129:  isinst     "System.Exception"
                    IL_012e:  dup
                    IL_012f:  brtrue.s   IL_0134
                    IL_0131:  ldloc.s    V_4
                    IL_0133:  throw
                    IL_0134:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_0139:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_013e:  ldarg.0
                    IL_013f:  ldnull
                    IL_0140:  stfld      "object Test.<G>d__1.<>7__wrap2"
                    IL_0145:  ldnull
                    IL_0146:  throw
                  }
                  catch System.Exception
                  {
                    IL_0147:  stloc.s    V_5
                    IL_0149:  ldarg.0
                    IL_014a:  ldc.i4.s   -2
                    IL_014c:  stfld      "int Test.<G>d__1.<>1__state"
                    IL_0151:  ldarg.0
                    IL_0152:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder"
                    IL_0157:  ldloc.s    V_5
                    IL_0159:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)"
                    IL_015e:  leave.s    IL_0160
                  }
                  IL_0160:  ret
                }
                """);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x1, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       72 (0x48)
                  .maxstack  2
                  .locals init (int V_0, //x
                                object V_1,
                                int V_2)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  IL_0002:  ldnull
                  IL_0003:  stloc.1
                  .try
                  {
                    IL_0004:  call       "System.Threading.Tasks.Task<int> Test.F()"
                    IL_0009:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_000e:  stloc.0
                    IL_000f:  ldloca.s   V_0
                    IL_0011:  call       "string int.ToString()"
                    IL_0016:  newobj     "System.Exception..ctor(string)"
                    IL_001b:  throw
                  }
                  catch object
                  {
                    IL_001c:  stloc.1
                    IL_001d:  leave.s    IL_001f
                  }
                  IL_001f:  ldloc.0
                  IL_0020:  call       "System.Threading.Tasks.Task<int> Test.F()"
                  IL_0025:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_002a:  stloc.2
                  IL_002b:  ldloc.2
                  IL_002c:  add
                  IL_002d:  stloc.0
                  IL_002e:  ldloc.1
                  IL_002f:  brfalse.s  IL_0046
                  IL_0031:  ldloc.1
                  IL_0032:  isinst     "System.Exception"
                  IL_0037:  dup
                  IL_0038:  brtrue.s   IL_003c
                  IL_003a:  ldloc.1
                  IL_003b:  throw
                  IL_003c:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0041:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0046:  ldnull
                  IL_0047:  throw
                }
                """);
        }

        [Fact]
        public void AsyncInFinally006_AsyncVoid_01()
        {
            var source = """
                using System;
                using System.Threading;
                using System.Threading.Tasks;
                class Test
                {
                    static async Task<int> F()
                    {
                        return 2;
                    }
                    static async void G(SemaphoreSlim semaphore)
                    {
                        int x = 0;
                        try
                        {
                            x = await F();
                        }
                        finally
                        {
                            x += await F();
                            Console.WriteLine(x);
                            semaphore.Release();
                        }
                    }
                    public static void Main()
                    {
                        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
                        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
                        try
                        {
                            var semaphore = new SemaphoreSlim(0, 1);
                            G(semaphore);
                            semaphore.Wait(1000 * 60);
                        }
                        finally
                        {
                            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
                        }
                    }
                }
                """;
            var expected = "4";
            var verifier = CompileAndVerify(source, expectedOutput: expected);
            verifier.VerifyIL("Test.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
                {
                  // Code size      377 (0x179)
                  .maxstack  3
                  .locals init (int V_0,
                                int V_1,
                                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                                object V_3,
                                System.Exception V_4)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "int Test.<G>d__1.<>1__state"
                  IL_0006:  stloc.0
                  .try
                  {
                    IL_0007:  ldloc.0
                    IL_0008:  brfalse.s  IL_0026
                    IL_000a:  ldloc.0
                    IL_000b:  ldc.i4.1
                    IL_000c:  beq        IL_00db
                    IL_0011:  ldarg.0
                    IL_0012:  ldc.i4.0
                    IL_0013:  stfld      "int Test.<G>d__1.<x>5__2"
                    IL_0018:  ldarg.0
                    IL_0019:  ldnull
                    IL_001a:  stfld      "object Test.<G>d__1.<>7__wrap2"
                    IL_001f:  ldarg.0
                    IL_0020:  ldc.i4.0
                    IL_0021:  stfld      "int Test.<G>d__1.<>7__wrap3"
                    IL_0026:  nop
                    .try
                    {
                      IL_0027:  ldloc.0
                      IL_0028:  brfalse.s  IL_0061
                      IL_002a:  call       "System.Threading.Tasks.Task<int> Test.F()"
                      IL_002f:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                      IL_0034:  stloc.2
                      IL_0035:  ldloca.s   V_2
                      IL_0037:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                      IL_003c:  brtrue.s   IL_007d
                      IL_003e:  ldarg.0
                      IL_003f:  ldc.i4.0
                      IL_0040:  dup
                      IL_0041:  stloc.0
                      IL_0042:  stfld      "int Test.<G>d__1.<>1__state"
                      IL_0047:  ldarg.0
                      IL_0048:  ldloc.2
                      IL_0049:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                      IL_004e:  ldarg.0
                      IL_004f:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                      IL_0054:  ldloca.s   V_2
                      IL_0056:  ldarg.0
                      IL_0057:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)"
                      IL_005c:  leave      IL_0178
                      IL_0061:  ldarg.0
                      IL_0062:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                      IL_0067:  stloc.2
                      IL_0068:  ldarg.0
                      IL_0069:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                      IL_006e:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                      IL_0074:  ldarg.0
                      IL_0075:  ldc.i4.m1
                      IL_0076:  dup
                      IL_0077:  stloc.0
                      IL_0078:  stfld      "int Test.<G>d__1.<>1__state"
                      IL_007d:  ldloca.s   V_2
                      IL_007f:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                      IL_0084:  stloc.1
                      IL_0085:  ldarg.0
                      IL_0086:  ldloc.1
                      IL_0087:  stfld      "int Test.<G>d__1.<x>5__2"
                      IL_008c:  leave.s    IL_0098
                    }
                    catch object
                    {
                      IL_008e:  stloc.3
                      IL_008f:  ldarg.0
                      IL_0090:  ldloc.3
                      IL_0091:  stfld      "object Test.<G>d__1.<>7__wrap2"
                      IL_0096:  leave.s    IL_0098
                    }
                    IL_0098:  ldarg.0
                    IL_0099:  ldarg.0
                    IL_009a:  ldfld      "int Test.<G>d__1.<x>5__2"
                    IL_009f:  stfld      "int Test.<G>d__1.<>7__wrap4"
                    IL_00a4:  call       "System.Threading.Tasks.Task<int> Test.F()"
                    IL_00a9:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                    IL_00ae:  stloc.2
                    IL_00af:  ldloca.s   V_2
                    IL_00b1:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                    IL_00b6:  brtrue.s   IL_00f7
                    IL_00b8:  ldarg.0
                    IL_00b9:  ldc.i4.1
                    IL_00ba:  dup
                    IL_00bb:  stloc.0
                    IL_00bc:  stfld      "int Test.<G>d__1.<>1__state"
                    IL_00c1:  ldarg.0
                    IL_00c2:  ldloc.2
                    IL_00c3:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                    IL_00c8:  ldarg.0
                    IL_00c9:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                    IL_00ce:  ldloca.s   V_2
                    IL_00d0:  ldarg.0
                    IL_00d1:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)"
                    IL_00d6:  leave      IL_0178
                    IL_00db:  ldarg.0
                    IL_00dc:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                    IL_00e1:  stloc.2
                    IL_00e2:  ldarg.0
                    IL_00e3:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                    IL_00e8:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                    IL_00ee:  ldarg.0
                    IL_00ef:  ldc.i4.m1
                    IL_00f0:  dup
                    IL_00f1:  stloc.0
                    IL_00f2:  stfld      "int Test.<G>d__1.<>1__state"
                    IL_00f7:  ldloca.s   V_2
                    IL_00f9:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                    IL_00fe:  stloc.1
                    IL_00ff:  ldarg.0
                    IL_0100:  ldarg.0
                    IL_0101:  ldfld      "int Test.<G>d__1.<>7__wrap4"
                    IL_0106:  ldloc.1
                    IL_0107:  add
                    IL_0108:  stfld      "int Test.<G>d__1.<x>5__2"
                    IL_010d:  ldarg.0
                    IL_010e:  ldfld      "int Test.<G>d__1.<x>5__2"
                    IL_0113:  call       "void System.Console.WriteLine(int)"
                    IL_0118:  ldarg.0
                    IL_0119:  ldfld      "System.Threading.SemaphoreSlim Test.<G>d__1.semaphore"
                    IL_011e:  callvirt   "int System.Threading.SemaphoreSlim.Release()"
                    IL_0123:  pop
                    IL_0124:  ldarg.0
                    IL_0125:  ldfld      "object Test.<G>d__1.<>7__wrap2"
                    IL_012a:  stloc.3
                    IL_012b:  ldloc.3
                    IL_012c:  brfalse.s  IL_0143
                    IL_012e:  ldloc.3
                    IL_012f:  isinst     "System.Exception"
                    IL_0134:  dup
                    IL_0135:  brtrue.s   IL_0139
                    IL_0137:  ldloc.3
                    IL_0138:  throw
                    IL_0139:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_013e:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0143:  ldarg.0
                    IL_0144:  ldnull
                    IL_0145:  stfld      "object Test.<G>d__1.<>7__wrap2"
                    IL_014a:  leave.s    IL_0165
                  }
                  catch System.Exception
                  {
                    IL_014c:  stloc.s    V_4
                    IL_014e:  ldarg.0
                    IL_014f:  ldc.i4.s   -2
                    IL_0151:  stfld      "int Test.<G>d__1.<>1__state"
                    IL_0156:  ldarg.0
                    IL_0157:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                    IL_015c:  ldloc.s    V_4
                    IL_015e:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
                    IL_0163:  leave.s    IL_0178
                  }
                  IL_0165:  ldarg.0
                  IL_0166:  ldc.i4.s   -2
                  IL_0168:  stfld      "int Test.<G>d__1.<>1__state"
                  IL_016d:  ldarg.0
                  IL_016e:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                  IL_0173:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
                  IL_0178:  ret
                }
                """);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x1, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G(System.Threading.SemaphoreSlim)", """
                {
                  // Code size       43 (0x2b)
                  .maxstack  2
                  .locals init (Test.<G>d__1 V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  call       "System.Runtime.CompilerServices.AsyncVoidMethodBuilder System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Create()"
                  IL_0007:  stfld      "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                  IL_000c:  ldloca.s   V_0
                  IL_000e:  ldarg.0
                  IL_000f:  stfld      "System.Threading.SemaphoreSlim Test.<G>d__1.semaphore"
                  IL_0014:  ldloca.s   V_0
                  IL_0016:  ldc.i4.m1
                  IL_0017:  stfld      "int Test.<G>d__1.<>1__state"
                  IL_001c:  ldloca.s   V_0
                  IL_001e:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                  IL_0023:  ldloca.s   V_0
                  IL_0025:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Test.<G>d__1>(ref Test.<G>d__1)"
                  IL_002a:  ret
                }
                """);
        }

        [Fact]
        public void AsyncInFinally006_AsyncVoid_02()
        {
            var source = """
                using System;
                using System.Threading;
                using System.Threading.Tasks;
                class Test
                {
                    static async Task<int> F()
                    {
                        return 2;
                    }
                    static async void G(SemaphoreSlim semaphore)
                    {
                        int x = 0;
                        try
                        {
                            x = await F();
                        }
                        finally
                        {
                            x += await F();
                        }

                        Console.WriteLine(x);
                        semaphore.Release();
                    }
                    public static void Main()
                    {
                        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
                        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
                        try
                        {
                            var semaphore = new SemaphoreSlim(0, 1);
                            G(semaphore);
                            semaphore.Wait(1000 * 60);
                        }
                        finally
                        {
                            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
                        }
                    }
                }
                """;
            var expected = "4";
            var verifier = CompileAndVerify(source, expectedOutput: expected);
            verifier.VerifyIL("Test.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
                {
                  // Code size      377 (0x179)
                  .maxstack  3
                  .locals init (int V_0,
                                int V_1,
                                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                                object V_3,
                                System.Exception V_4)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "int Test.<G>d__1.<>1__state"
                  IL_0006:  stloc.0
                  .try
                  {
                    IL_0007:  ldloc.0
                    IL_0008:  brfalse.s  IL_0026
                    IL_000a:  ldloc.0
                    IL_000b:  ldc.i4.1
                    IL_000c:  beq        IL_00db
                    IL_0011:  ldarg.0
                    IL_0012:  ldc.i4.0
                    IL_0013:  stfld      "int Test.<G>d__1.<x>5__2"
                    IL_0018:  ldarg.0
                    IL_0019:  ldnull
                    IL_001a:  stfld      "object Test.<G>d__1.<>7__wrap2"
                    IL_001f:  ldarg.0
                    IL_0020:  ldc.i4.0
                    IL_0021:  stfld      "int Test.<G>d__1.<>7__wrap3"
                    IL_0026:  nop
                    .try
                    {
                      IL_0027:  ldloc.0
                      IL_0028:  brfalse.s  IL_0061
                      IL_002a:  call       "System.Threading.Tasks.Task<int> Test.F()"
                      IL_002f:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                      IL_0034:  stloc.2
                      IL_0035:  ldloca.s   V_2
                      IL_0037:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                      IL_003c:  brtrue.s   IL_007d
                      IL_003e:  ldarg.0
                      IL_003f:  ldc.i4.0
                      IL_0040:  dup
                      IL_0041:  stloc.0
                      IL_0042:  stfld      "int Test.<G>d__1.<>1__state"
                      IL_0047:  ldarg.0
                      IL_0048:  ldloc.2
                      IL_0049:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                      IL_004e:  ldarg.0
                      IL_004f:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                      IL_0054:  ldloca.s   V_2
                      IL_0056:  ldarg.0
                      IL_0057:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)"
                      IL_005c:  leave      IL_0178
                      IL_0061:  ldarg.0
                      IL_0062:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                      IL_0067:  stloc.2
                      IL_0068:  ldarg.0
                      IL_0069:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                      IL_006e:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                      IL_0074:  ldarg.0
                      IL_0075:  ldc.i4.m1
                      IL_0076:  dup
                      IL_0077:  stloc.0
                      IL_0078:  stfld      "int Test.<G>d__1.<>1__state"
                      IL_007d:  ldloca.s   V_2
                      IL_007f:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                      IL_0084:  stloc.1
                      IL_0085:  ldarg.0
                      IL_0086:  ldloc.1
                      IL_0087:  stfld      "int Test.<G>d__1.<x>5__2"
                      IL_008c:  leave.s    IL_0098
                    }
                    catch object
                    {
                      IL_008e:  stloc.3
                      IL_008f:  ldarg.0
                      IL_0090:  ldloc.3
                      IL_0091:  stfld      "object Test.<G>d__1.<>7__wrap2"
                      IL_0096:  leave.s    IL_0098
                    }
                    IL_0098:  ldarg.0
                    IL_0099:  ldarg.0
                    IL_009a:  ldfld      "int Test.<G>d__1.<x>5__2"
                    IL_009f:  stfld      "int Test.<G>d__1.<>7__wrap4"
                    IL_00a4:  call       "System.Threading.Tasks.Task<int> Test.F()"
                    IL_00a9:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                    IL_00ae:  stloc.2
                    IL_00af:  ldloca.s   V_2
                    IL_00b1:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                    IL_00b6:  brtrue.s   IL_00f7
                    IL_00b8:  ldarg.0
                    IL_00b9:  ldc.i4.1
                    IL_00ba:  dup
                    IL_00bb:  stloc.0
                    IL_00bc:  stfld      "int Test.<G>d__1.<>1__state"
                    IL_00c1:  ldarg.0
                    IL_00c2:  ldloc.2
                    IL_00c3:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                    IL_00c8:  ldarg.0
                    IL_00c9:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                    IL_00ce:  ldloca.s   V_2
                    IL_00d0:  ldarg.0
                    IL_00d1:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)"
                    IL_00d6:  leave      IL_0178
                    IL_00db:  ldarg.0
                    IL_00dc:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                    IL_00e1:  stloc.2
                    IL_00e2:  ldarg.0
                    IL_00e3:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__1"
                    IL_00e8:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                    IL_00ee:  ldarg.0
                    IL_00ef:  ldc.i4.m1
                    IL_00f0:  dup
                    IL_00f1:  stloc.0
                    IL_00f2:  stfld      "int Test.<G>d__1.<>1__state"
                    IL_00f7:  ldloca.s   V_2
                    IL_00f9:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                    IL_00fe:  stloc.1
                    IL_00ff:  ldarg.0
                    IL_0100:  ldarg.0
                    IL_0101:  ldfld      "int Test.<G>d__1.<>7__wrap4"
                    IL_0106:  ldloc.1
                    IL_0107:  add
                    IL_0108:  stfld      "int Test.<G>d__1.<x>5__2"
                    IL_010d:  ldarg.0
                    IL_010e:  ldfld      "object Test.<G>d__1.<>7__wrap2"
                    IL_0113:  stloc.3
                    IL_0114:  ldloc.3
                    IL_0115:  brfalse.s  IL_012c
                    IL_0117:  ldloc.3
                    IL_0118:  isinst     "System.Exception"
                    IL_011d:  dup
                    IL_011e:  brtrue.s   IL_0122
                    IL_0120:  ldloc.3
                    IL_0121:  throw
                    IL_0122:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_0127:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_012c:  ldarg.0
                    IL_012d:  ldnull
                    IL_012e:  stfld      "object Test.<G>d__1.<>7__wrap2"
                    IL_0133:  ldarg.0
                    IL_0134:  ldfld      "int Test.<G>d__1.<x>5__2"
                    IL_0139:  call       "void System.Console.WriteLine(int)"
                    IL_013e:  ldarg.0
                    IL_013f:  ldfld      "System.Threading.SemaphoreSlim Test.<G>d__1.semaphore"
                    IL_0144:  callvirt   "int System.Threading.SemaphoreSlim.Release()"
                    IL_0149:  pop
                    IL_014a:  leave.s    IL_0165
                  }
                  catch System.Exception
                  {
                    IL_014c:  stloc.s    V_4
                    IL_014e:  ldarg.0
                    IL_014f:  ldc.i4.s   -2
                    IL_0151:  stfld      "int Test.<G>d__1.<>1__state"
                    IL_0156:  ldarg.0
                    IL_0157:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                    IL_015c:  ldloc.s    V_4
                    IL_015e:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
                    IL_0163:  leave.s    IL_0178
                  }
                  IL_0165:  ldarg.0
                  IL_0166:  ldc.i4.s   -2
                  IL_0168:  stfld      "int Test.<G>d__1.<>1__state"
                  IL_016d:  ldarg.0
                  IL_016e:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                  IL_0173:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
                  IL_0178:  ret
                }
                """);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x1, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G(System.Threading.SemaphoreSlim)", """
                {
                  // Code size       43 (0x2b)
                  .maxstack  2
                  .locals init (Test.<G>d__1 V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  call       "System.Runtime.CompilerServices.AsyncVoidMethodBuilder System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Create()"
                  IL_0007:  stfld      "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                  IL_000c:  ldloca.s   V_0
                  IL_000e:  ldarg.0
                  IL_000f:  stfld      "System.Threading.SemaphoreSlim Test.<G>d__1.semaphore"
                  IL_0014:  ldloca.s   V_0
                  IL_0016:  ldc.i4.m1
                  IL_0017:  stfld      "int Test.<G>d__1.<>1__state"
                  IL_001c:  ldloca.s   V_0
                  IL_001e:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<G>d__1.<>t__builder"
                  IL_0023:  ldloca.s   V_0
                  IL_0025:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Test.<G>d__1>(ref Test.<G>d__1)"
                  IL_002a:  ret
                }
                """);
        }

        [Fact]
        public void AsyncInFinallyWithGotos()
        {
            var source = """
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
                        bool loop = true;
                        goto afterLabel;
                label:
                        loop = false;
                afterLabel:
                        try
                        {
                            x = await F();
                        }
                        finally
                        {
                            x += await F();
                        }
                        if (loop)
                        {
                            goto label;
                        }
                        return x;
                    }
                    public static void Main()
                    {
                        Task<int> t2 = G();
                        t2.Wait(1000 * 60);
                        Console.WriteLine(t2.Result);
                    }
                }
                """;

            var expected = "4";
            CompileAndVerify(source, expected);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x1, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G]: Unexpected type on the stack. { Offset = 0x45, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       70 (0x46)
                  .maxstack  2
                  .locals init (int V_0, //x
                                bool V_1, //loop
                                object V_2,
                                int V_3)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  IL_0002:  ldc.i4.1
                  IL_0003:  stloc.1
                  IL_0004:  br.s       IL_0008
                  IL_0006:  ldc.i4.0
                  IL_0007:  stloc.1
                  IL_0008:  ldnull
                  IL_0009:  stloc.2
                  .try
                  {
                    IL_000a:  call       "System.Threading.Tasks.Task<int> Test.F()"
                    IL_000f:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0014:  stloc.0
                    IL_0015:  leave.s    IL_001a
                  }
                  catch object
                  {
                    IL_0017:  stloc.2
                    IL_0018:  leave.s    IL_001a
                  }
                  IL_001a:  ldloc.0
                  IL_001b:  call       "System.Threading.Tasks.Task<int> Test.F()"
                  IL_0020:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0025:  stloc.3
                  IL_0026:  ldloc.3
                  IL_0027:  add
                  IL_0028:  stloc.0
                  IL_0029:  ldloc.2
                  IL_002a:  brfalse.s  IL_0041
                  IL_002c:  ldloc.2
                  IL_002d:  isinst     "System.Exception"
                  IL_0032:  dup
                  IL_0033:  brtrue.s   IL_0037
                  IL_0035:  ldloc.2
                  IL_0036:  throw
                  IL_0037:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_003c:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0041:  ldloc.1
                  IL_0042:  brtrue.s   IL_0006
                  IL_0044:  ldloc.0
                  IL_0045:  ret
                }
                """);
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0xc1, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics(
                // (23,17): warning CS0162: Unreachable code detected
                //                 System.Console.WriteLine("FAIL");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(23, 17),
                // (42,9): warning CS0162: Unreachable code detected
                //         System.Console.WriteLine("FAIL");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(42, 9)
            );
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0xc7, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics(
                // (25,21): warning CS0162: Unreachable code detected
                //                     System.Console.WriteLine("FAIL");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(25, 21),
                // (32,17): warning CS0162: Unreachable code detected
                //                 System.Console.WriteLine("FAIL");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(32, 17),
                // (46,13): warning CS0162: Unreachable code detected
                //             System.Console.WriteLine("FAIL");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(46, 13)
            );
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0x96, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics(
                // (25,21): warning CS0162: Unreachable code detected
                //                     System.Console.WriteLine("FAIL");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(25, 21),
                // (32,17): warning CS0162: Unreachable code detected
                //                 System.Console.WriteLine("FAIL");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(32, 17),
                // (47,13): warning CS0162: Unreachable code detected
                //             System.Console.WriteLine("FAIL");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(47, 13)
            );
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

            var comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(
                comp,
                expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x1, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0x1f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       32 (0x20)
                  .maxstack  2
                  .locals init (int V_0, //x
                                int V_1)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  IL_0002:  ldc.i4.0
                  IL_0003:  stloc.1
                  .try
                  {
                    IL_0004:  ldloc.0
                    IL_0005:  ldloc.0
                    IL_0006:  div
                    IL_0007:  stloc.0
                    IL_0008:  leave.s    IL_000f
                  }
                  catch object
                  {
                    IL_000a:  pop
                    IL_000b:  ldc.i4.1
                    IL_000c:  stloc.1
                    IL_000d:  leave.s    IL_000f
                  }
                  IL_000f:  ldloc.1
                  IL_0010:  ldc.i4.1
                  IL_0011:  bne.un.s   IL_001e
                  IL_0013:  call       "System.Threading.Tasks.Task<int> Test.F()"
                  IL_0018:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_001d:  stloc.0
                  IL_001e:  ldloc.0
                  IL_001f:  ret
                }
                """);
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0x5f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0x58, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0x9d, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0xa4, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Test.G", """
                {
                  // Code size      165 (0xa5)
                  .maxstack  2
                  .locals init (int V_0, //x
                                int V_1,
                                System.Exception V_2, //ex
                                object V_3,
                                int V_4,
                                object V_5,
                                System.Exception V_6)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  IL_0002:  ldc.i4.0
                  IL_0003:  stloc.1
                  .try
                  {
                    IL_0004:  ldc.i4.0
                    IL_0005:  stloc.s    V_4
                    .try
                    {
                      IL_0007:  ldloc.0
                      IL_0008:  ldloc.0
                      IL_0009:  div
                      IL_000a:  stloc.0
                      IL_000b:  leave.s    IL_002d
                    }
                    filter
                    {
                      IL_000d:  isinst     "object"
                      IL_0012:  dup
                      IL_0013:  brtrue.s   IL_0019
                      IL_0015:  pop
                      IL_0016:  ldc.i4.0
                      IL_0017:  br.s       IL_0025
                      IL_0019:  stloc.s    V_5
                      IL_001b:  ldloc.s    V_5
                      IL_001d:  stloc.3
                      IL_001e:  ldloc.0
                      IL_001f:  ldc.i4.0
                      IL_0020:  cgt.un
                      IL_0022:  ldc.i4.0
                      IL_0023:  cgt.un
                      IL_0025:  endfilter
                    }  // end filter
                    {  // handler
                      IL_0027:  pop
                      IL_0028:  ldc.i4.1
                      IL_0029:  stloc.s    V_4
                      IL_002b:  leave.s    IL_002d
                    }
                    IL_002d:  ldloc.s    V_4
                    IL_002f:  ldc.i4.1
                    IL_0030:  bne.un.s   IL_0052
                    IL_0032:  call       "System.Threading.Tasks.Task<int> Test.F()"
                    IL_0037:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_003c:  stloc.0
                    IL_003d:  ldloc.3
                    IL_003e:  isinst     "System.Exception"
                    IL_0043:  dup
                    IL_0044:  brtrue.s   IL_0048
                    IL_0046:  ldloc.3
                    IL_0047:  throw
                    IL_0048:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_004d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0052:  leave.s    IL_0089
                  }
                  filter
                  {
                    IL_0054:  isinst     "System.Exception"
                    IL_0059:  dup
                    IL_005a:  brtrue.s   IL_0060
                    IL_005c:  pop
                    IL_005d:  ldc.i4.0
                    IL_005e:  br.s       IL_0082
                    IL_0060:  stloc.s    V_6
                    IL_0062:  ldloc.s    V_6
                    IL_0064:  castclass  "System.Exception"
                    IL_0069:  stloc.2
                    IL_006a:  ldloc.0
                    IL_006b:  brtrue.s   IL_007e
                    IL_006d:  ldstr      "hello"
                    IL_0072:  newobj     "System.Exception..ctor(string)"
                    IL_0077:  dup
                    IL_0078:  stloc.2
                    IL_0079:  ldnull
                    IL_007a:  cgt.un
                    IL_007c:  br.s       IL_007f
                    IL_007e:  ldc.i4.0
                    IL_007f:  ldc.i4.0
                    IL_0080:  cgt.un
                    IL_0082:  endfilter
                  }  // end filter
                  {  // handler
                    IL_0084:  pop
                    IL_0085:  ldc.i4.1
                    IL_0086:  stloc.1
                    IL_0087:  leave.s    IL_0089
                  }
                  IL_0089:  ldloc.1
                  IL_008a:  ldc.i4.1
                  IL_008b:  bne.un.s   IL_00a3
                  IL_008d:  call       "System.Threading.Tasks.Task<int> Test.F()"
                  IL_0092:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0097:  stloc.0
                  IL_0098:  ldloc.2
                  IL_0099:  callvirt   "string System.Exception.Message.get"
                  IL_009e:  call       "void System.Console.WriteLine(string)"
                  IL_00a3:  ldloc.0
                  IL_00a4:  ret
                }
                """);
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0xcf, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [G]: Unexpected type on the stack. { Offset = 0x16e, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();
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

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        [<G>b__0]: Unexpected type on the stack. { Offset = 0x1a9, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                        """
                });
            verifier.VerifyDiagnostics();
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

            var comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(
                comp,
                expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [F]: Return value missing on the stack. { Offset = 0x3d }
                        """
                });
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem(67091, "https://github.com/dotnet/roslyn/issues/67091")]
        public void NestedCatch_DuplicateLocal()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                using static System.Console;

                class C
                {
                    static async Task Main()
                    {
                        await new C().M1(catchFirst: false);
                        WriteLine("--- catch first ---");
                        await new C().M1(catchFirst: true);
                    }

                    bool F(string caller, Exception ex, bool result)
                    {
                        WriteLine($"F: {caller} {ex.Message}");
                        return result;
                    }

                    async Task M1(bool catchFirst)
                    {
                        try
                        {
                            throw new Exception("M1");
                        }
                        catch (Exception ex) when (F("M1-catch1", ex, catchFirst))
                        {
                            await M2("M1-catch1", ex);
                        }
                        catch (Exception ex) when (F("M1-catch2", ex, true))
                        {
                            try
                            {
                                throw new Exception("M1-catch2");
                            }
                            catch 
                            {
                                await M2("M1-catch2-catch", ex);
                            }
                        }
                    }

                    async Task M2(string caller, Exception ex)
                    {
                        WriteLine($"M2: {caller} {ex.Message}");
                        await Task.Yield();
                    }
                }
                """;
            var expectedOutput = """
                F: M1-catch1 M1
                F: M1-catch2 M1
                M2: M1-catch2-catch M1
                --- catch first ---
                F: M1-catch1 M1
                M2: M1-catch1 M1
                """;

            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: expectedOutput).VerifyDiagnostics();
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput).VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [Main]: Return value missing on the stack. { Offset = 0x2a }
                        [M1]: Return value missing on the stack. { Offset = 0x6d }
                        [M1]: Return value missing on the stack. { Offset = 0xaa }
                        [M1]: Return value missing on the stack. { Offset = 0x7f }
                        [M2]: Return value missing on the stack. { Offset = 0x3f }
                        """
                });
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem(67091, "https://github.com/dotnet/roslyn/issues/67091")]
        public void NestedCatch_DuplicateLocal_Level2()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                using static System.Console;

                class C
                {
                    static async Task Main()
                    {
                        foreach (var catchFirst1 in new[] { false, true })
                        {
                            foreach (var catchFirst2 in new[] { false, true })
                            {
                                WriteLine($"--- catchFirst1={catchFirst1}, catchFirst2={catchFirst2} ---");
                                await new C().M1(catchFirst1: catchFirst1, catchFirst2: catchFirst2);
                            }
                        }
                    }

                    bool F(string caller, Exception ex, bool result)
                    {
                        WriteLine($"F: {caller} {ex.Message}");
                        return result;
                    }

                    async Task M1(bool catchFirst1, bool catchFirst2)
                    {
                        try
                        {
                            throw new Exception("M1-try");
                        }
                        catch (Exception ex) when (F("M1-catch1", ex, catchFirst1))
                        {
                            await M2("M1-catch1", ex);
                        }
                        catch (Exception ex) when (F("M1-catch2", ex, true))
                        {
                            try
                            {
                                throw new Exception("M1-catch2");
                            }
                            catch (Exception ex2) when (F("M1-catch2-catch1", ex2, catchFirst2))
                            {
                                await M2("M1-catch2-catch1-ex", ex);
                                await M2("M1-catch2-catch1-ex2", ex2);
                            }
                            catch (Exception ex2) when (F("M1-catch2-catch2", ex2, true))
                            {
                                try
                                {
                                    throw new Exception("M1-catch2-catch1");
                                }
                                catch
                                {
                                    await M2("M1-catch2-catch2-catch-ex", ex);
                                    await M2("M1-catch2-catch2-catch-ex2", ex2);
                                }
                            }
                        }
                    }

                    async Task M2(string caller, Exception ex)
                    {
                        await Task.Yield();
                        WriteLine($"M2: {caller} {ex.Message}");
                    }
                }
                """;
            var expectedOutput = """
                --- catchFirst1=False, catchFirst2=False ---
                F: M1-catch1 M1-try
                F: M1-catch2 M1-try
                F: M1-catch2-catch1 M1-catch2
                F: M1-catch2-catch2 M1-catch2
                M2: M1-catch2-catch2-catch-ex M1-try
                M2: M1-catch2-catch2-catch-ex2 M1-catch2
                --- catchFirst1=False, catchFirst2=True ---
                F: M1-catch1 M1-try
                F: M1-catch2 M1-try
                F: M1-catch2-catch1 M1-catch2
                M2: M1-catch2-catch1-ex M1-try
                M2: M1-catch2-catch1-ex2 M1-catch2
                --- catchFirst1=True, catchFirst2=False ---
                F: M1-catch1 M1-try
                M2: M1-catch1 M1-try
                --- catchFirst1=True, catchFirst2=True ---
                F: M1-catch1 M1-try
                M2: M1-catch1 M1-try
                """;

            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: expectedOutput).VerifyDiagnostics();
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput).VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput),
                verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [Main]: Return value missing on the stack. { Offset = 0xa3 }
                        [M1]: Return value missing on the stack. { Offset = 0x6d }
                        [M1]: Return value missing on the stack. { Offset = 0xf8 }
                        [M1]: Return value missing on the stack. { Offset = 0x159 }
                        [M1]: Return value missing on the stack. { Offset = 0x11c }
                        [M1]: Return value missing on the stack. { Offset = 0x7f }
                        [M2]: Return value missing on the stack. { Offset = 0x3f }
                        """
                });
            verifier.VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/70483")]
        public void NestedCatch_DuplicateLocal_NoAwaitInNestedCatch(bool awaitInTry2)
        {
            var source = $$"""
                using System;
                using System.Threading.Tasks;
                using static System.Console;
                
                class C
                {
                    static async Task Main()
                    {
                        await new C().M1(catchFirst: false);
                        WriteLine("--- catch first ---");
                        await new C().M1(catchFirst: true);
                    }
                
                    bool F(string caller, bool result)
                    {
                        WriteLine($"F: {caller}");
                        return result;
                    }
                
                    async Task M1(bool catchFirst)
                    {
                        try
                        {
                            throw new Exception("M1");
                        }
                        catch (Exception ex) when (F("M1-catch1", catchFirst))
                        {
                            await M2("M1-catch1", ex);
                        }
                        catch (Exception ex) when (F("M1-catch2", true))
                        {
                            try
                            {
                                {{(awaitInTry2 ? "await Task.Yield();" : "")}}
                                throw new Exception("M1-catch2");
                            }
                            catch 
                            {
                                _ = M2("M1-catch2-catch", ex);
                            }
                        }
                    }
                
                    async Task M2(string caller, Exception ex)
                    {
                        WriteLine($"M2: {caller} {ex.Message}");
                        await Task.Yield();
                    }
                }
                """;

            var expectedOutput = """
                F: M1-catch1
                F: M1-catch2
                M2: M1-catch2-catch M1
                --- catch first ---
                F: M1-catch1
                M2: M1-catch1 M1
                """;

            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: expectedOutput).VerifyDiagnostics();
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput).VerifyDiagnostics();

            var ilVerifyMessage = awaitInTry2
                ? """
                    [Main]: Return value missing on the stack. { Offset = 0x2a }
                    [M1]: Return value missing on the stack. { Offset = 0x6b }
                    [M1]: Return value missing on the stack. { Offset = 0xc1 }
                    [M1]: Return value missing on the stack. { Offset = 0x7d }
                    [M2]: Return value missing on the stack. { Offset = 0x3f }
                    """
                : """
                    [Main]: Return value missing on the stack. { Offset = 0x2a }
                    [M1]: Return value missing on the stack. { Offset = 0x88 }
                    [M2]: Return value missing on the stack. { Offset = 0x3f }
                    """;

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with { ILVerifyMessage = ilVerifyMessage });
            verifier.VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/71569")]
        public void NestedRethrow(bool await1, bool await2)
        {
            var source = $$"""
                using System;
                using System.Threading.Tasks;

                try
                {
                    await Test1();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType().Name);
                }

                try
                {
                    await Test2();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType().Name);
                }

                static async Task Test1()
                {
                    try
                    {
                        throw new Exception1();
                    }
                    catch (Exception1)
                    {
                        try
                        {
                            await Task.FromException(new Exception2());
                        }
                        catch (Exception2)
                        {
                            {{(await1 ? "await Task.Yield();" : "")}}
                            throw;
                        }
                    }
                }

                static async Task Test2()
                {
                    try
                    {
                        throw new Exception1();
                    }
                    catch (Exception1)
                    {
                        try
                        {
                            await Task.FromException(new Exception2());
                        }
                        catch (Exception2 ex)
                        {
                            {{(await2 ? "await Task.Yield();" : "")}}
                            throw ex;
                        }
                    }
                }

                class Exception1 : Exception { }
                class Exception2 : Exception { }
                """;

            var expectedOutput = """
                Exception2
                Exception2
                """;

            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: expectedOutput,
                targetFramework: TargetFramework.Mscorlib46).VerifyDiagnostics();
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput,
                targetFramework: TargetFramework.Mscorlib46).VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source);
            var ilVerifyMessage = (await1, await2) switch
            {
                (true, true) => """
                    [<Main>$]: Return value missing on the stack. { Offset = 0x3b }
                    [<<Main>$>g__Test1|0_0]: Return value missing on the stack. { Offset = 0x67 }
                    [<<Main>$>g__Test2|0_1]: Return value missing on the stack. { Offset = 0x59 }
                    """,
                (false, true) => """
                    [<Main>$]: Return value missing on the stack. { Offset = 0x3b }
                    [<<Main>$>g__Test1|0_0]: Return value missing on the stack. { Offset = 0x26 }
                    [<<Main>$>g__Test2|0_1]: Return value missing on the stack. { Offset = 0x59 }
                    """,
                (true, false) => """
                    [<Main>$]: Return value missing on the stack. { Offset = 0x3b }
                    [<<Main>$>g__Test1|0_0]: Return value missing on the stack. { Offset = 0x67 }
                    [<<Main>$>g__Test2|0_1]: Return value missing on the stack. { Offset = 0x24 }
                    """,
                (false, false) => """
                    [<Main>$]: Return value missing on the stack. { Offset = 0x3b }
                    [<<Main>$>g__Test1|0_0]: Return value missing on the stack. { Offset = 0x26 }
                    [<<Main>$>g__Test2|0_1]: Return value missing on the stack. { Offset = 0x24 }
                    """
            };

            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with { ILVerifyMessage = ilVerifyMessage });
            verifier.VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/71569")]
        public void NestedRethrow_02(bool await1, bool await2, bool await3)
        {
            var source = $$"""
                using System;
                using System.Threading.Tasks;

                try
                {
                    await Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType().Name);
                }

                static async Task Run()
                {
                    try
                    {
                        throw new Exception1();
                    }
                    catch (Exception1)
                    {
                        try
                        {
                            {{(await1 ? "await Task.Yield();" : "")}}
                            throw new Exception2();
                        }
                        catch (Exception2)
                        {
                            try
                            {
                                {{(await2 ? "await Task.Yield();" : "")}}
                                throw new Exception3();
                            }
                            catch (Exception3)
                            {
                                {{(await3 ? "await Task.Yield();" : "")}}
                                throw;
                            }
                        }
                    }
                }

                class Exception1 : Exception { }
                class Exception2 : Exception { }
                class Exception3 : Exception { }
                """;

            var expectedOutput = "Exception3";

            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: expectedOutput).VerifyDiagnostics();
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput).VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = "[<Main>$]: Return value missing on the stack. { Offset = 0x1d }"
            });
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.<<Main>$>g__Run|0_0()", getIL());

            string getIL() => (await1, await2, await3) switch
            {
                (false, false, false) => """
                    {
                      // Code size       23 (0x17)
                      .maxstack  1
                      .try
                      {
                        IL_0000:  newobj     "Exception1..ctor()"
                        IL_0005:  throw
                      }
                      catch Exception1
                      {
                        IL_0006:  pop
                        .try
                        {
                          IL_0007:  newobj     "Exception2..ctor()"
                          IL_000c:  throw
                        }
                        catch Exception2
                        {
                          IL_000d:  pop
                          .try
                          {
                            IL_000e:  newobj     "Exception3..ctor()"
                            IL_0013:  throw
                          }
                          catch Exception3
                          {
                            IL_0014:  pop
                            IL_0015:  rethrow
                          }
                        }
                      }
                    }
                    """,
                (true, false, false) => """
                    {
                      // Code size       72 (0x48)
                      .maxstack  2
                      .locals init (int V_0,
                                    System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                                    System.Runtime.CompilerServices.YieldAwaitable V_2)
                      IL_0000:  ldc.i4.0
                      IL_0001:  stloc.0
                      .try
                      {
                        IL_0002:  newobj     "Exception1..ctor()"
                        IL_0007:  throw
                      }
                      catch Exception1
                      {
                        IL_0008:  pop
                        IL_0009:  ldc.i4.1
                        IL_000a:  stloc.0
                        IL_000b:  leave.s    IL_000d
                      }
                      IL_000d:  ldloc.0
                      IL_000e:  ldc.i4.1
                      IL_000f:  bne.un.s   IL_0046
                      IL_0011:  nop
                      .try
                      {
                        IL_0012:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                        IL_0017:  stloc.2
                        IL_0018:  ldloca.s   V_2
                        IL_001a:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                        IL_001f:  stloc.1
                        IL_0020:  ldloca.s   V_1
                        IL_0022:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                        IL_0027:  brtrue.s   IL_002f
                        IL_0029:  ldloc.1
                        IL_002a:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                        IL_002f:  ldloca.s   V_1
                        IL_0031:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                        IL_0036:  newobj     "Exception2..ctor()"
                        IL_003b:  throw
                      }
                      catch Exception2
                      {
                        IL_003c:  pop
                        .try
                        {
                          IL_003d:  newobj     "Exception3..ctor()"
                          IL_0042:  throw
                        }
                        catch Exception3
                        {
                          IL_0043:  pop
                          IL_0044:  rethrow
                        }
                      }
                      IL_0046:  ldnull
                      IL_0047:  throw
                    }
                    """,
                (false, true, false) => """
                    {
                      // Code size       82 (0x52)
                      .maxstack  2
                      .locals init (int V_0,
                                    int V_1,
                                    System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                                    System.Runtime.CompilerServices.YieldAwaitable V_3)
                      IL_0000:  ldc.i4.0
                      IL_0001:  stloc.0
                      .try
                      {
                        IL_0002:  newobj     "Exception1..ctor()"
                        IL_0007:  throw
                      }
                      catch Exception1
                      {
                        IL_0008:  pop
                        IL_0009:  ldc.i4.1
                        IL_000a:  stloc.0
                        IL_000b:  leave.s    IL_000d
                      }
                      IL_000d:  ldloc.0
                      IL_000e:  ldc.i4.1
                      IL_000f:  bne.un.s   IL_0050
                      IL_0011:  ldc.i4.0
                      IL_0012:  stloc.1
                      .try
                      {
                        IL_0013:  newobj     "Exception2..ctor()"
                        IL_0018:  throw
                      }
                      catch Exception2
                      {
                        IL_0019:  pop
                        IL_001a:  ldc.i4.1
                        IL_001b:  stloc.1
                        IL_001c:  leave.s    IL_001e
                      }
                      IL_001e:  ldloc.1
                      IL_001f:  ldc.i4.1
                      IL_0020:  bne.un.s   IL_0050
                      IL_0022:  nop
                      .try
                      {
                        IL_0023:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                        IL_0028:  stloc.3
                        IL_0029:  ldloca.s   V_3
                        IL_002b:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                        IL_0030:  stloc.2
                        IL_0031:  ldloca.s   V_2
                        IL_0033:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                        IL_0038:  brtrue.s   IL_0040
                        IL_003a:  ldloc.2
                        IL_003b:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                        IL_0040:  ldloca.s   V_2
                        IL_0042:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                        IL_0047:  newobj     "Exception3..ctor()"
                        IL_004c:  throw
                      }
                      catch Exception3
                      {
                        IL_004d:  pop
                        IL_004e:  rethrow
                      }
                      IL_0050:  ldnull
                      IL_0051:  throw
                    }
                    """,
                (false, false, true) => """
                    {
                      // Code size      113 (0x71)
                      .maxstack  2
                      .locals init (int V_0,
                                    int V_1,
                                    object V_2,
                                    int V_3,
                                    System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_4,
                                    System.Runtime.CompilerServices.YieldAwaitable V_5)
                      IL_0000:  ldc.i4.0
                      IL_0001:  stloc.0
                      .try
                      {
                        IL_0002:  newobj     "Exception1..ctor()"
                        IL_0007:  throw
                      }
                      catch Exception1
                      {
                        IL_0008:  pop
                        IL_0009:  ldc.i4.1
                        IL_000a:  stloc.0
                        IL_000b:  leave.s    IL_000d
                      }
                      IL_000d:  ldloc.0
                      IL_000e:  ldc.i4.1
                      IL_000f:  bne.un.s   IL_006f
                      IL_0011:  ldc.i4.0
                      IL_0012:  stloc.1
                      .try
                      {
                        IL_0013:  newobj     "Exception2..ctor()"
                        IL_0018:  throw
                      }
                      catch Exception2
                      {
                        IL_0019:  pop
                        IL_001a:  ldc.i4.1
                        IL_001b:  stloc.1
                        IL_001c:  leave.s    IL_001e
                      }
                      IL_001e:  ldloc.1
                      IL_001f:  ldc.i4.1
                      IL_0020:  bne.un.s   IL_006f
                      IL_0022:  ldc.i4.0
                      IL_0023:  stloc.3
                      .try
                      {
                        IL_0024:  newobj     "Exception3..ctor()"
                        IL_0029:  throw
                      }
                      catch Exception3
                      {
                        IL_002a:  stloc.2
                        IL_002b:  ldc.i4.1
                        IL_002c:  stloc.3
                        IL_002d:  leave.s    IL_002f
                      }
                      IL_002f:  ldloc.3
                      IL_0030:  ldc.i4.1
                      IL_0031:  bne.un.s   IL_006f
                      IL_0033:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                      IL_0038:  stloc.s    V_5
                      IL_003a:  ldloca.s   V_5
                      IL_003c:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                      IL_0041:  stloc.s    V_4
                      IL_0043:  ldloca.s   V_4
                      IL_0045:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                      IL_004a:  brtrue.s   IL_0053
                      IL_004c:  ldloc.s    V_4
                      IL_004e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                      IL_0053:  ldloca.s   V_4
                      IL_0055:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                      IL_005a:  ldloc.2
                      IL_005b:  isinst     "System.Exception"
                      IL_0060:  dup
                      IL_0061:  brtrue.s   IL_0065
                      IL_0063:  ldloc.2
                      IL_0064:  throw
                      IL_0065:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                      IL_006a:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                      IL_006f:  ldnull
                      IL_0070:  throw
                    }
                    """,
                (true, true, false) => """
                    {
                      // Code size      118 (0x76)
                      .maxstack  2
                      .locals init (int V_0,
                                    int V_1,
                                    System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                                    System.Runtime.CompilerServices.YieldAwaitable V_3)
                      IL_0000:  ldc.i4.0
                      IL_0001:  stloc.0
                      .try
                      {
                        IL_0002:  newobj     "Exception1..ctor()"
                        IL_0007:  throw
                      }
                      catch Exception1
                      {
                        IL_0008:  pop
                        IL_0009:  ldc.i4.1
                        IL_000a:  stloc.0
                        IL_000b:  leave.s    IL_000d
                      }
                      IL_000d:  ldloc.0
                      IL_000e:  ldc.i4.1
                      IL_000f:  bne.un.s   IL_0074
                      IL_0011:  ldc.i4.0
                      IL_0012:  stloc.1
                      .try
                      {
                        IL_0013:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                        IL_0018:  stloc.3
                        IL_0019:  ldloca.s   V_3
                        IL_001b:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                        IL_0020:  stloc.2
                        IL_0021:  ldloca.s   V_2
                        IL_0023:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                        IL_0028:  brtrue.s   IL_0030
                        IL_002a:  ldloc.2
                        IL_002b:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                        IL_0030:  ldloca.s   V_2
                        IL_0032:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                        IL_0037:  newobj     "Exception2..ctor()"
                        IL_003c:  throw
                      }
                      catch Exception2
                      {
                        IL_003d:  pop
                        IL_003e:  ldc.i4.1
                        IL_003f:  stloc.1
                        IL_0040:  leave.s    IL_0042
                      }
                      IL_0042:  ldloc.1
                      IL_0043:  ldc.i4.1
                      IL_0044:  bne.un.s   IL_0074
                      IL_0046:  nop
                      .try
                      {
                        IL_0047:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                        IL_004c:  stloc.3
                        IL_004d:  ldloca.s   V_3
                        IL_004f:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                        IL_0054:  stloc.2
                        IL_0055:  ldloca.s   V_2
                        IL_0057:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                        IL_005c:  brtrue.s   IL_0064
                        IL_005e:  ldloc.2
                        IL_005f:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                        IL_0064:  ldloca.s   V_2
                        IL_0066:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                        IL_006b:  newobj     "Exception3..ctor()"
                        IL_0070:  throw
                      }
                      catch Exception3
                      {
                        IL_0071:  pop
                        IL_0072:  rethrow
                      }
                      IL_0074:  ldnull
                      IL_0075:  throw
                    }
                    """,
                (true, false, true) => """
                    {
                      // Code size      155 (0x9b)
                      .maxstack  2
                      .locals init (int V_0,
                                    int V_1,
                                    System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                                    System.Runtime.CompilerServices.YieldAwaitable V_3,
                                    object V_4,
                                    int V_5)
                      IL_0000:  ldc.i4.0
                      IL_0001:  stloc.0
                      .try
                      {
                        IL_0002:  newobj     "Exception1..ctor()"
                        IL_0007:  throw
                      }
                      catch Exception1
                      {
                        IL_0008:  pop
                        IL_0009:  ldc.i4.1
                        IL_000a:  stloc.0
                        IL_000b:  leave.s    IL_000d
                      }
                      IL_000d:  ldloc.0
                      IL_000e:  ldc.i4.1
                      IL_000f:  bne.un     IL_0099
                      IL_0014:  ldc.i4.0
                      IL_0015:  stloc.1
                      .try
                      {
                        IL_0016:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                        IL_001b:  stloc.3
                        IL_001c:  ldloca.s   V_3
                        IL_001e:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                        IL_0023:  stloc.2
                        IL_0024:  ldloca.s   V_2
                        IL_0026:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                        IL_002b:  brtrue.s   IL_0033
                        IL_002d:  ldloc.2
                        IL_002e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                        IL_0033:  ldloca.s   V_2
                        IL_0035:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                        IL_003a:  newobj     "Exception2..ctor()"
                        IL_003f:  throw
                      }
                      catch Exception2
                      {
                        IL_0040:  pop
                        IL_0041:  ldc.i4.1
                        IL_0042:  stloc.1
                        IL_0043:  leave.s    IL_0045
                      }
                      IL_0045:  ldloc.1
                      IL_0046:  ldc.i4.1
                      IL_0047:  bne.un.s   IL_0099
                      IL_0049:  ldc.i4.0
                      IL_004a:  stloc.s    V_5
                      .try
                      {
                        IL_004c:  newobj     "Exception3..ctor()"
                        IL_0051:  throw
                      }
                      catch Exception3
                      {
                        IL_0052:  stloc.s    V_4
                        IL_0054:  ldc.i4.1
                        IL_0055:  stloc.s    V_5
                        IL_0057:  leave.s    IL_0059
                      }
                      IL_0059:  ldloc.s    V_5
                      IL_005b:  ldc.i4.1
                      IL_005c:  bne.un.s   IL_0099
                      IL_005e:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                      IL_0063:  stloc.3
                      IL_0064:  ldloca.s   V_3
                      IL_0066:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                      IL_006b:  stloc.2
                      IL_006c:  ldloca.s   V_2
                      IL_006e:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                      IL_0073:  brtrue.s   IL_007b
                      IL_0075:  ldloc.2
                      IL_0076:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                      IL_007b:  ldloca.s   V_2
                      IL_007d:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                      IL_0082:  ldloc.s    V_4
                      IL_0084:  isinst     "System.Exception"
                      IL_0089:  dup
                      IL_008a:  brtrue.s   IL_008f
                      IL_008c:  ldloc.s    V_4
                      IL_008e:  throw
                      IL_008f:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                      IL_0094:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                      IL_0099:  ldnull
                      IL_009a:  throw
                    }
                    """,
                (false, true, true) => """
                    {
                      // Code size      155 (0x9b)
                      .maxstack  2
                      .locals init (int V_0,
                                    int V_1,
                                    object V_2,
                                    int V_3,
                                    System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_4,
                                    System.Runtime.CompilerServices.YieldAwaitable V_5)
                      IL_0000:  ldc.i4.0
                      IL_0001:  stloc.0
                      .try
                      {
                        IL_0002:  newobj     "Exception1..ctor()"
                        IL_0007:  throw
                      }
                      catch Exception1
                      {
                        IL_0008:  pop
                        IL_0009:  ldc.i4.1
                        IL_000a:  stloc.0
                        IL_000b:  leave.s    IL_000d
                      }
                      IL_000d:  ldloc.0
                      IL_000e:  ldc.i4.1
                      IL_000f:  bne.un     IL_0099
                      IL_0014:  ldc.i4.0
                      IL_0015:  stloc.1
                      .try
                      {
                        IL_0016:  newobj     "Exception2..ctor()"
                        IL_001b:  throw
                      }
                      catch Exception2
                      {
                        IL_001c:  pop
                        IL_001d:  ldc.i4.1
                        IL_001e:  stloc.1
                        IL_001f:  leave.s    IL_0021
                      }
                      IL_0021:  ldloc.1
                      IL_0022:  ldc.i4.1
                      IL_0023:  bne.un.s   IL_0099
                      IL_0025:  ldc.i4.0
                      IL_0026:  stloc.3
                      .try
                      {
                        IL_0027:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                        IL_002c:  stloc.s    V_5
                        IL_002e:  ldloca.s   V_5
                        IL_0030:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                        IL_0035:  stloc.s    V_4
                        IL_0037:  ldloca.s   V_4
                        IL_0039:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                        IL_003e:  brtrue.s   IL_0047
                        IL_0040:  ldloc.s    V_4
                        IL_0042:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                        IL_0047:  ldloca.s   V_4
                        IL_0049:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                        IL_004e:  newobj     "Exception3..ctor()"
                        IL_0053:  throw
                      }
                      catch Exception3
                      {
                        IL_0054:  stloc.2
                        IL_0055:  ldc.i4.1
                        IL_0056:  stloc.3
                        IL_0057:  leave.s    IL_0059
                      }
                      IL_0059:  ldloc.3
                      IL_005a:  ldc.i4.1
                      IL_005b:  bne.un.s   IL_0099
                      IL_005d:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                      IL_0062:  stloc.s    V_5
                      IL_0064:  ldloca.s   V_5
                      IL_0066:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                      IL_006b:  stloc.s    V_4
                      IL_006d:  ldloca.s   V_4
                      IL_006f:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                      IL_0074:  brtrue.s   IL_007d
                      IL_0076:  ldloc.s    V_4
                      IL_0078:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                      IL_007d:  ldloca.s   V_4
                      IL_007f:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                      IL_0084:  ldloc.2
                      IL_0085:  isinst     "System.Exception"
                      IL_008a:  dup
                      IL_008b:  brtrue.s   IL_008f
                      IL_008d:  ldloc.2
                      IL_008e:  throw
                      IL_008f:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                      IL_0094:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                      IL_0099:  ldnull
                      IL_009a:  throw
                    }
                    """,
                (true, true, true) => """
                    {
                      // Code size      191 (0xbf)
                      .maxstack  2
                      .locals init (int V_0,
                                    int V_1,
                                    System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                                    System.Runtime.CompilerServices.YieldAwaitable V_3,
                                    object V_4,
                                    int V_5)
                      IL_0000:  ldc.i4.0
                      IL_0001:  stloc.0
                      .try
                      {
                        IL_0002:  newobj     "Exception1..ctor()"
                        IL_0007:  throw
                      }
                      catch Exception1
                      {
                        IL_0008:  pop
                        IL_0009:  ldc.i4.1
                        IL_000a:  stloc.0
                        IL_000b:  leave.s    IL_000d
                      }
                      IL_000d:  ldloc.0
                      IL_000e:  ldc.i4.1
                      IL_000f:  bne.un     IL_00bd
                      IL_0014:  ldc.i4.0
                      IL_0015:  stloc.1
                      .try
                      {
                        IL_0016:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                        IL_001b:  stloc.3
                        IL_001c:  ldloca.s   V_3
                        IL_001e:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                        IL_0023:  stloc.2
                        IL_0024:  ldloca.s   V_2
                        IL_0026:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                        IL_002b:  brtrue.s   IL_0033
                        IL_002d:  ldloc.2
                        IL_002e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                        IL_0033:  ldloca.s   V_2
                        IL_0035:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                        IL_003a:  newobj     "Exception2..ctor()"
                        IL_003f:  throw
                      }
                      catch Exception2
                      {
                        IL_0040:  pop
                        IL_0041:  ldc.i4.1
                        IL_0042:  stloc.1
                        IL_0043:  leave.s    IL_0045
                      }
                      IL_0045:  ldloc.1
                      IL_0046:  ldc.i4.1
                      IL_0047:  bne.un.s   IL_00bd
                      IL_0049:  ldc.i4.0
                      IL_004a:  stloc.s    V_5
                      .try
                      {
                        IL_004c:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                        IL_0051:  stloc.3
                        IL_0052:  ldloca.s   V_3
                        IL_0054:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                        IL_0059:  stloc.2
                        IL_005a:  ldloca.s   V_2
                        IL_005c:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                        IL_0061:  brtrue.s   IL_0069
                        IL_0063:  ldloc.2
                        IL_0064:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                        IL_0069:  ldloca.s   V_2
                        IL_006b:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                        IL_0070:  newobj     "Exception3..ctor()"
                        IL_0075:  throw
                      }
                      catch Exception3
                      {
                        IL_0076:  stloc.s    V_4
                        IL_0078:  ldc.i4.1
                        IL_0079:  stloc.s    V_5
                        IL_007b:  leave.s    IL_007d
                      }
                      IL_007d:  ldloc.s    V_5
                      IL_007f:  ldc.i4.1
                      IL_0080:  bne.un.s   IL_00bd
                      IL_0082:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                      IL_0087:  stloc.3
                      IL_0088:  ldloca.s   V_3
                      IL_008a:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                      IL_008f:  stloc.2
                      IL_0090:  ldloca.s   V_2
                      IL_0092:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                      IL_0097:  brtrue.s   IL_009f
                      IL_0099:  ldloc.2
                      IL_009a:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                      IL_009f:  ldloca.s   V_2
                      IL_00a1:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                      IL_00a6:  ldloc.s    V_4
                      IL_00a8:  isinst     "System.Exception"
                      IL_00ad:  dup
                      IL_00ae:  brtrue.s   IL_00b3
                      IL_00b0:  ldloc.s    V_4
                      IL_00b2:  throw
                      IL_00b3:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                      IL_00b8:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                      IL_00bd:  ldnull
                      IL_00be:  throw
                    }
                    """,
            };
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/71569")]
        [InlineData("await Task.Yield();")]
        [InlineData("await using var c = new C();")]
        [InlineData("await foreach (var x in new C()) { }")]
        public void NestedRethrow_03(string statement)
        {
            var source = $$"""
                using System;
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                try
                {
                    await Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType().Name);
                }

                static async Task Run()
                {
                    try
                    {
                        throw new Exception1();
                    }
                    catch (Exception1)
                    {
                        {{statement}}
                        try
                        {
                            throw new Exception2();
                        }
                        catch (Exception2)
                        {
                            try
                            {
                                throw new Exception3();
                            }
                            catch (Exception3)
                            {
                                throw;
                            }
                        }
                    }
                }

                class Exception1 : Exception { }
                class Exception2 : Exception { }
                class Exception3 : Exception { }

                class C : IAsyncDisposable, IAsyncEnumerable<int>
                {
                    public async ValueTask DisposeAsync() => await Task.Yield();
                    public async IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken ct)
                    {
                        await Task.Yield();
                        yield return 1;
                    }
                }
                """;

            CSharpTestSource sources = [source, AsyncStreamsTypes];

            var expectedOutput = "Exception3";

            CompileAndVerify(CreateCompilationWithTasksExtensions(sources, options: TestOptions.DebugExe), expectedOutput: expectedOutput).VerifyDiagnostics();
            CompileAndVerify(CreateCompilationWithTasksExtensions(sources, options: TestOptions.ReleaseExe), expectedOutput: expectedOutput).VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped);
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program.<<Main>$>g__Run|0_0()", getIL(statement));

            static string getIL(string statement)
            {
                if (statement.Contains("using"))
                {
                    return """
                        {
                          // Code size       84 (0x54)
                          .maxstack  2
                          .locals init (int V_0,
                                        C V_1, //c
                                        object V_2)
                          IL_0000:  ldc.i4.0
                          IL_0001:  stloc.0
                          .try
                          {
                            IL_0002:  newobj     "Exception1..ctor()"
                            IL_0007:  throw
                          }
                          catch Exception1
                          {
                            IL_0008:  pop
                            IL_0009:  ldc.i4.1
                            IL_000a:  stloc.0
                            IL_000b:  leave.s    IL_000d
                          }
                          IL_000d:  ldloc.0
                          IL_000e:  ldc.i4.1
                          IL_000f:  bne.un.s   IL_0052
                          IL_0011:  newobj     "C..ctor()"
                          IL_0016:  stloc.1
                          IL_0017:  ldnull
                          IL_0018:  stloc.2
                          .try
                          {
                            .try
                            {
                              IL_0019:  newobj     "Exception2..ctor()"
                              IL_001e:  throw
                            }
                            catch Exception2
                            {
                              IL_001f:  pop
                              .try
                              {
                                IL_0020:  newobj     "Exception3..ctor()"
                                IL_0025:  throw
                              }
                              catch Exception3
                              {
                                IL_0026:  pop
                                IL_0027:  rethrow
                              }
                            }
                          }
                          catch object
                          {
                            IL_0029:  stloc.2
                            IL_002a:  leave.s    IL_002c
                          }
                          IL_002c:  ldloc.1
                          IL_002d:  brfalse.s  IL_003a
                          IL_002f:  ldloc.1
                          IL_0030:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                          IL_0035:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                          IL_003a:  ldloc.2
                          IL_003b:  brfalse.s  IL_0052
                          IL_003d:  ldloc.2
                          IL_003e:  isinst     "System.Exception"
                          IL_0043:  dup
                          IL_0044:  brtrue.s   IL_0048
                          IL_0046:  ldloc.2
                          IL_0047:  throw
                          IL_0048:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                          IL_004d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                          IL_0052:  ldnull
                          IL_0053:  throw
                        }
                        """;
                }
                else if (statement.Contains("foreach"))
                {
                    return """
                        {
                          // Code size      123 (0x7b)
                          .maxstack  2
                          .locals init (int V_0,
                                        System.Collections.Generic.IAsyncEnumerator<int> V_1,
                                        System.Threading.CancellationToken V_2,
                                        object V_3)
                          IL_0000:  ldc.i4.0
                          IL_0001:  stloc.0
                          .try
                          {
                            IL_0002:  newobj     "Exception1..ctor()"
                            IL_0007:  throw
                          }
                          catch Exception1
                          {
                            IL_0008:  pop
                            IL_0009:  ldc.i4.1
                            IL_000a:  stloc.0
                            IL_000b:  leave.s    IL_000d
                          }
                          IL_000d:  ldloc.0
                          IL_000e:  ldc.i4.1
                          IL_000f:  bne.un.s   IL_0079
                          IL_0011:  newobj     "C..ctor()"
                          IL_0016:  ldloca.s   V_2
                          IL_0018:  initobj    "System.Threading.CancellationToken"
                          IL_001e:  ldloc.2
                          IL_001f:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                          IL_0024:  stloc.1
                          IL_0025:  ldnull
                          IL_0026:  stloc.3
                          .try
                          {
                            IL_0027:  br.s       IL_0030
                            IL_0029:  ldloc.1
                            IL_002a:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                            IL_002f:  pop
                            IL_0030:  ldloc.1
                            IL_0031:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                            IL_0036:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                            IL_003b:  brtrue.s   IL_0029
                            IL_003d:  leave.s    IL_0042
                          }
                          catch object
                          {
                            IL_003f:  stloc.3
                            IL_0040:  leave.s    IL_0042
                          }
                          IL_0042:  ldloc.1
                          IL_0043:  brfalse.s  IL_0050
                          IL_0045:  ldloc.1
                          IL_0046:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                          IL_004b:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                          IL_0050:  ldloc.3
                          IL_0051:  brfalse.s  IL_0068
                          IL_0053:  ldloc.3
                          IL_0054:  isinst     "System.Exception"
                          IL_0059:  dup
                          IL_005a:  brtrue.s   IL_005e
                          IL_005c:  ldloc.3
                          IL_005d:  throw
                          IL_005e:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                          IL_0063:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                          IL_0068:  nop
                          .try
                          {
                            IL_0069:  newobj     "Exception2..ctor()"
                            IL_006e:  throw
                          }
                          catch Exception2
                          {
                            IL_006f:  pop
                            .try
                            {
                              IL_0070:  newobj     "Exception3..ctor()"
                              IL_0075:  throw
                            }
                            catch Exception3
                            {
                              IL_0076:  pop
                              IL_0077:  rethrow
                            }
                          }
                          IL_0079:  ldnull
                          IL_007a:  throw
                        }
                        """;
                }
                else
                {
                    return """
                        {
                          // Code size       71 (0x47)
                          .maxstack  2
                          .locals init (int V_0,
                                        System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                                        System.Runtime.CompilerServices.YieldAwaitable V_2)
                          IL_0000:  ldc.i4.0
                          IL_0001:  stloc.0
                          .try
                          {
                            IL_0002:  newobj     "Exception1..ctor()"
                            IL_0007:  throw
                          }
                          catch Exception1
                          {
                            IL_0008:  pop
                            IL_0009:  ldc.i4.1
                            IL_000a:  stloc.0
                            IL_000b:  leave.s    IL_000d
                          }
                          IL_000d:  ldloc.0
                          IL_000e:  ldc.i4.1
                          IL_000f:  bne.un.s   IL_0045
                          IL_0011:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                          IL_0016:  stloc.2
                          IL_0017:  ldloca.s   V_2
                          IL_0019:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                          IL_001e:  stloc.1
                          IL_001f:  ldloca.s   V_1
                          IL_0021:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                          IL_0026:  brtrue.s   IL_002e
                          IL_0028:  ldloc.1
                          IL_0029:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                          IL_002e:  ldloca.s   V_1
                          IL_0030:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                          .try
                          {
                            IL_0035:  newobj     "Exception2..ctor()"
                            IL_003a:  throw
                          }
                          catch Exception2
                          {
                            IL_003b:  pop
                            .try
                            {
                              IL_003c:  newobj     "Exception3..ctor()"
                              IL_0041:  throw
                            }
                            catch Exception3
                            {
                              IL_0042:  pop
                              IL_0043:  rethrow
                            }
                          }
                          IL_0045:  ldnull
                          IL_0046:  throw
                        }
                        """;
                }
            }
        }
    }
}
