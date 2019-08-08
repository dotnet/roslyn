// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class NullableConversionTests : CompilingTestBase
    {
        [Fact, WorkItem(544450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544450")]
        public void TestBug12780()
        {
            string source = @"
enum E : byte { A, B }
class Program
{
    static void Main()
    {
        E? x = 0;
        System.Console.Write(x);
        x = (E?) E.B;
        System.Console.Write(x);
    }
}
";
            var verifier = CompileAndVerify(source: source, expectedOutput: "AB");
        }

        [Fact]
        public void TestNullableConversions()
        {
            // IntPtr and UIntPtr violate the rules of user-defined conversions for 
            // backwards-compatibility reasons. All of these should compile without error.

            string source = @"
using System;
class P
{
    public static void Main()
    {
        int? x = 123;
        long? y = x;        

        V(x.HasValue);
        V(x.Value == 123);
        V((int)x == 123);
        V(y.HasValue);
        V(y.Value == 123);
        V((long)y == 123);
        V((int)y == 123);
        x = null;
        y = x;
        V(x.HasValue);
        V(y.HasValue);
        bool caught = false;
        try
        {
            y = (int) y;
        }
        catch
        {
            caught = true;
        }
        V(caught);
    }

    static void V(bool b)
    {
        Console.Write(b ? 't' : 'f');
    }
}
";

            string expectedOutput = @"tttttttfft";
            var verifier = CompileAndVerify(source: source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestLiftedUserDefinedConversions()
        {
            string source = @"
struct Conv
{
    public static implicit operator int(Conv c)
    {
        return 1;
    }

    // DELIBERATE SPEC VIOLATION: We allow 'lifting' even though the
    // return type is not a non-nullable value type.

    // UNDONE: Test pointer types

    public static implicit operator string(Conv c)
    {
        return '2'.ToString();
    }

    public static implicit operator double?(Conv c)
    {
        return 123.0;
    }

    static void Main()
    {
        Conv? c = new Conv();
        int? i = c;
        string s = c;
        double? d = c;

        V(i.HasValue);
        V(i == 1);

        V(s != null);
        V(s.Length == 1);
        V(s[0] == '2');

        V(d.HasValue);
        V(d == 123.0);

        c = null;
        i = c;
        s = c;
        d = c;

        V(!i.HasValue);
        V(s == null);
        V(!d.HasValue);
    }
    static void V(bool f)
    {
        System.Console.Write(f ? 't' : 'f');
    }
}
";

            string expectedOutput = @"tttttttttt";
            var verifier = CompileAndVerify(source: source, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(529279, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529279")]
        public void TestNullableWithGenericConstraints01()
        {
            string source = @"
class GenC<T, U> where T : struct, U where U : class
{
    public void Test(T t)
    {
        T? nt = t;
        U valueUn = nt;
        System.Console.WriteLine(valueUn.ToString());
    }
}
interface I1 { }
struct S1 : I1 { public override string ToString() { return ""Hola""; } }
static class Program
{
    static void Main()
    {
        (new GenC<S1, I1>()).Test(default(S1));
    }
}";
            CompileAndVerify(source, expectedOutput: "Hola");
        }

        [Fact, WorkItem(543996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543996")]
        public void TestNonLiftedUDCOnStruct()
        {
            string source = @"using System;
struct A
{
    public C CFld;
}

class C
{
    public A AFld;

    public static implicit operator A(C c)
    {
        return c.AFld;
    }

    public static explicit operator C(A a)
    {
        return a.CFld;
    }
}

public class Test
{
    public static void Main()
    {
        A a = new A();
        a.CFld = new C();
        a.CFld.AFld = a;

        C c = a.CFld;
        A? nubA = c;    // Assert here
        Console.Write(nubA.HasValue && nubA.Value.CFld == c);
        C nubC = (C)nubA;
        Console.Write(nubC.AFld.CFld == c);
    }
}
";

            CompileAndVerify(source, expectedOutput: "TrueTrue");
        }

        [Fact, WorkItem(543997, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543997")]
        public void TestImplicitLiftedUDCOnStruct()
        {
            string source = @"using System;

namespace Test
{
    static class Program
    {
        static void Main()
        {
            S.v = 0;
            S? S2 = 123;                  // not lifted, int=>int?, int?=>S, S=>S?
            Console.WriteLine(S.v == 123);
        }
    }

    public struct S
    {
        public static int v;
        // s == null, return v = -1
        public static implicit operator S(int? s)
        {
            Console.Write(""Imp S::int? -> S "");
            S ss = new S();
            S.v = s ?? -1;
            return ss;
        }
    }
}
";

            CompileAndVerify(source, expectedOutput: "Imp S::int? -> S True");
        }

        [Fact]
        public void TestExplicitUnliftedUDC()
        {
            string source = @"
using System;
namespace Test
{
    static class Program
    {
        static void Main()
        {
            int? i = 123;
            C c = (C)i;
            Console.WriteLine(c.v == 123 ? 't' : 'f');
        }
    }

    public class C
    {
        public readonly int v;
        public C(int v) { this.v = v; }
        public static implicit operator C(int v)
        {
            Console.Write(v);
            return new C(v);
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "123t");
        }

        [Fact, WorkItem(545091, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545091")]
        public void TestImplicitUDCInNullCoalescingOperand()
        {
            string source = @"using System;

class C
{
    public static implicit operator C(string s) 
    {
        Console.Write(""implicit "");
        return new C(); 
    }
    public override string ToString() { return ""C""; }
}

class A
{
    static void Main()
    {
        var ret = ""str"" ?? new C();
        Console.Write(ret.GetType());
    }
}
";

            CompileAndVerify(source, expectedOutput: "implicit C");
        }

        [WorkItem(545377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545377")]
        [Fact]
        public void TestLiftedVsUnlifted()
        {
            // The correct behavior here is to choose operator 2. Binary operator overload
            // resolution should determine that the best built-in addition operator is
            // lifted int + int, which has signature int? + int? --> int?. However, the 
            // native compiler gets this wrong. The native compiler, pretends
            // that there are *three* lifted operators: int + int? --> int?, int? + int --> int?,
            // and int? + int? --> int?. Therefore the native compiler decides that the 
            // int? + int --> int operator is the best, because it is the applicable operator
            // with the most specific types, and therefore chooses operator 1.
            //            
            // This does not match the specification.
            // It seems reasonable that if someone has done this very strange thing of making 
            // conversions S --> int and S --> int?, that they probably intend for the
            // lifted operation to use the conversion specifically designed for nullables.
            //
            // Roslyn matches the specification and takes the break from the native compiler.

            // See the next test case for more thoughts on this.

            string source = @"
using System;

public struct S
{
    public static implicit operator int(S n) // 1 native compiler
    {
        Console.WriteLine(1);
        return 0;
    }

    public static implicit operator int?(S n) // 2 Roslyn compiler
    {
        Console.WriteLine(2);
        return null;
    }

    public static void Main()
    {
        int? qa = 5;
        S b = default(S);
        var sum = qa + b;
    }
}
";

            CompileAndVerify(source, expectedOutput: "2");
        }

        [WorkItem(545377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545377")]
        [Fact]
        public void TestLiftedVsUnlifted_Combinations()
        {
            // The point of this test is to show that Roslyn and the native compiler
            // agree on resolution of *conversions* but do not agree on resolution
            // of *binary operators*. That is, we wish to show that we are isolating
            // the breaking change to the binary operator overload resolution, and not
            // to the conversion resolution code. See the previous bug for details.

            string source = @"
using System;

struct S___A
{
    public static implicit operator int(S___A n)  { Console.Write('A'); return 0; }
}

struct S__B_
{
    public static implicit operator int(S__B_? n) { Console.Write('B'); return 0; }
}

struct S__BA
{
    public static implicit operator int(S__BA? n) { Console.Write('B'); return 0; }
    public static implicit operator int(S__BA n)  { Console.Write('A'); return 0; }
}

struct S_C__
{
    public static implicit operator int?(S_C__ n) { Console.Write('C'); return 0; }

}

struct S_C_A
{
    public static implicit operator int?(S_C_A n) { Console.Write('C'); return 0; }
    public static implicit operator int(S_C_A n)  { Console.Write('A'); return 0; }
}

struct S_CB_
{
    public static implicit operator int?(S_CB_ n) { Console.Write('C'); return 0; }
    public static implicit operator int(S_CB_? n) { Console.Write('B'); return 0; }
}

struct S_CBA
{
    public static implicit operator int?(S_CBA n) { Console.Write('C'); return 0; }
    public static implicit operator int(S_CBA? n) { Console.Write('B'); return 0; }
    public static implicit operator int(S_CBA n)  { Console.Write('A'); return 0; }
}

struct SD___
{
    public static implicit operator int?(SD___? n){ Console.Write('D'); return 0; }
}

struct SD__A
{
    public static implicit operator int?(SD__A? n){ Console.Write('D'); return 0; }
    public static implicit operator int(SD__A n)  { Console.Write('A'); return 0; }
}

struct SD_B_
{
    public static implicit operator int?(SD_B_? n){ Console.Write('D'); return 0; }
    public static implicit operator int(SD_B_? n) { Console.Write('B'); return 0; }
}

struct SD_BA
{
    public static implicit operator int?(SD_BA? n){ Console.Write('D'); return 0; }
    public static implicit operator int(SD_BA? n) { Console.Write('B'); return 0; }
    public static implicit operator int(SD_BA n)  { Console.Write('A'); return 0; }
}

struct SDC__
{
    public static implicit operator int?(SDC__? n){ Console.Write('D'); return 0; }
    public static implicit operator int?(SDC__ n) { Console.Write('C'); return 0; }
}

struct SDC_A
{
    public static implicit operator int?(SDC_A? n){ Console.Write('D'); return 0; }
    public static implicit operator int?(SDC_A n) { Console.Write('C'); return 0; }
    public static implicit operator int(SDC_A n)  { Console.Write('A'); return 0; }
}

struct SDCB_
{
    public static implicit operator int?(SDCB_? n){ Console.Write('D'); return 0; }
    public static implicit operator int?(SDCB_ n) { Console.Write('C'); return 0; }
    public static implicit operator int(SDCB_? n) { Console.Write('B'); return 0; }
}

struct SDCBA
{
    public static implicit operator int?(SDCBA? n){ Console.Write('D'); return 0; }
    public static implicit operator int?(SDCBA n) { Console.Write('C'); return 0; }
    public static implicit operator int(SDCBA? n) { Console.Write('B'); return 0; }
    public static implicit operator int(SDCBA n)  { Console.Write('A'); return 0; }
}

class Program
{
    
    static S___A s___a1;
    static S__B_ s__b_1;
    static S__BA s__ba1;
    static S_C__ s_c__1;
    static S_C_A s_c_a1;
    static S_CB_ s_cb_1;
    static S_CBA s_cba1;
    static SD___ sd___1;
    static SD__A sd__a1;
    static SD_B_ sd_b_1;
    static SD_BA sd_ba1;
    static SDC__ sdc__1;
    static SDC_A sdc_a1;
    static SDCB_ sdcb_1;
    static SDCBA sdcba1;

    static S___A? s___a2;
    static S__B_? s__b_2;
    static S__BA? s__ba2;
    static S_C__? s_c__2;
    static S_C_A? s_c_a2;
    static S_CB_? s_cb_2;
    static S_CBA? s_cba2;
    static SD___? sd___2;
    static SD__A? sd__a2;
    static SD_B_? sd_b_2;
    static SD_BA? sd_ba2;
    static SDC__? sdc__2;
    static SDC_A? sdc_a2;
    static SDCB_? sdcb_2;
    static SDCBA? sdcba2;

    static int i1 = 0;
    static int? i2 = 0;

    static void Main()
    {
        TestConversions();
        Console.WriteLine();
        TestAdditions();
    }

    static void TestConversions()
    {
        i1 = s___a1;
        i1 = s__b_1;
        i1 = s__ba1;
        // i1 = s_c__1;
        i1 = s_c_a1;
        i1 = s_cb_1;
        i1 = s_cba1;
        // i1 = sd___1;
        i1 = sd__a1;
        i1 = sd_b_1;
        i1 = sd_ba1;
        // i1 = sdc__1;
        i1 = sdc_a1;
        i1 = sdcb_1;
        i1 = sdcba1;
           
        // i1 = s___a2;
        i1 = s__b_2;
        i1 = s__ba2;
        // i1 = s_c__2;
        // i1 = s_c_a2;
        i1 = s_cb_2;
        i1 = s_cba2;
        //i1 = sd___2;
        //i1 = sd__a2;
        i1 = sd_b_2;
        i1 = sd_ba2;
        //i1 = sdc__2;
        //i1 = sdc_a2;
        i1 = sdcb_2;
        i1 = sdcba2;
 
        i2 = s___a1;
        i2 = s__b_1;
        i2 = s__ba1;
        i2 = s_c__1;
        i2 = s_c_a1;
        i2 = s_cb_1;
        i2 = s_cba1;
        i2 = sd___1;
        i2 = sd__a1;
        i2 = sd_b_1;
        i2 = sd_ba1;
        i2 = sdc__1;
        i2 = sdc_a1;
        i2 = sdcb_1;
        i2 = sdcba1;
           
        i2 = s___a2;
        i2 = s__b_2;
        i2 = s__ba2;
        i2 = s_c__2;
        i2 = s_c_a2;
        //i2 = s_cb_2;
        //i2 = s_cba2;
        i2 = sd___2;
        i2 = sd__a2;
        i2 = sd_b_2;
        i2 = sd_ba2;
        i2 = sdc__2;
        i2 = sdc_a2;
        i2 = sdcb_2;
        i2 = sdcba2;
    }

    static void TestAdditions()
    {
        i2 = i1 + s___a1;
        i2 = i1 + s__b_1;
        i2 = i1 + s__ba1;
        i2 = i1 + s_c__1;
        i2 = i1 + s_c_a1;
        i2 = i1 + s_cb_1;
        i2 = i1 + s_cba1;
        i2 = i1 + sd___1;
        i2 = i1 + sd__a1;
        i2 = i1 + sd_b_1;
        i2 = i1 + sd_ba1;
        i2 = i1 + sdc__1;
        i2 = i1 + sdc_a1;
        i2 = i1 + sdcb_1;
        i2 = i1 + sdcba1;
        
        i2 = i1 + s___a2;
        i2 = i1 + s__b_2;
        i2 = i1 + s__ba2;
        i2 = i1 + s_c__2;
        i2 = i1 + s_c_a2;
        i2 = i1 + s_cb_2;
        i2 = i1 + s_cba2;
        i2 = i1 + sd___2;
        i2 = i1 + sd__a2;
        i2 = i1 + sd_b_2;
        i2 = i1 + sd_ba2;
        i2 = i1 + sdc__2;
        i2 = i1 + sdc_a2;
        i2 = i1 + sdcb_2;
        i2 = i1 + sdcba2;

        i2 = i2 + s___a1;
        i2 = i2 + s__b_1;
        i2 = i2 + s__ba1;
        i2 = i2 + s_c__1;
        i2 = i2 + s_c_a1;
        i2 = i2 + s_cb_1;
        i2 = i2 + s_cba1;
        i2 = i2 + sd___1;
        i2 = i2 + sd__a1;
        i2 = i2 + sd_b_1;
        i2 = i2 + sd_ba1;
        i2 = i2 + sdc__1;
        i2 = i2 + sdc_a1;
        i2 = i2 + sdcb_1;
        i2 = i2 + sdcba1;
        
        i2 = i2 + s___a2;
        i2 = i2 + s__b_2;
        i2 = i2 + s__ba2;
        i2 = i2 + s_c__2;
        i2 = i2 + s_c_a2;
        // i2 = i2 + s_cb_2; // Native compiler allows these because it actually converts to int,
        // i2 = i2 + s_cba2; // not int?, which is not ambiguous. Roslyn takes the breaking change.
        i2 = i2 + sd___2;
        i2 = i2 + sd__a2;
        i2 = i2 + sd_b_2;
        i2 = i2 + sd_ba2;
        i2 = i2 + sdc__2;
        i2 = i2 + sdc_a2;
        i2 = i2 + sdcb_2;
        i2 = i2 + sdcba2;

    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithWarningLevel(0));

            // Roslyn and native compiler both produce ABAABAABAABABBBBBBBBABACCCCDADACCCCBBDDDDDDDD 
            // for straight conversions. 
            // Because Roslyn (correctly) prefers converting to int? instead of int when doing lifted addition,
            // native compiler produces ABACABADABACABABBBBDDBBDDBBABACABADABACABABBDDBBDDBB for additions.
            // Roslyn compiler produces ABACABADABACABABBBBDDBBDDBBABACCCCDADACCCCBBDDDDDDDD. That is,
            // preference is given to int?-returning conversions C and D over int-returning A and B.

            string expected = @"ABAABAABAABABBBBBBBBABACCCCDADACCCCBBDDDDDDDD
ABACABADABACABABBBBDDBBDDBBABACCCCDADACCCCBBDDDDDDDD";

            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        [WorkItem(1084278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084278")]
        public void NullableConversionFromFloatingPointConst()
        {
            var source = @"
class C
{
    void Use(int? p)
    {

    }

    void Test()
    {
        int? i;

        // double checks
        i = (int?)3.5d;
        i = (int?)double.MaxValue;
        i = (int?)double.NaN;
        i = (int?)double.NegativeInfinity;
        i = (int?)double.PositiveInfinity;

        // float checks
        i = (int?)3.5d;
        i = (int?)float.MaxValue;
        i = (int?)float.NaN;
        i = (int?)float.NegativeInfinity;
        i = (int?)float.PositiveInfinity;

        Use(i);

        unchecked {
            // double checks
            i = (int?)3.5d;
            i = (int?)double.MaxValue;
            i = (int?)double.NaN;
            i = (int?)double.NegativeInfinity;
            i = (int?)double.PositiveInfinity;

            // float checks
            i = (int?)3.5d;
            i = (int?)float.MaxValue;
            i = (int?)float.NaN;
            i = (int?)float.NegativeInfinity;
            i = (int?)float.PositiveInfinity;
        }
    }
}
";
            var compilation = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(source,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 15, Column = 13 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 16, Column = 13 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 17, Column = 13 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 18, Column = 13 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 22, Column = 13 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 23, Column = 13 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 24, Column = 13 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstOutOfRangeChecked, Line = 25, Column = 13 });

            var syntaxTree = compilation.SyntaxTrees.First();
            var target = syntaxTree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().ToList()[2];
            var operand = target.Expression;
            Assert.Equal("double.NaN", operand.ToFullString());

            // Note: there is a valid conversion here at the type level.  It's the process of evaluating the conversion, which for
            // constants happens at compile time, that triggers the error.
            HashSet<DiagnosticInfo> unused = null;
            var bag = DiagnosticBag.GetInstance();
            var nullableIntType = compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(compilation.GetSpecialType(SpecialType.System_Int32));
            var conversion = compilation.Conversions.ClassifyConversionFromExpression(
                compilation.GetBinder(target).BindExpression(operand, bag),
                nullableIntType,
                ref unused);
            Assert.True(conversion.IsExplicit && conversion.IsNullable);
        }
    }
}
