﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// this place is dedicated to binding related error tests
    /// </summary>
    public class SemanticErrorTests : CompilingTestBase
    {
        #region "Targeted Error Tests - please arrange tests in the order of error code"

        [Fact]
        public void CS0019ERR_BadBinaryOps01()
        {
            var text = @"
namespace x
{
    public class b
    {
        public static void Main()
        {
            bool q = false;
            if (q == 1)
            { }
        }
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadBinaryOps, Line = 9, Column = 17 });
        }

        [Fact]
        public void CS0019ERR_BadBinaryOps02()
        {
            var text =
@"using System;
enum E { A, B, C }
enum F { X = (E.A + E.B) * DayOfWeek.Monday } // no error
class C
{
    static void M(object o)
    {
        M((E.A + E.B) * DayOfWeek.Monday);
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadBinaryOps, Line = 8, Column = 12 });
        }

        [WorkItem(539906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539906")]
        [Fact]
        public void CS0019ERR_BadBinaryOps03()
        {
            var text =
@"delegate void MyDelegate1(ref int x, out float y);
class Program
{
    public void DelegatedMethod1(ref int x, out float y)
    {
        y = 1;
    }
    public void DelegatedMethod2(out int x, ref float y)
    {
        x = 1;
    }
    public void DelegatedMethod3(out int x, float y = 1)
    {
        x = 1;
    }
    static void Main(string[] args)
    {
        Program mc = new Program();
        MyDelegate1 md1 = null;
        md1 += mc.DelegatedMethod1;
        md1 += mc.DelegatedMethod2; // Invalid
        md1 += mc.DelegatedMethod3; // Invalid
        md1 -= mc.DelegatedMethod1;
        md1 -= mc.DelegatedMethod2; // Invalid
        md1 -= mc.DelegatedMethod3; // Invalid
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                // (21,19): error CS0123: No overload for 'DelegatedMethod2' matches delegate 'MyDelegate1'
                //         md1 += mc.DelegatedMethod2; // Invalid
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "DelegatedMethod2").WithArguments("DelegatedMethod2", "MyDelegate1"),
                // (22,19): error CS0123: No overload for 'DelegatedMethod3' matches delegate 'MyDelegate1'
                //         md1 += mc.DelegatedMethod3; // Invalid
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "DelegatedMethod3").WithArguments("DelegatedMethod3", "MyDelegate1"),
                // (24,19): error CS0123: No overload for 'DelegatedMethod2' matches delegate 'MyDelegate1'
                //         md1 -= mc.DelegatedMethod2; // Invalid
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "DelegatedMethod2").WithArguments("DelegatedMethod2", "MyDelegate1"),
                // (25,19): error CS0123: No overload for 'DelegatedMethod3' matches delegate 'MyDelegate1'
                //         md1 -= mc.DelegatedMethod3; // Invalid
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "DelegatedMethod3").WithArguments("DelegatedMethod3", "MyDelegate1")
                );
        }

        // Method List to removal or concatenation
        [WorkItem(539906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539906")]
        [Fact]
        public void CS0019ERR_BadBinaryOps04()
        {
            var text =
@"using System;
delegate void boo();
public class abc
{
    public void bar() { System.Console.WriteLine(""bar""); }
    static public void far() { System.Console.WriteLine(""far""); }
}
class C
{
    static void Main(string[] args)
    {
        abc p = new abc();
        boo goo = null;
        boo goo1 = new boo(abc.far);
        boo[] arrfoo = { p.bar, abc.far };
        goo += arrfoo; // Invalid
        goo -= arrfoo; // Invalid
        goo += new boo[] { p.bar, abc.far };	// Invalid
        goo -= new boo[] { p.bar, abc.far };	// Invalid
        goo += Delegate.Combine(arrfoo);	// Invalid
        goo += Delegate.Combine(goo, goo1);  	// Invalid
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (16,16): error CS0029: Cannot implicitly convert type 'boo[]' to 'boo'
                //         goo += arrfoo; // Invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "arrfoo").WithArguments("boo[]", "boo"),
                // (17,16): error CS0029: Cannot implicitly convert type 'boo[]' to 'boo'
                //         goo -= arrfoo; // Invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "arrfoo").WithArguments("boo[]", "boo"),
                // (18,16): error CS0029: Cannot implicitly convert type 'boo[]' to 'boo'
                //         goo += new boo[] { p.bar, abc.far };	// Invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new boo[] { p.bar, abc.far }").WithArguments("boo[]", "boo"),
                // (19,16): error CS0029: Cannot implicitly convert type 'boo[]' to 'boo'
                //         goo -= new boo[] { p.bar, abc.far };	// Invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new boo[] { p.bar, abc.far }").WithArguments("boo[]", "boo"),
                // (20,16): error CS0266: Cannot implicitly convert type 'System.Delegate' to 'boo'. An explicit conversion exists (are you missing a cast?)
                //         goo += Delegate.Combine(arrfoo);	// Invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "Delegate.Combine(arrfoo)").WithArguments("System.Delegate", "boo"),
                // (21,16): error CS0266: Cannot implicitly convert type 'System.Delegate' to 'boo'. An explicit conversion exists (are you missing a cast?)
                //         goo += Delegate.Combine(goo, goo1);  	// Invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "Delegate.Combine(goo, goo1)").WithArguments("System.Delegate", "boo")
                );
        }

        [WorkItem(539906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539906")]
        [Fact]
        public void CS0019ERR_BadBinaryOps05()
        {
            var text =
@"public delegate double MyDelegate1(ref int integerPortion, out float fraction);
public delegate double MyDelegate2(ref int integerPortion, out float fraction);
class C
{
    static void Main(string[] args)
    {
        C mc = new C();
        MyDelegate1 md1 = null;
        MyDelegate2 md2 = null;
        md1 += md2; // Invalid
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,16): error CS0029: Cannot implicitly convert type 'MyDelegate2' to 'MyDelegate1'
                //         md1 += md2; // Invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "md2").WithArguments("MyDelegate2", "MyDelegate1")
                );
        }

        // Anonymous  method to removal or concatenation
        [WorkItem(539906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539906")]
        [Fact]
        public void CS0019ERR_BadBinaryOps06()
        {
            var text =
@"delegate void boo(int x);
class C
{
    static void Main(string[] args)
    {
        boo goo = null;
        goo += delegate (string x) { System.Console.WriteLine(x); };// Invalid
        goo -= delegate (string x) { System.Console.WriteLine(x); };// Invalid
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,16): error CS1661: Cannot convert anonymous method to delegate type 'boo' because the parameter types do not match the delegate parameter types
                //         goo += delegate (string x) { System.Console.WriteLine(x); };// Invalid
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "delegate (string x) { System.Console.WriteLine(x); }").WithArguments("anonymous method", "boo"),
                // (7,33): error CS1678: Parameter 1 is declared as type 'string' but should be 'int'
                //         goo += delegate (string x) { System.Console.WriteLine(x); };// Invalid
                Diagnostic(ErrorCode.ERR_BadParamType, "x").WithArguments("1", "", "string", "", "int"),
                // (8,16): error CS1661: Cannot convert anonymous method to delegate type 'boo' because the parameter types do not match the delegate parameter types
                //         goo -= delegate (string x) { System.Console.WriteLine(x); };// Invalid
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "delegate (string x) { System.Console.WriteLine(x); }").WithArguments("anonymous method", "boo"),
                // (8,33): error CS1678: Parameter 1 is declared as type 'string' but should be 'int'
                //         goo -= delegate (string x) { System.Console.WriteLine(x); };// Invalid
                Diagnostic(ErrorCode.ERR_BadParamType, "x").WithArguments("1", "", "string", "", "int")
                );
        }

        // Lambda expression to removal or concatenation
        [WorkItem(539906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539906")]
        [Fact]
        public void CS0019ERR_BadBinaryOps07()
        {
            var text =
@"delegate void boo(int x);
class C
{
    static void Main(string[] args)
    {
        boo goo = null;
        goo += (string x) => { };// Invalid
        goo -= (string x) => { };// Invalid
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,16): error CS1661: Cannot convert lambda expression to delegate type 'boo' because the parameter types do not match the delegate parameter types
                //         goo += (string x) => { };// Invalid
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "(string x) => { }").WithArguments("lambda expression", "boo"),
                // (7,24): error CS1678: Parameter 1 is declared as type 'string' but should be 'int'
                //         goo += (string x) => { };// Invalid
                Diagnostic(ErrorCode.ERR_BadParamType, "x").WithArguments("1", "", "string", "", "int"),
                // (8,16): error CS1661: Cannot convert lambda expression to delegate type 'boo' because the parameter types do not match the delegate parameter types
                //         goo -= (string x) => { };// Invalid
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "(string x) => { }").WithArguments("lambda expression", "boo"),
                // (8,24): error CS1678: Parameter 1 is declared as type 'string' but should be 'int'
                //         goo -= (string x) => { };// Invalid
                Diagnostic(ErrorCode.ERR_BadParamType, "x").WithArguments("1", "", "string", "", "int")
                );
        }

        // Successive operator for addition and subtraction assignment
        [WorkItem(539906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539906")]
        [Fact]
        public void CS0019ERR_BadBinaryOps08()
        {
            var text =
@"using System;
delegate void boo(int x);
class C
{
    public void bar(int x) { Console.WriteLine("""", x); }
    static public void far(int x) { Console.WriteLine(""far:{0}"", x); }
    static void Main(string[] args)
    {
        C p = new C();
        boo goo = null;
        goo += p.bar + far;// Invalid
        goo += (x) => { System.Console.WriteLine(""Lambda:{0}"", x); } + far;// Invalid
        goo += delegate (int x) { System.Console.WriteLine(""Anonymous:{0}"", x); } + far;// Invalid
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,16): error CS0019: Operator '+' cannot be applied to operands of type 'method group' and 'method group'
                //         goo += p.bar + far;// Invalid
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p.bar + far").WithArguments("+", "method group", "method group").WithLocation(11, 16),
                // (12,16): error CS0019: Operator '+' cannot be applied to operands of type 'lambda expression' and 'method group'
                //         goo += (x) => { System.Console.WriteLine("Lambda:{0}", x); } + far;// Invalid
                Diagnostic(ErrorCode.ERR_BadBinaryOps, @"(x) => { System.Console.WriteLine(""Lambda:{0}"", x); } + far").WithArguments("+", "lambda expression", "method group").WithLocation(12, 16),
                // (13,16): error CS0019: Operator '+' cannot be applied to operands of type 'anonymous method' and 'method group'
                //         goo += delegate (int x) { System.Console.WriteLine("Anonymous:{0}", x); } + far;// Invalid
                Diagnostic(ErrorCode.ERR_BadBinaryOps, @"delegate (int x) { System.Console.WriteLine(""Anonymous:{0}"", x); } + far").WithArguments("+", "anonymous method", "method group").WithLocation(13, 16)
                );
        }

        // Removal or concatenation for the delegate on Variance
        [WorkItem(539906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539906")]
        [Fact]
        public void CS0019ERR_BadBinaryOps09()
        {
            var text =
@"using System.Collections.Generic;

delegate IList<int> Delegate1(List<int> x);
delegate IEnumerable<int> Delegate2(IList<int> x);
delegate IEnumerable<long> Delegate3(IList<long> x);

class C
{
    public static List<int> Method1(IList<int> x)
    {
        return null;
    }

    public static IList<long> Method1(IList<long> x)
    {
        return null;
    }
    static void Main(string[] args)
    {
        Delegate1 d1 = Method1;
        d1 += Method1;
        Delegate2 d2 = Method1;
        d2 += Method1;
        Delegate3 d3 = Method1;
        d1 += d2; // invalid
        d2 += d1; // invalid
        d2 += d3; // invalid
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (25,15): error CS0029: Cannot implicitly convert type 'Delegate2' to 'Delegate1'
                //         d1 += d2; // invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d2").WithArguments("Delegate2", "Delegate1"),
                // (26,15): error CS0029: Cannot implicitly convert type 'Delegate1' to 'Delegate2'
                //         d2 += d1; // invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d1").WithArguments("Delegate1", "Delegate2"),
                // (27,15): error CS0029: Cannot implicitly convert type 'Delegate3' to 'Delegate2'
                //         d2 += d3; // invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d3").WithArguments("Delegate3", "Delegate2")
                );
        }

        // generic-delegate (goo<t>(...)) += non generic-methodgroup(bar<t>(...))
        [WorkItem(539906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539906")]
        [Fact]
        public void CS0019ERR_BadBinaryOps10()
        {
            var text =
@"delegate void boo<T>(T x);
class C
{
    public void bar(int x) { System.Console.WriteLine(""bar:{0}"", x); }
    public void bar1(string x) { System.Console.WriteLine(""bar1:{0}"", x); }

    static void Main(string[] args)
    {
        C p = new C();
        boo<int> goo = null;
        goo += p.bar;// OK
        goo += p.bar1;// Invalid
        goo += (x) => { System.Console.WriteLine(""Lambda:{0}"", x); };// OK
        goo += (string x) => { System.Console.WriteLine(""Lambda:{0}"", x); };// Invalid
        goo += delegate (int x) { System.Console.WriteLine(""Anonymous:{0}"", x); };// OK
        goo += delegate (string x) { System.Console.WriteLine(""Anonymous:{0}"", x); };// Invalid

        boo<string> goo1 = null;
        goo1 += p.bar;// Invalid
        goo1 += p.bar1;// OK
        goo1 += (x) => { System.Console.WriteLine(""Lambda:{0}"", x); };// OK
        goo1 += (int x) => { System.Console.WriteLine(""Lambda:{0}"", x); };// Invalid
        goo1 += delegate (int x) { System.Console.WriteLine(""Anonymous:{0}"", x); };// Invalid
        goo1 += delegate (string x) { System.Console.WriteLine(""Anonymous:{0}"", x); };// OK
        goo += goo1;// Invalid
        goo1 += goo;// Invalid
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (12,18): error CS0123: No overload for 'bar1' matches delegate 'boo<int>'
                //         goo += p.bar1;// Invalid
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "bar1").WithArguments("bar1", "boo<int>"),
                // (14,16): error CS1661: Cannot convert lambda expression to delegate type 'boo<int>' because the parameter types do not match the delegate parameter types
                //         goo += (string x) => { System.Console.WriteLine("Lambda:{0}", x); };// Invalid
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, @"(string x) => { System.Console.WriteLine(""Lambda:{0}"", x); }").WithArguments("lambda expression", "boo<int>"),
                // (14,24): error CS1678: Parameter 1 is declared as type 'string' but should be 'int'
                //         goo += (string x) => { System.Console.WriteLine("Lambda:{0}", x); };// Invalid
                Diagnostic(ErrorCode.ERR_BadParamType, "x").WithArguments("1", "", "string", "", "int"),
                // (16,16): error CS1661: Cannot convert anonymous method to delegate type 'boo<int>' because the parameter types do not match the delegate parameter types
                //         goo += delegate (string x) { System.Console.WriteLine("Anonymous:{0}", x); };// Invalid
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, @"delegate (string x) { System.Console.WriteLine(""Anonymous:{0}"", x); }").WithArguments("anonymous method", "boo<int>"),
                // (16,33): error CS1678: Parameter 1 is declared as type 'string' but should be 'int'
                //         goo += delegate (string x) { System.Console.WriteLine("Anonymous:{0}", x); };// Invalid
                Diagnostic(ErrorCode.ERR_BadParamType, "x").WithArguments("1", "", "string", "", "int"),
                // (19,19): error CS0123: No overload for 'bar' matches delegate 'boo<string>'
                //         goo1 += p.bar;// Invalid
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "bar").WithArguments("bar", "boo<string>"),
                // (22,17): error CS1661: Cannot convert lambda expression to delegate type 'boo<string>' because the parameter types do not match the delegate parameter types
                //         goo1 += (int x) => { System.Console.WriteLine("Lambda:{0}", x); };// Invalid
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, @"(int x) => { System.Console.WriteLine(""Lambda:{0}"", x); }").WithArguments("lambda expression", "boo<string>"),
                // (22,22): error CS1678: Parameter 1 is declared as type 'int' but should be 'string'
                //         goo1 += (int x) => { System.Console.WriteLine("Lambda:{0}", x); };// Invalid
                Diagnostic(ErrorCode.ERR_BadParamType, "x").WithArguments("1", "", "int", "", "string"),
                // (23,17): error CS1661: Cannot convert anonymous method to delegate type 'boo<string>' because the parameter types do not match the delegate parameter types
                //         goo1 += delegate (int x) { System.Console.WriteLine("Anonymous:{0}", x); };// Invalid
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, @"delegate (int x) { System.Console.WriteLine(""Anonymous:{0}"", x); }").WithArguments("anonymous method", "boo<string>"),
                // (23,31): error CS1678: Parameter 1 is declared as type 'int' but should be 'string'
                //         goo1 += delegate (int x) { System.Console.WriteLine("Anonymous:{0}", x); };// Invalid
                Diagnostic(ErrorCode.ERR_BadParamType, "x").WithArguments("1", "", "int", "", "string"),
                // (25,16): error CS0029: Cannot implicitly convert type 'boo<string>' to 'boo<int>'
                //         goo += goo1;// Invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "goo1").WithArguments("boo<string>", "boo<int>"),
                // (26,17): error CS0029: Cannot implicitly convert type 'boo<int>' to 'boo<string>'
                //         goo1 += goo;// Invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "goo").WithArguments("boo<int>", "boo<string>")
                );
        }

        // generic-delegate (goo<t>(...)) += generic-methodgroup(bar<t>(...))
        [WorkItem(539906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539906")]
        [Fact]
        public void CS0019ERR_BadBinaryOps11()
        {
            var text =
@"delegate void boo<T>(T x);
class C
{
    static void far<T>(T x) { }
    static void Main(string[] args)
    {
        C p = new C();
        boo<int> goo = null;
        goo += far<int>;// OK
        goo += far<short>;// Invalid
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,16): error CS0123: No overload for 'far' matches delegate 'boo<int>'
                //         goo += far<short>;// Invalid
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "far<short>").WithArguments("far", "boo<int>")
                );
        }

        // non generic-delegate (goo<t>(...)) += generic-methodgroup(bar<t>(...))
        [WorkItem(539906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539906")]
        [Fact]
        public void CS0019ERR_BadBinaryOps12()
        {
            var text =
@"delegate void boo<T>(T x);
class C
{
    static void far<T>(T x) { }
    static void Main(string[] args)
    {
        C p = new C();
        boo<int> goo = null;
        goo += far<int>;// OK
        goo += far<short>;// Invalid
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,16): error CS0123: No overload for 'far' matches delegate 'boo<int>'
                //         goo += far<short>;// Invalid
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "far<short>").WithArguments("far", "boo<int>")
                );
        }

        // distinguish '|' from '||'
        [WorkItem(540235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540235")]
        [Fact]
        public void CS0019ERR_BadBinaryOps13()
        {
            var text = @"
class C
{
    int a = 1 | 1;
    int b = 1 & 1;
    int c = 1 || 1;
    int d = 1 && 1;

    bool e = true | true;
    bool f = true & true;
    bool g = true || true;
    bool h = true && true;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,13): error CS0019: Operator '||' cannot be applied to operands of type 'int' and 'int'
                //     int c = 1 || 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "1 || 1").WithArguments("||", "int", "int"),
                // (7,13): error CS0019: Operator '&&' cannot be applied to operands of type 'int' and 'int'
                //     int d = 1 && 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "1 && 1").WithArguments("&&", "int", "int"),
                // (4,9): warning CS0414: The field 'C.a' is assigned but its value is never used
                //     int a = 1 | 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "a").WithArguments("C.a"),
                // (5,9): warning CS0414: The field 'C.b' is assigned but its value is never used
                //     int b = 1 & 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "b").WithArguments("C.b"),
                // (9,10): warning CS0414: The field 'C.e' is assigned but its value is never used
                //     bool e = true | true;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "e").WithArguments("C.e"),
                // (10,10): warning CS0414: The field 'C.f' is assigned but its value is never used
                //     bool f = true & true;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "f").WithArguments("C.f"),
                // (11,10): warning CS0414: The field 'C.g' is assigned but its value is never used
                //     bool g = true || true;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "g").WithArguments("C.g"),
                // (12,10): warning CS0414: The field 'C.h' is assigned but its value is never used
                //     bool h = true && true;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "h").WithArguments("C.h"));
        }

        /// <summary>
        /// Conversion errors for Null Coalescing operator(??)
        /// </summary>
        [Fact]
        public void CS0019ERR_BadBinaryOps14()
        {
            var text = @"
public class D { }
public class Error
{
    public int? NonNullableValueType_a(int a)
    {
        int? b = null;
        int? z = a ?? b;
        return z;
    }
    public int? NonNullableValueType_b(char ch)
    {
        char b = ch;
        int? z = null ?? b;
        return z;
    }
    public int NonNullableValueType_const_a(char ch)
    {
        char b = ch;
        int z = 10 ?? b;
        return z;
    }
    public D NoPossibleConversionError()
    {
        D b = new D();
        Error a = null;
        D z = a ?? b;
        return z;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,18): error CS0019: Operator '??' cannot be applied to operands of type 'int' and 'int?'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "a ?? b").WithArguments("??", "int", "int?"),
                // (13,18): error CS0019: Operator '??' cannot be applied to operands of type '<null>' and 'char'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "null ?? b").WithArguments("??", "<null>", "char"),
                // (19,17): error CS0019: Operator '??' cannot be applied to operands of type 'int' and 'char'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "10 ?? b").WithArguments("??", "int", "char"),
                // (26,15): error CS0019: Operator '??' cannot be applied to operands of type 'Error' and 'D'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "a ?? b").WithArguments("??", "Error", "D"));
        }

        [WorkItem(542115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542115")]
        [Fact]
        public void CS0019ERR_BadBinaryOps15()
        {
            var text =
@"class C
{
    static void M<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4, int i, C c)
        where T2 : class
        where T3 : struct
        where T4 : T1
    {
        bool b;
        b = (t1 == t1);
        b = (t1 == t2);
        b = (t1 == t3);
        b = (t1 == t4);
        b = (t1 == i);
        b = (t1 == c);
        b = (t1 == null);
        b = (t2 == t1);
        b = (t2 == t2);
        b = (t2 == t3);
        b = (t2 == t4);
        b = (t2 == i);
        b = (t2 == c);
        b = (t2 == null);
        b = (t3 == t1);
        b = (t3 == t2);
        b = (t3 == t3);
        b = (t3 == t4);
        b = (t3 == i);
        b = (t3 == c);
        b = (t3 == null);
        b = (t4 != t1);
        b = (t4 != t2);
        b = (t4 != t3);
        b = (t4 != t4);
        b = (t4 != i);
        b = (t4 != c);
        b = (t4 != null);
        b = (i != t1);
        b = (i != t2);
        b = (i != t3);
        b = (i != t4);
        b = (i != i);
        b = (i != c);
        b = (i != null);
        b = (c != t1);
        b = (c != t2);
        b = (c != t3);
        b = (c != t4);
        b = (c != i);
        b = (c != c);
        b = (c != null);
        b = (null != t1);
        b = (null != t2);
        b = (null != t3);
        b = (null != t4);
        b = (null != i);
        b = (null != c);
        b = (null != null);
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,14): error CS0019: Operator '==' cannot be applied to operands of type 'T1' and 'T1'
                //         b = (t1 == t1);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t1 == t1").WithArguments("==", "T1", "T1"),
                // (10,14): error CS0019: Operator '==' cannot be applied to operands of type 'T1' and 'T2'
                //         b = (t1 == t2);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t1 == t2").WithArguments("==", "T1", "T2"),
                // (11,14): error CS0019: Operator '==' cannot be applied to operands of type 'T1' and 'T3'
                //         b = (t1 == t3);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t1 == t3").WithArguments("==", "T1", "T3"),
                // (12,14): error CS0019: Operator '==' cannot be applied to operands of type 'T1' and 'T4'
                //         b = (t1 == t4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t1 == t4").WithArguments("==", "T1", "T4"),
                // (13,14): error CS0019: Operator '==' cannot be applied to operands of type 'T1' and 'int'
                //         b = (t1 == i);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t1 == i").WithArguments("==", "T1", "int"),
                // (14,14): error CS0019: Operator '==' cannot be applied to operands of type 'T1' and 'C'
                //         b = (t1 == c);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t1 == c").WithArguments("==", "T1", "C"),
                // (16,14): error CS0019: Operator '==' cannot be applied to operands of type 'T2' and 'T1'
                //         b = (t2 == t1);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t2 == t1").WithArguments("==", "T2", "T1"),
                // (18,14): error CS0019: Operator '==' cannot be applied to operands of type 'T2' and 'T3'
                //         b = (t2 == t3);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t2 == t3").WithArguments("==", "T2", "T3"),
                // (19,14): error CS0019: Operator '==' cannot be applied to operands of type 'T2' and 'T4'
                //         b = (t2 == t4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t2 == t4").WithArguments("==", "T2", "T4"),
                // (20,14): error CS0019: Operator '==' cannot be applied to operands of type 'T2' and 'int'
                //         b = (t2 == i);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t2 == i").WithArguments("==", "T2", "int"),
                // (23,14): error CS0019: Operator '==' cannot be applied to operands of type 'T3' and 'T1'
                //         b = (t3 == t1);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t3 == t1").WithArguments("==", "T3", "T1"),
                // (24,14): error CS0019: Operator '==' cannot be applied to operands of type 'T3' and 'T2'
                //         b = (t3 == t2);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t3 == t2").WithArguments("==", "T3", "T2"),
                // (25,14): error CS0019: Operator '==' cannot be applied to operands of type 'T3' and 'T3'
                //         b = (t3 == t3);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t3 == t3").WithArguments("==", "T3", "T3"),
                // (26,14): error CS0019: Operator '==' cannot be applied to operands of type 'T3' and 'T4'
                //         b = (t3 == t4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t3 == t4").WithArguments("==", "T3", "T4"),
                // (27,14): error CS0019: Operator '==' cannot be applied to operands of type 'T3' and 'int'
                //         b = (t3 == i);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t3 == i").WithArguments("==", "T3", "int"),
                // (28,14): error CS0019: Operator '==' cannot be applied to operands of type 'T3' and 'C'
                //         b = (t3 == c);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t3 == c").WithArguments("==", "T3", "C"),
                // (29,14): error CS0019: Operator '==' cannot be applied to operands of type 'T3' and '<null>'
                //         b = (t3 == null);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t3 == null").WithArguments("==", "T3", "<null>"),
                // (30,14): error CS0019: Operator '!=' cannot be applied to operands of type 'T4' and 'T1'
                //         b = (t4 != t1);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t4 != t1").WithArguments("!=", "T4", "T1"),
                // (31,14): error CS0019: Operator '!=' cannot be applied to operands of type 'T4' and 'T2'
                //         b = (t4 != t2);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t4 != t2").WithArguments("!=", "T4", "T2"),
                // (32,14): error CS0019: Operator '!=' cannot be applied to operands of type 'T4' and 'T3'
                //         b = (t4 != t3);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t4 != t3").WithArguments("!=", "T4", "T3"),
                // (33,14): error CS0019: Operator '!=' cannot be applied to operands of type 'T4' and 'T4'
                //         b = (t4 != t4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t4 != t4").WithArguments("!=", "T4", "T4"),
                // (34,14): error CS0019: Operator '!=' cannot be applied to operands of type 'T4' and 'int'
                //         b = (t4 != i);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t4 != i").WithArguments("!=", "T4", "int"),
                // (35,14): error CS0019: Operator '!=' cannot be applied to operands of type 'T4' and 'C'
                //         b = (t4 != c);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t4 != c").WithArguments("!=", "T4", "C"),
                // (37,14): error CS0019: Operator '!=' cannot be applied to operands of type 'int' and 'T1'
                //         b = (i != t1);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i != t1").WithArguments("!=", "int", "T1"),
                // (38,14): error CS0019: Operator '!=' cannot be applied to operands of type 'int' and 'T2'
                //         b = (i != t2);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i != t2").WithArguments("!=", "int", "T2"),
                // (39,14): error CS0019: Operator '!=' cannot be applied to operands of type 'int' and 'T3'
                //         b = (i != t3);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i != t3").WithArguments("!=", "int", "T3"),
                // (40,14): error CS0019: Operator '!=' cannot be applied to operands of type 'int' and 'T4'
                //         b = (i != t4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i != t4").WithArguments("!=", "int", "T4"),
                // (42,14): error CS0019: Operator '!=' cannot be applied to operands of type 'int' and 'C'
                //         b = (i != c);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i != c").WithArguments("!=", "int", "C"),
                // (44,14): error CS0019: Operator '!=' cannot be applied to operands of type 'C' and 'T1'
                //         b = (c != t1);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c != t1").WithArguments("!=", "C", "T1"),
                // (46,14): error CS0019: Operator '!=' cannot be applied to operands of type 'C' and 'T3'
                //         b = (c != t3);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c != t3").WithArguments("!=", "C", "T3"),
                // (47,14): error CS0019: Operator '!=' cannot be applied to operands of type 'C' and 'T4'
                //         b = (c != t4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c != t4").WithArguments("!=", "C", "T4"),
                // (48,14): error CS0019: Operator '!=' cannot be applied to operands of type 'C' and 'int'
                //         b = (c != i);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c != i").WithArguments("!=", "C", "int"),
                // (53,14): error CS0019: Operator '!=' cannot be applied to operands of type '<null>' and 'T3'
                //         b = (null != t3);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "null != t3").WithArguments("!=", "<null>", "T3"),
                // (17,14): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         b = (t2 == t2);
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "t2 == t2"),
                // (41,14): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         b = (i != i);
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "i != i"),
                // (43,14): warning CS0472: The result of the expression is always 'true' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         b = (i != null);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "i != null").WithArguments("true", "int", "int?"),
                // (49,14): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         b = (c != c);
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "c != c"),
                // (55,14): warning CS0472: The result of the expression is always 'true' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         b = (null != i);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "null != i").WithArguments("true", "int", "int?"));
        }

        [Fact]
        public void CS0019ERR_BadBinaryOps16()
        {
            var text =
@"class A { }
class B : A { }
interface I { }
class C
{
    static void M<T, U>(T t, U u, A a, B b, C c, I i)
        where T : A
        where U : B
    {
        bool x;
        x = (t == t);
        x = (t == u);
        x = (t == a);
        x = (t == b);
        x = (t == c);
        x = (t == i);
        x = (u == t);
        x = (u == u);
        x = (u == a);
        x = (u == b);
        x = (u == c);
        x = (u == i);
        x = (a == t);
        x = (a == u);
        x = (a == a);
        x = (a == b);
        x = (a == c);
        x = (a == i);
        x = (b == t);
        x = (b == u);
        x = (b == a);
        x = (b == b);
        x = (b == c);
        x = (b == i);
        x = (c == t);
        x = (c == u);
        x = (c == a);
        x = (c == b);
        x = (c == c);
        x = (c == i);
        x = (i == t);
        x = (i == u);
        x = (i == a);
        x = (i == b);
        x = (i == c);
        x = (i == i);
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (15,14): error CS0019: Operator '==' cannot be applied to operands of type 'T' and 'C'
                //         x = (t == c);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t == c").WithArguments("==", "T", "C"),
                // (21,14): error CS0019: Operator '==' cannot be applied to operands of type 'U' and 'C'
                //         x = (u == c);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "u == c").WithArguments("==", "U", "C"),
                // (27,14): error CS0019: Operator '==' cannot be applied to operands of type 'A' and 'C'
                //         x = (a == c);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "a == c").WithArguments("==", "A", "C"),
                // (33,14): error CS0019: Operator '==' cannot be applied to operands of type 'B' and 'C'
                //         x = (b == c);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "b == c").WithArguments("==", "B", "C"),
                // (35,14): error CS0019: Operator '==' cannot be applied to operands of type 'C' and 'T'
                //         x = (c == t);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c == t").WithArguments("==", "C", "T"),
                // (36,14): error CS0019: Operator '==' cannot be applied to operands of type 'C' and 'U'
                //         x = (c == u);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c == u").WithArguments("==", "C", "U"),
                // (37,14): error CS0019: Operator '==' cannot be applied to operands of type 'C' and 'A'
                //         x = (c == a);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c == a").WithArguments("==", "C", "A"),
                // (38,14): error CS0019: Operator '==' cannot be applied to operands of type 'C' and 'B'
                //         x = (c == b);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c == b").WithArguments("==", "C", "B"),
                // (11,14): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         x = (t == t);
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "t == t"),
                // (18,14): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         x = (u == u);
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "u == u"),
                // (25,14): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         x = (a == a);
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "a == a"),
                // (32,14): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         x = (b == b);
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "b == b"),
                // (39,14): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         x = (c == c);
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "c == c"),
                // (46,14): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         x = (i == i);
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "i == i"));
        }

        [Fact]
        public void CS0019ERR_BadBinaryOps17()
        {
            var text =
@"struct S { }
abstract class A<T>
{
    internal virtual void M<U>(U u) where U : T
    {
        bool b;
        b = (u == null);
        b = (null != u);
    }
}
class B : A<S>
{
    internal override void M<U>(U u)
    {
        bool b;
        b = (u == null);
        b = (null != u);
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (16,14): error CS0019: Operator '==' cannot be applied to operands of type 'U' and '<null>'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "u == null").WithArguments("==", "U", "<null>").WithLocation(16, 14),
                // (17,14): error CS0019: Operator '!=' cannot be applied to operands of type '<null>' and 'U'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "null != u").WithArguments("!=", "<null>", "U").WithLocation(17, 14));
        }

        [Fact]
        public void CS0020ERR_IntDivByZero()
        {
            var text = @"
namespace x
{
    public class b
    {
        public static int Main()
        {
            int s = 1 / 0;   // CS0020
            return s;
        }
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_IntDivByZero, Line = 8, Column = 21 } });
        }

        [Fact]
        public void CS0020ERR_IntDivByZero_02()
        {
            var text = @"
namespace x
{
    public class b
    {
        public static void Main()
        {
            decimal x1 = 1.20M / 0;                         // CS0020
            decimal x2 = 1.20M / decimal.Zero;              // CS0020
            decimal x3 = decimal.MaxValue / decimal.Zero;   // CS0020
        }
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] {
                    new ErrorDescription { Code = (int)ErrorCode.ERR_IntDivByZero, Line = 8, Column = 26 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_IntDivByZero, Line = 9, Column = 26 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_IntDivByZero, Line = 10, Column = 26 } });
        }

        [Fact]
        public void CS0021ERR_BadIndexLHS()
        {
            var text =
@"enum E { }
class C
{
    static void M<T>()
    {
        object o;
        o = M[0];
        o = ((System.Action)null)[0];
        o = ((dynamic)o)[0];
        o = default(E)[0];
        o = default(T)[0];
        o = (new C())[0];
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,13): error CS0021: Cannot apply indexing with [] to an expression of type 'method group'
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "M[0]").WithArguments("method group").WithLocation(7, 13),
                // (8,13): error CS0021: Cannot apply indexing with [] to an expression of type 'System.Action'
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "((System.Action)null)[0]").WithArguments("System.Action").WithLocation(8, 13),
                // (10,13): error CS0021: Cannot apply indexing with [] to an expression of type 'E'
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "default(E)[0]").WithArguments("E").WithLocation(10, 13),
                // (11,13): error CS0021: Cannot apply indexing with [] to an expression of type 'T'
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "default(T)[0]").WithArguments("T").WithLocation(11, 13),
                // (12,13): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "(new C())[0]").WithArguments("C").WithLocation(12, 13));
        }

        [Fact]
        public void CS0022ERR_BadIndexCount()
        {
            var text = @"
namespace x
{
    public class b
    {
        public static void Main()
        {
            int[,] a = new int[10,2] ;
                        a[2] = 4; //bad index count in access
        }
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadIndexCount, Line = 9, Column = 25 } });
        }

        [WorkItem(542486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542486")]
        [Fact]
        public void CS0022ERR_BadIndexCount02()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        int[,] a = new int[1 2]; //bad index count in size specifier - no initializer
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,30): error CS1003: Syntax error, ',' expected
                Diagnostic(ErrorCode.ERR_SyntaxError, "2").WithArguments(",", ""));
        }

        [WorkItem(542486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542486")]
        [Fact]
        public void CS0022ERR_BadIndexCount03()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        int[,] a = new int[1 2] { 1 }; //bad index count in size specifier - with initializer
    }
}
";
            // NOTE: Dev10 just gives a parse error on '2'
            CreateCompilation(text).VerifyDiagnostics(
                // (6,30): error CS1003: Syntax error, ',' expected
                Diagnostic(ErrorCode.ERR_SyntaxError, "2").WithArguments(",", ""),
                // (6,35): error CS0846: A nested array initializer is expected
                Diagnostic(ErrorCode.ERR_ArrayInitializerExpected, "1"));
        }

        [WorkItem(542486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542486")]
        [Fact]
        public void CS0022ERR_BadIndexCount04()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        int[,] a = new int[1,]; //bad index count in size specifier - no initializer
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,30): error CS0443: Syntax error; value expected
                Diagnostic(ErrorCode.ERR_ValueExpected, ""));
        }

        [WorkItem(542486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542486")]
        [Fact]
        public void CS0022ERR_BadIndexCount05()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        int[,] a = new int[1,] { { 1 } }; //bad index count in size specifier - with initializer
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,30): error CS0443: Syntax error; value expected
                Diagnostic(ErrorCode.ERR_ValueExpected, ""));
        }

        [WorkItem(539590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539590")]
        [Fact]
        public void CS0023ERR_BadUnaryOp1()
        {
            var text = @"
namespace X
{
    class C
    {
        object M()
        {
            object q = new object();
            if (!q) // CS0023
            { }

            object obj = -null; // CS0023
            obj = !null; // CS0023
            obj = ~null; // CS0023

            obj++; // CS0023
            --obj; // CS0023
            return +null; // CS0023
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,17): error CS0023: Operator '!' cannot be applied to operand of type 'object'
                //             if (!q) // CS0023
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "!q").WithArguments("!", "object").WithLocation(9, 17),
                // (12,26): error CS8310: Operator '-' cannot be applied to operand '<null>'
                //             object obj = -null; // CS0023
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "-null").WithArguments("-", "<null>").WithLocation(12, 26),
                // (13,19): error CS8310: Operator '!' cannot be applied to operand '<null>'
                //             obj = !null; // CS0023
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "!null").WithArguments("!", "<null>").WithLocation(13, 19),
                // (14,19): error CS8310: Operator '~' cannot be applied to operand '<null>'
                //             obj = ~null; // CS0023
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "~null").WithArguments("~", "<null>").WithLocation(14, 19),
                // (16,13): error CS0023: Operator '++' cannot be applied to operand of type 'object'
                //             obj++; // CS0023
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "obj++").WithArguments("++", "object").WithLocation(16, 13),
                // (17,13): error CS0023: Operator '--' cannot be applied to operand of type 'object'
                //             --obj; // CS0023
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "--obj").WithArguments("--", "object").WithLocation(17, 13),
                // (18,20): error CS8310: Operator '+' cannot be applied to operand '<null>'
                //             return +null; // CS0023
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "+null").WithArguments("+", "<null>").WithLocation(18, 20)
                );
        }

        [WorkItem(539590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539590")]
        [Fact]
        public void CS0023ERR_BadUnaryOp_Nullable()
        {
            var text = @"
public class Test
{
    public static void Main()
    {
        bool? b = !null;   // CS0023
        int? n = ~null;    // CS0023
        float? f = +null;  // CS0023
        long? u = -null;   // CS0023

        ++n;
        n--;
        --u;
        u++;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,19): error CS8310: Operator '!' cannot be applied to operand '<null>'
                //         bool? b = !null;   // CS0023
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "!null").WithArguments("!", "<null>").WithLocation(6, 19),
                // (7,18): error CS8310: Operator '~' cannot be applied to operand '<null>'
                //         int? n = ~null;    // CS0023
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "~null").WithArguments("~", "<null>").WithLocation(7, 18),
                // (8,20): error CS8310: Operator '+' cannot be applied to operand '<null>'
                //         float? f = +null;  // CS0023
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "+null").WithArguments("+", "<null>").WithLocation(8, 20),
                // (9,19): error CS8310: Operator '-' cannot be applied to operand '<null>'
                //         long? u = -null;   // CS0023
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "-null").WithArguments("-", "<null>").WithLocation(9, 19)
                );
        }

        [WorkItem(539590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539590")]
        [Fact]
        public void CS0023ERR_BadUnaryOp2()
        {
            var text = @"
namespace X
{
    class C
    {
        void M()
        {
            System.Action f = M;
            f = +M; // CS0023
            f = +(() => { }); // CS0023
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,17): error CS0023: Operator '+' cannot be applied to operand of type 'method group'
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+M").WithArguments("+", "method group"),
                // (10,17): error CS0023: Operator '+' cannot be applied to operand of type 'lambda expression'
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+(() => { })").WithArguments("+", "lambda expression"));
        }

        [WorkItem(540211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540211")]
        [Fact]
        public void CS0023ERR_BadUnaryOp_VoidMissingInstanceMethod()
        {
            var text = @"class C
{
    void M()
    {
        M().Goo();
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,12): error CS0023: Operator '.' cannot be applied to operand of type 'void'
                Diagnostic(ErrorCode.ERR_BadUnaryOp, ".").WithArguments(".", "void"));
        }

        [WorkItem(540211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540211")]
        [Fact]
        public void CS0023ERR_BadUnaryOp_VoidToString()
        {
            var text = @"class C
{
    void M()
    {
        M().ToString(); //plausible, but still wrong
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,12): error CS0023: Operator '.' cannot be applied to operand of type 'void'
                Diagnostic(ErrorCode.ERR_BadUnaryOp, ".").WithArguments(".", "void"));
        }

        [WorkItem(540329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540329")]
        [Fact]
        public void CS0023ERR_BadUnaryOp_null()
        {
            var text = @"
class X
{
    static void Main()
    {
        int x = null.Length;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "null.Length").WithArguments(".", "<null>"));
        }

        [WorkItem(540329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540329")]
        [Fact]
        public void CS0023ERR_BadUnaryOp_lambdaExpression()
        {
            var text = @"
class X
{
    static void Main()
    {
        System.Func<int, int> f = arg => { arg = 2; return arg; }.ToString();
        
        var x = delegate { }.ToString();
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "arg => { arg = 2; return arg; }.ToString").WithArguments(".", "lambda expression"),
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "delegate { }.ToString").WithArguments(".", "anonymous method"));
        }

        [Fact]
        public void CS0026ERR_ThisInStaticMeth()
        {
            var text = @"
public class MyClass
{
    public static int i = 0;

    public static void Main()
    {
        // CS0026
        this.i = this.i + 1;
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] {
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ThisInStaticMeth, Line = 9, Column = 9 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ObjectProhibited, Line = 9, Column = 9 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ThisInStaticMeth, Line = 9, Column = 18 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ObjectProhibited, Line = 9, Column = 18 }
                });
        }

        [Fact]
        public void CS0026ERR_ThisInStaticMeth_StaticConstructor()
        {
            var text = @"
public class MyClass
{
    int f;
    void M() { }
    int P { get; set; }

    static MyClass()
    {
        this.f = this.P;
        this.M();
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,9): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this"),
                // (10,18): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this"),
                // (11,9): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this"));
        }

        [Fact]
        public void CS0026ERR_ThisInStaticMeth_Combined()
        {
            var text = @"
using System;

class CLS
{
    static CLS() { var x = this.ToString(); }
    static object FLD = this.ToString();
    static object PROP { get { return this.ToString(); } }
    static object METHOD() { return this.ToString(); }
}

class A : Attribute
{
    public object P;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,25): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                //     static object FLD = this.ToString();
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this"),
                // (6,28): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                //     static CLS() { var x = this.ToString(); }
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this"),
                // (8,39): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                //     static object PROP { get { return this.ToString(); } }
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this"),
                // (9,37): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                //     static object METHOD() { return this.ToString(); }
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this"),
                // (14,19): warning CS0649: Field 'A.P' is never assigned to, and will always have its default value null
                //     public object P;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "P").WithArguments("A.P", "null")
                );
        }

        [Fact]
        public void CS0027ERR_ThisInBadContext()
        {
            var text = @"
namespace ConsoleApplication3
{
    class MyClass
    {
        int err1 = this.Fun() + 1;  // CS0027 
        public void Fun()
        {
        }
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_ThisInBadContext, Line = 6, Column = 20 } });
        }

        [Fact]
        public void CS0027ERR_ThisInBadContext_2()
        {
            var text = @"
using System;

[assembly: A(P = this.ToString())]

class A : Attribute
{
    public object P;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,18): error CS0027: Keyword 'this' is not available in the current context
                // [assembly: A(P = this.ToString())]
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this"));
        }

        [Fact]
        public void CS0027ERR_ThisInBadContext_Interactive()
        {
            string text = @"
int a;
int b = a;
int c = this.a; // 1
this.c = // 2
    this.a; // 3
int prop { get { return 1; } set { this.a = 1;} } // 4

void goo() {
    this.goo(); // 5
    this.a =    // 6
        this.b; // 7
    object c = this; // 8
}

this.prop = 1; // 9

class C
{
    C() : base()
    {
    }

    void goo()
    {
        this.goo(); // OK
    }
}";
            var comp = CreateCompilationWithMscorlib45(
                new[] { SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.Script) });
            comp.VerifyDiagnostics(
                // (4,9): error CS0027: Keyword 'this' is not available in the current context
                // int c = this.a; // 1
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(4, 9),
                // (5,1): error CS0027: Keyword 'this' is not available in the current context
                // this.c = // 2
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(5, 1),
                // (6,5): error CS0027: Keyword 'this' is not available in the current context
                //     this.a; // 3
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(6, 5),
                // (16,1): error CS0027: Keyword 'this' is not available in the current context
                // this.prop = 1; // 9
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(16, 1),
                // (7,36): error CS0027: Keyword 'this' is not available in the current context
                // int prop { get { return 1; } set { this.a = 1;} } // 4
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(7, 36),
                // (10,5): error CS0027: Keyword 'this' is not available in the current context
                //     this.goo(); // 5
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(10, 5),
                // (11,5): error CS0027: Keyword 'this' is not available in the current context
                //     this.a =    // 6
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(11, 5),
                // (12,9): error CS0027: Keyword 'this' is not available in the current context
                //         this.b; // 7
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(12, 9),
                // (13,16): error CS0027: Keyword 'this' is not available in the current context
                //     object c = this; // 8
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(13, 16)
                );
        }

        [Fact]
        public void CS0029ERR_NoImplicitConv01()
        {
            var text = @"
namespace ConsoleApplication3
{
    class MyClass
    {

        int err1 =  1;   

        public string Fun()
        {
            return err1;
        }

        public static void Main()
        {
            MyClass c = new MyClass();
        }
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_NoImplicitConv, Line = 11, Column = 20 } });
        }

        [Fact]
        public void CS0029ERR_NoImplicitConv02()
        {
            var source = "enum E { A = new[] { 1, 2, 3 } }";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,14): error CS0029: Cannot implicitly convert type 'int[]' to 'int'
                // enum E { A = new[] { 1, 2, 3 } }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new[] { 1, 2, 3 }").WithArguments("int[]", "int").WithLocation(1, 14));
        }

        [Fact]
        public void CS0029ERR_NoImplicitConv03()
        {
            var source =
@"class C
{
    static void M()
    {
        const C d = F();
    }
    static D F()
    {
        return null;
    }
}
class D
{
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,21): error CS0029: Cannot implicitly convert type 'D' to 'C'
                //         const C d = F();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "F()").WithArguments("D", "C").WithLocation(5, 21));
        }

        [WorkItem(541719, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541719")]
        [Fact]
        public void CS0029ERR_NoImplicitConv04()
        {
            var text =
@"class C1
{
    public static void Main()
    {
        bool m = true;
        int[] arr = new int[m];    // Invalid
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,29): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                //         int[] arr = new int[m];    // Invalid
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "m").WithArguments("bool", "int").WithLocation(6, 29));
        }

        [Fact, WorkItem(40405, "https://github.com/dotnet/roslyn/issues/40405")]
        public void ThrowExpression_ImplicitVoidConversion_Return()
        {
            string text = @"
class C
{
    void M1()
    {
        return true ? throw null : M2();
    }

    void M2() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,23): error CS0029: Cannot implicitly convert type '<throw expression>' to 'void'
                //         return true ? throw null : M2();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "throw null").WithArguments("<throw expression>", "void").WithLocation(6, 23));
        }

        [Fact, WorkItem(40405, "https://github.com/dotnet/roslyn/issues/40405")]
        public void ThrowExpression_ImplicitVoidConversion_Assignment()
        {
            string text = @"
class C
{
    void M1()
    {
        object obj = true ? throw null : M2();
    }

    void M2() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,29): error CS0029: Cannot implicitly convert type '<throw expression>' to 'void'
                //         object obj = true ? throw null : M2();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "throw null").WithArguments("<throw expression>", "void").WithLocation(6, 29));
        }

        [Fact, WorkItem(40405, "https://github.com/dotnet/roslyn/issues/40405")]
        public void IntLiteral_ImplicitVoidConversion_Assignment()
        {
            string text = @"
class C
{
    void M1()
    {
        object obj = true ? 0 : M2();
    }

    void M2() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,22): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'int' and 'void'
                //         object obj = true ? 0 : M2();
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? 0 : M2()").WithArguments("int", "void").WithLocation(6, 22));
        }

        [Fact, WorkItem(40405, "https://github.com/dotnet/roslyn/issues/40405")]
        public void VoidCall_ImplicitVoidConversion_Assignment()
        {
            string text = @"
class C
{
    void M1()
    {
        object obj = true ? M2() : M2();
    }

    void M2() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,22): error CS0029: Cannot implicitly convert type 'void' to 'object'
                //         object obj = true ? M2() : M2();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "true ? M2() : M2()").WithArguments("void", "object").WithLocation(6, 22));
        }

        [Fact, WorkItem(40405, "https://github.com/dotnet/roslyn/issues/40405")]
        public void VoidCall_ImplicitVoidConversion_DiscardAssignment()
        {
            string text = @"
class C
{
    void M1()
    {
        _ = true ? M2() : M2();
    }

    void M2() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,9): error CS8209: A value of type 'void' may not be assigned.
                //         _ = true ? M2() : M2();
                Diagnostic(ErrorCode.ERR_VoidAssignment, "_").WithLocation(6, 9));
        }

        [Fact, WorkItem(40405, "https://github.com/dotnet/roslyn/issues/40405")]
        public void VoidCall_Assignment()
        {
            string text = @"
class C
{
    void M1()
    {
        object obj = M2();
    }

    void M2() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,22): error CS0029: Cannot implicitly convert type 'void' to 'object'
                //         object obj = M2();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "M2()").WithArguments("void", "object").WithLocation(6, 22));
        }

        [Fact, WorkItem(40405, "https://github.com/dotnet/roslyn/issues/40405")]
        public void VoidCall_DiscardAssignment()
        {
            string text = @"
class C
{
    void M1()
    {
        _ = M2();
    }

    void M2() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,9): error CS8209: A value of type 'void' may not be assigned.
                //         _ = M2();
                Diagnostic(ErrorCode.ERR_VoidAssignment, "_").WithLocation(6, 9));
        }

        [Fact, WorkItem(40405, "https://github.com/dotnet/roslyn/issues/40405")]
        public void VoidCall_ImplicitVoidConversion_Return()
        {
            string text = @"
class C
{
    void M1()
    {
        return true ? M2() : M2();
    }

    void M2() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,9): error CS0127: Since 'C.M1()' returns void, a return keyword must not be followed by an object expression
                //         return true ? M2() : M2();
                Diagnostic(ErrorCode.ERR_RetNoObjectRequired, "return").WithArguments("C.M1()").WithLocation(6, 9));
        }

        [Fact]
        public void CS0030ERR_NoExplicitConv()
        {
            var text = @"
namespace x
{
    public class iii
    {
        public static iii operator ++(iii aa)
        {
            return (iii)0;   // CS0030
        }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
               // (8,20): error CS0030: Cannot convert type 'int' to 'x.iii'
               //             return (iii)0;   // CS0030
               Diagnostic(ErrorCode.ERR_NoExplicitConv, "(iii)0").WithArguments("int", "x.iii"));
        }

        [WorkItem(528539, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528539")]
        [WorkItem(1119609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1119609")]
        [WorkItem(920, "http://github.com/dotnet/roslyn/issues/920")]
        [Fact]
        public void CS0030ERR_NoExplicitConv02()
        {
            const string text = @"
public class C
{
    public static void Main()
    {
        decimal x = (decimal)double.PositiveInfinity;
    }
}";
            var diagnostics = CreateCompilation(text).GetDiagnostics();

            var savedCurrentCulture = Thread.CurrentThread.CurrentCulture;
            var savedCurrentUICulture = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            try
            {
                diagnostics.Verify(
                    // (6,21): error CS0031: Constant value 'Infinity' cannot be converted to a 'decimal'
                    //         decimal x = (decimal)double.PositiveInfinity;
                    Diagnostic(ErrorCode.ERR_ConstOutOfRange, "(decimal)double.PositiveInfinity").WithArguments("Infinity", "decimal"),
                    // (6,17): warning CS0219: The variable 'x' is assigned but its value is never used
                    //         decimal x = (decimal)double.PositiveInfinity;
                    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = savedCurrentCulture;
                Thread.CurrentThread.CurrentUICulture = savedCurrentUICulture;
            }
        }

        [Fact]
        public void CS0030ERR_NoExplicitConv_Foreach()
        {
            var text = @"
public class Test
{
    static void Main(string[] args)
    {
        int[][] arr = new int[][] { new int[] { 1, 2 }, new int[] { 4, 5, 6 } };
        foreach (int outer in arr) { } // invalid
    }
}";
            CreateCompilation(text).VerifyDiagnostics(Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("int[]", "int"));
        }

        [Fact]
        public void CS0031ERR_ConstOutOfRange01()
        {
            var text =
@"public class a
{
    int num = (int)2147483648M; //CS0031
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,15): error CS0031: Constant value '2147483648M' cannot be converted to a 'int'
                //     int num = (int)2147483648M; //CS0031
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "(int)2147483648M").WithArguments("2147483648M", "int"),
                // (3,9): warning CS0414: The field 'a.num' is assigned but its value is never used
                //     int num = (int)2147483648M; //CS0031
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "num").WithArguments("a.num"));
        }

        [WorkItem(528539, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528539")]
        [Fact]
        public void CS0031ERR_ConstOutOfRange02()
        {
            var text = @"
enum E : ushort
{
    A = 10,
    B = -1 // CS0031
}
enum F : sbyte
{
    A = 0x7f,
    B = 0xf0, // CS0031
    C,
    D = (A + 1) - 2,
    E = (A + 1), // CS0031
}
class A
{
    byte bt = 256;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,9): error CS0031: Constant value '-1' cannot be converted to a 'ushort'
                //     B = -1 // CS0031
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "-1").WithArguments("-1", "ushort").WithLocation(5, 9),
                // (10,9): error CS0031: Constant value '240' cannot be converted to a 'sbyte'
                //     B = 0xf0, // CS0031
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "0xf0").WithArguments("240", "sbyte").WithLocation(10, 9),
                // (13,10): error CS0031: Constant value '128' cannot be converted to a 'sbyte'
                //     E = (A + 1), // CS0031
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "A + 1").WithArguments("128", "sbyte").WithLocation(13, 10),
                // (17,15): error CS0031: Constant value '256' cannot be converted to a 'byte'
                //     byte bt = 256;
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "256").WithArguments("256", "byte").WithLocation(17, 15),
                // (17,10): warning CS0414: The field 'A.bt' is assigned but its value is never used
                //     byte bt = 256;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "bt").WithArguments("A.bt").WithLocation(17, 10));
        }

        [Fact]
        public void CS0221ERR_ConstOutOfRangeChecked04()
        {
            // Confirm that we truncate the constant value before performing the range check
            var template =
@"public class C
{
    void M()
    {
        System.Console.WriteLine((System.Int32)(System.Int32.MinValue - 0.9));
        System.Console.WriteLine((System.Int32)(System.Int32.MinValue - 1.0)); //CS0221
        System.Console.WriteLine((System.Int32)(System.Int32.MaxValue + 0.9));
        System.Console.WriteLine((System.Int32)(System.Int32.MaxValue + 1.0)); //CS0221
    }
}
";
            var integralTypes = new Type[]
            {
                typeof(char),
                typeof(sbyte),
                typeof(byte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
            };

            foreach (Type t in integralTypes)
            {
                DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(template.Replace("System.Int32", t.ToString()),
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 6, Column = 34 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 8, Column = 34 });
            }
        }

        // Note that the errors for Int64 and UInt64 are not
        // exactly the same as for Int32, etc. above, but the
        // differences match the native compiler.
        [WorkItem(528715, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528715")]
        [Fact]
        public void CS0221ERR_ConstOutOfRangeChecked05()
        {
            // Confirm that we truncate the constant value before performing the range check
            var text1 =
@"public class C
{
    void M()
    {
        System.Console.WriteLine((System.Int64)(System.Int64.MinValue - 0.9));
        System.Console.WriteLine((System.Int64)(System.Int64.MinValue - 1.0));
        System.Console.WriteLine((System.Int64)(System.Int64.MaxValue + 0.9)); //CS0221
        System.Console.WriteLine((System.Int64)(System.Int64.MaxValue + 1.0)); //CS0221
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text1,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 7, Column = 34 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 8, Column = 34 });

            var text2 =
@"public class C
{
    void M()
    {
        System.Console.WriteLine((System.UInt64)(System.UInt64.MinValue - 0.9));
        System.Console.WriteLine((System.UInt64)(System.UInt64.MinValue - 1.0)); //CS0221
        System.Console.WriteLine((System.UInt64)(System.UInt64.MaxValue + 0.9)); //CS0221
        System.Console.WriteLine((System.UInt64)(System.UInt64.MaxValue + 1.0)); //CS0221
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text2,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 6, Column = 34 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 7, Column = 34 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 8, Column = 34 });

            var text3 =
@"class C
{
    static void Main()
    {
        System.Console.WriteLine(long.MinValue);
        System.Console.WriteLine((long)(double)long.MinValue);
    }
}";
            CreateCompilation(text3).VerifyDiagnostics();
        }

        [Fact]
        public void CS0034ERR_AmbigBinaryOps()
        {
            #region "Source"
            var text = @"
public class A
{
    // allows for the conversion of A object to int
    public static implicit operator int(A s)
    {
        return 0;
    }

    public static implicit operator string(A i)
    {
        return null;
    }
}

public class B
{
    public static implicit operator int(B s)
    // one way to resolve this CS0034 is to make one conversion explicit
    // public static explicit operator int (B s)
    {
        return 0;
    }

    public static implicit operator string(B i)
    {
        return null;
    }

    public static implicit operator B(string i)
    {
        return null;
    }

    public static implicit operator B(int i)
    {
        return null;
    }
}

public class C
{
    public static void Main()
    {
        A a = new A();
        B b = new B();
        b = b + a;   // CS0034
        // another way to resolve this CS0034 is to make a cast
        // b = b + (int)a;
    }
}
";
            #endregion

            CreateCompilation(text).VerifyDiagnostics(
                // (47,13): error CS0034: Operator '+' is ambiguous on operands of type 'B' and 'A'
                //         b = b + a;   // CS0034
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "b + a").WithArguments("+", "B", "A"));
        }

        [Fact]
        public void CS0035ERR_AmbigUnaryOp_RoslynCS0023()
        {
            var text = @"
class MyClass
{
    private int i;

    public MyClass(int i)
    {
        this.i = i;
    }

    public static implicit operator double(MyClass x)
    {
        return (double)x.i;
    }

    public static implicit operator decimal(MyClass x)
    {
        return (decimal)x.i;
    }
}

class MyClass2
{
    static void Main()
    {
        MyClass x = new MyClass(7);
        object o = -x;   // CS0035
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (27,20): error CS0035: Operator '-' is ambiguous on an operand of type 'MyClass'
                //         object o = -x;   // CS0035
                Diagnostic(ErrorCode.ERR_AmbigUnaryOp, "-x").WithArguments("-", "MyClass"));
        }

        [Fact]
        public void CS0037ERR_ValueCantBeNull01()
        {
            var source =
@"enum E { }
struct S { }
class C
{
    static void M()
    {
        int i;
        i = null;
        i = (int)null;
        E e;
        e = null;
        e = (E)null;
        S s;
        s = null;
        s = (S)null;
        X x;
        x = null;
        x = (X)null;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,13): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(8, 13),
                // (9,13): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "(int)null").WithArguments("int").WithLocation(9, 13),
                // (11,13): error CS0037: Cannot convert null to 'E' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("E").WithLocation(11, 13),
                // (12,13): error CS0037: Cannot convert null to 'E' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "(E)null").WithArguments("E").WithLocation(12, 13),
                // (14,13): error CS0037: Cannot convert null to 'S' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("S").WithLocation(14, 13),
                // (15,13): error CS0037: Cannot convert null to 'S' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "(S)null").WithArguments("S").WithLocation(15, 13),
                // (16,9): error CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(16, 9),
                // (18,14): error CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(18, 14));
        }

        [Fact]
        public void CS0037ERR_ValueCantBeNull02()
        {
            var source =
@"interface I { }
class A { }
class B<T1, T2, T3, T4, T5, T6, T7>
    where T2 : class
    where T3 : struct
    where T4 : new()
    where T5 : I
    where T6 : A
    where T7 : T1
{
    static void M(object o)
    {
        o = (T1)null;
        o = (T2)null;
        o = (T3)null;
        o = (T4)null;
        o = (T5)null;
        o = (T6)null;
        o = (T7)null;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,17): error CS0037: Cannot convert null to 'T1' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "(T1)null").WithArguments("T1").WithLocation(13, 13),
                // (15,17): error CS0037: Cannot convert null to 'T3' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "(T3)null").WithArguments("T3").WithLocation(15, 13),
                // (16,17): error CS0037: Cannot convert null to 'T4' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "(T4)null").WithArguments("T4").WithLocation(16, 13),
                // (17,17): error CS0037: Cannot convert null to 'T5' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "(T5)null").WithArguments("T5").WithLocation(17, 13),
                // (19,17): error CS0037: Cannot convert null to 'T7' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "(T7)null").WithArguments("T7").WithLocation(19, 13));
        }

        [WorkItem(539589, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539589")]
        [Fact]
        public void CS0037ERR_ValueCantBeNull03()
        {
            var text = @"
class Program
{
    enum MyEnum
    {
        Zero = 0,
        One = 1
    }

    static int Main()
    {
        return Goo((MyEnum)null);
    }

    static int Goo(MyEnum x)
    {
        return 1;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(Diagnostic(ErrorCode.ERR_ValueCantBeNull, "(MyEnum)null").WithArguments("Program.MyEnum").WithLocation(12, 20));
        }

        [Fact(), WorkItem(528875, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528875")]
        public void CS0038ERR_WrongNestedThis()
        {
            var text = @"
class OuterClass
{
   public int count;
   // try the following line instead
   // public static int count;

   class InnerClass
   {
      void func()
      {
         // or, create an instance
         // OuterClass class_inst = new OuterClass();
         // int count2 = class_inst.count;
         int count2 = count;   // CS0038
      }
   }

   public static void Main()
   {
   }
}";
            // Triage decided not to implement the more specific error (WrongNestedThis) and stick with ObjectRequired.
            var comp = CreateCompilation(text, options: TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>()
            {
                { MessageProvider.Instance.GetIdForErrorCode(649), ReportDiagnostic.Suppress }
            }));

            comp.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_ObjectRequired, "count").WithArguments("OuterClass.count"));
        }

        [Fact]
        public void CS0039ERR_NoExplicitBuiltinConv01()
        {
            var text =
@"class A
{
}
class B: A
{
}
class C: A
{
}
class M
{
    static void Main()
    {
        A a = new C();
        B b = new B();
        C c;

        // This is valid; there is a built-in reference
        // conversion from A to C.
        c = a as C;  

        //The following generates CS0039; there is no
        // built-in reference conversion from B to C.
        c = b as C;  // CS0039
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (24,13): error CS0039: Cannot convert type 'B' to 'C' via a reference conversion, boxing conversion, unboxing conversion, wrapping conversion, or null type conversion
                Diagnostic(ErrorCode.ERR_NoExplicitBuiltinConv, "b as C").WithArguments("B", "C").WithLocation(24, 13));
        }

        [Fact, WorkItem(541142, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541142")]
        public void CS0039ERR_NoExplicitBuiltinConv02()
        {
            var text =
@"delegate void D();
class C
{
    static void M(C c)
    {
        (F as D)();
        (c.F as D)();
        (G as D)();
    }
    void F() { }
    static void G() { }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,10): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         (F as D)();
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "F as D").WithLocation(6, 10),
                // (7,10): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         (c.F as D)();
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "c.F as D").WithLocation(7, 10),
                // (8,10): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         (G as D)();
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "G as D").WithLocation(8, 10));
        }

        [Fact, WorkItem(542047, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542047")]
        public void CS0039ERR_ConvTypeReferenceToObject()
        {
            var text = @"using System;
class C
{
    static void Main()
    {
        TypedReference a = new TypedReference();
        object obj = a as object;   //CS0039
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                //(7,22): error CS0039: Cannot convert type 'System.TypedReference' to 'object' via a reference conversion, boxing conversion, unboxing conversion, wrapping conversion, or null type conversion
                Diagnostic(ErrorCode.ERR_NoExplicitBuiltinConv, "a as object").WithArguments("System.TypedReference", "object").WithLocation(7, 22));
        }

        [Fact]
        public void CS0069ERR_EventPropertyInInterface()
        {
            var text = @"
interface I
{
    event System.Action E1 { add; }
    event System.Action E2 { remove; }
    event System.Action E3 { add; remove; }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular7, targetFramework: TargetFramework.NetStandardLatest).VerifyDiagnostics(
                // (4,30): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     event System.Action E1 { add; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "8.0").WithLocation(4, 30),
                // (4,33): error CS0073: An add or remove accessor must have a body
                //     event System.Action E1 { add; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 33),
                // (5,30): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     event System.Action E2 { remove; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "8.0").WithLocation(5, 30),
                // (5,36): error CS0073: An add or remove accessor must have a body
                //     event System.Action E2 { remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(5, 36),
                // (6,30): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     event System.Action E3 { add; remove; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "8.0").WithLocation(6, 30),
                // (6,33): error CS0073: An add or remove accessor must have a body
                //     event System.Action E3 { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 33),
                // (6,35): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     event System.Action E3 { add; remove; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "8.0").WithLocation(6, 35),
                // (6,41): error CS0073: An add or remove accessor must have a body
                //     event System.Action E3 { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 41),
                // (4,25): error CS0065: 'I.E1': event property must have both add and remove accessors
                //     event System.Action E1 { add; }
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I.E1").WithLocation(4, 25),
                // (5,25): error CS0065: 'I.E2': event property must have both add and remove accessors
                //     event System.Action E2 { remove; }
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E2").WithArguments("I.E2").WithLocation(5, 25));
        }

        [Fact]
        public void CS0070ERR_BadEventUsage()
        {
            var text = @"
public delegate void EventHandler();

public class A
{
   public event EventHandler Click;

   public static void OnClick()
   {
      EventHandler eh;
      A a = new A();
      eh = a.Click;
   }

   public static void Main()
   {
   }
}

public class B
{
   public int mf ()
   {
      EventHandler eh = new EventHandler(A.OnClick);
      A a = new A();
      eh = a.Click;   // CS0070
      // try the following line instead
      // a.Click += eh;
      return 1;
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadEventUsage, Line = 26, Column = 14 } });
        }

        [Fact]
        public void CS0079ERR_BadEventUsageNoField()
        {
            var text = @"
public delegate void MyEventHandler();

public class Class1
{
    private MyEventHandler _e;

    public event MyEventHandler Pow
    {
        add
        {
            _e += value;
        }
        remove
        {
            _e -= value;
        }
    }

    public void Handler()
    {
    }

    public void Fire()
    {
        if (_e != null)
        {
            Pow();   // CS0079
            // try the following line instead
            // _e();
        }
    }

    public static void Main()
    {
        Class1 p = new Class1();
        p.Pow += new MyEventHandler(p.Handler);
        p._e();
        p.Pow += new MyEventHandler(p.Handler);
        p._e();
        p._e -= new MyEventHandler(p.Handler);
        if (p._e != null)
        {
            p._e();
        }
        p.Pow -= new MyEventHandler(p.Handler);
        if (p._e != null)
        {
            p._e();
        }
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadEventUsageNoField, Line = 28, Column = 13 } });
        }

        [WorkItem(538213, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538213")]
        [Fact]
        public void CS0103ERR_NameNotInContext()
        {
            var text = @"
class C
{
    static void M()
    {
        IO.File.Exists(""test"");
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NameNotInContext, "IO").WithArguments("IO").WithLocation(6, 9));
        }

        [WorkItem(542574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542574")]
        [Fact]
        public void CS0103ERR_NameNotInContextLambdaExtension()
        {
            var text = @"using System.Linq;
class Test
{
    static void Main()
    {
        int[] sourceA = { 1, 2, 3, 4, 5 };
        int[] sourceB = { 3, 4, 5, 6, 7 };

        var query = sourceA.Join(sourceB, a => b, b => 5, (a, b) => a + b);
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(9, 48));
        }

        [Fact()]
        public void CS0103ERR_NameNotInContext_foreach()
        {
            var text = @"class C
{
    static void Main()
    {
        foreach (var y in new[] {new {y = y }}){ }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(5, 43));
        }

        [WorkItem(528780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528780")]
        [Fact]
        public void CS0103ERR_NameNotInContext_namedAndOptional()
        {
            var text = @"using System;
class NamedExample
{
    static void Main(string[] args)
    {
    }
    static int CalculateBMI(int weight, int height = weight)
    {
        return (weight * 703) / (height * height);
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,54): error CS0103: The name 'weight' does not exist in the current context
                //     static int CalculateBMI(int weight, int height = weight)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "weight").WithArguments("weight").WithLocation(7, 54),
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1)
                );
        }

        [Fact]
        public void CS0118ERR_BadSKknown()
        {
            CreateCompilation(
@"public class TestType {}

public class MyClass {

    public static int Main() {
        TestType myTest = new TestType();
        bool b = myTest is myTest;
        return 1;
    }

}", parseOptions: TestOptions.Regular6)
                .VerifyDiagnostics(
                // (7,22): error CS0118: 'myTest' is a 'variable' but is used like a 'type'
                Diagnostic(ErrorCode.ERR_BadSKknown, "myTest").WithArguments("myTest", "variable", "type"));
        }

        [Fact]
        public void CS0118ERR_BadSKknown_02()
        {
            CreateCompilation(@"
using System;
public class P {
    public static void Main(string[] args) {
#pragma warning disable 219
        Action<args> a = null;
        Action<a> b = null;
    }
}")
                .VerifyDiagnostics(
                    // (6,16): error CS0118: 'args' is a variable but is used like a type
                    //         Action<args> a = null;
                    Diagnostic(ErrorCode.ERR_BadSKknown, "args").WithArguments("args", "variable", "type").WithLocation(6, 16),
                    // (7,16): error CS0118: 'a' is a variable but is used like a type
                    //         Action<a> b = null;
                    Diagnostic(ErrorCode.ERR_BadSKknown, "a").WithArguments("a", "variable", "type").WithLocation(7, 16));
        }


        [Fact]
        public void CS0118ERR_BadSKknown_CheckedUnchecked()
        {
            string source = @"
using System;
 
class Program
{
    static void Main()
    {
		var z = 1;
    
		(Console).WriteLine();                          // error
        (System).Console.WriteLine();                   // error
        checked(Console).WriteLine();                   // error 
        checked(System).Console.WriteLine();            // error

        checked(z).ToString();                          // ok
        checked(typeof(Console)).ToString();            // ok
        checked(Console.WriteLine)();                   // ok
        checked(z) = 1;                                 // ok
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,4): error CS0119: 'System.Console' is a type, which is not valid in the given context
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Console").WithArguments("System.Console", "type"),
                // (11,10): error CS0118: 'System' is a namespace but is used like a variable
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "variable"),
                // (12,17): error CS0119: 'System.Console' is a type, which is not valid in the given context
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Console").WithArguments("System.Console", "type"),
                // (13,17): error CS0118: 'System' is a namespace but is used like a variable
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "variable"));
        }

        [WorkItem(542773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542773")]
        [Fact]
        public void CS0119ERR_BadSKunknown01_switch()
        {
            CreateCompilation(
@"class A
{
    public static void Main()
    { }
    void goo(color color1)
    {
        switch (color)
        {
            default:
                break;
        }
    }
}
enum color
{
    blue,
    green
}
")
                .VerifyDiagnostics(Diagnostic(ErrorCode.ERR_BadSKunknown, "color").WithArguments("color", "type"));
        }

        [Fact, WorkItem(538214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538214"), WorkItem(528703, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528703")]
        public void CS0119ERR_BadSKunknown01()
        {
            var source =
@"class Test
{
    public static void M()
    {
        int x = 0;
        x = (global::System.Int32) + x;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,14): error CS0119: 'int' is a type, which is not valid in the given context
                //         x = (global::System.Int32) + x;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "global::System.Int32").WithArguments("int", "type"),
                // (6,14): error CS0119: 'int' is a type, which is not valid in the given context
                //         x = (global::System.Int32) + x;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "global::System.Int32").WithArguments("int", "type"));
        }

        [Fact]
        public void CS0119ERR_BadSKunknown02()
        {
            var source =
@"class A
{
    internal static object F;
    internal static void M() { }
}
class B<T, U> where T : A
{
    static void M(T t)
    {
        U.ReferenceEquals(T.F, null);
        T.M();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,27): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         U.ReferenceEquals(T.F, null);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter"),
                // (10,9): error CS0119: 'U' is a type parameter, which is not valid in the given context
                //         U.ReferenceEquals(T.F, null);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "U").WithArguments("U", "type parameter"),
                // (11,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.M();
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter"),
                // (3,28): warning CS0649: Field 'A.F' is never assigned to, and will always have its default value null
                //     internal static object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("A.F", "null")
                );
        }

        [WorkItem(541203, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541203")]
        [Fact]
        public void CS0119ERR_BadSKunknown_InThrowStmt()
        {
            CreateCompilation(
@"class Test
{
    public static void M()
    {
        throw System.Exception;
    }
}")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Exception").WithArguments("System.Exception", "type"));
        }

        [Fact]
        public void CS0110ERR_CircConstValue01()
        {
            var source =
@"namespace x
{
    public class C
    {
        const int x = 1;

        const int a = x + b;
        const int b = x + c;
        const int c = x + d;
        const int d = x + e;
        const int e = x + a;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,19): error CS0110: The evaluation of the constant value for 'x.C.a' involves a circular definition
                //         const int a = x + b;
                Diagnostic(ErrorCode.ERR_CircConstValue, "a").WithArguments("x.C.a").WithLocation(7, 19));
        }

        [Fact]
        public void CS0110ERR_CircConstValue02()
        {
            var source =
@"enum E { A = B, B = A }
enum F { X, Y = Y }
";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,10): error CS0110: The evaluation of the constant value for 'E.A' involves a circular definition
                // enum E { A = B, B = A }
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E.A").WithLocation(1, 10),
                // (2,13): error CS0110: The evaluation of the constant value for 'F.Y' involves a circular definition
                // enum F { X, Y = Y }
                Diagnostic(ErrorCode.ERR_CircConstValue, "Y").WithArguments("F.Y").WithLocation(2, 13));
        }

        [Fact]
        public void CS0110ERR_CircConstValue03()
        {
            var source =
@"enum E { A, B = A } // no error
enum F { W, X = Z, Y, Z }
";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,13): error CS0110: The evaluation of the constant value for 'F.X' involves a circular definition
                // enum F { W, X = Z, Y, Z }
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("F.X").WithLocation(2, 13));
        }

        [Fact]
        public void CS0110ERR_CircConstValue04()
        {
            var source =
@"enum E { A = B, B }
";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,10): error CS0110: The evaluation of the constant value for 'E.A' involves a circular definition
                // enum E { A = B, B }
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E.A").WithLocation(1, 10));
        }

        [Fact]
        public void CS0110ERR_CircConstValue05()
        {
            var source =
@"enum E { A = C, B = C, C }
";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,17): error CS0110: The evaluation of the constant value for 'E.B' involves a circular definition
                // enum E { A = C, B = C, C }
                Diagnostic(ErrorCode.ERR_CircConstValue, "B").WithArguments("E.B").WithLocation(1, 17));
        }

        [Fact]
        public void CS0110ERR_CircConstValue06()
        {
            var source =
@"class C
{
    private const int F = (int)E.B;
    enum E { A = F, B }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,23): error CS0110: The evaluation of the constant value for 'C.F' involves a circular definition
                //     private const int F = (int)E.B;
                Diagnostic(ErrorCode.ERR_CircConstValue, "F").WithArguments("C.F").WithLocation(3, 23));
        }

        [Fact]
        public void CS0110ERR_CircConstValue07()
        {
            // Should report errors from other subexpressions
            // in addition to circular reference.
            var source =
@"class C
{
    const int F = (long)(F + F + G);
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,34): error CS0103: The name 'G' does not exist in the current context
                //     const int F = (long)(F + F + G);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "G").WithArguments("G").WithLocation(3, 34),
                // (3,15): error CS0110: The evaluation of the constant value for 'C.F' involves a circular definition
                //     const int F = (long)(F + F + G);
                Diagnostic(ErrorCode.ERR_CircConstValue, "F").WithArguments("C.F").WithLocation(3, 15));
        }

        [Fact]
        public void CS0110ERR_CircConstValue08()
        {
            // Decimal constants are special (since they're not runtime constants).
            var source =
@"class C
{
    const decimal D = D;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,19): error CS0110: The evaluation of the constant value for 'C.D' involves a circular definition
                //     const decimal D = D;
                Diagnostic(ErrorCode.ERR_CircConstValue, "D").WithArguments("C.D").WithLocation(3, 19));
        }

        [Fact]
        public void CS0116ERR_NamespaceUnexpected_1()
        {
            var test = @"
int x;
";
            CreateCompilation(test, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (2,5): warning CS0168: The variable 'x' is declared but never used
                // int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(2, 5)
                );
        }

        [Fact]
        public void CS0116ERR_NamespaceUnexpected_2()
        {
            var test = @"
namespace x
{
    using System;
    void Method(string str) // CS0116
    {
        Console.WriteLine(str);
    }
}
int AIProp { get ; set ; }
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(test,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NamespaceUnexpected, Line = 5, Column = 10 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NamespaceUnexpected, Line = 10, Column = 5 });
        }

        [Fact]
        public void CS0116ERR_NamespaceUnexpected_3()
        {
            var test = @"
namespace ns1
{
    goto Labl; // Invalid
    const int x = 1;
    Lab1:
    const int y = 2;
}
";
            // TODO (tomat): EOFUnexpected shouldn't be reported if we enable parsing global statements in namespaces
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(test,
                // (4,5): error CS1022: Type or namespace definition, or end-of-file expected
                // (4,10): error CS0116: A namespace does not directly contain members such as fields or methods
                // (4,14): error CS1022: Type or namespace definition, or end-of-file expected
                // (6,5): error CS0116: A namespace does not directly contain members such as fields or methods
                // (6,9): error CS1022: Type or namespace definition, or end-of-file expected
                // (5,15): error CS0116: A namespace does not directly contain members such as fields or methods
                // (7,15): error CS0116: A namespace does not directly contain members such as fields or methods
                new ErrorDescription { Code = (int)ErrorCode.ERR_EOFExpected, Line = 4, Column = 5 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NamespaceUnexpected, Line = 4, Column = 10 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_EOFExpected, Line = 4, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NamespaceUnexpected, Line = 6, Column = 5 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_EOFExpected, Line = 6, Column = 9 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NamespaceUnexpected, Line = 5, Column = 15 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NamespaceUnexpected, Line = 7, Column = 15 });
        }

        [WorkItem(540091, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540091")]
        [Fact]
        public void CS0116ERR_NamespaceUnexpected_4()
        {
            var test = @"
delegate int D();
D d = null;
";
            CreateCompilation(test, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (3,1): error CS8803: Top-level statements must precede namespace and type declarations.
                    // D d = null;
                    Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "D d = null;").WithLocation(3, 1),
                    // (3,3): warning CS0219: The variable 'd' is assigned but its value is never used
                    // D d = null;
                    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "d").WithArguments("d").WithLocation(3, 3)
                );
        }

        [WorkItem(540091, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540091")]
        [Fact]
        public void CS0116ERR_NamespaceUnexpected_5()
        {
            var test = @"
delegate int D();
D d = {;}
";
            // In this case, CS0116 is suppressed because of the syntax errors

            CreateCompilation(test, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (3,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // D d = {;}
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "D d = {;").WithLocation(3, 1),
                // (3,8): error CS1513: } expected
                Diagnostic(ErrorCode.ERR_RbraceExpected, ";"),
                // (3,9): error CS1022: Type or namespace definition, or end-of-file expected
                Diagnostic(ErrorCode.ERR_EOFExpected, "}"),
                // (3,7): error CS0622: Can only use array initializer expressions to assign to array types. Try using a new expression instead.
                Diagnostic(ErrorCode.ERR_ArrayInitToNonArrayType, "{"));
        }

        [WorkItem(539129, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539129")]
        [Fact]
        public void CS0117ERR_NoSuchMember()
        {
            CreateCompilation(
@"enum E { }
class C
{
    static void M()
    {
        C.F(E.A);
        C.P = C.Q;
    }
}")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "F").WithArguments("C", "F"),
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "A").WithArguments("E", "A"),
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "P").WithArguments("C", "P"),
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "Q").WithArguments("C", "Q"));
        }

        [Fact]
        public void CS0120ERR_ObjectRequired01()
        {
            CreateCompilation(
@"class C
{
    object field;
    object Property { get; set; }
    void Method() { }
    static void M()
    {
        field = Property;
        C.field = C.Property;
        Method();
        C.Method();
    }
}
")
            .VerifyDiagnostics(
                // (8,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.field'
                //         field = Property;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "field").WithArguments("C.field").WithLocation(8, 9),
                // (8,17): error CS0120: An object reference is required for the non-static field, method, or property 'C.Property'
                //         field = Property;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Property").WithArguments("C.Property").WithLocation(8, 17),
                // (9,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.field'
                //         C.field = C.Property;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.field").WithArguments("C.field").WithLocation(9, 9),
                // (9,19): error CS0120: An object reference is required for the non-static field, method, or property 'C.Property'
                //         C.field = C.Property;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.Property").WithArguments("C.Property").WithLocation(9, 19),
                // (10,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.Method()'
                //         Method();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Method").WithArguments("C.Method()").WithLocation(10, 9),
                // (11,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.Method()'
                //         C.Method();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.Method").WithArguments("C.Method()").WithLocation(11, 9));
        }

        [Fact]
        public void CS0120ERR_ObjectRequired02()
        {
            CreateCompilation(
@"using System;

class Program
{
    private readonly int v = 5;
    delegate int del(int i);
    static void Main(string[] args)
    {
        del myDelegate = (int x) => x * v;
        Console.Write(string.Concat(myDelegate(7), ""he""));
    }
}")
            .VerifyDiagnostics(
                // (9,41): error CS0120: An object reference is required for the non-static field, method, or property 'C.field'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "v").WithArguments("Program.v"));
        }

        [Fact]
        public void CS0120ERR_ObjectRequired03()
        {
            var source =
@"delegate int boo();
interface I
{
    int bar();
}
public struct abc : I
{
    public int bar() { System.Console.WriteLine(""bar""); return 0x01; }
}
class C
{
    static void Main(string[] args)
    {
        abc p = new abc();
        boo goo = null;
        goo += new boo(I.bar);
        goo();
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (16,24): error CS0120: An object reference is required for the non-static field, method, or property 'I.bar()'
                //         goo += new boo(I.bar);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "I.bar").WithArguments("I.bar()"),
                // (14,13): warning CS0219: The variable 'p' is assigned but its value is never used
                //         abc p = new abc();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "p").WithArguments("p")
            );
            CreateCompilation(source, parseOptions: TestOptions.Regular).VerifyDiagnostics(
                // (16,24): error CS0120: An object reference is required for the non-static field, method, or property 'I.bar()'
                //         goo += new boo(I.bar);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "I.bar").WithArguments("I.bar()").WithLocation(16, 24),
                // (14,13): warning CS0219: The variable 'p' is assigned but its value is never used
                //         abc p = new abc();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "p").WithArguments("p").WithLocation(14, 13)
            );
        }

        [WorkItem(543950, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543950")]
        [Fact]
        public void CS0120ERR_ObjectRequired04()
        {
            CreateCompilation(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        var f = new Func<string>(() => ToString());
        var g = new Func<int>(() => GetHashCode());
    }
}")
            .VerifyDiagnostics(
                // (7,40): error CS0120: An object reference is required for the non-static field, method, or property 'object.ToString()'
                //         var f = new Func<string>(() => ToString());
                Diagnostic(ErrorCode.ERR_ObjectRequired, "ToString").WithArguments("object.ToString()"),
                // (8,37): error CS0120: An object reference is required for the non-static field, method, or property 'object.GetHashCode()'
                //         var g = new Func<int>(() => GetHashCode());
                Diagnostic(ErrorCode.ERR_ObjectRequired, "GetHashCode").WithArguments("object.GetHashCode()")
            );
        }

        [Fact]
        public void CS0120ERR_ObjectRequired_ConstructorInitializer()
        {
            CreateCompilation(
@"class B
{
    public B(params int[] p) { }
}

class C : B
{
    int instanceField;
    static int staticField;

    int InstanceProperty { get; set; }
    static int StaticProperty { get; set; }

    int InstanceMethod() { return 0; }
    static int StaticMethod() { return 0; }

    C(int param) : base(
        param,
        instanceField, //CS0120
        staticField,
        InstanceProperty, //CS0120
        StaticProperty,
        InstanceMethod(), //CS0120
        StaticMethod(),
        this.instanceField, //CS0027
        C.staticField,
        this.InstanceProperty, //CS0027
        C.StaticProperty,
        this.InstanceMethod(), //CS0027
        C.StaticMethod()) 
    { 
    }
}
")
            .VerifyDiagnostics(
                // (19,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.instanceField'
                //         instanceField, //CS0120
                Diagnostic(ErrorCode.ERR_ObjectRequired, "instanceField").WithArguments("C.instanceField").WithLocation(19, 9),
                // (21,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceProperty'
                //         InstanceProperty, //CS0120
                Diagnostic(ErrorCode.ERR_ObjectRequired, "InstanceProperty").WithArguments("C.InstanceProperty").WithLocation(21, 9),
                // (23,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod()'
                //         InstanceMethod(), //CS0120
                Diagnostic(ErrorCode.ERR_ObjectRequired, "InstanceMethod").WithArguments("C.InstanceMethod()").WithLocation(23, 9),
                // (25,9): error CS0027: Keyword 'this' is not available in the current context
                //         this.instanceField, //CS0027
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(25, 9),
                // (27,9): error CS0027: Keyword 'this' is not available in the current context
                //         this.InstanceProperty, //CS0027
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(27, 9),
                // (29,9): error CS0027: Keyword 'this' is not available in the current context
                //         this.InstanceMethod(), //CS0027
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(29, 9),
                // (8,9): warning CS0649: Field 'C.instanceField' is never assigned to, and will always have its default value 0
                //     int instanceField;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "instanceField").WithArguments("C.instanceField", "0").WithLocation(8, 9),
                // (9,16): warning CS0649: Field 'C.staticField' is never assigned to, and will always have its default value 0
                //     static int staticField;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "staticField").WithArguments("C.staticField", "0").WithLocation(9, 16));
        }

        [Fact]
        public void CS0120ERR_ObjectRequired_StaticConstructor()
        {
            CreateCompilation(
@"class C
{
    object field;
    object Property { get; set; }
    void Method() { }
    static C()
    {
        field = Property;
        C.field = C.Property;
        Method();
        C.Method();
    }
}
")
            .VerifyDiagnostics(
                // (8,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.field'
                //         field = Property;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "field").WithArguments("C.field").WithLocation(8, 9),
                // (8,17): error CS0120: An object reference is required for the non-static field, method, or property 'C.Property'
                //         field = Property;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Property").WithArguments("C.Property").WithLocation(8, 17),
                // (9,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.field'
                //         C.field = C.Property;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.field").WithArguments("C.field").WithLocation(9, 9),
                // (9,19): error CS0120: An object reference is required for the non-static field, method, or property 'C.Property'
                //         C.field = C.Property;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.Property").WithArguments("C.Property").WithLocation(9, 19),
                // (10,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.Method()'
                //         Method();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Method").WithArguments("C.Method()").WithLocation(10, 9),
                // (11,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.Method()'
                //         C.Method();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.Method").WithArguments("C.Method()").WithLocation(11, 9));
        }

        [Fact]
        public void CS0120ERR_ObjectRequired_NestedClass()
        {
            CreateCompilation(
@"
class C
{
    object field;
    object Property { get; set; }
    void Method() { }

    class D
    {
        object field2;
        object Property2 { get; set; }

        public void Goo() 
        {
            object f = field;
            object p = Property;
            Method();
        }

        public static void Bar() 
        {
            object f1 = field;
            object p1 = Property;
            Method();

            object f2 = field2;
            object p2 = Property2;
            Goo();
        }
    }

    class E : C
    {
        public void Goo() 
        {
            object f3 = field;
            object p3 = Property;
            Method();
        }
    }
}")
            .VerifyDiagnostics(
                // (15,24): error CS0120: An object reference is required for the non-static field, method, or property 'C.field'
                //             object f = field;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "field").WithArguments("C.field"),
                // (16,24): error CS0120: An object reference is required for the non-static field, method, or property 'C.Property'
                //             object p = Property;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Property").WithArguments("C.Property"),
                // (17,13): error CS0120: An object reference is required for the non-static field, method, or property 'C.Method()'
                //             Method();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Method").WithArguments("C.Method()"),
                // (22,25): error CS0120: An object reference is required for the non-static field, method, or property 'C.field'
                //             object f1 = field;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "field").WithArguments("C.field"),
                // (23,25): error CS0120: An object reference is required for the non-static field, method, or property 'C.Property'
                //             object p1 = Property;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Property").WithArguments("C.Property"),
                // (24,13): error CS0120: An object reference is required for the non-static field, method, or property 'C.Method()'
                //             Method();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Method").WithArguments("C.Method()"),
                // (26,25): error CS0120: An object reference is required for the non-static field, method, or property 'C.D.field2'
                //             object f2 = field2;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "field2").WithArguments("C.D.field2"),
                // (27,25): error CS0120: An object reference is required for the non-static field, method, or property 'C.D.Property2'
                //             object p2 = Property2;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Property2").WithArguments("C.D.Property2"),
                // (28,13): error CS0120: An object reference is required for the non-static field, method, or property 'C.D.Goo()'
                //             Goo();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Goo").WithArguments("C.D.Goo()"),
                // (4,12): warning CS0649: Field 'C.field' is never assigned to, and will always have its default value null
                //     object field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C.field", "null"),
                // (10,16): warning CS0649: Field 'C.D.field2' is never assigned to, and will always have its default value null
                //         object field2;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field2").WithArguments("C.D.field2", "null"));
        }

        [Fact, WorkItem(541505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541505")]
        public void CS0120ERR_ObjectRequired_Attribute()
        {
            var text = @"
using System.ComponentModel;
enum ProtectionLevel
{
  Privacy = 0
}
class F
{
  [DefaultValue(Prop.Privacy)] // CS0120
  ProtectionLevel Prop { get { return 0; } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,17): error CS0120: An object reference is required for the non-static field, method, or property 'F.Prop'
                //   [DefaultValue(Prop.Privacy)] // CS0120
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Prop").WithArguments("F.Prop"),
                // (9,17): error CS0176: Member 'ProtectionLevel.Privacy' cannot be accessed with an instance reference; qualify it with a type name instead
                //   [DefaultValue(Prop.Privacy)] // CS0120
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "Prop.Privacy").WithArguments("ProtectionLevel.Privacy")  // Extra In Roslyn
                );
        }

        [Fact]
        public void CS0121ERR_AmbigCall()
        {
            var text = @"
public class C
{
   void f(int i, double d) { }
   void f(double d, int i) { }

    public static void Main()
    {
        new C().f(1, 1);   // CS0121
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.f(int, double)' and 'C.f(double, int)'
                //         new C().f(1, 1);   // CS0121
                Diagnostic(ErrorCode.ERR_AmbigCall, "f").WithArguments("C.f(int, double)", "C.f(double, int)")
                );
        }

        [WorkItem(539817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539817")]
        [Fact]
        public void CS0122ERR_BadAccess()
        {
            var text = @"
class Base
{
    private class P { int X; }
}

class Test : Base
{
    void M()
    {
        object o = (P p) => 0;
        int x = P.X;
        int y = (P)null;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,21): error CS0122: 'Base.P' is inaccessible due to its protection level
                //         object o = (P p) => 0;
                Diagnostic(ErrorCode.ERR_BadAccess, "P").WithArguments("Base.P"),
                // (11,20): error CS1660: Cannot convert lambda expression to type 'object' because it is not a delegate type
                //         object o = (P p) => 0;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "(P p) => 0").WithArguments("lambda expression", "object"),
                // (12,17): error CS0122: 'Base.P' is inaccessible due to its protection level
                //         int x = P.X;
                Diagnostic(ErrorCode.ERR_BadAccess, "P").WithArguments("Base.P"),
                // (13,18): error CS0122: 'Base.P' is inaccessible due to its protection level
                //         int y = (P)null;
                Diagnostic(ErrorCode.ERR_BadAccess, "P").WithArguments("Base.P"),
                // (4,27): warning CS0169: The field 'Base.P.X' is never used
                //     private class P { int X; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "X").WithArguments("Base.P.X"));
        }

        [WorkItem(537683, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537683")]
        [Fact]
        public void CS0122ERR_BadAccess02()
        {
            var text = @"public class Outer
{
    private class base1 { }
}

public class MyClass : Outer.base1
{
}
";
            var comp = CreateCompilation(text);
            var type1 = comp.SourceModule.GlobalNamespace.GetMembers("MyClass").Single() as NamedTypeSymbol;
            var b = type1.BaseType();
            var errs = comp.GetDiagnostics();
            Assert.Equal(1, errs.Count());
            Assert.Equal(122, errs.First().Code);
        }

        [WorkItem(539628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539628")]
        [Fact]
        public void CS0122ERR_BadAccess03()
        {
            var text = @"
class C1
{
    private C1() { }
}
class C2
{
    protected C2() { }
    private C2(short x) {}
}

class C3 : C2
{
    C3() : base(3) {}   // CS0122
}

class Test
{
    public static int Main()
    {
        C1 c1 = new C1();   // CS0122
        C2 c2 = new C2();   // CS0122
        return 1;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (14,12): error CS0122: 'C2.C2(short)' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "base").WithArguments("C2.C2(short)"),
                // (21,21): error CS0122: 'C1.C1()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "C1").WithArguments("C1.C1()"),
                // (22,21): error CS0122: 'C2.C2()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "C2").WithArguments("C2.C2()"));
        }

        [WorkItem(539628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539628")]
        [WorkItem(540336, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540336")]
        [Fact]
        public void CS0122ERR_BadAccess04()
        {
            var text = @"
class A
{
    protected class ProtectedClass { }

    public class B : I<ProtectedClass> { }
}
interface I<T> { }

class Error
{
    static void Goo<T>(I<T> i) { }

    static void Main()
    {
        Goo(new A.B());
    }
}
";
            var tree = Parse(text);
            var compilation = CreateCompilation(tree);
            var model = compilation.GetSemanticModel(tree);

            var compilationUnit = tree.GetCompilationUnitRoot();
            var classError = (TypeDeclarationSyntax)compilationUnit.Members[2];
            var mainMethod = (MethodDeclarationSyntax)classError.Members[1];
            var callStmt = (ExpressionStatementSyntax)mainMethod.Body.Statements[0];
            var callExpr = callStmt.Expression;

            var callPosition = callExpr.SpanStart;

            var boundCall = model.GetSpeculativeSymbolInfo(callPosition, callExpr, SpeculativeBindingOption.BindAsExpression);

            Assert.Null(boundCall.Symbol);
            Assert.Equal(1, boundCall.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, boundCall.CandidateReason);

            var constructedMethodSymbol = (IMethodSymbol)(boundCall.CandidateSymbols[0]);
            Assert.Equal("void Error.Goo<A.ProtectedClass>(I<A.ProtectedClass> i)", constructedMethodSymbol.ToTestDisplayString());

            var typeArgSymbol = constructedMethodSymbol.TypeArguments.Single();
            Assert.Equal("A.ProtectedClass", typeArgSymbol.ToTestDisplayString());
            Assert.False(model.IsAccessible(callPosition, typeArgSymbol), "Protected inner class is inaccessible");

            var paramTypeSymbol = constructedMethodSymbol.Parameters.Single().Type;
            Assert.Equal("I<A.ProtectedClass>", paramTypeSymbol.ToTestDisplayString());
            Assert.False(model.IsAccessible(callPosition, typeArgSymbol), "Type should be inaccessible since type argument is inaccessible");

            // The original test attempted to verify that "Error.Goo<A.ProtectedClass>" is an 
            // inaccessible method when inside Error.Main. The C# specification nowhere gives 
            // a special rule for constructed generic methods; the accessibility domain of
            // a method depends only on its declared accessibility and the declared accessibility
            // of its containing type. 
            //
            // We should decide whether the answer to "is this method accessible in Main?" is 
            // yes or no, and if no, change the implementation of IsAccessible accordingly.
            //
            // Assert.False(model.IsAccessible(callPosition, constructedMethodSymbol), "Method should be inaccessible since parameter type is inaccessible");

            compilation.VerifyDiagnostics(
                // (16,9): error CS0122: 'Error.Goo<A.ProtectedClass>(I<A.ProtectedClass>)' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "Goo").WithArguments("Error.Goo<A.ProtectedClass>(I<A.ProtectedClass>)"));
        }

        [WorkItem(539628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539628")]
        [Fact]
        public void CS0122ERR_BadAccess05()
        {
            var text = @"
class Base
{
    private Base() { }
}

class Derived : Base
{
    private Derived() : this(1) { } //fine: can see own private members
    private Derived(int x) : base() { } //CS0122: cannot see base private members
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,30): error CS0122: 'Base.Base()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "base").WithArguments("Base.Base()"));
        }

        [WorkItem(539628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539628")]
        [Fact]
        public void CS0122ERR_BadAccess06()
        {
            var text = @"
class Base
{
    private Base() { } //private, but a match
    public Base(int x) { } //public, but not a match
}

class Derived : Base
{
    private Derived() { } //implicit constructor initializer
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,13): error CS0122: 'Base.Base()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "Derived").WithArguments("Base.Base()"));
        }

        [Fact]
        public void CS0123ERR_MethDelegateMismatch()
        {
            var text = @"
delegate void D();
delegate void D2(int i);

public class C
{
    public static void f(int i) { }

    public static void Main()
    {
        D d = new D(f);   // CS0123
        D2 d2 = new D2(f);   // OK
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_MethDelegateMismatch, Line = 11, Column = 15 } });
        }

        [WorkItem(539909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539909")]
        [Fact]
        public void CS0123ERR_MethDelegateMismatch_01()
        {
            var text = @"
delegate void boo(short x);
class C
{
    static void far<T>(T x) { }
    static void Main(string[] args)
    {
        C p = new C();
        boo goo = null;
        goo += far<int>;// Invalid
        goo += far<short>;// OK
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,16): error CS0123: No overload for 'C.far<int>(int)' matches delegate 'boo'
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "far<int>").WithArguments("C.far<int>(int)", "boo"));
        }

        [WorkItem(539909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539909")]
        [WorkItem(540053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540053")]
        [Fact]
        public void CS0123ERR_MethDelegateMismatch_02()
        {
            var text = @"
delegate void boo(short x);
class C<T>
{
    public static void far(T x) { }
    public static void par<U>(U x) { System.Console.WriteLine(""par""); }
    public static boo goo = null;

}
class D
{
    static void Main(string[] args)
    {
        C<long> p = new C<long>();
        C<long>.goo += C<long>.far;
        C<long>.goo += C<long>.par<byte>;
        C<long>.goo(byte.MaxValue);
        C<long>.goo(long.MaxValue);
        C<short>.goo(long.MaxValue);
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (15,24): error CS0123: No overload for 'C<long>.far(long)' matches delegate 'boo'
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "C<long>.far").WithArguments("C<long>.far(long)", "boo").WithLocation(15, 24),
                // (16,32): error CS0123: No overload for 'par' matches delegate 'boo'
                //         C<long>.goo += C<long>.par<byte>;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "par<byte>").WithArguments("par", "boo").WithLocation(16, 32),
                // (18,21): error CS1503: Argument 1: cannot convert from 'long' to 'short'
                Diagnostic(ErrorCode.ERR_BadArgType, "long.MaxValue").WithArguments("1", "long", "short").WithLocation(18, 21),
                // (19,22): error CS1503: Argument 1: cannot convert from 'long' to 'short'
                Diagnostic(ErrorCode.ERR_BadArgType, "long.MaxValue").WithArguments("1", "long", "short").WithLocation(19, 22)
                );
        }

        [Fact]
        public void CS0123ERR_MethDelegateMismatch_DelegateVariance()
        {
            var text = @"
delegate TOut D<out TOut, in TIn>(TIn p);

class A { }
class B : A { }
class C : B { }

class Test
{
    static void Main()
    {
        D<B, B> d;
        d = F1; //CS0407
        d = F2; //CS0407
        d = F3; //CS0123
        d = F4;
        d = F5;
        d = F6; //CS0123
        d = F7;
        d = F8;
        d = F9; //CS0123
    }

    static A F1(A p) { return null; }
    static A F2(B p) { return null; }
    static A F3(C p) { return null; }

    static B F4(A p) { return null; }
    static B F5(B p) { return null; }
    static B F6(C p) { return null; }

    static C F7(A p) { return null; }
    static C F8(B p) { return null; }
    static C F9(C p) { return null; }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (13,13): error CS0407: 'A Test.F1(A)' has the wrong return type
                Diagnostic(ErrorCode.ERR_BadRetType, "F1").WithArguments("Test.F1(A)", "A"),
                // (14,13): error CS0407: 'A Test.F2(B)' has the wrong return type
                Diagnostic(ErrorCode.ERR_BadRetType, "F2").WithArguments("Test.F2(B)", "A"),
                // (15,13): error CS0123: No overload for 'F3' matches delegate 'D<B, B>'
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "F3").WithArguments("F3", "D<B, B>"),
                // (18,13): error CS0123: No overload for 'F6' matches delegate 'D<B, B>'
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "F6").WithArguments("F6", "D<B, B>"),
                // (21,13): error CS0123: No overload for 'F9' matches delegate 'D<B, B>'
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "F9").WithArguments("F9", "D<B, B>"));
        }

        [Fact]
        public void CS0126ERR_RetObjectRequired()
        {
            var source =
@"namespace N
{
    class C
    {
        object F() { return; }
        X G() { return; }
        C P { get { return; } }
        Y Q { get { return; } }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,9): error CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                //         X G() { return; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(6, 9),
                // (8,9): error CS0246: The type or namespace name 'Y' could not be found (are you missing a using directive or an assembly reference?)
                //         Y Q { get { return; } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Y").WithArguments("Y").WithLocation(8, 9),
                // (5,22): error CS0126: An object of a type convertible to 'object' is required
                //         object F() { return; }
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("object").WithLocation(5, 22),
                // (6,17): error CS0126: An object of a type convertible to 'X' is required
                //         X G() { return; }
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("X").WithLocation(6, 17),
                // (7,21): error CS0126: An object of a type convertible to 'C' is required
                //         C P { get { return; } }
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("N.C").WithLocation(7, 21),
                // (8,21): error CS0126: An object of a type convertible to 'Y' is required
                //         Y Q { get { return; } }
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("Y").WithLocation(8, 21));
        }

        [WorkItem(540115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540115")]
        [Fact]
        public void CS0126ERR_RetObjectRequired_02()
        {
            var source =
@"namespace Test
{
    public delegate object D();

    public class TestClass
    {
        public static int Test(D src)
        {
            src();
            return 0;
        }
    }

    public class MainClass
    {
        public static int Main()
        {
            return TestClass.Test(delegate() { return; });
// The native compiler produces two errors for this code:
//
// CS0126: An object of a type convertible to 'object' is required
//
// CS1662: Cannot convert anonymous method to delegate type 
// 'Test.D' because some of the return types in the block are not implicitly 
// convertible to the delegate return type
//
// This is not great; the first error is right, but does not tell us anything about
// the fact that overload resolution has failed on the first argument. The second
// error is actually incorrect; it is not that 'some of the return types are incorrect',
// it is that some of the returns do not return anything in the first place! There's
// no 'type' to get wrong. 
//
// I would like Roslyn to give two errors:
//
// CS1503: Argument 1: cannot convert from 'anonymous method' to 'Test.D'
// CS0126: An object of a type convertible to 'object' is required
//
// Neal Gafter says: I'd like one error instead of two.  There is an error inside the
// body of the lambda.  It is enough to report that specific error.  This is consistent
// with our design guideline to suppress errors higher in the tree when it is caused
// by an error lower in the tree.
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (18,48): error CS0126: An object of a type convertible to 'object' is required
                //             return TestClass.Test(delegate() { return; });
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("object").WithLocation(18, 48));
        }

        [Fact]
        public void CS0127ERR_RetNoObjectRequired()
        {
            var source =
@"namespace MyNamespace
{
    public class MyClass
    {
        public void F() { return 0; } // CS0127
        public int P
        {
            get { return 0; }
            set { return 0; } // CS0127, set has an implicit void return type
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,27): error CS0127: Since 'MyClass.F()' returns void, a return keyword must not be followed by an object expression
                //         public void F() { return 0; } // CS0127
                Diagnostic(ErrorCode.ERR_RetNoObjectRequired, "return").WithArguments("MyNamespace.MyClass.F()").WithLocation(5, 27),
                // (9,19): error CS0127: Since 'MyClass.P.set' returns void, a return keyword must not be followed by an object expression
                //             set { return 0; } // CS0127, set has an implicit void return type
                Diagnostic(ErrorCode.ERR_RetNoObjectRequired, "return").WithArguments("MyNamespace.MyClass.P.set").WithLocation(9, 19));
        }

        [Fact]
        public void CS0127ERR_RetNoObjectRequired_StaticConstructor()
        {
            string text = @"
class C
{
    static C()
    {
        return 1;
    }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (6,9): error CS0127: Since 'C.C()' returns void, a return keyword must not be followed by an object expression
                Diagnostic(ErrorCode.ERR_RetNoObjectRequired, "return").WithArguments("C.C()"));
        }

        [Fact]
        public void CS0128ERR_LocalDuplicate()
        {
            var text = @"
namespace MyNamespace
{
   public class MyClass
   {
      public static void Main()
      {
         char i = 'a';
         int i = 2;   // CS0128
         if (i == 2) {}
      }
   }
}";
            CreateCompilation(text).
                VerifyDiagnostics(
                    // (9,14): error CS0128: A local variable or function named 'i' is already defined in this scope
                    //          int i = 2;   // CS0128
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "i").WithArguments("i").WithLocation(9, 14),
                    // (9,14): warning CS0219: The variable 'i' is assigned but its value is never used
                    //          int i = 2;   // CS0128
                    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(9, 14)
                );
        }

        [Fact]
        public void CS0131ERR_AssgLvalueExpected01()
        {
            CreateCompilation(
@"class C
{
    int i = 0;
    int P { get; set; }
    int this[int x] { get { return x; } set { } }
    void M()
    {
        ++P = 1; // CS0131
        ++i = 1; // CS0131
        ++this[0] = 1; //CS0131
    }
}
")
            .VerifyDiagnostics(
                // (7,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "++P"),
                // (8,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "++i"),
                // (10,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "++this[0]"));
        }

        [Fact]
        public void CS0131ERR_AssgLvalueExpected02()
        {
            var source =
@"class C
{
    const object NoObject = null;
    static void M()
    {
        const int i = 0;
        i += 1;
        3 *= 1;
        (i + 1) -= 1;
        ""string"" = null;
        null = new object();
        NoObject = ""string"";
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         i += 1;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "i").WithLocation(7, 9),
                // (8,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         3 *= 1;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "3").WithLocation(8, 9),
                // (9,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (i + 1) -= 1;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "i + 1").WithLocation(9, 10),
                // (10,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         "string" = null;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, @"""string""").WithLocation(10, 9),
                // (11,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         null = new object();
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "null").WithLocation(11, 9),
                // (12,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         NoObject = "string";
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "NoObject").WithLocation(12, 9));
        }

        /// <summary>
        /// Breaking change from Dev10. CS0131 is now reported for all value
        /// types, not just struct types. Specifically, CS0131 is now reported
        /// for type parameters constrained to "struct". (See also CS1612.)
        /// </summary>
        [WorkItem(528763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528763")]
        [Fact]
        public void CS0131ERR_AssgLvalueExpected03()
        {
            var source =
@"struct S
{
    public object P { get; set; }
    public object this[object index] { get { return null; } set { } }
}
interface I
{
    object P { get; set; }
    object this[object index] { get; set; }
}
class C
{
    static void M<T>()
        where T : struct, I
    {
        default(S).P = null;
        default(T).P = null; // Dev10: no error
        default(S)[0] = null;
        default(T)[0] = null; // Dev10: no error
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S).P").WithLocation(16, 9),
                // (16,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(T).P").WithLocation(17, 9),
                // (18,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S)[0]").WithLocation(18, 9),
                // (18,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(T)[0]").WithLocation(19, 9));
        }

        [WorkItem(538077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538077")]
        [Fact]
        public void CS0132ERR_StaticConstParam()
        {
            var text = @"
class A
{
  static A(int z)
  {
  }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
               new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_StaticConstParam, Parameters = new string[] { "A.A(int)" } } });
        }

        [Fact]
        public void CS0133ERR_NotConstantExpression01()
        {
            var source =
@"class MyClass
{
   public const int a = b; //no error since b is declared const
   public const int b = c; //CS0133, c is not constant
   public static int c = 1; //change static to const to correct program
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,25): error CS0133: The expression being assigned to 'MyClass.b' must be constant
                //    public const int b = c; //CS0133, c is not constant
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "c").WithArguments("MyClass.b").WithLocation(4, 25));
        }

        [Fact]
        public void CS0133ERR_NotConstantExpression02()
        {
            var source =
@"enum E
{
    X,
    Y = C.F(),
    Z = C.G() + 1,
}
class C
{
    public static E F()
    {
        return E.X;
    }
    public static int G()
    {
        return 0;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,9): error CS0266: Cannot implicitly convert type 'E' to 'int'. An explicit conversion exists (are you missing a cast?)
                //     Y = C.F(),
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "C.F()").WithArguments("E", "int").WithLocation(4, 9),
                // (4,9): error CS0133: The expression being assigned to 'E.Y' must be constant
                //     Y = C.F(),
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "C.F()").WithArguments("E.Y").WithLocation(4, 9),
                // (5,9): error CS0133: The expression being assigned to 'E.Z' must be constant
                //     Z = C.G() + 1,
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "C.G() + 1").WithArguments("E.Z").WithLocation(5, 9));
        }

        [Fact]
        public void CS0133ERR_NotConstantExpression03()
        {
            var source =
@"class C
{
    static void M()
    {
        int y = 1;
        const int x = 2 * y;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,23): error CS0133: The expression being assigned to 'x' must be constant
                //         const int x = 2 * y;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "2 * y").WithArguments("x").WithLocation(6, 23));
        }

        [Fact]
        public void CS0133ERR_NotConstantExpression04()
        {
            var source =
@"class C
{
    static void M()
    {
        const int x = x + x;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,27): error CS0110: The evaluation of the constant value for 'x' involves a circular definition
                //         const int x = x + x;
                Diagnostic(ErrorCode.ERR_CircConstValue, "x").WithArguments("x").WithLocation(5, 27),
                // (5,23): error CS0110: The evaluation of the constant value for 'x' involves a circular definition
                //         const int x = x + x;
                Diagnostic(ErrorCode.ERR_CircConstValue, "x").WithArguments("x").WithLocation(5, 23));
        }

        [Fact]
        public void CS0135ERR_NameIllegallyOverrides()
        {
            // See NameCollisionTests.cs for commentary on this error.

            var text = @"
public class MyClass2
{
   public static int i = 0;

   public static void Main()
   {
      {
         int i = 4; // CS0135: Roslyn reports this error here
         i++;
      }
      i = 0;   // Native compiler reports the error here
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);
        }

        [Fact]
        public void CS0135ERR_NameIllegallyOverrides02()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;

class Test
{
    static int x;
    static void Main()
    {
        int z = x;
        var y = from x in Enumerable.Range(1, 100) // CS1931
                select x;
    }
}").VerifyDiagnostics(
    // (6,16): warning CS0649: Field 'Test.x' is never assigned to, and will always have its default value 0
    //     static int x;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("Test.x", "0").WithLocation(6, 16)
);
        }

        [Fact]
        public void CS0136ERR_LocalIllegallyOverrides01()
        {
            // See comments in NameCollisionTests for thoughts on this error.

            string text =
@"class C
{
    static void M(object x)
    {
        string x = null; // CS0136
        string y = null;
        if (x != null)
        {
            int y = 0; // CS0136
            M(y);
        }
        M(x);
        M(y);
    }
    object P
    {
        get
        {
            int value = 0; // no error
            return value;
        }
        set
        {
            int value = 0; // CS0136
            M(value);
        }
    }
    static void N(int q)
    {
        System.Func<int, int> f = q=>q; // 0136
    }
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (5,16): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         string x = null; // CS0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(5, 16),
                // (9,17): error CS0136: A local or parameter named 'y' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int y = 0; // CS0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y").WithArguments("y").WithLocation(9, 17),
                // (24,17): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int value = 0; // CS0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(24, 17),
                // (30,35): error CS0136: A local or parameter named 'q' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         System.Func<int, int> f = q=>q; // 0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "q").WithArguments("q").WithLocation(30, 35));

            comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (5,16): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         string x = null; // CS0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(5, 16),
                // (9,17): error CS0136: A local or parameter named 'y' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int y = 0; // CS0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y").WithArguments("y").WithLocation(9, 17),
                // (24,17): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int value = 0; // CS0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(24, 17));
        }

        [Fact]
        public void CS0136ERR_LocalIllegallyOverrides02()
        {
            // See comments in NameCollisionTests for commentary on this error.

            CreateCompilation(
@"class C
{
    static void M(object o)
    {
        try
        {
        }
        catch (System.IO.IOException e) 
        {
            M(e);
        }
        catch (System.Exception e) // Legal; the two 'e' variables are in non-overlapping declaration spaces
        {
            M(e);
        }
        try
        {
        }
        catch (System.Exception o) // CS0136: Illegal; the two 'o' variables are in overlapping declaration spaces.
        {
            M(o);
        }
    }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "o").WithArguments("o").WithLocation(19, 33));
        }

        [Fact]
        public void CS0136ERR_LocalIllegallyOverrides03()
        {
            // See comments in NameCollisionTests for commentary on this error.

            CreateCompilation(
@"class C
{
    int field = 0;
    int property { get; set; }

    static public void Main()
    {
        int[] ints = new int[] { 1, 2, 3 };
        string[] strings = new string[] { ""1"", ""2"", ""3"" };
        int conflict = 1;
        System.Console.WriteLine(conflict);
        foreach (int field in ints) { }          // Legal: local hides field but name is used consistently
        foreach (string property in strings) { } // Legal: local hides property but name is used consistently
        foreach (string conflict in strings) { } // 0136: local hides another local in an enclosing local declaration space.
    }
}
")
                .VerifyDiagnostics(
                // (14,25): error CS0136: A local or parameter named 'conflict' cannot be declared in this 
                //          scope because that name is used in an enclosing local scope to define a local or parameter
                // foreach (string conflict in strings) { } // 0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "conflict").WithArguments("conflict").WithLocation(14, 25),
                // (3,9): warning CS0414: The field 'C.field' is assigned but its value is never used
                //     int field = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "field").WithArguments("C.field"));
        }

        [WorkItem(538045, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538045")]
        [Fact]
        public void CS0139ERR_NoBreakOrCont()
        {
            var text = @"
namespace x
{
    public class a
    {
        public static void Main(bool b)
        {
            if (b)
                continue;  // CS0139
            else
                break;     // CS0139
        }
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_NoBreakOrCont, Line = 9, Column = 17 },
                                         new ErrorDescription { Code = (int)ErrorCode.ERR_NoBreakOrCont, Line = 11, Column = 17 }});
        }

        [WorkItem(542400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542400")]
        [Fact]
        public void CS0140ERR_DuplicateLabel()
        {
            var text = @"
namespace MyNamespace
{
   public class MyClass
   {
      public static void Main()
      {
         label1: int i = M();
         label1: int j = M();   // CS0140, comment this line to resolve
         goto label1;
      }
      static int M() { return 0; }
   }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (9,10): error CS0140: The label 'label1' is a duplicate
                //          label1: int j = M();   // CS0140, comment this line to resolve
                Diagnostic(ErrorCode.ERR_DuplicateLabel, "label1").WithArguments("label1").WithLocation(9, 10)
            );
        }

        [WorkItem(542420, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542420")]
        [Fact]
        public void ErrorMeansSuccess_Attribute()
        {
            var text = @"
using A;
using B;
using System;

namespace A
{
    class var { }
    class XAttribute : Attribute { }
}
namespace B
{
    class var { }
    class XAttribute : Attribute { }
    class X : Attribute { }
}
class Xyzzy
{
    [X] // 17.2 If an attribute class is found both with and without this suffix, an ambiguity is present and a compile-time error occurs.
    public static void Main(string[] args)
    {
    }
    static int M() { return 0; }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();
        }

        [WorkItem(542420, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542420")]
        [Fact]
        public void ErrorMeansSuccess_var()
        {
            var text = @"
using A;
using B;

namespace A
{
    class var { }
}
namespace B
{
    class var { }
}
class Xyzzy
{
    public static void Main(string[] args)
    {
        var x = M(); // 8.5.1 When the local-variable-type is specified as var and no type named var is in scope, ...
    }
    static int M() { return 0; }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (21,9): error CS0104: 'var' is an ambiguous reference between 'A.var' and 'B.var'
                //         var x = M(); // 8.5.1 When the local-variable-type is specified as var and no type named var is in scope, ...
                Diagnostic(ErrorCode.ERR_AmbigContext, "var").WithArguments("var", "A.var", "B.var")
            );
        }

        [Fact]
        public void CS0144ERR_NoNewAbstract()
        {
            var text = @"
interface ii
{
}

abstract class aa
{
}

public class a
{
   public static void Main()
   {
      ii xx = new ii();   // CS0144
      ii yy = new ii(Error);   // CS0144, CS0103
      aa zz = new aa();   // CS0144
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new ErrorDescription[]
                {
                    new ErrorDescription { Code = (int)ErrorCode.ERR_NoNewAbstract, Line = 14, Column = 15 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_NoNewAbstract, Line = 15, Column = 15 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_NoNewAbstract, Line = 16, Column = 15 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_NameNotInContext, Line = 15, Column = 22 }
                });
        }

        [WorkItem(539583, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539583")]
        [Fact]
        public void CS0150ERR_ConstantExpected()
        {
            var test = @"
class C
{
    static void Main()
    {
        byte x = 1;
        int[] a1 = new int[x];
        int[] a2 = new int[x] { 1 }; //CS0150

        const sbyte y = 1;
        const short z = 2;
        int[] b1 = new int[y + z];
        int[] b2 = new int[y + z] { 1, 2, 3 };
    }
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (8,28): error CS0150: A constant value is expected
                Diagnostic(ErrorCode.ERR_ConstantExpected, "x"));
        }

        [Fact()]
        public void CS0151ERR_IntegralTypeValueExpected()
        {
            var text = @"
public class iii
{
   public static implicit operator int (iii aa)
   {
      return 0;
   }

   public static implicit operator long (iii aa)
   {
      return 0;
   }

   public static void Main()
   {
      iii a = new iii();

      switch (a)   // CS0151, compiler cannot choose between int and long
      {
         case 1:
            break;
      }
   }
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular6);
            comp.VerifyDiagnostics(
                // (18,15): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type in C# 6 and earlier.
                //       switch (a)   // CS0151, compiler cannot choose between int and long
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a").WithLocation(18, 15),
                // (20,15): error CS0029: Cannot implicitly convert type 'int' to 'iii'
                //          case 1:
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "iii").WithLocation(20, 15)
                );
        }

        [Fact]
        public void CS0152ERR_DuplicateCaseLabel()
        {
            var text = @"
namespace x
{
   public class a
   {
      public static void Main()
      {
         int i = 0;

         switch (i)
         {
            case 1:
               i++;
               return;

            case 1:   // CS0152, two case 1 statements
               i++;
               return;
         }
      }
   }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (16,13): error CS0152: The switch statement contains multiple cases with the label value '1'
                //             case 1:   // CS0152, two case 1 statements
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 1:").WithArguments("1").WithLocation(16, 13)
                );
        }

        [Fact]
        public void CS0153ERR_InvalidGotoCase()
        {
            var text = @"
public class a
{
   public static void Main()
   {
      goto case 5;   // CS0153
   }
}";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (6,7): error CS0153: A goto case is only valid inside a switch statement
                //       goto case 5;   // CS0153
                Diagnostic(ErrorCode.ERR_InvalidGotoCase, "goto case 5;").WithLocation(6, 7));
        }

        [Fact]
        public void CS0153ERR_InvalidGotoCase_2()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        string Fruit = ""Apple"";
        switch (Fruit)
        {
            case ""Banana"":
                break;
            default:
                break;
        }
        goto default;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (14,9): error CS0153: A goto case is only valid inside a switch statement
                //         goto default;
                Diagnostic(ErrorCode.ERR_InvalidGotoCase, "goto default;").WithLocation(14, 9));
        }

        [Fact]
        public void CS0154ERR_PropertyLacksGet01()
        {
            CreateCompilation(
@"class C
{
    static object P { set { } }
    static int Q { set { } }
    static void M(object o)
    {
        C.P = null;
        o = C.P; // CS0154
        M(P); // CS0154
        ++C.Q; // CS0154
    }
}
")
            .VerifyDiagnostics(
                // (8,13): error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "C.P").WithArguments("C.P"),
                // (9,11): error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("C.P"),
                // (10,11): error CS0154: The property or indexer 'C.Q' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "C.Q").WithArguments("C.Q"));
        }

        [Fact]
        public void CS0154ERR_PropertyLacksGet02()
        {
            var source =
@"class A
{
    public virtual A P { get; set; }
    public object Q { set { } }
}
class B : A
{
    public override A P { set { } }
    void M()
    {
        M(Q); // CS0154, no get method
    }
    static void M(B b)
    {
        object o = b.P; // no error
        o = b.Q; // CS0154, no get method
        b.P.Q = null; // no error
        o = b.P.Q; // CS0154, no get method
    }
    static void M(object o) { }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,11): error CS0154: The property or indexer 'A.Q' cannot be used in this context because it lacks the get accessor
                //         M(Q); // CS0154, no get method
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Q").WithArguments("A.Q").WithLocation(11, 11),
                // (16,13): error CS0154: The property or indexer 'A.Q' cannot be used in this context because it lacks the get accessor
                //         o = b.Q; // CS0154, no get method
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "b.Q").WithArguments("A.Q").WithLocation(16, 13),
                // (18,13): error CS0154: The property or indexer 'A.Q' cannot be used in this context because it lacks the get accessor
                //         o = b.P.Q; // CS0154, no get method
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "b.P.Q").WithArguments("A.Q").WithLocation(18, 13));
        }

        [Fact]
        public void CS0154ERR_PropertyLacksGet03()
        {
            var source =
@"class C
{
    int P { set { } }
    void M()
    {
        P += 1;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,9): error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor
                //         P += 1;
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("C.P").WithLocation(6, 9));
        }

        [Fact]
        public void CS0154ERR_PropertyLacksGet04()
        {
            var source =
@"class C
{
    object p;
    object P { set { p = P; } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,26): error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor
                //     object P { set { p = P; } }
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("C.P").WithLocation(4, 26));
        }

        [Fact]
        public void CS0154ERR_PropertyLacksGet05()
        {
            CreateCompilation(
@"class C
{
    object P { set { } }
    static bool Q { set { } }
    void M()
    {
        object o = P as string;
        o = P ?? Q;
        o = (o != null) ? P : Q;
        o = !Q;
    }
}")
            .VerifyDiagnostics(
                // (7,20): error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("C.P").WithLocation(7, 20),
                // (8,13): error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("C.P").WithLocation(8, 13),
                // (8,18): error CS0154: The property or indexer 'C.Q' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Q").WithArguments("C.Q").WithLocation(8, 18),
                // (9,27): error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("C.P").WithLocation(9, 27),
                // (9,31): error CS0154: The property or indexer 'C.Q' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Q").WithArguments("C.Q").WithLocation(9, 31),
                // (10,14): error CS0154: The property or indexer 'C.Q' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Q").WithArguments("C.Q").WithLocation(10, 14));
        }

        [Fact]
        public void CS0154ERR_PropertyLacksGet06()
        {
            CreateCompilation(
@"class C
{
    int this[int x] { set { } }
    void M(int b)
    {
        b = this[0];
        b = 1 + this[1];
        M(this[2]);
        this[3]++;
        this[4] += 1;
    }
}")
            .VerifyDiagnostics(
                // (6,13): error CS0154: The property or indexer 'C.this[int]' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "this[0]").WithArguments("C.this[int]"),
                // (7,17): error CS0154: The property or indexer 'C.this[int]' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "this[1]").WithArguments("C.this[int]"),
                // (8,11): error CS0154: The property or indexer 'C.this[int]' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "this[2]").WithArguments("C.this[int]"),
                // (9,9): error CS0154: The property or indexer 'C.this[int]' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "this[3]").WithArguments("C.this[int]"),
                // (10,9): error CS0154: The property or indexer 'C.this[int]' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "this[4]").WithArguments("C.this[int]"));
        }

        [Fact]
        public void CS0154ERR_PropertyLacksGet07()
        {
            var source1 =
@"public class A
{
    public virtual object P { private get { return null; } set { } }
}
public class B : A
{
    public override object P { set { } }
}";
            var compilation1 = CreateCompilation(source1);
            compilation1.VerifyDiagnostics();
            var compilationVerifier = CompileAndVerify(compilation1);
            var reference1 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);
            var source2 =
@"class C
{
    static void M(B b)
    {
        var o = b.P;
        b.P = o;
    }
}";
            var compilation2 = CreateCompilation(source2, references: new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (5,17): error CS0154: The property or indexer 'B.P' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "b.P").WithArguments("B.P").WithLocation(5, 17));
        }

        [Fact]
        public void CS0155ERR_BadExceptionType()
        {
            var text =
@"interface IA { }
interface IB : IA { }
struct S { }
class C
{
    static void M()
    {
        try { }
        catch (object) { }
        catch (System.Exception) { }
        catch (System.DateTime) { }
        catch (System.Int32) { }
        catch (IA) { }
        catch (IB) { }
        catch (S) { }
        catch (S) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadExceptionType, "object").WithLocation(9, 16),
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "System.Exception").WithArguments("object").WithLocation(10, 16),
                Diagnostic(ErrorCode.ERR_BadExceptionType, "System.DateTime").WithLocation(11, 16),
                Diagnostic(ErrorCode.ERR_BadExceptionType, "System.Int32").WithLocation(12, 16),
                Diagnostic(ErrorCode.ERR_BadExceptionType, "IA").WithLocation(13, 16),
                Diagnostic(ErrorCode.ERR_BadExceptionType, "IB").WithLocation(14, 16),
                Diagnostic(ErrorCode.ERR_BadExceptionType, "S").WithLocation(15, 16),
                Diagnostic(ErrorCode.ERR_BadExceptionType, "S").WithLocation(16, 16));
        }

        [Fact]
        public void CS0155ERR_BadExceptionType_Null()
        {
            var text = @"class C
{
    static readonly bool False = false;
    const string T = null;

    static void M(object o) 
    {
        const string s = null;
        if (False) throw null;
        if (False) throw (string)null; //CS0155
        if (False) throw s; //CS0155
        if (False) throw T; //CS0155
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,26): error CS0029: Cannot implicitly convert type 'string' to 'System.Exception'
                //         if (False) throw (string)null; //CS0155
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(string)null").WithArguments("string", "System.Exception").WithLocation(10, 26),
                // (11,26): error CS0029: Cannot implicitly convert type 'string' to 'System.Exception'
                //         if (False) throw s; //CS0155
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("string", "System.Exception").WithLocation(11, 26),
                // (12,26): error CS0029: Cannot implicitly convert type 'string' to 'System.Exception'
                //         if (False) throw T; //CS0155
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "T").WithArguments("string", "System.Exception").WithLocation(12, 26)
                );
        }

        [Fact]
        public void CS0155ERR_BadExceptionType_FailingAs()
        {
            var text = @"
class C
{
    static readonly bool False = false;

    static void M(object o) 
    {
        if (False) throw new C() as D; //CS0155, though always null
    }
}

class D : C { }
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,26): error CS0029: Cannot implicitly convert type 'D' to 'System.Exception'
                //         if (False) throw new C() as D; //CS0155, though always null
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C() as D").WithArguments("D", "System.Exception").WithLocation(8, 26)
                );
        }

        [Fact]
        public void CS0155ERR_BadExceptionType_TypeParameters()
        {
            var text = @"using System;
class C
{
    static readonly bool False = false;

    static void M<T, TC, TS, TE>(object o) 
        where TC : class
        where TS : struct
        where TE : Exception, new()
    {
        if (False) throw default(T); //CS0155
        if (False) throw default(TC); //CS0155
        if (False) throw default(TS); //CS0155
        if (False) throw default(TE);
        if (False) throw new TE();
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,26): error CS0029: Cannot implicitly convert type 'T' to 'System.Exception'
                //         if (False) throw default(T); //CS0155
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "default(T)").WithArguments("T", "System.Exception").WithLocation(11, 26),
                // (12,26): error CS0029: Cannot implicitly convert type 'TC' to 'System.Exception'
                //         if (False) throw default(TC); //CS0155
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "default(TC)").WithArguments("TC", "System.Exception").WithLocation(12, 26),
                // (13,26): error CS0029: Cannot implicitly convert type 'TS' to 'System.Exception'
                //         if (False) throw default(TS); //CS0155
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "default(TS)").WithArguments("TS", "System.Exception").WithLocation(13, 26)
                );
        }

        [Fact()]
        public void CS0155ERR_BadExceptionType_UserDefinedConversions()
        {
            var text = @"using System;
class C
{
    static readonly bool False = false;

    static void M(object o) 
    {
        if (False) throw new Implicit(); //CS0155
        if (False) throw new Explicit(); //CS0155
        if (False) throw (Exception)new Implicit();
        if (False) throw (Exception)new Explicit();
    }
}

class Implicit
{
    public static implicit operator Exception(Implicit i)
    {
        return null;
    }
}

class Explicit
{
    public static explicit operator Exception(Explicit i)
    {
        return null;
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (8,20): error CS0155: The type caught or thrown must be derived from System.Exception
                Diagnostic(ErrorCode.ERR_BadExceptionType, "new Implicit()"),
                // (8,20): error CS0155: The type caught or thrown must be derived from System.Exception
                Diagnostic(ErrorCode.ERR_BadExceptionType, "new Explicit()")
                );
            CreateCompilation(text).VerifyDiagnostics(
                    // (9,26): error CS0266: Cannot implicitly convert type 'Explicit' to 'System.Exception'. An explicit conversion exists (are you missing a cast?)
                    //         if (False) throw new Explicit(); //CS0155
                    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new Explicit()").WithArguments("Explicit", "System.Exception").WithLocation(9, 26)
                );
        }

        [Fact]
        public void CS0155ERR_BadExceptionType_Dynamic()
        {
            var text = @"
class C
{
    static readonly bool False = false;

    static void M(object o) 
    {
        dynamic d = null;
        if (False) throw d; //CS0155
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (9,26): error CS0155: The type caught or thrown must be derived from System.Exception
                Diagnostic(ErrorCode.ERR_BadExceptionType, "d"));
            CreateCompilation(text).VerifyDiagnostics(); // dynamic conversion to Exception
        }

        [WorkItem(542995, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542995")]
        [Fact]
        public void CS0155ERR_BadExceptionType_Struct()
        {
            var text = @"
public class Test
{
    public static void Main(string[] args)
    {
    }
    private void Method()
    {
        try
        {
        }
        catch (s1 s)
        {
        }
    }
}
struct s1
{ }
";
            CreateCompilation(text).VerifyDiagnostics(
                // (12,16): error CS0155: The type caught or thrown must be derived from System.Exception
                //         catch (s1 s)
                Diagnostic(ErrorCode.ERR_BadExceptionType, "s1"),
                // (12,19): warning CS0168: The variable 's' is declared but never used
                //         catch (s1 s)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "s").WithArguments("s")
                );
        }

        [Fact]
        public void CS0156ERR_BadEmptyThrow()
        {
            var text = @"
using System;

namespace x
{
   public class b : Exception
   {
   }

   public class a
   {
      public static void Main()
      {
         try
         {
            throw;   // CS0156
         }

         catch(b)
         {
            throw;   // this throw is valid
         }
      }
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadEmptyThrow, Line = 16, Column = 13 } });
        }

        [Fact]
        public void CS0156ERR_BadEmptyThrow_Nesting()
        {
            var text = @"
class C
{
    void M()
    {
        bool b = System.DateTime.Now.Second > 1; //avoid unreachable code
        if (b) throw; //CS0156
        try
        {
            if (b) throw; //CS0156
            try
            {
                if (b) throw; //CS0156
            }
            catch
            {
                if (b) throw; //fine
            }
            finally
            {
                if (b) throw; //CS0156
            }
        }
        catch
        {
            if (b) throw; //fine
            try
            {
                if (b) throw; //fine
            }
            catch
            {
                if (b) throw; //fine
            }
            finally
            {
                if (b) throw; //CS0724

                try
                {
                    if (b) throw; //CS0724
                }
                catch
                {
                    if (b) throw; //fine
                }
                finally
                {
                    if (b) throw; //CS0724
                }
            }
        }
        finally
        {
            if (b) throw; //CS0156
            try
            {
                if (b) throw; //CS0156
            }
            catch
            {
                if (b) throw; //fine
            }
            finally
            {
                if (b) throw; //CS0156
            }
        }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,9): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (9,13): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (12,17): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (20,17): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (36,17): error CS0724: A throw statement with no arguments is not allowed in a finally clause that is nested inside the nearest enclosing catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrowInFinally, "throw"),
                // (36,17): error CS0724: A throw statement with no arguments is not allowed in a finally clause that is nested inside the nearest enclosing catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrowInFinally, "throw"),
                // (36,17): error CS0724: A throw statement with no arguments is not allowed in a finally clause that is nested inside the nearest enclosing catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrowInFinally, "throw"),
                // (41,13): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (44,17): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (52,17): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"));
        }

        [Fact]
        public void CS0156ERR_BadEmptyThrow_Lambdas()
        {
            var text = @"
class C
{
    void M()
    {
        bool b = System.DateTime.Now.Second > 1; // avoid unreachable code
        System.Action a;
        a = () => { throw; }; //CS0156
        try
        {
            a = () =>
            {
                if (b) throw; //CS0156
                try
                {
                    if (b) throw; //CS0156
                }
                catch
                {
                    if (b) throw; //fine
                }
                finally
                {
                    if (b) throw; //CS0156
                }
            };
        }
        catch
        {
            a = () =>
            {
                if (b) throw; //CS0156
                try
                {
                    if (b) throw; //CS0156
                }
                catch
                {
                    if (b) throw; //fine
                }
                finally
                {
                    if (b) throw; //CS0156
                }
            };
        }
        finally
        {
            a = () =>
            {
                if (b) throw; //CS0156
                try
                {
                    if (b) throw; //CS0156
                }
                catch
                {
                    if (b) throw; //fine
                }
                finally
                {
                    if (b) throw; //CS0156
                }
            };
        }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,21): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (13,24): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (16,28): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (24,28): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (32,24): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (35,28): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (43,28): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (51,24): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (54,28): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"),
                // (62,28): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw"));
        }

        [WorkItem(540817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540817")]
        [Fact]
        public void CS0157ERR_BadFinallyLeave01()
        {
            var text =
@"class C
{
    static int F;
    static void M()
    {
        if (F == 0)
            goto Before;
        else if (F == 1)
            goto After;
    Before:
        ;
        try
        {
            if (F == 0)
                goto Before;
            else if (F == 1)
                goto After;
            else if (F == 2)
                goto TryBlock;
            else if (F == 3)
                return;
        TryBlock:
            ;
        }
        catch (System.Exception)
        {
            if (F == 0)
                goto Before;
            else if (F == 1)
                goto After;
            else if (F == 2)
                goto CatchBlock;
            else if (F == 3)
                return;
        CatchBlock:
            ;
        }
        finally
        {
            if (F == 0)
                goto Before;
            else if (F == 1)
                goto After;
            else if (F == 2)
                goto FinallyBlock;
            else if (F == 3)
                return;
        FinallyBlock:
            ;
        }
    After:
        ;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (41,17): error CS0157: Control cannot leave the body of a finally clause
                //                 goto Before;
                Diagnostic(ErrorCode.ERR_BadFinallyLeave, "goto"),
                // (43,17): error CS0157: Control cannot leave the body of a finally clause
                //                 goto After;
                Diagnostic(ErrorCode.ERR_BadFinallyLeave, "goto"),
                // (47,17): error CS0157: Control cannot leave the body of a finally clause
                //                 return;
                Diagnostic(ErrorCode.ERR_BadFinallyLeave, "return"),
                // (3,16): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 0
                //     static int F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "0")
                );
        }

        [WorkItem(540817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540817")]
        [Fact]
        public void CS0157ERR_BadFinallyLeave02()
        {
            var text =
@"using System;
class C
{
    static void F(int i)
    {
    }
    static void M()
    {
        for (int i = 0; i < 10;)
        {
            if (i < 5)
            {
                try { F(i); }
                catch (Exception) { continue; }
                finally { break; }
            }
            else
            {
                try { F(i); }
                catch (Exception) { break; }
                finally { continue; }
            }
        }
    }
}";
            CreateCompilation(text).
                VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_BadFinallyLeave, "break").WithLocation(15, 27),
                    Diagnostic(ErrorCode.ERR_BadFinallyLeave, "continue").WithLocation(21, 27));
        }

        [WorkItem(540817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540817")]
        [Fact]
        public void CS0157ERR_BadFinallyLeave03()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        int i = 0;
        try { i = 1; }
        catch { i = 2; }
        finally { i = 3; goto lab1; }// invalid
    lab1:
        return;
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                // (9,26): error CS0157: Control cannot leave the body of a finally clause
                //         finally { i = 3; goto lab1; }// invalid
                Diagnostic(ErrorCode.ERR_BadFinallyLeave, "goto"),
                // (6,13): warning CS0219: The variable 'i' is assigned but its value is never used
                //         int i = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i")
                );
        }

        [WorkItem(539890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539890")]
        [Fact]
        public void CS0158ERR_LabelShadow()
        {
            var text = @"
namespace MyNamespace
{
   public class MyClass
   {
      public static void Main()
      {
         goto lab1;
         lab1:
         {
            lab1:
            goto lab1;   // CS0158
         }
      }
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_LabelShadow, Line = 11, Column = 13 } });
        }

        [WorkItem(539890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539890")]
        [Fact]
        public void CS0158ERR_LabelShadow_02()
        {
            var text = @"
delegate int del(int i);
class C
{
    static void Main(string[] args)
    {
        del p = x =>
        {
            goto label1;
        label1:        // invalid
            return x * x;
        };
    label1:
        return;
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                // (10,9): error CS0158: The label 'label1' shadows another label by the same name in a contained scope
                //         label1:        // invalid
                Diagnostic(ErrorCode.ERR_LabelShadow, "label1").WithArguments("label1"),
                // (13,5): warning CS0164: This label has not been referenced
                //     label1:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label1")
                );
        }

        [WorkItem(539875, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539875")]
        [Fact]
        public void CS0159ERR_LabelNotFound()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        goto Label2;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_LabelNotFound, Line = 6, Column = 14 }
                });
        }

        [WorkItem(528799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528799")]
        [Fact()]
        public void CS0159ERR_LabelNotFound_2()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        int s = 23;
        switch (s)
        {
            case 21:
                break;
            case 23:
                goto default;
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (12,17): error CS0159: No such label 'default:' within the scope of the goto statement
                //                 goto default;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto default;").WithArguments("default:"),
                // (11,13): error CS8070: Control cannot fall out of switch from final case label ('case 23:')
                //             case 23:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 23:").WithArguments("case 23:"));
        }

        [WorkItem(539876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539876")]
        [Fact]
        public void CS0159ERR_LabelNotFound_3()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        goto Label;
    }
    public static void Goo()
    {
    Label:
        ;
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_LabelNotFound, "Label").WithArguments("Label"),
                                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "Label"));
        }

        [WorkItem(539876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539876")]
        [Fact]
        public void CS0159ERR_LabelNotFound_4()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 10; i++)
        {
        Label:
            i++;
        }
        goto Label;
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_LabelNotFound, "Label").WithArguments("Label"),
                                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "Label"));
        }

        [WorkItem(539876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539876")]
        [Fact]
        public void CS0159ERR_LabelNotFound_5()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        if (true)
        {
        Label1:
            goto Label2;
        }
        else
        {
        Label2:
            goto Label1;
        }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_LabelNotFound, "Label2").WithArguments("Label2"),
                                     Diagnostic(ErrorCode.ERR_LabelNotFound, "Label1").WithArguments("Label1"),
                                    Diagnostic(ErrorCode.WRN_UnreachableCode, "Label2"),
                                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "Label1"),
                                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "Label2"));
        }

        [WorkItem(539876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539876")]
        [Fact]
        public void CS0159ERR_LabelNotFound_6()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        { goto L; }
        { L: return; }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_LabelNotFound, "L").WithArguments("L"),
                                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L"));
        }

        [WorkItem(539876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539876")]
        [Fact]
        public void CS0159ERR_LabelNotFound_7()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 3;
        if (true)
        {
        label1:
            goto label3;
            if (!false)
            {
            label2:
                goto label5;
                if (i > 2)
                {
                label3:
                    goto label2;
                    if (i == 3)
                    {
                    label4:
                        if (i < 5)
                        {
                        label5:
                                if (i == 4)
                                {
                                }
                                else
                                {
                                    System.Console.WriteLine(""a"");
                                }
                        }
                    }
                }
            }
        }
    }
}

";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_LabelNotFound, "label3").WithArguments("label3"),
                                Diagnostic(ErrorCode.ERR_LabelNotFound, "label5").WithArguments("label5"),
                                Diagnostic(ErrorCode.WRN_UnreachableCode, "if"),
                                Diagnostic(ErrorCode.WRN_UnreachableCode, "label4"),
                                Diagnostic(ErrorCode.WRN_UnreachableCode, "label5"),
                                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label1"),
                                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label3"),
                                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label4"),
                                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label5"));
        }

        [WorkItem(540818, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540818")]
        [Fact]
        public void CS0159ERR_LabelNotFound_8()
        {
            var text = @"
delegate int del(int i);
class C
{
    static void Main(string[] args)
    {
        del q = x =>
            {
                goto label2; // invalid
                return x * x;
            };
    label2:
        return;
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                // (10,17): warning CS0162: Unreachable code detected
                //                 return x * x;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"),
                // (9,17): error CS0159: No such label 'label2' within the scope of the goto statement
                //                 goto label2; // invalid
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("label2"),
                // (12,5): warning CS0164: This label has not been referenced
                //     label2:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label2")
                    );
        }

        [WorkItem(539876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539876")]
        [Fact]
        public void CS0159ERR_LabelNotFound_9()
        {
            var text = @"
public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            goto innerLoop;
            foreach (char y in x)
            {
            innerLoop:
                return;
            }
        }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_LabelNotFound, "innerLoop").WithArguments("innerLoop"),
                                        Diagnostic(ErrorCode.WRN_UnreferencedLabel, "innerLoop"));
        }

        [Fact]
        public void CS0160ERR_UnreachableCatch()
        {
            var text =
@"using System;
using System.IO;
class A : Exception { }
class B : A { }
class C : IOException { }
interface I { }
class D : Exception, I { }
class E : IOException, I { }
class F<T> : Exception { }
class Program
{
    static void M()
    {
        try { }
        catch (A) { }
        catch (D) { }
        catch (E) { }
        catch (IOException) { }
        catch (C) { }
        catch (F<bool>) { }
        catch (F<int>) { }
        catch (Exception) { }
        catch (B) { }
        catch (StackOverflowException) { }
        catch (F<int>) { }
        catch (F<float>) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "C").WithArguments("System.IO.IOException").WithLocation(19, 16),
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "B").WithArguments("A").WithLocation(23, 16),
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "StackOverflowException").WithArguments("System.Exception").WithLocation(24, 16),
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "F<int>").WithArguments("F<int>").WithLocation(25, 16),
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "F<float>").WithArguments("System.Exception").WithLocation(26, 16));
        }

        [Fact]
        public void CS0160ERR_UnreachableCatch_Filter1()
        {
            var text = @"
using System;
class A : Exception { }
class B : A { }

class Program
{
    static void M()
    {
        int a = 1;
        try { }
        catch when (a == 1) { }
        catch (Exception e) when (e.Message == null) { }
        catch (A) { }
        catch (B e) when (e.Message == null) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (15,16): error CS0160: A previous catch clause already catches all exceptions of this or of a super type ('A')
                //         catch (B e) when (e.Message == null) { }
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "B").WithArguments("A").WithLocation(15, 16));
        }

        [Fact]
        public void CS8359WRN_FilterIsConstantFalse_NonBoolean()
        {
            // Non-boolean constant filters are not considered for WRN_FilterIsConstant warnings. 

            var text = @"
using System;
class A : Exception { }
class B : A { }

class Program
{
    static void M()
    {
        try { }
        catch (A) when (1) { }
        catch (B) when (0) { }
        catch (B) when (""false"") { }
        catch (B) when (false) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,25): error CS0029: Cannot implicitly convert type 'int' to 'bool'
                //         catch (A) when (1) { }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "bool").WithLocation(11, 25),
                // (12,25): error CS0029: Cannot implicitly convert type 'int' to 'bool'
                //         catch (B) when (0) { }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "0").WithArguments("int", "bool").WithLocation(12, 25),
                // (13,25): error CS0029: Cannot implicitly convert type 'string' to 'bool'
                //         catch (B) when ("false") { }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""false""").WithArguments("string", "bool").WithLocation(13, 25),
                // (14,25): warning CS8359: Filter expression is a constant 'false', consider removing the catch clause
                //         catch (B) when (false) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalse, "false").WithLocation(14, 25));
        }

        [Fact]
        public void CS8359WRN_FilterIsConstantFalse1()
        {
            var text = @"
using System;
class A : Exception { }
class B : A { }

class Program
{
    static void M()
    {
        try { }
        catch (A) when (false) { }
        catch (B) when (false) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,25): warning CS8359: Filter expression is a constant 'false', consider removing the catch clause
                //         catch (A) when (false) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalse, "false").WithLocation(11, 25),
                // (12,25): warning CS8359: Filter expression is a constant 'false', consider removing the catch clause
                //         catch (B) when (false) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalse, "false").WithLocation(12, 25));
        }

        [Fact]
        public void CS8359WRN_FilterIsConstantFalse2()
        {
            var text = @"
using System;
class A : Exception { }
class B : A { }

class Program
{
    static void M()
    {
        try { }
        catch (A) when ((1+1)!=2) { }
        catch (B) when (false) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,25): warning CS8359: Filter expression is a constant 'false', consider removing the catch clause
                //         catch (A) when (false) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalse, "(1+1)!=2").WithLocation(11, 25),
                // (12,25): warning CS8359: Filter expression is a constant 'false', consider removing the catch clause
                //         catch (B) when (false) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalse, "false").WithLocation(12, 25));
        }

        [Fact]
        public void CS8359WRN_FilterIsConstantFalse3()
        {
            var text = @"
using System;
class A : Exception { }

class Program
{
    static void M()
    {
        try { }
        catch (A) when (false) { }
        finally { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,25): warning CS8359: Filter expression is a constant 'false', consider removing the catch clause
                //         catch (A) when (false) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalse, "false").WithLocation(10, 25));
        }

        [Fact]
        public void CS8360WRN_FilterIsConstantRedundantTryCatch1()
        {
            var text = @"
using System;
class A : Exception { }

class Program
{
    static void M()
    {
        try { }
        catch (A) when (false) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,25): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //         catch (A) when (false) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "false").WithLocation(10, 25));
        }

        [Fact]
        public void CS8360WRN_FilterIsConstantRedundantTryCatch2()
        {
            var text = @"
using System;
class A : Exception { }

class Program
{
    static void M()
    {
        try { }
        catch (A) when ((1+1)!=2) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,25): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //         catch (A) when (false) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "(1+1)!=2").WithLocation(10, 25));
        }

        [Fact]
        public void CS7095WRN_FilterIsConstantTrue1()
        {
            var text = @"
using System;
class A : Exception { }
class B : A { }

class Program
{
    static void M()
    {
        try { }
        catch (A) when (true) { }
        catch (B) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,25): warning CS7095: Filter expression is a constant 'true', consider removing the filter
                //         catch (A) when (true) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantTrue, "true").WithLocation(11, 25));
        }

        [Fact]
        public void CS7095WRN_FilterIsConstantTrue2()
        {
            var text = @"
using System;
class A : Exception { }

class Program
{
    static void M()
    {
        try { }
        catch when (true) { }
        catch (A) { }
        catch when (false) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,19): warning CS7095: Filter expression is a constant 'true', consider removing the filter
                //         catch when (true) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantTrue, "true").WithLocation(10, 21),
                // (12,19): warning CS8359: Filter expression is a constant 'false', consider removing the catch clause
                //         catch when (false) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalse, "false").WithLocation(12, 21));
        }

        [Fact]
        public void CS7095WRN_FilterIsConstantTrue3()
        {
            var text = @"
using System;
class A : Exception { }

class Program
{
    static void M()
    {
        try { }
        catch when ((1+1)==2) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,19): warning CS7095: Filter expression is a constant 'true', consider removing the filter
                //         catch when (true) { }
                Diagnostic(ErrorCode.WRN_FilterIsConstantTrue, "(1+1)==2").WithLocation(10, 21));
        }

        [Fact]
        public void CS0162WRN_UnreachableCode_Filter_ConstantCondition()
        {
            var text = @"
using System;
class A : Exception { }
class B : A { }

class Program
{
    static void M()
    {
        try { }
        catch (A) when (false) 
        {
            Console.WriteLine(1); 
        }
        catch (B) { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
    // (11,25): warning CS8359: Filter expression is a constant 'false', consider removing the catch clause
    //         catch (A) when (false) 
    Diagnostic(ErrorCode.WRN_FilterIsConstantFalse, "false").WithLocation(11, 25),
    // (13,13): warning CS0162: Unreachable code detected
    //             Console.WriteLine(1); 
    Diagnostic(ErrorCode.WRN_UnreachableCode, "Console").WithLocation(13, 13)
                );
        }

        [Fact]
        public void CS0162WRN_UnreachableCode_Filter_ConstantCondition2()
        {
            var text = @"
using System;

class Program
{
    static void M()
    {
        int x;
        try { }
        catch (Exception) when (false) 
        {
            Console.WriteLine(x);
        }
    }
}
";
            // Unlike an unreachable code in if statement block we don't allow using
            // a variable that's not definitely assigned. The reason why we allow it in an if statement
            // is to make conditional compilation easier. Such scenario doesn't apply to filters.

            CreateCompilation(text).VerifyDiagnostics(
    // (10,33): warning CS7105: Filter expression is a constant 'false', consider removing the try-catch block
    //         catch (Exception) when (false) 
    Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "false").WithLocation(10, 33),
    // (12,13): warning CS0162: Unreachable code detected
    //             Console.WriteLine(x);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "Console").WithLocation(12, 13)
                );
        }

        [Fact]
        public void CS0162WRN_UnreachableCode_Filter_ConstantCondition3()
        {
            var text = @"
using System;

class Program
{
    static void M()
    {
        int x;
        try { }
        catch (Exception) when (true) 
        {
            Console.WriteLine(x);
        }
    }
}
";
            // Unlike an unreachable code in if statement block we don't allow using
            // a variable that's not definitely assigned. The reason why we allow it in an if statement
            // is to make conditional compilation easier. Such scenario doesn't apply to filters.

            CreateCompilation(text).VerifyDiagnostics(
    // (10,33): warning CS7095: Filter expression is a constant 'true', consider removing the filter
    //         catch (Exception) when (true) 
    Diagnostic(ErrorCode.WRN_FilterIsConstantTrue, "true").WithLocation(10, 33),
    // (12,31): error CS0165: Use of unassigned local variable 'x'
    //             Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(12, 31)
                );
        }

        [Fact]
        public void CS0160ERR_UnreachableCatch_Dynamic()
        {
            string source = @"
using System;

public class EG<T> : Exception { }

public class A
{
    public void M1()
    {
        try
        {
            Goo();
        }
        catch (EG<object>)
        {
        }
        catch (EG<dynamic>)
        {
        }
    }

    void Goo() { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (17,16): error CS0160: A previous catch clause already catches all exceptions of this or of a super type ('EG<object>')
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "EG<dynamic>").WithArguments("EG<object>"));
        }

        [Fact]
        public void CS0160ERR_UnreachableCatch_TypeParameter()
        {
            string source = @"
using System;

public class EA : Exception { }
public class EB : EA { }

public class A<T> where T : EB
{
    public void M1()
    {
        try
        {
            Goo();
        }
        catch (EA)
        {
        }
        catch (T)
        {
        }
    }

    void Goo() { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (18,16): error CS0160: A previous catch clause already catches all exceptions of this or of a super type ('EA')
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "T").WithArguments("EA"));
        }

        [Fact]
        public void CS0160ERR_UnreachableCatch_TypeParameter_Dynamic1()
        {
            string source = @"
using System;

public class EG<T> : Exception { }

public abstract class A<T>
{
    public abstract void M<U>() where U : T;
}

public class B<V> : A<EG<dynamic>> where V : EG<object>
{
    public override void M<U>()
    {
        try
        {
            Goo();
        }
        catch (EG<dynamic>)
        {
        }
        catch (V)
        {
        }
        catch (U)
        {
        }
    }

    void Goo() { } 
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (22,16): error CS0160: A previous catch clause already catches all exceptions of this or of a super type ('EG<dynamic>')
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "V").WithArguments("EG<dynamic>"),
                // (25,16): error CS0160: A previous catch clause already catches all exceptions of this or of a super type ('EG<dynamic>')
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "U").WithArguments("EG<dynamic>"));
        }

        [Fact]
        public void TypeParameter_DynamicConversions()
        {
            string source = @"
using System;

public class EG<T> : Exception { }

public abstract class A<T>
{
    public abstract void M<U>() where U : T;
}

public class B<V> : A<EG<dynamic>> where V : EG<object>
{
    public override void M<U>()
    {
        V v = default(V);
        U u = default(U);

        // implicit
        EG<dynamic> egd = v;
        // implicit
        egd = u;

        //explicit
        v = (V)egd;
        //explicit
        u = (U)egd;

        //implicit array
        V[] va = null;        
        EG<dynamic>[] egda = va;

        // explicit array
        va = (V[])egda;      
    }

    void Goo() { } 
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }


        [Fact]
        public void CS0160ERR_UnreachableCatch_TypeParameter_Dynamic2()
        {
            string source = @"
using System;

public class EG<T> : Exception { }

public abstract class A<T>
{
    public abstract void M<U>() where U : T;
}

public class B<V> : A<EG<dynamic>> where V : EG<object>
{
    public override void M<U>()
    {
        try
        {
            Goo();
        }
        catch (EG<object>)
        {
        }
        catch (V)
        {
        }
        catch (U)
        {
        }
    }

    void Goo() { } 
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (22,16): error CS0160: A previous catch clause already catches all exceptions of this or of a super type ('EG<object>')
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "V").WithArguments("EG<object>"),
                // (25,16): error CS0160: A previous catch clause already catches all exceptions of this or of a super type ('EG<object>')
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "U").WithArguments("EG<object>"));
        }

        [Fact]
        public void CS0161ERR_ReturnExpected()
        {
            var text = @"
public class Test
{
   public static int Main() // CS0161
   {
      int i = 10;
      if (i < 10)
      {
         return i;
      }
      else
      {
         // uncomment the following line to resolve
         // return 1;
      }
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_ReturnExpected, Line = 4, Column = 22 } });
        }

        [Fact]
        public void CS0163ERR_SwitchFallThrough()
        {
            var text = @"
public class MyClass
{
   public static void Main()
   {
      int i = 0;

      switch (i)   // CS0163
      {
         case 1:
            i++;
            // uncomment one of the following lines to resolve
            // return;
            // break;
            // goto case 3;

         case 2:
            i++;
            return;

         case 3:
            i = 0;
            return;
      }
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_SwitchFallThrough, Line = 10, Column = 10 } });
        }

        [Fact]
        public void CS0165ERR_UseDefViolation01()
        {
            var text = @"
class MyClass
{
   public int i;
}

class MyClass2
{
   public static void Main(string [] args)
   {
      int i, j;
      if (args[0] == ""test"")
      {
         i = 0;
      }

      /*
      // to resolve, either initialize the variables when declared
      // or provide for logic to initialize them, as follows:
      else
      {
         i = 1;
      }
      */

      j = i;   // CS0165, i might be uninitialized

      MyClass myClass;
      myClass.i = 0;   // CS0165
      // use new as follows
      // MyClass myClass = new MyClass();
      // myClass.i = 0;
      i = j;
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_UseDefViolation, Line = 26, Column = 11 } ,
                new ErrorDescription { Code = (int)ErrorCode.ERR_UseDefViolation, Line = 29, Column = 7 }});
        }

        [Fact]
        public void CS0165ERR_UseDefViolation02()
        {
            CreateCompilation(
@"class C
{
    static void M(int m)
    {
        int v;
        for (int i = 0; i < m; ++i)
        {
            v = 0;
        }
        M(v);
        int w;
        for (; ; )
        {
            w = 0;
            break;
        }
        M(w);
        for (int x; x < 1; ++x)
        {
        }
        for (int y; m < 1; ++y)
        {
        }
        for (int z; ; )
        {
            M(z);
        }
    }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "v").WithArguments("v").WithLocation(10, 11),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(18, 21),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(21, 30),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(26, 15));
        }

        [Fact]
        public void CS0165ERR_UseDefViolation03()
        {
            CreateCompilation(
@"class C
{
    static int F()
    {
        return 0;
    }
    static void M0()
    {
        int a, b, c, d;
        for (a = 1; (b = F()) < 2; c = 3) { d = 4; }
        if (a == 0) { }
        if (b == 0) { }
        if (c == 0) { } // Use of unassigned 'c'
        if (d == 0) { } // Use of unassigned 'd'
    }
    static void M1()
    {
        int x, y;
        for (x = 0; (y = x) < 10; ) { }
        if (y == 0) { } // no error
    }
    static void M2()
    {
        int x, y;
        for (x = 0; x < 10; y = x) { }
        if (y == 0) { } // Use of unassigned 'y'
    }
    static void M3()
    {
        int x, y;
        for (x = 0; x < 10; ) { y = x; }
        if (y == 0) { } // Use of unassigned 'y'
    }
    static void M4()
    {
        int x, y;
        for (y = x; (x = 0) < 10; ) { } // Use of unassigned 'x'
        if (y == 0) { } // no error
    }
    static void M5()
    {
        int x, y;
        for (; (x = 0) < 10; y = x) { }
        if (y == 0) { } // Use of unassigned 'y'
    }
    static void M6()
    {
        int x, y;
        for (; (x = 0) < 10; ) { y = x; }
        if (y == 0) { } // Use of unassigned 'y'
    }
    static void M7()
    {
        int x, y;
        for (y = x; F() < 10; x = 0) { } // Use of unassigned 'x'
        if (y == 0) { } // no error
    }
    static void M8()
    {
        int x, y;
        for (; (y = x) < 10; x = 0) { } // Use of unassigned 'x'
        if (y == 0) { } // no error
    }
    static void M9()
    {
        int x, y;
        for (; F() < 10; x = 0) { y = x; } // Use of unassigned 'x'
        if (y == 0) { } // no error
    }
    static void M10()
    {
        int x, y;
        for (y = x; F() < 10; ) { x = 0; } // Use of unassigned 'x'
        if (y == 0) { } // no error
    }
    static void M11()
    {
        int x, y;
        for (; F() < 10; y = x) { x = 0; }
        if (y == 0) { } // Use of unassigned 'y'
    }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(13, 13),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "d").WithArguments("d").WithLocation(14, 13),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(26, 13),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(32, 13),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(37, 18),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(44, 13),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(50, 13),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(55, 18),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(61, 21),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(67, 39),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(68, 13),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(73, 18),
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(80, 13));
        }

        [Fact]
        public void CS0165ERR_UseDefViolation04()
        {
            CreateCompilation(
@"class C
{
    static int M()
    {
        int x, y, z;
        try
        {
            x = 0;
            y = 1;
        }
        catch (System.Exception)
        {
            x = 1;
        }
        finally
        {
            z = 1;
        }
        return (x + y + z);
    }
}")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(19, 21));
        }

        [Fact]
        public void CS0165ERR_UseDefViolation05()
        {
            // This is a "negative" test case; we should *not* be producing a "use of unassigned
            // local variable" error here. In an earlier revision we were doing so because we were
            // losing the information about the first argument being "out" when the bad call node
            // was created. Later flow analysis then did not know that "x" need not be assigned
            // before it was used, and we'd produce a wrong error.
            CreateCompilation(
@"class C
{
    static int N(out int q) { q = 1; return 2;}
    static void M()
    {
        int x = N(out x, 123);
    }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_BadArgCount, "N").WithArguments("N", "2"));
        }

        [WorkItem(540860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540860")]
        [Fact]
        public void CS0165ERR_UseDefViolation06()
        {
            // Should not generate "unassigned local variable" for struct.
            CreateCompilation(
@"struct S
{
    public void M() { }
}
class C
{
    void M()
    {
        S s;
        s.M();
    }
}")
                .VerifyDiagnostics();
        }

        [Fact]
        public void CS0165ERR_UseDefViolation07()
        {
            // Make sure flow analysis is hooked up for indexers
            CreateCompilation(
@"struct S
{
    public int this[int x] { get { return 0; } }
}
class C
{
    public int this[int x] { get { return 0; } }
}
class Test
{
    static void Main()
    {
        int unassigned1;
        int unassigned2;
        int sink;

        C c;
        sink = c[1]; //CS0165

        c = new C();
        sink = c[1]; //fine
        sink = c[unassigned1]; //CS0165

        S s;
        sink = s[1]; //fine - struct with no fields

        s = new S();
        sink = s[1]; //fine
        sink = s[unassigned2]; //CS0165
    }
}")
                .VerifyDiagnostics(
                    // (18,16): error CS0165: Use of unassigned local variable 'c'
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c"),
                    // (22,18): error CS0165: Use of unassigned local variable 'unassigned1'
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "unassigned1").WithArguments("unassigned1"),
                    // (29,18): error CS0165: Use of unassigned local variable 'unassigned2'
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "unassigned2").WithArguments("unassigned2"));
        }

        [WorkItem(3402, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CS0170ERR_UseDefViolationField()
        {
            var text = @"
public struct error
{
   public int i;
}

public class MyClass
{
   public static void Main()
   {
      error e;
      // uncomment the next line to resolve this error
      // e.i = 0;
      System.Console.WriteLine( e.i );   // CS0170 because 
                                         //e.i was never assigned
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_UseDefViolationField, Line = 14, Column = 33 } });
        }

        [Fact]
        public void CS0171ERR_UnassignedThis()
        {
            var text = @"
struct MyStruct
{
   MyStruct(int initField)   // CS0171
   {
      // i = initField;      // uncomment this line to resolve this error
   }
   public int i;
}

class MyClass
{
   public static void Main()
   {
      MyStruct aStruct = new MyStruct();
   }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,4): error CS0171: Field 'MyStruct.i' must be fully assigned before control is returned to the caller
                //    MyStruct(int initField)   // CS0171
                Diagnostic(ErrorCode.ERR_UnassignedThis, "MyStruct").WithArguments("MyStruct.i"),
                // (15,16): warning CS0219: The variable 'aStruct' is assigned but its value is never used
                //       MyStruct aStruct = new MyStruct();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "aStruct").WithArguments("aStruct"),
                // (8,15): warning CS0649: Field 'MyStruct.i' is never assigned to, and will always have its default value 0
                //    public int i;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("MyStruct.i", "0")
                );
        }

        [Fact]
        public void FieldAssignedInReferencedConstructor()
        {
            var text =
@"struct S
{
    private readonly object _x;
    S(object o)
    {
        _x = o;
    }
    S(object x, object y) : this(x ?? y)
    {
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(); // No CS0171 for S._x
        }

        [Fact()]
        public void CS0172ERR_AmbigQM()
        {
            var text = @"
public class Square
{
   public class Circle
   {
      public static implicit operator Circle(Square aa)
      {
         return null;
      }

      public static implicit operator Square(Circle aa)
      {
         return null;
      }
   }

   public static void Main()
   {
      Circle aa = new Circle();
      Square ii = new Square();
      object o = (1 == 1) ? aa : ii;   // CS0172
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AmbigQM, Line = 21, Column = 18 } });
        }

        [Fact]
        public void CS0173ERR_InvalidQM()
        {
            var text = @"
public class C {}
public class A {}

public class MyClass
{
   public static void F(bool b)
   {
      A a = new A();
      C c = new C();
      object o = b ? a : c;  // CS0173
   }

   public static void Main()
   {
       F(true);
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_InvalidQM, Line = 11, Column = 18 } });
        }

        [WorkItem(528331, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528331")]
        [Fact]
        public void CS0173ERR_InvalidQM_FuncCall()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        var s = true ? System.Console.WriteLine(0) : System.Console.WriteLine(1);
    }
}";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "s = true ? System.Console.WriteLine(0) : System.Console.WriteLine(1)").WithArguments("void").WithLocation(6, 13));
        }

        [Fact]
        public void CS0173ERR_InvalidQM_GeneralType()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        A<string> a = new A<string>();
        A<int> b = new A<int>();
        System.Console.WriteLine(1 > 2 ? a : b);	// Invalid, Can't implicit convert 
    }
}
class A<T>
{
}";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_InvalidQM, "1 > 2 ? a : b").WithArguments("A<string>", "A<int>").WithLocation(8, 34));
        }

        [WorkItem(540902, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540902")]
        [Fact]
        public void CS0173ERR_InvalidQM_foreach()
        {
            var text = @"
public class Test
{
    public static void Main()
    {
        S[] x = null;
        foreach (S x in true ? x : 1) // Dev10: CS0173 ONLY
        { }
        C[] y= null;
        foreach (C c in false ? 1 : y) // Dev10: CS0173 ONLY
        { }
    }
}
struct S { }
class C { }
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,25): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'S[]' and 'int'
                //         foreach (S x in true ? x : 1) // Dev10: CS0173 ONLY
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? x : 1").WithArguments("S[]", "int"),
                // (7,20): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         foreach (S x in true ? x : 1) // Dev10: CS0173 ONLY
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x"),
                // (11,25): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'int' and 'C[]'
                //         foreach (C c in false ? 1 : y) // Dev10: CS0173 ONLY
                Diagnostic(ErrorCode.ERR_InvalidQM, "false ? 1 : y").WithArguments("int", "C[]")
                );
        }

        //      /// Scenarios? 
        //        [Fact]
        //        public void CS0174ERR_NoBaseClass()
        //        {
        //            var text = @"
        //                         ";
        //            CreateCompilationWithMscorlib(text).VerifyDiagnostics(Diagnostic(ErrorCode.ERR_NoBaseClass, "?"));
        //        }

        [WorkItem(543360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543360")]
        [Fact()]
        public void CS0175ERR_BaseIllegal()
        {
            var text = @"
using System;
class Base
{
    public int TestInt = 0;
}

class MyClass : Base
{
    public void BaseTest()
    {
        Console.WriteLine(base); // CS0175
        base = 9;   // CS0175
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,27): error CS0175: Use of keyword 'base' is not valid in this context
                Diagnostic(ErrorCode.ERR_BaseIllegal, "base").WithLocation(12, 27),
                // (12,9): error CS0175: Use of keyword 'base' is not valid in this context
                Diagnostic(ErrorCode.ERR_BaseIllegal, "base").WithLocation(13, 9)
            );
        }

        [WorkItem(528624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528624")]
        [Fact()]
        public void CS0175ERR_BaseIllegal_02()
        {
            var text = @"
using System.Collections.Generic;

class MyClass : List<int>
{
    public void BaseTest()
    {
        var x = from i in base select i;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,27): error CS0175: Use of keyword 'base' is not valid in this context
                //         var x = from i in base select i;
                Diagnostic(ErrorCode.ERR_BaseIllegal, "base")
            );
        }

        [Fact]
        public void CS0176ERR_ObjectProhibited01()
        {
            var source = @"
class A
{
    class B
    {
        static void Method() { }
        void M()
        {
            this.Method();
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,13): error CS0176: Member 'A.B.Method()' cannot be accessed with an instance reference; qualify it with a type name instead
                //             this.Method();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.Method").WithArguments("A.B.Method()").WithLocation(9, 13)
                );
        }

        [Fact]
        public void CS0176ERR_ObjectProhibited02()
        {
            var source = @"
class C
{
    static object field;
    static object Property { get; set; }
    void M(C c)
    {
        Property = field; // no error
        C.Property = C.field; // no error
        this.field = this.Property;
        c.Property = c.field;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
    // (9,9): error CS0176: Member 'C.field' cannot be accessed with an instance reference; qualify it with a type name instead
    //         this.field = this.Property;
    Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.field").WithArguments("C.field"),
    // (9,22): error CS0176: Member 'C.Property' cannot be accessed with an instance reference; qualify it with a type name instead
    //         this.field = this.Property;
    Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.Property").WithArguments("C.Property"),
    // (10,9): error CS0176: Member 'C.Property' cannot be accessed with an instance reference; qualify it with a type name instead
    //         c.Property = c.field;
    Diagnostic(ErrorCode.ERR_ObjectProhibited, "c.Property").WithArguments("C.Property"),
    // (10,22): error CS0176: Member 'C.field' cannot be accessed with an instance reference; qualify it with a type name instead
    //         c.Property = c.field;
    Diagnostic(ErrorCode.ERR_ObjectProhibited, "c.field").WithArguments("C.field")
                                    );
        }

        [Fact]
        public void CS0176ERR_ObjectProhibited03()
        {
            var source =
@"class A
{
    internal static object F;
}
class B<T> where T : A
{
    static void M(T t)
    {
        object q = t.F;
        t.ReferenceEquals(q, null);
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,20): error CS0176: Member 'A.F' cannot be accessed with an instance reference; qualify it with a type name instead
                //         object q = t.F;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "t.F").WithArguments("A.F").WithLocation(9, 20),
                // (10,9): error CS0176: Member 'object.ReferenceEquals(object, object)' cannot be accessed with an instance reference; qualify it with a type name instead
                //         t.ReferenceEquals(q, null);
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "t.ReferenceEquals").WithArguments("object.ReferenceEquals(object, object)").WithLocation(10, 9),
                // (3,28): warning CS0649: Field 'A.F' is never assigned to, and will always have its default value null
                //     internal static object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("A.F", "null").WithLocation(3, 28));
        }

        [WorkItem(543361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543361")]
        [Fact]
        public void CS0176ERR_ObjectProhibited04()
        {
            var source = @"
public delegate void D();
class Test
{
    public event D D;

    public void TestIdenticalEventName()
    {
        D.CreateDelegate(null, null, null); // CS0176
    }
}
";
            CreateCompilation(source, targetFramework: TargetFramework.Mscorlib45).VerifyDiagnostics(
                // (9,9): error CS0176: Member 'Delegate.CreateDelegate(Type, object, string)' cannot be accessed with an instance reference; qualify it with a type name instead
                //         D.CreateDelegate(null, null, null); // CS0176
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "D.CreateDelegate").WithArguments("System.Delegate.CreateDelegate(System.Type, object, string)").WithLocation(9, 9)
                );
        }

        // Identical to CS0176ERR_ObjectProhibited04, but with event keyword removed (i.e. field instead of field-like event).
        [WorkItem(543361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543361")]
        [Fact]
        public void CS0176ERR_ObjectProhibited05()
        {
            var source = @"
public delegate void D();
class Test
{
    public D D;

    public void TestIdenticalEventName()
    {
        D.CreateDelegate(null, null, null); // CS0176
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,14): warning CS0649: Field 'Test.D' is never assigned to, and will always have its default value null
                //     public D D;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "D").WithArguments("Test.D", "null")
            );
        }

        [Fact]
        public void CS0177ERR_ParamUnassigned01()
        {
            var text =
@"class C
{
    static void M(out int x, out int y, out int z)
    {
        try
        {
            x = 0;
            y = 1;
        }
        catch (System.Exception)
        {
            x = 1;
        }
        finally
        {
            z = 1;
        }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "M").WithArguments("y").WithLocation(3, 17));
        }

        [WorkItem(528243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528243")]
        [Fact]
        public void CS0177ERR_ParamUnassigned02()
        {
            var text =
@"class C
{
    static bool P { get { return false; } }
    static object M(out object x)
    {
        if (P)
        {
            object o = P ? M(out x) : null;
            return o;
        }
        return P ? null : M(out x);
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //             return o;
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return o;").WithArguments("x").WithLocation(9, 13),
                // (11,9): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //         return P ? null : M(out x);
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return P ? null : M(out x);").WithArguments("x").WithLocation(11, 9));
        }

        [Fact]
        public void CS0185ERR_LockNeedsReference()
        {
            var text = @"
public class MainClass
{
    public static void Main ()
    {
        lock (1)   // CS0185
        // try the following lines instead
        // MainClass x = new MainClass();
        // lock(x)
        {
        }
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_LockNeedsReference, Line = 6, Column = 15 } });
        }

        [Fact]
        public void CS0186ERR_NullNotValid()
        {
            var text = @"
using System.Collections;

class MyClass
{
    static void Main()
    {
        // Each of the following lines generates CS0186:
        foreach (int i in null) { }   // CS0186
        foreach (int i in (IEnumerable)null) { };   // CS0186
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_NullNotValid, Line = 9, Column = 27 } ,
                                            new ErrorDescription { Code = (int)ErrorCode.ERR_NullNotValid, Line = 10, Column = 27 }});
        }

        [Fact]
        public void CS0186ERR_NullNotValid02()
        {
            var text = @"
public class Test
{
    public static void Main(string[] args)
    {
        foreach (var x in default(int[])) { }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_NullNotValid, "default(int[])"));
        }

        [WorkItem(540983, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540983")]
        [Fact]
        public void CS0188ERR_UseDefViolationThis()
        {
            var text = @"
namespace MyNamespace
{
    class MyClass
    {
        struct S
        {
            public int a;

            void Goo()
            {
            }

            S(int i)
            {
                // a = i;
                Goo();  // CS0188
            }
        }
        public static void Main()
        { }

    }
}";
            CreateCompilation(text).
                VerifyDiagnostics(
                // (17,17): error CS0188: The 'this' object cannot be used before all of its fields are assigned to
                //                 Goo();  // CS0188
                Diagnostic(ErrorCode.ERR_UseDefViolationThis, "Goo").WithArguments("this"),
                // (8,24): warning CS0649: Field 'MyNamespace.MyClass.S.a' is never assigned to, and will always have its default value 0
                //             public int a;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "a").WithArguments("MyNamespace.MyClass.S.a", "0"));
        }

        [Fact, WorkItem(579533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579533"), WorkItem(864605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864605")]
        public void CS0188ERR_UseDefViolationThis_MethodGroupInIsOperator_ImplicitReceiver()
        {
            string source = @"
using System;
 
struct S
{
	int value;

    public S(int v)
    {
        var b1 = F is Action;
        value = v;
    }
 
    void F()
    {
        
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (10,18): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "F is Action").WithLocation(10, 18),
                // (10,18): error CS0188: The 'this' object cannot be used before all of its fields are assigned to
                Diagnostic(ErrorCode.ERR_UseDefViolationThis, "F").WithArguments("this"));
        }

        [Fact, WorkItem(579533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579533"), WorkItem(864605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864605")]
        public void CS0188ERR_UseDefViolationThis_MethodGroupInIsOperator_ExplicitReceiver()
        {
            string source = @"
using System;
 
struct S
{
	int value;

    public S(int v)
    {
        var b1 = this.F is Action;
        value = v;
    }
 
    void F()
    {
        
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (10,18): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         var b1 = this.F is Action;
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "this.F is Action").WithLocation(10, 18),
                // (10,18): error CS0188: The 'this' object cannot be used before all of its fields are assigned to
                //         var b1 = this.F is Action;
                Diagnostic(ErrorCode.ERR_UseDefViolationThis, "this").WithArguments("this"));
        }

        [Fact, WorkItem(579533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579533")]
        public void CS0188ERR_UseDefViolationThis_ImplicitReceiverInDynamic()
        {
            string source = @"
using System;
 
struct S
{
    dynamic value;
 
    public S(dynamic d)
    {
        /*this.*/ Add(d);
        throw new NotImplementedException();
    }
 
    void Add(int value)
    {
        this.value += value;
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (10,19): error CS0188: The 'this' object cannot be used before all of its fields are assigned to
                Diagnostic(ErrorCode.ERR_UseDefViolationThis, "Add").WithArguments("this"));
        }

        [Fact, WorkItem(579533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579533")]
        public void CS0188ERR_UseDefViolationThis_ExplicitReceiverInDynamic()
        {
            string source = @"
using System;
 
struct S
{
    dynamic value;
 
    public S(dynamic d)
    {
        this.Add(d);
        throw new NotImplementedException();
    }
 
    void Add(int value)
    {
        this.value += value;
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (10,9): error CS0188: The 'this' object cannot be used before all of its fields are assigned to
                Diagnostic(ErrorCode.ERR_UseDefViolationThis, "this").WithArguments("this"));
        }

        [Fact]
        public void CS0190ERR_ArgsInvalid()
        {
            string source = @"
using System;
public class C
{
  static void M(__arglist)
  {
    ArgIterator ai = new ArgIterator(__arglist);    
  }
  static void Main()
  {
    M(__arglist);
  }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Mscorlib45);
            comp.VerifyDiagnostics(
                // (11,7): error CS0190: The __arglist construct is valid only within a variable argument method
                //     M(__arglist);
                Diagnostic(ErrorCode.ERR_ArgsInvalid, "__arglist")
                );
        }

        [Fact]
        public void CS4013ERR_SpecialByRefInLambda01()
        {
            // Note that the native compiler does *not* produce an error when you illegally
            // use __arglist inside a lambda, oddly enough. Roslyn does.

            string source = @"
using System;
using System.Linq;
public class C
{
  delegate int D(RuntimeArgumentHandle r);
  static void M(__arglist)
  {
    D f = null;
    f = x=>f(__arglist);
    f = delegate { return f(__arglist); };
    var q = from x in new int[10] select f(__arglist);
  }
  static void Main()
  {
  }
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (10,14): error CS4013: Instance of type 'System.RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
                //     f = x=>f(__arglist);
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle"),
                // (11,29): error CS4013: Instance of type 'System.RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
                //     f = delegate { return f(__arglist); };
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle"),
                // (12,44): error CS4013: Instance of type 'System.RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
                //     var q = from x in new int[10] select f(__arglist);
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle")
                );
        }

        [Fact]
        public void CS4013ERR_SpecialByRefInLambda02()
        {
            string source = @"
using System;
public class C
{
  static void M(__arglist)
  {
    RuntimeArgumentHandle h = __arglist;
    Action action = ()=> 
    { 
      RuntimeArgumentHandle h2 = h; // Bad use of h
      ArgIterator args1 = new ArgIterator(h); // Bad use of h
      RuntimeArgumentHandle h3 = h2; // no error; does not create field
      ArgIterator args2 = new ArgIterator(h2); // no error; does not create field
    };
  }
  static void Main()
  {
  }
}";
            CreateCompilationWithMscorlib45(source).VerifyEmitDiagnostics(
                // (10,34): error CS4013: Instance of type 'System.RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
                //       RuntimeArgumentHandle h2 = h; // Bad use of h
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "h").WithArguments("System.RuntimeArgumentHandle"),
                // (11,43): error CS4013: Instance of type 'System.RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
                //       ArgIterator args1 = new ArgIterator(h); // Bad use of h
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "h").WithArguments("System.RuntimeArgumentHandle"));
        }

        [Fact]
        public void CS4013ERR_SpecialByRefInLambda03()
        {
            string source = @"
using System;
using System.Collections.Generic;
public class C
{
  static void N(RuntimeArgumentHandle x) {}
  static IEnumerable<int> M(RuntimeArgumentHandle h1) // Error: hoisted to field
  {
    N(h1);
    yield return 1;
    RuntimeArgumentHandle h2 = default(RuntimeArgumentHandle);
    yield return 2;
    N(h2); // Error: hoisted to field
    yield return 3;
    RuntimeArgumentHandle h3 = default(RuntimeArgumentHandle);
    N(h3); // No error; we don't need to hoist this one to a field
  }
  static void Main()
  {
  }
}";

            CreateCompilation(source).Emit(new System.IO.MemoryStream()).Diagnostics
                .Verify(
                // (7,51): error CS4013: Instance of type 'System.RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
                //   static IEnumerable<int> M(RuntimeArgumentHandle h1) // Error: hoisted to field
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "h1").WithArguments("System.RuntimeArgumentHandle"),
                // (13,7): error CS4013: Instance of type 'System.RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
                //     N(h2); // Error: hoisted to field
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "h2").WithArguments("System.RuntimeArgumentHandle")
                );
        }

        [Fact, WorkItem(538008, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538008")]
        public void CS0191ERR_AssgReadonly()
        {
            var source =
@"class MyClass
{
    public readonly int TestInt = 6;  // OK to assign to readonly field in declaration

    public MyClass()
    {
        TestInt = 11; // OK to assign to readonly field in constructor
        TestInt = 12; // OK to assign to readonly field multiple times in constructor
        this.TestInt = 13; // OK to assign with explicit this receiver
        MyClass t = this;
        t.TestInt = 14; // CS0191 - we can't be sure that the receiver is this
    }

    public void TestReadOnly()
    {
        TestInt = 19;                  // CS0191
    }

    public static void Main()
    {
    }
}

class MyDerived : MyClass
{
    MyDerived()
    {
        TestInt = 15; // CS0191 - not in declaring class
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (28,9): error CS0191: A readonly field cannot be assigned to (except in the constructor of the class in which the field is defined or a variable initializer))
                //         TestInt = 15; // CS0191 - not in declaring class
                Diagnostic(ErrorCode.ERR_AssgReadonly, "TestInt").WithLocation(28, 9),
                // (11,9): error CS0191: A readonly field cannot be assigned to (except in the constructor of the class in which the field is defined or a variable initializer))
                //         t.TestInt = 14; // CS0191 - we can't be sure that the receiver is this
                Diagnostic(ErrorCode.ERR_AssgReadonly, "t.TestInt").WithLocation(11, 9),
                // (16,9): error CS0191: A readonly field cannot be assigned to (except in the constructor of the class in which the field is defined or a variable initializer))
                //         TestInt = 19;                  // CS0191
                Diagnostic(ErrorCode.ERR_AssgReadonly, "TestInt").WithLocation(16, 9));
        }

        [WorkItem(538009, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538009")]
        [Fact]
        public void CS0192ERR_RefReadonly()
        {
            var text = @"
    class MyClass
{
    public readonly int TestInt = 6;
    static void TestMethod(ref int testInt)
    {
        testInt = 0;
    }

    MyClass()
    {
        TestMethod(ref TestInt);   // OK
    }

    public void PassReadOnlyRef()
    {
        TestMethod(ref TestInt);   // CS0192
    }

    public static void Main()
    {
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_RefReadonly, Line = 17, Column = 24 } });
        }

        [Fact]
        public void CS0193ERR_PtrExpected()
        {
            var text = @"
using System;

public struct Age
{
   public int AgeYears;
   public int AgeMonths;
   public int AgeDays;
}

public class MyClass
{
   public static void SetAge(ref Age anAge, int years, int months, int days)
   {
      anAge->Months = 3;   // CS0193, anAge is not a pointer
      // try the following line instead
      // anAge.AgeMonths = 3;
   }

   public static void Main()
   {
      Age MyAge = new Age();
      Console.WriteLine(MyAge.AgeMonths);
      SetAge(ref MyAge, 22, 4, 15);
      Console.WriteLine(MyAge.AgeMonths);
   }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_PtrExpected, Line = 15, Column = 7 } });
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void CS0196ERR_PtrIndexSingle()
        {
            var text = @"
unsafe public class MyClass
{
   public static void Main ()
   {
      int *i = null;
      int j = 0;
      j = i[1,2];   // CS0196
      // try the following line instead
      // j = i[1];
   }
}";
            var compilation = CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,11): error CS0196: A pointer must be indexed by only one value
                //       j = i[1,2];   // CS0196
                Diagnostic(ErrorCode.ERR_PtrIndexSingle, "i[1,2]"));


            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().First();

            Assert.Equal("i[1,2]", node.ToString());

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'i[1,2]')
  Children(2):
      ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32*, IsInvalid) (Syntax: 'i')
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'i[1,2]')
        Children(2):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
");
        }

        [Fact]
        public void CS0198ERR_AssgReadonlyStatic()
        {
            var text = @"
public class MyClass
{
   public static readonly int TestInt = 6;

   static MyClass()
   {
      TestInt = 7;
      TestInt = 8;
      MyClass.TestInt = 7;
   }

   public MyClass()
   {
      TestInt = 11;   // CS0198, constructor is not static and readonly field is
   }

   private void InstanceMethod()
   {
       TestInt = 12;   // CS0198
   }

   private void StaticMethod()
   {
       TestInt = 13;   // CS0198
   }

   public static void Main()
   {
   }
}

class MyDerived : MyClass
{
    static MyDerived()
    {
        MyClass.TestInt = 14; // CS0198, not in declaring class
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new ErrorDescription[]
            {
                new ErrorDescription { Code = (int)ErrorCode.ERR_AssgReadonlyStatic, Line = 15, Column = 7 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_AssgReadonlyStatic, Line = 20, Column = 8 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_AssgReadonlyStatic, Line = 25, Column = 8 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_AssgReadonlyStatic, Line = 37, Column = 9 },
            });
        }

        [Fact, WorkItem(990, "https://github.com/dotnet/roslyn/issues/990")]
        public void WriteOfReadonlyStaticMemberOfAnotherInstantiation01()
        {
            var text =
@"public static class Goo<T>
{
    static Goo()
    {
        Goo<int>.X = 1;
        Goo<int>.Y = 2;
        Goo<T>.Y = 3;
    }

    public static readonly int X;
    public static int Y { get; }
}";
            CreateCompilation(text, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (6,9): error CS0200: Property or indexer 'Goo<int>.Y' cannot be assigned to -- it is read only
                //         Goo<int>.Y = 2;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Goo<int>.Y").WithArguments("Goo<int>.Y").WithLocation(6, 9)
                );
            CreateCompilation(text, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithStrictFeature()).VerifyDiagnostics(
                // (5,9): error CS0198: A static readonly field cannot be assigned to (except in a static constructor or a variable initializer)
                //         Goo<int>.X = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyStatic, "Goo<int>.X").WithLocation(5, 9),
                // (6,9): error CS0200: Property or indexer 'Goo<int>.Y' cannot be assigned to -- it is read only
                //         Goo<int>.Y = 2;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Goo<int>.Y").WithArguments("Goo<int>.Y").WithLocation(6, 9)
                );
        }

        [Fact, WorkItem(990, "https://github.com/dotnet/roslyn/issues/990")]
        public void WriteOfReadonlyStaticMemberOfAnotherInstantiation02()
        {
            var text =
@"using System;
using System.Threading;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(Goo<long>.x);
        Console.WriteLine(Goo<int>.x);
        Console.WriteLine(Goo<string>.x);
        Console.WriteLine(Goo<int>.x);
    }
}

public static class Goo<T>
{
    static Goo()
    {
        Console.WriteLine(""initializing for "" + typeof(T));
        Goo<int>.x = typeof(T).Name;
    }

    public static readonly string x;
}";
            var expectedOutput =
@"initializing for System.Int64
initializing for System.Int32

Int64
initializing for System.String

String
";
            // Although we accept this nasty code, it will not verify.
            CompileAndVerify(text, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        [Fact]
        public void CS0199ERR_RefReadonlyStatic()
        {
            var text = @"
class MyClass
{
    public static readonly int TestInt = 6;

    static void TestMethod(ref int testInt)
    {
        testInt = 0;
    }

    MyClass()
    {
        TestMethod(ref TestInt);   // CS0199, TestInt is static
    }

    public static void Main()
    {
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_RefReadonlyStatic, Line = 13, Column = 24 } });
        }

        [Fact]
        public void CS0200ERR_AssgReadonlyProp01()
        {
            var source =
@"abstract class A
{
    internal static A P { get { return null; } }
    internal object Q { get; set; }
    public abstract object R { get; }
}
class B : A
{
    public override object R { get { return null; } }
}
class Program
{
    static void M(B b)
    {
        B.P.Q = null;
        B.P = null; // CS0200
        b.R = null; // CS0200
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,9): error CS0200: Property or indexer 'A.P' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "B.P").WithArguments("A.P").WithLocation(16, 9),
                // (17,9): error CS0200: Property or indexer 'B.R' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "b.R").WithArguments("B.R").WithLocation(17, 9));
        }

        [Fact]
        public void CS0200ERR_AssgReadonlyProp02()
        {
            var source =
@"class A
{
    public virtual A P { get; set; }
    public A Q { get { return null; } }
}
class B : A
{
    public override A P { get { return null; } }
}
class Program
{
    static void M(B b)
    {
        b.P = null;
        b.Q = null; // CS0200
        b.Q.P = null;
        b.P.Q = null; // CS0200
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,9): error CS0200: Property or indexer 'A.Q' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "b.Q").WithArguments("A.Q").WithLocation(15, 9),
                // (17,9): error CS0200: Property or indexer 'A.Q' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "b.P.Q").WithArguments("A.Q").WithLocation(17, 9));
        }

        [Fact]
        public void CS0200ERR_AssgReadonlyProp03()
        {
            var source =
@"class C
{
    static int P { get { return 0; } }
    int Q { get { return 0; } }
    static void M(C c)
    {
        ++P;
        ++c.Q;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,11): error CS0200: Property or indexer 'C.P' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P").WithArguments("C.P").WithLocation(7, 11),
                // (8,11): error CS0200: Property or indexer 'C.Q' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "c.Q").WithArguments("C.Q").WithLocation(8, 11));
        }

        [Fact]
        public void CS0200ERR_AssgReadonlyProp04()
        {
            var source =
@"class C
{
    object P { get { P = null; return null; } }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,22): error CS0200: Property or indexer 'C.P' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P").WithArguments("C.P").WithLocation(3, 22));
        }

        [Fact]
        public void CS0200ERR_AssgReadonlyProp05()
        {
            CreateCompilation(
@"class C
{
    int this[int x] { get { return x; } }
    void M(int b)
    {
        this[0] = b;
        this[1]++;
        this[2] += 1;
    }
}")
            .VerifyDiagnostics(
                // (6,9): error CS0200: Property or indexer 'C.this[int]' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "this[0]").WithArguments("C.this[int]"),
                // (7,9): error CS0200: Property or indexer 'C.this[int]' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "this[1]").WithArguments("C.this[int]"),
                // (8,9): error CS0200: Property or indexer 'C.this[int]' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "this[2]").WithArguments("C.this[int]"));
        }

        [Fact]
        public void CS0200ERR_AssgReadonlyProp06()
        {
            var source1 =
@"public class A
{
    public virtual object P { get { return null; } private set { } }
}
public class B : A
{
    public override object P { get { return null; } }
}";
            var compilation1 = CreateCompilation(source1);
            compilation1.VerifyDiagnostics();
            var compilationVerifier = CompileAndVerify(compilation1);
            var reference1 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);
            var source2 =
@"class C
{
    static void M(B b)
    {
        var o = b.P;
        b.P = o;
    }
}";
            var compilation2 = CreateCompilation(source2, references: new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (6,9): error CS0200: Property or indexer 'B.P' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "b.P").WithArguments("B.P").WithLocation(6, 9));
        }

        [Fact]
        public void CS0201ERR_IllegalStatement1()
        {
            var text = @"
public class MyList<T> 
{
   public void Add(T x)
   {
      int i = 0;
      if ( (object)x == null)
      {
         checked(i++);   // CS0201

         // OK
         checked {
            i++; 
         }
      }
   }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,10): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                Diagnostic(ErrorCode.ERR_IllegalStatement, "checked(i++)"));
        }

        [Fact, WorkItem(536863, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536863")]
        public void CS0201ERR_IllegalStatement2()
        {
            var text = @"
class A
{
    public static int Main()
    {
        (a) => a;
        (a, b) => { };
        int x = 0; int y = 0;
        x + y; x == 1;
    }
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular.WithTuplesFeature()).VerifyDiagnostics(
    // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         (a) => a;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "(a) => a").WithLocation(6, 9),
    // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         (a, b) => { };
    Diagnostic(ErrorCode.ERR_IllegalStatement, "(a, b) => { }").WithLocation(7, 9),
    // (9,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         x + y; x == 1;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "x + y").WithLocation(9, 9),
    // (9,16): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         x + y; x == 1;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "x == 1").WithLocation(9, 16),
    // (4,23): error CS0161: 'A.Main()': not all code paths return a value
    //     public static int Main()
    Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("A.Main()").WithLocation(4, 23)
    );
        }

        [Fact, WorkItem(536863, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536863")]
        public void CS0201ERR_IllegalStatement2WithCSharp6()
        {
            var test = @"
class A
{
    public static int Main()
    {
        (a) => a;
        (a, b) => { };
        int x = 0; int y = 0;
        x + y; x == 1;
    }
}";
            var comp = CreateCompilation(new[] { Parse(test, options: TestOptions.Regular6) }, new MetadataReference[] { });
            comp.VerifyDiagnostics(
    // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         (a) => a;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "(a) => a").WithLocation(6, 9),
    // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         (a, b) => { };
    Diagnostic(ErrorCode.ERR_IllegalStatement, "(a, b) => { }").WithLocation(7, 9),
    // (9,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         x + y; x == 1;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "x + y").WithLocation(9, 9),
    // (9,16): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         x + y; x == 1;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "x == 1").WithLocation(9, 16),
    // (4,23): error CS0161: 'A.Main()': not all code paths return a value
    //     public static int Main()
    Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("A.Main()").WithLocation(4, 23)
    );
        }

        [Fact]
        public void CS0202ERR_BadGetEnumerator()
        {
            var text = @"
public class C1
{
   public int Current
   {
      get
      {
         return 0;
      }
   }

   public bool MoveNext ()
   {
      return false;
   }

   public static implicit operator C1 (int c1)
   {
      return 0;
   }
}

public class C2
{
   public int Current
   {
      get
      {
         return 0;
      }
   }

   public bool MoveNext ()
   {
      return false;
   }

   public C1[] GetEnumerator ()
   {
      return null;
   }
}

public class MainClass
{
   public static void Main ()
   {
      C2 c2 = new C2();

      foreach (C1 x in c2)   // CS0202
      {
         System.Console.WriteLine(x.Current);
      }
   }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadGetEnumerator, Line = 50, Column = 24 } });
        }

        //        [Fact()]
        //        public void CS0204ERR_TooManyLocals()
        //        {
        //            var text = @"
        //";
        //            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
        //                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_TooManyLocals, Line = 8, Column = 13 } }
        //                );
        //        }

        [Fact()]
        public void CS0205ERR_AbstractBaseCall()
        {
            var text =
@"abstract class A
{
    abstract public void M();
    abstract protected object P { get; }
}
class B : A
{
    public override void M()
    {
        base.M(); // CS0205
        object o = base.P; // CS0205
    }
    protected override object P { get { return null; } }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AbstractBaseCall, Line = 10, Column = 9 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_AbstractBaseCall, Line = 11, Column = 20 });
        }

        [Fact]
        public void CS0205ERR_AbstractBaseCall_Override()
        {
            var text =
@"
public class Base1
{
    public virtual long Property1 { get { return 0; } set { } }
}
abstract public class Base2 : Base1
{
    public abstract override long Property1 { get; }
    void test1()
    {
        Property1 += 1;
    }
}
public class Derived : Base2
{
    public override long Property1 { get { return 1; } set { } }
    void test2()
    {
        base.Property1++;
        base.Property1 = 2;
        long x = base.Property1;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (19,9): error CS0205: Cannot call an abstract base member: 'Base2.Property1'
                Diagnostic(ErrorCode.ERR_AbstractBaseCall, "base.Property1").WithArguments("Base2.Property1"),
                // (21,18): error CS0205: Cannot call an abstract base member: 'Base2.Property1'
                Diagnostic(ErrorCode.ERR_AbstractBaseCall, "base.Property1").WithArguments("Base2.Property1"));
        }

        [Fact]
        public void CS0206ERR_RefProperty()
        {
            var text =
@"class C
{
    static int P { get; set; }
    object Q { get; set; }
    static void M(ref int i)
    {
    }
    static void M(out object o)
    {
        o = null;
    }
    void M()
    {
        M(ref P); // CS0206
        M(out this.Q); // CS0206
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (14,15): error CS0206: A property or indexer may not be passed as an out or ref parameter
                Diagnostic(ErrorCode.ERR_RefProperty, "P").WithArguments("C.P"),
                // (15,15): error CS0206: A property or indexer may not be passed as an out or ref parameter
                Diagnostic(ErrorCode.ERR_RefProperty, "this.Q").WithArguments("C.Q"));
        }

        [Fact]
        public void CS0206ERR_RefProperty_Indexers()
        {
            var text =
@"class C
{
    int this[int x] { get { return x; } set { } }
    static void R(ref int i)
    {
    }
    static void O(out int o)
    {
        o = 0;
    }
    void M()
    {
        R(ref this[0]); // CS0206
        O(out this[0]); // CS0206
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (13,15): error CS0206: A property or indexer may not be passed as an out or ref parameter
                Diagnostic(ErrorCode.ERR_RefProperty, "this[0]").WithArguments("C.this[int]"),
                // (14,15): error CS0206: A property or indexer may not be passed as an out or ref parameter
                Diagnostic(ErrorCode.ERR_RefProperty, "this[0]").WithArguments("C.this[int]"));
        }

        [Fact]
        public void CS0208ERR_ManagedAddr01()
        {
            var text = @"
class myClass
{
    public int a = 98;
}

struct myProblemStruct
{
    string s;
    float f;
}

struct myGoodStruct
{
    int i;
    float f;
}

public class MyClass
{
    unsafe public static void Main()
    {
        // myClass is a class, a managed type.
        myClass s = new myClass();  
        myClass* s2 = &s;    // CS0208

        // The struct contains a string, a managed type.
        int i = sizeof(myProblemStruct); //CS0208
        
        // The struct contains only value types.
        i = sizeof(myGoodStruct); //OK
        
    }
}

";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (25,9): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('myClass')
                //         myClass* s2 = &s;    // CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "myClass*").WithArguments("myClass"),
                // (25,23): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('myClass')
                //         myClass* s2 = &s;    // CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "&s").WithArguments("myClass"),
                // (28,17): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('myProblemStruct')
                //         int i = sizeof(myProblemStruct); //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "sizeof(myProblemStruct)").WithArguments("myProblemStruct"),

                // (9,12): warning CS0169: The field 'myProblemStruct.s' is never used
                //     string s;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "s").WithArguments("myProblemStruct.s"),
                // (10,11): warning CS0169: The field 'myProblemStruct.f' is never used
                //     float f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("myProblemStruct.f"),
                // (15,9): warning CS0169: The field 'myGoodStruct.i' is never used
                //     int i;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("myGoodStruct.i"),
                // (16,11): warning CS0169: The field 'myGoodStruct.f' is never used
                //     float f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("myGoodStruct.f"));
        }

        [Fact]
        public void CS0208ERR_ManagedAddr02()
        {
            var source =
@"enum E { }
delegate void D();
struct S { }
interface I { }
unsafe class C
{
    object* _object;
    void* _void;
    bool* _bool;
    char* _char;
    sbyte* _sbyte;
    byte* _byte;
    short* _short;
    ushort* _ushort;
    int* _int;
    uint* _uint;
    long* _long;
    ulong* _ulong;
    decimal* _decimal;
    float* _float;
    double* _double;
    string* _string;
    System.IntPtr* _intptr;
    System.UIntPtr* _uintptr;
    int** _intptr2;
    int?* _nullable;
    dynamic* _dynamic;
    E* e;
    D* d;
    S* s;
    I* i;
    C* c;
}";
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeReleaseDll)
                .GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Verify(
                    // (22,13): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('string')
                    //     string* _string;
                    Diagnostic(ErrorCode.ERR_ManagedAddr, "_string").WithArguments("string").WithLocation(22, 13),
                    // (27,14): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('dynamic')
                    //     dynamic* _dynamic;
                    Diagnostic(ErrorCode.ERR_ManagedAddr, "_dynamic").WithArguments("dynamic").WithLocation(27, 14),
                    // (29,8): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('D')
                    //     D* d;
                    Diagnostic(ErrorCode.ERR_ManagedAddr, "d").WithArguments("D").WithLocation(29, 8),
                    // (31,8): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('I')
                    //     I* i;
                    Diagnostic(ErrorCode.ERR_ManagedAddr, "i").WithArguments("I").WithLocation(31, 8),
                    // (32,8): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('C')
                    //     C* c;
                    Diagnostic(ErrorCode.ERR_ManagedAddr, "c").WithArguments("C").WithLocation(32, 8),
                    // (7,13): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('object')
                    //     object* _object;
                    Diagnostic(ErrorCode.ERR_ManagedAddr, "_object").WithArguments("object").WithLocation(7, 13));
        }

        [Fact]
        public void CS0209ERR_BadFixedInitType()
        {
            var text = @"
class Point
{
   public int x, y;
}

public class MyClass
{
   unsafe public static void Main()
   {
      Point pt = new Point();

      fixed (int i)    // CS0209
      {
      }
   }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (13,18): error CS0209: The type of a local declared in a fixed statement must be a pointer type
                //       fixed (int i)    // CS0209
                Diagnostic(ErrorCode.ERR_BadFixedInitType, "i"),
                // (13,18): error CS0210: You must provide an initializer in a fixed or using statement declaration
                //       fixed (int i)    // CS0209
                Diagnostic(ErrorCode.ERR_FixedMustInit, "i"),

                // (4,15): warning CS0649: Field 'Point.x' is never assigned to, and will always have its default value 0
                //    public int x, y;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("Point.x", "0"),
                // (4,18): warning CS0649: Field 'Point.y' is never assigned to, and will always have its default value 0
                //    public int x, y;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "y").WithArguments("Point.y", "0"));
        }

        [Fact]
        public void CS0210ERR_FixedMustInit()
        {
            var text = @"
using System.IO;
class Test 
{
   static void Main() 
   {
      using (StreamWriter w) // CS0210
      {
         w.WriteLine(""Hello there"");
      }

      using (StreamWriter x, y) // CS0210, CS0210
      {
      }
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,27): error CS0210: You must provide an initializer in a fixed or using statement declaration
                //       using (StreamWriter w) // CS0210
                Diagnostic(ErrorCode.ERR_FixedMustInit, "w").WithLocation(7, 27),
                // (12,27): error CS0210: You must provide an initializer in a fixed or using statement declaration
                //       using (StreamWriter x, y) // CS0210, CS0210
                Diagnostic(ErrorCode.ERR_FixedMustInit, "x").WithLocation(12, 27),
                // (12,30): error CS0210: You must provide an initializer in a fixed or using statement declaration
                //       using (StreamWriter x, y) // CS0210, CS0210
                Diagnostic(ErrorCode.ERR_FixedMustInit, "y").WithLocation(12, 30),
                // (9,10): error CS0165: Use of unassigned local variable 'w'
                //          w.WriteLine("Hello there");
                Diagnostic(ErrorCode.ERR_UseDefViolation, "w").WithArguments("w").WithLocation(9, 10)
                );
        }

        [Fact]
        public void CS0211ERR_InvalidAddrOp()
        {
            var text = @"
public class MyClass
{
   unsafe public void M()
   {
      int a = 0, b = 0;
      int *i = &(a + b);   // CS0211, the addition of two local variables
      // try the following line instead
      // int *i = &a;
   }

   public static void Main()
   {
   }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,18): error CS0211: Cannot take the address of the given expression
                //       int *i = &(a + b);   // CS0211, the addition of two local variables
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "a + b"));
        }

        [Fact]
        public void CS0212ERR_FixedNeeded()
        {
            var text = @"
public class A {
   public int iField = 5;
   
   unsafe public void M() { 
      A a = new A();
      int* ptr = &a.iField;   // CS0212 
   }

   // OK
   unsafe public void M2() {
      A a = new A();
      fixed (int* ptr = &a.iField) {}
   }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,18): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //       int* ptr = &a.iField;   // CS0212 
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&a.iField"));
        }

        [Fact]
        public void CS0213ERR_FixedNotNeeded()
        {
            var text = @"
public class MyClass
{
   unsafe public static void Main()
   {
      int i = 45;
      fixed (int *j = &i) { }  // CS0213
      // try the following line instead
      // int* j = &i;

      int[] a = new int[] {1,2,3};
      fixed (int *b = a)
      {
         fixed (int *c = b) { }  // CS0213
         // try the following line instead
         // int *c = b;
      }
   }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,23): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                //       fixed (int *j = &i) { }  // CS0213
                Diagnostic(ErrorCode.ERR_FixedNotNeeded, "&i").WithLocation(7, 23),
                // (14,26): error CS9385: The given expression cannot be used in a fixed statement
                //          fixed (int *c = b) { }  // CS0213
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "b").WithLocation(14, 26));
        }

        [Fact]
        public void CS0217ERR_BadBoolOp()
        {
            // Note that the wording of this error message has changed.

            var text = @"
public class MyClass
{
   public static bool operator true (MyClass f)
   {
      return false;
   }

   public static bool operator false (MyClass f)
   {
      return false;
   }

   public static int operator & (MyClass f1, MyClass f2)   
   {
      return 0;
   }

   public static void Main()
   {
      MyClass f = new MyClass();
      int i = f && f; // CS0217
   }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (22,15): error CS0217: In order to be applicable as a short circuit operator a user-defined logical operator ('MyClass.operator &(MyClass, MyClass)') must have the same return type and parameter types
                //       int i = f && f; // CS0217
                Diagnostic(ErrorCode.ERR_BadBoolOp, "f && f").WithArguments("MyClass.operator &(MyClass, MyClass)"));
        }

        // CS0220 ERR_CheckedOverflow - see ConstantTests

        [Fact]
        public void CS0221ERR_ConstOutOfRangeChecked01()
        {
            string text =
@"class MyClass
{
    static void F(int x) { }
    static void M()
    {
        F((int)0xFFFFFFFF); // CS0221
        F(unchecked((int)uint.MaxValue));
        F(checked((int)(uint.MaxValue - 1))); // CS0221
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,11): error CS0221: Constant value '4294967295' cannot be converted to a 'int' (use 'unchecked' syntax to override)
                Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, "(int)0xFFFFFFFF").WithArguments("4294967295", "int"),
                // (8,19): error CS0221: Constant value '4294967294' cannot be converted to a 'int' (use 'unchecked' syntax to override)
                Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, "(int)(uint.MaxValue - 1)").WithArguments("4294967294", "int"));
        }

        [Fact]
        public void CS0221ERR_ConstOutOfRangeChecked02()
        {
            string text =
@"enum E : byte { A, B = 0xfe, C }
class C
{
    const int F = (int)(E.C + 1); // CS0221
    const int G = (int)unchecked(1 + E.C);
    const int H = (int)checked(E.A - 1); // CS0221
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,25): error CS0031: Constant value '256' cannot be converted to a 'E'
                Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, "E.C + 1").WithArguments("256", "E"),
                // (6,32): error CS0221: Constant value '-1' cannot be converted to a 'E' (use 'unchecked' syntax to override)
                Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, "E.A - 1").WithArguments("-1", "E"));
        }

        [WorkItem(1119609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1119609")]
        [Fact(Skip = "1119609")]
        public void CS0221ERR_ConstOutOfRangeChecked03()
        {
            var text =
@"public class MyClass
{
    decimal x1 = (decimal)double.PositiveInfinity;  //CS0221
    decimal x2 = (decimal)double.NegativeInfinity;  //CS0221
    decimal x3 = (decimal)double.NaN;               //CS0221
    decimal x4 = (decimal)double.MaxValue;          //CS0221

    public static void Main() {}
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,18): error CS0031: Constant value 'Infinity' cannot be converted to a 'decimal'
                //     decimal x1 = (decimal)double.PositiveInfinity;  //CS0221
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "(decimal)double.PositiveInfinity").WithArguments("Infinity", "decimal"),
                // (4,18): error CS0031: Constant value '-Infinity' cannot be converted to a 'decimal'
                //     decimal x2 = (decimal)double.NegativeInfinity;  //CS0221
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "(decimal)double.NegativeInfinity").WithArguments("-Infinity", "decimal"),
                // (5,18): error CS0031: Constant value 'NaN' cannot be converted to a 'decimal'
                //     decimal x3 = (decimal)double.NaN;               //CS0221
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "(decimal)double.NaN").WithArguments("NaN", "decimal"),
                // (6,18): error CS0031: Constant value '1.79769313486232E+308' cannot be converted to a 'decimal'
                //     decimal x4 = (decimal)double.MaxValue;          //CS0221
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "(decimal)double.MaxValue").WithArguments("1.79769313486232E+308", "decimal"),
                // (3,13): warning CS0414: The field 'MyClass.x1' is assigned but its value is never used
                //     decimal x1 = (decimal)double.PositiveInfinity;  //CS0221
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "x1").WithArguments("MyClass.x1"),
                // (4,13): warning CS0414: The field 'MyClass.x2' is assigned but its value is never used
                //     decimal x2 = (decimal)double.NegativeInfinity;  //CS0221
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "x2").WithArguments("MyClass.x2"),
                // (5,13): warning CS0414: The field 'MyClass.x3' is assigned but its value is never used
                //     decimal x3 = (decimal)double.NaN;               //CS0221
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "x3").WithArguments("MyClass.x3"),
                // (6,13): warning CS0414: The field 'MyClass.x4' is assigned but its value is never used
                //     decimal x4 = (decimal)double.MaxValue;          //CS0221
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "x4").WithArguments("MyClass.x4"));
        }

        [Fact]
        public void CS0226ERR_IllegalArglist()
        {
            var text = @"
public class C
    {
    public static int Main ()
        {
        __arglist(1,""This is a string""); // CS0226
        return 0;
        }
    }
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_IllegalArglist, @"__arglist(1,""This is a string"")"));
        }

        //        [Fact()]
        //        public void CS0228ERR_NoAccessibleMember()
        //        {
        //            var text = @"
        //";
        //            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
        //                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_NoAccessibleMember, Line = 31, Column = 17 } }
        //                );
        //        }

        [Fact]
        public void CS0229ERR_AmbigMember()
        {
            var text = @"
interface IList
{
    int Count
    {
        get;
        set;
    }

    void Counter();
}

interface Icounter
{
    double Count
    {
        get;
        set;
    }
}

interface IListCounter : IList , Icounter {}

class MyClass
{
    void Test(IListCounter x)
    {
        x.Count = 1;  // CS0229
        // Try one of the following lines instead:
        // ((IList)x).Count = 1;
        // or
        // ((Icounter)x).Count = 1;
    }

    public static void Main() {}
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AmbigMember, Line = 28, Column = 11 } });
        }

        [Fact]
        public void CS0233ERR_SizeofUnsafe()
        {
            var text = @"
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct S
{
    public int a;
}

public class MyClass
{
    public static void Main()
    {
        S myS = new S();
        Console.WriteLine(sizeof(S));   // CS0233
        // Try the following line instead:
        // Console.WriteLine(Marshal.SizeOf(myS));
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (16,27): error CS0233: 'S' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
                //         Console.WriteLine(sizeof(S));   // CS0233
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(S)").WithArguments("S"),

                // (15,11): warning CS0219: The variable 'myS' is assigned but its value is never used
                //         S myS = new S();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "myS").WithArguments("myS"));
        }

        [Fact]
        public void CS0236ERR_FieldInitRefNonstatic()
        {
            var text = @"
public class MyClass
{
    int[] instanceArray;
    static int[] staticArray;

    static int staticField = 1;
    const int constField = 1;

    int a;
    int b = 1;
    int c = b; //CS0236
    int d = this.b; //CS0027
    int e = InstanceMethod(); //CS0236
    int f = this.InstanceMethod(); //CS0027
    int g = StaticMethod();
    int h = MyClass.StaticMethod();
    int i = GenericInstanceMethod<int>(1); //CS0236
    int j = this.GenericInstanceMethod<int>(1); //CS0027
    int k = GenericStaticMethod<int>(1);
    int l = MyClass.GenericStaticMethod<int>(1);
    int m = InstanceProperty; //CS0236
    int n = this.InstanceProperty; //CS0027
    int o = StaticProperty;
    int p = MyClass.StaticProperty;
    int q = instanceArray[0]; //CS0236
    int r = this.instanceArray[0]; //CS0027
    int s = staticArray[0];
    int t = MyClass.staticArray[0];
    int u = staticField;
    int v = MyClass.staticField;
    int w = constField;
    int x = MyClass.constField;

    MyClass()
    {
        a = b;
    }

    int InstanceMethod()
    {
        return a;
    }

    static int StaticMethod()
    {
        return 1;
    }

    T GenericInstanceMethod<T>(T t)
    {
        return t;
    }

    static T GenericStaticMethod<T>(T t)
    {
        return t;
    }

    int InstanceProperty { get { return a; } }

    static int StaticProperty { get { return 1; } }

    public static void Main()
    {
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (12,13): error CS0236: A field initializer cannot reference the non-static field, method, or property 'MyClass.b'
                //     int c = b; //CS0236
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "b").WithArguments("MyClass.b").WithLocation(12, 13),
                // (13,13): error CS0027: Keyword 'this' is not available in the current context
                //     int d = this.b; //CS0027
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(13, 13),
                // (14,13): error CS0236: A field initializer cannot reference the non-static field, method, or property 'MyClass.InstanceMethod()'
                //     int e = InstanceMethod(); //CS0236
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "InstanceMethod").WithArguments("MyClass.InstanceMethod()").WithLocation(14, 13),
                // (15,13): error CS0027: Keyword 'this' is not available in the current context
                //     int f = this.InstanceMethod(); //CS0027
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(15, 13),
                // (18,13): error CS0236: A field initializer cannot reference the non-static field, method, or property 'MyClass.GenericInstanceMethod<int>(int)'
                //     int i = GenericInstanceMethod<int>(1); //CS0236
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "GenericInstanceMethod<int>").WithArguments("MyClass.GenericInstanceMethod<int>(int)").WithLocation(18, 13),
                // (19,13): error CS0027: Keyword 'this' is not available in the current context
                //     int j = this.GenericInstanceMethod<int>(1); //CS0027
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(19, 13),
                // (22,13): error CS0236: A field initializer cannot reference the non-static field, method, or property 'MyClass.InstanceProperty'
                //     int m = InstanceProperty; //CS0236
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "InstanceProperty").WithArguments("MyClass.InstanceProperty").WithLocation(22, 13),
                // (23,13): error CS0027: Keyword 'this' is not available in the current context
                //     int n = this.InstanceProperty; //CS0027
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(23, 13),
                // (26,13): error CS0236: A field initializer cannot reference the non-static field, method, or property 'MyClass.instanceArray'
                //     int q = instanceArray[0]; //CS0236
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "instanceArray").WithArguments("MyClass.instanceArray").WithLocation(26, 13),
                // (27,13): error CS0027: Keyword 'this' is not available in the current context
                //     int r = this.instanceArray[0]; //CS0027
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(27, 13),
                // (4,11): warning CS0649: Field 'MyClass.instanceArray' is never assigned to, and will always have its default value null
                //     int[] instanceArray;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "instanceArray").WithArguments("MyClass.instanceArray", "null").WithLocation(4, 11),
                // (5,18): warning CS0649: Field 'MyClass.staticArray' is never assigned to, and will always have its default value null
                //     static int[] staticArray;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "staticArray").WithArguments("MyClass.staticArray", "null").WithLocation(5, 18),
                // (33,9): warning CS0414: The field 'MyClass.x' is assigned but its value is never used
                //     int x = MyClass.constField;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "x").WithArguments("MyClass.x").WithLocation(33, 9),
                // (32,9): warning CS0414: The field 'MyClass.w' is assigned but its value is never used
                //     int w = constField;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "w").WithArguments("MyClass.w").WithLocation(32, 9)
            );
        }

        [Fact]
        public void CS0236ERR_FieldInitRefNonstaticMethodGroups()
        {
            var text = @"
delegate void F();
public class MyClass
{
    F a = Static;
    F b = MyClass.Static;
    F c = Static<int>;
    F d = MyClass.Static<int>;
    F e = Instance;
    F f = this.Instance;
    F g = Instance<int>;
    F h = this.Instance<int>;

    static void Static() { }
    static void Static<T>() { }

    void Instance() { }
    void Instance<T>() { }

    public static void Main()
    {
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(
                text,
                new ErrorDescription[]
                {
                    new ErrorDescription { Code = (int)ErrorCode.ERR_FieldInitRefNonstatic, Line = 9, Column = 11 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ThisInBadContext, Line = 10, Column = 11 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_FieldInitRefNonstatic, Line = 11, Column = 11 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ThisInBadContext, Line = 12, Column = 11 },
                });
        }

        [WorkItem(541501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541501")]
        [Fact]
        public void CS0236ERR_FieldInitRefNonstaticProperty()
        {
            CreateCompilation(
@"
enum ProtectionLevel
{
  Privacy = 0
}
 
class F
{
  const ProtectionLevel p = ProtectionLevel.Privacy; // CS0236
 
  int ProtectionLevel { get { return 0; } }
}
")
            .VerifyDiagnostics(
                // (9,29): error CS0236: A field initializer cannot reference the non-static field, method, or property 'F.ProtectionLevel'
                //   const ProtectionLevel p = ProtectionLevel.Privacy; // CS0120
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "ProtectionLevel").WithArguments("F.ProtectionLevel"));
        }

        [WorkItem(541501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541501")]
        [Fact]
        public void CS0236ERR_FieldInitRefNonstatic_ObjectInitializer()
        {
            CreateCompilation(
@"
public class Goo
{
    public int i;
    public string s;
}

public class MemberInitializerTest
{
    private int i =10;
    private string s = ""abc"";
    private Goo f = new Goo{i = i, s = s};

    public static void Main()
    {
    }
}
")
            .VerifyDiagnostics(
                // (12,33): error CS0236: A field initializer cannot reference the non-static field, method, or property 'MemberInitializerTest.i'
                //     private Goo f = new Goo{i = i, s = s};
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "i").WithArguments("MemberInitializerTest.i").WithLocation(12, 33),
                // (12,40): error CS0236: A field initializer cannot reference the non-static field, method, or property 'MemberInitializerTest.s'
                //     private Goo f = new Goo{i = i, s = s};
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "s").WithArguments("MemberInitializerTest.s").WithLocation(12, 40));
        }

        [Fact]
        public void CS0236ERR_FieldInitRefNonstatic_AnotherInitializer()
        {
            CreateCompilation(
@"
class TestClass
{
    int P1 { get; }

    int y = (P1 = 123);
    int y1 { get; } = (P1 = 123);

    static void Main()
    {
    }
}
")
            .VerifyDiagnostics(
    // (6,14): error CS0236: A field initializer cannot reference the non-static field, method, or property 'TestClass.P1'
    //     int y = (P1 = 123);
    Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "P1").WithArguments("TestClass.P1").WithLocation(6, 14),
    // (7,24): error CS0236: A field initializer cannot reference the non-static field, method, or property 'TestClass.P1'
    //     int y1 { get; } = (P1 = 123);
    Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "P1").WithArguments("TestClass.P1").WithLocation(7, 24)
                );
        }


        [Fact]
        public void CS0242ERR_VoidError()
        {
            var text = @"
class TestClass
{
    public unsafe void Test()
    {
        void* p = null;
        p++; //CS0242
        p += 2; //CS0242
        void* q = p + 1; //CS0242
        long diff = q - p; //CS0242
        var v = *p;
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,9): error CS0242: The operation in question is undefined on void pointers
                //         p++; //CS0242
                Diagnostic(ErrorCode.ERR_VoidError, "p++"),
                // (8,9): error CS0242: The operation in question is undefined on void pointers
                //         p += 2; //CS0242
                Diagnostic(ErrorCode.ERR_VoidError, "p += 2"),
                // (9,19): error CS0242: The operation in question is undefined on void pointers
                //         void* q = p + 1; //CS0242
                Diagnostic(ErrorCode.ERR_VoidError, "p + 1"),
                // (10,21): error CS0242: The operation in question is undefined on void pointers
                //         long diff = q - p; //CS0242
                Diagnostic(ErrorCode.ERR_VoidError, "q - p"),
                // (11,17): error CS0242: The operation in question is undefined on void pointers
                //         var v = *p;
                Diagnostic(ErrorCode.ERR_VoidError, "*p"));
        }

        [Fact]
        public void CS0244ERR_PointerInAsOrIs()
        {
            var text = @"
class UnsafeTest
{
   unsafe static void SquarePtrParam (int* p)
   {
      bool b = p is object;   // CS0244 p is pointer
   }

   unsafe public static void Main()
   {
      int i = 5;
      SquarePtrParam (&i);
   }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,16): error CS0244: Neither 'is' nor 'as' is valid on pointer types
                //       bool b = p is object;   // CS0244 p is pointer
                Diagnostic(ErrorCode.ERR_PointerInAsOrIs, "p is object"));
        }

        [Fact]
        public void CS0245ERR_CallingFinalizeDeprecated()
        {
            var text = @"
class MyClass // : IDisposable
{
   /*
   public void Dispose()
   {
      // cleanup code goes here
   }
   */

   void m()
   {
      this.Finalize();   // CS0245
      // this.Dispose();
   }

   public static void Main()
   {
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (13,7): error CS0245: Destructors and object.Finalize cannot be called directly. Consider calling IDisposable.Dispose if available.
                Diagnostic(ErrorCode.ERR_CallingFinalizeDeprecated, "this.Finalize()"));
        }

        [WorkItem(540722, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540722")]
        [Fact]
        public void CS0246ERR_SingleTypeNameNotFound05()
        {
            CreateCompilation(@"
namespace nms
{
    public class Mine
    {
        private static int retval = 5;
        public static int Main()
        {
            try { }
            catch (e) { }
            return retval;
        }
    };
}
")
            .VerifyDiagnostics(
                // (10,20): error CS0246: The type or namespace name 'e' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "e").WithArguments("e"));
        }

        [WorkItem(528446, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528446")]
        [Fact]
        public void CS0246ERR_SingleTypeNameNotFoundNoCS8000()
        {
            CreateCompilation(@"
class Test
{
    void Main()
    {
        var sum = new j();
    }
}
")
            .VerifyDiagnostics(
                // (11,20): error CS0246: The type or namespace name 'j' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "j").WithArguments("j"));
        }

        [Fact]
        public void CS0247ERR_NegativeStackAllocSize()
        {
            var text = @"
public class MyClass
{
   unsafe public static void Main()
   {
      int *p = stackalloc int [-30];   // CS0247
   }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,32): error CS0247: Cannot use a negative size with stackalloc
                //       int *p = stackalloc int [-30];   // CS0247
                Diagnostic(ErrorCode.ERR_NegativeStackAllocSize, "-30"));
        }

        [Fact]
        public void CS0248ERR_NegativeArraySize()
        {
            var text = @"
class MyClass
{
    public static void Main()
    {
        int[] myArray = new int[-3] {1,2,3};   // CS0248, pass a nonnegative number
        int[] myArray2 = new int[-5000000000]; // slightly different code path for long array sizes
        int[] myArray3 = new int[3000000000u]; // slightly different code path for uint array sizes
        var myArray4 = new object[-2, 1, -1] {{{null}},{{null}}};
        var myArray5 = new object[-1L] {null};
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,33): error CS0248: Cannot create an array with a negative size
                //         int[] myArray = new int[-3] {1,2,3};   // CS0248, pass a nonnegative number
                Diagnostic(ErrorCode.ERR_NegativeArraySize, "-3").WithLocation(6, 33),
                // (7,34): error CS0248: Cannot create an array with a negative size
                //         int[] myArray2 = new int[-5000000000]; // slightly different code path for long array sizes
                Diagnostic(ErrorCode.ERR_NegativeArraySize, "-5000000000").WithLocation(7, 34),
                // (9,35): error CS0248: Cannot create an array with a negative size
                //         var myArray4 = new object[-2, 1, -1] {{{null}},{{null}}};
                Diagnostic(ErrorCode.ERR_NegativeArraySize, "-2").WithLocation(9, 35),
                // (9,42): error CS0248: Cannot create an array with a negative size
                //         var myArray4 = new object[-2, 1, -1] {{{null}},{{null}}};
                Diagnostic(ErrorCode.ERR_NegativeArraySize, "-1").WithLocation(9, 42),
                // (10,35): error CS0248: Cannot create an array with a negative size
                //         var myArray5 = new object[-1L] {null};
                Diagnostic(ErrorCode.ERR_NegativeArraySize, "-1L").WithLocation(10, 35),
                // (10,35): error CS0150: A constant value is expected
                //         var myArray5 = new object[-1L] {null};
                Diagnostic(ErrorCode.ERR_ConstantExpected, "-1L").WithLocation(10, 35));
        }

        [WorkItem(528912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528912")]
        [Fact]
        public void CS0250ERR_CallingBaseFinalizeDeprecated()
        {
            var text = @"
class B
{
}

class C : B
{
   ~C()
   {
      base.Finalize();   // CS0250
   }

   public static void Main()
   {
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,7): error CS0250: Do not directly call your base class Finalize method. It is called automatically from your destructor.
                Diagnostic(ErrorCode.ERR_CallingBaseFinalizeDeprecated, "base.Finalize()"));
        }

        [Fact]
        public void CS0254ERR_BadCastInFixed()
        {
            var text = @"
class Point
{
   public uint x, y;
}

class FixedTest
{
   unsafe static void SquarePtrParam (int* p)
   {
      *p *= *p;
   }

   unsafe public static void Main()
   {
      Point pt = new Point();
      pt.x = 5;
      pt.y = 6;

      fixed (int* p = (int*)&pt.x)   // CS0254
      // try the following line instead
      // fixed (uint* p = &pt.x)
      {
         SquarePtrParam ((int*)p);
      }
   }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (20,23): error CS9385: The given expression cannot be used in a fixed statement
                //       fixed (int* p = (int*)&pt.x)   // CS0254
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "(int*)&pt.x").WithLocation(20, 23));
        }

        [Fact]
        public void CS0255ERR_StackallocInFinally()
        {
            var text = @"
unsafe class Test
{
    void M()
    {
        try
        {
            // Something        
        }
        finally
        {
            int* fib = stackalloc int[100];
        }
    }
}";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (12,24): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* fib = stackalloc int[100];
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[100]").WithLocation(12, 24));
        }

        [Fact]
        public void CS0255ERR_StackallocInCatch()
        {
            var text = @"
unsafe class Test
{
    void M()
    {
        try
        {
            // Something        
        }
        catch
        {
            int* fib = stackalloc int[100];
        }
    }
}";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (12,24): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* fib = stackalloc int[100];
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[100]").WithLocation(12, 24));
        }

        [Fact]
        public void CS0266ERR_NoImplicitConvCast01()
        {
            var text = @"
class MyClass
{
    public static void Main()
    {
        object obj = ""MyString"";
        // Cannot implicitly convert 'object' to 'MyClass'
        MyClass myClass = obj;  // CS0266
        // Try this line instead
        // MyClass c = ( MyClass )obj;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_NoImplicitConvCast, Line = 8, Column = 27 } });
        }

        [Fact]
        public void CS0266ERR_NoImplicitConvCast02()
        {
            var source =
@"class C
{
    const int f = 0L;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,19): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                //     const int f = 0L;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "0L").WithArguments("long", "int").WithLocation(3, 19));
        }

        [Fact]
        public void CS0266ERR_NoImplicitConvCast03()
        {
            var source =
@"class C
{
    static void M()
    {
        const short s = 1L;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,25): error CS0266: Cannot implicitly convert type 'long' to 'short'. An explicit conversion exists (are you missing a cast?)
                //         const short s = 1L;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1L").WithArguments("long", "short").WithLocation(5, 25),
                // (5,21): warning CS0219: The variable 's' is assigned but its value is never used
                //         const short s = 1L;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s").WithLocation(5, 21));
        }

        [Fact]
        public void CS0266ERR_NoImplicitConvCast04()
        {
            var source =
@"enum E { A = 1 }
class C
{
    E f = 2; // CS0266
    E g = E.A;
    void M()
    {
        f = E.A;
        g = 'c'; // CS0266
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,11): error CS0266: Cannot implicitly convert type 'int' to 'E'. An explicit conversion exists (are you missing a cast?)
                //     E f = 2; // CS0266
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "2").WithArguments("int", "E").WithLocation(4, 11),
                // (9,13): error CS0266: Cannot implicitly convert type 'char' to 'E'. An explicit conversion exists (are you missing a cast?)
                //         g = 'c'; // CS0266
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "'c'").WithArguments("char", "E").WithLocation(9, 13),
                // (4,7): warning CS0414: The field 'C.f' is assigned but its value is never used
                //     E f = 2; // CS0266
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "f").WithArguments("C.f").WithLocation(4, 7),
                // (5,7): warning CS0414: The field 'C.g' is assigned but its value is never used
                //     E g = E.A;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "g").WithArguments("C.g").WithLocation(5, 7));
        }

        [Fact]
        public void CS0266ERR_NoImplicitConvCast05()
        {
            var source =
@"enum E : byte
{
    A = 'a', // CS0266
    B = 0xff,
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,9): error CS0266: Cannot implicitly convert type 'char' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //     A = 'a', // CS0266
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "'a'").WithArguments("char", "byte").WithLocation(3, 9));
        }

        [Fact]
        public void CS0266ERR_NoImplicitConvCast06()
        {
            var source =
@"enum E
{
    A = 1,
    B = 1L // CS0266
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,9): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                //     B = 1L // CS0266
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1L").WithArguments("long", "int").WithLocation(4, 9));
        }

        [Fact]
        public void CS0266ERR_NoImplicitConvCast07()
        {
            // No errors
            var source = "enum E { A, B = A }";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void CS0266ERR_NoImplicitConvCast08()
        {
            // No errors
            var source =
@"enum E { A = 1, B }
enum F { X = E.A + 1, Y }
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void CS0266ERR_NoImplicitConvCast09()
        {
            var source =
@"enum E
{
    A = F.A,
    B = F.B,
    C = G.A,
    D = G.B,
}
enum F : short { A = 1, B }
enum G : long { A = 1, B }
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,9): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                //     C = G.A,
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "G.A").WithArguments("long", "int").WithLocation(5, 9),
                // (6,9): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                //     D = G.B,
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "G.B").WithArguments("long", "int").WithLocation(6, 9));
        }

        [Fact]
        public void CS0266ERR_NoImplicitConvCast10()
        {
            var source =
@"class C
{
    public const int F = D.G + 1;
}
class D
{
    public const int G = E.H + 1;
}
class E
{
    public const int H = 1L;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,26): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                //     public const int H = 1L;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1L").WithArguments("long", "int").WithLocation(11, 26));
        }

        [Fact()]
        public void CS0266ERR_NoImplicitConvCast11()
        {
            string text = @"class Program
{
    static void Main(string[] args)
    {
        bool? b = true;
        int result = b ? 0 : 1; // Compiler error
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b").WithArguments("bool?", "bool"));
        }

        [WorkItem(541718, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541718")]
        [Fact]
        public void CS0266ERR_NoImplicitConvCast12()
        {
            string text = @"
class C1
{
    public static void Main()
    {
        var cube = new int[Number.One][];
    }
}
enum Number
{
    One,
    Two
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,28): error CS0266: Cannot implicitly convert type 'Number' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var cube = new int[Number.One][];
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "Number.One").WithArguments("Number", "int").WithLocation(6, 28));
        }

        [WorkItem(541718, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541718")]
        [Fact]
        public void CS0266ERR_NoImplicitConvCast13()
        {
            string text = @"
class C1
{
    public static void Main()
    {
        double x = 5;
        int[] arr4 = new int[x];// Invalid

        float y = 5;
        int[] arr5 = new int[y];// Invalid

        decimal z = 5;
        int[] arr6 = new int[z];// Invalid
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                    // (7,30): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                    //         int[] arr4 = new int[x];// Invalid
                    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("double", "int").WithLocation(7, 30),
                    // (10,30): error CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                    //         int[] arr5 = new int[y];// Invalid
                    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("float", "int").WithLocation(10, 30),
                    // (13,30): error CS0266: Cannot implicitly convert type 'decimal' to 'int'. An explicit conversion exists (are you missing a cast?)
                    //         int[] arr6 = new int[z];// Invalid
                    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "z").WithArguments("decimal", "int").WithLocation(13, 30));
        }

        [Fact]
        public void CS0269ERR_UseDefViolationOut()
        {
            var text = @"
class C
{
    public static void F(out int i)
    {
        try
        {
            // Assignment occurs, but compiler can't verify it
            i = 1;
        }
        catch
        {
        }

        int k = i;  // CS0269
        i = 1;
    }

    public static void Main()
    {
        int myInt;
        F(out myInt);
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_UseDefViolationOut, Line = 15, Column = 17 } });
        }

        [Fact]
        public void CS0271ERR_InaccessibleGetter01()
        {
            var source =
@"class C
{
    internal static object P { private get; set; }
    public C Q { protected get { return null; } set { } }
}
class P
{
    static void M(C c)
    {
        object o = C.P;
        M(c.Q);
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,20): error CS0271: The property or indexer 'C.P' cannot be used in this context because the get accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "C.P").WithArguments("C.P").WithLocation(10, 20),
                // (11,11): error CS0271: The property or indexer 'C.Q' cannot be used in this context because the get accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "c.Q").WithArguments("C.Q").WithLocation(11, 11));
        }

        [Fact]
        public void CS0271ERR_InaccessibleGetter02()
        {
            var source =
@"class A
{
    public virtual object P { protected get; set; }
}
class B : A
{
    public override object P { set { } }
    void M()
    {
        object o = P; // no error
    }
}
class C
{
    void M(B b)
    {
        object o = b.P; // CS0271
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (17,20): error CS0271: The property or indexer 'B.P' cannot be used in this context because the get accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "b.P").WithArguments("B.P").WithLocation(17, 20));
        }

        [Fact]
        public void CS0271ERR_InaccessibleGetter03()
        {
            var source =
@"namespace N1
{
    class A
    {
        void M(N2.B b)
        {
            object o = b.P;
        }
    }
}
namespace N2
{
    class B : N1.A
    {
        public object P { protected get; set; }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,24): error CS0271: The property or indexer 'N2.B.P' cannot be used in this context because the get accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "b.P").WithArguments("N2.B.P").WithLocation(7, 24));
        }

        [Fact]
        public void CS0271ERR_InaccessibleGetter04()
        {
            var source =
@"class A
{
    static public object P { protected get; set; }
    static internal object Q { private get; set; }
    static void M()
    {
        object o = B.Q; // no error
        o = A.Q; // no error
    }
}
class B : A
{
    static void M()
    {
        object o = B.P; // no error
        o = P; // no error
        o = Q; // CS0271
    }
}
class C
{
    static void M()
    {
        object o = B.P; // CS0271
        o = A.Q; // CS0271
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (17,13): error CS0271: The property or indexer 'A.Q' cannot be used in this context because the get accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "Q").WithArguments("A.Q").WithLocation(17, 13),
                // (24,20): error CS0271: The property or indexer 'A.P' cannot be used in this context because the get accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "B.P").WithArguments("A.P").WithLocation(24, 20),
                // (25,13): error CS0271: The property or indexer 'A.Q' cannot be used in this context because the get accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "A.Q").WithArguments("A.Q").WithLocation(25, 13));
        }

        [Fact]
        public void CS0271ERR_InaccessibleGetter05()
        {
            CreateCompilation(
@"class A
{
    public object this[int x] { protected get { return null; } set { } }
    internal object this[string s] { private get { return null; } set { } }
    void M()
    {
        object o = new B()[""hello""]; // no error
        o = new A()[""hello""]; // no error
    }
}
class B : A
{
    void M()
    {
        object o = new B()[0]; // no error
        o = this[0]; // no error
        o = this[""hello""]; // CS0271
    }
}
class C
{
    void M()
    {
        object o = new B()[0]; // CS0271
        o = new A()[""hello""]; // CS0271
    }
}")
            .VerifyDiagnostics(
                // (17,13): error CS0271: The property or indexer 'A.this[string]' cannot be used in this context because the get accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, @"this[""hello""]").WithArguments("A.this[string]"),
                // (24,20): error CS0271: The property or indexer 'A.this[int]' cannot be used in this context because the get accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "new B()[0]").WithArguments("A.this[int]"),
                // (25,13): error CS0271: The property or indexer 'A.this[string]' cannot be used in this context because the get accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, @"new A()[""hello""]").WithArguments("A.this[string]"));
        }

        [Fact]
        public void CS0272ERR_InaccessibleSetter01()
        {
            var source =
@"namespace N
{
    class C
    {
        internal object P { get; private set; }
        static public C Q { get { return null; } protected set { } }
    }
    class P
    {
        static void M(C c)
        {
            c.P = c;
            C.Q = c;
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,13): error CS0272: The property or indexer 'N.C.P' cannot be used in this context because the set accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "c.P").WithArguments("N.C.P").WithLocation(12, 13),
                // (13,13): error CS0272: The property or indexer 'N.C.Q' cannot be used in this context because the set accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "C.Q").WithArguments("N.C.Q").WithLocation(13, 13));
        }

        [Fact]
        public void CS0272ERR_InaccessibleSetter02()
        {
            var source =
@"namespace N1
{
    abstract class A
    {
        public virtual object P { get; protected set; }
    }
}
namespace N2
{
    class B : N1.A
    {
        public override object P { get { return null; } }
        void M()
        {
            P = null; // no error
        }
    }
}
class C
{
    void M(N2.B b)
    {
        b.P = null; // CS0272
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (23,9): error CS0272: The property or indexer 'N2.B.P' cannot be used in this context because the set accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "b.P").WithArguments("N2.B.P").WithLocation(23, 9));
        }

        [Fact]
        public void CS0272ERR_InaccessibleSetter03()
        {
            CreateCompilation(
@"namespace N1
{
    abstract class A
    {
        public virtual object this[int x] { get { return null; } protected set { } }
    }
}
namespace N2
{
    class B : N1.A
    {
        public override object this[int x] { get { return null; } }
        void M()
        {
            this[0] = null; // no error
        }
    }
}
class C
{
    void M(N2.B b)
    {
        b[0] = null; // CS0272
    }
}
")
            .VerifyDiagnostics(
                // (23,9): error CS0272: The property or indexer 'N2.B.this[int]' cannot be used in this context because the set accessor is inaccessible
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "b[0]").WithArguments("N2.B.this[int]"));
        }

        [Fact]
        public void CS0283ERR_BadConstType()
        {
            // Test for both ERR_BadConstType and an error for RHS to ensure
            // the RHS is not reported multiple times (when calculating the
            // constant value for the symbol and also when binding).
            var source =
@"struct S
{
    static void M(object o)
    {
        const S s = 2;
        M(s);
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,15): error CS0283: The type 'S' cannot be declared const
                //         const S s = 2;
                Diagnostic(ErrorCode.ERR_BadConstType, "S").WithArguments("S").WithLocation(5, 15),
                // (5,21): error CS0029: Cannot implicitly convert type 'int' to 'S'
                //         const S s = 2;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "2").WithArguments("int", "S").WithLocation(5, 21));
        }

        [Fact]
        public void CS0304ERR_NoNewTyvar01()
        {
            var source =
@"struct S<T, U> where U : new()
{
    void M<V>()
    {
        object o;
        o = new T();
        o = new U();
        o = new V();
    }
}
class C<T, U>
    where T : struct
    where U : class
{
    void M<V, W>()
        where V : struct
        where W : class, new()
    {
        object o;
        o = new T();
        o = new U();
        o = new V();
        o = new W();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,13): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new T()").WithArguments("T").WithLocation(6, 13),
                // (8, 13): error CS0304: Cannot create an instance of the variable type 'V' because it does not have the new() constraint
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new V()").WithArguments("V").WithLocation(8, 13),
                // (21,13): error CS0304: Cannot create an instance of the variable type 'U' because it does not have the new() constraint
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new U()").WithArguments("U").WithLocation(21, 13));
        }

        [WorkItem(542377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542377")]
        [Fact]
        public void CS0304ERR_NoNewTyvar02()
        {
            var source =
@"struct S { }
class C { }
abstract class A<T>
{
    public abstract U F<U>() where U : T;
}
class B1 : A<int>
{
    public override U F<U>() { return new U(); }
}
class B2 : A<S>
{
    public override U F<U>() { return new U(); }
}
class B3 : A<C>
{
    public override U F<U>() { return new U(); }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (17,39): error CS0304: Cannot create an instance of the variable type 'U' because it does not have the new() constraint
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new U()").WithArguments("U").WithLocation(17, 39));
        }

        [WorkItem(542547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542547")]
        [Fact]
        public void CS0305ERR_BadArity()
        {
            var text = @"
public class NormalType
{
    public static int M1<T1>(T1 p1, T1 p2) { return 0; }
    public static int Main()
    {
        M1<int, >(10, 11);
        return -1;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,17): error CS1031: Type expected
                Diagnostic(ErrorCode.ERR_TypeExpected, ">"),
                // (7,9): error CS0305: Using the generic method 'NormalType.M1<T1>(T1, T1)' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "M1<int, >").WithArguments("NormalType.M1<T1>(T1, T1)", "method", "1"));
        }

        [Fact]
        public void CS0310ERR_NewConstraintNotSatisfied01()
        {
            var text =
@"class A<T> { }
class B
{
    private B() { }
}
delegate void D();
enum E { }
struct S { }
class C<T, U> where T : new()
{
    static void M<V>() where V : new()
    {
        M<A<int>>();
        M<B>();
        M<D>();
        M<E>();
        M<object>();
        M<int>();
        M<S>();
        M<T>();
        M<U>();
        M<B, B>();
        M<T, U>();
        M<T, V>();
        M<T[]>();
        M<dynamic>();
    }
    static void M<V, W>()
        where V : new()
        where W : new()
    {
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (14,9): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'V' in the generic type or method 'C<T, U>.M<V>()'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<B>").WithArguments("C<T, U>.M<V>()", "V", "B").WithLocation(14, 9),
                // (15,9): error CS0310: 'D' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'V' in the generic type or method 'C<T, U>.M<V>()'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<D>").WithArguments("C<T, U>.M<V>()", "V", "D").WithLocation(15, 9),
                // (21,9): error CS0310: 'U' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'V' in the generic type or method 'C<T, U>.M<V>()'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<U>").WithArguments("C<T, U>.M<V>()", "V", "U").WithLocation(21, 9),
                // (22,9): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'V' in the generic type or method 'C<T, U>.M<V, W>()'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<B, B>").WithArguments("C<T, U>.M<V, W>()", "V", "B").WithLocation(22, 9),
                // (22,9): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'W' in the generic type or method 'C<T, U>.M<V, W>()'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<B, B>").WithArguments("C<T, U>.M<V, W>()", "W", "B").WithLocation(22, 9),
                // (23,9): error CS0310: 'U' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'W' in the generic type or method 'C<T, U>.M<V, W>()'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<T, U>").WithArguments("C<T, U>.M<V, W>()", "W", "U").WithLocation(23, 9),
                // (25,9): error CS0310: 'T[]' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'V' in the generic type or method 'C<T, U>.M<V>()'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<T[]>").WithArguments("C<T, U>.M<V>()", "V", "T[]").WithLocation(25, 9));
        }

        [Fact]
        public void CS0310ERR_NewConstraintNotSatisfied02()
        {
            var text =
@"class A { }
class B
{
    internal B() { }
}
class C<T> where T : new()
{
    internal static void M<U>() where U : new() { }
    internal static void E<U>(D<U> d) { } // Error: missing constraint on E<U> to satisfy constraint on D<U>
}
delegate T D<T>() where T : new();
static class E
{
    internal static void M<T>(this object o) where T : new() { }
    internal static void F<T>(D<T> d) where T : new() { }
}
class F<T, U> where U : new()
{
}
abstract class G { }
class H : G { }
interface I { }
struct S
{
    private S(object o) { }
    static void M()
    {
        C<A>.M<A>();
        C<A>.M<B>();
        C<B>.M<A>();
        C<B>.M<B>();
        C<G>.M<H>();
        C<H>.M<G>();
        C<I>.M<S>();
        E.F(S.F<A>);
        E.F(S.F<B>);
        E.F(S.F<C<A>>);
        E.F(S.F<C<B>>);
        var o = new object();
        o.M<A>();
        o.M<B>();
        o = new F<A, B>();
        o = new F<B, A>();
    }
    static T F<T>() { return default(T); }
}";

            // Note that none of these errors except the first one are reported by the native compiler, because
            // it does not report additional errors after an error is found in a formal parameter of a method.

            CreateCompilationWithMscorlib40(text, references: new[] { SystemCoreRef }).VerifyDiagnostics(
                // (9,36): error CS0310: 'U' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'D<T>'
                //     internal static void E<U>(D<U> d) { } // Error: missing constraint on E<U> to satisfy constraint on D<U>
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "d").WithArguments("D<T>", "T", "U").WithLocation(9, 36),
                // (29,14): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'U' in the generic type or method 'C<A>.M<U>()'
                //         C<A>.M<B>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<B>").WithArguments("C<A>.M<U>()", "U", "B").WithLocation(29, 14),
                // (30,11): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //         C<B>.M<A>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "B").WithArguments("C<T>", "T", "B").WithLocation(30, 11),
                // (31,11): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //         C<B>.M<B>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "B").WithArguments("C<T>", "T", "B").WithLocation(31, 11),
                // (31,14): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'U' in the generic type or method 'C<B>.M<U>()'
                //         C<B>.M<B>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<B>").WithArguments("C<B>.M<U>()", "U", "B").WithLocation(31, 14),
                // (32,11): error CS0310: 'G' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //         C<G>.M<H>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "G").WithArguments("C<T>", "T", "G").WithLocation(32, 11),
                // (33,14): error CS0310: 'G' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'U' in the generic type or method 'C<H>.M<U>()'
                //         C<H>.M<G>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<G>").WithArguments("C<H>.M<U>()", "U", "G").WithLocation(33, 14),
                // (34,11): error CS0310: 'I' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //         C<I>.M<S>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "I").WithArguments("C<T>", "T", "I").WithLocation(34, 11),
                // (36,11): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'E.F<T>(D<T>)'
                //         E.F(S.F<B>);
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "F").WithArguments("E.F<T>(D<T>)", "T", "B").WithLocation(36, 11),
                // (38,19): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //         E.F(S.F<C<B>>);
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "B").WithArguments("C<T>", "T", "B").WithLocation(38, 19),

                // This invocation of E.F(S.F<C<B>>) is an extremely interesting one. 

                // First off, obviously the type argument for S.F is prima facie wrong, so we give an error for that above.
                // But what about the overload resolution problem in error recovery? Even though the argument is bad we still
                // might want to try to get an overload resolution result. Thus we must infer a type for T in E.F<T>(D<T>). 
                // We must do overload resolution on an invocation S.F<C<B>>(). Overload resolution succeeds; it has no reason
                // to fail. (Overload resolution would fail if a formal parameter type of S.F<C<B>>() did not satisfy one of its
                // constraints, but there are no formal parameters. Also, there are no constraints at all on T in S.F<T>.)
                //
                // Thus T in D<T> is inferred to be C<B>, and thus T in E.F<T> is inferred to be C<B>. 
                //
                // Now we check to see whether E.F<C<B>>(D<C<B>>) is applicable. It is inapplicable because
                // B fails to meet the constraints of T in C<T>. (C<B> does not fail to meet the constraints
                // of T in D<T> because C<B> has a public default parameterless ctor.)
                //
                // Therefore E.F<C.B>(S.F<C<B>>) fails overload resolution. Why? Because B is not valid for T in C<T>.
                // (We cannot say that the constraints on T in E.F<T> is unmet because again, C<B> meets the
                // constraint; it has a ctor.) So that is the error we report.
                //
                // This is arguably a "cascading" error; we have already reported an error for C<B> when the 
                // argument was bound. Normally we avoid reporting "cascading" errors in overload resolution by
                // saying that an erroneous argument is implicitly convertible to any formal parameter type;
                // thus we avoid an erroneous expression from causing overload resolution to make every
                // candidate method inapplicable. (Though it might cause overload resolution to fail by making
                // every candidate method applicable, causing an ambiguity!)  But the overload resolution 
                // error here is not caused by an argument *conversion* in the first place; the overload
                // resolution error is caused because *the deduced formal parameter type is illegal.*
                //
                // We might want to put some gear in place to suppress this cascading error. It is not
                // entirely clear what that machinery might look like.

                // (38,11): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //         E.F(S.F<C<B>>);
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "F").WithArguments("C<T>", "T", "B").WithLocation(38, 11),

                // (41,11): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'E.M<T>(object)'
                //         o.M<B>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<B>").WithArguments("E.M<T>(object)", "T", "B").WithLocation(41, 11),
                // (42,22): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'U' in the generic type or method 'F<T, U>'
                //         o = new F<A, B>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "B").WithArguments("F<T, U>", "U", "B").WithLocation(42, 22));
        }

        [Fact]
        public void CS0310ERR_NewConstraintNotSatisfied03()
        {
            var text =
@"class A { }
class B
{
    private B() { }
}
class C<T, U> where U : struct
{
    internal static void M<V>(V v) where V : new() { }
    void M()
    {
        A a = default(A);
        M(a);
        a.E();
        B b = default(B);
        M(b);
        b.E();
        T t = default(T);
        M(t);
        t.E();
        U u1 = default(U);
        M(u1);
        u1.E();
        U? u2 = null;
        M(u2);
        u2.E();
    }
}
static class S
{
    internal static void E<T>(this T t) where T : new() { }
}";
            CreateCompilationWithMscorlib40(text, references: new[] { SystemCoreRef }).VerifyDiagnostics(
                // (15,9): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'V' in the generic type or method 'C<T, U>.M<V>(V)'
                //         M(b);
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M").WithArguments("C<T, U>.M<V>(V)", "V", "B").WithLocation(15, 9),
                // (16,11): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'S.E<T>(T)'
                //         b.E();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "E").WithArguments("S.E<T>(T)", "T", "B").WithLocation(16, 11),
                // (18,9): error CS0310: 'T' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'V' in the generic type or method 'C<T, U>.M<V>(V)'
                //         M(t);
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M").WithArguments("C<T, U>.M<V>(V)", "V", "T").WithLocation(18, 9),
                // (19,11): error CS0310: 'T' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'S.E<T>(T)'
                //         t.E();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "E").WithArguments("S.E<T>(T)", "T", "T").WithLocation(19, 11)
);
        }

        /// <summary>
        /// Constraint errors within aliases.
        /// </summary>
        [Fact]
        public void CS0310ERR_NewConstraintNotSatisfied04()
        {
            var text =
@"using NA = N.A;
using NB = N.B;
using CA = N.C<N.A>;
using CB = N.C<N.B>;
namespace N
{
    using CAD = C<N.A>.D;
    using CBD = C<N.B>.D;
    class A { } // public (default) .ctor
    class B { private B() { } } // private .ctor
    class C<T> where T : new()
    {
        internal static void M<U>() where U : new() { }
        internal class D
        {
            private D() { } // private .ctor
            internal static void M<U>() where U : new() { }
        }
    }
    class E
    {
        static void M()
        {
            C<N.A>.M<N.B>();
            C<NB>.M<NA>();
            C<C<N.A>.D>.M<N.A>();
            C<N.A>.D.M<N.B>();
            C<N.B>.D.M<N.A>();
            CA.M<N.B>();
            CB.M<N.A>();
            CAD.M<N.B>();
            CBD.M<N.A>();
            C<CAD>.M<N.A>();
            C<CBD>.M<N.A>();
        }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,7): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                // using CB = N.C<N.B>;
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "CB").WithArguments("N.C<T>", "T", "N.B").WithLocation(4, 7),
                // (8,11): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //     using CBD = C<N.B>.D;
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "CBD").WithArguments("N.C<T>", "T", "N.B").WithLocation(8, 11),
                // (24,20): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'U' in the generic type or method 'C<A>.M<U>()'
                //             C<N.A>.M<N.B>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<N.B>").WithArguments("N.C<N.A>.M<U>()", "U", "N.B").WithLocation(24, 20),
                // (25,15): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //             C<NB>.M<NA>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "NB").WithArguments("N.C<T>", "T", "N.B").WithLocation(25, 15),
                // (26,15): error CS0310: 'C<A>.D' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //             C<C<N.A>.D>.M<N.A>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "C<N.A>.D").WithArguments("N.C<T>", "T", "N.C<N.A>.D").WithLocation(26, 15),
                // (27,22): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'U' in the generic type or method 'C<A>.D.M<U>()'
                //             C<N.A>.D.M<N.B>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<N.B>").WithArguments("N.C<N.A>.D.M<U>()", "U", "N.B").WithLocation(27, 22),
                // (28,15): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //             C<N.B>.D.M<N.A>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "N.B").WithArguments("N.C<T>", "T", "N.B").WithLocation(28, 15),
                // (29,16): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'U' in the generic type or method 'C<A>.M<U>()'
                //             CA.M<N.B>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<N.B>").WithArguments("N.C<N.A>.M<U>()", "U", "N.B").WithLocation(29, 16),
                // (31,17): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'U' in the generic type or method 'C<A>.D.M<U>()'
                //             CAD.M<N.B>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<N.B>").WithArguments("N.C<N.A>.D.M<U>()", "U", "N.B").WithLocation(31, 17),
                // (33,15): error CS0310: 'C<A>.D' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //             C<CAD>.M<N.A>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "CAD").WithArguments("N.C<T>", "T", "N.C<N.A>.D").WithLocation(33, 15),
                // (34,15): error CS0310: 'C<B>.D' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //             C<CBD>.M<N.A>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "CBD").WithArguments("N.C<T>", "T", "N.C<N.B>.D").WithLocation(34, 15));
        }

        /// <summary>
        /// Constructors with optional and params args
        /// should not be considered parameterless.
        /// </summary>
        [Fact]
        public void CS0310ERR_NewConstraintNotSatisfied05()
        {
            var text =
@"class A
{
    public A() { }
}
class B
{
    public B(object o = null) { }
}
class C
{
    public C(params object[] args) { }
}
class D<T> where T : new()
{
    static void M()
    {
        D<A>.M();
        D<B>.M();
        D<C>.M();
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (18,11): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'D<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "B").WithArguments("D<T>", "T", "B").WithLocation(18, 11),
                // (19,11): error CS0310: 'C' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'D<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "C").WithArguments("D<T>", "T", "C").WithLocation(19, 11));
        }

        [Fact]
        public void CS0311ERR_GenericConstraintNotSatisfiedRefType01()
        {
            var source =
@"class A { }
class B { }
class C<T> where T : A { }
class D
{
    static void M<T>() where T : A { }
    static void M()
    {
        object o = new C<B>();
        M<B>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,26): error CS0311: The type 'B' cannot be used as type parameter 'T' in the generic type or method 'C<T>'. There is no implicit reference conversion from 'B' to 'A'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "B").WithArguments("C<T>", "A", "T", "B").WithLocation(9, 26),
                // (10,9): error CS0311: The type 'B' cannot be used as type parameter 'T' in the generic type or method 'D.M<T>()'. There is no implicit reference conversion from 'B' to 'A'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<B>").WithArguments("D.M<T>()", "A", "T", "B").WithLocation(10, 9));
        }

        [Fact]
        public void CS0311ERR_GenericConstraintNotSatisfiedRefType02()
        {
            var source =
@"class C<T, U> where U : T
{
    void M<V>() where V : C<T, V> { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,12): error CS0311: The type 'V' cannot be used as type parameter 'U' in the generic type or method 'C<T, U>'. There is no implicit reference conversion from 'V' to 'T'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "V").WithArguments("C<T, U>", "T", "U", "V").WithLocation(3, 12));
        }

        [Fact]
        public void CS0311ERR_GenericConstraintNotSatisfiedRefType03()
        {
            var source =
@"interface I<T> where T : I<I<T>> { }";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,13): error CS0311: The type 'I<T>' cannot be used as type parameter 'T' in the generic type or method 'I<T>'. There is no implicit reference conversion from 'I<T>' to 'I<I<I<T>>>'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "T").WithArguments("I<T>", "I<I<I<T>>>", "T", "I<T>").WithLocation(1, 13));
        }

        [Fact]
        public void CS0311ERR_GenericConstraintNotSatisfiedRefType04()
        {
            var source =
@"interface IA<T> { }
interface IB<T> where T : IA<T> { }
class C<T1, T2, T3>
    where T1 : IB<object[]>
    where T2 : IB<T2>
    where T3 : IB<IB<T3>[]>, IA<T3>
{
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,9): error CS0311: The type 'object[]' cannot be used as type parameter 'T' in the generic type or method 'IB<T>'. There is no implicit reference conversion from 'object[]' to 'IA<object[]>'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "T1").WithArguments("IB<T>", "IA<object[]>", "T", "object[]").WithLocation(3, 9),
                // (3,13): error CS0311: The type 'T2' cannot be used as type parameter 'T' in the generic type or method 'IB<T>'. There is no boxing conversion or type parameter conversion from 'T2' to 'IA<T2>'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "T2").WithArguments("IB<T>", "IA<T2>", "T", "T2").WithLocation(3, 13),
                // (3,17): error CS0311: The type 'IB<T3>[]' cannot be used as type parameter 'T' in the generic type or method 'IB<T>'. There is no implicit reference conversion from 'IB<T3>[]' to 'IA<IB<T3>[]>'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "T3").WithArguments("IB<T>", "IA<IB<T3>[]>", "T", "IB<T3>[]").WithLocation(3, 17));
        }

        [Fact]
        public void CS0311ERR_GenericConstraintNotSatisfiedRefType05()
        {
            var source =
@"namespace N
{
    class C<T, U> where U : T
    {
        static object F()
        {
            return null;
        }
        static object G<V>() where V : T
        {
            return null;
        }
        static void M()
        {
            object o;
            o = C<int, object>.F();
            o = N.C<int, int>.G<string>();
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,24): error CS0311: The type 'object' cannot be used as type parameter 'U' in the generic type or method 'C<T, U>'. There is no implicit reference conversion from 'object' to 'int'.
                //             o = C<int, object>.F();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "object").WithArguments("N.C<T, U>", "int", "U", "object").WithLocation(16, 24),
                // (17,31): error CS0311: The type 'string' cannot be used as type parameter 'V' in the generic type or method 'C<int, int>.G<V>()'. There is no implicit reference conversion from 'string' to 'int'.
                //             o = N.C<int, int>.G<string>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "G<string>").WithArguments("N.C<int, int>.G<V>()", "int", "V", "string").WithLocation(17, 31));
        }

        [Fact]
        public void CS0312ERR_GenericConstraintNotSatisfiedNullableEnum()
        {
            var source =
@"class A<T, U> where T : U { }
class B<T>
{
    static void M<U>() where U : T { }
    static void M()
    {
        object o = new A<int?, int>();
        B<int>.M<int?>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,26): error CS0312: The type 'int?' cannot be used as type parameter 'T' in the generic type or method 'A<T, U>'. The nullable type 'int?' does not satisfy the constraint of 'int'.
                //         object o = new A<int?, int>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedNullableEnum, "int?").WithArguments("A<T, U>", "int", "T", "int?").WithLocation(7, 26),
                // (8,16): error CS0312: The type 'int?' cannot be used as type parameter 'U' in the generic type or method 'B<int>.M<U>()'. The nullable type 'int?' does not satisfy the constraint of 'int'.
                //         B<int>.M<int?>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedNullableEnum, "M<int?>").WithArguments("B<int>.M<U>()", "int", "U", "int?").WithLocation(8, 16));
        }

        [Fact]
        public void CS0313ERR_GenericConstraintNotSatisfiedNullableInterface()
        {
            var source =
@"interface I { }
struct S : I { }
class A<T> where T : I { }
class B
{
    static void M<T>() where T : I { }
    static void M()
    {
        object o = new A<S?>();
        M<S?>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,26): error CS0313: The type 'S?' cannot be used as type parameter 'T' in the generic type or method 'A<T>'. The nullable type 'S?' does not satisfy the constraint of 'I'. Nullable types can not satisfy any interface constraints.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedNullableInterface, "S?").WithArguments("A<T>", "I", "T", "S?").WithLocation(9, 26),
                // (10,9): error CS0313: The type 'S?' cannot be used as type parameter 'T' in the generic type or method 'B.M<T>()'. The nullable type 'S?' does not satisfy the constraint of 'I'. Nullable types can not satisfy any interface constraints.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedNullableInterface, "M<S?>").WithArguments("B.M<T>()", "I", "T", "S?").WithLocation(10, 9));
        }

        [Fact]
        public void CS0314ERR_GenericConstraintNotSatisfiedTyVar01()
        {
            var source =
@"class A { }
class B<T> where T : A { }
class C<T> where T : struct
{
    static void M<U>() where U : A { }
    static void M()
    {
        object o = new B<T>();
        M<T>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,26): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'B<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'A'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "T").WithArguments("B<T>", "A", "T", "T").WithLocation(8, 26),
                // (9,9): error CS0314: The type 'T' cannot be used as type parameter 'U' in the generic type or method 'C<T>.M<U>()'. There is no boxing conversion or type parameter conversion from 'T' to 'A'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "M<T>").WithArguments("C<T>.M<U>()", "A", "U", "T").WithLocation(9, 9));
        }

        [Fact]
        public void CS0314ERR_GenericConstraintNotSatisfiedTyVar02()
        {
            var source =
@"class C<T, U> where U : T
{
    void M<V>() where V : C<V, U> { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,12): error CS0314: The type 'U' cannot be used as type parameter 'U' in the generic type or method 'C<T, U>'. There is no boxing conversion or type parameter conversion from 'U' to 'V'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "V").WithArguments("C<T, U>", "V", "U", "U").WithLocation(3, 12));
        }

        [Fact]
        public void CS0314ERR_GenericConstraintNotSatisfiedTyVar03()
        {
            var source =
@"interface IA<T> where T : IB<T> { }
interface IB<T> where T : IA<T> { }";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,14): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'IB<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'IA<T>'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "T").WithArguments("IB<T>", "IA<T>", "T", "T").WithLocation(1, 14),
                // (2,14): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'IA<T>'. There is no boxing conversion or type parameter conversion from 'T' to 'IB<T>'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "T").WithArguments("IA<T>", "IB<T>", "T", "T").WithLocation(2, 14));
        }

        [Fact]
        public void CS0315ERR_GenericConstraintNotSatisfiedValType()
        {
            var source =
@"class A { }
class B<T> where T : A { }
struct S { }
class C
{
    static void M<T, U>() where U : A { }
    static void M()
    {
        object o = new B<S>();
        M<int, double>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,26): error CS0315: The type 'S' cannot be used as type parameter 'T' in the generic type or method 'B<T>'. There is no boxing conversion from 'S' to 'A'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "S").WithArguments("B<T>", "A", "T", "S").WithLocation(9, 26),
                // (10,9): error CS0315: The type 'double?' cannot be used as type parameter 'U' in the generic type or method 'C.M<T, U>()'. There is no boxing conversion from 'double?' to 'A'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M<int, double>").WithArguments("C.M<T, U>()", "A", "U", "double").WithLocation(10, 9));
        }

        [Fact]
        public void CS0316ERR_DuplicateGeneratedName()
        {
            var text = @"
public class Test
{
    public int this[int value] // CS0316
    {
        get { return 1; }
        set { }
    }

    public int this[char @value] // CS0316
    {
        get { return 1; }
        set { }
    }

    public int this[string value] // no error since no setter
    {
        get { return 1; }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,26): error CS0316: The parameter name 'value' conflicts with an automatically-generated parameter name
                //     public int this[char @value] // CS0316
                Diagnostic(ErrorCode.ERR_DuplicateGeneratedName, "@value").WithArguments("value").WithLocation(10, 26),
                // (4,25): error CS0316: The parameter name 'value' conflicts with an automatically-generated parameter name
                //     public int this[int value] // CS0316
                Diagnostic(ErrorCode.ERR_DuplicateGeneratedName, "value").WithArguments("value").WithLocation(4, 25));
        }

        [Fact]
        public void CS0403ERR_TypeVarCantBeNull()
        {
            var source =
@"interface I { }
class A { }
class B<T1, T2, T3, T4, T5, T6, T7>
    where T2 : class
    where T3 : struct
    where T4 : new()
    where T5 : I
    where T6 : A
    where T7 : T1
{
    static void M()
    {
        T1 t1 = null;
        T2 t2 = null;
        T3 t3 = null;
        T4 t4 = null;
        T5 t5 = null;
        T6 t6 = null;
        T7 t7 = null;
    }
    static T1 F1() { return null; }
    static T2 F2() { return null; }
    static T3 F3() { return null; }
    static T4 F4() { return null; }
    static T5 F5() { return null; }
    static T6 F6() { return null; }
    static T7 F7() { return null; }
}";
            CreateCompilation(source).VerifyDiagnostics(
    // (13,17): error CS0403: Cannot convert null to type parameter 'T1' because it could be a non-nullable value type. Consider using 'default(T1)' instead.
    //         T1 t1 = null;
    Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T1"),
    // (15,17): error CS0403: Cannot convert null to type parameter 'T3' because it could be a non-nullable value type. Consider using 'default(T3)' instead.
    //         T3 t3 = null;
    Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T3"),
    // (16,17): error CS0403: Cannot convert null to type parameter 'T4' because it could be a non-nullable value type. Consider using 'default(T4)' instead.
    //         T4 t4 = null;
    Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T4"),
    // (17,17): error CS0403: Cannot convert null to type parameter 'T5' because it could be a non-nullable value type. Consider using 'default(T5)' instead.
    //         T5 t5 = null;
    Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T5"),
    // (19,17): error CS0403: Cannot convert null to type parameter 'T7' because it could be a non-nullable value type. Consider using 'default(T7)' instead.
    //         T7 t7 = null;
    Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T7"),
    // (14,12): warning CS0219: The variable 't2' is assigned but its value is never used
    //         T2 t2 = null;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "t2").WithArguments("t2"),
    // (18,12): warning CS0219: The variable 't6' is assigned but its value is never used
    //         T6 t6 = null;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "t6").WithArguments("t6"),
    // (21,29): error CS0403: Cannot convert null to type parameter 'T1' because it could be a non-nullable value type. Consider using 'default(T1)' instead.
    //     static T1 F1() { return null; }
    Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T1"),
    // (23,29): error CS0403: Cannot convert null to type parameter 'T3' because it could be a non-nullable value type. Consider using 'default(T3)' instead.
    //     static T3 F3() { return null; }
    Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T3"),
    // (24,29): error CS0403: Cannot convert null to type parameter 'T4' because it could be a non-nullable value type. Consider using 'default(T4)' instead.
    //     static T4 F4() { return null; }
    Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T4"),
    // (25,29): error CS0403: Cannot convert null to type parameter 'T5' because it could be a non-nullable value type. Consider using 'default(T5)' instead.
    //     static T5 F5() { return null; }
    Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T5"),
    // (27,29): error CS0403: Cannot convert null to type parameter 'T7' because it could be a non-nullable value type. Consider using 'default(T7)' instead.
    //     static T7 F7() { return null; }
    Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T7")
            );
        }

        [WorkItem(539901, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539901")]
        [Fact]
        public void CS0407ERR_BadRetType_01()
        {
            var text = @"
public delegate int MyDelegate();

class C
{
    MyDelegate d;

    public C()
    {
        d = new MyDelegate(F);  // OK: F returns int
        d = new MyDelegate(G);  // CS0407 - G doesn't return int
    }

    public int F()
    {
        return 1;
    }

    public void G()
    {
    }

    public static void Main()
    {
        C c1 = new C();
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.WithoutImprovedOverloadCandidates).VerifyDiagnostics(
                // (11,28): error CS0407: 'void C.G()' has the wrong return type
                //         d = new MyDelegate(G);  // CS0407 - G doesn't return int
                Diagnostic(ErrorCode.ERR_BadRetType, "G").WithArguments("C.G()", "void").WithLocation(11, 28)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (11,28): error CS0407: 'void C.G()' has the wrong return type
                //         d = new MyDelegate(G);  // CS0407 - G doesn't return int
                Diagnostic(ErrorCode.ERR_BadRetType, "G").WithArguments("C.G()", "void").WithLocation(11, 28)
                );
        }

        [WorkItem(925899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/925899")]
        [Fact]
        public void CS0407ERR_BadRetType_02()
        {
            var text = @"
using System;

class C
{
    public static void Main()
    {
        var oo = new Func<object, object>(x => 1);
 
        var os = new Func<object, string>(oo);
        var ss = new Func<string, string>(oo);
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.WithoutImprovedOverloadCandidates).VerifyDiagnostics(
                // (10,43): error CS0407: 'object System.Func<object, object>.Invoke(object)' has the wrong return type
                //         var os = new Func<object, string>(oo);
                Diagnostic(ErrorCode.ERR_BadRetType, "oo").WithArguments("System.Func<object, object>.Invoke(object)", "object").WithLocation(10, 43),
                // (11,43): error CS0407: 'object System.Func<object, object>.Invoke(object)' has the wrong return type
                //         var ss = new Func<string, string>(oo);
                Diagnostic(ErrorCode.ERR_BadRetType, "oo").WithArguments("System.Func<object, object>.Invoke(object)", "object").WithLocation(11, 43)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (10,43): error CS0407: 'object Func<object, object>.Invoke(object)' has the wrong return type
                //         var os = new Func<object, string>(oo);
                Diagnostic(ErrorCode.ERR_BadRetType, "oo").WithArguments("System.Func<object, object>.Invoke(object)", "object").WithLocation(10, 43),
                // (11,43): error CS0407: 'object Func<object, object>.Invoke(object)' has the wrong return type
                //         var ss = new Func<string, string>(oo);
                Diagnostic(ErrorCode.ERR_BadRetType, "oo").WithArguments("System.Func<object, object>.Invoke(object)", "object").WithLocation(11, 43)
                );
        }

        [WorkItem(539924, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539924")]
        [Fact]
        public void CS0407ERR_BadRetType_03()
        {
            var text = @"
delegate DerivedClass MyDerivedDelegate(DerivedClass x);
public class BaseClass
{
    public static BaseClass DelegatedMethod(BaseClass x)
    {
        System.Console.WriteLine(""Base"");
        return x;
    }
}
public class DerivedClass : BaseClass
{
    public static DerivedClass DelegatedMethod(DerivedClass x)
    {
        System.Console.WriteLine(""Derived"");
        return x;
    }
    static void Main(string[] args)
    {
        MyDerivedDelegate goo1 = null;
        goo1 += BaseClass.DelegatedMethod;
        goo1 += DerivedClass.DelegatedMethod;
        goo1(new DerivedClass());
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (21,17): error CS0407: 'BaseClass BaseClass.DelegatedMethod(BaseClass)' has the wrong return type
                Diagnostic(ErrorCode.ERR_BadRetType, "BaseClass.DelegatedMethod").WithArguments("BaseClass.DelegatedMethod(BaseClass)", "BaseClass"));
        }

        [WorkItem(3401, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CS0411ERR_CantInferMethTypeArgs01()
        {
            var text = @"
class C
{
    public void F<T>(T t) where T : C 
    {
    }

    public static void Main()
    {
        C c = new C();
        c.F(null);  // CS0411
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_CantInferMethTypeArgs, Line = 11, Column = 11 } });
        }

        [WorkItem(2099, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/2099")]
        [Fact(Skip = "529560")]
        public void CS0411ERR_CantInferMethTypeArgs02()
        {
            var text = @"
public class MemberInitializerTest
{
    delegate void D<T>();
    public static void GenericMethod<T>() { }
    public static void Run()
    {
        var genD = (D<int>)GenericMethod;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,20): error CS0030: The type arguments for method 'MemberInitializerTest.GenericMethod<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         var genD = (D<int>)GenericMethod;
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "(D<int>)GenericMethod").WithArguments("MemberInitializerTest.GenericMethod<T>()")
                );
        }

        [Fact]
        public void CS0412ERR_LocalSameNameAsTypeParam()
        {
            var text = @"
using System;

class C
{
    // Parameter name is the same as method type parameter name
    public void G<T>(int T)  // CS0412
    {
    }
    public void F<T>()
    {
        // Method local variable name is the same as method type
        // parameter name
        double T = 0.0;  // CS0412
        Console.WriteLine(T);
    }

    public static void Main()
    {
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_LocalSameNameAsTypeParam, Line = 7, Column = 26 },
                                            new ErrorDescription { Code = (int)ErrorCode.ERR_LocalSameNameAsTypeParam, Line = 14, Column = 16 } });
        }

        [Fact]
        public void CS0413ERR_AsWithTypeVar()
        {
            var source =
@"interface I { }
class A { }
class B<T1, T2, T3, T4, T5, T6, T7>
    where T2 : class
    where T3 : struct
    where T4 : new()
    where T5 : I
    where T6 : A
    where T7 : T1
{
    static void M(object o)
    {
        o = o as T1;
        o = o as T2;
        o = o as T3;
        o = o as T4;
        o = o as T5;
        o = o as T6;
        o = o as T7;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,13): error CS0413: The type parameter 'T1' cannot be used with the 'as' operator because it does not have a class type constraint nor a 'class' constraint
                Diagnostic(ErrorCode.ERR_AsWithTypeVar, "o as T1").WithArguments("T1").WithLocation(13, 13),
                // (15,13): error CS0413: The type parameter 'T3' cannot be used with the 'as' operator because it does not have a class type constraint nor a 'class' constraint
                Diagnostic(ErrorCode.ERR_AsWithTypeVar, "o as T3").WithArguments("T3").WithLocation(15, 13),
                // (16,13): error CS0413: The type parameter 'T4' cannot be used with the 'as' operator because it does not have a class type constraint nor a 'class' constraint
                Diagnostic(ErrorCode.ERR_AsWithTypeVar, "o as T4").WithArguments("T4").WithLocation(16, 13),
                // (17,13): error CS0413: The type parameter 'T5' cannot be used with the 'as' operator because it does not have a class type constraint nor a 'class' constraint
                Diagnostic(ErrorCode.ERR_AsWithTypeVar, "o as T5").WithArguments("T5").WithLocation(17, 13),
                // (19,13): error CS0413: The type parameter 'T7' cannot be used with the 'as' operator because it does not have a class type constraint nor a 'class' constraint
                Diagnostic(ErrorCode.ERR_AsWithTypeVar, "o as T7").WithArguments("T7").WithLocation(19, 13));
        }

        [Fact]
        public void CS0417ERR_NewTyvarWithArgs01()
        {
            var source =
@"struct S<T> where T : new()
{
    T F(object o)
    {
        return new T(o);
    }
    U G<U, V>(object o)
        where U : new()
        where V : struct
    {
        return new U(new V(o));
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,16): error CS0417: 'T': cannot provide arguments when creating an instance of a variable type
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "new T(o)").WithArguments("T").WithLocation(5, 16),
                // (11,16): error CS0417: 'U': cannot provide arguments when creating an instance of a variable type
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "new U(new V(o))").WithArguments("U").WithLocation(11, 16),
                // (11,22): error CS0417: 'V': cannot provide arguments when creating an instance of a variable type
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "new V(o)").WithArguments("V").WithLocation(11, 22));
        }

        [Fact]
        public void CS0417ERR_NewTyvarWithArgs02()
        {
            var source =
@"class C
{
    public C() { }
    public C(object o) { }
    static void M<T>() where T : C, new()
    {
        new T();
        new T(null);
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,9): error CS0417: 'T': cannot provide arguments when creating an instance of a variable type
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "new T(null)").WithArguments("T").WithLocation(8, 9));
        }

        [Fact]
        public void CS0428ERR_MethGrpToNonDel()
        {
            var text = @"
namespace ConsoleApplication1
{
    class Program
    {
        delegate int Del1();
        delegate object Del2();

        static void Main(string[] args)
        {
            ExampleClass ec = new ExampleClass();
            int i = ec.Method1;
            Del1 d1 = ec.Method1;
            i = ec.Method1();
            ec = ExampleClass.Method2;
            Del2 d2 = ExampleClass.Method2;
            ec = ExampleClass.Method2();
        }
    }

    public class ExampleClass
    {
        public int Method1() { return 1; }
        public static ExampleClass Method2() { return null; }
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_MethGrpToNonDel, Line = 12, Column = 24 },
                                            new ErrorDescription { Code = (int)ErrorCode.ERR_MethGrpToNonDel, Line = 15, Column = 31 }});
        }

        [Fact, WorkItem(528649, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528649")]
        public void CS0431ERR_ColColWithTypeAlias()
        {
            var text = @"
using AliasC = C;
class C
{
    public class Goo { }
}
class Test
{
    class C { }
    static int Main()
    {
        AliasC::Goo goo = new AliasC::Goo();
        return 0;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_ColColWithTypeAlias, "AliasC").WithArguments("AliasC"),
                Diagnostic(ErrorCode.ERR_ColColWithTypeAlias, "AliasC").WithArguments("AliasC"));
        }

        [WorkItem(3402, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CS0445ERR_UnboxNotLValue()
        {
            var text = @"
namespace ConsoleApplication1
{
    // CS0445.CS
    class UnboxingTest
    {
        public static void Main()
        {
            Point p = new Point();
            p.x = 1;
            p.y = 5;
            object obj = p;

            // Generates CS0445:
            ((Point)obj).x = 2;
        }
    }

    public struct Point
    {
        public int x;
        public int y;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_UnboxNotLValue, Line = 15, Column = 13 } });
        }

        [Fact]
        public void CS0446ERR_AnonMethGrpInForEach()
        {
            var text = @"
class Tester 
{
    static void Main() 
    {
        int[] intArray = new int[5];
        foreach (int i in M) { } // CS0446
    }
    static void M() { }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonMethGrpInForEach, Line = 7, Column = 27 } });
        }

        [Fact]
        [WorkItem(36203, "https://github.com/dotnet/roslyn/issues/36203")]
        public void CS0452_GenericConstraintError_HasHigherPriorityThanMethodOverloadError()
        {
            var code = @"
class Code
{
    void GenericMethod<T>(int i) where T: class => throw null;
    void GenericMethod<T>(string s) => throw null;

    void IncorrectMethodCall()
    {
        GenericMethod<int>(1);
    }
}";
            CreateCompilation(code).VerifyDiagnostics(
                // (9,9): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'Code.GenericMethod<T>(int)'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "GenericMethod<int>").WithArguments("Code.GenericMethod<T>(int)", "T", "int").WithLocation(9, 9));
        }

        [Fact]
        public void CS0457ERR_AmbigUDConv()
        {
            var text = @"
public class A { }

public class G0 {  }
public class G1<R> : G0 {  }

public class H0 {
   public static implicit operator G0(H0 h) {
      return new G0();
   }
}
public class H1<R> : H0 {
   public static implicit operator G1<R>(H1<R> h) {
      return new G1<R>();
   }
}

public class Test 
{
   public static void F0(G0 g) {  }
   public static void Main() 
   {
      H1<A> h1a = new H1<A>();
      F0(h1a);   // CS0457
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (24,10): error CS0457: Ambiguous user defined conversions 'H1<A>.implicit operator G1<A>(H1<A>)' and 'H0.implicit operator G0(H0)' when converting from 'H1<A>' to 'G0'
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "h1a").WithArguments("H1<A>.implicit operator G1<A>(H1<A>)", "H0.implicit operator G0(H0)", "H1<A>", "G0"));
        }

        [WorkItem(22306, "https://github.com/dotnet/roslyn/issues/22306")]
        [Fact]
        public void AddrOnReadOnlyLocal()
        {
            var text = @"
class A
{
    public unsafe void M1()
    {
        int[] ints = new int[] { 1, 2, 3 };
        foreach (int i in ints)
        {
            int *j = &i;  
        }

        fixed (int *i = &_i)
        {
            int **j = &i;  
        }
    }

    private int _i = 0;
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void CS0463ERR_DecConstError()
        {
            var text = @"
using System; 
class MyClass 
{
    public static void Main()    
    {
        const decimal myDec = 79000000000000000000000000000.0m + 79000000000000000000000000000.0m; // CS0463
        Console.WriteLine(myDec.ToString());
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_DecConstError, Line = 7, Column = 31 } });
        }

        [WorkItem(543272, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543272")]
        [Fact]
        public void CS0463ERR_DecConstError_02()
        {
            var text = @"
class MyClass 
{
    public static void Main()    
    {
        decimal x1 = decimal.MaxValue + 1;                  // CS0463
        decimal x2 = decimal.MaxValue + decimal.One;        // CS0463
        decimal x3 = decimal.MinValue - decimal.One;        // CS0463
        decimal x4 = decimal.MinValue + decimal.MinusOne;   // CS0463
        decimal x5 = decimal.MaxValue - decimal.MinValue;   // CS0463        
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,22): error CS0463: Evaluation of the decimal constant expression failed
                //         decimal x1 = decimal.MaxValue + 1;                  // CS0463
                Diagnostic(ErrorCode.ERR_DecConstError, "decimal.MaxValue + 1"),
                // (7,22): error CS0463: Evaluation of the decimal constant expression failed
                //         decimal x2 = decimal.MaxValue + decimal.One;        // CS0463
                Diagnostic(ErrorCode.ERR_DecConstError, "decimal.MaxValue + decimal.One"),
                // (8,22): error CS0463: Evaluation of the decimal constant expression failed
                //         decimal x3 = decimal.MinValue - decimal.One;        // CS0463
                Diagnostic(ErrorCode.ERR_DecConstError, "decimal.MinValue - decimal.One"),
                // (9,22): error CS0463: Evaluation of the decimal constant expression failed
                //         decimal x4 = decimal.MinValue + decimal.MinusOne;   // CS0463
                Diagnostic(ErrorCode.ERR_DecConstError, "decimal.MinValue + decimal.MinusOne"),
                // (10,22): error CS0463: Evaluation of the decimal constant expression failed
                //         decimal x5 = decimal.MaxValue - decimal.MinValue;   // CS0463        
                Diagnostic(ErrorCode.ERR_DecConstError, "decimal.MaxValue - decimal.MinValue"));
        }

        [Fact()]
        public void CS0471ERR_TypeArgsNotAllowedAmbig()
        {
            var text = @"
class Test
{
    public void F(bool x, bool y) {}
    public void F1()
    {
        int a = 1, b = 2, c = 3;
        F(a<b, c>(3));    // CS0471
        // To resolve, try the following instead:
        // F((a<b), c>(3));
    }
}

";
            //Dev11 used to give 'The {1} '{0}' is not a generic method. If you intended an expression list, use parentheses around the &lt; expression.'
            //Roslyn will be satisfied with something less helpful.

            var noWarns = new Dictionary<string, ReportDiagnostic>();
            noWarns.Add(MessageProvider.Instance.GetIdForErrorCode(219), ReportDiagnostic.Suppress);

            CreateCompilation(text, options: TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(noWarns)).VerifyDiagnostics(
                // (8,13): error CS0118: 'b' is a variable but is used like a type
                //         F(a<b, c>(3));    // CS0471
                Diagnostic(ErrorCode.ERR_BadSKknown, "b").WithArguments("b", "variable", "type"),
                // (8,16): error CS0118: 'c' is a variable but is used like a type
                //         F(a<b, c>(3));    // CS0471
                Diagnostic(ErrorCode.ERR_BadSKknown, "c").WithArguments("c", "variable", "type"),
                // (8,11): error CS0307: The variable 'a' cannot be used with type arguments
                //         F(a<b, c>(3));    // CS0471
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "a<b, c>").WithArguments("a", "variable"));
        }

        [Fact]
        public void CS0516ERR_RecursiveConstructorCall()
        {
            var text = @"
namespace x
{
   public class clx
   {
      public clx() : this()   // CS0516
      {
      }

      public static void Main()
      {
      }
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,22): error CS0516: Constructor 'x.clx.clx()' cannot call itself
                Diagnostic(ErrorCode.ERR_RecursiveConstructorCall, "this").WithArguments("x.clx.clx()"));
        }

        [WorkItem(751825, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/751825")]
        [Fact]
        public void Repro751825()
        {
            var text = @"
public class A : A<int>
{
    public A() : base() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (2,18): error CS0308: The non-generic type 'A' cannot be used with type arguments
                // public class A : A<int>
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "A<int>").WithArguments("A", "type"),
                // (4,18): error CS0516: Constructor 'A.A()' cannot call itself
                //     public A() : base() { }
                Diagnostic(ErrorCode.ERR_RecursiveConstructorCall, "base").WithArguments("A.A()"));
        }

        [WorkItem(366, "https://github.com/dotnet/roslyn/issues/366")]
        [Fact]
        public void IndirectConstructorCycle()
        {
            var text = @"
public class A
{
    public A() : this(1) {}
    public A(int x) : this(string.Empty) {}
    public A(string s) : this(1) {}
    public A(long l) : this(double.MaxValue) {}
    public A(double d) : this(char.MaxValue) {}
    public A(char c) : this(long.MaxValue) {}
    public A(short s) : this() {}
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,24): error CS0768: Constructor 'A.A(string)' cannot call itself through another constructor
                //     public A(string s) : this(1) {}
                Diagnostic(ErrorCode.ERR_IndirectRecursiveConstructorCall, ": this(1)").WithArguments("A.A(string)").WithLocation(6, 24),
                // (9,22): error CS0768: Constructor 'A.A(char)' cannot call itself through another constructor
                //     public A(char c) : this(long.MaxValue) {}
                Diagnostic(ErrorCode.ERR_IndirectRecursiveConstructorCall, ": this(long.MaxValue)").WithArguments("A.A(char)").WithLocation(9, 22)
                );
        }

        [Fact]
        public void CS0517ERR_ObjectCallingBaseConstructor()
        {
            var text = @"namespace System
{
    public class Void { } //just need the type to be defined

    public class Object
    {
        public Object() : base() { }
    }
}
";
            CreateEmptyCompilation(text).VerifyDiagnostics(
                // (7,16): error CS0517: 'object' has no base class and cannot call a base constructor
                Diagnostic(ErrorCode.ERR_ObjectCallingBaseConstructor, "Object").WithArguments("object"));
        }

        [Fact]
        public void CS0522ERR_StructWithBaseConstructorCall()
        {
            var text = @"
public class clx
{
   public clx(int i)
   {
   }

   public static void Main()
   {
   }
}

public struct cly
{
   public cly(int i):base(0)   // CS0522
   // try the following line instead
   // public cly(int i)
   {
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (15,11): error CS0522: 'cly': structs cannot call base class constructors
                Diagnostic(ErrorCode.ERR_StructWithBaseConstructorCall, "cly").WithArguments("cly"));
        }

        [Fact]
        public void CS0543ERR_EnumeratorOverflow01()
        {
            var source =
@"enum E
{
    A = int.MaxValue - 1,
    B,
    C, // CS0543
    D,
    E = C,
    F,
    G = B,
    H, // CS0543
    I
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,5): error CS0543: 'E.C': the enumerator value is too large to fit in its type
                //     C, // CS0543
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "C").WithArguments("E.C").WithLocation(5, 5),
                // (10,5): error CS0543: 'E.H': the enumerator value is too large to fit in its type
                //     H, // CS0543
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "H").WithArguments("E.H").WithLocation(10, 5));
        }

        [Fact]
        public void CS0543ERR_EnumeratorOverflow02()
        {
            var source =
@"namespace N
{
    enum E : byte { A = 255, B, C }
    enum F : short { A = 0x00ff, B = 0x7f00, C = A | B, D }
    enum G : int { X = int.MinValue, Y = X - 1, Z }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,30): error CS0543: 'E.B': the enumerator value is too large to fit in its type
                //     enum E : byte { A = 255, B, C }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "B").WithArguments("N.E.B").WithLocation(3, 30),
                // (5,42): error CS0220: The operation overflows at compile time in checked mode
                //     enum G : int { X = int.MinValue, Y = X - 1, Z }
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "X - 1").WithLocation(5, 42),
                // (4,57): error CS0543: 'F.D': the enumerator value is too large to fit in its type
                //     enum F : short { A = 0x00ff, B = 0x7f00, C = A | B, D }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "D").WithArguments("N.F.D").WithLocation(4, 57));
        }

        [Fact]
        public void CS0543ERR_EnumeratorOverflow03()
        {
            var source =
@"enum S8 : sbyte { A = sbyte.MinValue, B, C, D = -1, E, F, G = sbyte.MaxValue - 2, H, I, J, K }
enum S16 : short { A = short.MinValue, B, C, D = -1, E, F, G = short.MaxValue - 2, H, I, J, K }
enum S32 : int { A = int.MinValue, B, C, D = -1, E, F, G = int.MaxValue - 2, H, I, J, K }
enum S64 : long { A = long.MinValue, B, C, D = -1, E, F, G = long.MaxValue - 2, H, I, J, K }
enum U8 : byte { A = 0, B, C, D = byte.MaxValue - 2, E, F, G, H }
enum U16 : ushort { A = 0, B, C, D = ushort.MaxValue - 2, E, F, G, H }
enum U32 : uint { A = 0, B, C, D = uint.MaxValue - 2, E, F, G, H }
enum U64 : ulong { A = 0, B, C, D = ulong.MaxValue - 2, E, F, G, H }
";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,84): error CS0543: 'S32.J': the enumerator value is too large to fit in its type
                // enum S32 : int { A = int.MinValue, B, C, D = -1, E, F, G = int.MaxValue - 2, H, I, J, K }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "J").WithArguments("S32.J").WithLocation(3, 84),
                // (4,87): error CS0543: 'S64.J': the enumerator value is too large to fit in its type
                // enum S64 : long { A = long.MinValue, B, C, D = -1, E, F, G = long.MaxValue - 2, H, I, J, K }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "J").WithArguments("S64.J").WithLocation(4, 87),
                // (7,61): error CS0543: 'U32.G': the enumerator value is too large to fit in its type
                // enum U32 : uint { A = 0, B, C, D = uint.MaxValue - 2, E, F, G, H }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "G").WithArguments("U32.G").WithLocation(7, 61),
                // (6,65): error CS0543: 'U16.G': the enumerator value is too large to fit in its type
                // enum U16 : ushort { A = 0, B, C, D = ushort.MaxValue - 2, E, F, G, H }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "G").WithArguments("U16.G").WithLocation(6, 65),
                // (5,60): error CS0543: 'U8.G': the enumerator value is too large to fit in its type
                // enum U8 : byte { A = 0, B, C, D = byte.MaxValue - 2, E, F, G, H }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "G").WithArguments("U8.G").WithLocation(5, 60),
                // (2,90): error CS0543: 'S16.J': the enumerator value is too large to fit in its type
                // enum S16 : short { A = short.MinValue, B, C, D = -1, E, F, G = short.MaxValue - 2, H, I, J, K }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "J").WithArguments("S16.J").WithLocation(2, 90),
                // (1,89): error CS0543: 'S8.J': the enumerator value is too large to fit in its type
                // enum S8 : sbyte { A = sbyte.MinValue, B, C, D = -1, E, F, G = sbyte.MaxValue - 2, H, I, J, K }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "J").WithArguments("S8.J").WithLocation(1, 89),
                // (8,63): error CS0543: 'U64.G': the enumerator value is too large to fit in its type
                // enum U64 : ulong { A = 0, B, C, D = ulong.MaxValue - 2, E, F, G, H }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "G").WithArguments("U64.G").WithLocation(8, 63));
        }

        [Fact]
        public void CS0543ERR_EnumeratorOverflow04()
        {
            string source = string.Format(
@"enum A {0}
enum B : byte {1}
enum C : byte {2}
enum D : sbyte {3}",
                  CreateEnumValues(300, "E"),
                  CreateEnumValues(256, "E"),
                  CreateEnumValues(300, "E"),
                  CreateEnumValues(300, "E", sbyte.MinValue));

            CreateCompilation(source).VerifyDiagnostics(
                // (3,1443): error CS0543: 'C.E256': the enumerator value is too large to fit in its type
                // enum C : byte { E0, E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16, E17, E18, E19, E20, E21, E22, E23, E24, E25, E26, E27, E28, E29, E30, E31, E32, E33, E34, E35, E36, E37, E38, E39, E40, E41, E42, E43, E44, E45, E46, E47, E48, E49, E50, E51, E52, E53, E54, E55, E56, E57, E58, E59, E60, E61, E62, E63, E64, E65, E66, E67, E68, E69, E70, E71, E72, E73, E74, E75, E76, E77, E78, E79, E80, E81, E82, E83, E84, E85, E86, E87, E88, E89, E90, E91, E92, E93, E94, E95, E96, E97, E98, E99, E100, E101, E102, E103, E104, E105, E106, E107, E108, E109, E110, E111, E112, E113, E114, E115, E116, E117, E118, E119, E120, E121, E122, E123, E124, E125, E126, E127, E128, E129, E130, E131, E132, E133, E134, E135, E136, E137, E138, E139, E140, E141, E142, E143, E144, E145, E146, E147, E148, E149, E150, E151, E152, E153, E154, E155, E156, E157, E158, E159, E160, E161, E162, E163, E164, E165, E166, E167, E168, E169, E170, E171, E172, E173, E174, E175, E176, E177, E178, E179, E180, E181, E182, E183, E184, E185, E186, E187, E188, E189, E190, E191, E192, E193, E194, E195, E196, E197, E198, E199, E200, E201, E202, E203, E204, E205, E206, E207, E208, E209, E210, E211, E212, E213, E214, E215, E216, E217, E218, E219, E220, E221, E222, E223, E224, E225, E226, E227, E228, E229, E230, E231, E232, E233, E234, E235, E236, E237, E238, E239, E240, E241, E242, E243, E244, E245, E246, E247, E248, E249, E250, E251, E252, E253, E254, E255, E256, E257, E258, E259, E260, E261, E262, E263, E264, E265, E266, E267, E268, E269, E270, E271, E272, E273, E274, E275, E276, E277, E278, E279, E280, E281, E282, E283, E284, E285, E286, E287, E288, E289, E290, E291, E292, E293, E294, E295, E296, E297, E298, E299,  }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "E256").WithArguments("C.E256").WithLocation(3, 1443),
                // (4,1451): error CS0543: 'D.E256': the enumerator value is too large to fit in its type
                // enum D : sbyte { E0 = -128, E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16, E17, E18, E19, E20, E21, E22, E23, E24, E25, E26, E27, E28, E29, E30, E31, E32, E33, E34, E35, E36, E37, E38, E39, E40, E41, E42, E43, E44, E45, E46, E47, E48, E49, E50, E51, E52, E53, E54, E55, E56, E57, E58, E59, E60, E61, E62, E63, E64, E65, E66, E67, E68, E69, E70, E71, E72, E73, E74, E75, E76, E77, E78, E79, E80, E81, E82, E83, E84, E85, E86, E87, E88, E89, E90, E91, E92, E93, E94, E95, E96, E97, E98, E99, E100, E101, E102, E103, E104, E105, E106, E107, E108, E109, E110, E111, E112, E113, E114, E115, E116, E117, E118, E119, E120, E121, E122, E123, E124, E125, E126, E127, E128, E129, E130, E131, E132, E133, E134, E135, E136, E137, E138, E139, E140, E141, E142, E143, E144, E145, E146, E147, E148, E149, E150, E151, E152, E153, E154, E155, E156, E157, E158, E159, E160, E161, E162, E163, E164, E165, E166, E167, E168, E169, E170, E171, E172, E173, E174, E175, E176, E177, E178, E179, E180, E181, E182, E183, E184, E185, E186, E187, E188, E189, E190, E191, E192, E193, E194, E195, E196, E197, E198, E199, E200, E201, E202, E203, E204, E205, E206, E207, E208, E209, E210, E211, E212, E213, E214, E215, E216, E217, E218, E219, E220, E221, E222, E223, E224, E225, E226, E227, E228, E229, E230, E231, E232, E233, E234, E235, E236, E237, E238, E239, E240, E241, E242, E243, E244, E245, E246, E247, E248, E249, E250, E251, E252, E253, E254, E255, E256, E257, E258, E259, E260, E261, E262, E263, E264, E265, E266, E267, E268, E269, E270, E271, E272, E273, E274, E275, E276, E277, E278, E279, E280, E281, E282, E283, E284, E285, E286, E287, E288, E289, E290, E291, E292, E293, E294, E295, E296, E297, E298, E299,  }
                Diagnostic(ErrorCode.ERR_EnumeratorOverflow, "E256").WithArguments("D.E256").WithLocation(4, 1451));
        }

        // Create string "{ E0, E1, ..., En }"
        private static string CreateEnumValues(int count, string prefix, int? initialValue = null)
        {
            var builder = new System.Text.StringBuilder("{ ");
            for (int i = 0; i < count; i++)
            {
                builder.Append(prefix);
                builder.Append(i);
                if ((i == 0) && (initialValue != null))
                {
                    builder.AppendFormat(" = {0}", initialValue.Value);
                }
                builder.Append(", ");
            }
            builder.Append(" }");
            return builder.ToString();
        }

        // CS0570 --> Symbols\OverriddenOrHiddenMembersTests.cs

        [Fact]
        public void CS0571ERR_CantCallSpecialMethod01()
        {
            var source =
@"class C
{
    protected virtual object P { get; set; }
    static object Q { get; set; }
    void M(D d)
    {
        this.set_P(get_Q());
        D.set_Q(d.get_P());
        ((this.get_P))();
    }
}
class D : C
{
    protected override object P { get { return null; } set { } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,20): error CS0571: 'C.Q.get': cannot explicitly call operator or accessor
                //         this.set_P(get_Q());
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_Q").WithArguments("C.Q.get").WithLocation(7, 20),
                // (7,14): error CS0571: 'C.P.set': cannot explicitly call operator or accessor
                //         this.set_P(get_Q());
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_P").WithArguments("C.P.set").WithLocation(7, 14),
                // (8,19): error CS0571: 'D.P.get': cannot explicitly call operator or accessor
                //         D.set_Q(d.get_P());
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_P").WithArguments("D.P.get").WithLocation(8, 19),
                // (8,11): error CS0571: 'C.Q.set': cannot explicitly call operator or accessor
                //         D.set_Q(d.get_P());
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_Q").WithArguments("C.Q.set").WithLocation(8, 11),
                // (9,16): error CS0571: 'C.P.get': cannot explicitly call operator or accessor
                //         ((this.get_P))();
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_P").WithArguments("C.P.get").WithLocation(9, 16));

            // CONSIDER: Dev10 reports 'C.P.get' for the fourth error.  Roslyn reports 'D.P.get'
            // because it is in the more-derived type and because Binder.LookupMembersInClass
            // calls MergeHidingLookups(D.P.get, C.P.get) with both arguments non-viable
            // (i.e. keeps current, since new value isn't better).
        }

        [Fact]
        public void CS0571ERR_CantCallSpecialMethod02()
        {
            var source =
@"using System;
namespace A.B
{
    class C
    {
        object P { get; set; }
        static object Q { get { return 0; } set { } }
        void M(C c)
        {
            Func<object> f = get_P;
            f = C.get_Q;
            Action<object> a = c.set_P;
            a = set_Q;
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,30): error CS0571: 'C.P.get': cannot explicitly call operator or accessor
                //             Func<object> f = get_P;
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_P").WithArguments("A.B.C.P.get").WithLocation(10, 30),
                // (11,19): error CS0571: 'C.Q.get': cannot explicitly call operator or accessor
                //             f = C.get_Q;
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_Q").WithArguments("A.B.C.Q.get").WithLocation(11, 19),
                // (12,34): error CS0571: 'C.P.set': cannot explicitly call operator or accessor
                //             Action<object> a = c.set_P;
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_P").WithArguments("A.B.C.P.set").WithLocation(12, 34),
                // (13,17): error CS0571: 'C.Q.set': cannot explicitly call operator or accessor
                //             a = set_Q;
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_Q").WithArguments("A.B.C.Q.set").WithLocation(13, 17));
        }

        /// <summary>
        /// No errors should be reported if method with
        /// accessor name is defined in different class.
        /// </summary>
        [Fact]
        public void CS0571ERR_CantCallSpecialMethod03()
        {
            var source =
@"class A
{
    public object get_P() { return null; }
}
class B : A
{
    public object P { get; set; }
    void M()
    {
        object o = this.P;
        o = this.get_P();
    }
}
class C
{
    void M(B b)
    {
        object o = b.P;
        o = b.get_P();
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact()]
        public void CS0571ERR_CantCallSpecialMethod04()
        {
            var compilation = CreateCompilation(
@"public class MyClass
{
    public static MyClass operator ++(MyClass c)
    {
        return null;
    }
    public static void M()
    {
        op_Increment(null);   // CS0571
    }
}
").VerifyDiagnostics(
                // (9,9): error CS0571: 'MyClass.operator ++(MyClass)': cannot explicitly call operator or accessor
                //         op_Increment(null);   // CS0571
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "op_Increment").WithArguments("MyClass.operator ++(MyClass)"));
        }

        [Fact]
        public void CS0571ERR_CantCallSpecialMethod05()
        {
            var source =
@"
using System;
public class C
{
    public static void M()
    {
        IntPtr.op_Addition(default(IntPtr), 0);
        IntPtr.op_Subtraction(default(IntPtr), 0);
        IntPtr.op_Equality(default(IntPtr), default(IntPtr));
        IntPtr.op_Inequality(default(IntPtr), default(IntPtr));
        IntPtr.op_Explicit(0);
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,16): error CS0571: 'IntPtr.operator +(IntPtr, int)': cannot explicitly call operator or accessor
                //         IntPtr.op_Addition(default(IntPtr), 0);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "op_Addition").WithArguments("System.IntPtr.operator +(System.IntPtr, int)").WithLocation(7, 16),
                // (8,16): error CS0571: 'IntPtr.operator -(IntPtr, int)': cannot explicitly call operator or accessor
                //         IntPtr.op_Subtraction(default(IntPtr), 0);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "op_Subtraction").WithArguments("System.IntPtr.operator -(System.IntPtr, int)").WithLocation(8, 16),
                // (9,16): error CS0571: 'IntPtr.operator ==(IntPtr, IntPtr)': cannot explicitly call operator or accessor
                //         IntPtr.op_Equality(default(IntPtr), default(IntPtr));
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "op_Equality").WithArguments("System.IntPtr.operator ==(System.IntPtr, System.IntPtr)").WithLocation(9, 16),
                // (10,16): error CS0571: 'IntPtr.operator !=(IntPtr, IntPtr)': cannot explicitly call operator or accessor
                //         IntPtr.op_Inequality(default(IntPtr), default(IntPtr));
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "op_Inequality").WithArguments("System.IntPtr.operator !=(System.IntPtr, System.IntPtr)").WithLocation(10, 16),
                // (11,16): error CS0571: 'IntPtr.explicit operator IntPtr(int)': cannot explicitly call operator or accessor
                //         IntPtr.op_Explicit(0);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "op_Explicit").WithArguments("System.IntPtr.explicit operator System.IntPtr(int)").WithLocation(11, 16));
        }

        [Fact]
        public void CS0572ERR_BadTypeReference()
        {
            var text = @"
using System;
class C
{
   public class Inner
   {
      public static int v = 9;
   }
}

class D : C
{
   public static void Main()
   {
      C cValue = new C();
      Console.WriteLine(cValue.Inner.v);   // CS0572
      // try the following line instead
      // Console.WriteLine(C.Inner.v);
   }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadTypeReference, Line = 16, Column = 32 } });
        }

        [Fact]
        public void CS0574ERR_BadDestructorName()
        {
            var test = @"
namespace x
{
    public class iii
    {
        ~iiii(){}
        public static void Main()
        {
        }
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (6,10): error CS0574: Name of destructor must match name of class
                //         ~iiii(){}
                Diagnostic(ErrorCode.ERR_BadDestructorName, "iiii").WithLocation(6, 10));
        }

        [WorkItem(541951, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541951")]
        [Fact]
        public void CS0611ERR_ArrayElementCantBeRefAny()
        {
            var text = @"
public class Test
{
    public System.TypedReference[] x;
    public System.RuntimeArgumentHandle[][] y;
}
";
            var comp = CreateCompilation(text, targetFramework: TargetFramework.Mscorlib45);
            comp.VerifyDiagnostics(
                // (4,12): error CS0611: Array elements cannot be of type 'System.TypedReference'
                //     public System.TypedReference[] x;
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(4, 12),
                // (5,12): error CS0611: Array elements cannot be of type 'System.RuntimeArgumentHandle'
                //     public System.RuntimeArgumentHandle[][] y;
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "System.RuntimeArgumentHandle").WithArguments("System.RuntimeArgumentHandle").WithLocation(5, 12));
        }

        [WorkItem(541951, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541951")]
        [Fact]
        public void CS0611ERR_ArrayElementCantBeRefAny_1()
        {
            var text =
@"using System;
class C
{
    static void M()
    {
        var x = new[] { new ArgIterator() };
        var y = new[] { new TypedReference() };
        var z = new[] { new RuntimeArgumentHandle() };
    }
}";
            var comp = CreateCompilation(text, targetFramework: TargetFramework.Mscorlib45);
            comp.VerifyDiagnostics(
                // (6,17): error CS0611: Array elements cannot be of type 'System.ArgIterator'
                //         var x = new[] { new ArgIterator() };
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "new[] { new ArgIterator() }").WithArguments("System.ArgIterator").WithLocation(6, 17),
                // (7,17): error CS0611: Array elements cannot be of type 'System.TypedReference'
                //         var y = new[] { new TypedReference() };
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "new[] { new TypedReference() }").WithArguments("System.TypedReference").WithLocation(7, 17),
                // (8,17): error CS0611: Array elements cannot be of type 'System.RuntimeArgumentHandle'
                //         var z = new[] { new RuntimeArgumentHandle() };
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "new[] { new RuntimeArgumentHandle() }").WithArguments("System.RuntimeArgumentHandle").WithLocation(8, 17));
        }

        [Fact, WorkItem(546062, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546062")]
        public void CS0619ERR_DeprecatedSymbolStr()
        {
            var text = @"
using System;
namespace a
{
    [Obsolete]
    class C1 { }

    [Obsolete(""Obsolescence message"", true)]
    interface I1 { }

    public class CI1 : I1 { }

    public class MainClass
    {
        public static void Main()
        {
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,24): error CS0619: 'a.I1' is obsolete: 'Obsolescence message'
                //     public class CI1 : I1 { }
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "I1").WithArguments("a.I1", "Obsolescence message")
                );
        }

        [Fact]
        public void CS0622ERR_ArrayInitToNonArrayType()
        {
            var text = @"
public class Test
{
    public static void Main ()
    {
        Test t = { new Test() };   // CS0622
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_ArrayInitToNonArrayType, Line = 6, Column = 18 } });
        }

        [Fact]
        public void CS0623ERR_ArrayInitInBadPlace()
        {
            var text = @"
class X
{
    public void goo(int a)
    {
        int[] x = { { 4 } }; //CS0623
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_ArrayInitInBadPlace, Line = 6, Column = 21 } });
        }

        [Fact]
        public void CS0631ERR_IllegalRefParam()
        {
            var compilation = CreateCompilation(
@"interface I
{
    object this[ref object index] { get; set; }
}
class C
{
    internal object this[object x, out object y] { get { y = null; return null; } }
}
struct S
{
    internal object this[out int x, out int y] { set { x = 0; y = 0; } }
}");
            compilation.VerifyDiagnostics(
                // (3,17): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(3, 17),
                // (7,36): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out").WithLocation(7, 36),
                // (11,26): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out").WithLocation(11, 26),
                // (11,37): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out").WithLocation(11, 37));
        }

        [WorkItem(529305, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529305")]
        [Fact()]
        public void CS0664ERR_LiteralDoubleCast()
        {
            var text = @"
class Example
{
    static void Main()
    {
        // CS0664, because 1.0 is interpreted as a double:
        decimal d1 = 1.0;
        float f1 = 2.0;
    }
}";
            var compilation = CreateCompilation(text);
            compilation.VerifyDiagnostics(
                // (7,22): error CS0664: Literal of type double cannot be implicitly converted to type 'decimal'; use an 'M' suffix to create a literal of this type
                //         decimal d1 = 1.0;
                Diagnostic(ErrorCode.ERR_LiteralDoubleCast, "1.0").WithArguments("M", "decimal").WithLocation(7, 22),
                // (8,20): error CS0664: Literal of type double cannot be implicitly converted to type 'float'; use an 'F' suffix to create a literal of this type
                //         float f1 = 2.0;
                Diagnostic(ErrorCode.ERR_LiteralDoubleCast, "2.0").WithArguments("F", "float").WithLocation(8, 20),
                // (7,17): warning CS0219: The variable 'd1' is assigned but its value is never used
                //         decimal d1 = 1.0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "d1").WithArguments("d1").WithLocation(7, 17),
                // (8,15): warning CS0219: The variable 'f1' is assigned but its value is never used
                //         float f1 = 2.0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "f1").WithArguments("f1").WithLocation(8, 15));
        }

        [Fact]
        public void CS0670ERR_FieldCantHaveVoidType()
        {
            CreateCompilation(@"
class C 
{
    void x = default(void); 
}").VerifyDiagnostics(
                // (4,22): error CS1547: Keyword 'void' cannot be used in this context
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void"),
                // (4,5): error CS0670: Field cannot have void type
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "void"));
        }

        [Fact]
        public void CS0670ERR_FieldCantHaveVoidType_Var()
        {
            CreateCompilationWithMscorlib45(@"
var x = default(void); 
", parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (2,17): error CS1547: Keyword 'void' cannot be used in this context
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void"),
                // (2,1): error CS0670: Field cannot have void type
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "var"));
        }

        [WorkItem(538016, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538016")]
        [Fact]
        public void CS0687ERR_AliasQualAsExpression()
        {
            var text = @"
using M = Test;
using System;

public class Test
{
    public static int x = 77;

    public static void Main() 
    {
        Console.WriteLine(M::x); // CS0687
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_ColColWithTypeAlias, "M").WithArguments("M")
            );
        }

        [Fact]
        public void CS0704ERR_LookupInTypeVariable()
        {
            var text =
@"using System;
class A
{
    internal class B : Attribute { }
    internal class C<T> { }
}
class D<T> where T : A
{
    class E : T.B { }
    interface I<U> where U : T.B { }
    [T.B]
    static object M<U>()
    {
        T.C<object> b1 = new T.C<object>();
        T<U>.B b2 = null;
        b1 = default(T.B);
        object o = typeof(T.C<A>);
        o = o as T.B;
        return b1;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,15): error CS0704: Cannot do member lookup in 'T' because it is a type parameter class E : T.B { }
                Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T.B").WithArguments("T"),
                // (10,30): error CS0704: Cannot do member lookup in 'T' because it is a type parameter
                //     interface I<U> where U : T.B { }
                Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T.B").WithArguments("T"),
                // (11,6): error CS0704: Cannot do member lookup in 'T' because it is a type parameter
                //     [T.B]
                Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T.B").WithArguments("T"),
                // (14,9): error CS0704: Cannot do member lookup in 'T' because it is a type parameter
                //         T.C<object> b1 = new T.C<object>();
                Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T.C<object>").WithArguments("T"),
                // (14,30): error CS0704: Cannot do member lookup in 'T' because it is a type parameter
                //         T.C<object> b1 = new T.C<object>();
                Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T.C<object>").WithArguments("T"),
                // (15,9): error CS0307: The type parameter 'T' cannot be used with type arguments
                //         T<U>.B b2 = null;
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "T<U>").WithArguments("T", "type parameter"),
                // (16,22): error CS0704: Cannot do member lookup in 'T' because it is a type parameter
                //         b1 = default(T.B);
                Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T.B").WithArguments("T"),
                // (17,27): error CS0704: Cannot do member lookup in 'T' because it is a type parameter
                //         object o = typeof(T.C<A>);
                Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T.C<A>").WithArguments("T"),
                // (18,18): error CS0704: Cannot do member lookup in 'T' because it is a type parameter
                //         o = o as T.B;
                Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T.B").WithArguments("T")
                );
        }

        [Fact]
        public void CS0712ERR_InstantiatingStaticClass()
        {
            var text = @"
public static class SC
{
}

public class CMain
{
    public static void Main()
    {
        SC sc = new SC();  // CS0712
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_InstantiatingStaticClass, Line = 10, Column = 17 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_VarDeclIsStaticClass, Line = 10, Column = 9 }});
        }

        [Fact]
        public void CS0716ERR_ConvertToStaticClass()
        {
            var text = @"
public static class SC
{
    static void F() { }
}

public class Test
{
    public static void Main()
    {
        object o = new object();
        System.Console.WriteLine((SC)o);  // CS0716
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_ConvertToStaticClass, Line = 12, Column = 34 } });
        }

        [Fact]
        [WorkItem(36203, "https://github.com/dotnet/roslyn/issues/36203")]
        public void CS0718_StaticClassError_HasHigherPriorityThanMethodOverloadError()
        {
            var code = @"
static class StaticClass { }

class Code
{
    void GenericMethod<T>(int i) => throw null;
    void GenericMethod<T>(string s) => throw null;

    void IncorrectMethodCall()
    {
        GenericMethod<StaticClass>(1);
    }
}";
            CreateCompilation(code).VerifyDiagnostics(
                // (11,9): error CS0718: 'StaticClass': static types cannot be used as type arguments
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "GenericMethod<StaticClass>").WithArguments("StaticClass").WithLocation(11, 9));
        }

        [Fact]
        public void CS0723ERR_VarDeclIsStaticClass_Locals()
        {
            CreateCompilation(
@"static class SC
{
    static void M()
    {
        SC sc = null;  // CS0723
        N(sc);
        var sc2 = new SC();
    }
    static void N(object o)
    {
    }
}").VerifyDiagnostics(
            // (5,9): error CS0723: Cannot declare a variable of static type 'SC'
            Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "SC").WithArguments("SC"),
            // (7,19): error CS0712: Cannot create an instance of the static class 'SC'
            Diagnostic(ErrorCode.ERR_InstantiatingStaticClass, "new SC()").WithArguments("SC"),
            // (7,9): error CS0723: Cannot declare a variable of static type 'SC'
            Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "var").WithArguments("SC"));
        }

        [Fact]
        public void CS0723ERR_VarDeclIsStaticClass_Fields()
        {
            CreateCompilationWithMscorlib45(@"
static class SC {} 

var sc2 = new SC();
", parseOptions: TestOptions.Script).VerifyDiagnostics(
            // (4,5): error CS0723: Cannot declare a variable of static type 'SC'
            Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "sc2").WithArguments("SC"),
            // (4,11): error CS0712: Cannot create an instance of the static class 'SC'
            Diagnostic(ErrorCode.ERR_InstantiatingStaticClass, "new SC()").WithArguments("SC"));
        }

        [Fact]
        public void CS0724ERR_BadEmptyThrowInFinally()
        {
            var text = @"
using System;

class X
{
    static void Test()
    {
        try
        {
            throw new Exception();
        }
        catch
        {
            try
            {
            }
            finally
            {
                throw; // CS0724
            }
        }
    }

    static void Main()
    {
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (19,17): error CS0724: A throw statement with no arguments is not allowed in a finally clause that is nested inside the nearest enclosing catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrowInFinally, "throw").WithLocation(19, 17));
        }

        [Fact, WorkItem(1040213, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040213")]
        public void CS0724ERR_BadEmptyThrowInFinally_Nesting()
        {
            var text = @"
using System;

class X
{
    static void Test(bool b)
    {
        try
        {
            throw new Exception();
        }
        catch
        {
            try
            {
            }
            finally
            {
                if (b) throw; // CS0724

                try
                {
                    throw; // CS0724
                }
                catch
                {
                    throw; // OK
                }
                finally
                {
                    throw; // CS0724
                }
            }
        }
    }

    static void Main()
    {
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (19,17): error CS0724: A throw statement with no arguments is not allowed in a finally clause that is nested inside the nearest enclosing catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrowInFinally, "throw"),
                // (19,17): error CS0724: A throw statement with no arguments is not allowed in a finally clause that is nested inside the nearest enclosing catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrowInFinally, "throw"),
                // (19,17): error CS0724: A throw statement with no arguments is not allowed in a finally clause that is nested inside the nearest enclosing catch clause
                Diagnostic(ErrorCode.ERR_BadEmptyThrowInFinally, "throw"));
        }

        [Fact]
        public void CS0747ERR_InvalidInitializerElementInitializer()
        {
            var text = @"
using System.Collections.Generic;

public class C
{
    public static int Main()
    {
        var t = new List<int> { Capacity = 2, 1 }; // CS0747
        return 1;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "1"));
        }

        [Fact]
        public void CS0762ERR_PartialMethodToDelegate()
        {
            var text = @"
public delegate void TestDel();

    public partial class C
    {
        partial void Part();

        public static int Main()
        {
            C c = new C();
            TestDel td = new TestDel(c.Part); // CS0762
            return 1;
        }

    }
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_PartialMethodToDelegate, Line = 11, Column = 38 } });
        }

        [Fact]
        public void CS0765ERR_PartialMethodInExpressionTree()
        {
            var text = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

public delegate void dele();

public class ConClass
{
    [Conditional(""CONDITION"")]
    public static void TestMethod() { }
}

public partial class PartClass : IEnumerable
{
    List<object> list = new List<object>();

    partial void Add(int x);

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }

    static void Main()
    {
        Expression<Func<PartClass>> testExpr1 = () => new PartClass { 1, 2 }; // CS0765
        Expression<dele> testExpr2 = () => ConClass.TestMethod(); // CS0765
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (30,71): error CS0765: Partial methods with only a defining declaration or removed conditional methods cannot be used in expression trees
                //         Expression<Func<PartClass>> testExpr1 = () => new PartClass { 1, 2 }; // CS0765
                Diagnostic(ErrorCode.ERR_PartialMethodInExpressionTree, "1"),
                // (30,74): error CS0765: Partial methods with only a defining declaration or removed conditional methods cannot be used in expression trees
                //         Expression<Func<PartClass>> testExpr1 = () => new PartClass { 1, 2 }; // CS0765
                Diagnostic(ErrorCode.ERR_PartialMethodInExpressionTree, "2"),
                // (31,44): error CS0765: Partial methods with only a defining declaration or removed conditional methods cannot be used in expression trees
                //         Expression<dele> testExpr2 = () => ConClass.TestMethod(); // CS0765
                Diagnostic(ErrorCode.ERR_PartialMethodInExpressionTree, "ConClass.TestMethod()"));
        }

        [Fact]
        public void CS0815ERR_ImplicitlyTypedVariableAssignedBadValue_Local()
        {
            CreateCompilation(@"
class Test
{
    public static void Main()
    {
        var m = Main; // CS0815
        var d = s => -1; // CS0815
        var e = (string s) => 0; // CS0815
        var p = null;//CS0815
        var del = delegate(string a) { return -1; };// CS0815
        var v = M(); // CS0815
    }
    static void M() {}
}").VerifyDiagnostics(
                // (6,13): error CS0815: Cannot assign method group to an implicitly-typed variable
                //         var m = Main; // CS0815
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "m = Main").WithArguments("method group"),
                // (7,13): error CS0815: Cannot assign lambda expression to an implicitly-typed variable
                //         var d = s => -1; // CS0815
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "d = s => -1").WithArguments("lambda expression"),
                // (8,13): error CS0815: Cannot assign lambda expression to an implicitly-typed variable
                //         var e = (string s) => 0; // CS0815
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "e = (string s) => 0").WithArguments("lambda expression"),
                // (9,13): error CS0815: Cannot assign <null> to an implicitly-typed variable
                //         var p = null;//CS0815
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "p = null").WithArguments("<null>"),
                // (10,13): error CS0815: Cannot assign anonymous method to an implicitly-typed variable
                //         var del = delegate(string a) { return -1; };// CS0815
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "del = delegate(string a) { return -1; }").WithArguments("anonymous method"),
                // (11,13): error CS0815: Cannot assign void to an implicitly-typed variable
                //         var v = M(); // CS0815
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "v = M()").WithArguments("void"));
        }

        [Fact]
        public void CS0815ERR_ImplicitlyTypedVariableAssignedBadValue_Field()
        {
            CreateCompilationWithMscorlib45(@"
static void M() {}

var m = M;       
var d = s => -1; 
var e = (string s) => 0;
var p = null;    
var del = delegate(string a) { return -1; };
var v = M();     
", parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (4,5): error CS0815: Cannot assign method group to an implicitly-typed variable
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "m = M").WithArguments("method group"),
                // (5,5): error CS0815: Cannot assign lambda expression to an implicitly-typed variable
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "d = s => -1").WithArguments("lambda expression"),
                // (6,5): error CS0815: Cannot assign lambda expression to an implicitly-typed variable
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "e = (string s) => 0").WithArguments("lambda expression"),
                // (7,5): error CS0815: Cannot assign <null> to an implicitly-typed variable
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "p = null").WithArguments("<null>"),
                // (8,5): error CS0815: Cannot assign anonymous method to an implicitly-typed variable
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "del = delegate(string a) { return -1; }").WithArguments("anonymous method"),
                // (9,1): error CS0670: Field cannot have void type
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "var"));
        }

        [Fact]
        public void CS0818ERR_ImplicitlyTypedVariableWithNoInitializer()
        {
            var text = @"
class A
{
    public static int Main()
    {
        var a; // CS0818
        return -1;
    }
}";
            // In the native compiler we skip post-initial-binding error analysis if there was
            // an error during the initial binding, so we report only that the "var" declaration 
            // is bad. We do not report warnings like "variable b is assigned but never used".  
            // In Roslyn we do flow analysis even if the initial binding pass produced an error,
            // so we have extra errors here.

            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, Line = 6, Column = 13 },
                new ErrorDescription { Code = (int)ErrorCode.WRN_UnreferencedVar, Line = 6, Column = 13, IsWarning = true }});
        }

        [Fact]
        public void CS0818ERR_ImplicitlyTypedVariableWithNoInitializer_Fields()
        {
            CreateCompilationWithMscorlib45(@"
var a; // CS0818
", parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (1,5): error CS0818: Implicitly-typed variables must be initialized
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "a"));
        }

        [Fact]
        public void CS0819ERR_ImplicitlyTypedVariableMultipleDeclarator_Locals()
        {
            var text = @"
class A
{
    public static int Main()
    {
        var a = 3, b = 2; // CS0819
        return -1;
    }
}
";
            // In the native compiler we skip post-initial-binding error analysis if there was
            // an error during the initial binding, so we report only that the "var" declaration 
            // is bad. We do not report warnings like "variable b is assigned but never used".  
            // In Roslyn we do flow analysis even if the initial binding pass produced an error,
            // so we have extra errors here.

            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new[] {
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, Line = 6, Column = 9 },
                    new ErrorDescription { Code = (int)ErrorCode.WRN_UnreferencedVarAssg, Line = 6, Column = 13, IsWarning = true },
                    new ErrorDescription { Code = (int)ErrorCode.WRN_UnreferencedVarAssg, Line = 6, Column = 20, IsWarning = true }});
        }

        [Fact]
        public void CS0819ERR_ImplicitlyTypedVariableMultipleDeclarator_Fields()
        {
            CreateCompilationWithMscorlib45(@"
var goo = 4, bar = 4.5;
", parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (2,1): error CS0819: Implicitly-typed fields cannot have multiple declarators
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, "var"));
        }

        [Fact]
        public void CS0820ERR_ImplicitlyTypedVariableAssignedArrayInitializer()
        {
            var text = @"
class G
{
    public static int Main()
    {
        var a = { 1, 2, 3 }; //CS0820
        return -1;
    }
}";
            // In the native compilers this code produces two errors, both 
            // "you can't assign an array initializer to an implicitly typed local" and
            // "you can only use an array initializer to assign to an array type". 
            // It seems like the first error ought to prevent the second. In Roslyn
            // we only produce the first error.

            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedVariableAssignedArrayInitializer, Line = 6, Column = 13 }});
        }

        [Fact]
        public void CS0820ERR_ImplicitlyTypedVariableAssignedArrayInitializer_Fields()
        {
            CreateCompilationWithMscorlib45(@"
var y = { 1, 2, 3 };
", parseOptions: TestOptions.Script).VerifyDiagnostics(
            // (1,5): error CS0820: Cannot initialize an implicitly-typed variable with an array initializer
            Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedArrayInitializer, "y = { 1, 2, 3 }"));
        }

        [Fact]
        public void CS0821ERR_ImplicitlyTypedLocalCannotBeFixed()
        {
            var text = @"
class A
{
    static int x;

    public static int Main()
    {
        unsafe
        {
            fixed (var p = &x) { }
        }
        return -1;
    }
}";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (10,24): error CS0821: Implicitly-typed local variables cannot be fixed
                //             fixed (var p = &x) { }
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedLocalCannotBeFixed, "p = &x"));
        }

        [Fact]
        public void CS0822ERR_ImplicitlyTypedLocalCannotBeConst()
        {
            var text = @"
class A
{
    public static void Main()
    {
        const var x = 0; // CS0822.cs
        const var y = (int?)null + x;
    }
}";
            // In the dev10 compiler, the second line reports both that "const var" is illegal 
            // and that the initializer must be a valid constant. This seems a bit odd, so
            // in Roslyn we just report the first error. Let the user sort out whether they
            // meant it to be a constant or a variable, and then we can tell them if its a
            // bad constant.

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (6,15): error CS0822: Implicitly-typed variables cannot be constant
                //         const var x = 0; // CS0822.cs
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableCannotBeConst, "var x = 0").WithLocation(6, 15),
                // (7,15): error CS0822: Implicitly-typed variables cannot be constant
                //         const var y = (int?)null + x;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableCannotBeConst, "var y = (int?)null + x").WithLocation(7, 15),
                // (7,23): warning CS0458: The result of the expression is always 'null' of type 'int?'
                //         const var y = (int?)null + x;
                Diagnostic(ErrorCode.WRN_AlwaysNull, "(int?)null + x").WithArguments("int?").WithLocation(7, 23)
                );
        }

        [Fact]
        public void CS0822ERR_ImplicitlyTypedVariableCannotBeConst_Fields()
        {
            CreateCompilationWithMscorlib45(@"
const var x = 0; // CS0822.cs
", parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (2,7): error CS0822: Implicitly-typed variables cannot be constant
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableCannotBeConst, "var"));
        }

        [Fact]
        public void CS0825ERR_ImplicitlyTypedVariableCannotBeUsedAsTheTypeOfAParameter_Fields()
        {
            CreateCompilationWithMscorlib45(@"
void goo(var arg) { }
var goo(int arg) { return 2; }
", parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (1,10): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var"),
                // (2,1): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var"));
        }

        [Fact]
        public void CS0825ERR_ImplicitlyTypedVariableCannotBeUsedAsTheTypeOfAParameter_Fields2()
        {
            CreateCompilationWithMscorlib45(@"
T goo<T>() { return default(T); }
goo<var>();
", parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (2,5): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var"));
        }

        [Fact()]
        public void CS0826ERR_ImplicitlyTypedArrayNoBestType()
        {
            var text = @"
public class C
{
    delegate void D();
    public static void M1() {}
    public static void M2(int x) {}
    public static int M3() { return 1; }
    public static int M4(int x) { return x; }

    public static int Main()
    {
        var z = new[] { 1, ""str"" }; // CS0826

        char c = 'c';
        short s1 = 0;
        short s2 = -0;
        short s3 = 1;
        short s4 = -1;
            
        var array1 = new[] { s1, s2, s3, s4, c, '1' }; // CS0826

        var a = new [] {}; // CS0826

        byte b = 3;
        var arr = new [] {b, c};   // CS0826

        var a1 = new [] {null};   // CS0826
        var a2 = new [] {null, null, null};    // CS0826

        D[] l1 = new [] {x=>x+1};   // CS0826
        D[] l2 = new [] {x=>x+1, x=>{return x + 1;}, (int x)=>x+1, (int x)=>{return x + 1;}, (x, y)=>x + y, ()=>{return 1;}};   // CS0826

        D[] d1 = new [] {delegate {}};  // CS0826
        D[] d2 = new [] {delegate {}, delegate (){}, delegate {return 1;}, delegate {return;}, delegate(int x){}, delegate(int x){return x;}, delegate(int x, int y){return x + y;}};   // CS0826

        var m = new [] {M1, M2, M3, M4};    // CS0826

        return 1;
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] {
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, Line = 12, Column = 17 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, Line = 20, Column = 22 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, Line = 22, Column = 17 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, Line = 25, Column = 19 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, Line = 27, Column = 18 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, Line = 28, Column = 18 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, Line = 30, Column = 18 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, Line = 31, Column = 18 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, Line = 33, Column = 18 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, Line = 34, Column = 18 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, Line = 36, Column = 17 }});
        }

        [Fact]
        public void CS0828ERR_AnonymousTypePropertyAssignedBadValue()
        {
            var text = @"
public class C
{
    public static int Main()
    {
        var c = new { p1 = null }; // CS0828
        return 1;
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, Line = 6, Column = 23 } });
        }

        [Fact]
        public void CS0828ERR_AnonymousTypePropertyAssignedBadValue_2()
        {
            var text = @"
public class C
{
    public static void Main()
    {
        var c = new { p1 = Main }; // CS0828
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, Line = 6, Column = 23 } });
        }

        [Fact]
        public void CS0828ERR_AnonymousTypePropertyAssignedBadValue_3()
        {
            var text = @"
public class C
{
    public static void Main()
    {
        var c = new { p1 = Main() }; // CS0828
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, Line = 6, Column = 23 } });
        }

        [Fact]
        public void CS0828ERR_AnonymousTypePropertyAssignedBadValue_4()
        {
            var text = @"
public class C
{
    public static void Main()
    {
        var c = new { p1 = ()=>3 }; // CS0828
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, Line = 6, Column = 23 } });
        }

        [Fact]
        public void CS0828ERR_AnonymousTypePropertyAssignedBadValue_5()
        {
            var text = @"
public class C
{
    public static void Main()
    {
        var c = new 
        { 
            p1 = delegate { return 1; } // CS0828
        }; 
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, Line = 8, Column = 13 } });
        }

        [Fact]
        public void CS0831ERR_ExpressionTreeContainsBaseAccess()
        {
            var text = @"
using System;
using System.Linq.Expressions;

public class A
{
    public virtual int BaseMethod() { return 1; }
}
public class C : A
{
    public override int BaseMethod() { return 2; }
    public int Test(C c)
    {
        Expression<Func<int>> e = () => base.BaseMethod(); // CS0831
        return 1;
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (14,41): error CS0831: An expression tree may not contain a base access
                //         Expression<Func<int>> e = () => base.BaseMethod(); // CS0831
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsBaseAccess, "base")
                );
        }

        [Fact]
        public void CS0832ERR_ExpressionTreeContainsAssignment()
        {
            var text = @"
using System;
using System.Linq.Expressions;

public class C
{
    public static int Main()
    {
        Expression<Func<int, int>> e1 = x => x += 5; // CS0843
        Expression<Func<int, int>> e2 = x => x = 5; // CS0843
        return 1;
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (9,46): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<Func<int, int>> e1 = x => x += 5; // CS0843
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "x += 5"),
                // (10,46): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<Func<int, int>> e2 = x => x = 5; // CS0843
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "x = 5")
                );
        }

        [Fact]
        public void CS0833ERR_AnonymousTypeDuplicatePropertyName()
        {
            var text = @"
public class C
{
    public static int Main()
    {
        var c = new { p1 = 1, p1 = 2 }; // CS0833
        return 1;
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypeDuplicatePropertyName, Line = 6, Column = 31 } });
        }

        [Fact]
        public void CS0833ERR_AnonymousTypeDuplicatePropertyName_2()
        {
            var text = @"
public class C
{
    public static int Main()
    {
        var c = new { C.Prop, Prop = 2 }; // CS0833
        return 1;
    }
    static string Prop { get; set; }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypeDuplicatePropertyName, Line = 6, Column = 31 } });
        }

        [Fact]
        public void CS0833ERR_AnonymousTypeDuplicatePropertyName_3()
        {
            var text = @"
public class C
{
    public static int Main()
    {
        var c = new { C.Prop, Prop = 2 }; // CS0833 + CS0828
        return 1;
    }
    static string Prop() { return null; }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, Line = 6, Column = 23 },
                                         new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypeDuplicatePropertyName, Line = 6, Column = 31 }});
        }

        [Fact]
        public void CS0834ERR_StatementLambdaToExpressionTree()
        {
            var text = @"
using System;
using System.Linq.Expressions;

public class C
{
    public static void Main()
    {
        Expression<Func<int, int>> e = x => { return x; }; // CS0834
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (9,40): error CS0834: A lambda expression with a statement body cannot be converted to an expression tree
                //         Expression<Func<int, int>> e = x => { return x; }; // CS0834
                Diagnostic(ErrorCode.ERR_StatementLambdaToExpressionTree, "x => { return x; }")
                );
        }

        [Fact]
        public void CS0835ERR_ExpressionTreeMustHaveDelegate()
        {
            var text = @"
using System.Linq.Expressions;

public class Myclass
{
    public static int Main()
    {
        Expression<int> e = x => x + 1; // CS0835
        return 1;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new[] { LinqAssemblyRef },
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_ExpressionTreeMustHaveDelegate, Line = 8, Column = 29 } });
        }

        [Fact]
        public void CS0836ERR_AnonymousTypeNotAvailable()
        {
            var text = @"
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class MyClass : Attribute
{
    public MyClass(object obj)
    {
    }
}

[MyClass(new { })] // CS0836
public class ClassGoo
{
}

public class Test
{
    public static int Main()
    {
        return 0;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypeNotAvailable, Line = 11, Column = 10 } });
        }

        [Fact]
        public void CS0836ERR_AnonymousTypeNotAvailable2()
        {
            var text = @"
public class Test
{
    const object x = new { };
    public static int Main()
    {
        return 0;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypeNotAvailable, Line = 4, Column = 22 } });
        }

        [Fact]
        public void CS0836ERR_AnonymousTypeNotAvailable3()
        {
            var text = @"
public class Test
{
    static object y = new { };
    private object x = new { };
    public static int Main()
    {
        return 0;
    }
}
";
            // NOTE: Actually we assert that #836 is NOT generated
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);
        }

        [Fact]
        public void CS0836ERR_AnonymousTypeNotAvailable4()
        {
            var text = @"
public class Test
{
    public static int Main(object x = new { })
    {
        return 0;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_DefaultValueMustBeConstant, Line = 4, Column = 39 } });
        }

        [Fact]
        public void CS0836ERR_AnonymousTypeNotAvailable5()
        {
            var text = @"
using System;
[AttributeUsage(AttributeTargets.All)]
public class MyClass : Attribute
{
    public MyClass(object obj)
    {
    }
}

public class Test
{
    [MyClass(new { })] // CS0836
    public static int Main()
    {
        return 0;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypeNotAvailable, Line = 13, Column = 14 } });
        }

        [Fact]
        public void CS0836ERR_AnonymousTypeNotAvailable6()
        {
            var text = @"
using System;
[AttributeUsage(AttributeTargets.All)]
public class MyClass : Attribute
{
    public MyClass(object obj)
    {
    }
}

public class Test
{
    [MyClass(new { })] // CS0836
    static object y = new { };

    public static int Main()
    {
        return 0;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonymousTypeNotAvailable, Line = 13, Column = 14 } });
        }

        [Fact]
        public void CS0837ERR_LambdaInIsAs()
        {
            var text = @"
namespace TestNamespace
{
    public delegate void Del();

    class Test
    {
        static int Main()
        {
            bool b1 = (() => { }) is Del;   // CS0837
            bool b2 = delegate() { } is Del;// CS0837
            Del d1 = () => { } as Del;      // CS0837
            Del d2 = delegate() { } as Del; // CS0837
            return 1;
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,23): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //             bool b1 = (() => { }) is Del;   // CS0837
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "(() => { }) is Del").WithLocation(10, 23),
                // (11,23): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //             bool b2 = delegate() { } is Del;// CS0837
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "delegate() { } is Del").WithLocation(11, 23),
                // (12,22): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //             Del d1 = () => { } as Del;      // CS0837
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "() => { } as Del").WithLocation(12, 22),
                // (13,22): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //             Del d2 = delegate() { } as Del; // CS0837
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "delegate() { } as Del").WithLocation(13, 22)
                );
        }

        [Fact]
        public void CS0841ERR_VariableUsedBeforeDeclaration01()
        {
            CreateCompilation(
@"class C
{
    static void M()
    {
        j = 5; // CS0841
        int j; // To fix, move this line up.
    }
}
")
                // The native compiler just produces the "var used before decl" error; the Roslyn
                // compiler runs the flow checking pass even if the initial binding failed. We 
                // might consider turning off flow checking if the initial binding failed, and
                // removing the warning here.

                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "j").WithArguments("j"),
                    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));
        }

        [Fact]
        public void CS0841ERR_VariableUsedBeforeDeclaration02()
        {
            CreateCompilation(
@"class C
{
    static void M()
    {
        int a = b, b = 0, c = a;
        for (int x = y, y = 0; ; )
        {
        }
    }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "b").WithArguments("b"),
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y"));
        }

        [Fact]
        public void CS0841ERR_VariableUsedBeforeDeclaration03()
        {
            // It is a bit unfortunate that we produce "can't use variable before decl" here
            // when the variable is being used after the decl. Perhaps we should generate
            // a better error?

            CreateCompilation(
@"class C
{
    static int N(out int q) { q = 1; return 2;}
    static void M()
    {
        var x = N(out x);
    }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x"));
        }

        [Fact]
        public void CS0841ERR_VariableUsedBeforeDeclaration04()
        {
            var systemRef = TestReferences.NetFx.v4_0_30319.System;
            CreateCompilationWithMscorlib40AndSystemCore(
@"using System.Collections.Generic;
class Base
{
    int i;
}
class Derived : Base
{
    int j;
}
class C
{
    public static void Main()
    {
        HashSet<Base> set1 = new HashSet<Base>();

        foreach (Base b in set1)
        {
            Derived d = b as Derived;
            Base b = null;
        }
    }
}
", new List<MetadataReference> { systemRef })
                .VerifyDiagnostics(
                // (18,25): error CS0841: Cannot use local variable 'b' before it is declared
                //             Derived d = b as Derived;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "b").WithArguments("b"),
                // (19,18): error CS0136: A local or parameter named 'b' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             Base b = null;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b").WithArguments("b"),
                // (4,9): warning CS0169: The field 'Base.i' is never used
                //     int i;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("Base.i"),
                // (8,9): warning CS0169: The field 'Derived.j' is never used
                //     int j;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "j").WithArguments("Derived.j"));
        }

        /// <summary>
        /// No errors using statics before declaration.
        /// </summary>
        [Fact]
        public void StaticUsedBeforeDeclaration()
        {
            var text =
@"class C
{
    static int F = G + 2;
    static int G = F + 1;
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);
        }

        [Fact]
        public void CS0843ERR_UnassignedThisAutoProperty()
        {
            var text = @"
struct S
{
    public int AIProp { get; set; }
    public S(int i) { } //CS0843
}

class Test
{
    static int Main()
    {
        return 1;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_UnassignedThisAutoProperty, Line = 5, Column = 12 } });
        }

        [Fact]
        public void CS0844ERR_VariableUsedBeforeDeclarationAndHidesField()
        {
            var text = @"
public class MyClass
{
    int num;
    public void TestMethod()
    {
        num = 5;   // CS0844
        int num = 6;
        System.Console.WriteLine(num);
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (7,9): error CS0844: Cannot use local variable 'num' before it is declared. The declaration of the local variable hides the field 'MyClass.num'.
                //         num = 5;   // CS0844
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclarationAndHidesField, "num").WithArguments("num", "MyClass.num"),
                // (4,9): warning CS0169: The field 'MyClass.num' is never used
                //     int num;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "num").WithArguments("MyClass.num")
            );
        }

        [Fact]
        public void CS0845ERR_ExpressionTreeContainsBadCoalesce()
        {
            var text = @"
using System;
using System.Linq.Expressions;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Expression<Func<object>> e = () => null ?? ""x""; // CS0845
        }
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (11,48): error CS0845: An expression tree lambda may not contain a coalescing operator with a null literal left-hand side
                //             Expression<Func<object>> e = () => null ?? "x"; // CS0845
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsBadCoalesce, "null")
                );
        }

        [WorkItem(3717, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CS0846ERR_ArrayInitializerExpected()
        {
            var text = @"public class Myclass
{
    public static void Main()
    {
        int[,] a = new int[,] { 1 }; // error
    } 
} 
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_ArrayInitializerExpected, Line = 5, Column = 33 } });
        }

        [Fact]
        public void CS0847ERR_ArrayInitializerIncorrectLength()
        {
            var text = @"
public class Program
{
    public static void Main(string[] args)
    {
        int[] ar0 = new int[0]{0}; // error CS0847: An array initializer of length `0' was expected
        int[] ar1 = new int[3]{}; // error CS0847: An array initializer of length `3' was expected
        ar0[0] = ar1[0];
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,31): error CS0847: An array initializer of length '0' is expected
                //         int[] ar0 = new int[0]{0}; // error CS0847: An array initializer of length `0' was expected
                Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "{0}").WithArguments("0").WithLocation(6, 31),
                // (7,31): error CS0847: An array initializer of length '3' is expected
                //         int[] ar1 = new int[3]{}; // error CS0847: An array initializer of length `3' was expected
                Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "{}").WithArguments("3").WithLocation(7, 31));
        }

        [Fact]
        public void CS0853ERR_ExpressionTreeContainsNamedArgument01()
        {
            var text = @"
using System.Linq.Expressions;
namespace ConsoleApplication3
{
    class Program
    {
        delegate string dg(int x);
        static void Main(string[] args)
        {
            Expression<dg> myET = x => Index(minSessions:5);
        }
        public static string Index(int minSessions = 0)
        {
            return minSessions.ToString();
        }
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (10,40): error CS0853: An expression tree may not contain a named argument specification
                //             Expression<dg> myET = x => Index(minSessions:5);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "Index(minSessions:5)").WithLocation(10, 40)
                );
        }

        [WorkItem(545063, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545063")]
        [Fact]
        public void CS0853ERR_ExpressionTreeContainsNamedArgument02()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
 
class A
{
    static void Main()
    {
        Expression<Func<int>> f = () => new List<int> { 1 } [index: 0];
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (10,41): error CS0853: An expression tree may not contain a named argument specification
                //         Expression<Func<int>> f = () => new List<int> { 1 } [index: 0];
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "new List<int> { 1 } [index: 0]").WithLocation(10, 41)
                );
        }

        [Fact]
        public void CS0854ERR_ExpressionTreeContainsOptionalArgument01()
        {
            var text = @"
using System.Linq.Expressions;
namespace ConsoleApplication3
{
    class Program
    {
        delegate string dg(int x);
        static void Main(string[] args)
        {
            Expression<dg> myET = x => Index();
        }
        public static string Index(int minSessions = 0)
        {
            return minSessions.ToString();
        }
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (10,40): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                //             Expression<dg> myET = x => Index();
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "Index()").WithLocation(10, 40)
                );
        }

        [Fact]
        public void CS0854ERR_ExpressionTreeContainsOptionalArgument02()
        {
            var text =
@"using System;
using System.Linq.Expressions;
class A
{
    internal object this[int x, int y = 2]
    {
        get { return null; }
    }
}
class B
{
    internal object this[int x, int y = 2]
    {
        set { }
    }
}
class C
{
    static void M(A a, B b)
    {
        Expression<Func<object>> e1;
        e1 = () => a[0];
        e1 = () => a[1, 2];
        Expression<Action> e2;
        e2 = () => b[3] = null;
        e2 = () => b[4, 5] = null;
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (22,20): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "a[0]").WithLocation(22, 20),
                // (25,20): error CS0832: An expression tree may not contain an assignment operator
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "b[3] = null").WithLocation(25, 20),
                // (25,20): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "b[3]").WithLocation(25, 20),
                // (26,20): error CS0832: An expression tree may not contain an assignment operator
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "b[4, 5] = null").WithLocation(26, 20));
        }

        [Fact]
        public void CS0855ERR_ExpressionTreeContainsIndexedProperty()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface I
    Property P(index As Object) As Integer
    Property Q(Optional index As Object = Nothing) As Integer
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"using System;
using System.Linq.Expressions;
class C
{
    static void M(I i)
    {
        Expression<Func<int>> e1;
        e1 = () => i.P[1];
        e1 = () => i.get_P(2); // no error
        e1 = () => i.Q;
        e1 = () => i.Q[index:3];
        Expression<Action> e2;
        e2 = () => i.P[4] = 0;
        e2 = () => i.set_P(5, 6); // no error
    }
}";
            var compilation2 = CreateCompilationWithMscorlib40AndSystemCore(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (8,20): error CS0855: An expression tree may not contain an indexed property
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsIndexedProperty, "i.P[1]").WithLocation(8, 20),
                // (10,20): error CS0855: An expression tree may not contain an indexed property
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsIndexedProperty, "i.Q").WithLocation(10, 20),
                // (11,20): error CS0855: An expression tree may not contain an indexed property
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsIndexedProperty, "i.Q[index:3]").WithLocation(11, 20),
                // (13,20): error CS0832: An expression tree may not contain an assignment operator
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "i.P[4] = 0").WithLocation(13, 20),
                // (13,20): error CS0855: An expression tree may not contain an indexed property
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsIndexedProperty, "i.P[4]").WithLocation(13, 20));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(23004, "https://github.com/dotnet/roslyn/issues/23004")]
        public void CS0856ERR_IndexedPropertyRequiresParams01()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface I
    Property P(x As Object, Optional y As Object = Nothing) As Object
    Property P(Optional x As Integer = 1, Optional y As Integer = 2) As Object
    Property Q(Optional x As Integer = 1, Optional y As Integer = 2) As Object
    Property Q(x As Object, Optional y As Object = Nothing) As Object
    Property R(x As Integer, y As Integer, Optional z As Integer = 3) As Object
    Property S(ParamArray args As Integer()) As Object
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class C
{
    static void M(I i)
    {
        object o;
        o = i.P; // CS0856 (Dev11)
        o = i.Q;
        i.R = o; // CS0856
        i.R[1] = o; // CS1501
        o = i.S; // CS0856 (Dev11)
        i.S = o; // CS0856 (Dev11)
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (8,9): error CS0856: Indexed property 'I.R' has non-optional arguments which must be provided
                Diagnostic(ErrorCode.ERR_IndexedPropertyRequiresParams, "i.R").WithArguments("I.R").WithLocation(8, 9),
                // (9,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'y' of 'I.R[int, int, int]'
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "i.R[1]").WithArguments("y", "I.R[int, int, int]").WithLocation(9, 9));

            var tree = compilation2.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().First();

            Assert.Equal("i.R[1]", node.ToString());

            compilation2.VerifyOperationTree(node, expectedOperationTree:
@"
IInvalidOperation (OperationKind.Invalid, Type: System.Object, IsInvalid) (Syntax: 'i.R[1]')
  Children(2):
      IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: I, IsInvalid) (Syntax: 'i')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
");
        }

        [Fact]
        public void CS0856ERR_IndexedPropertyRequiresParams02()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Class A
    Protected ReadOnly Property P(Optional o As Object = Nothing) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public ReadOnly Property P(i As Integer) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class C
{
    static object F(A a)
    {
        return a.P;
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (5,16): error CS0856: Indexed property 'A.P' has non-optional arguments which must be provided
                Diagnostic(ErrorCode.ERR_IndexedPropertyRequiresParams, "a.P").WithArguments("A.P").WithLocation(5, 16));
        }

        [Fact]
        public void CS0857ERR_IndexedPropertyMustHaveAllOptionalParams()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
<CoClass(GetType(A))>
Public Interface IA
    Property P(x As Object, Optional y As Object = Nothing) As Object
    Property P(Optional x As Integer = 1, Optional y As Integer = 2) As Object
    Property Q(Optional x As Integer = 1, Optional y As Integer = 2) As Object
    Property Q(x As Object, Optional y As Object = Nothing) As Object
    Property R(x As Integer, y As Integer, Optional z As Integer = 3) As Object
    Property S(ParamArray args As Integer()) As Object
End Interface
Public Class A
    Implements IA
    Property P(x As Object, Optional y As Object = Nothing) As Object Implements IA.P
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
    Property P(Optional x As Integer = 1, Optional y As Integer = 2) As Object Implements IA.P
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
    Property Q(Optional x As Integer = 1, Optional y As Integer = 2) As Object Implements IA.Q
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
    Property Q(x As Object, Optional y As Object = Nothing) As Object Implements IA.Q
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
    Property R(x As Integer, y As Integer, Optional z As Integer = 3) As Object Implements IA.R
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
    Property S(ParamArray args As Integer()) As Object Implements IA.S
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Passes);
            var source2 =
@"class B
{
    static void M()
    {
        IA a;
        a = new IA() { P = null }; // CS0857 (Dev11)
        a = new IA() { Q = null };
        a = new IA() { R = null }; // CS0857
        a = new IA() { S = null }; // CS0857 (Dev11)
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (8,24): error CS0857: Indexed property 'IA.R' must have all arguments optional
                Diagnostic(ErrorCode.ERR_IndexedPropertyMustHaveAllOptionalParams, "R").WithArguments("IA.R").WithLocation(8, 24));
        }

        [Fact]
        public void CS1059ERR_IncrementLvalueExpected01()
        {
            var text =
@"enum E { A, B }
class C
{
    static void M()
    {
        ++E.A; // CS1059
        E.A++; // CS1059
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_IncrementLvalueExpected, Line = 6, Column = 11 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_IncrementLvalueExpected, Line = 7, Column = 9 });
        }

        [Fact]
        public void CS1059ERR_IncrementLvalueExpected02()
        {
            var text =
@"class C
{
    const int field = 0;
    static void M()
    {
        const int local = 0;
        ++local;
        local++;
        --field;
        field--;
        ++(local + 3);
        (local + 3)++;
        --2;
        2--;

        dynamic d = null;
        (d + 1)++;
        --(d + 1);
        d++++;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,11): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "local"),
                // (8,9): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "local"),
                // (9,11): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "field"),
                // (10,9): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "field"),
                // (11,12): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "local + 3"),
                // (12,10): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "local + 3"),
                // (13,11): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "2"),
                // (14,9): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "2"),
                // (17,10): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "d + 1"),
                // (18,12): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "d + 1"),
                // (19,9): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "d++"));
        }

        [Fact]
        public void CS1059ERR_IncrementLvalueExpected03()
        {
            var text = @"
class C
{
    void M()
    {
        ++this; // CS1059
        this--; // CS1059
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,11): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         ++this; // CS1059
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "this").WithArguments("this"),
                // (7,9): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         this--; // CS1059
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "this").WithArguments("this"));
        }

        [Fact]
        public void CS1061ERR_NoSuchMemberOrExtension01()
        {
            var text = @"
public class TestClass1
{
    public void WriteSomething(string s)
    {
        System.Console.WriteLine(s);
    }
}

public class TestClass2
{
    public void DisplaySomething(string s)
    {
        System.Console.WriteLine(s);
    }
}

public class TestTheClasses
{
    public static void Main()
    {
        TestClass1 tc1 = new TestClass1();
        TestClass2 tc2 = new TestClass2();
        if (tc1 == null | tc2 == null) {}
        tc1.DisplaySomething(""Hello"");      // CS1061
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_NoSuchMemberOrExtension, Line = 25, Column = 13 } });
        }

        [Fact]
        public void CS1061ERR_NoSuchMemberOrExtension02()
        {
            var source =
@"enum E { }
class C
{
    static void M(E e)
    {
        object o = e.value__;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,22): error CS1061: 'E' does not contain a definition for 'value__' and no extension method 'value__' accepting a first argument of type 'E' could be found (are you missing a using directive or an assembly reference?)
                //         object o = e.value__;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "value__").WithArguments("E", "value__").WithLocation(6, 22));
        }

        [Fact]
        public void CS1061ERR_NoSuchMemberOrExtension03()
        {
            CreateCompilation(
@"class A
{
}
class B
{
    void M()
    {
        this.F();
        this.P = this.Q;
    }
    static void M(A a)
    {
        a.F();
        a.P = a.Q;
    }
}")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("A", "F"),
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("A", "P"),
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Q").WithArguments("A", "Q"),
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("B", "F"),
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("B", "P"),
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Q").WithArguments("B", "Q"));
        }

        [Fact]
        public void CS1061ERR_NoSuchMemberOrExtension04()
        {
            CreateCompilation(
@"using System.Collections.Generic;
class C
{
    static void M(List<object> list)
    {
        object o = list.Item;
        list.Item = o;
    }
}")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item").WithArguments("System.Collections.Generic.List<object>", "Item").WithLocation(6, 25),
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item").WithArguments("System.Collections.Generic.List<object>", "Item").WithLocation(7, 14));
        }

        [Fact]
        public void CS1061ERR_NoSuchMemberOrExtension05()
        {
            CreateCompilationWithMscorlib40AndSystemCore(
@"using System.Linq;

class Test
{
    static void Main()
    {
        var q = 1.Select(z => z);
    }
}
")
                .VerifyDiagnostics(
                    // (7,17): error CS1061: 'int' does not contain a definition for 'Select' and no extension method 'Select' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                    //         var q = 1.Select(z => z);
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Select").WithArguments("int", "Select").WithLocation(7, 19));
        }

        [Fact]
        public void CS1061ERR_NoSuchMemberOrExtension06()
        {
            var source =
@"interface I<T> { }
static class C
{
    static void M(object o)
    {
        o.M1(o, o);
        o.M2(o, o);
    }
    static void M1<T>(this I<T> o, object arg) { }
    static void M2<T>(this I<T> o, params object[] args) { }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (6,9): error CS1501: No overload for method 'M1' takes 2 arguments
                Diagnostic(ErrorCode.ERR_BadArgCount, "M1").WithArguments("M1", "2").WithLocation(6, 11),
                // (7,9): error CS1061: 'object' does not contain a definition for 'M2' and no extension method 'M2' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M2").WithArguments("object", "M2").WithLocation(7, 11));
        }

        [Fact]
        public void CS1113ERR_ValueTypeExtDelegate01()
        {
            var source =
@"class C
{
    public void M() { }
}
interface I
{
    void M();
}
enum E
{
}
struct S
{
    public void M() { }
}
static class SC
{
    static void Test(C c, I i, E e, S s, double d)
    {
        System.Action cm = c.M;   // OK -- instance method
        System.Action cm1 = c.M1; // OK -- extension method on ref type
        System.Action im = i.M;   // OK -- instance method
        System.Action im2 = i.M2; // OK -- extension method on ref type
        System.Action em3 = e.M3; // BAD -- extension method on value type
        System.Action sm = s.M;   // OK -- instance method
        System.Action sm4 = s.M4; // BAD -- extension method on value type
        System.Action dm5 = d.M5; // BAD -- extension method on value type
    }

    static void M1(this C c) { }
    static void M2(this I i) { }
    static void M3(this E e) { }
    static void M4(this S s) { }
    static void M5(this double d) { }
}";
            CreateCompilationWithMscorlib40(source, references: new[] { SystemCoreRef }).VerifyDiagnostics(
                // (24,29): error CS1113: Extension methods 'SC.M3(E)' defined on value type 'E' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "e.M3").WithArguments("SC.M3(E)", "E").WithLocation(24, 29),
                // (26,29): error CS1113: Extension methods 'SC.M4(S)' defined on value type 'S' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "s.M4").WithArguments("SC.M4(S)", "S").WithLocation(26, 29),
                // (27,29): error CS1113: Extension methods 'SC.M5(double)' defined on value type 'double' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "d.M5").WithArguments("SC.M5(double)", "double").WithLocation(27, 29));
        }

        [Fact]
        public void CS1113ERR_ValueTypeExtDelegate02()
        {
            var source =
@"delegate void D();
interface I { }
struct S { }
class C
{
    static void M<T1, T2, T3, T4, T5>(int i, S s, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        where T2 : class
        where T3 : struct
        where T4 : I
        where T5 : C
    {
        D d;
        d = i.M1;
        d = i.M2<int, object>;
        d = s.M1;
        d = s.M2<S, object>;
        d = t1.M1;
        d = t1.M2<T1, object>;
        d = t2.M1;
        d = t2.M2<T2, object>;
        d = t3.M1;
        d = t3.M2<T3, object>;
        d = t4.M1;
        d = t4.M2<T4, object>;
        d = t5.M1;
        d = t5.M2<T5, object>;
    }
}
static class E
{
    internal static void M1<T>(this T t) { }
    internal static void M2<T, U>(this T t) { }
}";
            CreateCompilationWithMscorlib40(source, references: new[] { SystemCoreRef }).VerifyDiagnostics(
                // (13,13): error CS1113: Extension methods 'E.M1<int>(int)' defined on value type 'int' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "i.M1").WithArguments("E.M1<int>(int)", "int").WithLocation(13, 13),
                // (14,13): error CS1113: Extension methods 'E.M2<int, object>(int)' defined on value type 'int' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "i.M2<int, object>").WithArguments("E.M2<int, object>(int)", "int").WithLocation(14, 13),
                // (15,13): error CS1113: Extension methods 'E.M1<S>(S)' defined on value type 'S' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "s.M1").WithArguments("E.M1<S>(S)", "S").WithLocation(15, 13),
                // (16,13): error CS1113: Extension methods 'E.M2<S, object>(S)' defined on value type 'S' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "s.M2<S, object>").WithArguments("E.M2<S, object>(S)", "S").WithLocation(16, 13),
                // (17,13): error CS1113: Extension methods 'E.M1<T1>(T1)' defined on value type 'T1' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t1.M1").WithArguments("E.M1<T1>(T1)", "T1").WithLocation(17, 13),
                // (18,13): error CS1113: Extension methods 'E.M2<T1, object>(T1)' defined on value type 'T1' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t1.M2<T1, object>").WithArguments("E.M2<T1, object>(T1)", "T1").WithLocation(18, 13),
                // (21,13): error CS1113: Extension methods 'E.M1<T3>(T3)' defined on value type 'T3' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t3.M1").WithArguments("E.M1<T3>(T3)", "T3").WithLocation(21, 13),
                // (22,13): error CS1113: Extension methods 'E.M2<T3, object>(T3)' defined on value type 'T3' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t3.M2<T3, object>").WithArguments("E.M2<T3, object>(T3)", "T3").WithLocation(22, 13),
                // (23,13): error CS1113: Extension methods 'E.M1<T4>(T4)' defined on value type 'T4' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t4.M1").WithArguments("E.M1<T4>(T4)", "T4").WithLocation(23, 13),
                // (24,13): error CS1113: Extension methods 'E.M2<T4, object>(T4)' defined on value type 'T4' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t4.M2<T4, object>").WithArguments("E.M2<T4, object>(T4)", "T4").WithLocation(24, 13));
        }

        [WorkItem(528758, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528758")]
        [Fact(Skip = "528758")]
        public void CS1113ERR_ValueTypeExtDelegate03()
        {
            var source =
@"delegate void D();
interface I { }
struct S { }
class C
{
    static void M<T1, T2, T3, T4, T5>(int i, S s, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        where T2 : class
        where T3 : struct
        where T4 : I
        where T5 : C
    {
        F(i.M1);
        F(i.M2<int, object>);
        F(s.M1);
        F(s.M2<S, object>);
        F(t1.M1);
        F(t1.M2<T1, object>);
        F(t2.M1);
        F(t2.M2<T2, object>);
        F(t3.M1);
        F(t3.M2<T3, object>);
        F(t4.M1);
        F(t4.M2<T4, object>);
        F(t5.M1);
        F(t5.M2<T5, object>);
    }
    static void F(D d) { }
}
static class E
{
    internal static void M1<T>(this T t) { }
    internal static void M2<T, U>(this T t) { }
}";
            CreateCompilation(source, references: new[] { SystemCoreRef }).VerifyDiagnostics(
                // (12,11): error CS1113: Extension methods 'E.M1<int>(int)' defined on value type 'int' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "i.M1").WithArguments("E.M1<int>(int)", "int").WithLocation(12, 11),
                // (13,11): error CS1113: Extension methods 'E.M2<int, object>(int)' defined on value type 'int' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "i.M2<int, object>").WithArguments("E.M2<int, object>(int)", "int").WithLocation(13, 11),
                // (14,11): error CS1113: Extension methods 'E.M1<S>(S)' defined on value type 'S' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "s.M1").WithArguments("E.M1<S>(S)", "S").WithLocation(14, 11),
                // (15,11): error CS1113: Extension methods 'E.M2<S, object>(S)' defined on value type 'S' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "s.M2<S, object>").WithArguments("E.M2<S, object>(S)", "S").WithLocation(15, 11),
                // (16,11): error CS1113: Extension methods 'E.M1<T1>(T1)' defined on value type 'T1' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t1.M1").WithArguments("E.M1<T1>(T1)", "T1").WithLocation(16, 11),
                // (17,11): error CS1113: Extension methods 'E.M2<T1, object>(T1)' defined on value type 'T1' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t1.M2<T1, object>").WithArguments("E.M2<T1, object>(T1)", "T1").WithLocation(17, 11),
                // (20,11): error CS1113: Extension methods 'E.M1<T3>(T3)' defined on value type 'T3' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t3.M1").WithArguments("E.M1<T3>(T3)", "T3").WithLocation(20, 11),
                // (21,11): error CS1113: Extension methods 'E.M2<T3, object>(T3)' defined on value type 'T3' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t3.M2<T3, object>").WithArguments("E.M2<T3, object>(T3)", "T3").WithLocation(21, 11),
                // (22,11): error CS1113: Extension methods 'E.M1<T4>(T4)' defined on value type 'T4' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t4.M1").WithArguments("E.M1<T4>(T4)", "T4").WithLocation(22, 11),
                // (23,11): error CS1113: Extension methods 'E.M2<T4, object>(T4)' defined on value type 'T4' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "t4.M2<T4, object>").WithArguments("E.M2<T4, object>(T4)", "T4").WithLocation(23, 11));
        }

        [Fact]
        public void CS1501ERR_BadArgCount()
        {
            var text = @"
using System;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            ExampleClass ec = new ExampleClass();
            ec.ExampleMethod(10, 20);
        }
    }

    class ExampleClass
    {
        public void ExampleMethod()
        {
            Console.WriteLine(""Zero parameters"");
        }
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadArgCount, Line = 11, Column = 16 } });
        }

        [Fact]
        public void CS1502ERR_BadArgTypes()
        {
            var text = @"
namespace x
{
    public class a
    {
        public a(char i)
        {
        }

        public static void Main()
        {
            a aa = new a(2222);   // CS1502 & CS1503
            if (aa == null) {}
        }
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new ErrorDescription[] {
                //new ErrorDescription { Code = (int)ErrorCode.ERR_BadArgTypes, Line = 12, Column = 24 },  //specifically omitted by roslyn
                 new ErrorDescription { Code = (int)ErrorCode.ERR_BadArgType, Line = 12, Column = 26 }});
        }

        [Fact]
        public void CS1502ERR_BadArgTypes_ConstructorInitializer()
        {
            var text = @"
namespace x
{
    public class a
    {
        public a() : this(""string"") //CS1502, CS1503
        {
        }

        public a(char i)
        {
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                //// (6,22): error CS1502: The best overloaded method match for 'x.a.a(char)' has some invalid arguments
                //Diagnostic(ErrorCode.ERR_BadArgTypes, "this").WithArguments("x.a.a(char)"),  //specifically omitted by roslyn
                // (6,27): error CS1503: Argument 1: cannot convert from 'string' to 'char'
                Diagnostic(ErrorCode.ERR_BadArgType, "\"string\"").WithArguments("1", "string", "char"));
        }

        [Fact]
        public void CS1503ERR_BadArgType01()
        {
            var source =
@"namespace X
{
    public class C
    {
        public C(int i, char c)
        {
        }
        static void M()
        {
            new C(1, 2); // CS1502 & CS1503
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,22): error CS1503: Argument 2: cannot convert from 'int' to 'char'
                //             new C(1, 2); // CS1502 & CS1503
                Diagnostic(ErrorCode.ERR_BadArgType, "2").WithArguments("2", "int", "char").WithLocation(10, 22));
        }

        [Fact]
        public void CS1503ERR_BadArgType02()
        {
            var source =
@"enum E1 { A, B, C }
enum E2 { X, Y, Z }
class C
{
    static void F(int i) { }
    static void G(E1 e) { }
    static void M()
    {
        F(E1.A); // CS1502 & CS1503
        F((E2)E1.B); // CS1502 & CS1503
        F((int)E1.C);
        G(E2.X); // CS1502 & CS1503
        G((E1)E2.Y);
        G((int)E2.Z); // CS1502 & CS1503
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,11): error CS1503: Argument 1: cannot convert from 'E1' to 'int'
                //         F(E1.A); // CS1502 & CS1503
                Diagnostic(ErrorCode.ERR_BadArgType, "E1.A").WithArguments("1", "E1", "int").WithLocation(9, 11),
                // (10,11): error CS1503: Argument 1: cannot convert from 'E2' to 'int'
                //         F((E2)E1.B); // CS1502 & CS1503
                Diagnostic(ErrorCode.ERR_BadArgType, "(E2)E1.B").WithArguments("1", "E2", "int").WithLocation(10, 11),
                // (12,11): error CS1503: Argument 1: cannot convert from 'E2' to 'E1'
                //         G(E2.X); // CS1502 & CS1503
                Diagnostic(ErrorCode.ERR_BadArgType, "E2.X").WithArguments("1", "E2", "E1").WithLocation(12, 11),
                // (14,11): error CS1503: Argument 1: cannot convert from 'int' to 'E1'
                //         G((int)E2.Z); // CS1502 & CS1503
                Diagnostic(ErrorCode.ERR_BadArgType, "(int)E2.Z").WithArguments("1", "int", "E1").WithLocation(14, 11));
        }

        [WorkItem(538939, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538939")]
        [Fact]
        public void CS1503ERR_BadArgType03()
        {
            var source =
@"class C
{
    static void F(out int i)
    {
        i = 0;
    }
    static void M(long arg)
    {
        F(out arg); // CS1503
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,15): error CS1503: Argument 1: cannot convert from 'out long' to 'out int'
                //         F(out arg); // CS1503
                Diagnostic(ErrorCode.ERR_BadArgType, "arg").WithArguments("1", "out long", "out int").WithLocation(9, 15));
        }

        [Fact]
        public void CS1503ERR_BadArgType_MixedMethodsAndTypes()
        {
            var text = @"
class A
{
  public static void Goo(int x) { }
}
class B : A
{
  public class Goo { }
}
class C : B
{
  public static void Goo(string x) { }

  static void Main()
  {
    ((Goo))(1);
  }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] {
                    new ErrorDescription { Code = (int)ErrorCode.WRN_NewRequired, Line = 8, Column = 16, IsWarning = true },
                    new ErrorDescription { Code = (int)ErrorCode.WRN_NewRequired, Line = 12, Column = 22, IsWarning = true },
                    //new ErrorDescription { Code = (int)ErrorCode.ERR_BadArgTypes, Line = 16, Column = 5 },  //specifically omitted by roslyn
                    new ErrorDescription { Code = (int)ErrorCode.ERR_BadArgType, Line = 16, Column = 13 }
                });
        }

        [Fact]
        public void CS1510ERR_RefLvalueExpected_01()
        {
            var text =
@"class C
{
    void M(ref int i)
    {
        M(ref 2); // CS1510, can't pass a number as a ref parameter
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_RefLvalueExpected, Line = 5, Column = 15 });
        }

        [Fact]
        public void CS1510ERR_RefLvalueExpected_02()
        {
            var text =
@"class C
{
    void M()
    {
        var a = new System.Action<int>(ref x => x = 1);
        var b = new System.Action<int, int>(ref (x,y) => x = 1);
        var c = new System.Action<int>(ref delegate (int x) {x = 1;});
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,44): error CS1510: A ref or out argument must be an assignable variable
                //         var a = new System.Action<int>(ref x => x = 1);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x => x = 1").WithLocation(5, 44),
                // (6,49): error CS1510: A ref or out argument must be an assignable variable
                //         var b = new System.Action<int, int>(ref (x,y) => x = 1);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(x,y) => x = 1").WithLocation(6, 49),
                // (7,44): error CS1510: A ref or out argument must be an assignable variable
                //         var c = new System.Action<int>(ref delegate (int x) {x = 1;});
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "delegate (int x) {x = 1;}").WithLocation(7, 44));
        }

        [Fact]
        public void CS1510ERR_RefLvalueExpected_03()
        {
            var text =
@"class C
{
    void M()
    {
        var a = new System.Action<int>(out x => x = 1);
        var b = new System.Action<int, int>(out (x,y) => x = 1);
        var c = new System.Action<int>(out delegate (int x) {x = 1;});
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,44): error CS1510: A ref or out argument must be an assignable variable
                //         var a = new System.Action<int>(out x => x = 1);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x => x = 1").WithLocation(5, 44),
                // (6,49): error CS1510: A ref or out argument must be an assignable variable
                //         var b = new System.Action<int, int>(out (x,y) => x = 1);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(x,y) => x = 1").WithLocation(6, 49),
                // (7,44): error CS1510: A ref or out argument must be an assignable variable
                //         var c = new System.Action<int>(out delegate (int x) {x = 1;});
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "delegate (int x) {x = 1;}").WithLocation(7, 44));
        }

        [Fact]
        public void CS1510ERR_RefLvalueExpected_04()
        {
            var text =
@"class C
{
    void Goo<T>(ref System.Action<T> t) {}
    void Goo<T1,T2>(ref System.Action<T1,T2> t) {}
    void M()
    {
        Goo<int>(ref x => x = 1);
        Goo<int, int>(ref (x,y) => x = 1);
        Goo<int>(ref delegate (int x) {x = 1;});
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,22): error CS1510: A ref or out argument must be an assignable variable
                //         Goo<int>(ref x => x = 1);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x => x = 1").WithLocation(7, 22),
                // (8,27): error CS1510: A ref or out argument must be an assignable variable
                //         Goo<int, int>(ref (x,y) => x = 1);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(x,y) => x = 1").WithLocation(8, 27),
                // (9,22): error CS1510: A ref or out argument must be an assignable variable
                //         Goo<int>(ref delegate (int x) {x = 1;});
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "delegate (int x) {x = 1;}").WithLocation(9, 22));
        }

        [Fact]
        public void CS1510ERR_RefLvalueExpected_05()
        {
            var text =
@"class C
{
    void Goo<T>(out System.Action<T> t) {t = null;}
    void Goo<T1,T2>(out System.Action<T1,T2> t) {t = null;}
    void M()
    {
        Goo<int>(out x => x = 1);
        Goo<int, int>(out (x,y) => x = 1);
        Goo<int>(out delegate (int x) {x = 1;});
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,22): error CS1510: A ref or out argument must be an assignable variable
                //         Goo<int>(out x => x = 1);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x => x = 1").WithLocation(7, 22),
                // (8,27): error CS1510: A ref or out argument must be an assignable variable
                //         Goo<int, int>(out (x,y) => x = 1);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(x,y) => x = 1").WithLocation(8, 27),
                // (9,22): error CS1510: A ref or out argument must be an assignable variable
                //         Goo<int>(out delegate (int x) {x = 1;});
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "delegate (int x) {x = 1;}").WithLocation(9, 22));
        }

        [Fact]
        public void CS1510ERR_RefLvalueExpected_Strict()
        {
            var text =
@"class C
{
    void D(int i) {}
    void M()
    {
        System.Action<int> del = D;

        var a = new System.Action<int>(ref D);
        var b = new System.Action<int>(out D);
        var c = new System.Action<int>(ref del);
        var d = new System.Action<int>(out del);
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular.WithStrictFeature()).VerifyDiagnostics(
                // (8,44): error CS1657: Cannot pass 'D' as a ref or out argument because it is a 'method group'
                //         var a = new System.Action<int>(ref D);
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "D").WithArguments("D", "method group").WithLocation(8, 44),
                // (9,44): error CS1657: Cannot pass 'D' as a ref or out argument because it is a 'method group'
                //         var b = new System.Action<int>(out D);
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "D").WithArguments("D", "method group").WithLocation(9, 44),
                // (10,44): error CS0149: Method name expected
                //         var c = new System.Action<int>(ref del);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "del").WithLocation(10, 44),
                // (11,44): error CS0149: Method name expected
                //         var d = new System.Action<int>(out del);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "del").WithLocation(11, 44));
        }

        [Fact]
        public void CS1511ERR_BaseInStaticMeth()
        {
            var text = @"
public class A
{
   public int j = 0;
}

class C : A
{
   public void Method()
   {
      base.j = 3;   // base allowed here
   }

   public static int StaticMethod()
   {
      base.j = 3;   // CS1511
      return 1;
   }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BaseInStaticMeth, Line = 16, Column = 7 } });
        }

        [Fact]
        public void CS1511ERR_BaseInStaticMeth_Combined()
        {
            var text = @"
using System;

class CLS
{
    static CLS() { var x = base.ToString(); }
    static object FLD = base.ToString();
    static object PROP { get { return base.ToString(); } }
    static object METHOD() { return base.ToString(); }
}

class A : Attribute
{
    public object P;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,25): error CS1511: Keyword 'base' is not available in a static method
                //     static object FLD = base.ToString();
                Diagnostic(ErrorCode.ERR_BaseInStaticMeth, "base"),
                // (6,28): error CS1511: Keyword 'base' is not available in a static method
                //     static CLS() { var x = base.ToString(); }
                Diagnostic(ErrorCode.ERR_BaseInStaticMeth, "base"),
                // (8,39): error CS1511: Keyword 'base' is not available in a static method
                //     static object PROP { get { return base.ToString(); } }
                Diagnostic(ErrorCode.ERR_BaseInStaticMeth, "base"),
                // (9,37): error CS1511: Keyword 'base' is not available in a static method
                //     static object METHOD() { return base.ToString(); }
                Diagnostic(ErrorCode.ERR_BaseInStaticMeth, "base"),
                // (14,19): warning CS0649: Field 'A.P' is never assigned to, and will always have its default value null
                //     public object P;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "P").WithArguments("A.P", "null")
                );
        }

        [Fact]
        public void CS1512ERR_BaseInBadContext()
        {
            var text = @"
using System;

class Base { }

class CMyClass : Base
{
    private String xx = base.ToString();   // CS1512
    
    public static void Main()
    {
        CMyClass z = new CMyClass();
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BaseInBadContext, Line = 8, Column = 25 } });
        }

        [Fact]
        public void CS1512ERR_BaseInBadContext_AttributeArgument()
        {
            var text = @"
using System;

[assembly: A(P = base.ToString())]

public class A : Attribute
{
    public object P;
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BaseInBadContext, Line = 4, Column = 18 } });
        }

        [Fact]
        public void CS1520ERR_MemberNeedsType_02()
        {
            CreateCompilation(
@"class Program
{
    Main() {}
    Helper() {}
    \u0050rogram(int x) {}
}")
                .VerifyDiagnostics(
                // (3,5): error CS1520: Method must have a return type
                //     Main() {}
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "Main"),
                // (4,5): error CS1520: Method must have a return type
                //     Helper() {}
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "Helper").WithLocation(4, 5)
                );
        }

        [Fact]
        public void CS1525ERR_InvalidExprTerm()
        {
            CreateCompilation(
@"public class MyClass {

    public static int Main() {
        bool b = string is string;
        return 1;
    }

}")
                .VerifyDiagnostics(Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string"));
        }

        [WorkItem(543167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543167")]
        [Fact]
        public void CS1525ERR_InvalidExprTerm_1()
        {
            CreateCompilation(
@"class D
{
    public static void Main()
    { 
        var s = 1?;
    }
}
")
            .VerifyDiagnostics(
                // (5,19): error CS1525: Invalid expression term ';'
                //         var s = 1?;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";"),
                // (5,19): error CS1003: Syntax error, ':' expected
                //         var s = 1?;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":", ";"),
                // (5,19): error CS1525: Invalid expression term ';'
                //         var s = 1?;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";"),
                // (5,17): error CS0029: Cannot implicitly convert type 'int' to 'bool'
                //         var s = 1?;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "bool")
                );
        }

        [Fact]
        public void CS1525ERR_InvalidExprTerm_ConditionalOperator()
        {
            CreateCompilation(
@"class Program
{
    static void Main(string[] args)
    {
        int x = 1;
        int y = 1;
        System.Console.WriteLine(((x == y)) ?); // Invalid
        System.Console.WriteLine(((x == y)) ? (x++)); // Invalid
        System.Console.WriteLine(((x == y)) ? (x++) : (x++) : ((((y++)))));    // Invalid
        System.Console.WriteLine(((x == y)) ?  : :); 	// Invalid
    }
}
")
                .VerifyDiagnostics(Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")"),
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":", ")"),
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")"),
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":", ")"),
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")"),
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",", ":"),
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",", "("),
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":"),
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":"),
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",", ":"));
        }

        [WorkItem(528657, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528657")]
        [Fact]
        public void CS0106ERR_BadMemberFlag()
        {
            CreateCompilation(
@"new class MyClass
{
}")
                .VerifyDiagnostics(Diagnostic(ErrorCode.ERR_BadMemberFlag, "MyClass").WithArguments("new"));
        }

        [Fact]
        public void CS1540ERR_BadProtectedAccess01()
        {
            var text = @"
namespace CS1540
{
    class Program1
    {
        static void Main()
        {
            Employee.PreparePayroll();
        }
    }

    class Person
    {
        protected virtual void CalculatePay() 
        {
        }
    }

    class Manager : Person
    {
        protected override void CalculatePay() 
        {
        }
    }

    class Employee : Person
    {
        public static void PreparePayroll()
        {
            Employee emp1 = new Employee();
            Person emp2 = new Manager();
            Person emp3 = new Employee();
            emp1.CalculatePay(); 
            emp2.CalculatePay();
            emp3.CalculatePay();
        }
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadProtectedAccess, Line = 34, Column = 18 },
                                        new ErrorDescription { Code = (int)ErrorCode.ERR_BadProtectedAccess, Line = 35, Column = 18 }});
        }

        [Fact]
        public void CS1540ERR_BadProtectedAccess02()
        {
            var text =
@"class A
{
    protected object F;
    protected void M()
    {
    }
    protected object P { get; set; }
    public object Q { get; protected set; }
    public object R { protected get; set; }
    public object S { private get; set; }
}
class B : A
{
    void M(object o)
    {
        // base.
        base.M();
        base.P = base.F;
        base.Q = null;
        M(base.R);
        M(base.S);
        // a.
        A a = new A();
        a.M();
        a.P = a.F;
        a.Q = null;
        M(a.R);
        M(a.S);
        // G().
        G().M();
        G().P = G().F;
        G().Q = null;
        M(G().R);
        M(G().S);
        // no qualifier
        M();
        P = F;
        Q = null;
        M(R);
        M(S);
        // this.
        this.M();
        this.P = this.F;
        this.Q = null;
        M(this.R);
        M(this.S);
        // ((A)this).
        ((A)this).M();
        ((A)this).P = ((A)this).F;
        ((A)this).Q = null;
        M(((A)this).R);
        M(((A)this).S);
    }
    static A G()
    {
        return null;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (21,11): error CS0271: The property or indexer 'A.S' cannot be used in this context because the get accessor is inaccessible
                //         M(base.S);
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "base.S").WithArguments("A.S"),
                // (24,11): error CS1540: Cannot access protected member 'A.M()' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         a.M();
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "M").WithArguments("A.M()", "A", "B"),
                // (25,11): error CS1540: Cannot access protected member 'A.P' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         a.P = a.F;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "P").WithArguments("A.P", "A", "B"),
                // (25,17): error CS1540: Cannot access protected member 'A.F' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         a.P = a.F;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "F").WithArguments("A.F", "A", "B"),
                // (26,9): error CS1540: Cannot access protected member 'A.Q' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         a.Q = null;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "a.Q").WithArguments("A.Q", "A", "B"),
                // (27,11): error CS1540: Cannot access protected member 'A.R' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         M(a.R);
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "a.R").WithArguments("A.R", "A", "B"),
                // (28,11): error CS0271: The property or indexer 'A.S' cannot be used in this context because the get accessor is inaccessible
                //         M(a.S);
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "a.S").WithArguments("A.S"),
                // (30,13): error CS1540: Cannot access protected member 'A.M()' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         G().M();
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "M").WithArguments("A.M()", "A", "B"),
                // (31,13): error CS1540: Cannot access protected member 'A.P' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         G().P = G().F;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "P").WithArguments("A.P", "A", "B"),
                // (31,21): error CS1540: Cannot access protected member 'A.F' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         G().P = G().F;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "F").WithArguments("A.F", "A", "B"),
                // (32,9): error CS1540: Cannot access protected member 'A.Q' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         G().Q = null;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "G().Q").WithArguments("A.Q", "A", "B"),
                // (33,11): error CS1540: Cannot access protected member 'A.R' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         M(G().R);
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "G().R").WithArguments("A.R", "A", "B"),
                // (34,11): error CS0271: The property or indexer 'A.S' cannot be used in this context because the get accessor is inaccessible
                //         M(G().S);
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "G().S").WithArguments("A.S"),
                // (40,11): error CS0271: The property or indexer 'A.S' cannot be used in this context because the get accessor is inaccessible
                //         M(S);
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "S").WithArguments("A.S"),
                // (46,11): error CS0271: The property or indexer 'A.S' cannot be used in this context because the get accessor is inaccessible
                //         M(this.S);
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "this.S").WithArguments("A.S"),
                // (48,19): error CS1540: Cannot access protected member 'A.M()' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         ((A)this).M();
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "M").WithArguments("A.M()", "A", "B"),
                // (49,19): error CS1540: Cannot access protected member 'A.P' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         ((A)this).P = ((A)this).F;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "P").WithArguments("A.P", "A", "B"),
                // (49,33): error CS1540: Cannot access protected member 'A.F' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         ((A)this).P = ((A)this).F;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "F").WithArguments("A.F", "A", "B"),
                // (50,9): error CS1540: Cannot access protected member 'A.Q' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         ((A)this).Q = null;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "((A)this).Q").WithArguments("A.Q", "A", "B"),
                // (51,11): error CS1540: Cannot access protected member 'A.R' via a qualifier of type 'A'; the qualifier must be of type 'B' (or derived from it)
                //         M(((A)this).R);
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "((A)this).R").WithArguments("A.R", "A", "B"),
                // (52,11): error CS0271: The property or indexer 'A.S' cannot be used in this context because the get accessor is inaccessible
                //         M(((A)this).S);
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "((A)this).S").WithArguments("A.S"),
                // (3,22): warning CS0649: Field 'A.F' is never assigned to, and will always have its default value null
                //     protected object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("A.F", "null")
                );
        }

        [WorkItem(540271, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540271")]
        [Fact]
        public void CS0122ERR_BadAccessProtectedCtor()
        {
            // It is illegal to access any "protected" instance method with a "this" that is not of the 
            // current class's type. Oddly enough, that includes constructors. It is legal to call
            // a protected ctor via ": base()" because then the "this" is of the derived type. But
            // in a derived class you cannot call "new MyBase()" if the ctor is protected.
            //
            // The native compiler produces the error CS1540 whether the offending method is a regular
            // method or a ctor:
            //
            //   Cannot access protected member 'MyBase.MyBase' via a qualifier of type 'MyBase'; 
            //   the qualifier must be of type 'Derived' (or derived from it)
            //
            // Though technically correct, this is a very confusing error message for this scenario;
            // one does not typically think of the constructor as being a method that is 
            // called with an implicit "this" of a particular receiver type, even though of course
            // that is exactly what it is.
            //
            // The better error message here is to simply say that the best possible ctor cannot
            // be accessed because it is not accessible. That's what Roslyn does.
            //
            // CONSIDER: We might consider making up a new error message for this situation.

            // 
            // CS0122: 'Base.Base' is inaccessible due to its protection level

            var text = @"namespace CS0122
{
    public class Base
    {
        protected Base() {}
    }

    public class Derived : Base
    {
        void M()
        {
            Base b = new Base(); 
        }
    }
}
";
            CreateCompilation(text).
               VerifyDiagnostics(Diagnostic(ErrorCode.ERR_BadAccess, "Base").WithArguments("CS0122.Base.Base()"));
        }

        // CS1545ERR_BindToBogusProp2 --> Symbols\Source\EventTests.cs
        // CS1546ERR_BindToBogusProp1 --> Symbols\Source\PropertyTests.cs

        [WorkItem(528658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528658")]
        [Fact()]
        public void CS1560ERR_FileNameTooLong()
        {
            var text = @"
#line 1 ""ccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc""

public class C {
    public void Main ()
        {
        }
}
";
            //EDMAURER no need to enforce a limit here.
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);//,
                                                                             //new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_FileNameTooLong, Line = 1, Column = 25 } });
        }

        [Fact]
        public void CS1579ERR_ForEachMissingMember()
        {
            var text = @"
using System;
public class MyCollection
{
    int[] items;
    public MyCollection()
    {
        items = new int[5] { 12, 44, 33, 2, 50 };
    }

    MyEnumerator GetEnumerator()
    {
        return new MyEnumerator(this);
    }

    public class MyEnumerator
    {
        int nIndex;
        MyCollection collection;
        public MyEnumerator(MyCollection coll)
        {
            collection = coll;
            nIndex = -1;
        }

        public bool MoveNext()
        {
            nIndex++;
            return (nIndex < collection.items.GetLength(0));
        }

        public int Current
        {
            get
            {
                return (collection.items[nIndex]);
            }
        }
    }

    public static void Main()
    {
        MyCollection col = new MyCollection();
        Console.WriteLine(""Values in the collection are:"");
        foreach (int i in col)   // CS1579
        {
            Console.WriteLine(i);
        }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (45,27): warning CS0279: 'MyCollection' does not implement the 'collection' pattern. 'MyCollection.GetEnumerator()' is either static or not public.
                //         foreach (int i in col)   // CS1579
                Diagnostic(ErrorCode.WRN_PatternStaticOrInaccessible, "col").WithArguments("MyCollection", "collection", "MyCollection.GetEnumerator()"),
                // (45,27): error CS1579: foreach statement cannot operate on variables of type 'MyCollection' because 'MyCollection' does not contain a public definition for 'GetEnumerator'
                //         foreach (int i in col)   // CS1579
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "col").WithArguments("MyCollection", "GetEnumerator"));
        }

        [Fact]
        public void CS1579ERR_ForEachMissingMember02()
        {
            var text = @"
public class Test
{
    public static void Main(string[] args)
    {
        foreach (int x in F(1)) { }
    }
    static void F(int x) { }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_ForEachMissingMember, "F(1)").WithArguments("void", "GetEnumerator"));
        }

        [Fact]
        public void CS1593ERR_BadDelArgCount()
        {
            var text = @"
using System;
delegate string func(int i);   // declare delegate

class a
{
    public static void Main()
    {
        func dt = new func(z);
        x(dt);
    }

    public static string z(int j)
    {
        Console.WriteLine(j);
        return j.ToString();
    }

    public static void x(func hello)
    {
        hello(8, 9);   // CS1593
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadDelArgCount, Line = 21, Column = 9 } });
        }

        [Fact]
        public void CS1593ERR_BadDelArgCount_02()
        {
            var text = @"
delegate void MyDelegate1(int x, float y);
class Program
{
    public void DelegatedMethod(int x, float y = 3.0f) { System.Console.WriteLine(y); }
    static void Main(string[] args)
    {
        Program mc = new Program();
        MyDelegate1 md1 = null;
        md1 += mc.DelegatedMethod;
        md1(1);
        md1 -= mc.DelegatedMethod;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'y' of 'MyDelegate1'
                //         md1(1);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "md1").WithArguments("y", "MyDelegate1").WithLocation(11, 9));
        }

        [Fact]
        public void CS1593ERR_BadDelArgCount_03()
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
        new Action<int>(Console.WriteLine)(1, 1);
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_BadDelArgCount, "new Action<int>(Console.WriteLine)").WithArguments("System.Action<int>", "2"));
        }

        [Fact()]
        public void CS1594ERR_BadDelArgTypes()
        {
            var text = @"
using System;
delegate string func(int i);   // declare delegate

class a
{
    public static void Main()
    {
        func dt = new func(z);
        x(dt);
    }

    public static string z(int j)
    {
        Console.WriteLine(j);
        return j.ToString();
    }

    public static void x(func hello)
    {
        hello(""8"");   // CS1594
    }
}
";
            //EDMAURER Giving errors for the individual argument problems is better than generic "delegate 'blah' has some invalid arguments"
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadArgType, Line = 21, Column = 15 } });
            //new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadDelArgTypes, Line = 21, Column = 9 } });
        }

        // TODO: change this to CS0051 in Roslyn?
        [Fact]
        public void CS1604ERR_AssgReadonlyLocal()
        {
            var text = @"
class C
{
    void M()
    {
        this = null;
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (6,9): error CS1604: Cannot assign to 'this' because it is read-only
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this"));
        }

        [Fact]
        public void CS1605ERR_RefReadonlyLocal()
        {
            var text = @"
class C
{
    void Test()
    {
        Ref(ref this); //CS1605
        Out(out this); //CS1605
    }

    static void Ref(ref C c)
    {
    }

    static void Out(out C c)
    {
        c = null;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,17): error CS1605: Cannot pass 'this' as a ref or out argument because it is read-only
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "this").WithArguments("this"),
                // (7,17): error CS1605: Cannot pass 'this' as a ref or out argument because it is read-only
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "this").WithArguments("this"));
        }

        [Fact]
        public void CS1612ERR_ReturnNotLValue01()
        {
            var text = @"
public struct MyStruct
{
    public int Width;
}

public class ListView
{
    MyStruct ms;
    public MyStruct Size
    {
        get { return ms; }
        set { ms = value; }
    }
}

public class MyClass
{
    public MyClass()
    {
        ListView lvi;
        lvi = new ListView();
        lvi.Size.Width = 5; // CS1612
    }

    public static void Main()
    {
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_ReturnNotLValue, Line = 23, Column = 9 } });
        }

        /// <summary>
        /// Breaking change from Dev10. CS1612 is now reported for all value
        /// types, not just struct types. Specifically, CS1612 is now reported
        /// for type parameters constrained to "struct". (See also CS0131.)
        /// </summary>
        [WorkItem(528821, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528821")]
        [Fact]
        public void CS1612ERR_ReturnNotLValue02()
        {
            var source =
@"interface I
{
    object P { get; set; }
}
struct S : I
{
    public object P { get; set; }
}
class C<T, U, V>
    where T : struct, I
    where U : class, I
    where V : I
{
    S F1 { get; set; }
    T F2 { get; set; }
    U F3 { get; set; }
    V F4 { get; set; }
    void M()
    {
        F1.P = null;
        F2.P = null;
        F3.P = null;
        F4.P = null;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (20,9): error CS1612: Cannot modify the return value of 'C<T, U, V>.F1' because it is not a variable
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "F1").WithArguments("C<T, U, V>.F1").WithLocation(20, 9),
                // (20,9): error CS1612: Cannot modify the return value of 'C<T, U, V>.F2' because it is not a variable
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "F2").WithArguments("C<T, U, V>.F2").WithLocation(21, 9));
        }

        [Fact]
        public void CS1615ERR_BadArgExtraRef()
        {
            var text = @"
class C
{
   public void f(int i) {}
   public static void Main()
   {
      int i = 1;
      f(ref i);  // CS1615
   }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new ErrorDescription[] {
                //new ErrorDescription { Code = (int)ErrorCode.ERR_BadArgTypes, Line = 8, Column = 7 },  //specifically omitted by roslyn
                    new ErrorDescription { Code = (int)ErrorCode.ERR_BadArgExtraRef, Line = 8, Column = 13 } });
        }

        [Fact()]
        public void CS1618ERR_DelegateOnConditional()
        {
            var text = @"
using System.Diagnostics;

delegate void del();

class MakeAnError
{
    public static void Main()
    {
        del d = new del(ConditionalMethod);   // CS1618
    }
    [Conditional(""DEBUG"")]
    public static void ConditionalMethod()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,25): error CS1618: Cannot create delegate with 'MakeAnError.ConditionalMethod()' because it has a Conditional attribute
                //         del d = new del(ConditionalMethod);   // CS1618
                Diagnostic(ErrorCode.ERR_DelegateOnConditional, "ConditionalMethod").WithArguments("MakeAnError.ConditionalMethod()").WithLocation(10, 25));
        }

        [Fact()]
        public void CS1618ERR_DelegateOnConditional_02()
        {
            var text = @"
using System;
using System.Diagnostics;

delegate void del();

class MakeAnError
{
    class Goo: Attribute
    {
        public Goo(object o) {}
    }

    [Conditional(""DEBUG"")]
    [Goo(new del(ConditionalMethod))] // CS1618
    public static void ConditionalMethod()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (15,18): error CS1618: Cannot create delegate with 'MakeAnError.ConditionalMethod()' because it has a Conditional attribute
                //     [Goo(new del(ConditionalMethod))] // CS1618
                Diagnostic(ErrorCode.ERR_DelegateOnConditional, "ConditionalMethod").WithArguments("MakeAnError.ConditionalMethod()").WithLocation(15, 18));
        }

        [Fact]
        public void CS1620ERR_BadArgRef()
        {
            var text = @"
class C
{
    void f(ref int i) { }
    public static void Main()
    {
        int x = 1;
        f(out x);  // CS1620 - f takes a ref parameter, not an out parameter
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new ErrorDescription[] {
                //new ErrorDescription { Code = (int)ErrorCode.ERR_BadArgTypes, Line = 8, Column = 9 },  //specifically omitted by roslyn
                    new ErrorDescription { Code = (int)ErrorCode.ERR_BadArgRef, Line = 8, Column = 15 } });
        }

        [Fact]
        public void CS1621ERR_YieldInAnonMeth()
        {
            var text = @"
using System.Collections;

delegate object MyDelegate();

class C : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        MyDelegate d = delegate
        {
            yield return this; // CS1621
            return this;
        };
        d();
    }

    public static void Main()
    {
    }
}
";
            var comp = CreateCompilation(text);
            var expected = new DiagnosticDescription[] {
                // (12,13): error CS1621: The yield statement cannot be used inside an anonymous method or lambda expression
                //             yield return this; // CS1621
                Diagnostic(ErrorCode.ERR_YieldInAnonMeth, "yield"),
                // (8,24): error CS0161: 'C.GetEnumerator()': not all code paths return a value
                //     public IEnumerator GetEnumerator()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "GetEnumerator").WithArguments("C.GetEnumerator()")
            };
            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void CS1622ERR_ReturnInIterator()
        {
            var text = @"
using System.Collections;

class C : IEnumerable
{
   public IEnumerator GetEnumerator()
   {
      return (IEnumerator) this;  // CS1622
      yield return this;   // OK
   }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (8,7): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //       return (IEnumerator) this;  // CS1622
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return"),
                // (9,7): warning CS0162: Unreachable code detected
                //       yield return this;   // OK
                Diagnostic(ErrorCode.WRN_UnreachableCode, "yield")
                );
        }

        [Fact]
        public void CS1623ERR_BadIteratorArgType()
        {
            var text = @"
using System.Collections;

class C : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return 0;
    }

    public IEnumerator GetEnumerator(ref int i)  // CS1623
    {
        yield return i;
    }

    public IEnumerator GetEnumerator(out float f)  // CS1623
    {
        f = 0.0F;
        yield return f;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (11,46): error CS1623: Iterators cannot have ref, in or out parameters
                //     public IEnumerator GetEnumerator(ref int i)  // CS1623
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "i"),
                // (16,48): error CS1623: Iterators cannot have ref, in or out parameters
                //     public IEnumerator GetEnumerator(out float f)  // CS1623
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "f")
                );
        }

        [Fact]
        public void CS1624ERR_BadIteratorReturn()
        {
            var text = @"
class C
{
    public int Iterator
    {
        get  // CS1624
        {
            yield return 1;
        }
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadIteratorReturn, Line = 6, Column = 9 } });
        }

        [Fact]
        public void CS1625ERR_BadYieldInFinally()
        {
            var text = @"
using System.Collections;

class C : IEnumerable
{
   public IEnumerator GetEnumerator()
   {
      try
      {
      }
      finally
      {
        yield return this;  // CS1625
      }
   }
}

public class CMain
{
   public static void Main() { }
}

";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (13,9): error CS1625: Cannot yield in the body of a finally clause
                //         yield return this;  // CS1625
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield")
                );
        }

        [Fact]
        public void CS1626ERR_BadYieldInTryOfCatch()
        {
            var text = @"
using System.Collections;

class C : IEnumerable
{
   public IEnumerator GetEnumerator()
   {
      try
      {
         yield return this;  // CS1626
      }
      catch
      {
        
      }
   }
}

public class CMain
{
   public static void Main() { }
}

";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (10,10): error CS1626: Cannot yield a value in the body of a try block with a catch clause
                //          yield return this;  // CS1626
                Diagnostic(ErrorCode.ERR_BadYieldInTryOfCatch, "yield")
                );
        }

        [Fact]
        public void CS1628ERR_AnonDelegateCantUse()
        {
            var text = @"
delegate int MyDelegate();

class C
{
    public static void F(ref int i)
    {
        MyDelegate d = delegate { return i; };  // CS1628
    }

    public static void Main()
    {
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonDelegateCantUse, Line = 8, Column = 42 } });
        }

        [Fact]
        public void CS1629ERR_IllegalInnerUnsafe()
        {
            var text = @"
using System.Collections.Generic;
class C 
{
   IEnumerator<int> IteratorMeth() {
      int i;
      unsafe  // CS1629
      {
         int *p = &i;
         yield return *p;
      }
   }

    unsafe IEnumerator<int> IteratorMeth2() {   // CS1629
        yield break;
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,7): error CS1629: Unsafe code may not appear in iterators
                //       unsafe  // CS1629
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "unsafe"),
                // (9,10): error CS1629: Unsafe code may not appear in iterators
                //          int *p = &i;
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "int *"),
                // (9,19): error CS1629: Unsafe code may not appear in iterators
                //          int *p = &i;
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "&i"),
                // (10,24): error CS1629: Unsafe code may not appear in iterators
                //          yield return *p;
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "p"),
                // (14,29): error CS1629: Unsafe code may not appear in iterators
                //     unsafe IEnumerator<int> IteratorMeth2() {   // CS1629
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "IteratorMeth2")
                );
        }

        [Fact]
        public void CS1631ERR_BadYieldInCatch()
        {
            var text = @"
using System;
using System.Collections;

public class C : IEnumerable
{
   public IEnumerator GetEnumerator() 
   {
      try
      {
      }
      catch(Exception e)
      {
        yield return this;  // CS1631
      }
   }  

   public static void Main() 
   {
   }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (14,9): error CS1631: Cannot yield a value in the body of a catch clause
                //         yield return this;  // CS1631
                Diagnostic(ErrorCode.ERR_BadYieldInCatch, "yield"),
                // (12,23): warning CS0168: The variable 'e' is declared but never used
                //       catch(Exception e)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e")
                );
        }

        [Fact]
        public void CS1632ERR_BadDelegateLeave()
        {
            var text = @"
delegate void MyDelegate();
class MyClass
{
   public void Test()
   {      
      for (int i = 0 ; i < 5 ; i++)
      {
         MyDelegate d = delegate {
            break;   // CS1632
          };        
      }
   }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (10,13): error CS1632: Control cannot leave the body of an anonymous method or lambda expression
//             break;   // CS1632
Diagnostic(ErrorCode.ERR_BadDelegateLeave, "break")
                );
        }

        [Fact]
        public void CS1636ERR_VarargsIterator()
        {
            var text = @"using System.Collections;

public class Test
{
    IEnumerable Goo(__arglist)
    {
        yield return 1;
    }

    static int Main(string[] args)
    {
        return 1;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (5,17): error CS1636: __arglist is not allowed in the parameter list of iterators
//     IEnumerable Goo(__arglist)
Diagnostic(ErrorCode.ERR_VarargsIterator, "Goo"));
        }

        [Fact]
        public void CS1637ERR_UnsafeIteratorArgType()
        {
            var text = @"
using System.Collections;

public unsafe class C
{
    public IEnumerator Iterator1(int* p)  // CS1637
    {
        yield return null;
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,39): error CS1637: Iterators cannot have unsafe parameters or yield types
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "p"));
        }

        [Fact()]
        public void CS1639ERR_BadCoClassSig()
        {
            // BREAKING CHANGE:     Dev10 allows this test to compile, even though the output assembly is not verifiable and generates a runtime exception:
            // BREAKING CHANGE:     We disallow CoClass creation if coClassType is an unbound generic type and report a compile time error.

            var text = @"
using System.Runtime.InteropServices;

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(GenericClass<>))]
public interface InterfaceType {}

public class GenericClass<T>: InterfaceType {}

public class Program
{
    public static void Main() { var i = new InterfaceType(); }
}
        ";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_BadCoClassSig, Line = 12, Column = 41 } }
                );
        }

        [Fact()]
        public void CS1640ERR_MultipleIEnumOfT()
        {
            var text = @"
using System.Collections;
using System.Collections.Generic;

public class C : IEnumerable, IEnumerable<int>, IEnumerable<string>
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
        yield break;
    }

    IEnumerator<string> IEnumerable<string>.GetEnumerator()
    {
        yield break;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (IEnumerator)((IEnumerable<string>)this).GetEnumerator();
    }
}

public class Test
{
    public static int Main()
    {
        foreach (int i in new C()) { }    // CS1640
        return 1;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_MultipleIEnumOfT, Line = 27, Column = 27 } });
        }

        [WorkItem(7389, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CS1640ERR_MultipleIEnumOfT02()
        {
            var text = @"
using System.Collections.Generic;
public class Test
{
    public static void Main(string[] args)
    {
    }
}
public class C<T> where T : IEnumerable<int>, IEnumerable<string>
{
    public static void TestForeach(T t)
    {
        foreach (int i in t) { }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "t").WithArguments("T", "collection", "System.Collections.Generic.IEnumerable<int>.GetEnumerator()", "System.Collections.Generic.IEnumerable<string>.GetEnumerator()"),
                                  Diagnostic(ErrorCode.ERR_MultipleIEnumOfT, "t").WithArguments("T", "System.Collections.Generic.IEnumerable<T>"));
        }

        [Fact]
        public void CS1643ERR_AnonymousReturnExpected()
        {
            var text = @"
delegate int MyDelegate();

class C
{
    static void Main()
    {
        MyDelegate d = delegate
        {                 // CS1643
            int i = 0;
            if (i == 0)
                return 1;
        };
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,24): error CS1643: Not all code paths return a value in anonymous method of type 'MyDelegate'
                //         MyDelegate d = delegate
                Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "delegate").WithArguments("anonymous method", "MyDelegate").WithLocation(8, 24)
                );
        }

        [Fact]
        public void CS1643ERR_AnonymousReturnExpected_Foreach()
        {
            var text = @"
using System;
public class Test
{
    public static void Main(string[] args)
    {
        string[] arr = null;
        Func<int> f = () => { foreach (var x in arr) return x; };
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
    // (8,61): error CS0029: Cannot implicitly convert type 'string' to 'int'
    //         Func<int> f = () => { foreach (var x in arr) return x; };
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("string", "int").WithLocation(8, 61),
    // (8,61): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
    //         Func<int> f = () => { foreach (var x in arr) return x; };
    Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "x").WithArguments("lambda expression").WithLocation(8, 61),
    // (8,26): error CS1643: Not all code paths return a value in lambda expression of type 'Func<int>'
    //         Func<int> f = () => { foreach (var x in arr) return x; };
    Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "=>").WithArguments("lambda expression", "System.Func<int>").WithLocation(8, 26)
                );
        }

        [Fact]
        public void CS1648ERR_AssgReadonly2()
        {
            var text = @"
public struct Inner
  {
    public int i;
  }

class Outer
{  
  public readonly Inner inner = new Inner();
}

class D
{
   static void Main()
   {
      Outer outer = new Outer();
      outer.inner.i = 1;  // CS1648
   }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AssgReadonly2, Line = 17, Column = 7 } });
        }

        [Fact]
        public void CS1649ERR_RefReadonly2()
        {
            var text = @"
public struct Inner
{
    public int i;
}

class Outer
{
    public readonly Inner inner = new Inner();
}

class D
{
    static void f(ref int iref)
    {
    }

    static void Main()
    {
        Outer outer = new Outer();
        f(ref outer.inner.i);  // CS1649
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
    // (21,15): error CS1649: Members of readonly field 'Outer.inner' cannot be used as a ref or out value (except in a constructor)
    //         f(ref outer.inner.i);  // CS1649
    Diagnostic(ErrorCode.ERR_RefReadonly2, "outer.inner.i").WithArguments("Outer.inner").WithLocation(21, 15)
);
        }

        [Fact]
        public void CS1650ERR_AssgReadonlyStatic2()
        {
            string text =
@"public struct Inner
{
    public int i;
}

class Outer
{
    public static readonly Inner inner = new Inner();
}

class D
{
    static void Main()
    {
        Outer.inner.i = 1;  // CS1650
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (15,9): error CS1650: Fields of static readonly field 'Outer.inner' cannot be assigned to (except in a static constructor or a variable initializer)
                Diagnostic(ErrorCode.ERR_AssgReadonlyStatic2, "Outer.inner.i").WithArguments("Outer.inner"));
        }

        [Fact]
        public void CS1651ERR_RefReadonlyStatic2()
        {
            var text = @"
public struct Inner
{
    public int i;
}

class Outer
{
    public static readonly Inner inner = new Inner();
}

class D
{
    static void f(ref int iref)
    {
    }

    static void Main()
    {
        f(ref Outer.inner.i);  // CS1651
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (20,15): error CS1651: Fields of static readonly field 'Outer.inner' cannot be passed ref or out (except in a static constructor)
                Diagnostic(ErrorCode.ERR_RefReadonlyStatic2, "Outer.inner.i").WithArguments("Outer.inner"));
        }

        [Fact]
        public void CS1654ERR_AssgReadonlyLocal2Cause()
        {
            var text = @"
using System.Collections.Generic;

namespace CS1654
{
    struct Book
    {
        public string Title;
        public string Author;
        public double Price;
        public Book(string t, string a, double p)
        {
            Title = t;
            Author = a;
            Price = p;

        }
    }

    class Program
    {
        List<Book> list;
        static void Main(string[] args)
        {
            Program prog = new Program();
            prog.list = new List<Book>();
            foreach (Book b in prog.list)
            {
                b.Price += 9.95; // CS1654
            }
        }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (29,17): error CS1654: Cannot modify members of 'b' because it is a 'foreach iteration variable'
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal2Cause, "b.Price").WithArguments("b", "foreach iteration variable"));
        }

        [Fact]
        public void CS1655ERR_RefReadonlyLocal2Cause()
        {
            var text = @"
struct S 
{
   public int i;
}

class CMain
{
  static void f(ref int iref)
  {
  }
  
  public static void Main()
  {
     S[] sa = new S[10];
     foreach(S s in sa)
     {
        CMain.f(ref s.i);  // CS1655
     }
  }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (18,21): error CS1655: Cannot pass fields of 's' as a ref or out argument because it is a 'foreach iteration variable'
                //         CMain.f(ref s.i);  // CS1655
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal2Cause, "s.i").WithArguments("s", "foreach iteration variable")
                );
        }

        [Fact]
        public void CS1656ERR_AssgReadonlyLocalCause01()
        {
            var text = @"
using System;

class C : IDisposable
{
    public void Dispose() { }
}

class CMain
{
    unsafe public static void Main()
    {
        using (C c = new C())
        {
            c = new C(); // CS1656
        }

        int[] ary = new int[] { 1, 2, 3, 4 };
        fixed (int* p = ary)
        {
            p = null; // CS1656
        }
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (13,13): error CS1656: Cannot assign to 'c' because it is a 'using variable'
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "c").WithArguments("c", "using variable").WithLocation(15, 13),
                // (19,13): error CS1656: Cannot assign to 'p' because it is a 'fixed variable'
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "p").WithArguments("p", "fixed variable").WithLocation(21, 13));
        }

        [Fact]
        public void CS1656ERR_AssgReadonlyLocalCause02()
        {
            var text =
@"class C
{
    static void M()
    {
        M = null;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,9): error CS1656: Cannot assign to 'M' because it is a 'method group'
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "M").WithArguments("M", "method group").WithLocation(5, 9));
        }

        [Fact]
        public void CS1656ERR_AssgReadonlyLocalCause_NestedForeach()
        {
            var text = @"
public class Test
{
    static public void Main(string[] args)
    {
        string S = ""ABC"";
        string T = ""XYZ"";
        foreach (char x in S)
        {
            foreach (char y in T)
            {
                x = 'M';
            }
        }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x").WithArguments("x", "foreach iteration variable"));
        }

        [Fact]
        public void CS1657ERR_RefReadonlyLocalCause()
        {
            var text = @"
class C
{
    static void F(ref string s)
    {
    }

    static void Main(string[] args)
    {
        foreach (var a in args)
        {
            F(ref a); //CS1657
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
    // (12,19): error CS1657: Cannot use 'a' as a ref or out value because it is a 'foreach iteration variable'
    //             F(ref a); //CS1657
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "a").WithArguments("a", "foreach iteration variable").WithLocation(12, 19)
                );
        }

        [Fact]
        public void CS1660ERR_AnonMethToNonDel()
        {
            var text = @"
delegate int MyDelegate();
class C {
   static void Main()
   {
     int i = delegate { return 1; };  // CS1660
   }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_AnonMethToNonDel, Line = 6, Column = 14 } });
        }

        [Fact]
        public void CS1661ERR_CantConvAnonMethParams()
        {
            var text = @"
delegate void MyDelegate(int i);

class C
{
    public static void Main()
    {
        MyDelegate d = delegate(string s) { };  // CS1661
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] {
                    new ErrorDescription { Code = (int)ErrorCode.ERR_CantConvAnonMethParams, Line = 8, Column = 24 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_BadParamType, Line = 8, Column = 40 }
                });
        }

        [Fact]
        public void CS1662ERR_CantConvAnonMethReturns()
        {
            var text = @"
delegate int MyDelegate(int i);

class C
{
    delegate double D();
    public static void Main()
    {
        MyDelegate d = delegate(int i) { return 1.0; };  // CS1662
        D dd = () => { return ""Who knows the real sword of Gryffindor?""; };
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
    // (9,49): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
    //         MyDelegate d = delegate(int i) { return 1.0; };  // CS1662
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1.0").WithArguments("double", "int").WithLocation(9, 49),
    // (9,49): error CS1662: Cannot convert anonymous method to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
    //         MyDelegate d = delegate(int i) { return 1.0; };  // CS1662
    Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "1.0").WithArguments("anonymous method").WithLocation(9, 49),
    // (10,31): error CS0029: Cannot implicitly convert type 'string' to 'double'
    //         D dd = () => { return "Who knows the real sword of Gryffindor?"; };
    Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""Who knows the real sword of Gryffindor?""").WithArguments("string", "double").WithLocation(10, 31),
    // (10,31): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
    //         D dd = () => { return "Who knows the real sword of Gryffindor?"; };
    Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, @"""Who knows the real sword of Gryffindor?""").WithArguments("lambda expression").WithLocation(10, 31)
                 );
        }

        [Fact]
        public void CS1666ERR_FixedBufferNotFixedErr()
        {
            var text = @"
unsafe struct S
{
    public fixed int buffer[1];
}

unsafe class Test
{
    public static void Main()
    {
        var inst = new Test();
        System.Console.Write(inst.example1());
        System.Console.Write(inst.field.buffer[0]);
        System.Console.Write(inst.example2());
        System.Console.Write(inst.field.buffer[0]);
    }

    S field = new S();

    private int example1()
    {
        return (field.buffer[0] = 7);   // OK
    }

    private int example2()
    {
        fixed (int* p = field.buffer)
        {
            return (p[0] = 8);   // OK
        }
    }
}
";

            CreateCompilation(text, options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular7_2).VerifyDiagnostics(
                // (13,30): error CS8320: Feature 'indexing movable fixed buffers' is not available in C# 7.2. Please use language version 7.3 or greater.
                //         System.Console.Write(inst.field.buffer[0]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "inst.field.buffer").WithArguments("indexing movable fixed buffers", "7.3").WithLocation(13, 30),
                // (15,30): error CS8320: Feature 'indexing movable fixed buffers' is not available in C# 7.2. Please use language version 7.3 or greater.
                //         System.Console.Write(inst.field.buffer[0]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "inst.field.buffer").WithArguments("indexing movable fixed buffers", "7.3").WithLocation(15, 30),
                // (22,17): error CS8320: Feature 'indexing movable fixed buffers' is not available in C# 7.2. Please use language version 7.3 or greater.
                //         return (field.buffer[0] = 7);   // OK
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "field.buffer").WithArguments("indexing movable fixed buffers", "7.3").WithLocation(22, 17)
                 );
        }

        [Fact]
        public void CS1666ERR_FixedBufferNotUnsafeErr()
        {
            var text = @"
unsafe struct S
{
    public fixed int buffer[1];
}

class Test
{
    public static void Main()
    {
        var inst = new Test();
        System.Console.Write(inst.example1());
        System.Console.Write(inst.field.buffer[0]);
    }

    S field = new S();

    private int example1()
    {
        return (field.buffer[0] = 7);   // OK
    }
}
";

            CreateCompilation(text, options: TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
                // (13,30): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         System.Console.Write(inst.field.buffer[0]);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "inst.field.buffer").WithLocation(13, 30),
                // (20,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         return (field.buffer[0] = 7);   // OK
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "field.buffer").WithLocation(20, 17)
                 );
        }

        [Fact]
        public void CS1666ERR_FixedBufferNotFixed()
        {
            var text = @"
unsafe struct S
{
    public fixed int buffer[1];
}

unsafe class Test
{
    public static void Main()
    {
        var inst = new Test();
        System.Console.Write(inst.example1());
        System.Console.Write(inst.field.buffer[0]);
        System.Console.Write(inst.example2());
        System.Console.Write(inst.field.buffer[0]);
    }

    S field = new S();

    private int example1()
    {
        return (field.buffer[0] = 7);   // OK
    }

    private int example2()
    {
        fixed (int* p = field.buffer)
        {
            return (p[0] = 8);   // OK
        }
    }
}
";

            var c = CompileAndVerify(text, expectedOutput: "7788", verify: Verification.Fails, options: TestOptions.UnsafeReleaseExe);

            c.VerifyIL("Test.example1()", @"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""S Test.field""
  IL_0006:  ldflda     ""int* S.buffer""
  IL_000b:  ldflda     ""int S.<buffer>e__FixedBuffer.FixedElementField""
  IL_0010:  ldc.i4.7
  IL_0011:  dup
  IL_0012:  stloc.0
  IL_0013:  stind.i4
  IL_0014:  ldloc.0
  IL_0015:  ret
}
");

            c.VerifyIL("Test.example2()", @"
{
  // Code size       25 (0x19)
  .maxstack  3
  .locals init (pinned int& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""S Test.field""
  IL_0006:  ldflda     ""int* S.buffer""
  IL_000b:  ldflda     ""int S.<buffer>e__FixedBuffer.FixedElementField""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  conv.u
  IL_0013:  ldc.i4.8
  IL_0014:  dup
  IL_0015:  stloc.1
  IL_0016:  stind.i4
  IL_0017:  ldloc.1
  IL_0018:  ret
}
");
        }

        [Fact]
        public void CS1669ERR_IllegalVarArgs01()
        {
            var source =
@"class C 
{
    delegate void D(__arglist);    // CS1669
    static void Main()  {}
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,21): error CS1669: __arglist is not valid in this context
                //     delegate void D(__arglist);    // CS1669
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist")
                );
        }

        [Fact]
        public void CS1669ERR_IllegalVarArgs02()
        {
            var source =
@"class C
{
    object this[object index, __arglist]
    {
        get { return null; }
    }
    public static C operator +(C c1, __arglist) { return c1; }
    public static implicit operator int(__arglist) { return 0; }

}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,31): error CS1669: __arglist is not valid in this context
                //     object this[object index, __arglist]
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist"),

                // (7,38): error CS1669: __arglist is not valid in this context
                //     public static C operator +(C c1, __arglist) { return c1; }
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist"),

                // (8,41): error CS1669: __arglist is not valid in this context
                //     public static implicit operator int(__arglist) { return 0; }
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist")
                );
        }

        [WorkItem(863433, "DevDiv/Personal")]
        [Fact]
        public void CS1670ERR_IllegalParams()
        {
            // TODO: extra 1670 (not check for now)
            var test = @"
delegate int MyDelegate(params int[] paramsList);
class Test
{
    public static int Main()
    {
        MyDelegate d = delegate(params int[] paramsList)  // CS1670
        {
            return paramsList[0];
        };  
        return 1;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(test,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_IllegalParams, Line = 7, Column = 33 } });
        }

        [Fact]
        public void CS1673ERR_ThisStructNotInAnonMeth01()
        {
            var text = @"
delegate int MyDelegate();

public struct S
{
    int member;

    public int F(int i)
    {
        member = i;
        MyDelegate d = delegate()
        {
            i = this.member;  // CS1673
            return i;

        };
        return d();
    }
}

class CMain
{
    public static void Main()
    {
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_ThisStructNotInAnonMeth, Line = 13, Column = 17 } });
        }

        [Fact]
        public void CS1673ERR_ThisStructNotInAnonMeth02()
        {
            var text = @"
delegate int MyDelegate();

public struct S
{
    int member;

    public int F(int i)
    {
        member = i;
        MyDelegate d = delegate()
        {
            i = member;  // CS1673
            return i;

        };
        return d();
    }
}

class CMain
{
    public static void Main()
    {
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_ThisStructNotInAnonMeth, Line = 13, Column = 17 } });
        }

        [Fact]
        public void CS1674ERR_NoConvToIDisp()
        {
            var text = @"
class C
{
    public static void Main()
    {
        using (int a = 0) // CS1674
            using (a); //CS1674
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,22): warning CS0642: Possible mistaken empty statement
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";"),
                // (6,16): error CS1674: 'int': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "int a = 0").WithArguments("int"),
                // (7,20): error CS1674: 'int': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "a").WithArguments("int"));
        }

        [Fact]
        public void CS1676ERR_BadParamRef()
        {
            var text = @"
delegate void E(ref int i);
class Errors 
{
   static void Main()
   {
      E e = delegate(out int i) { };   // CS1676
   }
}
";
            var compilation = CreateCompilation(text);
            compilation.VerifyDiagnostics(
                // (7,13): error CS1661: Cannot convert anonymous method to delegate type 'E' because the parameter types do not match the delegate parameter types
                //       E e = delegate(out int i) { };   // CS1676
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "delegate(out int i) { }").WithArguments("anonymous method", "E"),
                // (7,22): error CS1676: Parameter 1 must be declared with the 'ref' keyword
                //       E e = delegate(out int i) { };   // CS1676
                Diagnostic(ErrorCode.ERR_BadParamRef, "i").WithArguments("1", "ref"),
                // (7,13): error CS0177: The out parameter 'i' must be assigned to before control leaves the current method
                //       E e = delegate(out int i) { };   // CS1676
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "delegate(out int i) { }").WithArguments("i")
                );
        }

        [Fact]
        public void CS1677ERR_BadParamExtraRef()
        {
            var text = @"
delegate void D(int i);
class Errors
{
    static void Main()
    {
        D d = delegate(out int i) { };   // CS1677
        D d = delegate(ref int j) { }; // CS1677
    }
}
";
            var compilation = CreateCompilation(text);
            compilation.VerifyDiagnostics(
                // (7,15): error CS1661: Cannot convert anonymous method to delegate type 'D' because the parameter types do not match the delegate parameter types
                //         D d = delegate(out int i) { };   // CS1677
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "delegate(out int i) { }").WithArguments("anonymous method", "D"),
                // (7,24): error CS1677: Parameter 1 should not be declared with the 'out' keyword
                //         D d = delegate(out int i) { };   // CS1677
                Diagnostic(ErrorCode.ERR_BadParamExtraRef, "i").WithArguments("1", "out"),
                // (8,15): error CS1661: Cannot convert anonymous method to delegate type 'D' because the parameter types do not match the delegate parameter types
                //         D d = delegate(ref int j) { }; // CS1677
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "delegate(ref int j) { }").WithArguments("anonymous method", "D"),
                // (8,24): error CS1677: Parameter 1 should not be declared with the 'ref' keyword
                //         D d = delegate(ref int j) { }; // CS1677
                Diagnostic(ErrorCode.ERR_BadParamExtraRef, "j").WithArguments("1", "ref"),
                // (8,11): error CS0128: A local variable named 'd' is already defined in this scope
                //         D d = delegate(ref int j) { }; // CS1677
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "d").WithArguments("d"),
                // (7,15): error CS0177: The out parameter 'i' must be assigned to before control leaves the current method
                //         D d = delegate(out int i) { };   // CS1677
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "delegate(out int i) { }").WithArguments("i")
                );
        }

        [Fact]
        public void CS1678ERR_BadParamType()
        {
            var text = @"
delegate void D(int i);
class Errors
{
    static void Main()
    {
        D d = delegate(string s) { };   // CS1678
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] {
                    new ErrorDescription { Code = (int)ErrorCode.ERR_CantConvAnonMethParams, Line = 7, Column = 15 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_BadParamType, Line = 7, Column = 31 }
                });
        }

        [Fact]
        public void CS1681ERR_GlobalExternAlias()
        {
            var text = @"
extern alias global;

class myClass
{
    static int Main()
    {
        //global::otherClass oc = new global::otherClass();
        return 0;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (2,14): error CS1681: You cannot redefine the global extern alias
                // extern alias global;
                Diagnostic(ErrorCode.ERR_GlobalExternAlias, "global"),
                // (2,14): error CS0430: The extern alias 'global' was not specified in a /reference option
                // extern alias global;
                Diagnostic(ErrorCode.ERR_BadExternAlias, "global").WithArguments("global"),
                // (2,1): info CS8020: Unused extern alias.
                // extern alias global;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias global;")
                );
        }

        [Fact]
        public void CS1686ERR_LocalCantBeFixedAndHoisted()
        {
            var text = @"
class MyClass
{
    public unsafe delegate int* MyDelegate();

    public unsafe int* Test()
    {
        int j = 0;
        MyDelegate d = delegate { return &j; };   // CS1686
        return &j;   // CS1686
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,42): error CS1686: Local 'j' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //         MyDelegate d = delegate { return &j; };   // CS1686
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&j").WithArguments("j"));
        }

        [Fact]
        public void CS1686ERR_LocalCantBeFixedAndHoisted02()
        {
            var text = @"using System;

unsafe struct S
{
    public fixed int buffer[1];
    public int i;
}

unsafe class Test
{
    private void example1()
    {
        S data = new S();
        data.i = data.i + 1;
        Func<S> lambda = () => data;
        fixed (int* p = data.buffer) // fail due to receiver being a local
        {
        }
        int *q = data.buffer; // fail due to lambda capture
    }
}";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (16,25): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                //         fixed (int* p = data.buffer) // fail due to receiver being a local
                Diagnostic(ErrorCode.ERR_FixedNotNeeded, "data.buffer"),
                // (19,18): error CS1686: Local 'data' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //         int *q = data.buffer; // fail due to lambda capture
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "data.buffer").WithArguments("data")
                );
        }

        [WorkItem(580537, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/580537")]
        [Fact]
        public void CS1686ERR_LocalCantBeFixedAndHoisted03()
        {
            var text = @"unsafe
public struct Test
{
    private delegate int D();
    public fixed int i[1];
    public void example()
    {
        Test t = this;
        t.i[0] = 5;
        D d = delegate {
            var x = t;
            return 0;
        };
    }
}";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,9): error CS1686: Local 't' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //         t.i[0] = 5;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "t.i").WithArguments("t")
                );
        }

        [Fact]
        public void CS1688ERR_CantConvAnonMethNoParams()
        {
            var text = @"
using System;
delegate void OutParam(out int i);
class ErrorCS1676
{
    static void Main()
    {
        OutParam o;
        o = delegate  // CS1688
        {
            Console.WriteLine("");
        };
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,31): error CS1010: Newline in constant
                //             Console.WriteLine(");
                Diagnostic(ErrorCode.ERR_NewlineInConst, "").WithLocation(11, 31),
                // (11,34): error CS1026: ) expected
                //             Console.WriteLine(");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(11, 34),
                // (11,34): error CS1002: ; expected
                //             Console.WriteLine(");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(11, 34),
                // (9,13): error CS1688: Cannot convert anonymous method block without a parameter list to delegate type 'OutParam' because it has one or more out parameters
                //         o = delegate  // CS1688
                Diagnostic(ErrorCode.ERR_CantConvAnonMethNoParams, @"delegate  // CS1688
        {
            Console.WriteLine("");
        }").WithArguments("OutParam").WithLocation(9, 13)
                );
        }

        [Fact]
        public void CS1708ERR_FixedNeedsLvalue()
        {
            var text = @"
unsafe public struct S
{
    public fixed char name[10];
}

public unsafe class C
{
    public S UnsafeMethod()
    {
        S myS = new S();
        return myS;
    }

    static void Main()
    {
        C myC = new C();
        myC.UnsafeMethod().name[3] = 'a';  // CS1708
        C._s1.name[3] = 'a';  // CS1648
        myC._s2.name[3] = 'a';  // CS1648
    }

    static readonly S _s1;
    public readonly S _s2;
}";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (18,9): error CS1708: Fixed size buffers can only be accessed through locals or fields
                //         myC.UnsafeMethod().name[3] = 'a';  // CS1708
                Diagnostic(ErrorCode.ERR_FixedNeedsLvalue, "myC.UnsafeMethod().name").WithLocation(18, 9),
                // (19,9): error CS1650: Fields of static readonly field 'C._s1' cannot be assigned to (except in a static constructor or a variable initializer)
                //         C._s1.name[3] = 'a';  // CS1648
                Diagnostic(ErrorCode.ERR_AssgReadonlyStatic2, "C._s1.name[3]").WithArguments("C._s1").WithLocation(19, 9),
                // (20,9): error CS1648: Members of readonly field 'C._s2' cannot be modified (except in a constructor, an init-only member or a variable initializer)
                //         myC._s2.name[3] = 'a';  // CS1648
                Diagnostic(ErrorCode.ERR_AssgReadonly2, "myC._s2.name[3]").WithArguments("C._s2").WithLocation(20, 9)
                );
        }

        [Fact, WorkItem(543995, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543995"), WorkItem(544258, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544258")]
        public void CS1728ERR_DelegateOnNullable()
        {
            var text = @"
using System;
class Test
{
   static void Main()
   {
        int? x = null;

        Func<string> f1 = x.ToString;               // no error
        Func<int> f2 = x.GetHashCode;               // no error
        Func<object, bool> f3 = x.Equals;           // no error
        Func<Type> f4 = x.GetType;                  // no error

        Func<int> x1 = x.GetValueOrDefault;         // 1728
        Func<int, int> x2 = x.GetValueOrDefault;    // 1728
   }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (14,24): error CS1728: Cannot bind delegate to 'int?.GetValueOrDefault()' because it is a member of 'System.Nullable<T>'
                //         Func<int> x1 = x.GetValueOrDefault;         // 1728
                Diagnostic(ErrorCode.ERR_DelegateOnNullable, "x.GetValueOrDefault").WithArguments("int?.GetValueOrDefault()"),
                // (15,29): error CS1728: Cannot bind delegate to 'int?.GetValueOrDefault(int)' because it is a member of 'System.Nullable<T>'
                //         Func<int, int> x2 = x.GetValueOrDefault;    // 1728
                Diagnostic(ErrorCode.ERR_DelegateOnNullable, "x.GetValueOrDefault").WithArguments("int?.GetValueOrDefault(int)")
                );
        }

        [Fact, WorkItem(999399, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/999399")]
        public void CS1729ERR_BadCtorArgCount()
        {
            var text = @"
class Test
{
    static int Main()
    {
        double d = new double(4.5);  // was CS0143 (Dev10)
        Test test1 = new Test(2); // CS1729
        Test test2 = new Test();
        Parent exampleParent1 = new Parent(10); // CS1729
        Parent exampleParent2 = new Parent(10, 1);
        if (test1 == test2 & exampleParent1 == exampleParent2) {}
        return 1;
    }
}

public class Parent
{
    public Parent(int i, int j) { }
}

public class Child : Parent { } // CS1729

public class Child2 : Parent
{
    public Child2(int k)
        : base(k, 0)
    {
    }
}";
            var compilation = CreateCompilation(text);

            DiagnosticDescription[] expected = {
                // (21,14): error CS7036: There is no argument given that corresponds to the required formal parameter 'i' of 'Parent.Parent(int, int)'
                // public class Child : Parent { } // CS1729
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Child").WithArguments("i", "Parent.Parent(int, int)").WithLocation(21, 14),
                // (6,24): error CS1729: 'double' does not contain a constructor that takes 1 arguments
                //         double d = new double(4.5);  // was CS0143 (Dev10)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "double").WithArguments("double", "1").WithLocation(6, 24),
                // (7,26): error CS1729: 'Test' does not contain a constructor that takes 1 arguments
                //         Test test1 = new Test(2); // CS1729
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test").WithArguments("Test", "1").WithLocation(7, 26),
                // (9,37): error CS7036: There is no argument given that corresponds to the required formal parameter 'j' of 'Parent.Parent(int, int)'
                //         Parent exampleParent1 = new Parent(10); // CS1729
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Parent").WithArguments("j", "Parent.Parent(int, int)").WithLocation(9, 37)
            };

            compilation.VerifyDiagnostics(expected);

            compilation.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, compilation.SyntaxTrees.Single(), filterSpanWithinTree: null, includeEarlierStages: true).Verify(expected);
        }

        [WorkItem(539631, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539631")]
        [Fact]
        public void CS1729ERR_BadCtorArgCount02()
        {
            var text = @"
class MyClass
{
    int intI = 1;
    MyClass()
    {
        intI = 2;
    }

    //this constructor initializer
    MyClass(int intJ) : this(3, 4) // CS1729
    {
        intI = intI * intJ;
    }
}

class MyBase
{
    public int intI = 1;
    protected MyBase()
    {
        intI = 2;
    }
    protected MyBase(int intJ)
    {
        intI = intJ;
    }
}

class MyDerived : MyBase
{
    MyDerived() : base(3, 4) // CS1729
    {
        intI = intI * 2;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadCtorArgCount, Line = 11, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadCtorArgCount, Line = 32, Column = 19 });
        }

        [Fact]
        public void CS1737ERR_DefaultValueBeforeRequiredValue()
        {
            var text = @"
class C
{
    public void Goo(string s = null, int x)
    {
    }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_DefaultValueBeforeRequiredValue, Line = 4, Column = 43 } } //sic: error on close paren
                );
        }

        [WorkItem(539007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539007")]
        [Fact]
        public void DevDiv4792_OptionalBeforeParams()
        {
            var text = @"
class C
{
    public void Goo(string s = null, params int[] ints)
    {
    }
}
";
            //no errors
            var comp = CreateCompilation(text);
            Assert.False(comp.GetDiagnostics().Any());
        }

        [WorkItem(527351, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527351")]
        [Fact]
        public void CS1738ERR_NamedArgumentSpecificationBeforeFixedArgument()
        {
            var text = @"
public class C
{
    public static int Main()
    {
        Test(age: 5,"""");
        return 0;
    }
    public static void Test(int age, string Name)
    { }
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular6);
            comp.VerifyDiagnostics(
                // (6,21): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                //         Test(age: 5,"");
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, @"""""").WithArguments("7.2").WithLocation(6, 21)
                );
        }

        [Fact]
        public void CS1739ERR_BadNamedArgument()
        {
            var text = @"
public class C
{
    public static int Main()
        {
            Test(5,Nam:null);
        return 0;
        }
    public static void Test(int age , string Name)
    { }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = 1739, Line = 6, Column = 20 } });
        }

        [Fact, WorkItem(866112, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866112")]
        public void CS1739ERR_BadNamedArgument_1()
        {
            var text = @"
public class C
{
    public static void Main()
    {
        Test(1, 2, Name:3);
    }

    public static void Test(params int [] array)
    { }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = 1739, Line = 6, Column = 20 } });
        }

        [Fact]
        public void CS1740ERR_DuplicateNamedArgument()
        {
            var text = @"
public class C
{
    public static int Main()
    {
        Test(age: 5, Name: ""5"", Name: """");
        return 0;
    }
    public static void Test(int age, string Name)
    {
    }
}";
            var compilation = CSharpTestBase.CreateCompilation(text);
            compilation.VerifyDiagnostics(
                // (6,33): error CS1740: Named argument 'Name' cannot be specified multiple times
                //         Test(age: 5, Name: "5", Name: "");
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "Name").WithArguments("Name").WithLocation(6, 33));
        }

        [Fact]
        public void CS1742ERR_NamedArgumentForArray()
        {
            var text = @"
public class B
{
    static int Main()
    {
        int[] arr = { };
        int s = arr[arr: 1];
        s = s + 1;
        return 1;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = 1742, Line = 7, Column = 17 } });
        }

        [Fact]
        public void CS1744ERR_NamedArgumentUsedInPositional()
        {
            var text = @"
public class C
{
    public static int Main()
        {
            Test(5, age: 3);
        return 0;
        }
    public static void Test(int age , string Name)
    { }
}";
            // CS1744: Named argument 'q' specifies a parameter for which a positional argument has already been given.
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = 1744, Line = 6, Column = 21 } });
        }

        [Fact]
        public void CS1744ERR_NamedArgumentUsedInPositional2()
        {
            // Unfortunately we allow "void M(params int[] x)" to be called in the expanded
            // form as "M(x : 123);". However, we still do not allow "M(123, x:456);".

            var text = @"
public class C
{
    public static void Main()
    {
        Test(5, x: 3);
    }
    public static void Test(params int[] x) { }
}";
            // CS1744: Named argument 'x' specifies a parameter for which a positional argument has already been given.
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = 1744, Line = 6, Column = 17 } });
        }

        [Fact]
        public void CS1744ERR_NamedArgumentUsedInPositional3()
        {
            var text = @"
public class C
{
    public static void Main()
    {
        Test(5, x : 6);
    }
    public static void Test(int x, int y = 10, params int[] z) { }
}";
            // CS1744: Named argument 'x' specifies a parameter for which a positional argument has already been given.
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = 1744, Line = 6, Column = 17 } });
        }

        [Fact]
        public void CS1744ERR_NamedArgumentUsedInPositional4()
        {
            var text = @"
public class C
{
    public static void Main()
    {
        Test(5, 6, 7, 8, 9, 10, z : 6);
    }
    public static void Test(int x, int y = 10, params int[] z) { }
}";
            // CS1744: Named argument 'z' specifies a parameter for which a positional argument has already been given.
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = 1744, Line = 6, Column = 33 } });
        }

        [Fact]
        public void CS1746ERR_BadNamedArgumentForDelegateInvoke()
        {
            var text = @"
public class C
{
    delegate int MyDg(int age);
    public static int Main()
        {
            MyDg dg = new MyDg(Test);
            int S = dg(Ne: 3);
        return 0;
        }
    public static int Test(int age)
    { return 1; }
}";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = 1746, Line = 8, Column = 24 } });
        }

        //        [Fact()]
        //        public void CS1752ERR_FixedNeedsLvalue()
        //        {
        //            var text = @"
        //";
        //            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
        //                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_FixedNeedsLvalue, Line = 20, Column = 9 } }
        //                );
        //        }

        // CS1912 --> ObjectAndCollectionInitializerTests.cs
        // CS1913 --> ObjectAndCollectionInitializerTests.cs
        // CS1914 --> ObjectAndCollectionInitializerTests.cs
        // CS1917 --> ObjectAndCollectionInitializerTests.cs
        // CS1918 --> ObjectAndCollectionInitializerTests.cs
        // CS1920 --> ObjectAndCollectionInitializerTests.cs
        // CS1921 --> ObjectAndCollectionInitializerTests.cs
        // CS1922 --> ObjectAndCollectionInitializerTests.cs

        [Fact]
        public void CS1919ERR_UnsafeTypeInObjectCreation()
        {
            var text = @"
unsafe public class C
{
    public static int Main()
    {
        var col1 = new int*(); // CS1919
        var col2 = new char*(); // CS1919
        return 1;
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,20): error CS1919: Unsafe type 'int*' cannot be used in object creation
                Diagnostic(ErrorCode.ERR_UnsafeTypeInObjectCreation, "new int*()").WithArguments("int*"),
                // (7,20): error CS1919: Unsafe type 'char*' cannot be used in object creation
                Diagnostic(ErrorCode.ERR_UnsafeTypeInObjectCreation, "new char*()").WithArguments("char*"));
        }

        [Fact]
        public void CS1928ERR_BadExtensionArgTypes()
        {
            var text =
@"class C
{
    static void M(float f)
    {
        f.F();
    }
}
static class S
{
    internal static void F(this double d) { }
}";
            var compilation = CreateCompilationWithMscorlib40(text, references: new[] { SystemCoreRef });
            // Previously ERR_BadExtensionArgTypes.
            compilation.VerifyDiagnostics(
                // (5,9): error CS1929: 'float' does not contain a definition for 'F' and the best extension method overload 'S.F(double)' requires a receiver of type 'double'
                //         f.F();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "f").WithArguments("float", "F", "S.F(double)", "double").WithLocation(5, 9));
        }

        [Fact]
        public void CS1929ERR_BadInstanceArgType()
        {
            var source = @"class A { }
class B : A
{
    static void M(A a)
    {
        a.E();
    }
}
static class S
{
    internal static void E(this B b) { }
}";
            var compilation = CreateCompilationWithMscorlib40(source, references: new[] { SystemCoreRef });
            compilation.VerifyDiagnostics(
                // (6,9): error CS1929: 'A' does not contain a definition for 'E' and the best extension method overload 'S.E(B)' requires a receiver of type 'B'
                //         a.E();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "a").WithArguments("A", "E", "S.E(B)", "B").WithLocation(6, 9)
                );
        }

        [Fact]
        public void CS1930ERR_QueryDuplicateRangeVariable()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;
class Program
{
    static void Main()
    {
        int[] nums = { 0, 1, 2, 3, 4, 5 };
        var query = from num in nums
                    let num = 3 // CS1930
                    select num; 
    }
}
").VerifyDiagnostics(
                // (10,25): error CS1930: The range variable 'num' has already been declared
                Diagnostic(ErrorCode.ERR_QueryDuplicateRangeVariable, "num").WithArguments("num"));
        }

        [Fact]
        public void CS1931ERR_QueryRangeVariableOverrides01()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;

class Test
{
    static void Main()
    {
        int x = 1;
        var y = from x in Enumerable.Range(1, 100) // CS1931
                select x + 1;
    }
}
").VerifyDiagnostics(
                // (9,22): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
                //         var y = from x in Enumerable.Range(1, 100) // CS1931
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x"),
                // (8,13): warning CS0219: The variable 'x' is assigned but its value is never used
                //         int x = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x")
            );
        }

        [Fact]
        public void CS1932ERR_QueryRangeVariableAssignedBadValue()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;
class Test
{
    static void Main()
    {
        var x = from i in Enumerable.Range(1, 100)
                let k = null
                select i;
    }
}
").VerifyDiagnostics(
                // (8,21): error CS1932: Cannot assign <null> to a range variable
                //                 let k = null
                Diagnostic(ErrorCode.ERR_QueryRangeVariableAssignedBadValue, "k = null").WithArguments("<null>")
             );
        }

        [Fact]
        public void CS1932ERR_QueryRangeVariableAssignedBadValue02()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;
class Test
{
    static void Main()
    {
        var x = from i in Enumerable.Range(1, 100)
                let k = ()=>3
                select i;
    }
}
").VerifyDiagnostics(
                // (8,21): error CS1932: Cannot assign lambda expression to a range variable
                //                 let k = ()=>3
                Diagnostic(ErrorCode.ERR_QueryRangeVariableAssignedBadValue, "k = ()=>3").WithArguments("lambda expression")
             );
        }

        [Fact]
        public void CS1932ERR_QueryRangeVariableAssignedBadValue03()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;
class Test
{
    static void Main()
    {
        var x = from i in Enumerable.Range(1, 100)
                let k = Main
                select i;
    }
}
").VerifyDiagnostics(
                // (8,21): error CS1932: Cannot assign method group to a range variable
                //                 let k = Main
                Diagnostic(ErrorCode.ERR_QueryRangeVariableAssignedBadValue, "k = Main").WithArguments("method group")
             );
        }

        [Fact]
        public void CS1932ERR_QueryRangeVariableAssignedBadValue04()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;
class Test
{
    static void Main()
    {
        var x = from i in Enumerable.Range(1, 100)
                let k = M()
                select i;
    }
    static void M() {}
}
").VerifyDiagnostics(
                // (8,21): error CS1932: Cannot assign void to a range variable
                //                 let k = M()
                Diagnostic(ErrorCode.ERR_QueryRangeVariableAssignedBadValue, "k = M()").WithArguments("void")
             );
        }

        [WorkItem(528756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528756")]
        [Fact()]
        public void CS1933ERR_QueryNotAllowed()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;
using System.Collections;

class P
{
    const IEnumerable e = from x in new int[] { 1, 2, 3 } select x; // CS1933
    static int Main()
    {
        return 1;
    }
}
").VerifyDiagnostics(
    // EDMAURER now giving the more generic message CS0133
    // (7,27): error CS1933: Expression cannot contain query expressions
    // from
    //Diagnostic(ErrorCode.ERR_QueryNotAllowed, "from").WithArguments());

    // (7,27): error CS0133: The expression being assigned to 'P.e' must be constant
    //     const IEnumerable e = from x in new int[] { 1, 2, 3 } select x; // CS1933
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "from x in new int[] { 1, 2, 3 } select x").WithArguments("P.e")
            );
        }

        [Fact]
        public void CS1934ERR_QueryNoProviderCastable()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;
using System.Collections;
static class Test
{
    public static void Main()
    {
        var list = new ArrayList();
        var q = from x in list // CS1934
                select x + 1;
    }
}
").VerifyDiagnostics(
             // (9,27): error CS1934: Could not find an implementation of the query pattern for source type 'System.Collections.ArrayList'.  'Select' not found.  Consider explicitly specifying the type of the range variable 'x'.
             // list
             Diagnostic(ErrorCode.ERR_QueryNoProviderCastable, "list").WithArguments("System.Collections.ArrayList", "Select", "x"));
        }

        [Fact]
        public void CS1935ERR_QueryNoProviderStandard()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Collections.Generic;
class Test
{
    static int Main()
    {
        int[] nums = { 0, 1, 2, 3, 4, 5 };
        IEnumerable<int> e = from n in nums
                             where n > 3
                             select n;
        return 0;
    }
}
").VerifyDiagnostics(
             // (8,40): error CS1935: Could not find an implementation of the query pattern for source type 'int[]'.  'Where' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?
             // nums
             Diagnostic(ErrorCode.ERR_QueryNoProviderStandard, "nums").WithArguments("int[]", "Where"));
        }

        [Fact]
        public void CS1936ERR_QueryNoProvider()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Collections;
using System.Linq;
class Test
{
    static int Main()
    {
        object obj = null;
        IEnumerable e = from x in obj // CS1936
                        select x;
        return 0;
    }
}
").VerifyDiagnostics(
             // (10,35): error CS1936: Could not find an implementation of the query pattern for source type 'object'.  'Select' not found.
             // obj
             Diagnostic(ErrorCode.ERR_QueryNoProvider, "obj").WithArguments("object", "Select"));
        }

        [Fact]
        public void CS1936ERR_QueryNoProvider01()
        {
            var program = @"
class X
{
    internal X Cast<T>() { return this; }
}
class Program
{
    static void Main()
    {
        var xx = new X();
        var q3 = from int x in xx select x;
    }
}";
            var comp = CreateCompilation(program);
            comp.VerifyDiagnostics(
                // (11,32): error CS1936: Could not find an implementation of the query pattern for source type 'X'.  'Select' not found.
                //         var q3 = from int x in xx select x;
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "xx").WithArguments("X", "Select")
                );
        }

        [Fact]
        public void CS1937ERR_QueryOuterKey()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;
class Test
{
    static void Main()
    {
        int[] sourceA = { 1, 2, 3, 4, 5 };
        int[] sourceB = { 3, 4, 5, 6, 7 };

        var query = from a in sourceA
                    join b in sourceB on b equals 5 // CS1937
                    select a + b;
    }
}
").VerifyDiagnostics(
                // (11,42): error CS1937: The name 'b' is not in scope on the left side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
                //                     join b in sourceB on b equals 5 // CS1937
                Diagnostic(ErrorCode.ERR_QueryOuterKey, "b").WithArguments("b")
            );
        }

        [Fact]
        public void CS1938ERR_QueryInnerKey()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;
class Test
{
    static void Main()
    {
        int[] sourceA = { 1, 2, 3, 4, 5 };
        int[] sourceB = { 3, 4, 5, 6, 7 };

        var query = from a in sourceA
                    join b in sourceB on 5 equals a // CS1938
                    select a + b;
    }
}
").VerifyDiagnostics(
                // (11,51): error CS1938: The name 'a' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
                //                     join b in sourceB on 5 equals a // CS1938
                Diagnostic(ErrorCode.ERR_QueryInnerKey, "a").WithArguments("a")
            );
        }

        [Fact]
        public void CS1939ERR_QueryOutRefRangeVariable()
        {
            var text = @"
using System.Linq;
class Test
{
    public static int F(ref int i) { return i; }
    public static void Main()
    {
        var list = new int[] { 0, 1, 2, 3, 4, 5 };
        var q = from x in list
                let k = x
                select Test.F(ref x); // CS1939
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (13,35): error CS1939: Cannot pass the range variable 'x' as an out or ref parameter
                //                 select Test.F(ref x); // CS1939
                Diagnostic(ErrorCode.ERR_QueryOutRefRangeVariable, "x").WithArguments("x"));
        }

        [Fact]
        public void CS1940ERR_QueryMultipleProviders()
        {
            var text =
@"using System; 
class Test
{
    public delegate int Dele(int x);
    int num = 0;
    public int Select(Func<int, int> d)
    {
        return d(this.num);
    }
    public int Select(Dele d) 
    {
        return d(this.num) + 1;
    }
    public static void Main()
    {
        var q = from x in new Test()
        select x; // CS1940
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (18,17): error CS1940: Multiple implementations of the query pattern were found for source type 'Test'.  Ambiguous call to 'Select'.
                // select
                Diagnostic(ErrorCode.ERR_QueryMultipleProviders, "select x").WithArguments("Test", "Select")
                );
        }

        [Fact]
        public void CS1941ERR_QueryTypeInferenceFailedMulti()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Collections;
using System.Linq;
class Test
{
    static int Main()
    {
        var nums = new int[] { 1, 2, 3, 4, 5, 6 };
        var words = new string[] { ""lake"", ""mountain"", ""sky"" };
        IEnumerable e = from n in nums
                        join w in words on n equals w // CS1941
                        select w;
        return 0;
    }
}
").VerifyDiagnostics(
                // (11,25): error CS1941: The type of one of the expressions in the join clause is incorrect.  Type inference failed in the call to 'Join'.
                // join
                Diagnostic(ErrorCode.ERR_QueryTypeInferenceFailedMulti, "join").WithArguments("join", "Join"));
        }

        [Fact]
        public void CS1942ERR_QueryTypeInferenceFailed()
        {
            var text = @"
using System;
class Q
{
    public Q Select<T,U>(Func<int, int> func) { return this; }
}
class Program
{
    static void Main(string[] args)
    {
        var x = from i in new Q()
                select i; //CS1942
    }
}
";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(text);
            comp.VerifyDiagnostics(
                // (12,17): error CS1942: The type of the expression in the select clause is incorrect.  Type inference failed in the call to 'Select'.
                //                 select i; //CS1942
                Diagnostic(ErrorCode.ERR_QueryTypeInferenceFailed, "select").WithArguments("select", "Select")
                );
        }

        [Fact]
        public void CS1943ERR_QueryTypeInferenceFailedSelectMany()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;
class Test
{
    class TestClass
    { }
    static void Main()
    {
        int[] nums = { 0, 1, 2, 3, 4, 5 };
        TestClass tc = new TestClass();
        
        var x = from n in nums
                from s in tc // CS1943
                select n + s;
    }
}
").VerifyDiagnostics(
                // (13,27): error CS1943: An expression of type 'Test.TestClass' is not allowed in a subsequent from clause in a query expression with source type 'int[]'.  Type inference failed in the call to 'SelectMany'.
                // tc
                Diagnostic(ErrorCode.ERR_QueryTypeInferenceFailedSelectMany, "tc").WithArguments("Test.TestClass", "int[]", "SelectMany"));
        }

        [Fact]
        public void CS1943ERR_QueryTypeInferenceFailedSelectMany02()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System;
class Test
{
    class F1
    {
        public F1 SelectMany<T, U>(Func<int, F1> func1, Func<int, int> func2) { return this; }
    }
    static void Main()
    {
        F1 f1 = new F1();
        var x =
            from f in f1
            from g in 3
            select f + g;
    }
}
").VerifyDiagnostics(
                    // (14,23): error CS1943: An expression of type 'int' is not allowed in a subsequent from clause in a query expression with source type 'Test.F1'.  Type inference failed in the call to 'SelectMany'.
                    //             from g in 3
                    Diagnostic(ErrorCode.ERR_QueryTypeInferenceFailedSelectMany, "3").WithArguments("int", "Test.F1", "SelectMany").WithLocation(14, 23)
            );
        }

        [Fact, WorkItem(546510, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546510")]
        public void CS1944ERR_ExpressionTreeContainsPointerOp()
        {
            var text = @"
using System;
using System.Linq.Expressions;

unsafe class Test
{
    public delegate int* D(int i);
    static void Main()
    {
        Expression<D> tree = x => &x; // CS1944
        Expression<Func<int, int*[]>> testExpr = x => new int*[] { &x };
    }
}
";
            //Assert.Equal("", text);
            CreateCompilationWithMscorlib40AndSystemCore(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (10,35): error CS1944: An expression tree may not contain an unsafe pointer operation
                //         Expression<D> tree = x => &x; // CS1944
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsPointerOp, "&x"),
                // (11,68): error CS1944: An expression tree may not contain an unsafe pointer operation
                //         Expression<Func<int, int*[]>> testExpr = x => new int*[] { &x };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsPointerOp, "&x")
                );
        }

        [Fact]
        public void CS1945ERR_ExpressionTreeContainsAnonymousMethod()
        {
            var text = @"
using System;
using System.Linq.Expressions;

public delegate void D();
class Test
{
    static void Main()
    {
        Expression<Func<int, Func<int, bool>>> tree = (x => delegate(int i) { return true; }); // CS1945
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (10,61): error CS1945: An expression tree may not contain an anonymous method expression
                //         Expression<Func<int, Func<int, bool>>> tree = (x => delegate(int i) { return true; }); // CS1945
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAnonymousMethod, "delegate(int i) { return true; }")
                );
        }

        [Fact]
        public void CS1946ERR_AnonymousMethodToExpressionTree()
        {
            var text = @"
using System.Linq.Expressions;

public delegate void D();

class Test
{
    static void Main()
    {
        Expression<D> tree = delegate() { }; //CS1946
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (10,30): error CS1946: An anonymous method expression cannot be converted to an expression tree
                //         Expression<D> tree = delegate() { }; //CS1946
                Diagnostic(ErrorCode.ERR_AnonymousMethodToExpressionTree, "delegate() { }")
                );
        }

        [Fact]
        public void CS1947ERR_QueryRangeVariableReadOnly()
        {
            var program = @"
using System.Linq;
class Test
{
    static void Main()
    {
        int[] array = new int[] { 1, 2, 3, 4, 5 };
        var x = from i in array
                let k = i
                select i = 5; // CS1947
        x.ToList();
    }
}
";
            CreateCompilation(program).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_QueryRangeVariableReadOnly, "i").WithArguments("i"));
        }

        [Fact]
        public void CS1948ERR_QueryRangeVariableSameAsTypeParam()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;
class Test
{
    public void TestMethod<T>(T t)
    {
        var x = from T in Enumerable.Range(1, 100) // CS1948
                select T;
    }
    public static void Main()
    {
    }
}
").VerifyDiagnostics(
                // (8,17): error CS1948: The range variable 'T' cannot have the same name as a method type parameter
                // T
                Diagnostic(ErrorCode.ERR_QueryRangeVariableSameAsTypeParam, "T").WithArguments("T"));
        }

        [Fact]
        public void CS1949ERR_TypeVarNotFoundRangeVariable()
        {
            var text =
@"using System.Linq;
class Test
{
    static void Main()
    {
        var x = from var i in Enumerable.Range(1, 100) // CS1949
        select i;
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (7,22): error CS1949: The contextual keyword 'var' cannot be used in a range variable declaration
                //         var x = from var i in Enumerable.Range(1, 100) // CS1949
                Diagnostic(ErrorCode.ERR_TypeVarNotFoundRangeVariable, "var")
                );
        }

        // CS1950 --> ObjectAndCollectionInitializerTests.cs

        [Fact]
        public void CS1951ERR_ByRefParameterInExpressionTree()
        {
            var text = @"
public delegate int TestDelegate(ref int i);
class Test
{
    static void Main()
    {
        System.Linq.Expressions.Expression<TestDelegate> tree1 = (ref int x) => x; // CS1951
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (7,75): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
                //         System.Linq.Expressions.Expression<TestDelegate> tree1 = (ref int x) => x; // CS1951
                Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "x").WithLocation(7, 75)
                );
        }

        [Fact]
        public void CS1951ERR_InParameterInExpressionTree()
        {
            var text = @"
public delegate int TestDelegate(in int i);
class Test
{
    static void Main()
    {
        System.Linq.Expressions.Expression<TestDelegate> tree1 = (in int x) => x; // CS1951
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (7,74): error CS1951: An expression tree lambda may not contain a ref, in or out parameter
                //         System.Linq.Expressions.Expression<TestDelegate> tree1 = (in int x) => x; // CS1951
                Diagnostic(ErrorCode.ERR_ByRefParameterInExpressionTree, "x").WithLocation(7, 74)
                );
        }

        [Fact]
        public void CS1952ERR_VarArgsInExpressionTree()
        {
            var text = @"
using System;
using System.Linq.Expressions;

class Test
{
    public static int M(__arglist)
    {
        return 1;
    }

    static int Main()
    {
        Expression<Func<int, int>> f = x => Test.M(__arglist(x)); // CS1952
        return 1;
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (14,52): error CS1952: An expression tree lambda may not contain a method with variable arguments
                //         Expression<Func<int, int>> f = x => Test.M(__arglist(x)); // CS1952
                Diagnostic(ErrorCode.ERR_VarArgsInExpressionTree, "__arglist(x)")
                );
        }

        [WorkItem(864605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864605")]
        [Fact]
        public void CS1953ERR_MemGroupInExpressionTree()
        {
            var text = @"
using System;
using System.Linq.Expressions;
class CS1953
{
    public static void Main()
    {
        double num = 10;
        Expression<Func<bool>> testExpr =
              () => num.GetType is int; // CS0837 
    }
}";
            // Used to be CS1953, but now a method group in an is expression is illegal anyway.
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (10,21): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //               () => num.GetType is int; // CS1953 
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "num.GetType is int").WithLocation(10, 21));
        }

        // CS1954 --> ObjectAndCollectionInitializerTests.cs

        [Fact]
        public void CS1955ERR_NonInvocableMemberCalled()
        {
            var text = @"
namespace CompilerError1955
{
    class ClassA
    {
        public int x = 100;
        public int X
        {
            get { return x; }
            set { x = value; }
        }
    }

    class Test
    {
        static void Main()
        {
            ClassA a = new ClassA();
            System.Console.WriteLine(a.x()); // CS1955
            System.Console.WriteLine(a.X()); // CS1955
        }
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.ERR_NonInvocableMemberCalled, Line = 19, Column = 40 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NonInvocableMemberCalled, Line = 20, Column = 40 }});
        }

        // CS1958 --> ObjectAndCollectionInitializerTests.cs

        [Fact]
        public void CS1959ERR_InvalidConstantDeclarationType()
        {
            var text = @"
class Program
    {
        static void Test<T>() where T : class
        {
            const T x = null; // CS1959
        }
    }
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (6,25): error CS1959: 'x' is of type 'T'. The type specified in a constant declaration must be sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, decimal, bool, string, an enum-type, or a reference-type.
                //             const T x = null; // CS1959
                Diagnostic(ErrorCode.ERR_InvalidConstantDeclarationType, "null").WithArguments("x", "T")
                );
        }

        /// <summary>
        /// Test the different contexts in which CS1961 can be seen.
        /// </summary>
        [Fact]
        public void CS1961ERR_UnexpectedVariance_Contexts()
        {
            var text = @"
interface IContexts<in TIn, out TOut, TInv>
{
    #region In
    TIn Property1In { set; }
    TIn Property2In { get; } //CS1961 on ""TIn""
    TIn Property3In { get; set; } //CS1961 on ""TIn""

    int this[TIn arg, char filler, char indexer1In] { get; }
    TIn this[int arg, char[] filler, char indexer2In] { get; } //CS1961 on ""TIn""
    int this[TIn arg, bool filler, char indexer3In] { set; }
    TIn this[int arg, bool[] filler, char indexer4In] { set; }
    int this[TIn arg, long filler, char indexer5In] { get; set; }
    TIn this[int arg, long[] filler, char indexer6In] { get; set; } //CS1961 on ""TIn""

    int Method1In(TIn p);
    TIn Method2In(); //CS1961 on ""TIn""
    int Method3In(out TIn p); //CS1961 on ""TIn""
    int Method4In(ref TIn p); //CS1961 on ""TIn""

    event DOut<TIn> Event1In;
    #endregion In

    #region Out
    TOut Property1Out { set; } //CS1961 on ""TOut""
    TOut Property2Out { get; }
    TOut Property3Out { get; set; } //CS1961 on ""TOut""

    int this[TOut arg, char filler, bool indexer1Out] { get; } //CS1961 on ""TOut""
    TOut this[int arg, char[] filler, bool indexer2Out] { get; }
    int this[TOut arg, bool filler, bool indexer3Out] { set; } //CS1961 on ""TOut""
    TOut this[int arg, bool[] filler, bool indexer4Out] { set; } //CS1961 on ""TOut""
    int this[TOut arg, long filler, bool indexer5Out] { get; set; } //CS1961 on ""TOut""
    TOut this[int arg, long[] filler, bool indexer6Out] { get; set; } //CS1961 on ""TOut""

    long Method1Out(TOut p); //CS1961 on ""TOut""
    TOut Method2Out();
    long Method3Out(out TOut p); //CS1961 on ""TOut"" (sic: out params have to be input-safe)
    long Method4Out(ref TOut p); //CS1961 on ""TOut""

    event DOut<TOut> Event1Out; //CS1961 on ""TOut""
    #endregion Out

    #region Inv
    TInv Property1Inv { set; }
    TInv Property2Inv { get; }
    TInv Property3Inv { get; set; }

    int this[TInv arg, char filler, long indexer1Inv] { get; }
    TInv this[int arg, char[] filler, long indexer2Inv] { get; }
    int this[TInv arg, bool filler, long indexer3Inv] { set; }
    TInv this[int arg, bool[] filler, long indexer4Inv] { set; }
    int this[TInv arg, long filler, long indexer5Inv] { get; set; }
    TInv this[int arg, long[] filler, long indexer6Inv] { get; set; }

    long Method1Inv(TInv p);
    TInv Method2Inv();
    long Method3Inv(out TInv p);
    long Method4Inv(ref TInv p);

    event DOut<TInv> Event1Inv;
    #endregion Inv
}

delegate void DOut<out T>(); //for event types - should preserve the variance of the type arg
";

            CreateCompilation(text).VerifyDiagnostics(
                // (6,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IContexts<TIn, TOut, TInv>.Property2In'. 'TIn' is contravariant.
                //     TIn Property2In { get; } //CS1961 on "TIn"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("IContexts<TIn, TOut, TInv>.Property2In", "TIn", "contravariant", "covariantly").WithLocation(6, 5),
                // (7,5): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IContexts<TIn, TOut, TInv>.Property3In'. 'TIn' is contravariant.
                //     TIn Property3In { get; set; } //CS1961 on "TIn"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("IContexts<TIn, TOut, TInv>.Property3In", "TIn", "contravariant", "invariantly").WithLocation(7, 5),
                // (10,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IContexts<TIn, TOut, TInv>.this[int, char[], char]'. 'TIn' is contravariant.
                //     TIn this[int arg, char[] filler, char indexer2In] { get; } //CS1961 on "TIn"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("IContexts<TIn, TOut, TInv>.this[int, char[], char]", "TIn", "contravariant", "covariantly").WithLocation(10, 5),
                // (14,5): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IContexts<TIn, TOut, TInv>.this[int, long[], char]'. 'TIn' is contravariant.
                //     TIn this[int arg, long[] filler, char indexer6In] { get; set; } //CS1961 on "TIn"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("IContexts<TIn, TOut, TInv>.this[int, long[], char]", "TIn", "contravariant", "invariantly").WithLocation(14, 5),
                // (17,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IContexts<TIn, TOut, TInv>.Method2In()'. 'TIn' is contravariant.
                //     TIn Method2In(); //CS1961 on "TIn"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("IContexts<TIn, TOut, TInv>.Method2In()", "TIn", "contravariant", "covariantly").WithLocation(17, 5),
                // (18,23): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IContexts<TIn, TOut, TInv>.Method3In(out TIn)'. 'TIn' is contravariant.
                //     int Method3In(out TIn p); //CS1961 on "TIn"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("IContexts<TIn, TOut, TInv>.Method3In(out TIn)", "TIn", "contravariant", "invariantly").WithLocation(18, 23),
                // (19,23): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IContexts<TIn, TOut, TInv>.Method4In(ref TIn)'. 'TIn' is contravariant.
                //     int Method4In(ref TIn p); //CS1961 on "TIn"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("IContexts<TIn, TOut, TInv>.Method4In(ref TIn)", "TIn", "contravariant", "invariantly").WithLocation(19, 23),
                // (25,5): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IContexts<TIn, TOut, TInv>.Property1Out'. 'TOut' is covariant.
                //     TOut Property1Out { set; } //CS1961 on "TOut"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IContexts<TIn, TOut, TInv>.Property1Out", "TOut", "covariant", "contravariantly").WithLocation(25, 5),
                // (27,5): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IContexts<TIn, TOut, TInv>.Property3Out'. 'TOut' is covariant.
                //     TOut Property3Out { get; set; } //CS1961 on "TOut"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IContexts<TIn, TOut, TInv>.Property3Out", "TOut", "covariant", "invariantly").WithLocation(27, 5),
                // (29,14): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IContexts<TIn, TOut, TInv>.this[TOut, char, bool]'. 'TOut' is covariant.
                //     int this[TOut arg, char filler, bool indexer1Out] { get; } //CS1961 on "TOut"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IContexts<TIn, TOut, TInv>.this[TOut, char, bool]", "TOut", "covariant", "contravariantly").WithLocation(29, 14),
                // (31,14): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IContexts<TIn, TOut, TInv>.this[TOut, bool, bool]'. 'TOut' is covariant.
                //     int this[TOut arg, bool filler, bool indexer3Out] { set; } //CS1961 on "TOut"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IContexts<TIn, TOut, TInv>.this[TOut, bool, bool]", "TOut", "covariant", "contravariantly").WithLocation(31, 14),
                // (32,5): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IContexts<TIn, TOut, TInv>.this[int, bool[], bool]'. 'TOut' is covariant.
                //     TOut this[int arg, bool[] filler, bool indexer4Out] { set; } //CS1961 on "TOut"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IContexts<TIn, TOut, TInv>.this[int, bool[], bool]", "TOut", "covariant", "contravariantly").WithLocation(32, 5),
                // (33,14): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IContexts<TIn, TOut, TInv>.this[TOut, long, bool]'. 'TOut' is covariant.
                //     int this[TOut arg, long filler, bool indexer5Out] { get; set; } //CS1961 on "TOut"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IContexts<TIn, TOut, TInv>.this[TOut, long, bool]", "TOut", "covariant", "contravariantly").WithLocation(33, 14),
                // (34,5): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IContexts<TIn, TOut, TInv>.this[int, long[], bool]'. 'TOut' is covariant.
                //     TOut this[int arg, long[] filler, bool indexer6Out] { get; set; } //CS1961 on "TOut"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IContexts<TIn, TOut, TInv>.this[int, long[], bool]", "TOut", "covariant", "invariantly").WithLocation(34, 5),
                // (36,21): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IContexts<TIn, TOut, TInv>.Method1Out(TOut)'. 'TOut' is covariant.
                //     long Method1Out(TOut p); //CS1961 on "TOut"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IContexts<TIn, TOut, TInv>.Method1Out(TOut)", "TOut", "covariant", "contravariantly").WithLocation(36, 21),
                // (38,25): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IContexts<TIn, TOut, TInv>.Method3Out(out TOut)'. 'TOut' is covariant.
                //     long Method3Out(out TOut p); //CS1961 on "TOut" (sic: out params have to be input-safe)
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IContexts<TIn, TOut, TInv>.Method3Out(out TOut)", "TOut", "covariant", "invariantly").WithLocation(38, 25),
                // (39,25): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IContexts<TIn, TOut, TInv>.Method4Out(ref TOut)'. 'TOut' is covariant.
                //     long Method4Out(ref TOut p); //CS1961 on "TOut"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IContexts<TIn, TOut, TInv>.Method4Out(ref TOut)", "TOut", "covariant", "invariantly").WithLocation(39, 25),
                // (41,22): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IContexts<TIn, TOut, TInv>.Event1Out'. 'TOut' is covariant.
                //     event DOut<TOut> Event1Out; //CS1961 on "TOut"
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "Event1Out").WithArguments("IContexts<TIn, TOut, TInv>.Event1Out", "TOut", "covariant", "contravariantly").WithLocation(41, 22));
        }

        /// <summary>
        /// Test all of the contexts that require output safety.
        /// Note: some also require input safety.
        /// </summary>
        [Fact]
        public void CS1961ERR_UnexpectedVariance_OutputUnsafe()
        {
            var text = @"
interface IOutputUnsafe<in TIn, out TOut, TInv>
{
    #region Case 1: contravariant type parameter
    TInv Property1Good { get; }
    TInv this[long[] Indexer1Good] { get; }
    TInv Method1Good();

    TIn Property1Bad { get; }
    TIn this[char[] Indexer1Bad] { get; }
    TIn Method1Bad();
    #endregion Case 1

    #region Case 2: array of output-unsafe
    TInv[] Property2Good { get; }
    TInv[] this[long[,] Indexer2Good] { get; }
    TInv[] Method2Good();

    TIn[] Property2Bad { get; }
    TIn[] this[char[,] Indexer2Bad] { get; }
    TIn[] Method2Bad();
    #endregion Case 2

    #region Case 3: constructed with output-unsafe type arg in covariant slot
    IOut<TInv> Property3Good { get; }
    IOut<TInv> this[long[,,] Indexer3Good] { get; }
    IOut<TInv> Method3Good();

    IOut<TIn> Property3Bad { get; }
    IOut<TIn> this[char[,,] Indexer3Bad] { get; }
    IOut<TIn> Method3Bad();
    #endregion Case 3

    #region Case 4: constructed with output-unsafe type arg in invariant slot
    IInv<TInv> Property4Good { get; }
    IInv<TInv> this[long[,,,] Indexer4Good] { get; }
    IInv<TInv> Method4Good();

    IInv<TIn> Property4Bad { get; }
    IInv<TIn> this[char[,,,] Indexer4Bad] { get; }
    IInv<TIn> Method4Bad();
    #endregion Case 4

    #region Case 5: constructed with input-unsafe (sic) type arg in contravariant slot
    IIn<TInv> Property5Good { get; }
    IIn<TInv> this[long[,,,,] Indexer5Good] { get; }
    IIn<TInv> Method5Good();

    IIn<TOut> Property5Bad { get; }
    IIn<TOut> this[char[,,,,] Indexer5Bad] { get; }
    IIn<TOut> Method5Bad();
    #endregion Case 5

    #region Case 6: constructed with input-unsafe (sic) type arg in invariant slot
    IInv<TInv> Property6Good { get; }
    IInv<TInv> this[long[,,,,,] Indexer6Good] { get; }
    IInv<TInv> Method6Good();

    IInv<TOut> Property6Bad { get; }
    IInv<TOut> this[char[,,,,,] Indexer6Bad] { get; }
    IInv<TOut> Method6Bad();
    #endregion Case 6
}

interface IIn<in T> { }
interface IOut<out T> { }
interface IInv<T> { }";

            CreateCompilation(text).VerifyDiagnostics(
                // (9,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Property1Bad'. 'TIn' is contravariant.
                //     TIn Property1Bad { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Property1Bad", "TIn", "contravariant", "covariantly").WithLocation(9, 5),
                // (10,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.this[char[]]'. 'TIn' is contravariant.
                //     TIn this[char[] Indexer1Bad] { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.this[char[]]", "TIn", "contravariant", "covariantly").WithLocation(10, 5),
                // (11,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Method1Bad()'. 'TIn' is contravariant.
                //     TIn Method1Bad();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Method1Bad()", "TIn", "contravariant", "covariantly").WithLocation(11, 5),
                // (19,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Property2Bad'. 'TIn' is contravariant.
                //     TIn[] Property2Bad { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn[]").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Property2Bad", "TIn", "contravariant", "covariantly").WithLocation(19, 5),
                // (20,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.this[char[*,*]]'. 'TIn' is contravariant.
                //     TIn[] this[char[,] Indexer2Bad] { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn[]").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.this[char[*,*]]", "TIn", "contravariant", "covariantly").WithLocation(20, 5),
                // (21,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Method2Bad()'. 'TIn' is contravariant.
                //     TIn[] Method2Bad();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn[]").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Method2Bad()", "TIn", "contravariant", "covariantly").WithLocation(21, 5),
                // (29,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Property3Bad'. 'TIn' is contravariant.
                //     IOut<TIn> Property3Bad { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IOut<TIn>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Property3Bad", "TIn", "contravariant", "covariantly").WithLocation(29, 5),
                // (30,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.this[char[*,*,*]]'. 'TIn' is contravariant.
                //     IOut<TIn> this[char[,,] Indexer3Bad] { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IOut<TIn>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.this[char[*,*,*]]", "TIn", "contravariant", "covariantly").WithLocation(30, 5),
                // (31,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Method3Bad()'. 'TIn' is contravariant.
                //     IOut<TIn> Method3Bad();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IOut<TIn>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Method3Bad()", "TIn", "contravariant", "covariantly").WithLocation(31, 5),
                // (39,5): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Property4Bad'. 'TIn' is contravariant.
                //     IInv<TIn> Property4Bad { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TIn>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Property4Bad", "TIn", "contravariant", "invariantly").WithLocation(39, 5),
                // (40,5): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*]]'. 'TIn' is contravariant.
                //     IInv<TIn> this[char[,,,] Indexer4Bad] { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TIn>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*]]", "TIn", "contravariant", "invariantly").WithLocation(40, 5),
                // (41,5): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Method4Bad()'. 'TIn' is contravariant.
                //     IInv<TIn> Method4Bad();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TIn>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Method4Bad()", "TIn", "contravariant", "invariantly").WithLocation(41, 5),
                // (49,5): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Property5Bad'. 'TOut' is covariant.
                //     IIn<TOut> Property5Bad { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IIn<TOut>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Property5Bad", "TOut", "covariant", "contravariantly").WithLocation(49, 5),
                // (50,5): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*]]'. 'TOut' is covariant.
                //     IIn<TOut> this[char[,,,,] Indexer5Bad] { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IIn<TOut>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*]]", "TOut", "covariant", "contravariantly").WithLocation(50, 5),
                // (51,5): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Method5Bad()'. 'TOut' is covariant.
                //     IIn<TOut> Method5Bad();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IIn<TOut>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Method5Bad()", "TOut", "covariant", "contravariantly").WithLocation(51, 5),
                // (59,5): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Property6Bad'. 'TOut' is covariant.
                //     IInv<TOut> Property6Bad { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TOut>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Property6Bad", "TOut", "covariant", "invariantly").WithLocation(59, 5),
                // (60,5): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*,*]]'. 'TOut' is covariant.
                //     IInv<TOut> this[char[,,,,,] Indexer6Bad] { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TOut>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*,*]]", "TOut", "covariant", "invariantly").WithLocation(60, 5),
                // (61,5): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IOutputUnsafe<TIn, TOut, TInv>.Method6Bad()'. 'TOut' is covariant.
                //     IInv<TOut> Method6Bad();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TOut>").WithArguments("IOutputUnsafe<TIn, TOut, TInv>.Method6Bad()", "TOut", "covariant", "invariantly").WithLocation(61, 5));
        }

        /// <summary>
        /// Test all of the contexts that require input safety.
        /// Note: some also require output safety.
        /// </summary>
        [Fact]
        public void CS1961ERR_UnexpectedVariance_InputUnsafe()
        {
            var text = @"
interface IInputUnsafe<in TIn, out TOut, TInv>
{
    #region Case 1: contravariant type parameter
    TInv Property1Good { set; }
    TInv this[long[] Indexer1GoodA] { set; }
    long this[long[] Indexer1GoodB, TInv p] { set; }
    long Method1Good(TInv p);

    TOut Property1Bad { set; }
    TOut this[char[] Indexer1BadA] { set; }
    long this[char[] Indexer1BadB, TOut p] { set; }
    long Method1Bad(TOut p);
    #endregion Case 1

    #region Case 2: array of input-unsafe
    TInv[] Property2Good { set; }
    TInv[] this[long[,] Indexer2GoodA] { set; }
    long this[long[,] Indexer2GoodB, TInv[] p] { set; }
    long Method2Good(TInv[] p);

    TOut[] Property2Bad { set; }
    TOut[] this[char[,] Indexer2BadA] { set; }
    long this[char[,] Indexer2BadB, TOut[] p] { set; }
    long Method2Bad(TOut[] p);
    #endregion Case 2

    #region Case 3: constructed with input-unsafe type arg in covariant (sic: not flipped) slot
    IOut<TInv> Property3Good { set; }
    IOut<TInv> this[long[,,] Indexer3GoodA] { set; }
    long this[long[,,] Indexer3GoodB, IOut<TInv> p] { set; }
    long Method3Good(IOut<TInv> p);
    event DOut<TInv> Event3Good;

    IOut<TOut> Property3Bad { set; }
    IOut<TOut> this[char[,,] Indexer3BadA] { set; }
    long this[char[,,] Indexer3BadB, IOut<TOut> p] { set; }
    long Method3Bad(IOut<TOut> p);
    event DOut<TOut> Event3Bad;
    #endregion Case 3

    #region Case 4: constructed with input-unsafe type arg in invariant slot
    IInv<TInv> Property4Good { set; }
    IInv<TInv> this[long[,,,] Indexer4GoodA] { set; }
    long this[long[,,,] Indexer4GoodB, IInv<TInv> p] { set; }
    long Method4Good(IInv<TInv> p);
    event DInv<TInv> Event4Good;

    IInv<TOut> Property4Bad { set; }
    IInv<TOut> this[char[,,,] Indexer4BadA] { set; }
    long this[char[,,,] Indexer4BadB, IInv<TOut> p] { set; }
    long Method4Bad(IInv<TOut> p);
    event DInv<TOut> Event4Bad;
    #endregion Case 4

    #region Case 5: constructed with output-unsafe (sic) type arg in contravariant (sic: not flipped) slot
    IIn<TInv> Property5Good { set; }
    IIn<TInv> this[long[,,,,] Indexer5GoodA] { set; }
    long this[long[,,,,] Indexer5GoodB, IIn<TInv> p] { set; }
    long Method5Good(IIn<TInv> p);
    event DIn<TInv> Event5Good;

    IIn<TIn> Property5Bad { set; }
    IIn<TIn> this[char[,,,,] Indexer5BadA] { set; }
    long this[char[,,,,] Indexer5BadB, IIn<TIn> p] { set; }
    long Method5Bad(IIn<TIn> p);
    event DIn<TIn> Event5Bad;
    #endregion Case 5

    #region Case 6: constructed with output-unsafe (sic) type arg in invariant slot
    IInv<TInv> Property6Good { set; }
    IInv<TInv> this[long[,,,,,] Indexer6GoodA] { set; }
    long this[long[,,,,,] Indexer6GoodB, IInv<TInv> p] { set; }
    long Method6Good(IInv<TInv> p);
    event DInv<TInv> Event6Good;

    IInv<TIn> Property6Bad { set; }
    IInv<TIn> this[char[,,,,,] Indexer6BadA] { set; }
    long this[char[,,,,,] Indexer6BadB, IInv<TIn> p] { set; }
    long Method6Bad(IInv<TIn> p);
    event DInv<TIn> Event6Bad;
    #endregion Case 6
}

interface IIn<in T> { }
interface IOut<out T> { }
interface IInv<T> { }

delegate void DIn<in T>();
delegate void DOut<out T>();
delegate void DInv<T>();
";

            CreateCompilation(text).VerifyDiagnostics(
                // (11,5): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[]]'. 'TOut' is covariant.
                //     TOut this[char[] Indexer1BadA] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[]]", "TOut", "covariant", "contravariantly").WithLocation(11, 5),
                // (12,36): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[], TOut]'. 'TOut' is covariant.
                //     long this[char[] Indexer1BadB, TOut p] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[], TOut]", "TOut", "covariant", "contravariantly").WithLocation(12, 36),
                // (23,5): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[*,*]]'. 'TOut' is covariant.
                //     TOut[] this[char[,] Indexer2BadA] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut[]").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[*,*]]", "TOut", "covariant", "contravariantly").WithLocation(23, 5),
                // (24,37): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[*,*], TOut[]]'. 'TOut' is covariant.
                //     long this[char[,] Indexer2BadB, TOut[] p] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut[]").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[*,*], TOut[]]", "TOut", "covariant", "contravariantly").WithLocation(24, 37),
                // (36,5): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*]]'. 'TOut' is covariant.
                //     IOut<TOut> this[char[,,] Indexer3BadA] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IOut<TOut>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*]]", "TOut", "covariant", "contravariantly").WithLocation(36, 5),
                // (37,38): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*], IOut<TOut>]'. 'TOut' is covariant.
                //     long this[char[,,] Indexer3BadB, IOut<TOut> p] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IOut<TOut>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*], IOut<TOut>]", "TOut", "covariant", "contravariantly").WithLocation(37, 38),
                // (50,5): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*]]'. 'TOut' is covariant.
                //     IInv<TOut> this[char[,,,] Indexer4BadA] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TOut>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*]]", "TOut", "covariant", "invariantly").WithLocation(50, 5),
                // (51,39): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*], IInv<TOut>]'. 'TOut' is covariant.
                //     long this[char[,,,] Indexer4BadB, IInv<TOut> p] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TOut>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*], IInv<TOut>]", "TOut", "covariant", "invariantly").WithLocation(51, 39),
                // (64,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*]]'. 'TIn' is contravariant.
                //     IIn<TIn> this[char[,,,,] Indexer5BadA] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IIn<TIn>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*]]", "TIn", "contravariant", "covariantly").WithLocation(64, 5),
                // (65,40): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*], IIn<TIn>]'. 'TIn' is contravariant.
                //     long this[char[,,,,] Indexer5BadB, IIn<TIn> p] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IIn<TIn>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*], IIn<TIn>]", "TIn", "contravariant", "covariantly").WithLocation(65, 40),
                // (78,5): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*,*]]'. 'TIn' is contravariant.
                //     IInv<TIn> this[char[,,,,,] Indexer6BadA] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TIn>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*,*]]", "TIn", "contravariant", "invariantly").WithLocation(78, 5),
                // (79,41): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*,*], IInv<TIn>]'. 'TIn' is contravariant.
                //     long this[char[,,,,,] Indexer6BadB, IInv<TIn> p] { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TIn>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.this[char[*,*,*,*,*,*], IInv<TIn>]", "TIn", "contravariant", "invariantly").WithLocation(79, 41),
                // (10,5): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Property1Bad'. 'TOut' is covariant.
                //     TOut Property1Bad { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Property1Bad", "TOut", "covariant", "contravariantly").WithLocation(10, 5),
                // (13,21): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Method1Bad(TOut)'. 'TOut' is covariant.
                //     long Method1Bad(TOut p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Method1Bad(TOut)", "TOut", "covariant", "contravariantly").WithLocation(13, 21),
                // (22,5): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Property2Bad'. 'TOut' is covariant.
                //     TOut[] Property2Bad { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut[]").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Property2Bad", "TOut", "covariant", "contravariantly").WithLocation(22, 5),
                // (25,21): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Method2Bad(TOut[])'. 'TOut' is covariant.
                //     long Method2Bad(TOut[] p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut[]").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Method2Bad(TOut[])", "TOut", "covariant", "contravariantly").WithLocation(25, 21),
                // (35,5): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Property3Bad'. 'TOut' is covariant.
                //     IOut<TOut> Property3Bad { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IOut<TOut>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Property3Bad", "TOut", "covariant", "contravariantly").WithLocation(35, 5),
                // (38,21): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Method3Bad(IOut<TOut>)'. 'TOut' is covariant.
                //     long Method3Bad(IOut<TOut> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IOut<TOut>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Method3Bad(IOut<TOut>)", "TOut", "covariant", "contravariantly").WithLocation(38, 21),
                // (39,22): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Event3Bad'. 'TOut' is covariant.
                //     event DOut<TOut> Event3Bad;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "Event3Bad").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Event3Bad", "TOut", "covariant", "contravariantly").WithLocation(39, 22),
                // (49,5): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Property4Bad'. 'TOut' is covariant.
                //     IInv<TOut> Property4Bad { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TOut>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Property4Bad", "TOut", "covariant", "invariantly").WithLocation(49, 5),
                // (52,21): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Method4Bad(IInv<TOut>)'. 'TOut' is covariant.
                //     long Method4Bad(IInv<TOut> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TOut>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Method4Bad(IInv<TOut>)", "TOut", "covariant", "invariantly").WithLocation(52, 21),
                // (53,22): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Event4Bad'. 'TOut' is covariant.
                //     event DInv<TOut> Event4Bad;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "Event4Bad").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Event4Bad", "TOut", "covariant", "invariantly").WithLocation(53, 22),
                // (63,5): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Property5Bad'. 'TIn' is contravariant.
                //     IIn<TIn> Property5Bad { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IIn<TIn>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Property5Bad", "TIn", "contravariant", "covariantly").WithLocation(63, 5),
                // (66,21): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Method5Bad(IIn<TIn>)'. 'TIn' is contravariant.
                //     long Method5Bad(IIn<TIn> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IIn<TIn>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Method5Bad(IIn<TIn>)", "TIn", "contravariant", "covariantly").WithLocation(66, 21),
                // (67,20): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Event5Bad'. 'TIn' is contravariant.
                //     event DIn<TIn> Event5Bad;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "Event5Bad").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Event5Bad", "TIn", "contravariant", "covariantly").WithLocation(67, 20),
                // (77,5): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Property6Bad'. 'TIn' is contravariant.
                //     IInv<TIn> Property6Bad { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TIn>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Property6Bad", "TIn", "contravariant", "invariantly").WithLocation(77, 5),
                // (80,21): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Method6Bad(IInv<TIn>)'. 'TIn' is contravariant.
                //     long Method6Bad(IInv<TIn> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInv<TIn>").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Method6Bad(IInv<TIn>)", "TIn", "contravariant", "invariantly").WithLocation(80, 21),
                // (81,21): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'IInputUnsafe<TIn, TOut, TInv>.Event6Bad'. 'TIn' is contravariant.
                //     event DInv<TIn> Event6Bad;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "Event6Bad").WithArguments("IInputUnsafe<TIn, TOut, TInv>.Event6Bad", "TIn", "contravariant", "invariantly").WithLocation(81, 21));
        }

        /// <summary>
        /// Test output-safety checks on base interfaces.
        /// </summary>
        [Fact]
        public void CS1961ERR_UnexpectedVariance_BaseInterfaces()
        {
            var text = @"
interface IBaseInterfaces<in TIn, out TOut, TInv> : IIn<TOut>, IOut<TIn>, IInv<TInv> { }

interface IIn<in T> { }
interface IOut<out T> { }
interface IInv<T> { }";

            CreateCompilation(text).VerifyDiagnostics(
                // (2,39): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'IIn<TOut>'. 'TOut' is covariant.
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("IIn<TOut>", "TOut", "covariant", "contravariantly"),
                // (2,30): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'IOut<TIn>'. 'TIn' is contravariant.
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("IOut<TIn>", "TIn", "contravariant", "covariantly"));
        }

        /// <summary>
        /// Test all type parameter/type argument combinations.
        ///                          | Type Arg Covariant   | Type Arg Contravariant | Type Arg Invariant
        /// -------------------------+----------------------+------------------------+--------------------
        /// Type Param Covariant     | Covariant            | Contravariant          | Invariant
        /// Type Param Contravariant | Contravariant        | Covariant              | Invariant
        /// Type Param Invariant     | Error                | Error                  | Invariant
        /// </summary>
        [Fact]
        public void CS1961ERR_UnexpectedVariance_Generics()
        {
            var text = @"
interface IOutputUnsafeTable<out TInputUnsafe, in TOutputUnsafe, TInvariant>
{
    ICovariant<TInputUnsafe> OutputUnsafe1();
    ICovariant<TOutputUnsafe> OutputUnsafe2();
    ICovariant<TInvariant> OutputUnsafe3();

    IContravariant<TInputUnsafe> OutputUnsafe4();
    IContravariant<TOutputUnsafe> OutputUnsafe5();
    IContravariant<TInvariant> OutputUnsafe6();

    IInvariant<TInputUnsafe> OutputUnsafe7();
    IInvariant<TOutputUnsafe> OutputUnsafe8();
    IInvariant<TInvariant> OutputUnsafe9();
}

interface IInputUnsafeTable<out TInputUnsafe, in TOutputUnsafe, TInvariant>
{
    void InputUnsafe1(ICovariant<TInputUnsafe> p);
    void InputUnsafe2(ICovariant<TOutputUnsafe> p);
    void InputUnsafe3(ICovariant<TInvariant> p);

    void InputUnsafe4(IContravariant<TInputUnsafe> p);
    void InputUnsafe5(IContravariant<TOutputUnsafe> p);
    void InputUnsafe6(IContravariant<TInvariant> p);

    void InputUnsafe7(IInvariant<TInputUnsafe> p);
    void InputUnsafe8(IInvariant<TOutputUnsafe> p);
    void InputUnsafe9(IInvariant<TInvariant> p);
}

interface IBothUnsafeTable<out TInputUnsafe, in TOutputUnsafe, TInvariant>
{
    void InputUnsafe1(ref ICovariant<TInputUnsafe> p);
    void InputUnsafe2(ref ICovariant<TOutputUnsafe> p);
    void InputUnsafe3(ref ICovariant<TInvariant> p);

    void InputUnsafe4(ref IContravariant<TInputUnsafe> p);
    void InputUnsafe5(ref IContravariant<TOutputUnsafe> p);
    void InputUnsafe6(ref IContravariant<TInvariant> p);

    void InputUnsafe7(ref IInvariant<TInputUnsafe> p);
    void InputUnsafe8(ref IInvariant<TOutputUnsafe> p);
    void InputUnsafe9(ref IInvariant<TInvariant> p);
}

interface ICovariant<out T> { }
interface IContravariant<in T> { }
interface IInvariant<T> { }";

            CreateCompilation(text).VerifyDiagnostics(
                // (5,5): error CS1961: Invalid variance: The type parameter 'TOutputUnsafe' must be covariantly valid on 'IOutputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.OutputUnsafe2()'. 'TOutputUnsafe' is contravariant.
                //     ICovariant<TOutputUnsafe> OutputUnsafe2();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ICovariant<TOutputUnsafe>").WithArguments("IOutputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.OutputUnsafe2()", "TOutputUnsafe", "contravariant", "covariantly").WithLocation(5, 5),
                // (8,5): error CS1961: Invalid variance: The type parameter 'TInputUnsafe' must be contravariantly valid on 'IOutputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.OutputUnsafe4()'. 'TInputUnsafe' is covariant.
                //     IContravariant<TInputUnsafe> OutputUnsafe4();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IContravariant<TInputUnsafe>").WithArguments("IOutputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.OutputUnsafe4()", "TInputUnsafe", "covariant", "contravariantly").WithLocation(8, 5),
                // (12,5): error CS1961: Invalid variance: The type parameter 'TInputUnsafe' must be invariantly valid on 'IOutputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.OutputUnsafe7()'. 'TInputUnsafe' is covariant.
                //     IInvariant<TInputUnsafe> OutputUnsafe7();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInvariant<TInputUnsafe>").WithArguments("IOutputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.OutputUnsafe7()", "TInputUnsafe", "covariant", "invariantly").WithLocation(12, 5),
                // (13,5): error CS1961: Invalid variance: The type parameter 'TOutputUnsafe' must be invariantly valid on 'IOutputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.OutputUnsafe8()'. 'TOutputUnsafe' is contravariant.
                //     IInvariant<TOutputUnsafe> OutputUnsafe8();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInvariant<TOutputUnsafe>").WithArguments("IOutputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.OutputUnsafe8()", "TOutputUnsafe", "contravariant", "invariantly").WithLocation(13, 5),
                // (19,23): error CS1961: Invalid variance: The type parameter 'TInputUnsafe' must be contravariantly valid on 'IInputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe1(ICovariant<TInputUnsafe>)'. 'TInputUnsafe' is covariant.
                //     void InputUnsafe1(ICovariant<TInputUnsafe> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ICovariant<TInputUnsafe>").WithArguments("IInputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe1(ICovariant<TInputUnsafe>)", "TInputUnsafe", "covariant", "contravariantly").WithLocation(19, 23),
                // (24,23): error CS1961: Invalid variance: The type parameter 'TOutputUnsafe' must be covariantly valid on 'IInputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe5(IContravariant<TOutputUnsafe>)'. 'TOutputUnsafe' is contravariant.
                //     void InputUnsafe5(IContravariant<TOutputUnsafe> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IContravariant<TOutputUnsafe>").WithArguments("IInputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe5(IContravariant<TOutputUnsafe>)", "TOutputUnsafe", "contravariant", "covariantly").WithLocation(24, 23),
                // (27,23): error CS1961: Invalid variance: The type parameter 'TInputUnsafe' must be invariantly valid on 'IInputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe7(IInvariant<TInputUnsafe>)'. 'TInputUnsafe' is covariant.
                //     void InputUnsafe7(IInvariant<TInputUnsafe> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInvariant<TInputUnsafe>").WithArguments("IInputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe7(IInvariant<TInputUnsafe>)", "TInputUnsafe", "covariant", "invariantly").WithLocation(27, 23),
                // (28,23): error CS1961: Invalid variance: The type parameter 'TOutputUnsafe' must be invariantly valid on 'IInputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe8(IInvariant<TOutputUnsafe>)'. 'TOutputUnsafe' is contravariant.
                //     void InputUnsafe8(IInvariant<TOutputUnsafe> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInvariant<TOutputUnsafe>").WithArguments("IInputUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe8(IInvariant<TOutputUnsafe>)", "TOutputUnsafe", "contravariant", "invariantly").WithLocation(28, 23),

                // Dev10 doesn't say "must be invariantly valid" for ref params - it lists whichever check fails first.  This approach seems nicer.

                // (34,27): error CS1961: Invalid variance: The type parameter 'TInputUnsafe' must be invariantly valid on 'IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe1(ref ICovariant<TInputUnsafe>)'. 'TInputUnsafe' is covariant.
                //     void InputUnsafe1(ref ICovariant<TInputUnsafe> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ICovariant<TInputUnsafe>").WithArguments("IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe1(ref ICovariant<TInputUnsafe>)", "TInputUnsafe", "covariant", "invariantly").WithLocation(34, 27),
                // (35,27): error CS1961: Invalid variance: The type parameter 'TOutputUnsafe' must be invariantly valid on 'IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe2(ref ICovariant<TOutputUnsafe>)'. 'TOutputUnsafe' is contravariant.
                //     void InputUnsafe2(ref ICovariant<TOutputUnsafe> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ICovariant<TOutputUnsafe>").WithArguments("IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe2(ref ICovariant<TOutputUnsafe>)", "TOutputUnsafe", "contravariant", "invariantly").WithLocation(35, 27),
                // (38,27): error CS1961: Invalid variance: The type parameter 'TInputUnsafe' must be invariantly valid on 'IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe4(ref IContravariant<TInputUnsafe>)'. 'TInputUnsafe' is covariant.
                //     void InputUnsafe4(ref IContravariant<TInputUnsafe> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IContravariant<TInputUnsafe>").WithArguments("IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe4(ref IContravariant<TInputUnsafe>)", "TInputUnsafe", "covariant", "invariantly").WithLocation(38, 27),
                // (39,27): error CS1961: Invalid variance: The type parameter 'TOutputUnsafe' must be invariantly valid on 'IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe5(ref IContravariant<TOutputUnsafe>)'. 'TOutputUnsafe' is contravariant.
                //     void InputUnsafe5(ref IContravariant<TOutputUnsafe> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IContravariant<TOutputUnsafe>").WithArguments("IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe5(ref IContravariant<TOutputUnsafe>)", "TOutputUnsafe", "contravariant", "invariantly").WithLocation(39, 27),
                // (42,27): error CS1961: Invalid variance: The type parameter 'TInputUnsafe' must be invariantly valid on 'IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe7(ref IInvariant<TInputUnsafe>)'. 'TInputUnsafe' is covariant.
                //     void InputUnsafe7(ref IInvariant<TInputUnsafe> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInvariant<TInputUnsafe>").WithArguments("IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe7(ref IInvariant<TInputUnsafe>)", "TInputUnsafe", "covariant", "invariantly").WithLocation(42, 27),
                // (43,27): error CS1961: Invalid variance: The type parameter 'TOutputUnsafe' must be invariantly valid on 'IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe8(ref IInvariant<TOutputUnsafe>)'. 'TOutputUnsafe' is contravariant.
                //     void InputUnsafe8(ref IInvariant<TOutputUnsafe> p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IInvariant<TOutputUnsafe>").WithArguments("IBothUnsafeTable<TInputUnsafe, TOutputUnsafe, TInvariant>.InputUnsafe8(ref IInvariant<TOutputUnsafe>)", "TOutputUnsafe", "contravariant", "invariantly").WithLocation(43, 27));
        }

        [Fact]
        public void CS1961ERR_UnexpectedVariance_DelegateInvoke()
        {
            var text = @"
delegate TIn D1<in TIn>(); //CS1961
delegate TOut D2<out TOut>();
delegate T D3<T>();

delegate void D4<in TIn>(TIn p);
delegate void D5<out TOut>(TOut p); //CS1961
delegate void D6<T>(T p);

delegate void D7<in TIn>(ref TIn p); //CS1961
delegate void D8<out TOut>(ref TOut p); //CS1961
delegate void D9<T>(ref T p);

delegate void D10<in TIn>(out TIn p); //CS1961
delegate void D11<out TOut>(out TOut p); //CS1961
delegate void D12<T>(out T p);
";

            CreateCompilation(text).VerifyDiagnostics(
                // (2,20): error CS1961: Invalid variance: The type parameter 'TIn' must be covariantly valid on 'D1<TIn>.Invoke()'. 'TIn' is contravariant.
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("D1<TIn>.Invoke()", "TIn", "contravariant", "covariantly"),
                // (7,22): error CS1961: Invalid variance: The type parameter 'TOut' must be contravariantly valid on 'D5<TOut>.Invoke(TOut)'. 'TOut' is covariant.
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("D5<TOut>.Invoke(TOut)", "TOut", "covariant", "contravariantly"),
                // (10,21): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'D7<TIn>.Invoke(ref TIn)'. 'TIn' is contravariant.
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("D7<TIn>.Invoke(ref TIn)", "TIn", "contravariant", "invariantly"),
                // (11,22): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'D8<TOut>.Invoke(ref TOut)'. 'TOut' is covariant.
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("D8<TOut>.Invoke(ref TOut)", "TOut", "covariant", "invariantly"),
                // (14,22): error CS1961: Invalid variance: The type parameter 'TIn' must be invariantly valid on 'D10<TIn>.Invoke(out TIn)'. 'TIn' is contravariant.
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TIn").WithArguments("D10<TIn>.Invoke(out TIn)", "TIn", "contravariant", "invariantly"),
                // (15,23): error CS1961: Invalid variance: The type parameter 'TOut' must be invariantly valid on 'D11<TOut>.Invoke(out TOut)'. 'TOut' is covariant.
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "TOut").WithArguments("D11<TOut>.Invoke(out TOut)", "TOut", "covariant", "invariantly"));
        }

        [Fact]
        public void CS1962ERR_BadDynamicTypeof()
        {
            var text = @"
public class C
{
    public static int Main()
        {
            dynamic S = typeof(dynamic);
        return 0;
        }
    public static int Test(int age)
    { 
        return 1;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,25): error CS1962: The typeof operator cannot be used on the dynamic type
                Diagnostic(ErrorCode.ERR_BadDynamicTypeof, "typeof(dynamic)"));
        }

        // CS1963ERR_ExpressionTreeContainsDynamicOperation --> SyntaxBinderTests

        [Fact]
        public void CS1964ERR_BadDynamicConversion()
        {
            var text = @"
class A
{
    public static implicit operator dynamic(A a)
    {
        return a;
    }

    public static implicit operator A(dynamic a)
    {
        return a;
}
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (9,37): error CS1964: 'A.implicit operator A(dynamic)': user-defined conversions to or from the dynamic type are not allowed
                Diagnostic(ErrorCode.ERR_BadDynamicConversion, "A").WithArguments("A.implicit operator A(dynamic)"),
                // (4,37): error CS1964: 'A.implicit operator dynamic(A)': user-defined conversions to or from the dynamic type are not allowed
                Diagnostic(ErrorCode.ERR_BadDynamicConversion, "dynamic").WithArguments("A.implicit operator dynamic(A)"));
        }

        // CS1969ERR_DynamicRequiredTypesMissing -> CodeGen_DynamicTests.Missing_*
        // CS1970ERR_ExplicitDynamicAttr --> AttributeTests_Dynamic.ExplicitDynamicAttribute

        [Fact]
        public void CS1971ERR_NoDynamicPhantomOnBase()
        {
            const string text = @"
public class B
{
    public virtual void M(object o) {}
}
public class D : B
{
    public override void M(object o) {}

    void N(dynamic d)
    {
        base.M(d);
    }
}
";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(text);
            comp.VerifyDiagnostics(
                // (12,9): error CS1971: The call to method 'M' needs to be dynamically dispatched, but cannot be because it is part of a base access expression. Consider casting the dynamic arguments or eliminating the base access.
                //         base.M(d);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBase, "base.M(d)").WithArguments("M"));
        }

        [Fact]
        public void CS1972ERR_NoDynamicPhantomOnBaseIndexer()
        {
            const string text = @"
public class B
{
    public string this[int index]
    {
        get { return ""You passed "" + index; }
    }
}
public class D : B
{
    public void M(object o)
    {
        int[] arr = { 1, 2, 3 };
        int s = base[(dynamic)o];
    }
}
";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(text);
            comp.VerifyDiagnostics(
                // (14,17): error CS1972: The indexer access needs to be dynamically dispatched, but cannot be because it is part of a base access expression. Consider casting the dynamic arguments or eliminating the base access.
                //         int s = base[(dynamic)o];
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseIndexer, "base[(dynamic)o]"));
        }

        [Fact]
        public void CS1973ERR_BadArgTypeDynamicExtension()
        {
            const string text = @"
class Program
{
    static void Main()
    {
        dynamic d = 1;
        B b = new B();
        b.Goo(d);
    }
}
public class B { }
static public class Extension
{
    public static void Goo(this B b, int x) { }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(text);
            comp.VerifyDiagnostics(
// (8,9): error CS1973: 'B' has no applicable method named 'Goo' but appears to have an extension method by that name. Extension methods cannot be dynamically dispatched. Consider casting the dynamic arguments or calling the extension method without the extension method syntax.
//         b.Goo(d);
Diagnostic(ErrorCode.ERR_BadArgTypeDynamicExtension, "b.Goo(d)").WithArguments("B", "Goo"));
        }

        [Fact]
        public void CS1975ERR_NoDynamicPhantomOnBaseCtor_Base()
        {
            var text = @"
class A
{
    public A(int x)
    {

    }
}
class B : A
{
    public B(dynamic d)
        : base(d)
    { }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (12,9): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "base"));
        }

        [Fact]
        public void CS1975ERR_NoDynamicPhantomOnBaseCtor_This()
        {
            var text = @"
class B
{
    public B(dynamic d)
        : this(d, 1)
    { }

    public B(int a, int b)
    { }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (12,9): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "this"));
        }

        [Fact]
        public void CS1976ERR_BadDynamicMethodArgMemgrp()
        {
            const string text = @"
class Program
{
    static void M(dynamic d)
    {
        d.Goo(M);
    }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(text);
            comp.VerifyDiagnostics(
                // (6,15): error CS1976: Cannot use a method group as an argument to a dynamically dispatched operation. Did you intend to invoke the method?
                //         d.Goo(M);
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgMemgrp, "M"));
        }

        [Fact]
        public void CS1977ERR_BadDynamicMethodArgLambda()
        {
            const string text = @"
class Program
{
    static void M(dynamic d)
    {
        d.Goo(()=>{});
        d.Goo(delegate () {});
    }
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(text);
            comp.VerifyDiagnostics(
                // (6,15): error CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                //         d.Goo(()=>{});
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "()=>{}"),
                // (7,15): error CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                //         d.Goo(delegate () {});
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "delegate () {}"));
        }

        [Fact, WorkItem(578352, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578352")]
        public void CS1977ERR_BadDynamicMethodArgLambda_CreateObject()
        {
            string source = @"
using System;
 
class C
{
    static void Main()
    {
        dynamic y = null;
        new C(delegate { }, y);
    }
 
    public C(Action a, Action y)
    {
    }
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source, new[] { CSharpRef });
            comp.VerifyDiagnostics(
                // (9,15): error CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "delegate { }"));
        }

        [Fact]
        public void CS1660ERR_BadDynamicMethodArgLambda_CollectionInitializer()
        {
            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;

unsafe  class C : IEnumerable<object>
{
    public static void M(__arglist) 
    {
        int a; 
        int* p = &a;
        dynamic d = null;

        var c = new C
        {
            { d, delegate() { } },
            { d, 1, p },
            { d, __arglist },
            { d, GetEnumerator },
            { d, SomeStaticMethod },
        };
    }

    public static void SomeStaticMethod() {}

    public void Add(dynamic d, int x, int* ptr)
    {
    }

    public void Add(dynamic d, RuntimeArgumentHandle x)
    {
    }

    public void Add(dynamic d, Action f)
    {
    }

    public void Add(dynamic d, Func<IEnumerator<object>> f)
    {
    }

    public IEnumerator<object> GetEnumerator()
    {
        return null;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return null;
    }
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source, new[] { CSharpRef }, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (16,18): error CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                //             { d, delegate() { } },
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "delegate() { }").WithLocation(16, 18),
                // (17,21): error CS1978: Cannot use an expression of type 'int*' as an argument to a dynamically dispatched operation.
                //             { d, 1, p },
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "p").WithArguments("int*").WithLocation(17, 21),
                // (18,18): error CS1978: Cannot use an expression of type 'RuntimeArgumentHandle' as an argument to a dynamically dispatched operation.
                //             { d, __arglist },
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "__arglist").WithArguments("System.RuntimeArgumentHandle").WithLocation(18, 18),
                // (19,13): error CS1950: The best overloaded Add method 'C.Add(dynamic, RuntimeArgumentHandle)' for the collection initializer has some invalid arguments
                //             { d, GetEnumerator },
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "{ d, GetEnumerator }").WithArguments("C.Add(dynamic, System.RuntimeArgumentHandle)").WithLocation(19, 13),
                // (19,18): error CS1503: Argument 2: cannot convert from 'method group' to 'RuntimeArgumentHandle'
                //             { d, GetEnumerator },
                Diagnostic(ErrorCode.ERR_BadArgType, "GetEnumerator").WithArguments("2", "method group", "System.RuntimeArgumentHandle").WithLocation(19, 18),
                // (20,18): error CS1976: Cannot use a method group as an argument to a dynamically dispatched operation. Did you intend to invoke the method?
                //             { d, SomeStaticMethod },
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgMemgrp, "SomeStaticMethod").WithLocation(20, 18));
        }

        [Fact]
        public void CS1978ERR_BadDynamicMethodArg()
        {
            // The dev 10 compiler gives arguably wrong error here; it says that "TypedReference may not be
            // used as a type argument". Though that is true, and though what is happening here behind the scenes 
            // is that TypedReference is being used as a type argument to a dynamic call site helper method,
            // that's giving an error about an implementation detail. A better error is to say that
            // TypedReference is not a legal type in a dynamic operation.
            //
            // Dev10 compiler didn't report an error for by-ref pointer argument. See Dev10 bug 819498.
            // The error should be reported for any pointer argument regardless of its refness.
            const string text = @"
class Program
{
    unsafe static void M(dynamic d, int* i, System.TypedReference tr)
    {
        d.Goo(i);
        d.Goo(tr);
        d.Goo(ref tr);
        d.Goo(out tr);
        d.Goo(out i);
        d.Goo(ref i);
    }
}
";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(text, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (6,15): error CS1978: Cannot use an expression of type 'int*' as an argument to a dynamically dispatched operation.
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "i").WithArguments("int*"),
                // (7,15): error CS1978: Cannot use an expression of type 'System.TypedReference' as an argument to a dynamically dispatched operation.
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "tr").WithArguments("System.TypedReference"),
                // (8,19): error CS1978: Cannot use an expression of type 'System.TypedReference' as an argument to a dynamically dispatched operation.
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "tr").WithArguments("System.TypedReference"),
                // (9,19): error CS1978: Cannot use an expression of type 'System.TypedReference' as an argument to a dynamically dispatched operation.
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "tr").WithArguments("System.TypedReference"),
                // (10,19): error CS1978: Cannot use an expression of type 'int*' as an argument to a dynamically dispatched operation.
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "i").WithArguments("int*"),
                // (11,19): error CS1978: Cannot use an expression of type 'int*' as an argument to a dynamically dispatched operation.
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "i").WithArguments("int*"));
        }

        // CS1979ERR_BadDynamicQuery --> DynamicTests.cs, DynamicQuery_* 

        // Test CS1980ERR_DynamicAttributeMissing moved to AttributeTests_Dynamic.cs

        // CS1763 is covered for different code path by SymbolErrorTests.CS1763ERR_NotNullRefDefaultParameter()
        [WorkItem(528854, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528854")]
        [Fact]
        public void CS1763ERR_NotNullRefDefaultParameter02()
        {
            string text = @"
class Program
{
    public void Goo<T, U>(T t = default(U)) where U : T
    {
    }
    static void Main(string[] args)
    {
        
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,29): error CS1763: 't' is of type 'T'. A default parameter value of a reference type other than string can only be initialized with null
                //     public void Goo<T, U>(T t = default(U)) where U : T
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "t").WithArguments("t", "T"));
        }

        #endregion

        #region "Targeted Warning Tests - please arrange tests in the order of error code"

        [Fact, WorkItem(542396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542396"), WorkItem(546817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546817")]
        public void CS0067WRN_UnreferencedEvent()
        {
            var text = @"
delegate void MyDelegate();
class MyClass
{
    public event MyDelegate evt;   // CS0067
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,29): warning CS0067: The event 'MyClass.evt' is never used
                //     public event MyDelegate evt;   // CS0067
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "evt").WithArguments("MyClass.evt"));
        }

        [Fact, WorkItem(542396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542396"), WorkItem(546817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546817")]
        public void CS0067WRN_UnreferencedEvent_Accessibility()
        {
            var text = @"
using System;
class MyClass
{
    public event Action E1;             // CS0067
    internal event Action E2;           // CS0067
    protected internal event Action E3; // CS0067
    protected event Action E4;          // CS0067
    private event Action E5;            // CS0067
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,25): warning CS0067: The event 'MyClass.E1' is never used
                //     public event Action E1;             // CS0067
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("MyClass.E1"),
                // (6,27): warning CS0067: The event 'MyClass.E2' is never used
                //     internal event Action E2;           // CS0067
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E2").WithArguments("MyClass.E2"),
                // (7,37): warning CS0067: The event 'MyClass.E3' is never used
                //     protected internal event Action E3; // CS0067
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E3").WithArguments("MyClass.E3"),
                // (8,28): warning CS0067: The event 'MyClass.E4' is never used
                //     protected event Action E4;          // CS0067
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E4").WithArguments("MyClass.E4"),
                // (9,26): warning CS0067: The event 'MyClass.E5' is never used
                //     private event Action E5;            // CS0067
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E5").WithArguments("MyClass.E5"));
        }

        [Fact, WorkItem(542396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542396"), WorkItem(546817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546817")]
        public void CS0067WRN_UnreferencedEvent_StructLayout()
        {
            var text = @"
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
struct S
{
    event Action E1;
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact, WorkItem(542396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542396"), WorkItem(546817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546817")]
        public void CS0067WRN_UnreferencedEvent_Kind()
        {
            var text = @"
using System;

class C
{
    event Action E1; // CS0067
    event Action E2 { add { } remove { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,18): warning CS0067: The event 'C.E1' is never used
                //     event Action E1; // CS0067
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("C.E1"));
        }

        [Fact, WorkItem(542396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542396"), WorkItem(546817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546817")]
        public void CS0067WRN_UnreferencedEvent_Accessed()
        {
            var text = @"
using System;

class C
{
    event Action None; // CS0067
    event Action Read;
    event Action Write;
    event Action Add; // CS0067

    void M(Action a)
    {
        M(Read);
        Write = a;
        Add += a;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,18): warning CS0067: The event 'C.Add' is never used
                //     event Action Add; // CS0067
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Add").WithArguments("C.Add"),
                // (6,18): warning CS0067: The event 'C.None' is never used
                //     event Action None; // CS0067
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "None").WithArguments("C.None"));
        }

        [Fact, WorkItem(581002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581002")]
        public void CS0067WRN_UnreferencedEvent_Virtual()
        {
            var text = @"class A
{
    public virtual event System.EventHandler B;
    class C : A
    {
        public override event System.EventHandler B;
    }
    static int Main()
    {
        C c = new C();
        A a = c;
        return 0;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
    // (3,46): warning CS0067: The event 'A.B' is never used
    //     public virtual event System.EventHandler B;
    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "B").WithArguments("A.B"),
    // (6,51): warning CS0067: The event 'A.C.B' is never used
    //         public override event System.EventHandler B;
    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "B").WithArguments("A.C.B"));
        }


        [Fact, WorkItem(539630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539630")]
        public void CS0162WRN_UnreachableCode01()
        {
            var text = @"
class MyTest { }
class MyClass
{
    const MyTest test = null;
    public static int Main()
    {
      goto lab1;
      {
         // The following statements cannot be reached:
         int i = 9;   // CS0162 
         i++;
      }
      lab1:
        if (test == null)
        {
            return 0;
        }
        else
        {
            return 1;  // CS0162
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_UnreachableCode, "int"),
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"));
        }

        [Fact, WorkItem(530037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530037")]
        public void CS0162WRN_UnreachableCode02()
        {
            var text = @"
using System;

public class Test 
{
  public static void Main(string[] args)
  {
  // (1)
    do
    {
    for (; ; ) { }
    } while (args.Length > 0);  // Native CS0162
  // (2)
    for (; ; )   // Roslyn CS0162
    {
      goto L2;
      Console.WriteLine(""Unreachable code"");
    L2:         // Roslyn CS0162
      break;
    }
} }
";
            CreateCompilation(text).VerifyDiagnostics(
 // (14,5): warning CS0162: Unreachable code detected
 //     for (; ; )   // Roslyn CS0162
 Diagnostic(ErrorCode.WRN_UnreachableCode, "for"),
 // (18,5): warning CS0162: Unreachable code detected
 //     L2:         // Roslyn CS0162
 Diagnostic(ErrorCode.WRN_UnreachableCode, "L2")
            );
        }

        [Fact, WorkItem(539873, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539873"), WorkItem(539981, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539981")]
        public void CS0162WRN_UnreachableCode04()
        {
            var text = @"
public class Cls
{
    public static int Main()
    {
        goto Label2;
        return 0;
    Label1:
        return 1;
    Label2:
        goto Label1;
        return 2;
    }

    delegate void Sub_0(); 
    static void M()
    {
        Sub_0 s1_3 = () => { if (2 == 1) return; else return; };
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"),
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"),
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"));
        }

        [Fact, WorkItem(540901, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540901")]
        public void CS0162WRN_UnreachableCode06_Loops()
        {
            var text = @"
class Program
{
    void F()
    {
    }
    void T()
    {
        for (int i = 0; i < 0; F(), i++)   // F() is unreachable
        {
            return;
        }
    }

    static void Main()
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                goto stop;
                System.Console.WriteLine(y); // unreachable
            }

            foreach (char y in x)
            {
                throw new System.Exception();
                System.Console.WriteLine(y); // unreachable
            }
        stop:
            return;
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_UnreachableCode, "F"),
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System"),
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System"));
        }

        [Fact]
        public void CS0162WRN_UnreachableCode06_Foreach03()
        {
            var text = @"
public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                return;
                System.Console.WriteLine(y);
            }
        }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnreachableCode, "System"));
        }

        [Fact]
        public void CS0162WRN_UnreachableCode07_GotoInLambda()
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
        Action a = () => { goto label1; Console.WriteLine(""unreachable""); label1: Console.WriteLine(""reachable""); };
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnreachableCode, @"Console"));
        }

        [Fact]
        public void CS0164WRN_UnreferencedLabel()
        {
            var text = @"
public class a
{
   public int i = 0;

   public static void Main()
   {
      int i = 0;   // CS0164
      l1: i++;
   }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l1").WithLocation(9, 7));
        }

        [Fact]
        public void CS0168WRN_UnreferencedVar01()
        {
            var text = @"
public class clx
{
    public int i;
}

public class clz
{
    public static void Main()
    {
        int j ;   // CS0168, uncomment the following line
        // j++;
        clx a;       // CS0168, try the following line instead
        // clx a = new clx();
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "j").WithArguments("j").WithLocation(11, 13),
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "a").WithArguments("a").WithLocation(13, 13));
        }

        [Fact]
        public void CS0168WRN_UnreferencedVar02()
        {
            var text =
@"using System;
class C
{
    static void M()
    {
        try { }
        catch (InvalidOperationException e) { }
        catch (InvalidCastException e) { throw; }
        catch (Exception e) { throw e; }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(7, 42),
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(8, 37));
        }

        [Fact]
        public void CS0169WRN_UnreferencedField()
        {
            var text = @"
public class ClassX
{
   int i;   // CS0169, i is not used anywhere

   public static void Main()
   {
   }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,8): warning CS0169: The field 'ClassX.i' is never used
                //    int i;   // CS0169, i is not used anywhere
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("ClassX.i")
                );
        }

        [Fact]
        public void CS0169WRN_UnreferencedField02()
        {
            var text =
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""OtherAssembly"")]

internal class InternalClass
{
    internal int ActuallyInternal;
    internal int ActuallyInternalAssigned = 0;
    private int ActuallyPrivate;
    private int ActuallyPrivateAssigned = 0;
    public int EffectivelyInternal;
    public int EffectivelyInternalAssigned = 0;

    private class PrivateClass
    {
        public int EffectivelyPrivate;
        public int EffectivelyPrivateAssigned = 0;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (7,17): warning CS0169: The field 'InternalClass.ActuallyPrivate' is never used
                //     private int ActuallyPrivate;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "ActuallyPrivate").WithArguments("InternalClass.ActuallyPrivate"),
                // (8,17): warning CS0414: The field 'InternalClass.ActuallyPrivateAssigned' is assigned but its value is never used
                //     private int ActuallyPrivateAssigned = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "ActuallyPrivateAssigned").WithArguments("InternalClass.ActuallyPrivateAssigned"),
                // (14,20): warning CS0649: Field 'InternalClass.PrivateClass.EffectivelyPrivate' is never assigned to, and will always have its default value 0
                //         public int EffectivelyPrivate;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "EffectivelyPrivate").WithArguments("InternalClass.PrivateClass.EffectivelyPrivate", "0")
                );
        }

        [Fact]
        public void CS0169WRN_UnreferencedField03()
        {
            var text =
@"internal class InternalClass
{
    internal int ActuallyInternal;
    internal int ActuallyInternalAssigned = 0;
    private int ActuallyPrivate;
    private int ActuallyPrivateAssigned = 0;
    public int EffectivelyInternal;
    public int EffectivelyInternalAssigned = 0;

    private class PrivateClass
    {
        public int EffectivelyPrivate;
        public int EffectivelyPrivateAssigned = 0;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (3,18): warning CS0649: Field 'InternalClass.ActuallyInternal' is never assigned to, and will always have its default value 0
                //     internal int ActuallyInternal;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "ActuallyInternal").WithArguments("InternalClass.ActuallyInternal", "0"),
                // (5,17): warning CS0169: The field 'InternalClass.ActuallyPrivate' is never used
                //     private int ActuallyPrivate;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "ActuallyPrivate").WithArguments("InternalClass.ActuallyPrivate"),
                // (6,17): warning CS0414: The field 'InternalClass.ActuallyPrivateAssigned' is assigned but its value is never used
                //     private int ActuallyPrivateAssigned = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "ActuallyPrivateAssigned").WithArguments("InternalClass.ActuallyPrivateAssigned"),
                // (7,16): warning CS0649: Field 'InternalClass.EffectivelyInternal' is never assigned to, and will always have its default value 0
                //     public int EffectivelyInternal;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "EffectivelyInternal").WithArguments("InternalClass.EffectivelyInternal", "0"),
                // (12,20): warning CS0649: Field 'InternalClass.PrivateClass.EffectivelyPrivate' is never assigned to, and will always have its default value 0
                //         public int EffectivelyPrivate;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "EffectivelyPrivate").WithArguments("InternalClass.PrivateClass.EffectivelyPrivate", "0")
                );
        }

        [Fact]
        public void CS0183WRN_IsAlwaysTrue()
        {
            var text = @"using System;
public class IsTest10
{
    public static int Main(String[] args)
    {
        Object obj3 = null;
        String str2 = ""Is 'is' too strict, per error CS0183?"";
        obj3 = str2;

        if (str2 is Object)    // no error CS0183
            Console.WriteLine(""str2 is Object"");

        Int32 int2 = 1;
        if (int2 is Object)    // error CS0183
            Console.WriteLine(""int2 is Object"");

        return 0;
    }
}
";

            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_IsAlwaysTrue, Line = 14, Column = 13, IsWarning = true });

            // TODO: extra checking
        }

        // Note: CS0184 tests moved to CodeGenOperator.cs to include IL verification.

        [WorkItem(530361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530361")]
        [Fact]
        public void CS0197WRN_ByRefNonAgileField()
        {
            var text = @"
class X : System.MarshalByRefObject
{
   public int i;
}

class M
{
   public int i;
   static void AddSeventeen(ref int i)
   {
      i += 17;
   }

   static void Main()
   {
      X x = new X();
      x.i = 12;
      AddSeventeen(ref x.i);   // CS0197

      // OK
      M m = new M();
      m.i = 12;
      AddSeventeen(ref m.i);
   }
}
    ";
            CreateCompilation(text).VerifyDiagnostics(
                // (19,24): warning CS0197: Passing 'X.i' as ref or out or taking its address may cause a runtime exception because it is a field of a marshal-by-reference class
                //       AddSeventeen(ref x.i);   // CS0197
                Diagnostic(ErrorCode.WRN_ByRefNonAgileField, "x.i").WithArguments("X.i"));
        }

        [WorkItem(530361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530361")]
        [Fact]
        public void CS0197WRN_ByRefNonAgileField_RefKind()
        {
            var text = @"
class NotByRef
{
    public int Instance;
    public static int Static;
}

class ByRef : System.MarshalByRefObject
{
    public int Instance;
    public static int Static;
}

class Test
{
    void M(ByRef b, NotByRef n)
    {
        None(n.Instance);
        Out(out n.Instance);
        Ref(ref n.Instance);

        None(NotByRef.Static);
        Out(out NotByRef.Static);
        Ref(ref NotByRef.Static);

        None(b.Instance);
        Out(out b.Instance);
        Ref(ref b.Instance);

        None(ByRef.Static);
        Out(out ByRef.Static);
        Ref(ref ByRef.Static);
    }

    void None(int x) { throw null; }
    void Out(out int x) { throw null; }
    void Ref(ref int x) { throw null; }
}
    ";
            CreateCompilation(text).VerifyDiagnostics(
                // (27,17): warning CS0197: Passing 'ByRef.Instance' as ref or out or taking its address may cause a runtime exception because it is a field of a marshal-by-reference class
                //         Out(out b.Instance);
                Diagnostic(ErrorCode.WRN_ByRefNonAgileField, "b.Instance").WithArguments("ByRef.Instance"),
                // (28,17): warning CS0197: Passing 'ByRef.Instance' as ref or out or taking its address may cause a runtime exception because it is a field of a marshal-by-reference class
                //         Ref(ref b.Instance);
                Diagnostic(ErrorCode.WRN_ByRefNonAgileField, "b.Instance").WithArguments("ByRef.Instance"));
        }

        [WorkItem(530361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530361")]
        [Fact]
        public void CS0197WRN_ByRefNonAgileField_Receiver()
        {
            var text = @"
using System;

class ByRef : MarshalByRefObject
{
    public int F;

    protected void Ref(ref int x) { }

    void Test()
    {
        Ref(ref F);

        Ref(ref this.F);
        Ref(ref ((ByRef)this).F);
    }
}

class Derived : ByRef
{
    void Test()
    {
        Ref(ref F);

        Ref(ref this.F);
        Ref(ref ((ByRef)this).F);

        Ref(ref base.F);
        //Ref(ref ((ByRef)base).F);
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (15,17): warning CS0197: Passing 'ByRef.F' as ref or out or taking its address may cause a runtime exception because it is a field of a marshal-by-reference class
                //         Ref(ref ((ByRef)this).F);
                Diagnostic(ErrorCode.WRN_ByRefNonAgileField, "((ByRef)this).F").WithArguments("ByRef.F"),
                // (26,17): warning CS0197: Passing 'ByRef.F' as ref or out or taking its address may cause a runtime exception because it is a field of a marshal-by-reference class
                //         Ref(ref ((ByRef)this).F);
                Diagnostic(ErrorCode.WRN_ByRefNonAgileField, "((ByRef)this).F").WithArguments("ByRef.F"));
        }

        [Fact]
        public void CS0219WRN_UnreferencedVarAssg01()
        {
            var text = @"public class MyClass
{
    public static void Main()
    {
        int a = 0;   // CS0219
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
    // (5,13): warning CS0219: The variable 'a' is assigned but its value is never used
    //         int a = 0;   // CS0219
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "a").WithArguments("a")
                );
        }

        [Fact]
        public void CS0219WRN_UnreferencedVarAssg02()
        {
            var text = @"
public class clx
{
    static void Main(string[] args)
    {
        int x = 1;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(6, 13));
        }

        [Fact]
        public void CS0219WRN_UnreferencedVarAssg03()
        {
            var text = @"
public class clx
{
    static void Main(string[] args)
    {
        int? x;
        x = null;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(6, 14));
        }

        [Fact, WorkItem(542473, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542473"), WorkItem(542474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542474")]
        public void CS0219WRN_UnreferencedVarAssg_StructString()
        {
            var text = @"
class program
{
    static void Main(string[] args)
    {
        s1 y = new s1();
        string s = """";
    }
}
struct s1 { }
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,12): warning CS0219: The variable 'y' is assigned but its value is never used
                //         s1 y = new s1();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(6, 12),
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s").WithLocation(7, 16)
            );
        }

        [Fact, WorkItem(542494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542494")]
        public void CS0219WRN_UnreferencedVarAssg_Default()
        {
            var text = @"
class S
{
    public int x = 5;
}

class C
{
    public static void Main()
    {
        var x = default(S);
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,13): warning CS0219: The variable 'x' is assigned but its value is never used
                //         var x = default(S);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(11, 13)
            );
        }

        [Fact]
        public void CS0219WRN_UnreferencedVarAssg_For()
        {
            var text = @"
class C
{
    public static void Main()
    {
        for (int i = 1; ; )
        {
            break;
        }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,18): warning CS0219: The variable 'i' is assigned but its value is never used
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 18)
            );
        }

        [Fact, WorkItem(546619, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546619")]
        public void NoCS0219WRN_UnreferencedVarAssg_ObjectInitializer()
        {
            var text = @"
struct S
{
    public int X { set {} }
}
class C
{
    public static void Main()
    {
        S s = new S { X = 2 }; // no error - not a constant
        int? i = new int? { }; // ditto - not the default value (though bitwise equal to it)
    }
}";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact, WorkItem(542472, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542472")]
        public void CS0251WRN_NegativeArrayIndex()
        {
            var text = @"
class C
{
    static void Main()
    {
        int[] a = new int[1];
        int[,] b = new int[1, 1];
        a[-1] = 1; // CS0251
        a[-1, -1] = 1; // Dev10 reports CS0022 and CS0251 (twice), Roslyn reports CS0022
        b[-1] = 1; // CS0022
        b[-1, -1] = 1; // fine
    }
}
 ";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,11): warning CS0251: Indexing an array with a negative index (array indices always start at zero)
                Diagnostic(ErrorCode.WRN_NegativeArrayIndex, "-1"),
                // (9,9): error CS0022: Wrong number of indices inside []; expected '1'
                Diagnostic(ErrorCode.ERR_BadIndexCount, "a[-1, -1]").WithArguments("1"),
                // (10,9): error CS0022: Wrong number of indices inside []; expected '2'
                Diagnostic(ErrorCode.ERR_BadIndexCount, "b[-1]").WithArguments("2"));
        }

        [WorkItem(530362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530362"), WorkItem(670322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670322")]
        [Fact]
        public void CS0252WRN_BadRefCompareLeft()
        {
            var text =
@"class MyClass
{
   public static void Main()
   {
      string s = ""11"";
      object o = s + s;

      bool b = o == s;   // CS0252
   }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,16): warning CS0252: Possible unintended reference comparison; to get a value comparison, cast the left hand side to type 'string'
                //       bool b = o == s;   // CS0252
                Diagnostic(ErrorCode.WRN_BadRefCompareLeft, "o == s").WithArguments("string")
                );
        }

        [WorkItem(781070, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/781070")]
        [Fact]
        public void CS0252WRN_BadRefCompareLeft_02()
        {
            var text =
@"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b)
    {
        return ReferenceEquals(a, null) || ReferenceEquals(b, null) || ReferenceEquals(a, b);
    }
    public static bool operator !=(Symbol a, Symbol b)
    {
        return !(a == b);
    }
    public override bool Equals(object obj)
    {
        return (obj is Symbol || obj == null) ? this == (Symbol)obj : false;
    }
    public override int GetHashCode()
    {
        return 0;
    }
}

public class MethodSymbol : Symbol
{
}

class Program
{
    static void Main(string[] args)
    {
        MethodSymbol a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        // In these cases the programmer explicitly inserted a cast to use object equality instead
        // of the user-defined equality operator.  Since the programmer did this explicitly, in
        // Roslyn we suppress the diagnostic that was given by the native compiler suggesting casting
        // the object-typed operand back to type Symbol to get value equality.
        Console.WriteLine((object)a1 == a2);
        Console.WriteLine((object)a1 != a2);
        Console.WriteLine((object)a2 == a1);
        Console.WriteLine((object)a2 != a1);

        Console.WriteLine(a1 == (object)a2);
        Console.WriteLine(a1 != (object)a2);
        Console.WriteLine(a2 == (object)a1);
        Console.WriteLine(a2 != (object)a1);
    }
}";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [WorkItem(781070, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/781070")]
        [Fact]
        public void CS0252WRN_BadRefCompareLeft_03()
        {
            var text =
@"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b)
    {
        return ReferenceEquals(a, null) || ReferenceEquals(b, null) || ReferenceEquals(a, b);
    }
    public static bool operator !=(Symbol a, Symbol b)
    {
        return !(a == b);
    }
    public override bool Equals(object obj)
    {
        return (obj is Symbol || obj == null) ? this == (Symbol)obj : false;
    }
    public override int GetHashCode()
    {
        return 0;
    }
}

public class MethodSymbol : Symbol
{
}

class Program
{
    static void Main(string[] args)
    {
        Object a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine(a1 == a2);
        Console.WriteLine(a1 != a2);
        Console.WriteLine(a2 == a1);
        Console.WriteLine(a2 != a1);
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (34,27): warning CS0252: Possible unintended reference comparison; to get a value comparison, cast the left hand side to type 'Symbol'
                //         Console.WriteLine(a1 == a2);
                Diagnostic(ErrorCode.WRN_BadRefCompareLeft, "a1 == a2").WithArguments("Symbol"),
                // (35,27): warning CS0252: Possible unintended reference comparison; to get a value comparison, cast the left hand side to type 'Symbol'
                //         Console.WriteLine(a1 != a2);
                Diagnostic(ErrorCode.WRN_BadRefCompareLeft, "a1 != a2").WithArguments("Symbol"),
                // (36,27): warning CS0253: Possible unintended reference comparison; to get a value comparison, cast the right hand side to type 'Symbol'
                //         Console.WriteLine(a2 == a1);
                Diagnostic(ErrorCode.WRN_BadRefCompareRight, "a2 == a1").WithArguments("Symbol"),
                // (37,27): warning CS0253: Possible unintended reference comparison; to get a value comparison, cast the right hand side to type 'Symbol'
                //         Console.WriteLine(a2 != a1);
                Diagnostic(ErrorCode.WRN_BadRefCompareRight, "a2 != a1").WithArguments("Symbol")
                );
        }

        [WorkItem(530362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530362"), WorkItem(670322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670322")]
        [Fact]
        public void CS0253WRN_BadRefCompareRight()
        {
            var text =
@"
class MyClass
{
   public static void Main()
   {
      string s = ""11"";
      object o = s + s;

      bool c = s == o;   // CS0253
      // try the following line instead
      // bool c = s == (string)o;
   }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,16): warning CS0253: Possible unintended reference comparison; to get a value comparison, cast the right hand side to type 'string'
                //       bool c = s == o;   // CS0253
                Diagnostic(ErrorCode.WRN_BadRefCompareRight, "s == o").WithArguments("string")
                );
        }

        [WorkItem(730177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/730177")]
        [Fact]
        public void CS0253WRN_BadRefCompare_None()
        {
            var text =
@"using System;
class MyClass
{
    public static void Main()
    {
        MulticastDelegate x1 = null;
        bool b1 = x1 == null;
        bool b2 = x1 != null;
        bool b3 = null == x1;
        bool b4 = null != x1;
    }
}";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [WorkItem(542399, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542399")]
        [Fact]
        public void CS0278WRN_PatternIsAmbiguous01()
        {
            var text = @"
using System.Collections.Generic;
public class myTest 
{
   public static void TestForeach<W>(W w) 
      where W: IEnumerable<int>, IEnumerable<string>
   {
      foreach (int i in w) {}   // CS0278
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,25): warning CS0278: 'W' does not implement the 'collection' pattern. 'System.Collections.Generic.IEnumerable<int>.GetEnumerator()' is ambiguous with 'System.Collections.Generic.IEnumerable<string>.GetEnumerator()'.
                //       foreach (int i in w) {}   // CS0278
                Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "w").WithArguments("W", "collection", "System.Collections.Generic.IEnumerable<int>.GetEnumerator()", "System.Collections.Generic.IEnumerable<string>.GetEnumerator()"),
                // (8,25): error CS1640: foreach statement cannot operate on variables of type 'W' because it implements multiple instantiations of 'System.Collections.Generic.IEnumerable<T>'; try casting to a specific interface instantiation
                //       foreach (int i in w) {}   // CS0278
                Diagnostic(ErrorCode.ERR_MultipleIEnumOfT, "w").WithArguments("W", "System.Collections.Generic.IEnumerable<T>"));
        }

        [Fact]
        public void CS0278WRN_PatternIsAmbiguous02()
        {
            var text =
@"using System.Collections;
using System.Collections.Generic;
class A : IEnumerable<A>
{
    public IEnumerator<A> GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
class B : IEnumerable<B>
{
    IEnumerator<B> IEnumerable<B>.GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
class C : IEnumerable<C>, IEnumerable<string>
{
    public IEnumerator<C> GetEnumerator() { return null; }
    IEnumerator<string> IEnumerable<string>.GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
class D : IEnumerable<D>, IEnumerable<string>
{
    IEnumerator<D> IEnumerable<D>.GetEnumerator() { return null; }
    IEnumerator<string> IEnumerable<string>.GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
class E
{
    static void M<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10)
        where T1 : A, IEnumerable<A> // duplicate interfaces
        where T2 : B, IEnumerable<B> // duplicate interfaces
        where T3 : A, IEnumerable<string>, IEnumerable<int> // multiple interfaces
        where T4 : B, IEnumerable<string>, IEnumerable<int> // multiple interfaces
        where T5 : C, IEnumerable<int> // multiple interfaces
        where T6 : D, IEnumerable<int> // multiple interfaces
        where T7 : A, IEnumerable<string>, IEnumerable<A> // duplicate and multiple interfaces
        where T8 : B, IEnumerable<string>, IEnumerable<B> // duplicate and multiple interfaces
        where T9 : C, IEnumerable<C> // duplicate and multiple interfaces
        where T10 : D, IEnumerable<D> // duplicate and multiple interfaces
    {
        foreach (A o in t1) { }
        foreach (B o in t2) { }
        foreach (A o in t3) { }
        foreach (var o in t4) { }
        foreach (C o in t5) { }
        foreach (int o in t6) { }
        foreach (A o in t7) { }
        foreach (var o in t8) { }
        foreach (C o in t9) { }
        foreach (D o in t10) { }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (42,27): warning CS0278: 'T4' does not implement the 'collection' pattern. 'System.Collections.Generic.IEnumerable<string>.GetEnumerator()' is ambiguous with 'System.Collections.Generic.IEnumerable<int>.GetEnumerator()'.
                Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "t4").WithArguments("T4", "collection", "System.Collections.Generic.IEnumerable<string>.GetEnumerator()", "System.Collections.Generic.IEnumerable<int>.GetEnumerator()").WithLocation(42, 27),
                // (42,27): error CS1640: foreach statement cannot operate on variables of type 'T4' because it implements multiple instantiations of 'System.Collections.Generic.IEnumerable<T>'; try casting to a specific interface instantiation
                Diagnostic(ErrorCode.ERR_MultipleIEnumOfT, "t4").WithArguments("T4", "System.Collections.Generic.IEnumerable<T>").WithLocation(42, 27),
                // (46,27): warning CS0278: 'T8' does not implement the 'collection' pattern. 'System.Collections.Generic.IEnumerable<string>.GetEnumerator()' is ambiguous with 'System.Collections.Generic.IEnumerable<B>.GetEnumerator()'.
                Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "t8").WithArguments("T8", "collection", "System.Collections.Generic.IEnumerable<string>.GetEnumerator()", "System.Collections.Generic.IEnumerable<B>.GetEnumerator()").WithLocation(46, 27),
                // (46,27): error CS1640: foreach statement cannot operate on variables of type 'T8' because it implements multiple instantiations of 'System.Collections.Generic.IEnumerable<T>'; try casting to a specific interface instantiation
                Diagnostic(ErrorCode.ERR_MultipleIEnumOfT, "t8").WithArguments("T8", "System.Collections.Generic.IEnumerable<T>").WithLocation(46, 27));
        }

        [Fact]
        public void CS0279WRN_PatternStaticOrInaccessible()
        {
            var text = @"
using System.Collections;

public class myTest : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator()
    {
        return null;
    }

    internal IEnumerator GetEnumerator()
    {
        return null;
    }

    public static void Main()
    {
        foreach (int i in new myTest()) {}  // CS0279
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (18,27): warning CS0279: 'myTest' does not implement the 'collection' pattern. 'myTest.GetEnumerator()' is either static or not public.
                Diagnostic(ErrorCode.WRN_PatternStaticOrInaccessible, "new myTest()").WithArguments("myTest", "collection", "myTest.GetEnumerator()"));
        }

        [Fact]
        public void CS0280WRN_PatternBadSignature()
        {
            var text = @"
using System.Collections;

public class ValidBase: IEnumerable
{
   IEnumerator IEnumerable.GetEnumerator()
   {
        return null;
   }

   internal IEnumerator GetEnumerator()
   {
        return null;
   }
}

class Derived : ValidBase
{
   // field, not method
   new public int GetEnumerator;
}

public class Test
{
   public static void Main()
   {
      foreach (int i in new Derived()) {}   // CS0280
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (27,25): warning CS0280: 'Derived' does not implement the 'collection' pattern. 'Derived.GetEnumerator' has the wrong signature.
                //       foreach (int i in new Derived()) {}   // CS0280
                Diagnostic(ErrorCode.WRN_PatternBadSignature, "new Derived()").WithArguments("Derived", "collection", "Derived.GetEnumerator"),
                // (20,19): warning CS0649: Field 'Derived.GetEnumerator' is never assigned to, and will always have its default value 0
                //    new public int GetEnumerator;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "GetEnumerator").WithArguments("Derived.GetEnumerator", "0")
                );
        }

        [Fact]
        public void CS0414WRN_UnreferencedFieldAssg()
        {
            var text = @"
class C
{
   private int i = 1;  // CS0414

   public static void Main()
   { }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,16): warning CS0414: The field 'C.i' is assigned but its value is never used
                //    private int i = 1;  // CS0414
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i").WithArguments("C.i")
                );
        }

        [Fact]
        public void CS0414WRN_UnreferencedFieldAssg02()
        {
            var text =
@"class S<T1, T2>
{
    T1 t1_field = default(T1);
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (3,8): warning CS0414: The field 'S<T1, T2>.t1_field' is assigned but its value is never used
                //     T1 t1_field = default(T1);
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "t1_field").WithArguments("S<T1, T2>.t1_field").WithLocation(3, 8)
                );
        }

        [Fact]
        public void CS0419WRN_AmbiguousXMLReference()
        {
            var text = @"
interface I
{
   void F();
   void F(int i);
}
public class MyClass
{
   /// <see cref=""I.F""/>
   public static void MyMethod(int i)
   {
   }
   public static void Main ()
   {
   }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (7,14): warning CS1591: Missing XML comment for publicly visible type or member 'MyClass'
                // public class MyClass
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "MyClass").WithArguments("MyClass"),
                // (9,19): warning CS0419: Ambiguous reference in cref attribute: 'I.F'. Assuming 'I.F()', but could have also matched other overloads including 'I.F(int)'.
                //    /// <see cref="I.F"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "I.F").WithArguments("I.F", "I.F()", "I.F(int)"),
                // (13,23): warning CS1591: Missing XML comment for publicly visible type or member 'MyClass.Main()'
                //    public static void Main ()
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Main").WithArguments("MyClass.Main()"));
        }

        [Fact]
        public void CS0420WRN_VolatileByRef()
        {
            var text = @"
class TestClass
{
   private volatile int i;

   public void TestVolatileRef(ref int ii)
   {
   }

   public void TestVolatileOut(out int ii)
   {
        ii = 0;
   }

   public static void Main()
   {
      TestClass x = new TestClass();
      x.TestVolatileRef(ref x.i);   // CS0420 
      x.TestVolatileOut(out x.i);   // CS0420 
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (18,29): warning CS0420: 'TestClass.i': a reference to a volatile field will not be treated as volatile
                Diagnostic(ErrorCode.WRN_VolatileByRef, "x.i").WithArguments("TestClass.i"),
                // (19,29): warning CS0420: 'TestClass.i': a reference to a volatile field will not be treated as volatile
                Diagnostic(ErrorCode.WRN_VolatileByRef, "x.i").WithArguments("TestClass.i"));
        }

        [Fact]
        public void CS0420WRN_VolatileByRef_Suppressed()
        {
            var text = @"
using System.Threading;

class TestClass
{
   private static volatile int x = 0;

   public static void TestVolatileByRef()
   {
      Interlocked.Increment(ref x);                             // no CS0420      
      Interlocked.Decrement(ref x);                             // no CS0420
      Interlocked.Add(ref x, 0);                                // no CS0420
      Interlocked.CompareExchange(ref x, 0, 0);                 // no CS0420
      Interlocked.Exchange(ref x, 0);                           // no CS0420

      // using fully qualified name
      System.Threading.Interlocked.Increment(ref x);             // no CS0420      
      System.Threading.Interlocked.Decrement(ref x);             // no CS0420
      System.Threading.Interlocked.Add(ref x, 0);                // no CS0420
      System.Threading.Interlocked.CompareExchange(ref x, 0, 0); // no CS0420
      System.Threading.Interlocked.Exchange(ref x, 0);           // no CS0420

      // passing volatile variables in a nested way
      Interlocked.Increment(ref Method1(ref x).y);              // CS0420 for x     
      Interlocked.Decrement(ref Method1(ref x).y);              // CS0420 for x
      Interlocked.Add(ref Method1(ref x).y, 0);                 // CS0420 for x
      Interlocked.CompareExchange(ref Method1(ref x).y, 0, 0);  // CS0420 for x
      Interlocked.Exchange(ref Method1(ref x).y, 0);            // CS0420 for x

      // located as a function argument 
      goo(Interlocked.Increment(ref x));                        // no CS0420

   }

   public static int goo(int x)
   {
      return x; 
   } 

   public static MyClass Method1(ref int x)
   {
       return new MyClass();
   }

   public class MyClass
   {
        public volatile int y = 0;
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (24,45): warning CS0420: 'TestClass.i': a reference to a volatile field will not be treated as volatile
                Diagnostic(ErrorCode.WRN_VolatileByRef, "x").WithArguments("TestClass.x"),
                // (25,45): warning CS0420: 'TestClass.i': a reference to a volatile field will not be treated as volatile
                Diagnostic(ErrorCode.WRN_VolatileByRef, "x").WithArguments("TestClass.x"),
                // (26,39): warning CS0420: 'TestClass.i': a reference to a volatile field will not be treated as volatile
                Diagnostic(ErrorCode.WRN_VolatileByRef, "x").WithArguments("TestClass.x"),
                // (27,51): warning CS0420: 'TestClass.i': a reference to a volatile field will not be treated as volatile
                Diagnostic(ErrorCode.WRN_VolatileByRef, "x").WithArguments("TestClass.x"),
                // (28,44): warning CS0420: 'TestClass.i': a reference to a volatile field will not be treated as volatile
                Diagnostic(ErrorCode.WRN_VolatileByRef, "x").WithArguments("TestClass.x"));
        }

        [WorkItem(728380, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/728380")]
        [Fact]
        public void Repro728380()
        {
            var source = @"
class Test
{
    static volatile int x;
    unsafe static void goo(int* pX) { }

    static int Main()
    {
        unsafe { Test.goo(&x); }
        return 1;
    }
}
";
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,27): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         unsafe { Test.goo(&x); }
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x"),
                // (9,28): warning CS0420: 'Test.x': a reference to a volatile field will not be treated as volatile
                //         unsafe { Test.goo(&x); }
                Diagnostic(ErrorCode.WRN_VolatileByRef, "x").WithArguments("Test.x"));
        }

        [Fact, WorkItem(528275, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528275")]
        public void CS0429WRN_UnreachableExpr()
        {
            var text = @"
public class cs0429 
{
    public static void Main() 
    {
        if (false && myTest())  // CS0429
        // Try the following line instead:
        // if (true && myTest())
        {
        }
        else
        {
            int i = 0;
            i++;
        }
    }

    static bool myTest() { return true; }
}
";
            // Dev11 compiler reports WRN_UnreachableExpr, but reachability is defined for statements not for expressions.
            // We don't report the warning.
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact, WorkItem(528275, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528275"), WorkItem(530071, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530071")]
        public void CS0429WRN_UnreachableExpr_02()
        {
            var text = @"
class Program
{
    static bool b = true;
    const bool con = true;
    static void Main(string[] args)
    {
        int x = 1;
        int y = 1;
        int s = true ? x++ : y++;    // y++ unreachable
        s = x == y ? x++ : y++;    // OK
        s = con ? x++ : y++;    // y++ unreachable
        bool con1 = true;
        s = con1 ? x++ : y++;    // OK
        s = b ? x++ : y++;
        s = 1 < 2 ? x++ : y++; 	// y++ unreachable
    }
}
";
            // Dev11 compiler reports WRN_UnreachableExpr, but reachability is defined for statements not for expressions.
            // We don't report the warning.
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact, WorkItem(543943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543943")]
        public void CS0458WRN_AlwaysNull()
        {
            var text = @"
public class Test 
{
    public static void Main()
    {
        int? x = 0;

        x = null + x;
        x = x + default(int?);
        x += new int?();

        x = null - x;
        x = x - default(int?);
        x -= new int?();

        x = null * x;
        x = x * default(int?);
        x *= new int?();

        x = null / x;
        x = x / default(int?);
        x /= new int?();

        x = null % x;
        x = x % default(int?);
        x %= new int?();

        x = null << x;
        x = x << default(int?);
        x <<= new int?();

        x = null >> x;
        x = x >> default(int?);
        x >>= new int?();
        
        x = null & x;
        x = x & default(int?);
        x &= new int?();

        x = null | x;
        x = x | default(int?);
        x |= new int?();

        x = null ^ x;
        x = x ^ default(int?);
        x ^= new int?();

        //The below block of code should not raise a warning
        bool? y = null;
        y = y & null;
        y = y |false;
        y = true | null;

        double? d = +default(double?);
        int? i = -default(int?);
        long? l = ~default(long?);
        bool? b = !default(bool?);

    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_AlwaysNull, "null + x").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x + default(int?)").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x += new int?()").WithArguments("int?"),

                Diagnostic(ErrorCode.WRN_AlwaysNull, "null - x").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x - default(int?)").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x -= new int?()").WithArguments("int?"),

                Diagnostic(ErrorCode.WRN_AlwaysNull, "null * x").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x * default(int?)").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x *= new int?()").WithArguments("int?"),

                Diagnostic(ErrorCode.WRN_AlwaysNull, "null / x").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x / default(int?)").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x /= new int?()").WithArguments("int?"),

                Diagnostic(ErrorCode.WRN_AlwaysNull, "null % x").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x % default(int?)").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x %= new int?()").WithArguments("int?"),

                Diagnostic(ErrorCode.WRN_AlwaysNull, "null << x").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x << default(int?)").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x <<= new int?()").WithArguments("int?"),

                Diagnostic(ErrorCode.WRN_AlwaysNull, "null >> x").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x >> default(int?)").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x >>= new int?()").WithArguments("int?"),

                Diagnostic(ErrorCode.WRN_AlwaysNull, "null & x").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x & default(int?)").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x &= new int?()").WithArguments("int?"),

                Diagnostic(ErrorCode.WRN_AlwaysNull, "null | x").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x | default(int?)").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x |= new int?()").WithArguments("int?"),

                Diagnostic(ErrorCode.WRN_AlwaysNull, "null ^ x").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x ^ default(int?)").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "x ^= new int?()").WithArguments("int?"),

                Diagnostic(ErrorCode.WRN_AlwaysNull, "+default(double?)").WithArguments("double?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "-default(int?)").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "~default(long?)").WithArguments("long?"),
                Diagnostic(ErrorCode.WRN_AlwaysNull, "!default(bool?)").WithArguments("bool?")
                );
        }

        [Fact]
        public void CS0464WRN_CmpAlwaysFalse()
        {
            var text = @"
class MyClass
{
    public struct S
    {
        public static bool operator <(S x, S y) { return true; } 
        public static bool operator >(S x, S y) { return true; }
        public static bool operator <=(S x, S y) { return true; } 
        public static bool operator >=(S x, S y) { return true; }

    }

    public static void W(bool b)
    {
        System.Console.Write(b ? 't' : 'f');
    }

    public static void Main()
    {
        S s = default(S);
        S? t = s;
        int i = 0;
        int? n = i;

        W(i < null);             // CS0464
        W(i <= null);            // CS0464
        W(i > null);             // CS0464
        W(i >= null);            // CS0464

        W(n < null);             // CS0464
        W(n <= null);            // CS0464
        W(n > null);             // CS0464
        W(n >= null);            // CS0464

        W(s < null);             // CS0464
        W(s <= null);            // CS0464
        W(s > null);             // CS0464
        W(s >= null);            // CS0464

        W(t < null);             // CS0464
        W(t <= null);            // CS0464
        W(t > null);             // CS0464
        W(t >= null);            // CS0464

        W(i < default(short?));             // CS0464
        W(i <= default(short?));            // CS0464
        W(i > default(short?));             // CS0464
        W(i >= default(short?));            // CS0464

        W(n < default(short?));             // CS0464
        W(n <= default(short?));            // CS0464
        W(n > default(short?));             // CS0464
        W(n >= default(short?));            // CS0464

        W(s < default(S?));             // CS0464
        W(s <= default(S?));            // CS0464
        W(s > default(S?));             // CS0464
        W(s >= default(S?));            // CS0464

        W(t < default(S?));             // CS0464
        W(t <= default(S?));            // CS0464
        W(t > default(S?));             // CS0464
        W(t >= default(S?));            // CS0464

        W(i < new sbyte?());             // CS0464
        W(i <= new sbyte?());            // CS0464
        W(i > new sbyte?());             // CS0464
        W(i >= new sbyte?());            // CS0464

        W(n < new sbyte?());             // CS0464
        W(n <= new sbyte?());            // CS0464
        W(n > new sbyte?());             // CS0464
        W(n >= new sbyte?());            // CS0464

        W(s < new S?());             // CS0464
        W(s <= new S?());            // CS0464
        W(s > new S?());             // CS0464
        W(s >= new S?());            // CS0464

        W(t < new S?());             // CS0464
        W(t <= new S?());            // CS0464
        W(t > new S?());             // CS0464
        W(t >= new S?());            // CS0464

        System.Console.WriteLine();

        W(null < i);             // CS0464
        W(null <= i);            // CS0464
        W(null > i);             // CS0464
        W(null >= i);            // CS0464

        W(null < n);             // CS0464
        W(null <= n);            // CS0464
        W(null > n);             // CS0464
        W(null >= n);            // CS0464

        W(null < s);             // CS0464
        W(null <= s);            // CS0464
        W(null > s);             // CS0464
        W(null >= s);            // CS0464

        W(null < t);             // CS0464
        W(null <= t);            // CS0464
        W(null > t);             // CS0464
        W(null >= t);            // CS0464

        W(default(short?) < i);             // CS0464
        W(default(short?) <= i);            // CS0464
        W(default(short?) > i);             // CS0464
        W(default(short?) >= i);            // CS0464

        W(default(short?) < n);             // CS0464
        W(default(short?) <= n);            // CS0464
        W(default(short?) > n);             // CS0464
        W(default(short?) >= n);            // CS0464

        W(default(S?) < s);             // CS0464
        W(default(S?) <= s);            // CS0464
        W(default(S?) > s);             // CS0464
        W(default(S?) >= s);            // CS0464

        W(default(S?) < t);             // CS0464
        W(default(S?) <= t);            // CS0464
        W(default(S?) > t);             // CS0464
        W(default(S?) >= t);            // CS0464

        W(new sbyte?() < i);             // CS0464
        W(new sbyte?() <= i);            // CS0464
        W(new sbyte?() > i);             // CS0464
        W(new sbyte?() >= i);            // CS0464

        W(new sbyte?() < n);             // CS0464
        W(new sbyte?() <= n);            // CS0464
        W(new sbyte?() > n);             // CS0464
        W(new sbyte?() > n);             // CS0464

        W(new S?() < s);             // CS0464
        W(new S?() <= s);            // CS0464
        W(new S?() > s);             // CS0464
        W(new S?() >= s);            // CS0464

        W(new S?() < t);             // CS0464
        W(new S?() <= t);            // CS0464
        W(new S?() > t);             // CS0464
        W(new S?() > t);             // CS0464

        System.Console.WriteLine();

        W(null > null);             // CS0464
        W(null >= null);            // CS0464
        W(null < null);             // CS0464
        W(null <= null);            // CS0464
   }
}
";
            var verifier = CompileAndVerify(source: text, expectedOutput: @"ffffffffffffffffffffffffffffffffffffffffffffffff
ffffffffffffffffffffffffffffffffffffffffffffffff
ffff");

            CreateCompilation(text).VerifyDiagnostics(
                // (25,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(i < null);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i < null").WithArguments("int?"),
                // (26,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(i <= null);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i <= null").WithArguments("int?"),
                // (27,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(i > null);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i > null").WithArguments("int?"),
                // (28,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(i >= null);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i >= null").WithArguments("int?"),
                // (30,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(n < null);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n < null").WithArguments("int?"),
                // (31,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(n <= null);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n <= null").WithArguments("int?"),
                // (32,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(n > null);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n > null").WithArguments("int?"),
                // (33,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(n >= null);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n >= null").WithArguments("int?"),
                // (35,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s < null);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s < null").WithArguments("MyClass.S?"),
                // (36,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s <= null);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s <= null").WithArguments("MyClass.S?"),
                // (37,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s > null);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s > null").WithArguments("MyClass.S?"),
                // (38,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s >= null);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s >= null").WithArguments("MyClass.S?"),
                // (40,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t < null);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t < null").WithArguments("MyClass.S?"),
                // (41,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t <= null);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t <= null").WithArguments("MyClass.S?"),
                // (42,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t > null);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t > null").WithArguments("MyClass.S?"),
                // (43,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t >= null);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t >= null").WithArguments("MyClass.S?"),
                // (45,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(i < default(short?));             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i < default(short?)").WithArguments("short?"),
                // (46,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(i <= default(short?));            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i <= default(short?)").WithArguments("short?"),
                // (47,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(i > default(short?));             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i > default(short?)").WithArguments("short?"),
                // (48,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(i >= default(short?));            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i >= default(short?)").WithArguments("short?"),
                // (50,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(n < default(short?));             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n < default(short?)").WithArguments("short?"),
                // (51,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(n <= default(short?));            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n <= default(short?)").WithArguments("short?"),
                // (52,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(n > default(short?));             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n > default(short?)").WithArguments("short?"),
                // (53,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(n >= default(short?));            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n >= default(short?)").WithArguments("short?"),
                // (55,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s < default(S?));             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s < default(S?)").WithArguments("MyClass.S?"),
                // (56,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s <= default(S?));            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s <= default(S?)").WithArguments("MyClass.S?"),
                // (57,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s > default(S?));             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s > default(S?)").WithArguments("MyClass.S?"),
                // (58,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s >= default(S?));            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s >= default(S?)").WithArguments("MyClass.S?"),
                // (60,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t < default(S?));             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t < default(S?)").WithArguments("MyClass.S?"),
                // (61,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t <= default(S?));            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t <= default(S?)").WithArguments("MyClass.S?"),
                // (62,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t > default(S?));             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t > default(S?)").WithArguments("MyClass.S?"),
                // (63,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t >= default(S?));            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t >= default(S?)").WithArguments("MyClass.S?"),
                // (65,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(i < new sbyte?());             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i < new sbyte?()").WithArguments("sbyte?"),
                // (66,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(i <= new sbyte?());            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i <= new sbyte?()").WithArguments("sbyte?"),
                // (67,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(i > new sbyte?());             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i > new sbyte?()").WithArguments("sbyte?"),
                // (68,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(i >= new sbyte?());            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "i >= new sbyte?()").WithArguments("sbyte?"),
                // (70,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(n < new sbyte?());             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n < new sbyte?()").WithArguments("sbyte?"),
                // (71,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(n <= new sbyte?());            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n <= new sbyte?()").WithArguments("sbyte?"),
                // (72,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(n > new sbyte?());             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n > new sbyte?()").WithArguments("sbyte?"),
                // (73,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(n >= new sbyte?());            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "n >= new sbyte?()").WithArguments("sbyte?"),
                // (75,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s < new S?());             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s < new S?()").WithArguments("MyClass.S?"),
                // (76,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s <= new S?());            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s <= new S?()").WithArguments("MyClass.S?"),
                // (77,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s > new S?());             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s > new S?()").WithArguments("MyClass.S?"),
                // (78,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(s >= new S?());            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "s >= new S?()").WithArguments("MyClass.S?"),
                // (80,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t < new S?());             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t < new S?()").WithArguments("MyClass.S?"),
                // (81,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t <= new S?());            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t <= new S?()").WithArguments("MyClass.S?"),
                // (82,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t > new S?());             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t > new S?()").WithArguments("MyClass.S?"),
                // (83,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(t >= new S?());            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "t >= new S?()").WithArguments("MyClass.S?"),
                // (87,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null < i);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null < i").WithArguments("int?"),
                // (88,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null <= i);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null <= i").WithArguments("int?"),
                // (89,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null > i);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null > i").WithArguments("int?"),
                // (90,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null >= i);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null >= i").WithArguments("int?"),
                // (92,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null < n);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null < n").WithArguments("int?"),
                // (93,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null <= n);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null <= n").WithArguments("int?"),
                // (94,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null > n);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null > n").WithArguments("int?"),
                // (95,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null >= n);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null >= n").WithArguments("int?"),
                // (97,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(null < s);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null < s").WithArguments("MyClass.S?"),
                // (98,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(null <= s);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null <= s").WithArguments("MyClass.S?"),
                // (99,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(null > s);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null > s").WithArguments("MyClass.S?"),
                // (100,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(null >= s);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null >= s").WithArguments("MyClass.S?"),
                // (102,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(null < t);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null < t").WithArguments("MyClass.S?"),
                // (103,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(null <= t);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null <= t").WithArguments("MyClass.S?"),
                // (104,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(null > t);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null > t").WithArguments("MyClass.S?"),
                // (105,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(null >= t);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null >= t").WithArguments("MyClass.S?"),
                // (107,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(default(short?) < i);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(short?) < i").WithArguments("short?"),
                // (108,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(default(short?) <= i);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(short?) <= i").WithArguments("short?"),
                // (109,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(default(short?) > i);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(short?) > i").WithArguments("short?"),
                // (110,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(default(short?) >= i);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(short?) >= i").WithArguments("short?"),
                // (112,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(default(short?) < n);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(short?) < n").WithArguments("short?"),
                // (113,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(default(short?) <= n);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(short?) <= n").WithArguments("short?"),
                // (114,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(default(short?) > n);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(short?) > n").WithArguments("short?"),
                // (115,11): warning CS0464: Comparing with null of type 'short?' always produces 'false'
                //         W(default(short?) >= n);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(short?) >= n").WithArguments("short?"),
                // (117,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(default(S?) < s);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(S?) < s").WithArguments("MyClass.S?"),
                // (118,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(default(S?) <= s);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(S?) <= s").WithArguments("MyClass.S?"),
                // (119,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(default(S?) > s);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(S?) > s").WithArguments("MyClass.S?"),
                // (120,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(default(S?) >= s);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(S?) >= s").WithArguments("MyClass.S?"),
                // (122,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(default(S?) < t);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(S?) < t").WithArguments("MyClass.S?"),
                // (123,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(default(S?) <= t);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(S?) <= t").WithArguments("MyClass.S?"),
                // (124,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(default(S?) > t);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(S?) > t").WithArguments("MyClass.S?"),
                // (125,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(default(S?) >= t);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "default(S?) >= t").WithArguments("MyClass.S?"),
                // (127,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(new sbyte?() < i);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new sbyte?() < i").WithArguments("sbyte?"),
                // (128,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(new sbyte?() <= i);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new sbyte?() <= i").WithArguments("sbyte?"),
                // (129,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(new sbyte?() > i);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new sbyte?() > i").WithArguments("sbyte?"),
                // (130,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(new sbyte?() >= i);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new sbyte?() >= i").WithArguments("sbyte?"),
                // (132,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(new sbyte?() < n);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new sbyte?() < n").WithArguments("sbyte?"),
                // (133,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(new sbyte?() <= n);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new sbyte?() <= n").WithArguments("sbyte?"),
                // (134,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(new sbyte?() > n);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new sbyte?() > n").WithArguments("sbyte?"),
                // (135,11): warning CS0464: Comparing with null of type 'sbyte?' always produces 'false'
                //         W(new sbyte?() > n);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new sbyte?() > n").WithArguments("sbyte?"),
                // (137,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(new S?() < s);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new S?() < s").WithArguments("MyClass.S?"),
                // (138,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(new S?() <= s);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new S?() <= s").WithArguments("MyClass.S?"),
                // (139,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(new S?() > s);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new S?() > s").WithArguments("MyClass.S?"),
                // (140,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(new S?() >= s);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new S?() >= s").WithArguments("MyClass.S?"),
                // (142,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(new S?() < t);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new S?() < t").WithArguments("MyClass.S?"),
                // (143,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(new S?() <= t);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new S?() <= t").WithArguments("MyClass.S?"),
                // (144,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(new S?() > t);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new S?() > t").WithArguments("MyClass.S?"),
                // (145,11): warning CS0464: Comparing with null of type 'MyClass.S?' always produces 'false'
                //         W(new S?() > t);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "new S?() > t").WithArguments("MyClass.S?"),
                // (149,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null > null);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null > null").WithArguments("int?"),
                // (150,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null >= null);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null >= null").WithArguments("int?"),
                // (151,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null < null);             // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null < null").WithArguments("int?"),
                // (152,11): warning CS0464: Comparing with null of type 'int?' always produces 'false'
                //         W(null <= null);            // CS0464
                Diagnostic(ErrorCode.WRN_CmpAlwaysFalse, "null <= null").WithArguments("int?")
                );
        }

        [Fact]
        public void CS0469WRN_GotoCaseShouldConvert()
        {
            var text = @"
class Test
{
   static void Main()
   {
      char c = (char)180;
      switch (c)
      {
         case (char)127:
            break;

         case (char)180: 
            goto case 127;   // CS0469
            // try the following line instead
            // goto case (char) 127;
      }
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (13,13): warning CS0469: The 'goto case' value is not implicitly convertible to type 'char'
                //             goto case 127;   // CS0469
                Diagnostic(ErrorCode.WRN_GotoCaseShouldConvert, "goto case 127;").WithArguments("char").WithLocation(13, 13)
                );
        }

        [Fact, WorkItem(663, "https://github.com/dotnet/roslyn/issues/663")]
        public void CS0472WRN_NubExprIsConstBool()
        {
            // Due to a long-standing bug, the native compiler does not produce warnings for "guid == null",
            // but does for "int == null". Roslyn corrects this lapse and produces warnings for both built-in
            // and user-defined lifted equality operators, but the new warnings for user-defined types are
            // only given in "strict" more.

            var text = @"
using System;
class MyClass
{
    public static void W(bool b)
    {
        System.Console.Write(b ? 't' : 'f');
    }

    enum E : int { };

    public static void Main()
    {
        Guid g = default(Guid);
        Guid? h = g;
        int i = 0;
        int? n = i;

        W(i == null);            // CS0472
        W(i != null);            // CS0472
        W(n == null);            // no error
        W(n != null);            // no error
        W(g == null);            // CS0472
        W(g != null);            // CS0472
        W(h == null);            // no error
        W(h != null);            // no error

        W(i == default(short?));            // CS0472
        W(i != default(short?));            // CS0472
        W(n == default(short?));            // no error
        W(n != default(short?));            // no error
        W(g == default(Guid?));             // CS0472
        W(g != default(Guid?));             // CS0472
        W(h == default(Guid?));             // no error
        W(h != default(Guid?));             // no error

        W(i == new sbyte?());            // CS0472
        W(i != new sbyte?());            // CS0472
        W(n == new sbyte?());            // no error
        W(n != new sbyte?());            // no error
        W(g == new Guid?());             // CS0472
        W(g != new Guid?());             // CS0472
        W(h == new Guid?());             // no error
        W(h != new Guid?());             // no error

        System.Console.WriteLine();


        W(null == i);            // CS0472
        W(null != i);            // CS0472
        W(null == n);            // no error
        W(null != n);            // no error
        W(null == g);            // CS0472
        W(null != g);            // CS0472
        W(null == h);            // no error
        W(null != h);            // no error

        W(default(long?) == i);            // CS0472
        W(default(long?) != i);            // CS0472
        W(default(long?) == n);            // no error
        W(default(long?) != n);            // no error
        W(default(Guid?) == g);            // CS0472
        W(default(Guid?) != g);            // CS0472
        W(default(Guid?) == h);            // no error
        W(default(Guid?) != h);            // no error

        W(new double?() == i);            // CS0472
        W(new double?() != i);            // CS0472
        W(new double?() == n);            // no error
        W(new double?() != n);            // no error
        W(new Guid?() == g);              // CS0472
        W(new Guid?() != g);              // CS0472
        W(new Guid?() == h);              // no error
        W(new Guid?() != h);              // no error

        System.Console.WriteLine();

        W(null == null);                  // No error, because both sides are nullable, but of course
        W(null != null);                  // we could give a warning here as well.

        System.Console.WriteLine();

        //check comparisons with converted constants
        W((E?)1 == null);
        W(null != (E?)1);

        W((int?)1 == null);
        W(null != (int?)1);

        //check comparisons when null is converted 

        W(0 == (int?)null);
        W((int?)null != 0);
 
        W(0 == (E?)null);
        W((E?)null != 0);
    }
}
";

            string expected = @"ftftftftftftftftftftftft
ftftftftftftftftftftftft
tf
ftftftft";
            var fullExpected = new DiagnosticDescription[] {
                // (19,11): warning CS0472: The result of the expression is always 'false' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         W(i == null);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "i == null").WithArguments("false", "int", "int?").WithLocation(19, 11),
                // (20,11): warning CS0472: The result of the expression is always 'true' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         W(i != null);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "i != null").WithArguments("true", "int", "int?").WithLocation(20, 11),
                // (23,11): warning CS8073: The result of the expression is always 'false' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(g == null);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "g == null").WithArguments("false", "System.Guid", "System.Guid?").WithLocation(23, 11),
                // (24,11): warning CS8073: The result of the expression is always 'true' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(g != null);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "g != null").WithArguments("true", "System.Guid", "System.Guid?").WithLocation(24, 11),
                // (28,11): warning CS0472: The result of the expression is always 'false' since a value of type 'int' is never equal to 'null' of type 'short?'
                //         W(i == default(short?));            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "i == default(short?)").WithArguments("false", "int", "short?").WithLocation(28, 11),
                // (29,11): warning CS0472: The result of the expression is always 'true' since a value of type 'int' is never equal to 'null' of type 'short?'
                //         W(i != default(short?));            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "i != default(short?)").WithArguments("true", "int", "short?").WithLocation(29, 11),
                // (32,11): warning CS8073: The result of the expression is always 'false' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(g == default(Guid?));             // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "g == default(Guid?)").WithArguments("false", "System.Guid", "System.Guid?").WithLocation(32, 11),
                // (33,11): warning CS8073: The result of the expression is always 'true' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(g != default(Guid?));             // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "g != default(Guid?)").WithArguments("true", "System.Guid", "System.Guid?").WithLocation(33, 11),
                // (37,11): warning CS0472: The result of the expression is always 'false' since a value of type 'int' is never equal to 'null' of type 'sbyte?'
                //         W(i == new sbyte?());            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "i == new sbyte?()").WithArguments("false", "int", "sbyte?").WithLocation(37, 11),
                // (38,11): warning CS0472: The result of the expression is always 'true' since a value of type 'int' is never equal to 'null' of type 'sbyte?'
                //         W(i != new sbyte?());            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "i != new sbyte?()").WithArguments("true", "int", "sbyte?").WithLocation(38, 11),
                // (41,11): warning CS8073: The result of the expression is always 'false' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(g == new Guid?());             // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "g == new Guid?()").WithArguments("false", "System.Guid", "System.Guid?").WithLocation(41, 11),
                // (42,11): warning CS8073: The result of the expression is always 'true' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(g != new Guid?());             // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "g != new Guid?()").WithArguments("true", "System.Guid", "System.Guid?").WithLocation(42, 11),
                // (49,11): warning CS0472: The result of the expression is always 'false' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         W(null == i);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "null == i").WithArguments("false", "int", "int?").WithLocation(49, 11),
                // (50,11): warning CS0472: The result of the expression is always 'true' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         W(null != i);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "null != i").WithArguments("true", "int", "int?").WithLocation(50, 11),
                // (53,11): warning CS8073: The result of the expression is always 'false' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(null == g);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "null == g").WithArguments("false", "System.Guid", "System.Guid?").WithLocation(53, 11),
                // (54,11): warning CS8073: The result of the expression is always 'true' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(null != g);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "null != g").WithArguments("true", "System.Guid", "System.Guid?").WithLocation(54, 11),
                // (58,11): warning CS0472: The result of the expression is always 'false' since a value of type 'long' is never equal to 'null' of type 'long?'
                //         W(default(long?) == i);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "default(long?) == i").WithArguments("false", "long", "long?").WithLocation(58, 11),
                // (59,11): warning CS0472: The result of the expression is always 'true' since a value of type 'long' is never equal to 'null' of type 'long?'
                //         W(default(long?) != i);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "default(long?) != i").WithArguments("true", "long", "long?").WithLocation(59, 11),
                // (62,11): warning CS8073: The result of the expression is always 'false' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(default(Guid?) == g);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "default(Guid?) == g").WithArguments("false", "System.Guid", "System.Guid?").WithLocation(62, 11),
                // (63,11): warning CS8073: The result of the expression is always 'true' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(default(Guid?) != g);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "default(Guid?) != g").WithArguments("true", "System.Guid", "System.Guid?").WithLocation(63, 11),
                // (67,11): warning CS0472: The result of the expression is always 'false' since a value of type 'double' is never equal to 'null' of type 'double?'
                //         W(new double?() == i);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "new double?() == i").WithArguments("false", "double", "double?").WithLocation(67, 11),
                // (68,11): warning CS0472: The result of the expression is always 'true' since a value of type 'double' is never equal to 'null' of type 'double?'
                //         W(new double?() != i);            // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "new double?() != i").WithArguments("true", "double", "double?").WithLocation(68, 11),
                // (71,11): warning CS8073: The result of the expression is always 'false' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(new Guid?() == g);              // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "new Guid?() == g").WithArguments("false", "System.Guid", "System.Guid?").WithLocation(71, 11),
                // (72,11): warning CS8073: The result of the expression is always 'true' since a value of type 'System.Guid' is never equal to 'null' of type 'System.Guid?'
                //         W(new Guid?() != g);              // CS0472
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "new Guid?() != g").WithArguments("true", "System.Guid", "System.Guid?").WithLocation(72, 11),
                // (84,11): warning CS0472: The result of the expression is always 'false' since a value of type 'MyClass.E' is never equal to 'null' of type 'MyClass.E?'
                //         W((E?)1 == null);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "(E?)1 == null").WithArguments("false", "MyClass.E", "MyClass.E?").WithLocation(84, 11),
                // (85,11): warning CS0472: The result of the expression is always 'true' since a value of type 'MyClass.E' is never equal to 'null' of type 'MyClass.E?'
                //         W(null != (E?)1);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "null != (E?)1").WithArguments("true", "MyClass.E", "MyClass.E?").WithLocation(85, 11),
                // (87,11): warning CS0472: The result of the expression is always 'false' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         W((int?)1 == null);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "(int?)1 == null").WithArguments("false", "int", "int?").WithLocation(87, 11),
                // (88,11): warning CS0472: The result of the expression is always 'true' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         W(null != (int?)1);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "null != (int?)1").WithArguments("true", "int", "int?").WithLocation(88, 11),
                // (92,11): warning CS0472: The result of the expression is always 'false' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         W(0 == (int?)null);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "0 == (int?)null").WithArguments("false", "int", "int?").WithLocation(92, 11),
                // (93,11): warning CS0472: The result of the expression is always 'true' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         W((int?)null != 0);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "(int?)null != 0").WithArguments("true", "int", "int?").WithLocation(93, 11),
                // (95,11): warning CS0472: The result of the expression is always 'false' since a value of type 'MyClass.E' is never equal to 'null' of type 'MyClass.E?'
                //         W(0 == (E?)null);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "0 == (E?)null").WithArguments("false", "MyClass.E", "MyClass.E?").WithLocation(95, 11),
                // (96,11): warning CS0472: The result of the expression is always 'true' since a value of type 'MyClass.E' is never equal to 'null' of type 'MyClass.E?'
                //         W((E?)null != 0);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "(E?)null != 0").WithArguments("true", "MyClass.E", "MyClass.E?").WithLocation(96, 11)
            };
            var compatibleExpected = fullExpected.Where(d => !d.Code.Equals((int)ErrorCode.WRN_NubExprIsConstBool2)).ToArray();
            this.CompileAndVerify(source: text, expectedOutput: expected).VerifyDiagnostics(compatibleExpected);
            this.CompileAndVerify(source: text, expectedOutput: expected, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithStrictFeature()).VerifyDiagnostics(fullExpected);
        }

        [Fact]
        public void CS0472WRN_NubExprIsConstBool_ConstructorInitializer()
        {
            var text =
@"class A
{
    internal A(bool b)
    {
    }
}
class B : A
{
    B(int i) : base(i == null)
    {
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,21): warning CS0472: The result of the expression is always 'false' since a value of type 'int' is never equal to 'null' of type 'int?'
                //     B(int i) : base(i == null)
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "i == null").WithArguments("false", "int", "int?").WithLocation(9, 21));
        }

        [Fact]
        public void CS0612WRN_DeprecatedSymbol()
        {
            var text = @"
using System;
class MyClass
{
   [Obsolete]
   public static void ObsoleteMethod()
   {
   }

   [Obsolete]
   public static int ObsoleteField;
}
class MainClass
{
   static public void Main()
   {
      MyClass.ObsoleteMethod();    // CS0612 here: method is deprecated
      MyClass.ObsoleteField = 0;   // CS0612 here: field is deprecated
   }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                // (17,7): warning CS0612: 'MyClass.ObsoleteMethod()' is obsolete
                //       MyClass.ObsoleteMethod();    // CS0612 here: method is deprecated
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "MyClass.ObsoleteMethod()").WithArguments("MyClass.ObsoleteMethod()"),
                // (18,7): warning CS0612: 'MyClass.ObsoleteField' is obsolete
                //       MyClass.ObsoleteField = 0;   // CS0612 here: field is deprecated
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "MyClass.ObsoleteField").WithArguments("MyClass.ObsoleteField"));
        }

        [Fact, WorkItem(546062, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546062")]
        public void CS0618WRN_DeprecatedSymbol()
        {
            var text = @"
public class ConsoleStub
{
    public static void Main(string[] args)
    {
        System.Collections.CaseInsensitiveHashCodeProvider x;
        System.Console.WriteLine(x);
    }
}";
            CreateCompilation(text).
                VerifyDiagnostics(
                // (6,9): warning CS0618: 'System.Collections.CaseInsensitiveHashCodeProvider' is obsolete: 'Please use StringComparer instead.'
                //         System.Collections.CaseInsensitiveHashCodeProvider x;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "System.Collections.CaseInsensitiveHashCodeProvider").WithArguments("System.Collections.CaseInsensitiveHashCodeProvider", "Please use StringComparer instead."),
                // (7,34): error CS0165: Use of unassigned local variable 'x'
                //         System.Console.WriteLine(x);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x"));
        }

        [WorkItem(545347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545347")]
        [Fact]
        public void CS0649WRN_UnassignedInternalField()
        {
            var text = @"
using System.Collections;

class MyClass
{
    Hashtable table;  // CS0649
    
    public void Func(object o, string p)
    {
        table[p] = o;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (6,15): warning CS0649: Field 'MyClass.table' is never assigned to, and will always have its default value null
                //     Hashtable table;  // CS0649
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "table").WithArguments("MyClass.table", "null")
                );
        }

        [Fact, WorkItem(543454, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543454")]
        public void CS0649WRN_UnassignedInternalField_1()
        {
            var text = @"
public class GenClass<T, U> { }
public class Outer
{
    internal protected class C1 { }
    public class C2 { }
    internal class Test
    {
        public GenClass<C1, C2> Fld;
    }
    public static int Main() { return 0; }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Fld").WithArguments("Outer.Test.Fld", "null"));
        }

        [WorkItem(546449, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546449")]
        [WorkItem(546949, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546949")]
        [Fact]
        public void CS0652WRN_VacuousIntegralComp()
        {
            var text = @"
public class Class1
{
   private static byte i = 0;
   public static void Main()
   {
      const short j = 256;
      if (i == j)   // CS0652, 256 is out of range for byte
         i = 0;

      // However, we do not give this warning if both sides of the comparison are constants. In those
      // cases, we are probably in machine-generated code anyways.

      const byte k = 0;
      if (k == j) {}
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,11): warning CS0652: Comparison to integral constant is useless; the constant is outside the range of type 'byte'
                //       if (i == j)   // CS0652, 256 is out of range for byte
                Diagnostic(ErrorCode.WRN_VacuousIntegralComp, "i == j").WithArguments("byte"));
        }

        [WorkItem(546790, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546790")]
        [Fact]
        public void CS0652WRN_VacuousIntegralComp_ExplicitCast()
        {
            var text = @"
using System;

public class Program
{
    public static void Main()
    {
        Int16 wSuiteMask = 0; 
        const int VER_SUITE_WH_SERVER = 0x00008000;
        if (VER_SUITE_WH_SERVER == (Int32)wSuiteMask)
        {
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void CS0665WRN_IncorrectBooleanAssg()
        {
            var text = @"
class Test
{
   public static void Main()
   {
      bool i = false;

      if (i = true)   // CS0665
      // try the following line instead
      // if (i == true)
      {
      }

      System.Console.WriteLine(i);
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,11): warning CS0665: Assignment in conditional expression is always constant; did you mean to use == instead of = ?
                Diagnostic(ErrorCode.WRN_IncorrectBooleanAssg, "i = true"));
        }

        [Fact, WorkItem(540777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540777")]
        public void CS0665WRN_IncorrectBooleanAssg_ConditionalOperator()
        {
            var text = @"
class Program
{
    static int Main(string[] args)
    {
        bool a = true;
        System.Console.WriteLine(a);
        return ((a = false) ? 50 : 100);    // Warning
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,18): warning CS0665: Assignment in conditional expression is always constant; did you mean to use == instead of = ?
                Diagnostic(ErrorCode.WRN_IncorrectBooleanAssg, "a = false"));
        }

        [Fact]
        public void CS0665WRN_IncorrectBooleanAssg_Contexts()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        bool b = args.Length > 1;

        if (b = false) { }
        while (b = false) { }
        do { } while (b = false);
        for (; b = false; ) { }
        System.Console.WriteLine((b = false) ? 1 : 2);
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,13): warning CS0665: Assignment in conditional expression is always constant; did you mean to use == instead of = ?
                //         if (b = false) { }
                Diagnostic(ErrorCode.WRN_IncorrectBooleanAssg, "b = false"),
                // (9,16): warning CS0665: Assignment in conditional expression is always constant; did you mean to use == instead of = ?
                //         while (b = false) { }
                Diagnostic(ErrorCode.WRN_IncorrectBooleanAssg, "b = false"),
                // (10,23): warning CS0665: Assignment in conditional expression is always constant; did you mean to use == instead of = ?
                //         do { } while (b = false);
                Diagnostic(ErrorCode.WRN_IncorrectBooleanAssg, "b = false"),
                // (11,16): warning CS0665: Assignment in conditional expression is always constant; did you mean to use == instead of = ?
                //         for (; b = false; ) { }
                Diagnostic(ErrorCode.WRN_IncorrectBooleanAssg, "b = false"),
                // (12,35): warning CS0665: Assignment in conditional expression is always constant; did you mean to use == instead of = ?
                //         System.Console.WriteLine((b = false) ? 1 : 2);
                Diagnostic(ErrorCode.WRN_IncorrectBooleanAssg, "b = false"));
        }

        [Fact]
        public void CS0665WRN_IncorrectBooleanAssg_Nesting()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        bool b = args.Length > 1;

        if ((b = false)) { } // parens - warn
        if (((b = false))) { } // more parens - warn
        if (M(b = false)) { } // call - do not warn
        if ((bool)(b = false)) { } // cast - do not warn
        if ((b = false) || (b = true)) { } // binary operator - do not warn

        B bb = new B();
        if (bb = false) { } // implicit conversion - do not warn
    }

    static bool M(bool b) { return b; }
}

class B
{
    public static implicit operator B(bool b)
    {
        return new B();
    }

    public static bool operator true(B b)
    {
        return true;
    }

    public static bool operator false(B b)
    {
        return false;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,14): warning CS0665: Assignment in conditional expression is always constant; did you mean to use == instead of = ?
                //         if ((b = false)) { } // parens - warn
                Diagnostic(ErrorCode.WRN_IncorrectBooleanAssg, "b = false"),
                // (9,15): warning CS0665: Assignment in conditional expression is always constant; did you mean to use == instead of = ?
                //         if (((b = false))) { } // more parens - warn
                Diagnostic(ErrorCode.WRN_IncorrectBooleanAssg, "b = false"));
        }

        [Fact, WorkItem(909, "https://github.com/dotnet/roslyn/issues/909")]
        public void CS0675WRN_BitwiseOrSignExtend()
        {
            var text = @"
public class sign
{
   public static void Main()
   {
      int i32_hi = 1;
      int i32_lo = 1;
      ulong u64 = 1;
      sbyte i08 = 1;
      short i16 = -1;

      object v1 = (((long)i32_hi) << 32) | i32_lo;          // CS0675
      object v2 = (ulong)i32_hi | u64;                      // CS0675
      object v3 = (ulong)i32_hi | (ulong)i32_lo;            // No warning; the sign extension bits are the same on both sides.
      object v4 = (ulong)(uint)(ushort)i08 | (ulong)i32_lo;      // CS0675
      object v5 = (int)i08 | (int)i32_lo;   // No warning; sign extension is considered to be 'expected' when casting.
      object v6 = (((ulong)i32_hi) << 32) | (uint) i32_lo; // No warning; we've cast to a smaller unsigned type first. 
      // We suppress the warning if the bits that are going to be wiped out are known already to be all zero or all one:
      object v7 = 0x0000BEEFU | (uint)i16;         
      object v8 = 0xFFFFBEEFU | (uint)i16;   
      object v9 = 0xDEADBEEFU | (uint)i16;   // CS0675 

// We should do the exact same logic for nullables.

      int? ni32_hi = 1;
      int? ni32_lo = 1;
      ulong? nu64 = 1;
      sbyte? ni08 = 1;
      short? ni16 = -1;

      object v11 = (((long?)ni32_hi) << 32) | ni32_lo;          // CS0675
      object v12 = (ulong?)ni32_hi | nu64;                      // CS0675
      object v13 = (ulong?)ni32_hi | (ulong?)ni32_lo;            // No warning; the sign extension bits are the same on both sides.
      object v14 = (ulong?)(uint?)(ushort?)ni08 | (ulong?)ni32_lo;      // CS0675
      object v15 = (int?)ni08 | (int?)ni32_lo;   // No warning; sign extension is considered to be 'expected' when casting.
      object v16 = (((ulong?)ni32_hi) << 32) | (uint?) ni32_lo; // No warning; we've cast to a smaller unsigned type first. 
      // We suppress the warning if the bits that are going to be wiped out are known already to be all zero or all one:
      object v17 = 0x0000BEEFU | (uint?)ni16;         
      object v18 = 0xFFFFBEEFU | (uint?)ni16;   
      object v19 = 0xDEADBEEFU | (uint?)ni16;   // CS0675 
   }
}

class Test
{
    static void Main()
    {
        long bits = 0;
        for (int i = 0; i < 32; i++)
        {
            if (i % 2 == 0)
            {
                bits |= (1 << i);
                bits = bits | (1 << i);
            }
        }
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (12,19): warning CS0675: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
                //       object v1 = (((long)i32_hi) << 32) | i32_lo;          // CS0675
                Diagnostic(ErrorCode.WRN_BitwiseOrSignExtend, "(((long)i32_hi) << 32) | i32_lo"),
                // (13,19): warning CS0675: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
                //       object v2 = (ulong)i32_hi | u64;                      // CS0675
                Diagnostic(ErrorCode.WRN_BitwiseOrSignExtend, "(ulong)i32_hi | u64"),
                // (15,19): warning CS0675: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
                //       object v4 = (ulong)(uint)(ushort)i08 | (ulong)i32_lo;      // CS0675
                Diagnostic(ErrorCode.WRN_BitwiseOrSignExtend, "(ulong)(uint)(ushort)i08 | (ulong)i32_lo"),
                // (21,19): warning CS0675: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
                //       object v9 = 0xDEADBEEFU | (uint)i16;   // CS0675 
                Diagnostic(ErrorCode.WRN_BitwiseOrSignExtend, "0xDEADBEEFU | (uint)i16"),
                // (31,20): warning CS0675: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
                //       object v11 = (((long?)ni32_hi) << 32) | ni32_lo;          // CS0675
                Diagnostic(ErrorCode.WRN_BitwiseOrSignExtend, "(((long?)ni32_hi) << 32) | ni32_lo"),
                // (32,20): warning CS0675: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
                //       object v12 = (ulong?)ni32_hi | nu64;                      // CS0675
                Diagnostic(ErrorCode.WRN_BitwiseOrSignExtend, "(ulong?)ni32_hi | nu64"),
                // (34,20): warning CS0675: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
                //       object v14 = (ulong?)(uint?)(ushort?)ni08 | (ulong?)ni32_lo;      // CS0675
                Diagnostic(ErrorCode.WRN_BitwiseOrSignExtend, "(ulong?)(uint?)(ushort?)ni08 | (ulong?)ni32_lo"),
                // (40,20): warning CS0675: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
                //       object v19 = 0xDEADBEEFU | (uint?)ni16;   // CS0675 
                Diagnostic(ErrorCode.WRN_BitwiseOrSignExtend, "0xDEADBEEFU | (uint?)ni16"),
                // (53,17): warning CS0675: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
                //                 bits |= (1 << i);
                Diagnostic(ErrorCode.WRN_BitwiseOrSignExtend, "bits |= (1 << i)").WithLocation(53, 17),
                // (54,24): warning CS0675: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
                //                 bits = bits | (1 << i);
                Diagnostic(ErrorCode.WRN_BitwiseOrSignExtend, "bits | (1 << i)").WithLocation(54, 24)
                );
        }

        [Fact]
        public void CS0728WRN_AssignmentToLockOrDispose01()
        {
            CreateCompilation(@"
using System;
public class ValidBase : IDisposable
{
    public void Dispose() {  }
}

public class Logger
{
    public static void dummy()
    {
        ValidBase vb = null;
        using (vb) 
        {
            vb = null;  // CS0728
        }
        vb = null;
    }
    public static void Main() { }
}")
                .VerifyDiagnostics(
                // (15,13): warning CS0728: Possibly incorrect assignment to local 'vb' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "vb").WithArguments("vb"));
        }

        [Fact]
        public void CS0728WRN_AssignmentToLockOrDispose02()
        {
            CreateCompilation(
@"class D : System.IDisposable
{
    public void Dispose() { }
}
class C
{
    static void M()
    {
        D d = new D();
        using (d)
        {
            N(ref d);
        }
        lock (d)
        {
            N(ref d);
        }
    }
    static void N(ref D d)
    {
    }
}")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "d").WithArguments("d").WithLocation(12, 19),
                    Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "d").WithArguments("d").WithLocation(16, 19));
        }

        [WorkItem(543615, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543615"), WorkItem(546550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546550")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void CS0811ERR_DebugFullNameTooLong()
        {
            var text = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    using VeryLong = List<List<List<List<List<List<List<List<List<List<List<List<List
   <List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<int>>>>>>>>>>>>>>>>>>>>>>>>>>>>; // CS0811

    class Test
    {
        static int Main()
        {
            VeryLong goo = null;
            Console.WriteLine(goo);
            return 1;
        }
    }
}
";

            var compilation = CreateCompilation(text, targetFramework: TargetFramework.Mscorlib45, options: TestOptions.DebugExe);

            var exebits = new System.IO.MemoryStream();
            var pdbbits = new System.IO.MemoryStream();
            var result = compilation.Emit(exebits, pdbbits, options: TestOptions.NativePdbEmit);

            result.Diagnostics.Verify(
                // (12,20): warning CS0811: The fully qualified name for 'AVeryLong TSystem.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' is too long for debug information. Compile without '/debug' option.
                //         static int Main()
                Diagnostic(ErrorCode.WRN_DebugFullNameTooLong, "Main").WithArguments("AVeryLong TSystem.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").WithLocation(12, 20));
        }

        [Fact]
        public void CS1058WRN_UnreachableGeneralCatch()
        {
            var text =
@"class C
{
    static void M()
    {
        try { }
        catch (System.Exception) { }
        catch (System.IO.IOException) { }
        catch { }
        try { }
        catch (System.IO.IOException) { }
        catch (System.Exception) { }
        catch { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "System.IO.IOException").WithArguments("System.Exception").WithLocation(7, 16),
                Diagnostic(ErrorCode.WRN_UnreachableGeneralCatch, "catch").WithLocation(8, 9),
                Diagnostic(ErrorCode.WRN_UnreachableGeneralCatch, "catch").WithLocation(12, 9));
        }

        //        [Fact(Skip = "11486")]
        //        public void CS1060WRN_UninitializedField()
        //        {
        //            var text = @"
        //namespace CS1060
        //{
        //    public class U
        //    {
        //        public int i;
        //    }
        //
        //    public struct S
        //    {
        //        public U u;
        //    }
        //    public class Test
        //    {
        //        static void Main()
        //        {
        //            S s;
        //            s.u.i = 5;  // CS1060
        //        }
        //    }
        //}
        //";
        //            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
        //                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.WRN_UninitializedField, Line = 18, Column = 13, IsWarning = true } });
        //        }

        //        [Fact()]
        //        public void CS1064ERR_DebugFullNameTooLong()
        //        {
        //            var text = @"
        //";
        //            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
        //                new ErrorDescription[] { new ErrorDescription { Code = 1064, Line = 7, Column = 5, IsWarning = true } }
        //                );
        //        }

        [Fact]
        public void CS1570WRN_XMLParseError()
        {
            var text = @"
namespace ns
{
   // the following line generates CS1570
   /// <summary> returns true if < 5 </summary>
   // try this instead
   // /// <summary> returns true if &lt;5 </summary>

   public class MyClass
   {
      public static void Main ()
      {
      }
   }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (5,35): warning CS1570: XML comment has badly formed XML -- 'An identifier was expected.'
                //    /// <summary> returns true if < 5 </summary>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (5,35): warning CS1570: XML comment has badly formed XML -- '5'
                //    /// <summary> returns true if < 5 </summary>
                Diagnostic(ErrorCode.WRN_XMLParseError, " ").WithArguments("5"),
                // (11,26): warning CS1591: Missing XML comment for publicly visible type or member 'ns.MyClass.Main()'
                //       public static void Main ()
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Main").WithArguments("ns.MyClass.Main()"));
        }

        [Fact]
        public void CS1571WRN_DuplicateParamTag()
        {
            var text = @"
/// <summary>help text</summary>
public class MyClass
{
   /// <param name='Int1'>Used to indicate status.</param>
   /// <param name='Char1'>An initial.</param>
   /// <param name='Int1'>Used to indicate status.</param> // CS1571
   public static void MyMethod(int Int1, char Char1)
   {
   }

   /// <summary>help text</summary>
   public static void Main ()
   {
   }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (7,15): warning CS1571: XML comment has a duplicate param tag for 'Int1'
                //    /// <param name='Int1'>Used to indicate status.</param> // CS1571
                Diagnostic(ErrorCode.WRN_DuplicateParamTag, "name='Int1'").WithArguments("Int1"));
        }

        [Fact]
        public void CS1572WRN_UnmatchedParamTag()
        {
            var text = @"
/// <summary>help text</summary>
public class MyClass
{
   /// <param name='Int1'>Used to indicate status.</param>
   /// <param name='Char1'>Used to indicate status.</param>
   /// <param name='Char2'>???</param> // CS1572
   public static void MyMethod(int Int1, char Char1)
   {
   }

   /// <summary>help text</summary>
   public static void Main ()
   {
   }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (7,21): warning CS1572: XML comment has a param tag for 'Char2', but there is no parameter by that name
                //    /// <param name='Char2'>???</param> // CS1572
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "Char2").WithArguments("Char2"));
        }

        [Fact]
        public void CS1573WRN_MissingParamTag()
        {
            var text = @"
/// <summary> </summary>
public class MyClass
{
    /// <param name='Int1'>Used to indicate status.</param>
    /// enter a comment for Char1?
    public static void MyMethod(int Int1, char Char1)
    {
    }
    /// <summary> </summary>
    public static void Main()
    {
    }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (7,48): warning CS1573: Parameter 'Char1' has no matching param tag in the XML comment for 'MyClass.MyMethod(int, char)' (but other parameters do)
                //     public static void MyMethod(int Int1, char Char1)
                Diagnostic(ErrorCode.WRN_MissingParamTag, "Char1").WithArguments("Char1", "MyClass.MyMethod(int, char)"));
        }

        [Fact]
        public void CS1574WRN_BadXMLRef()
        {
            var text = @"
/// <see cref=""D""/>
public class C
{
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (2,16): warning CS1574: XML comment has cref attribute 'D' that could not be resolved
                // /// <see cref="D"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "D").WithArguments("D"));
        }

        [Fact]
        public void CS1580WRN_BadXMLRefParamType()
        {
            var text = @"
/// <seealso cref=""Test(i)""/>   // CS1580
public class MyClass
{
   /// <summary>help text</summary>
   public static void Main()
   {
   }
   /// <summary>help text</summary>
   public void Test(int i)
   {
   }
   /// <summary>help text</summary>
   public void Test(char i)
   {
   }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (2,20): warning CS1580: Invalid type for parameter 'i' in XML comment cref attribute: 'Test(i)'
                // /// <seealso cref="Test(i)"/>   // CS1580
                Diagnostic(ErrorCode.WRN_BadXMLRefParamType, "i").WithArguments("i", "Test(i)"),
                // (2,20): warning CS1574: XML comment has cref attribute 'Test(i)' that could not be resolved
                // /// <seealso cref="Test(i)"/>   // CS1580
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Test(i)").WithArguments("Test(i)"));
        }

        [Fact]
        public void CS1581WRN_BadXMLRefReturnType()
        {
            var text = @"
/// <summary>help text</summary>
public class MyClass
{
   /// <summary>help text</summary>
   public static void Main()
   {
   }
   /// <summary>help text</summary>
   public static explicit operator int(MyClass f)
   {
      return 0;
   }
}
/// <seealso cref=""MyClass.explicit operator intt(MyClass)""/>   // CS1581
public class MyClass2
{
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (15,20): warning CS1581: Invalid return type in XML comment cref attribute
                // /// <seealso cref="MyClass.explicit operator intt(MyClass)"/>   // CS1581
                Diagnostic(ErrorCode.WRN_BadXMLRefReturnType, "intt").WithArguments("intt", "MyClass.explicit operator intt(MyClass)"),
                // (15,20): warning CS1574: XML comment has cref attribute 'MyClass.explicit operator intt(MyClass)' that could not be resolved
                // /// <seealso cref="MyClass.explicit operator intt(MyClass)"/>   // CS1581
                Diagnostic(ErrorCode.WRN_BadXMLRef, "MyClass.explicit operator intt(MyClass)").WithArguments("explicit operator intt(MyClass)"));
        }

        [Fact]
        public void CS1584WRN_BadXMLRefSyntax()
        {
            var text = @"
/// 
public class MyClass1
{
    /// 
    public static MyClass1 operator /(MyClass1 a1, MyClass1 a2)
    {
        return null;
    }
    /// <seealso cref=""MyClass1.operator@""/>
    public static void Main()
    {
    }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (10,24): warning CS1584: XML comment has syntactically incorrect cref attribute 'MyClass1.operator@'
                //     /// <seealso cref="MyClass1.operator@"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "MyClass1.operator").WithArguments("MyClass1.operator@"),
                // (10,41): warning CS1658: Overloadable operator expected. See also error CS1037.
                //     /// <seealso cref="MyClass1.operator@"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "@").WithArguments("Overloadable operator expected", "1037"),
                // (10,41): error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
                //     /// <seealso cref="MyClass1.operator@"/>
                Diagnostic(ErrorCode.ERR_ExpectedVerbatimLiteral, ""));
        }

        [Fact]
        public void CS1587WRN_UnprocessedXMLComment()
        {
            var text = @"
/// <summary>test</summary>   // CS1587, tag not allowed on namespace
namespace MySpace
{
   class MyClass
   {
      public static void Main()
      {
      }
   }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
        }

        [Fact]
        public void CS1589WRN_NoResolver()
        {
            var text = @"
/// <include file='CS1589.doc' path='MyDocs/MyMembers[@name=""test""]/' />   // CS1589
class Test
{
    public static void Main()
    {
    }
}
";
            var c = CreateCompilation(
                new[] { Parse(text, options: TestOptions.RegularWithDocumentationComments) },
                options: TestOptions.ReleaseDll.WithXmlReferenceResolver(null));

            c.VerifyDiagnostics(
                // (2,5): warning CS1589: Unable to include XML fragment 'MyDocs/MyMembers[@name="test"]/' of file 'CS1589.doc' -- References to XML documents are not supported.
                // /// <include file='CS1589.doc' path='MyDocs/MyMembers[@name="test"]/' />   // CS1589
                Diagnostic(ErrorCode.WRN_FailedInclude, @"<include file='CS1589.doc' path='MyDocs/MyMembers[@name=""test""]/' />").
                    WithArguments("CS1589.doc", @"MyDocs/MyMembers[@name=""test""]/", "References to XML documents are not supported."));
        }

        [Fact]
        public void CS1589WRN_FailedInclude()
        {
            var text = @"
/// <include file='CS1589.doc' path='MyDocs/MyMembers[@name=""test""]/' />   // CS1589
class Test
{
    public static void Main()
    {
    }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (2,5): warning CS1589: Unable to include XML fragment 'MyDocs/MyMembers[@name="test"]/' of file 'CS1589.doc' -- Unable to find the specified file.
                // /// <include file='CS1589.doc' path='MyDocs/MyMembers[@name="test"]/' />   // CS1589
                Diagnostic(ErrorCode.WRN_FailedInclude, @"<include file='CS1589.doc' path='MyDocs/MyMembers[@name=""test""]/' />").
                    WithArguments("CS1589.doc", @"MyDocs/MyMembers[@name=""test""]/", "File not found."));
        }

        [Fact]
        public void CS1590WRN_InvalidInclude()
        {
            var text = @"
/// <include path='MyDocs/MyMembers[@name=""test""]/*' />   // CS1590
class Test
{
   public static void Main()
   {
   }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (2,5): warning CS1590: Invalid XML include element -- Missing file attribute
                // /// <include path='MyDocs/MyMembers[@name="test"]/*' />   // CS1590
                Diagnostic(ErrorCode.WRN_InvalidInclude, @"<include path='MyDocs/MyMembers[@name=""test""]/*' />").WithArguments("Missing file attribute"));
        }

        [Fact]
        public void CS1591WRN_MissingXMLComment()
        {
            var text = @"
/// text
public class Test
{
   // /// text
   public static void Main()   // CS1591
   {
   }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (6,23): warning CS1591: Missing XML comment for publicly visible type or member 'Test.Main()'
                //    public static void Main()   // CS1591
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Main").WithArguments("Test.Main()"));
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/18610")]
        public void CS1592WRN_XMLParseIncludeError()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText("&");
            var sourceTemplate = @"
/// <include file='{0}' path='element'/>
public class Test {{ }}
";
            var comp = CreateCompilationWithMscorlib40AndDocumentationComments(string.Format(sourceTemplate, xmlFile.Path));

            using (new EnsureEnglishUICulture())
            {
                comp.VerifyDiagnostics(
                    // dcf98d2ac30a.xml(1,1): warning CS1592: Badly formed XML in included comments file -- 'Data at the root level is invalid.'
                    Diagnostic(ErrorCode.WRN_XMLParseIncludeError).WithArguments("Data at the root level is invalid."));
            }
        }

        [Fact]
        public void CS1658WRN_ErrorOverride()
        {
            var text = @"
/// <seealso cref=""""/>    
public class Test 
{
    ///
    public static int Main() 
    {
        return 0;
    }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (2,20): warning CS1584: XML comment has syntactically incorrect cref attribute ''
                // /// <seealso cref=""/>    
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, @"""").WithArguments(""),
                // (2,20): warning CS1658: Identifier expected. See also error CS1001.
                // /// <seealso cref=""/>    
                Diagnostic(ErrorCode.WRN_ErrorOverride, @"""").WithArguments("Identifier expected", "1001"));
        }

        // TODO (tomat): Also fix AttributeTests.DllImport_AttributeRedefinition
        [Fact(Skip = "530377"), WorkItem(530377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530377"), WorkItem(685159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/685159")]
        public void CS1685WRN_MultiplePredefTypes()
        {
            var text = @"
public static class C
{
    public static void Extension(this int X) {}
}";
            // include both mscorlib 4.0 and System.Core 3.5, both of which contain ExtensionAttribute
            // These libraries are not yet in our suite
            CreateEmptyCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.WRN_MultiplePredefTypes, ""));
        }

        [Fact, WorkItem(530379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530379")]
        public void CS1690WRN_CallOnNonAgileField()
        {
            var text = @"
using System;

class WarningCS1690 : MarshalByRefObject
{
    int i = 5;

    public static void Main()
    {
        WarningCS1690 e = new WarningCS1690();
        e.i.ToString();   // CS1690
        int i = e.i;
        i.ToString();
        e.i = i;
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.WRN_CallOnNonAgileField, Line = 11, Column = 9, IsWarning = true } });
        }

        [Fact, WorkItem(530379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530379")]
        public void CS1690WRN_CallOnNonAgileField_Variations()
        {
            var text = @"
using System;

struct S
{
    public event Action Event;
    public int Field;
    public int Property { get; set; }
    public int this[int x] { get { return 0; } set { } }
    public void M() { }

    class WarningCS1690 : MarshalByRefObject
    {
        S s;

        public static void Main()
        {
            WarningCS1690 w = new WarningCS1690();
            w.s.Event = null;
            w.s.Event += null;
            w.s.Property++;
            w.s[0]++;
            w.s.M();
            Action a = w.s.M;
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (19,13): warning CS1690: Accessing a member on 'S.WarningCS1690.s' may cause a runtime exception because it is a field of a marshal-by-reference class
                //             w.s.Event = null;
                Diagnostic(ErrorCode.WRN_CallOnNonAgileField, "w.s").WithArguments("S.WarningCS1690.s"),
                // (20,13): warning CS1690: Accessing a member on 'S.WarningCS1690.s' may cause a runtime exception because it is a field of a marshal-by-reference class
                //             w.s.Event += null;
                Diagnostic(ErrorCode.WRN_CallOnNonAgileField, "w.s").WithArguments("S.WarningCS1690.s"),
                // (21,13): warning CS1690: Accessing a member on 'S.WarningCS1690.s' may cause a runtime exception because it is a field of a marshal-by-reference class
                //             w.s.Property++;
                Diagnostic(ErrorCode.WRN_CallOnNonAgileField, "w.s").WithArguments("S.WarningCS1690.s"),
                // (22,13): warning CS1690: Accessing a member on 'S.WarningCS1690.s' may cause a runtime exception because it is a field of a marshal-by-reference class
                //             w.s[0]++;
                Diagnostic(ErrorCode.WRN_CallOnNonAgileField, "w.s").WithArguments("S.WarningCS1690.s"),
                // (23,13): warning CS1690: Accessing a member on 'S.WarningCS1690.s' may cause a runtime exception because it is a field of a marshal-by-reference class
                //             w.s.M();
                Diagnostic(ErrorCode.WRN_CallOnNonAgileField, "w.s").WithArguments("S.WarningCS1690.s"),
                // (24,24): warning CS1690: Accessing a member on 'S.WarningCS1690.s' may cause a runtime exception because it is a field of a marshal-by-reference class
                //             Action a = w.s.M;
                Diagnostic(ErrorCode.WRN_CallOnNonAgileField, "w.s").WithArguments("S.WarningCS1690.s"),

                // (7,16): warning CS0649: Field 'S.Field' is never assigned to, and will always have its default value 0
                //     public int Field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field").WithArguments("S.Field", "0"),
                // (6,25): warning CS0414: The field 'S.Event' is assigned but its value is never used
                //     public event Action Event;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "Event").WithArguments("S.Event"));
        }

        [Fact, WorkItem(530379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530379")]
        public void CS1690WRN_CallOnNonAgileField_Class()
        {
            var text = @"
using System;

class S
{
    public event Action Event;
    public int Field;
    public int Property { get; set; }
    public int this[int x] { get { return 0; } set { } }
    public void M() { }

    class WarningCS1690 : MarshalByRefObject
    {
        S s;

        public static void Main()
        {
            WarningCS1690 w = new WarningCS1690();
            w.s.Event = null;
            w.s.Event += null;
            w.s.Property++;
            w.s[0]++;
            w.s.M();
            Action a = w.s.M;
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (14,11): warning CS0649: Field 'S.WarningCS1690.s' is never assigned to, and will always have its default value null
                //         S s;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "s").WithArguments("S.WarningCS1690.s", "null"),
                // (7,16): warning CS0649: Field 'S.Field' is never assigned to, and will always have its default value 0
                //     public int Field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field").WithArguments("S.Field", "0"),
                // (6,25): warning CS0414: The field 'S.Event' is assigned but its value is never used
                //     public event Action Event;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "Event").WithArguments("S.Event"));
        }

        //        [Fact()]
        //        public void CS1707WRN_DelegateNewMethBind()
        //        {
        //            var text = @"
        //";
        //            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
        //                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.WRN_DelegateNewMethBind, Line = 7, Column = 5, IsWarning = true } }
        //                );
        //        }

        [Fact(), WorkItem(530384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530384")]
        public void CS1709WRN_EmptyFileName()
        {
            var text = @"
class Test
{
    static void Main()
    {
#pragma checksum """" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" """"  // CS1709
    }
}
";
            //EDMAURER no longer giving this low-value warning.
            CreateCompilation(text).
                VerifyDiagnostics();
            //VerifyDiagnostics(Diagnostic(ErrorCode.WRN_EmptyFileName, @""""));
        }

        [Fact]
        public void CS1710WRN_DuplicateTypeParamTag()
        {
            var text = @"
class Stack<ItemType>
{
}
/// <typeparam name=""MyType"">can be an int</typeparam>
/// <typeparam name=""MyType"">can be an int</typeparam>
class MyStackWrapper<MyType>
{
    // Open constructed type Stack<MyType>.
    Stack<MyType> stack;
    public MyStackWrapper(Stack<MyType> s)
    {
        stack = s;
    }
}
class CMain
{
    public static void Main()
    {
    }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (6,16): warning CS1710: XML comment has a duplicate typeparam tag for 'MyType'
                // /// <typeparam name="MyType">can be an int</typeparam>
                Diagnostic(ErrorCode.WRN_DuplicateTypeParamTag, @"name=""MyType""").WithArguments("MyType"));
        }

        [Fact]
        public void CS1711WRN_UnmatchedTypeParamTag()
        {
            var text = @"
///<typeparam name=""WrongName"">can be an int</typeparam>
class CMain
{
    public static void Main() { }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (2,21): warning CS1711: XML comment has a typeparam tag for 'WrongName', but there is no type parameter by that name
                // ///<typeparam name="WrongName">can be an int</typeparam>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "WrongName").WithArguments("WrongName"));
        }

        [Fact]
        public void CS1712WRN_MissingTypeParamTag()
        {
            var text = @"
///<summary>A generic list delegate.</summary>
///<typeparam name=""T"">The first type stored by the list.</typeparam>
public delegate void List<T,W>();

///
public class Test
{
    ///
    public static void Main()
    {
    }
}	
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (4,29): warning CS1712: Type parameter 'W' has no matching typeparam tag in the XML comment on 'List<T, W>' (but other type parameters do)
                // public delegate void List<T,W>();
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "W").WithArguments("W", "List<T, W>"));
        }

        [Fact]
        public void CS1717WRN_AssignmentToSelf()
        {
            var text = @"
public class Test
{
   public static void Main()
   {
      int x = 0;
      x = x;   // CS1717
   }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] { new ErrorDescription { Code = (int)ErrorCode.WRN_AssignmentToSelf, Line = 7, Column = 7, IsWarning = true } });
        }

        [WorkItem(543470, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543470")]
        [Fact]
        public void CS1717WRN_AssignmentToSelf02()
        {
            var text = @"
class C
{
  void M(object p)
  {
    object oValue = p;
    if (oValue is int)
    {
      //(SQL 9.0) 653716 + common sense
      oValue = (double) ((int) oValue);
     }
  }
}";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void CS1717WRN_AssignmentToSelf03()
        {
            var text = @"
using System;

class Program
{
    int f;
    event Action e;

    void Test(int p)
    {
        int l = 0;

        l = l;
        p = p;
        f = f;
        e = e;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (13,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         l = l;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "l = l"),
                // (14,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         p = p;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "p = p"),
                // (15,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         f = f;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "f = f"),
                // (16,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         e = e;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "e = e"));
        }

        [Fact]
        public void CS1717WRN_AssignmentToSelf04()
        {
            var text = @"
using System;

class Program
{
    int f;
    event Action e;
    
    static int sf;
    static event Action se;

    void Test(Program other)
    {
        f = this.f;
        e = this.e;

        f = other.f; //fine
        e = other.e; //fine

        sf = sf;
        se = se;

        sf = Program.sf;
        se = Program.se;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (14,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         f = this.f;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "f = this.f"),
                // (15,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         e = this.e;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "e = this.e"),
                // (20,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         sf = sf;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "sf = sf"),
                // (21,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         se = se;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "se = se"),
                // (23,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         sf = Program.sf;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "sf = Program.sf"),
                // (24,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         se = Program.se;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "se = Program.se"));
        }

        [Fact]
        public void CS1717WRN_AssignmentToSelf05()
        {
            var text = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var unused = from x in args select x = x;
    }
}
";
            // CONSIDER: dev11 reports WRN_AssignmentToSelf.
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (8,44): error CS1947: Range variable 'x' cannot be assigned to -- it is read only
                //         var unused = from x in args select x = x;
                Diagnostic(ErrorCode.ERR_QueryRangeVariableReadOnly, "x").WithArguments("x"));
        }

        [Fact]
        public void CS1717WRN_AssignmentToSelf06()
        {
            var text = @"
class C
{
    void M(
        byte b,
        sbyte sb,
        short s,
        ushort us,
        int i,
        uint ui,
        long l,
        ulong ul,
        float f,
        double d,
        decimal m,
        bool bo,
        object o,
        C cl,
        S st)
    {
        b = (byte)b;
        sb = (sbyte)sb;
        s = (short)s;
        us = (ushort)us;
        i = (int)i;
        ui = (uint)ui;
        l = (long)l;
        ul = (ulong)ul;
        f = (float)f; // Not reported by dev11.
        d = (double)d; // Not reported by dev11.
        m = (decimal)m;
        bo = (bool)bo;
        o = (object)o;
        cl = (C)cl;
        st = (S)st;
    }
}

struct S
{
}
";
            // CONSIDER: dev11 does not strip off float or double identity-conversions and, thus,
            // does not warn about those assignments.
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (21,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         b = (byte)b;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "b = (byte)b"),
                // (22,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         sb = (sbyte)sb;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "sb = (sbyte)sb"),
                // (23,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         s = (short)s;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "s = (short)s"),
                // (24,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         us = (ushort)us;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "us = (ushort)us"),
                // (25,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         i = (int)i;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "i = (int)i"),
                // (26,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         ui = (uint)ui;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "ui = (uint)ui"),
                // (27,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         l = (long)l;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "l = (long)l"),
                // (28,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         ul = (ulong)ul;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "ul = (ulong)ul"),
                // (29,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         f = (float)f; // Not reported by dev11.
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "f = (float)f"),
                // (30,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         d = (double)d; // Not reported by dev11.
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "d = (double)d"),
                // (31,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         m = (decimal)m;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "m = (decimal)m"),
                // (32,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         bo = (bool)bo;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "bo = (bool)bo"),
                // (33,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         o = (object)o;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "o = (object)o"),
                // (34,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         cl = (C)cl;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "cl = (C)cl"),
                // (35,9): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         st = (S)st;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "st = (S)st"));
        }

        [Fact, WorkItem(546493, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546493")]
        public void CS1718WRN_ComparisonToSelf()
        {
            var text = @"
class Tester
{
    static int j = 123;
    static void Main()
    {
        int i = 0;
        if (i == i) i++;
        if (j == Tester.j) j++;
    }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (8,13): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         if (i == i) i++;
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "i == i"),
                // (9,13): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         if (j == Tester.j) j++;
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "j == Tester.j"));
        }

        [Fact, WorkItem(580501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/580501")]
        public void CS1718WRN_ComparisonToSelf2()
        {
            var text = @"
using System.Linq;
class Tester
{
    static void Main()
    {
        var q = from int x1 in new[] { 2, 9, 1, 8, }
        where x1 > x1 // CS1718
        select x1;
    }
}
";

            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (8,15): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         where x1 > x1 // CS1718
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "x1 > x1"));
        }

        [Fact]
        public void CS1720WRN_DotOnDefault01()
        {
            var source =
@"class A
{
    internal object P { get; set; }
}
interface I
{
    object P { get; set; }
}
static class C
{
    static void M<T1, T2, T3, T4, T5, T6, T7>()
        where T2 : new()
        where T3 : struct
        where T4 : class
        where T5 : T1
        where T6 : A
        where T7 : I
    {
        default(int).GetHashCode();
        default(object).GetHashCode();
        default(T1).GetHashCode();
        default(T2).GetHashCode();
        default(T3).GetHashCode();
        default(T4).GetHashCode();
        default(T5).GetHashCode();
        default(T6).GetHashCode();
        default(T7).GetHashCode();
        default(T6).P = null;
        default(T7).P = null;
        default(int).E();
        default(object).E();
        default(T1).E();
        default(T2).E();
        default(T3).E();
        default(T4).E();
        default(T5).E();
        default(T6).E(); // Dev10 (incorrectly) reports CS1720
        default(T7).E();
    }
    static void E(this object o) { }
}";
            CreateCompilationWithMscorlib40(source, references: new[] { SystemCoreRef }, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (20,9): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'object' is null
                //         default(object).GetHashCode();
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(object).GetHashCode").WithArguments("object").WithLocation(20, 9),
                // (24,9): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'T4' is null
                //         default(T4).GetHashCode();
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(T4).GetHashCode").WithArguments("T4").WithLocation(24, 9),
                // (26,9): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'T6' is null
                //         default(T6).GetHashCode();
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(T6).GetHashCode").WithArguments("T6").WithLocation(26, 9),
                // (28,9): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'T6' is null
                //         default(T6).P = null;
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(T6).P").WithArguments("T6").WithLocation(28, 9));
            CreateCompilationWithMscorlib40(source, references: new[] { SystemCoreRef }, options: TestOptions.ReleaseDll.WithNullableContextOptions(NullableContextOptions.Disable)).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CS1720WRN_DotOnDefault02()
        {
            var source =
@"class A
{
    internal object this[object index] { get { return null; } set { } }
}
struct S
{
    internal object this[object index] { get { return null; } set { } }
}
interface I
{
    object this[object index] { get; set; }
}
class C
{
    unsafe static void M<T1, T2, T3, T4>()
        where T1 : A
        where T2 : I
        where T3 : struct, I
        where T4 : class, I
    {
        object o;
        o = default(int*)[0];
        o = default(A)[0];
        o = default(S)[0];
        o = default(I)[0];
        o = default(object[])[0];
        o = default(T1)[0];
        o = default(T2)[0];
        o = default(T3)[0];
        o = default(T4)[0];
        default(int*)[1] = 1;
        default(A)[1] = o;
        default(I)[1] = o;
        default(object[])[1] = o;
        default(T1)[1] = o;
        default(T2)[1] = o;
        default(T3)[1] = o;
        default(T4)[1] = o;
    }
}";
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (23,13): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'A' is null
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(A)[0]").WithArguments("A").WithLocation(23, 13),
                // (25,13): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'I' is null
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(I)[0]").WithArguments("I").WithLocation(25, 13),
                // (26,13): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'object[]' is null
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(object[])[0]").WithArguments("object[]").WithLocation(26, 13),
                // (27,13): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'T1' is null
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(T1)[0]").WithArguments("T1").WithLocation(27, 13),
                // (30,13): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'T4' is null
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(T4)[0]").WithArguments("T4").WithLocation(30, 13),
                // (32,9): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'A' is null
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(A)[1]").WithArguments("A").WithLocation(32, 9),
                // (33,9): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'I' is null
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(I)[1]").WithArguments("I").WithLocation(33, 9),
                // (34,9): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'object[]' is null
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(object[])[1]").WithArguments("object[]").WithLocation(34, 9),
                // (35,9): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'T1' is null
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(T1)[1]").WithArguments("T1").WithLocation(35, 9),
                // (37,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(T3)[1]").WithLocation(37, 9), // Incorrect? See CS0131ERR_AssgLvalueExpected03 unit test.
                                                                                                    // (38,9): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'T4' is null
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(T4)[1]").WithArguments("T4").WithLocation(38, 9));
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (37,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         default(T3)[1] = o;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(T3)[1]").WithLocation(37, 9));
        }

        [Fact]
        public void CS1720WRN_DotOnDefault03()
        {
            var source =
@"static class A
{
    static void Main()
    {
        System.Console.WriteLine(default(string).IsNull());
    }

    internal static bool IsNull(this string val)
    {
        return (object)val == null; 
    }
}
";
            CompileAndVerifyWithMscorlib40(source, expectedOutput: "True", references: new[] { SystemCoreRef }, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // Do not report the following warning:
                // (5,34): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'string' is null
                //         System.Console.WriteLine(default(string).IsNull());
                // Diagnostic(ErrorCode.WRN_DotOnDefault, "default(string).IsNull").WithArguments("string").WithLocation(5, 34)
                );
            CompileAndVerifyWithMscorlib40(source, expectedOutput: "True", references: new[] { SystemCoreRef }).VerifyDiagnostics();
        }

        [Fact]
        public void CS1723WRN_BadXMLRefTypeVar()
        {
            var text = @"
///<summary>A generic list class.</summary>
///<see cref=""T"" />   // CS1723
public class List<T>
{
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(text).VerifyDiagnostics(
                // (3,15): warning CS1723: XML comment has cref attribute 'T' that refers to a type parameter
                // ///<see cref="T" />   // CS1723
                Diagnostic(ErrorCode.WRN_BadXMLRefTypeVar, "T").WithArguments("T"));
        }

        [Fact]
        public void CS1974WRN_DynamicDispatchToConditionalMethod()
        {
            var text = @"
using System.Diagnostics;
class Myclass
{
    static void Main()
    {
        dynamic d = null;
        // Warning because Goo might be conditional.
        Goo(d); 
        // No warning; only the two-parameter Bar is conditional.
        Bar(d);
    }

    [Conditional(""DEBUG"")]
    static void Goo(string d) {}

    [Conditional(""DEBUG"")]
    static void Bar(int x, int y) {}
    
    static void Bar(string x) {}
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(text);
            comp.VerifyDiagnostics(
// (9,9): warning CS1974: The dynamically dispatched call to method 'Goo' may fail at runtime because one or more applicable overloads are conditional methods.
//         Goo(d); 
Diagnostic(ErrorCode.WRN_DynamicDispatchToConditionalMethod, "Goo(d)").WithArguments("Goo"));
        }

        [Fact]
        public void CS1981WRN_IsDynamicIsConfusing()
        {
            var text = @"
public class D : C { }
public class C
{
    public static int Main()
    {
        // is dynamic
        bool bi = 123 is dynamic;            
        // dynamicType is valueType
        dynamic i2 = 123;
        bi = i2 is int;
        // dynamicType is refType
        dynamic c = new D();
        bi = c is C;
        dynamic c2 = new C();
        bi = c is C;

        // valueType as dynamic
        int i = 123 as dynamic;
        // refType as dynamic
        dynamic c3 = new D() as dynamic;
        // dynamicType as dynamic
        dynamic s = ""asd"";
        string s2 = s as dynamic;
        // default(dynamic)
        dynamic d = default(dynamic); 
        // dynamicType as valueType : generate error
        int k = i2 as int;
        // dynamicType as refType            
        C c4 = c3 as C;

        return 0;
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (7,19): warning CS1981: Using 'is' to test compatibility with 'dynamic'
                // is essentially identical to testing compatibility with 'Object' and will
                // succeed for all non-null values
                Diagnostic(ErrorCode.WRN_IsDynamicIsConfusing, "123 is dynamic").WithArguments("is", "dynamic", "Object"),
                // (7,19): warning CS0183: The given expression is always of the provided ('dynamic') type
                Diagnostic(ErrorCode.WRN_IsAlwaysTrue, "123 is dynamic").WithArguments("dynamic"),
                // (27,17): error CS0077: The as operator must be used with a reference type
                // or nullable type ('int' is a non-nullable value type)
                Diagnostic(ErrorCode.ERR_AsMustHaveReferenceType, "i2 as int").WithArguments("int"),
                // (26,17): warning CS0219: The variable 'd' is assigned but its value is never used
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "d").WithArguments("d"));
        }

        [Fact]
        public void CS7003ERR_UnexpectedUnboundGenericName()
        {
            var text = @"
class C<T>
{
    void M(System.Type t)
    {
        M(typeof(C<C<>>)); //unbound inside bound
        M(typeof(C<>[])); //array of unbound
        M(typeof(C<>.D<int>)); //unbound containing bound
        M(typeof(C<int>.D<>)); //bound containing unbound
        M(typeof(D<,>[])); //multiple type parameters
    }

    class D<U> { }
}

class D<T, U> {}";

            // NOTE: Dev10 reports CS1031 (type expected) - CS7003 is new.
            CreateCompilation(text).VerifyDiagnostics(
                // (6,20): error CS7003: Unexpected use of an unbound generic name
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "C<>"),
                // (7,18): error CS7003: Unexpected use of an unbound generic name
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "C<>"),
                // (8,18): error CS7003: Unexpected use of an unbound generic name
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "C<>"),
                // (9,25): error CS7003: Unexpected use of an unbound generic name
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "D<>"),
                // (10,18): error CS7003: Unexpected use of an unbound generic name
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "D<,>"));
        }

        [Fact]
        public void CS7003ERR_UnexpectedUnboundGenericName_Nested()
        {
            var text = @"
class Outer<T>
{
    public static void Print()
    {
        System.Console.WriteLine(typeof(Inner<>));
        System.Console.WriteLine(typeof(Inner<T>));
        System.Console.WriteLine(typeof(Inner<int>));

        System.Console.WriteLine(typeof(Outer<>.Inner<>));
        System.Console.WriteLine(typeof(Outer<>.Inner<T>)); //CS7003
        System.Console.WriteLine(typeof(Outer<>.Inner<int>)); //CS7003

        System.Console.WriteLine(typeof(Outer<T>.Inner<>)); //CS7003
        System.Console.WriteLine(typeof(Outer<T>.Inner<T>));
        System.Console.WriteLine(typeof(Outer<T>.Inner<int>));

        System.Console.WriteLine(typeof(Outer<int>.Inner<>)); //CS7003
        System.Console.WriteLine(typeof(Outer<int>.Inner<T>));
        System.Console.WriteLine(typeof(Outer<int>.Inner<int>));
    }

    class Inner<U> { }
}";

            // NOTE: Dev10 reports CS1031 (type expected) - CS7003 is new.
            CreateCompilation(text).VerifyDiagnostics(
                // (11,41): error CS7003: Unexpected use of an unbound generic name
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "Outer<>"),
                // (12,41): error CS7003: Unexpected use of an unbound generic name
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "Outer<>"),
                // (14,50): error CS7003: Unexpected use of an unbound generic name
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "Inner<>"),
                // (18,52): error CS7003: Unexpected use of an unbound generic name
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "Inner<>"));
        }

        [Fact(), WorkItem(529583, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529583")]
        public void CS7003ERR_UnexpectedUnboundGenericName_Attributes()
        {
            var text = @"
using System;

class Outer<T>
{
    public class Inner<U>
    {

    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class Attr : Attribute
{
    public Attr(Type t)
    {
    }
}

[Attr(typeof(Outer<>.Inner<>))]
[Attr(typeof(Outer<int>.Inner<>))]
[Attr(typeof(Outer<>.Inner<int>))]
[Attr(typeof(Outer<int>.Inner<int>))]
public class Test
{
    public static void Main()
    {
    }
}";

            // NOTE: Dev10 reports CS1031 (type expected) - CS7003 is new.
            CreateCompilation(text).VerifyDiagnostics(
                // (21,25): error CS7003: Unexpected use of an unbound generic name
                // [Attr(typeof(Outer<int>.Inner<>))]
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "Inner<>"),
                // (22,14): error CS7003: Unexpected use of an unbound generic name
                // [Attr(typeof(Outer<>.Inner<int>))]
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "Outer<>"));
        }

        [Fact]
        public void CS7013WRN_MetadataNameTooLong()
        {
            var text = @"
namespace Namespace1.Namespace2
{
    public interface I<T>
    {
        void Goo();
    }

    public class OuterGenericClass<T, S>
    {
        public class NestedClass : OuterGenericClass<NestedClass, NestedClass> { }

        public class C : I<NestedClass.NestedClass.NestedClass.NestedClass.NestedClass>
        {
            void I<NestedClass.NestedClass.NestedClass.NestedClass.NestedClass>.Goo()
            {
            }
        }
    }
}
";
            // This error will necessarily have a very long error string.
            CreateCompilation(text).VerifyEmitDiagnostics(
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "Goo").WithArguments("Namespace1.Namespace2.I<Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass,Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass>.NestedClass,Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass,Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass,Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass>.NestedClass,Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass,Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass,Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass,Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass>.NestedClass,Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass,Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass,Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass>.NestedClass,Namespace1.Namespace2.OuterGenericClass<Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass,Namespace1.Namespace2.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.Goo"));
        }

        #endregion

        #region shotgun tests

        [Fact]
        public void DelegateCreationBad()
        {
            var text = @"
namespace CSSample
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        delegate void D1();
        delegate void D2();

        delegate int D3(int x);

        static D1 d1;
        static D2 d2;
        static D3 d3;

        internal virtual void V() { }
        void M() { }
        static void S() { }

        static int M2(int x) { return x; }

        static void F(Program p)
        {
            // Error cases
            d1 = new D1(2 + 2);
            d1 = new D1(d3);
            d1 = new D1(2, 3);
            d1 = new D1(x: 3);
            d1 = new D1(M2);
        }
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (28,25): error CS0149: Method name expected
                //             d1 = new D1(2 + 2);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "2 + 2").WithLocation(28, 25),
                // (29,18): error CS0123: No overload for 'Program.D3.Invoke(int)' matches delegate 'Program.D1'
                //             d1 = new D1(d3);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new D1(d3)").WithArguments("CSSample.Program.D3.Invoke(int)", "CSSample.Program.D1").WithLocation(29, 18),
                // (30,25): error CS0149: Method name expected
                //             d1 = new D1(2, 3);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "2, 3").WithLocation(30, 25),
                // (31,28): error CS0149: Method name expected
                //             d1 = new D1(x: 3);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "3").WithLocation(31, 28),
                // (32,18): error CS0123: No overload for 'M2' matches delegate 'Program.D1'
                //             d1 = new D1(M2);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new D1(M2)").WithArguments("M2", "CSSample.Program.D1").WithLocation(32, 18),
                // (16,19): warning CS0169: The field 'Program.d2' is never used
                //         static D2 d2;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "d2").WithArguments("CSSample.Program.d2").WithLocation(16, 19),
                // (17,19): warning CS0649: Field 'Program.d3' is never assigned to, and will always have its default value null
                //         static D3 d3;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "d3").WithArguments("CSSample.Program.d3", "null").WithLocation(17, 19));
        }

        [Fact, WorkItem(7359, "https://github.com/dotnet/roslyn/issues/7359")]
        public void DelegateCreationWithRefOut()
        {
            var source = @"
using System;
public class Program
{
    static Func<T, T> Goo<T>(Func<T, T> t) { return t; }
    static Func<string, string> Bar = Goo<string>(x => x);
    static Func<string, string> BarP => Goo<string>(x => x);
    static T Id<T>(T id) => id;

    static void Test(Func<string, string> Baz)
    {
        var k = Bar;
        var z1 = new Func<string, string>(ref Bar); // compat
        var z2 = new Func<string, string>(ref Baz); // compat
        var z3 = new Func<string, string>(ref k); // compat
        var z4 = new Func<string, string>(ref x => x);
        var z5 = new Func<string, string>(ref Goo<string>(x => x));
        var z6 = new Func<string, string>(ref BarP); 
        var z7 = new Func<string, string>(ref new Func<string, string>(x => x));
        var z8 = new Func<string, string>(ref Program.BarP); 
        var z9 = new Func<string, string>(ref Program.Goo<string>(x => x));
        var z10 = new Func<string, string>(ref Id); // compat
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,47): error CS1510: A ref or out argument must be an assignable variable
                //         var z4 = new Func<string, string>(ref x => x);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x => x").WithLocation(16, 47),
                // (17,47): error CS1510: A ref or out argument must be an assignable variable
                //         var z5 = new Func<string, string>(ref Goo<string>(x => x));
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "Goo<string>(x => x)").WithLocation(17, 47),
                // (18,43): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         var z6 = new Func<string, string>(ref BarP); 
                Diagnostic(ErrorCode.ERR_RefProperty, "ref BarP").WithArguments("Program.BarP").WithLocation(18, 43),
                // (19,47): error CS1510: A ref or out argument must be an assignable variable
                //         var z7 = new Func<string, string>(ref new Func<string, string>(x => x));
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "new Func<string, string>(x => x)").WithLocation(19, 47),
                // (20,43): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         var z8 = new Func<string, string>(ref Program.BarP); 
                Diagnostic(ErrorCode.ERR_RefProperty, "ref Program.BarP").WithArguments("Program.BarP").WithLocation(20, 43),
                // (21,47): error CS1510: A ref or out argument must be an assignable variable
                //         var z9 = new Func<string, string>(ref Program.Goo<string>(x => x));
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "Program.Goo<string>(x => x)").WithLocation(21, 47));

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithStrictFeature()).VerifyDiagnostics(
                // (13,47): error CS0149: Method name expected
                //         var z1 = new Func<string, string>(ref Bar); // compat
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Bar").WithLocation(13, 47),
                // (14,47): error CS0149: Method name expected
                //         var z2 = new Func<string, string>(ref Baz); // compat
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Baz").WithLocation(14, 47),
                // (15,47): error CS0149: Method name expected
                //         var z3 = new Func<string, string>(ref k); // compat
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "k").WithLocation(15, 47),
                // (16,47): error CS1510: A ref or out argument must be an assignable variable
                //         var z4 = new Func<string, string>(ref x => x);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x => x").WithLocation(16, 47),
                // (17,47): error CS1510: A ref or out argument must be an assignable variable
                //         var z5 = new Func<string, string>(ref Goo<string>(x => x));
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "Goo<string>(x => x)").WithLocation(17, 47),
                // (18,47): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         var z6 = new Func<string, string>(ref BarP); 
                Diagnostic(ErrorCode.ERR_RefProperty, "BarP").WithArguments("Program.BarP").WithLocation(18, 47),
                // (19,47): error CS1510: A ref or out argument must be an assignable variable
                //         var z7 = new Func<string, string>(ref new Func<string, string>(x => x));
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "new Func<string, string>(x => x)").WithLocation(19, 47),
                // (20,47): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         var z8 = new Func<string, string>(ref Program.BarP); 
                Diagnostic(ErrorCode.ERR_RefProperty, "Program.BarP").WithArguments("Program.BarP").WithLocation(20, 47),
                // (21,47): error CS1510: A ref or out argument must be an assignable variable
                //         var z9 = new Func<string, string>(ref Program.Goo<string>(x => x));
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "Program.Goo<string>(x => x)").WithLocation(21, 47),
                // (22,48): error CS1657: Cannot pass 'Id' as a ref or out argument because it is a 'method group'
                //         var z10 = new Func<string, string>(ref Id); // compat
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "Id").WithArguments("Id", "method group").WithLocation(22, 48));
        }

        [Fact, WorkItem(7359, "https://github.com/dotnet/roslyn/issues/7359")]
        public void DelegateCreationWithRefOut_Parens()
        {
            // these are allowed in compat mode without the parenthesis
            // with parenthesis, it behaves like strict mode
            var source = @"
using System;
public class Program
{
    static Func<T, T> Goo<T>(Func<T, T> t) { return t; }
    static Func<string, string> Bar = Goo<string>(x => x);

    static T Id<T>(T id) => id;

    static void Test(Func<string, string> Baz)
    {
        var k = Bar;
        var z1 = new Func<string, string>(ref (Bar)); 
        var z2 = new Func<string, string>(ref (Baz)); 
        var z3 = new Func<string, string>(ref (k)); 
        var z10 = new Func<string, string>(ref (Id)); 
        // these all are still valid for compat mode, no errors should be reported for compat mode
        var z4 = new Func<string, string>(ref Bar); 
        var z5 = new Func<string, string>(ref Baz); 
        var z6 = new Func<string, string>(ref k); 
        var z7 = new Func<string, string>(ref Id); 
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,48): error CS0149: Method name expected
                //         var z1 = new Func<string, string>(ref (Bar)); 
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Bar").WithLocation(13, 48),
                // (14,48): error CS0149: Method name expected
                //         var z2 = new Func<string, string>(ref (Baz)); 
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Baz").WithLocation(14, 48),
                // (15,48): error CS0149: Method name expected
                //         var z3 = new Func<string, string>(ref (k)); 
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "k").WithLocation(15, 48),
                // (16,49): error CS1657: Cannot pass 'Id' as a ref or out argument because it is a 'method group'
                //         var z10 = new Func<string, string>(ref (Id)); 
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "Id").WithArguments("Id", "method group").WithLocation(16, 49));

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithStrictFeature()).VerifyDiagnostics(
                // (13,48): error CS0149: Method name expected
                //         var z1 = new Func<string, string>(ref (Bar)); 
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Bar").WithLocation(13, 48),
                // (14,48): error CS0149: Method name expected
                //         var z2 = new Func<string, string>(ref (Baz)); 
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Baz").WithLocation(14, 48),
                // (15,48): error CS0149: Method name expected
                //         var z3 = new Func<string, string>(ref (k)); 
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "k").WithLocation(15, 48),
                // (16,49): error CS1657: Cannot pass 'Id' as a ref or out argument because it is a 'method group'
                //         var z10 = new Func<string, string>(ref (Id)); 
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "Id").WithArguments("Id", "method group").WithLocation(16, 49),
                // (18,47): error CS0149: Method name expected
                //         var z4 = new Func<string, string>(ref Bar); 
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Bar").WithLocation(18, 47),
                // (19,47): error CS0149: Method name expected
                //         var z5 = new Func<string, string>(ref Baz); 
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Baz").WithLocation(19, 47),
                // (20,47): error CS0149: Method name expected
                //         var z6 = new Func<string, string>(ref k); 
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "k").WithLocation(20, 47),
                // (21,47): error CS1657: Cannot pass 'Id' as a ref or out argument because it is a 'method group'
                //         var z7 = new Func<string, string>(ref Id); 
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "Id").WithArguments("Id", "method group").WithLocation(21, 47));
        }

        [Fact, WorkItem(7359, "https://github.com/dotnet/roslyn/issues/7359")]
        public void DelegateCreationWithRefOut_MultipleArgs()
        {
            var source = @"
using System;
public class Program
{
    static Func<string, string> BarP => null;
    static void Test(Func<string, string> Baz)
    {
        var a = new Func<string, string>(ref Baz, Baz.Invoke);
        var b = new Func<string, string>(Baz, ref Baz.Invoke);
        var c = new Func<string, string>(ref Baz, ref Baz.Invoke);
        var d = new Func<string, string>(ref BarP, BarP.Invoke);
        var e = new Func<string, string>(BarP, ref BarP.Invoke);
        var f = new Func<string, string>(ref BarP, ref BarP.Invoke);
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,46): error CS0149: Method name expected
                //         var a = new Func<string, string>(ref Baz, Baz.Invoke);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Baz, Baz.Invoke").WithLocation(8, 46),
                // (9,42): error CS0149: Method name expected
                //         var b = new Func<string, string>(Baz, ref Baz.Invoke);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Baz, ref Baz.Invoke").WithLocation(9, 42),
                // (10,46): error CS0149: Method name expected
                //         var c = new Func<string, string>(ref Baz, ref Baz.Invoke);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Baz, ref Baz.Invoke").WithLocation(10, 46),
                // (11,42): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         var d = new Func<string, string>(ref BarP, BarP.Invoke);
                Diagnostic(ErrorCode.ERR_RefProperty, "ref BarP").WithArguments("Program.BarP").WithLocation(11, 42),
                // (11,46): error CS0149: Method name expected
                //         var d = new Func<string, string>(ref BarP, BarP.Invoke);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "BarP, BarP.Invoke").WithLocation(11, 46),
                // (12,42): error CS0149: Method name expected
                //         var e = new Func<string, string>(BarP, ref BarP.Invoke);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "BarP, ref BarP.Invoke").WithLocation(12, 42),
                // (13,42): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         var f = new Func<string, string>(ref BarP, ref BarP.Invoke);
                Diagnostic(ErrorCode.ERR_RefProperty, "ref BarP").WithArguments("Program.BarP").WithLocation(13, 42),
                // (13,46): error CS0149: Method name expected
                //         var f = new Func<string, string>(ref BarP, ref BarP.Invoke);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "BarP, ref BarP.Invoke").WithLocation(13, 46)
                );
        }

        [WorkItem(538430, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538430")]
        [Fact]
        public void NestedGenericAccessibility()
        {
            var text = @"
public class C<T>
{
}
public class E
{
   class D : C<D>
   {
   }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new ErrorDescription[] {
            });
        }

        [WorkItem(542419, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542419")]
        [Fact]
        public void EmptyAngleBrackets()
        {
            var text = @"
class Program
{
    int f;
    int P { get; set; }
    int M() { return 0; }
    interface I { }
    class C { }
    struct S { }
    delegate void D();

    void Test(object p)
    {
        int l = 0;

        Test(l<>);
        Test(p<>);

        Test(f<>);
        Test(P<>);
        Test(M<>());

        Test(this.f<>);
        Test(this.P<>);
        Test(this.M<>());

        System.Func<int> m;
        
        m = M<>;
        m = this.M<>;

        I<> i1 = null;
        C<> c1 = new C();
        C c2 = new C<>();
        S<> s1 = new S();
        S s2 = new S<>();
        D<> d1 = null;

        Program.I<> i2 = null;
        Program.C<> c3 = new Program.C();
        Program.C c4 = new Program.C<>();
        Program.S<> s3 = new Program.S();
        Program.S s4 = new Program.S<>();
        Program.D<> d2 = null;

        Test(default(I<>));
        Test(default(C<>));
        Test(default(S<>));

        Test(default(Program.I<>));
        Test(default(Program.C<>));
        Test(default(Program.S<>));

        string s;

        s = typeof(I<>).Name;
        s = typeof(C<>).Name;
        s = typeof(S<>).Name;
        
        s = typeof(Program.I<>).Name;
        s = typeof(Program.C<>).Name;
        s = typeof(Program.S<>).Name;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (16,14): error CS0307: The variable 'l' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "l<>").WithArguments("l", "variable"),
                // (17,14): error CS0307: The variable 'object' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "p<>").WithArguments("object", "variable"),
                // (19,14): error CS0307: The field 'Program.f' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "f<>").WithArguments("Program.f", "field"),
                // (20,14): error CS0307: The property 'Program.P' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "P<>").WithArguments("Program.P", "property"),
                // (21,14): error CS0308: The non-generic method 'Program.M()' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "M<>").WithArguments("Program.M()", "method"),
                // (23,19): error CS0307: The field 'Program.f' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "f<>").WithArguments("Program.f", "field"),
                // (24,19): error CS0307: The property 'Program.P' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "P<>").WithArguments("Program.P", "property"),
                // (25,19): error CS0308: The non-generic method 'Program.M()' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "M<>").WithArguments("Program.M()", "method"),
                // (29,13): error CS0308: The non-generic method 'Program.M()' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "M<>").WithArguments("Program.M()", "method"),
                // (30,18): error CS0308: The non-generic method 'Program.M()' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "M<>").WithArguments("Program.M()", "method"),
                // (32,9): error CS0308: The non-generic type 'Program.I' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "I<>").WithArguments("Program.I", "type"),
                // (33,9): error CS0308: The non-generic type 'Program.C' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<>").WithArguments("Program.C", "type"),
                // (34,20): error CS0308: The non-generic type 'Program.C' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<>").WithArguments("Program.C", "type"),
                // (35,9): error CS0308: The non-generic type 'Program.S' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "S<>").WithArguments("Program.S", "type"),
                // (36,20): error CS0308: The non-generic type 'Program.S' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "S<>").WithArguments("Program.S", "type"),
                // (37,9): error CS0308: The non-generic type 'Program.D' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "D<>").WithArguments("Program.D", "type"),
                // (39,17): error CS0308: The non-generic type 'Program.I' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "I<>").WithArguments("Program.I", "type"),
                // (40,17): error CS0308: The non-generic type 'Program.C' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<>").WithArguments("Program.C", "type"),
                // (41,36): error CS0308: The non-generic type 'Program.C' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<>").WithArguments("Program.C", "type"),
                // (42,17): error CS0308: The non-generic type 'Program.S' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "S<>").WithArguments("Program.S", "type"),
                // (43,36): error CS0308: The non-generic type 'Program.S' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "S<>").WithArguments("Program.S", "type"),
                // (44,17): error CS0308: The non-generic type 'Program.D' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "D<>").WithArguments("Program.D", "type"),
                // (46,22): error CS0308: The non-generic type 'Program.I' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "I<>").WithArguments("Program.I", "type"),
                // (47,22): error CS0308: The non-generic type 'Program.C' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<>").WithArguments("Program.C", "type"),
                // (48,22): error CS0308: The non-generic type 'Program.S' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "S<>").WithArguments("Program.S", "type"),
                // (50,30): error CS0308: The non-generic type 'Program.I' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "I<>").WithArguments("Program.I", "type"),
                // (51,30): error CS0308: The non-generic type 'Program.C' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<>").WithArguments("Program.C", "type"),
                // (52,30): error CS0308: The non-generic type 'Program.S' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "S<>").WithArguments("Program.S", "type"),
                // (56,20): error CS0308: The non-generic type 'Program.I' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "I<>").WithArguments("Program.I", "type"),
                // (57,20): error CS0308: The non-generic type 'Program.C' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<>").WithArguments("Program.C", "type"),
                // (58,20): error CS0308: The non-generic type 'Program.S' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "S<>").WithArguments("Program.S", "type"),
                // (60,28): error CS0308: The non-generic type 'Program.I' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "I<>").WithArguments("Program.I", "type"),
                // (61,28): error CS0308: The non-generic type 'Program.C' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<>").WithArguments("Program.C", "type"),
                // (62,28): error CS0308: The non-generic type 'Program.S' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "S<>").WithArguments("Program.S", "type"),
                // (4,9): warning CS0649: Field 'Program.f' is never assigned to, and will always have its default value 0
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "f").WithArguments("Program.f", "0")
                );
        }

        [WorkItem(542419, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542419")]
        [Fact]
        public void EmptyAngleBrackets_Events()
        {
            var text = @"
class Program
{
    event System.Action E;
    event System.Action F { add { } remove { } }

    void Test<T>(T p)
    {
        Test(E<>);
        Test(this.E<>);

        E<> += null; //parse error
        F<> += null; //parse error

        this.E<> += null; //parse error
        this.F<> += null; //parse error
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // Parser

                // (12,11): error CS1525: Invalid expression term '>'
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ">").WithArguments(">"),
                // (12,13): error CS1525: Invalid expression term '+='
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "+=").WithArguments("+="),
                // (13,11): error CS1525: Invalid expression term '>'
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ">").WithArguments(">"),
                // (13,13): error CS1525: Invalid expression term '+='
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "+=").WithArguments("+="),
                // (15,16): error CS1525: Invalid expression term '>'
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ">").WithArguments(">"),
                // (15,18): error CS1525: Invalid expression term '+='
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "+=").WithArguments("+="),
                // (16,16): error CS1525: Invalid expression term '>'
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ">").WithArguments(">"),

                // Binder

                // (16,18): error CS1525: Invalid expression term '+='
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "+=").WithArguments("+="),
                // (9,14): error CS0307: The event 'Program.E' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "E<>").WithArguments("Program.E", "event"),
                // (10,19): error CS0307: The event 'Program.E' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "E<>").WithArguments("Program.E", "event"),
                // (13,9): error CS0079: The event 'Program.F' can only appear on the left hand side of += or -=
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("Program.F"),
                // (16,14): error CS0079: The event 'Program.F' can only appear on the left hand side of += or -=
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("Program.F"));
        }

        [WorkItem(542419, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542419")]
        [WorkItem(542679, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542679")]
        [Fact]
        public void EmptyAngleBrackets_TypeParameters()
        {
            var text = @"
class Program
{
    void Test<T>(T p)
    {
        Test(default(T<>));
        string s = typeof(T<>).Name;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6, 24): error CS0307: The type parameter 'T' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "T<>").WithArguments("T", "type parameter"),
                // (7,27): error CS0307: The type parameter 'T' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "T<>").WithArguments("T", "type parameter"));
        }

        [Fact, WorkItem(542796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542796")]
        public void EmptyAngleBrackets_TypeWithCorrectArity()
        {
            var text = @"
class C<T>
{
    static void M()
    {
        C<>.M();
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,9): error CS0305: Using the generic type 'C<T>' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "C<>").WithArguments("C<T>", "type", "1"));
        }

        [Fact, WorkItem(542796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542796")]
        public void EmptyAngleBrackets_MethodWithCorrectArity()
        {
            var text = @"
class C
{
    static void M<T>()
    {
        M<>();
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,9): error CS0305: Using the generic method group 'M' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "M<>").WithArguments("M", "method group", "1"));
        }

        [Fact, WorkItem(542796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542796")]
        public void EmptyAngleBrackets_QualifiedTypeWithCorrectArity()
        {
            var text = @"
class A
{
    class C<T>
    {
        static void M()
        {
            A.C<>.M();
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,15): error CS0305: Using the generic type 'A.C<T>' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "C<>").WithArguments("A.C<T>", "type", "1"));
        }

        [Fact, WorkItem(542796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542796")]
        public void EmptyAngleBrackets_QualifiedMethodWithCorrectArity()
        {
            var text = @"
class C
{
    static void M<T>()
    {
        C.M<>();
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,9): error CS0305: Using the generic method group 'M' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "C.M<>").WithArguments("M", "method group", "1"));
        }

        [Fact]
        public void NamesTooLong()
        {
            var longE = new String('e', 1024);

            var builder = new System.Text.StringBuilder();

            builder.Append(@"
class C
{
");
            builder.AppendFormat("int {0}1;\n", longE);
            builder.AppendFormat("event System.Action {0}2;\n", longE);
            builder.AppendFormat("public void {0}3() {{ }}\n", longE);
            builder.AppendFormat("public void goo(int {0}4) {{ }}\n", longE);
            builder.AppendFormat("public string {0}5 {{ get; set; }}\n", longE);

            builder.AppendLine(@"
}
");
            CreateCompilation(builder.ToString(), null, TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)).VerifyEmitDiagnostics(
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, longE + 2).WithArguments(longE + 2),  //event
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, longE + 2).WithArguments(longE + 2),  //backing field
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, longE + 2).WithArguments("add_" + longE + 2),  //accessor
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, longE + 2).WithArguments("remove_" + longE + 2),  //accessor
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, longE + 3).WithArguments(longE + 3),
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, longE + 4).WithArguments(longE + 4),
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, longE + 5).WithArguments(longE + 5),
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, longE + 5).WithArguments("<" + longE + 5 + ">k__BackingField"),
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "get").WithArguments("get_" + longE + 5),
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "set").WithArguments("set_" + longE + 5),
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, longE + 1).WithArguments(longE + 1)
            );
        }
        #endregion

        #region regression tests

        [WorkItem(541605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541605")]
        [Fact]
        public void CS0019ERR_ImplicitlyTypedVariableAssignedNullCoalesceExpr()
        {
            CreateCompilation(@"
class Test
{
    public static void Main()
    {
        var p = null ?? null; //CS0019
    }
}
").VerifyDiagnostics(
                // error CS0019: Operator '??' cannot be applied to operands of type '<null>' and '<null>'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "null ?? null").WithArguments("??", "<null>", "<null>"));
        }

        [WorkItem(528577, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528577")]
        [Fact(Skip = "528577")]
        public void CS0122ERR_InaccessibleGenericType()
        {
            CreateCompilation(@"
public class Top<T>
{
    class Outer<U>
    {
    }
}

public class MyClass
{
    public static void Main()
    {
        var test = new Top<int>.Outer<string>();
    }
}
").VerifyDiagnostics(
                // (13,33): error CS0122: 'Top<int>.Outer<string>' is inaccessible due to its protection level
                //          var test = new Top<int>.Outer<string>();
                Diagnostic(ErrorCode.ERR_BadAccess, "new Top<int>.Outer<string>()").WithArguments("Top<int>.Outer<string>"));
        }

        [WorkItem(528591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528591")]
        [Fact]
        public void CS0121ERR_IncorrectErrorSpan1()
        {
            CreateCompilation(@"
class Test
{
    public static void Method1(int a, long b)
    {
    }

    public static void Method1(long a, int b)
    {
    }

    public static void Main()
    {
        Method1(10, 20); //CS0121
    }
}
").VerifyDiagnostics(
                // (14,9): error CS0121: The call is ambiguous between the following methods or properties: 'Test.Method1(int, long)' and 'Test.Method1(long, int)'
                //          Method1(10, 20)
                Diagnostic(ErrorCode.ERR_AmbigCall, "Method1").WithArguments("Test.Method1(int, long)", "Test.Method1(long, int)"));
        }

        [WorkItem(528592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528592")]
        [Fact]
        public void CS0121ERR_IncorrectErrorSpan2()
        {
            CreateCompilation(@"
public class Class1
{
    public Class1(int a, long b)
    {
    }

    public Class1(long a, int b)
    {
    }
}

class Test
{
    public static void Main()
    {
        var i1 = new Class1(10, 20);  //CS0121
    }
}
").VerifyDiagnostics(
                // (17,18): error CS0121: The call is ambiguous between the following methods or properties: 'Class1.Class1(int, long)' and 'Class1.Class1(long, int)'
                //          new Class1(10, 20)
                Diagnostic(ErrorCode.ERR_AmbigCall, "Class1").WithArguments("Class1.Class1(int, long)", "Class1.Class1(long, int)"));
        }

        [WorkItem(542468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542468")]
        [Fact]
        public void CS1513ERR_RbraceExpected_DevDiv9741()
        {
            var text = @"
class Program
{
    private delegate string D();
    static void Main(string[] args)
    {
        D d = delegate
        {
            .ToString();
        };
    }
} 
";
            // Used to assert.
            CreateCompilation(text).VerifyDiagnostics(
    // (8,10): error CS1513: } expected
    //         {
    Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(8, 10),
    // (9,14): error CS0120: An object reference is required for the non-static field, method, or property 'object.ToString()'
    //             .ToString();
    Diagnostic(ErrorCode.ERR_ObjectRequired, "ToString").WithArguments("object.ToString()").WithLocation(9, 14),
    // (7,15): error CS1643: Not all code paths return a value in anonymous method of type 'Program.D'
    //         D d = delegate
    Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "delegate").WithArguments("anonymous method", "Program.D").WithLocation(7, 15)
                );
        }

        [WorkItem(543473, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543473")]
        [Fact]
        public void CS0815ERR_CannotAssignLambdaExpressionToAnImplicitlyTypedLocalVariable()
        {
            var text =
@"class Program
{
    static void Main(string[] args)
    {
        var a1 = checked((a) => a);
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (5,13): error CS0815: Cannot assign lambda expression to an implicitly-typed variable
                //         var a1 = checked((a) => a);
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "a1 = checked((a) => a)").WithArguments("lambda expression"));
        }

        [Fact, WorkItem(543665, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543665")]
        public void CS0246ERR_SingleTypeNameNotFound_UndefinedTypeInDelegateSignature()
        {
            var text = @"
using System;
class Test
{
  static void Main()
  {
    var d = (Action<List<int>>)delegate(List<int> t) {};
  }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,41): error CS0246: The type or namespace name 'List<>' could not be found (are you missing a using directive or an assembly reference?)
                //     var d = (Action<List<int>>)delegate(List<int> t) {};
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "List<int>").WithArguments("List<>").WithLocation(7, 41),
                // (7,21): error CS0246: The type or namespace name 'List<>' could not be found (are you missing a using directive or an assembly reference?)
                //     var d = (Action<List<int>>)delegate(List<int> t) {};
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "List<int>").WithArguments("List<>").WithLocation(7, 21)
                );
        }

        [Fact]
        [WorkItem(633183, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633183")]
        public void CS0199ERR_RefReadonlyStatic_StaticFieldInitializer()
        {
            var source = @"
class Program
{
    Program(ref string s) { }
    static readonly Program Field1 = new Program(ref Program.Field2);
    static readonly string Field2 = """";
    static void Main() { }
}
";

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(633183, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633183")]
        public void CS0199ERR_RefReadonlyStatic_NestedStaticFieldInitializer()
        {
            var source = @"
class Program
{
    Program(ref string s) { }
    static readonly Program Field1 = new Program(ref Program.Field2);
    static readonly string Field2 = """";
    static void Main() { }

    class Inner
    {
        static readonly Program Field3 = new Program(ref Program.Field2);
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
    // (11,58): error CS0199: A static readonly field cannot be used as a ref or out value (except in a static constructor)
    //         static readonly Program Field3 = new Program(ref Program.Field2);
    Diagnostic(ErrorCode.ERR_RefReadonlyStatic, "Program.Field2").WithLocation(11, 58)
);
        }

        [Fact]
        public void BadYield_MultipleReasons()
        {
            var source = @"
using System.Collections.Generic;

class Program
{
    IEnumerable<int> Test()
    {
        try
        {
            try
            {
                yield return 11; // CS1626
            }
            catch
            {
                yield return 12; // CS1626
            }
            finally
            {
                yield return 13; // CS1625
            }
        }
        catch
        {
            try
            {
                yield return 21; // CS1626
            }
            catch
            {
                yield return 22; // CS1631
            }
            finally
            {
                yield return 23; // CS1625
            }
        }
        finally
        {
            try
            {
                yield return 31; // CS1625
            }
            catch
            {
                yield return 32; // CS1625
            }
            finally
            {
                yield return 33; // CS1625
            }
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (12,17): error CS1626: Cannot yield a value in the body of a try block with a catch clause
                //                 yield return 11; // CS1626
                Diagnostic(ErrorCode.ERR_BadYieldInTryOfCatch, "yield"),
                // (16,17): error CS1626: Cannot yield a value in the body of a try block with a catch clause
                //                 yield return 12; // CS1626
                Diagnostic(ErrorCode.ERR_BadYieldInTryOfCatch, "yield"),
                // (20,17): error CS1625: Cannot yield in the body of a finally clause
                //                 yield return 13; // CS1625
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield"),

                // (27,17): error CS1626: Cannot yield a value in the body of a try block with a catch clause
                //                 yield return 21; // CS1626
                Diagnostic(ErrorCode.ERR_BadYieldInTryOfCatch, "yield"),
                // (31,17): error CS1631: Cannot yield a value in the body of a catch clause
                //                 yield return 22; // CS1631
                Diagnostic(ErrorCode.ERR_BadYieldInCatch, "yield"),
                // (35,17): error CS1625: Cannot yield in the body of a finally clause
                //                 yield return 23; // CS1625
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield"),

                // (42,17): error CS1625: Cannot yield in the body of a finally clause
                //                 yield return 31; // CS1625
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield"),
                // (46,17): error CS1625: Cannot yield in the body of a finally clause
                //                 yield return 32; // CS1625
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield"),
                // (50,17): error CS1625: Cannot yield in the body of a finally clause
                //                 yield return 33; // CS1625
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield"));
        }

        [Fact]
        public void BadYield_Lambda()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    IEnumerable<int> Test()
    {
        try
        {
        }
        finally
        {
            Action a = () => { yield break; };
            Action b = () =>
            {
                try
                {
                }
                finally
                {
                    yield break;
                }
            };
        }

        yield break;
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (14,32): error CS1621: The yield statement cannot be used inside an anonymous method or lambda expression
                //             Action a = () => { yield break; };
                Diagnostic(ErrorCode.ERR_YieldInAnonMeth, "yield"),

                // CONSIDER: ERR_BadYieldInFinally is redundant, but matches dev11.

                // (22,21): error CS1625: Cannot yield in the body of a finally clause
                //                     yield break;
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield"),
                // (22,21): error CS1621: The yield statement cannot be used inside an anonymous method or lambda expression
                //                     yield break;
                Diagnostic(ErrorCode.ERR_YieldInAnonMeth, "yield"));
        }

        #endregion

        [Fact]
        public void Bug528147()
        {
            var text = @"
using System;
 
interface I<T> { }
 
class A
{
    private class B { }
    public class C : I<B>
    {
    }
}
 
class Program
{
    delegate void D(A.C x);
 
    static void M<T>(I<T> c)
    {
        Console.WriteLine(""I"");
    }

    static void Main()
    {
        D d = M;
        d(null);
    }
}
";

            CreateCompilation(text).VerifyDiagnostics(
// (25,15): error CS0122: 'Program.M<A.B>(I<A.B>)' is inaccessible due to its protection level
//         D d = M;
Diagnostic(ErrorCode.ERR_BadAccess, "M").WithArguments("Program.M<A.B>(I<A.B>)")
                );
        }

        [WorkItem(630799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/630799")]
        [Fact]
        public void Bug630799()
        {
            var text = @"
using System;
 
class Program
{
    static void Goo<T,S>() where T : S where S : Exception
    {
        try
        {
        }
        catch(S e)
        {
        }
        catch(T e)
        {
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
    // (14,15): error CS0160: A previous catch clause already catches all exceptions of this or of a super type ('S')
    //         catch(T e)
    Diagnostic(ErrorCode.ERR_UnreachableCatch, "T").WithArguments("S").WithLocation(14, 15),
    // (11,17): warning CS0168: The variable 'e' is declared but never used
    //         catch(S e)
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(11, 17),
    // (14,17): warning CS0168: The variable 'e' is declared but never used
    //         catch(T e)
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(14, 17)
    );
        }

        [Fact]
        public void ConditionalMemberAccess001()
        {
            var text = @"
class Program
{
    public int P1
    {
        set { }
    }

    public void V() { }

    static void Main(string[] args)
    {
        var x = 123 ?.ToString();

        var p = new Program();
        var x1 = p.P1 ?.ToString();
        var x2 = p.V() ?.ToString();
        var x3 = p.V ?.ToString();
        var x4 = ()=> { return 1; } ?.ToString();
    }
}
";
            CreateCompilationWithMscorlib45(text).VerifyDiagnostics(
    // (13,21): error CS0023: Operator '?' cannot be applied to operand of type 'int'
    //         var x = 123 ?.ToString();
    Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "int").WithLocation(13, 21),
    // (16,18): error CS0154: The property or indexer 'Program.P1' cannot be used in this context because it lacks the get accessor
    //         var x1 = p.P1 ?.ToString();
    Diagnostic(ErrorCode.ERR_PropertyLacksGet, "p.P1").WithArguments("Program.P1").WithLocation(16, 18),
    // (17,24): error CS0023: Operator '?' cannot be applied to operand of type 'void'
    //         var x2 = p.V() ?.ToString();
    Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "void").WithLocation(17, 24),
    // (18,20): error CS0119: 'Program.V()' is a method, which is not valid in the given context
    //         var x3 = p.V ?.ToString();
    Diagnostic(ErrorCode.ERR_BadSKunknown, "V").WithArguments("Program.V()", "method").WithLocation(18, 20),
    // (19,18): error CS0023: Operator '?' cannot be applied to operand of type 'lambda expression'
    //         var x4 = ()=> { return 1; } ?.ToString();
    Diagnostic(ErrorCode.ERR_BadUnaryOp, "()=> { return 1; } ?.ToString()").WithArguments("?", "lambda expression").WithLocation(19, 18)
               );
        }

        [Fact]
        public void ConditionalMemberAccess002_notIn5()
        {
            var text = @"
class Program
{
    public int? P1
    {
        get { return null; }
    }  

    public void V() { }

    static void Main(string[] args)
    {
        var p = new Program();
        var x1 = p.P1 ?.ToString;
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5)).VerifyDiagnostics(
                // (14,18): error CS8026: Feature 'null propagation operator' is not available in C# 5. Please use language version 6 or greater.
                //         var x1 = p.P1 ?.ToString;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "p.P1 ?.ToString").WithArguments("null propagating operator", "6").WithLocation(14, 18),
                // (14,23): error CS0023: Operator '?' cannot be applied to operand of type 'method group'
                //         var x1 = p.P1 ?.ToString;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "method group").WithLocation(14, 23)
                );
        }

        [Fact]
        public void ConditionalMemberAccess002()
        {
            var text = @"
class Program
{
    public int? P1
    {
        get { return null; }
    }  

    public void V() { }

    static void Main(string[] args)
    {
        var p = new Program();
        var x1 = p.P1 ?.ToString;
    }
}
";
            CreateCompilationWithMscorlib45(text).VerifyDiagnostics(
    // (14,23): error CS0023: Operator '?' cannot be applied to operand of type 'method group'
    //         var x1 = p.P1 ?.ToString;
    Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "method group").WithLocation(14, 23)
               );
        }


        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(23009, "https://github.com/dotnet/roslyn/issues/23009")]
        public void ConditionalElementAccess001()
        {
            var text = @"
class Program
{
    public int P1
    {
        set { }
    }

    public void V() 
    { 
        var x6 = base?.ToString();
    }

    static void Main(string[] args)
    {
        var x = 123 ?[1,2];

        var p = new Program();
        var x1 = p.P1 ?[1,2];
        var x2 = p.V() ?[1,2];
        var x3 = p.V ?[1,2];
        var x4 = ()=> { return 1; } ?[1,2];

        var x5 = null?.ToString();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(text).VerifyDiagnostics(
    // (11,18): error CS0175: Use of keyword 'base' is not valid in this context
    //         var x6 = base?.ToString();
    Diagnostic(ErrorCode.ERR_BaseIllegal, "base").WithLocation(11, 18),
    // (16,21): error CS0023: Operator '?' cannot be applied to operand of type 'int'
    //         var x = 123 ?[1,2];
    Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "int").WithLocation(16, 21),
    // (19,18): error CS0154: The property or indexer 'Program.P1' cannot be used in this context because it lacks the get accessor
    //         var x1 = p.P1 ?[1,2];
    Diagnostic(ErrorCode.ERR_PropertyLacksGet, "p.P1").WithArguments("Program.P1").WithLocation(19, 18),
    // (20,24): error CS0023: Operator '?' cannot be applied to operand of type 'void'
    //         var x2 = p.V() ?[1,2];
    Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "void").WithLocation(20, 24),
    // (21,20): error CS0119: 'Program.V()' is a method, which is not valid in the given context
    //         var x3 = p.V ?[1,2];
    Diagnostic(ErrorCode.ERR_BadSKunknown, "V").WithArguments("Program.V()", "method").WithLocation(21, 20),
    // (22,18): error CS0023: Operator '?' cannot be applied to operand of type 'lambda expression'
    //         var x4 = ()=> { return 1; } ?[1,2];
    Diagnostic(ErrorCode.ERR_BadUnaryOp, "()=> { return 1; } ?[1,2]").WithArguments("?", "lambda expression").WithLocation(22, 18),
    // (24,22): error CS0023: Operator '?' cannot be applied to operand of type '<null>'
    //         var x5 = null?.ToString();
    Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "<null>").WithLocation(24, 22)
    );
            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().First();

            Assert.Equal("base?.ToString()", node.ToString());

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: ?, IsInvalid) (Syntax: 'base?.ToString()')
  Operation: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsInvalid) (Syntax: 'base')
  WhenNotNull: 
    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
      Instance Receiver: 
        IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'base')
      Arguments(0)
");
        }

        [Fact]
        [WorkItem(976765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/976765")]
        public void ConditionalMemberAccessPtr()
        {
            var text = @"
using System;
 
class Program
{
    unsafe static void Main()
    {
        IntPtr? intPtr = null;
        var p = intPtr?.ToPointer();
    }
}

";
            CreateCompilationWithMscorlib45(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
    // (9,23): error CS0023: Operator '?' cannot be applied to operand of type 'void*'
    //         var p = intPtr?.ToPointer();
    Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "void*").WithLocation(9, 23)
               );
        }

        [Fact]
        [WorkItem(991490, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991490")]
        public void ConditionalMemberAccessExprLambda()
        {
            var text = @"
using System;
using System.Linq.Expressions;

class Program
{
    static void M<T>(T x)
    {
        Expression<Func<string>> s = () => x?.ToString();
        Expression<Func<char?>> c = () => x.ToString()?[0];
        Expression<Func<int?>> c1 = () => x.ToString()?.Length;

        Expression<Func<int?>> c2 = () => x?.ToString()?.Length;
}

    static void Main()
    {
        M((string)null);
    }
}
";
            CreateCompilationWithMscorlib45(text, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }, options: TestOptions.ReleaseDll).VerifyDiagnostics(
    // (9,44): error CS8072: An expression tree lambda may not contain a null propagating operator.
    //         Expression<Func<string>> s = () => x?.ToString();
    Diagnostic(ErrorCode.ERR_NullPropagatingOpInExpressionTree, "x?.ToString()").WithLocation(9, 44),
    // (10,43): error CS8072: An expression tree lambda may not contain a null propagating operator.
    //         Expression<Func<char?>> c = () => x.ToString()?[0];
    Diagnostic(ErrorCode.ERR_NullPropagatingOpInExpressionTree, "x.ToString()?[0]").WithLocation(10, 43),
    // (11,43): error CS8072: An expression tree lambda may not contain a null propagating operator.
    //         Expression<Func<int?>> c1 = () => x.ToString()?.Length;
    Diagnostic(ErrorCode.ERR_NullPropagatingOpInExpressionTree, "x.ToString()?.Length").WithLocation(11, 43),
    // (13,43): error CS8072: An expression tree lambda may not contain a null propagating operator.
    //         Expression<Func<int?>> c2 = () => x?.ToString()?.Length;
    Diagnostic(ErrorCode.ERR_NullPropagatingOpInExpressionTree, "x?.ToString()?.Length").WithLocation(13, 43),
    // (13,45): error CS8072: An expression tree lambda may not contain a null propagating operator.
    //         Expression<Func<int?>> c2 = () => x?.ToString()?.Length;
    Diagnostic(ErrorCode.ERR_NullPropagatingOpInExpressionTree, ".ToString()?.Length").WithLocation(13, 45)
               );
        }

        [Fact]
        [WorkItem(915609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/915609")]
        public void DictionaryInitializerInExprLambda()
        {
            var text = @"
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

class Program
{
    static void M<T>(T x)
    {
        Expression<Func<Dictionary<int, int>>> s = () => new Dictionary<int, int> () {[1] = 2};
}

    static void Main()
    {
        M((string)null);
    }
}
";
            CreateCompilationWithMscorlib45(text, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }).VerifyDiagnostics(
    // (10,87): error CS8073: An expression tree lambda may not contain a dictionary initializer.
    //         Expression<Func<Dictionary<int, int>>> s = () => new Dictionary<int, int> () {[1] = 2};
    Diagnostic(ErrorCode.ERR_DictionaryInitializerInExpressionTree, "[1]").WithLocation(10, 87)
               );
        }

        [Fact]
        [WorkItem(915609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/915609")]
        public void DictionaryInitializerInExprLambda1()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ConsoleApplication31
{
    class Program
    {
        static void Main(string[] args)
        {
            var o = new Goo();
            var x = o.E.Compile()().Pop();
            System.Console.WriteLine(x);
        }
    }

    static class StackExtensions
    {
        public static void Add<T>(this Stack<T> s, T x) => s.Push(x);
    }

    class Goo
    {
        public Expression<Func<Stack<int>>> E = () => new Stack<int> { 42 };
    }
}

";
            CreateCompilationWithMscorlib45(text, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }).VerifyDiagnostics(
    // (25,72): error CS8075: An expression tree lambda may not contain an extension collection element initializer.
    //         public Expression<Func<Stack<int>>> E = () => new Stack<int> { 42 };
    Diagnostic(ErrorCode.ERR_ExtensionCollectionElementInitializerInExpressionTree, "42").WithLocation(25, 72)
               );
        }

        [WorkItem(310, "https://github.com/dotnet/roslyn/issues/310")]
        [Fact]
        public void ExtensionElementInitializerInExpressionLambda()
        {
            var text = @"
using System;
using System.Collections;
using System.Linq.Expressions;
class C
{
    static void Main()
    {
        Expression<Func<C>> e = () => new C { H = { [""Key""] = ""Value"" } };
        Console.WriteLine(e);
        var c = e.Compile().Invoke();
        Console.WriteLine(c.H[""Key""]);
    }
    readonly Hashtable H = new Hashtable();
}

";
            CreateCompilationWithMscorlib45(text, new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef }).VerifyDiagnostics(
    // (9,53): error CS8073: An expression tree lambda may not contain a dictionary initializer.
    //         Expression<Func<C>> e = () => new C { H = { ["Key"] = "Value" } };
    Diagnostic(ErrorCode.ERR_DictionaryInitializerInExpressionTree, @"[""Key""]").WithLocation(9, 53)
               );
        }

        [WorkItem(12900, "https://github.com/dotnet/roslyn/issues/12900")]
        [WorkItem(17138, "https://github.com/dotnet/roslyn/issues/17138")]
        [Fact]
        public void CSharp7FeaturesInExprTrees()
        {
            var source = @"
using System;
//using System.Collections;
using System.Linq.Expressions;
class C
{
    static void Main()
    {
        // out variable declarations
        Expression<Func<bool>> e1 = () => TryGetThree(out int x) && x == 3; // ERROR 1

        // pattern matching
        object o = 3;
        Expression<Func<bool>> e2 = () => o is int y && y == 3; // ERROR 2

        // direct tuple creation could be OK, as it is just a constructor invocation,
        // not for long tuples the generated code is more complex, and we would
        // prefer custom expression trees to express the semantics.
        Expression<Func<object>> e3 = () => (1, o); // ERROR 3: tuple literal
        Expression<Func<(int, int)>> e4 = () => (1, 2); // ERROR 4: tuple literal

        // tuple conversions
        (byte, byte) t1 = (1, 2);
        Expression<Func<(byte a, byte b)>> e5 = () => t1; // OK, identity conversion
        Expression<Func<(int, int)>> e6 = () => t1; // ERROR 5: tuple conversion

        Expression<Func<int>> e7 = () => TakeRef(ref GetRefThree()); // ERROR 6: calling ref-returning method

        // discard
        Expression<Func<bool>> e8 = () => TryGetThree(out int _);
        Expression<Func<bool>> e9 = () => TryGetThree(out var _);
        Expression<Func<bool>> e10 = () => _ = (bool)o;
        Expression<Func<object>> e11 = () => _ = (_, _) = GetTuple();
        Expression<Func<object>> e12 = () => _ = var (a, _) = GetTuple();
        Expression<Func<object>> e13 = () => _ = (var a, var _) = GetTuple();
        Expression<Func<bool>> e14 = () => TryGetThree(out _);
    }

    static bool TryGetThree(out int three)
    {
        three = 3;
        return true;
    }

    static int three = 3;
    static ref int GetRefThree()
    {
        return ref three;
    }
    static int TakeRef(ref int x)
    {
        Console.WriteLine(""wow"");
        return x;
    }
    static (object, object) GetTuple()
    {
        return (null, null);
    }
}
namespace System
{
    struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) { Item1 = item1; Item2 = item2; }
    }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that the use of <see cref=""System.ValueTuple""/> on a member is meant to be treated as a tuple with element names.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Class | AttributeTargets.Struct )]
    public sealed class TupleElementNamesAttribute : Attribute
    {
        public TupleElementNamesAttribute(string[] transformNames) { }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (34,50): error CS8185: A declaration is not allowed in this context.
                //         Expression<Func<object>> e12 = () => _ = var (a, _) = GetTuple();
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (a, _)").WithLocation(34, 50),
                // (35,51): error CS8185: A declaration is not allowed in this context.
                //         Expression<Func<object>> e13 = () => _ = (var a, var _) = GetTuple();
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var a").WithLocation(35, 51),
                // (10,59): error CS8198: An expression tree may not contain an out argument variable declaration.
                //         Expression<Func<bool>> e1 = () => TryGetThree(out int x) && x == 3; // ERROR 1
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOutVariable, "int x").WithLocation(10, 59),
                // (14,43): error CS8122: An expression tree may not contain an 'is' pattern-matching operator.
                //         Expression<Func<bool>> e2 = () => o is int y && y == 3; // ERROR 2
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsIsMatch, "o is int y").WithLocation(14, 43),
                // (19,45): error CS8143: An expression tree may not contain a tuple literal.
                //         Expression<Func<object>> e3 = () => (1, o); // ERROR 3: tuple literal
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsTupleLiteral, "(1, o)").WithLocation(19, 45),
                // (20,49): error CS8143: An expression tree may not contain a tuple literal.
                //         Expression<Func<(int, int)>> e4 = () => (1, 2); // ERROR 4: tuple literal
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsTupleLiteral, "(1, 2)").WithLocation(20, 49),
                // (25,49): error CS8144: An expression tree may not contain a tuple conversion.
                //         Expression<Func<(int, int)>> e6 = () => t1; // ERROR 5: tuple conversion
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsTupleConversion, "t1").WithLocation(25, 49),
                // (27,54): error CS8156: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //         Expression<Func<int>> e7 = () => TakeRef(ref GetRefThree()); // ERROR 6: calling ref-returning method
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "GetRefThree()").WithLocation(27, 54),
                // (30,59): error CS8205: An expression tree may not contain a discard.
                //         Expression<Func<bool>> e8 = () => TryGetThree(out int _);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDiscard, "int _").WithLocation(30, 59),
                // (31,59): error CS8205: An expression tree may not contain a discard.
                //         Expression<Func<bool>> e9 = () => TryGetThree(out var _);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDiscard, "var _").WithLocation(31, 59),
                // (32,44): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<Func<bool>> e10 = () => _ = (bool)o;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "_ = (bool)o").WithLocation(32, 44),
                // (33,46): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<Func<object>> e11 = () => _ = (_, _) = GetTuple();
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "_ = (_, _) = GetTuple()").WithLocation(33, 46),
                // (33,50): error CS8143: An expression tree may not contain a tuple literal.
                //         Expression<Func<object>> e11 = () => _ = (_, _) = GetTuple();
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsTupleLiteral, "(_, _)").WithLocation(33, 50),
                // (36,60): error CS8205: An expression tree may not contain a discard.
                //         Expression<Func<bool>> e14 = () => TryGetThree(out _);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDiscard, "_").WithLocation(36, 60)
                );
        }

        [Fact]
        public void DictionaryInitializerInCS5()
        {
            var text = @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var s = new Dictionary<int, int> () {[1] = 2};
    }
}
";
            CreateCompilationWithMscorlib45(text,
                new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef },
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5)).VerifyDiagnostics(
    // (8,46): error CS8026: Feature 'dictionary initializer' is not available in C# 5. Please use language version 6 or greater.
    //         var s = new Dictionary<int, int> () {[1] = 2};
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "[1] = 2").WithArguments("dictionary initializer", "6").WithLocation(8, 46)
               );
        }

        [Fact]
        public void DictionaryInitializerDataFlow()
        {
            var text = @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        int i;
        var s = new Dictionary<int, int> () {[i] = 2};

        i = 1;
        System.Console.WriteLine(i);
    }

    static void Goo()
    {
        int i;
        var s = new Dictionary<int, int> () {[i = 1] = 2};

        System.Console.WriteLine(i);
    }
}
";
            CreateCompilationWithMscorlib45(text,
                new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef },
                parseOptions: TestOptions.Regular).VerifyDiagnostics(
    // (9,47): error CS0165: Use of unassigned local variable 'i'
    //         var s = new Dictionary<int, int> () {[i] = 2};
    Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i").WithLocation(9, 47)
               );
        }

        [Fact]
        public void ConditionalMemberAccessNotStatement()
        {
            var text = @"
class Program
{
    static void Main()
    {
        var x = new int[10];

        x?.Length;
        x?[1];
        x?.ToString()[1];
    }
}
";
            CreateCompilationWithMscorlib45(text, options: TestOptions.ReleaseDll).VerifyDiagnostics(
    // (8,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         x?.Length;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "x?.Length").WithLocation(8, 9),
    // (9,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         x?[1];
    Diagnostic(ErrorCode.ERR_IllegalStatement, "x?[1]").WithLocation(9, 9),
    // (10,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         x?.ToString()[1];
    Diagnostic(ErrorCode.ERR_IllegalStatement, "x?.ToString()[1]").WithLocation(10, 9)
               );
        }

        [WorkItem(23422, "https://github.com/dotnet/roslyn/issues/23422")]
        [Fact]
        public void ConditionalMemberAccessRefLike()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        var o = new Program();

        o?.F(); // this is ok

        var x = o?.F();

        var y = o?.F() ?? default;

        var z = o?.F().field ?? default;
    }

    S2 F() => throw null;
}

public ref struct S1
{

}

public ref struct S2
{
    public S1 field;
}
";
            CreateCompilationWithMscorlib45(text, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (10,18): error CS0023: Operator '?' cannot be applied to operand of type 'S2'
                //         var x = o?.F();
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "S2").WithLocation(10, 18),
                // (12,18): error CS0023: Operator '?' cannot be applied to operand of type 'S2'
                //         var y = o?.F() ?? default;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "S2").WithLocation(12, 18),
                // (14,18): error CS0023: Operator '?' cannot be applied to operand of type 'S1'
                //         var z = o?.F().field ?? default;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "S1").WithLocation(14, 18)
               );
        }

        [Fact]
        [WorkItem(1179322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1179322")]
        public void LabelSameNameAsParameter()
        {
            var text = @"
class Program
{
    static object M(object obj, object value)
    {
        if (((string)obj).Length == 0)  value: new Program();
    }
}
";
            var compilation = CreateCompilation(text);
            compilation.GetParseDiagnostics().Verify();

            // Make sure the compiler can handle producing method body diagnostics for this pattern when 
            // queried via an API (command line compile would exit after parse errors were reported). 
            compilation.GetMethodBodyDiagnostics().Verify(
                // (6,41): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //         if (((string)obj).Length == 0)  value: new Program();
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "value: new Program();").WithLocation(6, 41),
                // (6,41): warning CS0164: This label has not been referenced
                //         if (((string)obj).Length == 0)  value: new Program();
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "value").WithLocation(6, 41),
                // (4,19): error CS0161: 'Program.M(object, object)': not all code paths return a value
                //     static object M(object obj, object value)
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("Program.M(object, object)").WithLocation(4, 19));
        }

        [Fact]
        public void ThrowInExpressionTree()
        {
            var text = @"
using System;
using System.Linq.Expressions;

namespace ConsoleApplication1
{
    class Program
    {
        static bool b = true;
        static object o = string.Empty;
        static void Main(string[] args)
        {
            Expression<Func<object>> e1 = () => o ?? throw null;
            Expression<Func<object>> e2 = () => b ? throw null : o;
            Expression<Func<object>> e3 = () => b ? o : throw null;
            Expression<Func<object>> e4 = () => throw null;
        }
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (13,54): error CS8188: An expression tree may not contain a throw-expression.
                //             Expression<Func<object>> e1 = () => o ?? throw null;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsThrowExpression, "throw null").WithLocation(13, 54),
                // (14,53): error CS8188: An expression tree may not contain a throw-expression.
                //             Expression<Func<object>> e2 = () => b ? throw null : o;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsThrowExpression, "throw null").WithLocation(14, 53),
                // (15,57): error CS8188: An expression tree may not contain a throw-expression.
                //             Expression<Func<object>> e3 = () => b ? o : throw null;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsThrowExpression, "throw null").WithLocation(15, 57),
                // (16,49): error CS8188: An expression tree may not contain a throw-expression.
                //             Expression<Func<object>> e4 = () => throw null;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsThrowExpression, "throw null").WithLocation(16, 49)
                );
        }

        [Fact, WorkItem(17674, "https://github.com/dotnet/roslyn/issues/17674")]
        public void VoidDiscardAssignment()
        {
            var text = @"
class Program
{
    public static void Main(string[] args)
    {
        _ = M();
    }
    static void M() { }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (6,9): error CS8209: A value of type 'void' may not be assigned.
                //         _ = M();
                Diagnostic(ErrorCode.ERR_VoidAssignment, "_").WithLocation(6, 9)
                );
        }

        [Fact, WorkItem(22880, "https://github.com/dotnet/roslyn/issues/22880")]
        public void AttributeCtorInParam()
        {
            var text = @"
[A(1)]
class A : System.Attribute {
  A(in int x) { }
}

[B()]
class B : System.Attribute {
  B(in int x = 1) { }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (2,2): error CS8358: Cannot use attribute constructor 'A.A(in int)' because it has 'in' parameters.
                // [A(1)]
                Diagnostic(ErrorCode.ERR_AttributeCtorInParameter, "A(1)").WithArguments("A.A(in int)").WithLocation(2, 2),
                // (7,2): error CS8358: Cannot use attribute constructor 'B.B(in int)' because it has 'in' parameters.
                // [B()]
                Diagnostic(ErrorCode.ERR_AttributeCtorInParameter, "B()").WithArguments("B.B(in int)").WithLocation(7, 2)
                );
        }

        [Fact]
        public void ERR_ExpressionTreeContainsSwitchExpression()
        {
            var text = @"
using System;
using System.Linq.Expressions;

public class C
{
    public int Test()
    {
        Expression<Func<int, int>> e = a => a switch { 0 => 1, _ => 2 }; // CS8411
        return 1;
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(text, parseOptions: TestOptions.RegularWithRecursivePatterns).VerifyDiagnostics(
                // (9,45): error CS8411: An expression tree may not contain a switch expression.
                //         Expression<Func<int, int>> e = a => a switch { 0 => 1, _ => 2 }; // CS8411
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsSwitchExpression, "a switch { 0 => 1, _ => 2 }").WithLocation(9, 45)
                );
        }

        [Fact]
        public void PointerGenericConstraintTypes()
        {
            var source = @"
namespace A
{
    class D {}
}

class B {}

unsafe class C<T, U, V, X, Y, Z> where T : byte*
                                 where U : unmanaged
                                 where V : U*
                                 where X : object*
                                 where Y : B*
                                 where Z : A.D*
{
    void M1<A>() where A : byte* {}
    void M2<A, B>() where A : unmanaged 
                    where B : A* {}
    void M3<A>() where A : object* {}
    void M4<A>() where A : B* {}
    void M5<A>() where A : T {}
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                    // (9,44): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                    // unsafe class C<T, U, V, X, Y, Z> where T : byte*
                    Diagnostic(ErrorCode.ERR_BadConstraintType, "byte*").WithLocation(9, 44),
                    // (11,44): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                    //                                  where V : U*
                    Diagnostic(ErrorCode.ERR_BadConstraintType, "U*").WithLocation(11, 44),
                    // (12,44): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                    //                                  where X : object*
                    Diagnostic(ErrorCode.ERR_BadConstraintType, "object*").WithLocation(12, 44),
                    // (13,44): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                    //                                  where Y : B*
                    Diagnostic(ErrorCode.ERR_BadConstraintType, "B*").WithLocation(13, 44),
                    // (14,44): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                    //                                  where Z : A.D*
                    Diagnostic(ErrorCode.ERR_BadConstraintType, "A.D*").WithLocation(14, 44),
                    // (16,28): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                    //     void M1<A>() where A : byte* {}
                    Diagnostic(ErrorCode.ERR_BadConstraintType, "byte*").WithLocation(16, 28),
                    // (18,31): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                    //                     where B : A* {}
                    Diagnostic(ErrorCode.ERR_BadConstraintType, "A*").WithLocation(18, 31),
                    // (19,28): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                    //     void M3<A>() where A : object* {}
                    Diagnostic(ErrorCode.ERR_BadConstraintType, "object*").WithLocation(19, 28),
                    // (20,28): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                    //     void M4<A>() where A : B* {}
                    Diagnostic(ErrorCode.ERR_BadConstraintType, "B*").WithLocation(20, 28)
            );
        }

        [Fact]
        public void ArrayGenericConstraintTypes()
        {
            var source = @"class A<T> where T : object[] {}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                    // (1,22): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                    // class A<T> where T : object[] {}
                    Diagnostic(ErrorCode.ERR_BadConstraintType, "object[]").WithLocation(1, 22));
        }
    }
}
