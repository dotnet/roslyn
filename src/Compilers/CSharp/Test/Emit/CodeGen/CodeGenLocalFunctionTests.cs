using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public static class LocalFunctionTestsUtil
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

    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public class CodeGenLocalFunctionTests : CSharpTestBase
    {
        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicParameterLocalFunction()
        {
            var src = @"
using System;

class C
{
    static void Main(string[] args) => M(0);

    static void M(int x)
    {
        dynamic y = x + 1;
        Action a;
        Action local(dynamic z) 
        {
            Console.Write(z);
            Console.Write(y);
            return () => Console.Write(y + z + 1);
        }
        a = local(x);
        a();
    }
}";
            VerifyOutput(src, "012");
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
        [CompilerTrait(CompilerFeature.ExpressionBody)]
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
        [CompilerTrait(CompilerFeature.Params)]
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
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicArgShadowing()
        {
            var src = @"
using System;
class C
{
    static void Shadow(int x) => Console.Write(x + 1);

    static void Main()
    {
        void Shadow(int x) => Console.Write(x);

        dynamic val = 2;
        Shadow(val);
    }
}";
            VerifyOutput(src, "2");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
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
        [CompilerTrait(CompilerFeature.Dynamic)]
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
        [CompilerTrait(CompilerFeature.Dynamic)]
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
        dynamic L2(int x)
        {
            void L2_1(int y)
            {
                Console.Write(x);
                Console.Write(y);
            }
            dynamic z = x + 1;
            void L2_2() => L2_1(z);
            return (Action)L2_2;
        } 
        dynamic local = (Func<dynamic, dynamic>)Local;
        Console.Write(local(2));
        L2(3)();
    }
}
";
            VerifyOutput(source, "234");
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
                    Print(d + b);
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
            VerifyOutput(source, "4");
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
        [CompilerTrait(CompilerFeature.Iterator)]
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
        [CompilerTrait(CompilerFeature.Iterator)]
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
        [CompilerTrait(CompilerFeature.Iterator)]
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
        [CompilerTrait(CompilerFeature.Iterator)]
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
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicArgument()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        int capture1 = 0;
        void L1(int x) => Console.Write(x);
        void L2(int x)
        {
            Console.Write(capture1);
            Console.Write(x);
        }
        dynamic L4(int x)
        {
            Console.Write(capture1);
            return x;
        }
        Action<int> L5(int x)
        {
            Console.Write(x);
            return L1;
        }

        dynamic val = 2;
        Console.WriteLine();
        L1(val);
        L2(val);
        Console.WriteLine();
        L2(L4(val));
        L5(val)(val);
    }
}
";
            VerifyOutput(source, output: @"202
00222");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic, CompilerFeature.Params)]
        public void DynamicArgsAndParams()
        {
            var src = @"
int capture1 = 0;
void L1(int x, params int[] ys)
{
    Console.Write(capture1);
    Console.Write(x);
    foreach (var y in ys)
    {
        Console.Write(y);
    }
}

dynamic val = 2;
int val2 = 3;
L1(val, val2);
L1(val);
L1(val, val, val);
";
            VerifyOutputInMain(src, "023020222", "System");
        }

        [Fact]
        public void Basic()
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
        public void Param()
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
        [CompilerTrait(CompilerFeature.Async)]
        public void GenericAsync()
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
        [CompilerTrait(CompilerFeature.Async)]
        public void Void()
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
        [CompilerTrait(CompilerFeature.Async)]
        public void AwaitAwait()
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
        [CompilerTrait(CompilerFeature.Async)]
        public void Keyword()
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
        [CompilerTrait(CompilerFeature.Async)]
        public void UnsafeKeyword()
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

        internal CompilationVerifier VerifyOutput(string source, string output, CSharpCompilationOptions options)
        {
            var comp = CreateCompilationWithMscorlib45AndCSruntime(source, options: options);
            return CompileAndVerify(comp, expectedOutput: output).VerifyDiagnostics(); // no diagnostics
        }

        internal CompilationVerifier VerifyOutput(string source, string output)
        {
            var comp = CreateCompilationWithMscorlib45AndCSruntime(source, options: TestOptions.ReleaseExe);
            return CompileAndVerify(comp, expectedOutput: output).VerifyDiagnostics(); // no diagnostics
        }

        internal CompilationVerifier VerifyOutputInMain(string methodBody, string output, params string[] usings)
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
    }
}
