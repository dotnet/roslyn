// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.AsyncStreams, CompilerFeature.Async)]
    public class CodeGenAwaitForeachTests : EmitMetadataTestBase
    {
        [Fact]
        public void TestWithCSharp7_3()
        {
            string source = @"
using System.Collections.Generic;
class C : IAsyncEnumerable<int>
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (int i in new C())
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
        => throw null;
}";
            var expected = new[]
            {
                // (7,9): error CS8652: The feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         await foreach (int i in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "await").WithArguments("async streams", "8.0").WithLocation(7, 9)
            };

            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(expected);

            comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestDeconstructionWithCSharp7_3()
        {
            string source = @"
using System.Collections.Generic;
class C : IAsyncEnumerable<(int, int)>
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var (i, j) in new C())
        {
        }
    }
    IAsyncEnumerator<(int, int)> IAsyncEnumerable<(int, int)>.GetAsyncEnumerator(System.Threading.CancellationToken token)
        => throw null;
}";
            var expected = new[]
            {
                // (7,9): error CS8652: The feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         await foreach (int i in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "await").WithArguments("async streams", "8.0").WithLocation(7, 9)
            };

            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(expected);

            comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestWithMissingValueTask()
        {
            string lib_cs = @"
using System.Collections.Generic;
public class C : IAsyncEnumerable<int>
{
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
        => throw null;
}";

            var lib = CreateCompilationWithTasksExtensions(new[] { lib_cs, s_IAsyncEnumerable });
            lib.VerifyDiagnostics();

            string source = @"
class D
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (int i in new C()) { }
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(source, references: new[] { lib.EmitToImageReference() });
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_ValueTask);
            comp.VerifyDiagnostics(
                // (6,9): error CS0518: Predefined type 'System.Threading.Tasks.ValueTask' is not defined or imported
                //         await foreach (int i in new C()) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await foreach (int i in new C()) { }").WithArguments("System.Threading.Tasks.ValueTask").WithLocation(6, 9)
                );
        }

        [Fact]
        public void TestWithIAsyncEnumerator()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
public class C
{
    async Task M(IAsyncEnumerator<int> enumerator)
    {
        await foreach (int i in enumerator) { }
    }
}
";
            var comp_checked = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable });
            comp_checked.VerifyDiagnostics(
                // (8,33): error CS8411: Async foreach statement cannot operate on variables of type 'IAsyncEnumerator<int>' because 'IAsyncEnumerator<int>' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                //         await foreach (int i in enumerator) { }
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "enumerator").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "GetAsyncEnumerator").WithLocation(8, 33)
                );
        }

        [Fact]
        public void TestWithUIntToIntConversion()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
public class C : IAsyncEnumerable<uint>
{
    public static async Task Main()
    {
        try
        {
            REPLACE
            {
                await foreach (int i in new C()) { System.Console.Write($""0x{i:X8}""); }
            }
        }
        catch (System.OverflowException)
        {
            System.Console.Write(""overflow"");
        }
    }

    public IAsyncEnumerator<uint> GetAsyncEnumerator(System.Threading.CancellationToken token)
        => new AsyncEnumerator();
    public sealed class AsyncEnumerator : IAsyncEnumerator<uint>
    {
        bool firstValue = true;
        public uint Current => 0xFFFFFFFF;
        public async ValueTask<bool> MoveNextAsync()
        {
            await Task.Yield();
            bool result = firstValue;
            firstValue = false;
            return result;
        }
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
        }
    }
}
";
            var checkedSource = source.Replace("REPLACE", "checked");
            var comp_checked = CreateCompilationWithTasksExtensions(new[] { checkedSource, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp_checked.VerifyDiagnostics();
            CompileAndVerify(comp_checked, expectedOutput: "overflow");

            var uncheckedSource = source.Replace("REPLACE", "unchecked");
            var comp_unchecked = CreateCompilationWithTasksExtensions(new[] { uncheckedSource, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp_unchecked.VerifyDiagnostics();
            CompileAndVerify(comp_unchecked, expectedOutput: "0xFFFFFFFF");

            var runtimeAsyncCompChecked = CreateRuntimeAsyncCompilation(checkedSource, TestOptions.ReleaseExe);
            var verifierChecked = CompileAndVerify(runtimeAsyncCompChecked, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("overflow"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x95 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x31, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x24 }
                    """
            });
            verifierChecked.VerifyIL("C.Main()", """
                {
                  // Code size      150 (0x96)
                  .maxstack  3
                  .locals init (System.Collections.Generic.IAsyncEnumerator<uint> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                int V_3, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4)
                  .try
                  {
                    IL_0000:  newobj     "C..ctor()"
                    IL_0005:  ldloca.s   V_1
                    IL_0007:  initobj    "System.Threading.CancellationToken"
                    IL_000d:  ldloc.1
                    IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<uint> System.Collections.Generic.IAsyncEnumerable<uint>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                    IL_0013:  stloc.0
                    IL_0014:  ldnull
                    IL_0015:  stloc.2
                    .try
                    {
                      IL_0016:  br.s       IL_004e
                      IL_0018:  ldloc.0
                      IL_0019:  callvirt   "uint System.Collections.Generic.IAsyncEnumerator<uint>.Current.get"
                      IL_001e:  conv.ovf.i4.un
                      IL_001f:  stloc.3
                      IL_0020:  ldc.i4.2
                      IL_0021:  ldc.i4.1
                      IL_0022:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                      IL_0027:  stloc.s    V_4
                      IL_0029:  ldloca.s   V_4
                      IL_002b:  ldstr      "0x"
                      IL_0030:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                      IL_0035:  ldloca.s   V_4
                      IL_0037:  ldloc.3
                      IL_0038:  ldstr      "X8"
                      IL_003d:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, string)"
                      IL_0042:  ldloca.s   V_4
                      IL_0044:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                      IL_0049:  call       "void System.Console.Write(string)"
                      IL_004e:  ldloc.0
                      IL_004f:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<uint>.MoveNextAsync()"
                      IL_0054:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                      IL_0059:  brtrue.s   IL_0018
                      IL_005b:  leave.s    IL_0060
                    }
                    catch object
                    {
                      IL_005d:  stloc.2
                      IL_005e:  leave.s    IL_0060
                    }
                    IL_0060:  ldloc.0
                    IL_0061:  brfalse.s  IL_006e
                    IL_0063:  ldloc.0
                    IL_0064:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                    IL_0069:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_006e:  ldloc.2
                    IL_006f:  brfalse.s  IL_0086
                    IL_0071:  ldloc.2
                    IL_0072:  isinst     "System.Exception"
                    IL_0077:  dup
                    IL_0078:  brtrue.s   IL_007c
                    IL_007a:  ldloc.2
                    IL_007b:  throw
                    IL_007c:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_0081:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0086:  leave.s    IL_0095
                  }
                  catch System.OverflowException
                  {
                    IL_0088:  pop
                    IL_0089:  ldstr      "overflow"
                    IL_008e:  call       "void System.Console.Write(string)"
                    IL_0093:  leave.s    IL_0095
                  }
                  IL_0095:  ret
                }
                """);

            var runtimeAsyncCompUnchecked = CreateRuntimeAsyncCompilation(uncheckedSource, TestOptions.ReleaseExe);
            var verifierUnchecked = CompileAndVerify(runtimeAsyncCompUnchecked, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("0xFFFFFFFF"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x94 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x31, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x24 }
                    """
            });
            verifierUnchecked.VerifyIL("C.Main()", """
                {
                  // Code size      149 (0x95)
                  .maxstack  3
                  .locals init (System.Collections.Generic.IAsyncEnumerator<uint> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                int V_3, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4)
                  .try
                  {
                    IL_0000:  newobj     "C..ctor()"
                    IL_0005:  ldloca.s   V_1
                    IL_0007:  initobj    "System.Threading.CancellationToken"
                    IL_000d:  ldloc.1
                    IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<uint> System.Collections.Generic.IAsyncEnumerable<uint>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                    IL_0013:  stloc.0
                    IL_0014:  ldnull
                    IL_0015:  stloc.2
                    .try
                    {
                      IL_0016:  br.s       IL_004d
                      IL_0018:  ldloc.0
                      IL_0019:  callvirt   "uint System.Collections.Generic.IAsyncEnumerator<uint>.Current.get"
                      IL_001e:  stloc.3
                      IL_001f:  ldc.i4.2
                      IL_0020:  ldc.i4.1
                      IL_0021:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                      IL_0026:  stloc.s    V_4
                      IL_0028:  ldloca.s   V_4
                      IL_002a:  ldstr      "0x"
                      IL_002f:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                      IL_0034:  ldloca.s   V_4
                      IL_0036:  ldloc.3
                      IL_0037:  ldstr      "X8"
                      IL_003c:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, string)"
                      IL_0041:  ldloca.s   V_4
                      IL_0043:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                      IL_0048:  call       "void System.Console.Write(string)"
                      IL_004d:  ldloc.0
                      IL_004e:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<uint>.MoveNextAsync()"
                      IL_0053:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                      IL_0058:  brtrue.s   IL_0018
                      IL_005a:  leave.s    IL_005f
                    }
                    catch object
                    {
                      IL_005c:  stloc.2
                      IL_005d:  leave.s    IL_005f
                    }
                    IL_005f:  ldloc.0
                    IL_0060:  brfalse.s  IL_006d
                    IL_0062:  ldloc.0
                    IL_0063:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                    IL_0068:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_006d:  ldloc.2
                    IL_006e:  brfalse.s  IL_0085
                    IL_0070:  ldloc.2
                    IL_0071:  isinst     "System.Exception"
                    IL_0076:  dup
                    IL_0077:  brtrue.s   IL_007b
                    IL_0079:  ldloc.2
                    IL_007a:  throw
                    IL_007b:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_0080:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0085:  leave.s    IL_0094
                  }
                  catch System.OverflowException
                  {
                    IL_0087:  pop
                    IL_0088:  ldstr      "overflow"
                    IL_008d:  call       "void System.Console.Write(string)"
                    IL_0092:  leave.s    IL_0094
                  }
                  IL_0094:  ret
                }
                """);
        }

        [Fact]
        public void TestWithTwoIAsyncEnumerableImplementations()
        {
            string source = @"
using System.Collections.Generic;
class C : IAsyncEnumerable<int>, IAsyncEnumerable<string>
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (int i in new C())
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
        => throw null;
    IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator(System.Threading.CancellationToken token)
        => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable });
            comp.VerifyDiagnostics(
                // (7,33): error CS8413: Async foreach statement cannot operate on variables of type 'C' because it implements multiple instantiations of 'IAsyncEnumerable<T>'; try casting to a specific interface instantiation
                //         await foreach (int i in new C())
                Diagnostic(ErrorCode.ERR_MultipleIAsyncEnumOfT, "new C()").WithArguments("C", "System.Collections.Generic.IAsyncEnumerable<T>").WithLocation(7, 33)
                );
        }

        [Fact]
        public void TestWithMissingPattern()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8411: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithStaticGetEnumerator()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    public static Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        throw null;
    }
    public sealed class Enumerator
    {
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8411: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithInaccessibleGetEnumerator()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    internal Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }
    public sealed class Enumerator
    {
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): warning CS0279: 'C' does not implement the 'async streams' pattern. 'C.GetAsyncEnumerator(System.Threading.CancellationToken)' is not a public instance or extension method.
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_PatternNotPublicOrNotInstance, "new C()").WithArguments("C", "async streams", "C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(6, 33),
                // (6,33): error CS8411: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithObsoletePatternMethods()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    [System.Obsolete]
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }
    public sealed class Enumerator
    {
        [System.Obsolete]
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
        {
            throw null;
        }
        [System.Obsolete]
        public int Current
        {
            get => throw null;
        }
     }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,15): warning CS0612: 'C.GetAsyncEnumerator(CancellationToken)' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(6, 15),
                // (6,15): warning CS0612: 'C.Enumerator.MoveNextAsync()' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.Enumerator.MoveNextAsync()").WithLocation(6, 15),
                // (6,15): warning CS0612: 'C.Enumerator.Current' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.Enumerator.Current").WithLocation(6, 15)
                );
        }

        [Fact]
        public void TestWithMoveNextAsync_ReturnsValueTaskOfObject()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C()) { }
    }
    public Enumerator GetAsyncEnumerator() => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<object> MoveNextAsync() => throw null;
        public int Current => throw null;
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C()) { }
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Theory, CombinatorialData, WorkItem(65363, "https://github.com/dotnet/roslyn/issues/65363")]
        public void TestWithMoveNextAsync_ReturnsValueTaskOfObject_Extension(bool useCsharp8)
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C()) { }
    }
}
static class Extension
{
    public static Enumerator GetAsyncEnumerator(this C c) => throw null;
}
public sealed class Enumerator
{
    public System.Threading.Tasks.Task<object> MoveNextAsync() => throw null;
    public int Current => throw null;
}
";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: useCsharp8 ? TestOptions.Regular8 : TestOptions.Regular9);
            if (useCsharp8)
            {
                comp.VerifyDiagnostics(
                    // (6,33): error CS8400: Feature 'extension GetAsyncEnumerator' is not available in C# 8.0. Please use language version 9.0 or greater.
                    //         await foreach (var i in new C()) { }
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "new C()").WithArguments("extension GetAsyncEnumerator", "9.0").WithLocation(6, 33),
                    // (6,33): error CS8412: Asynchronous foreach requires that the return type 'Enumerator' of 'Extension.GetAsyncEnumerator(C)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                    //         await foreach (var i in new C()) { }
                    Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("Enumerator", "Extension.GetAsyncEnumerator(C)").WithLocation(6, 33)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (6,33): error CS8412: Asynchronous foreach requires that the return type 'Enumerator' of 'Extension.GetAsyncEnumerator(C)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                    //         await foreach (var i in new C()) { }
                    Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("Enumerator", "Extension.GetAsyncEnumerator(C)").WithLocation(6, 33)
                    );
            }
        }

        [Fact]
        public void TestWithMoveNextAsync_ReturnsObject()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator() => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<object> MoveNextAsync() => throw null; // returns Task<object>
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithMoveNextAsync_Static()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public static System.Threading.Tasks.Task<bool> MoveNextAsync()
        {
            throw null;
        }
        public int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator(CancellationToken)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithCurrent_Static()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C()) { }
    }
    public Enumerator GetAsyncEnumerator()
        => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public static int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C()) { }
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithMoveNextAsync_NonPublic()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C()) { }
    }
    public Enumerator GetAsyncEnumerator()
        => throw null;
    public sealed class Enumerator
    {
        internal System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current
            => throw null;
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C()) { }
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithCurrent_NonPublicProperty()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C()) { }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
        => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        private int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS0122: 'C.Enumerator.Current' is inaccessible due to its protection level
                //         await foreach (var i in new C()) { }
                Diagnostic(ErrorCode.ERR_BadAccess, "new C()").WithArguments("C.Enumerator.Current").WithLocation(6, 33),
                // (6,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator(CancellationToken)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C()) { }
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithCurrent_NonPublicGetter()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C()) { }
    }
    public Enumerator GetAsyncEnumerator()
        => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current
        {
            private get => throw null;
            set => throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C()) { }
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithCurrent_MissingGetter()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C()) { }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
        => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current
        {
            set => throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator(CancellationToken)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C()) { }
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithCurrent_MissingGetterOnInterface()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    public static async Task M(C c)
    {
        await foreach (var i in c)
        {
        }
    }
    public IAsyncEnumerator<int> GetAsyncEnumerator(System.Threading.CancellationToken token = default) => throw null;
}
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token = default);
    }

    public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        System.Threading.Tasks.Task<bool> MoveNextAsync();
        T Current { set; }
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
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyEmitDiagnostics(
                // (8,33): error CS8412: Asynchronous foreach requires that the return type 'IAsyncEnumerator<int>' of 'C.GetAsyncEnumerator(CancellationToken)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in c)
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "c").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(8, 33),
                // (24,9): error CS1961: Invalid variance: The type parameter 'T' must be contravariantly valid on 'IAsyncEnumerator<T>.Current'. 'T' is covariant.
                //         T Current { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "T").WithArguments("System.Collections.Generic.IAsyncEnumerator<T>.Current", "T", "covariant", "contravariantly").WithLocation(24, 9)
                );
        }

        [Fact]
        public void TestWithCurrent_MissingPropertyOnInterface()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    public static async Task M(C c)
    {
        await foreach (var i in c)
        {
        }
    }
    public IAsyncEnumerator<int> GetAsyncEnumerator(System.Threading.CancellationToken token = default) => throw null;
}
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token = default);
    }

    public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        System.Threading.Tasks.Task<bool> MoveNextAsync();
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
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyEmitDiagnostics(
                // (8,33): error CS0117: 'IAsyncEnumerator<int>' does not contain a definition for 'Current'
                //         await foreach (var i in c)
                Diagnostic(ErrorCode.ERR_NoSuchMember, "c").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "Current").WithLocation(8, 33),
                // (8,33): error CS8412: Asynchronous foreach requires that the return type 'IAsyncEnumerator<int>' of 'C.GetAsyncEnumerator(CancellationToken)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in c)
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "c").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(8, 33)
                );
        }

        [Fact]
        public void TestMoveNextAsync_ReturnsTask()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task MoveNextAsync()
        {
            throw null;
        }
        public int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator(CancellationToken)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestMoveNextAsync_ReturnsTaskOfInt()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<int> MoveNextAsync()
        {
            throw null;
        }
        public int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator(CancellationToken)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestMoveNextAsync_WithOptionalParameter()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async System.Threading.Tasks.Task<bool> MoveNextAsync(int ok = 1)
        {
            System.Console.Write($""MoveNextAsync {ok}"");
            await System.Threading.Tasks.Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync 1");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync 1"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x2b }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x4f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       44 (0x2c)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                System.Threading.CancellationToken V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  call       "C.Enumerator C.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  br.s       IL_001d
                  IL_0016:  ldloc.0
                  IL_0017:  callvirt   "int C.Enumerator.Current.get"
                  IL_001c:  pop
                  IL_001d:  ldloc.0
                  IL_001e:  ldc.i4.1
                  IL_001f:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync(int)"
                  IL_0024:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0029:  brtrue.s   IL_0016
                  IL_002b:  ret
                }
                """);
        }

        [Fact]
        public void TestMoveNextAsync_WithParamsParameter()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async System.Threading.Tasks.Task<bool> MoveNextAsync(params int[] ok)
        {
            System.Console.Write($""MoveNextAsync {ok.Length}"");
            await System.Threading.Tasks.Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync 0");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync 0"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x2f }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x51, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       48 (0x30)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                System.Threading.CancellationToken V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  call       "C.Enumerator C.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  br.s       IL_001d
                  IL_0016:  ldloc.0
                  IL_0017:  callvirt   "int C.Enumerator.Current.get"
                  IL_001c:  pop
                  IL_001d:  ldloc.0
                  IL_001e:  call       "int[] System.Array.Empty<int>()"
                  IL_0023:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync(params int[])"
                  IL_0028:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_002d:  brtrue.s   IL_0016
                  IL_002f:  ret
                }
                """);
        }

        [Fact]
        public void TestMoveNextAsync_Missing()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    public static async Task M(C c)
    {
        await foreach (var i in c)
        {
        }
    }
    public IAsyncEnumerator<int> GetAsyncEnumerator(System.Threading.CancellationToken token = default) => throw null;
}
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token = default);
    }

    public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
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
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyEmitDiagnostics(
                // (8,33): error CS0117: 'IAsyncEnumerator<int>' does not contain a definition for 'MoveNextAsync'
                //         await foreach (var i in c)
                Diagnostic(ErrorCode.ERR_NoSuchMember, "c").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "MoveNextAsync").WithLocation(8, 33),
                // (8,33): error CS8412: Asynchronous foreach requires that the return type 'IAsyncEnumerator<int>' of 'C.GetAsyncEnumerator(CancellationToken)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in c)
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "c").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(8, 33)
                );
        }

        [Fact]
        public void TestWithNonConvertibleElementType()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (string i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
        => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (6,15): error CS0030: Cannot convert type 'int' to 'string'
                //         await foreach (string i in new C())
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("int", "string").WithLocation(6, 15)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.True(info.IsAsynchronous);
            Assert.Equal("C.Enumerator C.GetAsyncEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> C.Enumerator.MoveNextAsync()", info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Enumerator.Current { get; }", info.CurrentProperty.ToTestDisplayString());
            Assert.Null(info.DisposeMethod);
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.NoConversion, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
        }

        [Fact]
        public void TestWithNonConvertibleElementType2()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    async Task M()
    {
        await foreach (Element i in new C())
        {
        }
    }
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
            => throw null;
    public sealed class AsyncEnumerator
    {
        public int Current
        {
            get => throw null;
        }
        public Task<bool> MoveNextAsync()
            => throw null;
        public ValueTask DisposeAsync()
            => throw null;
    }
}
class Element
{
}";
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyDiagnostics(
                // (7,15): error CS0030: Cannot convert type 'int' to 'Element'
                //         await foreach (Element i in new C())
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("int", "Element").WithLocation(7, 15)
                );
        }

        [Fact]
        public void TestWithExplicitlyConvertibleElementType()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (Element i in new C())
        {
            Write($""Got({i}) "");
        }
    }
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        return new AsyncEnumerator();
    }
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        int i = 0;
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async Task<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            bool more = await Task.FromResult(i < 4);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Yield();
        }
    }
}
class Element
{
    int i;
    public static explicit operator Element(int value) { Write($""Convert({value}) ""); return new Element(value); }
    private Element(int value) { i = value; }
    public override string ToString() => i.ToString();
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            string expectedOutput = "NextAsync(0) Current(1) Convert(1) Got(1) NextAsync(1) Current(2) Convert(2) Got(2) NextAsync(2) Current(3) Convert(3) Got(3) NextAsync(3) Dispose(4)";
            CompileAndVerify(comp,
                expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x91 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x5f }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      146 (0x92)
                  .maxstack  2
                  .locals init (C.AsyncEnumerator V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                Element V_3, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  call       "C.AsyncEnumerator C.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0059
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int C.AsyncEnumerator.Current.get"
                    IL_001e:  call       "Element Element.op_Explicit(int)"
                    IL_0023:  stloc.3
                    IL_0024:  ldc.i4.6
                    IL_0025:  ldc.i4.1
                    IL_0026:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_002b:  stloc.s    V_4
                    IL_002d:  ldloca.s   V_4
                    IL_002f:  ldstr      "Got("
                    IL_0034:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0039:  ldloca.s   V_4
                    IL_003b:  ldloc.3
                    IL_003c:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<Element>(Element)"
                    IL_0041:  ldloca.s   V_4
                    IL_0043:  ldstr      ") "
                    IL_0048:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_004d:  ldloca.s   V_4
                    IL_004f:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0054:  call       "void System.Console.Write(string)"
                    IL_0059:  ldloc.0
                    IL_005a:  callvirt   "System.Threading.Tasks.Task<bool> C.AsyncEnumerator.MoveNextAsync()"
                    IL_005f:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0064:  brtrue.s   IL_0018
                    IL_0066:  leave.s    IL_006b
                  }
                  catch object
                  {
                    IL_0068:  stloc.2
                    IL_0069:  leave.s    IL_006b
                  }
                  IL_006b:  ldloc.0
                  IL_006c:  brfalse.s  IL_0079
                  IL_006e:  ldloc.0
                  IL_006f:  callvirt   "System.Threading.Tasks.ValueTask C.AsyncEnumerator.DisposeAsync()"
                  IL_0074:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0079:  ldloc.2
                  IL_007a:  brfalse.s  IL_0091
                  IL_007c:  ldloc.2
                  IL_007d:  isinst     "System.Exception"
                  IL_0082:  dup
                  IL_0083:  brtrue.s   IL_0087
                  IL_0085:  ldloc.2
                  IL_0086:  throw
                  IL_0087:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_008c:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0091:  ret
                }
                """);
        }

        [Fact]
        public void TestWithCaptureOfIterationVariable()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
public class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        System.Action f = null;
        await foreach (var i in new C())
        {
            Write($""Got({i}) "");
            if (f == null) f = () => Write($""Captured({i})"");
        }
        f();
    }
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        return new AsyncEnumerator();
    }
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        int i = 0;
        public int Current => i;
        public async Task<bool> MoveNextAsync()
        {
            i++;
            return await Task.FromResult(i < 3);
        }
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Got(1) Got(2) Captured(1)");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("Got(1) Got(2) Captured(1)"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xb8 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x21, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x24 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      185 (0xb9)
                  .maxstack  2
                  .locals init (System.Action V_0, //f
                                C.AsyncEnumerator V_1,
                                System.Threading.CancellationToken V_2,
                                object V_3,
                                C.<>c__DisplayClass0_0 V_4, //CS$<>8__locals0
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_5)
                  IL_0000:  ldnull
                  IL_0001:  stloc.0
                  IL_0002:  newobj     "C..ctor()"
                  IL_0007:  ldloca.s   V_2
                  IL_0009:  initobj    "System.Threading.CancellationToken"
                  IL_000f:  ldloc.2
                  IL_0010:  call       "C.AsyncEnumerator C.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0015:  stloc.1
                  IL_0016:  ldnull
                  IL_0017:  stloc.3
                  .try
                  {
                    IL_0018:  br.s       IL_007a
                    IL_001a:  newobj     "C.<>c__DisplayClass0_0..ctor()"
                    IL_001f:  stloc.s    V_4
                    IL_0021:  ldloc.s    V_4
                    IL_0023:  ldloc.1
                    IL_0024:  callvirt   "int C.AsyncEnumerator.Current.get"
                    IL_0029:  stfld      "int C.<>c__DisplayClass0_0.i"
                    IL_002e:  ldc.i4.6
                    IL_002f:  ldc.i4.1
                    IL_0030:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0035:  stloc.s    V_5
                    IL_0037:  ldloca.s   V_5
                    IL_0039:  ldstr      "Got("
                    IL_003e:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0043:  ldloca.s   V_5
                    IL_0045:  ldloc.s    V_4
                    IL_0047:  ldfld      "int C.<>c__DisplayClass0_0.i"
                    IL_004c:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0051:  ldloca.s   V_5
                    IL_0053:  ldstr      ") "
                    IL_0058:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_005d:  ldloca.s   V_5
                    IL_005f:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0064:  call       "void System.Console.Write(string)"
                    IL_0069:  ldloc.0
                    IL_006a:  brtrue.s   IL_007a
                    IL_006c:  ldloc.s    V_4
                    IL_006e:  ldftn      "void C.<>c__DisplayClass0_0.<Main>b__0()"
                    IL_0074:  newobj     "System.Action..ctor(object, System.IntPtr)"
                    IL_0079:  stloc.0
                    IL_007a:  ldloc.1
                    IL_007b:  callvirt   "System.Threading.Tasks.Task<bool> C.AsyncEnumerator.MoveNextAsync()"
                    IL_0080:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0085:  brtrue.s   IL_001a
                    IL_0087:  leave.s    IL_008c
                  }
                  catch object
                  {
                    IL_0089:  stloc.3
                    IL_008a:  leave.s    IL_008c
                  }
                  IL_008c:  ldloc.1
                  IL_008d:  brfalse.s  IL_009a
                  IL_008f:  ldloc.1
                  IL_0090:  callvirt   "System.Threading.Tasks.ValueTask C.AsyncEnumerator.DisposeAsync()"
                  IL_0095:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_009a:  ldloc.3
                  IL_009b:  brfalse.s  IL_00b2
                  IL_009d:  ldloc.3
                  IL_009e:  isinst     "System.Exception"
                  IL_00a3:  dup
                  IL_00a4:  brtrue.s   IL_00a8
                  IL_00a6:  ldloc.3
                  IL_00a7:  throw
                  IL_00a8:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_00ad:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_00b2:  ldloc.0
                  IL_00b3:  callvirt   "void System.Action.Invoke()"
                  IL_00b8:  ret
                }
                """);
        }

        [Fact]
        public void TestWithGenericIterationVariable()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
class IntContainer
{
    public int Value { get; set; }
}
public class Program
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C<IntContainer>())
        {
            Write($""Got({i.Value}) "");
        }
    }
}
class C<T> where T : IntContainer, new()
{
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        return new AsyncEnumerator();
    }
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        int i = 0;
        public T Current
        {
            get
            {
                Write($""Current({i}) "");
                var result = new T();
                ((IntContainer)result).Value = i;
                return result;
            }
        }
        public async Task<bool> MoveNextAsync()
        {
            i++;
            Write($""NextAsync({i}) "");
            return await Task.FromResult(i < 4);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var expectedOutput = "NextAsync(1) Current(1) Got(1) NextAsync(2) Current(2) Got(2) NextAsync(3) Current(3) Got(3) NextAsync(4) Dispose(4)";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x91 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x5f }
                    """
            });
            verifier.VerifyIL("Program.Main()", """
                {
                  // Code size      146 (0x92)
                  .maxstack  2
                  .locals init (C<IntContainer>.AsyncEnumerator V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                IntContainer V_3, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4)
                  IL_0000:  newobj     "C<IntContainer>..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  call       "C<IntContainer>.AsyncEnumerator C<IntContainer>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0059
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "IntContainer C<IntContainer>.AsyncEnumerator.Current.get"
                    IL_001e:  stloc.3
                    IL_001f:  ldc.i4.6
                    IL_0020:  ldc.i4.1
                    IL_0021:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0026:  stloc.s    V_4
                    IL_0028:  ldloca.s   V_4
                    IL_002a:  ldstr      "Got("
                    IL_002f:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0034:  ldloca.s   V_4
                    IL_0036:  ldloc.3
                    IL_0037:  callvirt   "int IntContainer.Value.get"
                    IL_003c:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0041:  ldloca.s   V_4
                    IL_0043:  ldstr      ") "
                    IL_0048:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_004d:  ldloca.s   V_4
                    IL_004f:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0054:  call       "void System.Console.Write(string)"
                    IL_0059:  ldloc.0
                    IL_005a:  callvirt   "System.Threading.Tasks.Task<bool> C<IntContainer>.AsyncEnumerator.MoveNextAsync()"
                    IL_005f:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0064:  brtrue.s   IL_0018
                    IL_0066:  leave.s    IL_006b
                  }
                  catch object
                  {
                    IL_0068:  stloc.2
                    IL_0069:  leave.s    IL_006b
                  }
                  IL_006b:  ldloc.0
                  IL_006c:  brfalse.s  IL_0079
                  IL_006e:  ldloc.0
                  IL_006f:  callvirt   "System.Threading.Tasks.ValueTask C<IntContainer>.AsyncEnumerator.DisposeAsync()"
                  IL_0074:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0079:  ldloc.2
                  IL_007a:  brfalse.s  IL_0091
                  IL_007c:  ldloc.2
                  IL_007d:  isinst     "System.Exception"
                  IL_0082:  dup
                  IL_0083:  brtrue.s   IL_0087
                  IL_0085:  ldloc.2
                  IL_0086:  throw
                  IL_0087:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_008c:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0091:  ret
                }
                """);
        }

        [Fact]
        public void TestWithThrowingGetAsyncEnumerator()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        try
        {
            await foreach (var i in new C())
            {
                throw null;
            }
            throw null;
        }
        catch (System.ArgumentException e)
        {
            Write(e.Message);
        }
    }
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
        => throw new System.ArgumentException(""exception"");
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current
            => throw null;
        public Task<bool> MoveNextAsync()
            => throw null;
        public ValueTask DisposeAsync()
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "exception");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("exception"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x67 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      104 (0x68)
                  .maxstack  2
                  .locals init (C.AsyncEnumerator V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2)
                  .try
                  {
                    IL_0000:  newobj     "C..ctor()"
                    IL_0005:  ldloca.s   V_1
                    IL_0007:  initobj    "System.Threading.CancellationToken"
                    IL_000d:  ldloc.1
                    IL_000e:  call       "C.AsyncEnumerator C.GetAsyncEnumerator(System.Threading.CancellationToken)"
                    IL_0013:  stloc.0
                    IL_0014:  ldnull
                    IL_0015:  stloc.2
                    .try
                    {
                      IL_0016:  br.s       IL_0021
                      IL_0018:  ldloc.0
                      IL_0019:  callvirt   "int C.AsyncEnumerator.Current.get"
                      IL_001e:  pop
                      IL_001f:  ldnull
                      IL_0020:  throw
                      IL_0021:  ldloc.0
                      IL_0022:  callvirt   "System.Threading.Tasks.Task<bool> C.AsyncEnumerator.MoveNextAsync()"
                      IL_0027:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                      IL_002c:  brtrue.s   IL_0018
                      IL_002e:  leave.s    IL_0033
                    }
                    catch object
                    {
                      IL_0030:  stloc.2
                      IL_0031:  leave.s    IL_0033
                    }
                    IL_0033:  ldloc.0
                    IL_0034:  brfalse.s  IL_0041
                    IL_0036:  ldloc.0
                    IL_0037:  callvirt   "System.Threading.Tasks.ValueTask C.AsyncEnumerator.DisposeAsync()"
                    IL_003c:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_0041:  ldloc.2
                    IL_0042:  brfalse.s  IL_0059
                    IL_0044:  ldloc.2
                    IL_0045:  isinst     "System.Exception"
                    IL_004a:  dup
                    IL_004b:  brtrue.s   IL_004f
                    IL_004d:  ldloc.2
                    IL_004e:  throw
                    IL_004f:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_0054:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0059:  ldnull
                    IL_005a:  throw
                  }
                  catch System.ArgumentException
                  {
                    IL_005b:  callvirt   "string System.Exception.Message.get"
                    IL_0060:  call       "void System.Console.Write(string)"
                    IL_0065:  leave.s    IL_0067
                  }
                  IL_0067:  ret
                }
                """);
        }

        [Fact]
        public void TestWithThrowingMoveNextAsync()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        try
        {
            await foreach (var i in new C())
            {
                throw null;
            }
            throw null;
        }
        catch (System.ArgumentException e)
        {
            Write(e.Message);
        }
    }
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
        => new AsyncEnumerator();
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current
            => throw null;
        public Task<bool> MoveNextAsync()
            => throw new System.ArgumentException(""exception"");
        public async ValueTask DisposeAsync()
        {
            Write(""dispose "");
            await Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "dispose exception");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("dispose exception"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x67 }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      104 (0x68)
                  .maxstack  2
                  .locals init (C.AsyncEnumerator V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2)
                  .try
                  {
                    IL_0000:  newobj     "C..ctor()"
                    IL_0005:  ldloca.s   V_1
                    IL_0007:  initobj    "System.Threading.CancellationToken"
                    IL_000d:  ldloc.1
                    IL_000e:  call       "C.AsyncEnumerator C.GetAsyncEnumerator(System.Threading.CancellationToken)"
                    IL_0013:  stloc.0
                    IL_0014:  ldnull
                    IL_0015:  stloc.2
                    .try
                    {
                      IL_0016:  br.s       IL_0021
                      IL_0018:  ldloc.0
                      IL_0019:  callvirt   "int C.AsyncEnumerator.Current.get"
                      IL_001e:  pop
                      IL_001f:  ldnull
                      IL_0020:  throw
                      IL_0021:  ldloc.0
                      IL_0022:  callvirt   "System.Threading.Tasks.Task<bool> C.AsyncEnumerator.MoveNextAsync()"
                      IL_0027:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                      IL_002c:  brtrue.s   IL_0018
                      IL_002e:  leave.s    IL_0033
                    }
                    catch object
                    {
                      IL_0030:  stloc.2
                      IL_0031:  leave.s    IL_0033
                    }
                    IL_0033:  ldloc.0
                    IL_0034:  brfalse.s  IL_0041
                    IL_0036:  ldloc.0
                    IL_0037:  callvirt   "System.Threading.Tasks.ValueTask C.AsyncEnumerator.DisposeAsync()"
                    IL_003c:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_0041:  ldloc.2
                    IL_0042:  brfalse.s  IL_0059
                    IL_0044:  ldloc.2
                    IL_0045:  isinst     "System.Exception"
                    IL_004a:  dup
                    IL_004b:  brtrue.s   IL_004f
                    IL_004d:  ldloc.2
                    IL_004e:  throw
                    IL_004f:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_0054:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0059:  ldnull
                    IL_005a:  throw
                  }
                  catch System.ArgumentException
                  {
                    IL_005b:  callvirt   "string System.Exception.Message.get"
                    IL_0060:  call       "void System.Console.Write(string)"
                    IL_0065:  leave.s    IL_0067
                  }
                  IL_0067:  ret
                }
                """);
        }

        [Fact]
        public void TestWithThrowingCurrent()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        try
        {
            await foreach (var i in new C())
            {
                throw null;
            }
            throw null;
        }
        catch (System.ArgumentException e)
        {
            Write(e.Message);
        }
    }
    public AsyncEnumerator GetAsyncEnumerator()
        => new AsyncEnumerator();
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current
            => throw new System.ArgumentException(""exception"");
        public async Task<bool> MoveNextAsync()
        {
            Write(""wait "");
            await Task.Yield();
            return true;
        }
        public async ValueTask DisposeAsync()
        {
            Write(""dispose "");
            await Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "wait dispose exception");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("wait dispose exception"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x5e }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       95 (0x5f)
                  .maxstack  2
                  .locals init (C.AsyncEnumerator V_0,
                                object V_1)
                  .try
                  {
                    IL_0000:  newobj     "C..ctor()"
                    IL_0005:  call       "C.AsyncEnumerator C.GetAsyncEnumerator()"
                    IL_000a:  stloc.0
                    IL_000b:  ldnull
                    IL_000c:  stloc.1
                    .try
                    {
                      IL_000d:  br.s       IL_0018
                      IL_000f:  ldloc.0
                      IL_0010:  callvirt   "int C.AsyncEnumerator.Current.get"
                      IL_0015:  pop
                      IL_0016:  ldnull
                      IL_0017:  throw
                      IL_0018:  ldloc.0
                      IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.AsyncEnumerator.MoveNextAsync()"
                      IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                      IL_0023:  brtrue.s   IL_000f
                      IL_0025:  leave.s    IL_002a
                    }
                    catch object
                    {
                      IL_0027:  stloc.1
                      IL_0028:  leave.s    IL_002a
                    }
                    IL_002a:  ldloc.0
                    IL_002b:  brfalse.s  IL_0038
                    IL_002d:  ldloc.0
                    IL_002e:  callvirt   "System.Threading.Tasks.ValueTask C.AsyncEnumerator.DisposeAsync()"
                    IL_0033:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_0038:  ldloc.1
                    IL_0039:  brfalse.s  IL_0050
                    IL_003b:  ldloc.1
                    IL_003c:  isinst     "System.Exception"
                    IL_0041:  dup
                    IL_0042:  brtrue.s   IL_0046
                    IL_0044:  ldloc.1
                    IL_0045:  throw
                    IL_0046:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_004b:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0050:  ldnull
                    IL_0051:  throw
                  }
                  catch System.ArgumentException
                  {
                    IL_0052:  callvirt   "string System.Exception.Message.get"
                    IL_0057:  call       "void System.Console.Write(string)"
                    IL_005c:  leave.s    IL_005e
                  }
                  IL_005e:  ret
                }
                """);
        }

        [Fact]
        public void TestWithThrowingDisposeAsync()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        try
        {
            await foreach (var i in new C())
            {
                throw null;
            }
            throw null;
        }
        catch (System.ArgumentException e)
        {
            Write(e.Message);
        }
    }
    public AsyncEnumerator GetAsyncEnumerator()
        => new AsyncEnumerator();
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current => throw null;
        public async Task<bool> MoveNextAsync()
        {
            Write(""wait "");
            await Task.Yield();
            return false;
        }
        public ValueTask DisposeAsync()
            => throw new System.ArgumentException(""exception"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "wait exception");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("wait exception"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x5e }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       95 (0x5f)
                  .maxstack  2
                  .locals init (C.AsyncEnumerator V_0,
                                object V_1)
                  .try
                  {
                    IL_0000:  newobj     "C..ctor()"
                    IL_0005:  call       "C.AsyncEnumerator C.GetAsyncEnumerator()"
                    IL_000a:  stloc.0
                    IL_000b:  ldnull
                    IL_000c:  stloc.1
                    .try
                    {
                      IL_000d:  br.s       IL_0018
                      IL_000f:  ldloc.0
                      IL_0010:  callvirt   "int C.AsyncEnumerator.Current.get"
                      IL_0015:  pop
                      IL_0016:  ldnull
                      IL_0017:  throw
                      IL_0018:  ldloc.0
                      IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.AsyncEnumerator.MoveNextAsync()"
                      IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                      IL_0023:  brtrue.s   IL_000f
                      IL_0025:  leave.s    IL_002a
                    }
                    catch object
                    {
                      IL_0027:  stloc.1
                      IL_0028:  leave.s    IL_002a
                    }
                    IL_002a:  ldloc.0
                    IL_002b:  brfalse.s  IL_0038
                    IL_002d:  ldloc.0
                    IL_002e:  callvirt   "System.Threading.Tasks.ValueTask C.AsyncEnumerator.DisposeAsync()"
                    IL_0033:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_0038:  ldloc.1
                    IL_0039:  brfalse.s  IL_0050
                    IL_003b:  ldloc.1
                    IL_003c:  isinst     "System.Exception"
                    IL_0041:  dup
                    IL_0042:  brtrue.s   IL_0046
                    IL_0044:  ldloc.1
                    IL_0045:  throw
                    IL_0046:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_004b:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0050:  ldnull
                    IL_0051:  throw
                  }
                  catch System.ArgumentException
                  {
                    IL_0052:  callvirt   "string System.Exception.Message.get"
                    IL_0057:  call       "void System.Console.Write(string)"
                    IL_005c:  leave.s    IL_005e
                  }
                  IL_005e:  ret
                }
                """);
        }

        [Fact]
        public void TestWithDynamicCollection()
        {
            string source = @"
class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in (dynamic)new C())
        {
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable });
            // (6,33): error CS8416: Cannot use a collection of dynamic type in an asynchronous foreach
            //         await foreach (var i in (dynamic)new C())
            DiagnosticDescription expected = Diagnostic(ErrorCode.ERR_BadDynamicAwaitForEach, "(dynamic)new C()").WithLocation(6, 33);
            comp.VerifyDiagnostics(expected);

            comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void TestWithIncompleteInterface()
        {
            string source = @"
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
    }
}
class C
{
    async System.Threading.Tasks.Task M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        await foreach (var i in collection)
        {
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (12,33): error CS8411: Async foreach statement cannot operate on variables of type 'IAsyncEnumerable<int>' because 'IAsyncEnumerable<int>' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                //         await foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerable<int>", "GetAsyncEnumerator").WithLocation(12, 33)
                );
        }

        [Fact]
        public void TestWithIncompleteInterface2()
        {
            string source = @"
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token = default);
    }

    public interface IAsyncEnumerator<out T>
    {
        System.Threading.Tasks.Task<bool> MoveNextAsync();
    }
}
class C
{
    async System.Threading.Tasks.Task M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        await foreach (var i in collection)
        {
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (18,33): error CS0117: 'IAsyncEnumerator<int>' does not contain a definition for 'Current'
                //         await foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_NoSuchMember, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "Current").WithLocation(18, 33),
                // (18,33): error CS8412: Async foreach requires that the return type 'IAsyncEnumerator<int>' of 'IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(18, 33)
                );
        }

        [Fact]
        public void TestWithIncompleteInterface3()
        {
            string source = @"
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token = default);
    }

    public interface IAsyncEnumerator<out T>
    {
        T Current { get; }
    }
}
class C
{
    async System.Threading.Tasks.Task M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        await foreach (var i in collection)
        {
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (18,33): error CS0117: 'IAsyncEnumerator<int>' does not contain a definition for 'MoveNextAsync'
                //         await foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_NoSuchMember, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "MoveNextAsync").WithLocation(18, 33),
                // (18,33): error CS8412: Async foreach requires that the return type 'IAsyncEnumerator<int>' of 'IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(18, 33)
                );
        }

        [Fact]
        public void TestWithSyncPattern()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        bool MoveNext() => throw null;
        int Current => throw null;
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8411: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestRegularForeachWithAsyncPattern()
        {
            string source = @"
class C
{
    void M()
    {
        foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default) => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,27): error CS8414: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'. Did you mean 'await foreach' rather than 'foreach'?
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_ForEachMissingMemberWrongAsync, "new C()").WithArguments("C", "GetEnumerator").WithLocation(6, 27)
                );
        }

        [Fact]
        public void TestRegularForeachWithAsyncInterface()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    void M(IAsyncEnumerable<int> collection)
    {
        foreach (var i in collection)
        {
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (7,27): error CS8414: foreach statement cannot operate on variables of type 'IAsyncEnumerable<int>' because 'IAsyncEnumerable<int>' does not contain a public instance or extension definition for 'GetEnumerator'. Did you mean 'await foreach'?
                //         foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_ForEachMissingMemberWrongAsync, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerable<int>", "GetEnumerator").WithLocation(7, 27)
                );
        }

        [Fact]
        public void TestWithSyncInterfaceInRegularMethod()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    void M(IEnumerable<int> collection)
    {
        await foreach (var i in collection)
        {
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (7,9): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         await foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(7, 9),
                // (7,33): error CS8415: Asynchronous foreach statement cannot operate on variables of type 'IEnumerable<int>' because 'IEnumerable<int>' does not contain a public instance or extension definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'await foreach'?
                //         await foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMemberWrongAsync, "collection").WithArguments("System.Collections.Generic.IEnumerable<int>", "GetAsyncEnumerator").WithLocation(7, 33)
                );
        }

        [Fact]
        public void TestPatternBasedAsyncEnumerableWithRegularForeach()
        {
            string source = @"
class C
{
    void M()
    {
        foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
        => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (6,27): error CS8414: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'. Did you mean 'await foreach'?
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_ForEachMissingMemberWrongAsync, "new C()").WithArguments("C", "GetEnumerator").WithLocation(6, 27)
                );
        }

        [Fact]
        public void TestPatternBased_GetEnumeratorWithoutCancellationToken()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator() // no parameter
        => new Enumerator(); 
    public sealed class Enumerator
    {
        public async System.Threading.Tasks.Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync"");
            await System.Threading.Tasks.Task.Yield();
            return false;
        }
        public int Current => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x21 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       34 (0x22)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0014
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  pop
                  IL_0014:  ldloc.0
                  IL_0015:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_001f:  brtrue.s   IL_000d
                  IL_0021:  ret
                }
                """);
        }

        [Fact]
        public void TestPatternBasedEnumerableWithAwaitForeach()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator<int> GetEnumerator()
        => throw null;
    public class Enumerator<T>
    {
        public T Current { get; }
        public bool MoveNext()
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (6,33): error CS8415: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'await foreach'?
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMemberWrongAsync, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithPattern()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
        {
            throw null;
        }
        public int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("C.Enumerator C.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> C.Enumerator.MoveNextAsync()", info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Enumerator.Current { get; }", info.CurrentProperty.ToTestDisplayString());
            Assert.Null(info.DisposeMethod);
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.False(internalInfo.NeedsDisposal);
        }

        [Fact]
        public void TestWithPattern_Ref()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (ref var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current { get => throw null; }
    }
}" + s_IAsyncEnumerable;

            var comp = CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (6,32): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (ref var i in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "i").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 32));

            var expectedDiagnostics = new[]
            {
                // (6,37): error CS1510: A ref or out value must be an assignable variable
                //         await foreach (ref var i in new C())
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "new C()").WithLocation(6, 37)
            };

            comp = CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular13);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            Assert.Equal(default, model.GetForEachStatementInfo(foreachSyntax));
        }

        [Theory, CombinatorialData]
        public void TestWithPattern_Ref_Iterator([CombinatorialValues("        ", "readonly")] string modifier)
        {
            var source = $$"""
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                class C
                {
                    static async Task Main()
                    {
                        await foreach (int i in F())
                        {
                            Console.Write(i);
                        }
                    }

                    static async IAsyncEnumerable<int> F()
                    {
                        await foreach (ref {{modifier}} var i in new C())
                        {
                            yield return i;
                        }
                    }

                    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default) => new();

                    public sealed class Enumerator
                    {
                        private readonly int[] _array = [1, 2, 3];
                        private int _index = -1;
                        public Task<bool> MoveNextAsync()
                        {
                            if (_index < _array.Length) _index++;
                            return Task.FromResult(_index < _array.Length);
                        }       
                        public ref int Current => ref _array[_index];
                    }
                }
                """;

            CSharpTestSource sources = [source, AsyncStreamsTypes];
            var comp = CreateCompilationWithTasksExtensions(sources, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (17,41): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (ref readonly var i in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "i").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(17, 41));

            var expectedOutput = "123";

            comp = CreateCompilationWithTasksExtensions(sources, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp = CreateCompilationWithTasksExtensions(sources, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x5b }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       92 (0x5c)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2)
                  IL_0000:  call       "System.Collections.Generic.IAsyncEnumerable<int> C.F()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0023
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_001e:  call       "void System.Console.Write(int)"
                    IL_0023:  ldloc.0
                    IL_0024:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_0029:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_002e:  brtrue.s   IL_0018
                    IL_0030:  leave.s    IL_0035
                  }
                  catch object
                  {
                    IL_0032:  stloc.2
                    IL_0033:  leave.s    IL_0035
                  }
                  IL_0035:  ldloc.0
                  IL_0036:  brfalse.s  IL_0043
                  IL_0038:  ldloc.0
                  IL_0039:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_003e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0043:  ldloc.2
                  IL_0044:  brfalse.s  IL_005b
                  IL_0046:  ldloc.2
                  IL_0047:  isinst     "System.Exception"
                  IL_004c:  dup
                  IL_004d:  brtrue.s   IL_0051
                  IL_004f:  ldloc.2
                  IL_0050:  throw
                  IL_0051:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0056:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_005b:  ret
                }
                """);
        }

        [Fact]
        public void TestWithPattern_Ref_Iterator_Used()
        {
            var source = """
                using System.Collections.Generic;
                using System.Threading.Tasks;

                class C
                {
                    static async IAsyncEnumerable<int> F()
                    {
                        await foreach (ref var i in new C())
                        {
                            yield return i;
                            M(ref i);
                        }
                    }

                    static void M(ref int i) { }

                    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default) => new();

                    public sealed class Enumerator
                    {
                        private readonly int[] _array = [1, 2, 3];
                        private int _index = -1;
                        public Task<bool> MoveNextAsync()
                        {
                            if (_index < _array.Length) _index++;
                            return Task.FromResult(_index < _array.Length);
                        }       
                        public ref int Current => ref _array[_index];
                    }
                }
                """ + AsyncStreamsTypes;

            var comp = CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (8,32): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (ref var i in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "i").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 32));

            var expectedDiagnostics = new[]
            {
                // (11,19): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             M(ref i);
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "i").WithLocation(11, 19)
            };

            comp = CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular13);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void TestWithPattern_PointerType()
        {
            string source = @"
unsafe class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default) => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
        public int* Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (6,9): error CS4004: Cannot await in an unsafe context
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await").WithLocation(6, 9));

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            Assert.Equal(default, model.GetForEachStatementInfo(foreachSyntax));
        }

        [Fact]
        public void TestWithPattern_InaccessibleGetAsyncEnumerator()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new D())
        {
        }
    }
}
class D
{
    private Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token)
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (6,33): error CS8411: Async foreach statement cannot operate on variables of type 'D' because 'D' does not contain a public definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new D())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new D()").WithArguments("D", "GetAsyncEnumerator").WithLocation(6, 33)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            Assert.Equal(default, model.GetForEachStatementInfo(foreachSyntax));
        }

        [Fact]
        public void TestWithPattern_InaccessibleMoveNextAsync()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new D())
        {
        }
    }
}
class D
{
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
            => throw null;
    public sealed class Enumerator
    {
        private System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (6,33): error CS0122: 'D.Enumerator.MoveNextAsync()' is inaccessible due to its protection level
                //         await foreach (var i in new D())
                Diagnostic(ErrorCode.ERR_BadAccess, "new D()").WithArguments("D.Enumerator.MoveNextAsync()").WithLocation(6, 33),
                // (6,33): error CS8412: Async foreach requires that the return type 'D.Enumerator' of 'D.GetAsyncEnumerator(System.Threading.CancellationToken)' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new D())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new D()").WithArguments("D.Enumerator", "D.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(6, 33)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            Assert.Equal(default, model.GetForEachStatementInfo(foreachSyntax));
        }

        [Fact]
        public void TestWithPattern_InaccessibleCurrent()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new D()) { }
    }
}
class D
{
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
        private int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (6,33): error CS0122: 'D.Enumerator.Current' is inaccessible due to its protection level
                //         await foreach (var i in new D()) { }
                Diagnostic(ErrorCode.ERR_BadAccess, "new D()").WithArguments("D.Enumerator.Current").WithLocation(6, 33),
                // (6,33): error CS8412: Async foreach requires that the return type 'D.Enumerator' of 'D.GetAsyncEnumerator(System.Threading.CancellationToken)' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new D()) { }
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new D()").WithArguments("D.Enumerator", "D.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(6, 33)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            Assert.Equal(default, model.GetForEachStatementInfo(foreachSyntax));
        }

        [Fact]
        public void TestWithPattern_InaccessibleCurrentGetter()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new D()) { }
    }
}
class D
{
    public Enumerator GetAsyncEnumerator()
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
        public int Current { private get => throw null; set => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (6,33): error CS8412: Async foreach requires that the return type 'D.Enumerator' of 'D.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new D()) { }
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new D()").WithArguments("D.Enumerator", "D.GetAsyncEnumerator()").WithLocation(6, 33)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            Assert.Equal(default, model.GetForEachStatementInfo(foreachSyntax));
        }

        [Fact]
        public void TestWithPattern_RefStruct()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
public class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var s in new C())
        {
            Write($""{s.ToString()} "");
        }
        Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator() => new Enumerator();
    public sealed class Enumerator : System.IAsyncDisposable
    {
        int i = 0;
        public int Current => i;
        public async Task<bool> MoveNextAsync()
        {
            i++;
            return await Task.FromResult(i < 3);
        }
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
        }
    }
}
public ref struct S
{
    int i;
    public S(int i)
    {
        this.i = i;
    }
    public override string ToString() => i.ToString();
}
";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2 Done");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("1 2 Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x6e }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x21, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x24 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      111 (0x6f)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                object V_1,
                                int V_2) //s
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_002c
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "int C.Enumerator.Current.get"
                    IL_0015:  stloc.2
                    IL_0016:  ldloca.s   V_2
                    IL_0018:  call       "string int.ToString()"
                    IL_001d:  ldstr      " "
                    IL_0022:  call       "string string.Concat(string, string)"
                    IL_0027:  call       "void System.Console.Write(string)"
                    IL_002c:  ldloc.0
                    IL_002d:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                    IL_0032:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0037:  brtrue.s   IL_000f
                    IL_0039:  leave.s    IL_003e
                  }
                  catch object
                  {
                    IL_003b:  stloc.1
                    IL_003c:  leave.s    IL_003e
                  }
                  IL_003e:  ldloc.0
                  IL_003f:  brfalse.s  IL_004c
                  IL_0041:  ldloc.0
                  IL_0042:  callvirt   "System.Threading.Tasks.ValueTask C.Enumerator.DisposeAsync()"
                  IL_0047:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_004c:  ldloc.1
                  IL_004d:  brfalse.s  IL_0064
                  IL_004f:  ldloc.1
                  IL_0050:  isinst     "System.Exception"
                  IL_0055:  dup
                  IL_0056:  brtrue.s   IL_005a
                  IL_0058:  ldloc.1
                  IL_0059:  throw
                  IL_005a:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_005f:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0064:  ldstr      "Done"
                  IL_0069:  call       "void System.Console.Write(string)"
                  IL_006e:  ret
                }
                """);
        }

        [Theory]
        [InlineData("")]
        [InlineData("await Task.Yield();")]
        public void TestWithPattern_RefStructEnumerator_Async(string body)
        {
            var source = $$"""
                using System.Threading.Tasks;
                public class C
                {
                    public static async Task Main()
                    {
                        await foreach (var s in new C())
                        {
                            {{body}}
                        }
                    }
                    public Enumerator GetAsyncEnumerator() => new Enumerator();
                    public ref struct Enumerator
                    {
                        public int Current => 0;
                        public Task<bool> MoveNextAsync() => throw null;
                    }
                }
                """;

            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (6,15): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "foreach").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 15));

            var expectedDiagnostics = new[]
            {
                // (6,9): error CS4007: Instance of type 'C.Enumerator' cannot be preserved across 'await' or 'yield' boundary.
                //         await foreach (var s in new C())
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, @"await foreach (var s in new C())
        {
            " + body + @"
        }").WithArguments("C.Enumerator").WithLocation(6, 9)
            };

            CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilation(source).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Theory]
        [InlineData("")]
        [InlineData("await Task.Yield();")]
        [InlineData("yield return x;")]
        [InlineData("yield return x; await Task.Yield();")]
        [InlineData("await Task.Yield(); yield return x;")]
        public void TestWithPattern_RefStructEnumerator_AsyncIterator(string body)
        {
            var source = $$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class C
                {
                    public static async IAsyncEnumerable<int> M()
                    {
                        await foreach (var x in new C())
                        {
                            {{body}}
                        }
                        yield return -1;
                    }
                    public Enumerator GetAsyncEnumerator() => new Enumerator();
                    public ref struct Enumerator
                    {
                        public int Current => 0;
                        public Task<bool> MoveNextAsync() => throw null;
                    }
                }
                """ + AsyncStreamsTypes;

            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (7,15): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (var x in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "foreach").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 15));

            var expectedDiagnostics = new[]
            {
                // (7,9): error CS4007: Instance of type 'C.Enumerator' cannot be preserved across 'await' or 'yield' boundary.
                //         await foreach (var x in new C())
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, @"await foreach (var x in new C())
        {
            " + body + @"
        }").WithArguments("C.Enumerator").WithLocation(7, 9)
            };

            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilationWithTasksExtensions(source).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void TestWithPattern_RefStructEnumerator_Iterator()
        {
            var source = """
                using System.Collections.Generic;
                public class C
                {
                    public static IEnumerable<int> M()
                    {
                        foreach (var x in new C())
                        {
                            yield return x;
                        }
                    }
                    public Enumerator GetEnumerator() => new Enumerator();
                    public ref struct Enumerator
                    {
                        public int Current => 0;
                        public bool MoveNext() => throw null;
                    }
                }
                """;

            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (6,9): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (var x in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "foreach").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 9));

            var expectedDiagnostics = new[]
            {
                // (6,9): error CS4007: Instance of type 'C.Enumerator' cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (var x in new C())
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, @"foreach (var x in new C())
        {
            yield return x;
        }").WithArguments("C.Enumerator").WithLocation(6, 9)
            };

            CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilation(source).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void TestWithPattern_RefStructEnumerable_Async()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                public class C
                {
                    public static async Task Main()
                    {
                        await foreach (var x in new Enumerable())
                        {
                            await Task.Yield();
                            Console.Write($"{x} ");
                        }
                        Console.Write("Done");
                    }
                    public ref struct Enumerable
                    {
                        public Enumerator GetAsyncEnumerator() => new();
                    }
                    public class Enumerator
                    {
                        int i = 0;
                        public int Current => i;
                        public async Task<bool> MoveNextAsync()
                        {
                            i++;
                            await Task.Yield();
                            return i < 3;
                        }
                    }
                }
                """ + s_IAsyncEnumerable;
            var comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "1 2 Done").VerifyDiagnostics();

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("1 2 Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x7d }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x3b, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      126 (0x7e)
                  .maxstack  3
                  .locals init (C.Enumerator V_0,
                                C.Enumerable V_1,
                                int V_2, //x
                                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_3,
                                System.Runtime.CompilerServices.YieldAwaitable V_4,
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_5)
                  IL_0000:  ldloca.s   V_1
                  IL_0002:  dup
                  IL_0003:  initobj    "C.Enumerable"
                  IL_0009:  call       "C.Enumerator C.Enumerable.GetAsyncEnumerator()"
                  IL_000e:  stloc.0
                  IL_000f:  br.s       IL_0066
                  IL_0011:  ldloc.0
                  IL_0012:  callvirt   "int C.Enumerator.Current.get"
                  IL_0017:  stloc.2
                  IL_0018:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                  IL_001d:  stloc.s    V_4
                  IL_001f:  ldloca.s   V_4
                  IL_0021:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                  IL_0026:  stloc.3
                  IL_0027:  ldloca.s   V_3
                  IL_0029:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                  IL_002e:  brtrue.s   IL_0036
                  IL_0030:  ldloc.3
                  IL_0031:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                  IL_0036:  ldloca.s   V_3
                  IL_0038:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                  IL_003d:  ldloca.s   V_5
                  IL_003f:  ldc.i4.1
                  IL_0040:  ldc.i4.1
                  IL_0041:  call       "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                  IL_0046:  ldloca.s   V_5
                  IL_0048:  ldloc.2
                  IL_0049:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                  IL_004e:  ldloca.s   V_5
                  IL_0050:  ldstr      " "
                  IL_0055:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                  IL_005a:  ldloca.s   V_5
                  IL_005c:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                  IL_0061:  call       "void System.Console.Write(string)"
                  IL_0066:  ldloc.0
                  IL_0067:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_006c:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0071:  brtrue.s   IL_0011
                  IL_0073:  ldstr      "Done"
                  IL_0078:  call       "void System.Console.Write(string)"
                  IL_007d:  ret
                }
                """);
        }

        [Fact]
        public void TestWithPattern_RefStructEnumerable_AsyncIterator()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class C
                {
                    public static async Task Main()
                    {
                        await foreach (var i in M())
                        {
                            Console.Write($"{i} ");
                        }
                        Console.Write("Done");
                    }
                    public static async IAsyncEnumerable<int> M()
                    {
                        await foreach (var x in new Enumerable())
                        {
                            await Task.Yield();
                            yield return x * 2;
                        }
                        yield return -1;
                    }
                    public ref struct Enumerable
                    {
                        public Enumerator GetAsyncEnumerator() => new();
                    }
                    public class Enumerator
                    {
                        int i = 0;
                        public int Current => i;
                        public async Task<bool> MoveNextAsync()
                        {
                            i++;
                            await Task.Yield();
                            return i < 3;
                        }
                    }
                }
                """;
            var comp = CreateCompilationWithTasksExtensions([source, AsyncStreamsTypes], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "2 4 -1 Done").VerifyDiagnostics();

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("2 4 -1 Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x8a }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x3b, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      139 (0x8b)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                int V_3, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4)
                  IL_0000:  call       "System.Collections.Generic.IAsyncEnumerable<int> C.M()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0048
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_001e:  stloc.3
                    IL_001f:  ldc.i4.1
                    IL_0020:  ldc.i4.1
                    IL_0021:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0026:  stloc.s    V_4
                    IL_0028:  ldloca.s   V_4
                    IL_002a:  ldloc.3
                    IL_002b:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0030:  ldloca.s   V_4
                    IL_0032:  ldstr      " "
                    IL_0037:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_003c:  ldloca.s   V_4
                    IL_003e:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0043:  call       "void System.Console.Write(string)"
                    IL_0048:  ldloc.0
                    IL_0049:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_004e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_0053:  brtrue.s   IL_0018
                    IL_0055:  leave.s    IL_005a
                  }
                  catch object
                  {
                    IL_0057:  stloc.2
                    IL_0058:  leave.s    IL_005a
                  }
                  IL_005a:  ldloc.0
                  IL_005b:  brfalse.s  IL_0068
                  IL_005d:  ldloc.0
                  IL_005e:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0063:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0068:  ldloc.2
                  IL_0069:  brfalse.s  IL_0080
                  IL_006b:  ldloc.2
                  IL_006c:  isinst     "System.Exception"
                  IL_0071:  dup
                  IL_0072:  brtrue.s   IL_0076
                  IL_0074:  ldloc.2
                  IL_0075:  throw
                  IL_0076:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_007b:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0080:  ldstr      "Done"
                  IL_0085:  call       "void System.Console.Write(string)"
                  IL_008a:  ret
                }
                """);
        }

        [Fact]
        public void TestWithPattern_RefStructEnumerable_Iterator()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                public class C
                {
                    public static void Main()
                    {
                        foreach (var i in M())
                        {
                            Console.Write($"{i} ");
                        }
                        Console.Write("Done");
                    }
                    public static IEnumerable<int> M()
                    {
                        foreach (var x in new Enumerable())
                        {
                            yield return x * 2;
                        }
                        yield return -1;
                    }
                    public ref struct Enumerable
                    {
                        public Enumerator GetEnumerator() => new();
                    }
                    public class Enumerator
                    {
                        int i = 0;
                        public int Current => i;
                        public bool MoveNext()
                        {
                            i++;
                            return i < 3;
                        }
                    }
                }
                """;
            CompileAndVerify(source, expectedOutput: "2 4 -1 Done").VerifyDiagnostics();
        }

        [Fact]
        public void TestWithPattern_RefStructCurrent_Async()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                public class C
                {
                    public static async Task Main()
                    {
                        await foreach (var s in new C())
                        {
                            Console.Write($"{s.ToString()} ");
                        }
                        Console.Write("Done");
                    }
                    public Enumerator GetAsyncEnumerator() => new Enumerator();
                    public sealed class Enumerator : IAsyncDisposable
                    {
                        int i = 0;
                        public S Current => new S(i);
                        public async Task<bool> MoveNextAsync()
                        {
                            i++;
                            await Task.Yield();
                            return i < 3;
                        }
                        public async ValueTask DisposeAsync()
                        {
                            await Task.Yield();
                        }
                    }
                }
                public ref struct S
                {
                    int i;
                    public S(int i)
                    {
                        this.i = i;
                    }
                    public override string ToString() => i.ToString();
                }
                """ + s_IAsyncEnumerable;

            var expectedDiagnostics = new[]
            {
                // (7,24): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "var").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 24)
            };

            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);

            var expectedOutput = "1 2 Done";

            var comp = CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular13, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();

            comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x74 }
                    [get_Current]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xb }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x3b, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x24 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      117 (0x75)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                object V_1,
                                S V_2) //s
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_0032
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "S C.Enumerator.Current.get"
                    IL_0015:  stloc.2
                    IL_0016:  ldloca.s   V_2
                    IL_0018:  constrained. "S"
                    IL_001e:  callvirt   "string object.ToString()"
                    IL_0023:  ldstr      " "
                    IL_0028:  call       "string string.Concat(string, string)"
                    IL_002d:  call       "void System.Console.Write(string)"
                    IL_0032:  ldloc.0
                    IL_0033:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                    IL_0038:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_003d:  brtrue.s   IL_000f
                    IL_003f:  leave.s    IL_0044
                  }
                  catch object
                  {
                    IL_0041:  stloc.1
                    IL_0042:  leave.s    IL_0044
                  }
                  IL_0044:  ldloc.0
                  IL_0045:  brfalse.s  IL_0052
                  IL_0047:  ldloc.0
                  IL_0048:  callvirt   "System.Threading.Tasks.ValueTask C.Enumerator.DisposeAsync()"
                  IL_004d:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0052:  ldloc.1
                  IL_0053:  brfalse.s  IL_006a
                  IL_0055:  ldloc.1
                  IL_0056:  isinst     "System.Exception"
                  IL_005b:  dup
                  IL_005c:  brtrue.s   IL_0060
                  IL_005e:  ldloc.1
                  IL_005f:  throw
                  IL_0060:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0065:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_006a:  ldstr      "Done"
                  IL_006f:  call       "void System.Console.Write(string)"
                  IL_0074:  ret
                }
                """);
        }

        [Fact]
        public void TestWithPattern_RefStructCurrent_AsyncIterator()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class C
                {
                    public static async Task Main()
                    {
                        await foreach (var s in M())
                        {
                            Console.Write($"M:{s} ");
                        }
                        Console.Write("MainDone");
                    }
                    public static async IAsyncEnumerable<string> M()
                    {
                        await foreach (var s in new C())
                        {
                            yield return s.ToString();
                        }
                        yield return "Done";
                    }
                    public Enumerator GetAsyncEnumerator() => new Enumerator();
                    public sealed class Enumerator : IAsyncDisposable
                    {
                        int i = 0;
                        public S Current => new S(i);
                        public async Task<bool> MoveNextAsync()
                        {
                            i++;
                            await Task.Yield();
                            return i < 3;
                        }
                        public async ValueTask DisposeAsync()
                        {
                            await Task.Yield();
                        }
                    }
                }
                public ref struct S
                {
                    int i;
                    public S(int i)
                    {
                        this.i = i;
                    }
                    public override string ToString() => i.ToString();
                }
                """;

            var expectedDiagnostics = new[]
            {
                // (16,24): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "var").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(16, 24)
            };

            CSharpTestSource sources = [source, AsyncStreamsTypes];
            CreateCompilationWithTasksExtensions(sources, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);

            var expectedOutput = "M:1 M:2 M:Done MainDone";

            var comp = CreateCompilationWithTasksExtensions(sources, parseOptions: TestOptions.Regular13, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();

            comp = CreateCompilationWithTasksExtensions(sources, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x76 }
                    [get_Current]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xb }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x3b, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x24 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      119 (0x77)
                  .maxstack  3
                  .locals init (System.Collections.Generic.IAsyncEnumerator<string> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                string V_3) //s
                  IL_0000:  call       "System.Collections.Generic.IAsyncEnumerable<string> C.M()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<string> System.Collections.Generic.IAsyncEnumerable<string>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0034
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "string System.Collections.Generic.IAsyncEnumerator<string>.Current.get"
                    IL_001e:  stloc.3
                    IL_001f:  ldstr      "M:"
                    IL_0024:  ldloc.3
                    IL_0025:  ldstr      " "
                    IL_002a:  call       "string string.Concat(string, string, string)"
                    IL_002f:  call       "void System.Console.Write(string)"
                    IL_0034:  ldloc.0
                    IL_0035:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<string>.MoveNextAsync()"
                    IL_003a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_003f:  brtrue.s   IL_0018
                    IL_0041:  leave.s    IL_0046
                  }
                  catch object
                  {
                    IL_0043:  stloc.2
                    IL_0044:  leave.s    IL_0046
                  }
                  IL_0046:  ldloc.0
                  IL_0047:  brfalse.s  IL_0054
                  IL_0049:  ldloc.0
                  IL_004a:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_004f:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0054:  ldloc.2
                  IL_0055:  brfalse.s  IL_006c
                  IL_0057:  ldloc.2
                  IL_0058:  isinst     "System.Exception"
                  IL_005d:  dup
                  IL_005e:  brtrue.s   IL_0062
                  IL_0060:  ldloc.2
                  IL_0061:  throw
                  IL_0062:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0067:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_006c:  ldstr      "MainDone"
                  IL_0071:  call       "void System.Console.Write(string)"
                  IL_0076:  ret
                }
                """);
        }

        [Fact]
        public void TestWithPattern_RefStructCurrent_Iterator()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                public class C
                {
                    public static void Main()
                    {
                        foreach (var s in M())
                        {
                            Console.Write($"M:{s} ");
                        }
                        Console.Write("MainDone");
                    }
                    public static IEnumerable<string> M()
                    {
                        foreach (var s in new C())
                        {
                            yield return s.ToString();
                        }
                        yield return "Done";
                    }
                    public Enumerator GetEnumerator() => new Enumerator();
                    public sealed class Enumerator
                    {
                        int i = 0;
                        public S Current => new S(i);
                        public bool MoveNext()
                        {
                            i++;
                            return i < 3;
                        }
                    }
                }
                public ref struct S
                {
                    int i;
                    public S(int i)
                    {
                        this.i = i;
                    }
                    public override string ToString() => i.ToString();
                }
                """;

            var expectedOutput = "M:1 M:2 M:Done MainDone";

            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.Regular13, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();
        }

        [Fact]
        public void TestWithPattern_RefReturningCurrent_Async()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
public class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var s in new C())
        {
            Write($""{s.ToString()} "");
        }
        Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator() => new Enumerator();
    public sealed class Enumerator
    {
        int i = 0;
        S current;
        public ref S Current
        {
            get
            {
                current = new S(i);
                return ref current;
            }
        }
        public async Task<bool> MoveNextAsync()
        {
            i++;
            return await Task.FromResult(i < 4);
        }
    }
}
public struct S
{
    int i;
    public S(int i)
    {
        this.i = i;
    }
    public override string ToString()
        => i.ToString();
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2 3 Done", verify: Verification.Fails);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("1 2 3 Done"), verify: Verification.Fails);
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       77 (0x4d)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                S V_1) //s
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0035
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "ref S C.Enumerator.Current.get"
                  IL_0013:  ldobj      "S"
                  IL_0018:  stloc.1
                  IL_0019:  ldloca.s   V_1
                  IL_001b:  constrained. "S"
                  IL_0021:  callvirt   "string object.ToString()"
                  IL_0026:  ldstr      " "
                  IL_002b:  call       "string string.Concat(string, string)"
                  IL_0030:  call       "void System.Console.Write(string)"
                  IL_0035:  ldloc.0
                  IL_0036:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_003b:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0040:  brtrue.s   IL_000d
                  IL_0042:  ldstr      "Done"
                  IL_0047:  call       "void System.Console.Write(string)"
                  IL_004c:  ret
                }
                """);
        }

        [Fact]
        public void TestWithPattern_RefReturningCurrent_Async_RefVariable()
        {
            string source = """
                using System;
                using System.Threading.Tasks;
                public class C
                {
                    public static async Task Main()
                    {
                        await foreach (ref var s in new C())
                        {
                            Console.Write($"{s} ");
                            s.F++;
                        }
                        Console.Write("Done");
                    }
                    public Enumerator GetAsyncEnumerator() => new Enumerator();
                    public sealed class Enumerator
                    {
                        S _current;
                        public ref S Current => ref _current;
                        public async Task<bool> MoveNextAsync()
                        {
                            Current = new S(Current.F + 1);
                            await Task.Yield();
                            return Current.F < 4;
                        }
                    }
                }
                public struct S
                {
                    public int F;
                    public S(int i)
                    {
                        this.F = i;
                    }
                    public override string ToString() => F.ToString();
                }
                """ + s_IAsyncEnumerable;

            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (7,32): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (ref var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "s").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 32));

            var expectedOutput = "1 3 Done";

            var comp = CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular13, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x64 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x4f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      101 (0x65)
                  .maxstack  3
                  .locals init (C.Enumerator V_0,
                                S& V_1, //s
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_2)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_004d
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "ref S C.Enumerator.Current.get"
                  IL_0013:  stloc.1
                  IL_0014:  ldloca.s   V_2
                  IL_0016:  ldc.i4.1
                  IL_0017:  ldc.i4.1
                  IL_0018:  call       "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                  IL_001d:  ldloca.s   V_2
                  IL_001f:  ldloc.1
                  IL_0020:  ldobj      "S"
                  IL_0025:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<S>(S)"
                  IL_002a:  ldloca.s   V_2
                  IL_002c:  ldstr      " "
                  IL_0031:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                  IL_0036:  ldloca.s   V_2
                  IL_0038:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                  IL_003d:  call       "void System.Console.Write(string)"
                  IL_0042:  ldloc.1
                  IL_0043:  ldflda     "int S.F"
                  IL_0048:  dup
                  IL_0049:  ldind.i4
                  IL_004a:  ldc.i4.1
                  IL_004b:  add
                  IL_004c:  stind.i4
                  IL_004d:  ldloc.0
                  IL_004e:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_0053:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0058:  brtrue.s   IL_000d
                  IL_005a:  ldstr      "Done"
                  IL_005f:  call       "void System.Console.Write(string)"
                  IL_0064:  ret
                }
                """);
        }

        [Fact]
        public void TestWithPattern_RefReturningCurrent_AsyncIterator_RefVariable_01()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class C
                {
                    public static async Task Main()
                    {
                        await foreach (var s in M())
                        {
                            Console.Write($"M:{s} ");
                        }
                        Console.Write("MainDone");
                    }
                    public static async IAsyncEnumerable<string> M()
                    {
                        await foreach (ref var s in new C())
                        {
                            s.F++;
                            yield return s.ToString();
                        }
                        yield return "Done";
                    }
                    public Enumerator GetAsyncEnumerator() => new Enumerator();
                    public sealed class Enumerator
                    {
                        S _current;
                        public ref S Current => ref _current;
                        public async Task<bool> MoveNextAsync()
                        {
                            Current = new S(Current.F + 1);
                            await Task.Yield();
                            return Current.F < 4;
                        }
                    }
                }
                public struct S
                {
                    public int F;
                    public S(int i)
                    {
                        this.F = i;
                    }
                    public override string ToString() => F.ToString();
                }
                """;

            CSharpTestSource sources = [source, AsyncStreamsTypes];
            CreateCompilationWithTasksExtensions(sources, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (16,32): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (ref var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "s").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(16, 32));

            var expectedOutput = "M:2 M:4 M:Done MainDone";

            var comp = CreateCompilationWithTasksExtensions(sources, parseOptions: TestOptions.Regular13, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp = CreateCompilationWithTasksExtensions(sources, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x76 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x4f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      119 (0x77)
                  .maxstack  3
                  .locals init (System.Collections.Generic.IAsyncEnumerator<string> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                string V_3) //s
                  IL_0000:  call       "System.Collections.Generic.IAsyncEnumerable<string> C.M()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<string> System.Collections.Generic.IAsyncEnumerable<string>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0034
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "string System.Collections.Generic.IAsyncEnumerator<string>.Current.get"
                    IL_001e:  stloc.3
                    IL_001f:  ldstr      "M:"
                    IL_0024:  ldloc.3
                    IL_0025:  ldstr      " "
                    IL_002a:  call       "string string.Concat(string, string, string)"
                    IL_002f:  call       "void System.Console.Write(string)"
                    IL_0034:  ldloc.0
                    IL_0035:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<string>.MoveNextAsync()"
                    IL_003a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_003f:  brtrue.s   IL_0018
                    IL_0041:  leave.s    IL_0046
                  }
                  catch object
                  {
                    IL_0043:  stloc.2
                    IL_0044:  leave.s    IL_0046
                  }
                  IL_0046:  ldloc.0
                  IL_0047:  brfalse.s  IL_0054
                  IL_0049:  ldloc.0
                  IL_004a:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_004f:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0054:  ldloc.2
                  IL_0055:  brfalse.s  IL_006c
                  IL_0057:  ldloc.2
                  IL_0058:  isinst     "System.Exception"
                  IL_005d:  dup
                  IL_005e:  brtrue.s   IL_0062
                  IL_0060:  ldloc.2
                  IL_0061:  throw
                  IL_0062:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0067:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_006c:  ldstr      "MainDone"
                  IL_0071:  call       "void System.Console.Write(string)"
                  IL_0076:  ret
                }
                """);
        }

        [Fact]
        public void TestWithPattern_RefReturningCurrent_AsyncIterator_RefVariable_02()
        {
            string source = """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class C
                {
                    public static async IAsyncEnumerable<string> M()
                    {
                        await foreach (ref var s in new C())
                        {
                            yield return s.ToString();
                            s.F++;
                        }
                        yield return "Done";
                    }
                    public Enumerator GetAsyncEnumerator() => new Enumerator();
                    public sealed class Enumerator
                    {
                        public ref S Current => throw null;
                        public Task<bool> MoveNextAsync() => throw null;
                    }
                }
                public struct S
                {
                    public int F;
                }
                """ + AsyncStreamsTypes;

            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (7,32): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await foreach (ref var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "s").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 32));

            var expectedDiagnostics = new[]
            {
                // (10,13): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             s.F++;
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "s.F").WithLocation(10, 13)
            };

            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilationWithTasksExtensions(source).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void TestWithPattern_RefReturningCurrent_Iterator_RefVariable_01()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                public class C
                {
                    public static void Main()
                    {
                        foreach (var s in M())
                        {
                            Console.Write($"M:{s} ");
                        }
                        Console.Write("MainDone");
                    }
                    public static IEnumerable<string> M()
                    {
                        foreach (ref var s in new C())
                        {
                            s.F++;
                            yield return s.ToString();
                        }
                        yield return "Done";
                    }
                    public Enumerator GetEnumerator() => new Enumerator();
                    public sealed class Enumerator
                    {
                        S _current;
                        public ref S Current => ref _current;
                        public bool MoveNext()
                        {
                            Current = new S(Current.F + 1);
                            return Current.F < 4;
                        }
                    }
                }
                public struct S
                {
                    public int F;
                    public S(int i)
                    {
                        this.F = i;
                    }
                    public override string ToString() => F.ToString();
                }
                """;

            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (15,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "s").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(15, 26));

            var expectedOutput = "M:2 M:4 M:Done MainDone";

            CompileAndVerify(source, parseOptions: TestOptions.Regular13, expectedOutput: expectedOutput).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void TestWithPattern_RefReturningCurrent_Iterator_RefVariable_02()
        {
            string source = """
                using System.Collections.Generic;
                public class C
                {
                    public static IEnumerable<string> M()
                    {
                        foreach (ref var s in new C())
                        {
                            yield return s.ToString();
                            s.F++;
                        }
                        yield return "Done";
                    }
                    public Enumerator GetEnumerator() => new Enumerator();
                    public sealed class Enumerator
                    {
                        public ref S Current => throw null;
                        public bool MoveNext() => throw null;
                    }
                }
                public struct S
                {
                    public int F;
                }
                """;

            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (6,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref var s in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "s").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 26));

            var expectedDiagnostics = new[]
            {
                // (9,13): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             s.F++;
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "s.F").WithLocation(9, 13)
            };

            CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilation(source).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void TestWithPattern_IterationVariableIsReadOnly()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
            i = 1;
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
        => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable });
            comp.VerifyDiagnostics(
                // (8,13): error CS1656: Cannot assign to 'i' because it is a 'foreach iteration variable'
                //             i = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "foreach iteration variable").WithLocation(8, 13)
                );
        }

        [Fact]
        public void TestWithPattern_WithStruct_MoveNextAsyncReturnsTask()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var i in new C())
        {
            Write($""Got({i}) "");
        }
        Write($""Done"");
    }
    public AsyncEnumerator GetAsyncEnumerator()
    {
        return new AsyncEnumerator(0);
    }
    public struct AsyncEnumerator
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                i++;
                return i;
            }
        }
        public async Task<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            bool more = await Task.FromResult(i < 4);
            i = i + 100; // Note: side-effects of async methods in structs are lost
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""DisposeAsync "");
            await Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var expectedOutput = "NextAsync(0) Current(0) Got(1) NextAsync(1) Current(1) Got(2) NextAsync(2) Current(2) Got(3) NextAsync(3) Current(3) Got(4) NextAsync(4) DisposeAsync Done";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x8c }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x65, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      141 (0x8d)
                  .maxstack  2
                  .locals init (C.AsyncEnumerator V_0,
                                object V_1,
                                int V_2, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_3)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.AsyncEnumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_004b
                    IL_000f:  ldloca.s   V_0
                    IL_0011:  call       "int C.AsyncEnumerator.Current.get"
                    IL_0016:  stloc.2
                    IL_0017:  ldc.i4.6
                    IL_0018:  ldc.i4.1
                    IL_0019:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_001e:  stloc.3
                    IL_001f:  ldloca.s   V_3
                    IL_0021:  ldstr      "Got("
                    IL_0026:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_002b:  ldloca.s   V_3
                    IL_002d:  ldloc.2
                    IL_002e:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0033:  ldloca.s   V_3
                    IL_0035:  ldstr      ") "
                    IL_003a:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_003f:  ldloca.s   V_3
                    IL_0041:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0046:  call       "void System.Console.Write(string)"
                    IL_004b:  ldloca.s   V_0
                    IL_004d:  call       "System.Threading.Tasks.Task<bool> C.AsyncEnumerator.MoveNextAsync()"
                    IL_0052:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0057:  brtrue.s   IL_000f
                    IL_0059:  leave.s    IL_005e
                  }
                  catch object
                  {
                    IL_005b:  stloc.1
                    IL_005c:  leave.s    IL_005e
                  }
                  IL_005e:  ldloca.s   V_0
                  IL_0060:  call       "System.Threading.Tasks.ValueTask C.AsyncEnumerator.DisposeAsync()"
                  IL_0065:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_006a:  ldloc.1
                  IL_006b:  brfalse.s  IL_0082
                  IL_006d:  ldloc.1
                  IL_006e:  isinst     "System.Exception"
                  IL_0073:  dup
                  IL_0074:  brtrue.s   IL_0078
                  IL_0076:  ldloc.1
                  IL_0077:  throw
                  IL_0078:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_007d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0082:  ldstr      "Done"
                  IL_0087:  call       "void System.Console.Write(string)"
                  IL_008c:  ret
                }
                """);
        }

        [Fact]
        public void TestWithPattern_MoveNextAsyncReturnsValueTask()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var i in new C())
        {
            Write($""Got({i}) "");
        }
        Write($""Done"");
    }
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        return new AsyncEnumerator(0);
    }
    public class AsyncEnumerator : System.IAsyncDisposable
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 4);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var expectedOutput = "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4) Done";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> C.AsyncEnumerator.MoveNextAsync()", info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposal);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x96 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x5f }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      151 (0x97)
                  .maxstack  2
                  .locals init (C.AsyncEnumerator V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                int V_3, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  call       "C.AsyncEnumerator C.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0054
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int C.AsyncEnumerator.Current.get"
                    IL_001e:  stloc.3
                    IL_001f:  ldc.i4.6
                    IL_0020:  ldc.i4.1
                    IL_0021:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0026:  stloc.s    V_4
                    IL_0028:  ldloca.s   V_4
                    IL_002a:  ldstr      "Got("
                    IL_002f:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0034:  ldloca.s   V_4
                    IL_0036:  ldloc.3
                    IL_0037:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_003c:  ldloca.s   V_4
                    IL_003e:  ldstr      ") "
                    IL_0043:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0048:  ldloca.s   V_4
                    IL_004a:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_004f:  call       "void System.Console.Write(string)"
                    IL_0054:  ldloc.0
                    IL_0055:  callvirt   "System.Threading.Tasks.ValueTask<bool> C.AsyncEnumerator.MoveNextAsync()"
                    IL_005a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_005f:  brtrue.s   IL_0018
                    IL_0061:  leave.s    IL_0066
                  }
                  catch object
                  {
                    IL_0063:  stloc.2
                    IL_0064:  leave.s    IL_0066
                  }
                  IL_0066:  ldloc.0
                  IL_0067:  brfalse.s  IL_0074
                  IL_0069:  ldloc.0
                  IL_006a:  callvirt   "System.Threading.Tasks.ValueTask C.AsyncEnumerator.DisposeAsync()"
                  IL_006f:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0074:  ldloc.2
                  IL_0075:  brfalse.s  IL_008c
                  IL_0077:  ldloc.2
                  IL_0078:  isinst     "System.Exception"
                  IL_007d:  dup
                  IL_007e:  brtrue.s   IL_0082
                  IL_0080:  ldloc.2
                  IL_0081:  throw
                  IL_0082:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0087:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_008c:  ldstr      "Done"
                  IL_0091:  call       "void System.Console.Write(string)"
                  IL_0096:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(31609, "https://github.com/dotnet/roslyn/issues/31609")]
        public void TestWithPattern_MoveNextAsyncReturnsAwaitable()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var i in new C())
        {
            Write($""Item({i}) "");
            break;
        }
        Write($""Done"");
    }
    public AsyncEnumerator GetAsyncEnumerator()
    {
        return new AsyncEnumerator();
    }
    public class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current => 1;
        public Awaitable MoveNextAsync()
        {
            return new Awaitable();
        }
        public async ValueTask DisposeAsync()
        {
            Write(""Dispose "");
            await Task.Yield();
        }
    }

    public class Awaitable
    {
        public Awaiter GetAwaiter() { return new Awaiter(); }
    }
    public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool IsCompleted { get { return true; } }
        public bool GetResult() { return true; }
        public void OnCompleted(System.Action continuation) { }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Item(1) Dispose Done");

            var tree = comp.SyntaxTrees.First();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("C.Awaitable C.AsyncEnumerator.MoveNextAsync()", info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("Item(1) Dispose Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xa7 }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      168 (0xa8)
                  .maxstack  2
                  .locals init (C.AsyncEnumerator V_0,
                                object V_1,
                                int V_2, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_3,
                                C.Awaiter V_4)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.AsyncEnumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_004c
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "int C.AsyncEnumerator.Current.get"
                    IL_0015:  stloc.2
                    IL_0016:  ldc.i4.7
                    IL_0017:  ldc.i4.1
                    IL_0018:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_001d:  stloc.3
                    IL_001e:  ldloca.s   V_3
                    IL_0020:  ldstr      "Item("
                    IL_0025:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_002a:  ldloca.s   V_3
                    IL_002c:  ldloc.2
                    IL_002d:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0032:  ldloca.s   V_3
                    IL_0034:  ldstr      ") "
                    IL_0039:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_003e:  ldloca.s   V_3
                    IL_0040:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0045:  call       "void System.Console.Write(string)"
                    IL_004a:  br.s       IL_0072
                    IL_004c:  ldloc.0
                    IL_004d:  callvirt   "C.Awaitable C.AsyncEnumerator.MoveNextAsync()"
                    IL_0052:  callvirt   "C.Awaiter C.Awaitable.GetAwaiter()"
                    IL_0057:  stloc.s    V_4
                    IL_0059:  ldloc.s    V_4
                    IL_005b:  callvirt   "bool C.Awaiter.IsCompleted.get"
                    IL_0060:  brtrue.s   IL_0069
                    IL_0062:  ldloc.s    V_4
                    IL_0064:  call       "void System.Runtime.CompilerServices.AsyncHelpers.AwaitAwaiter<C.Awaiter>(C.Awaiter)"
                    IL_0069:  ldloc.s    V_4
                    IL_006b:  callvirt   "bool C.Awaiter.GetResult()"
                    IL_0070:  brtrue.s   IL_000f
                    IL_0072:  leave.s    IL_0077
                  }
                  catch object
                  {
                    IL_0074:  stloc.1
                    IL_0075:  leave.s    IL_0077
                  }
                  IL_0077:  ldloc.0
                  IL_0078:  brfalse.s  IL_0085
                  IL_007a:  ldloc.0
                  IL_007b:  callvirt   "System.Threading.Tasks.ValueTask C.AsyncEnumerator.DisposeAsync()"
                  IL_0080:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0085:  ldloc.1
                  IL_0086:  brfalse.s  IL_009d
                  IL_0088:  ldloc.1
                  IL_0089:  isinst     "System.Exception"
                  IL_008e:  dup
                  IL_008f:  brtrue.s   IL_0093
                  IL_0091:  ldloc.1
                  IL_0092:  throw
                  IL_0093:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0098:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_009d:  ldstr      "Done"
                  IL_00a2:  call       "void System.Console.Write(string)"
                  IL_00a7:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(31609, "https://github.com/dotnet/roslyn/issues/31609")]
        public void TestWithPattern_MoveNextAsyncReturnsAwaitable_WithoutGetAwaiter()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default) => throw null;
    public class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current => 1;
        public Awaitable MoveNextAsync() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
    public class Awaitable
    {
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,33): error CS1061: 'C.Awaitable' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'C.Awaitable' could be found (are you missing a using directive or an assembly reference?)
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "new C()").WithArguments("C.Awaitable", "GetAwaiter").WithLocation(7, 33)
                );
            VerifyEmptyForEachStatementInfo(comp);
        }

        [Fact]
        [WorkItem(31609, "https://github.com/dotnet/roslyn/issues/31609")]
        public void TestWithPattern_MoveNextAsyncReturnsAwaitable_WithoutIsCompleted()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public AsyncEnumerator GetAsyncEnumerator() => throw null;
    public class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current => 1;
        public Awaitable MoveNextAsync() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
    public class Awaitable
    {
        public Awaiter GetAwaiter() => throw null;
    }
    public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool GetResult() { return true; }
        public void OnCompleted(System.Action continuation) { }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,33): error CS0117: 'C.Awaiter' does not contain a definition for 'IsCompleted'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Awaiter", "IsCompleted").WithLocation(7, 33)
                );
            VerifyEmptyForEachStatementInfo(comp);
        }

        private static void VerifyEmptyForEachStatementInfo(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Null(info.MoveNextMethod);
            Assert.Null(info.ElementType);
        }

        [Fact]
        [WorkItem(31609, "https://github.com/dotnet/roslyn/issues/31609")]
        public void TestWithPattern_MoveNextAsyncReturnsAwaitable_WithoutGetResult()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public AsyncEnumerator GetAsyncEnumerator() => throw null;
    public class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current => 1;
        public Awaitable MoveNextAsync() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
    public class Awaitable
    {
        public Awaiter GetAwaiter() => throw null;
    }
    public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool IsCompleted { get { return true; } }
        public void OnCompleted(System.Action continuation) { }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,33): error CS1061: 'C.Awaiter' does not contain a definition for 'GetResult' and no accessible extension method 'GetResult' accepting a first argument of type 'C.Awaiter' could be found (are you missing a using directive or an assembly reference?)
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "new C()").WithArguments("C.Awaiter", "GetResult").WithLocation(7, 33)
                );
            VerifyEmptyForEachStatementInfo(comp);
        }

        [Fact]
        [WorkItem(31609, "https://github.com/dotnet/roslyn/issues/31609")]
        public void TestWithPattern_MoveNextAsyncReturnsAwaitable_WithoutOnCompleted()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default) => throw null;
    public class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current => 1;
        public Awaitable MoveNextAsync() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }

    public class Awaitable
    {
        public Awaiter GetAwaiter() => throw null;
    }
    public class Awaiter
    {
        public bool IsCompleted { get { return true; } }
        public bool GetResult() { return true; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,33): error CS4027: 'C.Awaiter' does not implement 'INotifyCompletion'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_DoesntImplementAwaitInterface, "new C()").WithArguments("C.Awaiter", "System.Runtime.CompilerServices.INotifyCompletion").WithLocation(7, 33)
                );
            VerifyEmptyForEachStatementInfo(comp);
        }

        [Fact]
        public void TestWithPattern_MoveNextAsyncReturnsBadType()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public AsyncEnumerator GetAsyncEnumerator() => throw null;
    public class AsyncEnumerator
    {
        public int Current => throw null;
        public int MoveNextAsync() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable });
            comp.VerifyDiagnostics(
                // (7,33): error CS1061: 'int' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "new C()").WithArguments("int", "GetAwaiter").WithLocation(7, 33)
                );

            var tree = comp.SyntaxTrees.First();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);
            Assert.Null(info.MoveNextMethod);
            Assert.Null(info.ElementType);
        }

        [Fact]
        public void TestWithPattern_WithUnsealed()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Write($""Got({i}) "");
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public class Enumerator
    {
        int i = 0;
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async Task<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 4);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Yield();
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposal);

            string expectedOutput = "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4)";
            CompileAndVerify(comp,
                expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x82 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x68 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      131 (0x83)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                object V_1,
                                int V_2, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_3)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_004a
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "int C.Enumerator.Current.get"
                    IL_0015:  stloc.2
                    IL_0016:  ldc.i4.6
                    IL_0017:  ldc.i4.1
                    IL_0018:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_001d:  stloc.3
                    IL_001e:  ldloca.s   V_3
                    IL_0020:  ldstr      "Got("
                    IL_0025:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_002a:  ldloca.s   V_3
                    IL_002c:  ldloc.2
                    IL_002d:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0032:  ldloca.s   V_3
                    IL_0034:  ldstr      ") "
                    IL_0039:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_003e:  ldloca.s   V_3
                    IL_0040:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0045:  call       "void System.Console.Write(string)"
                    IL_004a:  ldloc.0
                    IL_004b:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                    IL_0050:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0055:  brtrue.s   IL_000f
                    IL_0057:  leave.s    IL_005c
                  }
                  catch object
                  {
                    IL_0059:  stloc.1
                    IL_005a:  leave.s    IL_005c
                  }
                  IL_005c:  ldloc.0
                  IL_005d:  brfalse.s  IL_006a
                  IL_005f:  ldloc.0
                  IL_0060:  callvirt   "System.Threading.Tasks.ValueTask C.Enumerator.DisposeAsync()"
                  IL_0065:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_006a:  ldloc.1
                  IL_006b:  brfalse.s  IL_0082
                  IL_006d:  ldloc.1
                  IL_006e:  isinst     "System.Exception"
                  IL_0073:  dup
                  IL_0074:  brtrue.s   IL_0078
                  IL_0076:  ldloc.1
                  IL_0077:  throw
                  IL_0078:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_007d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0082:  ret
                }
                """);
        }

        [Fact]
        public void TestWithPattern_WithUnsealed_WithIAsyncDisposable()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Write($""Got({i}) "");
        }
    }
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        return new DerivedEnumerator();
    }
    public class Enumerator
    {
        protected int i = 0;
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async Task<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 4);
        }
    }
    public class DerivedEnumerator : Enumerator, System.IAsyncDisposable
    {
        public ValueTask DisposeAsync() => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.False(internalInfo.NeedsDisposal);

            var verifier = CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3)");

            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      273 (0x111)
  .maxstack  3
  .locals init (int V_0,
                System.Threading.CancellationToken V_1,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_2,
                C.<Main>d__0 V_3,
                System.Exception V_4)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0017
    IL_0010:  br.s       IL_001c
    IL_0012:  br         IL_00a5
    IL_0017:  br         IL_00a5
    // sequence point: {
    IL_001c:  nop
    // sequence point: await foreach
    IL_001d:  nop
    // sequence point: new C()
    IL_001e:  ldarg.0
    IL_001f:  newobj     ""C..ctor()""
    IL_0024:  ldloca.s   V_1
    IL_0026:  initobj    ""System.Threading.CancellationToken""
    IL_002c:  ldloc.1
    IL_002d:  call       ""C.Enumerator C.GetAsyncEnumerator(System.Threading.CancellationToken)""
    IL_0032:  stfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    // sequence point: <hidden>
    IL_0037:  br.s       IL_0067
    // sequence point: var i
    IL_0039:  ldarg.0
    IL_003a:  ldarg.0
    IL_003b:  ldfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    IL_0040:  callvirt   ""int C.Enumerator.Current.get""
    IL_0045:  stfld      ""int C.<Main>d__0.<i>5__2""
    // sequence point: {
    IL_004a:  nop
    // sequence point: Write($""Got({i}) "");
    IL_004b:  ldstr      ""Got({0}) ""
    IL_0050:  ldarg.0
    IL_0051:  ldfld      ""int C.<Main>d__0.<i>5__2""
    IL_0056:  box        ""int""
    IL_005b:  call       ""string string.Format(string, object)""
    IL_0060:  call       ""void System.Console.Write(string)""
    IL_0065:  nop
    // sequence point: }
    IL_0066:  nop
    // sequence point: in
    IL_0067:  ldarg.0
    IL_0068:  ldfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    IL_006d:  callvirt   ""System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()""
    IL_0072:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_0077:  stloc.2
    // sequence point: <hidden>
    IL_0078:  ldloca.s   V_2
    IL_007a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_007f:  brtrue.s   IL_00c1
    IL_0081:  ldarg.0
    IL_0082:  ldc.i4.0
    IL_0083:  dup
    IL_0084:  stloc.0
    IL_0085:  stfld      ""int C.<Main>d__0.<>1__state""
    // async: yield
    IL_008a:  ldarg.0
    IL_008b:  ldloc.2
    IL_008c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_0091:  ldarg.0
    IL_0092:  stloc.3
    IL_0093:  ldarg.0
    IL_0094:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_0099:  ldloca.s   V_2
    IL_009b:  ldloca.s   V_3
    IL_009d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Main>d__0)""
    IL_00a2:  nop
    IL_00a3:  leave.s    IL_0110
    // async: resume
    IL_00a5:  ldarg.0
    IL_00a6:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_00ab:  stloc.2
    IL_00ac:  ldarg.0
    IL_00ad:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_00b2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_00b8:  ldarg.0
    IL_00b9:  ldc.i4.m1
    IL_00ba:  dup
    IL_00bb:  stloc.0
    IL_00bc:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00c1:  ldarg.0
    IL_00c2:  ldloca.s   V_2
    IL_00c4:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_00c9:  stfld      ""bool C.<Main>d__0.<>s__3""
    IL_00ce:  ldarg.0
    IL_00cf:  ldfld      ""bool C.<Main>d__0.<>s__3""
    IL_00d4:  brtrue     IL_0039
    IL_00d9:  ldarg.0
    IL_00da:  ldnull
    IL_00db:  stfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    IL_00e0:  leave.s    IL_00fc
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_00e2:  stloc.s    V_4
    IL_00e4:  ldarg.0
    IL_00e5:  ldc.i4.s   -2
    IL_00e7:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00ec:  ldarg.0
    IL_00ed:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_00f2:  ldloc.s    V_4
    IL_00f4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00f9:  nop
    IL_00fa:  leave.s    IL_0110
  }
  // sequence point: }
  IL_00fc:  ldarg.0
  IL_00fd:  ldc.i4.s   -2
  IL_00ff:  stfld      ""int C.<Main>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0104:  ldarg.0
  IL_0105:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_010a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_010f:  nop
  IL_0110:  ret
}
", sequencePointDisplay: SequencePointDisplayMode.Enhanced);
        }

        [Fact]
        public void TestWithPattern_WithIAsyncDisposable()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Write($""Got({i}) "");
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator : System.IAsyncDisposable
    {
        int i = 0;
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async Task<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 4);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Yield();
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposal);

            var expectedOutput = "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4)";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x82 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x68 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      131 (0x83)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                object V_1,
                                int V_2, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_3)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_004a
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "int C.Enumerator.Current.get"
                    IL_0015:  stloc.2
                    IL_0016:  ldc.i4.6
                    IL_0017:  ldc.i4.1
                    IL_0018:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_001d:  stloc.3
                    IL_001e:  ldloca.s   V_3
                    IL_0020:  ldstr      "Got("
                    IL_0025:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_002a:  ldloca.s   V_3
                    IL_002c:  ldloc.2
                    IL_002d:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0032:  ldloca.s   V_3
                    IL_0034:  ldstr      ") "
                    IL_0039:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_003e:  ldloca.s   V_3
                    IL_0040:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0045:  call       "void System.Console.Write(string)"
                    IL_004a:  ldloc.0
                    IL_004b:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                    IL_0050:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0055:  brtrue.s   IL_000f
                    IL_0057:  leave.s    IL_005c
                  }
                  catch object
                  {
                    IL_0059:  stloc.1
                    IL_005a:  leave.s    IL_005c
                  }
                  IL_005c:  ldloc.0
                  IL_005d:  brfalse.s  IL_006a
                  IL_005f:  ldloc.0
                  IL_0060:  callvirt   "System.Threading.Tasks.ValueTask C.Enumerator.DisposeAsync()"
                  IL_0065:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_006a:  ldloc.1
                  IL_006b:  brfalse.s  IL_0082
                  IL_006d:  ldloc.1
                  IL_006e:  isinst     "System.Exception"
                  IL_0073:  dup
                  IL_0074:  brtrue.s   IL_0078
                  IL_0076:  ldloc.1
                  IL_0077:  throw
                  IL_0078:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_007d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0082:  ret
                }
                """);
        }

        [Fact]
        public void TestWithPattern_WithIAsyncDisposableUseSiteError()
        {
            string enumerator = @"
using System.Threading.Tasks;
public class C
{
    public Enumerator GetAsyncEnumerator() => throw null;
    public sealed class Enumerator : System.IAsyncDisposable
    {
        public int Current { get => throw null; }
        public Task<bool> MoveNextAsync() => throw null;
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
        }
    }
}";
            string source = @"
using System.Threading.Tasks;
class Client
{
    async Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
}";
            var lib = CreateCompilationWithTasksExtensions(enumerator + s_IAsyncEnumerable);
            lib.VerifyDiagnostics();

            var comp = CreateCompilationWithTasksExtensions(source, references: new[] { lib.EmitToImageReference() });
            comp.MakeTypeMissing(WellKnownType.System_IAsyncDisposable);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposal);
        }

        [Fact]
        public void TestWithMultipleInterface()
        {
            string source = @"
using System.Collections.Generic;
class C : IAsyncEnumerable<int>, IAsyncEnumerable<string>
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        throw null;
    }
    IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (7,33): error CS8413: Async foreach statement cannot operate on variables of type 'C' because it implements multiple instantiations of 'IAsyncEnumerable<T>'; try casting to a specific interface instantiation
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_MultipleIAsyncEnumOfT, "new C()").WithArguments("C", "System.Collections.Generic.IAsyncEnumerable<T>").WithLocation(7, 33)
                );
        }

        [Fact]
        public void TestWithMultipleImplementations()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class Base : IAsyncEnumerable<string>
{
    IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator(System.Threading.CancellationToken token)
        => throw null;
}
class C : Base, IAsyncEnumerable<int>
{
    async Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
        => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (13,33): error CS8413: Async foreach statement cannot operate on variables of type 'C' because it implements multiple instantiations of 'IAsyncEnumerable<T>'; try casting to a specific interface instantiation
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_MultipleIAsyncEnumOfT, "new C()").WithArguments("C", "System.Collections.Generic.IAsyncEnumerable<T>").WithLocation(13, 33)
                );
        }

        [Fact]
        public void TestWithInterface()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Console;
class C : IAsyncEnumerable<int>
{
    static async Task Main()
    {
        await foreach (var i in new C())
        {
            Write($""Got({i}) "");
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i = 0;
        int IAsyncEnumerator<int>.Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        async ValueTask<bool> IAsyncEnumerator<int>.MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 4);
        }
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            Write($""Disp"");
            await Task.Yield();
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var expectedOutput = "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4)";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var tree = comp.SyntaxTrees.First();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposal);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x8c }
                    [System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [System.IAsyncDisposable.DisposeAsync]: Return value missing on the stack. { Offset = 0x68 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      141 (0x8d)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                int V_3, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0054
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_001e:  stloc.3
                    IL_001f:  ldc.i4.6
                    IL_0020:  ldc.i4.1
                    IL_0021:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0026:  stloc.s    V_4
                    IL_0028:  ldloca.s   V_4
                    IL_002a:  ldstr      "Got("
                    IL_002f:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0034:  ldloca.s   V_4
                    IL_0036:  ldloc.3
                    IL_0037:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_003c:  ldloca.s   V_4
                    IL_003e:  ldstr      ") "
                    IL_0043:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0048:  ldloca.s   V_4
                    IL_004a:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_004f:  call       "void System.Console.Write(string)"
                    IL_0054:  ldloc.0
                    IL_0055:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_005a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_005f:  brtrue.s   IL_0018
                    IL_0061:  leave.s    IL_0066
                  }
                  catch object
                  {
                    IL_0063:  stloc.2
                    IL_0064:  leave.s    IL_0066
                  }
                  IL_0066:  ldloc.0
                  IL_0067:  brfalse.s  IL_0074
                  IL_0069:  ldloc.0
                  IL_006a:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_006f:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0074:  ldloc.2
                  IL_0075:  brfalse.s  IL_008c
                  IL_0077:  ldloc.2
                  IL_0078:  isinst     "System.Exception"
                  IL_007d:  dup
                  IL_007e:  brtrue.s   IL_0082
                  IL_0080:  ldloc.2
                  IL_0081:  throw
                  IL_0082:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0087:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_008c:  ret
                }
                """);
        }

        [Fact]
        public void TestWithInterface_OnStruct_ImplicitInterfaceImplementation()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
struct C : IAsyncEnumerable<int>
{
    static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C())
        {
            Write($""Got({i}) "");
        }
    }
    public IAsyncEnumerator<int> GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator();
    }
    class AsyncEnumerator : IAsyncEnumerator<int>
    {
        public int i;
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 4);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Yield();
            Write($""ose({i}) "");
        }
    }
}";
            // Note: the enumerator type should not be a struct, otherwise you will loop forever
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var expectedOutput = "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4)";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);

            // The thing to notice here is that the call to GetAsyncEnumerator is a constrained call (we're not boxing to `IAsyncEnumerable<int>`)
            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      496 (0x1f0)
  .maxstack  3
  .locals init (int V_0,
                C V_1,
                System.Threading.CancellationToken V_2,
                System.Runtime.CompilerServices.ValueTaskAwaiter<bool> V_3,
                System.Threading.Tasks.ValueTask<bool> V_4,
                C.<Main>d__0 V_5,
                object V_6,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_7,
                System.Threading.Tasks.ValueTask V_8,
                System.Exception V_9)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_004c
    IL_0014:  br         IL_015c
    IL_0019:  nop
    IL_001a:  nop
    IL_001b:  ldarg.0
    IL_001c:  ldloca.s   V_1
    IL_001e:  dup
    IL_001f:  initobj    ""C""
    IL_0025:  ldloca.s   V_2
    IL_0027:  initobj    ""System.Threading.CancellationToken""
    IL_002d:  ldloc.2
    IL_002e:  constrained. ""C""
    IL_0034:  callvirt   ""System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)""
    IL_0039:  stfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<Main>d__0.<>s__1""
    IL_003e:  ldarg.0
    IL_003f:  ldnull
    IL_0040:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_0045:  ldarg.0
    IL_0046:  ldc.i4.0
    IL_0047:  stfld      ""int C.<Main>d__0.<>s__3""
    IL_004c:  nop
    .try
    {
      IL_004d:  ldloc.0
      IL_004e:  brfalse.s  IL_0052
      IL_0050:  br.s       IL_0054
      IL_0052:  br.s       IL_00ca
      IL_0054:  br.s       IL_0084
      IL_0056:  ldarg.0
      IL_0057:  ldarg.0
      IL_0058:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<Main>d__0.<>s__1""
      IL_005d:  callvirt   ""int System.Collections.Generic.IAsyncEnumerator<int>.Current.get""
      IL_0062:  stfld      ""int C.<Main>d__0.<i>5__4""
      IL_0067:  nop
      IL_0068:  ldstr      ""Got({0}) ""
      IL_006d:  ldarg.0
      IL_006e:  ldfld      ""int C.<Main>d__0.<i>5__4""
      IL_0073:  box        ""int""
      IL_0078:  call       ""string string.Format(string, object)""
      IL_007d:  call       ""void System.Console.Write(string)""
      IL_0082:  nop
      IL_0083:  nop
      IL_0084:  ldarg.0
      IL_0085:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<Main>d__0.<>s__1""
      IL_008a:  callvirt   ""System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()""
      IL_008f:  stloc.s    V_4
      IL_0091:  ldloca.s   V_4
      IL_0093:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> System.Threading.Tasks.ValueTask<bool>.GetAwaiter()""
      IL_0098:  stloc.3
      IL_0099:  ldloca.s   V_3
      IL_009b:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter<bool>.IsCompleted.get""
      IL_00a0:  brtrue.s   IL_00e6
      IL_00a2:  ldarg.0
      IL_00a3:  ldc.i4.0
      IL_00a4:  dup
      IL_00a5:  stloc.0
      IL_00a6:  stfld      ""int C.<Main>d__0.<>1__state""
      IL_00ab:  ldarg.0
      IL_00ac:  ldloc.3
      IL_00ad:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00b2:  ldarg.0
      IL_00b3:  stloc.s    V_5
      IL_00b5:  ldarg.0
      IL_00b6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
      IL_00bb:  ldloca.s   V_3
      IL_00bd:  ldloca.s   V_5
      IL_00bf:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, C.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, ref C.<Main>d__0)""
      IL_00c4:  nop
      IL_00c5:  leave      IL_01ef
      IL_00ca:  ldarg.0
      IL_00cb:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00d0:  stloc.3
      IL_00d1:  ldarg.0
      IL_00d2:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00d7:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool>""
      IL_00dd:  ldarg.0
      IL_00de:  ldc.i4.m1
      IL_00df:  dup
      IL_00e0:  stloc.0
      IL_00e1:  stfld      ""int C.<Main>d__0.<>1__state""
      IL_00e6:  ldarg.0
      IL_00e7:  ldloca.s   V_3
      IL_00e9:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter<bool>.GetResult()""
      IL_00ee:  stfld      ""bool C.<Main>d__0.<>s__5""
      IL_00f3:  ldarg.0
      IL_00f4:  ldfld      ""bool C.<Main>d__0.<>s__5""
      IL_00f9:  brtrue     IL_0056
      IL_00fe:  leave.s    IL_010c
    }
    catch object
    {
      IL_0100:  stloc.s    V_6
      IL_0102:  ldarg.0
      IL_0103:  ldloc.s    V_6
      IL_0105:  stfld      ""object C.<Main>d__0.<>s__2""
      IL_010a:  leave.s    IL_010c
    }
    IL_010c:  ldarg.0
    IL_010d:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<Main>d__0.<>s__1""
    IL_0112:  brfalse.s  IL_0181
    IL_0114:  ldarg.0
    IL_0115:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<Main>d__0.<>s__1""
    IL_011a:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_011f:  stloc.s    V_8
    IL_0121:  ldloca.s   V_8
    IL_0123:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_0128:  stloc.s    V_7
    IL_012a:  ldloca.s   V_7
    IL_012c:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_0131:  brtrue.s   IL_0179
    IL_0133:  ldarg.0
    IL_0134:  ldc.i4.1
    IL_0135:  dup
    IL_0136:  stloc.0
    IL_0137:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_013c:  ldarg.0
    IL_013d:  ldloc.s    V_7
    IL_013f:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_0144:  ldarg.0
    IL_0145:  stloc.s    V_5
    IL_0147:  ldarg.0
    IL_0148:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_014d:  ldloca.s   V_7
    IL_014f:  ldloca.s   V_5
    IL_0151:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<Main>d__0)""
    IL_0156:  nop
    IL_0157:  leave      IL_01ef
    IL_015c:  ldarg.0
    IL_015d:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_0162:  stloc.s    V_7
    IL_0164:  ldarg.0
    IL_0165:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_016a:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_0170:  ldarg.0
    IL_0171:  ldc.i4.m1
    IL_0172:  dup
    IL_0173:  stloc.0
    IL_0174:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_0179:  ldloca.s   V_7
    IL_017b:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_0180:  nop
    IL_0181:  ldarg.0
    IL_0182:  ldfld      ""object C.<Main>d__0.<>s__2""
    IL_0187:  stloc.s    V_6
    IL_0189:  ldloc.s    V_6
    IL_018b:  brfalse.s  IL_01aa
    IL_018d:  ldloc.s    V_6
    IL_018f:  isinst     ""System.Exception""
    IL_0194:  stloc.s    V_9
    IL_0196:  ldloc.s    V_9
    IL_0198:  brtrue.s   IL_019d
    IL_019a:  ldloc.s    V_6
    IL_019c:  throw
    IL_019d:  ldloc.s    V_9
    IL_019f:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_01a4:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_01a9:  nop
    IL_01aa:  ldarg.0
    IL_01ab:  ldfld      ""int C.<Main>d__0.<>s__3""
    IL_01b0:  pop
    IL_01b1:  ldarg.0
    IL_01b2:  ldnull
    IL_01b3:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_01b8:  ldarg.0
    IL_01b9:  ldnull
    IL_01ba:  stfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<Main>d__0.<>s__1""
    IL_01bf:  leave.s    IL_01db
  }
  catch System.Exception
  {
    IL_01c1:  stloc.s    V_9
    IL_01c3:  ldarg.0
    IL_01c4:  ldc.i4.s   -2
    IL_01c6:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_01cb:  ldarg.0
    IL_01cc:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_01d1:  ldloc.s    V_9
    IL_01d3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_01d8:  nop
    IL_01d9:  leave.s    IL_01ef
  }
  IL_01db:  ldarg.0
  IL_01dc:  ldc.i4.s   -2
  IL_01de:  stfld      ""int C.<Main>d__0.<>1__state""
  IL_01e3:  ldarg.0
  IL_01e4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_01e9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01ee:  nop
  IL_01ef:  ret
}");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var runtimeAsyncVerifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x98 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x68 }
                    """
            });
            runtimeAsyncVerifier.VerifyIL("C.Main()", """
                {
                  // Code size      153 (0x99)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                C V_1,
                                System.Threading.CancellationToken V_2,
                                object V_3,
                                int V_4, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_5)
                  IL_0000:  ldloca.s   V_1
                  IL_0002:  dup
                  IL_0003:  initobj    "C"
                  IL_0009:  ldloca.s   V_2
                  IL_000b:  initobj    "System.Threading.CancellationToken"
                  IL_0011:  ldloc.2
                  IL_0012:  constrained. "C"
                  IL_0018:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_001d:  stloc.0
                  IL_001e:  ldnull
                  IL_001f:  stloc.3
                  .try
                  {
                    IL_0020:  br.s       IL_0060
                    IL_0022:  ldloc.0
                    IL_0023:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_0028:  stloc.s    V_4
                    IL_002a:  ldc.i4.6
                    IL_002b:  ldc.i4.1
                    IL_002c:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0031:  stloc.s    V_5
                    IL_0033:  ldloca.s   V_5
                    IL_0035:  ldstr      "Got("
                    IL_003a:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_003f:  ldloca.s   V_5
                    IL_0041:  ldloc.s    V_4
                    IL_0043:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0048:  ldloca.s   V_5
                    IL_004a:  ldstr      ") "
                    IL_004f:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0054:  ldloca.s   V_5
                    IL_0056:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_005b:  call       "void System.Console.Write(string)"
                    IL_0060:  ldloc.0
                    IL_0061:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_0066:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_006b:  brtrue.s   IL_0022
                    IL_006d:  leave.s    IL_0072
                  }
                  catch object
                  {
                    IL_006f:  stloc.3
                    IL_0070:  leave.s    IL_0072
                  }
                  IL_0072:  ldloc.0
                  IL_0073:  brfalse.s  IL_0080
                  IL_0075:  ldloc.0
                  IL_0076:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_007b:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0080:  ldloc.3
                  IL_0081:  brfalse.s  IL_0098
                  IL_0083:  ldloc.3
                  IL_0084:  isinst     "System.Exception"
                  IL_0089:  dup
                  IL_008a:  brtrue.s   IL_008e
                  IL_008c:  ldloc.3
                  IL_008d:  throw
                  IL_008e:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0093:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0098:  ret
                }
                """);
        }

        [Fact]
        public void TestWithInterface_WithEarlyCompletion1()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C())
        {
            Write($""Got({i}) "");
        }
        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator(2);
    }
    internal sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 4);
        }
        public ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            return new ValueTask(Task.CompletedTask); // return a completed task
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var expectedOutput = "NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4) Done";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x96 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      151 (0x97)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                int V_3, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0054
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_001e:  stloc.3
                    IL_001f:  ldc.i4.6
                    IL_0020:  ldc.i4.1
                    IL_0021:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0026:  stloc.s    V_4
                    IL_0028:  ldloca.s   V_4
                    IL_002a:  ldstr      "Got("
                    IL_002f:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0034:  ldloca.s   V_4
                    IL_0036:  ldloc.3
                    IL_0037:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_003c:  ldloca.s   V_4
                    IL_003e:  ldstr      ") "
                    IL_0043:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0048:  ldloca.s   V_4
                    IL_004a:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_004f:  call       "void System.Console.Write(string)"
                    IL_0054:  ldloc.0
                    IL_0055:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_005a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_005f:  brtrue.s   IL_0018
                    IL_0061:  leave.s    IL_0066
                  }
                  catch object
                  {
                    IL_0063:  stloc.2
                    IL_0064:  leave.s    IL_0066
                  }
                  IL_0066:  ldloc.0
                  IL_0067:  brfalse.s  IL_0074
                  IL_0069:  ldloc.0
                  IL_006a:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_006f:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0074:  ldloc.2
                  IL_0075:  brfalse.s  IL_008c
                  IL_0077:  ldloc.2
                  IL_0078:  isinst     "System.Exception"
                  IL_007d:  dup
                  IL_007e:  brtrue.s   IL_0082
                  IL_0080:  ldloc.2
                  IL_0081:  throw
                  IL_0082:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0087:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_008c:  ldstr      "Done"
                  IL_0091:  call       "void System.Console.Write(string)"
                  IL_0096:  ret
                }
                """);
        }

        [Fact]
        public void TestWithInterface_WithBreakAndContinue()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C())
        {
            if (i == 2 || i == 3) { Write($""Continue({i}) ""); continue; }
            if (i == 4) { Write(""Break ""); break; }
            Write($""Got({i}) "");
        }
        Write(""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i = 0;
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 10);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Yield();
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            string expectedOutput = "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Continue(2) NextAsync(2) Current(3) Continue(3) NextAsync(3) Current(4) Break Dispose(4) Done";
            CompileAndVerify(comp,
                expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xec }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5d, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x68 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      237 (0xed)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                int V_3, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4,
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_5)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br         IL_00a7
                    IL_001b:  ldloc.0
                    IL_001c:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_0021:  stloc.3
                    IL_0022:  ldloc.3
                    IL_0023:  ldc.i4.2
                    IL_0024:  beq.s      IL_002a
                    IL_0026:  ldloc.3
                    IL_0027:  ldc.i4.3
                    IL_0028:  bne.un.s   IL_0062
                    IL_002a:  ldc.i4.s   11
                    IL_002c:  ldc.i4.1
                    IL_002d:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0032:  stloc.s    V_4
                    IL_0034:  ldloca.s   V_4
                    IL_0036:  ldstr      "Continue("
                    IL_003b:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0040:  ldloca.s   V_4
                    IL_0042:  ldloc.3
                    IL_0043:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0048:  ldloca.s   V_4
                    IL_004a:  ldstr      ") "
                    IL_004f:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0054:  ldloca.s   V_4
                    IL_0056:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_005b:  call       "void System.Console.Write(string)"
                    IL_0060:  br.s       IL_00a7
                    IL_0062:  ldloc.3
                    IL_0063:  ldc.i4.4
                    IL_0064:  bne.un.s   IL_0072
                    IL_0066:  ldstr      "Break "
                    IL_006b:  call       "void System.Console.Write(string)"
                    IL_0070:  br.s       IL_00b7
                    IL_0072:  ldc.i4.6
                    IL_0073:  ldc.i4.1
                    IL_0074:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0079:  stloc.s    V_5
                    IL_007b:  ldloca.s   V_5
                    IL_007d:  ldstr      "Got("
                    IL_0082:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0087:  ldloca.s   V_5
                    IL_0089:  ldloc.3
                    IL_008a:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_008f:  ldloca.s   V_5
                    IL_0091:  ldstr      ") "
                    IL_0096:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_009b:  ldloca.s   V_5
                    IL_009d:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_00a2:  call       "void System.Console.Write(string)"
                    IL_00a7:  ldloc.0
                    IL_00a8:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_00ad:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_00b2:  brtrue     IL_001b
                    IL_00b7:  leave.s    IL_00bc
                  }
                  catch object
                  {
                    IL_00b9:  stloc.2
                    IL_00ba:  leave.s    IL_00bc
                  }
                  IL_00bc:  ldloc.0
                  IL_00bd:  brfalse.s  IL_00ca
                  IL_00bf:  ldloc.0
                  IL_00c0:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_00c5:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_00ca:  ldloc.2
                  IL_00cb:  brfalse.s  IL_00e2
                  IL_00cd:  ldloc.2
                  IL_00ce:  isinst     "System.Exception"
                  IL_00d3:  dup
                  IL_00d4:  brtrue.s   IL_00d8
                  IL_00d6:  ldloc.2
                  IL_00d7:  throw
                  IL_00d8:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_00dd:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_00e2:  ldstr      "Done"
                  IL_00e7:  call       "void System.Console.Write(string)"
                  IL_00ec:  ret
                }
                """);
        }

        [Fact]
        public void TestWithInterface_WithGoto()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C())
        {
            if (i == 2 || i == 3) { Write($""Continue({i}) ""); continue; }
            if (i == 4) { Write(""Goto ""); goto done; }
            Write($""Got({i}) "");
        }
        done:
        Write(""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i = 0;
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 10);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Yield();
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            string expectedOutput = "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Continue(2) NextAsync(2) Current(3) Continue(3) NextAsync(3) Current(4) Goto Dispose(4) Done";
            CompileAndVerify(comp,
                expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xfc }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5d, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x68 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      253 (0xfd)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                int V_3,
                                int V_4, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_5,
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_6)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  IL_0016:  ldc.i4.0
                  IL_0017:  stloc.3
                  .try
                  {
                    IL_0018:  br         IL_00af
                    IL_001d:  ldloc.0
                    IL_001e:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_0023:  stloc.s    V_4
                    IL_0025:  ldloc.s    V_4
                    IL_0027:  ldc.i4.2
                    IL_0028:  beq.s      IL_002f
                    IL_002a:  ldloc.s    V_4
                    IL_002c:  ldc.i4.3
                    IL_002d:  bne.un.s   IL_0068
                    IL_002f:  ldc.i4.s   11
                    IL_0031:  ldc.i4.1
                    IL_0032:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0037:  stloc.s    V_5
                    IL_0039:  ldloca.s   V_5
                    IL_003b:  ldstr      "Continue("
                    IL_0040:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0045:  ldloca.s   V_5
                    IL_0047:  ldloc.s    V_4
                    IL_0049:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_004e:  ldloca.s   V_5
                    IL_0050:  ldstr      ") "
                    IL_0055:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_005a:  ldloca.s   V_5
                    IL_005c:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0061:  call       "void System.Console.Write(string)"
                    IL_0066:  br.s       IL_00af
                    IL_0068:  ldloc.s    V_4
                    IL_006a:  ldc.i4.4
                    IL_006b:  bne.un.s   IL_0079
                    IL_006d:  ldstr      "Goto "
                    IL_0072:  call       "void System.Console.Write(string)"
                    IL_0077:  br.s       IL_00c1
                    IL_0079:  ldc.i4.6
                    IL_007a:  ldc.i4.1
                    IL_007b:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0080:  stloc.s    V_6
                    IL_0082:  ldloca.s   V_6
                    IL_0084:  ldstr      "Got("
                    IL_0089:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_008e:  ldloca.s   V_6
                    IL_0090:  ldloc.s    V_4
                    IL_0092:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0097:  ldloca.s   V_6
                    IL_0099:  ldstr      ") "
                    IL_009e:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_00a3:  ldloca.s   V_6
                    IL_00a5:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_00aa:  call       "void System.Console.Write(string)"
                    IL_00af:  ldloc.0
                    IL_00b0:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_00b5:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_00ba:  brtrue     IL_001d
                    IL_00bf:  leave.s    IL_00c8
                    IL_00c1:  ldc.i4.1
                    IL_00c2:  stloc.3
                    IL_00c3:  leave.s    IL_00c8
                  }
                  catch object
                  {
                    IL_00c5:  stloc.2
                    IL_00c6:  leave.s    IL_00c8
                  }
                  IL_00c8:  ldloc.0
                  IL_00c9:  brfalse.s  IL_00d6
                  IL_00cb:  ldloc.0
                  IL_00cc:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_00d1:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_00d6:  ldloc.2
                  IL_00d7:  brfalse.s  IL_00ee
                  IL_00d9:  ldloc.2
                  IL_00da:  isinst     "System.Exception"
                  IL_00df:  dup
                  IL_00e0:  brtrue.s   IL_00e4
                  IL_00e2:  ldloc.2
                  IL_00e3:  throw
                  IL_00e4:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_00e9:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_00ee:  ldloc.3
                  IL_00ef:  ldc.i4.1
                  IL_00f0:  pop
                  IL_00f1:  pop
                  IL_00f2:  ldstr      "Done"
                  IL_00f7:  call       "void System.Console.Write(string)"
                  IL_00fc:  ret
                }
                """);
        }

        [Fact]
        public void TestWithInterface_WithStruct()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    static async Task Main()
    {
        await foreach (var i in new C())
        {
            Write($""Got({i}) "");
        }
        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator(0);
    }
    internal struct AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                i++;
                return i;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            bool more = await Task.FromResult(i < 4);
            i = i + 100; // Note: side-effects of async methods in structs are lost
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            string expectedOutput = "NextAsync(0) Current(0) Got(1) NextAsync(1) Current(1) Got(2) NextAsync(2) Current(2) Got(3) NextAsync(3) Current(3) Got(4) NextAsync(4) Dispose(4) Done";
            CompileAndVerify(comp,
                expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x96 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x65, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x66 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      151 (0x97)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                int V_3, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0054
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_001e:  stloc.3
                    IL_001f:  ldc.i4.6
                    IL_0020:  ldc.i4.1
                    IL_0021:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0026:  stloc.s    V_4
                    IL_0028:  ldloca.s   V_4
                    IL_002a:  ldstr      "Got("
                    IL_002f:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0034:  ldloca.s   V_4
                    IL_0036:  ldloc.3
                    IL_0037:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_003c:  ldloca.s   V_4
                    IL_003e:  ldstr      ") "
                    IL_0043:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0048:  ldloca.s   V_4
                    IL_004a:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_004f:  call       "void System.Console.Write(string)"
                    IL_0054:  ldloc.0
                    IL_0055:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_005a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_005f:  brtrue.s   IL_0018
                    IL_0061:  leave.s    IL_0066
                  }
                  catch object
                  {
                    IL_0063:  stloc.2
                    IL_0064:  leave.s    IL_0066
                  }
                  IL_0066:  ldloc.0
                  IL_0067:  brfalse.s  IL_0074
                  IL_0069:  ldloc.0
                  IL_006a:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_006f:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0074:  ldloc.2
                  IL_0075:  brfalse.s  IL_008c
                  IL_0077:  ldloc.2
                  IL_0078:  isinst     "System.Exception"
                  IL_007d:  dup
                  IL_007e:  brtrue.s   IL_0082
                  IL_0080:  ldloc.2
                  IL_0081:  throw
                  IL_0082:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0087:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_008c:  ldstr      "Done"
                  IL_0091:  call       "void System.Console.Write(string)"
                  IL_0096:  ret
                }
                """);
        }

        [Fact, WorkItem(27651, "https://github.com/dotnet/roslyn/issues/27651")]
        public void TestControlFlowAnalysis()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        await foreach (var item in collection) { }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var loop = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var ctrlFlowAnalysis = model.AnalyzeControlFlow(loop);
            Assert.Equal(0, ctrlFlowAnalysis.ExitPoints.Count());
        }

        [Fact]
        public void TestWithNullLiteralCollection()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    async Task M()
    {
        await foreach (var i in null)
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (8,33): error CS0186: Use of null is not valid in this context
                //         await foreach (var i in null)
                Diagnostic(ErrorCode.ERR_NullNotValid, "null").WithLocation(8, 33)
                );
        }

        [Fact]
        public void TestWithNullCollection()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    public static async Task Main()
    {
        C c = null;
        try
        {
            await foreach (var i in c)
            {
            }
        }
        catch (System.NullReferenceException)
        {
            System.Console.Write(""Success"");
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator(0);
    }
    internal struct AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int Current { get => throw new System.Exception(); }
        public ValueTask<bool> MoveNextAsync() => throw new System.Exception();
        public ValueTask DisposeAsync() => throw new System.Exception();
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Success");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("Success"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x64 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      101 (0x65)
                  .maxstack  2
                  .locals init (C V_0, //c
                                System.Collections.Generic.IAsyncEnumerator<int> V_1,
                                System.Threading.CancellationToken V_2,
                                object V_3)
                  IL_0000:  ldnull
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldloc.0
                    IL_0003:  ldloca.s   V_2
                    IL_0005:  initobj    "System.Threading.CancellationToken"
                    IL_000b:  ldloc.2
                    IL_000c:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                    IL_0011:  stloc.1
                    IL_0012:  ldnull
                    IL_0013:  stloc.3
                    .try
                    {
                      IL_0014:  br.s       IL_001d
                      IL_0016:  ldloc.1
                      IL_0017:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                      IL_001c:  pop
                      IL_001d:  ldloc.1
                      IL_001e:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                      IL_0023:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                      IL_0028:  brtrue.s   IL_0016
                      IL_002a:  leave.s    IL_002f
                    }
                    catch object
                    {
                      IL_002c:  stloc.3
                      IL_002d:  leave.s    IL_002f
                    }
                    IL_002f:  ldloc.1
                    IL_0030:  brfalse.s  IL_003d
                    IL_0032:  ldloc.1
                    IL_0033:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                    IL_0038:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_003d:  ldloc.3
                    IL_003e:  brfalse.s  IL_0055
                    IL_0040:  ldloc.3
                    IL_0041:  isinst     "System.Exception"
                    IL_0046:  dup
                    IL_0047:  brtrue.s   IL_004b
                    IL_0049:  ldloc.3
                    IL_004a:  throw
                    IL_004b:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_0050:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0055:  leave.s    IL_0064
                  }
                  catch System.NullReferenceException
                  {
                    IL_0057:  pop
                    IL_0058:  ldstr      "Success"
                    IL_005d:  call       "void System.Console.Write(string)"
                    IL_0062:  leave.s    IL_0064
                  }
                  IL_0064:  ret
                }
                """);
        }

        [Fact]
        public void TestInCatch()
        {
            string source = @"
using System.Collections.Generic;
using static System.Console;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    public static async Task Main()
    {
        try
        {
            Write($""Try "");
            throw null;
        }
        catch (System.NullReferenceException)
        {
            await foreach (var i in new C())
            {
                Write($""Got({i}) "");
            }
        }
        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator(0);
    }
    internal class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 4);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Yield();
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var expectedOutput = "Try NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4) Done";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xb2 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x68 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      179 (0xb3)
                  .maxstack  2
                  .locals init (int V_0,
                                System.Collections.Generic.IAsyncEnumerator<int> V_1,
                                System.Threading.CancellationToken V_2,
                                object V_3,
                                int V_4, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_5)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldstr      "Try "
                    IL_0007:  call       "void System.Console.Write(string)"
                    IL_000c:  ldnull
                    IL_000d:  throw
                  }
                  catch System.NullReferenceException
                  {
                    IL_000e:  pop
                    IL_000f:  ldc.i4.1
                    IL_0010:  stloc.0
                    IL_0011:  leave.s    IL_0013
                  }
                  IL_0013:  ldloc.0
                  IL_0014:  ldc.i4.1
                  IL_0015:  bne.un     IL_00a8
                  IL_001a:  newobj     "C..ctor()"
                  IL_001f:  ldloca.s   V_2
                  IL_0021:  initobj    "System.Threading.CancellationToken"
                  IL_0027:  ldloc.2
                  IL_0028:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_002d:  stloc.1
                  IL_002e:  ldnull
                  IL_002f:  stloc.3
                  .try
                  {
                    IL_0030:  br.s       IL_0070
                    IL_0032:  ldloc.1
                    IL_0033:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_0038:  stloc.s    V_4
                    IL_003a:  ldc.i4.6
                    IL_003b:  ldc.i4.1
                    IL_003c:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0041:  stloc.s    V_5
                    IL_0043:  ldloca.s   V_5
                    IL_0045:  ldstr      "Got("
                    IL_004a:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_004f:  ldloca.s   V_5
                    IL_0051:  ldloc.s    V_4
                    IL_0053:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0058:  ldloca.s   V_5
                    IL_005a:  ldstr      ") "
                    IL_005f:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0064:  ldloca.s   V_5
                    IL_0066:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_006b:  call       "void System.Console.Write(string)"
                    IL_0070:  ldloc.1
                    IL_0071:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_0076:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_007b:  brtrue.s   IL_0032
                    IL_007d:  leave.s    IL_0082
                  }
                  catch object
                  {
                    IL_007f:  stloc.3
                    IL_0080:  leave.s    IL_0082
                  }
                  IL_0082:  ldloc.1
                  IL_0083:  brfalse.s  IL_0090
                  IL_0085:  ldloc.1
                  IL_0086:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_008b:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0090:  ldloc.3
                  IL_0091:  brfalse.s  IL_00a8
                  IL_0093:  ldloc.3
                  IL_0094:  isinst     "System.Exception"
                  IL_0099:  dup
                  IL_009a:  brtrue.s   IL_009e
                  IL_009c:  ldloc.3
                  IL_009d:  throw
                  IL_009e:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_00a3:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_00a8:  ldstr      "Done"
                  IL_00ad:  call       "void System.Console.Write(string)"
                  IL_00b2:  ret
                }
                """);
        }

        /// Covered in greater details by <see cref="CodeGenAsyncIteratorTests.TryFinally_AwaitForeachInFinally"/>
        [Fact]
        public void TestInFinally()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    public static async Task Main()
    {
        try
        {
        }
        finally
        {
            await foreach (var i in new C())
            {
            }
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestWithConversionToElement()
        {
            string source = @"
using System.Collections.Generic;
using static System.Console;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    public static async Task Main()
    {
        await foreach (Element i in new C())
        {
            Write($""Got({i}) "");
        }
        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator(0);
    }
    internal class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 3);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Yield();
            Write($""ose({i}) "");
        }
    }
}
class Element
{
    int i;
    public static implicit operator Element(int value) { Write($""Convert({value}) ""); return new Element(value); }
    private Element(int value) { i = value; }
    public override string ToString() => i.ToString();
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var expectedOutput = "NextAsync(0) Current(1) Convert(1) Got(1) NextAsync(1) Current(2) Convert(2) Got(2) NextAsync(2) Dispose(3) Done";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ExplicitUserDefined, info.ElementConversion.Kind);
            Assert.Equal("Element Element.op_Implicit(System.Int32 value)", info.ElementConversion.MethodSymbol.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposal);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x9b }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x68 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      156 (0x9c)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                Element V_3, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0059
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_001e:  call       "Element Element.op_Implicit(int)"
                    IL_0023:  stloc.3
                    IL_0024:  ldc.i4.6
                    IL_0025:  ldc.i4.1
                    IL_0026:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_002b:  stloc.s    V_4
                    IL_002d:  ldloca.s   V_4
                    IL_002f:  ldstr      "Got("
                    IL_0034:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0039:  ldloca.s   V_4
                    IL_003b:  ldloc.3
                    IL_003c:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<Element>(Element)"
                    IL_0041:  ldloca.s   V_4
                    IL_0043:  ldstr      ") "
                    IL_0048:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_004d:  ldloca.s   V_4
                    IL_004f:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0054:  call       "void System.Console.Write(string)"
                    IL_0059:  ldloc.0
                    IL_005a:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_005f:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_0064:  brtrue.s   IL_0018
                    IL_0066:  leave.s    IL_006b
                  }
                  catch object
                  {
                    IL_0068:  stloc.2
                    IL_0069:  leave.s    IL_006b
                  }
                  IL_006b:  ldloc.0
                  IL_006c:  brfalse.s  IL_0079
                  IL_006e:  ldloc.0
                  IL_006f:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0074:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0079:  ldloc.2
                  IL_007a:  brfalse.s  IL_0091
                  IL_007c:  ldloc.2
                  IL_007d:  isinst     "System.Exception"
                  IL_0082:  dup
                  IL_0083:  brtrue.s   IL_0087
                  IL_0085:  ldloc.2
                  IL_0086:  throw
                  IL_0087:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_008c:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0091:  ldstr      "Done"
                  IL_0096:  call       "void System.Console.Write(string)"
                  IL_009b:  ret
                }
                """);
        }

        [Fact]
        public void TestWithNullableCollection()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
struct C : IAsyncEnumerable<int>
{
    static async System.Threading.Tasks.Task Main()
    {
        C? c = new C(); // non-null value
        await foreach (var i in c)
        {
            Write($""Got({i}) "");
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i = 0;
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 3);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Yield();
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var expectedOutput = "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Dispose(3)";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposal);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xae }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x68 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      175 (0xaf)
                  .maxstack  2
                  .locals init (C? V_0, //c
                                C V_1,
                                System.Collections.Generic.IAsyncEnumerator<int> V_2,
                                System.Threading.CancellationToken V_3,
                                object V_4,
                                int V_5, //i
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_6)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  ldloca.s   V_1
                  IL_0004:  initobj    "C"
                  IL_000a:  ldloc.1
                  IL_000b:  call       "C?..ctor(C)"
                  IL_0010:  ldloca.s   V_0
                  IL_0012:  call       "readonly C C?.Value.get"
                  IL_0017:  stloc.1
                  IL_0018:  ldloca.s   V_1
                  IL_001a:  ldloca.s   V_3
                  IL_001c:  initobj    "System.Threading.CancellationToken"
                  IL_0022:  ldloc.3
                  IL_0023:  constrained. "C"
                  IL_0029:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_002e:  stloc.2
                  IL_002f:  ldnull
                  IL_0030:  stloc.s    V_4
                  .try
                  {
                    IL_0032:  br.s       IL_0072
                    IL_0034:  ldloc.2
                    IL_0035:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_003a:  stloc.s    V_5
                    IL_003c:  ldc.i4.6
                    IL_003d:  ldc.i4.1
                    IL_003e:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0043:  stloc.s    V_6
                    IL_0045:  ldloca.s   V_6
                    IL_0047:  ldstr      "Got("
                    IL_004c:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0051:  ldloca.s   V_6
                    IL_0053:  ldloc.s    V_5
                    IL_0055:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_005a:  ldloca.s   V_6
                    IL_005c:  ldstr      ") "
                    IL_0061:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0066:  ldloca.s   V_6
                    IL_0068:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_006d:  call       "void System.Console.Write(string)"
                    IL_0072:  ldloc.2
                    IL_0073:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_0078:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_007d:  brtrue.s   IL_0034
                    IL_007f:  leave.s    IL_0085
                  }
                  catch object
                  {
                    IL_0081:  stloc.s    V_4
                    IL_0083:  leave.s    IL_0085
                  }
                  IL_0085:  ldloc.2
                  IL_0086:  brfalse.s  IL_0093
                  IL_0088:  ldloc.2
                  IL_0089:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_008e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0093:  ldloc.s    V_4
                  IL_0095:  brfalse.s  IL_00ae
                  IL_0097:  ldloc.s    V_4
                  IL_0099:  isinst     "System.Exception"
                  IL_009e:  dup
                  IL_009f:  brtrue.s   IL_00a4
                  IL_00a1:  ldloc.s    V_4
                  IL_00a3:  throw
                  IL_00a4:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_00a9:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_00ae:  ret
                }
                """);
        }

        [Fact]
        public void TestWithNullableCollection2()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
struct C : IAsyncEnumerable<int>
{
    static async Task Main()
    {
        C? c = null; // null value
        try
        {
            await foreach (var i in c)
            {
                Write($""UNREACHABLE"");
            }
        }
        catch (System.InvalidOperationException)
        {
            Write($""Success"");
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        throw new System.Exception();
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Success");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("Success"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x88 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      137 (0x89)
                  .maxstack  2
                  .locals init (C? V_0, //c
                                System.Collections.Generic.IAsyncEnumerator<int> V_1,
                                C V_2,
                                System.Threading.CancellationToken V_3,
                                object V_4)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "C?"
                  .try
                  {
                    IL_0008:  ldloca.s   V_0
                    IL_000a:  call       "readonly C C?.Value.get"
                    IL_000f:  stloc.2
                    IL_0010:  ldloca.s   V_2
                    IL_0012:  ldloca.s   V_3
                    IL_0014:  initobj    "System.Threading.CancellationToken"
                    IL_001a:  ldloc.3
                    IL_001b:  constrained. "C"
                    IL_0021:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                    IL_0026:  stloc.1
                    IL_0027:  ldnull
                    IL_0028:  stloc.s    V_4
                    .try
                    {
                      IL_002a:  br.s       IL_003d
                      IL_002c:  ldloc.1
                      IL_002d:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                      IL_0032:  pop
                      IL_0033:  ldstr      "UNREACHABLE"
                      IL_0038:  call       "void System.Console.Write(string)"
                      IL_003d:  ldloc.1
                      IL_003e:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                      IL_0043:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                      IL_0048:  brtrue.s   IL_002c
                      IL_004a:  leave.s    IL_0050
                    }
                    catch object
                    {
                      IL_004c:  stloc.s    V_4
                      IL_004e:  leave.s    IL_0050
                    }
                    IL_0050:  ldloc.1
                    IL_0051:  brfalse.s  IL_005e
                    IL_0053:  ldloc.1
                    IL_0054:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                    IL_0059:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_005e:  ldloc.s    V_4
                    IL_0060:  brfalse.s  IL_0079
                    IL_0062:  ldloc.s    V_4
                    IL_0064:  isinst     "System.Exception"
                    IL_0069:  dup
                    IL_006a:  brtrue.s   IL_006f
                    IL_006c:  ldloc.s    V_4
                    IL_006e:  throw
                    IL_006f:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_0074:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0079:  leave.s    IL_0088
                  }
                  catch System.InvalidOperationException
                  {
                    IL_007b:  pop
                    IL_007c:  ldstr      "Success"
                    IL_0081:  call       "void System.Console.Write(string)"
                    IL_0086:  leave.s    IL_0088
                  }
                  IL_0088:  ret
                }
                """);
        }

        [Fact]
        public void TestWithInterfaceAndDeconstruction()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    static async System.Threading.Tasks.Task Main()
    {
        await foreach (var (i, j) in new C())
        {
            Write($""Got({i},{j}) "");
        }
        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i = 0;
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 3);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Yield();
        }
    }
}
public static class Extensions
{
    public static void Deconstruct(this int i, out string x1, out int x2) { Write($""Deconstruct({i}) ""); x1 = i.ToString(); x2 = -i; }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var expectedOutput = "NextAsync(0) Current(1) Deconstruct(1) Got(1,-1) NextAsync(1) Current(2) Deconstruct(2) Got(2,-2) NextAsync(2) Dispose(3) Done";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposal);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xba }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x5f }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      187 (0xbb)
                  .maxstack  3
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                string V_3, //i
                                int V_4, //j
                                string V_5,
                                int V_6,
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_7)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0078
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_001e:  ldloca.s   V_5
                    IL_0020:  ldloca.s   V_6
                    IL_0022:  call       "void Extensions.Deconstruct(int, out string, out int)"
                    IL_0027:  ldloc.s    V_5
                    IL_0029:  stloc.3
                    IL_002a:  ldloc.s    V_6
                    IL_002c:  stloc.s    V_4
                    IL_002e:  ldc.i4.7
                    IL_002f:  ldc.i4.2
                    IL_0030:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0035:  stloc.s    V_7
                    IL_0037:  ldloca.s   V_7
                    IL_0039:  ldstr      "Got("
                    IL_003e:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0043:  ldloca.s   V_7
                    IL_0045:  ldloc.3
                    IL_0046:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(string)"
                    IL_004b:  ldloca.s   V_7
                    IL_004d:  ldstr      ","
                    IL_0052:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0057:  ldloca.s   V_7
                    IL_0059:  ldloc.s    V_4
                    IL_005b:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0060:  ldloca.s   V_7
                    IL_0062:  ldstr      ") "
                    IL_0067:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_006c:  ldloca.s   V_7
                    IL_006e:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0073:  call       "void System.Console.Write(string)"
                    IL_0078:  ldloc.0
                    IL_0079:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_007e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_0083:  brtrue.s   IL_0018
                    IL_0085:  leave.s    IL_008a
                  }
                  catch object
                  {
                    IL_0087:  stloc.2
                    IL_0088:  leave.s    IL_008a
                  }
                  IL_008a:  ldloc.0
                  IL_008b:  brfalse.s  IL_0098
                  IL_008d:  ldloc.0
                  IL_008e:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0093:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0098:  ldloc.2
                  IL_0099:  brfalse.s  IL_00b0
                  IL_009b:  ldloc.2
                  IL_009c:  isinst     "System.Exception"
                  IL_00a1:  dup
                  IL_00a2:  brtrue.s   IL_00a6
                  IL_00a4:  ldloc.2
                  IL_00a5:  throw
                  IL_00a6:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_00ab:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_00b0:  ldstr      "Done"
                  IL_00b5:  call       "void System.Console.Write(string)"
                  IL_00ba:  ret
                }
                """);
        }

        [Fact]
        public void TestWithDeconstructionInNonAsyncMethod()
        {
            string source = @"
using System.Collections.Generic;
class C : IAsyncEnumerable<int>
{
    static void Main()
    {
        await foreach (var (i, j) in new C())
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
        => throw null;
}
public static class Extensions
{
    public static void Deconstruct(this int i, out int x1, out int x2) { x1 = i; x2 = -i; }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (7,9): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         await foreach (var (i, j) in new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(7, 9)
                );
        }

        [Fact]
        public void TestWithPatternAndDeconstructionOfTuple()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<(string, int)>
{
    public static async System.Threading.Tasks.Task Main()
    {
        await foreach (var (i, j) in new C())
        {
            Write($""Got({i},{j}) "");
        }
        Write($""Done"");
    }
    IAsyncEnumerator<(string, int)> IAsyncEnumerable<(string, int)>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<(string, int)>
    {
        int i = 0;
        public (string, int) Current
        {
            get
            {
                Write($""Current({i}) "");
                return (i.ToString(), -i);
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 3);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var expectedOutput = "NextAsync(0) Current(1) Got(1,-1) NextAsync(1) Current(2) Got(2,-2) NextAsync(2) Dispose(3) Done";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)> System.Collections.Generic.IAsyncEnumerable<(System.String, System.Int32)>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)>.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("(System.String, System.Int32) System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)>.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("(System.String, System.Int32)", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xb8 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x5f }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      185 (0xb9)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<System.ValueTuple<string, int>> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                string V_3, //i
                                int V_4, //j
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_5)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<System.ValueTuple<string, int>> System.Collections.Generic.IAsyncEnumerable<System.ValueTuple<string, int>>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0076
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "System.ValueTuple<string, int> System.Collections.Generic.IAsyncEnumerator<System.ValueTuple<string, int>>.Current.get"
                    IL_001e:  dup
                    IL_001f:  ldfld      "string System.ValueTuple<string, int>.Item1"
                    IL_0024:  stloc.3
                    IL_0025:  ldfld      "int System.ValueTuple<string, int>.Item2"
                    IL_002a:  stloc.s    V_4
                    IL_002c:  ldc.i4.7
                    IL_002d:  ldc.i4.2
                    IL_002e:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0033:  stloc.s    V_5
                    IL_0035:  ldloca.s   V_5
                    IL_0037:  ldstr      "Got("
                    IL_003c:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0041:  ldloca.s   V_5
                    IL_0043:  ldloc.3
                    IL_0044:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(string)"
                    IL_0049:  ldloca.s   V_5
                    IL_004b:  ldstr      ","
                    IL_0050:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0055:  ldloca.s   V_5
                    IL_0057:  ldloc.s    V_4
                    IL_0059:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_005e:  ldloca.s   V_5
                    IL_0060:  ldstr      ") "
                    IL_0065:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_006a:  ldloca.s   V_5
                    IL_006c:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0071:  call       "void System.Console.Write(string)"
                    IL_0076:  ldloc.0
                    IL_0077:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<System.ValueTuple<string, int>>.MoveNextAsync()"
                    IL_007c:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_0081:  brtrue.s   IL_0018
                    IL_0083:  leave.s    IL_0088
                  }
                  catch object
                  {
                    IL_0085:  stloc.2
                    IL_0086:  leave.s    IL_0088
                  }
                  IL_0088:  ldloc.0
                  IL_0089:  brfalse.s  IL_0096
                  IL_008b:  ldloc.0
                  IL_008c:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0091:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0096:  ldloc.2
                  IL_0097:  brfalse.s  IL_00ae
                  IL_0099:  ldloc.2
                  IL_009a:  isinst     "System.Exception"
                  IL_009f:  dup
                  IL_00a0:  brtrue.s   IL_00a4
                  IL_00a2:  ldloc.2
                  IL_00a3:  throw
                  IL_00a4:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_00a9:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_00ae:  ldstr      "Done"
                  IL_00b3:  call       "void System.Console.Write(string)"
                  IL_00b8:  ret
                }
                """);
        }

        [Fact]
        public void TestWithInterfaceAndDeconstruction_ManualIteration()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    static async Task Main()
    {
        var e = ((IAsyncEnumerable<int>)new C()).GetAsyncEnumerator();
        try
        {
            while (await e.MoveNextAsync())
            {
                (int i, int j) = e.Current;
                Write($""Got({i},{j}) "");
            }
        }
        finally { await e.DisposeAsync(); }

        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator(0);
    }
    internal class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int Current
        {
            get
            {
                Write($""Current({i}) "");
                return i;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 3);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Yield();
            Write($""ose({i}) "");
        }
    }
}
public static class Extensions
{
    public static void Deconstruct(this int i, out int x1, out int x2) { x1 = i; x2 = -i; }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            string expectedOutput = "NextAsync(0) Current(1) Got(1,-1) NextAsync(1) Current(2) Got(2,-2) NextAsync(2) Dispose(3) Done";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xb7 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x68 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      184 (0xb8)
                  .maxstack  3
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0, //e
                                System.Threading.CancellationToken V_1,
                                object V_2,
                                int V_3, //i
                                int V_4, //j
                                int V_5,
                                int V_6,
                                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_7)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0078
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_001e:  ldloca.s   V_5
                    IL_0020:  ldloca.s   V_6
                    IL_0022:  call       "void Extensions.Deconstruct(int, out int, out int)"
                    IL_0027:  ldloc.s    V_5
                    IL_0029:  stloc.3
                    IL_002a:  ldloc.s    V_6
                    IL_002c:  stloc.s    V_4
                    IL_002e:  ldc.i4.7
                    IL_002f:  ldc.i4.2
                    IL_0030:  newobj     "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)"
                    IL_0035:  stloc.s    V_7
                    IL_0037:  ldloca.s   V_7
                    IL_0039:  ldstr      "Got("
                    IL_003e:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0043:  ldloca.s   V_7
                    IL_0045:  ldloc.3
                    IL_0046:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_004b:  ldloca.s   V_7
                    IL_004d:  ldstr      ","
                    IL_0052:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_0057:  ldloca.s   V_7
                    IL_0059:  ldloc.s    V_4
                    IL_005b:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)"
                    IL_0060:  ldloca.s   V_7
                    IL_0062:  ldstr      ") "
                    IL_0067:  call       "void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)"
                    IL_006c:  ldloca.s   V_7
                    IL_006e:  call       "string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()"
                    IL_0073:  call       "void System.Console.Write(string)"
                    IL_0078:  ldloc.0
                    IL_0079:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_007e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_0083:  brtrue.s   IL_0018
                    IL_0085:  leave.s    IL_008a
                  }
                  catch object
                  {
                    IL_0087:  stloc.2
                    IL_0088:  leave.s    IL_008a
                  }
                  IL_008a:  ldloc.0
                  IL_008b:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0090:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0095:  ldloc.2
                  IL_0096:  brfalse.s  IL_00ad
                  IL_0098:  ldloc.2
                  IL_0099:  isinst     "System.Exception"
                  IL_009e:  dup
                  IL_009f:  brtrue.s   IL_00a3
                  IL_00a1:  ldloc.2
                  IL_00a2:  throw
                  IL_00a3:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_00a8:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_00ad:  ldstr      "Done"
                  IL_00b2:  call       "void System.Console.Write(string)"
                  IL_00b7:  ret
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30257")]
        public void TestWithPatternAndObsolete_WithDisposableInterface()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    [System.Obsolete]
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }
    [System.Obsolete]
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        [System.Obsolete]
        public int Current { get => throw null; }
        [System.Obsolete]
        public Task<bool> MoveNextAsync() => throw null;
        [System.Obsolete]
        public ValueTask DisposeAsync() => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,15): warning CS0612: 'C.AsyncEnumerator.DisposeAsync()' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.AsyncEnumerator.DisposeAsync()").WithLocation(7, 15),
                // (7,15): warning CS0612: 'C.GetAsyncEnumerator(CancellationToken)' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(7, 15),
                // (7,15): warning CS0612: 'C.AsyncEnumerator.MoveNextAsync()' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.AsyncEnumerator.MoveNextAsync()").WithLocation(7, 15),
                // (7,15): warning CS0612: 'C.AsyncEnumerator.Current' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.AsyncEnumerator.Current").WithLocation(7, 15)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30257")]
        public void TestWithPatternAndObsolete_WithoutDisposableInterface()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    [System.Obsolete]
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }
    [System.Obsolete]
    public sealed class AsyncEnumerator
    {
        [System.Obsolete]
        public int Current { get => throw null; }
        [System.Obsolete]
        public Task<bool> MoveNextAsync() => throw null;
        [System.Obsolete]
        public ValueTask DisposeAsync() => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,15): warning CS0612: 'C.AsyncEnumerator.DisposeAsync()' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.AsyncEnumerator.DisposeAsync()").WithLocation(7, 15),
                // (7,15): warning CS0612: 'C.GetAsyncEnumerator(CancellationToken)' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(7, 15),
                // (7,15): warning CS0612: 'C.AsyncEnumerator.MoveNextAsync()' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.AsyncEnumerator.MoveNextAsync()").WithLocation(7, 15),
                // (7,15): warning CS0612: 'C.AsyncEnumerator.Current' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.AsyncEnumerator.Current").WithLocation(7, 15)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30257")]
        public void TestWithPatternAndObsolete_WithExplicitInterfaceImplementation()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current { get => throw null; }
        public Task<bool> MoveNextAsync() => throw null;
        [System.Obsolete]
        ValueTask System.IAsyncDisposable.DisposeAsync() => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void TestWithUnassignedCollection()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    async System.Threading.Tasks.Task M()
    {
        C c;
        await foreach (var i in c)
        {
        }
    }
    public IAsyncEnumerator<int> GetAsyncEnumerator(System.Threading.CancellationToken token = default) => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (8,33): error CS0165: Use of unassigned local variable 'c'
                //         await foreach (var i in c)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(8, 33)
                );
        }

        [Fact]
        public void TestInRegularForeach()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    void M()
    {
        foreach (var i in new C())
        {
        }
    }
    public IAsyncEnumerator<int> GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (7,27): error CS8414: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'. Did you mean 'await foreach'?
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_ForEachMissingMemberWrongAsync, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27)
                );
        }

        [Fact]
        public void TestWithGenericCollection()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
class Collection<T> : IAsyncEnumerable<T>
{
    IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<T>
    {
        int i = 0;
        public T Current
        {
            get
            {
                Write($""Current({i}) "");
                return default;
            }
        }
        public async ValueTask<bool> MoveNextAsync()
        {
            Write($""NextAsync({i}) "");
            i++;
            return await Task.FromResult(i < 4);
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Yield();
        }
    }
}
class C
{
    static async System.Threading.Tasks.Task Main()
    {
        await foreach (var i in new Collection<int>())
        {
            Write($""Got "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var expectedOutput = "NextAsync(0) Current(1) Got NextAsync(1) Current(2) Got NextAsync(2) Current(3) Got NextAsync(3) Dispose(4)";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposal);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x61 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x5f }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       98 (0x62)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2)
                  IL_0000:  newobj     "Collection<int>..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0029
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_001e:  pop
                    IL_001f:  ldstr      "Got "
                    IL_0024:  call       "void System.Console.Write(string)"
                    IL_0029:  ldloc.0
                    IL_002a:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_002f:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_0034:  brtrue.s   IL_0018
                    IL_0036:  leave.s    IL_003b
                  }
                  catch object
                  {
                    IL_0038:  stloc.2
                    IL_0039:  leave.s    IL_003b
                  }
                  IL_003b:  ldloc.0
                  IL_003c:  brfalse.s  IL_0049
                  IL_003e:  ldloc.0
                  IL_003f:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0044:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0049:  ldloc.2
                  IL_004a:  brfalse.s  IL_0061
                  IL_004c:  ldloc.2
                  IL_004d:  isinst     "System.Exception"
                  IL_0052:  dup
                  IL_0053:  brtrue.s   IL_0057
                  IL_0055:  ldloc.2
                  IL_0056:  throw
                  IL_0057:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_005c:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0061:  ret
                }
                """);
        }

        [Fact]
        public void TestWithInterfaceImplementingPattern()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;

public interface ICollection<T>
{
    IMyAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token = default);
}
public interface IMyAsyncEnumerator<T>
{
    T Current { get; }
    Task<bool> MoveNextAsync();
}

public class Collection<T> : ICollection<T>
{
    public IMyAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        return new MyAsyncEnumerator<T>();
    }
}
public sealed class MyAsyncEnumerator<T> : IMyAsyncEnumerator<T>
{
    int i = 0;
    public T Current
    {
        get
        {
            Write($""Current({i}) "");
            return default;
        }
    }
    public async Task<bool> MoveNextAsync()
    {
        Write($""NextAsync({i}) "");
        i++;
        return await Task.FromResult(i < 4);
    }
}

class C
{
    static async System.Threading.Tasks.Task Main()
    {
        ICollection<int> c = new Collection<int>();
        await foreach (var i in c)
        {
            Write($""Got "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            string expectedOutput = "NextAsync(0) Current(1) Got NextAsync(1) Current(2) Got NextAsync(2) Current(3) Got NextAsync(3)";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("IMyAsyncEnumerator<System.Int32> ICollection<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> IMyAsyncEnumerator<System.Int32>.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 IMyAsyncEnumerator<System.Int32>.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Null(info.DisposeMethod);
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.False(internalInfo.NeedsDisposal);

            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      283 (0x11b)
  .maxstack  3
  .locals init (int V_0,
                System.Threading.CancellationToken V_1,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_2,
                C.<Main>d__0 V_3,
                System.Exception V_4)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0017
    IL_0010:  br.s       IL_001c
    IL_0012:  br         IL_00a1
    IL_0017:  br         IL_00a1
    // sequence point: {
    IL_001c:  nop
    // sequence point: ICollection<int> c = new Collection<int>();
    IL_001d:  ldarg.0
    IL_001e:  newobj     ""Collection<int>..ctor()""
    IL_0023:  stfld      ""ICollection<int> C.<Main>d__0.<c>5__1""
    // sequence point: await foreach
    IL_0028:  nop
    // sequence point: c
    IL_0029:  ldarg.0
    IL_002a:  ldarg.0
    IL_002b:  ldfld      ""ICollection<int> C.<Main>d__0.<c>5__1""
    IL_0030:  ldloca.s   V_1
    IL_0032:  initobj    ""System.Threading.CancellationToken""
    IL_0038:  ldloc.1
    IL_0039:  callvirt   ""IMyAsyncEnumerator<int> ICollection<int>.GetAsyncEnumerator(System.Threading.CancellationToken)""
    IL_003e:  stfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    // sequence point: <hidden>
    IL_0043:  br.s       IL_0063
    // sequence point: var i
    IL_0045:  ldarg.0
    IL_0046:  ldarg.0
    IL_0047:  ldfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    IL_004c:  callvirt   ""int IMyAsyncEnumerator<int>.Current.get""
    IL_0051:  stfld      ""int C.<Main>d__0.<i>5__3""
    // sequence point: {
    IL_0056:  nop
    // sequence point: Write($""Got "");
    IL_0057:  ldstr      ""Got ""
    IL_005c:  call       ""void System.Console.Write(string)""
    IL_0061:  nop
    // sequence point: }
    IL_0062:  nop
    // sequence point: in
    IL_0063:  ldarg.0
    IL_0064:  ldfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    IL_0069:  callvirt   ""System.Threading.Tasks.Task<bool> IMyAsyncEnumerator<int>.MoveNextAsync()""
    IL_006e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_0073:  stloc.2
    // sequence point: <hidden>
    IL_0074:  ldloca.s   V_2
    IL_0076:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_007b:  brtrue.s   IL_00bd
    IL_007d:  ldarg.0
    IL_007e:  ldc.i4.0
    IL_007f:  dup
    IL_0080:  stloc.0
    IL_0081:  stfld      ""int C.<Main>d__0.<>1__state""
    // async: yield
    IL_0086:  ldarg.0
    IL_0087:  ldloc.2
    IL_0088:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_008d:  ldarg.0
    IL_008e:  stloc.3
    IL_008f:  ldarg.0
    IL_0090:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_0095:  ldloca.s   V_2
    IL_0097:  ldloca.s   V_3
    IL_0099:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Main>d__0)""
    IL_009e:  nop
    IL_009f:  leave.s    IL_011a
    // async: resume
    IL_00a1:  ldarg.0
    IL_00a2:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_00a7:  stloc.2
    IL_00a8:  ldarg.0
    IL_00a9:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_00ae:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_00b4:  ldarg.0
    IL_00b5:  ldc.i4.m1
    IL_00b6:  dup
    IL_00b7:  stloc.0
    IL_00b8:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00bd:  ldarg.0
    IL_00be:  ldloca.s   V_2
    IL_00c0:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_00c5:  stfld      ""bool C.<Main>d__0.<>s__4""
    IL_00ca:  ldarg.0
    IL_00cb:  ldfld      ""bool C.<Main>d__0.<>s__4""
    IL_00d0:  brtrue     IL_0045
    IL_00d5:  ldarg.0
    IL_00d6:  ldnull
    IL_00d7:  stfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    IL_00dc:  leave.s    IL_00ff
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_00de:  stloc.s    V_4
    IL_00e0:  ldarg.0
    IL_00e1:  ldc.i4.s   -2
    IL_00e3:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00e8:  ldarg.0
    IL_00e9:  ldnull
    IL_00ea:  stfld      ""ICollection<int> C.<Main>d__0.<c>5__1""
    IL_00ef:  ldarg.0
    IL_00f0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_00f5:  ldloc.s    V_4
    IL_00f7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00fc:  nop
    IL_00fd:  leave.s    IL_011a
  }
  // sequence point: }
  IL_00ff:  ldarg.0
  IL_0100:  ldc.i4.s   -2
  IL_0102:  stfld      ""int C.<Main>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0107:  ldarg.0
  IL_0108:  ldnull
  IL_0109:  stfld      ""ICollection<int> C.<Main>d__0.<c>5__1""
  IL_010e:  ldarg.0
  IL_010f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_0114:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0119:  nop
  IL_011a:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Enhanced);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [Main]: Return value missing on the stack. { Offset = 0x34 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       53 (0x35)
                  .maxstack  2
                  .locals init (IMyAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1)
                  IL_0000:  newobj     "Collection<int>..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "IMyAsyncEnumerator<int> ICollection<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  br.s       IL_0027
                  IL_0016:  ldloc.0
                  IL_0017:  callvirt   "int IMyAsyncEnumerator<int>.Current.get"
                  IL_001c:  pop
                  IL_001d:  ldstr      "Got "
                  IL_0022:  call       "void System.Console.Write(string)"
                  IL_0027:  ldloc.0
                  IL_0028:  callvirt   "System.Threading.Tasks.Task<bool> IMyAsyncEnumerator<int>.MoveNextAsync()"
                  IL_002d:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0032:  brtrue.s   IL_0016
                  IL_0034:  ret
                }
                """);
        }

        [Fact]
        public void TestWithInterfaceImplementingPattern_ChildImplementsDisposeAsync()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;

public interface ICollection<T>
{
    IMyAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token = default);
}
public interface IMyAsyncEnumerator<T>
{
    T Current { get; }
    Task<bool> MoveNextAsync();
}

public class Collection<T> : ICollection<T>
{
    public IMyAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        return new MyAsyncEnumerator<T>();
    }
}
public sealed class MyAsyncEnumerator<T> : IMyAsyncEnumerator<T>
{
    int i = 0;
    public T Current
    {
        get
        {
            Write($""Current({i}) "");
            return default;
        }
    }
    public async Task<bool> MoveNextAsync()
    {
        Write($""NextAsync({i}) "");
        i++;
        return await Task.FromResult(i < 4);
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
        => throw null;
}

class C
{
    static async System.Threading.Tasks.Task Main()
    {
        ICollection<int> c = new Collection<int>();
        await foreach (var i in c)
        {
            Write($""Got "");
        }
    }
}";
            // DisposeAsync on implementing type is ignored, since we don't do runtime check
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var expectedOutput = "NextAsync(0) Current(1) Got NextAsync(1) Current(2) Got NextAsync(2) Current(3) Got NextAsync(3)";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Null(info.DisposeMethod);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier2 = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x5c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [Main]: Return value missing on the stack. { Offset = 0x34 }
                    """
            });
            verifier2.VerifyIL("C.Main()", """
                {
                  // Code size       53 (0x35)
                  .maxstack  2
                  .locals init (IMyAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1)
                  IL_0000:  newobj     "Collection<int>..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "IMyAsyncEnumerator<int> ICollection<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  br.s       IL_0027
                  IL_0016:  ldloc.0
                  IL_0017:  callvirt   "int IMyAsyncEnumerator<int>.Current.get"
                  IL_001c:  pop
                  IL_001d:  ldstr      "Got "
                  IL_0022:  call       "void System.Console.Write(string)"
                  IL_0027:  ldloc.0
                  IL_0028:  callvirt   "System.Threading.Tasks.Task<bool> IMyAsyncEnumerator<int>.MoveNextAsync()"
                  IL_002d:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0032:  brtrue.s   IL_0016
                  IL_0034:  ret
                }
                """);
        }

        [Fact]
        public void GetAsyncEnumerator_CancellationTokenMustBeOptional()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public IAsyncEnumerator<int> GetAsyncEnumerator(System.Threading.CancellationToken token)
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable });
            comp.VerifyDiagnostics(
                // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                );
        }

        [Fact]
        [WorkItem(50182, "https://github.com/dotnet/roslyn/issues/50182")]
        public void GetAsyncEnumerator_CancellationTokenMustBeOptional_OnIAsyncEnumerable()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    public static async Task M(IAsyncEnumerable<int> e)
    {
        await foreach (var i in e)
        {
        }
    }
}
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token);
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
";
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyEmitDiagnostics(
                // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'IAsyncEnumerable<int>' because 'IAsyncEnumerable<int>' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                //         await foreach (var i in e)
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "e").WithArguments("System.Collections.Generic.IAsyncEnumerable<int>", "GetAsyncEnumerator").WithLocation(8, 33)
                );
        }

        [Fact]
        [WorkItem(50182, "https://github.com/dotnet/roslyn/issues/50182")]
        public void GetAsyncEnumerator_CancellationTokenMustBeOptional_OnIAsyncEnumerable_ImplicitImplementation()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    public static async Task M(C c)
    {
        await foreach (var i in c)
        {
        }
    }
    public IAsyncEnumerator<int> GetAsyncEnumerator(System.Threading.CancellationToken token) => throw null;
}
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token);
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
";
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyEmitDiagnostics(
                // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                //         await foreach (var i in c)
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "c").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                );
        }

        [Fact]
        [WorkItem(50182, "https://github.com/dotnet/roslyn/issues/50182")]
        public void GetAsyncEnumerator_CancellationTokenMustBeOptional_OnIAsyncEnumerable_ExplicitImplementation()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    public static async Task M(C c)
    {
        await foreach (var i in c)
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken token) => throw null;
}
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token);
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
";
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyEmitDiagnostics(
                // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                //         await foreach (var i in c)
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "c").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                );
        }

        [Fact]
        public void GetAsyncEnumerator_Missing()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    public static async Task M(C c)
    {
        await foreach (var i in c)
        {
        }
    }
}
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
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
";
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyEmitDiagnostics(
                // (8,33): error CS0656: Missing compiler required member 'System.Collections.Generic.IAsyncEnumerable`1.GetAsyncEnumerator'
                //         await foreach (var i in c)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Collections.Generic.IAsyncEnumerable`1", "GetAsyncEnumerator").WithLocation(8, 33)
                );
        }

        [Fact]
        public void GetAsyncEnumerator_WithOptionalParameter()
        {
            string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public IAsyncEnumerator<int> GetAsyncEnumerator(int opt = 0)
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void GetAsyncEnumerator_WithParams()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator(params int[] x)
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync"");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x26 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       39 (0x27)
                  .maxstack  2
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "int[] System.Array.Empty<int>()"
                  IL_000a:  call       "C.Enumerator C.GetAsyncEnumerator(params int[])"
                  IL_000f:  stloc.0
                  IL_0010:  br.s       IL_0019
                  IL_0012:  ldloc.0
                  IL_0013:  callvirt   "int C.Enumerator.Current.get"
                  IL_0018:  pop
                  IL_0019:  ldloc.0
                  IL_001a:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001f:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0024:  brtrue.s   IL_0012
                  IL_0026:  ret
                }
                """);
        }

        [Fact]
        public void GetAsyncEnumerator_WithMultipleValidCandidatesWithOptionalParameters()
        {
            string source = @"
using System.Threading.Tasks;
using System.Collections.Generic;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public IEnumerator<int> GetAsyncEnumerator(int a = 0) => throw null;
    public IEnumerator<int> GetAsyncEnumerator(bool a = true) => throw null;
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                    // (8,33): warning CS0278: 'C' does not implement the 'collection' pattern. 'C.GetAsyncEnumerator(int)' is ambiguous with 'C.GetAsyncEnumerator(bool)'.
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "new C()").WithArguments("C", "collection", "C.GetAsyncEnumerator(int)", "C.GetAsyncEnumerator(bool)").WithLocation(8, 33),
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                );
        }

        [Fact]
        public void GetAsyncEnumerator_WithMultipleInvalidCandidates()
        {
            string source = @"
using System.Threading.Tasks;
using System.Collections.Generic;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public IEnumerator<int> GetAsyncEnumerator(int a) => throw null;
    public IEnumerator<int> GetAsyncEnumerator(bool a) => throw null;
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                );
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void PatternBasedDisposal()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
        System.Console.Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync "");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
        public async Task DisposeAsync()
        {
            System.Console.Write(""DisposeAsync "");
            await Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync DisposeAsync Done");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync DisposeAsync Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x58 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       89 (0x59)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_0016
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "int C.Enumerator.Current.get"
                    IL_0015:  pop
                    IL_0016:  ldloc.0
                    IL_0017:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                    IL_001c:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0021:  brtrue.s   IL_000f
                    IL_0023:  leave.s    IL_0028
                  }
                  catch object
                  {
                    IL_0025:  stloc.1
                    IL_0026:  leave.s    IL_0028
                  }
                  IL_0028:  ldloc.0
                  IL_0029:  brfalse.s  IL_0036
                  IL_002b:  ldloc.0
                  IL_002c:  callvirt   "System.Threading.Tasks.Task C.Enumerator.DisposeAsync()"
                  IL_0031:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
                  IL_0036:  ldloc.1
                  IL_0037:  brfalse.s  IL_004e
                  IL_0039:  ldloc.1
                  IL_003a:  isinst     "System.Exception"
                  IL_003f:  dup
                  IL_0040:  brtrue.s   IL_0044
                  IL_0042:  ldloc.1
                  IL_0043:  throw
                  IL_0044:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0049:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_004e:  ldstr      "Done"
                  IL_0053:  call       "void System.Console.Write(string)"
                  IL_0058:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void PatternBasedDisposal_TwoOverloads()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
        System.Console.Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync "");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
        public async Task DisposeAsync(int i = 0)
        {
            System.Console.Write(""DisposeAsync "");
            await Task.Yield();
        }
        public Task DisposeAsync(params string[] s)
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync DisposeAsync Done");
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void PatternBasedDisposal_NoExtensions()
        {
            string source = @"
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
        System.Console.Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync "");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
    }
}
public static class Extension
{
    public static ValueTask DisposeAsync(this C.Enumerator e) => throw null;
}";
            // extension methods do not contribute to pattern-based disposal
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync Done");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x2b }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       44 (0x2c)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0014
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  pop
                  IL_0014:  ldloc.0
                  IL_0015:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_001f:  brtrue.s   IL_000d
                  IL_0021:  ldstr      "Done"
                  IL_0026:  call       "void System.Console.Write(string)"
                  IL_002b:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void PatternBasedDisposal_NoExtensions_TwoExtensions()
        {
            string source = @"
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
        System.Console.Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync "");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
    }
}
public static class Extension1
{
    public static ValueTask DisposeAsync(this C.Enumerator c) => throw null;
}
public static class Extension2
{
    public static ValueTask DisposeAsync(this C.Enumerator c) => throw null;
}
";
            // extension methods do not contribute to pattern-based disposal
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync Done");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x2b }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       44 (0x2c)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0014
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  pop
                  IL_0014:  ldloc.0
                  IL_0015:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_001f:  brtrue.s   IL_000d
                  IL_0021:  ldstr      "Done"
                  IL_0026:  call       "void System.Console.Write(string)"
                  IL_002b:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void PatternBasedDisposal_InstanceMethodPreferredOverInterface()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
        System.Console.Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator : System.IAsyncDisposable
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync "");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
        ValueTask System.IAsyncDisposable.DisposeAsync() => throw null;
        public async ValueTask DisposeAsync()
        {
            System.Console.Write(""DisposeAsync "");
            await Task.Yield();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync DisposeAsync Done");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync DisposeAsync Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x58 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       89 (0x59)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_0016
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "int C.Enumerator.Current.get"
                    IL_0015:  pop
                    IL_0016:  ldloc.0
                    IL_0017:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                    IL_001c:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0021:  brtrue.s   IL_000f
                    IL_0023:  leave.s    IL_0028
                  }
                  catch object
                  {
                    IL_0025:  stloc.1
                    IL_0026:  leave.s    IL_0028
                  }
                  IL_0028:  ldloc.0
                  IL_0029:  brfalse.s  IL_0036
                  IL_002b:  ldloc.0
                  IL_002c:  callvirt   "System.Threading.Tasks.ValueTask C.Enumerator.DisposeAsync()"
                  IL_0031:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0036:  ldloc.1
                  IL_0037:  brfalse.s  IL_004e
                  IL_0039:  ldloc.1
                  IL_003a:  isinst     "System.Exception"
                  IL_003f:  dup
                  IL_0040:  brtrue.s   IL_0044
                  IL_0042:  ldloc.1
                  IL_0043:  throw
                  IL_0044:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0049:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_004e:  ldstr      "Done"
                  IL_0053:  call       "void System.Console.Write(string)"
                  IL_0058:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void PatternBasedDisposal_ReturnsVoid()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
        => throw null;
    public sealed class Enumerator
    {
        public Task<bool> MoveNextAsync()
            => throw null;
        public int Current
        {
            get => throw null;
        }
        public void DisposeAsync()
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable });
            comp.VerifyDiagnostics(
                // (7,33): error CS4008: Cannot await 'void'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadAwaitArgVoidCall, "new C()").WithLocation(7, 33)
                );
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void PatternBasedDisposal_ReturnsInt()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
        public int DisposeAsync()
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable });
            comp.VerifyDiagnostics(
                // (7,33): error CS1061: 'int' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "new C()").WithArguments("int", "GetAwaiter").WithLocation(7, 33)
                );
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void PatternBasedDisposal_ReturnsAwaitable()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
        System.Console.Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync "");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
        public Awaitable DisposeAsync()
        {
            System.Console.Write(""DisposeAsync "");
            return new Awaitable();
        }
    }
}

public class Awaitable
{
    public Awaiter GetAwaiter() { return new Awaiter(); }
}
public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public bool IsCompleted { get { return true; } }
    public bool GetResult() { return true; }
    public void OnCompleted(System.Action continuation) { }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync DisposeAsync Done");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync DisposeAsync Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x6e }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      111 (0x6f)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                object V_1,
                                Awaiter V_2)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_0016
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "int C.Enumerator.Current.get"
                    IL_0015:  pop
                    IL_0016:  ldloc.0
                    IL_0017:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                    IL_001c:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0021:  brtrue.s   IL_000f
                    IL_0023:  leave.s    IL_0028
                  }
                  catch object
                  {
                    IL_0025:  stloc.1
                    IL_0026:  leave.s    IL_0028
                  }
                  IL_0028:  ldloc.0
                  IL_0029:  brfalse.s  IL_004c
                  IL_002b:  ldloc.0
                  IL_002c:  callvirt   "Awaitable C.Enumerator.DisposeAsync()"
                  IL_0031:  callvirt   "Awaiter Awaitable.GetAwaiter()"
                  IL_0036:  stloc.2
                  IL_0037:  ldloc.2
                  IL_0038:  callvirt   "bool Awaiter.IsCompleted.get"
                  IL_003d:  brtrue.s   IL_0045
                  IL_003f:  ldloc.2
                  IL_0040:  call       "void System.Runtime.CompilerServices.AsyncHelpers.AwaitAwaiter<Awaiter>(Awaiter)"
                  IL_0045:  ldloc.2
                  IL_0046:  callvirt   "bool Awaiter.GetResult()"
                  IL_004b:  pop
                  IL_004c:  ldloc.1
                  IL_004d:  brfalse.s  IL_0064
                  IL_004f:  ldloc.1
                  IL_0050:  isinst     "System.Exception"
                  IL_0055:  dup
                  IL_0056:  brtrue.s   IL_005a
                  IL_0058:  ldloc.1
                  IL_0059:  throw
                  IL_005a:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_005f:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0064:  ldstr      "Done"
                  IL_0069:  call       "void System.Console.Write(string)"
                  IL_006e:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void PatternBasedDisposal_ReturnsTask()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
        System.Console.Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync "");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
        public async Task DisposeAsync()
        {
            System.Console.Write(""DisposeAsync "");
            await Task.Yield();
        }
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync DisposeAsync Done");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync DisposeAsync Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x58 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       89 (0x59)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_0016
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "int C.Enumerator.Current.get"
                    IL_0015:  pop
                    IL_0016:  ldloc.0
                    IL_0017:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                    IL_001c:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0021:  brtrue.s   IL_000f
                    IL_0023:  leave.s    IL_0028
                  }
                  catch object
                  {
                    IL_0025:  stloc.1
                    IL_0026:  leave.s    IL_0028
                  }
                  IL_0028:  ldloc.0
                  IL_0029:  brfalse.s  IL_0036
                  IL_002b:  ldloc.0
                  IL_002c:  callvirt   "System.Threading.Tasks.Task C.Enumerator.DisposeAsync()"
                  IL_0031:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
                  IL_0036:  ldloc.1
                  IL_0037:  brfalse.s  IL_004e
                  IL_0039:  ldloc.1
                  IL_003a:  isinst     "System.Exception"
                  IL_003f:  dup
                  IL_0040:  brtrue.s   IL_0044
                  IL_0042:  ldloc.1
                  IL_0043:  throw
                  IL_0044:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0049:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_004e:  ldstr      "Done"
                  IL_0053:  call       "void System.Console.Write(string)"
                  IL_0058:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void PatternBasedDisposal_ReturnsTaskOfInt()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
        System.Console.Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync "");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
        public async Task<int> DisposeAsync()
        {
            System.Console.Write(""DisposeAsync "");
            await Task.Yield();
            return 1;
        }
    }
}
";
            // it's okay to await `Task<int>` even if we don't care about the result
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync DisposeAsync Done");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync DisposeAsync Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x59 }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       90 (0x5a)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_0016
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "int C.Enumerator.Current.get"
                    IL_0015:  pop
                    IL_0016:  ldloc.0
                    IL_0017:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                    IL_001c:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0021:  brtrue.s   IL_000f
                    IL_0023:  leave.s    IL_0028
                  }
                  catch object
                  {
                    IL_0025:  stloc.1
                    IL_0026:  leave.s    IL_0028
                  }
                  IL_0028:  ldloc.0
                  IL_0029:  brfalse.s  IL_0037
                  IL_002b:  ldloc.0
                  IL_002c:  callvirt   "System.Threading.Tasks.Task<int> C.Enumerator.DisposeAsync()"
                  IL_0031:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0036:  pop
                  IL_0037:  ldloc.1
                  IL_0038:  brfalse.s  IL_004f
                  IL_003a:  ldloc.1
                  IL_003b:  isinst     "System.Exception"
                  IL_0040:  dup
                  IL_0041:  brtrue.s   IL_0045
                  IL_0043:  ldloc.1
                  IL_0044:  throw
                  IL_0045:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_004a:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_004f:  ldstr      "Done"
                  IL_0054:  call       "void System.Console.Write(string)"
                  IL_0059:  ret
                }
                """);
        }

        [Fact]
        public void PatternBasedDisposal_WithOptionalParameter()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
        }
        System.Console.Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync "");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
        public async Task<int> DisposeAsync(int i = 1)
        {
            System.Console.Write($""DisposeAsync {i} "");
            await Task.Yield();
            return 1;
        }
    }
}
";
            // it's okay to await `Task<int>` even if we don't care about the result
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync DisposeAsync 1 Done");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("MoveNextAsync DisposeAsync 1 Done"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x5a }
                    [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    [DisposeAsync]: Unexpected type on the stack. { Offset = 0x5b, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       91 (0x5b)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_0016
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "int C.Enumerator.Current.get"
                    IL_0015:  pop
                    IL_0016:  ldloc.0
                    IL_0017:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                    IL_001c:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0021:  brtrue.s   IL_000f
                    IL_0023:  leave.s    IL_0028
                  }
                  catch object
                  {
                    IL_0025:  stloc.1
                    IL_0026:  leave.s    IL_0028
                  }
                  IL_0028:  ldloc.0
                  IL_0029:  brfalse.s  IL_0038
                  IL_002b:  ldloc.0
                  IL_002c:  ldc.i4.1
                  IL_002d:  callvirt   "System.Threading.Tasks.Task<int> C.Enumerator.DisposeAsync(int)"
                  IL_0032:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0037:  pop
                  IL_0038:  ldloc.1
                  IL_0039:  brfalse.s  IL_0050
                  IL_003b:  ldloc.1
                  IL_003c:  isinst     "System.Exception"
                  IL_0041:  dup
                  IL_0042:  brtrue.s   IL_0046
                  IL_0044:  ldloc.1
                  IL_0045:  throw
                  IL_0046:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_004b:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0050:  ldstr      "Done"
                  IL_0055:  call       "void System.Console.Write(string)"
                  IL_005a:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(30956, "https://github.com/dotnet/roslyn/issues/30956")]
        public void GetAwaiterBoxingConversion()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

interface I1 { }
interface I2 { }
struct StructAwaitable1 : I1 { }
struct StructAwaitable2 : I2 { }

class Enumerable
{
    public Enumerator GetAsyncEnumerator() => new Enumerator();
    internal class Enumerator
    {
        public object Current => null;
        public StructAwaitable1 MoveNextAsync() => new StructAwaitable1();
        public StructAwaitable2 DisposeAsync() => new StructAwaitable2();
    }
}

static class Extensions
{
    internal static TaskAwaiter<bool> GetAwaiter(this I1 x)
    {
        if (x == null) throw new ArgumentNullException(nameof(x));
        Console.Write(x);
        return Task.FromResult(false).GetAwaiter();
    }
    internal static TaskAwaiter GetAwaiter(this I2 x)
    {
        if (x == null) throw new ArgumentNullException(nameof(x));
        Console.Write(x);
        return Task.CompletedTask.GetAwaiter();
    }
}

class Program
{
    static async Task Main()
    {
        await foreach (var o in new Enumerable())
        {
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "StructAwaitable1StructAwaitable2");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("StructAwaitable1StructAwaitable2"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x86 }
                    """
            });
            verifier.VerifyIL("Program.Main()", """
                {
                  // Code size      135 (0x87)
                  .maxstack  2
                  .locals init (Enumerable.Enumerator V_0,
                                object V_1,
                                System.Runtime.CompilerServices.TaskAwaiter<bool> V_2,
                                System.Runtime.CompilerServices.TaskAwaiter V_3)
                  IL_0000:  newobj     "Enumerable..ctor()"
                  IL_0005:  call       "Enumerable.Enumerator Enumerable.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_0016
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "object Enumerable.Enumerator.Current.get"
                    IL_0015:  pop
                    IL_0016:  ldloc.0
                    IL_0017:  callvirt   "StructAwaitable1 Enumerable.Enumerator.MoveNextAsync()"
                    IL_001c:  box        "StructAwaitable1"
                    IL_0021:  call       "System.Runtime.CompilerServices.TaskAwaiter<bool> Extensions.GetAwaiter(I1)"
                    IL_0026:  stloc.2
                    IL_0027:  ldloca.s   V_2
                    IL_0029:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get"
                    IL_002e:  brtrue.s   IL_0036
                    IL_0030:  ldloc.2
                    IL_0031:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.TaskAwaiter<bool>>(System.Runtime.CompilerServices.TaskAwaiter<bool>)"
                    IL_0036:  ldloca.s   V_2
                    IL_0038:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()"
                    IL_003d:  brtrue.s   IL_000f
                    IL_003f:  leave.s    IL_0044
                  }
                  catch object
                  {
                    IL_0041:  stloc.1
                    IL_0042:  leave.s    IL_0044
                  }
                  IL_0044:  ldloc.0
                  IL_0045:  brfalse.s  IL_006e
                  IL_0047:  ldloc.0
                  IL_0048:  callvirt   "StructAwaitable2 Enumerable.Enumerator.DisposeAsync()"
                  IL_004d:  box        "StructAwaitable2"
                  IL_0052:  call       "System.Runtime.CompilerServices.TaskAwaiter Extensions.GetAwaiter(I2)"
                  IL_0057:  stloc.3
                  IL_0058:  ldloca.s   V_3
                  IL_005a:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
                  IL_005f:  brtrue.s   IL_0067
                  IL_0061:  ldloc.3
                  IL_0062:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.TaskAwaiter>(System.Runtime.CompilerServices.TaskAwaiter)"
                  IL_0067:  ldloca.s   V_3
                  IL_0069:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
                  IL_006e:  ldloc.1
                  IL_006f:  brfalse.s  IL_0086
                  IL_0071:  ldloc.1
                  IL_0072:  isinst     "System.Exception"
                  IL_0077:  dup
                  IL_0078:  brtrue.s   IL_007c
                  IL_007a:  ldloc.1
                  IL_007b:  throw
                  IL_007c:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0081:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0086:  ret
                }
                """);
        }

        [Fact]
        public void TestInvalidForeachOnConstantNullObject()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in (object)null)
        {
            Console.Write(i);
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'object' because 'object' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in (object)null)
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "(object)null").WithArguments("object", "GetAsyncEnumerator").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestConstantNullObjectImplementingIEnumerable()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in (IAsyncEnumerable<int>)null)
        {
            Console.Write(i);
        }
    }
}";
            CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, parseOptions: TestOptions.Regular9)
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void TestConstantNullObjectWithGetAsyncEnumeratorPattern()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in (C)null)
        {
            Console.Write(i);
        }
    }

    public IAsyncEnumerator<int> GetAsyncEnumerator() => throw null;
}";
            CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, parseOptions: TestOptions.Regular9)
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void TestConstantNullableImplementingIEnumerable()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
public struct C : IAsyncEnumerable<int>
{
    public static async Task Main()
    {
        await foreach (var i in (C?)null)
        {
            Console.Write(i);
        }
    }

    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken) => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x72 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      115 (0x73)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                C? V_1,
                                C V_2,
                                System.Threading.CancellationToken V_3,
                                object V_4)
                  IL_0000:  ldloca.s   V_1
                  IL_0002:  dup
                  IL_0003:  initobj    "C?"
                  IL_0009:  call       "readonly C C?.Value.get"
                  IL_000e:  stloc.2
                  IL_000f:  ldloca.s   V_2
                  IL_0011:  ldloca.s   V_3
                  IL_0013:  initobj    "System.Threading.CancellationToken"
                  IL_0019:  ldloc.3
                  IL_001a:  constrained. "C"
                  IL_0020:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0025:  stloc.0
                  IL_0026:  ldnull
                  IL_0027:  stloc.s    V_4
                  .try
                  {
                    IL_0029:  br.s       IL_0036
                    IL_002b:  ldloc.0
                    IL_002c:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_0031:  call       "void System.Console.Write(int)"
                    IL_0036:  ldloc.0
                    IL_0037:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_003c:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_0041:  brtrue.s   IL_002b
                    IL_0043:  leave.s    IL_0049
                  }
                  catch object
                  {
                    IL_0045:  stloc.s    V_4
                    IL_0047:  leave.s    IL_0049
                  }
                  IL_0049:  ldloc.0
                  IL_004a:  brfalse.s  IL_0057
                  IL_004c:  ldloc.0
                  IL_004d:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0052:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0057:  ldloc.s    V_4
                  IL_0059:  brfalse.s  IL_0072
                  IL_005b:  ldloc.s    V_4
                  IL_005d:  isinst     "System.Exception"
                  IL_0062:  dup
                  IL_0063:  brtrue.s   IL_0068
                  IL_0065:  ldloc.s    V_4
                  IL_0067:  throw
                  IL_0068:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_006d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0072:  ret
                }
                """);
        }

        [Fact]
        public void TestConstantNullableWithGetAsyncEnumeratorPattern()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
public struct C
{
    public static async Task Main()
    {
        await foreach (var i in (C?)null)
        {
            Console.Write(i);
        }
    }

    public IAsyncEnumerator<int> GetAsyncEnumerator() => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x5e }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       95 (0x5f)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                C? V_1,
                                C V_2,
                                object V_3)
                  IL_0000:  ldloca.s   V_1
                  IL_0002:  dup
                  IL_0003:  initobj    "C?"
                  IL_0009:  call       "readonly C C?.Value.get"
                  IL_000e:  stloc.2
                  IL_000f:  ldloca.s   V_2
                  IL_0011:  call       "System.Collections.Generic.IAsyncEnumerator<int> C.GetAsyncEnumerator()"
                  IL_0016:  stloc.0
                  IL_0017:  ldnull
                  IL_0018:  stloc.3
                  .try
                  {
                    IL_0019:  br.s       IL_0026
                    IL_001b:  ldloc.0
                    IL_001c:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_0021:  call       "void System.Console.Write(int)"
                    IL_0026:  ldloc.0
                    IL_0027:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_002c:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_0031:  brtrue.s   IL_001b
                    IL_0033:  leave.s    IL_0038
                  }
                  catch object
                  {
                    IL_0035:  stloc.3
                    IL_0036:  leave.s    IL_0038
                  }
                  IL_0038:  ldloc.0
                  IL_0039:  brfalse.s  IL_0046
                  IL_003b:  ldloc.0
                  IL_003c:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0041:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0046:  ldloc.3
                  IL_0047:  brfalse.s  IL_005e
                  IL_0049:  ldloc.3
                  IL_004a:  isinst     "System.Exception"
                  IL_004f:  dup
                  IL_0050:  brtrue.s   IL_0054
                  IL_0052:  ldloc.3
                  IL_0053:  throw
                  IL_0054:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0059:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_005e:  ret
                }
                """);
        }

        [Fact]
        public void TestForeachNullLiteral()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in null)
        {
            Console.Write(i);
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS0186: Use of null is not valid in this context
                    //         await foreach (var i in null)
                    Diagnostic(ErrorCode.ERR_NullNotValid, "null").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestForeachDefaultLiteral()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in default)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this object self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS8716: There is no target type for the default literal.
                    //         await foreach (var i in default)
                    Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensions()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("C.Enumerator Extensions.GetAsyncEnumerator(this C self)", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> C.Enumerator.MoveNextAsync()", info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Enumerator.Current { get; private set; }", info.CurrentProperty.ToTestDisplayString());
            Assert.Null(info.DisposeMethod);
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithUpcast()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this object self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(object)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsOnDefaultObject()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in default(object))
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this object self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x21 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       34 (0x22)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  ldnull
                  IL_0001:  call       "C.Enumerator Extensions.GetAsyncEnumerator(object)"
                  IL_0006:  stloc.0
                  IL_0007:  br.s       IL_0014
                  IL_0009:  ldloc.0
                  IL_000a:  callvirt   "int C.Enumerator.Current.get"
                  IL_000f:  call       "void System.Console.Write(int)"
                  IL_0014:  ldloc.0
                  IL_0015:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_001f:  brtrue.s   IL_0009
                  IL_0021:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithStructEnumerator()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x27 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       40 (0x28)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0019
                  IL_000d:  ldloca.s   V_0
                  IL_000f:  call       "readonly int C.Enumerator.Current.get"
                  IL_0014:  call       "void System.Console.Write(int)"
                  IL_0019:  ldloca.s   V_0
                  IL_001b:  call       "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_0020:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0025:  brtrue.s   IL_000d
                  IL_0027:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithUserDefinedImplicitConversion()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static implicit operator int(C c) => 0;

    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this int self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (10,33): error CS1929: 'C' does not contain a definition for 'GetAsyncEnumerator' and the best extension method overload 'Extensions.GetAsyncEnumerator(int)' requires a receiver of type 'int'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new C()").WithArguments("C", "GetAsyncEnumerator", "Extensions.GetAsyncEnumerator(int)", "int").WithLocation(10, 33),
                // (10,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(10, 33)
                );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithNullableValueTypeConversion()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in 1)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this int? self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS1929: 'int' does not contain a definition for 'GetAsyncEnumerator' and the best extension method overload 'Extensions.GetAsyncEnumerator(int?)' requires a receiver of type 'int?'
                    //         await foreach (var i in 1)
                    Diagnostic(ErrorCode.ERR_BadInstanceArgType, "1").WithArguments("int", "GetAsyncEnumerator", "Extensions.GetAsyncEnumerator(int?)", "int?").WithLocation(8, 33),
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'int' because 'int' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in 1)
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "1").WithArguments("int", "GetAsyncEnumerator").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithUnboxingConversion()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new object())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this int self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS1929: 'object' does not contain a definition for 'GetAsyncEnumerator' and the best extension method overload 'Extensions.GetAsyncEnumerator(int)' requires a receiver of type 'int'
                    //         await foreach (var i in new object())
                    Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new object()").WithArguments("object", "GetAsyncEnumerator", "Extensions.GetAsyncEnumerator(int)", "int").WithLocation(8, 33),
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'object' because 'object' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in new object())
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new object()").WithArguments("object", "GetAsyncEnumerator").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithNullableUnwrapping()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in (int?)1)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this int self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS1929: 'int?' does not contain a definition for 'GetAsyncEnumerator' and the best extension method overload 'Extensions.GetAsyncEnumerator(int)' requires a receiver of type 'int'
                    //         await foreach (var i in (int?)1)
                    Diagnostic(ErrorCode.ERR_BadInstanceArgType, "(int?)1").WithArguments("int?", "GetAsyncEnumerator", "Extensions.GetAsyncEnumerator(int)", "int").WithLocation(8, 33),
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'int?' because 'int?' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in (int?)1)
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "(int?)1").WithArguments("int?", "GetAsyncEnumerator").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithZeroToEnumConversion()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public enum E { Default = 0 }
public class C
{
    public static async Task Main()
    {
        await foreach (var i in 0)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this E self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (9,33): error CS1929: 'int' does not contain a definition for 'GetAsyncEnumerator' and the best extension method overload 'Extensions.GetAsyncEnumerator(E)' requires a receiver of type 'E'
                    //         await foreach (var i in 0)
                    Diagnostic(ErrorCode.ERR_BadInstanceArgType, "0").WithArguments("int", "GetAsyncEnumerator", "Extensions.GetAsyncEnumerator(E)", "E").WithLocation(9, 33),
                    // (9,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'int' because 'int' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in 0)
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "0").WithArguments("int", "GetAsyncEnumerator").WithLocation(9, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithUnconstrainedGenericConversion()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await Inner(1);

        async Task Inner<T>(T t)
        {
            await foreach (var i in t)
            {
                Console.Write(i);
            }
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this object self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xb }
                    [<Main>g__Inner|0_0]: Return value missing on the stack. { Offset = 0x26 }
                    """
            });
            verifier.VerifyIL("C.<Main>g__Inner|0_0<T>(T)", """
                {
                  // Code size       39 (0x27)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  box        "T"
                  IL_0006:  call       "C.Enumerator Extensions.GetAsyncEnumerator(object)"
                  IL_000b:  stloc.0
                  IL_000c:  br.s       IL_0019
                  IL_000e:  ldloc.0
                  IL_000f:  callvirt   "int C.Enumerator.Current.get"
                  IL_0014:  call       "void System.Console.Write(int)"
                  IL_0019:  ldloc.0
                  IL_001a:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001f:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0024:  brtrue.s   IL_000e
                  IL_0026:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithConstrainedGenericConversion()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await Inner(1);

        async Task Inner<T>(T t) where T : IConvertible
        {
            await foreach (var i in t)
            {
                Console.Write(i);
            }
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this IConvertible self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xb }
                    [<Main>g__Inner|0_0]: Return value missing on the stack. { Offset = 0x26 }
                    """
            });
            verifier.VerifyIL("C.<Main>g__Inner|0_0<T>(T)", """
                {
                  // Code size       39 (0x27)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  box        "T"
                  IL_0006:  call       "C.Enumerator Extensions.GetAsyncEnumerator(System.IConvertible)"
                  IL_000b:  stloc.0
                  IL_000c:  br.s       IL_0019
                  IL_000e:  ldloc.0
                  IL_000f:  callvirt   "int C.Enumerator.Current.get"
                  IL_0014:  call       "void System.Console.Write(int)"
                  IL_0019:  ldloc.0
                  IL_001a:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001f:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0024:  brtrue.s   IL_000e
                  IL_0026:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithFormattableStringConversion1()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in $"" "")
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this FormattableString self) => throw null;
    public static C.Enumerator GetAsyncEnumerator(this object self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  ldstr      " "
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(object)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithFormattableStringConversion2()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in $"" "")
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this string self) => new C.Enumerator();
    public static C.Enumerator GetAsyncEnumerator(this FormattableString self) => throw null;
    public static C.Enumerator GetAsyncEnumerator(this object self) => throw null;
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  ldstr      " "
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(string)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithFormattableStringConversion3()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in $"" "")
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this FormattableString self) => throw null;
}";
            CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS1929: 'string' does not contain a definition for 'GetAsyncEnumerator' and the best extension method overload 'Extensions.GetAsyncEnumerator(FormattableString)' requires a receiver of type 'FormattableString'
                    //         await foreach (var i in $" ")
                    Diagnostic(ErrorCode.ERR_BadInstanceArgType, @"$"" """).WithArguments("string", "GetAsyncEnumerator", "Extensions.GetAsyncEnumerator(System.FormattableString)", "System.FormattableString").WithLocation(8, 33),
                    // (8,33): error CS8415: Asynchronous foreach statement cannot operate on variables of type 'string' because 'string' does not contain a public instance or extension definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'await foreach'?
                    //         await foreach (var i in $" ")
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMemberWrongAsync, @"$"" """).WithArguments("string", "GetAsyncEnumerator").WithLocation(8, 33));

        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithDelegateConversion()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in () => 42)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this Func<int> self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS0446: Foreach cannot operate on a 'lambda expression'. Did you intend to invoke the 'lambda expression'?
                    //         await foreach (var i in () => 42)
                    Diagnostic(ErrorCode.ERR_AnonMethGrpInForEach, "() => 42").WithArguments("lambda expression").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithBoxing()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public struct C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this object self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x2e }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       47 (0x2f)
                  .maxstack  1
                  .locals init (C.Enumerator V_0,
                                C V_1)
                  IL_0000:  ldloca.s   V_1
                  IL_0002:  initobj    "C"
                  IL_0008:  ldloc.1
                  IL_0009:  box        "C"
                  IL_000e:  call       "C.Enumerator Extensions.GetAsyncEnumerator(object)"
                  IL_0013:  stloc.0
                  IL_0014:  br.s       IL_0021
                  IL_0016:  ldloc.0
                  IL_0017:  callvirt   "int C.Enumerator.Current.get"
                  IL_001c:  call       "void System.Console.Write(int)"
                  IL_0021:  ldloc.0
                  IL_0022:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_0027:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_002c:  brtrue.s   IL_0016
                  IL_002e:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsOnInterface()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public interface I {}
public class C : I
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this I self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(I)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsOnDelegate()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in (Func<int>)(() => 42))
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this Func<int> self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x3f }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       64 (0x40)
                  .maxstack  2
                  .locals init (C.Enumerator V_0)
                  IL_0000:  ldsfld     "System.Func<int> C.<>c.<>9__0_0"
                  IL_0005:  dup
                  IL_0006:  brtrue.s   IL_001f
                  IL_0008:  pop
                  IL_0009:  ldsfld     "C.<>c C.<>c.<>9"
                  IL_000e:  ldftn      "int C.<>c.<Main>b__0_0()"
                  IL_0014:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_0019:  dup
                  IL_001a:  stsfld     "System.Func<int> C.<>c.<>9__0_0"
                  IL_001f:  call       "C.Enumerator Extensions.GetAsyncEnumerator(System.Func<int>)"
                  IL_0024:  stloc.0
                  IL_0025:  br.s       IL_0032
                  IL_0027:  ldloc.0
                  IL_0028:  callvirt   "int C.Enumerator.Current.get"
                  IL_002d:  call       "void System.Console.Write(int)"
                  IL_0032:  ldloc.0
                  IL_0033:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_0038:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_003d:  brtrue.s   IL_0027
                  IL_003f:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsOnEnum()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public enum E { Default }
public class C
{
    public static async Task Main()
    {
        await foreach (var i in E.Default)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this E self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x21 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       34 (0x22)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  ldc.i4.0
                  IL_0001:  call       "C.Enumerator Extensions.GetAsyncEnumerator(E)"
                  IL_0006:  stloc.0
                  IL_0007:  br.s       IL_0014
                  IL_0009:  ldloc.0
                  IL_000a:  callvirt   "int C.Enumerator.Current.get"
                  IL_000f:  call       "void System.Console.Write(int)"
                  IL_0014:  ldloc.0
                  IL_0015:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_001f:  brtrue.s   IL_0009
                  IL_0021:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsOnNullable()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in (int?)null)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this int? self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x29 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       42 (0x2a)
                  .maxstack  1
                  .locals init (C.Enumerator V_0,
                                int? V_1)
                  IL_0000:  ldloca.s   V_1
                  IL_0002:  initobj    "int?"
                  IL_0008:  ldloc.1
                  IL_0009:  call       "C.Enumerator Extensions.GetAsyncEnumerator(int?)"
                  IL_000e:  stloc.0
                  IL_000f:  br.s       IL_001c
                  IL_0011:  ldloc.0
                  IL_0012:  callvirt   "int C.Enumerator.Current.get"
                  IL_0017:  call       "void System.Console.Write(int)"
                  IL_001c:  ldloc.0
                  IL_001d:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_0022:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0027:  brtrue.s   IL_0011
                  IL_0029:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsOnConstantNullObject()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in (object)null)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this object self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x21 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       34 (0x22)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  ldnull
                  IL_0001:  call       "C.Enumerator Extensions.GetAsyncEnumerator(object)"
                  IL_0006:  stloc.0
                  IL_0007:  br.s       IL_0014
                  IL_0009:  ldloc.0
                  IL_000a:  callvirt   "int C.Enumerator.Current.get"
                  IL_000f:  call       "void System.Console.Write(int)"
                  IL_0014:  ldloc.0
                  IL_0015:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001a:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_001f:  brtrue.s   IL_0009
                  IL_0021:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsOnTypeParameter()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new object())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator<T>(this T self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "object..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator<object>(object)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternOnRange()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in 1..4)
        {
            Console.Write(i);
        }
    }
}
public static class Extensions
{
    public static async IAsyncEnumerator<int> GetAsyncEnumerator(this Range range)
    {
        await Task.Yield();
        for(var i = range.Start.Value; i < range.End.Value; i++)
        {
            yield return i;
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(
                new[] { source, TestSources.Index, TestSources.Range, AsyncStreamsTypes },
                options: TestOptions.DebugExe,
                parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x5e }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       95 (0x5f)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                object V_1)
                  IL_0000:  ldc.i4.1
                  IL_0001:  call       "System.Index System.Index.op_Implicit(int)"
                  IL_0006:  ldc.i4.4
                  IL_0007:  call       "System.Index System.Index.op_Implicit(int)"
                  IL_000c:  newobj     "System.Range..ctor(System.Index, System.Index)"
                  IL_0011:  call       "System.Collections.Generic.IAsyncEnumerator<int> Extensions.GetAsyncEnumerator(System.Range)"
                  IL_0016:  stloc.0
                  IL_0017:  ldnull
                  IL_0018:  stloc.1
                  .try
                  {
                    IL_0019:  br.s       IL_0026
                    IL_001b:  ldloc.0
                    IL_001c:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_0021:  call       "void System.Console.Write(int)"
                    IL_0026:  ldloc.0
                    IL_0027:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_002c:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_0031:  brtrue.s   IL_001b
                    IL_0033:  leave.s    IL_0038
                  }
                  catch object
                  {
                    IL_0035:  stloc.1
                    IL_0036:  leave.s    IL_0038
                  }
                  IL_0038:  ldloc.0
                  IL_0039:  brfalse.s  IL_0046
                  IL_003b:  ldloc.0
                  IL_003c:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0041:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0046:  ldloc.1
                  IL_0047:  brfalse.s  IL_005e
                  IL_0049:  ldloc.1
                  IL_004a:  isinst     "System.Exception"
                  IL_004f:  dup
                  IL_0050:  brtrue.s   IL_0054
                  IL_0052:  ldloc.1
                  IL_0053:  throw
                  IL_0054:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0059:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_005e:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsOnTuple()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
public struct C
{
    public static async Task Main()
    {
        await foreach (var i in (1, 2, 3))
        {
            Console.Write(i);
        }
    }
}
public static class Extensions
{
    public static async IAsyncEnumerator<T> GetAsyncEnumerator<T>(this (T first, T second, T third) self)
    {
        await Task.Yield();
        yield return self.first;
        yield return self.second;
        yield return self.third;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x55 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       86 (0x56)
                  .maxstack  3
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                object V_1)
                  IL_0000:  ldc.i4.1
                  IL_0001:  ldc.i4.2
                  IL_0002:  ldc.i4.3
                  IL_0003:  newobj     "System.ValueTuple<int, int, int>..ctor(int, int, int)"
                  IL_0008:  call       "System.Collections.Generic.IAsyncEnumerator<int> Extensions.GetAsyncEnumerator<int>(System.ValueTuple<int, int, int>)"
                  IL_000d:  stloc.0
                  IL_000e:  ldnull
                  IL_000f:  stloc.1
                  .try
                  {
                    IL_0010:  br.s       IL_001d
                    IL_0012:  ldloc.0
                    IL_0013:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_0018:  call       "void System.Console.Write(int)"
                    IL_001d:  ldloc.0
                    IL_001e:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_0023:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_0028:  brtrue.s   IL_0012
                    IL_002a:  leave.s    IL_002f
                  }
                  catch object
                  {
                    IL_002c:  stloc.1
                    IL_002d:  leave.s    IL_002f
                  }
                  IL_002f:  ldloc.0
                  IL_0030:  brfalse.s  IL_003d
                  IL_0032:  ldloc.0
                  IL_0033:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0038:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_003d:  ldloc.1
                  IL_003e:  brfalse.s  IL_0055
                  IL_0040:  ldloc.1
                  IL_0041:  isinst     "System.Exception"
                  IL_0046:  dup
                  IL_0047:  brtrue.s   IL_004b
                  IL_0049:  ldloc.1
                  IL_004a:  throw
                  IL_004b:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0050:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0055:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsOnTupleWithNestedConversions()
        {
            var source = @"
using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

public struct C
{
    public static async Task Main()
    {
        await foreach (var (a, b) in (new[] { 1, 2, 3 }, new List<decimal>{ 0.1m, 0.2m, 0.3m }))
        {
            Console.WriteLine((a + b).ToString(CultureInfo.InvariantCulture));
        }
    }
}
public static class Extensions
{
    public static async IAsyncEnumerator<(T1, T2)> GetAsyncEnumerator<T1, T2>(this (IEnumerable<T1> first, IEnumerable<T2> second) self)
    {
        await Task.Yield();
        foreach(var pair in self.first.Zip(self.second, (a,b) => (a,b)))
        {
            yield return pair;
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            string expectedOutput = @"1.1
2.2
3.3";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xd4 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      213 (0xd5)
                  .maxstack  9
                  .locals init (System.Collections.Generic.IAsyncEnumerator<System.ValueTuple<int, decimal>> V_0,
                                System.ValueTuple<int[], System.Collections.Generic.List<decimal>> V_1,
                                object V_2,
                                int V_3, //a
                                decimal V_4, //b
                                decimal V_5)
                  IL_0000:  ldloca.s   V_1
                  IL_0002:  ldc.i4.3
                  IL_0003:  newarr     "int"
                  IL_0008:  dup
                  IL_0009:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D"
                  IL_000e:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
                  IL_0013:  newobj     "System.Collections.Generic.List<decimal>..ctor()"
                  IL_0018:  dup
                  IL_0019:  ldc.i4.1
                  IL_001a:  ldc.i4.0
                  IL_001b:  ldc.i4.0
                  IL_001c:  ldc.i4.0
                  IL_001d:  ldc.i4.1
                  IL_001e:  newobj     "decimal..ctor(int, int, int, bool, byte)"
                  IL_0023:  callvirt   "void System.Collections.Generic.List<decimal>.Add(decimal)"
                  IL_0028:  dup
                  IL_0029:  ldc.i4.2
                  IL_002a:  ldc.i4.0
                  IL_002b:  ldc.i4.0
                  IL_002c:  ldc.i4.0
                  IL_002d:  ldc.i4.1
                  IL_002e:  newobj     "decimal..ctor(int, int, int, bool, byte)"
                  IL_0033:  callvirt   "void System.Collections.Generic.List<decimal>.Add(decimal)"
                  IL_0038:  dup
                  IL_0039:  ldc.i4.3
                  IL_003a:  ldc.i4.0
                  IL_003b:  ldc.i4.0
                  IL_003c:  ldc.i4.0
                  IL_003d:  ldc.i4.1
                  IL_003e:  newobj     "decimal..ctor(int, int, int, bool, byte)"
                  IL_0043:  callvirt   "void System.Collections.Generic.List<decimal>.Add(decimal)"
                  IL_0048:  call       "System.ValueTuple<int[], System.Collections.Generic.List<decimal>>..ctor(int[], System.Collections.Generic.List<decimal>)"
                  IL_004d:  ldloc.1
                  IL_004e:  ldfld      "int[] System.ValueTuple<int[], System.Collections.Generic.List<decimal>>.Item1"
                  IL_0053:  ldloc.1
                  IL_0054:  ldfld      "System.Collections.Generic.List<decimal> System.ValueTuple<int[], System.Collections.Generic.List<decimal>>.Item2"
                  IL_0059:  newobj     "System.ValueTuple<System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<decimal>>..ctor(System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<decimal>)"
                  IL_005e:  call       "System.Collections.Generic.IAsyncEnumerator<System.ValueTuple<int, decimal>> Extensions.GetAsyncEnumerator<int, decimal>(System.ValueTuple<System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<decimal>>)"
                  IL_0063:  stloc.0
                  IL_0064:  ldnull
                  IL_0065:  stloc.2
                  .try
                  {
                    IL_0066:  br.s       IL_009c
                    IL_0068:  ldloc.0
                    IL_0069:  callvirt   "System.ValueTuple<int, decimal> System.Collections.Generic.IAsyncEnumerator<System.ValueTuple<int, decimal>>.Current.get"
                    IL_006e:  dup
                    IL_006f:  ldfld      "int System.ValueTuple<int, decimal>.Item1"
                    IL_0074:  stloc.3
                    IL_0075:  ldfld      "decimal System.ValueTuple<int, decimal>.Item2"
                    IL_007a:  stloc.s    V_4
                    IL_007c:  ldloc.3
                    IL_007d:  call       "decimal decimal.op_Implicit(int)"
                    IL_0082:  ldloc.s    V_4
                    IL_0084:  call       "decimal decimal.op_Addition(decimal, decimal)"
                    IL_0089:  stloc.s    V_5
                    IL_008b:  ldloca.s   V_5
                    IL_008d:  call       "System.Globalization.CultureInfo System.Globalization.CultureInfo.InvariantCulture.get"
                    IL_0092:  call       "string decimal.ToString(System.IFormatProvider)"
                    IL_0097:  call       "void System.Console.WriteLine(string)"
                    IL_009c:  ldloc.0
                    IL_009d:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<System.ValueTuple<int, decimal>>.MoveNextAsync()"
                    IL_00a2:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_00a7:  brtrue.s   IL_0068
                    IL_00a9:  leave.s    IL_00ae
                  }
                  catch object
                  {
                    IL_00ab:  stloc.2
                    IL_00ac:  leave.s    IL_00ae
                  }
                  IL_00ae:  ldloc.0
                  IL_00af:  brfalse.s  IL_00bc
                  IL_00b1:  ldloc.0
                  IL_00b2:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_00b7:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_00bc:  ldloc.2
                  IL_00bd:  brfalse.s  IL_00d4
                  IL_00bf:  ldloc.2
                  IL_00c0:  isinst     "System.Exception"
                  IL_00c5:  dup
                  IL_00c6:  brtrue.s   IL_00ca
                  IL_00c8:  ldloc.2
                  IL_00c9:  throw
                  IL_00ca:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_00cf:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_00d4:  ret
                }
                """);
        }

        [Fact]
        public void TestMoveNextAsyncPatternViaExtensions1()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
    public static bool MoveNext(this C.Enumerator e) => false;
}";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,33): error CS0117: 'C.Enumerator' does not contain a definition for 'MoveNextAsync'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator", "MoveNextAsync").WithLocation(8, 33),
                // (8,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'Extensions.GetAsyncEnumerator(C)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "Extensions.GetAsyncEnumerator(C)").WithLocation(8, 33)
                );
        }

        [Fact]
        public void TestMoveNextAsyncPatternViaExtensions2()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        
    }

    public C.Enumerator GetAsyncEnumerator() => new C.Enumerator();
}
public static class Extensions
{
    public static bool MoveNext(this C.Enumerator e) => false;
}";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,33): error CS0117: 'C.Enumerator' does not contain a definition for 'MoveNextAsync'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator", "MoveNextAsync").WithLocation(8, 33),
                // (8,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(8, 33)
                );
        }

        [Fact]
        public void TestPreferAsyncEnumeratorPatternFromInstanceThanViaExtension()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator1
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
    public sealed class Enumerator2
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => throw null;
    }

    public C.Enumerator1 GetAsyncEnumerator() => new C.Enumerator1();
}

public static class Extensions
{
    public static C.Enumerator2 GetAsyncEnumerator(this C self) => throw null;
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator1 V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator1 C.GetAsyncEnumerator()"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator1.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator1.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestPreferAsyncEnumeratorPatternFromInstanceThanViaExtensionEvenWhenInvalid()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator1
    {
    }
    public sealed class Enumerator2
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => throw null;
    }

    public C.Enumerator1 GetAsyncEnumerator() => throw null;
}

public static class Extensions
{
    public static C.Enumerator2 GetAsyncEnumerator(this C self) => throw null;
}";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,33): error CS0117: 'C.Enumerator1' does not contain a definition for 'Current'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator1", "Current").WithLocation(8, 33),
                // (8,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator1' of 'C.GetAsyncEnumerator()' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator1", "C.GetAsyncEnumerator()").WithLocation(8, 33));
        }

        [Fact]
        public void TestPreferAsyncEnumeratorPatternFromIAsyncEnumerableInterfaceThanViaExtension()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class C : IAsyncEnumerable<int>
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    
    public sealed class Enumerator1 : IAsyncEnumerator<int>
    {
        public int Current { get; private set; }

        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(Current++ != 3);

        public ValueTask DisposeAsync() => new ValueTask();
    }

    public sealed class Enumerator2
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => throw null;
    }

    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken cancellationToken) => new C.Enumerator1();
}

public static class Extensions
{
    public static C.Enumerator2 GetAsyncEnumerator(this C self) => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x5b }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       92 (0x5c)
                  .maxstack  2
                  .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                System.Threading.CancellationToken V_1,
                                object V_2)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldloca.s   V_1
                  IL_0007:  initobj    "System.Threading.CancellationToken"
                  IL_000d:  ldloc.1
                  IL_000e:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_0013:  stloc.0
                  IL_0014:  ldnull
                  IL_0015:  stloc.2
                  .try
                  {
                    IL_0016:  br.s       IL_0023
                    IL_0018:  ldloc.0
                    IL_0019:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                    IL_001e:  call       "void System.Console.Write(int)"
                    IL_0023:  ldloc.0
                    IL_0024:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                    IL_0029:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_002e:  brtrue.s   IL_0018
                    IL_0030:  leave.s    IL_0035
                  }
                  catch object
                  {
                    IL_0032:  stloc.2
                    IL_0033:  leave.s    IL_0035
                  }
                  IL_0035:  ldloc.0
                  IL_0036:  brfalse.s  IL_0043
                  IL_0038:  ldloc.0
                  IL_0039:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_003e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0043:  ldloc.2
                  IL_0044:  brfalse.s  IL_005b
                  IL_0046:  ldloc.2
                  IL_0047:  isinst     "System.Exception"
                  IL_004c:  dup
                  IL_004d:  brtrue.s   IL_0051
                  IL_004f:  ldloc.2
                  IL_0050:  throw
                  IL_0051:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0056:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_005b:  ret
                }
                """);
        }

        [Fact]
        public void TestCannotUseExtensionGetAsyncEnumeratorOnDynamic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public class C
{
    public static async Task Main()
    {
        await foreach (var i in (dynamic)new C())
        {
            Console.Write(i);
        }
    }

    public sealed class Enumerator2
    {
        public int Current { get; private set; }
        public bool MoveNext() => throw null;
    }
}

public static class Extensions
{
    public static C.Enumerator2 GetAsyncEnumerator(this C self) => throw null;
}";
            // (9,33): error CS8416: Cannot use a collection of dynamic type in an asynchronous foreach
            //         await foreach (var i in (dynamic)new C())
            DiagnosticDescription expected = Diagnostic(ErrorCode.ERR_BadDynamicAwaitForEach, "(dynamic)new C()").WithLocation(9, 33);
            CreateCompilationWithCSharp(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(expected);

            var comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAmbiguousExtensions()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions1
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}
public static class Extensions2
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,33): warning CS0278: 'C' does not implement the 'collection' pattern. 'Extensions1.GetAsyncEnumerator(C)' is ambiguous with 'Extensions2.GetAsyncEnumerator(C)'.
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "new C()").WithArguments("C", "collection", "Extensions1.GetAsyncEnumerator(C)", "Extensions2.GetAsyncEnumerator(C)").WithLocation(8, 33),
                // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAmbiguousExtensionsWhenOneHasCorrectPattern()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions1
{
    public static int GetAsyncEnumerator(this C self) => 42;
}
public static class Extensions2
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): warning CS0278: 'C' does not implement the 'collection' pattern. 'Extensions1.GetAsyncEnumerator(C)' is ambiguous with 'Extensions2.GetAsyncEnumerator(C)'.
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "new C()").WithArguments("C", "collection", "Extensions1.GetAsyncEnumerator(C)", "Extensions2.GetAsyncEnumerator(C)").WithLocation(8, 33),
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAmbiguousExtensionsWhenNeitherHasCorrectPattern()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions1
{
    public static int GetAsyncEnumerator(this C self) => 42;
}
public static class Extensions2
{
    public static bool GetAsyncEnumerator(this C self) => true;
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): warning CS0278: 'C' does not implement the 'collection' pattern. 'Extensions1.GetAsyncEnumerator(C)' is ambiguous with 'Extensions2.GetAsyncEnumerator(C)'.
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "new C()").WithArguments("C", "collection", "Extensions1.GetAsyncEnumerator(C)", "Extensions2.GetAsyncEnumerator(C)").WithLocation(8, 33),
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAmbiguousExtensionsWhenOneHasCorrectNumberOfParameters()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions1
{
    public static C.Enumerator GetAsyncEnumerator(this C self, int _) => throw null;
}
public static class Extensions2
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions2.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAmbiguousExtensionsWhenNeitherHasCorrectNumberOfParameters()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions1
{
    public static C.Enumerator GetAsyncEnumerator(this C self, int _) => new C.Enumerator();
}
public static class Extensions2
{
    public static C.Enumerator GetAsyncEnumerator(this C self, bool _) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS1501: No overload for method 'GetAsyncEnumerator' takes 0 arguments
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadArgCount, "new C()").WithArguments("GetAsyncEnumerator", "0").WithLocation(8, 33),
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAmbiguousExtensionsOnDifferentInterfaces()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public interface I1 {}
public interface I2 {}

public class C : I1, I2
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions1
{
    public static C.Enumerator GetAsyncEnumerator(this I1 self) => new C.Enumerator();
}
public static class Extensions2
{
    public static C.Enumerator GetAsyncEnumerator(this I2 self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (12,33): warning CS0278: 'C' does not implement the 'collection' pattern. 'Extensions1.GetAsyncEnumerator(I1)' is ambiguous with 'Extensions2.GetAsyncEnumerator(I2)'.
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "new C()").WithArguments("C", "collection", "Extensions1.GetAsyncEnumerator(I1)", "Extensions2.GetAsyncEnumerator(I2)").WithLocation(12, 33),
                    // (12,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(12, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAmbiguousExtensionsWithMostSpecificReceiver()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public interface I {}
public class C : I
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions1
{
    public static C.Enumerator GetAsyncEnumerator(this I self) => throw null;
}
public static class Extensions2
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions2.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAmbiguousExtensionsWithMostSpecificReceiverWhenMostSpecificReceiverDoesntImplementPattern()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public interface I {}
public class C : I
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions1
{
    public static C.Enumerator GetAsyncEnumerator(this I self) => throw null;
}
public static class Extensions2
{ 
    public static int GetAsyncEnumerator(this C self) => 42;
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (10,33): error CS0117: 'int' does not contain a definition for 'Current'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("int", "Current").WithLocation(10, 33),
                    // (10,33): error CS8412: Asynchronous foreach requires that the return type 'int' of 'Extensions2.GetAsyncEnumerator(C)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("int", "Extensions2.GetAsyncEnumerator(C)").WithLocation(10, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAmbiguousExtensionsWhenOneHasOptionalParams()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions1
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}
public static class Extensions2
{
    public static C.Enumerator GetAsyncEnumerator(this C self, int a = 0) => throw null;
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions1.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAmbiguousExtensionsWhenOneHasFewerOptionalParams()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions1
{
    public static C.Enumerator GetAsyncEnumerator(this C self, int a = 0, int b = 1) => new C.Enumerator();
}
public static class Extensions2
{
    public static C.Enumerator GetAsyncEnumerator(this C self, int a = 0) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): warning CS0278: 'C' does not implement the 'collection' pattern. 'Extensions1.GetAsyncEnumerator(C, int, int)' is ambiguous with 'Extensions2.GetAsyncEnumerator(C, int)'.
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "new C()").WithArguments("C", "collection", "Extensions1.GetAsyncEnumerator(C, int, int)", "Extensions2.GetAsyncEnumerator(C, int)").WithLocation(8, 33),
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionWithOptionalParameter()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public Enumerator(int start) => Current = start;
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self, int x = 1) => new C.Enumerator(x);
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "23");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("23"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x26 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       39 (0x27)
                  .maxstack  2
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldc.i4.1
                  IL_0006:  call       "C.Enumerator Extensions.GetAsyncEnumerator(C, int)"
                  IL_000b:  stloc.0
                  IL_000c:  br.s       IL_0019
                  IL_000e:  ldloc.0
                  IL_000f:  callvirt   "int C.Enumerator.Current.get"
                  IL_0014:  call       "void System.Console.Write(int)"
                  IL_0019:  ldloc.0
                  IL_001a:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001f:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0024:  brtrue.s   IL_000e
                  IL_0026:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionWithArgList()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self, __arglist) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                 .VerifyDiagnostics(
                    // (8,33): error CS7036: There is no argument given that corresponds to the required parameter '__arglist' of 'Extensions.GetAsyncEnumerator(C, __arglist)'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "new C()").WithArguments("__arglist", "Extensions.GetAsyncEnumerator(C, __arglist)").WithLocation(8, 33),
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionWithParams()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public Enumerator(int[] arr) => Current = arr.Length;
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self, params int[] x) => new C.Enumerator(x);
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x2a }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       43 (0x2b)
                  .maxstack  2
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "int[] System.Array.Empty<int>()"
                  IL_000a:  call       "C.Enumerator Extensions.GetAsyncEnumerator(C, params int[])"
                  IL_000f:  stloc.0
                  IL_0010:  br.s       IL_001d
                  IL_0012:  ldloc.0
                  IL_0013:  callvirt   "int C.Enumerator.Current.get"
                  IL_0018:  call       "void System.Console.Write(int)"
                  IL_001d:  ldloc.0
                  IL_001e:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_0023:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0028:  brtrue.s   IL_0012
                  IL_002a:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaRefExtensionOnNonAssignableVariable()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public struct C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this ref C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,33): error CS1510: A ref or out value must be an assignable variable
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "new C()").WithLocation(8, 33));
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaRefExtensionOnAssignableVariable()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public struct C
{
    public static async Task Main()
    {
        var c = new C();
        await foreach (var i in c)
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this ref C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,33): error CS1510: A ref or out value must be an assignable variable
                //         await foreach (var i in c)
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "c").WithLocation(9, 33)
                );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaOutExtension()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public struct C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this out C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,33): error CS1620: Argument 1 must be passed with the 'out' keyword
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadArgRef, "new C()").WithArguments("1", "out").WithLocation(8, 33),
                // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33),
                // (21,56): error CS8328:  The parameter modifier 'out' cannot be used with 'this'
                //     public static C.Enumerator GetAsyncEnumerator(this out C self) => new C.Enumerator();
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this").WithLocation(21, 56));
        }

        [Theory]
        [InlineData("in", LanguageVersion.CSharp9)]
        [InlineData("ref readonly", LanguageVersion.Preview)]
        public void TestGetAsyncEnumeratorPatternViaInExtensionOnNonAssignableVariable(string modifier, LanguageVersion languageVersion)
        {
            string source = @"
using System;
using System.Threading.Tasks;
public struct C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this " + modifier + @" C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x2b }
                    """
            });
            verifier.VerifyIL("C.Main()", $$"""
                {
                  // Code size       44 (0x2c)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                C V_1)
                  IL_0000:  ldloca.s   V_1
                  IL_0002:  dup
                  IL_0003:  initobj    "C"
                  IL_0009:  call       "C.Enumerator Extensions.GetAsyncEnumerator({{modifier}} C)"
                  IL_000e:  stloc.0
                  IL_000f:  br.s       IL_001d
                  IL_0011:  ldloca.s   V_0
                  IL_0013:  call       "readonly int C.Enumerator.Current.get"
                  IL_0018:  call       "void System.Console.Write(int)"
                  IL_001d:  ldloca.s   V_0
                  IL_001f:  call       "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_0024:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0029:  brtrue.s   IL_0011
                  IL_002b:  ret
                }
                """);
        }

        [Theory]
        [InlineData("in", LanguageVersion.CSharp9)]
        [InlineData("ref readonly", LanguageVersion.Preview)]
        public void TestGetAsyncEnumeratorPatternViaInExtensionOnAssignableVariable(string modifier, LanguageVersion languageVersion)
        {
            string source = @"
using System;
using System.Threading.Tasks;
public struct C
{
    public static async Task Main()
    {
        var c = new C();
        await foreach (var i in c)
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this " + modifier + @" C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x2c }
                    """
            });
            verifier.VerifyIL("C.Main()", $$"""
                {
                  // Code size       45 (0x2d)
                  .maxstack  1
                  .locals init (C V_0, //c
                                C.Enumerator V_1)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "C"
                  IL_0008:  ldloca.s   V_0
                  IL_000a:  call       "C.Enumerator Extensions.GetAsyncEnumerator({{modifier}} C)"
                  IL_000f:  stloc.1
                  IL_0010:  br.s       IL_001e
                  IL_0012:  ldloca.s   V_1
                  IL_0014:  call       "readonly int C.Enumerator.Current.get"
                  IL_0019:  call       "void System.Console.Write(int)"
                  IL_001e:  ldloca.s   V_1
                  IL_0020:  call       "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_0025:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_002a:  brtrue.s   IL_0012
                  IL_002c:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsCSharp8()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,33): error CS8400: Feature 'extension GetAsyncEnumerator' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "new C()").WithArguments("extension GetAsyncEnumerator", "9.0").WithLocation(8, 33)
                );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaInternalExtensions()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    internal static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionInInternalClass()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
internal static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionWithInvalidEnumerator()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
    }
}
internal static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS0117: 'C.Enumerator' does not contain a definition for 'MoveNextAsync'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator", "MoveNextAsync").WithLocation(8, 33),
                    // (8,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator' of 'Extensions.GetAsyncEnumerator(C)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "Extensions.GetAsyncEnumerator(C)").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionWithInstanceGetAsyncEnumeratorReturningTypeWhichDoesntMatchPattern()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }

    public sealed class Enumerator1
    {
        public int Current { get; private set; }
    }

    public sealed class Enumerator2
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }

    public Enumerator1 GetAsyncEnumerator() => new Enumerator1();
}
internal static class Extensions
{
    public static C.Enumerator2 GetAsyncEnumerator(this C self) => new C.Enumerator2();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS0117: 'C.Enumerator1' does not contain a definition for 'MoveNextAsync'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator1", "MoveNextAsync").WithLocation(8, 33),
                    // (8,33): error CS8412: Asynchronous foreach requires that the return type 'C.Enumerator1' of 'C.GetAsyncEnumerator()' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator1", "C.GetAsyncEnumerator()").WithLocation(8, 33)
                );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionWithInternalInstanceGetAsyncEnumerator()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }

    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }

    internal Enumerator GetAsyncEnumerator() => throw null;
}
internal static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,33): warning CS0279: 'C' does not implement the 'async streams' pattern. 'C.GetAsyncEnumerator()' is not a public instance or extension method.
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_PatternNotPublicOrNotInstance, "new C()").WithArguments("C", "async streams", "C.GetAsyncEnumerator()").WithLocation(8, 33)
                );
            CompileAndVerify(comp, expectedOutput: "123");
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionWithInstanceGetAsyncEnumeratorWithTooManyParameters()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }

    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }

    internal Enumerator GetAsyncEnumerator(int a) => throw null;
}
internal static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionWithStaticGetAsyncEnumeratorDeclaredInType()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }

    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }

    public static Enumerator GetAsyncEnumerator() => throw null;
}
internal static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestAwaitForEachViaExtensionImplicitImplementationOfIAsyncDisposableStruct()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var x in new C())
        {
            Console.Write(x);
        }
    }
}

static class Extensions
{
    public static Enumerator GetAsyncEnumerator(this C _) => new Enumerator();
}

struct Enumerator : IAsyncDisposable
{
    public int Current { get; private set; }
    public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    public ValueTask DisposeAsync() { Console.Write(""Disposed""); return new ValueTask(); }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: @"123Disposed");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123Disposed"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x52 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       83 (0x53)
                  .maxstack  2
                  .locals init (Enumerator V_0,
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_001b
                    IL_000f:  ldloca.s   V_0
                    IL_0011:  call       "readonly int Enumerator.Current.get"
                    IL_0016:  call       "void System.Console.Write(int)"
                    IL_001b:  ldloca.s   V_0
                    IL_001d:  call       "System.Threading.Tasks.Task<bool> Enumerator.MoveNextAsync()"
                    IL_0022:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0027:  brtrue.s   IL_000f
                    IL_0029:  leave.s    IL_002e
                  }
                  catch object
                  {
                    IL_002b:  stloc.1
                    IL_002c:  leave.s    IL_002e
                  }
                  IL_002e:  ldloca.s   V_0
                  IL_0030:  call       "System.Threading.Tasks.ValueTask Enumerator.DisposeAsync()"
                  IL_0035:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_003a:  ldloc.1
                  IL_003b:  brfalse.s  IL_0052
                  IL_003d:  ldloc.1
                  IL_003e:  isinst     "System.Exception"
                  IL_0043:  dup
                  IL_0044:  brtrue.s   IL_0048
                  IL_0046:  ldloc.1
                  IL_0047:  throw
                  IL_0048:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_004d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0052:  ret
                }
                """);
        }

        [Fact]
        public void TestAwaitForEachViaExtensionExplicitlyDisposableStruct()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var x in new C())
        {
            Console.Write(x);
        }
    }
}

static class Extensions
{
    public static Enumerator GetAsyncEnumerator(this C _) => new Enumerator();
}

struct Enumerator : IAsyncDisposable
{
    public int Current { get; private set; }
    public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    ValueTask IAsyncDisposable.DisposeAsync() { Console.Write(""Disposed""); return new ValueTask(); }
}";

            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: @"123Disposed");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123Disposed"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x58 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       89 (0x59)
                  .maxstack  2
                  .locals init (Enumerator V_0,
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_001b
                    IL_000f:  ldloca.s   V_0
                    IL_0011:  call       "readonly int Enumerator.Current.get"
                    IL_0016:  call       "void System.Console.Write(int)"
                    IL_001b:  ldloca.s   V_0
                    IL_001d:  call       "System.Threading.Tasks.Task<bool> Enumerator.MoveNextAsync()"
                    IL_0022:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0027:  brtrue.s   IL_000f
                    IL_0029:  leave.s    IL_002e
                  }
                  catch object
                  {
                    IL_002b:  stloc.1
                    IL_002c:  leave.s    IL_002e
                  }
                  IL_002e:  ldloca.s   V_0
                  IL_0030:  constrained. "Enumerator"
                  IL_0036:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_003b:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0040:  ldloc.1
                  IL_0041:  brfalse.s  IL_0058
                  IL_0043:  ldloc.1
                  IL_0044:  isinst     "System.Exception"
                  IL_0049:  dup
                  IL_004a:  brtrue.s   IL_004e
                  IL_004c:  ldloc.1
                  IL_004d:  throw
                  IL_004e:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0053:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0058:  ret
                }
                """);
        }

        [Fact]
        public void TestAwaitForEachViaExtensionAsyncDisposeStruct()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await foreach (var x in new C())
        {
            Console.Write(x);
        }
    }
}

static class Extensions
{
    public static Enumerator GetAsyncEnumerator(this C _) => new Enumerator();
}

struct Enumerator
{
    public int Current { get; private set; }
    public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    public ValueTask DisposeAsync() { Console.Write(""Disposed""); return new ValueTask(); }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: @"123Disposed");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123Disposed"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x52 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       83 (0x53)
                  .maxstack  2
                  .locals init (Enumerator V_0,
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_001b
                    IL_000f:  ldloca.s   V_0
                    IL_0011:  call       "readonly int Enumerator.Current.get"
                    IL_0016:  call       "void System.Console.Write(int)"
                    IL_001b:  ldloca.s   V_0
                    IL_001d:  call       "System.Threading.Tasks.Task<bool> Enumerator.MoveNextAsync()"
                    IL_0022:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0027:  brtrue.s   IL_000f
                    IL_0029:  leave.s    IL_002e
                  }
                  catch object
                  {
                    IL_002b:  stloc.1
                    IL_002c:  leave.s    IL_002e
                  }
                  IL_002e:  ldloca.s   V_0
                  IL_0030:  call       "System.Threading.Tasks.ValueTask Enumerator.DisposeAsync()"
                  IL_0035:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_003a:  ldloc.1
                  IL_003b:  brfalse.s  IL_0052
                  IL_003d:  ldloc.1
                  IL_003e:  isinst     "System.Exception"
                  IL_0043:  dup
                  IL_0044:  brtrue.s   IL_0048
                  IL_0046:  ldloc.1
                  IL_0047:  throw
                  IL_0048:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_004d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0052:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionsWithTaskLikeTypeMoveNext()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.ValueTask<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestWithObsoletePatternMethodsViaExtension()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        [Obsolete]
        public int Current { get; private set; }
        [Obsolete]
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
        [Obsolete]
        public Task DisposeAsync() { Console.Write(""Disposed""); return Task.CompletedTask; }
    }
}
[Obsolete]
public static class Extensions
{
    [Obsolete]
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,15): warning CS0612: 'C.Enumerator.DisposeAsync()' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.Enumerator.DisposeAsync()").WithLocation(8, 15),
                // (8,15): warning CS0612: 'Extensions.GetAsyncEnumerator(C)' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("Extensions.GetAsyncEnumerator(C)").WithLocation(8, 15),
                // (8,15): warning CS0612: 'C.Enumerator.MoveNextAsync()' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.Enumerator.MoveNextAsync()").WithLocation(8, 15),
                // (8,15): warning CS0612: 'C.Enumerator.Current' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.Enumerator.Current").WithLocation(8, 15)
                );
            CompileAndVerify(comp, expectedOutput: "123Disposed");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123Disposed"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x52 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       83 (0x53)
                  .maxstack  2
                  .locals init (C.Enumerator V_0,
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  ldnull
                  IL_000c:  stloc.1
                  .try
                  {
                    IL_000d:  br.s       IL_001a
                    IL_000f:  ldloc.0
                    IL_0010:  callvirt   "int C.Enumerator.Current.get"
                    IL_0015:  call       "void System.Console.Write(int)"
                    IL_001a:  ldloc.0
                    IL_001b:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                    IL_0020:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                    IL_0025:  brtrue.s   IL_000f
                    IL_0027:  leave.s    IL_002c
                  }
                  catch object
                  {
                    IL_0029:  stloc.1
                    IL_002a:  leave.s    IL_002c
                  }
                  IL_002c:  ldloc.0
                  IL_002d:  brfalse.s  IL_003a
                  IL_002f:  ldloc.0
                  IL_0030:  callvirt   "System.Threading.Tasks.Task C.Enumerator.DisposeAsync()"
                  IL_0035:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
                  IL_003a:  ldloc.1
                  IL_003b:  brfalse.s  IL_0052
                  IL_003d:  ldloc.1
                  IL_003e:  isinst     "System.Exception"
                  IL_0043:  dup
                  IL_0044:  brtrue.s   IL_0048
                  IL_0046:  ldloc.1
                  IL_0047:  throw
                  IL_0048:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_004d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0052:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaImportedExtensions()
        {
            string source = @"
using System;
using System.Threading.Tasks;
using N;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
namespace N
{
    public static class Extensions
    {
        public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
    }
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator N.Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaUnimportedExtensions()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
namespace N
{
    public static class Extensions
    {
        public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
    }
}";
            CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestWithPatternGetAsyncEnumeratorViaExtensionOnUnassignedCollection()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        C c;
        await foreach (var i in c)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,33): error CS0165: Use of unassigned local variable 'c'
                //         await foreach (var i in c)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(9, 33)
                );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaValidExtensionInClosestNamespaceInvalidInFurtherNamespace1()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using N1.N2.N3;

namespace N1
{
    public static class Extensions
    {
        public static int GetAsyncEnumerator(this C self) => throw null;
    }

    namespace N2
    {
        public static class Extensions
        {
            public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
        }

        namespace N3
        {
            using N2;
            public class C
            {
                public static async Task Main()
                {
                    await foreach (var i in new C())
                    {
                        Console.Write(i);
                    }
                }
                public sealed class Enumerator
                {
                    public int Current { get; private set; }
                    public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
                }
            }
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("N1.N2.N3.C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (N1.N2.N3.C.Enumerator V_0)
                  IL_0000:  newobj     "N1.N2.N3.C..ctor()"
                  IL_0005:  call       "N1.N2.N3.C.Enumerator N1.N2.Extensions.GetAsyncEnumerator(N1.N2.N3.C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int N1.N2.N3.C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> N1.N2.N3.C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaValidExtensionInClosestNamespaceInvalidInFurtherNamespace2()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using N1;
using N3;

namespace N1
{
    using N2;
    public class C
    {
        public static async Task Main()
        {
            await foreach (var i in new C())
            {
                Console.Write(i);
            }
        }
        public sealed class Enumerator
        {
            public int Current { get; private set; }
            public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
        }
    }
}

namespace N2
{
    public static class Extensions
    {
        public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
    }
}

namespace N3
{
    public static class Extensions
    {
        public static int GetAsyncEnumerator(this C self) => throw null;
    }
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,1): hidden CS8019: Unnecessary using directive.
                // using N3;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N3;").WithLocation(5, 1));
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("N1.C.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (N1.C.Enumerator V_0)
                  IL_0000:  newobj     "N1.C..ctor()"
                  IL_0005:  call       "N1.C.Enumerator N2.Extensions.GetAsyncEnumerator(N1.C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int N1.C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> N1.C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaInvalidExtensionInClosestNamespaceValidInFurtherNamespace1()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using N1.N2.N3;

namespace N1
{
    public static class Extensions
    {
        public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
    }

    namespace N2
    {
        public static class Extensions
        {
            public static int GetAsyncEnumerator(this C self) => throw null;
        }

        namespace N3
        {
            using N2;
            public class C
            {
                public static async Task Main()
                {
                    await foreach (var i in new C())
                    {
                        Console.Write(i);
                    }
                }
                public sealed class Enumerator
                {
                    public int Current { get; private set; }
                    public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
                }
            }
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (27,45): error CS0117: 'int' does not contain a definition for 'Current'
                    //                     await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("int", "Current").WithLocation(27, 45),
                    // (27,45): error CS8412: Asynchronous foreach requires that the return type 'int' of 'Extensions.GetAsyncEnumerator(C)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                    //                     await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("int", "N1.N2.Extensions.GetAsyncEnumerator(N1.N2.N3.C)").WithLocation(27, 45));
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaInvalidExtensionInClosestNamespaceValidInFurtherNamespace2()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using N1;
using N2;

namespace N1
{
    using N3;
    public class C
    {
        public static async Task Main()
        {
            await foreach (var i in new C())
            {
                Console.Write(i);
            }
        }
        public sealed class Enumerator
        {
            public int Current { get; private set; }
            public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
        }
    }
}

namespace N2
{
    public static class Extensions
    {
        public static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
    }
}

namespace N3
{
    public static class Extensions
    {
        public static int GetAsyncEnumerator(this C self) => throw null;
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (5,1): hidden CS8019: Unnecessary using directive.
                    // using N2;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(5, 1),
                    // (14,37): error CS0117: 'int' does not contain a definition for 'Current'
                    //             await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("int", "Current").WithLocation(14, 37),
                    // (14,37): error CS8412: Asynchronous foreach requires that the return type 'int' of 'Extensions.GetAsyncEnumerator(C)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                    //             await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("int", "N3.Extensions.GetAsyncEnumerator(N1.C)").WithLocation(14, 37));
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAccessiblePrivateExtension()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public static class Program
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }

    private static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}

public class C
{
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("Program.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Program.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAccessiblePrivateExtensionInNestedClass()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public static class Program
{
    public static class Inner
    {
        public static async Task Main()
        {
            await foreach (var i in new C())
            {
                Console.Write(i);
            }
        }
    }

    private static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}

public class C
{
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });
            verifier.VerifyIL("Program.Inner.Main()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "C.Enumerator Program.GetAsyncEnumerator(C)"
                  IL_000a:  stloc.0
                  IL_000b:  br.s       IL_0018
                  IL_000d:  ldloc.0
                  IL_000e:  callvirt   "int C.Enumerator.Current.get"
                  IL_0013:  call       "void System.Console.Write(int)"
                  IL_0018:  ldloc.0
                  IL_0019:  callvirt   "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_001e:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0023:  brtrue.s   IL_000d
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaInaccessiblePrivateExtension()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    private static C.Enumerator GetAsyncEnumerator(this C self) => new C.Enumerator();
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance or extension definition for 'GetAsyncEnumerator'
                    //         await foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
                    );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensionWithRefReturn()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
        
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => Task.FromResult(Current++ != 3);
    }
}
public static class Extensions
{
    public static C.Enumerator Instance = new C.Enumerator();
    public static ref C.Enumerator GetAsyncEnumerator(this C self) => ref Instance;
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123123");

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("123123"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x58 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       89 (0x59)
                  .maxstack  1
                  .locals init (C.Enumerator V_0)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  call       "ref C.Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_000a:  ldobj      "C.Enumerator"
                  IL_000f:  stloc.0
                  IL_0010:  br.s       IL_001e
                  IL_0012:  ldloca.s   V_0
                  IL_0014:  call       "readonly int C.Enumerator.Current.get"
                  IL_0019:  call       "void System.Console.Write(int)"
                  IL_001e:  ldloca.s   V_0
                  IL_0020:  call       "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_0025:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_002a:  brtrue.s   IL_0012
                  IL_002c:  newobj     "C..ctor()"
                  IL_0031:  call       "ref C.Enumerator Extensions.GetAsyncEnumerator(C)"
                  IL_0036:  ldobj      "C.Enumerator"
                  IL_003b:  stloc.0
                  IL_003c:  br.s       IL_004a
                  IL_003e:  ldloca.s   V_0
                  IL_0040:  call       "readonly int C.Enumerator.Current.get"
                  IL_0045:  call       "void System.Console.Write(int)"
                  IL_004a:  ldloca.s   V_0
                  IL_004c:  call       "System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()"
                  IL_0051:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0056:  brtrue.s   IL_003e
                  IL_0058:  ret
                }
                """);
        }

        [Theory, WorkItem(59955, "https://github.com/dotnet/roslyn/issues/59955")]
        [InlineData(true)]
        [InlineData(false)]
        public void DisposePatternPreferredOverIAsyncDisposable(bool withCSharp8)
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class C
{
    public static async Task Main()
    {
        await foreach (var i in new AsyncEnumerable())
        {
        }
    }
}

struct AsyncEnumerable : IAsyncEnumerable<int>
{
    public AsyncEnumerator GetAsyncEnumerator(CancellationToken token = default) => new AsyncEnumerator();
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken token) => throw null;
}

struct AsyncEnumerator : IAsyncEnumerator<int>
{
    public int Current => 0;
    public async ValueTask<bool> MoveNextAsync()
    {
        await Task.Yield();
        return false;
    }
    public async ValueTask DisposeAsync()
    {
        Console.WriteLine(""RAN"");
        await Task.Yield();
    }

    int IAsyncEnumerator<int>.Current => throw null;
    ValueTask<bool> IAsyncEnumerator<int>.MoveNextAsync() => throw null;
    ValueTask IAsyncDisposable.DisposeAsync() => throw null;
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe,
                parseOptions: withCSharp8 ? TestOptions.Regular8 : TestOptions.Regular7_3);

            if (withCSharp8)
            {
                comp.VerifyDiagnostics();
                CompileAndVerify(comp, expectedOutput: "RAN");

                var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
                var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("RAN"), verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [Main]: Return value missing on the stack. { Offset = 0x5b }
                        [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                        [DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                        """
                });
                verifier.VerifyIL("C.Main()", """
                    {
                      // Code size       92 (0x5c)
                      .maxstack  2
                      .locals init (AsyncEnumerator V_0,
                                    AsyncEnumerable V_1,
                                    System.Threading.CancellationToken V_2,
                                    object V_3)
                      IL_0000:  ldloca.s   V_1
                      IL_0002:  dup
                      IL_0003:  initobj    "AsyncEnumerable"
                      IL_0009:  ldloca.s   V_2
                      IL_000b:  initobj    "System.Threading.CancellationToken"
                      IL_0011:  ldloc.2
                      IL_0012:  call       "AsyncEnumerator AsyncEnumerable.GetAsyncEnumerator(System.Threading.CancellationToken)"
                      IL_0017:  stloc.0
                      IL_0018:  ldnull
                      IL_0019:  stloc.3
                      .try
                      {
                        IL_001a:  br.s       IL_0024
                        IL_001c:  ldloca.s   V_0
                        IL_001e:  call       "int AsyncEnumerator.Current.get"
                        IL_0023:  pop
                        IL_0024:  ldloca.s   V_0
                        IL_0026:  call       "System.Threading.Tasks.ValueTask<bool> AsyncEnumerator.MoveNextAsync()"
                        IL_002b:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                        IL_0030:  brtrue.s   IL_001c
                        IL_0032:  leave.s    IL_0037
                      }
                      catch object
                      {
                        IL_0034:  stloc.3
                        IL_0035:  leave.s    IL_0037
                      }
                      IL_0037:  ldloca.s   V_0
                      IL_0039:  call       "System.Threading.Tasks.ValueTask AsyncEnumerator.DisposeAsync()"
                      IL_003e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                      IL_0043:  ldloc.3
                      IL_0044:  brfalse.s  IL_005b
                      IL_0046:  ldloc.3
                      IL_0047:  isinst     "System.Exception"
                      IL_004c:  dup
                      IL_004d:  brtrue.s   IL_0051
                      IL_004f:  ldloc.3
                      IL_0050:  throw
                      IL_0051:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                      IL_0056:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                      IL_005b:  ret
                    }
                    """);
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (11,9): error CS8370: Feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //         await foreach (var i in new AsyncEnumerable())
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "await").WithArguments("async streams", "8.0").WithLocation(11, 9)
                    );
            }

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.True(info.IsAsynchronous);
            Assert.Equal("AsyncEnumerator AsyncEnumerable.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> AsyncEnumerator.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 AsyncEnumerator.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask AsyncEnumerator.DisposeAsync()",
                info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
        }

        [Theory, WorkItem(59955, "https://github.com/dotnet/roslyn/issues/59955")]
        [InlineData(true)]
        [InlineData(false)]
        public void DisposePatternPreferredOverIAsyncDisposable_NoIAsyncEnumerable(bool withCSharp8)
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class C
{
    public static async Task Main()
    {
        await foreach (var i in new AsyncEnumerable())
        {
        }
    }
}

struct AsyncEnumerable
{
    public AsyncEnumerator GetAsyncEnumerator(CancellationToken token = default) => new AsyncEnumerator();
}

struct AsyncEnumerator : IAsyncDisposable
{
    public int Current => 0;
    public async ValueTask<bool> MoveNextAsync()
    {
        await Task.Yield();
        return false;
    }
    public async ValueTask DisposeAsync()
    {
        Console.WriteLine(""RAN"");
        await Task.Yield();
    }

    ValueTask IAsyncDisposable.DisposeAsync() => throw null;
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe,
                parseOptions: withCSharp8 ? TestOptions.Regular8 : TestOptions.Regular7_3);

            if (withCSharp8)
            {
                comp.VerifyDiagnostics();
                CompileAndVerify(comp, expectedOutput: "RAN");

                var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
                var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("RAN"), verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [Main]: Return value missing on the stack. { Offset = 0x5b }
                        [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                        [DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                        """
                });
                verifier.VerifyIL("C.Main()", """
                    {
                      // Code size       92 (0x5c)
                      .maxstack  2
                      .locals init (AsyncEnumerator V_0,
                                    AsyncEnumerable V_1,
                                    System.Threading.CancellationToken V_2,
                                    object V_3)
                      IL_0000:  ldloca.s   V_1
                      IL_0002:  dup
                      IL_0003:  initobj    "AsyncEnumerable"
                      IL_0009:  ldloca.s   V_2
                      IL_000b:  initobj    "System.Threading.CancellationToken"
                      IL_0011:  ldloc.2
                      IL_0012:  call       "AsyncEnumerator AsyncEnumerable.GetAsyncEnumerator(System.Threading.CancellationToken)"
                      IL_0017:  stloc.0
                      IL_0018:  ldnull
                      IL_0019:  stloc.3
                      .try
                      {
                        IL_001a:  br.s       IL_0024
                        IL_001c:  ldloca.s   V_0
                        IL_001e:  call       "int AsyncEnumerator.Current.get"
                        IL_0023:  pop
                        IL_0024:  ldloca.s   V_0
                        IL_0026:  call       "System.Threading.Tasks.ValueTask<bool> AsyncEnumerator.MoveNextAsync()"
                        IL_002b:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                        IL_0030:  brtrue.s   IL_001c
                        IL_0032:  leave.s    IL_0037
                      }
                      catch object
                      {
                        IL_0034:  stloc.3
                        IL_0035:  leave.s    IL_0037
                      }
                      IL_0037:  ldloca.s   V_0
                      IL_0039:  call       "System.Threading.Tasks.ValueTask AsyncEnumerator.DisposeAsync()"
                      IL_003e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                      IL_0043:  ldloc.3
                      IL_0044:  brfalse.s  IL_005b
                      IL_0046:  ldloc.3
                      IL_0047:  isinst     "System.Exception"
                      IL_004c:  dup
                      IL_004d:  brtrue.s   IL_0051
                      IL_004f:  ldloc.3
                      IL_0050:  throw
                      IL_0051:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                      IL_0056:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                      IL_005b:  ret
                    }
                    """);
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (10,9): error CS8370: Feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //         await foreach (var i in new AsyncEnumerable())
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "await").WithArguments("async streams", "8.0").WithLocation(10, 9)
                    );
            }

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.True(info.IsAsynchronous);
            Assert.Equal("AsyncEnumerator AsyncEnumerable.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> AsyncEnumerator.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 AsyncEnumerator.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask AsyncEnumerator.DisposeAsync()",
                info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
        }

        [Theory, WorkItem(59955, "https://github.com/dotnet/roslyn/issues/59955")]
        [InlineData(true)]
        [InlineData(false)]
        public void AsyncEnumerationViaInterfaceUsesIAsyncDisposable(bool withCSharp8)
        {
            // The enumerator type is IAsyncEnumerator<int> so disposal uses IAsyncDisposable.DisposeAsync()
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class C
{
    public static async Task Main()
    {
        await foreach (var i in new AsyncEnumerable())
        {
        }
    }
}

struct AsyncEnumerable : IAsyncEnumerable<int>
{
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken token) => new AsyncEnumerator();
}

struct AsyncEnumerator : IAsyncEnumerator<int>
{
    public ValueTask DisposeAsync() => throw null;

    int IAsyncEnumerator<int>.Current => 0;
    async ValueTask<bool> IAsyncEnumerator<int>.MoveNextAsync()
    {
        await Task.Yield();
        return false;
    }
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        Console.WriteLine(""RAN"");
        await Task.Yield();
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe,
                parseOptions: withCSharp8 ? TestOptions.Regular8 : TestOptions.Regular7_3);

            if (withCSharp8)
            {
                comp.VerifyDiagnostics();
                CompileAndVerify(comp, expectedOutput: "RAN");

                var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
                var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("RAN"), verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [Main]: Return value missing on the stack. { Offset = 0x61 }
                        [System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                        [System.IAsyncDisposable.DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                        """
                });
                verifier.VerifyIL("C.Main()", """
                    {
                      // Code size       98 (0x62)
                      .maxstack  2
                      .locals init (System.Collections.Generic.IAsyncEnumerator<int> V_0,
                                    AsyncEnumerable V_1,
                                    System.Threading.CancellationToken V_2,
                                    object V_3)
                      IL_0000:  ldloca.s   V_1
                      IL_0002:  dup
                      IL_0003:  initobj    "AsyncEnumerable"
                      IL_0009:  ldloca.s   V_2
                      IL_000b:  initobj    "System.Threading.CancellationToken"
                      IL_0011:  ldloc.2
                      IL_0012:  constrained. "AsyncEnumerable"
                      IL_0018:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                      IL_001d:  stloc.0
                      IL_001e:  ldnull
                      IL_001f:  stloc.3
                      .try
                      {
                        IL_0020:  br.s       IL_0029
                        IL_0022:  ldloc.0
                        IL_0023:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
                        IL_0028:  pop
                        IL_0029:  ldloc.0
                        IL_002a:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
                        IL_002f:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                        IL_0034:  brtrue.s   IL_0022
                        IL_0036:  leave.s    IL_003b
                      }
                      catch object
                      {
                        IL_0038:  stloc.3
                        IL_0039:  leave.s    IL_003b
                      }
                      IL_003b:  ldloc.0
                      IL_003c:  brfalse.s  IL_0049
                      IL_003e:  ldloc.0
                      IL_003f:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                      IL_0044:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                      IL_0049:  ldloc.3
                      IL_004a:  brfalse.s  IL_0061
                      IL_004c:  ldloc.3
                      IL_004d:  isinst     "System.Exception"
                      IL_0052:  dup
                      IL_0053:  brtrue.s   IL_0057
                      IL_0055:  ldloc.3
                      IL_0056:  throw
                      IL_0057:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                      IL_005c:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                      IL_0061:  ret
                    }
                    """);
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (11,9): error CS8370: Feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //         await foreach (var i in new AsyncEnumerable())
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "await").WithArguments("async streams", "8.0").WithLocation(11, 9)
                    );
            }

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.True(info.IsAsynchronous);
            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
               info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()",
                info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
        }

        [Fact, WorkItem(59955, "https://github.com/dotnet/roslyn/issues/59955")]
        public void EnumerationViaInterfaceUsesIDisposable()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class C
{
    public static void Main()
    {
        foreach (var i in new Enumerable())
        {
        }
    }
}

struct Enumerable : IEnumerable<int>
{
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => new Enumerator();
}

struct Enumerator : IEnumerator<int>
{
    public void Dispose() => throw null;

    int IEnumerator<int>.Current => 0;
    object IEnumerator.Current => throw null;
    void IEnumerator.Reset() => throw null;
    bool IEnumerator.MoveNext() => false;

    void IDisposable.Dispose()
    {
        Console.WriteLine(""RAN"");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "RAN");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("void System.IDisposable.Dispose()", info.DisposeMethod.ToTestDisplayString());
        }

        [Theory, WorkItem(59955, "https://github.com/dotnet/roslyn/issues/59955")]
        [InlineData(true)]
        [InlineData(false)]
        public void DisposePatternPreferredOverIAsyncDisposable_Deconstruction(bool withCSharp8)
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class C
{
    public static async Task Main()
    {
        await foreach (var (i, j) in new AsyncEnumerable())
        {
        }
    }
}

struct AsyncEnumerable : IAsyncEnumerable<(int, int)>
{
    public AsyncEnumerator GetAsyncEnumerator(CancellationToken token = default) => new AsyncEnumerator();
    IAsyncEnumerator<(int, int)> IAsyncEnumerable<(int, int)>.GetAsyncEnumerator(CancellationToken token) => throw null;
}

struct AsyncEnumerator : IAsyncEnumerator<(int, int)>
{
    public (int, int) Current => (0, 0);
    public async ValueTask<bool> MoveNextAsync()
    {
        await Task.Yield();
        return false;
    }
    public async ValueTask DisposeAsync()
    {
        Console.WriteLine(""RAN"");
        await Task.Yield();
    }

    (int, int) IAsyncEnumerator<(int, int)>.Current => throw null;
    ValueTask<bool> IAsyncEnumerator<(int, int)>.MoveNextAsync() => throw null;
    ValueTask IAsyncDisposable.DisposeAsync() => throw null;
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe,
                parseOptions: withCSharp8 ? TestOptions.Regular8 : TestOptions.Regular7_3);

            if (withCSharp8)
            {
                comp.VerifyDiagnostics();
                CompileAndVerify(comp, expectedOutput: "RAN");

                var runtimeAsyncComp = CreateRuntimeAsyncCompilation(source, options: TestOptions.ReleaseExe);
                var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("RAN"), verify: Verification.Fails with
                {
                    ILVerifyMessage = """
                        [Main]: Return value missing on the stack. { Offset = 0x5b }
                        [MoveNextAsync]: Unexpected type on the stack. { Offset = 0x25, Found = Int32, Expected = value '[System.Runtime]System.Threading.Tasks.ValueTask`1<bool>' }
                        [DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                        """
                });
                verifier.VerifyIL("C.Main()", """
                    {
                      // Code size       92 (0x5c)
                      .maxstack  2
                      .locals init (AsyncEnumerator V_0,
                                    AsyncEnumerable V_1,
                                    System.Threading.CancellationToken V_2,
                                    object V_3)
                      IL_0000:  ldloca.s   V_1
                      IL_0002:  dup
                      IL_0003:  initobj    "AsyncEnumerable"
                      IL_0009:  ldloca.s   V_2
                      IL_000b:  initobj    "System.Threading.CancellationToken"
                      IL_0011:  ldloc.2
                      IL_0012:  call       "AsyncEnumerator AsyncEnumerable.GetAsyncEnumerator(System.Threading.CancellationToken)"
                      IL_0017:  stloc.0
                      IL_0018:  ldnull
                      IL_0019:  stloc.3
                      .try
                      {
                        IL_001a:  br.s       IL_0024
                        IL_001c:  ldloca.s   V_0
                        IL_001e:  call       "System.ValueTuple<int, int> AsyncEnumerator.Current.get"
                        IL_0023:  pop
                        IL_0024:  ldloca.s   V_0
                        IL_0026:  call       "System.Threading.Tasks.ValueTask<bool> AsyncEnumerator.MoveNextAsync()"
                        IL_002b:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                        IL_0030:  brtrue.s   IL_001c
                        IL_0032:  leave.s    IL_0037
                      }
                      catch object
                      {
                        IL_0034:  stloc.3
                        IL_0035:  leave.s    IL_0037
                      }
                      IL_0037:  ldloca.s   V_0
                      IL_0039:  call       "System.Threading.Tasks.ValueTask AsyncEnumerator.DisposeAsync()"
                      IL_003e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                      IL_0043:  ldloc.3
                      IL_0044:  brfalse.s  IL_005b
                      IL_0046:  ldloc.3
                      IL_0047:  isinst     "System.Exception"
                      IL_004c:  dup
                      IL_004d:  brtrue.s   IL_0051
                      IL_004f:  ldloc.3
                      IL_0050:  throw
                      IL_0051:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                      IL_0056:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                      IL_005b:  ret
                    }
                    """);
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (11,9): error CS8370: Feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //         await foreach (var i in new AsyncEnumerable())
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "await").WithArguments("async streams", "8.0").WithLocation(11, 9)
                    );
            }

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.True(info.IsAsynchronous);
            Assert.Equal("AsyncEnumerator AsyncEnumerable.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> AsyncEnumerator.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32) AsyncEnumerator.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask AsyncEnumerator.DisposeAsync()",
                info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)", info.ElementType.ToTestDisplayString());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72819")]
        public void PatternBasedFails_AwaitForeach()
        {
            var src = """
using System;
using System.Threading;
using System.Threading.Tasks;

interface ICustomEnumerator
{
    public int Current {get;}

    public ValueTask<bool> MoveNextAsync();
}

interface IGetEnumerator<TEnumerator> where TEnumerator : ICustomEnumerator
{
    TEnumerator GetAsyncEnumerator(CancellationToken token = default);
}

struct S1 : IGetEnumerator<S2>
{
    public S2 GetAsyncEnumerator(CancellationToken token = default)
    {
        return new S2();
    }
}

interface IMyAsyncDisposable1
{
    ValueTask DisposeAsync();
}

interface IMyAsyncDisposable2
{
    ValueTask DisposeAsync();
}

struct S2 : ICustomEnumerator, IMyAsyncDisposable1, IMyAsyncDisposable2, IAsyncDisposable
{
    ValueTask IMyAsyncDisposable1.DisposeAsync() => throw null;
    ValueTask IMyAsyncDisposable2.DisposeAsync() => throw null;
    public ValueTask DisposeAsync()
    { 
        System.Console.Write("D");
        return ValueTask.CompletedTask;
    }

    public int Current => throw null;
    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(false);
    }
}

class C
{
    static async Task Main()
    {
        await Test<S1, S2>();
    }

    static async Task Test<TEnumerable, TEnumerator>()
        where TEnumerable : IGetEnumerator<TEnumerator>
        where TEnumerator : ICustomEnumerator, IMyAsyncDisposable1, IMyAsyncDisposable2, IAsyncDisposable
    {
        await foreach (var i in default(TEnumerable))
        {
            System.Console.Write(i);
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var expectedOutput = ExecutionConditionUtil.IsMonoOrCoreClr ? "D" : null;
            CompileAndVerify(comp,
                expectedOutput: expectedOutput,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();

            var runtimeAsyncComp = CreateRuntimeAsyncCompilation(src, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(runtimeAsyncComp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xa }
                    [Test]: Return value missing on the stack. { Offset = 0x7f }
                    """
            });
            verifier.VerifyIL("C.Test<TEnumerable, TEnumerator>()", """
                {
                  // Code size      128 (0x80)
                  .maxstack  2
                  .locals init (TEnumerator V_0,
                                TEnumerable V_1,
                                System.Threading.CancellationToken V_2,
                                object V_3)
                  IL_0000:  ldloca.s   V_1
                  IL_0002:  dup
                  IL_0003:  initobj    "TEnumerable"
                  IL_0009:  ldloca.s   V_2
                  IL_000b:  initobj    "System.Threading.CancellationToken"
                  IL_0011:  ldloc.2
                  IL_0012:  constrained. "TEnumerable"
                  IL_0018:  callvirt   "TEnumerator IGetEnumerator<TEnumerator>.GetAsyncEnumerator(System.Threading.CancellationToken)"
                  IL_001d:  stloc.0
                  IL_001e:  ldnull
                  IL_001f:  stloc.3
                  .try
                  {
                    IL_0020:  br.s       IL_0034
                    IL_0022:  ldloca.s   V_0
                    IL_0024:  constrained. "TEnumerator"
                    IL_002a:  callvirt   "int ICustomEnumerator.Current.get"
                    IL_002f:  call       "void System.Console.Write(int)"
                    IL_0034:  ldloca.s   V_0
                    IL_0036:  constrained. "TEnumerator"
                    IL_003c:  callvirt   "System.Threading.Tasks.ValueTask<bool> ICustomEnumerator.MoveNextAsync()"
                    IL_0041:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.ValueTask<bool>)"
                    IL_0046:  brtrue.s   IL_0022
                    IL_0048:  leave.s    IL_004d
                  }
                  catch object
                  {
                    IL_004a:  stloc.3
                    IL_004b:  leave.s    IL_004d
                  }
                  IL_004d:  ldloc.0
                  IL_004e:  box        "TEnumerator"
                  IL_0053:  brfalse.s  IL_0067
                  IL_0055:  ldloca.s   V_0
                  IL_0057:  constrained. "TEnumerator"
                  IL_005d:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0062:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0067:  ldloc.3
                  IL_0068:  brfalse.s  IL_007f
                  IL_006a:  ldloc.3
                  IL_006b:  isinst     "System.Exception"
                  IL_0070:  dup
                  IL_0071:  brtrue.s   IL_0075
                  IL_0073:  ldloc.3
                  IL_0074:  throw
                  IL_0075:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_007a:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_007f:  ret
                }
                """);
        }
    }
}
