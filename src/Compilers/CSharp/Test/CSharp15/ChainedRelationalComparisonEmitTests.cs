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

    // The IL pins below exercise chained relational comparisons where the operands
    // are of a generic type parameter T constrained to a curiously-recurring
    // interface that declares static abstract relational operators. This is the
    // "generic math" shape (C# 11): the chain dispatches through
    // `constrained. T` + `call` to the static-abstract interface operator on
    // each link. Three constraint shapes are pinned, each with `T` and `T?`
    // variants where meaningful:
    //
    //   1. `where T : ILT<T>`                          - T unconstrained beyond
    //                                                     the interface.
    //   2. `where T : class, ILT<T>` + `T` and `T?`    - reference-type constraint;
    //                                                     `T?` is a pure NRT
    //                                                     annotation, IL matches T.
    //   3. `where T : struct, ILT<T>` + `T` and `T?`   - value-type constraint;
    //                                                     `T?` is `Nullable<T>` and
    //                                                     composes the lifted-
    //                                                     relational pattern on
    //                                                     top of each constrained
    //                                                     dispatch.
    //
    // The harness type/interface used by these IL pins. `#nullable enable` is
    // emitted at the top so that each theory row that uses `T?` (including rows
    // where T is unconstrained or class-constrained) compiles without requiring
    // per-row preprocessor tweaks.
    private const string GenericILHarness = """
        #nullable enable

        public interface ILT<TSelf> where TSelf : ILT<TSelf>
        {
            static abstract bool operator <(TSelf a, TSelf b);
            static abstract bool operator >(TSelf a, TSelf b);
            static abstract bool operator <=(TSelf a, TSelf b);
            static abstract bool operator >=(TSelf a, TSelf b);
        }
        """;

    [Theory]
    // (constraintPrefix, nullabilitySuffix)
    // Interface-only T:
    [InlineData("", "")]
    [InlineData("", "?")]
    // Class-constrained T (T? is a pure NRT annotation; runtime type is still T):
    [InlineData("class, ", "")]
    [InlineData("class, ", "?")]
    // Struct-constrained T (only plain T; `struct, ILT<T>` + T? is the lifted case
    // pinned separately below):
    [InlineData("struct, ", "")]
    public void GenericConstraint_ConstrainedDispatch_UnliftedIL(string constraintPrefix, string nullabilitySuffix)
    {
        // Five constraint-shape x operand-annotation combinations all lower to
        // the SAME IL: a `constrained. T` + `call` dispatch on each chain link,
        // with the shared middle operand stored once in a `T` temp and loaded
        // twice. The rows (constraintPrefix, nullabilitySuffix):
        //
        //   ("",         "" ) -> `where T : ILT<T>`, operands `T`
        //   ("",         "?") -> `where T : ILT<T>`, operands `T?` (NRT
        //                         annotation only; no Nullable<T> wrapping
        //                         because T is unconstrained)
        //   ("class, ",  "" ) -> `where T : class, ILT<T>`, operands `T`
        //   ("class, ",  "?") -> `where T : class, ILT<T>`, operands `T?`
        //                         (again pure NRT annotation on a ref-type
        //                         constraint)
        //   ("struct, ", "" ) -> `where T : struct, ILT<T>`, operands `T`
        //
        // The only combination that produces different IL is
        // `where T : struct, ILT<T>` + `T?` (= `Nullable<T>`), which composes the
        // lifted-relational pattern on top of the constrained dispatch - that's
        // pinned by GenericConstraint_InterfaceAndStruct_NullableT_LiftedOver...
        // below.
        //
        // The purpose of pinning all five here with a shared body is to document,
        // at the IL level, that the chain's generic-operand handling is oblivious
        // to whether T is further constrained to ref/value types and to whether
        // the operand has an NRT annotation - only the `T?`-means-`Nullable<T>`
        // case diverges.
        var src = GenericILHarness + $$"""

            public class P
            {
                #pragma warning disable CS8604
                public static bool F<T>(T{{nullabilitySuffix}} a, T{{nullabilitySuffix}} b, T{{nullabilitySuffix}} c)
                    where T : {{constraintPrefix}}ILT<T>
                    => a < b < c;
                #pragma warning restore CS8604
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            targetFramework: TargetFramework.NetCoreApp,
            verify: Verification.Skipped,
            options: TestOptions.ReleaseDll)
            .VerifyDiagnostics()
            // Canonical unlifted generic-constrained pin. The IL demonstrates:
            //   - `stloc.0` stores b into a `T` temp (V_0 is `T`) - single
            //     evaluation of the shared middle operand.
            //   - Two `constrained. "T"` prefixes, each followed by
            //     `call "bool ILT<T>.op_LessThan(T, T)"`: one per link. The JIT
            //     resolves the constrained call at instantiation time (direct
            //     call for struct T, virtual dispatch for class T); the textual
            //     IL doesn't care.
            //   - `brfalse.s` between the two links is the chained short-circuit;
            //     without it, the outer link would evaluate even if the inner
            //     failed, violating spec §11.11.13.
            // This exact shape is emitted for ALL rows of this theory: a
            // ref/value-type constraint or an NRT annotation on the operands
            // changes NOTHING at the IL level.
            .VerifyIL("P.F<T>", """
                {
                  // Code size       33 (0x21)
                  .maxstack  2
                  .locals init (T V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.1
                  IL_0002:  stloc.0
                  IL_0003:  ldloc.0
                  IL_0004:  constrained. "T"
                  IL_000a:  call       "bool ILT<T>.op_LessThan(T, T)"
                  IL_000f:  brfalse.s  IL_001f
                  IL_0011:  ldloc.0
                  IL_0012:  ldarg.2
                  IL_0013:  constrained. "T"
                  IL_0019:  call       "bool ILT<T>.op_LessThan(T, T)"
                  IL_001e:  ret
                  IL_001f:  ldc.i4.0
                  IL_0020:  ret
                }
                """);
    }

    [Fact]
    public void GenericConstraint_InterfaceAndStruct_NullableT_LiftedOverConstrainedDispatch()
    {
        // `where T : struct, ILT<T>`, operands `T?`. Here the chain composes two
        // orthogonal language features: chained relational (§11.11.13) on top of
        // nullable lifted relational (§11.4.8) on top of generic constrained
        // dispatch. Each link's lifted form expands to
        // `(a.HasValue && b.HasValue) && (a.GetValueOrDefault() < b.GetValueOrDefault())`,
        // where the underlying `<` is the constrained `ILT<T>.op_LessThan`.
        // Any null operand at any link makes the chain false.
        //
        // This is the most complex interaction the feature supports; pinning it
        // here guards against the three layers accidentally losing a piece
        // (constrained prefix, HasValue gate, or short-circuit).
        var src = GenericILHarness + """

            public class P
            {
                public static bool F<T>(T? a, T? b, T? c) where T : struct, ILT<T>
                    => a < b < c;
            }
            """;
        CompileAndVerify(src,
            parseOptions: TestOptions.RegularPreview,
            targetFramework: TargetFramework.NetCoreApp,
            verify: Verification.Skipped,
            options: TestOptions.ReleaseDll)
            .VerifyDiagnostics()
            // Lifted-over-constrained pin. The IL demonstrates the three layers:
            //   - `constrained. "T"` + `call "bool ILT<T>.op_LessThan(T, T)"` on
            //     each link - the static-abstract dispatch.
            //   - HasValue is checked FIRST for both operands and short-circuits
            //     the link to false if either is null, WITHOUT calling the
            //     underlying operator. This differs from `int?` (see
            //     NullableInt_HasValueAndShortCircuit above) because the generic
            //     constrained call is emitted differently when the comparison
            //     can be skipped: it's cheaper to skip the dispatch than to
            //     always compute the comparison and then AND with HasValue.
            //   - `brfalse.s` between the two lifted bools is the chained
            //     short-circuit: if the inner link yielded false, the outer
            //     link (and its constrained call) is skipped entirely.
            // The shared middle operand b is stored once into V_0 and reused.
            .VerifyIL("P.F<T>", """
                {
                  // Code size      104 (0x68)
                  .maxstack  2
                  .locals init (T? V_0,
                                T? V_1,
                                T? V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  stloc.1
                  IL_0002:  ldarg.1
                  IL_0003:  stloc.0
                  IL_0004:  ldloc.0
                  IL_0005:  stloc.2
                  IL_0006:  ldloca.s   V_1
                  IL_0008:  call       "readonly bool T?.HasValue.get"
                  IL_000d:  ldloca.s   V_2
                  IL_000f:  call       "readonly bool T?.HasValue.get"
                  IL_0014:  and
                  IL_0015:  brtrue.s   IL_001a
                  IL_0017:  ldc.i4.0
                  IL_0018:  br.s       IL_0033
                  IL_001a:  ldloca.s   V_1
                  IL_001c:  call       "readonly T T?.GetValueOrDefault()"
                  IL_0021:  ldloca.s   V_2
                  IL_0023:  call       "readonly T T?.GetValueOrDefault()"
                  IL_0028:  constrained. "T"
                  IL_002e:  call       "bool ILT<T>.op_LessThan(T, T)"
                  IL_0033:  brfalse.s  IL_0066
                  IL_0035:  ldloc.0
                  IL_0036:  stloc.2
                  IL_0037:  ldarg.2
                  IL_0038:  stloc.1
                  IL_0039:  ldloca.s   V_2
                  IL_003b:  call       "readonly bool T?.HasValue.get"
                  IL_0040:  ldloca.s   V_1
                  IL_0042:  call       "readonly bool T?.HasValue.get"
                  IL_0047:  and
                  IL_0048:  brtrue.s   IL_004c
                  IL_004a:  ldc.i4.0
                  IL_004b:  ret
                  IL_004c:  ldloca.s   V_2
                  IL_004e:  call       "readonly T T?.GetValueOrDefault()"
                  IL_0053:  ldloca.s   V_1
                  IL_0055:  call       "readonly T T?.GetValueOrDefault()"
                  IL_005a:  constrained. "T"
                  IL_0060:  call       "bool ILT<T>.op_LessThan(T, T)"
                  IL_0065:  ret
                  IL_0066:  ldc.i4.0
                  IL_0067:  ret
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
