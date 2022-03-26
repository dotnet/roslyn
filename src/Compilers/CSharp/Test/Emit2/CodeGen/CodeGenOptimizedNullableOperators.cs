// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenOptimizedNullableOperators : CSharpTestBase
    {
        [Fact]
        public void TestComparisonNullableWithNonDefaultConstantValue()
        {
            var code = @"
public class C
{
    public bool M(int? x)
    {
        return x == 42;
    }
}
";
            CompileAndVerify(code).VerifyDiagnostics().VerifyIL("C.M", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (int? V_0,
                int V_1)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.s   42
  IL_0004:  stloc.1
  IL_0005:  ldloca.s   V_0
  IL_0007:  call       ""int int?.GetValueOrDefault()""
  IL_000c:  ldloc.1
  IL_000d:  ceq
  IL_000f:  ret
}
");
        }

        [Fact]
        public void TestComparisonNullableWithDefaultConstantValue()
        {
            var code = @"
public class C
{
    public bool M(int? x)
    {
        return x == 0;
    }
}
";
            CompileAndVerify(code).VerifyDiagnostics().VerifyIL("C.M", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (int? V_0,
                int V_1)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_0
  IL_0006:  call       ""int int?.GetValueOrDefault()""
  IL_000b:  ldloc.1
  IL_000c:  ceq
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       ""bool int?.HasValue.get""
  IL_0015:  and
  IL_0016:  ret
}
");
        }

        [Fact]
        public void TestComparisonNullableWithNull()
        {
            var code = @"
public class C
{
    public bool M(int? x)
    {
        return x == null;
    }
}
";
            CompileAndVerify(code).VerifyDiagnostics().VerifyIL("C.M", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldarga.s   V_1
  IL_0002:  call       ""bool int?.HasValue.get""
  IL_0007:  ldc.i4.0
  IL_0008:  ceq
  IL_000a:  ret
}
");
        }

        [Fact]
        public void TestComparisonTupleContainingNullable()
        {
            var code = @"
public class C
{
    public bool M((int?, int) x)
    {
        return x == (40, 41);
    }
}
";
            CompileAndVerify(code).VerifyDiagnostics().VerifyIL("C.M", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (System.ValueTuple<int?, int> V_0,
                int? V_1,
                int V_2)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldfld      ""int? System.ValueTuple<int?, int>.Item1""
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.s   40
  IL_000b:  stloc.2
  IL_000c:  ldloca.s   V_1
  IL_000e:  call       ""int int?.GetValueOrDefault()""
  IL_0013:  ldloc.2
  IL_0014:  bne.un.s   IL_0021
  IL_0016:  ldloc.0
  IL_0017:  ldfld      ""int System.ValueTuple<int?, int>.Item2""
  IL_001c:  ldc.i4.s   41
  IL_001e:  ceq
  IL_0020:  ret
  IL_0021:  ldc.i4.0
  IL_0022:  ret
}
");
        }
    }
}
