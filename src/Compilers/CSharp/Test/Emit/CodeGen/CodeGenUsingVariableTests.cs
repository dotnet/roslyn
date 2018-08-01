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

        [Fact]
        public void UsingVariableUsingPatternIntersectionEmitTest()
        {
            var source = @"
    using System;
    class C1
    {
        public void M()
        {
            Console.WriteLine(""This method has run."");
        }
        public void Dispose()
        {
            Console.WriteLine(""This object has been properly disposed."");
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            using C1 o1 = new C1();
            o1.M();
        }
    }";

            var output = @"This method has run.
This object has been properly disposed.";
            CompileAndVerify(source, expectedOutput: output).VerifyIL("Program.Main", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (C1 V_0) //o1
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  .try
  {
    IL_0006:  ldloc.0
    IL_0007:  callvirt   ""void C1.M()""
    IL_000c:  leave.s    IL_0018
  }
  finally
  {
    IL_000e:  ldloc.0
    IL_000f:  brfalse.s  IL_0017
    IL_0011:  ldloc.0
    IL_0012:  callvirt   ""void C1.Dispose()""
    IL_0017:  endfinally
  }
  IL_0018:  ret
}");
        }

        [Fact]
        public void UsingVariableUsingPatternIntersectionTwoDisposeMethodsEmitTest()
        {
            var source = @"
    using System;
    class C1 : IDisposable
    {
        public void M()
        {
            Console.WriteLine(""This method has run."");
        }
        public void Dispose()
        {
            Console.WriteLine(""This object has been disposed by C1.Dispose()."");
        }
        void IDisposable.Dispose()
        {
            Console.WriteLine(""This object has been disposed by IDisposable.Dispose()."");
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            using C1 o1 = new C1();
            o1.M();
        }
    }";

            var output = @"This method has run.
This object has been disposed by IDisposable.Dispose().";
            CompileAndVerify(source, expectedOutput: output).VerifyIL("Program.Main", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (C1 V_0) //o1
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  .try
  {
    IL_0006:  ldloc.0
    IL_0007:  callvirt   ""void C1.M()""
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
        public void MultipleUsingVarEmitTest()
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
        using C1 o1 = new C1(), o2 = new C1();
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
        public void MultipleUsingVarPrecedingCodeEmitTest()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    private string name;
    public C1(string name)
    {
        this.name = name;
        Console.WriteLine(""Object "" + name + "" has been created."");
    }
    public void M() { } 
    public void Dispose()
    {
        Console.WriteLine(""Object "" + name + "" has been disposed."");
    }
}
class C2
{
    public static void Main()                                                                                                           
    {
        C1 o0 = new C1(""first"");
        o0.M();
        using C1 o1 = new C1(""second""), o2 = new C1(""third"");
    }
}";
            var output = @"Object first has been created.
Object second has been created.
Object third has been created.
Object third has been disposed.
Object second has been disposed.";
            CompileAndVerify(source, expectedOutput: output).VerifyIL("C2.Main", @"
{
  // Code size       60 (0x3c)
  .maxstack  1
  .locals init (C1 V_0, //o1
                C1 V_1) //o2
  IL_0000:  ldstr      ""first""
  IL_0005:  newobj     ""C1..ctor(string)""
  IL_000a:  callvirt   ""void C1.M()""
  IL_000f:  ldstr      ""second""
  IL_0014:  newobj     ""C1..ctor(string)""
  IL_0019:  stloc.0
  .try
  {
    IL_001a:  ldstr      ""third""
    IL_001f:  newobj     ""C1..ctor(string)""
    IL_0024:  stloc.1
    .try
    {
      IL_0025:  leave.s    IL_003b
    }
    finally
    {
      IL_0027:  ldloc.1
      IL_0028:  brfalse.s  IL_0030
      IL_002a:  ldloc.1
      IL_002b:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0030:  endfinally
    }
  }
  finally
  {
    IL_0031:  ldloc.0
    IL_0032:  brfalse.s  IL_003a
    IL_0034:  ldloc.0
    IL_0035:  callvirt   ""void System.IDisposable.Dispose()""
    IL_003a:  endfinally
  }
  IL_003b:  ret
}");
        }

        [Fact]
        public void MultipleUsingVarFollowingCodeEmitTest()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    private string name;
    public C1(string name)
    {
        this.name = name;
        Console.WriteLine(""Object "" + name + "" has been created."");
    }
    public void M() { } 
    public void Dispose()
    {
        Console.WriteLine(""Object "" + name + "" has been disposed."");
    }
}
class C2
{
    public static void Main()                                                                                                           
    {
        using C1 o1 = new C1(""first""), o2 = new C1(""second"");
        C1 o0 = new C1(""third"");
        o0.M();
    }
}";
            var output = @"Object first has been created.
Object second has been created.
Object third has been created.
Object second has been disposed.
Object first has been disposed.";
            CompileAndVerify(source, expectedOutput: output).VerifyIL("C2.Main", @"
{
  // Code size       60 (0x3c)
  .maxstack  1
  .locals init (C1 V_0, //o1
                C1 V_1) //o2
  IL_0000:  ldstr      ""first""
  IL_0005:  newobj     ""C1..ctor(string)""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  ldstr      ""second""
    IL_0010:  newobj     ""C1..ctor(string)""
    IL_0015:  stloc.1
    .try
    {
      IL_0016:  ldstr      ""third""
      IL_001b:  newobj     ""C1..ctor(string)""
      IL_0020:  callvirt   ""void C1.M()""
      IL_0025:  leave.s    IL_003b
    }
    finally
    {
      IL_0027:  ldloc.1
      IL_0028:  brfalse.s  IL_0030
      IL_002a:  ldloc.1
      IL_002b:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0030:  endfinally
    }
  }
  finally
  {
    IL_0031:  ldloc.0
    IL_0032:  brfalse.s  IL_003a
    IL_0034:  ldloc.0
    IL_0035:  callvirt   ""void System.IDisposable.Dispose()""
    IL_003a:  endfinally
  }
  IL_003b:  ret
}");
        }
    }
}
