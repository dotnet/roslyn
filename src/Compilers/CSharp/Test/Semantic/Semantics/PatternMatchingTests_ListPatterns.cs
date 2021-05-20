// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTests_ListPatterns : PatternMatchingTestBase
    {
        [Fact]
        public void ListPattern()
        {
            string testMethod(string type) =>
@"static bool Test(" + type + @" input)
{
    switch (input)
    {
        case [0]:
        case {_}:
          return true;
        case {var first, ..var others, var last} when first == last:
          return Test(others);
        default:
          return false;
    }
}";
            var source = @"
using System;
public class X
{
    " + testMethod("Span<char>") + @"
    " + testMethod("char[]") + @"
    " + testMethod("string") + @"
    static void Check(int num)
    {
        Console.Write(Test((string)num.ToString()) ? 1 : 0);
        Console.Write(Test((char[])num.ToString().ToCharArray()) ? 1 : 0);
        Console.Write(Test((Span<char>)num.ToString().ToCharArray()) ? 1 : 0);
        Console.WriteLine();
    }
    public static void Main()
    {
        Check(1);
        Check(11);
        Check(12);
        Check(123);
        Check(121);
        Check(1221);
        Check(1222);
    }
}
" + TestSources.GetSubArray;
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            var expectedOutput = @"
111
111
000
000
111
111
000";
            var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            AssertEx.Multiple(
                () => verifier.VerifyIL("X.Test(System.Span<char>)", @"
{
  // Code size       76 (0x4c)
  .maxstack  4
  .locals init (char V_0, //first
                System.Span<char> V_1, //others
                char V_2, //last
                System.Span<char> V_3,
                int V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.3
  IL_0002:  ldloca.s   V_3
  IL_0004:  call       ""int System.Span<char>.Length.get""
  IL_0009:  stloc.s    V_4
  IL_000b:  ldloc.s    V_4
  IL_000d:  ldc.i4.2
  IL_000e:  bge.s      IL_0017
  IL_0010:  ldloc.s    V_4
  IL_0012:  ldc.i4.1
  IL_0013:  ble.un.s   IL_003d
  IL_0015:  br.s       IL_004a
  IL_0017:  ldloca.s   V_3
  IL_0019:  ldc.i4.0
  IL_001a:  call       ""ref char System.Span<char>.this[int].get""
  IL_001f:  ldind.u2
  IL_0020:  stloc.0
  IL_0021:  ldloca.s   V_3
  IL_0023:  ldc.i4.1
  IL_0024:  ldloc.s    V_4
  IL_0026:  ldc.i4.2
  IL_0027:  sub
  IL_0028:  call       ""System.Span<char> System.Span<char>.Slice(int, int)""
  IL_002d:  stloc.1
  IL_002e:  ldloca.s   V_3
  IL_0030:  ldloc.s    V_4
  IL_0032:  ldc.i4.1
  IL_0033:  sub
  IL_0034:  call       ""ref char System.Span<char>.this[int].get""
  IL_0039:  ldind.u2
  IL_003a:  stloc.2
  IL_003b:  br.s       IL_003f
  IL_003d:  ldc.i4.1
  IL_003e:  ret
  IL_003f:  ldloc.0
  IL_0040:  ldloc.2
  IL_0041:  bne.un.s   IL_004a
  IL_0043:  ldloc.1
  IL_0044:  call       ""bool X.Test(System.Span<char>)""
  IL_0049:  ret
  IL_004a:  ldc.i4.0
  IL_004b:  ret
}
"),
                () => verifier.VerifyIL("X.Test(char[])", @"
{
  // Code size       79 (0x4f)
  .maxstack  4
  .locals init (char V_0, //first
                char[] V_1, //others
                char V_2, //last
                char[] V_3,
                int V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.3
  IL_0002:  ldloc.3
  IL_0003:  brfalse.s  IL_004d
  IL_0005:  ldloc.3
  IL_0006:  callvirt   ""int System.Array.Length.get""
  IL_000b:  stloc.s    V_4
  IL_000d:  ldloc.s    V_4
  IL_000f:  ldc.i4.2
  IL_0010:  bge.s      IL_0019
  IL_0012:  ldloc.s    V_4
  IL_0014:  ldc.i4.1
  IL_0015:  ble.un.s   IL_0040
  IL_0017:  br.s       IL_004d
  IL_0019:  ldloc.3
  IL_001a:  ldc.i4.0
  IL_001b:  ldelem.u2
  IL_001c:  stloc.0
  IL_001d:  ldloc.3
  IL_001e:  ldc.i4.1
  IL_001f:  ldc.i4.0
  IL_0020:  newobj     ""System.Index..ctor(int, bool)""
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.1
  IL_0027:  newobj     ""System.Index..ctor(int, bool)""
  IL_002c:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0031:  call       ""char[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<char>(char[], System.Range)""
  IL_0036:  stloc.1
  IL_0037:  ldloc.3
  IL_0038:  ldloc.s    V_4
  IL_003a:  ldc.i4.1
  IL_003b:  sub
  IL_003c:  ldelem.u2
  IL_003d:  stloc.2
  IL_003e:  br.s       IL_0042
  IL_0040:  ldc.i4.1
  IL_0041:  ret
  IL_0042:  ldloc.0
  IL_0043:  ldloc.2
  IL_0044:  bne.un.s   IL_004d
  IL_0046:  ldloc.1
  IL_0047:  call       ""bool X.Test(char[])""
  IL_004c:  ret
  IL_004d:  ldc.i4.0
  IL_004e:  ret
}
"),
                () => verifier.VerifyIL("X.Test(string)", @"
{
  // Code size       73 (0x49)
  .maxstack  4
  .locals init (char V_0, //first
                string V_1, //others
                char V_2, //last
                string V_3,
                int V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.3
  IL_0002:  ldloc.3
  IL_0003:  brfalse.s  IL_0047
  IL_0005:  ldloc.3
  IL_0006:  callvirt   ""int string.Length.get""
  IL_000b:  stloc.s    V_4
  IL_000d:  ldloc.s    V_4
  IL_000f:  ldc.i4.2
  IL_0010:  bge.s      IL_0019
  IL_0012:  ldloc.s    V_4
  IL_0014:  ldc.i4.1
  IL_0015:  ble.un.s   IL_003a
  IL_0017:  br.s       IL_0047
  IL_0019:  ldloc.3
  IL_001a:  ldc.i4.0
  IL_001b:  callvirt   ""char string.this[int].get""
  IL_0020:  stloc.0
  IL_0021:  ldloc.3
  IL_0022:  ldc.i4.1
  IL_0023:  ldloc.s    V_4
  IL_0025:  ldc.i4.2
  IL_0026:  sub
  IL_0027:  callvirt   ""string string.Substring(int, int)""
  IL_002c:  stloc.1
  IL_002d:  ldloc.3
  IL_002e:  ldloc.s    V_4
  IL_0030:  ldc.i4.1
  IL_0031:  sub
  IL_0032:  callvirt   ""char string.this[int].get""
  IL_0037:  stloc.2
  IL_0038:  br.s       IL_003c
  IL_003a:  ldc.i4.1
  IL_003b:  ret
  IL_003c:  ldloc.0
  IL_003d:  ldloc.2
  IL_003e:  bne.un.s   IL_0047
  IL_0040:  ldloc.1
  IL_0041:  call       ""bool X.Test(string)""
  IL_0046:  ret
  IL_0047:  ldc.i4.0
  IL_0048:  ret
}
")
                );
        }

        [Fact]
        public void LengthPattern()
        {
            var source = @"
using System;
class X
{
    public static int Test(int[] a, int[] b)
    {
        return (a, b) switch
        {
            ({1}, {1}) => 0,
            ([1], [1]) => 1,
            ([>1], [>1]) => 2,
            ([var length], _) => length,
            _ => -1
        };
    } 
    public static void Main()
    {
        Console.Write(Test(null, null));
        Console.Write(Test(new[]{1}, new[]{1}));
        Console.Write(Test(new[]{2}, new[]{2}));
        Console.Write(Test(new[]{1,2}, new[]{1,2}));
        Console.Write(Test(new[]{1,2,3}, null));
    } 
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(compilation, expectedOutput: "-10123");
            verifier.VerifyIL("X.Test", @"
{
  // Code size       92 (0x5c)
  .maxstack  2
  .locals init (int V_0, //length
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0058
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int System.Array.Length.get""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  bgt.s      IL_003a
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  bne.un.s   IL_0054
  IL_0012:  ldarg.0
  IL_0013:  ldc.i4.0
  IL_0014:  ldelem.i4
  IL_0015:  ldc.i4.1
  IL_0016:  bne.un.s   IL_002c
  IL_0018:  ldarg.1
  IL_0019:  brfalse.s  IL_0054
  IL_001b:  ldarg.1
  IL_001c:  callvirt   ""int System.Array.Length.get""
  IL_0021:  ldc.i4.1
  IL_0022:  bne.un.s   IL_0054
  IL_0024:  ldarg.1
  IL_0025:  ldc.i4.0
  IL_0026:  ldelem.i4
  IL_0027:  ldc.i4.1
  IL_0028:  beq.s      IL_0048
  IL_002a:  br.s       IL_004c
  IL_002c:  ldarg.1
  IL_002d:  brfalse.s  IL_0054
  IL_002f:  ldarg.1
  IL_0030:  callvirt   ""int System.Array.Length.get""
  IL_0035:  ldc.i4.1
  IL_0036:  beq.s      IL_004c
  IL_0038:  br.s       IL_0054
  IL_003a:  ldarg.1
  IL_003b:  brfalse.s  IL_0054
  IL_003d:  ldarg.1
  IL_003e:  callvirt   ""int System.Array.Length.get""
  IL_0043:  ldc.i4.1
  IL_0044:  bgt.s      IL_0050
  IL_0046:  br.s       IL_0054
  IL_0048:  ldc.i4.0
  IL_0049:  stloc.1
  IL_004a:  br.s       IL_005a
  IL_004c:  ldc.i4.1
  IL_004d:  stloc.1
  IL_004e:  br.s       IL_005a
  IL_0050:  ldc.i4.2
  IL_0051:  stloc.1
  IL_0052:  br.s       IL_005a
  IL_0054:  ldloc.0
  IL_0055:  stloc.1
  IL_0056:  br.s       IL_005a
  IL_0058:  ldc.i4.m1
  IL_0059:  stloc.1
  IL_005a:  ldloc.1
  IL_005b:  ret
}");
        }

        [Fact]
        public void ListPattern_Index()
        {
            var source = @"
using System;

class Test1 
{
    public int this[Index i] => 1;
    public int Length => 1;
}
class Test2
{
    public int this[Index i, int ignored = 5] => 1;
    public int Length => 1;
}
class Test3
{
    public int this[Index i, params int[] ignored] => 1;
    public int Length => 1;
}
class Test4
{
    public int this[params Index[] i] => 1;
    public int Length => 1;
}
class Test5
{
    public int this[int i] => 1;
    public int Length => 1;
}
class X
{
    void EnsureTypeIsIndexable()
    {
        _ = new Test1()[^1];
        _ = new Test2()[^1];
        _ = new Test3()[^1];
        _ = new Test4()[^1];
        _ = new Test5()[^1];
    }

    static bool Test1(Test1 t) => t is { 1 };
    static bool Test2(Test2 t) => t is { 1 };
    static bool Test3(Test3 t) => t is { 1 };
    static bool Test4(Test4 t) => t is { 1 };
    static bool Test5(Test5 t) => t is { 1 };

    public static void Main()
    {
        Console.WriteLine(Test1(new()));
        Console.WriteLine(Test2(new()));
        Console.WriteLine(Test3(new()));
        Console.WriteLine(Test4(new()));
        Console.WriteLine(Test5(new()));
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            var expectedOutput = @"
True
True
True
True
True
";
            var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);

            AssertEx.Multiple(
                () => verifier.VerifyIL("X.Test1", @"
{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_001d
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test1.Length.get""
  IL_0009:  ldc.i4.1
  IL_000a:  bne.un.s   IL_001d
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  newobj     ""System.Index..ctor(int, bool)""
  IL_0014:  callvirt   ""int Test1.this[System.Index].get""
  IL_0019:  ldc.i4.1
  IL_001a:  ceq
  IL_001c:  ret
  IL_001d:  ldc.i4.0
  IL_001e:  ret
}"),
                () => verifier.VerifyIL("X.Test2", @"
{
  // Code size       32 (0x20)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_001e
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test2.Length.get""
  IL_0009:  ldc.i4.1
  IL_000a:  bne.un.s   IL_001e
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  newobj     ""System.Index..ctor(int, bool)""
  IL_0014:  ldc.i4.5
  IL_0015:  callvirt   ""int Test2.this[System.Index, int].get""
  IL_001a:  ldc.i4.1
  IL_001b:  ceq
  IL_001d:  ret
  IL_001e:  ldc.i4.0
  IL_001f:  ret
}"),
                () => verifier.VerifyIL("X.Test3", @"
{
  // Code size       36 (0x24)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0022
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test3.Length.get""
  IL_0009:  ldc.i4.1
  IL_000a:  bne.un.s   IL_0022
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  newobj     ""System.Index..ctor(int, bool)""
  IL_0014:  call       ""int[] System.Array.Empty<int>()""
  IL_0019:  callvirt   ""int Test3.this[System.Index, params int[]].get""
  IL_001e:  ldc.i4.1
  IL_001f:  ceq
  IL_0021:  ret
  IL_0022:  ldc.i4.0
  IL_0023:  ret
}"),
                () => verifier.VerifyIL("X.Test4", @"
{
  // Code size       44 (0x2c)
  .maxstack  6
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_002a
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test4.Length.get""
  IL_0009:  ldc.i4.1
  IL_000a:  bne.un.s   IL_002a
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.1
  IL_000e:  newarr     ""System.Index""
  IL_0013:  dup
  IL_0014:  ldc.i4.0
  IL_0015:  ldc.i4.0
  IL_0016:  ldc.i4.0
  IL_0017:  newobj     ""System.Index..ctor(int, bool)""
  IL_001c:  stelem     ""System.Index""
  IL_0021:  callvirt   ""int Test4.this[params System.Index[]].get""
  IL_0026:  ldc.i4.1
  IL_0027:  ceq
  IL_0029:  ret
  IL_002a:  ldc.i4.0
  IL_002b:  ret
}"),
                () => verifier.VerifyIL("X.Test5", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0017
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test5.Length.get""
  IL_0009:  ldc.i4.1
  IL_000a:  bne.un.s   IL_0017
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.0
  IL_000e:  callvirt   ""int Test5.this[int].get""
  IL_0013:  ldc.i4.1
  IL_0014:  ceq
  IL_0016:  ret
  IL_0017:  ldc.i4.0
  IL_0018:  ret
}")
            );
        }

        [Fact]
        public void ListPattern_Range()
        {
            var source = @"
using System;

class Test1 
{
    public int this[Range i] => 1;
    public int this[int i] => throw new();
    public int Count => 1;
}
class Test2
{
    public int this[Range i, int ignored = 5] => 1;
    public int this[int i] => throw new();
    public int Count => 1;
}
class Test3
{
    public int this[Range i, params int[] ignored] => 1;
    public int this[int i] => throw new();
    public int Count => 1;
}
class Test4
{
    public int this[params Range[] i] => 1;
    public int this[int i] => throw new();
    public int Count => 1;
}
class Test5
{
    public int Slice(int i, int j) => 1;
    public int this[int i] => throw new();
    public int Count => 1;
}
class X
{
    void EnsureTypeIsSliceable()
    {
        _ = new Test1()[..];
        _ = new Test2()[..];
        _ = new Test3()[..];
        _ = new Test4()[..];
        _ = new Test5()[..];
    }

    static bool Test1(Test1 t) => t is { .. 1 };
    static bool Test2(Test2 t) => t is { .. 1 };
    static bool Test3(Test3 t) => t is { .. 1 };
    static bool Test4(Test4 t) => t is { .. 1 };
    static bool Test5(Test5 t) => t is { .. 1 };

    public static void Main()
    {
        Console.WriteLine(Test1(new()));
        Console.WriteLine(Test2(new()));
        Console.WriteLine(Test3(new()));
        Console.WriteLine(Test4(new()));
        Console.WriteLine(Test5(new()));
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            var expectedOutput = @"
True
True
True
True
True
";
            var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            AssertEx.Multiple(
                () => verifier.VerifyIL("X.Test1", @"
{
  // Code size       43 (0x2b)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0029
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test1.Count.get""
  IL_0009:  ldc.i4.0
  IL_000a:  blt.s      IL_0029
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  newobj     ""System.Index..ctor(int, bool)""
  IL_0014:  ldc.i4.0
  IL_0015:  ldc.i4.1
  IL_0016:  newobj     ""System.Index..ctor(int, bool)""
  IL_001b:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0020:  callvirt   ""int Test1.this[System.Range].get""
  IL_0025:  ldc.i4.1
  IL_0026:  ceq
  IL_0028:  ret
  IL_0029:  ldc.i4.0
  IL_002a:  ret
}"),
                () => verifier.VerifyIL("X.Test2", @"
{
  // Code size       44 (0x2c)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_002a
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test2.Count.get""
  IL_0009:  ldc.i4.0
  IL_000a:  blt.s      IL_002a
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  newobj     ""System.Index..ctor(int, bool)""
  IL_0014:  ldc.i4.0
  IL_0015:  ldc.i4.1
  IL_0016:  newobj     ""System.Index..ctor(int, bool)""
  IL_001b:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0020:  ldc.i4.5
  IL_0021:  callvirt   ""int Test2.this[System.Range, int].get""
  IL_0026:  ldc.i4.1
  IL_0027:  ceq
  IL_0029:  ret
  IL_002a:  ldc.i4.0
  IL_002b:  ret
}"),
                () => verifier.VerifyIL("X.Test3", @"
{
  // Code size       48 (0x30)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_002e
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test3.Count.get""
  IL_0009:  ldc.i4.0
  IL_000a:  blt.s      IL_002e
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  newobj     ""System.Index..ctor(int, bool)""
  IL_0014:  ldc.i4.0
  IL_0015:  ldc.i4.1
  IL_0016:  newobj     ""System.Index..ctor(int, bool)""
  IL_001b:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0020:  call       ""int[] System.Array.Empty<int>()""
  IL_0025:  callvirt   ""int Test3.this[System.Range, params int[]].get""
  IL_002a:  ldc.i4.1
  IL_002b:  ceq
  IL_002d:  ret
  IL_002e:  ldc.i4.0
  IL_002f:  ret
}"),
                () => verifier.VerifyIL("X.Test4", @"
{
  // Code size       56 (0x38)
  .maxstack  7
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0036
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test4.Count.get""
  IL_0009:  ldc.i4.0
  IL_000a:  blt.s      IL_0036
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.1
  IL_000e:  newarr     ""System.Range""
  IL_0013:  dup
  IL_0014:  ldc.i4.0
  IL_0015:  ldc.i4.0
  IL_0016:  ldc.i4.0
  IL_0017:  newobj     ""System.Index..ctor(int, bool)""
  IL_001c:  ldc.i4.0
  IL_001d:  ldc.i4.1
  IL_001e:  newobj     ""System.Index..ctor(int, bool)""
  IL_0023:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0028:  stelem     ""System.Range""
  IL_002d:  callvirt   ""int Test4.this[params System.Range[]].get""
  IL_0032:  ldc.i4.1
  IL_0033:  ceq
  IL_0035:  ret
  IL_0036:  ldc.i4.0
  IL_0037:  ret
}"),
                () => verifier.VerifyIL("X.Test5", @"
{
  // Code size       30 (0x1e)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_001c
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test5.Count.get""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  blt.s      IL_001c
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.0
  IL_0010:  ldloc.0
  IL_0011:  ldc.i4.0
  IL_0012:  sub
  IL_0013:  callvirt   ""int Test5.Slice(int, int)""
  IL_0018:  ldc.i4.1
  IL_0019:  ceq
  IL_001b:  ret
  IL_001c:  ldc.i4.0
  IL_001d:  ret
}")
            );
        }

        [Fact]
        public void ListPattern_UnsupportedTypes()
        {
            var source = @"
class X
{
    public static void Main()
    {
        _ = 0 is {0};
        _ = 0 is [0];
        _ = 0 is [0]{0};
        _ = 0 is {..0};
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics(
                // (6,18): error CS9200: List patterns may not be used for a value of type 'int'.
                //         _ = 0 is {0};
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "{0}").WithArguments("int").WithLocation(6, 18),
                // (7,18): error CS9202: Length patterns may not be used for a value of type 'int'.
                //         _ = 0 is [0];
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForLengthPattern, "[0]").WithArguments("int").WithLocation(7, 18),
                // (8,18): error CS9202: Length patterns may not be used for a value of type 'int'.
                //         _ = 0 is [0]{0};
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForLengthPattern, "[0]").WithArguments("int").WithLocation(8, 18),
                // (9,18): error CS9200: List patterns may not be used for a value of type 'int'.
                //         _ = 0 is {..0};
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "{..0}").WithArguments("int").WithLocation(9, 18),
                // (9,19): error CS9201: Slice patterns may not be used for a value of type 'int'.
                //         _ = 0 is {..0};
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..0").WithArguments("int").WithLocation(9, 19));
        }

        [Fact]
        public void ListPattern_MissingMembers_Constructors()
        {
            var source = @"
using System;
namespace System
{
    public struct Index
    {
    }
    public struct Range
    {
    }
}
class X
{
    public int this[Range i] => 1;
    public int this[Index i] => 1;
    public int Length => 1;

    public static void Main()
    {
        _ = new X() is { 1 };
        _ = new X() is { .. 1 };
    } 
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics(
                // (20,24): error CS0656: Missing compiler required member 'System.Index..ctor'
                //         _ = new X() is { 1 };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ 1 }").WithArguments("System.Index", ".ctor").WithLocation(20, 24),
                // (21,24): error CS0656: Missing compiler required member 'System.Index..ctor'
                //         _ = new X() is { .. 1 };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ .. 1 }").WithArguments("System.Index", ".ctor").WithLocation(21, 24),
                // (21,26): error CS0656: Missing compiler required member 'System.Range..ctor'
                //         _ = new X() is { .. 1 };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".. 1").WithArguments("System.Range", ".ctor").WithLocation(21, 26));
        }

        [Fact]
        public void ListPattern_MissingMembers_ArrayLength()
        {
            var source = @"
class X
{
    public static void Main()
    {
        _ = new int[0] is [0];
        _ = new int[0] is {0};
        _ = new int[0] is {.._};
    } 
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.MakeMemberMissing(SpecialMember.System_Array__Length);
            // PROTOTYPE(list-patterns) Nope
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation);
        }

        [Fact]
        public void ListPattern_MissingMembers_Substring()
        {
            var source = @"
class X
{
    public static void Main()
    {
        _ = """" is {.. var slice};
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.MakeMemberMissing(SpecialMember.System_String__Substring);
            compilation.VerifyEmitDiagnostics(
                // (6,20): error CS9201: Slice patterns may not be used for a value of type 'string'.
                //         _ = "" is {.. var slice};
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, ".. var slice").WithArguments("string").WithLocation(6, 20));
        }

        [Fact]
        public void ListPattern_MissingMembers_GetSubArray()
        {
            var source = @"
class X
{
    public static void Main()
    {
        _ = new int[0] is {.. var slice};
    } 
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics(
                // (6,27): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray'
                //         _ = new int[0] is {.. var slice};
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{.. var slice}").WithArguments("System.Runtime.CompilerServices.RuntimeHelpers", "GetSubArray").WithLocation(6, 27));
        }

        [Theory]
        [CombinatorialData]
        public void ListPattern_MissingMembers(bool isIndexable, bool isSliceable, bool isCountable)
        {
            var random = new Random();
            (bool, bool) split(bool supported)
            {
                return !supported ? (false, false) : random.Next(3) switch
                {
                    0 => (false, true),
                    1 => (true, false),
                    2 => (true, true),
                    _ => throw new("unreachable"),
                };
            }

            var (implicitIndex, explicitIndex) = split(isIndexable);
            var (implicitRange, explicitRange) = split(isSliceable);
            var (hasLengthProp, hasCountProp) = split(isCountable);

            var source = @$"
class X
{{
    {(implicitIndex ? "public int this[int i] => 1;" : null)}
    {(explicitIndex ? "public int this[System.Index i] => 1;" : null)}
    {(explicitRange ? "public int this[System.Range i] => 1;" : null)}
    {(implicitRange ? "public int Slice(int i, int j) => 1;" : null)}
    {(hasLengthProp ? "public int Length => 1;" : null)}
    {(hasCountProp ? "public int Count => 1;" : null)}

    public static void Main()
    {{
        _ = new X() is {{ .. 1 }};
    }}
}}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            switch (isIndexable, isSliceable, isCountable)
            {
                case (true, true, true):
                    compilation.VerifyEmitDiagnostics();
                    return;
                case (true, false, true):
                    compilation.VerifyEmitDiagnostics(
                        // (13,26): error CS9201: Slice patterns may not be used for a value of type 'X'.
                        //         _ = new X() is { .. 1 };
                        Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, ".. 1").WithArguments("X").WithLocation(13, 26));
                    return;
                case (false, true, true):
                case (false, true, false):
                case (true, true, false):
                    compilation.VerifyEmitDiagnostics(
                        // (13,24): error CS9200: List patterns may not be used for a value of type 'X'.
                        //         _ = new X() is { .. 1 };
                        Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "{ .. 1 }").WithArguments("X").WithLocation(13, 24));
                    return;
                case (true, false, false):
                case (false, false, true):
                case (false, false, false):
                    compilation.VerifyEmitDiagnostics(
                        // (13,24): error CS9200: List patterns may not be used for a value of type 'X'.
                        //         _ = new X() is { .. 1 };
                        Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "{ .. 1 }").WithArguments("X").WithLocation(13, 24),
                        // (13,26): error CS9201: Slice patterns may not be used for a value of type 'X'.
                        //         _ = new X() is { .. 1 };
                        Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, ".. 1").WithArguments("X").WithLocation(13, 26));
                    return;
                default:
                    throw new("unreachable");
            }
        }

        [Fact]
        public void ListPattern_ObsoleteMembers()
        {
            var source = @"
using System;
class Test1
{
    [Obsolete(""error"", error: true)]
    public int Slice(int i, int j) => 0;
    [Obsolete(""error"", error: true)]
    public int this[int i] => 0;
    [Obsolete(""error"", error: true)]
    public int Count => 0;
}
class Test2
{
    [Obsolete(""error"", error: true)]
    public int this[Index i] => 0;
    [Obsolete(""error"", error: true)]
    public int this[Range i] => 0;
    [Obsolete(""error"", error: true)]
    public int Length => 0;
}
class X
{
    public void M()
    {
        _ = new Test1() is {0};
        _ = new Test1() is [0];
        _ = new Test1() is [1]{0};
        _ = new Test1() is {..0};
        _ = new Test2() is {0};
        _ = new Test2() is [0];
        _ = new Test2() is [1]{0};
        _ = new Test2() is {..0};
    } 
}
";
            // PROTOTYPE(list-patterns): Missing errors for implicit support https://github.com/dotnet/roslyn/issues/53418
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
            compilation.VerifyEmitDiagnostics(
                // (29,28): error CS0619: 'Test2.this[Index]' is obsolete: 'error'
                //         _ = new Test2() is {0};
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "{0}").WithArguments("Test2.this[System.Index]", "error").WithLocation(29, 28),
                // (31,31): error CS0619: 'Test2.this[Index]' is obsolete: 'error'
                //         _ = new Test2() is [1]{0};
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "{0}").WithArguments("Test2.this[System.Index]", "error").WithLocation(31, 31),
                // (32,28): error CS0619: 'Test2.this[Index]' is obsolete: 'error'
                //         _ = new Test2() is {..0};
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "{..0}").WithArguments("Test2.this[System.Index]", "error").WithLocation(32, 28),
                // (32,29): error CS0619: 'Test2.this[Range]' is obsolete: 'error'
                //         _ = new Test2() is {..0};
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Test2.this[System.Range]", "error").WithLocation(32, 29));
        }

        [Fact]
        public void SlicePattern_Misplaced()
        {
            var source = @"
class X
{
    public void M(int[] a)
    {
        _ = a is {.., ..};
        _ = a is { 1, .., 2, .., 3 };
        _ = a is {(..)};
        _ = a is ..;
        _ = a is [..];
        _ = a switch { .. => 0, _ => 0 };
        switch (a) { case ..: break; }
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
            compilation.VerifyEmitDiagnostics(
                // (6,23): error CS9203: Slice patterns may only be used once and directly inside a list pattern.
                //         _ = a is {.., ..};
                Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(6, 23),
                // (7,30): error CS9203: Slice patterns may only be used once and directly inside a list pattern.
                //         _ = a is { 1, .., 2, .., 3 };
                Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(7, 30),
                // (8,20): error CS9203: Slice patterns may only be used once and directly inside a list pattern.
                //         _ = a is {(..)};
                Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(8, 20),
                // (8,20): error CS9201: Slice patterns may not be used for a value of type 'int'.
                //         _ = a is {(..)};
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..").WithArguments("int").WithLocation(8, 20),
                // (9,18): error CS9203: Slice patterns may only be used once and directly inside a list pattern.
                //         _ = a is ..;
                Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(9, 18),
                // (10,19): error CS9203: Slice patterns may only be used once and directly inside a list pattern.
                //         _ = a is [..];
                Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(10, 19),
                // (10,19): error CS9201: Slice patterns may not be used for a value of type 'int'.
                //         _ = a is [..];
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..").WithArguments("int").WithLocation(10, 19),
                // (11,24): error CS9203: Slice patterns may only be used once and directly inside a list pattern.
                //         _ = a switch { .. => 0, _ => 0 };
                Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(11, 24),
                // (12,27): error CS9203: Slice patterns may only be used once and directly inside a list pattern.
                //         switch (a) { case ..: break; }
                Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(12, 27));
        }

        [Fact]
        public void ListPattern_MemberLookup_StaticIndexer()
        {
            var vbSource = @"
Namespace System
    Public Structure Index
    End Structure
End Namespace
Public Class Test1
    Public Shared ReadOnly Property Item(i As System.Index) As Integer
        Get
            Return 0
        End Get
    End Property
    Public Property Length As Integer = 0
End Class
";
            var csSource = @"
class X
{
    public static void Main()
    {
        _ = new Test1() is {0};
    } 
}
";
            var vbCompilation = CreateVisualBasicCompilation(vbSource);
            var csCompilation = CreateCompilation(csSource, parseOptions: TestOptions.RegularWithListPatterns, references: new[] { vbCompilation.EmitToImageReference() });
            // PROTOTYPE(list-patterns) Unsupported because the lookup fails not that the indexer is static
            csCompilation.VerifyEmitDiagnostics(
                // (6,28): error CS9200: List patterns may not be used for a value of type 'Test1'.
                //         _ = new Test1() is {0};
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "{0}").WithArguments("Test1").WithLocation(6, 28));
        }

        [Theory]
        [InlineData("public int this[Index i] { set {} }")]
        [InlineData("public int this[int i, int ignored = 0] => 0;")]
        [InlineData("public int this[params int[] i] => 0;")]
        [InlineData("private int this[Index i] => 1;")]
        public void ListPattern_MemberLookup_ErrorCases(string indexer)
        {
            var source = @"
using System;
class Test1
{
    " + indexer + @"
    public int Length => 0;
}
class X
{
    public static void Main()
    {
        _ = new Test1() is {0};
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics(
                // (12,28): error CS9200: List patterns may not be used for a value of type 'Test1'.
                //         _ = new Test1() is {0};
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "{0}").WithArguments("Test1").WithLocation(12, 28));
        }

        [Fact]
        public void ListPattern_MemberLookup_OverridenIndexer()
        {
            var source = @"
using System;
class Test1
{
    public virtual int this[Index i] => 1;
    public int Count => 0;
}
class Test2 : Test1
{
}
class Test3 : Test2
{
    public override int this[Index i] => 1;
}
class X
{
    public static void Main()
    {
        _ = new Test1() is {0};
        _ = new Test2() is {0};
        _ = new Test3() is {0};
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
        }


        [Fact]
        public void ListPattern_RefReturns()
        {
            var source = @"
using System;
class Test1
{
    int value = 1;
    public ref int this[Index i] => ref value;
    public int Count => 1;
}
class X
{
    public static void Main()
    {
        Console.Write(new Test1() is {1});
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(compilation, expectedOutput: "True");
            verifier.VerifyIL("X.Main", @"
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (Test1 V_0)
  IL_0000:  newobj     ""Test1..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brfalse.s  IL_0025
  IL_0009:  ldloc.0
  IL_000a:  callvirt   ""int Test1.Count.get""
  IL_000f:  ldc.i4.1
  IL_0010:  bne.un.s   IL_0025
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.0
  IL_0014:  ldc.i4.0
  IL_0015:  newobj     ""System.Index..ctor(int, bool)""
  IL_001a:  callvirt   ""ref int Test1.this[System.Index].get""
  IL_001f:  ldind.i4
  IL_0020:  ldc.i4.1
  IL_0021:  ceq
  IL_0023:  br.s       IL_0026
  IL_0025:  ldc.i4.0
  IL_0026:  call       ""void System.Console.Write(bool)""
  IL_002b:  ret
}");
        }

        [Fact]
        public void SlicePattern_SliceValue()
        {
            var source = @"
using System;
class X
{
    public static void Main()
    {
        var arr = new[] { 1, 2, 3, 4 };
        if (arr is {.. var start, _})
            Console.Write(string.Join("""", start));
        if (arr is {_, .. var end})
            Console.Write(string.Join("""", end));
        if (arr is {_, .. var middle, _})
            Console.Write(string.Join("""", middle));
        if (arr is {.. var all})
            Console.Write(string.Join("""", all));
    } 
}
" + TestSources.GetSubArray;
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "123234231234");
        }

        [Fact]
        public void ListPattern_OrderOfEvaluation()
        {
            var source = @"
using System;
class X
{
    int this[int i]
    {
        get 
        {
            Console.Write(i);
            return i;
        }
    }
    int Count
    {
        get
        {
            Console.Write(-1);
            return 3;
        }
    }
    public static void Main()
    {
        Console.Write(new X() is { 0, 1, 2 } ? 1 : 0);
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "-10121");
        }

        [Fact]
        public void ListPattern_NarrowedTypes()
        {
            var source = @"
using System;
class X
{
    static int Test(object o)
    {
        return o switch
        {
            int[] {1,2,3} => 1,
            double[] and {1,2,3} => 2,
            float[] [3] => 3,
            _ => -1,
        };
    }
    public static void Main()
    {
        Console.Write(Test(new int[] { 1, 2, 3 }));
        Console.Write(Test(new double[] { 1, 2, 3 }));
        Console.Write(Test(new float[] { 1, 2, 3 }));
    } 
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "123");
        }

        [Fact]
        public void ListPattern_ImpossiblePattern()
        {
            var source = @"
using System;
class X
{
    public static void Main()
    {
        _ = new int[0] is [0]{1};
        _ = new int[0] is {Length:0} and {1};
        _ = new int[0] is [1]{}; // ok
    } 
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics(
                // (7,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
                //         _ = new int[0] is [0]{1};
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new int[0] is [0]{1}").WithArguments("int[]").WithLocation(7, 13),
                // (8,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
                //         _ = new int[0] is {Length:0} and {1};
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new int[0] is {Length:0} and {1}").WithArguments("int[]").WithLocation(8, 13));
        }

        [Fact]
        public void ListPattern_Symbols()
        {
            var source =
@"class X
{
    public static void Main()
    {
        _ = new int[0] is [var length];
        _ = new int[0] is {var element};
        _ = new int[0] is {..var slice};
    }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            foreach (var designation in tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>())
            {
                var model = compilation.GetSemanticModel(tree);
                var symbol = model.GetDeclaredSymbol(designation);
                Assert.Equal(SymbolKind.Local, symbol.Kind);
                Assert.Equal(symbol.Name == "slice" ? "int[]?" : "int", ((ILocalSymbol)symbol).Type.ToDisplayString());
            }
        }
    }
}
