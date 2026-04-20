// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

/// <summary>
/// Nullable-flow-analysis tests for "chained relational comparison"
/// (C# preview feature; spec §11.11.13). Two orthogonal concerns exercised here:
///
///   1. Assignments inside the chain (<c>a &lt; (b = C()) &lt; D(b)</c>) must update
///      <c>b</c>'s flow state for subsequent reads within the chain and inside the
///      when-true branch. This follows naturally from <c>NullableWalker</c>'s normal
///      <c>VisitAssignmentOperator</c> handling; no chained-specific logic is needed.
///
///   2. Lifted chain links (<c>a &lt; b &lt; c</c> where any operand is a nullable
///      value type) must apply the same "when-true implies both operands non-null"
///      refinement that classical lifted <c>&lt;</c> / <c>&lt;=</c> / <c>&gt;</c> /
///      <c>&gt;=</c> gets. Without this, the chain's then-branch leaves the outer
///      operands at their pre-chain nullability, producing spurious CS8629 /
///      CS8602 warnings.
/// </summary>
public sealed class ChainedRelationalComparisonNullableAnalysisTests : CSharpTestBase
{
    [Fact]
    public void AssignmentInsideChain_MiddleOperandAssignedNonNull_NoWarnings()
    {
        // `int? b = null;` starts as maybe-null. `(b = C())` assigns a non-null int to b
        // as part of the chain's middle operand. NullableWalker's normal assignment-visit
        // updates b's flow state as it descends the inner link's right operand, so
        // `D(b.Value)` in the outer right sees b as non-null. Inside the `if` body,
        // `b.Value` is still non-null because the inner-link's when-true state flows
        // through the chain's when-true.
        var src = """
            #nullable enable
            class P
            {
                static int C() => 5;
                static int D(int x) => x;

                static int Test()
                {
                    int a = 0;
                    int? b = null;
                    int d = 100;

                    if (a < (b = C()) < D(b.Value))
                    {
                        return b.Value;
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (11,13): warning CS0219: The variable 'd' is assigned but its value is never used
                //         int d = 100;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "d").WithArguments("d").WithLocation(11, 13));
    }

    [Fact]
    public void LiftedChain_WhenTrueRefinesAllOperandsToNonNull()
    {
        // Classical `if (a < b)` with a (int?) maybe-null b refines b to non-null in the
        // then-branch via the lifted-relational refinement at
        // `ReinferAndVisitBinaryOperator`. Chained `if (a < b < c)` must apply the same
        // refinement at the outer link too - otherwise b is refined by the inner link
        // alone and c is never refined, leaving CS8629 on `c.Value` inside the block.
        var src = """
            #nullable enable
            class P
            {
                static int? Foo() => 5;

                static int Test()
                {
                    int a = 0;
                    int? b = Foo();   // maybe null
                    int? c = Foo();   // maybe null

                    if (a < b < c)
                    {
                        // Chain true => b non-null (inner) AND c non-null (outer).
                        return b.Value + c.Value;
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_NAry_WhenTrueRefinesAllOperandsToNonNull()
    {
        // Four-operand chain: a < b < c < d with every operand int?. The when-true
        // refinement must apply at every link, not just the outermost. This is the
        // stack-walker-friendly shape; if the refinement only fired on the outer-most
        // link, `c.Value` below would still warn.
        var src = """
            #nullable enable
            class P
            {
                static int? Foo() => 5;

                static int Test()
                {
                    int? a = Foo(), b = Foo(), c = Foo(), d = Foo();
                    if (a < b < c < d)
                    {
                        return a.Value + b.Value + c.Value + d.Value;
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_NullConditionalAccessOnOuterRight_RefinesReceiver()
    {
        // The outer right operand is `s?.Length`, which is non-null iff s is non-null.
        // When the chain is true, the lifted outer link implies `s?.Length` is non-null,
        // which in turn implies `s` is non-null. NullableWalker's slot machinery for
        // `?.` access does this transitive refinement when fed through
        // SplitAndLearnFromNonNullTest, so `s.Length` inside the then-branch should not
        // warn.
        var src = """
            #nullable enable
            class P
            {
                static int Test(int[] arr, string? s)
                {
                    if (0 < arr.Length < s?.Length)
                    {
                        return s.Length;
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_WhenFalseBranch_DoesNotOverRefine()
    {
        // The refinement is applied to the chain's when-true branch only. The
        // else-branch (`when-false`) sees the chain short-circuit, where b could have
        // been null to cause the inner link to return false (making the chain false),
        // OR c could have been null / out-of-range. So b and c stay maybe-null in the
        // else-branch. Reading b.Value / c.Value there must warn.
        var src = """
            #nullable enable
            class P
            {
                static int? Foo() => 5;

                static int Test()
                {
                    int a = 0;
                    int? b = Foo();
                    int? c = Foo();

                    if (a < b < c)
                    {
                        return 0;
                    }
                    return b.Value + c.Value;  // warnings expected
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (16,16): warning CS8629: Nullable value type may be null.
                //         return b.Value + c.Value;  // warnings expected
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "b").WithLocation(16, 16),
                // (16,26): warning CS8629: Nullable value type may be null.
                //         return b.Value + c.Value;  // warnings expected
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "c").WithLocation(16, 26));
    }

    [Fact]
    public void LiftedChain_MixedNullableAndNonNullable_RefinesNullableOperands()
    {
        // Only the nullable operands need (and get) refinement. Non-nullable operands
        // are already non-null before the chain. Pin that the refinement handles the
        // mixed shape cleanly: b is int?, a and c are non-nullable int.
        var src = """
            #nullable enable
            class P
            {
                static int? Foo() => 5;

                static int Test()
                {
                    int a = 0;
                    int? b = Foo();
                    int c = 100;

                    if (a < b < c)
                    {
                        return b.Value;  // refined to non-null
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void AssignmentInsideChain_ReferenceType_NoWarningOnNonNullParameterCall()
    {
        // Reference-type analog with a user-defined relational operator on a wrapper
        // class. `D(b)` takes a non-null `Wrapper`; `b` starts out nullable but is
        // reassigned non-null inside the chain via `(b = C())`. NullableWalker's
        // assignment tracking flows the non-null state through the chain, so `D(b)`
        // does not warn (CS8604).
        var src = """
            #nullable enable
            class Wrapper
            {
                public int V;
                public Wrapper(int v) { V = v; }
                public static bool operator <(Wrapper a, Wrapper b) => a.V < b.V;
                public static bool operator >(Wrapper a, Wrapper b) => a.V > b.V;
            }

            class P
            {
                static Wrapper C() => new Wrapper(5);
                static int D(Wrapper w) => w.V;

                static int Test()
                {
                    Wrapper a = new Wrapper(0);
                    Wrapper? b = null;

                    if (a < (b = C()) < new Wrapper(D(b)))
                    {
                        return D(b);  // b is non-null here
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Theory]
    // Every chainable relational operator must apply the when-true refinement, not just `<`.
    // The refinement in NullableWalker is gated purely on `OperatorKind.IsLifted()`, so if
    // the operator-shape check accidentally narrowed to LessThan only, `<=`/`>`/`>=` rows
    // would emit CS8629 on `b.Value` inside the block.
    [InlineData("<")]
    [InlineData("<=")]
    [InlineData(">")]
    [InlineData(">=")]
    public void LiftedChain_AllRelationalOperators_RefineOperandsOnWhenTrue(string op)
    {
        // For `>`/`>=` we flip the operand magnitudes so the chain is still sometimes true.
        // The values a=100, b=50, c=0 give `100 > 50 > 0 == true` and the mirror for `<`.
        var (aVal, cVal) = op.StartsWith(">") ? ("100", "0") : ("0", "100");
        var src = $$"""
            #nullable enable
            class P
            {
                static int? Foo() => 50;

                static int Test()
                {
                    int a = {{aVal}};
                    int? b = Foo();
                    int c = {{cVal}};

                    if (a {{op}} b {{op}} c)
                    {
                        return b.Value;
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_MixedDirection_RefineOperandsOnWhenTrue()
    {
        // `a < b > c` with nullable middle is a valid chain per spec §11.11.13 (mixed-
        // direction is allowed because each link is a bool-returning lifted relational).
        // Refinement must apply to b even when the two links have different directions.
        var src = """
            #nullable enable
            class P
            {
                static int? Foo() => 50;

                static int Test()
                {
                    int a = 0;
                    int? b = Foo();
                    int c = 10;

                    if (a < b > c)
                    {
                        return b.Value;  // refined to non-null in when-true
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_SixOperands_RefinementAtEveryLink()
    {
        // 6-operand chain stresses the refinement firing in every stack-walker iteration.
        // If the refinement only fired for the outermost link (or only for the inner
        // link), some of these `.Value` reads would warn.
        var src = """
            #nullable enable
            class P
            {
                static int? Foo(int v) => v;

                static int Test()
                {
                    int? a = Foo(0), b = Foo(1), c = Foo(2), d = Foo(3), e = Foo(4), f = Foo(5);

                    if (a < b < c < d < e < f)
                    {
                        return a.Value + b.Value + c.Value + d.Value + e.Value + f.Value;
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_NullCoalescingInMiddleOperand_RefineReceiver()
    {
        // The middle operand is `x ?? y`. After `if (a < (x ?? y) < c)`, the evaluated
        // result is non-null on when-true (lifted relational), and since `??` returns
        // non-null iff at least one side was - and classical NullableWalker tracking
        // already handles `??` correctly - the relevant receiver states propagate.
        var src = """
            #nullable enable
            class P
            {
                static int? Foo() => 50;

                static int Test(int? x)
                {
                    int? y = Foo();
                    int a = 0;
                    int c = 100;

                    if (a < (x ?? y) < c)
                    {
                        // The `??` expression is non-null when-true, but that does not
                        // necessarily refine x or y individually. The assertion is just
                        // that the chain compiles without spurious nullable warnings on
                        // the `??` subexpression itself or on the chain's result.
                        return 0;
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_NullCoalescingOnInnerLeft_RefineReceiver()
    {
        // `?? ` in the inner-left position of the chain.
        var src = """
            #nullable enable
            class P
            {
                static int? Foo() => 50;

                static int Test(int? x)
                {
                    int? y = Foo();
                    int? b = 50;
                    int c = 100;

                    if ((x ?? y) < b < c)
                    {
                        return b.Value;  // b refined by both links; chain works.
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_NullConditionalAccessInMiddleOperand_RefinesReceiver()
    {
        // `?.` in the MIDDLE operand position. Classical NullableWalker refinement for the
        // outer lifted link sees `arr?.Length` non-null on when-true, which transitively
        // implies `arr` is non-null.
        var src = """
            #nullable enable
            class P
            {
                static int Test(int[]? arr)
                {
                    if (0 < arr?.Length < 100)
                    {
                        return arr.Length;  // arr refined to non-null in when-true.
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_NullConditionalAccessOnInnerLeft_RefinesReceiver()
    {
        // `?.` in the INNER-LEFT position.
        var src = """
            #nullable enable
            class P
            {
                static int Test(int[]? arr)
                {
                    int? b = 50;
                    int c = 100;

                    if (arr?.Length < b < c)
                    {
                        return arr.Length + b.Value;  // both receivers refined.
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void Chain_OutVarInMiddleOperand_LaterOperandSeesAssignment()
    {
        // `a < M(out var x) < D(x)` is the definite-assignment analog of our existing
        // `(b = C())` test, but using `out` rather than expression-assignment. NullableWalker
        // must see x as assigned (and at its declared non-null state) by the time D(x) is
        // evaluated - same as the classical `(a < M(out var x)) && (M(out var x) < D(x))`
        // hand-written form.
        var src = """
            #nullable enable
            class P
            {
                static int M(out int x) { x = 42; return x; }
                static int D(int x) => x;

                static int Test()
                {
                    int a = 0;

                    if (a < M(out var x) < D(x))
                    {
                        return x;
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_OperandsWithoutNullableSlots_DoesNotThrow()
    {
        // Method-call results have no nullable slot (they're temporary values).
        // GetSlotsToMarkAsNotNullable yields an empty builder, and
        // MarkSlotsAsNotNull is skipped. Pin that the refinement path is robust
        // when no operand contributes a slot.
        var src = """
            #nullable enable
            class P
            {
                static int? Foo() => 50;
                static int? Bar() => 75;

                static bool Test()
                {
                    return 0 < Foo() < Bar();
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void Chain_AllNonNullableOperands_NoLiftedPath()
    {
        // Chain with all non-nullable int operands. OperatorKind.IsLifted() is false,
        // so the refinement block is skipped and the stock
        // AfterRightChildOfBinaryLogicalOperatorHasBeenVisited handles the state
        // transition. Pins that the non-lifted chained path still works correctly
        // (no refinement applied, but no crash / no spurious diagnostics either).
        var src = """
            #nullable enable
            class P
            {
                static int Test(int a, int b, int c)
                {
                    if (a < b < c)
                    {
                        return b;
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_OperandsAreFieldsAndProperties_Refined()
    {
        // Nullable operand shapes other than locals: fields and auto-properties. Each
        // contributes a slot; the refinement should fire at the relevant positions.
        var src = """
            #nullable enable
            class P
            {
                static int? _b;
                static int? B { get; set; }

                static int Test()
                {
                    _b = 50;
                    B = 75;
                    int a = 0;
                    int c = 100;

                    if (a < _b < c)
                    {
                        _ = _b.Value;  // field refined
                    }

                    if (a < B < c)
                    {
                        _ = B.Value;   // property refined
                    }

                    return 0;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_OperandIsParameter_Refined()
    {
        // Parameter operand in the middle position.
        var src = """
            #nullable enable
            class P
            {
                static int Test(int a, int? b, int c)
                {
                    if (a < b < c)
                    {
                        return b.Value;  // parameter refined to non-null.
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_InsideLambda_RefinementFires()
    {
        // Chain inside a lambda captures nullable locals. Refinement inside the lambda
        // body should behave identically to refinement at method scope.
        var src = """
            #nullable enable
            using System;

            class P
            {
                static int? Foo() => 50;

                static int Test()
                {
                    int a = 0;
                    int? b = Foo();
                    int c = 100;

                    Func<int> f = () =>
                    {
                        if (a < b < c)
                        {
                            return b.Value;
                        }
                        return -1;
                    };

                    return f();
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void LiftedChain_InsideLocalFunction_RefinementFires()
    {
        // Same as the lambda case but for a local function.
        var src = """
            #nullable enable
            class P
            {
                static int? Foo() => 50;

                static int Test()
                {
                    int a = 0;
                    int? b = Foo();
                    int c = 100;

                    int Local()
                    {
                        if (a < b < c)
                        {
                            return b.Value;
                        }
                        return -1;
                    }

                    return Local();
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void UserDefinedChainedRelational_ReturnStateIsAlwaysNotNullBool()
    {
        // Spec §11.11.13 rule 2(b) requires the chained outer operator to return bool,
        // and bool is never nullable. So InferResultNullability's result for a chained
        // outer node is always NotNull bool, regardless of what TypeWithState is passed
        // in as leftType. Pin the observable: a user-defined chained operator's result
        // can be used as `bool` without any nullable warning, and annotations like
        // [NotNullIfNotNull] have no effect on the chain's result state (since bool
        // is always non-null anyway).
        //
        // This test guards against a future InferResultNullability change that might
        // start leaking nullability from the wrong operand type into the chain's result.
        var src = """
            #nullable enable
            struct Wrapper
            {
                public int V;
                public Wrapper(int v) { V = v; }

                // User-defined `<` / `>` on Wrapper. Both return bool per spec rule 2(b)
                // requirement for chainable relational operators.
                public static bool operator <(Wrapper a, Wrapper b) => a.V < b.V;
                public static bool operator >(Wrapper a, Wrapper b) => a.V > b.V;
            }

            class P
            {
                static bool Test()
                {
                    Wrapper a = new Wrapper(0);
                    Wrapper b = new Wrapper(50);
                    Wrapper c = new Wrapper(100);
                    // The chain binds to user-defined `<`/`<`. Result is `bool`, not `bool?`.
                    bool r = a < b < c;
                    return r;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }
}
