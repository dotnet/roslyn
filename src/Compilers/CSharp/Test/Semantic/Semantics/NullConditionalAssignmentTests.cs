// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
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
        public void MemberAccessAssignment_01()
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
        public void MemberAccessAssignment_Nested_01()
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
        public void MemberAccessAssignment_Nested_02()
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
            // TODO2: verify IL
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
            // TODO2: verify IL
        }

        // TODO2: compound assignments
        // TODO2: type parameter valued assignments
    }
}
