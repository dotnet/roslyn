// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
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
            var comp = CreateCompilationWithAsyncIterator(@"
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
}", options: TestOptions.DebugExe);

            var v = CompileAndVerify(comp, expectedOutput: "hello world");
            v.VerifyIL("C.<GetSplits>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      254 (0xfe)
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
    IL_0018:  leave      IL_00d4
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
    IL_009e:  leave.s    IL_00d4
    IL_00a0:  ldarg.0
    IL_00a1:  ldc.i4.1
    IL_00a2:  stfld      ""bool C.<GetSplits>d__1.<>w__disposeMode""
    IL_00a7:  leave.s    IL_00d4
  }
  catch System.Exception
  {
    IL_00a9:  stloc.1
    IL_00aa:  ldarg.0
    IL_00ab:  ldc.i4.s   -2
    IL_00ad:  stfld      ""int C.<GetSplits>d__1.<>1__state""
    IL_00b2:  ldarg.0
    IL_00b3:  ldnull
    IL_00b4:  stfld      ""string C.<GetSplits>d__1.<>2__current""
    IL_00b9:  ldarg.0
    IL_00ba:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<GetSplits>d__1.<>t__builder""
    IL_00bf:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_00c4:  nop
    IL_00c5:  ldarg.0
    IL_00c6:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<GetSplits>d__1.<>v__promiseOfValueOrEnd""
    IL_00cb:  ldloc.1
    IL_00cc:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_00d1:  nop
    IL_00d2:  leave.s    IL_00fd
  }
  IL_00d4:  ldarg.0
  IL_00d5:  ldc.i4.s   -2
  IL_00d7:  stfld      ""int C.<GetSplits>d__1.<>1__state""
  IL_00dc:  ldarg.0
  IL_00dd:  ldnull
  IL_00de:  stfld      ""string C.<GetSplits>d__1.<>2__current""
  IL_00e3:  ldarg.0
  IL_00e4:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<GetSplits>d__1.<>t__builder""
  IL_00e9:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_00ee:  nop
  IL_00ef:  ldarg.0
  IL_00f0:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<GetSplits>d__1.<>v__promiseOfValueOrEnd""
  IL_00f5:  ldc.i4.0
  IL_00f6:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_00fb:  nop
  IL_00fc:  ret
  IL_00fd:  ret
}");
        }

        [Fact]
        [WorkItem(38961, "https://github.com/dotnet/roslyn/issues/38961")]
        public void FinallyInsideFinally()
        {
            var comp = CreateCompilationWithAsyncIterator(@"
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
}", options: TestOptions.DebugExe);

            var v = CompileAndVerify(comp, expectedOutput: "hello world!");
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

        [ConditionalFact(typeof(DesktopOnly))]
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
            CompileAndVerify(comp, expectedOutput: @"
2
8");
        }

        [ConditionalFact(typeof(DesktopOnly))]
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
            CompileAndVerify(comp, expectedOutput: @"
2
8");
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
            CompileAndVerify(comp, expectedOutput: @"42");
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
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "M").WithArguments("System.Collections.Generic.IAsyncEnumerable<T>", "T", "S").WithLocation(4, 65)
            };

            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // source(4,65): error CS9244: The type 'S' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'IAsyncEnumerable<T>'
                //     static async System.Collections.Generic.IAsyncEnumerable<S> M()
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "M").WithArguments("System.Collections.Generic.IAsyncEnumerable<T>", "T", "S").WithLocation(4, 65),
                // source(11,24): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         await foreach (var s in M())
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "var").WithArguments("ref and unsafe in async and iterator methods").WithLocation(11, 24));
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
                // source(8,24): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         await foreach (var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "var").WithArguments("ref and unsafe in async and iterator methods").WithLocation(8, 24));

            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "123", verify: Verification.FailsILVerify);
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
                // source(8,24): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         await foreach (var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "var").WithArguments("ref and unsafe in async and iterator methods").WithLocation(8, 24));

            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "123", verify: Verification.FailsILVerify);
            verifier.VerifyDiagnostics();
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
                // source(8,24): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         await foreach (var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "var").WithArguments("ref and unsafe in async and iterator methods").WithLocation(8, 24));

            var expectedDiagnostics = new[]
            {
                // source(11,34): error CS4007: Instance of type 'S' cannot be preserved across 'await' or 'yield' boundary.
                //             System.Console.Write(s.F);
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "s.F").WithArguments("S").WithLocation(11, 34)
            };

            comp = CreateCompilationWithAsyncIterator(source, parseOptions: TestOptions.RegularNext);
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
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.M");
                AssertEx.SetEqual(new[] { "AsyncIteratorStateMachineAttribute" },
                    GetAttributeNames(method.GetAttributes()));

                var attribute = method.GetAttributes().Single();
                var argument = attribute.ConstructorArguments.Single();
                Assert.Equal("System.Type", argument.Type.ToTestDisplayString());
                Assert.Equal("C.<M>d__0", ((ITypeSymbol)argument.Value).ToTestDisplayString());
            });
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
                    type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys.Select(m => m.ToTestDisplayString()));
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
                // (4,60): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 60),
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
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (4,74): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 74)
                );
            comp.VerifyEmitDiagnostics(
                // (4,74): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 74),
                // (4,74): error CS8420: The body of an async-iterator method must contain a 'yield' statement. Consider removing 'async' from the method declaration or adding a 'yield' statement.
                //     public static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_PossibleAsyncIteratorWithoutYieldOrAwait, "M").WithLocation(4, 74)
                );

            var m = comp.SourceModule.GlobalNamespace.GetMember<MethodSymbol>("C.M");
            Assert.False(m.IsIterator);
            Assert.True(m.IsAsync);
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
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (4,74): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 74)
                );
            comp.VerifyEmitDiagnostics(
                // (4,74): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 74),
                // (4,74): error CS8420: The body of an async-iterator method must contain a 'yield' statement. Consider removing `async` from the method declaration.
                //     public static async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_PossibleAsyncIteratorWithoutYieldOrAwait, "M").WithLocation(4, 74)
                );
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
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (4,60): error CS8419: The body of an async-iterator method must contain a 'yield' statement.
                //     async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_PossibleAsyncIteratorWithoutYield, "M").WithLocation(4, 60)
                );
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
            var comp = CreateCompilationWithAsyncIterator(source);
            comp.VerifyDiagnostics(
                // (4,60): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 60)
                );
            comp.VerifyEmitDiagnostics(
                // (4,60): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 60),
                // (4,60): error CS8420: The body of an async-iterator method must contain a 'yield' statement. Consider removing `async` from the method declaration.
                //     async System.Collections.Generic.IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_PossibleAsyncIteratorWithoutYieldOrAwait, "M").WithLocation(4, 60)
                );
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
                // (4,74): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 74),
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
            var comp = CreateCompilationWithAsyncIterator(new[] { Run(iterations: 2), source }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (4,74): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 74)
                );
            CompileAndVerify(comp, expectedOutput: "1 END DISPOSAL DONE");
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
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "M").WithLocation(4, 62),
                // (4,62): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async System.Collections.Generic.IEnumerable<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 62)
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
                // (4,74): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 74),
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
            comp.VerifyDiagnostics(
                // (4,67): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async System.Collections.Generic.IAsyncEnumerator<int> M(int value)
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 67)
                );
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

        [Fact]
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
                    "System.Collections.Generic.IAsyncEnumerator<System.Int32> C.<M>d__0.System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
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
                    type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys.Select(m => m.ToTestDisplayString()));
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2 Stream1:3 4 2 1 2 Stream2:3 4 2 Done");
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Stream1:0 Stream2:0 1 2 Stream1:3 4 2 1 2 Stream2:3 4 2 Done");
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Stream1:0 1 2 Stream1:3 4 42 Await Stream2:0 1 2 Stream2:3 4 42 Done");
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Stream1:1 Finally Stream2:1 Stream2:2 Finally Done");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
                var comp = CreateCompilationWithAsyncIterator(source, options: options);
                comp.VerifyDiagnostics();
                var verifier = CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 5");

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
  IL_0008:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_000d:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_0012:  ldarg.0
  IL_0013:  ldarg.1
  IL_0014:  stfld      ""int C.<M>d__0.<>1__state""
  IL_0019:  ldarg.0
  IL_001a:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_001f:  stfld      ""int C.<M>d__0.<>l__initialThreadId""
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
  IL_0007:  call       ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create()""
  IL_000c:  stfld      ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
  IL_0011:  ldarg.0
  IL_0012:  ldarg.1
  IL_0013:  stfld      ""int C.<M>d__0.<>1__state""
  IL_0018:  ldarg.0
  IL_0019:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_001e:  stfld      ""int C.<M>d__0.<>l__initialThreadId""
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
}", sequencePoints: "C+<M>d__0.MoveNext", source: source);
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
}", sequencePoints: "C+<M>d__0.MoveNext", source: source);
                }
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
}", sequencePoints: "C+<M>d__0.MoveNext", source: source);
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
}", sequencePoints: "C+<M>d__0.MoveNext", source: source);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
}", sequencePoints: "C+<<M>g__local|0_0>d.MoveNext", source: source);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
}", sequencePoints: "C+<M>d__0.MoveNext", source: source);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 5");
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 5");
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Start p:10 p:11 Value p:12 End");
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 Done");
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 5 Done");
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 Done");
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
                comp.VerifyDiagnostics();
                CompileAndVerify(comp, expectedOutput: expectation);
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
                CompileAndVerify(comp, expectedOutput: expectation);
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1 2 3 4 Done");
        }

        [ConditionalTheory(typeof(WindowsDesktopOnly))]
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
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
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
        await System.Threading.Tasks.Task.Yield();
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
            await System.Threading.Tasks.Task.Yield();
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (4,67): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async System.Collections.Generic.IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 67)
                );
            CompileAndVerify(comp, expectedOutput: "1");
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
            var comp = CreateCompilation(AsyncStreamsTypes, references: new[] { TestMetadata.SystemThreadingTasksExtensions.NetStandard20Lib }, targetFramework: TargetFramework.NetStandard20);
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
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "DisposeAsync threw. Already cancelled");
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "done");
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "done");
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
        var enumerable = new MyEnumerable(42);
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            CancellationToken token = source.Token;
            await using (var enumerator = enumerable.GetAsyncEnumerator(token))
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
public class MyEnumerable
{
    private int value;
    public MyEnumerable(int value)
    {
        this.value = value;
    }
    public async System.Collections.Generic.IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken token)
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
            var comp = CreateCompilationWithAsyncIterator(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42 43 Long Cancelled");
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
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42 43 Cancelled");

            // IL for GetAsyncEnumerator is verified by AsyncIteratorWithAwaitCompletedAndYield already
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
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42 43 Cancelled");

            // IL for GetAsyncEnumerator is verified by AsyncIteratorWithAwaitCompletedAndYield_LocalFunction already
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
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42 43 Cancelled");
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
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42 43 Cancelled");
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
                var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: options);
                comp.VerifyDiagnostics();
                var verifier = CompileAndVerify(comp, expectedOutput: "42 43 Cancelled");

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
                var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: options, parseOptions: TestOptions.Regular9);
                comp.VerifyDiagnostics();
                var verifier = CompileAndVerify(comp, expectedOutput: "42 43 Cancelled");

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
                var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: options);
                comp.VerifyDiagnostics();
                CompileAndVerify(comp, expectedOutput: "42 43 Cancelled");
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
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (24,67): error CS8425: Async-iterator 'C.Iter(int, CancellationToken)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
                //     static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, CancellationToken token1) // no attribute set
                Diagnostic(ErrorCode.WRN_UndecoratedCancellationTokenParameter, "Iter").WithArguments("C.Iter(int, System.Threading.CancellationToken)").WithLocation(24, 67)
                );
            CompileAndVerify(comp, expectedOutput: "42 43 REACHED 44");
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
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (24,71): warning CS8425: Async-iterator 'Iter(int, CancellationToken)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
                //         static async System.Collections.Generic.IAsyncEnumerable<int> Iter(int value, CancellationToken token1) // no attribute set
                Diagnostic(ErrorCode.WRN_UndecoratedCancellationTokenParameter, "Iter").WithArguments("Iter(int, System.Threading.CancellationToken)").WithLocation(24, 71)
                );
            CompileAndVerify(comp, expectedOutput: "42 43 REACHED 44");
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_SomeOtherTokenPassedInGetAsyncEnumerator()
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
}";
            // cancelling either the token given as argument or the one given to GetAsyncEnumerator results in cancelling the combined token3
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                foreach (var sourceToCancel in new[] { "source1", "source2" })
                {
                    var comp = CreateCompilationWithAsyncIterator(new[] { source.Replace("SOURCETOCANCEL", sourceToCancel), EnumeratorCancellationAttributeType }, options: options);
                    comp.VerifyDiagnostics();
                    CompileAndVerify(comp, expectedOutput: "42 43 Cancelled");
                }
            }
        }

        [Fact, WorkItem(34407, "https://github.com/dotnet/roslyn/issues/34407")]
        public void CancellationTokenParameter_SomeOtherTokenPassedInGetAsyncEnumerator_LocalFunction()
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
}";
            // cancelling either the token given as argument or the one given to GetAsyncEnumerator results in cancelling the combined token3
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                foreach (var sourceToCancel in new[] { "source1", "source2" })
                {
                    var comp = CreateCompilationWithAsyncIterator(new[] { source.Replace("SOURCETOCANCEL", sourceToCancel), EnumeratorCancellationAttributeType }, options: options, parseOptions: TestOptions.Regular9);
                    comp.VerifyDiagnostics();
                    CompileAndVerify(comp, expectedOutput: "42 43 Cancelled");
                }
            }
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
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1");
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
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,73): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'value' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-enumerable method
                //     static async System.Collections.Generic.IAsyncEnumerable<int> Iter([EnumeratorCancellation] int value)
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("value").WithLocation(6, 73)
                );
            CompileAndVerify(comp, expectedOutput: "42");
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
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, TestOptions.DebugExe, TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (12,77): warning CS8424: The EnumeratorCancellationAttribute applied to parameter 'value' will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable
                //         static async System.Collections.Generic.IAsyncEnumerable<int> Iter([EnumeratorCancellation] int value) // 1
                Diagnostic(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, "EnumeratorCancellation").WithArguments("value").WithLocation(12, 77)
                );
            CompileAndVerify(comp, expectedOutput: "42");
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
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42 43 Cancelled 42 43 Reached 44");
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
            // The overridden method lacks the EnumeratorCancellation attribute
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (28,76): error CS8425: Async-iterator 'C.Iter(CancellationToken, int)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
                //     public override async System.Collections.Generic.IAsyncEnumerable<int> Iter(CancellationToken token1, int value) // 1
                Diagnostic(ErrorCode.WRN_UndecoratedCancellationTokenParameter, "Iter").WithArguments("C.Iter(System.Threading.CancellationToken, int)").WithLocation(28, 76)
                );
            CompileAndVerify(comp, expectedOutput: "Reached 42");
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
            // The overridden method has the EnumeratorCancellation attribute
            var comp = CreateCompilationWithAsyncIterator(new[] { source, EnumeratorCancellationAttributeType }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (8,75): error CS8425: Async-iterator 'Base.Iter(CancellationToken, int)' has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
                //     public virtual async System.Collections.Generic.IAsyncEnumerable<int> Iter(CancellationToken token1, int value) // 1
                Diagnostic(ErrorCode.WRN_UndecoratedCancellationTokenParameter, "Iter").WithArguments("Base.Iter(System.Threading.CancellationToken, int)").WithLocation(8, 75)
                );
            CompileAndVerify(comp, expectedOutput: "42 Cancelled");
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
            var comp = CreateCompilationWithAsyncIterator(@"
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
}", options: TestOptions.DebugExe);

            var v = CompileAndVerify(comp, expectedOutput: "BEFORE INSIDE INSIDE2 AFTER");
            v.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(43936, "https://github.com/dotnet/roslyn/issues/43936")]
        public void TryFinallyNestedInsideFinally_WithAwaitInFinally()
        {
            var comp = CreateCompilationWithAsyncIterator(@"
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
}", options: TestOptions.DebugExe);

            var v = CompileAndVerify(comp, expectedOutput: "BEFORE INSIDE INSIDE2 AFTER");
            v.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(43936, "https://github.com/dotnet/roslyn/issues/43936")]
        public void TryFinallyNestedInsideFinally_WithAwaitInNestedFinally()
        {
            var comp = CreateCompilationWithAsyncIterator(@"
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
}", options: TestOptions.DebugExe);

            var v = CompileAndVerify(comp, expectedOutput: "BEFORE INSIDE INSIDE2 AFTER");
            v.VerifyDiagnostics();
        }

        [Fact, WorkItem(58444, "https://github.com/dotnet/roslyn/issues/58444")]
        public void ClearCurrentOnRegularExit()
        {
            var source = @"
using System;
using System.Collections;
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
            var comp = CreateCompilationWithAsyncIterator(source);
            CompileAndVerify(comp, expectedOutput: "RAN RAN RAN CLEARED");
        }

        [Fact, WorkItem(58444, "https://github.com/dotnet/roslyn/issues/58444")]
        public void ClearCurrentOnException()
        {
            var source = @"
using System;
using System.Collections;
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
            var comp = CreateCompilationWithAsyncIterator(source);
            CompileAndVerify(comp, expectedOutput: "RAN RAN EXCEPTION CLEARED");
        }

        [Fact, WorkItem(58444, "https://github.com/dotnet/roslyn/issues/58444")]
        public void ClearCurrentOnRegularExit_Generic()
        {
            var source = @"
using System;
using System.Collections;
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
            var comp = CreateCompilationWithAsyncIterator(source);
            CompileAndVerify(comp, expectedOutput: "RAN RAN RAN CLEARED");
        }
    }
}
