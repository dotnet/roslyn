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
}
