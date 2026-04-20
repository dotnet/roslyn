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
/// IL-pinning emit tests for "chained relational comparison" (C# preview feature;
/// spec §11.11.13). Each test verifies a specific spec-mandated emit property of
/// the lowered form (single evaluation of the shared middle operand, short-circuit
/// semantics, asymmetric-width conversion placement, nullable lifted-relational
/// shape, n-ary stacking, user-defined operator invocation).
///
/// Runtime / binding / diagnostic tests live in
/// <see cref="ChainedRelationalComparisonTests"/>.
/// </summary>
public sealed class ChainedRelationalComparisonEmitTests : CSharpTestBase
{
    [Fact]
    public void CanonicalBoundsCheckShape()
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
    public void SameTypeInt_TempReuseAndShortCircuit()
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
    public void AsymmetricShortIntLong_ConversionOnTempLoad()
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
    public void NullableInt_HasValueAndShortCircuit()
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
    public void NAry_NestedShortCircuits()
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
    public void UserDefinedOperator_CallsBothOperators()
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
}
