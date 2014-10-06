// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

            compOptions = compOptions ?? TestOptions.ReleaseExe;

            IEnumerable<MetadataReference> asyncRefs = new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef };
            references = (references != null) ? references.Concat(asyncRefs) : asyncRefs;

            return CreateCompilationWithMscorlib45(source, options: compOptions, references: references);
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput, IEnumerable<MetadataReference> references = null, TestEmitters emitOptions = TestEmitters.All, CSharpCompilationOptions compOptions = null)
        {
            SynchronizationContext.SetSynchronizationContext(null);

            var compilation = this.CreateCompilation(source, references: references, compOptions: compOptions);
            return base.CompileAndVerify(compilation, expectedOutput: expectedOutput, emitOptions: emitOptions);
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
                var stateMachine = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test").GetMember<NamedTypeSymbol>("<F>d__1");
                Assert.Equal(TypeKind.Struct, stateMachine.TypeKind);
            }, expectedOutput: "123");


            options = TestOptions.DebugExe;
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
            CompileAndVerify(source, expectedOutput:"0");
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
            CompileAndVerify(source, "0", compOptions: TestOptions.UnsafeReleaseExe);
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
            CompileAndVerify(source, "0", compOptions: TestOptions.UnsafeDebugExe);
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
            CompileAndVerify(source, "0", compOptions: TestOptions.UnsafeDebugExe);
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
            CompileAndVerify(source, "0", compOptions: TestOptions.UnsafeDebugExe);
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
            CompileAndVerify(source, "0", compOptions: TestOptions.UnsafeDebugExe);
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
            CompileAndVerify(source, "0", compOptions: TestOptions.UnsafeDebugExe);
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
            CompileAndVerify(source, "0", compOptions: TestOptions.UnsafeDebugExe);
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
  // Code size      184 (0xb8)
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
    IL_0008:  brfalse.s  IL_005e
    IL_000a:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
    IL_000f:  ldsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
    IL_0014:  dup
    IL_0015:  brtrue.s   IL_002a
    IL_0017:  pop
    IL_0018:  ldnull
    IL_0019:  ldftn      ""int Test.<F>b__0(object)""
    IL_001f:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
    IL_0024:  dup
    IL_0025:  stsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
    IL_002a:  callvirt   ""System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)""
    IL_002f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0034:  stloc.2
    IL_0035:  ldloca.s   V_2
    IL_0037:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003c:  brtrue.s   IL_007a
    IL_003e:  ldarg.0
    IL_003f:  ldc.i4.0
    IL_0040:  dup
    IL_0041:  stloc.0
    IL_0042:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_0047:  ldarg.0
    IL_0048:  ldloc.2
    IL_0049:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_004e:  ldarg.0
    IL_004f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
    IL_0054:  ldloca.s   V_2
    IL_0056:  ldarg.0
    IL_0057:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__1)""
    IL_005c:  leave.s    IL_00b7
    IL_005e:  ldarg.0
    IL_005f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_0064:  stloc.2
    IL_0065:  ldarg.0
    IL_0066:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_006b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0071:  ldarg.0
    IL_0072:  ldc.i4.m1
    IL_0073:  dup
    IL_0074:  stloc.0
    IL_0075:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_007a:  ldloca.s   V_2
    IL_007c:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0081:  ldloca.s   V_2
    IL_0083:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0089:  stloc.1
    IL_008a:  leave.s    IL_00a3
  }
  catch System.Exception
  {
    IL_008c:  stloc.3
    IL_008d:  ldarg.0
    IL_008e:  ldc.i4.s   -2
    IL_0090:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_0095:  ldarg.0
    IL_0096:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
    IL_009b:  ldloc.3
    IL_009c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a1:  leave.s    IL_00b7
  }
  IL_00a3:  ldarg.0
  IL_00a4:  ldc.i4.s   -2
  IL_00a6:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_00ab:  ldarg.0
  IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_00b1:  ldloc.1
  IL_00b2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b7:  ret
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
            var c = CompileAndVerify(source, expectedOutput: expected, compOptions: TestOptions.DebugExe);

            c.VerifyIL("Test.F", @"
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (Test.<F>d__1 V_0,
                System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> V_1)
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
  IL_0033:  ret
}
");

            c.VerifyIL("Test.<F>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      205 (0xcd)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
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
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0068
   -IL_000e:  nop
   -IL_000f:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
    IL_0014:  ldsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
    IL_0019:  dup
    IL_001a:  brtrue.s   IL_002f
    IL_001c:  pop
    IL_001d:  ldnull
    IL_001e:  ldftn      ""int Test.<F>b__0(object)""
    IL_0024:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
    IL_0029:  dup
    IL_002a:  stsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
    IL_002f:  callvirt   ""System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)""
    IL_0034:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0039:  stloc.3
    IL_003a:  ldloca.s   V_3
    IL_003c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0041:  brtrue.s   IL_0084
    IL_0043:  ldarg.0
    IL_0044:  ldc.i4.0
    IL_0045:  dup
    IL_0046:  stloc.0
    IL_0047:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_004c:  ldarg.0
    IL_004d:  ldloc.3
    IL_004e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_0053:  ldarg.0
    IL_0054:  stloc.s    V_5
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
    IL_005c:  ldloca.s   V_3
    IL_005e:  ldloca.s   V_5
    IL_0060:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__1)""
    IL_0065:  nop
    IL_0066:  leave.s    IL_00cc
    IL_0068:  ldarg.0
    IL_0069:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_006e:  stloc.3
    IL_006f:  ldarg.0
    IL_0070:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_0075:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_007b:  ldarg.0
    IL_007c:  ldc.i4.m1
    IL_007d:  dup
    IL_007e:  stloc.0
    IL_007f:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_0084:  ldloca.s   V_3
    IL_0086:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_008b:  stloc.s    V_4
    IL_008d:  ldloca.s   V_3
    IL_008f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0095:  ldloc.s    V_4
    IL_0097:  stloc.2
    IL_0098:  ldloc.2
    IL_0099:  stloc.1
    IL_009a:  leave.s    IL_00b7
  }
  catch System.Exception
  {
   ~IL_009c:  stloc.s    V_6
    IL_009e:  nop
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.s   -2
    IL_00a2:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
    IL_00ad:  ldloc.s    V_6
    IL_00af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00b4:  nop
    IL_00b5:  leave.s    IL_00cc
  }
 -IL_00b7:  ldarg.0
  IL_00b8:  ldc.i4.s   -2
  IL_00ba:  stfld      ""int Test.<F>d__1.<>1__state""
 ~IL_00bf:  ldarg.0
  IL_00c0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_00c5:  ldloc.1
  IL_00c6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00cb:  nop
  IL_00cc:  ret
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
  // Code size      190 (0xbe)
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
    IL_0008:  brfalse.s  IL_005e
    IL_000a:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
    IL_000f:  ldsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
    IL_0014:  dup
    IL_0015:  brtrue.s   IL_002a
    IL_0017:  pop
    IL_0018:  ldnull
    IL_0019:  ldftn      ""int Test.<F>b__0(object)""
    IL_001f:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
    IL_0024:  dup
    IL_0025:  stsfld     ""System.Func<int> Test.CS$<>9__CachedAnonymousMethodDelegate1""
    IL_002a:  callvirt   ""System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)""
    IL_002f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0034:  stloc.1
    IL_0035:  ldloca.s   V_1
    IL_0037:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003c:  brtrue.s   IL_007a
    IL_003e:  ldarg.0
    IL_003f:  ldc.i4.0
    IL_0040:  dup
    IL_0041:  stloc.0
    IL_0042:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_0047:  ldarg.0
    IL_0048:  ldloc.1
    IL_0049:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_004e:  ldarg.0
    IL_004f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__1.<>t__builder""
    IL_0054:  ldloca.s   V_1
    IL_0056:  ldarg.0
    IL_0057:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__1)""
    IL_005c:  leave.s    IL_00bd
    IL_005e:  ldarg.0
    IL_005f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_0064:  stloc.1
    IL_0065:  ldarg.0
    IL_0066:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter2""
    IL_006b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0071:  ldarg.0
    IL_0072:  ldc.i4.m1
    IL_0073:  dup
    IL_0074:  stloc.0
    IL_0075:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_007a:  ldloca.s   V_1
    IL_007c:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0081:  pop
    IL_0082:  ldloca.s   V_1
    IL_0084:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_008a:  ldc.i4.s   42
    IL_008c:  call       ""void System.Console.WriteLine(int)""
    IL_0091:  leave.s    IL_00aa
  }
  catch System.Exception
  {
    IL_0093:  stloc.2
    IL_0094:  ldarg.0
    IL_0095:  ldc.i4.s   -2
    IL_0097:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_009c:  ldarg.0
    IL_009d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__1.<>t__builder""
    IL_00a2:  ldloc.2
    IL_00a3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a8:  leave.s    IL_00bd
  }
  IL_00aa:  ldarg.0
  IL_00ab:  ldc.i4.s   -2
  IL_00ad:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_00b2:  ldarg.0
  IL_00b3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<F>d__1.<>t__builder""
  IL_00b8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00bd:  ret
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
  // Code size      194 (0xc2)
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
    IL_0008:  brfalse.s  IL_005e
    IL_000a:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
    IL_000f:  ldsfld     ""System.Action Test.CS$<>9__CachedAnonymousMethodDelegate1""
    IL_0014:  dup
    IL_0015:  brtrue.s   IL_002a
    IL_0017:  pop
    IL_0018:  ldnull
    IL_0019:  ldftn      ""void Test.<F>b__0(object)""
    IL_001f:  newobj     ""System.Action..ctor(object, System.IntPtr)""
    IL_0024:  dup
    IL_0025:  stsfld     ""System.Action Test.CS$<>9__CachedAnonymousMethodDelegate1""
    IL_002a:  callvirt   ""System.Threading.Tasks.Task System.Threading.Tasks.TaskFactory.StartNew(System.Action)""
    IL_002f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0034:  stloc.1
    IL_0035:  ldloca.s   V_1
    IL_0037:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_003c:  brtrue.s   IL_007a
    IL_003e:  ldarg.0
    IL_003f:  ldc.i4.0
    IL_0040:  dup
    IL_0041:  stloc.0
    IL_0042:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_0047:  ldarg.0
    IL_0048:  ldloc.1
    IL_0049:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter Test.<F>d__1.<>u__$awaiter2""
    IL_004e:  ldarg.0
    IL_004f:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<F>d__1.<>t__builder""
    IL_0054:  ldloca.s   V_1
    IL_0056:  ldarg.0
    IL_0057:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, Test.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter, ref Test.<F>d__1)""
    IL_005c:  leave.s    IL_00c1
    IL_005e:  ldarg.0
    IL_005f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter Test.<F>d__1.<>u__$awaiter2""
    IL_0064:  stloc.1
    IL_0065:  ldarg.0
    IL_0066:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter Test.<F>d__1.<>u__$awaiter2""
    IL_006b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0071:  ldarg.0
    IL_0072:  ldc.i4.m1
    IL_0073:  dup
    IL_0074:  stloc.0
    IL_0075:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_007a:  ldloca.s   V_1
    IL_007c:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0081:  ldloca.s   V_1
    IL_0083:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0089:  ldarg.0
    IL_008a:  ldfld      ""System.Threading.AutoResetEvent Test.<F>d__1.handle""
    IL_008f:  callvirt   ""bool System.Threading.EventWaitHandle.Set()""
    IL_0094:  pop
    IL_0095:  leave.s    IL_00ae
  }
  catch System.Exception
  {
    IL_0097:  stloc.2
    IL_0098:  ldarg.0
    IL_0099:  ldc.i4.s   -2
    IL_009b:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_00a0:  ldarg.0
    IL_00a1:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<F>d__1.<>t__builder""
    IL_00a6:  ldloc.2
    IL_00a7:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_00ac:  leave.s    IL_00c1
  }
  IL_00ae:  ldarg.0
  IL_00af:  ldc.i4.s   -2
  IL_00b1:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_00b6:  ldarg.0
  IL_00b7:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<F>d__1.<>t__builder""
  IL_00bc:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_00c1:  ret
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

            var comp = CSharpTestBaseBase.CreateCompilation(source, new[] { MscorlibRef }, TestOptions.ReleaseDll); // NOTE: 4.0, not 4.5, so it's missing the async helpers.

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
            var v = CompileAndVerify(source, null, compOptions: TestOptions.DebugDll);

            v.VerifyIL("Test.<F>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      222 (0xde)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                S[] V_2, //array
                int V_3,
                S& V_4,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
                int V_6,
                Test.<F>d__1 V_7,
                System.Exception V_8)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0067
    IL_000e:  nop
    IL_000f:  ldc.i4.s   10
    IL_0011:  newarr     ""S""
    IL_0016:  stloc.2
    IL_0017:  ldarg.0
    IL_0018:  ldloc.2
    IL_0019:  stfld      ""S[] Test.<F>d__1.<>7__wrap1""
    IL_001e:  ldarg.0
    IL_001f:  ldfld      ""S[] Test.<F>d__1.<>7__wrap1""
    IL_0024:  ldc.i4.1
    IL_0025:  ldelema    ""S""
    IL_002a:  stloc.s    V_4
    IL_002c:  call       ""System.Threading.Tasks.Task<int> Test.G()""
    IL_0031:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0036:  stloc.s    V_5
    IL_0038:  ldloca.s   V_5
    IL_003a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003f:  brtrue.s   IL_0084
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_004a:  ldarg.0
    IL_004b:  ldloc.s    V_5
    IL_004d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter0""
    IL_0052:  ldarg.0
    IL_0053:  stloc.s    V_7
    IL_0055:  ldarg.0
    IL_0056:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
    IL_005b:  ldloca.s   V_5
    IL_005d:  ldloca.s   V_7
    IL_005f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__1)""
    IL_0064:  nop
    IL_0065:  leave.s    IL_00dd
    IL_0067:  ldarg.0
    IL_0068:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter0""
    IL_006d:  stloc.s    V_5
    IL_006f:  ldarg.0
    IL_0070:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__1.<>u__$awaiter0""
    IL_0075:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_007b:  ldarg.0
    IL_007c:  ldc.i4.m1
    IL_007d:  dup
    IL_007e:  stloc.0
    IL_007f:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_0084:  ldloca.s   V_5
    IL_0086:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_008b:  stloc.s    V_6
    IL_008d:  ldloca.s   V_5
    IL_008f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0095:  ldloc.s    V_6
    IL_0097:  stloc.3
    IL_0098:  ldarg.0
    IL_0099:  ldfld      ""S[] Test.<F>d__1.<>7__wrap1""
    IL_009e:  ldc.i4.1
    IL_009f:  ldelema    ""S""
    IL_00a4:  ldloc.3
    IL_00a5:  call       ""int S.Mutate(int)""
    IL_00aa:  stloc.1
    IL_00ab:  leave.s    IL_00c8
  }
  catch System.Exception
  {
    IL_00ad:  stloc.s    V_8
    IL_00af:  nop
    IL_00b0:  ldarg.0
    IL_00b1:  ldc.i4.s   -2
    IL_00b3:  stfld      ""int Test.<F>d__1.<>1__state""
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
    IL_00be:  ldloc.s    V_8
    IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00c5:  nop
    IL_00c6:  leave.s    IL_00dd
  }
  IL_00c8:  ldarg.0
  IL_00c9:  ldc.i4.s   -2
  IL_00cb:  stfld      ""int Test.<F>d__1.<>1__state""
  IL_00d0:  ldarg.0
  IL_00d1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__1.<>t__builder""
  IL_00d6:  ldloc.1
  IL_00d7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00dc:  nop
  IL_00dd:  ret
}
            ");
        }
    }
}
