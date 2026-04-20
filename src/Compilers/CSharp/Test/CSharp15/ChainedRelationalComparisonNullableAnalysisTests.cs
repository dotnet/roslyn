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
}
