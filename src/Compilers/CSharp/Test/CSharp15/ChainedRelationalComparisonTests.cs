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
/// Tests for "chained relational comparison" (C# preview feature; spec §11.11.13).
/// See proposals/chained-relational-comparison.md in dotnet/csharplang.
/// </summary>
public sealed class ChainedRelationalComparisonTests : CSharpTestBase
{
    #region Basic binding and runtime behaviour

    [Fact]
    public void Chain_Int_BasicHappyPath()
    {
        var src = """
            using System;

            class P
            {
                static void Main()
                {
                    Console.WriteLine($"{0 <= 5 < 10},{0 <= 5 < 2},{10 <= 5 < 100},{1 < 2 < 3},{3 < 2 < 1}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "True,False,False,True,False")
            .VerifyDiagnostics();
    }

    // Every permutation of `{short, short?, int, int?, long, long?}` at the three
    // operand positions (216 rows).
    public static IEnumerable<object[]> AllShortIntLongNullablePermutations()
    {
        var types = new[] { "short", "short?", "int", "int?", "long", "long?" };
        foreach (var a in types)
            foreach (var b in types)
                foreach (var c in types)
                    yield return new object[] { a, b, c };
    }

    [Theory]
    [MemberData(nameof(AllShortIntLongNullablePermutations))]
    public void Chain_AllShortIntLongNullablePermutations(string aType, string bType, string cType)
    {
        // a=0, b=5, c=10 satisfy `a < b < c` for every row in the grid.
        var src = $$"""
            using System;

            class P
            {
                static void Main()
                {
                    {{aType}} a = 0;
                    {{bType}} b = 5;
                    {{cType}} c = 10;
                    Console.WriteLine(a < b < c);
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "True")
            .VerifyDiagnostics();
    }

    [Theory]
    [InlineData("null", "5", "10", "False")]
    [InlineData("0", "null", "10", "False")]
    [InlineData("0", "5", "null", "False")]
    [InlineData("null", "null", "10", "False")]
    [InlineData("null", "null", "null", "False")]
    [InlineData("0", "5", "10", "True")]
    public void Chain_AllNullableOperands_NullInAnyPositionShortCircuitsToFalse(string aVal, string bVal, string cVal, string expected)
    {
        var src = $$"""
            using System;

            class P
            {
                static void Main()
                {
                    int? a = {{aVal}};
                    int? b = {{bVal}};
                    int? c = {{cVal}};
                    Console.WriteLine(a < b < c);
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: expected)
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_Nullable_LiftedOperatorsReturnBool()
    {
        // Lifted relational operators return `bool`, not `bool?` (spec §11.4.8).
        var src = """
            using System;

            class P
            {
                static void Main()
                {
                    int? a = 0, b = 5, c = 10, n = null;
                    Console.WriteLine($"{a < b < c},{a < n < c},{n < b < c},{a < b < n}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "True,False,False,False")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_MixedDirection_InterpretedAsAnd()
    {
        var src = """
            using System;

            class P
            {
                static void Main()
                {
                    Console.WriteLine($"{1 < 5 > 2},{1 < 5 > 10},{5 < 1 > 10}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "True,False,False")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_EmbeddedInStringConcat_LowersExactlyOnce()
    {
        // Chain nested inside a non-chained binary (`+`).
        var src = """
            using System;

            class P
            {
                static void Main()
                {
                    Console.WriteLine("r=" + (0 <= 5 < 10));
                    Console.WriteLine("r=" + (0 <= 5 < 2));
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: """
                r=True
                r=False
                """)
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_ResultConvertedToNullableBool_CompilesAndRuns()
    {
        // Chain's compile-time type is `bool`; implicit conversion to `bool?` applies.
        var src = """
            using System;

            class P
            {
                static void Main()
                {
                    bool? a = 0 <= 5 < 10;
                    bool? b = 0 <= 5 < 2;
                    Console.WriteLine($"a={a}, b={b}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "a=True, b=False")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_WithOperandShapes_NullCoalescingAndConditionalAccess_Works()
    {
        // Operands that are themselves lowered expressions (`??`, `?.`).
        var src = """
            #nullable enable
            using System;

            class P
            {
                static int Fallback() { Console.Write("fb "); return 5; }

                static void Main()
                {
                    int? maybe = null;
                    int[] arr = { 1, 2, 3 };
                    int[]? maybeArr = null;

                    Console.WriteLine(0 < (maybe ?? Fallback()) < 10);   // "fb True"
                    Console.WriteLine(0 < maybeArr?.Length < 10);        // "False"
                    Console.WriteLine(0 < arr.Length < maybeArr?.Length); // "False"
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: """
                fb True
                False
                False
                """)
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_WithOperandShapes_ConditionalExpression_Works()
    {
        // Ternary middle operand. `A()` returns in-range; `B()` returns out-of-range.
        var src = """
            using System;

            class P
            {
                static int A() { Console.Write("A "); return 5; }
                static int B() { Console.Write("B "); return 200; }

                static void Main()
                {
                    bool cond = true;
                    Console.WriteLine(0 < (cond ? A() : B()) < 100);
                    cond = false;
                    Console.WriteLine(0 < (cond ? A() : B()) < 100);
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: """
                A True
                B False
                """)
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_UsedAsGenericArgumentOfFuncTakingBool_Works()
    {
        var src = """
            using System;

            class P
            {
                static void Accept(bool b) => Console.WriteLine($"b={b}");
                static void AcceptFunc(Func<bool> f) => Console.WriteLine($"f()={f()}");

                static void Main()
                {
                    Accept(0 <= 5 < 10);
                    AcceptFunc(() => 0 <= 5 < 10);
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: """
                b=True
                f()=True
                """)
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_FourAndFiveOperands_WorkNAry()
    {
        var src = """
            using System;

            class P
            {
                static void Main()
                {
                    Console.WriteLine(1 < 2 < 3 < 4);
                    Console.WriteLine(1 < 2 < 3 < 2);
                    Console.WriteLine(1 < 2 < 3 < 4 < 5);
                    Console.WriteLine(1 < 2 < 3 < 4 < 3);
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: """
                True
                False
                True
                False
                """)
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_MiddleOperandEvaluatedOnce()
    {
        // The shared middle is evaluated exactly once on every path.
        var src = """
            using System;

            class P
            {
                static int calls;
                static int M() { calls++; return 5; }

                static void Report(bool r) => Console.WriteLine($"r={r}, calls={calls}");

                static void Main()
                {
                    calls = 0; Report(0 <= M() <= 10);
                    calls = 0; Report(0 <= M() <= 2);
                    calls = 0; Report(100 <= M() <= 10);
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: """
                r=True, calls=1
                r=False, calls=1
                r=False, calls=1
                """)
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_MatchesHandWrittenAndChain_SingleEvaluationPinned()
    {
        // Chain calls `M()` once; hand-written `&&` form calls it twice.
        var src = """
            using System;

            class P
            {
                static int calls;
                static int M() { calls++; return 5; }

                static void Main()
                {
                    calls = 0; bool chained = 0 <= M() <= 10;
                    int chainedCalls = calls;

                    calls = 0; bool andForm = 0 <= M() && M() <= 10;
                    int andFormCalls = calls;

                    Console.WriteLine($"chained={chained} calls={chainedCalls}");
                    Console.WriteLine($"&&-form={andForm} calls={andFormCalls}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: """
                chained=True calls=1
                &&-form=True calls=2
                """)
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_SameTypeEquivalence_MatchesHandWrittenAndChain()
    {
        // Same-type counterpart to AsymmetricConversion_EquivalentToHandWrittenShortCircuit_AllCombinations.
        var src = """
            using System;

            class P
            {
                static void Main()
                {
                    int total = 0, mismatched = 0;
                    for (int a = 0; a < 4; a++)
                    for (int b = 0; b < 4; b++)
                    for (int c = 0; c < 4; c++)
                    {
                        bool chained = a < b < c;
                        bool handWritten = a < b && b < c;
                        total++;
                        if (chained != handWritten) mismatched++;
                    }
                    Console.WriteLine($"total={total}, mismatched={mismatched}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "total=64, mismatched=0")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_AllOperandsAreMethodCalls_EachEvaluatedOnceInLeftToRightOrder()
    {
        var src = """
            using System;
            using System.Collections.Generic;

            class P
            {
                static List<string> log;
                static int A() { log.Add("A"); return 1; }
                static int B() { log.Add("B"); return 2; }
                static int C() { log.Add("C"); return 3; }

                static void Report(string label, bool r)
                {
                    Console.WriteLine($"{label}: result={r}, log=[{string.Join(",", log)}]");
                }

                static void Main()
                {
                    log = new(); Report("1<2<3", A() < B() < C());
                    log = new(); Report("3<2<1", C() < B() < A());
                    log = new(); Report("1<3<2", A() < C() < B());
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: """
                1<2<3: result=True, log=[A,B,C]
                3<2<1: result=False, log=[C,B]
                1<3<2: result=False, log=[A,C,B]
                """)
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_ShortCircuit_DoesNotEvaluateArgumentsNestedInsideRightOperand()
    {
        // When inner is false, neither `C(...)` nor its argument `D()` runs.
        var src = """
            using System;

            class P
            {
                static int aCalls, bCalls, cCalls, dCalls;
                static int A() { aCalls++; return 100; }
                static int B() { bCalls++; return 1; }
                static int C(int x) { cCalls++; return x; }
                static int D() { dCalls++; return 5; }

                static void Main()
                {
                    aCalls = bCalls = cCalls = dCalls = 0;
                    bool r = A() < B() < C(D());
                    Console.WriteLine($"r={r}, a={aCalls}, b={bCalls}, c={cCalls}, d={dCalls}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=False, a=1, b=1, c=0, d=0")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_NoShortCircuit_ArgumentsInsideRightOperandRunInSourceOrder()
    {
        // When inner is true, the outer-right's sub-expressions run in source order.
        var src = """
            using System;
            using System.Collections.Generic;

            class P
            {
                static List<string> log;
                static int A() { log.Add("A"); return 1; }
                static int B() { log.Add("B"); return 2; }
                static int C(int x) { log.Add("C"); return x; }
                static int D() { log.Add("D"); return 5; }

                static void Main()
                {
                    log = new();
                    bool r = A() < B() < C(D());
                    Console.WriteLine($"r={r}, log=[{string.Join(",", log)}]");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=True, log=[A,B,D,C]")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_ShortCircuit_InMiddleOfFourOperand_SkipsAllRemainingArgumentsToo()
    {
        // Middle link false -> outer link's call and its argument never run.
        var src = """
            using System;

            class P
            {
                static int aCalls, bCalls, cCalls, dCalls, eCalls;
                static int A() { aCalls++; return 1; }
                static int B() { bCalls++; return 10; }
                static int C() { cCalls++; return 5; }
                static int D(int x) { dCalls++; return x; }
                static int E() { eCalls++; return 100; }

                static void Main()
                {
                    aCalls = bCalls = cCalls = dCalls = eCalls = 0;
                    bool r = A() < B() < C() < D(E());
                    Console.WriteLine($"r={r}, a={aCalls}, b={bCalls}, c={cCalls}, d={dCalls}, e={eCalls}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=False, a=1, b=1, c=1, d=0, e=0")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_FourMethodCalls_EachSharedMiddleEvaluatedOnce()
    {
        // Four-operand chain: each shared middle (B, C) is evaluated at most once.
        var src = """
            using System;
            using System.Collections.Generic;

            class P
            {
                static List<string> log;
                static int A() { log.Add("A"); return 1; }
                static int B() { log.Add("B"); return 2; }
                static int C() { log.Add("C"); return 3; }
                static int D() { log.Add("D"); return 4; }

                static void Report(string label, bool r)
                {
                    Console.WriteLine($"{label}: result={r}, log=[{string.Join(",", log)}]");
                }

                static void Main()
                {
                    log = new(); Report("1<2<3<4", A() < B() < C() < D());
                    log = new(); Report("2<1<3<4 (inner false)", B() < A() < C() < D());
                    log = new(); Report("1<3<2<4 (middle false)", A() < C() < B() < D());
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: """
                1<2<3<4: result=True, log=[A,B,C,D]
                2<1<3<4 (inner false): result=False, log=[B,A]
                1<3<2<4 (middle false): result=False, log=[A,C,B]
                """)
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_UserDefinedBoolReturningOperator_Works()
    {
        var src = """
            using System;

            struct T
            {
                public int V;
                public T(int v) => V = v;
                public static bool operator <(T a, T b) => a.V < b.V;
                public static bool operator >(T a, T b) => a.V > b.V;
                public static bool operator <=(T a, T b) => a.V <= b.V;
                public static bool operator >=(T a, T b) => a.V >= b.V;
                public static bool operator ==(T a, T b) => a.V == b.V;
                public static bool operator !=(T a, T b) => a.V != b.V;
                public override bool Equals(object o) => o is T t && t.V == V;
                public override int GetHashCode() => V;
            }

            class P
            {
                static void Main()
                {
                    Console.WriteLine(new T(1) < new T(2) < new T(3));
                    Console.WriteLine(new T(1) < new T(5) < new T(3));
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: """
                True
                False
                """)
            .VerifyDiagnostics();
    }

    #endregion

    #region Back-compat and error cases

    [Fact]
    public void ParensBlockChainShape_ReportsOrdinaryCS0019()
    {
        // Parens break the chain shape: `(a < b) < c` falls back to ordinary CS0019.
        var src = """
            class P
            {
                static bool F(int a, int b, int c) => (a < b) < c;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (3,43): error CS0019: Operator '<' cannot be applied to operands of type 'bool' and 'int'
                //     static bool F(int a, int b, int c) => (a < b) < c;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(a < b) < c").WithArguments("<", "bool", "int").WithLocation(3, 43));
    }

    [Fact]
    public void EqualityOperators_DoNotChain()
    {
        // The chain rule applies only to `<`, `<=`, `>`, `>=`.
        var src = """
            class P
            {
                static bool F(int a, int b, int c) => a == b == c;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (3,43): error CS0019: Operator '==' cannot be applied to operands of type 'bool' and 'int'
                //     static bool F(int a, int b, int c) => a == b == c;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "a == b == c").WithArguments("==", "bool", "int").WithLocation(3, 43));
    }

    [Fact]
    public void ChainInsideExpressionTree_ReportsCS9389()
    {
        var src = """
            using System;
            using System.Linq.Expressions;

            class P
            {
                static Expression<Func<int, int, int, bool>> F = (a, b, c) => a < b < c;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (6,67): error CS9389: An expression tree may not contain a chained relational comparison.
                //     static Expression<Func<int, int, int, bool>> F = (a, b, c) => a < b < c;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsChainedRelationalComparison, "a < b < c").WithLocation(6, 67));
    }

    [Fact]
    public void Dynamic_IsBoundDynamically_NoChainFallback()
    {
        // `dynamic` operands bypass the chain rule entirely. The runtime binder then
        // throws on `bool < int` (the outer link's Left is the inner bool result).
        var src = """
            using System;
            using Microsoft.CSharp.RuntimeBinder;

            class P
            {
                static void Main()
                {
                    dynamic a = 0, b = 5, c = 10;
                    try
                    {
                        bool r = a < b < c;
                        Console.WriteLine("no-throw");
                    }
                    catch (RuntimeBinderException)
                    {
                        Console.WriteLine("threw");
                    }
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            targetFramework: TargetFramework.StandardAndCSharp,
            expectedOutput: "threw")
            .VerifyDiagnostics();
    }

    #endregion

    #region Language-version gating

    [Fact]
    public void ChainRequiresPreviewLanguageVersion()
    {
        var src = """
            class P
            {
                static bool F(int a, int b, int c) => a < b < c;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.Regular14)
            .VerifyDiagnostics(
                // (3,43): error CS8652: The feature 'chained relational comparison' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static bool F(int a, int b, int c) => a < b < c;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "a < b < c").WithArguments("chained relational comparison").WithLocation(3, 43));
    }

    #endregion

    #region Asymmetric conversions of a shared middle operand
    //
    // Option A (permissive) per the spec's open question: when a shared middle Y
    // needs different conversions at its two adjacent links, Y is evaluated once
    // and each link applies its own conversion at its point of use.

    public static IEnumerable<object[]> AllShortIntLongPermutations()
    {
        var types = new[] { "short", "int", "long" };
        foreach (var a in types)
            foreach (var b in types)
                foreach (var c in types)
                    yield return new object[] { a, b, c };
    }

    [Theory]
    [MemberData(nameof(AllShortIntLongPermutations))]
    public void AsymmetricConversion_AllPermutationsOfShortIntLong(string aType, string bType, string cType)
    {
        // a=1, b=5, c=10 fits every row; per-call counters pin single-evaluation.
        var src = $$"""
            using System;

            class P
            {
                static int aCalls, bCalls, cCalls;
                static {{aType}} A() { aCalls++; return 1; }
                static {{bType}} B() { bCalls++; return 5; }
                static {{cType}} C() { cCalls++; return 10; }

                static void Main()
                {
                    aCalls = bCalls = cCalls = 0;
                    bool r = A() < B() < C();
                    Console.WriteLine($"r={r}, a={aCalls}, b={bCalls}, c={cCalls}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=True, a=1, b=1, c=1")
            .VerifyDiagnostics();
    }

    [Fact]
    public void AsymmetricConversion_WideningOnOuterLink_IntIntLong()
    {
        // Inner resolves as `int<int`, outer as `long<long` widening b on load.
        var src = """
            using System;

            class P
            {
                static int evals;
                static int B() { evals++; return 42; }

                static void Main()
                {
                    int a = 0;
                    long c = 100L;
                    evals = 0;
                    bool r = a < B() < c;
                    Console.WriteLine($"r={r}, evals={evals}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=True, evals=1")
            .VerifyDiagnostics();
    }

    [Fact]
    public void AsymmetricConversion_WideningOnInnerLink_ShortIntInt()
    {
        // Inner widens `a` to int; outer is pure `int<int`. No asymmetry on b.
        var src = """
            using System;

            class P
            {
                static int evals;
                static int B() { evals++; return 42; }

                static void Main()
                {
                    short a = 0;
                    int c = 100;
                    evals = 0;
                    bool r = a < B() < c;
                    Console.WriteLine($"r={r}, evals={evals}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=True, evals=1")
            .VerifyDiagnostics();
    }

    [Fact]
    public void AsymmetricConversion_BothLinksDiffer_ShortIntLong()
    {
        // Inner is `int<int`, outer is `long<long`. b is evaluated once, converted on each link.
        var src = """
            using System;

            class P
            {
                static int evals;
                static int B() { evals++; return 42; }

                static void Main()
                {
                    short a = 0;
                    long c = 100L;
                    evals = 0;
                    bool r = a < B() < c;
                    Console.WriteLine($"r={r}, evals={evals}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=True, evals=1")
            .VerifyDiagnostics();
    }

    [Fact]
    public void AsymmetricConversion_NegativeLowerBound_DoesNotTruncate()
    {
        // int.MinValue at the lower bound of a widening chain preserves its sign.
        var src = """
            using System;

            class P
            {
                static void Main()
                {
                    int a = int.MinValue;
                    int b = int.MaxValue;
                    long c = (long)int.MaxValue + 100L;

                    bool chained = a < b < c;
                    long bWide = b;
                    bool handWritten = (a < b) && (bWide < c);

                    Console.WriteLine($"chained={chained}, handWritten={handWritten}, match={chained == handWritten}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "chained=True, handWritten=True, match=True")
            .VerifyDiagnostics();
    }

    [Fact]
    public void AsymmetricConversion_EquivalentToHandWrittenShortCircuit_AllCombinations()
    {
        // Chain must equal hand-written `(a < b) && ((long)b < c)` on a full grid.
        var src = """
            using System;

            class P
            {
                static void Main()
                {
                    short[] aVals = { short.MinValue, -1, 0, 1, short.MaxValue };
                    int[]   bVals = { int.MinValue, -1, 0, 1, int.MaxValue };
                    long[]  cVals = { long.MinValue, -1L, 0L, 1L, (long)int.MaxValue + 1, long.MaxValue };

                    int total = 0, mismatched = 0;
                    foreach (var a in aVals)
                    foreach (var b in bVals)
                    foreach (var c in cVals)
                    {
                        bool chained = a < b < c;
                        bool handWritten = ((int)a < b) && ((long)b < c);
                        total++;
                        if (chained != handWritten) mismatched++;
                    }
                    Console.WriteLine($"total={total}, mismatched={mismatched}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "total=150, mismatched=0")
            .VerifyDiagnostics();
    }

    [Fact]
    public void AsymmetricConversion_ShortCircuitOfRightHandSide_NotEvaluated()
    {
        // Short-circuit still holds under asymmetric widening.
        var src = """
            using System;

            class P
            {
                static int bCalls;
                static int cCalls;
                static int B() { bCalls++; return 42; }
                static long C() { cCalls++; return 100L; }

                static void Main()
                {
                    int a = 100;
                    bCalls = cCalls = 0;
                    bool r = a < B() < C();
                    Console.WriteLine($"r={r}, bCalls={bCalls}, cCalls={cCalls}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=False, bCalls=1, cCalls=0")
            .VerifyDiagnostics();
    }

    #endregion

    #region operator true / operator false interactions

    [Fact]
    public void OperatorTrueFalse_DefinedOnOperandType_DoesNotAffectChain()
    {
        // The chain's `&&`-combination is over bools; `operator true`/`false` on S must not run.
        var src = """
            using System;

            struct S
            {
                public int V;
                public S(int v) => V = v;

                public static bool operator <(S a, S b) { Console.WriteLine($"  < {a.V} {b.V}"); return a.V < b.V; }
                public static bool operator >(S a, S b) => a.V > b.V;
                public static bool operator <=(S a, S b) => a.V <= b.V;
                public static bool operator >=(S a, S b) => a.V >= b.V;
                public static bool operator ==(S a, S b) => a.V == b.V;
                public static bool operator !=(S a, S b) => a.V != b.V;

                public static bool operator true(S s)  { Console.WriteLine($"  op_true called on {s.V}");  return s.V != 0; }
                public static bool operator false(S s) { Console.WriteLine($"  op_false called on {s.V}"); return s.V == 0; }

                public override bool Equals(object o) => o is S s && s.V == V;
                public override int GetHashCode() => V;
            }

            class P
            {
                static void Main()
                {
                    Console.WriteLine("S(0) < S(1) < S(2):");
                    Console.WriteLine("  result=" + (new S(0) < new S(1) < new S(2)));
                    Console.WriteLine("S(5) < S(1) < S(2) (short-circuit):");
                    Console.WriteLine("  result=" + (new S(5) < new S(1) < new S(2)));
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: """
                S(0) < S(1) < S(2):
                  < 0 1
                  < 1 2
                  result=True
                S(5) < S(1) < S(2) (short-circuit):
                  < 5 1
                  result=False
                """)
            .VerifyDiagnostics();
    }

    [Fact]
    public void NonBoolReturningOperator_PreservesClassicalBinding()
    {
        // Non-bool-returning `operator <` binds classically; chain fallback never triggers.
        var src = """
            struct S
            {
                public static S operator <(S a, S b) => default;
                public static S operator >(S a, S b) => default;
                public static S operator <=(S a, S b) => default;
                public static S operator >=(S a, S b) => default;
                public static S operator ==(S a, S b) => default;
                public static S operator !=(S a, S b) => default;
                public override bool Equals(object o) => false;
                public override int GetHashCode() => 0;
            }

            class P
            {
                static bool F(S a, S b, S c) => a < b < c;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (15,37): error CS0029: Cannot implicitly convert type 'S' to 'bool'
                //     static bool F(S a, S b, S c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "a < b < c").WithArguments("S", "bool").WithLocation(15, 37));
    }

    [Fact]
    public void ChainFallback_OuterLinkResolvesToNonBoolOperator_ReportsCS9388()
    {
        // Outer `b < c` resolves but its operator returns S, not bool -> CS9388.
        var src = """
            struct S
            {
                public static bool operator <(S a, S b) => true;
                public static bool operator >(S a, S b) => false;
                public static bool operator <=(S a, S b) => true;
                public static bool operator >=(S a, S b) => false;
                public static bool operator ==(S a, S b) => true;
                public static bool operator !=(S a, S b) => false;

                public static S operator <(S a, int b) => default;
                public static S operator >(S a, int b) => default;
                public static S operator <=(S a, int b) => default;
                public static S operator >=(S a, int b) => default;

                public override bool Equals(object o) => false;
                public override int GetHashCode() => 0;
            }

            class P
            {
                static bool F(S a, S b, int c) => a < b < c;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (21,45): error CS9388: Operator '<' cannot be applied to operands of type 'S' and 'int' as a chained relational comparison.
                //     static bool F(S a, S b, int c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "S", "int").WithLocation(21, 45));
    }

    [Fact]
    public void ChainFallback_OuterLinkResolutionIsAmbiguous_ReportsCS9388()
    {
        // Ambiguous `Y op B` overload resolution also routes through CS9388.
        var src = """
            struct Wrap1
            {
                public static implicit operator Wrap1(int i) => default;
            }

            struct Wrap2
            {
                public static implicit operator Wrap2(int i) => default;
            }

            struct S
            {
                public static bool operator <(S a, S b) => true;
                public static bool operator >(S a, S b) => false;
                public static bool operator <=(S a, S b) => true;
                public static bool operator >=(S a, S b) => false;
                public static bool operator ==(S a, S b) => true;
                public static bool operator !=(S a, S b) => false;

                public static bool operator <(S a, Wrap1 b) => true;
                public static bool operator >(S a, Wrap1 b) => false;
                public static bool operator <=(S a, Wrap1 b) => true;
                public static bool operator >=(S a, Wrap1 b) => false;

                public static bool operator <(S a, Wrap2 b) => true;
                public static bool operator >(S a, Wrap2 b) => false;
                public static bool operator <=(S a, Wrap2 b) => true;
                public static bool operator >=(S a, Wrap2 b) => false;

                public override bool Equals(object o) => false;
                public override int GetHashCode() => 0;
            }

            class P
            {
                static bool F(S a, S b, int c) => a < b < c;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (36,45): error CS9388: Operator '<' cannot be applied to operands of type 'S' and 'int' as a chained relational comparison.
                //     static bool F(S a, S b, int c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "S", "int").WithLocation(36, 45));
    }

    [Fact]
    public void ChainFallback_OnOlderLanguageVersion_ReportsBothPreviewAndRule2Errors()
    {
        // Both CS8652 (preview-gate) and CS9388 (rule 2(b)) fire on an older langver.
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

            struct Point { }

            class P
            {
                static bool F(S a, S b, Point c) => a < b < c;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.Regular14)
            .VerifyDiagnostics(
                // (17,41): error CS8652: The feature 'chained relational comparison' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static bool F(S a, S b, Point c) => a < b < c;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "a < b < c").WithArguments("chained relational comparison").WithLocation(17, 41),
                // (17,47): error CS9388: Operator '<' cannot be applied to operands of type 'S' and 'Point' as a chained relational comparison.
                //     static bool F(S a, S b, Point c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "S", "Point").WithLocation(17, 47));
    }

    [Fact]
    public void ChainFallback_InnerConversionShapesYAwayFromCompatibleRawType_ReportsCS9388()
    {
        // The outer resolution sees Y *as classified by the inner link*. The inner link
        // converts `b` from B to int, and outer `int < string` has no operator -> CS9388.
        var src = """
            struct B
            {
                public static implicit operator int(B b) => 0;

                public static bool operator <(B a, string b) => true;
                public static bool operator >(B a, string b) => false;
                public static bool operator <=(B a, string b) => true;
                public static bool operator >=(B a, string b) => false;
                public static bool operator ==(B a, string b) => false;
                public static bool operator !=(B a, string b) => true;

                public override bool Equals(object o) => false;
                public override int GetHashCode() => 0;
            }

            class P
            {
                static bool F(int a, B b, string c) => a < b < c;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (18,50): error CS9388: Operator '<' cannot be applied to operands of type 'int' and 'string' as a chained relational comparison.
                //     static bool F(int a, B b, string c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "int", "string").WithLocation(18, 50));
    }

    [Theory]
    [InlineData("<")]
    [InlineData("<=")]
    [InlineData(">")]
    [InlineData(">=")]
    public void ChainFallback_OuterLinkError_ReportsCS9388_ForEveryChainableOperator(string op)
    {
        // Cross-check the CS9388 path on each of the four chainable operators.
        var src = $$"""
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

            struct Point { }

            class P
            {
                static bool F(S a, S b, Point c) => a {{op}} b {{op}} c;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, op).WithArguments(op, "S", "Point"));
    }

    [Fact]
    public void ChainFallback_OuterLinkHasNoApplicableOperator_ReportsCS9388()
    {
        // Outer `S < Point` has no applicable operator -> CS9388 (not CS0019).
        var src = """
            struct S
            {
                public static bool operator <(S a, S b) => true;
                public static bool operator >(S a, S b) => false;
                public static bool operator <=(S a, S b) => true;
                public static bool operator >=(S a, S b) => false;
                public static bool operator ==(S a, S b) => true;
                public static bool operator !=(S a, S b) => false;

                public static bool operator true(S s)  => true;
                public static bool operator false(S s) => false;

                public override bool Equals(object o) => false;
                public override int GetHashCode() => 0;
            }

            struct Point { }

            class P
            {
                static bool F(S a, S b, Point c) => a < b < c;
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (21,47): error CS9388: Operator '<' cannot be applied to operands of type 'S' and 'Point' as a chained relational comparison.
                //     static bool F(S a, S b, Point c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "S", "Point").WithLocation(21, 47));
    }

    #endregion

    #region Definite assignment interactions

    [Fact]
    public void DefiniteAssignment_MiddleOperandAssignsLocalReadByOuterRight()
    {
        // If the middle operand assigns to a local, that local is definitely assigned
        // when the outer-right operand runs.
        var src = """
            using System;

            class P
            {
                static int cCalls;
                static int C() { cCalls++; return 5; }
                static int D(int x) { Console.Write($"D({x}) "); return x + 10; }

                static void Main()
                {
                    int a = 0;
                    int b;
                    bool r = a < (b = C()) < D(b);

                    Console.WriteLine($"r={r}, cCalls={cCalls}, b={b}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "D(5) r=True, cCalls=1, b=5")
            .VerifyDiagnostics();
    }

    #endregion

    #region Generic constrained operands (static abstract interface operators)
    //
    // Chains over a generic `T` constrained to an interface with static-abstract
    // relational operators. IL-level pins live in ChainedRelationalComparisonEmitTests.

    private const string GenericConstrainedHarness = """
        #nullable enable
        using System;

        public interface ILT<TSelf> where TSelf : ILT<TSelf>
        {
            static abstract bool operator <(TSelf a, TSelf b);
            static abstract bool operator >(TSelf a, TSelf b);
            static abstract bool operator <=(TSelf a, TSelf b);
            static abstract bool operator >=(TSelf a, TSelf b);
        }

        public sealed class RefImpl : ILT<RefImpl>
        {
            public readonly int V;
            public RefImpl(int v) { V = v; }
            public static bool operator <(RefImpl a, RefImpl b) => a.V < b.V;
            public static bool operator >(RefImpl a, RefImpl b) => a.V > b.V;
            public static bool operator <=(RefImpl a, RefImpl b) => a.V <= b.V;
            public static bool operator >=(RefImpl a, RefImpl b) => a.V >= b.V;
        }

        public struct ValImpl : ILT<ValImpl>
        {
            public readonly int V;
            public ValImpl(int v) { V = v; }
            public static bool operator <(ValImpl a, ValImpl b) => a.V < b.V;
            public static bool operator >(ValImpl a, ValImpl b) => a.V > b.V;
            public static bool operator <=(ValImpl a, ValImpl b) => a.V <= b.V;
            public static bool operator >=(ValImpl a, ValImpl b) => a.V >= b.V;
        }
        """;

    // CoreClrOnly: NetCoreApp-referenced binary can't execute on desktop.
    [ConditionalTheory(typeof(CoreClrOnly))]
    // (constraintPrefix, nullabilitySuffix, typeArgument)
    [InlineData("", "", "RefImpl")]
    [InlineData("", "", "ValImpl")]
    [InlineData("", "?", "RefImpl")]
    [InlineData("class, ", "", "RefImpl")]
    [InlineData("class, ", "?", "RefImpl")]
    [InlineData("struct, ", "", "ValImpl")]
    public void GenericConstraint_ConstrainedDispatch_ChainBindsAndRuns(string constraintPrefix, string nullabilitySuffix, string typeArgument)
    {
        var src = GenericConstrainedHarness + $$"""

            class P
            {
                #pragma warning disable CS8604
                static bool Chain<T>(T{{nullabilitySuffix}} a, T{{nullabilitySuffix}} b, T{{nullabilitySuffix}} c)
                    where T : {{constraintPrefix}}ILT<T> => a < b < c;
                #pragma warning restore CS8604

                static void Main()
                {
                    Console.WriteLine(Chain<{{typeArgument}}>(new(0), new(5), new(10)));
                    Console.WriteLine(Chain<{{typeArgument}}>(new(0), new(5), new(2)));
                    Console.WriteLine(Chain<{{typeArgument}}>(new(10), new(5), new(100)));
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            targetFramework: TargetFramework.NetCoreApp,
            verify: Verification.Skipped,
            expectedOutput: """
                True
                False
                False
                """)
            .VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericConstraint_InterfaceAndStruct_NullableT_ChainBindsAndRunsWithLifting()
    {
        // `T? a, T? b, T? c` with `where T : struct, ILT<T>` uses lifted relational semantics.
        var src = GenericConstrainedHarness + """

            class P
            {
                static bool ChainNullable<T>(T? a, T? b, T? c) where T : struct, ILT<T>
                    => a < b < c;

                static void Main()
                {
                    Console.WriteLine(ChainNullable<ValImpl>(new ValImpl(0), new ValImpl(5), new ValImpl(10)));
                    Console.WriteLine(ChainNullable<ValImpl>(null, new ValImpl(5), new ValImpl(10)));
                    Console.WriteLine(ChainNullable<ValImpl>(new ValImpl(0), null, new ValImpl(10)));
                    Console.WriteLine(ChainNullable<ValImpl>(new ValImpl(0), new ValImpl(5), null));
                    Console.WriteLine(ChainNullable<ValImpl>(new ValImpl(0), new ValImpl(5), new ValImpl(2)));
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            targetFramework: TargetFramework.NetCoreApp,
            verify: Verification.Skipped,
            expectedOutput: """
                True
                False
                False
                False
                False
                """)
            .VerifyDiagnostics();
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericConstraint_InterfaceAndStruct_NullableT_MiddleOperandEvaluatedOnce()
    {
        // Single-evaluation also holds under lifted-over-generic-constrained dispatch.
        var src = GenericConstrainedHarness + """

            class P
            {
                static int middleCalls;
                static ValImpl? Middle() { middleCalls++; return new ValImpl(5); }

                static bool Chain<T>(T? a, Func<T?> middle, T? c) where T : struct, ILT<T>
                    => a < middle() < c;

                static void Main()
                {
                    middleCalls = 0;
                    bool r = Chain<ValImpl>(new ValImpl(0), Middle, new ValImpl(10));
                    Console.WriteLine($"r={r}, middleCalls={middleCalls}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            targetFramework: TargetFramework.NetCoreApp,
            verify: Verification.Skipped,
            expectedOutput: "r=True, middleCalls=1")
            .VerifyDiagnostics();
    }

    #endregion
}
