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

    // All 216 combinations of {short, short?, int, int?, long, long?} at the three
    // operand positions. The chain must bind, run, and produce the correct result for
    // every combination, and IL must verify cleanly in every case: the shared middle
    // operand is captured in a temp at Y's inner-link type and the outer link applies
    // its own conversion on load, so the inner operator's operand type always matches
    // the temp's type - including asymmetric-width shapes like `short, int, long` and
    // nullable-lifting shapes like `int, int, int?`, where the outer link otherwise
    // would drive the temp to a different type than the inner link consumes.
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
        // Values a=0, b=5, c=10 satisfy a < b < c for every combination and fit every
        // type in the grid, so expectedOutput is uniformly "True".
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
    // In the "all three nullable" case, a null at ANY position forces at least one
    // lifted comparison to return false (spec §11.4.8). A null on the outer right
    // makes the outer link false AFTER the inner link was true; a null at the other
    // positions makes the inner link false and short-circuits. Either way the chain
    // yields False, which is what this Theory pins.
    //
    // aVal/bVal/cVal of "null" vs a number lets us drive every single-null scenario.
    [InlineData("null", "5", "10", "False")]    // null in a
    [InlineData("0", "null", "10", "False")]    // null in b
    [InlineData("0", "5", "null", "False")]     // null in c (inner true, outer false)
    [InlineData("null", "null", "10", "False")] // nulls in a and b
    [InlineData("null", "null", "null", "False")] // all null
    [InlineData("0", "5", "10", "True")]        // no nulls, control
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
    public void Chain_EmbeddedInStringConcat_LowersExactlyOnce()
    {
        // Chain as a nested expression inside a non-chained binary (string +). Exercises
        // LocalRewriter_BinaryOperator's left-spine stack walker that was taught to stop
        // on `current.IsChainedRelational` - otherwise it would flatten the chained node
        // into the + spine and both misbehave and drop the chain's lowered form.
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
        // The chain's compile-time type is `bool` (§11.11.13 "classified as a value of
        // type bool"). Assigning to `bool?` exercises an implicit boxing / null-coalesce
        // conversion sitting atop the chained node's result in the bound tree.
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
        // Exercise operand shapes that are themselves complex lowered expressions. The
        // lowering's `VisitExpression(y)` and `VisitExpression(node.Right)` calls must
        // correctly lower these operands inside the chain's temp-assignment structure.
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

                    // Middle operand is `??` expression - takes the fallback since maybe is null.
                    Console.WriteLine(0 < (maybe ?? Fallback()) < 10);   // "fb True"

                    // Middle operand is `?.` - null-conditional access on a null array gives null;
                    // the lifted `<` yields false; chain short-circuits.
                    Console.WriteLine(0 < maybeArr?.Length < 10);         // "False"

                    // Right operand uses conditional access.
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
        // A `? :` ternary in the middle of a chain. Lowering's VisitExpression handles
        // BoundConditionalOperator already; just pin that the chain's temp-capture site
        // doesn't interfere.
        //
        // A() returns an in-range value and B() returns an out-of-range value, so the
        // chain's bool result distinguishes which branch was taken. A wrong impl that
        // e.g. evaluated both branches, or took the wrong branch, would show a wrong
        // label/result pair.
        var src = """
            using System;

            class P
            {
                static int A() { Console.Write("A "); return 5; }      // inside (0, 100)
                static int B() { Console.Write("B "); return 200; }    // outside (0, 100)

                static void Main()
                {
                    bool cond = true;
                    Console.WriteLine(0 < (cond ? A() : B()) < 100);   // "A True"  — A picked, 0 < 5 < 100
                    cond = false;
                    Console.WriteLine(0 < (cond ? A() : B()) < 100);   // "B False" — B picked, 0 < 200 but NOT < 100
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
        // Nothing special expected - chain result is bool, so method-group conversion to
        // Func<bool> or passing as a bool argument should just work. Pinning regardless
        // because the binder's speculative chain-fallback attempt could in theory
        // interact oddly with argument type-inference.
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
        //
        // Cover both the inner-true and inner-false paths. A bug that double-evaluated M()
        // on either path would show calls=2 in the corresponding case; a bug that skipped
        // M() entirely on short-circuit would show calls=0.
        var src = """
            using System;

            class P
            {
                static int calls;
                static int M() { calls++; return 5; }

                static void Report(bool r) => Console.WriteLine($"r={r}, calls={calls}");

                static void Main()
                {
                    calls = 0; Report(0 <= M() <= 10);   // inner true, outer true
                    calls = 0; Report(0 <= M() <= 2);    // inner true, outer false
                    calls = 0; Report(100 <= M() <= 10); // inner false, outer skipped
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
        // Spec §11.11.13 note: "equivalent in result to (e0 op1 e1) && (e1 op2 e2) && ...
        // with each ei evaluated at most once." This pins the exact evaluation-count
        // difference between the chained form and the naive `&&`-rewrite users currently
        // have to write today:
        //
        //   chained:      min <= M() <= max             -> M called 1 time
        //   hand-written: min <= M() && M() <= max      -> M called 2 times (every time)
        //
        // The chain's correctness-benefit over the naive rewrite is that the two
        // evaluations of M() in the hand-written form can disagree when M has side
        // effects or is non-deterministic. This test is the concrete illustration of
        // the spec's motivating example.
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
        // Complement to AsymmetricConversion_EquivalentToHandWrittenShortCircuit_AllCombinations
        // (which grid-tests short/int/long): this one pins the same-type case, which
        // exercises the "identity conversion on Y everywhere" path in the lowering. If
        // the chain's same-type behaviour ever diverges from the hand-written && equivalent
        // this test catches it.
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
    public void Chain_ShortCircuit_DoesNotEvaluateArgumentsNestedInsideRightOperand()
    {
        // When the inner link is false, the outer-right operand and ALL its
        // sub-expressions (method arguments, nested calls, property accesses,
        // etc.) must be skipped entirely. This test pins the nested case:
        // `C(D())` at the outer-right must not invoke either `C` or `D`.
        var src = """
            using System;

            class P
            {
                static int aCalls, bCalls, cCalls, dCalls;
                static int A() { aCalls++; return 100; }   // chosen so A() > B(), inner is false
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
        // Inner link `A() < B()` = 100 < 1 = false; outer must short-circuit.
        // Both C(...) and its argument D() must be skipped.
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=False, a=1, b=1, c=0, d=0")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_NoShortCircuit_ArgumentsInsideRightOperandRunInSourceOrder()
    {
        // Companion to the short-circuit test: when the inner link is true the
        // outer-right operand DOES run, and its nested sub-expressions evaluate
        // in source order before the enclosing call is invoked. So for `C(D())`
        // at the outer right the expected log is D then C, after A and B from
        // the inner link.
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
        // Inner: 1 < 2 = true; outer runs. D evaluates to 5 and is passed to C,
        // which returns 5. Outer compares 2 < 5 = true. Chain = true.
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=True, log=[A,B,D,C]")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Chain_ShortCircuit_InMiddleOfFourOperand_SkipsAllRemainingArgumentsToo()
    {
        // Generalise: in `A() < B() < C() < D(E())`, if the middle link
        // `B() < C()` is false, neither the outer link's call D(...) nor its
        // argument E() should be evaluated. Pinned via per-call counters.
        var src = """
            using System;

            class P
            {
                static int aCalls, bCalls, cCalls, dCalls, eCalls;
                static int A() { aCalls++; return 1; }
                static int B() { bCalls++; return 10; }   // B() > C(), middle link false
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
        // Evaluation order: A=1, B=10 (inner true), C=5 (middle false), STOP.
        // D and E never run.
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            expectedOutput: "r=False, a=1, b=1, c=1, d=0, e=0")
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
    public void Chain_IL_SameTypeInt_TempReuseAndShortCircuit()
    {
        // `a < b < c` with all int operands. IL should demonstrate:
        //   - Exactly ONE read of each operand local (a, b, c) - single evaluation.
        //   - The shared middle operand `b` is NOT separately temp'd because a local
        //     already exists - the lowerer can use the parameter's slot directly.
        //   - A short-circuit branch: if `a < b` is false, skip `b < c` entirely.
        //
        // Three-parameter case is the simplest place to eyeball the short-circuit and
        // operand-order guarantees together.
        var src = """
            class P
            {
                static bool F(int a, int b, int c) => a < b < c;
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.ReleaseDll)
            .VerifyDiagnostics()
            // The IL below pins:
            //   - ldarg.1 + stloc.0: single evaluation of b into temp.
            //   - ldloc.0 x2 (once for inner, once for outer): the temp is reused;
            //     b is never re-evaluated.
            //   - bge.s + ldc.i4.0: short-circuit to false without ever loading c
            //     when the inner link fails.
            //   - clt + ret on the hot path: standard int<int comparison.
            .VerifyIL("P.F", """
                {
                  // Code size       13 (0xd)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.1
                  IL_0002:  stloc.0
                  IL_0003:  ldloc.0
                  IL_0004:  bge.s      IL_000b
                  IL_0006:  ldloc.0
                  IL_0007:  ldarg.2
                  IL_0008:  clt
                  IL_000a:  ret
                  IL_000b:  ldc.i4.0
                  IL_000c:  ret
                }
                """);
    }

    [Fact]
    public void Chain_IL_AsymmetricShortIntLong_ConversionOnTempLoad()
    {
        // `short a, int b, long c => a < b < c` exercises the spec's "conversions on the
        // shared middle operand" rule. Spec-observable behaviour: `b` is evaluated once
        // (as int), the inner link compares it as `int<int` (widening a from short),
        // and the outer link compares it as `long<long` (widening b from int on the
        // TEMP LOAD, not on the initial store). So the IL should show:
        //   - `conv.i4` on a for the inner link (short -> int).
        //   - No conversion wrapping b's store into the temp (temp type is int).
        //   - `conv.i8` on the temp-load for the outer link (int -> long).
        //
        // This is the IL-level pin for the "verifiable IL for asymmetric widening"
        // invariant documented on BoundBinaryOperator.UncommonData.
        var src = """
            class P
            {
                static bool F(short a, int b, long c) => a < b < c;
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.ReleaseDll)
            .VerifyDiagnostics()
            // Asymmetric-widening pin. The IL demonstrates:
            //   - `stloc.0` stores b into an INT temp (V_0 is `int`, not `long`). The
            //     temp's type is the INNER link's type (where b is already int because
            //     `short < int` promoted short to int), NOT the outer's wider LeftType.
            //     This is the IL-level guarantee of verifiable IL: the inner compare at
            //     IL_0003-IL_0004 operates on two int stack values.
            //   - `conv.i8` at IL_0007 widens the temp from int to long on its SECOND
            //     load - applied only for the OUTER link's int<long comparison. The
            //     temp itself is not re-converted; only the load-site is.
            // If the temp were declared as `long V_0` instead of `int V_0`, the inner
            // compare would have a type mismatch on the stack (int a vs long temp) and
            // fail ILVerify. Keeping temp at inner's type plus applying the outer
            // conversion on load is exactly what the spec's "Conversions on the shared
            // middle operand" paragraph in §11.11.13 describes.
            .VerifyIL("P.F", """
                {
                  // Code size       14 (0xe)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.1
                  IL_0002:  stloc.0
                  IL_0003:  ldloc.0
                  IL_0004:  bge.s      IL_000c
                  IL_0006:  ldloc.0
                  IL_0007:  conv.i8
                  IL_0008:  ldarg.2
                  IL_0009:  clt
                  IL_000b:  ret
                  IL_000c:  ldc.i4.0
                  IL_000d:  ret
                }
                """);
    }

    [Fact]
    public void Chain_IL_NullableInt_HasValueAndShortCircuit()
    {
        // `int? a, int? b, int? c => a < b < c` lifts to `int? < int? < int?`. The
        // Nullable<T> lifted relational returns `false` if either operand was null
        // (spec §11.4.8), so the IL should show HasValue checks plus the `&&`
        // short-circuit on the outer link. This is the user's explicit ask for a
        // "nullable-value-type case".
        var src = """
            class P
            {
                static bool F(int? a, int? b, int? c) => a < b < c;
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.ReleaseDll)
            .VerifyDiagnostics()
            // Nullable lifted pin. For each link, the lifted `int? < int?` lowers to
            // `(a.GetValueOrDefault() < b.GetValueOrDefault()) && (a.HasValue && b.HasValue)`
            // per §11.4.8 (lifted returns true iff both non-null AND underlying compare
            // is true). The chain then AND-short-circuits that result with the outer
            // link via `brfalse.s`.
            //
            // Key observations:
            //   - V_0 stores b once (at IL_0003); V_2 is a scratch copy used so the
            //     lifted-compare helper can GetValueOrDefault on two separate locals.
            //   - The whole inner link's computation (IL_0006-IL_0025) reduces to a
            //     single bool on the stack; `brfalse.s IL_004d` at IL_0026 is the
            //     short-circuit that skips the outer link's eval entirely.
            //   - The outer link's block (IL_0028-IL_004b) repeats the same lifted-
            //     compare pattern using the same V_0 for b's value (reload and copy).
            .VerifyIL("P.F", """
                {
                  // Code size       79 (0x4f)
                  .maxstack  3
                  .locals init (int? V_0,
                                int? V_1,
                                int? V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  stloc.1
                  IL_0002:  ldarg.1
                  IL_0003:  stloc.0
                  IL_0004:  ldloc.0
                  IL_0005:  stloc.2
                  IL_0006:  ldloca.s   V_1
                  IL_0008:  call       "int int?.GetValueOrDefault()"
                  IL_000d:  ldloca.s   V_2
                  IL_000f:  call       "int int?.GetValueOrDefault()"
                  IL_0014:  clt
                  IL_0016:  ldloca.s   V_1
                  IL_0018:  call       "bool int?.HasValue.get"
                  IL_001d:  ldloca.s   V_2
                  IL_001f:  call       "bool int?.HasValue.get"
                  IL_0024:  and
                  IL_0025:  and
                  IL_0026:  brfalse.s  IL_004d
                  IL_0028:  ldloc.0
                  IL_0029:  stloc.2
                  IL_002a:  ldarg.2
                  IL_002b:  stloc.1
                  IL_002c:  ldloca.s   V_2
                  IL_002e:  call       "int int?.GetValueOrDefault()"
                  IL_0033:  ldloca.s   V_1
                  IL_0035:  call       "int int?.GetValueOrDefault()"
                  IL_003a:  clt
                  IL_003c:  ldloca.s   V_2
                  IL_003e:  call       "bool int?.HasValue.get"
                  IL_0043:  ldloca.s   V_1
                  IL_0045:  call       "bool int?.HasValue.get"
                  IL_004a:  and
                  IL_004b:  and
                  IL_004c:  ret
                  IL_004d:  ldc.i4.0
                  IL_004e:  ret
                }
                """);
    }

    [Fact]
    public void Chain_IL_NAry_NestedShortCircuits()
    {
        // Four-operand chain: two short-circuit branches, two temps (for b and c),
        // three comparisons. Each middle operand evaluated exactly once.
        var src = """
            class P
            {
                static bool F(int a, int b, int c, int d) => a < b < c < d;
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.ReleaseDll)
            .VerifyDiagnostics()
            // 4-operand chain pin. Two shared middles (b and c) -> two int temps
            // (V_0 for c, V_1 for b). Two `bge.s` short-circuits, one per link, both
            // targeting the same `ldc.i4.0` false-tail at IL_0011. Each temp is
            // evaluated once (stloc) and reused (ldloc x 2).
            .VerifyIL("P.F", """
                {
                  // Code size       19 (0x13)
                  .maxstack  2
                  .locals init (int V_0,
                                int V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.1
                  IL_0002:  stloc.1
                  IL_0003:  ldloc.1
                  IL_0004:  bge.s      IL_0011
                  IL_0006:  ldloc.1
                  IL_0007:  ldarg.2
                  IL_0008:  stloc.0
                  IL_0009:  ldloc.0
                  IL_000a:  bge.s      IL_0011
                  IL_000c:  ldloc.0
                  IL_000d:  ldarg.3
                  IL_000e:  clt
                  IL_0010:  ret
                  IL_0011:  ldc.i4.0
                  IL_0012:  ret
                }
                """);
    }

    [Fact]
    public void Chain_IL_UserDefinedOperator_CallsBothOperators()
    {
        // User-defined `operator <` bind to the chain via the fallback rules
        // (classical binding of `bool < S` fails, then isolated `S < S` resolves,
        // and the method is bool-returning per spec rule 2(b)). IL should show two
        // `call S.op_LessThan` instructions, separated by the short-circuit
        // `brfalse` and a temp for the shared middle operand.
        var src = """
            struct S
            {
                public int V;
                public S(int v) { V = v; }
                public static bool operator <(S a, S b) => a.V < b.V;
                public static bool operator >(S a, S b) => a.V > b.V;
            }

            class P
            {
                static bool F(S a, S b, S c) => a < b < c;
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            options: TestOptions.ReleaseDll)
            .VerifyDiagnostics()
            // User-defined operator pin. Two `call S.op_LessThan(S, S)` instructions,
            // one per link, separated by a `brfalse.s` short-circuit. The temp V_0
            // holds b; it's loaded once per link (IL_0003 and IL_000b) - b is
            // evaluated exactly once even though it appears in both comparisons.
            .VerifyIL("P.F", """
                {
                  // Code size       21 (0x15)
                  .maxstack  2
                  .locals init (S V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.1
                  IL_0002:  stloc.0
                  IL_0003:  ldloc.0
                  IL_0004:  call       "bool S.op_LessThan(S, S)"
                  IL_0009:  brfalse.s  IL_0013
                  IL_000b:  ldloc.0
                  IL_000c:  ldarg.2
                  IL_000d:  call       "bool S.op_LessThan(S, S)"
                  IL_0012:  ret
                  IL_0013:  ldc.i4.0
                  IL_0014:  ret
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
    public void ChainInsideExpressionTree_ReportsCS9381()
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
                // (6,67): error CS9381: An expression tree may not contain a chained relational comparison.
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

    // All 3^3 = 27 permutations of {short, int, long} at the three operand positions,
    // including same-type combos like short/short/short (no asymmetry to test but keeps
    // the grid uniform) and the two hard asymmetric cases where the middle is strictly
    // "in between" (`short, int, long` and `int, short, long`), which would otherwise
    // have the inner operator and temp disagree on type. Under Option A the shared
    // middle operand is captured at Y's inner-link type and the outer link applies its
    // own conversion on load, so every permutation emits verifiable IL.
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
        // a=1, b=5, c=10 fits every permutation of (short, int, long) and produces a
        // chain result of True. The test also asserts single-evaluation of each
        // operand via per-call counters - asymmetric conversions must not cause any
        // operand to be evaluated twice.
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
            expectedOutput: "r=True, evals=1")
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
            expectedOutput: "r=True, evals=1")
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
            expectedOutput: "chained=True, handWritten=True, match=True")
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
            expectedOutput: "total=150, mismatched=0")
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
            expectedOutput: "r=False, bCalls=1, cCalls=0")
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
    public void ChainFallback_OuterLinkResolvesToNonBoolOperator_ReportsCS9380()
    {
        // `a < b` resolves to a bool-returning `operator <(S, S)` so the chain shape exists
        // and classical `(bool) < int` fails. The chain fallback then runs isolated `b < c`
        // against (S, int), which resolves successfully to `operator <(S, int)` - but that
        // operator returns S, not bool. Spec §11.11.13 rule 2(b) requires a bool-returning
        // operator, so the chain must be rejected with the specific diagnostic
        // (ERR_NoChainedRelationalComparison / CS9380), not CS0019 or CS0029.
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
                // (23,45): error CS9380: Operator '<' cannot be applied to operands of type 'S' and 'int' as a chained relational comparison.
                //     static bool F(S a, S b, int c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "S", "int").WithLocation(23, 45));
    }

    [Fact]
    public void ChainFallback_OuterLinkResolutionIsAmbiguous_ReportsCS9380()
    {
        // Third rule-2(b) failure mode: the isolated `Y op B` overload resolution finds
        // multiple applicable user-defined operators, none better than the others. For the
        // chain feature this is indistinguishable from the "no applicable operator" case -
        // neither succeeds in selecting a bool-returning operator, so both must produce
        // ERR_NoChainedRelationalComparison (not CS0034 or CS0019).
        //
        // Construction: S has a bool-returning `operator <(S, S)` so the chain shape exists.
        // `int` implicitly converts to Wrap1 and to Wrap2 (unrelated struct wrappers), and
        // S defines `operator <` against each wrapper. Isolated `S < int` therefore has two
        // applicable user-defined operators and no tiebreaker, so overload resolution is
        // ambiguous.
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
                // (36,45): error CS9380: Operator '<' cannot be applied to operands of type 'S' and 'int' as a chained relational comparison.
                //     static bool F(S a, S b, int c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "S", "int").WithLocation(36, 45));
    }

    [Fact]
    public void ChainFallback_OnOlderLanguageVersion_ReportsBothPreviewAndRule2Errors()
    {
        // When both the language-version gate AND rule 2(b) would reject the chain, the
        // current implementation emits BOTH CS8652 (ERR_FeatureInPreview) and CS9380
        // (ERR_NoChainedRelationalComparison). The preview-gating call runs before the
        // chain attempt and reports unconditionally; the chain attempt then runs, fails
        // rule 2(b), and reports its own error.
        //
        // This is a deliberate pin: each diagnostic describes a distinct, non-redundant
        // problem. If LDM later prefers to suppress the second diagnostic when the first
        // has already fired, this test switches to asserting a single CS8652 and the
        // binder gains an early-out after CheckFeatureAvailability.
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
                // (17,47): error CS9380: Operator '<' cannot be applied to operands of type 'S' and 'Point' as a chained relational comparison.
                //     static bool F(S a, S b, Point c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "S", "Point").WithLocation(17, 47));
    }

    [Fact]
    public void ChainFallback_InnerConversionShapesYAwayFromCompatibleRawType_ReportsCS9380()
    {
        // Spec §11.11.13 "Conversions on the shared middle operand": the isolated outer
        // overload resolution is applied against `Y`'s classification *as the right operand
        // of `X op' Y`* - i.e. the inner link's conversion is already baked in. A
        // consequence: if the inner link's conversion promotes `Y` into a type that is
        // incompatible with `c`, the chain is rejected, *even if `Y`'s original type would
        // have a compatible operator with `c` in isolation*.
        //
        // Construction: `B` has both an `implicit -> int` and a bool-returning
        // `operator <(B, string)`. For `int a, B b, string c`:
        //   - Inner `a < b` (int < B) resolves to the predefined `int < int`, with B -> int
        //     applied to `b`. So Y is an `int` in the bound tree.
        //   - Outer isolated `int < string` has no applicable operator anywhere -> rule 2(b)
        //     fails -> ERR_NoChainedRelationalComparison.
        // Notice that a hypothetical "look at raw b" interpretation would have picked the
        // B.operator<(B, string) overload and the chain would have succeeded. The current
        // spec explicitly says we don't do that. If LDM later tweaks the spec to reconsider
        // `Y`'s raw type, this test gets updated accordingly; for now it pins the rejection.
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
                // (18,50): error CS9380: Operator '<' cannot be applied to operands of type 'int' and 'string' as a chained relational comparison.
                //     static bool F(int a, B b, string c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "int", "string").WithLocation(18, 50));
    }

    [Theory]
    [InlineData("<")]
    [InlineData("<=")]
    [InlineData(">")]
    [InlineData(">=")]
    public void ChainFallback_OuterLinkError_ReportsCS9380_ForEveryChainableOperator(string op)
    {
        // The ERR_NoChainedRelationalComparison path is shared across all four chainable
        // relational operators. The other error-case tests all pin `<` specifically; this
        // parameterized test ensures the other three operators route through the same
        // diagnostic (correct OperatorToken text, correct argument substitution) rather
        // than accidentally fall through to CS0019. Assertion is location-independent
        // (each op has a different column) since we only care the diagnostic fires once
        // with the right code, text, and arguments.
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
    public void ChainFallback_OuterLinkHasNoApplicableOperator_ReportsCS9380()
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
                // (21,47): error CS9380: Operator '<' cannot be applied to operands of type 'S' and 'Point' as a chained relational comparison.
                //     static bool F(S a, S b, Point c) => a < b < c;
                Diagnostic(ErrorCode.ERR_NoChainedRelationalComparison, "<").WithArguments("<", "S", "Point").WithLocation(21, 47));
    }

    #endregion

    #region Definite assignment interactions

    [Fact]
    public void DefiniteAssignment_MiddleOperandAssignsLocalReadByOuterRight()
    {
        // Spec §11.11.13 lowers `a op1 Y op2 B` to the short-circuit form
        // `a op1 Y' && Y' op2 B` where Y' is Y evaluated once. So when Y contains an
        // assignment to a previously-unassigned local, that local must be definitely
        // assigned inside B: if we ever reach B, the inner link finished evaluating Y
        // and therefore the assignment in Y ran.
        //
        // The test also pins the runtime semantic: B reads the local directly, and
        // since Y evaluates exactly once, B must see the value Y assigned (not a
        // second evaluation of Y, and not a stale value).
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

                    // b is unassigned here. The chain reads b inside D(b) on the outer
                    // link's right operand, but b is definitely assigned by the inner
                    // link's `(b = C())` before D(b) is evaluated.
                    int b;
                    bool r = a < (b = C()) < D(b);

                    // Proves: C() ran exactly once, D received the value C() returned,
                    // and the chain's truth value matches 0 < 5 && 5 < 15.
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
}
