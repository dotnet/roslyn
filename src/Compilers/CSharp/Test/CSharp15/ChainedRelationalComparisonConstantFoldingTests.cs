// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    // Simple `a op b op c` chains where a single relational operator appears at both
    // links. Each row supplies the three operand expressions (as strings), the operator,
    // and the expected bool value. The chained form `a op b op c` and the hand-written
    // expansion `(a op b) && (b op c)` are folded as separate const initializers in the
    // same compilation, and both must agree. The test body builds both forms from the
    // parameters so there's a single source of truth per row.
    //
    // Basic int chains.
    [InlineData("0", "5", "10", "<", "True")]
    [InlineData("0", "5", "2", "<", "False")]    // inner true, outer false
    [InlineData("10", "5", "100", "<", "False")] // inner false
    // Other relational operators.
    [InlineData("0", "5", "10", "<=", "True")]
    [InlineData("10", "5", "0", ">=", "True")]
    // Boundary: equal operands.
    [InlineData("0", "0", "0", "<=", "True")]
    [InlineData("0", "0", "0", "<", "False")]
    // Negative and overflow-adjacent values.
    [InlineData("-1", "0", "1", "<", "True")]
    [InlineData("int.MinValue", "0", "int.MaxValue", "<", "True")]
    // Asymmetric conversions (exercises the outer LeftConversion at fold time).
    [InlineData("0", "5", "10L", "<", "True")]
    [InlineData("0L", "5", "10", "<", "True")]
    [InlineData("(short)0", "5", "10L", "<", "True")]
    [InlineData("0", "(short)5", "10L", "<", "True")]
    public void ThreeOperandChain_ChainedAndExpandedFoldAgree(string a, string b, string c, string op, string expected)
    {
        var src = $$"""
            using System;

            class P
            {
                const bool Chained  = {{a}} {{op}} {{b}} {{op}} {{c}};
                const bool Expanded = ({{a}} {{op}} {{b}}) && ({{b}} {{op}} {{c}});

                static void Main()
                {
                    Console.Write($"c={Chained},e={Expanded}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: $"c={expected},e={expected}")
            .VerifyDiagnostics();
    }

    [Theory]
    // Chain shapes that don't fit the simple `a op b op c` template: mixed relational
    // operators at different positions, and n-ary chains of length 4+. Same side-by-side
    // chained-vs-expanded assertion, but the chain and its expansion are supplied as
    // full expressions rather than synthesized from parts.
    [InlineData("1 < 2 > 0", "(1 < 2) && (2 > 0)", "True")]
    [InlineData("1 < 2 < 3 < 4", "(1 < 2) && (2 < 3) && (3 < 4)", "True")]
    [InlineData("1 < 2 < 3 < 4 < 5", "(1 < 2) && (2 < 3) && (3 < 4) && (4 < 5)", "True")]
    [InlineData("1 < 2 < 3 < 2", "(1 < 2) && (2 < 3) && (3 < 2)", "False")]
    public void MixedOperatorsOrNAryChain_ChainedAndExpandedFoldAgree(string chain, string expanded, string expected)
    {
        var src = $$"""
            using System;

            class P
            {
                const bool Chained  = {{chain}};
                const bool Expanded = {{expanded}};

                static void Main()
                {
                    Console.Write($"c={Chained},e={Expanded}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: $"c={expected},e={expected}")
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
        // Four assertions per row, side-by-side:
        //   constChained    - chained form as a `const bool` initializer (fails CS0133 if
        //                     the chained fold returns null).
        //   constExpanded   - hand-written `(a < b) && (b < c)` form as a `const bool`
        //                     initializer, folded by classical FoldBinaryOperator.
        //   runtimeChained  - chained form on runtime variables.
        //   runtimeExpanded - hand-written form on runtime variables.
        //
        // All four must equal True. Any divergence between chained and expanded (at
        // either fold-time or runtime) means the chain's semantics drifted from the
        // spec's `A && (Y op B)` equivalence; any divergence between fold-time and
        // runtime means the fold computed a value different from what the emitted IL
        // would produce.
        var src = $$"""
            using System;

            class P
            {
                const bool ConstChained  = ({{aType}})0 < ({{bType}})5 < ({{cType}})10;
                const bool ConstExpanded = (({{aType}})0 < ({{bType}})5) && (({{bType}})5 < ({{cType}})10);

                static void Main()
                {
                    {{aType}} a = 0;
                    {{bType}} b = 5;
                    {{cType}} c = 10;
                    bool runtimeChained  = a < b < c;
                    bool runtimeExpanded = (a < b) && (b < c);
                    Console.Write($"cc={ConstChained},ce={ConstExpanded},rc={runtimeChained},re={runtimeExpanded}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "cc=True,ce=True,rc=True,re=True")
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
        // Same four-way side-by-side assertion as the signed grid: chained vs
        // expanded at fold-time, chained vs expanded at runtime. The two folds
        // must agree for every unsigned-widening permutation, matching what the
        // hand-written expansion produces when each `<` runs its own
        // FoldBinaryOperator call with the right operand types.
        var src = $$"""
            using System;

            class P
            {
                const bool ConstChained  = ({{aType}})0 < ({{bType}})5 < ({{cType}})10;
                const bool ConstExpanded = (({{aType}})0 < ({{bType}})5) && (({{bType}})5 < ({{cType}})10);

                static void Main()
                {
                    {{aType}} a = 0;
                    {{bType}} b = 5;
                    {{cType}} c = 10;
                    bool runtimeChained  = a < b < c;
                    bool runtimeExpanded = (a < b) && (b < c);
                    Console.Write($"cc={ConstChained},ce={ConstExpanded},rc={runtimeChained},re={runtimeExpanded}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "cc=True,ce=True,rc=True,re=True")
            .VerifyDiagnostics();
    }

    [Fact]
    public void UnsignedWideningToSigned_PreservesMagnitude()
    {
        // Widening an unsigned constant to a signed wider type must preserve its
        // magnitude (i.e., go through FoldConstantConversion), not sign-extend
        // through the narrower signed representation. Pin that the fold of a chain
        // whose shared middle operand is `uint.MaxValue` correctly widens to
        // 4_294_967_295L (not -1L).
        //
        // The chain `(uint)4_000_000_000 < uint.MaxValue < 5L` distinguishes the two
        // interpretations: inner `4_000_000_000u < 4_294_967_295u` is true regardless,
        // but the outer widening decides the chain's value.
        //   Magnitude-preserving:  4_294_967_295L < 5L => false.
        //   Sign-extending:        -1L            < 5L => true.
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
        // A `null` literal promoted to int? is still a nullable-typed expression, so
        // it too falls outside §11.20's constant-expression types and the chain must
        // not fold, even though its runtime value is trivially false (any comparison
        // against null via a lifted relational operator yields false). Pin that we do
        // not fold based on "we know the answer at compile time": the operand is not a
        // constant expression.
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
    // the whole chain folds to True.
    //
    // Each row carries the chained form and the equivalent hand-written expansion
    // `(a op b) && (b op c)`. Both forms are placed into `const bool` initializers and
    // printed at runtime, so a fold drift would show up as a mismatched output.
    [InlineData("0 < 1 < 2u", "(0 < 1) && (1 < 2u)")]
    [InlineData("0u < 1 < 2u", "(0u < 1) && (1 < 2u)")]
    [InlineData("0 < 1u < 2", "(0 < 1u) && (1u < 2)")]
    [InlineData("0u < 1 < 2L", "(0u < 1) && (1 < 2L)")]
    [InlineData("0 < 1L < 2ul", "(0 < 1L) && (1L < 2ul)")]
    [InlineData("0 < 5 < 10ul", "(0 < 5) && (5 < 10ul)")]
    [InlineData("-1 < 5u < 10", "(-1 < 5u) && (5u < 10)")]
    [InlineData("-1 < 0u < 10", "(-1 < 0u) && (0u < 10)")]
    [InlineData("(byte)0 < (short)5 < 10L", "((byte)0 < (short)5) && ((short)5 < 10L)")]
    [InlineData("(byte)0 < (sbyte)5 < 10", "((byte)0 < (sbyte)5) && ((sbyte)5 < 10)")]
    [InlineData("'a' < 100 < 200", "('a' < 100) && (100 < 200)")]
    [InlineData("(sbyte)(-1) < (byte)0 < 10", "((sbyte)(-1) < (byte)0) && ((byte)0 < 10)")]
    [InlineData("int.MinValue < 0u < 10", "(int.MinValue < 0u) && (0u < 10)")]
    [InlineData("0 < uint.MaxValue < long.MaxValue", "(0 < uint.MaxValue) && (uint.MaxValue < long.MaxValue)")]
    public void MixedSignedUnsigned_ChainsThatClassicalBindingAccepts_Fold(string chain, string expanded)
    {
        // Pin that the chained fold and the classical hand-written expansion produce the
        // same constant value (True for every row - every operand is chosen to satisfy
        // a < b < c). If a fold reverts to the raw Int64Value accessor trick or skips
        // the outer LeftConversion, at least one row will show `c != e`.
        var src = $$"""
            using System;

            class P
            {
                const bool Chained  = {{chain}};
                const bool Expanded = {{expanded}};

                static void Main()
                {
                    Console.Write($"c={Chained},e={Expanded}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "c=True,e=True")
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
        // Cross-check across fold / runtime and chained / expanded: for every shape that
        // folds AND runs, all four variants must agree. This is the mixed-type analog of
        // the signed / unsigned side-by-side tests above, spanning shapes chosen to
        // exercise different conversion paths (explicitly excluding shapes like
        // `long < ulong` where the non-constant variant can't bind - those are covered
        // by MixedSignedUnsigned_ConstantFoldsButRuntimeFails_NonConstantLongUlongChain).
        var src = """
            using System;

            class P
            {
                const bool Fc1 = 0 < 1 < 2u;
                const bool Fe1 = (0 < 1) && (1 < 2u);
                const bool Fc2 = -1 < 5u < 10;
                const bool Fe2 = (-1 < 5u) && (5u < 10);
                const bool Fc3 = (byte)0 < (short)5 < 10L;
                const bool Fe3 = ((byte)0 < (short)5) && ((short)5 < 10L);
                const bool Fc4 = 'a' < 100 < 200;
                const bool Fe4 = ('a' < 100) && (100 < 200);
                const bool Fc5 = 0 < uint.MaxValue < long.MaxValue;
                const bool Fe5 = (0 < uint.MaxValue) && (uint.MaxValue < long.MaxValue);

                static void Main()
                {
                    int    a1 = 0;   int    b1 = 1;              uint c1 = 2;
                    int    a2 = -1;  uint   b2 = 5;              int  c2 = 10;
                    byte   a3 = 0;   short  b3 = 5;              long c3 = 10;
                    char   a4 = 'a'; int    b4 = 100;            int  c4 = 200;
                    int    a5 = 0;   uint   b5 = uint.MaxValue;  long c5 = long.MaxValue;

                    bool rc1 = a1 < b1 < c1; bool re1 = (a1 < b1) && (b1 < c1);
                    bool rc2 = a2 < b2 < c2; bool re2 = (a2 < b2) && (b2 < c2);
                    bool rc3 = a3 < b3 < c3; bool re3 = (a3 < b3) && (b3 < c3);
                    bool rc4 = a4 < b4 < c4; bool re4 = (a4 < b4) && (b4 < c4);
                    bool rc5 = a5 < b5 < c5; bool re5 = (a5 < b5) && (b5 < c5);

                    Console.Write($"1:Fc={Fc1},Fe={Fe1},rc={rc1},re={re1} ");
                    Console.Write($"2:Fc={Fc2},Fe={Fe2},rc={rc2},re={re2} ");
                    Console.Write($"3:Fc={Fc3},Fe={Fe3},rc={rc3},re={re3} ");
                    Console.Write($"4:Fc={Fc4},Fe={Fe4},rc={rc4},re={re4} ");
                    Console.Write($"5:Fc={Fc5},Fe={Fe5},rc={rc5},re={re5}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput:
                "1:Fc=True,Fe=True,rc=True,re=True " +
                "2:Fc=True,Fe=True,rc=True,re=True " +
                "3:Fc=True,Fe=True,rc=True,re=True " +
                "4:Fc=True,Fe=True,rc=True,re=True " +
                "5:Fc=True,Fe=True,rc=True,re=True")
            .VerifyDiagnostics();
    }
}
