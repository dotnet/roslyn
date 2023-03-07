// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class SemanticAnalyzerTests : CompilingTestBase
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

            CreateCompilationWithMscorlib40AndSystemCore(source)
                .VerifyDiagnostics(
// (9,32): error CS0428: Cannot convert method group 'Main' to non-delegate type 'System.Linq.Expressions.Expression<System.Action>'. Did you intend to invoke the method?
//         Expression<Action> e = Main;
Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "System.Linq.Expressions.Expression<System.Action>")
                );
        }

        [Fact, WorkItem(546737, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546737")]
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
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(546716, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546716")]
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
            CreateCompilation(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (8,40): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'Program.C' is null
                //         var o1 = (D)(delegate{ var s = default(C).ToString();});
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(C).ToString").WithArguments("Program.C"),
                // (9,42): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'Program.C' is null
                //         var o2 = new D(delegate{ var s = default(C).ToString();});
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(C).ToString").WithArguments("Program.C"));
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestParenthesisErrors()
        {
            string source = @"
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
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,18): error CS0118: 'N' is a namespace but is used like a variable
                //         int y = (N).D.x;
                Diagnostic(ErrorCode.ERR_BadSKknown, "N").WithArguments("N", "namespace", "variable").WithLocation(8, 18),
                // (9,18): error CS0119: 'D' is a type, which is not valid in the given context
                //         int z = (N.D).x;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "N.D").WithArguments("N.D", "type").WithLocation(9, 18),
                // (5,16): warning CS0169: The field 'C.x' is never used
                //     static int x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("C.x").WithLocation(5, 16),
                // (2,43): warning CS0649: Field 'D.x' is never assigned to, and will always have its default value 0
                // namespace N { class D { public static int x; } } 
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("N.D.x", "0").WithLocation(2, 43));
        }

        [Fact]
        public void TestConstantFoldingErrors()
        {
            // UNDONE: integer overflows in checked contexts are not detected yet.
            string source = @"
class C 
{ 
    void M() 
    { 
        int a = 10 / 0;
        double y = 10.0 % (-0.0); // not an error, makes an infinity
        decimal z = 10m % 0m;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,17): error CS0020: Division by constant zero
                //         int a = 10 / 0;
                Diagnostic(ErrorCode.ERR_IntDivByZero, "10 / 0").WithLocation(6, 17),
                // (8,21): error CS0020: Division by constant zero
                //         decimal z = 10m % 0m;
                Diagnostic(ErrorCode.ERR_IntDivByZero, "10m % 0m").WithLocation(8, 21),
                // (7,16): warning CS0219: The variable 'y' is assigned but its value is never used
                //         double y = 10.0 % (-0.0); // not an error, makes an infinity
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(7, 16));
        }

        [Fact]
        public void TestStatementBindingErrors()
        {
            string source = @"
class C 
{ 
    void M() 
    { 
        x;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,9): error CS0103: The name 'x' does not exist in the current context
                //         x;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 9),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         x;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "x").WithLocation(6, 9));
        }

        [Fact]
        public void TestLocalDeclarationErrors()
        {
            // In the native compiler we report both that Dx[] is an illegal array and that
            // it is an illegal type for a local or type argument. This seems unnecessary 
            // and redundant. In Roslyn I've eliminated the second error.

            string source = @"
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
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS0723: Cannot declare a variable of static type 'D1'
                //         D1 x1; 
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "D1").WithArguments("D1").WithLocation(7, 9),
                // (8,9): error CS0719: 'D2': array elements cannot be of static type
                //         D2[] x2;
                Diagnostic(ErrorCode.ERR_ArrayOfStaticClass, "D2").WithArguments("D2").WithLocation(8, 9),
                // (9,11): error CS0718: 'D3': static types cannot be used as type arguments
                //         G<D3> x3;
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "D3").WithArguments("D3").WithLocation(9, 11),
                // (10,11): error CS0719: 'D4': array elements cannot be of static type
                //         G<D4[]> x4;
                Diagnostic(ErrorCode.ERR_ArrayOfStaticClass, "D4").WithArguments("D4").WithLocation(10, 11),
                // (7,12): warning CS0168: The variable 'x1' is declared but never used
                //         D1 x1; 
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x1").WithArguments("x1").WithLocation(7, 12),
                // (8,14): warning CS0168: The variable 'x2' is declared but never used
                //         D2[] x2;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x2").WithArguments("x2").WithLocation(8, 14),
                // (9,15): warning CS0168: The variable 'x3' is declared but never used
                //         G<D3> x3;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 15),
                // (10,17): warning CS0168: The variable 'x4' is declared but never used
                //         G<D4[]> x4;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x4").WithArguments("x4").WithLocation(10, 17));
        }

        [Fact]
        public void TestOverloadResolutionErrors()
        {
            string source = @"
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
//        // error CS0250: Do not directly call your base type Finalize method. It is called
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
}";
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                // (69,28): error CS1110: Cannot define a new extension method because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference to System.Core.dll?
                //     public static P Select(this P p, Func<P, P> projection)
                Diagnostic(ErrorCode.ERR_ExtensionAttrNotFound, "this").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute").WithLocation(69, 28),
                // (84,29): error CS1110: Cannot define a new extension method because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference to System.Core.dll?
                //     public static void PExt(this P p) {}
                Diagnostic(ErrorCode.ERR_ExtensionAttrNotFound, "this").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute").WithLocation(84, 29),
                // (80,33): error CS1110: Cannot define a new extension method because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference to System.Core.dll?
                //     public static object Select(this Q q, object projection)
                Diagnostic(ErrorCode.ERR_ExtensionAttrNotFound, "this").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute").WithLocation(80, 33),
                // (76,28): error CS1110: Cannot define a new extension method because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference to System.Core.dll?
                //     public static P Select(this P p, Func<P, P> projection)
                Diagnostic(ErrorCode.ERR_ExtensionAttrNotFound, "this").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute").WithLocation(76, 28),
                // (95,11): error CS0121: The call is ambiguous between the following methods or properties: 'P.Ambiguous(string, object)' and 'P.Ambiguous(object, string)'
                //         p.Ambiguous(null, null); 
                Diagnostic(ErrorCode.ERR_AmbigCall, "Ambiguous").WithArguments("P.Ambiguous(string, object)", "P.Ambiguous(object, string)").WithLocation(95, 11),
                // (113,11): error CS1061: 'P' does not contain a definition for 'Blah' and no extension method 'Blah' accepting a first argument of type 'P' could be found (are you missing a using directive or an assembly reference?)
                //         p.Blah();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Blah").WithArguments("P", "Blah").WithLocation(113, 11),
                // (118,11): error CS0411: The type arguments for method 'P.Generic<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         p.Generic();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Generic").WithArguments("P.Generic<T>()").WithLocation(118, 11),
                // (122,11): error CS0305: Using the generic method 'P.Generic<T>()' requires 1 type arguments
                //         p.Generic<int, int>();
                Diagnostic(ErrorCode.ERR_BadArity, "Generic<int, int>").WithArguments("P.Generic<T>()", "method", "1").WithLocation(122, 11),
                // (125,11): error CS0308: The non-generic method 'P.NotGeneric()' cannot be used with type arguments
                //         p.NotGeneric<int, int>();
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "NotGeneric<int, int>").WithArguments("P.NotGeneric()", "method").WithLocation(125, 11),
                // (134,23): error CS1739: The best overload for 'NoParameter' does not have a parameter named 'frob'
                //         p.NoParameter(frob: null);
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "frob").WithArguments("NoParameter", "frob").WithLocation(134, 23),
                // (160,11): error CS1501: No overload for method 'NoParameter' takes 2 arguments
                //         p.NoParameter(123, 456);
                Diagnostic(ErrorCode.ERR_BadArgCount, "NoParameter").WithArguments("NoParameter", "2").WithLocation(160, 11),
                // (165,24): error CS1503: Argument 1: cannot convert from 'double' to 'int'
                //         p.OneParameter(123.456);
                Diagnostic(ErrorCode.ERR_BadArgType, "123.456").WithArguments("1", "double", "int").WithLocation(165, 24),
                // (178,28): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         p.TwoRefParameters(1234, 4567, 1234.4567);
                Diagnostic(ErrorCode.ERR_BadArgRef, "1234").WithArguments("1", "ref").WithLocation(178, 28),
                // (178,34): error CS1620: Argument 2 must be passed with the 'ref' keyword
                //         p.TwoRefParameters(1234, 4567, 1234.4567);
                Diagnostic(ErrorCode.ERR_BadArgRef, "4567").WithArguments("2", "ref").WithLocation(178, 34),
                // (178,40): error CS1503: Argument 3: cannot convert from 'double' to 'int'
                //         p.TwoRefParameters(1234, 4567, 1234.4567);
                Diagnostic(ErrorCode.ERR_BadArgType, "1234.4567").WithArguments("3", "double", "int").WithLocation(178, 40),
                // (184,30): error CS1503: Argument 1: cannot convert from '<null>' to 'ref int'
                //         p.ThreeRefParameters(null, p.ToString, 12345.67);
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "ref int").WithLocation(184, 30),
                // (184,36): error CS1503: Argument 2: cannot convert from 'method group' to 'ref int'
                //         p.ThreeRefParameters(null, p.ToString, 12345.67);
                Diagnostic(ErrorCode.ERR_BadArgType, "p.ToString").WithArguments("2", "method group", "ref int").WithLocation(184, 36),
                // (184,48): error CS1620: Argument 3 must be passed with the 'ref' keyword
                //         p.ThreeRefParameters(null, p.ToString, 12345.67);
                Diagnostic(ErrorCode.ERR_BadArgRef, "12345.67").WithArguments("3", "ref").WithLocation(184, 48),
                // (190,24): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         p.RefParameter(456);
                Diagnostic(ErrorCode.ERR_BadArgRef, "456").WithArguments("1", "ref").WithLocation(190, 24),
                // (195,28): error CS1615: Argument 1 may not be passed with the 'out' keyword
                //         p.OneParameter(out local);  
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "local").WithArguments("1", "out").WithLocation(195, 28),
                // (199,28): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         p.RefParameter(out local);  
                Diagnostic(ErrorCode.ERR_BadArgRef, "local").WithArguments("1", "ref").WithLocation(199, 28),
                // (216,9): error CS0176: Member 'P.StaticMethod()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         p.StaticMethod();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "p.StaticMethod").WithArguments("P.StaticMethod()").WithLocation(216, 9),
                // (219,9): error CS0120: An object reference is required for the non-static field, method, or property 'P.NoParameter()'
                //         P.NoParameter();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P.NoParameter").WithArguments("P.NoParameter()").WithLocation(219, 9),
                // (223,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod()'
                //         InstanceMethod(); // Verify that use of 'implicit this' is not legal in a static method.
                Diagnostic(ErrorCode.ERR_ObjectRequired, "InstanceMethod").WithArguments("C.InstanceMethod()").WithLocation(223, 9)
                );
        }

        [WorkItem(538651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538651")]
        [Fact]
        public void TestMemberResolutionWithHiding()
        {
            string source = @"
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
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (18,16): warning CS0108: 'B.Q' hides inherited member 'A.Q()'. Use the new keyword if hiding was intended.
                //     public int Q { get; set; } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "Q").WithArguments("B.Q", "A.Q()").WithLocation(18, 16),
                // (19,23): warning CS0108: 'B.R' hides inherited member 'A.R()'. Use the new keyword if hiding was intended.
                //     public static int R { get; set; } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "R").WithArguments("B.R", "A.R()").WithLocation(19, 23),
                // (20,16): warning CS0108: 'B.S' hides inherited member 'A.S()'. Use the new keyword if hiding was intended.
                //     public int S { get; set; } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "S").WithArguments("B.S", "A.S()").WithLocation(20, 16),
                // (21,24): warning CS0108: 'B.T()' hides inherited member 'A.T'. Use the new keyword if hiding was intended.
                //     public static void T() { } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("B.T()", "A.T").WithLocation(21, 24),
                // (22,17): warning CS0108: 'B.U()' hides inherited member 'A.U'. Use the new keyword if hiding was intended.
                //     public void U() { } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "U").WithArguments("B.U()", "A.U").WithLocation(22, 17),
                // (23,24): warning CS0108: 'B.V()' hides inherited member 'A.V'. Use the new keyword if hiding was intended.
                //     public static void V() { } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "V").WithArguments("B.V()", "A.V").WithLocation(23, 24),
                // (24,17): warning CS0108: 'B.W()' hides inherited member 'A.W'. Use the new keyword if hiding was intended.
                //     public void W() { } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "W").WithArguments("B.W()", "A.W").WithLocation(24, 17),
                // (17,23): warning CS0108: 'B.P' hides inherited member 'A.P()'. Use the new keyword if hiding was intended.
                //     public static int P { get; set; } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "P").WithArguments("B.P", "A.P()").WithLocation(17, 23),
                // (45,11): error CS1503: Argument 1: cannot convert from 'method group' to 'int'
                //         F(B.T); // CS1503
                Diagnostic(ErrorCode.ERR_BadArgType, "B.T").WithArguments("1", "method group", "int").WithLocation(45, 11),
                // (46,11): error CS1503: Argument 1: cannot convert from 'method group' to 'int'
                //         F(B.U); // CS1503
                Diagnostic(ErrorCode.ERR_BadArgType, "B.U").WithArguments("1", "method group", "int").WithLocation(46, 11),
                // (47,11): error CS1503: Argument 1: cannot convert from 'method group' to 'int'
                //         F(b.V); // CS1503
                Diagnostic(ErrorCode.ERR_BadArgType, "b.V").WithArguments("1", "method group", "int").WithLocation(47, 11),
                // (48,11): error CS1503: Argument 1: cannot convert from 'method group' to 'int'
                //         F(b.W); // CS1503
                Diagnostic(ErrorCode.ERR_BadArgType, "b.W").WithArguments("1", "method group", "int").WithLocation(48, 11),
                // (54,9): error CS1656: Cannot assign to 'T' because it is a 'method group'
                //         B.T = 0; // CS1656
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "B.T").WithArguments("T", "method group").WithLocation(54, 9),
                // (55,9): error CS1656: Cannot assign to 'U' because it is a 'method group'
                //         B.U = 0; // CS1656
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "B.U").WithArguments("U", "method group").WithLocation(55, 9),
                // (56,9): error CS1656: Cannot assign to 'V' because it is a 'method group'
                //         b.V = 0; // CS1656
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "b.V").WithArguments("V", "method group").WithLocation(56, 9),
                // (57,9): error CS1656: Cannot assign to 'W' because it is a 'method group'
                //         b.W = 0; // CS1656
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "b.W").WithArguments("W", "method group").WithLocation(57, 9));
        }

        [Fact]
        public void TestAssignmentErrors01()
        {
            string source = @"
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
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,7): error CS0161: 'C.GetS(Q<string, double>?[][*,*][*,*,*], int?)': not all code paths return a value
                //     S GetS(N.Q<string, double>?[][,][,,] q, int? x) { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "GetS").WithArguments("C.GetS(N.Q<string, double>?[][*,*][*,*,*], int?)").WithLocation(7, 7),
                // (12,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         null = 123;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "null").WithLocation(12, 9),
                // (13,9): error CS1656: Cannot assign to 'M' because it is a 'method group'
                //         M = 123;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "M").WithArguments("M", "method group").WithLocation(13, 9),
                // (14,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         x = 123;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "x").WithLocation(14, 9),
                // (15,9): error CS0118: 'N' is a namespace but is used like a variable
                //         N = 123;
                Diagnostic(ErrorCode.ERR_BadSKknown, "N").WithArguments("N", "namespace", "variable").WithLocation(15, 9),
                // (16,9): error CS0118: 'C' is a type but is used like a variable
                //         C = 123;
                Diagnostic(ErrorCode.ERR_BadSKknown, "C").WithArguments("C", "type", "variable").WithLocation(16, 9),
                // (17,9): error CS1650: Fields of static readonly field 'C.static_readonly' cannot be assigned to (except in a static constructor or a variable initializer)
                //         static_readonly.z = 123;
                Diagnostic(ErrorCode.ERR_AssgReadonlyStatic2, "static_readonly.z").WithArguments("C.static_readonly").WithLocation(17, 9),
                // (18,9): error CS1612: Cannot modify the return value of 'C.GetS(Q<string, double>?[][*,*][*,*,*], int?)' because it is not a variable
                //         GetS(null, null).z = 123;
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "GetS(null, null)").WithArguments("C.GetS(N.Q<string, double>?[][*,*][*,*,*], int?)").WithLocation(18, 9),
                // (24,9): error CS0191: A readonly field cannot be assigned to (except in the constructor of the class in which the field is defined or a variable initializer))
                //         instance_readonly = 123;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "instance_readonly").WithLocation(24, 9),
                // (31,18): error CS0029: Cannot implicitly convert type 'void' to 'int'
                //         int m1 = M();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "M()").WithArguments("void", "int").WithLocation(31, 18),
                // (32,18): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         int m2 = null;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(32, 18),
                // (34,18): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         int m3 = l1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "l1").WithArguments("long", "int").WithLocation(34, 18),
                // (35,18): error CS0428: Cannot convert method group 'M' to non-delegate type 'int'. Did you intend to invoke the method?
                //         int m4 = M;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "int").WithLocation(35, 18),
                // (36,9): error CS1604: Cannot assign to 'this' because it is read-only
                //         this = null;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(36, 9)
                );
        }

        [Fact]
        public void TestAssignmentErrors02()
        {
            string source =
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
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,9): error CS1612: Cannot modify the return value of 'C.Property' because it is not a variable
                //         Property.Field = 0;
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "Property").WithArguments("C.Property").WithLocation(8, 9),
                // (9,9): error CS1612: Cannot modify the return value of 'C.Get()' because it is not a variable
                //         Get().Field = 1;
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "Get()").WithArguments("C.Get()").WithLocation(9, 9));
        }

        [Fact]
        public void TestRefErrors()
        {
            string source = @"
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
        ";
            CreateCompilation(source).VerifyDiagnostics(
                // (27,24): error CS1510: A ref or out argument must be an assignable variable
                //                 N1(ref 123);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "123").WithLocation(27, 24),
                // (28,24): error CS1510: A ref or out argument must be an assignable variable
                //                 N1(ref x);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x").WithLocation(28, 24),
                // (33,24): error CS1510: A ref or out argument must be an assignable variable
                //                 N2(ref y + y);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "y + y").WithLocation(33, 24),
                // (35,24): error CS1510: A ref or out argument must be an assignable variable
                //                 N3(ref null);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "null").WithLocation(35, 24),
                // (36,24): error CS1657: Cannot pass 'M' as a ref or out argument because it is a 'method group'
                //                 N4(ref M);
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "M").WithArguments("M", "method group").WithLocation(36, 24),
                // (37,24): error CS0118: 'C' is a type but is used like a variable
                //                 N5(ref C);
                Diagnostic(ErrorCode.ERR_BadSKknown, "C").WithArguments("C", "type", "variable").WithLocation(37, 24),
                // (12,33): warning CS0169: The field 'C.static_readonly' is never used
                //             static readonly int static_readonly;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "static_readonly").WithArguments("C.static_readonly").WithLocation(12, 33),
                // (4,35): warning CS0649: Field 'C.S.z' is never assigned to, and will always have its default value 0
                //             struct S { public int z; }
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "z").WithArguments("C.S.z", "0").WithLocation(4, 35));
        }

        [Fact]
        public void TestStaticErrors()
        {
            string source = @"
class C 
{ 
    int instanceField;
    static void M() 
    { 
        int x = instanceField;
        x = this.instanceField;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,17): error CS0120: An object reference is required for the non-static field, method, or property 'C.instanceField'
                //         int x = instanceField;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "instanceField").WithArguments("C.instanceField").WithLocation(7, 17),
                // (8,13): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                //         x = this.instanceField;
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this").WithLocation(8, 13),
                // (4,9): warning CS0649: Field 'C.instanceField' is never assigned to, and will always have its default value 0
                //     int instanceField;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "instanceField").WithArguments("C.instanceField", "0").WithLocation(4, 9));
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
            CreateCompilation(source).VerifyDiagnostics(
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

        [Fact, WorkItem(672714, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/672714")]
        public void TestAssignmentWarnings_Dynamic()
        {
            string source = @"
class Program
{
    void goo(dynamic d)
    {
        d = (int)d;
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestDuplicateLocalDeclaration()
        {
            string source = @"
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
";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,17): error CS0128: A local variable named 'i' is already defined in this scope
                //         long k, i;
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "i").WithArguments("i").WithLocation(9, 17),
                // (6,16): warning CS0219: The variable 'q' is assigned but its value is never used
                //         string q = "hello";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "q").WithArguments("q").WithLocation(6, 16),
                // (7,13): warning CS0168: The variable 'i' is declared but never used
                //         int i;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i").WithArguments("i").WithLocation(7, 13),
                // (8,13): warning CS0168: The variable 'j' is declared but never used
                //         int j;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "j").WithArguments("j").WithLocation(8, 13),
                // (9,14): warning CS0168: The variable 'k' is declared but never used
                //         long k, i;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "k").WithArguments("k").WithLocation(9, 14),
                // (9,17): warning CS0168: The variable 'i' is declared but never used
                //         long k, i;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i").WithArguments("i").WithLocation(9, 17));
        }

        [Fact]
        public void TestEnumZeroArg()
        {
            string source =
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
";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,11): error CS1503: Argument 1: cannot convert from 'bool' to 'E'
                //         M(false); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "false").WithArguments("1", "bool", "E").WithLocation(10, 11),
                // (11,11): error CS1503: Argument 1: cannot convert from 'char' to 'E'
                //         M((char)(One - 1)); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(char)(One - 1)").WithArguments("1", "char", "E").WithLocation(11, 11),
                // (24,11): error CS1503: Argument 1: cannot convert from 'F' to 'E'
                //         M((F)(One - 1)); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(F)(One - 1)").WithArguments("1", "F", "E").WithLocation(24, 11),
                // (25,11): error CS1503: Argument 1: cannot convert from 'object' to 'E'
                //         M(Null); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "Null").WithArguments("1", "object", "E").WithLocation(25, 11),
                // (26,11): error CS1503: Argument 1: cannot convert from '<null>' to 'E'
                //         M(null); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "E").WithLocation(26, 11),
                // (29,11): error CS1503: Argument 1: cannot convert from 'F' to 'E'
                //         M(F.Zero); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "F.Zero").WithArguments("1", "F", "E").WithLocation(29, 11),
                // (31,11): error CS1503: Argument 1: cannot convert from 'bool' to 'E'
                //         M(true); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "true").WithArguments("1", "bool", "E").WithLocation(31, 11),
                // (32,11): error CS1503: Argument 1: cannot convert from 'char' to 'E'
                //         M((char)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(char)One").WithArguments("1", "char", "E").WithLocation(32, 11),
                // (33,11): error CS1503: Argument 1: cannot convert from 'sbyte' to 'E'
                //         M((sbyte)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(sbyte)One").WithArguments("1", "sbyte", "E").WithLocation(33, 11),
                // (34,11): error CS1503: Argument 1: cannot convert from 'byte' to 'E'
                //         M((byte)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(byte)One").WithArguments("1", "byte", "E").WithLocation(34, 11),
                // (35,11): error CS1503: Argument 1: cannot convert from 'short' to 'E'
                //         M((short)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(short)One").WithArguments("1", "short", "E").WithLocation(35, 11),
                // (36,11): error CS1503: Argument 1: cannot convert from 'ushort' to 'E'
                //         M((ushort)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(ushort)One").WithArguments("1", "ushort", "E").WithLocation(36, 11),
                // (37,11): error CS1503: Argument 1: cannot convert from 'int' to 'E'
                //         M((int)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(int)One").WithArguments("1", "int", "E").WithLocation(37, 11),
                // (38,11): error CS1503: Argument 1: cannot convert from 'uint' to 'E'
                //         M((uint)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(uint)One").WithArguments("1", "uint", "E").WithLocation(38, 11),
                // (39,11): error CS1503: Argument 1: cannot convert from 'long' to 'E'
                //         M((long)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(long)One").WithArguments("1", "long", "E").WithLocation(39, 11),
                // (40,11): error CS1503: Argument 1: cannot convert from 'ulong' to 'E'
                //         M((ulong)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(ulong)One").WithArguments("1", "ulong", "E").WithLocation(40, 11),
                // (41,11): error CS1503: Argument 1: cannot convert from 'decimal' to 'E'
                //         M((decimal)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(decimal)One").WithArguments("1", "decimal", "E").WithLocation(41, 11),
                // (42,11): error CS1503: Argument 1: cannot convert from 'float' to 'E'
                //         M((float)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(float)One").WithArguments("1", "float", "E").WithLocation(42, 11),
                // (43,11): error CS1503: Argument 1: cannot convert from 'double' to 'E'
                //         M((double)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(double)One").WithArguments("1", "double", "E").WithLocation(43, 11),
                // (45,11): error CS1503: Argument 1: cannot convert from 'F' to 'E'
                //         M((F)One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "(F)One").WithArguments("1", "F", "E").WithLocation(45, 11),
                // (46,11): error CS1503: Argument 1: cannot convert from 'object' to 'E'
                //         M(new object()); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "new object()").WithArguments("1", "object", "E").WithLocation(46, 11),
                // (47,11): error CS1503: Argument 1: cannot convert from 'int' to 'E'
                //         M(1); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "E").WithLocation(47, 11),
                // (49,11): error CS1503: Argument 1: cannot convert from 'F' to 'E'
                //         M(F.One); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "F.One").WithArguments("1", "F", "E").WithLocation(49, 11));
        }

        [Fact]
        public void TestEnumZeroAssign()
        {
            string source =
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
";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,13): error CS0029: Cannot implicitly convert type 'bool' to 'E'
                //         e = false; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "false").WithArguments("bool", "E").WithLocation(11, 13),
                // (12,13): error CS0266: Cannot implicitly convert type 'char' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (char)(One - 1); // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(char)(One - 1)").WithArguments("char", "E").WithLocation(12, 13),
                // (25,13): error CS0266: Cannot implicitly convert type 'F' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (F)(One - 1); // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(F)(One - 1)").WithArguments("F", "E").WithLocation(25, 13),
                // (26,13): error CS0266: Cannot implicitly convert type 'object' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = Null; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "Null").WithArguments("object", "E").WithLocation(26, 13),
                // (27,13): error CS0037: Cannot convert null to 'E' because it is a non-nullable value type
                //         e = null; // error
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("E").WithLocation(27, 13),
                // (30,13): error CS0266: Cannot implicitly convert type 'F' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = F.Zero; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "F.Zero").WithArguments("F", "E").WithLocation(30, 13),
                // (32,13): error CS0029: Cannot implicitly convert type 'bool' to 'E'
                //         e = true; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "true").WithArguments("bool", "E").WithLocation(32, 13),
                // (33,13): error CS0266: Cannot implicitly convert type 'char' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (char)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(char)One").WithArguments("char", "E").WithLocation(33, 13),
                // (34,13): error CS0266: Cannot implicitly convert type 'sbyte' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (sbyte)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(sbyte)One").WithArguments("sbyte", "E").WithLocation(34, 13),
                // (35,13): error CS0266: Cannot implicitly convert type 'byte' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (byte)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(byte)One").WithArguments("byte", "E").WithLocation(35, 13),
                // (36,13): error CS0266: Cannot implicitly convert type 'short' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (short)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(short)One").WithArguments("short", "E").WithLocation(36, 13),
                // (37,13): error CS0266: Cannot implicitly convert type 'ushort' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (ushort)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(ushort)One").WithArguments("ushort", "E").WithLocation(37, 13),
                // (38,13): error CS0266: Cannot implicitly convert type 'int' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (int)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(int)One").WithArguments("int", "E").WithLocation(38, 13),
                // (39,13): error CS0266: Cannot implicitly convert type 'uint' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (uint)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(uint)One").WithArguments("uint", "E").WithLocation(39, 13),
                // (40,13): error CS0266: Cannot implicitly convert type 'long' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (long)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(long)One").WithArguments("long", "E").WithLocation(40, 13),
                // (41,13): error CS0266: Cannot implicitly convert type 'ulong' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (ulong)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(ulong)One").WithArguments("ulong", "E").WithLocation(41, 13),
                // (42,13): error CS0266: Cannot implicitly convert type 'decimal' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (decimal)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(decimal)One").WithArguments("decimal", "E").WithLocation(42, 13),
                // (43,13): error CS0266: Cannot implicitly convert type 'float' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (float)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(float)One").WithArguments("float", "E").WithLocation(43, 13),
                // (44,13): error CS0266: Cannot implicitly convert type 'double' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (double)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(double)One").WithArguments("double", "E").WithLocation(44, 13),
                // (46,13): error CS0266: Cannot implicitly convert type 'F' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = (F)One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(F)One").WithArguments("F", "E").WithLocation(46, 13),
                // (47,13): error CS0266: Cannot implicitly convert type 'object' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = new object(); // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new object()").WithArguments("object", "E").WithLocation(47, 13),
                // (48,13): error CS0266: Cannot implicitly convert type 'int' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = 1; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1").WithArguments("int", "E").WithLocation(48, 13),
                // (50,13): error CS0266: Cannot implicitly convert type 'F' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         e = F.One; // error
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "F.One").WithArguments("F", "E").WithLocation(50, 13));
        }

        [Fact]
        public void TestControlFlowErrors()
        {
            string source = @"
class C 
{ 
    bool N() { return false; }
    void M() 
    { 
        if (N())
            break;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,13): error CS0139: No enclosing loop out of which to break or continue
                //             break;
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "break;").WithLocation(8, 13));
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
            CreateCompilation(source).VerifyDiagnostics(
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
            string source = @"
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
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,38): error CS0847: An array initializer of length '2' is expected
                //         int[] intArray1 = new int[2] { 1, 2, 3 };               // count mismatch
                Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "{ 1, 2, 3 }").WithArguments("2").WithLocation(8, 38),
                // (9,41): error CS0846: A nested array initializer is expected
                //         int[,] intArray2 = new int[,] { 4, 5, 6 };              // missing initializer
                Diagnostic(ErrorCode.ERR_ArrayInitializerExpected, "4").WithLocation(9, 41),
                // (9,44): error CS0846: A nested array initializer is expected
                //         int[,] intArray2 = new int[,] { 4, 5, 6 };              // missing initializer
                Diagnostic(ErrorCode.ERR_ArrayInitializerExpected, "5").WithLocation(9, 44),
                // (9,47): error CS0846: A nested array initializer is expected
                //         int[,] intArray2 = new int[,] { 4, 5, 6 };              // missing initializer
                Diagnostic(ErrorCode.ERR_ArrayInitializerExpected, "6").WithLocation(9, 47),
                // (10,51): error CS0847: An array initializer of length '2' is expected
                //         int[,] intArray3 = new int[,] { { 7, 8 }, { 9 } };          // inconsistent size
                Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "{ 9 }").WithArguments("2").WithLocation(10, 51),
                // (14,35): error CS0150: A constant value is expected
                //         int[] intArray5 = new int[x] { 10, 11, 12 };            // non-constant size with initializer
                Diagnostic(ErrorCode.ERR_ConstantExpected, "x").WithLocation(14, 35),
                // (15,40): error CS0623: Array initializers can only be used in a variable or field initializer. Try using a new expression instead.
                //         int[] intArray6 = new int[2] { { 13, 14 }, { 15, 16 } };    // unexpected initializer
                Diagnostic(ErrorCode.ERR_ArrayInitInBadPlace, "{ 13, 14 }").WithLocation(15, 40),
                // (15,52): error CS0623: Array initializers can only be used in a variable or field initializer. Try using a new expression instead.
                //         int[] intArray6 = new int[2] { { 13, 14 }, { 15, 16 } };    // unexpected initializer
                Diagnostic(ErrorCode.ERR_ArrayInitInBadPlace, "{ 15, 16 }").WithLocation(15, 52),
                // (17,35): error CS0118: 'System' is a namespace but is used like a variable
                //         int[] intArray8 = new int[System];                      // expected value, not namespace
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "variable").WithLocation(17, 35),
                // (18,39): error CS0119: 'long' is a type, which is not valid in the given context
                //         int[] intArray9 = new int[] { System.Int64 };           // expected value, not type
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Int64").WithArguments("long", "type").WithLocation(18, 39),
                // (19,26): error CS0622: Can only use array initializer expressions to assign to array types. Try using a new expression instead.
                //         int intArray10 = { 17, 18, 19 };                        // int is not an array type 
                Diagnostic(ErrorCode.ERR_ArrayInitToNonArrayType, "{ 17, 18, 19 }").WithLocation(19, 26),
                // (21,40): error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor
                //         int[] intArray11 = new int[] { P }; // write-only property
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("C.P").WithLocation(21, 40));
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
            Assert.Equal("Int32", call.Constructor.Parameters[0].TypeWithAnnotations.Type.Name);
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
            Assert.Equal("String", call.Constructor.Parameters[0].TypeWithAnnotations.Type.Name);
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
            Assert.Equal("Int32", newExpr.Constructor.Parameters[0].TypeWithAnnotations.Type.Name);
        }
    }
}
