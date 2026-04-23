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
/// spec §11.11.13). A chain folds when every operand folds, matching the spec's
/// <c>A &amp;&amp; (Y op B)</c> expansion.
/// </summary>
public sealed class ChainedRelationalComparisonConstantFoldingTests : CSharpTestBase
{
    [Theory]
    [InlineData("0", "5", "10", "<", "True")]
    [InlineData("0", "5", "2", "<", "False")]
    [InlineData("10", "5", "100", "<", "False")]
    [InlineData("0", "5", "10", "<=", "True")]
    [InlineData("10", "5", "0", ">=", "True")]
    [InlineData("0", "0", "0", "<=", "True")]
    [InlineData("0", "0", "0", "<", "False")]
    [InlineData("-1", "0", "1", "<", "True")]
    [InlineData("int.MinValue", "0", "int.MaxValue", "<", "True")]
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

    // `{short, int, long}` at the three operand positions (27 rows).
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
        // Four assertions: chained vs expanded at fold-time and at runtime.
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
    [InlineData("uint", "uint", "uint")]
    [InlineData("uint", "uint", "long")]
    [InlineData("uint", "int", "long")]
    [InlineData("int", "uint", "long")]
    [InlineData("ulong", "ulong", "ulong")]
    [InlineData("uint", "ulong", "ulong")]
    public void UnsignedWideningPermutations_FoldMatchesRuntime(string aType, string bType, string cType)
    {
        // Unsigned/signed widening counterpart to the signed grid above.
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
        // `uint.MaxValue` widens to 4_294_967_295L (not -1L) on the outer link.
        var src = """
            using System;

            class P
            {
                const bool B = (uint)4_000_000_000 < uint.MaxValue < 5L;

                static void Main()
                {
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
        var src = """
            class P
            {
                static int F()
                {
                    if (10 < 5 < 100)
                    {
                        return 1;
                    }
                    return 0;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (7,13): warning CS0162: Unreachable code detected
                //             return 1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(7, 13));
    }

    [Fact]
    public void ConstantTrueChain_FallThroughFlaggedUnreachable()
    {
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
        var src = """
            class P
            {
                static bool F(int x)
                {
                    const bool b = 0 < x < 10;
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
        // Nullable value types fall outside §11.20's constant-expression types, so any
        // nullable operand at any position makes the chain non-constant.
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
        // Even a trivially-false chain involving `(int?)null` is not a constant expression.
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
        // Non-constant doesn't mean rejected - the nullable chain still runs.
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
        // Matches `X && nonConst`: non-constant operand makes the whole chain non-constant.
        var src = """
            class P
            {
                static bool F(int x)
                {
                    const bool b = 10 > 5 > x;
                    return b;
                }

                static bool G(int x)
                {
                    const bool b = 5 > 10 > x;
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
        // User-defined relational operators never fold (§11.20 permits only predefined ones).
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
        // Folded chain composes with `&&` / `||` / `?:` in other constant contexts.
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
        // Outer link's LeftConversion on Y applies during folding.
        var src = """
            using System;

            class P
            {
                const bool B1 = (short)0 < 5 < 10L;
                const bool B2 = 0 < (short)5 < 10L;
                const bool B3 = 0L < (short)5 < 10;
                const bool B4 = (short)100 < 5 < 10L;

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
        // Outer `long < ulong` has no applicable operator -> chain reports CS9380 (not CS0034).
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
        // Chain fallback only runs for outer-link failures; inner-link failures surface classically.
        var src = """
            class P
            {
                static bool F(long a, ulong b)
                {
                    return a < b < 100;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (5,16): error CS0034: Operator '<' is ambiguous on operands of type 'long' and 'ulong'
                //         return a < b < 100;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "a < b").WithArguments("<", "long", "ulong").WithLocation(5, 16));
    }

    [Fact]
    public void MixedSignedUnsigned_ConstantFoldsButRuntimeFails_NonConstantLongUlongChain()
    {
        // Constant-folding accepts `1L < 2ul` (via the non-negative-constant rule); the
        // equivalent runtime chain does not.
        var src = """
            class P
            {
                const bool B = 0 < 1L < 2ul;
                static bool F(long b, ulong c) => 0 < b < c;
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
        // Fold-chained, fold-expanded, runtime-chained, runtime-expanded must all agree.
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

    [Theory]
    [InlineData("0f", "1f", "2f", "<", "True")]
    [InlineData("2f", "1f", "0f", ">", "True")]
    [InlineData("0f", "1f", "0.5f", "<", "False")]
    [InlineData("float.MinValue", "0f", "float.MaxValue", "<", "True")]
    [InlineData("0.0", "1.5", "3.14", "<", "True")]
    [InlineData("3.14", "1.5", "0.0", ">", "True")]
    [InlineData("double.MinValue", "0.0", "double.MaxValue", "<", "True")]
    public void FloatingPointConst_BasicAndBoundaries_FoldMatchesExpanded(string a, string b, string c, string op, string expected)
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
    // IEEE-754: any relational comparison with a NaN operand is false, so every
    // chain touching NaN folds to false regardless of the other operands or the
    // operator mix.
    [InlineData("double.NaN", "5.0", "10.0", "<")]
    [InlineData("0.0", "double.NaN", "10.0", "<")]
    [InlineData("0.0", "5.0", "double.NaN", "<")]
    [InlineData("double.NaN", "double.NaN", "double.NaN", "<")]
    [InlineData("0.0", "double.NaN", "10.0", "<=")]
    [InlineData("10.0", "double.NaN", "0.0", ">")]
    [InlineData("float.NaN", "5f", "10f", "<")]
    [InlineData("0f", "float.NaN", "10f", "<")]
    public void FloatingPointConst_NaNInAnyPosition_FoldsToFalse(string a, string b, string c, string op)
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
            expectedOutput: "c=False,e=False")
            .VerifyDiagnostics();
    }

    [Fact]
    public void DoubleConst_InfinitiesAndEpsilon_FoldMatchesExpanded()
    {
        // Pins the edge values: +/-infinity at the chain extremes, Epsilon as
        // the smallest-positive middle, and the chain involving both infinities
        // which folds false because +inf < -inf is false.
        var src = """
            using System;

            class P
            {
                const bool WithinInfinities  = double.NegativeInfinity < 0.0 < double.PositiveInfinity;
                const bool WithinInfinitiesE = (double.NegativeInfinity < 0.0) && (0.0 < double.PositiveInfinity);

                const bool BothInfinitiesOuter  = 0.0 < double.PositiveInfinity < double.NegativeInfinity;
                const bool BothInfinitiesOuterE = (0.0 < double.PositiveInfinity) && (double.PositiveInfinity < double.NegativeInfinity);

                const bool EpsilonMiddle  = 0.0 < double.Epsilon < 1.0;
                const bool EpsilonMiddleE = (0.0 < double.Epsilon) && (double.Epsilon < 1.0);

                static void Main()
                {
                    Console.Write($"wi={WithinInfinities}/{WithinInfinitiesE},bo={BothInfinitiesOuter}/{BothInfinitiesOuterE},ep={EpsilonMiddle}/{EpsilonMiddleE}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "wi=True/True,bo=False/False,ep=True/True")
            .VerifyDiagnostics();
    }

    [Fact]
    public void DoubleConst_NegativeZero_FoldMatchesExpanded()
    {
        // IEEE-754: -0.0 == 0.0, so -0.0 < 0.0 is false and any chain whose
        // inner link is -0.0 < 0.0 short-circuits to false; conversely
        // 0.0 > -0.0 is also false.
        var src = """
            using System;

            class P
            {
                const bool NegZeroInnerLT  = -0.0 < 0.0 < 1.0;
                const bool NegZeroInnerLTe = (-0.0 < 0.0) && (0.0 < 1.0);

                const bool NegZeroInnerLE  = -0.0 <= 0.0 <= 1.0;
                const bool NegZeroInnerLEe = (-0.0 <= 0.0) && (0.0 <= 1.0);

                const bool NegZeroMixed  = 1.0 > 0.0 > -0.0;
                const bool NegZeroMixede = (1.0 > 0.0) && (0.0 > -0.0);

                static void Main()
                {
                    Console.Write($"lt={NegZeroInnerLT}/{NegZeroInnerLTe},le={NegZeroInnerLE}/{NegZeroInnerLEe},mx={NegZeroMixed}/{NegZeroMixede}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "lt=False/False,le=True/True,mx=False/False")
            .VerifyDiagnostics();
    }

    [Theory]
    [InlineData("0m", "5m", "10m", "<", "True")]
    [InlineData("0m", "5m", "2m", "<", "False")]
    [InlineData("10m", "5m", "0m", ">", "True")]
    [InlineData("decimal.MinValue", "0m", "decimal.MaxValue", "<", "True")]
    [InlineData("decimal.MaxValue", "0m", "decimal.MinValue", ">", "True")]
    [InlineData("-0m", "0m", "1m", "<", "False")]
    [InlineData("-0m", "0m", "1m", "<=", "True")]
    public void DecimalConst_BasicAndBoundaries_FoldMatchesExpanded(string a, string b, string c, string op, string expected)
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

    [Fact]
    public void EnumConst_BasicAndMixedDirection_FoldMatchesExpanded()
    {
        var src = """
            using System;

            enum E { A, B, C, D }

            class P
            {
                const bool Up    = E.A < E.B < E.C < E.D;
                const bool UpE   = (E.A < E.B) && (E.B < E.C) && (E.C < E.D);
                const bool Mixed  = E.A < E.B > E.A;
                const bool MixedE = (E.A < E.B) && (E.B > E.A);
                const bool Down  = E.D > E.C > E.B;
                const bool DownE = (E.D > E.C) && (E.C > E.B);

                static void Main()
                {
                    Console.Write($"up={Up}/{UpE},mx={Mixed}/{MixedE},dn={Down}/{DownE}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "up=True/True,mx=True/True,dn=True/True")
            .VerifyDiagnostics();
    }

    [Theory]
    [InlineData("byte")]
    [InlineData("sbyte")]
    [InlineData("short")]
    [InlineData("ushort")]
    [InlineData("uint")]
    [InlineData("long")]
    [InlineData("ulong")]
    public void EnumConst_MixedUnderlyingType_FoldMatchesExpanded(string underlying)
    {
        var src = $$"""
            using System;

            enum E : {{underlying}} { A, B, C }

            class P
            {
                const bool Chained  = E.A < E.B < E.C;
                const bool Expanded = (E.A < E.B) && (E.B < E.C);

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
}
