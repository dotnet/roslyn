// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.DynamicAnalysis.UnitTests;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.Instruction;

// To run these tests with runtime-async execution enabled, set the DOTNET_RuntimeAsync environment variable to 1.
//
// set DOTNET_RuntimeAsync=1
// dotnet test Microsoft.CodeAnalysis.CSharp.Emit.UnitTests.csproj -f net10.0
//
// Here are some example of convenient filters that can be applied:
// --filter "FullyQualifiedName~CodeGenAsyncIteratorTests"
// --filter "DisplayName~RuntimeAsync_"

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

    [CompilerTrait(CompilerFeature.AsyncStreams, CompilerFeature.Async)]
    public class CodeGenAsyncIteratorTests : EmitMetadataTestBase
    {
        internal static string ExpectedOutput(string output)
        {
            return ExecutionConditionUtil.IsMonoOrCoreClr ? output : null;
        }

        /// <summary>
        /// Enumerates `C.M()` a given number of iterations.
        /// </summary>
        private static string Run(int iterations, [CallerMemberName] string testMethodName = null)
        {
            string runner = $@"
using static System.Console;
class {testMethodName}
";
            runner += @"
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
            return runner.Replace("ITERATIONS", iterations.ToString());
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
            var lib = CreateCompilationWithTasksExtensions(new[] { AsyncStreamsTypes });
            var lib_ref = lib.EmitToImageReference();
            var comp = CreateCompilationWithTasksExtensions(source, references: new[] { lib_ref });
            comp.MakeTypeMissing(type);
            comp.VerifyEmitDiagnostics(expected);
        }

        // Instrumentation to investigate CI failure: https://github.com/dotnet/roslyn/issues/34207
        private CSharpCompilation CreateCompilationWithAsyncIterator(string source, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null)
            => CreateCompilationWithTasksExtensions(new[] { (CSharpTestSource)CSharpTestBase.Parse(source, filename: "source", parseOptions), CSharpTestBase.Parse(AsyncStreamsTypes, filename: "AsyncStreamsTypes", parseOptions) },
                options: options);

        private CSharpCompilation CreateCompilationWithAsyncIterator(CSharpTestSource source, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null)
            => CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: options, parseOptions: parseOptions);

        [Fact]
        [WorkItem(38961, "https://github.com/dotnet/roslyn/issues/38961")]
        public void LockInsideFinally()
        {
            var src = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class C
{
    object _splitsLock = new object();
    public async IAsyncEnumerable<string> GetSplits()
    {
        try
        {
        }
        finally
        {
            lock (_splitsLock)
            {
                Console.Write(""hello "");
            }
            Console.Write(""world"");
        }
        yield break;
    }
    public static async Task Main()
    {
        await foreach (var i in new C().GetSplits()) { }
    }
}";
            var expectedOutput = "hello world";

            var comp = CreateCompilationWithAsyncIterator(src, options: TestOptions.DebugExe);

            var v = CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            v.VerifyIL("C.<GetSplits>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      268 (0x10c)
  .maxstack  3
  .locals init (int V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<GetSplits>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -3
    IL_000a:  beq.s      IL_000e
    IL_000c:  br.s       IL_0010
    IL_000e:  br.s       IL_0010
    IL_0010:  ldarg.0
    IL_0011:  ldfld      ""bool C.<GetSplits>d__1.<>w__disposeMode""
    IL_0016:  brfalse.s  IL_001d
    IL_0018:  leave      IL_00db
    IL_001d:  ldarg.0
    IL_001e:  ldc.i4.m1
    IL_001f:  dup
    IL_0020:  stloc.0
    IL_0021:  stfld      ""int C.<GetSplits>d__1.<>1__state""
    IL_0026:  nop
    .try
    {
      IL_0027:  nop
      IL_0028:  nop
      IL_0029:  leave.s    IL_0096
    }
    finally
    {
      IL_002b:  ldloc.0
      IL_002c:  ldc.i4.m1
      IL_002d:  bne.un.s   IL_0095
      IL_002f:  nop
      IL_0030:  ldarg.0
      IL_0031:  ldarg.0
      IL_0032:  ldfld      ""C C.<GetSplits>d__1.<>4__this""
      IL_0037:  ldfld      ""object C._splitsLock""
      IL_003c:  stfld      ""object C.<GetSplits>d__1.<>s__1""
      IL_0041:  ldarg.0
      IL_0042:  ldc.i4.0
      IL_0043:  stfld      ""bool C.<GetSplits>d__1.<>s__2""
      .try
      {
        IL_0048:  ldarg.0
        IL_0049:  ldfld      ""object C.<GetSplits>d__1.<>s__1""
        IL_004e:  ldarg.0
        IL_004f:  ldflda     ""bool C.<GetSplits>d__1.<>s__2""
        IL_0054:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
        IL_0059:  nop
        IL_005a:  nop
        IL_005b:  ldstr      ""hello ""
        IL_0060:  call       ""void System.Console.Write(string)""
        IL_0065:  nop
        IL_0066:  nop
        IL_0067:  leave.s    IL_0082
      }
      finally
      {
        IL_0069:  ldloc.0
        IL_006a:  ldc.i4.m1
        IL_006b:  bne.un.s   IL_0081
        IL_006d:  ldarg.0
        IL_006e:  ldfld      ""bool C.<GetSplits>d__1.<>s__2""
        IL_0073:  brfalse.s  IL_0081
        IL_0075:  ldarg.0
        IL_0076:  ldfld      ""object C.<GetSplits>d__1.<>s__1""
        IL_007b:  call       ""void System.Threading.Monitor.Exit(object)""
        IL_0080:  nop
        IL_0081:  endfinally
      }
      IL_0082:  ldarg.0
      IL_0083:  ldnull
      IL_0084:  stfld      ""object C.<GetSplits>d__1.<>s__1""
      IL_0089:  ldstr      ""world""
      IL_008e:  call       ""void System.Console.Write(string)""
      IL_0093:  nop
      IL_0094:  nop
      IL_0095:  endfinally
    }
    IL_0096:  ldarg.0
    IL_0097:  ldfld      ""bool C.<GetSplits>d__1.<>w__disposeMode""
    IL_009c:  brfalse.s  IL_00a0
    IL_009e:  leave.s    IL_00db
    IL_00a0:  ldarg.0
    IL_00a1:  ldc.i4.1
    IL_00a2:  stfld      ""bool C.<GetSplits>d__1.<>w__disposeMode""
    IL_00a7:  leave.s    IL_00db
  }
  catch System.Exception
  {
    IL_00a9:  stloc.1
    IL_00aa:  ldarg.0
    IL_00ab:  ldc.i4.s   -2
    IL_00ad:  stfld      ""int C.<GetSplits>d__1.<>1__state""
    IL_00b2:  ldarg.0
    IL_00b3:  ldnull
    IL_00b4:  stfld      ""object C.<GetSplits>d__1.<>s__1""
    IL_00b9:  ldarg.0
    IL_00ba:  ldnull
    IL_00bb:  stfld      ""string C.<GetSplits>d__1.<>2__current""
    IL_00c0:  ldarg.0
    IL_00c1:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<GetSplits>d__1.<>t__builder""
    IL_00c6:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_00cb:  nop
    IL_00cc:  ldarg.0
    IL_00cd:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<GetSplits>d__1.<>v__promiseOfValueOrEnd""
    IL_00d2:  ldloc.1
    IL_00d3:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_00d8:  nop
    IL_00d9:  leave.s    IL_010b
  }
  IL_00db:  ldarg.0
  IL_00dc:  ldc.i4.s   -2
  IL_00de:  stfld      ""int C.<GetSplits>d__1.<>1__state""
  IL_00e3:  ldarg.0
  IL_00e4:  ldnull
  IL_00e5:  stfld      ""object C.<GetSplits>d__1.<>s__1""
  IL_00ea:  ldarg.0
  IL_00eb:  ldnull
  IL_00ec:  stfld      ""string C.<GetSplits>d__1.<>2__current""
  IL_00f1:  ldarg.0
  IL_00f2:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<GetSplits>d__1.<>t__builder""
  IL_00f7:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_00fc:  nop
  IL_00fd:  ldarg.0
  IL_00fe:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<GetSplits>d__1.<>v__promiseOfValueOrEnd""
  IL_0103:  ldc.i4.0
  IL_0104:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0109:  nop
  IL_010a:  ret
  IL_010b:  ret
}");

            comp = CreateRuntimeAsyncCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(38961, "https://github.com/dotnet/roslyn/issues/38961")]
        public void FinallyInsideFinally()
        {
            var src = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class C
{
    public static async IAsyncEnumerable<string> GetSplits()
    {
        try
        {
        }
        finally
        {
            try
            {
                Console.Write(""hello "");
            }
            finally
            {
                Console.Write(""world"");
            }
            Console.Write(""!"");
        }

        yield break;
    }
    public static async Task Main()
    {
        await foreach (var i in GetSplits()) { }
    }
}";
            var expectedOutput = "hello world!";

            var comp = CreateCompilationWithAsyncIterator(src, options: TestOptions.DebugExe);

            var v = CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            v.VerifyIL("C.<GetSplits>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      195 (0xc3)
  .maxstack  3
  .locals init (int V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<GetSplits>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -3
    IL_000a:  beq.s      IL_000e
    IL_000c:  br.s       IL_0010
    IL_000e:  br.s       IL_0010
    IL_0010:  ldarg.0
    IL_0011:  ldfld      ""bool C.<GetSplits>d__0.<>w__disposeMode""
    IL_0016:  brfalse.s  IL_001a
    IL_0018:  leave.s    IL_0099
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.m1
    IL_001c:  dup
    IL_001d:  stloc.0
    IL_001e:  stfld      ""int C.<GetSplits>d__0.<>1__state""
    IL_0023:  nop
    .try
    {
      IL_0024:  nop
      IL_0025:  nop
      IL_0026:  leave.s    IL_005b
    }
    finally
    {
      IL_0028:  ldloc.0
      IL_0029:  ldc.i4.m1
      IL_002a:  bne.un.s   IL_005a
      IL_002c:  nop
      .try
      {
        IL_002d:  nop
        IL_002e:  ldstr      ""hello ""
        IL_0033:  call       ""void System.Console.Write(string)""
        IL_0038:  nop
        IL_0039:  nop
        IL_003a:  leave.s    IL_004e
      }
      finally
      {
        IL_003c:  ldloc.0
        IL_003d:  ldc.i4.m1
        IL_003e:  bne.un.s   IL_004d
        IL_0040:  nop
        IL_0041:  ldstr      ""world""
        IL_0046:  call       ""void System.Console.Write(string)""
        IL_004b:  nop
        IL_004c:  nop
        IL_004d:  endfinally
      }
      IL_004e:  ldstr      ""!""
      IL_0053:  call       ""void System.Console.Write(string)""
      IL_0058:  nop
      IL_0059:  nop
      IL_005a:  endfinally
    }
    IL_005b:  ldarg.0
    IL_005c:  ldfld      ""bool C.<GetSplits>d__0.<>w__disposeMode""
    IL_0061:  brfalse.s  IL_0065
    IL_0063:  leave.s    IL_0099
    IL_0065:  ldarg.0
    IL_0066:  ldc.i4.1
    IL_0067:  stfld      ""bool C.<GetSplits>d__0.<>w__disposeMode""
    IL_006c:  leave.s    IL_0099
  }
  catch System.Exception
  {
    IL_006e:  stloc.1
    IL_006f:  ldarg.0
    IL_0070:  ldc.i4.s   -2
    IL_0072:  stfld      ""int C.<GetSplits>d__0.<>1__state""
    IL_0077:  ldarg.0
    IL_0078:  ldnull
    IL_0079:  stfld      ""string C.<GetSplits>d__0.<>2__current""
    IL_007e:  ldarg.0
    IL_007f:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<GetSplits>d__0.<>t__builder""
    IL_0084:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_0089:  nop
    IL_008a:  ldarg.0
    IL_008b:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<GetSplits>d__0.<>v__promiseOfValueOrEnd""
    IL_0090:  ldloc.1
    IL_0091:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_0096:  nop
    IL_0097:  leave.s    IL_00c2
  }
  IL_0099:  ldarg.0
  IL_009a:  ldc.i4.s   -2
  IL_009c:  stfld      ""int C.<GetSplits>d__0.<>1__state""
  IL_00a1:  ldarg.0
  IL_00a2:  ldnull
  IL_00a3:  stfld      ""string C.<GetSplits>d__0.<>2__current""
  IL_00a8:  ldarg.0
  IL_00a9:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<GetSplits>d__0.<>t__builder""
  IL_00ae:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_00b3:  nop
  IL_00b4:  ldarg.0
  IL_00b5:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<GetSplits>d__0.<>v__promiseOfValueOrEnd""
  IL_00ba:  ldc.i4.0
  IL_00bb:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_00c0:  nop
  IL_00c1:  ret
  IL_00c2:  ret
}");

            comp = CreateRuntimeAsyncCompilation(src, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();

            verifier.VerifyIL("C.<GetSplits>d__0.System.Collections.Generic.IAsyncEnumerator<string>.MoveNextAsync()", """
{
  // Code size      145 (0x91)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<GetSplits>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -3
    IL_000a:  beq.s      IL_000e
    IL_000c:  br.s       IL_0010
    IL_000e:  br.s       IL_0010
    IL_0010:  ldarg.0
    IL_0011:  ldfld      "bool C.<GetSplits>d__0.<>w__disposeMode"
    IL_0016:  brfalse.s  IL_001a
    IL_0018:  leave.s    IL_0080
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.m1
    IL_001c:  dup
    IL_001d:  stloc.0
    IL_001e:  stfld      "int C.<GetSplits>d__0.<>1__state"
    IL_0023:  nop
    .try
    {
      IL_0024:  nop
      IL_0025:  nop
      IL_0026:  leave.s    IL_005b
    }
    finally
    {
      IL_0028:  ldloc.0
      IL_0029:  ldc.i4.m1
      IL_002a:  bne.un.s   IL_005a
      IL_002c:  nop
      .try
      {
        IL_002d:  nop
        IL_002e:  ldstr      "hello "
        IL_0033:  call       "void System.Console.Write(string)"
        IL_0038:  nop
        IL_0039:  nop
        IL_003a:  leave.s    IL_004e
      }
      finally
      {
        IL_003c:  ldloc.0
        IL_003d:  ldc.i4.m1
        IL_003e:  bne.un.s   IL_004d
        IL_0040:  nop
        IL_0041:  ldstr      "world"
        IL_0046:  call       "void System.Console.Write(string)"
        IL_004b:  nop
        IL_004c:  nop
        IL_004d:  endfinally
      }
      IL_004e:  ldstr      "!"
      IL_0053:  call       "void System.Console.Write(string)"
      IL_0058:  nop
      IL_0059:  nop
      IL_005a:  endfinally
    }
    IL_005b:  ldarg.0
    IL_005c:  ldfld      "bool C.<GetSplits>d__0.<>w__disposeMode"
    IL_0061:  brfalse.s  IL_0065
    IL_0063:  leave.s    IL_0080
    IL_0065:  ldarg.0
    IL_0066:  ldc.i4.1
    IL_0067:  stfld      "bool C.<GetSplits>d__0.<>w__disposeMode"
    IL_006c:  leave.s    IL_0080
  }
  catch System.Exception
  {
    IL_006e:  pop
    IL_006f:  ldarg.0
    IL_0070:  ldc.i4.s   -2
    IL_0072:  stfld      "int C.<GetSplits>d__0.<>1__state"
    IL_0077:  ldarg.0
    IL_0078:  ldnull
    IL_0079:  stfld      "string C.<GetSplits>d__0.<>2__current"
    IL_007e:  rethrow
  }
  IL_0080:  ldarg.0
  IL_0081:  ldc.i4.s   -2
  IL_0083:  stfld      "int C.<GetSplits>d__0.<>1__state"
  IL_0088:  ldarg.0
  IL_0089:  ldnull
  IL_008a:  stfld      "string C.<GetSplits>d__0.<>2__current"
  IL_008f:  ldc.i4.0
  IL_0090:  ret
}
""");
        }

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
            var src = @"
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
}";
            var expectedOutput = @"
2
8";
            var comp = CreateCompilationWithAsyncIterator(src, TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(src, TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(30566, "https://github.com/dotnet/roslyn/issues/30566")]
        public void YieldReturnAwait2()
        {
            var src = @"
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
}";
            var expectedOutput = @"
2
8";
            var comp = CreateCompilationWithAsyncIterator(src, TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(src, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
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
            var expectedOutput = "42";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, references: new[] { CSharpRef }, TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            // PROTOTYPE Tracked by https://github.com/dotnet/roslyn/issues/79762
            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (10,22): error CS9328: Method 'C.<M>d__0.IAsyncEnumerator<int>.MoveNextAsync()' uses a feature that is not supported by runtime async currently. Opt the method out of runtime async by attributing it with 'System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(false)'.
                //         yield return await d;
                Diagnostic(ErrorCode.ERR_UnsupportedFeatureInRuntimeAsync, "await d").WithArguments("C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()").WithLocation(10, 22));
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
            var expected = new[]
            {
                // (4,45): error CS0234: The type or namespace name 'IAsyncEnumerable<>' does not exist in the namespace 'System.Collections.Generic' (are you missing an assembly reference?)
                //     static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IAsyncEnumerable<int>").WithArguments("IAsyncEnumerable<>", "System.Collections.Generic").WithLocation(4, 45),
                // (4,67): error CS8652: The feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M").WithArguments("async streams", "8.0").WithLocation(4, 67),
                // (4,67): error CS8652: The feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M").WithArguments("async streams", "8.0").WithLocation(4, 67)
            };
            var comp = CreateCompilationWithTasksExtensions(new[] { source }, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(expected);

            comp = CreateCompilationWithTasksExtensions(new[] { source }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,45): error CS0234: The type or namespace name 'IAsyncEnumerable<>' does not exist in the namespace 'System.Collections.Generic' (are you missing an assembly reference?)
                //     static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IAsyncEnumerable<int>").WithArguments("IAsyncEnumerable<>", "System.Collections.Generic").WithLocation(4, 45));
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

            var expectedDiagnostics = new[]
            {
                // source(4,65): error CS9244: The type 'S' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'IAsyncEnumerable<T>'
                //     static async System.Collections.Generic.IAsyncEnumerable<S> M()
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "M").WithArguments("System.Collections.Generic.IAsyncEnumerable<T>", "T", "S").WithLocation(4, 65),
                // source(4,65): error CS9267: Element type of an iterator may not be a ref struct or a type parameter allowing ref structs
                //     static async System.Collections.Generic.IAsyncEnumerable<S> M()
                Diagnostic(ErrorCode.ERR_IteratorRefLikeElementType, "M").WithLocation(4, 65)
            };

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // source(4,65): error CS9244: The type 'S' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'IAsyncEnumerable<T>'
                //     static async System.Collections.Generic.IAsyncEnumerable<S> M()
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "M").WithArguments("System.Collections.Generic.IAsyncEnumerable<T>", "T", "S").WithLocation(4, 65),
                // source(4,65): error CS9267: Element type of an iterator may not be a ref struct or a type parameter allowing ref structs
                //     static async System.Collections.Generic.IAsyncEnumerable<S> M()
                Diagnostic(ErrorCode.ERR_IteratorRefLikeElementType, "M").WithLocation(4, 65),
                // source(11,24): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (var s in M())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "var").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(11, 24));
        }

        [Fact]
        public void RefStructElementType_NonGeneric()
        {
            string source = """
                using System.Threading.Tasks;

                class C
                {
                    public E GetAsyncEnumerator() => new E();
                    static async Task Main()
                    {
                        await foreach (var s in new C())
                        {
                            System.Console.Write(s.F);
                        }
                    }
                }
                class E
                {
                    bool _done;
                    public S Current => new S { F = 123 };
                    public async Task<bool> MoveNextAsync()
                    {
                        await Task.Yield();
                        return !_done ? (_done = true) : false;
                    }
                }
                ref struct S
                {
                    public int F;
                }
                """;

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // source(8,24): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "var").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 24));

            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp.VerifyEmitDiagnostics();

            var expectedOutput = "123";
            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.<Main>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
                {
                  // Code size      238 (0xee)
                  .maxstack  3
                  .locals init (int V_0,
                                S V_1, //s
                                System.Runtime.CompilerServices.TaskAwaiter<bool> V_2,
                                C.<Main>d__1 V_3,
                                System.Exception V_4)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "int C.<Main>d__1.<>1__state"
                  IL_0006:  stloc.0
                  .try
                  {
                    IL_0007:  ldloc.0
                    IL_0008:  brfalse.s  IL_0012
                    IL_000a:  br.s       IL_000c
                    IL_000c:  ldloc.0
                    IL_000d:  ldc.i4.1
                    IL_000e:  beq.s      IL_0014
                    IL_0010:  br.s       IL_0016
                    IL_0012:  br.s       IL_0082
                    IL_0014:  br.s       IL_0082
                    IL_0016:  nop
                    IL_0017:  nop
                    IL_0018:  ldarg.0
                    IL_0019:  newobj     "C..ctor()"
                    IL_001e:  call       "E C.GetAsyncEnumerator()"
                    IL_0023:  stfld      "E C.<Main>d__1.<>s__1"
                    IL_0028:  br.s       IL_0044
                    IL_002a:  ldarg.0
                    IL_002b:  ldfld      "E C.<Main>d__1.<>s__1"
                    IL_0030:  callvirt   "S E.Current.get"
                    IL_0035:  stloc.1
                    IL_0036:  nop
                    IL_0037:  ldloc.1
                    IL_0038:  ldfld      "int S.F"
                    IL_003d:  call       "void System.Console.Write(int)"
                    IL_0042:  nop
                    IL_0043:  nop
                    IL_0044:  ldarg.0
                    IL_0045:  ldfld      "E C.<Main>d__1.<>s__1"
                    IL_004a:  callvirt   "System.Threading.Tasks.Task<bool> E.MoveNextAsync()"
                    IL_004f:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()"
                    IL_0054:  stloc.2
                    IL_0055:  ldloca.s   V_2
                    IL_0057:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get"
                    IL_005c:  brtrue.s   IL_009e
                    IL_005e:  ldarg.0
                    IL_005f:  ldc.i4.0
                    IL_0060:  dup
                    IL_0061:  stloc.0
                    IL_0062:  stfld      "int C.<Main>d__1.<>1__state"
                    IL_0067:  ldarg.0
                    IL_0068:  ldloc.2
                    IL_0069:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__1.<>u__1"
                    IL_006e:  ldarg.0
                    IL_006f:  stloc.3
                    IL_0070:  ldarg.0
                    IL_0071:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__1.<>t__builder"
                    IL_0076:  ldloca.s   V_2
                    IL_0078:  ldloca.s   V_3
                    IL_007a:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Main>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Main>d__1)"
                    IL_007f:  nop
                    IL_0080:  leave.s    IL_00ed
                    IL_0082:  ldarg.0
                    IL_0083:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__1.<>u__1"
                    IL_0088:  stloc.2
                    IL_0089:  ldarg.0
                    IL_008a:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__1.<>u__1"
                    IL_008f:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<bool>"
                    IL_0095:  ldarg.0
                    IL_0096:  ldc.i4.m1
                    IL_0097:  dup
                    IL_0098:  stloc.0
                    IL_0099:  stfld      "int C.<Main>d__1.<>1__state"
                    IL_009e:  ldarg.0
                    IL_009f:  ldloca.s   V_2
                    IL_00a1:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()"
                    IL_00a6:  stfld      "bool C.<Main>d__1.<>s__2"
                    IL_00ab:  ldarg.0
                    IL_00ac:  ldfld      "bool C.<Main>d__1.<>s__2"
                    IL_00b1:  brtrue     IL_002a
                    IL_00b6:  ldarg.0
                    IL_00b7:  ldnull
                    IL_00b8:  stfld      "E C.<Main>d__1.<>s__1"
                    IL_00bd:  leave.s    IL_00d9
                  }
                  catch System.Exception
                  {
                    IL_00bf:  stloc.s    V_4
                    IL_00c1:  ldarg.0
                    IL_00c2:  ldc.i4.s   -2
                    IL_00c4:  stfld      "int C.<Main>d__1.<>1__state"
                    IL_00c9:  ldarg.0
                    IL_00ca:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__1.<>t__builder"
                    IL_00cf:  ldloc.s    V_4
                    IL_00d1:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                    IL_00d6:  nop
                    IL_00d7:  leave.s    IL_00ed
                  }
                  IL_00d9:  ldarg.0
                  IL_00da:  ldc.i4.s   -2
                  IL_00dc:  stfld      "int C.<Main>d__1.<>1__state"
                  IL_00e1:  ldarg.0
                  IL_00e2:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__1.<>t__builder"
                  IL_00e7:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
                  IL_00ec:  nop
                  IL_00ed:  ret
                }
                """);

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void RefStructElementType_NonGeneric_AwaitAfter()
        {
            string source = """
                using System.Threading.Tasks;

                class C
                {
                    public E GetAsyncEnumerator() => new E();
                    static async Task Main()
                    {
                        await foreach (var s in new C())
                        {
                            System.Console.Write(s.F);
                            await Task.Yield();
                        }
                    }
                }
                class E
                {
                    bool _done;
                    public S Current => new S { F = 123 };
                    public async Task<bool> MoveNextAsync()
                    {
                        await Task.Yield();
                        return !_done ? (_done = true) : false;
                    }
                }
                ref struct S
                {
                    public int F;
                }
                """;

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // source(8,24): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "var").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 24));

            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp.VerifyEmitDiagnostics();

            var expectedOutput = "123";
            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
            verifier.VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void RefStructElementType_NonGeneric_AwaitBefore()
        {
            string source = """
                using System.Threading.Tasks;

                class C
                {
                    public E GetAsyncEnumerator() => new E();
                    static async Task Main()
                    {
                        await foreach (var s in new C())
                        {
                            await Task.Yield();
                            System.Console.Write(s.F);
                        }
                    }
                }
                class E
                {
                    bool _done;
                    public S Current => new S { F = 123 };
                    public async Task<bool> MoveNextAsync()
                    {
                        await Task.Yield();
                        return !_done ? (_done = true) : false;
                    }
                }
                ref struct S
                {
                    public int F;
                }
                """;

            var comp = CreateCompilationWithAsyncIterator(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // source(8,24): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "var").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 24));

            var expectedDiagnostics = new[]
            {
                // source(11,34): error CS4007: Instance of type 'S' cannot be preserved across 'await' or 'yield' boundary.
                //             System.Console.Write(s.F);
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "s.F").WithArguments("S").WithLocation(11, 34)
            };

            comp = CreateCompilationWithAsyncIterator(source, parseOptions: TestOptions.Regular13);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);
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
            CompileAndVerify(comp);

            var m2 = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M2");
            Assert.False(m2.IsAsync);
            Assert.False(m2.IsIterator);

            var m = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            Assert.True(m.IsAsync);
            Assert.True(m.IsIterator);
        }

        [Fact, WorkItem(38201, "https://github.com/dotnet/roslyn/issues/38201")]
        public void ReturningIAsyncEnumerable_Misc()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    async IAsyncEnumerable<string> GetStrings(int i = 0) { yield return await Task.FromResult(""""); }

    async IAsyncEnumerable<string> AsyncExpressionBody()
        => GetStrings(await Task.FromResult(1)); // 1

    IAsyncEnumerable<string> ExpressionBody() => GetStrings();

    async IAsyncEnumerable<string> AsyncReturn()
    {
        return GetStrings(await Task.FromResult(1)); // 2
    }

    IAsyncEnumerable<string> Return() { return GetStrings(); }

    async IAsyncEnumerable<string> AsyncReturnAndYieldReturn()
    {
        bool b = true;
        if (b) { return GetStrings(); } // 3
        yield return await Task.FromResult("""");
    }

    IAsyncEnumerable<string> ReturnAndYieldReturn() // 4
    {
        bool b = true;
        if (b) { return GetStrings(); } // 5
        yield return """";
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType });
            comp.VerifyDiagnostics(
                // (9,12): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         => GetStrings(await Task.FromResult(1)); // 1
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "GetStrings(await Task.FromResult(1))").WithLocation(9, 12),
                // (15,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return GetStrings(await Task.FromResult(1)); // 2
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(15, 9),
                // (23,18): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         if (b) { return GetStrings(); } // 3
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(23, 18),
                // (27,30): error CS8403: Method 'C.ReturnAndYieldReturn()' with an iterator block must be 'async' to return 'IAsyncEnumerable<string>'
                //     IAsyncEnumerable<string> ReturnAndYieldReturn() // 4
                Diagnostic(ErrorCode.ERR_IteratorMustBeAsync, "ReturnAndYieldReturn").WithArguments("C.ReturnAndYieldReturn()", "System.Collections.Generic.IAsyncEnumerable<string>").WithLocation(27, 30),
                // (30,18): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         if (b) { return GetStrings(); } // 5
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(30, 18)
                );
        }

        [Fact, WorkItem(38201, "https://github.com/dotnet/roslyn/issues/38201")]
        public void ReturningIAsyncEnumerable_Misc_LocalFunction()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
#pragma warning disable 8321 // unused local function
class C
{
    void M()
    {
        async IAsyncEnumerable<string> GetStrings(int i = 0) { yield return await Task.FromResult(""""); }

        async IAsyncEnumerable<string> AsyncExpressionBody()
            => GetStrings(await Task.FromResult(1)); // 1

        IAsyncEnumerable<string> ExpressionBody() => GetStrings();

        async IAsyncEnumerable<string> AsyncReturn()
        {
            return GetStrings(await Task.FromResult(1)); // 2
        }

        IAsyncEnumerable<string> Return() { return GetStrings(); }

        async IAsyncEnumerable<string> AsyncReturnAndYieldReturn()
        {
            bool b = true;
            if (b) { return GetStrings(); } // 3
            yield return await Task.FromResult("""");
        }

        IAsyncEnumerable<string> ReturnAndYieldReturn() // 4
        {
            bool b = true;
            if (b) { return GetStrings(); } // 5
            yield return """";
        }
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType });
            comp.VerifyDiagnostics(
                // (12,16): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //             => GetStrings(await Task.FromResult(1)); // 1
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "GetStrings(await Task.FromResult(1))").WithLocation(12, 16),
                // (18,13): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //             return GetStrings(await Task.FromResult(1)); // 2
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(18, 13),
                // (26,22): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //             if (b) { return GetStrings(); } // 3
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(26, 22),
                // (30,34): error CS8403: Method 'ReturnAndYieldReturn()' with an iterator block must be 'async' to return 'IAsyncEnumerable<string>'
                //         IAsyncEnumerable<string> ReturnAndYieldReturn() // 4
                Diagnostic(ErrorCode.ERR_IteratorMustBeAsync, "ReturnAndYieldReturn").WithArguments("ReturnAndYieldReturn()", "System.Collections.Generic.IAsyncEnumerable<string>").WithLocation(30, 34),
                // (33,22): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //             if (b) { return GetStrings(); } // 5
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(33, 22)
                );
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

        [Fact]
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

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.M");
                AssertEx.SetEqual(new[] { "AsyncIteratorStateMachineAttribute" },
                    GetAttributeNames(method.GetAttributes()));

                var attribute = method.GetAttributes().Single();
                var argument = attribute.ConstructorArguments.Single();
                Assert.Equal("System.Type", argument.Type.ToTestDisplayString());
                Assert.Equal("C.<M>d__0", ((ITypeSymbol)argument.Value).ToTestDisplayString());
            }).VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source);

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.M");
                AssertEx.SetEqual(["AsyncIteratorStateMachineAttribute"], GetAttributeNames(method.GetAttributes())); // PROTOTYPE confirm what attribute to use, if any
            }, verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void AttributesSynthesized_Optional()
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
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_AsyncIteratorStateMachineAttribute);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.M");
                Assert.Empty(GetAttributeNames(method.GetAttributes()));
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
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
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
            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctorSourceAndToken,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.Tasks.ValueTask`1..ctor'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ValueTask`1", ".ctor").WithLocation(5, 64)
                );

            VerifyMissingMember(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctorValue,
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
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.Tasks.ValueTask`1", ".ctor").WithLocation(5, 64),
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
        public void MissingTypeAndMembers_CancellationToken()
        {
            VerifyMissingMember(_enumerable, WellKnownMember.System_Threading_CancellationToken__Equals,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.CancellationToken.Equals'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.CancellationToken", "Equals").WithLocation(5, 64)
                );

            VerifyMissingMember(_enumerator, WellKnownMember.System_Threading_CancellationToken__Equals);

            VerifyMissingType(_enumerable, WellKnownType.System_Threading_CancellationToken,
                // (5,64): error CS0656: Missing compiler required member 'System.Collections.Generic.IAsyncEnumerable`1.GetAsyncEnumerator'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Collections.Generic.IAsyncEnumerable`1", "GetAsyncEnumerator").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.CancellationToken.Equals'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.CancellationToken", "Equals").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.CancellationTokenSource.CreateLinkedTokenSource'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.CancellationTokenSource", "CreateLinkedTokenSource").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.CancellationTokenSource.Token'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.CancellationTokenSource", "Token").WithLocation(5, 64)
                );

            VerifyMissingType(_enumerator, WellKnownType.System_Threading_CancellationToken);
        }

        [Fact]
        public void MissingTypeAndMembers_CancellationTokenSource()
        {
            VerifyMissingMember(_enumerable, WellKnownMember.System_Threading_CancellationTokenSource__CreateLinkedTokenSource,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.CancellationTokenSource.CreateLinkedTokenSource'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.CancellationTokenSource", "CreateLinkedTokenSource").WithLocation(5, 64)
                );

            VerifyMissingMember(_enumerable, WellKnownMember.System_Threading_CancellationTokenSource__Token,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.CancellationTokenSource.Token'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.CancellationTokenSource", "Token").WithLocation(5, 64)
                );

            VerifyMissingMember(_enumerable, WellKnownMember.System_Threading_CancellationTokenSource__Dispose,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.CancellationTokenSource.Dispose'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.CancellationTokenSource", "Dispose").WithLocation(5, 64)
                );

            VerifyMissingType(_enumerable, WellKnownType.System_Threading_CancellationTokenSource,
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.CancellationTokenSource.CreateLinkedTokenSource'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.CancellationTokenSource", "CreateLinkedTokenSource").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.CancellationTokenSource.Token'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.CancellationTokenSource", "Token").WithLocation(5, 64),
                // (5,64): error CS0656: Missing compiler required member 'System.Threading.CancellationTokenSource.Dispose'
                //     async System.Collections.Generic.IAsyncEnumerable<int> M() { await Task.CompletedTask; yield return 3; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.CompletedTask; yield return 3; }").WithArguments("System.Threading.CancellationTokenSource", "Dispose").WithLocation(5, 64)
                );

            VerifyMissingMember(_enumerator, WellKnownMember.System_Threading_CancellationTokenSource__CreateLinkedTokenSource);
            VerifyMissingMember(_enumerator, WellKnownMember.System_Threading_CancellationTokenSource__Token);
            VerifyMissingMember(_enumerator, WellKnownMember.System_Threading_CancellationTokenSource__Dispose);
            VerifyMissingType(_enumerator, WellKnownType.System_Threading_CancellationTokenSource);
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
                // (12,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return default;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(12, 9)
                );
        }

        [Fact]
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
            var expectedOutput = "Value:0 1 2 Value:3 4 Value:5 Done";

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, expectedOutput: expectedOutput, symbolValidator: verifyAsync1MembersAndInterfaces)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All));
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput),
                symbolValidator: verifyAsync2MembersAndInterfaces, verify: Verification.Skipped);
            verifier.VerifyDiagnostics();

            // Note: for enumerator case, the initial state is different than for enumerable case,
            //   and we preserve the parameters directly into the final fields (no extra backup).
            verifier.VerifyIL("C.M(int)", """
{
  // Code size       15 (0xf)
  .maxstack  3
  IL_0000:  ldc.i4.s   -3
  IL_0002:  newobj     "C.<M>d__0..ctor(int)"
  IL_0007:  dup
  IL_0008:  ldarg.0
  IL_0009:  stfld      "int C.<M>d__0.value"
  IL_000e:  ret
}
""");

            // Enumerable case for contrast
            source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M(int value)
    {
        yield return value;
        await System.Threading.Tasks.Task.CompletedTask;
    }
}";
            comp = CreateRuntimeAsyncCompilation(source);
            verifier = CompileAndVerify(comp, verify: Verification.Skipped)
                .VerifyDiagnostics();

            verifier.VerifyIL("C.M(int)", """
{
  // Code size       15 (0xf)
  .maxstack  3
  IL_0000:  ldc.i4.s   -2
  IL_0002:  newobj     "C.<M>d__0..ctor(int)"
  IL_0007:  dup
  IL_0008:  ldarg.0
  IL_0009:  stfld      "int C.<M>d__0.<>3__value"
  IL_000e:  ret
}
""");

            verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)", """
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (C.<M>d__0 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_002a
  IL_000a:  ldarg.0
  IL_000b:  ldfld      "int C.<M>d__0.<>l__initialThreadId"
  IL_0010:  call       "int System.Environment.CurrentManagedThreadId.get"
  IL_0015:  bne.un.s   IL_002a
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.s   -3
  IL_001a:  stfld      "int C.<M>d__0.<>1__state"
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4.0
  IL_0021:  stfld      "bool C.<M>d__0.<>w__disposeMode"
  IL_0026:  ldarg.0
  IL_0027:  stloc.0
  IL_0028:  br.s       IL_0032
  IL_002a:  ldc.i4.s   -3
  IL_002c:  newobj     "C.<M>d__0..ctor(int)"
  IL_0031:  stloc.0
  IL_0032:  ldloc.0
  IL_0033:  ldarg.0
  IL_0034:  ldfld      "int C.<M>d__0.<>3__value"
  IL_0039:  stfld      "int C.<M>d__0.value"
  IL_003e:  ldloc.0
  IL_003f:  ret
}
""");

            static void verifyAsync1MembersAndInterfaces(ModuleSymbol module)
            {
                var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C.<M>d__0");
                AssertEx.SetEqual([
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                    "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder",
                    "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<System.Boolean> C.<M>d__0.<>v__promiseOfValueOrEnd",
                    "System.Int32 C.<M>d__0.<>2__current",
                    "System.Boolean C.<M>d__0.<>w__disposeMode",
                    "System.Int32 C.<M>d__0.value",
                    "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1",
                    "C.<M>d__0..ctor(System.Int32 <>1__state)",
                    "void C.<M>d__0.MoveNext()",
                    "void C.<M>d__0.SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)",
                    "System.Threading.Tasks.ValueTask<System.Boolean> C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync()",
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current.get",
                    "System.Boolean C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.GetResult(System.Int16 token)",
                    "System.Threading.Tasks.Sources.ValueTaskSourceStatus C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.GetStatus(System.Int16 token)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(System.Int16 token)",
                    "System.Threading.Tasks.Sources.ValueTaskSourceStatus C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.GetStatus(System.Int16 token)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)",
                    "System.Threading.Tasks.ValueTask C.<M>d__0.System.IAsyncDisposable.DisposeAsync()",
                    "System.Int32 C.<M>d__0.<>1__state"
                    ], type.GetMembersUnordered().ToTestDisplayStrings());

                AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IAsyncStateMachine",
                    "System.IAsyncDisposable",
                    "System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>",
                    "System.Threading.Tasks.Sources.IValueTaskSource",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32>"
                    ], type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys.ToTestDisplayStrings());
            }

            static void verifyAsync2MembersAndInterfaces(ModuleSymbol module)
            {
                var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C.<M>d__0");
                AssertEx.SetEqual([
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                    "System.Int32 C.<M>d__0.<>2__current",
                    "System.Boolean C.<M>d__0.<>w__disposeMode",
                    "System.Int32 C.<M>d__0.value",
                    "C.<M>d__0..ctor(System.Int32 <>1__state)",
                    "System.Threading.Tasks.ValueTask<System.Boolean> C.<M>d__0.MoveNextAsync()",
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current.get",
                    "System.Threading.Tasks.ValueTask C.<M>d__0.System.IAsyncDisposable.DisposeAsync()",
                    "System.Int32 C.<M>d__0.<>1__state"
                    ], type.GetMembersUnordered().ToTestDisplayStrings());

                AssertEx.SetEqual([
                    "System.IAsyncDisposable",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32>"
                    ], type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys.ToTestDisplayStrings());
            }
        }

        [Fact]
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
            var expected = new[]
            {
                // 1.cs(2,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                // #nullable disable
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(2, 2),
                // 0.cs(4,60): error CS8370: Feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M").WithArguments("async streams", "8.0").WithLocation(4, 60),
                // 1.cs(21,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                // #nullable disable
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(21, 2)
            };
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(expected);

            comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
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
                // (6,9): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         return null;
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(6, 9)
                );
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        [WorkItem(31057, "https://github.com/dotnet/roslyn/issues/31057")]
        [WorkItem(31113, "https://github.com/dotnet/roslyn/issues/31113")]
        [WorkItem(31608, "https://github.com/dotnet/roslyn/issues/31608")]
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
                // (4,61): error CS8403: Method 'C.M(int)' with an iterator block must be 'async' to return 'IAsyncEnumerator<int>'
                //     static System.Collections.Generic.IAsyncEnumerator<int> M(int value)
                Diagnostic(ErrorCode.ERR_IteratorMustBeAsync, "M").WithArguments("C.M(int)", "System.Collections.Generic.IAsyncEnumerator<int>").WithLocation(4, 61),
                // (7,9): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<IAsyncEnumerator<int>>'.
                //         await System.Threading.Tasks.Task.CompletedTask;
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await System.Threading.Tasks.Task.CompletedTask").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>").WithLocation(7, 9)
                );

            // This error message is rather poor. Tracked by https://github.com/dotnet/roslyn/issues/31113
        }

        [Fact]
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

        [Fact]
        [WorkItem(31552, "https://github.com/dotnet/roslyn/issues/31552")]
        public void AsyncIterator_WithThrowOnly()
        {
            string source = @"
class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        throw new System.NotImplementedException();
    }
}";
            DiagnosticDescription[] expectedDiagnostics = [
                // (4,74): error CS8420: The body of an async-iterator method must contain a 'yield' statement. Consider removing 'async' from the method declaration or adding a 'yield' statement.
                //     public static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_PossibleAsyncIteratorWithoutYieldOrAwait, "M").WithLocation(4, 74)
                ];

            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            var m = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            Assert.False(m.IsIterator);
            Assert.True(m.IsAsync);

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        [WorkItem(31552, "https://github.com/dotnet/roslyn/issues/31552")]
        public void AsyncIteratorReturningEnumerator_WithThrowOnly()
        {
            string source = @"
class C
{
    public static async System.Collections.Generic.IAsyncEnumerator<int> M()
    {
        throw new System.NotImplementedException();
    }
}";
            DiagnosticDescription[] expectedDiagnostics = [
                // (4,74): error CS8420: The body of an async-iterator method must contain a 'yield' statement. Consider removing `async` from the method declaration.
                //     public static async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_PossibleAsyncIteratorWithoutYieldOrAwait, "M").WithLocation(4, 74)
                ];

            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        [WorkItem(31552, "https://github.com/dotnet/roslyn/issues/31552")]
        public void AsyncIteratorReturningEnumerator_WithAwaitAndThrow()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerator<int> M()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        throw new System.NotImplementedException();
    }
}";

            DiagnosticDescription[] expectedDiagnostics = [
                // (4,60): error CS8419: The body of an async-iterator method must contain a 'yield' statement.
                //     async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_PossibleAsyncIteratorWithoutYield, "M").WithLocation(4, 60)
                ];

            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void AsyncIteratorReturningEnumerable_WithAwaitAndThrow_LocalFunction()
        {
            string source = """
await foreach (var i in local()) { }

async System.Collections.Generic.IAsyncEnumerable<int> local()
{
    await System.Threading.Tasks.Task.CompletedTask;
    throw new System.NotImplementedException();
}
""";

            DiagnosticDescription[] expectedDiagnostics = [
                // source(3,56): error CS8419: The body of an async-iterator method must contain a 'yield' statement.
                // async System.Collections.Generic.IAsyncEnumerable<int> local()
                Diagnostic(ErrorCode.ERR_PossibleAsyncIteratorWithoutYield, "local").WithLocation(3, 56)
                ];

            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        [WorkItem(31552, "https://github.com/dotnet/roslyn/issues/31552")]
        public void AsyncIteratorReturningEnumerator_WithThrow_WithAwaitInLambda()
        {
            string source = @"
class C
{
    async System.Collections.Generic.IAsyncEnumerator<int> M()
    {
        System.Func<System.Threading.Tasks.Task> lambda = async () => { await System.Threading.Tasks.Task.CompletedTask; };
        throw new System.NotImplementedException();
    }
}";
            DiagnosticDescription[] expectedDiagnostics = [
                // source(4,60): error CS8420: The body of an async-iterator method must contain a 'yield' statement. Consider removing 'async' from the method declaration or adding a 'yield' statement.
                //     async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_PossibleAsyncIteratorWithoutYieldOrAwait, "M").WithLocation(4, 60)
                ];

            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void AsyncIteratorReturningEnumerator_WithThrow_WithAwaitInLocalFunction()
        {
            string source = """
class C
{
    async System.Collections.Generic.IAsyncEnumerator<int> M()
    {
        async System.Threading.Tasks.Task local() { await System.Threading.Tasks.Task.CompletedTask; };
        throw new System.NotImplementedException();
    }
}
""";
            DiagnosticDescription[] expectedDiagnostics = [
                // source(3,60): error CS8420: The body of an async-iterator method must contain a 'yield' statement. Consider removing 'async' from the method declaration or adding a 'yield' statement.
                //     async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_PossibleAsyncIteratorWithoutYieldOrAwait, "M").WithLocation(3, 60),
                // source(5,43): warning CS8321: The local function 'local' is declared but never used
                //         async System.Threading.Tasks.Task local() { await System.Threading.Tasks.Task.CompletedTask; };
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(5, 43)
                ];

            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        [WorkItem(31552, "https://github.com/dotnet/roslyn/issues/31552")]
        public void AsyncIterator_WithEmptyBody()
        {
            string source = @"
class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (4,74): error CS0161: 'C.M()': not all code paths return a value
                //     public static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M()").WithLocation(4, 74)
                );
        }

        [Fact]
        [WorkItem(31608, "https://github.com/dotnet/roslyn/issues/31608")]
        public void AsyncIterator_WithoutAwait()
        {
            string source = @"
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
    }
}";
            var expectedOutput = "1 END DISPOSAL DONE";

            var comp = CreateCompilationWithAsyncIterator([Run(iterations: 2), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations: 2), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput))
                .VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(31608, "https://github.com/dotnet/roslyn/issues/31608")]
        [WorkItem(39970, "https://github.com/dotnet/roslyn/issues/39970")]
        public void AsyncIterator_WithoutAwait_WithoutAsync()
        {
            string source = @"
class C
{
    static System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            var expected = new DiagnosticDescription[] {
                // (4,61): error CS8403: Method 'C.M()' with an iterator block must be 'async' to return 'IAsyncEnumerable<int>'
                //     static System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_IteratorMustBeAsync, "M").WithArguments("C.M()", "System.Collections.Generic.IAsyncEnumerable<int>").WithLocation(4, 61)
            };
            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        [WorkItem(31608, "https://github.com/dotnet/roslyn/issues/31608")]
        public void AsyncIterator_WithoutAwait_WithoutAsync_LocalFunction()
        {
            string source = @"
class C
{
    void M()
    {
        _ = local();
        static System.Collections.Generic.IAsyncEnumerator<int> local()
        {
            yield break;
        }
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (7,65): error CS8403: Method 'local()' with an iterator block must be 'async' to return 'IAsyncEnumerator<int>'
                //         static System.Collections.Generic.IAsyncEnumerator<int> local()
                Diagnostic(ErrorCode.ERR_IteratorMustBeAsync, "local").WithArguments("local()", "System.Collections.Generic.IAsyncEnumerator<int>").WithLocation(7, 65)
                );
        }

        [Fact]
        [WorkItem(31608, "https://github.com/dotnet/roslyn/issues/31608")]
        public void Iterator_WithAsync()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IEnumerable<int> M()
    {
        yield return 1;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (4,62): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //     static async System.Collections.Generic.IEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "M").WithLocation(4, 62)
                );
        }

        [Fact]
        [WorkItem(31552, "https://github.com/dotnet/roslyn/issues/31552")]
        public void AsyncIteratorReturningEnumerator_WithoutBody()
        {
            string source = @"
class C
{
    public static async System.Collections.Generic.IAsyncEnumerator<int> M()
    {
    }
}";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (4,74): error CS0161: 'C.M()': not all code paths return a value
                //     public static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M()").WithLocation(4, 74)
                );
        }

        [Fact]
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
            comp.VerifyDiagnostics();
        }

        [Fact]
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

        [Fact]
        public void CallingMoveNextAsyncTwice()
        {
            string source = """
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write("1 ");
        await System.Threading.Tasks.Task.CompletedTask;
        Write("2 ");
        yield return 3;
        Write("4 ");
    }
    static async System.Threading.Tasks.Task Main()
    {
        Write("0 ");
        await using (var enumerator = M().GetAsyncEnumerator())
        {
            var found = await enumerator.MoveNextAsync();
            if (!found) throw null;
            var value = enumerator.Current;
            Write($"{value} ");
            found = await enumerator.MoveNextAsync();
            if (found) throw null;
            found = await enumerator.MoveNextAsync();
            if (found) throw null;
            Write("5");
        }
    }
}
""";
            var expectedOutput = "0 1 2 3 4 5";

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(30275, "https://github.com/dotnet/roslyn/issues/30275")]
        public void CallingGetEnumeratorTwice()
        {
            // Shows that disposeMode and parameter values are reset upon second enumeration
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
            var expectedOutput = "1 2 Stream1:3 1 2 Stream2:3 4 2 4 2 Done";

            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes },
                options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(comp, expectedOutput: expectedOutput, symbolValidator: verifyAsync1MembersAndInterfaces)
                .VerifyDiagnostics();
            // Illustrates that parameters are proxied (we save the original in the enumerable, then copy them into working fields when making an enumerator)

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All));

            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput),
                verify: Verification.Skipped, symbolValidator: verifyAsync2MembersAndInterfaces);
            verifier.VerifyDiagnostics();

            static void verifyAsync1MembersAndInterfaces(ModuleSymbol module)
            {
                var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C.<M>d__0");
                AssertEx.SetEqual([
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                    "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder",
                    "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<System.Boolean> C.<M>d__0.<>v__promiseOfValueOrEnd",
                    "System.Int32 C.<M>d__0.<>2__current",
                    "System.Boolean C.<M>d__0.<>w__disposeMode",
                    "System.Int32 C.<M>d__0.<>l__initialThreadId",
                    "System.Int32 C.<M>d__0.value",
                    "System.Int32 C.<M>d__0.<>3__value",
                    "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1",
                    "C.<M>d__0..ctor(System.Int32 <>1__state)",
                    "void C.<M>d__0.MoveNext()",
                    "void C.<M>d__0.SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32> C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
                    "System.Threading.Tasks.ValueTask<System.Boolean> C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync()",
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current.get",
                    "System.Boolean C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.GetResult(System.Int16 token)",
                    "System.Threading.Tasks.Sources.ValueTaskSourceStatus C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.GetStatus(System.Int16 token)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(System.Int16 token)",
                    "System.Threading.Tasks.Sources.ValueTaskSourceStatus C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.GetStatus(System.Int16 token)",
                    "void C.<M>d__0.System.Threading.Tasks.Sources.IValueTaskSource.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)",
                    "System.Threading.Tasks.ValueTask C.<M>d__0.System.IAsyncDisposable.DisposeAsync()",
                    "System.Int32 C.<M>d__0.<>1__state"
                    ], type.GetMembersUnordered().ToTestDisplayStrings());

                AssertEx.SetEqual([
                    "System.Runtime.CompilerServices.IAsyncStateMachine",
                    "System.IAsyncDisposable",
                    "System.Threading.Tasks.Sources.IValueTaskSource",
                    "System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>",
                    "System.Collections.Generic.IAsyncEnumerable<System.Int32>",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32>"
                    ], type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys.ToTestDisplayStrings());
            }

            static void verifyAsync2MembersAndInterfaces(ModuleSymbol module)
            {
                var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C.<M>d__0");
                AssertEx.SetEqual([
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                    "System.Int32 C.<M>d__0.<>2__current",
                    "System.Boolean C.<M>d__0.<>w__disposeMode",
                    "System.Int32 C.<M>d__0.<>l__initialThreadId",
                    "System.Int32 C.<M>d__0.value",
                    "System.Int32 C.<M>d__0.<>3__value",
                    "C.<M>d__0..ctor(System.Int32 <>1__state)",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32> C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)])",
                    "System.Threading.Tasks.ValueTask<System.Boolean> C.<M>d__0.MoveNextAsync()",
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current.get",
                    "System.Threading.Tasks.ValueTask C.<M>d__0.System.IAsyncDisposable.DisposeAsync()",
                    "System.Int32 C.<M>d__0.<>1__state"
                    ], type.GetMembersUnordered().ToTestDisplayStrings());

                AssertEx.SetEqual([
                    "System.Collections.Generic.IAsyncEnumerable<System.Int32>",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32>",
                    "System.IAsyncDisposable"
                    ], type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys.ToTestDisplayStrings());
            }
        }

        [Fact]
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
            var expectedOutput = "1 2 Stream1:3 4 2 1 2 Stream2:3 4 2 Done";

            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
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
            var expectedOutput = "Stream1:0 Stream2:0 1 2 Stream1:3 4 2 1 2 Stream2:3 4 2 Done";

            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
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
        await Task.Yield();
        Write(""2 "");
        yield return 3;
        await Task.Yield();
        Write(""4 "");
        value++;
        await Task.Yield();
        Write($""{value} "");
        await Task.Yield();
    }
    static async Task Main()
    {
        var enumerable = M(41);
        await foreach (var item1 in enumerable)
        {
            Write($""Stream1:{item1} "");
        }
        Write(""Await "");
        await Task.Yield();
        await foreach (var item2 in enumerable)
        {
            Write($""Stream2:{item2} "");
        }

        Write(""Done"");
    }
}";
            var expectedOutput = "Stream1:0 1 2 Stream1:3 4 42 Await Stream2:0 1 2 Stream2:3 4 42 Done";

            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void CallingGetEnumeratorTwice_AfterDisposing()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M(int value)
    {
        try
        {
            yield return 1;
            await System.Threading.Tasks.Task.Yield();
            yield return 2;
        }
        finally
        {
            Write(""Finally "");
        }
    }
    static async System.Threading.Tasks.Task Main()
    {
        var enumerable = M(1);

        var enumerator1 = enumerable.GetAsyncEnumerator();
        if (!await enumerator1.MoveNextAsync()) throw null;
        Write($""Stream1:{enumerator1.Current} "");

        await enumerator1.DisposeAsync();

        await foreach (var i in enumerable)
        {
            Write($""Stream2:{i} "");
        }

        Write(""Done"");
    }
}";
            var expectedOutput = "Stream1:1 Finally Stream2:1 Stream2:2 Finally Done";

            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                 .VerifyDiagnostics();

            // Note: the dispose mode is set back to false during reset
            verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)", """
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (C.<M>d__0 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_002a
  IL_000a:  ldarg.0
  IL_000b:  ldfld      "int C.<M>d__0.<>l__initialThreadId"
  IL_0010:  call       "int System.Environment.CurrentManagedThreadId.get"
  IL_0015:  bne.un.s   IL_002a
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.s   -3
  IL_001a:  stfld      "int C.<M>d__0.<>1__state"
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4.0
  IL_0021:  stfld      "bool C.<M>d__0.<>w__disposeMode"
  IL_0026:  ldarg.0
  IL_0027:  stloc.0
  IL_0028:  br.s       IL_0032
  IL_002a:  ldc.i4.s   -3
  IL_002c:  newobj     "C.<M>d__0..ctor(int)"
  IL_0031:  stloc.0
  IL_0032:  ldloc.0
  IL_0033:  ldarg.0
  IL_0034:  ldfld      "int C.<M>d__0.<>3__value"
  IL_0039:  stfld      "int C.<M>d__0.value"
  IL_003e:  ldloc.0
  IL_003f:  ret
}
""");
        }

        [Fact]
        public void AsyncIteratorWithAwaitCompletedAndYield()
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
                var expectedOutput = "0 1 2 3 4 5";

                var comp = CreateCompilationWithAsyncIterator(source, options: options);
                var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                var expectedFields = new[] {
                    "FieldDefinition:Int32 <>1__state",
                    "FieldDefinition:System.Runtime.CompilerServices.AsyncIteratorMethodBuilder <>t__builder",
                    "FieldDefinition:System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1{Boolean} <>v__promiseOfValueOrEnd",
                    "FieldDefinition:Int32 <>2__current",
                    "FieldDefinition:Boolean <>w__disposeMode",
                    "FieldDefinition:Int32 <>l__initialThreadId",
                    "FieldDefinition:System.Runtime.CompilerServices.TaskAwaiter <>u__1"
                };
                VerifyStateMachineFields(comp, "<M>d__0", expectedFields);

                verifier.VerifyIL("C.M", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.s   -2
  IL_0002:  newobj     ""C.<M>d__0..ctor(int)""
  IL_0007:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Enhanced);

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
  IL_0008:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_000d:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_0012:  ldarg.0
  IL_0013:  ldarg.1
  IL_0014:  stfld      ""int C.<M>d__0.<>1__state""
  IL_0019:  ldarg.0
  IL_001a:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_001f:  stfld      ""int C.<M>d__0.<>l__initialThreadId""
  IL_0024:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Enhanced);
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
  IL_0007:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_000c:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_0011:  ldarg.0
  IL_0012:  ldarg.1
  IL_0013:  stfld      ""int C.<M>d__0.<>1__state""
  IL_0018:  ldarg.0
  IL_0019:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_001e:  stfld      ""int C.<M>d__0.<>l__initialThreadId""
  IL_0023:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Enhanced);
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
  // Code size       99 (0x63)
  .maxstack  2
  .locals init (C.<M>d__0 V_0,
                short V_1,
                System.Threading.Tasks.ValueTask<bool> V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0014
  IL_000a:  ldloca.s   V_2
  IL_000c:  initobj    ""System.Threading.Tasks.ValueTask<bool>""
  IL_0012:  ldloc.2
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
  IL_002f:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0034:  call       ""short System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.Version.get""
  IL_0039:  stloc.1
  IL_003a:  ldarg.0
  IL_003b:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0040:  ldloc.1
  IL_0041:  call       ""System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.GetStatus(short)""
  IL_0046:  ldc.i4.1
  IL_0047:  bne.un.s   IL_005b
  IL_0049:  ldarg.0
  IL_004a:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_004f:  ldloc.1
  IL_0050:  call       ""bool System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.GetResult(short)""
  IL_0055:  newobj     ""System.Threading.Tasks.ValueTask<bool>..ctor(bool)""
  IL_005a:  ret
  IL_005b:  ldarg.0
  IL_005c:  ldloc.1
  IL_005d:  newobj     ""System.Threading.Tasks.ValueTask<bool>..ctor(System.Threading.Tasks.Sources.IValueTaskSource<bool>, short)""
  IL_0062:  ret
}");
                verifier.VerifyIL("C.<M>d__0.System.IAsyncDisposable.DisposeAsync()", @"
{
  // Code size       86 (0x56)
  .maxstack  2
  .locals init (C.<M>d__0 V_0,
                System.Threading.Tasks.ValueTask V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  ldc.i4.m1
  IL_0007:  blt.s      IL_000f
  IL_0009:  newobj     ""System.NotSupportedException..ctor()""
  IL_000e:  throw
  IL_000f:  ldarg.0
  IL_0010:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0015:  ldc.i4.s   -2
  IL_0017:  bne.un.s   IL_0023
  IL_0019:  ldloca.s   V_1
  IL_001b:  initobj    ""System.Threading.Tasks.ValueTask""
  IL_0021:  ldloc.1
  IL_0022:  ret
  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.1
  IL_0025:  stfld      ""bool C.<M>d__0.<>w__disposeMode""
  IL_002a:  ldarg.0
  IL_002b:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0030:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.Reset()""
  IL_0035:  ldarg.0
  IL_0036:  stloc.0
  IL_0037:  ldarg.0
  IL_0038:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_003d:  ldloca.s   V_0
  IL_003f:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.MoveNext<C.<M>d__0>(ref C.<M>d__0)""
  IL_0044:  ldarg.0
  IL_0045:  ldarg.0
  IL_0046:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_004b:  call       ""short System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.Version.get""
  IL_0050:  newobj     ""System.Threading.Tasks.ValueTask..ctor(System.Threading.Tasks.Sources.IValueTaskSource, short)""
  IL_0055:  ret
}
");
                verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)", @"
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (C.<M>d__0 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0035
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""int C.<M>d__0.<>l__initialThreadId""
  IL_0010:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_0015:  bne.un.s   IL_0035
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.s   -3
  IL_001a:  stfld      ""int C.<M>d__0.<>1__state""
  IL_001f:  ldarg.0
  IL_0020:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_0025:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_002a:  ldarg.0
  IL_002b:  ldc.i4.0
  IL_002c:  stfld      ""bool C.<M>d__0.<>w__disposeMode""
  IL_0031:  ldarg.0
  IL_0032:  stloc.0
  IL_0033:  br.s       IL_003d
  IL_0035:  ldc.i4.s   -3
  IL_0037:  newobj     ""C.<M>d__0..ctor(int)""
  IL_003c:  stloc.0
  IL_003d:  ldloc.0
  IL_003e:  ret
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
                    verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"{
  // Code size      336 (0x150)
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
    IL_0008:  ldc.i4.s   -4
    IL_000a:  sub
    IL_000b:  switch    (
        IL_0026,
        IL_002b,
        IL_002f,
        IL_002f,
        IL_002d)
    IL_0024:  br.s       IL_002f
    IL_0026:  br         IL_00ce
    IL_002b:  br.s       IL_002f
    IL_002d:  br.s       IL_008c
    IL_002f:  ldarg.0
    IL_0030:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_0035:  brfalse.s  IL_003c
    IL_0037:  leave      IL_0119
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.m1
    IL_003e:  dup
    IL_003f:  stloc.0
    IL_0040:  stfld      ""int C.<M>d__0.<>1__state""
    // sequence point: {
    IL_0045:  nop
    // sequence point: Write(""1 "");
    IL_0046:  ldstr      ""1 ""
    IL_004b:  call       ""void System.Console.Write(string)""
    IL_0050:  nop
    // sequence point: await System.Threading.Tasks.Task.CompletedTask;
    IL_0051:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get""
    IL_0056:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_005b:  stloc.1
    // sequence point: <hidden>
    IL_005c:  ldloca.s   V_1
    IL_005e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0063:  brtrue.s   IL_00a8
    IL_0065:  ldarg.0
    IL_0066:  ldc.i4.0
    IL_0067:  dup
    IL_0068:  stloc.0
    IL_0069:  stfld      ""int C.<M>d__0.<>1__state""
    // async: yield
    IL_006e:  ldarg.0
    IL_006f:  ldloc.1
    IL_0070:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0075:  ldarg.0
    IL_0076:  stloc.2
    IL_0077:  ldarg.0
    IL_0078:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_007d:  ldloca.s   V_1
    IL_007f:  ldloca.s   V_2
    IL_0081:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)""
    IL_0086:  nop
    IL_0087:  leave      IL_014f
    // async: resume
    IL_008c:  ldarg.0
    IL_008d:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0092:  stloc.1
    IL_0093:  ldarg.0
    IL_0094:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0099:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.m1
    IL_00a1:  dup
    IL_00a2:  stloc.0
    IL_00a3:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00a8:  ldloca.s   V_1
    IL_00aa:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00af:  nop
    // sequence point: Write(""2 "");
    IL_00b0:  ldstr      ""2 ""
    IL_00b5:  call       ""void System.Console.Write(string)""
    IL_00ba:  nop
    // sequence point: yield return 3;
    IL_00bb:  ldarg.0
    IL_00bc:  ldc.i4.3
    IL_00bd:  stfld      ""int C.<M>d__0.<>2__current""
    IL_00c2:  ldarg.0
    IL_00c3:  ldc.i4.s   -4
    IL_00c5:  dup
    IL_00c6:  stloc.0
    IL_00c7:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00cc:  leave.s    IL_0142
    // sequence point: <hidden>
    IL_00ce:  ldarg.0
    IL_00cf:  ldc.i4.m1
    IL_00d0:  dup
    IL_00d1:  stloc.0
    IL_00d2:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00d7:  ldarg.0
    IL_00d8:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_00dd:  brfalse.s  IL_00e1
    IL_00df:  leave.s    IL_0119
    // sequence point: Write("" 4 "");
    IL_00e1:  ldstr      "" 4 ""
    IL_00e6:  call       ""void System.Console.Write(string)""
    IL_00eb:  nop
    IL_00ec:  leave.s    IL_0119
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_00ee:  stloc.3
    IL_00ef:  ldarg.0
    IL_00f0:  ldc.i4.s   -2
    IL_00f2:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00f7:  ldarg.0
    IL_00f8:  ldc.i4.0
    IL_00f9:  stfld      ""int C.<M>d__0.<>2__current""
    IL_00fe:  ldarg.0
    IL_00ff:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_0104:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_0109:  nop
    IL_010a:  ldarg.0
    IL_010b:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_0110:  ldloc.3
    IL_0111:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_0116:  nop
    IL_0117:  leave.s    IL_014f
  }
  // sequence point: }
  IL_0119:  ldarg.0
  IL_011a:  ldc.i4.s   -2
  IL_011c:  stfld      ""int C.<M>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0121:  ldarg.0
  IL_0122:  ldc.i4.0
  IL_0123:  stfld      ""int C.<M>d__0.<>2__current""
  IL_0128:  ldarg.0
  IL_0129:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_012e:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_0133:  nop
  IL_0134:  ldarg.0
  IL_0135:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_013a:  ldc.i4.0
  IL_013b:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0140:  nop
  IL_0141:  ret
  IL_0142:  ldarg.0
  IL_0143:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0148:  ldc.i4.1
  IL_0149:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_014e:  nop
  IL_014f:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Enhanced);
                }
                else
                {
                    verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      314 (0x13a)
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
    IL_0008:  ldc.i4.s   -4
    IL_000a:  sub
    IL_000b:  switch    (
        IL_00be,
        IL_0024,
        IL_0024,
        IL_0024,
        IL_007e)
    IL_0024:  ldarg.0
    IL_0025:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_002a:  brfalse.s  IL_0031
    IL_002c:  leave      IL_0106
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.m1
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int C.<M>d__0.<>1__state""
    // sequence point: Write(""1 "");
    IL_003a:  ldstr      ""1 ""
    IL_003f:  call       ""void System.Console.Write(string)""
    // sequence point: await System.Threading.Tasks.Task.CompletedTask;
    IL_0044:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get""
    IL_0049:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_004e:  stloc.1
    // sequence point: <hidden>
    IL_004f:  ldloca.s   V_1
    IL_0051:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0056:  brtrue.s   IL_009a
    IL_0058:  ldarg.0
    IL_0059:  ldc.i4.0
    IL_005a:  dup
    IL_005b:  stloc.0
    IL_005c:  stfld      ""int C.<M>d__0.<>1__state""
    // async: yield
    IL_0061:  ldarg.0
    IL_0062:  ldloc.1
    IL_0063:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0068:  ldarg.0
    IL_0069:  stloc.2
    IL_006a:  ldarg.0
    IL_006b:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_0070:  ldloca.s   V_1
    IL_0072:  ldloca.s   V_2
    IL_0074:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)""
    IL_0079:  leave      IL_0139
    // async: resume
    IL_007e:  ldarg.0
    IL_007f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0084:  stloc.1
    IL_0085:  ldarg.0
    IL_0086:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_008b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0091:  ldarg.0
    IL_0092:  ldc.i4.m1
    IL_0093:  dup
    IL_0094:  stloc.0
    IL_0095:  stfld      ""int C.<M>d__0.<>1__state""
    IL_009a:  ldloca.s   V_1
    IL_009c:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    // sequence point: Write(""2 "");
    IL_00a1:  ldstr      ""2 ""
    IL_00a6:  call       ""void System.Console.Write(string)""
    // sequence point: yield return 3;
    IL_00ab:  ldarg.0
    IL_00ac:  ldc.i4.3
    IL_00ad:  stfld      ""int C.<M>d__0.<>2__current""
    IL_00b2:  ldarg.0
    IL_00b3:  ldc.i4.s   -4
    IL_00b5:  dup
    IL_00b6:  stloc.0
    IL_00b7:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00bc:  leave.s    IL_012d
    // sequence point: <hidden>
    IL_00be:  ldarg.0
    IL_00bf:  ldc.i4.m1
    IL_00c0:  dup
    IL_00c1:  stloc.0
    IL_00c2:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00c7:  ldarg.0
    IL_00c8:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_00cd:  brfalse.s  IL_00d1
    IL_00cf:  leave.s    IL_0106
    // sequence point: Write("" 4 "");
    IL_00d1:  ldstr      "" 4 ""
    IL_00d6:  call       ""void System.Console.Write(string)""
    IL_00db:  leave.s    IL_0106
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_00dd:  stloc.3
    IL_00de:  ldarg.0
    IL_00df:  ldc.i4.s   -2
    IL_00e1:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00e6:  ldarg.0
    IL_00e7:  ldc.i4.0
    IL_00e8:  stfld      ""int C.<M>d__0.<>2__current""
    IL_00ed:  ldarg.0
    IL_00ee:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_00f3:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_00f8:  ldarg.0
    IL_00f9:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_00fe:  ldloc.3
    IL_00ff:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_0104:  leave.s    IL_0139
  }
  // sequence point: }
  IL_0106:  ldarg.0
  IL_0107:  ldc.i4.s   -2
  IL_0109:  stfld      ""int C.<M>d__0.<>1__state""
  // sequence point: <hidden>
  IL_010e:  ldarg.0
  IL_010f:  ldc.i4.0
  IL_0110:  stfld      ""int C.<M>d__0.<>2__current""
  IL_0115:  ldarg.0
  IL_0116:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_011b:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_0120:  ldarg.0
  IL_0121:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0126:  ldc.i4.0
  IL_0127:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_012c:  ret
  IL_012d:  ldarg.0
  IL_012e:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0133:  ldc.i4.1
  IL_0134:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0139:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Enhanced);

                    comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
                    CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                        .VerifyDiagnostics();
                }
            }
        }

        [Fact]
        public void AsyncIteratorWithAwaitCompletedAndYield_WithEnumeratorCancellation()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M([EnumeratorCancellation] CancellationToken token)
    {
        _ = token;
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 3;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);

            var expectedFields = new[] {
                "FieldDefinition:Int32 <>1__state",
                "FieldDefinition:System.Runtime.CompilerServices.AsyncIteratorMethodBuilder <>t__builder",
                "FieldDefinition:System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1{Boolean} <>v__promiseOfValueOrEnd",
                "FieldDefinition:Int32 <>2__current",
                "FieldDefinition:Boolean <>w__disposeMode",
                "FieldDefinition:System.Threading.CancellationTokenSource <>x__combinedTokens",
                "FieldDefinition:Int32 <>l__initialThreadId",
                "FieldDefinition:System.Threading.CancellationToken token",
                "FieldDefinition:System.Threading.CancellationToken <>3__token",
                "FieldDefinition:System.Runtime.CompilerServices.TaskAwaiter <>u__1"
            };
            VerifyStateMachineFields(comp, "<M>d__0", expectedFields);

            // we generate initialization logic for the token parameter
            verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)", @"
{
  // Code size      176 (0xb0)
  .maxstack  3
  .locals init (C.<M>d__0 V_0,
                System.Threading.CancellationToken V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0035
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""int C.<M>d__0.<>l__initialThreadId""
  IL_0010:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_0015:  bne.un.s   IL_0035
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.s   -3
  IL_001a:  stfld      ""int C.<M>d__0.<>1__state""
  IL_001f:  ldarg.0
  IL_0020:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_0025:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_002a:  ldarg.0
  IL_002b:  ldc.i4.0
  IL_002c:  stfld      ""bool C.<M>d__0.<>w__disposeMode""
  IL_0031:  ldarg.0
  IL_0032:  stloc.0
  IL_0033:  br.s       IL_003d
  IL_0035:  ldc.i4.s   -3
  IL_0037:  newobj     ""C.<M>d__0..ctor(int)""
  IL_003c:  stloc.0
  IL_003d:  ldarg.0
  IL_003e:  ldflda     ""System.Threading.CancellationToken C.<M>d__0.<>3__token""
  IL_0043:  ldloca.s   V_1
  IL_0045:  initobj    ""System.Threading.CancellationToken""
  IL_004b:  ldloc.1
  IL_004c:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_0051:  brfalse.s  IL_005c
  IL_0053:  ldloc.0
  IL_0054:  ldarg.1
  IL_0055:  stfld      ""System.Threading.CancellationToken C.<M>d__0.token""
  IL_005a:  br.s       IL_00ae
  IL_005c:  ldarga.s   V_1
  IL_005e:  ldarg.0
  IL_005f:  ldfld      ""System.Threading.CancellationToken C.<M>d__0.<>3__token""
  IL_0064:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_0069:  brtrue.s   IL_007d
  IL_006b:  ldarga.s   V_1
  IL_006d:  ldloca.s   V_1
  IL_006f:  initobj    ""System.Threading.CancellationToken""
  IL_0075:  ldloc.1
  IL_0076:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_007b:  brfalse.s  IL_008b
  IL_007d:  ldloc.0
  IL_007e:  ldarg.0
  IL_007f:  ldfld      ""System.Threading.CancellationToken C.<M>d__0.<>3__token""
  IL_0084:  stfld      ""System.Threading.CancellationToken C.<M>d__0.token""
  IL_0089:  br.s       IL_00ae
  IL_008b:  ldarg.0
  IL_008c:  ldarg.0
  IL_008d:  ldfld      ""System.Threading.CancellationToken C.<M>d__0.<>3__token""
  IL_0092:  ldarg.1
  IL_0093:  call       ""System.Threading.CancellationTokenSource System.Threading.CancellationTokenSource.CreateLinkedTokenSource(System.Threading.CancellationToken, System.Threading.CancellationToken)""
  IL_0098:  stfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_009d:  ldloc.0
  IL_009e:  ldarg.0
  IL_009f:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_00a4:  callvirt   ""System.Threading.CancellationToken System.Threading.CancellationTokenSource.Token.get""
  IL_00a9:  stfld      ""System.Threading.CancellationToken C.<M>d__0.token""
  IL_00ae:  ldloc.0
  IL_00af:  ret
}");

            // we generate disposal logic for the combinedTokens field
            verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      343 (0x157)
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
    IL_0008:  ldc.i4.s   -4
    IL_000a:  sub
    IL_000b:  switch    (
        IL_00b4,
        IL_0024,
        IL_0024,
        IL_0024,
        IL_007b)
    IL_0024:  ldarg.0
    IL_0025:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_002a:  brfalse.s  IL_0031
    IL_002c:  leave      IL_0109
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.m1
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int C.<M>d__0.<>1__state""
    // sequence point: _ = token;
    IL_003a:  ldarg.0
    IL_003b:  ldfld      ""System.Threading.CancellationToken C.<M>d__0.token""
    IL_0040:  pop
    // sequence point: await System.Threading.Tasks.Task.CompletedTask;
    IL_0041:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get""
    IL_0046:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_004b:  stloc.1
    // sequence point: <hidden>
    IL_004c:  ldloca.s   V_1
    IL_004e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0053:  brtrue.s   IL_0097
    IL_0055:  ldarg.0
    IL_0056:  ldc.i4.0
    IL_0057:  dup
    IL_0058:  stloc.0
    IL_0059:  stfld      ""int C.<M>d__0.<>1__state""
    // async: yield
    IL_005e:  ldarg.0
    IL_005f:  ldloc.1
    IL_0060:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0065:  ldarg.0
    IL_0066:  stloc.2
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_006d:  ldloca.s   V_1
    IL_006f:  ldloca.s   V_2
    IL_0071:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)""
    IL_0076:  leave      IL_0156
    // async: resume
    IL_007b:  ldarg.0
    IL_007c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0081:  stloc.1
    IL_0082:  ldarg.0
    IL_0083:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0088:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.m1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0097:  ldloca.s   V_1
    IL_0099:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    // sequence point: yield return 3;
    IL_009e:  ldarg.0
    IL_009f:  ldc.i4.3
    IL_00a0:  stfld      ""int C.<M>d__0.<>2__current""
    IL_00a5:  ldarg.0
    IL_00a6:  ldc.i4.s   -4
    IL_00a8:  dup
    IL_00a9:  stloc.0
    IL_00aa:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00af:  leave      IL_014a
    // sequence point: <hidden>
    IL_00b4:  ldarg.0
    IL_00b5:  ldc.i4.m1
    IL_00b6:  dup
    IL_00b7:  stloc.0
    IL_00b8:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00bd:  ldarg.0
    IL_00be:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_00c3:  pop
    // sequence point: <hidden>
    IL_00c4:  leave.s    IL_0109
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_00c6:  stloc.3
    IL_00c7:  ldarg.0
    IL_00c8:  ldc.i4.s   -2
    IL_00ca:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00cf:  ldarg.0
    IL_00d0:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
    IL_00d5:  brfalse.s  IL_00e9
    IL_00d7:  ldarg.0
    IL_00d8:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
    IL_00dd:  callvirt   ""void System.Threading.CancellationTokenSource.Dispose()""
    IL_00e2:  ldarg.0
    IL_00e3:  ldnull
    IL_00e4:  stfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
    IL_00e9:  ldarg.0
    IL_00ea:  ldc.i4.0
    IL_00eb:  stfld      ""int C.<M>d__0.<>2__current""
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_00f6:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_00fb:  ldarg.0
    IL_00fc:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_0101:  ldloc.3
    IL_0102:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_0107:  leave.s    IL_0156
  }
  // sequence point: }
  IL_0109:  ldarg.0
  IL_010a:  ldc.i4.s   -2
  IL_010c:  stfld      ""int C.<M>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0111:  ldarg.0
  IL_0112:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_0117:  brfalse.s  IL_012b
  IL_0119:  ldarg.0
  IL_011a:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_011f:  callvirt   ""void System.Threading.CancellationTokenSource.Dispose()""
  IL_0124:  ldarg.0
  IL_0125:  ldnull
  IL_0126:  stfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_012b:  ldarg.0
  IL_012c:  ldc.i4.0
  IL_012d:  stfld      ""int C.<M>d__0.<>2__current""
  IL_0132:  ldarg.0
  IL_0133:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_0138:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_013d:  ldarg.0
  IL_013e:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0143:  ldc.i4.0
  IL_0144:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0149:  ret
  IL_014a:  ldarg.0
  IL_014b:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0150:  ldc.i4.1
  IL_0151:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0156:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Enhanced);
        }

        [Theory]
        [InlineData("[EnumeratorCancellation] ", "")]
        [InlineData("", "[EnumeratorCancellation] ")]
        public void AsyncIteratorWithAwaitCompletedAndYield_WithEnumeratorCancellation_ExtendedPartialMethod(string definitionAttributes, string implementationAttributes)
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
partial class C
{
    public static partial System.Collections.Generic.IAsyncEnumerable<int> M(" + definitionAttributes + @"CancellationToken token);
    public static async partial System.Collections.Generic.IAsyncEnumerable<int> M(" + implementationAttributes + @"CancellationToken token)
    {
        _ = token;
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 3;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);

            var expectedFields = new[] {
                "FieldDefinition:Int32 <>1__state",
                "FieldDefinition:System.Runtime.CompilerServices.AsyncIteratorMethodBuilder <>t__builder",
                "FieldDefinition:System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1{Boolean} <>v__promiseOfValueOrEnd",
                "FieldDefinition:Int32 <>2__current",
                "FieldDefinition:Boolean <>w__disposeMode",
                "FieldDefinition:System.Threading.CancellationTokenSource <>x__combinedTokens",
                "FieldDefinition:Int32 <>l__initialThreadId",
                "FieldDefinition:System.Threading.CancellationToken token",
                "FieldDefinition:System.Threading.CancellationToken <>3__token",
                "FieldDefinition:System.Runtime.CompilerServices.TaskAwaiter <>u__1"
            };
            VerifyStateMachineFields(comp, "<M>d__0", expectedFields);

            // we generate initialization logic for the token parameter
            verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)", @"
{
  // Code size      176 (0xb0)
  .maxstack  3
  .locals init (C.<M>d__0 V_0,
                System.Threading.CancellationToken V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0035
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""int C.<M>d__0.<>l__initialThreadId""
  IL_0010:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_0015:  bne.un.s   IL_0035
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.s   -3
  IL_001a:  stfld      ""int C.<M>d__0.<>1__state""
  IL_001f:  ldarg.0
  IL_0020:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_0025:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_002a:  ldarg.0
  IL_002b:  ldc.i4.0
  IL_002c:  stfld      ""bool C.<M>d__0.<>w__disposeMode""
  IL_0031:  ldarg.0
  IL_0032:  stloc.0
  IL_0033:  br.s       IL_003d
  IL_0035:  ldc.i4.s   -3
  IL_0037:  newobj     ""C.<M>d__0..ctor(int)""
  IL_003c:  stloc.0
  IL_003d:  ldarg.0
  IL_003e:  ldflda     ""System.Threading.CancellationToken C.<M>d__0.<>3__token""
  IL_0043:  ldloca.s   V_1
  IL_0045:  initobj    ""System.Threading.CancellationToken""
  IL_004b:  ldloc.1
  IL_004c:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_0051:  brfalse.s  IL_005c
  IL_0053:  ldloc.0
  IL_0054:  ldarg.1
  IL_0055:  stfld      ""System.Threading.CancellationToken C.<M>d__0.token""
  IL_005a:  br.s       IL_00ae
  IL_005c:  ldarga.s   V_1
  IL_005e:  ldarg.0
  IL_005f:  ldfld      ""System.Threading.CancellationToken C.<M>d__0.<>3__token""
  IL_0064:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_0069:  brtrue.s   IL_007d
  IL_006b:  ldarga.s   V_1
  IL_006d:  ldloca.s   V_1
  IL_006f:  initobj    ""System.Threading.CancellationToken""
  IL_0075:  ldloc.1
  IL_0076:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_007b:  brfalse.s  IL_008b
  IL_007d:  ldloc.0
  IL_007e:  ldarg.0
  IL_007f:  ldfld      ""System.Threading.CancellationToken C.<M>d__0.<>3__token""
  IL_0084:  stfld      ""System.Threading.CancellationToken C.<M>d__0.token""
  IL_0089:  br.s       IL_00ae
  IL_008b:  ldarg.0
  IL_008c:  ldarg.0
  IL_008d:  ldfld      ""System.Threading.CancellationToken C.<M>d__0.<>3__token""
  IL_0092:  ldarg.1
  IL_0093:  call       ""System.Threading.CancellationTokenSource System.Threading.CancellationTokenSource.CreateLinkedTokenSource(System.Threading.CancellationToken, System.Threading.CancellationToken)""
  IL_0098:  stfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_009d:  ldloc.0
  IL_009e:  ldarg.0
  IL_009f:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_00a4:  callvirt   ""System.Threading.CancellationToken System.Threading.CancellationTokenSource.Token.get""
  IL_00a9:  stfld      ""System.Threading.CancellationToken C.<M>d__0.token""
  IL_00ae:  ldloc.0
  IL_00af:  ret
}");

            // we generate disposal logic for the combinedTokens field
            verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      343 (0x157)
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
    IL_0008:  ldc.i4.s   -4
    IL_000a:  sub
    IL_000b:  switch    (
        IL_00b4,
        IL_0024,
        IL_0024,
        IL_0024,
        IL_007b)
    IL_0024:  ldarg.0
    IL_0025:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_002a:  brfalse.s  IL_0031
    IL_002c:  leave      IL_0109
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.m1
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int C.<M>d__0.<>1__state""
    // sequence point: _ = token;
    IL_003a:  ldarg.0
    IL_003b:  ldfld      ""System.Threading.CancellationToken C.<M>d__0.token""
    IL_0040:  pop
    // sequence point: await System.Threading.Tasks.Task.CompletedTask;
    IL_0041:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get""
    IL_0046:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_004b:  stloc.1
    // sequence point: <hidden>
    IL_004c:  ldloca.s   V_1
    IL_004e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0053:  brtrue.s   IL_0097
    IL_0055:  ldarg.0
    IL_0056:  ldc.i4.0
    IL_0057:  dup
    IL_0058:  stloc.0
    IL_0059:  stfld      ""int C.<M>d__0.<>1__state""
    // async: yield
    IL_005e:  ldarg.0
    IL_005f:  ldloc.1
    IL_0060:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0065:  ldarg.0
    IL_0066:  stloc.2
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_006d:  ldloca.s   V_1
    IL_006f:  ldloca.s   V_2
    IL_0071:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)""
    IL_0076:  leave      IL_0156
    // async: resume
    IL_007b:  ldarg.0
    IL_007c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0081:  stloc.1
    IL_0082:  ldarg.0
    IL_0083:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0088:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.m1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0097:  ldloca.s   V_1
    IL_0099:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    // sequence point: yield return 3;
    IL_009e:  ldarg.0
    IL_009f:  ldc.i4.3
    IL_00a0:  stfld      ""int C.<M>d__0.<>2__current""
    IL_00a5:  ldarg.0
    IL_00a6:  ldc.i4.s   -4
    IL_00a8:  dup
    IL_00a9:  stloc.0
    IL_00aa:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00af:  leave      IL_014a
    // sequence point: <hidden>
    IL_00b4:  ldarg.0
    IL_00b5:  ldc.i4.m1
    IL_00b6:  dup
    IL_00b7:  stloc.0
    IL_00b8:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00bd:  ldarg.0
    IL_00be:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_00c3:  pop
    // sequence point: <hidden>
    IL_00c4:  leave.s    IL_0109
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_00c6:  stloc.3
    IL_00c7:  ldarg.0
    IL_00c8:  ldc.i4.s   -2
    IL_00ca:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00cf:  ldarg.0
    IL_00d0:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
    IL_00d5:  brfalse.s  IL_00e9
    IL_00d7:  ldarg.0
    IL_00d8:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
    IL_00dd:  callvirt   ""void System.Threading.CancellationTokenSource.Dispose()""
    IL_00e2:  ldarg.0
    IL_00e3:  ldnull
    IL_00e4:  stfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
    IL_00e9:  ldarg.0
    IL_00ea:  ldc.i4.0
    IL_00eb:  stfld      ""int C.<M>d__0.<>2__current""
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_00f6:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_00fb:  ldarg.0
    IL_00fc:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_0101:  ldloc.3
    IL_0102:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_0107:  leave.s    IL_0156
  }
  // sequence point: }
  IL_0109:  ldarg.0
  IL_010a:  ldc.i4.s   -2
  IL_010c:  stfld      ""int C.<M>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0111:  ldarg.0
  IL_0112:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_0117:  brfalse.s  IL_012b
  IL_0119:  ldarg.0
  IL_011a:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_011f:  callvirt   ""void System.Threading.CancellationTokenSource.Dispose()""
  IL_0124:  ldarg.0
  IL_0125:  ldnull
  IL_0126:  stfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_012b:  ldarg.0
  IL_012c:  ldc.i4.0
  IL_012d:  stfld      ""int C.<M>d__0.<>2__current""
  IL_0132:  ldarg.0
  IL_0133:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_0138:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_013d:  ldarg.0
  IL_013e:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0143:  ldc.i4.0
  IL_0144:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0149:  ret
  IL_014a:  ldarg.0
  IL_014b:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0150:  ldc.i4.1
  IL_0151:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0156:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Enhanced);
        }

        [Fact]
        public void AsyncIteratorWithAwaitCompletedAndYield_WithEnumeratorCancellation_LocalFunction()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
class C
{
    void M()
    {
#pragma warning disable 8321 // Unreferenced local function
        async System.Collections.Generic.IAsyncEnumerable<int> local([EnumeratorCancellation] CancellationToken token)
        {
            _ = token;
            await System.Threading.Tasks.Task.CompletedTask;
            yield return 3;
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, EnumeratorCancellationAttributeType, AsyncStreamsTypes }, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);

            var expectedFields = new[] {
                "FieldDefinition:Int32 <>1__state",
                "FieldDefinition:System.Runtime.CompilerServices.AsyncIteratorMethodBuilder <>t__builder",
                "FieldDefinition:System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1{Boolean} <>v__promiseOfValueOrEnd",
                "FieldDefinition:Int32 <>2__current",
                "FieldDefinition:Boolean <>w__disposeMode",
                "FieldDefinition:System.Threading.CancellationTokenSource <>x__combinedTokens",
                "FieldDefinition:Int32 <>l__initialThreadId",
                "FieldDefinition:System.Threading.CancellationToken token",
                "FieldDefinition:System.Threading.CancellationToken <>3__token",
                "FieldDefinition:System.Runtime.CompilerServices.TaskAwaiter <>u__1"
            };
            VerifyStateMachineFields(comp, "<<M>g__local|0_0>d", expectedFields);

            // we generate initialization logic for the token parameter
            verifier.VerifyIL("C.<<M>g__local|0_0>d.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)", @"
{
  // Code size      176 (0xb0)
  .maxstack  3
  .locals init (C.<<M>g__local|0_0>d V_0,
                System.Threading.CancellationToken V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<<M>g__local|0_0>d.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0035
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""int C.<<M>g__local|0_0>d.<>l__initialThreadId""
  IL_0010:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_0015:  bne.un.s   IL_0035
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.s   -3
  IL_001a:  stfld      ""int C.<<M>g__local|0_0>d.<>1__state""
  IL_001f:  ldarg.0
  IL_0020:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_0025:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<<M>g__local|0_0>d.<>t__builder""
  IL_002a:  ldarg.0
  IL_002b:  ldc.i4.0
  IL_002c:  stfld      ""bool C.<<M>g__local|0_0>d.<>w__disposeMode""
  IL_0031:  ldarg.0
  IL_0032:  stloc.0
  IL_0033:  br.s       IL_003d
  IL_0035:  ldc.i4.s   -3
  IL_0037:  newobj     ""C.<<M>g__local|0_0>d..ctor(int)""
  IL_003c:  stloc.0
  IL_003d:  ldarg.0
  IL_003e:  ldflda     ""System.Threading.CancellationToken C.<<M>g__local|0_0>d.<>3__token""
  IL_0043:  ldloca.s   V_1
  IL_0045:  initobj    ""System.Threading.CancellationToken""
  IL_004b:  ldloc.1
  IL_004c:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_0051:  brfalse.s  IL_005c
  IL_0053:  ldloc.0
  IL_0054:  ldarg.1
  IL_0055:  stfld      ""System.Threading.CancellationToken C.<<M>g__local|0_0>d.token""
  IL_005a:  br.s       IL_00ae
  IL_005c:  ldarga.s   V_1
  IL_005e:  ldarg.0
  IL_005f:  ldfld      ""System.Threading.CancellationToken C.<<M>g__local|0_0>d.<>3__token""
  IL_0064:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_0069:  brtrue.s   IL_007d
  IL_006b:  ldarga.s   V_1
  IL_006d:  ldloca.s   V_1
  IL_006f:  initobj    ""System.Threading.CancellationToken""
  IL_0075:  ldloc.1
  IL_0076:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_007b:  brfalse.s  IL_008b
  IL_007d:  ldloc.0
  IL_007e:  ldarg.0
  IL_007f:  ldfld      ""System.Threading.CancellationToken C.<<M>g__local|0_0>d.<>3__token""
  IL_0084:  stfld      ""System.Threading.CancellationToken C.<<M>g__local|0_0>d.token""
  IL_0089:  br.s       IL_00ae
  IL_008b:  ldarg.0
  IL_008c:  ldarg.0
  IL_008d:  ldfld      ""System.Threading.CancellationToken C.<<M>g__local|0_0>d.<>3__token""
  IL_0092:  ldarg.1
  IL_0093:  call       ""System.Threading.CancellationTokenSource System.Threading.CancellationTokenSource.CreateLinkedTokenSource(System.Threading.CancellationToken, System.Threading.CancellationToken)""
  IL_0098:  stfld      ""System.Threading.CancellationTokenSource C.<<M>g__local|0_0>d.<>x__combinedTokens""
  IL_009d:  ldloc.0
  IL_009e:  ldarg.0
  IL_009f:  ldfld      ""System.Threading.CancellationTokenSource C.<<M>g__local|0_0>d.<>x__combinedTokens""
  IL_00a4:  callvirt   ""System.Threading.CancellationToken System.Threading.CancellationTokenSource.Token.get""
  IL_00a9:  stfld      ""System.Threading.CancellationToken C.<<M>g__local|0_0>d.token""
  IL_00ae:  ldloc.0
  IL_00af:  ret
}");

            // we generate disposal logic for the combinedTokens field
            verifier.VerifyIL("C.<<M>g__local|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      343 (0x157)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<<M>g__local|0_0>d V_2,
                System.Exception V_3)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<<M>g__local|0_0>d.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  sub
    IL_000b:  switch    (
        IL_00b4,
        IL_0024,
        IL_0024,
        IL_0024,
        IL_007b)
    IL_0024:  ldarg.0
    IL_0025:  ldfld      ""bool C.<<M>g__local|0_0>d.<>w__disposeMode""
    IL_002a:  brfalse.s  IL_0031
    IL_002c:  leave      IL_0109
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.m1
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int C.<<M>g__local|0_0>d.<>1__state""
    // sequence point: _ = token;
    IL_003a:  ldarg.0
    IL_003b:  ldfld      ""System.Threading.CancellationToken C.<<M>g__local|0_0>d.token""
    IL_0040:  pop
    // sequence point: await System.Threading.Tasks.Task.CompletedTask;
    IL_0041:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get""
    IL_0046:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_004b:  stloc.1
    // sequence point: <hidden>
    IL_004c:  ldloca.s   V_1
    IL_004e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0053:  brtrue.s   IL_0097
    IL_0055:  ldarg.0
    IL_0056:  ldc.i4.0
    IL_0057:  dup
    IL_0058:  stloc.0
    IL_0059:  stfld      ""int C.<<M>g__local|0_0>d.<>1__state""
    // async: yield
    IL_005e:  ldarg.0
    IL_005f:  ldloc.1
    IL_0060:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<<M>g__local|0_0>d.<>u__1""
    IL_0065:  ldarg.0
    IL_0066:  stloc.2
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<<M>g__local|0_0>d.<>t__builder""
    IL_006d:  ldloca.s   V_1
    IL_006f:  ldloca.s   V_2
    IL_0071:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<<M>g__local|0_0>d>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<<M>g__local|0_0>d)""
    IL_0076:  leave      IL_0156
    // async: resume
    IL_007b:  ldarg.0
    IL_007c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<<M>g__local|0_0>d.<>u__1""
    IL_0081:  stloc.1
    IL_0082:  ldarg.0
    IL_0083:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<<M>g__local|0_0>d.<>u__1""
    IL_0088:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.m1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int C.<<M>g__local|0_0>d.<>1__state""
    IL_0097:  ldloca.s   V_1
    IL_0099:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    // sequence point: yield return 3;
    IL_009e:  ldarg.0
    IL_009f:  ldc.i4.3
    IL_00a0:  stfld      ""int C.<<M>g__local|0_0>d.<>2__current""
    IL_00a5:  ldarg.0
    IL_00a6:  ldc.i4.s   -4
    IL_00a8:  dup
    IL_00a9:  stloc.0
    IL_00aa:  stfld      ""int C.<<M>g__local|0_0>d.<>1__state""
    IL_00af:  leave      IL_014a
    // sequence point: <hidden>
    IL_00b4:  ldarg.0
    IL_00b5:  ldc.i4.m1
    IL_00b6:  dup
    IL_00b7:  stloc.0
    IL_00b8:  stfld      ""int C.<<M>g__local|0_0>d.<>1__state""
    IL_00bd:  ldarg.0
    IL_00be:  ldfld      ""bool C.<<M>g__local|0_0>d.<>w__disposeMode""
    IL_00c3:  pop
    // sequence point: <hidden>
    IL_00c4:  leave.s    IL_0109
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_00c6:  stloc.3
    IL_00c7:  ldarg.0
    IL_00c8:  ldc.i4.s   -2
    IL_00ca:  stfld      ""int C.<<M>g__local|0_0>d.<>1__state""
    IL_00cf:  ldarg.0
    IL_00d0:  ldfld      ""System.Threading.CancellationTokenSource C.<<M>g__local|0_0>d.<>x__combinedTokens""
    IL_00d5:  brfalse.s  IL_00e9
    IL_00d7:  ldarg.0
    IL_00d8:  ldfld      ""System.Threading.CancellationTokenSource C.<<M>g__local|0_0>d.<>x__combinedTokens""
    IL_00dd:  callvirt   ""void System.Threading.CancellationTokenSource.Dispose()""
    IL_00e2:  ldarg.0
    IL_00e3:  ldnull
    IL_00e4:  stfld      ""System.Threading.CancellationTokenSource C.<<M>g__local|0_0>d.<>x__combinedTokens""
    IL_00e9:  ldarg.0
    IL_00ea:  ldc.i4.0
    IL_00eb:  stfld      ""int C.<<M>g__local|0_0>d.<>2__current""
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<<M>g__local|0_0>d.<>t__builder""
    IL_00f6:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_00fb:  ldarg.0
    IL_00fc:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<<M>g__local|0_0>d.<>v__promiseOfValueOrEnd""
    IL_0101:  ldloc.3
    IL_0102:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_0107:  leave.s    IL_0156
  }
  // sequence point: }
  IL_0109:  ldarg.0
  IL_010a:  ldc.i4.s   -2
  IL_010c:  stfld      ""int C.<<M>g__local|0_0>d.<>1__state""
  // sequence point: <hidden>
  IL_0111:  ldarg.0
  IL_0112:  ldfld      ""System.Threading.CancellationTokenSource C.<<M>g__local|0_0>d.<>x__combinedTokens""
  IL_0117:  brfalse.s  IL_012b
  IL_0119:  ldarg.0
  IL_011a:  ldfld      ""System.Threading.CancellationTokenSource C.<<M>g__local|0_0>d.<>x__combinedTokens""
  IL_011f:  callvirt   ""void System.Threading.CancellationTokenSource.Dispose()""
  IL_0124:  ldarg.0
  IL_0125:  ldnull
  IL_0126:  stfld      ""System.Threading.CancellationTokenSource C.<<M>g__local|0_0>d.<>x__combinedTokens""
  IL_012b:  ldarg.0
  IL_012c:  ldc.i4.0
  IL_012d:  stfld      ""int C.<<M>g__local|0_0>d.<>2__current""
  IL_0132:  ldarg.0
  IL_0133:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<<M>g__local|0_0>d.<>t__builder""
  IL_0138:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_013d:  ldarg.0
  IL_013e:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<<M>g__local|0_0>d.<>v__promiseOfValueOrEnd""
  IL_0143:  ldc.i4.0
  IL_0144:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0149:  ret
  IL_014a:  ldarg.0
  IL_014b:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<<M>g__local|0_0>d.<>v__promiseOfValueOrEnd""
  IL_0150:  ldc.i4.1
  IL_0151:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0156:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Enhanced);
        }

        [Fact]
        public void AsyncIteratorWithAwaitCompletedAndYield_WithEnumeratorCancellation_NoUsage()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M([EnumeratorCancellation] CancellationToken token)
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 3;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);

            var expectedFields = new[] {
                "FieldDefinition:Int32 <>1__state",
                "FieldDefinition:System.Runtime.CompilerServices.AsyncIteratorMethodBuilder <>t__builder",
                "FieldDefinition:System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1{Boolean} <>v__promiseOfValueOrEnd",
                "FieldDefinition:Int32 <>2__current",
                "FieldDefinition:Boolean <>w__disposeMode",
                "FieldDefinition:System.Threading.CancellationTokenSource <>x__combinedTokens", // we generated the field
                "FieldDefinition:Int32 <>l__initialThreadId",
                "FieldDefinition:System.Runtime.CompilerServices.TaskAwaiter <>u__1"
            };
            VerifyStateMachineFields(comp, "<M>d__0", expectedFields);

            // we don't generate initialization logic
            verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)", @"
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (C.<M>d__0 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0035
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""int C.<M>d__0.<>l__initialThreadId""
  IL_0010:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_0015:  bne.un.s   IL_0035
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.s   -3
  IL_001a:  stfld      ""int C.<M>d__0.<>1__state""
  IL_001f:  ldarg.0
  IL_0020:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_0025:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_002a:  ldarg.0
  IL_002b:  ldc.i4.0
  IL_002c:  stfld      ""bool C.<M>d__0.<>w__disposeMode""
  IL_0031:  ldarg.0
  IL_0032:  stloc.0
  IL_0033:  br.s       IL_003d
  IL_0035:  ldc.i4.s   -3
  IL_0037:  newobj     ""C.<M>d__0..ctor(int)""
  IL_003c:  stloc.0
  IL_003d:  ldloc.0
  IL_003e:  ret
}");

            // we generate disposal logic for the combinedTokens field
            verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      336 (0x150)
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
    IL_0008:  ldc.i4.s   -4
    IL_000a:  sub
    IL_000b:  switch    (
        IL_00ad,
        IL_0024,
        IL_0024,
        IL_0024,
        IL_0074)
    IL_0024:  ldarg.0
    IL_0025:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_002a:  brfalse.s  IL_0031
    IL_002c:  leave      IL_0102
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.m1
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int C.<M>d__0.<>1__state""
    // sequence point: await System.Threading.Tasks.Task.CompletedTask;
    IL_003a:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get""
    IL_003f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0044:  stloc.1
    // sequence point: <hidden>
    IL_0045:  ldloca.s   V_1
    IL_0047:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_004c:  brtrue.s   IL_0090
    IL_004e:  ldarg.0
    IL_004f:  ldc.i4.0
    IL_0050:  dup
    IL_0051:  stloc.0
    IL_0052:  stfld      ""int C.<M>d__0.<>1__state""
    // async: yield
    IL_0057:  ldarg.0
    IL_0058:  ldloc.1
    IL_0059:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_005e:  ldarg.0
    IL_005f:  stloc.2
    IL_0060:  ldarg.0
    IL_0061:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_0066:  ldloca.s   V_1
    IL_0068:  ldloca.s   V_2
    IL_006a:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)""
    IL_006f:  leave      IL_014f
    // async: resume
    IL_0074:  ldarg.0
    IL_0075:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_007a:  stloc.1
    IL_007b:  ldarg.0
    IL_007c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1""
    IL_0081:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0087:  ldarg.0
    IL_0088:  ldc.i4.m1
    IL_0089:  dup
    IL_008a:  stloc.0
    IL_008b:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0090:  ldloca.s   V_1
    IL_0092:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    // sequence point: yield return 3;
    IL_0097:  ldarg.0
    IL_0098:  ldc.i4.3
    IL_0099:  stfld      ""int C.<M>d__0.<>2__current""
    IL_009e:  ldarg.0
    IL_009f:  ldc.i4.s   -4
    IL_00a1:  dup
    IL_00a2:  stloc.0
    IL_00a3:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00a8:  leave      IL_0143
    // sequence point: <hidden>
    IL_00ad:  ldarg.0
    IL_00ae:  ldc.i4.m1
    IL_00af:  dup
    IL_00b0:  stloc.0
    IL_00b1:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00b6:  ldarg.0
    IL_00b7:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
    IL_00bc:  pop
    // sequence point: <hidden>
    IL_00bd:  leave.s    IL_0102
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_00bf:  stloc.3
    IL_00c0:  ldarg.0
    IL_00c1:  ldc.i4.s   -2
    IL_00c3:  stfld      ""int C.<M>d__0.<>1__state""
    IL_00c8:  ldarg.0
    IL_00c9:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
    IL_00ce:  brfalse.s  IL_00e2
    IL_00d0:  ldarg.0
    IL_00d1:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
    IL_00d6:  callvirt   ""void System.Threading.CancellationTokenSource.Dispose()""
    IL_00db:  ldarg.0
    IL_00dc:  ldnull
    IL_00dd:  stfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
    IL_00e2:  ldarg.0
    IL_00e3:  ldc.i4.0
    IL_00e4:  stfld      ""int C.<M>d__0.<>2__current""
    IL_00e9:  ldarg.0
    IL_00ea:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_00ef:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_00f4:  ldarg.0
    IL_00f5:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_00fa:  ldloc.3
    IL_00fb:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_0100:  leave.s    IL_014f
  }
  // sequence point: }
  IL_0102:  ldarg.0
  IL_0103:  ldc.i4.s   -2
  IL_0105:  stfld      ""int C.<M>d__0.<>1__state""
  // sequence point: <hidden>
  IL_010a:  ldarg.0
  IL_010b:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_0110:  brfalse.s  IL_0124
  IL_0112:  ldarg.0
  IL_0113:  ldfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_0118:  callvirt   ""void System.Threading.CancellationTokenSource.Dispose()""
  IL_011d:  ldarg.0
  IL_011e:  ldnull
  IL_011f:  stfld      ""System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens""
  IL_0124:  ldarg.0
  IL_0125:  ldc.i4.0
  IL_0126:  stfld      ""int C.<M>d__0.<>2__current""
  IL_012b:  ldarg.0
  IL_012c:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_0131:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_0136:  ldarg.0
  IL_0137:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_013c:  ldc.i4.0
  IL_013d:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0142:  ret
  IL_0143:  ldarg.0
  IL_0144:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
  IL_0149:  ldc.i4.1
  IL_014a:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_014f:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Enhanced);
        }

        private static void VerifyStateMachineFields(CSharpCompilation comp, string methodName, string[] expectedFields)
        {
            var peReader = new PEReader(comp.EmitToArray());
            var metadataReader = peReader.GetMetadataReader();
            var types = metadataReader.TypeDefinitions.Select(t => metadataReader.GetString(metadataReader.GetTypeDefinition(t).Name));
            var type = metadataReader.TypeDefinitions.Single(t => metadataReader.GetString(metadataReader.GetTypeDefinition(t).Name) == methodName);
            var fields = metadataReader.GetTypeDefinition(type).GetFields().Select(f => metadataReader.Dump(f));
            AssertEx.SetEqual(expectedFields, fields);
        }

        [Fact]
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
            var expectedOutput = "0 1 2 3 4 5";

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
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
            var expectedOutput = "0 1 2 3 4 5";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
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
        await System.Threading.Tasks.Task.Yield();
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
            var expectedOutput = "Start p:10 p:11 Value p:12 End";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).
                VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
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
        await System.Threading.Tasks.Task.Yield();
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
            var expectedOutput = "Start f:10 f:11 Value f:12 End";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
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

        [Fact]
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
            var expectedOutput = "0 1 2 3 4 Done";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
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
            var expectedOutput = "0 1 2 3 4 5 Done";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
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
        await System.Threading.Tasks.Task.Yield();
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
            var expectedOutput = "0 1 2 3 Done";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
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
            var expectedOutput = "0 1 2 3 Done";

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
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
                CompileAndVerify(comp, expectedOutput: expectation)
                    .VerifyDiagnostics();

                comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectation), verify: Verification.Skipped)
                    .VerifyDiagnostics();
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
                CompileAndVerify(comp, expectedOutput: expectation)
                    .VerifyDiagnostics();

                comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectation), verify: Verification.Skipped)
                    .VerifyDiagnostics();
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
                            //await System.Threading.Tasks.Task.Yield();
                            builder.AppendLine("await System.Threading.Tasks.Task.Yield();");
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

        [Fact]
        public void AsyncIteratorWithAwaitAndYieldAndAwait()
        {
            string source = @"
using static System.Console;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write(""1 "");
        await System.Threading.Tasks.Task.Yield();
        Write(""2 "");
        yield return 3;
        Write(""4 "");
        await System.Threading.Tasks.Task.Yield();
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

            var expectedOutput = "0 1 2 3 4 Done";

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
        [InlineData(1, "0 DISPOSAL DONE")]
        [InlineData(2, "0 1 DISPOSAL Finally DONE")]
        [InlineData(3, "0 1 Finally 2 DISPOSAL DONE")]
        [InlineData(4, "0 1 Finally 2 3 DISPOSAL Finally DONE")]
        [InlineData(5, "0 1 Finally 2 3 Finally END DISPOSAL DONE")]
        public void TryFinally_Goto(int iterations, string expectedOutput)
        {
            string source = @"
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        int counter = 0;
        start:
        yield return counter++;
        await System.Threading.Tasks.Task.Yield();
        try
        {
            yield return counter++;
            if (counter <= 2) goto start;
        }
        finally
        {
            Write(""Finally "");
            await System.Threading.Tasks.Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
    public async System.Collections.Generic.IAsyncEnumerator<int> GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        yield return 1;
        await System.Threading.Tasks.Task.Yield();
        try
        {
            try
            {
                await System.Threading.Tasks.Task.Yield();
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
            await System.Threading.Tasks.Task.Yield();
            yield break;
        }
        finally
        {
            Write(""Finally "");
            await System.Threading.Tasks.Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/79124")]
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
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
                {
                  // Code size      412 (0x19c)
                  .maxstack  3
                  .locals init (int V_0,
                                System.Runtime.CompilerServices.TaskAwaiter V_1,
                                C.<M>d__0 V_2,
                                System.Exception V_3)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
                  IL_0006:  stloc.0
                  .try
                  {
                    IL_0007:  ldloc.0
                    IL_0008:  ldc.i4.s   -4
                    IL_000a:  sub
                    IL_000b:  switch    (
                        IL_0026,
                        IL_0028,
                        IL_002c,
                        IL_002c,
                        IL_002a)
                    IL_0024:  br.s       IL_002c
                    IL_0026:  br.s       IL_0059
                    IL_0028:  br.s       IL_002c
                    IL_002a:  br.s       IL_00aa
                    IL_002c:  ldarg.0
                    IL_002d:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
                    IL_0032:  brfalse.s  IL_0039
                    IL_0034:  leave      IL_0165
                    IL_0039:  ldarg.0
                    IL_003a:  ldc.i4.m1
                    IL_003b:  dup
                    IL_003c:  stloc.0
                    IL_003d:  stfld      "int C.<M>d__0.<>1__state"
                    IL_0042:  nop
                    IL_0043:  ldarg.0
                    IL_0044:  ldc.i4.1
                    IL_0045:  stfld      "int C.<M>d__0.<>2__current"
                    IL_004a:  ldarg.0
                    IL_004b:  ldc.i4.s   -4
                    IL_004d:  dup
                    IL_004e:  stloc.0
                    IL_004f:  stfld      "int C.<M>d__0.<>1__state"
                    IL_0054:  leave      IL_018e
                    IL_0059:  ldarg.0
                    IL_005a:  ldc.i4.m1
                    IL_005b:  dup
                    IL_005c:  stloc.0
                    IL_005d:  stfld      "int C.<M>d__0.<>1__state"
                    IL_0062:  ldarg.0
                    IL_0063:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
                    IL_0068:  brfalse.s  IL_006f
                    IL_006a:  leave      IL_0165
                    IL_006f:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get"
                    IL_0074:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
                    IL_0079:  stloc.1
                    IL_007a:  ldloca.s   V_1
                    IL_007c:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
                    IL_0081:  brtrue.s   IL_00c6
                    IL_0083:  ldarg.0
                    IL_0084:  ldc.i4.0
                    IL_0085:  dup
                    IL_0086:  stloc.0
                    IL_0087:  stfld      "int C.<M>d__0.<>1__state"
                    IL_008c:  ldarg.0
                    IL_008d:  ldloc.1
                    IL_008e:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
                    IL_0093:  ldarg.0
                    IL_0094:  stloc.2
                    IL_0095:  ldarg.0
                    IL_0096:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
                    IL_009b:  ldloca.s   V_1
                    IL_009d:  ldloca.s   V_2
                    IL_009f:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)"
                    IL_00a4:  nop
                    IL_00a5:  leave      IL_019b
                    IL_00aa:  ldarg.0
                    IL_00ab:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
                    IL_00b0:  stloc.1
                    IL_00b1:  ldarg.0
                    IL_00b2:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
                    IL_00b7:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
                    IL_00bd:  ldarg.0
                    IL_00be:  ldc.i4.m1
                    IL_00bf:  dup
                    IL_00c0:  stloc.0
                    IL_00c1:  stfld      "int C.<M>d__0.<>1__state"
                    IL_00c6:  ldloca.s   V_1
                    IL_00c8:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
                    IL_00cd:  nop
                    .try
                    {
                      .try
                      {
                        IL_00ce:  nop
                        .try
                        {
                          IL_00cf:  nop
                          IL_00d0:  ldstr      "Break "
                          IL_00d5:  call       "void System.Console.Write(string)"
                          IL_00da:  nop
                          IL_00db:  ldarg.0
                          IL_00dc:  ldc.i4.1
                          IL_00dd:  stfld      "bool C.<M>d__0.<>w__disposeMode"
                          IL_00e2:  leave.s    IL_00f7
                        }
                        finally
                        {
                          IL_00e4:  ldloc.0
                          IL_00e5:  ldc.i4.m1
                          IL_00e6:  bne.un.s   IL_00f6
                          IL_00e8:  nop
                          IL_00e9:  ldstr      "Throw "
                          IL_00ee:  call       "void System.Console.Write(string)"
                          IL_00f3:  nop
                          IL_00f4:  ldnull
                          IL_00f5:  throw
                          IL_00f6:  endfinally
                        }
                        IL_00f7:  ldarg.0
                        IL_00f8:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
                        IL_00fd:  brfalse.s  IL_0101
                        IL_00ff:  leave.s    IL_012e
                        IL_0101:  nop
                        IL_0102:  leave.s    IL_011a
                      }
                      catch object
                      {
                        IL_0104:  pop
                        IL_0105:  nop
                        IL_0106:  ldstr      "Caught "
                        IL_010b:  call       "void System.Console.Write(string)"
                        IL_0110:  nop
                        IL_0111:  ldarg.0
                        IL_0112:  ldc.i4.1
                        IL_0113:  stfld      "bool C.<M>d__0.<>w__disposeMode"
                        IL_0118:  leave.s    IL_012e
                      }
                      IL_011a:  leave.s    IL_012e
                    }
                    finally
                    {
                      IL_011c:  ldloc.0
                      IL_011d:  ldc.i4.m1
                      IL_011e:  bne.un.s   IL_012d
                      IL_0120:  nop
                      IL_0121:  ldstr      "Finally "
                      IL_0126:  call       "void System.Console.Write(string)"
                      IL_012b:  nop
                      IL_012c:  nop
                      IL_012d:  endfinally
                    }
                    IL_012e:  ldarg.0
                    IL_012f:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
                    IL_0134:  brfalse.s  IL_0138
                    IL_0136:  leave.s    IL_0165
                    IL_0138:  leave.s    IL_0165
                  }
                  catch System.Exception
                  {
                    IL_013a:  stloc.3
                    IL_013b:  ldarg.0
                    IL_013c:  ldc.i4.s   -2
                    IL_013e:  stfld      "int C.<M>d__0.<>1__state"
                    IL_0143:  ldarg.0
                    IL_0144:  ldc.i4.0
                    IL_0145:  stfld      "int C.<M>d__0.<>2__current"
                    IL_014a:  ldarg.0
                    IL_014b:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
                    IL_0150:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
                    IL_0155:  nop
                    IL_0156:  ldarg.0
                    IL_0157:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
                    IL_015c:  ldloc.3
                    IL_015d:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
                    IL_0162:  nop
                    IL_0163:  leave.s    IL_019b
                  }
                  IL_0165:  ldarg.0
                  IL_0166:  ldc.i4.s   -2
                  IL_0168:  stfld      "int C.<M>d__0.<>1__state"
                  IL_016d:  ldarg.0
                  IL_016e:  ldc.i4.0
                  IL_016f:  stfld      "int C.<M>d__0.<>2__current"
                  IL_0174:  ldarg.0
                  IL_0175:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
                  IL_017a:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
                  IL_017f:  nop
                  IL_0180:  ldarg.0
                  IL_0181:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
                  IL_0186:  ldc.i4.0
                  IL_0187:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
                  IL_018c:  nop
                  IL_018d:  ret
                  IL_018e:  ldarg.0
                  IL_018f:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
                  IL_0194:  ldc.i4.1
                  IL_0195:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
                  IL_019a:  nop
                  IL_019b:  ret
                }
                """);

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();

            verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()", """
{
  // Code size      250 (0xfa)
  .maxstack  3
  .locals init (int V_0,
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  beq.s      IL_0015
    IL_000c:  br.s       IL_000e
    IL_000e:  ldloc.0
    IL_000f:  ldc.i4.s   -3
    IL_0011:  beq.s      IL_0017
    IL_0013:  br.s       IL_0019
    IL_0015:  br.s       IL_0048
    IL_0017:  br.s       IL_0019
    IL_0019:  ldarg.0
    IL_001a:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_001f:  brfalse.s  IL_0026
    IL_0021:  leave      IL_00e7
    IL_0026:  ldarg.0
    IL_0027:  ldc.i4.m1
    IL_0028:  dup
    IL_0029:  stloc.0
    IL_002a:  stfld      "int C.<M>d__0.<>1__state"
    IL_002f:  nop
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.1
    IL_0032:  stfld      "int C.<M>d__0.<>2__current"
    IL_0037:  ldarg.0
    IL_0038:  ldc.i4.s   -4
    IL_003a:  dup
    IL_003b:  stloc.0
    IL_003c:  stfld      "int C.<M>d__0.<>1__state"
    IL_0041:  ldc.i4.1
    IL_0042:  stloc.1
    IL_0043:  leave      IL_00f8
    IL_0048:  ldarg.0
    IL_0049:  ldc.i4.m1
    IL_004a:  dup
    IL_004b:  stloc.0
    IL_004c:  stfld      "int C.<M>d__0.<>1__state"
    IL_0051:  ldarg.0
    IL_0052:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_0057:  brfalse.s  IL_005e
    IL_0059:  leave      IL_00e7
    IL_005e:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get"
    IL_0063:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
    IL_0068:  nop
    .try
    {
      .try
      {
        IL_0069:  nop
        .try
        {
          IL_006a:  nop
          IL_006b:  ldstr      "Break "
          IL_0070:  call       "void System.Console.Write(string)"
          IL_0075:  nop
          IL_0076:  ldarg.0
          IL_0077:  ldc.i4.1
          IL_0078:  stfld      "bool C.<M>d__0.<>w__disposeMode"
          IL_007d:  leave.s    IL_0092
        }
        finally
        {
          IL_007f:  ldloc.0
          IL_0080:  ldc.i4.m1
          IL_0081:  bne.un.s   IL_0091
          IL_0083:  nop
          IL_0084:  ldstr      "Throw "
          IL_0089:  call       "void System.Console.Write(string)"
          IL_008e:  nop
          IL_008f:  ldnull
          IL_0090:  throw
          IL_0091:  endfinally
        }
        IL_0092:  ldarg.0
        IL_0093:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
        IL_0098:  brfalse.s  IL_009c
        IL_009a:  leave.s    IL_00c9
        IL_009c:  nop
        IL_009d:  leave.s    IL_00b5
      }
      catch object
      {
        IL_009f:  pop
        IL_00a0:  nop
        IL_00a1:  ldstr      "Caught "
        IL_00a6:  call       "void System.Console.Write(string)"
        IL_00ab:  nop
        IL_00ac:  ldarg.0
        IL_00ad:  ldc.i4.1
        IL_00ae:  stfld      "bool C.<M>d__0.<>w__disposeMode"
        IL_00b3:  leave.s    IL_00c9
      }
      IL_00b5:  leave.s    IL_00c9
    }
    finally
    {
      IL_00b7:  ldloc.0
      IL_00b8:  ldc.i4.m1
      IL_00b9:  bne.un.s   IL_00c8
      IL_00bb:  nop
      IL_00bc:  ldstr      "Finally "
      IL_00c1:  call       "void System.Console.Write(string)"
      IL_00c6:  nop
      IL_00c7:  nop
      IL_00c8:  endfinally
    }
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_00cf:  brfalse.s  IL_00d3
    IL_00d1:  leave.s    IL_00e7
    IL_00d3:  leave.s    IL_00e7
  }
  catch System.Exception
  {
    IL_00d5:  pop
    IL_00d6:  ldarg.0
    IL_00d7:  ldc.i4.s   -2
    IL_00d9:  stfld      "int C.<M>d__0.<>1__state"
    IL_00de:  ldarg.0
    IL_00df:  ldc.i4.0
    IL_00e0:  stfld      "int C.<M>d__0.<>2__current"
    IL_00e5:  rethrow
  }
  IL_00e7:  ldarg.0
  IL_00e8:  ldc.i4.s   -2
  IL_00ea:  stfld      "int C.<M>d__0.<>1__state"
  IL_00ef:  ldarg.0
  IL_00f0:  ldc.i4.0
  IL_00f1:  stfld      "int C.<M>d__0.<>2__current"
  IL_00f6:  ldc.i4.0
  IL_00f7:  stloc.1
  IL_00f8:  ldloc.1
  IL_00f9:  ret
}
""");
        }

        [Theory]
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
        await System.Threading.Tasks.Task.Yield();
        yield return 2;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
                await System.Threading.Tasks.Task.Yield();
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
                await System.Threading.Tasks.Task.Yield();
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
                await System.Threading.Tasks.Task.Yield();
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
        await System.Threading.Tasks.Task.Yield();
        yield return 2;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
                await System.Threading.Tasks.Task.Yield();
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
                await System.Threading.Tasks.Task.Yield();
                Write(""Throw "");
                bool b = true;
                if (b) throw null;
                Write(""SKIPPED"");
            }
            catch
            {
                Write(""Caught "");
                await System.Threading.Tasks.Task.Yield();
                yield break;
            }
            yield return 42;
            Write(""SKIPPED"");
        }
        finally
        {
            await System.Threading.Tasks.Task.Yield();
            Write(""Finally "");
        }
        yield return 42;
        Write(""SKIPPED"");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
                await System.Threading.Tasks.Task.Yield();
                Write(""Throw "");
                bool b = true;
                if (b) throw null;
            }
            catch
            {
                Write(""Caught "");
                try
                {
                    await System.Threading.Tasks.Task.Yield();
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
            await System.Threading.Tasks.Task.Yield();
            Write(""Finally2 "");
        }
        Write(""SKIPPED"");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            await System.Threading.Tasks.Task.Yield();
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
        [InlineData(0, "DISPOSAL DONE")]
        [InlineData(1, "1 DISPOSAL Finally1 Finally2 Finally5 Finally6 DONE")]
        [InlineData(2, "1 2 DISPOSAL Finally1 Finally2 Finally5 Finally6 DONE")]
        [InlineData(3, "1 2 Finally1 Finally2 3 DISPOSAL Finally5 Finally6 DONE")]
        [InlineData(4, "1 2 Finally1 Finally2 3 4 DISPOSAL Finally3 Finally4 Finally5 Finally6 DONE")]
        [InlineData(5, "1 2 Finally1 Finally2 3 4 5 DISPOSAL Finally3 Finally4 Finally5 Finally6 DONE")]
        [InlineData(6, "1 2 Finally1 Finally2 3 4 5 Finally3 Finally4 6 DISPOSAL Finally5 Finally6 DONE")]
        [InlineData(7, "1 2 Finally1 Finally2 3 4 5 Finally3 Finally4 6 Finally5 Finally6 7 DISPOSAL DONE")]
        [InlineData(8, "1 2 Finally1 Finally2 3 4 5 Finally3 Finally4 6 Finally5 Finally6 7 END DISPOSAL DONE")]
        public void TryFinally_MultipleSameLevelTrys(int iterations, string expectedOutput)
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
                await System.Threading.Tasks.Task.Yield();
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
                await System.Threading.Tasks.Task.Yield();
                Write(""Finally4 "");
            }
            yield return 6;
        }
        finally
        {
            Write(""Finally5 "");
            await System.Threading.Tasks.Task.Yield();
            Write(""Finally6 "");
        }

        yield return 7;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
        await System.Threading.Tasks.Task.Yield();
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
        await System.Threading.Tasks.Task.Yield();
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            await System.Threading.Tasks.Task.Yield();
        }
        finally
        {
            await System.Threading.Tasks.Task.Yield();
            Write(""Finally "");
            await System.Threading.Tasks.Task.CompletedTask;
        }
        yield return 2;
    }
}
";

            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            await System.Threading.Tasks.Task.Yield();
            Write(""Throw "");
            bool b = true;
            if (b) throw null;
            await System.Threading.Tasks.Task.Yield();
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            await System.Threading.Tasks.Task.Yield();
            await SlowThrowAsync();
            Write(""SKIPPED"");
            await System.Threading.Tasks.Task.Yield();
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
        await System.Threading.Tasks.Task.Yield();
        Write(""Throw2 "");
        throw null;
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            await System.Threading.Tasks.Task.Yield();
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            await System.Threading.Tasks.Task.Yield();
            ThrowIf(2);

            try
            {
                Write(""2 "");
                ThrowIf(3);
                await System.Threading.Tasks.Task.Yield();
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
                await System.Threading.Tasks.Task.Yield();
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            await System.Threading.Tasks.Task.Yield();
            Write(""Caught2 "");
        }
        Write(""After "");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
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
        public void TryFinally_NoYieldBreakInFinally_Nested2()
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
            catch
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

        [Fact]
        public void TryFinally_NoYieldInFinally_NestedTryCatch()
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
                yield return 1;
            }
            catch { }
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (14,17): error CS1625: Cannot yield in the body of a finally clause
                //                 yield return 1;
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield").WithLocation(14, 17)
                );
        }

        [Theory]
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
                await System.Threading.Tasks.Task.Yield();
            }
            finally
            {
                Write($""Finally{counter++} "");
                await System.Threading.Tasks.Task.Yield();
            }

            Write($""Again "");
        }

        Write($""SKIPPED"");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            await System.Threading.Tasks.Task.Yield();
        }
        finally
        {
            Write($""Finally "");
            await System.Threading.Tasks.Task.Yield();
            bool b = true;
            if (b) throw null;
        }
        Write($""SKIPPED "");
    }
}
";

            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
                await System.Threading.Tasks.Task.Yield();
            }
            finally
            {
                Write($""Finally1 "");
                await System.Threading.Tasks.Task.Yield();
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
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            await System.Threading.Tasks.Task.Yield();
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
            await System.Threading.Tasks.Task.Yield();
            Write(""Finally2 "");
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            await System.Threading.Tasks.Task.Yield();
            Write(""Throw "");
            bool b = true;
            if (b) throw new System.Exception();
        }
        finally
        {
            Write(""Finally1 "");
            await System.Threading.Tasks.Task.Yield();
            Write(""Finally2 "");
        }
        Write(""SKIPPED"");
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
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
            await System.Threading.Tasks.Task.Yield();
            Write(""Try2 "");
        }
        finally
        {
            Write(""Finally1 "");
            await System.Threading.Tasks.Task.Yield();
            Write(""Finally2 "");
        }
    }
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations), source }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation([Run(iterations), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void TryFinally_01()
        {
            // await in a catch/finally (so the catch and finally are extracted/rewritten), execution path through catch
            string source = """
using static System.Console;
public class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;

        try
        {
            Write("Throw ");
            throw new System.Exception("thrown");
        }
        catch
        {
            await System.Threading.Tasks.Task.CompletedTask;
            Write("Caught ");
            yield break;
        }
        finally
        {
            await System.Threading.Tasks.Task.CompletedTask;
            Write("Finally ");
        }
    }
}
""";
            var expectedOutput = "1 Throw Caught Finally END DISPOSAL DONE";
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(2), source }, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      647 (0x287)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                C.<M>d__0 V_4,
                object V_5,
                System.Runtime.CompilerServices.TaskAwaiter V_6,
                System.Exception V_7)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  sub
    IL_000b:  switch    (
        IL_002a,
        IL_002c,
        IL_0035,
        IL_0035,
        IL_002e,
        IL_0030)
    IL_0028:  br.s       IL_0035
    IL_002a:  br.s       IL_0062
    IL_002c:  br.s       IL_0035
    IL_002e:  br.s       IL_0086
    IL_0030:  br         IL_0193
    IL_0035:  ldarg.0
    IL_0036:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_003b:  brfalse.s  IL_0042
    IL_003d:  leave      IL_0242
    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.m1
    IL_0044:  dup
    IL_0045:  stloc.0
    IL_0046:  stfld      "int C.<M>d__0.<>1__state"
    IL_004b:  nop
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.1
    IL_004e:  stfld      "int C.<M>d__0.<>2__current"
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.s   -4
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      "int C.<M>d__0.<>1__state"
    IL_005d:  leave      IL_0279
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      "int C.<M>d__0.<>1__state"
    IL_006b:  ldarg.0
    IL_006c:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_0071:  brfalse.s  IL_0078
    IL_0073:  leave      IL_0242
    IL_0078:  ldarg.0
    IL_0079:  ldnull
    IL_007a:  stfld      "object C.<M>d__0.<>s__1"
    IL_007f:  ldarg.0
    IL_0080:  ldc.i4.0
    IL_0081:  stfld      "int C.<M>d__0.<>s__2"
    IL_0086:  nop
    .try
    {
      IL_0087:  ldloc.0
      IL_0088:  brfalse.s  IL_008c
      IL_008a:  br.s       IL_008e
      IL_008c:  br.s       IL_0107
      IL_008e:  ldarg.0
      IL_008f:  ldc.i4.0
      IL_0090:  stfld      "int C.<M>d__0.<>s__4"
      .try
      {
        IL_0095:  nop
        IL_0096:  ldstr      "Throw "
        IL_009b:  call       "void System.Console.Write(string)"
        IL_00a0:  nop
        IL_00a1:  ldstr      "thrown"
        IL_00a6:  newobj     "System.Exception..ctor(string)"
        IL_00ab:  throw
      }
      catch object
      {
        IL_00ac:  stloc.1
        IL_00ad:  ldarg.0
        IL_00ae:  ldloc.1
        IL_00af:  stfld      "object C.<M>d__0.<>s__3"
        IL_00b4:  ldarg.0
        IL_00b5:  ldc.i4.1
        IL_00b6:  stfld      "int C.<M>d__0.<>s__4"
        IL_00bb:  leave.s    IL_00bd
      }
      IL_00bd:  ldarg.0
      IL_00be:  ldfld      "int C.<M>d__0.<>s__4"
      IL_00c3:  stloc.2
      IL_00c4:  ldloc.2
      IL_00c5:  ldc.i4.1
      IL_00c6:  beq.s      IL_00ca
      IL_00c8:  br.s       IL_013f
      IL_00ca:  nop
      IL_00cb:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get"
      IL_00d0:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
      IL_00d5:  stloc.3
      IL_00d6:  ldloca.s   V_3
      IL_00d8:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
      IL_00dd:  brtrue.s   IL_0123
      IL_00df:  ldarg.0
      IL_00e0:  ldc.i4.0
      IL_00e1:  dup
      IL_00e2:  stloc.0
      IL_00e3:  stfld      "int C.<M>d__0.<>1__state"
      IL_00e8:  ldarg.0
      IL_00e9:  ldloc.3
      IL_00ea:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
      IL_00ef:  ldarg.0
      IL_00f0:  stloc.s    V_4
      IL_00f2:  ldarg.0
      IL_00f3:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
      IL_00f8:  ldloca.s   V_3
      IL_00fa:  ldloca.s   V_4
      IL_00fc:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)"
      IL_0101:  nop
      IL_0102:  leave      IL_0286
      IL_0107:  ldarg.0
      IL_0108:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
      IL_010d:  stloc.3
      IL_010e:  ldarg.0
      IL_010f:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
      IL_0114:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
      IL_011a:  ldarg.0
      IL_011b:  ldc.i4.m1
      IL_011c:  dup
      IL_011d:  stloc.0
      IL_011e:  stfld      "int C.<M>d__0.<>1__state"
      IL_0123:  ldloca.s   V_3
      IL_0125:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
      IL_012a:  nop
      IL_012b:  ldstr      "Caught "
      IL_0130:  call       "void System.Console.Write(string)"
      IL_0135:  nop
      IL_0136:  ldarg.0
      IL_0137:  ldc.i4.1
      IL_0138:  stfld      "bool C.<M>d__0.<>w__disposeMode"
      IL_013d:  leave.s    IL_0154
      IL_013f:  ldarg.0
      IL_0140:  ldnull
      IL_0141:  stfld      "object C.<M>d__0.<>s__3"
      IL_0146:  leave.s    IL_0154
    }
    catch object
    {
      IL_0148:  stloc.s    V_5
      IL_014a:  ldarg.0
      IL_014b:  ldloc.s    V_5
      IL_014d:  stfld      "object C.<M>d__0.<>s__1"
      IL_0152:  leave.s    IL_0154
    }
    IL_0154:  nop
    IL_0155:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get"
    IL_015a:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
    IL_015f:  stloc.s    V_6
    IL_0161:  ldloca.s   V_6
    IL_0163:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
    IL_0168:  brtrue.s   IL_01b0
    IL_016a:  ldarg.0
    IL_016b:  ldc.i4.1
    IL_016c:  dup
    IL_016d:  stloc.0
    IL_016e:  stfld      "int C.<M>d__0.<>1__state"
    IL_0173:  ldarg.0
    IL_0174:  ldloc.s    V_6
    IL_0176:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
    IL_017b:  ldarg.0
    IL_017c:  stloc.s    V_4
    IL_017e:  ldarg.0
    IL_017f:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
    IL_0184:  ldloca.s   V_6
    IL_0186:  ldloca.s   V_4
    IL_0188:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)"
    IL_018d:  nop
    IL_018e:  leave      IL_0286
    IL_0193:  ldarg.0
    IL_0194:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
    IL_0199:  stloc.s    V_6
    IL_019b:  ldarg.0
    IL_019c:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
    IL_01a1:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
    IL_01a7:  ldarg.0
    IL_01a8:  ldc.i4.m1
    IL_01a9:  dup
    IL_01aa:  stloc.0
    IL_01ab:  stfld      "int C.<M>d__0.<>1__state"
    IL_01b0:  ldloca.s   V_6
    IL_01b2:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
    IL_01b7:  nop
    IL_01b8:  ldstr      "Finally "
    IL_01bd:  call       "void System.Console.Write(string)"
    IL_01c2:  nop
    IL_01c3:  nop
    IL_01c4:  ldarg.0
    IL_01c5:  ldfld      "object C.<M>d__0.<>s__1"
    IL_01ca:  stloc.s    V_5
    IL_01cc:  ldloc.s    V_5
    IL_01ce:  brfalse.s  IL_01ed
    IL_01d0:  ldloc.s    V_5
    IL_01d2:  isinst     "System.Exception"
    IL_01d7:  stloc.s    V_7
    IL_01d9:  ldloc.s    V_7
    IL_01db:  brtrue.s   IL_01e0
    IL_01dd:  ldloc.s    V_5
    IL_01df:  throw
    IL_01e0:  ldloc.s    V_7
    IL_01e2:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
    IL_01e7:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
    IL_01ec:  nop
    IL_01ed:  ldarg.0
    IL_01ee:  ldfld      "int C.<M>d__0.<>s__2"
    IL_01f3:  pop
    IL_01f4:  ldarg.0
    IL_01f5:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_01fa:  brfalse.s  IL_01fe
    IL_01fc:  leave.s    IL_0242
    IL_01fe:  ldarg.0
    IL_01ff:  ldnull
    IL_0200:  stfld      "object C.<M>d__0.<>s__1"
    IL_0205:  ldnull
    IL_0206:  throw
  }
  catch System.Exception
  {
    IL_0207:  stloc.s    V_7
    IL_0209:  ldarg.0
    IL_020a:  ldc.i4.s   -2
    IL_020c:  stfld      "int C.<M>d__0.<>1__state"
    IL_0211:  ldarg.0
    IL_0212:  ldnull
    IL_0213:  stfld      "object C.<M>d__0.<>s__1"
    IL_0218:  ldarg.0
    IL_0219:  ldnull
    IL_021a:  stfld      "object C.<M>d__0.<>s__3"
    IL_021f:  ldarg.0
    IL_0220:  ldc.i4.0
    IL_0221:  stfld      "int C.<M>d__0.<>2__current"
    IL_0226:  ldarg.0
    IL_0227:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
    IL_022c:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
    IL_0231:  nop
    IL_0232:  ldarg.0
    IL_0233:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
    IL_0238:  ldloc.s    V_7
    IL_023a:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
    IL_023f:  nop
    IL_0240:  leave.s    IL_0286
  }
  IL_0242:  ldarg.0
  IL_0243:  ldc.i4.s   -2
  IL_0245:  stfld      "int C.<M>d__0.<>1__state"
  IL_024a:  ldarg.0
  IL_024b:  ldnull
  IL_024c:  stfld      "object C.<M>d__0.<>s__1"
  IL_0251:  ldarg.0
  IL_0252:  ldnull
  IL_0253:  stfld      "object C.<M>d__0.<>s__3"
  IL_0258:  ldarg.0
  IL_0259:  ldc.i4.0
  IL_025a:  stfld      "int C.<M>d__0.<>2__current"
  IL_025f:  ldarg.0
  IL_0260:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
  IL_0265:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
  IL_026a:  nop
  IL_026b:  ldarg.0
  IL_026c:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
  IL_0271:  ldc.i4.0
  IL_0272:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_0277:  nop
  IL_0278:  ret
  IL_0279:  ldarg.0
  IL_027a:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
  IL_027f:  ldc.i4.1
  IL_0280:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_0285:  nop
  IL_0286:  ret
}
""");
            comp = CreateRuntimeAsyncCompilation([Run(2), source], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
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

        [Fact]
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
            var expectedOutput = "1";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
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
            var expectedOutput = "none";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
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
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "int").WithLocation(6, 22)
                );
        }

        [Fact]
        public void TestWellKnownMembers()
        {
            var comp = CreateCompilation(AsyncStreamsTypes, references: new[] { NetStandard20.ExtraReferences.SystemThreadingTasksExtensions }, targetFramework: TargetFramework.NetStandard20);
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

            verifyMember(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctorSourceAndToken,
                "System.Threading.Tasks.ValueTask<TResult>..ctor(System.Threading.Tasks.Sources.IValueTaskSource<TResult> source, System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctorValue,
                "System.Threading.Tasks.ValueTask<TResult>..ctor(TResult result)");

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
        public void DisposeAsyncInBadState()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M([EnumeratorCancellation] CancellationToken token)
    {
        yield return 1;
        while (true)
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
        }
    }
    public static async Task Main()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        var enumerator = M(token).GetAsyncEnumerator();
        if (!await enumerator.MoveNextAsync()) { throw null; }

        var task = enumerator.MoveNextAsync();
        try
        {
            await enumerator.DisposeAsync();
            throw null;
        }
        catch (System.NotSupportedException)
        {
            System.Console.Write(""DisposeAsync threw. "");
        }

        source.Cancel();
        try
        {
            await task;
        }
        catch (System.OperationCanceledException)
        {
            System.Console.Write(""Already cancelled"");
        }
    }
}";
            var expectedOutput = "DisposeAsync threw. Already cancelled";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void DisposeAsyncBeforeRunning()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        await System.Threading.Tasks.Task.CompletedTask;
    }
    public static async System.Threading.Tasks.Task Main()
    {
        var enumerator = M().GetAsyncEnumerator();
        await enumerator.DisposeAsync();
        System.Console.Write(""done"");
    }
}";
            var expectedOutput = "done";

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void DisposeAsyncTwiceAfterRunning()
        {
            string source = @"
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        await System.Threading.Tasks.Task.CompletedTask;
    }
    public static async System.Threading.Tasks.Task Main()
    {
        var enumerator = M().GetAsyncEnumerator();
        if (!await enumerator.MoveNextAsync()) throw null;

        if (await enumerator.MoveNextAsync()) throw null;

        await enumerator.DisposeAsync();
        await enumerator.DisposeAsync();

        if (await enumerator.MoveNextAsync()) throw null;

        System.Console.Write(""done"");
    }
}";
            var expectedOutput = "done";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
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
            var expectedOutput = "Base.Func;Derived.Func;";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
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
            var expectedOutput = "B1::F;D::F;B1::F;";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void AsyncIteratorReturningEnumerator_UsingCancellationToken()
        {
            string source = @"
using static System.Console;
using System.Threading;
using System.Threading.Tasks;
public class D
{
    static async Task Main()
    {
        var c = new C(42);
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            CancellationToken token = source.Token;
            await using (var enumerator = c.M(token))
            {
                if (!await enumerator.MoveNextAsync()) throw null;
                System.Console.Write($""{enumerator.Current} ""); // 42

                if (!await enumerator.MoveNextAsync()) throw null;
                System.Console.Write($""{enumerator.Current} ""); // 43

                var task = enumerator.MoveNextAsync(); // starts long computation
                source.Cancel();

                try
                {
                    await task;
                }
                catch (System.OperationCanceledException)
                {
                    Write(""Cancelled"");
                }
            }
        }
    }
}
public class C
{
    private int value;
    public C(int value)
    {
        this.value = value;
    }
    public async System.Collections.Generic.IAsyncEnumerator<int> M(CancellationToken token)
    {
        yield return value++;
        yield return value;
        System.Console.Write($""Long "");
        bool b = true;
        while (b)
        {
            await Task.Delay(100);
            token.ThrowIfCancellationRequested();
        }
        System.Console.Write($""SKIPPED"");
    }
}";
            var expectedOutput = "42 43 Long Cancelled";
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_NoTokenPassedInGetAsyncEnumerator()
        {
            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        var enumerable = Iter(42, token, token);
        await using var enumerator = enumerable.GetAsyncEnumerator(); // no token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        source.Cancel();
        try
        {
            await enumerator.MoveNextAsync();
        }
        catch (System.OperationCanceledException)
        {
            Write(""Cancelled"");
        }
    }
    static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token, CancellationToken origToken)
    {
        if (!token.Equals(origToken)) throw null; // no need for a combined token
        yield return value++;
        await Task.Yield();
        yield return value++;
        token.ThrowIfCancellationRequested();
        Write(""SKIPPED"");
        yield return value++;
    }
}";
            var expectedOutput = "42 43 Cancelled";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            // IL for GetAsyncEnumerator is verified by AsyncIteratorWithAwaitCompletedAndYield already
            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_NoTokenPassedInGetAsyncEnumerator_LocalFunction()
        {
            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        var enumerable = localIter(42, token, token);
        await using var enumerator = enumerable.GetAsyncEnumerator(); // no token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        source.Cancel();
        try
        {
            await enumerator.MoveNextAsync();
        }
        catch (System.OperationCanceledException)
        {
            Write(""Cancelled"");
        }

        static async System.Collections.Generic.IAsyncEnumerable<int> localIter(int value, [EnumeratorCancellation] CancellationToken token, CancellationToken origToken)
        {
            if (!token.Equals(origToken)) throw null; // no need for a combined token
            yield return value++;
            await Task.Yield();
            yield return value++;
            token.ThrowIfCancellationRequested();
            Write(""SKIPPED"");
            yield return value++;
        }
    }
}";
            var expectedOutput = "42 43 Cancelled";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            // IL for GetAsyncEnumerator is verified by AsyncIteratorWithAwaitCompletedAndYield_LocalFunction already
            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_SameTokenPassedInGetAsyncEnumerator()
        {
            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        var enumerable = Iter(42, token, origToken: token);
        await using var enumerator = enumerable.GetAsyncEnumerator(token); // same token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        source.Cancel();
        try
        {
            await enumerator.MoveNextAsync();
        }
        catch (System.OperationCanceledException)
        {
            Write(""Cancelled"");
        }
    }
    static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token, CancellationToken origToken)
    {
        if (!token.Equals(origToken)) throw null; // no need for a combined token
        yield return value++;
        await Task.Yield();
        yield return value++;
        token.ThrowIfCancellationRequested();
        Write(""SKIPPED"");
        yield return value++;
    }
}";
            var expectedOutput = "42 43 Cancelled";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_DefaultTokenPassedInGetAsyncEnumerator()
        {
            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        var enumerable = Iter(42, token);
        await using var enumerator = enumerable.GetAsyncEnumerator(default); // default token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        source.Cancel();
        try
        {
            await enumerator.MoveNextAsync();
        }
        catch (System.OperationCanceledException)
        {
            Write(""Cancelled"");
        }
    }
    static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token)
    {
        yield return value++;
        await Task.Yield();
        yield return value++;
        token.ThrowIfCancellationRequested();
        Write(""SKIPPED"");
        yield return value++;
    }
}";
            var expectedOutput = "42 43 Cancelled";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_SomeTokenPassedInGetAsyncEnumerator()
        {
            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        var enumerable = Iter(42, default, token);
        await using var enumerator = enumerable.GetAsyncEnumerator(token); // some token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        source.Cancel();
        try
        {
            await enumerator.MoveNextAsync();
        }
        catch (System.OperationCanceledException)
        {
            Write(""Cancelled"");
        }
    }
    static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token1, CancellationToken origToken)
    {
        if (!token1.Equals(origToken)) throw null;
        yield return value++;
        await Task.Yield();
        yield return value++;
        token1.ThrowIfCancellationRequested();
        Write(""SKIPPED"");
        yield return value++;
    }
}";
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                var expectedOutput = "42 43 Cancelled";
                var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: options);
                var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                // GetAsyncEnumerator's token parameter is used directly, since the argument token is default
                verifier.VerifyIL("C.<Iter>d__1.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)", @"
{
  // Code size      200 (0xc8)
  .maxstack  3
  .locals init (C.<Iter>d__1 V_0,
                System.Threading.CancellationToken V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Iter>d__1.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0035
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""int C.<Iter>d__1.<>l__initialThreadId""
  IL_0010:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_0015:  bne.un.s   IL_0035
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.s   -3
  IL_001a:  stfld      ""int C.<Iter>d__1.<>1__state""
  IL_001f:  ldarg.0
  IL_0020:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_0025:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<Iter>d__1.<>t__builder""
  IL_002a:  ldarg.0
  IL_002b:  ldc.i4.0
  IL_002c:  stfld      ""bool C.<Iter>d__1.<>w__disposeMode""
  IL_0031:  ldarg.0
  IL_0032:  stloc.0
  IL_0033:  br.s       IL_003d
  IL_0035:  ldc.i4.s   -3
  IL_0037:  newobj     ""C.<Iter>d__1..ctor(int)""
  IL_003c:  stloc.0
  IL_003d:  ldloc.0
  IL_003e:  ldarg.0
  IL_003f:  ldfld      ""int C.<Iter>d__1.<>3__value""
  IL_0044:  stfld      ""int C.<Iter>d__1.value""
  IL_0049:  ldarg.0
  IL_004a:  ldflda     ""System.Threading.CancellationToken C.<Iter>d__1.<>3__token1""
  IL_004f:  ldloca.s   V_1
  IL_0051:  initobj    ""System.Threading.CancellationToken""
  IL_0057:  ldloc.1
  IL_0058:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_005d:  brfalse.s  IL_0068
  IL_005f:  ldloc.0
  IL_0060:  ldarg.1
  IL_0061:  stfld      ""System.Threading.CancellationToken C.<Iter>d__1.token1""
  IL_0066:  br.s       IL_00ba
  IL_0068:  ldarga.s   V_1
  IL_006a:  ldarg.0
  IL_006b:  ldfld      ""System.Threading.CancellationToken C.<Iter>d__1.<>3__token1""
  IL_0070:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_0075:  brtrue.s   IL_0089
  IL_0077:  ldarga.s   V_1
  IL_0079:  ldloca.s   V_1
  IL_007b:  initobj    ""System.Threading.CancellationToken""
  IL_0081:  ldloc.1
  IL_0082:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_0087:  brfalse.s  IL_0097
  IL_0089:  ldloc.0
  IL_008a:  ldarg.0
  IL_008b:  ldfld      ""System.Threading.CancellationToken C.<Iter>d__1.<>3__token1""
  IL_0090:  stfld      ""System.Threading.CancellationToken C.<Iter>d__1.token1""
  IL_0095:  br.s       IL_00ba
  IL_0097:  ldarg.0
  IL_0098:  ldarg.0
  IL_0099:  ldfld      ""System.Threading.CancellationToken C.<Iter>d__1.<>3__token1""
  IL_009e:  ldarg.1
  IL_009f:  call       ""System.Threading.CancellationTokenSource System.Threading.CancellationTokenSource.CreateLinkedTokenSource(System.Threading.CancellationToken, System.Threading.CancellationToken)""
  IL_00a4:  stfld      ""System.Threading.CancellationTokenSource C.<Iter>d__1.<>x__combinedTokens""
  IL_00a9:  ldloc.0
  IL_00aa:  ldarg.0
  IL_00ab:  ldfld      ""System.Threading.CancellationTokenSource C.<Iter>d__1.<>x__combinedTokens""
  IL_00b0:  callvirt   ""System.Threading.CancellationToken System.Threading.CancellationTokenSource.Token.get""
  IL_00b5:  stfld      ""System.Threading.CancellationToken C.<Iter>d__1.token1""
  IL_00ba:  ldloc.0
  IL_00bb:  ldarg.0
  IL_00bc:  ldfld      ""System.Threading.CancellationToken C.<Iter>d__1.<>3__origToken""
  IL_00c1:  stfld      ""System.Threading.CancellationToken C.<Iter>d__1.origToken""
  IL_00c6:  ldloc.0
  IL_00c7:  ret
}
");
                comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                    .VerifyDiagnostics();
            }
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_SomeTokenPassedInGetAsyncEnumerator_LocalFunction()
        {
            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        var enumerable = Iter(42, default, token);
        await using var enumerator = enumerable.GetAsyncEnumerator(token); // some token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        source.Cancel();
        try
        {
            await enumerator.MoveNextAsync();
        }
        catch (System.OperationCanceledException)
        {
            Write(""Cancelled"");
        }
        static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token1, CancellationToken origToken)
        {
            if (!token1.Equals(origToken)) throw null;
            yield return value++;
            await Task.Yield();
            yield return value++;
            token1.ThrowIfCancellationRequested();
            Write(""SKIPPED"");
            yield return value++;
        }
    }
}";
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                var expectedOutput = "42 43 Cancelled";
                var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: options, parseOptions: TestOptions.Regular9);
                var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                // GetAsyncEnumerator's token parameter is used directly, since the argument token is default
                verifier.VerifyIL("C.<<Main>g__Iter|0_0>d.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)", @"
{
  // Code size      200 (0xc8)
  .maxstack  3
  .locals init (C.<<Main>g__Iter|0_0>d V_0,
                System.Threading.CancellationToken V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<<Main>g__Iter|0_0>d.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0035
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""int C.<<Main>g__Iter|0_0>d.<>l__initialThreadId""
  IL_0010:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_0015:  bne.un.s   IL_0035
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.s   -3
  IL_001a:  stfld      ""int C.<<Main>g__Iter|0_0>d.<>1__state""
  IL_001f:  ldarg.0
  IL_0020:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_0025:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<<Main>g__Iter|0_0>d.<>t__builder""
  IL_002a:  ldarg.0
  IL_002b:  ldc.i4.0
  IL_002c:  stfld      ""bool C.<<Main>g__Iter|0_0>d.<>w__disposeMode""
  IL_0031:  ldarg.0
  IL_0032:  stloc.0
  IL_0033:  br.s       IL_003d
  IL_0035:  ldc.i4.s   -3
  IL_0037:  newobj     ""C.<<Main>g__Iter|0_0>d..ctor(int)""
  IL_003c:  stloc.0
  IL_003d:  ldloc.0
  IL_003e:  ldarg.0
  IL_003f:  ldfld      ""int C.<<Main>g__Iter|0_0>d.<>3__value""
  IL_0044:  stfld      ""int C.<<Main>g__Iter|0_0>d.value""
  IL_0049:  ldarg.0
  IL_004a:  ldflda     ""System.Threading.CancellationToken C.<<Main>g__Iter|0_0>d.<>3__token1""
  IL_004f:  ldloca.s   V_1
  IL_0051:  initobj    ""System.Threading.CancellationToken""
  IL_0057:  ldloc.1
  IL_0058:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_005d:  brfalse.s  IL_0068
  IL_005f:  ldloc.0
  IL_0060:  ldarg.1
  IL_0061:  stfld      ""System.Threading.CancellationToken C.<<Main>g__Iter|0_0>d.token1""
  IL_0066:  br.s       IL_00ba
  IL_0068:  ldarga.s   V_1
  IL_006a:  ldarg.0
  IL_006b:  ldfld      ""System.Threading.CancellationToken C.<<Main>g__Iter|0_0>d.<>3__token1""
  IL_0070:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_0075:  brtrue.s   IL_0089
  IL_0077:  ldarga.s   V_1
  IL_0079:  ldloca.s   V_1
  IL_007b:  initobj    ""System.Threading.CancellationToken""
  IL_0081:  ldloc.1
  IL_0082:  call       ""bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)""
  IL_0087:  brfalse.s  IL_0097
  IL_0089:  ldloc.0
  IL_008a:  ldarg.0
  IL_008b:  ldfld      ""System.Threading.CancellationToken C.<<Main>g__Iter|0_0>d.<>3__token1""
  IL_0090:  stfld      ""System.Threading.CancellationToken C.<<Main>g__Iter|0_0>d.token1""
  IL_0095:  br.s       IL_00ba
  IL_0097:  ldarg.0
  IL_0098:  ldarg.0
  IL_0099:  ldfld      ""System.Threading.CancellationToken C.<<Main>g__Iter|0_0>d.<>3__token1""
  IL_009e:  ldarg.1
  IL_009f:  call       ""System.Threading.CancellationTokenSource System.Threading.CancellationTokenSource.CreateLinkedTokenSource(System.Threading.CancellationToken, System.Threading.CancellationToken)""
  IL_00a4:  stfld      ""System.Threading.CancellationTokenSource C.<<Main>g__Iter|0_0>d.<>x__combinedTokens""
  IL_00a9:  ldloc.0
  IL_00aa:  ldarg.0
  IL_00ab:  ldfld      ""System.Threading.CancellationTokenSource C.<<Main>g__Iter|0_0>d.<>x__combinedTokens""
  IL_00b0:  callvirt   ""System.Threading.CancellationToken System.Threading.CancellationTokenSource.Token.get""
  IL_00b5:  stfld      ""System.Threading.CancellationToken C.<<Main>g__Iter|0_0>d.token1""
  IL_00ba:  ldloc.0
  IL_00bb:  ldarg.0
  IL_00bc:  ldfld      ""System.Threading.CancellationToken C.<<Main>g__Iter|0_0>d.<>3__origToken""
  IL_00c1:  stfld      ""System.Threading.CancellationToken C.<<Main>g__Iter|0_0>d.origToken""
  IL_00c6:  ldloc.0
  IL_00c7:  ret
}
");
                comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                    .VerifyDiagnostics();
            }
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_SomeTokenPassedInGetAsyncEnumerator_OptionalParameter()
        {
            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        var enumerable = Iter(42); // default value from optional parameter
        await using var enumerator = enumerable.GetAsyncEnumerator(token); // some token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        source.Cancel();
        try
        {
            await enumerator.MoveNextAsync();
        }
        catch (System.OperationCanceledException)
        {
            Write(""Cancelled"");
        }
    }
    static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token1 = default)
    {
        yield return value++;
        await Task.Yield();
        yield return value++;
        token1.ThrowIfCancellationRequested();
        Write(""SKIPPED"");
        yield return value++;
    }
}";
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                // field for token1 gets overridden with value from GetAsyncEnumerator's token parameter
                var expectedOutput = "42 43 Cancelled";
                var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: options);
                CompileAndVerify(comp, expectedOutput: expectedOutput)
                    .VerifyDiagnostics();

                comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                    .VerifyDiagnostics();
            }
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_SomeTokenPassedInGetAsyncEnumerator_ButNoAttribute()
        {
            string source = @"
using static System.Console;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        var enumerable = Iter(42, default);
        await using var enumerator = enumerable.GetAsyncEnumerator(token); // some token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        source.Cancel();
        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 44
    }
    static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, CancellationToken token1) // no attribute set
    {
        yield return value++;
        await Task.Yield();
        yield return value++;
        token1.ThrowIfCancellationRequested();
        Write(""REACHED "");
        yield return value++;
    }
}";
            var expectedOutput = "42 43 REACHED 44";
            DiagnosticDescription[] expectedDiagnostics = [
                // 0.cs(24,67): warning CS8425: Async-iterator 'C.Iter(int, CancellationToken)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
                //     static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, CancellationToken token1) // no attribute set
                Diagnostic(ErrorCode.WRN_UndecoratedCancellationTokenParameter, "Iter").WithArguments("C.Iter(int, System.Threading.CancellationToken)").WithLocation(24, 67)
                ];

            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_SomeTokenPassedInGetAsyncEnumerator_ButNoAttribute_LocalFunction()
        {
            string source = @"
using static System.Console;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        var enumerable = Iter(42, default);
        await using var enumerator = enumerable.GetAsyncEnumerator(token); // some token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        source.Cancel();
        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 44

        static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, CancellationToken token1) // no attribute set
        {
            yield return value++;
            await Task.Yield();
            yield return value++;
            token1.ThrowIfCancellationRequested();
            Write(""REACHED "");
            yield return value++;
        }
    }
}";
            var expectedOutput = "42 43 REACHED 44";
            DiagnosticDescription[] expectedDiagnostics = [
                // (24,71): warning CS8425: Async-iterator 'Iter(int, CancellationToken)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
                //         static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, CancellationToken token1) // no attribute set
                Diagnostic(ErrorCode.WRN_UndecoratedCancellationTokenParameter, "Iter").WithArguments("Iter(int, System.Threading.CancellationToken)").WithLocation(24, 71)
                ];

            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics(expectedDiagnostics);
        }

        [Theory, CombinatorialData, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_SomeOtherTokenPassedInGetAsyncEnumerator(bool useDebug, bool cancelFirst)
        {
            var options = useDebug ? TestOptions.DebugExe : TestOptions.ReleaseExe;
            var sourceToCancel = cancelFirst ? "source1" : "source2";

            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source1 = new CancellationTokenSource();
        CancellationToken token1 = source1.Token;
        using CancellationTokenSource source2 = new CancellationTokenSource();
        CancellationToken token2 = source2.Token;
        var enumerable = Iter(token1, token2, token1, 42);

        await using var enumerator = enumerable.GetAsyncEnumerator(token2); // some other token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        SOURCETOCANCEL.Cancel();
        try
        {
            await enumerator.MoveNextAsync();
        }
        catch (System.OperationCanceledException)
        {
            Write(""Cancelled"");
        }
    }

    static async System.Collections.Generic.IAsyncEnumerable<int> Iter(CancellationToken token1, CancellationToken token2, [EnumeratorCancellation] CancellationToken token3, int value) // note: token is in first position
    {
        if (token3.Equals(token1) || token3.Equals(token2)) throw null;
        yield return value++;
        await Task.Yield();
        yield return value++;
        token3.ThrowIfCancellationRequested();
        Write(""SKIPPED"");
        yield return value++;
    }
}".Replace("SOURCETOCANCEL", sourceToCancel);

            var expectedOutput = "42 43 Cancelled";

            // cancelling either the token given as argument or the one given to GetAsyncEnumerator results in cancelling the combined token3

            var comp = CreateCompilationWithAsyncIterator([source, EnumeratorCancellationAttributeType], options: options);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All));
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput),
                symbolValidator: verifyAsync2MembersAndInterfaces, verify: Verification.Skipped);

            verifier.VerifyDiagnostics();

            verifier.VerifyIL("C.<Iter>d__1.System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)", """
{
  // Code size      201 (0xc9)
  .maxstack  3
  .locals init (C.<Iter>d__1 V_0,
                System.Threading.CancellationToken V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<Iter>d__1.<>1__state"
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_002a
  IL_000a:  ldarg.0
  IL_000b:  ldfld      "int C.<Iter>d__1.<>l__initialThreadId"
  IL_0010:  call       "int System.Environment.CurrentManagedThreadId.get"
  IL_0015:  bne.un.s   IL_002a
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.s   -3
  IL_001a:  stfld      "int C.<Iter>d__1.<>1__state"
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4.0
  IL_0021:  stfld      "bool C.<Iter>d__1.<>w__disposeMode"
  IL_0026:  ldarg.0
  IL_0027:  stloc.0
  IL_0028:  br.s       IL_0032
  IL_002a:  ldc.i4.s   -3
  IL_002c:  newobj     "C.<Iter>d__1..ctor(int)"
  IL_0031:  stloc.0
  IL_0032:  ldloc.0
  IL_0033:  ldarg.0
  IL_0034:  ldfld      "System.Threading.CancellationToken C.<Iter>d__1.<>3__token1"
  IL_0039:  stfld      "System.Threading.CancellationToken C.<Iter>d__1.token1"
  IL_003e:  ldloc.0
  IL_003f:  ldarg.0
  IL_0040:  ldfld      "System.Threading.CancellationToken C.<Iter>d__1.<>3__token2"
  IL_0045:  stfld      "System.Threading.CancellationToken C.<Iter>d__1.token2"
  IL_004a:  ldarg.0
  IL_004b:  ldflda     "System.Threading.CancellationToken C.<Iter>d__1.<>3__token3"
  IL_0050:  ldloca.s   V_1
  IL_0052:  initobj    "System.Threading.CancellationToken"
  IL_0058:  ldloc.1
  IL_0059:  call       "bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)"
  IL_005e:  brfalse.s  IL_0069
  IL_0060:  ldloc.0
  IL_0061:  ldarg.1
  IL_0062:  stfld      "System.Threading.CancellationToken C.<Iter>d__1.token3"
  IL_0067:  br.s       IL_00bb
  IL_0069:  ldarga.s   V_1
  IL_006b:  ldarg.0
  IL_006c:  ldfld      "System.Threading.CancellationToken C.<Iter>d__1.<>3__token3"
  IL_0071:  call       "bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)"
  IL_0076:  brtrue.s   IL_008a
  IL_0078:  ldarga.s   V_1
  IL_007a:  ldloca.s   V_1
  IL_007c:  initobj    "System.Threading.CancellationToken"
  IL_0082:  ldloc.1
  IL_0083:  call       "bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)"
  IL_0088:  brfalse.s  IL_0098
  IL_008a:  ldloc.0
  IL_008b:  ldarg.0
  IL_008c:  ldfld      "System.Threading.CancellationToken C.<Iter>d__1.<>3__token3"
  IL_0091:  stfld      "System.Threading.CancellationToken C.<Iter>d__1.token3"
  IL_0096:  br.s       IL_00bb
  IL_0098:  ldarg.0
  IL_0099:  ldarg.0
  IL_009a:  ldfld      "System.Threading.CancellationToken C.<Iter>d__1.<>3__token3"
  IL_009f:  ldarg.1
  IL_00a0:  call       "System.Threading.CancellationTokenSource System.Threading.CancellationTokenSource.CreateLinkedTokenSource(System.Threading.CancellationToken, System.Threading.CancellationToken)"
  IL_00a5:  stfld      "System.Threading.CancellationTokenSource C.<Iter>d__1.<>x__combinedTokens"
  IL_00aa:  ldloc.0
  IL_00ab:  ldarg.0
  IL_00ac:  ldfld      "System.Threading.CancellationTokenSource C.<Iter>d__1.<>x__combinedTokens"
  IL_00b1:  callvirt   "System.Threading.CancellationToken System.Threading.CancellationTokenSource.Token.get"
  IL_00b6:  stfld      "System.Threading.CancellationToken C.<Iter>d__1.token3"
  IL_00bb:  ldloc.0
  IL_00bc:  ldarg.0
  IL_00bd:  ldfld      "int C.<Iter>d__1.<>3__value"
  IL_00c2:  stfld      "int C.<Iter>d__1.value"
  IL_00c7:  ldloc.0
  IL_00c8:  ret
}
""");

            static void verifyAsync2MembersAndInterfaces(ModuleSymbol module)
            {
                var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C.<Iter>d__1");
                AssertEx.SetEqual([
                    "System.Int32 C.<Iter>d__1.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                    "System.Int32 C.<Iter>d__1.<>2__current",
                    "System.Boolean C.<Iter>d__1.<>w__disposeMode",
                    "System.Threading.CancellationTokenSource C.<Iter>d__1.<>x__combinedTokens",
                    "System.Int32 C.<Iter>d__1.<>l__initialThreadId",
                    "System.Threading.CancellationToken C.<Iter>d__1.token1",
                    "System.Threading.CancellationToken C.<Iter>d__1.<>3__token1",
                    "System.Threading.CancellationToken C.<Iter>d__1.token2",
                    "System.Threading.CancellationToken C.<Iter>d__1.<>3__token2",
                    "System.Threading.CancellationToken C.<Iter>d__1.token3",
                    "System.Threading.CancellationToken C.<Iter>d__1.<>3__token3",
                    "System.Int32 C.<Iter>d__1.value",
                    "System.Int32 C.<Iter>d__1.<>3__value",
                    "C.<Iter>d__1..ctor(System.Int32 <>1__state)",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32> C.<Iter>d__1.System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)])",
                    "System.Threading.Tasks.ValueTask<System.Boolean> C.<Iter>d__1.MoveNextAsync()",
                    "System.Int32 C.<Iter>d__1.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current.get",
                    "System.Threading.Tasks.ValueTask C.<Iter>d__1.System.IAsyncDisposable.DisposeAsync()",
                    "System.Int32 C.<Iter>d__1.<>1__state"
                    ], type.GetMembersUnordered().ToTestDisplayStrings());
            }
        }

        [Theory, CombinatorialData, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_SomeOtherTokenPassedInGetAsyncEnumerator_LocalFunction(bool useDebug, bool cancelFirst)
        {
            var options = useDebug ? TestOptions.DebugExe : TestOptions.ReleaseExe;
            var sourceToCancel = cancelFirst ? "source1" : "source2";

            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source1 = new CancellationTokenSource();
        CancellationToken token1 = source1.Token;
        using CancellationTokenSource source2 = new CancellationTokenSource();
        CancellationToken token2 = source2.Token;
        var enumerable = Iter(token1, token2, token1, 42);

        await using var enumerator = enumerable.GetAsyncEnumerator(token2); // some other token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        SOURCETOCANCEL.Cancel();
        try
        {
            await enumerator.MoveNextAsync();
        }
        catch (System.OperationCanceledException)
        {
            Write(""Cancelled"");
        }

        static async System.Collections.Generic.IAsyncEnumerable<int> Iter(CancellationToken token1, CancellationToken token2, [EnumeratorCancellation] CancellationToken token3, int value) // note: token is in first position
        {
            if (token3.Equals(token1) || token3.Equals(token2)) throw null;
            yield return value++;
            await Task.Yield();
            yield return value++;
            token3.ThrowIfCancellationRequested();
            Write(""SKIPPED"");
            yield return value++;
        }
    }
}".Replace("SOURCETOCANCEL", sourceToCancel);

            var expectedOutput = "42 43 Cancelled";

            // cancelling either the token given as argument or the one given to GetAsyncEnumerator results in cancelling the combined token3
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: options, parseOptions: TestOptions.Regular9);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_TwoDefaultTokens()
        {
            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        var enumerable = Iter(default);
        await using var enumerator = enumerable.GetAsyncEnumerator(default);

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 1

        if (await enumerator.MoveNextAsync()) throw null;
    }

    static async System.Collections.Generic.IAsyncEnumerable<int> Iter([EnumeratorCancellation] CancellationToken token)
    {
        if (!token.Equals(default)) Write(""SKIPPED"");
        yield return 1;
        await Task.Yield();
    }
}";
            var expectedOutput = "1";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        [WorkItem(39961, "https://github.com/dotnet/roslyn/issues/39961")]
        public void CancellationTokenParameter_WrongParameterType()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> Iter([EnumeratorCancellation] int value)
    {
        yield return value++;
        await Task.Yield();
    }
    static async Task Main()
    {
        await foreach (var i in Iter(42))
        {
            System.Console.Write(i);
        }
    }
}";
            var expectedOutput = "42";
            DiagnosticDescription[] expectedDiagnostics = [
                // (6,73): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'value' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-enumerable method
                //     static async System.Collections.Generic.IAsyncEnumerable<int> Iter([EnumeratorCancellation] int value)
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("value").WithLocation(6, 73)
                ];

            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        [WorkItem(39961, "https://github.com/dotnet/roslyn/issues/39961")]
        public void CancellationTokenParameter_WrongParameterType_LocalFunction()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var i in Iter(42))
        {
            System.Console.Write(i);
        }
        static async System.Collections.Generic.IAsyncEnumerable<int> Iter([EnumeratorCancellation] int value) // 1
        {
            yield return value++;
            await Task.Yield();
        }
    }
}";
            var expectedOutput = "42";
            DiagnosticDescription[] expectedDiagnostics = [
                // (12,77): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'value' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable
                //         static async System.Collections.Generic.IAsyncEnumerable<int> Iter([EnumeratorCancellation] int value) // 1
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("value").WithLocation(12, 77)
                ];

            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, TestOptions.DebugExe, TestOptions.Regular9);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_AsyncEnumerator()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async System.Collections.Generic.IAsyncEnumerator<int> Iter([EnumeratorCancellation] CancellationToken value)
    {
        yield return 1;
        await Task.Yield();
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType });
            comp.VerifyDiagnostics(
                // (7,73): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'value' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable
                //     static async System.Collections.Generic.IAsyncEnumerator<int> Iter([EnumeratorCancellation] CancellationToken value)
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("value").WithLocation(7, 73)
                );
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_IteratorMethod()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
class C
{
    static System.Collections.Generic.IEnumerable<int> Iter([EnumeratorCancellation] CancellationToken token)
    {
        yield return 1;
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType });
            comp.VerifyDiagnostics(
                // (6,62): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'token' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-enumerable method
                //     static System.Collections.Generic.IEnumerable<int> Iter([EnumeratorCancellation] CancellationToken token)
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("token").WithLocation(6, 62)
                );
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_AsyncMethod()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Async([EnumeratorCancellation] CancellationToken token)
    {
        await Task.Yield();
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType });
            comp.VerifyDiagnostics(
                // (7,30): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'token' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-enumerable method
                //     static async Task Async([EnumeratorCancellation] CancellationToken token)
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("token").WithLocation(7, 30)
                );
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_RegularMethod()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
class C
{
    static void M([EnumeratorCancellation] CancellationToken token)
    {
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType });
            comp.VerifyDiagnostics(
                // (6,20): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'token' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-enumerable method
                //     static void M([EnumeratorCancellation] CancellationToken token)
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("token").WithLocation(6, 20)
                );
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_Indexer()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
class C
{
    int this[[EnumeratorCancellation] CancellationToken key] => 0;
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType });
            comp.VerifyDiagnostics(
                // (6,15): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'key' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-enumerable method
                //     int this[[EnumeratorCancellation] CancellationToken key] => 0;
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("key").WithLocation(6, 15)
                );
        }

        [Fact]
        [WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        [WorkItem(35159, "https://github.com/dotnet/roslyn/issues/35159")]
        public void CancellationTokenParameter_TwoParameterHaveAttribute()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token1, [EnumeratorCancellation] CancellationToken token2)
    {
        yield return value++;
        await Task.Yield();
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType });
            comp.VerifyDiagnostics(
                // (7,67): error CS8426: The attribute [EnumeratorCancellation] cannot be used on multiple parameters
                //     static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token1, [EnumeratorCancellation] CancellationToken token2)
                Diagnostic(ErrorCode.ERR_MultipleEnumeratorCancellationAttributes, "Iter").WithLocation(7, 67)
                );
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_MissingAttributeType()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token1)
    {
        yield return value++;
        await Task.Yield();
    }
}";
            var comp = CreateCompilationWithAsyncIterator(new[] { source });
            comp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using System.Runtime.CompilerServices;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Runtime.CompilerServices;").WithLocation(2, 1),
                // (7,67): error CS8425: Async-iterator 'C.Iter(int, CancellationToken)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
                //     static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token1)
                Diagnostic(ErrorCode.WRN_UndecoratedCancellationTokenParameter, "Iter").WithArguments("C.Iter(int, System.Threading.CancellationToken)").WithLocation(7, 67),
                // (7,84): error CS0246: The type or namespace name 'EnumeratorCancellationAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //     static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token1)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "EnumeratorCancellation").WithArguments("EnumeratorCancellationAttribute").WithLocation(7, 84),
                // (7,84): error CS0246: The type or namespace name 'EnumeratorCancellation' could not be found (are you missing a using directive or an assembly reference?)
                //     static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token1)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "EnumeratorCancellation").WithArguments("EnumeratorCancellation").WithLocation(7, 84)
                );
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_ParameterProxyUntouched()
        {
            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        using CancellationTokenSource source1 = new CancellationTokenSource();
        CancellationToken token1 = source1.Token;
        var enumerable = Iter(42, token1);

        using CancellationTokenSource source2 = new CancellationTokenSource();
        CancellationToken token2 = source2.Token;
        await using var enumerator = enumerable.GetAsyncEnumerator(token2); // some token passed
        await EnumerateAsync(enumerator, source2);

        await using var enumerator2 = enumerable.GetAsyncEnumerator(default); // we'll use token1 saved in the enumerable
        await EnumerateAsync(enumerator2, default);
    }
    static async Task EnumerateAsync(System.Collections.Generic.IAsyncEnumerator<int> enumerator, CancellationTokenSource source)
    {
        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 43

        if (source != null)
            source.Cancel();
        try
        {
            await enumerator.MoveNextAsync();
            System.Console.Write($""{enumerator.Current} ""); // 44
        }
        catch (System.OperationCanceledException)
        {
            Write(""Cancelled "");
        }
    }
    static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, [EnumeratorCancellation] CancellationToken token1)
    {
        yield return value++;
        await Task.Yield();
        yield return value++;
        token1.ThrowIfCancellationRequested();
        Write(""Reached "");
        yield return value++;
    }
}";
            // The parameter proxy is left untouched by our copying the token parameter of GetAsyncEnumerator
            var expectedOutput = "42 43 Cancelled 42 43 Reached 44";

            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_Overridding()
        {
            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
public class Base
{
    public virtual async System.Collections.Generic.IAsyncEnumerable<int> Iter([EnumeratorCancellation] CancellationToken token1, int value)
    {
        yield return value++;
        await Task.Yield();
    }
}
public class C : Base
{
    static async Task Main()
    {
        var enumerable = new C().Iter(default, 42);

        using CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        source.Cancel();
        await using var enumerator = enumerable.GetAsyncEnumerator(token); // some token passed but unconsumed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current}""); // 42
    }
    public override async System.Collections.Generic.IAsyncEnumerable<int> Iter(CancellationToken token1, int value) // 1
    {
        await Task.Yield();
        token1.ThrowIfCancellationRequested();
        Write(""Reached "");
        yield return value;
    }
}";
            var expectedOutput = "Reached 42";
            DiagnosticDescription[] expectedDiagnostics = [
                // 0.cs(28,76): warning CS8425: Async-iterator 'C.Iter(CancellationToken, int)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
                //     public override async System.Collections.Generic.IAsyncEnumerable<int> Iter(CancellationToken token1, int value) // 1
                Diagnostic(ErrorCode.WRN_UndecoratedCancellationTokenParameter, "Iter").WithArguments("C.Iter(System.Threading.CancellationToken, int)").WithLocation(28, 76)
                ];

            // The overridden method lacks the EnumeratorCancellation attribute
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_Overridding2()
        {
            string source = @"
using static System.Console;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
public class Base
{
    public virtual async System.Collections.Generic.IAsyncEnumerable<int> Iter(CancellationToken token1, int value) // 1
    {
        yield return value++;
        await Task.Yield();
    }
}
public class C : Base
{
    static async Task Main()
    {
        var enumerable = new C().Iter(default, 42);

        using CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        await using var enumerator = enumerable.GetAsyncEnumerator(token); // some token passed

        if (!await enumerator.MoveNextAsync()) throw null;
        System.Console.Write($""{enumerator.Current} ""); // 42

        source.Cancel();
        try
        {
            await enumerator.MoveNextAsync();
        }
        catch (System.OperationCanceledException)
        {
            Write(""Cancelled"");
        }
    }
    public override async System.Collections.Generic.IAsyncEnumerable<int> Iter([EnumeratorCancellation] CancellationToken token1, int value)
    {
        yield return value++;
        await Task.Yield();
        token1.ThrowIfCancellationRequested();
        Write(""SKIPPED"");
        yield return value;
    }
}";
            var expectedOutput = "42 Cancelled";

            DiagnosticDescription[] expectedDiagnostics = [
                // 0.cs(8,75): warning CS8425: Async-iterator 'Base.Iter(CancellationToken, int)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
                //     public virtual async System.Collections.Generic.IAsyncEnumerable<int> Iter(CancellationToken token1, int value) // 1
                Diagnostic(ErrorCode.WRN_UndecoratedCancellationTokenParameter, "Iter").WithArguments("Base.Iter(System.Threading.CancellationToken, int)").WithLocation(8, 75)
                ];

            // The overridden method has the EnumeratorCancellation attribute
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact, WorkItem(35165, "https://github.com/dotnet/roslyn/issues/35165")]
        public void CancellationTokenParameter_MethodWithoutBody()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
public abstract class C
{
    public abstract async System.Collections.Generic.IAsyncEnumerable<int> M([EnumeratorCancellation] CancellationToken token); // 1
    public abstract System.Collections.Generic.IAsyncEnumerable<int> M2([EnumeratorCancellation] CancellationToken token); // 2
}
public interface I
{
    System.Collections.Generic.IAsyncEnumerable<int> M([EnumeratorCancellation] CancellationToken token); // 3
}
public partial class C2
{
    public delegate System.Collections.Generic.IAsyncEnumerable<int> Delegate([EnumeratorCancellation] CancellationToken token); // 4
    partial System.Collections.Generic.IAsyncEnumerable<int> M([EnumeratorCancellation] CancellationToken token); // 5
    partial async System.Collections.Generic.IAsyncEnumerable<int> M2([EnumeratorCancellation] CancellationToken token); // 6
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (6,76): error CS1994: The 'async' modifier can only be used in methods that have a body.
                //     public abstract async System.Collections.Generic.IAsyncEnumerable<int> M([EnumeratorCancellation] CancellationToken token); // 1
                Diagnostic(ErrorCode.ERR_BadAsyncLacksBody, "M").WithLocation(6, 76),
                // (7,74): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'token' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable
                //     public abstract System.Collections.Generic.IAsyncEnumerable<int> M2([EnumeratorCancellation] CancellationToken token); // 2
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("token").WithLocation(7, 74),
                // (11,57): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'token' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable
                //     System.Collections.Generic.IAsyncEnumerable<int> M([EnumeratorCancellation] CancellationToken token); // 3
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("token").WithLocation(11, 57),
                // (15,80): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'token' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable
                //     public delegate System.Collections.Generic.IAsyncEnumerable<int> Delegate([EnumeratorCancellation] CancellationToken token); // 4
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("token").WithLocation(15, 80),
                // (16,62): error CS8794: Partial method 'C2.M(CancellationToken)' must have accessibility modifiers because it has a non-void return type.
                //     partial System.Collections.Generic.IAsyncEnumerable<int> M([EnumeratorCancellation] CancellationToken token); // 5
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M").WithArguments("C2.M(System.Threading.CancellationToken)").WithLocation(16, 62),
                // (16,65): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'token' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable
                //     partial System.Collections.Generic.IAsyncEnumerable<int> M([EnumeratorCancellation] CancellationToken token); // 5
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("token").WithLocation(16, 65),
                // (17,68): error CS8794: Partial method 'C2.M2(CancellationToken)' must have accessibility modifiers because it has a non-void return type.
                //     partial async System.Collections.Generic.IAsyncEnumerable<int> M2([EnumeratorCancellation] CancellationToken token); // 6
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M2").WithArguments("C2.M2(System.Threading.CancellationToken)").WithLocation(17, 68)
                );
        }

        [Fact, WorkItem(35165, "https://github.com/dotnet/roslyn/issues/35165")]
        public void CancellationTokenParameter_LocalFunction()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
public class C
{
    async Task M()
    {
        await foreach (var i in local(default))
        {
        }

        async System.Collections.Generic.IAsyncEnumerable<int> local([EnumeratorCancellation] CancellationToken token)
        {
            yield return 1;
            await Task.Yield();
        }
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, EnumeratorCancellationAttributeType, AsyncStreamsTypes }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(35166, "https://github.com/dotnet/roslyn/issues/35166")]
        public void CancellationTokenParameter_ParameterWithoutAttribute()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
public class C
{
    public async System.Collections.Generic.IAsyncEnumerable<int> M1(CancellationToken token) // 1
    {
        yield return 1;
        await Task.Yield();
    }
    public async System.Collections.Generic.IAsyncEnumerable<int> M2(CancellationToken token, CancellationToken token2) // 2
    {
        yield return 1;
        await Task.Yield();
    }
    public async System.Collections.Generic.IAsyncEnumerable<int> M3(CancellationToken token, [EnumeratorCancellation] CancellationToken token2)
    {
        yield return 1;
        await Task.Yield();
    }
    async Task<int> M4(CancellationToken token)
    {
        await Task.Yield();
        return 1;
    }
}
public abstract class C2
{
    public abstract async System.Collections.Generic.IAsyncEnumerable<int> M(CancellationToken token); // 3
    public abstract System.Collections.Generic.IAsyncEnumerable<int> M2(CancellationToken token);
}
public interface I
{
    System.Collections.Generic.IAsyncEnumerable<int> M(CancellationToken token);
}
public partial class C3
{
    public delegate System.Collections.Generic.IAsyncEnumerable<int> Delegate(CancellationToken token);
    partial System.Collections.Generic.IAsyncEnumerable<int> M(CancellationToken token); // 4
    partial async System.Collections.Generic.IAsyncEnumerable<int> M2(CancellationToken token); // 5
}
";
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (7,67): warning CS8425: Async-iterator 'C.M1(CancellationToken)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
                //     public async System.Collections.Generic.IAsyncEnumerable<int> M1(CancellationToken token) // 1
                Diagnostic(ErrorCode.WRN_UndecoratedCancellationTokenParameter, "M1").WithArguments("C.M1(System.Threading.CancellationToken)").WithLocation(7, 67),
                // (12,67): warning CS8425: Async-iterator 'C.M2(CancellationToken, CancellationToken)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
                //     public async System.Collections.Generic.IAsyncEnumerable<int> M2(CancellationToken token, CancellationToken token2) // 2
                Diagnostic(ErrorCode.WRN_UndecoratedCancellationTokenParameter, "M2").WithArguments("C.M2(System.Threading.CancellationToken, System.Threading.CancellationToken)").WithLocation(12, 67),
                // (30,76): error CS1994: The 'async' modifier can only be used in methods that have a body.
                //     public abstract async System.Collections.Generic.IAsyncEnumerable<int> M(CancellationToken token); // 3
                Diagnostic(ErrorCode.ERR_BadAsyncLacksBody, "M").WithLocation(30, 76),
                // (40,62): error CS8794: Partial method 'C3.M(CancellationToken)' must have accessibility modifiers because it has a non-void return type.
                //     partial System.Collections.Generic.IAsyncEnumerable<int> M(CancellationToken token); // 4
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M").WithArguments("C3.M(System.Threading.CancellationToken)").WithLocation(40, 62),
                // (41,68): error CS8794: Partial method 'C3.M2(CancellationToken)' must have accessibility modifiers because it has a non-void return type.
                //     partial async System.Collections.Generic.IAsyncEnumerable<int> M2(CancellationToken token); // 5
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M2").WithArguments("C3.M2(System.Threading.CancellationToken)").WithLocation(41, 68)
                );
        }

        [Fact]
        [WorkItem(43936, "https://github.com/dotnet/roslyn/issues/43936")]
        public void TryFinallyNestedInsideFinally()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class C
{
    public static async IAsyncEnumerable<int> M()
    {
        try
        {
            await Task.Yield();
            yield return 1;
            throw null;
        }
        finally
        {
            Console.Write(""BEFORE "");

            try
            {
                Console.Write(""INSIDE "");
            }
            finally
            {
                Console.Write(""INSIDE2 "");
            }

            Console.Write(""AFTER "");
        }
        throw null;
    }
    public static async Task Main()
    {
        await foreach (var i in C.M())
        {
            break;
        }
    }
}";

            var expectedOutput = "BEFORE INSIDE INSIDE2 AFTER";

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(43936, "https://github.com/dotnet/roslyn/issues/43936")]
        public void TryFinallyNestedInsideFinally_WithAwaitInFinally()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class C
{
    public static async IAsyncEnumerable<int> M()
    {
        try
        {
            yield return 1;
            throw null;
        }
        finally
        {
            await Task.Yield();
            Console.Write(""BEFORE "");

            try
            {
                Console.Write(""INSIDE "");
            }
            finally
            {
                Console.Write(""INSIDE2 "");
            }

            Console.Write(""AFTER "");
        }
        throw null;
    }
    public static async Task Main()
    {
        await foreach (var i in C.M())
        {
            break;
        }
    }
}";
            var expectedOutput = "BEFORE INSIDE INSIDE2 AFTER";

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(43936, "https://github.com/dotnet/roslyn/issues/43936")]
        public void TryFinallyNestedInsideFinally_WithAwaitInNestedFinally()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class C
{
    public static async IAsyncEnumerable<int> M()
    {
        try
        {
            yield return 1;
            throw null;
        }
        finally
        {
            Console.Write(""BEFORE "");

            try
            {
                Console.Write(""INSIDE "");
            }
            finally
            {
                Console.Write(""INSIDE2 "");
                await Task.Yield();
            }

            Console.Write(""AFTER "");
        }
        throw null;
    }
    public static async Task Main()
    {
        await foreach (var i in C.M())
        {
            break;
        }
    }
}";
            var expectedOutput = "BEFORE INSIDE INSIDE2 AFTER";

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem(58444, "https://github.com/dotnet/roslyn/issues/58444")]
        public void ClearCurrentOnRegularExit()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

var r = new AsyncReader();
var enumerator = r.GetAsyncEnumerator();
try
{
    while (await enumerator.MoveNextAsync())
    {
        System.Console.Write(""RAN "");
    }

    if (enumerator.Current is null)
    {
        System.Console.Write(""CLEARED"");
    }
}
finally
{
    await enumerator.DisposeAsync();
}

class AsyncReader : IAsyncEnumerable<object>
{
    public async IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return new object();
        await Task.Yield();
        yield return new object();
        await Task.Yield();
        yield return new object();
    }
}
";
            var expectedOutput = "RAN RAN RAN CLEARED";

            var comp = CreateCompilationWithAsyncIterator(source);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem(58444, "https://github.com/dotnet/roslyn/issues/58444")]
        public void ClearCurrentOnException()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

var r = new AsyncReader();
var enumerator = r.GetAsyncEnumerator();
try
{
    try
    {
        while (await enumerator.MoveNextAsync())
        {
            Console.Write(""RAN "");
        }
    }
    catch (System.Exception e)
    {
        Console.Write(e.Message);
    }

    if (enumerator.Current is null)
    {
        Console.Write(""CLEARED"");
    }
}
finally
{
    await enumerator.DisposeAsync();
}

class AsyncReader : IAsyncEnumerable<object>
{
    public async IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return new object();
        await Task.Yield();
        yield return new object();
        await Task.Yield();
        throw new Exception(""EXCEPTION "");
    }
}
";
            var expectedOutput = "RAN RAN EXCEPTION CLEARED";

            var comp = CreateCompilationWithAsyncIterator(source);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem(58444, "https://github.com/dotnet/roslyn/issues/58444")]
        public void ClearCurrentOnRegularExit_Generic()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

var r = new AsyncReader<int>() { value = 42 };
var enumerator = r.GetAsyncEnumerator();
try
{
    while (await enumerator.MoveNextAsync())
    {
        if (enumerator.Current is 42)
            System.Console.Write(""RAN "");
    }

    if (enumerator.Current is 0)
        System.Console.Write(""CLEARED"");
}
finally
{
    await enumerator.DisposeAsync();
}

class AsyncReader<T> : IAsyncEnumerable<T>
{
    public T value;
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return value;
        await Task.Yield();
        yield return value;
        await Task.Yield();
        yield return value;
    }
}
";
            var expectedOutput = "RAN RAN RAN CLEARED";

            var comp = CreateCompilationWithAsyncIterator(source);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74362")]
        public void AwaitForeachInLocalFunctionInAccessor()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

foreach (var x in X.ValuesViaLocalFunction)
{
    Console.WriteLine(x);
}

public class X
{
    public static async IAsyncEnumerable<int> GetValues()
    {
        await Task.Yield();
        yield return 42;
    }

    public static IEnumerable<int> ValuesViaLocalFunction
    {
        get
        {
            foreach (var b in Do().ToBlockingEnumerable())
            {
                yield return b;
            }

            async IAsyncEnumerable<int> Do()
            {
                await foreach (var v in GetValues())
                {
                    yield return v;
                }
            }
        }
    }
}
""";
            var expectedOutput = "42";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.FailsPEVerify)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74362")]
        public void AwaitForeachInMethod()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

await foreach (var x in X.Do())
{
    Console.WriteLine(x);
}

public class X
{
    public static async IAsyncEnumerable<int> GetValues()
    {
        await Task.Yield();
        yield return 42;
    }

    public static async IAsyncEnumerable<int> Do()
    {
        await foreach (var v in GetValues())
        {
            yield return v;
        }
    }
}
""";
            var expectedOutput = "42";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.FailsPEVerify)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74362")]
        public void AwaitInForeachInLocalFunctionInAccessor()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class C
{
    public static void Main()
    {
        Console.Write(Property);
    }

    public static int Property
    {
        get
        {
            return Do().GetAwaiter().GetResult();

            async Task<int> Do()
            {
                IEnumerable<int> a = [42];
                foreach (var v in a)
                {
                    await Task.Yield();
                    return v;
                }

                return 0;
            }
        }
    }
}
""";
            var expectedOutput = "42";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.FailsPEVerify)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73563")]
        public void IsMetadataVirtual_01()
        {
            var src1 = @"
using System.Collections.Generic;
using System.Threading;

public struct S : IAsyncEnumerable<int>
{
    public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken token = default) => throw null;

    void M()
    {
        GetAsyncEnumerator();
    }
}
";

            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80);

            var src2 = @"
using System.Threading.Tasks;

class C
{
    static async Task Main()
    {
        await foreach (var i in new S())
        {
        }
    }
}
";
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net80);
            comp2.VerifyEmitDiagnostics(); // Indirectly calling IsMetadataVirtual on S.GetAsyncEnumerator (a read which causes the lock to be set)
            comp1.VerifyEmitDiagnostics(); // Would call EnsureMetadataVirtual on S.GetAsyncEnumerator and would therefore assert if S was not already ForceCompleted
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73563")]
        public void IsMetadataVirtual_02()
        {
            var src1 = @"
using System;
using System.Threading.Tasks;

public struct S2 : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    void M()
    {
        DisposeAsync();
    }
}
";

            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80);

            var src2 = @"
class C
{
    static async System.Threading.Tasks.Task Main()
    {
        await using (new S2())
        {
        }

        await using (var s = new S2())
        {
        }
    }
}
";
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net80);
            comp2.VerifyEmitDiagnostics(); // Indirectly calling IsMetadataVirtual on S.DisposeAsync (a read which causes the lock to be set)
            comp1.VerifyEmitDiagnostics(); // Would call EnsureMetadataVirtual on S.DisposeAsync and would therefore assert if S was not already ForceCompleted
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68027")]
        public void LambdaWithBindingErrorInYieldReturn()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    static async IAsyncEnumerable<Func<string, Task<string>>> BarAsync()
    {
        yield return async s =>
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        };
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics();

            src = """
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    static async IAsyncEnumerable<Func<string, Task<string>>> BarAsync()
    {
        yield return async s =>
        {
            s // 1
            await Task.CompletedTask;
            throw new NotImplementedException();
        };
    }
}
""";
            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (11,13): error CS0118: 's' is a variable but is used like a type
                //             s // 1
                Diagnostic(ErrorCode.ERR_BadSKknown, "s").WithArguments("s", "variable", "type").WithLocation(11, 13),
                // (12,13): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //             await Task.CompletedTask;
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(12, 13),
                // (12,13): warning CS0168: The variable 'await' is declared but never used
                //             await Task.CompletedTask;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "await").WithArguments("await").WithLocation(12, 13),
                // (12,19): error CS1002: ; expected
                //             await Task.CompletedTask;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "Task").WithLocation(12, 19),
                // (12,19): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //             await Task.CompletedTask;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "Task.CompletedTask").WithLocation(12, 19)
            );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var s = GetSyntax<IdentifierNameSyntax>(tree, "s");
            Assert.Null(model.GetSymbolInfo(s).Symbol);
            Assert.Equal(new[] { "System.String s" }, model.GetSymbolInfo(s).CandidateSymbols.ToTestDisplayStrings());
        }

        [Fact]
        public void LambdaWithBindingErrorInReturn()
        {
            var src = """
using System;
using System.Threading.Tasks;

class C
{
    static async Task<Func<string, Task<string>>> BarAsync()
    {
        return async s =>
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        };
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics();

            src = """
using System;
using System.Threading.Tasks;

class C
{
    static async Task<Func<string, Task<string>>> BarAsync()
    {
        return async s =>
        {
            s // 1
            await Task.CompletedTask;
            throw new NotImplementedException();
        };
    }
}
""";
            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (10,13): error CS0118: 's' is a variable but is used like a type
                //             s // 1
                Diagnostic(ErrorCode.ERR_BadSKknown, "s").WithArguments("s", "variable", "type").WithLocation(10, 13),
                // (11,13): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //             await Task.CompletedTask;
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(11, 13),
                // (11,13): warning CS0168: The variable 'await' is declared but never used
                //             await Task.CompletedTask;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "await").WithArguments("await").WithLocation(11, 13),
                // (11,19): error CS1002: ; expected
                //             await Task.CompletedTask;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "Task").WithLocation(11, 19),
                // (11,19): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //             await Task.CompletedTask;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "Task.CompletedTask").WithLocation(11, 19)
            );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var s = GetSyntax<IdentifierNameSyntax>(tree, "s");
            Assert.Null(model.GetSymbolInfo(s).Symbol);
            Assert.Equal(new[] { "System.String s" }, model.GetSymbolInfo(s).CandidateSymbols.ToTestDisplayStrings());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74013")]
        public void ClearCurrentWhenAwaiting_ReferenceType()
        {
            var src = """
using System.Threading.Tasks;
using System.Collections.Generic;

var tcs = new TaskCompletionSource();
var enumerable = C.M(tcs.Task);
var enumerator = enumerable.GetAsyncEnumerator();
if (!await enumerator.MoveNextAsync())
    throw null;

System.Console.Write(enumerator.Current);

var promise = enumerator.MoveNextAsync();
System.Console.Write(enumerator.Current is null);

tcs.SetResult();

if (!await promise)
    throw null;

System.Console.Write(enumerator.Current);

public class C
{
    public static async IAsyncEnumerable<object> M(Task t)
    {
        object o = "first ";
        yield return o;
        await t;
        yield return " second";
    }
}
""";
            var expectedOutput = "first True second";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            var verifier = CompileAndVerify(comp,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """"
{
  // Code size      349 (0x15d)
  .maxstack  3
  .locals init (int V_0,
                object V_1, //o
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C.<M>d__0 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -5
    IL_000a:  sub
    IL_000b:  switch    (
        IL_00ec,
        IL_005a,
        IL_0028,
        IL_0028,
        IL_0028,
        IL_00b2)
    IL_0028:  ldarg.0
    IL_0029:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_002e:  brfalse.s  IL_0035
    IL_0030:  leave      IL_0129
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.m1
    IL_0037:  dup
    IL_0038:  stloc.0
    IL_0039:  stfld      "int C.<M>d__0.<>1__state"
    IL_003e:  ldstr      "first "
    IL_0043:  stloc.1
    IL_0044:  ldarg.0
    IL_0045:  ldloc.1
    IL_0046:  stfld      "object C.<M>d__0.<>2__current"
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.s   -4
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "int C.<M>d__0.<>1__state"
    IL_0055:  leave      IL_0150
    IL_005a:  ldarg.0
    IL_005b:  ldc.i4.m1
    IL_005c:  dup
    IL_005d:  stloc.0
    IL_005e:  stfld      "int C.<M>d__0.<>1__state"
    IL_0063:  ldarg.0
    IL_0064:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_0069:  brfalse.s  IL_0070
    IL_006b:  leave      IL_0129
    IL_0070:  ldarg.0
    IL_0071:  ldnull
    IL_0072:  stfld      "object C.<M>d__0.<>2__current"
    IL_0077:  ldarg.0
    IL_0078:  ldfld      "System.Threading.Tasks.Task C.<M>d__0.t"
    IL_007d:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
    IL_0082:  stloc.2
    IL_0083:  ldloca.s   V_2
    IL_0085:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
    IL_008a:  brtrue.s   IL_00ce
    IL_008c:  ldarg.0
    IL_008d:  ldc.i4.0
    IL_008e:  dup
    IL_008f:  stloc.0
    IL_0090:  stfld      "int C.<M>d__0.<>1__state"
    IL_0095:  ldarg.0
    IL_0096:  ldloc.2
    IL_0097:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
    IL_009c:  ldarg.0
    IL_009d:  stloc.3
    IL_009e:  ldarg.0
    IL_009f:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
    IL_00a4:  ldloca.s   V_2
    IL_00a6:  ldloca.s   V_3
    IL_00a8:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)"
    IL_00ad:  leave      IL_015c
    IL_00b2:  ldarg.0
    IL_00b3:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
    IL_00b8:  stloc.2
    IL_00b9:  ldarg.0
    IL_00ba:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
    IL_00bf:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
    IL_00c5:  ldarg.0
    IL_00c6:  ldc.i4.m1
    IL_00c7:  dup
    IL_00c8:  stloc.0
    IL_00c9:  stfld      "int C.<M>d__0.<>1__state"
    IL_00ce:  ldloca.s   V_2
    IL_00d0:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
    IL_00d5:  ldarg.0
    IL_00d6:  ldstr      " second"
    IL_00db:  stfld      "object C.<M>d__0.<>2__current"
    IL_00e0:  ldarg.0
    IL_00e1:  ldc.i4.s   -5
    IL_00e3:  dup
    IL_00e4:  stloc.0
    IL_00e5:  stfld      "int C.<M>d__0.<>1__state"
    IL_00ea:  leave.s    IL_0150
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.m1
    IL_00ee:  dup
    IL_00ef:  stloc.0
    IL_00f0:  stfld      "int C.<M>d__0.<>1__state"
    IL_00f5:  ldarg.0
    IL_00f6:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_00fb:  pop
    IL_00fc:  leave.s    IL_0129
  }
  catch System.Exception
  {
    IL_00fe:  stloc.s    V_4
    IL_0100:  ldarg.0
    IL_0101:  ldc.i4.s   -2
    IL_0103:  stfld      "int C.<M>d__0.<>1__state"
    IL_0108:  ldarg.0
    IL_0109:  ldnull
    IL_010a:  stfld      "object C.<M>d__0.<>2__current"
    IL_010f:  ldarg.0
    IL_0110:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
    IL_0115:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
    IL_011a:  ldarg.0
    IL_011b:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
    IL_0120:  ldloc.s    V_4
    IL_0122:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
    IL_0127:  leave.s    IL_015c
  }
  IL_0129:  ldarg.0
  IL_012a:  ldc.i4.s   -2
  IL_012c:  stfld      "int C.<M>d__0.<>1__state"
  IL_0131:  ldarg.0
  IL_0132:  ldnull
  IL_0133:  stfld      "object C.<M>d__0.<>2__current"
  IL_0138:  ldarg.0
  IL_0139:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
  IL_013e:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
  IL_0143:  ldarg.0
  IL_0144:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
  IL_0149:  ldc.i4.0
  IL_014a:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_014f:  ret
  IL_0150:  ldarg.0
  IL_0151:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
  IL_0156:  ldc.i4.1
  IL_0157:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_015c:  ret
}
"""");

            comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74013")]
        public void ClearCurrentWhenAwaiting_ManagedType()
        {
            var src = """
using System.Threading.Tasks;
using System.Collections.Generic;

var tcs = new TaskCompletionSource();
var enumerable = C.M(tcs.Task);
var enumerator = enumerable.GetAsyncEnumerator();
if (!await enumerator.MoveNextAsync())
    throw null;

System.Console.Write(enumerator.Current.field);

var promise = enumerator.MoveNextAsync();
System.Console.Write(enumerator.Current.field is null);

tcs.SetResult();

if (!await promise)
    throw null;

System.Console.Write(enumerator.Current.field);

public struct S
{
    public object field;
}

public class C
{
    public static async IAsyncEnumerable<S> M(Task t)
    {
        object o = "first ";
        yield return new S { field = o };
        await t;
        yield return new S { field = " second" };
    }
}
""";
            var expectedOutput = "first True second";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            var verifier = CompileAndVerify(comp,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74013")]
        public void ClearCurrentWhenAwaiting_ValueType()
        {
            var src = """
using System.Threading.Tasks;
using System.Collections.Generic;

var tcs = new TaskCompletionSource();
var enumerable = C.M(tcs.Task);
var enumerator = enumerable.GetAsyncEnumerator();
if (!await enumerator.MoveNextAsync())
    throw null;

System.Console.Write(enumerator.Current);

var promise = enumerator.MoveNextAsync();
System.Console.Write(enumerator.Current);

tcs.SetResult();

if (!await promise)
    throw null;

System.Console.Write(enumerator.Current);

public class C
{
    public static async IAsyncEnumerable<int> M(Task t)
    {
        yield return 42;
        await t;
        yield return 43;
    }
}
""";
            var expectedOutput = "424243";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            var verifier = CompileAndVerify(comp,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74013")]
        public void ClearCurrentWhenAwaiting_UnmanagedType()
        {
            var src = """
using System.Threading.Tasks;
using System.Collections.Generic;

var tcs = new TaskCompletionSource();
var enumerable = C.M(tcs.Task);
var enumerator = enumerable.GetAsyncEnumerator();
if (!await enumerator.MoveNextAsync())
    throw null;

System.Console.Write(enumerator.Current.field);

var promise = enumerator.MoveNextAsync();
System.Console.Write(enumerator.Current.field);

tcs.SetResult();

if (!await promise)
    throw null;

System.Console.Write(enumerator.Current.field);

public struct S
{
    public int field;
}
public class C
{
    public static async IAsyncEnumerable<S> M(Task t)
    {
        yield return new S { field = 42 };
        await t;
        yield return new S { field = 43 };
    }
}
""";
            var expectedOutput = "424243";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            var verifier = CompileAndVerify(comp,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74013")]
        public void ClearCurrentWhenAwaiting_GenericType()
        {
            var src = """
using System.Threading.Tasks;
using System.Collections.Generic;

var tcs = new TaskCompletionSource();
var enumerable = C.M<string>(tcs.Task, "first ", " second");
var enumerator = enumerable.GetAsyncEnumerator();
if (!await enumerator.MoveNextAsync())
    throw null;

System.Console.Write(enumerator.Current);

var promise = enumerator.MoveNextAsync();
System.Console.Write(enumerator.Current is null);

tcs.SetResult();

if (!await promise)
    throw null;

System.Console.Write(enumerator.Current);

public class C
{
    public static async IAsyncEnumerable<T> M<T>(Task t, T t1, T t2)
    {
        yield return t1;
        await t;
        yield return t2;
    }
}
""";
            var expectedOutput = "first True second";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            var verifier = CompileAndVerify(comp,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            verifier.VerifyIL("C.<M>d__0<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """"
{
  // Code size      362 (0x16a)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<M>d__0<T> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0<T>.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -5
    IL_000a:  sub
    IL_000b:  switch    (
        IL_00f1,
        IL_0059,
        IL_0028,
        IL_0028,
        IL_0028,
        IL_00b6)
    IL_0028:  ldarg.0
    IL_0029:  ldfld      "bool C.<M>d__0<T>.<>w__disposeMode"
    IL_002e:  brfalse.s  IL_0035
    IL_0030:  leave      IL_0131
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.m1
    IL_0037:  dup
    IL_0038:  stloc.0
    IL_0039:  stfld      "int C.<M>d__0<T>.<>1__state"
    IL_003e:  ldarg.0
    IL_003f:  ldarg.0
    IL_0040:  ldfld      "T C.<M>d__0<T>.t1"
    IL_0045:  stfld      "T C.<M>d__0<T>.<>2__current"
    IL_004a:  ldarg.0
    IL_004b:  ldc.i4.s   -4
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "int C.<M>d__0<T>.<>1__state"
    IL_0054:  leave      IL_015d
    IL_0059:  ldarg.0
    IL_005a:  ldc.i4.m1
    IL_005b:  dup
    IL_005c:  stloc.0
    IL_005d:  stfld      "int C.<M>d__0<T>.<>1__state"
    IL_0062:  ldarg.0
    IL_0063:  ldfld      "bool C.<M>d__0<T>.<>w__disposeMode"
    IL_0068:  brfalse.s  IL_006f
    IL_006a:  leave      IL_0131
    IL_006f:  ldarg.0
    IL_0070:  ldflda     "T C.<M>d__0<T>.<>2__current"
    IL_0075:  initobj    "T"
    IL_007b:  ldarg.0
    IL_007c:  ldfld      "System.Threading.Tasks.Task C.<M>d__0<T>.t"
    IL_0081:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
    IL_0086:  stloc.1
    IL_0087:  ldloca.s   V_1
    IL_0089:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
    IL_008e:  brtrue.s   IL_00d2
    IL_0090:  ldarg.0
    IL_0091:  ldc.i4.0
    IL_0092:  dup
    IL_0093:  stloc.0
    IL_0094:  stfld      "int C.<M>d__0<T>.<>1__state"
    IL_0099:  ldarg.0
    IL_009a:  ldloc.1
    IL_009b:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0<T>.<>u__1"
    IL_00a0:  ldarg.0
    IL_00a1:  stloc.2
    IL_00a2:  ldarg.0
    IL_00a3:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0<T>.<>t__builder"
    IL_00a8:  ldloca.s   V_1
    IL_00aa:  ldloca.s   V_2
    IL_00ac:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0<T>>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0<T>)"
    IL_00b1:  leave      IL_0169
    IL_00b6:  ldarg.0
    IL_00b7:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0<T>.<>u__1"
    IL_00bc:  stloc.1
    IL_00bd:  ldarg.0
    IL_00be:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0<T>.<>u__1"
    IL_00c3:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
    IL_00c9:  ldarg.0
    IL_00ca:  ldc.i4.m1
    IL_00cb:  dup
    IL_00cc:  stloc.0
    IL_00cd:  stfld      "int C.<M>d__0<T>.<>1__state"
    IL_00d2:  ldloca.s   V_1
    IL_00d4:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
    IL_00d9:  ldarg.0
    IL_00da:  ldarg.0
    IL_00db:  ldfld      "T C.<M>d__0<T>.t2"
    IL_00e0:  stfld      "T C.<M>d__0<T>.<>2__current"
    IL_00e5:  ldarg.0
    IL_00e6:  ldc.i4.s   -5
    IL_00e8:  dup
    IL_00e9:  stloc.0
    IL_00ea:  stfld      "int C.<M>d__0<T>.<>1__state"
    IL_00ef:  leave.s    IL_015d
    IL_00f1:  ldarg.0
    IL_00f2:  ldc.i4.m1
    IL_00f3:  dup
    IL_00f4:  stloc.0
    IL_00f5:  stfld      "int C.<M>d__0<T>.<>1__state"
    IL_00fa:  ldarg.0
    IL_00fb:  ldfld      "bool C.<M>d__0<T>.<>w__disposeMode"
    IL_0100:  pop
    IL_0101:  leave.s    IL_0131
  }
  catch System.Exception
  {
    IL_0103:  stloc.3
    IL_0104:  ldarg.0
    IL_0105:  ldc.i4.s   -2
    IL_0107:  stfld      "int C.<M>d__0<T>.<>1__state"
    IL_010c:  ldarg.0
    IL_010d:  ldflda     "T C.<M>d__0<T>.<>2__current"
    IL_0112:  initobj    "T"
    IL_0118:  ldarg.0
    IL_0119:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0<T>.<>t__builder"
    IL_011e:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
    IL_0123:  ldarg.0
    IL_0124:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0<T>.<>v__promiseOfValueOrEnd"
    IL_0129:  ldloc.3
    IL_012a:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
    IL_012f:  leave.s    IL_0169
  }
  IL_0131:  ldarg.0
  IL_0132:  ldc.i4.s   -2
  IL_0134:  stfld      "int C.<M>d__0<T>.<>1__state"
  IL_0139:  ldarg.0
  IL_013a:  ldflda     "T C.<M>d__0<T>.<>2__current"
  IL_013f:  initobj    "T"
  IL_0145:  ldarg.0
  IL_0146:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0<T>.<>t__builder"
  IL_014b:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
  IL_0150:  ldarg.0
  IL_0151:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0<T>.<>v__promiseOfValueOrEnd"
  IL_0156:  ldc.i4.0
  IL_0157:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_015c:  ret
  IL_015d:  ldarg.0
  IL_015e:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0<T>.<>v__promiseOfValueOrEnd"
  IL_0163:  ldc.i4.1
  IL_0164:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_0169:  ret
}
"""");

            comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();

            verifier.VerifyIL("C.<M>d__0<T>.System.Collections.Generic.IAsyncEnumerator<T>.MoveNextAsync()", """
{
  // Code size       99 (0x63)
  .maxstack  2
  .locals init (C.<M>d__0<T> V_0,
                short V_1,
                System.Threading.Tasks.ValueTask<bool> V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0<T>.<>1__state"
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0014
  IL_000a:  ldloca.s   V_2
  IL_000c:  initobj    "System.Threading.Tasks.ValueTask<bool>"
  IL_0012:  ldloc.2
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0<T>.<>v__promiseOfValueOrEnd"
  IL_001a:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.Reset()"
  IL_001f:  ldarg.0
  IL_0020:  stloc.0
  IL_0021:  ldarg.0
  IL_0022:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0<T>.<>t__builder"
  IL_0027:  ldloca.s   V_0
  IL_0029:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.MoveNext<C.<M>d__0<T>>(ref C.<M>d__0<T>)"
  IL_002e:  ldarg.0
  IL_002f:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0<T>.<>v__promiseOfValueOrEnd"
  IL_0034:  call       "short System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.Version.get"
  IL_0039:  stloc.1
  IL_003a:  ldarg.0
  IL_003b:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0<T>.<>v__promiseOfValueOrEnd"
  IL_0040:  ldloc.1
  IL_0041:  call       "System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.GetStatus(short)"
  IL_0046:  ldc.i4.1
  IL_0047:  bne.un.s   IL_005b
  IL_0049:  ldarg.0
  IL_004a:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0<T>.<>v__promiseOfValueOrEnd"
  IL_004f:  ldloc.1
  IL_0050:  call       "bool System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.GetResult(short)"
  IL_0055:  newobj     "System.Threading.Tasks.ValueTask<bool>..ctor(bool)"
  IL_005a:  ret
  IL_005b:  ldarg.0
  IL_005c:  ldloc.1
  IL_005d:  newobj     "System.Threading.Tasks.ValueTask<bool>..ctor(System.Threading.Tasks.Sources.IValueTaskSource<bool>, short)"
  IL_0062:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74013")]
        public void ClearCurrentWhenAwaiting_StructOfGenericType()
        {
            var src = """
using System.Threading.Tasks;
using System.Collections.Generic;

var tcs = new TaskCompletionSource();
var enumerable = C.M<string>(tcs.Task, "first ", " second");
var enumerator = enumerable.GetAsyncEnumerator();
if (!await enumerator.MoveNextAsync())
    throw null;

System.Console.Write(enumerator.Current.field);

var promise = enumerator.MoveNextAsync();
System.Console.Write(enumerator.Current.field is null);

tcs.SetResult();

if (!await promise)
    throw null;

System.Console.Write(enumerator.Current.field);

public struct S<T>
{
    public T field;
}

public class C
{
    public static async IAsyncEnumerable<S<T>> M<T>(Task t, T t1, T t2)
    {
        yield return new S<T> { field = t1 };
        await t;
        yield return new S<T> { field = t2 };
    }
}
""";
            var expectedOutput = "first True second";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            var verifier = CompileAndVerify(comp,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74013")]
        public void ClearCurrentWhenAwaiting_StructOfUnmanagedGenericType()
        {
            var src = """
using System.Threading.Tasks;
using System.Collections.Generic;

var tcs = new TaskCompletionSource();
var enumerable = C.M<int>(tcs.Task, 42, 43);
var enumerator = enumerable.GetAsyncEnumerator();
if (!await enumerator.MoveNextAsync())
    throw null;

System.Console.Write(enumerator.Current.field);

var promise = enumerator.MoveNextAsync();
System.Console.Write(enumerator.Current.field is 0);

tcs.SetResult();

if (!await promise)
    throw null;

System.Console.Write(enumerator.Current.field);

public struct S<T> where T : unmanaged // UnmanagedWithGenerics
{
    public T field;
}

public class C
{
    public static async IAsyncEnumerable<S<T>> M<T>(Task t, T t1, T t2) where T : unmanaged
    {
        yield return new S<T> { field = t1 };
        await t;
        yield return new S<T> { field = t2 };
    }
}
""";
            var expectedOutput = "42False43";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(src);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                 .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_IntLocal()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
await foreach (int value in values) { }
System.Console.Write(((int)values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce()
    {
        int values2 = 42;
        await System.Threading.Tasks.Task.CompletedTask;
        yield return values2;
    }
}
""";
            var expectedOutput = "42";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80).VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();

            verifier.VerifyIL("C.<Produce>d__0.System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()", """
{
  // Code size      145 (0x91)
  .maxstack  3
  .locals init (int V_0,
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<Produce>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  beq.s      IL_0050
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.s   -3
    IL_000f:  pop
    IL_0010:  pop
    IL_0011:  ldarg.0
    IL_0012:  ldfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_0017:  brfalse.s  IL_001b
    IL_0019:  leave.s    IL_007e
    IL_001b:  ldarg.0
    IL_001c:  ldc.i4.m1
    IL_001d:  dup
    IL_001e:  stloc.0
    IL_001f:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_0024:  ldarg.0
    IL_0025:  ldc.i4.s   42
    IL_0027:  stfld      "int C.<Produce>d__0.<values2>5__2"
    IL_002c:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get"
    IL_0031:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
    IL_0036:  ldarg.0
    IL_0037:  ldarg.0
    IL_0038:  ldfld      "int C.<Produce>d__0.<values2>5__2"
    IL_003d:  stfld      "int C.<Produce>d__0.<>2__current"
    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.s   -4
    IL_0045:  dup
    IL_0046:  stloc.0
    IL_0047:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_004c:  ldc.i4.1
    IL_004d:  stloc.1
    IL_004e:  leave.s    IL_008f
    IL_0050:  ldarg.0
    IL_0051:  ldc.i4.m1
    IL_0052:  dup
    IL_0053:  stloc.0
    IL_0054:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_0059:  ldarg.0
    IL_005a:  ldfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_005f:  brfalse.s  IL_0063
    IL_0061:  leave.s    IL_007e
    IL_0063:  ldarg.0
    IL_0064:  ldc.i4.1
    IL_0065:  stfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_006a:  leave.s    IL_007e
  }
  catch System.Exception
  {
    IL_006c:  pop
    IL_006d:  ldarg.0
    IL_006e:  ldc.i4.s   -2
    IL_0070:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_0075:  ldarg.0
    IL_0076:  ldc.i4.0
    IL_0077:  stfld      "int C.<Produce>d__0.<>2__current"
    IL_007c:  rethrow
  }
  IL_007e:  ldarg.0
  IL_007f:  ldc.i4.s   -2
  IL_0081:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_0086:  ldarg.0
  IL_0087:  ldc.i4.0
  IL_0088:  stfld      "int C.<Produce>d__0.<>2__current"
  IL_008d:  ldc.i4.0
  IL_008e:  stloc.1
  IL_008f:  ldloc.1
  IL_0090:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringLocal()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
await foreach (int value in values)
{
    System.Console.Write(((string)values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
}
System.Console.Write(((string)values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce()
    {
        string values2 = "value ";
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 1;
        System.Console.Write(values2);
    }
}
""";
            var expectedOutput = "value value True";

            var verifier = CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
 {
  // Code size      320 (0x140)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<Produce>d__0 V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<Produce>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  sub
    IL_000b:  switch    (
        IL_00b5,
        IL_0024,
        IL_0024,
        IL_0024,
        IL_007f)
    IL_0024:  ldarg.0
    IL_0025:  ldfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_002a:  brfalse.s  IL_0031
    IL_002c:  leave      IL_0105
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.m1
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_003a:  ldarg.0
    IL_003b:  ldstr      "value "
    IL_0040:  stfld      "string C.<Produce>d__0.<values2>5__2"
    IL_0045:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get"
    IL_004a:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
    IL_004f:  stloc.1
    IL_0050:  ldloca.s   V_1
    IL_0052:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
    IL_0057:  brtrue.s   IL_009b
    IL_0059:  ldarg.0
    IL_005a:  ldc.i4.0
    IL_005b:  dup
    IL_005c:  stloc.0
    IL_005d:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_0062:  ldarg.0
    IL_0063:  ldloc.1
    IL_0064:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<Produce>d__0.<>u__1"
    IL_0069:  ldarg.0
    IL_006a:  stloc.2
    IL_006b:  ldarg.0
    IL_006c:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<Produce>d__0.<>t__builder"
    IL_0071:  ldloca.s   V_1
    IL_0073:  ldloca.s   V_2
    IL_0075:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<Produce>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<Produce>d__0)"
    IL_007a:  leave      IL_013f
    IL_007f:  ldarg.0
    IL_0080:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<Produce>d__0.<>u__1"
    IL_0085:  stloc.1
    IL_0086:  ldarg.0
    IL_0087:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<Produce>d__0.<>u__1"
    IL_008c:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
    IL_0092:  ldarg.0
    IL_0093:  ldc.i4.m1
    IL_0094:  dup
    IL_0095:  stloc.0
    IL_0096:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_009b:  ldloca.s   V_1
    IL_009d:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
    IL_00a2:  ldarg.0
    IL_00a3:  ldc.i4.1
    IL_00a4:  stfld      "int C.<Produce>d__0.<>2__current"
    IL_00a9:  ldarg.0
    IL_00aa:  ldc.i4.s   -4
    IL_00ac:  dup
    IL_00ad:  stloc.0
    IL_00ae:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_00b3:  leave.s    IL_0133
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.m1
    IL_00b7:  dup
    IL_00b8:  stloc.0
    IL_00b9:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_00be:  ldarg.0
    IL_00bf:  ldfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_00c4:  brfalse.s  IL_00c8
    IL_00c6:  leave.s    IL_0105
    IL_00c8:  ldarg.0
    IL_00c9:  ldfld      "string C.<Produce>d__0.<values2>5__2"
    IL_00ce:  call       "void System.Console.Write(string)"
    IL_00d3:  leave.s    IL_0105
  }
  catch System.Exception
  {
    IL_00d5:  stloc.3
    IL_00d6:  ldarg.0
    IL_00d7:  ldc.i4.s   -2
    IL_00d9:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_00de:  ldarg.0
    IL_00df:  ldnull
    IL_00e0:  stfld      "string C.<Produce>d__0.<values2>5__2"
    IL_00e5:  ldarg.0
    IL_00e6:  ldc.i4.0
    IL_00e7:  stfld      "int C.<Produce>d__0.<>2__current"
    IL_00ec:  ldarg.0
    IL_00ed:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<Produce>d__0.<>t__builder"
    IL_00f2:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
    IL_00f7:  ldarg.0
    IL_00f8:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<Produce>d__0.<>v__promiseOfValueOrEnd"
    IL_00fd:  ldloc.3
    IL_00fe:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
    IL_0103:  leave.s    IL_013f
  }
  IL_0105:  ldarg.0
  IL_0106:  ldc.i4.s   -2
  IL_0108:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_010d:  ldarg.0
  IL_010e:  ldnull
  IL_010f:  stfld      "string C.<Produce>d__0.<values2>5__2"
  IL_0114:  ldarg.0
  IL_0115:  ldc.i4.0
  IL_0116:  stfld      "int C.<Produce>d__0.<>2__current"
  IL_011b:  ldarg.0
  IL_011c:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<Produce>d__0.<>t__builder"
  IL_0121:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
  IL_0126:  ldarg.0
  IL_0127:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<Produce>d__0.<>v__promiseOfValueOrEnd"
  IL_012c:  ldc.i4.0
  IL_012d:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_0132:  ret
  IL_0133:  ldarg.0
  IL_0134:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<Produce>d__0.<>v__promiseOfValueOrEnd"
  IL_0139:  ldc.i4.1
  IL_013a:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_013f:  ret
}
""");
            var comp = CreateRuntimeAsyncCompilation(src);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();

            verifier.VerifyIL("C.<Produce>d__0.System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()", """
{
  // Code size      168 (0xa8)
  .maxstack  3
  .locals init (int V_0,
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<Produce>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  beq.s      IL_004e
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.s   -3
    IL_000f:  pop
    IL_0010:  pop
    IL_0011:  ldarg.0
    IL_0012:  ldfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_0017:  brfalse.s  IL_001b
    IL_0019:  leave.s    IL_008e
    IL_001b:  ldarg.0
    IL_001c:  ldc.i4.m1
    IL_001d:  dup
    IL_001e:  stloc.0
    IL_001f:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_0024:  ldarg.0
    IL_0025:  ldstr      "value "
    IL_002a:  stfld      "string C.<Produce>d__0.<values2>5__2"
    IL_002f:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get"
    IL_0034:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
    IL_0039:  ldarg.0
    IL_003a:  ldc.i4.1
    IL_003b:  stfld      "int C.<Produce>d__0.<>2__current"
    IL_0040:  ldarg.0
    IL_0041:  ldc.i4.s   -4
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_004a:  ldc.i4.1
    IL_004b:  stloc.1
    IL_004c:  leave.s    IL_00a6
    IL_004e:  ldarg.0
    IL_004f:  ldc.i4.m1
    IL_0050:  dup
    IL_0051:  stloc.0
    IL_0052:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_0057:  ldarg.0
    IL_0058:  ldfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_005d:  brfalse.s  IL_0061
    IL_005f:  leave.s    IL_008e
    IL_0061:  ldarg.0
    IL_0062:  ldfld      "string C.<Produce>d__0.<values2>5__2"
    IL_0067:  call       "void System.Console.Write(string)"
    IL_006c:  ldarg.0
    IL_006d:  ldc.i4.1
    IL_006e:  stfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_0073:  leave.s    IL_008e
  }
  catch System.Exception
  {
    IL_0075:  pop
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.s   -2
    IL_0079:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_007e:  ldarg.0
    IL_007f:  ldnull
    IL_0080:  stfld      "string C.<Produce>d__0.<values2>5__2"
    IL_0085:  ldarg.0
    IL_0086:  ldc.i4.0
    IL_0087:  stfld      "int C.<Produce>d__0.<>2__current"
    IL_008c:  rethrow
  }
  IL_008e:  ldarg.0
  IL_008f:  ldc.i4.s   -2
  IL_0091:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_0096:  ldarg.0
  IL_0097:  ldnull
  IL_0098:  stfld      "string C.<Produce>d__0.<values2>5__2"
  IL_009d:  ldarg.0
  IL_009e:  ldc.i4.0
  IL_009f:  stfld      "int C.<Produce>d__0.<>2__current"
  IL_00a4:  ldc.i4.0
  IL_00a5:  stloc.1
  IL_00a6:  ldloc.1
  IL_00a7:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringLocal_YieldBreak()
        {
            string src = """
using System.Reflection;

var values = C.Produce(true);
await foreach (int value in values)
{
    System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
}
System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce(bool b)
    {
        string s = "value ";
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 42;
        System.Console.Write(s);
        if (b) yield break;
        throw null;
    }
}
""";
            var expectedOutput = "value value True";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringLocal_ThrownException()
        {
            string src = """
using System.Reflection;

var values = C.Produce(true);
try
{
    await foreach (int value in values) { }
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce(bool b)
    {
        string s = "value ";
        await System.Threading.Tasks.Task.CompletedTask;
        System.Console.Write(s);
        if (b) throw new System.Exception("exception ");
        yield break;
    }
}
""";
            var expectedOutput = "value exception True";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringLocal_EarlyIterationExit()
        {
            string src = """
using System.Reflection;

var values = C.Produce(true);
await foreach (var value in values)
{
    System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
    break;
}
System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce(bool b)
    {
        string s = "value ";
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 42;
        _ = s.ToString();
    }
}
""";
            var expectedOutput = "value True";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal()
        {
            string src = """
using System.Reflection;

var tcs = new System.Threading.Tasks.TaskCompletionSource();
var values = C.Produce(true, tcs.Task);
var enumerator = values.GetAsyncEnumerator();
assert(await enumerator.MoveNextAsync());
assert(enumerator.Current == 1);
System.Console.Write(((string)values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));

assert(await enumerator.MoveNextAsync());
assert(enumerator.Current == 2);
_ = enumerator.MoveNextAsync();

System.Console.Write(((string)values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

void assert(bool b)
{
    if (!b) throw new System.Exception();
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce(bool b, System.Threading.Tasks.Task task)
    {
        while (b)
        {
            string values2 = "value ";
            yield return 1;
            System.Console.Write(values2);
            b = false;
        }
        yield return 2;
        await task;
        yield return 3;
    }
}
""";
            var expectedOutput = "value value True";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal_YieldBreak()
        {
            string src = """
using System.Reflection;

var values = C.Produce(true);
var enumerator = values.GetAsyncEnumerator();
assert(await enumerator.MoveNextAsync());
assert(enumerator.Current == 1);
System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
assert(!(await enumerator.MoveNextAsync()));

System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

void assert(bool b)
{
    if (!b) throw new System.Exception();
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce(bool b)
    {
        while (b)
        {
            string s = "value ";
            yield return 1;
            System.Console.Write(s);
            await System.Threading.Tasks.Task.CompletedTask;
            if (b) yield break;
        }
        throw null;
    }
}
""";
            var expectedOutput = "value value True";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal_ThrownException()
        {
            string src = """
using System.Reflection;

var values = C.Produce(true);
var enumerator = values.GetAsyncEnumerator();
assert(await enumerator.MoveNextAsync());
assert(enumerator.Current == 1);
try
{
    assert(!(await enumerator.MoveNextAsync()));
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}
await enumerator.DisposeAsync();

System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

void assert(bool b)
{
    if (!b) throw new System.Exception();
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce(bool b)
    {
        while (b)
        {
            string s = "value ";
            yield return 1;
            System.Console.Write(s);
            await System.Threading.Tasks.Task.CompletedTask;
            if (b) throw new System.Exception("exception ");
        }
        throw null;
    }
}
""";
            var expectedOutput = "value exception True";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal_EarlyIterationExit()
        {
            string src = """
using System.Reflection;

var values = C.Produce(true);
await foreach (var value in values)
{
    System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
    break;
}
System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce(bool b)
    {
        while (b)
        {
            string s = "value ";
            yield return 1;
            System.Console.Write(s);
            await System.Threading.Tasks.Task.CompletedTask;
            throw null;
        }
        throw null;
    }
}
""";
            var expectedOutput = "value True";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedLocalWithStructFromAnotherCompilation()
        {
            var libSrc = """
public struct S
{
    public int field;
    public override string ToString() => field.ToString();
}
""";
            var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net80);
            string src = """
using System.Reflection;

var tcs = new System.Threading.Tasks.TaskCompletionSource();
var values = C.Produce(true, tcs.Task);
var enumerator = values.GetAsyncEnumerator();
assert(await enumerator.MoveNextAsync());
assert(enumerator.Current == 1);
assert(await enumerator.MoveNextAsync());
assert(enumerator.Current == 2);
_ = enumerator.MoveNextAsync();

System.Console.Write(((S)values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));

void assert(bool b)
{
    if (!b) throw new System.Exception();
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce(bool b, System.Threading.Tasks.Task task)
    {
        while (b)
        {
            S values2 = new S { field = 42 };
            yield return 1;
            System.Console.Write(values2);
            b = false;
        }
        yield return 2;
        await task;
        yield return 3;
    }
}
""";
            var expectedOutput = "4242";

            var verifier = CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), references: [libComp.EmitToImageReference()],
                verify: Verification.Skipped, targetFramework: TargetFramework.Net80).VerifyDiagnostics();

            verifier.VerifyIL("C.<Produce>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      437 (0x1b5)
  .maxstack  3
  .locals init (int V_0,
                S V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C.<Produce>d__0 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<Produce>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -6
    IL_000a:  sub
    IL_000b:  switch    (
        IL_0144,
        IL_00bd,
        IL_0072,
        IL_002c,
        IL_002c,
        IL_002c,
        IL_010e)
    IL_002c:  ldarg.0
    IL_002d:  ldfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_0032:  brfalse.s  IL_0039
    IL_0034:  leave      IL_0181
    IL_0039:  ldarg.0
    IL_003a:  ldc.i4.m1
    IL_003b:  dup
    IL_003c:  stloc.0
    IL_003d:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_0042:  br.s       IL_009f
    IL_0044:  ldarg.0
    IL_0045:  ldloca.s   V_1
    IL_0047:  initobj    "S"
    IL_004d:  ldloca.s   V_1
    IL_004f:  ldc.i4.s   42
    IL_0051:  stfld      "int S.field"
    IL_0056:  ldloc.1
    IL_0057:  stfld      "S C.<Produce>d__0.<values2>5__2"
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.1
    IL_005e:  stfld      "int C.<Produce>d__0.<>2__current"
    IL_0063:  ldarg.0
    IL_0064:  ldc.i4.s   -4
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_006d:  leave      IL_01a8
    IL_0072:  ldarg.0
    IL_0073:  ldc.i4.m1
    IL_0074:  dup
    IL_0075:  stloc.0
    IL_0076:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_007b:  ldarg.0
    IL_007c:  ldfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_0081:  brfalse.s  IL_0088
    IL_0083:  leave      IL_0181
    IL_0088:  ldarg.0
    IL_0089:  ldfld      "S C.<Produce>d__0.<values2>5__2"
    IL_008e:  box        "S"
    IL_0093:  call       "void System.Console.Write(object)"
    IL_0098:  ldarg.0
    IL_0099:  ldc.i4.0
    IL_009a:  stfld      "bool C.<Produce>d__0.b"
    IL_009f:  ldarg.0
    IL_00a0:  ldfld      "bool C.<Produce>d__0.b"
    IL_00a5:  brtrue.s   IL_0044
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.2
    IL_00a9:  stfld      "int C.<Produce>d__0.<>2__current"
    IL_00ae:  ldarg.0
    IL_00af:  ldc.i4.s   -5
    IL_00b1:  dup
    IL_00b2:  stloc.0
    IL_00b3:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_00b8:  leave      IL_01a8
    IL_00bd:  ldarg.0
    IL_00be:  ldc.i4.m1
    IL_00bf:  dup
    IL_00c0:  stloc.0
    IL_00c1:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_00c6:  ldarg.0
    IL_00c7:  ldfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_00cc:  brfalse.s  IL_00d3
    IL_00ce:  leave      IL_0181
    IL_00d3:  ldarg.0
    IL_00d4:  ldfld      "System.Threading.Tasks.Task C.<Produce>d__0.task"
    IL_00d9:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
    IL_00de:  stloc.2
    IL_00df:  ldloca.s   V_2
    IL_00e1:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
    IL_00e6:  brtrue.s   IL_012a
    IL_00e8:  ldarg.0
    IL_00e9:  ldc.i4.0
    IL_00ea:  dup
    IL_00eb:  stloc.0
    IL_00ec:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_00f1:  ldarg.0
    IL_00f2:  ldloc.2
    IL_00f3:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<Produce>d__0.<>u__1"
    IL_00f8:  ldarg.0
    IL_00f9:  stloc.3
    IL_00fa:  ldarg.0
    IL_00fb:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<Produce>d__0.<>t__builder"
    IL_0100:  ldloca.s   V_2
    IL_0102:  ldloca.s   V_3
    IL_0104:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<Produce>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<Produce>d__0)"
    IL_0109:  leave      IL_01b4
    IL_010e:  ldarg.0
    IL_010f:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<Produce>d__0.<>u__1"
    IL_0114:  stloc.2
    IL_0115:  ldarg.0
    IL_0116:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<Produce>d__0.<>u__1"
    IL_011b:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
    IL_0121:  ldarg.0
    IL_0122:  ldc.i4.m1
    IL_0123:  dup
    IL_0124:  stloc.0
    IL_0125:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_012a:  ldloca.s   V_2
    IL_012c:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
    IL_0131:  ldarg.0
    IL_0132:  ldc.i4.3
    IL_0133:  stfld      "int C.<Produce>d__0.<>2__current"
    IL_0138:  ldarg.0
    IL_0139:  ldc.i4.s   -6
    IL_013b:  dup
    IL_013c:  stloc.0
    IL_013d:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_0142:  leave.s    IL_01a8
    IL_0144:  ldarg.0
    IL_0145:  ldc.i4.m1
    IL_0146:  dup
    IL_0147:  stloc.0
    IL_0148:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_014d:  ldarg.0
    IL_014e:  ldfld      "bool C.<Produce>d__0.<>w__disposeMode"
    IL_0153:  pop
    IL_0154:  leave.s    IL_0181
  }
  catch System.Exception
  {
    IL_0156:  stloc.s    V_4
    IL_0158:  ldarg.0
    IL_0159:  ldc.i4.s   -2
    IL_015b:  stfld      "int C.<Produce>d__0.<>1__state"
    IL_0160:  ldarg.0
    IL_0161:  ldc.i4.0
    IL_0162:  stfld      "int C.<Produce>d__0.<>2__current"
    IL_0167:  ldarg.0
    IL_0168:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<Produce>d__0.<>t__builder"
    IL_016d:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
    IL_0172:  ldarg.0
    IL_0173:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<Produce>d__0.<>v__promiseOfValueOrEnd"
    IL_0178:  ldloc.s    V_4
    IL_017a:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
    IL_017f:  leave.s    IL_01b4
  }
  IL_0181:  ldarg.0
  IL_0182:  ldc.i4.s   -2
  IL_0184:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_0189:  ldarg.0
  IL_018a:  ldc.i4.0
  IL_018b:  stfld      "int C.<Produce>d__0.<>2__current"
  IL_0190:  ldarg.0
  IL_0191:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<Produce>d__0.<>t__builder"
  IL_0196:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
  IL_019b:  ldarg.0
  IL_019c:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<Produce>d__0.<>v__promiseOfValueOrEnd"
  IL_01a1:  ldc.i4.0
  IL_01a2:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_01a7:  ret
  IL_01a8:  ldarg.0
  IL_01a9:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<Produce>d__0.<>v__promiseOfValueOrEnd"
  IL_01ae:  ldc.i4.1
  IL_01af:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_01b4:  ret
}
""");
            libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net100);
            var comp = CreateRuntimeAsyncCompilation(src, references: [libComp.EmitToImageReference()]);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal_InTryFinally_WithThrow_EarlyIterationExit()
        {
            string src = """
using System.Reflection;

var values = C.Produce(true);
try
{
    await foreach (var value in values) { break; } // we interrupt the iteration early
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce(bool b)
    {
        try
        {
            string s = "value ";
            await System.Threading.Tasks.Task.CompletedTask;
            yield return 42;
            s.ToString();
            throw null;
        }
        finally
        {
            throw new System.Exception("exception ");
        }
    }
}
""";
            var expectedOutput = "exception True";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringLocal_IAsyncEnumerator()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
assert(await values.MoveNextAsync());
assert(values.Current == 1);
System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
assert(!(await values.MoveNextAsync()));
await values.DisposeAsync();

System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

static void assert(bool b) { if (!b) throw new System.Exception(); }

class C
{
    public static async System.Collections.Generic.IAsyncEnumerator<int> Produce()
    {
        string s = "value ";
        await System.Threading.Tasks.Task.CompletedTask;
        yield return 1;
        System.Console.Write(s);
    }
}
""";
            var expectedOutput = "value value True";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NotCleanedTooSoon()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
await foreach (int i in values)
{
    System.Console.Write((values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
    break;
}
System.Console.Write((values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce()
    {
        try
        {
            string s = "value ";
            try
            {
                await System.Threading.Tasks.Task.CompletedTask;
                yield return 42;
            }
            finally
            {
                System.Console.Write(s);
            }
        }
        finally
        {
            System.Console.Write("outer ");
        }
    }
}
""";
            var expectedOutput = "value value outer True";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_HoistedFromRefExpression()
        {
            string src = """
using System.Reflection;

var c = new C();
var values = Program.Produce(c);
await foreach (int i in values)
{
    System.Console.Write((values.GetType().GetField("<>7__wrap3", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);
    System.Console.Write($" {i} ");
}
System.Console.Write((values.GetType().GetField("<>7__wrap3", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

partial class Program
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce(C x)
    {
        int i = 0;
        foreach (var y in x.F)
        {
            await System.Threading.Tasks.Task.CompletedTask;
            yield return i++;
        }
    }
}

class C
{
    public Buffer2<int> F = default;
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    private T _element0;
}
""";
            var expectedOutput = "False 0 False 1 True";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_HoistedFromRefExpression_Debug()
        {
            string src = """
using System.Reflection;

var c = new C();
var values = Program.Produce(c);
await foreach (int i in values)
{
    System.Console.Write((values.GetType().GetField("<>s__4", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);
    System.Console.Write($" {i} ");
}
System.Console.Write((values.GetType().GetField("<>s__4", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

partial class Program
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> Produce(C x)
    {
        int i = 0;
        foreach (var y in x.F)
        {
            await System.Threading.Tasks.Task.CompletedTask;
            yield return i++;
        }
    }
}

class C
{
    public Buffer2<int> F = default;
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    private T _element0;
}
""";
            var expectedOutput = "False 0 False 1 True";

            var verifier = CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput),
                verify: Verification.Skipped, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.<Produce>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      449 (0x1c1)
  .maxstack  4
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                Program.<Produce>d__1 V_2,
                int V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int Program.<Produce>d__1.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  sub
    IL_000b:  switch    (
        IL_0026,
        IL_002b,
        IL_0032,
        IL_0032,
        IL_002d)
    IL_0024:  br.s       IL_0032
    IL_0026:  br         IL_0118
    IL_002b:  br.s       IL_0032
    IL_002d:  br         IL_00ce
    IL_0032:  ldarg.0
    IL_0033:  ldfld      "bool Program.<Produce>d__1.<>w__disposeMode"
    IL_0038:  brfalse.s  IL_003f
    IL_003a:  leave      IL_0183
    IL_003f:  ldarg.0
    IL_0040:  ldc.i4.m1
    IL_0041:  dup
    IL_0042:  stloc.0
    IL_0043:  stfld      "int Program.<Produce>d__1.<>1__state"
    IL_0048:  nop
    IL_0049:  ldarg.0
    IL_004a:  ldc.i4.0
    IL_004b:  stfld      "int Program.<Produce>d__1.<i>5__1"
    IL_0050:  nop
    IL_0051:  ldarg.0
    IL_0052:  ldarg.0
    IL_0053:  ldfld      "C Program.<Produce>d__1.x"
    IL_0058:  stfld      "C Program.<Produce>d__1.<>s__4"
    IL_005d:  ldarg.0
    IL_005e:  ldfld      "C Program.<Produce>d__1.<>s__4"
    IL_0063:  ldfld      "Buffer2<int> C.F"
    IL_0068:  pop
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.0
    IL_006b:  stfld      "int Program.<Produce>d__1.<>s__2"
    IL_0070:  br         IL_013a
    IL_0075:  ldarg.0
    IL_0076:  ldarg.0
    IL_0077:  ldfld      "C Program.<Produce>d__1.<>s__4"
    IL_007c:  ldflda     "Buffer2<int> C.F"
    IL_0081:  ldarg.0
    IL_0082:  ldfld      "int Program.<Produce>d__1.<>s__2"
    IL_0087:  call       "ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer2<int>, int>(ref Buffer2<int>, int)"
    IL_008c:  ldind.i4
    IL_008d:  stfld      "int Program.<Produce>d__1.<y>5__3"
    IL_0092:  nop
    IL_0093:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get"
    IL_0098:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
    IL_009d:  stloc.1
    IL_009e:  ldloca.s   V_1
    IL_00a0:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
    IL_00a5:  brtrue.s   IL_00ea
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.0
    IL_00a9:  dup
    IL_00aa:  stloc.0
    IL_00ab:  stfld      "int Program.<Produce>d__1.<>1__state"
    IL_00b0:  ldarg.0
    IL_00b1:  ldloc.1
    IL_00b2:  stfld      "System.Runtime.CompilerServices.TaskAwaiter Program.<Produce>d__1.<>u__1"
    IL_00b7:  ldarg.0
    IL_00b8:  stloc.2
    IL_00b9:  ldarg.0
    IL_00ba:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder Program.<Produce>d__1.<>t__builder"
    IL_00bf:  ldloca.s   V_1
    IL_00c1:  ldloca.s   V_2
    IL_00c3:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, Program.<Produce>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter, ref Program.<Produce>d__1)"
    IL_00c8:  nop
    IL_00c9:  leave      IL_01c0
    IL_00ce:  ldarg.0
    IL_00cf:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter Program.<Produce>d__1.<>u__1"
    IL_00d4:  stloc.1
    IL_00d5:  ldarg.0
    IL_00d6:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter Program.<Produce>d__1.<>u__1"
    IL_00db:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
    IL_00e1:  ldarg.0
    IL_00e2:  ldc.i4.m1
    IL_00e3:  dup
    IL_00e4:  stloc.0
    IL_00e5:  stfld      "int Program.<Produce>d__1.<>1__state"
    IL_00ea:  ldloca.s   V_1
    IL_00ec:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
    IL_00f1:  nop
    IL_00f2:  ldarg.0
    IL_00f3:  ldarg.0
    IL_00f4:  ldfld      "int Program.<Produce>d__1.<i>5__1"
    IL_00f9:  stloc.3
    IL_00fa:  ldarg.0
    IL_00fb:  ldloc.3
    IL_00fc:  ldc.i4.1
    IL_00fd:  add
    IL_00fe:  stfld      "int Program.<Produce>d__1.<i>5__1"
    IL_0103:  ldloc.3
    IL_0104:  stfld      "int Program.<Produce>d__1.<>2__current"
    IL_0109:  ldarg.0
    IL_010a:  ldc.i4.s   -4
    IL_010c:  dup
    IL_010d:  stloc.0
    IL_010e:  stfld      "int Program.<Produce>d__1.<>1__state"
    IL_0113:  leave      IL_01b3
    IL_0118:  ldarg.0
    IL_0119:  ldc.i4.m1
    IL_011a:  dup
    IL_011b:  stloc.0
    IL_011c:  stfld      "int Program.<Produce>d__1.<>1__state"
    IL_0121:  ldarg.0
    IL_0122:  ldfld      "bool Program.<Produce>d__1.<>w__disposeMode"
    IL_0127:  brfalse.s  IL_012b
    IL_0129:  leave.s    IL_0183
    IL_012b:  nop
    IL_012c:  ldarg.0
    IL_012d:  ldarg.0
    IL_012e:  ldfld      "int Program.<Produce>d__1.<>s__2"
    IL_0133:  ldc.i4.1
    IL_0134:  add
    IL_0135:  stfld      "int Program.<Produce>d__1.<>s__2"
    IL_013a:  ldarg.0
    IL_013b:  ldfld      "int Program.<Produce>d__1.<>s__2"
    IL_0140:  ldc.i4.2
    IL_0141:  blt        IL_0075
    IL_0146:  ldarg.0
    IL_0147:  ldnull
    IL_0148:  stfld      "C Program.<Produce>d__1.<>s__4"
    IL_014d:  leave.s    IL_0183
  }
  catch System.Exception
  {
    IL_014f:  stloc.s    V_4
    IL_0151:  ldarg.0
    IL_0152:  ldc.i4.s   -2
    IL_0154:  stfld      "int Program.<Produce>d__1.<>1__state"
    IL_0159:  ldarg.0
    IL_015a:  ldnull
    IL_015b:  stfld      "C Program.<Produce>d__1.<>s__4"
    IL_0160:  ldarg.0
    IL_0161:  ldc.i4.0
    IL_0162:  stfld      "int Program.<Produce>d__1.<>2__current"
    IL_0167:  ldarg.0
    IL_0168:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder Program.<Produce>d__1.<>t__builder"
    IL_016d:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
    IL_0172:  nop
    IL_0173:  ldarg.0
    IL_0174:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> Program.<Produce>d__1.<>v__promiseOfValueOrEnd"
    IL_0179:  ldloc.s    V_4
    IL_017b:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
    IL_0180:  nop
    IL_0181:  leave.s    IL_01c0
  }
  IL_0183:  ldarg.0
  IL_0184:  ldc.i4.s   -2
  IL_0186:  stfld      "int Program.<Produce>d__1.<>1__state"
  IL_018b:  ldarg.0
  IL_018c:  ldnull
  IL_018d:  stfld      "C Program.<Produce>d__1.<>s__4"
  IL_0192:  ldarg.0
  IL_0193:  ldc.i4.0
  IL_0194:  stfld      "int Program.<Produce>d__1.<>2__current"
  IL_0199:  ldarg.0
  IL_019a:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder Program.<Produce>d__1.<>t__builder"
  IL_019f:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
  IL_01a4:  nop
  IL_01a5:  ldarg.0
  IL_01a6:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> Program.<Produce>d__1.<>v__promiseOfValueOrEnd"
  IL_01ab:  ldc.i4.0
  IL_01ac:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_01b1:  nop
  IL_01b2:  ret
  IL_01b3:  ldarg.0
  IL_01b4:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> Program.<Produce>d__1.<>v__promiseOfValueOrEnd"
  IL_01b9:  ldc.i4.1
  IL_01ba:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_01bf:  nop
  IL_01c0:  ret
}
""");
            var comp = CreateRuntimeAsyncCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void AddVariableCleanup_Unmanaged_UseSiteError()
        {
            var missingLibS1 = CreateCompilation(@"
public struct S1
{
    public int i;
}
", assemblyName: "libS1", targetFramework: TargetFramework.Net80).ToMetadataReference();

            var libS2 = CreateCompilation(@"
public struct S2
{
    public S1 s1;
}
", references: [missingLibS1], assemblyName: "libS2", targetFramework: TargetFramework.Net80).ToMetadataReference();

            var source = @"
class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<S2> M(S2 p)
    {
        S2 local = p;
        await System.Threading.Tasks.Task.CompletedTask;
        yield return local;
        System.Console.Write(local);
    }
}
";
            var comp = CreateCompilation(source, references: [libS2], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // error CS0012: The type 'S1' is defined in an assembly that is not referenced. You must add a reference to assembly 'libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_NoTypeDef).WithArguments("S1", "libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1),
                // error CS0012: The type 'S1' is defined in an assembly that is not referenced. You must add a reference to assembly 'libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_NoTypeDef).WithArguments("S1", "libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1),
                // error CS0012: The type 'S1' is defined in an assembly that is not referenced. You must add a reference to assembly 'libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_NoTypeDef).WithArguments("S1", "libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1),
                // error CS0012: The type 'S1' is defined in an assembly that is not referenced. You must add a reference to assembly 'libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_NoTypeDef).WithArguments("S1", "libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1));

            comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll, references: [libS2, missingLibS1], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_YieldReturn()
        {
            string src = """
var enumerator = C.Produce();
System.Console.Write(await enumerator.MoveNextAsync());
System.Console.Write(enumerator.Current);

await enumerator.DisposeAsync();

System.Console.Write(await enumerator.MoveNextAsync());
System.Console.Write(enumerator.Current is null ? " null" : throw null);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerator<string> Produce()
    {
        await System.Threading.Tasks.Task.CompletedTask;
        yield return " one ";
        yield return " two ";
    }
}
""";
            var expectedOutput = "True one False null";

            CompileAndVerify(src, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped, targetFramework: TargetFramework.Net80)
                .VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_01()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve1Attribute : Attribute { }

[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve2Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test1
{
    async IAsyncEnumerable<T> M2<[Preserve1][Preserve2]T>(T x)
    {
        await Task.Yield();
        yield return x;
    }
}
";
            var comp1 = CreateCompilation([source1, source2, CompilerLoweringPreserveAttributeDefinition], targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember<NamedTypeSymbol>("Test1.<M2>d__0").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_02()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test1
{
    async IAsyncEnumerable<int> M2([Preserve1][Preserve2][Preserve3]int x)
    {
        await Task.Yield();
        yield return x;
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<M2>d__0.x").GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<M2>d__0.<>3__x").GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_03()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve1Attribute : Attribute { }

[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve2Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS8321 // Local function is declared but never used

class Test1
{
    static void Test<[Preserve1][Preserve2]T>()
    {
        async IAsyncEnumerable<T> local(T x)
        {
            await Task.Yield();
            yield return x;
        }
    }
}
";
            var comp1 = CreateCompilation([source1, source2, CompilerLoweringPreserveAttributeDefinition], targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            string source3 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS8321 // Local function is declared but never used

class Test1
{
    static void Test()
    {
        async IAsyncEnumerable<T> local<[Preserve1][Preserve2]T>(T x)
        {
            await Task.Yield();
            yield return x;
        }
    }
}
";
            comp1 = CreateCompilation([source1, source3, CompilerLoweringPreserveAttributeDefinition], targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute", "Preserve2Attribute"],
                    m.GlobalNamespace.GetMember<MethodSymbol>("Test1.<Test>g__local|0_0").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember<NamedTypeSymbol>("Test1.<<Test>g__local|0_0>d").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_04()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS8321 // Local function is declared but never used

class Test1
{
    static void Test()
    {
        async IAsyncEnumerable<int> local([Preserve1][Preserve2][Preserve3]int x)
        {
            await Task.Yield();
            yield return x;
        }
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute", "Preserve2Attribute", "Preserve3Attribute"],
                    m.GlobalNamespace.GetMember<MethodSymbol>("Test1.<Test>g__local|0_0").Parameters.Single().GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<<Test>g__local|0_0>d.x").GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<<Test>g__local|0_0>d.<>3__x").GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_05()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS8321 // Local function is declared but never used

class Test1
{
    static void Test([Preserve1][Preserve2][Preserve3]int x)
    {
        async IAsyncEnumerable<int> local()
        {
            await Task.Yield();
            yield return x;
        }
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<>c__DisplayClass0_0.x").GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_06()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve1Attribute : Attribute { }

[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve2Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

static class Test1
{
    extension<[Preserve1][Preserve2]T>(T x)
    {
        async IAsyncEnumerable<T> M2()
        {
            await Task.Yield();
            yield return x;
        }
    }
}
";
            var comp1 = CreateCompilation([source1, source2, CompilerLoweringPreserveAttributeDefinition], targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            string source3 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

static class Test1
{
    extension(int i)
    {
        async IAsyncEnumerable<T> M2<[Preserve1][Preserve2]T>(T x)
        {
            await Task.Yield();
            yield return x;
        }
    }
}
";
            comp1 = CreateCompilation([source1, source3, CompilerLoweringPreserveAttributeDefinition], targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember<NamedTypeSymbol>("Test1.<M2>d__1").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_07()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

static class Test1
{
    extension([Preserve1][Preserve2][Preserve3]int x)
    {
        async IAsyncEnumerable<int> M2()
        {
            await Task.Yield();
            yield return x;
        }
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            string source3 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

static class Test1
{
    extension(int i)
    {
        async IAsyncEnumerable<int> M2([Preserve1][Preserve2][Preserve3]int x)
        {
            await Task.Yield();
            yield return x;
        }
    }
}
";
            comp1 = CreateCompilation(
                [source1, source3, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<M2>d__1.x").GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<M2>d__1.<>3__x").GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_08()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve1Attribute : Attribute { }

[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve2Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension<[Preserve1][Preserve2]T>(int i)
    {
        static void Test()
        {
            async IAsyncEnumerable<T> local(T x)
            {
                await Task.Yield();
                yield return x;
            }
        }
    }
}
";
            var comp1 = CreateCompilation([source1, source2, CompilerLoweringPreserveAttributeDefinition], targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            string source3 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension(int i)
    {
        static void Test<[Preserve1][Preserve2]T>()
        {
            async IAsyncEnumerable<T> local(T x)
            {
                await Task.Yield();
                yield return x;
            }
        }
    }
}
";
            comp1 = CreateCompilation([source1, source3, CompilerLoweringPreserveAttributeDefinition], targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            string source4 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension(int i)
    {
        static void Test()
        {
            async IAsyncEnumerable<T> local<[Preserve1][Preserve2]T>(T x)
            {
                await Task.Yield();
                yield return x;
            }
        }
    }
}
";
            comp1 = CreateCompilation([source1, source4, CompilerLoweringPreserveAttributeDefinition], targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute", "Preserve2Attribute"],
                    m.GlobalNamespace.GetMember<MethodSymbol>("Test1.<Test>g__local|1_0").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember<NamedTypeSymbol>("Test1.<<Test>g__local|1_0>d").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_09()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension(int i)
    {
        static void Test()
        {
            async IAsyncEnumerable<int> local([Preserve1][Preserve2][Preserve3]int x)
            {
                await Task.Yield();
                yield return x;
            }
        }
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute", "Preserve2Attribute", "Preserve3Attribute"],
                    m.GlobalNamespace.GetMember<MethodSymbol>("Test1.<Test>g__local|1_0").Parameters.Single().GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<<Test>g__local|1_0>d.x").GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<<Test>g__local|1_0>d.<>3__x").GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_10()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension([Preserve1][Preserve2][Preserve3]int x)
    {
        void Test()
        {
            async IAsyncEnumerable<int> local()
            {
                await Task.Yield();
                yield return x;
            }
        }
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            string source3 = @"
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension(int i)
    {
        static void Test([Preserve1][Preserve2][Preserve3]int x)
        {
            async IAsyncEnumerable<int> local()
            {
                await Task.Yield();
                yield return x;
            }
        }
    }
}
";
            comp1 = CreateCompilation(
                [source1, source3, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp1, symbolValidator: validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<>c__DisplayClass1_0.x").GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        [WorkItem(78640, "https://github.com/dotnet/roslyn/issues/78640")]
        public void Repro_78640()
        {
            var source = """
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                using System.Threading;
                using System;

                static class C
                {
                    public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> enumerable, [EnumeratorCancellation] CancellationToken cancellationToken)
                    {
                        ArgumentNullException.ThrowIfNull(enumerable);

                        cancellationToken.ThrowIfCancellationRequested();
                        foreach (T item in enumerable)
                        {
                            yield return item;
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                }
                """;

            var verifier = CompileAndVerify(
                [source, DynamicAnalysisResourceTests.InstrumentationHelperSource],
                targetFramework: TargetFramework.NetCoreApp,
                emitOptions: EmitOptions.Default.WithInstrumentationKinds([InstrumentationKind.TestCoverage]),
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.<AsAsyncEnumerable>d__0<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
                 {
                  // Code size      520 (0x208)
                  .maxstack  6
                  .locals init (int V_0,
                                T V_1, //item
                                System.Exception V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "int C.<AsAsyncEnumerable>d__0<T>.<>1__state"
                  IL_0006:  stloc.0
                  .try
                  {
                    IL_0007:  ldloc.0
                    IL_0008:  ldc.i4.s   -4
                    IL_000a:  beq        IL_00b4
                    IL_000f:  ldloc.0
                    IL_0010:  ldc.i4.s   -3
                    IL_0012:  pop
                    IL_0013:  pop
                    IL_0014:  ldarg.0
                    IL_0015:  ldfld      "bool C.<AsAsyncEnumerable>d__0<T>.<>w__disposeMode"
                    IL_001a:  brfalse.s  IL_0021
                    IL_001c:  leave      IL_01a7
                    IL_0021:  ldarg.0
                    IL_0022:  ldc.i4.m1
                    IL_0023:  dup
                    IL_0024:  stloc.0
                    IL_0025:  stfld      "int C.<AsAsyncEnumerable>d__0<T>.<>1__state"
                    IL_002a:  ldarg.0
                    IL_002b:  ldsfld     "bool[][] <PrivateImplementationDetails>.PayloadRoot0"
                    IL_0030:  ldtoken    "System.Collections.Generic.IAsyncEnumerable<T> C.AsAsyncEnumerable<T>(System.Collections.Generic.IEnumerable<T>, System.Threading.CancellationToken)"
                    IL_0035:  ldelem.ref
                    IL_0036:  stfld      "bool[] C.<AsAsyncEnumerable>d__0<T>.<>7__wrap1"
                    IL_003b:  ldarg.0
                    IL_003c:  ldfld      "bool[] C.<AsAsyncEnumerable>d__0<T>.<>7__wrap1"
                    IL_0041:  brtrue.s   IL_006d
                    IL_0043:  ldarg.0
                    IL_0044:  ldsfld     "System.Guid <PrivateImplementationDetails>.MVID"
                    IL_0049:  ldtoken    "System.Collections.Generic.IAsyncEnumerable<T> C.AsAsyncEnumerable<T>(System.Collections.Generic.IEnumerable<T>, System.Threading.CancellationToken)"
                    IL_004e:  ldtoken    Source Document 0
                    IL_0053:  ldsfld     "bool[][] <PrivateImplementationDetails>.PayloadRoot0"
                    IL_0058:  ldtoken    "System.Collections.Generic.IAsyncEnumerable<T> C.AsAsyncEnumerable<T>(System.Collections.Generic.IEnumerable<T>, System.Threading.CancellationToken)"
                    IL_005d:  ldelema    "bool[]"
                    IL_0062:  ldc.i4.6
                    IL_0063:  call       "bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, int, ref bool[], int)"
                    IL_0068:  stfld      "bool[] C.<AsAsyncEnumerable>d__0<T>.<>7__wrap1"
                    IL_006d:  ldarg.0
                    IL_006e:  ldfld      "bool[] C.<AsAsyncEnumerable>d__0<T>.<>7__wrap1"
                    IL_0073:  ldc.i4.0
                    IL_0074:  ldc.i4.1
                    IL_0075:  stelem.i1
                    IL_0076:  ldarg.0
                    IL_0077:  ldfld      "bool[] C.<AsAsyncEnumerable>d__0<T>.<>7__wrap1"
                    IL_007c:  ldc.i4.1
                    IL_007d:  ldc.i4.1
                    IL_007e:  stelem.i1
                    IL_007f:  ldarg.0
                    IL_0080:  ldfld      "System.Collections.Generic.IEnumerable<T> C.<AsAsyncEnumerable>d__0<T>.enumerable"
                    IL_0085:  ldstr      "enumerable"
                    IL_008a:  call       "void System.ArgumentNullException.ThrowIfNull(object, string)"
                    IL_008f:  ldarg.0
                    IL_0090:  ldfld      "bool[] C.<AsAsyncEnumerable>d__0<T>.<>7__wrap1"
                    IL_0095:  ldc.i4.2
                    IL_0096:  ldc.i4.1
                    IL_0097:  stelem.i1
                    IL_0098:  ldarg.0
                    IL_0099:  ldflda     "System.Threading.CancellationToken C.<AsAsyncEnumerable>d__0<T>.cancellationToken"
                    IL_009e:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
                    IL_00a3:  ldarg.0
                    IL_00a4:  ldarg.0
                    IL_00a5:  ldfld      "System.Collections.Generic.IEnumerable<T> C.<AsAsyncEnumerable>d__0<T>.enumerable"
                    IL_00aa:  callvirt   "System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()"
                    IL_00af:  stfld      "System.Collections.Generic.IEnumerator<T> C.<AsAsyncEnumerable>d__0<T>.<>7__wrap2"
                    IL_00b4:  nop
                    .try
                    {
                      IL_00b5:  ldloc.0
                      IL_00b6:  ldc.i4.s   -4
                      IL_00b8:  beq.s      IL_00f0
                      IL_00ba:  br.s       IL_0117
                      IL_00bc:  ldarg.0
                      IL_00bd:  ldfld      "bool[] C.<AsAsyncEnumerable>d__0<T>.<>7__wrap1"
                      IL_00c2:  ldc.i4.5
                      IL_00c3:  ldc.i4.1
                      IL_00c4:  stelem.i1
                      IL_00c5:  ldarg.0
                      IL_00c6:  ldfld      "System.Collections.Generic.IEnumerator<T> C.<AsAsyncEnumerable>d__0<T>.<>7__wrap2"
                      IL_00cb:  callvirt   "T System.Collections.Generic.IEnumerator<T>.Current.get"
                      IL_00d0:  stloc.1
                      IL_00d1:  ldarg.0
                      IL_00d2:  ldfld      "bool[] C.<AsAsyncEnumerable>d__0<T>.<>7__wrap1"
                      IL_00d7:  ldc.i4.3
                      IL_00d8:  ldc.i4.1
                      IL_00d9:  stelem.i1
                      IL_00da:  ldarg.0
                      IL_00db:  ldloc.1
                      IL_00dc:  stfld      "T C.<AsAsyncEnumerable>d__0<T>.<>2__current"
                      IL_00e1:  ldarg.0
                      IL_00e2:  ldc.i4.s   -4
                      IL_00e4:  dup
                      IL_00e5:  stloc.0
                      IL_00e6:  stfld      "int C.<AsAsyncEnumerable>d__0<T>.<>1__state"
                      IL_00eb:  leave      IL_01fb
                      IL_00f0:  ldarg.0
                      IL_00f1:  ldc.i4.m1
                      IL_00f2:  dup
                      IL_00f3:  stloc.0
                      IL_00f4:  stfld      "int C.<AsAsyncEnumerable>d__0<T>.<>1__state"
                      IL_00f9:  ldarg.0
                      IL_00fa:  ldfld      "bool C.<AsAsyncEnumerable>d__0<T>.<>w__disposeMode"
                      IL_00ff:  brfalse.s  IL_0103
                      IL_0101:  leave.s    IL_013e
                      IL_0103:  ldarg.0
                      IL_0104:  ldfld      "bool[] C.<AsAsyncEnumerable>d__0<T>.<>7__wrap1"
                      IL_0109:  ldc.i4.4
                      IL_010a:  ldc.i4.1
                      IL_010b:  stelem.i1
                      IL_010c:  ldarg.0
                      IL_010d:  ldflda     "System.Threading.CancellationToken C.<AsAsyncEnumerable>d__0<T>.cancellationToken"
                      IL_0112:  call       "void System.Threading.CancellationToken.ThrowIfCancellationRequested()"
                      IL_0117:  ldarg.0
                      IL_0118:  ldfld      "System.Collections.Generic.IEnumerator<T> C.<AsAsyncEnumerable>d__0<T>.<>7__wrap2"
                      IL_011d:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                      IL_0122:  brtrue.s   IL_00bc
                      IL_0124:  leave.s    IL_013e
                    }
                    finally
                    {
                      IL_0126:  ldloc.0
                      IL_0127:  ldc.i4.m1
                      IL_0128:  bne.un.s   IL_013d
                      IL_012a:  ldarg.0
                      IL_012b:  ldfld      "System.Collections.Generic.IEnumerator<T> C.<AsAsyncEnumerable>d__0<T>.<>7__wrap2"
                      IL_0130:  brfalse.s  IL_013d
                      IL_0132:  ldarg.0
                      IL_0133:  ldfld      "System.Collections.Generic.IEnumerator<T> C.<AsAsyncEnumerable>d__0<T>.<>7__wrap2"
                      IL_0138:  callvirt   "void System.IDisposable.Dispose()"
                      IL_013d:  endfinally
                    }
                    IL_013e:  ldarg.0
                    IL_013f:  ldfld      "bool C.<AsAsyncEnumerable>d__0<T>.<>w__disposeMode"
                    IL_0144:  brfalse.s  IL_0148
                    IL_0146:  leave.s    IL_01a7
                    IL_0148:  ldarg.0
                    IL_0149:  ldnull
                    IL_014a:  stfld      "System.Collections.Generic.IEnumerator<T> C.<AsAsyncEnumerable>d__0<T>.<>7__wrap2"
                    IL_014f:  leave.s    IL_01a7
                  }
                  catch System.Exception
                  {
                    IL_0151:  stloc.2
                    IL_0152:  ldarg.0
                    IL_0153:  ldc.i4.s   -2
                    IL_0155:  stfld      "int C.<AsAsyncEnumerable>d__0<T>.<>1__state"
                    IL_015a:  ldarg.0
                    IL_015b:  ldnull
                    IL_015c:  stfld      "bool[] C.<AsAsyncEnumerable>d__0<T>.<>7__wrap1"
                    IL_0161:  ldarg.0
                    IL_0162:  ldnull
                    IL_0163:  stfld      "System.Collections.Generic.IEnumerator<T> C.<AsAsyncEnumerable>d__0<T>.<>7__wrap2"
                    IL_0168:  ldarg.0
                    IL_0169:  ldfld      "System.Threading.CancellationTokenSource C.<AsAsyncEnumerable>d__0<T>.<>x__combinedTokens"
                    IL_016e:  brfalse.s  IL_0182
                    IL_0170:  ldarg.0
                    IL_0171:  ldfld      "System.Threading.CancellationTokenSource C.<AsAsyncEnumerable>d__0<T>.<>x__combinedTokens"
                    IL_0176:  callvirt   "void System.Threading.CancellationTokenSource.Dispose()"
                    IL_017b:  ldarg.0
                    IL_017c:  ldnull
                    IL_017d:  stfld      "System.Threading.CancellationTokenSource C.<AsAsyncEnumerable>d__0<T>.<>x__combinedTokens"
                    IL_0182:  ldarg.0
                    IL_0183:  ldflda     "T C.<AsAsyncEnumerable>d__0<T>.<>2__current"
                    IL_0188:  initobj    "T"
                    IL_018e:  ldarg.0
                    IL_018f:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<AsAsyncEnumerable>d__0<T>.<>t__builder"
                    IL_0194:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
                    IL_0199:  ldarg.0
                    IL_019a:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<AsAsyncEnumerable>d__0<T>.<>v__promiseOfValueOrEnd"
                    IL_019f:  ldloc.2
                    IL_01a0:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
                    IL_01a5:  leave.s    IL_0207
                  }
                  IL_01a7:  ldarg.0
                  IL_01a8:  ldc.i4.s   -2
                  IL_01aa:  stfld      "int C.<AsAsyncEnumerable>d__0<T>.<>1__state"
                  IL_01af:  ldarg.0
                  IL_01b0:  ldnull
                  IL_01b1:  stfld      "bool[] C.<AsAsyncEnumerable>d__0<T>.<>7__wrap1"
                  IL_01b6:  ldarg.0
                  IL_01b7:  ldnull
                  IL_01b8:  stfld      "System.Collections.Generic.IEnumerator<T> C.<AsAsyncEnumerable>d__0<T>.<>7__wrap2"
                  IL_01bd:  ldarg.0
                  IL_01be:  ldfld      "System.Threading.CancellationTokenSource C.<AsAsyncEnumerable>d__0<T>.<>x__combinedTokens"
                  IL_01c3:  brfalse.s  IL_01d7
                  IL_01c5:  ldarg.0
                  IL_01c6:  ldfld      "System.Threading.CancellationTokenSource C.<AsAsyncEnumerable>d__0<T>.<>x__combinedTokens"
                  IL_01cb:  callvirt   "void System.Threading.CancellationTokenSource.Dispose()"
                  IL_01d0:  ldarg.0
                  IL_01d1:  ldnull
                  IL_01d2:  stfld      "System.Threading.CancellationTokenSource C.<AsAsyncEnumerable>d__0<T>.<>x__combinedTokens"
                  IL_01d7:  ldarg.0
                  IL_01d8:  ldflda     "T C.<AsAsyncEnumerable>d__0<T>.<>2__current"
                  IL_01dd:  initobj    "T"
                  IL_01e3:  ldarg.0
                  IL_01e4:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<AsAsyncEnumerable>d__0<T>.<>t__builder"
                  IL_01e9:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
                  IL_01ee:  ldarg.0
                  IL_01ef:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<AsAsyncEnumerable>d__0<T>.<>v__promiseOfValueOrEnd"
                  IL_01f4:  ldc.i4.0
                  IL_01f5:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
                  IL_01fa:  ret
                  IL_01fb:  ldarg.0
                  IL_01fc:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<AsAsyncEnumerable>d__0<T>.<>v__promiseOfValueOrEnd"
                  IL_0201:  ldc.i4.1
                  IL_0202:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
                  IL_0207:  ret
                }
                """);
        }

        [Fact]
        public void RuntimeAsync_01()
        {
            // simplest scenario
            string source = """
using static System.Console;

await using var enumerator = C.M().GetAsyncEnumerator();
var found = await enumerator.MoveNextAsync();
if (!found) throw new System.Exception("A");
var value = enumerator.Current;
Write($"{value} ");
found = await enumerator.MoveNextAsync();
if (found) throw new System.Exception("B");
found = await enumerator.MoveNextAsync();
if (found) throw new System.Exception("C");
Write("5");

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write("1 ");
        await System.Threading.Tasks.Task.CompletedTask;
        Write("2 ");
        yield return 3;
        Write("4 ");
    }
}
""";
            var comp = CreateRuntimeAsyncCompilation(source);

            Verification expectedVerification = Verification.FailsILVerify with
            {
                ILVerifyMessage = """
                    [<Main>$]: Return value missing on the stack. { Offset = 0xce }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0xa1, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [System.IAsyncDisposable.DisposeAsync]: Return value missing on the stack. { Offset = 0x19 }
                    [System.IAsyncDisposable.DisposeAsync]: Return value missing on the stack. { Offset = 0x2d }
                    """
            };

            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("1 2 3 4 5"),
                symbolValidator: verify, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedVerification : Verification.Skipped);
            // Note: for runtime-async codegen, it is expected that we place bool instead of ValueTask<bool> on the stack, so IL verification is expected to fail.

            // PROTOTYPE confirm what attribute the kickoff method should have (if any)
            verifier.VerifyTypeIL("C", """
.class private auto ansi beforefieldinit C
    extends [System.Runtime]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed beforefieldinit '<M>d__0'
        extends [System.Runtime]System.Object
        implements class [System.Runtime]System.Collections.Generic.IAsyncEnumerable`1<int32>,
                   class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<int32>,
                   [System.Runtime]System.IAsyncDisposable
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field public int32 '<>1__state'
        .field private int32 '<>2__current'
        .field private bool '<>w__disposeMode'
        .field private int32 '<>l__initialThreadId'
        // Methods
        .method public hidebysig specialname rtspecialname
            instance void .ctor (
                int32 '<>1__state'
            ) cil managed
        {
            .custom instance void [System.Runtime]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            // Code size 25 (0x19)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: call instance void [System.Runtime]System.Object::.ctor()
            IL_0006: ldarg.0
            IL_0007: ldarg.1
            IL_0008: stfld int32 C/'<M>d__0'::'<>1__state'
            IL_000d: ldarg.0
            IL_000e: call int32 [System.Runtime]System.Environment::get_CurrentManagedThreadId()
            IL_0013: stfld int32 C/'<M>d__0'::'<>l__initialThreadId'
            IL_0018: ret
        } // end of method '<M>d__0'::.ctor
        .method private final hidebysig newslot virtual
            instance class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<int32> 'System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator' (
                [opt] valuetype [System.Runtime]System.Threading.CancellationToken cancellationToken
            ) cil managed
        {
            .custom instance void [System.Runtime]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<!0> class [System.Runtime]System.Collections.Generic.IAsyncEnumerable`1<int32>::GetAsyncEnumerator(valuetype [System.Runtime]System.Threading.CancellationToken)
            .param [0]
                .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                    01 00 01 00 00
                )
            .param [1] = nullref
            // Code size 52 (0x34)
            .maxstack 2
            .locals init (
                [0] class C/'<M>d__0'
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 C/'<M>d__0'::'<>1__state'
            IL_0006: ldc.i4.s -2
            IL_0008: bne.un.s IL_002a
            IL_000a: ldarg.0
            IL_000b: ldfld int32 C/'<M>d__0'::'<>l__initialThreadId'
            IL_0010: call int32 [System.Runtime]System.Environment::get_CurrentManagedThreadId()
            IL_0015: bne.un.s IL_002a
            IL_0017: ldarg.0
            IL_0018: ldc.i4.s -3
            IL_001a: stfld int32 C/'<M>d__0'::'<>1__state'
            IL_001f: ldarg.0
            IL_0020: ldc.i4.0
            IL_0021: stfld bool C/'<M>d__0'::'<>w__disposeMode'
            IL_0026: ldarg.0
            IL_0027: stloc.0
            IL_0028: br.s IL_0032
            IL_002a: ldc.i4.s -3
            IL_002c: newobj instance void C/'<M>d__0'::.ctor(int32)
            IL_0031: stloc.0
            IL_0032: ldloc.0
            IL_0033: ret
        } // end of method '<M>d__0'::'System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator'
        .method private final hidebysig newslot virtual
            instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<bool> MoveNextAsync () cil managed flag(2000)
        {
            .override method instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<bool> class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<int32>::MoveNextAsync()
            // Code size 162 (0xa2)
            .maxstack 3
            .locals init (
                [0] int32,
                [1] bool
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 C/'<M>d__0'::'<>1__state'
            IL_0006: stloc.0
            .try
            {
                IL_0007: ldloc.0
                IL_0008: ldc.i4.s -4
                IL_000a: beq.s IL_0057
                IL_000c: ldloc.0
                IL_000d: ldc.i4.s -3
                IL_000f: pop
                IL_0010: pop
                IL_0011: ldarg.0
                IL_0012: ldfld bool C/'<M>d__0'::'<>w__disposeMode'
                IL_0017: brfalse.s IL_001b
                IL_0019: leave.s IL_008f
                IL_001b: ldarg.0
                IL_001c: ldc.i4.m1
                IL_001d: dup
                IL_001e: stloc.0
                IL_001f: stfld int32 C/'<M>d__0'::'<>1__state'
                IL_0024: ldstr "1 "
                IL_0029: call void [System.Console]System.Console::Write(string)
                IL_002e: call class [System.Runtime]System.Threading.Tasks.Task [System.Runtime]System.Threading.Tasks.Task::get_CompletedTask()
                IL_0033: call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
                IL_0038: ldstr "2 "
                IL_003d: call void [System.Console]System.Console::Write(string)
                IL_0042: ldarg.0
                IL_0043: ldc.i4.3
                IL_0044: stfld int32 C/'<M>d__0'::'<>2__current'
                IL_0049: ldarg.0
                IL_004a: ldc.i4.s -4
                IL_004c: dup
                IL_004d: stloc.0
                IL_004e: stfld int32 C/'<M>d__0'::'<>1__state'
                IL_0053: ldc.i4.1
                IL_0054: stloc.1
                IL_0055: leave.s IL_00a0
                IL_0057: ldarg.0
                IL_0058: ldc.i4.m1
                IL_0059: dup
                IL_005a: stloc.0
                IL_005b: stfld int32 C/'<M>d__0'::'<>1__state'
                IL_0060: ldarg.0
                IL_0061: ldfld bool C/'<M>d__0'::'<>w__disposeMode'
                IL_0066: brfalse.s IL_006a
                IL_0068: leave.s IL_008f
                IL_006a: ldstr "4 "
                IL_006f: call void [System.Console]System.Console::Write(string)
                IL_0074: ldarg.0
                IL_0075: ldc.i4.1
                IL_0076: stfld bool C/'<M>d__0'::'<>w__disposeMode'
                IL_007b: leave.s IL_008f
            } // end .try
            catch [System.Runtime]System.Exception
            {
                IL_007d: pop
                IL_007e: ldarg.0
                IL_007f: ldc.i4.s -2
                IL_0081: stfld int32 C/'<M>d__0'::'<>1__state'
                IL_0086: ldarg.0
                IL_0087: ldc.i4.0
                IL_0088: stfld int32 C/'<M>d__0'::'<>2__current'
                IL_008d: rethrow
            } // end handler
            IL_008f: ldarg.0
            IL_0090: ldc.i4.s -2
            IL_0092: stfld int32 C/'<M>d__0'::'<>1__state'
            IL_0097: ldarg.0
            IL_0098: ldc.i4.0
            IL_0099: stfld int32 C/'<M>d__0'::'<>2__current'
            IL_009e: ldc.i4.0
            IL_009f: stloc.1
            IL_00a0: ldloc.1
            IL_00a1: ret
        } // end of method '<M>d__0'::MoveNextAsync
        .method private final hidebysig specialname newslot virtual
            instance int32 'System.Collections.Generic.IAsyncEnumerator<System.Int32>.get_Current' () cil managed
        {
            .custom instance void [System.Runtime]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance !0 class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<int32>::get_Current()
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld int32 C/'<M>d__0'::'<>2__current'
            IL_0006: ret
        } // end of method '<M>d__0'::'System.Collections.Generic.IAsyncEnumerator<System.Int32>.get_Current'
        .method private final hidebysig newslot virtual
            instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync () cil managed flag(2000)
        {
            .custom instance void [System.Runtime]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask [System.Runtime]System.IAsyncDisposable::DisposeAsync()
            // Code size 46 (0x2e)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld int32 C/'<M>d__0'::'<>1__state'
            IL_0006: ldc.i4.m1
            IL_0007: blt.s IL_000f
            IL_0009: newobj instance void [System.Runtime]System.NotSupportedException::.ctor()
            IL_000e: throw
            IL_000f: ldarg.0
            IL_0010: ldfld int32 C/'<M>d__0'::'<>1__state'
            IL_0015: ldc.i4.s -2
            IL_0017: bne.un.s IL_001a
            IL_0019: ret
            IL_001a: ldarg.0
            IL_001b: ldc.i4.1
            IL_001c: stfld bool C/'<M>d__0'::'<>w__disposeMode'
            IL_0021: ldarg.0
            IL_0022: callvirt instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<bool> class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<int32>::MoveNextAsync()
            IL_0027: call !!0 [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await<bool>(valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<!!0>)
            IL_002c: pop
            IL_002d: ret
        } // end of method '<M>d__0'::System.IAsyncDisposable.DisposeAsync
        // Properties
        .property instance int32 'System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current'()
        {
            .get instance int32 C/'<M>d__0'::'System.Collections.Generic.IAsyncEnumerator<System.Int32>.get_Current'()
        }
    } // end of class <M>d__0
    // Methods
    .method public hidebysig static
        class [System.Runtime]System.Collections.Generic.IAsyncEnumerable`1<int32> M () cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute::.ctor(class [System.Runtime]System.Type) = (
            01 00 09 43 2b 3c 4d 3e 64 5f 5f 30 00 00
        )
        // Code size 8 (0x8)
        .maxstack 8
        IL_0000: ldc.i4.s -2
        IL_0002: newobj instance void C/'<M>d__0'::.ctor(int32)
        IL_0007: ret
    } // end of method C::M
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: ret
    } // end of method C::.ctor
} // end of class C
""");
            // PROTOTYPE should this be debugger hidden?
            verifier.VerifyIL("C.M()", """
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.s   -2
  IL_0002:  newobj     "C.<M>d__0..ctor(int)"
  IL_0007:  ret
}
""", sequencePointDisplay: SequencePointDisplayMode.Enhanced);

            verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()", """
{
  // Code size      162 (0xa2)
  .maxstack  3
  .locals init (int V_0,
                bool V_1)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  beq.s      IL_0057
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.s   -3
    IL_000f:  pop
    IL_0010:  pop
    IL_0011:  ldarg.0
    IL_0012:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_0017:  brfalse.s  IL_001b
    IL_0019:  leave.s    IL_008f
    IL_001b:  ldarg.0
    IL_001c:  ldc.i4.m1
    IL_001d:  dup
    IL_001e:  stloc.0
    IL_001f:  stfld      "int C.<M>d__0.<>1__state"
    // sequence point: Write("1 ");
    IL_0024:  ldstr      "1 "
    IL_0029:  call       "void System.Console.Write(string)"
    // sequence point: await System.Threading.Tasks.Task.CompletedTask;
    IL_002e:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get"
    IL_0033:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
    // sequence point: Write("2 ");
    IL_0038:  ldstr      "2 "
    IL_003d:  call       "void System.Console.Write(string)"
    // sequence point: yield return 3;
    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.3
    IL_0044:  stfld      "int C.<M>d__0.<>2__current"
    IL_0049:  ldarg.0
    IL_004a:  ldc.i4.s   -4
    IL_004c:  dup
    IL_004d:  stloc.0
    IL_004e:  stfld      "int C.<M>d__0.<>1__state"
    IL_0053:  ldc.i4.1
    IL_0054:  stloc.1
    IL_0055:  leave.s    IL_00a0
    // sequence point: <hidden>
    IL_0057:  ldarg.0
    IL_0058:  ldc.i4.m1
    IL_0059:  dup
    IL_005a:  stloc.0
    IL_005b:  stfld      "int C.<M>d__0.<>1__state"
    IL_0060:  ldarg.0
    IL_0061:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_0066:  brfalse.s  IL_006a
    IL_0068:  leave.s    IL_008f
    // sequence point: Write("4 ");
    IL_006a:  ldstr      "4 "
    IL_006f:  call       "void System.Console.Write(string)"
    IL_0074:  ldarg.0
    IL_0075:  ldc.i4.1
    IL_0076:  stfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_007b:  leave.s    IL_008f
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_007d:  pop
    IL_007e:  ldarg.0
    IL_007f:  ldc.i4.s   -2
    IL_0081:  stfld      "int C.<M>d__0.<>1__state"
    IL_0086:  ldarg.0
    IL_0087:  ldc.i4.0
    IL_0088:  stfld      "int C.<M>d__0.<>2__current"
    IL_008d:  rethrow
  }
  // sequence point: }
  IL_008f:  ldarg.0
  IL_0090:  ldc.i4.s   -2
  IL_0092:  stfld      "int C.<M>d__0.<>1__state"
  // sequence point: <hidden>
  IL_0097:  ldarg.0
  IL_0098:  ldc.i4.0
  IL_0099:  stfld      "int C.<M>d__0.<>2__current"
  IL_009e:  ldc.i4.0
  IL_009f:  stloc.1
  IL_00a0:  ldloc.1
  IL_00a1:  ret
}
""", sequencePointDisplay: SequencePointDisplayMode.Enhanced);

            void verify(ModuleSymbol module)
            {
                var test = module.ContainingAssembly.GetTypeByMetadataName("C").GetTypeMember("<M>d__0");
                var f = test.GetMethod("MoveNextAsync");
                Assert.Equal(MethodImplAttributes.Async, f.ImplementationAttributes);
            }
        }

        [Fact]
        public void RuntimeAsync_02()
        {
            // scenario with parameter
            string source = """
using static System.Console;

await using var enumerator = C.M(42).GetAsyncEnumerator();
var found = await enumerator.MoveNextAsync();
if (!found) throw null;
var value = enumerator.Current;
Write($"{value} ");
found = await enumerator.MoveNextAsync();
if (found) throw null;
found = await enumerator.MoveNextAsync();
if (found) throw null;
Write("5");

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(int value)
    {
        Write("1 ");
        await System.Threading.Tasks.Task.CompletedTask;
        Write("2 ");
        yield return value;
        Write("4 ");
    }
}
""";
            var comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All));

            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("1 2 42 4 5"),
                symbolValidator: verifyAsyncMembersAndInterfaces, verify: Verification.Skipped);

            verifier.VerifyTypeIL("C", """
.class private auto ansi beforefieldinit C
    extends [System.Runtime]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed beforefieldinit '<M>d__0'
        extends [System.Runtime]System.Object
        implements class [System.Runtime]System.Collections.Generic.IAsyncEnumerable`1<int32>,
                   class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<int32>,
                   [System.Runtime]System.IAsyncDisposable
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field public int32 '<>1__state'
        .field private int32 '<>2__current'
        .field private bool '<>w__disposeMode'
        .field private int32 '<>l__initialThreadId'
        .field private int32 'value'
        .field public int32 '<>3__value'
        // Methods
        .method public hidebysig specialname rtspecialname
            instance void .ctor (
                int32 '<>1__state'
            ) cil managed
        {
            .custom instance void [System.Runtime]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            // Code size 26 (0x1a)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: call instance void [System.Runtime]System.Object::.ctor()
            IL_0006: nop
            IL_0007: ldarg.0
            IL_0008: ldarg.1
            IL_0009: stfld int32 C/'<M>d__0'::'<>1__state'
            IL_000e: ldarg.0
            IL_000f: call int32 [System.Runtime]System.Environment::get_CurrentManagedThreadId()
            IL_0014: stfld int32 C/'<M>d__0'::'<>l__initialThreadId'
            IL_0019: ret
        } // end of method '<M>d__0'::.ctor
        .method private final hidebysig newslot virtual
            instance class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<int32> 'System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator' (
                [opt] valuetype [System.Runtime]System.Threading.CancellationToken cancellationToken
            ) cil managed
        {
            .custom instance void [System.Runtime]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<!0> class [System.Runtime]System.Collections.Generic.IAsyncEnumerable`1<int32>::GetAsyncEnumerator(valuetype [System.Runtime]System.Threading.CancellationToken)
            .param [0]
                .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                    01 00 01 00 00
                )
            .param [1] = nullref
            // Code size 64 (0x40)
            .maxstack 2
            .locals init (
                [0] class C/'<M>d__0'
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 C/'<M>d__0'::'<>1__state'
            IL_0006: ldc.i4.s -2
            IL_0008: bne.un.s IL_002a
            IL_000a: ldarg.0
            IL_000b: ldfld int32 C/'<M>d__0'::'<>l__initialThreadId'
            IL_0010: call int32 [System.Runtime]System.Environment::get_CurrentManagedThreadId()
            IL_0015: bne.un.s IL_002a
            IL_0017: ldarg.0
            IL_0018: ldc.i4.s -3
            IL_001a: stfld int32 C/'<M>d__0'::'<>1__state'
            IL_001f: ldarg.0
            IL_0020: ldc.i4.0
            IL_0021: stfld bool C/'<M>d__0'::'<>w__disposeMode'
            IL_0026: ldarg.0
            IL_0027: stloc.0
            IL_0028: br.s IL_0032
            IL_002a: ldc.i4.s -3
            IL_002c: newobj instance void C/'<M>d__0'::.ctor(int32)
            IL_0031: stloc.0
            IL_0032: ldloc.0
            IL_0033: ldarg.0
            IL_0034: ldfld int32 C/'<M>d__0'::'<>3__value'
            IL_0039: stfld int32 C/'<M>d__0'::'value'
            IL_003e: ldloc.0
            IL_003f: ret
        } // end of method '<M>d__0'::'System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator'
        .method private final hidebysig newslot virtual
            instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<bool> MoveNextAsync () cil managed flag(2000)
        {
            .override method instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<bool> class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<int32>::MoveNextAsync()
            // Code size 180 (0xb4)
            .maxstack 3
            .locals init (
                [0] int32,
                [1] bool
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 C/'<M>d__0'::'<>1__state'
            IL_0006: stloc.0
            .try
            {
                IL_0007: ldloc.0
                IL_0008: ldc.i4.s -4
                IL_000a: beq.s IL_0015
                IL_000c: br.s IL_000e
                IL_000e: ldloc.0
                IL_000f: ldc.i4.s -3
                IL_0011: beq.s IL_0017
                IL_0013: br.s IL_0019
                IL_0015: br.s IL_0068
                IL_0017: br.s IL_0019
                IL_0019: ldarg.0
                IL_001a: ldfld bool C/'<M>d__0'::'<>w__disposeMode'
                IL_001f: brfalse.s IL_0023
                IL_0021: leave.s IL_00a1
                IL_0023: ldarg.0
                IL_0024: ldc.i4.m1
                IL_0025: dup
                IL_0026: stloc.0
                IL_0027: stfld int32 C/'<M>d__0'::'<>1__state'
                IL_002c: nop
                IL_002d: ldstr "1 "
                IL_0032: call void [System.Console]System.Console::Write(string)
                IL_0037: nop
                IL_0038: call class [System.Runtime]System.Threading.Tasks.Task [System.Runtime]System.Threading.Tasks.Task::get_CompletedTask()
                IL_003d: call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
                IL_0042: nop
                IL_0043: ldstr "2 "
                IL_0048: call void [System.Console]System.Console::Write(string)
                IL_004d: nop
                IL_004e: ldarg.0
                IL_004f: ldarg.0
                IL_0050: ldfld int32 C/'<M>d__0'::'value'
                IL_0055: stfld int32 C/'<M>d__0'::'<>2__current'
                IL_005a: ldarg.0
                IL_005b: ldc.i4.s -4
                IL_005d: dup
                IL_005e: stloc.0
                IL_005f: stfld int32 C/'<M>d__0'::'<>1__state'
                IL_0064: ldc.i4.1
                IL_0065: stloc.1
                IL_0066: leave.s IL_00b2
                IL_0068: ldarg.0
                IL_0069: ldc.i4.m1
                IL_006a: dup
                IL_006b: stloc.0
                IL_006c: stfld int32 C/'<M>d__0'::'<>1__state'
                IL_0071: ldarg.0
                IL_0072: ldfld bool C/'<M>d__0'::'<>w__disposeMode'
                IL_0077: brfalse.s IL_007b
                IL_0079: leave.s IL_00a1
                IL_007b: ldstr "4 "
                IL_0080: call void [System.Console]System.Console::Write(string)
                IL_0085: nop
                IL_0086: ldarg.0
                IL_0087: ldc.i4.1
                IL_0088: stfld bool C/'<M>d__0'::'<>w__disposeMode'
                IL_008d: leave.s IL_00a1
            } // end .try
            catch [System.Runtime]System.Exception
            {
                IL_008f: pop
                IL_0090: ldarg.0
                IL_0091: ldc.i4.s -2
                IL_0093: stfld int32 C/'<M>d__0'::'<>1__state'
                IL_0098: ldarg.0
                IL_0099: ldc.i4.0
                IL_009a: stfld int32 C/'<M>d__0'::'<>2__current'
                IL_009f: rethrow
            } // end handler
            IL_00a1: ldarg.0
            IL_00a2: ldc.i4.s -2
            IL_00a4: stfld int32 C/'<M>d__0'::'<>1__state'
            IL_00a9: ldarg.0
            IL_00aa: ldc.i4.0
            IL_00ab: stfld int32 C/'<M>d__0'::'<>2__current'
            IL_00b0: ldc.i4.0
            IL_00b1: stloc.1
            IL_00b2: ldloc.1
            IL_00b3: ret
        } // end of method '<M>d__0'::MoveNextAsync
        .method private final hidebysig specialname newslot virtual
            instance int32 'System.Collections.Generic.IAsyncEnumerator<System.Int32>.get_Current' () cil managed
        {
            .custom instance void [System.Runtime]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance !0 class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<int32>::get_Current()
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld int32 C/'<M>d__0'::'<>2__current'
            IL_0006: ret
        } // end of method '<M>d__0'::'System.Collections.Generic.IAsyncEnumerator<System.Int32>.get_Current'
        .method private final hidebysig newslot virtual
            instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync () cil managed flag(2000)
        {
            .custom instance void [System.Runtime]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask [System.Runtime]System.IAsyncDisposable::DisposeAsync()
            // Code size 46 (0x2e)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld int32 C/'<M>d__0'::'<>1__state'
            IL_0006: ldc.i4.m1
            IL_0007: blt.s IL_000f
            IL_0009: newobj instance void [System.Runtime]System.NotSupportedException::.ctor()
            IL_000e: throw
            IL_000f: ldarg.0
            IL_0010: ldfld int32 C/'<M>d__0'::'<>1__state'
            IL_0015: ldc.i4.s -2
            IL_0017: bne.un.s IL_001a
            IL_0019: ret
            IL_001a: ldarg.0
            IL_001b: ldc.i4.1
            IL_001c: stfld bool C/'<M>d__0'::'<>w__disposeMode'
            IL_0021: ldarg.0
            IL_0022: callvirt instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<bool> class [System.Runtime]System.Collections.Generic.IAsyncEnumerator`1<int32>::MoveNextAsync()
            IL_0027: call !!0 [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await<bool>(valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<!!0>)
            IL_002c: pop
            IL_002d: ret
        } // end of method '<M>d__0'::System.IAsyncDisposable.DisposeAsync
        // Properties
        .property instance int32 'System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current'()
        {
            .get instance int32 C/'<M>d__0'::'System.Collections.Generic.IAsyncEnumerator<System.Int32>.get_Current'()
        }
    } // end of class <M>d__0
    // Methods
    .method public hidebysig static
        class [System.Runtime]System.Collections.Generic.IAsyncEnumerable`1<int32> M (
            int32 'value'
        ) cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute::.ctor(class [System.Runtime]System.Type) = (
            01 00 09 43 2b 3c 4d 3e 64 5f 5f 30 00 00
        )
        // Code size 15 (0xf)
        .maxstack 8
        IL_0000: ldc.i4.s -2
        IL_0002: newobj instance void C/'<M>d__0'::.ctor(int32)
        IL_0007: dup
        IL_0008: ldarg.0
        IL_0009: stfld int32 C/'<M>d__0'::'<>3__value'
        IL_000e: ret
    } // end of method C::M
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        // Code size 8 (0x8)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    } // end of method C::.ctor
} // end of class C
""");

            static void verifyAsyncMembersAndInterfaces(ModuleSymbol module)
            {
                var type = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C.<M>d__0");
                AssertEx.SetEqual([
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                    "System.Int32 C.<M>d__0.<>2__current",
                    "System.Boolean C.<M>d__0.<>w__disposeMode",
                    "System.Int32 C.<M>d__0.<>l__initialThreadId",
                    "System.Int32 C.<M>d__0.value",
                    "System.Int32 C.<M>d__0.<>3__value",
                    "C.<M>d__0..ctor(System.Int32 <>1__state)",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32> C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)])",
                    "System.Threading.Tasks.ValueTask<System.Boolean> C.<M>d__0.MoveNextAsync()",
                    "System.Int32 C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current.get",
                    "System.Threading.Tasks.ValueTask C.<M>d__0.System.IAsyncDisposable.DisposeAsync()",
                    "System.Int32 C.<M>d__0.<>1__state"
                    ], type.GetMembersUnordered().ToTestDisplayStrings());

                AssertEx.SetEqual([
                    "System.Collections.Generic.IAsyncEnumerable<System.Int32>",
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32>",
                    "System.IAsyncDisposable"
                    ], type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys.ToTestDisplayStrings());
            }
        }

        [Fact]
        public void RuntimeAsync_03()
        {
            // scenario with yield break
            string source = """
using static System.Console;

await using var enumerator = C.M().GetAsyncEnumerator();
var found = await enumerator.MoveNextAsync();
if (found) throw null;
Write("5");

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write("1 ");
        await System.Threading.Tasks.Task.CompletedTask;

        bool b = true;
        if (b)
            yield break;

        throw null;
    }
}
""";
            var comp = CreateRuntimeAsyncCompilation(source);
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("1 5"));

            verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()", """
{
  // Code size      100 (0x64)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -3
    IL_000a:  pop
    IL_000b:  pop
    IL_000c:  ldarg.0
    IL_000d:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_0012:  brfalse.s  IL_0016
    IL_0014:  leave.s    IL_0053
    IL_0016:  ldarg.0
    IL_0017:  ldc.i4.m1
    IL_0018:  dup
    IL_0019:  stloc.0
    IL_001a:  stfld      "int C.<M>d__0.<>1__state"
    IL_001f:  ldstr      "1 "
    IL_0024:  call       "void System.Console.Write(string)"
    IL_0029:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get"
    IL_002e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
    IL_0033:  ldc.i4.1
    IL_0034:  brfalse.s  IL_003f
    IL_0036:  ldarg.0
    IL_0037:  ldc.i4.1
    IL_0038:  stfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_003d:  leave.s    IL_0053
    IL_003f:  ldnull
    IL_0040:  throw
  }
  catch System.Exception
  {
    IL_0041:  pop
    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.s   -2
    IL_0045:  stfld      "int C.<M>d__0.<>1__state"
    IL_004a:  ldarg.0
    IL_004b:  ldc.i4.0
    IL_004c:  stfld      "int C.<M>d__0.<>2__current"
    IL_0051:  rethrow
  }
  IL_0053:  ldarg.0
  IL_0054:  ldc.i4.s   -2
  IL_0056:  stfld      "int C.<M>d__0.<>1__state"
  IL_005b:  ldarg.0
  IL_005c:  ldc.i4.0
  IL_005d:  stfld      "int C.<M>d__0.<>2__current"
  IL_0062:  ldc.i4.0
  IL_0063:  ret
}
""");
        }

        [Fact]
        public void RuntimeAsync_04()
        {
            var src = """
var enumerator = local();
var found = await enumerator.MoveNextAsync();
if (!found) throw null;
System.Console.Write(enumerator.Current);

found = await enumerator.MoveNextAsync();
if (found) throw null;

async System.Collections.Generic.IAsyncEnumerator<int> local()
{
    yield return 42;
    await System.Threading.Tasks.Task.Yield();
}
""";
            var expectedOutput = "42";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.FailsPEVerify)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(src);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void DisposeCombinedTokens_01()
        {
            // CombinedTokens field is disposed/cleared when exiting normally
            string source = """
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using CancellationTokenSource source1 = new CancellationTokenSource();
CancellationToken token1 = source1.Token;
using CancellationTokenSource source2 = new CancellationTokenSource();
CancellationToken token2 = source2.Token;

var enumerable = C.M(token1, token2, token1);

var enumerator = enumerable.GetAsyncEnumerator(token2); // some other token passed

if (!await enumerator.MoveNextAsync()) throw null;
System.Console.Write($"{enumerator.Current} "); // 1
checkCombinedTokens(enumerator);

if (await enumerator.MoveNextAsync()) throw null;
System.Console.Write("end ");
checkCombinedTokens(enumerator);

await enumerator.DisposeAsync();
System.Console.Write("disposed ");
checkCombinedTokens(enumerator);

void checkCombinedTokens<T>(System.Collections.Generic.IAsyncEnumerator<T> enumerator)
{
    var type = enumerator.GetType();
    var field = type.GetField("<>x__combinedTokens", BindingFlags.Instance | BindingFlags.NonPublic);
    if (field is null) throw null;

    var combinedToken = (CancellationTokenSource)field.GetValue(enumerator);
    System.Console.Write(combinedToken is null ? "null " : "set ");
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(CancellationToken token1, CancellationToken token2, [EnumeratorCancellation] CancellationToken token3)
    {
        if (token3.Equals(token1) || token3.Equals(token2)) throw null;
        yield return 1;
        await Task.Yield();
    }
}
""";
            var expectedOutput = "1 set end null disposed null";

            var comp = CreateCompilationWithAsyncIterator([source, EnumeratorCancellationAttributeType], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();

            verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()", """
{
  // Code size      275 (0x113)
  .maxstack  3
  .locals init (int V_0,
                bool V_1,
                bool V_2,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_3,
                System.Runtime.CompilerServices.YieldAwaitable V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  beq.s      IL_0015
    IL_000c:  br.s       IL_000e
    IL_000e:  ldloc.0
    IL_000f:  ldc.i4.s   -3
    IL_0011:  beq.s      IL_0017
    IL_0013:  br.s       IL_0019
    IL_0015:  br.s       IL_0075
    IL_0017:  br.s       IL_0019
    IL_0019:  ldarg.0
    IL_001a:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_001f:  brfalse.s  IL_0026
    IL_0021:  leave      IL_00e5
    IL_0026:  ldarg.0
    IL_0027:  ldc.i4.m1
    IL_0028:  dup
    IL_0029:  stloc.0
    IL_002a:  stfld      "int C.<M>d__0.<>1__state"
    IL_002f:  nop
    IL_0030:  ldarg.0
    IL_0031:  ldflda     "System.Threading.CancellationToken C.<M>d__0.token3"
    IL_0036:  ldarg.0
    IL_0037:  ldfld      "System.Threading.CancellationToken C.<M>d__0.token1"
    IL_003c:  call       "bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)"
    IL_0041:  brtrue.s   IL_0056
    IL_0043:  ldarg.0
    IL_0044:  ldflda     "System.Threading.CancellationToken C.<M>d__0.token3"
    IL_0049:  ldarg.0
    IL_004a:  ldfld      "System.Threading.CancellationToken C.<M>d__0.token2"
    IL_004f:  call       "bool System.Threading.CancellationToken.Equals(System.Threading.CancellationToken)"
    IL_0054:  br.s       IL_0057
    IL_0056:  ldc.i4.1
    IL_0057:  stloc.1
    IL_0058:  ldloc.1
    IL_0059:  brfalse.s  IL_005d
    IL_005b:  ldnull
    IL_005c:  throw
    IL_005d:  ldarg.0
    IL_005e:  ldc.i4.1
    IL_005f:  stfld      "int C.<M>d__0.<>2__current"
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.s   -4
    IL_0067:  dup
    IL_0068:  stloc.0
    IL_0069:  stfld      "int C.<M>d__0.<>1__state"
    IL_006e:  ldc.i4.1
    IL_006f:  stloc.2
    IL_0070:  leave      IL_0111
    IL_0075:  ldarg.0
    IL_0076:  ldc.i4.m1
    IL_0077:  dup
    IL_0078:  stloc.0
    IL_0079:  stfld      "int C.<M>d__0.<>1__state"
    IL_007e:  ldarg.0
    IL_007f:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_0084:  brfalse.s  IL_0088
    IL_0086:  leave.s    IL_00e5
    IL_0088:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
    IL_008d:  stloc.s    V_4
    IL_008f:  ldloca.s   V_4
    IL_0091:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
    IL_0096:  stloc.3
    IL_0097:  ldloca.s   V_3
    IL_0099:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
    IL_009e:  brtrue.s   IL_00a7
    IL_00a0:  ldloc.3
    IL_00a1:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
    IL_00a6:  nop
    IL_00a7:  ldloca.s   V_3
    IL_00a9:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_00ae:  nop
    IL_00af:  ldarg.0
    IL_00b0:  ldc.i4.1
    IL_00b1:  stfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_00b6:  leave.s    IL_00e5
  }
  catch System.Exception
  {
    IL_00b8:  pop
    IL_00b9:  ldarg.0
    IL_00ba:  ldc.i4.s   -2
    IL_00bc:  stfld      "int C.<M>d__0.<>1__state"
    IL_00c1:  ldarg.0
    IL_00c2:  ldfld      "System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens"
    IL_00c7:  brfalse.s  IL_00dc
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      "System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens"
    IL_00cf:  callvirt   "void System.Threading.CancellationTokenSource.Dispose()"
    IL_00d4:  nop
    IL_00d5:  ldarg.0
    IL_00d6:  ldnull
    IL_00d7:  stfld      "System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens"
    IL_00dc:  ldarg.0
    IL_00dd:  ldc.i4.0
    IL_00de:  stfld      "int C.<M>d__0.<>2__current"
    IL_00e3:  rethrow
  }
  IL_00e5:  ldarg.0
  IL_00e6:  ldc.i4.s   -2
  IL_00e8:  stfld      "int C.<M>d__0.<>1__state"
  IL_00ed:  ldarg.0
  IL_00ee:  ldfld      "System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens"
  IL_00f3:  brfalse.s  IL_0108
  IL_00f5:  ldarg.0
  IL_00f6:  ldfld      "System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens"
  IL_00fb:  callvirt   "void System.Threading.CancellationTokenSource.Dispose()"
  IL_0100:  nop
  IL_0101:  ldarg.0
  IL_0102:  ldnull
  IL_0103:  stfld      "System.Threading.CancellationTokenSource C.<M>d__0.<>x__combinedTokens"
  IL_0108:  ldarg.0
  IL_0109:  ldc.i4.0
  IL_010a:  stfld      "int C.<M>d__0.<>2__current"
  IL_010f:  ldc.i4.0
  IL_0110:  stloc.2
  IL_0111:  ldloc.2
  IL_0112:  ret
}
""");
        }

        [Fact]
        public void DisposeCombinedTokens_02()
        {
            // CombinedTokens field is disposed/cleared when exiting with exception
            string source = """
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using CancellationTokenSource source1 = new CancellationTokenSource();
CancellationToken token1 = source1.Token;
using CancellationTokenSource source2 = new CancellationTokenSource();
CancellationToken token2 = source2.Token;

var enumerable = C.M(token1, token2, token1);

var enumerator = enumerable.GetAsyncEnumerator(token2); // some other token passed

if (!await enumerator.MoveNextAsync()) throw null;
System.Console.Write($"{enumerator.Current} "); // 1
checkCombinedTokens(enumerator);

try
{
    await enumerator.MoveNextAsync();
    throw null;
}
catch (System.Exception)
{
    System.Console.Write("caught ");
    checkCombinedTokens(enumerator);
}

await enumerator.DisposeAsync();
System.Console.Write("disposed ");
checkCombinedTokens(enumerator);

void checkCombinedTokens<T>(System.Collections.Generic.IAsyncEnumerator<T> enumerator)
{
    var type = enumerator.GetType();
    var field = type.GetField("<>x__combinedTokens", BindingFlags.Instance | BindingFlags.NonPublic);
    if (field is null) throw null;

    var combinedToken = (CancellationTokenSource)field.GetValue(enumerator);
    System.Console.Write(combinedToken is null ? "null " : "set ");
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(CancellationToken token1, CancellationToken token2, [EnumeratorCancellation] CancellationToken token3)
    {
        if (token3.Equals(token1) || token3.Equals(token2)) throw null;
        yield return 1;
        await Task.Yield();
        throw new System.Exception("exception ");
    }
}
""";
            var expectedOutput = "1 set caught null disposed null";

            var comp = CreateCompilationWithAsyncIterator([source, EnumeratorCancellationAttributeType], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void DisposeCombinedTokens_03()
        {
            // CombinedTokens field is disposed/cleared when exiting with yield break
            string source = """
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using CancellationTokenSource source1 = new CancellationTokenSource();
CancellationToken token1 = source1.Token;
using CancellationTokenSource source2 = new CancellationTokenSource();
CancellationToken token2 = source2.Token;

var enumerable = C.M(token1, token2, token1);

var enumerator = enumerable.GetAsyncEnumerator(token2); // some other token passed

if (!await enumerator.MoveNextAsync()) throw null;
System.Console.Write($"{enumerator.Current} "); // 1
checkCombinedTokens(enumerator);

if (await enumerator.MoveNextAsync()) throw null;
System.Console.Write("end ");
checkCombinedTokens(enumerator);

await enumerator.DisposeAsync();
System.Console.Write("disposed ");
checkCombinedTokens(enumerator);

void checkCombinedTokens<T>(System.Collections.Generic.IAsyncEnumerator<T> enumerator)
{
    var type = enumerator.GetType();
    var field = type.GetField("<>x__combinedTokens", BindingFlags.Instance | BindingFlags.NonPublic);
    if (field is null) throw null;

    var combinedToken = (CancellationTokenSource)field.GetValue(enumerator);
    System.Console.Write(combinedToken is null ? "null " : "set ");
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(CancellationToken token1, CancellationToken token2, [EnumeratorCancellation] CancellationToken token3)
    {
        if (token3.Equals(token1) || token3.Equals(token2)) throw null;
        yield return 1;
        await Task.Yield();
        bool b = true;
        if (b) yield break;

        throw null;
    }
}
""";
            var expectedOutput = "1 set end null disposed null";

            var comp = CreateCompilationWithAsyncIterator([source, EnumeratorCancellationAttributeType], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void ObserveState_01()
        {
            // Observe state when enumeration terminates normally
            string source = """
using System.Reflection;
using System.Threading.Tasks;

var enumerable = C.M();

var enumerator = enumerable.GetAsyncEnumerator();
checkState(enumerator);

if (!await enumerator.MoveNextAsync()) throw null;
System.Console.Write($"{enumerator.Current} "); // 1
checkState(enumerator);

if (await enumerator.MoveNextAsync()) throw null;
System.Console.Write("end ");
checkState(enumerator);

await enumerator.DisposeAsync();
System.Console.Write("disposed ");
checkState(enumerator);

void checkState<T>(System.Collections.Generic.IAsyncEnumerator<T> enumerator)
{
    var type = enumerator.GetType();
    var field = type.GetField("<>1__state", BindingFlags.Instance | BindingFlags.Public);
    if (field is null) throw null;

    System.Console.Write($"{field.GetValue(enumerator)} ");
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        await Task.Yield();
    }
}
""";
            var expectedOutput = "-3 1 -4 end -2 disposed -2";

            var comp = CreateCompilationWithAsyncIterator([source, EnumeratorCancellationAttributeType], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void ObserveState_02()
        {
            // Observe state when enumeration terminates with exception
            string source = """
using System.Reflection;
using System.Threading.Tasks;

var enumerable = C.M();
var enumerator = enumerable.GetAsyncEnumerator();
checkState(enumerator);

if (!await enumerator.MoveNextAsync()) throw null;
System.Console.Write($"{enumerator.Current} "); // 1
checkState(enumerator);

try
{
    await enumerator.MoveNextAsync();
    throw null;
}
catch (System.Exception)
{
    System.Console.Write("caught ");
    checkState(enumerator);
}

await enumerator.DisposeAsync();
System.Console.Write("disposed ");
checkState(enumerator);

void checkState<T>(System.Collections.Generic.IAsyncEnumerator<T> enumerator)
{
    var type = enumerator.GetType();
    var field = type.GetField("<>1__state", BindingFlags.Instance | BindingFlags.Public);
    if (field is null) throw null;

    System.Console.Write($"{field.GetValue(enumerator)} ");
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        await Task.Yield();
        throw new System.Exception("exception ");
    }
}
""";
            var expectedOutput = "-3 1 -4 caught -2 disposed -2";

            var comp = CreateCompilationWithAsyncIterator([source, EnumeratorCancellationAttributeType], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void ObserveState_03()
        {
            // Observe state when enumeration terminates with yield break
            string source = """
using System.Reflection;
using System.Threading.Tasks;

var enumerable = C.M();
var enumerator = enumerable.GetAsyncEnumerator();
checkState(enumerator);

if (!await enumerator.MoveNextAsync()) throw null;
System.Console.Write($"{enumerator.Current} "); // 1
checkState(enumerator);

if (await enumerator.MoveNextAsync()) throw null;
System.Console.Write("end ");
checkState(enumerator);

await enumerator.DisposeAsync();
System.Console.Write("disposed ");
checkState(enumerator);

void checkState<T>(System.Collections.Generic.IAsyncEnumerator<T> enumerator)
{
    var type = enumerator.GetType();
    var field = type.GetField("<>1__state", BindingFlags.Instance | BindingFlags.Public);
    if (field is null) throw null;

    System.Console.Write($"{field.GetValue(enumerator)} ");
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        await Task.Yield();
        bool b = true;
        if (b) yield break;

        throw null;
    }
}
""";
            var expectedOutput = "-3 1 -4 end -2 disposed -2";

            var comp = CreateCompilationWithAsyncIterator([source, EnumeratorCancellationAttributeType], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void ObserveState_04()
        {
            // Observe state when enumeration terminates with early disposal
            string source = """
using System.Reflection;

var enumerable = C.M();
var enumerator = enumerable.GetAsyncEnumerator();
checkState(enumerator);

if (!await enumerator.MoveNextAsync()) throw null;
System.Console.Write($"{enumerator.Current} "); // 1
checkState(enumerator);

await enumerator.DisposeAsync();
System.Console.Write("disposed ");
checkState(enumerator);

void checkState<T>(System.Collections.Generic.IAsyncEnumerator<T> enumerator)
{
    var type = enumerator.GetType();
    var field = type.GetField("<>1__state", BindingFlags.Instance | BindingFlags.Public);
    if (field is null) throw null;

    System.Console.Write($"{field.GetValue(enumerator)} ");
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        throw null;
    }
}
""";
            var expectedOutput = "-3 1 -4 disposed -2";

            var comp = CreateCompilationWithAsyncIterator([source, EnumeratorCancellationAttributeType], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void ObserveState_05()
        {
            // Observe state when running
            string source = """
using System.Reflection;
using System.Threading.Tasks;

var tcs = new System.Threading.Tasks.TaskCompletionSource();
var enumerable = C.M(tcs.Task);
var enumerator = enumerable.GetAsyncEnumerator();
checkState(enumerator);

if (!await enumerator.MoveNextAsync()) throw null;
System.Console.Write($"{enumerator.Current} "); // 1
checkState(enumerator);

var promise = enumerator.MoveNextAsync();
System.Console.Write("waiting ");
checkState(enumerator);

tcs.SetResult();
if (await promise) throw null;
System.Console.Write("done ");
checkState(enumerator);

await enumerator.DisposeAsync();
System.Console.Write("disposed ");
checkState(enumerator);

void checkState<T>(System.Collections.Generic.IAsyncEnumerator<T> enumerator)
{
    var type = enumerator.GetType();
    var field = type.GetField("<>1__state", BindingFlags.Instance | BindingFlags.Public);
    if (field is null) throw null;

    System.Console.Write($"{field.GetValue(enumerator)} ");
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(Task task)
    {
        yield return 1;
        await task;
    }
}
""";

            // Note: states for await suspensions are increasing from 0
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput("-3 1 -4 waiting 0 done -2 disposed -2"), verify: Verification.Skipped)
                .VerifyDiagnostics();

            // Note: the state machine is in running state when in an await
            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("-3 1 -4 waiting -1 done -2 disposed -2"), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void ObserveState_06()
        {
            // Observe state when running during early disposal
            string source = """
using System.Reflection;
using System.Threading.Tasks;

var tcs = new TaskCompletionSource();
var enumerable = C.M(tcs.Task);
var enumerator = enumerable.GetAsyncEnumerator();
checkState(enumerator);

if (!await enumerator.MoveNextAsync()) throw null;
System.Console.Write($"{enumerator.Current} "); // 1
checkState(enumerator);

var promise = enumerator.DisposeAsync();
System.Console.Write("waiting ");
checkState(enumerator);

tcs.SetResult();
await promise;
System.Console.Write("disposed ");
checkState(enumerator);

void checkState<T>(System.Collections.Generic.IAsyncEnumerator<T> enumerator)
{
    var type = enumerator.GetType();
    var field = type.GetField("<>1__state", BindingFlags.Instance | BindingFlags.Public);
    if (field is null) throw null;

    System.Console.Write($"{field.GetValue(enumerator)} ");
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(Task task)
    {
        try
        {
            yield return 1;
            throw null;
        }
        finally
        {
            await task;
        }

        throw null;
    }
}
""";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput("-3 1 -4 waiting 0 disposed -2"), verify: Verification.Skipped)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("-3 1 -4 waiting -1 disposed -2"), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void ObserveState_07()
        {
            // Observe state when enumeration terminates normally, enumerator
            string source = """
using System.Reflection;
using System.Threading.Tasks;

var enumerator = C.M();
checkState(enumerator);

if (!await enumerator.MoveNextAsync()) throw null;
System.Console.Write($"{enumerator.Current} "); // 1
checkState(enumerator);

if (await enumerator.MoveNextAsync()) throw null;
System.Console.Write("end ");
checkState(enumerator);

await enumerator.DisposeAsync();
System.Console.Write("disposed ");
checkState(enumerator);

void checkState<T>(System.Collections.Generic.IAsyncEnumerator<T> enumerator)
{
    var type = enumerator.GetType();
    var field = type.GetField("<>1__state", BindingFlags.Instance | BindingFlags.Public);
    if (field is null) throw null;

    System.Console.Write($"{field.GetValue(enumerator)} ");
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerator<int> M()
    {
        yield return 1;
        await Task.Yield();
    }
}
""";
            var expectedOutput = "-3 1 -4 end -2 disposed -2";

            var comp = CreateCompilationWithAsyncIterator([source, EnumeratorCancellationAttributeType], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void RefHoisting_01()
        {
            // an await in the middle of a compound assignment involving a ref temp
            string source = """
using System.Threading.Tasks;

await foreach (var i in C.M()) 
{
    System.Console.Write(i);
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        var array = new int[] { 42 };
        GetArray(array)[await GetIndexAsync()] += await GetIncrementAsync();
        yield return array[0];
    }

    static int[] GetArray(int[] array)
    {
        System.Console.Write("GetArray() "); 
        return array;
    }

    static async Task<int> GetIndexAsync()
    {
        System.Console.Write("GetIndexAsync() "); 
        await Task.Yield();
        return 0;
    }

    static async Task<int> GetIncrementAsync()
    {
        System.Console.Write("GetIncrementAsync() "); 
        await Task.Yield();
        return 1;
    }
}
""";
            var expectedOutput = "GetArray() GetIndexAsync() GetIncrementAsync() 43";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Theory]
        [InlineData("DebuggerHiddenAttribute")]
        [InlineData("DebuggerNonUserCodeAttribute")]
        [InlineData("DebuggerStepperBoundaryAttribute")]
        [InlineData("DebuggerStepThroughAttribute")]
        public void KickoffMethodAttributes_01(string attribute)
        {
            string source = $$"""
class C
{
    [System.Diagnostics.{{attribute}}]
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        await System.Threading.Tasks.Task.Yield();
    }
}
""";

            var comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, verify: Verification.Skipped, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetTypeMember("C").GetTypeMember("<M>d__0").GetMethod("MoveNextAsync");
                AssertEx.SetEqual([attribute], GetAttributeNames(method.GetAttributes()));
            });
        }

        [Fact]
        public void KickoffMethodAttributes_02()
        {
            string source = """
class C
{
    [My]
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        yield return 1;
        await System.Threading.Tasks.Task.Yield();
    }
}
class MyAttribute : System.Attribute { }
""";

            var comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, verify: Verification.Skipped, symbolValidator: module =>
            {
                var moveNextMethod = module.GlobalNamespace.GetTypeMember("C").GetTypeMember("<M>d__0").GetMethod("MoveNextAsync");
                Assert.Empty(moveNextMethod.GetAttributes());

                var mMethod = module.GlobalNamespace.GetTypeMember("C").GetMethod("M");
                AssertEx.SetEqual(["System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute(typeof(C.<M>d__0))", "MyAttribute"], mMethod.GetAttributes().ToStrings());
            });
        }

        [Fact]
        public void Lock_01()
        {
            // yield return in lock body, with Lock type
            string source = """
class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(System.Threading.Lock l)
    {
        lock (l)
        {
            System.Console.Write(1);
            yield return 2;
            System.Console.Write(3);
        }

        await System.Threading.Tasks.Task.Yield();
    }
}
""";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            comp.VerifyEmitDiagnostics(
                // (5,15): error CS4007: Instance of type 'System.Threading.Lock.Scope' cannot be preserved across 'await' or 'yield' boundary.
                //         lock (l)
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "l").WithArguments("System.Threading.Lock.Scope").WithLocation(5, 15));

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,15): error CS4007: Instance of type 'System.Threading.Lock.Scope' cannot be preserved across 'await' or 'yield' boundary.
                //         lock (l)
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "l").WithArguments("System.Threading.Lock.Scope").WithLocation(5, 15));
        }

        [Fact]
        public void Lock_02()
        {
            // yield return in lock body, with object type, async-enumerable
            string source = """
object o = new object();
await foreach (var i in C.M(o)) 
{
    System.Console.Write(i);
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(object o)
    {
        lock (o)
        {
            System.Console.Write(1);
            yield return 2;
            System.Console.Write(3);
        }

        await System.Threading.Tasks.Task.Yield();
    }
}

namespace System.Threading
{
    public class Monitor
    {
        public static void Enter(object obj, ref bool lockTaken)
        {
            System.Console.Write("Enter ");
            lockTaken = true;
        }

        public static void Exit(object obj)
        {
            System.Console.Write(" Exit");
        }
    }
}
""";

            var expectedOutput = "Enter 123 Exit";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            var verifier = CompileAndVerify(comp, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();

            verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      406 (0x196)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                C.<M>d__0 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  sub
    IL_000b:  switch    (
        IL_004d,
        IL_0024,
        IL_0024,
        IL_0024,
        IL_0104)
    IL_0024:  ldarg.0
    IL_0025:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_002a:  brfalse.s  IL_0031
    IL_002c:  leave      IL_015b
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.m1
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      "int C.<M>d__0.<>1__state"
    IL_003a:  ldarg.0
    IL_003b:  ldarg.0
    IL_003c:  ldfld      "object C.<M>d__0.o"
    IL_0041:  stfld      "object C.<M>d__0.<>7__wrap1"
    IL_0046:  ldarg.0
    IL_0047:  ldc.i4.0
    IL_0048:  stfld      "bool C.<M>d__0.<>7__wrap2"
    IL_004d:  nop
    .try
    {
      IL_004e:  ldloc.0
      IL_004f:  ldc.i4.s   -4
      IL_0051:  beq.s      IL_0080
      IL_0053:  ldarg.0
      IL_0054:  ldfld      "object C.<M>d__0.<>7__wrap1"
      IL_0059:  ldarg.0
      IL_005a:  ldflda     "bool C.<M>d__0.<>7__wrap2"
      IL_005f:  call       "void System.Threading.Monitor.Enter(object, ref bool)"
      IL_0064:  ldc.i4.1
      IL_0065:  call       "void System.Console.Write(int)"
      IL_006a:  ldarg.0
      IL_006b:  ldc.i4.2
      IL_006c:  stfld      "int C.<M>d__0.<>2__current"
      IL_0071:  ldarg.0
      IL_0072:  ldc.i4.s   -4
      IL_0074:  dup
      IL_0075:  stloc.0
      IL_0076:  stfld      "int C.<M>d__0.<>1__state"
      IL_007b:  leave      IL_0189
      IL_0080:  ldarg.0
      IL_0081:  ldc.i4.m1
      IL_0082:  dup
      IL_0083:  stloc.0
      IL_0084:  stfld      "int C.<M>d__0.<>1__state"
      IL_0089:  ldarg.0
      IL_008a:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
      IL_008f:  brfalse.s  IL_0093
      IL_0091:  leave.s    IL_00b3
      IL_0093:  ldc.i4.3
      IL_0094:  call       "void System.Console.Write(int)"
      IL_0099:  leave.s    IL_00b3
    }
    finally
    {
      IL_009b:  ldloc.0
      IL_009c:  ldc.i4.m1
      IL_009d:  bne.un.s   IL_00b2
      IL_009f:  ldarg.0
      IL_00a0:  ldfld      "bool C.<M>d__0.<>7__wrap2"
      IL_00a5:  brfalse.s  IL_00b2
      IL_00a7:  ldarg.0
      IL_00a8:  ldfld      "object C.<M>d__0.<>7__wrap1"
      IL_00ad:  call       "void System.Threading.Monitor.Exit(object)"
      IL_00b2:  endfinally
    }
    IL_00b3:  ldarg.0
    IL_00b4:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_00b9:  brfalse.s  IL_00c0
    IL_00bb:  leave      IL_015b
    IL_00c0:  ldarg.0
    IL_00c1:  ldnull
    IL_00c2:  stfld      "object C.<M>d__0.<>7__wrap1"
    IL_00c7:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
    IL_00cc:  stloc.2
    IL_00cd:  ldloca.s   V_2
    IL_00cf:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
    IL_00d4:  stloc.1
    IL_00d5:  ldloca.s   V_1
    IL_00d7:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
    IL_00dc:  brtrue.s   IL_0120
    IL_00de:  ldarg.0
    IL_00df:  ldc.i4.0
    IL_00e0:  dup
    IL_00e1:  stloc.0
    IL_00e2:  stfld      "int C.<M>d__0.<>1__state"
    IL_00e7:  ldarg.0
    IL_00e8:  ldloc.1
    IL_00e9:  stfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<M>d__0.<>u__1"
    IL_00ee:  ldarg.0
    IL_00ef:  stloc.3
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
    IL_00f6:  ldloca.s   V_1
    IL_00f8:  ldloca.s   V_3
    IL_00fa:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<M>d__0)"
    IL_00ff:  leave      IL_0195
    IL_0104:  ldarg.0
    IL_0105:  ldfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<M>d__0.<>u__1"
    IL_010a:  stloc.1
    IL_010b:  ldarg.0
    IL_010c:  ldflda     "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<M>d__0.<>u__1"
    IL_0111:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0117:  ldarg.0
    IL_0118:  ldc.i4.m1
    IL_0119:  dup
    IL_011a:  stloc.0
    IL_011b:  stfld      "int C.<M>d__0.<>1__state"
    IL_0120:  ldloca.s   V_1
    IL_0122:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_0127:  leave.s    IL_015b
  }
  catch System.Exception
  {
    IL_0129:  stloc.s    V_4
    IL_012b:  ldarg.0
    IL_012c:  ldc.i4.s   -2
    IL_012e:  stfld      "int C.<M>d__0.<>1__state"
    IL_0133:  ldarg.0
    IL_0134:  ldnull
    IL_0135:  stfld      "object C.<M>d__0.<>7__wrap1"
    IL_013a:  ldarg.0
    IL_013b:  ldc.i4.0
    IL_013c:  stfld      "int C.<M>d__0.<>2__current"
    IL_0141:  ldarg.0
    IL_0142:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
    IL_0147:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
    IL_014c:  ldarg.0
    IL_014d:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
    IL_0152:  ldloc.s    V_4
    IL_0154:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
    IL_0159:  leave.s    IL_0195
  }
  IL_015b:  ldarg.0
  IL_015c:  ldc.i4.s   -2
  IL_015e:  stfld      "int C.<M>d__0.<>1__state"
  IL_0163:  ldarg.0
  IL_0164:  ldnull
  IL_0165:  stfld      "object C.<M>d__0.<>7__wrap1"
  IL_016a:  ldarg.0
  IL_016b:  ldc.i4.0
  IL_016c:  stfld      "int C.<M>d__0.<>2__current"
  IL_0171:  ldarg.0
  IL_0172:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder"
  IL_0177:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
  IL_017c:  ldarg.0
  IL_017d:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
  IL_0182:  ldc.i4.0
  IL_0183:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_0188:  ret
  IL_0189:  ldarg.0
  IL_018a:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd"
  IL_018f:  ldc.i4.1
  IL_0190:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_0195:  ret
}
""");

            comp = CreateRuntimeAsyncCompilation(source);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();

            verifier.VerifyIL("C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()", """
{
  // Code size      275 (0x113)
  .maxstack  3
  .locals init (int V_0,
                bool V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  beq.s      IL_003a
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.s   -3
    IL_000f:  pop
    IL_0010:  pop
    IL_0011:  ldarg.0
    IL_0012:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_0017:  brfalse.s  IL_001e
    IL_0019:  leave      IL_00f9
    IL_001e:  ldarg.0
    IL_001f:  ldc.i4.m1
    IL_0020:  dup
    IL_0021:  stloc.0
    IL_0022:  stfld      "int C.<M>d__0.<>1__state"
    IL_0027:  ldarg.0
    IL_0028:  ldarg.0
    IL_0029:  ldfld      "object C.<M>d__0.o"
    IL_002e:  stfld      "object C.<M>d__0.<>7__wrap1"
    IL_0033:  ldarg.0
    IL_0034:  ldc.i4.0
    IL_0035:  stfld      "bool C.<M>d__0.<>7__wrap2"
    IL_003a:  nop
    .try
    {
      IL_003b:  ldloc.0
      IL_003c:  ldc.i4.s   -4
      IL_003e:  beq.s      IL_006f
      IL_0040:  ldarg.0
      IL_0041:  ldfld      "object C.<M>d__0.<>7__wrap1"
      IL_0046:  ldarg.0
      IL_0047:  ldflda     "bool C.<M>d__0.<>7__wrap2"
      IL_004c:  call       "void System.Threading.Monitor.Enter(object, ref bool)"
      IL_0051:  ldc.i4.1
      IL_0052:  call       "void System.Console.Write(int)"
      IL_0057:  ldarg.0
      IL_0058:  ldc.i4.2
      IL_0059:  stfld      "int C.<M>d__0.<>2__current"
      IL_005e:  ldarg.0
      IL_005f:  ldc.i4.s   -4
      IL_0061:  dup
      IL_0062:  stloc.0
      IL_0063:  stfld      "int C.<M>d__0.<>1__state"
      IL_0068:  ldc.i4.1
      IL_0069:  stloc.1
      IL_006a:  leave      IL_0111
      IL_006f:  ldarg.0
      IL_0070:  ldc.i4.m1
      IL_0071:  dup
      IL_0072:  stloc.0
      IL_0073:  stfld      "int C.<M>d__0.<>1__state"
      IL_0078:  ldarg.0
      IL_0079:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
      IL_007e:  brfalse.s  IL_0082
      IL_0080:  leave.s    IL_00a2
      IL_0082:  ldc.i4.3
      IL_0083:  call       "void System.Console.Write(int)"
      IL_0088:  leave.s    IL_00a2
    }
    finally
    {
      IL_008a:  ldloc.0
      IL_008b:  ldc.i4.m1
      IL_008c:  bne.un.s   IL_00a1
      IL_008e:  ldarg.0
      IL_008f:  ldfld      "bool C.<M>d__0.<>7__wrap2"
      IL_0094:  brfalse.s  IL_00a1
      IL_0096:  ldarg.0
      IL_0097:  ldfld      "object C.<M>d__0.<>7__wrap1"
      IL_009c:  call       "void System.Threading.Monitor.Exit(object)"
      IL_00a1:  endfinally
    }
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_00a8:  brfalse.s  IL_00ac
    IL_00aa:  leave.s    IL_00f9
    IL_00ac:  ldarg.0
    IL_00ad:  ldnull
    IL_00ae:  stfld      "object C.<M>d__0.<>7__wrap1"
    IL_00b3:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
    IL_00b8:  stloc.3
    IL_00b9:  ldloca.s   V_3
    IL_00bb:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
    IL_00c0:  stloc.2
    IL_00c1:  ldloca.s   V_2
    IL_00c3:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
    IL_00c8:  brtrue.s   IL_00d0
    IL_00ca:  ldloc.2
    IL_00cb:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
    IL_00d0:  ldloca.s   V_2
    IL_00d2:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_00d7:  ldarg.0
    IL_00d8:  ldc.i4.1
    IL_00d9:  stfld      "bool C.<M>d__0.<>w__disposeMode"
    IL_00de:  leave.s    IL_00f9
  }
  catch System.Exception
  {
    IL_00e0:  pop
    IL_00e1:  ldarg.0
    IL_00e2:  ldc.i4.s   -2
    IL_00e4:  stfld      "int C.<M>d__0.<>1__state"
    IL_00e9:  ldarg.0
    IL_00ea:  ldnull
    IL_00eb:  stfld      "object C.<M>d__0.<>7__wrap1"
    IL_00f0:  ldarg.0
    IL_00f1:  ldc.i4.0
    IL_00f2:  stfld      "int C.<M>d__0.<>2__current"
    IL_00f7:  rethrow
  }
  IL_00f9:  ldarg.0
  IL_00fa:  ldc.i4.s   -2
  IL_00fc:  stfld      "int C.<M>d__0.<>1__state"
  IL_0101:  ldarg.0
  IL_0102:  ldnull
  IL_0103:  stfld      "object C.<M>d__0.<>7__wrap1"
  IL_0108:  ldarg.0
  IL_0109:  ldc.i4.0
  IL_010a:  stfld      "int C.<M>d__0.<>2__current"
  IL_010f:  ldc.i4.0
  IL_0110:  stloc.1
  IL_0111:  ldloc.1
  IL_0112:  ret
}
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Iterator)]
        public void Lock_03()
        {
            // yield return in lock body, with object type, enumerable
            string source = """
object o = new object();
foreach (var i in C.M(o)) 
{
    System.Console.Write(i);
}

class C
{
    public static System.Collections.Generic.IEnumerable<int> M(object o)
    {
        lock (o)
        {
            System.Console.Write(1);
            yield return 2;
            System.Console.Write(3);
        }
    }
}

namespace System.Threading
{
    public class Monitor
    {
        public static void Enter(object obj, ref bool lockTaken)
        {
            System.Console.Write("Enter ");
            lockTaken = true;
        }

        public static void Exit(object obj)
        {
            System.Console.Write(" Exit");
        }
    }
}
""";

            var expectedOutput = "Enter 123 Exit";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void Lock_04()
        {
            // yield break in lock body
            string source = """
object o = new object();
await foreach (var i in C.M(o)) 
{
    throw null;
}

System.Console.Write(2);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(object o)
    {
        await System.Threading.Tasks.Task.Yield();

        lock (o)
        {
            bool b = true;
            if (b)
            {
                System.Console.Write(1);
                yield break;
            }

            throw null;
        }
    }
}
""";

            var expected = "12";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void Lock_05()
        {
            // await in lock body
            string source = """
class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(object o)
    {
        lock (o)
        {
            await System.Threading.Tasks.Task.Yield();
        }

        yield return 0;
    }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS1996: Cannot await in the body of a lock statement
                //             await System.Threading.Tasks.Task.Yield();
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await System.Threading.Tasks.Task.Yield()").WithLocation(7, 13));

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS1996: Cannot await in the body of a lock statement
                //             await System.Threading.Tasks.Task.Yield();
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await System.Threading.Tasks.Task.Yield()").WithLocation(7, 13));
        }

        [Fact]
        public void Using_01()
        {
            // yield return in using, normal exit
            string source = """
D d = new D();
await foreach (var i in C.M(d))
{
    System.Console.Write(i);
}

System.Console.Write(5);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(System.IDisposable d)
    {
        using (d)
        {
            System.Console.Write(1);
            yield return 2;
            System.Console.Write(3);
        }

        await System.Threading.Tasks.Task.Yield();
    }
}

class D : System.IDisposable
{
    public void Dispose()
    {
        System.Console.Write(4);
    }
}
""";

            var expected = "12345";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void Using_02()
        {
            // yield return in using, break
            string source = """
D d = new D();
await foreach (var i in C.M(d))
{
    System.Console.Write(i);
    break;
}

System.Console.Write(4);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(System.IDisposable d)
    {
        await System.Threading.Tasks.Task.Yield();

        using (d)
        {
            System.Console.Write(1);
            yield return 2;
            throw null;
        }
    }
}

class D : System.IDisposable
{
    public void Dispose()
    {
        System.Console.Write(3);
    }
}
""";

            var expected = "1234";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void Using_03()
        {
            // yield return in using, throw
            string source = """
D d = new D();
try
{
    await foreach (var i in C.M(d))
    {
        break;
    }
}
catch (System.Exception)
{
    System.Console.Write(3);
    return;
}

throw null;

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(System.IDisposable d)
    {
        using (d)
        {
            bool b = true;
            if (b)
            {
                System.Console.Write(1);
                throw new System.Exception();
            }

            yield return 2;
        }

        await System.Threading.Tasks.Task.Yield();
    }
}

class D : System.IDisposable
{
    public void Dispose()
    {
        System.Console.Write(2);
    }
}
""";

            var expected = "123";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void Using_04()
        {
            // yield break in using
            string source = """
System.IDisposable d = new D();
await foreach (var i in C.M(d)) 
{
    throw null;
}

System.Console.Write(3);

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(System.IDisposable d)
    {
        await System.Threading.Tasks.Task.Yield();

        using (d)
        {
            bool b = true;
            if (b)
            {
                System.Console.Write(1);
                yield break;
            }

            throw null;
        }

        throw null;
    }
}

class D : System.IDisposable
{
    public void Dispose()
    {
        System.Console.Write(2);
    }
}
""";

            var expected = "123";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void Using_05()
        {
            // await in using
            string source = """
System.IDisposable d = new D();
await foreach (var i in C.M(d)) 
{
    System.Console.Write(i);
}

class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M(System.IDisposable d)
    {
        using (d)
        {
            System.Console.Write(1);
            await System.Threading.Tasks.Task.Yield();
            System.Console.Write(2);
        }

        yield return 4;
    }
}

class D : System.IDisposable
{
    public void Dispose()
    {
        System.Console.Write(3);
    }
}
""";

            var expected = "1234";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            CompileAndVerify(comp, expectedOutput: ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();

            comp = CreateRuntimeAsyncCompilation(source);
            CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expected), verify: Verification.Skipped)
                .VerifyDiagnostics();
        }
    }
}
