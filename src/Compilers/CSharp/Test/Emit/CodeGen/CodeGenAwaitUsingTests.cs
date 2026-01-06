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
    [CompilerTrait(CompilerFeature.AsyncStreams, CompilerFeature.Async)]
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
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "System.IAsyncDisposable").WithArguments("C", "System.IAsyncDisposable.DisposeAsync()", "C.DisposeAsync()", "System.Threading.Tasks.ValueTask").WithLocation(3, 11)
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
            string expectedOutput = "C body DisposeAsync1 DisposeAsync2 end";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x29 }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x38 }
                    [<Main>b__1_0]: Return value missing on the stack. { Offset = 0x47 }
                    """
            });
            verifier.VerifyIL("C.<>c.<Main>b__1_0()", """
                {
                  // Code size       72 (0x48)
                  .maxstack  2
                  .locals init (C V_0, //y
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  .try
                  {
                    IL_0008:  ldstr      "body "
                    IL_000d:  call       "void System.Console.Write(string)"
                    IL_0012:  leave.s    IL_0017
                  }
                  catch object
                  {
                    IL_0014:  stloc.1
                    IL_0015:  leave.s    IL_0017
                  }
                  IL_0017:  ldloc.0
                  IL_0018:  brfalse.s  IL_0025
                  IL_001a:  ldloc.0
                  IL_001b:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                  IL_0020:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0025:  ldloc.1
                  IL_0026:  brfalse.s  IL_003d
                  IL_0028:  ldloc.1
                  IL_0029:  isinst     "System.Exception"
                  IL_002e:  dup
                  IL_002f:  brtrue.s   IL_0033
                  IL_0031:  ldloc.1
                  IL_0032:  throw
                  IL_0033:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0038:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_003d:  ldstr      "end"
                  IL_0042:  call       "void System.Console.Write(string)"
                  IL_0047:  ret
                }
                """);
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
            string expectedOutput = "try using dispose_start dispose_end end";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x62 }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x38 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       99 (0x63)
                  .maxstack  2
                  .locals init (int V_0,
                                C V_1, //x
                                object V_2)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldstr      "try "
                    IL_0007:  call       "void System.Console.Write(string)"
                    IL_000c:  newobj     "System.ArgumentNullException..ctor()"
                    IL_0011:  throw
                  }
                  catch System.ArgumentNullException
                  {
                    IL_0012:  pop
                    IL_0013:  ldc.i4.1
                    IL_0014:  stloc.0
                    IL_0015:  leave.s    IL_0017
                  }
                  IL_0017:  ldloc.0
                  IL_0018:  ldc.i4.1
                  IL_0019:  bne.un.s   IL_0062
                  IL_001b:  newobj     "C..ctor()"
                  IL_0020:  stloc.1
                  IL_0021:  ldnull
                  IL_0022:  stloc.2
                  .try
                  {
                    IL_0023:  ldstr      "using "
                    IL_0028:  call       "void System.Console.Write(string)"
                    IL_002d:  leave.s    IL_0032
                  }
                  catch object
                  {
                    IL_002f:  stloc.2
                    IL_0030:  leave.s    IL_0032
                  }
                  IL_0032:  ldloc.1
                  IL_0033:  brfalse.s  IL_0040
                  IL_0035:  ldloc.1
                  IL_0036:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                  IL_003b:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0040:  ldloc.2
                  IL_0041:  brfalse.s  IL_0058
                  IL_0043:  ldloc.2
                  IL_0044:  isinst     "System.Exception"
                  IL_0049:  dup
                  IL_004a:  brtrue.s   IL_004e
                  IL_004c:  ldloc.2
                  IL_004d:  throw
                  IL_004e:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0053:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0058:  ldstr      "end"
                  IL_005d:  call       "void System.Console.Write(string)"
                  IL_0062:  ret
                }
                """);
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
            comp.VerifyDiagnostics();
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
            comp.VerifyDiagnostics();
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
            string expectedOutput = "using dispose_start dispose_end return";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Unexpected type on the stack. { Offset = 0x67, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x38 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      104 (0x68)
                  .maxstack  2
                  .locals init (object V_0,
                                C V_1, //x
                                object V_2)
                  IL_0000:  ldnull
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  leave.s    IL_0007
                  }
                  catch object
                  {
                    IL_0004:  stloc.0
                    IL_0005:  leave.s    IL_0007
                  }
                  IL_0007:  newobj     "C..ctor()"
                  IL_000c:  stloc.1
                  IL_000d:  ldnull
                  IL_000e:  stloc.2
                  .try
                  {
                    IL_000f:  ldstr      "using "
                    IL_0014:  call       "void System.Console.Write(string)"
                    IL_0019:  leave.s    IL_001e
                  }
                  catch object
                  {
                    IL_001b:  stloc.2
                    IL_001c:  leave.s    IL_001e
                  }
                  IL_001e:  ldloc.1
                  IL_001f:  brfalse.s  IL_002c
                  IL_0021:  ldloc.1
                  IL_0022:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                  IL_0027:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_002c:  ldloc.2
                  IL_002d:  brfalse.s  IL_0044
                  IL_002f:  ldloc.2
                  IL_0030:  isinst     "System.Exception"
                  IL_0035:  dup
                  IL_0036:  brtrue.s   IL_003a
                  IL_0038:  ldloc.2
                  IL_0039:  throw
                  IL_003a:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_003f:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0044:  ldloc.0
                  IL_0045:  brfalse.s  IL_005c
                  IL_0047:  ldloc.0
                  IL_0048:  isinst     "System.Exception"
                  IL_004d:  dup
                  IL_004e:  brtrue.s   IL_0052
                  IL_0050:  ldloc.0
                  IL_0051:  throw
                  IL_0052:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0057:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_005c:  ldstr      "return"
                  IL_0061:  call       "void System.Console.Write(string)"
                  IL_0066:  ldc.i4.1
                  IL_0067:  ret
                }
                """);
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
            string expectedOutput = "using caught message";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x61 }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x3c }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       98 (0x62)
                  .maxstack  2
                  .locals init (C V_0, //x
                                object V_1,
                                System.Exception V_2) //e
                  .try
                  {
                    IL_0000:  newobj     "C..ctor()"
                    IL_0005:  stloc.0
                    IL_0006:  ldnull
                    IL_0007:  stloc.1
                    .try
                    {
                      IL_0008:  ldstr      "using "
                      IL_000d:  call       "void System.Console.Write(string)"
                      IL_0012:  leave.s    IL_0017
                    }
                    catch object
                    {
                      IL_0014:  stloc.1
                      IL_0015:  leave.s    IL_0017
                    }
                    IL_0017:  ldloc.0
                    IL_0018:  brfalse.s  IL_0025
                    IL_001a:  ldloc.0
                    IL_001b:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                    IL_0020:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_0025:  ldloc.1
                    IL_0026:  brfalse.s  IL_003d
                    IL_0028:  ldloc.1
                    IL_0029:  isinst     "System.Exception"
                    IL_002e:  dup
                    IL_002f:  brtrue.s   IL_0033
                    IL_0031:  ldloc.1
                    IL_0032:  throw
                    IL_0033:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_0038:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_003d:  leave.s    IL_0057
                  }
                  catch System.Exception
                  {
                    IL_003f:  stloc.2
                    IL_0040:  ldstr      "caught "
                    IL_0045:  ldloc.2
                    IL_0046:  callvirt   "string System.Exception.Message.get"
                    IL_004b:  call       "string string.Concat(string, string)"
                    IL_0050:  call       "void System.Console.Write(string)"
                    IL_0055:  leave.s    IL_0061
                  }
                  IL_0057:  ldstr      "SKIPPED"
                  IL_005c:  call       "void System.Console.Write(string)"
                  IL_0061:  ret
                }
                """);
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
            string expectedOutput = "before after";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Unexpected type on the stack. { Offset = 0x58, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       89 (0x59)
                  .maxstack  2
                  .locals init (object V_0,
                                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                                System.Runtime.CompilerServices.YieldAwaitable V_2)
                  IL_0000:  ldnull
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  leave.s    IL_0007
                  }
                  catch object
                  {
                    IL_0004:  stloc.0
                    IL_0005:  leave.s    IL_0007
                  }
                  IL_0007:  ldstr      "before "
                  IL_000c:  call       "void System.Console.Write(string)"
                  IL_0011:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                  IL_0016:  stloc.2
                  IL_0017:  ldloca.s   V_2
                  IL_0019:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                  IL_001e:  stloc.1
                  IL_001f:  ldloca.s   V_1
                  IL_0021:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                  IL_0026:  brtrue.s   IL_002e
                  IL_0028:  ldloc.1
                  IL_0029:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                  IL_002e:  ldloca.s   V_1
                  IL_0030:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                  IL_0035:  ldstr      "after"
                  IL_003a:  call       "void System.Console.Write(string)"
                  IL_003f:  ldloc.0
                  IL_0040:  brfalse.s  IL_0057
                  IL_0042:  ldloc.0
                  IL_0043:  isinst     "System.Exception"
                  IL_0048:  dup
                  IL_0049:  brtrue.s   IL_004d
                  IL_004b:  ldloc.0
                  IL_004c:  throw
                  IL_004d:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0052:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0057:  ldc.i4.1
                  IL_0058:  ret
                }
                """);
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
                // (6,22): error CS8410: 'C': type used in an asynchronous using statement must implement 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "new C()").WithArguments("C").WithLocation(6, 22),
                // (9,22): error CS8410: 'C': type used in an asynchronous using statement must implement 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
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
                // (6,22): error CS8410: 'C': type used in an asynchronous using statement must implement 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "new C()").WithArguments("C").WithLocation(6, 22),
                // (9,9): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<Task<int>>'.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await").WithArguments("System.Threading.Tasks.Task<int>").WithLocation(9, 9),
                // (9,22): error CS8410: 'C': type used in an asynchronous using statement must implement 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
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
                // (6,16): error CS1674: 'C': type used in a using statement must implement 'System.IDisposable'.
                //         using (new C())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "new C()").WithArguments("C").WithLocation(6, 16),
                // (9,16): error CS1674: 'C': type used in a using statement must implement 'System.IDisposable'.
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
            string expectedOutput = "body DisposeAsync";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Unexpected type on the stack. { Offset = 0x48, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       75 (0x4b)
                  .maxstack  2
                  .locals init (C V_0, //x
                                object V_1,
                                int V_2,
                                int V_3)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  IL_0008:  ldc.i4.0
                  IL_0009:  stloc.2
                  .try
                  {
                    IL_000a:  ldstr      "body "
                    IL_000f:  call       "void System.Console.Write(string)"
                    IL_0014:  ldc.i4.1
                    IL_0015:  stloc.3
                    IL_0016:  ldc.i4.1
                    IL_0017:  stloc.2
                    IL_0018:  leave.s    IL_001d
                  }
                  catch object
                  {
                    IL_001a:  stloc.1
                    IL_001b:  leave.s    IL_001d
                  }
                  IL_001d:  ldloc.0
                  IL_001e:  brfalse.s  IL_002b
                  IL_0020:  ldloc.0
                  IL_0021:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                  IL_0026:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_002b:  ldloc.1
                  IL_002c:  brfalse.s  IL_0043
                  IL_002e:  ldloc.1
                  IL_002f:  isinst     "System.Exception"
                  IL_0034:  dup
                  IL_0035:  brtrue.s   IL_0039
                  IL_0037:  ldloc.1
                  IL_0038:  throw
                  IL_0039:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_003e:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0043:  ldloc.2
                  IL_0044:  ldc.i4.1
                  IL_0045:  bne.un.s   IL_0049
                  IL_0047:  ldloc.3
                  IL_0048:  ret
                  IL_0049:  ldnull
                  IL_004a:  throw
                }
                """);
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
                // (6,16): error CS8418: 'C': type used in a using statement must implement 'System.IDisposable'. Did you mean 'await using' rather than 'using'?
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
                // (6,16): error CS8418: 'C': type used in a using statement must implement 'System.IDisposable'. Did you mean 'await using'?
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
            var expectedOutput = "body DisposeAsync end";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            // https://github.com/dotnet/roslyn/issues/79762: Test dynamic

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,30): error CS9328: Method 'C.Main()' uses a feature that is not supported by runtime async currently. Opt the method out of runtime async by attributing it with 'System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(false)'.
                //         await using (dynamic x = new C())
                Diagnostic(ErrorCode.ERR_UnsupportedFeatureInRuntimeAsync, "x = new C()").WithArguments("C.Main()").WithLocation(6, 30)
            );
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
            string expectedOutput = "body DisposeAsync end";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            // https://github.com/dotnet/roslyn/issues/79762: Test dynamic
            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,30): error CS9328: Method 'C.Main()' uses a feature that is not supported by runtime async currently. Opt the method out of runtime async by attributing it with 'System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(false)'.
                //         await using (dynamic x = new C())
                Diagnostic(ErrorCode.ERR_UnsupportedFeatureInRuntimeAsync, "x = new C()").WithArguments("C.Main()").WithLocation(6, 30)
            );
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
            string expectedOutput = "body DisposeAsync";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
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
    IL_0101:  ldnull
    IL_0102:  throw
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

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x45 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       72 (0x48)
                  .maxstack  2
                  .locals init (C V_0,
                                object V_1,
                                int V_2)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  IL_0008:  ldc.i4.0
                  IL_0009:  stloc.2
                  .try
                  {
                    IL_000a:  ldstr      "body "
                    IL_000f:  call       "void System.Console.Write(string)"
                    IL_0014:  ldc.i4.1
                    IL_0015:  stloc.2
                    IL_0016:  leave.s    IL_001b
                  }
                  catch object
                  {
                    IL_0018:  stloc.1
                    IL_0019:  leave.s    IL_001b
                  }
                  IL_001b:  ldloc.0
                  IL_001c:  brfalse.s  IL_0029
                  IL_001e:  ldloc.0
                  IL_001f:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0024:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0029:  ldloc.1
                  IL_002a:  brfalse.s  IL_0041
                  IL_002c:  ldloc.1
                  IL_002d:  isinst     "System.Exception"
                  IL_0032:  dup
                  IL_0033:  brtrue.s   IL_0037
                  IL_0035:  ldloc.1
                  IL_0036:  throw
                  IL_0037:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_003c:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0041:  ldloc.2
                  IL_0042:  ldc.i4.1
                  IL_0043:  bne.un.s   IL_0046
                  IL_0045:  ret
                  IL_0046:  ldnull
                  IL_0047:  throw
                }
                """);
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
            string expectedOutput = "body DisposeAsync";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
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
    IL_0101:  ldnull
    IL_0102:  throw
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

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x45 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       72 (0x48)
                  .maxstack  2
                  .locals init (C V_0,
                                object V_1,
                                int V_2)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  IL_0008:  ldc.i4.0
                  IL_0009:  stloc.2
                  .try
                  {
                    IL_000a:  ldstr      "body "
                    IL_000f:  call       "void System.Console.Write(string)"
                    IL_0014:  ldc.i4.1
                    IL_0015:  stloc.2
                    IL_0016:  leave.s    IL_001b
                  }
                  catch object
                  {
                    IL_0018:  stloc.1
                    IL_0019:  leave.s    IL_001b
                  }
                  IL_001b:  ldloc.0
                  IL_001c:  brfalse.s  IL_0029
                  IL_001e:  ldloc.0
                  IL_001f:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                  IL_0024:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0029:  ldloc.1
                  IL_002a:  brfalse.s  IL_0041
                  IL_002c:  ldloc.1
                  IL_002d:  isinst     "System.Exception"
                  IL_0032:  dup
                  IL_0033:  brtrue.s   IL_0037
                  IL_0035:  ldloc.1
                  IL_0036:  throw
                  IL_0037:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_003c:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0041:  ldloc.2
                  IL_0042:  ldc.i4.1
                  IL_0043:  bne.un.s   IL_0046
                  IL_0045:  ret
                  IL_0046:  ldnull
                  IL_0047:  throw
                }
                """);
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
            string expectedOutput = "body DisposeAsync";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);

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
    IL_0101:  ldnull
    IL_0102:  throw
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

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x45 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       72 (0x48)
                  .maxstack  2
                  .locals init (C V_0,
                                object V_1,
                                int V_2)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  IL_0008:  ldc.i4.0
                  IL_0009:  stloc.2
                  .try
                  {
                    IL_000a:  ldstr      "body "
                    IL_000f:  call       "void System.Console.Write(string)"
                    IL_0014:  ldc.i4.1
                    IL_0015:  stloc.2
                    IL_0016:  leave.s    IL_001b
                  }
                  catch object
                  {
                    IL_0018:  stloc.1
                    IL_0019:  leave.s    IL_001b
                  }
                  IL_001b:  ldloc.0
                  IL_001c:  brfalse.s  IL_0029
                  IL_001e:  ldloc.0
                  IL_001f:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                  IL_0024:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0029:  ldloc.1
                  IL_002a:  brfalse.s  IL_0041
                  IL_002c:  ldloc.1
                  IL_002d:  isinst     "System.Exception"
                  IL_0032:  dup
                  IL_0033:  brtrue.s   IL_0037
                  IL_0035:  ldloc.1
                  IL_0036:  throw
                  IL_0037:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_003c:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0041:  ldloc.2
                  IL_0042:  ldc.i4.1
                  IL_0043:  bne.un.s   IL_0046
                  IL_0045:  ret
                  IL_0046:  ldnull
                  IL_0047:  throw
                }
                """);
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
            string expectedOutput = "body";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xa }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       11 (0xb)
                  .maxstack  1
                  IL_0000:  ldstr      "body"
                  IL_0005:  call       "void System.Console.Write(string)"
                  IL_000a:  ret
                }
                """);
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
            string expectedOutput = "body DisposeAsync";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x86 }
                    """
            });

            verifier.VerifyIL("C.Main()", """
                {
                  // Code size      137 (0x89)
                  .maxstack  3
                  .locals init (object V_0, //d
                                System.IAsyncDisposable V_1,
                                object V_2,
                                int V_3)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.IAsyncDisposable>> C.<>o__0.<>p__0"
                  IL_000b:  brtrue.s   IL_0031
                  IL_000d:  ldc.i4.0
                  IL_000e:  ldtoken    "System.IAsyncDisposable"
                  IL_0013:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                  IL_0018:  ldtoken    "C"
                  IL_001d:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                  IL_0022:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)"
                  IL_0027:  call       "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.IAsyncDisposable>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.IAsyncDisposable>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
                  IL_002c:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.IAsyncDisposable>> C.<>o__0.<>p__0"
                  IL_0031:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.IAsyncDisposable>> C.<>o__0.<>p__0"
                  IL_0036:  ldfld      "System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.IAsyncDisposable> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.IAsyncDisposable>>.Target"
                  IL_003b:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.IAsyncDisposable>> C.<>o__0.<>p__0"
                  IL_0040:  ldloc.0
                  IL_0041:  callvirt   "System.IAsyncDisposable System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.IAsyncDisposable>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)"
                  IL_0046:  stloc.1
                  IL_0047:  ldnull
                  IL_0048:  stloc.2
                  IL_0049:  ldc.i4.0
                  IL_004a:  stloc.3
                  .try
                  {
                    IL_004b:  ldstr      "body "
                    IL_0050:  call       "void System.Console.Write(string)"
                    IL_0055:  ldc.i4.1
                    IL_0056:  stloc.3
                    IL_0057:  leave.s    IL_005c
                  }
                  catch object
                  {
                    IL_0059:  stloc.2
                    IL_005a:  leave.s    IL_005c
                  }
                  IL_005c:  ldloc.1
                  IL_005d:  brfalse.s  IL_006a
                  IL_005f:  ldloc.1
                  IL_0060:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0065:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_006a:  ldloc.2
                  IL_006b:  brfalse.s  IL_0082
                  IL_006d:  ldloc.2
                  IL_006e:  isinst     "System.Exception"
                  IL_0073:  dup
                  IL_0074:  brtrue.s   IL_0078
                  IL_0076:  ldloc.2
                  IL_0077:  throw
                  IL_0078:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_007d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0082:  ldloc.3
                  IL_0083:  ldc.i4.1
                  IL_0084:  bne.un.s   IL_0087
                  IL_0086:  ret
                  IL_0087:  ldnull
                  IL_0088:  throw
                }
                """);
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
            string expectedOutput = "body DisposeAsync";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
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
    IL_00f9:  ldnull
    IL_00fa:  throw
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

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x4b }
                    """
            });
            verifier.VerifyIL("S.Main()", """
                {
                  // Code size       78 (0x4e)
                  .maxstack  2
                  .locals init (S V_0,
                                object V_1,
                                int V_2)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "S"
                  IL_0008:  ldnull
                  IL_0009:  stloc.1
                  IL_000a:  ldc.i4.0
                  IL_000b:  stloc.2
                  .try
                  {
                    IL_000c:  ldstr      "body "
                    IL_0011:  call       "void System.Console.Write(string)"
                    IL_0016:  ldc.i4.1
                    IL_0017:  stloc.2
                    IL_0018:  leave.s    IL_001d
                  }
                  catch object
                  {
                    IL_001a:  stloc.1
                    IL_001b:  leave.s    IL_001d
                  }
                  IL_001d:  ldloca.s   V_0
                  IL_001f:  constrained. "S"
                  IL_0025:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_002a:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_002f:  ldloc.1
                  IL_0030:  brfalse.s  IL_0047
                  IL_0032:  ldloc.1
                  IL_0033:  isinst     "System.Exception"
                  IL_0038:  dup
                  IL_0039:  brtrue.s   IL_003d
                  IL_003b:  ldloc.1
                  IL_003c:  throw
                  IL_003d:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0042:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0047:  ldloc.2
                  IL_0048:  ldc.i4.1
                  IL_0049:  bne.un.s   IL_004c
                  IL_004b:  ret
                  IL_004c:  ldnull
                  IL_004d:  throw
                }
                """);
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
            string expectedOutput = "body DisposeAsync";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
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
    IL_00f3:  ldnull
    IL_00f4:  throw
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

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x45 }
                    """
            });
            verifier.VerifyIL("S.Main()", """
                {
                  // Code size       72 (0x48)
                  .maxstack  2
                  .locals init (S V_0,
                                object V_1,
                                int V_2)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "S"
                  IL_0008:  ldnull
                  IL_0009:  stloc.1
                  IL_000a:  ldc.i4.0
                  IL_000b:  stloc.2
                  .try
                  {
                    IL_000c:  ldstr      "body "
                    IL_0011:  call       "void System.Console.Write(string)"
                    IL_0016:  ldc.i4.1
                    IL_0017:  stloc.2
                    IL_0018:  leave.s    IL_001d
                  }
                  catch object
                  {
                    IL_001a:  stloc.1
                    IL_001b:  leave.s    IL_001d
                  }
                  IL_001d:  ldloca.s   V_0
                  IL_001f:  call       "System.Threading.Tasks.ValueTask S.DisposeAsync()"
                  IL_0024:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0029:  ldloc.1
                  IL_002a:  brfalse.s  IL_0041
                  IL_002c:  ldloc.1
                  IL_002d:  isinst     "System.Exception"
                  IL_0032:  dup
                  IL_0033:  brtrue.s   IL_0037
                  IL_0035:  ldloc.1
                  IL_0036:  throw
                  IL_0037:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_003c:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0041:  ldloc.2
                  IL_0042:  ldc.i4.1
                  IL_0043:  bne.un.s   IL_0046
                  IL_0045:  ret
                  IL_0046:  ldnull
                  IL_0047:  throw
                }
                """);
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

            // Runtime async verification
            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("True"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x4f }
                    """
            });
            verifier.VerifyIL("S.Main()", """
                {
                  // Code size       80 (0x50)
                  .maxstack  2
                  .locals init (S V_0, //s
                                S V_1,
                                object V_2)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  newobj     "C..ctor()"
                  IL_0007:  call       "S..ctor(C)"
                  IL_000c:  ldloc.0
                  IL_000d:  stloc.1
                  IL_000e:  ldnull
                  IL_000f:  stloc.2
                  .try
                  {
                    IL_0010:  leave.s    IL_0015
                  }
                  catch object
                  {
                    IL_0012:  stloc.2
                    IL_0013:  leave.s    IL_0015
                  }
                  IL_0015:  ldloca.s   V_1
                  IL_0017:  constrained. "S"
                  IL_001d:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0022:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0027:  ldloc.2
                  IL_0028:  brfalse.s  IL_003f
                  IL_002a:  ldloc.2
                  IL_002b:  isinst     "System.Exception"
                  IL_0030:  dup
                  IL_0031:  brtrue.s   IL_0035
                  IL_0033:  ldloc.2
                  IL_0034:  throw
                  IL_0035:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_003a:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_003f:  ldloc.0
                  IL_0040:  ldfld      "C S._c"
                  IL_0045:  ldfld      "bool C._disposed"
                  IL_004a:  call       "void System.Console.WriteLine(bool)"
                  IL_004f:  ret
                }
                """);
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

            // Runtime async verification
            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("body DisposeAsync"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x63 }
                    """
            });
            verifier.VerifyIL("S.Main", """
                {
                  // Code size      102 (0x66)
                  .maxstack  2
                  .locals init (S V_0,
                                S? V_1,
                                object V_2,
                                int V_3)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "S"
                  IL_0008:  ldloc.0
                  IL_0009:  newobj     "S?..ctor(S)"
                  IL_000e:  stloc.1
                  IL_000f:  ldnull
                  IL_0010:  stloc.2
                  IL_0011:  ldc.i4.0
                  IL_0012:  stloc.3
                  .try
                  {
                    IL_0013:  ldstr      "body "
                    IL_0018:  call       "void System.Console.Write(string)"
                    IL_001d:  ldc.i4.1
                    IL_001e:  stloc.3
                    IL_001f:  leave.s    IL_0024
                  }
                  catch object
                  {
                    IL_0021:  stloc.2
                    IL_0022:  leave.s    IL_0024
                  }
                  IL_0024:  ldloca.s   V_1
                  IL_0026:  call       "readonly bool S?.HasValue.get"
                  IL_002b:  brfalse.s  IL_0047
                  IL_002d:  ldloca.s   V_1
                  IL_002f:  call       "readonly S S?.GetValueOrDefault()"
                  IL_0034:  stloc.0
                  IL_0035:  ldloca.s   V_0
                  IL_0037:  constrained. "S"
                  IL_003d:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_0042:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0047:  ldloc.2
                  IL_0048:  brfalse.s  IL_005f
                  IL_004a:  ldloc.2
                  IL_004b:  isinst     "System.Exception"
                  IL_0050:  dup
                  IL_0051:  brtrue.s   IL_0055
                  IL_0053:  ldloc.2
                  IL_0054:  throw
                  IL_0055:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_005a:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_005f:  ldloc.3
                  IL_0060:  ldc.i4.1
                  IL_0061:  bne.un.s   IL_0064
                  IL_0063:  ret
                  IL_0064:  ldnull
                  IL_0065:  throw
                }
                """);
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

            // Runtime async verification
            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("body"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x5e }
                    """
            });
            verifier.VerifyIL("S.Main", """
                {
                  // Code size       97 (0x61)
                  .maxstack  2
                  .locals init (S? V_0,
                                object V_1,
                                int V_2,
                                S V_3)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "S?"
                  IL_0008:  ldloc.0
                  IL_0009:  stloc.0
                  IL_000a:  ldnull
                  IL_000b:  stloc.1
                  IL_000c:  ldc.i4.0
                  IL_000d:  stloc.2
                  .try
                  {
                    IL_000e:  ldstr      "body"
                    IL_0013:  call       "void System.Console.Write(string)"
                    IL_0018:  ldc.i4.1
                    IL_0019:  stloc.2
                    IL_001a:  leave.s    IL_001f
                  }
                  catch object
                  {
                    IL_001c:  stloc.1
                    IL_001d:  leave.s    IL_001f
                  }
                  IL_001f:  ldloca.s   V_0
                  IL_0021:  call       "readonly bool S?.HasValue.get"
                  IL_0026:  brfalse.s  IL_0042
                  IL_0028:  ldloca.s   V_0
                  IL_002a:  call       "readonly S S?.GetValueOrDefault()"
                  IL_002f:  stloc.3
                  IL_0030:  ldloca.s   V_3
                  IL_0032:  constrained. "S"
                  IL_0038:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
                  IL_003d:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0042:  ldloc.1
                  IL_0043:  brfalse.s  IL_005a
                  IL_0045:  ldloc.1
                  IL_0046:  isinst     "System.Exception"
                  IL_004b:  dup
                  IL_004c:  brtrue.s   IL_0050
                  IL_004e:  ldloc.1
                  IL_004f:  throw
                  IL_0050:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0055:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_005a:  ldloc.2
                  IL_005b:  ldc.i4.1
                  IL_005c:  bne.un.s   IL_005f
                  IL_005e:  ret
                  IL_005f:  ldnull
                  IL_0060:  throw
                }
                """);
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
            string expectedOutput = "ctor1 ctor2 body dispose2_start dispose2_end dispose1_start dispose1_end";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x8c }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x9a }
                    """
            });
            verifier.VerifyIL("S.Main()", """
                {
                  // Code size      143 (0x8f)
                  .maxstack  2
                  .locals init (S V_0, //s1
                                S V_1, //s2
                                object V_2,
                                int V_3,
                                object V_4,
                                int V_5)
                  IL_0000:  ldc.i4.1
                  IL_0001:  newobj     "S..ctor(int)"
                  IL_0006:  stloc.0
                  IL_0007:  ldnull
                  IL_0008:  stloc.2
                  IL_0009:  ldc.i4.0
                  IL_000a:  stloc.3
                  .try
                  {
                    IL_000b:  ldc.i4.2
                    IL_000c:  newobj     "S..ctor(int)"
                    IL_0011:  stloc.1
                    IL_0012:  ldnull
                    IL_0013:  stloc.s    V_4
                    IL_0015:  ldc.i4.0
                    IL_0016:  stloc.s    V_5
                    .try
                    {
                      IL_0018:  ldstr      "body "
                      IL_001d:  call       "void System.Console.Write(string)"
                      IL_0022:  ldc.i4.1
                      IL_0023:  stloc.s    V_5
                      IL_0025:  leave.s    IL_002b
                    }
                    catch object
                    {
                      IL_0027:  stloc.s    V_4
                      IL_0029:  leave.s    IL_002b
                    }
                    IL_002b:  ldloc.1
                    IL_002c:  brfalse.s  IL_0039
                    IL_002e:  ldloc.1
                    IL_002f:  callvirt   "System.Threading.Tasks.ValueTask S.DisposeAsync()"
                    IL_0034:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_0039:  ldloc.s    V_4
                    IL_003b:  brfalse.s  IL_0054
                    IL_003d:  ldloc.s    V_4
                    IL_003f:  isinst     "System.Exception"
                    IL_0044:  dup
                    IL_0045:  brtrue.s   IL_004a
                    IL_0047:  ldloc.s    V_4
                    IL_0049:  throw
                    IL_004a:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_004f:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0054:  ldloc.s    V_5
                    IL_0056:  ldc.i4.1
                    IL_0057:  beq.s      IL_005b
                    IL_0059:  leave.s    IL_0062
                    IL_005b:  ldc.i4.1
                    IL_005c:  stloc.3
                    IL_005d:  leave.s    IL_0062
                  }
                  catch object
                  {
                    IL_005f:  stloc.2
                    IL_0060:  leave.s    IL_0062
                  }
                  IL_0062:  ldloc.0
                  IL_0063:  brfalse.s  IL_0070
                  IL_0065:  ldloc.0
                  IL_0066:  callvirt   "System.Threading.Tasks.ValueTask S.DisposeAsync()"
                  IL_006b:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0070:  ldloc.2
                  IL_0071:  brfalse.s  IL_0088
                  IL_0073:  ldloc.2
                  IL_0074:  isinst     "System.Exception"
                  IL_0079:  dup
                  IL_007a:  brtrue.s   IL_007e
                  IL_007c:  ldloc.2
                  IL_007d:  throw
                  IL_007e:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0083:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0088:  ldloc.3
                  IL_0089:  ldc.i4.1
                  IL_008a:  bne.un.s   IL_008d
                  IL_008c:  ret
                  IL_008d:  ldnull
                  IL_008e:  throw
                }
                """);
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
            string expectedOutput = "ctor1 ctor2 body dispose2 dispose1 caught";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x85 }
                    """
            });
            verifier.VerifyIL("S.Main()", """
                {
                  // Code size      134 (0x86)
                  .maxstack  2
                  .locals init (S V_0, //s1
                                S V_1, //s2
                                object V_2,
                                object V_3)
                  .try
                  {
                    IL_0000:  ldc.i4.1
                    IL_0001:  newobj     "S..ctor(int)"
                    IL_0006:  stloc.0
                    IL_0007:  ldnull
                    IL_0008:  stloc.2
                    .try
                    {
                      IL_0009:  ldc.i4.2
                      IL_000a:  newobj     "S..ctor(int)"
                      IL_000f:  stloc.1
                      IL_0010:  ldnull
                      IL_0011:  stloc.3
                      .try
                      {
                        IL_0012:  ldstr      "body "
                        IL_0017:  call       "void System.Console.Write(string)"
                        IL_001c:  newobj     "System.Exception..ctor()"
                        IL_0021:  throw
                      }
                      catch object
                      {
                        IL_0022:  stloc.3
                        IL_0023:  leave.s    IL_0025
                      }
                      IL_0025:  ldloc.1
                      IL_0026:  brfalse.s  IL_0033
                      IL_0028:  ldloc.1
                      IL_0029:  callvirt   "System.Threading.Tasks.ValueTask S.DisposeAsync()"
                      IL_002e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                      IL_0033:  ldloc.3
                      IL_0034:  brfalse.s  IL_004b
                      IL_0036:  ldloc.3
                      IL_0037:  isinst     "System.Exception"
                      IL_003c:  dup
                      IL_003d:  brtrue.s   IL_0041
                      IL_003f:  ldloc.3
                      IL_0040:  throw
                      IL_0041:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                      IL_0046:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                      IL_004b:  leave.s    IL_0050
                    }
                    catch object
                    {
                      IL_004d:  stloc.2
                      IL_004e:  leave.s    IL_0050
                    }
                    IL_0050:  ldloc.0
                    IL_0051:  brfalse.s  IL_005e
                    IL_0053:  ldloc.0
                    IL_0054:  callvirt   "System.Threading.Tasks.ValueTask S.DisposeAsync()"
                    IL_0059:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_005e:  ldloc.2
                    IL_005f:  brfalse.s  IL_0076
                    IL_0061:  ldloc.2
                    IL_0062:  isinst     "System.Exception"
                    IL_0067:  dup
                    IL_0068:  brtrue.s   IL_006c
                    IL_006a:  ldloc.2
                    IL_006b:  throw
                    IL_006c:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_0071:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0076:  leave.s    IL_0085
                  }
                  catch System.Exception
                  {
                    IL_0078:  pop
                    IL_0079:  ldstr      "caught"
                    IL_007e:  call       "void System.Console.Write(string)"
                    IL_0083:  leave.s    IL_0085
                  }
                  IL_0085:  ret
                }
                """);
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
            string expectedOutput = "ctor1 ctor2 dispose1 caught";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x81 }
                    """
            });
            verifier.VerifyIL("S.Main()", """
                {
                  // Code size      130 (0x82)
                  .maxstack  2
                  .locals init (S V_0, //s1
                                S V_1, //s2
                                object V_2,
                                object V_3)
                  .try
                  {
                    IL_0000:  ldc.i4.1
                    IL_0001:  newobj     "S..ctor(int)"
                    IL_0006:  stloc.0
                    IL_0007:  ldnull
                    IL_0008:  stloc.2
                    .try
                    {
                      IL_0009:  ldc.i4.2
                      IL_000a:  newobj     "S..ctor(int)"
                      IL_000f:  stloc.1
                      IL_0010:  ldnull
                      IL_0011:  stloc.3
                      .try
                      {
                        IL_0012:  ldstr      "SKIPPED"
                        IL_0017:  call       "void System.Console.Write(string)"
                        IL_001c:  leave.s    IL_0021
                      }
                      catch object
                      {
                        IL_001e:  stloc.3
                        IL_001f:  leave.s    IL_0021
                      }
                      IL_0021:  ldloc.1
                      IL_0022:  brfalse.s  IL_002f
                      IL_0024:  ldloc.1
                      IL_0025:  callvirt   "System.Threading.Tasks.ValueTask S.DisposeAsync()"
                      IL_002a:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                      IL_002f:  ldloc.3
                      IL_0030:  brfalse.s  IL_0047
                      IL_0032:  ldloc.3
                      IL_0033:  isinst     "System.Exception"
                      IL_0038:  dup
                      IL_0039:  brtrue.s   IL_003d
                      IL_003b:  ldloc.3
                      IL_003c:  throw
                      IL_003d:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                      IL_0042:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                      IL_0047:  leave.s    IL_004c
                    }
                    catch object
                    {
                      IL_0049:  stloc.2
                      IL_004a:  leave.s    IL_004c
                    }
                    IL_004c:  ldloc.0
                    IL_004d:  brfalse.s  IL_005a
                    IL_004f:  ldloc.0
                    IL_0050:  callvirt   "System.Threading.Tasks.ValueTask S.DisposeAsync()"
                    IL_0055:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                    IL_005a:  ldloc.2
                    IL_005b:  brfalse.s  IL_0072
                    IL_005d:  ldloc.2
                    IL_005e:  isinst     "System.Exception"
                    IL_0063:  dup
                    IL_0064:  brtrue.s   IL_0068
                    IL_0066:  ldloc.2
                    IL_0067:  throw
                    IL_0068:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                    IL_006d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                    IL_0072:  leave.s    IL_0081
                  }
                  catch System.Exception
                  {
                    IL_0074:  pop
                    IL_0075:  ldstr      "caught"
                    IL_007a:  call       "void System.Console.Write(string)"
                    IL_007f:  leave.s    IL_0081
                  }
                  IL_0081:  ret
                }
                """);
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
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            var getAwaiter1 = (MethodSymbol)comp.GetMember("C.GetAwaiter");
            var isCompleted1 = (PropertySymbol)comp.GetMember("C.IsCompleted");
            var getResult1 = (MethodSymbol)comp.GetMember("C.GetResult");
            var awaitRuntimeCall1 = (MethodSymbol)comp.GetMembers("System.Runtime.CompilerServices.AsyncHelpers.Await")[0];
            var first = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), getResult1.GetPublicSymbol(), awaitRuntimeCall1.GetPublicSymbol(), false);

            var nulls1 = new AwaitExpressionInfo(null, isCompleted1.GetPublicSymbol(), getResult1.GetPublicSymbol(), awaitRuntimeCall1.GetPublicSymbol(), false);
            var nulls2 = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), null, getResult1.GetPublicSymbol(), awaitRuntimeCall1.GetPublicSymbol(), false);
            var nulls3 = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), null, awaitRuntimeCall1.GetPublicSymbol(), false);
            var nulls4 = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), getResult1.GetPublicSymbol(), null, false);
            var nulls5 = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), getResult1.GetPublicSymbol(), awaitRuntimeCall1.GetPublicSymbol(), true);

            Assert.False(first.Equals(nulls1));
            Assert.False(first.Equals(nulls2));
            Assert.False(first.Equals(nulls3));
            Assert.False(first.Equals(nulls4));
            Assert.False(first.Equals(nulls5));

            Assert.False(nulls1.Equals(first));
            Assert.False(nulls2.Equals(first));
            Assert.False(nulls3.Equals(first));
            Assert.False(nulls4.Equals(first));
            Assert.False(nulls5.Equals(first));

            _ = nulls1.GetHashCode();
            _ = nulls2.GetHashCode();
            _ = nulls3.GetHashCode();
            _ = nulls4.GetHashCode();
            _ = nulls5.GetHashCode();

            object nullObj = null;
            Assert.False(first.Equals(nullObj));

            var getAwaiter2 = (MethodSymbol)comp.GetMember("D.GetAwaiter");
            var isCompleted2 = (PropertySymbol)comp.GetMember("D.IsCompleted");
            var getResult2 = (MethodSymbol)comp.GetMember("D.GetResult");
            var awaitRuntimeCall2 = (MethodSymbol)comp.GetMembers("System.Runtime.CompilerServices.AsyncHelpers.Await")[1];
            var second1 = new AwaitExpressionInfo(getAwaiter2.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), getResult1.GetPublicSymbol(), awaitRuntimeCall2.GetPublicSymbol(), false);
            var second2 = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted2.GetPublicSymbol(), getResult1.GetPublicSymbol(), awaitRuntimeCall2.GetPublicSymbol(), false);
            var second3 = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), getResult2.GetPublicSymbol(), awaitRuntimeCall2.GetPublicSymbol(), false);
            var second4 = new AwaitExpressionInfo(getAwaiter2.GetPublicSymbol(), isCompleted2.GetPublicSymbol(), getResult2.GetPublicSymbol(), awaitRuntimeCall2.GetPublicSymbol(), false);
            var second5 = new AwaitExpressionInfo(getAwaiter2.GetPublicSymbol(), isCompleted2.GetPublicSymbol(), getResult2.GetPublicSymbol(), awaitRuntimeCall2.GetPublicSymbol(), false);
            Assert.False(first.Equals(second1));
            Assert.False(first.Equals(second2));
            Assert.False(first.Equals(second3));
            Assert.False(first.Equals(second4));
            Assert.False(first.Equals(second5));

            Assert.False(second1.Equals(first));
            Assert.False(second2.Equals(first));
            Assert.False(second3.Equals(first));
            Assert.False(second4.Equals(first));
            Assert.False(second5.Equals(first));

            Assert.True(first.Equals(first));
            Assert.True(first.Equals((object)first));

            var another = new AwaitExpressionInfo(getAwaiter1.GetPublicSymbol(), isCompleted1.GetPublicSymbol(), getResult1.GetPublicSymbol(), awaitRuntimeCall1.GetPublicSymbol(), false);
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
            var expectedOutput = "dispose";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            // Runtime async verification
            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Unexpected type on the stack. { Offset = 0x35, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                    """
            });
            verifier.VerifyIL("C.Main", """
                {
                  // Code size       54 (0x36)
                  .maxstack  2
                  .locals init (C V_0, //x
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  .try
                  {
                    IL_0008:  leave.s    IL_000d
                  }
                  catch object
                  {
                    IL_000a:  stloc.1
                    IL_000b:  leave.s    IL_000d
                  }
                  IL_000d:  ldloc.0
                  IL_000e:  brfalse.s  IL_001c
                  IL_0010:  ldloc.0
                  IL_0011:  ldc.i4.0
                  IL_0012:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync(int)"
                  IL_0017:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_001c:  ldloc.1
                  IL_001d:  brfalse.s  IL_0034
                  IL_001f:  ldloc.1
                  IL_0020:  isinst     "System.Exception"
                  IL_0025:  dup
                  IL_0026:  brtrue.s   IL_002a
                  IL_0028:  ldloc.1
                  IL_0029:  throw
                  IL_002a:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_002f:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0034:  ldc.i4.1
                  IL_0035:  ret
                }
                """);
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
            var expectedOutput = "using dispose_start dispose_end return";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            // Runtime async verification
            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Unexpected type on the stack. { Offset = 0x48, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x38 }
                    """
            });
            verifier.VerifyIL("C.Main", """
                {
                  // Code size       73 (0x49)
                  .maxstack  2
                  .locals init (C V_0,
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  .try
                  {
                    IL_0008:  ldstr      "using "
                    IL_000d:  call       "void System.Console.Write(string)"
                    IL_0012:  leave.s    IL_0017
                  }
                  catch object
                  {
                    IL_0014:  stloc.1
                    IL_0015:  leave.s    IL_0017
                  }
                  IL_0017:  ldloc.0
                  IL_0018:  brfalse.s  IL_0025
                  IL_001a:  ldloc.0
                  IL_001b:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                  IL_0020:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0025:  ldloc.1
                  IL_0026:  brfalse.s  IL_003d
                  IL_0028:  ldloc.1
                  IL_0029:  isinst     "System.Exception"
                  IL_002e:  dup
                  IL_002f:  brtrue.s   IL_0033
                  IL_0031:  ldloc.1
                  IL_0032:  throw
                  IL_0033:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0038:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_003d:  ldstr      "return"
                  IL_0042:  call       "void System.Console.Write(string)"
                  IL_0047:  ldc.i4.1
                  IL_0048:  ret
                }
                """);
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

            // Runtime async verification
            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("using dispose_start dispose_end return"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Unexpected type on the stack. { Offset = 0x48, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x38 }
                    """
            });
            verifier.VerifyIL("C.Main", """
                {
                  // Code size       73 (0x49)
                  .maxstack  2
                  .locals init (C V_0, //x
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  .try
                  {
                    IL_0008:  ldstr      "using "
                    IL_000d:  call       "void System.Console.Write(string)"
                    IL_0012:  leave.s    IL_0017
                  }
                  catch object
                  {
                    IL_0014:  stloc.1
                    IL_0015:  leave.s    IL_0017
                  }
                  IL_0017:  ldloc.0
                  IL_0018:  brfalse.s  IL_0025
                  IL_001a:  ldloc.0
                  IL_001b:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                  IL_0020:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0025:  ldloc.1
                  IL_0026:  brfalse.s  IL_003d
                  IL_0028:  ldloc.1
                  IL_0029:  isinst     "System.Exception"
                  IL_002e:  dup
                  IL_002f:  brtrue.s   IL_0033
                  IL_0031:  ldloc.1
                  IL_0032:  throw
                  IL_0033:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0038:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_003d:  ldstr      "return"
                  IL_0042:  call       "void System.Console.Write(string)"
                  IL_0047:  ldc.i4.1
                  IL_0048:  ret
                }
                """);
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
            string expectedOutput = "using dispose_start dispose_end return";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Unexpected type on the stack. { Offset = 0x48, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x38 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       73 (0x49)
                  .maxstack  2
                  .locals init (C V_0, //x
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  .try
                  {
                    IL_0008:  ldstr      "using "
                    IL_000d:  call       "void System.Console.Write(string)"
                    IL_0012:  leave.s    IL_0017
                  }
                  catch object
                  {
                    IL_0014:  stloc.1
                    IL_0015:  leave.s    IL_0017
                  }
                  IL_0017:  ldloc.0
                  IL_0018:  brfalse.s  IL_0025
                  IL_001a:  ldloc.0
                  IL_001b:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                  IL_0020:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0025:  ldloc.1
                  IL_0026:  brfalse.s  IL_003d
                  IL_0028:  ldloc.1
                  IL_0029:  isinst     "System.Exception"
                  IL_002e:  dup
                  IL_002f:  brtrue.s   IL_0033
                  IL_0031:  ldloc.1
                  IL_0032:  throw
                  IL_0033:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0038:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_003d:  ldstr      "return"
                  IL_0042:  call       "void System.Console.Write(string)"
                  IL_0047:  ldc.i4.1
                  IL_0048:  ret
                }
                """);
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
            string expectedOutput = "using dispose_start dispose_end return";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Unexpected type on the stack. { Offset = 0x49, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x38 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       74 (0x4a)
                  .maxstack  2
                  .locals init (C V_0, //x
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  .try
                  {
                    IL_0008:  ldstr      "using "
                    IL_000d:  call       "void System.Console.Write(string)"
                    IL_0012:  leave.s    IL_0017
                  }
                  catch object
                  {
                    IL_0014:  stloc.1
                    IL_0015:  leave.s    IL_0017
                  }
                  IL_0017:  ldloc.0
                  IL_0018:  brfalse.s  IL_0026
                  IL_001a:  ldloc.0
                  IL_001b:  ldc.i4.0
                  IL_001c:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync(int)"
                  IL_0021:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0026:  ldloc.1
                  IL_0027:  brfalse.s  IL_003e
                  IL_0029:  ldloc.1
                  IL_002a:  isinst     "System.Exception"
                  IL_002f:  dup
                  IL_0030:  brtrue.s   IL_0034
                  IL_0032:  ldloc.1
                  IL_0033:  throw
                  IL_0034:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0039:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_003e:  ldstr      "return"
                  IL_0043:  call       "void System.Console.Write(string)"
                  IL_0048:  ldc.i4.1
                  IL_0049:  ret
                }
                """);
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
            string expectedOutput = "using dispose_start dispose_end(0) return";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Unexpected type on the stack. { Offset = 0x4d, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x66 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       78 (0x4e)
                  .maxstack  2
                  .locals init (C V_0, //x
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  .try
                  {
                    IL_0008:  ldstr      "using "
                    IL_000d:  call       "void System.Console.Write(string)"
                    IL_0012:  leave.s    IL_0017
                  }
                  catch object
                  {
                    IL_0014:  stloc.1
                    IL_0015:  leave.s    IL_0017
                  }
                  IL_0017:  ldloc.0
                  IL_0018:  brfalse.s  IL_002a
                  IL_001a:  ldloc.0
                  IL_001b:  call       "int[] System.Array.Empty<int>()"
                  IL_0020:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync(params int[])"
                  IL_0025:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_002a:  ldloc.1
                  IL_002b:  brfalse.s  IL_0042
                  IL_002d:  ldloc.1
                  IL_002e:  isinst     "System.Exception"
                  IL_0033:  dup
                  IL_0034:  brtrue.s   IL_0038
                  IL_0036:  ldloc.1
                  IL_0037:  throw
                  IL_0038:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_003d:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0042:  ldstr      "return"
                  IL_0047:  call       "void System.Console.Write(string)"
                  IL_004c:  ldc.i4.1
                  IL_004d:  ret
                }
                """);
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
                // 0.cs(6,22): error CS8410: 'C': type used in an asynchronous using statement must implement 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (var x = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "var x = new C()").WithArguments("C").WithLocation(6, 22));
        }

        [Fact]
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
            string expectedOutput = "using dispose_start dispose_end return";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);

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
", sequencePointDisplay: SequencePointDisplayMode.Enhanced);

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Unexpected type on the stack. { Offset = 0x48, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x38 }
                    """
            });
            verifier.VerifyIL("C.Main()", """
                {
                  // Code size       73 (0x49)
                  .maxstack  2
                  .locals init (C V_0, //x
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  .try
                  {
                    IL_0008:  ldstr      "using "
                    IL_000d:  call       "void System.Console.Write(string)"
                    IL_0012:  leave.s    IL_0017
                  }
                  catch object
                  {
                    IL_0014:  stloc.1
                    IL_0015:  leave.s    IL_0017
                  }
                  IL_0017:  ldloc.0
                  IL_0018:  brfalse.s  IL_0025
                  IL_001a:  ldloc.0
                  IL_001b:  callvirt   "System.Threading.Tasks.ValueTask C.DisposeAsync()"
                  IL_0020:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.ValueTask)"
                  IL_0025:  ldloc.1
                  IL_0026:  brfalse.s  IL_003d
                  IL_0028:  ldloc.1
                  IL_0029:  isinst     "System.Exception"
                  IL_002e:  dup
                  IL_002f:  brtrue.s   IL_0033
                  IL_0031:  ldloc.1
                  IL_0032:  throw
                  IL_0033:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0038:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_003d:  ldstr      "return"
                  IL_0042:  call       "void System.Console.Write(string)"
                  IL_0047:  ldc.i4.1
                  IL_0048:  ret
                }
                """);
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
            var expectedOutput = "using dispose_start dispose_end return";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            // Runtime async verification
            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with { ILVerifyMessage = """
                [Main]: Unexpected type on the stack. { Offset = 0x5e, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                """ });
            verifier.VerifyIL("C.Main", """
                {
                  // Code size       95 (0x5f)
                  .maxstack  2
                  .locals init (C V_0, //x
                                object V_1,
                                Awaiter V_2)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "C"
                  IL_0008:  ldnull
                  IL_0009:  stloc.1
                  .try
                  {
                    IL_000a:  ldstr      "using "
                    IL_000f:  call       "void System.Console.Write(string)"
                    IL_0014:  leave.s    IL_0019
                  }
                  catch object
                  {
                    IL_0016:  stloc.1
                    IL_0017:  leave.s    IL_0019
                  }
                  IL_0019:  ldloca.s   V_0
                  IL_001b:  call       "Awaitable C.DisposeAsync()"
                  IL_0020:  callvirt   "Awaiter Awaitable.GetAwaiter()"
                  IL_0025:  stloc.2
                  IL_0026:  ldloc.2
                  IL_0027:  callvirt   "bool Awaiter.IsCompleted.get"
                  IL_002c:  brtrue.s   IL_0034
                  IL_002e:  ldloc.2
                  IL_002f:  call       "void System.Runtime.CompilerServices.AsyncHelpers.AwaitAwaiter<Awaiter>(Awaiter)"
                  IL_0034:  ldloc.2
                  IL_0035:  callvirt   "bool Awaiter.GetResult()"
                  IL_003a:  pop
                  IL_003b:  ldloc.1
                  IL_003c:  brfalse.s  IL_0053
                  IL_003e:  ldloc.1
                  IL_003f:  isinst     "System.Exception"
                  IL_0044:  dup
                  IL_0045:  brtrue.s   IL_0049
                  IL_0047:  ldloc.1
                  IL_0048:  throw
                  IL_0049:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_004e:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0053:  ldstr      "return"
                  IL_0058:  call       "void System.Console.Write(string)"
                  IL_005d:  ldc.i4.1
                  IL_005e:  ret
                }
                """);
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
            var expectedOutput = "using dispose_start dispose_end return";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            // Runtime async verification
            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Unexpected type on the stack. { Offset = 0x48, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [DisposeAsync]: Return value missing on the stack. { Offset = 0x38 }
                    """
            });
            verifier.VerifyIL("C.Main", """
                {
                  // Code size       73 (0x49)
                  .maxstack  2
                  .locals init (C V_0, //x
                                object V_1)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "C"
                  IL_0008:  ldnull
                  IL_0009:  stloc.1
                  .try
                  {
                    IL_000a:  ldstr      "using "
                    IL_000f:  call       "void System.Console.Write(string)"
                    IL_0014:  leave.s    IL_0019
                  }
                  catch object
                  {
                    IL_0016:  stloc.1
                    IL_0017:  leave.s    IL_0019
                  }
                  IL_0019:  ldloca.s   V_0
                  IL_001b:  call       "System.Threading.Tasks.Task C.DisposeAsync()"
                  IL_0020:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
                  IL_0025:  ldloc.1
                  IL_0026:  brfalse.s  IL_003d
                  IL_0028:  ldloc.1
                  IL_0029:  isinst     "System.Exception"
                  IL_002e:  dup
                  IL_002f:  brtrue.s   IL_0033
                  IL_0031:  ldloc.1
                  IL_0032:  throw
                  IL_0033:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0038:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_003d:  ldstr      "return"
                  IL_0042:  call       "void System.Console.Write(string)"
                  IL_0047:  ldc.i4.1
                  IL_0048:  ret
                }
                """);
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
            var expectedOutput = "using dispose_start dispose_end return";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            // Runtime async verification
            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with { ILVerifyMessage = """
                [Main]: Unexpected type on the stack. { Offset = 0x49, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                [DisposeAsync]: Unexpected type on the stack. { Offset = 0x39, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                """ });
            verifier.VerifyIL("C.Main", """
                {
                  // Code size       74 (0x4a)
                  .maxstack  2
                  .locals init (C V_0, //x
                                object V_1)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "C"
                  IL_0008:  ldnull
                  IL_0009:  stloc.1
                  .try
                  {
                    IL_000a:  ldstr      "using "
                    IL_000f:  call       "void System.Console.Write(string)"
                    IL_0014:  leave.s    IL_0019
                  }
                  catch object
                  {
                    IL_0016:  stloc.1
                    IL_0017:  leave.s    IL_0019
                  }
                  IL_0019:  ldloca.s   V_0
                  IL_001b:  call       "System.Threading.Tasks.Task<int> C.DisposeAsync()"
                  IL_0020:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0025:  pop
                  IL_0026:  ldloc.1
                  IL_0027:  brfalse.s  IL_003e
                  IL_0029:  ldloc.1
                  IL_002a:  isinst     "System.Exception"
                  IL_002f:  dup
                  IL_0030:  brtrue.s   IL_0034
                  IL_0032:  ldloc.1
                  IL_0033:  throw
                  IL_0034:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_0039:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_003e:  ldstr      "return"
                  IL_0043:  call       "void System.Console.Write(string)"
                  IL_0048:  ldc.i4.1
                  IL_0049:  ret
                }
                """);
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

            // Runtime async verification
            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("StructAwaitable"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x4f }
                    """
            });
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size       80 (0x50)
                  .maxstack  2
                  .locals init (Disposable V_0,
                                object V_1,
                                System.Runtime.CompilerServices.TaskAwaiter V_2)
                  IL_0000:  newobj     "Disposable..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  .try
                  {
                    IL_0008:  leave.s    IL_000d
                  }
                  catch object
                  {
                    IL_000a:  stloc.1
                    IL_000b:  leave.s    IL_000d
                  }
                  IL_000d:  ldloc.0
                  IL_000e:  brfalse.s  IL_0037
                  IL_0010:  ldloc.0
                  IL_0011:  callvirt   "StructAwaitable Disposable.DisposeAsync()"
                  IL_0016:  box        "StructAwaitable"
                  IL_001b:  call       "System.Runtime.CompilerServices.TaskAwaiter Extensions.GetAwaiter(object)"
                  IL_0020:  stloc.2
                  IL_0021:  ldloca.s   V_2
                  IL_0023:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
                  IL_0028:  brtrue.s   IL_0030
                  IL_002a:  ldloc.2
                  IL_002b:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.TaskAwaiter>(System.Runtime.CompilerServices.TaskAwaiter)"
                  IL_0030:  ldloca.s   V_2
                  IL_0032:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
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
                  IL_004f:  ret
                }
                """);
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

            // Runtime async verification (Note: This test doesn't require IAsyncDisposableDefinition since the interface is missing)
            comp = CreateRuntimeAsyncCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_IAsyncDisposable);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("DISPOSED"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [<Main>$]: Return value missing on the stack. { Offset = 0x33 }
                    """
            });
            verifier.VerifyIL("<top-level-statements-entry-point>", """
                {
                  // Code size       52 (0x34)
                  .maxstack  2
                  .locals init (C V_0,
                                object V_1)
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldnull
                  IL_0007:  stloc.1
                  .try
                  {
                    IL_0008:  leave.s    IL_000d
                  }
                  catch object
                  {
                    IL_000a:  stloc.1
                    IL_000b:  leave.s    IL_000d
                  }
                  IL_000d:  ldloc.0
                  IL_000e:  brfalse.s  IL_001b
                  IL_0010:  ldloc.0
                  IL_0011:  callvirt   "System.Threading.Tasks.Task C.DisposeAsync()"
                  IL_0016:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
                  IL_001b:  ldloc.1
                  IL_001c:  brfalse.s  IL_0033
                  IL_001e:  ldloc.1
                  IL_001f:  isinst     "System.Exception"
                  IL_0024:  dup
                  IL_0025:  brtrue.s   IL_0029
                  IL_0027:  ldloc.1
                  IL_0028:  throw
                  IL_0029:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
                  IL_002e:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
                  IL_0033:  ret
                }
                """);
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
                // source(2,8): error CS1674: 'C': type used in a using statement must implement 'System.IDisposable'.
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

            // Runtime async verification
            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("DISPOSED"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [<Main>$]: Return value missing on the stack. { Offset = 0x3b }
                    [System.IAsyncDisposable.DisposeAsync]: Return value missing on the stack. { Offset = 0x2e }
                    """
            });
            verifier.VerifyIL("Class1.System.IAsyncDisposable.DisposeAsync", """
                {
                  // Code size       47 (0x2f)
                  .maxstack  1
                  .locals init (System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_0,
                                System.Runtime.CompilerServices.YieldAwaitable V_1)
                  IL_0000:  ldstr      "DISPOSED"
                  IL_0005:  call       "void System.Console.Write(string)"
                  IL_000a:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                  IL_000f:  stloc.1
                  IL_0010:  ldloca.s   V_1
                  IL_0012:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                  IL_0017:  stloc.0
                  IL_0018:  ldloca.s   V_0
                  IL_001a:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                  IL_001f:  brtrue.s   IL_0027
                  IL_0021:  ldloc.0
                  IL_0022:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                  IL_0027:  ldloca.s   V_0
                  IL_0029:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                  IL_002e:  ret
                }
                """);
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
                // (5,1): error CS8410: 'Class1': type used in an asynchronous using statement must implement 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
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
                // (7,32): error CS9334: 'Class1.DisposeAsync()' return type must be 'System.Threading.Tasks.ValueTask' to match implemented member 'System.IAsyncDisposable.DisposeAsync()'
                //     ValueTask IAsyncDisposable.DisposeAsync()
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceMemberReturnTypeMismatch, "DisposeAsync").WithArguments("Class1.DisposeAsync()", "System.Threading.Tasks.ValueTask", "System.IAsyncDisposable.DisposeAsync()").WithLocation(7, 32),
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
