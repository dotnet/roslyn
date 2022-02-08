// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MethodGroupConversion : CSharpTestBase
    {
        [Fact]
        public void TestAmbiguous()
        {
            var source = @"
partial class Program
{
    static void Main()
    {
        D d;
        d = M;
        d = new D(M);
        M2(M);

        d = M3;
        d = new D(M3);
        M2(M3);
    }
    static void M(I1 i1) {}
    static void M(I2 i2) {}
    static void M2(D d) {}

    static void M3(int x) {}
}
interface I1 {}
interface I2 {}
class C : I1, I2 {}

delegate void D(C c);
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,13): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(I1)' and 'Program.M(I2)'
                //         d = M;
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(I1)", "Program.M(I2)").WithLocation(7, 13),
                // (8,19): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(I1)' and 'Program.M(I2)'
                //         d = new D(M);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(I1)", "Program.M(I2)").WithLocation(8, 19),
                // (9,12): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(I1)' and 'Program.M(I2)'
                //         M2(M);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(I1)", "Program.M(I2)").WithLocation(9, 12),
                // (11,13): error CS0123: No overload for 'M3' matches delegate 'D'
                //         d = M3;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M3").WithArguments("M3", "D").WithLocation(11, 13),
                // (12,13): error CS0123: No overload for 'M3' matches delegate 'D'
                //         d = new D(M3);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new D(M3)").WithArguments("M3", "D").WithLocation(12, 13),
                // (13,12): error CS1503: Argument 1: cannot convert from 'method group' to 'D'
                //         M2(M3);
                Diagnostic(ErrorCode.ERR_BadArgType, "M3").WithArguments("1", "method group", "D").WithLocation(13, 12)
                );
        }
    }
}
