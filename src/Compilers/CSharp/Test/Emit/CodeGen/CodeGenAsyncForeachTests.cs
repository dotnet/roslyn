// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.AsyncStreams)]
    public class CodeGenAsyncForeachTests : EmitMetadataTestBase
    {
        public CodeGenAsyncForeachTests()
        {
        }

        private static readonly string s_interfaces = @"
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator();
    }

    public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask<bool> WaitForNextAsync();
        T TryGetNext(out bool success);
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

        [Fact]
        public void TestWithCSharp7_3()
        {
            string source = @"
using System.Collections.Generic;
class C : IAsyncEnumerable<int>
{
    public static async System.Threading.Tasks.Task Main()
    {
        foreach await (int i in new C())
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
        => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (7,17): error CS8370: Feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         foreach await (int i in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "await").WithArguments("async streams", "8.0").WithLocation(7, 17)
                );
        }

        [Fact]
        public void TestWithMissingValueTask()
        {
            string lib_cs = @"
using System.Collections.Generic;
public class C : IAsyncEnumerable<int>
{
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
        => throw null;
}";

            var lib = CreateCompilationWithTasksExtensions(new[] { lib_cs, s_interfaces });
            lib.VerifyDiagnostics();

            string source = @"
class D
{
    public static async System.Threading.Tasks.Task Main()
    {
        foreach await (int i in new C()) { }
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(source, references: new[] { lib.EmitToImageReference() });
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_ValueTask);
            comp.VerifyDiagnostics(
                // (6,9): error CS0518: Predefined type 'System.Threading.Tasks.ValueTask' is not defined or imported
                //         foreach await (int i in new C()) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "foreach await (int i in new C()) { }").WithArguments("System.Threading.Tasks.ValueTask").WithLocation(6, 9)
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
                foreach await (int i in new C()) { System.Console.Write($""0x{i:X8}""); }
            }
        }
        catch (System.OverflowException)
        {
            System.Console.Write(""overflow"");
        }
    }

    public IAsyncEnumerator<uint> GetAsyncEnumerator()
        => new AsyncEnumerator();
    public sealed class AsyncEnumerator : IAsyncEnumerator<uint>
    {
        bool firstWait = true;
        bool firstValue = true;
        public uint TryGetNext(out bool found)
        {
            found = firstValue;
            firstValue = false;
            return 0xFFFFFFFF;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            await Task.Delay(10);
            bool result = firstWait;
            firstWait = false;
            return result;
        }
        public async ValueTask DisposeAsync()
        {
            await Task.Delay(10);
        }
    }
}
";
            var comp_checked = CreateCompilationWithTasksExtensions(new[] { source.Replace("REPLACE", "checked"), s_interfaces }, options: TestOptions.DebugExe);
            comp_checked.VerifyDiagnostics();
            CompileAndVerify(comp_checked, expectedOutput: "overflow");

            var comp_unchecked = CreateCompilationWithTasksExtensions(new[] { source.Replace("REPLACE", "unchecked"), s_interfaces }, options: TestOptions.DebugExe);
            comp_unchecked.VerifyDiagnostics();
            CompileAndVerify(comp_unchecked, expectedOutput: "0xFFFFFFFF");
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
        foreach await (int i in new C())
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
        => throw null;
    IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator()
        => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
            comp.VerifyDiagnostics(
                // (7,33): error CS9003: Async foreach statement cannot operate on variables of type 'C' because it implements multiple instantiations of 'IAsyncEnumerable<T>'; try casting to a specific interface instantiation
                //         foreach await (int i in new C())
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
        foreach await (var i in new C())
        {
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9001: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetAsyncEnumerator'
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
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
        foreach await (var i in new C())
        {
        }
    }
    public static Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9001: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetAsyncEnumerator'
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
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
        foreach await (var i in new C())
        {
        }
    }
    internal Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): warning CS0279: 'C' does not implement the 'async streams' pattern. 'C.GetAsyncEnumerator()' is either static or not public.
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.WRN_PatternStaticOrInaccessible, "new C()").WithArguments("C", "async streams", "C.GetAsyncEnumerator()").WithLocation(6, 33),
                // (6,33): error CS9001: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetAsyncEnumerator'
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
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
        foreach await (var i in new C())
        {
        }
    }
    [System.Obsolete]
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        [System.Obsolete]
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        [System.Obsolete]
        public int TryGetNext(out bool success)
        {
            throw null;
        }
     }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,9): warning CS0612: 'C.GetAsyncEnumerator()' is obsolete
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.GetAsyncEnumerator()").WithLocation(6, 9),
                // (6,9): warning CS0612: 'C.Enumerator.WaitForNextAsync()' is obsolete
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.Enumerator.WaitForNextAsync()").WithLocation(6, 9),
                // (6,9): warning CS0612: 'C.Enumerator.TryGetNext(out bool)' is obsolete
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.Enumerator.TryGetNext(out bool)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void TestWithStaticWaitForNextAsync()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public static System.Threading.Tasks.Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        public int TryGetNext(out bool success)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithStaticTryGetNext()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        public static int TryGetNext(out bool success)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithNonPublicWaitForNextAsync()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        internal System.Threading.Tasks.Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        public int TryGetNext(out bool success)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWithNonPublicTryGetNext()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        private int TryGetNext(out bool success)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS0122: 'C.Enumerator.TryGetNext(out bool)' is inaccessible due to its protection level
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadAccess, "new C()").WithArguments("C.Enumerator.TryGetNext(out bool)").WithLocation(6, 33),
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWaitForNextAsync_ReturnsTask()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task WaitForNextAsync()
        {
            throw null;
        }
        public int TryGetNext(out bool success)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWaitForNextAsync_ReturnsTaskOfInt()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<int> WaitForNextAsync()
        {
            throw null;
        }
        public int TryGetNext(out bool success)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestWaitForNextAsync_WithOptionalParameter()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync(int bad = 0)
        {
            throw null;
        }
        public int TryGetNext(out bool success)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestTryGetNext_WithOptionalParameter()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        public int TryGetNext(out bool success, int bad = 0)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestTryGetNext_WithByValParameter()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        public int TryGetNext(bool success)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestTryGetNext_WithIntParameter()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        public int TryGetNext(out int success)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestTryGetNext_WithVoidReturn()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        public void TryGetNext(out bool success)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
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
        foreach await (string i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
        => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
            => throw null;
        public int TryGetNext(out bool success)
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,9): error CS0030: Cannot convert type 'int' to 'string'
                //         foreach await (string i in new C())
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("int", "string").WithLocation(6, 9)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("C.Enumerator C.GetAsyncEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> C.Enumerator.WaitForNextAsync()", info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Enumerator.TryGetNext(out System.Boolean success)", info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
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
        foreach await (Element i in new C())
        {
        }
    }
    public AsyncEnumerator GetAsyncEnumerator()
            => throw null;
    public sealed class AsyncEnumerator
    {
        public int TryGetNext(out bool found)
            => throw null;
        public Task<bool> WaitForNextAsync()
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
                // (7,9): error CS0030: Cannot convert type 'int' to 'Element'
                //         foreach await (Element i in new C())
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("int", "Element").WithLocation(7, 9)
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
        foreach await (Element i in new C())
        {
            Write($""Got({i}) "");
        }
    }
    public AsyncEnumerator GetAsyncEnumerator()
    {
        return new AsyncEnumerator();
    }
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        int i = 0;
        public int TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            return found ? i++ : 0;
        }
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Delay(10);
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics( );
            // https://github.com/dotnet/roslyn/issues/30258 Convert(0) is here because we're converting the result even if TryGetNext returned false
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Convert(11) Got(11) Next(12) Convert(12) Got(12) " +
                "Next(13) Convert(0) NextAsync(13) Next(24) Convert(24) Got(24) Next(25) Convert(25) Got(25) " +
                "Next(26) Convert(0) NextAsync(26) Dispose(37)", verify: Verification.Skipped);
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
        foreach await (var i in new C())
        {
            Write($""Got({i}) "");
            if (f == null) f = () => Write($""Captured({i})"");
        }
        f();
    }
    public AsyncEnumerator GetAsyncEnumerator()
    {
        return new AsyncEnumerator();
    }
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        int i = 0;
        public int TryGetNext(out bool found)
        {
            found = i % 10 % 3 != 0;
            return found ? i++ : 0;
        }
        public async Task<bool> WaitForNextAsync()
        {
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Got(11) Got(12) Got(24) Got(25) Captured(11)",
                verify: Verification.Skipped);
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
        foreach await (var i in new C<IntContainer>())
        {
            Write($""Got({i.Value}) "");
        }
    }
}
class C<T> where T : IntContainer, new()
{
    public AsyncEnumerator GetAsyncEnumerator()
    {
        return new AsyncEnumerator();
    }
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        int i = 0;
        public T TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            var result = new T();
            ((IntContainer)result).Value = found ? i++ : 0;
            return result;
        }
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11) Next(12) Got(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Got(25) Next(26) NextAsync(26) Dispose(37) ",
                verify: Verification.Skipped);
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
            foreach await (var i in new C())
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
        => throw new System.ArgumentException(""exception"");
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        public int TryGetNext(out bool found)
            => throw null;
        public Task<bool> WaitForNextAsync()
            => throw null;
        public ValueTask DisposeAsync()
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "exception", verify: Verification.Skipped);
        }

        [Fact]
        public void TestWithThrowingWaitForNextAsync()
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
            foreach await (var i in new C())
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
        public int TryGetNext(out bool found)
            => throw null;
        public Task<bool> WaitForNextAsync()
            => throw new System.ArgumentException(""exception"");
        public async ValueTask DisposeAsync()
        {
            Write(""dispose "");
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "dispose exception", verify: Verification.Skipped);
        }

        [Fact]
        public void TestWithThrowingTryGetNext()
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
            foreach await (var i in new C())
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
        public int TryGetNext(out bool found)
            => throw new System.ArgumentException(""exception"");
        public async Task<bool> WaitForNextAsync()
        {
            Write(""wait "");
            await Task.Delay(10);
            return true;
        }
        public async ValueTask DisposeAsync()
        {
            Write(""dispose "");
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "wait dispose exception", verify: Verification.Skipped);
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
            foreach await (var i in new C())
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
        public int TryGetNext(out bool found)
            => throw null;
        public async Task<bool> WaitForNextAsync()
        {
            Write(""wait "");
            await Task.Delay(10);
            return false;
        }
        public ValueTask DisposeAsync()
            => throw new System.ArgumentException(""exception"");
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "wait exception", verify: Verification.Skipped);
        }

        [Fact]
        public void TestWithDynamicCollection()
        {
            string source = @"
class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        foreach await (var i in (dynamic)new C())
        {
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
            comp.VerifyDiagnostics(
                // (6,33): error CS9006: Cannot use a collection of dynamic type in an asynchronous foreach
                //         foreach await (var i in (dynamic)new C())
                Diagnostic(ErrorCode.ERR_BadDynamicAsyncForEach, "(dynamic)new C()").WithLocation(6, 33));
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
        foreach await (var i in collection)
        {
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (12,33): error CS9001: Async foreach statement cannot operate on variables of type 'IAsyncEnumerable<int>' because 'IAsyncEnumerable<int>' does not contain a public definition for 'GetEnumerator'
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMember, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerable<int>", "GetEnumerator").WithLocation(12, 33)
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
        IAsyncEnumerator<T> GetAsyncEnumerator();
    }

    public interface IAsyncEnumerator<out T>
    {
        System.Threading.Tasks.Task<bool> WaitForNextAsync();
    }
}
class C
{
    async System.Threading.Tasks.Task M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        foreach await (var i in collection)
        {
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (18,33): error CS0117: 'IAsyncEnumerator<int>' does not contain a definition for 'TryGetNext'
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_NoSuchMember, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "TryGetNext").WithLocation(18, 33),
                // (18,33): error CS9002: Async foreach requires that the return type 'IAsyncEnumerator<int>' of 'IAsyncEnumerable<int>.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator()").WithLocation(18, 33)
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
        IAsyncEnumerator<T> GetAsyncEnumerator();
    }

    public interface IAsyncEnumerator<out T>
    {
        T TryGetNext(out bool success);
    }
}
class C
{
    async System.Threading.Tasks.Task M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        foreach await (var i in collection)
        {
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (18,33): error CS0117: 'IAsyncEnumerator<int>' does not contain a definition for 'WaitForNextAsync'
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_NoSuchMember, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "WaitForNextAsync").WithLocation(18, 33),
                // (18,33): error CS9002: Async foreach requires that the return type 'IAsyncEnumerator<int>' of 'IAsyncEnumerable<int>.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerator<int>", "System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator()").WithLocation(18, 33)
                );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaExtensions()
        {
            string source = @"
public class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public sealed class Enumerator
    {
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C c)
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS9001: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetAsyncEnumerator'
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensions()
        {
            string source = @"
public class C
{
    void M()
    {
        foreach (var i in new C())
        {
        }
    }
    public sealed class Enumerator
    {
        public int Current => throw null;
        public void MoveNext() => throw null;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self) => throw null;
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetEnumerator'
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(6, 27)
                );
        }

        [Fact]
        public void TestWaitForNextAsyncPatternViaExtensions()
        {
            string source = @"
public class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public int TryGetNext(out bool success)
        {
            throw null;
        }
    }
}
public static class Extensions
{
    public static System.Threading.Tasks.Task<bool> WaitForNextAsync(this C.Enumerator e)
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS0117: 'C.Enumerator' does not contain a definition for 'WaitForNextAsync'
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator", "WaitForNextAsync").WithLocation(6, 33),
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestTryGetNextPatternViaExtensions()
        {
            string source = @"
public class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
        {
            throw null;
        }
    }
}
public static class Extensions
{
    public static int TryGetNext(this C.Enumerator e, out bool success)
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,33): error CS0117: 'C.Enumerator' does not contain a definition for 'TryGetNext'
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator", "TryGetNext").WithLocation(6, 33),
                // (6,33): error CS9002: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestMoveNextPatternViaExtensions()
        {
            string source = @"
public class C
{
    void M()
    {
        foreach (var i in new C())
        {
        }
    }
    public Enumerator GetEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public int Current => throw null;
    }
}
public static class Extensions
{
    public static void MoveNext(this C.Enumerator e)
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,27): error CS0117: 'C.Enumerator' does not contain a definition for 'MoveNext'
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator", "MoveNext").WithLocation(6, 27),
                // (6,27): error CS0202: foreach requires that the return type 'C.Enumerator' of 'C.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetEnumerator()").WithLocation(6, 27)
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
        foreach await (var i in new C())
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
                // (6,33): error CS9001: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetAsyncEnumerator'
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
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
    public Enumerator GetAsyncEnumerator() => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync() => throw null;
        public int TryGetNext(out bool success) => throw null;
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,27): error CS9004: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'foreach await'?
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,27): error CS9004: foreach statement cannot operate on variables of type 'IAsyncEnumerable<int>' because 'IAsyncEnumerable<int>' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'foreach await'?
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
        foreach await (var i in collection)
        {
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,33): error CS9005: Async foreach statement cannot operate on variables of type 'IEnumerable<int>' because 'IEnumerable<int>' does not contain a public instance definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'foreach await'?
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMemberWrongAsync, "collection").WithArguments("System.Collections.Generic.IEnumerable<int>", "GetAsyncEnumerator").WithLocation(7, 33),
                // (7,17): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(7, 17)
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
    public Enumerator GetAsyncEnumerator()
        => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
            => throw null;
        public int TryGetNext(out bool success)
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,27): error CS9004: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'foreach await'?
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_ForEachMissingMemberWrongAsync, "new C()").WithArguments("C", "GetEnumerator").WithLocation(6, 27)
                );
        }

        [Fact]
        public void TestPatternBasedEnumerableWithAsyncForeach()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,33): error CS9005: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'foreach await'?
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMemberWrongAsync, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
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
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        public int TryGetNext(out bool success)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("C.Enumerator C.GetAsyncEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> C.Enumerator.WaitForNextAsync()", info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Enumerator.TryGetNext(out System.Boolean success)", info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Null(info.DisposeMethod);
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.False(internalInfo.NeedsDisposeMethod);
        }

        [Fact]
        public void TestWithPattern_Ref()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (ref var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
            => throw null;
        public int TryGetNext(out bool success)
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,32): error CS8177: Async methods cannot have by-reference locals
                //         foreach await (ref var i in new C())
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "i").WithLocation(6, 32));

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            Assert.Equal(default, model.GetForEachStatementInfo(foreachSyntax));
        }

        [Fact]
        public void TestWithPattern_PointerType()
        {
            string source = @"
unsafe class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
            => throw null;
        public int* TryGetNext(out bool success)
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (6,17): error CS4004: Cannot await in an unsafe context
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await").WithLocation(6, 17));

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
        foreach await (var i in new D())
        {
        }
    }
}
class D
{
    private Enumerator GetAsyncEnumerator()
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
            => throw null;
        public int TryGetNext(out bool success)
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,33): error CS9001: Async foreach statement cannot operate on variables of type 'D' because 'D' does not contain a public definition for 'GetAsyncEnumerator'
                //         foreach await (var i in new D())
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMember, "new D()").WithArguments("D", "GetAsyncEnumerator").WithLocation(6, 33)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            Assert.Equal(default, model.GetForEachStatementInfo(foreachSyntax));
        }

        [Fact]
        public void TestWithPattern_InaccessibleWaitForNextAsync()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new D())
        {
        }
    }
}
class D
{
    public Enumerator GetAsyncEnumerator()
            => throw null;
    public sealed class Enumerator
    {
        private System.Threading.Tasks.Task<bool> WaitForNextAsync()
            => throw null;
        public int TryGetNext(out bool success)
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,33): error CS0122: 'D.Enumerator.WaitForNextAsync()' is inaccessible due to its protection level
                //         foreach await (var i in new D())
                Diagnostic(ErrorCode.ERR_BadAccess, "new D()").WithArguments("D.Enumerator.WaitForNextAsync()").WithLocation(6, 33),
                // (6,33): error CS9002: Async foreach requires that the return type 'D.Enumerator' of 'D.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new D())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new D()").WithArguments("D.Enumerator", "D.GetAsyncEnumerator()").WithLocation(6, 33)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            Assert.Equal(default, model.GetForEachStatementInfo(foreachSyntax));
        }

        [Fact]
        public void TestWithPattern_InaccessibleTryGetNext()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new D())
        {
        }
    }
}
class D
{
    public Enumerator GetAsyncEnumerator()
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
            => throw null;
        private int TryGetNext(out bool success)
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,33): error CS0122: 'D.Enumerator.TryGetNext(out bool)' is inaccessible due to its protection level
                //         foreach await (var i in new D())
                Diagnostic(ErrorCode.ERR_BadAccess, "new D()").WithArguments("D.Enumerator.TryGetNext(out bool)").WithLocation(6, 33),
                // (6,33): error CS9002: Async foreach requires that the return type 'D.Enumerator' of 'D.GetAsyncEnumerator()' must have suitable public WaitForNextAsync and TryGetNext methods
                //         foreach await (var i in new D())
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
        foreach await (var s in new C())
        {
            Write($""{s.ToString()} "");
        }
        Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator() => new Enumerator();
    public sealed class Enumerator
    {
        int i = -1;
        public S TryGetNext(out bool success)
        {
            i++;
            success = (i % 10 % 3 != 0);
            return new S(i);
        }
        public async Task<bool> WaitForNextAsync()
        {
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
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
    public override string ToString()
        => i.ToString();
}
";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "11 12 Done");
        }

        [Fact]
        public void TestWithPattern_RefReturningTryGetNext()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
public class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        foreach await (var s in new C())
        {
            Write($""{s.ToString()} "");
        }
        Write(""Done"");
    }
    public Enumerator GetAsyncEnumerator() => new Enumerator();
    public sealed class Enumerator
    {
        int i = -1;
        S current;
        public ref S TryGetNext(out bool success)
        {
            i++;
            success = (i % 10 % 3 != 0);
            current = new S(i);
            return ref current;
        }
        public async Task<bool> WaitForNextAsync()
        {
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "11 12 Done", verify: Verification.Fails);
        }

        [Fact]
        public void TestWithPattern_IterationVariableIsReadOnly()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        foreach await (var i in new C())
        {
            i = 1;
        }
    }
    public Enumerator GetAsyncEnumerator()
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> WaitForNextAsync()
            => throw null;
        public int TryGetNext(out bool success)
            => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
            comp.VerifyDiagnostics(
                // (8,13): error CS1656: Cannot assign to 'i' because it is a 'foreach iteration variable'
                //             i = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "foreach iteration variable").WithLocation(8, 13)
                );
        }

        [Fact]
        public void TestWithPattern_WithStruct_WaitForNextAsyncReturnsTask()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        foreach await (var i in new C())
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
        public int TryGetNext(out bool success)
        {
            Write($""Next({i}) "");
            i++;
            success = (i % 10 % 3 != 0);
            return i;
        }
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await new ValueTask(Task.Delay(10));
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            // Note: NextAsync(3) is followed by Next(3) as NextAsync incremented a copy of the enumerator struct
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(0) Got(1) Next(1) Got(2) Next(2) NextAsync(3) Next(3) "+
                "Got(4) Next(4) Got(5) Next(5) NextAsync(6) Next(6) Got(7) Next(7) Got(8) Next(8) NextAsync(9) Done", verify: Verification.Skipped);
        }

        [Fact]
        public void TestWithPattern_WaitForNextAsyncReturnsValueTask()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        foreach await (var i in new C())
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
        public int TryGetNext(out bool success)
        {
            i++;
            success = (i % 10 % 3 != 0);
            return i;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await new ValueTask(Task.Delay(10));
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Got(1) Got(2) Got(4) Got(5) Got(7) Got(8) Done", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> C.AsyncEnumerator.WaitForNextAsync()", info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.False(internalInfo.NeedsDisposeMethod);
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
        foreach await (var i in new C())
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
        public int TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            return found ? i++ : 0;
        }
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11) Next(12) Got(12) Next(13) NextAsync(13) Next(24) Got(24) "+
                "Next(25) Got(25) Next(26) NextAsync(26)", verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void TestWithPattern_WithUnsealed_WithIAsyncDisposable()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        foreach await (var i in new C())
        {
            Write($""Got({i}) "");
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        return new DerivedEnumerator();
    }
    public class Enumerator
    {
        protected int i = 0;
        public int TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            return found ? i++ : 0;
        }
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
    }
    public class DerivedEnumerator : Enumerator, System.IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);

            var verifier = CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11) Next(12) Got(12) Next(13) NextAsync(13) "+
                "Next(24) Got(24) Next(25) Got(25) Next(26) NextAsync(26) Dispose(37)", verify: Verification.Skipped);

            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      485 (0x1e5)
  .maxstack  3
  .locals init (int V_0,
                bool V_1,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_2,
                C.<Main>d__0 V_3,
                object V_4,
                System.IAsyncDisposable V_5,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_6,
                System.Threading.Tasks.ValueTask V_7,
                System.Exception V_8)
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
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_0039
    IL_0014:  br         IL_0151
    // sequence point: {
    IL_0019:  nop
    // sequence point: foreach
    IL_001a:  nop
    IL_001b:  ldarg.0
    IL_001c:  newobj     ""C..ctor()""
    IL_0021:  call       ""C.Enumerator C.GetAsyncEnumerator()""
    IL_0026:  stfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    // sequence point: <hidden>
    IL_002b:  ldarg.0
    IL_002c:  ldnull
    IL_002d:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_0032:  ldarg.0
    IL_0033:  ldc.i4.0
    IL_0034:  stfld      ""int C.<Main>d__0.<>s__3""
    // sequence point: <hidden>
    IL_0039:  nop
    .try
    {
      // sequence point: <hidden>
      IL_003a:  ldloc.0
      IL_003b:  brfalse.s  IL_003f
      IL_003d:  br.s       IL_0041
      IL_003f:  br.s       IL_00bb
      // sequence point: <hidden>
      IL_0041:  br.s       IL_007a
      // sequence point: var i
      IL_0043:  ldarg.0
      IL_0044:  ldarg.0
      IL_0045:  ldfld      ""C.Enumerator C.<Main>d__0.<>s__1""
      IL_004a:  ldloca.s   V_1
      IL_004c:  callvirt   ""int C.Enumerator.TryGetNext(out bool)""
      IL_0051:  stfld      ""int C.<Main>d__0.<i>5__4""
      IL_0056:  ldloc.1
      IL_0057:  brtrue.s   IL_005b
      IL_0059:  br.s       IL_007a
      // sequence point: {
      IL_005b:  nop
      // sequence point: Write($""Got({i}) "");
      IL_005c:  ldstr      ""Got({0}) ""
      IL_0061:  ldarg.0
      IL_0062:  ldfld      ""int C.<Main>d__0.<i>5__4""
      IL_0067:  box        ""int""
      IL_006c:  call       ""string string.Format(string, object)""
      IL_0071:  call       ""void System.Console.Write(string)""
      IL_0076:  nop
      // sequence point: }
      IL_0077:  nop
      IL_0078:  br.s       IL_0043
      // sequence point: in
      IL_007a:  ldarg.0
      IL_007b:  ldfld      ""C.Enumerator C.<Main>d__0.<>s__1""
      IL_0080:  callvirt   ""System.Threading.Tasks.Task<bool> C.Enumerator.WaitForNextAsync()""
      IL_0085:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
      IL_008a:  stloc.2
      // sequence point: <hidden>
      IL_008b:  ldloca.s   V_2
      IL_008d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
      IL_0092:  brtrue.s   IL_00d7
      IL_0094:  ldarg.0
      IL_0095:  ldc.i4.0
      IL_0096:  dup
      IL_0097:  stloc.0
      IL_0098:  stfld      ""int C.<Main>d__0.<>1__state""
      // async: yield
      IL_009d:  ldarg.0
      IL_009e:  ldloc.2
      IL_009f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00a4:  ldarg.0
      IL_00a5:  stloc.3
      IL_00a6:  ldarg.0
      IL_00a7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
      IL_00ac:  ldloca.s   V_2
      IL_00ae:  ldloca.s   V_3
      IL_00b0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Main>d__0)""
      IL_00b5:  nop
      IL_00b6:  leave      IL_01e4
      // async: resume
      IL_00bb:  ldarg.0
      IL_00bc:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00c1:  stloc.2
      IL_00c2:  ldarg.0
      IL_00c3:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00c8:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
      IL_00ce:  ldarg.0
      IL_00cf:  ldc.i4.m1
      IL_00d0:  dup
      IL_00d1:  stloc.0
      IL_00d2:  stfld      ""int C.<Main>d__0.<>1__state""
      IL_00d7:  ldarg.0
      IL_00d8:  ldloca.s   V_2
      IL_00da:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
      IL_00df:  stfld      ""bool C.<Main>d__0.<>s__5""
      IL_00e4:  ldarg.0
      IL_00e5:  ldfld      ""bool C.<Main>d__0.<>s__5""
      IL_00ea:  brtrue     IL_0043
      // sequence point: <hidden>
      IL_00ef:  leave.s    IL_00fd
    }
    catch object
    {
      // sequence point: <hidden>
      IL_00f1:  stloc.s    V_4
      IL_00f3:  ldarg.0
      IL_00f4:  ldloc.s    V_4
      IL_00f6:  stfld      ""object C.<Main>d__0.<>s__2""
      IL_00fb:  leave.s    IL_00fd
    }
    // sequence point: <hidden>
    IL_00fd:  ldarg.0
    IL_00fe:  ldfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    IL_0103:  isinst     ""System.IAsyncDisposable""
    IL_0108:  stloc.s    V_5
    IL_010a:  ldloc.s    V_5
    IL_010c:  brfalse.s  IL_0176
    IL_010e:  ldloc.s    V_5
    IL_0110:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_0115:  stloc.s    V_7
    IL_0117:  ldloca.s   V_7
    IL_0119:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_011e:  stloc.s    V_6
    // sequence point: <hidden>
    IL_0120:  ldloca.s   V_6
    IL_0122:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_0127:  brtrue.s   IL_016e
    IL_0129:  ldarg.0
    IL_012a:  ldc.i4.1
    IL_012b:  dup
    IL_012c:  stloc.0
    IL_012d:  stfld      ""int C.<Main>d__0.<>1__state""
    // async: yield
    IL_0132:  ldarg.0
    IL_0133:  ldloc.s    V_6
    IL_0135:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_013a:  ldarg.0
    IL_013b:  stloc.3
    IL_013c:  ldarg.0
    IL_013d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_0142:  ldloca.s   V_6
    IL_0144:  ldloca.s   V_3
    IL_0146:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<Main>d__0)""
    IL_014b:  nop
    IL_014c:  leave      IL_01e4
    // async: resume
    IL_0151:  ldarg.0
    IL_0152:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_0157:  stloc.s    V_6
    IL_0159:  ldarg.0
    IL_015a:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_015f:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_0165:  ldarg.0
    IL_0166:  ldc.i4.m1
    IL_0167:  dup
    IL_0168:  stloc.0
    IL_0169:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_016e:  ldloca.s   V_6
    IL_0170:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_0175:  nop
    // sequence point: <hidden>
    IL_0176:  ldarg.0
    IL_0177:  ldfld      ""object C.<Main>d__0.<>s__2""
    IL_017c:  stloc.s    V_4
    IL_017e:  ldloc.s    V_4
    IL_0180:  brfalse.s  IL_019f
    IL_0182:  ldloc.s    V_4
    IL_0184:  isinst     ""System.Exception""
    IL_0189:  stloc.s    V_8
    IL_018b:  ldloc.s    V_8
    IL_018d:  brtrue.s   IL_0192
    IL_018f:  ldloc.s    V_4
    IL_0191:  throw
    IL_0192:  ldloc.s    V_8
    IL_0194:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_0199:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_019e:  nop
    IL_019f:  ldarg.0
    IL_01a0:  ldfld      ""int C.<Main>d__0.<>s__3""
    IL_01a5:  pop
    IL_01a6:  ldarg.0
    IL_01a7:  ldnull
    IL_01a8:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_01ad:  ldarg.0
    IL_01ae:  ldnull
    IL_01af:  stfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    IL_01b4:  leave.s    IL_01d0
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_01b6:  stloc.s    V_8
    IL_01b8:  ldarg.0
    IL_01b9:  ldc.i4.s   -2
    IL_01bb:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_01c0:  ldarg.0
    IL_01c1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_01c6:  ldloc.s    V_8
    IL_01c8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_01cd:  nop
    IL_01ce:  leave.s    IL_01e4
  }
  // sequence point: }
  IL_01d0:  ldarg.0
  IL_01d1:  ldc.i4.s   -2
  IL_01d3:  stfld      ""int C.<Main>d__0.<>1__state""
  // sequence point: <hidden>
  IL_01d8:  ldarg.0
  IL_01d9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_01de:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01e3:  nop
  IL_01e4:  ret
}
", sequencePoints: "C+<Main>d__0.MoveNext", source: source + s_interfaces);
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
        foreach await (var i in new C())
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
        public int TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            return found ? i++ : 0;
        }
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11) Next(12) Got(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Got(25) Next(26) NextAsync(26) Dispose(37)", verify: Verification.Skipped);
        }

        [Fact]
        public void TestWithPattern_WithIAsyncDisposableUseSiteError()
        {
            string enumerator = @"
using System.Threading.Tasks;
public class C
{
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator : System.IAsyncDisposable
    {
        public Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        public int TryGetNext(out bool success)
        {
            throw null;
        }
        public async ValueTask DisposeAsync()
        {
            await new ValueTask(Task.Delay(10));
        }
    }
}";
            string source = @"
using System.Threading.Tasks;
class Client
{
    async Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
}";
            var lib = CreateCompilationWithTasksExtensions(enumerator + s_interfaces);
            lib.VerifyDiagnostics();

            var comp = CreateCompilationWithTasksExtensions(source, references: new[] { lib.EmitToImageReference() });
            comp.MakeTypeMissing(WellKnownType.System_IAsyncDisposable);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.False(internalInfo.NeedsDisposeMethod);
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
        foreach await (var i in new C())
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        throw null;
    }
    IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator()
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,33): error CS9003: Async foreach statement cannot operate on variables of type 'C' because it implements multiple instantiations of 'IAsyncEnumerable<T>'; try casting to a specific interface instantiation
                //         foreach await (var i in new C())
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
    IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator()
        => throw null;
}
class C : Base, IAsyncEnumerable<int>
{
    async Task M()
    {
        foreach await (var i in new C())
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
        => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (13,33): error CS9003: Async foreach statement cannot operate on variables of type 'C' because it implements multiple instantiations of 'IAsyncEnumerable<T>'; try casting to a specific interface instantiation
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_MultipleIAsyncEnumOfT, "new C()").WithArguments("C", "System.Collections.Generic.IAsyncEnumerable<T>").WithLocation(13, 33)
                );
        }

        [Fact]
        public void TestWithInterface()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    static async System.Threading.Tasks.Task Main()
    {
        foreach await (var i in new C())
        {
            Write($""Got({i}) "");
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i = 0;
        public int TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            return found ? i++ : 0;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11) Next(12) Got(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Got(25) Next(26) NextAsync(26) Dispose(37)", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
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
        foreach await (var i in new C())
        {
            Write($""Got({i}) "");
        }
        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator(3);
    }
    internal sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            return found ? i++ : 0;
        }
        public ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            return new ValueTask<bool>(i < 30); // return a completed task
        }
        public ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            return new ValueTask(Task.CompletedTask); // return a completed task
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(3) Next(14) Got(14) Next(15) Got(15) Next(16) NextAsync(16) Next(27) Got(27) Next(28) Got(28) Next(29) NextAsync(29) Dispose(40) Done", verify: Verification.Skipped);
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
        foreach await (var i in new C())
        {
            if (i == 11 || i == 12) { Write($""Continue({i}) ""); continue; }
            if (i == 25) { Write(""Break ""); break; }
            Write($""Got({i}) "");
        }
        Write(""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i = 0;
        public int TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            return found ? i++ : 0;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Continue(11) Next(12) Continue(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Break Dispose(26) Done", verify: Verification.Skipped);
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
        foreach await (var i in new C())
        {
            if (i == 11 || i == 12) { Write($""Continue({i}) ""); continue; }
            if (i == 25) { Write(""Break ""); goto done; }
            Write($""Got({i}) "");
        }
        done:
        Write(""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i = 0;
        public int TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            return found ? i++ : 0;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Continue(11) Next(12) Continue(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Break Dispose(26) Done", verify: Verification.Skipped);
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
        foreach await (var i in new C())
        {
            Write($""Got({i}) "");
        }
        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator(0);
    }
    internal struct AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int TryGetNext(out bool success)
        {
            Write($""Next({i}) "");
            i++;
            success = (i % 10 % 3 != 0);
            return i;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            // Note: NextAsync(3) is followed by Next(3) as NextAsync incremented a copy of the enumerator struct
            // PROTOTYPE(async-streams) This seems strange, as I would expect we'd be handling as an IAsyncEnumerator<int> rather than as a struct
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(0) Got(1) Next(1) Got(2) Next(2) NextAsync(3) Next(3) Got(4) Next(4) Got(5) Next(5) NextAsync(6) Next(6) Got(7) Next(7) Got(8) Next(8) NextAsync(9) Dispose(9) Done", verify: Verification.Skipped);
        }

        [Fact]
        public void TestWithInterface_WithStruct_ManualIteration()
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
            while (await e.WaitForNextAsync())
            {
                while (true)
                {
                    int i = e.TryGetNext(out bool success);
                    if (!success) break;
                    Write($""Got({i}) "");
                }
            }
        }
        finally { await e.DisposeAsync(); }

        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator(0);
    }
    internal struct AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int TryGetNext(out bool success)
        {
            Write($""Next({i}) "");
            i++;
            success = (i % 10 % 3 != 0);
            return i;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            // Note: NextAsync(3) is followed by Next(3) as NextAsync incremented a copy of the enumerator struct
            // PROTOTYPE(async-streams) This seems strange, as I would expect we'd be handling as an IAsyncEnumerator<int> rather than as a struct
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(0) Got(1) Next(1) Got(2) Next(2) NextAsync(3) Next(3) Got(4) Next(4) Got(5) Next(5) NextAsync(6) Next(6) Got(7) Next(7) Got(8) Next(8) NextAsync(9) Dispose(9) Done", verify: Verification.Skipped);
        }

        [Fact]
        public void TestWithInterface_WithStruct2()
        {
            string source = @"
using static System.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
class C : IAsyncEnumerable<int>
{
    static async Task Main()
    {
        foreach await (var i in new C())
        {
            Write($""Got({i}) "");
        }
        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator(0);
    }
    internal struct AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        int lastReturned;
        internal AsyncEnumerator(int start) { i = start; lastReturned = -1; }
        public int TryGetNext(out bool success)
        {
            Write($""Next({i}) "");
            if (lastReturned != i)
            {
                lastReturned = i;
                success = true;
                return i;
            }
            i++;
            success = false;
            return -2;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 10;
            bool more = await Task.FromResult(i < 13);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(0) Got(0) Next(0) NextAsync(1) Next(1) Got(1) Next(1) NextAsync(2) Next(2) Got(2) Next(2) NextAsync(3) Dispose(3) Done", verify: Verification.Skipped);
        }

        [Fact, WorkItem(27651, "https://github.com/dotnet/roslyn/issues/27651")]
        public void TestControlFlowAnalysis()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        foreach await (var item in collection) { }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
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
        foreach await (var i in null)
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (8,33): error CS0186: Use of null is not valid in this context
                //         foreach await (var i in null)
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
            foreach await (var i in c)
            {
            }
        }
        catch (System.NullReferenceException)
        {
            System.Console.Write(""Success"");
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator(0);
    }
    internal struct AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int TryGetNext(out bool success)
        {
            throw new System.Exception();
        }
        public ValueTask<bool> WaitForNextAsync()
        {
            throw new System.Exception();
        }
        public ValueTask DisposeAsync()
        {
            throw new System.Exception();
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Success", verify: Verification.Skipped);
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
            foreach await (var i in new C())
            {
                Write($""Got({i}) "");
            }
        }
        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator(0);
    }
    internal struct AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int TryGetNext(out bool success)
        {
            Write($""Next({i}) "");
            i++;
            success = (i % 10 % 3 != 0);
            return i;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Try NextAsync(0) Next(0) Got(1) Next(1) Got(2) Next(2) NextAsync(3) Next(3) Got(4) Next(4) Got(5) Next(5) NextAsync(6) Next(6) Got(7) Next(7) Got(8) Next(8) NextAsync(9) Dispose(9) Done", verify: Verification.Skipped);
        }

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
            foreach await (var i in new C())
            {
            }
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (13,13): error CS0157: Control cannot leave the body of a finally clause
                //             foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadFinallyLeave, "foreach").WithLocation(13, 13)
                );
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
        foreach await (Element i in new C())
        {
            Write($""Got({i}) "");
        }
        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator(0);
    }
    internal class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int TryGetNext(out bool success)
        {
            Write($""Next({i}) "");
            i++;
            success = (i % 10 % 3 != 0);
            return i;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Convert(12) Got(12) Next(12) Convert(13) NextAsync(13) Dispose(24) Done", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ExplicitUserDefined, info.ElementConversion.Kind);
            Assert.Equal("Element Element.op_Implicit(System.Int32 value)", info.ElementConversion.MethodSymbol.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
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
        foreach await (var i in c)
        {
            Write($""Got({i}) "");
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i = 0;
        public int TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            return found ? i++ : 0;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11) Next(12) Got(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Got(25) Next(26) NextAsync(26) Dispose(37)", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
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
            foreach await (var i in c)
            {
                Write($""UNREACHABLE"");
            }
        }
        catch (System.InvalidOperationException)
        {
            Write($""Success"");
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        throw new System.Exception();
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Success", verify: Verification.Skipped);
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
        foreach await (var (i, j) in new C())
        {
            Write($""Got({i},{j}) "");
        }
        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i = 0;
        public int TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            return found ? i++ : 0;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await new ValueTask(Task.Delay(10));
        }
    }
}
public static class Extensions
{
    public static void Deconstruct(this int i, out string x1, out int x2) { Write($""Deconstruct({i}) ""); x1 = i.ToString(); x2 = -i; }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            // https://github.com/dotnet/roslyn/issues/30258 The deconstruction should be after the check for success (ie. it should not occur if TryGetNext got no item)
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Deconstruct(11) Got(11,-11) Next(12) Deconstruct(12) Got(12,-12) Next(13) Deconstruct(0) NextAsync(13) Dispose(24) Done", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
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
        foreach await (var (i, j) in new C())
        {
        }
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
        => throw null;
}
public static class Extensions
{
    public static void Deconstruct(this int i, out int x1, out int x2) { x1 = i; x2 = -i; }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,17): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         foreach await (var (i, j) in new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(7, 17)
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
        foreach await (var (i, j) in new C())
        {
            Write($""Got({i},{j}) "");
        }
        Write($""Done"");
    }
    IAsyncEnumerator<(string, int)> IAsyncEnumerable<(string, int)>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<(string, int)>
    {
        int i = 0;
        public (string, int) TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            found = i % 10 % 3 != 0;
            int value = found ? i++ : 0;
            return (value.ToString(), -value);
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await new ValueTask(Task.Delay(10));
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11,-11) Next(12) Got(12,-12) Next(13) NextAsync(13) Dispose(24) Done", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)> System.Collections.Generic.IAsyncEnumerable<(System.String, System.Int32)>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("(System.String, System.Int32) System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("(System.String, System.Int32)", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
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
            while (await e.WaitForNextAsync())
            {
                while (true)
                {
                    (int i, int j) = e.TryGetNext(out bool success);
                    if (!success) break;
                    Write($""Got({i},{j}) "");
                }
            }
        }
        finally { await e.DisposeAsync(); }

        Write($""Done"");
    }
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator(0);
    }
    internal class AsyncEnumerator : IAsyncEnumerator<int>
    {
        int i;
        internal AsyncEnumerator(int start) { i = start; }
        public int TryGetNext(out bool success)
        {
            Write($""Next({i}) "");
            i++;
            success = (i % 10 % 3 != 0);
            return i;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}
public static class Extensions
{
    public static void Deconstruct(this int i, out int x1, out int x2) { x1 = i; x2 = -i; }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(12,-12) Next(12) NextAsync(13) Dispose(24) Done", verify: Verification.Skipped);
        }

        [Fact]
        public void TestWithPatternAndObsolete()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    static async System.Threading.Tasks.Task Main()
    {
        foreach await (var i in new C())
        {
        }
    }
    [System.Obsolete]
    public AsyncEnumerator GetAsyncEnumerator()
    {
        throw null;
    }
    [System.Obsolete]
    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        [System.Obsolete]
        public int TryGetNext(out bool found)
        {
            throw null;
        }
        [System.Obsolete]
        public Task<bool> WaitForNextAsync()
        {
            throw null;
        }
        [System.Obsolete]
        public ValueTask DisposeAsync()
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,9): warning CS0612: 'C.GetAsyncEnumerator()' is obsolete
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.GetAsyncEnumerator()").WithLocation(7, 9),
                // (7,9): warning CS0612: 'C.AsyncEnumerator.WaitForNextAsync()' is obsolete
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.AsyncEnumerator.WaitForNextAsync()").WithLocation(7, 9),
                // (7,9): warning CS0612: 'C.AsyncEnumerator.TryGetNext(out bool)' is obsolete
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.AsyncEnumerator.TryGetNext(out bool)").WithLocation(7, 9)
                );
            // Note: Obsolete on DisposeAsync is not reported since always called through IAsyncDisposable interface
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
        foreach await (var i in c)
        {
        }
    }
    public IAsyncEnumerator<int> GetAsyncEnumerator()
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (8,33): error CS0165: Use of unassigned local variable 'c'
                //         foreach await (var i in c)
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
    public IAsyncEnumerator<int> GetAsyncEnumerator()
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,27): error CS9004: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'foreach await'?
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
    IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator()
    {
        return new AsyncEnumerator();
    }
    sealed class AsyncEnumerator : IAsyncEnumerator<T>
    {
        int i = 0;
        public T TryGetNext(out bool found)
        {
            Write($""Next({i}) "");
            i++;
            found = i % 10 % 3 != 0;
            return default;
        }
        public async ValueTask<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async ValueTask DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await new ValueTask(Task.Delay(10));
        }
    }
}
class C
{
    static async System.Threading.Tasks.Task Main()
    {
        foreach await (var i in new Collection<int>())
        {
            Write($""Got "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got Next(12) NextAsync(13) Next(24) Got Next(25) NextAsync(26) Dispose(37)", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void TestWithInterfaceImplementingPattern()
        {
            string source = @"
using static System.Console;
using System.Threading.Tasks;

public interface ICollection<T>
{
    IMyAsyncEnumerator<T> GetAsyncEnumerator();
}
public interface IMyAsyncEnumerator<T>
{
    T TryGetNext(out bool found);
    Task<bool> WaitForNextAsync();
}

public class Collection<T> : ICollection<T>
{
    public IMyAsyncEnumerator<T> GetAsyncEnumerator()
    {
        return new MyAsyncEnumerator<T>();
    }
}
public sealed class MyAsyncEnumerator<T> : IMyAsyncEnumerator<T>
{
    int i = 0;
    public T TryGetNext(out bool found)
    {
        Write($""Next({i}) "");
        i++;
        found = i % 10 % 3 != 0;
        return default;
    }
    public async Task<bool> WaitForNextAsync()
    {
        Write($""NextAsync({i}) "");
        i = i + 11;
        bool more = await Task.FromResult(i < 30);
        return more;
    }
}

class C
{
    static async System.Threading.Tasks.Task Main()
    {
        ICollection<int> c = new Collection<int>();
        foreach await (var i in c)
        {
            Write($""Got "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got Next(12) NextAsync(13) Next(24) Got Next(25) NextAsync(26)", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("IMyAsyncEnumerator<System.Int32> ICollection<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> IMyAsyncEnumerator<System.Int32>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 IMyAsyncEnumerator<System.Int32>.TryGetNext(out System.Boolean found)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);

            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      481 (0x1e1)
  .maxstack  3
  .locals init (int V_0,
                bool V_1,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_2,
                C.<Main>d__0 V_3,
                object V_4,
                System.IAsyncDisposable V_5,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_6,
                System.Threading.Tasks.ValueTask V_7,
                System.Exception V_8)
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
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_0045
    IL_0014:  br         IL_014d
    // sequence point: {
    IL_0019:  nop
    // sequence point: ICollection<int> c = new Collection<int>();
    IL_001a:  ldarg.0
    IL_001b:  newobj     ""Collection<int>..ctor()""
    IL_0020:  stfld      ""ICollection<int> C.<Main>d__0.<c>5__1""
    // sequence point: foreach
    IL_0025:  nop
    IL_0026:  ldarg.0
    IL_0027:  ldarg.0
    IL_0028:  ldfld      ""ICollection<int> C.<Main>d__0.<c>5__1""
    IL_002d:  callvirt   ""IMyAsyncEnumerator<int> ICollection<int>.GetAsyncEnumerator()""
    IL_0032:  stfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    // sequence point: <hidden>
    IL_0037:  ldarg.0
    IL_0038:  ldnull
    IL_0039:  stfld      ""object C.<Main>d__0.<>s__3""
    IL_003e:  ldarg.0
    IL_003f:  ldc.i4.0
    IL_0040:  stfld      ""int C.<Main>d__0.<>s__4""
    // sequence point: <hidden>
    IL_0045:  nop
    .try
    {
      // sequence point: <hidden>
      IL_0046:  ldloc.0
      IL_0047:  brfalse.s  IL_004b
      IL_0049:  br.s       IL_004d
      IL_004b:  br.s       IL_00b7
      // sequence point: <hidden>
      IL_004d:  br.s       IL_0076
      // sequence point: var i
      IL_004f:  ldarg.0
      IL_0050:  ldarg.0
      IL_0051:  ldfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
      IL_0056:  ldloca.s   V_1
      IL_0058:  callvirt   ""int IMyAsyncEnumerator<int>.TryGetNext(out bool)""
      IL_005d:  stfld      ""int C.<Main>d__0.<i>5__5""
      IL_0062:  ldloc.1
      IL_0063:  brtrue.s   IL_0067
      IL_0065:  br.s       IL_0076
      // sequence point: {
      IL_0067:  nop
      // sequence point: Write($""Got "");
      IL_0068:  ldstr      ""Got ""
      IL_006d:  call       ""void System.Console.Write(string)""
      IL_0072:  nop
      // sequence point: }
      IL_0073:  nop
      IL_0074:  br.s       IL_004f
      // sequence point: in
      IL_0076:  ldarg.0
      IL_0077:  ldfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
      IL_007c:  callvirt   ""System.Threading.Tasks.Task<bool> IMyAsyncEnumerator<int>.WaitForNextAsync()""
      IL_0081:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
      IL_0086:  stloc.2
      // sequence point: <hidden>
      IL_0087:  ldloca.s   V_2
      IL_0089:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
      IL_008e:  brtrue.s   IL_00d3
      IL_0090:  ldarg.0
      IL_0091:  ldc.i4.0
      IL_0092:  dup
      IL_0093:  stloc.0
      IL_0094:  stfld      ""int C.<Main>d__0.<>1__state""
      // async: yield
      IL_0099:  ldarg.0
      IL_009a:  ldloc.2
      IL_009b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00a0:  ldarg.0
      IL_00a1:  stloc.3
      IL_00a2:  ldarg.0
      IL_00a3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
      IL_00a8:  ldloca.s   V_2
      IL_00aa:  ldloca.s   V_3
      IL_00ac:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Main>d__0)""
      IL_00b1:  nop
      IL_00b2:  leave      IL_01e0
      // async: resume
      IL_00b7:  ldarg.0
      IL_00b8:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00bd:  stloc.2
      IL_00be:  ldarg.0
      IL_00bf:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00c4:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
      IL_00ca:  ldarg.0
      IL_00cb:  ldc.i4.m1
      IL_00cc:  dup
      IL_00cd:  stloc.0
      IL_00ce:  stfld      ""int C.<Main>d__0.<>1__state""
      IL_00d3:  ldarg.0
      IL_00d4:  ldloca.s   V_2
      IL_00d6:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
      IL_00db:  stfld      ""bool C.<Main>d__0.<>s__6""
      IL_00e0:  ldarg.0
      IL_00e1:  ldfld      ""bool C.<Main>d__0.<>s__6""
      IL_00e6:  brtrue     IL_004f
      // sequence point: <hidden>
      IL_00eb:  leave.s    IL_00f9
    }
    catch object
    {
      // sequence point: <hidden>
      IL_00ed:  stloc.s    V_4
      IL_00ef:  ldarg.0
      IL_00f0:  ldloc.s    V_4
      IL_00f2:  stfld      ""object C.<Main>d__0.<>s__3""
      IL_00f7:  leave.s    IL_00f9
    }
    // sequence point: <hidden>
    IL_00f9:  ldarg.0
    IL_00fa:  ldfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    IL_00ff:  isinst     ""System.IAsyncDisposable""
    IL_0104:  stloc.s    V_5
    IL_0106:  ldloc.s    V_5
    IL_0108:  brfalse.s  IL_0172
    IL_010a:  ldloc.s    V_5
    IL_010c:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_0111:  stloc.s    V_7
    IL_0113:  ldloca.s   V_7
    IL_0115:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_011a:  stloc.s    V_6
    // sequence point: <hidden>
    IL_011c:  ldloca.s   V_6
    IL_011e:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_0123:  brtrue.s   IL_016a
    IL_0125:  ldarg.0
    IL_0126:  ldc.i4.1
    IL_0127:  dup
    IL_0128:  stloc.0
    IL_0129:  stfld      ""int C.<Main>d__0.<>1__state""
    // async: yield
    IL_012e:  ldarg.0
    IL_012f:  ldloc.s    V_6
    IL_0131:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_0136:  ldarg.0
    IL_0137:  stloc.3
    IL_0138:  ldarg.0
    IL_0139:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_013e:  ldloca.s   V_6
    IL_0140:  ldloca.s   V_3
    IL_0142:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<Main>d__0)""
    IL_0147:  nop
    IL_0148:  leave      IL_01e0
    // async: resume
    IL_014d:  ldarg.0
    IL_014e:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_0153:  stloc.s    V_6
    IL_0155:  ldarg.0
    IL_0156:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_015b:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_0161:  ldarg.0
    IL_0162:  ldc.i4.m1
    IL_0163:  dup
    IL_0164:  stloc.0
    IL_0165:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_016a:  ldloca.s   V_6
    IL_016c:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_0171:  nop
    // sequence point: <hidden>
    IL_0172:  ldarg.0
    IL_0173:  ldfld      ""object C.<Main>d__0.<>s__3""
    IL_0178:  stloc.s    V_4
    IL_017a:  ldloc.s    V_4
    IL_017c:  brfalse.s  IL_019b
    IL_017e:  ldloc.s    V_4
    IL_0180:  isinst     ""System.Exception""
    IL_0185:  stloc.s    V_8
    IL_0187:  ldloc.s    V_8
    IL_0189:  brtrue.s   IL_018e
    IL_018b:  ldloc.s    V_4
    IL_018d:  throw
    IL_018e:  ldloc.s    V_8
    IL_0190:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_0195:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_019a:  nop
    IL_019b:  ldarg.0
    IL_019c:  ldfld      ""int C.<Main>d__0.<>s__4""
    IL_01a1:  pop
    IL_01a2:  ldarg.0
    IL_01a3:  ldnull
    IL_01a4:  stfld      ""object C.<Main>d__0.<>s__3""
    IL_01a9:  ldarg.0
    IL_01aa:  ldnull
    IL_01ab:  stfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    IL_01b0:  leave.s    IL_01cc
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_01b2:  stloc.s    V_8
    IL_01b4:  ldarg.0
    IL_01b5:  ldc.i4.s   -2
    IL_01b7:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_01bc:  ldarg.0
    IL_01bd:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_01c2:  ldloc.s    V_8
    IL_01c4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_01c9:  nop
    IL_01ca:  leave.s    IL_01e0
  }
  // sequence point: }
  IL_01cc:  ldarg.0
  IL_01cd:  ldc.i4.s   -2
  IL_01cf:  stfld      ""int C.<Main>d__0.<>1__state""
  // sequence point: <hidden>
  IL_01d4:  ldarg.0
  IL_01d5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_01da:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01df:  nop
  IL_01e0:  ret
}
", sequencePoints: "C+<Main>d__0.MoveNext", source: source);
        }
    }
}
