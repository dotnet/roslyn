// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.AsyncStreams)]
    public class CodeGenAsyncDisposableTests : CSharpTestBase
    {
        private static readonly string s_interfaces = @"
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.Task DisposeAsync();
    }
}
";

        [Fact]
        public void TestWithCSharp7_1()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    async System.Threading.Tasks.Task M()
    {
        using await (var x = new C())
        {
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,15): error CS8302: Feature 'async streams' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         using await (var x = new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "await").WithArguments("async streams", "7.2").WithLocation(6, 15)
                );
            // PROTOTYPE(async-streams) LangVersion for async-streams will be adjusted before release
        }

        [Fact]
        public void TestInNonAsyncVoidMethod()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    void M()
    {
        using await (var x = new C())
        {
            return;
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,15): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         using await (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(6, 15)
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
        using await (var x = new C())
        {
            return 1;
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,15): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<int>'.
                //         using await (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await").WithArguments("int").WithLocation(6, 15)
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
            using await (var y = new C())
            {
                return;
            }
        };
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (8,19): error CS4034: The 'await' operator can only be used within an async lambda expression. Consider marking this lambda expression with the 'async' modifier.
                //             using await (var y = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncLambda, "await").WithArguments("lambda expression").WithLocation(8, 19)
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
            using await (var y = new C())
            {
                System.Console.Write(""body "");
            }
            System.Console.Write(""end"");
        };
        await x();
    }
    public async Task DisposeAsync()
    {
        System.Console.Write(""DisposeAsync1 "");
        await Task.Delay(10);
        System.Console.Write(""DisposeAsync2 "");
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
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
            using await (var x = new C())
            {
                return 1;
            }
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (8,19): error CS4004: Cannot await in an unsafe context
                //             using await (var x = new C())
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await").WithLocation(8, 19)
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
            using await (var x = new C())
            {
                return 1;
            }
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (8,19): error CS1996: Cannot await in the body of a lock statement
                //             using await (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await").WithLocation(8, 19)
                );
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
            using await (var x = new C())
            {
                System.Console.Write(""using "");
            }
            System.Console.Write(""end"");
        }
    }
    public async System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Delay(10);
        System.Console.Write($""dispose_end "");
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
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
                using await (null) { }
            }; // these awaits don't count towards Main method
        };

        async System.Threading.Tasks.Task local()
        {
            await local();
            using await (null) { }
            // neither do these
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
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
                using await (null) { }
            }; // these awaits don't count towards lambda
        };

        System.Action lambda2 = async () => await local2(); // this await counts towards lambda

        async System.Threading.Tasks.Task local()
        {
            System.Func<System.Threading.Tasks.Task> c = innerLocal;

            async System.Threading.Tasks.Task innerLocal()
            {
                await local();
                using await (null) { }
                // these awaits don't count towards lambda either
            }
        }

        async System.Threading.Tasks.Task local2() => await local(); // this await counts towards local function
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
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
            using await (var x = new C())
            {
                System.Console.Write(""using "");
            }
        }

        System.Console.Write(""return"");
        return 1;
    }
    public async System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write($""dispose_start "");
        await System.Threading.Tasks.Task.Delay(10);
        System.Console.Write($""dispose_end "");
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "using dispose_start dispose_end return");
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
            await Task.Delay(10);
            System.Console.Write(""after"");
        }
        return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
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
        using await (new C())
        {
        }
        using await (var x = new C())
        {
            return 1;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (6,15): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         using await (new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(6, 15),
                // (6,22): error CS9000: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'
                //         using await (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "new C()").WithArguments("C").WithLocation(6, 22),
                // (9,15): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         using await (var x = new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(9, 15),
                // (9,22): error CS9000: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'
                //         using await (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "var x = new C()").WithArguments("C").WithLocation(9, 22)
                );
        }

        [Fact]
        public void TestMissingIAsyncDisposableAndMissingTaskAndMissingAsync()
        {
            string source = @"
class C
{
    System.Threading.Tasks.Task<int> M()
    {
        using await (new C())
        {
        }
        using await (var x = new C())
        {
            return 1;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_Task);
            comp.VerifyDiagnostics(
                // (6,15): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         using await (new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(6, 15),
                // (6,22): error CS9000: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'
                //         using await (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "new C()").WithArguments("C").WithLocation(6, 22),
                // (6,15): error CS0518: Predefined type 'System.Threading.Tasks.Task' is not defined or imported
                //         using await (new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.Threading.Tasks.Task").WithLocation(6, 15),
                // (6,15): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<Task<int>>'.
                //         using await (new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await").WithArguments("System.Threading.Tasks.Task<int>").WithLocation(6, 15),
                // (9,15): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         using await (var x = new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(9, 15),
                // (9,22): error CS9000: 'C': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'
                //         using await (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "var x = new C()").WithArguments("C").WithLocation(9, 22),
                // (9,15): error CS0518: Predefined type 'System.Threading.Tasks.Task' is not defined or imported
                //         using await (var x = new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.Threading.Tasks.Task").WithLocation(9, 15),
                // (9,15): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<Task<int>>'.
                //         using await (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await").WithArguments("System.Threading.Tasks.Task<int>").WithLocation(9, 15),
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
                // (6,16): error CS1674: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "new C()").WithArguments("C").WithLocation(6, 16),
                // (9,9): error CS0518: Predefined type 'System.IDisposable' is not defined or imported
                //         using (var x = new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "using").WithArguments("System.IDisposable").WithLocation(9, 9),
                // (9,16): error CS1674: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable'
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
        using await (new C()) { }
        using await (var x = new C()) { return 1; }
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (13,15): error CS0656: Missing compiler required member 'System.IAsyncDisposable.DisposeAsync'
                //         using await (new C()) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "await").WithArguments("System.IAsyncDisposable", "DisposeAsync").WithLocation(13, 15),
                // (14,15): error CS0656: Missing compiler required member 'System.IAsyncDisposable.DisposeAsync'
                //         using await (var x = new C()) { return 1; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "await").WithArguments("System.IAsyncDisposable", "DisposeAsync").WithLocation(14, 15)
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
            var comp = CreateCompilationWithMscorlib46(source);
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
        using await (new C()) { }
        using await (var x = new C()) { return 1; }
    }
    public int DisposeAsync()
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (13,15): error CS0656: Missing compiler required member 'System.IAsyncDisposable.DisposeAsync'
                //         using await (new C()) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "await").WithArguments("System.IAsyncDisposable", "DisposeAsync").WithLocation(13, 15),
                // (14,15): error CS0656: Missing compiler required member 'System.IAsyncDisposable.DisposeAsync'
                //         using await (var x = new C()) { return 1; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "await").WithArguments("System.IAsyncDisposable", "DisposeAsync").WithLocation(14, 15)
                );
        }

        [Fact]
        public void TestMissingTaskType()
        {
            string lib_cs = @"
public class Base : System.IAsyncDisposable
{
    public System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
";

            string comp_cs = @"
public class C : Base
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        using await (var x = new C())
        {
            System.Console.Write(""body "");
            return 1;
        }
    }
}
";
            var libComp = CreateCompilationWithMscorlib46(lib_cs + s_interfaces);
            var comp = CreateCompilationWithMscorlib46(comp_cs, references: new[] { libComp.EmitToImageReference() }, options: TestOptions.DebugExe);
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_Task);
            comp.VerifyDiagnostics(
                // (6,15): error CS0518: Predefined type 'System.Threading.Tasks.Task' is not defined or imported
                //         using await (var x = new C())
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.Threading.Tasks.Task").WithLocation(6, 15)
                );
        }

        [Fact]
        public void TestWithDeclaration()
        {
            string source = @"
class C : System.IAsyncDisposable, System.IDisposable
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        using await (var x = new C())
        {
            System.Console.Write(""body "");
            return 1;
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return System.Threading.Tasks.Task.CompletedTask;
    }
    public void Dispose()
    {
        System.Console.Write(""IGNORED"");
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
        }

        [Fact]
        public void TestIAsyncDisposableInRegularUsing()
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
    public System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write(""IGNORED"");
        return System.Threading.Tasks.Task.CompletedTask;
    }
    public void Dispose()
    {
        System.Console.Write(""Dispose"");
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body Dispose");
        }

        [Fact]
        public void TestWithDynamicDeclaration()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task<int> Main()
    {
        using await (dynamic x = new C())
        {
            System.Console.Write(""body "");
        }
        System.Console.Write(""end "");
        return 1;
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write(""DisposeAsync "");
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe, references: new[] { SystemCoreRef, CSharpRef });
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync end");
        }

        [Fact]
        public void TestWithExpression()
        {
            string source = @"
class C : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        using await (new C())
        {
            System.Console.Write(""body "");
            return;
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
            verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      302 (0x12e)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C.<Main>d__0 V_3,
                System.Exception V_4,
                int V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_0011
    IL_000c:  br         IL_0095
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
    IL_0052:  brfalse.s  IL_00b9
    IL_0054:  ldarg.0
    IL_0055:  ldfld      ""C C.<Main>d__0.<>s__1""
    IL_005a:  callvirt   ""System.Threading.Tasks.Task System.IAsyncDisposable.DisposeAsync()""
    IL_005f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0064:  stloc.2
    IL_0065:  ldloca.s   V_2
    IL_0067:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_006c:  brtrue.s   IL_00b1
    IL_006e:  ldarg.0
    IL_006f:  ldc.i4.0
    IL_0070:  dup
    IL_0071:  stloc.0
    IL_0072:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_0077:  ldarg.0
    IL_0078:  ldloc.2
    IL_0079:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<Main>d__0.<>u__1""
    IL_007e:  ldarg.0
    IL_007f:  stloc.3
    IL_0080:  ldarg.0
    IL_0081:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_0086:  ldloca.s   V_2
    IL_0088:  ldloca.s   V_3
    IL_008a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<Main>d__0)""
    IL_008f:  nop
    IL_0090:  leave      IL_012d
    IL_0095:  ldarg.0
    IL_0096:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<Main>d__0.<>u__1""
    IL_009b:  stloc.2
    IL_009c:  ldarg.0
    IL_009d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<Main>d__0.<>u__1""
    IL_00a2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00a8:  ldarg.0
    IL_00a9:  ldc.i4.m1
    IL_00aa:  dup
    IL_00ab:  stloc.0
    IL_00ac:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_00b1:  ldloca.s   V_2
    IL_00b3:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00b8:  nop
    IL_00b9:  ldarg.0
    IL_00ba:  ldfld      ""object C.<Main>d__0.<>s__2""
    IL_00bf:  stloc.1
    IL_00c0:  ldloc.1
    IL_00c1:  brfalse.s  IL_00de
    IL_00c3:  ldloc.1
    IL_00c4:  isinst     ""System.Exception""
    IL_00c9:  stloc.s    V_4
    IL_00cb:  ldloc.s    V_4
    IL_00cd:  brtrue.s   IL_00d1
    IL_00cf:  ldloc.1
    IL_00d0:  throw
    IL_00d1:  ldloc.s    V_4
    IL_00d3:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00d8:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00dd:  nop
    IL_00de:  ldarg.0
    IL_00df:  ldfld      ""int C.<Main>d__0.<>s__3""
    IL_00e4:  stloc.s    V_5
    IL_00e6:  ldloc.s    V_5
    IL_00e8:  ldc.i4.1
    IL_00e9:  beq.s      IL_00ed
    IL_00eb:  br.s       IL_00ef
    IL_00ed:  leave.s    IL_0119
    IL_00ef:  ldarg.0
    IL_00f0:  ldnull
    IL_00f1:  stfld      ""object C.<Main>d__0.<>s__2""
    IL_00f6:  ldarg.0
    IL_00f7:  ldnull
    IL_00f8:  stfld      ""C C.<Main>d__0.<>s__1""
    IL_00fd:  leave.s    IL_0119
  }
  catch System.Exception
  {
    IL_00ff:  stloc.s    V_4
    IL_0101:  ldarg.0
    IL_0102:  ldc.i4.s   -2
    IL_0104:  stfld      ""int C.<Main>d__0.<>1__state""
    IL_0109:  ldarg.0
    IL_010a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
    IL_010f:  ldloc.s    V_4
    IL_0111:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0116:  nop
    IL_0117:  leave.s    IL_012d
  }
  IL_0119:  ldarg.0
  IL_011a:  ldc.i4.s   -2
  IL_011c:  stfld      ""int C.<Main>d__0.<>1__state""
  IL_0121:  ldarg.0
  IL_0122:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
  IL_0127:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_012c:  nop
  IL_012d:  ret
}");
        }

        [Fact]
        public void TestWithNullExpression()
        {
            string source = @"
class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        using await (null)
        {
            System.Console.Write(""body"");
            return;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body");
        }

        [Fact]
        public void TestWithMethodName()
        {
            string source = @"
class C
{
    public static async System.Threading.Tasks.Task Main()
    {
        using await (Main)
        {
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces);
            comp.VerifyDiagnostics(
                // (6,22): error CS9000: 'method group': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'
                //         using await (Main)
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
        using await (d)
        {
            System.Console.Write(""body "");
            return;
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe, references: new[] { SystemCoreRef, CSharpRef });
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
        }

        [Fact]
        public void TestWithStructExpression()
        {
            string source = @"
struct S : System.IAsyncDisposable
{
    public static async System.Threading.Tasks.Task Main()
    {
        using await (new S())
        {
            System.Console.Write(""body "");
            return;
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
            verifier.VerifyIL("S.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      294 (0x126)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                S.<Main>d__0 V_3,
                System.Exception V_4,
                int V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int S.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_0011
    IL_000c:  br         IL_0094
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
    IL_0059:  callvirt   ""System.Threading.Tasks.Task System.IAsyncDisposable.DisposeAsync()""
    IL_005e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0063:  stloc.2
    IL_0064:  ldloca.s   V_2
    IL_0066:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_006b:  brtrue.s   IL_00b0
    IL_006d:  ldarg.0
    IL_006e:  ldc.i4.0
    IL_006f:  dup
    IL_0070:  stloc.0
    IL_0071:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_0076:  ldarg.0
    IL_0077:  ldloc.2
    IL_0078:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter S.<Main>d__0.<>u__1""
    IL_007d:  ldarg.0
    IL_007e:  stloc.3
    IL_007f:  ldarg.0
    IL_0080:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
    IL_0085:  ldloca.s   V_2
    IL_0087:  ldloca.s   V_3
    IL_0089:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, S.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref S.<Main>d__0)""
    IL_008e:  nop
    IL_008f:  leave      IL_0125
    IL_0094:  ldarg.0
    IL_0095:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter S.<Main>d__0.<>u__1""
    IL_009a:  stloc.2
    IL_009b:  ldarg.0
    IL_009c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter S.<Main>d__0.<>u__1""
    IL_00a1:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.m1
    IL_00a9:  dup
    IL_00aa:  stloc.0
    IL_00ab:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_00b0:  ldloca.s   V_2
    IL_00b2:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00b7:  nop
    IL_00b8:  ldarg.0
    IL_00b9:  ldfld      ""object S.<Main>d__0.<>s__2""
    IL_00be:  stloc.1
    IL_00bf:  ldloc.1
    IL_00c0:  brfalse.s  IL_00dd
    IL_00c2:  ldloc.1
    IL_00c3:  isinst     ""System.Exception""
    IL_00c8:  stloc.s    V_4
    IL_00ca:  ldloc.s    V_4
    IL_00cc:  brtrue.s   IL_00d0
    IL_00ce:  ldloc.1
    IL_00cf:  throw
    IL_00d0:  ldloc.s    V_4
    IL_00d2:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00d7:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00dc:  nop
    IL_00dd:  ldarg.0
    IL_00de:  ldfld      ""int S.<Main>d__0.<>s__3""
    IL_00e3:  stloc.s    V_5
    IL_00e5:  ldloc.s    V_5
    IL_00e7:  ldc.i4.1
    IL_00e8:  beq.s      IL_00ec
    IL_00ea:  br.s       IL_00ee
    IL_00ec:  leave.s    IL_0111
    IL_00ee:  ldarg.0
    IL_00ef:  ldnull
    IL_00f0:  stfld      ""object S.<Main>d__0.<>s__2""
    IL_00f5:  leave.s    IL_0111
  }
  catch System.Exception
  {
    IL_00f7:  stloc.s    V_4
    IL_00f9:  ldarg.0
    IL_00fa:  ldc.i4.s   -2
    IL_00fc:  stfld      ""int S.<Main>d__0.<>1__state""
    IL_0101:  ldarg.0
    IL_0102:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
    IL_0107:  ldloc.s    V_4
    IL_0109:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_010e:  nop
    IL_010f:  leave.s    IL_0125
  }
  IL_0111:  ldarg.0
  IL_0112:  ldc.i4.s   -2
  IL_0114:  stfld      ""int S.<Main>d__0.<>1__state""
  IL_0119:  ldarg.0
  IL_011a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder S.<Main>d__0.<>t__builder""
  IL_011f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0124:  nop
  IL_0125:  ret
}");
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
        using await (s)
        {
            System.Console.Write(""body "");
            return;
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
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
        using await (s)
        {
            System.Console.Write(""body"");
            return;
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write(""NOT RELEVANT"");
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "body");
        }

        [Fact]
        [WorkItem(24267, "https://github.com/dotnet/roslyn/issues/24267")]
        public void AssignsInAsyncWithAsyncUsing()
        {
            var comp = CreateCompilationWithMscorlib46(@"
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
            using await (null) { }
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
        using await (S s1 = new S(1), s2 = new S(2))
        {
            System.Console.Write(""body "");
            return;
        }
    }
    public async System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write($""dispose{_i}_start "");
        await System.Threading.Tasks.Task.Delay(10);
        System.Console.Write($""dispose{_i}_end "");
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
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
            using await (S s1 = new S(1), s2 = new S(2))
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
    public System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write($""dispose{_i} "");
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
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
            using await (S s1 = new S(1), s2 = new S(2))
            {
                System.Console.Write(""SKIPPED"");
            }
        }
        catch (System.Exception)
        {
            System.Console.Write(""caught"");
        }
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        System.Console.Write($""dispose{_i} "");
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source + s_interfaces, options: TestOptions.DebugExe);
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
            var comp = CreateStandardCompilation(source);
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
    }
}
