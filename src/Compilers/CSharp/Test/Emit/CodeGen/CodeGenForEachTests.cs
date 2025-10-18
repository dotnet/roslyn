// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
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
using System.Globalization;
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
            System.Console.WriteLine(x.ToString(CultureInfo.InvariantCulture));
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

            compilation.VerifyIL("C.Main", """
{
  // Code size      104 (0x68)
  .maxstack  3
  .locals init (double[,] V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4,
                double V_5) //x
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.4
  IL_0002:  newobj     "double[*,*]..ctor"
  IL_0007:  dup
  IL_0008:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=64 <PrivateImplementationDetails>.B600FC1A4E79D6311C0D8211E6ADB6C750C0EDBFD2A8B9DF903CBEAFEC712F98"
  IL_000d:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.0
  IL_0015:  callvirt   "int System.Array.GetUpperBound(int)"
  IL_001a:  stloc.1
  IL_001b:  ldloc.0
  IL_001c:  ldc.i4.1
  IL_001d:  callvirt   "int System.Array.GetUpperBound(int)"
  IL_0022:  stloc.2
  IL_0023:  ldloc.0
  IL_0024:  ldc.i4.0
  IL_0025:  callvirt   "int System.Array.GetLowerBound(int)"
  IL_002a:  stloc.3
  IL_002b:  br.s       IL_0063
  IL_002d:  ldloc.0
  IL_002e:  ldc.i4.1
  IL_002f:  callvirt   "int System.Array.GetLowerBound(int)"
  IL_0034:  stloc.s    V_4
  IL_0036:  br.s       IL_005a
  IL_0038:  ldloc.0
  IL_0039:  ldloc.3
  IL_003a:  ldloc.s    V_4
  IL_003c:  call       "double[*,*].Get"
  IL_0041:  stloc.s    V_5
  IL_0043:  ldloca.s   V_5
  IL_0045:  call       "System.Globalization.CultureInfo System.Globalization.CultureInfo.InvariantCulture.get"
  IL_004a:  call       "string double.ToString(System.IFormatProvider)"
  IL_004f:  call       "void System.Console.WriteLine(string)"
  IL_0054:  ldloc.s    V_4
  IL_0056:  ldc.i4.1
  IL_0057:  add
  IL_0058:  stloc.s    V_4
  IL_005a:  ldloc.s    V_4
  IL_005c:  ldloc.2
  IL_005d:  ble.s      IL_0038
  IL_005f:  ldloc.3
  IL_0060:  ldc.i4.1
  IL_0061:  add
  IL_0062:  stloc.3
  IL_0063:  ldloc.3
  IL_0064:  ldloc.1
  IL_0065:  ble.s      IL_002d
  IL_0067:  ret
}
""");
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

            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il, TargetFramework.Mscorlib40);

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

            compilation.VerifyIL("C.Main", @"
{
  // Code size       62 (0x3e)
  .maxstack  2
  .locals init (System.Collections.IEnumerator V_0,
                Enumerable V_1,
                System.IDisposable V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  dup
  IL_0003:  initobj    ""Enumerable""
  IL_0009:  constrained. ""Enumerable""
  IL_000f:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_0014:  stloc.0
  .try
  {
    IL_0015:  br.s       IL_0022
    IL_0017:  ldloc.0
    IL_0018:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_001d:  call       ""void System.Console.WriteLine(object)""
    IL_0022:  ldloc.0
    IL_0023:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0028:  brtrue.s   IL_0017
    IL_002a:  leave.s    IL_003d
  }
  finally
  {
    IL_002c:  ldloc.0
    IL_002d:  isinst     ""System.IDisposable""
    IL_0032:  stloc.2
    IL_0033:  ldloc.2
    IL_0034:  brfalse.s  IL_003c
    IL_0036:  ldloc.2
    IL_0037:  callvirt   ""void System.IDisposable.Dispose()""
    IL_003c:  endfinally
  }
  IL_003d:  ret
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

            compilation.VerifyIL("C.Main", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (System.Collections.IEnumerator V_0,
                Enumerable V_1,
                System.IDisposable V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  dup
  IL_0003:  initobj    ""Enumerable""
  IL_0009:  call       ""System.Collections.IEnumerator Enumerable.GetEnumerator()""
  IL_000e:  stloc.0
  .try
  {
    IL_000f:  br.s       IL_001c
    IL_0011:  ldloc.0
    IL_0012:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_0017:  call       ""void System.Console.WriteLine(object)""
    IL_001c:  ldloc.0
    IL_001d:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0022:  brtrue.s   IL_0011
    IL_0024:  leave.s    IL_0037
  }
  finally
  {
    IL_0026:  ldloc.0
    IL_0027:  isinst     ""System.IDisposable""
    IL_002c:  stloc.2
    IL_002d:  ldloc.2
    IL_002e:  brfalse.s  IL_0036
    IL_0030:  ldloc.2
    IL_0031:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0036:  endfinally
  }
  IL_0037:  ret
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

            compilation.VerifyIL("C.Main", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (System.Collections.IEnumerator V_0,
                Enumerable V_1,
                System.IDisposable V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  dup
  IL_0003:  initobj    ""Enumerable""
  IL_0009:  call       ""System.Collections.IEnumerator Enumerable.GetEnumerator()""
  IL_000e:  stloc.0
  .try
  {
    IL_000f:  br.s       IL_001c
    IL_0011:  ldloc.0
    IL_0012:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_0017:  call       ""void System.Console.WriteLine(object)""
    IL_001c:  ldloc.0
    IL_001d:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0022:  brtrue.s   IL_0011
    IL_0024:  leave.s    IL_0037
  }
  finally
  {
    IL_0026:  ldloc.0
    IL_0027:  isinst     ""System.IDisposable""
    IL_002c:  stloc.2
    IL_002d:  ldloc.2
    IL_002e:  brfalse.s  IL_0036
    IL_0030:  ldloc.2
    IL_0031:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0036:  endfinally
  }
  IL_0037:  ret
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

            compilation.VerifyIL("C.Main", @"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (System.Collections.Generic.IEnumerator<int> V_0,
                Enumerable V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  dup
  IL_0003:  initobj    ""Enumerable""
  IL_0009:  constrained. ""Enumerable""
  IL_000f:  callvirt   ""System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()""
  IL_0014:  stloc.0
  .try
  {
    IL_0015:  br.s       IL_0022
    IL_0017:  ldloc.0
    IL_0018:  callvirt   ""int System.Collections.Generic.IEnumerator<int>.Current.get""
    IL_001d:  call       ""void System.Console.WriteLine(int)""
    IL_0022:  ldloc.0
    IL_0023:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0028:  brtrue.s   IL_0017
    IL_002a:  leave.s    IL_0036
  }
  finally
  {
    IL_002c:  ldloc.0
    IL_002d:  brfalse.s  IL_0035
    IL_002f:  ldloc.0
    IL_0030:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0035:  endfinally
  }
  IL_0036:  ret
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

            compilation.VerifyIL("C.Main", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (System.Collections.IEnumerator V_0,
                Enumerable V_1,
                System.IDisposable V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  dup
  IL_0003:  initobj    ""Enumerable""
  IL_0009:  call       ""System.Collections.IEnumerator Enumerable.GetEnumerator()""
  IL_000e:  stloc.0
  .try
  {
    IL_000f:  br.s       IL_001c
    IL_0011:  ldloc.0
    IL_0012:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_0017:  call       ""void System.Console.WriteLine(object)""
    IL_001c:  ldloc.0
    IL_001d:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0022:  brtrue.s   IL_0011
    IL_0024:  leave.s    IL_0037
  }
  finally
  {
    IL_0026:  ldloc.0
    IL_0027:  isinst     ""System.IDisposable""
    IL_002c:  stloc.2
    IL_002d:  ldloc.2
    IL_002e:  brfalse.s  IL_0036
    IL_0030:  ldloc.2
    IL_0031:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0036:  endfinally
  }
  IL_0037:  ret
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
        public void TestForEachPatternDisposableRefStruct()
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
    }
}

class Enumerable1
{
    public DisposableEnumerator GetEnumerator() { return new DisposableEnumerator(); }
}

ref struct DisposableEnumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    public void Dispose() { System.Console.WriteLine(""Done with DisposableEnumerator""); }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.MakeTypeMissing(SpecialType.System_IDisposable);

            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            var verifier = CompileAndVerify(compilation, verify: Verification.FailsILVerify, expectedOutput: @"
1
2
3
Done with DisposableEnumerator");

            // IL Should not contain any Box/unbox instructions as we're a ref struct 
            verifier.VerifyIL("C.Main", @"
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (DisposableEnumerator V_0)
  IL_0000:  newobj     ""Enumerable1..ctor()""
  IL_0005:  call       ""DisposableEnumerator Enumerable1.GetEnumerator()""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  br.s       IL_0019
    IL_000d:  ldloca.s   V_0
    IL_000f:  call       ""int DisposableEnumerator.Current.get""
    IL_0014:  call       ""void System.Console.WriteLine(int)""
    IL_0019:  ldloca.s   V_0
    IL_001b:  call       ""bool DisposableEnumerator.MoveNext()""
    IL_0020:  brtrue.s   IL_000d
    IL_0022:  leave.s    IL_002c
  }
  finally
  {
    IL_0024:  ldloca.s   V_0
    IL_0026:  call       ""void DisposableEnumerator.Dispose()""
    IL_002b:  endfinally
  }
  IL_002c:  ret
}");
        }

        [Fact]
        public void TestForEachPatternDisposableRefStructWithParams()
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
    }
}

class Enumerable1
{
    public DisposableEnumerator GetEnumerator() { return new DisposableEnumerator(); }
}

ref struct DisposableEnumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    public void Dispose(params object[] args) { System.Console.WriteLine($""Done with DisposableEnumerator. args was {args}, length {args.Length}""); }
}";
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            var compilation = CompileAndVerify(source, verify: Verification.FailsILVerify, expectedOutput: @"
1
2
3
Done with DisposableEnumerator. args was System.Object[], length 0");
        }

        [Fact]
        public void TestForEachPatternDisposableRefStructWithDefaultArguments()
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
    }
}

class Enumerable1
{
    public DisposableEnumerator GetEnumerator() { return new DisposableEnumerator(); }
}

ref struct DisposableEnumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    public void Dispose(int arg = 1) { System.Console.WriteLine($""Done with DisposableEnumerator. arg was {arg}""); }
}";
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            var compilation = CompileAndVerify(source, verify: Verification.FailsILVerify, expectedOutput: @"
1
2
3
Done with DisposableEnumerator. arg was 1");
        }

        [Fact]
        public void TestForEachPatternDisposableRefStructWithExtensionMethod()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable1())
        {
            System.Console.Write(x);
        }
    }
}

class Enumerable1
{
    public DisposableEnumerator GetEnumerator() { return new DisposableEnumerator(); }
}

ref struct DisposableEnumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
}

static class DisposeExtension
{
    public static void Dispose(this DisposableEnumerator de) => throw null;
}
";
            // extension methods do not contribute to disposal
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            CompileAndVerify(source, verify: Verification.FailsILVerify, expectedOutput: @"123");
        }

        [Fact]
        public void TestForEachPatternDisposableRefStructWithTwoExtensionMethods()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable1())
        {
            System.Console.Write(x);
        }
    }
}

class Enumerable1
{
    public DisposableEnumerator GetEnumerator() { return new DisposableEnumerator(); }
}

ref struct DisposableEnumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
}

static class DisposeExtension1
{
    public static void Dispose(this DisposableEnumerator de) => throw null;
}
static class DisposeExtension2
{
    public static void Dispose(this DisposableEnumerator de) => throw null;
}
";
            // extension methods do not contribute to disposal
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            CompileAndVerify(source, verify: Verification.FailsILVerify, expectedOutput: @"123");
        }

        [Fact]
        public void TestForEachPatternDisposableRefStructWithExtensionMethodAndDefaultArguments()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable1())
        {
            System.Console.Write(x);
        }
    }
}

class Enumerable1
{
    public DisposableEnumerator GetEnumerator() { return new DisposableEnumerator(); }
}

ref struct DisposableEnumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
}

static class DisposeExtension
{
    public static void Dispose(this DisposableEnumerator de, int arg = 4) => throw null;
}
";
            // extension methods do not contribute to disposal
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            CompileAndVerify(source, verify: Verification.FailsILVerify, expectedOutput: @"123");
        }

        [Fact]
        public void TestForEachPatternDisposableRefStructWithExtensionMethodAndParams()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in new Enumerable1())
        {
            System.Console.Write(x);
        }
    }
}

class Enumerable1
{
    public DisposableEnumerator GetEnumerator() { return new DisposableEnumerator(); }
}

ref struct DisposableEnumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
}

static class DisposeExtension
{
    public static void Dispose(this DisposableEnumerator de, params object[] args) => throw null;
}
";
            // extension methods do not contribute to disposal
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            CompileAndVerify(source, verify: Verification.FailsILVerify, expectedOutput: @"123");
        }

        [Fact]
        public void TestForEachPatternDisposableIgnoredForNonRefStruct()
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
    }
}

class Enumerable1
{
    public DisposableEnumerator GetEnumerator() { return new DisposableEnumerator(); }
}

struct DisposableEnumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    public void Dispose() { System.Console.WriteLine(""Done with DisposableEnumerator""); }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");
        }

        [Fact]
        public void TestForEachPatternDisposableIgnoredForClass()
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
    }
}

class Enumerable1
{
    public DisposableEnumerator GetEnumerator() { return new DisposableEnumerator(); }
}

class DisposableEnumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    public void Dispose() { System.Console.WriteLine(""Done with DisposableEnumerator""); }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"
1
2
3");
        }

        [Fact]
        public void TestForEachPatternDisposableReportedForCSharp7_3()
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
    }
}

class Enumerable1
{
    public DisposableEnumerator GetEnumerator() { return new DisposableEnumerator(); }
}

ref struct DisposableEnumerator
{
    int x;
    public int Current { get { return x; } }
    public bool MoveNext() { return ++x < 4; }
    public void Dispose() { System.Console.WriteLine(""Done with DisposableEnumerator""); }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (6,27): error CS8370: Feature 'pattern-based disposal' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         foreach (var x in new Enumerable1())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "new Enumerable1()").WithArguments("pattern-based disposal", "8.0").WithLocation(6, 27)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var info = model.GetForEachStatementInfo(foreachSyntax);

            Assert.Equal("void DisposableEnumerator.Dispose()", info.DisposeMethod.ToTestDisplayString());
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
        Goo(x => { foreach (var y in x) { } });
    }

    static void Goo(Action<IEnumerable> a) { Console.WriteLine(1); }
    static void Goo(Action<A> a) { }}

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
            compilation.VerifyIL("C.Main", @"
{
  // Code size       62 (0x3e)
  .maxstack  2
  .locals init (System.Collections.IEnumerator V_0,
                Enumerable V_1,
                System.IDisposable V_2)
  IL_0000:  ldloca.s   V_1
  IL_0002:  dup
  IL_0003:  initobj    ""Enumerable""
  IL_0009:  constrained. ""Enumerable""
  IL_000f:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_0014:  stloc.0
  .try
  {
    IL_0015:  br.s       IL_0022
    IL_0017:  ldloc.0
    IL_0018:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_001d:  call       ""void System.Console.WriteLine(object)""
    IL_0022:  ldloc.0
    IL_0023:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0028:  brtrue.s   IL_0017
    IL_002a:  leave.s    IL_003d
  }
  finally
  {
    IL_002c:  ldloc.0
    IL_002d:  isinst     ""System.IDisposable""
    IL_0032:  stloc.2
    IL_0033:  ldloc.2
    IL_0034:  brfalse.s  IL_003c
    IL_0036:  ldloc.2
    IL_0037:  callvirt   ""void System.IDisposable.Dispose()""
    IL_003c:  endfinally
  }
  IL_003d:  ret
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

        [Fact]
        public void TestInvalidForeachOnConstantNullObject()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in (object)null)
        {
            Console.Write(i);
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'object' because 'object' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in (object)null)
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "(object)null").WithArguments("object", "GetEnumerator").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestConstantNullObjectImplementingIEnumerable()
        {
            var source = @"
using System;
using System.Collections.Generic;
public class C
{
    public static void Main()
    {
        foreach (var i in (IEnumerable<int>)null)
        {
            Console.Write(i);
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void TestConstantNullObjectWithGetEnumeratorPattern()
        {
            var source = @"
using System;
using System.Collections.Generic;
public class C
{
    public static void Main()
    {
        foreach (var i in (C)null)
        {
            Console.Write(i);
        }
    }

    public IEnumerator<int> GetEnumerator() => throw null;
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void TestConstantNullArray()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in (int[])null)
        {
            Console.Write(i);
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void TestConstantNullableImplementingIEnumerable()
        {
            var source = @"
using System;
using System.Collections;
public struct C : IEnumerable
{
    public static void Main()
    {
        foreach (var i in (C?)null)
        {
            Console.Write(i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => throw null;
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9)
                .VerifyIL("C.Main", @"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (System.Collections.IEnumerator V_0,
                C? V_1,
                C V_2,
                System.IDisposable V_3)
  IL_0000:  ldloca.s   V_1
  IL_0002:  dup
  IL_0003:  initobj    ""C?""
  IL_0009:  call       ""C C?.Value.get""
  IL_000e:  stloc.2
  IL_000f:  ldloca.s   V_2
  IL_0011:  constrained. ""C""
  IL_0017:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_001c:  stloc.0
  .try
  {
    IL_001d:  br.s       IL_002a
    IL_001f:  ldloc.0
    IL_0020:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_0025:  call       ""void System.Console.Write(object)""
    IL_002a:  ldloc.0
    IL_002b:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0030:  brtrue.s   IL_001f
    IL_0032:  leave.s    IL_0045
  }
  finally
  {
    IL_0034:  ldloc.0
    IL_0035:  isinst     ""System.IDisposable""
    IL_003a:  stloc.3
    IL_003b:  ldloc.3
    IL_003c:  brfalse.s  IL_0044
    IL_003e:  ldloc.3
    IL_003f:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0044:  endfinally
  }
  IL_0045:  ret
}");
        }

        [Fact]
        public void TestConstantNullableWithGetEnumeratorPattern()
        {
            var source = @"
using System;
using System.Collections;
public struct C
{
    public static void Main()
    {
        foreach (var i in (C?)null)
        {
            Console.Write(i);
        }
    }

    public IEnumerator GetEnumerator() => throw null;
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9)
                .VerifyIL("C.Main", @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (System.Collections.IEnumerator V_0,
                C? V_1,
                C V_2,
                System.IDisposable V_3)
  IL_0000:  ldloca.s   V_1
  IL_0002:  dup
  IL_0003:  initobj    ""C?""
  IL_0009:  call       ""C C?.Value.get""
  IL_000e:  stloc.2
  IL_000f:  ldloca.s   V_2
  IL_0011:  call       ""System.Collections.IEnumerator C.GetEnumerator()""
  IL_0016:  stloc.0
  .try
  {
    IL_0017:  br.s       IL_0024
    IL_0019:  ldloc.0
    IL_001a:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_001f:  call       ""void System.Console.Write(object)""
    IL_0024:  ldloc.0
    IL_0025:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_002a:  brtrue.s   IL_0019
    IL_002c:  leave.s    IL_003f
  }
  finally
  {
    IL_002e:  ldloc.0
    IL_002f:  isinst     ""System.IDisposable""
    IL_0034:  stloc.3
    IL_0035:  ldloc.3
    IL_0036:  brfalse.s  IL_003e
    IL_0038:  ldloc.3
    IL_0039:  callvirt   ""void System.IDisposable.Dispose()""
    IL_003e:  endfinally
  }
  IL_003f:  ret
}");
        }

        [Fact]
        public void TestForeachNullLiteral()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in null)
        {
            Console.Write(i);
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS0186: Use of null is not valid in this context
                    //         foreach (var i in null)
                    Diagnostic(ErrorCode.ERR_NullNotValid, "null").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestForeachDefaultLiteral()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in default)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this object self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS8716: There is no target type for the default literal.
                    //         foreach (var i in default)
                    Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensions()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithUpcast()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this object self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsOnDefaultObject()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in default(object))
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this object self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithStructEnumerator()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithUserDefinedImplicitConversion()
        {
            var source = @"
using System;
public class C
{
    public static implicit operator int(C c) => 0;

    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this int self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (9,27): error CS1929: 'C' does not contain a definition for 'GetEnumerator' and the best extension method overload 'Extensions.GetEnumerator(int)' requires a receiver of type 'int'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new C()").WithArguments("C", "GetEnumerator", "Extensions.GetEnumerator(int)", "int").WithLocation(9, 27),
                    // (9,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(9, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithNullableValueTypeConversion()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in 1)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this int? self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS1929: 'int' does not contain a definition for 'GetEnumerator' and the best extension method overload 'Extensions.GetEnumerator(int?)' requires a receiver of type 'int?'
                    //         foreach (var i in 1)
                    Diagnostic(ErrorCode.ERR_BadInstanceArgType, "1").WithArguments("int", "GetEnumerator", "Extensions.GetEnumerator(int?)", "int?").WithLocation(7, 27),
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'int' because 'int' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in 1)
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "1").WithArguments("int", "GetEnumerator").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithUnboxingConversion()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new object())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this int self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS1929: 'object' does not contain a definition for 'GetEnumerator' and the best extension method overload 'Extensions.GetEnumerator(int)' requires a receiver of type 'int'
                    //         foreach (var i in new object())
                    Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new object()").WithArguments("object", "GetEnumerator", "Extensions.GetEnumerator(int)", "int").WithLocation(7, 27),
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'object' because 'object' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new object())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new object()").WithArguments("object", "GetEnumerator").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithNullableUnwrapping()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in (int?)1)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this int self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS1929: 'int?' does not contain a definition for 'GetEnumerator' and the best extension method overload 'Extensions.GetEnumerator(int)' requires a receiver of type 'int'
                    //         foreach (var i in (int?)1)
                    Diagnostic(ErrorCode.ERR_BadInstanceArgType, "(int?)1").WithArguments("int?", "GetEnumerator", "Extensions.GetEnumerator(int)", "int").WithLocation(7, 27),
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'int?' because 'int?' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in (int?)1)
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "(int?)1").WithArguments("int?", "GetEnumerator").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithZeroToEnumConversion()
        {
            var source = @"
using System;

public enum E { Default = 0 }
public class C
{
    public static void Main()
    {
        foreach (var i in 0)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this E self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (9,27): error CS1929: 'int' does not contain a definition for 'GetEnumerator' and the best extension method overload 'Extensions.GetEnumerator(E)' requires a receiver of type 'E'
                    //         foreach (var i in 0)
                    Diagnostic(ErrorCode.ERR_BadInstanceArgType, "0").WithArguments("int", "GetEnumerator", "Extensions.GetEnumerator(E)", "E").WithLocation(9, 27),
                    // (9,27): error CS1579: foreach statement cannot operate on variables of type 'int' because 'int' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in 0)
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "0").WithArguments("int", "GetEnumerator").WithLocation(9, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithUnconstrainedGenericConversion()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        Inner(1);

        void Inner<T>(T t)
        {
            foreach (var i in t)
            {
                Console.Write(i);
            }
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this object self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithConstrainedGenericConversion()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        Inner(1);

        void Inner<T>(T t) where T : IConvertible
        {
            foreach (var i in t)
            {
                Console.Write(i);
            }
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this IConvertible self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithFormattableStringConversion()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in $"" "")
        {
            Console.Write(i.GetType());
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this FormattableString self) => throw null;
    public static C.Enumerator GetEnumerator(this object self) => throw null;
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "System.Char");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithDelegateConversion()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in () => 42)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this Func<int> self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS0446: Foreach cannot operate on a 'lambda expression'. Did you intend to invoke the 'lambda expression'?
                    //         foreach (var i in () => 42)
                    Diagnostic(ErrorCode.ERR_AnonMethGrpInForEach, "() => 42").WithArguments("lambda expression").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsWithBoxing()
        {
            var source = @"
using System;
public struct C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this object self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsOnInterface()
        {
            var source = @"
using System;
public interface I {}
public class C : I
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this I self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsOnDelegate()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in (Func<int>)(() => 42))
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this Func<int> self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsOnEnum()
        {
            var source = @"
using System;
public enum E { Default }
public class C
{
    public static void Main()
    {
        foreach (var i in E.Default)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this E self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsOnNullable()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in (int?)null)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this int? self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsOnConstantNullObject()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in (object)null)
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this object self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsOnTypeParameter()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new object())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator<T>(this T self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsOnRefStruct()
        {
            var source = @"
using System;
public class Program
{
    public static void Main()
    {
        foreach (var i in new C{span = stackalloc int[] {1,2,3} })
        {
            Console.Write(i);
        }
    }
}

public ref struct C
{
    public Span<int> span;
}

public static class Extensions
{
    public static Span<int>.Enumerator GetEnumerator(this C self) => self.span.GetEnumerator();
}";
            var comp = CreateCompilationWithSpan(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123", verify: Verification.Skipped);
        }

        [Fact]
        public void TestGetEnumeratorPatternOnRange()
        {
            var source = @"
using System;
using System.Collections.Generic;
public class C
{
    public static void Main()
    {
        foreach (var i in 1..4)
        {
            Console.Write(i);
        }
    }
}
public static class Extensions
{
    public static IEnumerator<int> GetEnumerator(this Range range)
    {
        for(var i = range.Start.Value; i < range.End.Value; i++)
        {
            yield return i;
        }
    }
}";
            var comp = CreateCompilationWithIndexAndRange(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsOnTuple()
        {
            var source = @"
using System;
using System.Collections.Generic;
public struct C
{
    public static void Main()
    {
        foreach (var i in (1, 2, 3))
        {
            Console.Write(i);
        }
    }
}
public static class Extensions
{
    public static IEnumerator<T> GetEnumerator<T>(this (T first, T second, T third) self)
    {
        yield return self.first;
        yield return self.second;
        yield return self.third;
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsOnTupleWithNestedConversions()
        {
            var source = @"
using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
public struct C
{
    public static void Main()
    {
        foreach (var (a, b) in (new[] { 1, 2, 3 }, new List<decimal>{ 0.1m, 0.2m, 0.3m }))
        {
            Console.WriteLine((a + b).ToString(CultureInfo.InvariantCulture));
        }
    }
}
public static class Extensions
{
    public static IEnumerator<(T1, T2)> GetEnumerator<T1, T2>(this (IEnumerable<T1> first, IEnumerable<T2> second) self)
    {
        return self.first.Zip(self.second, (a,b) => (a,b)).GetEnumerator();
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: @"1.1
2.2
3.3");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtension_WhereTypeParameternotInferred1()
        {
            var source = @"
using System;
using System.Collections.Generic;
public class C
{
    public static void Main()
    {
        foreach (var i in new object())
        {
            Console.Write(i);
        }
    }
}
public static class Extensions
{
    public static IEnumerator<T> GetEnumerator<T>(this object o) => throw null;
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,27): error CS0411: The type arguments for method 'Extensions.GetEnumerator<T>(object)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                    //         foreach (var i in new object())
                    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "new object()").WithArguments("Extensions.GetEnumerator<T>(object)").WithLocation(8, 27),
                    // (8,27): error CS1579: foreach statement cannot operate on variables of type 'object' because 'object' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new object())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new object()").WithArguments("object", "GetEnumerator").WithLocation(8, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtension_WhereTypeParameternotInferred2()
        {
            var source = @"
using System;
using System.Collections.Generic;
public class C
{
    public static void Main()
    {
        foreach (var i in new object())
        {
            Console.Write(i);
        }
    }
}
public static class Extensions
{
    public static IEnumerator<T> GetEnumerator<T>(this object o, params T[] arr) => throw null;
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,27): error CS0411: The type arguments for method 'Extensions.GetEnumerator<T>(object, params T[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                    //         foreach (var i in new object())
                    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "new object()").WithArguments("Extensions.GetEnumerator<T>(object, params T[])").WithLocation(8, 27),
                    // (8,27): error CS1579: foreach statement cannot operate on variables of type 'object' because 'object' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new object())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new object()").WithArguments("object", "GetEnumerator").WithLocation(8, 27)
                    );
        }

        [Fact]
        public void TestMoveNextPatternViaExtensions_OnExtensionGetEnumerator()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
    public static bool MoveNext(this C.Enumerator e) => false;
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS0117: 'C.Enumerator' does not contain a definition for 'MoveNext'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator", "MoveNext").WithLocation(7, 27),
                    // (7,27): error CS0202: foreach requires that the return type 'C.Enumerator' of 'Extensions.GetEnumerator(C)' must have a suitable public 'MoveNext' method and public 'Current' property
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new C()").WithArguments("C.Enumerator", "Extensions.GetEnumerator(C)").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestMoveNextPatternViaExtensions_OnInstanceGetEnumerator()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
    }

    public C.Enumerator GetEnumerator() => new C.Enumerator();
}
public static class Extensions
{
    public static bool MoveNext(this C.Enumerator e) => false;
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS0117: 'C.Enumerator' does not contain a definition for 'MoveNext'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator", "MoveNext").WithLocation(7, 27),
                    // (7,27): error CS0202: foreach requires that the return type 'C.Enumerator' of 'C.GetEnumerator()' must have a suitable public 'MoveNext' method and public 'Current' property
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new C()").WithArguments("C.Enumerator", "C.GetEnumerator()").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestPreferEnumeratorPatternFromInstanceThanViaExtension()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator1
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
    public sealed class Enumerator2
    {
        public int Current { get; private set; }
        public bool MoveNext() => throw null;
    }

    public C.Enumerator1 GetEnumerator() => new C.Enumerator1();
}

public static class Extensions
{
    public static C.Enumerator2 GetEnumerator(this C self) => throw null;
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9,
                     expectedOutput: "123");
        }

        [Fact]
        public void TestPreferEnumeratorPatternFromInstanceThanViaExtensionEvenWhenInvalid()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator1
    {
    }
    public sealed class Enumerator2
    {
        public int Current { get; private set; }
        public bool MoveNext() => throw null;
    }

    public C.Enumerator1 GetEnumerator() => throw null;
}

public static class Extensions
{
    public static C.Enumerator2 GetEnumerator(this C self) => throw null;
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS0117: 'C.Enumerator1' does not contain a definition for 'Current'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator1", "Current").WithLocation(7, 27),
                    // (7,27): error CS0202: foreach requires that the return type 'C.Enumerator1' of 'C.GetEnumerator()' must have a suitable public 'MoveNext' method and public 'Current' property
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new C()").WithArguments("C.Enumerator1", "C.GetEnumerator()").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestPreferEnumeratorPatternFromIEnumerableInterfaceThanViaExtension()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
public class C : IEnumerable<int>
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    
    public sealed class Enumerator1 : IEnumerator<int>
    {
        object IEnumerator.Current => Current;
        public int Current { get; private set; }

        public bool MoveNext() => Current++ != 3;

        public void Dispose() {}

        public void Reset() { }
    }

    public sealed class Enumerator2
    {
        public int Current { get; private set; }
        public bool MoveNext() => throw null;
    }

    IEnumerator<int> IEnumerable<int>.GetEnumerator() => new C.Enumerator1();
    IEnumerator IEnumerable.GetEnumerator() => new C.Enumerator1();
}

public static class Extensions
{
    public static C.Enumerator2 GetEnumerator(this C self) => throw null;
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9,
                     expectedOutput: "123");
        }

        [Fact]
        public void TestPreferIEnumeratorInterfaceOnDynamicThanViaExtension()
        {
            var source = @"
using System;
using System.Collections;

public class C : IEnumerable
{
    public static void Main()
    {
        foreach (var i in (dynamic)new C())
        {
            Console.Write(i);
        }
    }

    public sealed class Enumerator2
    {
        public int Current { get; private set; }
        public bool MoveNext() => throw null;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }
}

public static class Extensions
{
    public static C.Enumerator2 GetEnumerator(this C self) => throw null;
}";
            var comp = CreateCompilationWithCSharp(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAmbiguousExtensions()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions1
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}
public static class Extensions2
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): warning CS0278: 'C' does not implement the 'collection' pattern. 'Extensions1.GetEnumerator(C)' is ambiguous with 'Extensions2.GetEnumerator(C)'.
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "new C()").WithArguments("C", "collection", "Extensions1.GetEnumerator(C)", "Extensions2.GetEnumerator(C)").WithLocation(7, 27),
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAmbiguousExtensionsWhenOneHasCorrectPattern()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions1
{
    public static int GetEnumerator(this C self) => 42;
}
public static class Extensions2
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): warning CS0278: 'C' does not implement the 'collection' pattern. 'Extensions1.GetEnumerator(C)' is ambiguous with 'Extensions2.GetEnumerator(C)'.
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "new C()").WithArguments("C", "collection", "Extensions1.GetEnumerator(C)", "Extensions2.GetEnumerator(C)").WithLocation(7, 27),
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAmbiguousExtensionsWhenNeitherHasCorrectPattern()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions1
{
    public static int GetEnumerator(this C self) => 42;
}
public static class Extensions2
{
    public static bool GetEnumerator(this C self) => true;
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): warning CS0278: 'C' does not implement the 'collection' pattern. 'Extensions1.GetEnumerator(C)' is ambiguous with 'Extensions2.GetEnumerator(C)'.
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "new C()").WithArguments("C", "collection", "Extensions1.GetEnumerator(C)", "Extensions2.GetEnumerator(C)").WithLocation(7, 27),
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAmbiguousExtensionsWhenOneHasCorrectNumberOfParameters()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions1
{
    public static C.Enumerator GetEnumerator(this C self, int _) => throw null;
}
public static class Extensions2
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAmbiguousExtensionsWhenNeitherHasCorrectNumberOfParameters()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions1
{
    public static C.Enumerator GetEnumerator(this C self, int _) => new C.Enumerator();
}
public static class Extensions2
{
    public static C.Enumerator GetEnumerator(this C self, bool _) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS1501: No overload for method 'GetEnumerator' takes 0 arguments
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadArgCount, "new C()").WithArguments("GetEnumerator", "0").WithLocation(7, 27),
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAmbiguousExtensionsOnDifferentInterfaces()
        {
            var source = @"
using System;

public interface I1 {}
public interface I2 {}

public class C : I1, I2
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions1
{
    public static C.Enumerator GetEnumerator(this I1 self) => new C.Enumerator();
}
public static class Extensions2
{
    public static C.Enumerator GetEnumerator(this I2 self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (11,27): warning CS0278: 'C' does not implement the 'collection' pattern. 'Extensions1.GetEnumerator(I1)' is ambiguous with 'Extensions2.GetEnumerator(I2)'.
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "new C()").WithArguments("C", "collection", "Extensions1.GetEnumerator(I1)", "Extensions2.GetEnumerator(I2)").WithLocation(11, 27),
                    // (11,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(11, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAmbiguousExtensionsWithMostSpecificReceiver()
        {
            var source = @"
using System;

public interface I {}
public class C : I
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions1
{
    public static C.Enumerator GetEnumerator(this I self) => throw null;
}
public static class Extensions2
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAmbiguousExtensionsWithMostSpecificReceiverWhenMostSpecificReceiverDoesntImplementPattern()
        {
            var source = @"
using System;

public interface I {}
public class C : I
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions1
{
    public static C.Enumerator GetEnumerator(this I self) => throw null;
}
public static class Extensions2
{
    public static int GetEnumerator(this C self) => 42;
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (9,27): error CS0117: 'int' does not contain a definition for 'Current'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("int", "Current").WithLocation(9, 27),
                    // (9,27): error CS0202: foreach requires that the return type 'int' of 'Extensions2.GetEnumerator(C)' must have a suitable public 'MoveNext' method and public 'Current' property
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new C()").WithArguments("int", "Extensions2.GetEnumerator(C)").WithLocation(9, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAmbiguousExtensionsWhenOneHasOptionalParams()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions1
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}
public static class Extensions2
{
    public static C.Enumerator GetEnumerator(this C self, int a = 0) => throw null;
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAmbiguousExtensionsWhenOneHasFewerOptionalParams()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions1
{
    public static C.Enumerator GetEnumerator(this C self, int a = 0, int b = 1) => new C.Enumerator();
}
public static class Extensions2
{
    public static C.Enumerator GetEnumerator(this C self, int a = 0) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): warning CS0278: 'C' does not implement the 'collection' pattern. 'Extensions1.GetEnumerator(C, int, int)' is ambiguous with 'Extensions2.GetEnumerator(C, int)'.
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_PatternIsAmbiguous, "new C()").WithArguments("C", "collection", "Extensions1.GetEnumerator(C, int, int)", "Extensions2.GetEnumerator(C, int)").WithLocation(7, 27),
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionWithOptionalParameter()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public Enumerator(int start) => Current = start;
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self, int x = 1) => new C.Enumerator(x);
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "23");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionWithParams()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public Enumerator(int[] arr) => Current = arr.Length;
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self, params int[] x) => new C.Enumerator(x);
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionWithArgList()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self, __arglist) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                 .VerifyDiagnostics(
                    // (7,27): error CS7036: There is no argument given that corresponds to the required parameter '__arglist' of 'Extensions.GetEnumerator(C, __arglist)'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "new C()").WithArguments("__arglist", "Extensions.GetEnumerator(C, __arglist)").WithLocation(7, 27),
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27));
        }

        [Fact]
        public void TestGetEnumeratorPatternViaRefExtensionOnNonAssignableVariable()
        {
            var source = @"
using System;
public struct C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this ref C self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                 .VerifyDiagnostics(
                    // (7,27): error CS1510: A ref or out value must be an assignable variable
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "new C()").WithLocation(7, 27));
        }

        [Fact]
        public void TestGetEnumeratorPatternViaRefExtensionOnAssignableVariable()
        {
            var source = @"
using System;
public struct C
{
    public static void Main()
    {
        var c = new C();
        foreach (var i in c)
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this ref C self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (8,27): error CS1510: A ref or out value must be an assignable variable
                    //         foreach (var i in c)
                    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "c").WithLocation(8, 27)
                );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaOutExtension()
        {
            var source = @"
using System;
public struct C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this out C self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS1620: Argument 1 must be passed with the 'out' keyword
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadArgRef, "new C()").WithArguments("1", "out").WithLocation(7, 27),
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27),
                    // (20,51): error CS8328:  The parameter modifier 'out' cannot be used with 'this'
                    //     public static C.Enumerator GetEnumerator(this out C self) => new C.Enumerator();
                    Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this").WithLocation(20, 51));
        }

        [Fact]
        public void TestGetEnumeratorPatternViaInExtensionOnNonAssignableVariable()
        {
            var source = @"
using System;
public struct C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this in C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaInExtensionOnAssignableVariable()
        {
            var source = @"
using System;
public struct C
{
    public static void Main()
    {
        var c = new C();
        foreach (var i in c)
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this in C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Theory, CombinatorialData]
        public void TestGetEnumeratorPatternViaInExtensionOnAssignableVariable_OptionalParameter(
            [CombinatorialValues("ref", "in", "ref readonly", "")] string modifier)
        {
            var source = $$"""
                using System;
                public struct C
                {
                    public static void Main()
                    {
                        var c = new C();
                        foreach (var i in c)
                        {
                            Console.Write(i);
                        }
                    }
                    public struct Enumerator
                    {
                        public int Current { get; private set; }
                        public bool MoveNext() => Current++ != 3;
                    }
                }
                public static class Extensions
                {
                    public static C.Enumerator GetEnumerator(this in C self, {{modifier}} int x = 9)
                    {
                        Console.Write(x);
                        return new C.Enumerator();
                    }
                }
                """;
            if (modifier == "ref")
            {
                CreateCompilation(source).VerifyDiagnostics(
                    // (7,27): error CS7036: There is no argument given that corresponds to the required parameter 'x' of 'Extensions.GetEnumerator(in C, ref int)'
                    //         foreach (var i in c)
                    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "c").WithArguments("x", "Extensions.GetEnumerator(in C, ref int)").WithLocation(7, 27),
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in c)
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "c").WithArguments("C", "GetEnumerator").WithLocation(7, 27),
                    // (20,62): error CS1741: A ref or out parameter cannot have a default value
                    //     public static C.Enumerator GetEnumerator(this in C self, ref int x = 9)
                    Diagnostic(ErrorCode.ERR_RefOutDefaultValue, "ref").WithLocation(20, 62));
            }
            else
            {
                var verifier = CompileAndVerify(source, expectedOutput: "9123");
                if (modifier == "ref readonly")
                {
                    verifier.VerifyDiagnostics(
                        // (20,83): warning CS9200: A default value is specified for 'ref readonly' parameter 'x', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
                        //     public static C.Enumerator GetEnumerator(this in C self, ref readonly int x = 9)
                        Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "9").WithArguments("x").WithLocation(20, 83));
                }
                else
                {
                    verifier.VerifyDiagnostics();
                }
            }
        }

        [Theory, CombinatorialData]
        public void TestDisposePattern_OptionalParameter(
            [CombinatorialValues("ref", "in", "ref readonly", "")] string modifier)
        {
            var source = $$"""
                using System;
                public struct C
                {
                    public static void Main()
                    {
                        var c = new C();
                        foreach (var i in c)
                        {
                            Console.Write(i);
                        }
                    }
                    public Enumerator GetEnumerator()
                    {
                        return new Enumerator();
                    }
                    public ref struct Enumerator
                    {
                        public int Current { get; private set; }
                        public bool MoveNext() => Current++ != 3;
                        public void Dispose({{modifier}} int x = 5) { Console.Write(x); }
                    }
                }
                """;
            if (modifier == "ref")
            {
                CreateCompilation(source).VerifyDiagnostics(
                    // (20,29): error CS1741: A ref or out parameter cannot have a default value
                    //         public void Dispose(ref int x = 5) { Console.Write(x); }
                    Diagnostic(ErrorCode.ERR_RefOutDefaultValue, "ref").WithLocation(20, 29));
            }
            else
            {
                var verifier = CompileAndVerify(source, expectedOutput: "1235", verify: Verification.FailsILVerify);
                if (modifier == "ref readonly")
                {
                    verifier.VerifyDiagnostics(
                        // (20,50): warning CS9200: A default value is specified for 'ref readonly' parameter 'x', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
                        //         public void Dispose(ref readonly int x = 5) { Console.Write(x); }
                        Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "5").WithArguments("x").WithLocation(20, 50));
                }
                else
                {
                    verifier.VerifyDiagnostics();
                }
            }
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionsCSharp8()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,27): error CS8400: Feature 'extension GetEnumerator' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "new C()").WithArguments("extension GetEnumerator", "9.0").WithLocation(7, 27)
                );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaInternalExtensions()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    internal static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionOnInternalClass()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
internal static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionWithInvalidEnumerator()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
    }
}
internal static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS0117: 'C.Enumerator' does not contain a definition for 'MoveNext'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator", "MoveNext").WithLocation(7, 27),
                    // (7,27): error CS0202: foreach requires that the return type 'C.Enumerator' of 'Extensions.GetEnumerator(C)' must have a suitable public 'MoveNext' method and public 'Current' property
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new C()").WithArguments("C.Enumerator", "Extensions.GetEnumerator(C)").WithLocation(7, 27));
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionWithInstanceGetEnumeratorReturningTypeWhichDoesntMatchPattern()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }

    public sealed class Enumerator1
    {
        public int Current { get; private set; }
    }

    public sealed class Enumerator2
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }

    public Enumerator1 GetEnumerator() => new Enumerator1();
}
internal static class Extensions
{
    public static C.Enumerator2 GetEnumerator(this C self) => new C.Enumerator2();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS0117: 'C.Enumerator1' does not contain a definition for 'MoveNext'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("C.Enumerator1", "MoveNext").WithLocation(7, 27),
                    // (7,27): error CS0202: foreach requires that the return type 'C.Enumerator1' of 'C.GetEnumerator()' must have a suitable public 'MoveNext' method and public 'Current' property
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new C()").WithArguments("C.Enumerator1", "C.GetEnumerator()").WithLocation(7, 27)
                );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionWithInternalInstanceGetEnumerator()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }

    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }

    internal Enumerator GetEnumerator() => throw null;
}
internal static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionWithInstanceGetEnumeratorWithTooManyParameters()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }

    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }

    internal Enumerator GetEnumerator(int a) => throw null;
}
internal static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionWithStaticGetEnumeratorDeclaredInType()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }

    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }

    public static Enumerator GetEnumerator() => throw null;
}
internal static class Extensions
{
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestForEachViaExtensionImplicitlyDisposableStruct()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        foreach (var x in new C())
        {
            Console.Write(x);
        }
    }
}

static class Extensions
{
    public static Enumerator GetEnumerator(this C _) => new Enumerator();
}

struct Enumerator : IDisposable
{
    public int Current { get; private set; }
    public bool MoveNext() => Current++ != 3;
    public void Dispose() { Console.Write(""Disposed""); }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: @"123Disposed")
                .VerifyIL("C.Main", @"
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  call       ""Enumerator Extensions.GetEnumerator(C)""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  br.s       IL_0019
    IL_000d:  ldloca.s   V_0
    IL_000f:  call       ""readonly int Enumerator.Current.get""
    IL_0014:  call       ""void System.Console.Write(int)""
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
        public void TestForEachViaExtensionExplicitlyDisposableStruct()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        foreach (var x in new C())
        {
            Console.Write(x);
        }
    }
}

static class Extensions
{
    public static Enumerator GetEnumerator(this C _) => new Enumerator();
}

struct Enumerator : IDisposable
{
    public int Current { get; private set; }
    public bool MoveNext() => Current++ != 3;
    void IDisposable.Dispose() { Console.Write(""Disposed""); }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: @"123Disposed")
                .VerifyIL("C.Main", @"
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  call       ""Enumerator Extensions.GetEnumerator(C)""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  br.s       IL_0019
    IL_000d:  ldloca.s   V_0
    IL_000f:  call       ""readonly int Enumerator.Current.get""
    IL_0014:  call       ""void System.Console.Write(int)""
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
        public void TestForEachViaExtensionDisposeStruct()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        foreach (var x in new C())
        {
            Console.Write(x);
        }
    }
}

static class Extensions
{
    public static Enumerator GetEnumerator(this C _) => new Enumerator();
}

struct Enumerator
{
    public int Current { get; private set; }
    public bool MoveNext() => Current++ != 3;
    public void Dispose() { Console.Write(""Disposed""); }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: @"123")
                .VerifyIL("C.Main", @"
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  call       ""Enumerator Extensions.GetEnumerator(C)""
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0019
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""readonly int Enumerator.Current.get""
  IL_0014:  call       ""void System.Console.Write(int)""
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       ""bool Enumerator.MoveNext()""
  IL_0020:  brtrue.s   IL_000d
  IL_0022:  ret
}");
        }

        [Fact]
        public void TestForEachViaExtensionDisposeRefStruct()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        foreach (var x in new C())
        {
            Console.Write(x);
        }
    }
}

static class Extensions
{
    public static Enumerator GetEnumerator(this C _) => new Enumerator();
}

ref struct Enumerator
{
    public int Current { get; private set; }
    public bool MoveNext() => Current++ != 3;
    public void Dispose() { Console.Write(""Disposed""); }
}";
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, verify: Verification.FailsILVerify, expectedOutput: @"123Disposed")
                .VerifyIL("C.Main", @"
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  call       ""Enumerator Extensions.GetEnumerator(C)""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  br.s       IL_0019
    IL_000d:  ldloca.s   V_0
    IL_000f:  call       ""readonly int Enumerator.Current.get""
    IL_0014:  call       ""void System.Console.Write(int)""
    IL_0019:  ldloca.s   V_0
    IL_001b:  call       ""bool Enumerator.MoveNext()""
    IL_0020:  brtrue.s   IL_000d
    IL_0022:  leave.s    IL_002c
  }
  finally
  {
    IL_0024:  ldloca.s   V_0
    IL_0026:  call       ""void Enumerator.Dispose()""
    IL_002b:  endfinally
  }
  IL_002c:  ret
}");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaObsoleteExtension()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        [Obsolete]
        public int Current { get; private set; }
        [Obsolete]
        public bool MoveNext() => Current++ != 3;
    }
}
[Obsolete]
public static class Extensions
{
    [Obsolete]
    public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123")
                .VerifyDiagnostics(
                    // (7,9): warning CS0612: 'Extensions.GetEnumerator(C)' is obsolete
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("Extensions.GetEnumerator(C)").WithLocation(7, 9),
                    // (7,9): warning CS0612: 'C.Enumerator.MoveNext()' is obsolete
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.Enumerator.MoveNext()").WithLocation(7, 9),
                    // (7,9): warning CS0612: 'C.Enumerator.Current' is obsolete
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.Enumerator.Current").WithLocation(7, 9));
        }

        [Fact]
        public void TestGetEnumeratorPatternViaImportedExtension()
        {
            var source = @"
using System;
using N;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
namespace N
{
    public static class Extensions
    {
        public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaUnimportedExtension()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
namespace N
{
    public static class Extensions
    {
        public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                // (7,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                //         foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27));
        }

        [Fact]
        public void TestGetEnumeratorPatternViaValidExtensionInClosestNamespaceInvalidInFurtherNamespace1()
        {
            var source = @"
using System;
using N1.N2.N3;

namespace N1
{
    public static class Extensions
    {
        public static int GetEnumerator(this C self) => throw null;
    }

    namespace N2
    {
        public static class Extensions
        {
            public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
        }

        namespace N3
        {
            public class C
            {
                public static void Main()
                {
                    foreach (var i in new C())
                    {
                        Console.Write(i);
                    }
                }
                public sealed class Enumerator
                {
                    public int Current { get; private set; }
                    public bool MoveNext() => Current++ != 3;
                }
            }
        }
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaValidExtensionInClosestNamespaceInvalidInFurtherNamespace2()
        {
            var source = @"
using System;
using N1;
using N3;

namespace N1
{
    using N2;
    public class C
    {
        public static void Main()
        {
            foreach (var i in new C())
            {
                Console.Write(i);
            }
        }
        public sealed class Enumerator
        {
            public int Current { get; private set; }
            public bool MoveNext() => Current++ != 3;
        }
    }
}

namespace N2
{
    public static class Extensions
    {
        public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
    }
}

namespace N3
{
    public static class Extensions
    {
        public static int GetEnumerator(this C self) => throw null;
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaInvalidExtensionInClosestNamespaceValidInFurtherNamespace1()
        {
            var source = @"
using System;
using N1.N2.N3;

namespace N1
{
    public static class Extensions
    {
        public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
    }

    namespace N2
    {
        public static class Extensions
        {
            public static int GetEnumerator(this C self) => throw null;
        }

        namespace N3
        {
            public class C
            {
                public static void Main()
                {
                    foreach (var i in new C())
                    {
                        Console.Write(i);
                    }
                }
                public sealed class Enumerator
                {
                    public int Current { get; private set; }
                    public bool MoveNext() => Current++ != 3;
                }
            }
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (25,39): error CS0117: 'int' does not contain a definition for 'Current'
                    //                     foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("int", "Current").WithLocation(25, 39),
                    // (25,39): error CS0202: foreach requires that the return type 'int' of 'Extensions.GetEnumerator(C)' must have a suitable public 'MoveNext' method and public 'Current' property
                    //                     foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new C()").WithArguments("int", "N1.N2.Extensions.GetEnumerator(N1.N2.N3.C)").WithLocation(25, 39));
        }

        [Fact]
        public void TestGetEnumeratorPatternViaInvalidExtensionInClosestNamespaceValidInFurtherNamespace2()
        {
            var source = @"
using System;
using N1;
using N2;

namespace N1
{
    using N3;
    public class C
    {
        public static void Main()
        {
            foreach (var i in new C())
            {
                Console.Write(i);
            }
        }
        public sealed class Enumerator
        {
            public int Current { get; private set; }
            public bool MoveNext() => Current++ != 3;
        }
    }
}

namespace N2
{
    public static class Extensions
    {
        public static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
    }
}

namespace N3
{
    public static class Extensions
    {
        public static int GetEnumerator(this C self) => throw null;
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (4,1): hidden CS8019: Unnecessary using directive.
                    // using N2;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(4, 1),
                    // (13,31): error CS0117: 'int' does not contain a definition for 'Current'
                    //             foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "new C()").WithArguments("int", "Current").WithLocation(13, 31),
                    // (13,31): error CS0202: foreach requires that the return type 'int' of 'Extensions.GetEnumerator(C)' must have a suitable public 'MoveNext' method and public 'Current' property
                    //             foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new C()").WithArguments("int", "N3.Extensions.GetEnumerator(N1.C)").WithLocation(13, 31));
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAccessiblePrivateExtension()
        {
            var source = @"
using System;

public static class Program
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }

    private static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}

public class C
{
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaAccessiblePrivateExtensionInNestedClass()
        {
            var source = @"
using System;

public static class Program
{
    public static class Inner
    {
        public static void Main()
        {
            foreach (var i in new C())
            {
                Console.Write(i);
            }
        }
    }

    private static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}

public class C
{
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123");
        }

        [Fact]
        public void TestGetEnumeratorPatternViaInaccessiblePrivateExtension()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public sealed class Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    private static C.Enumerator GetEnumerator(this C self) => new C.Enumerator();
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics(
                    // (7,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                    //         foreach (var i in new C())
                    Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(7, 27)
                    );
        }

        [Fact]
        public void TestGetEnumeratorPatternViaExtensionWithRefReturn()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        foreach (var i in new C())
        {
            Console.Write(i);
        }
        
        foreach (var i in new C())
        {
            Console.Write(i);
        }
    }
    public struct Enumerator
    {
        public int Current { get; private set; }
        public bool MoveNext() => Current++ != 3;
    }
}
public static class Extensions
{
    public static C.Enumerator Instance = new C.Enumerator();
    public static ref C.Enumerator GetEnumerator(this C self) => ref Instance;
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular9, expectedOutput: "123123");
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/73741")]
        public void MutatingThroughRefFields_01(
            [CombinatorialValues("ref", "")] string eRef,
            [CombinatorialValues("readonly", "")] string vReadonly)
        {
            var source = $$"""
                using System;

                V[] arr = new V[3];

                foreach (var r in new E(arr))
                {
                    r.V.F++;
                }

                foreach (var v in arr) Console.Write(v.F);

                {{eRef}} struct E(V[] arr)
                {
                    int i;
                    public E GetEnumerator() => this;
                    public R Current => new(ref arr[i - 1]);
                    public bool MoveNext() => i++ < arr.Length;
                }

                ref struct R(ref V v)
                {
                    public {{vReadonly}} ref V V = ref v;
                }

                struct V
                {
                    public int F;
                }
                """;
            CompileAndVerify(source, targetFramework: TargetFramework.Net70,
                verify: Verification.Fails,
                expectedOutput: ExecutionConditionUtil.IsDesktop ? null : "111").VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/73741")]
        public void MutatingThroughRefFields_02(
            [CombinatorialValues("ref", "")] string eRef,
            [CombinatorialValues("readonly", "")] string vReadonly)
        {
            var source = $$"""
                using System;

                V[] arr = new V[3];

                foreach (var r in new E(arr))
                {
                    r.V.F += 2;
                }

                foreach (var v in arr) Console.Write(v.F);

                {{eRef}} struct E(V[] arr)
                {
                    int i;
                    public E GetEnumerator() => this;
                    public R Current => new(ref arr[i - 1]);
                    public bool MoveNext() => i++ < arr.Length;
                }

                ref struct R(ref V v)
                {
                    public {{vReadonly}} ref V V = ref v;
                }

                struct V
                {
                    public int F;
                }
                """;
            CompileAndVerify(source, targetFramework: TargetFramework.Net70,
                verify: Verification.Fails,
                expectedOutput: ExecutionConditionUtil.IsDesktop ? null : "222").VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/73741")]
        public void MutatingThroughRefFields_03(
            [CombinatorialValues("ref", "")] string eRef,
            [CombinatorialValues("readonly", "")] string vReadonly)
        {
            var source = $$"""
                using System;

                V[] arr = new V[3];

                foreach (var r in new E(arr))
                {
                    r.V.S.Inc();
                }

                foreach (var v in arr) Console.Write(v.S.F);

                {{eRef}} struct E(V[] arr)
                {
                    int i;
                    public E GetEnumerator() => this;
                    public R Current => new(ref arr[i - 1]);
                    public bool MoveNext() => i++ < arr.Length;
                }

                ref struct R(ref V v)
                {
                    public {{vReadonly}} ref V V = ref v;
                }

                struct V
                {
                    public S S;
                }

                struct S
                {
                    public int F;
                    public void Inc() => F++;
                }
                """;
            CompileAndVerify(source, targetFramework: TargetFramework.Net70,
                verify: Verification.Fails,
                expectedOutput: ExecutionConditionUtil.IsDesktop ? null : "111").VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/73741")]
        public void MutatingThroughRefFields_04(
            [CombinatorialValues("ref", "")] string eRef,
            [CombinatorialValues("readonly", "")] string vReadonly)
        {
            var source = $$"""
                using System;

                V[] arr = new V[3];

                foreach (var r in new E(arr))
                {
                    r.V.F++;
                }

                foreach (var v in arr) Console.Write(v.F);

                {{eRef}} struct E(V[] arr)
                {
                    int i;
                    public E GetEnumerator() => this;
                    public R Current => new(ref arr[i - 1]);
                    public bool MoveNext() => i++ < arr.Length;
                }

                ref struct R(ref V v)
                {
                    public {{vReadonly}} ref readonly V V = ref v;
                }

                struct V
                {
                    public int F;
                }
                """;
            CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyDiagnostics(
                // (7,5): error CS8332: Cannot assign to a member of field 'V' or use it as the right hand side of a ref assignment because it is a readonly variable
                //     r.V.F++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "r.V.F").WithArguments("field", "V").WithLocation(7, 5));
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/73741")]
        public void MutatingThroughRefFields_05(
            [CombinatorialValues("ref", "")] string eRef,
            [CombinatorialValues("readonly", "")] string vReadonly)
        {
            var source = $$"""
                using System;

                V[] arr = new V[3];

                foreach (ref var r in new E(arr))
                {
                    r.S.F++;
                }

                foreach (var v in arr) Console.Write(v.S.F);

                {{eRef}} struct E(V[] arr)
                {
                    int i;
                    public E GetEnumerator() => this;
                    public {{vReadonly}} ref V Current => ref arr[i - 1];
                    public bool MoveNext() => i++ < arr.Length;
                }

                struct V
                {
                    public S S;
                }

                struct S
                {
                    public int F;
                }
                """;
            CompileAndVerify(source, targetFramework: TargetFramework.Net70,
                verify: Verification.Skipped,
                expectedOutput: ExecutionConditionUtil.IsDesktop ? null : "111").VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/73741")]
        public void MutatingThroughRefFields_06(
            [CombinatorialValues("ref", "")] string eRef,
            [CombinatorialValues("readonly", "")] string vReadonly,
            [CombinatorialValues("readonly", "")] string vReadonlyInner)
        {
            var source = $$"""
                using System;

                V[] arr = new V[3];

                foreach (ref readonly var r in new E(arr))
                {
                    r.S.F++;
                }

                foreach (var v in arr) Console.Write(v.S.F);

                {{eRef}} struct E(V[] arr)
                {
                    int i;
                    public E GetEnumerator() => this;
                    public {{vReadonly}} ref {{vReadonlyInner}} V Current => ref arr[i - 1];
                    public bool MoveNext() => i++ < arr.Length;
                }

                struct V
                {
                    public S S;
                }

                struct S
                {
                    public int F;
                }
                """;
            CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyDiagnostics(
                // (7,5): error CS1654: Cannot modify members of 'r' because it is a 'foreach iteration variable'
                //     r.S.F++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal2Cause, "r.S.F").WithArguments("r", "foreach iteration variable").WithLocation(7, 5));
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2187060")]
        public void ExtensionDisposeMethodWithParams()
        {
            var source = """
System.ReadOnlySpan<int> values = [4, 2];
foreach (int value in values) { System.Console.Write(value); }

public static class C
{
    public static void Dispose(this int i, params int[] other) { }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "42" : null, verify: Verification.Skipped)
                .VerifyDiagnostics();
        }

        [Fact]
        public void TestWithPatternAndObsolete_WithDisposableInterface()
        {
            string source = """
foreach (var i in new C())
{
}
class C
{
    [System.Obsolete]
    public MyEnumerator GetEnumerator()
    {
        throw null;
    }
    [System.Obsolete]
    public sealed class MyEnumerator : System.IDisposable
    {
        [System.Obsolete]
        public int Current { get => throw null; }
        [System.Obsolete]
        public bool MoveNext() => throw null;
        [System.Obsolete("error", true)]
        public void Dispose() => throw null;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,1): warning CS0612: 'C.GetEnumerator()' is obsolete
                // foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.GetEnumerator()").WithLocation(1, 1),
                // (1,1): warning CS0612: 'C.MyEnumerator.MoveNext()' is obsolete
                // foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.MyEnumerator.MoveNext()").WithLocation(1, 1),
                // (1,1): warning CS0612: 'C.MyEnumerator.Current' is obsolete
                // foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.MyEnumerator.Current").WithLocation(1, 1));
            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       41 (0x29)
  .maxstack  1
  .locals init (C.MyEnumerator V_0)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  call       "C.MyEnumerator C.GetEnumerator()"
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  br.s       IL_0014
    IL_000d:  ldloc.0
    IL_000e:  callvirt   "int C.MyEnumerator.Current.get"
    IL_0013:  pop
    IL_0014:  ldloc.0
    IL_0015:  callvirt   "bool C.MyEnumerator.MoveNext()"
    IL_001a:  brtrue.s   IL_000d
    IL_001c:  leave.s    IL_0028
  }
  finally
  {
    IL_001e:  ldloc.0
    IL_001f:  brfalse.s  IL_0027
    IL_0021:  ldloc.0
    IL_0022:  callvirt   "void System.IDisposable.Dispose()"
    IL_0027:  endfinally
  }
  IL_0028:  ret
}
""");
        }

        [Fact]
        public void TestWithPatternAndObsolete_WithoutDisposableInterface()
        {
            string source = """
foreach (var i in new C())
{
}
class C
{
    [System.Obsolete]
    public MyEnumerator GetEnumerator()
    {
        throw null;
    }
    [System.Obsolete]
    public sealed class MyEnumerator
    {
        [System.Obsolete]
        public int Current { get => throw null; }
        [System.Obsolete]
        public bool MoveNext() => throw null;
        [System.Obsolete("error", true)]
        public void Dispose() => throw null;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,1): warning CS0612: 'C.GetEnumerator()' is obsolete
                // foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.GetEnumerator()").WithLocation(1, 1),
                // (1,1): warning CS0612: 'C.MyEnumerator.MoveNext()' is obsolete
                // foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.MyEnumerator.MoveNext()").WithLocation(1, 1),
                // (1,1): warning CS0612: 'C.MyEnumerator.Current' is obsolete
                // foreach (var i in new C())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "foreach").WithArguments("C.MyEnumerator.Current").WithLocation(1, 1));
            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (C.MyEnumerator V_0)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  call       "C.MyEnumerator C.GetEnumerator()"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0014
  IL_000d:  ldloc.0
  IL_000e:  callvirt   "int C.MyEnumerator.Current.get"
  IL_0013:  pop
  IL_0014:  ldloc.0
  IL_0015:  callvirt   "bool C.MyEnumerator.MoveNext()"
  IL_001a:  brtrue.s   IL_000d
  IL_001c:  ret
}
""");
        }

        [Fact]
        public void TestWithPatternAndObsolete_WithoutDisposableInterface_RefStructEnumerator()
        {
            string source = """
foreach (var i in new C())
{
}

class C
{
    public MyEnumerator GetEnumerator()
    {
        throw null;
    }

    public ref struct MyEnumerator
    {
        public int Current { get => throw null; }
        public bool MoveNext() => throw null;

        [System.Obsolete("error", true)]
        public void Dispose() => throw null;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,1): error CS0619: 'C.MyEnumerator.Dispose()' is obsolete: 'error'
                // foreach (var i in new C())
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "foreach").WithArguments("C.MyEnumerator.Dispose()", "error").WithLocation(1, 1));
        }

        [Fact]
        public void TestWithPatternAndObsolete_WithoutDisposableInterface_RefStructEnumerator_Spread()
        {
            string source = """
int[] a = [42, ..new C()];

class C
{
    public MyEnumerator GetEnumerator()
    {
        throw null;
    }

    public ref struct MyEnumerator
    {
        public int Current { get => throw null; }
        public bool MoveNext() => throw null;

        [System.Obsolete("error", true)]
        public void Dispose() => throw null;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,16): error CS0619: 'C.MyEnumerator.Dispose()' is obsolete: 'error'
                // int[] a = [42, ..new C()];
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..new C()").WithArguments("C.MyEnumerator.Dispose()", "error").WithLocation(1, 16));
        }

        [Fact]
        public void TestWithPatternAndObsolete_WithoutDisposableInterface_RefStructEnumerator_CollectionType()
        {
            string source = """
C c = [42];

[System.Runtime.CompilerServices.CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
public class C
{
    public MyEnumerator GetEnumerator()
    {
        throw null;
    }

    public ref struct MyEnumerator
    {
        public int Current { get => throw null; }
        public bool MoveNext() => throw null;

        [System.Obsolete("error", true)]
        public void Dispose() => throw null;
    }
}

public class MyCollectionBuilder
{
    public static C Create(System.ReadOnlySpan<int> items) => throw null;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics();
        }
    }
}
