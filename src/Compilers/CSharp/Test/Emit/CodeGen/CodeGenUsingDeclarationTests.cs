// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenUsingDeclarationTests : EmitMetadataTestBase
    {
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
  // sequence point: using var c1 = new C1();
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  .try
  {
    // sequence point: }
    IL_0006:  leave.s    IL_0012
  }
  finally
  {
    // sequence point: <hidden>
    IL_0008:  ldloc.0
    IL_0009:  brfalse.s  IL_0011
    IL_000b:  ldloc.0
    IL_000c:  callvirt   ""void System.IDisposable.Dispose()""
    // sequence point: <hidden>
    IL_0011:  endfinally
  }
  // sequence point: }
  IL_0012:  ret
}", sequencePoints: "C2.Main", source: source);
        }

        [Fact]
        public void UsingVariableEmitTest()
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
        public void PreexistingVariablesUsingDeclarationEmitTest()
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
        public void AsPartOfLabelStatement()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    public void Dispose() { Console.Write(""Dispose; "");}
}
class C2
{
    public static void Main()                                                                                                           
    {
        label1:
        using C1 o1 = new C1();
        using C1 o2 = new C1();
        label2:
        using C1 o3 = new C1();
    }
}";
            CompileAndVerify(source, expectedOutput: "Dispose; Dispose; Dispose; ").VerifyIL("C2.Main", @"
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (C1 V_0, //o1
                C1 V_1, //o2
                C1 V_2) //o3
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  .try
  {
    IL_0006:  newobj     ""C1..ctor()""
    IL_000b:  stloc.1
    .try
    {
      IL_000c:  newobj     ""C1..ctor()""
      IL_0011:  stloc.2
      .try
      {
        IL_0012:  leave.s    IL_0032
      }
      finally
      {
        IL_0014:  ldloc.2
        IL_0015:  brfalse.s  IL_001d
        IL_0017:  ldloc.2
        IL_0018:  callvirt   ""void System.IDisposable.Dispose()""
        IL_001d:  endfinally
      }
    }
    finally
    {
      IL_001e:  ldloc.1
      IL_001f:  brfalse.s  IL_0027
      IL_0021:  ldloc.1
      IL_0022:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0027:  endfinally
    }
  }
  finally
  {
    IL_0028:  ldloc.0
    IL_0029:  brfalse.s  IL_0031
    IL_002b:  ldloc.0
    IL_002c:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0031:  endfinally
  }
  IL_0032:  ret
}
");
        }

        [Fact]
        public void AsPartOfMultipleLabelStatements()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    public void Dispose() { Console.Write(""Dispose; "");}
}
class C2
{
    public static void Main()                                                                                                           
    {
        label1:
        label2:
        Console.Write(""Start; "");
        label3:
        label4:
        label5:
        label6:
        using C1 o1 = new C1();
        Console.Write(""Middle1; "");
        using C1 o2 = new C1();
        Console.Write(""Middle2; "");
        label7:
        using C1 o3 = new C1();
        Console.Write(""End; "");
    }
}";
            CompileAndVerify(source, expectedOutput: "Start; Middle1; Middle2; End; Dispose; Dispose; Dispose; ").VerifyIL("C2.Main", @"
{
  // Code size       91 (0x5b)
  .maxstack  1
  .locals init (C1 V_0, //o1
                C1 V_1, //o2
                C1 V_2) //o3
  IL_0000:  ldstr      ""Start; ""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  newobj     ""C1..ctor()""
  IL_000f:  stloc.0
  .try
  {
    IL_0010:  ldstr      ""Middle1; ""
    IL_0015:  call       ""void System.Console.Write(string)""
    IL_001a:  newobj     ""C1..ctor()""
    IL_001f:  stloc.1
    .try
    {
      IL_0020:  ldstr      ""Middle2; ""
      IL_0025:  call       ""void System.Console.Write(string)""
      IL_002a:  newobj     ""C1..ctor()""
      IL_002f:  stloc.2
      .try
      {
        IL_0030:  ldstr      ""End; ""
        IL_0035:  call       ""void System.Console.Write(string)""
        IL_003a:  leave.s    IL_005a
      }
      finally
      {
        IL_003c:  ldloc.2
        IL_003d:  brfalse.s  IL_0045
        IL_003f:  ldloc.2
        IL_0040:  callvirt   ""void System.IDisposable.Dispose()""
        IL_0045:  endfinally
      }
    }
    finally
    {
      IL_0046:  ldloc.1
      IL_0047:  brfalse.s  IL_004f
      IL_0049:  ldloc.1
      IL_004a:  callvirt   ""void System.IDisposable.Dispose()""
      IL_004f:  endfinally
    }
  }
  finally
  {
    IL_0050:  ldloc.0
    IL_0051:  brfalse.s  IL_0059
    IL_0053:  ldloc.0
    IL_0054:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0059:  endfinally
  }
  IL_005a:  ret
}
");
        }

        [Fact]
        public void InsideTryCatchFinallyBlocks()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    public string Text { get; set; }
    public void Dispose() { Console.Write($""Dispose {Text}; "");}
}
class C2
{
    public static void Main()                                                                                                           
    {
        try
        {
            using var x = new C1() { Text = ""Try"" };
            throw new Exception();
        }
        catch
        {
            using var x = new C1(){ Text = ""Catch"" };
        }
        finally
        {
            using var x = new C1(){ Text = ""Finally"" };
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "Dispose Try; Dispose Catch; Dispose Finally; ").VerifyIL("C2.Main", @"
{
  // Code size       96 (0x60)
  .maxstack  3
  .locals init (C1 V_0, //x
                C1 V_1, //x
                C1 V_2) //x
  .try
  {
    .try
    {
      IL_0000:  newobj     ""C1..ctor()""
      IL_0005:  dup
      IL_0006:  ldstr      ""Try""
      IL_000b:  callvirt   ""void C1.Text.set""
      IL_0010:  stloc.0
      .try
      {
        IL_0011:  newobj     ""System.Exception..ctor()""
        IL_0016:  throw
      }
      finally
      {
        IL_0017:  ldloc.0
        IL_0018:  brfalse.s  IL_0020
        IL_001a:  ldloc.0
        IL_001b:  callvirt   ""void System.IDisposable.Dispose()""
        IL_0020:  endfinally
      }
    }
    catch object
    {
      IL_0021:  pop
      IL_0022:  newobj     ""C1..ctor()""
      IL_0027:  dup
      IL_0028:  ldstr      ""Catch""
      IL_002d:  callvirt   ""void C1.Text.set""
      IL_0032:  stloc.1
      .try
      {
        IL_0033:  leave.s    IL_003f
      }
      finally
      {
        IL_0035:  ldloc.1
        IL_0036:  brfalse.s  IL_003e
        IL_0038:  ldloc.1
        IL_0039:  callvirt   ""void System.IDisposable.Dispose()""
        IL_003e:  endfinally
      }
      IL_003f:  leave.s    IL_005f
    }
  }
  finally
  {
    IL_0041:  newobj     ""C1..ctor()""
    IL_0046:  dup
    IL_0047:  ldstr      ""Finally""
    IL_004c:  callvirt   ""void C1.Text.set""
    IL_0051:  stloc.2
    .try
    {
      IL_0052:  leave.s    IL_005e
    }
    finally
    {
      IL_0054:  ldloc.2
      IL_0055:  brfalse.s  IL_005d
      IL_0057:  ldloc.2
      IL_0058:  callvirt   ""void System.IDisposable.Dispose()""
      IL_005d:  endfinally
    }
    IL_005e:  endfinally
  }
  IL_005f:  ret
}
");
        }

        [Fact]
        public void InsideTryCatchFinallyBlocksAsync()
        {
            string source = @"
using System;
using System.Threading.Tasks;
class C1 : IAsyncDisposable
{
    public string Text { get; set; }

    public C1(string text)
    {
        Text = text;
        Console.WriteLine($""Created {Text}"");
    }

    public ValueTask DisposeAsync()
    {
        Console.WriteLine($""Dispose Async {Text}"");
        return new ValueTask(Task.CompletedTask);
    }
}
class C2
{
    public static async Task Main()                                                                                                           
    {
        try
        {
            await using var x = new C1(""Try"");
            throw new Exception();
        }
        catch
        {
            await using var x =  new C1(""Catch"");
        }
        finally
        {
            await using var x = new C1(""Finally"");
        }
    }
}";
            string expectedOutput = @"
Created Try
Dispose Async Try
Created Catch
Dispose Async Catch
Created Finally
Dispose Async Finally
";
            var compilation = CreateCompilationWithTasksExtensions(source + CSharpTestBase.IAsyncDisposableDefinition, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void UsingDeclarationUsingPatternIntersectionEmitTest()
        {
            var source = @"
    using System;
    ref struct S1
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
            using S1 s1 = new S1();
            s1.M();
        }
    }";

            var output = @"This method has run.
This object has been properly disposed.";
            CompileAndVerify(source, expectedOutput: output).VerifyIL("Program.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (S1 V_0) //s1
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S1""
  .try
  {
    IL_0008:  ldloca.s   V_0
    IL_000a:  call       ""void S1.M()""
    IL_000f:  leave.s    IL_0019
  }
  finally
  {
    IL_0011:  ldloca.s   V_0
    IL_0013:  call       ""void S1.Dispose()""
    IL_0018:  endfinally
  }
  IL_0019:  ret
}
");
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
        public void UsingDeclarationUsingPatternExtensionMethod()
        {
            var source = @"
    using System;
    ref struct S1
    {
    }
    internal static class C2
    {
        internal static void Dispose(this S1 s1)
        {
            Console.Write(""Disposed; "");
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            using S1 s1 = new S1();
        }
    }";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (17,13): error CS1674: 'S1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or implement a suitable 'Dispose' method.
                //             using S1 s1 = new S1();
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "using S1 s1 = new S1();").WithArguments("S1").WithLocation(17, 13)
                );
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

        [Fact]
        public void JumpBackOverUsingDeclaration()
        {
            string source = @"
using System;
class C1 : IDisposable
{
    private string name;
    public C1(string name)
    {
        this.name = name;
    }
    public void Dispose()
    {
        Console.WriteLine(""Disposed "" + name);
    }
}
class C2
{
    public static void Main()                                                                                                           
    {
        int x = 0;
        label1:
        {
            using C1 o1 = new C1(""first"");
            if(x++ < 3)
            {
                goto label1;
            }
        }
    }
}";
            var output = @"Disposed first
Disposed first
Disposed first
Disposed first";
            CompileAndVerify(source, expectedOutput: output).VerifyIL("C2.Main", @"
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (int V_0, //x
                C1 V_1) //o1
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldstr      ""first""
  IL_0007:  newobj     ""C1..ctor(string)""
  IL_000c:  stloc.1
  .try
  {
    IL_000d:  ldloc.0
    IL_000e:  dup
    IL_000f:  ldc.i4.1
    IL_0010:  add
    IL_0011:  stloc.0
    IL_0012:  ldc.i4.3
    IL_0013:  bge.s      IL_0017
    IL_0015:  leave.s    IL_0002
    IL_0017:  leave.s    IL_0023
  }
  finally
  {
    IL_0019:  ldloc.1
    IL_001a:  brfalse.s  IL_0022
    IL_001c:  ldloc.1
    IL_001d:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0022:  endfinally
  }
  IL_0023:  ret
}
");
        }

        [Fact]
        public void UsingVariableFromAwaitExpressionDisposesOnlyIfAwaitSucceeds()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C2 : IDisposable
{
    public void Dispose()
    {
        Console.Write($""Dispose; "");
    }
}

class C
{
    static Task<IDisposable> GetDisposable()
    {
        return Task.FromResult<IDisposable>(new C2());
    }

    static Task<IDisposable> GetDisposableError()
    {
        throw null;
    }

    static async Task Main()
    {
        try
        {
            using IDisposable x = await GetDisposable(); // disposed
            using IDisposable y = await GetDisposableError(); // not disposed as never assigned
        }
        catch { } 
    }
}
";
            CompileAndVerify(source, expectedOutput: "Dispose; ");
        }

        [Fact]
        public void UsingDeclarationAsync()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C1 : IAsyncDisposable
{
    public ValueTask DisposeAsync() 
    { 
        Console.WriteLine(""Dispose async"");
        return new ValueTask(Task.CompletedTask);
    }
}

class C2
{
    static async Task Main()
    {
        await using C1 c = new C1();
    }
}";
            var compilation = CreateCompilationWithTasksExtensions(source + CSharpTestBase.IAsyncDisposableDefinition, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: "Dispose async");
        }

        [Fact]
        public void UsingDeclarationAsyncExplicit()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C1 : IAsyncDisposable
{
    ValueTask IAsyncDisposable.DisposeAsync() 
    { 
        Console.WriteLine(""Dispose async"");
        return new ValueTask(Task.CompletedTask);
    }
}

class C2
{
    static async Task Main()
    {
        await using C1 c = new C1();
    }
}";
            var compilation = CreateCompilationWithTasksExtensions(source + CSharpTestBase.IAsyncDisposableDefinition, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: "Dispose async");
        }

        [Fact]
        public void UsingDeclarationAsyncWithMultipleDeclarations()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C1 : IAsyncDisposable
{
    string text;

    public C1(string text)
    {
        this.text = text;
        Console.WriteLine($""Created {text}"");
    }

    public ValueTask DisposeAsync() 
    { 
        Console.WriteLine($""Dispose async {text}"");
        return new ValueTask(Task.CompletedTask);
    }
}

class C2
{
    static async Task Main()
    {
        await using C1 c = new C1(""first""), c2 = new C1(""second""), c3 = new C1(""third"");
        Console.WriteLine(""After declarations"");
    }
}";
            string expectedOutput = @"
Created first
Created second
Created third
After declarations
Dispose async third
Dispose async second
Dispose async first
";
            var compilation = CreateCompilationWithTasksExtensions(source + CSharpTestBase.IAsyncDisposableDefinition, options: TestOptions.DebugExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void UsingDeclarationAsyncWithMultipleInARow()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C1 : IAsyncDisposable
{
    string text;

    public C1(string text)
    {
        this.text = text;
        Console.WriteLine($""Created {text}"");
    }

    public ValueTask DisposeAsync() 
    { 
        Console.WriteLine($""Dispose async {text}"");
        return new ValueTask(Task.CompletedTask);
    }
}

class C2
{
    static async Task Main()
    {
        await using C1 c1 = new C1(""first"");
        await using C1 c2 = new C1(""second"");
        await using C1 c3 = new C1(""third"");
        Console.WriteLine(""After declarations"");
    }
}";
            string expectedOutput = @"
Created first
Created second
Created third
After declarations
Dispose async third
Dispose async second
Dispose async first
";

            var compilation = CreateCompilationWithTasksExtensions(source + CSharpTestBase.IAsyncDisposableDefinition, options: TestOptions.DebugExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void UsingDeclarationWithNull()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C1 : IDisposable
{
    public void Dispose() 
    {
        Console.Write(""Dispose; "");
    }
}

 class C2 : IAsyncDisposable
{
    public ValueTask DisposeAsync() 
    { 
        System.Console.WriteLine(""Dispose async"");
        return new ValueTask(Task.CompletedTask);
    }
}

class C3
{
    static async Task Main()
    {
        using C1 c1 = null; 
        await using C2 c2 = null;
        Console.Write(""After declarations; "");
    }
}";
            var compilation = CreateCompilationWithTasksExtensions(source + CSharpTestBase.IAsyncDisposableDefinition, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: "After declarations; ");
        }

        [Fact]
        public void UsingDeclarationAsyncMissingValueTask()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C1 : IAsyncDisposable
{
    public ValueTask DisposeAsync() 
    { 
        return new ValueTask(Task.CompletedTask);
    }
}

class C2
{
    static async Task Main()
    {
        await using C1 c1 = new C1();
    }
}";

            var comp = CreateCompilationWithTasksExtensions(source + CSharpTestBase.IAsyncDisposableDefinition);
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_ValueTask);
            comp.VerifyDiagnostics(
                // (16,9): error CS0518: Predefined type 'System.Threading.Tasks.ValueTask' is not defined or imported
                //         await using C1 c1 = new C1();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.Threading.Tasks.ValueTask").WithLocation(16, 9)
                );
        }
    }
}
