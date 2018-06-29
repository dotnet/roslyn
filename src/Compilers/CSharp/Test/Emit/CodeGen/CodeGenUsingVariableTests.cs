using Roslyn.Test.Utilities;
using Xunit;


namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenUsingVariableTests : EmitMetadataTestBase
    {
        [Fact]
        public void UsingVariableVarEmitTest()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    public void Dispose() { }
}
class C2
{
    public static void Main()
    {
        using var c1 = new C1(); 
    }
}";
            CompileAndVerify(source).VerifyIL("C2.Main", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (C1 V_0) //c1
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  .try
  {
    IL_0006:  leave.s    IL_0012
  }
  finally
  {
    IL_0008:  ldloc.0
    IL_0009:  brfalse.s  IL_0011
    IL_000b:  ldloc.0
    IL_000c:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0011:  endfinally
  }
  IL_0012:  ret
}");
        }

        [Fact]
        public void UseUsingVariableEmitTest()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    public void Method1() { }
    public void Dispose() { }
}
class C2
{
    public static void Main()
    {
        using var c1 = new C1(); 
        c1.Method1();
    }
}";
            CompileAndVerify(source).VerifyIL("C2.Main", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (C1 V_0) //c1
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  .try
  {
    IL_0006:  ldloc.0
    IL_0007:  callvirt   ""void C1.Method1()""
    IL_000c:  leave.s    IL_0018
  }
  finally
  {
    IL_000e:  ldloc.0
    IL_000f:  brfalse.s  IL_0017
    IL_0011:  ldloc.0
    IL_0012:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0017:  endfinally
  }
  IL_0018:  ret
}");
        }

        [Fact]
        public void UsingVariableTypedVariable()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    public void Method1() { }
    public void Dispose() { }
}
class C2
{
    public static void Main()
    {
        using C1 c1 = new C1(); 
        c1.Method1();
    }
}";
            CompileAndVerify(source).VerifyIL("C2.Main", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (C1 V_0) //c1
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  .try
  {
    IL_0006:  ldloc.0
    IL_0007:  callvirt   ""void C1.Method1()""
    IL_000c:  leave.s    IL_0018
  }
  finally
  {
    IL_000e:  ldloc.0
    IL_000f:  brfalse.s  IL_0017
    IL_0011:  ldloc.0
    IL_0012:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0017:  endfinally
  }
  IL_0018:  ret
}");
        }
    }
}
