﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
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
        private static CSharpCompilation CreateCompilation(string source, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions options = null)
        {
            options = options ?? TestOptions.ReleaseExe;

            IEnumerable<MetadataReference> asyncRefs = new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef };
            references = (references != null) ? references.Concat(asyncRefs) : asyncRefs;

            return CreateCompilationWithMscorlib45(source, options: options, references: references);
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions options = null, Verification verify = Verification.Passes)
        {
            var compilation = CreateCompilation(source, references: references, options: options);
            return base.CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: verify);
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

            options = TestOptions.ReleaseExe;
            Assert.False(options.EnableEditAndContinue);

            CompileAndVerify(c.WithOptions(options), symbolValidator: module =>
            {
                var stateMachine = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test").GetMember<NamedTypeSymbol>("<F>d__0");
                Assert.Equal(TypeKind.Struct, stateMachine.TypeKind);
            }, expectedOutput: "123");

            options = TestOptions.ReleaseDebugExe;
            Assert.False(options.EnableEditAndContinue);

            CompileAndVerify(c.WithOptions(options), symbolValidator: module =>
            {
                var stateMachine = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test").GetMember<NamedTypeSymbol>("<F>d__0");
                Assert.Equal(TypeKind.Struct, stateMachine.TypeKind);
            }, expectedOutput: "123");

            options = TestOptions.DebugExe;
            Assert.True(options.EnableEditAndContinue);

            CompileAndVerify(c.WithOptions(options), symbolValidator: module =>
            {
                var stateMachine = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test").GetMember<NamedTypeSymbol>("<F>d__0");
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
    public static T Goo<T>(T t)
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
            int x1 = Goo(await Bar(4));
            Task<int> t = Bar(5);
            int x2 = Goo(await t);
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
        public void AwaitInDelegateConstructor()
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
            CompileAndVerify(source, expectedOutput: "0");
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
    public async Task TriggerEvent(T p)
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
            await ms.TriggerEvent(str);
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
            CompileAndVerify(source, expectedOutput: "0", options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails);
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
    public Task<string> Goo
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
        var x1 = await TestCase<string>.GetValue(await t.Goo);
        if (x1 == ""abc"")
            Driver.Count++;

        tests++;
        var x2 = await TestCase<string>.GetValue1(t.Goo);
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
            CompileAndVerify(source, expectedOutput: "0", options: TestOptions.UnsafeDebugExe, verify: Verification.Passes);
        }

        [Fact]
        public void IsAndAsOperators()
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
        var x1 = ((await Goo1()) is object);
        var x2 = ((await Goo2()) as string);
        if (x1 == true)
            tests++;
        if (x2 == ""string"")
            tests++;
        Driver.Result = TestCase.Count - tests;
        //When test complete, set the flag.
        Driver.CompletedSignal.Set();
    }

    public async Task<int> Goo1()
    {
        await Task.Delay(1);
        TestCase.Count++;
        int i = 0;
        return i;
    }

    public async Task<object> Goo2()
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
            CompileAndVerify(source, expectedOutput: "0", options: TestOptions.UnsafeDebugExe, verify: Verification.Passes);
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
            CompileAndVerify(source, expectedOutput: "0", options: TestOptions.UnsafeDebugExe, verify: Verification.Passes);
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
            CompileAndVerify(source, "0", options: TestOptions.UnsafeDebugExe);
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
            CompileAndVerify(source, "0", options: TestOptions.UnsafeDebugExe);
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
    public int Goo(Func<Task<U>> f) { return 1; }
    public int Goo(Func<Task<V>> f) { return 2; }
    public int Goo(Func<Task<W>> f) { return 3; }
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
        rez = test.Goo(async () => { return 1.0; });
        if (rez == 3) Driver.Count++;

        //pick int
        Driver.Tests++;
        rez = test.Goo(async delegate() { return 1; });
        if (rez == 1) Driver.Count++;

        // The best overload is Func<Task<object>>
        Driver.Tests++;
        rez = test.Goo(async () => { return """"; });
        if (rez == 2) Driver.Count++;

        Driver.Tests++;
        rez = test.Goo(async delegate() { return """"; });
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
            CompileAndVerify(source, "0", options: TestOptions.UnsafeDebugExe);
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
        [WorkItem(638261, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638261")]
        public void Await15()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

struct DynamicClass
{
    public async Task<dynamic> Goo<T>(T t)
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
            var x1 = await dc.Goo("""");
            if (x1 == """") Driver.Count++;

            tests++;
            var x2 = await await dc.Bar(d);
            if (x2 == 123) Driver.Count++;

            tests++;
            var x3 = await await dc.Bar(await dc.Goo(234));
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
            var x = await ms[index: await Goo(null)];
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

    public async Task<Task<Func<int>>> Goo(Task<Func<int>> d)
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
        [WorkItem(625282, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/625282")]
        public void Generic05()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public T Goo<T>(T x, T y, int z)
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
        yield return Goo(t, d, 3);
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
            CompileAndVerifyWithMscorlib40(source, new[] { CSharpRef, SystemCoreRef });
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
  .locals init (Test.<F>d__0 V_0,
                System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Create()""
  IL_0007:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      ""int Test.<F>d__0.<>1__state""
  IL_0014:  ldloc.0
  IL_0015:  ldfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  ldloca.s   V_0
  IL_001f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Start<Test.<F>d__0>(ref Test.<F>d__0)""
  IL_0024:  ldloca.s   V_0
  IL_0026:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_002b:  call       ""System.Threading.Tasks.Task<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Task.get""
  IL_0030:  ret
}
");

            c.VerifyIL("Test.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      180 (0xb4)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0062
    IL_000a:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
    IL_000f:  ldsfld     ""System.Func<int> Test.<>c.<>9__0_0""
    IL_0014:  dup
    IL_0015:  brtrue.s   IL_002e
    IL_0017:  pop
    IL_0018:  ldsfld     ""Test.<>c Test.<>c.<>9""
    IL_001d:  ldftn      ""int Test.<>c.<F>b__0_0()""
    IL_0023:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
    IL_0028:  dup
    IL_0029:  stsfld     ""System.Func<int> Test.<>c.<>9__0_0""
    IL_002e:  callvirt   ""System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)""
    IL_0033:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0038:  stloc.2
    IL_0039:  ldloca.s   V_2
    IL_003b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0040:  brtrue.s   IL_007e
    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.0
    IL_0044:  dup
    IL_0045:  stloc.0
    IL_0046:  stfld      ""int Test.<F>d__0.<>1__state""
    IL_004b:  ldarg.0
    IL_004c:  ldloc.2
    IL_004d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
    IL_0058:  ldloca.s   V_2
    IL_005a:  ldarg.0
    IL_005b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__0)""
    IL_0060:  leave.s    IL_00b3
    IL_0062:  ldarg.0
    IL_0063:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_0068:  stloc.2
    IL_0069:  ldarg.0
    IL_006a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_006f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0075:  ldarg.0
    IL_0076:  ldc.i4.m1
    IL_0077:  dup
    IL_0078:  stloc.0
    IL_0079:  stfld      ""int Test.<F>d__0.<>1__state""
    IL_007e:  ldloca.s   V_2
    IL_0080:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0085:  stloc.1
    IL_0086:  leave.s    IL_009f
  }
  catch System.Exception
  {
    IL_0088:  stloc.3
    IL_0089:  ldarg.0
    IL_008a:  ldc.i4.s   -2
    IL_008c:  stfld      ""int Test.<F>d__0.<>1__state""
    IL_0091:  ldarg.0
    IL_0092:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
    IL_0097:  ldloc.3
    IL_0098:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_009d:  leave.s    IL_00b3
  }
  IL_009f:  ldarg.0
  IL_00a0:  ldc.i4.s   -2
  IL_00a2:  stfld      ""int Test.<F>d__0.<>1__state""
  IL_00a7:  ldarg.0
  IL_00a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_00ad:  ldloc.1
  IL_00ae:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b3:  ret
}
");

            c.VerifyIL("Test.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_0006:  ldarg.1
  IL_0007:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine)""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void AsyncStateMachineIL_Struct_TaskT_A()
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
            var c = CompileAndVerify(source, options: TestOptions.ReleaseDebugExe, expectedOutput: expected);

            c.VerifyIL("Test.F", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (Test.<F>d__0 V_0,
                System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Create()""
  IL_0007:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      ""int Test.<F>d__0.<>1__state""
  IL_0014:  ldloc.0
  IL_0015:  ldfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  ldloca.s   V_0
  IL_001f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Start<Test.<F>d__0>(ref Test.<F>d__0)""
  IL_0024:  ldloca.s   V_0
  IL_0026:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_002b:  call       ""System.Threading.Tasks.Task<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Task.get""
  IL_0030:  ret
}
");

            c.VerifyIL("Test.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      184 (0xb8)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0062
    IL_000a:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
    IL_000f:  ldsfld     ""System.Func<int> Test.<>c.<>9__0_0""
    IL_0014:  dup
    IL_0015:  brtrue.s   IL_002e
    IL_0017:  pop
    IL_0018:  ldsfld     ""Test.<>c Test.<>c.<>9""
    IL_001d:  ldftn      ""int Test.<>c.<F>b__0_0()""
    IL_0023:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
    IL_0028:  dup
    IL_0029:  stsfld     ""System.Func<int> Test.<>c.<>9__0_0""
    IL_002e:  callvirt   ""System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)""
    IL_0033:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0038:  stloc.3
    IL_0039:  ldloca.s   V_3
    IL_003b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0040:  brtrue.s   IL_007e
    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.0
    IL_0044:  dup
    IL_0045:  stloc.0
    IL_0046:  stfld      ""int Test.<F>d__0.<>1__state""
    IL_004b:  ldarg.0
    IL_004c:  ldloc.3
    IL_004d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
    IL_0058:  ldloca.s   V_3
    IL_005a:  ldarg.0
    IL_005b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__0)""
    IL_0060:  leave.s    IL_00b7
    IL_0062:  ldarg.0
    IL_0063:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_0068:  stloc.3
    IL_0069:  ldarg.0
    IL_006a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_006f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0075:  ldarg.0
    IL_0076:  ldc.i4.m1
    IL_0077:  dup
    IL_0078:  stloc.0
    IL_0079:  stfld      ""int Test.<F>d__0.<>1__state""
    IL_007e:  ldloca.s   V_3
    IL_0080:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0085:  stloc.2
    IL_0086:  ldloc.2
    IL_0087:  stloc.1
    IL_0088:  leave.s    IL_00a3
  }
  catch System.Exception
  {
    IL_008a:  stloc.s    V_4
    IL_008c:  ldarg.0
    IL_008d:  ldc.i4.s   -2
    IL_008f:  stfld      ""int Test.<F>d__0.<>1__state""
    IL_0094:  ldarg.0
    IL_0095:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
    IL_009a:  ldloc.s    V_4
    IL_009c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a1:  leave.s    IL_00b7
  }
  IL_00a3:  ldarg.0
  IL_00a4:  ldc.i4.s   -2
  IL_00a6:  stfld      ""int Test.<F>d__0.<>1__state""
  IL_00ab:  ldarg.0
  IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_00b1:  ldloc.1
  IL_00b2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b7:  ret
}
");

            c.VerifyIL("Test.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
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
            var c = CompileAndVerify(source, expectedOutput: expected, options: TestOptions.DebugExe);

            c.VerifyIL("Test.F", @"
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (Test.<F>d__0 V_0,
                System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> V_1)
  IL_0000:  newobj     ""Test.<F>d__0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Create()""
  IL_000c:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.m1
  IL_0013:  stfld      ""int Test.<F>d__0.<>1__state""
  IL_0018:  ldloc.0
  IL_0019:  ldfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  ldloca.s   V_0
  IL_0023:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Start<Test.<F>d__0>(ref Test.<F>d__0)""
  IL_0028:  ldloc.0
  IL_0029:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_002e:  call       ""System.Threading.Tasks.Task<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Task.get""
  IL_0033:  ret
}
");

            c.VerifyIL("Test.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      205 (0xcd)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                Test.<F>d__0 V_3,
                System.Exception V_4)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_006b
   -IL_000e:  nop
   -IL_000f:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
    IL_0014:  ldsfld     ""System.Func<int> Test.<>c.<>9__0_0""
    IL_0019:  dup
    IL_001a:  brtrue.s   IL_0033
    IL_001c:  pop
    IL_001d:  ldsfld     ""Test.<>c Test.<>c.<>9""
    IL_0022:  ldftn      ""int Test.<>c.<F>b__0_0()""
    IL_0028:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
    IL_002d:  dup
    IL_002e:  stsfld     ""System.Func<int> Test.<>c.<>9__0_0""
    IL_0033:  callvirt   ""System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)""
    IL_0038:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_003d:  stloc.2
   ~IL_003e:  ldloca.s   V_2
    IL_0040:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0045:  brtrue.s   IL_0087
    IL_0047:  ldarg.0
    IL_0048:  ldc.i4.0
    IL_0049:  dup
    IL_004a:  stloc.0
    IL_004b:  stfld      ""int Test.<F>d__0.<>1__state""
   <IL_0050:  ldarg.0
    IL_0051:  ldloc.2
    IL_0052:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_0057:  ldarg.0
    IL_0058:  stloc.3
    IL_0059:  ldarg.0
    IL_005a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
    IL_005f:  ldloca.s   V_2
    IL_0061:  ldloca.s   V_3
    IL_0063:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__0)""
    IL_0068:  nop
    IL_0069:  leave.s    IL_00cc
   >IL_006b:  ldarg.0
    IL_006c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_0071:  stloc.2
    IL_0072:  ldarg.0
    IL_0073:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_0078:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_007e:  ldarg.0
    IL_007f:  ldc.i4.m1
    IL_0080:  dup
    IL_0081:  stloc.0
    IL_0082:  stfld      ""int Test.<F>d__0.<>1__state""
    IL_0087:  ldarg.0
    IL_0088:  ldloca.s   V_2
    IL_008a:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_008f:  stfld      ""int Test.<F>d__0.<>s__1""
    IL_0094:  ldarg.0
    IL_0095:  ldfld      ""int Test.<F>d__0.<>s__1""
    IL_009a:  stloc.1
    IL_009b:  leave.s    IL_00b7
  }
  catch System.Exception
  {
   ~IL_009d:  stloc.s    V_4
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.s   -2
    IL_00a2:  stfld      ""int Test.<F>d__0.<>1__state""
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
    IL_00ad:  ldloc.s    V_4
    IL_00af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00b4:  nop
    IL_00b5:  leave.s    IL_00cc
  }
 -IL_00b7:  ldarg.0
  IL_00b8:  ldc.i4.s   -2
  IL_00ba:  stfld      ""int Test.<F>d__0.<>1__state""
 ~IL_00bf:  ldarg.0
  IL_00c0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__0.<>t__builder""
  IL_00c5:  ldloc.1
  IL_00c6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00cb:  nop
  IL_00cc:  ret
}", sequencePoints: "Test+<F>d__0.MoveNext");

            c.VerifyIL("Test.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine", @"
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
            var c = CompileAndVerify(source, expectedOutput: expected);

            c.VerifyIL("Test.F", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (Test.<F>d__0 V_0,
                System.Runtime.CompilerServices.AsyncTaskMethodBuilder V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create()""
  IL_0007:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__0.<>t__builder""
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      ""int Test.<F>d__0.<>1__state""
  IL_0014:  ldloc.0
  IL_0015:  ldfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__0.<>t__builder""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  ldloca.s   V_0
  IL_001f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Test.<F>d__0>(ref Test.<F>d__0)""
  IL_0024:  ldloca.s   V_0
  IL_0026:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__0.<>t__builder""
  IL_002b:  call       ""System.Threading.Tasks.Task System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Task.get""
  IL_0030:  ret
}
");
            c.VerifyIL("Test.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      186 (0xba)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0062
    IL_000a:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
    IL_000f:  ldsfld     ""System.Func<int> Test.<>c.<>9__0_0""
    IL_0014:  dup
    IL_0015:  brtrue.s   IL_002e
    IL_0017:  pop
    IL_0018:  ldsfld     ""Test.<>c Test.<>c.<>9""
    IL_001d:  ldftn      ""int Test.<>c.<F>b__0_0()""
    IL_0023:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
    IL_0028:  dup
    IL_0029:  stsfld     ""System.Func<int> Test.<>c.<>9__0_0""
    IL_002e:  callvirt   ""System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)""
    IL_0033:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0038:  stloc.1
    IL_0039:  ldloca.s   V_1
    IL_003b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0040:  brtrue.s   IL_007e
    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.0
    IL_0044:  dup
    IL_0045:  stloc.0
    IL_0046:  stfld      ""int Test.<F>d__0.<>1__state""
    IL_004b:  ldarg.0
    IL_004c:  ldloc.1
    IL_004d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__0.<>t__builder""
    IL_0058:  ldloca.s   V_1
    IL_005a:  ldarg.0
    IL_005b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__0)""
    IL_0060:  leave.s    IL_00b9
    IL_0062:  ldarg.0
    IL_0063:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_0068:  stloc.1
    IL_0069:  ldarg.0
    IL_006a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__0.<>u__1""
    IL_006f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0075:  ldarg.0
    IL_0076:  ldc.i4.m1
    IL_0077:  dup
    IL_0078:  stloc.0
    IL_0079:  stfld      ""int Test.<F>d__0.<>1__state""
    IL_007e:  ldloca.s   V_1
    IL_0080:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0085:  pop
    IL_0086:  ldc.i4.s   42
    IL_0088:  call       ""void System.Console.WriteLine(int)""
    IL_008d:  leave.s    IL_00a6
  }
  catch System.Exception
  {
    IL_008f:  stloc.2
    IL_0090:  ldarg.0
    IL_0091:  ldc.i4.s   -2
    IL_0093:  stfld      ""int Test.<F>d__0.<>1__state""
    IL_0098:  ldarg.0
    IL_0099:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__0.<>t__builder""
    IL_009e:  ldloc.2
    IL_009f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a4:  leave.s    IL_00b9
  }
  IL_00a6:  ldarg.0
  IL_00a7:  ldc.i4.s   -2
  IL_00a9:  stfld      ""int Test.<F>d__0.<>1__state""
  IL_00ae:  ldarg.0
  IL_00af:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__0.<>t__builder""
  IL_00b4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b9:  ret
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
  // Code size      190 (0xbe)
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
    IL_0008:  brfalse.s  IL_0062
    IL_000a:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
    IL_000f:  ldsfld     ""System.Action Test.<>c.<>9__1_0""
    IL_0014:  dup
    IL_0015:  brtrue.s   IL_002e
    IL_0017:  pop
    IL_0018:  ldsfld     ""Test.<>c Test.<>c.<>9""
    IL_001d:  ldftn      ""void Test.<>c.<F>b__1_0()""
    IL_0023:  newobj     ""System.Action..ctor(object, System.IntPtr)""
    IL_0028:  dup
    IL_0029:  stsfld     ""System.Action Test.<>c.<>9__1_0""
    IL_002e:  callvirt   ""System.Threading.Tasks.Task System.Threading.Tasks.TaskFactory.StartNew(System.Action)""
    IL_0033:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0038:  stloc.1
    IL_0039:  ldloca.s   V_1
    IL_003b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0040:  brtrue.s   IL_007e
    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.0
    IL_0044:  dup
    IL_0045:  stloc.0
    IL_0046:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_004b:  ldarg.0
    IL_004c:  ldloc.1
    IL_004d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter Test.<F>d__1.<>u__1""
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<F>d__1.<>t__builder""
    IL_0058:  ldloca.s   V_1
    IL_005a:  ldarg.0
    IL_005b:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, Test.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter, ref Test.<F>d__1)""
    IL_0060:  leave.s    IL_00bd
    IL_0062:  ldarg.0
    IL_0063:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter Test.<F>d__1.<>u__1""
    IL_0068:  stloc.1
    IL_0069:  ldarg.0
    IL_006a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter Test.<F>d__1.<>u__1""
    IL_006f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0075:  ldarg.0
    IL_0076:  ldc.i4.m1
    IL_0077:  dup
    IL_0078:  stloc.0
    IL_0079:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_007e:  ldloca.s   V_1
    IL_0080:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0085:  ldarg.0
    IL_0086:  ldfld      ""System.Threading.AutoResetEvent Test.<F>d__1.handle""
    IL_008b:  callvirt   ""bool System.Threading.EventWaitHandle.Set()""
    IL_0090:  pop
    IL_0091:  leave.s    IL_00aa
  }
  catch System.Exception
  {
    IL_0093:  stloc.2
    IL_0094:  ldarg.0
    IL_0095:  ldc.i4.s   -2
    IL_0097:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_009c:  ldarg.0
    IL_009d:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<F>d__1.<>t__builder""
    IL_00a2:  ldloc.2
    IL_00a3:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_00a8:  leave.s    IL_00bd
  }
  IL_00aa:  ldarg.0
  IL_00ab:  ldc.i4.s   -2
  IL_00ad:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_00b2:  ldarg.0
  IL_00b3:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<F>d__1.<>t__builder""
  IL_00b8:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_00bd:  ret
}
");
        }

        [Fact]
        [WorkItem(564036, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/564036")]
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
        [WorkItem(620987, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/620987")]
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
        [WorkItem(621705, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/621705")]
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
        [WorkItem(602028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602028")]
        public void BetterConversionFromAsyncLambda()
        {
            var source =
@"using System.Threading;
using System.Threading.Tasks;
using System;
class TestCase
{
    public static int Goo(Func<Task<double>> f) { return 12; }
    public static int Goo(Func<Task<object>> f) { return 13; }
    public static void Main()
    {
        Console.WriteLine(Goo(async delegate() { return 14; }));
    }
}
";
            var expected =
@"12";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        [WorkItem(602206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602206")]
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
        [WorkItem(748527, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/748527")]
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
        [WorkItem(602216, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602216")]
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
        [WorkItem(602246, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602246")]
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

        [WorkItem(628654, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/628654")]
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
        Goo<int>().Wait();
    }
 
    static async Task Goo<T>()
    {
        Console.WriteLine(""{0}"" as dynamic, await Task.FromResult(new T[] { }));
    }
}";
            var expected = @"
System.Int32[]
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [WorkItem(640282, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/640282")]
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

        [WorkItem(840843, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/840843")]
        [Fact]
        public void MissingAsyncVoidMethodBuilder()
        {
            var source = @"
class C
{
    async void M() {}
}
";

            var comp = CSharpTestBase.CreateEmptyCompilation(source, new[] { MscorlibRef }, TestOptions.ReleaseDll); // NOTE: 4.0, not 4.5, so it's missing the async helpers.

            // CONSIDER: It would be nice if we didn't squiggle the whole method body, but this is a corner case.
            comp.VerifyEmitDiagnostics(
                // (4,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async void M() {}
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 16),
                // (4,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.AsyncVoidMethodBuilder' is not defined or imported
                //     async void M() {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "{}").WithArguments("System.Runtime.CompilerServices.AsyncVoidMethodBuilder").WithLocation(4, 20),
                // (4,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Create'
                //     async void M() {}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{}").WithArguments("System.Runtime.CompilerServices.AsyncVoidMethodBuilder", "Create").WithLocation(4, 20),
                // (4,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext'
                //     async void M() {}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{}").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "MoveNext").WithLocation(4, 20),
                // (4,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine'
                //     async void M() {}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{}").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "SetStateMachine").WithLocation(4, 20));
        }

        [Fact]
        public void MissingAsyncTaskMethodBuilder()
        {
            var source =
@"using System.Threading.Tasks;
class C
{
    async Task M() {}
}";
            var comp = CSharpTestBase.CreateEmptyCompilation(source, new[] { MscorlibRef }, TestOptions.ReleaseDll); // NOTE: 4.0, not 4.5, so it's missing the async helpers.
            comp.VerifyEmitDiagnostics(
                // (4,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async Task M() {}
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 16),
                // (4,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.AsyncTaskMethodBuilder' is not defined or imported
                //     async Task M() {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "{}").WithArguments("System.Runtime.CompilerServices.AsyncTaskMethodBuilder").WithLocation(4, 20),
                // (4,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create'
                //     async Task M() {}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{}").WithArguments("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", "Create").WithLocation(4, 20),
                // (4,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Task'
                //     async Task M() {}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{}").WithArguments("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", "Task").WithLocation(4, 20),
                // (4,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext'
                //     async Task M() {}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{}").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "MoveNext").WithLocation(4, 20),
                // (4,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine'
                //     async Task M() {}
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{}").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "SetStateMachine").WithLocation(4, 20));
        }

        [Fact]
        public void MissingAsyncTaskMethodBuilder_T()
        {
            var source =
@"using System.Threading.Tasks;
class C
{
    async Task<int> F() => 3;
}";
            var comp = CSharpTestBase.CreateEmptyCompilation(source, new[] { MscorlibRef }, TestOptions.ReleaseDll); // NOTE: 4.0, not 4.5, so it's missing the async helpers.
            comp.VerifyEmitDiagnostics(
                // (4,21): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async Task<int> F() => 3;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F").WithLocation(4, 21),
                // (4,25): error CS0518: Predefined type 'System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1' is not defined or imported
                //     async Task<int> F() => 3;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "=> 3").WithArguments("System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1").WithLocation(4, 25),
                // (4,25): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.Create'
                //     async Task<int> F() => 3;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> 3").WithArguments("System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1", "Create").WithLocation(4, 25),
                // (4,25): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.Task'
                //     async Task<int> F() => 3;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> 3").WithArguments("System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1", "Task").WithLocation(4, 25),
                // (4,25): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext'
                //     async Task<int> F() => 3;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> 3").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "MoveNext").WithLocation(4, 25),
                // (4,25): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine'
                //     async Task<int> F() => 3;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> 3").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "SetStateMachine").WithLocation(4, 25));
        }

        private static string AsyncBuilderCode(string builderTypeName, string tasklikeTypeName, string genericTypeParameter = null, bool isStruct = false)
        {
            string ofT = genericTypeParameter == null ? "" : "<" + genericTypeParameter + ">";
            return $@"
public {(isStruct ? "struct" : "class")} {builderTypeName}{ofT}
{{
    public static {builderTypeName}{ofT} Create() => default({builderTypeName}{ofT});
    public {tasklikeTypeName}{ofT} Task {{ get; }}
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {{ }}
    public void SetException(System.Exception exception) {{ }}
    public void SetResult({(genericTypeParameter == null ? "" : genericTypeParameter + " result")}) {{ }}
    public void SetStateMachine(IAsyncStateMachine stateMachine) {{ }}
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {{ }}
}}
";
        }

        [Fact]
        public void PresentAsyncTasklikeBuilderMethod()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    async ValueTask f() { await (Task)null; }
    async ValueTask<int> g() { await (Task)null; return 1; }
}
[AsyncMethodBuilder(typeof(ValueTaskMethodBuilder))]
struct ValueTask { }
[AsyncMethodBuilder(typeof(ValueTaskMethodBuilder<>))]
struct ValueTask<T> { }
class ValueTaskMethodBuilder
{
    public static ValueTaskMethodBuilder Create() => null;
    public ValueTask Task { get; }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void SetException(System.Exception exception) { }
    public void SetResult() { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
}
class ValueTaskMethodBuilder<T>
{
    public static ValueTaskMethodBuilder<T> Create() => null;
    public ValueTask<T> Task { get; }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void SetException(System.Exception exception) { }
    public void SetResult(T result) { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
}
namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var v = CompileAndVerify(source, null, options: TestOptions.ReleaseDll);
            v.VerifyIL("C.g",
@"{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (C.<g>d__1 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""ValueTaskMethodBuilder<int> ValueTaskMethodBuilder<int>.Create()""
  IL_0007:  stfld      ""ValueTaskMethodBuilder<int> C.<g>d__1.<>t__builder""
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      ""int C.<g>d__1.<>1__state""
  IL_0014:  ldloc.0
  IL_0015:  ldfld      ""ValueTaskMethodBuilder<int> C.<g>d__1.<>t__builder""
  IL_001a:  ldloca.s   V_0
  IL_001c:  callvirt   ""void ValueTaskMethodBuilder<int>.Start<C.<g>d__1>(ref C.<g>d__1)""
  IL_0021:  ldloc.0
  IL_0022:  ldfld      ""ValueTaskMethodBuilder<int> C.<g>d__1.<>t__builder""
  IL_0027:  callvirt   ""ValueTask<int> ValueTaskMethodBuilder<int>.Task.get""
  IL_002c:  ret
}");
            v.VerifyIL("C.f",
@"{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (C.<f>d__0 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""ValueTaskMethodBuilder ValueTaskMethodBuilder.Create()""
  IL_0007:  stfld      ""ValueTaskMethodBuilder C.<f>d__0.<>t__builder""
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      ""int C.<f>d__0.<>1__state""
  IL_0014:  ldloc.0
  IL_0015:  ldfld      ""ValueTaskMethodBuilder C.<f>d__0.<>t__builder""
  IL_001a:  ldloca.s   V_0
  IL_001c:  callvirt   ""void ValueTaskMethodBuilder.Start<C.<f>d__0>(ref C.<f>d__0)""
  IL_0021:  ldloc.0
  IL_0022:  ldfld      ""ValueTaskMethodBuilder C.<f>d__0.<>t__builder""
  IL_0027:  callvirt   ""ValueTask ValueTaskMethodBuilder.Task.get""
  IL_002c:  ret
}");
        }

        [Fact]
        public void AsyncTasklikeGenericBuilder()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class N
{
    class BN { }
    class BG<U> { }

    [AsyncMethodBuilder(typeof(N.BG<int>))] class T_NIT<V> { }
    [AsyncMethodBuilder(typeof(N.BG<int>))] class T_NIN { }
    [AsyncMethodBuilder(typeof(N.BG<>))] class T_NOT<V> { }
    [AsyncMethodBuilder(typeof(N.BG<>))] class T_NON { }
    [AsyncMethodBuilder(typeof(N.BN))] class T_NNT<V> { }
    [AsyncMethodBuilder(typeof(N.BN))] class T_NNN { }

    async T_NIT<int> f1() => await Task.FromResult(1); 
    async T_NIN f2() => await Task.FromResult(1);      
    async T_NOT<int> f3() => await Task.FromResult(1); // ok builderType genericity (but missing members)
    async T_NON f4() => await Task.FromResult(1);      
    async T_NNT<int> f5() => await Task.FromResult(1); 
    async T_NNN f6() => await Task.FromResult(1);      // ok builderType genericity (but missing members)
}

class G<T>
{
    class BN { }
    class BG<U> { }

    [AsyncMethodBuilder(typeof(G<int>.BG<int>))] class T_IIT<V> { }
    [AsyncMethodBuilder(typeof(G<int>.BG<int>))] class T_IIN { }
    [AsyncMethodBuilder(typeof(G<int>.BN))] class T_INT<V> { }
    [AsyncMethodBuilder(typeof(G<int>.BN))] class T_INN { }
    [AsyncMethodBuilder(typeof(G<>.BG<>))] class T_OOT<V> { }
    [AsyncMethodBuilder(typeof(G<>.BG<>))] class T_OON { }
    [AsyncMethodBuilder(typeof(G<>.BN))] class T_ONT<V> { }
    [AsyncMethodBuilder(typeof(G<>.BN))] class T_ONN { }

    async T_IIT<int> g1() => await Task.FromResult(1);
    async T_IIN g2() => await Task.FromResult(1);
    async T_INT<int> g3() => await Task.FromResult(1);
    async T_INN g4() => await Task.FromResult(1);      // might have been ok builder genericity but we decided not
    async T_OOT<int> g5() => await Task.FromResult(1);
    async T_OON g6() => await Task.FromResult(1);
    async T_ONT<int> g7() => await Task.FromResult(1);
    async T_ONN g8() => await Task.FromResult(1);
}

class Program { static void Main() { } }

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,27): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_NIT<int> f1() => await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(17, 27),
                // (18,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_NIN f2() => await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(18, 22),
                // (19,27): error CS0656: Missing compiler required member 'N.BG<int>.Task'
                //     async T_NOT<int> f3() => await Task.FromResult(1); // ok builderType genericity (but missing members)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.FromResult(1)").WithArguments("N.BG<int>", "Task").WithLocation(19, 27),
                // (19,27): error CS0656: Missing compiler required member 'N.BG<int>.Create'
                //     async T_NOT<int> f3() => await Task.FromResult(1); // ok builderType genericity (but missing members)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.FromResult(1)").WithArguments("N.BG<int>", "Create").WithLocation(19, 27),
                // (20,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_NON f4() => await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(20, 22),
                // (21,27): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_NNT<int> f5() => await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(21, 27),
                // (22,22): error CS0656: Missing compiler required member 'N.BN.Task'
                //     async T_NNN f6() => await Task.FromResult(1);      // ok builderType genericity (but missing members)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.FromResult(1)").WithArguments("N.BN", "Task").WithLocation(22, 22),
                // (22,22): error CS0656: Missing compiler required member 'N.BN.Create'
                //     async T_NNN f6() => await Task.FromResult(1);      // ok builderType genericity (but missing members)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.FromResult(1)").WithArguments("N.BN", "Create").WithLocation(22, 22),
                // (39,27): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_IIT<int> g1() => await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(39, 27),
                // (40,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_IIN g2() => await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(40, 22),
                // (41,27): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_INT<int> g3() => await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(41, 27),
                // (42,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_INN g4() => await Task.FromResult(1);      // might have been ok builder genericity but we decided not
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(42, 22),
                // (43,27): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_OOT<int> g5() => await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(43, 27),
                // (44,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_OON g6() => await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(44, 22),
                // (45,27): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_ONT<int> g7() => await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(45, 27),
                // (46,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T_ONN g8() => await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.FromResult(1)").WithLocation(46, 22)
                );
        }

        [Fact]
        public void AsyncTasklikeBadAttributeArgument1()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[AsyncMethodBuilder(typeof(void))] class T { }

class Program {
    static void Main() { }
    async T f() => await Task.Delay(1);
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (9,17): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T f() => await Task.Delay(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.Delay(1)").WithLocation(9, 17)
                );
        }

        [Fact]
        public void AsyncTasklikeBadAttributeArgument2()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[AsyncMethodBuilder(""hello"")] class T { }

class Program {
    static void Main() { }
    async T f() => await Task.Delay(1);
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (5,15): error CS1503: Argument 1: cannot convert from 'string' to 'System.Type'
                // [AsyncMethodBuilder("hello")] class T { }
                Diagnostic(ErrorCode.ERR_BadArgType, @"""hello""").WithArguments("1", "string", "System.Type").WithLocation(5, 21),
                // (9,13): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T f() => await Task.Delay(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "f").WithLocation(9, 13)
                );
        }

        [Fact]
        public void AsyncTasklikeBadAttributeArgument3()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[AsyncMethodBuilder(typeof(Nonexistent))] class T { }

class Program {
    static void Main() { }
    async T f() => await Task.Delay(1);
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (5,22): error CS0246: The type or namespace name 'Nonexistent' could not be found (are you missing a using directive or an assembly reference?)
                // [AsyncMethodBuilder(typeof(Nonexistent))] class T { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Nonexistent").WithArguments("Nonexistent").WithLocation(5, 28)
                );
        }

        [Fact]
        public void AsyncTasklikeBadAttributeArgument4()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[AsyncMethodBuilder(null)] class T { }

class Program {
    static void Main() { }
    async T f() => await Task.Delay(1);
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (9,17): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T f() => await Task.Delay(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.Delay(1)").WithLocation(9, 17)
                );
        }

        [Fact]
        public void AsyncTasklikeMissingBuilderType()
        {
            // Builder
            var libB = @"public class B { }";
            var cB = CreateCompilationWithMscorlib45(libB);
            var rB = cB.EmitToImageReference();

            // Tasklike
            var libT = @"
using System.Runtime.CompilerServices;

[AsyncMethodBuilder(typeof(B))] public class T { }

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var cT = CreateCompilationWithMscorlib45(libT, references: new[] { rB });
            var rT = cT.EmitToImageReference();

            // Consumer, fails to reference builder
            var source = @"
using System.Threading.Tasks;

class Program {
    static void Main() { }
    async T f() => await Task.Delay(1);
}
";
            var c = CreateCompilationWithMscorlib45(source, references: new[] { rT });
            c.VerifyEmitDiagnostics(
                // (6,17): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T f() => await Task.Delay(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.Delay(1)").WithLocation(6, 17)
                );
        }

        [Fact]
        public void AsyncTasklikeCreateMethod()
        {
            var source = $@"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class Program {{
    static void Main() {{ }}
    async T0 f0() => await Task.Delay(0);
    async T1 f1() => await Task.Delay(1);
    async T2 f2() => await Task.Delay(2);
    async T3 f3() => await Task.Delay(3);
    async T4 f4() => await Task.Delay(4);
    async T5 f5() => await Task.Delay(5);
    async T6 f6() => await Task.Delay(6);
    async T7 f7() => await Task.Delay(7);
    async T8 f8() => await Task.Delay(8);
}}

[AsyncMethodBuilder(typeof(B0))] public class T0 {{ }}
[AsyncMethodBuilder(typeof(B1))] public class T1 {{ }}
[AsyncMethodBuilder(typeof(B2))] public class T2 {{ }}
[AsyncMethodBuilder(typeof(B3))] public class T3 {{ }}
[AsyncMethodBuilder(typeof(B4))] public class T4 {{ }}
[AsyncMethodBuilder(typeof(B5))] public class T5 {{ }}
[AsyncMethodBuilder(typeof(B6))] public class T6 {{ }}
[AsyncMethodBuilder(typeof(B7))] public class T7 {{ }}
[AsyncMethodBuilder(typeof(B8))] public class T8 {{ }}

{AsyncBuilderCode("B0", "T0").Replace("public static B0 Create()", "public static B0 Create()")}
{AsyncBuilderCode("B1", "T1").Replace("public static B1 Create()", "private static B1 Create()")}
{AsyncBuilderCode("B2", "T2").Replace("public static B2 Create() => default(B2);", "public static void Create() { }")}
{AsyncBuilderCode("B3", "T3").Replace("public static B3 Create() => default(B3);", "public static B1 Create() => default(B1);")}
{AsyncBuilderCode("B4", "T4").Replace("public static B4 Create()", "public static B4 Create(int i)")}
{AsyncBuilderCode("B5", "T5").Replace("public static B5 Create()", "public static B5 Create<T>()")}
{AsyncBuilderCode("B6", "T6").Replace("public static B6 Create()", "public static B6 Create(object arg = null)")}
{AsyncBuilderCode("B7", "T7").Replace("public static B7 Create()", "public static B7 Create(params object[] arg)")}
{AsyncBuilderCode("B8", "T8").Replace("public static B8 Create()", "public B8 Create()")}

namespace System.Runtime.CompilerServices {{ class AsyncMethodBuilderAttribute : System.Attribute {{ public AsyncMethodBuilderAttribute(System.Type t) {{ }} }} }}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (8,19): error CS0656: Missing compiler required member 'B1.Create'
                //     async T1 f1() => await Task.Delay(1);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(1)").WithArguments("B1", "Create").WithLocation(8, 19),
                // (9,19): error CS0656: Missing compiler required member 'B2.Create'
                //     async T2 f2() => await Task.Delay(2);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(2)").WithArguments("B2", "Create").WithLocation(9, 19),
                // (10,19): error CS0656: Missing compiler required member 'B3.Create'
                //     async T3 f3() => await Task.Delay(3);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(3)").WithArguments("B3", "Create").WithLocation(10, 19),
                // (11,19): error CS0656: Missing compiler required member 'B4.Create'
                //     async T4 f4() => await Task.Delay(4);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(4)").WithArguments("B4", "Create").WithLocation(11, 19),
                // (12,19): error CS0656: Missing compiler required member 'B5.Create'
                //     async T5 f5() => await Task.Delay(5);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(5)").WithArguments("B5", "Create").WithLocation(12, 19),
                // (13,19): error CS0656: Missing compiler required member 'B6.Create'
                //     async T6 f6() => await Task.Delay(6);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(6)").WithArguments("B6", "Create").WithLocation(13, 19),
                // (14,19): error CS0656: Missing compiler required member 'B7.Create'
                //     async T7 f7() => await Task.Delay(7);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(7)").WithArguments("B7", "Create").WithLocation(14, 19),
                // (15,19): error CS0656: Missing compiler required member 'B8.Create'
                //     async T8 f8() => await Task.Delay(8);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> await Task.Delay(8)").WithArguments("B8", "Create").WithLocation(15, 19)
                );
        }

        [Fact]
        public void AsyncInterfaceTasklike()
        {
            var source = $@"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class Program {{
    static void Main() {{ }}
    async I0 f0() => await Task.Delay(0);
    async I1<int> f1() {{  await Task.Delay(1); return 1; }}
}}

[AsyncMethodBuilder(typeof(B0))] public interface I0 {{ }}
[AsyncMethodBuilder(typeof(B1<>))] public interface I1<T> {{ }}

{AsyncBuilderCode("B0", "I0", genericTypeParameter: null)}
{AsyncBuilderCode("B1", "I1", genericTypeParameter: "T")}

namespace System.Runtime.CompilerServices {{ class AsyncMethodBuilderAttribute : System.Attribute {{ public AsyncMethodBuilderAttribute(System.Type t) {{ }} }} }}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                );
        }

        [Fact]
        public void AsyncTasklikeBuilderAccessibility()
        {
            var source = $@"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[AsyncMethodBuilder(typeof(B1))] public class T1 {{ }}
[AsyncMethodBuilder(typeof(B2))] public class T2 {{ }}
[AsyncMethodBuilder(typeof(B3))] internal class T3 {{ }}
[AsyncMethodBuilder(typeof(B4))] internal class T4 {{ }}

{AsyncBuilderCode("B1", "T1").Replace("public class B1", "public class B1")}
{AsyncBuilderCode("B2", "T2").Replace("public class B2", "internal class B2")}
{AsyncBuilderCode("B3", "T3").Replace("public class B3", "public class B3").Replace("public T3 Task { get; }", "internal T3 Task {get; }")}
{AsyncBuilderCode("B4", "T4").Replace("public class B4", "internal class B4")}

class Program {{
    static void Main() {{ }}
    async T1 f1() => await Task.Delay(1);
    async T2 f2() => await Task.Delay(2);
    async T3 f3() => await Task.Delay(3);
    async T4 f4() => await Task.Delay(4);
}}

namespace System.Runtime.CompilerServices {{ class AsyncMethodBuilderAttribute : System.Attribute {{ public AsyncMethodBuilderAttribute(System.Type t) {{ }} }} }}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (66,19): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T2 f2() => await Task.Delay(2);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.Delay(2)").WithLocation(66, 19),
                // (67,19): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T3 f3() => await Task.Delay(3);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=> await Task.Delay(3)").WithLocation(67, 19)
                );
        }

        [Fact]
        public void AsyncTasklikeLambdaOverloads()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    static void Main()
    {
        f(async () => { await (Task)null; });
        g(async () => { await (Task)null; });
        k(async () => { await (Task)null; });
    }

    static void f(Func<MyTask> lambda) { }
    static void g(Func<Task> lambda) { }
    static void k<T>(Func<T> lambda) { }
}
[AsyncMethodBuilder(typeof(MyTaskBuilder))]
class MyTask { }
class MyTaskBuilder
{
    public static MyTaskBuilder Create() => null;
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void SetResult() { }
    public void SetException(Exception exception) { }
    public MyTask Task => default(MyTask);
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
}
namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var v = CompileAndVerify(source, null, options: TestOptions.ReleaseDll);
            v.VerifyIL("C.Main", @"
{
  // Code size      109 (0x6d)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<MyTask> C.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_000e:  ldftn      ""MyTask C.<>c.<Main>b__0_0()""
  IL_0014:  newobj     ""System.Func<MyTask>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""System.Func<MyTask> C.<>c.<>9__0_0""
  IL_001f:  call       ""void C.f(System.Func<MyTask>)""
  IL_0024:  ldsfld     ""System.Func<System.Threading.Tasks.Task> C.<>c.<>9__0_1""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0032:  ldftn      ""System.Threading.Tasks.Task C.<>c.<Main>b__0_1()""
  IL_0038:  newobj     ""System.Func<System.Threading.Tasks.Task>..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""System.Func<System.Threading.Tasks.Task> C.<>c.<>9__0_1""
  IL_0043:  call       ""void C.g(System.Func<System.Threading.Tasks.Task>)""
  IL_0048:  ldsfld     ""System.Func<System.Threading.Tasks.Task> C.<>c.<>9__0_2""
  IL_004d:  dup
  IL_004e:  brtrue.s   IL_0067
  IL_0050:  pop
  IL_0051:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0056:  ldftn      ""System.Threading.Tasks.Task C.<>c.<Main>b__0_2()""
  IL_005c:  newobj     ""System.Func<System.Threading.Tasks.Task>..ctor(object, System.IntPtr)""
  IL_0061:  dup
  IL_0062:  stsfld     ""System.Func<System.Threading.Tasks.Task> C.<>c.<>9__0_2""
  IL_0067:  call       ""void C.k<System.Threading.Tasks.Task>(System.Func<System.Threading.Tasks.Task>)""
  IL_006c:  ret
}");
        }

        [Fact]
        public void AsyncTasklikeIncompleteBuilder()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    static void Main() { }
    async ValueTask0 f() { await Task.Delay(0); }
    async ValueTask1 g() { await Task.Delay(0); }
    async ValueTask2 h() { await Task.Delay(0); }
}
[AsyncMethodBuilder(typeof(ValueTaskMethodBuilder0))]
struct ValueTask0 { }
[AsyncMethodBuilder(typeof(ValueTaskMethodBuilder1))]
struct ValueTask1 { }
[AsyncMethodBuilder(typeof(ValueTaskMethodBuilder2))]
struct ValueTask2 { }
class ValueTaskMethodBuilder0
{
    public static ValueTaskMethodBuilder0 Create() => null;
    public ValueTask0 Task => default(ValueTask0);
}
class ValueTaskMethodBuilder1
{
    public static ValueTaskMethodBuilder1 Create() => null;
    public ValueTask1 Task => default(ValueTask1);
    public void SetException(System.Exception ex) { }
}
class ValueTaskMethodBuilder2
{
    public static ValueTaskMethodBuilder2 Create() => null;
    public ValueTask2 Task => default(ValueTask2);
    public void SetException(System.Exception ex) { } public void SetResult() { }
}
namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (7,26): error CS0656: Missing compiler required member 'ValueTaskMethodBuilder0.SetException'
                //     async ValueTask0 f() { await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.Delay(0); }").WithArguments("ValueTaskMethodBuilder0", "SetException").WithLocation(7, 26),
                // (8,26): error CS0656: Missing compiler required member 'ValueTaskMethodBuilder1.SetResult'
                //     async ValueTask1 g() { await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.Delay(0); }").WithArguments("ValueTaskMethodBuilder1", "SetResult").WithLocation(8, 26),
                // (9,26): error CS0656: Missing compiler required member 'ValueTaskMethodBuilder2.AwaitOnCompleted'
                //     async ValueTask2 h() { await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.Delay(0); }").WithArguments("ValueTaskMethodBuilder2", "AwaitOnCompleted").WithLocation(9, 26)
                );
        }

        [Fact]
        public void AsyncTasklikeBuilderArityMismatch()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C {
    async Mismatch1<int> f() { await (Task)null; return 1; }
    async Mismatch2 g() { await (Task)null; return 1; }
}
[AsyncMethodBuilder(typeof(Mismatch1MethodBuilder))]
struct Mismatch1<T> { }
[AsyncMethodBuilder(typeof(Mismatch2MethodBuilder<>))]
struct Mismatch2 { }
class Mismatch1MethodBuilder
{
    public static Mismatch1MethodBuilder Create() => null;
}
class Mismatch2MethodBuilder<T>
{
    public static Mismatch2MethodBuilder<T> Create() => null;
}
namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var comp = CreateCompilationWithMscorlib45(source);
            comp.VerifyEmitDiagnostics(
                // (5,30): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async Mismatch1<int> f() { await (Task)null; return 1; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "{ await (Task)null; return 1; }").WithLocation(5, 30),
                // (6,45): error CS1997: Since 'C.g()' is an async method that returns 'Task', a return keyword must not be followed by an object expression. Did you intend to return 'Task<T>'?
                //     async Mismatch2 g() { await (Task)null; return 1; }
                Diagnostic(ErrorCode.ERR_TaskRetNoObjectRequired, "return").WithArguments("C.g()").WithLocation(6, 45)
                );
        }

        [WorkItem(12616, "https://github.com/dotnet/roslyn/issues/12616")]
        [Fact]
        public void AsyncTasklikeBuilderConstraints()
        {
            var source1 = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    static void Main() { }
    async MyTask f() { await (Task)null; }
}

[AsyncMethodBuilder(typeof(MyTaskBuilder))]
class MyTask { }

interface I { }

class MyTaskBuilder
{
    public static MyTaskBuilder Create() => null;
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TSM>(ref TSM stateMachine) where TSM : I { }
    public void AwaitOnCompleted<TA, TSM>(ref TA awaiter, ref TSM stateMachine) { }
    public void AwaitUnsafeOnCompleted<TA, TSM>(ref TA awaiter, ref TSM stateMachine) { }
    public void SetResult() { }
    public void SetException(Exception ex) { }
    public MyTask Task => null;
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe);
            comp1.VerifyEmitDiagnostics(
                // (8,22): error CS0311: The type 'C.<f>d__1' cannot be used as type parameter 'TSM' in the generic type or method 'MyTaskBuilder.Start<TSM>(ref TSM)'. There is no implicit reference conversion from 'C.<f>d__1' to 'I'.
                //     async MyTask f() { await (Task)null; }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "{ await (Task)null; }").WithArguments("MyTaskBuilder.Start<TSM>(ref TSM)", "I", "TSM", "C.<f>d__1").WithLocation(8, 22));

            var source2 = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    static void Main() { }
    async MyTask f() { await (Task)null; }
}

[AsyncMethodBuilder(typeof(MyTaskBuilder))]
class MyTask { }

class MyTaskBuilder
{
    public static MyTaskBuilder Create() => null;
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TSM>(ref TSM stateMachine) where TSM : IAsyncStateMachine { }
    public void AwaitOnCompleted<TA, TSM>(ref TA awaiter, ref TSM stateMachine) where TA : INotifyCompletion where TSM : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TA, TSM>(ref TA awaiter, ref TSM stateMachine) { }
    public void SetResult() { }
    public void SetException(Exception ex) { }
    public MyTask Task => null;
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe);
            comp2.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem(868822, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/868822")]
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

        [Fact]
        public void MutatingArrayOfStructs()
        {
            var source = @"
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

struct S
{
    public int A;

    public int Mutate(int b)
    {
        A += b;
        return 1;
    }
}

class Test
{
    static int i = 0;

    public static Task<int> G() { return null; }

    public static async Task<int> F()
    {
        S[] array = new S[10];    
        
        return array[1].Mutate(await G());
    }
}";
            var v = CompileAndVerify(source, null, options: TestOptions.DebugDll);

            v.VerifyIL("Test.<F>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      241 (0xf1)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                Test.<F>d__2 V_3,
                System.Exception V_4)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0070
   -IL_000e:  nop
   -IL_000f:  ldarg.0
    IL_0010:  ldc.i4.s   10
    IL_0012:  newarr     ""S""
    IL_0017:  stfld      ""S[] Test.<F>d__2.<array>5__1""
   -IL_001c:  ldarg.0
    IL_001d:  ldarg.0
    IL_001e:  ldfld      ""S[] Test.<F>d__2.<array>5__1""
    IL_0023:  stfld      ""S[] Test.<F>d__2.<>s__3""
    IL_0028:  ldarg.0
    IL_0029:  ldfld      ""S[] Test.<F>d__2.<>s__3""
    IL_002e:  ldc.i4.1
    IL_002f:  ldelema    ""S""
    IL_0034:  pop
    IL_0035:  call       ""System.Threading.Tasks.Task<int> Test.G()""
    IL_003a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_003f:  stloc.2
   ~IL_0040:  ldloca.s   V_2
    IL_0042:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0047:  brtrue.s   IL_008c
    IL_0049:  ldarg.0
    IL_004a:  ldc.i4.0
    IL_004b:  dup
    IL_004c:  stloc.0
    IL_004d:  stfld      ""int Test.<F>d__2.<>1__state""
   <IL_0052:  ldarg.0
    IL_0053:  ldloc.2
    IL_0054:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_0059:  ldarg.0
    IL_005a:  stloc.3
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
    IL_0061:  ldloca.s   V_2
    IL_0063:  ldloca.s   V_3
    IL_0065:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__2)""
    IL_006a:  nop
    IL_006b:  leave      IL_00f0
   >IL_0070:  ldarg.0
    IL_0071:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_0076:  stloc.2
    IL_0077:  ldarg.0
    IL_0078:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_007d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.m1
    IL_0085:  dup
    IL_0086:  stloc.0
    IL_0087:  stfld      ""int Test.<F>d__2.<>1__state""
    IL_008c:  ldarg.0
    IL_008d:  ldloca.s   V_2
    IL_008f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0094:  stfld      ""int Test.<F>d__2.<>s__2""
    IL_0099:  ldarg.0
    IL_009a:  ldfld      ""S[] Test.<F>d__2.<>s__3""
    IL_009f:  ldc.i4.1
    IL_00a0:  ldelema    ""S""
    IL_00a5:  ldarg.0
    IL_00a6:  ldfld      ""int Test.<F>d__2.<>s__2""
    IL_00ab:  call       ""int S.Mutate(int)""
    IL_00b0:  stloc.1
    IL_00b1:  leave.s    IL_00d4
  }
  catch System.Exception
  {
   ~IL_00b3:  stloc.s    V_4
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.s   -2
    IL_00b8:  stfld      ""int Test.<F>d__2.<>1__state""
    IL_00bd:  ldarg.0
    IL_00be:  ldnull
    IL_00bf:  stfld      ""S[] Test.<F>d__2.<array>5__1""
    IL_00c4:  ldarg.0
    IL_00c5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
    IL_00ca:  ldloc.s    V_4
    IL_00cc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00d1:  nop
    IL_00d2:  leave.s    IL_00f0
  }
 -IL_00d4:  ldarg.0
  IL_00d5:  ldc.i4.s   -2
  IL_00d7:  stfld      ""int Test.<F>d__2.<>1__state""
 ~IL_00dc:  ldarg.0
  IL_00dd:  ldnull
  IL_00de:  stfld      ""S[] Test.<F>d__2.<array>5__1""
  IL_00e3:  ldarg.0
  IL_00e4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
  IL_00e9:  ldloc.1
  IL_00ea:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00ef:  nop
  IL_00f0:  ret
}",
            sequencePoints: "Test+<F>d__2.MoveNext");
        }

        [Fact]
        public void MutatingStructWithUsing()
        {
            var source =
@"using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    public static void Main()
    {
        (new Program()).Test().Wait();
    }

    public async Task Test()
    {
        var list = new List<int> {1, 2, 3};

        using (var enumerator = list.GetEnumerator()) 
        {
            Console.WriteLine(enumerator.MoveNext());
            Console.WriteLine(enumerator.Current);

            await Task.Delay(1);
        }
    }
}";

            var expectedOutput = @"True
1";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(1942, "https://github.com/dotnet/roslyn/issues/1942")]
        public void HoistStructure()
        {
            var source = @"
using System;
using System.Threading.Tasks;
namespace ConsoleApp
{
    struct TestStruct
    {
        public long i;
        public long j;
    }
    class Program
    {
        static async Task TestAsync()
        {
            TestStruct t;
            t.i = 12;
            Console.WriteLine(""Before {0}"", t.i); // emits ""Before 12"" 
            await Task.Delay(100);
            Console.WriteLine(""After {0}"", t.i); // emits ""After 0"" expecting ""After 12"" 
        }
        static void Main(string[] args)
        {
            TestAsync().Wait();
        }
    }
}";

            var expectedOutput = @"Before 12
After 12";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: expectedOutput);

            CompileAndVerify(comp.WithOptions(TestOptions.ReleaseExe), expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(2567, "https://github.com/dotnet/roslyn/issues/2567")]
        public void AwaitInUsingAndForeach()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Program
{
    System.Collections.Generic.IEnumerable<int> ien = null;
    async Task<int> Test(IDisposable id, Task<int> task)
    {
        try
        {
            foreach (var i in ien)
            {
                return await task;
            }
            using (id)
            {
                return await task;
            }
        }
        catch (Exception)
        {
            return await task;
        }
    }
    public static void Main() {}
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp);
            CompileAndVerify(comp.WithOptions(TestOptions.ReleaseExe));
        }

        [Fact, WorkItem(4697, "https://github.com/dotnet/roslyn/issues/4697")]
        public void AwaitInObjInitializer()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace CompilerCrashRepro2
{
    public class Item<T>
    {
        public T Value { get; set; }
    }

    public class Crasher
    {
        public static void Main()
        {
            var r = Build<int>()().Result.Value;
            System.Console.WriteLine(r);
        }

        public static Func<Task<Item<T>>> Build<T>()
        {
            return async () => new Item<T>()
            {
                Value = await GetValue<T>()
            };
        }

        public static Task<T> GetValue<T>()
        {
            return Task.FromResult(default(T));
        }
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "0");
            CompileAndVerify(comp.WithOptions(TestOptions.ReleaseExe), expectedOutput: "0");
        }

        [Fact]
        public void AwaitInScriptExpression()
        {
            var source =
@"System.Console.WriteLine(await System.Threading.Tasks.Task.FromResult(1));";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void AwaitInScriptGlobalStatement()
        {
            var source =
@"await System.Threading.Tasks.Task.FromResult(4);";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void AwaitInScriptDeclaration()
        {
            var source =
@"int x = await System.Threading.Tasks.Task.Run(() => 2);
System.Console.WriteLine(x);";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void AwaitInInteractiveExpression()
        {
            var references = new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef };
            var source0 =
@"static async System.Threading.Tasks.Task<int> F()
{
    return await System.Threading.Tasks.Task.FromResult(3);
}";
            var source1 =
@"await F()";
            var s0 = CSharpCompilation.CreateScriptCompilation("s0.dll", SyntaxFactory.ParseSyntaxTree(source0, options: TestOptions.Script), references);
            var s1 = CSharpCompilation.CreateScriptCompilation("s1.dll", SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.Script), references, previousScriptCompilation: s0);
            s1.VerifyDiagnostics();
        }

        [Fact]
        public void AwaitInInteractiveGlobalStatement()
        {
            var references = new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef };
            var source0 =
@"await System.Threading.Tasks.Task.FromResult(5);";
            var s0 = CSharpCompilation.CreateScriptCompilation("s0.dll", SyntaxFactory.ParseSyntaxTree(source0, options: TestOptions.Script), references);
            s0.VerifyDiagnostics();
        }

        /// <summary>
        /// await should be disallowed in static field initializer
        /// since the static initialization of the class must be
        /// handled synchronously in the .cctor.
        /// </summary>
        [WorkItem(5787, "https://github.com/dotnet/roslyn/issues/5787")]
        [Fact]
        public void AwaitInScriptStaticInitializer()
        {
            var source =
@"static int x = 1 +
    await System.Threading.Tasks.Task.FromResult(1);
int y = x +
    await System.Threading.Tasks.Task.FromResult(2);";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (2,5): error CS8100: The 'await' operator cannot be used in a static script variable initializer.
                //     await System.Threading.Tasks.Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_BadAwaitInStaticVariableInitializer, "await System.Threading.Tasks.Task.FromResult(1)").WithLocation(2, 5));
        }

        [Fact, WorkItem(4839, "https://github.com/dotnet/roslyn/issues/4839")]
        public void SwitchOnAwaitedValueAsync()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Program
{
    static void Main()
    {
        M(0).Wait();
    }

    static async Task M(int input)
    {
        var value = 1; 
        switch (value)
        {
            case 0:
                return;
            case 1:
                return;
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp);
            CompileAndVerify(comp.WithOptions(TestOptions.ReleaseExe));
        }

        [Fact, WorkItem(4839, "https://github.com/dotnet/roslyn/issues/4839")]
        public void SwitchOnAwaitedValue()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Program
{
    static void Main()
    {
        M(0);
    }

    static void M(int input)
    {
        try
        {
            var value = 1;
            switch (value)
            {
                case 1:
                    return;
                case 2:
                    return;
            }
        }
        catch (Exception)
        {
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp).
                VerifyIL("Program.M(int)",
                @"
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (int V_0) //value
  .try
  {
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  ldloc.0
    IL_0003:  ldc.i4.1
    IL_0004:  beq.s      IL_000a
    IL_0006:  ldloc.0
    IL_0007:  ldc.i4.2
    IL_0008:  pop
    IL_0009:  pop
    IL_000a:  leave.s    IL_000f
  }
  catch System.Exception
  {
    IL_000c:  pop
    IL_000d:  leave.s    IL_000f
  }
  IL_000f:  ret
}");
        }

        [Fact, WorkItem(4839, "https://github.com/dotnet/roslyn/issues/4839")]
        public void SwitchOnAwaitedValueString()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Program
{
    static void Main()
    {
        M(0).Wait();
    }

    static async Task M(int input)
    {
        var value = ""q""; 
        switch (value)
        {
            case ""a"":
                return;
            case ""b"":
                return;
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp);
            CompileAndVerify(comp.WithOptions(TestOptions.ReleaseExe));
        }

        [Fact, WorkItem(4838, "https://github.com/dotnet/roslyn/issues/4838")]
        public void SwitchOnAwaitedValueInLoop()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Program
{
    static void Main()
    {
        M(0).Wait();
    }

    static async Task M(int input)
    {
        for (;;)
        {
            var value = await Task.FromResult(input);
            switch (value)
            {
                case 0:
                    return;
                case 3:
                    return;
                case 4:
                    continue;
                case 100:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(""Unknown value: "" + value);
            }
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp);
            CompileAndVerify(comp.WithOptions(TestOptions.ReleaseExe));
        }

        [Fact, WorkItem(7669, "https://github.com/dotnet/roslyn/issues/7669")]
        public void HoistUsing001()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(M(0).Result);
    }

    class D : IDisposable
    {
        public void Dispose()
        {
            Console.WriteLine(""disposed"");
        }
    }

    static async Task<string> M(int input)
    {
        Console.WriteLine(""Pre"");
        var window = new D();
        try
        {
            Console.WriteLine(""show"");

            for (int i = 0; i < 2; i++)
            {
                await Task.Delay(100);
            }
        }
        finally
        {
            window.Dispose();
        }

        Console.WriteLine(""Post"");
        return ""result"";
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            var expectedOutput = @"Pre
show
disposed
Post
result";

            CompileAndVerify(comp, expectedOutput: expectedOutput);
            CompileAndVerify(comp.WithOptions(TestOptions.ReleaseExe), expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(7669, "https://github.com/dotnet/roslyn/issues/7669")]
        public void HoistUsing002()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(M(0).Result);
    }

    class D : IDisposable
    {
        public void Dispose()
        {
            Console.WriteLine(""disposed"");
        }
    }

    static async Task<string> M(int input)
    {
        Console.WriteLine(""Pre"");

        using (var window = new D())
        {
            Console.WriteLine(""show"");

            for (int i = 0; i < 2; i++)
            {
                await Task.Delay(100);
            }
        }

        Console.WriteLine(""Post"");
        return ""result"";
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            var expectedOutput = @"Pre
show
disposed
Post
result";

            CompileAndVerify(comp, expectedOutput: expectedOutput);
            CompileAndVerify(comp.WithOptions(TestOptions.ReleaseExe), expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(7669, "https://github.com/dotnet/roslyn/issues/7669")]
        public void HoistUsing003()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(M(0).Result);
    }

    class D : IDisposable
    {
        public void Dispose()
        {
            Console.WriteLine(""disposed"");
        }
    }

    static async Task<string> M(int input)
    {
        Console.WriteLine(""Pre"");

        using (var window1 = new D())
        {
            Console.WriteLine(""show"");

            using (var window = new D())
            {
                Console.WriteLine(""show"");

                for (int i = 0; i < 2; i++)
                {
                    await Task.Delay(100);
                }
            }
        }

        Console.WriteLine(""Post"");
        return ""result"";
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            var expectedOutput = @"Pre
show
show
disposed
disposed
Post
result";

            CompileAndVerify(comp, expectedOutput: expectedOutput);
            CompileAndVerify(comp.WithOptions(TestOptions.ReleaseExe), expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(9463, "https://github.com/dotnet/roslyn/issues/9463")]
        public void AsyncIteratorReportsDiagnosticsWhenCoreTypesAreMissing()
        {
            // Note that IAsyncStateMachine.MoveNext and IAsyncStateMachine.SetStateMachine are missing
            var source = @"
using System.Threading.Tasks;

namespace System
{
    public class Object { }
    public struct Int32 { }
    public struct Boolean { }
    public class String { }
    public class Exception { }
    public class ValueType { }
    public class Enum { }
    public struct Void { }
}

namespace System.Threading.Tasks
{
    public class Task
    {
        public TaskAwaiter GetAwaiter() { return null; }
    }

    public class TaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool IsCompleted { get { return true; } }
        public void GetResult() {  }
    }
}

namespace System.Runtime.CompilerServices
{
    public interface INotifyCompletion { }
    public interface ICriticalNotifyCompletion { }
    public interface IAsyncStateMachine { }

    public class AsyncTaskMethodBuilder
    {
        public System.Threading.Tasks.Task Task { get { return null; } }
        public void SetException(System.Exception e) { }
        public void SetResult() { }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        { }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        { }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }
}

class C
{
    async Task GetNumber(Task task) { await task; }
}";
            var compilation = CreateEmptyCompilation(new[] { Parse(source) });

            compilation.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (62,37): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create'
                //     async Task GetNumber(Task task) { await task; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await task; }").WithArguments("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", "Create").WithLocation(62, 37),
                // (62,37): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext'
                //     async Task GetNumber(Task task) { await task; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await task; }").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "MoveNext").WithLocation(62, 37),
                // (62,37): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine'
                //     async Task GetNumber(Task task) { await task; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await task; }").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "SetStateMachine").WithLocation(62, 37));
        }

        [Fact, WorkItem(16531, "https://github.com/dotnet/roslyn/issues/16531")]
        public void ArityMismatch()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public class Program
{
    public async MyAwesomeType<string> CustomTask() { await Task.Delay(1000); return string.Empty; }
}

[AsyncMethodBuilder(typeof(CustomAsyncTaskMethodBuilder<,>))]
public struct MyAwesomeType<T>
{
    public T Result { get; set; }
}

public class CustomAsyncTaskMethodBuilder<T, V>
{
    public MyAwesomeType<T> Task => default(MyAwesomeType<T>);
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public static CustomAsyncTaskMethodBuilder<T, V> Create() { return default(CustomAsyncTaskMethodBuilder<T, V>); }
    public void SetException(Exception exception) { }
    public void SetResult(T t) { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
}

namespace System.Runtime.CompilerServices
{
    public class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(Type type) { } }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyEmitDiagnostics(
                // (8,53): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     public async MyAwesomeType<string> CustomTask() { await Task.Delay(1000); return string.Empty; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "{ await Task.Delay(1000); return string.Empty; }").WithLocation(8, 53)
                );
        }

        [Fact, WorkItem(16493, "https://github.com/dotnet/roslyn/issues/16493")]
        public void AsyncMethodBuilderReturnsDifferentTypeThanTasklikeType()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public class G<T>
{
    public async ValueTask Method() { await Task.Delay(5); return; }

    [AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder))]
    public struct ValueTask
    {
    }
}

public class AsyncValueTaskMethodBuilder
{
    public G<int>.ValueTask Task { get => default(G<int>.ValueTask); }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public static AsyncValueTaskMethodBuilder Create() { return default(AsyncValueTaskMethodBuilder); }
    public void SetException(Exception exception) { }
    public void SetResult() { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
}

namespace System.Runtime.CompilerServices
{
    public class AsyncMethodBuilderAttribute : System.Attribute
    {
        public AsyncMethodBuilderAttribute(Type type) { }
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyEmitDiagnostics(
                // (8,37): error CS8204: For type 'AsyncValueTaskMethodBuilder' to be used as an AsyncMethodBuilder for type 'G<T>.ValueTask', its Task property should return type 'G<T>.ValueTask' instead of type 'G<int>.ValueTask'.
                //     public async ValueTask Method() { await Task.Delay(5); return; }
                Diagnostic(ErrorCode.ERR_BadAsyncMethodBuilderTaskProperty, "{ await Task.Delay(5); return; }").WithArguments("AsyncValueTaskMethodBuilder", "G<T>.ValueTask", "G<int>.ValueTask").WithLocation(8, 37)
                );
        }

        [Fact, WorkItem(16493, "https://github.com/dotnet/roslyn/issues/16493")]
        public void AsyncMethodBuilderReturnsDifferentTypeThanTasklikeType2()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    static async MyTask M() { await Task.Delay(5); throw null; }
}
[AsyncMethodBuilder(typeof(MyTaskBuilder))]
class MyTask { }
class MyTaskBuilder
{
    public static MyTaskBuilder Create() => null;
    public int Task => 0;
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void SetResult() { }
    public void SetException(Exception exception) { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
}
namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyEmitDiagnostics(
                // (7,29): error CS8204: For type 'MyTaskBuilder' to be used as an AsyncMethodBuilder for type 'MyTask', its Task property should return type 'MyTask' instead of type 'int'.
                //     static async MyTask M() { await Task.Delay(5); throw null; }
                Diagnostic(ErrorCode.ERR_BadAsyncMethodBuilderTaskProperty, "{ await Task.Delay(5); throw null; }").WithArguments("MyTaskBuilder", "MyTask", "int").WithLocation(7, 29)
                );
        }

        [Fact, WorkItem(18257, "https://github.com/dotnet/roslyn/issues/18257")]
        public void PatternTempsAreLongLived()
        {
            var source = @"using System;
 
public class Goo {}
 
public class C {
    public static void Main(string[] args)
    {
        var c = new C();
        c.M(new Goo());
        c.M(new object());
    }
    public async void M(object o) {
        switch (o)
        {
            case Goo _:
                Console.Write(0);
                break;
            default:
                Console.Write(1);
                break;
        }
    }
}";
            var expectedOutput = @"01";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            base.CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(18257, "https://github.com/dotnet/roslyn/issues/18257")]
        public void PatternTempsSpill()
        {
            // This test exercises the spilling machinery of async for pattern-matching temps
            var source = @"using System;
using System.Threading.Tasks;

public class C {
    public class Goo
    {
        public int Value;
    }
    public static void Main(string[] args)
    {
        var c = new C();
        c.M(new Goo() { Value = 1 });
        c.M(new Goo() { Value = 2 });
        c.M(new Goo() { Value = 3 });
        c.M(new object());
    }
    public void M(object o)
    {
        MAsync(o).Wait();
    }
    public async Task MAsync(object o) {
        switch (o)
        {
            case Goo goo when await Copy(goo.Value) == 1:
                Console.Write($""{goo.Value}=1 "");
                break;
            case Goo goo when await Copy(goo.Value) == 2:
                Console.Write($""{goo.Value}=2 "");
                break;
            case Goo goo:
                Console.Write($""{goo.Value} "");
                break;
            default:
                Console.Write(""X "");
                break;
        }
    }
    public async Task<int> Copy(int i)
    {
        await Task.Delay(1);
        return i;
    }
}";
            var expectedOutput = @"1=1 2=2 3 X";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            base.CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(19831, "https://github.com/dotnet/roslyn/issues/19831")]
        public void CaptureAssignedInOuterFinally()
        {
            var source = @"

    using System;
    using System.Threading.Tasks;

    public class Program
    {
        static void Main(string[] args)
        {
            Test().Wait();
            System.Console.WriteLine(""success"");
        }

        public static async Task Test()
        {
            // declaring variable before try/finally and nulling it in finally cause NRE in try's body
            var obj = new Object();

            try
            {
                for(int i = 0; i < 3; i++)
                {
                    // NRE on second iteration
                    obj.ToString();
                    await Task.Yield();
                }
            }
            finally
            {
                obj = null;
            }
        }
    }
";
            var expectedOutput = @"success";

            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            base.CompileAndVerify(compilation, expectedOutput: expectedOutput);

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            base.CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        public void CaptureStructReceiver()
        {
            var source = @"

    using System;
    using System.Threading.Tasks;

    public class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine(Test1().Result);
        }

        static int x = 123;
        async static Task<string> Test1()
        {
            // cannot capture 'x' by value, since write in M1 is observable
            return x.ToString(await M1());
        }

        async static Task<string> M1()
        {
            x = 42;
            await Task.Yield();
            return """";
        }
    }
";
            var expectedOutput = @"42";

            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            base.CompileAndVerify(compilation, expectedOutput: expectedOutput);

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            base.CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(13759, "https://github.com/dotnet/roslyn/issues/13759")]
        public void Unnecessary_Lifted_01()
        {
            var source = @"
using System.IO;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        public static void Main() { }

        public static async Task M(Stream source, Stream destination)
        {
            byte[] buffer = new byte[0x1000];
            int bytesRead; // this variable should not be lifted
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead);
            }
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp).
                VerifyIL("Test.Program.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
                @"
{
  // Code size      315 (0x13b)
  .maxstack  4
  .locals init (int V_0,
                int V_1, //bytesRead
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.Program.<M>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0068
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00d4
    IL_0011:  ldarg.0
    IL_0012:  ldc.i4     0x1000
    IL_0017:  newarr     ""byte""
    IL_001c:  stfld      ""byte[] Test.Program.<M>d__1.<buffer>5__2""
    IL_0021:  br.s       IL_008b
    IL_0023:  ldarg.0
    IL_0024:  ldfld      ""System.IO.Stream Test.Program.<M>d__1.destination""
    IL_0029:  ldarg.0
    IL_002a:  ldfld      ""byte[] Test.Program.<M>d__1.<buffer>5__2""
    IL_002f:  ldc.i4.0
    IL_0030:  ldloc.1
    IL_0031:  callvirt   ""System.Threading.Tasks.Task System.IO.Stream.WriteAsync(byte[], int, int)""
    IL_0036:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_003b:  stloc.2
    IL_003c:  ldloca.s   V_2
    IL_003e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0043:  brtrue.s   IL_0084
    IL_0045:  ldarg.0
    IL_0046:  ldc.i4.0
    IL_0047:  dup
    IL_0048:  stloc.0
    IL_0049:  stfld      ""int Test.Program.<M>d__1.<>1__state""
    IL_004e:  ldarg.0
    IL_004f:  ldloc.2
    IL_0050:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter Test.Program.<M>d__1.<>u__1""
    IL_0055:  ldarg.0
    IL_0056:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.Program.<M>d__1.<>t__builder""
    IL_005b:  ldloca.s   V_2
    IL_005d:  ldarg.0
    IL_005e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, Test.Program.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter, ref Test.Program.<M>d__1)""
    IL_0063:  leave      IL_013a
    IL_0068:  ldarg.0
    IL_0069:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter Test.Program.<M>d__1.<>u__1""
    IL_006e:  stloc.2
    IL_006f:  ldarg.0
    IL_0070:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter Test.Program.<M>d__1.<>u__1""
    IL_0075:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_007b:  ldarg.0
    IL_007c:  ldc.i4.m1
    IL_007d:  dup
    IL_007e:  stloc.0
    IL_007f:  stfld      ""int Test.Program.<M>d__1.<>1__state""
    IL_0084:  ldloca.s   V_2
    IL_0086:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_008b:  ldarg.0
    IL_008c:  ldfld      ""System.IO.Stream Test.Program.<M>d__1.source""
    IL_0091:  ldarg.0
    IL_0092:  ldfld      ""byte[] Test.Program.<M>d__1.<buffer>5__2""
    IL_0097:  ldc.i4.0
    IL_0098:  ldarg.0
    IL_0099:  ldfld      ""byte[] Test.Program.<M>d__1.<buffer>5__2""
    IL_009e:  ldlen
    IL_009f:  conv.i4
    IL_00a0:  callvirt   ""System.Threading.Tasks.Task<int> System.IO.Stream.ReadAsync(byte[], int, int)""
    IL_00a5:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00aa:  stloc.3
    IL_00ab:  ldloca.s   V_3
    IL_00ad:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00b2:  brtrue.s   IL_00f0
    IL_00b4:  ldarg.0
    IL_00b5:  ldc.i4.1
    IL_00b6:  dup
    IL_00b7:  stloc.0
    IL_00b8:  stfld      ""int Test.Program.<M>d__1.<>1__state""
    IL_00bd:  ldarg.0
    IL_00be:  ldloc.3
    IL_00bf:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.Program.<M>d__1.<>u__2""
    IL_00c4:  ldarg.0
    IL_00c5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.Program.<M>d__1.<>t__builder""
    IL_00ca:  ldloca.s   V_3
    IL_00cc:  ldarg.0
    IL_00cd:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.Program.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.Program.<M>d__1)""
    IL_00d2:  leave.s    IL_013a
    IL_00d4:  ldarg.0
    IL_00d5:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.Program.<M>d__1.<>u__2""
    IL_00da:  stloc.3
    IL_00db:  ldarg.0
    IL_00dc:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.Program.<M>d__1.<>u__2""
    IL_00e1:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00e7:  ldarg.0
    IL_00e8:  ldc.i4.m1
    IL_00e9:  dup
    IL_00ea:  stloc.0
    IL_00eb:  stfld      ""int Test.Program.<M>d__1.<>1__state""
    IL_00f0:  ldloca.s   V_3
    IL_00f2:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00f7:  dup
    IL_00f8:  stloc.1
    IL_00f9:  brtrue     IL_0023
    IL_00fe:  leave.s    IL_0120
  }
  catch System.Exception
  {
    IL_0100:  stloc.s    V_4
    IL_0102:  ldarg.0
    IL_0103:  ldc.i4.s   -2
    IL_0105:  stfld      ""int Test.Program.<M>d__1.<>1__state""
    IL_010a:  ldarg.0
    IL_010b:  ldnull
    IL_010c:  stfld      ""byte[] Test.Program.<M>d__1.<buffer>5__2""
    IL_0111:  ldarg.0
    IL_0112:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.Program.<M>d__1.<>t__builder""
    IL_0117:  ldloc.s    V_4
    IL_0119:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_011e:  leave.s    IL_013a
  }
  IL_0120:  ldarg.0
  IL_0121:  ldc.i4.s   -2
  IL_0123:  stfld      ""int Test.Program.<M>d__1.<>1__state""
  IL_0128:  ldarg.0
  IL_0129:  ldnull
  IL_012a:  stfld      ""byte[] Test.Program.<M>d__1.<buffer>5__2""
  IL_012f:  ldarg.0
  IL_0130:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.Program.<M>d__1.<>t__builder""
  IL_0135:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_013a:  ret
}");
        }

        [Fact, WorkItem(13759, "https://github.com/dotnet/roslyn/issues/13759")]
        public void Unnecessary_Lifted_02()
        {
            var source = @"
using System.IO;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        public static void Main() { }

        public static async Task M(Stream source, Stream destination)
        {
            bool someCondition = true;
            bool notLiftedVariable;
            while (someCondition && (notLiftedVariable = await M1()))
            {
                M2(notLiftedVariable);
            }
        }

        private static async Task<bool> M1()
        {
            await System.Threading.Tasks.Task.Delay(1);
            return true;
        }

        private static void M2(bool b) { }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp).
                VerifyIL("Test.Program.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
                @"
{
  // Code size      175 (0xaf)
  .maxstack  3
  .locals init (int V_0,
                bool V_1, //notLiftedVariable
                bool V_2,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.Program.<M>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0057
    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.1
    IL_000c:  stfld      ""bool Test.Program.<M>d__1.<someCondition>5__2""
    IL_0011:  br.s       IL_0019
    IL_0013:  ldloc.1
    IL_0014:  call       ""void Test.Program.M2(bool)""
    IL_0019:  ldarg.0
    IL_001a:  ldfld      ""bool Test.Program.<M>d__1.<someCondition>5__2""
    IL_001f:  stloc.2
    IL_0020:  ldloc.2
    IL_0021:  brfalse.s  IL_007d
    IL_0023:  call       ""System.Threading.Tasks.Task<bool> Test.Program.M1()""
    IL_0028:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_002d:  stloc.3
    IL_002e:  ldloca.s   V_3
    IL_0030:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_0035:  brtrue.s   IL_0073
    IL_0037:  ldarg.0
    IL_0038:  ldc.i4.0
    IL_0039:  dup
    IL_003a:  stloc.0
    IL_003b:  stfld      ""int Test.Program.<M>d__1.<>1__state""
    IL_0040:  ldarg.0
    IL_0041:  ldloc.3
    IL_0042:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> Test.Program.<M>d__1.<>u__1""
    IL_0047:  ldarg.0
    IL_0048:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.Program.<M>d__1.<>t__builder""
    IL_004d:  ldloca.s   V_3
    IL_004f:  ldarg.0
    IL_0050:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, Test.Program.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref Test.Program.<M>d__1)""
    IL_0055:  leave.s    IL_00ae
    IL_0057:  ldarg.0
    IL_0058:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> Test.Program.<M>d__1.<>u__1""
    IL_005d:  stloc.3
    IL_005e:  ldarg.0
    IL_005f:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> Test.Program.<M>d__1.<>u__1""
    IL_0064:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_006a:  ldarg.0
    IL_006b:  ldc.i4.m1
    IL_006c:  dup
    IL_006d:  stloc.0
    IL_006e:  stfld      ""int Test.Program.<M>d__1.<>1__state""
    IL_0073:  ldloca.s   V_3
    IL_0075:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_007a:  dup
    IL_007b:  stloc.1
    IL_007c:  stloc.2
    IL_007d:  ldloc.2
    IL_007e:  brtrue.s   IL_0013
    IL_0080:  leave.s    IL_009b
  }
  catch System.Exception
  {
    IL_0082:  stloc.s    V_4
    IL_0084:  ldarg.0
    IL_0085:  ldc.i4.s   -2
    IL_0087:  stfld      ""int Test.Program.<M>d__1.<>1__state""
    IL_008c:  ldarg.0
    IL_008d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.Program.<M>d__1.<>t__builder""
    IL_0092:  ldloc.s    V_4
    IL_0094:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0099:  leave.s    IL_00ae
  }
  IL_009b:  ldarg.0
  IL_009c:  ldc.i4.s   -2
  IL_009e:  stfld      ""int Test.Program.<M>d__1.<>1__state""
  IL_00a3:  ldarg.0
  IL_00a4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.Program.<M>d__1.<>t__builder""
  IL_00a9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00ae:  ret
}");
        }

        [Fact, WorkItem(25991, "https://github.com/dotnet/roslyn/issues/25991")]
        public void CompilerCrash01()
        {
            var source =
@"namespace Issue25991
{
    using System;
    using System.Threading.Tasks;

    public class CrashClass
    {
        public static void Main()
        {
            Console.WriteLine(""Passed"");
        }
        public async Task CompletedTask()
        {
        }
        public async Task OnCrash()
        {
            var switchObject = new object();
            switch (switchObject)
            {
                case InvalidCastException _:
                    switch (switchObject)
                    {
                        case NullReferenceException exception:
                            await CompletedTask();
                            var myexception = exception;
                            break;
                    }
                    break;
                case InvalidOperationException _:
                    switch (switchObject)
                    {
                        case NullReferenceException exception:
                            await CompletedTask();
                            var myexception = exception;
                            break;
                    }
                    break;
            }
        }
    }
}
";
            var expected = @"Passed";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact, WorkItem(25991, "https://github.com/dotnet/roslyn/issues/25991")]
        public void CompilerCrash02()
        {
            var source =
@"namespace Issue25991
{
    using System;
    using System.Threading.Tasks;

    public class CrashClass
    {
        public static void Main()
        {
            Console.WriteLine(""Passed"");
        }
        public async Task CompletedTask()
        {
        }
        public async Task OnCrash()
        {
            var switchObject = new object();
            switch (switchObject)
            {
                case InvalidCastException x1:
                    switch (switchObject)
                    {
                        case NullReferenceException exception:
                            await CompletedTask();
                            var myexception1 = x1;
                            var myexception = exception;
                            break;
                    }
                    break;
                case InvalidOperationException x1:
                    switch (switchObject)
                    {
                        case NullReferenceException exception:
                            await CompletedTask();
                            var myexception1 = x1;
                            var myexception = exception;
                            var x2 = switchObject;
                            break;
                    }
                    break;
            }
        }
    }
}
";
            var expected = @"Passed";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact, WorkItem(19905, "https://github.com/dotnet/roslyn/issues/19905")]
        public void FinallyEnteredFromExceptionalControlFlow()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task Run()
    {
        try
        {
            var tmp = await (new { task = Task.Run<string>(async () => { await Task.Delay(1); return """"; }) }).task;
            throw new Exception(tmp);
        }
        finally
        {
            Console.Write(0);
        }
    }
}

class Driver
{
    static void Main()
    {
        var t = new TestCase();
        try
        {
            t.Run().Wait();
        }
        catch (Exception)
        {
            Console.Write(1);
        }
    }
}";
            var expectedOutput = @"01";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            base.CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(38543, "https://github.com/dotnet/roslyn/issues/38543")]
        public void AsyncLambdaWithAwaitedTasksInTernary()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static Task M(bool b) => M2(async () =>
        b ? await Task.Delay(1) : await Task.Delay(2));

    static T M2<T>(Func<T> f) => f();
}";
            // The diagnostic message isn't great, but it is correct that we report an error
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics(
                // (8,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         b ? await Task.Delay(1) : await Task.Delay(2));
                Diagnostic(ErrorCode.ERR_IllegalStatement, "b ? await Task.Delay(1) : await Task.Delay(2)").WithLocation(8, 9)
                );
        }

        [Fact]
        [WorkItem(30956, "https://github.com/dotnet/roslyn/issues/30956")]
        public void GetAwaiterBoxingConversion_01()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

interface IAwaitable { }
struct StructAwaitable : IAwaitable { }

static class Extensions
{
    public static TaskAwaiter GetAwaiter(this IAwaitable x)
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
        await new StructAwaitable();
    }
}";
            var comp = CSharpTestBase.CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "StructAwaitable");
        }

        [Fact]
        [WorkItem(30956, "https://github.com/dotnet/roslyn/issues/30956")]
        public void GetAwaiterBoxingConversion_02()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

struct StructAwaitable { }

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
        StructAwaitable? s = new StructAwaitable();
        await s;
    }
}";
            var comp = CSharpTestBase.CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "StructAwaitable");
        }
    }
}
