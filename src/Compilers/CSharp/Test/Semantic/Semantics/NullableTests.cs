// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class NullableSemanticTests : SemanticModelTestBase
    {
        [Fact, WorkItem(651624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651624")]
        public void NestedNullableWithAttemptedConversion()
        {
            var src =
@"using System;
class C {
  public void Main()
  {
      Nullable<Nullable<int>> x = null;
      Nullable<int> y = null;
      Console.WriteLine(x == y);
  }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,16): error CS0453: The type 'int?' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'System.Nullable<T>'
                //       Nullable<Nullable<int>> x = null;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "Nullable<int>").WithArguments("System.Nullable<T>", "T", "int?"),
                // (7,25): error CS0019: Operator '==' cannot be applied to operands of type 'int??' and 'int?'
                //       Console.WriteLine(x == y);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == y").WithArguments("==", "int??", "int?"));
        }

        [Fact, WorkItem(544152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544152")]
        public void TestBug12347()
        {
            string source = @"
using System;
class C
{
  static void Main()
  {
    string? s1 = null;
    Nullable<string> s2 = null;
    Console.WriteLine(s1.ToString() + s2.ToString());
  }
}";
            var expected = new[]
            {
                // (7,11): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     string? s1 = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(7, 11),
                // (8,14): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //     Nullable<string> s2 = null;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string").WithArguments("System.Nullable<T>", "T", "string").WithLocation(8, 14)
            };
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(expected);
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,11): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //     string? s1 = null;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(7, 11),
                // (8,14): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //     Nullable<string> s2 = null;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string").WithArguments("System.Nullable<T>", "T", "string").WithLocation(8, 14));
        }

        [Fact, WorkItem(544152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544152")]
        public void TestBug12347_CSharp8()
        {
            string source = @"
using System;
class C
{
    static void Main()
    {
        string? s1 = null;
        Nullable<string> s2 = null;
        Console.WriteLine(s1.ToString() + s2.ToString());
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,15): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //         string? s1 = null;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(7, 15),
                // (8,18): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         Nullable<string> s2 = null;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string").WithArguments("System.Nullable<T>", "T", "string").WithLocation(8, 18)
                );
        }

        [Fact, WorkItem(529269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529269")]
        public void TestLiftedIncrementOperatorBreakingChanges01()
        {
            // The native compiler not only *allows* this to compile, it lowers to:
            // 
            // C temp1 = c;
            // int? temp2 = C.op_Implicit_C_To_Nullable_Int(temp1);
            // c = temp2.HasValue ? 
            //         C.op_Implicit_Nullable_Int_To_C(new short?((short)(temp2.GetValueOrDefault() + 1))) :
            //         null;
            //
            // !!!
            //
            // Not only does the native compiler silently insert a data-losing conversion from int to short,
            // if the result of the initial conversion to int? is null, the result is a null *C*, not 
            // an implicit conversion from a null *int?* to C.
            //
            // This should simply be disallowed. The increment on int? produces int?, and there is no implicit
            // conversion from int? to S.

            string source1 = @"
class C
{
  public readonly int? i;
  public C(int? i) { this.i = i; }
  public static implicit operator int?(C c) { return c.i; }
  public static implicit operator C(short? s) { return new C(s); }
  static void Main()
  {
    C c = new C(null);
    c++;
    System.Console.WriteLine(object.ReferenceEquals(c, null));
  }
}";
            var comp = CreateCompilation(source1);
            comp.VerifyDiagnostics(
                // (11,5): error CS0266: Cannot implicitly convert type 'int?' to 'C'. An explicit conversion exists (are you missing a cast?)
                //     c++;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c++").WithArguments("int?", "C")
                );
        }

        [Fact, WorkItem(543954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543954")]
        public void TestLiftedIncrementOperatorBreakingChanges02()
        {
            // Now here we have a case where the compilation *should* succeed, and does, but 
            // the native compiler and Roslyn produce opposite behavior. Again, the native
            // compiler lowers this to:
            //
            // C temp1 = c;
            // int? temp2 = C.op_Implicit_C_To_Nullable_Int(temp1);
            // c = temp2.HasValue ? 
            //         C.op_Implicit_Nullable_Int_To_C(new int?(temp2.GetValueOrDefault() + 1)) :
            //         null;
            //
            // And therefore produces "True". The correct lowering, performed by Roslyn, is:
            //
            // C temp1 = c;
            // int? temp2 = C.op_Implicit_C_To_Nullable_Int( temp1 );
            // int? temp3 = temp2.HasValue ? 
            //                  new int?(temp2.GetValueOrDefault() + 1)) : 
            //                  default(int?);
            // c = C.op_Implicit_Nullable_Int_To_C(temp3);
            //
            // and therefore should produce "False".

            string source2 = @"
class C
{
  public readonly int? i;
  public C(int? i) { this.i = i; }
  public static implicit operator int?(C c) { return c.i; }
  public static implicit operator C(int? s) { return new C(s); }
  static void Main()
  {
    C c = new C(null);
    c++;
    System.Console.WriteLine(object.ReferenceEquals(c, null) ? 1 : 0);
  }
}";

            var verifier = CompileAndVerify(source: source2, expectedOutput: "0");
            verifier = CompileAndVerify(source: source2, expectedOutput: "0");

            // And in fact, this should work if there is an implicit conversion from the result of the addition
            // to the type:

            string source3 = @"
class C
{
  public readonly int? i;
  public C(int? i) { this.i = i; }
  public static implicit operator int?(C c) { return c.i; }
  // There is an implicit conversion from int? to long? and therefore from int? to S.
  public static implicit operator C(long? s) { return new C((int?)s); }
  static void Main()
  {
    C c1 = new C(null);
    c1++;
    C c2 = new C(123);
    c2++;
    System.Console.WriteLine(!object.ReferenceEquals(c1, null) && c2.i.Value == 124 ? 1 : 0);
  }
}";

            verifier = CompileAndVerify(source: source3, expectedOutput: "1", verify: Verification.FailsPEVerify);
            verifier = CompileAndVerify(source: source3, expectedOutput: "1", parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());
        }

        [Fact, WorkItem(543954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543954")]
        public void TestLiftedIncrementOperatorBreakingChanges03()
        {
            // Let's in fact verify that this works correctly for all possible conversions to built-in types:

            string source4 = @"
using System;
class C
{
  public readonly TYPE? i;
  public C(TYPE? i) { this.i = i; }
  public static implicit operator TYPE?(C c) { return c.i; }
  public static implicit operator C(TYPE? s) { return new C(s); }
  static void Main()
  {
    TYPE q = 10;
    C x = new C(10);

    T(1, x.i.Value == q);
    T(2, (x++).i.Value == (q++));
    T(3, x.i.Value == q);
    T(4, (++x).i.Value == (++q));
    T(5, x.i.Value == q);
    T(6, (x--).i.Value == (q--));
    T(7, x.i.Value == q);
    T(8, (--x).i.Value == (--q));
    T(9, x.i.Value == q);

    C xn = new C(null);

    F(11, xn.i.HasValue);
    F(12, (xn++).i.HasValue);
    F(13, xn.i.HasValue);
    F(14, (++xn).i.HasValue);
    F(15, xn.i.HasValue);
    F(16, (xn--).i.HasValue);
    F(17, xn.i.HasValue);
    F(18, (--xn).i.HasValue);
    F(19, xn.i.HasValue);

    System.Console.WriteLine(0);

  }
  static void T(int line, bool b)
  {
    if (!b) throw new Exception(""TYPE"" + line.ToString());
  }
  static void F(int line, bool b)
  {
    if (b) throw new Exception(""TYPE"" + line.ToString());
  }
}
";
            foreach (string type in new[] { "int", "ushort", "byte", "long", "float", "decimal" })
            {
                CompileAndVerify(source: source4.Replace("TYPE", type), expectedOutput: "0", verify: Verification.FailsPEVerify);
                CompileAndVerify(source: source4.Replace("TYPE", type), expectedOutput: "0", parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());
            }
        }

        [Fact]
        public void TestLiftedBuiltInIncrementOperators()
        {
            string source = @"
using System;
class C
{
  static void Main()
  {
    TYPE q = 10;
    TYPE? x = 10;

    T(1, x.Value == q);
    T(2, (x++).Value == (q++));
    T(3, x.Value == q);
    T(4, (++x).Value == (++q));
    T(5, x.Value == q);
    T(6, (x--).Value == (q--));
    T(7, x.Value == q);
    T(8, (--x).Value == (--q));
    T(9, x.Value == q);

    int? xn = null;

    F(11, xn.HasValue);
    F(12, (xn++).HasValue);
    F(13, xn.HasValue);
    F(14, (++xn).HasValue);
    F(15, xn.HasValue);
    F(16, (xn--).HasValue);
    F(17, xn.HasValue);
    F(18, (--xn).HasValue);
    F(19, xn.HasValue);

    System.Console.WriteLine(0);

  }

  static void T(int line, bool b)
  {
    if (!b) throw new Exception(""TYPE"" + line.ToString());
  }
  static void F(int line, bool b)
  {
    if (b) throw new Exception(""TYPE"" + line.ToString());
  }


}";

            foreach (string type in new[] { "uint", "short", "sbyte", "ulong", "double", "decimal" })
            {
                string expected = "0";
                var verifier = CompileAndVerify(source: source.Replace("TYPE", type), expectedOutput: expected);
            }
        }

        [Fact]
        public void TestLiftedUserDefinedIncrementOperators()
        {
            string source = @"
using System;
struct S
{
  public int x;
  public S(int x) { this.x = x; }
  public static S operator ++(S s) { return new S(s.x + 1); }
  public static S operator --(S s) { return new S(s.x - 1); }
}

class C
{
  static void Main()
  {
    S? n = new S(1);
    S s = new S(1);

    T(1, n.Value.x == s.x);
    T(2, (n++).Value.x == (s++).x);
    T(3, n.Value.x == s.x);
    T(4, (n--).Value.x == (s--).x);
    T(5, n.Value.x == s.x);
    T(6, (++n).Value.x == (++s).x);
    T(7, n.Value.x == s.x);
    T(8, (--n).Value.x == (--s).x);
    T(9, n.Value.x == s.x);

    n = null;

    F(11, n.HasValue);
    F(12, (n++).HasValue);
    F(13, n.HasValue);
    F(14, (n--).HasValue);
    F(15, n.HasValue);
    F(16, (++n).HasValue);
    F(17, n.HasValue);
    F(18, (--n).HasValue);
    F(19, n.HasValue);
    
    Console.WriteLine(1);
  }

  static void T(int line, bool b)
  {
    if (!b) throw new Exception(line.ToString());
  }
  static void F(int line, bool b)
  {
    if (b) throw new Exception(line.ToString());
  }

}
";
            var verifier = CompileAndVerify(source: source, expectedOutput: "1");
        }

        [Fact]
        public void TestNullableBuiltInUnaryOperator()
        {
            string source = @"
using System;
class C
{
  static void Main()
  {
    Console.Write('!');
    bool? bf = false;
    bool? bt = true;
    bool? bn = null;
    Test((!bf).HasValue);
    Test((!bf).Value);
    Test((!bt).HasValue);
    Test((!bt).Value);
    Test((!bn).HasValue);

    Console.WriteLine();

    Console.Write('-');
    int? i32 = -1;
    int? i32n = null;
    Test((-i32).HasValue);
    Test((-i32) == 1);
    Test((-i32) == -1);
    Test((-i32n).HasValue);

    Console.Write(1);
    long? i64 = -1;
    long? i64n = null;
    Test((-i64).HasValue);
    Test((-i64) == 1);
    Test((-i64) == -1);
    Test((-i64n).HasValue);

    Console.Write(2);
    float? r32 = -1.5f;
    float? r32n = null;
    Test((-r32).HasValue);
    Test((-r32) == 1.5f);
    Test((-r32) == -1.5f);
    Test((-r32n).HasValue);

    Console.Write(3);
    double? r64 = -1.5;
    double? r64n = null;
    Test((-r64).HasValue);
    Test((-r64) == 1.5);
    Test((-r64) == -1.5);
    Test((-r64n).HasValue);

    Console.Write(4);
    decimal? d = -1.5m;
    decimal? dn = null;
    Test((-d).HasValue);
    Test((-d) == 1.5m);
    Test((-d) == -1.5m);
    Test((-dn).HasValue);

    Console.WriteLine();

    Console.Write('+');
    Test((+i32).HasValue);
    Test((+i32) == 1);
    Test((+i32) == -1);
    Test((+i32n).HasValue);

    Console.Write(1);
    uint? ui32 = 1;
    uint? ui32n = null;
    Test((+ui32).HasValue);
    Test((+ui32) == 1);
    Test((+ui32n).HasValue);

    Console.Write(2);
    Test((+i64).HasValue);
    Test((+i64) == 1);
    Test((+i64) == -1);
    Test((+i64n).HasValue);

    Console.Write(3);
    ulong? ui64 = 1;
    ulong? ui64n = null;
    Test((+ui64).HasValue);
    Test((+ui64) == 1);
    Test((+ui64n).HasValue);

    Console.Write(4);
    Test((+r32).HasValue);
    Test((+r32) == 1.5f);
    Test((+r32) == -1.5f);
    Test((+r32n).HasValue);

    Console.Write(5);
    Test((+r64).HasValue);
    Test((+r64) == 1.5);
    Test((+r64) == -1.5);
    Test((+r64n).HasValue);

    Console.Write(6);
    Test((+d).HasValue);
    Test((+d) == 1.5m);
    Test((+d) == -1.5m);
    Test((+dn).HasValue);

    Console.WriteLine();

    Console.Write('~');
    i32 = 1;
    Test((~i32).HasValue);
    Test((~i32) == -2);
    Test((~i32n).HasValue);

    Console.Write(1);
    Test((~ui32).HasValue);
    Test((~ui32) == 0xFFFFFFFE);
    Test((~ui32n).HasValue);

    Console.Write(2);
    i64 = 1;
    Test((~i64).HasValue);
    Test((~i64) == -2L);
    Test((~i64n).HasValue);

    Console.Write(3);
    Test((~ui64).HasValue);
    Test((~ui64) == 0xFFFFFFFFFFFFFFFE);
    Test((~ui64n).HasValue);

    Console.Write(4);

    Base64FormattingOptions? e = Base64FormattingOptions.InsertLineBreaks;
    Base64FormattingOptions? en = null;
    Test((~e).HasValue);
    Test((~e) == (Base64FormattingOptions)(-2));
    Test((~en).HasValue);
  }
  static void Test(bool b)
  {
    Console.Write(b ? 'T' : 'F');
  }
}";

            string expected =
@"!TTTFF
-TTFF1TTFF2TTFF3TTFF4TTFF
+TFTF1TTF2TFTF3TTF4TFTF5TFTF6TFTF
~TTF1TTF2TTF3TTF4TTF";

            var verifier = CompileAndVerify(source: source, expectedOutput: expected);
        }

        [Fact]
        public void TestNullableUserDefinedUnary()
        {
            string source = @"
using System;

struct S
{
  public S(char c) { this.str = c.ToString(); }
  public S(string str) { this.str = str; }
  public string str;
  public static S operator !(S s)
  {
    return new S('!' + s.str);
  }
  public static S operator ~(S s)
  {
    return new S('~' + s.str);
  }
  public static S operator -(S s)
  {
    return new S('-' + s.str);
  }
  public static S operator +(S s)
  {
    return new S('+' + s.str);
  }
}
class C
{
  static void Main()
  {
    S? s = new S('x');
    S? sn = null;

    Test((~s).HasValue);
    Test((~sn).HasValue);
    Console.WriteLine((~s).Value.str);

    Test((!s).HasValue);
    Test((!sn).HasValue);
    Console.WriteLine((!s).Value.str);

    Test((+s).HasValue);
    Test((+sn).HasValue);
    Console.WriteLine((+s).Value.str);
    
    Test((-s).HasValue);
    Test((-sn).HasValue);
    Console.WriteLine((-s).Value.str);
  }
  static void Test(bool b)
  {
    Console.Write(b ? 'T' : 'F');
  }
}";

            string expected =
@"TF~x
TF!x
TF+x
TF-x";

            var verifier = CompileAndVerify(source: source, expectedOutput: expected);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7803")]
        public void TestLiftedComparison()
        {
            TestNullableComparison("==", "FFTFF1FTFFTF2FFTFFT3TFFTFF4FTFFTF5FFTFFT",
                "int", "short", "byte", "long", "double", "decimal", "char", "Base64FormattingOptions", "S", "bool");

            TestNullableComparison("!=", "TTFTT1TFTTFT2TTFTTF3FTTFTT4TFTTFT5TTFTTF",
                "uint", "ushort", "sbyte", "ulong", "float", "decimal", "char", "Base64FormattingOptions", "S", "bool");

            TestNullableComparison("<", "FFFFF1FFTFFT2FFFFFF3FFFFFF4FFTFFT5FFFFFF",
                "uint", "sbyte", "float", "decimal", "Base64FormattingOptions", "S");

            TestNullableComparison("<=", "FFFFF1FTTFTT2FFTFFT3FFFFFF4FTTFTT5FFTFFT",
                "int", "byte", "double", "decimal", "char");

            TestNullableComparison(">", "FFFFF1FFFFFF2FTFFTF3FFFFFF4FFFFFF5FTFFTF",
                "ushort", "ulong", "decimal");

            TestNullableComparison(">=", "FFFFF1FTFFTF2FTTFTT3FFFFFF4FTFFTF5FTTFTT",
                "short", "long", "decimal");
        }

        private void TestNullableComparison(
            string oper,
            string expected,
            params string[] types)
        {
            string source = @"
using System;
struct S 
{
  public int i;
  public S(int i) { this.i = i; }
  public static bool operator ==(S x, S y) { return x.i == y.i; }
  public static bool operator !=(S x, S y) { return x.i != y.i; }
  public static bool operator <(S x, S y) { return x.i < y.i; }
  public static bool operator <=(S x, S y) { return x.i <= y.i; }
  public static bool operator >(S x, S y) { return x.i > y.i; }
  public static bool operator >=(S x, S y) { return x.i >= y.i; }
  
}
class C
{
  static void Main()
  {
    TYPE? xn0 = ZERO;
    TYPE? xn1 = ONE;
    TYPE? xnn = null;
    TYPE x0 = ZERO;
    TYPE x1 = ONE;

    TYPE? yn0 = ZERO;
    TYPE? yn1 = ONE;
    TYPE? ynn = null;
    TYPE y0 = ZERO;
    TYPE y1 = ONE;
    
    Test(null OP yn0);
    Test(null OP yn1);
    Test(null OP ynn);
    Test(null OP y0);
    Test(null OP y1);
    Console.Write('1');
    Test(xn0 OP null);
    Test(xn0 OP yn0);
    Test(xn0 OP yn1);
    Test(xn0 OP ynn);
    Test(xn0 OP y0);
    Test(xn0 OP y1);
    Console.Write('2');
    Test(xn1 OP null);
    Test(xn1 OP yn0);
    Test(xn1 OP yn1);
    Test(xn1 OP ynn);
    Test(xn1 OP y0);
    Test(xn1 OP y1);
    Console.Write('3');
    Test(xnn OP null);
    Test(xnn OP yn0);
    Test(xnn OP yn1);
    Test(xnn OP ynn);
    Test(xnn OP y0);
    Test(xnn OP y1);
    Console.Write('4');
    Test(x0 OP null);
    Test(x0 OP yn0);
    Test(x0 OP yn1);
    Test(x0 OP ynn);
    Test(x0 OP y0);
    Test(x0 OP y1);
    Console.Write('5');
    Test(x1 OP null);
    Test(x1 OP yn0);
    Test(x1 OP yn1);
    Test(x1 OP ynn);
    Test(x1 OP y0);
    Test(x1 OP y1);
  }
  static void Test(bool b)
  {
    Console.Write(b ? 'T' : 'F');
  }
}
";
            var zeros = new Dictionary<string, string>()
            {
                { "int", "0" },
                { "uint", "0" },
                { "short", "0" },
                { "ushort", "0" },
                { "byte", "0" },
                { "sbyte", "0" },
                { "long", "0" },
                { "ulong", "0" },
                { "double", "0" },
                { "float", "0" },
                { "decimal", "0" },
                { "char", "'a'" },
                { "bool", "false" },
                { "Base64FormattingOptions", "Base64FormattingOptions.None" },
                { "S", "new S(0)" }
            };
            var ones = new Dictionary<string, string>()
            {
                { "int", "1" },
                { "uint", "1" },
                { "short", "1" },
                { "ushort", "1" },
                { "byte", "1" },
                { "sbyte", "1" },
                { "long", "1" },
                { "ulong", "1" },
                { "double", "1" },
                { "float", "1" },
                { "decimal", "1" },
                { "char", "'b'" },
                { "bool", "true" },
                { "Base64FormattingOptions", "Base64FormattingOptions.InsertLineBreaks" },
                { "S", "new S(1)" }
            };

            foreach (string t in types)
            {
                string s = source.Replace("TYPE", t).Replace("OP", oper).Replace("ZERO", zeros[t]).Replace("ONE", ones[t]);
                var verifier = CompileAndVerify(source: s, expectedOutput: expected);
            }
        }

        [Fact]
        public void TestLiftedBuiltInBinaryArithmetic()
        {
            string[,] enumAddition =
            {
                //{ "sbyte", "Base64FormattingOptions"},
                { "byte", "Base64FormattingOptions"},
                //{ "short", "Base64FormattingOptions"},
                { "ushort", "Base64FormattingOptions"},
                //{ "int", "Base64FormattingOptions"},
                //{ "uint", "Base64FormattingOptions"},
                //{ "long", "Base64FormattingOptions"},
                //{ "ulong", "Base64FormattingOptions"},
                { "char", "Base64FormattingOptions"},
                //{ "decimal", "Base64FormattingOptions"},
                //{ "double", "Base64FormattingOptions"},
                //{ "float", "Base64FormattingOptions"},
                { "Base64FormattingOptions", "sbyte" },
                { "Base64FormattingOptions", "byte" },
                { "Base64FormattingOptions", "short" },
                { "Base64FormattingOptions", "ushort" },
                { "Base64FormattingOptions", "int" },
                //{ "Base64FormattingOptions", "uint" },
                //{ "Base64FormattingOptions", "long" },
                //{ "Base64FormattingOptions", "ulong" },
                { "Base64FormattingOptions", "char" },
                //{ "Base64FormattingOptions", "decimal" },
                //{ "Base64FormattingOptions", "double" },
                //{ "Base64FormattingOptions", "float" },
                //{ "Base64FormattingOptions", "Base64FormattingOptions"},
            };

            string[,] enumSubtraction =
            {
                { "Base64FormattingOptions", "sbyte" },
                //{ "Base64FormattingOptions", "byte" },
                { "Base64FormattingOptions", "short" },
                { "Base64FormattingOptions", "ushort" },
                { "Base64FormattingOptions", "int" },
                //{ "Base64FormattingOptions", "uint" },
                //{ "Base64FormattingOptions", "long" },
                //{ "Base64FormattingOptions", "ulong" },
                //{ "Base64FormattingOptions", "char" },
                //{ "Base64FormattingOptions", "decimal" },
                //{ "Base64FormattingOptions", "double" },
                //{ "Base64FormattingOptions", "float" },
                { "Base64FormattingOptions", "Base64FormattingOptions"},
            };

            string[,] numerics1 =
            {
                { "sbyte", "sbyte" },
                { "sbyte", "byte" },
                //{ "sbyte", "short" },
                { "sbyte", "ushort" },
                //{ "sbyte", "int" },
                { "sbyte", "uint" },
                //{ "sbyte", "long" },
                //{ "sbyte", "ulong" },
                //{ "sbyte", "char" },
                { "sbyte", "decimal" },
                { "sbyte", "double" },
                //{ "sbyte", "float" },

                //{ "byte", "sbyte" },
                { "byte", "byte" },
                //{ "byte", "short" },
                { "byte", "ushort" },
                //{ "byte", "int" },
                { "byte", "uint" },
                //{ "byte", "long" },
                { "byte", "ulong" },
                //{ "byte", "char" },
                { "byte", "decimal" },
                //{ "byte", "double" },
                { "byte", "float" },

                { "short", "sbyte" },
                { "short", "byte" },
                { "short", "short" },
                //{ "short", "ushort" },
                { "short", "int" },
                //{ "short", "uint" },
                { "short", "long" },
                //{ "short", "ulong" },
                //{ "short", "char" },
                { "short", "decimal" },
                //{ "short", "double" },
                { "short", "float" },
            };

            string[,] numerics2 =
            {
                //{ "ushort", "sbyte" },
                //{ "ushort", "byte" },
                { "ushort", "short" },
                { "ushort", "ushort" },
                //{ "ushort", "int" },
                { "ushort", "uint" },
                { "ushort", "long" },
                //{ "ushort", "ulong" },
                //{ "ushort", "char" },
                //{ "ushort", "decimal" },
                //{ "ushort", "double" },
                { "ushort", "float" },

                { "int", "sbyte" },
                { "int", "byte" },
                //{ "int", "short" },
                { "int", "ushort" },
                { "int", "int" },
                //{ "int", "uint" },
                { "int", "long" },
                // { "int", "ulong" },
                { "int", "char" },
                //{ "int", "decimal" },
                { "int", "double" },
                //{ "int", "float" },

                //{ "uint", "sbyte" },
                //{ "uint", "byte" },
                { "uint", "short" },
                //{ "uint", "ushort" },
                { "uint", "int" },
                { "uint", "uint" },
                { "uint", "long" },
                //{ "uint", "ulong" },
                { "uint", "char" },
                //{ "uint", "decimal" },
                //{ "uint", "double" },
                { "uint", "float" },
            };

            string[,] numerics3 =
            {
                { "long", "sbyte" },
                { "long", "byte" },
                //{ "long", "short" },
                //{ "long", "ushort" },
                //{ "long", "int" },
                //{ "long", "uint" },
                { "long", "long" },
                // { "long", "ulong" },
                { "long", "char" },
                //{ "long", "decimal" },
                //{ "long", "double" },
                { "long", "float" },

                //{ "ulong", "sbyte" },
                //{ "ulong", "byte" },
                //{ "ulong", "short" },
                { "ulong", "ushort" },
                //{ "ulong", "int" },
                { "ulong", "uint" },
                //{ "ulong", "long" },
                { "ulong", "ulong" },
                //{ "ulong", "char" },
                { "ulong", "decimal" },
                { "ulong", "double" },
                //{ "ulong", "float" },
            };

            string[,] numerics4 =
            {
                { "char", "sbyte" },
                { "char", "byte" },
                { "char", "short" },
                { "char", "ushort" },
                //{ "char", "int" },
                //{ "char", "uint" },
                //{ "char", "long" },
                { "char", "ulong" },
                { "char", "char" },
                //{ "char", "decimal" },
                //{ "char", "double" },
                { "char", "float" },

                //{ "decimal", "sbyte" },
                //{ "decimal", "byte" },
                //{ "decimal", "short" },
                { "decimal", "ushort" },
                { "decimal", "int" },
                { "decimal", "uint" },
                { "decimal", "long" },
                //{ "decimal", "ulong" },
                { "decimal", "char" },
                { "decimal", "decimal" },
                //{ "decimal", "double" },
                //{ "decimal", "float" },
            };

            string[,] numerics5 =
            {
                //{ "double", "sbyte" },
                { "double", "byte" },
                { "double", "short" },
                { "double", "ushort" },
                //{ "double", "int" },
                { "double", "uint" },
                { "double", "long" },
                //{ "double", "ulong" },
                { "double", "char" },
                //{ "double", "decimal" },
                { "double", "double" },
                { "double", "float" },

                { "float", "sbyte" },
                //{ "float", "byte" },
                //{ "float", "short" },
                //{ "float", "ushort" },
                { "float", "int" },
                //{ "float", "uint" },
                //{ "float", "long" },
                { "float", "ulong" },
                //{ "float", "char" },
                //{ "float", "decimal" },
                //{ "float", "double" },
                { "float", "float" },
           };

            string[,] shift1 =
            {
                { "sbyte", "sbyte" },
                { "sbyte", "byte" },
                { "sbyte", "short" },
                { "sbyte", "ushort" },
                { "sbyte", "int" },
                { "sbyte", "char" },

                { "byte", "sbyte" },
                { "byte", "byte" },
                { "byte", "short" },
                { "byte", "ushort" },
                { "byte", "int" },
                { "byte", "char" },

                { "short", "sbyte" },
                { "short", "byte" },
                { "short", "short" },
                { "short", "ushort" },
                { "short", "int" },
                { "short", "char" },

                { "ushort", "sbyte" },
                { "ushort", "byte" },
                { "ushort", "short" },
                { "ushort", "ushort" },
                { "ushort", "int" },
                { "ushort", "char" },
            };

            string[,] shift2 =
            {
                { "int", "sbyte" },
                { "int", "byte" },
                { "int", "short" },
                { "int", "ushort" },
                { "int", "int" },
                { "int", "char" },

                { "uint", "sbyte" },
                { "uint", "byte" },
                { "uint", "short" },
                { "uint", "ushort" },
                { "uint", "int" },
                { "uint", "char" },

                { "long", "sbyte" },
                { "long", "byte" },
                { "long", "short" },
                { "long", "ushort" },
                { "long", "int" },
                { "long", "char" },

                { "ulong", "sbyte" },
                { "ulong", "byte" },
                { "ulong", "short" },
                { "ulong", "ushort" },
                { "ulong", "int" },
                { "ulong", "char" },

                { "char", "sbyte" },
                { "char", "byte" },
                { "char", "short" },
                { "char", "ushort" },
                { "char", "int" },
                { "char", "char" },
            };

            string[,] logical1 =
            {
                { "sbyte", "sbyte" },
                //{ "sbyte", "byte" },
                { "sbyte", "short" },
                { "sbyte", "ushort" },
                { "sbyte", "int" },
                //{ "sbyte", "uint" },
                { "sbyte", "long" },
                //{ "sbyte", "ulong" },
                //{ "sbyte", "char" },

                { "byte", "sbyte" },
                { "byte", "byte" },
                //{ "byte", "short" },
                { "byte", "ushort" },
                //{ "byte", "int" },
                { "byte", "uint" },
                //{ "byte", "long" },
                //{ "byte", "ulong" },
                { "byte", "char" },

                { "short", "sbyte" },
                { "short", "byte" },
                { "short", "short" },
                //{ "short", "ushort" },
                { "short", "int" },
                //{ "short", "uint" },
                { "short", "long" },
                //{ "short", "ulong" },
                { "short", "char" },
            };

            string[,] logical2 =
            {
                //{ "ushort", "sbyte" },
                { "ushort", "byte" },
                { "ushort", "short" },
                { "ushort", "ushort" },
                //{ "ushort", "int" },
                { "ushort", "uint" },
                //{ "ushort", "long" },
                //{ "ushort", "ulong" },
                //{ "ushort", "char" },

                //{ "int", "sbyte" },
                { "int", "byte" },
                //{ "int", "short" },
                { "int", "ushort" },
                { "int", "int" },
                //{ "int", "uint" },
                { "int", "long" },
                //{ "int", "ulong" },
                //{ "int", "char" },

                { "uint", "sbyte" },
                //{ "uint", "byte" },
                { "uint", "short" },
                //{ "uint", "ushort" },
                { "uint", "int" },
                { "uint", "uint" },
                //{ "uint", "long" },
                //{ "uint", "ulong" },
                { "uint", "char" },
            };

            string[,] logical3 =
            {
                //{ "long", "sbyte" },
                { "long", "byte" },
                //{ "long", "short" },
                { "long", "ushort" },
                //{ "long", "int" },
                { "long", "uint" },
                { "long", "long" },
                // { "long", "ulong" },
                { "long", "char" },

                //{ "ulong", "sbyte" },
                { "ulong", "byte" },
                //{ "ulong", "short" },
                { "ulong", "ushort" },
                //{ "ulong", "int" },
                { "ulong", "uint" },
                //{ "ulong", "long" },
                { "ulong", "ulong" },
                //{ "ulong", "char" },

                { "char", "sbyte" },
                //{ "char", "byte" },
                //{ "char", "short" },
                { "char", "ushort" },
                { "char", "int" },
                //{ "char", "uint" },
                //{ "char", "long" },
                { "char", "ulong" },
                { "char", "char" },

                { "Base64FormattingOptions", "Base64FormattingOptions"},
            };

            // Use 2 instead of 0 so that we don't get divide by zero errors.
            var twos = new Dictionary<string, string>()
            {
                { "int", "2" },
                { "uint", "2" },
                { "short", "2" },
                { "ushort", "2" },
                { "byte", "2" },
                { "sbyte", "2" },
                { "long", "2" },
                { "ulong", "2" },
                { "double", "2" },
                { "float", "2" },
                { "decimal", "2" },
                { "char", "'\\u0002'" },
                { "Base64FormattingOptions", "Base64FormattingOptions.None" },
            };
            var ones = new Dictionary<string, string>()
            {
                { "int", "1" },
                { "uint", "1" },
                { "short", "1" },
                { "ushort", "1" },
                { "byte", "1" },
                { "sbyte", "1" },
                { "long", "1" },
                { "ulong", "1" },
                { "double", "1" },
                { "float", "1" },
                { "decimal", "1" },
                { "char", "'\\u0001'" },
                { "Base64FormattingOptions", "Base64FormattingOptions.InsertLineBreaks" },
            };

            var names = new Dictionary<string, string>()
            {
                { "+", "plus" },
                { "-", "minus" },
                { "*", "times" },
                { "/", "divide" },
                { "%", "remainder" },
                { ">>", "rshift" },
                { ">>>", "urshift" },
                { "<<", "lshift" },
                { "&", "and" },
                { "|", "or" },
                { "^", "xor" }
            };

            var source = new StringBuilder(@"
using System; 
class C 
{
  static void T(int x, bool b)
  {
    if (!b) throw new Exception(x.ToString());
  }
  static void F(int x, bool b)
  {
    if (b) throw new Exception(x.ToString());
  }
");
            string main = "static void Main() {";
            string method = @"
  static void METHOD_TYPEX_NAME_TYPEY()
  {
    TYPEX? xn0 = TWOX;
    TYPEX? xn1 = ONEX;
    TYPEX? xnn = null;
    TYPEX x0 = TWOX;
    TYPEX x1 = ONEX;

    TYPEY? yn0 = TWOY;
    TYPEY? yn1 = ONEY;
    TYPEY? ynn = null;
    TYPEY y0 = TWOY;
    TYPEY y1 = ONEY;

    F(1, (null OP yn0).HasValue);
    F(2, (null OP yn1).HasValue);
    F(3, (null OP ynn).HasValue);
    F(4, (null OP y0).HasValue);
    F(5, (null OP y1).HasValue);

    F(6, (xn0 OP null).HasValue);
    T(7, (xn0 OP yn0).Value == (x0 OP y0));
    T(8, (xn0 OP yn1).Value == (x0 OP y1));
    F(9, (xn0 OP ynn).HasValue);
    T(10, (xn0 OP y0).Value == (x0 OP y0));
    T(11, (xn0 OP y1).Value == (x0 OP y1));

    F(12, (xn1 OP null).HasValue);
    T(13, (xn1 OP yn0).Value == (x1 OP y0));
    T(14, (xn1 OP yn1).Value == (x1 OP y1));
    F(15, (xn1 OP ynn).HasValue);
    T(16, (xn1 OP y0).Value == (x1 OP y0));
    T(17, (xn1 OP y1).Value == (x1 OP y1));

    F(18, (xnn OP null).HasValue);
    F(19, (xnn OP yn0).HasValue);
    F(20, (xnn OP yn1).HasValue);
    F(21, (xnn OP ynn).HasValue);
    F(22, (xnn OP y0).HasValue);
    F(23, (xnn OP y1).HasValue);

    F(24, (x0 OP null).HasValue);
    T(25, (x0 OP yn0).Value == (x0 OP y0));
    T(26, (x0 OP yn1).Value == (x0 OP y1));
    F(27, (x0 OP ynn).HasValue);

    F(28, (x1 OP null).HasValue);
    T(29, (x1 OP yn0).Value == (x1 OP y0));
    T(30, (x1 OP yn1).Value == (x1 OP y1));
    F(31, (x1 OP ynn).HasValue);
  }";

            List<Tuple<string, string[,]>> items = new List<Tuple<string, string[,]>>()
            {
                Tuple.Create("*", numerics1),
                Tuple.Create("/", numerics2),
                Tuple.Create("%", numerics3),
                Tuple.Create("+", numerics4),
                Tuple.Create("+", enumAddition),
                Tuple.Create("-", numerics5),
                // UNDONE: Overload resolution of "enum - null" ,
                // UNDONE: so this test is disabled:
                // UNDONE: Tuple.Create("-", enumSubtraction),
                Tuple.Create(">>", shift1),
                Tuple.Create(">>>", shift1),
                Tuple.Create("<<", shift2),
                Tuple.Create("&", logical1),
                Tuple.Create("|", logical2),
                Tuple.Create("^", logical3)
            };

            int m = 0;

            foreach (var item in items)
            {
                string oper = item.Item1;
                string[,] types = item.Item2;
                for (int i = 0; i < types.GetLength(0); ++i)
                {
                    ++m;
                    string typeX = types[i, 0];
                    string typeY = types[i, 1];
                    source.Append(method
                    .Replace("METHOD", "M" + m)
                    .Replace("TYPEX", typeX)
                    .Replace("TYPEY", typeY)
                    .Replace("OP", oper)
                    .Replace("NAME", names[oper])
                    .Replace("TWOX", twos[typeX])
                    .Replace("ONEX", ones[typeX])
                    .Replace("TWOY", twos[typeY])
                    .Replace("ONEY", ones[typeY]));

                    main += "M" + m + "_" + typeX + "_" + names[oper] + "_" + typeY + "();\n";
                }
            }

            source.Append(main);
            source.Append("} }");

            var verifier = CompileAndVerify(source: source.ToString(), expectedOutput: "");
        }

        [Fact]
        public void TestLiftedUserDefinedBinaryArithmetic()
        {
            string source = @"
using System;
struct SX
{
    public string str;
    public SX(string str) { this.str = str; }
    public SX(char c) { this.str = c.ToString(); }
    public static SZ operator +(SX sx, SY sy) { return new SZ(sx.str + '+' + sy.str); }
    public static SZ operator -(SX sx, SY sy) { return new SZ(sx.str + '-' + sy.str); }
    public static SZ operator *(SX sx, SY sy) { return new SZ(sx.str + '*' + sy.str); }
    public static SZ operator /(SX sx, SY sy) { return new SZ(sx.str + '/' + sy.str); }
    public static SZ operator %(SX sx, SY sy) { return new SZ(sx.str + '%' + sy.str); }
    public static SZ operator &(SX sx, SY sy) { return new SZ(sx.str + '&' + sy.str); }
    public static SZ operator |(SX sx, SY sy) { return new SZ(sx.str + '|' + sy.str); }
    public static SZ operator ^(SX sx, SY sy) { return new SZ(sx.str + '^' + sy.str); }
    public static SZ operator >>(SX sx, int i) { return new SZ(sx.str + '>' + '>' + i.ToString()); }
    public static SZ operator <<(SX sx, int i) { return new SZ(sx.str + '<' + '<' + i.ToString()); }
}

struct SY
{
    public string str;
    public SY(string str) { this.str = str; }
    public SY(char c) { this.str = c.ToString(); }
}

struct SZ
{
    public string str;
    public SZ(string str) { this.str = str; }
    public SZ(char c) { this.str = c.ToString(); }
    public static bool operator ==(SZ sz1, SZ sz2) { return sz1.str == sz2.str; }
    public static bool operator !=(SZ sz1, SZ sz2) { return sz1.str != sz2.str; }
    public override bool Equals(object x) { return true; }
    public override int GetHashCode() { return 0; }
}
class C
{
    static void T(bool b)
    {
        if (!b) throw new Exception();
    }
    static void F(bool b)
    {
        if (b) throw new Exception();
    }
    static void Main()
    {
        SX sx = new SX('a');
        SX? sxn = sx;
        SX? sxnn = null;
        
        SY sy = new SY('b');
        SY? syn = sy;
        SY? synn = null;

        int i1 = 1; 
        int? i1n = 1;
        int? i1nn = null; 
";

            source += @"
                T((sx + syn).Value == (sx + sy));
                F((sx - synn).HasValue);
                F((sx * null).HasValue);
                T((sxn % sy).Value == (sx % sy));
                T((sxn / syn).Value == (sx / sy));
                F((sxn ^ synn).HasValue);
                F((sxn & null).HasValue);
                F((sxnn | sy).HasValue);
                F((sxnn ^ syn).HasValue);
                F((sxnn + synn).HasValue);
                F((sxnn - null).HasValue);";

            source += @"
                T((sx << i1n).Value == (sx << i1));
                F((sx >> i1nn).HasValue);
                F((sx << null).HasValue);
                T((sxn >> i1).Value == (sx >> i1));
                T((sxn << i1n).Value == (sx << i1));
                F((sxn >> i1nn).HasValue);
                F((sxn << null).HasValue);
                F((sxnn >> i1).HasValue);
                F((sxnn << i1n).HasValue);
                F((sxnn >> i1nn).HasValue);
                F((sxnn << null).HasValue);";

            source += "}}";

            var verifier = CompileAndVerify(source: source, expectedOutput: "");
        }

        [Fact]
        public void TestLiftedBoolLogicOperators()
        {
            string source = @"
using System;
class C
{
    
    static void T(int x, bool? b) { if (!(b.HasValue && b.Value)) throw new Exception(x.ToString()); }
    static void F(int x, bool? b) { if (!(b.HasValue && !b.Value)) throw new Exception(x.ToString()); }
    static void N(int x, bool? b) { if (b.HasValue) throw new Exception(x.ToString()); }

    static void Main()
    {
        bool bt = true;
        bool bf = false;
        bool? bnt = bt;
        bool? bnf = bf;
        bool? bnn = null;

            

        T(1, true & bnt);
        T(2, true & bnt);
        F(3, true & bnf);
        N(4, true & null);
        N(5, true & bnn);

        T(6, bt & bnt);
        T(7, bt & bnt);
        F(8, bt & bnf);
        N(9, bt & null);
        N(10, bt & bnn);

        T(11, bnt & true);
        T(12, bnt & bt);
        T(13, bnt & bnt);
        F(14, bnt & false);
        F(15, bnt & bf);
        F(16, bnt & bnf);
        N(17, bnt & null);
        N(18, bnt & bnn);

        F(19, false & bnt);
        F(20, false & bnf);
        F(21, false & null);
        F(22, false & bnn);

        F(23, bf & bnt);
        F(24, bf & bnf);
        F(25, bf & null);
        F(26, bf & bnn);

        F(27, bnf & true);
        F(28, bnf & bt);
        F(29, bnf & bnt);
        F(30, bnf & false);
        F(31, bnf & bf);
        F(32, bnf & bnf);
        F(33, bnf & null);
        F(34, bnf & bnn);
        
        N(35, null & true);
        N(36, null & bt);
        N(37, null & bnt);
        F(38, null & false);
        F(39, null & bf);
        F(40, null & bnf);
        N(41, null & bnn);

        N(42, bnn & true);
        N(43, bnn & bt);
        N(44, bnn & bnt);
        F(45, bnn & false);
        F(46, bnn & bf);
        F(47, bnn & bnf);
        N(48, bnn & null);
        N(49, bnn & bnn);

        T(51, true | bnt);
        T(52, true | bnt);
        T(53, true | bnf);
        T(54, true | null);
        T(55, true | bnn);

        T(56, bt | bnt);
        T(57, bt | bnt);
        T(58, bt | bnf);
        T(59, bt | null);
        T(60, bt | bnn);

        T(61, bnt | true);
        T(62, bnt | bt);
        T(63, bnt | bnt);
        T(64, bnt | false);
        T(65, bnt | bf);
        T(66, bnt | bnf);
        T(67, bnt | null);
        T(68, bnt | bnn);

        T(69, false | bnt);
        F(70, false | bnf);
        N(71, false | null);
        N(72, false | bnn);

        T(73, bf | bnt);
        F(74, bf | bnf);
        N(75, bf | null);
        N(76, bf | bnn);

        T(77, bnf | true);
        T(78, bnf | bt);
        T(79, bnf | bnt);
        F(80, bnf | false);
        F(81, bnf | bf);
        F(82, bnf | bnf);
        N(83, bnf | null);
        N(84, bnf | bnn);
       
        T(85, null | true);
        T(86, null | bt);
        T(87, null | bnt);
        N(88, null | false);
        N(89, null | bf);
        N(90, null | bnf);
        N(91, null | bnn);

        T(92, bnn | true);
        T(93, bnn | bt);
        T(94, bnn | bnt);
        N(95, bnn | false);
        N(96, bnn | bf);
        N(97, bnn | bnf);
        N(98, bnn | null);
        N(99, bnn | bnn);

        F(101, true ^ bnt);
        F(102, true ^ bnt);
        T(103, true ^ bnf);
        N(104, true ^ null);
        N(105, true ^ bnn);

        F(106, bt ^ bnt);
        F(107, bt ^ bnt);
        T(108, bt ^ bnf);
        N(109, bt ^ null);
        N(110, bt ^ bnn);

        F(111, bnt ^ true);
        F(112, bnt ^ bt);
        F(113, bnt ^ bnt);
        T(114, bnt ^ false);
        T(115, bnt ^ bf);
        T(116, bnt ^ bnf);
        N(117, bnt ^ null);
        N(118, bnt ^ bnn);

        T(119, false ^ bnt);
        F(120, false ^ bnf);
        N(121, false ^ null);
        N(122, false ^ bnn);

        T(123, bf ^ bnt);
        F(124, bf ^ bnf);
        N(125, bf ^ null);
        N(126, bf ^ bnn);

        T(127, bnf ^ true);
        T(128, bnf ^ bt);
        T(129, bnf ^ bnt);
        F(130, bnf ^ false);
        F(131, bnf ^ bf);
        F(132, bnf ^ bnf);
        N(133, bnf ^ null);
        N(134, bnf ^ bnn);
        
        N(135, null ^ true);
        N(136, null ^ bt);
        N(137, null ^ bnt);
        N(138, null ^ false);
        N(139, null ^ bf);
        N(140, null ^ bnf);
        N(141, null ^ bnn);

        N(142, bnn ^ true);
        N(143, bnn ^ bt);
        N(144, bnn ^ bnt);
        N(145, bnn ^ false);
        N(146, bnn ^ bf);
        N(147, bnn ^ bnf);
        N(148, bnn ^ null);
        N(149, bnn ^ bnn);

    }
}";

            var verifier = CompileAndVerify(source: source, expectedOutput: "");
        }

        [Fact]
        public void TestLiftedCompoundAssignment()
        {
            string source = @"
using System;
class C
{
  static void Main()
  {
    int? n = 1;
    int a = 2;
    int? b = 3;
    short c = 4;
    short? d = 5;
    int? e = null;
    
    n += a;
    T(1, n == 3);
    n += b;
    T(2, n == 6);
    n += c;
    T(3, n == 10);
    n += d;
    T(4, n == 15);
    n += e;
    F(5, n.HasValue);
    n += a;
    F(6, n.HasValue);
    n += b;
    F(7, n.HasValue);
    n += c;
    F(8, n.HasValue);
    n += d;
    F(9, n.HasValue);

    Console.WriteLine(123);
  }

    static void T(int x, bool b) { if (!b) throw new Exception(x.ToString()); }
    static void F(int x, bool b) { if (b) throw new Exception(x.ToString()); }
}";

            var verifier = CompileAndVerify(source: source, expectedOutput: "123");
        }

        #region "Regression"

        [Fact, WorkItem(543837, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543837")]
        public void Test11827()
        {
            string source2 = @"
using System;
class Program
{       
    static void Main()
    {
        Func<decimal?, decimal?> lambda = a => { return checked(a * a); };
        Console.WriteLine(0);
    }
}";
            var verifier = CompileAndVerify(source: source2, expectedOutput: "0");
        }

        [Fact, WorkItem(544001, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544001")]
        public void NullableUsedInUsingStatement()
        {
            string source = @"
using System;

struct S : IDisposable
{
    public void Dispose()
    {
        Console.WriteLine(123);
    }

    static void Main()
    {
        using (S? r = new S())
        {
            Console.Write(r);
        }
    }
}
";

            CompileAndVerify(source: source, expectedOutput: @"S123");
        }

        [Fact, WorkItem(544002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544002")]
        public void NullableUserDefinedUnary02()
        {
            string source = @"
using System;

struct S
{
    public static int operator +(S s) { return 1; }
    public static int operator +(S? s) { return s.HasValue ? 10 : -10; }

    public static int operator -(S s) { return 2; }
    public static int operator -(S? s) { return s.HasValue ? 20 : -20; }

    public static int operator !(S s) { return 3; }
    public static int operator !(S? s) { return s.HasValue ? 30 : -30; }

    public static int operator ~(S s) { return 4; }
    public static int operator ~(S? s) { return s.HasValue ? 40 : -40; }

    public static void Main()
    {
        S? sq = new S();
        Console.Write(+sq);
        Console.Write(-sq);
        Console.Write(!sq);
        Console.Write(~sq);

        sq = null;
        Console.Write(+sq);
        Console.Write(-sq);
        Console.Write(!sq);
        Console.Write(~sq);
    }
}
";

            CompileAndVerify(source: source, expectedOutput: @"10203040-10-20-30-40");
        }

        [Fact, WorkItem(544005, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544005")]
        public void NoNullableValueFromOptionalParam()
        {
            string source = @"
class Test
{
    static void M(
        double? d0 = null,
        double? d1 = 1.11,
        double? d2 = 2,
        double? d3 = default(double?),
        double d4 = 4,
        double d5 = default(double),
        string s6 = ""6"",
        string s7 = null,
        string s8 = default(string)) 
    { 
        System.Console.WriteLine(""0:{0} 1:{1} 2:{2} 3:{3} 4:{4} 5:{5} 6:{6} 7:{7} 8:{8}"",
            d0, d1.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), d2, d3, d4, d5, s6, s7, s8);
    }

    static void Main()
    {
        M();
    }
}
";
            string expected = @"0: 1:1.11 2:2 3: 4:4 5:0 6:6 7: 8:";

            var verifier = CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact, WorkItem(544006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544006")]
        public void ConflictImportedMethodWithNullableOptionalParam()
        {
            string source = @"
public class Parent
{
    public int Goo(int? d = 0) { return (int)d; }
}
";
            string source2 = @"
public class Parent
{
    public int Goo(int? d = 0) { return (int)d; }
}

public class Test
{
    public static void Main()
    {
        Parent p = new Parent();
        System.Console.Write(p.Goo(0));
    }
}
";

            var complib = CreateCompilation(
                source,
                options: TestOptions.ReleaseDll,
                assemblyName: "TestDLL");

            var comp = CreateCompilation(
                source2,
                references: new MetadataReference[] { complib.EmitToImageReference() },
                options: TestOptions.ReleaseExe,
                assemblyName: "TestEXE");

            comp.VerifyDiagnostics(
                // (11,9): warning CS0436: The type 'Parent' in '' conflicts with the imported type 'Parent' in 'TestDLL, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         Parent p = new Parent();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Parent").WithArguments("", "Parent", "TestDLL, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Parent"),
                // (11,24): warning CS0436: The type 'Parent' in '' conflicts with the imported type 'Parent' in 'TestDLL, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         Parent p = new Parent();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Parent").WithArguments("", "Parent", "TestDLL, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Parent")
                );

            CompileAndVerify(comp, expectedOutput: @"0");
        }

        [Fact, WorkItem(544258, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544258")]
        public void BindDelegateToObjectMethods()
        {
            string source = @"
using System;
public class Test
{
    delegate int I();
    static void Main()
    {
        int? x = 123;
        Func<string> d1 = x.ToString;
        I d2 = x.GetHashCode;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(544909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544909")]
        public void OperationOnEnumNullable()
        {
            string source = @"
using System;
public class NullableTest
{
    public enum E : short { Zero = 0, One = 1, Max = System.Int16.MaxValue, Min = System.Int16.MinValue }
    static E? NULL = null;

    public static void Main()
    {
        E? nub = 0;
        Test(nub.HasValue); // t
        nub = NULL;
        Test(nub.HasValue); // f
        nub = ~nub;
        Test(nub.HasValue); // f
        nub = NULL++;
        Test(nub.HasValue); // f
        nub = 0;
        nub++;
        Test(nub.HasValue); // t
        Test(nub.GetValueOrDefault() == E.One); // t
        nub = E.Max;
        nub++;
        Test(nub.GetValueOrDefault() == E.Min); // t
    }
    static void Test(bool b) 
    {
        Console.Write(b ? 't' : 'f');
    }
}
";
            CompileAndVerify(source, expectedOutput: "tfffttt");
        }

        [Fact, WorkItem(544583, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544583")]
        public void ShortCircuitOperatorsOnNullable()
        {
            string source = @"
class A
{
    static void Main()
    {
        bool? b1 = true, b2 = false;
        var bb = b1 && b2;
        bb = b1 || b2;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
// (7,18): error CS0019: Operator '&&' cannot be applied to operands of type 'bool?' and 'bool?'
//         var bb = b1 && b2;
Diagnostic(ErrorCode.ERR_BadBinaryOps, "b1 && b2").WithArguments("&&", "bool?", "bool?"),
// (8,14): error CS0019: Operator '||' cannot be applied to operands of type 'bool?' and 'bool?'
//         bb = b1 || b2;
Diagnostic(ErrorCode.ERR_BadBinaryOps, "b1 || b2").WithArguments("||", "bool?", "bool?")
                );
        }

        [Fact]
        public void ShortCircuitLiftedUserDefinedOperators()
        {
            // This test illustrates a bug in the native compiler which Roslyn fixes.
            // The native compiler disallows a *lifted* & operator from being used as an &&
            // operator, but allows a *nullable* & operator to be used as an && operator.
            // There is no good reason for this discrepancy; either both should be legal
            // (because we can obviously generate good code that does what the user wants)
            // or we should disallow both. 

            string source = @"
using System;
struct C 
{
    public bool b;
    public C(bool b) { this.b = b; }
    public static C operator &(C c1, C c2) { return new C(c1.b & c2.b); }
    public static C operator |(C c1, C c2) { return new C(c1.b | c2.b); }

    // null is false
    public static bool operator true(C? c) { return c == null ? false : c.Value.b; }
    public static bool operator false(C? c) { return c == null ? true : !c.Value.b; }

    public static C? True() { Console.Write('t'); return new C(true); }
    public static C? False() { Console.Write('f'); return new C(false); }
    public static C? Null() { Console.Write('n'); return new C?(); }
}

struct D
{
    public bool b;
    public D(bool b) { this.b = b; }
    public static D? operator &(D? d1, D? d2) { return d1.HasValue && d2.HasValue ? new D?(new D(d1.Value.b & d2.Value.b)) : (D?)null; }
    public static D? operator |(D? d1, D? d2) { return d1.HasValue && d2.HasValue ? new D?(new D(d1.Value.b | d2.Value.b)) : (D?)null; }

    // null is false
    public static bool operator true(D? d) { return d == null ? false : d.Value.b; }
    public static bool operator false(D? d) { return d == null ? true : !d.Value.b; }
    
    public static D? True() { Console.Write('t'); return new D(true); }
    public static D? False() { Console.Write('f'); return new D(false); }
    public static D? Null() { Console.Write('n'); return new D?(); }
}

class P
{
    static void Main()
    {
        D?[] results1 = 
        {
            D.True() && D.True(),   // tt --> t
            D.True() && D.False(),  // tf --> f
            D.True() && D.Null(),   // tn --> n
            D.False() && D.True(),  // f  --> f
            D.False() && D.False(), // f  --> f
            D.False() && D.Null(),  // f  --> f
            D.Null() && D.True(),   // n  --> n
            D.Null() && D.False(),  // n  --> n
            D.Null() && D.Null()    // n  --> n
        };
        Console.WriteLine();

        foreach(D? r in results1)
            Console.Write(r == null ? 'n' : r.Value.b ? 't' : 'f');

        Console.WriteLine();

        C?[] results2 = 
        {
            C.True() && C.True(),
            C.True() && C.False(),
            C.True() && C.Null(),
            C.False() && C.True(),
            C.False() && C.False(),
            C.False() && C.Null(),
            C.Null() && C.True(),
            C.Null() && C.False(),
            C.Null() && C.Null()
        };
        Console.WriteLine();

        foreach(C? r in results2)
            Console.Write(r == null ? 'n' : r.Value.b ? 't' : 'f');

        Console.WriteLine();

D?[] results3 = 
        {
            D.True() || D.True(),    // t --> t
            D.True() || D.False(),   // t --> t
            D.True() || D.Null(),    // t --> t
            D.False() || D.True(),   // ft --> t
            D.False() || D.False(),  // ff --> f
            D.False() || D.Null(),   // fn --> n
            D.Null() || D.True(),    // nt --> n
            D.Null() || D.False(),   // nf --> n
            D.Null() || D.Null()     // nn --> n
        };
        Console.WriteLine();

        foreach(D? r in results3)
            Console.Write(r == null ? 'n' : r.Value.b ? 't' : 'f');

        Console.WriteLine();

        C?[] results4 = 
        {
            C.True() || C.True(),
            C.True() || C.False(),
            C.True() || C.Null(),
            C.False() || C.True(),
            C.False() || C.False(),
            C.False() || C.Null(),
            C.Null() || C.True(),
            C.Null() || C.False(),
            C.Null() || C.Null()
        };
        Console.WriteLine();

        foreach(C? r in results4)
            Console.Write(r == null ? 'n' : r.Value.b ? 't' : 'f');
    }
}
";
            string expected = @"tttftnfffnnn
tfnfffnnn
tttftnfffnnn
tfnfffnnn
tttftfffnntnfnn
ttttfnnnn
tttftfffnntnfnn
ttttfnnnn";

            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact, WorkItem(529530, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529530"), WorkItem(1036392, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036392")]
        public void NullableEnumMinusNull()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Base64FormattingOptions? xn0 = Base64FormattingOptions.None;
        Console.WriteLine((xn0 - null).HasValue);
    }
}";

            CompileAndVerify(source, expectedOutput: "False").VerifyDiagnostics(
    // (9,28): warning CS0458: The result of the expression is always 'null' of type 'int?'
    //         Console.WriteLine((xn0 - null).HasValue);
    Diagnostic(ErrorCode.WRN_AlwaysNull, "xn0 - null").WithArguments("int?").WithLocation(9, 28)
                );
        }

        [Fact]
        public void NullableNullEquality()
        {
            var source = @"
using System;

public struct S
{
    public static void Main()
    {
        S? s = new S();
        Console.WriteLine(null == s);
    }
}";

            CompileAndVerify(source, expectedOutput: "False");
        }

        [Fact, WorkItem(545166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545166")]
        public void Op_ExplicitImplicitOnNullable()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
        var x = (int?).op_Explicit(1);
        var y = (Nullable<int>).op_Implicit(2); 
    }
}
";

            // VB now allow these syntax, but C# does NOT (spec said)
            // Dev11 & Roslyn: (8,23): error CS1525: Invalid expression term '.'
            // ---
            // Dev11: error CS0118: 'int?' is a 'type' but is used like a 'variable'
            // Roslyn: (9,18): error CS0119: 'int?' is a type, which is not valid in the given context
            // Roslyn: (9,33): error CS0571: 'int?.implicit operator int?(int)': cannot explicitly call operator or accessor
            CreateCompilation(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ".").WithArguments("."),
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Nullable<int>").WithArguments("int?", "type"),
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "op_Implicit").WithArguments("int?.implicit operator int?(int)")
            );
        }

        #endregion

        [Fact]
        public void UserDefinedConversion_01()
        {
            var source = @"


_ = (bool?)new S();
bool? z;
z = new S();

z.GetValueOrDefault();

struct S
{
    [System.Obsolete()]
    public static implicit operator bool(S s) => true;
}
";

            CreateCompilation(source).VerifyEmitDiagnostics(
                // (4,5): warning CS0612: 'S.implicit operator bool(S)' is obsolete
                // _ = (bool?)new S();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "(bool?)new S()").WithArguments("S.implicit operator bool(S)").WithLocation(4, 5),
                // (6,5): warning CS0612: 'S.implicit operator bool(S)' is obsolete
                // z = new S();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new S()").WithArguments("S.implicit operator bool(S)").WithLocation(6, 5)
                );
        }

        [Fact]
        public void TestIsNullable1()
        {
            var source = @"
class C
{
    void M(object o)
    {
        if (o is int? i)
        {
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (6,23): error CS0103: The name 'i' does not exist in the current context
                //         if (o is int? i)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "i").WithArguments("i").WithLocation(6, 23),
                // (6,24): error CS1003: Syntax error, ':' expected
                //         if (o is int? i)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":").WithLocation(6, 24),
                // (6,24): error CS1525: Invalid expression term ')'
                //         if (o is int? i)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 24));
        }

        [Fact]
        public void TestIsNullable2()
        {
            var source = @"
using A = System.Nullable<int>;
class C
{
    void M(object o)
    {
        if (o is A i)
        {
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (7,18): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
                //         if (o is A i)
                Diagnostic(ErrorCode.ERR_PatternNullableType, "A").WithArguments("int").WithLocation(7, 18));
        }

        [Fact]
        public void TestIsNullable3()
        {
            var source = @"
using A = int?;
class C
{
    void M(object o)
    {
        if (o is A i)
        {
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (7,18): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
                //         if (o is A i)
                Diagnostic(ErrorCode.ERR_PatternNullableType, "A").WithArguments("int").WithLocation(7, 18));
        }
    }
}
