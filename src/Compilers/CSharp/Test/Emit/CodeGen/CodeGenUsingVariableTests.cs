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

        [Fact]
        public void PreexistingVariablesUsingVariableEmitTest()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    public void Dispose() { }
    public void Method1() { }
}
class C2
{
    public static void Main()
    {
        C1 c0 = new C1();
        c0.Method1();
        using var c1 = new C1();
    }
}";
            CompileAndVerify(source).VerifyIL("C2.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (C1 V_0) //c1
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  callvirt   ""void C1.Method1()""
  IL_000a:  newobj     ""C1..ctor()""
  IL_000f:  stloc.0
  .try
  {
    IL_0010:  leave.s    IL_001c
  }
  finally
  {
    IL_0012:  ldloc.0
    IL_0013:  brfalse.s  IL_001b
    IL_0015:  ldloc.0
    IL_0016:  callvirt   ""void System.IDisposable.Dispose()""
    IL_001b:  endfinally
  }
  IL_001c:  ret
}");
        }

        [Fact]
        public void TwoUsingVarsInARow()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    public void M() { } 
    public void Dispose() { }
}
class C2
{
    public static void Main()                                                                                                           
    {
        using C1 o1 = new C1();
        using C1 o2 = new C1();
    }
}";
            CompileAndVerify(source).VerifyIL("C2.Main", @"
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (C1 V_0, //o1
                C1 V_1) //o2
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  .try
  {
    IL_0006:  newobj     ""C1..ctor()""
    IL_000b:  stloc.1
    .try
    {
      IL_000c:  leave.s    IL_0022
    }
    finally
    {
      IL_000e:  ldloc.1
      IL_000f:  brfalse.s  IL_0017
      IL_0011:  ldloc.1
      IL_0012:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0017:  endfinally
    }
  }
  finally
  {
    IL_0018:  ldloc.0
    IL_0019:  brfalse.s  IL_0021
    IL_001b:  ldloc.0
    IL_001c:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0021:  endfinally
  }
  IL_0022:  ret
}");
        }

        [Fact]
        public void UsingVarSandwich()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    public void M() { } 
    public void Dispose() { }
}
class C2
{
    public static void Main()                                                                                                           
    {
        using C1 o1 = new C1();
        o1.M();
        using C1 o2 = new C1();
    }
}";
            CompileAndVerify(source).VerifyIL("C2.Main", @"
{
  // Code size       41 (0x29)
  .maxstack  1
  .locals init (C1 V_0, //o1
                C1 V_1) //o2
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  .try
  {
    IL_0006:  ldloc.0
    IL_0007:  callvirt   ""void C1.M()""
    IL_000c:  newobj     ""C1..ctor()""
    IL_0011:  stloc.1
    .try
    {
      IL_0012:  leave.s    IL_0028
    }
    finally
    {
      IL_0014:  ldloc.1
      IL_0015:  brfalse.s  IL_001d
      IL_0017:  ldloc.1
      IL_0018:  callvirt   ""void System.IDisposable.Dispose()""
      IL_001d:  endfinally
    }
  }
  finally
  {
    IL_001e:  ldloc.0
    IL_001f:  brfalse.s  IL_0027
    IL_0021:  ldloc.0
    IL_0022:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0027:  endfinally
  }
  IL_0028:  ret
}");
        }

        [Fact]
        public void InsideOfUsingVarInCorrectOrder()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    public void M() { } 
    public void Dispose() { }
}
class C2
{
    public static void Main()                                                                                                           
    {
        using C1 o1 = new C1();
        using C1 o2 = new C1();
        o2.M();
        o1.M();
    }
}";
            CompileAndVerify(source).VerifyIL("C2.Main", @"
{
  // Code size       47 (0x2f)
  .maxstack  1
  .locals init (C1 V_0, //o1
                C1 V_1) //o2
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  .try
  {
    IL_0006:  newobj     ""C1..ctor()""
    IL_000b:  stloc.1
    .try
    {
      IL_000c:  ldloc.1
      IL_000d:  callvirt   ""void C1.M()""
      IL_0012:  ldloc.0
      IL_0013:  callvirt   ""void C1.M()""
      IL_0018:  leave.s    IL_002e
    }
    finally
    {
      IL_001a:  ldloc.1
      IL_001b:  brfalse.s  IL_0023
      IL_001d:  ldloc.1
      IL_001e:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0023:  endfinally
    }
  }
  finally
  {
    IL_0024:  ldloc.0
    IL_0025:  brfalse.s  IL_002d
    IL_0027:  ldloc.0
    IL_0028:  callvirt   ""void System.IDisposable.Dispose()""
    IL_002d:  endfinally
  }
  IL_002e:  ret
}");
        }
    }
}
