// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LocalFunctionTests : CSharpTestBase
    {
        private readonly CSharpParseOptions _parseOptions = TestOptions.Regular.WithLocalFunctionsFeature();

        [Fact]
        public void EndToEnd()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void Local()
        {
            Console.WriteLine(""Hello, world!"");
        }
        Local();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
Hello, world!
");
        }

        [Fact]
        public void ExpressionBody()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int Local() => 2;
        Console.WriteLine(Local());
        void VoidLocal() => Console.WriteLine(2);
        VoidLocal();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
2
");
        }

        [Fact]
        public void StandardMethodFeatures()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    static void Main(string[] args)
    {
        void Params(params int[] x)
        {
            Console.WriteLine(string.Join("","", x));
        }
        void RefOut(ref int x, out int y)
        {
            y = ++x;
        }
        void NamedOptional(int x = 2)
        {
            Console.WriteLine(x);
        }
        void CallerMemberName([CallerMemberName] string s = null)
        {
            Console.WriteLine(s);
        }
        void LocalFuncName()
        {
            CallerMemberName();
        }
        Params(2);
        int a = 1;
        int b;
        RefOut(ref a, out b);
        Console.WriteLine(a);
        Console.WriteLine(b);
        NamedOptional(x: 2);
        NamedOptional();
        LocalFuncName();
        CallerMemberName();
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
2
2
2
2
LocalFuncName
Main
");
        }

        [Fact]
        public void BadStandardMethodFeatures()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    static void Main(string[] args)
    {
        void Params(params int x)
        {
            Console.WriteLine(x);
        }
        void RefOut(ref int x = 2)
        {
            x++;
        }
        void NamedOptional(string x = 2)
        {
            Console.WriteLine(x);
        }
        void CallerMemberName([CallerMemberName] int s = 2)
        {
            Console.WriteLine(s);
        }
    }
}
";
            // TODO: SourceComplexParameterSymbol reports to AddDeclarationDiagnostics, which is frozen at the time local functions are bound.
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib45(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (9,21): error CS0225: The params parameter must be a single dimensional array
    //         void Params(params int x)
    Diagnostic(ErrorCode.ERR_ParamsMustBeArray, "params").WithLocation(9, 21),
    // (13,21): error CS1741: A ref or out parameter cannot have a default value
    //         void RefOut(ref int x = 2)
    Diagnostic(ErrorCode.ERR_RefOutDefaultValue, "ref").WithLocation(13, 21),
    // (17,35): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type 'string'
    //         void NamedOptional(string x = 2)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("int", "string").WithLocation(17, 35),
    // (21,32): error CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type 'string' to type 'int'
    //         void CallerMemberName([CallerMemberName] int s = 2)
    Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithArguments("string", "int").WithLocation(21, 32)
                );
        }

        [Fact]
        public void Property()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static int Foo1
    {
        get
        {
            int Local()
            {
                return 2;
            }
            return Local();
        }
    }

    static int Foo2
    {
        get
        {
            int a = 2;
            IEnumerable<int> Local()
            {
                yield return a;
            }
            foreach (var x in Local())
            {
                return x;
            }
            return 0;
        }
    }

    static void Main(string[] args)
    {
        Console.WriteLine(Foo1);
        Console.WriteLine(Foo2);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
2
");
        }

        [Fact]
        public void Delegate()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int Local(int x) => x;
        Func<int, int> local = Local;
        Console.WriteLine(local(2));
        void Local2()
        {
            Console.WriteLine(2);
        }
        var local2 = new Action(Local2);
        local2();
        var local3 = (Action)Local2;
        local3();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
2
2
");
        }

        [Fact]
        public void Closure()
        {
            var source = @"
using System;

class Program
{
    int _a;
    static int _sa;

    static void Print(object a)
    {
        Console.Write(' ');
        Console.Write(a);
    }

    static void Print(int a)
    {
        Console.Write(' ');
        Console.Write(a);
    }

    static void A(int y)
    {
        int x = 1;
        void Local()
        {
            Print(x); Print(y);
        }
        Local();
        Print(x); Print(y);
        x = 3;
        y = 4;
        Local();
        Print(x); Print(y);
        void Local2()
        {
            Print(x); Print(y);
            x += 2;
            y += 2;
            Print(x); Print(y);
        }
        Local2();
        Print(x); Print(y);
    }

    void B()
    {
        _a = 2;
        void Local()
        {
            Print(_a);
            _a++;
            Print(_a);
        }
        Print(_a);
        Local();
        Print(_a);
    }

    void C()
    {
        _sa = 2;
        void Local()
        {
            Print(_sa);
            _sa++;
            Print(_sa);
        }
        Print(_sa);
        Local();
        Print(_sa);
    }

    class Gen<T1>
    {
        T1 t1;

        public Gen(T1 t1)
        {
            this.t1 = t1;
        }

        public void D<T2>(T2 t2)
        {
            T2 Local(T1 x)
            {
                Print(x);
                Print(t1);
                t1 = (T1)(object)((int)(object)x + 2);
                t2 = (T2)(object)x;
                return (T2)(object)((int)(object)t2 + 4);
            }
            Print(t1);
            Print(t2);
            Print(Local(t1));
            Print(t1);
            Print(t2);
        }
    }

    static void E()
    {
        int a = 2;
        void M1()
        {
            int b = a;
            Action M2 = () =>
            {
                int c = b;
                void M3()
                {
                    int d = c;
                    Print(d);
                }
                M3();
            };
            M2();
        }
        M1();
    }

    static void Main(string[] args)
    {
        A(2);
        Console.WriteLine();
        new Program().B(); // this-only closure
        Console.WriteLine();
        new Program().C(); // static closure in instance
        Console.WriteLine();
        new Gen<int>(2).D<int>(3); // generics
        Console.WriteLine();
        E(); // Interaction between functions and lambdas
        Console.WriteLine();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
 1 2 1 2 3 4 3 4 3 4 5 6 5 6
 2 2 3 3
 2 2 3 3
 2 3 2 2 6 4 2
 2
");
        }

        [Fact]
        public void ClosureTripleNested()
        {
            var source = @"
using System;

class Program
{
    static void Print(int a)
    {
        Console.Write(' ');
        Console.Write(a);
    }

    static void A()
    {
        int a = 0;
        void M1()
        {
            int b = a;
            void M2()
            {
                int c = b;
                void M3()
                {
                    Print(c);
                    c = 2;
                }
                Print(b);
                M3();
                Print(c);
                b = 2;
            }
            Print(a);
            M2();
            Print(b);
            a = 2;
        }
        M1();
        Print(a);
    }

    static void B()
    {
        int a = 0;
        void M1()
        {
            int b = a;
            void M2()
            {
                void M3()
                {
                    Print(b);
                    b = 2;
                }
                M3();
                Print(b);
            }
            Print(a);
            M2();
            Print(b);
            a = 2;
        }
        M1();
        Print(a);
    }

    static void C()
    {
        int a = 0;
        void M1()
        {
            void M2()
            {
                int c = a;
                void M3()
                {
                    Print(c);
                    c = 2;
                }
                Print(a);
                M3();
                Print(c);
                a = 2;
            }
            M2();
            Print(a);
        }
        M1();
        Print(a);
    }

    static void D()
    {
        void M1()
        {
            int b = 0;
            void M2()
            {
                int c = b;
                void M3()
                {
                    Print(c);
                    c = 2;
                }
                Print(b);
                M3();
                Print(c);
                b = 2;
            }
            M2();
            Print(b);
        }
        M1();
    }

    static void E()
    {
        int a = 0;
        void M1()
        {
            void M2()
            {
                void M3()
                {
                    Print(a);
                    a = 2;
                }
                M3();
                Print(a);
            }
            M2();
            Print(a);
        }
        M1();
        Print(a);
    }

    static void F()
    {
        void M1()
        {
            int b = 0;
            void M2()
            {
                void M3()
                {
                    Print(b);
                    b = 2;
                }
                M3();
                Print(b);
            }
            M2();
            Print(b);
        }
        M1();
    }

    static void G()
    {
        void M1()
        {
            void M2()
            {
                int c = 0;
                void M3()
                {
                    Print(c);
                    c = 2;
                }
                M3();
                Print(c);
            }
            M2();
        }
        M1();
    }

    static void Main(string[] args)
    {
        A();
        Console.WriteLine();
        B();
        Console.WriteLine();
        C();
        Console.WriteLine();
        D();
        Console.WriteLine();
        E();
        Console.WriteLine();
        F();
        Console.WriteLine();
        G();
        Console.WriteLine();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
 0 0 0 2 2 2
 0 0 2 2 2
 0 0 2 2 2
 0 0 2 2
 0 2 2 2
 0 2 2
 0 2
");
        }

        [Fact]
        public void InstanceClosure()
        {
            var source = @"
using System;

class Program
{
    int w;

    int A(int y)
    {
        int x = 1;
        int Local1(int z)
        {
            int Local2()
            {
                return Local1(x + y + w);
            }
            return z != -1 ? z : Local2();
        }
        return Local1(-1);
    }

    static void Main(string[] args)
    {
        var prog = new Program();
        prog.w = 3;
        Console.WriteLine(prog.A(2));
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
6
");
        }

        [Fact]
        public void SelfClosure()
        {
            var source = @"
using System;

class Program
{
    static int Test()
    {
        int x = 2;
        int Local1(int y)
        {
            int Local2()
            {
                return Local1(x);
            }
            return y != 0 ? y : Local2();
        }
        return Local1(0);
    }

    static void Main(string[] args)
    {
        Console.WriteLine(Test());
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
");
        }

        [Fact]
        public void Recursion()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        void Foo(int depth)
        {
            if (depth > 10)
            {
                Console.WriteLine(2);
                return;
            }
            Foo(depth + 1);
        }
        Foo(0);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
");
        }

        [Fact]
        public void MutualRecursion()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void Foo(int depth)
        {
            if (depth > 10)
            {
                Console.WriteLine(2);
                return;
            }
            void Bar(int depth2)
            {
                Foo(depth2 + 1);
            }
            Bar(depth + 1);
        }
        Foo(0);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
");
        }

        [Fact]
        public void Iterator()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        IEnumerable<int> Local()
        {
            yield return 2;
        }
        IEnumerable<T> LocalGeneric<T>(T val)
        {
            yield return val;
        }
        IEnumerable LocalNongen()
        {
            yield return 2;
        }
        IEnumerator LocalEnumerator()
        {
            yield return 2;
        }
        Console.WriteLine(string.Join("","", Local()));
        Console.WriteLine(string.Join("","", LocalGeneric(2)));
        foreach (int x in LocalNongen())
        {
            Console.WriteLine(x);
        }
        var y = LocalEnumerator();
        y.MoveNext();
        Console.WriteLine(y.Current);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
2
2
2
");
        }

        [Fact]
        public void Async()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        async Task<int> Local()
        {
            return await Task.FromResult(2);
        }
        Console.WriteLine(Local().Result);
        async Task<int> LocalParam(int x)
        {
            return await Task.FromResult(x);
        }
        Console.WriteLine(LocalParam(2).Result);
        async Task<T> LocalGeneric<T>(T x)
        {
            return await Task.FromResult(x);
        }
        Console.WriteLine(LocalGeneric(2).Result);
        // had bug with parser where 'async [keyword]' didn't parse.
        async void LocalVoid()
        {
            Console.WriteLine(2);
        }
        LocalVoid();

        Task<int> Fun(int x)
        {
            return Task.FromResult(x);
        }
        async Task<int> AwaitAwait()
        {
            var a = Fun(2);
            await Fun(await a);
            return await Fun(await a);
        }
        Console.WriteLine(AwaitAwait().Result);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
2
2
2
2
2
");
        }

        [Fact]
        public void AsyncKeyword()
        {
            var source = @"
using System;

struct async
{
    public override string ToString() => ""2"";
}
struct await
{
    public override string ToString() => ""2"";
}

class Program
{
    static string A()
    {
        async async()
        {
            return new async();
        }
        return async().ToString();
    }
    static string B()
    {
        string async()
        {
            return ""2"";
        }
        return async();
    }
    static string C()
    {
        async Foo()
        {
            return new async();
        }
        return Foo().ToString();
    }
    static string D()
    {
        await Fun(await x)
        {
            return x;
        }
        return Fun(new await()).ToString();
    }

    static void Main(string[] args)
    {
        Console.WriteLine(A());
        Console.WriteLine(B());
        Console.WriteLine(C());
        Console.WriteLine(D());
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
2
2
2
2
");
        }

        [Fact]
        public void AsyncUnsafeKeyword()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static string A()
    {
        async unsafe Task<int> async()
        {
            return 2;
        }
        return async().Result.ToString();
    }
    static string B()
    {
        unsafe async Task<int> async()
        {
            return 2;
        }
        return async().Result.ToString();
    }

    static void Main(string[] args)
    {
        Console.WriteLine(A());
        Console.WriteLine(B());
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithAllowUnsafe(true).WithWarningLevel(0),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
2
2
");
        }

        [Fact]
        public void Generic()
        {
            var source = @"
using System;

class Program
{
    // No closure. Return 'valu'.
    static T A1<T>(T val)
    {
        T Local(T valu)
        {
            return valu;
        }
        return Local(val);
    }
    static int B1(int val)
    {
        T Local<T>(T valu)
        {
            return valu;
        }
        return Local(val);
    }
    static T1 C1<T1>(T1 val)
    {
        T2 Local<T2>(T2 valu)
        {
            return valu;
        }
        return Local<T1>(val);
    }
    // General closure. Return 'val'.
    static T A2<T>(T val)
    {
        T Local(T valu)
        {
            return val;
        }
        return Local(val);
    }
    static int B2(int val)
    {
        T Local<T>(T valu)
        {
            return (T)(object)val;
        }
        return Local(val);
    }
    static T1 C2<T1>(T1 val)
    {
        T2 Local<T2>(T2 valu)
        {
            return (T2)(object)val;
        }
        return Local<T1>(val);
    }
    // This-only closure. Return 'field'.
    int field = 2;
    T A3<T>(T val)
    {
        T Local(T valu)
        {
            return (T)(object)field;
        }
        return Local(val);
    }
    int B3(int val)
    {
        T Local<T>(T valu)
        {
            return (T)(object)field;
        }
        return Local(val);
    }
    T1 C3<T1>(T1 val)
    {
        T2 Local<T2>(T2 valu)
        {
            return (T2)(object)field;
        }
        return Local<T1>(val);
    }
    static void Main(string[] args)
    {
        var program = new Program();
        Console.WriteLine(Program.A1(2));
        Console.WriteLine(Program.B1(2));
        Console.WriteLine(Program.C1(2));
        Console.WriteLine(Program.A2(2));
        Console.WriteLine(Program.B2(2));
        Console.WriteLine(Program.C2(2));
        Console.WriteLine(program.A3(2));
        Console.WriteLine(program.B3(2));
        Console.WriteLine(program.C3(2));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
2
2
2
2
2
2
2
2
2
");
        }

        [Fact]
        public void GenericConstraint()
        {
            var source = @"
using System;

class Program
{
    static T A<T>(T val) where T : struct
    {
        T Local(T valu)
        {
            return valu;
        }
        return Local(val);
    }
    static int B(int val)
    {
        T Local<T>(T valu) where T : struct
        {
            return valu;
        }
        return Local(val);
    }
    static T1 C<T1>(T1 val) where T1 : struct
    {
        T2 Local<T2>(T2 valu) where T2 : struct
        {
            return valu;
        }
        return Local(val);
    }
    static object D(object val)
    {
        T Local<T>(T valu) where T : object
        {
            return valu;
        }
        return Local(val);
    }
    static void Main(string[] args)
    {
        Console.WriteLine(A(2));
        Console.WriteLine(B(2));
        Console.WriteLine(C(2));
        Console.WriteLine(D(2));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
2
2
2
2
");
        }

        [Fact]
        public void GenericTripleNestedNoClosure()
        {
            var source = @"
using System;

class Program
{
    // Name of method is T[outer][middle][inner] where brackets are g=generic n=nongeneric
    // One generic
    static T1 Tgnn<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngn(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tnng(int a)
    {
        int Local(int aa)
        {
            T1 Local2<T1>(T1 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Two generic
    static T1 Tggn<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T2 Local2(T2 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 Tgng<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngg(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Three generic
    static T1 Tggg<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static void Main(string[] args)
    {
        Console.WriteLine(Program.Tgnn(2));
        Console.WriteLine(Program.Tngn(2));
        Console.WriteLine(Program.Tnng(2));
        Console.WriteLine(Program.Tggn(2));
        Console.WriteLine(Program.Tgng(2));
        Console.WriteLine(Program.Tngg(2));
        Console.WriteLine(Program.Tggg(2));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
2
2
2
2
2
2
2
");
        }

        [Fact]
        public void GenericTripleNestedMiddleClosure()
        {
            var source = @"
using System;

class Program
{
    // Name of method is T[outer][middle][inner] where brackets are g=generic n=nongeneric
    // One generic
    static T1 Tgnn<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return (T1)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngn(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return (T1)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tnng(int a)
    {
        int Local(int aa)
        {
            T1 Local2<T1>(T1 aaa)
            {
                return (T1)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Two generic
    static T1 Tggn<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T2 Local2(T2 aaa)
            {
                return (T2)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 Tgng<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return (T2)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngg(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return (T2)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Three generic
    static T1 Tggg<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                return (T3)(object)aa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static void Main(string[] args)
    {
        Console.WriteLine(Program.Tgnn(2));
        Console.WriteLine(Program.Tngn(2));
        Console.WriteLine(Program.Tnng(2));
        Console.WriteLine(Program.Tggn(2));
        Console.WriteLine(Program.Tgng(2));
        Console.WriteLine(Program.Tngg(2));
        Console.WriteLine(Program.Tggg(2));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
2
2
2
2
2
2
2
");
        }

        [Fact]
        public void GenericTripleNestedOuterClosure()
        {
            var source = @"
using System;

class Program
{
    // Name of method is T[outer][middle][inner] where brackets are g=generic n=nongeneric
    // One generic
    static T1 Tgnn<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return (T1)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngn(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T1 Local2(T1 aaa)
            {
                return (T1)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tnng(int a)
    {
        int Local(int aa)
        {
            T1 Local2<T1>(T1 aaa)
            {
                return (T1)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Two generic
    static T1 Tggn<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T2 Local2(T2 aaa)
            {
                return (T2)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 Tgng<T1>(T1 a)
    {
        T1 Local(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return (T2)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tngg(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            T2 Local2<T2>(T2 aaa)
            {
                return (T2)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    // Three generic
    static T1 Tggg<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                return (T3)(object)a;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static void Main(string[] args)
    {
        Console.WriteLine(Program.Tgnn(2));
        Console.WriteLine(Program.Tngn(2));
        Console.WriteLine(Program.Tnng(2));
        Console.WriteLine(Program.Tggn(2));
        Console.WriteLine(Program.Tgng(2));
        Console.WriteLine(Program.Tngg(2));
        Console.WriteLine(Program.Tggg(2));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
2
2
2
2
2
2
2
");
        }

        [Fact]
        public void GenericTripleNestedNoClosureLambda()
        {
            var source = @"
using System;

class Program
{
    // Name of method is T[outer][middle][inner] where brackets are g=generic n=nongeneric
    // One generic
    static T1 Tgnn<T1>(T1 a)
    {
        Func<T1, T1> Local = aa =>
        {
            Func<T1, T1> Local2 = aaa =>
            {
                return aaa;
            };
            return Local2(aa);
        };
        return Local(a);
    }
    static int Tngn(int a)
    {
        T1 Local<T1>(T1 aa)
        {
            Func<T1, T1> Local2 = aaa =>
            {
                return aaa;
            };
            return Local2(aa);
        }
        return Local(a);
    }
    static int Tnng(int a)
    {
        Func<int, int> Local = aa =>
        {
            T1 Local2<T1>(T1 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        };
        return Local(a);
    }
    // Two generic
    static T1 Tggn<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            Func<T2, T2> Local2 = aaa =>
            {
                return aaa;
            };
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 Tgng<T1>(T1 a)
    {
        Func<T1, T1> Local = aa =>
        {
            T2 Local2<T2>(T2 aaa)
            {
                return aaa;
            }
            return Local2(aa);
        };
        return Local(a);
    }
    // Tngg and Tggg are impossible with lambdas
    static void Main(string[] args)
    {
        Console.WriteLine(Program.Tgnn(2));
        Console.WriteLine(Program.Tngn(2));
        Console.WriteLine(Program.Tnng(2));
        Console.WriteLine(Program.Tggn(2));
        Console.WriteLine(Program.Tgng(2));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
2
2
2
2
2
");
        }

        [Fact]
        public void GenericUpperCall()
        {
            var source = @"
using System;

class Program
{
    static T1 InnerToOuter<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                if ((object)aaa == null)
                    return InnerToOuter((T3)new object());
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 InnerToMiddle<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                if ((object)aaa == null)
                    return InnerToMiddle((T3)new object());
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 InnerToOuterScoping<T1>(T1 a)
    {
        T2 Local<T2>(T2 aa)
        {
            T3 Local2<T3>(T3 aaa)
            {
                if ((object)aaa == null)
                    return (T3)(object)InnerToOuter((T1)new object());
                return aaa;
            }
            return Local2(aa);
        }
        return Local(a);
    }
    static T1 M1<T1>(T1 a)
    {
        T2 M2<T2>(T2 aa)
        {
            T2 x = aa;
            T3 M3<T3>(T3 aaa)
            {
                T4 M4<T4>(T4 aaaa)
                {
                    return (T4)(object)x;
                }
                return M4(aaa);
            }
            return M3(aa);
        }
        return M2(a);
    }
    // Tngg and Tggg are impossible with lambdas
    static void Main(string[] args)
    {
        Console.WriteLine(Program.InnerToOuter((object)null));
        Console.WriteLine(Program.InnerToMiddle((object)null));
        Console.WriteLine(Program.InnerToOuterScoping((object)null));
        Console.WriteLine(Program.M1(2));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
System.Object
System.Object
System.Object
2
");
        }

        [Fact]
        public void Dynamic()
        {
            object f = 0;
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        // TODO: Fix local functions with dynamic arguments
        //void Local(int x)
        //{
        //    Console.WriteLine(x);
        //}
        //dynamic val = 2;
        //Local(val);
        dynamic RetDyn()
        {
            return 2;
        }
        Console.WriteLine(RetDyn());
        var RetDynVar()
        {
            return (dynamic)2;
        }
        Console.WriteLine(RetDynVar());
    }
}
";
            var comp = CreateCompilationWithMscorlib45AndCSruntime(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
2
");
        }

        [Fact]
        public void Nameof()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void Local()
        {
        }
        Console.WriteLine(nameof(Local));
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
Local
");
        }

        [Fact]
        public void Shadows()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Local()
    {
        Console.WriteLine(""bad"");
    }

    static void Main(string[] args)
    {
        void Local()
        {
            Console.WriteLine(2);
        }
        Local();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
");
        }

        [Fact]
        public void Scoping()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void Local()
        {
            Console.WriteLine(2);
        }
        if (true)
        {
            Local();
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
");
        }

        [Fact]
        public void BadScoping()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        if (true)
        {
            void Local()
            {
                Console.WriteLine(2);
            }
        }
        Local();

        Local2();
        void Local2()
        {
            Console.WriteLine(2);
        }
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (15,9): error CS0103: The name 'Local' does not exist in the current context
    //         Local();
    Diagnostic(ErrorCode.ERR_NameNotInContext, "Local").WithArguments("Local").WithLocation(15, 9),
    // (17,9): error CS0841: Cannot use local variable 'Local2' before it is declared
    //         Local2();
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "Local2").WithArguments("Local2").WithLocation(17, 9)
                );
        }

        [Fact]
        public void NameConflict()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Duplicate() { }
        void Duplicate() { }
        int T;
        void Param(int T) { }
        void Generic<T>() { }
        int Conflict;
        void Conflict() { }
        void Conflict2() { }
        int Conflict2;
    }
}
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe.WithWarningLevel(0), parseOptions: _parseOptions).VerifyDiagnostics(
    // (7,14): error CS0128: A local variable named 'Duplicate' is already defined in this scope
    //         void Duplicate() { }
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "Duplicate").WithArguments("Duplicate").WithLocation(7, 14),
    // (9,24): error CS0136: A local or parameter named 'T' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         void Param(int T) { }
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "T").WithArguments("T").WithLocation(9, 24),
    // (10,22): error CS0136: A local or parameter named 'T' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         void Generic<T>() { }
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "T").WithArguments("T").WithLocation(10, 22),
    // (12,14): error CS0128: A local variable named 'Conflict' is already defined in this scope
    //         void Conflict() { }
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "Conflict").WithArguments("Conflict").WithLocation(12, 14),
    // (13,14): error CS0136: A local or parameter named 'Conflict2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         void Conflict2() { }
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "Conflict2").WithArguments("Conflict2").WithLocation(13, 14)
                );
        }

        [Fact]
        public void Unsafe()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void A()
    {
        unsafe void Local()
        {
            int x = 2;
            Console.WriteLine(*&x);
        }
        Local();
    }
    static unsafe void B()
    {
        int x = 2;
        unsafe void Local(int* y)
        {
            Console.WriteLine(*y);
        }
        Local(&x);
    }
    static unsafe void C()
    {
        int y = 2;
        int* x = &y;
        unsafe void Local()
        {
            Console.WriteLine(*x);
        }
        Local();
    }

    static void Main(string[] args)
    {
        A();
        B();
        C();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(true), parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
2
2
");
        }

        [Fact]
        public void BadUnsafe()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        void Local()
        {
            int x = 2;
            Console.WriteLine(*&x);
        }
        Local();
    }
    static unsafe void B()
    {
        void Local()
        {
            int x = 2;
            Console.WriteLine(*&x);
        }
        Local();
    }

    static void Main(string[] args)
    {
        A();
        B();
    }
}
";
            var option = TestOptions.ReleaseExe.WithAllowUnsafe(true);
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (11,32): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
    //             Console.WriteLine(*&x);
    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(11, 32),
    // (20,32): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
    //             Console.WriteLine(*&x);
    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(20, 32)
                );
        }

        [Fact]
        public void DefiniteAssignment()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        goto Label;
        int x = 2;
        void Local()
        {
            Console.WriteLine(x);
        }
        Label:
        Local();
    }
    static void B()
    {
        goto Label;
        int x = 2;
        void Local()
        {
            Console.WriteLine(x);
        }
        Label:
        Action foo = Local;
    }
    static void C()
    {
        goto Label;
        int x = 2;
        void Local()
        {
            Console.WriteLine(x);
        }
        Label:
        var bar = new Action(Local);
    }
    static void D()
    {
        void Local()
        {
        }
    }

    static void Main(string[] args)
    {
        A();
        B();
        C();
    }
}
";
            var option = TestOptions.ReleaseExe.WithWarningLevel(0);
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (15,9): error CS0165: Use of unassigned local variable 'Local'
    //         Local();
    Diagnostic(ErrorCode.ERR_UseDefViolation, "Local()").WithArguments("Local").WithLocation(15, 9),
    // (26,22): error CS0165: Use of unassigned local variable 'Local'
    //         Action foo = Local;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "Local").WithArguments("Local").WithLocation(26, 22),
    // (37,19): error CS0165: Use of unassigned local variable 'Local'
    //         var bar = new Action(Local);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "new Action(Local)").WithArguments("Local").WithLocation(37, 19)
                );
        }

        [Fact]
        public void BadClosures()
        {
            var source = @"
using System;

class Program
{
    int _a = 0;

    static void A(ref int x)
    {
        void Local()
        {
            Console.WriteLine(x);
        }
    }
    static void B(__arglist)
    {
        void Local()
        {
            Console.WriteLine(__arglist);
        }
    }
    static void C() // C and D produce different errors
    {
        void Local(__arglist)
        {
            Console.WriteLine(__arglist);
        }
    }
    static void D(__arglist)
    {
        void Local(__arglist)
        {
            Console.WriteLine(__arglist);
        }
    }
    static void E()
    {
        void Local()
        {
            Console.WriteLine(_a);
        }
    }

    static void Main()
    {
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (12,31): error CS1628: Cannot use ref or out parameter 'x' inside an anonymous method, lambda expression, or query expression
    //             Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x").WithArguments("x").WithLocation(12, 31),
    // (19,31): error CS4013: Instance of type 'RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle").WithLocation(19, 31),
    // (26,31): error CS0190: The __arglist construct is valid only within a variable argument method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_ArgsInvalid, "__arglist").WithLocation(26, 31),
    // (33,31): error CS4013: Instance of type 'RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle").WithLocation(33, 31),
    // (40,31): error CS0120: An object reference is required for the non-static field, method, or property 'Program._a'
    //             Console.WriteLine(_a);
    Diagnostic(ErrorCode.ERR_ObjectRequired, "_a").WithArguments("Program._a").WithLocation(40, 31)
                );
        }

        [Fact]
        public void BadStateMachine()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        IEnumerable<int> RefEnumerable(ref int x)
        {
            yield return x;
        }
        async Task<int> RefAsync(ref int x)
        {
            return await Task.FromResult(x);
        }
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib45(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (13,42): error CS1988: Async methods cannot have ref or out parameters
    //         async Task<int> RefAsync(ref int x)
    Diagnostic(ErrorCode.ERR_BadAsyncArgType, "x").WithLocation(13, 42),
    // (9,48): error CS1623: Iterators cannot have ref or out parameters
    //         IEnumerable<int> RefEnumerable(ref int x)
    Diagnostic(ErrorCode.ERR_BadIteratorArgType, "x").WithLocation(9, 48)
                );
        }

        [Fact]
        public void Extension()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int Local(this int x)
        {
            return x;
        }
        Console.WriteLine(Local(2));
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (8,13): error CS1106: Extension method must be defined in a non-generic static class
    //         int Local(this int x)
    Diagnostic(ErrorCode.ERR_BadExtensionAgg, "Local").WithLocation(8, 13)
                );
        }

        [Fact]
        public void BadModifiers()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        const void LocalConst()
        {
        }
        static void LocalStatic()
        {
        }
        readonly void LocalReadonly()
        {
        }
        volatile void LocalVolatile()
        {
        }
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (6,9): error CS0106: The modifier 'const' is not valid for this item
    //         const void LocalConst()
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "const").WithArguments("const").WithLocation(6, 9),
    // (9,9): error CS0106: The modifier 'static' is not valid for this item
    //         static void LocalStatic()
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "static").WithArguments("static").WithLocation(9, 9),
    // (12,9): error CS0106: The modifier 'readonly' is not valid for this item
    //         readonly void LocalReadonly()
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(12, 9),
    // (15,9): error CS0106: The modifier 'volatile' is not valid for this item
    //         volatile void LocalVolatile()
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "volatile").WithArguments("volatile").WithLocation(15, 9)
                );
        }

        [Fact]
        public void InferredReturn()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var Local()
        {
            return 2;
        }
        var LocalIf(bool cond)
        {
            if (cond)
            {
                return 2;
            }
            else
            {
                return 3;
            }
        }
        var LocalNest()
        {
            var Inner()
            {
                return 2;
            }
            return Inner();
        }
        var LocalVoid()
        {
            Console.WriteLine(2);
        }
        async var LocalAsyncRet()
        {
            return await Task.FromResult(2);
        }
        async var LocalAsyncVoid()
        {
            await Task.Yield();
            Console.WriteLine(2);
        }
        Console.WriteLine(Local());
        Console.WriteLine(LocalIf(true));
        Console.WriteLine(LocalNest());
        LocalVoid();
        Console.WriteLine(LocalAsyncRet().Result);
        LocalAsyncVoid().Wait();
    }
}
";
            var comp = CreateCompilationWithMscorlib45AndCSruntime(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
2
2
2
2
2
");
        }

        [Fact]
        public void BadInferredReturn()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        var Local()
        {
            return Local() + 1;
        }
        var LocalRec()
        {
            var Inner()
            {
                return LocalRec();
            }
            return Inner();
        }
        var IteratorReturn()
        {
            yield return 2;
        }
        var IteratorVoid()
        {
            yield break;
        }
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (6,9): error CS7019: Type of 'Local()' cannot be inferred since its initializer directly or indirectly refers to the definition.
    //         var Local()
    Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "var").WithArguments("Local()").WithLocation(6, 9),
    // (10,9): error CS7019: Type of 'LocalRec()' cannot be inferred since its initializer directly or indirectly refers to the definition.
    //         var LocalRec()
    Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "var").WithArguments("LocalRec()").WithLocation(10, 9),
    // (18,13): error CS1624: The body of 'IteratorReturn()' cannot be an iterator block because 'var' is not an iterator interface type
    //         var IteratorReturn()
    Diagnostic(ErrorCode.ERR_BadIteratorReturn, "IteratorReturn").WithArguments("IteratorReturn()", "var").WithLocation(18, 13),
    // (22,13): error CS1624: The body of 'IteratorVoid()' cannot be an iterator block because 'var' is not an iterator interface type
    //         var IteratorVoid()
    Diagnostic(ErrorCode.ERR_BadIteratorReturn, "IteratorVoid").WithArguments("IteratorVoid()", "var").WithLocation(22, 13)
                );
        }

        [Fact]
        public void ArglistIterator()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        IEnumerable<int> Local(__arglist)
        {
            yield return 2;
        }
        Console.WriteLine(string.Join("","", Local(__arglist())));
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (9,26): error CS1636: __arglist is not allowed in the parameter list of iterators
    //         IEnumerable<int> Local(__arglist)
    Diagnostic(ErrorCode.ERR_VarargsIterator, "Local").WithLocation(9, 26)
                );
        }

        [Fact]
        public void ForwardReference()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(Local());
        int Local() => 2;
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (8,27): error CS0841: Cannot use local variable 'Local' before it is declared
    //         Console.WriteLine(Local());
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "Local").WithArguments("Local").WithLocation(8, 27)
                );
        }

        [Fact]
        public void NoFeatureSwitch()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Local()
        {
        }
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics(
    // (6,9): error CS8058: Feature 'local functions' is only available in 'experimental' language version.
    //         void Local()
    Diagnostic(ErrorCode.ERR_FeatureIsExperimental, @"void Local()
        {
        }").WithArguments("local functions").WithLocation(6, 9)
                );
        }
    }
}
