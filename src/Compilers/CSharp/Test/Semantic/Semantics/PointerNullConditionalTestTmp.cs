// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PointerNullConditionalTestTmp : CSharpTestBase
    {
        [Fact]
        public void PointerReturnValue()
        {
            var source = @"
public unsafe class A
{
    public byte* Ptr;
}
unsafe class Test
{
    void M()
    {
        var x = new A();
        byte* ptr = x?.Ptr;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (4,18): warning CS0649: Field 'A.Ptr' is never assigned to, and will always have its default value 
                //     public byte* Ptr;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Ptr").WithArguments("A.Ptr", "").WithLocation(4, 18),
                // (11,23): error CS8978: 'byte*' cannot be made nullable.
                //         byte* ptr = x?.Ptr;
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".Ptr").WithArguments("byte*").WithLocation(11, 23));
        }
        
        [Fact]
        public void PointerReceiver()
        {
            var source = @"
unsafe struct S
{
    public int F;
}
unsafe class Test
{
    void M(S* x)
    {
        var y = x?.F;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (9,18): error CS0023: Operator '?' cannot be applied to operand of type 'S*'
                //         var y = x?.F;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "S*").WithLocation(9, 18));
        }
    }
}
