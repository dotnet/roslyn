// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class NamedAndOptionalTests : CompilingTestBase
    {
        [Fact]
        public void Test13984()
        {
            string source = @"
using System;
class Program
{
    static void Main() { }
    static void M(DateTime da = new DateTime(2012, 6, 22),
                  decimal d = new decimal(5),
                  int i = new int())
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS1736: Default parameter value for 'da' must be a compile-time constant
                //     static void M(DateTime da = new DateTime(2012, 6, 22),
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new DateTime(2012, 6, 22)").WithArguments("da"),

                // (7,31): error CS1736: Default parameter value for 'd' must be a compile-time constant
                //                   decimal d = new decimal(5),
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new decimal(5)").WithArguments("d"));
        }


        [Fact]
        public void Test13861()
        {
            // * There are two decimal constant attribute constructors; we should honour both of them.
            // * Using named arguments to re-order the arguments must not change the value of the constant.

            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
 
class Program
{
    public static void Foo1([Optional][DecimalConstant(0, 0, low: (uint)100, mid: (uint)0, hi: (uint)0)] decimal i)
    {
        System.Console.Write(i);
    }
    public static void Foo2([Optional][DecimalConstant(0, 0, 0, 0, 200)] decimal i)
    {
        System.Console.Write(i);
    }
    static void Main(string[] args)
    {
        Foo1();
        Foo2();
    }
}";
            string expected = "100200";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void TestNamedAndOptionalParamsInCtors()
        {
            string source = @"
class Alpha
{
    public Alpha(int x = 123) { }
}

class Bravo : Alpha
{   // See bug 7846.
    // This should be legal; the generated ctor for Bravo should call base(123)
}

class Charlie : Alpha
{
    public Charlie() : base() {} 
    // This should be legal; should call base(123)
}

class Delta : Alpha
{
    public Delta() {} 
    // This should be legal; should call base(123)
}

abstract class Echo
{
    protected Echo(int x = 123) {}
}

abstract class Foxtrot : Echo
{
}

abstract class Hotel : Echo
{
    protected Hotel() {}
}

abstract class Golf : Echo
{
    protected Golf() : base() {}
}



";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestNamedAndOptionalParamsErrors()
        {
            string source = @"

class Base
{
    public virtual void Foo(int reqParam1, 
                            int optParam1 = 0, 
                            int optParam2 = default(int), 
                            int optParam3 = new int(),
                            string optParam4 = null,
                            double optParam5 = 128L)
    {
    }
}

class Middle : Base
{
    //override and change the parameters names
    public override void Foo(int reqChParam1,
                             int optChParam1 = 0,
                             int optChParam2 = default(int),
                             int optChParam3 = new int(),
                             string optChParam4 = null,
                             double optChParam5 = 128L)
    {
    }
}

class C : Middle
{
    public void Q(params int[] x) {}

    public void M()
    {
        var c = new C();
        // calling child class parameters with base names
        // error CS1739: The best overload for 'Foo' does not have a parameter named 'optParam3'
        c.Foo(optParam3: 333, reqParam1: 111 , optParam2: 222, optParam1: 1111); 
        // error CS1738: Named argument specifications must appear after all fixed arguments have been specified
        c.Foo(optArg1: 3333, 11111);
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (37,15): error CS1739: The best overload for 'Foo' does not have a parameter named 'optParam3'
                //         c.Foo(optParam3: 333, reqParam1: 111 , optParam2: 222, optParam1: 1111); 
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "optParam3").WithArguments("Foo", "optParam3").WithLocation(37, 15),
                // (39,30): error CS1738: Named argument specifications must appear after all fixed arguments have been specified
                //         c.Foo(optArg1: 3333, 11111);
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "11111").WithLocation(39, 30));
        }

        [Fact]
        public void TestNamedAndOptionalParamsErrors2()
        {
            string source = @"
class C
{
    //error CS1736 
    public void M(string s = new string('c',5)) {}
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,30): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     public void M(string s = new string('c',5)) {}
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new string('c',5)").WithArguments("s").WithLocation(5, 30));
        }

        [Fact]
        public void TestNamedAndOptionalParamsErrors3()
        {
            // Here we cannot report that "no overload of M takes two arguments" because of course
            // M(1, 2) is legal. We cannot report that any argument does not correspond to a formal;
            // all of them do. We cannot report that named arguments precede positional arguments.
            // We cannot report that any argument is not convertible to its corresponding formal;
            // all of them are convertible. The only error we can report here is that a formal 
            // parameter has no corresponding argument.

            string source = @"
class C
{
    // CS7036 (ERR_NoCorrespondingArgument) 
    delegate void F(int fx, int fg, int fz = 123);
    C(int cx, int cy, int cz = 123) {}
    public static void M(int mx, int my, int mz = 123) 
    {
        F f = null;
        f(0, fz : 456);
        M(0, mz : 456);
        new C(0, cz : 456);
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'fg' of 'C.F'
                //         f(0, fz : 456);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "f").WithArguments("fg", "C.F").WithLocation(10, 9),
                // (11,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'my' of 'C.M(int, int, int)'
                //         M(0, mz : 456);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("my", "C.M(int, int, int)").WithLocation(11, 9),
                // (12,13): error CS7036: There is no argument given that corresponds to the required formal parameter 'cy' of 'C.C(int, int, int)'
                //         new C(0, cz : 456);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("cy", "C.C(int, int, int)").WithLocation(12, 13));
        }

        [Fact]
        public void TestNamedAndOptionalParamsCrazy()
        {
            // This was never supposed to work and the spec does not require it, but
            // nevertheless, the native compiler allows this:
            const string source = @"
class C
{
  static void C(int q = 10, params int[] x) {}
  static int X() { return 123; }
  static int Q() { return 345; }
  static void M()
  {
    C(x:X(), q:Q());
  }
}";
            // and so Roslyn does too. It seems likely that someone has taken a dependency
            // on the bad pattern.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,15): error CS0542: 'C': member names cannot be the same as their enclosing type
                //   static void C(int q = 10, params int[] x) {}
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "C").WithArguments("C").WithLocation(4, 15));
        }

        [Fact]
        public void TestNamedAndOptionalParamsCrazyError()
        {
            // Fortunately, however, this is still illegal:
            const string source = @"
class C
{
  static void C(int q = 10, params int[] x) {}
  static void M()
  {
    C(1, 2, 3, x:4);
  }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,15): error CS0542: 'C': member names cannot be the same as their enclosing type
                //   static void C(int q = 10, params int[] x) {}
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "C").WithArguments("C").WithLocation(4, 15),
                // (7,16): error CS1744: Named argument 'x' specifies a parameter for which a positional argument has already been given
                //     C(1, 2, 3, x:4);
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "x").WithArguments("x").WithLocation(7, 16));
        }

        [Fact]
        public void TestNamedAndOptionalParamsBasic()
        {
            string source = @"
    using System;

    public enum E
    {
        zero,
        one,
        two,
        three
    }

    public enum ELong : long
    {
        zero,
        one,
        two,
        three
    }

    public class EnumDefaultValues
    {
        public static void Run()
        {
            var x = new EnumDefaultValues();
            x.M();
        }

        void M(
            E e1 = 0,
            E e2 = default(E),
            E e3 = E.zero,
            E e4 = E.one,
            E? ne1 = 0,
            E? ne2 = default(E),
            E? ne3 = E.zero,
            E? ne4 = E.one,
            E? ne5 = null,
            E? ne6 = default(E?),
            ELong el1 = 0,
            ELong el2 = default(ELong),
            ELong el3 = ELong.zero,
            ELong el4 = ELong.one,
            ELong? nel1 = 0,
            ELong? nel2 = default(ELong),
            ELong? nel3 = ELong.zero,
            ELong? nel4 = ELong.one,
            ELong? nel5 = null,
            ELong? nel6 = default(ELong?)
            )
        {
            Show(e1);
            Show(e2);
            Show(e3);
            Show(e4);
            Show(ne1);
            Show(ne2);
            Show(ne3);
            Show(ne4);
            Show(ne5);
            Show(ne6);
            Show(el1);
            Show(el2);
            Show(el3);
            Show(el4);
            Show(nel1);
            Show(nel2);
            Show(nel3);
            Show(nel4);
            Show(nel5);
            Show(nel6);
        }

        static void Show<T>(T t)
        {
            object o = t;
            Console.WriteLine(""{0}: {1}"", typeof(T), o != null ? o : ""<null>"");
        }

    }

    struct Sierra
    {
        public Alpha alpha;
        public Bravo bravo;
        public int i;

        public Sierra(Alpha alpha, Bravo bravo, int i)
        {
            this.alpha = alpha;
            this.bravo = bravo;
            this.i = i;
        }
    }

    class Alpha
    {
        public virtual int Mike(int xray)
        {
            return xray;
        }
    }

    class Bravo : Alpha
    {
        public override int Mike(int yankee)
        {
            return yankee;
        }
    }

    class Charlie : Bravo
    {
        void Foxtrot(
            int xray = 10, 
            string yankee = ""sam"")
        {
            Console.WriteLine(""Foxtrot: xray={0} yankee={1}"", xray, yankee);
        }

        void Quebec(
            int xray, 
            int yankee = 10, 
            int zulu = 11)
        {
            Console.WriteLine(""Quebec: xray={0} yankee={1} zulu={2}"", xray, yankee, zulu);
        }

        void OutRef(
            out int xray, 
            ref int yankee)
        {
            xray = 0;
            yankee = 0;
        }

        void ParamArray(params int[] xray)
        {
            Console.WriteLine(""ParamArray: xray={0}"", string.Join<int>("","", xray));
        }

        void ParamArray2(
            int yankee = 10, 
            params int[] xray)
        {
            Console.WriteLine(""ParamArray2: yankee={0} xray={1}"", yankee, string.Join<int>("","", xray));
        }

        void Zeros(
            int xray = 0, 
            int? yankee = 0, 
            int? zulu = null, 
            Charlie charlie = null)
        {
            Console.WriteLine(""Zeros: xray={0} yankee={1} zulu={2} charlie={3}"", 
                xray, 
                yankee == null ? ""null"" : yankee.ToString(), 
                zulu == null ? ""null"" : zulu.ToString(), 
                charlie == null ? ""null"" : charlie.ToString() );
        }

        void OtherDefaults(
            string str = default(string),
            Alpha alpha = default(Alpha),
            Bravo bravo = default(Bravo),
            int i = default(int),
            Sierra sierra = default(Sierra))
        {
            Console.WriteLine(""OtherDefaults: str={0} alpha={1} bravo={2} i={3} sierra={4}"",
                str == null ? ""null"" : str,
                alpha == null ? ""null"" : alpha.ToString(),
                bravo == null ? ""null"" : bravo.ToString(),
                i,
                sierra.alpha == null && sierra.bravo == null && sierra.i == 0 ? ""default(Sierra)"" : sierra.ToString());
        }

        int Bar()
        {
            Console.WriteLine(""Bar"");
            return 96;
        }

        string Baz()
        {
            Console.WriteLine(""Baz"");
            return ""Baz"";
        }

        void BasicOptionalTests()
        {
            Console.WriteLine(""BasicOptional"");
            Foxtrot(0);
            Foxtrot();
            ParamArray(1, 2, 3);
            Zeros();
            OtherDefaults();
        }

        void BasicNamedTests()
        {
            Console.WriteLine(""BasicNamed"");
            // Basic named test.
            Foxtrot(yankee: ""test"", xray: 1);
            Foxtrot(xray: 1, yankee: ""test"");

            // Test to see which execution comes first.
            Foxtrot(yankee: Baz(), xray: Bar());

            int y = 100;
            int x = 100;
            OutRef(yankee: ref y, xray: out x);
            Console.WriteLine(x);
            Console.WriteLine(y);

            Charlie c = new Charlie();

            ParamArray(xray: 1);
            ParamArray(xray: new int[] { 1, 2, 3 });
            ParamArray2(xray: 1);
            ParamArray2(xray: new int[] { 1, 2, 3 });
            ParamArray2(xray: 1, yankee: 20);
            ParamArray2(xray: new int[] { 1, 2, 3 }, yankee: 20);
            ParamArray2();
        }

        void BasicNamedAndOptionalTests()
        {
            Console.WriteLine(""BasicNamedAndOptional"");
            Foxtrot(yankee: ""test"");
            Foxtrot(xray: 0);

            Quebec(1, yankee: 1);
            Quebec(1, zulu: 10);
        }

        void OverrideTest()
        {
            Console.WriteLine(Mike(yankee: 10));
        }

        void TypeParamTest<T>() where T : Bravo, new()
        {
            T t = new T();
            Console.WriteLine(t.Mike(yankee: 4));
        }

        static void Main()
        {
            Charlie c = new Charlie();
            c.BasicOptionalTests();
            c.BasicNamedTests();
            c.BasicNamedAndOptionalTests();
            c.OverrideTest();
            c.TypeParamTest<Bravo>();
            EnumDefaultValues.Run();
        }
    }
";

            string expected = @"BasicOptional
Foxtrot: xray=0 yankee=sam
Foxtrot: xray=10 yankee=sam
ParamArray: xray=1,2,3
Zeros: xray=0 yankee=0 zulu=null charlie=null
OtherDefaults: str=null alpha=null bravo=null i=0 sierra=default(Sierra)
BasicNamed
Foxtrot: xray=1 yankee=test
Foxtrot: xray=1 yankee=test
Baz
Bar
Foxtrot: xray=96 yankee=Baz
0
0
ParamArray: xray=1
ParamArray: xray=1,2,3
ParamArray2: yankee=10 xray=1
ParamArray2: yankee=10 xray=1,2,3
ParamArray2: yankee=20 xray=1
ParamArray2: yankee=20 xray=1,2,3
ParamArray2: yankee=10 xray=
BasicNamedAndOptional
Foxtrot: xray=10 yankee=test
Foxtrot: xray=0 yankee=sam
Quebec: xray=1 yankee=1 zulu=11
Quebec: xray=1 yankee=10 zulu=10
10
4
E: zero
E: zero
E: zero
E: one
System.Nullable`1[E]: zero
System.Nullable`1[E]: zero
System.Nullable`1[E]: zero
System.Nullable`1[E]: one
System.Nullable`1[E]: <null>
System.Nullable`1[E]: <null>
ELong: zero
ELong: zero
ELong: zero
ELong: one
System.Nullable`1[ELong]: zero
System.Nullable`1[ELong]: zero
System.Nullable`1[ELong]: zero
System.Nullable`1[ELong]: one
System.Nullable`1[ELong]: <null>
System.Nullable`1[ELong]: <null>";

            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void TestNamedAndOptionalParamsOnAttributes()
        {
            string source = @"
using System;
class MyAttribute : Attribute
{
    public MyAttribute(int a = 1, int b = 2, int c = 3) 
    {
        A = a;
        B = b;
        C = c;
    }
    public int X;
    public int A;
    public int B;
    public int C;
}

[MyAttribute(4, c:5, X=6)]
class C 
{ 
    static void Main() 
    {
        MyAttribute m1 = new MyAttribute();
        Console.Write(m1.A); // 1
        Console.Write(m1.B); // 2
        Console.Write(m1.C); // 3
        Console.Write(m1.X); // 0
        MyAttribute m2 = new MyAttribute(c: 7);
        Console.Write(m2.A); // 1
        Console.Write(m2.B); // 2
        Console.Write(m2.C); // 7
        Console.Write(m2.X); // 0

        Type t = typeof(C);

        foreach (MyAttribute attr in t.GetCustomAttributes(false))
        {
            Console.Write(attr.A); // 4
            Console.Write(attr.B); // 2
            Console.Write(attr.C); // 5
            Console.Write(attr.X); // 6
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "123012704256");
        }

        [Fact]
        public void TestNamedAndOptionalParamsOnIndexers()
        {
            string source = @"
using System; 
class D
{
    public int this[string s = ""four""] { get { return s.Length; } set { } }
    public int this[int x = 2, int y = 5] { get { return x + y; } set { } }
    public int this[string str = ""foo"", int i = 13] 
    { 
        get { Console.WriteLine(""D.this[str: '{0}', i: {1}].get"", str, i); return i;}
        set { Console.WriteLine(""D.this[str: '{0}', i: {1}].set"", str, i); }
    }
}
            
class C
{
    int this[int x, int y] { get { return x + y; } set { } }
    static void Main()
    {
        C c = new C();
        Console.WriteLine(c[y:10, x:10]);
        D d = new D();
        Console.WriteLine(d[1]);
        Console.WriteLine(d[0,2]);
        Console.WriteLine(d[x:2]);
        Console.WriteLine(d[x:3, y:0]);
        Console.WriteLine(d[y:3, x:2]);
        Console.WriteLine(d[""abc""]);
        Console.WriteLine(d[s:""12345""]);
        d[i:1] = 0;
        d[str:""bar""] = 0;
        d[i:2, str:""baz""] = 0;
        d[str:""bah"", i:3] = 0;
    }
}";

            string expected = @"20
6
2
7
3
5
3
5
D.this[str: 'foo', i: 1].set
D.this[str: 'bar', i: 13].set
D.this[str: 'baz', i: 2].set
D.this[str: 'bah', i: 3].set";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void TestNamedAndOptionalParamsOnPartialMethods()
        {
            string source = @"
using System; 
partial class C
{
    static partial void PartialMethod(int x);
}
partial class C
{
    static partial void PartialMethod(int y) { Console.WriteLine(y); }
    static void Main()
    {
        // Declaring partial wins.
        PartialMethod(x:123);
    }
}";
            string expected = "123";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void TestNamedAndOptionalParamsOnPartialMethodsErrors()
        {
            string source = @"
using System; 
partial class C
{
    static partial void PartialMethod(int x);
}
partial class C
{
    static partial void PartialMethod(int y) { Console.WriteLine(y); }
    static void Main()
    {
        // Implementing partial loses.
        PartialMethod(y:123);
    }
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
// (13,23): error CS1739: The best overload for 'PartialMethod' does not have a parameter named 'y'
//         PartialMethod(y:123);
Diagnostic(ErrorCode.ERR_BadNamedArgument, "y").WithArguments("PartialMethod", "y")
                );
        }

        [Fact]
        public void TestNamedAndOptionalParametersUnsafe()
        {
            string source = @"
using System;
unsafe class C
{
    static void M(
        int* x1 = default(int*), 
        IntPtr x2 = default(IntPtr), 
        UIntPtr x3 = default(UIntPtr),
        int x4 = default(int))
    {
    }
    static void Main()
    {
    	M();
    }
}";

            // We make an improvement on the native compiler here; we generate default(UIntPtr) and 
            // default(IntPtr) as "load zero, convert to type", rather than making a stack slot and calling
            // init on it.

            var c = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll);

            c.VerifyIL("C.Main", @"{
  // Code size       13 (0xd)
  .maxstack  4
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  ldc.i4.0
  IL_0003:  conv.i
  IL_0004:  ldc.i4.0
  IL_0005:  conv.u
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""void C.M(int*, System.IntPtr, System.UIntPtr, int)""
  IL_000c:  ret
}");
        }

        [WorkItem(528783, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528783")]
        [Fact]
        public void TestNamedAndOptionalParametersArgumentName()
        {
            const string text = @"
using System;

namespace NS
{
    class Test
    {
        static void M(sbyte sb = 0, string ss = null) {}
        static void Main()
        {
            M(/*<bind>*/ss/*</bind>*/: ""QC"");
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var nodeAndModel = GetBindingNodeAndModel<IdentifierNameSyntax>(comp);

            var typeInfo = nodeAndModel.Item2.GetTypeInfo(nodeAndModel.Item1);
            // parameter name has no type
            Assert.Null(typeInfo.Type);

            var symInfo = nodeAndModel.Item2.GetSymbolInfo(nodeAndModel.Item1);
            Assert.NotNull(symInfo.Symbol);
            Assert.Equal(SymbolKind.Parameter, symInfo.Symbol.Kind);
            Assert.Equal("ss", symInfo.Symbol.Name);
        }

        [WorkItem(542418, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542418")]
        [Fact]
        public void OptionalValueInvokesInstanceMethod()
        {
            var source =
@"class C
{
    object F() { return null; }
    void M1(object value = F()) { }
    object M2(object value = M2()) { return null; }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,28): error CS1736: Default parameter value for 'value' must be a compile-time constant
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "F()").WithArguments("value").WithLocation(4, 28),
                // (5,30): error CS1736: Default parameter value for 'value' must be a compile-time constant
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M2()").WithArguments("value").WithLocation(5, 30));
        }

        [Fact]
        public void OptionalValueInvokesStaticMethod()
        {
            var source =
@"class C
{
    static object F() { return null; }
    static void M1(object value = F()) { }
    static object M2(object value = M2()) { return null; }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,35): error CS1736: Default parameter value for 'value' must be a compile-time constant
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "F()").WithArguments("value").WithLocation(4, 35),
                // (5,37): error CS1736: Default parameter value for 'value' must be a compile-time constant
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M2()").WithArguments("value").WithLocation(5, 37));
        }

        [WorkItem(542411, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542411")]
        [WorkItem(542365, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542365")]
        [Fact]
        public void GenericOptionalParameters()
        {
            var source =
@"class C
{
    static void Foo<T>(T t = default(T)) {}
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }


        [WorkItem(542458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542458")]
        [Fact]
        public void OptionalValueTypeFromReferencedAssembly()
        {
            // public struct S{}
            // public class C
            // {
            //     public static void Foo(string s, S t = default(S)) {}
            // }            
            string ilSource = @"
// =============== CLASS MEMBERS DECLARATION ===================

.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class S

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  Foo(string s,
                                            [opt] valuetype S t) cil managed
  {
    .param [2] = nullref
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method C::Foo
} // end of class C

";

            var source =
                @"
public class D
{
    public static void Caller()
    {
        C.Foo("""");
    }
}";

            CompileWithCustomILSource(source, ilSource);
        }

        [WorkItem(542867, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542867")]
        [Fact]
        public void OptionalParameterDeclaredWithAttributes()
        {
            string source = @"
using System.Runtime.InteropServices;

public class Parent{
    public int Foo([Optional]object i = null) {
        return 1;
    }

    public int Bar([DefaultParameterValue(1)]int i = 2) {
        return 1;
    }
}

class Test{
    public static int Main(){
        Parent p = new Parent();
        return p.Foo();
    }
}
";
            CreateCompilationWithMscorlib(source, new[] { SystemRef }).VerifyDiagnostics(
                // (5,21): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
                Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "Optional"),
                // (9,21): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
                Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "DefaultParameterValue"));
        }

        [WorkItem(10290, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void OptionalParamOfTypeObject()
        {
            string source = @"
public class Test
{
    public static int M1(object p1 = null) { if (p1 == null) return 0; else return 1; }
    public static void Main()
    {
        System.Console.WriteLine(M1());
    }
}
";
            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(543871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543871")]
        [Fact]
        public void RefParameterDeclaredWithOptionalAttribute()
        {
            // The native compiler produces "CS1501: No overload for method 'Foo' takes 0 arguments."
            // Roslyn produces a slightly more informative error message.

            string source = @"
using System.Runtime.InteropServices;
public class Parent
{
     public static void Foo([Optional] ref int x) {}
     static void Main()
     {
         Foo();
     }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemRef });
            comp.VerifyDiagnostics(
 // (8,10): error CS7036: There is no argument given that corresponds to the required formal parameter 'x' of 'Parent.Foo(ref int)'
 //          Foo();
 Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Foo").WithArguments("x", "Parent.Foo(ref int)"));
        }

        [Fact, WorkItem(544491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544491")]
        public void EnumAsDefaultParameterValue()
        {
            string source = @"
using System.Runtime.InteropServices;

public enum MyEnum { one, two, three }
public interface IOptionalRef
{
    MyEnum MethodRef([In, Out, Optional, DefaultParameterValue(MyEnum.three)] ref MyEnum v);
}
";
            CompileAndVerify(source, new[] { SystemRef }).VerifyDiagnostics();
        }

        [Fact]
        public void DefaultParameterValueErrors()
        {
            string source = @"
using System.Runtime.InteropServices;

public enum I8 : sbyte { v = 1 }
public enum U8 : byte { v = 1 }

public enum I16 : short { v = 1 }
public enum U16 : ushort { v = 1 }

public enum I32 : int { v = 1 }
public enum U32 : uint { v = 1 }

public enum I64 : long { v = 1 }
public enum U64 : ulong { v = 1 }

public class C { }
public delegate void D();
public interface I { }
public struct S {
}

public static class ErrorCases
{

    public static void M(
        // bool
        [Optional][DefaultParameterValue(0)]         bool b1,
        [Optional][DefaultParameterValue(""hello"")]   bool b2,

        // integral
        [Optional][DefaultParameterValue(12)]        sbyte sb1,
        [Optional][DefaultParameterValue(""hello"")]   byte by1,

        // char
        [Optional][DefaultParameterValue(""c"")]       char ch1,

        // float
        [Optional][DefaultParameterValue(1.0)]       float fl1,
        [Optional][DefaultParameterValue(1)]         double dbl1,

        // enum
        [Optional][DefaultParameterValue(0)]         I8 i8,
        [Optional][DefaultParameterValue(12)]        U8 u8,
        [Optional][DefaultParameterValue(""hello"")]   I16 i16,

        // string
        [Optional][DefaultParameterValue(5)]         string str1,
        [Optional][DefaultParameterValue(new int[] { 12 })] string str2,

        // reference types
        [Optional][DefaultParameterValue(2)]         C c1,
        [Optional][DefaultParameterValue(""hello"")]   C c2,

        [DefaultParameterValue(new int[] { 1, 2 })]  int[] arr1,
        [DefaultParameterValue(null)]                int[] arr2,
        [DefaultParameterValue(new int[] { 1, 2 })]  object arr3,

        [DefaultParameterValue(typeof(object))]      System.Type type1,
        [DefaultParameterValue(null)]                System.Type type2,
        [DefaultParameterValue(typeof(object))]      object type3,

        // user defined struct
        [DefaultParameterValue(null)]                S userStruct1,
        [DefaultParameterValue(0)]                   S userStruct2,
        [DefaultParameterValue(""hel"")]               S userStruct3,

        // null value to non-ref type
        [Optional][DefaultParameterValue(null)]      bool b3,

        // integral
        [Optional][DefaultParameterValue(null)]      int i2,

        // char
        [Optional][DefaultParameterValue(null)]      char ch2,

        // float
        [Optional][DefaultParameterValue(null)]      float fl2,

        // enum
        [Optional][DefaultParameterValue(null)]      I8 i82
       )
    {
    }
}
";
            // NOTE: anywhere dev10 reported CS1909, roslyn reports CS1910.
            CreateCompilationWithMscorlib(source, new[] { SystemRef }).VerifyDiagnostics(
                // (27,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(0)]         bool b1,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (28,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue("hello")]   bool b2,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (31,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(12)]        sbyte sb1,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (32,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue("hello")]   byte by1,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (35,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue("c")]       char ch1,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (38,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(1.0)]       float fl1,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (42,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(0)]         I8 i8,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (43,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(12)]        U8 u8,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (44,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue("hello")]   I16 i16,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (47,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(5)]         string str1,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (48,20): error CS1910: Argument of type 'int[]' is not applicable for the DefaultParameterValue attribute
                //         [Optional][DefaultParameterValue(new int[] { 12 })] string str2,
                Diagnostic(ErrorCode.ERR_DefaultValueBadValueType, "DefaultParameterValue").WithArguments("int[]"),
                // (51,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(2)]         C c1,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (52,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue("hello")]   C c2,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (54,10): error CS1910: Argument of type 'int[]' is not applicable for the DefaultParameterValue attribute
                //         [DefaultParameterValue(new int[] { 1, 2 })]  int[] arr1,
                Diagnostic(ErrorCode.ERR_DefaultValueBadValueType, "DefaultParameterValue").WithArguments("int[]"),

                // NOTE: Roslyn specifically allows this usage (illegal in dev10).

                //// (55,10): error CS1909: The DefaultParameterValue attribute is not applicable on parameters of type 'int[]', unless the default value is null
                ////         [DefaultParameterValue(null)]                int[] arr2,
                //Diagnostic(ErrorCode.ERR_DefaultValueBadParamType, "DefaultParameterValue").WithArguments("int[]"),

                // (56,10): error CS1910: Argument of type 'int[]' is not applicable for the DefaultParameterValue attribute
                //         [DefaultParameterValue(new int[] { 1, 2 })]  object arr3,
                Diagnostic(ErrorCode.ERR_DefaultValueBadValueType, "DefaultParameterValue").WithArguments("int[]"),
                // (58,10): error CS1910: Argument of type 'System.Type' is not applicable for the DefaultParameterValue attribute
                //         [DefaultParameterValue(typeof(object))]      System.Type type1,
                Diagnostic(ErrorCode.ERR_DefaultValueBadValueType, "DefaultParameterValue").WithArguments("System.Type"),

                // NOTE: Roslyn specifically allows this usage (illegal in dev10).

                //// (59,10): error CS1909: The DefaultParameterValue attribute is not applicable on parameters of type 'System.Type', unless the default value is null
                ////         [DefaultParameterValue(null)]                System.Type type2,
                //Diagnostic(ErrorCode.ERR_DefaultValueBadParamType, "DefaultParameterValue").WithArguments("System.Type"),

                // (60,10): error CS1910: Argument of type 'System.Type' is not applicable for the DefaultParameterValue attribute
                //         [DefaultParameterValue(typeof(object))]      object type3,
                Diagnostic(ErrorCode.ERR_DefaultValueBadValueType, "DefaultParameterValue").WithArguments("System.Type"),
                // (63,10): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [DefaultParameterValue(null)]                S userStruct1,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (64,10): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [DefaultParameterValue(0)]                   S userStruct2,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (65,10): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [DefaultParameterValue("hel")]               S userStruct3,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (68,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(null)]      bool b3,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (71,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(null)]      int i2,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (74,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(null)]      char ch2,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (77,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(null)]      float fl2,
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (80,20): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //         [Optional][DefaultParameterValue(null)]      I8 i82
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"));
        }

        [WorkItem(544440, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544440")]
        [ClrOnlyFact]
        public void TestBug12768()
        {
            string sourceDefinitions = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class C
{
  public static void M1(object x = null)
  {
    Console.WriteLine(x ?? 1);
  }
  public static void M2([Optional] object x)
  {
    Console.WriteLine(x ?? 2);
  }
  public static void M3([MarshalAs(UnmanagedType.Interface)] object x = null)
  {
    Console.WriteLine(x ?? 3);
  }
  public static void M4([MarshalAs(UnmanagedType.Interface)][Optional] object x)
  {
    Console.WriteLine(x ?? 4);
  }
  public static void M5([IDispatchConstant] object x = null)
  {
    Console.WriteLine(x ?? 5);
  }
  public static void M6([IDispatchConstant] [Optional] object x)
  {
    Console.WriteLine(x ?? 6);
  }
  public static void M7([IDispatchConstant] [MarshalAs(UnmanagedType.Interface)] object x = null)
  {
    Console.WriteLine(x ?? 7);
  }
  public static void M8([IDispatchConstant] [MarshalAs(UnmanagedType.Interface)][Optional] object x)
  {
    Console.WriteLine(x ?? 8);
  }
  public static void M9([IUnknownConstant]object x = null)
  {
    Console.WriteLine(x ?? 9);
  }
  public static void M10([IUnknownConstant][Optional] object x)
  {
    Console.WriteLine(x ?? 10);
  }
  public static void M11([IUnknownConstant][MarshalAs(UnmanagedType.Interface)] object x = null)
  {
    Console.WriteLine(x ?? 11);
  }
  public static void M12([IUnknownConstant][MarshalAs(UnmanagedType.Interface)][Optional] object x)
  {
    Console.WriteLine(x ?? 12);
  }
  public static void M13([IUnknownConstant][IDispatchConstant] object x = null)
  {
    Console.WriteLine(x ?? 13);
  }
  public static void M14([IDispatchConstant][IUnknownConstant][Optional] object x)
  {
    Console.WriteLine(x ?? 14);
  }
  public static void M15([IUnknownConstant][IDispatchConstant] [MarshalAs(UnmanagedType.Interface)] object x = null)
  {
    Console.WriteLine(x ?? 15);
  }
  public static void M16([IUnknownConstant][IDispatchConstant] [MarshalAs(UnmanagedType.Interface)][Optional] object x)
  {
    Console.WriteLine(x ?? 16);
  }
  public static void M17([MarshalAs(UnmanagedType.Interface)][IDispatchConstant][Optional] object x)
  {
    Console.WriteLine(x ?? 17);
  }
  public static void M18([MarshalAs(UnmanagedType.Interface)][IUnknownConstant][Optional] object x)
  {
    Console.WriteLine(x ?? 18);
  }
}
";
            string sourceCalls = @"
internal class D
{  
  static void Main()
  {
      C.M1(); // null
      C.M2(); // Missing
      C.M3(); // null
      C.M4(); // null
      C.M5(); // null
      C.M6(); // DispatchWrapper
      C.M7(); // null
      C.M8(); // null 
      C.M9(); // null
      C.M10(); // UnknownWrapper
      C.M11(); // null
      C.M12(); // null
      C.M13(); // null
      C.M14(); // UnknownWrapper
      C.M15(); // null
      C.M16(); // null
      C.M17(); // null
      C.M18(); // null
  }
}";

            string expected = @"1
System.Reflection.Missing
3
4
5
System.Runtime.InteropServices.DispatchWrapper
7
8
9
System.Runtime.InteropServices.UnknownWrapper
11
12
13
System.Runtime.InteropServices.UnknownWrapper
15
16
17
18";
            // definitions in source:
            var verifier = CompileAndVerify(new[] { sourceDefinitions, sourceCalls }, new[] { SystemRef }, expectedOutput: expected);

            // definitions in metadata:
            using (var assembly = AssemblyMetadata.CreateFromImage(verifier.EmittedAssemblyData))
            {
                CompileAndVerify(new[] { sourceCalls }, new[] { SystemRef, assembly.GetReference() }, expectedOutput: expected);
            }
        }

        [WorkItem(545329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545329")]
        [Fact()]
        public void ComOptionalRefParameter()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""00020813-0000-0000-c000-000000000046"")]
interface ComClass
{
    void M([Optional]ref object o);
}

class D : ComClass
{
    public void M(ref object o)
    {
    }
}

class C
{
    static void Main()
    {
        D d = new D();
        ComClass c = d;
        c.M(); //fine
        d.M(); //CS1501
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (25,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'o' of 'D.M(ref object)'
                //         d.M(); //CS1501
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("o", "D.M(ref object)").WithLocation(25, 11));
        }

        [WorkItem(545337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545337")]
        [ClrOnlyFact]
        public void TestVbDecimalAndDateTimeDefaultParameters()
        {
            var vb = @"
Imports System

public Module VBModule
    Sub I(Optional ByVal x As System.Int32 = 456)
        Console.WriteLine(x)
    End Sub
    Sub NI(Optional ByVal x As System.Int32? = 457)
        Console.WriteLine(x)
    End Sub
    Sub OI(Optional ByVal x As Object = 458 )
        Console.WriteLine(x)
    End Sub
    Sub DA(Optional ByVal x As DateTime = #1/2/2007#)
        Console.WriteLine(x = #1/2/2007#)
    End Sub
    Sub NDT(Optional ByVal x As DateTime? = #1/2/2007#)
        Console.WriteLine(x = #1/2/2007#)
    End Sub
    Sub ODT(Optional ByVal x As Object = #1/2/2007#)
        Console.WriteLine(x = #1/2/2007#)
    End Sub
    Sub Dec(Optional ByVal x as Decimal = 12.3D)
        Console.WriteLine(x.ToString(System.Globalization.CultureInfo.InvariantCulture))
    End Sub
    Sub NDec(Optional ByVal x as Decimal? = 12.4D)
        Console.WriteLine(x.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
    End Sub
    Sub ODec(Optional ByVal x as Object = 12.5D)
        Console.WriteLine(DirectCast(x, Decimal).ToString(System.Globalization.CultureInfo.InvariantCulture))
    End Sub

End Module
";

            var csharp = @"
using System;

public class D
{
    static void Main()
    {
        // Ensure suites run in invariant culture across machines
        System.Threading.Thread.CurrentThread.CurrentCulture 
            = System.Globalization.CultureInfo.InvariantCulture;

        // Possible in both C# and VB:

        VBModule.I();
        VBModule.NI();

        VBModule.Dec();
        VBModule.NDec();

        // Not possible in C#, possible in VB, but C# honours the parameter:

        VBModule.OI();

        VBModule.ODec();

        VBModule.DA(); 
        VBModule.NDT(); 
        VBModule.ODT(); 
    }
}
";

            string expected = @"456
457
12.3
12.4
458
12.5
True
True
True";

            string il = @"{
  // Code size      181 (0xb5)
  .maxstack  5
  IL_0000:  call       ""System.Threading.Thread System.Threading.Thread.CurrentThread.get""
  IL_0005:  call       ""System.Globalization.CultureInfo System.Globalization.CultureInfo.InvariantCulture.get""
  IL_000a:  callvirt   ""void System.Threading.Thread.CurrentCulture.set""
  IL_000f:  ldc.i4     0x1c8
  IL_0014:  call       ""void VBModule.I(int)""
  IL_0019:  ldc.i4     0x1c9
  IL_001e:  newobj     ""int?..ctor(int)""
  IL_0023:  call       ""void VBModule.NI(int?)""
  IL_0028:  ldc.i4.s   123
  IL_002a:  ldc.i4.0
  IL_002b:  ldc.i4.0
  IL_002c:  ldc.i4.0
  IL_002d:  ldc.i4.1
  IL_002e:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_0033:  call       ""void VBModule.Dec(decimal)""
  IL_0038:  ldc.i4.s   124
  IL_003a:  ldc.i4.0
  IL_003b:  ldc.i4.0
  IL_003c:  ldc.i4.0
  IL_003d:  ldc.i4.1
  IL_003e:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_0043:  newobj     ""decimal?..ctor(decimal)""
  IL_0048:  call       ""void VBModule.NDec(decimal?)""
  IL_004d:  ldc.i4     0x1ca
  IL_0052:  box        ""int""
  IL_0057:  call       ""void VBModule.OI(object)""
  IL_005c:  ldc.i4.s   125
  IL_005e:  ldc.i4.0
  IL_005f:  ldc.i4.0
  IL_0060:  ldc.i4.0
  IL_0061:  ldc.i4.1
  IL_0062:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_0067:  box        ""decimal""
  IL_006c:  call       ""void VBModule.ODec(object)""
  IL_0071:  ldc.i8     0x8c8fc181490c000
  IL_007a:  newobj     ""System.DateTime..ctor(long)""
  IL_007f:  call       ""void VBModule.DA(System.DateTime)""
  IL_0084:  ldc.i8     0x8c8fc181490c000
  IL_008d:  newobj     ""System.DateTime..ctor(long)""
  IL_0092:  newobj     ""System.DateTime?..ctor(System.DateTime)""
  IL_0097:  call       ""void VBModule.NDT(System.DateTime?)""
  IL_009c:  ldc.i8     0x8c8fc181490c000
  IL_00a5:  newobj     ""System.DateTime..ctor(long)""
  IL_00aa:  box        ""System.DateTime""
  IL_00af:  call       ""void VBModule.ODT(object)""
  IL_00b4:  ret
}";

            var vbCompilation = CreateVisualBasicCompilation("VB", vb,
                compilationOptions: new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            vbCompilation.VerifyDiagnostics();

            var csharpCompilation = CreateCSharpCompilation("CS", csharp,
                compilationOptions: TestOptions.ReleaseExe,
                referencedCompilations: new[] { vbCompilation });

            var verifier = CompileAndVerify(csharpCompilation, expectedOutput: expected);
            verifier.VerifyIL("D.Main", il);
        }

        [WorkItem(545337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545337")]
        [Fact]
        public void TestCSharpDecimalAndDateTimeDefaultParameters()
        {
            var library = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public enum E
{
    one,
    two,
    three
}

public class C
{
    public void Foo(
            [Optional][DateTimeConstant(100000)]DateTime dateTime,
            decimal dec = 12345678901234567890m,
            int? x = 0,
            int? q = null,
            short y = 10,
            int z = default(int),
            S? s = null)
            //S? s2 = default(S))
    {
        if (dateTime == new DateTime(100000))
        {
            Console.WriteLine(""DatesMatch"");

        }
        else
        {
            Console.WriteLine(""Dates dont match!!"");
        }
        Write(dec);
        Write(x);
        Write(q);
        Write(y);
        Write(z);
        Write(s);
        //Write(s2);
    }

    public void Bar(S? s1, S? s2, S? s3)
    {
    }

    public void Baz(E? e1 = E.one, long? x = 0)
    {
        if (e1.HasValue)
        {
            Console.WriteLine(e1);
        }
        else
        {
            Console.WriteLine(""null"");
        }
        Console.WriteLine(x);
    }

    public void Write(object o)
    {
        if (o == null)
        {
            Console.WriteLine(""null"");
        }
        else
        {
            Console.WriteLine(o);
        }
    }
}

public struct S
{
}
";

            var main = @"
using System;

public class D
{
    static void Main()
    {
        // Ensure suites run in invariant culture across machines
        System.Threading.Thread.CurrentThread.CurrentCulture 
            = System.Globalization.CultureInfo.InvariantCulture;

        C c = new C();
        c.Foo();
        c.Baz();
    }
}
";

            var libComp = CreateCompilationWithMscorlib(library, options: TestOptions.ReleaseDll, assemblyName: "Library");
            libComp.VerifyDiagnostics();

            var exeComp = CreateCompilationWithMscorlib(main, new[] { new CSharpCompilationReference(libComp) }, options: TestOptions.ReleaseExe, assemblyName: "Main");

            var verifier = CompileAndVerify(exeComp, expectedOutput: @"DatesMatch
12345678901234567890
0
null
10
0
null
one
0");

            verifier.VerifyIL("D.Main", @"{
  // Code size       97 (0x61)
  .maxstack  9
  .locals init (int? V_0,
                S? V_1)
  IL_0000:  call       ""System.Threading.Thread System.Threading.Thread.CurrentThread.get""
  IL_0005:  call       ""System.Globalization.CultureInfo System.Globalization.CultureInfo.InvariantCulture.get""
  IL_000a:  callvirt   ""void System.Threading.Thread.CurrentCulture.set""
  IL_000f:  newobj     ""C..ctor()""
  IL_0014:  dup
  IL_0015:  ldc.i4     0x186a0
  IL_001a:  conv.i8
  IL_001b:  newobj     ""System.DateTime..ctor(long)""
  IL_0020:  ldc.i8     0xab54a98ceb1f0ad2
  IL_0029:  newobj     ""decimal..ctor(ulong)""
  IL_002e:  ldc.i4.0
  IL_002f:  newobj     ""int?..ctor(int)""
  IL_0034:  ldloca.s   V_0
  IL_0036:  initobj    ""int?""
  IL_003c:  ldloc.0
  IL_003d:  ldc.i4.s   10
  IL_003f:  ldc.i4.0
  IL_0040:  ldloca.s   V_1
  IL_0042:  initobj    ""S?""
  IL_0048:  ldloc.1
  IL_0049:  callvirt   ""void C.Foo(System.DateTime, decimal, int?, int?, short, int, S?)""
  IL_004e:  ldc.i4.0
  IL_004f:  newobj     ""E?..ctor(E)""
  IL_0054:  ldc.i4.0
  IL_0055:  conv.i8
  IL_0056:  newobj     ""long?..ctor(long)""
  IL_005b:  callvirt   ""void C.Baz(E?, long?)""
  IL_0060:  ret
}");
        }


        [Fact]
        public void OmittedComOutParameter()
        {
            // We allow omitting optional ref arguments but not optional out arguments.
            var source = @"
using System;
using System.Runtime.InteropServices;
[ComImport, Guid(""989FE455-5A6D-4D05-A349-1A221DA05FDA"")]
interface I
{
    void M([Optional]out object o);
}
class P
{
    static void Q(I i) { i.M(); }
}
";
            // Note that the native compiler gives a slightly less informative error message here.

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
// (11,26): error CS7036: There is no argument given that corresponds to the required formal parameter 'o' of 'I.M(out object)'
//     static void Q(I i) { i.M(); }
Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("o", "I.M(out object)")
                );
        }

        [Fact]
        public void OmittedComRefParameter()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""A8FAF53B-F502-4465-9429-CAB2A19B47BE"")]
interface ICom
{
    void M(out int w, int x, [Optional]ref object o, int z = 0);
}

class Com : ICom
{
    public void M(out int w, int x, ref object o, int z)
    {
        w = 123;
        Console.WriteLine(x);
        if (o != null)
        {
            Console.WriteLine(o.GetType());
        }
        else
        {
            Console.WriteLine(""null"");
        }
        Console.WriteLine(z);
    }

    static void Main()
    {
        ICom c = new Com();
        int q;
        c.M(w: out q, z: 10, x: 100);
        Console.WriteLine(q);
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"
100
System.Reflection.Missing
10
123");
            verifier.VerifyIL("Com.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  5
  .locals init (int V_0, //q
  object V_1)
  IL_0000:  newobj     ""Com..ctor()""
  IL_0005:  ldloca.s   V_0
  IL_0007:  ldc.i4.s   100
  IL_0009:  ldsfld     ""object System.Type.Missing""
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  ldc.i4.s   10
  IL_0013:  callvirt   ""void ICom.M(out int, int, ref object, int)""
  IL_0018:  ldloc.0
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void ArrayElementComRefParameter()
        {
            var source =
@"using System;
using System.Runtime.InteropServices;
[ComImport]
[Guid(""B107A073-4ACE-4057-B4BA-837891E3C274"")]
interface IA
{
    void M(ref int i);
}
class A : IA
{
    void IA.M(ref int i)
    {
        i += 2;
    }
}
class B
{
    static void M(IA a)
    {
        a.M(F()[0]);
    }
    static void MByRef(IA a)
    {
        a.M(ref F()[0]);
    }
    static int[] i = { 0 };
    static int[] F()
    {
        Console.WriteLine(""F()"");
        return i;
    }
    static void Main()
    {
        IA a = new A();
        M(a);
        ReportAndReset();
        MByRef(a);
        ReportAndReset();
    }
    static void ReportAndReset()
    {
        Console.WriteLine(""{0}"", i[0]);
        i = new[] { 0 };
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput:
@"F()
0
F()
2");
            verifier.VerifyIL("B.M(IA)",
@"{
  // Code size       17 (0x11)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int[] B.F()""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelem.i4
  IL_0008:  stloc.0
  IL_0009:  ldloca.s   V_0
  IL_000b:  callvirt   ""void IA.M(ref int)""
  IL_0010:  ret
}");
            verifier.VerifyIL("B.MByRef(IA)",
@"{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""int[] B.F()""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""int""
  IL_000c:  callvirt   ""void IA.M(ref int)""
  IL_0011:  ret
}");
        }

        [Fact]
        public void ArrayElementComRefParametersReordered()
        {
            var source =
@"using System;
using System.Runtime.InteropServices;
[ComImport]
[Guid(""B107A073-4ACE-4057-B4BA-837891E3C274"")]
interface IA
{
    void M(ref int x, ref int y);
}
class A : IA
{
    void IA.M(ref int x, ref int y)
    {
        x += 2;
        y += 3;
    }
}
class B
{
    static void M(IA a)
    {
        a.M(y: ref F2()[0], x: ref F1()[0]);
    }
    static int[] i1 = { 0 };
    static int[] i2 = { 0 };
    static int[] F1()
    {
        Console.WriteLine(""F1()"");
        return i1;
    }
    static int[] F2()
    {
        Console.WriteLine(""F2()"");
        return i2;
    }
    static void Main()
    {
        IA a = new A();
        M(a);
        Console.WriteLine(""{0}, {1}"", i1[0], i2[0]);
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput:
@"F2()
F1()
2, 3
");
            verifier.VerifyIL("B.M(IA)",
@"{
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (int& V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int[] B.F2()""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""int""
  IL_000c:  stloc.0
  IL_000d:  call       ""int[] B.F1()""
  IL_0012:  ldc.i4.0
  IL_0013:  ldelema    ""int""
  IL_0018:  ldloc.0
  IL_0019:  callvirt   ""void IA.M(ref int, ref int)""
  IL_001e:  ret
}");
        }

        [Fact]
        [WorkItem(546713, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546713")]
        public void Test16631()
        {
            var source =
@"    
public abstract class B
{
    protected abstract void E<T>();
}

public class D : B
{
    void M()
    {
        // There are two possible methods to choose here. The static method
        // is better because it is declared in a more derived class; the
        // virtual method is better because it has exactly the right number
        // of parameters. In this case, the static method wins. The virtual
        // method is to be treated as though it was a method of the base class,
        // and therefore automatically loses.  (The bug we are regressing here
        // is that Roslyn did not correctly identify the originally-defining
        // type B when the method E was generic. The original repro scenario in
        // bug 16631 was much more complicated than this, but it boiled down to
        // overload resolution choosing the wrong method.)

        E<int>();
    }

    protected override void E<T>()
    {
        System.Console.WriteLine(1);
    }

    static void E<T>(int x = 0xBEEF)
    {
        System.Console.WriteLine(2);
    }

    static void Main()
    {
        (new D()).M();
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "2");
            verifier.VerifyIL("D.M()",
@"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldc.i4     0xbeef
  IL_0005:  call       ""void D.E<int>(int)""
  IL_000a:  ret
}");
        }

        [Fact]
        [WorkItem(529775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529775")]
        public void IsOptionalVsHasDefaultValue_PrimitiveStruct()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class C
{
    public void M0(int p) { }
    public void M1(int p = 0) { } // default of type
    public void M2(int p = 1) { } // not default of type
    public void M3([Optional]int p) { } // no default specified (would be illegal)
    public void M4([DefaultParameterValue(0)]int p) { } // default of type, not optional
    public void M5([DefaultParameterValue(1)]int p) { } // not default of type, not optional
    public void M6([Optional][DefaultParameterValue(0)]int p) { } // default of type, optional
    public void M7([Optional][DefaultParameterValue(1)]int p) { } // not default of type, optional
}
";

            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var methods = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary).ToArray();
                Assert.Equal(8, methods.Length);

                var parameters = methods.Select(m => m.Parameters.Single()).ToArray();

                Assert.False(parameters[0].IsOptional);
                Assert.False(parameters[0].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[0].ExplicitDefaultValue);
                Assert.Null(parameters[0].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[0].GetAttributes().Length);

                Assert.True(parameters[1].IsOptional);
                Assert.True(parameters[1].HasExplicitDefaultValue);
                Assert.Equal(0, parameters[1].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create(0), parameters[1].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[1].GetAttributes().Length);

                Assert.True(parameters[2].IsOptional);
                Assert.True(parameters[2].HasExplicitDefaultValue);
                Assert.Equal(1, parameters[2].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create(1), parameters[2].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[2].GetAttributes().Length);

                Assert.True(parameters[3].IsOptional);
                Assert.False(parameters[3].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[3].ExplicitDefaultValue);
                Assert.Null(parameters[3].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 1 : 0, parameters[3].GetAttributes().Length);

                Assert.False(parameters[4].IsOptional);
                Assert.False(parameters[4].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[4].ExplicitDefaultValue);
                Assert.True(parameters[4].HasMetadataConstantValue);
                Assert.Equal(ConstantValue.Create(0), parameters[4].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 1 : 0, parameters[4].GetAttributes().Length);

                Assert.False(parameters[5].IsOptional);
                Assert.False(parameters[5].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[5].ExplicitDefaultValue);
                Assert.True(parameters[5].HasMetadataConstantValue);
                Assert.Equal(ConstantValue.Create(1), parameters[5].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 1 : 0, parameters[5].GetAttributes().Length);

                Assert.True(parameters[6].IsOptional);
                Assert.True(parameters[6].HasExplicitDefaultValue);
                Assert.Equal(0, parameters[6].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create(0), parameters[6].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 2 : 0, parameters[6].GetAttributes().Length);

                Assert.True(parameters[7].IsOptional);
                Assert.True(parameters[7].HasExplicitDefaultValue);
                Assert.Equal(1, parameters[7].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create(1), parameters[7].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 2 : 0, parameters[7].GetAttributes().Length);
            };

            CompileAndVerify(source, new[] { SystemRef }, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }

        [Fact]
        [WorkItem(529775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529775")]
        public void IsOptionalVsHasDefaultValue_UserDefinedStruct()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class C
{
    public void M0(S p) { }
    public void M1(S p = default(S)) { }
    public void M2([Optional]S p) { } // no default specified (would be illegal)
}

public struct S
{
    public int x;
}
";

            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var methods = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary).ToArray();
                Assert.Equal(3, methods.Length);

                var parameters = methods.Select(m => m.Parameters.Single()).ToArray();

                Assert.False(parameters[0].IsOptional);
                Assert.False(parameters[0].HasExplicitDefaultValue);
                Assert.Null(parameters[0].ExplicitDefaultConstantValue);
                Assert.Throws<InvalidOperationException>(() => parameters[0].ExplicitDefaultValue);
                Assert.Equal(0, parameters[0].GetAttributes().Length);

                Assert.True(parameters[1].IsOptional);
                Assert.True(parameters[1].HasExplicitDefaultValue);
                Assert.Null(parameters[1].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Null, parameters[1].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[1].GetAttributes().Length);

                Assert.True(parameters[2].IsOptional);
                Assert.False(parameters[2].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[2].ExplicitDefaultValue);
                Assert.Null(parameters[2].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 1 : 0, parameters[2].GetAttributes().Length);
            };

            // TODO: RefEmit doesn't emit the default value of M1's parameter.
            CompileAndVerify(source, new[] { SystemRef }, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }

        [Fact]
        [WorkItem(529775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529775")]
        public void IsOptionalVsHasDefaultValue_String()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class C
{
    public void M0(string p) { }
    public void M1(string p = null) { }
    public void M2(string p = ""A"") { }
    public void M3([Optional]string p) { } // no default specified (would be illegal)
    public void M4([DefaultParameterValue(null)]string p) { }
    public void M5([Optional][DefaultParameterValue(null)]string p) { }
    public void M6([DefaultParameterValue(""A"")]string p) { }
    public void M7([Optional][DefaultParameterValue(""A"")]string p) { }
}
";

            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var methods = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary).ToArray();
                Assert.Equal(8, methods.Length);

                var parameters = methods.Select(m => m.Parameters.Single()).ToArray();

                Assert.False(parameters[0].IsOptional);
                Assert.False(parameters[0].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[0].ExplicitDefaultValue);
                Assert.Null(parameters[0].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[0].GetAttributes().Length);

                Assert.True(parameters[1].IsOptional);
                Assert.True(parameters[1].HasExplicitDefaultValue);
                Assert.Null(parameters[1].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Null, parameters[1].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[1].GetAttributes().Length);

                Assert.True(parameters[2].IsOptional);
                Assert.True(parameters[2].HasExplicitDefaultValue);
                Assert.Equal("A", parameters[2].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create("A"), parameters[2].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[2].GetAttributes().Length);

                Assert.True(parameters[3].IsOptional);
                Assert.False(parameters[3].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[3].ExplicitDefaultValue);
                Assert.Null(parameters[3].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 1 : 0, parameters[3].GetAttributes().Length);

                Assert.False(parameters[4].IsOptional);
                Assert.False(parameters[4].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[4].ExplicitDefaultValue);
                Assert.True(parameters[4].HasMetadataConstantValue);
                Assert.Equal(ConstantValue.Null, parameters[4].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 1 : 0, parameters[4].GetAttributes().Length);

                Assert.True(parameters[5].IsOptional);
                Assert.True(parameters[5].HasExplicitDefaultValue);
                Assert.Null(parameters[5].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Null, parameters[5].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 2 : 0, parameters[5].GetAttributes().Length);

                Assert.False(parameters[6].IsOptional);
                Assert.False(parameters[6].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[6].ExplicitDefaultValue);
                Assert.True(parameters[6].HasMetadataConstantValue);
                Assert.Equal(ConstantValue.Create("A"), parameters[6].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 1 : 0, parameters[6].GetAttributes().Length);

                Assert.True(parameters[7].IsOptional);
                Assert.True(parameters[7].HasExplicitDefaultValue);
                Assert.Equal("A", parameters[7].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create("A"), parameters[7].ExplicitDefaultConstantValue); // not imported for non-optional parameter
                Assert.Equal(isFromSource ? 2 : 0, parameters[7].GetAttributes().Length);
            };

            CompileAndVerify(source, new[] { SystemRef }, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }

        [Fact]
        [WorkItem(529775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529775")]
        public void IsOptionalVsHasDefaultValue_Decimal()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class C
{
    public void M0(decimal p) { }
    public void M1(decimal p = 0) { } // default of type
    public void M2(decimal p = 1) { } // not default of type
    public void M3([Optional]decimal p) { } // no default specified (would be illegal)
    public void M4([DecimalConstant(0,0,0,0,0)]decimal p) { } // default of type, not optional
    public void M5([DecimalConstant(0,0,0,0,1)]decimal p) { } // not default of type, not optional
    public void M6([Optional][DecimalConstant(0,0,0,0,0)]decimal p) { } // default of type, optional
    public void M7([Optional][DecimalConstant(0,0,0,0,1)]decimal p) { } // not default of type, optional
}
";

            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var methods = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary).ToArray();
                Assert.Equal(8, methods.Length);

                var parameters = methods.Select(m => m.Parameters.Single()).ToArray();

                Assert.False(parameters[0].IsOptional);
                Assert.False(parameters[0].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[0].ExplicitDefaultValue);
                Assert.Null(parameters[0].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[0].GetAttributes().Length);

                Assert.True(parameters[1].IsOptional);
                Assert.True(parameters[1].HasExplicitDefaultValue);
                Assert.Equal(0M, parameters[1].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create(0M), parameters[1].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[1].GetAttributes().Length);

                Assert.True(parameters[2].IsOptional);
                Assert.True(parameters[2].HasExplicitDefaultValue);
                Assert.Equal(1M, parameters[2].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create(1M), parameters[2].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[2].GetAttributes().Length);

                Assert.True(parameters[3].IsOptional);
                Assert.False(parameters[3].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[3].ExplicitDefaultValue);
                Assert.Null(parameters[3].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 1 : 0, parameters[3].GetAttributes().Length);

                Assert.False(parameters[4].IsOptional);
                Assert.False(parameters[4].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[4].ExplicitDefaultValue);
                Assert.False(parameters[4].HasMetadataConstantValue);
                Assert.Equal(isFromSource ? ConstantValue.Create(0M) : null, parameters[4].ExplicitDefaultConstantValue); // not imported for non-optional parameter
                Assert.Equal(1, parameters[4].GetAttributes().Length); // DecimalConstantAttribute

                Assert.False(parameters[5].IsOptional);
                Assert.False(parameters[5].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[5].ExplicitDefaultValue);
                Assert.False(parameters[5].HasMetadataConstantValue);
                Assert.Equal(isFromSource ? ConstantValue.Create(1M) : null, parameters[5].ExplicitDefaultConstantValue); // not imported for non-optional parameter
                Assert.Equal(1, parameters[5].GetAttributes().Length); // DecimalConstantAttribute

                Assert.True(parameters[6].IsOptional);
                Assert.True(parameters[6].HasExplicitDefaultValue);
                Assert.Equal(0M, parameters[6].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create(0M), parameters[6].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 2 : 0, parameters[6].GetAttributes().Length); // Optional+DecimalConstantAttribute / DecimalConstantAttribute

                Assert.True(parameters[7].IsOptional);
                Assert.True(parameters[7].HasExplicitDefaultValue);
                Assert.Equal(1M, parameters[7].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create(1M), parameters[7].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 2 : 0, parameters[7].GetAttributes().Length); // Optional+DecimalConstantAttribute / DecimalConstantAttribute
            };

            CompileAndVerify(source, new[] { SystemRef }, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }

        [Fact]
        [WorkItem(529775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529775")]
        public void IsOptionalVsHasDefaultValue_DateTime()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class C
{
    public void M0(DateTime p) { }
    public void M1(DateTime p = default(DateTime)) { }
    public void M2([Optional]DateTime p) { } // no default specified (would be illegal)
    public void M3([DateTimeConstant(0)]DateTime p) { } // default of type, not optional
    public void M4([DateTimeConstant(1)]DateTime p) { } // not default of type, not optional
    public void M5([Optional][DateTimeConstant(0)]DateTime p) { } // default of type, optional
    public void M6([Optional][DateTimeConstant(1)]DateTime p) { } // not default of type, optional
}
";

            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var methods = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary).ToArray();
                Assert.Equal(7, methods.Length);

                var parameters = methods.Select(m => m.Parameters.Single()).ToArray();

                Assert.False(parameters[0].IsOptional);
                Assert.False(parameters[0].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[0].ExplicitDefaultValue);
                Assert.Null(parameters[0].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[0].GetAttributes().Length);

                Assert.True(parameters[1].IsOptional);
                Assert.True(parameters[1].HasExplicitDefaultValue);
                Assert.Null(parameters[1].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Null, parameters[1].ExplicitDefaultConstantValue);
                Assert.Equal(0, parameters[1].GetAttributes().Length); // As in dev11, [DateTimeConstant] is not emitted in this case.

                Assert.True(parameters[2].IsOptional);
                Assert.False(parameters[2].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[2].ExplicitDefaultValue);
                Assert.Null(parameters[2].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 1 : 0, parameters[2].GetAttributes().Length);

                Assert.False(parameters[3].IsOptional);
                Assert.False(parameters[3].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[3].ExplicitDefaultValue);
                Assert.False(parameters[3].HasMetadataConstantValue);
                Assert.Equal(isFromSource ? ConstantValue.Create(new DateTime(0)) : null, parameters[3].ExplicitDefaultConstantValue); // not imported for non-optional parameter
                Assert.Equal(1, parameters[3].GetAttributes().Length); // DateTimeConstant

                Assert.False(parameters[4].IsOptional);
                Assert.False(parameters[4].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => parameters[4].ExplicitDefaultValue);
                Assert.False(parameters[4].HasMetadataConstantValue);
                Assert.Equal(isFromSource ? ConstantValue.Create(new DateTime(1)) : null, parameters[4].ExplicitDefaultConstantValue); // not imported for non-optional parameter
                Assert.Equal(1, parameters[4].GetAttributes().Length); // DateTimeConstant

                Assert.True(parameters[5].IsOptional);
                Assert.True(parameters[5].HasExplicitDefaultValue);
                Assert.Equal(new DateTime(0), parameters[5].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create(new DateTime(0)), parameters[5].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 2 : 0, parameters[5].GetAttributes().Length); // Optional+DateTimeConstant / DateTimeConstant

                Assert.True(parameters[6].IsOptional);
                Assert.True(parameters[6].HasExplicitDefaultValue);
                Assert.Equal(new DateTime(1), parameters[6].ExplicitDefaultValue);
                Assert.Equal(ConstantValue.Create(new DateTime(1)), parameters[6].ExplicitDefaultConstantValue);
                Assert.Equal(isFromSource ? 2 : 0, parameters[6].GetAttributes().Length); // Optional+DateTimeConstant / DateTimeConstant
            };

            // TODO: Guess - RefEmit doesn't like DateTime constants.
            CompileAndVerify(source, new[] { SystemRef }, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }
    }
}
