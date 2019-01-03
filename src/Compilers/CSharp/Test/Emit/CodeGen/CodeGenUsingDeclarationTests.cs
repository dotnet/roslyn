// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenUsingDeclarationTests : EmitMetadataTestBase
    {
        private const string _asyncDisposable = @"
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask DisposeAsync();
    }
}";

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
            var compilation = CreateCompilationWithTasksExtensions(source + _asyncDisposable, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: expectedOutput).VerifyIL("C2.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      906 (0x38a)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_2,
                System.Threading.Tasks.ValueTask V_3,
                C2.<Main>d__0 V_4,
                System.Exception V_5,
                int V_6,
                object V_7,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_8,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_9)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C2.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.1
    IL_0009:  ble.un.s   IL_0013
    IL_000b:  br.s       IL_000d
    IL_000d:  ldloc.0
    IL_000e:  ldc.i4.2
    IL_000f:  beq.s      IL_0015
    IL_0011:  br.s       IL_001a
    IL_0013:  br.s       IL_0029
    IL_0015:  br         IL_02be
    IL_001a:  nop
    IL_001b:  ldarg.0
    IL_001c:  ldnull
    IL_001d:  stfld      ""object C2.<Main>d__0.<>s__1""
    IL_0022:  ldarg.0
    IL_0023:  ldc.i4.0
    IL_0024:  stfld      ""int C2.<Main>d__0.<>s__2""
    IL_0029:  nop
    .try
    {
      IL_002a:  ldloc.0
      IL_002b:  brfalse.s  IL_0035
      IL_002d:  br.s       IL_002f
      IL_002f:  ldloc.0
      IL_0030:  ldc.i4.1
      IL_0031:  beq.s      IL_0037
      IL_0033:  br.s       IL_003c
      IL_0035:  br.s       IL_0043
      IL_0037:  br         IL_01c7
      IL_003c:  ldarg.0
      IL_003d:  ldc.i4.0
      IL_003e:  stfld      ""int C2.<Main>d__0.<>s__4""
      IL_0043:  nop
      .try
      {
        IL_0044:  ldloc.0
        IL_0045:  brfalse.s  IL_0049
        IL_0047:  br.s       IL_004b
        IL_0049:  br.s       IL_00c7
        IL_004b:  nop
        IL_004c:  ldarg.0
        IL_004d:  ldstr      ""Try""
        IL_0052:  newobj     ""C1..ctor(string)""
        IL_0057:  stfld      ""C1 C2.<Main>d__0.<x>5__5""
        IL_005c:  ldarg.0
        IL_005d:  ldnull
        IL_005e:  stfld      ""object C2.<Main>d__0.<>s__6""
        IL_0063:  ldarg.0
        IL_0064:  ldc.i4.0
        IL_0065:  stfld      ""int C2.<Main>d__0.<>s__7""
        .try
        {
          IL_006a:  newobj     ""System.Exception..ctor()""
          IL_006f:  throw
        }
        catch object
        {
          IL_0070:  stloc.1
          IL_0071:  ldarg.0
          IL_0072:  ldloc.1
          IL_0073:  stfld      ""object C2.<Main>d__0.<>s__6""
          IL_0078:  leave.s    IL_007a
        }
        IL_007a:  ldarg.0
        IL_007b:  ldfld      ""C1 C2.<Main>d__0.<x>5__5""
        IL_0080:  brfalse.s  IL_00eb
        IL_0082:  ldarg.0
        IL_0083:  ldfld      ""C1 C2.<Main>d__0.<x>5__5""
        IL_0088:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
        IL_008d:  stloc.3
        IL_008e:  ldloca.s   V_3
        IL_0090:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
        IL_0095:  stloc.2
        IL_0096:  ldloca.s   V_2
        IL_0098:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
        IL_009d:  brtrue.s   IL_00e3
        IL_009f:  ldarg.0
        IL_00a0:  ldc.i4.0
        IL_00a1:  dup
        IL_00a2:  stloc.0
        IL_00a3:  stfld      ""int C2.<Main>d__0.<>1__state""
        IL_00a8:  ldarg.0
        IL_00a9:  ldloc.2
        IL_00aa:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C2.<Main>d__0.<>u__1""
        IL_00af:  ldarg.0
        IL_00b0:  stloc.s    V_4
        IL_00b2:  ldarg.0
        IL_00b3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C2.<Main>d__0.<>t__builder""
        IL_00b8:  ldloca.s   V_2
        IL_00ba:  ldloca.s   V_4
        IL_00bc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C2.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C2.<Main>d__0)""
        IL_00c1:  nop
        IL_00c2:  leave      IL_0389
        IL_00c7:  ldarg.0
        IL_00c8:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C2.<Main>d__0.<>u__1""
        IL_00cd:  stloc.2
        IL_00ce:  ldarg.0
        IL_00cf:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C2.<Main>d__0.<>u__1""
        IL_00d4:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
        IL_00da:  ldarg.0
        IL_00db:  ldc.i4.m1
        IL_00dc:  dup
        IL_00dd:  stloc.0
        IL_00de:  stfld      ""int C2.<Main>d__0.<>1__state""
        IL_00e3:  ldloca.s   V_2
        IL_00e5:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
        IL_00ea:  nop
        IL_00eb:  ldarg.0
        IL_00ec:  ldfld      ""object C2.<Main>d__0.<>s__6""
        IL_00f1:  stloc.1
        IL_00f2:  ldloc.1
        IL_00f3:  brfalse.s  IL_0110
        IL_00f5:  ldloc.1
        IL_00f6:  isinst     ""System.Exception""
        IL_00fb:  stloc.s    V_5
        IL_00fd:  ldloc.s    V_5
        IL_00ff:  brtrue.s   IL_0103
        IL_0101:  ldloc.1
        IL_0102:  throw
        IL_0103:  ldloc.s    V_5
        IL_0105:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
        IL_010a:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
        IL_010f:  nop
        IL_0110:  ldarg.0
        IL_0111:  ldfld      ""int C2.<Main>d__0.<>s__7""
        IL_0116:  pop
        IL_0117:  ldarg.0
        IL_0118:  ldnull
        IL_0119:  stfld      ""object C2.<Main>d__0.<>s__6""
        IL_011e:  nop
        IL_011f:  ldarg.0
        IL_0120:  ldnull
        IL_0121:  stfld      ""C1 C2.<Main>d__0.<x>5__5""
        IL_0126:  leave.s    IL_0139
      }
      catch object
      {
        IL_0128:  stloc.1
        IL_0129:  ldarg.0
        IL_012a:  ldloc.1
        IL_012b:  stfld      ""object C2.<Main>d__0.<>s__3""
        IL_0130:  ldarg.0
        IL_0131:  ldc.i4.1
        IL_0132:  stfld      ""int C2.<Main>d__0.<>s__4""
        IL_0137:  leave.s    IL_0139
      }
      IL_0139:  ldarg.0
      IL_013a:  ldfld      ""int C2.<Main>d__0.<>s__4""
      IL_013f:  stloc.s    V_6
      IL_0141:  ldloc.s    V_6
      IL_0143:  ldc.i4.1
      IL_0144:  beq.s      IL_014b
      IL_0146:  br         IL_022d
      IL_014b:  nop
      IL_014c:  ldarg.0
      IL_014d:  ldstr      ""Catch""
      IL_0152:  newobj     ""C1..ctor(string)""
      IL_0157:  stfld      ""C1 C2.<Main>d__0.<x>5__8""
      IL_015c:  ldarg.0
      IL_015d:  ldnull
      IL_015e:  stfld      ""object C2.<Main>d__0.<>s__9""
      IL_0163:  ldarg.0
      IL_0164:  ldc.i4.0
      IL_0165:  stfld      ""int C2.<Main>d__0.<>s__10""
      .try
      {
        IL_016a:  leave.s    IL_0178
      }
      catch object
      {
        IL_016c:  stloc.s    V_7
        IL_016e:  ldarg.0
        IL_016f:  ldloc.s    V_7
        IL_0171:  stfld      ""object C2.<Main>d__0.<>s__9""
        IL_0176:  leave.s    IL_0178
      }
      IL_0178:  ldarg.0
      IL_0179:  ldfld      ""C1 C2.<Main>d__0.<x>5__8""
      IL_017e:  brfalse.s  IL_01ec
      IL_0180:  ldarg.0
      IL_0181:  ldfld      ""C1 C2.<Main>d__0.<x>5__8""
      IL_0186:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
      IL_018b:  stloc.3
      IL_018c:  ldloca.s   V_3
      IL_018e:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
      IL_0193:  stloc.s    V_8
      IL_0195:  ldloca.s   V_8
      IL_0197:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
      IL_019c:  brtrue.s   IL_01e4
      IL_019e:  ldarg.0
      IL_019f:  ldc.i4.1
      IL_01a0:  dup
      IL_01a1:  stloc.0
      IL_01a2:  stfld      ""int C2.<Main>d__0.<>1__state""
      IL_01a7:  ldarg.0
      IL_01a8:  ldloc.s    V_8
      IL_01aa:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C2.<Main>d__0.<>u__1""
      IL_01af:  ldarg.0
      IL_01b0:  stloc.s    V_4
      IL_01b2:  ldarg.0
      IL_01b3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C2.<Main>d__0.<>t__builder""
      IL_01b8:  ldloca.s   V_8
      IL_01ba:  ldloca.s   V_4
      IL_01bc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C2.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C2.<Main>d__0)""
      IL_01c1:  nop
      IL_01c2:  leave      IL_0389
      IL_01c7:  ldarg.0
      IL_01c8:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C2.<Main>d__0.<>u__1""
      IL_01cd:  stloc.s    V_8
      IL_01cf:  ldarg.0
      IL_01d0:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C2.<Main>d__0.<>u__1""
      IL_01d5:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
      IL_01db:  ldarg.0
      IL_01dc:  ldc.i4.m1
      IL_01dd:  dup
      IL_01de:  stloc.0
      IL_01df:  stfld      ""int C2.<Main>d__0.<>1__state""
      IL_01e4:  ldloca.s   V_8
      IL_01e6:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
      IL_01eb:  nop
      IL_01ec:  ldarg.0
      IL_01ed:  ldfld      ""object C2.<Main>d__0.<>s__9""
      IL_01f2:  stloc.s    V_7
      IL_01f4:  ldloc.s    V_7
      IL_01f6:  brfalse.s  IL_0215
      IL_01f8:  ldloc.s    V_7
      IL_01fa:  isinst     ""System.Exception""
      IL_01ff:  stloc.s    V_5
      IL_0201:  ldloc.s    V_5
      IL_0203:  brtrue.s   IL_0208
      IL_0205:  ldloc.s    V_7
      IL_0207:  throw
      IL_0208:  ldloc.s    V_5
      IL_020a:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
      IL_020f:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
      IL_0214:  nop
      IL_0215:  ldarg.0
      IL_0216:  ldfld      ""int C2.<Main>d__0.<>s__10""
      IL_021b:  pop
      IL_021c:  ldarg.0
      IL_021d:  ldnull
      IL_021e:  stfld      ""object C2.<Main>d__0.<>s__9""
      IL_0223:  nop
      IL_0224:  ldarg.0
      IL_0225:  ldnull
      IL_0226:  stfld      ""C1 C2.<Main>d__0.<x>5__8""
      IL_022b:  br.s       IL_022d
      IL_022d:  ldarg.0
      IL_022e:  ldnull
      IL_022f:  stfld      ""object C2.<Main>d__0.<>s__3""
      IL_0234:  leave.s    IL_0242
    }
    catch object
    {
      IL_0236:  stloc.s    V_7
      IL_0238:  ldarg.0
      IL_0239:  ldloc.s    V_7
      IL_023b:  stfld      ""object C2.<Main>d__0.<>s__1""
      IL_0240:  leave.s    IL_0242
    }
    IL_0242:  nop
    IL_0243:  ldarg.0
    IL_0244:  ldstr      ""Finally""
    IL_0249:  newobj     ""C1..ctor(string)""
    IL_024e:  stfld      ""C1 C2.<Main>d__0.<x>5__11""
    IL_0253:  ldarg.0
    IL_0254:  ldnull
    IL_0255:  stfld      ""object C2.<Main>d__0.<>s__12""
    IL_025a:  ldarg.0
    IL_025b:  ldc.i4.0
    IL_025c:  stfld      ""int C2.<Main>d__0.<>s__13""
    .try
    {
      IL_0261:  leave.s    IL_026f
    }
    catch object
    {
      IL_0263:  stloc.s    V_7
      IL_0265:  ldarg.0
      IL_0266:  ldloc.s    V_7
      IL_0268:  stfld      ""object C2.<Main>d__0.<>s__12""
      IL_026d:  leave.s    IL_026f
    }
    IL_026f:  ldarg.0
    IL_0270:  ldfld      ""C1 C2.<Main>d__0.<x>5__11""
    IL_0275:  brfalse.s  IL_02e3
    IL_0277:  ldarg.0
    IL_0278:  ldfld      ""C1 C2.<Main>d__0.<x>5__11""
    IL_027d:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_0282:  stloc.3
    IL_0283:  ldloca.s   V_3
    IL_0285:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_028a:  stloc.s    V_9
    IL_028c:  ldloca.s   V_9
    IL_028e:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_0293:  brtrue.s   IL_02db
    IL_0295:  ldarg.0
    IL_0296:  ldc.i4.2
    IL_0297:  dup
    IL_0298:  stloc.0
    IL_0299:  stfld      ""int C2.<Main>d__0.<>1__state""
    IL_029e:  ldarg.0
    IL_029f:  ldloc.s    V_9
    IL_02a1:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C2.<Main>d__0.<>u__1""
    IL_02a6:  ldarg.0
    IL_02a7:  stloc.s    V_4
    IL_02a9:  ldarg.0
    IL_02aa:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C2.<Main>d__0.<>t__builder""
    IL_02af:  ldloca.s   V_9
    IL_02b1:  ldloca.s   V_4
    IL_02b3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C2.<Main>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C2.<Main>d__0)""
    IL_02b8:  nop
    IL_02b9:  leave      IL_0389
    IL_02be:  ldarg.0
    IL_02bf:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C2.<Main>d__0.<>u__1""
    IL_02c4:  stloc.s    V_9
    IL_02c6:  ldarg.0
    IL_02c7:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C2.<Main>d__0.<>u__1""
    IL_02cc:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_02d2:  ldarg.0
    IL_02d3:  ldc.i4.m1
    IL_02d4:  dup
    IL_02d5:  stloc.0
    IL_02d6:  stfld      ""int C2.<Main>d__0.<>1__state""
    IL_02db:  ldloca.s   V_9
    IL_02dd:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_02e2:  nop
    IL_02e3:  ldarg.0
    IL_02e4:  ldfld      ""object C2.<Main>d__0.<>s__12""
    IL_02e9:  stloc.s    V_7
    IL_02eb:  ldloc.s    V_7
    IL_02ed:  brfalse.s  IL_030c
    IL_02ef:  ldloc.s    V_7
    IL_02f1:  isinst     ""System.Exception""
    IL_02f6:  stloc.s    V_5
    IL_02f8:  ldloc.s    V_5
    IL_02fa:  brtrue.s   IL_02ff
    IL_02fc:  ldloc.s    V_7
    IL_02fe:  throw
    IL_02ff:  ldloc.s    V_5
    IL_0301:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_0306:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_030b:  nop
    IL_030c:  ldarg.0
    IL_030d:  ldfld      ""int C2.<Main>d__0.<>s__13""
    IL_0312:  pop
    IL_0313:  ldarg.0
    IL_0314:  ldnull
    IL_0315:  stfld      ""object C2.<Main>d__0.<>s__12""
    IL_031a:  nop
    IL_031b:  ldarg.0
    IL_031c:  ldnull
    IL_031d:  stfld      ""C1 C2.<Main>d__0.<x>5__11""
    IL_0322:  ldarg.0
    IL_0323:  ldfld      ""object C2.<Main>d__0.<>s__1""
    IL_0328:  stloc.s    V_7
    IL_032a:  ldloc.s    V_7
    IL_032c:  brfalse.s  IL_034b
    IL_032e:  ldloc.s    V_7
    IL_0330:  isinst     ""System.Exception""
    IL_0335:  stloc.s    V_5
    IL_0337:  ldloc.s    V_5
    IL_0339:  brtrue.s   IL_033e
    IL_033b:  ldloc.s    V_7
    IL_033d:  throw
    IL_033e:  ldloc.s    V_5
    IL_0340:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_0345:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_034a:  nop
    IL_034b:  ldarg.0
    IL_034c:  ldfld      ""int C2.<Main>d__0.<>s__2""
    IL_0351:  pop
    IL_0352:  ldarg.0
    IL_0353:  ldnull
    IL_0354:  stfld      ""object C2.<Main>d__0.<>s__1""
    IL_0359:  leave.s    IL_0375
  }
  catch System.Exception
  {
    IL_035b:  stloc.s    V_5
    IL_035d:  ldarg.0
    IL_035e:  ldc.i4.s   -2
    IL_0360:  stfld      ""int C2.<Main>d__0.<>1__state""
    IL_0365:  ldarg.0
    IL_0366:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C2.<Main>d__0.<>t__builder""
    IL_036b:  ldloc.s    V_5
    IL_036d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0372:  nop
    IL_0373:  leave.s    IL_0389
  }
  IL_0375:  ldarg.0
  IL_0376:  ldc.i4.s   -2
  IL_0378:  stfld      ""int C2.<Main>d__0.<>1__state""
  IL_037d:  ldarg.0
  IL_037e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C2.<Main>d__0.<>t__builder""
  IL_0383:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0388:  nop
  IL_0389:  ret
}
");
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

            var output = @"Disposed; ";
            CompileAndVerify(source, expectedOutput: output).VerifyIL("Program.Main", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (S1 V_0) //s1
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S1""
  .try
  {
    IL_0008:  leave.s    IL_0011
  }
  finally
  {
    IL_000a:  ldloc.0
    IL_000b:  call       ""void C2.Dispose(S1)""
    IL_0010:  endfinally
  }
  IL_0011:  ret
}
");
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
            var compilation = CreateCompilationWithTasksExtensions(source + _asyncDisposable, options: TestOptions.DebugExe).VerifyDiagnostics();

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
            var compilation = CreateCompilationWithTasksExtensions(source + _asyncDisposable, options: TestOptions.DebugExe).VerifyDiagnostics();

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
            var compilation = CreateCompilationWithTasksExtensions(source + _asyncDisposable, options: TestOptions.DebugExe).VerifyDiagnostics();
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

            var compilation = CreateCompilationWithTasksExtensions(source + _asyncDisposable, options: TestOptions.DebugExe).VerifyDiagnostics();
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
            var compilation = CreateCompilationWithTasksExtensions(source + _asyncDisposable, options: TestOptions.DebugExe).VerifyDiagnostics();

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

            var comp = CreateCompilationWithTasksExtensions(source + _asyncDisposable);
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_ValueTask);
            comp.VerifyDiagnostics(
                // (16,9): error CS0518: Predefined type 'System.Threading.Tasks.ValueTask' is not defined or imported
                //         await using C1 c1 = new C1();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.Threading.Tasks.ValueTask").WithLocation(16, 9)
                );
        }
    }
}
