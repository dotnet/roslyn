// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
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
        System.Threading.Tasks.Task<bool> WaitForNextAsync();
        T TryGetNext(out bool success);
    }
}
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.Task DisposeAsync();
    }
}
";

        [Fact]
        void TestWithMissingPattern()
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
                // (6,33): error CS9001: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetAsyncEnumerator'
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
                );
        }

        [Fact]
        void TestWithStaticGetEnumerator()
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
                // (6,33): warning CS0279: 'C' does not implement the 'async streams' pattern. 'C.GetAsyncEnumerator()' is either static or not public.
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.WRN_PatternStaticOrInaccessible, "new C()").WithArguments("C", "async streams", "C.GetAsyncEnumerator()").WithLocation(6, 33),
                // (6,33): error CS9001: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetAsyncEnumerator'
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
                );
        }

        [Fact]
        void TestWithInaccessibleGetEnumerator()
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
        void TestWithObsoletePatternMethods()
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
        void TestWithStaticWaitForNextAsync()
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
        void TestWithStaticTryGetNext()
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
        void TestWithNonPublicWaitForNextAsync()
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
        void TestWithNonPublicTryGetNext()
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
        void TestWaitForNextAsync_ReturnsTask()
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
        void TestWaitForNextAsync_ReturnsTaskOfInt()
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
        void TestWaitForNextAsync_WithOptionalParameter()
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
        void TestTryGetNext_WithOptionalParameter()
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
        void TestTryGetNext_WithByValParameter()
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
        void TestTryGetNext_WithIntParameter()
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
        void TestTryGetNext_WithVoidReturn()
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
        void TestWithNonConvertibleElementType()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
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
        void TestWithNonConvertibleElementType2()
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
        public Task DisposeAsync()
            => throw null;
    }
}
class Element
{
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (7,9): error CS0030: Cannot convert type 'int' to 'Element'
                //         foreach await (Element i in new C())
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("int", "Element").WithLocation(7, 9)
                );
        }

        [Fact]
        void TestWithExplicitlyConvertibleElementType()
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
        public async Task DisposeAsync()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics( );
            // PROTOTYPE(async-streams) Convert(0) is here because we're converting the result even if TryGetNext returned false
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Convert(11) Got(11) Next(12) Convert(12) Got(12) Next(13) Convert(0) NextAsync(13) Next(24) Convert(24) Got(24) Next(25) Convert(25) Got(25) Next(26) Convert(0) NextAsync(26) Dispose(37)");
        }

        [Fact]
        void TestWithIncompleteInterface()
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
        void TestWithIncompleteInterface2()
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
        void TestWithIncompleteInterface3()
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
        void TestGetAsyncEnumeratorPatternViaExtensions()
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
        void TestGetEnumeratorPatternViaExtensions()
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
        void TestWaitForNextAsyncPatternViaExtensions()
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
        void TestTryGetNextPatternViaExtensions()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
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
        void TestMoveNextPatternViaExtensions()
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
        void TestWithSyncPattern()
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
        void TestRegularForeachWithAsyncPattern()
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
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetEnumerator'
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(6, 27)
                );
        }

        [Fact]
        void TestRegularForeachWithAsyncInterface()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,27): error CS1579: foreach statement cannot operate on variables of type 'IAsyncEnumerable<int>' because 'IAsyncEnumerable<int>' does not contain a public definition for 'GetEnumerator'
                //         foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "collection").WithArguments("System.Collections.Generic.IAsyncEnumerable<int>", "GetEnumerator").WithLocation(7, 27)
                );
        }

        [Fact]
        void TestWithSyncInterfaceInRegularMethod()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,33): error CS9001: Async foreach statement cannot operate on variables of type 'IEnumerable<int>' because 'IEnumerable<int>' does not contain a public definition for 'GetAsyncEnumerator'
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_AsyncForEachMissingMember, "collection").WithArguments("System.Collections.Generic.IEnumerable<int>", "GetAsyncEnumerator").WithLocation(7, 33),
                // (7,17): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(7, 17)
                );
        }

        [Fact]
        void TestWithPattern()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
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
        void TestWithPattern_WithStruct_SimpleImplementation()
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
        public async Task DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            // Note: NextAsync(3) is followed by Next(3) as NextAsync incremented a copy of the enumerator struct
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(0) Got(1) Next(1) Got(2) Next(2) NextAsync(3) Next(3) Got(4) Next(4) Got(5) Next(5) NextAsync(6) Next(6) Got(7) Next(7) Got(8) Next(8) NextAsync(9) Done");
        }

        [Fact]
        void TestWithPattern_WithUnsealed()
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
        public async Task DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11) Next(12) Got(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Got(25) Next(26) NextAsync(26)");
        }

        [Fact]
        void TestWithPattern_WithUnsealed_WithIAsyncDisposable()
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
        public async Task DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11) Next(12) Got(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Got(25) Next(26) NextAsync(26) Dispose(37)");
        }

        [Fact]
        void TestWithPattern_WithIAsyncDisposable()
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
        public async Task DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11) Next(12) Got(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Got(25) Next(26) NextAsync(26) Dispose(37)");
        }

        [Fact]
        void TestWithPattern_WithIAsyncDisposableUseSiteError()
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
        public async Task DisposeAsync()
        {
            await Task.Delay(10);
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
            var lib = CreateCompilationWithMscorlib46(enumerator + s_interfaces);
            lib.VerifyDiagnostics();

            var comp = CreateCompilationWithMscorlib46(source, references: new[] { lib.EmitToImageReference() });
            comp.MakeTypeMissing(WellKnownType.System_IAsyncDisposable);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.False(internalInfo.NeedsDisposeMethod);

            // PROTOTYPE(async-streams) I didn't manage to hit the case I wanted in ForEachLoopBinder.GetEnumeratorInfo:
            //if (!enumeratorType.IsSealed ||
            //    this.Conversions.ClassifyImplicitConversionFromType(enumeratorType,
            //        IsAsync ? this.Compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable) : this.Compilation.GetSpecialType(SpecialType.System_IDisposable),
            //        ref useSiteDiagnostics).IsImplicit)
            //{
            //    builder.NeedsDisposeMethod = true;
            //    diagnostics.AddRange(useSiteDiagnosticBag);
            //}
        }

        [Fact]
        void TestWithMultipleInterface()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,33): error CS9003: Async foreach statement cannot operate on variables of type 'C' because it implements multiple instantiations of 'IAsyncEnumerable<T>'; try casting to a specific interface instantiation
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_MultipleIAsyncEnumOfT, "new C()").WithArguments("C", "System.Collections.Generic.IAsyncEnumerable<T>").WithLocation(7, 33)
                );
        }

        [Fact]
        void TestWithMultipleImplementations()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (13,33): error CS9003: Async foreach statement cannot operate on variables of type 'C' because it implements multiple instantiations of 'IAsyncEnumerable<T>'; try casting to a specific interface instantiation
                //         foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_MultipleIAsyncEnumOfT, "new C()").WithArguments("C", "System.Collections.Generic.IAsyncEnumerable<T>").WithLocation(13, 33)
                );
        }

        [Fact]
        void TestWithInterface()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async Task DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11) Next(12) Got(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Got(25) Next(26) NextAsync(26) Dispose(37)");

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.Task System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [Fact]
        void TestWithInterface_WithEarlyCompletion1()
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
        public Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            return Task.FromResult(i < 30); // return a completed task
        }
        public Task DisposeAsync()
        {
            Write($""Dispose({i}) "");
            return Task.CompletedTask; // return a completed task
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(3) Next(14) Got(14) Next(15) Got(15) Next(16) NextAsync(16) Next(27) Got(27) Next(28) Got(28) Next(29) NextAsync(29) Dispose(40) Done");
        }

        [Fact]
        void TestWithInterface_WithBreakAndContinue()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async Task DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Continue(11) Next(12) Continue(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Break Dispose(26) Done");
        }

        [Fact]
        void TestWithInterface_WithGoto()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async Task DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Continue(11) Next(12) Continue(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Break Dispose(26) Done");
        }

        [Fact]
        void TestWithInterface_WithStruct()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async Task DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            // Note: NextAsync(3) is followed by Next(3) as NextAsync incremented a copy of the enumerator struct
            // PROTOTYPE(async-streams) This seems strange, as I would expect we'd be handling as an IAsyncEnumerator<int> rather than as a struct
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(0) Got(1) Next(1) Got(2) Next(2) NextAsync(3) Next(3) Got(4) Next(4) Got(5) Next(5) NextAsync(6) Next(6) Got(7) Next(7) Got(8) Next(8) NextAsync(9) Dispose(9) Done");
        }

        [Fact]
        void TestWithInterface_WithStruct_ManualIteration()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async Task DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            // Note: NextAsync(3) is followed by Next(3) as NextAsync incremented a copy of the enumerator struct
            // PROTOTYPE(async-streams) This seems strange, as I would expect we'd be handling as an IAsyncEnumerator<int> rather than as a struct
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(0) Got(1) Next(1) Got(2) Next(2) NextAsync(3) Next(3) Got(4) Next(4) Got(5) Next(5) NextAsync(6) Next(6) Got(7) Next(7) Got(8) Next(8) NextAsync(9) Dispose(9) Done");
        }

        [Fact]
        void TestWithInterface_WithStruct2()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 10;
            bool more = await Task.FromResult(i < 13);
            return more;
        }
        public async Task DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(0) Got(0) Next(0) NextAsync(1) Next(1) Got(1) Next(1) NextAsync(2) Next(2) Got(2) Next(2) NextAsync(3) Dispose(3) Done");
        }

        [Fact]
        void TestWithNullLiteralCollection()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (8,33): error CS0186: Use of null is not valid in this context
                //         foreach await (var i in null)
                Diagnostic(ErrorCode.ERR_NullNotValid, "null").WithLocation(8, 33)
                );
        }

        [Fact]
        void TestWithNullCollection()
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
        public Task<bool> WaitForNextAsync()
        {
            throw new System.Exception();
        }
        public Task DisposeAsync()
        {
            throw new System.Exception();
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Success");
        }

        [Fact]
        void TestInCatch()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async Task DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Try NextAsync(0) Next(0) Got(1) Next(1) Got(2) Next(2) NextAsync(3) Next(3) Got(4) Next(4) Got(5) Next(5) NextAsync(6) Next(6) Got(7) Next(7) Got(8) Next(8) NextAsync(9) Dispose(9) Done");
        }

        [Fact]
        void TestInFinally()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (13,13): error CS0157: Control cannot leave the body of a finally clause
                //             foreach await (var i in new C())
                Diagnostic(ErrorCode.ERR_BadFinallyLeave, "foreach").WithLocation(13, 13)
                );
        }

        [Fact]
        void TestWithConversionToElement()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async Task DisposeAsync()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Convert(12) Got(12) Next(12) Convert(13) NextAsync(13) Dispose(24) Done");

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.Task System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ExplicitUserDefined, info.ElementConversion.Kind);
            Assert.Equal("Element Element.op_Implicit(System.Int32 value)", info.ElementConversion.MethodSymbol.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [Fact]
        void TestWithNullableCollection()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async Task DisposeAsync()
        {
            Write($""Disp"");
            await Task.Delay(10);
            Write($""ose({i}) "");
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11) Next(12) Got(12) Next(13) NextAsync(13) Next(24) Got(24) Next(25) Got(25) Next(26) NextAsync(26) Dispose(37)");

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.Task System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [Fact]
        void TestWithNullableCollection2()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Success");
        }

        [Fact]
        void TestWithInterfaceAndDeconstruction()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async Task DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Delay(10);
        }
    }
}
public static class Extensions
{
    public static void Deconstruct(this int i, out string x1, out int x2) { Write($""Deconstruct({i}) ""); x1 = i.ToString(); x2 = -i; }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            // PROTOTYPE(async-streams) The deconstruction should be after the check for success (ie. it should not occur if TryGetNext got no item)
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Deconstruct(11) Got(11,-11) Next(12) Deconstruct(12) Got(12,-12) Next(13) Deconstruct(0) NextAsync(13) Dispose(24) Done");

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.Task System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [Fact]
        void TestWithDeconstructionInNonAsyncMethod()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,17): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         foreach await (var (i, j) in new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(7, 17)
                );
        }

        [Fact]
        void TestWithPatternAndDeconstructionOfTuple()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async Task DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Delay(10);
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(11,-11) Next(12) Got(12,-12) Next(13) NextAsync(13) Dispose(24) Done");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)> System.Collections.Generic.IAsyncEnumerable<(System.String, System.Int32)>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("(System.String, System.Int32) System.Collections.Generic.IAsyncEnumerator<(System.String, System.Int32)>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.Task System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("(System.String, System.Int32)", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
        }

        [Fact]
        void TestWithInterfaceAndDeconstruction_ManualIteration()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 20);
            return more;
        }
        public async Task DisposeAsync()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got(12,-12) Next(12) NextAsync(13) Dispose(24) Done");
        }

        [Fact]
        void TestWithPatternAndObsolete()
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
        public Task DisposeAsync()
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
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
        void TestWithUnassignedCollection()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (8,33): error CS0165: Use of unassigned local variable 'c'
                //         foreach await (var i in c)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(8, 33)
                );
        }

        [Fact]
        void TestInRegularForeach()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (7,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetEnumerator'
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27)
                );
            // PROTOTYPE(async-streams) We should offer a better error message, to guide users towards async-foreach? Maybe code fixer?
        }

        [Fact]
        void TestWithGenericCollection()
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
        public async Task<bool> WaitForNextAsync()
        {
            Write($""NextAsync({i}) "");
            i = i + 11;
            bool more = await Task.FromResult(i < 30);
            return more;
        }
        public async Task DisposeAsync()
        {
            Write($""Dispose({i}) "");
            await Task.Delay(10);
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got Next(12) NextAsync(13) Next(24) Got Next(25) NextAsync(26) Dispose(37)");

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator()",
                info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Threading.Tasks.Task<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.WaitForNextAsync()",
                info.WaitForNextAsyncMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.TryGetNext(out System.Boolean success)",
                info.TryGetNextMethod.ToTestDisplayString());
            Assert.Null(info.CurrentProperty);
            Assert.Null(info.MoveNextMethod);
            Assert.Equal("System.Threading.Tasks.Task System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [Fact]
        void TestWithInterfaceImplementingPattern()
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
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Next(11) Got Next(12) NextAsync(13) Next(24) Got Next(25) NextAsync(26)");

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
            Assert.Equal("System.Threading.Tasks.Task System.IAsyncDisposable.DisposeAsync()", info.DisposeMethod.ToTestDisplayString());
            Assert.Equal("System.Int32", info.ElementType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, info.ElementConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);

            var memberModel = model.GetMemberModel(foreachSyntax);
            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(foreachSyntax);
            ForEachEnumeratorInfo internalInfo = boundNode.EnumeratorInfoOpt;
            Assert.True(internalInfo.NeedsDisposeMethod);
        }

        [Fact]
        public void TestFoEachStatementInfo_IEquatable()
        {
            // PROTOTYPE(async-streams) test ForEachStatementInfo equality and such
        }

        // PROTOTYPE(async-streams) More test ideas
        // block dynamic

        // test with captures:
//        int[] values = { 7, 9, 13 };
//        Action f = null;
//foreach (var value in values)
//{
//    if (f == null) f = () => Console.WriteLine("First value: " + value); // captures 7, which ends up printed
//}
//    f();

        // pointer element type
        // verify that the foreach variables are readonly
        // nested deconstruction or tuple
        // pattern with inaccessible methods
        // review instrumentation
        // test various things that could go wrong with binding the await for DisposeAsync
        // throwing exception from enumerator, or from inside the async-foreach
        // collection has type from type parameter
        // collection type implements multiple IAsyncEnumerable<T>'s
        // semantic model
        // IOperation
        // IDE
        // scripting?
        // foreach on restricted type (like ref struct) in async or iterator method
        // foreach on restricted type in a regular method

        // Misc other test ideas:
        // WaitForMoveNext with task-like type (see IsCustomTaskType). Would that provide any benefits?
        // DisposeAsync with task-like return
        // Verify that async-dispose doesn't have a similar bug with struct resource
        // cleanup: use statement lists for async-using, instead of blocks
        // IAsyncEnumerable has an 'out' type parameter, any tests I need to do related to that?
        // spec: struct case should be blocked?
        // spec: extension methods don't contribute
    }
}
