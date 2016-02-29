// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    static class LocalFunctionTestsExt
    {
        public static IMethodSymbol FindLocalFunction(this CommonTestBase.CompilationVerifier verifier, string localFunctionName)
        {
            localFunctionName = (char)GeneratedNameKind.LocalFunction + "__" + localFunctionName;
            var methods = verifier.TestData.GetMethodsByName();
            IMethodSymbol result = null;
            foreach (var kvp in methods)
            {
                if (kvp.Key.Contains(localFunctionName))
                {
                    Assert.Null(result); // more than one name matched
                    result = kvp.Value.Method;
                }
            }
            Assert.NotNull(result); // no methods matched
            return result;
        }
    }

    public class LocalFunctionTests : CSharpTestBase
    {
        private readonly CSharpParseOptions _parseOptions = TestOptions.Regular.WithLocalFunctionsFeature();

        CompilationVerifier VerifyOutput(string source, string output, CSharpCompilationOptions options)
        {
            var comp = CreateCompilationWithMscorlib45AndCSruntime(source, options: options, parseOptions: _parseOptions);
            return CompileAndVerify(comp, expectedOutput: output).VerifyDiagnostics(); // no diagnostics
        }

        CompilationVerifier VerifyOutput(string source, string output)
        {
            var comp = CreateCompilationWithMscorlib45AndCSruntime(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            return CompileAndVerify(comp, expectedOutput: output).VerifyDiagnostics(); // no diagnostics
        }

        CompilationVerifier VerifyOutputInMain(string methodBody, string output, params string[] usings)
        {
            for (var i = 0; i < usings.Length; i++)
            {
                usings[i] = "using " + usings[i] + ";";
            }
            var usingBlock = string.Join(Environment.NewLine, usings);
            var source = usingBlock + @"
class Program
{
    static void Main()
    {
" + methodBody + @"
    }
}";
            return VerifyOutput(source, output);
        }

        void VerifyDiagnostics(string source, params DiagnosticDescription[] expected)
        {
            var comp = CreateCompilationWithMscorlib45AndCSruntime(source, options: TestOptions.ReleaseExe, parseOptions: _parseOptions);
            comp.VerifyDiagnostics(expected);
        }

        void VerifyDiagnostics(string source, CSharpCompilationOptions options, params DiagnosticDescription[] expected)
        {
            var comp = CreateCompilationWithMscorlib45AndCSruntime(source, options: options, parseOptions: _parseOptions);
            comp.VerifyDiagnostics(expected);
        }

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
            VerifyOutput(source, "Hello, world!");
        }

        [Fact]
        public void ExpressionBody()
        {
            var source = @"
int Local() => 2;
Console.Write(Local());
Console.Write(' ');
void VoidLocal() => Console.Write(2);
VoidLocal();
";
            VerifyOutputInMain(source, "2 2", "System");
        }

        [Fact]
        public void EmptyStatementAfter()
        {
            var source = @"
void Local()
{
    Console.Write(2);
};
Local();
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void Params()
        {
            var source = @"
void Params(params int[] x)
{
    Console.WriteLine(string.Join("","", x));
}
Params(2);
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void RefAndOut()
        {
            var source = @"
void RefOut(ref int x, out int y)
{
    y = ++x;
}
int a = 1;
int b;
RefOut(ref a, out b);
Console.Write(a);
Console.Write(' ');
Console.Write(b);
";
            VerifyOutputInMain(source, "2 2", "System");
        }

        [Fact]
        public void NamedAndOptional()
        {
            var source = @"
void NamedOptional(int x = 2)
{
    Console.Write(x);
}
NamedOptional(x: 3);
Console.Write(' ');
NamedOptional();
";
            VerifyOutputInMain(source, "3 2", "System");
        }

        [Fact]
        public void CallerMemberName()
        {
            var source = @"
void CallerMemberName([CallerMemberName] string s = null)
{
    Console.Write(s);
}
void LocalFuncName()
{
    CallerMemberName();
}
LocalFuncName();
Console.Write(' ');
CallerMemberName();
";
            VerifyOutputInMain(source, "LocalFuncName Main", "System", "System.Runtime.CompilerServices");
        }

        [Fact]
        public void BadParams()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void Params(params int x)
        {
            Console.WriteLine(x);
        }
        Params(2);
    }
}
";
            VerifyDiagnostics(source,
    // (8,21): error CS0225: The params parameter must be a single dimensional array
    //         void Params(params int x)
    Diagnostic(ErrorCode.ERR_ParamsMustBeArray, "params").WithLocation(8, 21)
    );
        }

        [Fact]
        public void BadRefWithDefault()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void RefOut(ref int x = 2)
        {
            x++;
        }
        int y = 2;
        RefOut(ref y);
    }
}
";
            VerifyDiagnostics(source,
    // (6,21): error CS1741: A ref or out parameter cannot have a default value
    //         void RefOut(ref int x = 2)
    Diagnostic(ErrorCode.ERR_RefOutDefaultValue, "ref").WithLocation(6, 21)
    );
        }

        [Fact]
        public void BadDefaultValueType()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void NamedOptional(string x = 2)
        {
            Console.WriteLine(x);
        }
        NamedOptional(""2"");
    }
}
";
            VerifyDiagnostics(source,
    // (8,35): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type 'string'
    //         void NamedOptional(string x = 2)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("int", "string").WithLocation(8, 35)
    );
        }

        [Fact]
        public void BadCallerMemberName()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    static void Main(string[] args)
    {
        void CallerMemberName([CallerMemberName] int s = 2)
        {
            Console.WriteLine(s);
        }
        CallerMemberName();
    }
}
";
            VerifyDiagnostics(source,
    // (9,32): error CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type 'string' to type 'int'
    //         void CallerMemberName([CallerMemberName] int s = 2)
    Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithArguments("string", "int").WithLocation(9, 32)
    );
        }

        [Fact]
        public void Property()
        {
            var source = @"
using System;

class Program
{
    static int Foo
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
    static void Main(string[] args)
    {
        Console.Write(Foo);
    }
}";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void PropertyIterator()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static int Foo
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
        Console.Write(Foo);
    }
}";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void DelegateFunc()
        {
            var source = @"
int Local(int x) => x;
Func<int, int> local = Local;
Console.Write(local(2));
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void DelegateFuncGenericImplicit()
        {
            var source = @"
T Local<T>(T x) => x;
Func<int, int> local = Local;
Console.Write(local(2));
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void DelegateFuncGenericExplicit()
        {
            var source = @"
T Local<T>(T x) => x;
Func<int, int> local = Local<int>;
Console.Write(local(2));
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void DelegateAction()
        {
            var source = @"
void Local()
{
    Console.Write(2);
}
var local = new Action(Local);
local();
Console.Write(' ');
local = (Action)Local;
local();
";
            VerifyOutputInMain(source, "2 2", "System");
        }

        [Fact]
        public void InterpolatedString()
        {
            var source = @"
int x = 1;
int Bar() => ++x;
var str = $@""{((Func<int>)(() => { int Foo() => Bar(); return Foo(); }))()}"";
Console.Write(str + ' ' + x);
";
            VerifyOutputInMain(source, "2 2", "System");
        }

        // StaticNoClosure*() are generic because the reference to the locfunc is constructed, and actual local function is not
        // (i.e. testing to make sure we use MethodSymbol.OriginalDefinition in LambdaRewriter.Analysis)
        [Fact]
        public void StaticNoClosure()
        {
            var source = @"
T Foo<T>(T x)
{
    return x;
}
Console.Write(Foo(2));
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var foo = verify.FindLocalFunction("Foo");
            Assert.True(foo.IsStatic);
            Assert.Equal(verify.Compilation.GetTypeByMetadataName("Program"), foo.ContainingType);
        }

        [Fact]
        public void StaticNoClosureDelegate()
        {
            var source = @"
T Foo<T>(T x)
{
    return x;
}
Func<int, int> foo = Foo;
Console.Write(foo(2));
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var foo = verify.FindLocalFunction("Foo");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.False(foo.IsStatic);
            Assert.Equal("<>c", foo.ContainingType.Name);
            Assert.Equal(program, foo.ContainingType.ContainingType);
        }

        [Fact]
        public void ClosureBasic()
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
    static void Main(string[] args)
    {
        A(2);
    }
}
";
            VerifyOutput(source, "1 2 1 2 3 4 3 4 3 4 5 6 5 6");
        }

        [Fact]
        public void ClosureThisOnly()
        {
            var source = @"
using System;

class Program
{
    int _a;
    static void Print(int a)
    {
        Console.Write(' ');
        Console.Write(a);
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
    static void Main(string[] args)
    {
        new Program().B();
    }
}";
            VerifyOutput(source, "2 2 3 3");
        }

        [Fact]
        public void ClosureGeneralThisOnly()
        {
            var source = @"
var x = 0;
void Outer()
{
    if (++x == 2)
    {
        Console.Write(x);
        return;
    }
    void Inner()
    {
        Outer();
    }
    Inner();
}
Outer();
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var outer = verify.FindLocalFunction("Outer");
            var inner = verify.FindLocalFunction("Inner");
            Assert.Equal(outer.ContainingType, inner.ContainingType);
        }

        [Fact]
        public void ClosureStaticInInstance()
        {
            var source = @"
using System;

class Program
{
    static int _sa;
    static void Print(int a)
    {
        Console.Write(' ');
        Console.Write(a);
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
    static void Main(string[] args)
    {
        new Program().C();
    }
}";
            VerifyOutput(source, "2 2 3 3");
        }

        [Fact]
        public void ClosureGeneric()
        {
            var source = @"
using System;

class Program
{
    static void Print(object a)
    {
        Console.Write(' ');
        Console.Write(a);
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
    static void Main(string[] args)
    {
        new Gen<int>(2).D<int>(3);
    }
}";
            VerifyOutput(source, "2 3 2 2 6 4 2");
        }

        [Fact]
        public void ClosureLambdasAndLocfuncs()
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
        E();
    }
}";
            VerifyOutput(source, "2");
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
            VerifyOutput(source, @"
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
            VerifyOutput(source, "6");
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
            VerifyOutput(source, "2");
        }

        [Fact]
        public void StructClosure()
        {
            var source = @"
int x = 2;
void Foo()
{
    Console.Write(x);
}
Foo();
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var foo = verify.FindLocalFunction("Foo");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.Equal(program, foo.ContainingType);
            Assert.True(foo.IsStatic);
            Assert.Equal(RefKind.Ref, foo.Parameters[0].RefKind);
            Assert.True(foo.Parameters[0].Type.IsValueType);
        }

        [Fact]
        public void StructClosureGeneric()
        {
            var source = @"
int x = 2;
void Foo<T1>()
{
    int y = x;
    void Bar<T2>()
    {
        Console.Write(x + y);
    }
    Bar<T1>();
}
Foo<int>();
";
            var verify = VerifyOutputInMain(source, "4", "System");
            var foo = verify.FindLocalFunction("Foo");
            var bar = verify.FindLocalFunction("Bar");
            Assert.Equal(1, foo.Parameters.Length);
            Assert.Equal(2, bar.Parameters.Length);
            Assert.Equal(RefKind.Ref, foo.Parameters[0].RefKind);
            Assert.Equal(RefKind.Ref, bar.Parameters[0].RefKind);
            Assert.Equal(RefKind.Ref, bar.Parameters[1].RefKind);
            Assert.True(foo.Parameters[0].Type.IsValueType);
            Assert.True(bar.Parameters[0].Type.IsValueType);
            Assert.True(bar.Parameters[1].Type.IsValueType);
            Assert.Equal(foo.Parameters[0].Type.OriginalDefinition, bar.Parameters[0].Type.OriginalDefinition);
            var fooFrame = (INamedTypeSymbol)foo.Parameters[0].Type;
            var barFrame = (INamedTypeSymbol)bar.Parameters[1].Type;
            Assert.Equal(0, fooFrame.Arity);
            Assert.Equal(1, barFrame.Arity);
        }

        [Fact]
        public void ClosureOfStructClosure()
        {
            var source = @"
void Outer()
{
    int a = 0;
    void Middle()
    {
        int b = 0;
        void Inner()
        {
            a++;
            b++;
        }

        a++;
        Inner();
    }

    Middle();
    Console.WriteLine(a);
}

Outer();
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var inner = verify.FindLocalFunction("Inner");
            var middle = verify.FindLocalFunction("Middle");
            var outer = verify.FindLocalFunction("Outer");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.Equal(program, inner.ContainingType);
            Assert.Equal(program, middle.ContainingType);
            Assert.Equal(program, outer.ContainingType);
            Assert.True(inner.IsStatic);
            Assert.True(middle.IsStatic);
            Assert.True(outer.IsStatic);
            Assert.Equal(2, inner.Parameters.Length);
            Assert.Equal(1, middle.Parameters.Length);
            Assert.Equal(0, outer.Parameters.Length);
            Assert.Equal(RefKind.Ref, inner.Parameters[0].RefKind);
            Assert.Equal(RefKind.Ref, inner.Parameters[1].RefKind);
            Assert.Equal(RefKind.Ref, middle.Parameters[0].RefKind);
            Assert.True(inner.Parameters[0].Type.IsValueType);
            Assert.True(inner.Parameters[1].Type.IsValueType);
            Assert.True(middle.Parameters[0].Type.IsValueType);
        }

        [Fact]
        public void ThisClosureCallingOtherClosure()
        {
            var source = @"
using System;

class Program
{
    int _x;
    int Test()
    {
        int First()
        {
            return ++_x;
        }
        int Second()
        {
            return First();
        }
        return Second();
    }
    static void Main()
    {
        Console.Write(new Program() { _x = 1 }.Test());
    }
}
";
            var verify = VerifyOutput(source, "2");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.Equal(program, verify.FindLocalFunction("First").ContainingType);
            Assert.Equal(program, verify.FindLocalFunction("Second").ContainingType);
        }

        [Fact]
        public void RecursiveStructClosure()
        {
            var source = @"
int x = 0;
void Foo()
{
    if (x != 2)
    {
        x++;
        Foo();
    }
    else
    {
        Console.Write(x);
    }
}
Foo();
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var foo = verify.FindLocalFunction("Foo");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.Equal(program, foo.ContainingType);
            Assert.True(foo.IsStatic);
            Assert.Equal(RefKind.Ref, foo.Parameters[0].RefKind);
            Assert.True(foo.Parameters[0].Type.IsValueType);
        }

        [Fact]
        public void MutuallyRecursiveStructClosure()
        {
            var source = @"
int x = 0;
void Foo(int depth)
{
    int dummy = 0;
    void Bar(int depth2)
    {
        dummy++;
        Foo(depth2);
    }
    if (depth != 2)
    {
        x++;
        Bar(depth + 1);
    }
    else
    {
        Console.Write(x);
    }
}
Foo(0);
";
            var verify = VerifyOutputInMain(source, "2", "System");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            var foo = verify.FindLocalFunction("Foo");
            var bar = verify.FindLocalFunction("Bar");
            Assert.Equal(program, foo.ContainingType);
            Assert.Equal(program, bar.ContainingType);
            Assert.True(foo.IsStatic);
            Assert.True(bar.IsStatic);
            Assert.Equal(2, foo.Parameters.Length);
            Assert.Equal(3, bar.Parameters.Length);
            Assert.Equal(RefKind.Ref, foo.Parameters[1].RefKind);
            Assert.Equal(RefKind.Ref, bar.Parameters[1].RefKind);
            Assert.Equal(RefKind.Ref, bar.Parameters[2].RefKind);
            Assert.True(foo.Parameters[1].Type.IsValueType);
            Assert.True(bar.Parameters[1].Type.IsValueType);
            Assert.True(bar.Parameters[2].Type.IsValueType);
        }

        [Fact]
        public void Recursion()
        {
            var source = @"
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
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void MutualRecursion()
        {
            var source = @"
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
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void RecursionThisOnlyClosure()
        {
            var source = @"
using System;

class Program
{
    int _x;
    void Outer()
    {
        void Inner()
        {
            if (_x == 0)
            {
                return;
            }
            Console.Write(_x);
            Console.Write(' ');
            _x = 0;
            Inner();
        }
        Inner();
    }
    public static void Main()
    {
        new Program() { _x = 2 }.Outer();
    }
}
";
            var verify = VerifyOutput(source, "2");
            var program = verify.Compilation.GetTypeByMetadataName("Program");
            Assert.Equal(program, verify.FindLocalFunction("Inner").ContainingType);
        }

        [Fact]
        public void RecursionFrameCaptureTest()
        {
            // ensures that referring to a local function in an otherwise noncapturing Inner captures the frame of Outer.
            var source = @"
int x = 0;
int Outer(bool isRecursive)
{
    if (isRecursive)
    {
        return x;
    }
    x++;
    int Middle()
    {
        int Inner()
        {
            return Outer(true);
        }
        return Inner();
    }
    return Middle();
}
Console.Write(Outer(false));
Console.Write(' ');
Console.Write(x);
";
            VerifyOutputInMain(source, "1 1", "System");
        }

        [Fact]
        public void IteratorBasic()
        {
            var source = @"
IEnumerable<int> Local()
{
    yield return 2;
}
Console.Write(string.Join("","", Local()));
";
            VerifyOutputInMain(source, "2", "System", "System.Collections.Generic");
        }

        [Fact]
        public void IteratorGeneric()
        {
            var source = @"
IEnumerable<T> LocalGeneric<T>(T val)
{
    yield return val;
}
Console.Write(string.Join("","", LocalGeneric(2)));
";
            VerifyOutputInMain(source, "2", "System", "System.Collections.Generic");
        }

        [Fact]
        public void IteratorNonGeneric()
        {
            var source = @"
IEnumerable LocalNongen()
{
    yield return 2;
}
foreach (int x in LocalNongen())
{
    Console.Write(x);
}
";
            VerifyOutputInMain(source, "2", "System", "System.Collections");
        }

        [Fact]
        public void IteratorEnumerator()
        {
            var source = @"
IEnumerator LocalEnumerator()
{
    yield return 2;
}
var y = LocalEnumerator();
y.MoveNext();
Console.Write(y.Current);
";
            VerifyOutputInMain(source, "2", "System", "System.Collections");
        }

        [Fact]
        public void AsyncBasic()
        {
            var source = @"
async Task<int> Local()
{
    return await Task.FromResult(2);
}
Console.Write(Local().Result);
";
            VerifyOutputInMain(source, "2", "System", "System.Threading.Tasks");
        }

        [Fact]
        public void AsyncParam()
        {
            var source = @"
async Task<int> LocalParam(int x)
{
    return await Task.FromResult(x);
}
Console.Write(LocalParam(2).Result);
";
            VerifyOutputInMain(source, "2", "System", "System.Threading.Tasks");
        }

        [Fact]
        public void AsyncGeneric()
        {
            var source = @"
async Task<T> LocalGeneric<T>(T x)
{
    return await Task.FromResult(x);
}
Console.Write(LocalGeneric(2).Result);
";
            VerifyOutputInMain(source, "2", "System", "System.Threading.Tasks");
        }

        [Fact]
        public void AsyncVoid()
        {
            var source = @"
// had bug with parser where 'async [keyword]' didn't parse.
async void LocalVoid()
{
    Console.Write(2);
    await Task.Yield();
}
LocalVoid();
";
            VerifyOutputInMain(source, "2", "System", "System.Threading.Tasks");
        }

        [Fact]
        public void AsyncAwaitAwait()
        {
            var source = @"
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
";
            VerifyOutputInMain(source, "2", "System", "System.Threading.Tasks");
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
            var output = @"
2
2
2
2
";
            VerifyOutput(source, output);
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
            var output = @"
2
2
";
            VerifyOutput(source, output, TestOptions.ReleaseExe.WithAllowUnsafe(true).WithWarningLevel(0));
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
            var output = @"
2
2
2
2
2
2
2
2
2
";
            VerifyOutput(source, output);
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
            var output = @"
2
2
2
2
";
            VerifyOutput(source, output);
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
            var output = @"
2
2
2
2
2
2
2
";
            VerifyOutput(source, output);
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
            var output = @"
2
2
2
2
2
2
2
";
            VerifyOutput(source, output);
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
            var output = @"
2
2
2
2
2
2
2
";
            VerifyOutput(source, output);
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
            var output = @"
2
2
2
2
2
";
            VerifyOutput(source, output);
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
    static void Main(string[] args)
    {
        Console.WriteLine(Program.InnerToOuter((object)null));
        Console.WriteLine(Program.InnerToMiddle((object)null));
        Console.WriteLine(Program.InnerToOuterScoping((object)null));
        Console.WriteLine(Program.M1(2));
    }
}
";
            var output = @"
System.Object
System.Object
System.Object
2
";
            VerifyOutput(source, output);
        }

        [Fact]
        public void CompoundOperatorExecutesOnce()
        {
            var source = @"
using System;

class Program
{
    int _x = 2;
    public static void Main()
    {
        var prog = new Program();
        Program SideEffect()
        {
            Console.Write(prog._x);
            return prog;
        }
        SideEffect()._x += 2;
        Console.Write(' ');
        SideEffect();
    }
}
";
            VerifyOutput(source, "2 4");
        }

        [Fact]
        public void ConstValueDoesntMakeClosure()
        {
            var source = @"
const int x = 2;
void Local()
{
    Console.Write(x);
}
Local();
";
            // Should be a static method on "Program" itself, not a display class like "Program+<>c__DisplayClass0_0"
            var verify = VerifyOutputInMain(source, "2", "System");
            var foo = verify.FindLocalFunction("Local");
            Assert.True(foo.IsStatic);
            Assert.Equal(verify.Compilation.GetTypeByMetadataName("Program"), foo.ContainingType);
        }

        [Fact]
        public void DynamicArgument()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        void Local(int x)
        {
            Console.Write(x);
        }
        dynamic val = 2;
        Local(val);
    }
}
";
            VerifyDiagnostics(source,
    // (12,9): error CS8098: Cannot invoke the local function 'Local' with dynamic parameters.
    //         Local(val);
    Diagnostic(ErrorCode.ERR_DynamicLocalFunctionParameter, "Local(val)").WithArguments("Local").WithLocation(12, 9)
    );
        }

        [Fact]
        public void DynamicParameter()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        void Local(dynamic x)
        {
            Console.Write(x);
        }
        Local(2);
    }
}
";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void DynamicReturn()
        {
            var source = @"
dynamic RetDyn()
{
    return 2;
}
Console.Write(RetDyn());
";
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void DynamicDelegate()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        dynamic Local(dynamic x)
        {
            return x;
        }
        dynamic local = (Func<dynamic, dynamic>)Local;
        Console.Write(local(2));
    }
}
";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void Nameof()
        {
            var source = @"
void Local()
{
}
Console.Write(nameof(Local));
";
            VerifyOutputInMain(source, "Local", "System");
        }

        [Fact]
        public void ExpressionTreeParameter()
        {
            var source = @"
Expression<Func<int, int>> Local(Expression<Func<int, int>> f)
{
    return f;
}
Console.Write(Local(x => x));
";
            VerifyOutputInMain(source, "x => x", "System", "System.Linq.Expressions");
        }

        [Fact]
        public void ExpressionTreeLocfuncUsage()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        T Id<T>(T x)
        {
            return x;
        }
        Expression<Func<T>> Local<T>(Expression<Func<T>> f)
        {
            return f;
        }
        Console.Write(Local(() => Id(2)));
        Console.Write(Local<Func<int, int>>(() => Id));
        Console.Write(Local(() => new Func<int, int>(Id)));
        // Disabled because of https://github.com/dotnet/roslyn/issues/3923
        // Should produce a diagnostic once uncommented.
        //Console.Write(Local(() => nameof(Id)));
    }
}
";
            VerifyDiagnostics(source,
    // (16,35): error CS8096: An expression tree may not contain a reference to a local function
    //         Console.Write(Local(() => Id(2)));
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Id(2)").WithLocation(16, 35),
    // (17,51): error CS8096: An expression tree may not contain a reference to a local function
    //         Console.Write(Local<Func<int, int>>(() => Id));
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Id").WithLocation(17, 51),
    // (18,35): error CS8096: An expression tree may not contain a reference to a local function
    //         Console.Write(Local(() => new Func<int, int>(Id)));
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "new Func<int, int>(Id)").WithLocation(18, 35)
    );
        }

        [Fact]
        public void ExpressionTreeLocfuncInside()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        Expression<Func<int, int>> f = x =>
        {
            int Local(int y) => y;
            return Local(x);
        };
        Console.Write(f);
    }
}
";
            VerifyDiagnostics(source,
    // (8,40): error CS0834: A lambda expression with a statement body cannot be converted to an expression tree
    //         Expression<Func<int, int>> f = x =>
    Diagnostic(ErrorCode.ERR_StatementLambdaToExpressionTree, @"x =>
        {
            int Local(int y) => y;
            return Local(x);
        }").WithLocation(8, 40),
    // (11,20): error CS8096: An expression tree may not contain a local function or a reference to a local function
    //             return Local(x);
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Local(x)").WithLocation(11, 20)
    );
        }

        [Fact]
        public void LinqInLocalFunction()
        {
            var source = @"
IEnumerable<int> Query(IEnumerable<int> values)
{
    return from x in values where x < 5 select x * x;
}
Console.Write(string.Join("","", Query(Enumerable.Range(0, 10))));
";
            VerifyOutputInMain(source, "0,1,4,9,16", "System", "System.Linq", "System.Collections.Generic");
        }

        [Fact]
        public void ConstructorWithoutArg()
        {
            var source = @"
using System;

class Base
{
    public int x;
    public Base(int x)
    {
        this.x = x;
    }
}

class Program : Base
{
    Program() : base(2)
    {
        void Local()
        {
            Console.Write(x);
        }
        Local();
    }
    public static void Main()
    {
        new Program();
    }
}
";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void ConstructorWithArg()
        {
            var source = @"
using System;

class Base
{
    public int x;
    public Base(int x)
    {
        this.x = x;
    }
}

class Program : Base
{
    Program(int x) : base(x + 2)
    {
        void Local()
        {
            Console.Write(x);
            Console.Write(' ');
            Console.Write(base.x);
        }
        Local();
    }
    public static void Main()
    {
        new Program(2);
    }
}
";
            VerifyOutput(source, "2 4");
        }

        [Fact]
        public void IfDef()
        {
            var source = @"
using System;

class Program
{
    public static void Main()
    {
        #if LocalFunc
        void Local()
        {
            Console.Write(2);
            Console.Write(' ');
        #endif
            Console.Write(4);
        #if LocalFunc
        }
        Local();
        #endif
    }
}
";
            VerifyOutput(source, "4");
            source = "#define LocalFunc" + source;
            VerifyOutput(source, "2 4");
        }

        [Fact]
        public void PragmaWarningDisableEntersLocfunc()
        {
            var source = @"
#pragma warning disable CS0168
void Local()
{
    int x; // unused
    Console.Write(2);
}
#pragma warning restore CS0168
Local();
";
            // No diagnostics is asserted in VerifyOutput, so if the warning happens, then we'll catch it
            VerifyOutputInMain(source, "2", "System");
        }

        [Fact]
        public void ObsoleteAttributeRecursion()
        {
            var source = @"
using System;

class Program
{
    [Obsolete]
    public void Obs()
    {
        void Local()
        {
            Obs(); // shouldn't emit warning
        }
        Local();
    }
    public static void Main()
    {
        Console.Write(2);
    }
}
";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void MainLocfuncIsntEntry()
        {
            var source = @"
void Main()
{
    Console.Write(4);
}
Console.Write(2);
Console.Write(' ');
Main();
";
            VerifyOutputInMain(source, "2 4", "System");
        }

        [Fact]
        public void Shadows()
        {
            var source = @"
using System;

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
            Console.Write(2);
        }
        Local();
    }
}
";
            VerifyOutput(source, "2");
        }

        [Fact]
        public void ExtensionMethodClosure()
        {
            var source = @"
using System;

static class Program
{
    public static void Ext(this int x)
    {
        void Local()
        {
            Console.Write(x);
        }
        Local();
    }
    public static void Main()
    {
        2.Ext();
    }
}
";
            // warning level 0 because extension method generates CS1685 (predefined type multiple definition) for ExtensionAttribute in System.Core and mscorlib
            VerifyOutput(source, "2", TestOptions.ReleaseExe.WithWarningLevel(0));
        }

        [Fact]
        public void Scoping()
        {
            var source = @"
void Local()
{
    Console.Write(2);
}
if (true)
{
    Local();
}
";
            VerifyOutputInMain(source, "2", "System");
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
            Local();
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
            VerifyDiagnostics(source,
    // (16,9): error CS0103: The name 'Local' does not exist in the current context
    //         Local();
    Diagnostic(ErrorCode.ERR_NameNotInContext, "Local").WithArguments("Local").WithLocation(16, 9),
    // (18,9): error CS0841: Cannot use local variable 'Local2' before it is declared
    //         Local2();
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "Local2").WithArguments("Local2").WithLocation(18, 9)
    );
        }

        [Fact]
        public void NameConflictDuplicate()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Duplicate() { }
        void Duplicate() { }
        Duplicate();
    }
}
";
            VerifyDiagnostics(source,
    // (7,14): error CS0128: A local variable named 'Duplicate' is already defined in this scope
    //         void Duplicate() { }
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "Duplicate").WithArguments("Duplicate").WithLocation(7, 14),
    // (7,14): warning CS0168: The variable 'Duplicate' is declared but never used
    //         void Duplicate() { }
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Duplicate").WithArguments("Duplicate").WithLocation(7, 14)
    );
        }

        [Fact]
        public void NameConflictParameter()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int x = 2;
        void Param(int x) { }
        Param(x);
    }
}
";
            VerifyDiagnostics(source,
    // (7,24): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         void Param(int x) { }
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(7, 24)
    );
        }

        [Fact]
        public void NameConflictTypeParameter()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int T;
        void Generic<T>() { }
        Generic<int>();
    }
}
";
            VerifyDiagnostics(source,
    // (7,22): error CS0136: A local or parameter named 'T' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         void Generic<T>() { }
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "T").WithArguments("T").WithLocation(7, 22),
    // (6,13): warning CS0168: The variable 'T' is declared but never used
    //         int T;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "T").WithArguments("T").WithLocation(6, 13)
    );
        }

        [Fact]
        public void NameConflictNestedTypeParameter()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        T Outer<T>()
        {
            T Inner<T>()
            {
                return default(T);
            }
            return Inner<T>();
        }
        System.Console.Write(Outer<int>());
    }
}
";
            VerifyDiagnostics(source,
    // (8,21): warning CS0693: Type parameter 'T' has the same name as the type parameter from outer type 'Outer<T>()'
    //             T Inner<T>()
    Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "T").WithArguments("T", "Outer<T>()").WithLocation(8, 21)
    );
        }

        [Fact]
        public void NameConflictLocalVarFirst()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int Conflict;
        void Conflict() { }
    }
}
";
            VerifyDiagnostics(source,
    // (7,14): error CS0128: A local variable named 'Conflict' is already defined in this scope
    //         void Conflict() { }
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "Conflict").WithArguments("Conflict").WithLocation(7, 14),
    // (6,13): warning CS0168: The variable 'Conflict' is declared but never used
    //         int Conflict;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Conflict").WithArguments("Conflict").WithLocation(6, 13),
    // (7,14): warning CS0168: The variable 'Conflict' is declared but never used
    //         void Conflict() { }
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Conflict").WithArguments("Conflict").WithLocation(7, 14)
    );
        }

        [Fact]
        public void NameConflictLocalVarLast()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Conflict() { }
        int Conflict;
    }
}
";
            // TODO: This is strange. Probably has to do with the fact that local variables are preferred over functions.
            VerifyDiagnostics(source,
    // (6,14): error CS0136: A local or parameter named 'Conflict' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         void Conflict() { }
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "Conflict").WithArguments("Conflict").WithLocation(6, 14),
    // (7,13): warning CS0168: The variable 'Conflict' is declared but never used
    //         int Conflict;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Conflict").WithArguments("Conflict").WithLocation(7, 13),
    // (6,14): warning CS0168: The variable 'Conflict' is declared but never used
    //         void Conflict() { }
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Conflict").WithArguments("Conflict").WithLocation(6, 14)
    );
        }

        [Fact]
        public void UnsafeBasic()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        unsafe void Local()
        {
            int x = 2;
            Console.Write(*&x);
        }
        Local();
    }
    static void Main(string[] args)
    {
        A();
    }
}
";
            VerifyOutput(source, "2", TestOptions.ReleaseExe.WithAllowUnsafe(true));
        }

        [Fact]
        public void UnsafeParameter()
        {
            var source = @"
using System;

class Program
{
    static unsafe void B()
    {
        int x = 2;
        unsafe void Local(int* y)
        {
            Console.Write(*y);
        }
        Local(&x);
    }
    static void Main(string[] args)
    {
        B();
    }
}
";
            VerifyOutput(source, "2", TestOptions.ReleaseExe.WithAllowUnsafe(true));
        }

        [Fact]
        public void UnsafeClosure()
        {
            var source = @"
using System;

class Program
{
    static unsafe void C()
    {
        int y = 2;
        int* x = &y;
        unsafe void Local()
        {
            Console.Write(*x);
        }
        Local();
    }
    static void Main(string[] args)
    {
        C();
    }
}
";
            VerifyOutput(source, "2", TestOptions.ReleaseExe.WithAllowUnsafe(true));
        }

        [Fact]
        public void BadUnsafeNoKeyword()
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
    static void Main(string[] args)
    {
        A();
    }
}
";
            VerifyDiagnostics(source,
    // (11,32): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
    //             Console.WriteLine(*&x);
    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(11, 32)
    );
        }

        [Fact]
        public void BadUnsafeKeywordDoesntApply()
        {
            var source = @"
using System;

class Program
{
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
        B();
    }
}
";
            VerifyDiagnostics(source, TestOptions.ReleaseExe.WithAllowUnsafe(true),
    // (11,32): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
    //             Console.WriteLine(*&x);
    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(11, 32)
    );
        }

        [Fact]
        public void BadEmptyBody()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Local(int x);
        Local(2);
    }
}";
            VerifyDiagnostics(source,
    // (6,14): error CS0501: 'Local(int)' must declare a body because it is not marked abstract, extern, or partial
    //         void Local(int x);
    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "Local").WithArguments("Local(int)").WithLocation(6, 14)
    );
        }

        [Fact]
        public void BadGotoInto()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        goto A;
        void Local()
        {
        A:  Console.Write(2);
        }
        Local();
    }
}";
            VerifyDiagnostics(source,
    // (8,14): error CS0159: No such label 'A' within the scope of the goto statement
    //         goto A;
    Diagnostic(ErrorCode.ERR_LabelNotFound, "A").WithArguments("A").WithLocation(8, 14),
    // (11,9): warning CS0164: This label has not been referenced
    //         A:  Console.Write(2);
    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "A").WithLocation(11, 9)
    );
        }

        [Fact]
        public void BadGotoOutOf()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Local()
        {
            goto A;
        }
    A:  Local();
    }
}";
            VerifyDiagnostics(source,
    // (8,13): error CS0159: No such label 'A' within the scope of the goto statement
    //             goto A;
    Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("A").WithLocation(8, 13),
    // (10,5): warning CS0164: This label has not been referenced
    //     A:  Local();
    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "A").WithLocation(10, 5)
    );
        }

        [Fact]
        public void BadDefiniteAssignmentCall()
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
            Console.Write(x);
        }
        Label:
        Local();
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
    // (9,9): warning CS0162: Unreachable code detected
    //         int x = 2;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(9, 9),
    // (15,9): error CS0165: Use of unassigned local variable 'Local'
    //         Local();
    Diagnostic(ErrorCode.ERR_UseDefViolation, "Local()").WithArguments("Local").WithLocation(15, 9)
    );
        }

        [Fact]
        public void BadDefiniteAssignmentDelegateConversion()
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
            Console.Write(x);
        }
        Label:
        Action foo = Local;
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
    // (9,9): warning CS0162: Unreachable code detected
    //         int x = 2;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(9, 9),
    // (15,22): error CS0165: Use of unassigned local variable 'Local'
    //         Action foo = Local;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "Local").WithArguments("Local").WithLocation(15, 22)
    );
        }

        [Fact]
        public void BadDefiniteAssignmentDelegateConstruction()
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
            Console.Write(x);
        }
        Label:
        var bar = new Action(Local);
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
    // (9,9): warning CS0162: Unreachable code detected
    //         int x = 2;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(9, 9),
    // (15,19): error CS0165: Use of unassigned local variable 'Local'
    //         var bar = new Action(Local);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "new Action(Local)").WithArguments("Local").WithLocation(15, 19)
    );
        }

        [Fact]
        public void BadNotUsed()
        {
            var source = @"
class Program
{
    static void A()
    {
        void Local()
        {
        }
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
    // (6,14): warning CS0168: The variable 'Local' is declared but never used
    //         void Local()
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Local").WithArguments("Local").WithLocation(6, 14)
    );
        }

        [Fact]
        public void BadNotUsedSwitch()
        {
            var source = @"
class Program
{
    static void A()
    {
        switch (0)
        {
        case 0:
            void Local()
            {
            }
            break;
        }
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
    // (9,18): warning CS0168: The variable 'Local' is declared but never used
    //             void Local()
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Local").WithArguments("Local").WithLocation(9, 18)
    );
        }

        [Fact]
        public void BadByRefClosure()
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
        Local();
    }
    static void Main()
    {
    }
}";
            VerifyDiagnostics(source,
    // (10,31): error CS1628: Cannot use ref or out parameter 'x' inside an anonymous method, lambda expression, or query expression
    //             Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x").WithArguments("x").WithLocation(10, 31)
    );
        }

        [Fact]
        public void BadArglistUse()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        void Local()
        {
            Console.WriteLine(__arglist);
        }
        Local();
    }
    static void B(__arglist)
    {
        void Local()
        {
            Console.WriteLine(__arglist);
        }
        Local();
    }
    static void C() // C and D produce different errors
    {
        void Local(__arglist)
        {
            Console.WriteLine(__arglist);
        }
        Local(__arglist());
    }
    static void D(__arglist)
    {
        void Local(__arglist)
        {
            Console.WriteLine(__arglist);
        }
        Local(__arglist());
    }
    static void Main()
    {
    }
}
";
            VerifyDiagnostics(source,
    // (10,31): error CS0190: The __arglist construct is valid only within a variable argument method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_ArgsInvalid, "__arglist").WithLocation(10, 31),
    // (18,31): error CS4013: Instance of type 'RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle").WithLocation(18, 31),
    // (26,31): error CS0190: The __arglist construct is valid only within a variable argument method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_ArgsInvalid, "__arglist").WithLocation(26, 31),
    // (34,31): error CS4013: Instance of type 'RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle").WithLocation(34, 31)
    );
        }

        [Fact]
        public void BadClosureStaticRefInstance()
        {
            var source = @"
using System;

class Program
{
    int _a = 0;
    static void A()
    {
        void Local()
        {
            Console.WriteLine(_a);
        }
        Local();
    }
    static void Main()
    {
    }
}
";
            VerifyDiagnostics(source,
    // (11,31): error CS0120: An object reference is required for the non-static field, method, or property 'Program._a'
    //             Console.WriteLine(_a);
    Diagnostic(ErrorCode.ERR_ObjectRequired, "_a").WithArguments("Program._a").WithLocation(11, 31)
    );
        }

        [Fact]
        public void BadRefIterator()
        {
            var source = @"
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        IEnumerable<int> RefEnumerable(ref int x)
        {
            yield return x;
        }
        int y = 0;
        RefEnumerable(ref y);
    }
}
";
            VerifyDiagnostics(source,
    // (8,48): error CS1623: Iterators cannot have ref or out parameters
    //         IEnumerable<int> RefEnumerable(ref int x)
    Diagnostic(ErrorCode.ERR_BadIteratorArgType, "x").WithLocation(8, 48)
    );
        }

        [Fact]
        public void BadRefAsync()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        async Task<int> RefAsync(ref int x)
        {
            return await Task.FromResult(x);
        }
        int y = 2;
        Console.Write(RefAsync(ref y).Result);
    }
}
";
            VerifyDiagnostics(source,
    // (9,42): error CS1988: Async methods cannot have ref or out parameters
    //         async Task<int> RefAsync(ref int x)
    Diagnostic(ErrorCode.ERR_BadAsyncArgType, "x").WithLocation(9, 42)
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
            VerifyDiagnostics(source,
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
        LocalConst();
        LocalStatic();
        LocalReadonly();
        LocalVolatile();
    }
}
";
            VerifyDiagnostics(source,
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
            VerifyDiagnostics(source,
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
            VerifyDiagnostics(source,
    // (8,27): error CS0841: Cannot use local variable 'Local' before it is declared
    //         Console.WriteLine(Local());
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "Local").WithArguments("Local").WithLocation(8, 27)
    );
        }

        [Fact]
        public void OtherSwitchBlock()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = int.Parse(Console.ReadLine());
        switch (x)
        {
        case 0:
            void Local()
            {
            }
            break;
        default:
            Local();
            break;
        }
    }
}
";
            VerifyDiagnostics(source,
    // (17,13): error CS0165: Use of unassigned local variable 'Local'
    //             Local();
    Diagnostic(ErrorCode.ERR_UseDefViolation, "Local()").WithArguments("Local").WithLocation(17, 13)
    );
        }

        [Fact]
        public void NoOperator()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        Program operator +(Program left, Program right)
        {
            return left;
        }
    }
}
";
            VerifyDiagnostics(source,
    // (6,17): error CS1002: ; expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "operator").WithLocation(6, 17),
    // (6,17): error CS1513: } expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_RbraceExpected, "operator").WithLocation(6, 17),
    // (6,36): error CS1026: ) expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "left").WithLocation(6, 36),
    // (6,36): error CS1002: ; expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "left").WithLocation(6, 36),
    // (6,40): error CS1002: ; expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 40),
    // (6,40): error CS1513: } expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(6, 40),
    // (6,55): error CS1002: ; expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 55),
    // (6,55): error CS1513: } expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 55),
    // (6,9): error CS0119: 'Program' is a type, which is not valid in the given context
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(6, 9),
    // (6,28): error CS0119: 'Program' is a type, which is not valid in the given context
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(6, 28),
    // (6,28): error CS0119: 'Program' is a type, which is not valid in the given context
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(6, 28),
    // (6,36): error CS0103: The name 'left' does not exist in the current context
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "left").WithArguments("left").WithLocation(6, 36),
    // (8,20): error CS0103: The name 'left' does not exist in the current context
    //             return left;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "left").WithArguments("left").WithLocation(8, 20),
    // (6,50): warning CS0168: The variable 'right' is declared but never used
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "right").WithArguments("right").WithLocation(6, 50)
    );
        }

        [Fact]
        public void NoProperty()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int Foo
        {
            get
            {
                return 2;
            }
        }
        int Bar => 2;
    }
}
";
            VerifyDiagnostics(source,
    // (6,16): error CS1002: ; expected
    //         int Foo
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 16),
    // (8,16): error CS1002: ; expected
    //             get
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 16),
    // (13,17): error CS1003: Syntax error, ',' expected
    //         int Bar => 2;
    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(13, 17),
    // (13,20): error CS1002: ; expected
    //         int Bar => 2;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "2").WithLocation(13, 20),
    // (8,13): error CS0103: The name 'get' does not exist in the current context
    //             get
    Diagnostic(ErrorCode.ERR_NameNotInContext, "get").WithArguments("get").WithLocation(8, 13),
    // (10,17): error CS0127: Since 'Program.Main(string[])' returns void, a return keyword must not be followed by an object expression
    //                 return 2;
    Diagnostic(ErrorCode.ERR_RetNoObjectRequired, "return").WithArguments("Program.Main(string[])").WithLocation(10, 17),
    // (13,20): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         int Bar => 2;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "2").WithLocation(13, 20),
    // (13,9): warning CS0162: Unreachable code detected
    //         int Bar => 2;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(13, 9),
    // (6,13): warning CS0168: The variable 'Foo' is declared but never used
    //         int Foo
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Foo").WithArguments("Foo").WithLocation(6, 13),
    // (13,13): warning CS0168: The variable 'Bar' is declared but never used
    //         int Bar => 2;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Bar").WithArguments("Bar").WithLocation(13, 13)
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
        Local();
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(source, options: option, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics(
    // (6,9): error CS8058: Feature 'local functions' is only available in 'experimental' language version.
    //         void Local()
    Diagnostic(ErrorCode.ERR_FeatureIsExperimental, @"void Local()
        {
        }").WithArguments("local functions").WithLocation(6, 9)
                );
        }
    }
}
