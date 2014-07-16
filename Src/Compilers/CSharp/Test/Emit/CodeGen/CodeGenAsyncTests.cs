// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenAsyncTests : EmitMetadataTestBase
    {
        private CSharpCompilation CreateCompilation(string source, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions compOptions = null)
        {
            SynchronizationContext.SetSynchronizationContext(null);

            compOptions = compOptions ?? TestOptions.OptimizedExe;

            IEnumerable<MetadataReference> asyncRefs = new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef };
            references = (references != null) ? references.Concat(asyncRefs) : asyncRefs;

            return CreateCompilationWithMscorlib45(source, compOptions: compOptions, references: references);
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput, IEnumerable<MetadataReference> references = null, EmitOptions emitOptions = EmitOptions.All, CSharpCompilationOptions compOptions = null, bool emitPdb = false)
        {
            SynchronizationContext.SetSynchronizationContext(null);

            var compilation = this.CreateCompilation(source, references: references, compOptions: compOptions);
            return base.CompileAndVerify(compilation, expectedOutput: expectedOutput, emitOptions: emitOptions, emitPdb: emitPdb);
        }

        [Fact]
        public void StructVsClass()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;

class Test
{
    public static async Task F(int a)
    {
        await Task.Factory.StartNew(() => { System.Console.WriteLine(a); });
    }

    public static void Main()
    {   
        F(123).Wait();
    }
}";
            var c = CreateCompilationWithMscorlib45(source);

            CompilationOptions options;

            options = TestOptions.OptimizedExe;
            Assert.False(options.EnableEditAndContinue);

            CompileAndVerify(c.WithOptions(options), symbolValidator: module =>
            {
                var stateMachine = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test").GetMember<NamedTypeSymbol>("<F>d__1");
                Assert.Equal(TypeKind.Struct, stateMachine.TypeKind);
            }, expectedOutput: "123");

            options = TestOptions.UnoptimizedExe;
            Assert.False(options.EnableEditAndContinue);

            CompileAndVerify(c.WithOptions(options), symbolValidator: module =>
            {
                var stateMachine = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test").GetMember<NamedTypeSymbol>("<F>d__1");
                Assert.Equal(TypeKind.Struct, stateMachine.TypeKind);
            }, expectedOutput: "123");

            options = TestOptions.OptimizedExe.WithDebugInformationKind(DebugInformationKind.Full);
            Assert.False(options.EnableEditAndContinue);

            CompileAndVerify(c.WithOptions(options), symbolValidator: module =>
            {
                var stateMachine = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test").GetMember<NamedTypeSymbol>("<F>d__1");
                Assert.Equal(TypeKind.Struct, stateMachine.TypeKind);
            }, expectedOutput: "123");

            options = TestOptions.UnoptimizedExe.WithDebugInformationKind(DebugInformationKind.PdbOnly);
            Assert.False(options.EnableEditAndContinue);

            CompileAndVerify(c.WithOptions(options), symbolValidator: module =>
            {
                var stateMachine = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test").GetMember<NamedTypeSymbol>("<F>d__1");
                Assert.Equal(TypeKind.Struct, stateMachine.TypeKind);
            }, expectedOutput: "123");

            options = TestOptions.UnoptimizedExe.WithDebugInformationKind(DebugInformationKind.Full);
            Assert.True(options.EnableEditAndContinue);

            CompileAndVerify(c.WithOptions(options), symbolValidator: module =>
            {
                var stateMachine = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test").GetMember<NamedTypeSymbol>("<F>d__1");
                Assert.Equal(TypeKind.Class, stateMachine.TypeKind);
            }, expectedOutput: "123");
        }

        [Fact]
        public void VoidReturningAsync()
        {
            var source = @"
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Test
{
    static int i = 0;

    public static async void F(AutoResetEvent handle)
    {
        try
        {
            await Task.Factory.StartNew(() =>
            {
                Interlocked.Increment(ref Test.i);
            });
        }
        finally
        {
            handle.Set();
        }
    }

    public static void Main()
    {
        var handle = new AutoResetEvent(false);
        F(handle);
        handle.WaitOne(1000 * 60);
        Console.WriteLine(i);
    }
}";
            var expected = @"
1
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        [WorkItem(624970, "DevDiv")]
        public void AsyncWithEH()
        {
            var source = @"
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Test
{
    static int awaitCount = 0;
    static int finallyCount = 0;

    static void LogAwait()
    {
        Interlocked.Increment(ref awaitCount);
    }

    static void LogException()
    {
        Interlocked.Increment(ref finallyCount);
    }

    public static async void F(AutoResetEvent handle)
    {
        try
        {
            await Task.Factory.StartNew(LogAwait);
            try
            {
                await Task.Factory.StartNew(LogAwait);
                try
                {
                    await Task.Factory.StartNew(LogAwait);
                    try
                    {
                        await Task.Factory.StartNew(LogAwait);
                        throw new Exception();
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        LogException();
                    }
                    await Task.Factory.StartNew(LogAwait);
                    throw new Exception();
                }
                catch (Exception)
                {
                }
                finally
                {
                    LogException();
                }
                await Task.Factory.StartNew(LogAwait);
                throw new Exception();
            }
            catch (Exception)
            {
            }
            finally
            {
                LogException();
            }
            await Task.Factory.StartNew(LogAwait);
        }
        finally
        {
            handle.Set();
        }
    }

    public static void Main2(int i)
    {
        try
        {
            awaitCount = 0;
            finallyCount = 0;
            var handle = new AutoResetEvent(false);
            F(handle);
            var completed = handle.WaitOne(1000 * 60);
            if (completed)
            {
                if (awaitCount != 7 || finallyCount != 3)
                {
                    throw new Exception(""failed at i="" + i);
                }
            }
            else
            {
                Console.WriteLine(""Test did not complete in time."");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(""unexpected exception thrown:"");
            Console.WriteLine(ex.ToString());
        }
    }

    public static void Main()
    {
        for (int i = 0; i < 1500; i++)
        {
            Main2(i);
        }
    }
}";
            var expected = @"";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact, WorkItem(855080, "DevDiv")]
        public void GenericCatchVariableInAsyncMethod()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Bar().Result);
        }
        static async Task<int> Bar()
        {
            NotImplementedException ex = await Foo<NotImplementedException>();
            return 3;
        }
        public static async Task<T> Foo<T>() where T : Exception
        {
            Task<int> task = null;
            if (task != null) await task;
            T result = null;
            try
            {
            }
            catch (T ex)
            {
                result = ex;
            }
            return result;
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "3");
        }

        [Fact]
        public void TaskReturningAsync()
        {
            var source = @"
using System;
using System.Diagnostics;
using System.Threading.Tasks;

class Test
{
    static int i = 0;

    public static async Task F()
    {
        await Task.Factory.StartNew(() =>
        {
            Test.i = 42;
        });
    }

    public static void Main()
    {
        Task t = F();
        t.Wait(1000 * 60);
        Console.WriteLine(Test.i);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void GenericTaskReturningAsync()
        {
            var source = @"
using System;
using System.Diagnostics;
using System.Threading.Tasks;

class Test
{
    public static async Task<string> F()
    {
        return await Task.Factory.StartNew(() => { return ""O brave new world...""; });
    }

    public static void Main()
    {
        Task<string> t = F();
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
O brave new world...
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithLocals()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(int x)
    {
        int c = 0;
        await F(x);
        c += x;
        await F(x);
        c += x;
        return c;
    }

    public static void Main()
    {
        Task<int> t = G(21);
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithTernary()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<T> F<T>(T x)
    {
        Console.WriteLine(""F("" + x + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(bool b1, bool b2)
    {
        int c = 0;
        c = c + (b1 ? 1 : await F(2));
        c = c + (b2 ? await F(4) : 8);
        return await F(c);
    }

    public static int H(bool b1, bool b2)
    {
        Task<int> t = G(b1, b2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        Console.WriteLine(H(false, false));
        Console.WriteLine(H(false, true));
        Console.WriteLine(H(true, false));
        Console.WriteLine(H(true, true));
    }
}";
            var expected = @"
F(2)
F(10)
10
F(2)
F(4)
F(6)
6
F(9)
9
F(4)
F(5)
5
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithAnd()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<T> F<T>(T x)
    {
        Console.WriteLine(""F("" + x + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(bool b1, bool b2)
    {
        bool x1 = b1 && await F(true);
        bool x2 = b1 && await F(false);
        bool x3 = b2 && await F(true);
        bool x4 = b2 && await F(false);
        int c = 0;
        if (x1) c += 1;
        if (x2) c += 2;
        if (x3) c += 4;
        if (x4) c += 8;
        return await F(c);
    }

    public static int H(bool b1, bool b2)
    {
        Task<int> t = G(b1, b2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        Console.WriteLine(H(false, true));
    }
}";
            var expected = @"
F(True)
F(False)
F(4)
4
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithOr()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<T> F<T>(T x)
    {
        Console.WriteLine(""F("" + x + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(bool b1, bool b2)
    {
        bool x1 = b1 || await F(true);
        bool x2 = b1 || await F(false);
        bool x3 = b2 || await F(true);
        bool x4 = b2 || await F(false);
        int c = 0;
        if (x1) c += 1;
        if (x2) c += 2;
        if (x3) c += 4;
        if (x4) c += 8;
        return await F(c);
    }

    public static int H(bool b1, bool b2)
    {
        Task<int> t = G(b1, b2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        Console.WriteLine(H(false, true));
    }
}";
            var expected = @"
F(True)
F(False)
F(13)
13
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithCoalesce()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<string> F(string x)
    {
        Console.WriteLine(""F("" + (x ?? ""null"") + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<string> G(string s1, string s2)
    {
        var result = await F(s1) ?? await F(s2);
        Console.WriteLine("" "" + (result ?? ""null""));
        return result;
    }

    public static string H(string s1, string s2)
    {
        Task<string> t = G(s1, s2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        H(null, null);
        H(null, ""a"");
        H(""b"", null);
        H(""c"", ""d"");
    }
}";
            var expected = @"
F(null)
F(null)
 null
F(null)
F(a)
 a
F(b)
 b
F(c)
 c
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithParam()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> G(int x)
    {
        await Task.Factory.StartNew(() => { return x; });
        x += 21;
        await Task.Factory.StartNew(() => { return x; });
        x += 21;
        return x;
    }

    public static void Main()
    {
        Task<int> t = G(0);
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AwaitInExpr()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F()
    {
        return await Task.Factory.StartNew(() => 21);
    }

    public static async Task<int> G()
    {
        int c = 0;
        c = (await F()) + 21;
        return c;
    }

    public static void Main()
    {
        Task<int> t = G();
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithParamsAndLocals_UnHoisted()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(int x)
    {
        int c = 0;
        c = await F(x);
        return c;
    }

    public static void Main()
    {
        Task<int> t = G(21);
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
21
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithParamsAndLocals_Hoisted()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(int x)
    {
        int c = 0;
        c = await F(x);
        return c;
    }

    public static void Main()
    {
        Task<int> t = G(21);
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
21
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithParamsAndLocals_DoubleAwait_Spilling()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(int x)
    {
        int c = 0;
        c = (await F(x)) + c;
        c = (await F(x)) + c;
        return c;
    }

    public static void Main()
    {
        Task<int> t = G(21);
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            // When the local 'c' gets hoisted, the statement:
            //   c = (await F(x)) + c;
            // Gets rewritten to:
            //   this.c_field = (await F(x)) + this.c_field;
            //
            // The code-gen for the assignment is something like this:
            //   ldarg0  // load the 'this' reference to the stack
            //   <emitted await expression>
            //   stfld
            //
            // What we really want is to evaluate any parts of the lvalue that have side-effects (which is this case is
            // nothing), and then defer loading the address for the field reference until after the await expression:
            //   <emitted await expression>
            //   <store to tmp>
            //   ldarg0
            //   <load tmp>
            //   stfld
            //
            // So this case actually requires stack spilling, which is not yet implemented. This has the unfortunate
            // consequence of preventing await expressions from being assigned to hoisted locals.
            //
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithDynamic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F(dynamic t)
    {
        return await t;
    }

    public static void Main()
    {
        Task<int> t = F(Task.Factory.StartNew(() => { return 42; }));
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expectedOutput: expected, references: new[] { CSharpRef });
        }

        [Fact]
        public void AsyncWithThisRef()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    int x = 42;

    public async Task<int> F()
    {
        int c = this.x;
        return await Task.Factory.StartNew(() => c);
    }
}

class Test
{
    public static void Main()
    {
        Task<int> t = new C().F();
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithBaseRef()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class B
{
    protected int x = 42;   
}

class C : B
{
    public async Task<int> F()
    {
        int c = base.x;
        return await Task.Factory.StartNew(() => c);
    }
}

class Test
{
    public static void Main()
    {
        Task<int> t = new C().F();
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithException1()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        throw new Exception();
    }

    static async Task<int> G()
    {
        try
        {
            return await F();
        }
        catch(Exception)
        {
            return -1;
        }
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
-1
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithException2()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        throw new Exception();
    }

    static async Task<int> H()
    {
        return await F();
    }

    public static void Main()
    {
        Task<int> t1 = H();
        try
        {
            t1.Wait(1000 * 60);
        }
        catch (AggregateException)
        {
            Console.WriteLine(""exception"");
        }
    }
}";
            var expected = @"
exception
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInFinally001()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        return 2;
    }

    static async Task<int> G()
    {
        int x = 42;

        try
        {
        }
        finally
        {
            x = await F();
        }

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
2
";
            CompileAndVerify(source, expectedOutput: expected).
VerifyIL("Test.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      229 (0xe5)
  .maxstack  3
  .locals init (int V_0,
  int V_1,
  object V_2,
  System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
  object V_4,
  System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<G>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_000e
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_005f
  IL_000e:  ldarg.0
  IL_000f:  ldnull
  IL_0010:  stfld      ""object Test.<G>d__1.<>7__wrap1""
  IL_0015:  ldarg.0
  IL_0016:  ldc.i4.0
  IL_0017:  stfld      ""int Test.<G>d__1.<>7__wrap2""
  .try
{
  IL_001c:  leave.s    IL_0028
}
  catch object
{
  IL_001e:  stloc.2
  IL_001f:  ldarg.0
  IL_0020:  ldloc.2
  IL_0021:  stfld      ""object Test.<G>d__1.<>7__wrap1""
  IL_0026:  leave.s    IL_0028
}
  IL_0028:  call       ""System.Threading.Tasks.Task<int> Test.F()""
  IL_002d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
  IL_0032:  stloc.3
  IL_0033:  ldloca.s   V_3
  IL_0035:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
  IL_003a:  brtrue.s   IL_007b
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.1
  IL_003e:  dup
  IL_003f:  stloc.0
  IL_0040:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_0045:  ldarg.0
  IL_0046:  ldloc.3
  IL_0047:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
  IL_004c:  ldarg.0
  IL_004d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_0052:  ldloca.s   V_3
  IL_0054:  ldarg.0
  IL_0055:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
  IL_005a:  leave      IL_00e4
  IL_005f:  ldarg.0
  IL_0060:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
  IL_0065:  stloc.3
  IL_0066:  ldarg.0
  IL_0067:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
  IL_006c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_0072:  ldarg.0
  IL_0073:  ldc.i4.m1
  IL_0074:  dup
  IL_0075:  stloc.0
  IL_0076:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_007b:  ldloca.s   V_3
  IL_007d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
  IL_0082:  ldloca.s   V_3
  IL_0084:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_008a:  ldarg.0
  IL_008b:  ldfld      ""object Test.<G>d__1.<>7__wrap1""
  IL_0090:  stloc.s    V_4
  IL_0092:  ldloc.s    V_4
  IL_0094:  brfalse.s  IL_00ad
  IL_0096:  ldloc.s    V_4
  IL_0098:  isinst     ""System.Exception""
  IL_009d:  dup
  IL_009e:  brtrue.s   IL_00a3
  IL_00a0:  ldloc.s    V_4
  IL_00a2:  throw
  IL_00a3:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
  IL_00a8:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
  IL_00ad:  ldarg.0
  IL_00ae:  ldnull
  IL_00af:  stfld      ""object Test.<G>d__1.<>7__wrap1""
  IL_00b4:  stloc.1
  IL_00b5:  leave.s    IL_00d0
}
  catch System.Exception
{
  IL_00b7:  stloc.s    V_5
  IL_00b9:  ldarg.0
  IL_00ba:  ldc.i4.s   -2
  IL_00bc:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_00c1:  ldarg.0
  IL_00c2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_00c7:  ldloc.s    V_5
  IL_00c9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
  IL_00ce:  leave.s    IL_00e4
}
  IL_00d0:  ldarg.0
  IL_00d1:  ldc.i4.s   -2
  IL_00d3:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_00d8:  ldarg.0
  IL_00d9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_00de:  ldloc.1
  IL_00df:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00e4:  ret
}
");
        }

        [Fact]
        public void AsyncInFinally002()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        System.Console.Write(""F"");
        return 2;
    }

    static async Task G()
    {
        int x = 0;

        try
        {
            throw new Exception(""hello"");
        }
        finally
        {
            x += await F();
        }
    }

    public static void Main()
    {
        Task t2 = G();
        try
        {
            t2.Wait(1000 * 60);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}";
            var expected = @"FOne or more errors occurred.
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInFinally003()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        return 2;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            x = await F();
            return x;
        }
        finally
        {
            x += await F();
        }
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
2
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInFinally004()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        return 2;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                throw new Exception();
            }
            finally
            {
                x += await F();
            }
        }
        catch
        {
            return x;
        }
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
2
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInFinallyNested001()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F(int a)
    {
        await Task.Yield();
        return a;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                x = await F(1);
                goto L1;
                System.Console.WriteLine(""FAIL"");
            }
            finally
            {
                x += await F(2);
            }
        }
        finally
        {
            try
            {
                x += await F(4);
            }
            finally
            {
                x += await F(8);
            }
        }

        System.Console.WriteLine(""FAIL"");

        L1:
        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"15";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInFinallyNested002()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F(int a)
    {
        await Task.Yield();
        return a;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                try
                {
                    x = await F(1);
                    throw new Exception(""hello"");
                    System.Console.WriteLine(""FAIL"");
                }
                finally
                {
                    x += await F(2);
                }

                System.Console.WriteLine(""FAIL"");
            }
            finally
            {
                try
                {
                    x += await F(4);
                }
                finally
                {
                    x += await F(8);
                }
            }

            System.Console.WriteLine(""FAIL"");
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex.Message);
        }      

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"hello
15";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInFinallyNested003()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F(int a)
    {
        await Task.Yield();
        return a;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                try
                {
                    x = await F(1);
                    throw new Exception(""hello"");
                    System.Console.WriteLine(""FAIL"");
                }
                finally
                {
                    x += await F(2);
                }

                System.Console.WriteLine(""FAIL"");
            }
            finally
            {
                try
                {
                    x += await F(4);
                }
                finally
                {
                    x += await F(8);
                    throw new Exception(""bye"");
                }
            }

            System.Console.WriteLine(""FAIL"");
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex.Message);
        }      

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"bye
15";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInCatch001()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        return 2;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            x = x / x;
        }
        catch
        {
            x = await F();
        }

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
2
";
            CompileAndVerify(source, expectedOutput: expected).
VerifyIL("Test.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      182 (0xb6)
  .maxstack  3
  .locals init (int V_0,
  int V_1, 
  int V_2, //x
  int V_3,
  System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
  System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<G>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_000e
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0057
  IL_000e:  ldc.i4.0
  IL_000f:  stloc.2
  IL_0010:  ldc.i4.0
  IL_0011:  stloc.3
  .try
{
  IL_0012:  ldloc.2
  IL_0013:  dup
  IL_0014:  div
  IL_0015:  stloc.2
  IL_0016:  leave.s    IL_001d
}
  catch object
{
  IL_0018:  pop
  IL_0019:  ldc.i4.1
  IL_001a:  stloc.3
  IL_001b:  leave.s    IL_001d
}
  IL_001d:  ldloc.3
  IL_001e:  ldc.i4.1
  IL_001f:  bne.un.s   IL_0084
  IL_0021:  call       ""System.Threading.Tasks.Task<int> Test.F()""
  IL_0026:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
  IL_002b:  stloc.s    V_4
  IL_002d:  ldloca.s   V_4
  IL_002f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
  IL_0034:  brtrue.s   IL_0074
  IL_0036:  ldarg.0
  IL_0037:  ldc.i4.1
  IL_0038:  dup
  IL_0039:  stloc.0
  IL_003a:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_003f:  ldarg.0
  IL_0040:  ldloc.s    V_4
  IL_0042:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
  IL_0047:  ldarg.0
  IL_0048:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_004d:  ldloca.s   V_4
  IL_004f:  ldarg.0
  IL_0050:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<G>d__1)""
  IL_0055:  leave.s    IL_00b5
  IL_0057:  ldarg.0
  IL_0058:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
  IL_005d:  stloc.s    V_4
  IL_005f:  ldarg.0
  IL_0060:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<G>d__1.<>u__$awaiter0""
  IL_0065:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_006b:  ldarg.0
  IL_006c:  ldc.i4.m1
  IL_006d:  dup
  IL_006e:  stloc.0
  IL_006f:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_0074:  ldloca.s   V_4
  IL_0076:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
  IL_007b:  ldloca.s   V_4
  IL_007d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_0083:  stloc.2
  IL_0084:  ldloc.2
  IL_0085:  stloc.1
  IL_0086:  leave.s    IL_00a1
}
  catch System.Exception
{
  IL_0088:  stloc.s    V_5
  IL_008a:  ldarg.0
  IL_008b:  ldc.i4.s   -2
  IL_008d:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_0092:  ldarg.0
  IL_0093:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_0098:  ldloc.s    V_5
  IL_009a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
  IL_009f:  leave.s    IL_00b5
}
  IL_00a1:  ldarg.0
  IL_00a2:  ldc.i4.s   -2
  IL_00a4:  stfld      ""int Test.<G>d__1.<>1__state""
  IL_00a9:  ldarg.0
  IL_00aa:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<G>d__1.<>t__builder""
  IL_00af:  ldloc.1
  IL_00b0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b5:  ret
}
");
        }

        [Fact]
        public void AsyncInCatchRethrow()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        await Task.Yield();
        return 2;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                x = x / x;
            }
            catch
            {
                x = await F();
                throw;
            }
        }
        catch(DivideByZeroException ex)
        {
            x = await F();
            System.Console.WriteLine(ex.Message);
        }

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
Attempted to divide by zero.
2
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInCatchFilter()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        await Task.Yield();
        return 2;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                x = x / x;
            }
            catch if(x != 0)
            {
                x = await F();
                throw;
            }
        }
        catch(Exception ex) if(x == 0 && ((ex = new Exception(""hello"")) != null))
        {
            x = await F();
            System.Console.WriteLine(ex.Message);
        }

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
hello
2
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInCatchFilterLifted()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F()
    {
        await Task.Yield();
        return 2;
    }

    static bool T(Func<bool> f, ref Exception ex)
    {
        var result = f();
        ex = new Exception(result.ToString());
        return result;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            x = x / x;
        }
        catch(Exception ex) if(T(()=>ex.Message == null, ref ex))
        {
            x = await F();
            System.Console.WriteLine(ex.Message);
        }
        catch(Exception ex) if(T(()=>ex.Message != null, ref ex))
        {
            x = await F();
            System.Console.WriteLine(ex.Message);
        }
        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"True
2
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInCatchFinallyMixed()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F(int x)
    {
        await Task.Yield();
        return x;
    }

    static async Task<int> G()
    {
        int x = 0;

        try
        {
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        try
                        {
                            x = x / await F(0);
                        }
                        catch (DivideByZeroException) if (i < 3)
                        {
                            await Task.Yield();
                            continue;
                        }
                        catch (DivideByZeroException)
                        {
                            x = 2 + await F(x);
                            throw;
                        }
                        System.Console.WriteLine(""FAIL"");
                    }
                    finally
                    {
                        x = await F(x) + 3;
                        if (i >= 3)
                        {
                            throw new Exception(""hello"");
                        }
                    }
                }
            }
            finally
            {
                x = 11 + await F(x);
            }
        }
        catch (Exception ex)
        {
            x = await F(x) + 17;
            System.Console.WriteLine(ex.Message);
        }

        return x;
    }

    public static void Main()
    {
        Task<int> t2 = G();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
hello
42
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInCatchFinallyMixed_InAsyncLambda()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<int> F(int x)
    {
        await Task.Yield();
        return x;
    }

    static Func<Task<int>> G()
    {
        int x = 0;

        return async () =>
        {
            try
            {
                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            try
                            {
                                x = x / await F(0);
                            }
                            catch (DivideByZeroException) if (i < 3)
                            {
                                await Task.Yield();
                                continue;
                            }
                            catch (DivideByZeroException)
                            {
                                x = 2 + await F(x);
                                throw;
                            }
                            System.Console.WriteLine(""FAIL"");
                        }
                        finally
                        {
                            x = await F(x) + 3;
                            if (i >= 3)
                            {
                                throw new Exception(""hello"");
                            }
                        }
                    }
                }
                finally
                {
                    x = 11 + await F(x);
                }
            }
            catch (Exception ex)
            {
                x = await F(x) + 17;
                System.Console.WriteLine(ex.Message);
            }

            return x;
        };
    }

    public static void Main()
    {
        Task<int> t2 = G()();
        t2.Wait(1000 * 60);
        Console.WriteLine(t2.Result);
    }
}";
            var expected = @"
hello
42
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncInConditionalAccess()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{

    class C1
    {
        public int M(int x)
        {
            return x;
        }
    }

    public static int Get(int x)
    {
        Console.WriteLine(""> "" + x);
        return x;
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task<int?> G()
    {
        var c = new C1();
        return c?.M(await F(Get(42)));
    }

    public static void Main()
    {
        var t = G();
        System.Console.WriteLine(t.Result);
    }
}";
            var expected = @"
> 42
42";
            CompileAndVerifyExperimental(source, expected);
        }


        [Fact]
        public void Conformance_Awaiting_Methods_Generic01()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading;

//Implementation of you own async pattern
public class MyTask<T>
{
    public MyTaskAwaiter<T> GetAwaiter()
    {
        return new MyTaskAwaiter<T>();
    }

    public async void Run<U>(U u) where U : MyTask<int>, new()
    {
        try
        {
            int tests = 0;

            tests++;
            var rez = await u;
            if (rez == 0)
                Driver.Count++;

            Driver.Result = Driver.Count - tests;
        }
        finally
        {
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}
public class MyTaskAwaiter<T> : INotifyCompletion
{
    public void OnCompleted(Action continuationAction)
    {
    }

    public T GetResult()
    {
        return default(T);
    }

    public bool IsCompleted { get { return true; } }
}
//-------------------------------------

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        new MyTask<int>().Run<MyTask<int>>(new MyTask<int>());

        CompletedSignal.WaitOne();

        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Conformance_Awaiting_Methods_Method01()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;


public interface IExplicit
{
    Task Method(int x = 4);
}

class C1 : IExplicit
{
    Task IExplicit.Method(int x)
    {
        //This will fail until Run and RunEx are merged back together
        return Task.Run(async () =>
        {
            await Task.Delay(1);
            Driver.Count++;
        });
    }
}

class TestCase
{
    public async void Run()
    {
        try
        {
            int tests = 0;
            tests++;

            C1 c = new C1();
            IExplicit e = (IExplicit)c;
            await e.Method();

            Driver.Result = Driver.Count - tests;
        }
        finally
        {
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void DoFinallyBodies()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Driver
{
    public static int finally_count = 0;

    static async Task F()
    {
        try
        {
            await Task.Factory.StartNew(() => { });
        }
        finally
        {
            Driver.finally_count++;
        }
    }
    
    static void Main()
    {
        var t = F();
        t.Wait();
        Console.WriteLine(Driver.finally_count);
    }
}";
            var expected = @"
1
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void Conformance_Awaiting_Methods_Parameter003()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

class TestCase
{
    public static int Count = 0;
    public static T Foo<T>(T t)
    {
        return t;
    }

    public async static Task<T> Bar<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public static async void Run()
    {
        try
        {
            int x1 = Foo(await Bar(4));
            Task<int> t = Bar(5);
            int x2 = Foo(await t);
            if (x1 != 4)
                Count++;
            if (x2 != 5)
                Count++;
        }
        finally
        {
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        TestCase.Run();

        CompletedSignal.WaitOne();

        // 0 - success
        Console.WriteLine(TestCase.Count);
    }
}";
            CompileAndVerify(source, expectedOutput: "0");
        }

        [Fact]
        public void Conformance_Awaiting_Methods_Method05()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;

class C
{
    public int Status;
    public C(){}  
}
interface IImplicit
{
    T Method<T>(params decimal[] d) where T : Task<C>;
}
class Impl : IImplicit
{
   public T Method<T>(params decimal[] d) where T : Task<C>
    {
        //this will fail until Run and RunEx<C> are merged
        return (T) Task.Run(async() =>
        {
            await Task.Delay(1);
            Driver.Count++;
            return new C() { Status = 1 };
        });
    }
}

class TestCase
{
    public async void Run()
    {
        try
        {
            int tests = 0;
            Impl i = new Impl();

            tests++;
            await i.Method<Task<C>>(3m, 4m);

            Driver.Result = Driver.Count - tests;
        }
        finally
        {
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Conformance_Awaiting_Methods_Accessible010()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

class TestCase:Test
{
    public static int Count = 0;
    public async static void Run()
    {
        try
        {
            int x = await Test.GetValue<int>(1);
            if (x != 1)
                Count++;
        }
        finally
        {
            Driver.CompletedSignal.Set();
        }
    }
}

class Test
{
    protected async static Task<T> GetValue<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }
}

class Driver
{
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        TestCase.Run();

        CompletedSignal.WaitOne();

        // 0 - success
        Console.WriteLine(TestCase.Count);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void NestedUnary()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F()
    {
        return 1;
    }

    public static async Task<int> G1()
    {
        return -(await F());
    }

    public static async Task<int> G2()
    {
        return -(-(await F()));
    }

    public static async Task<int> G3()
    {
        return -(-(-(await F())));
    }

    public static void WaitAndPrint(Task<int> t)
    {
        t.Wait();
        Console.WriteLine(t.Result);
    }

    public static void Main()
    {
        WaitAndPrint(G1());
        WaitAndPrint(G2());
        WaitAndPrint(G3());
    }
}";
            var expected = @"
-1
1
-1
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillCall()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b, int c, int d, int e)
    {
        foreach (var x in new List<int>() { a, b, c, d, e })
        {
            Console.WriteLine(x);
        }
    }

    public static int Get(int x)
    {
        Console.WriteLine(""> "" + x);
        return x;
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(Get(111), Get(222), Get(333), await F(Get(444)), Get(555));
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expected = @"
> 111
> 222
> 333
> 444
> 555
111
222
333
444
555
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillCall2()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b, int c, int d, int e)
    {
        foreach (var x in new List<int>() { a, b, c, d, e })
        {
            Console.WriteLine(x);
        }
    }

    public static int Get(int x)
    {
        Console.WriteLine(""> "" + x);
        return x;
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(Get(111), await F(Get(222)), Get(333), await F(Get(444)), Get(555));
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expected = @"
> 111
> 222
> 333
> 444
> 555
111
222
333
444
555
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillCall3()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b, int c, int d, int e, int f)
    {
        foreach (var x in new List<int>(){a, b, c, d, e, f})
        {
            Console.WriteLine(x);
        }
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(1, await F(2), 3, await F(await F(await F(await F(4)))), await F(5), 6);
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expected = @"
1
2
3
4
5
6
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillCall4()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b)
    {
        foreach (var x in new List<int>(){a, b})
        {
            Console.WriteLine(x);
        }
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(1, await F(await F(2)));
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expected = @"
1
2
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void Array01()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            tests++;
            int[] arr = new int[await GetVal(4)];
            if (arr.Length == 4)
                Driver.Count++;

            //multidimensional
            tests++;
            decimal[,] arr2 = new decimal[await GetVal(4), await GetVal(4)];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;

            arr2 = new decimal[4, await GetVal(4)];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;

            tests++;
            arr2 = new decimal[await GetVal(4), 4];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;


            //jagged array
            tests++;
            decimal?[][] arr3 = new decimal?[await GetVal(4)][];
            if (arr3.Rank == 2 && arr3.Length == 4)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void Array02()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            tests++;
            int[] arr = new int[await GetVal(4)];
            if (arr.Length == 4)
                Driver.Count++;

            tests++;
            arr[0] = await GetVal(4);
            if (arr[0] == 4)
                Driver.Count++;

            tests++;
            arr[0] += await GetVal(4);
            if (arr[0] == 8)
                Driver.Count++;

            tests++;
            arr[1] += await (GetVal(arr[0]));
            if (arr[1] == 8)
                Driver.Count++;

            tests++;
            arr[1] += await (GetVal(arr[await GetVal(0)]));
            if (arr[1] == 16)
                Driver.Count++;

            tests++;
            arr[await GetVal(2)]++;
            if (arr[2] == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }



        [Fact]
        public void Array03()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[,] arr = new int[await GetVal(4), await GetVal(4)];

            tests++;
            arr[0, 0] = await GetVal(4);
            if (arr[0, await (GetVal(0))] == 4)
                Driver.Count++;

            tests++;
            arr[0, 0] += await GetVal(4);
            if (arr[0, 0] == 8)
                Driver.Count++;

            tests++;
            arr[1, 1] += await (GetVal(arr[0, 0]));
            if (arr[1, 1] == 8)
                Driver.Count++;

            tests++;
            arr[1, 1] += await (GetVal(arr[0, await GetVal(0)]));
            if (arr[1, 1] == 16)
                Driver.Count++;

            tests++;
            arr[2, await GetVal(2)]++;
            if (arr[2, 2] == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }


        [Fact]
        public void Array04()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;

struct MyStruct<T>
{
    T t { get; set; }
    public T this[T index]
    {
        get
        {
            return t;
        }
        set
        {
            t = value;
        }
    }
}

struct TestCase
{
    public async void Run()
    {
        try
        {
            MyStruct<int> ms = new MyStruct<int>();
            var x = ms[index: await Foo()];
        }
        finally
        {
            Driver.CompletedSignal.Set();
        }
    }
    public async Task<int> Foo()
    {
        await Task.Delay(1);
        return 1;
    }
}

class Driver
{
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();
        CompletedSignal.WaitOne();
    }
}";
            CompileAndVerify(source, "");
        }

        [Fact]
        public void ArrayAssign()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class TestCase
{
    static int[] arr = new int[4];

    static async Task Run()
    {
        arr[0] = await Task.Factory.StartNew(() => 42);
    }

    static void Main()
    {
        Task task = Run();
        task.Wait();
        Console.WriteLine(arr[0]);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void CaptureThis()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;

struct TestCase
{
    public async Task<int> Run()
    {
        return await Foo();
    }

    public async Task<int> Foo()
    {
        return await Task.Factory.StartNew(() => 42);
    }
}

class Driver
{
    static void Main()
    {
        var t = new TestCase();
        var task = t.Run();
        task.Wait();
        Console.WriteLine(task.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }


        [Fact]
        public void CaptureThis2()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

struct TestCase
{
    public IEnumerable<int> Run()
    {
        yield return Foo();
    }

    public int Foo()
    {
        return 42;
    }
}

class Driver
{
    static void Main()
    {
        var t = new TestCase();
        foreach (var x in t.Run())
        {
            Console.WriteLine(x);
        }
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void spillArrayLocal()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int[] arr = new int[2] { -1, 42 };

        int tests = 0;
        try
        {
            tests++;
            int t1 = arr[await GetVal(1)];
            if (t1 == 42)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }


        [Fact]
        public void SpillArrayCompoundAssignmentLValue()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Driver
{
    static int[] arr;

    static async Task Run()
    {
        arr = new int[1];
        arr[0] += await Task.Factory.StartNew(() => 42);
    }

    static void Main()
    {
        Run().Wait();
        Console.WriteLine(arr[0]);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }


        [Fact]
        public void SpillArrayCompoundAssignmentLValueAwait()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Driver
{
    static int[] arr;

    static async Task Run()
    {
        arr = new int[1];
        arr[await Task.Factory.StartNew(() => 0)] += await Task.Factory.StartNew(() => 42);
    }

    static void Main()
    {
        Run().Wait();
        Console.WriteLine(arr[0]);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArrayCompoundAssignmentLValueAwait2()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

struct S1
{
    public int x;
}

struct S2
{
    public S1 s1;
}

class Driver
{
    static async Task<int> Run()
    {
        var arr = new S2[1];
        arr[await Task.Factory.StartNew(() => 0)].s1.x += await Task.Factory.StartNew(() => 42);
        return arr[await Task.Factory.StartNew(() => 0)].s1.x;
    }

    static void Main()
    {
        var t = Run();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void DoubleSpillArrayCompoundAssignment()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

struct S1
{
    public int x;
}

struct S2
{
    public S1 s1;
}

class Driver
{
    static async Task<int> Run()
    {
        var arr = new S2[1];
        arr[await Task.Factory.StartNew(() => 0)].s1.x += (arr[await Task.Factory.StartNew(() => 0)].s1.x += await Task.Factory.StartNew(() => 42));
        return arr[await Task.Factory.StartNew(() => 0)].s1.x;
    }

    static void Main()
    {
        var t = Run();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void array05()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[][] arr1 = new[]
                {
                    new []{await GetVal(2),await GetVal(3)},
                    new []{4,await GetVal(5),await GetVal(6)}
                };
            if (arr1[0][1] == 3 && arr1[1][1] == 5 && arr1[1][2] == 6)
                Driver.Count++;

            tests++;
            int[][] arr2 = new[]
                {
                    new []{await GetVal(2),await GetVal(3)},
                    await Foo()
                };
            if (arr2[0][1] == 3 && arr2[1][1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public async Task<int[]> Foo()
    {
        await Task.Delay(1);
        return new int[] { 1, 2, 3 };
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void array06()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[,] arr1 = 
                {
                    {await GetVal(2),await GetVal(3)},
                    {await GetVal(5),await GetVal(6)}
                };
            if (arr1[0, 1] == 3 && arr1[1, 0] == 5 && arr1[1, 1] == 6)
                Driver.Count++;

            tests++;
            int[,] arr2 = 
                {
                    {await GetVal(2),3},
                    {4,await GetVal(5)}
                };
            if (arr2[0, 1] == 3 && arr2[1, 1] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void array07()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[][] arr1 = new[]
                {
                    new []{await GetVal(2),await Task.Run<int>(async()=>{await Task.Delay(1);return 3;})},
                    new []{await GetVal(5),4,await Task.Run<int>(async()=>{await Task.Delay(1);return 6;})}
                };
            if (arr1[0][1] == 3 && arr1[1][1] == 4 && arr1[1][2] == 6)
                Driver.Count++;

            tests++;
            dynamic arr2 = new[]
                {
                    new []{await GetVal(2),3},
                    await Foo()
                };
            if (arr2[0][1] == 3 && arr2[1][1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public async Task<int[]> Foo()
    {
        await Task.Delay(1);
        return new int[] { 1, 2, 3 };
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void AssignToAwait()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class S
{
    public int x = -1;
}

class Test
{
    static S _s = new S();

    public static async Task<S> GetS()
    {
        return await Task.Factory.StartNew(() => _s);
    }

    public static async Task Run()
    {
        (await GetS()).x = 42;
        Console.WriteLine(_s.x);
    }
}

class Driver
{
    static void Main()
    {
        Test.Run().Wait();
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void AssignAwaitToAwait()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class S
{
    public int x = -1;
}

class Test
{
    static S _s = new S();

    public static async Task<S> GetS()
    {
        return await Task.Factory.StartNew(() => _s);
    }

    public static async Task Run()
    {
        (await GetS()).x = await Task.Factory.StartNew(() => 42);
        Console.WriteLine(_s.x);
    }
}

class Driver
{
    static void Main()
    {
        Test.Run().Wait();
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void ReuseFields()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    static void F1(int x, int y)
    {
    }

    async static Task<int> F2()
    {
        return await Task.Factory.StartNew(() => 42);
    }

    public static async void Run()
    {
        int x = 1;
        F1(x, await F2());

        int y = 2;
        F1(y, await F2());

        int z = 3;
        F1(z, await F2());
    }

    public static void Main()
    {
        Run();
    }
}";
            var reference = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { SystemRef_v4_0_30319_17929 }).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib45("", new[] { reference }, compOptions: TestOptions.DllAlwaysImportInternals);
            var testClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            var stateMachineClass = (NamedTypeSymbol)testClass.GetMembers().Single(s => s.Name.StartsWith("<Run>"));
            IEnumerable<IGrouping<TypeSymbol, FieldSymbol>> spillFieldsByType = stateMachineClass.GetMembers().Where(m => m.Kind == SymbolKind.Field && m.Name.StartsWith("<>7__wrap")).Cast<FieldSymbol>().GroupBy(x => x.Type);

            Assert.Equal(1, spillFieldsByType.Count());
            Assert.Equal(1, spillFieldsByType.Single(x => x.Key == comp.GetSpecialType(SpecialType.System_Int32)).Count());
        }

        [Fact]
        public void NextedExpressionInArrayInitializer()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int[,]> Run()
    {
        return new int[,] {
            {1, 2, 21 + (await Task.Factory.StartNew(() => 21)) },
        };
    }

    public static void Main()
    {
        var t = Run();
        t.Wait();
        foreach (var xs in t.Result)
        {
            Console.WriteLine(xs);
        }
    }
}";
            var expected = @"
1
2
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void Basic02()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    static int test = 0;
    static int count = 0;

    public static async Task Run()
    {
        try
        {
            test++;
            var f = new Func<int, object>(checked(await Bar()));
            var x = f(1);
            if ((string)x != ""1"")
                count--;
        }
        finally
        {
            Driver.Result = test - count;
            Driver.CompleteSignal.Set();
        }
    }
    static async Task<Converter<int, string>> Bar()
    {
        count++;
        await Task.Delay(1);

        return delegate(int p1) { return p1.ToString(); };
    }
}

class Driver
{
    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static int Result = -1;
    public static void Main()
    {
        TestCase.Run();
        CompleteSignal.WaitOne();

        Console.Write(Result);
    }
}";
            CompileAndVerify(source, expectedOutput:"0");
        }

//        [Fact]
//        public void Argument02()
//        {
//            var source = @"
//using System;
//using System.Runtime.InteropServices;
//using System.Threading;
//using System.Threading.Tasks;
//
//[ComImport]
//[Guid(""09133803-EF59-4467-9135-255A65B606C2"")]
//interface IA
//{
//    void Foo(ref long x, long? y);
//}
//
//class TestCase
//{
//    static int count = 0;
//    public async Task Run()
//    {
//        int test = 0;
//
//        test = 2;
//        IA a = null;
//        long x;
//        a.Foo(await Bar(), await Bar());
//
//        Driver.Result = test - count;
//        Driver.CompleteSignal.Set();
//    }
//    async Task<int> Bar()
//    {
//        await Task.Delay(1);
//        count++;
//        return 2;
//    }
//}
//class Driver
//{
//    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
//    public static int Result = -1;
//    public static void Main()
//    {
//        TestCase tc = new TestCase();
//        tc.Run();
//        CompleteSignal.WaitOne();
//
//        Console.WriteLine(Result);
//    }
//}
//";

//            CompileAndVerify(source, expectedOutput: "0");
//        }

        [Fact]
        public void Argument03()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    static StringBuilder sb = new StringBuilder();
    public async Task Run()
    {
        try
        {
            Bar(__arglist(One(), await Two()));
            if (sb.ToString() == ""OneTwo"")
                Driver.Result = 0;
        }
        finally
        {
            Driver.CompleteSignal.Set();
        }
    }
    int One()
    {
        sb.Append(""One"");
        return 1;
    }
    async Task<int> Two()
    {
        await Task.Delay(1);
        sb.Append(""Two"");
        return 2;
    }
    void Bar(__arglist)
    {
        var ai = new ArgIterator(__arglist);
        while (ai.GetRemainingCount() > 0)
            Console.WriteLine( __refvalue(ai.GetNextArg(), int));
    }
}
class Driver
{
    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static int Result = -1;
    public static void Main()
    {
        TestCase tc = new TestCase();
        tc.Run();
        CompleteSignal.WaitOne();

        Console.WriteLine(Result);
    }
}";
            var expected = @"
1
2
0
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void ObjectInit02()
        {
            var source = @"
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;


struct TestCase : IEnumerable
{
    int X;
    public async Task Run()
    {
        int test = 0;
        int count = 0;
        try
        {
            test++;
            var x = new TestCase { X = await Bar() };
            if (x.X == 1)
                count++;
        }
        finally
        {
            Driver.Result = test - count;
            Driver.CompleteSignal.Set();
        }
    }
    async Task<int> Bar()
    {
        await Task.Delay(1);
        return 1;
    }

    public IEnumerator GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}
class Driver
{
    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static int Result = -1;
    public static void Main()
    {
        TestCase tc = new TestCase();
        tc.Run();
        CompleteSignal.WaitOne();

        Console.WriteLine(Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void Generic01()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    static int test = 0;
    static int count = 0;

    public static async Task Run()
    {
        try
        {
            test++;
            Qux(async () => { return 1; });
            await Task.Delay(50);
        }
        finally
        {
            Driver.Result = test - count;
            Driver.CompleteSignal.Set();
        }
    }
    static async void Qux<T>(Func<Task<T>> x)
    {
        var y = await x();
        if ((int)(object)y == 1)
            count++;
    }

}

class Driver
{
    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static int Result = -1;
    public static void Main()
    {
        TestCase.Run();
        CompleteSignal.WaitOne();

        Console.WriteLine(Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void Ref01()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class BaseTestCase
{
    public void FooRef(ref decimal d, int x, out decimal od)
    {
        od = d;
        d++;
    }

    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }
}

class TestCase : BaseTestCase
{
    public async void Run()
    {
        int tests = 0;
        try
        {
            decimal d = 1;
            decimal od;

            tests++;
            base.FooRef(ref d, await base.GetVal(4), out od);
            if (d == 2 && od == 1) Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Struct02()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

struct TestCase
{
    private Task<int> t;
    public async void Run()
    {
        int tests = 0;
        try
        {
            tests++;
            t = Task.Run(async () => { await Task.Delay(1); return 1; });
            var x = await t;
            if (x == 1) Driver.Count++;

            tests++;
            t = Task.Run(async () => { await Task.Delay(1); return 1; });
            var x2 = await this.t;
            if (x2 == 1) Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.Write(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void StackSpill_Operator_Compound02()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void OperatorHoist()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Delegate10()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;

delegate Task MyDel<U>(out U u);
class MyClass<T>
{
    public static Task Meth(out T t)
    {
        t = default(T);
        return Task.Run(async () => { await Task.Delay(1); TestCase.Count++; });
    }
    public MyDel<T> myDel;
    public event MyDel<T> myEvent;
    public async Task TrigerEvent(T p)
    {
        try
        {
            await myEvent(out p);
        }
        catch
        {
            TestCase.Count += 5;
        }

    }
}
struct TestCase
{
    public static int Count = 0;
    private int tests;
    public async void Run()
    {
        tests = 0;
        try
        {
            tests++;
            MyClass<string> ms = new MyClass<string>();
            ms.myDel = MyClass<string>.Meth;
            string str = """";
            await ms.myDel(out str);

            tests++;
            ms.myEvent += MyClass<string>.Meth;
            await ms.TrigerEvent(str);
        }
        finally
        {
            Driver.Result = TestCase.Count - this.tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Unhoisted_Used_Param()
        {
            var source = @"
using System.Collections.Generic;

struct Test
{
    public static IEnumerable<int> F(int x)
    {
        x = 2;
        yield return 1;
    }

    public static void Main()
    {
        F(1);
    }
}";
            CompileAndVerify(source, "");
        }

        [Fact]
        public void AwaitSwitch()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async void Run()
    {
        int test = 0;
        int result = 0;
        try
        {
            test++;
            switch (await ((Func<Task<int>>)(async () => { await Task.Delay(1); return 5; }))())
            {
                case 1:
                case 2: break;
                case 5: result++; break;
                default: break;
            }
        }
        finally
        {
            Driver.Result = test - result;
            Driver.CompleteSignal.Set();
        }
    }

}

class Driver
{
    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static int Result = -1;
    public static void Main()
    {
        TestCase tc = new TestCase();
        tc.Run();
        CompleteSignal.WaitOne();

        Console.WriteLine(Result);
    }
}";
            CompileAndVerify(source, "0");
        }


        [Fact]
        public void Return07()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    unsafe struct S
    {
        public int value;
        public S* next;
    }

    public async void Run()
    {
        int test = 0;
        int result = 0;
        try
        {
            Func<Task<dynamic>> func, func2 = null;

            test++;
            S s = new S();
            S s1 = new S();
            unsafe
            {
                S* head = &s;
                s.next = &s1;
                func = async () => { (*(head->next)).value = 1; result++; return head->next->value; };
                func2 = async () => (*(head->next));
            }

            var x = await func();
            if (x != 1)
                result--;
            var xx = await func2();
            if (xx.value != 1)
                result--;
        }
        finally
        {
            Driver.Result = test - result;
            Driver.CompleteSignal.Set();
        }
    }
}

class Driver
{
    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static int Result = -1;
    public static void Main()
    {
        TestCase tc = new TestCase();
        tc.Run();
        CompleteSignal.WaitOne();

        Console.WriteLine(Result);
    }
}";
            var compOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: false, concurrentBuild: false, allowUnsafe: true);
            CompileAndVerify(source, "0", compOptions: compOptions);
        }

        [Fact]
        public void Inference()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

struct Test
{
    public Task<string> Foo
    {
        get { return Task.Run<string>(async () => { await Task.Delay(1); return ""abc""; }); }
    }
}

class TestCase<U>
{
    public static async Task<object> GetValue(object x)
    {
        await Task.Delay(1);
        return x;
    }

    public static T GetValue1<T>(T t) where T : Task<U>
    {
        return t;
    }

    public async void Run()
    {
        int tests = 0;

        Test t = new Test();

        tests++;
        var x1 = await TestCase<string>.GetValue(await t.Foo);
        if (x1 == ""abc"")
            Driver.Count++;

        tests++;
        var x2 = await TestCase<string>.GetValue1(t.Foo);
        if (x2 == ""abc"")
            Driver.Count++;

        Driver.Result = Driver.Count - tests;
        //When test completes, set the flag.
        Driver.CompletedSignal.Set();
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase<int>();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var compOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: false, concurrentBuild: false, allowUnsafe: true);
            CompileAndVerify(source, "0", compOptions: compOptions);
        }

        [Fact]
        public void Operator05()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;


class TestCase
{
    public static int Count = 0;
    public async void Run()
    {
        int tests = 0;
        var x1 = ((await Foo1()) is object);
        var x2 = ((await Foo2()) as string);
        if (x1 == true)
            tests++;
        if (x2 == ""string"")
            tests++;
        Driver.Result = TestCase.Count - tests;
        //When test complete, set the flag.
        Driver.CompletedSignal.Set();
    }

    public async Task<int> Foo1()
    {
        await Task.Delay(1);
        TestCase.Count++;
        int i = 0;
        return i;
    }

    public async Task<object> Foo2()
    {
        await Task.Delay(1);
        TestCase.Count++;
        return ""string"";
    }
}

class Driver
{
    public static int Result = -1;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.Write(Driver.Result);
    }
}";
            var compOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: false, concurrentBuild: false, allowUnsafe: true);
            CompileAndVerify(source, "0", compOptions: compOptions);
        }

        [Fact]
        public void Property21()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;

class Base
{
    public virtual int MyProp { get; private set; }
}

class TestClass : Base
{
    async Task<int> getBaseMyProp() { await Task.Delay(1); return base.MyProp; }

    async public void Run()
    {
        Driver.Result = await getBaseMyProp();

        Driver.CompleteSignal.Set();
    }
}
class Driver
{
    public static AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static void Main()
    {
        TestClass tc = new TestClass();
        tc.Run();

        CompleteSignal.WaitOne();
        Console.WriteLine(Result);
    }

    public static int Result = -1;
}";
            var compOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: false, concurrentBuild: false, allowUnsafe: true);
            CompileAndVerify(source, "0", compOptions: compOptions);
        }

        [Fact]
        public void AnonType32()
        {
            var source =
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            try
            {
                var tmp = await (new { task = Task.Run<string>(async () => { await Task.Delay(1); return """"; }) }).task;
                throw new Exception(tmp);
            }
            catch (Exception ex)
            {
                if (ex.Message == """")
                    Driver.Count++;
            }
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var compOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: false, concurrentBuild: false, allowUnsafe: true);
            CompileAndVerify(source, "0", compOptions: compOptions);
        }

        [Fact]
        public void Init19()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


class ObjInit
{
    public int async;
    public Task t;
    public long l;
}
class TestCase
{
    private T Throw<T>(T i)
    {
        MethodCount++;
        throw new OverflowException();
    }
    private async Task<T> GetVal<T>(T x)
    {
        await Task.Delay(1);
        Throw(x);
        return x;
    }
    public Task<long> MyProperty { get; set; }
    public async void Run()
    {
        int tests = 0;
        Task<int> t = Task.Run<int>(async () => { await Task.Delay(1); throw new FieldAccessException(); return 1; });
        //object type init
        tests++;
        try
        {
            MyProperty = Task.Run<long>(async () => { await Task.Delay(1); throw new DataMisalignedException(); return 1; });
            var obj = new ObjInit()
            {
                async = await t,
                t = GetVal((Task.Run(async () => { await Task.Delay(1); }))),
                l = await MyProperty
            };
            await obj.t;
        }
        catch (FieldAccessException)
        {
            Driver.Count++;
        }
        catch
        {
            Driver.Count--;
        }

        Driver.Result = Driver.Count - tests;
        //When test complete, set the flag.
        Driver.CompletedSignal.Set();
    }

    public int MethodCount = 0;
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var compOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: false, concurrentBuild: false, allowUnsafe: true);
            CompileAndVerify(source, "0", compOptions: compOptions);
        }

        [Fact]
        public void Conformance_OverloadResolution_1Class_Generic_regularMethod05()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;

struct Test<U, V, W>
{
    //Regular methods
    public int Foo(Func<Task<U>> f) { return 1; }
    public int Foo(Func<Task<V>> f) { return 2; }
    public int Foo(Func<Task<W>> f) { return 3; }
}

class TestCase
{
    //where there is a conversion between types (int->double)
    public void Run()
    {
        Test<decimal, string, dynamic> test = new Test<decimal, string, dynamic>();

        int rez = 0;
        // Pick double
        Driver.Tests++;
        rez = test.Foo(async () => { return 1.0; });
        if (rez == 3) Driver.Count++;

        //pick int
        Driver.Tests++;
        rez = test.Foo(async delegate() { return 1; });
        if (rez == 1) Driver.Count++;

        // The best overload is Func<Task<object>>
        Driver.Tests++;
        rez = test.Foo(async () => { return """"; });
        if (rez == 2) Driver.Count++;

        Driver.Tests++;
        rez = test.Foo(async delegate() { return """"; });
        if (rez == 2) Driver.Count++;
    }
}

class Driver
{
    public static int Count = 0;
    public static int Tests = 0;

    static int Main()
    {
        var t = new TestCase();
        t.Run();
        var ret = Driver.Tests - Driver.Count;
        Console.WriteLine(ret);
        return ret;
    }
}";
            var compOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimize: false, concurrentBuild: false, allowUnsafe: true);
            CompileAndVerify(source, "0", compOptions: compOptions);
        }

        [Fact]
        public void Dynamic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<dynamic> F1(dynamic d)
    {
        return await d;
    }

    public static async Task<int> F2(Task<int> d)
    {
        return await d;
    }

    public static async Task<int> Run()
    {
        int a = await F1(Task.Factory.StartNew(() => 21));
        int b = await F2(Task.Factory.StartNew(() => 21));
        return a + b;
    }

    static void Main()
    {
        var t = Run();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";
            CompileAndVerify(source, "42");
        }

        [Fact]
        [WorkItem(638261, "DevDiv")]
        public void Await15()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

struct DynamicClass
{
    public async Task<dynamic> Foo<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task<Task<dynamic>> Bar(int i)
    {
        await Task.Delay(1);
        return Task.Run<dynamic>(async () => { await Task.Delay(1); return i; });
    }
}

class TestCase
{
    public async void Run()
    {
        int tests = 0;
        DynamicClass dc = new DynamicClass();

        dynamic d = 123;

        try
        {
            tests++;  
            var x1 = await dc.Foo("""");
            if (x1 == """") Driver.Count++;

            tests++;
            var x2 = await await dc.Bar(d);
            if (x2 == 123) Driver.Count++;

            tests++;
            var x3 = await await dc.Bar(await dc.Foo(234));
            if (x3 == 234) Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Await01()
        {
            // The legacy compiler allows this; we don't. This kills conformance_await_dynamic_await01.

            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class DynamicMembers
{
    public dynamic Prop { get; set; }
}

class Driver
{
    static void Main()
    {
        DynamicMembers dc2 = new DynamicMembers();
        dc2.Prop = (Func<Task<int>>)(async () => { await Task.Delay(1); return 1; });
        var rez2 = dc2.Prop();
    }
}";
            CompileAndVerify(source, "");
        }

        [Fact]
        public void Await40()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class C1
{
    public async Task<int> Method(int x)
    {
        await Task.Delay(1);
        return x;
    }
}

class C2
{
    public int Status;
    public C2(int x = 5)
    {
        this.Status = x;
    }

    public C2(int x, int y)
    {
        this.Status = x + y;
    }

    public int Bar(int x)
    {
        return x;
    }
}

class TestCase
{
    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            dynamic c = new C1();
            C2 cc = new C2(x: await c.Method(1));
            if (cc.Status == 1)
                Driver.Count++;

            tests++;
            dynamic f = (Func<Task<dynamic>>)(async () => { await Task.Delay(1); return 4; });
            cc = new C2(await c.Method(2), await f());
            if (cc.Status == 6)
                Driver.Count++;

            tests++;
            var x = new C2(2).Bar(await c.Method(1));
            if (cc.Status == 6 && x == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Await43()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;

struct MyClass
{
    public static Task operator *(MyClass c, int x)
    {
        return Task.Run(async delegate
        {
            await Task.Delay(1);
            TestCase.Count++;
        });
    }

    public static Task operator +(MyClass c, long x)
    {
        return Task.Run(async () =>
        {
            await Task.Delay(1);
            TestCase.Count++;
        });
    }
}

class TestCase
{
    public static int Count = 0;
    private int tests;
    public async void Run()
    {
        this.tests = 0;
        dynamic dy = Task.Run<MyClass>(async () => { await Task.Delay(1); return new MyClass(); });

        try
        {
            this.tests++;
            await (await dy * 5);

            this.tests++;
            dynamic d = new MyClass();
            dynamic dd = Task.Run<long>(async () => { await Task.Delay(1); return 1L; });
            await (d + await dd);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            Driver.Result = TestCase.Count - this.tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Await44()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;

class MyClass
{
    public static implicit operator Task(MyClass c)
    {
        return Task.Run(async delegate
        {
            await Task.Delay(1);
            TestCase.Count++;
        });
    }
}
class TestCase
{
    public static int Count = 0;
    private int tests;
    public async void Run()
    {
        this.tests = 0;
        dynamic mc = new MyClass();

        try
        {
            tests++;
            Task t1 = mc;
            await t1;

            tests++;
            dynamic t2 = (Task)mc;
            await t2;
        }
        finally
        {
            Driver.Result = TestCase.Count - this.tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void ThisShouldProbablyCompileToVerifiableCode()
        {
            var source = @"
using System;

class Driver
{
    public static bool Run()
    {
        dynamic dynamicThing = false;
        return true && dynamicThing;
    }

    static void Main()
    {
        Console.WriteLine(Run());
    }
}";
            CompileAndVerify(source, "False");
        }

        [Fact]
        public void Async_Conformance_Awaiting_indexer23()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;

struct MyStruct<T> where T : Task<Func<int>>
{
    T t { get; set; }

    public T this[T index]
    {
        get
        {
            return t;
        }
        set
        {
            t = value;
        }
    }
}
struct TestCase
{
    public static int Count = 0;
    private int tests;
    public async void Run()
    {
        this.tests = 0;
        MyStruct<Task<Func<int>>> ms = new MyStruct<Task<Func<int>>>();

        try
        {
            ms[index: null] = Task.Run<Func<int>>(async () => { await Task.Delay(1); Interlocked.Increment(ref TestCase.Count); return () => (123); });
            this.tests++;
            var x = await ms[index: await Foo(null)];
            if (x() == 123)
                this.tests++;
        }
        finally
        {
            Driver.Result = TestCase.Count - this.tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public async Task<Task<Func<int>>> Foo(Task<Func<int>> d)
    {
        await Task.Delay(1);
        Interlocked.Increment(ref TestCase.Count);
        return d;
    }
}

class Driver
{
    public static int Result = -1;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Async_StackSpill_Argument_Generic04()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class mc<T>
{
    async public System.Threading.Tasks.Task<dynamic> Foo<V>(T t, V u) { await Task.Delay(1); return u; }
}

class Test
{
    static async Task<int> Foo()
    {
        dynamic mc = new mc<string>();
        var rez = await mc.Foo<string>(null, await ((Func<Task<string>>)(async () => { await Task.Delay(1); return ""Test""; }))());
        if (rez == ""Test"")
            return 0;
        return 1;
    }

    static void Main()
    {
        Console.WriteLine(Foo().Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void AsyncStackSpill_assign01()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

struct TestCase
{
    private int val;
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            val = x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5 && val == await GetVal(5))
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Conformance_Exceptions_Async_Await_Names()
        {
            var source = @"
using System;

class TestCase
{
    public void Run()
    {
        Driver.Tests++;
        try
        {
            throw new ArgumentException();
        }
        catch (Exception await)
        {
            if (await is ArgumentException)
                Driver.Count++;
        }


        Driver.Tests++;
        try
        {
            throw new ArgumentException();
        }
        catch (Exception async)
        {
            if (async is ArgumentException)
                Driver.Count++;
        }
    }
}

class Driver
{
    public static int Tests;
    public static int Count;
    static void Main()
    {
        TestCase t = new TestCase();
        t.Run();
        Console.WriteLine(Tests - Count);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void MyTask_08()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

//Implementation of you own async pattern
public class MyTask
{
    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            var myTask = new MyTask();
            var x = await myTask;
            if (x == 123) Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}
public class MyTaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action continuationAction)
    {
    }

    public int GetResult()
    {
        return 123;
    }

    public bool IsCompleted { get { return true; } }
}

public static class Extension
{
    public static MyTaskAwaiter GetAwaiter(this MyTask my)
    {
        return new MyTaskAwaiter();
    }
}
//-------------------------------------

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        new MyTask().Run();

        CompletedSignal.WaitOne();

        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void MyTask_16()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

//Implementation of you own async pattern
public class MyTask
{
    public MyTaskAwaiter GetAwaiter()
    {
        return new MyTaskAwaiter();
    }

    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            var myTask = new MyTask();
            var x = await myTask;
            if (x == 123) Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

public class MyTaskBaseAwaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action continuationAction)
    {
    }

    public int GetResult()
    {
        return 123;
    }

    public bool IsCompleted { get { return true; } }
}

public class MyTaskAwaiter : MyTaskBaseAwaiter
{
}

//-------------------------------------

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        new MyTask().Run();

        CompletedSignal.WaitOne();

        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void InitCollection_04()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

struct PrivateCollection : IEnumerable
{
    public List<int> lst; //public so we can check the values
    public void Add(int x)
    {
        if (lst == null)
            lst = new List<int>();
        lst.Add(x);
    }

    public IEnumerator GetEnumerator()
    {
        return lst as IEnumerator;
    }
}

class TestCase
{
    public async Task<T> GetValue<T>(T x)
    {
        await Task.Delay(1);
        return x;
    }

    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            var myCol = new PrivateCollection() { 
                await GetValue(1),
                await GetValue(2)
            };
            if (myCol.lst[0] == 1 && myCol.lst[1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test completes, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public int Foo { get; set; }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Return07_2()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    unsafe struct S
    {
        public int value;
        public S* next;
    }

    public async void Run()
    {
        int test = 0;
        int result = 0;
        Func<Task<dynamic>> func, func2 = null;

        try
        {
            test++;
            S s = new S();
            S s1 = new S();
            unsafe
            {
                S* head = &s;
                s.next = &s1;
                func = async () => { (*(head->next)).value = 1; result++; return head->next->value; };
                func2 = async () => (*(head->next));
            }

            var x = await func();
            if (x != 1)
                result--;
            var xx = await func2();
            if (xx.value != 1)
                result--;
        }
        finally
        {
            Driver.Result = test - result;
            Driver.CompleteSignal.Set();
        }
    }
}

class Driver
{
    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static int Result = -1;
    public static void Main()
    {
        TestCase tc = new TestCase();
        tc.Run();
        CompleteSignal.WaitOne();

        Console.WriteLine(Result);
    }
}";
            var compOptions= new CSharpCompilationOptions(OutputKind.ConsoleApplication, allowUnsafe: true);
            CompileAndVerify(source, "0", compOptions: compOptions);
        }

        [Fact]
        [WorkItem(625282, "DevDiv")]
        public void Generic05()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public T Foo<T>(T x, T y, int z)
    {
        return x;
    }

    public T GetVal<T>(T t)
    {
        return t;
    }

    public IEnumerable<T> Run<T>(T t)
    {
        dynamic d = GetVal(t);
        yield return Foo(t, d, 3);
    }
}

class Driver
{
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);
    }
}";
            CompileAndVerify(source, new[] { CSharpRef, SystemCoreRef });
        }

        [Fact]
        public void RefExpr()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class MyClass
{
    public int Field;
}

class TestCase
{
    public static int Foo(ref int x, int y)
    {
        return x + y;
    }

    public async Task<int> Run()
    {
        return Foo(
            ref (new MyClass() { Field = 21 }.Field),
            await Task.Factory.StartNew(() => 21));
    }
}

static class Driver
{
    static void Main()
    {
        var t = new TestCase().Run();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";
            CompileAndVerify(source, "42");
        }

        [Fact]
        public void ManagedPointerSpillAssign03()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    class PrivClass
    {
        internal struct ValueT
        {
            public int Field;
        }

        internal ValueT[] arr = new ValueT[3];
    }

    private PrivClass myClass;

    public async void Run()
    {
        int tests = 0;
        this.myClass = new PrivClass();

        try
        {
            tests++;
            this.myClass.arr[0].Field = await GetVal(4);
            if (myClass.arr[0].Field == 4)
                Driver.Count++;

            tests++;
            this.myClass.arr[0].Field += await GetVal(4);
            if (myClass.arr[0].Field == 8)
                Driver.Count++;

            tests++;
            this.myClass.arr[await GetVal(1)].Field += await GetVal(4);
            if (myClass.arr[1].Field == 4)
                Driver.Count++;

            tests++;
            this.myClass.arr[await GetVal(1)].Field++;
            if (myClass.arr[1].Field == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void SacrificialRead()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void F1(ref int x, int y, int z)
    {
        x += y + z;
    }

    static int F0()
    {
        Console.WriteLine(-1);
        return 0;
    }

    static async Task<int> F2()
    {
        int[] x = new int[1] { 21 };
        x = null;
        F1(ref x[0], F0(), await Task.Factory.StartNew(() => 21));
        return x[0];
    }

    public static void Main()
    {
        var t = F2();
        try
        {
            t.Wait();   
        }
        catch(Exception e)
        {
            Console.WriteLine(0);
            return;
        }

        Console.WriteLine(-1);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void RefThisStruct()
        {
            var source = @"
using System;
using System.Threading.Tasks;

struct s1
{
    public int X;

    public async void Foo1()
    {
        Bar(ref this, await Task<int>.FromResult(42));
    }

    public void Foo2()
    {
        Bar(ref this, 42);
    }

    public void Bar(ref s1 x, int y)
    {
        x.X = 42;
    }
}

class c1
{
    public int X;

    public async void Foo1()
    {
        Bar(this, await Task<int>.FromResult(42));
    }

    public void Foo2()
    {
        Bar(this, 42);
    }

    public void Bar(c1 x, int y)
    {
        x.X = 42;
    }
}

class C
{
    public static void Main()
    {
        {
            s1 s;
            s.X = -1;
            s.Foo1();
            Console.WriteLine(s.X);
        }

        {
            s1 s;
            s.X = -1;
            s.Foo2();
            Console.WriteLine(s.X);
        }

        {
            c1 c = new c1();
            c.X = -1;
            c.Foo1();
            Console.WriteLine(c.X);
        }

        {
            c1 c = new c1();
            c.X = -1;
            c.Foo2();
            Console.WriteLine(c.X);
        }
    }
}";
            var expected = @"
-1
42
42
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void AsyncStateMachineIL_Struct_TaskT()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F()
    {
        return await Task.Factory.StartNew(() => 42);
    }

    public static void Main()
    {
        var t = F();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            var c = CompileAndVerify(source, expectedOutput: expected);

            c.VerifyIL("Test.F", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (Test.<F>d__1 V_0,
  System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Create()""
  IL_0007:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_0014:  ldloc.0
  IL_0015:  ldfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  ldloca.s   V_0
  IL_001f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Start<Test.<F>d__1>(ref Test.<F>d__1)""
  IL_0024:  ldloca.s   V_0
  IL_0026:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_002b:  call       ""System.Threading.Tasks.Task<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Task.get""
  IL_0030:  ret
}
");

            c.VerifyIL("Test.<F>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      188 (0xbc)
  .maxstack  3
  .locals init (int V_0,
  int V_1,
  System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
  System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_000e
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0062
  IL_000e:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
  IL_0013:  ldsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_0018:  dup
  IL_0019:  brtrue.s   IL_002e
  IL_001b:  pop
  IL_001c:  ldnull
  IL_001d:  ldftn      ""int Test.<F>b__0(object)""
  IL_0023:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_002e:  callvirt   ""System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)""
  IL_0033:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
  IL_0038:  stloc.2
  IL_0039:  ldloca.s   V_2
  IL_003b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
  IL_0040:  brtrue.s   IL_007e
  IL_0042:  ldarg.0
  IL_0043:  ldc.i4.1
  IL_0044:  dup
  IL_0045:  stloc.0
  IL_0046:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_004b:  ldarg.0
  IL_004c:  ldloc.2
  IL_004d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
  IL_0052:  ldarg.0
  IL_0053:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_0058:  ldloca.s   V_2
  IL_005a:  ldarg.0
  IL_005b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__1)""
  IL_0060:  leave.s    IL_00bb
  IL_0062:  ldarg.0
  IL_0063:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
  IL_0068:  stloc.2
  IL_0069:  ldarg.0
  IL_006a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
  IL_006f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_0075:  ldarg.0
  IL_0076:  ldc.i4.m1
  IL_0077:  dup
  IL_0078:  stloc.0
  IL_0079:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_007e:  ldloca.s   V_2
  IL_0080:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
  IL_0085:  ldloca.s   V_2
  IL_0087:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_008d:  stloc.1
  IL_008e:  leave.s    IL_00a7
}
  catch System.Exception
{
  IL_0090:  stloc.3
  IL_0091:  ldarg.0
  IL_0092:  ldc.i4.s   -2
  IL_0094:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_0099:  ldarg.0
  IL_009a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_009f:  ldloc.3
  IL_00a0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
  IL_00a5:  leave.s    IL_00bb
}
  IL_00a7:  ldarg.0
  IL_00a8:  ldc.i4.s   -2
  IL_00aa:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_00af:  ldarg.0
  IL_00b0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_00b5:  ldloc.1
  IL_00b6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00bb:  ret
}
");

            c.VerifyIL("Test.<F>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_0006:  ldarg.1
  IL_0007:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine)""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void AsyncStateMachineIL_Class_TaskT()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F()
    {
        return await Task.Factory.StartNew(() => 42);
    }

    public static void Main()
    {
        var t = F();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            var c = CompileAndVerify(source, expectedOutput: expected, compOptions: TestOptions.DebugExe, emitPdb: true);

            c.VerifyIL("Test.F", @"
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (Test.<F>d__1 V_0,
  System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> V_1,
  System.Threading.Tasks.Task<int> V_2)
  IL_0000:  newobj     ""Test.<F>d__1..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Create()""
  IL_000c:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.m1
  IL_0013:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_0018:  ldloc.0
  IL_0019:  ldfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  ldloca.s   V_0
  IL_0023:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Start<Test.<F>d__1>(ref Test.<F>d__1)""
  IL_0028:  ldloc.0
  IL_0029:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_002e:  call       ""System.Threading.Tasks.Task<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Task.get""
  IL_0033:  stloc.2
  IL_0034:  ldloc.2
  IL_0035:  ret
}
");

            c.VerifyIL("Test.<F>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      213 (0xd5)
  .maxstack  3
  .locals init (int V_0, //CS$524$0000
           int V_1, //CS$523$0001
           int V_2,
           System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
           int V_4,
           Test.<F>d__1 V_5,
           System.Exception V_6)
 ~IL_0000:  ldarg.0   
  IL_0001:  ldfld      ""int Test.<F>d__1.<>1__state""
  IL_0006:  stloc.0   
  .try
  {
   ~IL_0007:  ldloc.0   
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0   
    IL_000d:  ldc.i4.1  
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0016
    IL_0012:  br.s       IL_0016
    IL_0014:  br.s       IL_0070
   -IL_0016:  nop       
   -IL_0017:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
    IL_001c:  ldsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
    IL_0021:  dup       
    IL_0022:  brtrue.s   IL_0037
    IL_0024:  pop       
    IL_0025:  ldnull    
    IL_0026:  ldftn      ""int Test.<F>b__0(object)""
    IL_002c:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
    IL_0031:  dup       
    IL_0032:  stsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
    IL_0037:  callvirt   ""System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)""
    IL_003c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0041:  stloc.3   
    IL_0042:  ldloca.s   V_3
    IL_0044:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0049:  brtrue.s   IL_008c
    IL_004b:  ldarg.0   
    IL_004c:  ldc.i4.1  
    IL_004d:  dup       
    IL_004e:  stloc.0   
    IL_004f:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_0054:  ldarg.0   
    IL_0055:  ldloc.3   
    IL_0056:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_005b:  ldarg.0   
    IL_005c:  stloc.s    V_5
    IL_005e:  ldarg.0   
    IL_005f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
    IL_0064:  ldloca.s   V_3
    IL_0066:  ldloca.s   V_5
    IL_0068:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__1)""
    IL_006d:  nop       
    IL_006e:  leave.s    IL_00d4
    IL_0070:  ldarg.0   
    IL_0071:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_0076:  stloc.3   
    IL_0077:  ldarg.0   
    IL_0078:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_007d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0083:  ldarg.0   
    IL_0084:  ldc.i4.m1 
    IL_0085:  dup       
    IL_0086:  stloc.0   
    IL_0087:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_008c:  ldloca.s   V_3
    IL_008e:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0093:  stloc.s    V_4
    IL_0095:  ldloca.s   V_3
    IL_0097:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_009d:  ldloc.s    V_4
    IL_009f:  stloc.2   
    IL_00a0:  ldloc.2   
    IL_00a1:  stloc.1   
    IL_00a2:  leave.s    IL_00bf
  }
  catch System.Exception
  {
   ~IL_00a4:  stloc.s    V_6
    IL_00a6:  nop       
    IL_00a7:  ldarg.0   
    IL_00a8:  ldc.i4.s   -2
    IL_00aa:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_00af:  ldarg.0   
    IL_00b0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
    IL_00b5:  ldloc.s    V_6
    IL_00b7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00bc:  nop       
    IL_00bd:  leave.s    IL_00d4
  }
 -IL_00bf:  ldarg.0   
  IL_00c0:  ldc.i4.s   -2
  IL_00c2:  stfld      ""int Test.<F>d__1.<>1__state""
 ~IL_00c7:  ldarg.0   
  IL_00c8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_00cd:  ldloc.1   
  IL_00ce:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00d3:  nop       
  IL_00d4:  ret       

}
", sequencePoints: "Test+<F>d__1.MoveNext");

            c.VerifyIL("Test.<F>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
        }
");
        }

        [Fact]
        public void IL_Task()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task F()
    {
        await Task.Factory.StartNew(() => 42);
        Console.WriteLine(42);
    }

    public static void Main()
    {
        var t = F();
        t.Wait();
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expectedOutput: expected).VerifyIL("Test.F", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (Test.<F>d__1 V_0,
  System.Runtime.CompilerServices.AsyncTaskMethodBuilder V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create()""
  IL_0007:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__1.<>t__builder""
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_0014:  ldloc.0
  IL_0015:  ldfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__1.<>t__builder""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  ldloca.s   V_0
  IL_001f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Test.<F>d__1>(ref Test.<F>d__1)""
  IL_0024:  ldloca.s   V_0
  IL_0026:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__1.<>t__builder""
  IL_002b:  call       ""System.Threading.Tasks.Task System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Task.get""
  IL_0030:  ret
}
").VerifyIL("Test.<F>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      194 (0xc2)
  .maxstack  3
  .locals init (int V_0,
  System.Runtime.CompilerServices.TaskAwaiter<int> V_1,
  System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_000e
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0062
  IL_000e:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
  IL_0013:  ldsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_0018:  dup
  IL_0019:  brtrue.s   IL_002e
  IL_001b:  pop
  IL_001c:  ldnull
  IL_001d:  ldftn      ""int Test.<F>b__0(object)""
  IL_0023:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_002e:  callvirt   ""System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)""
  IL_0033:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
  IL_0038:  stloc.1
  IL_0039:  ldloca.s   V_1
  IL_003b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
  IL_0040:  brtrue.s   IL_007e
  IL_0042:  ldarg.0
  IL_0043:  ldc.i4.1
  IL_0044:  dup
  IL_0045:  stloc.0
  IL_0046:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_004b:  ldarg.0
  IL_004c:  ldloc.1
  IL_004d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
  IL_0052:  ldarg.0
  IL_0053:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__1.<>t__builder""
  IL_0058:  ldloca.s   V_1
  IL_005a:  ldarg.0
  IL_005b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__1)""
  IL_0060:  leave.s    IL_00c1
  IL_0062:  ldarg.0
  IL_0063:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
  IL_0068:  stloc.1
  IL_0069:  ldarg.0
  IL_006a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
  IL_006f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_0075:  ldarg.0
  IL_0076:  ldc.i4.m1
  IL_0077:  dup
  IL_0078:  stloc.0
  IL_0079:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_007e:  ldloca.s   V_1
  IL_0080:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
  IL_0085:  pop
  IL_0086:  ldloca.s   V_1
  IL_0088:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_008e:  ldc.i4.s   42
  IL_0090:  call       ""void System.Console.WriteLine(int)""
  IL_0095:  leave.s    IL_00ae
}
  catch System.Exception
{
  IL_0097:  stloc.2
  IL_0098:  ldarg.0
  IL_0099:  ldc.i4.s   -2
  IL_009b:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_00a0:  ldarg.0
  IL_00a1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__1.<>t__builder""
  IL_00a6:  ldloc.2
  IL_00a7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
  IL_00ac:  leave.s    IL_00c1
}
  IL_00ae:  ldarg.0
  IL_00af:  ldc.i4.s   -2
  IL_00b1:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_00b6:  ldarg.0
  IL_00b7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__1.<>t__builder""
  IL_00bc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c1:  ret
}
");
        }

        [Fact]
        public void IL_Void()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test
{
    static int i = 0;

    public static async void F(AutoResetEvent handle)
    {
        await Task.Factory.StartNew(() => { Test.i = 42; });
        handle.Set();
    }

    public static void Main()
    {
        var handle = new AutoResetEvent(false);
        F(handle);
        handle.WaitOne(1000 * 60);
        Console.WriteLine(i);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expectedOutput: expected).VerifyIL("Test.F", @"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (Test.<F>d__1 V_0,
  System.Runtime.CompilerServices.AsyncVoidMethodBuilder V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldarg.0
  IL_0003:  stfld      ""System.Threading.AutoResetEvent Test.<F>d__1.handle""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Create()""
  IL_000f:  stfld      ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<F>d__1.<>t__builder""
  IL_0014:  ldloca.s   V_0
  IL_0016:  ldc.i4.m1
  IL_0017:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_001c:  ldloc.0
  IL_001d:  ldfld      ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<F>d__1.<>t__builder""
  IL_0022:  stloc.1
  IL_0023:  ldloca.s   V_1
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Test.<F>d__1>(ref Test.<F>d__1)""
  IL_002c:  ret
}
").VerifyIL("Test.<F>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      198 (0xc6)
  .maxstack  3
  .locals init (int V_0,
  System.Runtime.CompilerServices.TaskAwaiter V_1,
  System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_000e
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0062
  IL_000e:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
  IL_0013:  ldsfld     ""System.Action Test.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_0018:  dup
  IL_0019:  brtrue.s   IL_002e
  IL_001b:  pop
  IL_001c:  ldnull
  IL_001d:  ldftn      ""void Test.<F>b__0(object)""
  IL_0023:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Action Test.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_002e:  callvirt   ""System.Threading.Tasks.Task System.Threading.Tasks.TaskFactory.StartNew(System.Action)""
  IL_0033:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
  IL_0038:  stloc.1
  IL_0039:  ldloca.s   V_1
  IL_003b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
  IL_0040:  brtrue.s   IL_007e
  IL_0042:  ldarg.0
  IL_0043:  ldc.i4.1
  IL_0044:  dup
  IL_0045:  stloc.0
  IL_0046:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_004b:  ldarg.0
  IL_004c:  ldloc.1
  IL_004d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter Test.<F>d__1.<>u__$awaiter2""
  IL_0052:  ldarg.0
  IL_0053:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<F>d__1.<>t__builder""
  IL_0058:  ldloca.s   V_1
  IL_005a:  ldarg.0
  IL_005b:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, Test.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter, ref Test.<F>d__1)""
  IL_0060:  leave.s    IL_00c5
  IL_0062:  ldarg.0
  IL_0063:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter Test.<F>d__1.<>u__$awaiter2""
  IL_0068:  stloc.1
  IL_0069:  ldarg.0
  IL_006a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter Test.<F>d__1.<>u__$awaiter2""
  IL_006f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
  IL_0075:  ldarg.0
  IL_0076:  ldc.i4.m1
  IL_0077:  dup
  IL_0078:  stloc.0
  IL_0079:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_007e:  ldloca.s   V_1
  IL_0080:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
  IL_0085:  ldloca.s   V_1
  IL_0087:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
  IL_008d:  ldarg.0
  IL_008e:  ldfld      ""System.Threading.AutoResetEvent Test.<F>d__1.handle""
  IL_0093:  callvirt   ""bool System.Threading.EventWaitHandle.Set()""
  IL_0098:  pop
  IL_0099:  leave.s    IL_00b2
}
  catch System.Exception
{
  IL_009b:  stloc.2
  IL_009c:  ldarg.0
  IL_009d:  ldc.i4.s   -2
  IL_009f:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_00a4:  ldarg.0
  IL_00a5:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<F>d__1.<>t__builder""
  IL_00aa:  ldloc.2
  IL_00ab:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
  IL_00b0:  leave.s    IL_00c5
}
  IL_00b2:  ldarg.0
  IL_00b3:  ldc.i4.s   -2
  IL_00b5:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_00ba:  ldarg.0
  IL_00bb:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<F>d__1.<>t__builder""
  IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_00c5:  ret
}
");
        }

        [Fact]
        [WorkItem(564036, "DevDiv")]
        public void InferFromAsyncLambda()
        {
            var source =
@"using System;
using System.Threading.Tasks;

class Program
{
    public static T CallWithCatch<T>(Func<T> func)
    {
        Console.WriteLine(typeof(T).ToString());
        return func();
    }

    private static async Task LoadTestDataAsync()
    {
        await CallWithCatch(async () => await LoadTestData());
    }

    private static async Task LoadTestData()
    {
        await Task.Run(() => { });
    }

    public static void Main(string[] args)
    {
        Task t = LoadTestDataAsync();
        t.Wait(1000);
    }
}";
            var expected = @"System.Threading.Tasks.Task";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        [WorkItem(620987, "DevDiv")]
        public void PrematureNull()
        {
            var source =
@"using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class Program
{
    public static void Main(string[] args)
    {
        try
        {
            var ar = FindReferencesInDocumentAsync(""Document"");
            ar.Wait(1000 * 60);
            Console.WriteLine(ar.Result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    internal static async Task<string> GetTokensWithIdentifierAsync()
    {
        Console.WriteLine(""in GetTokensWithIdentifierAsync"");
        return ""GetTokensWithIdentifierAsync"";
    }
    protected static async Task<string> FindReferencesInTokensAsync(
        string document,
        string tokens)
    {
        Console.WriteLine(""in FindReferencesInTokensAsync"");
        if (tokens == null) throw new NullReferenceException(""tokens"");
        Console.WriteLine(""tokens were fine"");
        if (document == null) throw new NullReferenceException(""document"");
        Console.WriteLine(""document was fine"");
        return ""FindReferencesInTokensAsync"";
    }
    public static async Task<string> FindReferencesInDocumentAsync(
        string document)
    {
        Console.WriteLine(""in FindReferencesInDocumentAsync"");
        if (document == null) throw new NullReferenceException(""document"");
        var nonAliasReferences = await FindReferencesInTokensAsync(
            document,
            await GetTokensWithIdentifierAsync()
            ).ConfigureAwait(true);
        return ""done!"";
    }
}";
            var expected =
@"in FindReferencesInDocumentAsync
in GetTokensWithIdentifierAsync
in FindReferencesInTokensAsync
tokens were fine
document was fine
done!";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        [WorkItem(621705, "DevDiv")]
        public void GenericAsyncLambda()
        {
            var source =
@"using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class G<T>
{
    T t;
    public G(T t, Func<T, Task<T>> action)
    {
        var tt = action(t);
        var completed = tt.Wait(1000 * 60);
        Debug.Assert(completed);
        this.t = tt.Result;
    }
    public override string ToString()
    {
        return t.ToString();
    }
}

class Test
{
    static G<U> M<U>(U t)
    {
        return new G<U>(t, async x =>
        {
            return await IdentityAsync(x);
        }
        );
    }
    static async Task<V> IdentityAsync<V>(V x)
    {
        await Task.Delay(1);
        return x;
    }

    public static void Main()
    {
        var g = M(12);
        Console.WriteLine(g);
    }
}";
            var expected =
@"12";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        [WorkItem(602028, "DevDiv")]
        public void BetterConversionFromAsyncLambda()
        {
            var source =
@"using System.Threading;
using System.Threading.Tasks;
using System;
class TestCase
{
    public static int Foo(Func<Task<double>> f) { return 12; }
    public static int Foo(Func<Task<object>> f) { return 13; }
    public static void Main()
    {
        Console.WriteLine(Foo(async delegate() { return 14; }));
    }
}
";
            var expected =
@"12";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        [WorkItem(602206, "DevDiv")]
        public void ExtensionAddMethod()
        {
            var source =
@"using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
static public class Extension
{
    static public void Add<T>(this Stack<T> stack, T item)
    {
        Console.WriteLine(""Add "" + item.ToString());
        stack.Push(item);
    }
}
class TestCase
{
    AutoResetEvent handle = new AutoResetEvent(false);
    private async Task<T> GetVal<T>(T x)
    {
        await Task.Delay(1);
        Console.WriteLine(""GetVal "" + x.ToString());
        return x;
    }
    public async void Run()
    {
        try
        {
            Stack<int> stack = new Stack<int>() { await GetVal(1), 2, 3 }; // CS0117
        }
        finally
        {
            handle.Set();
        }
    }
    public static void Main(string[] args)
    {
        var tc = new TestCase();
        tc.Run();
        tc.handle.WaitOne(1000 * 60);
    }
}";
            var expected =
@"GetVal 1
Add 1
Add 2
Add 3";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        [WorkItem(748527, "DevDiv")]
        public void Bug748527()
        {
            var source = @"using System.Threading.Tasks;
using System;
namespace A
{
    public struct TestClass
    {
        async public System.Threading.Tasks.Task<int> IntRet(int IntI)
        {
            return  await ((Func<Task<int>>)(async ()=> { await Task.Yield(); return IntI ; } ))() ;
        }
    }
    public class B
    {
        async public static System.Threading.Tasks.Task<int> MainMethod()
        {
            int MyRet = 0;
            TestClass TC = new TestClass();
            if ((  await ((Func<Task<int>>)(async ()=> { await Task.Yield(); return (await(new TestClass().IntRet( await ((Func<Task<int>>)(async ()=> { await Task.Yield(); return 3 ; } ))() ))) ; } ))()  ) !=  await ((Func<Task<int>>)(async ()=> { await Task.Yield(); return 3 ; } ))() )
            {
                MyRet = 1;
            }
            return  await ((Func<Task<int>>)(async ()=> {await Task.Yield(); return MyRet;}))();
        }
        static void Main ()
        {
            MainMethod();
            return;
        }
    }
}";
            var expectedOutput = "";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(602216, "DevDiv")]
        public void AsyncMethodOnlyWritesToEnclosingStruct()
        {
            var source =
@"public struct GenC<T> where T : struct
{
    public T? valueN;
    public async void Test(T t)
    {
        valueN = t;
    }
}
public class Test
{
    public static void Main()
    {
        int test = 12;
        GenC<int> _int = new GenC<int>();
        _int.Test(test);
        System.Console.WriteLine(_int.valueN ?? 1);
    }
}";
            var expected =
@"1";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        [WorkItem(602246, "DevDiv")]
        public void Bug602246()
        {
            var source =
@"using System;
using System.Threading.Tasks;

public class TestCase
{
    public static async Task<T> Run<T>(T t)
    {
        await Task.Delay(1);
        Func<Func<Task<T>>, Task<T>> f = async (x) => { return await x(); };
        var rez = await f(async () => { await Task.Delay(1); return t; });
        return rez;
    }
    public static void Main()
    {
        var t = TestCase.Run<int>(12);
        if (!t.Wait(1000 * 60)) throw new Exception();
        Console.Write(t.Result);
    }
}";
            var expected =
@"12";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [WorkItem(628654, "DevDiv")]
        [Fact]
        public void AsyncWithDynamic01()
        {
            var source = @"
using System;
using System.Threading.Tasks;
 
class Program
{
    static void Main()
    {
        Foo<int>().Wait();
    }
 
    static async Task Foo<T>()
    {
        Console.WriteLine(""{0}"" as dynamic, await Task.FromResult(new T[] { }));
    }
}";
            var expected = @"
System.Int32[]
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [WorkItem(640282, "DevDiv")]
        [Fact]
        public void CustomAsyncWithDynamic01()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class MyTask
{
    public dynamic GetAwaiter()
    {
        return new MyTaskAwaiter<Action>();
    }

    public async void Run<T>()
    {
        int tests = 0;

        tests++;
        dynamic myTask = new MyTask();
        var x = await myTask;
        if (x == 123) Driver.Count++;

        Driver.Result = Driver.Count - tests;
        //When test complete, set the flag.
        Driver.CompletedSignal.Set();
    }
}
class MyTaskAwaiter<U>
{
    public void OnCompleted(U continuationAction)
    {
    }

    public int GetResult()
    {
        return 123;
    }

    public dynamic IsCompleted { get { return true; } }
}
class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    public static void Main()
    {
        new MyTask().Run<int>();

        CompletedSignal.WaitOne();

        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"0";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [WorkItem(840843, "DevDiv")]
        [Fact]
        public void MissingAsyncMethodBuilder()
        {
            var source = @"
class C
{
    async void M() {}
    }
";

            var comp = CSharpTestBaseBase.CreateCompilation(source, new[] { MscorlibRef }, OptionsDll); // NOTE: 4.0, not 4.5, so it's missing the async helpers.

            // CONSIDER: It would be nice if we didn't squiggle the whole method body, but this is a corner case.
            comp.VerifyEmitDiagnostics(
    // (4,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M"),
    // (5,5): error CS0518: Predefined type 'System.Runtime.CompilerServices.AsyncVoidMethodBuilder' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"{}").WithArguments("System.Runtime.CompilerServices.AsyncVoidMethodBuilder"),
    // (5,5): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{}").WithArguments("System.Runtime.CompilerServices.AsyncVoidMethodBuilder", "SetException"));
        }


        [Fact]
        [WorkItem(868822, "DevDiv")]
        public void AsyncDelegates()
        {
            var source =
@"using System;
using System.Threading.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
            test1();
            test2();
        }

        static void test1()
        {
            Invoke(async delegate
            {
                if (0.ToString().Length == 0)
                {
                    await Task.Yield();                        
                }
                else
                {
                    System.Console.WriteLine(0.ToString());
                }
            });
        }

        static string test2()
        {
            return Invoke(async delegate
            {
                if (0.ToString().Length == 0)
                {
                    await Task.Yield();
                    return 1.ToString();
                }
                else
                {
                    System.Console.WriteLine(2.ToString());
                    return null;
                }
            });
        }

        static void Invoke(Action method)
        {
            method();
        }

        static void Invoke(Func<Task> method)
        {
            method().Wait();
        }

        static TResult Invoke<TResult>(Func<TResult> method)
        {
            return method();
        }

        internal static TResult Invoke<TResult>(Func<Task<TResult>> method)
        {
            if (method != null)
            {
                return Invoke1(async delegate
                {
                    await Task.Yield();
                    return await method();
                });
            }

            return default(TResult);
        }

        internal static TResult Invoke1<TResult>(Func<Task<TResult>> method)
        {
            return method().Result;
        }
    }

";
            var expected =
@"0
2";
            CompileAndVerify(source, expectedOutput: expected);
        }
    }
}
