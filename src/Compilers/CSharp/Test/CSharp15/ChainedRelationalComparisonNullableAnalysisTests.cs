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
/// (C# preview feature; spec §11.11.13). A lifted chain link must apply the same
/// when-true non-null refinement as a classical lifted <c>&lt;</c>/<c>&lt;=</c>/<c>&gt;</c>/<c>&gt;=</c>.
/// </summary>
public sealed class ChainedRelationalComparisonNullableAnalysisTests : CSharpTestBase
{
    [Fact]
    public void AssignmentInsideChain_MiddleOperandAssignedNonNull_NoWarnings()
    {
        // Assignment inside the middle operand refines `b` for subsequent reads.
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
        // Refinement must fire at every link, not just the outermost.
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
        // `s?.Length` non-null on when-true transitively refines `s` to non-null.
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
        // Refinement is when-true only; the else-branch leaves operands maybe-null.
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
                    return b.Value + c.Value;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (16,16): warning CS8629: Nullable value type may be null.
                //         return b.Value + c.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "b").WithLocation(16, 16),
                // (16,26): warning CS8629: Nullable value type may be null.
                //         return b.Value + c.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "c").WithLocation(16, 26));
    }

    [Fact]
    public void LiftedChain_MixedNullableAndNonNullable_RefinesNullableOperands()
    {
        // Mixed `int, int?, int`: middle nullable refines, ends are already non-null.
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
                        return b.Value;
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
        // Reference-type analog: `(b = C())` refines `b` for later non-null reads.
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
                        return D(b);
                    }
                    return -1;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Theory]
    [InlineData("<")]
    [InlineData("<=")]
    [InlineData(">")]
    [InlineData(">=")]
    public void LiftedChain_AllRelationalOperators_RefineOperandsOnWhenTrue(string op)
    {
        // `>`/`>=` rows flip the operand magnitudes so the chain is sometimes true.
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
        // Mixed-direction `a < b > c` still refines b on when-true.
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
        // 6-operand chain: refinement must fire at every link.
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
        // `x ?? y` middle. Chain compiles without spurious warnings.
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
        // `x ?? y` in the inner-left position.
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
                        return b.Value;
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
        // `?.` in the middle operand position.
        var src = """
            #nullable enable
            class P
            {
                static int Test(int[]? arr)
                {
                    if (0 < arr?.Length < 100)
                    {
                        return arr.Length;
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
        // `?.` in the inner-left position.
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
                        return arr.Length + b.Value;
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
        // `out var x` in the middle: `D(x)` in the outer-right sees x definitely assigned.
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
        // No operand contributes a nullable slot (all are method-call results).
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
        // Non-lifted chain: refinement is a no-op, but no crash or spurious diagnostics.
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
        // Field and auto-property operands both refine.
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
                        _ = _b.Value;
                    }

                    if (a < B < c)
                    {
                        _ = B.Value;
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
        var src = """
            #nullable enable
            class P
            {
                static int Test(int a, int? b, int c)
                {
                    if (a < b < c)
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
    public void LiftedChain_InsideLambda_RefinementFires()
    {
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
        // A chained outer always returns non-null bool; no nullable warning on the result.
        var src = """
            #nullable enable
            struct Wrapper
            {
                public int V;
                public Wrapper(int v) { V = v; }

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
                    bool r = a < b < c;
                    return r;
                }
            }
            """;
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }
}
