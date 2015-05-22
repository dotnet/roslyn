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
    // (9,9): error CS0501: 'Program.Local1()' must declare a body because it is not marked abstract, extern, or partial
    //         void Local1()
    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, @"void Local1()
").WithArguments("Program.Local1()").WithLocation(9, 9),
    // (10,9): error CS0501: 'Program.Local2()' must declare a body because it is not marked abstract, extern, or partial
    //         void Local2();
    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "void Local2();").WithArguments("Program.Local2()").WithLocation(10, 9)
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
        public void PartialExpressionBody()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        void Local() => Console.Writ
    }
}
";
            var option = TestOptions.ReleaseExe.WithWarningLevel(0);
            CreateCompilationWithMscorlibAndSystemCore(source, options: option).VerifyDiagnostics(
    // (9,37): error CS1002: ; expected
    //         void Local() => Console.Writ
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(9, 37),
    // (9,33): error CS0117: 'Console' does not contain a definition for 'Writ'
    //         void Local() => Console.Writ
    Diagnostic(ErrorCode.ERR_NoSuchMember, "Writ").WithArguments("System.Console", "Writ").WithLocation(9, 33),
    // (9,25): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         void Local() => Console.Writ
    Diagnostic(ErrorCode.ERR_IllegalStatement, "Console.Writ").WithLocation(9, 25)
                );
        }
    }
}
