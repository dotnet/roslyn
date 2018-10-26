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
    public class CodeGenAwaitForeachTests : EmitMetadataTestBase
    {
        private static readonly string s_interfaces = @"
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator();
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
        => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (7,9): error CS8370: Feature 'async streams' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         await foreach (int i in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "await").WithArguments("async streams", "8.0").WithLocation(7, 9)
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

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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

    public IAsyncEnumerator<uint> GetAsyncEnumerator()
        => new AsyncEnumerator();
    public sealed class AsyncEnumerator : IAsyncEnumerator<uint>
    {
        bool firstValue = true;
        public uint Current => 0xFFFFFFFF;
        public async ValueTask<bool> MoveNextAsync()
        {
            await Task.Delay(10);
            bool result = firstValue;
            firstValue = false;
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
        await foreach (int i in new C())
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
                // (6,33): error CS8411: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetAsyncEnumerator'
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
                // (6,33): error CS8411: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetAsyncEnumerator'
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
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_PatternStaticOrInaccessible, "new C()").WithArguments("C", "async streams", "C.GetAsyncEnumerator()").WithLocation(6, 33),
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
    public Enumerator GetAsyncEnumerator()
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
                // (6,15): warning CS0612: 'C.GetAsyncEnumerator()' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.GetAsyncEnumerator()").WithLocation(6, 15),
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
    public Enumerator GetAsyncEnumerator()
        => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<object> MoveNextAsync()
        => throw null;
        public int Current
        {
            get => throw null;
        }
     }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new C()) { }
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
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
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
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
    public Enumerator GetAsyncEnumerator()
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
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
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
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
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
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
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
    public Enumerator GetAsyncEnumerator()
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
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new C()) { }
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
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
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
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
    public Enumerator GetAsyncEnumerator()
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
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new C()) { }
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
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
    public Enumerator GetAsyncEnumerator()
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
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
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
    public Enumerator GetAsyncEnumerator()
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
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator()").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestMoveNextAsync_WithOptionalParameter()
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
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync(int bad = 0)
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
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new C())
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,15): error CS0030: Cannot convert type 'int' to 'string'
                //         await foreach (string i in new C())
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("int", "string").WithLocation(6, 15)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

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
    public AsyncEnumerator GetAsyncEnumerator()
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

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    public AsyncEnumerator GetAsyncEnumerator()
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics( );

            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(1) Convert(1) Got(1) NextAsync(1) Current(2) Convert(2) Got(2) NextAsync(2) Current(3) Convert(3) Got(3) NextAsync(3) Dispose(4)",
                verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    public AsyncEnumerator GetAsyncEnumerator()
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
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Got(1) Got(2) Captured(1)", verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    public AsyncEnumerator GetAsyncEnumerator()
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
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "NextAsync(1) Current(1) Got(1) NextAsync(2) Current(2) Got(2) NextAsync(3) Current(3) Got(3) NextAsync(4) Dispose(4)",
                verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    public AsyncEnumerator GetAsyncEnumerator()
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "exception", verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    public AsyncEnumerator GetAsyncEnumerator()
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
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "dispose exception", verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
        await foreach (var i in (dynamic)new C())
        {
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
            comp.VerifyDiagnostics(
                // (6,33): error CS8416: Cannot use a collection of dynamic type in an asynchronous foreach
                //         await foreach (var i in (dynamic)new C())
                Diagnostic(ErrorCode.ERR_BadDynamicAwaitForEach, "(dynamic)new C()").WithLocation(6, 33));
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
                // (12,33): error CS8411: Async foreach statement cannot operate on variables of type 'IAsyncEnumerable<int>' because 'IAsyncEnumerable<int>' does not contain a public instance definition for 'GetAsyncEnumerator'
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
        IAsyncEnumerator<T> GetAsyncEnumerator();
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
                // (18,33): error CS8412: Async foreach requires that the return type 'IAsyncEnumerator<int>' of 'IAsyncEnumerable<int>.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in collection)
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
                // (18,33): error CS8412: Async foreach requires that the return type 'IAsyncEnumerator<int>' of 'IAsyncEnumerable<int>.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in collection)
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
        await foreach (var i in new C())
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
                // (6,33): error CS8411: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
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
        public void TestMoveNextAsyncPatternViaExtensions()
        {
            string source = @"
public class C
{
    async System.Threading.Tasks.Task M()
    {
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
    {
        throw null;
    }
    public sealed class Enumerator
    {
        public int Current
        {
            get => throw null;
        }
    }
}
public static class Extensions
{
    public static System.Threading.Tasks.Task<bool> MoveNextAsync(this C.Enumerator e)
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS0117: 'C.Enumerator' does not contain a definition for 'MoveNextAsync'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator", "MoveNextAsync").WithLocation(6, 33),
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new C())
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
    public Enumerator GetAsyncEnumerator() => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,27): error CS8414: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'await foreach'?
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
                // (7,27): error CS8414: foreach statement cannot operate on variables of type 'IAsyncEnumerable<int>' because 'IAsyncEnumerable<int>' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'await foreach'?
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,33): error CS8415: Async foreach statement cannot operate on variables of type 'IEnumerable<int>' because 'IEnumerable<int>' does not contain a public instance definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'await foreach'?
                //         await foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMemberWrongAsync, "collection").WithArguments("System.Collections.Generic.IEnumerable<int>", "GetAsyncEnumerator").WithLocation(7, 33),
                // (7,9): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         await foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(7, 9)
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
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,27): error CS8414: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'await foreach'?
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_ForEachMissingMemberWrongAsync, "new C()").WithArguments("C", "GetEnumerator").WithLocation(6, 27)
                );
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,33): error CS8415: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'await foreach'?
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
    public Enumerator GetAsyncEnumerator()
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("C.Enumerator C.GetAsyncEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> C.Enumerator.MoveNextAsync()", info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Enumerator.Current { get; }", info.CurrentProperty.ToTestDisplayString());
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
        await foreach (ref var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator()
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,32): error CS8177: Async methods cannot have by-reference locals
                //         await foreach (ref var i in new C())
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
        await foreach (var i in new C())
        {
        }
    }
    public Enumerator GetAsyncEnumerator() => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
        public int* Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.UnsafeDebugDll);
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
    private Enumerator GetAsyncEnumerator()
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
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
    public Enumerator GetAsyncEnumerator()
            => throw null;
    public sealed class Enumerator
    {
        private System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,33): error CS0122: 'D.Enumerator.MoveNextAsync()' is inaccessible due to its protection level
                //         await foreach (var i in new D())
                Diagnostic(ErrorCode.ERR_BadAccess, "new D()").WithArguments("D.Enumerator.MoveNextAsync()").WithLocation(6, 33),
                // (6,33): error CS8412: Async foreach requires that the return type 'D.Enumerator' of 'D.GetAsyncEnumerator()' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new D())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new D()").WithArguments("D.Enumerator", "D.GetAsyncEnumerator()").WithLocation(6, 33)
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
    public Enumerator GetAsyncEnumerator()
            => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
        private int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,33): error CS0122: 'D.Enumerator.Current' is inaccessible due to its protection level
                //         await foreach (var i in new D()) { }
                Diagnostic(ErrorCode.ERR_BadAccess, "new D()").WithArguments("D.Enumerator.Current").WithLocation(6, 33),
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
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

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            await Task.Delay(10);
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2 Done");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void TestWithPattern_RefReturningCurrent()
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2 3 Done", verify: Verification.Fails);
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
    public Enumerator GetAsyncEnumerator()
        => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync()
            => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
            comp.VerifyDiagnostics(
                // (8,13): error CS1656: Cannot assign to 'i' because it is a 'foreach iteration variable'
                //             i = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "foreach iteration variable").WithLocation(8, 13)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            Write($""Dispose({i}) "");
            await new ValueTask(Task.Delay(10));
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(0) Got(1) NextAsync(1) Current(1) Got(2) NextAsync(2) Current(2) Got(3) NextAsync(3) Current(3) Got(4) NextAsync(4) Done",
                verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    public AsyncEnumerator GetAsyncEnumerator()
    {
        return new AsyncEnumerator(0);
    }
    public class AsyncEnumerator
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
            await new ValueTask(Task.Delay(10));
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Done", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> C.AsyncEnumerator.MoveNextAsync()", info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());

            var memberModel = model.GetMemberModel(foreachSyntax);
            var boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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

            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3)",
                verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
    public Enumerator GetAsyncEnumerator()
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

            var verifier = CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4)",
                verify: Verification.Skipped);

            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      464 (0x1d0)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_1,
                C.<Main>d__0 V_2,
                object V_3,
                System.IAsyncDisposable V_4,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_5,
                System.Threading.Tasks.ValueTask V_6,
                System.Exception V_7)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0035
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq        IL_0140
    IL_0013:  br.s       IL_0015
    // sequence point: {
    IL_0015:  nop
    // sequence point: foreach
    IL_0016:  nop
    // sequence point: new C()
    IL_0017:  ldarg.0
    IL_0018:  newobj     ""C..ctor()""
    IL_001d:  call       ""C.Enumerator C.GetAsyncEnumerator()""
    IL_0022:  stfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    // sequence point: <hidden>
    IL_0027:  ldarg.0
    IL_0028:  ldnull
    IL_0029:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.0
    IL_0030:  stfld      ""int C.<Main>d__0.<>s__3""
    // sequence point: <hidden>
    IL_0035:  nop
    .try
    {
      // sequence point: <hidden>
      IL_0036:  ldloc.0
      IL_0037:  brfalse.s  IL_00ac
      IL_0039:  br.s       IL_003b
      // sequence point: <hidden>
      IL_003b:  br.s       IL_006b
      // sequence point: var i
      IL_003d:  ldarg.0
      IL_003e:  ldarg.0
      IL_003f:  ldfld      ""C.Enumerator C.<Main>d__0.<>s__1""
      IL_0044:  callvirt   ""int C.Enumerator.Current.get""
      IL_0049:  stfld      ""int C.<Main>d__0.<i>5__4""
      // sequence point: {
      IL_004e:  nop
      // sequence point: Write($""Got({i}) "");
      IL_004f:  ldstr      ""Got({0}) ""
      IL_0054:  ldarg.0
      IL_0055:  ldfld      ""int C.<Main>d__0.<i>5__4""
      IL_005a:  box        ""int""
      IL_005f:  call       ""string string.Format(string, object)""
      IL_0064:  call       ""void System.Console.Write(string)""
      IL_0069:  nop
      // sequence point: }
      IL_006a:  nop
      // sequence point: in
      IL_006b:  ldarg.0
      IL_006c:  ldfld      ""C.Enumerator C.<Main>d__0.<>s__1""
      IL_0071:  callvirt   ""System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()""
      IL_0076:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
      IL_007b:  stloc.1
      // sequence point: <hidden>
      IL_007c:  ldloca.s   V_1
      IL_007e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
      IL_0083:  brtrue.s   IL_00c8
      IL_0085:  ldarg.0
      IL_0086:  ldc.i4.0
      IL_0087:  dup
      IL_0088:  stloc.0
      IL_0089:  stfld      ""int C.<Main>d__0.<>1__state""
      // async: yield
      IL_008e:  ldarg.0
      IL_008f:  ldloc.1
      IL_0090:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_0095:  ldarg.0
      IL_0096:  stloc.2
      IL_0097:  ldarg.0
      IL_0098:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
      IL_009d:  ldloca.s   V_1
      IL_009f:  ldloca.s   V_2
      IL_00a1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Main>d__0)""
      IL_00a6:  nop
      IL_00a7:  leave      IL_01cf
      // async: resume
      IL_00ac:  ldarg.0
      IL_00ad:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00b2:  stloc.1
      IL_00b3:  ldarg.0
      IL_00b4:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00b9:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
      IL_00bf:  ldarg.0
      IL_00c0:  ldc.i4.m1
      IL_00c1:  dup
      IL_00c2:  stloc.0
      IL_00c3:  stfld      ""int C.<Main>d__0.<>1__state""
      IL_00c8:  ldarg.0
      IL_00c9:  ldloca.s   V_1
      IL_00cb:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
      IL_00d0:  stfld      ""bool C.<Main>d__0.<>s__5""
      IL_00d5:  ldarg.0
      IL_00d6:  ldfld      ""bool C.<Main>d__0.<>s__5""
      IL_00db:  brtrue     IL_003d
      // sequence point: <hidden>
      IL_00e0:  leave.s    IL_00ec
    }
    catch object
    {
      // sequence point: <hidden>
      IL_00e2:  stloc.3
      IL_00e3:  ldarg.0
      IL_00e4:  ldloc.3
      IL_00e5:  stfld      ""object C.<Main>d__0.<>s__2""
      IL_00ea:  leave.s    IL_00ec
    }
    // sequence point: <hidden>
    IL_00ec:  ldarg.0
    IL_00ed:  ldfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    IL_00f2:  isinst     ""System.IAsyncDisposable""
    IL_00f7:  stloc.s    V_4
    IL_00f9:  ldloc.s    V_4
    IL_00fb:  brfalse.s  IL_0165
    IL_00fd:  ldloc.s    V_4
    IL_00ff:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_0104:  stloc.s    V_6
    IL_0106:  ldloca.s   V_6
    IL_0108:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_010d:  stloc.s    V_5
    // sequence point: <hidden>
    IL_010f:  ldloca.s   V_5
    IL_0111:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_0116:  brtrue.s   IL_015d
    IL_0118:  ldarg.0
    IL_0119:  ldc.i4.1
    IL_011a:  dup
    IL_011b:  stloc.0
    IL_011c:  stfld      ""int C.<Main>d__0.<>1__state""
    // async: yield
    IL_0121:  ldarg.0
    IL_0122:  ldloc.s    V_5
    IL_0124:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_0129:  ldarg.0
    IL_012a:  stloc.2
    IL_012b:  ldarg.0
    IL_012c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_0131:  ldloca.s   V_5
    IL_0133:  ldloca.s   V_2
    IL_0135:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<Main>d__0)""
    IL_013a:  nop
    IL_013b:  leave      IL_01cf
    // async: resume
    IL_0140:  ldarg.0
    IL_0141:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_0146:  stloc.s    V_5
    IL_0148:  ldarg.0
    IL_0149:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_014e:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_0154:  ldarg.0
    IL_0155:  ldc.i4.m1
    IL_0156:  dup
    IL_0157:  stloc.0
    IL_0158:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_015d:  ldloca.s   V_5
    IL_015f:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_0164:  nop
    // sequence point: <hidden>
    IL_0165:  ldarg.0
    IL_0166:  ldfld      ""object C.<Main>d__0.<>s__2""
    IL_016b:  stloc.3
    IL_016c:  ldloc.3
    IL_016d:  brfalse.s  IL_018a
    IL_016f:  ldloc.3
    IL_0170:  isinst     ""System.Exception""
    IL_0175:  stloc.s    V_7
    IL_0177:  ldloc.s    V_7
    IL_0179:  brtrue.s   IL_017d
    IL_017b:  ldloc.3
    IL_017c:  throw
    IL_017d:  ldloc.s    V_7
    IL_017f:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_0184:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_0189:  nop
    IL_018a:  ldarg.0
    IL_018b:  ldfld      ""int C.<Main>d__0.<>s__3""
    IL_0190:  pop
    IL_0191:  ldarg.0
    IL_0192:  ldnull
    IL_0193:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_0198:  ldarg.0
    IL_0199:  ldnull
    IL_019a:  stfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    IL_019f:  leave.s    IL_01bb
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_01a1:  stloc.s    V_7
    IL_01a3:  ldarg.0
    IL_01a4:  ldc.i4.s   -2
    IL_01a6:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_01ab:  ldarg.0
    IL_01ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_01b1:  ldloc.s    V_7
    IL_01b3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_01b8:  nop
    IL_01b9:  leave.s    IL_01cf
  }
  // sequence point: }
  IL_01bb:  ldarg.0
  IL_01bc:  ldc.i4.s   -2
  IL_01be:  stfld      ""int C.<Main>d__0.<>1__state""
  // sequence point: <hidden>
  IL_01c3:  ldarg.0
  IL_01c4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_01c9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01ce:  nop
  IL_01cf:  ret
}
", sequencePoints: "C+<Main>d__0.MoveNext", source: source + s_interfaces);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4)", verify: Verification.Skipped);
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
        await foreach (var i in new C())
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
        await foreach (var i in new C())
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
    IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator()
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
        => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (13,33): error CS8413: Async foreach statement cannot operate on variables of type 'C' because it implements multiple instantiations of 'IAsyncEnumerable<T>'; try casting to a specific interface instantiation
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_MultipleIAsyncEnumOfT, "new C()").WithArguments("C", "System.Collections.Generic.IAsyncEnumerable<T>").WithLocation(13, 33)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
        await foreach (var i in new C())
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
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4)", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
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
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4) Done", verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
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
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Continue(2) NextAsync(2) Current(3) Continue(3) NextAsync(3) Current(4) Break Dispose(4) Done",
                verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
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
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Continue(2) NextAsync(2) Current(3) Continue(3) NextAsync(3) Current(4) Goto Dispose(4) Done",
                verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
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
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(0) Got(1) NextAsync(1) Current(1) Got(2) NextAsync(2) Current(2) Got(3) NextAsync(3) Current(3) Got(4) NextAsync(4) Dispose(4) Done",
                verify: Verification.Skipped);
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
        await foreach (var i in null)
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
                //         await foreach (var i in null)
                Diagnostic(ErrorCode.ERR_NullNotValid, "null").WithLocation(8, 33)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Success", verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
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
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Try NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4) Done", verify: Verification.Skipped);
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
            await foreach (var i in new C())
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
                //             await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadFinallyLeave, "await").WithLocation(13, 13)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
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
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Convert(1) Got(1) NextAsync(1) Current(2) Convert(2) Got(2) NextAsync(2) Dispose(3) Done", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
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
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
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
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Dispose(3)", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
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
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
    {
        throw new System.Exception();
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Success", verify: Verification.Skipped);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
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

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Deconstruct(1) Got(1,-1) NextAsync(1) Current(2) Deconstruct(2) Got(2,-2) NextAsync(2) Dispose(3) Done", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
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
        await foreach (var (i, j) in new C())
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
                // (7,9): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         await foreach (var (i, j) in new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(7, 9)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<(string, int)> IAsyncEnumerable<(string, int)>.GetAsyncEnumerator()
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
            await new ValueTask(Task.Delay(10));
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1,-1) NextAsync(1) Current(2) Got(2,-2) NextAsync(2) Dispose(3) Done", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)> System.Collections.Generic.IAsyncEnumerable<(System.String, System.Int32)>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)>.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("(System.String, System.Int32) System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)>.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("(System.String, System.Int32)", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator()
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

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1,-1) NextAsync(1) Current(2) Got(2,-2) NextAsync(2) Dispose(3) Done", verify: Verification.Skipped);
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
        await foreach (var i in new C())
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
        public int Current { get => throw null; }
        [System.Obsolete]
        public Task<bool> MoveNextAsync() => throw null;
        [System.Obsolete]
        public ValueTask DisposeAsync() => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,15): warning CS0612: 'C.GetAsyncEnumerator()' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.GetAsyncEnumerator()").WithLocation(7, 15),
                // (7,15): warning CS0612: 'C.AsyncEnumerator.MoveNextAsync()' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.AsyncEnumerator.MoveNextAsync()").WithLocation(7, 15),
                // (7,15): warning CS0612: 'C.AsyncEnumerator.Current' is obsolete
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.AsyncEnumerator.Current").WithLocation(7, 15)
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
        await foreach (var i in c)
        {
        }
    }
    public IAsyncEnumerator<int> GetAsyncEnumerator() => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
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
    public IAsyncEnumerator<int> GetAsyncEnumerator()
    {
        throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,27): error CS8414: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'await foreach'?
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_ForEachMissingMemberWrongAsync, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            await new ValueTask(Task.Delay(10));
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got NextAsync(1) Current(2) Got NextAsync(2) Current(3) Got NextAsync(3) Dispose(4)", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
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
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
    T Current { get; }
    Task<bool> MoveNextAsync();
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got NextAsync(1) Current(2) Got NextAsync(2) Current(3) Got NextAsync(3)", verify: Verification.Skipped);

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("IMyAsyncEnumerator<System.Int32> ICollection<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> IMyAsyncEnumerator<System.Int32>.MoveNextAsync()",
                info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 IMyAsyncEnumerator<System.Int32>.Current { get; }",
                info.CurrentProperty.ToTestDisplayString());
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
  // Code size      460 (0x1cc)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_1,
                C.<Main>d__0 V_2,
                object V_3,
                System.IAsyncDisposable V_4,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_5,
                System.Threading.Tasks.ValueTask V_6,
                System.Exception V_7)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0041
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq        IL_013c
    IL_0013:  br.s       IL_0015
    // sequence point: {
    IL_0015:  nop
    // sequence point: ICollection<int> c = new Collection<int>();
    IL_0016:  ldarg.0
    IL_0017:  newobj     ""Collection<int>..ctor()""
    IL_001c:  stfld      ""ICollection<int> C.<Main>d__0.<c>5__1""
    // sequence point: foreach
    IL_0021:  nop
    // sequence point: c
    IL_0022:  ldarg.0
    IL_0023:  ldarg.0
    IL_0024:  ldfld      ""ICollection<int> C.<Main>d__0.<c>5__1""
    IL_0029:  callvirt   ""IMyAsyncEnumerator<int> ICollection<int>.GetAsyncEnumerator()""
    IL_002e:  stfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    // sequence point: <hidden>
    IL_0033:  ldarg.0
    IL_0034:  ldnull
    IL_0035:  stfld      ""object C.<Main>d__0.<>s__3""
    IL_003a:  ldarg.0
    IL_003b:  ldc.i4.0
    IL_003c:  stfld      ""int C.<Main>d__0.<>s__4""
    // sequence point: <hidden>
    IL_0041:  nop
    .try
    {
      // sequence point: <hidden>
      IL_0042:  ldloc.0
      IL_0043:  brfalse.s  IL_00a8
      IL_0045:  br.s       IL_0047
      // sequence point: <hidden>
      IL_0047:  br.s       IL_0067
      // sequence point: var i
      IL_0049:  ldarg.0
      IL_004a:  ldarg.0
      IL_004b:  ldfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
      IL_0050:  callvirt   ""int IMyAsyncEnumerator<int>.Current.get""
      IL_0055:  stfld      ""int C.<Main>d__0.<i>5__5""
      // sequence point: {
      IL_005a:  nop
      // sequence point: Write($""Got "");
      IL_005b:  ldstr      ""Got ""
      IL_0060:  call       ""void System.Console.Write(string)""
      IL_0065:  nop
      // sequence point: }
      IL_0066:  nop
      // sequence point: in
      IL_0067:  ldarg.0
      IL_0068:  ldfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
      IL_006d:  callvirt   ""System.Threading.Tasks.Task<bool> IMyAsyncEnumerator<int>.MoveNextAsync()""
      IL_0072:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
      IL_0077:  stloc.1
      // sequence point: <hidden>
      IL_0078:  ldloca.s   V_1
      IL_007a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
      IL_007f:  brtrue.s   IL_00c4
      IL_0081:  ldarg.0
      IL_0082:  ldc.i4.0
      IL_0083:  dup
      IL_0084:  stloc.0
      IL_0085:  stfld      ""int C.<Main>d__0.<>1__state""
      // async: yield
      IL_008a:  ldarg.0
      IL_008b:  ldloc.1
      IL_008c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_0091:  ldarg.0
      IL_0092:  stloc.2
      IL_0093:  ldarg.0
      IL_0094:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
      IL_0099:  ldloca.s   V_1
      IL_009b:  ldloca.s   V_2
      IL_009d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Main>d__0)""
      IL_00a2:  nop
      IL_00a3:  leave      IL_01cb
      // async: resume
      IL_00a8:  ldarg.0
      IL_00a9:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00ae:  stloc.1
      IL_00af:  ldarg.0
      IL_00b0:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
      IL_00b5:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
      IL_00bb:  ldarg.0
      IL_00bc:  ldc.i4.m1
      IL_00bd:  dup
      IL_00be:  stloc.0
      IL_00bf:  stfld      ""int C.<Main>d__0.<>1__state""
      IL_00c4:  ldarg.0
      IL_00c5:  ldloca.s   V_1
      IL_00c7:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
      IL_00cc:  stfld      ""bool C.<Main>d__0.<>s__6""
      IL_00d1:  ldarg.0
      IL_00d2:  ldfld      ""bool C.<Main>d__0.<>s__6""
      IL_00d7:  brtrue     IL_0049
      // sequence point: <hidden>
      IL_00dc:  leave.s    IL_00e8
    }
    catch object
    {
      // sequence point: <hidden>
      IL_00de:  stloc.3
      IL_00df:  ldarg.0
      IL_00e0:  ldloc.3
      IL_00e1:  stfld      ""object C.<Main>d__0.<>s__3""
      IL_00e6:  leave.s    IL_00e8
    }
    // sequence point: <hidden>
    IL_00e8:  ldarg.0
    IL_00e9:  ldfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    IL_00ee:  isinst     ""System.IAsyncDisposable""
    IL_00f3:  stloc.s    V_4
    IL_00f5:  ldloc.s    V_4
    IL_00f7:  brfalse.s  IL_0161
    IL_00f9:  ldloc.s    V_4
    IL_00fb:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_0100:  stloc.s    V_6
    IL_0102:  ldloca.s   V_6
    IL_0104:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_0109:  stloc.s    V_5
    // sequence point: <hidden>
    IL_010b:  ldloca.s   V_5
    IL_010d:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_0112:  brtrue.s   IL_0159
    IL_0114:  ldarg.0
    IL_0115:  ldc.i4.1
    IL_0116:  dup
    IL_0117:  stloc.0
    IL_0118:  stfld      ""int C.<Main>d__0.<>1__state""
    // async: yield
    IL_011d:  ldarg.0
    IL_011e:  ldloc.s    V_5
    IL_0120:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_0125:  ldarg.0
    IL_0126:  stloc.2
    IL_0127:  ldarg.0
    IL_0128:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_012d:  ldloca.s   V_5
    IL_012f:  ldloca.s   V_2
    IL_0131:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<Main>d__0)""
    IL_0136:  nop
    IL_0137:  leave      IL_01cb
    // async: resume
    IL_013c:  ldarg.0
    IL_013d:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_0142:  stloc.s    V_5
    IL_0144:  ldarg.0
    IL_0145:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__2""
    IL_014a:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_0150:  ldarg.0
    IL_0151:  ldc.i4.m1
    IL_0152:  dup
    IL_0153:  stloc.0
    IL_0154:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_0159:  ldloca.s   V_5
    IL_015b:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_0160:  nop
    // sequence point: <hidden>
    IL_0161:  ldarg.0
    IL_0162:  ldfld      ""object C.<Main>d__0.<>s__3""
    IL_0167:  stloc.3
    IL_0168:  ldloc.3
    IL_0169:  brfalse.s  IL_0186
    IL_016b:  ldloc.3
    IL_016c:  isinst     ""System.Exception""
    IL_0171:  stloc.s    V_7
    IL_0173:  ldloc.s    V_7
    IL_0175:  brtrue.s   IL_0179
    IL_0177:  ldloc.3
    IL_0178:  throw
    IL_0179:  ldloc.s    V_7
    IL_017b:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_0180:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_0185:  nop
    IL_0186:  ldarg.0
    IL_0187:  ldfld      ""int C.<Main>d__0.<>s__4""
    IL_018c:  pop
    IL_018d:  ldarg.0
    IL_018e:  ldnull
    IL_018f:  stfld      ""object C.<Main>d__0.<>s__3""
    IL_0194:  ldarg.0
    IL_0195:  ldnull
    IL_0196:  stfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    IL_019b:  leave.s    IL_01b7
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_019d:  stloc.s    V_7
    IL_019f:  ldarg.0
    IL_01a0:  ldc.i4.s   -2
    IL_01a2:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_01a7:  ldarg.0
    IL_01a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_01ad:  ldloc.s    V_7
    IL_01af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_01b4:  nop
    IL_01b5:  leave.s    IL_01cb
  }
  // sequence point: }
  IL_01b7:  ldarg.0
  IL_01b8:  ldc.i4.s   -2
  IL_01ba:  stfld      ""int C.<Main>d__0.<>1__state""
  // sequence point: <hidden>
  IL_01bf:  ldarg.0
  IL_01c0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_01c5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01ca:  nop
  IL_01cb:  ret
}
", sequencePoints: "C+<Main>d__0.MoveNext", source: source);
        }
    }
}
