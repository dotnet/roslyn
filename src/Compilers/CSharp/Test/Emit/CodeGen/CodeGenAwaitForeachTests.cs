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

            comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.RegularPreview);
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
                // (8,33): error CS8411: Async foreach statement cannot operate on variables of type 'IAsyncEnumerator<int>' because 'IAsyncEnumerator<int>' does not contain a suitable public instance definition for 'GetAsyncEnumerator'
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
            var comp_checked = CreateCompilationWithTasksExtensions(new[] { source.Replace("REPLACE", "checked"), s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp_checked.VerifyDiagnostics();
            CompileAndVerify(comp_checked, expectedOutput: "overflow");

            var comp_unchecked = CreateCompilationWithTasksExtensions(new[] { source.Replace("REPLACE", "unchecked"), s_IAsyncEnumerable }, options: TestOptions.DebugExe);
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
                // (6,33): error CS8411: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance definition for 'GetAsyncEnumerator'
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
                // (6,33): error CS8411: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance definition for 'GetAsyncEnumerator'
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
                // (6,33): warning CS0279: 'C' does not implement the 'async streams' pattern. 'C.GetAsyncEnumerator(System.Threading.CancellationToken)' is either static or not public.
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_PatternStaticOrInaccessible, "new C()").WithArguments("C", "async streams", "C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(6, 33),
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
        public async System.Threading.Tasks.Task<bool> MoveNextAsync(int ok = 0)
        {
            System.Console.Write(""MoveNextAsync"");
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
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync");
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
            System.Console.Write(""MoveNextAsync"");
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
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync");
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

            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(1) Convert(1) Got(1) NextAsync(1) Current(2) Convert(2) Got(2) NextAsync(2) Current(3) Convert(3) Got(3) NextAsync(3) Dispose(4)");
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
            CompileAndVerify(comp, expectedOutput: "NextAsync(1) Current(1) Got(1) NextAsync(2) Current(2) Got(2) NextAsync(3) Current(3) Got(3) NextAsync(4) Dispose(4)");
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
                // (12,33): error CS8411: Async foreach statement cannot operate on variables of type 'IAsyncEnumerable<int>' because 'IAsyncEnumerable<int>' does not contain a suitable public instance definition for 'GetAsyncEnumerator'
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
            // Pattern-based lookup does not bind extension methods at the moment
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8411: Async foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(6, 33)
                );
        }

        [Fact]
        public void TestGetAsyncEnumeratorPatternViaAmbiguousExtensions()
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
public static class Extensions1
{
    public static C.Enumerator GetAsyncEnumerator(this C c)
    {
        throw null;
    }
}
public static class Extensions2
{
    public static C.Enumerator GetAsyncEnumerator(this C c)
    {
        throw null;
    }
}";

            // Pattern-based lookup does not bind extension methods at the moment
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance definition for 'GetAsyncEnumerator'
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
        _ = (new C()).GetEnumerator();
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
            // Pattern-based lookup does not bind extension methods at the moment
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
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
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
                // (6,33): error CS8412: Async foreach requires that the return type 'C.Enumerator' of 'C.GetAsyncEnumerator(System.Threading.CancellationToken)' must have a suitable public MoveNextAsync method and public Current property
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetAsyncEnumerator(System.Threading.CancellationToken)").WithLocation(6, 33)
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
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default) => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
        public int Current { get => throw null; }
    }
}";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,27): error CS8414: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'await foreach' rather than 'foreach'?
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
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
            comp.VerifyDiagnostics(
                // (7,9): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         await foreach (var i in collection)
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(7, 9),
                // (7,33): error CS8415: Asynchronous foreach statement cannot operate on variables of type 'IEnumerable<int>' because 'IEnumerable<int>' does not contain a public instance definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'await foreach'?
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
                // (6,27): error CS8414: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'await foreach'?
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
                // (6,33): error CS8415: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'await foreach'?
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
}";
            var comp = CreateCompilationWithTasksExtensions(source + s_IAsyncEnumerable);
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
        }

        [Fact]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
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
            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(0) Got(1) NextAsync(1) Current(1) Got(2) NextAsync(2) Current(2) Got(3) NextAsync(3) Current(3) Got(4) NextAsync(4) DisposeAsync Done");
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
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4) Done");

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

            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4)");
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
  // Code size      262 (0x106)
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
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_0011
    IL_000c:  br         IL_009a
    // sequence point: {
    IL_0011:  nop
    // sequence point: foreach
    IL_0012:  nop
    // sequence point: new C()
    IL_0013:  ldarg.0
    IL_0014:  newobj     ""C..ctor()""
    IL_0019:  ldloca.s   V_1
    IL_001b:  initobj    ""System.Threading.CancellationToken""
    IL_0021:  ldloc.1
    IL_0022:  call       ""C.Enumerator C.GetAsyncEnumerator(System.Threading.CancellationToken)""
    IL_0027:  stfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    // sequence point: <hidden>
    IL_002c:  br.s       IL_005c
    // sequence point: var i
    IL_002e:  ldarg.0
    IL_002f:  ldarg.0
    IL_0030:  ldfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    IL_0035:  callvirt   ""int C.Enumerator.Current.get""
    IL_003a:  stfld      ""int C.<Main>d__0.<i>5__2""
    // sequence point: {
    IL_003f:  nop
    // sequence point: Write($""Got({i}) "");
    IL_0040:  ldstr      ""Got({0}) ""
    IL_0045:  ldarg.0
    IL_0046:  ldfld      ""int C.<Main>d__0.<i>5__2""
    IL_004b:  box        ""int""
    IL_0050:  call       ""string string.Format(string, object)""
    IL_0055:  call       ""void System.Console.Write(string)""
    IL_005a:  nop
    // sequence point: }
    IL_005b:  nop
    // sequence point: in
    IL_005c:  ldarg.0
    IL_005d:  ldfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    IL_0062:  callvirt   ""System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()""
    IL_0067:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_006c:  stloc.2
    // sequence point: <hidden>
    IL_006d:  ldloca.s   V_2
    IL_006f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_0074:  brtrue.s   IL_00b6
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.0
    IL_0078:  dup
    IL_0079:  stloc.0
    IL_007a:  stfld      ""int C.<Main>d__0.<>1__state""
    // async: yield
    IL_007f:  ldarg.0
    IL_0080:  ldloc.2
    IL_0081:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_0086:  ldarg.0
    IL_0087:  stloc.3
    IL_0088:  ldarg.0
    IL_0089:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_008e:  ldloca.s   V_2
    IL_0090:  ldloca.s   V_3
    IL_0092:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Main>d__0)""
    IL_0097:  nop
    IL_0098:  leave.s    IL_0105
    // async: resume
    IL_009a:  ldarg.0
    IL_009b:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_00a0:  stloc.2
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_00a7:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_00ad:  ldarg.0
    IL_00ae:  ldc.i4.m1
    IL_00af:  dup
    IL_00b0:  stloc.0
    IL_00b1:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00b6:  ldarg.0
    IL_00b7:  ldloca.s   V_2
    IL_00b9:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_00be:  stfld      ""bool C.<Main>d__0.<>s__3""
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      ""bool C.<Main>d__0.<>s__3""
    IL_00c9:  brtrue     IL_002e
    IL_00ce:  ldarg.0
    IL_00cf:  ldnull
    IL_00d0:  stfld      ""C.Enumerator C.<Main>d__0.<>s__1""
    IL_00d5:  leave.s    IL_00f1
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_00d7:  stloc.s    V_4
    IL_00d9:  ldarg.0
    IL_00da:  ldc.i4.s   -2
    IL_00dc:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00e1:  ldarg.0
    IL_00e2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_00e7:  ldloc.s    V_4
    IL_00e9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ee:  nop
    IL_00ef:  leave.s    IL_0105
  }
  // sequence point: }
  IL_00f1:  ldarg.0
  IL_00f2:  ldc.i4.s   -2
  IL_00f4:  stfld      ""int C.<Main>d__0.<>1__state""
  // sequence point: <hidden>
  IL_00f9:  ldarg.0
  IL_00fa:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_00ff:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0104:  nop
  IL_0105:  ret
}", sequencePoints: "C+<Main>d__0.MoveNext", source: source + s_IAsyncEnumerable);
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

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4)");
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

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4)");

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

            var verifier = CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4)");

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

            CompileAndVerify(comp, expectedOutput: "NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4) Done");
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

            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Continue(2) NextAsync(2) Current(3) Continue(3) NextAsync(3) Current(4) Break Dispose(4) Done");
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

            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Continue(2) NextAsync(2) Current(3) Continue(3) NextAsync(3) Current(4) Goto Dispose(4) Done");
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

            CompileAndVerify(comp,
                expectedOutput: "NextAsync(0) Current(0) Got(1) NextAsync(1) Current(1) Got(2) NextAsync(2) Current(2) Got(3) NextAsync(3) Current(3) Got(4) NextAsync(4) Dispose(4) Done");
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
            CompileAndVerify(comp, expectedOutput: "Try NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Current(3) Got(3) NextAsync(3) Dispose(4) Done");
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
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Convert(1) Got(1) NextAsync(1) Current(2) Convert(2) Got(2) NextAsync(2) Dispose(3) Done");

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

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1) NextAsync(1) Current(2) Got(2) NextAsync(2) Dispose(3)");

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

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Deconstruct(1) Got(1,-1) NextAsync(1) Current(2) Deconstruct(2) Got(2,-2) NextAsync(2) Dispose(3) Done");

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
            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1,-1) NextAsync(1) Current(2) Got(2,-2) NextAsync(2) Dispose(3) Done");

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

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got(1,-1) NextAsync(1) Current(2) Got(2,-2) NextAsync(2) Dispose(3) Done");
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
                // (7,27): error CS8414: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'await foreach'?
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

            CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got NextAsync(1) Current(2) Got NextAsync(2) Current(3) Got NextAsync(3) Dispose(4)");

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
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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

            var verifier = CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got NextAsync(1) Current(2) Got NextAsync(2) Current(3) Got NextAsync(3)");

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
  // Code size      258 (0x102)
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
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_0011
    IL_000c:  br         IL_0096
    // sequence point: {
    IL_0011:  nop
    // sequence point: ICollection<int> c = new Collection<int>();
    IL_0012:  ldarg.0
    IL_0013:  newobj     ""Collection<int>..ctor()""
    IL_0018:  stfld      ""ICollection<int> C.<Main>d__0.<c>5__1""
    // sequence point: foreach
    IL_001d:  nop
    // sequence point: c
    IL_001e:  ldarg.0
    IL_001f:  ldarg.0
    IL_0020:  ldfld      ""ICollection<int> C.<Main>d__0.<c>5__1""
    IL_0025:  ldloca.s   V_1
    IL_0027:  initobj    ""System.Threading.CancellationToken""
    IL_002d:  ldloc.1
    IL_002e:  callvirt   ""IMyAsyncEnumerator<int> ICollection<int>.GetAsyncEnumerator(System.Threading.CancellationToken)""
    IL_0033:  stfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    // sequence point: <hidden>
    IL_0038:  br.s       IL_0058
    // sequence point: var i
    IL_003a:  ldarg.0
    IL_003b:  ldarg.0
    IL_003c:  ldfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    IL_0041:  callvirt   ""int IMyAsyncEnumerator<int>.Current.get""
    IL_0046:  stfld      ""int C.<Main>d__0.<i>5__3""
    // sequence point: {
    IL_004b:  nop
    // sequence point: Write($""Got "");
    IL_004c:  ldstr      ""Got ""
    IL_0051:  call       ""void System.Console.Write(string)""
    IL_0056:  nop
    // sequence point: }
    IL_0057:  nop
    // sequence point: in
    IL_0058:  ldarg.0
    IL_0059:  ldfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    IL_005e:  callvirt   ""System.Threading.Tasks.Task<bool> IMyAsyncEnumerator<int>.MoveNextAsync()""
    IL_0063:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_0068:  stloc.2
    // sequence point: <hidden>
    IL_0069:  ldloca.s   V_2
    IL_006b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_0070:  brtrue.s   IL_00b2
    IL_0072:  ldarg.0
    IL_0073:  ldc.i4.0
    IL_0074:  dup
    IL_0075:  stloc.0
    IL_0076:  stfld      ""int C.<Main>d__0.<>1__state""
    // async: yield
    IL_007b:  ldarg.0
    IL_007c:  ldloc.2
    IL_007d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_0082:  ldarg.0
    IL_0083:  stloc.3
    IL_0084:  ldarg.0
    IL_0085:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_008a:  ldloca.s   V_2
    IL_008c:  ldloca.s   V_3
    IL_008e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Main>d__0)""
    IL_0093:  nop
    IL_0094:  leave.s    IL_0101
    // async: resume
    IL_0096:  ldarg.0
    IL_0097:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_009c:  stloc.2
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Main>d__0.<>u__1""
    IL_00a3:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_00a9:  ldarg.0
    IL_00aa:  ldc.i4.m1
    IL_00ab:  dup
    IL_00ac:  stloc.0
    IL_00ad:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00b2:  ldarg.0
    IL_00b3:  ldloca.s   V_2
    IL_00b5:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_00ba:  stfld      ""bool C.<Main>d__0.<>s__4""
    IL_00bf:  ldarg.0
    IL_00c0:  ldfld      ""bool C.<Main>d__0.<>s__4""
    IL_00c5:  brtrue     IL_003a
    IL_00ca:  ldarg.0
    IL_00cb:  ldnull
    IL_00cc:  stfld      ""IMyAsyncEnumerator<int> C.<Main>d__0.<>s__2""
    IL_00d1:  leave.s    IL_00ed
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_00d3:  stloc.s    V_4
    IL_00d5:  ldarg.0
    IL_00d6:  ldc.i4.s   -2
    IL_00d8:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00dd:  ldarg.0
    IL_00de:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_00e3:  ldloc.s    V_4
    IL_00e5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ea:  nop
    IL_00eb:  leave.s    IL_0101
  }
  // sequence point: }
  IL_00ed:  ldarg.0
  IL_00ee:  ldc.i4.s   -2
  IL_00f0:  stfld      ""int C.<Main>d__0.<>1__state""
  // sequence point: <hidden>
  IL_00f5:  ldarg.0
  IL_00f6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_00fb:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0100:  nop
  IL_0101:  ret
}", sequencePoints: "C+<Main>d__0.MoveNext", source: source);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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

            var verifier = CompileAndVerify(comp, expectedOutput: "NextAsync(0) Current(1) Got NextAsync(1) Current(2) Got NextAsync(2) Current(3) Got NextAsync(3)");

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Null(info.DisposeMethod);
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
                // (8,33): error CS8411: Asynchronous foreach statement cannot operate on variables of type 'C' because 'C' does not contain a suitable public instance definition for 'GetAsyncEnumerator'
                //         await foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMember, "new C()").WithArguments("C", "GetAsyncEnumerator").WithLocation(8, 33)
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
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void PatternBasedDisposal_InterfacePreferredToInstanceMethod()
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
        async ValueTask System.IAsyncDisposable.DisposeAsync()
        {
            System.Console.Write(""DisposeAsync "");
            await Task.Yield();
        }
        public ValueTask DisposeAsync() => throw null;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync DisposeAsync Done");
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
        }
    }
}
