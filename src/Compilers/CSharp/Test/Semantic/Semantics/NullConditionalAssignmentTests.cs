// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class NullConditionalAssignmentTests : SemanticModelTestBase
    {
        [Fact]
        public void LangVersion_01()
        {
            var source = """
                class C
                {
                    string f;
                    static void M(C c)
                    {
                        c?.f = "a";
                    }
                }
                """;

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular13);
            comp.VerifyEmitDiagnostics(
                // (3,12): warning CS0414: The field 'C.f' is assigned but its value is never used
                //     string f;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "f").WithArguments("C.f").WithLocation(3, 12),
                // (6,11): error CS8652: The feature 'null-conditional assignment' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         c?.f = "a";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @".f = ""a""").WithArguments("null-conditional assignment").WithLocation(6, 11));

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,12): warning CS0414: The field 'C.f' is assigned but its value is never used
                //     string f;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "f").WithArguments("C.f").WithLocation(3, 12));
        }


        [Fact]
        public void LangVersion_02()
        {
            // The only thing we want to diagnose is a member binding expression as the LHS of any assignment.
            // Nested assignments within conditional access args, etc. have always been allowed.
            var source = """
                class C
                {
                    public string this[string s] { get => s; set { } }

                    static void M(C c)
                    {
                        string s = "a";
                        _ = c?[s = "b"];
                        c?[s = "b"] = "c"; // 1
                    }
                }
                """;

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular13);
            comp.VerifyEmitDiagnostics(
                // (7,16): warning CS0219: The variable 's' is assigned but its value is never used
                //         string s = "a";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s").WithLocation(7, 16),
                // (9,11): error CS8652: The feature 'null-conditional assignment' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         c?[s = "b"] = "c"; // 1
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"[s = ""b""] = ""c""").WithArguments("null-conditional assignment").WithLocation(9, 11));

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,16): warning CS0219: The variable 's' is assigned but its value is never used
                //         string s = "a";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s").WithLocation(7, 16));
        }

        [Fact]
        public void FieldAccessAssignment_01()
        {
            var source = """
                using System;

                class C
                {
                    int f;

                    static void Main()
                    {
                        var c = new C();
                        M1(c, 1);
                        M2(c, 2);
                        c = null;
                        M3(c, 3);
                        M4(c, 4);
                    }

                    static void M1(C c, int i)
                    {
                        c?.f = i;
                        Console.Write(c.f);
                    }

                    static void M2(C c, int i)
                    {
                        Console.Write(c?.f = i);
                    }

                    static void M3(C c, int i)
                    {
                        c?.f = i;
                        Console.Write(c?.f is null);
                    }

                    static void M4(C c, int i)
                    {
                        Console.Write((c?.f = i) is null);
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "12TrueTrue");
            verifier.VerifyIL("C.M1", """
                {
                  // Code size       22 (0x16)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  brfalse.s  IL_000a
                  IL_0003:  ldarg.0
                  IL_0004:  ldarg.1
                  IL_0005:  stfld      "int C.f"
                  IL_000a:  ldarg.0
                  IL_000b:  ldfld      "int C.f"
                  IL_0010:  call       "void System.Console.Write(int)"
                  IL_0015:  ret
                }
                """);

            verifier.VerifyIL("C.M2", """
                {
                  // Code size       40 (0x28)
                  .maxstack  3
                  .locals init (int? V_0,
                                int V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  brtrue.s   IL_000e
                  IL_0003:  ldloca.s   V_0
                  IL_0005:  initobj    "int?"
                  IL_000b:  ldloc.0
                  IL_000c:  br.s       IL_001d
                  IL_000e:  ldarg.0
                  IL_000f:  ldarg.1
                  IL_0010:  dup
                  IL_0011:  stloc.1
                  IL_0012:  stfld      "int C.f"
                  IL_0017:  ldloc.1
                  IL_0018:  newobj     "int?..ctor(int)"
                  IL_001d:  box        "int?"
                  IL_0022:  call       "void System.Console.Write(object)"
                  IL_0027:  ret
                }
                """);

            verifier.VerifyIL("C.M3", """
                {
                  // Code size       52 (0x34)
                  .maxstack  2
                  .locals init (int? V_0,
                                int? V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  brfalse.s  IL_000a
                  IL_0003:  ldarg.0
                  IL_0004:  ldarg.1
                  IL_0005:  stfld      "int C.f"
                  IL_000a:  ldarg.0
                  IL_000b:  brtrue.s   IL_0018
                  IL_000d:  ldloca.s   V_1
                  IL_000f:  initobj    "int?"
                  IL_0015:  ldloc.1
                  IL_0016:  br.s       IL_0023
                  IL_0018:  ldarg.0
                  IL_0019:  ldfld      "int C.f"
                  IL_001e:  newobj     "int?..ctor(int)"
                  IL_0023:  stloc.0
                  IL_0024:  ldloca.s   V_0
                  IL_0026:  call       "bool int?.HasValue.get"
                  IL_002b:  ldc.i4.0
                  IL_002c:  ceq
                  IL_002e:  call       "void System.Console.Write(bool)"
                  IL_0033:  ret
                }
                """);

            verifier.VerifyIL("C.M4", """
                {
                  // Code size       46 (0x2e)
                  .maxstack  3
                  .locals init (int? V_0,
                                int? V_1,
                                int V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  brtrue.s   IL_000e
                  IL_0003:  ldloca.s   V_1
                  IL_0005:  initobj    "int?"
                  IL_000b:  ldloc.1
                  IL_000c:  br.s       IL_001d
                  IL_000e:  ldarg.0
                  IL_000f:  ldarg.1
                  IL_0010:  dup
                  IL_0011:  stloc.2
                  IL_0012:  stfld      "int C.f"
                  IL_0017:  ldloc.2
                  IL_0018:  newobj     "int?..ctor(int)"
                  IL_001d:  stloc.0
                  IL_001e:  ldloca.s   V_0
                  IL_0020:  call       "bool int?.HasValue.get"
                  IL_0025:  ldc.i4.0
                  IL_0026:  ceq
                  IL_0028:  call       "void System.Console.Write(bool)"
                  IL_002d:  ret
                }
                """);

            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void FieldAccessAssignment_StructReceiver_01()
        {
            // NB: assignment of a 'readonly' setter is permitted even when property access receiver is not a variable
            var source = """
                using System;

                struct S
                {
                    int f;
                    int P { get; set; }
                    readonly int RP { get => 0; set { } }

                    static void M1(S? s)
                    {
                        s?.f = 1; // 1
                        s?.P = 2; // 2
                        s?.RP = 2;
                    }

                    static void M2(S? s)
                    {
                        Console.Write(s?.f = 4); // 3
                        Console.Write(s?.P = 5); // 4
                        Console.Write(s?.RP = 6);
                    }

                    static void M2(S s)
                    {
                        s?.f = 7; // 5
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (11,11): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         s?.f = 1; // 1
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, ".f").WithLocation(11, 11),
                // (12,11): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         s?.P = 2; // 2
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, ".P").WithLocation(12, 11),
                // (18,25): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         Console.Write(s?.f = 4); // 3
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, ".f").WithLocation(18, 25),
                // (19,25): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         Console.Write(s?.P = 5); // 4
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, ".P").WithLocation(19, 25),
                // (25,10): error CS0023: Operator '?' cannot be applied to operand of type 'S'
                //         s?.f = 7; // 5
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "S").WithLocation(25, 10));
        }

        [Fact]
        public void FieldAccessAssignment_StructReceiver_02()
        {
            // NB: assignment of a 'readonly' setter is permitted even when property access receiver is not a variable
            var source = """
                using System;

                class C
                {
                    public int F;
                }

                struct S
                {
                    C c;

                    readonly int RP { get => c.F; set => c.F = value; }

                    static void Main()
                    {
                        M1(new S() { c = new C() });
                        M2(new S() { c = new C() });
                        M1(null);
                        M2(null);
                    }

                    static void M1(S? s)
                    {
                        s?.RP = 1;
                        Console.Write(s?.RP ?? 3);
                    }

                    static void M2(S? s)
                    {
                        Console.Write((s?.RP = 2) ?? 4);
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "1234");

            verifier.VerifyIL("S.M1", """
                {
                  // Code size       58 (0x3a)
                  .maxstack  2
                  .locals init (S V_0)
                  IL_0000:  ldarga.s   V_0
                  IL_0002:  call       "bool S?.HasValue.get"
                  IL_0007:  brfalse.s  IL_0019
                  IL_0009:  ldarga.s   V_0
                  IL_000b:  call       "S S?.GetValueOrDefault()"
                  IL_0010:  stloc.0
                  IL_0011:  ldloca.s   V_0
                  IL_0013:  ldc.i4.1
                  IL_0014:  call       "readonly void S.RP.set"
                  IL_0019:  ldarga.s   V_0
                  IL_001b:  call       "bool S?.HasValue.get"
                  IL_0020:  brtrue.s   IL_0025
                  IL_0022:  ldc.i4.3
                  IL_0023:  br.s       IL_0034
                  IL_0025:  ldarga.s   V_0
                  IL_0027:  call       "S S?.GetValueOrDefault()"
                  IL_002c:  stloc.0
                  IL_002d:  ldloca.s   V_0
                  IL_002f:  call       "readonly int S.RP.get"
                  IL_0034:  call       "void System.Console.Write(int)"
                  IL_0039:  ret
                }
                """);

            verifier.VerifyIL("S.M2", """
                {
                  // Code size       37 (0x25)
                  .maxstack  3
                  .locals init (int V_0,
                                S V_1)
                  IL_0000:  ldarga.s   V_0
                  IL_0002:  call       "bool S?.HasValue.get"
                  IL_0007:  brtrue.s   IL_000c
                  IL_0009:  ldc.i4.4
                  IL_000a:  br.s       IL_001f
                  IL_000c:  ldarga.s   V_0
                  IL_000e:  call       "S S?.GetValueOrDefault()"
                  IL_0013:  stloc.1
                  IL_0014:  ldloca.s   V_1
                  IL_0016:  ldc.i4.2
                  IL_0017:  dup
                  IL_0018:  stloc.0
                  IL_0019:  call       "readonly void S.RP.set"
                  IL_001e:  ldloc.0
                  IL_001f:  call       "void System.Console.Write(int)"
                  IL_0024:  ret
                }
                """);

            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructionLeft()
        {
            var source = """
                using System;

                class C
                {
                    int F;

                    static void M1(C c1, C c2)
                    {
                        (c1?.F, c2?.F) = (1, 2); // 1
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,9): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 0
                //     int F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "0").WithLocation(5, 9),
                // (9,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (c1?.F, c2?.F) = (1, 2); // 1
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "c1?.F").WithLocation(9, 10),
                // (9,17): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (c1?.F, c2?.F) = (1, 2); // 1
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "c2?.F").WithLocation(9, 17));
        }

        [Fact]
        public void RefAssignment_01()
        {
            var source = """
                using System;

                ref struct RS
                {
                    ref int RF;

                    static void M1()
                    {
                        int i = 0;
                        var rs = new RS { RF = ref i };
                        rs?.RF = ref i; // 1

                        RS? nrs = rs; // 2
                        nrs?.RF = ref i; // 3
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (11,11): error CS0023: Operator '?' cannot be applied to operand of type 'RS'
                //         rs?.RF = ref i; // 1
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "RS").WithLocation(11, 11),
                // (13,9): error CS9244: The type 'RS' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         RS? nrs = rs; // 2
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "RS?").WithArguments("System.Nullable<T>", "T", "RS").WithLocation(13, 9),
                // (14,13): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         nrs?.RF = ref i; // 3
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, ".RF").WithLocation(14, 13));
        }

        [Fact]
        public void AssignRefReturningMethod_01()
        {
            var source = """
                using System;

                class C
                {
                    static int F;
                    ref int M() => ref F;

                    static void Main()
                    {
                        M1(new C());
                        M2(new C());
                        M1(null);
                        M2(null);
                    }

                    static void M1(C c)
                    {
                        c?.M() = 1;
                        Console.Write(c?.M() ?? 3);
                    }

                    static void M2(C c)
                    {
                        Console.Write((c?.M() = 2) ?? 4);
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "1234");

            verifier.VerifyIL("C.M1", """
                {
                  // Code size       30 (0x1e)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  brfalse.s  IL_000b
                  IL_0003:  ldarg.0
                  IL_0004:  call       "ref int C.M()"
                  IL_0009:  ldc.i4.1
                  IL_000a:  stind.i4
                  IL_000b:  ldarg.0
                  IL_000c:  brtrue.s   IL_0011
                  IL_000e:  ldc.i4.3
                  IL_000f:  br.s       IL_0018
                  IL_0011:  ldarg.0
                  IL_0012:  call       "ref int C.M()"
                  IL_0017:  ldind.i4
                  IL_0018:  call       "void System.Console.Write(int)"
                  IL_001d:  ret
                }
                """);

            verifier.VerifyIL("C.M2", """
                {
                  // Code size       23 (0x17)
                  .maxstack  3
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  brtrue.s   IL_0006
                  IL_0003:  ldc.i4.4
                  IL_0004:  br.s       IL_0011
                  IL_0006:  ldarg.0
                  IL_0007:  call       "ref int C.M()"
                  IL_000c:  ldc.i4.2
                  IL_000d:  dup
                  IL_000e:  stloc.0
                  IL_000f:  stind.i4
                  IL_0010:  ldloc.0
                  IL_0011:  call       "void System.Console.Write(int)"
                  IL_0016:  ret
                }
                """);

            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void PropertyAccessAssignment_01()
        {
            var source = """
                using System;

                class C
                {
                    int P { get; set; }

                    static void Main()
                    {
                        var c = new C();
                        M1(c, 1);
                        M2(c, 2);
                        c = null;
                        M3(c, 3);
                        M4(c, 4);
                    }

                    static void M1(C c, int i)
                    {
                        c?.P = i;
                        Console.Write(c.P);
                    }

                    static void M2(C c, int i)
                    {
                        Console.Write(c?.P = i);
                    }

                    static void M3(C c, int i)
                    {
                        c?.P = i;
                        Console.Write(c?.P is null);
                    }

                    static void M4(C c, int i)
                    {
                        Console.Write((c?.P = i) is null);
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "12TrueTrue");
            verifier.VerifyIL("C.M1", """
                {
                  // Code size       22 (0x16)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  brfalse.s  IL_000a
                  IL_0003:  ldarg.0
                  IL_0004:  ldarg.1
                  IL_0005:  call       "void C.P.set"
                  IL_000a:  ldarg.0
                  IL_000b:  callvirt   "int C.P.get"
                  IL_0010:  call       "void System.Console.Write(int)"
                  IL_0015:  ret
                }
                """);

            verifier.VerifyIL("C.M2", """
                {
                  // Code size       40 (0x28)
                  .maxstack  3
                  .locals init (int? V_0,
                                int V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  brtrue.s   IL_000e
                  IL_0003:  ldloca.s   V_0
                  IL_0005:  initobj    "int?"
                  IL_000b:  ldloc.0
                  IL_000c:  br.s       IL_001d
                  IL_000e:  ldarg.0
                  IL_000f:  ldarg.1
                  IL_0010:  dup
                  IL_0011:  stloc.1
                  IL_0012:  call       "void C.P.set"
                  IL_0017:  ldloc.1
                  IL_0018:  newobj     "int?..ctor(int)"
                  IL_001d:  box        "int?"
                  IL_0022:  call       "void System.Console.Write(object)"
                  IL_0027:  ret
                }
                """);

            verifier.VerifyIL("C.M3", """
                {
                  // Code size       52 (0x34)
                  .maxstack  2
                  .locals init (int? V_0,
                                int? V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  brfalse.s  IL_000a
                  IL_0003:  ldarg.0
                  IL_0004:  ldarg.1
                  IL_0005:  call       "void C.P.set"
                  IL_000a:  ldarg.0
                  IL_000b:  brtrue.s   IL_0018
                  IL_000d:  ldloca.s   V_1
                  IL_000f:  initobj    "int?"
                  IL_0015:  ldloc.1
                  IL_0016:  br.s       IL_0023
                  IL_0018:  ldarg.0
                  IL_0019:  call       "int C.P.get"
                  IL_001e:  newobj     "int?..ctor(int)"
                  IL_0023:  stloc.0
                  IL_0024:  ldloca.s   V_0
                  IL_0026:  call       "bool int?.HasValue.get"
                  IL_002b:  ldc.i4.0
                  IL_002c:  ceq
                  IL_002e:  call       "void System.Console.Write(bool)"
                  IL_0033:  ret
                }
                """);

            verifier.VerifyIL("C.M4", """
                {
                  // Code size       46 (0x2e)
                  .maxstack  3
                  .locals init (int? V_0,
                                int? V_1,
                                int V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  brtrue.s   IL_000e
                  IL_0003:  ldloca.s   V_1
                  IL_0005:  initobj    "int?"
                  IL_000b:  ldloc.1
                  IL_000c:  br.s       IL_001d
                  IL_000e:  ldarg.0
                  IL_000f:  ldarg.1
                  IL_0010:  dup
                  IL_0011:  stloc.2
                  IL_0012:  call       "void C.P.set"
                  IL_0017:  ldloc.2
                  IL_0018:  newobj     "int?..ctor(int)"
                  IL_001d:  stloc.0
                  IL_001e:  ldloca.s   V_0
                  IL_0020:  call       "bool int?.HasValue.get"
                  IL_0025:  ldc.i4.0
                  IL_0026:  ceq
                  IL_0028:  call       "void System.Console.Write(bool)"
                  IL_002d:  ret
                }
                """);

            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void EventAssignment_01()
        {
            var source = """
                using System;

                class C
                {
                    public event Action E;

                    static void Main()
                    {
                        M(new C());
                        M(null);
                    }

                    static void M(C c)
                    {
                        c?.E += () => { Console.Write(1); };
                        c?.E();
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyIL("C.M", """
                {
                  // Code size       55 (0x37)
                  .maxstack  3
                  IL_0000:  ldarg.0
                  IL_0001:  brfalse.s  IL_0028
                  IL_0003:  ldarg.0
                  IL_0004:  ldsfld     "System.Action C.<>c.<>9__4_0"
                  IL_0009:  dup
                  IL_000a:  brtrue.s   IL_0023
                  IL_000c:  pop
                  IL_000d:  ldsfld     "C.<>c C.<>c.<>9"
                  IL_0012:  ldftn      "void C.<>c.<M>b__4_0()"
                  IL_0018:  newobj     "System.Action..ctor(object, System.IntPtr)"
                  IL_001d:  dup
                  IL_001e:  stsfld     "System.Action C.<>c.<>9__4_0"
                  IL_0023:  call       "void C.E.add"
                  IL_0028:  ldarg.0
                  IL_0029:  brfalse.s  IL_0036
                  IL_002b:  ldarg.0
                  IL_002c:  ldfld      "System.Action C.E"
                  IL_0031:  callvirt   "void System.Action.Invoke()"
                  IL_0036:  ret
                }
                """);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_01()
        {
            var source = """
                using System;

                class C
                {
                    int f;

                    static void Main()
                    {
                        M1(new C());
                        M1(null);
                        M2(new C());
                        M2(null);
                    }

                    static void M1(C c)
                    {
                        c?.f += 1;
                        Console.Write(c?.f ?? 2);
                    }

                    static void M2(C c)
                    {
                        Console.Write((c?.f += 3) ?? 4);
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "1234");
            verifier.VerifyIL("C.M1", """
                {
                  // Code size       35 (0x23)
                  .maxstack  3
                  IL_0000:  ldarg.0
                  IL_0001:  brfalse.s  IL_0011
                  IL_0003:  ldarg.0
                  IL_0004:  dup
                  IL_0005:  ldfld      "int C.f"
                  IL_000a:  ldc.i4.1
                  IL_000b:  add
                  IL_000c:  stfld      "int C.f"
                  IL_0011:  ldarg.0
                  IL_0012:  brtrue.s   IL_0017
                  IL_0014:  ldc.i4.2
                  IL_0015:  br.s       IL_001d
                  IL_0017:  ldarg.0
                  IL_0018:  ldfld      "int C.f"
                  IL_001d:  call       "void System.Console.Write(int)"
                  IL_0022:  ret
                }
                """);
            verifier.VerifyIL("C.M2", """
                {
                  // Code size       29 (0x1d)
                  .maxstack  3
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  brtrue.s   IL_0006
                  IL_0003:  ldc.i4.4
                  IL_0004:  br.s       IL_0017
                  IL_0006:  ldarg.0
                  IL_0007:  dup
                  IL_0008:  ldfld      "int C.f"
                  IL_000d:  ldc.i4.3
                  IL_000e:  add
                  IL_000f:  dup
                  IL_0010:  stloc.0
                  IL_0011:  stfld      "int C.f"
                  IL_0016:  ldloc.0
                  IL_0017:  call       "void System.Console.Write(int)"
                  IL_001c:  ret
                }
                """);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void FieldAccessAssignment_Nested_01()
        {
            var source = """
                using System;

                class C
                {
                    int f;

                    static void Main()
                    {
                        var c = new C();
                        int x = 1;
                        c?.f = x = 2;
                        Console.Write(c.f);
                        Console.Write(x);
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "22");
            verifier.VerifyIL("C.Main", """
                {
                  // Code size       39 (0x27)
                  .maxstack  4
                  .locals init (int V_0) //x
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldc.i4.1
                  IL_0006:  stloc.0
                  IL_0007:  dup
                  IL_0008:  dup
                  IL_0009:  brtrue.s   IL_000e
                  IL_000b:  pop
                  IL_000c:  br.s       IL_0016
                  IL_000e:  ldc.i4.2
                  IL_000f:  dup
                  IL_0010:  stloc.0
                  IL_0011:  stfld      "int C.f"
                  IL_0016:  ldfld      "int C.f"
                  IL_001b:  call       "void System.Console.Write(int)"
                  IL_0020:  ldloc.0
                  IL_0021:  call       "void System.Console.Write(int)"
                  IL_0026:  ret
                }
                """);

            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void FieldAccessAssignment_Nested_02()
        {
            var source = """
                using System;

                class C
                {
                    int f;

                    static void Main()
                    {
                        C c = null;
                        int x = 1;
                        c?.f = x = 2;
                        Console.Write(c?.f is null);
                        Console.Write(x);
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "True1");
            verifier.VerifyIL("C.Main", """
                {
                  // Code size       66 (0x42)
                  .maxstack  4
                  .locals init (int V_0, //x
                                int? V_1,
                                int? V_2)
                  IL_0000:  ldnull
                  IL_0001:  ldc.i4.1
                  IL_0002:  stloc.0
                  IL_0003:  dup
                  IL_0004:  dup
                  IL_0005:  brtrue.s   IL_000a
                  IL_0007:  pop
                  IL_0008:  br.s       IL_0012
                  IL_000a:  ldc.i4.2
                  IL_000b:  dup
                  IL_000c:  stloc.0
                  IL_000d:  stfld      "int C.f"
                  IL_0012:  dup
                  IL_0013:  brtrue.s   IL_0021
                  IL_0015:  pop
                  IL_0016:  ldloca.s   V_2
                  IL_0018:  initobj    "int?"
                  IL_001e:  ldloc.2
                  IL_001f:  br.s       IL_002b
                  IL_0021:  ldfld      "int C.f"
                  IL_0026:  newobj     "int?..ctor(int)"
                  IL_002b:  stloc.1
                  IL_002c:  ldloca.s   V_1
                  IL_002e:  call       "bool int?.HasValue.get"
                  IL_0033:  ldc.i4.0
                  IL_0034:  ceq
                  IL_0036:  call       "void System.Console.Write(bool)"
                  IL_003b:  ldloc.0
                  IL_003c:  call       "void System.Console.Write(int)"
                  IL_0041:  ret
                }
                """);

            verifier.VerifyDiagnostics();
        }
        // TODO2: assignment in a nested cond access

        [Fact]
        public void TypeParameter_01()
        {
            var source = """
                class C<T>
                {
                    public T t;
                    public static void M(C<T> c)
                    {
                        c?.t = default;
                        var x = c?.t = default; // 1
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,19): error CS8978: 'T' cannot be made nullable.
                //         var x = c?.t = default; // 1
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".t = default").WithArguments("T").WithLocation(7, 19));
        }

        [Fact]
        public void TypeParameter_02()
        {
            var source = """
                using System;

                class Program
                {
                    public static void Main()
                    {
                        C<string>.M(new C<string>());
                    }
                }

                class C<T> where T : class
                {
                    public T t;
                    public static void M(C<T> c)
                    {
                        c?.t = null;
                        var x = c?.t = null; // 1
                        Console.Write(c.t is null);
                        Console.Write(x is null);
                    }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "TrueTrue");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C<T>.M", """
                {
                  // Code size       80 (0x50)
                  .maxstack  3
                  .locals init (T V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  brfalse.s  IL_000f
                  IL_0003:  ldarg.0
                  IL_0004:  ldflda     "T C<T>.t"
                  IL_0009:  initobj    "T"
                  IL_000f:  ldarg.0
                  IL_0010:  brtrue.s   IL_001d
                  IL_0012:  ldloca.s   V_0
                  IL_0014:  initobj    "T"
                  IL_001a:  ldloc.0
                  IL_001b:  br.s       IL_002f
                  IL_001d:  ldarg.0
                  IL_001e:  ldloca.s   V_0
                  IL_0020:  initobj    "T"
                  IL_0026:  ldloc.0
                  IL_0027:  dup
                  IL_0028:  stloc.0
                  IL_0029:  stfld      "T C<T>.t"
                  IL_002e:  ldloc.0
                  IL_002f:  ldarg.0
                  IL_0030:  ldfld      "T C<T>.t"
                  IL_0035:  box        "T"
                  IL_003a:  ldnull
                  IL_003b:  ceq
                  IL_003d:  call       "void System.Console.Write(bool)"
                  IL_0042:  box        "T"
                  IL_0047:  ldnull
                  IL_0048:  ceq
                  IL_004a:  call       "void System.Console.Write(bool)"
                  IL_004f:  ret
                }
                """);
        }

        [Fact]
        public void TypeParameter_03()
        {
            var source = """
                using System;

                class Program
                {
                    public static void Main()
                    {
                        C<int>.M(new C<int>(), 1);
                    }
                }

                class C<T> where T : struct
                {
                    public T t;
                    public static void M(C<T> c, T param)
                    {
                        c?.t = param;
                        var x = c?.t = param;
                        Console.Write(c?.t);
                        Console.Write(x);
                    }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "11");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C<T>.M", """
                {
                  // Code size       85 (0x55)
                  .maxstack  3
                  .locals init (T? V_0,
                                T V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  brfalse.s  IL_000a
                  IL_0003:  ldarg.0
                  IL_0004:  ldarg.1
                  IL_0005:  stfld      "T C<T>.t"
                  IL_000a:  ldarg.0
                  IL_000b:  brtrue.s   IL_0018
                  IL_000d:  ldloca.s   V_0
                  IL_000f:  initobj    "T?"
                  IL_0015:  ldloc.0
                  IL_0016:  br.s       IL_0027
                  IL_0018:  ldarg.0
                  IL_0019:  ldarg.1
                  IL_001a:  dup
                  IL_001b:  stloc.1
                  IL_001c:  stfld      "T C<T>.t"
                  IL_0021:  ldloc.1
                  IL_0022:  newobj     "T?..ctor(T)"
                  IL_0027:  ldarg.0
                  IL_0028:  brtrue.s   IL_0035
                  IL_002a:  ldloca.s   V_0
                  IL_002c:  initobj    "T?"
                  IL_0032:  ldloc.0
                  IL_0033:  br.s       IL_0040
                  IL_0035:  ldarg.0
                  IL_0036:  ldfld      "T C<T>.t"
                  IL_003b:  newobj     "T?..ctor(T)"
                  IL_0040:  box        "T?"
                  IL_0045:  call       "void System.Console.Write(object)"
                  IL_004a:  box        "T?"
                  IL_004f:  call       "void System.Console.Write(object)"
                  IL_0054:  ret
                }
                """);
        }

        [Fact]
        public void Parentheses_Assignment_LHS_01()
        {
            var source = """
                using System;

                class C
                {
                    int F;
                    static void M(C c)
                    {
                        (c?.F) = 1; // 1
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,9): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 0
                //     int F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "0").WithLocation(5, 9),
                // (8,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (c?.F) = 1; // 1
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "c?.F").WithLocation(8, 10));
        }

        // TODO2: null-coalescing-assignment
        // TODO2: other tricky cases from the proposal
    }
}
