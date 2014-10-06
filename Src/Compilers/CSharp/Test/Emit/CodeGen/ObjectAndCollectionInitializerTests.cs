// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public partial class ObjectAndCollectionInitializerTests : EmitMetadataTestBase
    {
        #region "Object Initializer Tests"

        [Fact]
        public void ObjectInitializerTest_ClassType()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 1, y = 2 };
        System.Console.WriteLine(i.x);
        System.Console.WriteLine(i.y);
    }
}
";
            string expectedOutput = @"1
2";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("MemberInitializerTest.Main", @"
{
  // Code size       41 (0x29)
  .maxstack  3
  IL_0000:  newobj     ""MemberInitializerTest..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  stfld      ""int MemberInitializerTest.x""
  IL_000c:  dup
  IL_000d:  ldc.i4.2
  IL_000e:  callvirt   ""void MemberInitializerTest.y.set""
  IL_0013:  dup
  IL_0014:  ldfld      ""int MemberInitializerTest.x""
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  callvirt   ""int MemberInitializerTest.y.get""
  IL_0023:  call       ""void System.Console.WriteLine(int)""
  IL_0028:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_StructType()
        {
            var source = @"
public struct MemberInitializerTest
{   
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 1, y = 2 };
        System.Console.WriteLine(i.x);
        System.Console.WriteLine(i.y);

    }
}
";
            string expectedOutput = @"1
2";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("MemberInitializerTest.Main", @"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (MemberInitializerTest V_0, //i
  MemberInitializerTest V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""MemberInitializerTest""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.1
  IL_000b:  stfld      ""int MemberInitializerTest.x""
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldc.i4.2
  IL_0013:  call       ""void MemberInitializerTest.y.set""
  IL_0018:  ldloc.1
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  ldfld      ""int MemberInitializerTest.x""
  IL_0020:  call       ""void System.Console.WriteLine(int)""
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""int MemberInitializerTest.y.get""
  IL_002c:  call       ""void System.Console.WriteLine(int)""
  IL_0031:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_TypeParameterType()
        {
            var source = @"
public class Base
{
    public Base() {}
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        MemberInitializerTest<Base>.Foo();
    }
}

public class MemberInitializerTest<T> where T: Base, new()
{   
    public static void Foo()
    {
        var i = new T() { x = 1, y = 2 };
        System.Console.WriteLine(i.x);
        System.Console.WriteLine(i.y);
    }
}
";
            string expectedOutput = @"1
2";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("MemberInitializerTest<T>.Foo", @"
{
  // Code size       61 (0x3d)
  .maxstack  3
  IL_0000:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0005:  dup
  IL_0006:  box        ""T""
  IL_000b:  ldc.i4.1
  IL_000c:  stfld      ""int Base.x""
  IL_0011:  dup
  IL_0012:  box        ""T""
  IL_0017:  ldc.i4.2
  IL_0018:  callvirt   ""void Base.y.set""
  IL_001d:  dup
  IL_001e:  box        ""T""
  IL_0023:  ldfld      ""int Base.x""
  IL_0028:  call       ""void System.Console.WriteLine(int)""
  IL_002d:  box        ""T""
  IL_0032:  callvirt   ""int Base.y.get""
  IL_0037:  call       ""void System.Console.WriteLine(int)""
  IL_003c:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_TypeParameterType_InterfaceConstraint()
        {
            var source = @"
using System;

class MemberInitializerTest
{
    static int Main()
    {
        Console.WriteLine(Foo<S>());
        return 0;
    }

    static byte Foo<T>() where T : I, new()
    {
        var b = new T { X = 1 };
        return b.X;
    }
}

interface I
{
    byte X { get; set; }
}

struct S : I
{
    public byte X { get; set; }
}
";
            string expectedOutput = "1";
            var compVerifier = CompileAndVerify(source, emitOptions: TestEmitters.CCI, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("MemberInitializerTest.Foo<T>", @"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (T V_0, //b
                T V_1)
  IL_0000:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  ldc.i4.1
  IL_0009:  constrained. ""T""
  IL_000f:  callvirt   ""void I.X.set""
  IL_0014:  ldloc.1
  IL_0015:  stloc.0
  IL_0016:  ldloca.s   V_0
  IL_0018:  constrained. ""T""
  IL_001e:  callvirt   ""byte I.X.get""
  IL_0023:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_NullableType()
        {
            var source = @"
using System;

class MemberInitializerTest
{
    static int Main()
    {
        Console.WriteLine(Foo<S>().Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return 0;
    }

    static Decimal? Foo<T>() where T : I, new()
    {
        var b = new T { X = 1.1M };
        return b.X;
    }
}

interface I
{
    Decimal? X { get; set; }
}

struct S : I
{
    public Decimal? X { get; set; }
}";
            string expectedOutput = "1.1";
            var compVerifier = CompileAndVerify(source, emitOptions: TestEmitters.CCI, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("MemberInitializerTest.Foo<T>", @"
{
  // Code size       51 (0x33)
  .maxstack  6
  .locals init (T V_0, //b
                T V_1)
  IL_0000:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  ldc.i4.s   11
  IL_000a:  ldc.i4.0
  IL_000b:  ldc.i4.0
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.1
  IL_000e:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_0013:  newobj     ""decimal?..ctor(decimal)""
  IL_0018:  constrained. ""T""
  IL_001e:  callvirt   ""void I.X.set""
  IL_0023:  ldloc.1
  IL_0024:  stloc.0
  IL_0025:  ldloca.s   V_0
  IL_0027:  constrained. ""T""
  IL_002d:  callvirt   ""decimal? I.X.get""
  IL_0032:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_AssignWriteOnlyProperty()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int x;
    public int Prop { set { x = value; } }

    public static void Main()
    {
        var i = new MemberInitializerTest() { Prop = 1 };
        System.Console.WriteLine(i.x);
    }
}
";
            string expectedOutput = "1";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("MemberInitializerTest.Main", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  newobj     ""MemberInitializerTest..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   ""void MemberInitializerTest.Prop.set""
  IL_000c:  ldfld      ""int MemberInitializerTest.x""
  IL_0011:  call       ""void System.Console.WriteLine(int)""
  IL_0016:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_AssignMembersOfReadOnlyField()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int x;
    public int y { get; set; }
}

public class Test
{
    public readonly MemberInitializerTest m = new MemberInitializerTest();
    
    public static void Main()
    {
        var i = new Test() { m = { x = 1, y = 2 } };
        System.Console.WriteLine(i.m.x);
        System.Console.WriteLine(i.m.y);
    }
}
";
            string expectedOutput = @"1
2";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       61 (0x3d)
  .maxstack  3
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""MemberInitializerTest Test.m""
  IL_000b:  ldc.i4.1
  IL_000c:  stfld      ""int MemberInitializerTest.x""
  IL_0011:  dup
  IL_0012:  ldfld      ""MemberInitializerTest Test.m""
  IL_0017:  ldc.i4.2
  IL_0018:  callvirt   ""void MemberInitializerTest.y.set""
  IL_001d:  dup
  IL_001e:  ldfld      ""MemberInitializerTest Test.m""
  IL_0023:  ldfld      ""int MemberInitializerTest.x""
  IL_0028:  call       ""void System.Console.WriteLine(int)""
  IL_002d:  ldfld      ""MemberInitializerTest Test.m""
  IL_0032:  callvirt   ""int MemberInitializerTest.y.get""
  IL_0037:  call       ""void System.Console.WriteLine(int)""
  IL_003c:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_AssignFieldWithSameNameAsLocal()
        {
            var source = @"
public class MemberInitializerTest
{   
    public int x;
    public static void Main()
    {
        int x = 1;
        MemberInitializerTest m = new MemberInitializerTest() { x = x + 1 };
        System.Console.WriteLine(x);
        System.Console.WriteLine(m.x);
    }
}
";
            string expectedOutput = @"1
2";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("MemberInitializerTest.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  4
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  newobj     ""MemberInitializerTest..ctor()""
  IL_0007:  dup
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  add
  IL_000b:  stfld      ""int MemberInitializerTest.x""
  IL_0010:  ldloc.0
  IL_0011:  call       ""void System.Console.WriteLine(int)""
  IL_0016:  ldfld      ""int MemberInitializerTest.x""
  IL_001b:  call       ""void System.Console.WriteLine(int)""
  IL_0020:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_AssignmentOrderingIsPreserved()
        {
            var source = @"
public class MemberInitializerTest
{
    public int x, y, z;

    public static void Main()
    {
        Foo();
    }

    public static void Foo(MemberInitializerTest nullArg = null)
    {
        MemberInitializerTest m = new MemberInitializerTest() { x = -1, y = -1, z = -1 };

        try
        {
            m = new MemberInitializerTest { x = Bar(1), y = nullArg.y, z = Bar(3) };
        }
        catch(System.NullReferenceException)
        {
        }

        System.Console.WriteLine(m.x);
        System.Console.WriteLine(m.y);
        System.Console.WriteLine(m.z);   
    }

    public static int Bar(int i)
    {
        System.Console.WriteLine(i);
        return i;
    }
}
";
            string expectedOutput = @"1
-1
-1
-1";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("MemberInitializerTest.Foo", @"
{
  // Code size      108 (0x6c)
  .maxstack  3
  .locals init (MemberInitializerTest V_0) //m
  IL_0000:  newobj     ""MemberInitializerTest..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.m1
  IL_0007:  stfld      ""int MemberInitializerTest.x""
  IL_000c:  dup
  IL_000d:  ldc.i4.m1
  IL_000e:  stfld      ""int MemberInitializerTest.y""
  IL_0013:  dup
  IL_0014:  ldc.i4.m1
  IL_0015:  stfld      ""int MemberInitializerTest.z""
  IL_001a:  stloc.0
  .try
{
  IL_001b:  newobj     ""MemberInitializerTest..ctor()""
  IL_0020:  dup
  IL_0021:  ldc.i4.1
  IL_0022:  call       ""int MemberInitializerTest.Bar(int)""
  IL_0027:  stfld      ""int MemberInitializerTest.x""
  IL_002c:  dup
  IL_002d:  ldarg.0
  IL_002e:  ldfld      ""int MemberInitializerTest.y""
  IL_0033:  stfld      ""int MemberInitializerTest.y""
  IL_0038:  dup
  IL_0039:  ldc.i4.3
  IL_003a:  call       ""int MemberInitializerTest.Bar(int)""
  IL_003f:  stfld      ""int MemberInitializerTest.z""
  IL_0044:  stloc.0
  IL_0045:  leave.s    IL_004a
}
  catch System.NullReferenceException
{
  IL_0047:  pop
  IL_0048:  leave.s    IL_004a
}
  IL_004a:  ldloc.0
  IL_004b:  ldfld      ""int MemberInitializerTest.x""
  IL_0050:  call       ""void System.Console.WriteLine(int)""
  IL_0055:  ldloc.0
  IL_0056:  ldfld      ""int MemberInitializerTest.y""
  IL_005b:  call       ""void System.Console.WriteLine(int)""
  IL_0060:  ldloc.0
  IL_0061:  ldfld      ""int MemberInitializerTest.z""
  IL_0066:  call       ""void System.Console.WriteLine(int)""
  IL_006b:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_NestedObjectInitializerExpression()
        {
            var source = @"
public class MemberInitializerTest
{
    public int x;
    public int y { get; set; }
}

public class Test
{   
    public int x;
    public int y { get; set; }
    public MemberInitializerTest z;

    public static void Main()
    {
        var i = new Test() { x = 1, y = 2, z = new MemberInitializerTest() { x = 3, y = 4 } };
        System.Console.WriteLine(i.x);
        System.Console.WriteLine(i.y);
        System.Console.WriteLine(i.z.x);
        System.Console.WriteLine(i.z.y);
    }
}
";
            string expectedOutput = @"1
2
3
4";
            var compVerifier = CompileAndVerify(source, emitOptions: TestEmitters.CCI, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       98 (0x62)
  .maxstack  5
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  stfld      ""int Test.x""
  IL_000c:  dup
  IL_000d:  ldc.i4.2
  IL_000e:  callvirt   ""void Test.y.set""
  IL_0013:  dup
  IL_0014:  newobj     ""MemberInitializerTest..ctor()""
  IL_0019:  dup
  IL_001a:  ldc.i4.3
  IL_001b:  stfld      ""int MemberInitializerTest.x""
  IL_0020:  dup
  IL_0021:  ldc.i4.4
  IL_0022:  callvirt   ""void MemberInitializerTest.y.set""
  IL_0027:  stfld      ""MemberInitializerTest Test.z""
  IL_002c:  dup
  IL_002d:  ldfld      ""int Test.x""
  IL_0032:  call       ""void System.Console.WriteLine(int)""
  IL_0037:  dup
  IL_0038:  callvirt   ""int Test.y.get""
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  dup
  IL_0043:  ldfld      ""MemberInitializerTest Test.z""
  IL_0048:  ldfld      ""int MemberInitializerTest.x""
  IL_004d:  call       ""void System.Console.WriteLine(int)""
  IL_0052:  ldfld      ""MemberInitializerTest Test.z""
  IL_0057:  callvirt   ""int MemberInitializerTest.y.get""
  IL_005c:  call       ""void System.Console.WriteLine(int)""
  IL_0061:  ret
}");
        }

        [Fact()]
        public void ObjectInitializerTest_NestedObjectInitializer_InitializerValue()
        {
            var source = @"
public class MemberInitializerTest
{
    public int x;
    public int y { get; set; }
}

public class Test
{   
    public int x;
    public int y { get; set; }
    public readonly MemberInitializerTest z = new MemberInitializerTest();

    public static void Main()
    {
        var i = new Test() { x = 1, y = 2, z = { x = 3, y = 4 } };
        System.Console.WriteLine(i.x);
        System.Console.WriteLine(i.y);
        System.Console.WriteLine(i.z.x);
        System.Console.WriteLine(i.z.y);
    }
}
";
            string expectedOutput = @"1
2
3
4";
            var compVerifier = CompileAndVerify(source, emitOptions: TestEmitters.CCI, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       97 (0x61)
  .maxstack  3
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  stfld      ""int Test.x""
  IL_000c:  dup
  IL_000d:  ldc.i4.2
  IL_000e:  callvirt   ""void Test.y.set""
  IL_0013:  dup
  IL_0014:  ldfld      ""MemberInitializerTest Test.z""
  IL_0019:  ldc.i4.3
  IL_001a:  stfld      ""int MemberInitializerTest.x""
  IL_001f:  dup
  IL_0020:  ldfld      ""MemberInitializerTest Test.z""
  IL_0025:  ldc.i4.4
  IL_0026:  callvirt   ""void MemberInitializerTest.y.set""
  IL_002b:  dup
  IL_002c:  ldfld      ""int Test.x""
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  dup
  IL_0037:  callvirt   ""int Test.y.get""
  IL_003c:  call       ""void System.Console.WriteLine(int)""
  IL_0041:  dup
  IL_0042:  ldfld      ""MemberInitializerTest Test.z""
  IL_0047:  ldfld      ""int MemberInitializerTest.x""
  IL_004c:  call       ""void System.Console.WriteLine(int)""
  IL_0051:  ldfld      ""MemberInitializerTest Test.z""
  IL_0056:  callvirt   ""int MemberInitializerTest.y.get""
  IL_005b:  call       ""void System.Console.WriteLine(int)""
  IL_0060:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_NestedCollectionInitializerExpression()
        {
            var source = @"
using System;
using System.Collections.Generic;

public class MemberInitializerTest
{
    public List<int> x = new List<int>();
    public List<int> y { get { return x; } set { x = value; } }
}

public class Test
{   
    public List<int> x = new List<int>();
    public List<int> y { get { return x; } set { x = value; } }
    
    public MemberInitializerTest z;

    public static void Main()
    {
        var i = new Test() { x = { 1 }, y = { 2 }, z = new MemberInitializerTest() { x = { 3 }, y = { 4 } } };
        DisplayCollection(i.x);
        DisplayCollection(i.y);
        DisplayCollection(i.z.x);
        DisplayCollection(i.z.y);
    }

    public static void DisplayCollection<T>(IEnumerable<T> collection)
    {
        foreach (var i in collection)
        {
            Console.WriteLine(i);
        }
    }
}
";
            string expectedOutput = @"1
2
1
2
3
4
3
4";
            var compVerifier = CompileAndVerify(source, emitOptions: TestEmitters.CCI, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size      118 (0x76)
  .maxstack  5
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""System.Collections.Generic.List<int> Test.x""
  IL_000b:  ldc.i4.1
  IL_000c:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0011:  dup
  IL_0012:  callvirt   ""System.Collections.Generic.List<int> Test.y.get""
  IL_0017:  ldc.i4.2
  IL_0018:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_001d:  dup
  IL_001e:  newobj     ""MemberInitializerTest..ctor()""
  IL_0023:  dup
  IL_0024:  ldfld      ""System.Collections.Generic.List<int> MemberInitializerTest.x""
  IL_0029:  ldc.i4.3
  IL_002a:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_002f:  dup
  IL_0030:  callvirt   ""System.Collections.Generic.List<int> MemberInitializerTest.y.get""
  IL_0035:  ldc.i4.4
  IL_0036:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_003b:  stfld      ""MemberInitializerTest Test.z""
  IL_0040:  dup
  IL_0041:  ldfld      ""System.Collections.Generic.List<int> Test.x""
  IL_0046:  call       ""void Test.DisplayCollection<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_004b:  dup
  IL_004c:  callvirt   ""System.Collections.Generic.List<int> Test.y.get""
  IL_0051:  call       ""void Test.DisplayCollection<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_0056:  dup
  IL_0057:  ldfld      ""MemberInitializerTest Test.z""
  IL_005c:  ldfld      ""System.Collections.Generic.List<int> MemberInitializerTest.x""
  IL_0061:  call       ""void Test.DisplayCollection<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_0066:  ldfld      ""MemberInitializerTest Test.z""
  IL_006b:  callvirt   ""System.Collections.Generic.List<int> MemberInitializerTest.y.get""
  IL_0070:  call       ""void Test.DisplayCollection<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_0075:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_NestedCollectionInitializer_InitializerValue()
        {
            var source = @"
using System;
using System.Collections.Generic;

public class MemberInitializerTest
{
    public List<int> x = new List<int>();
    public List<int> y { get { return x; } set { x = value; } }
}

public class Test
{   
    public List<int> x = new List<int>();
    public List<int> y { get { return x; } set { x = value; } }
    
    public MemberInitializerTest z = new MemberInitializerTest();

    public static void Main()
    {
        var i = new Test() { x = { 1 }, y = { 2 }, z = { x = { 3 }, y = { 4 } } };
        DisplayCollection(i.x);
        DisplayCollection(i.y);
        DisplayCollection(i.z.x);
        DisplayCollection(i.z.y);
    }

    public static void DisplayCollection<T>(IEnumerable<T> collection)
    {
        foreach (var i in collection)
        {
            Console.WriteLine(i);
        }
    }
}
";
            string expectedOutput = @"1
2
1
2
3
4
3
4";
            var compVerifier = CompileAndVerify(source, emitOptions: TestEmitters.CCI, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size      117 (0x75)
  .maxstack  3
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""System.Collections.Generic.List<int> Test.x""
  IL_000b:  ldc.i4.1
  IL_000c:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0011:  dup
  IL_0012:  callvirt   ""System.Collections.Generic.List<int> Test.y.get""
  IL_0017:  ldc.i4.2
  IL_0018:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_001d:  dup
  IL_001e:  ldfld      ""MemberInitializerTest Test.z""
  IL_0023:  ldfld      ""System.Collections.Generic.List<int> MemberInitializerTest.x""
  IL_0028:  ldc.i4.3
  IL_0029:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_002e:  dup
  IL_002f:  ldfld      ""MemberInitializerTest Test.z""
  IL_0034:  callvirt   ""System.Collections.Generic.List<int> MemberInitializerTest.y.get""
  IL_0039:  ldc.i4.4
  IL_003a:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_003f:  dup
  IL_0040:  ldfld      ""System.Collections.Generic.List<int> Test.x""
  IL_0045:  call       ""void Test.DisplayCollection<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_004a:  dup
  IL_004b:  callvirt   ""System.Collections.Generic.List<int> Test.y.get""
  IL_0050:  call       ""void Test.DisplayCollection<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_0055:  dup
  IL_0056:  ldfld      ""MemberInitializerTest Test.z""
  IL_005b:  ldfld      ""System.Collections.Generic.List<int> MemberInitializerTest.x""
  IL_0060:  call       ""void Test.DisplayCollection<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_0065:  ldfld      ""MemberInitializerTest Test.z""
  IL_006a:  callvirt   ""System.Collections.Generic.List<int> MemberInitializerTest.y.get""
  IL_006f:  call       ""void Test.DisplayCollection<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_0074:  ret
}");
        }

        [WorkItem(529272, "DevDiv")]
        [Fact()]
        public void ObjectInitializerFieldlikeEvent()
        {
            var source = @"
public delegate void D();

public struct MemberInitializerTest
{
    public event D z;

    public static void Main()
    {
        var i = new MemberInitializerTest() { z = null };
    }
}";
            var compVerifier = CompileAndVerify(source, expectedOutput: "");
            compVerifier.VerifyIL("MemberInitializerTest.Main", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (MemberInitializerTest V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""MemberInitializerTest""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldnull
  IL_000b:  stfld      ""D MemberInitializerTest.z""
  IL_0010:  ret
}");
        }

        [Fact]
        public void ObjectInitializerTest_UseVariableBeingAssignedInObjectInitializer()
        {
            var source = @"
public class Test
{   
    public int x, y;
    public static void Main()
    {
        Test m = new Test() { x = Foo(out m), y = m.x };
        System.Console.WriteLine(m.x);  // Print 1
        System.Console.WriteLine(m.y);  // Print 0
    }

    public static int Foo(out Test m)
    {
        m = new Test() { x = 0 };
        return 1;
    }
}
";
            string expectedOutput = @"1
0";
            var compVerifier = CompileAndVerify(source, emitOptions: TestEmitters.CCI, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (Test V_0) //m
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  dup
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""int Test.Foo(out Test)""
  IL_000d:  stfld      ""int Test.x""
  IL_0012:  dup
  IL_0013:  ldloc.0
  IL_0014:  ldfld      ""int Test.x""
  IL_0019:  stfld      ""int Test.y""
  IL_001e:  stloc.0
  IL_001f:  ldloc.0
  IL_0020:  ldfld      ""int Test.x""
  IL_0025:  call       ""void System.Console.WriteLine(int)""
  IL_002a:  ldloc.0
  IL_002b:  ldfld      ""int Test.y""
  IL_0030:  call       ""void System.Console.WriteLine(int)""
  IL_0035:  ret
}");
        }

        [Fact]
        public void DictionaryInitializerTest001()
        {
            var source = @"
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int>() {[""aaa""] = 3};
        System.Console.WriteLine(x[""aaa""]);
    }
}
";
            string expectedOutput = @"3";

            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Program.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  4
  IL_0000:  newobj     ""System.Collections.Generic.Dictionary<string, int>..ctor()""
  IL_0005:  dup
  IL_0006:  ldstr      ""aaa""
  IL_000b:  ldc.i4.3
  IL_000c:  callvirt   ""void System.Collections.Generic.Dictionary<string, int>.this[string].set""
  IL_0011:  ldstr      ""aaa""
  IL_0016:  callvirt   ""int System.Collections.Generic.Dictionary<string, int>.this[string].get""
  IL_001b:  call       ""void System.Console.WriteLine(int)""
  IL_0020:  ret
}
");
        }

        [Fact]
        public void DictionaryInitializerTest002()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var l = new cls1() { [""aaa""] = { 1, 2 }, [""bbb""] = { 42 } };
        System.Console.Write(l[""bbb""][0]);
        System.Console.Write(l[""aaa""][1]);
    }

    class cls1
    {
        private Dictionary<string, List<int>> dict = new Dictionary<string, List<int>>();

        public dynamic this[string value]
        {
            get
            {
                List<int> member;
                if (dict.TryGetValue(value, out member))
                {
                    return member;
                }

                return dict[value] = new List<int>();
            }
        }
    }
}
";
            string expectedOutput = @"422";

            var compVerifier = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, expectedOutput: expectedOutput);
        }

        [Fact]
        public void DictionaryInitializerTest003()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var l = new Cls1()
        {
            [""aaa""] =
            {
                [""x""] = 1,
                [""y""] = 2
            },
            [""bbb""] =
            {
                [""z""] = 42
            }
        };

        System.Console.Write(l[""bbb""][""z""]);
        System.Console.Write(l[""aaa""][""y""]);
    }

    class Cls1
    {
        private Dictionary<string, Dictionary<string, int>> dict = 
            new Dictionary<string, Dictionary<string, int>>();

        public Dictionary<string, int> this[string arg]
        {
            get
            {
                Dictionary<string, int> member;
                if (dict.TryGetValue(arg, out member))
                {
                    return member;
                }

                return dict[arg] = new Dictionary<string, int>();
            }
        }
    }
}
";
            string expectedOutput = @"422";

            var compVerifier = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, expectedOutput: expectedOutput);
        }

        [Fact]
        public void DictionaryInitializerTest004()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var l = new Cls1()
        {
            [""aaa""] =
            {
                [""x""] = 1,
                [""y""] = 2
            },
            [""bbb""] =
            {
                [""z""] = 42
            }
        };

        System.Console.Write(l[""bbb""][""z""]);
        System.Console.Write(l[""aaa""][""y""]);
    }

    class Cls1
    {
        private Dictionary<string, Dictionary<string, int>> dict = 
            new Dictionary<string, Dictionary<string, int>>();

        public dynamic this[string arg]
        {
            get
            {
                Dictionary<string, int> member;
                if (dict.TryGetValue(arg, out member))
                {
                    return member;
                }

                return dict[arg] = new Dictionary<string, int>();
            }
        }
    }
}
";
            string expectedOutput = @"422";

            var compVerifier = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }, expectedOutput: expectedOutput);
        }

        #endregion

        #region "Collection Initializer Tests"

        [Fact]
        public void CollectionInitializerTest_GenericList()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        List<int> list = new List<int>() { 1, 2, 3, 4, 5 };
        DisplayCollection(list);
        return 0;
    }

    public static void DisplayCollection<T>(IEnumerable<T> collection)
    {
        foreach (var i in collection)
        {
            Console.WriteLine(i);
        }
    }
}
";
            string expectedOutput = @"1
2
3
4
5";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       47 (0x2f)
  .maxstack  3
  IL_0000:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_000c:  dup
  IL_000d:  ldc.i4.2
  IL_000e:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0013:  dup
  IL_0014:  ldc.i4.3
  IL_0015:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_001a:  dup
  IL_001b:  ldc.i4.4
  IL_001c:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0021:  dup
  IL_0022:  ldc.i4.5
  IL_0023:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0028:  call       ""void Test.DisplayCollection<int>(System.Collections.Generic.IEnumerable<int>)""
  IL_002d:  ldc.i4.0
  IL_002e:  ret
}");
        }

        [Fact]
        public void CollectionInitializerTest_GenericList_WithComplexElementInitializer()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        List<long> list = new List<long>() { 1, 2, { 4L }, { 9 }, 3L };
        DisplayCollection(list);
        return 0;
    }

    public static void DisplayCollection<T>(IEnumerable<T> collection)
    {
        foreach (var i in collection)
        {
            Console.WriteLine(i);
        }
    }
}
";
            string expectedOutput = @"1
2
4
9
3";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       53 (0x35)
  .maxstack  3
  IL_0000:  newobj     ""System.Collections.Generic.List<long>..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  conv.i8
  IL_0008:  callvirt   ""void System.Collections.Generic.List<long>.Add(long)""
  IL_000d:  dup
  IL_000e:  ldc.i4.2
  IL_000f:  conv.i8
  IL_0010:  callvirt   ""void System.Collections.Generic.List<long>.Add(long)""
  IL_0015:  dup
  IL_0016:  ldc.i4.4
  IL_0017:  conv.i8
  IL_0018:  callvirt   ""void System.Collections.Generic.List<long>.Add(long)""
  IL_001d:  dup
  IL_001e:  ldc.i4.s   9
  IL_0020:  conv.i8
  IL_0021:  callvirt   ""void System.Collections.Generic.List<long>.Add(long)""
  IL_0026:  dup
  IL_0027:  ldc.i4.3
  IL_0028:  conv.i8
  IL_0029:  callvirt   ""void System.Collections.Generic.List<long>.Add(long)""
  IL_002e:  call       ""void Test.DisplayCollection<long>(System.Collections.Generic.IEnumerable<long>)""
  IL_0033:  ldc.i4.0
  IL_0034:  ret
}");
        }

        [Fact]
        public void CollectionInitializerTest_TypeParameter()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class A:IEnumerable
{
	public static List<int> list = new List<int>();
	public void Add(int i)
	{
		list.Add(i);	
	}

    public IEnumerator GetEnumerator()
	{
        for (int i = 0; i < list.Count; i++)
		    yield return list[i];
    }
}

class C<T> where T: A, new()
{
	public void M()
	{
		T t = new T {1, 2, 3, 4, 5};

        foreach (var x in t)
        {
            Console.WriteLine(x);
        }
	}
}

class Test
{
	static void Main()
	{
		C<A> testC = new C<A>();
		testC.M();
	}
}
";
            string expectedOutput = @"1
2
3
4
5";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C<T>.M", @"
{
  // Code size      117 (0x75)
  .maxstack  3
  .locals init (System.Collections.IEnumerator V_0,
  System.IDisposable V_1)
  IL_0000:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0005:  dup
  IL_0006:  box        ""T""
  IL_000b:  ldc.i4.1
  IL_000c:  callvirt   ""void A.Add(int)""
  IL_0011:  dup
  IL_0012:  box        ""T""
  IL_0017:  ldc.i4.2
  IL_0018:  callvirt   ""void A.Add(int)""
  IL_001d:  dup
  IL_001e:  box        ""T""
  IL_0023:  ldc.i4.3
  IL_0024:  callvirt   ""void A.Add(int)""
  IL_0029:  dup
  IL_002a:  box        ""T""
  IL_002f:  ldc.i4.4
  IL_0030:  callvirt   ""void A.Add(int)""
  IL_0035:  dup
  IL_0036:  box        ""T""
  IL_003b:  ldc.i4.5
  IL_003c:  callvirt   ""void A.Add(int)""
  IL_0041:  box        ""T""
  IL_0046:  callvirt   ""System.Collections.IEnumerator A.GetEnumerator()""
  IL_004b:  stloc.0
  .try
{
  IL_004c:  br.s       IL_0059
  IL_004e:  ldloc.0
  IL_004f:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_0054:  call       ""void System.Console.WriteLine(object)""
  IL_0059:  ldloc.0
  IL_005a:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_005f:  brtrue.s   IL_004e
  IL_0061:  leave.s    IL_0074
}
  finally
{
  IL_0063:  ldloc.0
  IL_0064:  isinst     ""System.IDisposable""
  IL_0069:  stloc.1
  IL_006a:  ldloc.1
  IL_006b:  brfalse.s  IL_0073
  IL_006d:  ldloc.1
  IL_006e:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0073:  endfinally
}
  IL_0074:  ret
}");
        }

        [Fact]
        public void CollectionInitializerTest_InitializerTypeImplementsIEnumerable_ClassType()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        B coll = new B { 1, 2, { 4L }, { 9 }, 3L };
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
            Console.WriteLine(collection.Current);
        }
    }
}

public class B : IEnumerable
{
    List<object> list = new List<object>();

    public void Add(long i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            string expectedOutput = @"1
2
4
9
3";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       58 (0x3a)
  .maxstack  3
  IL_0000:  newobj     ""B..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  conv.i8
  IL_0008:  callvirt   ""void B.Add(long)""
  IL_000d:  dup
  IL_000e:  ldc.i4.2
  IL_000f:  conv.i8
  IL_0010:  callvirt   ""void B.Add(long)""
  IL_0015:  dup
  IL_0016:  ldc.i4.4
  IL_0017:  conv.i8
  IL_0018:  callvirt   ""void B.Add(long)""
  IL_001d:  dup
  IL_001e:  ldc.i4.s   9
  IL_0020:  conv.i8
  IL_0021:  callvirt   ""void B.Add(long)""
  IL_0026:  dup
  IL_0027:  ldc.i4.3
  IL_0028:  conv.i8
  IL_0029:  callvirt   ""void B.Add(long)""
  IL_002e:  callvirt   ""System.Collections.IEnumerator B.GetEnumerator()""
  IL_0033:  call       ""void Test.DisplayCollection(System.Collections.IEnumerator)""
  IL_0038:  ldc.i4.0
  IL_0039:  ret
}");
        }

        [Fact]
        public void CollectionInitializerTest_InitializerTypeImplementsIEnumerable_StructType()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        B coll = new B(1) { 2, { 4L }, { 9 }, 3L };
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
            Console.WriteLine(collection.Current);
        }
    }
}

public struct B : IEnumerable
{
    List<object> list;

    public B(long i)
    {
        list = new List<object>();
        Add(i);
    }

    public void Add(long i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            string expectedOutput = @"1
2
4
9
3";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       62 (0x3e)
  .maxstack  2
  .locals init (B V_0, //coll
  B V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  ldc.i4.1
  IL_0003:  conv.i8
  IL_0004:  call       ""B..ctor(long)""
  IL_0009:  ldloca.s   V_1
  IL_000b:  ldc.i4.2
  IL_000c:  conv.i8
  IL_000d:  call       ""void B.Add(long)""
  IL_0012:  ldloca.s   V_1
  IL_0014:  ldc.i4.4
  IL_0015:  conv.i8
  IL_0016:  call       ""void B.Add(long)""
  IL_001b:  ldloca.s   V_1
  IL_001d:  ldc.i4.s   9
  IL_001f:  conv.i8
  IL_0020:  call       ""void B.Add(long)""
  IL_0025:  ldloca.s   V_1
  IL_0027:  ldc.i4.3
  IL_0028:  conv.i8
  IL_0029:  call       ""void B.Add(long)""
  IL_002e:  ldloc.1
  IL_002f:  stloc.0
  IL_0030:  ldloca.s   V_0
  IL_0032:  call       ""System.Collections.IEnumerator B.GetEnumerator()""
  IL_0037:  call       ""void Test.DisplayCollection(System.Collections.IEnumerator)""
  IL_003c:  ldc.i4.0
  IL_003d:  ret
}");
        }

        [Fact]
        public void CollectionInitializerTest_InitializerTypeImplementsIEnumerable_MultipleAddOverloads()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        B coll = new B { 1, 2, { 4L }, { 9 }, 3L };
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
            Console.WriteLine(collection.Current);
        }
    }
}

public class B : IEnumerable
{
    List<object> list = new List<object>();

    public B()
    {
    }

    public B(int i)
    {
    }

    public void Add(int i)
    {
        list.Add(i);
    }

    public void Add(long i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            string expectedOutput = @"1
2
4
9
3";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       55 (0x37)
  .maxstack  3
  IL_0000:  newobj     ""B..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   ""void B.Add(int)""
  IL_000c:  dup
  IL_000d:  ldc.i4.2
  IL_000e:  callvirt   ""void B.Add(int)""
  IL_0013:  dup
  IL_0014:  ldc.i4.4
  IL_0015:  conv.i8
  IL_0016:  callvirt   ""void B.Add(long)""
  IL_001b:  dup
  IL_001c:  ldc.i4.s   9
  IL_001e:  callvirt   ""void B.Add(int)""
  IL_0023:  dup
  IL_0024:  ldc.i4.3
  IL_0025:  conv.i8
  IL_0026:  callvirt   ""void B.Add(long)""
  IL_002b:  callvirt   ""System.Collections.IEnumerator B.GetEnumerator()""
  IL_0030:  call       ""void Test.DisplayCollection(System.Collections.IEnumerator)""
  IL_0035:  ldc.i4.0
  IL_0036:  ret
}");
        }

        [Fact]
        public void CollectionInitializerTest_InitializerTypeImplementsIEnumerable_AddOverload_OptionalArgument()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        D coll = new D { 1, { 2 }, { 3, (float?)4.4 } };
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
            if (collection.Current.GetType() == typeof(float))
                Console.WriteLine(((float)collection.Current).ToString(System.Globalization.CultureInfo.InvariantCulture));
            else
                Console.WriteLine(collection.Current);
        }
    }
}

public class D : IEnumerable
{
    List<object> list = new List<object>();

    public D() { }

    public void Add(int i, float? j = null)
    {
        list.Add(i);
        if (j.HasValue)
        {
            list.Add(j);
        }
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            string expectedOutput = @"1
2
3
4.4";
            var compVerifier = CompileAndVerify(source, emitOptions: TestEmitters.CCI, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       71 (0x47)
  .maxstack  4
  .locals init (float? V_0)
  IL_0000:  newobj     ""D..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  ldloca.s   V_0
  IL_0009:  initobj    ""float?""
  IL_000f:  ldloc.0
  IL_0010:  callvirt   ""void D.Add(int, float?)""
  IL_0015:  dup
  IL_0016:  ldc.i4.2
  IL_0017:  ldloca.s   V_0
  IL_0019:  initobj    ""float?""
  IL_001f:  ldloc.0
  IL_0020:  callvirt   ""void D.Add(int, float?)""
  IL_0025:  dup
  IL_0026:  ldc.i4.3
  IL_0027:  ldc.r8     4.4
  IL_0030:  conv.r4
  IL_0031:  newobj     ""float?..ctor(float)""
  IL_0036:  callvirt   ""void D.Add(int, float?)""
  IL_003b:  callvirt   ""System.Collections.IEnumerator D.GetEnumerator()""
  IL_0040:  call       ""void Test.DisplayCollection(System.Collections.IEnumerator)""
  IL_0045:  ldc.i4.0
  IL_0046:  ret
}");
        }

        [Fact]
        public void CollectionInitializerTest_InitializerTypeImplementsIEnumerable_AddOverload_ParamsArrayArgument()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var implicitTypedArr = new[] { 7.7, 8.8 };
        D coll = new D { 1, { 2 }, { 3, 4.4 }, new double[] { 5, 6 }, implicitTypedArr, null };
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
           if (collection.Current.GetType() == typeof(double))
                Console.WriteLine(((double)collection.Current).ToString(System.Globalization.CultureInfo.InvariantCulture));
            else
                Console.WriteLine(collection.Current);
        }
    }
}

public class D : IEnumerable
{
    List<object> list = new List<object>();

    public D() { }

    public void Add(params double[] i)
    {
        if (i != null)
        {
            foreach (var x in i)
            {
                list.Add(x);
            }
        }
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            string expectedOutput = @"1
2
3
4.4
5
6
7.7
8.8";
            var compVerifier = CompileAndVerify(source, emitOptions: TestEmitters.CCI, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size      184 (0xb8)
  .maxstack  5
  .locals init (double[] V_0, //implicitTypedArr
  D V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""double""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.r8     7.7
  IL_0011:  stelem.r8
  IL_0012:  dup
  IL_0013:  ldc.i4.1
  IL_0014:  ldc.r8     8.8
  IL_001d:  stelem.r8
  IL_001e:  stloc.0
  IL_001f:  newobj     ""D..ctor()""
  IL_0024:  stloc.1
  IL_0025:  ldloc.1
  IL_0026:  ldc.i4.1
  IL_0027:  newarr     ""double""
  IL_002c:  dup
  IL_002d:  ldc.i4.0
  IL_002e:  ldc.r8     1
  IL_0037:  stelem.r8
  IL_0038:  callvirt   ""void D.Add(params double[])""
  IL_003d:  ldloc.1
  IL_003e:  ldc.i4.1
  IL_003f:  newarr     ""double""
  IL_0044:  dup
  IL_0045:  ldc.i4.0
  IL_0046:  ldc.r8     2
  IL_004f:  stelem.r8
  IL_0050:  callvirt   ""void D.Add(params double[])""
  IL_0055:  ldloc.1
  IL_0056:  ldc.i4.2
  IL_0057:  newarr     ""double""
  IL_005c:  dup
  IL_005d:  ldc.i4.0
  IL_005e:  ldc.r8     3
  IL_0067:  stelem.r8
  IL_0068:  dup
  IL_0069:  ldc.i4.1
  IL_006a:  ldc.r8     4.4
  IL_0073:  stelem.r8
  IL_0074:  callvirt   ""void D.Add(params double[])""
  IL_0079:  ldloc.1
  IL_007a:  ldc.i4.2
  IL_007b:  newarr     ""double""
  IL_0080:  dup
  IL_0081:  ldc.i4.0
  IL_0082:  ldc.r8     5
  IL_008b:  stelem.r8
  IL_008c:  dup
  IL_008d:  ldc.i4.1
  IL_008e:  ldc.r8     6
  IL_0097:  stelem.r8
  IL_0098:  callvirt   ""void D.Add(params double[])""
  IL_009d:  ldloc.1
  IL_009e:  ldloc.0
  IL_009f:  callvirt   ""void D.Add(params double[])""
  IL_00a4:  ldloc.1
  IL_00a5:  ldnull
  IL_00a6:  callvirt   ""void D.Add(params double[])""
  IL_00ab:  ldloc.1
  IL_00ac:  callvirt   ""System.Collections.IEnumerator D.GetEnumerator()""
  IL_00b1:  call       ""void Test.DisplayCollection(System.Collections.IEnumerator)""
  IL_00b6:  ldc.i4.0
  IL_00b7:  ret
}");
        }

        [Fact]
        public void CollectionInitializerTest_NestedCollectionInitializerExpression()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var listOfList = new List<List<int>>() { new List<int> { 1, 2, 3, 4, 5 }, new List<int> { 6, 7, 8, 9, 10 } };
        DisplayCollectionOfCollection(listOfList);
        return 0;
    }

    public static void DisplayCollectionOfCollection(IEnumerable<List<int>> collectionOfCollection)
    {
        foreach (var collection in collectionOfCollection)
        {
            foreach (var i in collection)
            {
                Console.WriteLine(i);
            }
        }
    }
}
";
            string expectedOutput = @"1
2
3
4
5
6
7
8
9
10";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size      106 (0x6a)
  .maxstack  5
  IL_0000:  newobj     ""System.Collections.Generic.List<System.Collections.Generic.List<int>>..ctor()""
  IL_0005:  dup
  IL_0006:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0012:  dup
  IL_0013:  ldc.i4.2
  IL_0014:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0019:  dup
  IL_001a:  ldc.i4.3
  IL_001b:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0020:  dup
  IL_0021:  ldc.i4.4
  IL_0022:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0027:  dup
  IL_0028:  ldc.i4.5
  IL_0029:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_002e:  callvirt   ""void System.Collections.Generic.List<System.Collections.Generic.List<int>>.Add(System.Collections.Generic.List<int>)""
  IL_0033:  dup
  IL_0034:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_0039:  dup
  IL_003a:  ldc.i4.6
  IL_003b:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0040:  dup
  IL_0041:  ldc.i4.7
  IL_0042:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0047:  dup
  IL_0048:  ldc.i4.8
  IL_0049:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_004e:  dup
  IL_004f:  ldc.i4.s   9
  IL_0051:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0056:  dup
  IL_0057:  ldc.i4.s   10
  IL_0059:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_005e:  callvirt   ""void System.Collections.Generic.List<System.Collections.Generic.List<int>>.Add(System.Collections.Generic.List<int>)""
  IL_0063:  call       ""void Test.DisplayCollectionOfCollection(System.Collections.Generic.IEnumerable<System.Collections.Generic.List<int>>)""
  IL_0068:  ldc.i4.0
  IL_0069:  ret
}");
        }

        [Fact]
        public void CollectionInitializerTest_NestedObjectAndCollectionInitializer()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var coll = new List<B> { new B(0) { list = new List<int>() { 1, 2, 3 } }, new B(1) { list = { 2, 3 } } };
        DisplayCollection(coll);
        return 0;
    }

    public static void DisplayCollection(IEnumerable<B> collection)
    {
        foreach (var i in collection)
        {
            i.Display();
        }
    }
}

public class B
{
    public List<int> list = new List<int>();
    public B() { }
    public B(int i) { list.Add(i); }

    public void Display()
    {
        foreach (var i in list)
        {
            Console.WriteLine(i);
        }
    }
}
";
            string expectedOutput = @"1
2
3
1
2
3";
            var compVerifier = CompileAndVerify(source, emitOptions: TestEmitters.CCI, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       92 (0x5c)
  .maxstack  7
  IL_0000:  newobj     ""System.Collections.Generic.List<B>..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.0
  IL_0007:  newobj     ""B..ctor(int)""
  IL_000c:  dup
  IL_000d:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_0012:  dup
  IL_0013:  ldc.i4.1
  IL_0014:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0019:  dup
  IL_001a:  ldc.i4.2
  IL_001b:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0020:  dup
  IL_0021:  ldc.i4.3
  IL_0022:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0027:  stfld      ""System.Collections.Generic.List<int> B.list""
  IL_002c:  callvirt   ""void System.Collections.Generic.List<B>.Add(B)""
  IL_0031:  dup
  IL_0032:  ldc.i4.1
  IL_0033:  newobj     ""B..ctor(int)""
  IL_0038:  dup
  IL_0039:  ldfld      ""System.Collections.Generic.List<int> B.list""
  IL_003e:  ldc.i4.2
  IL_003f:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0044:  dup
  IL_0045:  ldfld      ""System.Collections.Generic.List<int> B.list""
  IL_004a:  ldc.i4.3
  IL_004b:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_0050:  callvirt   ""void System.Collections.Generic.List<B>.Add(B)""
  IL_0055:  call       ""void Test.DisplayCollection(System.Collections.Generic.IEnumerable<B>)""
  IL_005a:  ldc.i4.0
  IL_005b:  ret
}");
        }
        
        [Fact]
        public void CollectionInitializerTest_CtorAddsToCollection()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        B coll = new B(1) { 2 };
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
            Console.WriteLine(collection.Current);
        }
    }
}

public class B : IEnumerable
{
    List<object> list = new List<object>();

    public B()
    {
    }

    public B(int i)
    {
        list.Add(i);
    }

    public void Add(long i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            string expectedOutput = @"1
2";
            var compVerifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  3
  IL_0000:  ldc.i4.1
  IL_0001:  newobj     ""B..ctor(int)""
  IL_0006:  dup
  IL_0007:  ldc.i4.2
  IL_0008:  conv.i8
  IL_0009:  callvirt   ""void B.Add(long)""
  IL_000e:  callvirt   ""System.Collections.IEnumerator B.GetEnumerator()""
  IL_0013:  call       ""void Test.DisplayCollection(System.Collections.IEnumerator)""
  IL_0018:  ldc.i4.0
  IL_0019:  ret
}");
        }

        [Fact]
        public void CollectionInitializerTest_NestedObjectAndCollectionInitializer_02()
        {
            // --------------------------------------------------------------------------------------------
            // SPEC:    7.6.10.3 Collection initializers
            // --------------------------------------------------------------------------------------------
            //
            // SPEC:    The following class represents a contact with a name and a list of phone numbers:
            //
            // SPEC:    public class Contact
            // SPEC:    {
            // SPEC:    	string name;
            // SPEC:    	List<string> phoneNumbers = new List<string>();
            // SPEC:    	public string Name { get { return name; } set { name = value; } }
            // SPEC:    	public List<string> PhoneNumbers { get { return phoneNumbers; } }
            // SPEC:    }
            //
            // SPEC:    A List<Contact> can be created and initialized as follows:
            //
            // SPEC:    var contacts = new List<Contact> {
            // SPEC:    	new Contact {
            // SPEC:    		Name = "Chris Smith",
            // SPEC:    		PhoneNumbers = { "206-555-0101", "425-882-8080" }
            // SPEC:    	},
            // SPEC:    	new Contact {
            // SPEC:    		Name = "Bob Harris",
            // SPEC:    		PhoneNumbers = { "650-555-0199" }
            // SPEC:    	}
            // SPEC:    };
            //
            // SPEC:    which has the same effect as
            //
            // SPEC:    var __clist = new List<Contact>();
            // SPEC:    Contact __c1 = new Contact();
            // SPEC:    __c1.Name = "Chris Smith";
            // SPEC:    __c1.PhoneNumbers.Add("206-555-0101");
            // SPEC:    __c1.PhoneNumbers.Add("425-882-8080");
            // SPEC:    __clist.Add(__c1);
            // SPEC:    Contact __c2 = new Contact();
            // SPEC:    __c2.Name = "Bob Harris";
            // SPEC:    __c2.PhoneNumbers.Add("650-555-0199");
            // SPEC:    __clist.Add(__c2);
            // SPEC:    var contacts = __clist;
            //
            // SPEC:    where __clist, __c1 and __c2 are temporary variables that are otherwise invisible and inaccessible.

            var source = @"
using System;
using System.Collections.Generic;
using System.Collections;

public class Contact
{
    string name;
    List<string> phoneNumbers = new List<string>();
    public string Name { get { return name; } set { name = value; } }
    public List<string> PhoneNumbers { get { return phoneNumbers; } }

    public void DisplayContact()
    {
        Console.WriteLine(""Name:"" + name);
        Console.WriteLine(""PH:"");
        foreach (var ph in phoneNumbers)
        {
            Console.WriteLine(ph);
        }
    }
}

class Test
{
    public static void Main()
    {
        var contacts = new List<Contact> {
	        new Contact {
		        Name = ""Chris Smith"",
		        PhoneNumbers = { ""206-555-0101"", ""425-882-8080"" }
    	    },
	        new Contact {
		        Name = ""Bob Harris"",
		        PhoneNumbers = { ""650-555-0199"" }
	        }
        };

        DisplayContacts(contacts);
    }

    public static void DisplayContacts(IEnumerable<Contact> contacts)
    {
        foreach (var contact in contacts)
        {
            contact.DisplayContact();
        }
    }
}
";
            string expectedOutput = @"Name:Chris Smith
PH:
206-555-0101
425-882-8080
Name:Bob Harris
PH:
650-555-0199";

            var compVerifier = CompileAndVerify(source, emitOptions: TestEmitters.CCI, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size      103 (0x67)
  .maxstack  5
  IL_0000:  newobj     ""System.Collections.Generic.List<Contact>..ctor()""
  IL_0005:  dup
  IL_0006:  newobj     ""Contact..ctor()""
  IL_000b:  dup
  IL_000c:  ldstr      ""Chris Smith""
  IL_0011:  callvirt   ""void Contact.Name.set""
  IL_0016:  dup
  IL_0017:  callvirt   ""System.Collections.Generic.List<string> Contact.PhoneNumbers.get""
  IL_001c:  ldstr      ""206-555-0101""
  IL_0021:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0026:  dup
  IL_0027:  callvirt   ""System.Collections.Generic.List<string> Contact.PhoneNumbers.get""
  IL_002c:  ldstr      ""425-882-8080""
  IL_0031:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0036:  callvirt   ""void System.Collections.Generic.List<Contact>.Add(Contact)""
  IL_003b:  dup
  IL_003c:  newobj     ""Contact..ctor()""
  IL_0041:  dup
  IL_0042:  ldstr      ""Bob Harris""
  IL_0047:  callvirt   ""void Contact.Name.set""
  IL_004c:  dup
  IL_004d:  callvirt   ""System.Collections.Generic.List<string> Contact.PhoneNumbers.get""
  IL_0052:  ldstr      ""650-555-0199""
  IL_0057:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_005c:  callvirt   ""void System.Collections.Generic.List<Contact>.Add(Contact)""
  IL_0061:  call       ""void Test.DisplayContacts(System.Collections.Generic.IEnumerable<Contact>)""
  IL_0066:  ret
}");
        }

        [Fact]
        public void PartialAddMethods()
        {
            var source = @"
using System;
using System.Collections;

partial class C : IEnumerable
{
    public IEnumerator GetEnumerator() { return null; }

    partial void Add(int i);
    partial void Add(char c);

    partial void Add(char c) { }

    static void Main()
    {
        Console.WriteLine(new C { 1, 2, 3 }); // all removed
        Console.WriteLine(new C { 1, 'b', 3 }); // some removed
        Console.WriteLine(new C { 'a', 'b', 'c' }); // none removed
    }
}";
            CompileAndVerify(source).VerifyIL("C.Main", @"
{
  // Code size       63 (0x3f)
  .maxstack  3
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  call       ""void System.Console.WriteLine(object)""
  IL_000a:  newobj     ""C..ctor()""
  IL_000f:  dup
  IL_0010:  ldc.i4.s   98
  IL_0012:  callvirt   ""void C.Add(char)""
  IL_0017:  call       ""void System.Console.WriteLine(object)""
  IL_001c:  newobj     ""C..ctor()""
  IL_0021:  dup
  IL_0022:  ldc.i4.s   97
  IL_0024:  callvirt   ""void C.Add(char)""
  IL_0029:  dup
  IL_002a:  ldc.i4.s   98
  IL_002c:  callvirt   ""void C.Add(char)""
  IL_0031:  dup
  IL_0032:  ldc.i4.s   99
  IL_0034:  callvirt   ""void C.Add(char)""
  IL_0039:  call       ""void System.Console.WriteLine(object)""
  IL_003e:  ret
}
");
        }

        #endregion
    }
}