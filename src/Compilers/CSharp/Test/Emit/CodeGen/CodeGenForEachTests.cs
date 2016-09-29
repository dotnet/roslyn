// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenForEachTests : CSharpTestBase
    {
        [Fact]
        public void TestForEachArray()
        {
            var source = @"
class C
{
    static void Main()
    {
        int[] array = new int[3];
        array[0] = 1;
        array[1] = 2;
        array[2] = 3;

        foreach (var x in array)
        {
            System.Console.WriteLine(x);
        }
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            // Lowered to a for-loop from 0 to length.
            // No disposal required.
            compilation.VerifyIL("C.Main", @"{
  // Code size       42 (0x2a)
  .maxstack  4
  .locals init (int[] V_0,
  int V_1)
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
  IL_0012:  stloc.0
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.1
  IL_0015:  br.s       IL_0023
  IL_0017:  ldloc.0
  IL_0018:  ldloc.1
  IL_0019:  ldelem.i4
  IL_001a:  call       ""void System.Console.WriteLine(int)""
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4.1
  IL_0021:  add
  IL_0022:  stloc.1
  IL_0023:  ldloc.1
  IL_0024:  ldloc.0
  IL_0025:  ldlen
  IL_0026:  conv.i4
  IL_0027:  blt.s      IL_0017
  IL_0029:  ret
}");
        }

        [WorkItem(544937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544937")]
        [Fact]
        public void TestForEachMultiDimensionalArray()
        {
            var source = @"
class C
{
    static void Main()
    {
        double[,] values = {
            { 1.2, 2.3, 3.4, 4.5 },
            { 5.6, 6.7, 7.8, 8.9 },
        };

        foreach (var x in values)
        {
            System.Console.WriteLine(x);
        }
    }
}";
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe.WithModuleName("MODULE"), expectedOutput: @"
1.2
2.3
3.4
4.5
5.6
6.7
7.8
8.9");

            compilation.VerifyIL("C.Main", @"
{
  // Code size       90 (0x5a)
  .maxstack  3
  .locals init (double[,] V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4)
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.4
  IL_0002:  newobj     ""double[*,*]..ctor""
  IL_0007:  dup
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=64 <PrivateImplementationDetails>.E19C080DB8DAB85AF7CA3EF40FFB01B0778F9D25""
  IL_000d:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.0
  IL_0015:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_001a:  stloc.1
  IL_001b:  ldloc.0
  IL_001c:  ldc.i4.1
  IL_001d:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0022:  stloc.2
  IL_0023:  ldloc.0
  IL_0024:  ldc.i4.0
  IL_0025:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_002a:  stloc.3
  IL_002b:  br.s       IL_0055
  IL_002d:  ldloc.0
  IL_002e:  ldc.i4.1
  IL_002f:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0034:  stloc.s    V_4
  IL_0036:  br.s       IL_004c
  IL_0038:  ldloc.0
  IL_0039:  ldloc.3
  IL_003a:  ldloc.s    V_4
  IL_003c:  call       ""double[*,*].Get""
  IL_0041:  call       ""void System.Console.WriteLine(double)""
  IL_0046:  ldloc.s    V_4
  IL_0048:  ldc.i4.1
  IL_0049:  add
  IL_004a:  stloc.s    V_4
  IL_004c:  ldloc.s    V_4
  IL_004e:  ldloc.2
  IL_004f:  ble.s      IL_0038
  IL_0051:  ldloc.3
  IL_0052:  ldc.i4.1
  IL_0053:  add
  IL_0054:  stloc.3
  IL_0055:  ldloc.3
  IL_0056:  ldloc.1
  IL_0057:  ble.s      IL_002d
  IL_0059:  ret
}");
        }

        [WorkItem(544937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544937")]
        [Fact]
        public void TestForEachMultiDimensionalArrayBreakAndContinue()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        int[, ,] array = new[,,]
        {
            { {1, 2}, {3, 4} },
            { {5, 6}, {7, 8} },
        };

        Test(array);
    }

    static void Test(int[, ,] array)
    {
        foreach (int i in array)
        {
            if (i % 2 == 1) continue;
            Console.WriteLine(i);
        }

        foreach (int i in array)
        {
            if (i > 4) break;
            Console.WriteLine(i);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
2
4
6
8
1
2
3
4");

            compilation.VerifyIL("C.Test", @"
{
  // Code size      239 (0xef)
  .maxstack  4
  .locals init (int[,,] V_0,
      int V_1,
      int V_2,
      int V_3,
      int V_4,
      int V_5,
      int V_6,
      int V_7, //i
      int V_8) //i
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0009:  stloc.1
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0011:  stloc.2
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.2
  IL_0014:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0019:  stloc.3
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0021:  stloc.s    V_4
  IL_0023:  br.s       IL_0073
  IL_0025:  ldloc.0
  IL_0026:  ldc.i4.1
  IL_0027:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_002c:  stloc.s    V_5
  IL_002e:  br.s       IL_0068
  IL_0030:  ldloc.0
  IL_0031:  ldc.i4.2
  IL_0032:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0037:  stloc.s    V_6
  IL_0039:  br.s       IL_005d
  IL_003b:  ldloc.0
  IL_003c:  ldloc.s    V_4
  IL_003e:  ldloc.s    V_5
  IL_0040:  ldloc.s    V_6
  IL_0042:  call       ""int[*,*,*].Get""
  IL_0047:  stloc.s    V_7
  IL_0049:  ldloc.s    V_7
  IL_004b:  ldc.i4.2
  IL_004c:  rem
  IL_004d:  ldc.i4.1
  IL_004e:  beq.s      IL_0057
  IL_0050:  ldloc.s    V_7
  IL_0052:  call       ""void System.Console.WriteLine(int)""
  IL_0057:  ldloc.s    V_6
  IL_0059:  ldc.i4.1
  IL_005a:  add
  IL_005b:  stloc.s    V_6
  IL_005d:  ldloc.s    V_6
  IL_005f:  ldloc.3
  IL_0060:  ble.s      IL_003b
  IL_0062:  ldloc.s    V_5
  IL_0064:  ldc.i4.1
  IL_0065:  add
  IL_0066:  stloc.s    V_5
  IL_0068:  ldloc.s    V_5
  IL_006a:  ldloc.2
  IL_006b:  ble.s      IL_0030
  IL_006d:  ldloc.s    V_4
  IL_006f:  ldc.i4.1
  IL_0070:  add
  IL_0071:  stloc.s    V_4
  IL_0073:  ldloc.s    V_4
  IL_0075:  ldloc.1
  IL_0076:  ble.s      IL_0025
  IL_0078:  ldarg.0
  IL_0079:  stloc.0
  IL_007a:  ldloc.0
  IL_007b:  ldc.i4.0
  IL_007c:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0081:  stloc.3
  IL_0082:  ldloc.0
  IL_0083:  ldc.i4.1
  IL_0084:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0089:  stloc.2
  IL_008a:  ldloc.0
  IL_008b:  ldc.i4.2
  IL_008c:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0091:  stloc.1
  IL_0092:  ldloc.0
  IL_0093:  ldc.i4.0
  IL_0094:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0099:  stloc.s    V_4
  IL_009b:  br.s       IL_00e9
  IL_009d:  ldloc.0
  IL_009e:  ldc.i4.1
  IL_009f:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_00a4:  stloc.s    V_5
  IL_00a6:  br.s       IL_00de
  IL_00a8:  ldloc.0
  IL_00a9:  ldc.i4.2
  IL_00aa:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_00af:  stloc.s    V_6
  IL_00b1:  br.s       IL_00d3
  IL_00b3:  ldloc.0
  IL_00b4:  ldloc.s    V_4
  IL_00b6:  ldloc.s    V_5
  IL_00b8:  ldloc.s    V_6
  IL_00ba:  call       ""int[*,*,*].Get""
  IL_00bf:  stloc.s    V_8
  IL_00c1:  ldloc.s    V_8
  IL_00c3:  ldc.i4.4
  IL_00c4:  bgt.s      IL_00ee
  IL_00c6:  ldloc.s    V_8
  IL_00c8:  call       ""void System.Console.WriteLine(int)""
  IL_00cd:  ldloc.s    V_6
  IL_00cf:  ldc.i4.1
  IL_00d0:  add
  IL_00d1:  stloc.s    V_6
  IL_00d3:  ldloc.s    V_6
  IL_00d5:  ldloc.1
  IL_00d6:  ble.s      IL_00b3
  IL_00d8:  ldloc.s    V_5
  IL_00da:  ldc.i4.1
  IL_00db:  add
  IL_00dc:  stloc.s    V_5
  IL_00de:  ldloc.s    V_5
  IL_00e0:  ldloc.2
  IL_00e1:  ble.s      IL_00a8
  IL_00e3:  ldloc.s    V_4
  IL_00e5:  ldc.i4.1
  IL_00e6:  add
  IL_00e7:  stloc.s    V_4
  IL_00e9:  ldloc.s    V_4
  IL_00eb:  ldloc.3
  IL_00ec:  ble.s      IL_009d
  IL_00ee:  ret
}");
        }

        [Fact]
        public void TestForEachString()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var c in ""hello"")
        {
            System.Console.WriteLine(c);
        }
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
h
e
l
l
o");

            // Lowered to a for-loop from 0 to length.
            // No disposal required.
            compilation.VerifyIL("C.Main", @"{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (string V_0,
  int V_1)
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  IL_0008:  br.s       IL_001a
  IL_000a:  ldloc.0
  IL_000b:  ldloc.1
  IL_000c:  callvirt   ""char string.this[int].get""
  IL_0011:  call       ""void System.Console.WriteLine(char)""
  IL_0016:  ldloc.1
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  stloc.1
  IL_001a:  ldloc.1
  IL_001b:  ldloc.0
  IL_001c:  callvirt   ""int string.Length.get""
  IL_0021:  blt.s      IL_000a
  IL_0023:  ret
}");
        }

        [Fact]
        public void TestForEachPattern()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    int x = 0;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            // Lowered to a while-loop over an enumerator.
            // Worst-case disposal code: 'as' and null check.
            compilation.VerifyIL("C.Main", @"{
  // Code size       52 (0x34)
  .maxstack  1
  .locals init (Enumerator V_0,
  System.IDisposable V_1)
  IL_0000:  newobj     ""Enumerable..ctor()""
  IL_0005:  call       ""Enumerator Enumerable.GetEnumerator()""
  IL_000a:  stloc.0
  .try
{
  IL_000b:  br.s       IL_0018
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""int Enumerator.Current.get""
  IL_0013:  call       ""void System.Console.WriteLine(int)""
  IL_0018:  ldloc.0
  IL_0019:  callvirt   ""bool Enumerator.MoveNext()""
  IL_001e:  brtrue.s   IL_000d
  IL_0020:  leave.s    IL_0033
}
  finally
{
  IL_0022:  ldloc.0
  IL_0023:  isinst     ""System.IDisposable""
  IL_0028:  stloc.1
  IL_0029:  ldloc.1
  IL_002a:  brfalse.s  IL_0032
  IL_002c:  ldloc.1
  IL_002d:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0032:  endfinally
}
  IL_0033:  ret
}");
        }

        [Fact]
        public void TestForEachInterface()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}

class Enumerable : System.Collections.IEnumerable
{
    // Explicit implementation won't match pattern.
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        System.Collections.Generic.List<int> list = new  System.Collections.Generic.List<int>();
        list.Add(3);
        list.Add(2);
        list.Add(1);
        return list.GetEnumerator(); 
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
3
2
1");

            // Same as TestForEachPattern, but calls interface methods
            compilation.VerifyIL("C.Main", @"{
  // Code size       52 (0x34)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0,
  System.IDisposable V_1)
  IL_0000:  newobj     ""Enumerable..ctor()""
  IL_0005:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_000a:  stloc.0
  .try
{
  IL_000b:  br.s       IL_0018
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_0013:  call       ""void System.Console.WriteLine(object)""
  IL_0018:  ldloc.0
  IL_0019:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_001e:  brtrue.s   IL_000d
  IL_0020:  leave.s    IL_0033
}
  finally
{
  IL_0022:  ldloc.0
  IL_0023:  isinst     ""System.IDisposable""
  IL_0028:  stloc.1
  IL_0029:  ldloc.1
  IL_002a:  brfalse.s  IL_0032
  IL_002c:  ldloc.1
  IL_002d:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0032:  endfinally
}
  IL_0033:  ret
}");
        }

        [Fact]
        public void TestForEachExplicitlyDisposableStruct()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

struct Enumerator : System.IDisposable
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    void System.IDisposable.Dispose() { }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            // Disposal does not require a null check.
            // Dispose called on boxed Enumerator.
            compilation.VerifyIL("C.Main", @"{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     ""Enumerable..ctor()""
  IL_0005:  call       ""Enumerator Enumerable.GetEnumerator()""
  IL_000a:  stloc.0
  .try
{
  IL_000b:  br.s       IL_0019
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""int Enumerator.Current.get""
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       ""bool Enumerator.MoveNext()""
  IL_0020:  brtrue.s   IL_000d
  IL_0022:  leave.s    IL_0032
}
  finally
{
  IL_0024:  ldloca.s   V_0
  IL_0026:  constrained. ""Enumerator""
  IL_002c:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0031:  endfinally
}
  IL_0032:  ret
}");
        }

        [Fact]
        public void TestForEachImplicitlyDisposableStruct()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

struct Enumerator : System.IDisposable
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    public void Dispose() { }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            // Disposal does not require a null check.
            // Dispose called directly on Enumerator.
            compilation.VerifyIL("C.Main", @"{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     ""Enumerable..ctor()""
  IL_0005:  call       ""Enumerator Enumerable.GetEnumerator()""
  IL_000a:  stloc.0
  .try
{
  IL_000b:  br.s       IL_0019
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""int Enumerator.Current.get""
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       ""bool Enumerator.MoveNext()""
  IL_0020:  brtrue.s   IL_000d
  IL_0022:  leave.s    IL_0032
}
  finally
{
  IL_0024:  ldloca.s   V_0
  IL_0026:  constrained. ""Enumerator""
  IL_002c:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0031:  endfinally
}
  IL_0032:  ret
}");
        }

        [Fact]
        public void TestForEachDisposeStruct()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}
class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}
struct Enumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    public void Dispose() { }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            compilation.VerifyIL("C.Main", @"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     ""Enumerable..ctor()""
  IL_0005:  call       ""Enumerator Enumerable.GetEnumerator()""
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0019
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""int Enumerator.Current.get""
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       ""bool Enumerator.MoveNext()""
  IL_0020:  brtrue.s   IL_000d
  IL_0022:  ret
}");
        }

        [Fact]
        public void TestForEachDisposableConvertibleStruct()
        {
            var csharp = @"
class C
{
    void Test()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}";

            // NOTE: can't convert to interface in source
            var il = @"
.class public sequential ansi sealed beforefieldinit Enumerator
       extends [mscorlib]System.ValueType
{
  .field private int32 x
  .method public hidebysig specialname instance int32 
          get_Current() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 Enumerator::x
    IL_0006:  ret
  } // end of method Enumerator::get_Current

  .method public hidebysig instance bool 
          MoveNext() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  3
    .locals init ([0] int32 CS$0$0000)
    IL_0000:  ldarg.0
    IL_0001:  dup
    IL_0002:  ldfld      int32 Enumerator::x
    IL_0007:  ldc.i4.1
    IL_0008:  add
    IL_0009:  dup
    IL_000a:  stloc.0
    IL_000b:  stfld      int32 Enumerator::x
    IL_0010:  ldloc.0
    IL_0011:  ldc.i4.4
    IL_0012:  clt
    IL_0014:  ret
  } // end of method Enumerator::MoveNext

  .property instance int32 Current()
  {
    .get instance int32 Enumerator::get_Current()
  } // end of property Enumerator::Current

  .method public hidebysig specialname static 
          class [mscorlib]System.IDisposable 
          op_Implicit(valuetype Enumerator e) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldnull
    IL_0001:  ret
  } // end of method Enumerator::op_Implicit
} // end of class Enumerator";

            var compilation = CreateCompilationWithCustomILSource(csharp, il);

            // We specifically ignore user-defined conversions to interfaces, even from metadata.
            CompileAndVerify(compilation).VerifyIL("C.Test", @"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     ""Enumerable..ctor()""
  IL_0005:  call       ""Enumerator Enumerable.GetEnumerator()""
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0019
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""int Enumerator.Current.get""
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       ""bool Enumerator.MoveNext()""
  IL_0020:  brtrue.s   IL_000d
  IL_0022:  ret
}");
        }

        [Fact]
        public void TestForEachNonDisposableStruct()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

struct Enumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            // Disposal not required - no try-finally.
            compilation.VerifyIL("C.Main", @"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     ""Enumerable..ctor()""
  IL_0005:  call       ""Enumerator Enumerable.GetEnumerator()""
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0019
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""int Enumerator.Current.get""
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       ""bool Enumerator.MoveNext()""
  IL_0020:  brtrue.s   IL_000d
  IL_0022:  ret
}");
        }

        [WorkItem(540943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540943")]
        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestForEachExplicitlyGetEnumeratorStruct()
        {
            var source = @"
using System.Collections;
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}
struct Enumerable : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() { return new int[]{1,2,3}.GetEnumerator(); }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            compilation.VerifyIL("C.Main", @"{
  // Code size       65 (0x41)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0,
  Enumerable V_1,
  System.IDisposable V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""Enumerable""
  IL_0008:  ldloc.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  constrained. ""Enumerable""
  IL_0012:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_0017:  stloc.0
  .try
{
  IL_0018:  br.s       IL_0025
  IL_001a:  ldloc.0
  IL_001b:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_0020:  call       ""void System.Console.WriteLine(object)""
  IL_0025:  ldloc.0
  IL_0026:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_002b:  brtrue.s   IL_001a
  IL_002d:  leave.s    IL_0040
}
  finally
{
  IL_002f:  ldloc.0
  IL_0030:  isinst     ""System.IDisposable""
  IL_0035:  stloc.2
  IL_0036:  ldloc.2
  IL_0037:  brfalse.s  IL_003f
  IL_0039:  ldloc.2
  IL_003a:  callvirt   ""void System.IDisposable.Dispose()""
  IL_003f:  endfinally
}
  IL_0040:  ret
}");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestForEachImplicitlyGetEnumeratorStruct()
        {
            var source = @"
using System.Collections;
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}
struct Enumerable : IEnumerable
{
    public IEnumerator GetEnumerator() { return new int[]{1,2,3}.GetEnumerator(); }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            compilation.VerifyIL("C.Main", @"{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0,
  Enumerable V_1,
  System.IDisposable V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""Enumerable""
  IL_0008:  ldloc.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       ""System.Collections.IEnumerator Enumerable.GetEnumerator()""
  IL_0011:  stloc.0
  .try
{
  IL_0012:  br.s       IL_001f
  IL_0014:  ldloc.0
  IL_0015:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_001a:  call       ""void System.Console.WriteLine(object)""
  IL_001f:  ldloc.0
  IL_0020:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_0025:  brtrue.s   IL_0014
  IL_0027:  leave.s    IL_003a
}
  finally
{
  IL_0029:  ldloc.0
  IL_002a:  isinst     ""System.IDisposable""
  IL_002f:  stloc.2
  IL_0030:  ldloc.2
  IL_0031:  brfalse.s  IL_0039
  IL_0033:  ldloc.2
  IL_0034:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0039:  endfinally
}
  IL_003a:  ret
}");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestForEachGetEnumeratorStruct()
        {
            var source = @"
using System.Collections;
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}
struct Enumerable 
{
    public IEnumerator GetEnumerator() { return new int[]{1,2,3}.GetEnumerator(); }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            compilation.VerifyIL("C.Main", @"{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0,
  Enumerable V_1,
  System.IDisposable V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""Enumerable""
  IL_0008:  ldloc.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       ""System.Collections.IEnumerator Enumerable.GetEnumerator()""
  IL_0011:  stloc.0
  .try
{
  IL_0012:  br.s       IL_001f
  IL_0014:  ldloc.0
  IL_0015:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_001a:  call       ""void System.Console.WriteLine(object)""
  IL_001f:  ldloc.0
  IL_0020:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_0025:  brtrue.s   IL_0014
  IL_0027:  leave.s    IL_003a
}
  finally
{
  IL_0029:  ldloc.0
  IL_002a:  isinst     ""System.IDisposable""
  IL_002f:  stloc.2
  IL_0030:  ldloc.2
  IL_0031:  brfalse.s  IL_0039
  IL_0033:  ldloc.2
  IL_0034:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0039:  endfinally
}
  IL_003a:  ret
}");
        }

        [WorkItem(540943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540943")]
        [Fact]
        public void TestForEachExplicitlyGetEnumeratorGenericStruct()
        {
            var source = @"
using System.Collections.Generic;
using System.Collections;
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}
struct Enumerable : IEnumerable<int>
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator() { var temp = new List<int>();
        temp.Add(1);
        temp.Add(2);
        temp.Add(3);
        return temp.GetEnumerator(); }
    IEnumerator IEnumerable.GetEnumerator() { throw null; }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            compilation.VerifyIL("C.Main", @"{
  // Code size       58 (0x3a)
  .maxstack  1
  .locals init (System.Collections.Generic.IEnumerator<int> V_0,
  Enumerable V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""Enumerable""
  IL_0008:  ldloc.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  constrained. ""Enumerable""
  IL_0012:  callvirt   ""System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()""
  IL_0017:  stloc.0
  .try
{
  IL_0018:  br.s       IL_0025
  IL_001a:  ldloc.0
  IL_001b:  callvirt   ""int System.Collections.Generic.IEnumerator<int>.Current.get""
  IL_0020:  call       ""void System.Console.WriteLine(int)""
  IL_0025:  ldloc.0
  IL_0026:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_002b:  brtrue.s   IL_001a
  IL_002d:  leave.s    IL_0039
}
  finally
{
  IL_002f:  ldloc.0
  IL_0030:  brfalse.s  IL_0038
  IL_0032:  ldloc.0
  IL_0033:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0038:  endfinally
}
  IL_0039:  ret
}");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestForEachImplicitlyGetEnumeratorGenericStruct()
        {
            var source = @"
using System.Collections;
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}
struct Enumerable : IEnumerable
{
    public IEnumerator GetEnumerator() { return new int[]{1,2,3}.GetEnumerator(); }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            compilation.VerifyIL("C.Main", @"{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0,
  Enumerable V_1,
  System.IDisposable V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""Enumerable""
  IL_0008:  ldloc.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       ""System.Collections.IEnumerator Enumerable.GetEnumerator()""
  IL_0011:  stloc.0
  .try
{
  IL_0012:  br.s       IL_001f
  IL_0014:  ldloc.0
  IL_0015:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_001a:  call       ""void System.Console.WriteLine(object)""
  IL_001f:  ldloc.0
  IL_0020:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_0025:  brtrue.s   IL_0014
  IL_0027:  leave.s    IL_003a
}
  finally
{
  IL_0029:  ldloc.0
  IL_002a:  isinst     ""System.IDisposable""
  IL_002f:  stloc.2
  IL_0030:  ldloc.2
  IL_0031:  brfalse.s  IL_0039
  IL_0033:  ldloc.2
  IL_0034:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0039:  endfinally
}
  IL_003a:  ret
}");
        }

        [Fact]
        public void TestForEachDisposableSealed()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

sealed class Enumerator : System.IDisposable
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    void System.IDisposable.Dispose() { }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            // Null check followed by upcast (same as if unsealed).
            compilation.VerifyIL("C.Main", @"{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     ""Enumerable..ctor()""
  IL_0005:  call       ""Enumerator Enumerable.GetEnumerator()""
  IL_000a:  stloc.0
  .try
{
  IL_000b:  br.s       IL_0018
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""int Enumerator.Current.get""
  IL_0013:  call       ""void System.Console.WriteLine(int)""
  IL_0018:  ldloc.0
  IL_0019:  callvirt   ""bool Enumerator.MoveNext()""
  IL_001e:  brtrue.s   IL_000d
  IL_0020:  leave.s    IL_002c
}
  finally
{
  IL_0022:  ldloc.0
  IL_0023:  brfalse.s  IL_002b
  IL_0025:  ldloc.0
  IL_0026:  callvirt   ""void System.IDisposable.Dispose()""
  IL_002b:  endfinally
}
  IL_002c:  ret
}");
        }

        [Fact]
        public void TestForEachNonDisposableSealed()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

sealed class Enumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            // Disposal not required - no try-finally.
            compilation.VerifyIL("C.Main", @"{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     ""Enumerable..ctor()""
  IL_0005:  call       ""Enumerator Enumerable.GetEnumerator()""
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0018
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""int Enumerator.Current.get""
  IL_0013:  call       ""void System.Console.WriteLine(int)""
  IL_0018:  ldloc.0
  IL_0019:  callvirt   ""bool Enumerator.MoveNext()""
  IL_001e:  brtrue.s   IL_000d
  IL_0020:  ret
}");
        }

        [Fact]
        public void TestForEachNonDisposableAbstractClass()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable1())
        {
            System.Console.WriteLine(x);
        }

        foreach (var x in new Enumerable2())
        {
            System.Console.WriteLine(x);
        }
    }
}

class Enumerable1
{
    public AbstractEnumerator GetEnumerator() { return new DisposableEnumerator(); }
}

class Enumerable2
{
    public AbstractEnumerator GetEnumerator() { return new NonDisposableEnumerator(); }
}

abstract class AbstractEnumerator
{
    public abstract int Current { get; }
    public abstract bool MoveNext();
}

class DisposableEnumerator : AbstractEnumerator, System.IDisposable
{
    int x;
    public override int Current { get { return x; } }
    public override bool MoveNext() { return ++x < 4; }
    void System.IDisposable.Dispose() { System.Console.WriteLine(""Done with DisposableEnumerator""); }
}

class NonDisposableEnumerator : AbstractEnumerator
{
    int x;
    public override int Current { get { return x; } }
    public override bool MoveNext() { return --x > -4; }
}";
            // Both loops generate the same disposal code, but one calls dispose and
            // the other doesn't.
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3
Done with DisposableEnumerator
-1
-2
-3");
        }

        [Fact]
        public void TestForEachNested()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            foreach (var y in new Enumerable())
            {
                System.Console.WriteLine(""({0}, {1})"", x, y);
            }
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    int x = 0;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
(1, 1)
(1, 2)
(1, 3)
(2, 1)
(2, 2)
(2, 3)
(3, 1)
(3, 2)
(3, 3)");

            // try { loop { try { loop { } } finally } } finally
            compilation.VerifyIL("C.Main", @"
{
  // Code size      123 (0x7b)
  .maxstack  3
  .locals init (Enumerator V_0,
  int V_1, //x
  Enumerator V_2,
  int V_3, //y
  System.IDisposable V_4)
  IL_0000:  newobj     ""Enumerable..ctor()""
  IL_0005:  call       ""Enumerator Enumerable.GetEnumerator()""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  br.s       IL_005c
    IL_000d:  ldloc.0
    IL_000e:  callvirt   ""int Enumerator.Current.get""
    IL_0013:  stloc.1
    IL_0014:  newobj     ""Enumerable..ctor()""
    IL_0019:  call       ""Enumerator Enumerable.GetEnumerator()""
    IL_001e:  stloc.2
    .try
    {
      IL_001f:  br.s       IL_003e
      IL_0021:  ldloc.2
      IL_0022:  callvirt   ""int Enumerator.Current.get""
      IL_0027:  stloc.3
      IL_0028:  ldstr      ""({0}, {1})""
      IL_002d:  ldloc.1
      IL_002e:  box        ""int""
      IL_0033:  ldloc.3
      IL_0034:  box        ""int""
      IL_0039:  call       ""void System.Console.WriteLine(string, object, object)""
      IL_003e:  ldloc.2
      IL_003f:  callvirt   ""bool Enumerator.MoveNext()""
      IL_0044:  brtrue.s   IL_0021
      IL_0046:  leave.s    IL_005c
    }
    finally
    {
      IL_0048:  ldloc.2
      IL_0049:  isinst     ""System.IDisposable""
      IL_004e:  stloc.s    V_4
      IL_0050:  ldloc.s    V_4
      IL_0052:  brfalse.s  IL_005b
      IL_0054:  ldloc.s    V_4
      IL_0056:  callvirt   ""void System.IDisposable.Dispose()""
      IL_005b:  endfinally
    }
    IL_005c:  ldloc.0
    IL_005d:  callvirt   ""bool Enumerator.MoveNext()""
    IL_0062:  brtrue.s   IL_000d
    IL_0064:  leave.s    IL_007a
  }
  finally
  {
    IL_0066:  ldloc.0
    IL_0067:  isinst     ""System.IDisposable""
    IL_006c:  stloc.s    V_4
    IL_006e:  ldloc.s    V_4
    IL_0070:  brfalse.s  IL_0079
    IL_0072:  ldloc.s    V_4
    IL_0074:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0079:  endfinally
  }
  IL_007a:  ret
}");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestForEachCloseOverIterationVariable()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        List<Func<int>> thunks = new List<Func<int>>();
        foreach (int i in new int[] { 1, 2, 3 })
        {
            thunks.Add(() => i);
        }

        foreach (var thunk in thunks)
        {
            Console.WriteLine(thunk());
        }
    }
}";
            // NOTE: this is specifically not the dev10 behavior.  In dev10, the output is 3, 3, 3.
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");
        }

        [WorkItem(540952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540952")]
        [Fact]
        public void TestGetEnumeratorWithParams()
        {
            var source = @"
using System;
using System.Collections.Generic;
 
class Program
{
    static void Main()
    {
        foreach (var x in new B())
        {
            Console.WriteLine(x.ToLower());
        }
    }
}
 
class A
{
    public List<string>.Enumerator GetEnumerator()
    {
        var s = new List<string>();
        s.Add(""A""); 
        s.Add(""B""); 
        s.Add(""C""); 
        return s.GetEnumerator();
    }
}

class B : A
{
    public List<int>.Enumerator GetEnumerator(params int[] x)
    {
        return new List<int>.Enumerator();
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
a
b
c");
        }

        [WorkItem(540954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540954")]
        [Fact]
        public void TestMoveNextWithNonBoolDeclaredReturnType()
        {
            var source = @"
using System;
using System.Collections;

class Program
{
    static void Main()
    {
        Foo(x => { foreach (var y in x) { } });
    }

    static void Foo(Action<IEnumerable> a) { Console.WriteLine(1); }
    static void Foo(Action<A> a) { }}

class A
{
    public E<bool> GetEnumerator()
    {
        return new E<bool>();
    }
}

class E<T>
{
    public T MoveNext()
    {
        return default(T);
    }

    public int Current { get; set; }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "1");
        }

        [WorkItem(540958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540958")]
        [Fact]
        public void TestNonConstantNullInForeach()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        try
        {
            const string s = null;
            foreach (var y in s as string) { }
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(1);
        }
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "1");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestForEachStructEnumerable()
        {
            var source = @"
using System.Collections;
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        {
            System.Console.WriteLine(x);
        }
    }
}
struct Enumerable : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() { return new int[]{1,2,3}.GetEnumerator(); }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");

            // Confirm that GetEnumerator is a constrained call
            compilation.VerifyIL("C.Main", @"{
  // Code size       65 (0x41)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0,
  Enumerable V_1,
  System.IDisposable V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""Enumerable""
  IL_0008:  ldloc.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  constrained. ""Enumerable""
  IL_0012:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_0017:  stloc.0
  .try
{
  IL_0018:  br.s       IL_0025
  IL_001a:  ldloc.0
  IL_001b:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_0020:  call       ""void System.Console.WriteLine(object)""
  IL_0025:  ldloc.0
  IL_0026:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_002b:  brtrue.s   IL_001a
  IL_002d:  leave.s    IL_0040
}
  finally
{
  IL_002f:  ldloc.0
  IL_0030:  isinst     ""System.IDisposable""
  IL_0035:  stloc.2
  IL_0036:  ldloc.2
  IL_0037:  brfalse.s  IL_003f
  IL_0039:  ldloc.2
  IL_003a:  callvirt   ""void System.IDisposable.Dispose()""
  IL_003f:  endfinally
}
  IL_0040:  ret
}");
        }

        [Fact]
        public void TestForEachMutableStructEnumerablePattern()
        {
            var source = @"
class C
{
    static void Main()
    {
        Enumerable e = new Enumerable();
        System.Console.WriteLine(e.i);
        foreach (var x in e) { }
        System.Console.WriteLine(e.i);
    }
}

struct Enumerable
{
    public int i;
    public Enumerator GetEnumerator() { i++; return new Enumerator(); }
}

struct Enumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
0
1");

            // Confirm that GetEnumerator is called on the local, not on a copy
            compilation.VerifyIL("C.Main", @"
{
  // Code size       58 (0x3a)
  .maxstack  1
  .locals init (Enumerable V_0, //e
  Enumerator V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""Enumerable""
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""int Enumerable.i""
  IL_000e:  call       ""void System.Console.WriteLine(int)""
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       ""Enumerator Enumerable.GetEnumerator()""
  IL_001a:  stloc.1
  IL_001b:  br.s       IL_0025
  IL_001d:  ldloca.s   V_1
  IL_001f:  call       ""int Enumerator.Current.get""
  IL_0024:  pop
  IL_0025:  ldloca.s   V_1
  IL_0027:  call       ""bool Enumerator.MoveNext()""
  IL_002c:  brtrue.s   IL_001d
  IL_002e:  ldloc.0
  IL_002f:  ldfld      ""int Enumerable.i""
  IL_0034:  call       ""void System.Console.WriteLine(int)""
  IL_0039:  ret
}
");
        }

        [Fact]
        public void TestForEachMutableStructEnumerableInterface()
        {
            var source = @"
using System.Collections;

class C
{
    static void Main()
    {
        Enumerable e = new Enumerable();
        System.Console.WriteLine(e.i);
        foreach (var x in e) { }
        System.Console.WriteLine(e.i);
    }
}

struct Enumerable : IEnumerable
{
    public int i;
    IEnumerator IEnumerable.GetEnumerator() { i++; return new Enumerator(); }
}

struct Enumerator : IEnumerator
{
    int x;
    public object Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    public void Reset() { x = 0; }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"
0
1");

            // Confirm that GetEnumerator is called on the local, not on a copy
            compilation.VerifyIL("C.Main", @"
{
  // Code size       81 (0x51)
  .maxstack  1
  .locals init (Enumerable V_0, //e
  System.Collections.IEnumerator V_1,
  System.IDisposable V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""Enumerable""
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""int Enumerable.i""
  IL_000e:  call       ""void System.Console.WriteLine(int)""
  IL_0013:  ldloca.s   V_0
  IL_0015:  constrained. ""Enumerable""
  IL_001b:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_0020:  stloc.1
  .try
{
  IL_0021:  br.s       IL_002a
  IL_0023:  ldloc.1
  IL_0024:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_0029:  pop
  IL_002a:  ldloc.1
  IL_002b:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_0030:  brtrue.s   IL_0023
  IL_0032:  leave.s    IL_0045
}
  finally
{
  IL_0034:  ldloc.1
  IL_0035:  isinst     ""System.IDisposable""
  IL_003a:  stloc.2
  IL_003b:  ldloc.2
  IL_003c:  brfalse.s  IL_0044
  IL_003e:  ldloc.2
  IL_003f:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0044:  endfinally
}
  IL_0045:  ldloc.0
  IL_0046:  ldfld      ""int Enumerable.i""
  IL_004b:  call       ""void System.Console.WriteLine(int)""
  IL_0050:  ret
}
");
        }

        [Fact, WorkItem(2094, "https://github.com/dotnet/roslyn/issues/2111")]
        public void TestForEachValueTypeTypeParameterEnumeratorNoStruct()
        {
            var source = @"
using System.Collections.Generic;

class C<T> where T : IEnumerator<T>
{
    void M()
    {
        foreach (var c in this) { }
    }

    public T GetEnumerator()
    {
        return default(T);
    }
}
";
            CompileAndVerify(source).VerifyIL("C<T>.M", @"
{
  // Code size       63 (0x3f)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T C<T>.GetEnumerator()""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  br.s       IL_0017
    IL_0009:  ldloca.s   V_0
    IL_000b:  constrained. ""T""
    IL_0011:  callvirt   ""T System.Collections.Generic.IEnumerator<T>.Current.get""
    IL_0016:  pop
    IL_0017:  ldloca.s   V_0
    IL_0019:  constrained. ""T""
    IL_001f:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0024:  brtrue.s   IL_0009
    IL_0026:  leave.s    IL_003e
  }
  finally
  {
    IL_0028:  ldloc.0
    IL_0029:  box        ""T""
    IL_002e:  brfalse.s  IL_003d
    IL_0030:  ldloca.s   V_0
    IL_0032:  constrained. ""T""
    IL_0038:  callvirt   ""void System.IDisposable.Dispose()""
    IL_003d:  endfinally
  }
  IL_003e:  ret
}
");
        }

        [Fact, WorkItem(2094, "https://github.com/dotnet/roslyn/issues/2111")]
        public void TestForEachValueTypeTypeParameterEnumerator()
        {
            var source = @"
using System.Collections.Generic;

class C<T> where T : struct, IEnumerator<T>
{
    void M()
    {
        foreach (var c in this) { }
    }

    public T GetEnumerator()
    {
        return default(T);
    }
}
";
            // Note that there's no null check before the dispose call.
            // CONSIDER: Dev10 does have a null check, but it seems unnecessary.
            CompileAndVerify(source).VerifyIL("C<T>.M", @"
{
  // Code size       55 (0x37)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T C<T>.GetEnumerator()""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  br.s       IL_0017
    IL_0009:  ldloca.s   V_0
    IL_000b:  constrained. ""T""
    IL_0011:  callvirt   ""T System.Collections.Generic.IEnumerator<T>.Current.get""
    IL_0016:  pop
    IL_0017:  ldloca.s   V_0
    IL_0019:  constrained. ""T""
    IL_001f:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0024:  brtrue.s   IL_0009
    IL_0026:  leave.s    IL_0036
  }
  finally
  {
    IL_0028:  ldloca.s   V_0
    IL_002a:  constrained. ""T""
    IL_0030:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0035:  endfinally
  }
  IL_0036:  ret
}
");
        }

        /// <summary>
        /// Enumerable exposed with pattern, enumerator exposed
        /// through type parameter constrained to class with pattern,
        /// and implementing IDisposable. Dispose should be called
        /// without requiring "isinst IDisposable".
        /// </summary>
        [Fact]
        public void TestPatternEnumerableTypeParameterEnumeratorIDisposable()
        {
            var source =
@"class Enumerator : System.IDisposable
{
    public bool MoveNext() { return false; }
    public object Current { get { return null; } }
    void System.IDisposable.Dispose() { }
}
class Enumerable<T> where T : Enumerator
{
    public T GetEnumerator() { return null; }
}
class C
{
    static void M<T>(Enumerable<T> e) where T : Enumerator
    {
        foreach (var o in e) { }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.M<T>",
@"
{
  // Code size       57 (0x39)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""T Enumerable<T>.GetEnumerator()""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  br.s       IL_0015
  IL_0009:  ldloc.0
  IL_000a:  box        ""T""
  IL_000f:  callvirt   ""object Enumerator.Current.get""
  IL_0014:  pop
  IL_0015:  ldloc.0
  IL_0016:  box        ""T""
  IL_001b:  callvirt   ""bool Enumerator.MoveNext()""
  IL_0020:  brtrue.s   IL_0009
  IL_0022:  leave.s    IL_0038
}
  finally
{
  IL_0024:  ldloc.0
  IL_0025:  box        ""T""
  IL_002a:  brfalse.s  IL_0037
  IL_002c:  ldloc.0
  IL_002d:  box        ""T""
  IL_0032:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0037:  endfinally
}
  IL_0038:  ret
}");
        }
    }
}
