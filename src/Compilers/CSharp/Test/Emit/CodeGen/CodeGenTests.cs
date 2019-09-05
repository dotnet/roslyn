// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenTests : CSharpTestBase
    {
        [Fact]
        public void EmitWithSuppressedWarnAsError()
        {
            var src = @"
#pragma warning disable 1591

public class P {
    public static void Main() {}
}";
            var parseOptions = TestOptions.RegularWithDocumentationComments;
            var options = TestOptions.ReleaseDll
                .WithXmlReferenceResolver(XmlFileResolver.Default)
                .WithGeneralDiagnosticOption(ReportDiagnostic.Error);

            var comp = CreateCompilation(src, parseOptions: parseOptions, options: options);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp);
        }

        [Fact()]
        [WorkItem(776642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/776642")]
        public void Bug776642a()
        {
            const string source = @"
using System;
using System.Collections.Generic;

struct TwoInteger
{   
    public int x;
    public int y;
}

struct DoubleAndStruct
{
    public double x;
    public TwoInteger y;
}

class Program
{
    static void Main(string[] args)
    {
    }

    static void Main(object[] args)
    {
        Object trackArg1, trackArg2;
        DoubleAndStruct localArg1 = default(DoubleAndStruct);
        DoubleAndStruct localArg2 = default(DoubleAndStruct);
        args = new Object[] { localArg1, localArg2 };
        trackArg1 = args[0];
        trackArg2 = args[1];
        Console.WriteLine((((DoubleAndStruct)args[0]).y).x);
    }
}";
            var result = CompileAndVerify(source, options: TestOptions.DebugDll);

            result.VerifyIL("Program.Main(object[])",
@"
{
  // Code size       76 (0x4c)
  .maxstack  4
  .locals init (object V_0, //trackArg1
  object V_1, //trackArg2
  DoubleAndStruct V_2, //localArg1
  DoubleAndStruct V_3) //localArg2
  IL_0000:  nop
  IL_0001:  ldloca.s   V_2
  IL_0003:  initobj    ""DoubleAndStruct""
  IL_0009:  ldloca.s   V_3
  IL_000b:  initobj    ""DoubleAndStruct""
  IL_0011:  ldc.i4.2
  IL_0012:  newarr     ""object""
  IL_0017:  dup
  IL_0018:  ldc.i4.0
  IL_0019:  ldloc.2
  IL_001a:  box        ""DoubleAndStruct""
  IL_001f:  stelem.ref
  IL_0020:  dup
  IL_0021:  ldc.i4.1
  IL_0022:  ldloc.3
  IL_0023:  box        ""DoubleAndStruct""
  IL_0028:  stelem.ref
  IL_0029:  starg.s    V_0
  IL_002b:  ldarg.0
  IL_002c:  ldc.i4.0
  IL_002d:  ldelem.ref
  IL_002e:  stloc.0
  IL_002f:  ldarg.0
  IL_0030:  ldc.i4.1
  IL_0031:  ldelem.ref
  IL_0032:  stloc.1
  IL_0033:  ldarg.0
  IL_0034:  ldc.i4.0
  IL_0035:  ldelem.ref
  IL_0036:  unbox      ""DoubleAndStruct""
  IL_003b:  ldflda     ""TwoInteger DoubleAndStruct.y""
  IL_0040:  ldfld      ""int TwoInteger.x""
  IL_0045:  call       ""void System.Console.WriteLine(int)""
  IL_004a:  nop
  IL_004b:  ret
}
");
        }

        [Fact()]
        [WorkItem(776642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/776642")]
        public void Bug776642b()
        {
            const string source = @"
using System;
using System.Collections.Generic;

struct TwoInteger
{
    public int x;
    public int y;
}

struct DoubleAndStruct
{
    public double x;
    public TwoInteger y;
}

struct OuterStruct
{
    public DoubleAndStruct z;
}

class Program
{
    static void Main(string[] args)
    {
    }

    static void Main(object[] args)
    {
        args = new Object[] { default(OuterStruct) };
        Console.WriteLine(((((OuterStruct)args[0]).z).y).x);
    }
}";
            var result = CompileAndVerify(source, options: TestOptions.DebugDll);

            result.VerifyIL("Program.Main(object[])",
@"
{
  // Code size       56 (0x38)
  .maxstack  4
  .locals init (OuterStruct V_0)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  newarr     ""object""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    ""OuterStruct""
  IL_0011:  ldloc.0
  IL_0012:  box        ""OuterStruct""
  IL_0017:  stelem.ref
  IL_0018:  starg.s    V_0
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.0
  IL_001c:  ldelem.ref
  IL_001d:  unbox      ""OuterStruct""
  IL_0022:  ldflda     ""DoubleAndStruct OuterStruct.z""
  IL_0027:  ldflda     ""TwoInteger DoubleAndStruct.y""
  IL_002c:  ldfld      ""int TwoInteger.x""
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  nop
  IL_0037:  ret
}
");
        }

        [Fact()]
        [WorkItem(776642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/776642")]
        public void Bug776642c()
        {
            const string source = @"
using System;
using System.Collections.Generic;

struct TwoInteger
{
    public int x;
    public int y;
}

struct DoubleAndStruct
{
    public double x;
    public TwoInteger y;
}

class OuterStruct
{
    public DoubleAndStruct z;
}

class Program
{
    static void Main(string[] args)
    {
    }

    static void Main(object[] args)
    {
        args = new Object[] { default(OuterStruct) };
        Console.WriteLine(((((OuterStruct)args[0]).z).y).x);
    }
}";
            var result = CompileAndVerify(source, options: TestOptions.DebugDll);

            result.VerifyIL("Program.Main(object[])",
@"
{
  // Code size       39 (0x27)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  newarr     ""object""
  IL_0007:  starg.s    V_0
  IL_0009:  ldarg.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldelem.ref
  IL_000c:  castclass  ""OuterStruct""
  IL_0011:  ldflda     ""DoubleAndStruct OuterStruct.z""
  IL_0016:  ldflda     ""TwoInteger DoubleAndStruct.y""
  IL_001b:  ldfld      ""int TwoInteger.x""
  IL_0020:  call       ""void System.Console.WriteLine(int)""
  IL_0025:  nop
  IL_0026:  ret
}
");
        }

        [Fact()]
        [WorkItem(776642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/776642")]
        public void Bug776642_static()
        {
            const string source = @"
using System;
using System.Collections.Generic;

struct TwoInteger
{
    public int x;
    public int y;
}

struct DoubleAndStruct
{
    public double x;
    public TwoInteger y;
}

class OuterStruct
{
    public static DoubleAndStruct z;
}

class Program
{
    static void Main(string[] args)
    {
    }

    static void Main(object[] args)
    {
        Console.WriteLine(((OuterStruct.z).y).x);
    }
}";
            var result = CompileAndVerify(source, options: TestOptions.DebugDll);

            result.VerifyIL("Program.Main(object[])",
@"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldsflda    ""DoubleAndStruct OuterStruct.z""
  IL_0006:  ldflda     ""TwoInteger DoubleAndStruct.y""
  IL_000b:  ldfld      ""int TwoInteger.x""
  IL_0010:  call       ""void System.Console.WriteLine(int)""
  IL_0015:  nop
  IL_0016:  ret
}
");
        }

        [Fact()]
        [WorkItem(531366, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531366")]
        public void Bug18015()
        {
            const string source = @"
class P
{
  public object X { get; set; }
  public void Y() { }
  public object M<T>(T t) where T : C
  {
    ((P)(object)t).Y();
    return ((P)(object)t).X;
  }
}
public class C { }
";
            var result = CompileAndVerify(source, options: TestOptions.ReleaseDll);

            result.VerifyIL("P.M<T>(T)",
@"
{
  // Code size       33 (0x21)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  box        ""T""
  IL_0006:  castclass  ""P""
  IL_000b:  callvirt   ""void P.Y()""
  IL_0010:  ldarg.1
  IL_0011:  box        ""T""
  IL_0016:  castclass  ""P""
  IL_001b:  callvirt   ""object P.X.get""
  IL_0020:  ret
}
");
        }

        [Fact()]
        [WorkItem(546857, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546857")]
        public void Bug16996_MissingReturnValue()
        {
            string source = @"
using System;

public class C
{
	public static bool f;
	public static int x;
	
	public bool M()
	{
		bool success = f;

		if (success)
		{
			x = 1;
		}
		else
		{
			throw null;
		}

		return success;
	}
}";
            var result = CompileAndVerify(source, options: TestOptions.ReleaseDll);

            result.VerifyIL("C.M",
@"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldsfld     ""bool C.f""
  IL_0005:  dup
  IL_0006:  brfalse.s  IL_000f
  IL_0008:  ldc.i4.1
  IL_0009:  stsfld     ""int C.x""
  IL_000e:  ret
  IL_000f:  ldnull
  IL_0010:  throw
}
");
        }

        [Fact()]
        [WorkItem(546857, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546857")]
        public void Bug16996_MissingReturnValue01()
        {
            string source = @"
using System;

public class C
{
	public static bool f;
	public static int x;
	
    public void M()
    {
        bool success = f;

        if (success)
        {
            x = 1;
        }
        else
        {
            x = 2;
            goto L1;
        }

        return;

    L1:
        throw null;

    }
}
";
            var result = CompileAndVerify(source, options: TestOptions.ReleaseDll);

            result.VerifyIL("C.M",
@"
{
  // Code size       22 (0x16)
  .maxstack  1
  IL_0000:  ldsfld     ""bool C.f""
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldc.i4.1
  IL_0008:  stsfld     ""int C.x""
  IL_000d:  ret
  IL_000e:  ldc.i4.2
  IL_000f:  stsfld     ""int C.x""
  IL_0014:  ldnull
  IL_0015:  throw
}
");
        }
        [WorkItem(546412, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546412")]
        [ConditionalFact(typeof(DesktopOnly))]
        public void TestBug15818()
        {
            var source =
@"using System;

class C
{
    public static void Main(string[] args)
    {
        string s = ""Nothing"";
        string[] ss = s.Split(',');
        if(ss.Length != 4){
        }
        if(ss.Rank != 4){
        }
        Console.Write('k');
    }
}";
            var tree = Parse(source);
            var compilation = CreateEmptyCompilation(tree, new[] { MscorlibRefSilverlight }, TestOptions.ReleaseExe, assemblyName: "Test");
            CompileAndVerify(compilation, expectedOutput: "k");
        }

        [WorkItem(546853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546853")]
        [Fact]
        public void TestBug16981()
        {
            var il = @"
.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual instance bool  get_M1() cil managed
  {
    .maxstack  1
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  }

  .method public hidebysig newslot specialname virtual final instance bool  get_M2() cil managed
  {
    .maxstack  1
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }

  .property instance bool M1()
  {
    .get instance bool B::get_M1()
  }

  .property instance bool M2()
  {
    .get instance bool B::get_M2()
  }
}
";
            var source = @"
using System;

class C
{
    static void A()
    {
        var x = new B().M1 && new B().M2;
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, il, TargetFramework.Mscorlib45, options: TestOptions.ReleaseDll);
            var result = CompileAndVerify(compilation);

            result.VerifyIL("C.A", @"
{
  // Code size       27 (0x1b)
  .maxstack  1
  IL_0000:  newobj     ""B..ctor()""
  IL_0005:  callvirt   ""bool B.M1.get""
  IL_000a:  brfalse.s  IL_0018
  IL_000c:  newobj     ""B..ctor()""
  IL_0011:  call       ""bool B.M2.get""
  IL_0016:  br.s       IL_0019
  IL_0018:  ldc.i4.0
  IL_0019:  pop
  IL_001a:  ret
}
");
        }
        [WorkItem(546853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546853")]
        [Fact]

        public void TestBug16981b()
        {
            var il = @"
.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual instance bool  get_M1() cil managed
  {
    .maxstack  1
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  }

  .method public hidebysig newslot specialname virtual final instance bool  get_M2() cil managed
  {
    .maxstack  1
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }

  .property instance bool M1()
  {
    .get instance bool B::get_M1()
  }

  .property instance bool M2()
  {
    .get instance bool B::get_M2()
  }
}
";
            var source = @"
using System;

class C
{
    static void A()
    {
        var b = new B();
        var x = b.M1 && b.M2;
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, il, TargetFramework.Mscorlib45, options: TestOptions.DebugDll);
            var result = CompileAndVerify(compilation);

            result.VerifyIL("C.A",
@"{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (B V_0, //b
  bool V_1) //x
  IL_0000:  nop
  IL_0001:  newobj     ""B..ctor()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  callvirt   ""bool B.M1.get""
  IL_000d:  brfalse.s  IL_0017
  IL_000f:  ldloc.0
  IL_0010:  callvirt   ""bool B.M2.get""
  IL_0015:  br.s       IL_0018
  IL_0017:  ldc.i4.0
  IL_0018:  stloc.1
  IL_0019:  ret
}
");
        }

        [Fact, WorkItem(540019, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540019")]
        public void TestBug6156()
        {
            var source = @"
class Ref1 
{ public virtual void M(ref int x) { x = 1; } }
class Out1 : Ref1
{ public virtual void M(out int x) { x = 2; } }
class Ref2 : Out1
{ public override void M(ref int x) { x = 3; } }
class Out2 : Ref2 
{ public override void M(out int x) { x = 4; } }
class M 
{
  static void Main() 
  {
    int x = 0;
    Ref1 r1;
    r1 = new Ref1();
    r1.M(ref x);
    System.Console.Write(x);
    r1 = new Out1();
    r1.M(ref x);
    System.Console.Write(x);
    r1 = new Ref2();
    r1.M(ref x);
    System.Console.Write(x);
    r1 = new Out2();
    r1.M(ref x);
    System.Console.WriteLine(x);
    Out1 o1;
    o1 = new Out1();
    o1.M(ref x);
    System.Console.Write(x);
    o1 = new Ref2();
    o1.M(ref x);
    System.Console.Write(x);
    o1 = new Out2();
    o1.M(ref x);
    System.Console.WriteLine(x);
    Ref2 r2;
    r2 = new Ref2();
    r2.M(ref x);
    System.Console.Write(x);
    r2 = new Out2();
    r2.M(ref x);
    System.Console.WriteLine(x);
    Out2 o2;
    o2 = new Out2();
    o2.M(ref x);
    System.Console.WriteLine(x);
    o1 = new Out1();
    o1.M(out x);
    System.Console.Write(x);
    o1 = new Ref2();
    o1.M(out x);
    System.Console.Write(x);
    o1 = new Out2();
    o1.M(out x);
    System.Console.WriteLine(x);
    r2 = new Ref2();
    r2.M(out x);
    System.Console.Write(x);
    r2 = new Out2();
    r2.M(out x);
    System.Console.WriteLine(x);
    o2 = new Out2();
    o2.M(out x);
    System.Console.WriteLine(x);
  }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1111
111
11
1
234
34
4
");
        }

        [Fact]
        public void TestGeneratingLocals()
        {
            var source = @"
class C 
{ 
    public static void Main() 
    { 
        int i = 0, j, k = 2147483647;
        long l = 0, m = 9200000000000000000L;
        int b = -10;
        byte c = 200;
        float f = 3.14159F;
        double d = 2.71828;
        string s = ""abcdef"";
        bool x = true;

        System.Console.WriteLine(i);
        System.Console.WriteLine(k);
        System.Console.WriteLine(b);
        System.Console.WriteLine(c);
        System.Console.WriteLine(f);
        System.Console.WriteLine(d);
        System.Console.WriteLine(s);
        System.Console.WriteLine(x);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
0
2147483647
-10
200
3.14159
2.71828
abcdef
True
");

            compilation.VerifyIL("C.Main", @"{
  // Code size       94 (0x5e)
  .maxstack  2
  .locals init (int V_0, //i
  int V_1, //k
  byte V_2, //c
  float V_3, //f
  double V_4, //d
  string V_5, //s
  bool V_6) //x
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4     0x7fffffff
  IL_0007:  stloc.1
  IL_0008:  ldc.i4.s   -10
  IL_000a:  ldc.i4     0xc8
  IL_000f:  stloc.2
  IL_0010:  ldc.r4     3.14159
  IL_0015:  stloc.3
  IL_0016:  ldc.r8     2.71828
  IL_001f:  stloc.s    V_4
  IL_0021:  ldstr      ""abcdef""
  IL_0026:  stloc.s    V_5
  IL_0028:  ldc.i4.1
  IL_0029:  stloc.s    V_6
  IL_002b:  ldloc.0
  IL_002c:  call       ""void System.Console.WriteLine(int)""
  IL_0031:  ldloc.1
  IL_0032:  call       ""void System.Console.WriteLine(int)""
  IL_0037:  call       ""void System.Console.WriteLine(int)""
  IL_003c:  ldloc.2
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  ldloc.3
  IL_0043:  call       ""void System.Console.WriteLine(float)""
  IL_0048:  ldloc.s    V_4
  IL_004a:  call       ""void System.Console.WriteLine(double)""
  IL_004f:  ldloc.s    V_5
  IL_0051:  call       ""void System.Console.WriteLine(string)""
  IL_0056:  ldloc.s    V_6
  IL_0058:  call       ""void System.Console.WriteLine(bool)""
  IL_005d:  ret
}
");
        }

        [WorkItem(546749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546749")]
        [Fact()]
        public void TestToStringOnStruct()
        {
            var il = @"
.class sequential ansi sealed public Struct1
         extends [mscorlib]System.ValueType
{
    .method public hidebysig virtual instance string 
            ToString() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      .locals init (string V_0)
      IL_0000:  nop
      IL_0001:  ldstr      ""Struct1 ""
      IL_0006:  stloc.0
      IL_0007:  br.s       IL_0009
      IL_0009:  ldloc.0
      IL_000a:  ret
    }
}
.class sequential ansi sealed public Struct2
         extends [mscorlib]System.ValueType
{
    .method public strict virtual instance string
            ToString() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      .locals init (string V_0)
      IL_0000:  nop
      IL_0001:  ldstr      ""Struct2 ""
      IL_0006:  stloc.0
      IL_0007:  br.s       IL_0009
      IL_0009:  ldloc.0
      IL_000a:  ret
    }
}
";
            var source = @"
using System;

class Clazz
{ 
    public static void Main(string[] args)
    { 
        Struct1 s1 = new Struct1();
        Console.Write(s1.ToString());
        Struct2 s2 = new Struct2();
        Console.Write(s2.ToString());
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, il, TargetFramework.Mscorlib45, options: TestOptions.ReleaseExe);
            var result = CompileAndVerify(compilation, expectedOutput: "Struct1 Struct2 ");

            result.VerifyIL("Clazz.Main", @"
{
  // Code size       47 (0x2f)
  .maxstack  1
  .locals init (Struct1 V_0, //s1
  Struct2 V_1) //s2
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""Struct1""
  IL_0008:  ldloca.s   V_0
  IL_000a:  constrained. ""Struct1""
  IL_0010:  callvirt   ""string object.ToString()""
  IL_0015:  call       ""void System.Console.Write(string)""
  IL_001a:  ldloca.s   V_1
  IL_001c:  initobj    ""Struct2""
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       ""string Struct2.ToString()""
  IL_0029:  call       ""void System.Console.Write(string)""
  IL_002e:  ret
}
");
        }

        [Fact, WorkItem(543499, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543499")]
        public void TestGeneratingImplicitConstructor()
        {
            var source = @"
public class H
{
   public static void Main()
   {
   }
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("H..ctor",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}
");
        }

        [Fact, WorkItem(543499, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543499")]
        public void TestGeneratingImplicitStaticConstructor()
        {
            var source = @"
public class H
{
    public static bool value = false;
    public static void Main()
    {
        bool val = value;
    }
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("H..cctor",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
");
        }


        [Fact]
        public void TestGeneratingStaticMethod()
        {
            var source = @"
class C 
{ 
    void M() 
    { 
        System.Console.WriteLine(123);
    }
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.M",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.s   123
  IL_0002:  call       ""void System.Console.WriteLine(int)""
  IL_0007:  ret       
}
");
        }

        [Fact]
        public void TestUncheckedNumericConversions()
        {
            var source = @"
class C
{
  void M()
  {
    sbyte local_sbyte = (sbyte)0;
    byte local_byte = (byte)0;
    short local_short = (short)0;
    ushort local_ushort = (ushort)0;
    int local_int = (int)0;
    uint local_uint = (uint)0;
    long local_long = (long)0;
    ulong local_ulong = (ulong)0;
    char local_char = (char)0;
    float local_float = (float)0;
    double local_double = (double)0;
    local_sbyte = (sbyte)local_sbyte;
    local_sbyte = (sbyte)local_byte;
    local_sbyte = (sbyte)local_short;
    local_sbyte = (sbyte)local_ushort;
    local_sbyte = (sbyte)local_int;
    local_sbyte = (sbyte)local_uint;
    local_sbyte = (sbyte)local_long;
    local_sbyte = (sbyte)local_ulong;
    local_sbyte = (sbyte)local_char;
    local_sbyte = (sbyte)local_float;
    local_sbyte = (sbyte)local_double;
    local_byte = (byte)local_sbyte;
    local_byte = (byte)local_byte;
    local_byte = (byte)local_short;
    local_byte = (byte)local_ushort;
    local_byte = (byte)local_int;
    local_byte = (byte)local_uint;
    local_byte = (byte)local_long;
    local_byte = (byte)local_ulong;
    local_byte = (byte)local_char;
    local_byte = (byte)local_float;
    local_byte = (byte)local_double;
    local_short = (short)local_sbyte;
    local_short = (short)local_byte;
    local_short = (short)local_short;
    local_short = (short)local_ushort;
    local_short = (short)local_int;
    local_short = (short)local_uint;
    local_short = (short)local_long;
    local_short = (short)local_ulong;
    local_short = (short)local_char;
    local_short = (short)local_float;
    local_short = (short)local_double;
    local_ushort = (ushort)local_sbyte;
    local_ushort = (ushort)local_byte;
    local_ushort = (ushort)local_short;
    local_ushort = (ushort)local_ushort;
    local_ushort = (ushort)local_int;
    local_ushort = (ushort)local_uint;
    local_ushort = (ushort)local_long;
    local_ushort = (ushort)local_ulong;
    local_ushort = (ushort)local_char;
    local_ushort = (ushort)local_float;
    local_ushort = (ushort)local_double;
    local_int = (int)local_sbyte;
    local_int = (int)local_byte;
    local_int = (int)local_short;
    local_int = (int)local_ushort;
    local_int = (int)local_int;
    local_int = (int)local_uint;
    local_int = (int)local_long;
    local_int = (int)local_ulong;
    local_int = (int)local_char;
    local_int = (int)local_float;
    local_int = (int)local_double;
    local_uint = (uint)local_sbyte;
    local_uint = (uint)local_byte;
    local_uint = (uint)local_short;
    local_uint = (uint)local_ushort;
    local_uint = (uint)local_int;
    local_uint = (uint)local_uint;
    local_uint = (uint)local_long;
    local_uint = (uint)local_ulong;
    local_uint = (uint)local_char;
    local_uint = (uint)local_float;
    local_uint = (uint)local_double;
    local_long = (long)local_sbyte;
    local_long = (long)local_byte;
    local_long = (long)local_short;
    local_long = (long)local_ushort;
    local_long = (long)local_int;
    local_long = (long)local_uint;
    local_long = (long)local_long;
    local_long = (long)local_ulong;
    local_long = (long)local_char;
    local_long = (long)local_float;
    local_long = (long)local_double;
    local_ulong = (ulong)local_sbyte;
    local_ulong = (ulong)local_byte;
    local_ulong = (ulong)local_short;
    local_ulong = (ulong)local_ushort;
    local_ulong = (ulong)local_int;
    local_ulong = (ulong)local_uint;
    local_ulong = (ulong)local_long;
    local_ulong = (ulong)local_ulong;
    local_ulong = (ulong)local_char;
    local_ulong = (ulong)local_float;
    local_ulong = (ulong)local_double;
    local_char = (char)local_sbyte;
    local_char = (char)local_byte;
    local_char = (char)local_short;
    local_char = (char)local_ushort;
    local_char = (char)local_int;
    local_char = (char)local_uint;
    local_char = (char)local_long;
    local_char = (char)local_ulong;
    local_char = (char)local_char;
    local_char = (char)local_float;
    local_char = (char)local_double;
    local_float = (float)local_sbyte;
    local_float = (float)local_byte;
    local_float = (float)local_short;
    local_float = (float)local_ushort;
    local_float = (float)local_int;
    local_float = (float)local_uint;
    local_float = (float)local_long;
    local_float = (float)local_ulong;
    local_float = (float)local_char;
    local_float = (float)local_float;
    local_float = (float)local_double;
    local_double = (double)local_sbyte;
    local_double = (double)local_byte;
    local_double = (double)local_short;
    local_double = (double)local_ushort;
    local_double = (double)local_int;
    local_double = (double)local_uint;
    local_double = (double)local_long;
    local_double = (double)local_ulong;
    local_double = (double)local_char;
    local_double = (double)local_float;
    local_double = (double)local_double;
  }
}
";
            var compilation = CompileAndVerify(source);

            // TODO: There is unusual behavior here in the loading of constants, e.g. that they load
            // the int 0 and then do a conversion if appropriate, rather than say just loading a float
            // 0.0 to begin with. This is because the conversions in initialization expressions don't
            // fold the 0 literal to a 0.0 constant in place. We might need to look at that; but after
            // the initialization of the locals this IL is otherwise correct.
            compilation.VerifyIL("C.M",
@"{
  // Code size      536 (0x218)
  .maxstack  1
  .locals init (sbyte V_0, //local_sbyte
  byte V_1, //local_byte
  short V_2, //local_short
  ushort V_3, //local_ushort
  int V_4, //local_int
  uint V_5, //local_uint
  long V_6, //local_long
  ulong V_7, //local_ulong
  char V_8, //local_char
  float V_9, //local_float
  double V_10) //local_double
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  ldc.i4.0
  IL_0005:  stloc.2
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.3
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.s    V_4
  IL_000b:  ldc.i4.0
  IL_000c:  stloc.s    V_5
  IL_000e:  ldc.i4.0
  IL_000f:  conv.i8
  IL_0010:  stloc.s    V_6
  IL_0012:  ldc.i4.0
  IL_0013:  conv.i8
  IL_0014:  stloc.s    V_7
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.s    V_8
  IL_0019:  ldc.r4     0
  IL_001e:  stloc.s    V_9
  IL_0020:  ldc.r8     0
  IL_0029:  stloc.s    V_10
  IL_002b:  ldloc.0
  IL_002c:  stloc.0
  IL_002d:  ldloc.1
  IL_002e:  conv.i1
  IL_002f:  stloc.0
  IL_0030:  ldloc.2
  IL_0031:  conv.i1
  IL_0032:  stloc.0
  IL_0033:  ldloc.3
  IL_0034:  conv.i1
  IL_0035:  stloc.0
  IL_0036:  ldloc.s    V_4
  IL_0038:  conv.i1
  IL_0039:  stloc.0
  IL_003a:  ldloc.s    V_5
  IL_003c:  conv.i1
  IL_003d:  stloc.0
  IL_003e:  ldloc.s    V_6
  IL_0040:  conv.i1
  IL_0041:  stloc.0
  IL_0042:  ldloc.s    V_7
  IL_0044:  conv.i1
  IL_0045:  stloc.0
  IL_0046:  ldloc.s    V_8
  IL_0048:  conv.i1
  IL_0049:  stloc.0
  IL_004a:  ldloc.s    V_9
  IL_004c:  conv.i1
  IL_004d:  stloc.0
  IL_004e:  ldloc.s    V_10
  IL_0050:  conv.i1
  IL_0051:  stloc.0
  IL_0052:  ldloc.0
  IL_0053:  conv.u1
  IL_0054:  stloc.1
  IL_0055:  ldloc.1
  IL_0056:  stloc.1
  IL_0057:  ldloc.2
  IL_0058:  conv.u1
  IL_0059:  stloc.1
  IL_005a:  ldloc.3
  IL_005b:  conv.u1
  IL_005c:  stloc.1
  IL_005d:  ldloc.s    V_4
  IL_005f:  conv.u1
  IL_0060:  stloc.1
  IL_0061:  ldloc.s    V_5
  IL_0063:  conv.u1
  IL_0064:  stloc.1
  IL_0065:  ldloc.s    V_6
  IL_0067:  conv.u1
  IL_0068:  stloc.1
  IL_0069:  ldloc.s    V_7
  IL_006b:  conv.u1
  IL_006c:  stloc.1
  IL_006d:  ldloc.s    V_8
  IL_006f:  conv.u1
  IL_0070:  stloc.1
  IL_0071:  ldloc.s    V_9
  IL_0073:  conv.u1
  IL_0074:  stloc.1
  IL_0075:  ldloc.s    V_10
  IL_0077:  conv.u1
  IL_0078:  stloc.1
  IL_0079:  ldloc.0
  IL_007a:  stloc.2
  IL_007b:  ldloc.1
  IL_007c:  stloc.2
  IL_007d:  ldloc.2
  IL_007e:  stloc.2
  IL_007f:  ldloc.3
  IL_0080:  conv.i2
  IL_0081:  stloc.2
  IL_0082:  ldloc.s    V_4
  IL_0084:  conv.i2
  IL_0085:  stloc.2
  IL_0086:  ldloc.s    V_5
  IL_0088:  conv.i2
  IL_0089:  stloc.2
  IL_008a:  ldloc.s    V_6
  IL_008c:  conv.i2
  IL_008d:  stloc.2
  IL_008e:  ldloc.s    V_7
  IL_0090:  conv.i2
  IL_0091:  stloc.2
  IL_0092:  ldloc.s    V_8
  IL_0094:  conv.i2
  IL_0095:  stloc.2
  IL_0096:  ldloc.s    V_9
  IL_0098:  conv.i2
  IL_0099:  stloc.2
  IL_009a:  ldloc.s    V_10
  IL_009c:  conv.i2
  IL_009d:  stloc.2
  IL_009e:  ldloc.0
  IL_009f:  conv.u2
  IL_00a0:  stloc.3
  IL_00a1:  ldloc.1
  IL_00a2:  stloc.3
  IL_00a3:  ldloc.2
  IL_00a4:  conv.u2
  IL_00a5:  stloc.3
  IL_00a6:  ldloc.3
  IL_00a7:  stloc.3
  IL_00a8:  ldloc.s    V_4
  IL_00aa:  conv.u2
  IL_00ab:  stloc.3
  IL_00ac:  ldloc.s    V_5
  IL_00ae:  conv.u2
  IL_00af:  stloc.3
  IL_00b0:  ldloc.s    V_6
  IL_00b2:  conv.u2
  IL_00b3:  stloc.3
  IL_00b4:  ldloc.s    V_7
  IL_00b6:  conv.u2
  IL_00b7:  stloc.3
  IL_00b8:  ldloc.s    V_8
  IL_00ba:  stloc.3
  IL_00bb:  ldloc.s    V_9
  IL_00bd:  conv.u2
  IL_00be:  stloc.3
  IL_00bf:  ldloc.s    V_10
  IL_00c1:  conv.u2
  IL_00c2:  stloc.3
  IL_00c3:  ldloc.0
  IL_00c4:  stloc.s    V_4
  IL_00c6:  ldloc.1
  IL_00c7:  stloc.s    V_4
  IL_00c9:  ldloc.2
  IL_00ca:  stloc.s    V_4
  IL_00cc:  ldloc.3
  IL_00cd:  stloc.s    V_4
  IL_00cf:  ldloc.s    V_4
  IL_00d1:  stloc.s    V_4
  IL_00d3:  ldloc.s    V_5
  IL_00d5:  stloc.s    V_4
  IL_00d7:  ldloc.s    V_6
  IL_00d9:  conv.i4
  IL_00da:  stloc.s    V_4
  IL_00dc:  ldloc.s    V_7
  IL_00de:  conv.i4
  IL_00df:  stloc.s    V_4
  IL_00e1:  ldloc.s    V_8
  IL_00e3:  stloc.s    V_4
  IL_00e5:  ldloc.s    V_9
  IL_00e7:  conv.i4
  IL_00e8:  stloc.s    V_4
  IL_00ea:  ldloc.s    V_10
  IL_00ec:  conv.i4
  IL_00ed:  stloc.s    V_4
  IL_00ef:  ldloc.0
  IL_00f0:  stloc.s    V_5
  IL_00f2:  ldloc.1
  IL_00f3:  stloc.s    V_5
  IL_00f5:  ldloc.2
  IL_00f6:  stloc.s    V_5
  IL_00f8:  ldloc.3
  IL_00f9:  stloc.s    V_5
  IL_00fb:  ldloc.s    V_4
  IL_00fd:  stloc.s    V_5
  IL_00ff:  ldloc.s    V_5
  IL_0101:  stloc.s    V_5
  IL_0103:  ldloc.s    V_6
  IL_0105:  conv.u4
  IL_0106:  stloc.s    V_5
  IL_0108:  ldloc.s    V_7
  IL_010a:  conv.u4
  IL_010b:  stloc.s    V_5
  IL_010d:  ldloc.s    V_8
  IL_010f:  stloc.s    V_5
  IL_0111:  ldloc.s    V_9
  IL_0113:  conv.u4
  IL_0114:  stloc.s    V_5
  IL_0116:  ldloc.s    V_10
  IL_0118:  conv.u4
  IL_0119:  stloc.s    V_5
  IL_011b:  ldloc.0
  IL_011c:  conv.i8
  IL_011d:  stloc.s    V_6
  IL_011f:  ldloc.1
  IL_0120:  conv.u8
  IL_0121:  stloc.s    V_6
  IL_0123:  ldloc.2
  IL_0124:  conv.i8
  IL_0125:  stloc.s    V_6
  IL_0127:  ldloc.3
  IL_0128:  conv.u8
  IL_0129:  stloc.s    V_6
  IL_012b:  ldloc.s    V_4
  IL_012d:  conv.i8
  IL_012e:  stloc.s    V_6
  IL_0130:  ldloc.s    V_5
  IL_0132:  conv.u8
  IL_0133:  stloc.s    V_6
  IL_0135:  ldloc.s    V_6
  IL_0137:  stloc.s    V_6
  IL_0139:  ldloc.s    V_7
  IL_013b:  stloc.s    V_6
  IL_013d:  ldloc.s    V_8
  IL_013f:  conv.u8
  IL_0140:  stloc.s    V_6
  IL_0142:  ldloc.s    V_9
  IL_0144:  conv.i8
  IL_0145:  stloc.s    V_6
  IL_0147:  ldloc.s    V_10
  IL_0149:  conv.i8
  IL_014a:  stloc.s    V_6
  IL_014c:  ldloc.0
  IL_014d:  conv.i8
  IL_014e:  stloc.s    V_7
  IL_0150:  ldloc.1
  IL_0151:  conv.u8
  IL_0152:  stloc.s    V_7
  IL_0154:  ldloc.2
  IL_0155:  conv.i8
  IL_0156:  stloc.s    V_7
  IL_0158:  ldloc.3
  IL_0159:  conv.u8
  IL_015a:  stloc.s    V_7
  IL_015c:  ldloc.s    V_4
  IL_015e:  conv.i8
  IL_015f:  stloc.s    V_7
  IL_0161:  ldloc.s    V_5
  IL_0163:  conv.u8
  IL_0164:  stloc.s    V_7
  IL_0166:  ldloc.s    V_6
  IL_0168:  stloc.s    V_7
  IL_016a:  ldloc.s    V_7
  IL_016c:  stloc.s    V_7
  IL_016e:  ldloc.s    V_8
  IL_0170:  conv.u8
  IL_0171:  stloc.s    V_7
  IL_0173:  ldloc.s    V_9
  IL_0175:  conv.u8
  IL_0176:  stloc.s    V_7
  IL_0178:  ldloc.s    V_10
  IL_017a:  conv.u8
  IL_017b:  stloc.s    V_7
  IL_017d:  ldloc.0
  IL_017e:  conv.u2
  IL_017f:  stloc.s    V_8
  IL_0181:  ldloc.1
  IL_0182:  stloc.s    V_8
  IL_0184:  ldloc.2
  IL_0185:  conv.u2
  IL_0186:  stloc.s    V_8
  IL_0188:  ldloc.3
  IL_0189:  stloc.s    V_8
  IL_018b:  ldloc.s    V_4
  IL_018d:  conv.u2
  IL_018e:  stloc.s    V_8
  IL_0190:  ldloc.s    V_5
  IL_0192:  conv.u2
  IL_0193:  stloc.s    V_8
  IL_0195:  ldloc.s    V_6
  IL_0197:  conv.u2
  IL_0198:  stloc.s    V_8
  IL_019a:  ldloc.s    V_7
  IL_019c:  conv.u2
  IL_019d:  stloc.s    V_8
  IL_019f:  ldloc.s    V_8
  IL_01a1:  stloc.s    V_8
  IL_01a3:  ldloc.s    V_9
  IL_01a5:  conv.u2
  IL_01a6:  stloc.s    V_8
  IL_01a8:  ldloc.s    V_10
  IL_01aa:  conv.u2
  IL_01ab:  stloc.s    V_8
  IL_01ad:  ldloc.0
  IL_01ae:  conv.r4
  IL_01af:  stloc.s    V_9
  IL_01b1:  ldloc.1
  IL_01b2:  conv.r4
  IL_01b3:  stloc.s    V_9
  IL_01b5:  ldloc.2
  IL_01b6:  conv.r4
  IL_01b7:  stloc.s    V_9
  IL_01b9:  ldloc.3
  IL_01ba:  conv.r4
  IL_01bb:  stloc.s    V_9
  IL_01bd:  ldloc.s    V_4
  IL_01bf:  conv.r4
  IL_01c0:  stloc.s    V_9
  IL_01c2:  ldloc.s    V_5
  IL_01c4:  conv.r.un
  IL_01c5:  conv.r4
  IL_01c6:  stloc.s    V_9
  IL_01c8:  ldloc.s    V_6
  IL_01ca:  conv.r4
  IL_01cb:  stloc.s    V_9
  IL_01cd:  ldloc.s    V_7
  IL_01cf:  conv.r.un
  IL_01d0:  conv.r4
  IL_01d1:  stloc.s    V_9
  IL_01d3:  ldloc.s    V_8
  IL_01d5:  conv.r4
  IL_01d6:  stloc.s    V_9
  IL_01d8:  ldloc.s    V_9
  IL_01da:  conv.r4
  IL_01db:  stloc.s    V_9
  IL_01dd:  ldloc.s    V_10
  IL_01df:  conv.r4
  IL_01e0:  stloc.s    V_9
  IL_01e2:  ldloc.0
  IL_01e3:  conv.r8
  IL_01e4:  stloc.s    V_10
  IL_01e6:  ldloc.1
  IL_01e7:  conv.r8
  IL_01e8:  stloc.s    V_10
  IL_01ea:  ldloc.2
  IL_01eb:  conv.r8
  IL_01ec:  stloc.s    V_10
  IL_01ee:  ldloc.3
  IL_01ef:  conv.r8
  IL_01f0:  stloc.s    V_10
  IL_01f2:  ldloc.s    V_4
  IL_01f4:  conv.r8
  IL_01f5:  stloc.s    V_10
  IL_01f7:  ldloc.s    V_5
  IL_01f9:  conv.r.un
  IL_01fa:  conv.r8
  IL_01fb:  stloc.s    V_10
  IL_01fd:  ldloc.s    V_6
  IL_01ff:  conv.r8
  IL_0200:  stloc.s    V_10
  IL_0202:  ldloc.s    V_7
  IL_0204:  conv.r.un
  IL_0205:  conv.r8
  IL_0206:  stloc.s    V_10
  IL_0208:  ldloc.s    V_8
  IL_020a:  conv.r8
  IL_020b:  stloc.s    V_10
  IL_020d:  ldloc.s    V_9
  IL_020f:  conv.r8
  IL_0210:  stloc.s    V_10
  IL_0212:  ldloc.s    V_10
  IL_0214:  conv.r8
  IL_0215:  stloc.s    V_10
  IL_0217:  ret
}
");
        }

        [Fact]
        public void TestGeneratingClassConstructor()
        {
            var source = @"
class C 
{ 
    void M() 
    { 
        new System.Collections.ArrayList(4);
    }
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.M",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.4  
  IL_0001:  newobj     ""System.Collections.ArrayList..ctor(int)""
  IL_0006:  pop       
  IL_0007:  ret       
}
");
        }

        [Fact]
        public void TestMaxStack()
        {
            var source = @"
class C 
{
    static void Long(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14, int i15, int i16, int i17, int i18, int i19, int i20) { }
    void M() 
    { 
        Long(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
    }
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.M",
@"{
  // Code size       38 (0x26)
  .maxstack  20
  IL_0000:  ldc.i4.1  
  IL_0001:  ldc.i4.2  
  IL_0002:  ldc.i4.3  
  IL_0003:  ldc.i4.4  
  IL_0004:  ldc.i4.5  
  IL_0005:  ldc.i4.6  
  IL_0006:  ldc.i4.7  
  IL_0007:  ldc.i4.8  
  IL_0008:  ldc.i4.s   9
  IL_000a:  ldc.i4.s   10
  IL_000c:  ldc.i4.s   11
  IL_000e:  ldc.i4.s   12
  IL_0010:  ldc.i4.s   13
  IL_0012:  ldc.i4.s   14
  IL_0014:  ldc.i4.s   15
  IL_0016:  ldc.i4.s   16
  IL_0018:  ldc.i4.s   17
  IL_001a:  ldc.i4.s   18
  IL_001c:  ldc.i4.s   19
  IL_001e:  ldc.i4.s   20
  IL_0020:  call       ""void C.Long(int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int)""
  IL_0025:  ret       
}
");
        }

        [Fact]
        public void TestReturn()
        {
            var source = @"
class C 
{
    double M()
    {
        int x = 3;
        return x;
    }
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.M",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldc.i4.3
  IL_0001:  conv.r8
  IL_0002:  ret
}
");
        }

        [Fact]
        public void TestBranch()
        {
            var source = @"
class C
{ 
    static void M() 
    { 
        bool b = false;
        if (b)
        {
            System.Console.WriteLine(""1"");
        }
        else
        {
            System.Console.WriteLine(""2"");
        }
        
        while (b)
        {
            System.Console.WriteLine(""3"");
            b = false;
        }
    } 
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.M",
@"{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (bool V_0) //b
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  ldloc.0   
  IL_0003:  brfalse.s  IL_0011
  IL_0005:  ldstr      ""1""
  IL_000a:  call       ""void System.Console.WriteLine(string)""
  IL_000f:  br.s       IL_0029
  IL_0011:  ldstr      ""2""
  IL_0016:  call       ""void System.Console.WriteLine(string)""
  IL_001b:  br.s       IL_0029
  IL_001d:  ldstr      ""3""
  IL_0022:  call       ""void System.Console.WriteLine(string)""
  IL_0027:  ldc.i4.0  
  IL_0028:  stloc.0   
  IL_0029:  ldloc.0   
  IL_002a:  brtrue.s   IL_001d
  IL_002c:  ret       
}
");
        }

        [Fact]
        public void TestConst()
        {
            string source = @"
public class D
{
    static int M()
    {
        return 42;
    }    

    public static void Main()
    {
        System.Console.Write(M());
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "42");

            compilation.VerifyIL("D.M",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldc.i4.s   42
  IL_0002:  ret       
}
");
        }

        [Fact]
        public void TestArg()
        {
            string source = @"
public class D
{
    static int M(int x)
    {
        return x;
    }

    public static void Main()
    {
        System.Console.Write(M(42));
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "42");

            compilation.VerifyIL("D.M",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  ret       
}
");
        }

        [Fact]
        public void TestArgs()
        {
            string source = @"
public class C
{
    static int M(int x, int y, int z)
    {
        return y;
    }

    public static void Main()
    {
        System.Console.Write(M(0, 42, 1));
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "42");

            compilation.VerifyIL("C.M",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1   
  IL_0001:  ret       
}
");
        }

        [Fact]
        public void TestLocalAccess()
        {
            string source = @"
public class C
{
    static int M(bool getLocal, int arg)
    {
        int y = 123;
        System.Exception ex; // just for fun

        return y;
    }

    public static void Main()
    {
        System.Console.Write(M(false, 42));
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "123");

            compilation.VerifyIL("C.M",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldc.i4.s   123
  IL_0002:  ret
}
");
        }

        [Fact]
        public void TestConditionalLocalAccess()
        {
            string source = @"

public class D
{
    static int M(bool getLocal, int arg)
    {
        System.Exception ex; // just for fun
        int y = 123;

        if (getLocal)
        {
            return y;
        }
        else
        {
            return arg;
        }
    }

    public static void Main()
    {
        System.Console.Write(M(false, 42));
        System.Console.Write(M(true, 42));
    }
}
";
            CompileAndVerify(source, expectedOutput: "42123").
                VerifyIL("D.M",
@"{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (int V_0) //y
  IL_0000:  ldc.i4.s   123
  IL_0002:  stloc.0
  IL_0003:  ldarg.0
  IL_0004:  brfalse.s  IL_0008
  IL_0006:  ldloc.0
  IL_0007:  ret
  IL_0008:  ldarg.1
  IL_0009:  ret
}
");

            var v = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: "42123");

            v.VerifyIL("D.M",
@"{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (System.Exception V_0, //ex
                int V_1, //y
                bool V_2,
                int V_3)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.s   123
  IL_0003:  stloc.1
 -IL_0004:  ldarg.0
  IL_0005:  stloc.2
 ~IL_0006:  ldloc.2
  IL_0007:  brfalse.s  IL_000e
 -IL_0009:  nop
 -IL_000a:  ldloc.1
  IL_000b:  stloc.3
  IL_000c:  br.s       IL_0013
 -IL_000e:  nop
 -IL_000f:  ldarg.1
  IL_0010:  stloc.3
  IL_0011:  br.s       IL_0013
 -IL_0013:  ldloc.3
  IL_0014:  ret
}
", sequencePoints: "D.M");
        }

        [Fact]
        public void TestAssignRefNull()
        {
            string source = @"

public class D
{
    static System.Exception M()
    {
        System.Exception y = null;

        return y;
    }

    public static void Main()
    {
        System.Console.Write(M());
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "");

            compilation.VerifyIL("D.M",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        public void TestAssignIdentity()
        {
            string source = @"

public class D
{
    static System.Object M()
    {
        System.AppDomain y = System.AppDomain.CreateDomain(""qq"");

        return y;
    }

    public static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            System.Console.Write(M());
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
Name:qq
There are no context policies.
");

            compilation.VerifyIL("D.M",
@"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      ""qq""
  IL_0005:  call       ""System.AppDomain System.AppDomain.CreateDomain(string)""
  IL_000a:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        public void TestRefCast()
        {
            string source = @"
public class D
{
    static System.AppDomain M()
    {
        object y = System.AppDomain.CreateDomain(""qq"");

        System.AppDomain z = (System.AppDomain)y;

        return z;
    }

    public static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            System.Console.Write(M());
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
Name:qq
There are no context policies.
");

            compilation.VerifyIL("D.M",
@"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""qq""
  IL_0005:  call       ""System.AppDomain System.AppDomain.CreateDomain(string)""
  IL_000a:  castclass  ""System.AppDomain""
  IL_000f:  ret
}
");
        }

        [Fact]
        public void TestRefCtor()
        {
            string source = @"
public class D
{
    static System.Exception M()
    {
        System.Exception y = new System.Exception(""hello"");

        return y;
    }

    public static void Main()
    {
        System.Console.Write(M());
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"System.Exception: hello");

            compilation.VerifyIL("D.M",
@"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      ""hello""
  IL_0005:  newobj     ""System.Exception..ctor(string)""
  IL_000a:  ret
}");
        }

        [Fact]
        public void MethodParameterAccess()
        {
            string source = @"
public class D
{
    public class Moo
    {
        public Moo()
        {
        }

        public string Boo(int x, string y, object z)
        {
            return y;
        }
    }

    public static void Main()
    {
        Moo obj = new Moo();
        string s = obj.Boo(1, ""hi"", obj);
        System.Console.Write(s);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"hi");

            compilation.VerifyIL("D.Moo.Boo",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.2   
  IL_0001:  ret       
}
");
        }

        [Fact]
        public void PropertyGet()
        {
            string source = @"
class C
{
    int P { get { return 0; } }
    void M()
    {
        int p = P;
    }
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.M",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""int C.P.get""
  IL_0006:  pop
  IL_0007:  ret
}
");
        }

        [Fact]
        public void PropertyGetAndSet()
        {
            string source = @"
class C
{
    private int p;
    C This { get { return this; } }
    string S { get { return string.Empty; } }
    int P
    {
        get { return p; }
        set { p = value; }
    }
    void M(string s)
    {
        This.This.P = This.S.Length;
    }
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.M",
@"{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  call       ""C C.This.get""
  IL_0006:  callvirt   ""C C.This.get""
  IL_000b:  ldarg.0   
  IL_000c:  call       ""C C.This.get""
  IL_0011:  callvirt   ""string C.S.get""
  IL_0016:  callvirt   ""int string.Length.get""
  IL_001b:  callvirt   ""void C.P.set""
  IL_0020:  ret       
}
");
        }

        [Fact]
        public void PropertyStaticGetAndSet()
        {
            string source = @"
class C
{
    private static string s;
    public static string S
    {
        get { return s; }
        set { s = value; }
    }
    static void Main()
    {
        C.S = ""S"";
        System.Console.Write(C.S);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "S");

            compilation.VerifyIL("C.Main",
@"{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldstr      ""S""
  IL_0005:  call       ""void C.S.set""
  IL_000a:  call       ""string C.S.get""
  IL_000f:  call       ""void System.Console.Write(string)""
  IL_0014:  ret       
}
");
        }

        [Fact]
        public void PropertyAutoGetAndSet()
        {
            string source = @"
class C
{
    int P { get; set; }
    static void Main()
    {
        C c = new C();
        c.P = 2;
        System.Console.Write(""{0}"", c.P);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "2");

            compilation.VerifyIL("C.P.set",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  stfld      ""int C.<P>k__BackingField""
  IL_0007:  ret       
}
");
        }

        [Fact]
        public void PropertyStaticAutoGetAndSet()
        {
            string source = @"
class C
{
    static string S { get; set; }
    static void Main()
    {
        S = ""S"";
        System.Console.Write(S);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "S");

            compilation.VerifyIL("C.S.set",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  stsfld     ""string C.<S>k__BackingField""
  IL_0006:  ret       
}
");
        }

        [Fact]
        public void PropertyOfTGetAndSet()
        {
            string source = @"
class C<T>
{
    private T t;
    public T T2
    {
        get { return t; }
        set { t = value; }
    }
}
class P
{
    static void Main()
    {
        C<string> c = new C<string>();
        c.T2 = ""T2"";
        System.Console.Write(c.T2);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "T2");

            compilation.VerifyIL("P.Main",
@"{
  // Code size       27 (0x1b)
  .maxstack  3
  IL_0000:  newobj     ""C<string>..ctor()""
  IL_0005:  dup
  IL_0006:  ldstr      ""T2""
  IL_000b:  callvirt   ""void C<string>.T2.set""
  IL_0010:  callvirt   ""string C<string>.T2.get""
  IL_0015:  call       ""void System.Console.Write(string)""
  IL_001a:  ret
}
");
        }

        [Fact]
        public void PropertyStaticOfTGetAndSet()
        {
            string source = @"
class C<T>
{
    public static string S { get; set; }
    public static T T2 { get; set; }
}
class P
{
    static void Main()
    {
        C<int>.S = ""C<int>.S"";
        C<string>.S = ""C<string>.S"";
        C<string>.T2 = ""C<string>.T2"";
        System.Console.Write(""{0};{1};{2}"", C<int>.S, C<string>.S, C<string>.T2);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "C<int>.S;C<string>.S;C<string>.T2");

            compilation.VerifyIL("C<T>.S.set",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  stsfld     ""string C<T>.<S>k__BackingField""
  IL_0006:  ret       
}
");
        }

        [WorkItem(538677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538677")]
        [Fact]
        public void PropertyAssignmentExpression()
        {
            string source = @"
class C
{
    static C F() { return new C(); }
    static string P { set { } }
    string Q { set { } }
    static void Main()
    {
        string p = P = ""p"";
        string q = F().Q = ""q"";
        System.Console.Write(""{0}, {1}"", p, q);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "p, q");

            compilation.VerifyIL("C.Main",
@"{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (string V_0, //p
  string V_1, //q
  string V_2)
  IL_0000:  ldstr      ""p""
  IL_0005:  dup
  IL_0006:  call       ""void C.P.set""
  IL_000b:  stloc.0
  IL_000c:  call       ""C C.F()""
  IL_0011:  ldstr      ""q""
  IL_0016:  dup
  IL_0017:  stloc.2
  IL_0018:  callvirt   ""void C.Q.set""
  IL_001d:  ldloc.2
  IL_001e:  stloc.1
  IL_001f:  ldstr      ""{0}, {1}""
  IL_0024:  ldloc.0
  IL_0025:  ldloc.1
  IL_0026:  call       ""void System.Console.Write(string, object, object)""
  IL_002b:  ret
}
");
        }

        [Fact]
        public void CallVirtualMethods()
        {
            string source = @"
abstract class A
{
    public virtual void F() { }
    public abstract void G();
}
abstract class B : A
{
    public abstract override void F();
    public override void G() { }
}
class C : B
{
    public override void F() { }
}
class D : C
{
    public override void G() { }
}
class Program
{
    static void M(D d)
    {
        d.F();
        d.G();
    }
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("Program.M",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  callvirt   ""void A.F()""
  IL_0006:  ldarg.0   
  IL_0007:  callvirt   ""void A.G()""
  IL_000c:  ret       
}
");
        }

        [Fact]
        public void CallVirtualProperties()
        {
            string source = @"
abstract class A
{
    public abstract int Q { get; internal set; }
}
abstract class B : A
{
    public virtual int P { get; internal set; }
    public abstract override int Q { get; internal set; }
}
class C : B
{
    public override int P { internal set { } }
    public override int Q { get { return 0; } internal set { } }
}
class D : C
{
    public override int Q { internal set { } }
}
class E : D
{
    public override int P { get { return 0; } }
}
class Program
{
    static void M(E e)
    {
        e.Q = e.P;
        e.P = e.Q;
    }
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("Program.M",
@"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""int B.P.get""
  IL_0007:  callvirt   ""void A.Q.set""
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  callvirt   ""int A.Q.get""
  IL_0013:  callvirt   ""void B.P.set""
  IL_0018:  ret
}
");
        }

        [Fact]
        public void CallBaseMethods()
        {
            string source = @"
using System;
class A
{
    public virtual void M()
    {
        Console.Write(""A.M, "");
    }
    public virtual object P
    {
        get { return null; }
        set
        {
            Console.Write(""A.P, "");
        }
    }
}
class B : A
{
    public override void M()
    {
        Console.Write(""B.M, "");
    }
    public override object P
    {
        get { return null; }
        set
        {
            Console.Write(""B.P, "");
        }
    }
    public void N()
    {
        this.M();
        this.P = 0;
        base.M();
        base.P = 1;
    }
}
class Program
{
    static void Main()
    {
        new B().N();
    }
}
";
            CompileAndVerify(source, expectedOutput: "B.M, B.P, A.M, A.P, ").
                VerifyIL("B.N",
@"{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  callvirt   ""void A.M()""
  IL_0006:  ldarg.0   
  IL_0007:  ldc.i4.0  
  IL_0008:  box        ""int""
  IL_000d:  callvirt   ""void A.P.set""
  IL_0012:  ldarg.0   
  IL_0013:  call       ""void A.M()""
  IL_0018:  ldarg.0   
  IL_0019:  ldc.i4.1  
  IL_001a:  box        ""int""
  IL_001f:  call       ""void A.P.set""
  IL_0024:  ret       
}
");
        }

        [Fact]
        public void CallBaseMethods_VirtualSimple()
        {
            string source = @"
using System;
class A
{
    public virtual string M()
    {
        return ""A.M()"";
    }
}
class B : A
{
    public override string M()
    {
        return ""B.M()"";
    }
}
class C : B
{
    public string Test()
    {
        return this.M() + "":"" + base.M();
    }
}
class D : C
{
    public override string M()
    {
        return ""D.M()"";
    }

    public new string Test()
    {
        return this.M() + "":"" + base.M();
    }
}
class Program
{
    static void Main()
    {
        Console.Write(new C().Test());
        Console.Write("", "");
        Console.WriteLine(new D().Test());
    }
}";
            CompileAndVerify(source, expectedOutput: "B.M():B.M(), D.M():B.M()").
                VerifyIL("C.Test",
@"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""string A.M()""
  IL_0006:  ldstr      "":""
  IL_000b:  ldarg.0
  IL_000c:  call       ""string B.M()""
  IL_0011:  call       ""string string.Concat(string, string, string)""
  IL_0016:  ret
}
").
                VerifyIL("D.Test",
@"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""string A.M()""
  IL_0006:  ldstr      "":""
  IL_000b:  ldarg.0
  IL_000c:  call       ""string B.M()""
  IL_0011:  call       ""string string.Concat(string, string, string)""
  IL_0016:  ret
}
");
        }

        [Fact]
        public void CallBaseProperties_VirtualSimple()
        {
            string source = @"
using System;
class A
{
    public virtual string P
    {
        get { Console.Write(""A.P.get;""); return null; }
        set { Console.Write(""A.P.set;""); }
    }
}
class B : A
{
    public override string P
    {
        //get { Console.Write(""B.P.get;""); return null; }
        set { Console.Write(""B.P.set;""); }
    }
}
class C : B
{
    public void Test()
    {
        this.P = base.P;
        base.P = this.P;
    }
}
class D : C
{
    public override string P
    {
        get { Console.Write(""D.P.get;""); return null; }
        //set { Console.Write(""D.P.set;""); }
    }

    public new void Test()
    {
        this.P = base.P;
        base.P = this.P;
    }
}
class Program
{
    static void Main()
    {
        new C().Test();
        Console.WriteLine();
        new D().Test();
    }
}";
            CompileAndVerify(source, expectedOutput: $"A.P.get;B.P.set;A.P.get;B.P.set;{Environment.NewLine}A.P.get;B.P.set;D.P.get;B.P.set;").
                VerifyIL("C.Test",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""string A.P.get""
  IL_0007:  callvirt   ""void A.P.set""
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  callvirt   ""string A.P.get""
  IL_0013:  call       ""void B.P.set""
  IL_0018:  ret
}").
                VerifyIL("D.Test",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""string A.P.get""
  IL_0007:  callvirt   ""void A.P.set""
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  callvirt   ""string A.P.get""
  IL_0013:  call       ""void B.P.set""
  IL_0018:  ret
}
");
        }

        [Fact]
        public void CallBaseMethods_Virtual_WithPrivate()
        {
            string source = @"
using System;
class A
{
    public virtual string M()
    {
        return ""A.M()"";
    }
}
class B : A
{
    public override string M()
    {
        return ""B.M()"";
    }
}
class C : B
{
    private new string M()
    {
        return ""C.M()"";
    }
    public string Test()
    {
        return this.M() + "":"" + base.M();
    }
}
class D : C
{
    public override string M()
    {
        return ""D.M()"";
    }

    public new string Test()
    {
        return this.M() + "":"" + base.M();
    }
}
class Program
{
    static void Main()
    {
        Console.Write(new C().Test());
        Console.Write("", "");
        Console.WriteLine(new D().Test());
    }
}";
            CompileAndVerify(source, expectedOutput: "C.M():B.M(), D.M():B.M()").
                VerifyIL("C.Test",
@"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""string C.M()""
  IL_0006:  ldstr      "":""
  IL_000b:  ldarg.0
  IL_000c:  call       ""string B.M()""
  IL_0011:  call       ""string string.Concat(string, string, string)""
  IL_0016:  ret
}
").
                VerifyIL("D.Test",
@"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""string A.M()""
  IL_0006:  ldstr      "":""
  IL_000b:  ldarg.0
  IL_000c:  call       ""string B.M()""
  IL_0011:  call       ""string string.Concat(string, string, string)""
  IL_0016:  ret
}
");
        }

        [Fact]
        public void CallBaseProperties_Virtual_WithPrivate()
        {
            string source = @"
using System;
class A
{
    public virtual string P
    {
        get { Console.Write(""A.P.get;""); return null; }
        set { Console.Write(""A.P.set;""); }
    }
}
class B : A
{
    public override string P
    {
        //get { Console.Write(""B.P.get;""); return null; }
        set { Console.Write(""B.P.set;""); }
    }
}
class C : B
{
    private new string P
    {
        get { Console.Write(""C.P.get;""); return null; }
        set { Console.Write(""C.P.set;""); }
    }
    public void Test()
    {
        this.P = base.P;
        base.P = this.P;
    }
}
class D : C
{
    public override string P
    {
        get { Console.Write(""D.P.get;""); return null; }
        //set { Console.Write(""D.P.set;""); }
    }

    public new void Test()
    {
        this.P = base.P;
        base.P = this.P;
    }
}
class Program
{
    static void Main()
    {
        new C().Test();
        Console.WriteLine();
        new D().Test();
    }
}";
            CompileAndVerify(source, expectedOutput: $"A.P.get;C.P.set;C.P.get;B.P.set;{Environment.NewLine}A.P.get;B.P.set;D.P.get;B.P.set;").
                VerifyIL("C.Test",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""string A.P.get""
  IL_0007:  call       ""void C.P.set""
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  call       ""string C.P.get""
  IL_0013:  call       ""void B.P.set""
  IL_0018:  ret
}").
                VerifyIL("D.Test",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""string A.P.get""
  IL_0007:  callvirt   ""void A.P.set""
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  callvirt   ""string A.P.get""
  IL_0013:  call       ""void B.P.set""
  IL_0018:  ret
}
");
        }

        [Fact]
        public void CallBaseMethods_NonVirtual_WithPrivate()
        {
            string source = @"
using System;
class A
{
    public string M()
    {
        return ""A.M()"";
    }
}
class B : A
{
    public new string M()
    {
        return ""B.M()"";
    }
}
class C : B
{
    private new string M()
    {
        return ""C.M()"";
    }
    public string Test()
    {
        return this.M() + "":"" + base.M();
    }
}
class D : C
{
    public new string M()
    {
        return ""D.M()"";
    }

    public new string Test()
    {
        return this.M() + "":"" + base.M();
    }
}
class Program
{
    static void Main()
    {
        Console.Write(new C().Test());
        Console.Write("", "");
        Console.WriteLine(new D().Test());
    }
}";
            CompileAndVerify(source, expectedOutput: "C.M():B.M(), D.M():B.M()").
                VerifyIL("C.Test",
@"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""string C.M()""
  IL_0006:  ldstr      "":""
  IL_000b:  ldarg.0
  IL_000c:  call       ""string B.M()""
  IL_0011:  call       ""string string.Concat(string, string, string)""
  IL_0016:  ret
}
").
                VerifyIL("D.Test",
@"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""string D.M()""
  IL_0006:  ldstr      "":""
  IL_000b:  ldarg.0
  IL_000c:  call       ""string B.M()""
  IL_0011:  call       ""string string.Concat(string, string, string)""
  IL_0016:  ret
}
");
        }

        [Fact]
        public void CallBaseProperties_NonVirtual_WithPrivate()
        {
            string source = @"
using System;
class A
{
    public string P
    {
        get { Console.Write(""A.P.get;""); return null; }
        set { Console.Write(""A.P.set;""); }
    }
}
class B : A
{
    public new string P
    {
        get { Console.Write(""B.P.get;""); return null; }
        set { Console.Write(""B.P.set;""); }
    }
}
class C : B
{
    private new string P
    {
        get { Console.Write(""C.P.get;""); return null; }
        set { Console.Write(""C.P.set;""); }
    }
    public void Test()
    {
        this.P = base.P;
        base.P = this.P;
    }
}
class D : C
{
    public new string P
    {
        get { Console.Write(""D.P.get;""); return null; }
        set { Console.Write(""D.P.set;""); }
    }

    public new void Test()
    {
        this.P = base.P;
        base.P = this.P;
    }
}
class Program
{
    static void Main()
    {
        new C().Test();
        Console.WriteLine();
        new D().Test();
    }
}";
            CompileAndVerify(source, expectedOutput: $"B.P.get;C.P.set;C.P.get;B.P.set;{Environment.NewLine}B.P.get;D.P.set;D.P.get;B.P.set;").
                VerifyIL("C.Test",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""string B.P.get""
  IL_0007:  call       ""void C.P.set""
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  call       ""string C.P.get""
  IL_0013:  call       ""void B.P.set""
  IL_0018:  ret
}").
                VerifyIL("D.Test",
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""string B.P.get""
  IL_0007:  call       ""void D.P.set""
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  call       ""string D.P.get""
  IL_0013:  call       ""void B.P.set""
  IL_0018:  ret
}
");
        }

        [Fact]
        public void StaticFieldLoad()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        System.Console.Write(System.Decimal.One);       
        System.Console.Write(System.Type.Missing);       
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "1System.Reflection.Missing");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldsfld     ""decimal decimal.One""
  IL_0005:  call       ""void System.Console.Write(decimal)""
  IL_000a:  ldsfld     ""object System.Type.Missing""
  IL_000f:  call       ""void System.Console.Write(object)""
  IL_0014:  ret
}");
        }

        [Fact]
        public void ConstFieldLoad()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        System.Console.Write(System.Int32.MaxValue);       
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "2147483647");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldc.i4     0x7fffffff
  IL_0005:  call       ""void System.Console.Write(int)""
  IL_000a:  ret       
}
");
        }

        [Fact]
        public void StaticFieldStore()
        {
            string source = @"
public class D
{
  
    public class Moo
    {
        public static int I;
    }

    public static void Goo()
    {
        Moo.I = 42;
    }

    public static void Main()
    {
        Goo();
        System.Console.Write(Moo.I);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "42");

            compilation.VerifyIL("D.Goo",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.s   42
  IL_0002:  stsfld     ""int D.Moo.I""
  IL_0007:  ret       
}
");
        }

        [Fact]
        public void RefInstanceField()
        {
            string source = @"
public class D
{
  
    public class Moo
    {
        public int I;

        public Moo()
        {
        }
    }

    public static void Main()
    {
        Moo obj = new Moo();

        System.Console.Write(obj.I);
        obj.I = 42;
        System.Console.Write(obj.I);
        obj.I = 7;
        System.Console.Write(obj.I);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "0427");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       53 (0x35)
  .maxstack  3
  IL_0000:  newobj     ""D.Moo..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""int D.Moo.I""
  IL_000b:  call       ""void System.Console.Write(int)""
  IL_0010:  dup
  IL_0011:  ldc.i4.s   42
  IL_0013:  stfld      ""int D.Moo.I""
  IL_0018:  dup
  IL_0019:  ldfld      ""int D.Moo.I""
  IL_001e:  call       ""void System.Console.Write(int)""
  IL_0023:  dup
  IL_0024:  ldc.i4.7
  IL_0025:  stfld      ""int D.Moo.I""
  IL_002a:  ldfld      ""int D.Moo.I""
  IL_002f:  call       ""void System.Console.Write(int)""
  IL_0034:  ret
}
");
        }

        [Fact]
        public void RefInstanceFieldA()
        {
            string source = @"
public class D
{
  
    public class Moo
    {
        public int I;

        public Moo()
        {
        }
    }

    public static void Main()
    {
        Moo obj = new Moo();

        System.Console.Write(obj.I);
        obj.I = 42;
        System.Console.Write(obj.I);
        obj.I = 7;
        System.Console.Write(obj.I);
    }
}
";
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseDebugExe, expectedOutput: "0427");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (D.Moo V_0) //obj
  IL_0000:  newobj     ""D.Moo..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldfld      ""int D.Moo.I""
  IL_000c:  call       ""void System.Console.Write(int)""
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.s   42
  IL_0014:  stfld      ""int D.Moo.I""
  IL_0019:  ldloc.0
  IL_001a:  ldfld      ""int D.Moo.I""
  IL_001f:  call       ""void System.Console.Write(int)""
  IL_0024:  ldloc.0
  IL_0025:  ldc.i4.7
  IL_0026:  stfld      ""int D.Moo.I""
  IL_002b:  ldloc.0
  IL_002c:  ldfld      ""int D.Moo.I""
  IL_0031:  call       ""void System.Console.Write(int)""
  IL_0036:  ret
}
");
        }


        [Fact]
        public void RefStaticField()
        {
            string source = @"
public class D
{
  
    public class Moo
    {
        public static int S;

        public Moo()
        {
        }
    }

    public static void Main()
    {
        System.Console.Write(Moo.S);
        Moo.S = 42;
        System.Console.Write(Moo.S);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "042");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       28 (0x1c)
  .maxstack  1
  IL_0000:  ldsfld     ""int D.Moo.S""
  IL_0005:  call       ""void System.Console.Write(int)""
  IL_000a:  ldc.i4.s   42
  IL_000c:  stsfld     ""int D.Moo.S""
  IL_0011:  ldsfld     ""int D.Moo.S""
  IL_0016:  call       ""void System.Console.Write(int)""
  IL_001b:  ret       
}
");
        }

        [Fact]
        public void AssignExprUsed()
        {
            string source = @"

public class D
{
  
    public class Moo
    {
        public int I;
        public static int IS;

        public Moo()
        {
        }
    }

    public static void Main()
    {
        Moo obj1 = new Moo();
        Moo obj2 = new Moo();

        int x = 0;
        int y = x = obj2.I = Moo.IS = obj1.I = x = 123;

        System.Console.Write(Moo.IS);
        System.Console.Write(obj1.I);
        System.Console.Write(obj2.I);
        System.Console.Write(x);
        System.Console.Write(y);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "123123123123123");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       88 (0x58)
  .maxstack  4
  .locals init (D.Moo V_0, //obj1
  D.Moo V_1, //obj2
  int V_2, //x
  int V_3)
  IL_0000:  newobj     ""D.Moo..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""D.Moo..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.2
  IL_000e:  ldloc.1
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.s   123
  IL_0012:  dup
  IL_0013:  stloc.2
  IL_0014:  dup
  IL_0015:  stloc.3
  IL_0016:  stfld      ""int D.Moo.I""
  IL_001b:  ldloc.3
  IL_001c:  dup
  IL_001d:  stsfld     ""int D.Moo.IS""
  IL_0022:  dup
  IL_0023:  stloc.3
  IL_0024:  stfld      ""int D.Moo.I""
  IL_0029:  ldloc.3
  IL_002a:  dup
  IL_002b:  stloc.2
  IL_002c:  ldsfld     ""int D.Moo.IS""
  IL_0031:  call       ""void System.Console.Write(int)""
  IL_0036:  ldloc.0
  IL_0037:  ldfld      ""int D.Moo.I""
  IL_003c:  call       ""void System.Console.Write(int)""
  IL_0041:  ldloc.1
  IL_0042:  ldfld      ""int D.Moo.I""
  IL_0047:  call       ""void System.Console.Write(int)""
  IL_004c:  ldloc.2
  IL_004d:  call       ""void System.Console.Write(int)""
  IL_0052:  call       ""void System.Console.Write(int)""
  IL_0057:  ret
}");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void ArrayAccess()
        {
            string source = @"
public class D
{
    public static void Main()
    {
         int[] arr = new int[] {111, 222, 333, 444};

         System.Console.Write(arr[1]);   
         arr[1] = arr[2];
         System.Console.Write(arr[1]);   
    }
}
";
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe.WithModuleName("ＭＯＤＵＬＥ"), expectedOutput: "222333");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       41 (0x29)
  .maxstack  4
  .locals init (int[] V_0) //arr
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.42F1B77334EDFA917032CCF8353020C73F8C62E1""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.1
  IL_0014:  ldelem.i4
  IL_0015:  call       ""void System.Console.Write(int)""
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.1
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.2
  IL_001e:  ldelem.i4
  IL_001f:  stelem.i4
  IL_0020:  ldloc.0
  IL_0021:  ldc.i4.1
  IL_0022:  ldelem.i4
  IL_0023:  call       ""void System.Console.Write(int)""
  IL_0028:  ret
}
");
        }

        [Fact]
        public void ArrayLong()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        int[] arr = new int[4L];
        arr[0L] = 111;
        ulong t = 1UL;
        arr[t] = 222;
        arr[3L] = 333;

        System.Console.Write(arr[t].ToString());
        t = 3L;
        long tl = 2;
        arr[tl] = arr[t];
        System.Console.Write(arr[tl].ToString());
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "222333");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       89 (0x59)
  .maxstack  4
  .locals init (int[] V_0, //arr
           ulong V_1, //t
           long V_2) //tl
  IL_0000:  ldc.i4.4  
  IL_0001:  conv.i8   
  IL_0002:  conv.ovf.i
  IL_0003:  newarr     ""int""
  IL_0008:  stloc.0   
  IL_0009:  ldloc.0   
  IL_000a:  ldc.i4.0  
  IL_000b:  conv.i8   
  IL_000c:  conv.ovf.i
  IL_000d:  ldc.i4.s   111
  IL_000f:  stelem.i4 
  IL_0010:  ldc.i4.1  
  IL_0011:  conv.i8   
  IL_0012:  stloc.1   
  IL_0013:  ldloc.0   
  IL_0014:  ldloc.1   
  IL_0015:  conv.ovf.i.un
  IL_0016:  ldc.i4     0xde
  IL_001b:  stelem.i4 
  IL_001c:  ldloc.0   
  IL_001d:  ldc.i4.3  
  IL_001e:  conv.i8   
  IL_001f:  conv.ovf.i
  IL_0020:  ldc.i4     0x14d
  IL_0025:  stelem.i4 
  IL_0026:  ldloc.0   
  IL_0027:  ldloc.1   
  IL_0028:  conv.ovf.i.un
  IL_0029:  ldelema    ""int""
  IL_002e:  call       ""string int.ToString()""
  IL_0033:  call       ""void System.Console.Write(string)""
  IL_0038:  ldc.i4.3  
  IL_0039:  conv.i8   
  IL_003a:  stloc.1   
  IL_003b:  ldc.i4.2  
  IL_003c:  conv.i8   
  IL_003d:  stloc.2   
  IL_003e:  ldloc.0   
  IL_003f:  ldloc.2   
  IL_0040:  conv.ovf.i
  IL_0041:  ldloc.0   
  IL_0042:  ldloc.1   
  IL_0043:  conv.ovf.i.un
  IL_0044:  ldelem.i4 
  IL_0045:  stelem.i4 
  IL_0046:  ldloc.0   
  IL_0047:  ldloc.2   
  IL_0048:  conv.ovf.i
  IL_0049:  ldelema    ""int""
  IL_004e:  call       ""string int.ToString()""
  IL_0053:  call       ""void System.Console.Write(string)""
  IL_0058:  ret       
}
");
        }

        [Fact]
        public void ArrayStructFieldAccess()
        {
            string source = @"

public class D
{
    public struct Boo
    {
        public int I1;
        public int I2;
    }

    public static void Main()
    {      
         Boo b = new Boo();
         b.I1 = 345;
         b.I2 = 678;

         Boo[] arr = new Boo[]{new Boo(), new Boo(), b};

         System.Console.Write(arr[1].I1);   
         arr[1].I1 = arr[2].I1;
         System.Console.Write(arr[1].I1);   
         System.Console.Write(arr[1].I2);   
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "03450");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size      123 (0x7b)
  .maxstack  4
  .locals init (D.Boo V_0, //b
  D.Boo[] V_1) //arr
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""D.Boo""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4     0x159
  IL_000f:  stfld      ""int D.Boo.I1""
  IL_0014:  ldloca.s   V_0
  IL_0016:  ldc.i4     0x2a6
  IL_001b:  stfld      ""int D.Boo.I2""
  IL_0020:  ldc.i4.3
  IL_0021:  newarr     ""D.Boo""
  IL_0026:  dup
  IL_0027:  ldc.i4.2
  IL_0028:  ldloc.0
  IL_0029:  stelem     ""D.Boo""
  IL_002e:  stloc.1
  IL_002f:  ldloc.1
  IL_0030:  ldc.i4.1
  IL_0031:  ldelema    ""D.Boo""
  IL_0036:  ldfld      ""int D.Boo.I1""
  IL_003b:  call       ""void System.Console.Write(int)""
  IL_0040:  ldloc.1
  IL_0041:  ldc.i4.1
  IL_0042:  ldelema    ""D.Boo""
  IL_0047:  ldloc.1
  IL_0048:  ldc.i4.2
  IL_0049:  ldelema    ""D.Boo""
  IL_004e:  ldfld      ""int D.Boo.I1""
  IL_0053:  stfld      ""int D.Boo.I1""
  IL_0058:  ldloc.1
  IL_0059:  ldc.i4.1
  IL_005a:  ldelema    ""D.Boo""
  IL_005f:  ldfld      ""int D.Boo.I1""
  IL_0064:  call       ""void System.Console.Write(int)""
  IL_0069:  ldloc.1
  IL_006a:  ldc.i4.1
  IL_006b:  ldelema    ""D.Boo""
  IL_0070:  ldfld      ""int D.Boo.I2""
  IL_0075:  call       ""void System.Console.Write(int)""
  IL_007a:  ret
}
");
        }

        [Fact]
        public void ArrayClassFieldAccess()
        {
            string source = @"

public class D
{
    public class Boo
    {
        public int I1;
        public int I2;

        public Boo()
        {
        }
    }

    public static void Main()
    {      
         Boo b = new Boo();
         b.I1 = 345;
         b.I2 = 678;

         Boo[] arr = new Boo[] {new Boo(), new Boo(), b};

         System.Console.Write(arr[1].I1);   
         arr[1].I1 = arr[2].I1;
         System.Console.Write(arr[1].I1);   
         System.Console.Write(arr[1].I2);   
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "03450");

            compilation.VerifyIL("D.Main",
@"{
  // Code size      111 (0x6f)
  .maxstack  4
  .locals init (D.Boo V_0, //b
           D.Boo[] V_1) //arr
  IL_0000:  newobj     ""D.Boo..ctor()""
  IL_0005:  stloc.0   
  IL_0006:  ldloc.0   
  IL_0007:  ldc.i4     0x159
  IL_000c:  stfld      ""int D.Boo.I1""
  IL_0011:  ldloc.0   
  IL_0012:  ldc.i4     0x2a6
  IL_0017:  stfld      ""int D.Boo.I2""
  IL_001c:  ldc.i4.3  
  IL_001d:  newarr     ""D.Boo""
  IL_0022:  dup       
  IL_0023:  ldc.i4.0  
  IL_0024:  newobj     ""D.Boo..ctor()""
  IL_0029:  stelem.ref
  IL_002a:  dup       
  IL_002b:  ldc.i4.1  
  IL_002c:  newobj     ""D.Boo..ctor()""
  IL_0031:  stelem.ref
  IL_0032:  dup       
  IL_0033:  ldc.i4.2  
  IL_0034:  ldloc.0   
  IL_0035:  stelem.ref
  IL_0036:  stloc.1   
  IL_0037:  ldloc.1   
  IL_0038:  ldc.i4.1  
  IL_0039:  ldelem.ref
  IL_003a:  ldfld      ""int D.Boo.I1""
  IL_003f:  call       ""void System.Console.Write(int)""
  IL_0044:  ldloc.1   
  IL_0045:  ldc.i4.1  
  IL_0046:  ldelem.ref
  IL_0047:  ldloc.1   
  IL_0048:  ldc.i4.2  
  IL_0049:  ldelem.ref
  IL_004a:  ldfld      ""int D.Boo.I1""
  IL_004f:  stfld      ""int D.Boo.I1""
  IL_0054:  ldloc.1   
  IL_0055:  ldc.i4.1  
  IL_0056:  ldelem.ref
  IL_0057:  ldfld      ""int D.Boo.I1""
  IL_005c:  call       ""void System.Console.Write(int)""
  IL_0061:  ldloc.1   
  IL_0062:  ldc.i4.1  
  IL_0063:  ldelem.ref
  IL_0064:  ldfld      ""int D.Boo.I2""
  IL_0069:  call       ""void System.Console.Write(int)""
  IL_006e:  ret       
}
");
        }

        [Fact, WorkItem(538674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538674")]
        public void ArrayOneDimensionWithNoIntIndex()
        {
            string source = @"
using System;

public class MyArray
{
    static uint fidx;
    static void Main()
    {
        fidx = 2;
        const long idx = 9;
        // 12
        sbyte[] a1 = new sbyte[(ulong)5];
        a1[0] = a1[fidx + 1] = 123;
        Console.WriteLine(a1[3]);
        // 124
        byte[] a2 = new byte[1+2] { 127, 0, 1 };
        Console.WriteLine(a2[0]);
        // 123
        ushort[][,] a3 = new ushort[idx][,];
        // 1234
        short[][] a4 = new short[9 - 7][] { new short[1] { -1 }, new short[3] { short.MinValue, 0, short.MaxValue } };
        Console.WriteLine(a4[1][fidx - 2]);
        // 134
        string[][] a5 = new string[][] { new string[] { ""A"" }, new string[] { ""B"", ""b"" }, new string[] { ""C"", null, ""CCC"" } };
        Console.WriteLine(a5[fidx][2]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
123
127
-32768
CCC
");

            #region IL
            // Can NOT compare IL because the Guid is different every time - <PrivateImplementationDetails>{a6c2d596-042b-4294-99ab-d34a2758ec15}
#if false
            compilation.VerifyIL("MyArray.Main",
@"{
  // Code size      219 (0xdb)
  .maxstack  7
  .locals init (sbyte[] V_0, //a1
  sbyte V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  stsfld     ""uint MyArray.fidx""
  IL_0006:  ldc.i4.5
  IL_0007:  conv.i8
  IL_0008:  conv.ovf.i.un
  IL_0009:  newarr     ""sbyte""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.0
  IL_0011:  ldloc.0
  IL_0012:  ldsfld     ""uint MyArray.fidx""
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  ldc.i4.s   123
  IL_001b:  dup
  IL_001c:  stloc.1
  IL_001d:  stelem.i1
  IL_001e:  ldloc.1
  IL_001f:  stelem.i1
  IL_0020:  ldloc.0
  IL_0021:  ldc.i4.3
  IL_0022:  ldelem.i1
  IL_0023:  call       ""void System.Console.WriteLine(int)""
  IL_0028:  ldc.i4.3
  IL_0029:  newarr     ""byte""
  IL_002e:  dup
  IL_002f:  ldtoken    ""<PrivateImplementationDetails>{a6c2d596-042b-4294-99ab-d34a2758ec15}.__StaticArrayInitTypeSize=3  <PrivateImplementationDetails>{a6c2d596-042b-4294-99ab-d34a2758ec15}.0""
  IL_0034:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0039:  ldc.i4.0
  IL_003a:  ldelem.u1
  IL_003b:  call       ""void System.Console.WriteLine(int)""
  IL_0040:  ldc.i4.s   9
  IL_0042:  conv.i8
  IL_0043:  conv.ovf.i
  IL_0044:  newarr     ""ushort[,]""
  IL_0049:  pop
  IL_004a:  ldc.i4.2
  IL_004b:  newarr     ""short[]""
  IL_0050:  dup
  IL_0051:  ldc.i4.0
  IL_0052:  ldc.i4.1
  IL_0053:  newarr     ""short""
  IL_0058:  dup
  IL_0059:  ldc.i4.0
  IL_005a:  ldc.i4.m1
  IL_005b:  stelem.i2
  IL_005c:  stelem.ref
  IL_005d:  dup
  IL_005e:  ldc.i4.1
  IL_005f:  ldc.i4.3
  IL_0060:  newarr     ""short""
  IL_0065:  dup
  IL_0066:  ldtoken    ""<PrivateImplementationDetails>{a6c2d596-042b-4294-99ab-d34a2758ec15}.__StaticArrayInitTypeSize=6  <PrivateImplementationDetails>{a6c2d596-042b-4294-99ab-d34a2758ec15}.1""
  IL_006b:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0070:  stelem.ref
  IL_0071:  ldc.i4.1
  IL_0072:  ldelem.ref
  IL_0073:  ldsfld     ""uint MyArray.fidx""
  IL_0078:  ldc.i4.2
  IL_0079:  sub
  IL_007a:  ldelem.i2
  IL_007b:  call       ""void System.Console.WriteLine(int)""
  IL_0080:  ldc.i4.3
  IL_0081:  newarr     ""string[]""
  IL_0086:  dup
  IL_0087:  ldc.i4.0
  IL_0088:  ldc.i4.1
  IL_0089:  newarr     ""string""
  IL_008e:  dup
  IL_008f:  ldc.i4.0
  IL_0090:  ldstr      ""A""
  IL_0095:  stelem.ref
  IL_0096:  stelem.ref
  IL_0097:  dup
  IL_0098:  ldc.i4.1
  IL_0099:  ldc.i4.2
  IL_009a:  newarr     ""string""
  IL_009f:  dup
  IL_00a0:  ldc.i4.0
  IL_00a1:  ldstr      ""B""
  IL_00a6:  stelem.ref
  IL_00a7:  dup
  IL_00a8:  ldc.i4.1
  IL_00a9:  ldstr      ""b""
  IL_00ae:  stelem.ref
  IL_00af:  stelem.ref
  IL_00b0:  dup
  IL_00b1:  ldc.i4.2
  IL_00b2:  ldc.i4.3
  IL_00b3:  newarr     ""string""
  IL_00b8:  dup
  IL_00b9:  ldc.i4.0
  IL_00ba:  ldstr      ""C""
  IL_00bf:  stelem.ref
  IL_00c0:  dup
  IL_00c1:  ldc.i4.1
  IL_00c2:  ldnull
  IL_00c3:  stelem.ref
  IL_00c4:  dup
  IL_00c5:  ldc.i4.2
  IL_00c6:  ldstr      ""CCC""
  IL_00cb:  stelem.ref
  IL_00cc:  stelem.ref
  IL_00cd:  ldsfld     ""uint MyArray.fidx""
  IL_00d2:  ldelem.ref
  IL_00d3:  ldc.i4.2
  IL_00d4:  ldelem.ref
  IL_00d5:  call       ""void System.Console.WriteLine(string)""
  IL_00da:  ret
}
");
#endif
            #endregion
        }

        [Fact]
        public void AccessTypeParam()
        {
            string source = @"
public class D
{
    public class Outer<K> where K : class
    {
        public class Boo<T, R, U>
            where T : class
            where R : struct
            where U : K
        {
            public Boo()
            {
            }

            public void Goo(T x, R y, U z)
            {
                System.Collections.Generic.List<T> lT = new System.Collections.Generic.List<T>();
                lT.Add(x);
                T[] aT = lT.ToArray();
                System.Console.Write(aT[0].ToString());
                T eT = aT[0];
                System.Console.Write(eT.ToString());

                System.Collections.Generic.List<R> lR = new System.Collections.Generic.List<R>();
                lR.Add(y);
                R[] aR = lR.ToArray();
                System.Console.Write(aR[0].ToString());
                R eR = aR[0];
                System.Console.Write(eR.ToString());

                System.Collections.Generic.List<U> lU = new System.Collections.Generic.List<U>();
                lU.Add(z);
                U[] aU = lU.ToArray();
                System.Console.Write(aU[0].ToString());
                U eU = aU[0];
                System.Console.Write(eU.ToString());
            }
        }
    }

    public static void Main()
    {
        Outer<object>.Boo<string, int, int> b = new Outer<object>.Boo<string, int,  int>();
        b.Goo(""hi"", 42,  123);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "hihi4242123123");

            compilation.VerifyIL("D.Outer<K>.Boo<T, R, U>.Goo",
@"
{
  // Code size      195 (0xc3)
  .maxstack  3
  .locals init (R V_0, //eR
  U V_1) //eU
  IL_0000:  newobj     ""System.Collections.Generic.List<T>..ctor()""
  IL_0005:  dup
  IL_0006:  ldarg.1
  IL_0007:  callvirt   ""void System.Collections.Generic.List<T>.Add(T)""
  IL_000c:  callvirt   ""T[] System.Collections.Generic.List<T>.ToArray()""
  IL_0011:  dup
  IL_0012:  ldc.i4.0
  IL_0013:  ldelem     ""T""
  IL_0018:  box        ""T""
  IL_001d:  callvirt   ""string object.ToString()""
  IL_0022:  call       ""void System.Console.Write(string)""
  IL_0027:  ldc.i4.0
  IL_0028:  ldelem     ""T""
  IL_002d:  box        ""T""
  IL_0032:  callvirt   ""string object.ToString()""
  IL_0037:  call       ""void System.Console.Write(string)""
  IL_003c:  newobj     ""System.Collections.Generic.List<R>..ctor()""
  IL_0041:  dup
  IL_0042:  ldarg.2
  IL_0043:  callvirt   ""void System.Collections.Generic.List<R>.Add(R)""
  IL_0048:  callvirt   ""R[] System.Collections.Generic.List<R>.ToArray()""
  IL_004d:  dup
  IL_004e:  ldc.i4.0
  IL_004f:  readonly.
  IL_0051:  ldelema    ""R""
  IL_0056:  constrained. ""R""
  IL_005c:  callvirt   ""string object.ToString()""
  IL_0061:  call       ""void System.Console.Write(string)""
  IL_0066:  ldc.i4.0
  IL_0067:  ldelem     ""R""
  IL_006c:  stloc.0
  IL_006d:  ldloca.s   V_0
  IL_006f:  constrained. ""R""
  IL_0075:  callvirt   ""string object.ToString()""
  IL_007a:  call       ""void System.Console.Write(string)""
  IL_007f:  newobj     ""System.Collections.Generic.List<U>..ctor()""
  IL_0084:  dup
  IL_0085:  ldarg.3
  IL_0086:  callvirt   ""void System.Collections.Generic.List<U>.Add(U)""
  IL_008b:  callvirt   ""U[] System.Collections.Generic.List<U>.ToArray()""
  IL_0090:  dup
  IL_0091:  ldc.i4.0
  IL_0092:  readonly.
  IL_0094:  ldelema    ""U""
  IL_0099:  constrained. ""U""
  IL_009f:  callvirt   ""string object.ToString()""
  IL_00a4:  call       ""void System.Console.Write(string)""
  IL_00a9:  ldc.i4.0
  IL_00aa:  ldelem     ""U""
  IL_00af:  stloc.1
  IL_00b0:  ldloca.s   V_1
  IL_00b2:  constrained. ""U""
  IL_00b8:  callvirt   ""string object.ToString()""
  IL_00bd:  call       ""void System.Console.Write(string)""
  IL_00c2:  ret
}
");
        }

        [Fact]
        public void ConstrainedCalls()
        {
            string source = @"
public class D
{
    public class Boo
    {
        public int I1;
        public int I2;

        public Boo()
        {
        }
    }

    public class Boo<T>
    {
        public Boo()
        {
        }

        public void Moo(T x, T y)
        {      
         T[] arr = new T[]{x, y, x, y};

            // constrained generic
         string s = arr[1].ToString();

         System.Console.Write(s);
        }
    }

    public static void Main()
    {
        Boo x = new Boo();
        Boo y = new Boo();

        Boo<Boo> b = new Boo<Boo>();
        b.Moo(x, y);

        int iii = 123;

            // constrained nongeneric
        System.Console.Write(iii.GetType());

            // regular call
        System.Console.Write(iii.ToString());
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "D+BooSystem.Int32123");

            compilation.VerifyIL("D.Boo<T>.Moo",
@"{
  // Code size       63 (0x3f)
  .maxstack  4
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""T""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldarg.1
  IL_0009:  stelem     ""T""
  IL_000e:  dup
  IL_000f:  ldc.i4.1
  IL_0010:  ldarg.2
  IL_0011:  stelem     ""T""
  IL_0016:  dup
  IL_0017:  ldc.i4.2
  IL_0018:  ldarg.1
  IL_0019:  stelem     ""T""
  IL_001e:  dup
  IL_001f:  ldc.i4.3
  IL_0020:  ldarg.2
  IL_0021:  stelem     ""T""
  IL_0026:  ldc.i4.1
  IL_0027:  readonly.
  IL_0029:  ldelema    ""T""
  IL_002e:  constrained. ""T""
  IL_0034:  callvirt   ""string object.ToString()""
  IL_0039:  call       ""void System.Console.Write(string)""
  IL_003e:  ret
}
");
            compilation.VerifyIL("D.Main",
@"{
  // Code size       56 (0x38)
  .maxstack  3
  .locals init (D.Boo V_0, //x
  D.Boo V_1, //y
  int V_2) //iii
  IL_0000:  newobj     ""D.Boo..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""D.Boo..ctor()""
  IL_000b:  stloc.1
  IL_000c:  newobj     ""D.Boo<D.Boo>..ctor()""
  IL_0011:  ldloc.0
  IL_0012:  ldloc.1
  IL_0013:  callvirt   ""void D.Boo<D.Boo>.Moo(D.Boo, D.Boo)""
  IL_0018:  ldc.i4.s   123
  IL_001a:  stloc.2
  IL_001b:  ldloc.2
  IL_001c:  box        ""int""
  IL_0021:  call       ""System.Type object.GetType()""
  IL_0026:  call       ""void System.Console.Write(object)""
  IL_002b:  ldloca.s   V_2
  IL_002d:  call       ""string int.ToString()""
  IL_0032:  call       ""void System.Console.Write(string)""
  IL_0037:  ret
}
");
        }

        [Fact]
        public void TestConstructor()
        {
            string source = @"
public class P
{
    public class B
    {
        public B()
        {
            System.Console.Write(""B ctor-"");
        }
    }
    public class D : B
    {
        public D()
        {
            System.Console.Write(""D ctor"");
        }
    }
    public static void Main()
    {
        new D();
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "B ctor-D ctor");

            compilation.VerifyIL("P.D..ctor",
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  call       ""P.B..ctor()""
  IL_0006:  ldstr      ""D ctor""
  IL_000b:  call       ""void System.Console.Write(string)""
  IL_0010:  ret       
}
");
        }

        [Fact]
        public void BinderTemps()
        {
            string source = @"
public class D
{
    public class C1
    {
        public int Goo(int x, int y)
        {
            System.Console.Write(""["");
            System.Console.Write(x);   
            System.Console.Write(y);              
            System.Console.Write(""]"");
            
            return 8;
        }

        public C1()
        {
        }
    }

    public static int GetInt(int i)
    {
        System.Console.Write(""["");
        System.Console.Write(i);
        System.Console.Write(""]"");
        return i;
    }

    public static void Main()
    {
        C1 c = new C1();

        c.Goo(y: GetInt(2), 
              x: GetInt(3));

        System.Console.Write("" "");

        c.Goo(   GetInt(1), 
                 c.Goo(y: GetInt(2), 
                       x: GetInt(3)));

        System.Console.Write("" "");
        
        c.Goo(y: GetInt(1), 
              x: c.Goo(y: GetInt(2), 
                       x: GetInt(3)));
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
[2][3][32] [1][2][3][32][18] [1][2][3][32][81]
");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size      116 (0x74)
  .maxstack  5
  .locals init (D.C1 V_0, //c
                int V_1,
                int V_2)
  IL_0000:  newobj     ""D.C1..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.2
  IL_0008:  call       ""int D.GetInt(int)""
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.3
  IL_000f:  call       ""int D.GetInt(int)""
  IL_0014:  ldloc.1
  IL_0015:  callvirt   ""int D.C1.Goo(int, int)""
  IL_001a:  pop
  IL_001b:  ldstr      "" ""
  IL_0020:  call       ""void System.Console.Write(string)""
  IL_0025:  ldloc.0
  IL_0026:  ldc.i4.1
  IL_0027:  call       ""int D.GetInt(int)""
  IL_002c:  ldloc.0
  IL_002d:  ldc.i4.2
  IL_002e:  call       ""int D.GetInt(int)""
  IL_0033:  stloc.1
  IL_0034:  ldc.i4.3
  IL_0035:  call       ""int D.GetInt(int)""
  IL_003a:  ldloc.1
  IL_003b:  callvirt   ""int D.C1.Goo(int, int)""
  IL_0040:  callvirt   ""int D.C1.Goo(int, int)""
  IL_0045:  pop
  IL_0046:  ldstr      "" ""
  IL_004b:  call       ""void System.Console.Write(string)""
  IL_0050:  ldloc.0
  IL_0051:  ldc.i4.1
  IL_0052:  call       ""int D.GetInt(int)""
  IL_0057:  stloc.1
  IL_0058:  ldloc.0
  IL_0059:  ldc.i4.2
  IL_005a:  call       ""int D.GetInt(int)""
  IL_005f:  stloc.2
  IL_0060:  ldc.i4.3
  IL_0061:  call       ""int D.GetInt(int)""
  IL_0066:  ldloc.2
  IL_0067:  callvirt   ""int D.C1.Goo(int, int)""
  IL_006c:  ldloc.1
  IL_006d:  callvirt   ""int D.C1.Goo(int, int)""
  IL_0072:  pop
  IL_0073:  ret
}
");
        }

        [Fact]
        public void BinderTempsNestedScopes()
        {
            string source = @"
public class D
{
    public class C1
    {
        public int Goo(int x, int y)
        {
            System.Console.Write(""["");
            System.Console.Write(x);   
            System.Console.Write(y);              
            System.Console.Write(""]"");
            
            return 8;
        }

        public C1()
        {
        }
    }

    public static int GetInt(int i)
    {
        System.Console.Write(""["");
        System.Console.Write(i);
        System.Console.Write(""]"");
        return i;
    }

    public static void Main()
    {
        C1 c = new C1();
       
        c.Goo(y: GetInt(1), 
              x: c.Goo(y: c.Goo(y: GetInt(1), 
                                x: c.Goo(y: GetInt(2), 
                                         x: GetInt(3))), 
                       x: GetInt(3)));
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
[1][1][2][3][32][81][3][38][81]
");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size       68 (0x44)
  .maxstack  6
  .locals init (int V_0,
                int V_1,
                int V_2,
                int V_3)
  IL_0000:  newobj     ""D.C1..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  call       ""int D.GetInt(int)""
  IL_000c:  stloc.0
  IL_000d:  dup
  IL_000e:  dup
  IL_000f:  ldc.i4.1
  IL_0010:  call       ""int D.GetInt(int)""
  IL_0015:  stloc.2
  IL_0016:  ldc.i4.2
  IL_0017:  call       ""int D.GetInt(int)""
  IL_001c:  stloc.3
  IL_001d:  ldc.i4.3
  IL_001e:  call       ""int D.GetInt(int)""
  IL_0023:  ldloc.3
  IL_0024:  callvirt   ""int D.C1.Goo(int, int)""
  IL_0029:  ldloc.2
  IL_002a:  callvirt   ""int D.C1.Goo(int, int)""
  IL_002f:  stloc.1
  IL_0030:  ldc.i4.3
  IL_0031:  call       ""int D.GetInt(int)""
  IL_0036:  ldloc.1
  IL_0037:  callvirt   ""int D.C1.Goo(int, int)""
  IL_003c:  ldloc.0
  IL_003d:  callvirt   ""int D.C1.Goo(int, int)""
  IL_0042:  pop
  IL_0043:  ret
}");
        }

        //This is mostly to test array creation.
        //param call will certainly create one. 
        [Fact]
        public void ParamCallCreatesArray()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        System.Console.Write(""{0}{1}{2}{3}{4}{5}"", ""a"", ""b"", ""c"", ""d"", ""e"", ""f"");
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"abcdef");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       65 (0x41)
  .maxstack  5
  IL_0000:  ldstr      ""{0}{1}{2}{3}{4}{5}""
  IL_0005:  ldc.i4.6  
  IL_0006:  newarr     ""object""
  IL_000b:  dup       
  IL_000c:  ldc.i4.0  
  IL_000d:  ldstr      ""a""
  IL_0012:  stelem.ref
  IL_0013:  dup       
  IL_0014:  ldc.i4.1  
  IL_0015:  ldstr      ""b""
  IL_001a:  stelem.ref
  IL_001b:  dup       
  IL_001c:  ldc.i4.2  
  IL_001d:  ldstr      ""c""
  IL_0022:  stelem.ref
  IL_0023:  dup       
  IL_0024:  ldc.i4.3  
  IL_0025:  ldstr      ""d""
  IL_002a:  stelem.ref
  IL_002b:  dup       
  IL_002c:  ldc.i4.4  
  IL_002d:  ldstr      ""e""
  IL_0032:  stelem.ref
  IL_0033:  dup       
  IL_0034:  ldc.i4.5  
  IL_0035:  ldstr      ""f""
  IL_003a:  stelem.ref
  IL_003b:  call       ""void System.Console.Write(string, params object[])""
  IL_0040:  ret       
}
");
        }

        // Test that Array.Empty<T>() is used instead of "new T[0]" when Array.Empty<T>() is available.
        [Fact]
        public void ParamCallUsesCachedArray()
        {
            var verifier = CompileAndVerify(@"
namespace System
{
    public class Object { }
    public class ValueType { }
    public struct Int32 { }
    public class String { }
    public class Attribute { }
    public struct Void { }
    public class ParamArrayAttribute { }
    public abstract class Array {
        public static T[] Empty<T>() { return new T[0]; }
    }
}

public class Program
{
    public static void Callee1(params object[] values) { }
    public static void Callee2(params int[] values) { }
    public static void Callee3<T>(params T[] values) { }

    public static void Main() { }

    public static void M<T>()
    {
        Callee1();
        Callee1(System.Array.Empty<object>());
        Callee1(null);
        Callee1(new object[0]);
        Callee1(""Hello"");
        Callee1(""Hello"", ""World"");

        Callee2();
        Callee2(System.Array.Empty<int>());
        Callee2(null);
        Callee2(new int[0]);
        Callee2(1);
        Callee2(1, 2);

        Callee3<string>();
        Callee3<string>(System.Array.Empty<string>());
        Callee3<string>(null);
        Callee3<string>(new string[0]);
        Callee3<string>(""Hello"");
        Callee3<string>(""Hello"", ""World"");

        Callee3<T>();
        Callee3<T>(System.Array.Empty<T>());
        Callee3<T>(null);
        Callee3<T>(new T[0]);
        Callee3<T>(default(T));
        Callee3<T>(default(T), default(T));
    }
}
", verify: Verification.Fails, options: TestOptions.ReleaseExe);
            verifier.VerifyIL("Program.M<T>()",
@"{
  // Code size      297 (0x129)
  .maxstack  4
  IL_0000:  call       ""object[] System.Array.Empty<object>()""
  IL_0005:  call       ""void Program.Callee1(params object[])""
  IL_000a:  call       ""object[] System.Array.Empty<object>()""
  IL_000f:  call       ""void Program.Callee1(params object[])""
  IL_0014:  ldnull
  IL_0015:  call       ""void Program.Callee1(params object[])""
  IL_001a:  ldc.i4.0
  IL_001b:  newarr     ""object""
  IL_0020:  call       ""void Program.Callee1(params object[])""
  IL_0025:  ldc.i4.1
  IL_0026:  newarr     ""object""
  IL_002b:  dup
  IL_002c:  ldc.i4.0
  IL_002d:  ldstr      ""Hello""
  IL_0032:  stelem.ref
  IL_0033:  call       ""void Program.Callee1(params object[])""
  IL_0038:  ldc.i4.2
  IL_0039:  newarr     ""object""
  IL_003e:  dup
  IL_003f:  ldc.i4.0
  IL_0040:  ldstr      ""Hello""
  IL_0045:  stelem.ref
  IL_0046:  dup
  IL_0047:  ldc.i4.1
  IL_0048:  ldstr      ""World""
  IL_004d:  stelem.ref
  IL_004e:  call       ""void Program.Callee1(params object[])""
  IL_0053:  call       ""int[] System.Array.Empty<int>()""
  IL_0058:  call       ""void Program.Callee2(params int[])""
  IL_005d:  call       ""int[] System.Array.Empty<int>()""
  IL_0062:  call       ""void Program.Callee2(params int[])""
  IL_0067:  ldnull
  IL_0068:  call       ""void Program.Callee2(params int[])""
  IL_006d:  ldc.i4.0
  IL_006e:  newarr     ""int""
  IL_0073:  call       ""void Program.Callee2(params int[])""
  IL_0078:  ldc.i4.1
  IL_0079:  newarr     ""int""
  IL_007e:  dup
  IL_007f:  ldc.i4.0
  IL_0080:  ldc.i4.1
  IL_0081:  stelem.i4
  IL_0082:  call       ""void Program.Callee2(params int[])""
  IL_0087:  ldc.i4.2
  IL_0088:  newarr     ""int""
  IL_008d:  dup
  IL_008e:  ldc.i4.0
  IL_008f:  ldc.i4.1
  IL_0090:  stelem.i4
  IL_0091:  dup
  IL_0092:  ldc.i4.1
  IL_0093:  ldc.i4.2
  IL_0094:  stelem.i4
  IL_0095:  call       ""void Program.Callee2(params int[])""
  IL_009a:  call       ""string[] System.Array.Empty<string>()""
  IL_009f:  call       ""void Program.Callee3<string>(params string[])""
  IL_00a4:  call       ""string[] System.Array.Empty<string>()""
  IL_00a9:  call       ""void Program.Callee3<string>(params string[])""
  IL_00ae:  ldnull
  IL_00af:  call       ""void Program.Callee3<string>(params string[])""
  IL_00b4:  ldc.i4.0
  IL_00b5:  newarr     ""string""
  IL_00ba:  call       ""void Program.Callee3<string>(params string[])""
  IL_00bf:  ldc.i4.1
  IL_00c0:  newarr     ""string""
  IL_00c5:  dup
  IL_00c6:  ldc.i4.0
  IL_00c7:  ldstr      ""Hello""
  IL_00cc:  stelem.ref
  IL_00cd:  call       ""void Program.Callee3<string>(params string[])""
  IL_00d2:  ldc.i4.2
  IL_00d3:  newarr     ""string""
  IL_00d8:  dup
  IL_00d9:  ldc.i4.0
  IL_00da:  ldstr      ""Hello""
  IL_00df:  stelem.ref
  IL_00e0:  dup
  IL_00e1:  ldc.i4.1
  IL_00e2:  ldstr      ""World""
  IL_00e7:  stelem.ref
  IL_00e8:  call       ""void Program.Callee3<string>(params string[])""
  IL_00ed:  call       ""T[] System.Array.Empty<T>()""
  IL_00f2:  call       ""void Program.Callee3<T>(params T[])""
  IL_00f7:  call       ""T[] System.Array.Empty<T>()""
  IL_00fc:  call       ""void Program.Callee3<T>(params T[])""
  IL_0101:  ldnull
  IL_0102:  call       ""void Program.Callee3<T>(params T[])""
  IL_0107:  ldc.i4.0
  IL_0108:  newarr     ""T""
  IL_010d:  call       ""void Program.Callee3<T>(params T[])""
  IL_0112:  ldc.i4.1
  IL_0113:  newarr     ""T""
  IL_0118:  call       ""void Program.Callee3<T>(params T[])""
  IL_011d:  ldc.i4.2
  IL_011e:  newarr     ""T""
  IL_0123:  call       ""void Program.Callee3<T>(params T[])""
  IL_0128:  ret
}
");

            verifier = CompileAndVerify(@"
namespace System
{
    public class Object { }
    public class ValueType { }
    public struct Int32 { }
    public class String { }
    public class Attribute { }
    public struct Void { }
    public class ParamArrayAttribute { }
    public abstract class Array { }
}

public class Program
{
    public static void Callee1(params object[] values) { }
    public static void Callee2(params int[] values) { }
    public static void Callee3<T>(params T[] values) { }

    public static void Main() { }

    public static void M<T>()
    {
        Callee1();
        Callee2();
        Callee3<string>();
    }
}
", verify: Verification.Fails, options: TestOptions.ReleaseExe);
            verifier.VerifyIL("Program.M<T>()",
@"{
  // Code size       34 (0x22)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""object""
  IL_0006:  call       ""void Program.Callee1(params object[])""
  IL_000b:  ldc.i4.0
  IL_000c:  newarr     ""int""
  IL_0011:  call       ""void Program.Callee2(params int[])""
  IL_0016:  ldc.i4.0
  IL_0017:  newarr     ""string""
  IL_001c:  call       ""void Program.Callee3<string>(params string[])""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void BoxingConversions()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        object o = 123;
        int i = (int)o;
        System.Console.Write(i);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"123");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldc.i4.s   123
  IL_0002:  box        ""int""
  IL_0007:  unbox.any  ""int""
  IL_000c:  call       ""void System.Console.Write(int)""
  IL_0011:  ret
}
");
        }

        [Fact]
        public void AccessingThis()
        {
            string source = @"

public class D
{
    public class Goo
    {
        public int x;

        public void Bar()
        {
            x = 1234;
        }
    }

    public struct GooS
    {
        public int x;

        public void Bar()
        {
            x = 1234;
        }
    }

    public static void Main()
    {
        Goo f = new Goo();
        System.Console.Write(f.x);
        f.Bar();
        System.Console.Write(f.x);

        GooS fs = new GooS();
        System.Console.Write(fs.x);
        fs.Bar();
        System.Console.Write(fs.x);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"0123401234");

            compilation.VerifyIL("D.Goo.Bar",
@"{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldc.i4     0x4d2
  IL_0006:  stfld      ""int D.Goo.x""
  IL_000b:  ret       
}
");

            compilation.VerifyIL("D.GooS.Bar",
@"{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldc.i4     0x4d2
  IL_0006:  stfld      ""int D.GooS.x""
  IL_000b:  ret       
}
");
        }

        [Fact]
        public void AccessingThis1()
        {
            string source = @"
public class D
{
public class Moo<T>
{
    private string[] y;
    private T[] z;

    public void Assign(string[] y, T[] z, string y1, T z1)
    {
        this.y = y;
        this.z = z;

        this.y[1] = y1;
        this.z[1] = z1;
    }

    public void Print()
    {
        System.Console.Write(this.y[0].ToString());
        System.Console.Write(this.z[0].ToString());

        System.Console.Write(this.y[1].ToString());
        System.Console.Write(this.z[1].ToString());
    }

    public Moo()
    {
    }
}

public struct MooS<T>
{
    private string[] y;
    private T[] z;

    public void Assign(string[] y, T[] z, string y1, T z1)
    {
        this.y = y;
        this.z = z;

        this.y[1] = y1;
        this.z[1] = z1;

        MooS<T> tmp = this;
        this = tmp;
    }

    public void Print()
    {
        System.Console.Write(this.y[0].ToString());
        System.Console.Write(this.z[0].ToString());

        System.Console.Write(this.y[1].ToString());
        System.Console.Write(this.z[1].ToString());
    }
}


public static void Main()
{

    System.ApplicationException[] earr = new System.ApplicationException[]
                    {
                        new System.ApplicationException(""hello""), 
                        new System.ApplicationException(""hi"")
                    };

    string[] sarr = new string[] {""aaaa"", ""bbbb"", ""aaaa"", ""bbbb"", ""aaaa"", ""bbbb"",};
 
    Moo<System.Exception> obj = new Moo<System.Exception>();
    obj.Assign(sarr, earr, ""bye"", new System.ApplicationException(""cccc""));
    obj.Print();   

    MooS<System.Exception> objS = new MooS<System.Exception>();
    objS.Assign(sarr, earr, ""bye"", new System.ApplicationException(""cccc""));
    objS.Print();   
}}";

            // If we ever stop verifying the execution of this, we need to add IL verification for some of the method bodies.
            var compilation = CompileAndVerify(source, expectedOutput: @"
aaaaSystem.ApplicationException: hellobyeSystem.ApplicationException: ccccaaaaSystem.ApplicationException: hellobyeSystem.ApplicationException: cccc
");
        }

        [Fact]
        public void AccessingBase()
        {
            string source = @"
public class D
{
    public class Goo
    {
        public int x;

        public void Bar()
        {
            x = 1234;
        }
    }

    public class GooD : Goo
    {
        public new int x = 5555;
        public void Baz()
        {
            base.x = 4321;
            x = 7777;
        }
    }

    public static void Main()
    {
        GooD fd = new GooD();
        System.Console.Write(((Goo)fd).x);
        fd.Bar();
        System.Console.Write(((Goo)fd).x);
        fd.Baz();
        System.Console.Write(((Goo)fd).x);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"012344321");

            compilation.VerifyIL("D.GooD.Baz",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4     0x10e1
  IL_0006:  stfld      ""int D.Goo.x""
  IL_000b:  ldarg.0
  IL_000c:  ldc.i4     0x1e61
  IL_0011:  stfld      ""int D.GooD.x""
  IL_0016:  ret
}
");
        }

        [Fact]
        public void UnaryOp()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        int x = 1;
        x = -x;
        System.Console.Write(x);
        x = ~x;
        System.Console.Write(x);

        long y = -x;
        System.Console.Write(y);
        y = ~y;
        System.Console.Write(y);

        double z = -y;
        System.Console.Write(z);
        z = -z;
        System.Console.Write(z);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"-100-11-1");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       45 (0x2d)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  neg
  IL_0002:  dup
  IL_0003:  call       ""void System.Console.Write(int)""
  IL_0008:  not
  IL_0009:  dup
  IL_000a:  call       ""void System.Console.Write(int)""
  IL_000f:  neg
  IL_0010:  conv.i8
  IL_0011:  dup
  IL_0012:  call       ""void System.Console.Write(long)""
  IL_0017:  not
  IL_0018:  dup
  IL_0019:  call       ""void System.Console.Write(long)""
  IL_001e:  neg
  IL_001f:  conv.r8
  IL_0020:  dup
  IL_0021:  call       ""void System.Console.Write(double)""
  IL_0026:  neg
  IL_0027:  call       ""void System.Console.Write(double)""
  IL_002c:  ret
}
");
        }

        [Fact]
        public void BinaryOp()
        {
            string source = @"

public class D
{
    public static void Main()
    {
        int x = 1;
        x = x + x;
        System.Console.Write(x);
        System.Console.Write("" "");

        x = x << x;
        System.Console.Write(x);
        System.Console.Write("" "");

        long y = x + x;
        System.Console.Write(y);
        System.Console.Write("" "");

        y =  y + x;
        System.Console.Write(y);
        System.Console.Write("" "");

        double z = y;
        z = z * z;
        System.Console.Write(z);
        System.Console.Write("" "");

        z = z / 2;
        System.Console.Write(z);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"2 8 16 24 576 288");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size      119 (0x77)
  .maxstack  3
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldloc.0
  IL_0004:  add
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""void System.Console.Write(int)""
  IL_000c:  ldstr      "" ""
  IL_0011:  call       ""void System.Console.Write(string)""
  IL_0016:  ldloc.0
  IL_0017:  ldloc.0
  IL_0018:  ldc.i4.s   31
  IL_001a:  and
  IL_001b:  shl
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  call       ""void System.Console.Write(int)""
  IL_0023:  ldstr      "" ""
  IL_0028:  call       ""void System.Console.Write(string)""
  IL_002d:  ldloc.0
  IL_002e:  ldloc.0
  IL_002f:  add
  IL_0030:  conv.i8
  IL_0031:  dup
  IL_0032:  call       ""void System.Console.Write(long)""
  IL_0037:  ldstr      "" ""
  IL_003c:  call       ""void System.Console.Write(string)""
  IL_0041:  ldloc.0
  IL_0042:  conv.i8
  IL_0043:  add
  IL_0044:  dup
  IL_0045:  call       ""void System.Console.Write(long)""
  IL_004a:  ldstr      "" ""
  IL_004f:  call       ""void System.Console.Write(string)""
  IL_0054:  conv.r8
  IL_0055:  dup
  IL_0056:  mul
  IL_0057:  dup
  IL_0058:  call       ""void System.Console.Write(double)""
  IL_005d:  ldstr      "" ""
  IL_0062:  call       ""void System.Console.Write(string)""
  IL_0067:  ldc.r8     2
  IL_0070:  div
  IL_0071:  call       ""void System.Console.Write(double)""
  IL_0076:  ret
}
");
        }

        [Fact]
        public void LogOp()
        {
            string source = @"

public class D
{
    public static void Main()
    {
        int x = 1;
        bool b = x == 1;
        System.Console.Write(b);
        System.Console.Write("" "");

        long l = 5;
        System.Console.Write(l > 6);
        System.Console.Write("" "");

        float f = 25;
        System.Console.Write(f >= 25);
        System.Console.Write("" "");

        double d = 3;
        System.Console.Write(d <= f);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"True False True True");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       94 (0x5e)
  .maxstack  2
  .locals init (float V_0) //f
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.1
  IL_0002:  ceq
  IL_0004:  call       ""void System.Console.Write(bool)""
  IL_0009:  ldstr      "" ""
  IL_000e:  call       ""void System.Console.Write(string)""
  IL_0013:  ldc.i4.5
  IL_0014:  conv.i8
  IL_0015:  ldc.i4.6
  IL_0016:  conv.i8
  IL_0017:  cgt
  IL_0019:  call       ""void System.Console.Write(bool)""
  IL_001e:  ldstr      "" ""
  IL_0023:  call       ""void System.Console.Write(string)""
  IL_0028:  ldc.r4     25
  IL_002d:  stloc.0
  IL_002e:  ldloc.0
  IL_002f:  ldc.r4     25
  IL_0034:  clt.un
  IL_0036:  ldc.i4.0
  IL_0037:  ceq
  IL_0039:  call       ""void System.Console.Write(bool)""
  IL_003e:  ldstr      "" ""
  IL_0043:  call       ""void System.Console.Write(string)""
  IL_0048:  ldc.r8     3
  IL_0051:  ldloc.0
  IL_0052:  conv.r8
  IL_0053:  cgt.un
  IL_0055:  ldc.i4.0
  IL_0056:  ceq
  IL_0058:  call       ""void System.Console.Write(bool)""
  IL_005d:  ret
}
");
        }

        [Fact]
        public void UnsignedOp()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        int x = -100;
        x = x >> 2;
        System.Console.Write(x);
        System.Console.Write("" "");

        uint ux = 1;
        uint uy = ux - ux;
        uy = uy - ux;
        System.Console.Write(uy);
        System.Console.Write("" "");

        uy = uy >> 2;
        System.Console.Write(uy);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"-25 4294967295 1073741823");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (uint V_0) //ux
  IL_0000:  ldc.i4.s   -100
  IL_0002:  ldc.i4.2
  IL_0003:  shr
  IL_0004:  call       ""void System.Console.Write(int)""
  IL_0009:  ldstr      "" ""
  IL_000e:  call       ""void System.Console.Write(string)""
  IL_0013:  ldc.i4.1
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  ldloc.0
  IL_0017:  sub
  IL_0018:  ldloc.0
  IL_0019:  sub
  IL_001a:  dup
  IL_001b:  call       ""void System.Console.Write(uint)""
  IL_0020:  ldstr      "" ""
  IL_0025:  call       ""void System.Console.Write(string)""
  IL_002a:  ldc.i4.2
  IL_002b:  shr.un
  IL_002c:  call       ""void System.Console.Write(uint)""
  IL_0031:  ret
}
");
        }

        [Fact]
        public void ReadonlyAddressConstrained()
        {
            string source = @"
public class D
{
    public class Moo<T>
    {

        public void Boo(T x)
        {
            T local = x;
            System.Console.Write(local.ToString());
        }

        public void Goo(T[] x)
        {
            System.Console.Write(x.ToString());      // no need for readonly
            Boo(x[0]);                               // no need for readonly
            System.Console.Write(x[0].ToString());   // readonly         
        }

        public Moo()
        {
        }
    }

    public static void Main()
    {
        Moo<System.Exception> obj = new Moo<System.Exception>();
        obj.Goo(new System.ApplicationException[]{new System.ApplicationException(""hello"")});
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
System.ApplicationException[]System.ApplicationException: helloSystem.ApplicationException: hello
");

            compilation.VerifyIL("D.Moo<T>.Goo",
@"{
  // Code size       50 (0x32)
  .maxstack  3
  IL_0000:  ldarg.1   
  IL_0001:  callvirt   ""string object.ToString()""
  IL_0006:  call       ""void System.Console.Write(string)""
  IL_000b:  ldarg.0   
  IL_000c:  ldarg.1   
  IL_000d:  ldc.i4.0  
  IL_000e:  ldelem     ""T""
  IL_0013:  call       ""void D.Moo<T>.Boo(T)""
  IL_0018:  ldarg.1   
  IL_0019:  ldc.i4.0  
  IL_001a:  readonly. 
  IL_001c:  ldelema    ""T""
  IL_0021:  constrained. ""T""
  IL_0027:  callvirt   ""string object.ToString()""
  IL_002c:  call       ""void System.Console.Write(string)""
  IL_0031:  ret       
}
");
        }

        [WorkItem(22858, "https://github.com/dotnet/roslyn/issues/22858")]
        [Fact]
        public void CovariantArrayRefParam()
        {
            string source = @"
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Test(ref ((object[])new string[] { ""hi"", ""hello"" })[0]);
            }
            catch (System.ArrayTypeMismatchException)
            {
                System.Console.WriteLine(""PASS"");
            }
        }

        static void Test(ref object r)
        {
            throw null;
        }
    }";

            var compilation = CompileAndVerify(source, expectedOutput: @"PASS", verify: Verification.Passes);

            compilation.VerifyIL("Program.Main(string[])",
@"
{
  // Code size       51 (0x33)
  .maxstack  4
  .locals init (object[] V_0)
  .try
  {
    IL_0000:  ldc.i4.2
    IL_0001:  newarr     ""string""
    IL_0006:  dup
    IL_0007:  ldc.i4.0
    IL_0008:  ldstr      ""hi""
    IL_000d:  stelem.ref
    IL_000e:  dup
    IL_000f:  ldc.i4.1
    IL_0010:  ldstr      ""hello""
    IL_0015:  stelem.ref
    IL_0016:  stloc.0
    IL_0017:  ldloc.0
    IL_0018:  ldc.i4.0
    IL_0019:  ldelema    ""object""
    IL_001e:  call       ""void Program.Test(ref object)""
    IL_0023:  leave.s    IL_0032
  }
  catch System.ArrayTypeMismatchException
  {
    IL_0025:  pop
    IL_0026:  ldstr      ""PASS""
    IL_002b:  call       ""void System.Console.WriteLine(string)""
    IL_0030:  leave.s    IL_0032
  }
  IL_0032:  ret
}
");
        }

        [WorkItem(22841, "https://github.com/dotnet/roslyn/issues/22841")]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
        [Fact]
        public void ReadonlyAddressInParam()
        {
            string source = @"
    class Program
    {
        static void Main(string[] args)
        {
            var stringArray = new string[] { ""hi"", ""hello"" };
            var objectArray = (object[])stringArray;

            Test(objectArray[0]);
        }

        static void Test(in object r)
        {
            System.Console.WriteLine(r);
        }
    }";

            var compilation = CompileAndVerify(source, expectedOutput: @"hi", verify: Verification.Fails);

            compilation.VerifyIL("Program.Main(string[])",
@"
{
  // Code size       38 (0x26)
  .maxstack  4
  .locals init (object[] V_0)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""string""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""hi""
  IL_000d:  stelem.ref
  IL_000e:  dup
  IL_000f:  ldc.i4.1
  IL_0010:  ldstr      ""hello""
  IL_0015:  stelem.ref
  IL_0016:  stloc.0
  IL_0017:  ldloc.0
  IL_0018:  ldc.i4.0
  IL_0019:  readonly.
  IL_001b:  ldelema    ""object""
  IL_0020:  call       ""void Program.Test(in object)""
  IL_0025:  ret
}
");
            compilation = CompileAndVerify(source, expectedOutput: @"hi", verify: Verification.Passes, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());

            compilation.VerifyIL("Program.Main(string[])",
@"
{
  // Code size       35 (0x23)
  .maxstack  4
  .locals init (object[] V_0,
                object V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""string""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""hi""
  IL_000d:  stelem.ref
  IL_000e:  dup
  IL_000f:  ldc.i4.1
  IL_0010:  ldstr      ""hello""
  IL_0015:  stelem.ref
  IL_0016:  stloc.0
  IL_0017:  ldloc.0
  IL_0018:  ldc.i4.0
  IL_0019:  ldelem.ref
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  call       ""void Program.Test(in object)""
  IL_0022:  ret
}
");
        }

        [WorkItem(22841, "https://github.com/dotnet/roslyn/issues/22841")]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
        [Fact]
        public void ReadonlyAddressStrict()
        {
            string source = @"
    class Program
    {
        static void Main(string[] args)
        {
            var stringArray = new string[] { ""hi"", ""hello"" };
            var objectArray = (object[])stringArray;

            Test(GetElementRef(objectArray));
        }

        static ref readonly T GetElementRef<T>(T[] objectArray)
        {
            Test(in objectArray[0]);
            return ref objectArray[0];
        }

        static void Test<T>(in T r)
        {
            System.Console.Write(r);
        }
    }";

            var compilation = CompileAndVerify(source, expectedOutput: @"hihi", verify: Verification.Fails);

            var expectedIL = @"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  readonly.
  IL_0004:  ldelema    ""T""
  IL_0009:  call       ""void Program.Test<T>(in T)""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.0
  IL_0010:  readonly.
  IL_0012:  ldelema    ""T""
  IL_0017:  ret
}
";
            compilation.VerifyIL("Program.GetElementRef<T>(T[])", expectedIL);

            // expect the same IL in the compat case since direct references are required and must be emitted with "readonly.", even though unverifiable
            compilation = CompileAndVerify(source, expectedOutput: @"hihi", verify: Verification.Fails, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());

            compilation.VerifyIL("Program.GetElementRef<T>(T[])", expectedIL);
        }

        [Fact]
        public void EnumConv()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        long l = 1L;

        System.Runtime.GCLatencyMode x = (System.Runtime.GCLatencyMode)l;
        System.Console.Write(x);

        int y = (int)x;
        System.Console.Write(y);

        short z = (short)x;
        System.Console.Write(z);

        ushort z1 = (ushort)x;
        System.Console.Write(z1);

        x = (System.Runtime.GCLatencyMode)z1;

        System.StringComparison sc = (System.StringComparison)x;
        System.Console.Write(sc);

        System.Security.SecurityRuleSet sr = (System.Security.SecurityRuleSet)x;
        System.Console.Write(sr);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
Interactive111CurrentCultureIgnoreCaseLevel1
");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       57 (0x39)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  conv.i8
  IL_0002:  conv.i4
  IL_0003:  dup
  IL_0004:  box        ""System.Runtime.GCLatencyMode""
  IL_0009:  call       ""void System.Console.Write(object)""
  IL_000e:  dup
  IL_000f:  call       ""void System.Console.Write(int)""
  IL_0014:  dup
  IL_0015:  conv.i2
  IL_0016:  call       ""void System.Console.Write(int)""
  IL_001b:  conv.u2
  IL_001c:  dup
  IL_001d:  call       ""void System.Console.Write(int)""
  IL_0022:  dup
  IL_0023:  box        ""System.StringComparison""
  IL_0028:  call       ""void System.Console.Write(object)""
  IL_002d:  conv.u1
  IL_002e:  box        ""System.Security.SecurityRuleSet""
  IL_0033:  call       ""void System.Console.Write(object)""
  IL_0038:  ret
}
");
        }

        [Fact]
        public void TestListSample()
        {
            string source = @"
using System;
using System.Text;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            MyList<int> l = new MyList<int>();
            l.Add(1);
            l.Add(2);
            l.Add(3);

            Console.WriteLine(l.ToString());
            Console.WriteLine(l.Count());
            int[] arr = l.ToArray();
            Console.Write(arr[0]);
            Console.Write(',');
            Console.Write(arr[1]);
            Console.Write(',');
            Console.Write(arr[2]);
            Console.WriteLine();
        }
    }
}

public class MyList<T>
{
    public class Node
    {
        public T element;
        public Node next;

        public Node(T element)
        {
            this.element = element;
        }
    };

    Node head;

    public void Add(T element)
    {
        if (head != null)
        {
            Node t = head;
            while (t.next != null)
                t = t.next;

            t.next = new Node(element);
        }
        else
        {
            head = new Node(element);
        }
    }

    public override string ToString()
    {
        StringBuilder stbldr = new StringBuilder();

        Node t = head;
        while (t != null)
        {
            stbldr.AppendLine(t.element.ToString());
            t = t.next;
        }

        return stbldr.ToString();
    }

    public int Count()
    {
        int count = 0;
        Node node = head;
        while (node != null)
        {
            count = count + 1;
            node = node.next;
        }
        return count;
    }

    public T[] ToArray()
    {
        T[] arr = new T[Count()];
        int i = 0;
        Node node = head;
        while (node != null)
        {
            arr[i] = node.element;
            i = i + 1;
            node = node.next;
        }
        return arr;
    }
}";

            // If we ever stop verifying the execution of this, we need to add IL verification for some of the method bodies.
            CompileAndVerify(source, expectedOutput: @"
1
2
3

3
1,2,3
");
        }

        [Fact]
        public void BranchesAndReturn()
        {
            string source = @"
public class D
{
    static int R;

    static void M(bool getLocal, int arg)
    {
        int y = 123;

        if (getLocal)
        {
            while (getLocal)
            {
                if (!getLocal)
                {
                    R = y;
                }
                else
                {
                    R = y;
                    return;
                }
            }
            return;
        }
        else
        {
            R = arg;
            return;
        }
    }

    public static void Main()
    {
        M(false, 42);
        System.Console.Write(D.R);
        M(true, 42);
        System.Console.Write(D.R);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"42123");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldc.i4.0  
  IL_0001:  ldc.i4.s   42
  IL_0003:  call       ""void D.M(bool, int)""
  IL_0008:  ldsfld     ""int D.R""
  IL_000d:  call       ""void System.Console.Write(int)""
  IL_0012:  ldc.i4.1  
  IL_0013:  ldc.i4.s   42
  IL_0015:  call       ""void D.M(bool, int)""
  IL_001a:  ldsfld     ""int D.R""
  IL_001f:  call       ""void System.Console.Write(int)""
  IL_0024:  ret       
}
");
        }

        [Fact]
        public void UnaryLogOp()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        bool x = bool.Parse(""true"");
        bool y = !!!!!!!!!!!!!!!x;

        if (!y)
        {
            System.Console.Write(x);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"True");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (bool V_0) //x
  IL_0000:  ldstr      ""true""
  IL_0005:  call       ""bool bool.Parse(string)""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  ceq
  IL_000f:  brtrue.s   IL_0017
  IL_0011:  ldloc.0
  IL_0012:  call       ""void System.Console.Write(bool)""
  IL_0017:  ret
}
");
        }

        [Fact]
        public void LogAndOr()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        bool x = bool.Parse(""true"");
        bool y = bool.Parse(""false"");

        bool z = !(!(x || y) && x);

        if ((x || y) && x)
        {
            System.Console.Write(z);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"True");

            // The last if condition is more compact than in Dev10.
            compilation.VerifyIL("D.Main", @"
{
  // Code size       48 (0x30)
  .maxstack  2
  .locals init (bool V_0, //x
  bool V_1, //y
  bool V_2) //z
  IL_0000:  ldstr      ""true""
  IL_0005:  call       ""bool bool.Parse(string)""
  IL_000a:  stloc.0
  IL_000b:  ldstr      ""false""
  IL_0010:  call       ""bool bool.Parse(string)""
  IL_0015:  stloc.1
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  or
  IL_0019:  ldc.i4.0
  IL_001a:  ceq
  IL_001c:  ldloc.0
  IL_001d:  and
  IL_001e:  ldc.i4.0
  IL_001f:  ceq
  IL_0021:  stloc.2
  IL_0022:  ldloc.0
  IL_0023:  ldloc.1
  IL_0024:  or
  IL_0025:  ldloc.0
  IL_0026:  and
  IL_0027:  brfalse.s  IL_002f
  IL_0029:  ldloc.2
  IL_002a:  call       ""void System.Console.Write(bool)""
  IL_002f:  ret
}");
        }

        [Fact]
        public void RelationalOps1()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        int x = 1;
        int y = 2;

        if (x < y)
        {
            if (y > x)
            {
                if (y >= x)
                {
                    if (y != x)
                    {
                        System.Console.Write(1);
                    }
                    if (y == x)
                    {
                        System.Console.Write(1);
                    }
                }
            }
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: @"1").
                VerifyIL("D.Main",
@"{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (int V_0, //x
           int V_1) //y
  IL_0000:  ldc.i4.1  
  IL_0001:  stloc.0   
  IL_0002:  ldc.i4.2  
  IL_0003:  stloc.1   
  IL_0004:  ldloc.0   
  IL_0005:  ldloc.1   
  IL_0006:  bge.s      IL_0024
  IL_0008:  ldloc.1   
  IL_0009:  ldloc.0   
  IL_000a:  ble.s      IL_0024
  IL_000c:  ldloc.1   
  IL_000d:  ldloc.0   
  IL_000e:  blt.s      IL_0024
  IL_0010:  ldloc.1   
  IL_0011:  ldloc.0   
  IL_0012:  beq.s      IL_001a
  IL_0014:  ldc.i4.1  
  IL_0015:  call       ""void System.Console.Write(int)""
  IL_001a:  ldloc.1   
  IL_001b:  ldloc.0   
  IL_001c:  bne.un.s   IL_0024
  IL_001e:  ldc.i4.1  
  IL_001f:  call       ""void System.Console.Write(int)""
  IL_0024:  ret       
}
");

            var v = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: @"1");

            v.VerifyIL("D.Main",
@"{
  // Code size       82 (0x52)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1, //y
                bool V_2,
                bool V_3,
                bool V_4,
                bool V_5,
                bool V_6)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
 -IL_0003:  ldc.i4.2
  IL_0004:  stloc.1
 -IL_0005:  ldloc.0
  IL_0006:  ldloc.1
  IL_0007:  clt
  IL_0009:  stloc.2
 ~IL_000a:  ldloc.2
  IL_000b:  brfalse.s  IL_0051
 -IL_000d:  nop
 -IL_000e:  ldloc.1
  IL_000f:  ldloc.0
  IL_0010:  cgt
  IL_0012:  stloc.3
 ~IL_0013:  ldloc.3
  IL_0014:  brfalse.s  IL_0050
 -IL_0016:  nop
 -IL_0017:  ldloc.1
  IL_0018:  ldloc.0
  IL_0019:  clt
  IL_001b:  ldc.i4.0
  IL_001c:  ceq
  IL_001e:  stloc.s    V_4
 ~IL_0020:  ldloc.s    V_4
  IL_0022:  brfalse.s  IL_004f
 -IL_0024:  nop
 -IL_0025:  ldloc.1
  IL_0026:  ldloc.0
  IL_0027:  ceq
  IL_0029:  ldc.i4.0
  IL_002a:  ceq
  IL_002c:  stloc.s    V_5
 ~IL_002e:  ldloc.s    V_5
  IL_0030:  brfalse.s  IL_003b
 -IL_0032:  nop
 -IL_0033:  ldc.i4.1
  IL_0034:  call       ""void System.Console.Write(int)""
  IL_0039:  nop
 -IL_003a:  nop
 -IL_003b:  ldloc.1
  IL_003c:  ldloc.0
  IL_003d:  ceq
  IL_003f:  stloc.s    V_6
 ~IL_0041:  ldloc.s    V_6
  IL_0043:  brfalse.s  IL_004e
 -IL_0045:  nop
 -IL_0046:  ldc.i4.1
  IL_0047:  call       ""void System.Console.Write(int)""
  IL_004c:  nop
 -IL_004d:  nop
 -IL_004e:  nop
 -IL_004f:  nop
 -IL_0050:  nop
 -IL_0051:  ret
}
", sequencePoints: "D.Main");
        }

        [Fact]
        public void RelationalOps2()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        uint x = 1;
        uint y = 2;

        if (x < y)
        {
            if (!(x > y))
            {
                if (y >= x)
                {
                    if (y != x)
                    {
                        System.Console.Write(1);
                    }
                    if (y == x)
                    {
                        System.Console.Write(1);
                    }
                }
            }
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (uint V_0, //x
           uint V_1) //y
  IL_0000:  ldc.i4.1  
  IL_0001:  stloc.0   
  IL_0002:  ldc.i4.2  
  IL_0003:  stloc.1   
  IL_0004:  ldloc.0   
  IL_0005:  ldloc.1   
  IL_0006:  bge.un.s   IL_0024
  IL_0008:  ldloc.0   
  IL_0009:  ldloc.1   
  IL_000a:  bgt.un.s   IL_0024
  IL_000c:  ldloc.1   
  IL_000d:  ldloc.0   
  IL_000e:  blt.un.s   IL_0024
  IL_0010:  ldloc.1   
  IL_0011:  ldloc.0   
  IL_0012:  beq.s      IL_001a
  IL_0014:  ldc.i4.1  
  IL_0015:  call       ""void System.Console.Write(int)""
  IL_001a:  ldloc.1   
  IL_001b:  ldloc.0   
  IL_001c:  bne.un.s   IL_0024
  IL_001e:  ldc.i4.1  
  IL_001f:  call       ""void System.Console.Write(int)""
  IL_0024:  ret       
}
");
        }

        [Fact]
        public void SimpleCompareRef()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        bool x = bool.Parse(""true"");

        if (x.ToString() != null) 
        {
            System.Console.Write(x);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"True");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (bool V_0) //x
  IL_0000:  ldstr      ""true""
  IL_0005:  call       ""bool bool.Parse(string)""
  IL_000a:  stloc.0   
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""string bool.ToString()""
  IL_0012:  brfalse.s  IL_001a
  IL_0014:  ldloc.0   
  IL_0015:  call       ""void System.Console.Write(bool)""
  IL_001a:  ret       
}
");
        }

        [Fact]
        public void SimpleCompare()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        bool x = bool.Parse(""true"");

        if ((!!!!!!!!!!!!!!!!x) == true)
        {
            System.Console.Write(x);
        }

        if ((!!!!!!!!!!!!!!!x) == false)
        {
            System.Console.Write(x);
        }

        ulong u123 = ulong.Parse(""123"");
        if (u123 != 0)
        {
            System.Console.Write(x);
        }

        long l123 = long.Parse(""123"");
        if (-l123 != 0)
        {
            System.Console.Write(x);
        }

        double NaN = 1.0/0;
        if (NaN != 0)
        {
            System.Console.Write(x);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"TrueTrueTrueTrueTrue");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       93 (0x5d)
  .maxstack  2
  .locals init (bool V_0) //x
  IL_0000:  ldstr      ""true""
  IL_0005:  call       ""bool bool.Parse(string)""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  brfalse.s  IL_0014
  IL_000e:  ldloc.0
  IL_000f:  call       ""void System.Console.Write(bool)""
  IL_0014:  ldloc.0
  IL_0015:  brfalse.s  IL_001d
  IL_0017:  ldloc.0
  IL_0018:  call       ""void System.Console.Write(bool)""
  IL_001d:  ldstr      ""123""
  IL_0022:  call       ""ulong ulong.Parse(string)""
  IL_0027:  brfalse.s  IL_002f
  IL_0029:  ldloc.0
  IL_002a:  call       ""void System.Console.Write(bool)""
  IL_002f:  ldstr      ""123""
  IL_0034:  call       ""long long.Parse(string)""
  IL_0039:  neg
  IL_003a:  brfalse.s  IL_0042
  IL_003c:  ldloc.0
  IL_003d:  call       ""void System.Console.Write(bool)""
  IL_0042:  ldc.r8     Infinity
  IL_004b:  ldc.r8     0
  IL_0054:  beq.s      IL_005c
  IL_0056:  ldloc.0
  IL_0057:  call       ""void System.Console.Write(bool)""
  IL_005c:  ret
}
");
        }

        [Fact]
        public void ConstBranch()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        if (!!false) {
            System.Console.Write(0);
        }else{
            System.Console.Write(1);
        }

        if (!!!!!!!!!!!!!!!!true)
        {
            System.Console.Write(2);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"12");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldc.i4.1  
  IL_0001:  call       ""void System.Console.Write(int)""
  IL_0006:  ldc.i4.2  
  IL_0007:  call       ""void System.Console.Write(int)""
  IL_000c:  ret       
}
");
        }

        [Fact]
        public void ByrefArg()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        System.Collections.Generic.Dictionary<int, string> dict = new System.Collections.Generic.Dictionary<int, string>();

        dict.Add(1, ""one"");
        dict.Add(2, ""two"");

        string s = """";
        dict.TryGetValue(1, out s);
        System.Console.Write(s);

        dict.TryGetValue(2, out s);
        System.Console.Write(s);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"onetwo");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       67 (0x43)
  .maxstack  4
  .locals init (string V_0) //s
  IL_0000:  newobj     ""System.Collections.Generic.Dictionary<int, string>..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  ldstr      ""one""
  IL_000c:  callvirt   ""void System.Collections.Generic.Dictionary<int, string>.Add(int, string)""
  IL_0011:  dup
  IL_0012:  ldc.i4.2
  IL_0013:  ldstr      ""two""
  IL_0018:  callvirt   ""void System.Collections.Generic.Dictionary<int, string>.Add(int, string)""
  IL_001d:  ldstr      """"
  IL_0022:  stloc.0
  IL_0023:  dup
  IL_0024:  ldc.i4.1
  IL_0025:  ldloca.s   V_0
  IL_0027:  callvirt   ""bool System.Collections.Generic.Dictionary<int, string>.TryGetValue(int, out string)""
  IL_002c:  pop
  IL_002d:  ldloc.0
  IL_002e:  call       ""void System.Console.Write(string)""
  IL_0033:  ldc.i4.2
  IL_0034:  ldloca.s   V_0
  IL_0036:  callvirt   ""bool System.Collections.Generic.Dictionary<int, string>.TryGetValue(int, out string)""
  IL_003b:  pop
  IL_003c:  ldloc.0
  IL_003d:  call       ""void System.Console.Write(string)""
  IL_0042:  ret
}
");
        }

        [Fact]
        public void ByrefParam()
        {
            string source = @"
public class D
{
    public static void Inc(ref int x)
    {
        x = x + 1;
    }

    public static void Main()
    {
        int x = 1;
        Inc(ref x);
        System.Console.Write(x);

        Inc(ref x);
        System.Console.Write(x);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"23");

            compilation.VerifyIL("D.Inc",
@"
{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i4
  IL_0003:  ldc.i4.1
  IL_0004:  add
  IL_0005:  stind.i4
  IL_0006:  ret
}
");
        }

        [Fact]
        public void ByrefArg2()
        {
            string source = @"
public class D
{
            
    public static void ProxyGet(System.Collections.Generic.Dictionary<int, string> dict, int val, out string s)
    {
        dict.TryGetValue(val, out s);
    }

    public static void Main()
    {
        System.Collections.Generic.Dictionary<int, string> dict = new System.Collections.Generic.Dictionary<int, string>();

        dict.Add(1, ""one"");
        dict.Add(2, ""two"");

        string s = """";
        ProxyGet(dict, 1, out s);
        System.Console.Write(s);

        ProxyGet(dict, 2, out s);
        System.Console.Write(s);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"onetwo");

            compilation.VerifyIL("D.ProxyGet",
@"{
  // Code size       10 (0xa)
  .maxstack  3
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  ldarg.2   
  IL_0003:  callvirt   ""bool System.Collections.Generic.Dictionary<int, string>.TryGetValue(int, out string)""
  IL_0008:  pop       
  IL_0009:  ret       
}
");
        }

        [Fact]
        public void ByrefParamRef()
        {
            string source = @"
public class D
{
    public static void Inc(ref object x)
    {
        string s = (string)x;
        x = string.Concat(s, ""#"");
    }

    public static void Main()
    {
        object x = ""A"";
        Inc(ref x);
        System.Console.Write(x);

        Inc(ref x);
        System.Console.Write(x);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"A#A##");

            compilation.VerifyIL("D.Inc",
@"{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (string V_0) //s
  IL_0000:  ldarg.0   
  IL_0001:  ldind.ref 
  IL_0002:  castclass  ""string""
  IL_0007:  stloc.0   
  IL_0008:  ldarg.0   
  IL_0009:  ldloc.0   
  IL_000a:  ldstr      ""#""
  IL_000f:  call       ""string string.Concat(string, string)""
  IL_0014:  stind.ref 
  IL_0015:  ret       
}
");
        }

        [Fact]
        public void ByrefParamStruct()
        {
            string source = @"
public class D
{
    public struct S1
    {
        public S2 x;
    }

    public struct S2
    {
        public int x;
    }

    public static void Inc(ref S1 x)
    {
        x.x.x = x.x.x + 1;
    }

    public static void Main()
    {
        S1 x = new S1();
        Inc(ref x);
        System.Console.Write(x.x.x);

        Inc(ref x);
        System.Console.Write(x.x.x);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"12");

            compilation.VerifyIL("D.Inc",
@"{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  ldarg.0   
  IL_0001:  ldflda     ""D.S2 D.S1.x""
  IL_0006:  ldarg.0   
  IL_0007:  ldflda     ""D.S2 D.S1.x""
  IL_000c:  ldfld      ""int D.S2.x""
  IL_0011:  ldc.i4.1  
  IL_0012:  add       
  IL_0013:  stfld      ""int D.S2.x""
  IL_0018:  ret       
}
");
        }

        [Fact]
        public void ByrefParamEnum()
        {
            string source = @"
public class D
{
    public static void Inc(ref System.StringComparison x)
    {
        x = x + 1;
    }

    public static void Main()
    {
        System.StringComparison x = (System.StringComparison)0;
        Inc(ref x);
        System.Console.Write(x);

        Inc(ref x);
        System.Console.Write(x);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"CurrentCultureIgnoreCaseInvariantCulture");

            compilation.VerifyIL("D.Inc",
@"
{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i4
  IL_0003:  ldc.i4.1
  IL_0004:  add
  IL_0005:  stind.i4
  IL_0006:  ret
}
");
        }

        [Fact]
        public void RefAssign()
        {
            string source = @"
public class D
{
    public static void Inc(ref int x, ref int y)
    {
        System.Console.Write(x = y); // temp is NOT a ref here.
        y = x + 1;
    }

    public static void Main()
    {
        int x = 0;
        int y = 1;
        Inc(ref x, ref y);
        Inc(ref x, ref y);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"12");

            compilation.VerifyIL("D.Inc",
@"{
  // Code size       19 (0x13)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  ldind.i4  
  IL_0003:  dup       
  IL_0004:  stloc.0   
  IL_0005:  stind.i4  
  IL_0006:  ldloc.0   
  IL_0007:  call       ""void System.Console.Write(int)""
  IL_000c:  ldarg.1   
  IL_000d:  ldarg.0   
  IL_000e:  ldind.i4  
  IL_000f:  ldc.i4.1  
  IL_0010:  add       
  IL_0011:  stind.i4  
  IL_0012:  ret       
}
");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void ByrefTemp1()
        {
            string source = @"
public class D
{
    public class DD
    {
        public int[] a1;
        public int[] a2;

        public DD()
        {
            a1 = new int[] {1,2,3,4,5,6};
            a2 = new int[] {4,5,6,7,8,9};
        }

        public int[] B()
        {
                System.Console.Write(""B"");         
                return a1;            
        }

        public int C()
        {
                System.Console.Write(""C"");
                return 1;
        }

        public int[] D()
        {
                System.Console.Write(""D"");         
                return a2; 
        }

        public int E()
        {
            System.Console.Write(""E"");
            return 1;
        }

        public void M(ref int x, ref int y)
        {
            x = 42;
            y = 24;
        }

        public void Test()
        {
            // this will require a ref temp for ref B()[C()]
            M(y: ref B()[C()], x: ref D()[E()]);
        }
    }
            
    public static void Main()
    {
        DD v = new DD();
        v.Test();

        System.Console.Write(v.a1[1]);
        System.Console.Write(v.a2[1]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"BCDE2442");

            compilation.VerifyIL("D.DD.Test",
@"{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (int& V_0)
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.0   
  IL_0002:  call       ""int[] D.DD.B()""
  IL_0007:  ldarg.0   
  IL_0008:  call       ""int D.DD.C()""
  IL_000d:  ldelema    ""int""
  IL_0012:  stloc.0   
  IL_0013:  ldarg.0   
  IL_0014:  call       ""int[] D.DD.D()""
  IL_0019:  ldarg.0   
  IL_001a:  call       ""int D.DD.E()""
  IL_001f:  ldelema    ""int""
  IL_0024:  ldloc.0   
  IL_0025:  call       ""void D.DD.M(ref int, ref int)""
  IL_002a:  ret       
}
");
        }

        [Fact]
        public void LongBranch()
        {
            string source = @"
public class D
{
    public static double x;

    public static void Goo(double x)
    {
    }

    public static void Main()
    {
        if (x == 0)  // long branch
        {
            Goo(1.0);
            Goo(1.0);
            Goo(1.0);
            Goo(1.0);
            Goo(1.0);
            Goo(1.0);
            Goo(1.0);
            Goo(1.0);
            Goo(1.0);
            Goo(1.0);
        }
        else
        {
            if (x == 0) // short branch
            {
                Goo(1.0);
                Goo(1.0);
                Goo(1.0);
                Goo(1.0);
                Goo(1.0);
                Goo(1.0);
                Goo(1.0);
                Goo(1.0);
                Goo(1.0);
            }
        }                

        System.Console.Write(""hi"");
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"hi");

            compilation.VerifyIL("D.Main",
@"{
  // Code size      317 (0x13d)
  .maxstack  2
  IL_0000:  ldsfld     ""double D.x""
  IL_0005:  ldc.r8     0
  IL_000e:  bne.un     IL_00a4
  IL_0013:  ldc.r8     1
  IL_001c:  call       ""void D.Goo(double)""
  IL_0021:  ldc.r8     1
  IL_002a:  call       ""void D.Goo(double)""
  IL_002f:  ldc.r8     1
  IL_0038:  call       ""void D.Goo(double)""
  IL_003d:  ldc.r8     1
  IL_0046:  call       ""void D.Goo(double)""
  IL_004b:  ldc.r8     1
  IL_0054:  call       ""void D.Goo(double)""
  IL_0059:  ldc.r8     1
  IL_0062:  call       ""void D.Goo(double)""
  IL_0067:  ldc.r8     1
  IL_0070:  call       ""void D.Goo(double)""
  IL_0075:  ldc.r8     1
  IL_007e:  call       ""void D.Goo(double)""
  IL_0083:  ldc.r8     1
  IL_008c:  call       ""void D.Goo(double)""
  IL_0091:  ldc.r8     1
  IL_009a:  call       ""void D.Goo(double)""
  IL_009f:  br         IL_0132
  IL_00a4:  ldsfld     ""double D.x""
  IL_00a9:  ldc.r8     0
  IL_00b2:  bne.un.s   IL_0132
  IL_00b4:  ldc.r8     1
  IL_00bd:  call       ""void D.Goo(double)""
  IL_00c2:  ldc.r8     1
  IL_00cb:  call       ""void D.Goo(double)""
  IL_00d0:  ldc.r8     1
  IL_00d9:  call       ""void D.Goo(double)""
  IL_00de:  ldc.r8     1
  IL_00e7:  call       ""void D.Goo(double)""
  IL_00ec:  ldc.r8     1
  IL_00f5:  call       ""void D.Goo(double)""
  IL_00fa:  ldc.r8     1
  IL_0103:  call       ""void D.Goo(double)""
  IL_0108:  ldc.r8     1
  IL_0111:  call       ""void D.Goo(double)""
  IL_0116:  ldc.r8     1
  IL_011f:  call       ""void D.Goo(double)""
  IL_0124:  ldc.r8     1
  IL_012d:  call       ""void D.Goo(double)""
  IL_0132:  ldstr      ""hi""
  IL_0137:  call       ""void System.Console.Write(string)""
  IL_013c:  ret       
}
");
        }

        [Fact]
        public void InductiveShortBranches()
        {
            string source = @"
public class D
{
    public static int x;

    public static void Goo(double x)
    {
    }

    public static void Main()
    {
        //some branches will be short
        //some branches will be short only after nested branches become short
        if (x == 0)
        {
            if (x == 0)
            {
                if (x == 0)
                {
                    if (x == 0)
                    {
                        if (x == 0)
                        {
                            if (x == 0)
                            {
                                if (x == 0)
                                {
                                    if (x == 0)
                                    {
                                        if (x == 0)
                                        {
                                            if (x == 0)
                                            {
                                                if (x == 0)
                                                {
                                                    if (x == 0)
                                                    {
                                                        if (x == 0)
                                                        {
                                                            if (x == 0)
                                                            {
                                                                if (x == 0)
                                                                {
                                                                    if (x == 0)
                                                                    {
                                                                        if (x == 0)
                                                                        {
                                                                            if (x == 0)
                                                                            {
                                                                                if (x == 0)
                                                                                {
                                                                                    Goo(1.0);
                                                                                }    
                                                                            }    
                                                                        }    
                                                                    }    
                                                                }    
                                                            }    
                                                        }    
                                                    }    
                                                }    
                                            }    
                                        }    
                                    }    
                                }    
                            }    
                        }    
                    }    
                }    
            }    
        }    

        System.Console.Write(""hi"");
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"hi");

            compilation.VerifyIL("D.Main",
@"{
  // Code size      164 (0xa4)
  .maxstack  1
  IL_0000:  ldsfld     ""int D.x""
  IL_0005:  brtrue     IL_0099
  IL_000a:  ldsfld     ""int D.x""
  IL_000f:  brtrue     IL_0099
  IL_0014:  ldsfld     ""int D.x""
  IL_0019:  brtrue.s   IL_0099
  IL_001b:  ldsfld     ""int D.x""
  IL_0020:  brtrue.s   IL_0099
  IL_0022:  ldsfld     ""int D.x""
  IL_0027:  brtrue.s   IL_0099
  IL_0029:  ldsfld     ""int D.x""
  IL_002e:  brtrue.s   IL_0099
  IL_0030:  ldsfld     ""int D.x""
  IL_0035:  brtrue.s   IL_0099
  IL_0037:  ldsfld     ""int D.x""
  IL_003c:  brtrue.s   IL_0099
  IL_003e:  ldsfld     ""int D.x""
  IL_0043:  brtrue.s   IL_0099
  IL_0045:  ldsfld     ""int D.x""
  IL_004a:  brtrue.s   IL_0099
  IL_004c:  ldsfld     ""int D.x""
  IL_0051:  brtrue.s   IL_0099
  IL_0053:  ldsfld     ""int D.x""
  IL_0058:  brtrue.s   IL_0099
  IL_005a:  ldsfld     ""int D.x""
  IL_005f:  brtrue.s   IL_0099
  IL_0061:  ldsfld     ""int D.x""
  IL_0066:  brtrue.s   IL_0099
  IL_0068:  ldsfld     ""int D.x""
  IL_006d:  brtrue.s   IL_0099
  IL_006f:  ldsfld     ""int D.x""
  IL_0074:  brtrue.s   IL_0099
  IL_0076:  ldsfld     ""int D.x""
  IL_007b:  brtrue.s   IL_0099
  IL_007d:  ldsfld     ""int D.x""
  IL_0082:  brtrue.s   IL_0099
  IL_0084:  ldsfld     ""int D.x""
  IL_0089:  brtrue.s   IL_0099
  IL_008b:  ldc.r8     1
  IL_0094:  call       ""void D.Goo(double)""
  IL_0099:  ldstr      ""hi""
  IL_009e:  call       ""void System.Console.Write(string)""
  IL_00a3:  ret       
}
");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [ConditionalFact(typeof(DesktopOnly))]
        public void InitFromBlob()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        int[] x = new int[] { 1, 2, 3, 4, -5 };

        System.Console.WriteLine(x[2]);
        System.Console.WriteLine(x[4]);

        bool[] b = new bool[] { true, false, true, false, true };

        System.Console.WriteLine(b[2]);
        System.Console.WriteLine(b[3]);

        byte[] by = new byte[] { 0, 127, 223, 128, 220 };

        System.Console.WriteLine(by[2]);
        System.Console.WriteLine(by[3]);

        char[] c = new char[] { 'a', 'b', 'c', 'd', 'e' };

        System.Console.WriteLine(c[2]);
        System.Console.WriteLine(c[4]);

        float[] s = new float[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f };

        System.Console.WriteLine(s[2]);
        System.Console.WriteLine(s[4]);

        double[] d = new double[] { 1.1f, 2.2f, -3.3f / 0, 4.4f, -5.5f };

        System.Console.WriteLine(d[2]);
        System.Console.WriteLine(d[4]);
    }
}";
            var compilation = CompileAndVerifyWithMscorlib40(source, options: TestOptions.ReleaseExe.WithModuleName("MODULE"), expectedOutput: @"
3
-5
True
False
223
128
c
e
3.3
5.5
-Infinity
-5.5
");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size      193 (0xc1)
  .maxstack  3
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.56C14CB445C628421AC674599E302B0879FB496F""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  dup
  IL_0012:  ldc.i4.2
  IL_0013:  ldelem.i4
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ldc.i4.4
  IL_001a:  ldelem.i4
  IL_001b:  call       ""void System.Console.WriteLine(int)""
  IL_0020:  ldc.i4.5
  IL_0021:  newarr     ""bool""
  IL_0026:  dup
  IL_0027:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=5 <PrivateImplementationDetails>.4E724558F6B816715597A51663AD8F05247E2C4A""
  IL_002c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0031:  dup
  IL_0032:  ldc.i4.2
  IL_0033:  ldelem.u1
  IL_0034:  call       ""void System.Console.WriteLine(bool)""
  IL_0039:  ldc.i4.3
  IL_003a:  ldelem.u1
  IL_003b:  call       ""void System.Console.WriteLine(bool)""
  IL_0040:  ldc.i4.5
  IL_0041:  newarr     ""byte""
  IL_0046:  dup
  IL_0047:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=5 <PrivateImplementationDetails>.9755240DD0C4C1AD226DEBD40C6D2EBD408250CB""
  IL_004c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0051:  dup
  IL_0052:  ldc.i4.2
  IL_0053:  ldelem.u1
  IL_0054:  call       ""void System.Console.WriteLine(int)""
  IL_0059:  ldc.i4.3
  IL_005a:  ldelem.u1
  IL_005b:  call       ""void System.Console.WriteLine(int)""
  IL_0060:  ldc.i4.5
  IL_0061:  newarr     ""char""
  IL_0066:  dup
  IL_0067:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=10 <PrivateImplementationDetails>.E313A2813013780396D58750DC5D62221C86F42F""
  IL_006c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0071:  dup
  IL_0072:  ldc.i4.2
  IL_0073:  ldelem.u2
  IL_0074:  call       ""void System.Console.WriteLine(char)""
  IL_0079:  ldc.i4.4
  IL_007a:  ldelem.u2
  IL_007b:  call       ""void System.Console.WriteLine(char)""
  IL_0080:  ldc.i4.5
  IL_0081:  newarr     ""float""
  IL_0086:  dup
  IL_0087:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.2F3DD953DBFB23217E7CE0E76630EBD31267E237""
  IL_008c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0091:  dup
  IL_0092:  ldc.i4.2
  IL_0093:  ldelem.r4
  IL_0094:  call       ""void System.Console.WriteLine(float)""
  IL_0099:  ldc.i4.4
  IL_009a:  ldelem.r4
  IL_009b:  call       ""void System.Console.WriteLine(float)""
  IL_00a0:  ldc.i4.5
  IL_00a1:  newarr     ""double""
  IL_00a6:  dup
  IL_00a7:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=40 <PrivateImplementationDetails>.11F3436B917FFBA0FAB0FAD5563AF18FA24AC16A""
  IL_00ac:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_00b1:  dup
  IL_00b2:  ldc.i4.2
  IL_00b3:  ldelem.r8
  IL_00b4:  call       ""void System.Console.WriteLine(double)""
  IL_00b9:  ldc.i4.4
  IL_00ba:  ldelem.r8
  IL_00bb:  call       ""void System.Console.WriteLine(double)""
  IL_00c0:  ret
}
");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void InitFromBlobPartial()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        int[] x = new int[] {1,2,System.Int32.Parse(""3""),4,5};

        System.Console.Write(x[2]);

        x = new int[] {1,2,3,4, -x[4]};

        System.Console.Write(x[4]);
    }
}
";
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe.WithModuleName("MODULE"), expectedOutput: @"3-5");
            //string pid = "<PrivateImplementationDetails>" + compilation.Compilation.SourceModule.Pers
            compilation.VerifyIL("D.Main",
@"{
  // Code size       73 (0x49)
  .maxstack  5
  .locals init (int[] V_0) //x
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.FF942E5F620FC460CF9424D564C73AD8A99C74EE""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  dup
  IL_0012:  ldc.i4.2
  IL_0013:  ldstr      ""3""
  IL_0018:  call       ""int int.Parse(string)""
  IL_001d:  stelem.i4
  IL_001e:  stloc.0
  IL_001f:  ldloc.0
  IL_0020:  ldc.i4.2
  IL_0021:  ldelem.i4
  IL_0022:  call       ""void System.Console.Write(int)""
  IL_0027:  ldc.i4.5
  IL_0028:  newarr     ""int""
  IL_002d:  dup
  IL_002e:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.89E37886EEEDC70AEF61138E037CC60EFC35535F""
  IL_0033:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0038:  dup
  IL_0039:  ldc.i4.4
  IL_003a:  ldloc.0
  IL_003b:  ldc.i4.4
  IL_003c:  ldelem.i4
  IL_003d:  neg
  IL_003e:  stelem.i4
  IL_003f:  stloc.0
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.4
  IL_0042:  ldelem.i4
  IL_0043:  call       ""void System.Console.Write(int)""
  IL_0048:  ret
}
");
        }

        [Fact]
        public void InitFromBlobPartial001()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        int[] x = new int[] {0,0,System.Int32.Parse(""3""),System.Int32.Parse(""4""),5, 0,0,0,0,0};

        System.Console.Write(x[2]);

        x = new int[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,2,-x[4], 3, 4};

        System.Console.Write(x[4]);
    }
}
";
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe.WithModuleName("MODULE"), expectedOutput: @"30");
            //string pid = "<PrivateImplementationDetails>" + compilation.Compilation.SourceModule.Pers
            compilation.VerifyIL("D.Main",
@"{
  // Code size       82 (0x52)
  .maxstack  5
  .locals init (int[] V_0) //x
  IL_0000:  ldc.i4.s   10
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.2
  IL_0009:  ldstr      ""3""
  IL_000e:  call       ""int int.Parse(string)""
  IL_0013:  stelem.i4
  IL_0014:  dup
  IL_0015:  ldc.i4.3
  IL_0016:  ldstr      ""4""
  IL_001b:  call       ""int int.Parse(string)""
  IL_0020:  stelem.i4
  IL_0021:  dup
  IL_0022:  ldc.i4.4
  IL_0023:  ldc.i4.5
  IL_0024:  stelem.i4
  IL_0025:  stloc.0
  IL_0026:  ldloc.0
  IL_0027:  ldc.i4.2
  IL_0028:  ldelem.i4
  IL_0029:  call       ""void System.Console.Write(int)""
  IL_002e:  ldc.i4.s   15
  IL_0030:  newarr     ""int""
  IL_0035:  dup
  IL_0036:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=60 <PrivateImplementationDetails>.49608711F905702F9F227AA782F8B408777D5DF9""
  IL_003b:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0040:  dup
  IL_0041:  ldc.i4.s   12
  IL_0043:  ldloc.0
  IL_0044:  ldc.i4.4
  IL_0045:  ldelem.i4
  IL_0046:  neg
  IL_0047:  stelem.i4
  IL_0048:  stloc.0
  IL_0049:  ldloc.0
  IL_004a:  ldc.i4.4
  IL_004b:  ldelem.i4
  IL_004c:  call       ""void System.Console.Write(int)""
  IL_0051:  ret
}
");
        }


        [Fact]
        public void ArrayInitFromBlobEnum()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        System.TypeCode[] x = new System.TypeCode[] {
                        System.TypeCode.Boolean,
                        System.TypeCode.Byte, 
                        System.TypeCode.Char, 
                        System.TypeCode.DateTime, 
                        System.TypeCode.DBNull
                    };

        System.Console.WriteLine(x[1]);
        System.Console.WriteLine(x[2]);
        System.Console.WriteLine(x[4]);
    }
}
";
            var compilation = CompileAndVerifyWithMscorlib40(source, expectedOutput: @"
Byte
Char
DBNull
");
            //NOTE: 
            // the emit is specific to the target CLR version
            // on > 4.0 this is optimizable.
            compilation.VerifyIL("D.Main",
@"{
  // Code size       66 (0x42)
  .maxstack  4
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""System.TypeCode""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.3
  IL_0009:  stelem.i4
  IL_000a:  dup
  IL_000b:  ldc.i4.1
  IL_000c:  ldc.i4.6
  IL_000d:  stelem.i4
  IL_000e:  dup
  IL_000f:  ldc.i4.2
  IL_0010:  ldc.i4.4
  IL_0011:  stelem.i4
  IL_0012:  dup
  IL_0013:  ldc.i4.3
  IL_0014:  ldc.i4.s   16
  IL_0016:  stelem.i4
  IL_0017:  dup
  IL_0018:  ldc.i4.4
  IL_0019:  ldc.i4.2
  IL_001a:  stelem.i4
  IL_001b:  dup
  IL_001c:  ldc.i4.1
  IL_001d:  ldelem.i4
  IL_001e:  box        ""System.TypeCode""
  IL_0023:  call       ""void System.Console.WriteLine(object)""
  IL_0028:  dup
  IL_0029:  ldc.i4.2
  IL_002a:  ldelem.i4
  IL_002b:  box        ""System.TypeCode""
  IL_0030:  call       ""void System.Console.WriteLine(object)""
  IL_0035:  ldc.i4.4
  IL_0036:  ldelem.i4
  IL_0037:  box        ""System.TypeCode""
  IL_003c:  call       ""void System.Console.WriteLine(object)""
  IL_0041:  ret
}
");
        }

        [Fact]
        public void ArrayInitFromBlobEnumNetFx45()
        {
            var source = @"
public class D
{
    public static void Main()
    {
        System.TypeCode[] x = new System.TypeCode[] {
                        System.TypeCode.Boolean,
                        System.TypeCode.Byte, 
                        System.TypeCode.Char, 
                        System.TypeCode.DateTime, 
                        System.TypeCode.DBNull
                    };

        System.Console.WriteLine(x[1]);
        System.Console.WriteLine(x[2]);
        System.Console.WriteLine(x[4]);
    }
}
";

            var expectedOutput = @"
Byte
Char
DBNull
";

            var compilation = CreateCompilationWithMscorlib45(source: source, options: TestOptions.ReleaseExe.WithModuleName("MODULE"));
            var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);

            //NOTE: 
            // the emit is specific to the target CLR version
            verifier.VerifyIL("D.Main",
@"{
  // Code size       56 (0x38)
  .maxstack  3
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""System.TypeCode""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.3191FF614021ADF3122AC274EA5B6097C21BEB81""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  dup
  IL_0012:  ldc.i4.1
  IL_0013:  ldelem.i4
  IL_0014:  box        ""System.TypeCode""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  dup
  IL_001f:  ldc.i4.2
  IL_0020:  ldelem.i4
  IL_0021:  box        ""System.TypeCode""
  IL_0026:  call       ""void System.Console.WriteLine(object)""
  IL_002b:  ldc.i4.4
  IL_002c:  ldelem.i4
  IL_002d:  box        ""System.TypeCode""
  IL_0032:  call       ""void System.Console.WriteLine(object)""
  IL_0037:  ret
}
");
        }

        [WorkItem(538105, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538105")]
        [Fact]
        public void Temporaries()
        {
            string source = @"
using System;
class Program
{
    static void Main()
    {
        bool x = true;
        int y = (x != true).GetType().GetHashCode() - x.GetType().GetHashCode(); // Temps involved
        Console.Write((y + y).ToString()); // Temp involved
    }
    public void test()
    {
        this.bar(1).ToString(); // Temp involved
    }
    public int bar(int x)
    {
        return 0;
    }
}";

            var compilation = CompileAndVerify(source, expectedOutput: @"0");

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (bool V_0, //x
                int V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  ceq
  IL_0006:  box        ""bool""
  IL_000b:  call       ""System.Type object.GetType()""
  IL_0010:  callvirt   ""int object.GetHashCode()""
  IL_0015:  ldloc.0
  IL_0016:  box        ""bool""
  IL_001b:  call       ""System.Type object.GetType()""
  IL_0020:  callvirt   ""int object.GetHashCode()""
  IL_0025:  sub
  IL_0026:  dup
  IL_0027:  add
  IL_0028:  stloc.1
  IL_0029:  ldloca.s   V_1
  IL_002b:  call       ""string int.ToString()""
  IL_0030:  call       ""void System.Console.Write(string)""
  IL_0035:  ret
}
");
        }

        [Fact]
        public void TemporariesA()
        {
            string source = @"
using System;
class Program
{
    static void Main()
    {
        bool x = true;
        int y = (x != true).GetType().GetHashCode() - x.GetType().GetHashCode(); // Temps involved
        Console.Write((y + y).ToString()); // Temp involved
    }
    public void test()
    {
        this.bar(1).ToString(); // Temp involved
    }
    public int bar(int x)
    {
        return 0;
    }
}";

            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseDebugExe, expectedOutput: @"0");

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (bool V_0, //x
                int V_1, //y
                int V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  ceq
  IL_0006:  box        ""bool""
  IL_000b:  call       ""System.Type object.GetType()""
  IL_0010:  callvirt   ""int object.GetHashCode()""
  IL_0015:  ldloc.0
  IL_0016:  box        ""bool""
  IL_001b:  call       ""System.Type object.GetType()""
  IL_0020:  callvirt   ""int object.GetHashCode()""
  IL_0025:  sub
  IL_0026:  stloc.1
  IL_0027:  ldloc.1
  IL_0028:  ldloc.1
  IL_0029:  add
  IL_002a:  stloc.2
  IL_002b:  ldloca.s   V_2
  IL_002d:  call       ""string int.ToString()""
  IL_0032:  call       ""void System.Console.Write(string)""
  IL_0037:  ret
}
");
        }


        [Fact]
        public void EmitObjectToStringOnSimpleType()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int x = 123;
        Console.WriteLine(x.ToString());
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"123");

            compilation.VerifyIL("Program.Main",
@"{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.s   123
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""string int.ToString()""
  IL_000a:  call       ""void System.Console.WriteLine(string)""
  IL_000f:  ret
}
");
        }

        [WorkItem(543325, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543325")]
        [Fact()]
        public void EmitCastingMethodGroupToDelegate()
        {
            string source = @"
using System;

class Base
{
    public virtual void F()
    {
    }
}

class Derived : Base
{
    public override void F()
    {
        Action a = this.F;
        a();
    }
}";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("Derived.F",
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  ""void Base.F()""
  IL_0008:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_000d:  callvirt   ""void System.Action.Invoke()""
  IL_0012:  ret
}
");
        }

        [WorkItem(543325, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543325")]
        [Fact()]
        public void EmitCastingMethodGroupToDelegate_2()
        {
            string source = @"
using System;

class Base
{
    public virtual void F()
    {
    }
}

class Derived : Base
{
    public override void F()
    {
        Action a = new Action(this.F);
        a();
    }
}";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("Derived.F",
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  ""void Base.F()""
  IL_0008:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_000d:  callvirt   ""void System.Action.Invoke()""
  IL_0012:  ret
}
");
        }

        [WorkItem(543325, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543325")]
        [Fact()]
        public void EmitCastingMethodGroupToDelegate_3()
        {
            string source = @"
using System;

class Base
{
    public virtual void F<T>()
    {
    }
}

class Derived : Base
{
    public override void F<T>()
    {
        Action a = this.F<T>;
        a();

        a = new Action(this.F<T>);
        a();
    }
}";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("Derived.F<T>",
@"{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  ""void Base.F<T>()""
  IL_0008:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_000d:  callvirt   ""void System.Action.Invoke()""
  IL_0012:  ldarg.0
  IL_0013:  dup
  IL_0014:  ldvirtftn  ""void Base.F<T>()""
  IL_001a:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_001f:  callvirt   ""void System.Action.Invoke()""
  IL_0024:  ret
}
");
        }

        [Fact]
        public void EmitObjectMethodOnSpecialByRefType()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
    }

    static void M(System.TypedReference r)
    {
        bool b = r.Equals(1);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"");

            compilation.VerifyIL("Program.M",
@"{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  box        ""int""
  IL_0008:  call       ""bool System.TypedReference.Equals(object)""
  IL_000d:  pop
  IL_000e:  ret
}
");
        }

        [Fact]
        public void EmitCallToOverriddenToStringOnStruct()
        {
            string source = @"
using System;

class Program
{
    public struct S1
    {
        public override string ToString()
        {
            return ""123"";
        }
    }

    static void Main()
    {
        S1 s1 = new S1();
        Console.Write(s1.ToString());
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"123");

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (Program.S1 V_0) //s1
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""Program.S1""
  IL_0008:  ldloca.s   V_0
  IL_000a:  constrained. ""Program.S1""
  IL_0010:  callvirt   ""string object.ToString()""
  IL_0015:  call       ""void System.Console.Write(string)""
  IL_001a:  ret
}
");
        }

        [Fact]
        public void EmitNonVirtualInstanceEnumMethodCallOnEnum()
        {
            string source = @"
using System;

class Program
{
    enum Shade
    {
        White, Gray, Black
    }

    static void Main()
    {
        Shade v = Shade.Gray;
        Console.WriteLine(v.GetType());
        Console.WriteLine(v.HasFlag(Shade.Black));
        Console.WriteLine(v.ToString(""G""));
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"Program+Shade
False
Gray");

            compilation.VerifyIL("Program.Main",
@"{
  // Code size       62 (0x3e)
  .maxstack  2
  .locals init (Program.Shade V_0) //v
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  box        ""Program.Shade""
  IL_0008:  call       ""System.Type object.GetType()""
  IL_000d:  call       ""void System.Console.WriteLine(object)""
  IL_0012:  ldloc.0
  IL_0013:  box        ""Program.Shade""
  IL_0018:  ldc.i4.2
  IL_0019:  box        ""Program.Shade""
  IL_001e:  call       ""bool System.Enum.HasFlag(System.Enum)""
  IL_0023:  call       ""void System.Console.WriteLine(bool)""
  IL_0028:  ldloc.0
  IL_0029:  box        ""Program.Shade""
  IL_002e:  ldstr      ""G""
  IL_0033:  call       ""string System.Enum.ToString(string)""
  IL_0038:  call       ""void System.Console.WriteLine(string)""
  IL_003d:  ret
}
");
        }

        [Fact]
        public void NestedLoopAndJumpStatements01()
        {
            string source = @"using System;

class Program
{
    public class Test
    {
        public static short x; // = 9; NotImpl

        public long Goo(sbyte y)
        {
            while (y < x*x)
            {
                do
                {
                    if (x > y - 1)
                    {
                        if (x > y + 1)
                        {
                            x = (short)(x - 1);
                        }
                        x = (short)(x - 1);
                        continue;
                    }
                    else
                    {
                        if (x + 1 > y)
                        {
                            x = (short)(x - 2);
                        }
                        else
                        {
                            y = (sbyte)(y - 1);
                        }
                    }
                } while (x > 3);

                if (x < 3)
                    break;
            }

            return x;
        }
    }

    static void Main()
    {
        Test.x = 9;
        Console.Write(new Test().Goo(6));
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"2");

            compilation.VerifyIL("Program.Test.Goo",
@"{
  // Code size      118 (0x76)
  .maxstack  3
  IL_0000:  br.s       IL_0061
  IL_0002:  ldsfld     ""short Program.Test.x""
  IL_0007:  ldarg.1   
  IL_0008:  ldc.i4.1  
  IL_0009:  sub       
  IL_000a:  ble.s      IL_0032
  IL_000c:  ldsfld     ""short Program.Test.x""
  IL_0011:  ldarg.1   
  IL_0012:  ldc.i4.1  
  IL_0013:  add       
  IL_0014:  ble.s      IL_0023
  IL_0016:  ldsfld     ""short Program.Test.x""
  IL_001b:  ldc.i4.1  
  IL_001c:  sub       
  IL_001d:  conv.i2   
  IL_001e:  stsfld     ""short Program.Test.x""
  IL_0023:  ldsfld     ""short Program.Test.x""
  IL_0028:  ldc.i4.1  
  IL_0029:  sub       
  IL_002a:  conv.i2   
  IL_002b:  stsfld     ""short Program.Test.x""
  IL_0030:  br.s       IL_0051
  IL_0032:  ldsfld     ""short Program.Test.x""
  IL_0037:  ldc.i4.1  
  IL_0038:  add       
  IL_0039:  ldarg.1   
  IL_003a:  ble.s      IL_004b
  IL_003c:  ldsfld     ""short Program.Test.x""
  IL_0041:  ldc.i4.2  
  IL_0042:  sub       
  IL_0043:  conv.i2   
  IL_0044:  stsfld     ""short Program.Test.x""
  IL_0049:  br.s       IL_0051
  IL_004b:  ldarg.1   
  IL_004c:  ldc.i4.1  
  IL_004d:  sub       
  IL_004e:  conv.i1   
  IL_004f:  starg.s    V_1
  IL_0051:  ldsfld     ""short Program.Test.x""
  IL_0056:  ldc.i4.3  
  IL_0057:  bgt.s      IL_0002
  IL_0059:  ldsfld     ""short Program.Test.x""
  IL_005e:  ldc.i4.3  
  IL_005f:  blt.s      IL_006f
  IL_0061:  ldarg.1   
  IL_0062:  ldsfld     ""short Program.Test.x""
  IL_0067:  ldsfld     ""short Program.Test.x""
  IL_006c:  mul       
  IL_006d:  blt.s      IL_0002
  IL_006f:  ldsfld     ""short Program.Test.x""
  IL_0074:  conv.i8   
  IL_0075:  ret       
}
");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void OverloadsInvoke()
        {
            string source = @"
using System;

class Program
{
    public class C
    {
        public uint Goo(short p1, ushort p2) { return (ushort) (p1 + p2); }
        public uint Goo(short p1, string p2) { return (uint) p1; }
        public uint Goo(short p1, params ushort[] p2) { return (byte) (p2[0] + p2[1]); }
        public uint Goo(short p1, ref ushort p2) { return p2; }
        public uint Goo(out short p1, params ushort[] p2) { p1 = (sbyte)127; return (ushort)p1; }
        public uint Goo(short p1, out string p2) { p2 = ""Abc123""; return (ushort) (p1 * 3); }
    }

    public static uint field1, field2;
    internal static ushort field3;
    private static string field4;
    static void Main()
    {
        C obj = new C();
        field1 = obj.Goo(-99, 100) + obj.Goo(2, ""QC""); // 1 + 2
        field2 = obj.Goo(-1, 11, 22); // 33
        Console.WriteLine(String.Format(""F1={0}, F2={1}"", field1, field2));
        field3 = 444;
        Console.WriteLine(obj.Goo(12345, ref field3)); // 444

        short out1 = 0;
        uint local = obj.Goo(out out1, 1,2,3,4); // 127
        Console.WriteLine(local);
        local = obj.Goo(2, out field4);
        Console.WriteLine(local); // 6
        Console.WriteLine(field4);
    }
}
";
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe.WithModuleName("MODULE"), expectedOutput: @"
F1=3, F2=33
444
127
6
Abc123
");

            compilation.VerifyIL("Program.Main",
@"{
  // Code size      188 (0xbc)
  .maxstack  6
  .locals init (Program.C V_0, //obj
                short V_1) //out1
  IL_0000:  newobj     ""Program.C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.s   -99
  IL_0009:  ldc.i4.s   100
  IL_000b:  callvirt   ""uint Program.C.Goo(short, ushort)""
  IL_0010:  ldloc.0
  IL_0011:  ldc.i4.2
  IL_0012:  ldstr      ""QC""
  IL_0017:  callvirt   ""uint Program.C.Goo(short, string)""
  IL_001c:  add
  IL_001d:  stsfld     ""uint Program.field1""
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4.m1
  IL_0024:  ldc.i4.2
  IL_0025:  newarr     ""ushort""
  IL_002a:  dup
  IL_002b:  ldc.i4.0
  IL_002c:  ldc.i4.s   11
  IL_002e:  stelem.i2
  IL_002f:  dup
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.s   22
  IL_0033:  stelem.i2
  IL_0034:  callvirt   ""uint Program.C.Goo(short, params ushort[])""
  IL_0039:  stsfld     ""uint Program.field2""
  IL_003e:  ldstr      ""F1={0}, F2={1}""
  IL_0043:  ldsfld     ""uint Program.field1""
  IL_0048:  box        ""uint""
  IL_004d:  ldsfld     ""uint Program.field2""
  IL_0052:  box        ""uint""
  IL_0057:  call       ""string string.Format(string, object, object)""
  IL_005c:  call       ""void System.Console.WriteLine(string)""
  IL_0061:  ldc.i4     0x1bc
  IL_0066:  stsfld     ""ushort Program.field3""
  IL_006b:  ldloc.0
  IL_006c:  ldc.i4     0x3039
  IL_0071:  ldsflda    ""ushort Program.field3""
  IL_0076:  callvirt   ""uint Program.C.Goo(short, ref ushort)""
  IL_007b:  call       ""void System.Console.WriteLine(uint)""
  IL_0080:  ldc.i4.0
  IL_0081:  stloc.1
  IL_0082:  ldloc.0
  IL_0083:  ldloca.s   V_1
  IL_0085:  ldc.i4.4
  IL_0086:  newarr     ""ushort""
  IL_008b:  dup
  IL_008c:  ldtoken    ""long <PrivateImplementationDetails>.E9E8A66A117598333ABACF5B65971C2366E19B6C""
  IL_0091:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0096:  callvirt   ""uint Program.C.Goo(out short, params ushort[])""
  IL_009b:  call       ""void System.Console.WriteLine(uint)""
  IL_00a0:  ldloc.0
  IL_00a1:  ldc.i4.2
  IL_00a2:  ldsflda    ""string Program.field4""
  IL_00a7:  callvirt   ""uint Program.C.Goo(short, out string)""
  IL_00ac:  call       ""void System.Console.WriteLine(uint)""
  IL_00b1:  ldsfld     ""string Program.field4""
  IL_00b6:  call       ""void System.Console.WriteLine(string)""
  IL_00bb:  ret
}
");
        }

        [Fact]
        public void ArrayLength()
        {
            string source = @"
class A
{
   public static void Main()
   {
      int[] arr = new int[5];
      System.Console.Write(arr.Length > 0);
      if (0 < arr.Length)
      {
          System.Console.Write(arr.Length + 1);
      }
      if (arr.Length != 0)
      {
          System.Console.Write(arr.Length == 0);
      }
   }
}

";
            var compilation = CompileAndVerify(source, expectedOutput: "True6False");

            compilation.VerifyIL("A.Main",
@"
{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (int[] V_0) //arr
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldlen
  IL_0009:  ldc.i4.0
  IL_000a:  cgt.un
  IL_000c:  call       ""void System.Console.Write(bool)""
  IL_0011:  ldloc.0
  IL_0012:  ldlen
  IL_0013:  brfalse.s  IL_001f
  IL_0015:  ldloc.0
  IL_0016:  ldlen
  IL_0017:  conv.i4
  IL_0018:  ldc.i4.1
  IL_0019:  add
  IL_001a:  call       ""void System.Console.Write(int)""
  IL_001f:  ldloc.0
  IL_0020:  ldlen
  IL_0021:  brfalse.s  IL_002d
  IL_0023:  ldloc.0
  IL_0024:  ldlen
  IL_0025:  ldc.i4.0
  IL_0026:  ceq
  IL_0028:  call       ""void System.Console.Write(bool)""
  IL_002d:  ret
}");
        }

        [Fact]
        public void ArrayLongLength()
        {
            string source = @"
class A
{
 public static void Main()
 {
  int[] arr = new int[5];
  System.Console.Write(arr.LongLength + 1);
 }
}

";
            var compilation = CompileAndVerify(source, expectedOutput: "6");

            compilation.VerifyIL("A.Main",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""int""
  IL_0006:  ldlen
  IL_0007:  conv.i8
  IL_0008:  ldc.i4.1
  IL_0009:  conv.i8
  IL_000a:  add
  IL_000b:  call       ""void System.Console.Write(long)""
  IL_0010:  ret
}
");
        }

        [Fact]
        public void AssignmentWithRef()
        {
            string source = @"
using System;

class C
{
    public struct S
    {
        string field1;
        long field2;
        public void Goo(string s, ref long n, params long[] ary)
        {
            field1 = s; // field = param
            Console.WriteLine(field1);
            field2 = n; // field = ref param
            Console.WriteLine(field2);
            n = field2 + n; // ref p = field + ref p;
            ulong local = 12345;

            local = local + (ulong)Math.Abs(ary[0]); // local = ary element
            Console.WriteLine(local);
            ary[0] = (long)((long)local + ary[1]); // ary element = local
        }
    }

    long n;
    static void Main()
    {
        C obj = new C();
        S valobj = new S();
        obj.n = 9;
        long[] ary = new long[3];
        ary[0] = ary[1] = ary[2] = 123;
        valobj.Goo(""Qc"", ref obj.n, ary);
        Console.WriteLine(obj.n); 
        Console.WriteLine(ary[0]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
Qc
9
12468
18
12591
");

            compilation.VerifyIL("C.S.Goo",
@"{
  // Code size       81 (0x51)
  .maxstack  5
  .locals init (ulong V_0) //local
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  stfld      ""string C.S.field1""
  IL_0007:  ldarg.0   
  IL_0008:  ldfld      ""string C.S.field1""
  IL_000d:  call       ""void System.Console.WriteLine(string)""
  IL_0012:  ldarg.0   
  IL_0013:  ldarg.2   
  IL_0014:  ldind.i8  
  IL_0015:  stfld      ""long C.S.field2""
  IL_001a:  ldarg.0   
  IL_001b:  ldfld      ""long C.S.field2""
  IL_0020:  call       ""void System.Console.WriteLine(long)""
  IL_0025:  ldarg.2   
  IL_0026:  ldarg.0   
  IL_0027:  ldfld      ""long C.S.field2""
  IL_002c:  ldarg.2   
  IL_002d:  ldind.i8  
  IL_002e:  add       
  IL_002f:  stind.i8  
  IL_0030:  ldc.i4     0x3039
  IL_0035:  conv.i8   
  IL_0036:  stloc.0   
  IL_0037:  ldloc.0   
  IL_0038:  ldarg.3   
  IL_0039:  ldc.i4.0  
  IL_003a:  ldelem.i8 
  IL_003b:  call       ""long System.Math.Abs(long)""
  IL_0040:  add       
  IL_0041:  stloc.0   
  IL_0042:  ldloc.0   
  IL_0043:  call       ""void System.Console.WriteLine(ulong)""
  IL_0048:  ldarg.3   
  IL_0049:  ldc.i4.0  
  IL_004a:  ldloc.0   
  IL_004b:  ldarg.3   
  IL_004c:  ldc.i4.1  
  IL_004d:  ldelem.i8 
  IL_004e:  add       
  IL_004f:  stelem.i8 
  IL_0050:  ret       
}
");
        }

        [WorkItem(538177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538177")]
        [Fact]
        public void Bug3712()
        {
            string source = @"
class A
{
 public static void Main()
 {
  int[] arr;
  arr = new int[5] {1,2,3,4,5};
 }
}

";
            var compilation = CompileAndVerify(source, options: TestOptions.DebugExe.WithModuleName("MODULE"));

            compilation.VerifyIL("A.Main",
@"{
  // Code size       20 (0x14)
  .maxstack  3
  .locals init (int[] V_0) //arr
  IL_0000:  nop
  IL_0001:  ldc.i4.5
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.1036C5F8EF306104BD582D73E555F4DAE8EECB24""
  IL_000d:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0012:  stloc.0
  IL_0013:  ret
}
");
        }

        [WorkItem(538052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538052")]
        [WorkItem(538224, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538224")]
        [Fact]
        public void NamespaceAndTypeAlias()
        {
            string source = @"
namespace N1
{
    using N21;

    namespace N21
    {
        public interface IGoo<T>
        {
            void M(T t, bool b);
        }

        public class C
        {
        }
    }

    namespace N22
    {
        public class C
        {
            public System.String Field = null;
        }

        public class Goo : IGoo<C>
        {
            public void M(C p, bool b)
            {
                System.Console.WriteLine(p.Field);
            }
        }
    }
}

namespace N1.N2.N3.N4.N5.N6.N7.N8.N9.N10
{
}

namespace NS
{
    using N1;
    using N1.N21;
    using C = N1.N22.C;
    using Bob = N1.N2.N3.N4.N5.N6.N7.N8.N9.N10;

    class Test
    {
        //hides namespace alias Bob
        class Bob { }

        static bool global = false;

        static int Main()
        {
            C c = new C();
            c.Field = ""Hello"";
            global::N1.N21.IGoo<C> goo = new global::N1.N22.Goo();
            goo.M(c, global);
            return 0;
        }

        bool M(Bob p, bool b)
        {
            IGoo<long> goo = null;
            if (p != null)
                b = true;

            return b == (goo == null);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "Hello");

            compilation.VerifyIL("NS.Test.Main",
@"{
  // Code size       35 (0x23)
  .maxstack  3
  .locals init (N1.N22.C V_0) //c
  IL_0000:  newobj     ""N1.N22.C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      ""Hello""
  IL_000c:  stfld      ""string N1.N22.C.Field""
  IL_0011:  newobj     ""N1.N22.Goo..ctor()""
  IL_0016:  ldloc.0
  IL_0017:  ldsfld     ""bool NS.Test.global""
  IL_001c:  callvirt   ""void N1.N21.IGoo<N1.N22.C>.M(N1.N22.C, bool)""
  IL_0021:  ldc.i4.0
  IL_0022:  ret
}
");
        }

        [Fact]
        public void VolatileFieldAndNumericTypes()
        {
            string source = @"using System;
namespace CodeGen
{
    public struct MyData
    {
        private volatile bool vField;
        internal static volatile byte vsField;

        public static void Main()
        {
            double cd = 1.234;
            float f = (float)cd;
            short[] ary = new short[3];
            ary[0] = -123;
            MyData obj = new MyData();
            vsField = 0101;
            byte p = MyData.vsField;
            obj.vField = true;
            uint ret = obj.M(ary, ref p, ref f, ref cd);
            Console.Write(ret);
        }

        public uint M(short[] ary, ref byte p1, ref float p2, ref double p3)
        {
            if (vField)
            {
                if (ary[0] <= 0)
                    return (uint)-ary[0] + p1;

                return 12345;
            }
            return 0;
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"224");

            compilation.VerifyIL("CodeGen.MyData.M",
@"{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  volatile. 
  IL_0003:  ldfld      ""bool CodeGen.MyData.vField""
  IL_0008:  brfalse.s  IL_001e
  IL_000a:  ldarg.1   
  IL_000b:  ldc.i4.0  
  IL_000c:  ldelem.i2 
  IL_000d:  ldc.i4.0  
  IL_000e:  bgt.s      IL_0018
  IL_0010:  ldarg.1   
  IL_0011:  ldc.i4.0  
  IL_0012:  ldelem.i2 
  IL_0013:  neg       
  IL_0014:  ldarg.2   
  IL_0015:  ldind.u1  
  IL_0016:  add       
  IL_0017:  ret       
  IL_0018:  ldc.i4     0x3039
  IL_001d:  ret       
  IL_001e:  ldc.i4.0  
  IL_001f:  ret       
}
");
        }

        [Fact]
        public void VolatileFieldAsRefAndEnumTypes()
        {
            string source = @"using System.Threading;
using System;

public enum E
{
    Zero, One, Two
}

class Test
{
    // ref type
    public static volatile string V_2;
    static void Thread2()
    {
        V_2 = ""One"";
    }

    volatile E V_3;
    void Thread3(object p)
    {
        ((Test)p).V_3 = E.Two;
    }

    static void Main()
    {
        var V_1 = new Test();
        new Thread(new ThreadStart(Thread2)).Start();
        new Thread(V_1.Thread3).Start(V_1);

        for (; ;)
        {
            if (V_2 == ""One"" && V_1.V_3 == E.Two)
            {
                Console.WriteLine(""result={0},{1}"", V_2, V_1.V_3);
                return;
            }
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"result=One,Two");
            // Why IL show 
            //  ldfld      "E Test.V_3"
            // Instead of
            //  ldfld      valuetype E modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) Test::V_3
            compilation.VerifyIL("Test.Main",
@"{
      // Code size      112 (0x70)
      .maxstack  3
      .locals init (Test V_0) //V_1
      IL_0000:  newobj     ""Test..ctor()""
      IL_0005:  stloc.0
      IL_0006:  ldnull
      IL_0007:  ldftn      ""void Test.Thread2()""
      IL_000d:  newobj     ""System.Threading.ThreadStart..ctor(object, System.IntPtr)""
      IL_0012:  newobj     ""System.Threading.Thread..ctor(System.Threading.ThreadStart)""
      IL_0017:  call       ""void System.Threading.Thread.Start()""
      IL_001c:  ldloc.0
      IL_001d:  ldftn      ""void Test.Thread3(object)""
      IL_0023:  newobj     ""System.Threading.ParameterizedThreadStart..ctor(object, System.IntPtr)""
      IL_0028:  newobj     ""System.Threading.Thread..ctor(System.Threading.ParameterizedThreadStart)""
      IL_002d:  ldloc.0
      IL_002e:  call       ""void System.Threading.Thread.Start(object)""
      IL_0033:  volatile.
      IL_0035:  ldsfld     ""string Test.V_2""
      IL_003a:  ldstr      ""One""
      IL_003f:  call       ""bool string.op_Equality(string, string)""
      IL_0044:  brfalse.s  IL_0033
      IL_0046:  ldloc.0
      IL_0047:  volatile.
      IL_0049:  ldfld      ""E Test.V_3""
      IL_004e:  ldc.i4.2
      IL_004f:  bne.un.s   IL_0033
      IL_0051:  ldstr      ""result={0},{1}""
      IL_0056:  volatile.
      IL_0058:  ldsfld     ""string Test.V_2""
      IL_005d:  ldloc.0
      IL_005e:  volatile.
      IL_0060:  ldfld      ""E Test.V_3""
      IL_0065:  box        ""E""
      IL_006a:  call       ""void System.Console.WriteLine(string, object, object)""
      IL_006f:  ret
    }
");
        }

        [Fact]
        public void ArithmeticOperations()
        {
            string source = @"using System;
namespace CodeGen
{
    public class MyClass
    {
        public static ushort M(uint[] ary)
        {
            ulong local = 123456789;
            while (ary[0] >= 0)
            {
                ary[0] = (uint)(ary[0] - 1);
                ary[1] = (uint) ((int)(+ary[0]) % +999L);
                local = (local / 7) % ary[1];

                if (local < 9)
                    break;
            }
            return (ushort)local;
        }

        public static void Main()
        {
            uint[] ary = new uint[2];
            ary[0] = ushort.MaxValue;
            ary[1] = (uint)(ary[0] / 13L);
            Console.Write(MyClass.M(ary));
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"6");

            compilation.VerifyIL("CodeGen.MyClass.M",
@"{
  // Code size       57 (0x39)
  .maxstack  4
  .locals init (ulong V_0) //local
  IL_0000:  ldc.i4     0x75bcd15
  IL_0005:  conv.i8   
  IL_0006:  stloc.0   
  IL_0007:  br.s       IL_0030
  IL_0009:  ldarg.0   
  IL_000a:  ldc.i4.0  
  IL_000b:  ldarg.0   
  IL_000c:  ldc.i4.0  
  IL_000d:  ldelem.u4 
  IL_000e:  ldc.i4.1  
  IL_000f:  sub       
  IL_0010:  stelem.i4 
  IL_0011:  ldarg.0   
  IL_0012:  ldc.i4.1  
  IL_0013:  ldarg.0   
  IL_0014:  ldc.i4.0  
  IL_0015:  ldelem.u4 
  IL_0016:  conv.i8   
  IL_0017:  ldc.i4     0x3e7
  IL_001c:  conv.i8   
  IL_001d:  rem       
  IL_001e:  conv.u4   
  IL_001f:  stelem.i4 
  IL_0020:  ldloc.0   
  IL_0021:  ldc.i4.7  
  IL_0022:  conv.i8   
  IL_0023:  div.un    
  IL_0024:  ldarg.0   
  IL_0025:  ldc.i4.1  
  IL_0026:  ldelem.u4 
  IL_0027:  conv.u8   
  IL_0028:  rem.un    
  IL_0029:  stloc.0   
  IL_002a:  ldloc.0   
  IL_002b:  ldc.i4.s   9
  IL_002d:  conv.i8   
  IL_002e:  blt.un.s   IL_0036
  IL_0030:  ldarg.0   
  IL_0031:  ldc.i4.0  
  IL_0032:  ldelem.u4 
  IL_0033:  ldc.i4.0  
  IL_0034:  bge.un.s   IL_0009
  IL_0036:  ldloc.0   
  IL_0037:  conv.u2   
  IL_0038:  ret       
}
");
        }

        [Fact]
        public void Delegates()
        {
            string source = @"using System;

delegate void MyAction<T>(T x);

public class Program
{
    static void F(long l)
    {
        Console.WriteLine(l);
    }

    void G(long l)
    {
        Console.WriteLine(l);
    }

    internal virtual void H(long l)
    {
        Console.WriteLine(l);
    }

    public static void Main(string[] args)
    {
        Program p = new Program();
        MyAction<long> act1 = F;
        act1(12);
        MyAction<long> act2 = new MyAction<long>(p.G);
        act2(13);
        MyAction<long> act3 = p.H;
        act3(14);
        MyAction<long> act4 = new MyAction<long>(act3);
        act4(15);
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
12
13
14
15
");
        }

        [Fact]
        public void DelegateMethodDelegates()
        {
            string source = @"
using System;

class C
{
    Action invoke;
    Func<AsyncCallback, object, IAsyncResult> beginInvoke;
    Action<IAsyncResult> endInvoke;
    Func<object[], object> dynamicInvoke;
    Func<object> clone;
    Func<object, bool> equals;

    void M()
    {   
        var a = new Action(M);
        invoke = new Action(a.Invoke);
        beginInvoke = new Func<AsyncCallback, object, IAsyncResult>(a.BeginInvoke);
        endInvoke = new Action<IAsyncResult>(a.EndInvoke);
        dynamicInvoke = new Func<object[], object>(a.DynamicInvoke);
        clone = new Func<object>(a.Clone);
        equals = new Func<object, bool>(a.Equals);
    }
}
";
            CompileAndVerify(source).VerifyIL("C.M", @"
{
  // Code size      124 (0x7c)
  .maxstack  3
  .locals init (System.Action V_0) //a
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""void C.M()""
  IL_0007:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldloc.0
  IL_000f:  ldftn      ""void System.Action.Invoke()""
  IL_0015:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_001a:  stfld      ""System.Action C.invoke""
  IL_001f:  ldarg.0
  IL_0020:  ldloc.0
  IL_0021:  ldftn      ""System.IAsyncResult System.Action.BeginInvoke(System.AsyncCallback, object)""
  IL_0027:  newobj     ""System.Func<System.AsyncCallback, object, System.IAsyncResult>..ctor(object, System.IntPtr)""
  IL_002c:  stfld      ""System.Func<System.AsyncCallback, object, System.IAsyncResult> C.beginInvoke""
  IL_0031:  ldarg.0
  IL_0032:  ldloc.0
  IL_0033:  ldftn      ""void System.Action.EndInvoke(System.IAsyncResult)""
  IL_0039:  newobj     ""System.Action<System.IAsyncResult>..ctor(object, System.IntPtr)""
  IL_003e:  stfld      ""System.Action<System.IAsyncResult> C.endInvoke""
  IL_0043:  ldarg.0
  IL_0044:  ldloc.0
  IL_0045:  ldftn      ""object System.Delegate.DynamicInvoke(params object[])""
  IL_004b:  newobj     ""System.Func<object[], object>..ctor(object, System.IntPtr)""
  IL_0050:  stfld      ""System.Func<object[], object> C.dynamicInvoke""
  IL_0055:  ldarg.0
  IL_0056:  ldloc.0
  IL_0057:  dup
  IL_0058:  ldvirtftn  ""object System.Delegate.Clone()""
  IL_005e:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_0063:  stfld      ""System.Func<object> C.clone""
  IL_0068:  ldarg.0
  IL_0069:  ldloc.0
  IL_006a:  dup
  IL_006b:  ldvirtftn  ""bool object.Equals(object)""
  IL_0071:  newobj     ""System.Func<object, bool>..ctor(object, System.IntPtr)""
  IL_0076:  stfld      ""System.Func<object, bool> C.equals""
  IL_007b:  ret
}");
        }

        [Fact]
        public void ConstantLiteralToDecimal()
        {
            string source = @"
using System;

public class Program
{
    public static void Main()
    {
        Print(0); // ldc.i4.0 - decimal..ctor(int32)
        Print(1); // ldc.i4.1 - decimal..ctor(int32)
        Print(8); // ldc.i4.8 - decimal..ctor(int32)
        Print(-1);  // ldc.i4.m1 - decimal..ctor(int32)
        Print(-128);// ldc.i4.s - decimal..ctor(int32)
        Print(2147483647); // ldc.i4  - decimal..ctor(int32)
        Print(-2147483648); // ldc.i4  - decimal..ctor(int32)
        Print(4294967295); // ldc.i4.m1 - decimal..ctor(uint32) [Note: Dev11 uses decimal..ctor(int64)]
        Print(9223372036854775807); // ldc.i8 - decimal..ctor(int64)
        Print(-9223372036854775808); // ldc.i8 - decimal..ctor(int64)
        Print(18446744073709551615); // decimal..ctor(uint64) [Note: Dev11 uses decimal..ctor(int32, int32, int32, bool, byte)]
        Print(-79228162514264337593543950335m); // decimal..ctor(int32, int32, int32, bool, byte)
        Print((decimal)12345.679f); // ? ldc.r4 - decimal..ctor(Single)
    }

    public static void Print(decimal val)
    {
        Console.WriteLine(val);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
0
1
8
-1
-128
2147483647
-2147483648
4294967295
9223372036854775807
-9223372036854775808
18446744073709551615
-79228162514264337593543950335
12345.68
");

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size      179 (0xb3)
  .maxstack  5
  IL_0000:  ldsfld     ""decimal decimal.Zero""
  IL_0005:  call       ""void Program.Print(decimal)""
  IL_000a:  ldsfld     ""decimal decimal.One""
  IL_000f:  call       ""void Program.Print(decimal)""
  IL_0014:  ldc.i4.8
  IL_0015:  newobj     ""decimal..ctor(int)""
  IL_001a:  call       ""void Program.Print(decimal)""
  IL_001f:  ldsfld     ""decimal decimal.MinusOne""
  IL_0024:  call       ""void Program.Print(decimal)""
  IL_0029:  ldc.i4.s   -128
  IL_002b:  newobj     ""decimal..ctor(int)""
  IL_0030:  call       ""void Program.Print(decimal)""
  IL_0035:  ldc.i4     0x7fffffff
  IL_003a:  newobj     ""decimal..ctor(int)""
  IL_003f:  call       ""void Program.Print(decimal)""
  IL_0044:  ldc.i4     0x80000000
  IL_0049:  newobj     ""decimal..ctor(int)""
  IL_004e:  call       ""void Program.Print(decimal)""
  IL_0053:  ldc.i4.m1
  IL_0054:  newobj     ""decimal..ctor(uint)""
  IL_0059:  call       ""void Program.Print(decimal)""
  IL_005e:  ldc.i8     0x7fffffffffffffff
  IL_0067:  newobj     ""decimal..ctor(long)""
  IL_006c:  call       ""void Program.Print(decimal)""
  IL_0071:  ldc.i8     0x8000000000000000
  IL_007a:  newobj     ""decimal..ctor(long)""
  IL_007f:  call       ""void Program.Print(decimal)""
  IL_0084:  ldc.i4.m1
  IL_0085:  conv.i8
  IL_0086:  newobj     ""decimal..ctor(ulong)""
  IL_008b:  call       ""void Program.Print(decimal)""
  IL_0090:  ldc.i4.m1
  IL_0091:  ldc.i4.m1
  IL_0092:  ldc.i4.m1
  IL_0093:  ldc.i4.1
  IL_0094:  ldc.i4.0
  IL_0095:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_009a:  call       ""void Program.Print(decimal)""
  IL_009f:  ldc.i4     0x12d688
  IL_00a4:  ldc.i4.0
  IL_00a5:  ldc.i4.0
  IL_00a6:  ldc.i4.0
  IL_00a7:  ldc.i4.2
  IL_00a8:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_00ad:  call       ""void Program.Print(decimal)""
  IL_00b2:  ret
}
");
        }

        [WorkItem(542417, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542417")]
        [Fact]
        public void OptionalForConstructor()
        {
            string source = @"using System;
class NamedExample
{
    public NamedExample(int optional = 10)
    { }
    static void Main(string[] args)
    {
        var temp = new NamedExample();
    }
}
";
            CompileAndVerify(source);
        }

        [Fact]
        public void BranchReuse001()
        {
            string source = @"
using System;
class A
{
    public static void Main()
    {
        int x = int.Parse(""42"");
        if (x > 0)
        {
            string s = ""first"";
            Console.WriteLine(s);
        }
        else
        {
            string s = ""second"";
            Console.WriteLine(s);
        }

        Console.WriteLine(x);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"first
42");

            compilation.VerifyIL("A.Main",
@"{
  // Code size       42 (0x2a)
  .maxstack  3
  IL_0000:  ldstr      ""42""
  IL_0005:  call       ""int int.Parse(string)""
  IL_000a:  dup
  IL_000b:  ldc.i4.0
  IL_000c:  ble.s      IL_001a
  IL_000e:  ldstr      ""first""
  IL_0013:  call       ""void System.Console.WriteLine(string)""
  IL_0018:  br.s       IL_0024
  IL_001a:  ldstr      ""second""
  IL_001f:  call       ""void System.Console.WriteLine(string)""
  IL_0024:  call       ""void System.Console.WriteLine(int)""
  IL_0029:  ret
}
");
        }

        [Fact]
        public void IncrementUsed()
        {
            string source = @"
using System;
class A
{
    public static void Main()
    {
        int[] x = new int[3];
        x[0] = 1;
        x[1] = 2;
        x[2] = 3;

        int y = x[0]++;

        Console.WriteLine(y);
        Console.WriteLine(x[0]);
        Console.WriteLine(x[1]);
        Console.WriteLine(x[2]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"1
2
2
3");

            compilation.VerifyIL("A.Main",
@"{
  // Code size       62 (0x3e)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  stelem.i4
  IL_000a:  dup
  IL_000b:  ldc.i4.1
  IL_000c:  ldc.i4.2
  IL_000d:  stelem.i4
  IL_000e:  dup
  IL_000f:  ldc.i4.2
  IL_0010:  ldc.i4.3
  IL_0011:  stelem.i4
  IL_0012:  dup
  IL_0013:  ldc.i4.0
  IL_0014:  ldelema    ""int""
  IL_0019:  dup
  IL_001a:  ldind.i4
  IL_001b:  stloc.0
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.1
  IL_001e:  add
  IL_001f:  stind.i4
  IL_0020:  ldloc.0
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  dup
  IL_0027:  ldc.i4.0
  IL_0028:  ldelem.i4
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  dup
  IL_002f:  ldc.i4.1
  IL_0030:  ldelem.i4
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  ldc.i4.2
  IL_0037:  ldelem.i4
  IL_0038:  call       ""void System.Console.WriteLine(int)""
  IL_003d:  ret
}
");
        }

        [Fact]
        public void IncrementUnused()
        {
            string source = @"
using System;
class A
{
    public static void Main()
    {
        int[] x = new int[3];
        x[0] = 1;
        x[1] = 2;
        x[2] = 3;

        x[0]++;

        Console.WriteLine(x[0]);
        Console.WriteLine(x[1]);
        Console.WriteLine(x[2]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"2
2
3");

            compilation.VerifyIL("A.Main",
@"
{
  // Code size       54 (0x36)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  stelem.i4
  IL_000a:  dup
  IL_000b:  ldc.i4.1
  IL_000c:  ldc.i4.2
  IL_000d:  stelem.i4
  IL_000e:  dup
  IL_000f:  ldc.i4.2
  IL_0010:  ldc.i4.3
  IL_0011:  stelem.i4
  IL_0012:  dup
  IL_0013:  ldc.i4.0
  IL_0014:  ldelema    ""int""
  IL_0019:  dup
  IL_001a:  ldind.i4
  IL_001b:  ldc.i4.1
  IL_001c:  add
  IL_001d:  stind.i4
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldelem.i4
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  dup
  IL_0027:  ldc.i4.1
  IL_0028:  ldelem.i4
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  ldc.i4.2
  IL_002f:  ldelem.i4
  IL_0030:  call       ""void System.Console.WriteLine(int)""
  IL_0035:  ret
}
");
        }

        [Fact]
        public void IncrementUnused1()
        {
            string source = @"
using System;
class A
{
    private static int[] x = new int[3] {1, 2, 3};

    public static void Main()
    {
        Increment();

        Console.WriteLine(x[0]);
        Console.WriteLine(x[1]);
        Console.WriteLine(x[2]);
    }

    private static void Increment()
    {
        x[0]++;
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"2
2
3");

            compilation.VerifyIL("A.Increment",
@"
{
  // Code size       17 (0x11)
  .maxstack  3
  IL_0000:  ldsfld     ""int[] A.x""
  IL_0005:  ldc.i4.0
  IL_0006:  ldelema    ""int""
  IL_000b:  dup
  IL_000c:  ldind.i4
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  stind.i4
  IL_0010:  ret
}
");
        }

        [Fact]
        public void IncrementUnused2()
        {
            string source = @"
using System;
class A
{
    private static int[] x = new int[3] {1, 2, 3};

    public static void Main()
    {
        Increment();

        Console.WriteLine(x[0]);
        Console.WriteLine(x[1]);
        Console.WriteLine(x[2]);
    }

    private static void Increment()
    {
        ++x[0];
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"2
2
3");

            compilation.VerifyIL("A.Increment",
@"
{
  // Code size       17 (0x11)
  .maxstack  3
  IL_0000:  ldsfld     ""int[] A.x""
  IL_0005:  ldc.i4.0
  IL_0006:  ldelema    ""int""
  IL_000b:  dup
  IL_000c:  ldind.i4
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  stind.i4
  IL_0010:  ret
}
");
        }

        [Fact]
        public void IncrementNested()
        {
            string source = @"
using System;
class A
{
    static int[] x = new int[] {1,2,3};

    public static void Main()
    {
        x[x[x[0]++]++]++;

        Console.WriteLine(x[0]);
        Console.WriteLine(x[1]);
        Console.WriteLine(x[2]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"2
3
4");

            compilation.VerifyIL("A.Main",
@"
{
  // Code size       89 (0x59)
  .maxstack  5
  .locals init (int V_0,
  int V_1)
  IL_0000:  ldsfld     ""int[] A.x""
  IL_0005:  ldsfld     ""int[] A.x""
  IL_000a:  ldsfld     ""int[] A.x""
  IL_000f:  ldc.i4.0
  IL_0010:  ldelema    ""int""
  IL_0015:  dup
  IL_0016:  ldind.i4
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ldc.i4.1
  IL_001a:  add
  IL_001b:  stind.i4
  IL_001c:  ldloc.1
  IL_001d:  ldelema    ""int""
  IL_0022:  dup
  IL_0023:  ldind.i4
  IL_0024:  stloc.0
  IL_0025:  ldloc.0
  IL_0026:  ldc.i4.1
  IL_0027:  add
  IL_0028:  stind.i4
  IL_0029:  ldloc.0
  IL_002a:  ldelema    ""int""
  IL_002f:  dup
  IL_0030:  ldind.i4
  IL_0031:  ldc.i4.1
  IL_0032:  add
  IL_0033:  stind.i4
  IL_0034:  ldsfld     ""int[] A.x""
  IL_0039:  ldc.i4.0
  IL_003a:  ldelem.i4
  IL_003b:  call       ""void System.Console.WriteLine(int)""
  IL_0040:  ldsfld     ""int[] A.x""
  IL_0045:  ldc.i4.1
  IL_0046:  ldelem.i4
  IL_0047:  call       ""void System.Console.WriteLine(int)""
  IL_004c:  ldsfld     ""int[] A.x""
  IL_0051:  ldc.i4.2
  IL_0052:  ldelem.i4
  IL_0053:  call       ""void System.Console.WriteLine(int)""
  IL_0058:  ret
}
");
        }

        [Fact]
        public void PreIncrementUsed()
        {
            string source = @"
using System;
class A
{
    public static void Main()
    {
        int[] x = new int[3];
        x[0] = 1;
        x[1] = 2;
        x[2] = 3;

        int y = ++x[0];

        Console.WriteLine(y);
        Console.WriteLine(x[0]);
        Console.WriteLine(x[1]);
        Console.WriteLine(x[2]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"2
2
2
3");

            compilation.VerifyIL("A.Main",
@"{
  // Code size       62 (0x3e)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  stelem.i4
  IL_000a:  dup
  IL_000b:  ldc.i4.1
  IL_000c:  ldc.i4.2
  IL_000d:  stelem.i4
  IL_000e:  dup
  IL_000f:  ldc.i4.2
  IL_0010:  ldc.i4.3
  IL_0011:  stelem.i4
  IL_0012:  dup
  IL_0013:  ldc.i4.0
  IL_0014:  ldelema    ""int""
  IL_0019:  dup
  IL_001a:  ldind.i4
  IL_001b:  ldc.i4.1
  IL_001c:  add
  IL_001d:  stloc.0
  IL_001e:  ldloc.0
  IL_001f:  stind.i4
  IL_0020:  ldloc.0
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  dup
  IL_0027:  ldc.i4.0
  IL_0028:  ldelem.i4
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  dup
  IL_002f:  ldc.i4.1
  IL_0030:  ldelem.i4
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  ldc.i4.2
  IL_0037:  ldelem.i4
  IL_0038:  call       ""void System.Console.WriteLine(int)""
  IL_003d:  ret
}
");
        }

        [Fact]
        public void PreIncrementUnused()
        {
            string source = @"
using System;
class A
{
    public static void Main()
    {
        int[] x = new int[3];
        x[0] = 1;
        x[1] = 2;
        x[2] = 3;

        ++x[0];

        Console.WriteLine(x[0]);
        Console.WriteLine(x[1]);
        Console.WriteLine(x[2]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"2
2
3");

            compilation.VerifyIL("A.Main",
@"
{
  // Code size       54 (0x36)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  stelem.i4
  IL_000a:  dup
  IL_000b:  ldc.i4.1
  IL_000c:  ldc.i4.2
  IL_000d:  stelem.i4
  IL_000e:  dup
  IL_000f:  ldc.i4.2
  IL_0010:  ldc.i4.3
  IL_0011:  stelem.i4
  IL_0012:  dup
  IL_0013:  ldc.i4.0
  IL_0014:  ldelema    ""int""
  IL_0019:  dup
  IL_001a:  ldind.i4
  IL_001b:  ldc.i4.1
  IL_001c:  add
  IL_001d:  stind.i4
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldelem.i4
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  dup
  IL_0027:  ldc.i4.1
  IL_0028:  ldelem.i4
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  ldc.i4.2
  IL_002f:  ldelem.i4
  IL_0030:  call       ""void System.Console.WriteLine(int)""
  IL_0035:  ret
}
");
        }

        [Fact]
        public void PreIncrementUnusedA()
        {
            string source = @"
using System;
class A
{
    public static void Main()
    {
        int[] x = new int[3];
        x[0] = 1;
        x[1] = 2;
        x[2] = 3;

        ++x[0];

        Console.WriteLine(x[0]);
        Console.WriteLine(x[1]);
        Console.WriteLine(x[2]);
    }
}
";
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseDebugExe, expectedOutput: @"2
2
3");

            compilation.VerifyIL("A.Main",
@"
{
  // Code size       56 (0x38)
  .maxstack  3
  .locals init (int[] V_0) //x
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.2
  IL_0011:  ldc.i4.3
  IL_0012:  stelem.i4
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.0
  IL_0015:  ldelema    ""int""
  IL_001a:  dup
  IL_001b:  ldind.i4
  IL_001c:  ldc.i4.1
  IL_001d:  add
  IL_001e:  stind.i4
  IL_001f:  ldloc.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldelem.i4
  IL_0022:  call       ""void System.Console.WriteLine(int)""
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4.1
  IL_0029:  ldelem.i4
  IL_002a:  call       ""void System.Console.WriteLine(int)""
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4.2
  IL_0031:  ldelem.i4
  IL_0032:  call       ""void System.Console.WriteLine(int)""
  IL_0037:  ret
}
");
        }


        [Fact]
        public void PostIncrementUnusedStruct()
        {
            string source = @"
using System;
class A
{
    static S1 x = new S1();

    public static void Main()
    {
        x.y++;

        Console.WriteLine(x.y);
    }
}

struct S1
{
    public int y;
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            compilation.VerifyIL("A.Main",
@"
{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  ldsflda    ""S1 A.x""
  IL_0005:  ldflda     ""int S1.y""
  IL_000a:  dup
  IL_000b:  ldind.i4
  IL_000c:  ldc.i4.1
  IL_000d:  add
  IL_000e:  stind.i4
  IL_000f:  ldsflda    ""S1 A.x""
  IL_0014:  ldfld      ""int S1.y""
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ret
}
");
        }

        [Fact]
        public void PostIncrementUnusedStruct1()
        {
            string source = @"
using System;
class A
{
    public S1 x = new S1();

    public static void Main()
    {
        var v = new A();
        v.x.y+=42;

        Console.WriteLine(v.x.y);
    }
}

struct S1
{
    public int y;
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"42");

            compilation.VerifyIL("A.Main",
@"
{
  // Code size       38 (0x26)
  .maxstack  4
  IL_0000:  newobj     ""A..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""S1 A.x""
  IL_000b:  ldflda     ""int S1.y""
  IL_0010:  dup
  IL_0011:  ldind.i4
  IL_0012:  ldc.i4.s   42
  IL_0014:  add
  IL_0015:  stind.i4
  IL_0016:  ldflda     ""S1 A.x""
  IL_001b:  ldfld      ""int S1.y""
  IL_0020:  call       ""void System.Console.WriteLine(int)""
  IL_0025:  ret
}
");
        }

        [Fact]
        public void PostIncrementUnusedStruct1a()
        {
            string source = @"
using System;
class A
{
    public S1 x = new S1();

    public static void Main()
    {
        var v = new A();
        v.x.y+=42;

        Console.WriteLine(v.x.y);
    }
}

struct S1
{
    public int y;
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"42", options: TestOptions.ReleaseDebugExe);

            compilation.VerifyIL("A.Main",
@"
{
  // Code size       40 (0x28)
  .maxstack  3
  .locals init (A V_0) //v
  IL_0000:  newobj     ""A..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldflda     ""S1 A.x""
  IL_000c:  ldflda     ""int S1.y""
  IL_0011:  dup
  IL_0012:  ldind.i4
  IL_0013:  ldc.i4.s   42
  IL_0015:  add
  IL_0016:  stind.i4
  IL_0017:  ldloc.0
  IL_0018:  ldflda     ""S1 A.x""
  IL_001d:  ldfld      ""int S1.y""
  IL_0022:  call       ""void System.Console.WriteLine(int)""
  IL_0027:  ret
}
");
        }

        [Fact]
        public void PostIncrementUnusedStruct2()
        {
            string source = @"
using System;
class A
{
    public static S1 sv = default(S1);

    public static void Main()
    {
        var v = sv;
        v.y += 42;

        Console.WriteLine(v.y);
    }
}

struct S1
{
    public int y;
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"42");

            compilation.VerifyIL("A.Main",
@"
{
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (S1 V_0) //v
  IL_0000:  ldsfld     ""S1 A.sv""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldflda     ""int S1.y""
  IL_000d:  dup
  IL_000e:  ldind.i4
  IL_000f:  ldc.i4.s   42
  IL_0011:  add
  IL_0012:  stind.i4
  IL_0013:  ldloc.0
  IL_0014:  ldfld      ""int S1.y""
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ret
}
");
        }


        [Fact, WorkItem(543618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543618")]
        public void ImplicitConversionCharToDecimal()
        {
            var source = @"
public class Test
{
    static void Main() 
    {
        char source = '\x1';
        decimal	dest = source;
    } 
}";
            CompileAndVerify(source).
VerifyIL("Test.Main", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""decimal decimal.op_Implicit(char)""
  IL_0006:  pop
  IL_0007:  ret
}");
        }

        [Fact, WorkItem(543618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543618")]
        public void ConversionDecimalToChar()
        {
            var source = @"
public class Test
{
    static void Main() 
    {
        decimal source = 1M;
        char dest = (char)source;
    } 
}";
            CompileAndVerify(source).
VerifyIL("Test.Main", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldsfld     ""decimal decimal.One""
  IL_0005:  call       ""char decimal.op_Explicit(decimal)""
  IL_000a:  pop
  IL_000b:  ret
}");
        }

        [WorkItem(543621, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543621")]
        [Fact]
        public void PartialMethodInvocationInIfStatement()
        {
            var source = @"
    public static partial class Contract
    {
        public static void Assert(bool condition)
        {
            if (true)
                ReportFailure();
        }

        static partial void ReportFailure();
    }";
            CompileAndVerify(source);
        }

        [Fact, WorkItem(529173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529173")]
        public void CallWithStruct()
        {
            var source = @"
using System;

    class Program
    {
        static void Main(string[] args)
        {
            M1(new S1());

            M1(new S1(42));

            var x = new S1(42);
            M1(x);

            var y = new S1(42);
            M1(y);

            y.Bar();
        }

        static void M1(S1 arg)
        {
        }
    }

    struct S1
    {
        public S1(int x)
        {
            this.x = x;
        }

        public int x;

        public void Bar(){}
    }
";
            CompileAndVerify(source, expectedOutput: "").
VerifyIL("Program.Main", @"
{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init (S1 V_0, //y
  S1 V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""S1""
  IL_0008:  ldloc.1
  IL_0009:  call       ""void Program.M1(S1)""
  IL_000e:  ldc.i4.s   42
  IL_0010:  newobj     ""S1..ctor(int)""
  IL_0015:  call       ""void Program.M1(S1)""
  IL_001a:  ldc.i4.s   42
  IL_001c:  newobj     ""S1..ctor(int)""
  IL_0021:  call       ""void Program.M1(S1)""
  IL_0026:  ldloca.s   V_0
  IL_0028:  ldc.i4.s   42
  IL_002a:  call       ""S1..ctor(int)""
  IL_002f:  ldloc.0
  IL_0030:  call       ""void Program.M1(S1)""
  IL_0035:  ldloca.s   V_0
  IL_0037:  call       ""void S1.Bar()""
  IL_003c:  ret
}");
        }

        [Fact, WorkItem(543611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543611")]
        public void CallOnConst()
        {
            var source = @"
using System;

class Test
{
    static void Main()
    {
        Console.Write(decimal.One.CompareTo(decimal.Zero));
    }
}
";
            CompileAndVerify(source, expectedOutput: "1").
VerifyIL("Test.Main", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (decimal V_0)
  IL_0000:  ldsfld     ""decimal decimal.One""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldsfld     ""decimal decimal.Zero""
  IL_000d:  call       ""int decimal.CompareTo(decimal)""
  IL_0012:  call       ""void System.Console.Write(int)""
  IL_0017:  ret
}");
        }

        [Fact, WorkItem(543611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543611")]
        public void CallOnReadonlyValField()
        {
            var source = @"
using System;

class Test
{
    struct C1
    {
        public decimal x;
    }

    static C1 Goo()
    {
        return new C1();
    }

    static void Main()
    {
        Console.Write(Goo().x.CompareTo(decimal.One));
    }
}
";
            CompileAndVerify(source, expectedOutput: "-1").
VerifyIL("Test.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (Test.C1 V_0)
  IL_0000:  call       ""Test.C1 Test.Goo()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldflda     ""decimal Test.C1.x""
  IL_000d:  ldsfld     ""decimal decimal.One""
  IL_0012:  call       ""int decimal.CompareTo(decimal)""
  IL_0017:  call       ""void System.Console.Write(int)""
  IL_001c:  ret
}");
        }

        [Fact(), WorkItem(543691, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543691")]
        public void NullableAsArgsForTypeParameter()
        {
            CreateCompilationWithMscorlib40AndSystemCore(@"
using System;
class C
{
    public delegate R RefFunc<A, R>(ref A a);
    public static void Main()
    {
        int failcount = 0;
        failcount += Equal(M((char?)null, (ref int x) => ""hi""), typeof(int), typeof(string)); //nullable lifted
    }
    public static Type[] M<T, U>(T? t, RefFunc<T, U> f) where T : struct
    {
        return new Type[] { typeof(T), typeof(U) };
    }
    public static int Equal(Type[] actual, params Type[] expected)
    {
        return 1;
    }
}
").VerifyDiagnostics();
        }

        [Fact(), WorkItem(543693, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543693")]
        public void Anonymous()
        {
            var source = @"
using System;
using System.Collections.Generic;
delegate T Func<A0, T>(A0 a0);
class Y<U>
{
    public U u;
    public Y(U u)
    {
        this.u = u;
    }
    public Y<T> Select<T>(Func<U, T> selector)
    {
        return new Y<T>(selector(u));
    }
}
class P
{
    static void Main()
    {
        var src = new Y<int>(2);
        var q = from x in src
                let y = x + 3
                select new { X = x, Y = y };

        if ((q.u.X != 2 || q.u.Y != 5))
        {
        }
    }
}
";
            CompileAndVerify(source);
        }

        #region Regression

        [WorkItem(538224, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538224")]
        [Fact]
        public void CallMethodFromInterface01()
        {
            string source = @"
namespace NS
{
    public interface IGoo
    {
        void M();
    }

    public struct Goo : IGoo
    {
        public void M() 
        {
            System.Console.Write(""M() Called!"");
        }
    }

    class Test
    {
        static void Main()
        {
            Goo goo = new Goo();
            goo.M();
            IGoo igoo = goo;
            igoo.M();
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"M() Called!M() Called!");

            compilation.VerifyIL("NS.Test.Main",
@"
{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (NS.Goo V_0) //goo
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""NS.Goo""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""void NS.Goo.M()""
  IL_000f:  ldloc.0
  IL_0010:  box        ""NS.Goo""
  IL_0015:  callvirt   ""void NS.IGoo.M()""
  IL_001a:  ret
}");
        }

        [WorkItem(538224, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538224")]
        [Fact]
        public void CallMethodFromGenInterface01()
        {
            string source = @"
namespace NS
{
    public interface IGoo<T>
    {
        void M(T t);
    }

    public class Goo : IGoo<int>
    {
        public void M(int n) 
        {
            System.Console.WriteLine(n);
        }
    }

    class Test
    {
        static void Main()
        {
            IGoo<int> goo = new Goo();
            goo.M(123);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"123");

            compilation.VerifyIL("NS.Test.Main",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  newobj     ""NS.Goo..ctor()""
  IL_0005:  ldc.i4.s   123
  IL_0007:  callvirt   ""void NS.IGoo<int>.M(int)""
  IL_000c:  ret
}
");
        }

        [WorkItem(538226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538226")]
        [Fact]
        public void JumpWithFloatOperand()
        {
            string source = @"using System;
namespace CodeGen
{
    public class MyData
    {
        public static void Main()
        {
            bool[] a1 = new bool[2];
            a1[0] = false;
            a1[1] = true;
            byte[] a2 = new byte[3];
            a2[0] = a2[1] = a2[2] = 11;
            float[] a3 = new float[1];
            a3[0] = 0.1249f;
            double[] a4 = new double[2];
            a4[0] = a4[1] = a3[0];

            MyData obj = new MyData();
            ulong ret = obj.M(a1, a2, a3, a4);
            Console.Write(ret);
        }

        public ulong M(bool[] a1, byte[] a2, float[] a3, double[] a4)
        {
            if (a1[0])
            {
                if (99 <= a2[0])
                {
                    a2[1] = (byte)((a2[0] + (a2[1] + a2[2])) % byte.MaxValue);
                    return (ulong) (a2[1] + a2[2]);
                }
            }
            else if (a1[1])
            {
                if (a3[0] >= 0)
                {
                    a3[0] = a3[0] * 100;
                    return (ulong) a3[0];
                }
            }
            else
            {
                if (a4[0] <= 1)
                {
                    a4[1] = a4[0] + +100;
                    return 123ul;
                }
            }

            return 0UL;
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"12");

            compilation.VerifyIL("CodeGen.MyData.M",
@"
{
  // Code size      114 (0x72)
  .maxstack  6
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.u1
  IL_0003:  brfalse.s  IL_002a
  IL_0005:  ldc.i4.s   99
  IL_0007:  ldarg.2
  IL_0008:  ldc.i4.0
  IL_0009:  ldelem.u1
  IL_000a:  bgt.s      IL_006f
  IL_000c:  ldarg.2
  IL_000d:  ldc.i4.1
  IL_000e:  ldarg.2
  IL_000f:  ldc.i4.0
  IL_0010:  ldelem.u1
  IL_0011:  ldarg.2
  IL_0012:  ldc.i4.1
  IL_0013:  ldelem.u1
  IL_0014:  ldarg.2
  IL_0015:  ldc.i4.2
  IL_0016:  ldelem.u1
  IL_0017:  add
  IL_0018:  add
  IL_0019:  ldc.i4     0xff
  IL_001e:  rem
  IL_001f:  conv.u1
  IL_0020:  stelem.i1
  IL_0021:  ldarg.2
  IL_0022:  ldc.i4.1
  IL_0023:  ldelem.u1
  IL_0024:  ldarg.2
  IL_0025:  ldc.i4.2
  IL_0026:  ldelem.u1
  IL_0027:  add
  IL_0028:  conv.i8
  IL_0029:  ret
  IL_002a:  ldarg.1
  IL_002b:  ldc.i4.1
  IL_002c:  ldelem.u1
  IL_002d:  brfalse.s  IL_004a
  IL_002f:  ldarg.3
  IL_0030:  ldc.i4.0
  IL_0031:  ldelem.r4
  IL_0032:  ldc.r4     0
  IL_0037:  blt.un.s   IL_006f
  IL_0039:  ldarg.3
  IL_003a:  ldc.i4.0
  IL_003b:  ldarg.3
  IL_003c:  ldc.i4.0
  IL_003d:  ldelem.r4
  IL_003e:  ldc.r4     100
  IL_0043:  mul
  IL_0044:  stelem.r4
  IL_0045:  ldarg.3
  IL_0046:  ldc.i4.0
  IL_0047:  ldelem.r4
  IL_0048:  conv.u8
  IL_0049:  ret
  IL_004a:  ldarg.s    V_4
  IL_004c:  ldc.i4.0
  IL_004d:  ldelem.r8
  IL_004e:  ldc.r8     1
  IL_0057:  bgt.un.s   IL_006f
  IL_0059:  ldarg.s    V_4
  IL_005b:  ldc.i4.1
  IL_005c:  ldarg.s    V_4
  IL_005e:  ldc.i4.0
  IL_005f:  ldelem.r8
  IL_0060:  ldc.r8     100
  IL_0069:  add
  IL_006a:  stelem.r8
  IL_006b:  ldc.i4.s   123
  IL_006d:  conv.i8
  IL_006e:  ret
  IL_006f:  ldc.i4.0
  IL_0070:  conv.i8
  IL_0071:  ret
}
");
        }

        [WorkItem(538245, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538245")]
        [Fact]
        public void CallParamsCtor()
        {
            string source = @"
class MyClass
{
    int intTest;
    public MyClass(params int[] values)
    {
        intTest = values[0] + values[1] + values[2];
    }

    public static int Main()
    {
        int intI = 1;
        int intJ = 2;
        int intK = 3;

        MyClass mc = new MyClass(intI, intJ, intK);
        System.Console.Write(mc.intTest);
        if (mc.intTest == 6)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"6");

            // Dev10
            compilation.VerifyIL("MyClass.Main",
@"{
  // Code size       52 (0x34)
  .maxstack  4
  .locals init (int V_0, //intI
  int V_1, //intJ
  int V_2) //intK
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.2
  IL_0003:  stloc.1
  IL_0004:  ldc.i4.3
  IL_0005:  stloc.2
  IL_0006:  ldc.i4.3
  IL_0007:  newarr     ""int""
  IL_000c:  dup
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  stelem.i4
  IL_0010:  dup
  IL_0011:  ldc.i4.1
  IL_0012:  ldloc.1
  IL_0013:  stelem.i4
  IL_0014:  dup
  IL_0015:  ldc.i4.2
  IL_0016:  ldloc.2
  IL_0017:  stelem.i4
  IL_0018:  newobj     ""MyClass..ctor(params int[])""
  IL_001d:  dup
  IL_001e:  ldfld      ""int MyClass.intTest""
  IL_0023:  call       ""void System.Console.Write(int)""
  IL_0028:  ldfld      ""int MyClass.intTest""
  IL_002d:  ldc.i4.6
  IL_002e:  bne.un.s   IL_0032
  IL_0030:  ldc.i4.0
  IL_0031:  ret
  IL_0032:  ldc.i4.1
  IL_0033:  ret
}
");
        }

        [Fact]
        [WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")]
        [WorkItem(538246, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538246"), WorkItem(543655, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543655")]
        public void FloatDoubleInfinity()
        {
            string source = @"
using System;

public class MyClass
{
    public static int Main()
    {
        double d1 = double.MaxValue;
        double d2 = double.PositiveInfinity;
        double d3 = double.NegativeInfinity;
        int ret = 0;
        if ((double)(d1 + (d1 * 1.0e-15f)) != d2)
        {
            ret = 1;
        }
        if ((double)(d1 - (-d1 * 1.0e-15f)) != d2)
        {
            ret = ret + 10;
        }
        if ((double)(d1 * (1.0f + 1.0e-15f)) != d1) // note (1.0f + 1.0e-15f) == 1.0f
        {
            ret = ret + 100;
        }
        if ((double)(d1 / (1.0f - 1.0e-15f)) != d1) // note (1.0f - 1.0e-15f) == 1.0f
        {
            ret = ret + 1000;
        }
        if ((double)(-d1 + (-(d1 * 1.0e-15f))) != d3)
        {
            ret = ret + 10000;
        }
        if ((double)((-d1 - (d1 * 1.0e-15f))) != d3)
        {
            ret = ret + 100000;
        }
        if ((double)(-d1 * (1.0f + 1.0e-15f)) != -d1) // note (1.0f + 1.0e-15f) == 1.0f
        {
            ret = ret + 1000000;
        }
        if ((double)(-d1 / (1.0f - 1.0e-15f)) != -d1) // note (1.0f - 1.0e-15f) == 1.0f
        {
            ret = ret + 10000000;
        }
        Console.WriteLine(ret);
        return ret;
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "0").
VerifyIL("MyClass.Main", @"
{
  // Code size      228 (0xe4)
  .maxstack  3
  .locals init (double V_0, //d1
                double V_1, //d2
                double V_2, //d3
                int V_3) //ret
  IL_0000:  ldc.r8     1.79769313486232E+308
  IL_0009:  stloc.0
  IL_000a:  ldc.r8     Infinity
  IL_0013:  stloc.1
  IL_0014:  ldc.r8     -Infinity
  IL_001d:  stloc.2
  IL_001e:  ldc.i4.0
  IL_001f:  stloc.3
  IL_0020:  ldloc.0
  IL_0021:  ldloc.0
  IL_0022:  ldc.r8     1.00000000362749E-15
  IL_002b:  mul
  IL_002c:  add
  IL_002d:  conv.r8
  IL_002e:  ldloc.1
  IL_002f:  beq.s      IL_0033
  IL_0031:  ldc.i4.1
  IL_0032:  stloc.3
  IL_0033:  ldloc.0
  IL_0034:  ldloc.0
  IL_0035:  neg
  IL_0036:  ldc.r8     1.00000000362749E-15
  IL_003f:  mul
  IL_0040:  sub
  IL_0041:  conv.r8
  IL_0042:  ldloc.1
  IL_0043:  beq.s      IL_004a
  IL_0045:  ldloc.3
  IL_0046:  ldc.i4.s   10
  IL_0048:  add
  IL_0049:  stloc.3
  IL_004a:  ldloc.0
  IL_004b:  ldc.r8     1
  IL_0054:  mul
  IL_0055:  conv.r8
  IL_0056:  ldloc.0
  IL_0057:  beq.s      IL_005e
  IL_0059:  ldloc.3
  IL_005a:  ldc.i4.s   100
  IL_005c:  add
  IL_005d:  stloc.3
  IL_005e:  ldloc.0
  IL_005f:  ldc.r8     1
  IL_0068:  div
  IL_0069:  conv.r8
  IL_006a:  ldloc.0
  IL_006b:  beq.s      IL_0075
  IL_006d:  ldloc.3
  IL_006e:  ldc.i4     0x3e8
  IL_0073:  add
  IL_0074:  stloc.3
  IL_0075:  ldloc.0
  IL_0076:  neg
  IL_0077:  ldloc.0
  IL_0078:  ldc.r8     1.00000000362749E-15
  IL_0081:  mul
  IL_0082:  neg
  IL_0083:  add
  IL_0084:  conv.r8
  IL_0085:  ldloc.2
  IL_0086:  beq.s      IL_0090
  IL_0088:  ldloc.3
  IL_0089:  ldc.i4     0x2710
  IL_008e:  add
  IL_008f:  stloc.3
  IL_0090:  ldloc.0
  IL_0091:  neg
  IL_0092:  ldloc.0
  IL_0093:  ldc.r8     1.00000000362749E-15
  IL_009c:  mul
  IL_009d:  sub
  IL_009e:  conv.r8
  IL_009f:  ldloc.2
  IL_00a0:  beq.s      IL_00aa
  IL_00a2:  ldloc.3
  IL_00a3:  ldc.i4     0x186a0
  IL_00a8:  add
  IL_00a9:  stloc.3
  IL_00aa:  ldloc.0
  IL_00ab:  neg
  IL_00ac:  ldc.r8     1
  IL_00b5:  mul
  IL_00b6:  conv.r8
  IL_00b7:  ldloc.0
  IL_00b8:  neg
  IL_00b9:  beq.s      IL_00c3
  IL_00bb:  ldloc.3
  IL_00bc:  ldc.i4     0xf4240
  IL_00c1:  add
  IL_00c2:  stloc.3
  IL_00c3:  ldloc.0
  IL_00c4:  neg
  IL_00c5:  ldc.r8     1
  IL_00ce:  div
  IL_00cf:  conv.r8
  IL_00d0:  ldloc.0
  IL_00d1:  neg
  IL_00d2:  beq.s      IL_00dc
  IL_00d4:  ldloc.3
  IL_00d5:  ldc.i4     0x989680
  IL_00da:  add
  IL_00db:  stloc.3
  IL_00dc:  ldloc.3
  IL_00dd:  call       ""void System.Console.WriteLine(int)""
  IL_00e2:  ldloc.3
  IL_00e3:  ret
}");
        }

        [WorkItem(538839, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538839")]
        [Fact]
        public void LocalNumericConstInitWithDifferentType()
        {
            string source = @"
using System;
class Program
{
    static void Main()
    {
            const long const1 = 1;
            Console.WriteLine(const1);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            // Dev10
            compilation.VerifyIL("Program.Main",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.1  
  IL_0001:  conv.i8   
  IL_0002:  call       ""void System.Console.WriteLine(long)""
  IL_0007:  ret       
}");
        }

        [WorkItem(539425, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539425")]
        [Fact]
        public void ConstantLiftedEquality()
        {
            string source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        const bool b = 1 == null;
        Console.Write(b);
    }
}";

            //see Binder.TryFoldingNullableEquality for info on how/why this works
            var comp = CompileAndVerify(source, expectedOutput: @"False");

            comp.VerifyIL("Program.Main",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.0  
  IL_0001:  call       ""void System.Console.Write(bool)""
  IL_0006:  ret       
}");
        }

        [Fact]
        public void VariantInterfaceTypeParameters()
        {
            string source = @"
interface I<in TIn, out TOut> { }

class A { }
class B : A { }

class Program
{
    static void Main(string[] args)
    {
        I<A, A> aa1 = null;
        I<A, B> ab1 = null;
        I<B, A> ba1 = null;
        I<B, B> bb1 = null;

        I<A, A> aa2 = null;
        I<A, B> ab2 = null;
        I<B, A> ba2 = null;
        I<B, B> bb2 = null;

        aa1 = aa2;
        aa1 = ab2;
//        aa1 = ba2; //invalid
//        aa1 = bb2; //invalid

//        ab1 = aa2; //invalid
        ab1 = ab2;
//        ab1 = ba2; //invalid
//        ab1 = bb2; //invalid

        ba1 = aa2;
        ba1 = ab2;
        ba1 = ba2;
        ba1 = bb2;

//        bb1 = aa2; //invalid
        bb1 = ab2;
//        bb1 = ba2; //invalid
        bb1 = bb2;
    }
}";

            CompileAndVerify(source, expectedOutput: string.Empty);
        }

        [Fact]
        public void VariantDelegateTypeParameters()
        {
            string source = @"
delegate TOut D<in TIn, out TOut>(TIn p);

class A { }
class B : A { }

class Program
{
    static void Main(string[] args)
    {
        D<A, A> aa1 = null;
        D<A, B> ab1 = null;
        D<B, A> ba1 = null;
        D<B, B> bb1 = null;

        D<A, A> aa2 = null;
        D<A, B> ab2 = null;
        D<B, A> ba2 = null;
        D<B, B> bb2 = null;

        aa1 = aa2;
        aa1 = ab2;
//        aa1 = ba2; //invalid
//        aa1 = bb2; //invalid

//        ab1 = aa2; //invalid
        ab1 = ab2;
//        ab1 = ba2; //invalid
//        ab1 = bb2; //invalid

        ba1 = aa2;
        ba1 = ab2;
        ba1 = ba2;
        ba1 = bb2;

//        bb1 = aa2; //invalid
        bb1 = ab2;
//        bb1 = ba2; //invalid
        bb1 = bb2;
    }
}";

            CompileAndVerify(source, expectedOutput: string.Empty);
        }

        [WorkItem(540093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540093")]
        [Fact]
        public void UsingReferenceTypeConstField()
        {
            string source = @"
using System;
class MyClass { }

class MyMainClass
{
    public const MyClass mc = null;

    public static int Main()
    {
        int retval = 1;
        if (null == mc)
            retval = 0;
        Console.WriteLine(retval);
        return retval;
    }
}
";

            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(528060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528060")]
        [WorkItem(1043494, "DevDiv")]
        [Fact]
        public void DoubleDivByNegativeZero()
        {
            string source = @"
class MyClass
{
    public static int Main()
    {
        int ret = 0;
        double d1 = 2.0;
        double d2 = -0.0;
        double d3 = d1 / d2;

        if (d3 == double.NegativeInfinity)
        {
        }
        else
        {
            ret = ret + 1;
        }

        d1 = -2.0;
        d3 = d1 / d2;
        if (d3 != double.PositiveInfinity)
        {
            ret = ret + 1;
        }

        d1 = double.PositiveInfinity;
        d3 = d1 / d2;
        if (d3 != double.NegativeInfinity)
        {
            ret = ret + 1;
        }

        d1 = double.NegativeInfinity;
        d3 = d1 / d2;

        if (d3 != double.PositiveInfinity)
        {
            ret = ret + 1;
        }
        System.Console.WriteLine(ret);
        return ret;
    }
}
";

            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(540096, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540096")]
        [WorkItem(540878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540878")]
        [Fact]
        public void MissingOutFlagForParameter()
        {
            string source = @"
using System;
using System.Threading;

delegate T GenDelegate<T>(T p1, out T p2);
interface IGoo { U Function<U>(U i, out U j); }
class Goo : IGoo { public U Function<U>(U i, out U j) { j = i; return i; }}

class Test {

public static int Main() 
{
int i, j;
IGoo inst = new Goo();
GenDelegate<int> MyDelegate = new GenDelegate<int>(inst.Function<int>);
i = MyDelegate(10, out j);
if ((i != 10) || (j != 10))
{
Console.WriteLine(1);
return 1;
}
Console.WriteLine(0);
return 0;
}
}
";

            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(540097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540097")]
        [Fact]
        public void CallBaseMethodExplicitlyWithCall()
        {
            string source = @"
using System;
namespace Microsoft.Conformance.Expressions
{
    public class BaseClass
    {
        public virtual int intI() { return 1; }
    }
    public class base007 : BaseClass
    {
        override public int intI() { return 2; }
        public int TestInt()
        {
            return base.intI();
        }
        public static int Main()
        {
            base007 TC = new base007();
            if (TC.TestInt() == 1)
            {
                Console.WriteLine(0);
                return 0;
            }
            else
            {
                Console.WriteLine(1);
                return 1;
            }
        }
    }
}
";

            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(540149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540149")]
        [Fact]
        public void LHSParenthesizedProperty()
        {
            string source = @"
using System;

namespace Microsoft.Conformance.ParenthesizedExpression
{
    class A
    {
        public int p; 
        public int P  {    set {  p = value;  }    }

        static int Main(string[] args)
        {
            A a = new A();
            B b = new B();
            (a.P) = 0; //devdiv bug 168519.
            (b.a.P) = 0; //devdiv bug 168519.
            int ret = (a.p + b.a.p);
            Console.WriteLine(ret);
            return (ret);
        }
    }

    class B  {    public A a = new A();    }
}
";

            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(540158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540158")]
        [Fact]
        public void LoadMaxConstantForToString()
        {
            string source = @"
using System;

public class MyClass
{
    public static int Main()
    {
        int RetVal = 0;
        int i = int.MaxValue;
        string s = i.ToString();
        if (!s.Equals(Int32.MaxValue.ToString()))
        {
            RetVal = 1;
        }
        Console.WriteLine(RetVal);
        return RetVal;
    }
}
";

            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(540252, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540252")]
        [Fact]
        public void OverloadResolutionWithConsoleWriteLineMethodGroup()
        {
            var source =
                @"
using System;

class Program
{
    static void Main()
    {
        Goo(Console.WriteLine);
    }

    static void Goo(Action<string> a) { a(""Hello""); }
    static void Goo(Action<string, string, string, string, string> a) { }
}
";
            CompileAndVerify(source, expectedOutput: "Hello");
        }

        [WorkItem(540331, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540331")]
        [Fact]
        public void StringComparisonOpEquality()
        {
            string source = @"using System;

class Test
{
    static void Main()
    {
        string s = ""A"" + ""B"";
        if (s == ""AB"")
        {
            Console.Write(""Pass01|"");
        }
        else
        {
            Console.Write(""Fail01|"");
        }
        s = ""A"";
        string s2 = s + ""B"";
        if (s2 == ""AB"")
        {
            Console.Write(""Pass02|"");
        }
        else
        {
            Console.Write(""Fail02|"");
        }
        string s3 = s + ""b"";
        if (s3 != ""AB"")
        {
            Console.Write(""Pass03"");
        }
        else
        {
            Console.Write(""Fail03"");
        }
    }
}
";
            string expectedOutput = @"Pass01|Pass02|Pass03";

            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [WorkItem(528183, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528183")]
        [Fact]
        public void TestExternWithoutDLLImport()
        {
            string source = @"
class Test
{
    extern void Goo();
    static void Main()
    {
        (new Test()).Goo();
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            // Both Dev10 and Roslyn currently generate unverifiable code for this case...
            // Dev10 reports warning CS0626: Method, operator, or accessor 'Test.Goo()' is marked external
            // and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            comp.VerifyDiagnostics(
                // (4,17): warning CS0626: Method, operator, or accessor 'Test.Goo()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "Goo").WithArguments("Test.Goo()"));

            // NOTE: the resulting IL is unverifiable, but not an error for compat reasons
            CompileAndVerify(comp, verify: Verification.Fails).VerifyIL("Test.Main",
                @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  call       ""void Test.Goo()""
  IL_000a:  ret
}
");
        }

        [WorkItem(541790, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541790")]
        [Fact]
        public void TestLINQQueryableAnyExtensionMethod()
        {
            var source = @"
using System;
using System.Linq;
public class Test
{
    public static void Main(string[] args)
    {
        var collection = new int[] { 1, 2, 3 };
        var queryable = collection.AsQueryable();
        if (collection.AsQueryable().Any((a) => a != 0))
        {
            Console.WriteLine(""Success"");
        }
    }
}";
            CompileAndVerifyWithMscorlib40(
                source,
                references: new[] { SystemCoreRef },
                expectedOutput: @"Success");
        }

        [WorkItem(528651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528651")]
        [Fact]
        public void UserDefinedObject()
        {
            string source = @"
namespace System
{
    public class Object
    {
    }
    public class Void
    {
    }
}
class Test
{
    static void Main()
    {
    }
}
";
            CreateEmptyCompilation(source).VerifyEmitDiagnostics(
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion));
        }

        [WorkItem(542631, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542631")]
        [Fact]
        public void Queryable()
        {
            var source =
@"using System.Linq;
class C
{
    static void Main(string[] args)
    {
        IQueryable<int> q1 = null;
        var q = q1.Select(x => x);
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [WorkItem(542267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542267")]
        [Fact]
        public void PartialMethod()
        {
            var source =
@"partial class C
{
    partial void M1();
    partial void M1() { }
    partial void M2();
}";
            CompileAndVerify(source);
        }

        [WorkItem(542275, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542275")]
        [Fact]
        public void CallUnimplementedPartialMethod()
        {
            var source =
@"partial class C
{
    partial void M1();
    partial void M1() { }
    partial void M2();
    void M()
    {
        M1();
        M2();
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.M",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void C.M1()""
  IL_0006:  ret
}");
        }

        [WorkItem(542297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542297")]
        [Fact]
        public void GenericClassNameIdenticalToAliasSimpleName()
        {
            string source = @"using System;
using basic068Inner = basic068One.basic068Three;

public class Test
{
    public static void Main()
    {
        Console.Write(basic068Inner.basic068ThreeClass.F1);
    }
}

public class basic068Inner<A>
{
    public class basic068ThreeClass
    {
        public static int F1 = 1;
    }
}

namespace basic068One.basic068Three
{
    public class basic068ThreeClass
    {
        public static int F1 = 0;
    }
}
";
            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(542489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542489")]
        [Fact]
        public void PartialMethodOnlyHasDeclareButNotImplement()
        {
            string source = @"using System;
partial class program
{
    static void Main(string[] args)
    {
        goo(name: string.Empty, age: 1, gender: 1 > 2);
    }
}
partial class program
{
    static partial void goo(string name, int age, bool gender);
}
";
            CompileAndVerify(source);
        }

        [WorkItem(538544, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538544")]
        [Fact]
        public void DecimalLiteral01()
        {
            string source = @"using System;
public class MyClass {
    public static void Main()
    {
        Console.WriteLine(0E-10M);
        Console.WriteLine(1E-30M);
        Console.WriteLine(10E-1M);
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
0.0000000000
0.0000000000000000000000000000
1.0
");
        }

        [WorkItem(543897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543897")]
        [Fact]
        public void DecimalLiteral02()
        {
            string source = @"using System;
using System.Globalization;
using System.Threading;
public class MyClass {
    public static void Main()
    {
        System.Globalization.CultureInfo saveCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            decimal d = 1.00m;
            Console.WriteLine((d).ToString());
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = saveCulture;
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "1.00");
        }

        // Breaking change: native compiler considers
        // digits < 1e-49 when rounding.
        [WorkItem(529827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529827")]
        [WorkItem(568494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568494")]
        [WorkItem(32576, "https://github.com/dotnet/roslyn/issues/32576")]
        [Fact]
        public void DecimalLiteral_BreakingChange()
        {
            string source =
@"using System;
class C
{
    static void Main()
    {
        Console.WriteLine(3.0500000000000000000001e-27m); // 3.05e-27m + 1e-49m [Dev11/Roslyn rounds]
        Console.WriteLine(3.05000000000000000000001e-27m);  // 3.05e-27m + 1e-50m [Dev11 rounds, Roslyn does not]
        Console.WriteLine();
        Console.WriteLine(5.00000000000000000001e-29m); // 5.0e-29m + 1e-49m [Dev11/Roslyn rounds]
        Console.WriteLine(5.0000000000000000000000000000001e-29m); // 5.0e-29m + 1e-60m [Dev11 rounds, Roslyn does not]
        Console.WriteLine();
        Console.WriteLine(-5.00000000000000000001e-29m); // -5.0e-29m + 1e-49m [Dev11/Roslyn rounds]
        Console.WriteLine(-5.0000000000000000000000000000001e-29m); // -5.0e-29m + 1e-60m [Dev11 rounds, Roslyn does not]
        Console.WriteLine();
        //                         10        20        30        40        50
        Console.WriteLine(.10000000000000000000000000005000000000000000000001m); // [Dev11 chops at 50 digits and rounds, Roslyn does not round]
        Console.WriteLine(.100000000000000000000000000050000000000000000000001m); // [Dev11 chops at 50 digits and does not round, Roslyn does not round]
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"0.0000000000000000000000000031
0.0000000000000000000000000030

0.0000000000000000000000000001
0.0000000000000000000000000000

-0.0000000000000000000000000001
0.0000000000000000000000000000

0.1000000000000000000000000000
0.1000000000000000000000000000");
        }

        [Fact]
        public void DecimalZero()
        {
            var source = @"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        Dump(0E0M);
        Dump(0.0E0M);
        Dump(0.00E0M);
        Console.WriteLine();

        Dump(0E-1M);
        Dump(0E-10M);
        Dump(-0E-10M);
        Dump(0.00E-10M);
        Dump(0E-100M); //differs from dev10
        Console.WriteLine();

        Dump(decimal.Negate(0E0M));
        Dump(decimal.Negate(0.0E0M));
        Dump(decimal.Negate(0.00E0M));
        Console.WriteLine();

        Dump(decimal.Negate(0E-1M));
        Dump(decimal.Negate(0E-10M));
        Dump(decimal.Negate(-0E-10M));
        Dump(decimal.Negate(0.00E-10M));
        Dump(decimal.Negate(0E-100M)); //differs from dev10
    }

    static string ToHexString(decimal d)
    {
        return string.Join("""", decimal.GetBits(d).Select(word => string.Format(""{0:x8}"", word)));
    }

    static void Dump(decimal d)
    {
        Console.WriteLine(ToHexString(d));
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: @"
00000000000000000000000000000000
00000000000000000000000000010000
00000000000000000000000000020000

00000000000000000000000000010000
000000000000000000000000000a0000
000000000000000000000000800a0000
000000000000000000000000000c0000
000000000000000000000000001c0000

00000000000000000000000080000000
00000000000000000000000080010000
00000000000000000000000080020000

00000000000000000000000080010000
000000000000000000000000800a0000
000000000000000000000000000a0000
000000000000000000000000800c0000
000000000000000000000000801c0000
");
        }

        // Breaking change: native compiler allows 0eNm where N > 0.
        // (The native compiler ignores sign and scale in 0eNm if N > 0
        // and represents such cases as 0e0m.)
        [WorkItem(568475, "DevDiv")]
        [Fact]
        public void DecimalZero_BreakingChange()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        Console.WriteLine(0E1M);
        Console.WriteLine(0E10M);
        Console.WriteLine(-0E10M);
        Console.WriteLine(0.00E10M);
        Console.WriteLine(-0.00E10M);
        Console.WriteLine(0E100M);
    }
}";
            decimal d;
            if (decimal.TryParse("0E1", System.Globalization.NumberStyles.AllowExponent, null, out d))
            {
                CreateCompilation(source).VerifyDiagnostics();
            }
            else
            {
                CreateCompilation(source).VerifyDiagnostics(
                    // (6,27): error CS0594: Floating-point constant is outside the range of type 'decimal'
                    Diagnostic(ErrorCode.ERR_FloatOverflow, "0E1M").WithArguments("decimal").WithLocation(6, 27),
                    // (7,27): error CS0594: Floating-point constant is outside the range of type 'decimal'
                    Diagnostic(ErrorCode.ERR_FloatOverflow, "0E10M").WithArguments("decimal").WithLocation(7, 27),
                    // (8,28): error CS0594: Floating-point constant is outside the range of type 'decimal'
                    Diagnostic(ErrorCode.ERR_FloatOverflow, "0E10M").WithArguments("decimal").WithLocation(8, 28),
                    // (9,27): error CS0594: Floating-point constant is outside the range of type 'decimal'
                    Diagnostic(ErrorCode.ERR_FloatOverflow, "0.00E10M").WithArguments("decimal").WithLocation(9, 27),
                    // (10,28): error CS0594: Floating-point constant is outside the range of type 'decimal'
                    Diagnostic(ErrorCode.ERR_FloatOverflow, "0.00E10M").WithArguments("decimal").WithLocation(10, 28),
                    // (11,27): error CS0594: Floating-point constant is outside the range of type 'decimal'
                    Diagnostic(ErrorCode.ERR_FloatOverflow, "0E100M").WithArguments("decimal").WithLocation(11, 27));
            }
        }

        [Fact]
        public void DecimalPositive()
        {
            var source = @"
using System;
using System.Linq;

class Goo
{
    static void Main()
    {
        Dump(1E0M);
        Dump(1.0E0M);
        Dump(1.00E0M);
        Console.WriteLine();

        Dump(1E1M);
        Dump(1E10M);
        Dump(1E28M);
        Console.WriteLine();

        Dump(1E-1M);
        Dump(1E-10M);
        Dump(1E-28M);
        Console.WriteLine();

        Dump(decimal.Negate(1E0M));
        Dump(decimal.Negate(1.0E0M));
        Dump(decimal.Negate(1.00E0M));
        Console.WriteLine();

        Dump(decimal.Negate(1E1M));
        Dump(decimal.Negate(1E10M));
        Dump(decimal.Negate(1E28M));
        Console.WriteLine();

        Dump(decimal.Negate(1E-1M));
        Dump(decimal.Negate(1E-10M));
        Dump(decimal.Negate(1E-28M));
    }

    static string ToHexString(decimal d)
    {
        return string.Join("""", decimal.GetBits(d).Select(word => string.Format(""{0:x8}"", word)));
    }

    static void Dump(decimal d)
    {
        Console.WriteLine(ToHexString(d));
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: @"
00000001000000000000000000000000
0000000a000000000000000000010000
00000064000000000000000000020000

0000000a000000000000000000000000
540be400000000020000000000000000
100000003e250261204fce5e00000000

00000001000000000000000000010000
000000010000000000000000000a0000
000000010000000000000000001c0000

00000001000000000000000080000000
0000000a000000000000000080010000
00000064000000000000000080020000

0000000a000000000000000080000000
540be400000000020000000080000000
100000003e250261204fce5e80000000

00000001000000000000000080010000
000000010000000000000000800a0000
000000010000000000000000801c0000
");
        }

        [Fact]
        public void DecimalNegative()
        {
            var source = @"
using System;
using System.Linq;

class Goo
{
    static void Main()
    {
        Dump(-1E0M);
        Dump(-1.0E0M);
        Dump(-1.00E0M);
        Console.WriteLine();

        Dump(-1E1M);
        Dump(-1E10M);
        Dump(-1E28M);
        Console.WriteLine();

        Dump(-1E-1M);
        Dump(-1E-10M);
        Dump(-1E-28M);
        Console.WriteLine();

        Dump(decimal.Negate(-1E0M));
        Dump(decimal.Negate(-1.0E0M));
        Dump(decimal.Negate(-1.00E0M));
        Console.WriteLine();

        Dump(decimal.Negate(-1E1M));
        Dump(decimal.Negate(-1E10M));
        Dump(decimal.Negate(-1E28M));
        Console.WriteLine();

        Dump(decimal.Negate(-1E-1M));
        Dump(decimal.Negate(-1E-10M));
        Dump(decimal.Negate(-1E-28M));
    }

    static string ToHexString(decimal d)
    {
        return string.Join("""", decimal.GetBits(d).Select(word => string.Format(""{0:x8}"", word)));
    }

    static void Dump(decimal d)
    {
        Console.WriteLine(ToHexString(d));
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: @"
00000001000000000000000080000000
0000000a000000000000000080010000
00000064000000000000000080020000

0000000a000000000000000080000000
540be400000000020000000080000000
100000003e250261204fce5e80000000

00000001000000000000000080010000
000000010000000000000000800a0000
000000010000000000000000801c0000

00000001000000000000000000000000
0000000a000000000000000000010000
00000064000000000000000000020000

0000000a000000000000000000000000
540be400000000020000000000000000
100000003e250261204fce5e00000000

00000001000000000000000000010000
000000010000000000000000000a0000
000000010000000000000000001c0000
");
        }

        [Fact]
        public void DecimalMaxValue()
        {
            var source = @"
using System;
using System.Linq;

class Goo
{
    static void Main()
    {
        Dump(79228162514264337593543950335E0M);
        Dump(7922816251426433759354395033.5E1M);
        Dump(792281625142643375935439.50335E5M);
        Dump(7.9228162514264337593543950335E28M);
        Console.WriteLine();

        Dump(79228162514264337593543950335E-0M);
        Dump(79228162514264337593543950335E-1M);
        Dump(79228162514264337593543950335E-5M);
        Dump(79228162514264337593543950335E-28M);
        Console.WriteLine();

        Dump(decimal.Negate(79228162514264337593543950335E0M));
        Dump(decimal.Negate(7922816251426433759354395033.5E1M));
        Dump(decimal.Negate(792281625142643375935439.50335E5M));
        Dump(decimal.Negate(7.9228162514264337593543950335E28M));
        Console.WriteLine();

        Dump(decimal.Negate(79228162514264337593543950335E-0M));
        Dump(decimal.Negate(79228162514264337593543950335E-1M));
        Dump(decimal.Negate(79228162514264337593543950335E-5M));
        Dump(decimal.Negate(79228162514264337593543950335E-28M));
    }

    static string ToHexString(decimal d)
    {
        return string.Join("""", decimal.GetBits(d).Select(word => string.Format(""{0:x8}"", word)));
    }

    static void Dump(decimal d)
    {
        Console.WriteLine(ToHexString(d));
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: @"
ffffffffffffffffffffffff00000000
ffffffffffffffffffffffff00000000
ffffffffffffffffffffffff00000000
ffffffffffffffffffffffff00000000

ffffffffffffffffffffffff00000000
ffffffffffffffffffffffff00010000
ffffffffffffffffffffffff00050000
ffffffffffffffffffffffff001c0000

ffffffffffffffffffffffff80000000
ffffffffffffffffffffffff80000000
ffffffffffffffffffffffff80000000
ffffffffffffffffffffffff80000000

ffffffffffffffffffffffff80000000
ffffffffffffffffffffffff80010000
ffffffffffffffffffffffff80050000
ffffffffffffffffffffffff801c0000
");
        }

        [Fact]
        public void DecimalConversion01()
        {
            string source = @"
using System;

class C
{
    static int Goo()
    {
        int i = 10;
        return (int)(decimal)(int)i;
    }

    static int Bar()
    {
        return (int)decimal.One;
    }

    static void Main(string[] args)
    {
        int i = Goo();
        int j = Bar();

        Console.Write(i);
        Console.Write(j);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "101");
            compilation.VerifyIL("C.Goo",
            @"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldc.i4.s   10
  IL_0002:  call       ""decimal decimal.op_Implicit(int)""
  IL_0007:  call       ""int decimal.op_Explicit(decimal)""
  IL_000c:  ret
}");
            compilation.VerifyIL("C.Bar",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}");
        }

        [Fact]
        public void DecimalConversion02()
        {
            string source = @"
using System;

class C
{
    static void Main(string[] args)
    {
        decimal myMoney = 99.9m;
        double x = (double)myMoney;
        myMoney = (decimal)x;
        System.Console.Write(myMoney);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "99.9");
            compilation.VerifyIL("C.Main",
@"{
  // Code size       31 (0x1f)
  .maxstack  5
  IL_0000:  ldc.i4     0x3e7
  IL_0005:  ldc.i4.0
  IL_0006:  ldc.i4.0
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_000e:  call       ""double decimal.op_Explicit(decimal)""
  IL_0013:  conv.r8
  IL_0014:  call       ""decimal decimal.op_Explicit(double)""
  IL_0019:  call       ""void System.Console.Write(decimal)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void DecimalConversion03()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        D = I;
    }
 
    public static decimal D { get; set; }
    public static int I { get; set; }
}
";
            var compilation = CompileAndVerify(source);
        }

        [Fact]
        public void DecimalConversion04()
        {
            string source = @"
class Program
{
    double x = 1.0;
    decimal y = decimal.One;

    static void Main(string[] args)
    {
        Program a = new Program();
        double x = (double)a.y;
        decimal y = (decimal)a.x;

        System.Console.Write(x);
        System.Console.Write(y);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "11");
        }

        [WorkItem(543888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543888")]
        [Fact]
        public void DecimalConversion05()
        {
            string source = @"
using System;
enum E
{
    One = 1, Two, Three, Four, Five
}

public class C
{
    public static void Main()
    {
        decimal d1 = 1m;
        decimal d2 = 3m;
        decimal d3 = 5m;

        E e = (E)d1;
        Console.WriteLine(e);

        e = (E)d2;
        Console.WriteLine(e);

        e = (E)d3;
        Console.WriteLine(e);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
One
Three
Five
");
        }

        [WorkItem(543888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543888")]
        [Fact]
        public void DecimalConversion06()
        {
            string source = @"
using System;
enum E
{
    Sat, Sun, Mon, Tue, Wed, Thu, Fri
}

public class C
{
    public static void Main()
    {
        decimal d = (decimal)E.Sun;
        Console.WriteLine(d);

        d = (decimal)E.Wed;
        Console.WriteLine(d);

        d = (decimal)E.Fri;
        Console.WriteLine(d);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
4
6
");
        }

        [WorkItem(543888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543888")]
        [Fact]
        public void DecimalConversion07()
        {
            string source = @"
using System;
enum E
{
    E1, E2
}

public class C
{
    public static void Main()
    {
        decimal d1 = 1.00M;

        decimal d2 = (decimal)(E)d1;
        Console.WriteLine(d2);

        E e = (E)((decimal)(E.E1)+1);
        Console.WriteLine(e);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
E2
");
        }

        [WorkItem(539392, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539392")]
        [Fact]
        public void DecimalBinaryOp_01()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        var y = decimal.MaxValue + 0;
        System.Console.Write(y);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "79228162514264337593543950335");
            compilation.VerifyIL("C.Main",
@"{
  // Code size       16 (0x10)
  .maxstack  5
  IL_0000:  ldc.i4.m1
  IL_0001:  ldc.i4.m1
  IL_0002:  ldc.i4.m1
  IL_0003:  ldc.i4.0
  IL_0004:  ldc.i4.0
  IL_0005:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_000a:  call       ""void System.Console.Write(decimal)""
  IL_000f:  ret
}");
        }

        [WorkItem(543279, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543279")]
        [Fact]
        public void DecimalBinaryOp_02()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        System.Console.Write(decimal.MaxValue + decimal.MinusOne);     
        System.Console.Write(0 + decimal.MinusOne);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "79228162514264337593543950334-1");
            compilation.VerifyIL("C.Main",
@"
{
  // Code size       27 (0x1b)
  .maxstack  5
  IL_0000:  ldc.i4.s   -2
  IL_0002:  ldc.i4.m1
  IL_0003:  ldc.i4.m1
  IL_0004:  ldc.i4.0
  IL_0005:  ldc.i4.0
  IL_0006:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_000b:  call       ""void System.Console.Write(decimal)""
  IL_0010:  ldsfld     ""decimal decimal.MinusOne""
  IL_0015:  call       ""void System.Console.Write(decimal)""
  IL_001a:  ret
}");
        }

        [Fact]
        [WorkItem(32576, "https://github.com/dotnet/roslyn/issues/32576")]
        [WorkItem(34198, "https://github.com/dotnet/roslyn/issues/34198")]
        public void DecimalBinaryOp_03()
        {
            string source = @"
class C
{
    // http://msdn.microsoft.com/en-US/library/system.decimal.remainder(v=vs.110).aspx
    static void M(decimal d1, decimal d2)
    {
        var r1 = d1 + d2;
        System.Console.WriteLine(r1);

        var r2 = d1 - d2;
        System.Console.WriteLine(r2);

        var r3 = d1 * d2;
        System.Console.WriteLine(r3);

        var r4 = d1 / d2;
        System.Console.WriteLine(r4);

        var r5 = d1 % d2;
        System.Console.WriteLine(r5);
    }

    static void Main(string[] args)
    {
        M(1000M, 7M);
        M(-1000M, 7M);
        M(new decimal(1230000000, 0, 0, false, 7), 0.0012300M);
        M(12345678900000000M, 0.0000000012345678M);
        M(123456789.0123456789M, 123456789.1123456789M);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1007
993
7000
142.85714285714285714285714286
6
-993
-1007
-7000
-142.85714285714285714285714286
-6
123.0012300
122.9987700
0.15129000000000
100000
0.0000000
12345678900000000.000000001235
12345678899999999.999999998765
15241577.6390794200000000
10000000729000059778004901.796
0.000000000983
246913578.1246913578
-0.1000000000
15241578765584515.651425087878
0.9999999991899999933660999449
123456789.0123456789
");
        }

        [Fact]
        public void DecimalBinaryOp_04()
        {
            string source = @"
class C
{
    // http://msdn.microsoft.com/en-us/library/system.decimal.compare(v=vs.110).aspx
        static void M(decimal d1, decimal d2)
        {
            var r1 = d1 == d2;
            System.Console.WriteLine(r1);

            var r2 = d1 != d2;
            System.Console.WriteLine(r2);

            var r3 = d1 >= d2;
            System.Console.WriteLine(r3);

            var r4 = d1 > d2;
            System.Console.WriteLine(r4);

            var r5 = d1 <= d2;
            System.Console.WriteLine(r5);

            var r6 = d1 < d2;
            System.Console.WriteLine(r6);
            System.Console.WriteLine();
        }

        static void Main(string[] args)
        {
            M(new decimal(123.456), new decimal(1.2345600E+2));
            M(new decimal(123.456), 123.4561M);
            M(new decimal(123.456), 123.4559M);
            M(new decimal(123.456), 123.456000M);
            M(new decimal(123.456), new decimal(123456000, 0, 0, false, 6));
        }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
True
False
True
False
True
False

False
True
False
False
True
True

False
True
True
True
False
False

True
False
True
False
True
False

True
False
True
False
True
False

");
        }

        [Fact]
        public void DecimalUnaryOp_01()
        {
            string source = @"
class C
{
        static void Main(string[] args)
        {
            var x1 = +123.456M;
            System.Console.WriteLine(x1);

            var x2 = -123.456M;
            System.Console.WriteLine(x2);

            var x3 = +x1;
            System.Console.WriteLine(x3);

            var x4 = -x1;
            System.Console.WriteLine(x4);

            var x5 = ++x1;
            System.Console.WriteLine(x5);

            var x6 = x1++;
            System.Console.WriteLine(x6);

            var x7 = x1--;
            System.Console.WriteLine(x7);

            var x8 = --x1;
            System.Console.WriteLine(x8);
        }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
123.456
-123.456
123.456
-123.456
124.456
124.456
125.456
123.456
");
            compilation.VerifyIL("C.Main",
@"
{
  // Code size      111 (0x6f)
  .maxstack  6
  .locals init (decimal V_0) //x1
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4     0x1e240
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.3
  IL_000b:  call       ""decimal..ctor(int, int, int, bool, byte)""
  IL_0010:  ldloc.0
  IL_0011:  call       ""void System.Console.WriteLine(decimal)""
  IL_0016:  ldc.i4     0x1e240
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldc.i4.1
  IL_001e:  ldc.i4.3
  IL_001f:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_0024:  call       ""void System.Console.WriteLine(decimal)""
  IL_0029:  ldloc.0
  IL_002a:  call       ""void System.Console.WriteLine(decimal)""
  IL_002f:  ldloc.0
  IL_0030:  call       ""decimal decimal.op_UnaryNegation(decimal)""
  IL_0035:  call       ""void System.Console.WriteLine(decimal)""
  IL_003a:  ldloc.0
  IL_003b:  call       ""decimal decimal.op_Increment(decimal)""
  IL_0040:  dup
  IL_0041:  stloc.0
  IL_0042:  call       ""void System.Console.WriteLine(decimal)""
  IL_0047:  ldloc.0
  IL_0048:  dup
  IL_0049:  call       ""decimal decimal.op_Increment(decimal)""
  IL_004e:  stloc.0
  IL_004f:  call       ""void System.Console.WriteLine(decimal)""
  IL_0054:  ldloc.0
  IL_0055:  dup
  IL_0056:  call       ""decimal decimal.op_Decrement(decimal)""
  IL_005b:  stloc.0
  IL_005c:  call       ""void System.Console.WriteLine(decimal)""
  IL_0061:  ldloc.0
  IL_0062:  call       ""decimal decimal.op_Decrement(decimal)""
  IL_0067:  dup
  IL_0068:  stloc.0
  IL_0069:  call       ""void System.Console.WriteLine(decimal)""
  IL_006e:  ret
}
");
        }

        [Fact]
        public void DecimalUnaryOp_02()
        {
            string source = @"
using System;

class C
{
    // http://msdn.microsoft.com/en-us/library/system.decimal.op_unarynegation(v=vs.110).aspx
    public static string GetExceptionType(Exception ex)
    {
        string exceptionType = ex.GetType().ToString();
        return exceptionType.Substring(
            exceptionType.LastIndexOf('.') + 1);
    }

    // Display the argument and the incremented and decremented values.
    public static void DecIncrDecrUnary(decimal argument)
    {
        decimal toBeIncr = argument;
        decimal toBeDecr = argument;

        // Catch the exception if the increment operator throws one.
        try
        {
            toBeIncr++;
            System.Console.WriteLine(toBeIncr);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(GetExceptionType(ex));
        }

        // Catch the exception if the decrement operator throws one.
        try
        {
            toBeDecr--;
            System.Console.WriteLine(toBeDecr);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(GetExceptionType(ex));
        }

        System.Console.WriteLine();
    }

    static void Main(string[] args)
    {
        // Create objects to compare with the reference.
        DecIncrDecrUnary(0.000000123M);
        DecIncrDecrUnary(new decimal(123000000, 0, 0, false, 9));
        DecIncrDecrUnary(-new decimal(123000000, 0, 0, false, 9));
        DecIncrDecrUnary(+decimal.MaxValue);
        DecIncrDecrUnary(-decimal.MaxValue);
        DecIncrDecrUnary(+7.5000000000000000000000000001M);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1.000000123
-0.999999877

1.123000000
-0.877000000

0.877000000
-1.123000000

OverflowException
79228162514264337593543950334

-79228162514264337593543950334
OverflowException

8.500000000000000000000000000
6.5000000000000000000000000001

");
        }

        [WorkItem(542568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542568")]
        [Fact]
        public void ImplicitConversionForOptional()
        {
            string source = @"class MyClass
{
    void Goo(decimal x = 10) { }
}
";
            CompileAndVerify(source);
        }

        [WorkItem(542742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542742")]
        [Fact]
        public void CodeGenEmptyStatement()
        {
            string source = @"class Test
{
    static void Main()
    {
        ;
    }
}";
            CompileAndVerify(source);
        }

        [WorkItem(542458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542458")]
        [Fact]
        public void DefaultParamValueIsStruct_SameAssembly()
        {
            string source = @"
public class Test
{
    public static void Main()
    {
        DefaultParameterValues.M(""hello"");
    }
}

public class DefaultParameterValues
{
    public static void M(
            string text,
            string path = """",
            DefaultParameterValues d = null,
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { }
}
";
            CompileAndVerify(source).VerifyIL("Test.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  4
  .locals init (System.Threading.CancellationToken V_0)
  IL_0000:  ldstr      ""hello""
  IL_0005:  ldstr      """"
  IL_000a:  ldnull
  IL_000b:  ldloca.s   V_0
  IL_000d:  initobj    ""System.Threading.CancellationToken""
  IL_0013:  ldloc.0
  IL_0014:  call       ""void DefaultParameterValues.M(string, string, DefaultParameterValues, System.Threading.CancellationToken)""
  IL_0019:  ret
}
");
        }

        [WorkItem(542458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542458")]
        [Fact]
        public void DefaultParamValueIsStruct_DifferentAssembly()
        {
            var source1 = @"
public class DefaultParameterValues
{
    public static void M(
            string text,
            string path = """",
            DefaultParameterValues d = null,
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { }
}
";
            var compilation1 = CreateCompilation(source1);

            var source2 = @"
public class Test
{
    public static void Main()
    {
        DefaultParameterValues.M(""hello"");
    }
}
";
            CompileAndVerify(source2, new[] { new CSharpCompilationReference(compilation1) }).VerifyIL("Test.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  4
  .locals init (System.Threading.CancellationToken V_0)
  IL_0000:  ldstr      ""hello""
  IL_0005:  ldstr      """"
  IL_000a:  ldnull
  IL_000b:  ldloca.s   V_0
  IL_000d:  initobj    ""System.Threading.CancellationToken""
  IL_0013:  ldloc.0
  IL_0014:  call       ""void DefaultParameterValues.M(string, string, DefaultParameterValues, System.Threading.CancellationToken)""
  IL_0019:  ret
}
");
        }

        [WorkItem(542458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542458")]
        [Fact]
        public void DefaultParamValueIsStruct_MetadataAssembly()
        {
            var source = @"
public class Test
{
    public static void Main()
    {
        DefaultParameterValues.M(""hello"");
    }
}
";
            CompileAndVerify(source, new[] { TestReferences.SymbolsTests.Methods.CSMethods }).VerifyIL("Test.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  4
  .locals init (System.Threading.CancellationToken V_0)
  IL_0000:  ldstr      ""hello""
  IL_0005:  ldstr      """"
  IL_000a:  ldnull
  IL_000b:  ldloca.s   V_0
  IL_000d:  initobj    ""System.Threading.CancellationToken""
  IL_0013:  ldloc.0
  IL_0014:  call       ""void DefaultParameterValues.M(string, string, DefaultParameterValues, System.Threading.CancellationToken)""
  IL_0019:  ret
}
");
        }

        [WorkItem(542417, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542417")]
        [Fact]
        public void DefaultParameterInGenericMethod()
        {
            var source =
@"using System;

public class Program
{
    public static void Goo(string str2, string str = ""test1"")
    {
        Console.WriteLine(""Goo("" + str2 + "", "" + str + "")"");
    }

    public static void Goo<T>(T str2, string str = ""test2"")
    {
        Console.WriteLine(""Goo<"" + typeof(T) + "">("" + str2 + "", "" + str + "")"");
    }

    public static void Main()
    {
        Goo<string>(""test3"");
    }
}";
            CompileAndVerify(source, expectedOutput: "Goo<System.String>(test3, test2)");
        }

        [WorkItem(542920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542920")]
        [Fact]
        public void PassExceptionVarByRef()
        {
            string source = @"
    using System;
    class Test
    {
        static void Goo(out Exception ex)
        {
            ex = new Exception(""bye"");
        }

        static void Main()
        {
            try{
                throw new Exception(""hi"");
            } catch (Exception ex){
                Goo(out ex);
                Console.WriteLine(ex.Message);
            }
        }
    }
";
            var compilation = CompileAndVerify(source, expectedOutput: @"bye");

            compilation.VerifyIL("Test.Main",
@"{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (System.Exception V_0) //ex
  .try
{
  IL_0000:  ldstr      ""hi""
  IL_0005:  newobj     ""System.Exception..ctor(string)""
  IL_000a:  throw
}
  catch System.Exception
{
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       ""void Test.Goo(out System.Exception)""
  IL_0013:  ldloc.0
  IL_0014:  callvirt   ""string System.Exception.Message.get""
  IL_0019:  call       ""void System.Console.WriteLine(string)""
  IL_001e:  leave.s    IL_0020
}
  IL_0020:  ret
}
");
        }

        [WorkItem(10616, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EnumAsOptionalParameter()
        {
            string source = @"
using System;

public enum E
{
    e1, e2=3, e3
}

public class Parent
{
    public int Goo(E e = E.e2) { return e == E.e2 ? 0 : 1; }
}

public class Test
{
    public static int Main()
    {
        var ret = new Parent().Goo();
        Console.WriteLine(ret);
        return ret;
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"0");

            compilation.VerifyIL("Test.Main",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  newobj     ""Parent..ctor()""
  IL_0005:  ldc.i4.3
  IL_0006:  call       ""int Parent.Goo(E)""
  IL_000b:  dup
  IL_000c:  call       ""void System.Console.WriteLine(int)""
  IL_0011:  ret
}");
        }

        [WorkItem(2272, "https://github.com/dotnet/roslyn/issues/2272")]
        [Fact]
        public void DefaultParamValueIsEnum()
        {
            string source = @"
using System;

public class Parent
{
    public int Goo(ConsoleKey e = ConsoleKey.A) { return e == ConsoleKey.A ? 0 : 1; }
}

public class Test
{
    public static int Main()
    {
        var ret = new Parent().Goo();
        Console.WriteLine(ret);
        return ret;
    }
}
";
            CompileAndVerify(source, expectedOutput: @"0");
        }

        [WorkItem(543089, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543089")]
        [Fact()]
        public void NoOutAttributeOnMethodReturn()
        {
            string source = @"
using System;

class A : Attribute { }
class C
{
    [return: A]
    void Goo() { }

    static void Main()
    {
        var obj = new C();
        var attrs = ((Action)obj.Goo).Method.ReturnTypeCustomAttributes.GetCustomAttributes(false);
        Console.Write(attrs.Length);
        for (int i = 0; i < attrs.Length; i++)
            Console.WriteLine(attrs[i]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"1A");
        }

        [WorkItem(543090, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543090")]
        [Fact()]
        public void EmitAttributeOnPartialMethodReturnType()
        {
            string source = @"
using System;

class A : Attribute { }

partial class C
{
    [return: A]
    partial void Bar() { }
}

partial class C
{
    partial void Bar();

    static void Main()
    {
        var obj = new C();
        var attrs = ((Action)obj.Bar).Method.ReturnTypeCustomAttributes.GetCustomAttributes(false);
        Console.Write(attrs.Length);
        for (int i = 0; i < attrs.Length; i++)
            Console.WriteLine(attrs[i]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"1A");
        }

        [WorkItem(543530, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543530")]
        [Fact()]
        public void EmitAttributeWithNullTypeArgument1()
        {
            string source = @"
using System;
using System.Reflection;

class Base1 : Attribute
{
    public Base1(Type opt = null)
    {
        this.Result = opt == null ? ""null"" : opt.ToString();
    }
    public string Result;
}

class C1
{
    [Base1(null)]
    static void A(string[] args)
    {
    }
    [Base1()]
    static void B(string name)
    {
        var m = typeof(C1).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        Console.Write(((Base1)m.GetCustomAttributes(typeof(Base1), false)[0]).Result);
        Console.Write("";"");
    }
    [Base1(typeof(C1))]
    static void Main(string[] args)
    {
        B(""A"");
        B(""B"");
        B(""Main"");
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "null;null;C1;");
        }
        [WorkItem(543091, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543091")]
        [Fact()]
        public void EmitAttributeOnPartialMethodParameter()
        {
            string source = @"
using System;

class A : Attribute { }

partial class C
{
    partial void Gen<T>([A]T t);
}

partial class C
{
    partial void Gen<T>(T t) { }

    static void Main()
    {
        var obj = new C();
        var goo = ((Action<int>)obj.Gen<int>).Method;
        var attrs = goo.GetParameters()[0].GetCustomAttributes(false);
        Console.Write(attrs.Length);
        for (int i = 0; i < attrs.Length; i++)
            Console.WriteLine(attrs[i]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"1A");
        }

        [WorkItem(543156, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543156")]
        [Fact()]
        public void OverloadResolutionWithParams()
        {
            string source = @"
using System;

public class C
{
    public int Goo(object i = null, object j = null) { return 0; }
    public int Goo(int i = 0, params object[] arr) { return 1; }

    public int Bar(object o = null, int i = 1, params object[] arr) { return 1; }
    public int Bar(string s = ""PickMe"", int i = 1, object o = null) { return 0; }
}

class Test
{
    public static void Main()
    {
        var obj = new C();
        Console.Write(obj.Goo());
        Console.Write(obj.Bar(i: 3));
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"00");
        }

        [WorkItem(543157, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543157")]
        [Fact()]
        public void MultipleUseOfObjectInstance()
        {
            string source = @"
using System;

class Program
{
    public static void Main()
    {
        Console.WriteLine(Test());
    }

    public static int Test()
    {
        var obj = new C();
        if (obj.M1() != 10)
            return 1;

        if (obj.M2() != 20)
            return 1;

        return 0;
    }

    class C
    {
        public int M1()  {    return 10;    }
        public int M2()  {    return 20;    }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"0");
        }

        [WorkItem(543566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543566")]
        [Fact()]
        public void OptionalTypeParameterWithDefaultT()
        {
            string source = @"
using System;
using System.Collections.Generic;

public class Parent<T>
{
    public int Goo(T t = default(T))
    {
        if (t == null) return 0;
        return 1;
    }
}

class Test
{
    public static void Main()
    {
        var p = new Parent<String>();
        Console.Write(p.Goo());
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"0");
        }

        [Fact(), WorkItem(543667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543667")]
        public void BaseCallInAnonymousMethodInGenericMethodWithHoistedLocal()
        {
            string source = @"
using System;

class A<T>
{
    public virtual V PrintField<V, W>(V v, W w)
    {
        return default(V);
    }
}
class B<T> : A<T>
{
    public override V PrintField<V, W>(V v, W w)
    {
        int i = 10;
        Func<V, W, V> f = delegate(V dv, W dw)
        {
            var x = i;
            return base.PrintField(dv, dw);
        };
        return default(V);
    }
}";
            CompileAndVerify(source);
        }

        #endregion

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
        public void MutateReadonlyNested()
        {
            string source = @"

using System;
class Program
{
    public static void Main()
    {
        var c = new cls1();

        c.y.mutate(123);
        c.y.n.n.mutate(456);
        Console.WriteLine(c.y.n.n.num);
    }
}

class cls1
{
    public readonly MyManagedStruct y = new MyManagedStruct(42);
}

struct MyManagedStruct
{
    public struct Nested
    {
        public Nested1 n;

        public struct Nested1
        {
            public int num;

            public void mutate(int x)
            {
                num = x;
            }
        }
    }

    public Nested n;

    public void mutate(int x)
    {
        n.n.num = x;
    }

    public MyManagedStruct(int x)
    {
        n.n.num = x;
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"42", parseOptions: TestOptions.Regular7_2, verify: Verification.Fails);

            comp.VerifyIL("Program.Main",
@"
{
  // Code size       76 (0x4c)
  .maxstack  3
  .locals init (MyManagedStruct V_0,
                MyManagedStruct.Nested.Nested1 V_1)
  IL_0000:  newobj     ""cls1..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""MyManagedStruct cls1.y""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.s   123
  IL_0010:  call       ""void MyManagedStruct.mutate(int)""
  IL_0015:  dup
  IL_0016:  ldflda     ""MyManagedStruct cls1.y""
  IL_001b:  ldflda     ""MyManagedStruct.Nested MyManagedStruct.n""
  IL_0020:  ldfld      ""MyManagedStruct.Nested.Nested1 MyManagedStruct.Nested.n""
  IL_0025:  stloc.1
  IL_0026:  ldloca.s   V_1
  IL_0028:  ldc.i4     0x1c8
  IL_002d:  call       ""void MyManagedStruct.Nested.Nested1.mutate(int)""
  IL_0032:  ldflda     ""MyManagedStruct cls1.y""
  IL_0037:  ldflda     ""MyManagedStruct.Nested MyManagedStruct.n""
  IL_003c:  ldflda     ""MyManagedStruct.Nested.Nested1 MyManagedStruct.Nested.n""
  IL_0041:  ldfld      ""int MyManagedStruct.Nested.Nested1.num""
  IL_0046:  call       ""void System.Console.WriteLine(int)""
  IL_004b:  ret
}
");

            comp = CompileAndVerify(source, expectedOutput: @"42", verify: Verification.Passes, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());

            comp.VerifyIL("Program.Main",
@"
{
  // Code size       76 (0x4c)
  .maxstack  3
  .locals init (MyManagedStruct V_0)
  IL_0000:  newobj     ""cls1..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""MyManagedStruct cls1.y""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.s   123
  IL_0010:  call       ""void MyManagedStruct.mutate(int)""
  IL_0015:  dup
  IL_0016:  ldfld      ""MyManagedStruct cls1.y""
  IL_001b:  stloc.0
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldflda     ""MyManagedStruct.Nested MyManagedStruct.n""
  IL_0023:  ldflda     ""MyManagedStruct.Nested.Nested1 MyManagedStruct.Nested.n""
  IL_0028:  ldc.i4     0x1c8
  IL_002d:  call       ""void MyManagedStruct.Nested.Nested1.mutate(int)""
  IL_0032:  ldfld      ""MyManagedStruct cls1.y""
  IL_0037:  ldfld      ""MyManagedStruct.Nested MyManagedStruct.n""
  IL_003c:  ldfld      ""MyManagedStruct.Nested.Nested1 MyManagedStruct.Nested.n""
  IL_0041:  ldfld      ""int MyManagedStruct.Nested.Nested1.num""
  IL_0046:  call       ""void System.Console.WriteLine(int)""
  IL_004b:  ret
}
");

            comp = CompileAndVerify(source, expectedOutput: @"42", verify: Verification.Passes, parseOptions: TestOptions.Regular7_1);

            comp.VerifyIL("Program.Main",
@"
{
  // Code size       76 (0x4c)
  .maxstack  3
  .locals init (MyManagedStruct V_0)
  IL_0000:  newobj     ""cls1..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""MyManagedStruct cls1.y""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.s   123
  IL_0010:  call       ""void MyManagedStruct.mutate(int)""
  IL_0015:  dup
  IL_0016:  ldfld      ""MyManagedStruct cls1.y""
  IL_001b:  stloc.0
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldflda     ""MyManagedStruct.Nested MyManagedStruct.n""
  IL_0023:  ldflda     ""MyManagedStruct.Nested.Nested1 MyManagedStruct.Nested.n""
  IL_0028:  ldc.i4     0x1c8
  IL_002d:  call       ""void MyManagedStruct.Nested.Nested1.mutate(int)""
  IL_0032:  ldfld      ""MyManagedStruct cls1.y""
  IL_0037:  ldfld      ""MyManagedStruct.Nested MyManagedStruct.n""
  IL_003c:  ldfld      ""MyManagedStruct.Nested.Nested1 MyManagedStruct.Nested.n""
  IL_0041:  ldfld      ""int MyManagedStruct.Nested.Nested1.num""
  IL_0046:  call       ""void System.Console.WriteLine(int)""
  IL_004b:  ret
}
");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
        public void MutateReadonlyNested1()
        {
            string source = @"

    class Program
    {
        static void Main(string[] args)
        {
            GetRoRef().ro.ro.ro.ro.ToString();
            System.Console.Write(GetRoRef().ro.ro.ro.ro.x);
        }

        private static ref readonly Largest GetRoRef()
        {
            return ref (new Largest[1])[0];
        }
    }

    struct Largest
    {
        public int x;
        public readonly Large ro;
    }

    struct Large
    {
        public int x;
        public Medium ro;
    }

    struct Medium
    {
        public int x;
        public Small ro;
    }

    struct Small
    {
        public int x;
        public Smallest ro;
    }

    struct Smallest
    {
        public int x;

        public override string ToString()
        {
            x = -1;
            System.Console.Write(x);
            return null;
        }
    }";
            var comp = CompileAndVerify(source, expectedOutput: @"-10", verify: Verification.Fails);

            comp.VerifyIL("Program.Main",
@"
{
  // Code size       76 (0x4c)
  .maxstack  1
  .locals init (Smallest V_0)
  IL_0000:  call       ""ref readonly Largest Program.GetRoRef()""
  IL_0005:  ldflda     ""Large Largest.ro""
  IL_000a:  ldflda     ""Medium Large.ro""
  IL_000f:  ldflda     ""Small Medium.ro""
  IL_0014:  ldfld      ""Smallest Small.ro""
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  constrained. ""Smallest""
  IL_0022:  callvirt   ""string object.ToString()""
  IL_0027:  pop
  IL_0028:  call       ""ref readonly Largest Program.GetRoRef()""
  IL_002d:  ldflda     ""Large Largest.ro""
  IL_0032:  ldflda     ""Medium Large.ro""
  IL_0037:  ldflda     ""Small Medium.ro""
  IL_003c:  ldflda     ""Smallest Small.ro""
  IL_0041:  ldfld      ""int Smallest.x""
  IL_0046:  call       ""void System.Console.Write(int)""
  IL_004b:  ret
}
");

            comp = CompileAndVerify(source, expectedOutput: @"-10", verify: Verification.Passes, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());

            comp.VerifyIL("Program.Main",
@"
	{
	  // Code size       76 (0x4c)
	  .maxstack  1
	  .locals init (Large V_0)
	  IL_0000:  call       ""ref readonly Largest Program.GetRoRef()""
	  IL_0005:  ldfld      ""Large Largest.ro""
	  IL_000a:  stloc.0
	  IL_000b:  ldloca.s   V_0
	  IL_000d:  ldflda     ""Medium Large.ro""
	  IL_0012:  ldflda     ""Small Medium.ro""
	  IL_0017:  ldflda     ""Smallest Small.ro""
	  IL_001c:  constrained. ""Smallest""
	  IL_0022:  callvirt   ""string object.ToString()""
	  IL_0027:  pop
	  IL_0028:  call       ""ref readonly Largest Program.GetRoRef()""
	  IL_002d:  ldfld      ""Large Largest.ro""
	  IL_0032:  ldfld      ""Medium Large.ro""
	  IL_0037:  ldfld      ""Small Medium.ro""
	  IL_003c:  ldfld      ""Smallest Small.ro""
	  IL_0041:  ldfld      ""int Smallest.x""
	  IL_0046:  call       ""void System.Console.Write(int)""
	  IL_004b:  ret
	}
");
        }

        [Fact]
        public void LocalNumericConstToString()
        {
            string source = @"
using System;
class Program
{
    static void Main()
    {
        const SByte SB = 123;
        Console.Write(SB.ToString());
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"123");

            // Dev10
            compilation.VerifyIL("Program.Main",
@"{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (sbyte V_0)
  IL_0000:  ldc.i4.s   123
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""string sbyte.ToString()""
  IL_000a:  call       ""void System.Console.Write(string)""
  IL_000f:  ret
}");
        }

        [Fact]
        public void PassTypeParamFieldByRef()
        {
            string source = @"
using System;

static class Test
{
    class A
    {
        public object F;
    }

    class B<T>
    {
        public T F;
    }

    // Pass field on type T by ref.
    static void M1<T>(T o) where T : A
    {
        M(ref o.F);
    }

    // Pass field of type T by ref.
    static void M2<T>(B<T> o)
    {
        M(ref o.F);
    }

    static void M<T>(ref T arg)
    {
    }

    static void Main()
    {
        M1(new A());
        M2(new B<int>());
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"");

            // Dev10
            compilation.VerifyIL("Test.M1<T>(T)",
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  ldflda     ""object Test.A.F""
  IL_000b:  call       ""void Test.M<object>(ref object)""
  IL_0010:  ret
}");
        }

        [Fact]
        public void NotEqualIntegralAndFloat()
        {
            string source = @"
class Class1
{
    static void Main()
    {
        int i = -1;
        bool result = i != 0;

        if (result)
        {
            System.Console.WriteLine(""notequal1"");
        }

        result = i == 0;
        if (result)
        {
            System.Console.WriteLine(""equal1"");
        }


        double d = -1;
        result = d != 0;

        if (result)
        {
            System.Console.WriteLine(""notequal2"");
        }

        result = d == 0;
        if (result)
        {
            System.Console.WriteLine(""equal2"");
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"notequal1
notequal2");

            // Dev10
            compilation.VerifyIL("Class1.Main",
@"
{
  // Code size       92 (0x5c)
  .maxstack  3
  IL_0000:  ldc.i4.m1
  IL_0001:  dup
  IL_0002:  ldc.i4.0
  IL_0003:  cgt.un
  IL_0005:  brfalse.s  IL_0011
  IL_0007:  ldstr      ""notequal1""
  IL_000c:  call       ""void System.Console.WriteLine(string)""
  IL_0011:  ldc.i4.0
  IL_0012:  ceq
  IL_0014:  brfalse.s  IL_0020
  IL_0016:  ldstr      ""equal1""
  IL_001b:  call       ""void System.Console.WriteLine(string)""
  IL_0020:  ldc.r8     -1
  IL_0029:  dup
  IL_002a:  ldc.r8     0
  IL_0033:  ceq
  IL_0035:  ldc.i4.0
  IL_0036:  ceq
  IL_0038:  brfalse.s  IL_0044
  IL_003a:  ldstr      ""notequal2""
  IL_003f:  call       ""void System.Console.WriteLine(string)""
  IL_0044:  ldc.r8     0
  IL_004d:  ceq
  IL_004f:  brfalse.s  IL_005b
  IL_0051:  ldstr      ""equal2""
  IL_0056:  call       ""void System.Console.WriteLine(string)""
  IL_005b:  ret
}
");
        }

        [WorkItem(529267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529267")]
        [Fact]
        public void ModifyRangeVariable()
        {
            string source = @"
using System.Linq;

class C
{
    static void Main()
    {
        foreach (var e in from x in new int[2] select Goo(ref x))
        {
            System.Console.Write(e);
        }
    }

    static int Goo(ref int x)
    {
        return ++x;
    }
}
";
            // NOTE: this is a breaking change - dev10 builds and prints 11.
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (8,63): error CS1939: Cannot pass the range variable 'x' as an out or ref parameter
                //         foreach (var e in from x in new int[2] select Goo(ref x))
                Diagnostic(ErrorCode.ERR_QueryOutRefRangeVariable, "x").WithArguments("x"));
        }

        [Fact, WorkItem(529430, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529430")]
        public void DelegateEqualityComparison()
        {
            string source = @"
using System;

public class SinkHelper
{
    public Action action;
    public void OnEvent()
    {
        if (action == null)
            return;

        if (action != null)
            action();
    }
}
";
            CompileAndVerify(source).VerifyIL("SinkHelper.OnEvent", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""System.Action SinkHelper.action""
  IL_0006:  brtrue.s   IL_0009
  IL_0008:  ret
  IL_0009:  ldarg.0
  IL_000a:  ldfld      ""System.Action SinkHelper.action""
  IL_000f:  brfalse.s  IL_001c
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""System.Action SinkHelper.action""
  IL_0017:  callvirt   ""void System.Action.Invoke()""
  IL_001c:  ret
}");
        }

        [Fact]
        public void UnorthodoxBool()
        {
            var source = @"
using System;
 
class Class1
{
    static void Main()
    {
        byte[] x = { 0xFF };
        bool[] y = { true };
        Buffer.BlockCopy(x, 0, y, 0, 1);
 
        Console.WriteLine(y[0]);
        Console.Write(y[0] == true);
        Console.Write(y[0] == false);
        Console.Write(y[0] != false);
        Console.Write(y[0] != true);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"True
TrueFalseTrueFalse");

            compilation.VerifyIL("Class1.Main",
@"
{
  // Code size       83 (0x53)
  .maxstack  5
  .locals init (byte[] V_0, //x
  bool[] V_1) //y
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""byte""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4     0xff
  IL_000d:  stelem.i1
  IL_000e:  stloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  newarr     ""bool""
  IL_0015:  dup
  IL_0016:  ldc.i4.0
  IL_0017:  ldc.i4.1
  IL_0018:  stelem.i1
  IL_0019:  stloc.1
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.0
  IL_001c:  ldloc.1
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.1
  IL_001f:  call       ""void System.Buffer.BlockCopy(System.Array, int, System.Array, int, int)""
  IL_0024:  ldloc.1
  IL_0025:  ldc.i4.0
  IL_0026:  ldelem.u1
  IL_0027:  call       ""void System.Console.WriteLine(bool)""
  IL_002c:  ldloc.1
  IL_002d:  ldc.i4.0
  IL_002e:  ldelem.u1
  IL_002f:  call       ""void System.Console.Write(bool)""
  IL_0034:  ldloc.1
  IL_0035:  ldc.i4.0
  IL_0036:  ldelem.u1
  IL_0037:  ldc.i4.0
  IL_0038:  ceq
  IL_003a:  call       ""void System.Console.Write(bool)""
  IL_003f:  ldloc.1
  IL_0040:  ldc.i4.0
  IL_0041:  ldelem.u1
  IL_0042:  call       ""void System.Console.Write(bool)""
  IL_0047:  ldloc.1
  IL_0048:  ldc.i4.0
  IL_0049:  ldelem.u1
  IL_004a:  ldc.i4.0
  IL_004b:  ceq
  IL_004d:  call       ""void System.Console.Write(bool)""
  IL_0052:  ret
}
");
        }

        [WorkItem(529593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529593")]
        [Fact]
        public void FloatToDecimal01()
        {
            var source = @"
using System;

public class C
{
    static void Main()
    {
        decimal d1 = 1.712m;
        decimal d2 = (decimal)1.712f;
        decimal d3 = (decimal)1.712;
        decimal d4 = (decimal)(double)1.712f;
        Console.WriteLine(d1);
        Console.WriteLine(d2);
        Console.WriteLine(d3);
        Console.WriteLine(d4);
        Console.WriteLine(d1 == d2);
        Console.WriteLine(d2 == d3);
        Console.WriteLine(d3 == d4);
    }
}
";
            CompileAndVerify(source, expectedOutput:
@"1.712
1.712
1.712
1.71200001239777
True
True
False");
        }

        [WorkItem(529593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529593")]
        [Fact]
        public void FloatToDecimal02()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        Console.WriteLine((decimal)2147483648f);
        Console.WriteLine((decimal)2147483648d);
        Console.WriteLine((decimal)9.22337203685478e18f);
        Console.WriteLine((decimal)9.22337203685478e18d);
        Console.WriteLine((decimal)3.96140812571322e28f);
        Console.WriteLine((decimal)3.96140812571322e28d);
        Console.WriteLine((decimal)3.96140812663555e28f);
        Console.WriteLine((decimal)3.96140812663555e28d);
        Console.WriteLine((decimal)0.2147483648f);
        Console.WriteLine((decimal)0.2147483648d);
        Console.WriteLine((decimal)-0.0922337203685478f);
        Console.WriteLine((decimal)-0.0922337203685478d);
        Console.WriteLine((decimal)-3.96140812571322f);
        Console.WriteLine((decimal)-3.96140812571322d);
    }
}";
            CompileAndVerify(source, expectedOutput:
@"2147484000
2147483648
9223372000000000000
9223372036854780000
39614080000000000000000000000
39614081257132200000000000000
39614080000000000000000000000
39614081266355500000000000000
0.2147484
0.2147483648
-0.09223372
-0.0922337203685478
-3.961408
-3.96140812571322");
        }

        /// <summary>
        /// Dev11 reports CS0030 "Cannot convert type '...' to 'decimal' for
        /// float/double constants out of range, even in unchecked code.
        /// Roslyn reports CS0221 "Constant value '...' cannot be converted to a
        /// 'decimal' ..." in checked code only, and no error in unchecked code.
        /// </summary>
        [Fact]
        public void FloatToDecimal03()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        Console.WriteLine((decimal)-1e30d); // Dev11: CS0030
        Console.WriteLine((decimal)1e-30d);
        Console.WriteLine((decimal)-2e30f); // Dev11: CS0030
        Console.WriteLine((decimal)2e-30f);
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,27): error CS0031: Constant value '-1E+30' cannot be converted to a 'decimal'
                //         Console.WriteLine((decimal)-1e30d); // Dev11: CS0031
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "(decimal)-1e30d").WithArguments("-1E+30", "decimal"),
                // (8,27): error CS0031: Constant value '-2E+30' cannot be converted to a 'decimal'
                //         Console.WriteLine((decimal)-2e30f); // Dev11: CS0031
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "(decimal)-2e30f").WithArguments("-2E+30", "decimal"));
            source =
@"using System;
class C
{
    static void Main()
    {
        unchecked
        {
            Console.WriteLine((decimal)-3e30d); // Dev11: CS0030
            Console.WriteLine((decimal)3e-30d);
            Console.WriteLine((decimal)-4e30f); // Dev11: CS0030
            Console.WriteLine((decimal)4e-30f);
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,31): error CS0031: Constant value '-3E+30' cannot be converted to a 'decimal'
                //             Console.WriteLine((decimal)-3e30d); // Dev11: CS0030
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "(decimal)-3e30d").WithArguments("-3E+30", "decimal"),
                // (10,31): error CS0031: Constant value '-4E+30' cannot be converted to a 'decimal'
                //             Console.WriteLine((decimal)-4e30f); // Dev11: CS0030
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "(decimal)-4e30f").WithArguments("-4E+30", "decimal"));
        }

        [Fact]
        [WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")]
        public void Bug14064()
        {
            string source = @"
using System;
class C
{
    static void Main()
    {
        const int i = int.MaxValue;   // 2,147,483,647
        const float x = i; // rounded to 2,147,483,648
        float y = i;       // rounded to 2,147,483,648
        double z = i;                 // 2,147,483,647
        Test(x == y); // t
        Test(x == z); // f
        Test(y == z); // f
    }
    private static void Test(bool b) { Console.Write(b ? 't' : 'f'); }
}

";
            string expectedOutput = "tff";
            string expectedIL = @"{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (float V_0, //y
  double V_1) //z
  IL_0000:  ldc.r4     2.147484E+09
  IL_0005:  stloc.0
  IL_0006:  ldc.r8     2147483647
  IL_000f:  stloc.1
  IL_0010:  ldc.r4     2.147484E+09
  IL_0015:  ldloc.0
  IL_0016:  ceq
  IL_0018:  call       ""void C.Test(bool)""
  IL_001d:  ldc.r8     2147483648
  IL_0026:  ldloc.1
  IL_0027:  ceq
  IL_0029:  call       ""void C.Test(bool)""
  IL_002e:  ldloc.0
  IL_002f:  conv.r8
  IL_0030:  ldloc.1
  IL_0031:  ceq
  IL_0033:  call       ""void C.Test(bool)""
  IL_0038:  ret
}";

            var compilation = CompileAndVerify(source, expectedOutput: expectedOutput);

            compilation.VerifyIL("C.Main", expectedIL);
        }

        [WorkItem(545862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545862")]
        [Fact]
        public void TestPropertyAndTypeWithSameNameInDelegateInstantiation()
        {
            string source = @"
delegate void A();
class B
{
  public void M() { System.Console.WriteLine(123); }
}
class C
{
  B B { get { return new B(); } }
  A R()
  { 
    return new A(B.M);
  }
  static void Main() 
  { 
    (new C()).R()();
  }
}";
            string expectedOutput = "123";
            string expectedIL = @"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""B C.B.get""
  IL_0006:  ldftn      ""void B.M()""
  IL_000c:  newobj     ""A..ctor(object, System.IntPtr)""
  IL_0011:  ret
}";
            var compilation = CompileAndVerify(source, expectedOutput: expectedOutput);
            compilation.VerifyIL("C.R", expectedIL);
        }

        [WorkItem(545778, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545778")]
        [Fact]
        public void FormattingCharactersInName1()
        {
            var source = @"
enum " + "\u0915\u094d\u200d\u0937" + @"
{
    A
}
";
            System.Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("\u0915\u094d\u0937"); //formatting char removed
                Assert.True(type.CanBeReferencedByName);
            };

            CompileAndVerify(source, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [WorkItem(545778, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545778")]
        [Fact]
        public void FormattingCharactersInName2()
        {
            var il = @"
.class public auto ansi sealed E
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype E '" + "\u0915\u094d\u200d\u0937" + @"' = int32(0x00000000)
} // end of class E
";
            var comp = CreateCompilationWithILAndMscorlib40("", il);
            var @enum = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            var field = @enum.GetMembers().OfType<FieldSymbol>().Single();
            Assert.False(field.CanBeReferencedByName);
        }

        [WorkItem(545716, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545716")]
        [Fact]
        public void Regress14344()
        {
            string source = @"
class EdmFunction

{

private static void SetFunctionAttribute(ref FunctionAttributes field, FunctionAttributes attribute)

{

field ^= field & attribute;

}

}

enum FunctionAttributes : byte

{

None = 0,

Aggregate = 1,

}

";
            CompileAndVerify(source).VerifyIL("EdmFunction.SetFunctionAttribute", @"
{
  // Code size       10 (0xa)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.u1
  IL_0003:  ldarg.0
  IL_0004:  ldind.u1
  IL_0005:  ldarg.1
  IL_0006:  and
  IL_0007:  xor
  IL_0008:  stind.i1
  IL_0009:  ret
}
");
        }

        [Fact]
        public void SacrificialReadElement()
        {
            string source = @"

using System;

class Program
{
    static void Main()
    {
        var x = new Guid[10];

        if (x[1] is Guid)
        {
            Console.WriteLine(""hello"");
        }

        Test<Guid>(5);
    }

    private static void Test<T>(int i) where T:struct
    {
        T[] x = new T[10];

        if (x[i] is T)
        {
            Console.WriteLine(""hello"");
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: @"hello
hello").VerifyIL("Program.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldc.i4.s   10
  IL_0002:  newarr     ""System.Guid""
  IL_0007:  ldc.i4.1
  IL_0008:  ldelema    ""System.Guid""
  IL_000d:  pop
  IL_000e:  ldstr      ""hello""
  IL_0013:  call       ""void System.Console.WriteLine(string)""
  IL_0018:  ldc.i4.5
  IL_0019:  call       ""void Program.Test<System.Guid>(int)""
  IL_001e:  ret
}
            ").VerifyIL("Program.Test<T>", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldc.i4.s   10
  IL_0002:  newarr     ""T""
  IL_0007:  ldarg.0
  IL_0008:  readonly.
  IL_000a:  ldelema    ""T""
  IL_000f:  pop
  IL_0010:  ldstr      ""hello""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ret
}
            ");
        }

        [Fact]
        public void Regress15667()
        {
            string source = @"
class Program
{
static void Main(string[] args)
{
bool b;
int s = (b = false) ? 5 : 100; // Warning
int s1 = (b = false) ? 5 : 100; // Warning
System.Console.WriteLine(s1);
}
}
";
            string expectedOutput = "100";
            string expectedIL = @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.s   100
  IL_0002:  call       ""void System.Console.WriteLine(int)""
  IL_0007:  ret
}";

            var compilation = CompileAndVerify(source, expectedOutput: expectedOutput);

            compilation.VerifyIL("Program.Main", expectedIL);
        }

        [Fact(), WorkItem(546860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546860")]
        public void Bug17007()
        {
            CompileAndVerify(
@"
class Module1
{
    static void Main()
    {
	    System.Console.WriteLine(Test1(100));
    }

    static int Test1(int x)
    {
	    return Test2(x,checked(-x));
    }

    static int Test2(int x, int y)
    {   
	     return y;
    }
}", options: TestOptions.ReleaseExe,
expectedOutput: "-100");
        }

        /// <summary>
        /// Diagnostics from other methods should be ignored
        /// when compiling and emitting synthesized methods.
        /// </summary>
        [WorkItem(546867, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546867")]
        [Fact]
        public void IgnoreOtherDiagnosticsCompilingSynthesizedMethods()
        {
            var source =
@"class C
{
    static object F = new object(); // generate .cctor
    static System.Action M()
    {
        return () => { }; // generate lambda
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll.WithConcurrentBuild(false));
            var options = compilation.Options;
            var diagnostics = DiagnosticBag.GetInstance();

            var assembly = (SourceAssemblySymbol)compilation.Assembly;
            var module = new PEAssemblyBuilder(
                assembly,
                EmitOptions.Default,
                options.OutputKind,
                GetDefaultModulePropertiesForSerialization(),
                new ResourceDescription[0]);

            var methodBodyCompiler = new MethodCompiler(
                compilation: compilation,
                moduleBeingBuiltOpt: module,
                emittingPdb: false,
                emitTestCoverageData: false,
                hasDeclarationErrors: false,
                diagnostics: diagnostics,
                filterOpt: null,
                entryPointOpt: null,
                cancellationToken: CancellationToken.None);

            // Add diagnostic to MethodBodyCompiler bag, as if
            // code gen for an earlier method had generated an error.
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_IntDivByZero), NoLocation.Singleton));

            // Compile all methods for type including synthesized methods.
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            methodBodyCompiler.Visit(type);

            Assert.Equal(1, diagnostics.AsEnumerable().Count());
            diagnostics.Free();
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(546957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546957")]
        public void Bug17352_VarArgCtor()
        {
            string source = @"
using System;
class A
{
    public static void Main()
    {
        TestVarArgs instTestVarArgs = new TestVarArgs(__arglist());
        TestVarArgs instTestVarArgs1 = new TestVarArgs(__arglist(2, ""blah"", null, 2.2));
    }

    public class TestVarArgs
    {
        public TestVarArgs(__arglist)
        {
            Console.WriteLine(""Inside - TestVarArgs::ctor (__arglist)"");
            PrintArgList(new ArgIterator(__arglist));
        }

        private void PrintArgList(ArgIterator args)
        {
            int argCount = args.GetRemainingCount();
            Object[] objArgs = new Object[argCount];

            //Walk all of the args in the variable part of the argument list.
            for (int i = 1; args.GetRemainingCount() > 0 && i < argCount; i++)
            {
                TypedReference tr = args.GetNextArg();
                objArgs[i] = TypedReference.ToObject(tr);
                if (objArgs[i] != null)
                {
                    Console.WriteLine(objArgs[i].GetType());
                    Console.WriteLine(objArgs[i]);
                }
            }
        }
    }

}
";
            var compilation = CompileAndVerify(
                source,
                expectedOutput: @"Inside - TestVarArgs::ctor (__arglist)
Inside - TestVarArgs::ctor (__arglist)
System.Int32
2
System.String
blah");

            compilation.VerifyIL("A.Main",
@"
{
  // Code size       29 (0x1d)
  .maxstack  4
  IL_0000:  newobj     ""A.TestVarArgs..ctor(__arglist)""
  IL_0005:  pop
  IL_0006:  ldc.i4.2
  IL_0007:  ldstr      ""blah""
  IL_000c:  ldnull
  IL_000d:  ldc.r8     2.2
  IL_0016:  newobj     ""A.TestVarArgs..ctor(__arglist) with __arglist( int, string, object, double)""
  IL_001b:  pop
  IL_001c:  ret
}
");
        }

        [WorkItem(24348, "https://github.com/dotnet/roslyn/issues/24348")]
        [ConditionalFact(typeof(DesktopOnly))]
        public void VarargBridgeSource()
        {

            var code = @"
public class VarArgs
{
    public void Invoke(__arglist)
    {
        var ai = new System.ArgIterator(__arglist);
        System.Console.WriteLine(ai.GetRemainingCount());
    }
}

interface IVarArgs
{
    void Invoke(__arglist);
}

class MyVarArgs : VarArgs, IVarArgs
{

}

public static class P
{
    public static void Main()
    {
        IVarArgs iv = new MyVarArgs();

        iv.Invoke(__arglist(1, 2, 3, 4));
    }
}
";

            var compilation = CompileAndVerifyWithMscorlib40(code, expectedOutput: "4");
        }

        [WorkItem(26113, "https://github.com/dotnet/roslyn/issues/26113")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        public void VarargByRef()
        {
            var code = @"
using System;
class A
{
    static void Test(__arglist)
    {
        var args = new ArgIterator(__arglist);
        ref int a = ref __refvalue(args.GetNextArg(), int);
        a = 5;
    }
    static void Main()
    {
        int a = 0;
        Test(__arglist(ref a));
        Console.WriteLine(a);
    }
}
";
            var comp = CompileAndVerify(code, expectedOutput: "5", options: TestOptions.DebugExe);
            comp.VerifyIL("A.Main",
@"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (int V_0) //a
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""void A.Test(__arglist) with __arglist( ref int)""
  IL_000a:  nop
  IL_000b:  ldloc.0
  IL_000c:  call       ""void System.Console.WriteLine(int)""
  IL_0011:  nop
  IL_0012:  ret
}
");

            comp = CompileAndVerify(code, expectedOutput: "5", options: TestOptions.ReleaseExe);
            comp.VerifyIL("A.Main",
@"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int V_0) //a
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""void A.Test(__arglist) with __arglist( ref int)""
  IL_0009:  ldloc.0
  IL_000a:  call       ""void System.Console.WriteLine(int)""
  IL_000f:  ret
}
");
        }

        [WorkItem(24348, "https://github.com/dotnet/roslyn/issues/24348")]
        [ConditionalFact(typeof(DesktopOnly))]
        public void VarargBridgeMeta()
        {
            var reference = CreateCompilation(@"
public class VarArgs
{
    public void Invoke(__arglist)
    {
        var ai = new System.ArgIterator(__arglist);
        System.Console.WriteLine(ai.GetRemainingCount());
    }
}");

            var code = @"
interface IVarArgs
{
    void Invoke(__arglist);
}

class MyVarArgs : VarArgs, IVarArgs
{

}

public static class P
{
    public static void Main()
    {
        IVarArgs iv = new MyVarArgs();

        iv.Invoke(__arglist(1, 2, 3, 4));
    }
}
";

            var comp = CreateCompilation(code, references: new[] { reference.ToMetadataReference() });
            comp.VerifyDiagnostics(
                // (7,28): error CS0630: 'VarArgs.Invoke(__arglist)' cannot implement interface member 'IVarArgs.Invoke(__arglist)' in type 'MyVarArgs' because it has an __arglist parameter.
                // class MyVarArgs : VarArgs, IVarArgs
                Diagnostic(ErrorCode.ERR_InterfaceImplementedImplicitlyByVariadic, "IVarArgs").WithArguments("VarArgs.Invoke(__arglist)", "IVarArgs.Invoke(__arglist)", "MyVarArgs").WithLocation(7, 28)
                );

            comp = CreateCompilation(code, references: new[] { reference.EmitToImageReference() });
            comp.VerifyDiagnostics(
                // (7,28): error CS0630: 'VarArgs.Invoke(__arglist)' cannot implement interface member 'IVarArgs.Invoke(__arglist)' in type 'MyVarArgs' because it has an __arglist parameter.
                // class MyVarArgs : VarArgs, IVarArgs
                Diagnostic(ErrorCode.ERR_InterfaceImplementedImplicitlyByVariadic, "IVarArgs").WithArguments("VarArgs.Invoke(__arglist)", "IVarArgs.Invoke(__arglist)", "MyVarArgs").WithLocation(7, 28)
                );
        }

        [WorkItem(24348, "https://github.com/dotnet/roslyn/issues/24348")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void VarargBridgeSourceModopt()
        {

            var il = @"
.class interface public auto ansi abstract IVarArgs
{
	// Methods
	.method public hidebysig newslot abstract virtual 
		instance vararg int32 modopt(int64) Invoke () cil managed 
	{
	} // end of method IVarArgs::Invoke

} // end of class IVarArgs


";

            var reference = CompileIL(il);

            var code = @"

public class VarArgs
{
    public int Invoke(__arglist) => throw null;
}

class MyVarArgs : VarArgs, IVarArgs
{

}

class MyVarArgs2 : IVarArgs
{
    public int Invoke(__arglist) => throw null;
}

class MyVarArgs3 : IVarArgs
{
    // this is ok, modifiers are copied
    int IVarArgs.Invoke(__arglist) => throw null;
}

public static class P
{
    public static void Main()
    {
        IVarArgs iv = new MyVarArgs3();

        iv.Invoke(__arglist(1, 2, 3, 4));
    }
}
";

            var comp = CreateCompilation(code, references: new[] { reference });
            comp.VerifyDiagnostics(
                // (15,16): error CS0630: 'MyVarArgs2.Invoke(__arglist)' cannot implement interface member 'IVarArgs.Invoke(__arglist)' in type 'MyVarArgs2' because it has an __arglist parameter
                //     public int Invoke(__arglist) => throw null;
                Diagnostic(ErrorCode.ERR_InterfaceImplementedImplicitlyByVariadic, "Invoke").WithArguments("MyVarArgs2.Invoke(__arglist)", "IVarArgs.Invoke(__arglist)", "MyVarArgs2").WithLocation(15, 16),
                // (8,28): error CS0630: 'VarArgs.Invoke(__arglist)' cannot implement interface member 'IVarArgs.Invoke(__arglist)' in type 'MyVarArgs' because it has an __arglist parameter
                // class MyVarArgs : VarArgs, IVarArgs
                Diagnostic(ErrorCode.ERR_InterfaceImplementedImplicitlyByVariadic, "IVarArgs").WithArguments("VarArgs.Invoke(__arglist)", "IVarArgs.Invoke(__arglist)", "MyVarArgs").WithLocation(8, 28)
                );
        }

        [WorkItem(530067, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530067")]
        [Fact]
        public void NopAfterCall()
        {
            // For a nop to be inserted after a call, three conditions must be met:
            //   1) void return
            //   2) debug build

            var source = @"
class C
{
    static void Main()
    {
        Void();
        NonVoid();
    }

    static void Void() { }
    static int NonVoid() { return 1; }
}
";

            var compRelease = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var compDebug = CreateCompilation(source, options: TestOptions.DebugExe);

            // (2) is not met.
            CompileAndVerify(compRelease).VerifyIL("C.Main", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  call       ""void C.Void()""
  IL_0005:  call       ""int C.NonVoid()""
  IL_000a:  pop
  IL_000b:  ret
}");

            // Void meets (1), but NonVoid does not (it doesn't need a nop since it has a pop).
            CompileAndVerify(compDebug).VerifyIL("C.Main", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  call       ""void C.Void()""
  IL_0006:  nop
  IL_0007:  call       ""int C.NonVoid()""
  IL_000c:  pop
  IL_000d:  ret
}");
        }

        [WorkItem(530067, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530067")]
        [Fact]
        public void NopAfterCall_ForLoop()
        {
            var source = @"
class C
{
    static void Main()
    {
        for (int i = 0; i < 10; System.Diagnostics.Debugger.Break())
        {
        }
    }
}
";
            // Nop after Debugger.Break(), even though it isn't at the end of a statement.
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            var v = CompileAndVerify(comp);

            v.VerifyIL("C.Main", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (int V_0, //i
                bool V_1)
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
  IL_0003:  br.s       IL_000d
  IL_0005:  nop
  IL_0006:  nop
  IL_0007:  call       ""void System.Diagnostics.Debugger.Break()""
  IL_000c:  nop
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.s   10
  IL_0010:  clt
  IL_0012:  stloc.1
  IL_0013:  ldloc.1
  IL_0014:  brtrue.s   IL_0005
  IL_0016:  ret
}");
        }

        [Fact]
        public void CallOnFildInStruct()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
        {
            S1 s = new S1(new C1());
            Console.Write(s.Prop1);
        }
}

struct S1
{
    public readonly C1 c;

    public S1(C1 c)
    {
        this.c = c;
    }

    public int Prop1
    {
        get
        {
            return c.s.Length;
        }
    }
}

public class C1
{
    public string s = ""hi"";
}
";
            CompileAndVerify(source, expectedOutput: "2").VerifyIL("S1.Prop1.get", @"            
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C1 S1.c""
  IL_0006:  ldfld      ""string C1.s""
  IL_000b:  callvirt   ""int string.Length.get""
  IL_0010:  ret
}
");
        }


        [Fact]
        public void ReferenceEqualsIntrinsic()
        {
            var source = @"
using System;

public class Program
{
    static void Main()
    {
        object a = ""hi"";
        Exception b = null;

        // brtrue/brinst equivalence
        //   if (a)
        if (!ReferenceEquals(a, null))
        {
            Console.Write('1');
        }

        // trivial Eq
        //   unconditional
        if (ReferenceEquals(null, null))
        {
            Console.Write('2');
        }

        // demorgan's
        //   if (a || b)
        if (!(ReferenceEquals(a, null) && ReferenceEquals(b, null)))
        {
            Console.Write('3');
        }

        // conversions
        if (!(ReferenceEquals(1, (int?)null)))
        {
            Console.Write('4');
        }
    }
}
";

            CompileAndVerify(source, expectedOutput: "1234").VerifyIL("Program.Main", @"
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (System.Exception V_0) //b
  IL_0000:  ldstr      ""hi""
  IL_0005:  ldnull
  IL_0006:  stloc.0
  IL_0007:  dup
  IL_0008:  brfalse.s  IL_0011
  IL_000a:  ldc.i4.s   49
  IL_000c:  call       ""void System.Console.Write(char)""
  IL_0011:  ldc.i4.s   50
  IL_0013:  call       ""void System.Console.Write(char)""
  IL_0018:  brtrue.s   IL_001d
  IL_001a:  ldloc.0
  IL_001b:  brfalse.s  IL_0024
  IL_001d:  ldc.i4.s   51
  IL_001f:  call       ""void System.Console.Write(char)""
  IL_0024:  ldc.i4.1
  IL_0025:  box        ""int""
  IL_002a:  brfalse.s  IL_0033
  IL_002c:  ldc.i4.s   52
  IL_002e:  call       ""void System.Console.Write(char)""
  IL_0033:  ret
}");
        }

        [WorkItem(598029, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598029")]
        [Fact]
        public void TypeParameterInterfaceVersusNonInterface1()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        Test(new D());
    }

    static void Test<U>(U u) where U : C, IGoo
    {
        Console.WriteLine(u.ToString());
    }
}

interface IGoo
{
    string ToString();
}

class C
{
    public override string ToString()
    {
        return ""C"";
    }
}

class D : C, IGoo
{
    public override string ToString()
    {
        return ""D"";
    }

    string IGoo.ToString()
    {
        return ""IGoo"";
    }
}
";

            // We will have IGoo.ToString and C.ToString (which is an override of object.ToString)
            // in the candidate set. Does the rule apply to eliminate all interface methods?  NO.  The
            // rule only applies if the candidate set contains a method which originally came from a
            // class type other than object. The method C.ToString is the "slot" for
            // object.ToString, so this counts as coming from object.  M should call the explicit
            // interface implementation.
            CompileAndVerify(source, expectedOutput: "IGoo");
        }

        // Same as above, but C.ToString is "new virtual", rather than "override".
        [WorkItem(598029, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598029")]
        [Fact]
        public void TypeParameterInterfaceVersusNonInterface2()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        Test(new D());
    }

    static void Test<U>(U u) where U : C, IGoo
    {
        Console.WriteLine(u.ToString());
    }
}

interface IGoo
{
    string ToString();
}

class C
{
    public new virtual string ToString()
    {
        return ""C"";
    }
}

class D : C, IGoo
{
    public override string ToString()
    {
        return ""D"";
    }

    string IGoo.ToString()
    {
        return ""IGoo"";
    }
}
";

            // The candidate set contains a method ToString which comes from a class type other
            // than object. The interface method should be eliminated and M should call virtual
            // method C.ToString().
            CompileAndVerify(source, expectedOutput: "D");
        }

        [WorkItem(638119, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638119")]
        [Fact]
        public void ArrayInitZero()
        {
            string source = @"
    using System;
    class Test
    {
        static void Main()
        {
            System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            try
            {
                Run();
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
            }
        }

        static void Run()
        {
            // no element inits
            bool[] arrB1 = new bool[] {false, false, false};
            System.Console.WriteLine(arrB1[0]);

            // no element inits
            Exception[] arrE1 = new Exception[] {null, null, null};
            System.Console.WriteLine(arrE1[0]);

            // 1 element init
            bool[] arrB2 = new bool[] {false, true, false};
            System.Console.WriteLine(arrB2[1]);

            // 1 element init
            Exception[] arrE2 = new Exception[] {null, new Exception(), null};
            System.Console.WriteLine(arrE2[1]);

            // blob init
            bool[] arrB3 = new bool[] {true, false, true, true};
            System.Console.WriteLine(arrB3[2]);
        }
    }
";
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe.WithModuleName("MODULE"), expectedOutput: @"False

True
System.Exception: Exception of type 'System.Exception' was thrown.
True");

            compilation.VerifyIL("Test.Run",
@"{
  // Code size       89 (0x59)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""bool""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelem.u1
  IL_0008:  call       ""void System.Console.WriteLine(bool)""
  IL_000d:  ldc.i4.3
  IL_000e:  newarr     ""System.Exception""
  IL_0013:  ldc.i4.0
  IL_0014:  ldelem.ref
  IL_0015:  call       ""void System.Console.WriteLine(object)""
  IL_001a:  ldc.i4.3
  IL_001b:  newarr     ""bool""
  IL_0020:  dup
  IL_0021:  ldc.i4.1
  IL_0022:  ldc.i4.1
  IL_0023:  stelem.i1
  IL_0024:  ldc.i4.1
  IL_0025:  ldelem.u1
  IL_0026:  call       ""void System.Console.WriteLine(bool)""
  IL_002b:  ldc.i4.3
  IL_002c:  newarr     ""System.Exception""
  IL_0031:  dup
  IL_0032:  ldc.i4.1
  IL_0033:  newobj     ""System.Exception..ctor()""
  IL_0038:  stelem.ref
  IL_0039:  ldc.i4.1
  IL_003a:  ldelem.ref
  IL_003b:  call       ""void System.Console.WriteLine(object)""
  IL_0040:  ldc.i4.4
  IL_0041:  newarr     ""bool""
  IL_0046:  dup
  IL_0047:  ldtoken    ""int <PrivateImplementationDetails>.35CCB1599F52363510686EF38B7DB5E7998DB108""
  IL_004c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0051:  ldc.i4.2
  IL_0052:  ldelem.u1
  IL_0053:  call       ""void System.Console.WriteLine(bool)""
  IL_0058:  ret
}
");
        }

        [WorkItem(1043494, "DevDiv")]
        [Fact]
        public void NegativeZeroIsNotAZero()
        {
            var source = @"
class A
{
    static int Main()
    {
        double[] dArr = { -0.0 };
        double d = -0.0;
        // Invariant: x / -0 => -infinity
        if (System.Double.IsNegativeInfinity(2.0 / dArr[0]) != System.Double.IsNegativeInfinity(2.0 / d))
        {
            System.Console.WriteLine(""Failed test at test 1"");
            return 1;
        }
        if (System.Double.IsNegativeInfinity(2.0 / new double[] { -0.0 }[0]) != System.Double.IsNegativeInfinity(2.0 / d))
        {
            System.Console.WriteLine(""Failed test at test 2"");
            return 1;
        }
        // all tests pass
        return 0;
    }
}
";

            CompileAndVerify(source, expectedOutput: @"").VerifyIL("A.Main",
@"
{
  // Code size      144 (0x90)
  .maxstack  5
  .locals init (double[] V_0, //dArr
  double V_1) //d
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""double""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.r8     -0.0
  IL_0011:  stelem.r8
  IL_0012:  stloc.0
  IL_0013:  ldc.r8     -0.0
  IL_001c:  stloc.1
  IL_001d:  ldc.r8     2
  IL_0026:  ldloc.0
  IL_0027:  ldc.i4.0
  IL_0028:  ldelem.r8
  IL_0029:  div
  IL_002a:  call       ""bool double.IsNegativeInfinity(double)""
  IL_002f:  ldc.r8     2
  IL_0038:  ldloc.1
  IL_0039:  div
  IL_003a:  call       ""bool double.IsNegativeInfinity(double)""
  IL_003f:  beq.s      IL_004d
  IL_0041:  ldstr      ""Failed test at test 1""
  IL_0046:  call       ""void System.Console.WriteLine(string)""
  IL_004b:  ldc.i4.1
  IL_004c:  ret
  IL_004d:  ldc.r8     2
  IL_0056:  ldc.i4.1
  IL_0057:  newarr     ""double""
  IL_005c:  dup
  IL_005d:  ldc.i4.0
  IL_005e:  ldc.r8     -0.0
  IL_0067:  stelem.r8
  IL_0068:  ldc.i4.0
  IL_0069:  ldelem.r8
  IL_006a:  div
  IL_006b:  call       ""bool double.IsNegativeInfinity(double)""
  IL_0070:  ldc.r8     2
  IL_0079:  ldloc.1
  IL_007a:  div
  IL_007b:  call       ""bool double.IsNegativeInfinity(double)""
  IL_0080:  beq.s      IL_008e
  IL_0082:  ldstr      ""Failed test at test 2""
  IL_0087:  call       ""void System.Console.WriteLine(string)""
  IL_008c:  ldc.i4.1
  IL_008d:  ret
  IL_008e:  ldc.i4.0
  IL_008f:  ret
}
");
        }

        [WorkItem(1043494, "DevDiv")]
        [Fact]
        public void NegativeZeroIsNotAZero1()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new double[] { -0.0, -0.0, -0.0, -0.0, -0.0, };
        var y = new float[] { -0.0f, -0.0f, -0.0f, -0.0f, -0.0f, };
        var z = new decimal[] { -(decimal.Zero), -(decimal.Zero), -(decimal.Zero), -(decimal.Zero), -(decimal.Zero), };

        // false
        Console.WriteLine(IsZero(x[0]));

        // false
        Console.WriteLine(IsZero(y[0]));

        // true (I do not know how to get '-0' constant)
        Console.WriteLine(IsZero(z[0]));

        x = new double[] { -0.0 };
        y = new float[] { -0.0f };
        z = new decimal[] { -(decimal.Zero) };

        // false
        Console.WriteLine(IsZero(x[0]));

        // false
        Console.WriteLine(IsZero(y[0]));

        // true (I do not know how to get '-0' constant)
        Console.WriteLine(IsZero(z[0]));
    }


    private static bool IsZero(double x)
    {
        return x == 0 && 1 / x > 0;
    }

    private static bool IsZero(float x)
    {
        return x == 0 && 1 / x > 0;
    }

    private static bool IsZero(decimal x)
    {
        var bits = decimal.GetBits(x);
        foreach (int part in bits)
        {
            if (part != 0)
            {
                return false;
            }
        }

        return true;
    }

}
";

            CompileAndVerify(source, expectedOutput: @"False
False
True
False
False
True");
        }

        [Fact, WorkItem(649805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649805")]
        public void Repro649805()
        {
            var source = @"
public class Goo
{
    public static void Method(string s)
    {
        if (s == ""abc"") Test.Result++;
    }
    public string Prop { get; set; }
}
public class Test
{
    public Goo Goo { get; set; }
    public static void DoExample(dynamic d)
    {
        Goo.Method(d.Prop);
    }

    public static int Result = -1;
    static void Main()
    {
        try
        {
            DoExample(new Goo() { Prop = ""abc"" });
        }
        catch (System.Exception)
        {
            Test.Result--;
        }
        System.Console.WriteLine(Test.Result);
    }
}
";

            CompileAndVerifyWithMscorlib40(source, references: new[] { SystemCoreRef, CSharpRef }, expectedOutput: @"0");
        }


        [WorkItem(653588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/653588")]
        [Fact]
        public void SelfAssignStructCallTarget()
        {
            var source = @"
    struct S1
    {
        public int field;

        public S1(int v)
        {
            this.field = v;
        }

        public void Goo()
        {
            System.Console.WriteLine(field.ToString());
        }
    }

    class A
    {
        static void Main(string[] args)
        {
            S1 s = new S1();
            (s = s).Goo();

            S1 s1 = new S1(42);
            (s1 = s1).Goo();
        }
    }
";

            CompileAndVerify(source, expectedOutput: @"0
42").VerifyIL("A.Main", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (S1 V_0, //s
  S1 V_1, //s1
  S1 V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S1""
  IL_0008:  ldloc.0
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  stloc.2
  IL_000c:  ldloca.s   V_2
  IL_000e:  call       ""void S1.Goo()""
  IL_0013:  ldloca.s   V_1
  IL_0015:  ldc.i4.s   42
  IL_0017:  call       ""S1..ctor(int)""
  IL_001c:  ldloc.1
  IL_001d:  dup
  IL_001e:  stloc.1
  IL_001f:  stloc.2
  IL_0020:  ldloca.s   V_2
  IL_0022:  call       ""void S1.Goo()""
  IL_0027:  ret
}                                                                                                                 
");
        }

        [WorkItem(653588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/653588")]
        [Fact]
        public void UnusedStructFieldLoad()
        {
            var source = @"
struct S1
{
    public int field;

    public S1(int v)
    {
        this.field = v;
    }

    public void Goo()
    {
        System.Console.WriteLine(field.ToString());
    }
}

class A
{
    static void Main(string[] args)
    {
        var x = (new S1()).field;
        var y = (new S1(42)).field;
    }
}
";

            CompileAndVerify(source, expectedOutput: @"").VerifyIL("A.Main",
@"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldc.i4.s   42
  IL_0002:  newobj     ""S1..ctor(int)""
  IL_0007:  pop
  IL_0008:  ret
}                                                                                                         
");
        }

        [WorkItem(665317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665317")]
        [Fact]
        public void InitGenericElement()
        {
            var source = @"
using System;
class A { }
class B : A { }
class Program
{
    static void Goo<T>(T[] array) where T : class
    {
        array[0] = null;
    }

    static void Goo1<T>(T[] array) where T : struct
    {
        array[0] = default(T);
    }

    static void Bar<T>(T[] array)
    {
        array[0] = default(T);
    }

    static void Baz<T>(T[][] array)
    {
        array[0] = default(T[]);
    }

    static void Main(string[] args)
    {
        A[] array = new B[5];
        Goo<A>(array);
        Bar<A>(array);

        A[][] array1 = new B[5][];
        Baz<A>(array1);
    }
}
";

            CompileAndVerify(source, expectedOutput: @""
).VerifyIL("Program.Goo<T>(T[])",
@"
{
  // Code size       17 (0x11)
  .maxstack  3
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  initobj    ""T""
  IL_000a:  ldloc.0
  IL_000b:  stelem     ""T""
  IL_0010:  ret
}                                                                                                    
").VerifyIL("Program.Goo1<T>(T[])",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    ""T""
  IL_0007:  initobj    ""T""
  IL_000d:  ret
}                                                                                                     
").VerifyIL("Program.Bar<T>(T[])",
@"
{
  // Code size       17 (0x11)
  .maxstack  3
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  initobj    ""T""
  IL_000a:  ldloc.0
  IL_000b:  stelem     ""T""
  IL_0010:  ret
}                                                                                                       
").VerifyIL("Program.Baz<T>(T[][])",
@"
{
  // Code size        5 (0x5)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldnull
  IL_0003:  stelem.ref
  IL_0004:  ret
}                                                                                                       
");
        }

        [Fact]
        public void InitGenericVolatileField()
        {
            var source = @"
using System;
class A<T> where T: class
{ 
   public T field;
   public volatile T vField;
}

class Program
{
    static void Goo<T>(A<T> v) where T : class
    {
        v.field = null;
        v.field = default(T);

        v.vField = null;
        v.vField = default(T);
    }

    static void Main(string[] args)
    {
    }
}
";

            CompileAndVerify(source, expectedOutput: @""
).VerifyIL("Program.Goo<T>(A<T>)",
@"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""T A<T>.field""
  IL_0006:  initobj    ""T""
  IL_000c:  ldarg.0
  IL_000d:  ldflda     ""T A<T>.field""
  IL_0012:  initobj    ""T""
  IL_0018:  ldarg.0
  IL_0019:  ldloca.s   V_0
  IL_001b:  initobj    ""T""
  IL_0021:  ldloc.0
  IL_0022:  volatile.
  IL_0024:  stfld      ""T A<T>.vField""
  IL_0029:  ldarg.0
  IL_002a:  ldloca.s   V_0
  IL_002c:  initobj    ""T""
  IL_0032:  ldloc.0
  IL_0033:  volatile.
  IL_0035:  stfld      ""T A<T>.vField""
  IL_003a:  ret
}                                                                                                    
");
        }

        [Fact]
        public void CallFinalMethodOnTypeParam()
        {
            var source = @"
using System;


class Program
{
    static void Main(string[] args)
    {
        Test1(new cls1());
        Test1(new cls2());

        Test2(new cls2());
    }

    static void Test1<T>(T arg) where T : cls1
    {
        arg.Goo();
    }

    static void Test2<T>(T arg) where T : cls2
    {
        arg.Goo();
    }
}

interface i1
{
    void Goo();
}

class cls1 : i1
{
    public void Goo()
    {
        System.Console.Write(""Goo"");
    }
    }

    class cls2 : cls1
    {
    }

";

            CompileAndVerify(source, expectedOutput: @"GooGooGoo").
                VerifyIL("Program.Test1<T>(T)",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  callvirt   ""void cls1.Goo()""
  IL_000b:  ret
}                                                                                                         
").
                VerifyIL("Program.Test2<T>(T)",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  callvirt   ""void cls1.Goo()""
  IL_000b:  ret
}                                                                                                        
");
        }

        [WorkItem(797996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797996")]
        [Fact]
        public void MissingMember_System_String__op_Equality()
        {
            var text =
@"namespace System
{
    public class Object { }
    public struct Void { }
    public class ValueType { }
    public struct Int32 { }
    public struct Nullable<T> { }
    public class String { }
}
class C
{
    static void M(string s)
    {
        switch (s) { case ""A"": break; case ""B"": break; }
    }
}";
            var compilation = CreateEmptyCompilation(text);
            compilation.VerifyDiagnostics();
            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);
                result.Diagnostics.Verify(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (14,22): error CS0656: Missing compiler required member 'System.String.op_Equality'
                //         switch (s) { case "A": break; case "B": break; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"case ""A"":").WithArguments("System.String", "op_Equality").WithLocation(14, 22)
                );
            }
        }

        [WorkItem(797996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797996")]
        [Fact]
        public void MissingMember_System_Type__GetTypeFromHandle()
        {
            var text =
@"namespace System
{
    public class Object { }
    public struct Void { }
    public class ValueType { }
    public struct Int32 { }
    public struct Nullable<T> { }
    public class Type { }
    public class TypedReference { }
}
class C
{
    static object F = typeof(C);
}";
            var compilation = CreateEmptyCompilation(text);
            compilation.VerifyDiagnostics();
            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);
                result.Diagnostics.Verify(
                    // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                    Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion),
                    // (13,23): error CS0656: Missing compiler required member 'System.Type.GetTypeFromHandle'
                    //     static object F = typeof(C);
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "typeof(C)").WithArguments("System.Type", "GetTypeFromHandle").WithLocation(13, 23));
            }
        }

        [WorkItem(797996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797996")]
        [Fact]
        public void MissingMember_System_Type__GetTypeFromHandle_2()
        {
            var text =
@"namespace System
{
    public class Object { }
    public struct Void { }
    public class ValueType { }
    public struct Int32 { }
    public struct Nullable<T> { }
    public class Type { }
    public class TypedReference { }
}
class C
{
    static object F(object o)
    {
        return __reftype(__makeref(o));
    }
}";
            var compilation = CreateEmptyCompilation(text);
            compilation.VerifyDiagnostics();
            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);
                result.Diagnostics.Verify(
                    // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                    Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion),
                    // (15,16): error CS0656: Missing compiler required member 'System.Type.GetTypeFromHandle'
                    //         return __reftype(__makeref(o));
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "__reftype(__makeref(o))").WithArguments("System.Type", "GetTypeFromHandle").WithLocation(15, 16));
            }

            // Similar to above but with mscorlib.
            text =
@"class C
{
    static object F(object o)
    {
        return __reftype(__makeref(o));
    }
}";
            compilation = CreateCompilation(text);
            compilation.VerifyDiagnostics();
            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);
                Assert.True(result.Success);
                result.Diagnostics.Verify();
            }
        }

        [Fact]
        public void Regress530041()
        {
            var source = @"
using System;

    public static class Test
    {
        public static void Main()
        {
            if (SomeExpression()) return;
        }
        private static bool SomeExpression()
        {
            return true;
        }
    }

";

            CompileAndVerify(source, expectedOutput: @""
).VerifyIL("Test.Main()",
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  call       ""bool Test.SomeExpression()""
  IL_0005:  pop
  IL_0006:  ret
}                                                                                                
");
        }

        [Fact]
        public void Regress530041_1()
        {
            var source = @"
using System;

    public static class Test
    {
        public static void Main()
        {
            if (SomeExpression())
            {
                    System.Console.WriteLine(""hello"");
                    return;
            }
            else
            {
                    System.Console.WriteLine(""hello"");
                    return;
            }
        }

        private static bool SomeExpression()
        {
            return true;
        }
    }

";

            CompileAndVerify(source, expectedOutput: @"hello"
).VerifyIL("Test.Main()",
@"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  call       ""bool Test.SomeExpression()""
  IL_0005:  pop
  IL_0006:  ldstr      ""hello""
  IL_000b:  call       ""void System.Console.WriteLine(string)""
  IL_0010:  ret
}                                                                                           
");
        }

        [WorkItem(530049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530049")]
        [Fact]
        public void Regress530049()
        {
            var source = @"
enum MyEnum { first, second, last }
struct MyStruct { int intStructMember; }
public class Test
{
    static bool boolMember = false;
    static char charMember = '\0';
    static sbyte sbyteMember = 0;
    static byte byteMember = 0;
    static short shortMember = 0;
    static ushort ushortMember = 0;
    static int intMember = 0;
    static uint uintMember = 0;
    static long longMember = 0L;
    static ulong ulongMember = 0;
    static decimal decimalMember = default(decimal);
    static string strMember = null;
    static object objMember = null;
    static float floatMember = 0.0F;
    static double doubleMember = 0.0D;
    static MyEnum enumMember = MyEnum.first;
    MyStruct structMember = default(MyStruct);
}

class c1
{
    public static void Main()
    {
    }
}

";

            CompileAndVerify(source, expectedOutput: @"").
                VerifyIL("Test..cctor()",
@"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}                                                                                           
"); ;
        }

        [WorkItem(876784, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/876784")]
        [Fact]
        public void Repro876784()
        {
            var source = @"
public delegate void D();

public class A
{
    static D Test()
    {
        return M1(() => { }).M2;
    }

    static A M1(D d) { return null; }
}

public static class AExtensions
{
    public static void M2(this A a) { }
}
";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyEmitDiagnostics();
        }

        [WorkItem(877317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/877317")]
        [Fact]
        public void Repro877317()
        {
            var source = @"
delegate void D1();
delegate D1 D2();

class C
{
    private D2 Test()
    {
        return () => (D1)this.Ext;
    }
}

static class CExtensions
{
    public static void Ext(this C r) { }
}
";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void Regress924709()
        {
            var source = @"
using System;

    class Program
    {
        static bool[] bb;

        static void Main(string[] args)
        {
            bb = AllOnesBool();

            TestArrElement(bb);
            TestRef(ref bb[0]);
        }

        static void TestArrElement(bool[] bb)
        {
            if (bb[0] ^ GetArrElement<bool>(bb))
            {
                System.Console.WriteLine('f');
            }
        }

        static T GetArrElement<T>(T[] bb)
        {
            return bb[0];
        }

        static void TestRef(ref bool br)
        {
            if (br ^ GetRef<bool>(ref br))
            {
                System.Console.WriteLine('f');
            }
        }

        static T GetRef<T>(ref T br)
        {
            return br;
        }

        unsafe static bool[] AllOnesBool()
        {
            bool[] bb = new bool[1];

            fixed(bool* b = bb)
            {
                *(byte*)b = byte.MaxValue;
            }

            return bb;
        }
    }
";

            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @""
).VerifyIL("Program.TestArrElement(bool[])",
@"
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.u1
  IL_0003:  ldarg.0
  IL_0004:  call       ""bool Program.GetArrElement<bool>(bool[])""
  IL_0009:  xor
  IL_000a:  brfalse.s  IL_0013
  IL_000c:  ldc.i4.s   102
  IL_000e:  call       ""void System.Console.WriteLine(char)""
  IL_0013:  ret
}                                                                                         
").VerifyIL("Program.TestRef(ref bool)",
@"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldind.u1
  IL_0002:  ldarg.0
  IL_0003:  call       ""bool Program.GetRef<bool>(ref bool)""
  IL_0008:  xor
  IL_0009:  brfalse.s  IL_0012
  IL_000b:  ldc.i4.s   102
  IL_000d:  call       ""void System.Console.WriteLine(char)""
  IL_0012:  ret
}                                                                                         
");
        }

        [Fact]
        public void NamedParamsOptimizationAndParams001()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Test(a1: 5, a2: 42.ToString());
        Test(a2: 42.ToString(), a1: 5);
    }

    public static void Test(int a1, params object[] a2)
    {
        System.Console.WriteLine(a2[0]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"42
42");

            compilation.VerifyIL("Program.Main",
    @"
{
  // Code size       51 (0x33)
  .maxstack  5
  .locals init (int V_0)
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4.1
  IL_0002:  newarr     ""object""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.s   42
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       ""string int.ToString()""
  IL_0013:  stelem.ref
  IL_0014:  call       ""void Program.Test(int, params object[])""
  IL_0019:  ldc.i4.5
  IL_001a:  ldc.i4.1
  IL_001b:  newarr     ""object""
  IL_0020:  dup
  IL_0021:  ldc.i4.0
  IL_0022:  ldc.i4.s   42
  IL_0024:  stloc.0
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""string int.ToString()""
  IL_002c:  stelem.ref
  IL_002d:  call       ""void Program.Test(int, params object[])""
  IL_0032:  ret
}
");
        }

        [Fact]
        public void NamedParamsOptimizationAndParams002()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Test(a2: 42.ToString(), a1: 5.ToString());
    }

    public static void Test(string a1, params object[] a2)
    {
        System.Console.WriteLine(a2[0]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"42");

            compilation.VerifyIL("Program.Main",
    @"
{
  // Code size       36 (0x24)
  .maxstack  5
  .locals init (object V_0,
                int V_1)
  IL_0000:  ldc.i4.s   42
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_1
  IL_0005:  call       ""string int.ToString()""
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.5
  IL_000c:  stloc.1
  IL_000d:  ldloca.s   V_1
  IL_000f:  call       ""string int.ToString()""
  IL_0014:  ldc.i4.1
  IL_0015:  newarr     ""object""
  IL_001a:  dup
  IL_001b:  ldc.i4.0
  IL_001c:  ldloc.0
  IL_001d:  stelem.ref
  IL_001e:  call       ""void Program.Test(string, params object[])""
  IL_0023:  ret
}
");
        }

        [Fact]
        public void NamedParamsOptimizationAndParams003()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Test(a2: 42.ToString(), a1: 5);
    }

    public static void Test(int a1, int aOpt = 333, params object[] a2)
    {
        System.Console.WriteLine(a2[0]);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"42");

            compilation.VerifyIL("Program.Main",
    @"
{
  // Code size       31 (0x1f)
  .maxstack  6
  .locals init (int V_0)
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4     0x14d
  IL_0006:  ldc.i4.1
  IL_0007:  newarr     ""object""
  IL_000c:  dup
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.s   42
  IL_0010:  stloc.0
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       ""string int.ToString()""
  IL_0018:  stelem.ref
  IL_0019:  call       ""void Program.Test(int, int, params object[])""
  IL_001e:  ret
}
");
        }

        [Fact]
        [WorkItem(4196, "https://github.com/dotnet/roslyn/issues/4196")]
        public void BadDefaultParameterValue()
        {
            // In this DLL there is an optional parameter which has a corrupted metadata value
            // as the default argument.  This can happen in legitimate code when run through an
            // obfuscator program.  For compatibility with the native compiler we need to treat
            // the value as default(T) 
            string source = @"
using System;
using BadDefaultParameterValue;

class Program
{
    static void Main()
    {
        Util.M(""test"");
    }
}";

            var testReference = AssemblyMetadata.CreateFromImage(TestResources.Repros.BadDefaultParameterValue).GetReference();
            var compilation = CompileAndVerify(source, references: new[] { testReference });
            compilation.VerifyIL("Program.Main", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldstr      ""test""
  IL_0005:  ldnull
  IL_0006:  call       ""void BadDefaultParameterValue.Util.M(string, string)""
  IL_000b:  ret
}");
        }

        [WorkItem(5530, "https://github.com/dotnet/roslyn/issues/5530")]
        [Fact]
        public void InplaceCtorUsesLocal()
        {
            string source = @"

    class Program
    {
        private static S1[] arr = new S1[1];

        struct S1
        {
            public int a, b;
            public S1(int a, int b)
            {
                this.a = a;
                this.b = b;
            }
        }

        static void Main(string[] args)
        {
            var arg = System.Math.Max(1, 2);
            var val = new S1(arg, arg);
            arr[0] = val;
            System.Console.WriteLine(arr[0].a);
        }
    }
";

            var compilation = CompileAndVerify(source, expectedOutput: "2");

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (int V_0, //arg
                Program.S1 V_1) //val
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  call       ""int System.Math.Max(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldloc.0
  IL_000b:  ldloc.0
  IL_000c:  call       ""Program.S1..ctor(int, int)""
  IL_0011:  ldsfld     ""Program.S1[] Program.arr""
  IL_0016:  ldc.i4.0
  IL_0017:  ldloc.1
  IL_0018:  stelem     ""Program.S1""
  IL_001d:  ldsfld     ""Program.S1[] Program.arr""
  IL_0022:  ldc.i4.0
  IL_0023:  ldelema    ""Program.S1""
  IL_0028:  ldfld      ""int Program.S1.a""
  IL_002d:  call       ""void System.Console.WriteLine(int)""
  IL_0032:  ret
}
");
        }

        [WorkItem(5530, "https://github.com/dotnet/roslyn/issues/5530")]
        [Fact]
        public void TernaryConsequenceUsesLocal()
        {
            string source = @"

    class Program
    {
        static bool goo()
        {
            return true;
        }

        static void Main(string[] args)
        {
            bool arg;
            var val = (arg = goo())? arg & arg : false;

            System.Console.WriteLine(val);
        }
    }
";

            var compilation = CompileAndVerify(source, expectedOutput: "True");

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (bool V_0) //arg
  IL_0000:  call       ""bool Program.goo()""
  IL_0005:  dup
  IL_0006:  stloc.0
  IL_0007:  brtrue.s   IL_000c
  IL_0009:  ldc.i4.0
  IL_000a:  br.s       IL_000f
  IL_000c:  ldloc.0
  IL_000d:  ldloc.0
  IL_000e:  and
  IL_000f:  call       ""void System.Console.WriteLine(bool)""
  IL_0014:  ret
}
");
        }

        [WorkItem(5530, "https://github.com/dotnet/roslyn/issues/5530")]
        [Fact]
        public void CoalesceUsesLocal()
        {
            string source = @"

    class Program
    {
        static string goo()
        {
            return ""hi"";
        }

        static void Main(string[] args)
        {
            string str;
            var val = (str = goo()) ?? str + ""aa"";

            System.Console.WriteLine(val);
        }
    }
";

            var compilation = CompileAndVerify(source, expectedOutput: "hi");

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (string V_0) //str
  IL_0000:  call       ""string Program.goo()""
  IL_0005:  dup
  IL_0006:  stloc.0
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_0016
  IL_000a:  pop
  IL_000b:  ldloc.0
  IL_000c:  ldstr      ""aa""
  IL_0011:  call       ""string string.Concat(string, string)""
  IL_0016:  call       ""void System.Console.WriteLine(string)""
  IL_001b:  ret
}
");
        }

        [WorkItem(5530, "https://github.com/dotnet/roslyn/issues/5530")]
        [Fact]
        public void TernaryUsesLocal()
        {
            string source = @"

    class Program
    {
        static void Main(string[] args)
        {
            string sline = GetString();

            var lastChar = sline.Length == 0 ? '\0' : sline[sline.Length - 1];

            System.Console.WriteLine(lastChar);
        }

        private static string GetString()
        {
            return ""hello"";
        }
    }
";

            var compilation = CompileAndVerify(source, expectedOutput: "o");

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (string V_0) //sline
  IL_0000:  call       ""string Program.GetString()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  callvirt   ""int string.Length.get""
  IL_000c:  brfalse.s  IL_001e
  IL_000e:  ldloc.0
  IL_000f:  ldloc.0
  IL_0010:  callvirt   ""int string.Length.get""
  IL_0015:  ldc.i4.1
  IL_0016:  sub
  IL_0017:  callvirt   ""char string.this[int].get""
  IL_001c:  br.s       IL_001f
  IL_001e:  ldc.i4.0
  IL_001f:  call       ""void System.Console.WriteLine(char)""
  IL_0024:  ret
}
");
        }

        [WorkItem(5530, "https://github.com/dotnet/roslyn/issues/5530")]
        [Fact]
        public void LogicalOpUsesLocal()
        {
            string source = @"

    class Program
    {
        static void Main(string[] args)
        {
            string tokenString = GetString();


            if (tokenString[tokenString.Length - 1] != 'L' && tokenString[tokenString.Length -1] != 'l')
            {
                System.Console.WriteLine(""hi"");
            }
            }

            private static string GetString()
            {
                return ""hello"";
            }
    }
";

            var compilation = CompileAndVerify(source, expectedOutput: "hi");

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       53 (0x35)
  .maxstack  3
  .locals init (string V_0) //tokenString
  IL_0000:  call       ""string Program.GetString()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldloc.0
  IL_0008:  callvirt   ""int string.Length.get""
  IL_000d:  ldc.i4.1
  IL_000e:  sub
  IL_000f:  callvirt   ""char string.this[int].get""
  IL_0014:  ldc.i4.s   76
  IL_0016:  beq.s      IL_0034
  IL_0018:  ldloc.0
  IL_0019:  ldloc.0
  IL_001a:  callvirt   ""int string.Length.get""
  IL_001f:  ldc.i4.1
  IL_0020:  sub
  IL_0021:  callvirt   ""char string.this[int].get""
  IL_0026:  ldc.i4.s   108
  IL_0028:  beq.s      IL_0034
  IL_002a:  ldstr      ""hi""
  IL_002f:  call       ""void System.Console.WriteLine(string)""
  IL_0034:  ret
}
");
        }

        [WorkItem(5880, "https://github.com/dotnet/roslyn/issues/5880")]
        [Fact]
        public void StructCtorArgTernary()
        {
            string source = @"
   using System.Collections.Generic;
   using System;

   class Program
    {
        struct TextSpan
        {
            private int start;
            private int length;

            public int Start => start;
            public int End => start + length;
            public int Length => length;

            public TextSpan(int start, int length)
            {
                this.start = start;
                this.length = length;
            }
        }

        static void Main(string[] args)
        {
            int length = 123;
            int start = 5;

            var list = new List<TextSpan>(10);
            list.Add(new TextSpan(0, 10));
            list.Add(new TextSpan(0, 10));

            for (int i = 0; i < list.Count; i++)
            {
                var span = list[i];
                if (span.End < start)
                {
                    continue;
                }

                var newStart = Math.Min(Math.Max(span.Start + 10, 0), length);
                var newSpan = new TextSpan(newStart, newStart >= length ? 0 : span.Length);

                list[i] = newSpan;
            }
        }
    }
";

            var compilation = CompileAndVerify(source, expectedOutput: "");

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size      135 (0x87)
  .maxstack  4
  .locals init (int V_0, //length
                int V_1, //start
                System.Collections.Generic.List<Program.TextSpan> V_2, //list
                int V_3, //i
                Program.TextSpan V_4, //span
                int V_5, //newStart
                Program.TextSpan V_6) //newSpan
  IL_0000:  ldc.i4.s   123
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.5
  IL_0004:  stloc.1
  IL_0005:  ldc.i4.s   10
  IL_0007:  newobj     ""System.Collections.Generic.List<Program.TextSpan>..ctor(int)""
  IL_000c:  stloc.2
  IL_000d:  ldloc.2
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.s   10
  IL_0011:  newobj     ""Program.TextSpan..ctor(int, int)""
  IL_0016:  callvirt   ""void System.Collections.Generic.List<Program.TextSpan>.Add(Program.TextSpan)""
  IL_001b:  ldloc.2
  IL_001c:  ldc.i4.0
  IL_001d:  ldc.i4.s   10
  IL_001f:  newobj     ""Program.TextSpan..ctor(int, int)""
  IL_0024:  callvirt   ""void System.Collections.Generic.List<Program.TextSpan>.Add(Program.TextSpan)""
  IL_0029:  ldc.i4.0
  IL_002a:  stloc.3
  IL_002b:  br.s       IL_007d
  IL_002d:  ldloc.2
  IL_002e:  ldloc.3
  IL_002f:  callvirt   ""Program.TextSpan System.Collections.Generic.List<Program.TextSpan>.this[int].get""
  IL_0034:  stloc.s    V_4
  IL_0036:  ldloca.s   V_4
  IL_0038:  call       ""int Program.TextSpan.End.get""
  IL_003d:  ldloc.1
  IL_003e:  blt.s      IL_0079
  IL_0040:  ldloca.s   V_4
  IL_0042:  call       ""int Program.TextSpan.Start.get""
  IL_0047:  ldc.i4.s   10
  IL_0049:  add
  IL_004a:  ldc.i4.0
  IL_004b:  call       ""int System.Math.Max(int, int)""
  IL_0050:  ldloc.0
  IL_0051:  call       ""int System.Math.Min(int, int)""
  IL_0056:  stloc.s    V_5
  IL_0058:  ldloca.s   V_6
  IL_005a:  ldloc.s    V_5
  IL_005c:  ldloc.s    V_5
  IL_005e:  ldloc.0
  IL_005f:  bge.s      IL_006a
  IL_0061:  ldloca.s   V_4
  IL_0063:  call       ""int Program.TextSpan.Length.get""
  IL_0068:  br.s       IL_006b
  IL_006a:  ldc.i4.0
  IL_006b:  call       ""Program.TextSpan..ctor(int, int)""
  IL_0070:  ldloc.2
  IL_0071:  ldloc.3
  IL_0072:  ldloc.s    V_6
  IL_0074:  callvirt   ""void System.Collections.Generic.List<Program.TextSpan>.this[int].set""
  IL_0079:  ldloc.3
  IL_007a:  ldc.i4.1
  IL_007b:  add
  IL_007c:  stloc.3
  IL_007d:  ldloc.3
  IL_007e:  ldloc.2
  IL_007f:  callvirt   ""int System.Collections.Generic.List<Program.TextSpan>.Count.get""
  IL_0084:  blt.s      IL_002d
  IL_0086:  ret
}");
        }

        [WorkItem(10463, "https://github.com/dotnet/roslyn/issues/10463")]
        [Fact]
        public void FieldInitializerDynamic()
        {
            string source = @"
using System;

class M
{
    object a = Test((dynamic)2);

    static object Test(object obj) => obj;

    static void Main()
    {
        Console.Write(new M().a);
    }
}
";

            var compilation = CompileAndVerifyWithMscorlib40(source, new[] { SystemCoreRef, CSharpRef }, expectedOutput: "2");

            // the main point of this test is to have it PEVerify/run correctly, although checking IL too can't hurt.
            compilation.VerifyIL("M..ctor",
@"{
  // Code size      115 (0x73)
  .maxstack  10
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> M.<>o__3.<>p__0""
  IL_0006:  brtrue.s   IL_0043
  IL_0008:  ldc.i4.0
  IL_0009:  ldstr      ""Test""
  IL_000e:  ldnull
  IL_000f:  ldtoken    ""M""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  ldc.i4.2
  IL_001a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001f:  dup
  IL_0020:  ldc.i4.0
  IL_0021:  ldc.i4.s   33
  IL_0023:  ldnull
  IL_0024:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0029:  stelem.ref
  IL_002a:  dup
  IL_002b:  ldc.i4.1
  IL_002c:  ldc.i4.0
  IL_002d:  ldnull
  IL_002e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0033:  stelem.ref
  IL_0034:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0039:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> M.<>o__3.<>p__0""
  IL_0043:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> M.<>o__3.<>p__0""
  IL_0048:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Target""
  IL_004d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> M.<>o__3.<>p__0""
  IL_0052:  ldtoken    ""M""
  IL_0057:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005c:  ldc.i4.2
  IL_005d:  box        ""int""
  IL_0062:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_0067:  stfld      ""object M.a""
  IL_006c:  ldarg.0
  IL_006d:  call       ""object..ctor()""
  IL_0072:  ret
}");
        }

        [WorkItem(10463, "https://github.com/dotnet/roslyn/issues/10463")]
        [Fact]
        public void FieldInitializerDynamicParameter()
        {
            string source = @"
using System;

class M
{
    // inner call is dynamic parameter, static argument
    // outer call is dynamic parameter, dynamic argument
    object a = Test(Test(2));

    static dynamic Test(dynamic obj) => obj;

    static void Main()
    {
        Console.Write(new M().a);
    }
}
";

            var compilation = CompileAndVerifyWithMscorlib40(source, new[] { SystemCoreRef, CSharpRef }, expectedOutput: "2");

            compilation.VerifyIL("M..ctor",
@"{
  // Code size      120 (0x78)
  .maxstack  10
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> M.<>o__3.<>p__0""
  IL_0006:  brtrue.s   IL_0043
  IL_0008:  ldc.i4.0
  IL_0009:  ldstr      ""Test""
  IL_000e:  ldnull
  IL_000f:  ldtoken    ""M""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  ldc.i4.2
  IL_001a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001f:  dup
  IL_0020:  ldc.i4.0
  IL_0021:  ldc.i4.s   33
  IL_0023:  ldnull
  IL_0024:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0029:  stelem.ref
  IL_002a:  dup
  IL_002b:  ldc.i4.1
  IL_002c:  ldc.i4.0
  IL_002d:  ldnull
  IL_002e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0033:  stelem.ref
  IL_0034:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0039:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> M.<>o__3.<>p__0""
  IL_0043:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> M.<>o__3.<>p__0""
  IL_0048:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Target""
  IL_004d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> M.<>o__3.<>p__0""
  IL_0052:  ldtoken    ""M""
  IL_0057:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005c:  ldc.i4.2
  IL_005d:  box        ""int""
  IL_0062:  call       ""dynamic M.Test(dynamic)""
  IL_0067:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_006c:  stfld      ""object M.a""
  IL_0071:  ldarg.0
  IL_0072:  call       ""object..ctor()""
  IL_0077:  ret
}");
        }

        [WorkItem(10463, "https://github.com/dotnet/roslyn/issues/10463")]
        [Fact]
        public void FieldInitializerDynamicInstance()
        {
            string source = @"
using System;

class M
{
    object a = Test((dynamic)2);

    object Test(object obj) => obj;

    static void Main()
    {
        Console.Write(new M().a);
    }
}
";

            // BREAKING CHANGE: The native compiler allowed this (and generated code that will always throw at runtime)
            CreateCompilationWithMscorlib45AndCSharp(source).VerifyDiagnostics(
                // (6,16): error CS0236: A field initializer cannot reference the non-static field, method, or property 'M.Test(object)'
                //     object a = Test((dynamic)2);
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "Test").WithArguments("M.Test(object)").WithLocation(6, 16)
            );
        }

        [WorkItem(10463, "https://github.com/dotnet/roslyn/issues/10463")]
        [Fact]
        public void FieldInitializerDynamicBothStaticInstance()
        {
            string source = @"
using System;

class M
{
    object a = Test((dynamic)2L);
    object b = Test((dynamic)2);

    static object Test(long obj)
    {
        Console.Write(""long."");
        return obj;
    }

    object Test(int obj)
    {
        Console.Write(""int."");
        return obj;
    }

    static void Main()
    {
        try
        {
            Console.Write(new M().a);
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
        {
            Console.Write(""ex caught"");
        }
    }
}
";

            var compilation = CompileAndVerifyWithMscorlib40(source, new[] { SystemCoreRef, CSharpRef }, expectedOutput: "long.ex caught");
        }

        [WorkItem(10463, "https://github.com/dotnet/roslyn/issues/10463")]
        [Fact]
        public void CtorInitializerInstance()
        {
            string source = @"
using System;

class B
{
    public object a;

    public B(object obj)
    {
        this.a = obj;
    }
}

class M : B
{
    public M() : base((object)Test((dynamic)2))
    {
    }

    object Test(object obj)
    {
        return obj;
    }

    static void Main()
    {
        try
        {
            Console.Write(new M().a);
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
        {
            Console.Write(ex.Message);
        }
    }
}
";

            // BREAKING CHANGE: The native compiler allowed this (and generated code that will always throw at runtime)
            CreateCompilationWithMscorlib45AndCSharp(source).VerifyDiagnostics(
                // (16,31): error CS0120: An object reference is required for the non-static field, method, or property 'M.Test(object)'
                //     public M() : base((object)Test((dynamic)2))
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Test").WithArguments("M.Test(object)").WithLocation(16, 31)
            );
        }

        [WorkItem(10463, "https://github.com/dotnet/roslyn/issues/10463")]
        [Fact]
        public void StaticCtorInstance()
        {
            string source = @"
using System;

class M
{
    static M()
    {
        Console.Write((object)Test((dynamic)2));
    }

    object Test(object obj)
    {
        return obj;
    }

    static void Main()
    {
    }
}
";

            // BREAKING CHANGE: The native compiler allowed this (and generated code that will always throw at runtime)
            CreateCompilationWithMscorlib45AndCSharp(source).VerifyDiagnostics(
                // (8,31): error CS0120: An object reference is required for the non-static field, method, or property 'M.Test(object)'
                //         Console.Write((object)Test((dynamic)2));
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Test").WithArguments("M.Test(object)").WithLocation(8, 31)
            );
        }

        [WorkItem(10463, "https://github.com/dotnet/roslyn/issues/10463")]
        [Fact]
        public void StaticFieldInstance()
        {
            string source = @"
class M
{
    static object o = (object)Test((dynamic)2);

    object Test(object obj)
    {
        return obj;
    }

    static void Main()
    {
    }
}
";

            // BREAKING CHANGE: The native compiler allowed this (and generated code that will always throw at runtime)
            CreateCompilationWithMscorlib45AndCSharp(source).VerifyDiagnostics(
                // (4,31): error CS0236: A field initializer cannot reference the non-static field, method, or property 'M.Test(object)'
                //     static object o = (object)Test((dynamic)2);
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "Test").WithArguments("M.Test(object)").WithLocation(4, 31)
            );
        }

        [Fact]
        public void CallingInstanceDynamicallyFromStaticContext()
        {
            string source = @"
class B
{
    public B(int x)
    {
    }
}

class C : B
{
    int InstanceMethod(int x)
    {
        return x;
    }

    static int field = (int)InstanceMethod((dynamic)2);
    static int Property
    {
        get
        {
            return (int)InstanceMethod((dynamic)2);
        }
    }
    static int Method()
    {
        return (int)InstanceMethod((dynamic)2);
    }

    // these are all still static contexts, even though they're related to instance things
    int instanceField = (int)InstanceMethod((dynamic)2);
    public C(int x) : base((int)InstanceMethod((dynamic)x))
    {
    }
    public C() : this((int)InstanceMethod((dynamic)2))
    {
    }
}

class M
{
    static void Main()
    {
        // attempting to even load C will cause runtime errors in all cases of calling C.InstanceMethod
        System.Console.Write(5);
    }
}
";

            // BREAKING CHANGE: The native compiler allowed this (and generated code that will always throw at runtime)
            CreateCompilationWithMscorlib45AndCSharp(source).VerifyDiagnostics(
                // (16,29): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.InstanceMethod(int)'
                //     static int field = (int)InstanceMethod((dynamic)2);
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(16, 29),
                // (30,30): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.InstanceMethod(int)'
                //     int instanceField = (int)InstanceMethod((dynamic)2);
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(30, 30),
                // (21,25): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod(int)'
                //             return (int)InstanceMethod((dynamic)2);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(21, 25),
                // (26,21): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod(int)'
                //         return (int)InstanceMethod((dynamic)2);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(26, 21),
                // (31,33): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod(int)'
                //     public C(int x) : base((int)InstanceMethod((dynamic)x))
                Diagnostic(ErrorCode.ERR_ObjectRequired, "InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(31, 33),
                // (34,28): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod(int)'
                //     public C() : this((int)InstanceMethod((dynamic)2))
                Diagnostic(ErrorCode.ERR_ObjectRequired, "InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(34, 28)
            );
        }

        [Fact]
        public void CallingInstanceDynamicallyFromStaticContextWithTypeName()
        {
            // Very similar to CallingInstanceDynamicallyFromStaticContext, but every call to `InstanceMethod` is now `C.InstanceMethod`
            // The native compiler allows both cases, so it's an interesting backcompat case:
            // The spec (as of 2016-05-13, it may be changed) explicitly disallows `C.InstanceMethod`,
            // but doesn't say for just `InstanceMethod` (in a way that implies it should be allowed).
            // Roslyn disallows both cases.
            string source = @"
class B
{
    public B(int x)
    {
    }
}

class C : B
{
    int InstanceMethod(int x)
    {
        return x;
    }

    static int field = (int)C.InstanceMethod((dynamic)2);
    static int Property
    {
        get
        {
            return (int)C.InstanceMethod((dynamic)2);
        }
    }
    static int Method()
    {
        return (int)C.InstanceMethod((dynamic)2);
    }

    // these are all still static contexts, even though they're related to instance things
    int instanceField = (int)C.InstanceMethod((dynamic)2);
    public C(int x) : base((int)C.InstanceMethod((dynamic)x))
    {
    }
    public C() : this((int)C.InstanceMethod((dynamic)2))
    {
    }
}

class M
{
    static void Main()
    {
        // attempting to even load C will cause runtime errors in all cases of calling C.InstanceMethod
        System.Console.Write(5);
    }
}
";

            // BREAKING CHANGE: The native compiler allowed this (and generated code that will always throw at runtime)
            CreateCompilationWithMscorlib45AndCSharp(source).VerifyDiagnostics(
                // (16,29): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod(int)'
                //     static int field = (int)C.InstanceMethod((dynamic)2);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(16, 29),
                // (30,30): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod(int)'
                //     int instanceField = (int)C.InstanceMethod((dynamic)2);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(30, 30),
                // (21,25): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod(int)'
                //             return (int)C.InstanceMethod((dynamic)2);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(21, 25),
                // (26,21): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod(int)'
                //         return (int)C.InstanceMethod((dynamic)2);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(26, 21),
                // (31,33): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod(int)'
                //     public C(int x) : base((int)C.InstanceMethod((dynamic)x))
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(31, 33),
                // (34,28): error CS0120: An object reference is required for the non-static field, method, or property 'C.InstanceMethod(int)'
                //     public C() : this((int)C.InstanceMethod((dynamic)2))
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.InstanceMethod").WithArguments("C.InstanceMethod(int)").WithLocation(34, 28)
            );
        }

        [Fact, WorkItem(13486, "https://github.com/dotnet/roslyn/issues/13486")]
        public void BinaryMulOptimizationsAndSideeffects()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        // should not optimize
        System.Console.WriteLine(Goo1() * 0);
        System.Console.WriteLine(0 * Goo1());

        // should optimize
        var local = 123;
        System.Console.WriteLine(local * 0);
        System.Console.WriteLine(0 * default(int?));
        System.Console.WriteLine(0 * local);

        // should not capture
        System.Console.WriteLine(((Func<int>)(()=>local * 0))());

        // should not optimize
        System.Console.WriteLine(Goo2() & false);
        System.Console.WriteLine(false & Goo2());

        // should optimize
        var local1 = true;
        System.Console.WriteLine(local1 & false);
        System.Console.WriteLine(false & default(bool?));
        System.Console.WriteLine(false & ((bool?)local1).HasValue);
        System.Console.WriteLine(false & local1);

        // should optimize
        System.Console.WriteLine(Goo2() && false);
        System.Console.WriteLine(Goo2() && true);
        System.Console.WriteLine(false && Goo2());
        System.Console.WriteLine(true && Goo2());
        System.Console.WriteLine(Goo2() || false);
        System.Console.WriteLine(Goo2() || true);
        System.Console.WriteLine(false || Goo2());
        System.Console.WriteLine(true || Goo2());
    }

    static int Goo1()
    {
        System.Console.Write(""Goo1 "");
        return 42;
    }

    static bool Goo2()
    {
        System.Console.Write(""Goo2 "");
        return true;
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
Goo1 0
Goo1 0
0

0
0
Goo2 False
Goo2 False
False
False
False
False
Goo2 False
Goo2 True
False
Goo2 True
Goo2 True
Goo2 True
Goo2 True
True
");

            compilation.VerifyIL("Program.Main",
    @"
{
  // Code size      221 (0xdd)
  .maxstack  2
  IL_0000:  call       ""int Program.Goo1()""
  IL_0005:  pop
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""void System.Console.WriteLine(int)""
  IL_000c:  call       ""int Program.Goo1()""
  IL_0011:  pop
  IL_0012:  ldc.i4.0
  IL_0013:  call       ""void System.Console.WriteLine(int)""
  IL_0018:  ldc.i4.0
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ldnull
  IL_001f:  call       ""void System.Console.WriteLine(object)""
  IL_0024:  ldc.i4.0
  IL_0025:  call       ""void System.Console.WriteLine(int)""
  IL_002a:  ldsfld     ""System.Func<int> Program.<>c.<>9__0_0""
  IL_002f:  dup
  IL_0030:  brtrue.s   IL_0049
  IL_0032:  pop
  IL_0033:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0038:  ldftn      ""int Program.<>c.<Main>b__0_0()""
  IL_003e:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0043:  dup
  IL_0044:  stsfld     ""System.Func<int> Program.<>c.<>9__0_0""
  IL_0049:  callvirt   ""int System.Func<int>.Invoke()""
  IL_004e:  call       ""void System.Console.WriteLine(int)""
  IL_0053:  call       ""bool Program.Goo2()""
  IL_0058:  pop
  IL_0059:  ldc.i4.0
  IL_005a:  call       ""void System.Console.WriteLine(bool)""
  IL_005f:  call       ""bool Program.Goo2()""
  IL_0064:  pop
  IL_0065:  ldc.i4.0
  IL_0066:  call       ""void System.Console.WriteLine(bool)""
  IL_006b:  ldc.i4.0
  IL_006c:  call       ""void System.Console.WriteLine(bool)""
  IL_0071:  ldc.i4.0
  IL_0072:  box        ""bool""
  IL_0077:  call       ""void System.Console.WriteLine(object)""
  IL_007c:  ldc.i4.0
  IL_007d:  call       ""void System.Console.WriteLine(bool)""
  IL_0082:  ldc.i4.0
  IL_0083:  call       ""void System.Console.WriteLine(bool)""
  IL_0088:  call       ""bool Program.Goo2()""
  IL_008d:  brfalse.s  IL_0092
  IL_008f:  ldc.i4.0
  IL_0090:  br.s       IL_0093
  IL_0092:  ldc.i4.0
  IL_0093:  call       ""void System.Console.WriteLine(bool)""
  IL_0098:  call       ""bool Program.Goo2()""
  IL_009d:  call       ""void System.Console.WriteLine(bool)""
  IL_00a2:  ldc.i4.0
  IL_00a3:  call       ""void System.Console.WriteLine(bool)""
  IL_00a8:  call       ""bool Program.Goo2()""
  IL_00ad:  call       ""void System.Console.WriteLine(bool)""
  IL_00b2:  call       ""bool Program.Goo2()""
  IL_00b7:  call       ""void System.Console.WriteLine(bool)""
  IL_00bc:  call       ""bool Program.Goo2()""
  IL_00c1:  brtrue.s   IL_00c6
  IL_00c3:  ldc.i4.1
  IL_00c4:  br.s       IL_00c7
  IL_00c6:  ldc.i4.1
  IL_00c7:  call       ""void System.Console.WriteLine(bool)""
  IL_00cc:  call       ""bool Program.Goo2()""
  IL_00d1:  call       ""void System.Console.WriteLine(bool)""
  IL_00d6:  ldc.i4.1
  IL_00d7:  call       ""void System.Console.WriteLine(bool)""
  IL_00dc:  ret
}
");
        }

        [Fact, WorkItem(0, "http://stackoverflow.com/questions/39254676/roslyn-compiler-optimizing-away-function-call-multiplication-with-zero?stw=2")]
        public void SideEffectOptimizedAway()
        {
            var source =
@"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public static void Main(string[] args)
    {
        Stack<long> s = new Stack<long>();

        s.Push(1);           // stack contains [1]
        s.Push(s.Pop() * 0); // stack should contain [0]

        Console.WriteLine(string.Join(""|"", s.Reverse()));
    }
}
";
            CompileAndVerifyWithMscorlib40(source, references: new[] { SystemRef, SystemCoreRef },
                expectedOutput: "0");
        }

        [Fact, WorkItem(9703, "https://github.com/dotnet/roslyn/issues/9703")]
        public void IgnoredConversion()
        {
            string source = @"
using System;

public class Form1 {
    public class BadCompiler {
        public DateTime? Value {get; set;}
    }

    private BadCompiler TestObj = new BadCompiler();

    public void IPE() {
        object o;
        o = TestObj.Value;
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("Form1.IPE", @"
{
    // Code size       13 (0xd)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  ldfld      ""Form1.BadCompiler Form1.TestObj""
    IL_0006:  callvirt   ""System.DateTime? Form1.BadCompiler.Value.get""
    IL_000b:  pop
    IL_000c:  ret
}");
        }

        [Fact]
        public void CorrectOverloadOfStackAllocSpanChosen()
        {
            var source = @"
using System;
class Test
{
    unsafe public static void Main()
    {
        bool condition = false;

        var span1 = condition ? stackalloc int[1] : new Span<int>(null, 2);
        Console.Write(span1.Length);

        var span2 = condition ? new Span<int>(null, 3) : stackalloc int[4];
        Console.Write(span2.Length);
    }
}";

            var comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.UnsafeReleaseExe);
            CompileAndVerify(comp, expectedOutput: "24", verify: Verification.Fails);
            comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.UnsafeDebugExe);
            CompileAndVerify(comp, expectedOutput: "24", verify: Verification.Fails);
        }

        [Fact, WorkItem(35764, "https://github.com/dotnet/roslyn/issues/35764")]
        public void StackAllocExpressionIL()
        {
            var source = @"
using System;
class Test
{
    public static void Main()
    {
        Span<int> x = stackalloc int[33];
        Console.Write(x.Length);
        x = stackalloc int[0];
        Console.Write(x.Length);
    }
}";
            var expectedOutput = "330";
            CSharpCompilation comp;
            comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (System.Span<int> V_0) //x
  IL_0000:  ldc.i4     0x84
  IL_0005:  conv.u
  IL_0006:  localloc
  IL_0008:  ldc.i4.s   33
  IL_000a:  newobj     ""System.Span<int>..ctor(void*, int)""
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""int System.Span<int>.Length.get""
  IL_0017:  call       ""void System.Console.Write(int)""
  IL_001c:  ldloca.s   V_0
  IL_001e:  initobj    ""System.Span<int>""
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       ""int System.Span<int>.Length.get""
  IL_002b:  call       ""void System.Console.Write(int)""
  IL_0030:  ret
}");
            comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyIL("Test.Main", @"
    {
      // Code size       54 (0x36)
      .maxstack  2
      .locals init (System.Span<int> V_0, //x
                    System.Span<int> V_1)
      IL_0000:  nop
      IL_0001:  ldc.i4     0x84
      IL_0006:  conv.u
      IL_0007:  localloc
      IL_0009:  ldc.i4.s   33
      IL_000b:  newobj     ""System.Span<int>..ctor(void*, int)""
      IL_0010:  stloc.1
      IL_0011:  ldloc.1
      IL_0012:  stloc.0
      IL_0013:  ldloca.s   V_0
      IL_0015:  call       ""int System.Span<int>.Length.get""
      IL_001a:  call       ""void System.Console.Write(int)""
      IL_001f:  nop
      IL_0020:  ldloca.s   V_0
      IL_0022:  initobj    ""System.Span<int>""
      IL_0028:  ldloca.s   V_0
      IL_002a:  call       ""int System.Span<int>.Length.get""
      IL_002f:  call       ""void System.Console.Write(int)""
      IL_0034:  nop
      IL_0035:  ret
    }
");
        }

        [Fact]
        public void StackAllocSpanLengthNotEvaluatedTwice()
        {
            var source = @"
using System;
class Test
{
    private static int length = 0;

    private static int GetLength()
    {
        return ++length;
    }

    public static void Main()
    {
        for (int i = 0; i < 5; i++)
        {
            Span<int> x = stackalloc int[GetLength()];
            Console.Write(x.Length);
        }
    }
}";
            var comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "12345", verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (int V_0, //i
                System.Span<int> V_1, //x
                int V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0027
  IL_0004:  call       ""int Test.GetLength()""
  IL_0009:  stloc.2
  IL_000a:  ldloc.2
  IL_000b:  conv.u
  IL_000c:  ldc.i4.4
  IL_000d:  mul.ovf.un
  IL_000e:  localloc
  IL_0010:  ldloc.2
  IL_0011:  newobj     ""System.Span<int>..ctor(void*, int)""
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       ""int System.Span<int>.Length.get""
  IL_001e:  call       ""void System.Console.Write(int)""
  IL_0023:  ldloc.0
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  stloc.0
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4.5
  IL_0029:  blt.s      IL_0004
  IL_002b:  ret
}");
            comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "12345", verify: Verification.Fails).VerifyIL("Test.Main", @"
    {
      // Code size       56 (0x38)
      .maxstack  2
      .locals init (int V_0, //i
                    System.Span<int> V_1, //x
                    int V_2,
                    System.Span<int> V_3,
                    bool V_4)
      IL_0000:  nop
      IL_0001:  ldc.i4.0
      IL_0002:  stloc.0
      IL_0003:  br.s       IL_002d
      IL_0005:  nop
      IL_0006:  call       ""int Test.GetLength()""
      IL_000b:  stloc.2
      IL_000c:  ldloc.2
      IL_000d:  conv.u
      IL_000e:  ldc.i4.4
      IL_000f:  mul.ovf.un
      IL_0010:  localloc
      IL_0012:  ldloc.2
      IL_0013:  newobj     ""System.Span<int>..ctor(void*, int)""
      IL_0018:  stloc.3
      IL_0019:  ldloc.3
      IL_001a:  stloc.1
      IL_001b:  ldloca.s   V_1
      IL_001d:  call       ""int System.Span<int>.Length.get""
      IL_0022:  call       ""void System.Console.Write(int)""
      IL_0027:  nop
      IL_0028:  nop
      IL_0029:  ldloc.0
      IL_002a:  ldc.i4.1
      IL_002b:  add
      IL_002c:  stloc.0
      IL_002d:  ldloc.0
      IL_002e:  ldc.i4.5
      IL_002f:  clt
      IL_0031:  stloc.s    V_4
      IL_0033:  ldloc.s    V_4
      IL_0035:  brtrue.s   IL_0005
      IL_0037:  ret
    }
");
        }

        [Fact]
        public void NestedStackAlloc_01()
        {
            var source = @"
using System;
class Test
{
    public static void Main()
    {
        Console.WriteLine(M(2, stackalloc int[3]));
    }
    public static int M(int x, Span<int> y) => x * y.Length;
}";
            var comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "6", verify: Verification.Fails).VerifyIL("Test.Main", @"
    {
      // Code size       25 (0x19)
      .maxstack  2
      .locals init (System.Span<int> V_0)
      IL_0000:  ldc.i4.s   12
      IL_0002:  conv.u
      IL_0003:  localloc
      IL_0005:  ldc.i4.3
      IL_0006:  newobj     ""System.Span<int>..ctor(void*, int)""
      IL_000b:  stloc.0
      IL_000c:  ldc.i4.2
      IL_000d:  ldloc.0
      IL_000e:  call       ""int Test.M(int, System.Span<int>)""
      IL_0013:  call       ""void System.Console.WriteLine(int)""
      IL_0018:  ret
    }
");
            comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "6", verify: Verification.Fails).VerifyIL("Test.Main", @"
    {
      // Code size       27 (0x1b)
      .maxstack  2
      .locals init (System.Span<int> V_0)
      IL_0000:  nop
      IL_0001:  ldc.i4.s   12
      IL_0003:  conv.u
      IL_0004:  localloc
      IL_0006:  ldc.i4.3
      IL_0007:  newobj     ""System.Span<int>..ctor(void*, int)""
      IL_000c:  stloc.0
      IL_000d:  ldc.i4.2
      IL_000e:  ldloc.0
      IL_000f:  call       ""int Test.M(int, System.Span<int>)""
      IL_0014:  call       ""void System.Console.WriteLine(int)""
      IL_0019:  nop
      IL_001a:  ret
    }
");
        }

        [Fact]
        public void NestedStackAlloc_02()
        {
            var source = @"
using System;
class Test
{
    public static void Main()
    {
        int z = 2;
        Console.WriteLine(M(z, stackalloc int[3]));
    }
    public static int M(int x, Span<int> y) => x * y.Length;
}";
            var comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "6", verify: Verification.Fails).VerifyIL("Test.Main", @"
    {
      // Code size       27 (0x1b)
      .maxstack  2
      .locals init (int V_0,
                    System.Span<int> V_1)
      IL_0000:  ldc.i4.2
      IL_0001:  stloc.0
      IL_0002:  ldc.i4.s   12
      IL_0004:  conv.u
      IL_0005:  localloc
      IL_0007:  ldc.i4.3
      IL_0008:  newobj     ""System.Span<int>..ctor(void*, int)""
      IL_000d:  stloc.1
      IL_000e:  ldloc.0
      IL_000f:  ldloc.1
      IL_0010:  call       ""int Test.M(int, System.Span<int>)""
      IL_0015:  call       ""void System.Console.WriteLine(int)""
      IL_001a:  ret
    }
");
            comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "6", verify: Verification.Fails).VerifyIL("Test.Main", @"
    {
      // Code size       31 (0x1f)
      .maxstack  2
      .locals init (int V_0, //z
                    int V_1,
                    System.Span<int> V_2)
      IL_0000:  nop
      IL_0001:  ldc.i4.2
      IL_0002:  stloc.0
      IL_0003:  ldloc.0
      IL_0004:  stloc.1
      IL_0005:  ldc.i4.s   12
      IL_0007:  conv.u
      IL_0008:  localloc
      IL_000a:  ldc.i4.3
      IL_000b:  newobj     ""System.Span<int>..ctor(void*, int)""
      IL_0010:  stloc.2
      IL_0011:  ldloc.1
      IL_0012:  ldloc.2
      IL_0013:  call       ""int Test.M(int, System.Span<int>)""
      IL_0018:  call       ""void System.Console.WriteLine(int)""
      IL_001d:  nop
      IL_001e:  ret
    }
");
        }

        [Fact]
        public void NestedStackAlloc_03()
        {
            var source = @"
using System;
class Test
{
    public static void Main()
    {
        int z = 2;
        Console.WriteLine(z * stackalloc int[3] { 1, stackalloc int[2] { 4, 5 }.Length, 3 }.Length);
    }
    public static int M(int x, Span<int> y) => x * y.Length;
}";
            var comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "6", verify: Verification.Fails).VerifyIL("Test.Main", @"
    {
      // Code size       70 (0x46)
      .maxstack  4
      .locals init (int V_0,
                    System.Span<int> V_1,
                    System.Span<int> V_2)
      IL_0000:  ldc.i4.2
      IL_0001:  stloc.0
      IL_0002:  ldc.i4.8
      IL_0003:  conv.u
      IL_0004:  localloc
      IL_0006:  dup
      IL_0007:  ldc.i4.4
      IL_0008:  stind.i4
      IL_0009:  dup
      IL_000a:  ldc.i4.4
      IL_000b:  add
      IL_000c:  ldc.i4.5
      IL_000d:  stind.i4
      IL_000e:  ldc.i4.2
      IL_000f:  newobj     ""System.Span<int>..ctor(void*, int)""
      IL_0014:  stloc.2
      IL_0015:  ldc.i4.s   12
      IL_0017:  conv.u
      IL_0018:  localloc
      IL_001a:  dup
      IL_001b:  ldc.i4.1
      IL_001c:  stind.i4
      IL_001d:  dup
      IL_001e:  ldc.i4.4
      IL_001f:  add
      IL_0020:  ldloca.s   V_2
      IL_0022:  call       ""int System.Span<int>.Length.get""
      IL_0027:  stind.i4
      IL_0028:  dup
      IL_0029:  ldc.i4.2
      IL_002a:  conv.i
      IL_002b:  ldc.i4.4
      IL_002c:  mul
      IL_002d:  add
      IL_002e:  ldc.i4.3
      IL_002f:  stind.i4
      IL_0030:  ldc.i4.3
      IL_0031:  newobj     ""System.Span<int>..ctor(void*, int)""
      IL_0036:  stloc.1
      IL_0037:  ldloc.0
      IL_0038:  ldloca.s   V_1
      IL_003a:  call       ""int System.Span<int>.Length.get""
      IL_003f:  mul
      IL_0040:  call       ""void System.Console.WriteLine(int)""
      IL_0045:  ret
    }
");
            comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "6", verify: Verification.Fails).VerifyIL("Test.Main", @"
    {
      // Code size       74 (0x4a)
      .maxstack  4
      .locals init (int V_0, //z
                    int V_1,
                    System.Span<int> V_2,
                    System.Span<int> V_3)
      IL_0000:  nop
      IL_0001:  ldc.i4.2
      IL_0002:  stloc.0
      IL_0003:  ldloc.0
      IL_0004:  stloc.1
      IL_0005:  ldc.i4.8
      IL_0006:  conv.u
      IL_0007:  localloc
      IL_0009:  dup
      IL_000a:  ldc.i4.4
      IL_000b:  stind.i4
      IL_000c:  dup
      IL_000d:  ldc.i4.4
      IL_000e:  add
      IL_000f:  ldc.i4.5
      IL_0010:  stind.i4
      IL_0011:  ldc.i4.2
      IL_0012:  newobj     ""System.Span<int>..ctor(void*, int)""
      IL_0017:  stloc.3
      IL_0018:  ldc.i4.s   12
      IL_001a:  conv.u
      IL_001b:  localloc
      IL_001d:  dup
      IL_001e:  ldc.i4.1
      IL_001f:  stind.i4
      IL_0020:  dup
      IL_0021:  ldc.i4.4
      IL_0022:  add
      IL_0023:  ldloca.s   V_3
      IL_0025:  call       ""int System.Span<int>.Length.get""
      IL_002a:  stind.i4
      IL_002b:  dup
      IL_002c:  ldc.i4.2
      IL_002d:  conv.i
      IL_002e:  ldc.i4.4
      IL_002f:  mul
      IL_0030:  add
      IL_0031:  ldc.i4.3
      IL_0032:  stind.i4
      IL_0033:  ldc.i4.3
      IL_0034:  newobj     ""System.Span<int>..ctor(void*, int)""
      IL_0039:  stloc.2
      IL_003a:  ldloc.1
      IL_003b:  ldloca.s   V_2
      IL_003d:  call       ""int System.Span<int>.Length.get""
      IL_0042:  mul
      IL_0043:  call       ""void System.Console.WriteLine(int)""
      IL_0048:  nop
      IL_0049:  ret
    }
");
        }

        [Fact]
        public void ImplicitCastOperatorOnStackAllocIsLoweredCorrectly()
        {
            var source = @"
using System;
unsafe class Test
{
    public static void Main()
    {
        Test obj1 = stackalloc int[10];
        Console.Write(""|"");
        Test obj2 = stackalloc double[10];
    }
    
    public static implicit operator Test(Span<int> value) 
    {
        Console.Write(""SpanOpCalled"");
        return default(Test);
    }
    
    public static implicit operator Test(double* value) 
    {
        Console.Write(""PointerOpCalled"");
        return default(Test);
    }
}";
            var expectedOutput = "SpanOpCalled|PointerOpCalled";
            var comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.UnsafeReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.Fails);
            comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.UnsafeDebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        [Fact]
        public void ExplicitCastOperatorOnStackAllocIsLoweredCorrectly()
        {
            var source = @"
using System;
unsafe class Test
{
    public static void Main()
    {
        Test obj1 = (Test)stackalloc int[10];
    }
    
    public static explicit operator Test(Span<int> value) 
    {
        Console.Write(""SpanOpCalled"");
        return default(Test);
    }
}";
            var comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.UnsafeReleaseExe);
            CompileAndVerify(comp, expectedOutput: "SpanOpCalled", verify: Verification.Fails);
            comp = CreateCompilationWithMscorlibAndSpan(source, TestOptions.UnsafeDebugExe);
            CompileAndVerify(comp, expectedOutput: "SpanOpCalled", verify: Verification.Fails);
        }

        [Fact]
        public void ArrayElementCompoundAssignment_Invariant()
        {
            string source =
@"class C
{
    static void Main()
    {
        F(new string[] { """" }, ""B"");
    }
    static void F(string[] a, string s)
    {
        G(a, s);
        System.Console.Write(a[0]);
    }
    static void G(string[] a, string s)
    {
        a[0] += s;
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "B");
            verifier.VerifyIL("C.G",
@"{
  // Code size       17 (0x11)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    ""string""
  IL_0007:  dup
  IL_0008:  ldind.ref
  IL_0009:  ldarg.1
  IL_000a:  call       ""string string.Concat(string, string)""
  IL_000f:  stind.ref
  IL_0010:  ret
}");
        }

        [WorkItem(547533, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=547533")]
        [Fact]
        public void ArrayElementCompoundAssignment_Covariant()
        {
            string source =
@"class C
{
    static void Main()
    {
        F(new object[] { """" }, ""A"");
        F(new string[] { """" }, ""B"");
    }
    static void F(object[] a, string s)
    {
        G(a, s);
        System.Console.Write(a[0]);
    }
    static void G(object[] a, string s)
    {
        a[0] += s;
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "AB");
            verifier.VerifyIL("C.G",
@"{
  // Code size       27 (0x1b)
  .maxstack  4
  .locals init (object[] V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.0
  IL_0006:  ldelem.ref
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_000e
  IL_000a:  pop
  IL_000b:  ldnull
  IL_000c:  br.s       IL_0013
  IL_000e:  callvirt   ""string object.ToString()""
  IL_0013:  ldarg.1
  IL_0014:  call       ""string string.Concat(string, string)""
  IL_0019:  stelem.ref
  IL_001a:  ret
}");
        }

        [Fact]
        public void ArrayElementCompoundAssignment_ValueType()
        {
            string source =
@"class C
{
    static void Main()
    {
        F(new int[] { 1 }, 2);
    }
    static void F(int[] a, int i)
    {
        G(a, i);
        System.Console.Write(a[0]);
    }
    static void G(int[] a, int i)
    {
        a[0] += i;
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "3");
            verifier.VerifyIL("C.G",
@"{
  // Code size       13 (0xd)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    ""int""
  IL_0007:  dup
  IL_0008:  ldind.i4
  IL_0009:  ldarg.1
  IL_000a:  add
  IL_000b:  stind.i4
  IL_000c:  ret
}");
        }

        [WorkItem(547533, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=547533")]
        [Fact]
        public void ArrayElementCompoundAssignment_Covariant_NonConstantIndex()
        {
            string source =
@"class C
{
    static void Main()
    {
        F(new object[] { """" }, ""A"");
        F(new string[] { """" }, ""B"");
    }
    static void F(object[] a, string s)
    {
        G(a, s);
        System.Console.Write(a[0]);
    }
    static void G(object[] a, string s)
    {
        a[Index(a)] += s;
    }
    static int Index(object arg)
    {
        System.Console.Write(arg.GetType().Name);
        return 0;
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "Object[]AString[]B");
            verifier.VerifyIL("C.G",
@"{
  // Code size       34 (0x22)
  .maxstack  4
  .locals init (object[] V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int C.Index(object)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  ldelem.ref
  IL_000e:  dup
  IL_000f:  brtrue.s   IL_0015
  IL_0011:  pop
  IL_0012:  ldnull
  IL_0013:  br.s       IL_001a
  IL_0015:  callvirt   ""string object.ToString()""
  IL_001a:  ldarg.1
  IL_001b:  call       ""string string.Concat(string, string)""
  IL_0020:  stelem.ref
  IL_0021:  ret
}");
        }

        [Fact]
        public void ArrayElementIncrement_ValueType()
        {
            string source =
@"class C
{
    static void Main()
    {
        F(new int[] { 1 });
    }
    static void F(int[] a)
    {
        G(a);
        System.Console.Write(a[0]);
    }
    static void G(int[] a)
    {
        a[0]++;
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "2");
            verifier.VerifyIL("C.G",
@"{
  // Code size       13 (0xd)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    ""int""
  IL_0007:  dup
  IL_0008:  ldind.i4
  IL_0009:  ldc.i4.1
  IL_000a:  add
  IL_000b:  stind.i4
  IL_000c:  ret
}");
        }

        [Fact]
        public void EnumConstraint_NoBoxing()
        {
            var code = @"
enum E1
{
    A = 5
}
class Test1
{
    public static void M<T>(T arg)  where T : struct, System.Enum
    {
    }
}
class Test2
{
    public void M()
    {
        Test1.M(E1.A);
    }
}";

            CompileAndVerify(code).VerifyIL("Test2.M", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.5
  IL_0001:  call       ""void Test1.M<E1>(E1)""
  IL_0006:  ret
}");
        }

        [Fact]
        public void PartialMethodsWithInParameter_WithBody()
        {
            CompileAndVerify(@"
partial class C
{
    public void Call()
    {
        M(5);
    }
    partial void M(in int i);
}
partial class C
{
    partial void M(in int i)
    {
        System.Console.WriteLine(i);
    }
}
static class Program
{
    static void Main()
    {
        new C().Call();
    }
}",
                expectedOutput: "5")
                .VerifyIL("C.Call", @"

{
  // Code size       11 (0xb)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.5
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""void C.M(in int)""
  IL_000a:  ret
}");
        }

        [Fact]
        public void PartialMethodsWithInParameter_NoBody()
        {
            CompileAndVerify(@"
partial class C
{
    public void Call()
    {
        M(5);
    }
    partial void M(in int i);
}
static class Program
{
    static void Main()
    {
        new C().Call();
    }
}")
                .VerifyIL("C.Call", @"

{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [Fact]
        public void OverloadingPartialMethods_RefKindInWithNone_ImplementIn()
        {
            CompileAndVerify(@"
partial class C
{
    public void Call()
    {
        int x = 0;
        M(x);
        M(in x);
    }
    partial void M(in int i);
    partial void M(int i);
}
partial class C
{
    partial void M(in int i)
    {
        System.Console.WriteLine(""in called"");
    }
}
static class Program
{
    static void Main()
    {
        new C().Call();
    }
}",
                expectedOutput: "in called");
        }

        [Fact]
        public void OverloadingPartialMethods_RefKindInWithNone_ImplementNone()
        {
            CompileAndVerify(@"
partial class C
{
    public void Call()
    {
        int x = 0;
        M(x);
        M(in x);
    }
    partial void M(in int i);
    partial void M(int i);
}
partial class C
{
    partial void M(int i)
    {
        System.Console.WriteLine(""none called"");
    }
}
static class Program
{
    static void Main()
    {
        new C().Call();
    }
}",
                expectedOutput: "none called");
        }

        [Fact]
        public void OverloadingPartialMethods_RefKindInWithNone_ImplementBoth()
        {
            CompileAndVerify(@"
partial class C
{
    public void Call()
    {
        int x = 0;
        M(x);
        M(in x);
    }
    partial void M(in int i);
    partial void M(int i);
}
partial class C
{
    partial void M(int i)
    {
        System.Console.WriteLine(""none called"");
    }
    partial void M(in int i)
    {
        System.Console.WriteLine(""in called"");
    }
}
static class Program
{
    static void Main()
    {
        new C().Call();
    }
}",
                expectedOutput: @"
none called
in called");
        }

        [Fact]
        public void NormalizedNaN()
        {
            string source =
@"using System;
class Program
{
    static void Main()
    {
        CheckNaN(double.NaN);
        CheckNaN(0.0 / 0.0);
        CheckNaN(0.0 / -0.0);
        const double inf = 1.0 / 0.0;
        CheckNaN(inf + double.NaN);
        CheckNaN(inf - double.NaN);
        CheckNaN(-double.NaN);

        CheckNaN(float.NaN);
        CheckNaN(0.0f / 0.0f);
        CheckNaN(0.0f / -0.0f);
        const float finf = 1.0f / 0.0f;
        CheckNaN(finf + float.NaN);
        CheckNaN(finf - float.NaN);
        CheckNaN(-float.NaN);
}

    static void CheckNaN(double nan)
    {
        const long expected = unchecked((long)0xFFF8000000000000UL);
        long actual = BitConverter.DoubleToInt64Bits(nan);
        if (expected != actual)
            throw new Exception($""expected=0X{expected:X} actual=0X{actual:X}"");
    }

    static unsafe void CheckNaN(float nan)
    {
        const int expected = unchecked((int)0xFFC00000U);
        void* p = &nan;
        int* ip = (int*)p;
        int actual = *ip;
        if (expected != actual)
            throw new Exception($""expected=0X{expected:X} actual=0X{actual:X}"");
    }
}
";
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(true), verify: Verification.Skipped, expectedOutput: @"");
        }
    }
}
