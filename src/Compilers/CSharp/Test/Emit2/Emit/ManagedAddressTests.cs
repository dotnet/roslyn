// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class ManagedAddressTests : CSharpTestBase
    {
        [Fact]
        public void TestPointerToArray()
        {
            var source = """
                using System;

                unsafe
                {
                    var x = new[] { 0, 1, 2 };
                    int[]* xp = &x;
                    var c = new C(xp);
                    c.Print();
                }

                public unsafe class C
                {
                    private int[]* _x;

                    public C(int[]* x)
                    {
                        _x = x;
                    }

                    public void Print()
                    {
                        int[] x = *_x;
                        for (int i = 0; i < x.Length; i++)
                        {
                            Console.Write(x[i]);
                        }
                    }
                }
                """;

            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugExe).VerifyDiagnostics(
                // (6,5): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('int[]')
                //     int[]* xp = &x;
                Diagnostic(ErrorCode.WRN_ManagedAddr, "int[]*").WithArguments("int[]").WithLocation(6, 5),
                // (6,17): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('int[]')
                //     int[]* xp = &x;
                Diagnostic(ErrorCode.WRN_ManagedAddr, "&x").WithArguments("int[]").WithLocation(6, 17),
                // (13,20): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('int[]')
                //     private int[]* _x;
                Diagnostic(ErrorCode.WRN_ManagedAddr, "_x").WithArguments("int[]").WithLocation(13, 20),
                // (15,21): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('int[]')
                //     public C(int[]* x)
                Diagnostic(ErrorCode.WRN_ManagedAddr, "x").WithArguments("int[]").WithLocation(15, 21)
                );
            var verifier = CompileAndVerify(comp, expectedOutput: "012", verify: Verification.Fails with
            {
                ILVerifyMessage = """
                [<Main>$]: Expected numeric type on the stack. { Offset = 0x12, Found = address of 'int32[]' }
                [.ctor]: Unmanaged pointers are not a verifiable type. { Offset = 0x9 }
                [Print]: Expected ByRef on the stack. { Offset = 0x7, Found = Native Int }
                """,
                PEVerifyMessage = """
                [ : Program::<Main>$][offset 0x00000012][found address of ref ] Expected numeric type on the stack.
                [ : C::.ctor][offset 0x00000009] Unmanaged pointers are not a verifiable type.
                [ : C::Print][offset 0x00000007][found unmanaged pointer] Expected ByRef on the stack.
                """,
            });

            verifier.VerifyMethodBody("<top-level-statements-entry-point>", """
                {
                  // Code size       36 (0x24)
                  .maxstack  4
                  .locals init (int[] V_0, //x
                                int[]* V_1, //xp
                                C V_2) //c
                  // sequence point: {
                  IL_0000:  nop
                  // sequence point: var x = new[] { 0, 1, 2 };
                  IL_0001:  ldc.i4.3
                  IL_0002:  newarr     "int"
                  IL_0007:  dup
                  IL_0008:  ldc.i4.1
                  IL_0009:  ldc.i4.1
                  IL_000a:  stelem.i4
                  IL_000b:  dup
                  IL_000c:  ldc.i4.2
                  IL_000d:  ldc.i4.2
                  IL_000e:  stelem.i4
                  IL_000f:  stloc.0
                  // sequence point: int[]* xp = &x;
                  IL_0010:  ldloca.s   V_0
                  IL_0012:  conv.u
                  IL_0013:  stloc.1
                  // sequence point: var c = new C(xp);
                  IL_0014:  ldloc.1
                  IL_0015:  newobj     "C..ctor(int[]*)"
                  IL_001a:  stloc.2
                  // sequence point: c.Print();
                  IL_001b:  ldloc.2
                  IL_001c:  callvirt   "void C.Print()"
                  IL_0021:  nop
                  // sequence point: }
                  IL_0022:  nop
                  IL_0023:  ret
                }
                """);

            verifier.VerifyMethodBody("C..ctor", """
                {
                  // Code size       16 (0x10)
                  .maxstack  2
                  // sequence point: public C(int[]* x)
                  IL_0000:  ldarg.0
                  IL_0001:  call       "object..ctor()"
                  IL_0006:  nop
                  // sequence point: {
                  IL_0007:  nop
                  // sequence point: _x = x;
                  IL_0008:  ldarg.0
                  IL_0009:  ldarg.1
                  IL_000a:  stfld      "int[]* C._x"
                  // sequence point: }
                  IL_000f:  ret
                }
                """);

            verifier.VerifyMethodBody("C.Print", """
                {
                  // Code size       39 (0x27)
                  .maxstack  2
                  .locals init (int[] V_0, //x
                                int V_1, //i
                                bool V_2)
                  // sequence point: {
                  IL_0000:  nop
                  // sequence point: int[] x = *_x;
                  IL_0001:  ldarg.0
                  IL_0002:  ldfld      "int[]* C._x"
                  IL_0007:  ldind.ref
                  IL_0008:  stloc.0
                  // sequence point: int i = 0
                  IL_0009:  ldc.i4.0
                  IL_000a:  stloc.1
                  // sequence point: <hidden>
                  IL_000b:  br.s       IL_001c
                  // sequence point: {
                  IL_000d:  nop
                  // sequence point: Console.Write(x[i]);
                  IL_000e:  ldloc.0
                  IL_000f:  ldloc.1
                  IL_0010:  ldelem.i4
                  IL_0011:  call       "void System.Console.Write(int)"
                  IL_0016:  nop
                  // sequence point: }
                  IL_0017:  nop
                  // sequence point: i++
                  IL_0018:  ldloc.1
                  IL_0019:  ldc.i4.1
                  IL_001a:  add
                  IL_001b:  stloc.1
                  // sequence point: i < x.Length
                  IL_001c:  ldloc.1
                  IL_001d:  ldloc.0
                  IL_001e:  ldlen
                  IL_001f:  conv.i4
                  IL_0020:  clt
                  IL_0022:  stloc.2
                  // sequence point: <hidden>
                  IL_0023:  ldloc.2
                  IL_0024:  brtrue.s   IL_000d
                  // sequence point: }
                  IL_0026:  ret
                }
                """);
        }
    }
}
