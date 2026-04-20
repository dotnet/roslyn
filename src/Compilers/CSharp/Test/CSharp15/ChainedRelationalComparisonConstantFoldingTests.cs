// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

/// <summary>
/// Constant-folding tests for "chained relational comparison" (C# preview feature;
/// spec §11.11.13). See proposals/chained-relational-comparison.md in dotnet/csharplang.
///
/// A chained relational comparison is a constant expression when every operand is a
/// constant expression (spec §11.11.13, "Constant expressions" interaction bullet).
/// This mirrors C#'s §11.20 rule that <c>X &amp;&amp; Y</c> is a constant only when
/// both X and Y are: the chain's value is <c>A &amp;&amp; (Y op B)</c>, with A the
/// inner link and <c>(Y op B)</c> the outer isolated comparison.
///
/// The tests below pin two things at once:
///   1. That FoldChainedRelationalOperator computes the correct constant value.
///   2. That the resulting constant flows through all compile-time-constant consumers
///      (const fields, default parameter values, attribute arguments, case labels,
///      unreachable-code flow analysis), so the chain works anywhere a hand-written
///      <c>&amp;&amp;</c> chain would.
/// </summary>
public sealed class ChainedRelationalComparisonConstantFoldingTests : CSharpTestBase
{
    [Theory]
    // Same-type chains: predefined int comparisons.
    [InlineData("0 < 5 < 10",           "True")]
    [InlineData("0 < 5 < 2",            "False")]  // inner true, outer false
    [InlineData("10 < 5 < 100",         "False")]  // inner false (short-circuit)
    // Mixed relational operators.
    [InlineData("0 <= 5 <= 10",         "True")]
    [InlineData("10 >= 5 >= 0",         "True")]
    [InlineData("1 < 2 > 0",            "True")]
    // N-ary chains.
    [InlineData("1 < 2 < 3 < 4",        "True")]
    [InlineData("1 < 2 < 3 < 4 < 5",    "True")]
    [InlineData("1 < 2 < 3 < 2",        "False")]  // n-ary with outer false
    // Boundary: equal operands.
    [InlineData("0 <= 0 <= 0",          "True")]
    [InlineData("0 < 0 < 0",            "False")]
    // Asymmetric conversions in the chain: the outer LeftConversion must fold too.
    [InlineData("0 < 5 < 10L",          "True")]
    [InlineData("0L < 5 < 10",          "True")]
    [InlineData("(short)0 < 5 < 10L",   "True")]
    [InlineData("0 < (short)5 < 10L",   "True")]
    // Negative numbers and overflow-adjacent values.
    [InlineData("int.MinValue < 0 < int.MaxValue", "True")]
    [InlineData("-1 < 0 < 1",           "True")]
    public void PredefinedOperators_FoldsToExpectedValue(string chainExpression, string expectedValue)
    {
        // A `const bool` field initializer requires its right-hand side to be a
        // constant expression. If the chain doesn't fold, the compiler reports CS0133
        // ("requires a value that can be converted to the target type to a constant
        // expression"). VerifyDiagnostics() with no expected diagnostics therefore
        // pins BOTH that the chain is considered a constant expression AND that its
        // folded value is what we print at runtime.
        var src = $$"""
            using System;

            class P
            {
                const bool B = {{chainExpression}};

                static void Main()
                {
                    Console.WriteLine(B);
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: expectedValue)
            .VerifyDiagnostics();
    }

    // Every permutation of {short, int, long} at the three operand positions, as constants.
    // Each chain must both fold (compile as a `const bool` field initializer) AND produce
    // the same value that the equivalent runtime-evaluated chain does. Values 0/5/10 fit
    // every listed type and produce True for every chain. This is the fold-time analog of
    // AsymmetricConversion_AllPermutationsOfShortIntLong.
    public static IEnumerable<object[]> AllSignedIntegralPermutations()
    {
        var types = new[] { "short", "int", "long" };
        foreach (var a in types)
            foreach (var b in types)
                foreach (var c in types)
                    yield return new object[] { a, b, c };
    }

    [Theory]
    [MemberData(nameof(AllSignedIntegralPermutations))]
    public void SignedIntegralPermutations_FoldMatchesRuntime(string aType, string bType, string cType)
    {
        // The const line proves the fold compiles: `const bool` requires the RHS to be a
        // constant expression, so CS0133 fires if folding returns null. The runtime line
        // lets us print both side-by-side, so a silent value drift between fold-time and
        // run-time would show up as a mismatched expected output.
        var src = $$"""
            using System;

            class P
            {
                const bool ConstResult = ({{aType}})0 < ({{bType}})5 < ({{cType}})10;

                static void Main()
                {
                    {{aType}} a = 0;
                    {{bType}} b = 5;
                    {{cType}} c = 10;
                    bool runtimeResult = a < b < c;
                    Console.Write($"const={ConstResult},runtime={runtimeResult}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "const=True,runtime=True")
            .VerifyDiagnostics();
    }

    [Theory]
    // Unsigned + signed widening permutations that produce a bindable chain and exercise
    // the unsigned -> signed ConstantValue widening path on the shared middle operand. The
    // grid below was chosen so every row's inner and outer links have applicable predefined
    // operators (no signed/unsigned ambiguity at binding). Every row folds to True, and the
    // runtime variable form must agree.
    [InlineData("uint", "uint", "uint")]
    [InlineData("uint", "uint", "long")]   // outer `uint -> long` widening on Y
    [InlineData("uint", "int", "long")]    // outer widening across signedness
    [InlineData("int", "uint", "long")]    // inner `int < uint -> long<long`; outer identity
    [InlineData("ulong", "ulong", "ulong")]
    [InlineData("uint", "ulong", "ulong")]
    public void UnsignedWideningPermutations_FoldMatchesRuntime(string aType, string bType, string cType)
    {
        var src = $$"""
            using System;

            class P
            {
                const bool ConstResult = ({{aType}})0 < ({{bType}})5 < ({{cType}})10;

                static void Main()
                {
                    {{aType}} a = 0;
                    {{bType}} b = 5;
                    {{cType}} c = 10;
                    bool runtimeResult = a < b < c;
                    Console.Write($"const={ConstResult},runtime={runtimeResult}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "const=True,runtime=True")
            .VerifyDiagnostics();
    }

    [Fact]
    public void UnsignedWideningToSigned_PreservesMagnitude()
    {
        // The regression trap if a fold bypassed FoldConstantConversion and relied on
        // ConstantValue's default Int64Value fallback: a `uint` constant's Int64Value
        // would sign-extend through Int32Value, producing the WRONG long value for any
        // uint with the high bit set. Pin that the fold of a chain whose shared middle
        // operand is `uint.MaxValue` correctly widens to 4_294_967_295L (not -1L).
        //
        // The chain `(uint)4_000_000_000 < uint.MaxValue < 5L` distinguishes the two
        // interpretations: inner `4_000_000_000u < 4_294_967_295u` is true regardless,
        // but the outer widening decides the chain's value.
        //   Correct widening:   4_294_967_295L < 5L => false.
        //   Sign-extend bug:    -1L           < 5L => true.
        // So the observed result directly tells us which one the fold used.
        var src = """
            using System;

            class P
            {
                const bool B = (uint)4_000_000_000 < uint.MaxValue < 5L;

                static void Main()
                {
                    // Runtime evaluation uses widening-correct IL.
                    uint a = 4_000_000_000;
                    uint b = uint.MaxValue;
                    bool runtime = a < b < 5L;
                    Console.Write($"const={B},runtime={runtime}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "const=False,runtime=False")
            .VerifyDiagnostics();
    }

    [Fact]
    public void UsedAsDefaultParameterValue()
    {
        // Default parameter values are compile-time constants. A chain that folds should
        // be accepted here; a chain that doesn't fold would be rejected with CS1736.
        var src = """
            using System;

            class P
            {
                static bool F(bool b = 0 < 5 < 10) => b;

                static void Main()
                {
                    Console.WriteLine(F());
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "True")
            .VerifyDiagnostics();
    }

    [Fact]
    public void UsedAsAttributeArgument()
    {
        // Named attribute-argument values must be constant expressions.
        var src = """
            using System;

            class MyAttr : Attribute
            {
                public bool Flag { get; set; }
            }

            [MyAttr(Flag = 0 < 5 < 10)]
            class P
            {
                static void Main()
                {
                    var attr = (MyAttr)Attribute.GetCustomAttribute(typeof(P), typeof(MyAttr));
                    Console.WriteLine(attr.Flag);
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "True")
            .VerifyDiagnostics();
    }

    [Fact]
    public void UsedAsCaseLabel()
    {
        // `case` labels are constant expressions whose type must match the switch-expression
        // type. A chain folded to bool should work as a case label for `switch (bool)`.
        var src = """
            using System;

            class P
            {
                static void Main()
                {
                    bool x = true;
                    string result = x switch
                    {
                        0 < 5 < 10 => "inRange",
                        _ => "otherwise",
                    };
                    Console.WriteLine(result);
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "inRange")
            .VerifyDiagnostics();
    }

    [Fact]
    public void ConstantFalseChain_TriggersUnreachableCodeWarning()
    {
        // When the folded value is `false`, flow analysis treats `if (chain)` as having an
        // unreachable body. This is the same treatment a hand-written `false` or
        // `false && cond` gets, and is how the constant flows into data/control-flow passes.
        var src = """
            class P
            {
                static int F()
                {
                    if (10 < 5 < 100) // inner false
                    {
                        return 1; // unreachable
                    }
                    return 0;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (7,13): warning CS0162: Unreachable code detected
                //             return 1; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(7, 13));
    }

    [Fact]
    public void ConstantTrueChain_FallThroughFlaggedUnreachable()
    {
        // The dual of the above: when the chain folds to true, flow analysis treats the
        // else / fall-through arm as unreachable, same as a hand-written `if (true)`.
        // The then-branch itself must NOT be flagged; only the arm after the unconditional
        // return is. This pins that the true-folded constant flows correctly.
        var src = """
            class P
            {
                static int F()
                {
                    if (1 < 2 < 3)
                    {
                        return 1;
                    }
                    return 0;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (9,9): warning CS0162: Unreachable code detected
                //         return 0;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(9, 9));
    }

    [Fact]
    public void NonConstantOperand_ChainIsNotConstant()
    {
        // Spec: every operand must be a constant expression for the chain to be one.
        // Matches classical `&&`: `false && nonConst` is not a constant either.
        var src = """
            class P
            {
                static bool F(int x)
                {
                    const bool b = 0 < x < 10; // x is non-constant -> chain is non-constant
                    return b;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (5,24): error CS0133: The expression being assigned to 'b' must be constant
                //         const bool b = 0 < x < 10;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "0 < x < 10").WithArguments("b").WithLocation(5, 24));
    }

    [Fact]
    public void NullableOperand_IsNotConstant()
    {
        // Nullable value types are not one of the constant-expression types permitted by
        // §11.20, so a classical comparison involving a nullable operand is already
        // non-constant in C#, e.g. `const bool b = (int?)5 < (int?)10;` reports CS0133.
        //
        // Chained comparisons inherit the same rule for free: FoldConstantConversion
        // returns null for lifted (ImplicitNullable) conversions, which is what the outer
        // link's LeftConversion is whenever Y is a value-type-and-nullable mix. So every
        // chain where any operand is nullable-typed must fail to fold, at EVERY position
        // (left / middle / right / all-nullable). Pin this by asserting CS0133 on all four
        // positional variants.
        var src = """
            class P
            {
                const bool Left   = (int?)0 <       5  <       10 ;
                const bool Middle =       0  < (int?)5  <       10 ;
                const bool Right  =       0  <       5  < (int?)10 ;
                const bool All    = (int?)0  < (int?)5  < (int?)10 ;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (3,25): error CS0133: The expression being assigned to 'P.Left' must be constant
                //     const bool Left   = (int?)0 <       5  <       10 ;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int?)0 <       5  <       10").WithArguments("P.Left").WithLocation(3, 25),
                // (4,31): error CS0133: The expression being assigned to 'P.Middle' must be constant
                //     const bool Middle =       0  < (int?)5  <       10 ;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "0  < (int?)5  <       10").WithArguments("P.Middle").WithLocation(4, 31),
                // (5,31): error CS0133: The expression being assigned to 'P.Right' must be constant
                //     const bool Right  =       0  <       5  < (int?)10 ;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "0  <       5  < (int?)10").WithArguments("P.Right").WithLocation(5, 31),
                // (6,25): error CS0133: The expression being assigned to 'P.All' must be constant
                //     const bool All    = (int?)0  < (int?)5  < (int?)10 ;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int?)0  < (int?)5  < (int?)10").WithArguments("P.All").WithLocation(6, 25));
    }

    [Fact]
    public void NullableNullOperand_IsNotConstant()
    {
        // A `null` literal promoted to int? is still a nullable-typed expression, so it
        // too falls outside §11.20's constant-expression types and the chain must not
        // fold, even though its runtime value is trivially False (any comparison against
        // null via a lifted relational operator yields false). Pin that we don't silently
        // fold to False based on "we know the answer at compile time": we don't, because
        // the operand is not a constant expression.
        var src = """
            class P
            {
                const bool B = (int?)0 < (int?)null < (int?)10;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (3,20): error CS0133: The expression being assigned to 'P.B' must be constant
                //     const bool B = (int?)0 < (int?)null < (int?)10;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int?)0 < (int?)null < (int?)10").WithArguments("P.B").WithLocation(3, 20));
    }

    [Fact]
    public void NullableChain_StillRunsAtRuntime()
    {
        // Dual of the above: the chain is still a perfectly valid runtime expression -
        // non-constant means it's not usable where a constant is required, NOT that it's
        // rejected outright. Pin that `int < int? < int` still compiles and executes
        // correctly at runtime, so the fold failure path is strictly about constant-ness
        // and doesn't leak into normal use.
        var src = """
            using System;

            class P
            {
                static void Main()
                {
                    int? a = 0;
                    int  b = 5;
                    int? c = 10;
                    Console.WriteLine(a < b < c);
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "True")
            .VerifyDiagnostics();
    }

    [Fact]
    public void NonConstantOuterRight_InnerFalse_StillNotConstant()
    {
        // Edge case: inner is constant-false, so the chain's runtime value is known to be
        // false regardless of the outer right. But spec §11.20 (as applied via the spec's
        // "Constant expressions" bullet) requires EVERY operand to be a constant. `false &&
        // nonConst` is not a constant in C# today, so `false-inner < nonConst` must not be
        // either.
        var src = """
            class P
            {
                static bool F(int x)
                {
                    const bool b = 10 > 5 > x; // inner `10 > 5` is true; but x is non-const
                    return b;
                }

                static bool G(int x)
                {
                    const bool b = 5 > 10 > x; // inner `5 > 10` is false; x still non-const
                    return b;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (5,24): error CS0133: The expression being assigned to 'b' must be constant
                //         const bool b = 10 > 5 > x;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "10 > 5 > x").WithArguments("b").WithLocation(5, 24),
                // (11,24): error CS0133: The expression being assigned to 'b' must be constant
                //         const bool b = 5 > 10 > x;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "5 > 10 > x").WithArguments("b").WithLocation(11, 24));
    }

    [Fact]
    public void UserDefinedOperator_NotConstant()
    {
        // User-defined relational operators never fold (§11.20 permits only the predefined
        // operators). So even if every operand is a constant-looking expression, a chain
        // that resolves to user-defined operators is not a constant expression.
        var src = """
            struct S
            {
                public static bool operator <(S a, S b) => true;
                public static bool operator >(S a, S b) => false;
                public static bool operator <=(S a, S b) => true;
                public static bool operator >=(S a, S b) => false;
                public static bool operator ==(S a, S b) => true;
                public static bool operator !=(S a, S b) => false;

                public override bool Equals(object o) => false;
                public override int GetHashCode() => 0;
            }

            class P
            {
                static void F()
                {
                    const bool b = new S() < new S() < new S();
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (18,24): error CS0133: The expression being assigned to 'b' must be constant
                //         const bool b = new S() < new S() < new S();
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "new S() < new S() < new S()").WithArguments("b").WithLocation(18, 24));
    }

    [Fact]
    public void NestedInConstantExpression()
    {
        // A folded chain should itself be usable in other constant-expression forms,
        // including combined with classical constants via `&&`, `||`, `?:`, etc.
        var src = """
            using System;

            class P
            {
                const bool AllTrue   = (0 < 5 < 10) && (1 <= 1 <= 1);
                const bool EitherWay = (1 > 2) || (0 < 5 < 10);
                const int  PickedN   = (0 < 5 < 10) ? 42 : -1;

                static void Main()
                {
                    Console.WriteLine($"{AllTrue},{EitherWay},{PickedN}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "True,True,42")
            .VerifyDiagnostics();
    }

    [Fact]
    public void AsymmetricWidening_FoldsWithOuterConversion()
    {
        // Pin that the outer LeftConversion is applied during folding. For
        // `(short)5 < 100 < 10000L`:
        //   - inner `(short)5 < 100`: short->int on left, identity on right; int<int true.
        //   - outer `100 < 10000L`: Y is `(int)5` (inner-link type is int, value 5), outer
        //     signature is long<long, LeftConversion is int->long. So the fold must widen
        //     Y's constant value to long (still 100) before comparing to 10000L.
        // If the outer LeftConversion is not applied during folding, the fold would either
        // fail outright (operand-type mismatch) or compare Y at the wrong type. The const
        // field below would then fail to compile.
        var src = """
            using System;

            class P
            {
                const bool B1 = (short)0 < 5 < 10L;
                const bool B2 = 0 < (short)5 < 10L;
                const bool B3 = 0L < (short)5 < 10;
                const bool B4 = (short)100 < 5 < 10L; // inner `100<5` false -> chain false

                static void Main()
                {
                    Console.WriteLine($"{B1},{B2},{B3},{B4}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "True,True,True,False")
            .VerifyDiagnostics();
    }

    [Theory]
    // Mixed signed/unsigned integral types at different positions. Classical C# accepts
    // these because every operand is a constant that fits the other side's type (via
    // binary numeric promotion to long, or the implicit constant-expression conversion
    // from a non-negative int to an unsigned type, etc.), and the chained fold has to
    // match that behavior. Picking constants that fit every reasonable promotion path so
    // the whole chain folds and prints True.
    //
    // For each row: the declared types determine the inner link's operands and the outer
    // link's right operand; the chain must fold to True.
    [InlineData("0 < 1 < 2u",                  "True")]      // int, int, uint - outer int<uint
    [InlineData("0u < 1 < 2u",                 "True")]      // uint, int-literal-fits, uint
    [InlineData("0 < 1u < 2",                  "True")]      // middle is uint; 0/2 int literals fit
    [InlineData("0u < 1 < 2L",                 "True")]      // outer uint<long (widening on Y)
    [InlineData("0 < 1L < 2ul",                "True")]      // outer long<ulong with NON-NEGATIVE long - Roslyn folds
    [InlineData("0 < 5 < 10ul",                "True")]      // outer int<ulong; literal 5 fits in ulong
    [InlineData("-1 < 5u < 10",                "True")]      // inner -1<5u: promotes to long; outer uint<int also long
    [InlineData("-1 < 0u < 10",                "True")]      // -1 < 0u: promotes to long, -1<0 true
    [InlineData("(byte)0 < (short)5 < 10L",    "True")]      // byte->int, short->int, outer int<long
    [InlineData("(byte)0 < (sbyte)5 < 10",     "True")]      // byte and sbyte both promote to int
    [InlineData("'a' < 100 < 200",             "True")]      // char -> int
    [InlineData("(sbyte)(-1) < (byte)0 < 10",  "True")]      // sbyte/byte/int all go through int
    [InlineData("int.MinValue < 0u < 10",      "True")]      // inner promotes int.MinValue + 0u to long
    [InlineData("0 < uint.MaxValue < long.MaxValue", "True")] // uint.MaxValue widens to long
    public void MixedSignedUnsigned_ChainsThatClassicalBindingAccepts_Fold(string chain, string expected)
    {
        // If a fold for any of these reverts back to the raw Int64Value accessor trick or
        // skips the outer LeftConversion, at least one of the above rows will print the
        // wrong value or fail to compile as a `const`.
        var src = $$"""
            using System;

            class P
            {
                const bool B = {{chain}};

                static void Main()
                {
                    Console.WriteLine(B);
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: expected)
            .VerifyDiagnostics();
    }

    [Fact]
    public void MixedSignedUnsigned_NegativeLongAgainstUlongOuter_ReportsCS9380()
    {
        // The classical `long < ulong` binding is ambiguous (CS0034) when neither operand
        // is a non-negative compile-time constant that fits the other side. A chain whose
        // outer link hits that shape -> the chain fallback cannot find a bool-returning
        // operator for the isolated `Y op B`, so the specific chained-relational
        // diagnostic CS9380 is reported (spec §11.11.13 rule 2(b) failure), NOT the
        // classical CS0034 that non-chained code would see.
        var src = """
            class P
            {
                const bool B = 0 < (long)(-5) < 10ul;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (3,35): error CS9380: Operator '<' cannot be applied to operands of type 'long' and 'ulong' as a chained relational comparison.
                //     const bool B = 0 < (long)(-5) < 10ul;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "long", "ulong").WithLocation(3, 35));
    }

    [Fact]
    public void MixedSignedUnsigned_InnerClassicalFailure_SurfacesClassicalError()
    {
        // Dual of the above: when the INNER link's classical binding is the thing that
        // fails (here, ambiguous `long < ulong`), chain fallback is not even entered -
        // it only triggers when the OUTER link fails. So the user sees the classical
        // CS0034 from the inner link, NOT CS9380. Pin this so the error taxonomy stays
        // clear: CS9380 is specifically about "outer link would not resolve", not "any
        // chained shape failed somewhere".
        var src = """
            class P
            {
                static bool F(long a, ulong b)
                {
                    // Inner `a < b` is long < ulong -> CS0034 before any chain considers it.
                    return a < b < 100;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (6,16): error CS0034: Operator '<' is ambiguous on operands of type 'long' and 'ulong'
                //         return a < b < 100;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "a < b").WithArguments("<", "long", "ulong").WithLocation(6, 16));
    }

    [Fact]
    public void MixedSignedUnsigned_ConstantFoldsButRuntimeFails_NonConstantLongUlongChain()
    {
        // Specifically contrasts const and runtime behavior: with CONSTANTS, `0 < 1L < 2ul`
        // folds - Roslyn's classical constant-folding accepts `1L < 2ul` because 1L is a
        // non-negative compile-time constant that fits in ulong, and the chain's outer
        // link inherits that constant acceptance. With VARIABLES of the same declared
        // types, there's no compile-time-constant rescue and the outer `long < ulong`
        // falls back to CS0034-equivalent rejection, which at the chain position surfaces
        // as CS9380.
        //
        // Pinning both sides so the contrast is explicit: the chained fold's flexibility
        // is bounded by what classical constant folding permits, and no wider.
        var src = """
            class P
            {
                const bool B = 0 < 1L < 2ul;  // const: folds
                static bool F(long b, ulong c) => 0 < b < c;  // runtime: outer fails
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (4,45): error CS9380: Operator '<' cannot be applied to operands of type 'long' and 'ulong' as a chained relational comparison.
                //     static bool F(long b, ulong c) => 0 < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "long", "ulong").WithLocation(4, 45));
    }

    [Fact]
    public void MixedSignedUnsigned_FoldResultMatchesRuntime()
    {
        // Cross-check: for every shape that folds AND runs, the folded value must equal
        // the runtime-evaluated value. This is the mixed-type analog of the signed /
        // unsigned side-by-side tests above, spanning shapes chosen to exercise different
        // conversion paths (explicitly excluding shapes like `long < ulong` where the
        // non-constant variant can't bind - those are covered by
        // MixedSignedUnsigned_ConstantFoldsButRuntimeFails_NonConstantLongUlongChain).
        var src = """
            using System;

            class P
            {
                const bool F1 = 0 < 1 < 2u;
                const bool F2 = -1 < 5u < 10;
                const bool F3 = (byte)0 < (short)5 < 10L;
                const bool F4 = 'a' < 100 < 200;
                const bool F5 = 0 < uint.MaxValue < long.MaxValue;

                static void Main()
                {
                    int    a1 = 0;   int    b1 = 1;              uint c1 = 2;
                    int    a2 = -1;  uint   b2 = 5;              int  c2 = 10;
                    byte   a3 = 0;   short  b3 = 5;              long c3 = 10;
                    char   a4 = 'a'; int    b4 = 100;            int  c4 = 200;
                    int    a5 = 0;   uint   b5 = uint.MaxValue;  long c5 = long.MaxValue;

                    bool r1 = a1 < b1 < c1;
                    bool r2 = a2 < b2 < c2;
                    bool r3 = a3 < b3 < c3;
                    bool r4 = a4 < b4 < c4;
                    bool r5 = a5 < b5 < c5;

                    Console.Write($"F1={F1},r1={r1} ");
                    Console.Write($"F2={F2},r2={r2} ");
                    Console.Write($"F3={F3},r3={r3} ");
                    Console.Write($"F4={F4},r4={r4} ");
                    Console.Write($"F5={F5},r5={r5}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput:
                "F1=True,r1=True F2=True,r2=True F3=True,r3=True F4=True,r4=True F5=True,r5=True")
            .VerifyDiagnostics();
    }
}
