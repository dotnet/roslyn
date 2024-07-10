// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.AsyncStreams)]
    public class CodeGenAwaitUsingTests : CSharpTestBase
    {
        [Fact]
        public void TestWithCSharp7_3_01()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    async System.Threading.Tasks.Task M()
    {
        await using (var x = new C())
        {
        }
    }
    System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (6,9): error CS8652: The feature 'asynchronous using' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "await").WithArguments("asynchronous using", "8.0").WithLocation(6, 9)
                );
        }

        [Fact]
        public void TestWithCSharp7_3_02()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    async System.Threading.Tasks.Task M()
    {
        await using (var x = new C())
        {
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (6,9): error CS8652: The feature 'asynchronous using' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "await").WithArguments("asynchronous using", "8.0").WithLocation(6, 9),
                // 0.cs(6,22): error CS8370: Feature 'pattern-based disposal' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "var x = new C()").WithArguments("pattern-based disposal", "8.0").WithLocation(6, 22)
                );
        }

        [Fact]
        public void TestInNonAsyncVoidMethod()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    void M()
    {
        await using (var x = new C())
        {
            return;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (6,9): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(6, 9)
                );
        }

        [Fact]
        public void TestInNonAsyncMethodReturningInt()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    int M()
    {
        await using (var x = new C())
        {
            return 1;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (6,9): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<int>'.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await").WithArguments("int").WithLocation(6, 9)
                );
        }

        [Fact]
        public void TestInNonAsyncAnonymousMethod()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    void M()
    {
        System.Action x = () =>
        {
            await using (var y = new C())
            {
                return;
            }
        };
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (8,13): error CS4034: The 'await' operator can only be used within an async lambda expression. Consider marking this lambda expression with the 'async' modifier.
                //             await using (var y = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncLambda, "await").WithArguments("lambda expression").WithLocation(8, 13)
                );
        }

        [Fact]
        public void TestWithTaskReturningDisposeAsync()
        {
            string source = @"
using System.Threading.Tasks;
class C : System.IAsyncDisposable
{
    public static async Task Main()
    {
        await using (var y = new C()) { }
    }
    public async Task DisposeAsync() { }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (3,11): error CS0738: 'C' does not implement interface member 'IAsyncDisposable.DisposeAsync()'. 'C.DisposeAsync()' cannot implement 'IAsyncDisposable.DisposeAsync()' because it does not have the matching return type of 'ValueTask'.
                // class C : System.IAsyncDisposable
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "System.IAsyncDisposable").WithArguments("C", "System.IAsyncDisposable.DisposeAsync()", "C.DisposeAsync()", "System.Threading.Tasks.ValueTask").WithLocation(3, 11),
                // (9,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public async Task DisposeAsync() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "DisposeAsync").WithLocation(9, 23)
                );
        }

        [Fact]
        public void TestInAsyncAnonymousMethod()
        {
            string source = @"
using System.Threading.Tasks;
class C : System.IAsyncDisposable
{
    C()
    {
        System.Console.Write(""C "");
    }
    public static async Task Main()
    {
        System.Func<Task> x = async () =>
        {
            await using (var y = new C())
            {
                System.Console.Write(""body "");
            }
            System.Console.Write(""end"");
        };
        await x();
    }
    public async ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync1 "");
        await Task.Yield();
        System.Console.Write(""DisposeAsync2 "");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C body DisposeAsync1 DisposeAsync2 end");
        }

        [Fact]
        public void TestInUnsafeRegion()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    async System.Threading.Tasks.Task<int> M<T>()
    {
        unsafe
        {
            await using (var x = new C())
            {
                return 1;
            }
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (8,13): error CS4004: Cannot await in an unsafe context
                //             await using (var x = new C())
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await").WithLocation(8, 13)
                );
        }

        [Fact]
        public void TestInLock()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    async System.Threading.Tasks.Task<int> M<T>()
    {
        lock(this)
        {
            await using (var x = new C())
            {
                return 1;
            }
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (8,13): error CS1996: Cannot await in the body of a lock statement
                //             await using (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await").WithLocation(8, 13)
                );
        }

        [Fact]
        public void TestWithObsoleteDisposeAsync_01()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        await using (var x = new C())
        {
        }
    }
    [System.Obsolete]
    async System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()
    {
        await System.Threading.Tasks.Task.Yield();
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            // https://github.com/dotnet/roslyn/issues/30257 Confirm whether this behavior is ok (currently matching behavior of obsolete Dispose in non-async using)
        }

        [Fact]
        public void TestWithObsoleteDisposeAsync_02()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        await using (var x = new C())
        {
        }
    }
    [System.Obsolete]
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        await System.Threading.Tasks.Task.Yield();
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // 0.cs(6,22): warning CS0612: 'C.DisposeAsync()' is obsolete
                //         await using (var x = new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "var x = new C()").WithArguments("C.DisposeAsync()").WithLocation(6, 22)
                );

            comp = CreateCompilationWithTasksExtensions([source, IAsyncDisposableDefinition], parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (6,9): error CS8107: Feature 'asynchronous using' is not available in C# 7.0. Please use language version 8.0 or greater.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "await").WithArguments("asynchronous using", "8.0").WithLocation(6, 9),
                // (6,22): warning CS0612: 'C.DisposeAsync()' is obsolete
                //         await using (var x = new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "var x = new C()").WithArguments("C.DisposeAsync()").WithLocation(6, 22),
                // (6,22): error CS8107: Feature 'pattern-based disposal' is not available in C# 7.0. Please use language version 8.0 or greater.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "var x = new C()").WithArguments("pattern-based disposal", "8.0").WithLocation(6, 22)
                );

            comp = CreateCompilationWithTasksExtensions([source, IAsyncDisposableDefinition], parseOptions: TestOptions.Regular8, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // 0.cs(6,22): warning CS0612: 'C.DisposeAsync()' is obsolete
                //         await using (var x = new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "var x = new C()").WithArguments("C.DisposeAsync()").WithLocation(6, 22)
                );
        }

        [Fact]
        public void TestWithObsoleteDispose()
        {
            string source = @"
class C : System.IDisposable
{
    public static void Main()
    {
        using (var x = new C())
        {
        }
    }
    [System.Obsolete]
    public void Dispose()
    {
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestInCatchBlock()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        try
        {
            System.Console.Write(""try "");
            throw new System.ArgumentNullException();
        }
        catch (System.ArgumentNullException)
        {
            await using (var x = new C())
            {
                System.Console.Write(""using "");
            }
            System.Console.Write(""end"");
        }
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose_end "");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "try using dispose_start dispose_end end");
        }

        [Fact]
        public void MissingAwaitInAsyncMethod()
        {
            string source = @"
class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        System.Action a = () =>
        {
            System.Action b = async () =>
            {
                await local();
                await using (null) { }
            }; // these awaits don't count towards Main method
        };

        async System.Threading.Tasks.Task local()
        {
            await local();
            await using (null) { }
            // neither do these
        }
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (4,53): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async System.Threading.Tasks.Task Main()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Main").WithLocation(4, 53)
                );
        }

        [Fact]
        public void MissingAwaitInAsyncMethod2()
        {
            string source = @"
class C
{
    public static void Main()
    {
        System.Action lambda1 = async () =>
        {
            System.Action b = async () =>
            {
                await local();
                await using (null) { }
            }; // these awaits don't count towards lambda
        };

        System.Action lambda2 = async () => await local2(); // this await counts towards lambda

        async System.Threading.Tasks.Task local()
        {
            System.Func<System.Threading.Tasks.Task> c = innerLocal;

            async System.Threading.Tasks.Task innerLocal()
            {
                await local();
                await using (null) { }
                // these awaits don't count towards lambda either
            }
        }

        async System.Threading.Tasks.Task local2() => await local(); // this await counts towards local function
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (17,43): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         async System.Threading.Tasks.Task local()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "local").WithLocation(17, 43),
                // (6,42): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         System.Action lambda1 = async () =>
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(6, 42)
                );
        }

        [Fact]
        public void TestInFinallyBlock()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    static async System.Threading.Tasks.Task<int> Main()
    {
        try
        {
        }
        finally
        {
            await using (var x = new C())
            {
                System.Console.Write(""using "");
            }
        }

        System.Console.Write(""return"");
        return 1;
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose_end "");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [Fact]
        public void TestThrowingDisposeAsync()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    static async System.Threading.Tasks.Task Main()
    {
        try
        {
            await using (var x = new C())
            {
                System.Console.Write(""using "");
            }
        }
        catch (System.Exception e)
        {
            System.Console.Write($""caught {e.Message}"");
            return;
        }
        System.Console.Write(""SKIPPED"");
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        bool b = true;
        if (b) throw new System.Exception(""message"");
        System.Console.Write(""SKIPPED"");
        await System.Threading.Tasks.Task.Yield();
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using caught message");
        }

        [Fact]
        public void TestRegularAwaitInFinallyBlock()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    static async Task<int> Main()
    {
        try
        {
        }
        finally
        {
            System.Console.Write(""before "");
            await Task.Yield();
            System.Console.Write(""after"");
        }
        return 1;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "before after");
        }

        [Fact]
        public void TestMissingIAsyncDisposable()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task<int> M()
    {
        await using (new C())
        {
        }
        await using (var x = new C())
        {
            return 1;
        }
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyDiagnostics(
                // (6,22): error CS8410: 'C': type used in an asynchronous using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "new C()").WithArguments("C").WithLocation(6, 22),
                // (9,22): error CS8410: 'C': type used in an asynchronous using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "var x = new C()").WithArguments("C").WithLocation(9, 22)
                );
        }

        [Fact]
        public void TestMissingIAsyncDisposableAndMissingValueTaskAndMissingAsync()
        {
            string source = @"
class C
{
    System.Threading.Tasks.Task<int> M()
    {
        await using (new C())
        {
        }
        await using (var x = new C())
        {
            return 1;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_ValueTask);
            comp.VerifyDiagnostics(
                // (6,9): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<Task<int>>'.
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await").WithArguments("System.Threading.Tasks.Task<int>").WithLocation(6, 9),
                // (6,22): error CS8410: 'C': type used in an asynchronous using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "new C()").WithArguments("C").WithLocation(6, 22),
                // (9,9): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<Task<int>>'.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await").WithArguments("System.Threading.Tasks.Task<int>").WithLocation(9, 9),
                // (9,22): error CS8410: 'C': type used in an asynchronous using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "var x = new C()").WithArguments("C").WithLocation(9, 22),
                // (11,20): error CS0029: Cannot implicitly convert type 'int' to 'System.Threading.Tasks.Task<int>'
                //             return 1;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "System.Threading.Tasks.Task<int>").WithLocation(11, 20)
                );
        }

        [Fact]
        public void TestMissingIDisposable()
        {
            string source = @"
class C
{
    int M()
    {
        using (new C())
        {
        }
        using (var x = new C())
        {
            return 1;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.MakeTypeMissing(SpecialType.System_IDisposable);
            comp.VerifyEmitDiagnostics(
                // (6,16): error CS1674: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //         using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "new C()").WithArguments("C").WithLocation(6, 16),
                // (9,16): error CS1674: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //         using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var x = new C()").WithArguments("C").WithLocation(9, 16)
                );
        }

        [Fact]
        public void TestMissingDisposeAsync()
        {
            string source = @"
namespace System
{
    public interface IAsyncDisposable
    {
        // missing DisposeAsync
    }
}
class C : System.IAsyncDisposable
{
    async System.Threading.Tasks.Task<int> M()
    {
        await using (new C()) { }
        await using (var x = new C()) { return 1; }
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (13,9): error CS0656: Missing compiler required member 'System.IAsyncDisposable.DisposeAsync'
                //         await using (new C()) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "await").WithArguments("System.IAsyncDisposable", "DisposeAsync").WithLocation(13, 9),
                // (14,9): error CS0656: Missing compiler required member 'System.IAsyncDisposable.DisposeAsync'
                //         await using (var x = new C()) { return 1; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "await").WithArguments("System.IAsyncDisposable", "DisposeAsync").WithLocation(14, 9)
                );
        }

        [Fact]
        public void TestMissingDispose()
        {
            string source = @"
class C : System.IDisposable
{
    int M()
    {
        using (new C()) { }
        using (var x = new C()) { return 1; }
    }
    public void Dispose()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.MakeMemberMissing(SpecialMember.System_IDisposable__Dispose);
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0656: Missing compiler required member 'System.IDisposable.Dispose'
                //         using (new C()) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "using (new C()) { }").WithArguments("System.IDisposable", "Dispose").WithLocation(6, 9),
                // (7,9): error CS0656: Missing compiler required member 'System.IDisposable.Dispose'
                //         using (var x = new C()) { return 1; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "using (var x = new C()) { return 1; }").WithArguments("System.IDisposable", "Dispose").WithLocation(7, 9)
                );
        }

        [Fact]
        public void TestBadDisposeAsync_01()
        {
            string source = @"
namespace System
{
    public interface IAsyncDisposable
    {
        int DisposeAsync(); // bad return type
    }
}
class C : System.IAsyncDisposable
{
    async System.Threading.Tasks.Task<int> M<T>()
    {
        await using (new C()) { }
        await using (var x = new C()) { return 1; }
    }
    int System.IAsyncDisposable.DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (13,9): error CS0656: Missing compiler required member 'System.IAsyncDisposable.DisposeAsync'
                //         await using (new C()) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "await").WithArguments("System.IAsyncDisposable", "DisposeAsync").WithLocation(13, 9),
                // (14,9): error CS0656: Missing compiler required member 'System.IAsyncDisposable.DisposeAsync'
                //         await using (var x = new C()) { return 1; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "await").WithArguments("System.IAsyncDisposable", "DisposeAsync").WithLocation(14, 9)
                );
        }

        [Fact]
        public void TestBadDisposeAsync_02()
        {
            string source = @"
namespace System
{
    public interface IAsyncDisposable
    {
        int DisposeAsync(); // bad return type
    }
}
class C : System.IAsyncDisposable
{
    async System.Threading.Tasks.Task<int> M<T>()
    {
        await using (new C()) { }
        await using (var x = new C()) { return 1; }
    }
    public int DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(source);
            comp.VerifyDiagnostics(
                // (13,22): error CS1061: 'int' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         await using (new C()) { }
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "new C()").WithArguments("int", "GetAwaiter").WithLocation(13, 22),
                // (14,22): error CS1061: 'int' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         await using (var x = new C()) { return 1; }
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "var x = new C()").WithArguments("int", "GetAwaiter").WithLocation(14, 22)
                );
        }

        [Fact]
        public void TestMissingTaskType_01()
        {
            string lib_cs = @"
public class Base : System.IAsyncDisposable
{
    System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";

            string comp_cs = @"
public class C : Base
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
            System.Console.Write(""body "");
            return 1;
        }
    }
}
";
            var libComp = CreateCompilationWithTasksExtensions(lib_cs + IAsyncDisposableDefinition);
            var comp = CreateCompilationWithTasksExtensions(comp_cs, references: new[] { libComp.EmitToImageReference() }, options: TestOptions.DebugExe);
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_ValueTask);
            comp.VerifyDiagnostics(
                // (6,9): error CS0518: Predefined type 'System.Threading.Tasks.ValueTask' is not defined or imported
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.Threading.Tasks.ValueTask").WithLocation(6, 9)
                );
        }

        [Fact]
        public void TestMissingTaskType_02()
        {
            string lib_cs = @"
public class Base : System.IAsyncDisposable
{
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";

            string comp_cs = @"
public class C : Base
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
            System.Console.Write(""body "");
            return 1;
        }
    }
}
";
            var libComp = CreateCompilationWithTasksExtensions(lib_cs + IAsyncDisposableDefinition);
            var comp = CreateCompilationWithTasksExtensions(comp_cs, references: new[] { libComp.EmitToImageReference() }, options: TestOptions.DebugExe);
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_ValueTask);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void TestWithDeclaration()
        {
            string source = @"
class C : System.IAsyncDisposable, System.IDisposable
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
            System.Console.Write(""body "");
            return 1;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
    public void Dispose()
    {
        System.Console.Write(""IGNORED"");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
        }

        [Fact]
        public void TestIAsyncDisposableInRegularUsing()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static int Main()
    {
        using (var x = new C())
        {
            return 1;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
        => throw null;
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (6,16): error CS8418: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable'. Did you mean 'await using' rather than 'using'?
                //         using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIDispWrongAsync, "var x = new C()").WithArguments("C").WithLocation(6, 16)
                );
        }

        [Fact]
        public void TestIAsyncDisposableInRegularUsing_Expression()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static int Main()
    {
        using (new C())
        {
            return 1;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
        => throw null;
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (6,16): error CS8418: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable'. Did you mean 'await using'?
                //         using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIDispWrongAsync, "new C()").WithArguments("C").WithLocation(6, 16)
                );
        }

        [Fact]
        public void TestIAsyncDisposableInRegularUsing_WithDispose()
        {
            string source = @"
class C : System.IAsyncDisposable, System.IDisposable
{
    public static int Main()
    {
        using (var x = new C())
        {
            System.Console.Write(""body "");
            return 1;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""IGNORED"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
    public void Dispose()
    {
        System.Console.Write(""Dispose"");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body Dispose");
        }

        [Fact]
        public void TestIDisposableInAwaitUsing()
        {
            string source = @"
class C : System.IDisposable
{
    async System.Threading.Tasks.Task<int> M()
    {
        await using (var x = new C())
        {
            return 1;
        }
    }
    public void Dispose()
        => throw null;
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (6,22): error CS8417: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'. Did you mean 'using' rather than 'await using'?
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDispWrongAsync, "var x = new C()").WithArguments("C").WithLocation(6, 22)
                );
        }

        [Fact]
        public void TestIDisposableInAwaitUsing_Expression()
        {
            string source = @"
class C : System.IDisposable
{
    async System.Threading.Tasks.Task<int> M()
    {
        await using (new C())
        {
            return 1;
        }
    }
    public void Dispose()
        => throw null;
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (6,22): error CS8417: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'. Did you mean 'using' rather than 'await using'?
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDispWrongAsync, "new C()").WithArguments("C").WithLocation(6, 22)
                );
        }

        [Fact]
        public void TestWithDynamicDeclaration_ExplicitInterfaceImplementation()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (dynamic x = new C())
        {
            System.Console.Write(""body "");
        }
        System.Console.Write(""end "");
        return 1;
    }
    System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()
    {
        System.Console.Write(""DisposeAsync "");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe, references: new[] { CSharpRef });
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync end");
        }

        [Fact]
        public void TestWithDynamicDeclaration()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (dynamic x = new C())
        {
            System.Console.Write(""body "");
        }
        System.Console.Write(""end "");
        return 1;
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync "");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe, references: new[] { CSharpRef });
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync end");
        }

        [Fact]
        public void TestWithExpression_01()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        await using (new C())
        {
            System.Console.Write(""body "");
            return;
        }
    }
    System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      306 (0x132)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_2,
                System.Threading.Tasks.ValueTask V_3,
                C.<Main>d__0 V_4,
                System.Exception V_5,
                int V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_0011
    IL_000c:  br         IL_0099
    IL_0011:  nop
    IL_0012:  ldarg.0
    IL_0013:  newobj     ""C..ctor()""
    IL_0018:  stfld      ""C C.<Main>d__0.<>s__1""
    IL_001d:  ldarg.0
    IL_001e:  ldnull
    IL_001f:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_0024:  ldarg.0
    IL_0025:  ldc.i4.0
    IL_0026:  stfld      ""int C.<Main>d__0.<>s__3""
    .try
    {
      IL_002b:  nop
      IL_002c:  ldstr      ""body ""
      IL_0031:  call       ""void System.Console.Write(string)""
      IL_0036:  nop
      IL_0037:  br.s       IL_0039
      IL_0039:  ldarg.0
      IL_003a:  ldc.i4.1
      IL_003b:  stfld      ""int C.<Main>d__0.<>s__3""
      IL_0040:  leave.s    IL_004c
    }
    catch object
    {
      IL_0042:  stloc.1
      IL_0043:  ldarg.0
      IL_0044:  ldloc.1
      IL_0045:  stfld      ""object C.<Main>d__0.<>s__2""
      IL_004a:  leave.s    IL_004c
    }
    IL_004c:  ldarg.0
    IL_004d:  ldfld      ""C C.<Main>d__0.<>s__1""
    IL_0052:  brfalse.s  IL_00bd
    IL_0054:  ldarg.0
    IL_0055:  ldfld      ""C C.<Main>d__0.<>s__1""
    IL_005a:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_005f:  stloc.3
    IL_0060:  ldloca.s   V_3
    IL_0062:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_0067:  stloc.2
    IL_0068:  ldloca.s   V_2
    IL_006a:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_006f:  brtrue.s   IL_00b5
    IL_0071:  ldarg.0
    IL_0072:  ldc.i4.0
    IL_0073:  dup
    IL_0074:  stloc.0
    IL_0075:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_007a:  ldarg.0
    IL_007b:  ldloc.2
    IL_007c:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_0081:  ldarg.0
    IL_0082:  stloc.s    V_4
    IL_0084:  ldarg.0
    IL_0085:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_008a:  ldloca.s   V_2
    IL_008c:  ldloca.s   V_4
    IL_008e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<Main>d__0)""
    IL_0093:  nop
    IL_0094:  leave      IL_0131
    IL_0099:  ldarg.0
    IL_009a:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_009f:  stloc.2
    IL_00a0:  ldarg.0
    IL_00a1:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_00a6:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_00ac:  ldarg.0
    IL_00ad:  ldc.i4.m1
    IL_00ae:  dup
    IL_00af:  stloc.0
    IL_00b0:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00b5:  ldloca.s   V_2
    IL_00b7:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_00bc:  nop
    IL_00bd:  ldarg.0
    IL_00be:  ldfld      ""object C.<Main>d__0.<>s__2""
    IL_00c3:  stloc.1
    IL_00c4:  ldloc.1
    IL_00c5:  brfalse.s  IL_00e2
    IL_00c7:  ldloc.1
    IL_00c8:  isinst     ""System.Exception""
    IL_00cd:  stloc.s    V_5
    IL_00cf:  ldloc.s    V_5
    IL_00d1:  brtrue.s   IL_00d5
    IL_00d3:  ldloc.1
    IL_00d4:  throw
    IL_00d5:  ldloc.s    V_5
    IL_00d7:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00dc:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00e1:  nop
    IL_00e2:  ldarg.0
    IL_00e3:  ldfld      ""int C.<Main>d__0.<>s__3""
    IL_00e8:  stloc.s    V_6
    IL_00ea:  ldloc.s    V_6
    IL_00ec:  ldc.i4.1
    IL_00ed:  beq.s      IL_00f1
    IL_00ef:  br.s       IL_00f3
    IL_00f1:  leave.s    IL_011d
    IL_00f3:  ldarg.0
    IL_00f4:  ldnull
    IL_00f5:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_00fa:  ldarg.0
    IL_00fb:  ldnull
    IL_00fc:  stfld      ""C C.<Main>d__0.<>s__1""
    IL_0101:  leave.s    IL_011d
  }
  catch System.Exception
  {
    IL_0103:  stloc.s    V_5
    IL_0105:  ldarg.0
    IL_0106:  ldc.i4.s   -2
    IL_0108:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_010d:  ldarg.0
    IL_010e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_0113:  ldloc.s    V_5
    IL_0115:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_011a:  nop
    IL_011b:  leave.s    IL_0131
  }
  IL_011d:  ldarg.0
  IL_011e:  ldc.i4.s   -2
  IL_0120:  stfld      ""int C.<Main>d__0.<>1__state""
  IL_0125:  ldarg.0
  IL_0126:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_012b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0130:  nop
  IL_0131:  ret
}");
        }

        [Fact]
        public void TestWithExpression_02()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        await using (new C())
        {
            System.Console.Write(""body "");
            return;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      306 (0x132)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_2,
                System.Threading.Tasks.ValueTask V_3,
                C.<Main>d__0 V_4,
                System.Exception V_5,
                int V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_0011
    IL_000c:  br         IL_0099
    IL_0011:  nop
    IL_0012:  ldarg.0
    IL_0013:  newobj     ""C..ctor()""
    IL_0018:  stfld      ""C C.<Main>d__0.<>s__1""
    IL_001d:  ldarg.0
    IL_001e:  ldnull
    IL_001f:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_0024:  ldarg.0
    IL_0025:  ldc.i4.0
    IL_0026:  stfld      ""int C.<Main>d__0.<>s__3""
    .try
    {
      IL_002b:  nop
      IL_002c:  ldstr      ""body ""
      IL_0031:  call       ""void System.Console.Write(string)""
      IL_0036:  nop
      IL_0037:  br.s       IL_0039
      IL_0039:  ldarg.0
      IL_003a:  ldc.i4.1
      IL_003b:  stfld      ""int C.<Main>d__0.<>s__3""
      IL_0040:  leave.s    IL_004c
    }
    catch object
    {
      IL_0042:  stloc.1
      IL_0043:  ldarg.0
      IL_0044:  ldloc.1
      IL_0045:  stfld      ""object C.<Main>d__0.<>s__2""
      IL_004a:  leave.s    IL_004c
    }
    IL_004c:  ldarg.0
    IL_004d:  ldfld      ""C C.<Main>d__0.<>s__1""
    IL_0052:  brfalse.s  IL_00bd
    IL_0054:  ldarg.0
    IL_0055:  ldfld      ""C C.<Main>d__0.<>s__1""
    IL_005a:  callvirt   ""System.Threading.Tasks.ValueTask C.DisposeAsync()""
    IL_005f:  stloc.3
    IL_0060:  ldloca.s   V_3
    IL_0062:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_0067:  stloc.2
    IL_0068:  ldloca.s   V_2
    IL_006a:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_006f:  brtrue.s   IL_00b5
    IL_0071:  ldarg.0
    IL_0072:  ldc.i4.0
    IL_0073:  dup
    IL_0074:  stloc.0
    IL_0075:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_007a:  ldarg.0
    IL_007b:  ldloc.2
    IL_007c:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_0081:  ldarg.0
    IL_0082:  stloc.s    V_4
    IL_0084:  ldarg.0
    IL_0085:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_008a:  ldloca.s   V_2
    IL_008c:  ldloca.s   V_4
    IL_008e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<Main>d__0)""
    IL_0093:  nop
    IL_0094:  leave      IL_0131
    IL_0099:  ldarg.0
    IL_009a:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_009f:  stloc.2
    IL_00a0:  ldarg.0
    IL_00a1:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_00a6:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_00ac:  ldarg.0
    IL_00ad:  ldc.i4.m1
    IL_00ae:  dup
    IL_00af:  stloc.0
    IL_00b0:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00b5:  ldloca.s   V_2
    IL_00b7:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_00bc:  nop
    IL_00bd:  ldarg.0
    IL_00be:  ldfld      ""object C.<Main>d__0.<>s__2""
    IL_00c3:  stloc.1
    IL_00c4:  ldloc.1
    IL_00c5:  brfalse.s  IL_00e2
    IL_00c7:  ldloc.1
    IL_00c8:  isinst     ""System.Exception""
    IL_00cd:  stloc.s    V_5
    IL_00cf:  ldloc.s    V_5
    IL_00d1:  brtrue.s   IL_00d5
    IL_00d3:  ldloc.1
    IL_00d4:  throw
    IL_00d5:  ldloc.s    V_5
    IL_00d7:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00dc:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00e1:  nop
    IL_00e2:  ldarg.0
    IL_00e3:  ldfld      ""int C.<Main>d__0.<>s__3""
    IL_00e8:  stloc.s    V_6
    IL_00ea:  ldloc.s    V_6
    IL_00ec:  ldc.i4.1
    IL_00ed:  beq.s      IL_00f1
    IL_00ef:  br.s       IL_00f3
    IL_00f1:  leave.s    IL_011d
    IL_00f3:  ldarg.0
    IL_00f4:  ldnull
    IL_00f5:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_00fa:  ldarg.0
    IL_00fb:  ldnull
    IL_00fc:  stfld      ""C C.<Main>d__0.<>s__1""
    IL_0101:  leave.s    IL_011d
  }
  catch System.Exception
  {
    IL_0103:  stloc.s    V_5
    IL_0105:  ldarg.0
    IL_0106:  ldc.i4.s   -2
    IL_0108:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_010d:  ldarg.0
    IL_010e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_0113:  ldloc.s    V_5
    IL_0115:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_011a:  nop
    IL_011b:  leave.s    IL_0131
  }
  IL_011d:  ldarg.0
  IL_011e:  ldc.i4.s   -2
  IL_0120:  stfld      ""int C.<Main>d__0.<>1__state""
  IL_0125:  ldarg.0
  IL_0126:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_012b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0130:  nop
  IL_0131:  ret
}
");
        }

        [Fact]
        public void TestWithExpression_03()
        {
            string source = @"
class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        await using (new C())
        {
            System.Console.Write(""body "");
            return;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      306 (0x132)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_2,
                System.Threading.Tasks.ValueTask V_3,
                C.<Main>d__0 V_4,
                System.Exception V_5,
                int V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_0011
    IL_000c:  br         IL_0099
    IL_0011:  nop
    IL_0012:  ldarg.0
    IL_0013:  newobj     ""C..ctor()""
    IL_0018:  stfld      ""C C.<Main>d__0.<>s__1""
    IL_001d:  ldarg.0
    IL_001e:  ldnull
    IL_001f:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_0024:  ldarg.0
    IL_0025:  ldc.i4.0
    IL_0026:  stfld      ""int C.<Main>d__0.<>s__3""
    .try
    {
      IL_002b:  nop
      IL_002c:  ldstr      ""body ""
      IL_0031:  call       ""void System.Console.Write(string)""
      IL_0036:  nop
      IL_0037:  br.s       IL_0039
      IL_0039:  ldarg.0
      IL_003a:  ldc.i4.1
      IL_003b:  stfld      ""int C.<Main>d__0.<>s__3""
      IL_0040:  leave.s    IL_004c
    }
    catch object
    {
      IL_0042:  stloc.1
      IL_0043:  ldarg.0
      IL_0044:  ldloc.1
      IL_0045:  stfld      ""object C.<Main>d__0.<>s__2""
      IL_004a:  leave.s    IL_004c
    }
    IL_004c:  ldarg.0
    IL_004d:  ldfld      ""C C.<Main>d__0.<>s__1""
    IL_0052:  brfalse.s  IL_00bd
    IL_0054:  ldarg.0
    IL_0055:  ldfld      ""C C.<Main>d__0.<>s__1""
    IL_005a:  callvirt   ""System.Threading.Tasks.ValueTask C.DisposeAsync()""
    IL_005f:  stloc.3
    IL_0060:  ldloca.s   V_3
    IL_0062:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_0067:  stloc.2
    IL_0068:  ldloca.s   V_2
    IL_006a:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_006f:  brtrue.s   IL_00b5
    IL_0071:  ldarg.0
    IL_0072:  ldc.i4.0
    IL_0073:  dup
    IL_0074:  stloc.0
    IL_0075:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_007a:  ldarg.0
    IL_007b:  ldloc.2
    IL_007c:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_0081:  ldarg.0
    IL_0082:  stloc.s    V_4
    IL_0084:  ldarg.0
    IL_0085:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_008a:  ldloca.s   V_2
    IL_008c:  ldloca.s   V_4
    IL_008e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<Main>d__0)""
    IL_0093:  nop
    IL_0094:  leave      IL_0131
    IL_0099:  ldarg.0
    IL_009a:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_009f:  stloc.2
    IL_00a0:  ldarg.0
    IL_00a1:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_00a6:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_00ac:  ldarg.0
    IL_00ad:  ldc.i4.m1
    IL_00ae:  dup
    IL_00af:  stloc.0
    IL_00b0:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00b5:  ldloca.s   V_2
    IL_00b7:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_00bc:  nop
    IL_00bd:  ldarg.0
    IL_00be:  ldfld      ""object C.<Main>d__0.<>s__2""
    IL_00c3:  stloc.1
    IL_00c4:  ldloc.1
    IL_00c5:  brfalse.s  IL_00e2
    IL_00c7:  ldloc.1
    IL_00c8:  isinst     ""System.Exception""
    IL_00cd:  stloc.s    V_5
    IL_00cf:  ldloc.s    V_5
    IL_00d1:  brtrue.s   IL_00d5
    IL_00d3:  ldloc.1
    IL_00d4:  throw
    IL_00d5:  ldloc.s    V_5
    IL_00d7:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00dc:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00e1:  nop
    IL_00e2:  ldarg.0
    IL_00e3:  ldfld      ""int C.<Main>d__0.<>s__3""
    IL_00e8:  stloc.s    V_6
    IL_00ea:  ldloc.s    V_6
    IL_00ec:  ldc.i4.1
    IL_00ed:  beq.s      IL_00f1
    IL_00ef:  br.s       IL_00f3
    IL_00f1:  leave.s    IL_011d
    IL_00f3:  ldarg.0
    IL_00f4:  ldnull
    IL_00f5:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_00fa:  ldarg.0
    IL_00fb:  ldnull
    IL_00fc:  stfld      ""C C.<Main>d__0.<>s__1""
    IL_0101:  leave.s    IL_011d
  }
  catch System.Exception
  {
    IL_0103:  stloc.s    V_5
    IL_0105:  ldarg.0
    IL_0106:  ldc.i4.s   -2
    IL_0108:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_010d:  ldarg.0
    IL_010e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_0113:  ldloc.s    V_5
    IL_0115:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_011a:  nop
    IL_011b:  leave.s    IL_0131
  }
  IL_011d:  ldarg.0
  IL_011e:  ldc.i4.s   -2
  IL_0120:  stfld      ""int C.<Main>d__0.<>1__state""
  IL_0125:  ldarg.0
  IL_0126:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_012b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0130:  nop
  IL_0131:  ret
}
");
        }

        [Fact]
        public void TestWithNullExpression()
        {
            string source = @"
class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        await using (null)
        {
            System.Console.Write(""body"");
            return;
        }
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body");
        }

        [Fact]
        public void TestWithMethodName()
        {
            string source = @"
class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        await using (Main)
        {
        }
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (6,22): error CS8410: 'method group': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'
                //         await using (Main)
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "Main").WithArguments("method group").WithLocation(6, 22)
                );
        }

        [Fact]
        public void TestWithDynamicExpression()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        dynamic d = new C();
        await using (d)
        {
            System.Console.Write(""body "");
            return;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe, references: new[] { CSharpRef });
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
        }

        [Fact]
        public void TestWithStructExpression_01()
        {
            string source = @"
struct S : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        await using (new S())
        {
            System.Console.Write(""body "");
            return;
        }
    }
    System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
            verifier.VerifyIL("S.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      298 (0x12a)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_2,
                System.Threading.Tasks.ValueTask V_3,
                S.<Main>d__0 V_4,
                System.Exception V_5,
                int V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int S.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_0011
    IL_000c:  br         IL_0098
    IL_0011:  nop
    IL_0012:  ldarg.0
    IL_0013:  ldflda     ""S S.<Main>d__0.<>s__1""
    IL_0018:  initobj    ""S""
    IL_001e:  ldarg.0
    IL_001f:  ldnull
    IL_0020:  stfld      ""object S.<Main>d__0.<>s__2""
    IL_0025:  ldarg.0
    IL_0026:  ldc.i4.0
    IL_0027:  stfld      ""int S.<Main>d__0.<>s__3""
    .try
    {
      IL_002c:  nop
      IL_002d:  ldstr      ""body ""
      IL_0032:  call       ""void System.Console.Write(string)""
      IL_0037:  nop
      IL_0038:  br.s       IL_003a
      IL_003a:  ldarg.0
      IL_003b:  ldc.i4.1
      IL_003c:  stfld      ""int S.<Main>d__0.<>s__3""
      IL_0041:  leave.s    IL_004d
    }
    catch object
    {
      IL_0043:  stloc.1
      IL_0044:  ldarg.0
      IL_0045:  ldloc.1
      IL_0046:  stfld      ""object S.<Main>d__0.<>s__2""
      IL_004b:  leave.s    IL_004d
    }
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""S S.<Main>d__0.<>s__1""
    IL_0053:  constrained. ""S""
    IL_0059:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_005e:  stloc.3
    IL_005f:  ldloca.s   V_3
    IL_0061:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_0066:  stloc.2
    IL_0067:  ldloca.s   V_2
    IL_0069:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_006e:  brtrue.s   IL_00b4
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.0
    IL_0072:  dup
    IL_0073:  stloc.0
    IL_0074:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_0079:  ldarg.0
    IL_007a:  ldloc.2
    IL_007b:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter S.<Main>d__0.<>u__1""
    IL_0080:  ldarg.0
    IL_0081:  stloc.s    V_4
    IL_0083:  ldarg.0
    IL_0084:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
    IL_0089:  ldloca.s   V_2
    IL_008b:  ldloca.s   V_4
    IL_008d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, S.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref S.<Main>d__0)""
    IL_0092:  nop
    IL_0093:  leave      IL_0129
    IL_0098:  ldarg.0
    IL_0099:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter S.<Main>d__0.<>u__1""
    IL_009e:  stloc.2
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter S.<Main>d__0.<>u__1""
    IL_00a5:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_00ab:  ldarg.0
    IL_00ac:  ldc.i4.m1
    IL_00ad:  dup
    IL_00ae:  stloc.0
    IL_00af:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_00b4:  ldloca.s   V_2
    IL_00b6:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_00bb:  nop
    IL_00bc:  ldarg.0
    IL_00bd:  ldfld      ""object S.<Main>d__0.<>s__2""
    IL_00c2:  stloc.1
    IL_00c3:  ldloc.1
    IL_00c4:  brfalse.s  IL_00e1
    IL_00c6:  ldloc.1
    IL_00c7:  isinst     ""System.Exception""
    IL_00cc:  stloc.s    V_5
    IL_00ce:  ldloc.s    V_5
    IL_00d0:  brtrue.s   IL_00d4
    IL_00d2:  ldloc.1
    IL_00d3:  throw
    IL_00d4:  ldloc.s    V_5
    IL_00d6:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00db:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00e0:  nop
    IL_00e1:  ldarg.0
    IL_00e2:  ldfld      ""int S.<Main>d__0.<>s__3""
    IL_00e7:  stloc.s    V_6
    IL_00e9:  ldloc.s    V_6
    IL_00eb:  ldc.i4.1
    IL_00ec:  beq.s      IL_00f0
    IL_00ee:  br.s       IL_00f2
    IL_00f0:  leave.s    IL_0115
    IL_00f2:  ldarg.0
    IL_00f3:  ldnull
    IL_00f4:  stfld      ""object S.<Main>d__0.<>s__2""
    IL_00f9:  leave.s    IL_0115
  }
  catch System.Exception
  {
    IL_00fb:  stloc.s    V_5
    IL_00fd:  ldarg.0
    IL_00fe:  ldc.i4.s   -2
    IL_0100:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_0105:  ldarg.0
    IL_0106:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
    IL_010b:  ldloc.s    V_5
    IL_010d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0112:  nop
    IL_0113:  leave.s    IL_0129
  }
  IL_0115:  ldarg.0
  IL_0116:  ldc.i4.s   -2
  IL_0118:  stfld      ""int S.<Main>d__0.<>1__state""
  IL_011d:  ldarg.0
  IL_011e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
  IL_0123:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0128:  nop
  IL_0129:  ret
}");
        }

        [Fact]
        public void TestWithStructExpression_02()
        {
            string source = @"
struct S : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        await using (new S())
        {
            System.Console.Write(""body "");
            return;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
            verifier.VerifyIL("S.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      292 (0x124)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_2,
                System.Threading.Tasks.ValueTask V_3,
                S.<Main>d__0 V_4,
                System.Exception V_5,
                int V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int S.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_0011
    IL_000c:  br         IL_0092
    IL_0011:  nop
    IL_0012:  ldarg.0
    IL_0013:  ldflda     ""S S.<Main>d__0.<>s__1""
    IL_0018:  initobj    ""S""
    IL_001e:  ldarg.0
    IL_001f:  ldnull
    IL_0020:  stfld      ""object S.<Main>d__0.<>s__2""
    IL_0025:  ldarg.0
    IL_0026:  ldc.i4.0
    IL_0027:  stfld      ""int S.<Main>d__0.<>s__3""
    .try
    {
      IL_002c:  nop
      IL_002d:  ldstr      ""body ""
      IL_0032:  call       ""void System.Console.Write(string)""
      IL_0037:  nop
      IL_0038:  br.s       IL_003a
      IL_003a:  ldarg.0
      IL_003b:  ldc.i4.1
      IL_003c:  stfld      ""int S.<Main>d__0.<>s__3""
      IL_0041:  leave.s    IL_004d
    }
    catch object
    {
      IL_0043:  stloc.1
      IL_0044:  ldarg.0
      IL_0045:  ldloc.1
      IL_0046:  stfld      ""object S.<Main>d__0.<>s__2""
      IL_004b:  leave.s    IL_004d
    }
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""S S.<Main>d__0.<>s__1""
    IL_0053:  call       ""System.Threading.Tasks.ValueTask S.DisposeAsync()""
    IL_0058:  stloc.3
    IL_0059:  ldloca.s   V_3
    IL_005b:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_0060:  stloc.2
    IL_0061:  ldloca.s   V_2
    IL_0063:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_0068:  brtrue.s   IL_00ae
    IL_006a:  ldarg.0
    IL_006b:  ldc.i4.0
    IL_006c:  dup
    IL_006d:  stloc.0
    IL_006e:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_0073:  ldarg.0
    IL_0074:  ldloc.2
    IL_0075:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter S.<Main>d__0.<>u__1""
    IL_007a:  ldarg.0
    IL_007b:  stloc.s    V_4
    IL_007d:  ldarg.0
    IL_007e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
    IL_0083:  ldloca.s   V_2
    IL_0085:  ldloca.s   V_4
    IL_0087:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, S.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref S.<Main>d__0)""
    IL_008c:  nop
    IL_008d:  leave      IL_0123
    IL_0092:  ldarg.0
    IL_0093:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter S.<Main>d__0.<>u__1""
    IL_0098:  stloc.2
    IL_0099:  ldarg.0
    IL_009a:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter S.<Main>d__0.<>u__1""
    IL_009f:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_00a5:  ldarg.0
    IL_00a6:  ldc.i4.m1
    IL_00a7:  dup
    IL_00a8:  stloc.0
    IL_00a9:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_00ae:  ldloca.s   V_2
    IL_00b0:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_00b5:  nop
    IL_00b6:  ldarg.0
    IL_00b7:  ldfld      ""object S.<Main>d__0.<>s__2""
    IL_00bc:  stloc.1
    IL_00bd:  ldloc.1
    IL_00be:  brfalse.s  IL_00db
    IL_00c0:  ldloc.1
    IL_00c1:  isinst     ""System.Exception""
    IL_00c6:  stloc.s    V_5
    IL_00c8:  ldloc.s    V_5
    IL_00ca:  brtrue.s   IL_00ce
    IL_00cc:  ldloc.1
    IL_00cd:  throw
    IL_00ce:  ldloc.s    V_5
    IL_00d0:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00d5:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00da:  nop
    IL_00db:  ldarg.0
    IL_00dc:  ldfld      ""int S.<Main>d__0.<>s__3""
    IL_00e1:  stloc.s    V_6
    IL_00e3:  ldloc.s    V_6
    IL_00e5:  ldc.i4.1
    IL_00e6:  beq.s      IL_00ea
    IL_00e8:  br.s       IL_00ec
    IL_00ea:  leave.s    IL_010f
    IL_00ec:  ldarg.0
    IL_00ed:  ldnull
    IL_00ee:  stfld      ""object S.<Main>d__0.<>s__2""
    IL_00f3:  leave.s    IL_010f
  }
  catch System.Exception
  {
    IL_00f5:  stloc.s    V_5
    IL_00f7:  ldarg.0
    IL_00f8:  ldc.i4.s   -2
    IL_00fa:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_00ff:  ldarg.0
    IL_0100:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
    IL_0105:  ldloc.s    V_5
    IL_0107:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_010c:  nop
    IL_010d:  leave.s    IL_0123
  }
  IL_010f:  ldarg.0
  IL_0110:  ldc.i4.s   -2
  IL_0112:  stfld      ""int S.<Main>d__0.<>1__state""
  IL_0117:  ldarg.0
  IL_0118:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
  IL_011d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0122:  nop
  IL_0123:  ret
}");
        }

        [Fact]
        public void Struct_ExplicitImplementation()
        {
            string source =
@"using System;
using System.Threading.Tasks;
class C
{
    internal bool _disposed;
}
struct S : IAsyncDisposable
{
    C _c;
    S(C c)
    {
        _c = c;
    }
    static async Task Main()
    {
        var s = new S(new C());
        await using (s)
        {
        }
        Console.WriteLine(s._c._disposed);
    }
    ValueTask IAsyncDisposable.DisposeAsync()
    {
        _c._disposed = true;
        return new ValueTask(Task.CompletedTask);
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");
        }

        [Fact]
        public void TestWithNullableExpression()
        {
            string source = @"
struct S : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        S? s = new S();
        await using (s)
        {
            System.Console.Write(""body "");
            return;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
        }

        [Fact]
        public void TestWithNullNullableExpression()
        {
            string source = @"
struct S : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        S? s = null;
        await using (s)
        {
            System.Console.Write(""body"");
            return;
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""NOT RELEVANT"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body");
        }

        [Fact]
        [WorkItem(24267, "https://github.com/dotnet/roslyn/issues/24267")]
        public void AssignsInAsyncWithAwaitUsing()
        {
            var comp = CreateCompilationWithTasksExtensions(@"
class C
{
    public static void M2()
    {
        int a=0, x, y, z;
        L1();
        a++;
        x++;
        y++;
        z++;

        async void L1()
        {
            x = 0;
            await using (null) { }
            y = 0;

            // local function exists with a pending branch from the async using, in which `y` was not assigned
        }
    }
}" + IAsyncDisposableDefinition);
            comp.VerifyDiagnostics(
                // (9,9): error CS0165: Use of unassigned local variable 'x'
                //         x++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(9, 9),
                // (10,9): error CS0165: Use of unassigned local variable 'y'
                //         y++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(10, 9),
                // (11,9): error CS0165: Use of unassigned local variable 'z'
                //         z++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(11, 9)
                );
        }

        [Fact]
        public void TestWithMultipleResources()
        {
            string source = @"
class S : System.IAsyncDisposable
{
    private int _i;
    S(int i)
    {
        System.Console.Write($""ctor{i} "");
        _i = i;
    }
    public static async System.Threading.Tasks.Task Main()
    {
        await using (S s1 = new S(1), s2 = new S(2))
        {
            System.Console.Write(""body "");
            return;
        }
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write($""dispose{_i}_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose{_i}_end "");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ctor1 ctor2 body dispose2_start dispose2_end dispose1_start dispose1_end");
        }

        [Fact]
        public void TestWithMultipleResourcesAndException()
        {
            string source = @"
class S : System.IAsyncDisposable
{
    private int _i;
    S(int i)
    {
        System.Console.Write($""ctor{i} "");
        _i = i;
    }
    public static async System.Threading.Tasks.Task Main()
    {
        try
        {
            await using (S s1 = new S(1), s2 = new S(2))
            {
                System.Console.Write(""body "");
                throw new System.Exception();
            }
        }
        catch (System.Exception)
        {
            System.Console.Write(""caught"");
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write($""dispose{_i} "");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ctor1 ctor2 body dispose2 dispose1 caught");
        }

        [Fact]
        public void TestWithMultipleResourcesAndExceptionInSecondResource()
        {
            string source = @"
class S : System.IAsyncDisposable
{
    private int _i;
    S(int i)
    {
        System.Console.Write($""ctor{i} "");
        if (i == 1)
        {
            _i = i;
        }
        else
        {
            throw new System.Exception();
        }
    }
    public static async System.Threading.Tasks.Task Main()
    {
        try
        {
            await using (S s1 = new S(1), s2 = new S(2))
            {
                System.Console.Write(""SKIPPED"");
            }
        }
        catch (System.Exception)
        {
            System.Console.Write(""caught"");
        }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write($""dispose{_i} "");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ctor1 ctor2 dispose1 caught");
        }

        [Fact]
        public void TestAwaitExpressionInfo_IEquatable()
        {
            string source = @"
public class C
{
    void GetAwaiter() { }
    void GetResult() { }
    bool IsCompleted => true;
}
public class D
{
    void GetAwaiter() { }
    void GetResult() { }
    bool IsCompleted => true;
}
";
            var comp = CreateCompilation(source);
            var getAwaiter1 = (MethodSymbol)comp.GetMember("C.GetAwaiter");
            var isCompleted1 = (PropertySymbol)comp.GetMember("C.IsCompleted");
            var getResult1 = (MethodSymbol)comp.GetMember("C.GetResult");
            var first = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), getResult1.GetPublicSymbol(), false);

            var nulls1 = new AwaitExpressionInfo(null, isCompleted1.GetPublicSymbol(), getResult1.GetPublicSymbol(), false);
            var nulls2 = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), null, getResult1.GetPublicSymbol(), false);
            var nulls3 = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), null, false);
            var nulls4 = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), null, true);

            Assert.False(first.Equals(nulls1));
            Assert.False(first.Equals(nulls2));
            Assert.False(first.Equals(nulls3));
            Assert.False(first.Equals(nulls4));

            Assert.False(nulls1.Equals(first));
            Assert.False(nulls2.Equals(first));
            Assert.False(nulls3.Equals(first));
            Assert.False(nulls4.Equals(first));

            _ = nulls1.GetHashCode();
            _ = nulls2.GetHashCode();
            _ = nulls3.GetHashCode();
            _ = nulls4.GetHashCode();

            object nullObj = null;
            Assert.False(first.Equals(nullObj));

            var getAwaiter2 = (MethodSymbol)comp.GetMember("D.GetAwaiter");
            var isCompleted2 = (PropertySymbol)comp.GetMember("D.IsCompleted");
            var getResult2 = (MethodSymbol)comp.GetMember("D.GetResult");
            var second1 = new AwaitExpressionInfo(getAwaiter2.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), getResult1.GetPublicSymbol(), false);
            var second2 = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted2.GetPublicSymbol(), getResult1.GetPublicSymbol(), false);
            var second3 = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), getResult2.GetPublicSymbol(), false);
            var second4 = new AwaitExpressionInfo(getAwaiter2.GetPublicSymbol(), isCompleted2.GetPublicSymbol(), getResult2.GetPublicSymbol(), false);

            Assert.False(first.Equals(second1));
            Assert.False(first.Equals(second2));
            Assert.False(first.Equals(second3));
            Assert.False(first.Equals(second4));

            Assert.False(second1.Equals(first));
            Assert.False(second2.Equals(first));
            Assert.False(second3.Equals(first));
            Assert.False(second4.Equals(first));

            Assert.True(first.Equals(first));
            Assert.True(first.Equals((object)first));

            var another = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), getResult1.GetPublicSymbol(), false);
            Assert.True(first.GetHashCode() == another.GetHashCode());
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_ExtensionMethod()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
        }

        return 1;
    }
}
public static class Extensions
{
    public static System.Threading.Tasks.ValueTask DisposeAsync(this C c)
        => throw null;
}
";
            // extension methods do not contribute to pattern-based disposal
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,22): error CS8410: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "var x = new C()").WithArguments("C").WithLocation(6, 22)
                );
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_TwoOverloads()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
        }
        return 1;
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync(int i = 0)
    {
        System.Console.Write($""dispose"");
        await System.Threading.Tasks.Task.Yield();
    }
    public System.Threading.Tasks.ValueTask DisposeAsync(params string[] s)
        => throw null;
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "dispose");
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_Expression_ExtensionMethod()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (new C())
        {
        }

        return 1;
    }
}
public static class Extensions
{
    public static System.Threading.Tasks.ValueTask DisposeAsync(this C c)
        => throw null;
}
";
            // extension methods do not contribute to pattern-based disposal
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,22): error CS8410: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "new C()").WithArguments("C").WithLocation(6, 22)
                );
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_Expression_InstanceMethod()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (new C())
        {
            System.Console.Write(""using "");
        }

        System.Console.Write(""return"");
        return 1;
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose_end "");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_InstanceMethod()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
            System.Console.Write(""using "");
        }

        System.Console.Write(""return"");
        return 1;
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose_end "");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72573")]
        public void TestPatternBasedDisposal_InterfaceNotPreferredOverInstanceMethod()
        {
            string source = @"
public class C : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
            System.Console.Write(""using "");
        }

        System.Console.Write(""return"");
        return 1;
    }
    System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()
        => throw null;
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose_end "");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_InstanceMethod_OptionalParameter()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
            System.Console.Write(""using "");
        }

        System.Console.Write(""return"");
        return 1;
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync(int i = 0)
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose_end "");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_InstanceMethod_ParamsParameter()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
            System.Console.Write(""using "");
        }

        System.Console.Write(""return"");
        return 1;
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync(params int[] x)
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose_end({x.Length}) "");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end(0) return");
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_InstanceMethod_ReturningVoid()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
        }

        return 1;
    }
    public void DisposeAsync()
    {
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (6,22): error CS4008: Cannot await 'void'
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitArgVoidCall, "var x = new C()").WithLocation(6, 22)
                );
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_InstanceMethod_ReturningInt()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
        }

        return 1;
    }
    public int DisposeAsync()
        => throw null;
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (6,22): error CS1061: 'int' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "var x = new C()").WithArguments("int", "GetAwaiter").WithLocation(6, 22)
                );
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_InstanceMethod_Inaccessible()
        {
            string source = @"
public class D
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        await using (var x = new C())
        {
        }

        return 1;
    }
}
public class C
{
    private System.Threading.Tasks.ValueTask DisposeAsync()
        => throw null;
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // 0.cs(6,22): error CS8410: 'C': type used in an asynchronous using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "var x = new C()").WithArguments("C").WithLocation(6, 22));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_InstanceMethod_UsingDeclaration()
        {
            string source = @"
public class C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        {
            await using var x = new C();
            System.Console.Write(""using "");
        }
        System.Console.Write(""return"");
        return 1;
    }
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose_end "");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");

            // Sequence point highlights `await using ...`
            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      303 (0x12f)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                object V_2,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_3,
                System.Threading.Tasks.ValueTask V_4,
                C.<Main>d__0 V_5,
                System.Exception V_6)
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
    IL_000c:  br         IL_0091
    // sequence point: {
    IL_0011:  nop
    // sequence point: {
    IL_0012:  nop
    // sequence point: await using var x = new C();
    IL_0013:  ldarg.0
    IL_0014:  newobj     ""C..ctor()""
    IL_0019:  stfld      ""C C.<Main>d__0.<x>5__1""
    // sequence point: <hidden>
    IL_001e:  ldarg.0
    IL_001f:  ldnull
    IL_0020:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_0025:  ldarg.0
    IL_0026:  ldc.i4.0
    IL_0027:  stfld      ""int C.<Main>d__0.<>s__3""
    .try
    {
      // sequence point: System.Console.Write(""using "");
      IL_002c:  ldstr      ""using ""
      IL_0031:  call       ""void System.Console.Write(string)""
      IL_0036:  nop
      // sequence point: <hidden>
      IL_0037:  leave.s    IL_0043
    }
    catch object
    {
      // sequence point: <hidden>
      IL_0039:  stloc.2
      IL_003a:  ldarg.0
      IL_003b:  ldloc.2
      IL_003c:  stfld      ""object C.<Main>d__0.<>s__2""
      IL_0041:  leave.s    IL_0043
    }
    // sequence point: <hidden>
    IL_0043:  ldarg.0
    IL_0044:  ldfld      ""C C.<Main>d__0.<x>5__1""
    IL_0049:  brfalse.s  IL_00b5
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""C C.<Main>d__0.<x>5__1""
    IL_0051:  callvirt   ""System.Threading.Tasks.ValueTask C.DisposeAsync()""
    IL_0056:  stloc.s    V_4
    IL_0058:  ldloca.s   V_4
    IL_005a:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_005f:  stloc.3
    // sequence point: <hidden>
    IL_0060:  ldloca.s   V_3
    IL_0062:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_0067:  brtrue.s   IL_00ad
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.0
    IL_006b:  dup
    IL_006c:  stloc.0
    IL_006d:  stfld      ""int C.<Main>d__0.<>1__state""
    // async: yield
    IL_0072:  ldarg.0
    IL_0073:  ldloc.3
    IL_0074:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_0079:  ldarg.0
    IL_007a:  stloc.s    V_5
    IL_007c:  ldarg.0
    IL_007d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<Main>d__0.<>t__builder""
    IL_0082:  ldloca.s   V_3
    IL_0084:  ldloca.s   V_5
    IL_0086:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<Main>d__0)""
    IL_008b:  nop
    IL_008c:  leave      IL_012e
    // async: resume
    IL_0091:  ldarg.0
    IL_0092:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_0097:  stloc.3
    IL_0098:  ldarg.0
    IL_0099:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_009e:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_00a4:  ldarg.0
    IL_00a5:  ldc.i4.m1
    IL_00a6:  dup
    IL_00a7:  stloc.0
    IL_00a8:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00ad:  ldloca.s   V_3
    IL_00af:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_00b4:  nop
    // sequence point: <hidden>
    IL_00b5:  ldarg.0
    IL_00b6:  ldfld      ""object C.<Main>d__0.<>s__2""
    IL_00bb:  stloc.2
    IL_00bc:  ldloc.2
    IL_00bd:  brfalse.s  IL_00da
    IL_00bf:  ldloc.2
    IL_00c0:  isinst     ""System.Exception""
    IL_00c5:  stloc.s    V_6
    IL_00c7:  ldloc.s    V_6
    IL_00c9:  brtrue.s   IL_00cd
    IL_00cb:  ldloc.2
    IL_00cc:  throw
    IL_00cd:  ldloc.s    V_6
    IL_00cf:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00d4:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00d9:  nop
    IL_00da:  ldarg.0
    IL_00db:  ldfld      ""int C.<Main>d__0.<>s__3""
    IL_00e0:  pop
    IL_00e1:  ldarg.0
    IL_00e2:  ldnull
    IL_00e3:  stfld      ""object C.<Main>d__0.<>s__2""
    // sequence point: }
    IL_00e8:  nop
    IL_00e9:  ldarg.0
    IL_00ea:  ldnull
    IL_00eb:  stfld      ""C C.<Main>d__0.<x>5__1""
    // sequence point: System.Console.Write(""return"");
    IL_00f0:  ldstr      ""return""
    IL_00f5:  call       ""void System.Console.Write(string)""
    IL_00fa:  nop
    // sequence point: return 1;
    IL_00fb:  ldc.i4.1
    IL_00fc:  stloc.1
    IL_00fd:  leave.s    IL_0119
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_00ff:  stloc.s    V_6
    IL_0101:  ldarg.0
    IL_0102:  ldc.i4.s   -2
    IL_0104:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_0109:  ldarg.0
    IL_010a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<Main>d__0.<>t__builder""
    IL_010f:  ldloc.s    V_6
    IL_0111:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0116:  nop
    IL_0117:  leave.s    IL_012e
  }
  // sequence point: }
  IL_0119:  ldarg.0
  IL_011a:  ldc.i4.s   -2
  IL_011c:  stfld      ""int C.<Main>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0121:  ldarg.0
  IL_0122:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<Main>d__0.<>t__builder""
  IL_0127:  ldloc.1
  IL_0128:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_012d:  nop
  IL_012e:  ret
}
", sequencePoints: "C+<Main>d__0.MoveNext", source: source);
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_Awaitable()
        {
            string source = @"
public struct C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        {
            await using var x = new C();
            System.Console.Write(""using "");
        }
        System.Console.Write(""return"");
        return 1;
    }
    public Awaitable DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        System.Console.Write($""dispose_end "");
        return new Awaitable();
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_ReturnsTask()
        {
            string source = @"
public struct C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        {
            await using var x = new C();
            System.Console.Write(""using "");
        }
        System.Console.Write(""return"");
        return 1;
    }
    public async System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose_end "");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [Fact]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_ReturnsTaskOfInt()
        {
            string source = @"
public struct C
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        {
            await using var x = new C();
            System.Console.Write(""using "");
        }
        System.Console.Write(""return"");
        return 1;
    }
    public async System.Threading.Tasks.Task<int> DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose_end "");
        return 1;
    }
}
";
            // it's okay to await `Task<int>` even if we don't care about the result
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [Fact]
        public void TestInRegularMethod()
        {
            string source = @"
class C
{
    void M()
    {
        await using var x = new object();
        await using (var y = new object()) { }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            comp.VerifyDiagnostics(
                // (6,9): error CS8410: 'object': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using var x = new object();
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "await using var x = new object();").WithArguments("object").WithLocation(6, 9),
                // (6,9): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         await using var x = new object();
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(6, 9),
                // (7,9): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         await using (var y = new object()) { }
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(7, 9),
                // (7,22): error CS8410: 'object': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (var y = new object()) { }
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "var y = new object()").WithArguments("object").WithLocation(7, 22)
                );
        }

        [Fact]
        [WorkItem(30956, "https://github.com/dotnet/roslyn/issues/30956")]
        public void GetAwaiterBoxingConversion()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

struct StructAwaitable { }

class Disposable
{
    public StructAwaitable DisposeAsync() => new StructAwaitable();
}

static class Extensions
{
    public static TaskAwaiter GetAwaiter(this object x)
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
        await using (new Disposable())
        {
        }
    }
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "StructAwaitable");
        }

        [Fact, WorkItem(45111, "https://github.com/dotnet/roslyn/issues/45111")]
        public void MissingIAsyncDisposableInterfaceInPatternDisposal()
        {
            var source = @"
using System.Threading.Tasks;

await using (new C()) { }

class C
{
    public Task DisposeAsync()
    {
        System.Console.Write(""DISPOSED"");
        return Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(source);
            Assert.Equal(TypeKind.Error, comp.GetWellKnownType(WellKnownType.System_IAsyncDisposable).TypeKind);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "DISPOSED");
        }

        [Fact, WorkItem(45111, "https://github.com/dotnet/roslyn/issues/45111")]
        public void MissingIDisposableInterfaceOnClass()
        {
            var source = @"
using (new C()) { }

class C
{
    public void Dispose()
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(SpecialType.System_IDisposable);
            comp.VerifyDiagnostics(
                // source(2,8): error CS1674: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                // using (new C()) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "new C()").WithArguments("C").WithLocation(2, 8)
                );
        }

        [Fact, WorkItem(45111, "https://github.com/dotnet/roslyn/issues/45111")]
        public void MissingIDisposableInterfaceOnRefStruct()
        {
            var source = @"
using (new C()) { }

ref struct C
{
    public void Dispose()
    {
        System.Console.Write(""DISPOSED"");
    }
}
";
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(SpecialType.System_IDisposable);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "DISPOSED");
        }

        [Fact]
        public void RefStruct_AwaitInside()
        {
            var source = """
                using System.Threading.Tasks;
                class C
                {
                    async Task M()
                    {
                        using (new R())
                        {
                            await Task.Yield();
                        }
                    }
                }
                ref struct R
                {
                    public void Dispose() { }
                }
                """;
            // https://github.com/dotnet/roslyn/issues/73280 - should not be a langversion error since this remains an error in C# 13
            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (6,16): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         using (new R())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "new R()").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 16));

            var expectedDiagnostics = new[]
            {
                // (6,16): error CS4007: Instance of type 'R' cannot be preserved across 'await' or 'yield' boundary.
                //         using (new R())
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "new R()").WithArguments("R").WithLocation(6, 16)
            };

            CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilation(source).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void RefStruct_YieldReturnInside()
        {
            var source = """
                using System.Collections.Generic;
                class C
                {
                    IEnumerable<int> M()
                    {
                        using (new R())
                        {
                            yield return 1;
                        }
                    }
                }
                ref struct R
                {
                    public void Dispose() { }
                }
                """;

            var expectedDiagnostics = new[]
            {
                // (6,16): error CS4007: Instance of type 'R' cannot be preserved across 'await' or 'yield' boundary.
                //         using (new R())
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "new R()").WithArguments("R").WithLocation(6, 16)
            };

            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilation(source).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void RefStruct_YieldBreakInside()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                class C
                {
                    static void Main()
                    {
                        foreach (var x in M(true)) { Console.Write(x); }
                        Console.Write(" ");
                        foreach (var x in M(false)) { Console.Write(x); }
                    }
                    static IEnumerable<int> M(bool b)
                    {
                        yield return 123;
                        using (new R())
                        {
                            if (b) { yield break; }
                        }
                        yield return 456;
                    }
                }
                ref struct R
                {
                    public R() => Console.Write("C");
                    public void Dispose() => Console.Write("D");
                }
                """;

            var expectedOutput = "123CD 123CD456";

            CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular13).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void RefStruct_AwaitResource()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                class C
                {
                    static async Task Main()
                    {
                        Console.Write("1");
                        using ((await GetC()).GetR())
                        {
                            Console.Write("2");
                        }
                        Console.Write("3");
                    }
                    static async Task<C> GetC()
                    {
                        Console.Write("Ga");
                        await Task.Yield();
                        Console.Write("Gb");
                        return new C();
                    }
                    R GetR() => new R();
                }
                ref struct R
                {
                    public R() => Console.Write("C");
                    public void Dispose() => Console.Write("D");
                }
                """;
            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (8,16): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         using ((await GetC()).GetR())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "(await GetC()).GetR()").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 16));

            var expectedOutput = "1GaGbC2D3";

            CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular13, verify: Verification.FailsILVerify).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();
        }

        [Fact]
        public void RefStruct_AwaitOutside()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                class C
                {
                    static async Task Main()
                    {
                        Console.Write("1");
                        await Task.Yield();
                        Console.Write("2");
                        using (new R())
                        {
                            Console.Write("3");
                        }
                        Console.Write("4");
                        await Task.Yield();
                        Console.Write("5");
                    }
                }
                ref struct R
                {
                    public R() => Console.Write("C");
                    public void Dispose() => Console.Write("D");
                }
                """;
            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (10,16): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         using (new R())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "new R()").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(10, 16));

            var expectedOutput = "12C3D45";

            CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular13).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void RefStruct_YieldReturnOutside()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                class C
                {
                    static void Main()
                    {
                        foreach (var x in M())
                        {
                            Console.Write(x);
                        }
                    }
                    static IEnumerable<string> M()
                    {
                        Console.Write("1");
                        yield return "a";
                        Console.Write("2");
                        using (new R())
                        {
                            Console.Write("3");
                        }
                        Console.Write("4");
                        yield return "b";
                        Console.Write("5");
                    }
                }
                ref struct R
                {
                    public R() => Console.Write("C");
                    public void Dispose() => Console.Write("D");
                }
                """;

            var expectedOutput = "1a2C3D4b5";

            CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expectedOutput, parseOptions: TestOptions.Regular13).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void RefStruct_AwaitUsing()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                class C
                {
                    static async Task Main()
                    {
                        Console.Write("1");
                        await using (new R())
                        {
                            Console.Write("2");
                        }
                        Console.Write("3");
                    }
                }
                ref struct R
                {
                    public R() => Console.Write("C");
                    public ValueTask DisposeAsync()
                    {
                        Console.Write("D");
                        return default;
                    }
                }
                """;
            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (8,22): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await using (new R())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "new R()").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 22));

            var expectedOutput = "1C2D3";

            var comp = CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular13, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void RefStruct_AwaitUsing_AwaitInside()
        {
            var source = """
                using System.Threading.Tasks;
                class C
                {
                    async Task M()
                    {
                        await using (new R())
                        {
                            await Task.Yield();
                        }
                    }
                }
                ref struct R
                {
                    public ValueTask DisposeAsync() => default;
                }
                """;
            // https://github.com/dotnet/roslyn/issues/73280 - should not be a langversion error since this remains an error in C# 13
            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (6,22): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await using (new R())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "new R()").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 22));

            var expectedDiagnostics = new[]
            {
                // (6,22): error CS4007: Instance of type 'R' cannot be preserved across 'await' or 'yield' boundary.
                //         await using (new R())
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "new R()").WithArguments("R").WithLocation(6, 22)
            };

            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilationWithTasksExtensions(source).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void RefStruct_AwaitUsing_YieldReturnInside()
        {
            var source = """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                class C
                {
                    async IAsyncEnumerable<int> M()
                    {
                        await using (new R())
                        {
                            yield return 123;
                        }
                    }
                }
                ref struct R
                {
                    public ValueTask DisposeAsync() => default;
                }
                """ + AsyncStreamsTypes;
            // https://github.com/dotnet/roslyn/issues/73280 - should not be a langversion error since this remains an error in C# 13
            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (7,22): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await using (new R())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "new R()").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 22));

            var expectedDiagnostics = new[]
            {
                // (7,22): error CS4007: Instance of type 'R' cannot be preserved across 'await' or 'yield' boundary.
                //         await using (new R())
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "new R()").WithArguments("R").WithLocation(7, 22)
            };

            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilationWithTasksExtensions(source).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void RefStruct_AwaitUsing_YieldReturnInside_Var()
        {
            var source = """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                class C
                {
                    async IAsyncEnumerable<int> M()
                    {
                        await using var _ = new R();
                        yield return 123;
                    }
                }
                ref struct R
                {
                    public ValueTask DisposeAsync() => default;
                }
                """ + AsyncStreamsTypes;
            // https://github.com/dotnet/roslyn/issues/73280 - should not be a langversion error since this remains an error in C# 13
            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (7,21): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await using var _ = new R();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "var").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 21));

            var expectedDiagnostics = new[]
            {
                // (7,25): error CS4007: Instance of type 'R' cannot be preserved across 'await' or 'yield' boundary.
                //         await using var _ = new R();
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "_ = new R()").WithArguments("R").WithLocation(7, 25)
            };

            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilationWithTasksExtensions(source).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void RefStruct_AwaitUsing_YieldBreakInside()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                class C
                {
                    static async Task Main()
                    {
                        await foreach (var x in M(true)) { Console.Write(x); }
                        Console.Write(" ");
                        await foreach (var x in M(false)) { Console.Write(x); }
                    }
                    static async IAsyncEnumerable<int> M(bool b)
                    {
                        yield return 1;
                        await using (new R())
                        {
                            if (b) { yield break; }
                        }
                        yield return 2;
                    }
                }
                ref struct R
                {
                    public R() => Console.Write("C");
                    public ValueTask DisposeAsync()
                    {
                        Console.Write("D");
                        return default;
                    }
                }
                """ + AsyncStreamsTypes;
            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (15,22): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         await using (new R())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "new R()").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(15, 22));

            var expectedOutput = "1CD 1CD2";

            var comp = CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular13, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73691")]
        public void PatternBasedFails_WithInterfaceImplementation()
        {
            var source = """
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

await using var x = new Class1();

internal class Class1 : IAsyncDisposable
{
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        System.Console.Write("DISPOSED");
        await Task.Yield();
    }
}

internal static class EnumerableExtensions
{
    public static ValueTask DisposeAsync(this IEnumerable<object> objects)
    {
        throw null;
    }
}
""";
            var comp = CreateCompilationWithTasksExtensions([source, IAsyncDisposableDefinition]);
            CompileAndVerify(comp, expectedOutput: "DISPOSED").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73691")]
        public void PatternBasedFails_NoInterfaceImplementation()
        {
            var source = """
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

await using var x = new Class1();

internal class Class1 { }

internal static class EnumerableExtensions
{
    public static ValueTask DisposeAsync(this IEnumerable<object> objects)
    {
        throw null;
    }
}
""";
            var comp = CreateCompilationWithTasksExtensions([source, IAsyncDisposableDefinition]);
            comp.VerifyEmitDiagnostics(
                // (5,1): error CS8410: 'Class1': type used in an asynchronous using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                // await using var x = new Class1();
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "await using var x = new Class1();").WithArguments("Class1").WithLocation(5, 1));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73691")]
        public void PatternBasedFails_WithInterfaceImplementation_UseSite()
        {
            // We attempt to bind pattern-based disposal (and collect diagnostics)
            // then we bind to the IAsyncDisposable interface, which reports a use-site error
            // and so we add the collected diagnostics
            var source = """
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

internal class Class1 : IAsyncDisposable
{
    ValueTask IAsyncDisposable.DisposeAsync()
    {
        throw null;
    }

    public async Task MethodWithCompilerError()
    {
        await using var x = new Class1();
    }
}

internal static class EnumerableExtensions
{
    public static ValueTask DisposeAsync(this IEnumerable<object> objects)
    {
        throw null;
    }
}

namespace System.Threading.Tasks
{
    public struct ValueTask
    {
        public Awaiter GetAwaiter() => null;
        public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
        {
            public void OnCompleted(Action a) { }
            public bool IsCompleted => true;
            public void GetResult() { }
        }
    }
}
""";

            var ilSrc = """
.class interface public auto ansi abstract beforefieldinit System.IAsyncDisposable
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = ( 01 00 02 68 69 00 00 )
    .method public hidebysig newslot abstract virtual instance valuetype [mscorlib]System.Threading.Tasks.ValueTask DisposeAsync () cil managed 
    {
    }
}
""";
            var comp = CreateCompilationWithIL(source, ilSrc);
            comp.VerifyEmitDiagnostics(
                // (5,16): error CS9041: 'IAsyncDisposable' requires compiler feature 'hi', which is not supported by this version of the C# compiler.
                // internal class Class1 : IAsyncDisposable
                Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "Class1").WithArguments("System.IAsyncDisposable", "hi").WithLocation(5, 16),
                // (5,25): error CS9041: 'IAsyncDisposable' requires compiler feature 'hi', which is not supported by this version of the C# compiler.
                // internal class Class1 : IAsyncDisposable
                Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "IAsyncDisposable").WithArguments("System.IAsyncDisposable", "hi").WithLocation(5, 25),
                // (7,15): error CS9041: 'IAsyncDisposable' requires compiler feature 'hi', which is not supported by this version of the C# compiler.
                //     ValueTask IAsyncDisposable.DisposeAsync()
                Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "IAsyncDisposable").WithArguments("System.IAsyncDisposable", "hi").WithLocation(7, 15),
                // (7,32): error CS0539: 'Class1.DisposeAsync()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     ValueTask IAsyncDisposable.DisposeAsync()
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "DisposeAsync").WithArguments("Class1.DisposeAsync()").WithLocation(7, 32),
                // (14,9): error CS9041: 'IAsyncDisposable' requires compiler feature 'hi', which is not supported by this version of the C# compiler.
                //         await using var x = new Class1();
                Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "await using var x = new Class1();").WithArguments("System.IAsyncDisposable", "hi").WithLocation(14, 9),
                // (14,9): error CS9041: 'IAsyncDisposable' requires compiler feature 'hi', which is not supported by this version of the C# compiler.
                //         await using var x = new Class1();
                Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "await").WithArguments("System.IAsyncDisposable", "hi").WithLocation(14, 9));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72819")]
        public void PatternBasedFails_AwaitUsing_08()
        {
            var src = """
using System;
using System.Threading.Tasks;

interface IMyAsyncDisposable1
{
    ValueTask DisposeAsync();
}

interface IMyAsyncDisposable2
{
    ValueTask DisposeAsync();
}

struct S2 : IMyAsyncDisposable1, IMyAsyncDisposable2, IAsyncDisposable
{
    ValueTask IMyAsyncDisposable1.DisposeAsync() => throw null;
    ValueTask IMyAsyncDisposable2.DisposeAsync() => throw null;

    public ValueTask DisposeAsync()
    {
        System.Console.Write('D');
        return ValueTask.CompletedTask;
    }
}

class C
{
    static async Task Main()
    {
        await Test<S2>();
    }

    static async Task Test<T>() where T : IMyAsyncDisposable1, IMyAsyncDisposable2, IAsyncDisposable, new()
    {
        await using (new T())
        {
            System.Console.Write(123);
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "123D" : null,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2066463")]
        public void PatternBasedFails_AwaitUsing_Private()
        {
            var src = """
using System;
using System.Threading.Tasks;

await using var service = new Service1();

public sealed class Service1 : IAsyncDisposable
{
    private Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        System.Console.Write("ran");
        return new ValueTask(DisposeAsync());
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "ran" : null,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics();
        }
    }
}
