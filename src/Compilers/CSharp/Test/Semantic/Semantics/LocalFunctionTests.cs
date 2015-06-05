// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LocalFunctionTests : CSharpTestBase
    {
        private readonly CSharpParseOptions _parseOptions = TestOptions.Regular.WithFeatures(new SmallDictionary<string, string> { { "localFunctions", "true" } });

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
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
");
        }

        [Fact]
        public void StandardMethodFeatures()
        {
            var source = @"
using System;

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
        Params(2);
        int a = 1;
        int b;
        RefOut(ref a, out b);
        Console.WriteLine(a);
        Console.WriteLine(b);
        NamedOptional(x: 2);
        NamedOptional();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
2
2
2
2
");
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
        public void Closure()
        {
            var source = @"
using System;

class Program
{
    static void A(int y)
    {
        int x = 1;
        void Local()
        {
            Console.WriteLine(x);
            Console.WriteLine(y);
        }
        Local();
    }

    static void Main(string[] args)
    {
        A(2);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
1
2
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
        IEnumerable LocalNongen()
        {
            yield return 2;
        }
        IEnumerator LocalEnumerator()
        {
            yield return 2;
        }
        Console.WriteLine(string.Join("","", Local()));
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
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
2
");
        }

        [Fact]
        public void AsyncParam()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        async Task<int> Local(int x)
        {
            return await Task.FromResult(x);
        }
        Console.WriteLine(Local(2).Result);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
                parseOptions: _parseOptions);
            var comp = CompileAndVerify(compilation, expectedOutput: @"
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
    static void Main(string[] args)
    {
        T Local<T>(T val)
        {
            return val;
        }
        Console.WriteLine(Local<int>(2));
    }
}
";
            // TODO: Eventually support this
            var option = TestOptions.ReleaseExe.WithWarningLevel(0);
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (8,16): error CS1519: Invalid token '<T>' in class, struct, or interface member declaration
    //         T Local<T>(T val)
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "<T>").WithArguments("<T>").WithLocation(8, 16)
                );
        }

        [Fact]
        public void GenericClosure()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static T Outer<T>(T val)
    {
        T Local(T valu)
        {
            return valu;
        }
        return Local(val);
    }
    static void Main(string[] args)
    {
        Console.WriteLine(Outer(2));
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            var verify = CompileAndVerify(comp, expectedOutput: @"
2
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
using System.Collections.Generic;

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
            var option = TestOptions.ReleaseExe.WithWarningLevel(0).WithAllowUnsafe(true);
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (12,32): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
    //             Console.WriteLine(*&x);
    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(12, 32),
    // (21,32): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
    //             Console.WriteLine(*&x);
    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(21, 32)
                );
        }

        [Fact]
        public void BadClosures()
        {
            var source = @"
using System;

class Program
{
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
}
";
            var option = TestOptions.ReleaseExe.WithWarningLevel(0);
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (10,31): error CS1628: Cannot use ref or out parameter 'x' inside an anonymous method, lambda expression, or query expression
    //             Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x").WithArguments("x").WithLocation(10, 31),
    // (17,31): error CS4013: Instance of type 'RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle").WithLocation(17, 31),
    // (24,31): error CS0190: The __arglist construct is valid only within a variable argument method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_ArgsInvalid, "__arglist").WithLocation(24, 31),
    // (31,31): error CS4013: Instance of type 'RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle").WithLocation(31, 31)
                );
        }

        [Fact]
        public void ByRefIterator()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        IEnumerable<int> Local(ref int x)
        {
            yield return x;
        }
        int y = 2;
        Console.WriteLine(string.Join("","", Local(ref y)));
    }
}
";
            var option = TestOptions.ReleaseExe.WithWarningLevel(0);
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (9,40): error CS1623: Iterators cannot have ref or out parameters
    //         IEnumerable<int> Local(ref int x)
    Diagnostic(ErrorCode.ERR_BadIteratorArgType, "x").WithLocation(9, 40)
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
            var option = TestOptions.ReleaseExe.WithWarningLevel(0);
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
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(Local());
        int Local() => 2;
    }
}
";
            var option = TestOptions.ReleaseExe.WithWarningLevel(0);
            CreateCompilationWithMscorlibAndSystemCore(source, options: option, parseOptions: _parseOptions).VerifyDiagnostics(
    // (9,27): error CS0841: Cannot use local variable 'Local' before it is declared
    //         Console.WriteLine(Local());
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "Local").WithArguments("Local").WithLocation(9, 27)
                );
        }
    }
}
