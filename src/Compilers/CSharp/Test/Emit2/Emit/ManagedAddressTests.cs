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
            CompileAndVerify(comp, expectedOutput: "012", verify: Verification.Fails);
        }
    }
}
