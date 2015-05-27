// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LocalFunctionTests : CSharpTestBase
    {
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
            var comp = CompileAndVerify(source, expectedOutput: @"
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
            var comp = CompileAndVerify(source, expectedOutput: @"
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
            var comp = CompileAndVerify(source, expectedOutput: @"
2
2
2
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
            var comp = CompileAndVerify(source, expectedOutput: @"
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
            var comp = CompileAndVerify(source, expectedOutput: @"
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
    int x;

    void A(int y)
    {
        void Local()
        {
            A(x + y);
        }
        if (y != 0)
        {
            Console.WriteLine(y);
        }
        else
        {
            Local();
        }
    }

    static void Main(string[] args)
    {
        var prog = new Program();
        prog.x = 2;
        prog.A(0);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"
2
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
            var comp = CompileAndVerify(source, expectedOutput: @"
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
            var comp = CompileAndVerify(source, expectedOutput: @"
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
            var comp = CompileAndVerify(source, expectedOutput: @"
2
");
        }

        [Fact]
        public void Enumerator()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        IEnumerable<int> Local()
        {
            yield return 2;
        }
        Console.WriteLine(string.Join("","", Local()));
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"
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
            var compilation = CreateCompilationWithMscorlib45(source, options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));
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
            var compilation = CreateCompilationWithMscorlib45(source, options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));
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
            CreateCompilationWithMscorlibAndSystemCore(source, options: option).VerifyDiagnostics(
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
            var comp = CompileAndVerify(source, expectedOutput: @"
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
            var comp = CompileAndVerify(source, expectedOutput: @"
2
");
        }

        [Fact]
        public void NoBody()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        void Local1()
        void Local2();
    }
}
";
            var option = TestOptions.ReleaseExe.WithWarningLevel(0);
            CreateCompilationWithMscorlibAndSystemCore(source, options: option).VerifyDiagnostics(
    // (9,22): error CS1002: ; expected
    //         void Local1()
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(9, 22),
    // (9,14): error CS0501: 'Program.Local1()' must declare a body because it is not marked abstract, extern, or partial
    //         void Local1()
    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "Local1").WithArguments("Program.Local1()").WithLocation(9, 14),
    // (10,14): error CS0501: 'Program.Local2()' must declare a body because it is not marked abstract, extern, or partial
    //         void Local2();
    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "Local2").WithArguments("Program.Local2()").WithLocation(10, 14)
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
            CreateCompilationWithMscorlibAndSystemCore(source, options: option).VerifyDiagnostics(
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
            CreateCompilationWithMscorlibAndSystemCore(source, options: option).VerifyDiagnostics(
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
            CreateCompilationWithMscorlibAndSystemCore(source, options: option).VerifyDiagnostics(
    // (9,27): error CS0841: Cannot use local variable 'Local' before it is declared
    //         Console.WriteLine(Local());
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "Local").WithArguments("Local").WithLocation(9, 27)
                );
        }
    }
}
