// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

    [Fact]
    public void Chain_Nullable_LiftedOperatorsReturnBool()
    {
        // Lifted relational operators return bool (not bool?) per spec §11.4.8, so a chain
        // over int? / double? operands classifies each link as bool and the chain is
        // accepted. A null operand makes the corresponding comparison yield false, which
        // short-circuits the chain.
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
        // `a < b > c` means `a < b && b > c` per the chain rule; there is no requirement
        // that all links go the same direction.
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
    public void Chain_FourAndFiveOperands_WorkNAry()
    {
        // The chain rule is recursive, so arbitrary-length chains fall out.
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
        // The single-evaluation guarantee: the shared middle operand in `min <= M() <= max`
        // is evaluated exactly once even though it appears syntactically at the right of
        // one comparison and the left of the next.
        var src = """
            using System;

            class P
            {
                static int calls;
                static int M() { calls++; return 5; }

                static void Main()
                {
                    calls = 0;
                    bool r = 0 <= M() <= 10;
                    Console.WriteLine($"result={r}, calls={calls}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "result=True, calls=1")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_AllOperandsAreMethodCalls_EachEvaluatedOnceInLeftToRightOrder()
    {
        // `A() < B() < C()` must evaluate each of A, B, C exactly once and in source order.
        // In particular B() - the shared middle - must not be evaluated twice as the naive
        // `A() < B() && B() < C()` rewrite would do. C() (and anything under it) is only
        // evaluated when the inner link was true.
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
                    log = new(); Report("3<2<1", C() < B() < A()); // inner false -> outer skipped
                    log = new(); Report("1<3<2", A() < C() < B()); // inner true, outer false; still evaluates all three
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
    public void Chain_FourMethodCalls_EachSharedMiddleEvaluatedOnce()
    {
        // In `A() < B() < C() < D()`, both B and C are shared middle operands (B across
        // links 1-2, C across links 2-3). Each must be evaluated exactly once, in source
        // order, with short-circuit at the first false link.
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
                    // All links true -> all four evaluated in order.
                    log = new(); Report("1<2<3<4", A() < B() < C() < D());

                    // Inner link false -> remaining operands skipped.
                    log = new(); Report("2<1<3<4 (inner false)", B() < A() < C() < D());

                    // Middle link false -> last operand skipped.
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
    public void Chain_IL_CanonicalBoundsCheckShape()
    {
        // Canonical shape `0 <= i < array.Length` demonstrates the lowered form: a single
        // temp for the shared middle operand i (captured via inline-assign), followed by an
        // &&-chain. This is a same-type chain with no asymmetric conversions, so the IL is
        // clean and verifiable.
        var src = """
            class P
            {
                static bool InBounds(int i, int[] a) => 0 <= i < a.Length;
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.ReleaseDll)
            .VerifyDiagnostics()
            .VerifyIL("P.InBounds", """
                {
                  // Code size       15 (0xf)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldc.i4.0
                  IL_0001:  ldarg.0
                  IL_0002:  stloc.0
                  IL_0003:  ldloc.0
                  IL_0004:  bgt.s      IL_000d
                  IL_0006:  ldloc.0
                  IL_0007:  ldarg.1
                  IL_0008:  ldlen
                  IL_0009:  conv.i4
                  IL_000a:  clt
                  IL_000c:  ret
                  IL_000d:  ldc.i4.0
                  IL_000e:  ret
                }
                """);
    }

    [Fact]
    public void Chain_UserDefinedBoolReturningOperator_Works()
    {
        // A user type with a bool-returning `operator <` chains via the normal fallback
        // (classical binding fails on `bool < T`, then isolated `T < T` resolves and
        // returns bool, so the chain is accepted).
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
        // `(a < b) < c` has a parenthesized_expression as its left operand, not a
        // relational_expression of the chain shape. Therefore the chain rule does not
        // apply (spec §11.11.13 and its parentheses note) and the user sees the
        // ordinary CS0019 for `bool < int`.
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
        // `a == b == c` parses as `(a == b) == c`, i.e. a comparison of a bool to a
        // non-bool. That has always been a CS0019 and remains one; the chain rule only
        // applies to the relational operators `<`, `<=`, `>`, `>=`.
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
    public void ChainInsideExpressionTree_ReportsCS9380()
    {
        // Chained relational comparisons cannot be represented as System.Linq.Expressions
        // trees while preserving the single-evaluation guarantee, so any attempt to convert
        // one to an expression tree produces the specific diagnostic
        // ERR_ExpressionTreeContainsChainedRelationalComparison.
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
                // (6,67): error CS9380: An expression tree may not contain a chained relational comparison.
                //     static Expression<Func<int, int, int, bool>> F = (a, b, c) => a < b < c;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsChainedRelationalComparison, "a < b < c").WithLocation(6, 67));
    }

    [Fact]
    public void Dynamic_IsBoundDynamically_NoChainFallback()
    {
        // When any operand is `dynamic`, each relational_expression node is dynamically
        // bound per spec §11.11.1; dynamic binding never produces a binding-time error,
        // so overload resolution of `A op B` "succeeds" at compile time and §11.11.13
        // does not apply. The chain is therefore classical left-associative:
        //
        //   `a < b < c`   compiles as   `(a < b) < c`
        //
        // with each link dispatched at run time through the DLR. Since `a < b` yields a
        // bool and `bool < int` is not a valid operation for the dynamic binder, the
        // second dispatch throws RuntimeBinderException. The chain feature is
        // intentionally not in play here - this test pins that behaviour.
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
        // The feature is gated on LanguageVersion.Preview via
        // IDS_FeatureChainedRelationalComparison. Using an older language version produces
        // ERR_FeatureInPreview; the chain is still bound so the user does not see a
        // cascade of unrelated CS0019 errors.
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
    // These tests pin the behaviour of the "Asymmetric conversions" open question
    // in the spec: when a shift_expression Y appears as the right operand of one
    // comparison and as the left operand of the next, the two overload resolutions
    // can ask for different conversions on Y. The spec defers the final choice to
    // LDM, but documents the current Roslyn implementation as **Option A (permissive)**:
    // Y is evaluated exactly once; each link's conversion is applied at that link's
    // point of use, so the chain is equivalent to an ordinary short-circuit `&&`
    // chain where Y sits in a temp of Y's natural type.
    //
    // KNOWN ISSUE: the current lowering stores the outer-link's pre-converted Y in
    // the temp (e.g. long for an int<int<long chain), which makes the inner link's
    // compare ask for int32 on the stack while finding int64. The resulting IL runs
    // correctly on RyuJIT (sign-extension happens to do the right thing for signed
    // comparisons) but fails formal IL verification. The tests below therefore use
    // Verification.FailsILVerify for the asymmetric cases; when LDM chooses Option A
    // and the lowering is fixed to keep the temp at Y's inner type, these tests
    // should switch to Verification.Passes. If LDM chooses Option B (strict), the
    // tests should switch from CompileAndVerify + ExpectedOutput to a
    // VerifyDiagnostics expectation on ERR_NoChainedRelationalComparison.

    [Fact]
    public void AsymmetricConversion_WideningOnOuterLink_IntIntLong()
    {
        // Inner `a < b`: int < int (no conversion on b).
        // Outer `b < c`: resolved in isolation as `int < long` -> `long < long`, applying int->long to b.
        // Under Option A: b evaluates once; inner uses b as int; outer uses (long)b.
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
            expectedOutput: "r=True, evals=1",
            verify: Verification.FailsILVerify)
            .VerifyDiagnostics();
    }

    [Fact]
    public void AsymmetricConversion_WideningOnInnerLink_ShortIntInt()
    {
        // Inner `a < b`: short < int -> int < int (applies short->int on a, identity on b).
        // Outer `b < c`: int < int (identity both sides).
        // No disagreement on b's conversion; both options accept this.
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
        // The worst case: inner resolves short < int as int<int (no conv on b), outer
        // resolves int < long as long<long (int->long on b). Under Option A, b
        // evaluates once and each link uses its own view of the value.
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
            expectedOutput: "r=True, evals=1",
            verify: Verification.FailsILVerify)
            .VerifyDiagnostics();
    }

    [Fact]
    public void AsymmetricConversion_NegativeLowerBound_DoesNotTruncate()
    {
        // Sanity: if the widening chain were silently truncating, int.MinValue would
        // misbehave. Verify it produces the same result as the equivalent hand-written
        // short-circuit form.
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
            expectedOutput: "chained=True, handWritten=True, match=True",
            verify: Verification.FailsILVerify)
            .VerifyDiagnostics();
    }

    [Fact]
    public void AsymmetricConversion_EquivalentToHandWrittenShortCircuit_AllCombinations()
    {
        // Exhaustive check over a small grid of (short, int, long) operand values:
        // the chained form must agree with the equivalent hand-written
        // `(a < b) && ((long)b < c)` for every combination. If Option A's semantic
        // ever drifts, the assertion below will catch it.
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
            expectedOutput: "total=150, mismatched=0",
            verify: Verification.FailsILVerify)
            .VerifyDiagnostics();
    }

    [Fact]
    public void AsymmetricConversion_ShortCircuitOfRightHandSide_NotEvaluated()
    {
        // When the inner link is false, the outer operand (and anything under it)
        // must not be evaluated, even when an asymmetric widening conversion is
        // involved. This test keeps Option A honest about the short-circuit
        // guarantee under widening.
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
                    int a = 100; // a > B(), so the inner link is false and outer is skipped.
                    bCalls = cCalls = 0;
                    bool r = a < B() < C();
                    Console.WriteLine($"r={r}, bCalls={bCalls}, cCalls={cCalls}");
                }
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=False, bCalls=1, cCalls=0",
            verify: Verification.FailsILVerify)
            .VerifyDiagnostics();
    }

    #endregion

    #region operator true / operator false interactions
    //
    // The chained-relational feature does not involve `operator true`/`operator false` at any
    // layer: §11.11.13 rule 2(b) requires every link's isolated overload resolution to select
    // a bool-returning operator, and the chain is combined via plain `LogicalBoolAnd`. So
    // the truth-operator slot on BoundBinaryOperator (LeftTruthOperatorMethod) is never
    // non-null for a chained-relational node. These tests pin that invariant.

    [Fact]
    public void OperatorTrueFalse_DefinedOnOperandType_DoesNotAffectChain()
    {
        // S defines `operator <` returning bool AND `operator true`/`operator false`.
        // The chain should resolve normally (bool-returning `<` on both links) and the
        // truth operators should not be invoked: the chain's &&-combination is over bools.
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
        // No "op_true" / "op_false" lines should appear in the output - the chain uses a
        // plain bool `&&`, not the user-defined conditional logical.
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
        // S defines `operator <(S, S)` returning S (a custom "comparison result" type).
        // Ordinary overload resolution succeeds at every chain link: `a < b` binds to the
        // user-defined operator, yielding an S; `(S) < c` binds to the same operator,
        // yielding an S. Chain fallback (§11.11.13) therefore never activates - back-compat
        // is preserved, and any diagnostic comes from using the S result where a bool is
        // required (CS0029), not from the chain feature.
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
    public void ChainFallback_OuterLinkResolvesToNonBoolOperator_ReportsCS9379()
    {
        // `a < b` resolves to a bool-returning `operator <(S, S)` so the chain shape exists
        // and classical `(bool) < int` fails. The chain fallback then runs isolated `b < c`
        // against (S, int), which resolves successfully to `operator <(S, int)` - but that
        // operator returns S, not bool. Spec §11.11.13 rule 2(b) requires a bool-returning
        // operator, so the chain must be rejected with the specific diagnostic
        // (ERR_NoChainedRelationalComparison / CS9379), not CS0019 or CS0029.
        var src = """
            struct S
            {
                public static bool operator <(S a, S b) => true;
                public static bool operator >(S a, S b) => false;
                public static bool operator <=(S a, S b) => true;
                public static bool operator >=(S a, S b) => false;
                public static bool operator ==(S a, S b) => true;
                public static bool operator !=(S a, S b) => false;

                // Non-bool-returning overload against int - picks up during isolated
                // resolution of `S < int` in the chain fallback.
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
                // (23,45): error CS9379: Operator '<' cannot be applied to operands of type 'S' and 'int' as a chained relational comparison.
                //     static bool F(S a, S b, int c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "S", "int").WithLocation(23, 45));
    }

    [Fact]
    public void ChainFallback_OuterLinkHasNoApplicableOperator_ReportsCS9379()
    {
        // `a < b` is a bool-returning user-defined comparison on S, so the chain shape
        // exists and classical binding of `(bool) < c` fails. The chain fallback then tries
        // isolated `b < c` (i.e. `S < Point`), which has no applicable operator. That is
        // exactly the §11.11.13 rule 2(b) failure, so the specific
        // ERR_NoChainedRelationalComparison must be reported here rather than the generic
        // CS0019. No operator true / operator false on S interferes.
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
                // (21,47): error CS9379: Operator '<' cannot be applied to operands of type 'S' and 'Point' as a chained relational comparison.
                //     static bool F(S a, S b, Point c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "S", "Point").WithLocation(21, 47));
    }

    #endregion
}
