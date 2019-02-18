// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private static readonly string s_interfaces = @"
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (6,9): error CS8652: The feature 'async streams' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "await").WithArguments("async streams").WithLocation(6, 9)
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (3,11): error CS0738: 'C' does not implement interface member 'IAsyncDisposable.DisposeAsync()'. 'C.DisposeAsync()' cannot implement 'IAsyncDisposable.DisposeAsync()' because it does not have the matching return type of 'ValueTask'.
                // class C : System.IAsyncDisposable
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "System.IAsyncDisposable").WithArguments("C", "System.IAsyncDisposable.DisposeAsync()", "C.DisposeAsync()", "System.Threading.Tasks.ValueTask").WithLocation(3, 11),
                // (9,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public async Task DisposeAsync() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "DisposeAsync").WithLocation(9, 23)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.UnsafeDebugDll);
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (8,13): error CS1996: Cannot await in the body of a lock statement
                //             await using (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await").WithLocation(8, 13)
                );
        }

        [Fact]
        public void TestWithObsoleteDisposeAsync()
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            // https://github.com/dotnet/roslyn/issues/30257 Confirm whether this behavior is ok (currently matching behavior of obsolete Dispose in non-async using)
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (17,43): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         async System.Threading.Tasks.Task local()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "local").WithLocation(17, 43),
                // (6,42): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         System.Action lambda1 = async () =>
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(6, 42)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using caught message");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
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
                // (6,9): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(6, 9),
                // (6,22): error CS8410: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "new C()").WithArguments("C").WithLocation(6, 22),
                // (9,9): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(9, 9),
                // (9,22): error CS8410: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'
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
                // (6,9): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(6, 9),
                // (6,9): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<Task<int>>'.
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await").WithArguments("System.Threading.Tasks.Task<int>").WithLocation(6, 9),
                // (6,22): error CS8410: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "new C()").WithArguments("C").WithLocation(6, 22),
                // (9,9): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(9, 9),
                // (9,9): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<Task<int>>'.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await").WithArguments("System.Threading.Tasks.Task<int>").WithLocation(9, 9),
                // (9,22): error CS8410: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
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
                // (6,9): error CS0518: Predefined type 'System.IDisposable' is not defined or imported
                //         using (new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "using").WithArguments("System.IDisposable").WithLocation(6, 9),
                // (6,16): error CS1674: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable' or implement a suitable 'Dispose' method.
                //         using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "new C()").WithArguments("C").WithLocation(6, 16),
                // (9,9): error CS0518: Predefined type 'System.IDisposable' is not defined or imported
                //         using (var x = new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "using").WithArguments("System.IDisposable").WithLocation(9, 9),
                // (9,16): error CS1674: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable' or implement a suitable 'Dispose' method.
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
        public void TestBadDisposeAsync()
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
        public void TestMissingTaskType()
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
            var libComp = CreateCompilationWithTasksExtensions(lib_cs + s_interfaces);
            var comp = CreateCompilationWithTasksExtensions(comp_cs, references: new[] { libComp.EmitToImageReference() }, options: TestOptions.DebugExe);
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_ValueTask);
            comp.VerifyDiagnostics(
                // (6,9): error CS0518: Predefined type 'System.Threading.Tasks.ValueTask' is not defined or imported
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.Threading.Tasks.ValueTask").WithLocation(6, 9)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body Dispose");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
            comp.VerifyDiagnostics(
                // (6,22): error CS8417: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'. Did you mean 'using' rather than 'await using'?
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDispWrongAsync, "new C()").WithArguments("C").WithLocation(6, 22)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe, references: new[] { CSharpRef });
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync end");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe, references: new[] { CSharpRef });
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync end");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void TestWithExpression()
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      304 (0x130)
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
    IL_0008:  brfalse    IL_0097
    IL_000d:  br.s       IL_000f
    IL_000f:  nop
    IL_0010:  ldarg.0
    IL_0011:  newobj     ""C..ctor()""
    IL_0016:  stfld      ""C C.<Main>d__0.<>s__1""
    IL_001b:  ldarg.0
    IL_001c:  ldnull
    IL_001d:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_0022:  ldarg.0
    IL_0023:  ldc.i4.0
    IL_0024:  stfld      ""int C.<Main>d__0.<>s__3""
    .try
    {
      IL_0029:  nop
      IL_002a:  ldstr      ""body ""
      IL_002f:  call       ""void System.Console.Write(string)""
      IL_0034:  nop
      IL_0035:  br.s       IL_0037
      IL_0037:  ldarg.0
      IL_0038:  ldc.i4.1
      IL_0039:  stfld      ""int C.<Main>d__0.<>s__3""
      IL_003e:  leave.s    IL_004a
    }
    catch object
    {
      IL_0040:  stloc.1
      IL_0041:  ldarg.0
      IL_0042:  ldloc.1
      IL_0043:  stfld      ""object C.<Main>d__0.<>s__2""
      IL_0048:  leave.s    IL_004a
    }
    IL_004a:  ldarg.0
    IL_004b:  ldfld      ""C C.<Main>d__0.<>s__1""
    IL_0050:  brfalse.s  IL_00bb
    IL_0052:  ldarg.0
    IL_0053:  ldfld      ""C C.<Main>d__0.<>s__1""
    IL_0058:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_005d:  stloc.3
    IL_005e:  ldloca.s   V_3
    IL_0060:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_0065:  stloc.2
    IL_0066:  ldloca.s   V_2
    IL_0068:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_006d:  brtrue.s   IL_00b3
    IL_006f:  ldarg.0
    IL_0070:  ldc.i4.0
    IL_0071:  dup
    IL_0072:  stloc.0
    IL_0073:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_0078:  ldarg.0
    IL_0079:  ldloc.2
    IL_007a:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_007f:  ldarg.0
    IL_0080:  stloc.s    V_4
    IL_0082:  ldarg.0
    IL_0083:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_0088:  ldloca.s   V_2
    IL_008a:  ldloca.s   V_4
    IL_008c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<Main>d__0)""
    IL_0091:  nop
    IL_0092:  leave      IL_012f
    IL_0097:  ldarg.0
    IL_0098:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_009d:  stloc.2
    IL_009e:  ldarg.0
    IL_009f:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<Main>d__0.<>u__1""
    IL_00a4:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_00aa:  ldarg.0
    IL_00ab:  ldc.i4.m1
    IL_00ac:  dup
    IL_00ad:  stloc.0
    IL_00ae:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00b3:  ldloca.s   V_2
    IL_00b5:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_00ba:  nop
    IL_00bb:  ldarg.0
    IL_00bc:  ldfld      ""object C.<Main>d__0.<>s__2""
    IL_00c1:  stloc.1
    IL_00c2:  ldloc.1
    IL_00c3:  brfalse.s  IL_00e0
    IL_00c5:  ldloc.1
    IL_00c6:  isinst     ""System.Exception""
    IL_00cb:  stloc.s    V_5
    IL_00cd:  ldloc.s    V_5
    IL_00cf:  brtrue.s   IL_00d3
    IL_00d1:  ldloc.1
    IL_00d2:  throw
    IL_00d3:  ldloc.s    V_5
    IL_00d5:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00da:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00df:  nop
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      ""int C.<Main>d__0.<>s__3""
    IL_00e6:  stloc.s    V_6
    IL_00e8:  ldloc.s    V_6
    IL_00ea:  ldc.i4.1
    IL_00eb:  beq.s      IL_00ef
    IL_00ed:  br.s       IL_00f1
    IL_00ef:  leave.s    IL_011b
    IL_00f1:  ldarg.0
    IL_00f2:  ldnull
    IL_00f3:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_00f8:  ldarg.0
    IL_00f9:  ldnull
    IL_00fa:  stfld      ""C C.<Main>d__0.<>s__1""
    IL_00ff:  leave.s    IL_011b
  }
  catch System.Exception
  {
    IL_0101:  stloc.s    V_5
    IL_0103:  ldarg.0
    IL_0104:  ldc.i4.s   -2
    IL_0106:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_010b:  ldarg.0
    IL_010c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_0111:  ldloc.s    V_5
    IL_0113:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0118:  nop
    IL_0119:  leave.s    IL_012f
  }
  IL_011b:  ldarg.0
  IL_011c:  ldc.i4.s   -2
  IL_011e:  stfld      ""int C.<Main>d__0.<>1__state""
  IL_0123:  ldarg.0
  IL_0124:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_0129:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_012e:  nop
  IL_012f:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,22): error CS8410: 'method group': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'
                //         await using (Main)
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "Main").WithArguments("method group").WithLocation(6, 22)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe, references: new[] { CSharpRef });
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void TestWithStructExpression()
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
            verifier.VerifyIL("S.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      296 (0x128)
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
    IL_0008:  brfalse    IL_0096
    IL_000d:  br.s       IL_000f
    IL_000f:  nop
    IL_0010:  ldarg.0
    IL_0011:  ldflda     ""S S.<Main>d__0.<>s__1""
    IL_0016:  initobj    ""S""
    IL_001c:  ldarg.0
    IL_001d:  ldnull
    IL_001e:  stfld      ""object S.<Main>d__0.<>s__2""
    IL_0023:  ldarg.0
    IL_0024:  ldc.i4.0
    IL_0025:  stfld      ""int S.<Main>d__0.<>s__3""
    .try
    {
      IL_002a:  nop
      IL_002b:  ldstr      ""body ""
      IL_0030:  call       ""void System.Console.Write(string)""
      IL_0035:  nop
      IL_0036:  br.s       IL_0038
      IL_0038:  ldarg.0
      IL_0039:  ldc.i4.1
      IL_003a:  stfld      ""int S.<Main>d__0.<>s__3""
      IL_003f:  leave.s    IL_004b
    }
    catch object
    {
      IL_0041:  stloc.1
      IL_0042:  ldarg.0
      IL_0043:  ldloc.1
      IL_0044:  stfld      ""object S.<Main>d__0.<>s__2""
      IL_0049:  leave.s    IL_004b
    }
    IL_004b:  ldarg.0
    IL_004c:  ldflda     ""S S.<Main>d__0.<>s__1""
    IL_0051:  constrained. ""S""
    IL_0057:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_005c:  stloc.3
    IL_005d:  ldloca.s   V_3
    IL_005f:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_0064:  stloc.2
    IL_0065:  ldloca.s   V_2
    IL_0067:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_006c:  brtrue.s   IL_00b2
    IL_006e:  ldarg.0
    IL_006f:  ldc.i4.0
    IL_0070:  dup
    IL_0071:  stloc.0
    IL_0072:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_0077:  ldarg.0
    IL_0078:  ldloc.2
    IL_0079:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter S.<Main>d__0.<>u__1""
    IL_007e:  ldarg.0
    IL_007f:  stloc.s    V_4
    IL_0081:  ldarg.0
    IL_0082:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
    IL_0087:  ldloca.s   V_2
    IL_0089:  ldloca.s   V_4
    IL_008b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, S.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref S.<Main>d__0)""
    IL_0090:  nop
    IL_0091:  leave      IL_0127
    IL_0096:  ldarg.0
    IL_0097:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter S.<Main>d__0.<>u__1""
    IL_009c:  stloc.2
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter S.<Main>d__0.<>u__1""
    IL_00a3:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_00a9:  ldarg.0
    IL_00aa:  ldc.i4.m1
    IL_00ab:  dup
    IL_00ac:  stloc.0
    IL_00ad:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_00b2:  ldloca.s   V_2
    IL_00b4:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_00b9:  nop
    IL_00ba:  ldarg.0
    IL_00bb:  ldfld      ""object S.<Main>d__0.<>s__2""
    IL_00c0:  stloc.1
    IL_00c1:  ldloc.1
    IL_00c2:  brfalse.s  IL_00df
    IL_00c4:  ldloc.1
    IL_00c5:  isinst     ""System.Exception""
    IL_00ca:  stloc.s    V_5
    IL_00cc:  ldloc.s    V_5
    IL_00ce:  brtrue.s   IL_00d2
    IL_00d0:  ldloc.1
    IL_00d1:  throw
    IL_00d2:  ldloc.s    V_5
    IL_00d4:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00d9:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00de:  nop
    IL_00df:  ldarg.0
    IL_00e0:  ldfld      ""int S.<Main>d__0.<>s__3""
    IL_00e5:  stloc.s    V_6
    IL_00e7:  ldloc.s    V_6
    IL_00e9:  ldc.i4.1
    IL_00ea:  beq.s      IL_00ee
    IL_00ec:  br.s       IL_00f0
    IL_00ee:  leave.s    IL_0113
    IL_00f0:  ldarg.0
    IL_00f1:  ldnull
    IL_00f2:  stfld      ""object S.<Main>d__0.<>s__2""
    IL_00f7:  leave.s    IL_0113
  }
  catch System.Exception
  {
    IL_00f9:  stloc.s    V_5
    IL_00fb:  ldarg.0
    IL_00fc:  ldc.i4.s   -2
    IL_00fe:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_0103:  ldarg.0
    IL_0104:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
    IL_0109:  ldloc.s    V_5
    IL_010b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0110:  nop
    IL_0111:  leave.s    IL_0127
  }
  IL_0113:  ldarg.0
  IL_0114:  ldc.i4.s   -2
  IL_0116:  stfld      ""int S.<Main>d__0.<>1__state""
  IL_011b:  ldarg.0
  IL_011c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
  IL_0121:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0126:  nop
  IL_0127:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
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
}" + s_interfaces);
            comp.VerifyDiagnostics(
                // (10,9): error CS0165: Use of unassigned local variable 'y'
                //         y++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(10, 9),
                // (11,9): error CS0165: Use of unassigned local variable 'z'
                //         z++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(11, 9)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ctor1 ctor2 body dispose2_start dispose2_end dispose1_start dispose1_end");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ctor1 ctor2 body dispose2 dispose1 caught");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(source + s_interfaces, options: TestOptions.DebugExe);
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
            var first = new AwaitExpressionInfo(new AwaitableInfo(getAwaiter1, isCompleted1, getResult1));

            var nulls1 = new AwaitExpressionInfo(new AwaitableInfo(null, isCompleted1, getResult1));
            var nulls2 = new AwaitExpressionInfo(new AwaitableInfo(getAwaiter1, null, getResult1));
            var nulls3 = new AwaitExpressionInfo(new AwaitableInfo(getAwaiter1, isCompleted1, null));

            Assert.False(first.Equals(nulls1));
            Assert.False(first.Equals(nulls2));
            Assert.False(first.Equals(nulls3));

            Assert.False(nulls1.Equals(first));
            Assert.False(nulls2.Equals(first));
            Assert.False(nulls3.Equals(first));

            object nullObj = null;
            Assert.False(first.Equals(nullObj));

            var getAwaiter2 = (MethodSymbol)comp.GetMember("D.GetAwaiter");
            var isCompleted2 = (PropertySymbol)comp.GetMember("D.IsCompleted");
            var getResult2 = (MethodSymbol)comp.GetMember("D.GetResult");
            var second1 = new AwaitExpressionInfo(new AwaitableInfo(getAwaiter2, isCompleted1, getResult1));
            var second2 = new AwaitExpressionInfo(new AwaitableInfo(getAwaiter1, isCompleted2, getResult1));
            var second3 = new AwaitExpressionInfo(new AwaitableInfo(getAwaiter1, isCompleted1, getResult2));
            var second4 = new AwaitExpressionInfo(new AwaitableInfo(getAwaiter2, isCompleted2, getResult2));

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

            var another = new AwaitExpressionInfo(new AwaitableInfo(getAwaiter1, isCompleted1, getResult1));
            Assert.True(first.GetHashCode() == another.GetHashCode());
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,22): error CS8410: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "var x = new C()").WithArguments("C").WithLocation(6, 22)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "dispose");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,22): error CS8410: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "new C()").WithArguments("C").WithLocation(6, 22)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(32316, "https://github.com/dotnet/roslyn/issues/32316")]
        public void TestPatternBasedDisposal_InterfacePreferredOverInstanceMethod()
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
    async System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Yield();
        System.Console.Write($""dispose_end "");
    }
    public System.Threading.Tasks.ValueTask DisposeAsync()
        => throw null;
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end(0) return");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
            comp.VerifyDiagnostics(
                // (6,22): error CS4008: Cannot await 'void'
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitArgVoidCall, "var x = new C()").WithLocation(6, 22)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
            comp.VerifyDiagnostics(
                // (6,22): error CS1061: 'int' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "var x = new C()").WithArguments("int", "GetAwaiter").WithLocation(6, 22)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
            comp.VerifyDiagnostics(
                // (6,22): error CS0122: 'C.DisposeAsync()' is inaccessible due to its protection level
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAccess, "var x = new C()").WithArguments("C.DisposeAsync()").WithLocation(6, 22),
                // (6,22): error CS8410: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "var x = new C()").WithArguments("C").WithLocation(6, 22)
                );
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_interfaces });
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
    }
}
