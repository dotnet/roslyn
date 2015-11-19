// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class SyntaxBinderTests : CompilingTestBase
    {
        [Fact]
        public void TestMethodGroupConversionError()
        {
            string source = @"
using System;
using System.Linq.Expressions;
class C 
{ 
    static void Main() 
    { 
        Action a = Main;
        Expression<Action> e = Main;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source)
                .VerifyDiagnostics(
// (9,32): error CS0428: Cannot convert method group 'Main' to non-delegate type 'System.Linq.Expressions.Expression<System.Action>'. Did you intend to invoke the method?
//         Expression<Action> e = Main;
Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "System.Linq.Expressions.Expression<System.Action>")
                );
        }

        [Fact, WorkItem(546737, "DevDiv")]
        public void TestBug16693()
        {
            // We should treat I.Equals as hiding object.Equals, as noted in section 7.4.1 
            // of the specification. (That is, for the purposes of determining hiding,
            // object is considered to be a base type of the interface type.)

            string source = @"
public interface I
{
    bool Equals { get; }
}

class C
{
    bool M(I i) { return i.Equals; }
}

";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();
        }


        [Fact, WorkItem(546716, "DevDiv")]
        public void TestBug16639()
        {
            // The bug here was that the warning was reported for the first case -- the conversion 
            // to D -- but not for the second case -- the use of the anonymous function as the ctor argument.
            string source = @"
class Program
{
    delegate void D();
    class C {}
    static void Main()
    {
        var o1 = (D)(delegate{ var s = default(C).ToString();});
        var o2 = new D(delegate{ var s = default(C).ToString();});
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
// (8,40): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'Program.C' is null
//         var o1 = (D)(delegate{ var s = default(C).ToString();});
Diagnostic(ErrorCode.WRN_DotOnDefault, "default(C).ToString").WithArguments("Program.C"),

// (9,42): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'Program.C' is null
//         var o2 = new D(delegate{ var s = default(C).ToString();});
Diagnostic(ErrorCode.WRN_DotOnDefault, "default(C).ToString").WithArguments("Program.C")
                );
        }

        [Fact]
        public void TestParenthesisErrors()
        {
            TestErrors(@"
namespace N { class D { public static int x; } } 
class C 
{ 
    static int x;
    void M() 
    { 
        int y = (N).D.x;
        int z = (N.D).x;
    }
}
",
"'N' error CS0118: 'N' is a namespace but is used like a variable",
"'N.D' error CS0119: 'D' is a type, which is not valid in the given context");
        }

        [Fact]
        public void TestConstantFoldingErrors()
        {
            // UNDONE: integer overflows in checked contexts are not detected yet.
            TestErrors(@"
class C 
{ 
    void M() 
    { 
        int a = 10 / 0;
        double y = 10.0 % (-0.0); // not an error, makes an infinity
        decimal z = 10m % 0m;
    }
}
",
"'10m % 0m' error CS0020: Division by constant zero",
"'10 / 0' error CS0020: Division by constant zero");
        }

        [Fact]
        public void TestStatementBindingErrors()
        {
            TestErrors(@"
class C 
{ 
    void M() 
    { 
        x;
    }
}
",
"'x' error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement",
"'x' error CS0103: The name 'x' does not exist in the current context");
        }

        [Fact]
        public void TestLocalDeclarationErrors()
        {
            // In the native compiler we report both that Dx[] is an illegal array and that
            // it is an illegal type for a local or type argument. This seems unnecessary 
            // and redundant. In Roslyn I've eliminated the second error.

            TestErrors(@"
class C 
{ 
    class G<T> {}
    void M() 
    { 
        D1 x1; 
        D2[] x2;
        G<D3> x3;
        G<D4[]> x4;
    }
}
static class D1 { }
static class D2 { }
static class D3 { }
static class D4 { }
",
"'D1' error CS0723: Cannot declare a variable of static type 'D1'",
"'D2' error CS0719: 'D2': array elements cannot be of static type",
"'D3' error CS0718: 'D3': static types cannot be used as type arguments",
"'D4' error CS0719: 'D4': array elements cannot be of static type"
);
        }

        [Fact]
        public void TestOverloadResolutionErrors()
        {
            TestErrors(@"
using System;
using System.Collections;

// Some types that illustrate transitivity violations in convertibility,
// that then create interesting cases for overload resolution.
//class T1 { public static implicit operator T2(T1 t1) { return null; } }
//class T2 { public static implicit operator T3(T2 t2) { return null; } }
//class T3 { public static implicit operator T1(T3 t3) { return null; } }
//class T4 { public static implicit operator T1(T4 t4) { return null; } 
//           public static implicit operator T2(T4 t4) { return null; } 
//           public static implicit operator T3(T4 t4) { return null; } }
//class T5 { public static implicit operator T1(T5 t5) { return null; } 
//           public static implicit operator T2(T5 t5) { return null; } 
//           public static implicit operator T3(T5 t5) { return null; } }


class P // : IEnumerable
{
//    private void CallFinalize()
//    {
//        this.Finalize();
//        // error CS0245: Destructors and object.Finalize cannot be called directly. Consider
//        // calling IDisposable.Dispose if available.
//        base.Finalize();
//        // error CS0250: Do not directly call your base class Finalize method. It is called
//        // automatically from your destructor.
//    }
    public void GenericConstrained<T>(T t) where T : class { }
    public void Generic<T>(){}
    public void NotGeneric(){}
    public void NoParameter(){}
    public void OneParameter(int parameter){}
    public void RefParameter(ref int parameter) {}
    public void TwoParameters(int p1, int p2){}
    public void TwoRefParameters(ref int p1, ref int p2, int p3) {}
    public void ThreeRefParameters(ref int p1, ref int p2, ref int p3) {}   
    public void Add(int x) {}
    public void Add(ref string x) {}
    public IEnumerator GetEnumerator() { return null; }
    public static void StaticMethod() {}

    // Both of these are valid candidates for 'best'.  We should
    // report an ambiguity error.

    public void Ambiguous(string x, object y) {}
    public void Ambiguous(object x, string y) {}

//    // All of these are applicable but each is worse than another,
//    // thanks to transitivity. Again, an ambiguity error.
//    public void W1(T1 t1){}
//    public void W1(T2 t2){}
//    public void W1(T3 t3){}
//
//    // Here the first three are all worse than something
//    // and the last two are both valid candidates for 'best'.
//    // We should report the ambiguity on the last two, not on
//    // the first three.
//    public void W2(T1 t1){}
//    public void W2(T2 t2){}
//    public void W2(T3 t3){}
//    public void W2(T4 t4){}
//    public void W2(T5 t5){}

}
class Q {}
static class Extensions1
{
    public static P Select(this P p, Func<P, P> projection)
    {
        return null;
    }
}
static class Extensions2
{
    public static P Select(this P p, Func<P, P> projection)
    {
        return null;
    }
    public static object Select(this Q q, object projection)
    {
        return null;
    }
    public static void PExt(this P p) {}
}
class C 
{ 
    void InstanceMethod(){}
    // How many different ways can method member lookup / overload resolution fail?
    static void M() 
    {
        P p = new P();
        Q q = new Q();

        p.Ambiguous(null, null); 
        // error CS0121: The call is ambiguous between the following
        // methods or properties: 'P.Ambiguous(object, string)' and
        // 'P.Ambiguous(string, object)'

//         p.W1(null);
//        // error CS0121: The call is ambiguous between the following
//        // methods or properties: 'P.W1(T1)' and 'P.W1(T2)'
//         p.W2(null);
//        // error CS0121: The call is ambiguous between the following
//        // methods or properties: 'P.W2(T4)' and 'P.W2(T5)'
//        var r1 = from x in p select x;
//        // error CS1940: Multiple implementations of the query pattern were found for source
//        // type 'P'.  Ambiguous call to 'Select'.
//        var r2 = p.Select(x=>x);
//        // error CS0121: The call is ambiguous between the following methods or properties:
//        // 'Extensions1.Select(P, System.Func<P,P>)' and 'Extensions2.Select(P, System.Func<P,P>)'

        p.Blah();
        // error CS1061: 'P' does not contain a definition for 'Blah' and no extension method
        // 'Blah' accepting a first argument of type 'P' could be found (are you missing a using
        // directive or an assembly reference?)

        p.Generic();
        // error CS0411: The type arguments for method 'P.Generic<T>()' cannot be inferred
        // from the usage. Try specifying the type arguments explicitly.

        p.Generic<int, int>();
        // error CS0305: Using the generic method 'P.Generic<T>()' requires 1 type arguments

        p.NotGeneric<int, int>();
        // error CS0308: The non-generic method 'P.NotGeneric()' cannot be used with type arguments

//        Func<int, int> f = null;
//        f(frob : null);
//        f.Invoke(frob : null);
//        // error CS1746: The delegate 'Func' does not have a parameter named 'frob'

//        // bad argument arity sometimes takes precedence over missing name, sometimes does not!
        p.NoParameter(frob: null);
        // error CS1501: No overload for method 'NoParameter' takes 1 arguments
//        p.OneParameter(frob : null);
//        // error CS1739: The best overload for 'OneParameter' does not have a parameter named
//        // 'frob'
//        p.TwoParameters(frob : null);
//        // error CS1739: The best overload for 'TwoParameters' does not have a parameter
//        // named 'frob'
//        p.TwoParameters(p1 : 1, 0);
//        // error CS1738: Named argument specifications must appear after all fixed arguments
//        // have been specified
//        dynamic d = null;
//        q.Select(d);
//        // error CS1973: 'Q' has no applicable method named 'Select' but
//        // appears to have an extension method by that name. Extension methods
//        // cannot be dynamically dispatched. Consider casting the dynamic arguments
//        // or calling the extension method without the extension method syntax.
//        var r3 = from x in d select x;
//        // error CS1979: Query expressions over source type 'dynamic' or
//        // with a join sequence of type 'dynamic' are not allowed
//        Func<int, int> f = p.NotGeneric;
//        // error CS0123: No overload for 'NotGeneric' matches delegate 'System.Func<int,int>'
//        f(123, 456);
//        // error CS1593: Delegate 'Func' does not take 2 arguments
//        new P(123);
//        // error CS1729: 'P' does not contain a constructor that takes 1 arguments
        p.NoParameter(123, 456);
        // error CS1501: No overload for method 'NoParameter' takes 2 arguments
//        f(123.456);
//        // error CS1594: Delegate 'System.Func<int,int>' has some invalid arguments
//        // error CS1503: Argument 1: cannot convert from 'double' to 'int'
        p.OneParameter(123.456);
        // error CS1502: The best overloaded method match for 'P.OneParameter(int)' has some invalid arguments
        // error CS1503: Argument 1: cannot convert from 'double' to 'int'
//        p.Select(123.456);
//        // error CS1928: 'P' does not contain a definition for 'Select'
//        // and the best extension method overload 'Extensions1.Select(P,
//        // System.Func<P,P>)' has some invalid arguments
//        // error CS1503: Argument 2: cannot convert from 'double' to 'System.Func<P,P>'
//        new P() { 123.456 };
//        // error CS1950: The best overloaded Add method 'P.Add(int)' for
//        // the collection initializer has some invalid arguments
//        // error CS1503: Argument 1: cannot convert from 'double' to 'int'

        p.TwoRefParameters(1234, 4567, 1234.4567);
        // error CS1502: The best overloaded method match for 'P.TwoRefParameters(ref int, ref int, int)' has some invalid arguments
        // error CS1620: Argument 1 must be passed with the 'ref' keyword
        // error CS1620: Argument 2 must be passed with the 'ref' keyword
        // error CS1503: Argument 3: cannot convert from 'double' to 'int'

        p.ThreeRefParameters(null, p.ToString, 12345.67);
        // error CS1502: The best overloaded method match for 'P.ThreeRefParameters(ref int, ref int, ref int)' has some invalid arguments
        // error CS1503: Argument 1: cannot convert from '<null>' to 'ref int'
        // error CS1503: Argument 2: cannot convert from 'method group' to 'ref int'
        // error CS1620: Argument 3 must be passed with the 'ref' keyword

        p.RefParameter(456);
        // error CS1502: The best overloaded method match for 'P.RefParameter(ref int)' has some invalid arguments
        // error CS1620: Argument 1 must be passed with the 'ref' keyword

        int local = 456;
        p.OneParameter(out local);  
        // error CS1502: The best overloaded method match for 'P.OneParameter(int)' has some invalid arguments
        // error CS1615: Argument 1 should not be passed with the 'out' keyword

        p.RefParameter(out local);  
        // error CS1502: The best overloaded method match for 'P.RefParameter(ref int)' has some invalid arguments
        // error CS1620: Argument 1 must be passed with the 'ref' keyword

//        q.PExt();
//        // error CS1928: 'Q' does not contain a definition for 'PExt' and
//        // the best extension method overload 'Extensions2.PExt(P)' has some
//        // invalid arguments
//        // error CS1929: Instance argument: cannot convert from 'Q' to 'P'
//        new P() { ""hello"" };
//        // error CS1954: The best overloaded method match 'P.Add(ref
//        // string)' for the collection initializer element cannot be used.
//        // Collection initializer 'Add' methods cannot have ref or out parameters.
//        p.GenericConstrained(123);
//        // error CS0452: The type 'int' must be a reference type in order
//        // to use it as parameter 'T' in the generic type or method
//        // 'P.GenericConstrained<T>(T)'
        p.StaticMethod();
        // error CS0176: Member 'P.StaticMethod()' cannot be accessed
        // with an instance reference; qualify it with a type name instead
        P.NoParameter();
        // error CS0120: An object reference is required for the
        // non-static field, method, or property 'P.NoParameter()'

        InstanceMethod(); // Verify that use of 'implicit this' is not legal in a static method.
        // error CS0120: An object reference is required for the
        // non-static field, method, or property 'C.InstanceMethod()'

    }
}",
"'Ambiguous' error CS0121: The call is ambiguous between the following methods or properties: 'P.Ambiguous(string, object)' and 'P.Ambiguous(object, string)'",
"'Blah' error CS1061: 'P' does not contain a definition for 'Blah' and no extension method 'Blah' accepting a first argument of type 'P' could be found (are you missing a using directive or an assembly reference?)",
"'Generic' error CS0411: The type arguments for method 'P.Generic<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.",
"'NotGeneric<int, int>' error CS0308: The non-generic method 'P.NotGeneric()' cannot be used with type arguments",
"'Generic<int, int>' error CS0305: Using the generic method 'P.Generic<T>()' requires 1 type arguments",
"'frob' error CS1739: The best overload for 'NoParameter' does not have a parameter named 'frob'",
"'NoParameter' error CS1501: No overload for method 'NoParameter' takes 2 arguments",
"'p.StaticMethod' error CS0176: Member 'P.StaticMethod()' cannot be accessed with an instance reference; qualify it with a type name instead",
"'P.NoParameter' error CS0120: An object reference is required for the non-static field, method, or property 'P.NoParameter()'",

//"'p.OneParameter' error CS1502: The best overloaded method match for 'P.OneParameter(int)' has some invalid arguments",  //specifically omitted by roslyn
"'123.456' error CS1503: Argument 1: cannot convert from 'double' to 'int'",

//"'p.TwoRefParameters' error CS1502: The best overloaded method match for 'P.TwoRefParameters(ref int, ref int, int)' has some invalid arguments",  //specifically omitted by roslyn
"'1234' error CS1620: Argument 1 must be passed with the 'ref' keyword",
"'4567' error CS1620: Argument 2 must be passed with the 'ref' keyword",
"'1234.4567' error CS1503: Argument 3: cannot convert from 'double' to 'int'",

//"'p.RefParameter' error CS1502: The best overloaded method match for 'P.RefParameter(ref int)' has some invalid arguments",  //specifically omitted by roslyn
"'456' error CS1620: Argument 1 must be passed with the 'ref' keyword",

//"'p.OneParameter' error CS1502: The best overloaded method match for 'P.OneParameter(int)' has some invalid arguments",  //specifically omitted by roslyn
"'local' error CS1615: Argument 1 should not be passed with the 'out' keyword",

//"'p.RefParameter' error CS1502: The best overloaded method match for 'P.RefParameter(ref int)' has some invalid arguments",  //specifically omitted by roslyn
"'local' error CS1620: Argument 1 must be passed with the 'ref' keyword",

//"'p.ThreeRefParameters' error CS1502: The best overloaded method match for 'P.ThreeRefParameters(ref int, ref int, ref int)' has some invalid arguments",  //specifically omitted by roslyn
"'null' error CS1503: Argument 1: cannot convert from '<null>' to 'ref int'",
"'p.ToString' error CS1503: Argument 2: cannot convert from 'method group' to 'ref int'",
"'12345.67' error CS1620: Argument 3 must be passed with the 'ref' keyword",

"'InstanceMethod' error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod()'"

);
        }

        [WorkItem(538651, "DevDiv")]
        [Fact]
        public void TestMemberResolutionWithHiding()
        {
            TestErrors(@"
class A
{
    public static void P() { }
    public static void Q() { }
    public void R() { }
    public void S() { }
    public static int T { get; set; }
    public static int U { get; set; }
    public int V { get; set; }
    public int W { get; set; }
}
class B : A
{
    // Hiding a member (no new keyword)
    // generates a warning only.
    public static int P { get; set; } // CS0108
    public int Q { get; set; } // CS0108
    public static int R { get; set; } // CS0108
    public int S { get; set; } // CS0108
    public static void T() { } // CS0108
    public void U() { } // CS0108
    public static void V() { } // CS0108
    public void W() { } // CS0108
}
class C
{
    static void F(int i) { }
    static void M(B b)
    {
        // Methods.
        B.P();
        B.Q();
        b.R();
        b.S();
        B.T();
        b.U();
        B.V();
        b.W();
        // Property get.
        F(B.P);
        F(b.Q);
        F(B.R);
        F(b.S);
        F(B.T); // CS1503
        F(B.U); // CS1503
        F(b.V); // CS1503
        F(b.W); // CS1503
        // Property set.
        B.P = 0;
        b.Q = 0;
        B.R = 0;
        b.S = 0;
        B.T = 0; // CS1656
        B.U = 0; // CS1656
        b.V = 0; // CS1656
        b.W = 0; // CS1656
    }
}",
                //"'F' error CS1502: The best overloaded method match for 'C.F(int)' has some invalid arguments",  //specifically omitted by roslyn
                "'B.T' error CS1503: Argument 1: cannot convert from 'method group' to 'int'",
                //"'F' error CS1502: The best overloaded method match for 'C.F(int)' has some invalid arguments",  //specifically omitted by roslyn
                "'B.U' error CS1503: Argument 1: cannot convert from 'method group' to 'int'",
                //"'F' error CS1502: The best overloaded method match for 'C.F(int)' has some invalid arguments",  //specifically omitted by roslyn
                "'b.V' error CS1503: Argument 1: cannot convert from 'method group' to 'int'",
                //"'F' error CS1502: The best overloaded method match for 'C.F(int)' has some invalid arguments",  //specifically omitted by roslyn
                "'b.W' error CS1503: Argument 1: cannot convert from 'method group' to 'int'",
                "'B.T' error CS1656: Cannot assign to 'T' because it is a 'method group'",
                "'B.U' error CS1656: Cannot assign to 'U' because it is a 'method group'",
                "'b.V' error CS1656: Cannot assign to 'V' because it is a 'method group'",
                "'b.W' error CS1656: Cannot assign to 'W' because it is a 'method group'");
        }

        [Fact]
        public void TestAssignmentErrors01()
        {
            TestErrors(@"
namespace N { struct Q<T,U> {} }
struct S { public int z; }
class C 
{ 
    static readonly S static_readonly;
    S GetS(N.Q<string, double>?[][,][,,] q, int? x) { }
    readonly int instance_readonly;
    void M() 
    { 
        const int x = 123;
        null = 123;
        M = 123;
        x = 123;
        N = 123;
        C = 123;
        static_readonly.z = 123;
        GetS(null, null).z = 123;
        //object o = null;
        // UNDONE: ((S)o).x = 123;
        // UNDONE: event
        // UNDONE: lambdas and anonymous methods
        // UNDONE: read-only property
        instance_readonly = 123;
        // UNDONE: read-only indexer
        // UNDONE: s.property = 123, s[x] = 123 where s is a struct type and s is not a writable variable 
        // UNDONE: Inaccessible property
        // UNDONE: base.property where property setter is abstract
        // UNDONE: range variables
        // UNDONE: fixed, using and foreach locals
        int m1 = M();
        int m2 = null;
        long l1 = 0;
        int m3 = l1;
        int m4 = M;
        this = null;
    }
}
",
"'null' error CS0131: The left-hand side of an assignment must be a variable, property or indexer",
"'M' error CS1656: Cannot assign to 'M' because it is a 'method group'",
"'x' error CS0131: The left-hand side of an assignment must be a variable, property or indexer",
"'N' error CS0118: 'N' is a namespace but is used like a variable",
"'C' error CS0118: 'C' is a type but is used like a variable",
"'static_readonly.z' error CS1650: Fields of static readonly field 'C.static_readonly' cannot be assigned to (except in a static constructor or a variable initializer)",
"'GetS(null, null)' error CS1612: Cannot modify the return value of 'C.GetS(Q<string, double>?[][*,*][*,*,*], int?)' because it is not a variable",
"'instance_readonly' error CS0191: A readonly field cannot be assigned to (except in a constructor or a variable initializer)",
"'M()' error CS0029: Cannot implicitly convert type 'void' to 'int'",
"'null' error CS0037: Cannot convert null to 'int' because it is a non-nullable value type",
"'l1' error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)",
"'M' error CS0428: Cannot convert method group 'M' to non-delegate type 'int'. Did you intend to invoke the method?",
"'this' error CS1604: Cannot assign to 'this' because it is read-only"
 );
        }

        [Fact]
        public void TestAssignmentErrors02()
        {
            TestErrors(
@"struct S { public int Field; }
class C
{
    S Property { get; set; }
    S Get() { return new S(); }
    void M()
    {
        Property.Field = 0;
        Get().Field = 1;
    }
}
",
                "'Property' error CS1612: Cannot modify the return value of 'C.Property' because it is not a variable",
                "'Get()' error CS1612: Cannot modify the return value of 'C.Get()' because it is not a variable");
        }

        [Fact]
        public void TestRefErrors()
        {
            TestErrors(@"
        class C 
        { 
            struct S { public int z; }
            void N1(ref int x) {}
            void N2(ref int x) {}
            void N3(ref int x) {}
            void N4(ref int x) {}
            void N5(ref int x) {}
            void N6(ref int x) {}

            static readonly int static_readonly;
            static S GetS() { return new S(); }
            void M() 
            { 
                int y = 456;
                const int x = 123;
        
                // Here we have a case that is different than the native compiler. In the native compiler we give the
                // 'ref or out must be variable' error and then go on to do overload resolution. Overload resolution
                // then *fails* because we have not represented the argument as 'ref to constant int'. We've represented
                // it as a constant int, and we then give an incorrect 'bad first argument' and an incorrect 'missing ref'
                // error. Both seem wrong. The first argument is of the right type, it just isn't a variable, and the 
                // ref isn't missing.  The right thing to do is what Roslyn does: state that the expression is
                // bad, but continue with the type analysis regardless.

                N1(ref 123);
                N1(ref x);

                // Notice that the native compiler *does* do precisely that for this case. Here overload resolution
                // succeeds:

                N2(ref y + y);

                N3(ref null);
                N4(ref M);
                N5(ref C);

                N6(ref y); // No error

// UNDONE       N(ref static_readonly);
// UNDONE       N(ref GetS().z);
// UNDONE: event, property, indexer, lambda, anonymous method, using, fixed, foreach, lock, inaccessible field, range variables
            }
        }
        ",
        // (Note that in these three cases overload resolution succeeds. See above.)
        "'123' error CS1510: A ref or out argument must be an assignable variable",
        "'x' error CS1510: A ref or out argument must be an assignable variable",
        "'y + y' error CS1510: A ref or out argument must be an assignable variable",

        "'null' error CS1510: A ref or out argument must be an assignable variable",
        //"'N3' error CS1502: The best overloaded method match for 'C.N3(ref int)' has some invalid arguments",

        "'M' error CS1657: Cannot pass 'M' as a ref or out argument because it is a 'method group'",
        //"'N4' error CS1502: The best overloaded method match for 'C.N4(ref int)' has some invalid arguments",

        "'C' error CS0118: 'C' is a type but is used like a variable"
 // UNDONE: Note that the native compiler gives a slightly different error message; it says
 // UNDONE: cannot convert from 'ref C' to 'ref int'. Do we want to replicate that error message?
 //"'N5' error CS1502: The best overloaded method match for 'C.N5(ref int)' has some invalid arguments",
 );
        }

        [Fact]
        public void TestStaticErrors()
        {
            TestErrors(@"
class C 
{ 
    int instanceField;
    static void M() 
    { 
        int x = instanceField;
        x = this.instanceField;
    }
}
",
"'this' error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer",
"'instanceField' error CS0120: An object reference is required for the non-static field, method, or property 'C.instanceField'"
 );
        }

        [Fact]
        public void TestAssignmentWarnings()
        {
            var source = @"
class C 
{ 
    // UNDONE: Test warning: IDisp loc = whatever; using(loc) { loc = somethingelse; }
    struct S { public int z; }
    static int y;
    S s1;
    S s2;
    void M(int q) 
    { 
        int x = 123;
        x = x;
        y = y;
        s1.z = s1.z;
        s2.z = s2.z;
        s1.z = s2.z;
        q = q;
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         x = x;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "x = x"),
                // (13,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         y = y;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "y = y"),
                // (14,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         s1.z = s1.z;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "s1.z = s1.z"),
                // (15,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         s2.z = s2.z;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "s2.z = s2.z"),
                // (17,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         q = q;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "q = q"));
        }

        [Fact, WorkItem(672714, "DevDiv")]
        public void TestAssignmentWarnings_Dynamic()
        {
            string source = @"
class Program
{
    void foo(dynamic d)
    {
        d = (int)d;
    }
}";
            CreateCompilationWithMscorlib(source, new[] { SystemCoreRef }).VerifyDiagnostics();
        }

        [Fact]
        public void TestDuplicateLocalDeclaration()
        {
            TestErrors(@"
class C 
{ 
    void M() 
    { 
        string q = ""hello"";
        int i;
        int j;
        long k, i;
    }
}
static class D { }
",
"'i' error CS0128: A local variable named 'i' is already defined in this scope");
        }

        [Fact]
        public void TestEnumZeroArg()
        {
            TestErrors(
@"enum E { Zero, One }
enum F { Zero, One }
class C
{
    const int One = 1;
    const object Null = null;
    static void M(E e)
    {
        // Zeros
        M(false); // error
        M((char)(One - 1)); // error
        M((sbyte)(One - 1));
        M((byte)(One - 1));
        M((short)(One - 1));
        M((ushort)(One - 1));
        M((int)(One - 1));
        M((uint)(One - 1));
        M((long)(One - 1));
        M((ulong)(One - 1));
        M((decimal)(One - 1));
        M((float)(One - 1));
        M((double)(One - 1));
        M((E)(One - 1));
        M((F)(One - 1)); // error
        M(Null); // error
        M(null); // error
        M(0);
        M(E.Zero);
        M(F.Zero); // error
        // Ones
        M(true); // error
        M((char)One); // error
        M((sbyte)One); // error
        M((byte)One); // error
        M((short)One); // error
        M((ushort)One); // error
        M((int)One); // error
        M((uint)One); // error
        M((long)One); // error
        M((ulong)One); // error
        M((decimal)One); // error
        M((float)One); // error
        M((double)One); // error
        M((E)One);
        M((F)One); // error
        M(new object()); // error
        M(1); // error
        M(E.One);
        M(F.One); // error
    }
}
",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'false' error CS1503: Argument 1: cannot convert from 'bool' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(char)(One - 1)' error CS1503: Argument 1: cannot convert from 'char' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(F)(One - 1)' error CS1503: Argument 1: cannot convert from 'F' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'Null' error CS1503: Argument 1: cannot convert from 'object' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'null' error CS1503: Argument 1: cannot convert from '<null>' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'F.Zero' error CS1503: Argument 1: cannot convert from 'F' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'true' error CS1503: Argument 1: cannot convert from 'bool' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(char)One' error CS1503: Argument 1: cannot convert from 'char' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(sbyte)One' error CS1503: Argument 1: cannot convert from 'sbyte' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(byte)One' error CS1503: Argument 1: cannot convert from 'byte' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(short)One' error CS1503: Argument 1: cannot convert from 'short' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(ushort)One' error CS1503: Argument 1: cannot convert from 'ushort' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(int)One' error CS1503: Argument 1: cannot convert from 'int' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(uint)One' error CS1503: Argument 1: cannot convert from 'uint' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(long)One' error CS1503: Argument 1: cannot convert from 'long' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(ulong)One' error CS1503: Argument 1: cannot convert from 'ulong' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(decimal)One' error CS1503: Argument 1: cannot convert from 'decimal' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(float)One' error CS1503: Argument 1: cannot convert from 'float' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(double)One' error CS1503: Argument 1: cannot convert from 'double' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'(F)One' error CS1503: Argument 1: cannot convert from 'F' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'new object()' error CS1503: Argument 1: cannot convert from 'object' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'1' error CS1503: Argument 1: cannot convert from 'int' to 'E'",
                 //"'M' error CS1502: The best overloaded method match for 'C.M(E)' has some invalid arguments",  //specifically omitted by roslyn
                 "'F.One' error CS1503: Argument 1: cannot convert from 'F' to 'E'");
        }

        [Fact]
        public void TestEnumZeroAssign()
        {
            TestErrors(
@"enum E { Zero, One }
enum F { Zero, One }
class C
{
    const int One = 1;
    const object Null = null;
    static void M()
    {
        E e;
        // Zeros
        e = false; // error
        e = (char)(One - 1); // error
        e = (sbyte)(One - 1);
        e = (byte)(One - 1);
        e = (short)(One - 1);
        e = (ushort)(One - 1);
        e = (int)(One - 1);
        e = (uint)(One - 1);
        e = (long)(One - 1);
        e = (ulong)(One - 1);
        e = (decimal)(One - 1);
        e = (float)(One - 1);
        e = (double)(One - 1);
        e = (E)(One - 1);
        e = (F)(One - 1); // error
        e = Null; // error
        e = null; // error
        e = 0;
        e = E.Zero;
        e = F.Zero; // error
        // Ones
        e = true; // error
        e = (char)One; // error
        e = (sbyte)One; // error
        e = (byte)One; // error
        e = (short)One; // error
        e = (ushort)One; // error
        e = (int)One; // error
        e = (uint)One; // error
        e = (long)One; // error
        e = (ulong)One; // error
        e = (decimal)One; // error
        e = (float)One; // error
        e = (double)One; // error
        e = (E)One;
        e = (F)One; // error
        e = new object(); // error
        e = 1; // error
        e = E.One;
        e = F.One; // error
    }
}
",
                 "'false' error CS0029: Cannot implicitly convert type 'bool' to 'E'",
                 "'(char)(One - 1)' error CS0266: Cannot implicitly convert type 'char' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(F)(One - 1)' error CS0266: Cannot implicitly convert type 'F' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'Null' error CS0266: Cannot implicitly convert type 'object' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'null' error CS0037: Cannot convert null to 'E' because it is a non-nullable value type",
                 "'F.Zero' error CS0266: Cannot implicitly convert type 'F' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'true' error CS0029: Cannot implicitly convert type 'bool' to 'E'",
                 "'(char)One' error CS0266: Cannot implicitly convert type 'char' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(sbyte)One' error CS0266: Cannot implicitly convert type 'sbyte' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(byte)One' error CS0266: Cannot implicitly convert type 'byte' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(short)One' error CS0266: Cannot implicitly convert type 'short' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(ushort)One' error CS0266: Cannot implicitly convert type 'ushort' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(int)One' error CS0266: Cannot implicitly convert type 'int' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(uint)One' error CS0266: Cannot implicitly convert type 'uint' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(long)One' error CS0266: Cannot implicitly convert type 'long' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(ulong)One' error CS0266: Cannot implicitly convert type 'ulong' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(decimal)One' error CS0266: Cannot implicitly convert type 'decimal' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(float)One' error CS0266: Cannot implicitly convert type 'float' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(double)One' error CS0266: Cannot implicitly convert type 'double' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'(F)One' error CS0266: Cannot implicitly convert type 'F' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'new object()' error CS0266: Cannot implicitly convert type 'object' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'1' error CS0266: Cannot implicitly convert type 'int' to 'E'. An explicit conversion exists (are you missing a cast?)",
                 "'F.One' error CS0266: Cannot implicitly convert type 'F' to 'E'. An explicit conversion exists (are you missing a cast?)");
        }

        [Fact]
        public void TestControlFlowErrors()
        {
            TestErrors(@"
class C 
{ 
    bool N() { return false; }
    void M() 
    { 
        if (N())
            break;
    }
}
",
"'break;' error CS0139: No enclosing loop out of which to break or continue");
        }

        [Fact]
        public void TestControlFlowWarnings()
        {
            var source = @"
class C 
{ 
    void M() 
    { 
        bool b;
        if (b = false)
            System.Console.WriteLine();
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,13): warning CS0665: Assignment in conditional expression is always constant; did you mean to use == instead of = ?
                //         if (b = false)
                Diagnostic(ErrorCode.WRN_IncorrectBooleanAssg, "b = false"),
                // (6,14): warning CS0219: The variable 'b' is assigned but its value is never used
                //         bool b;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "b").WithArguments("b"));
        }

        [Fact]
        public void TestArrayCreationErrors()
        {
            TestErrors(@"
class C
{
    int P { set { } }
    void M()
    {
        int x = 3;
        int[] intArray1 = new int[2] { 1, 2, 3 };               // count mismatch
        int[,] intArray2 = new int[,] { 4, 5, 6 };              // missing initializer
        int[,] intArray3 = new int[,] { { 7, 8 }, { 9 } };          // inconsistent size
        // UNDONE: Negative constants are not represented correctly yet;
        // UNDONE: This is treated as unary minus applied to positive two!
        //      int[] intArray4 = new int[-2];                          // negative size
        int[] intArray5 = new int[x] { 10, 11, 12 };            // non-constant size with initializer
        int[] intArray6 = new int[2] { { 13, 14 }, { 15, 16 } };    // unexpected initializer
        // TODO int[] intArray7 = new int[] { null };                   // bad conversion
        int[] intArray8 = new int[System];                      // expected value, not namespace
        int[] intArray9 = new int[] { System.Int64 };           // expected value, not type
        int intArray10 = { 17, 18, 19 };                        // int is not an array type 
        // TODO int[] intArray11 = new int[1.23];                       // size must be integral
        int[] intArray11 = new int[] { P }; // write-only property
    }
}
",
"'{ 1, 2, 3 }' error CS0847: An array initializer of length '2' is expected",
"'4' error CS0846: A nested array initializer is expected",
"'5' error CS0846: A nested array initializer is expected",
"'6' error CS0846: A nested array initializer is expected",
"'{ 9 }' error CS0847: An array initializer of length '2' is expected",
"'x' error CS0150: A constant value is expected",
"'{ 13, 14 }' error CS0623: Array initializers can only be used in a variable or field initializer. Try using a new expression instead.",
"'{ 15, 16 }' error CS0623: Array initializers can only be used in a variable or field initializer. Try using a new expression instead.",
"'System' error CS0118: 'System' is a namespace but is used like a variable",
"'System.Int64' error CS0119: 'long' is a type, which is not valid in the given context",
"'{ 17, 18, 19 }' error CS0622: Can only use array initializer expressions to assign to array types. Try using a new expression instead.",
"'P' error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor");
        }

        [Fact]
        public void TestObjectCreationOfImportedTypeWithNoArguments()
        {
            var block = ParseAndBindMethodBody(@"
using System.Collections;

class C
{
   void M() 
   {
      ArrayList t = new ArrayList();
   }   
}
");
            Assert.NotNull(block);
            Assert.Equal(1, block.Statements.Length);
            Assert.NotNull(block.Statements[0]);
            Assert.Equal(BoundKind.LocalDeclaration, block.Statements[0].Kind);
            var decl = (BoundLocalDeclaration)block.Statements[0];
            Assert.NotNull(decl.InitializerOpt);
            Assert.Equal(BoundKind.ObjectCreationExpression, decl.InitializerOpt.Kind);
            var call = (BoundObjectCreationExpression)decl.InitializerOpt;
            Assert.Equal(".ctor", call.Constructor.Name);
            Assert.Equal(0, call.Constructor.Parameters.Length);
            Assert.Equal(0, call.Arguments.Length);
            Assert.Equal("ArrayList", call.Constructor.ContainingSymbol.Name);
        }

        [Fact]
        public void TestObjectCreationOfImportedTypeWithSingleArgument()
        {
            var block = ParseAndBindMethodBody(@"
using System.Collections;

class C
{
   void M() 
   {
      ArrayList t = new ArrayList(2);
   }   
}
");
            Assert.NotNull(block);
            Assert.Equal(1, block.Statements.Length);
            Assert.NotNull(block.Statements[0]);
            Assert.Equal(BoundKind.LocalDeclaration, block.Statements[0].Kind);
            var decl = (BoundLocalDeclaration)block.Statements[0];
            Assert.NotNull(decl.InitializerOpt);
            Assert.Equal(BoundKind.ObjectCreationExpression, decl.InitializerOpt.Kind);
            var call = (BoundObjectCreationExpression)decl.InitializerOpt;
            Assert.Equal(".ctor", call.Constructor.Name);
            Assert.Equal("ArrayList", call.Constructor.ContainingSymbol.Name);
            Assert.Equal(1, call.Constructor.Parameters.Length);
            Assert.Equal(1, call.Arguments.Length);
        }

        [Fact]
        public void TestObjectCreationOfImportedTypeWithSingleNamedArgument()
        {
            var block = ParseAndBindMethodBody(@"
using System.Collections;

class C
{
   void M() 
   {
      ArrayList t = new ArrayList(capacity: 2);
   }   
}
");
            Assert.NotNull(block);
            Assert.Equal(1, block.Statements.Length);
            Assert.NotNull(block.Statements[0]);
            Assert.Equal(BoundKind.LocalDeclaration, block.Statements[0].Kind);
            var decl = (BoundLocalDeclaration)block.Statements[0];
            Assert.NotNull(decl.InitializerOpt);
            Assert.Equal(BoundKind.ObjectCreationExpression, decl.InitializerOpt.Kind);
            var call = (BoundObjectCreationExpression)decl.InitializerOpt;
            Assert.Equal(".ctor", call.Constructor.Name);
            Assert.Equal("ArrayList", call.Constructor.ContainingSymbol.Name);
            Assert.Equal(1, call.Constructor.Parameters.Length);
            Assert.Equal("capacity", call.Constructor.Parameters[0].Name);
            Assert.Equal(1, call.Arguments.Length);
            Assert.Equal(1, call.ArgumentNamesOpt.Length);
            Assert.Equal("capacity", call.ArgumentNamesOpt[0]);
        }

        [Fact]
        public void TestObjectCreationOfDeclaredTypeWithNoArguments()
        {
            var block = ParseAndBindMethodBody(@"
class T
{
  public T() { }
  public T(int a) { }
  public T(int a, string b) {}
  public T(string a) { }
}

class C
{
   void M() 
   {
      T t = new T();
   }   
}
");
            Assert.NotNull(block);
            Assert.Equal(1, block.Statements.Length);
            Assert.NotNull(block.Statements[0]);
            Assert.Equal(BoundKind.LocalDeclaration, block.Statements[0].Kind);
            var decl = (BoundLocalDeclaration)block.Statements[0];
            Assert.NotNull(decl.InitializerOpt);
            Assert.Equal(BoundKind.ObjectCreationExpression, decl.InitializerOpt.Kind);
            var call = (BoundObjectCreationExpression)decl.InitializerOpt;
            Assert.Equal(".ctor", call.Constructor.Name);
            Assert.Equal(0, call.Constructor.Parameters.Length);
            Assert.Equal(0, call.Arguments.Length);
            Assert.Equal("T", call.Constructor.ContainingSymbol.Name);
        }


        [Fact]
        public void TestObjectCreationOfDeclaredTypeWithSingleIntArgument()
        {
            var block = ParseAndBindMethodBody(@"
class T
{
  public T() { }
  public T(int a) { }
  public T(int a, string b) {}
  public T(string a) { }
}

class C
{
   void M() 
   {
      T t = new T(1);
   }   
}
");
            Assert.NotNull(block);
            Assert.Equal(1, block.Statements.Length);
            Assert.NotNull(block.Statements[0]);
            Assert.Equal(BoundKind.LocalDeclaration, block.Statements[0].Kind);
            var decl = (BoundLocalDeclaration)block.Statements[0];
            Assert.NotNull(decl.InitializerOpt);
            Assert.Equal(BoundKind.ObjectCreationExpression, decl.InitializerOpt.Kind);
            var call = (BoundObjectCreationExpression)decl.InitializerOpt;
            Assert.Equal(".ctor", call.Constructor.Name);
            Assert.Equal(1, call.Arguments.Length);
            Assert.Equal(1, call.Constructor.Parameters.Length);
            Assert.Equal("a", call.Constructor.Parameters[0].Name);
            Assert.Equal("Int32", call.Constructor.Parameters[0].Type.Name);
        }

        [Fact]
        public void TestObjectCreationOfDeclaredTypeWithSingleStringArgument()
        {
            var block = ParseAndBindMethodBody(@"
class T
{
  public T() { }
  public T(int a) { }
  public T(int a, string b) {}
  public T(string a) { }
}

class C
{
   void M() 
   {
      T t = new T(""x"");
   }   
}
");
            Assert.NotNull(block);
            Assert.Equal(1, block.Statements.Length);
            Assert.NotNull(block.Statements[0]);
            Assert.Equal(BoundKind.LocalDeclaration, block.Statements[0].Kind);
            var decl = (BoundLocalDeclaration)block.Statements[0];
            Assert.NotNull(decl.InitializerOpt);
            Assert.Equal(BoundKind.ObjectCreationExpression, decl.InitializerOpt.Kind);
            var call = (BoundObjectCreationExpression)decl.InitializerOpt;
            Assert.Equal(".ctor", call.Constructor.Name);
            Assert.Equal(1, call.Arguments.Length);
            Assert.Equal(1, call.Constructor.Parameters.Length);
            Assert.Equal("a", call.Constructor.Parameters[0].Name);
            Assert.Equal("String", call.Constructor.Parameters[0].Type.Name);
        }

        [Fact]
        public void TestObjectCreationOfDeclaredTypeWithSingleNamedIntArgument()
        {
            var block = ParseAndBindMethodBody(@"
class T
{
  public T() { }
  public T(int a) { }
  public T(int a, string b) {}
  public T(string a) { }
}

class C
{
   void M() 
   {
      T t = new T(a: 1);
   }   
}
");
            Assert.NotNull(block);
            Assert.Equal(1, block.Statements.Length);
            Assert.NotNull(block.Statements[0]);
            Assert.Equal(BoundKind.LocalDeclaration, block.Statements[0].Kind);
            var decl = (BoundLocalDeclaration)block.Statements[0];
            Assert.NotNull(decl.InitializerOpt);
            Assert.Equal(BoundKind.ObjectCreationExpression, decl.InitializerOpt.Kind);
            var newExpr = (BoundObjectCreationExpression)decl.InitializerOpt;
            Assert.Equal(".ctor", newExpr.Constructor.Name);
            Assert.Equal(1, newExpr.Arguments.Length);
            Assert.Equal(1, newExpr.Constructor.Parameters.Length);
            Assert.Equal("a", newExpr.Constructor.Parameters[0].Name);
            Assert.Equal("Int32", newExpr.Constructor.Parameters[0].Type.Name);
        }
    }
}
