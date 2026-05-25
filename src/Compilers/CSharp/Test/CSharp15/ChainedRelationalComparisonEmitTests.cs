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
/// spec §11.11.13). Runtime and binding coverage lives in
/// <see cref="ChainedRelationalComparisonTests"/>.
/// </summary>
public sealed class ChainedRelationalComparisonEmitTests : CSharpTestBase
{
    [Fact]
    public void CanonicalBoundsCheckShape()
    {
        // Canonical `0 <= i < a.Length`.
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
        // Temp is declared at b's inner-link type (`int`); the outer widens on load
        // (`conv.i8`), keeping the IL verifiable.
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
        // Four operands: two temps (b, c) and two short-circuit branches.
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

    // Generic-math chains: `T` constrained to an interface with static-abstract
    // relational operators. Dispatch via `constrained. T` + `call`.
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

    // CoreClrOnly: NetCoreApp-referenced binary can't load on desktop.
    [ConditionalTheory(typeof(CoreClrOnly))]
    // (constraintPrefix, nullabilitySuffix)
    [InlineData("", "")]
    [InlineData("", "?")]
    [InlineData("class, ", "")]
    [InlineData("class, ", "?")]
    [InlineData("struct, ", "")]
    public void GenericConstraint_ConstrainedDispatch_UnliftedIL(string constraintPrefix, string nullabilitySuffix)
    {
        // Every row lowers to identical IL; only `struct, ILT<T>` + `T?` (lifted)
        // diverges and is pinned separately below.
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

    [ConditionalFact(typeof(CoreClrOnly))]
    public void GenericConstraint_InterfaceAndStruct_NullableT_LiftedOverConstrainedDispatch()
    {
        // `where T : struct, ILT<T>`, operands `T?`: chain + lifted-relational + constrained dispatch.
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
