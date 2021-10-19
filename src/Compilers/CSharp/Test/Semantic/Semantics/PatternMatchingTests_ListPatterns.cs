﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class PatternMatchingTests_ListPatterns : PatternMatchingTestBase
{
    [Fact]
    public void ListPattern()
    {
        static string testMethod(string type) =>
@"static bool Test(" + type + @" input)
{
    switch (input)
    {
        case []:
        case [_]:
          return true;
        case [var first, ..var others, var last] when first == last:
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
  // Code size       69 (0x45)
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
  IL_000d:  ldc.i4.1
  IL_000e:  ble.un.s   IL_0036
  IL_0010:  ldloca.s   V_3
  IL_0012:  ldc.i4.0
  IL_0013:  call       ""ref char System.Span<char>.this[int].get""
  IL_0018:  ldind.u2
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_3
  IL_001c:  ldc.i4.1
  IL_001d:  ldloc.s    V_4
  IL_001f:  ldc.i4.2
  IL_0020:  sub
  IL_0021:  call       ""System.Span<char> System.Span<char>.Slice(int, int)""
  IL_0026:  stloc.1
  IL_0027:  ldloca.s   V_3
  IL_0029:  ldloc.s    V_4
  IL_002b:  ldc.i4.1
  IL_002c:  sub
  IL_002d:  call       ""ref char System.Span<char>.this[int].get""
  IL_0032:  ldind.u2
  IL_0033:  stloc.2
  IL_0034:  br.s       IL_0038
  IL_0036:  ldc.i4.1
  IL_0037:  ret
  IL_0038:  ldloc.0
  IL_0039:  ldloc.2
  IL_003a:  bne.un.s   IL_0043
  IL_003c:  ldloc.1
  IL_003d:  call       ""bool X.Test(System.Span<char>)""
  IL_0042:  ret
  IL_0043:  ldc.i4.0
  IL_0044:  ret
}
"),
            () => verifier.VerifyIL("X.Test(char[])", @"
{
  // Code size       72 (0x48)
  .maxstack  4
  .locals init (char V_0, //first
                char[] V_1, //others
                char V_2, //last
                char[] V_3,
                int V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.3
  IL_0002:  ldloc.3
  IL_0003:  brfalse.s  IL_0046
  IL_0005:  ldloc.3
  IL_0006:  callvirt   ""int System.Array.Length.get""
  IL_000b:  stloc.s    V_4
  IL_000d:  ldloc.s    V_4
  IL_000f:  ldc.i4.1
  IL_0010:  ble.un.s   IL_0039
  IL_0012:  ldloc.3
  IL_0013:  ldc.i4.0
  IL_0014:  ldelem.u2
  IL_0015:  stloc.0
  IL_0016:  ldloc.3
  IL_0017:  ldc.i4.1
  IL_0018:  ldc.i4.0
  IL_0019:  newobj     ""System.Index..ctor(int, bool)""
  IL_001e:  ldc.i4.1
  IL_001f:  ldc.i4.1
  IL_0020:  newobj     ""System.Index..ctor(int, bool)""
  IL_0025:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_002a:  call       ""char[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<char>(char[], System.Range)""
  IL_002f:  stloc.1
  IL_0030:  ldloc.3
  IL_0031:  ldloc.s    V_4
  IL_0033:  ldc.i4.1
  IL_0034:  sub
  IL_0035:  ldelem.u2
  IL_0036:  stloc.2
  IL_0037:  br.s       IL_003b
  IL_0039:  ldc.i4.1
  IL_003a:  ret
  IL_003b:  ldloc.0
  IL_003c:  ldloc.2
  IL_003d:  bne.un.s   IL_0046
  IL_003f:  ldloc.1
  IL_0040:  call       ""bool X.Test(char[])""
  IL_0045:  ret
  IL_0046:  ldc.i4.0
  IL_0047:  ret
}
"),
            () => verifier.VerifyIL("X.Test(string)", @"
{
  // Code size       66 (0x42)
  .maxstack  4
  .locals init (char V_0, //first
                string V_1, //others
                char V_2, //last
                string V_3,
                int V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.3
  IL_0002:  ldloc.3
  IL_0003:  brfalse.s  IL_0040
  IL_0005:  ldloc.3
  IL_0006:  callvirt   ""int string.Length.get""
  IL_000b:  stloc.s    V_4
  IL_000d:  ldloc.s    V_4
  IL_000f:  ldc.i4.1
  IL_0010:  ble.un.s   IL_0033
  IL_0012:  ldloc.3
  IL_0013:  ldc.i4.0
  IL_0014:  callvirt   ""char string.this[int].get""
  IL_0019:  stloc.0
  IL_001a:  ldloc.3
  IL_001b:  ldc.i4.1
  IL_001c:  ldloc.s    V_4
  IL_001e:  ldc.i4.2
  IL_001f:  sub
  IL_0020:  callvirt   ""string string.Substring(int, int)""
  IL_0025:  stloc.1
  IL_0026:  ldloc.3
  IL_0027:  ldloc.s    V_4
  IL_0029:  ldc.i4.1
  IL_002a:  sub
  IL_002b:  callvirt   ""char string.this[int].get""
  IL_0030:  stloc.2
  IL_0031:  br.s       IL_0035
  IL_0033:  ldc.i4.1
  IL_0034:  ret
  IL_0035:  ldloc.0
  IL_0036:  ldloc.2
  IL_0037:  bne.un.s   IL_0040
  IL_0039:  ldloc.1
  IL_003a:  call       ""bool X.Test(string)""
  IL_003f:  ret
  IL_0040:  ldc.i4.0
  IL_0041:  ret
}
")
            );
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

    static bool Test1(Test1 t) => t is [1];
    static bool Test2(Test2 t) => t is [1];
    static bool Test3(Test3 t) => t is [1];
    static bool Test4(Test4 t) => t is [1];
    static bool Test5(Test5 t) => t is [1];

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

    static bool Test1(Test1 t) => t is [.. 1];
    static bool Test2(Test2 t) => t is [.. 1];
    static bool Test3(Test3 t) => t is [.. 1];
    static bool Test4(Test4 t) => t is [.. 1];
    static bool Test5(Test5 t) => t is [.. 1];

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
  // Code size       41 (0x29)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0027
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test1.Count.get""
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldc.i4.0
  IL_000c:  ldc.i4.0
  IL_000d:  newobj     ""System.Index..ctor(int, bool)""
  IL_0012:  ldc.i4.0
  IL_0013:  ldc.i4.1
  IL_0014:  newobj     ""System.Index..ctor(int, bool)""
  IL_0019:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_001e:  callvirt   ""int Test1.this[System.Range].get""
  IL_0023:  ldc.i4.1
  IL_0024:  ceq
  IL_0026:  ret
  IL_0027:  ldc.i4.0
  IL_0028:  ret
}"),
            () => verifier.VerifyIL("X.Test2", @"
{
  // Code size       42 (0x2a)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0028
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test2.Count.get""
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldc.i4.0
  IL_000c:  ldc.i4.0
  IL_000d:  newobj     ""System.Index..ctor(int, bool)""
  IL_0012:  ldc.i4.0
  IL_0013:  ldc.i4.1
  IL_0014:  newobj     ""System.Index..ctor(int, bool)""
  IL_0019:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_001e:  ldc.i4.5
  IL_001f:  callvirt   ""int Test2.this[System.Range, int].get""
  IL_0024:  ldc.i4.1
  IL_0025:  ceq
  IL_0027:  ret
  IL_0028:  ldc.i4.0
  IL_0029:  ret
}"),
            () => verifier.VerifyIL("X.Test3", @"
{
  // Code size       46 (0x2e)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_002c
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test3.Count.get""
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldc.i4.0
  IL_000c:  ldc.i4.0
  IL_000d:  newobj     ""System.Index..ctor(int, bool)""
  IL_0012:  ldc.i4.0
  IL_0013:  ldc.i4.1
  IL_0014:  newobj     ""System.Index..ctor(int, bool)""
  IL_0019:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_001e:  call       ""int[] System.Array.Empty<int>()""
  IL_0023:  callvirt   ""int Test3.this[System.Range, params int[]].get""
  IL_0028:  ldc.i4.1
  IL_0029:  ceq
  IL_002b:  ret
  IL_002c:  ldc.i4.0
  IL_002d:  ret
}"),
            () => verifier.VerifyIL("X.Test4", @"
{
  // Code size       54 (0x36)
  .maxstack  7
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0034
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test4.Count.get""
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldc.i4.1
  IL_000c:  newarr     ""System.Range""
  IL_0011:  dup
  IL_0012:  ldc.i4.0
  IL_0013:  ldc.i4.0
  IL_0014:  ldc.i4.0
  IL_0015:  newobj     ""System.Index..ctor(int, bool)""
  IL_001a:  ldc.i4.0
  IL_001b:  ldc.i4.1
  IL_001c:  newobj     ""System.Index..ctor(int, bool)""
  IL_0021:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0026:  stelem     ""System.Range""
  IL_002b:  callvirt   ""int Test4.this[params System.Range[]].get""
  IL_0030:  ldc.i4.1
  IL_0031:  ceq
  IL_0033:  ret
  IL_0034:  ldc.i4.0
  IL_0035:  ret
}"),
            () => verifier.VerifyIL("X.Test5", @"
{
  // Code size       26 (0x1a)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0018
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test5.Count.get""
  IL_0009:  stloc.0
  IL_000a:  ldarg.0
  IL_000b:  ldc.i4.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  sub
  IL_000f:  callvirt   ""int Test5.Slice(int, int)""
  IL_0014:  ldc.i4.1
  IL_0015:  ceq
  IL_0017:  ret
  IL_0018:  ldc.i4.0
  IL_0019:  ret
}")
        );
    }

    [Fact]
    public void ListPattern_CallerInfo()
    {
        var source = @"
using System;
using System.Runtime.CompilerServices;

class Test1
{
    public string this[Index i, [CallerMemberName] string member = null] => member;
    public int Count => 1;
}
class Test2
{
    public string this[Range i, [CallerMemberName] string member = null] => member;
    public int this[int i] => throw new();
    public int Count => 1;
}
class Test3
{
    public int this[Index i, [CallerLineNumber] int line = 0] => line;
    public int Count => 1;
}
class Test4
{
    public int this[Range i, [CallerLineNumber] int line = 0] => line;
    public int this[int i] => throw new();
    public int Count => 1;
}
class X
{
    static bool Test1(Test1 t) => t is [nameof(Test1)];
    static bool Test2(Test2 t) => t is [.. nameof(Test2)];
    #line 42
    static bool Test3(Test3 t) => t is [42];
    static bool Test4(Test4 t) => t is [.. 43];

    public static void Main()
    {
        Console.WriteLine(Test1(new()));
        Console.WriteLine(Test2(new()));
        Console.WriteLine(Test3(new()));
        Console.WriteLine(Test4(new()));
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
";
        CompileAndVerify(compilation, expectedOutput: expectedOutput);
    }

    [Theory]
    [CombinatorialData]
    public void ListPattern_UnsupportedTypes([CombinatorialValues("0", "..", ".._")] string subpattern)
    {
        var listPattern = $"[{subpattern}]";
        var source = @"
class X
{
    void M(object o)
    {
        _ = o is " + listPattern + @";
    }
}
";
        var expectedDiagnostics = new[]
        {
                // error CS9200: List patterns may not be used for a value of type 'object'.
                subpattern != "" ? Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, listPattern).WithArguments("object") : null,
                // error CS9201: Slice patterns may not be used for a value of type 'object'.
                subpattern == ".._" ? Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, subpattern).WithArguments("object") : null
            };
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(expectedDiagnostics.WhereNotNull().ToArray());
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
        _ = new X() is [1];
        _ = new X() is [.. 1];
    } 
}
";
        var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        compilation.VerifyEmitDiagnostics(
            // (20,24): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = new X() is [1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[1]").WithArguments("System.Index", ".ctor").WithLocation(20, 24),
            // (21,24): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = new X() is [.. 1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[.. 1]").WithArguments("System.Index", ".ctor").WithLocation(21, 24),
            // (21,25): error CS0656: Missing compiler required member 'System.Range..ctor'
            //         _ = new X() is [.. 1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".. 1").WithArguments("System.Range", ".ctor").WithLocation(21, 25)
            );
    }

    [Fact]
    public void ListPattern_MissingMembers_ArrayLength()
    {
        var source = @"
class X
{
    public void M(int[] a)
    {
        _ = a is [0];
        _ = a is [.._];
        _ = a[^1];
        _ = a[..];
    } 
}
" + TestSources.GetSubArray;
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.MakeMemberMissing(SpecialMember.System_Array__Length);
        compilation.VerifyEmitDiagnostics(
            // (6,18): error CS0656: Missing compiler required member 'System.Array.Length'
            //         _ = a is [0];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[0]").WithArguments("System.Array", "Length").WithLocation(6, 18),
            // (7,18): error CS0656: Missing compiler required member 'System.Array.Length'
            //         _ = a is [.._];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[.._]").WithArguments("System.Array", "Length").WithLocation(7, 18));
    }

    [Fact]
    public void ListPattern_MissingMembers_Substring()
    {
        var source = @"
class X
{
    public void M(string s)
    {
        _ = s is [.. var slice];
        _ = s[..];
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.MakeMemberMissing(SpecialMember.System_String__Substring);
        compilation.VerifyEmitDiagnostics(
            // (6,19): error CS9201: Slice patterns may not be used for a value of type 'string'.
            //         _ = s is {.. var slice};
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, ".. var slice").WithArguments("string").WithLocation(6, 19),
            // (7,15): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            //         _ = s[..];
            Diagnostic(ErrorCode.ERR_BadArgType, "..").WithArguments("1", "System.Range", "int").WithLocation(7, 15)
            );
    }

    [Fact]
    public void ListPattern_MissingMembers_GetSubArray_01()
    {
        var source = @"
class X
{
    public void M(int[] a)
    {
        _ = a is [.. var slice];
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (6,22): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray'
            //         _ = a is [.. var slice];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "var slice").WithArguments("System.Runtime.CompilerServices.RuntimeHelpers", "GetSubArray").WithLocation(6, 22)
            );
    }

    [Fact]
    public void ListPattern_MissingMembers_GetSubArray_02()
    {
        var source = @"
class X
{
    public void M(int[] a)
    {
        _ = a is [.. var slice];
        _ = a[..];
    } 
}
";
        // PROTOTYPE(list-patterns) Missing diagnostic on `.. var slice`; (this is strange as the test above works)
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (7,15): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray'
            //         _ = a[..];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "..").WithArguments("System.Runtime.CompilerServices.RuntimeHelpers", "GetSubArray").WithLocation(7, 15));
    }

    [Theory]
    [PairwiseData]
    public void ListPattern_MissingMembers(
        bool implicitIndex, bool explicitIndex,
        bool implicitRange, bool explicitRange,
        bool hasLengthProp, bool hasCountProp)
    {
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
        _ = new X() is [.._];
    }}
}}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);

        var isCountable = hasLengthProp || hasCountProp;
        var isSliceable = implicitRange || explicitRange;
        var isIndexable = implicitIndex || explicitIndex;
        var expectedDiagnostics = new[]
        {
                 // (13,24): error CS9200: List patterns may not be used for a value of type 'X'.
                //         _ = new X() is [.._];
                !isIndexable || !isCountable ? Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[.._]").WithArguments("X").WithLocation(13, 24) : null,
                // (13,25): error CS9201: Slice patterns may not be used for a value of type 'X'.
                //         _ = new X() is [.._];
                !isSliceable || !isCountable ? Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, ".._").WithArguments("X").WithLocation(13, 25) : null
            };
        compilation.VerifyEmitDiagnostics(expectedDiagnostics.WhereNotNull().ToArray());
    }

    [Fact]
    public void ListPattern_ObsoleteMembers()
    {
        var source = @"
using System;
class Test1
{
    [Obsolete(""error1"", error: true)]
    public int Slice(int i, int j) => 0;
    [Obsolete(""error2"", error: true)]
    public int this[int i] => 0;
    [Obsolete(""error3"", error: true)]
    public int Count => 0;
}
class Test2
{
    [Obsolete(""error4"", error: true)]
    public int this[Index i] => 0;
    [Obsolete(""error5"", error: true)]
    public int this[Range i] => 0;
    [Obsolete(""error6"", error: true)]
    public int Length => 0;
}
class X
{
    public void M()
    {
        _ = new Test1() is [0];
        _ = new Test1() is [..0];
        _ = new Test2() is [0];
        _ = new Test2() is [..0];
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (25,28): error CS0619: 'Test1.this[int]' is obsolete: 'error2'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test1.this[int]", "error2").WithLocation(25, 28),
            // (25,28): error CS0619: 'Test1.Count' is obsolete: 'error3'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test1.Count", "error3").WithLocation(25, 28),
            // (26,28): error CS0619: 'Test1.this[int]' is obsolete: 'error2'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test1.this[int]", "error2").WithLocation(26, 28),
            // (26,28): error CS0619: 'Test1.Count' is obsolete: 'error3'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test1.Count", "error3").WithLocation(26, 28),
            // (26,29): error CS0619: 'Test1.Slice(int, int)' is obsolete: 'error1'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Test1.Slice(int, int)", "error1").WithLocation(26, 29),
            // (26,29): error CS0619: 'Test1.Count' is obsolete: 'error3'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Test1.Count", "error3").WithLocation(26, 29),
            // (27,28): error CS0619: 'Test2.Length' is obsolete: 'error6'
            //         _ = new Test2() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test2.Length", "error6").WithLocation(27, 28),
            // (27,28): error CS0619: 'Test2.this[Index]' is obsolete: 'error4'
            //         _ = new Test2() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test2.this[System.Index]", "error4").WithLocation(27, 28),
            // (28,28): error CS0619: 'Test2.Length' is obsolete: 'error6'
            //         _ = new Test2() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test2.Length", "error6").WithLocation(28, 28),
            // (28,28): error CS0619: 'Test2.this[Index]' is obsolete: 'error4'
            //         _ = new Test2() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test2.this[System.Index]", "error4").WithLocation(28, 28),
            // (28,29): error CS0619: 'Test2.Length' is obsolete: 'error6'
            //         _ = new Test2() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Test2.Length", "error6").WithLocation(28, 29),
            // (28,29): error CS0619: 'Test2.this[Range]' is obsolete: 'error5'
            //         _ = new Test2() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Test2.this[System.Range]", "error5").WithLocation(28, 29)
            );
    }

    [Fact]
    public void SlicePattern_Misplaced()
    {
        var source = @"
class X
{
    public void M(int[] a)
    {
        _ = a is [.., ..];
        _ = a is [1, .., 2, .., 3];
        _ = a is [(..)];
        _ = a is ..;
        _ = a is [..];
        _ = a switch { .. => 0, _ => 0 };
        switch (a) { case ..: break; }
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (6,23): error CS9202: Slice patterns may only be used once and directly inside a list pattern.
            //         _ = a is [.., ..];
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(6, 23),
            // (7,29): error CS9202: Slice patterns may only be used once and directly inside a list pattern.
            //         _ = a is [1, .., 2, .., 3];
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(7, 29),
            // (8,20): error CS9202: Slice patterns may only be used once and directly inside a list pattern.
            //         _ = a is [(..)];
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(8, 20),
            // (9,18): error CS9202: Slice patterns may only be used once and directly inside a list pattern.
            //         _ = a is ..;
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(9, 18),
            // (11,24): error CS9202: Slice patterns may only be used once and directly inside a list pattern.
            //         _ = a switch { .. => 0, _ => 0 };
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(11, 24),
            // (12,27): error CS9202: Slice patterns may only be used once and directly inside a list pattern.
            //         switch (a) { case ..: break; }
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(12, 27)
            );
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
        _ = new Test1() is [0];
    } 
}
";
        var vbCompilation = CreateVisualBasicCompilation(vbSource);
        var csCompilation = CreateCompilation(csSource, parseOptions: TestOptions.RegularWithListPatterns, references: new[] { vbCompilation.EmitToImageReference() });
        // PROTOTYPE(list-patterns) Unsupported because the lookup fails not that the indexer is static
        csCompilation.VerifyEmitDiagnostics(
            // (6,28): error CS9200: List patterns may not be used for a value of type 'Test1'.
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[0]").WithArguments("Test1").WithLocation(6, 28));
    }

    [Theory]
    [InlineData("public int this[Index i] { set {} }")]
    [InlineData("public int this[Index i] { private get => 0; set {} }")]
    [InlineData("public int this[int i, int ignored = 0] => 0;")]
    [InlineData("public int this[long i, int ignored = 0] => 0;")]
    [InlineData("public int this[long i] => 0;")]
    [InlineData("public int this[params int[] i] => 0;")]
    [InlineData("private int this[Index i] => 0;")]
    [InlineData("public int this[Index i] => 0;", true)]
    public void ListPattern_MemberLookup_Index_ErrorCases(string indexer, bool valid = false)
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
        _ = new Test1() is [0];
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        if (valid)
        {
            compilation.VerifyEmitDiagnostics();
            return;
        }
        compilation.VerifyEmitDiagnostics(
            // (12,28): error CS9200: List patterns may not be used for a value of type 'Test1'.
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[0]").WithArguments("Test1").WithLocation(12, 28));
    }

    [Theory]
    [InlineData("public int this[Range i] { set {} }")]
    [InlineData("public int this[Range i] { private get => 0; set {} }")]
    [InlineData("public int Slice(int i, int j, int ignored = 0) => 0;")]
    [InlineData("public int Slice(int i, int j, params int[] ignored) => 0;")]
    [InlineData("public int Slice(long i, long j) => 0;")]
    [InlineData("public int Slice(params int[] i) => 0;")]
    [InlineData("private int Slice(int i, int j) => 0;")]
    [InlineData("public void Slice(int i, int j) {}")]
    [InlineData("public int this[Range i] => 0;", true)]
    [InlineData("public int Slice(int i, int j) => 0;", true)]
    public void ListPattern_MemberLookup_Range_ErrorCases(string member, bool valid = false)
    {
        var source = @"
#pragma warning disable 8019 // Unused using
using System;
class Test1
{
    " + member + @"
    public int this[int i] => throw new();
    public int Length => 0;
}
class X
{
    public static void Main()
    {
        _ = new Test1() is [..var p];
        _ = new Test1() is [..];
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        if (valid)
        {
            compilation.VerifyDiagnostics();
            return;
        }
        compilation.VerifyEmitDiagnostics(
            // (14,29): error CS9201: Slice patterns may not be used for a value of type 'Test1'.
            //         _ = new Test1() is {..var p};
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..var p").WithArguments("Test1").WithLocation(14, 29));
    }

    [Fact]
    public void ListPattern_MemberLookup_OverridenIndexer()
    {
        var source = @"
using System;
class Test1
{
    public virtual int this[Index i] => 1;
    public virtual int Count => 1;
}
class Test2 : Test1
{
}
class Test3 : Test2
{
    public override int this[Index i] => 2;
    public override int Count => 2;
}
class X
{
    public static void Main()
    {
        Console.WriteLine(new Test1() is [1]);
        Console.WriteLine(new Test2() is [1]);
        Console.WriteLine(new Test3() is [1]);
        Console.WriteLine(new Test1() is [2, 2]);
        Console.WriteLine(new Test2() is [2, 2]);
        Console.WriteLine(new Test3() is [2, 2]);
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        compilation.VerifyEmitDiagnostics();
        string expectedOutput = @"
True
True
False
False
False
True";
        CompileAndVerify(compilation, expectedOutput: expectedOutput);
    }

    [Fact]
    public void ListPattern_MemberLookup_Fallback_InaccessibleIndexer()
    {
        var source = @"
using System;
class Test1
{
    private int this[Index i] => throw new();
    private int this[Range i] => throw new();
    private int Length => throw new();
    public int this[int i] => 1;
    public int Slice(int i, int j) => 2;
    public int Count => 1;
}
class X
{
    public static void Main()
    {
        Console.WriteLine(new Test1() is [1, ..2]);
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        compilation.VerifyEmitDiagnostics();
        CompileAndVerify(compilation, expectedOutput: "True");
    }

    [Fact]
    public void ListPattern_MemberLookup_Fallback_MissingIndexOrRange()
    {
        var source = @"
using System;
class Test1
{
    public int this[int i] => 1;
    public int Slice(int i, int j) => 2;
    public int Count => 1;
}
class X
{
    public static void Main()
    {
        Console.WriteLine(new Test1() is [1, ..2]);
    } 
}
";
        var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        Assert.Null(compilation.GetTypeByMetadataName("System.Index"));
        Assert.Null(compilation.GetTypeByMetadataName("System.Range"));
        compilation.VerifyEmitDiagnostics();
        CompileAndVerify(compilation, expectedOutput: "True");
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
    public ref int this[Range i] => ref value;
    public int Count => 1;
}
class X
{
    public static void Main()
    {
        Console.WriteLine(new Test1() is [1] and [..1]);
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        compilation.VerifyEmitDiagnostics();
        var verifier = CompileAndVerify(compilation, expectedOutput: "True");
        verifier.VerifyIL("X.Main", @"
{
  // Code size       73 (0x49)
  .maxstack  4
  .locals init (Test1 V_0)
  IL_0000:  newobj     ""Test1..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brfalse.s  IL_0042
  IL_0009:  ldloc.0
  IL_000a:  callvirt   ""int Test1.Count.get""
  IL_000f:  ldc.i4.1
  IL_0010:  bne.un.s   IL_0042
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.0
  IL_0014:  ldc.i4.0
  IL_0015:  newobj     ""System.Index..ctor(int, bool)""
  IL_001a:  callvirt   ""ref int Test1.this[System.Index].get""
  IL_001f:  ldind.i4
  IL_0020:  ldc.i4.1
  IL_0021:  bne.un.s   IL_0042
  IL_0023:  ldloc.0
  IL_0024:  ldc.i4.0
  IL_0025:  ldc.i4.0
  IL_0026:  newobj     ""System.Index..ctor(int, bool)""
  IL_002b:  ldc.i4.0
  IL_002c:  ldc.i4.1
  IL_002d:  newobj     ""System.Index..ctor(int, bool)""
  IL_0032:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0037:  callvirt   ""ref int Test1.this[System.Range].get""
  IL_003c:  ldind.i4
  IL_003d:  ldc.i4.1
  IL_003e:  ceq
  IL_0040:  br.s       IL_0043
  IL_0042:  ldc.i4.0
  IL_0043:  call       ""void System.Console.WriteLine(bool)""
  IL_0048:  ret
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
        if (arr is [.. var start, _])
            Console.WriteLine(string.Join("", "", start));
        if (arr is [_, .. var end])
            Console.WriteLine(string.Join("", "", end));
        if (arr is [_, .. var middle, _])
            Console.WriteLine(string.Join("", "", middle));
        if (arr is [.. var all])
            Console.WriteLine(string.Join("", "", all));
    } 
}
" + TestSources.GetSubArray;
        var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        compilation.VerifyEmitDiagnostics();
        string expectedOutput = @"
1, 2, 3
2, 3, 4
2, 3
1, 2, 3, 4";
        CompileAndVerify(compilation, expectedOutput: expectedOutput);
    }

    [Fact]
    public void SlicePattern_Subpattern()
    {
        var source = @"
using System;
class C
{
    private int length;
    public C(int length) => this.length = length;
    public int this[int i] => 1;
    public int Slice(int i, int j) => throw new();
    public int Length { get { Console.WriteLine(nameof(Length)); return length; } }
}
class X
{
    public static bool Test1(C c) => c is [..];
    public static bool Test2(C c) => c is [.._];
    public static void Main()
    {
        Console.WriteLine(Test1(null));
        Console.WriteLine(Test2(null));
        Console.WriteLine(Test1(new(-1)));
        Console.WriteLine(Test1(new(0)));
        Console.WriteLine(Test1(new(1)));
        Console.WriteLine(Test2(new(-1)));
        Console.WriteLine(Test2(new(0)));
        Console.WriteLine(Test2(new(1)));
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        compilation.VerifyEmitDiagnostics();
        string expectedOutput = @"
False
False
True
True
True
Length
True
Length
True
Length
True
";
        var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        AssertEx.Multiple(
            () => verifier.VerifyIL("X.Test1", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  cgt.un
  IL_0004:  ret
}"),
            () => verifier.VerifyIL("X.Test2", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_000c
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int C.Length.get""
  IL_0009:  pop
  IL_000a:  ldc.i4.1
  IL_000b:  ret
  IL_000c:  ldc.i4.0
  IL_000d:  ret
}")
        );
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
        Console.Write(new X() is [0, 1, 2] ? 1 : 0);
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        compilation.VerifyEmitDiagnostics();
        CompileAndVerify(compilation, expectedOutput: "-10121");
    }

    [Theory]
    [InlineData(
        "{ null, null, new(0, 0) }",
        "[..{ Length: >=2 }, { X: 0, Y: 0 }]",
        "e.Length, e[0..^1], e[0..^1].Length, e[^1], e[^1].X, e[^1].Y, True")]
    [InlineData(
        "{ null, null, new(0, 0) }",
        "[.., { X: 0, Y: 0 }]",
        "e.Length, e[^1], e[^1].X, e[^1].Y, True")]
    [InlineData(
        "{ new(0, 5) }",
        "[.., { X:0, Y:0 }] or [{ X:0, Y:5 }]",
        "e.Length, e[^1], e[^1].X, e[^1].Y, e[0], e[0].X, e[0].Y, True")]
    [InlineData(
        "{ new(0, 1), new(0, 5) }",
        "[.., { X:0, Y:0 }] or [{ X:0, Y:5 }]",
        "e.Length, e[^1], e[^1].X, e[^1].Y, False")]
    [InlineData(
        "{ null, new(0, 5) }",
        "[.., { X:0, Y:0 }] or [{ X:0, Y:5 }]",
        "e.Length, e[^1], e[^1].X, e[^1].Y, False")]
    public void SlicePattern_OrderOfEvaluation(string array, string pattern, string expectedOutput)
    {
        var source = @"
using static System.Console;

Write(new MyArray(new MyPoint[] " + array + @") is " + pattern + @");

class MyPoint
{
    public Point point;
    public string source;

    public MyPoint(int x, int y) : this(new(x, y)) { }
    public MyPoint(Point point, string source = ""e"") => (this.point, this.source) = (point, source);
    public int X { get { Write($""{source}.{nameof(X)}, ""); return point.X; } }
    public int Y { get { Write($""{source}.{nameof(Y)}, ""); return point.Y; } }
}
class MyArray
{
    public MyPoint[] array;
    public string source;

    public MyArray(MyPoint[] array, string source = ""e"") => (this.array, this.source) = (array, source);
    public MyPoint this[System.Index index] { get { Write($""{source}[{index}], ""); return new(array[index].point, $""{source}[{index}]""); } }
    public MyArray this[System.Range range] { get { Write($""{source}[{range}], ""); return new(array[range], $""{source}[{range}]""); } }
    public int Length { get { Write($""{source}.{nameof(Length)}, ""); return array.Length; } }
}
struct Point
{
    public int X, Y;
    public Point(int x, int y) => (X, Y) = (x, y);
}
" + TestSources.GetSubArray;
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        CompileAndVerify(compilation, expectedOutput: expectedOutput).VerifyDiagnostics();
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
            int[] and [1,2,3] => 1,
            double[] and [1,2,3] => 2,
            float[] and [_,_,_] => 3,
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
    public void ImpossiblePattern_01()
    {
        var source = @"
using System;
class X
{
    public void M(int[] a, int[,] mdarray)
    {
        _ = a is [] and [1];          // 1
        _ = a is [1] and [1];
        _ = a is {Length:0} and [1];  // 2
        _ = a is {Length:1} and [1];
        _ = a is [1,2,3] and [1,2,4]; // 3
        _ = a is [1,2,3] and [1,2,3];
        _ = a is ([>0]) and ([<0]);   // 4
        _ = a is ([>0]) and ([>=0]);
        _ = a is [>0] and [<0];       // 5
        _ = a is [>0] and [>=0];
        _ = a is {Length:-1};         // 6
        _ = a is [.., >0] and [<0];   // 7
        _ = a is [.., >0, _] and [_, <=0, ..];
        _ = new { a } is { a.Length:-1 }; // 8
        _ = a is [..{ Length: -1 }];  // 9
        _ = a is [..{ Length: < -1 }];  // 10
        _ = a is [..{ Length: <= -1 }]; // 11
        _ = a is [..{ Length: >= -1 }];
        _ = a is [..{ Length: > -1 }];
        _ = a is [_, _, ..{ Length: int.MaxValue - 1 }]; // 12
        _ = a is [_, _, ..{ Length: <= int.MaxValue - 1 }];
        _ = a is [_, _, ..{ Length: >= int.MaxValue - 1 }]; // 13
        _ = a is [_, _, ..{ Length: < int.MaxValue - 1 }];
        _ = a is [_, _, ..{ Length: > int.MaxValue - 1 }]; // 14
        _ = a is { LongLength: -1 };
        _ = (Array)a is { Length: -1 };
        _ = mdarray is { Length: -1 };
    } 
}
";
        var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (7,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [] and [1];          // 1
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [] and [1]").WithArguments("int[]").WithLocation(7, 13),
            // (9,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is {Length:0} and [1];  // 2
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is {Length:0} and [1]").WithArguments("int[]").WithLocation(9, 13),
            // (11,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [1,2,3] and [1,2,4]; // 3
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [1,2,3] and [1,2,4]").WithArguments("int[]").WithLocation(11, 13),
            // (13,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is ([>0]) and ([<0]);   // 4
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is ([>0]) and ([<0])").WithArguments("int[]").WithLocation(13, 13),
            // (15,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [>0] and [<0];       // 5
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [>0] and [<0]").WithArguments("int[]").WithLocation(15, 13),
            // (17,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is {Length:-1};         // 6
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is {Length:-1}").WithArguments("int[]").WithLocation(17, 13),
            // (18,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [.., >0] and [<0];   // 7
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [.., >0] and [<0]").WithArguments("int[]").WithLocation(18, 13),
            //         _ = new { a } is { a.Length:-1 }; // 8
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new { a } is { a.Length:-1 }").WithArguments("<anonymous type: int[] a>").WithLocation(20, 13),
            // (21,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [..{ Length: -1 }];  // 9
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [..{ Length: -1 }]").WithArguments("int[]").WithLocation(21, 13),
            // (22,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [..{ Length: < -1 }];  // 10
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [..{ Length: < -1 }]").WithArguments("int[]").WithLocation(22, 13),
            // (23,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [..{ Length: <= -1 }]; // 11
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [..{ Length: <= -1 }]").WithArguments("int[]").WithLocation(23, 13),
            // (26,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [_, _, ..{ Length: int.MaxValue - 1 }]; // 12
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [_, _, ..{ Length: int.MaxValue - 1 }]").WithArguments("int[]").WithLocation(26, 13),
            // (28,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [_, _, ..{ Length: >= int.MaxValue - 1 }]; // 13
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [_, _, ..{ Length: >= int.MaxValue - 1 }]").WithArguments("int[]").WithLocation(28, 13),
            // (30,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [_, _, ..{ Length: > int.MaxValue - 1 }]; // 14
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [_, _, ..{ Length: > int.MaxValue - 1 }]").WithArguments("int[]").WithLocation(30, 13)
        );
    }

    [Fact]
    public void ImpossiblePattern_02()
    {
        var source = @"
interface IIndexable
{
    int this[int i] { get; }
}
interface ICountableViaCount
{
    int Count { get; }
}
interface ICountableViaLength
{
    int Length { get; }
}
class X
{
    public static void Test1<T>(T t) where T : ICountableViaCount
    {
        _ = t is { Count: -1 };
        _ = new { t } is { t.Count: -1 };
    }
    public static void Test2<T>(T t) where T : ICountableViaLength
    {
        _ = t is { Length: -1 };
        _ = new { t } is { t.Length: -1 };
    }
    public static void Test3<T>(T t) where T : IIndexable, ICountableViaCount
    {
        _ = t is { Count: -1 }; // 1
        _ = new { t } is { t.Count: -1 }; // 2
    }
    public static void Test4<T>(T t) where T : IIndexable, ICountableViaLength
    {
        _ = t is { Length: -1 }; // 3
        _ = new { t } is { t.Length: -1 }; // 4
    }
    public static void Test5<T>(T t) where T : IIndexable, ICountableViaLength, ICountableViaCount
    {
        _ = t is { Length: -1 }; // 5
        _ = t is { Count: -1 }; // 6
        _ = new { t } is { t.Count: -1 }; // 7
        _ = new { t } is { t.Length: -1 }; // 8
    }
}
";
        var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (28,13): error CS8518: An expression of type 'T' can never match the provided pattern.
            //         _ = t is { Count: -1 }; // 1
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "t is { Count: -1 }").WithArguments("T").WithLocation(28, 13),
            // (29,13): error CS8518: An expression of type '<anonymous type: T t>' can never match the provided pattern.
            //         _ = new { t } is { t.Count: -1 }; // 2
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new { t } is { t.Count: -1 }").WithArguments("<anonymous type: T t>").WithLocation(29, 13),
            // (33,13): error CS8518: An expression of type 'T' can never match the provided pattern.
            //         _ = t is { Length: -1 }; // 3
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "t is { Length: -1 }").WithArguments("T").WithLocation(33, 13),
            // (34,13): error CS8518: An expression of type '<anonymous type: T t>' can never match the provided pattern.
            //         _ = new { t } is { t.Length: -1 }; // 4
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new { t } is { t.Length: -1 }").WithArguments("<anonymous type: T t>").WithLocation(34, 13),
            // (38,13): error CS8518: An expression of type 'T' can never match the provided pattern.
            //         _ = t is { Length: -1 }; // 5
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "t is { Length: -1 }").WithArguments("T").WithLocation(38, 13),
            // (39,13): error CS8518: An expression of type 'T' can never match the provided pattern.
            //         _ = t is { Count: -1 }; // 6
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "t is { Count: -1 }").WithArguments("T").WithLocation(39, 13),
            // (40,13): error CS8518: An expression of type '<anonymous type: T t>' can never match the provided pattern.
            //         _ = new { t } is { t.Count: -1 }; // 7
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new { t } is { t.Count: -1 }").WithArguments("<anonymous type: T t>").WithLocation(40, 13),
            // (41,13): error CS8518: An expression of type '<anonymous type: T t>' can never match the provided pattern.
            //         _ = new { t } is { t.Length: -1 }; // 8
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new { t } is { t.Length: -1 }").WithArguments("<anonymous type: T t>").WithLocation(41, 13));
    }

    [Fact]
    public void BadConstant()
    {
        var source = @"
using System;
class X
{
    public void M(int[] a)
    {
        const int bad = a;
        _ = a is { Length: bad };
        _ = new { a } is { a.Length: bad };
        _ = a is [..{ Length: bad }];
        _ = a is [..{ Length: < bad }];
        _ = a is [..{ Length: <= bad }];
        _ = a is [..{ Length: >= bad }];
        _ = a is [..{ Length: > bad }];
    } 
}
";
        var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (7,25): error CS0029: Cannot implicitly convert type 'int[]' to 'int'
            //         const int bad = a;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "a").WithArguments("int[]", "int").WithLocation(7, 25));
    }

    [Fact]
    public void ListPattern_Interface()
    {
        var source = @"
D.M(new C());

interface I
{
    int Length { get; }
    int this[int i] { get; }
    string Slice(int i, int j);
}
class C : I
{
    public int Length => 1;
    public int this[int i] => 42;
    public string Slice(int i, int j) => ""slice"";
}
class D
{
    public static void M<T>(T t) where T : I
    {
        if (t is not [var item, ..var rest]) return;
        System.Console.WriteLine(item);
        System.Console.WriteLine(rest);
    }
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics();
        string expectedOutput = @"
42
slice
";
        CompileAndVerify(compilation, expectedOutput: expectedOutput);
    }

    [Fact]
    public void ListPattern_Nullable()
    {
        var source = @"
using System;
struct S
{
    public int Length => 1;
    public int this[int i] => 42;

    public static bool Test(S? s)
    {
        return s is [42];
    }

    public static void Main()
    {
        Console.WriteLine(Test(new S()));
        Console.WriteLine(Test(null));
    }
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        compilation.VerifyEmitDiagnostics();
        string expectedOutput = @"
True
False
";
        var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        verifier.VerifyIL("S.Test", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""bool S?.HasValue.get""
  IL_0007:  brfalse.s  IL_0028
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""S S?.GetValueOrDefault()""
  IL_0010:  stloc.0
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       ""int S.Length.get""
  IL_0018:  ldc.i4.1
  IL_0019:  bne.un.s   IL_0028
  IL_001b:  ldloca.s   V_0
  IL_001d:  ldc.i4.0
  IL_001e:  call       ""int S.this[int].get""
  IL_0023:  ldc.i4.s   42
  IL_0025:  ceq
  IL_0027:  ret
  IL_0028:  ldc.i4.0
  IL_0029:  ret
}
");
    }

    [Fact]
    public void ListPattern_Negated_01()
    {
        var source = @"
class X
{
    public void Test1(int[] a)
    {
        switch (a)
        {
            case not [{} y, .. {} z] x: _ = (x, y, z); break;
            case [not {} y, .. not {} z] x: _ = (x, y, z); break;
        }
    }
}
";
        var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (8,26): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //             case not [{} y, .. {} z] x: _ = (x, y, z); break;
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y").WithLocation(8, 26),
            // (8,35): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //             case not [{} y, .. {} z] x: _ = (x, y, z); break;
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "z").WithLocation(8, 35),
            // (8,38): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //             case not [{} y, .. {} z] x: _ = (x, y, z); break;
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "x").WithLocation(8, 38),
            // (8,46): error CS0165: Use of unassigned local variable 'x'
            //             case not [{} y, .. {} z] x: _ = (x, y, z); break;
            Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(8, 46),
            // (8,49): error CS0165: Use of unassigned local variable 'y'
            //             case not [{} y, .. {} z] x: _ = (x, y, z); break;
            Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(8, 49),
            // (8,52): error CS0165: Use of unassigned local variable 'z'
            //             case not [{} y, .. {} z] x: _ = (x, y, z); break;
            Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(8, 52),
            // (9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case [not {} y, .. not {} z] x: _ = (x, y, z); break;
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[not {} y, .. not {} z] x").WithLocation(9, 18),
            // (9,26): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //             case [not {} y, .. not {} z] x: _ = (x, y, z); break;
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y").WithLocation(9, 26),
            // (9,39): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //             case [not {} y, .. not {} z] x: _ = (x, y, z); break;
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "z").WithLocation(9, 39),
            // (9,53): error CS0165: Use of unassigned local variable 'y'
            //             case [not {} y, .. not {} z] x: _ = (x, y, z); break;
            Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(9, 53),
            // (9,56): error CS0165: Use of unassigned local variable 'z'
            //             case [not {} y, .. not {} z] x: _ = (x, y, z); break;
            Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(9, 56)
            );
    }

    [Fact]
    public void ListPattern_Negated_02()
    {
        var source = @"
class X
{
    public void Test1(int[] a)
    {
        if (a is not [{} y, .. {} z] x)
             _ = (x, y, z); // 1
        else 
             _ = (x, y, z);
    }
    public void Test2(int[] a)
    {
        if (a is [not {} y, .. not {} z] x)
             _ = (x, y, z);
        else 
             _ = (x, y, z); // 2
    }
}
";
        var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (7,19): error CS0165: Use of unassigned local variable 'x'
            //              _ = (x, y, z); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(7, 19),
            // (7,22): error CS0165: Use of unassigned local variable 'y'
            //              _ = (x, y, z); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(7, 22),
            // (7,25): error CS0165: Use of unassigned local variable 'z'
            //              _ = (x, y, z); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(7, 25),
            // (13,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         if (a is [not {} y, .. not {} z] x)
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [not {} y, .. not {} z] x").WithArguments("int[]").WithLocation(13, 13),
            // (13,26): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //         if (a is [not {} y, .. not {} z] x)
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y").WithLocation(13, 26),
            // (13,39): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //         if (a is [not {} y, .. not {} z] x)
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "z").WithLocation(13, 39),
            // (16,19): error CS0165: Use of unassigned local variable 'x'
            //              _ = (x, y, z); // 2
            Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(16, 19),
            // (16,22): error CS0165: Use of unassigned local variable 'y'
            //              _ = (x, y, z); // 2
            Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(16, 22),
            // (16,25): error CS0165: Use of unassigned local variable 'z'
            //              _ = (x, y, z); // 2
            Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(16, 25)
            );
    }

    [Fact]
    public void ListPattern_UseSiteErrorOnIndexerAndSlice()
    {
        var missing_cs = @"
public class Missing
{
}
";
        var missingRef = CreateCompilation(missing_cs, assemblyName: "missing")
            .EmitToImageReference();

        var lib2_cs = @"
public class C
{
    public int Length => 0;
    public Missing this[int i] => throw null;
    public Missing Slice(int i, int j) => throw null;
}
";
        var lib2Ref = CreateCompilation(lib2_cs, references: new[] { missingRef })
            .EmitToImageReference();

        var source = @"
class D
{
    void M(C c)
    {
        _ = c is [var item];
        _ = c is [..var rest];
        var index = c[^1];
        var range = c[1..^1];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range },
            references: new[] { lib2Ref }, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (6,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         _ = c is [var item];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[var item]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 18),
            // (7,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         _ = c is [..var rest];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[..var rest]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18),
            // (7,19): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         _ = c is [..var rest];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "..var rest").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 19),
            // (8,21): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         var index = c[^1];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 21),
            // (9,21): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         var range = c[1..^1];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[1..^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(9, 21)
            );

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
        var declarations = tree.GetRoot().DescendantNodes().OfType<VarPatternSyntax>().ToArray();

        verify(declarations[0], "item", "Missing?");
        verify(declarations[1], "rest", "Missing?");

        void verify(VarPatternSyntax declaration, string name, string expectedType)
        {
            var local = (ILocalSymbol)model.GetDeclaredSymbol(declaration.Designation)!;
            Assert.Equal(name, local.Name);
            Assert.Equal(expectedType, local.Type.ToTestDisplayString(includeNonNullable: true));
        }
    }

    [Fact]
    public void ListPattern_UseSiteErrorOnIndexAndRangeIndexers_WithFallback()
    {
        var missing_cs = @"
public class Missing
{
}
";
        var missingRef = CreateCompilation(missing_cs, assemblyName: "missing")
            .EmitToImageReference();

        var lib2_cs = @"
using System;
public class C
{
    public int Length => 0;
    public Missing this[Index i] => throw null;
    public Missing this[Range r] => throw null;
    public int this[int i] => throw null;
    public int Slice(int i, int j) => throw null;
}
";
        var lib2Ref = CreateCompilation(new[] { lib2_cs, TestSources.Index, TestSources.Range }, references: new[] { missingRef })
            .EmitToImageReference();

        var source = @"
class D
{
    void M(C c)
    {
        _ = c is [var item];
        _ = c is [..var rest];
        var index = c[^1];
        var range = c[1..^1];
    }
}
";
        var compilation = CreateCompilation(source, references: new[] { lib2Ref }, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (6,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         _ = c is [var item];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[var item]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 18),
            // (7,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         _ = c is [..var rest];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[..var rest]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18),
            // (7,19): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         _ = c is [..var rest];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "..var rest").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 19),
            // (8,21): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         var index = c[^1];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 21),
            // (9,21): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         var range = c[1..^1];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[1..^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(9, 21)
            );
    }

    [Fact]
    public void ListPattern_Symbols_01()
    {
        var source = @"
#nullable enable
class X
{
    public void Test(string[]? strings, int[] integers)
    {
        _ = strings is [var element1] list1a;
        _ = strings is [..var slice1] list1b;

        _ = integers is [var element2] list2a;
        _ = integers is [..var slice2] list2b;

        _ = strings is [string element3] list3a;
        _ = strings is [..string[] slice3] list3b;

        _ = integers is [int element4] list4a;
        _ = integers is [..int[] slice4] list4b;
    }
}";
        var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyDiagnostics();
        var tree = compilation.SyntaxTrees[0];
        var nodes = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>();
        Assert.Collection(nodes,
            d => verify(d, "element1", "string?", "string"),
            d => verify(d, "list1a", "string[]?", "string[]"),
            d => verify(d, "slice1", "string[]?", "string[]"),
            d => verify(d, "list1b", "string[]?", "string[]"),

            d => verify(d, "element2", "int", "int"),
            d => verify(d, "list2a", "int[]?", "int[]"),
            d => verify(d, "slice2", "int[]?", "int[]"),
            d => verify(d, "list2b", "int[]?", "int[]"),

            d => verify(d, "element3", "string", "string"),
            d => verify(d, "list3a", "string[]?", "string[]"),
            d => verify(d, "slice3", "string[]", "string[]"),
            d => verify(d, "list3b", "string[]?", "string[]"),

            d => verify(d, "element4", "int", "int"),
            d => verify(d, "list4a", "int[]?", "int[]"),
            d => verify(d, "slice4", "int[]", "int[]"),
            d => verify(d, "list4b", "int[]?", "int[]")
        );

        void verify(SyntaxNode designation, string syntax, string declaredType, string type)
        {
            Assert.Equal(syntax, designation.ToString());
            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(designation);
            Assert.Equal(SymbolKind.Local, symbol.Kind);
            Assert.Equal(declaredType, ((ILocalSymbol)symbol).Type.ToDisplayString());
            var typeInfo = model.GetTypeInfo(designation);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            typeInfo = model.GetTypeInfo(designation.Parent);
            Assert.Equal(type, typeInfo.Type.ToDisplayString());
            Assert.Equal(type, typeInfo.ConvertedType.ToDisplayString());
        }
    }

    [Fact]
    public void ListPattern_Symbols_02()
    {
        var source =
@"class X
{
    public void Test(string[] strings, int[] integers)
    {
        _ = strings is [{}];
        _ = strings is [..{}];

        _ = integers is [{}];
        _ = integers is [..{}];
    }
}";
        var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyDiagnostics();
        var tree = compilation.SyntaxTrees[0];
        var nodes = tree.GetRoot().DescendantNodes()
            .OfType<PropertyPatternClauseSyntax>()
            .Where(p => p.IsKind(SyntaxKind.PropertyPatternClause));
        Assert.Collection(nodes,
            d => verify(d, "[{}]", "string"),
            d => verify(d, "..{}", "string[]"),

            d => verify(d, "[{}]", "int"),
            d => verify(d, "..{}", "int[]")
        );

        void verify(PropertyPatternClauseSyntax clause, string syntax, string type)
        {
            Assert.Equal(syntax, clause.Parent.Parent.ToString());
            var model = compilation.GetSemanticModel(tree);
            var typeInfo = model.GetTypeInfo(clause.Parent); // inner {} pattern
            Assert.Equal(type, typeInfo.Type.ToDisplayString());
            Assert.Equal(type, typeInfo.ConvertedType.ToDisplayString());
        }
    }

    [Fact]
    public void PatternIndexRangeReadOnly_01()
    {
        // Relates to https://github.com/dotnet/roslyn/pull/37194
        var src = @"
using System;
struct S
{
    public int this[int i] => 0;
    public int Length => 0;
    public int Slice(int x, int y) => 0;

    readonly void M(Index i, Range r)
    {
        _ = this[i]; // 1, 2
        _ = this[r]; // 3, 4

        _ = this is [1];
        _ = this is [2, ..var rest];
    }
}";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyDiagnostics(
            // (11,13): warning CS8656: Call to non-readonly member 'S.Length.get' from a 'readonly' member results in an implicit copy of 'this'.
            //         _ = this[i]; // 1, 2
            Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Length.get", "this").WithLocation(11, 13),
            // (11,13): warning CS8656: Call to non-readonly member 'S.this[int].get' from a 'readonly' member results in an implicit copy of 'this'.
            //         _ = this[i]; // 1, 2
            Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.this[int].get", "this").WithLocation(11, 13),
            // (12,13): warning CS8656: Call to non-readonly member 'S.Length.get' from a 'readonly' member results in an implicit copy of 'this'.
            //         _ = this[r]; // 3, 4
            Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Length.get", "this").WithLocation(12, 13),
            // (12,13): warning CS8656: Call to non-readonly member 'S.Slice(int, int)' from a 'readonly' member results in an implicit copy of 'this'.
            //         _ = this[r]; // 3, 4
            Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Slice(int, int)", "this").WithLocation(12, 13));
    }

    [Fact]
    public void ListPattern_VoidLength()
    {
        var source = @"
class C
{
    public void Length => throw null;
    public int this[int i] => throw null;
    public void M()
    {
        _ = this is [1];
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (4,17): error CS0547: 'C.Length': property or indexer cannot have void type
            //     public void Length => throw null;
            Diagnostic(ErrorCode.ERR_PropertyCantHaveVoidType, "Length").WithArguments("C.Length").WithLocation(4, 17),
            // (8,21): error CS9200: List patterns may not be used for a value of type 'C'.
            //         _ = this is [1];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[1]").WithArguments("C").WithLocation(8, 21)
            );
    }

    [Fact]
    public void ListPattern_StringLength()
    {
        var source = @"
class C
{
    public string Length => throw null;
    public int this[int i] => throw null;

    public void M()
    {
        _ = this is [1];
        _ = this[^1];
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (9,21): error CS9200: List patterns may not be used for a value of type 'C'.
            //         _ = this is [1];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[1]").WithArguments("C").WithLocation(9, 21),
            // (10,18): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "^1").WithArguments("System.Index").WithLocation(10, 18),
            // (10,18): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^1").WithArguments("System.Index", ".ctor").WithLocation(10, 18)
            );
    }

    [Fact]
    public void SlicePattern_VoidReturn()
    {
        var source = @"
class C
{
    public int Length => throw null;
    public int this[int i] => throw null;
    public void Slice(int i, int j) => throw null;

    public void M()
    {
        _ = this is [..];
        _ = this is [.._];
        _ = this is [..var unused];
        if (this is [..var used])
        {
            System.Console.Write(used);
        }
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (11,22): error CS9201: Slice patterns may not be used for a value of type 'C'.
            //         _ = this is [.._];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, ".._").WithArguments("C").WithLocation(11, 22),
            // (12,22): error CS9201: Slice patterns may not be used for a value of type 'C'.
            //         _ = this is [..var unused];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..var unused").WithArguments("C").WithLocation(12, 22),
            // (13,22): error CS9201: Slice patterns may not be used for a value of type 'C'.
            //         if (this is [..var used])
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..var used").WithArguments("C").WithLocation(13, 22)
            );
    }

    [Theory]
    [InlineData("[.._]", "Length True")]
    [InlineData("[..]", "True")]
    [InlineData("[..var unused]", "Length Slice True")]
    [InlineData("[42, ..]", "Length Index True")]
    [InlineData("[42, .._]", "Length Index True")]
    [InlineData("[42, ..var unused]", "Length Index Slice True")]
    public void ListPattern_OnlyCallApisRequiredByPattern(string pattern, string expectedOutput)
    {
        var source = $@"
System.Console.Write(new C() is {pattern});

public class C
{{
    public int Length {{ get {{ System.Console.Write(""Length ""); return 1; }} }}
    public int this[int i] {{ get {{ System.Console.Write(""Index ""); return 42; }} }}
    public int Slice(int i, int j) {{ System.Console.Write(""Slice ""); return 0; }}
}}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyDiagnostics();
        CompileAndVerify(compilation, expectedOutput: expectedOutput);
    }

    [Fact]
    public void ListPattern_GenericIndexingParameter()
    {
        var source = @"
#nullable enable
class C<T>
{
    public int Length => throw null!;
    public int this[T i] => throw null!;

    public void M()
    {
        if (new C<int>() is [var item]) // 1
            item.ToString();
        _ = new C<int>()[^1]; // 2

        if (new C<int?>() is [var item2]) // 3
            item2.Value.ToString();
        _ = new C<int?>()[^1]; // 4

        if (new C<System.Index>() is [var item3])
            item3.ToString();
        _ = new C<System.Index>()[^1];

        if (new C<System.Index?>() is [var item4])
            item4.ToString();
        _ = new C<System.Index?>()[^1];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index });
        compilation.VerifyEmitDiagnostics(
            // (10,29): error CS9200: List patterns may not be used for a value of type 'C<int>'.
            //         if (new C<int>() is [var item]) // 1
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[var item]").WithArguments("C<int>").WithLocation(10, 29),
            // (12,26): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            //         _ = new C<int>()[^1]; // 2
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(12, 26),
            // (14,30): error CS9200: List patterns may not be used for a value of type 'C<int?>'.
            //         if (new C<int?>() is [var item2]) // 3
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[var item2]").WithArguments("C<int?>").WithLocation(14, 30),
            // (16,27): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int?'
            //         _ = new C<int?>()[^1]; // 4
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int?").WithLocation(16, 27)
            );
    }

    [Fact]
    public void ListPattern_Nullability()
    {
        var source = @"
#nullable enable
class C<T>
{
    public int Length => throw null!;
    public T this[int i] => throw null!;

    public void M()
    {
        if (new C<int>() is [var item])
            item.ToString();
        else
            item.ToString(); // 1

        if (new C<int?>() is [var item2])
            item2.Value.ToString(); // 2
        else
            item2.Value.ToString(); // 3, 4

        if (new C<string?>() is [var item3])
            item3.ToString(); // 5
        else
            item3.ToString(); // 6, 7

        if (new C<string>() is [var item4])
        {
            item4.ToString();
            item4 = null;
        }
        else
            item4.ToString(); // 8, 9
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (13,13): error CS0165: Use of unassigned local variable 'item'
            //             item.ToString(); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item").WithArguments("item").WithLocation(13, 13),
            // (16,13): warning CS8629: Nullable value type may be null.
            //             item2.Value.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "item2").WithLocation(16, 13),
            // (18,13): warning CS8629: Nullable value type may be null.
            //             item2.Value.ToString(); // 3, 4
            Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "item2").WithLocation(18, 13),
            // (18,13): error CS0165: Use of unassigned local variable 'item2'
            //             item2.Value.ToString(); // 3, 4
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item2").WithArguments("item2").WithLocation(18, 13),
            // (21,13): warning CS8602: Dereference of a possibly null reference.
            //             item3.ToString(); // 5
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "item3").WithLocation(21, 13),
            // (23,13): warning CS8602: Dereference of a possibly null reference.
            //             item3.ToString(); // 6, 7
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "item3").WithLocation(23, 13),
            // (23,13): error CS0165: Use of unassigned local variable 'item3'
            //             item3.ToString(); // 6, 7
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item3").WithArguments("item3").WithLocation(23, 13),
            // (31,13): warning CS8602: Dereference of a possibly null reference.
            //             item4.ToString(); // 8, 9
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "item4").WithLocation(31, 13),
            // (31,13): error CS0165: Use of unassigned local variable 'item4'
            //             item4.ToString(); // 8, 9
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item4").WithArguments("item4").WithLocation(31, 13)
            );

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
        var declarations = tree.GetRoot().DescendantNodes().OfType<VarPatternSyntax>().ToArray();

        Assert.Equal(4, declarations.Length);
        verify(declarations[0], "item", "System.Int32");
        verify(declarations[1], "item2", "System.Int32?");
        verify(declarations[2], "item3", "System.String?");
        verify(declarations[3], "item4", "System.String?");

        void verify(VarPatternSyntax declaration, string name, string expectedType)
        {
            var local = (ILocalSymbol)model.GetDeclaredSymbol(declaration.Designation)!;
            Assert.Equal(name, local.Name);
            Assert.Equal(expectedType, local.Type.ToTestDisplayString(includeNonNullable: true));
        }
    }

    [Fact]
    public void ListPattern_Nullability_IndexIndexer()
    {
        var source = @"
using System;
#nullable enable
class C<T>
{
    public int Length => throw null!;
    public T this[Index i] => throw null!;

    public void M()
    {
        if (new C<int>() is [var item])
            item.ToString();
        else
            item.ToString(); // 1

        if (new C<int?>() is [var item2])
            item2.Value.ToString(); // 2
        else
            item2.Value.ToString(); // 3, 4

        if (new C<string?>() is [var item3])
            item3.ToString(); // 5
        else
            item3.ToString(); // 6, 7

        if (new C<string>() is [var item4])
        {
            item4.ToString();
            item4 = null;
        }
        else
            item4.ToString(); // 8, 9
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index });
        compilation.VerifyEmitDiagnostics(
            // (14,13): error CS0165: Use of unassigned local variable 'item'
            //             item.ToString(); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item").WithArguments("item").WithLocation(14, 13),
            // (17,13): warning CS8629: Nullable value type may be null.
            //             item2.Value.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "item2").WithLocation(17, 13),
            // (19,13): warning CS8629: Nullable value type may be null.
            //             item2.Value.ToString(); // 3, 4
            Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "item2").WithLocation(19, 13),
            // (19,13): error CS0165: Use of unassigned local variable 'item2'
            //             item2.Value.ToString(); // 3, 4
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item2").WithArguments("item2").WithLocation(19, 13),
            // (22,13): warning CS8602: Dereference of a possibly null reference.
            //             item3.ToString(); // 5
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "item3").WithLocation(22, 13),
            // (24,13): warning CS8602: Dereference of a possibly null reference.
            //             item3.ToString(); // 6, 7
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "item3").WithLocation(24, 13),
            // (24,13): error CS0165: Use of unassigned local variable 'item3'
            //             item3.ToString(); // 6, 7
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item3").WithArguments("item3").WithLocation(24, 13),
            // (32,13): warning CS8602: Dereference of a possibly null reference.
            //             item4.ToString(); // 8, 9
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "item4").WithLocation(32, 13),
            // (32,13): error CS0165: Use of unassigned local variable 'item4'
            //             item4.ToString(); // 8, 9
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item4").WithArguments("item4").WithLocation(32, 13)
            );

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
        var declarations = tree.GetRoot().DescendantNodes().OfType<VarPatternSyntax>().ToArray();

        Assert.Equal(4, declarations.Length);
        verify(declarations[0], "item", "System.Int32");
        verify(declarations[1], "item2", "System.Int32?");
        verify(declarations[2], "item3", "System.String?");
        verify(declarations[3], "item4", "System.String?");

        void verify(VarPatternSyntax declaration, string name, string expectedType)
        {
            var local = (ILocalSymbol)model.GetDeclaredSymbol(declaration.Designation)!;
            Assert.Equal(name, local.Name);
            Assert.Equal(expectedType, local.Type.ToTestDisplayString(includeNonNullable: true));
        }
    }

    [Fact]
    public void ListPattern_Nullability_Array()
    {
        var source = @"
#nullable enable
class C
{
    public void M()
    {
        if (new int[0] is [var item])
            item.ToString();
        else
            item.ToString(); // 1

        if (new int?[0] is [var item2])
            item2.ToString();
        else
            item2.ToString(); // 2

        if (new string?[0] is [var item3])
            item3.ToString(); // 3
        else
            item3.ToString(); // 4, 5

        if (new string[0] is [var item4])
        {
            item4.ToString();
            item4 = null;
        }
        else
            item4.ToString(); // 6, 7
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (10,13): error CS0165: Use of unassigned local variable 'item'
            //             item.ToString(); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item").WithArguments("item").WithLocation(10, 13),
            // (15,13): error CS0165: Use of unassigned local variable 'item2'
            //             item2.ToString(); // 2
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item2").WithArguments("item2").WithLocation(15, 13),
            // (18,13): warning CS8602: Dereference of a possibly null reference.
            //             item3.ToString(); // 3
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "item3").WithLocation(18, 13),
            // (20,13): warning CS8602: Dereference of a possibly null reference.
            //             item3.ToString(); // 4, 5
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "item3").WithLocation(20, 13),
            // (20,13): error CS0165: Use of unassigned local variable 'item3'
            //             item3.ToString(); // 4, 5
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item3").WithArguments("item3").WithLocation(20, 13),
            // (28,13): warning CS8602: Dereference of a possibly null reference.
            //             item4.ToString(); // 6, 7
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "item4").WithLocation(28, 13),
            // (28,13): error CS0165: Use of unassigned local variable 'item4'
            //             item4.ToString(); // 6, 7
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item4").WithArguments("item4").WithLocation(28, 13)
            );

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
        var declarations = tree.GetRoot().DescendantNodes().OfType<VarPatternSyntax>().ToArray();

        Assert.Equal(4, declarations.Length);
        verify(declarations[0], "item", "System.Int32");
        verify(declarations[1], "item2", "System.Int32?");
        verify(declarations[2], "item3", "System.String?");
        verify(declarations[3], "item4", "System.String?");

        void verify(VarPatternSyntax declaration, string name, string expectedType)
        {
            var local = (ILocalSymbol)model.GetDeclaredSymbol(declaration.Designation)!;
            Assert.Equal(name, local.Name);
            Assert.Equal(expectedType, local.Type.ToTestDisplayString(includeNonNullable: true));
        }
    }

    [Fact]
    public void SlicePattern_Nullability()
    {
        var source = @"
#nullable enable
class C<T>
{
    public int Length => throw null!;
    public int this[int i] => throw null!;
    public T Slice(int i, int j) => throw null!;

    public void M()
    {
        if (new C<int>() is [1, ..var rest])
            rest.ToString();
        else
            rest.ToString(); // 1

        if (new C<int?>() is [1, ..var rest2])
            rest2.Value.ToString(); // 2
        else
            rest2.Value.ToString(); // 3, 4

        if (new C<string?>() is [1, ..var rest3])
            rest3.ToString(); // 5
        else
            rest3.ToString(); // 6, 7

        if (new C<string>() is [1, ..var rest4])
        {
            rest4.ToString();
            rest4 = null;
        }
        else
            rest4.ToString(); // 8, 9

        if (new C<T>() is [1, ..var rest5])
        {
            rest5.ToString(); // 10
            rest5 = default;
        }
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (14,13): error CS0165: Use of unassigned local variable 'rest'
            //             rest.ToString(); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest").WithArguments("rest").WithLocation(14, 13),
            // (17,13): warning CS8629: Nullable value type may be null.
            //             rest2.Value.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "rest2").WithLocation(17, 13),
            // (19,13): warning CS8629: Nullable value type may be null.
            //             rest2.Value.ToString(); // 3, 4
            Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "rest2").WithLocation(19, 13),
            // (19,13): error CS0165: Use of unassigned local variable 'rest2'
            //             rest2.Value.ToString(); // 3, 4
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest2").WithArguments("rest2").WithLocation(19, 13),
            // (22,13): warning CS8602: Dereference of a possibly null reference.
            //             rest3.ToString(); // 5
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest3").WithLocation(22, 13),
            // (24,13): warning CS8602: Dereference of a possibly null reference.
            //             rest3.ToString(); // 6, 7
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest3").WithLocation(24, 13),
            // (24,13): error CS0165: Use of unassigned local variable 'rest3'
            //             rest3.ToString(); // 6, 7
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest3").WithArguments("rest3").WithLocation(24, 13),
            // (32,13): warning CS8602: Dereference of a possibly null reference.
            //             rest4.ToString(); // 8, 9
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest4").WithLocation(32, 13),
            // (32,13): error CS0165: Use of unassigned local variable 'rest4'
            //             rest4.ToString(); // 8, 9
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest4").WithArguments("rest4").WithLocation(32, 13),
            // (36,13): warning CS8602: Dereference of a possibly null reference.
            //             rest5.ToString(); // 10
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest5").WithLocation(36, 13)
            );

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
        var declarations = tree.GetRoot().DescendantNodes().OfType<VarPatternSyntax>().ToArray();

        Assert.Equal(5, declarations.Length);
        verify(declarations[0], "rest", "System.Int32");
        verify(declarations[1], "rest2", "System.Int32?");
        verify(declarations[2], "rest3", "System.String?");
        verify(declarations[3], "rest4", "System.String?");
        verify(declarations[4], "rest5", "T?");

        void verify(VarPatternSyntax declaration, string name, string expectedType)
        {
            var local = (ILocalSymbol)model.GetDeclaredSymbol(declaration.Designation)!;
            Assert.Equal(name, local.Name);
            Assert.Equal(expectedType, local.Type.ToTestDisplayString(includeNonNullable: true));
        }
    }

    [Fact]
    public void SlicePattern_Nullability_RangeIndexer()
    {
        var source = @"
#nullable enable
using System;
class C<T>
{
    public int Length => throw null!;
    public int this[Index i] => throw null!;
    public T this[Range r] => throw null!;

    public void M()
    {
        if (new C<int>() is [1, ..var rest])
            rest.ToString();
        else
            rest.ToString(); // 1

        if (new C<int?>() is [1, ..var rest2])
            rest2.Value.ToString(); // 2
        else
            rest2.Value.ToString(); // 3, 4

        if (new C<string?>() is [1, ..var rest3])
            rest3.ToString(); // 5
        else
            rest3.ToString(); // 6, 7

        if (new C<string>() is [1, ..var rest4])
        {
            rest4.ToString();
            rest4 = null;
        }
        else
            rest4.ToString(); // 8, 9
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // (15,13): error CS0165: Use of unassigned local variable 'rest'
            //             rest.ToString(); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest").WithArguments("rest").WithLocation(15, 13),
            // (18,13): warning CS8629: Nullable value type may be null.
            //             rest2.Value.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "rest2").WithLocation(18, 13),
            // (20,13): warning CS8629: Nullable value type may be null.
            //             rest2.Value.ToString(); // 3, 4
            Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "rest2").WithLocation(20, 13),
            // (20,13): error CS0165: Use of unassigned local variable 'rest2'
            //             rest2.Value.ToString(); // 3, 4
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest2").WithArguments("rest2").WithLocation(20, 13),
            // (23,13): warning CS8602: Dereference of a possibly null reference.
            //             rest3.ToString(); // 5
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest3").WithLocation(23, 13),
            // (25,13): warning CS8602: Dereference of a possibly null reference.
            //             rest3.ToString(); // 6, 7
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest3").WithLocation(25, 13),
            // (25,13): error CS0165: Use of unassigned local variable 'rest3'
            //             rest3.ToString(); // 6, 7
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest3").WithArguments("rest3").WithLocation(25, 13),
            // (33,13): warning CS8602: Dereference of a possibly null reference.
            //             rest4.ToString(); // 8, 9
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest4").WithLocation(33, 13),
            // (33,13): error CS0165: Use of unassigned local variable 'rest4'
            //             rest4.ToString(); // 8, 9
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest4").WithArguments("rest4").WithLocation(33, 13)
            );
    }

    [Fact]
    public void SlicePattern_Nullability_Array()
    {
        var source = @"
#nullable enable
class C
{
    public void M()
    {
        if (new int[0] is [1, ..var rest])
        {
            rest.ToString();
            rest = null;
        }
        else
            rest.ToString(); // 1, 2

        if (new int?[0] is [1, ..var rest2])
            rest2.ToString();
        else
            rest2.ToString(); // 3, 4

        if (new string?[0] is [null, ..var rest3])
            rest3.ToString();
        else
            rest3.ToString(); // 5, 6

        if (new string[0] is [null, ..var rest4])
            rest4.ToString();
        else
            rest4.ToString(); // 7, 8
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (13,13): warning CS8602: Dereference of a possibly null reference.
            //             rest.ToString(); // 1, 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest").WithLocation(13, 13),
            // (13,13): error CS0165: Use of unassigned local variable 'rest'
            //             rest.ToString(); // 1, 2
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest").WithArguments("rest").WithLocation(13, 13),
            // (18,13): warning CS8602: Dereference of a possibly null reference.
            //             rest2.ToString(); // 3, 4
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest2").WithLocation(18, 13),
            // (18,13): error CS0165: Use of unassigned local variable 'rest2'
            //             rest2.ToString(); // 3, 4
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest2").WithArguments("rest2").WithLocation(18, 13),
            // (23,13): warning CS8602: Dereference of a possibly null reference.
            //             rest3.ToString(); // 5, 6
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest3").WithLocation(23, 13),
            // (23,13): error CS0165: Use of unassigned local variable 'rest3'
            //             rest3.ToString(); // 5, 6
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest3").WithArguments("rest3").WithLocation(23, 13),
            // (28,13): warning CS8602: Dereference of a possibly null reference.
            //             rest4.ToString(); // 7, 8
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest4").WithLocation(28, 13),
            // (28,13): error CS0165: Use of unassigned local variable 'rest4'
            //             rest4.ToString(); // 7, 8
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest4").WithArguments("rest4").WithLocation(28, 13)
            );

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
        var declarations = tree.GetRoot().DescendantNodes().OfType<VarPatternSyntax>().ToArray();

        Assert.Equal(4, declarations.Length);
        verify(declarations[0], "rest", "System.Int32[]?");
        verify(declarations[1], "rest2", "System.Int32?[]?");
        verify(declarations[2], "rest3", "System.String?[]?");
        verify(declarations[3], "rest4", "System.String![]?");

        void verify(VarPatternSyntax declaration, string name, string expectedType)
        {
            var local = (ILocalSymbol)model.GetDeclaredSymbol(declaration.Designation)!;
            Assert.Equal(name, local.Name);
            Assert.Equal(expectedType, local.Type.ToTestDisplayString(includeNonNullable: true));
        }
    }

    [Fact]
    public void SlicePattern_DefiniteAssignment()
    {
        var source = @"
class C
{
    public int Length => throw null!;
    public int this[int i] => throw null!;
    public int Slice(int i, int j) => throw null!;

    public void M()
    {
        if (new C() is [var item, ..var rest])
        {
            item.ToString();
            rest.ToString();
        }
        else
        {
            item.ToString(); // 1
            rest.ToString(); // 2
        }
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (17,13): error CS0165: Use of unassigned local variable 'item'
            //             item.ToString(); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "item").WithArguments("item").WithLocation(17, 13),
            // (18,13): error CS0165: Use of unassigned local variable 'rest'
            //             rest.ToString(); // 2
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest").WithArguments("rest").WithLocation(18, 13)
            );
    }

    [Fact]
    public void SlicePattern_LengthAndIndexAndSliceAreStatic()
    {
        // Length, indexer and Slice are static
        var il = @"
.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{
    .method public hidebysig specialname static int32 get_Length () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig static specialname int32 get_Item ( int32 i ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig static int32 Slice ( int32 i, int32 j ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }

    .property int32 Length()
    {
        .get int32 C::get_Length()
    }

    .property int32 Item( int32 i )
    {
        .get int32 C::get_Item(int32)
    }
}
";
        var source = @"
class D
{
    public void M()
    {
        _ = new C() is [var item, ..var rest];
    }
}
";
        var compilation = CreateCompilationWithIL(source, il);
        compilation.VerifyEmitDiagnostics(
            // (6,24): error CS9200: List patterns may not be used for a value of type 'C'.
            //         _ = new C() is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[var item, ..var rest]").WithArguments("C").WithLocation(6, 24),
            // (6,35): error CS9201: Slice patterns may not be used for a value of type 'C'.
            //         _ = new C() is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..var rest").WithArguments("C").WithLocation(6, 35)
            );
    }

    [Fact]
    public void SlicePattern_LengthAndIndexAndSliceAreStatic_IndexAndRange()
    {
        // Length, [Index] and [Range] are static
        var il = @"
.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{
    .method public hidebysig specialname static int32 get_Length () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig static specialname int32 get_Item ( valuetype System.Index i ) cil managed // static this[System.Index]
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig static specialname int32 get_Item ( valuetype System.Range i ) cil managed // static this[System.Range]
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }

    .property int32 Length()
    {
        .get int32 C::get_Length()
    }
    .property int32 Item( valuetype System.Index i )
    {
        .get int32 C::get_Item( valuetype System.Index )
    }
    .property int32 Item( valuetype System.Range i )
    {
        .get int32 C::get_Item( valuetype System.Range )
    }
}

.class public sequential ansi sealed beforefieldinit System.Index
    extends [mscorlib]System.ValueType
    implements class [mscorlib]System.IEquatable`1<valuetype System.Index>
{
    .pack 0
    .size 1

    .method public hidebysig specialname rtspecialname instance void .ctor ( int32 'value', [opt] bool fromEnd ) cil managed
    {
        .param [2] = bool(false)
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname static valuetype System.Index get_Start () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname static valuetype System.Index get_End () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig static valuetype System.Index FromStart ( int32 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig static valuetype System.Index FromEnd ( int32 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname instance int32 get_Value () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname instance bool get_IsFromEnd () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig instance int32 GetOffset ( int32 length ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public final hidebysig newslot virtual instance bool Equals ( valuetype System.Index other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname static valuetype System.Index op_Implicit ( int32 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property valuetype System.Index Start()
    {
        .get valuetype System.Index System.Index::get_Start()
    }
    .property valuetype System.Index End()
    {
        .get valuetype System.Index System.Index::get_End()
    }
    .property instance int32 Value()
    {
        .get instance int32 System.Index::get_Value()
    }
    .property instance bool IsFromEnd()
    {
        .get instance bool System.Index::get_IsFromEnd()
    }
}

.class public sequential ansi sealed beforefieldinit System.Range
    extends [mscorlib]System.ValueType
{
    .method public hidebysig specialname instance valuetype System.Index get_Start () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname instance valuetype System.Index get_End () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor ( valuetype System.Index start, valuetype System.Index end ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig static valuetype System.Range StartAt ( valuetype System.Index start ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig static valuetype System.Range EndAt ( valuetype System.Index end ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname static valuetype System.Range get_All () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance valuetype System.Index Start()
    {
        .get instance valuetype System.Index System.Range::get_Start()
    }
    .property instance valuetype System.Index End()
    {
        .get instance valuetype System.Index System.Range::get_End()
    }
    .property valuetype System.Range All()
    {
        .get valuetype System.Range System.Range::get_All()
    }
}
";
        var source = @"
class D
{
    public void M()
    {
        _ = new C() is [var item, ..var rest];
    }
}
";
        var compilation = CreateCompilationWithIL(source, il);
        compilation.VerifyEmitDiagnostics(
            // (6,24): error CS9200: List patterns may not be used for a value of type 'C'.
            //         _ = new C() is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[var item, ..var rest]").WithArguments("C").WithLocation(6, 24),
            // (6,35): error CS9201: Slice patterns may not be used for a value of type 'C'.
            //         _ = new C() is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..var rest").WithArguments("C").WithLocation(6, 35)
            );
    }

    [Fact]
    public void Pattern_Nullability_Exhaustiveness()
    {
        var source = @"
#nullable enable
object?[]? o = null;

_ = o switch
{
    null => 0,
    [null] => 0,
    [not null] => 0,
    { Length: 0 or > 1 } => 0,
};

_ = o switch // didn't test for null
{
    [null] => 0,
    [not null] => 0,
    { Length: 0 or > 1 } => 0,
};

_ = o switch // didn't test for [null]
{
    null => 0,
    [not null] => 0,
    { Length: 0 or > 1 } => 0,
};

_ = o switch // didn't test for [not null]
{
    null => 0,
    [null] => 0,
    { Length: 0 or > 1 } => 0,
};

_ = o switch
{
    null => 0,
    [] => 0,
    [.., null] => 0,
    [.., not null] => 0,
};

_ = o switch // didn't test for [.., null]
{
    null => 0,
    [] => 0,
    [.., not null] => 0,
};

_ = o switch // didn't test for [.., not null]
{
    null => 0,
    [] => 0,
    [.., null] => 0,
};

_ = o switch
{
    null => 0,
    [.., null] => 0,
    [not null, ..] => 0,
    { Length: 0 or > 1 } => 0,
};
";
        // PROTOTYPE: incorrect exhaustiveness examples from explainer
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (13,7): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
            // _ = o switch // didn't test for null
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(13, 7),
            // (20,7): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = o switch // didn't test for [null]
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("_").WithLocation(20, 7),
            // (27,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = o switch // didn't test for [not null]
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(27, 7),
            // (42,7): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = o switch // didn't test for [.., null]
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("_").WithLocation(42, 7),
            // (49,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = o switch // didn't test for [.., not null]
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(49, 7)
            );
    }

    [Fact]
    public void SlicePattern_Nullability_Exhaustiveness()
    {
        var source = @"
#nullable enable
using System;
class C
{
    public int Length => throw null!;
    public object? this[Index i] => throw null!;
    public object? this[Range r] => throw null!;

    public void M()
    {
        _ = this switch
        {
            null => 0,
            [] => 0,
            [.. null] => 0,
            [.. not null] => 0,
        };

        _ = this switch // no tests for null slice
        {
            null => 0,
            [] => 0,
            [.. not null] => 0,
        };

        _ = this switch // no test for not null slice
        {
            null => 0,
            [] => 0,
            [.. null] => 0,
        };

        _ = this switch
        {
            null => 0,
            [] => 0,
            [.. not null] => 0,
            [..] => 0,
        };

        _ = this switch
        {
            null => 0,
            [] => 0,
            [.. null] => 0,
            [..] => 0,
        };

        _ = this switch
        {
            null => 0,
            [] => 0,
            [null, .. null] => 0,
            [..] => 0,
        };
    }
}
";
        // PROTOTYPE: incorrect exhaustiveness examples from explainer
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // (20,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch // no tests for null slice
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("_").WithLocation(20, 18),
            // (27,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch // no test for not null slice
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(27, 18)
            );
    }

    [Fact]
    public void SlicePattern_Nullability_Exhaustiveness_NestedSlice()
    {
        var source = @"
#nullable enable
using System;
class C
{
    public int Length => throw null!;
    public object? this[Index i] => throw null!;
    public C? this[Range r] => throw null!;

    public void M()
    {
        _ = this switch
        {
            null => 0,
            [] => 0,
            [.. [null]] => 0,
            [not null] => 0,
            { Length: > 1 } => 0,
        };

        _ = this switch // didn't test for not null first element
        {
            null => 0,
            [] => 0,
            [.. [null]] => 0,
            { Length: > 1 } => 0,
        };

        _ = this switch // didn't test for null first element
        {
            null => 0,
            [] => 0,
            [.. [not null]] => 0,
            { Length: > 1 } => 0,
        };
    }
}
";
        // PROTOTYPE: unexpected exhaustiveness diagnostic
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // (12,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("_").WithLocation(12, 18),
            // (21,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch // didn't test for not null first element
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(21, 18),
            // (29,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch // didn't test for null first element
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("_").WithLocation(29, 18)
            );
    }

    [Fact]
    public void ListPattern_Dynamic()
    {
        var source = @"
class C
{
    void M(dynamic d)
    {
        _ = d is [_];
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (6,18): error CS9200: List patterns may not be used for a value of type 'dynamic'.
            //         _ = d is [_];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[_]").WithArguments("dynamic").WithLocation(6, 18)
            );
    }

    [Fact]
    public void ListPattern_UseSiteErrorOnIndexAndRangeIndexers()
    {
        var missing_cs = @"
public class Missing
{
}
";
        var missingRef = CreateCompilation(missing_cs, assemblyName: "missing")
            .EmitToImageReference();

        var lib2_cs = @"
using System;
public class C
{
    public int Length => 0;
    public Missing this[Index i] => throw null;
    public Missing this[Range r] => throw null;
}
";
        var lib2Ref = CreateCompilation(new[] { lib2_cs, TestSources.Index, TestSources.Range }, references: new[] { missingRef })
            .EmitToImageReference();

        var source = @"
class D
{
    void M(C c)
    {
        _ = c is [var item];
        _ = c is [..var rest];
        var index = c[^1];
        var range = c[1..^1];
    }
}
";
        var compilation = CreateCompilation(source, references: new[] { lib2Ref });
        compilation.VerifyEmitDiagnostics(
            // (6,18): error CS9200: List patterns may not be used for a value of type 'C'.
            //         _ = c is [var item];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[var item]").WithArguments("C").WithLocation(6, 18),
            // (7,18): error CS9200: List patterns may not be used for a value of type 'C'.
            //         _ = c is [..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[..var rest]").WithArguments("C").WithLocation(7, 18),
            // (7,19): error CS9201: Slice patterns may not be used for a value of type 'C'.
            //         _ = c is [..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..var rest").WithArguments("C").WithLocation(7, 19),
            // (8,21): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         var index = c[^1];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 21),
            // (9,21): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         var range = c[1..^1];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[1..^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(9, 21)
            );
    }

    [Fact]
    public void ListPattern_RefParameters()
    {
        var source = @"
class C
{
    public int Length => 0;
    public int this[ref int i] => 0;
    public int Slice(ref int i, ref int j) => 0;

    void M()
    {
        _ = this is [var item, ..var rest];
        _ = this[^1];
        _ = this[1..^1];
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (5,21): error CS0631: ref and out are not valid in this context
            //     public int this[ref int i] => 0;
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(5, 21),
            // (10,21): error CS9200: List patterns may not be used for a value of type 'C'.
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[var item, ..var rest]").WithArguments("C").WithLocation(10, 21),
            // (10,32): error CS9201: Slice patterns may not be used for a value of type 'C'.
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..var rest").WithArguments("C").WithLocation(10, 32),
            // (11,18): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "^1").WithArguments("System.Index").WithLocation(11, 18),
            // (11,18): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^1").WithArguments("System.Index", ".ctor").WithLocation(11, 18),
            // (12,18): error CS0518: Predefined type 'System.Range' is not defined or imported
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1..^1").WithArguments("System.Range").WithLocation(12, 18),
            // (12,18): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1").WithArguments("System.Index").WithLocation(12, 18),
            // (12,21): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "^1").WithArguments("System.Index").WithLocation(12, 21),
            // (12,21): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^1").WithArguments("System.Index", ".ctor").WithLocation(12, 21),
            // (12,21): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "^1").WithArguments("System.Index").WithLocation(12, 21)
            );
    }

    [Fact]
    public void ListPattern_RefParametersInIndexAndRangeIndexers()
    {
        var source = @"
using System;
class C
{
    public int Length => 0;
    public int this[ref Index i] => 0;
    public int this[ref Range r] => 0;

    void M()
    {
        _ = this is [var item, ..var rest];
        _ = this[^1];
        _ = this[1..^1];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // (6,21): error CS0631: ref and out are not valid in this context
            //     public int this[ref Index i] => 0;
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(6, 21),
            // (7,21): error CS0631: ref and out are not valid in this context
            //     public int this[ref Range r] => 0;
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(7, 21),
            // (11,21): error CS9200: List patterns may not be used for a value of type 'C'.
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[var item, ..var rest]").WithArguments("C").WithLocation(11, 21),
            // (11,32): error CS9201: Slice patterns may not be used for a value of type 'C'.
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..var rest").WithArguments("C").WithLocation(11, 32),
            // (12,18): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_BadArgRef, "^1").WithArguments("1", "ref").WithLocation(12, 18),
            // (13,18): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_BadArgRef, "1..^1").WithArguments("1", "ref").WithLocation(13, 18)
            );
    }

    [Fact]
    public void ListPattern_InParameters()
    {
        var source = @"
class C
{
    public int Length => 0;
    public int this[in int i] => 0;
    public int Slice(in int i, in int j) => 0;

    void M()
    {
        _ = this is [var item, ..var rest];
        _ = this[^1];
        _ = this[1..^1];
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (10,21): error CS9200: List patterns may not be used for a value of type 'C'.
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[var item, ..var rest]").WithArguments("C").WithLocation(10, 21),
            // (10,32): error CS9201: Slice patterns may not be used for a value of type 'C'.
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..var rest").WithArguments("C").WithLocation(10, 32),
            // (11,18): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "^1").WithArguments("System.Index").WithLocation(11, 18),
            // (11,18): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^1").WithArguments("System.Index", ".ctor").WithLocation(11, 18),
            // (12,18): error CS0518: Predefined type 'System.Range' is not defined or imported
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1..^1").WithArguments("System.Range").WithLocation(12, 18),
            // (12,18): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1").WithArguments("System.Index").WithLocation(12, 18),
            // (12,21): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "^1").WithArguments("System.Index").WithLocation(12, 21),
            // (12,21): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^1").WithArguments("System.Index", ".ctor").WithLocation(12, 21),
            // (12,21): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "^1").WithArguments("System.Index").WithLocation(12, 21)
            );
    }

    [Fact]
    public void ListPattern_InParametersInIndexAndRangeIndexers()
    {
        var source = @"
new C().M();

public class C
{
    public int Length => 2;
    public string this[in System.Index i] => ""item value"";
    public string this[in System.Range r] => ""rest value"";

    public void M()
    {
        if (this is [var item, ..var rest])
        {
            System.Console.Write((item, rest));
        }
    }

    void M2()
    {
        _ = this[^1];
        _ = this[1..^1];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics();
        var verifier = CompileAndVerify(compilation, expectedOutput: "(item value, rest value)");

        verifier.VerifyIL("C.M", @"
{
  // Code size       79 (0x4f)
  .maxstack  4
  .locals init (string V_0, //item
                string V_1, //rest
                C V_2,
                System.Index V_3,
                System.Range V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.2
  IL_0002:  ldloc.2
  IL_0003:  brfalse.s  IL_004e
  IL_0005:  ldloc.2
  IL_0006:  callvirt   ""int C.Length.get""
  IL_000b:  ldc.i4.1
  IL_000c:  blt.s      IL_004e
  IL_000e:  ldloc.2
  IL_000f:  ldc.i4.0
  IL_0010:  ldc.i4.0
  IL_0011:  newobj     ""System.Index..ctor(int, bool)""
  IL_0016:  stloc.3
  IL_0017:  ldloca.s   V_3
  IL_0019:  callvirt   ""string C.this[in System.Index].get""
  IL_001e:  stloc.0
  IL_001f:  ldloc.2
  IL_0020:  ldc.i4.1
  IL_0021:  ldc.i4.0
  IL_0022:  newobj     ""System.Index..ctor(int, bool)""
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.1
  IL_0029:  newobj     ""System.Index..ctor(int, bool)""
  IL_002e:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0033:  stloc.s    V_4
  IL_0035:  ldloca.s   V_4
  IL_0037:  callvirt   ""string C.this[in System.Range].get""
  IL_003c:  stloc.1
  IL_003d:  ldloc.0
  IL_003e:  ldloc.1
  IL_003f:  newobj     ""System.ValueTuple<string, string>..ctor(string, string)""
  IL_0044:  box        ""System.ValueTuple<string, string>""
  IL_0049:  call       ""void System.Console.Write(object)""
  IL_004e:  ret
}
");
    }

    [Fact]
    public void ListPattern_ImplicitlyConvertibleFromIndexAndRange()
    {
        var source = @"
new C().M();

public class MyIndex
{
    public static implicit operator MyIndex(System.Index i) => new MyIndex();
}

public class MyRange
{
    public static implicit operator MyRange(System.Range i) => new MyRange();
}

public class C
{
    public int Length => 2;
    public string this[MyIndex i] => ""item value"";
    public string this[MyRange r] => ""rest value"";

    public void M()
    {
        if (this is [var item, ..var rest])
        {
            System.Console.Write((item, rest));
        }
    }

    void M2()
    {
        _ = this[^1];
        _ = this[1..^1];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics();
        var verifier = CompileAndVerify(compilation, expectedOutput: "(item value, rest value)");

        verifier.VerifyIL("C.M", @"
{
  // Code size       82 (0x52)
  .maxstack  4
  .locals init (string V_0, //item
                string V_1, //rest
                C V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.2
  IL_0002:  ldloc.2
  IL_0003:  brfalse.s  IL_0051
  IL_0005:  ldloc.2
  IL_0006:  callvirt   ""int C.Length.get""
  IL_000b:  ldc.i4.1
  IL_000c:  blt.s      IL_0051
  IL_000e:  ldloc.2
  IL_000f:  ldc.i4.0
  IL_0010:  ldc.i4.0
  IL_0011:  newobj     ""System.Index..ctor(int, bool)""
  IL_0016:  call       ""MyIndex MyIndex.op_Implicit(System.Index)""
  IL_001b:  callvirt   ""string C.this[MyIndex].get""
  IL_0020:  stloc.0
  IL_0021:  ldloc.2
  IL_0022:  ldc.i4.1
  IL_0023:  ldc.i4.0
  IL_0024:  newobj     ""System.Index..ctor(int, bool)""
  IL_0029:  ldc.i4.0
  IL_002a:  ldc.i4.1
  IL_002b:  newobj     ""System.Index..ctor(int, bool)""
  IL_0030:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0035:  call       ""MyRange MyRange.op_Implicit(System.Range)""
  IL_003a:  callvirt   ""string C.this[MyRange].get""
  IL_003f:  stloc.1
  IL_0040:  ldloc.0
  IL_0041:  ldloc.1
  IL_0042:  newobj     ""System.ValueTuple<string, string>..ctor(string, string)""
  IL_0047:  box        ""System.ValueTuple<string, string>""
  IL_004c:  call       ""void System.Console.Write(object)""
  IL_0051:  ret
}
");
    }

    [Fact]
    public void ListPattern_ExpressionTree()
    {
        var source = @"
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
class C
{
    void M(int[] array)
    {
        Expression<Func<bool>> ok1 = () => array is [_, ..];
    }
}
";
        var compilation = CreateCompilation(source);
        compilation.VerifyEmitDiagnostics(
            // (9,44): error CS8122: An expression tree may not contain an 'is' pattern-matching operator.
            //         Expression<Func<bool>> ok1 = () => array is [_, ..];
            Diagnostic(ErrorCode.ERR_ExpressionTreeContainsIsMatch, "array is [_, ..]").WithLocation(9, 44)
            );
    }

    [Fact]
    public void RealIndexersPreferredToPattern()
    {
        var src = @"
using System;
class C
{
    public int Count => 2;

    public int this[int i] => throw null;
    public int this[Index i] { get { Console.Write(""Index ""); return 42; } }

    public int Slice(int i, int j) => throw null;
    public int this[Range r] { get { Console.Write(""Range ""); return 43; } }

    static void Main()
    {
        if (new C() is [var x, .. var y])
            Console.Write((x, y));
    }
}";
        CompileAndVerify(new[] { src, TestSources.Index, TestSources.Range }, expectedOutput: "Index Range (42, 43)");
    }

    [Fact]
    public void SlicePattern_ExtensionIgnored()
    {
        var src = @"
_ = new C() is [..var y];

static class Extensions
{
    public static int Slice(this C c, int i, int j) => throw null;
}
class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
}";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,17): error CS9201: Slice patterns may not be used for a value of type 'C'.
            // _ = new C() is [..var y];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForSlicePattern, "..var y").WithArguments("C").WithLocation(2, 17)
            );
    }

    [Fact]
    public void SlicePattern_String()
    {
        var src = @"
if (""abc"" is [var first, ..var rest])
{
    System.Console.Write((first, rest).ToString());
}
";
        CompileAndVerify(src, expectedOutput: "(a, bc)");
    }

    [Fact]
    public void ListPattern_Exhaustiveness_Count()
    {
        var src = @"
_ = new C() switch // 1
{
    { Count: 0 } => 0,
    [_] => 1,
    // missing
};

_ = new C() switch // 2
{
    { Count: 0 } => 0,
    // missing
    { _, _, .. } => 2,
};

_ = new C() switch // 3
{
    { Count: 0 } => 0,
    // missing
    [_, _] => 2,
    [_, _, ..] => 3,
};

_ = new C() switch
{
    { Count: 0 } => 0,
    { Count: 1 } => 1,
    [_, _] => 2,
    { Count: > 2 } => 3,
};

class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
}";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 2 }' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 2 }").WithLocation(2, 13),
            // (13,7): error CS8503: A property subpattern requires a reference to the property or field to be matched, e.g. '{ Name: _ }'
            //     { _, _, .. } => 2,
            Diagnostic(ErrorCode.ERR_PropertyPatternNameMissing, "_").WithArguments("_").WithLocation(13, 7),
            // (16,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            // _ = new C() switch // 3
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(16, 13)
            );
    }

    [Fact]
    public void ListPattern_Exhaustiveness_FirstPosition()
    {
        var src = @"
_ = new C() switch // 1
{
    [> 0] => 1,
    [< 0] => 2,
};

_ = new C() switch // 2
{
    [> 0] => 1,
    [< 0] => 2,
    { Count: 0 or > 1 } => 3,
};

_ = new C() switch
{
    [> 0] => 1,
    [< 0] => 2,
    { Count: 0 or > 1 } => 3,
    [0] => 4,
};

class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
}";
        // PROTOTYPE bad explanation for 2
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 0 }' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 0 }").WithLocation(2, 13),
            // (8,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = new C() switch // 2
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(8, 13)
            );
    }

    [Fact]
    public void ListPattern_Exhaustiveness_FirstPosition_Nullability()
    {
        var src = @"
#nullable enable
_ = new C() switch // 1
{
    null => 0,
    [not null] => 1,
    { Count: 0 or > 1 } => 2,
};

_ = new C() switch
{
    null => 0,
    [not null] => 1,
    [null] => 2,
    { Count: 0 or > 1 } => 3,
};

_ = new C() switch // 2
{
    [not null] => 1,
    { Count: 0 or > 1 } => 2,
};

_ = new C() switch
{
    [not null] => 1,
    [null] => 2,
    { Count: 0 or > 1 } => 3,
};

class C
{
    public int Count => throw null!;
    public string? this[int i] => throw null!;
}";
        // PROTOTYPE bad explanations on 1 and 2
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,13): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("_").WithLocation(3, 13),
            // (18,13): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
            // _ = new C() switch // 2
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(18, 13)
            );
    }

    [Fact]
    public void ListPattern_Exhaustiveness_SecondPosition()
    {
        var src = @"
_ = new C() switch // 1
{
    [_, > 0] => 1,
    [_, < 0] => 2,
    { Count: <= 1 or > 2 } => 3,
};

class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
}";
        // PROTOTYPE bad explanation
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(2, 13)
            );
    }

    [Fact]
    public void ListPattern_Exhaustiveness_SecondToLastPosition()
    {
        var src = @"
_ = new C() switch // 1
{
    [.., > 0, _] => 1,
    [.., < 0, _] => 2,
    { Count: <= 1 } => 3,
};

class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
}";
        // PROTOTYPE bad explanation
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(2, 13)
            );
    }

    [Fact]
    public void ListPattern_Exhaustiveness_LastPosition()
    {
        var src = @"
_ = new C() switch // 1
{
    [.., > 0] => 1,
    [.., < 0] => 2,
    { Count: 0 } => 3,
};

class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
}";
        // PROTOTYPE bad explanation
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(2, 13)
            );
    }

    [Fact]
    public void ListPattern_Exhaustiveness_StartAndEndPatternsOverlap()
    {
        var src = @"
_ = new C() switch
{
    [.., >= 0] => 1,
    [< 0] => 2,
    { Count: 0 or > 1 } => 3,
};

class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
}";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ListPattern_Exhaustiveness_NestedSlice()
    {
        var src = @"
_ = new C() switch
{
    [>= 0] => 1,
    [..[< 0]] => 2,
    { Count: 0 or > 1 } => 3,
};

class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
    public C Slice(int i, int j) => throw null;
}";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ListPattern_UintCount()
    {
        var src = @"
_ = new C() switch // 1
{
    [..] => 1,
};

_ = new C()[^1]; // 2

class C
{
    public uint Count => throw null!;
    public int this[int i] => throw null!;
}";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,5): error CS9200: List patterns may not be used for a value of type 'C'.
            //     [..] => 1,
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[..]").WithArguments("C").WithLocation(4, 5),
            // (7,13): error CS0518: Predefined type 'System.Index' is not defined or imported
            // _ = new C()[^1]; // 2
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "^1").WithArguments("System.Index").WithLocation(7, 13),
            // (7,13): error CS0656: Missing compiler required member 'System.Index..ctor'
            // _ = new C()[^1]; // 2
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^1").WithArguments("System.Index", ".ctor").WithLocation(7, 13)
            );
    }

    [Fact]
    public void ListPattern_NintCount()
    {
        var src = @"
_ = new C() switch // 1
{
    [..] => 1,
};

_ = new C()[^1]; // 2, 3

class C
{
    public nint Count => throw null!;
    public int this[int i] => throw null!;
}";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,5): error CS9200: List patterns may not be used for a value of type 'C'.
            //     [..] => 1,
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[..]").WithArguments("C").WithLocation(4, 5),
            // (7,13): error CS0518: Predefined type 'System.Index' is not defined or imported
            // _ = new C()[^1]; // 2, 3
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "^1").WithArguments("System.Index").WithLocation(7, 13),
            // (7,13): error CS0656: Missing compiler required member 'System.Index..ctor'
            // _ = new C()[^1]; // 2, 3
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^1").WithArguments("System.Index", ".ctor").WithLocation(7, 13)
            );
    }

    [Fact]
    public void Subsumption_01()
    {
        var src = @"
class C
{
    void Test(int[] a)
    {
        switch (a)
        {
            case [..,42]:
            case [42]:
                break;
        }
    }
}";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics(
                // (9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [42]:
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[42]").WithLocation(9, 18)
                );

        VerifyDecisionDagDump<SwitchStatementSyntax>(comp,
@"[0]: t0 != null ? [1] : [9]
[1]: t1 = t0.Length; [2]
[2]: t1 >= 1 ? [3] : [9]
[3]: t2 = t0[-1]; [4]
[4]: t2 == 42 ? [5] : [6]
[5]: leaf `case [..,42]:`
[6]: t1 == 1 ? [7] : [9]
[7]: t3 = t0[0]; [8]
[8]: t3 <-- t2; [9]
[9]: leaf <break> `switch (a)
        {
            case [..,42]:
            case [42]:
                break;
        }`
");
    }

    [Fact]
    public void Subsumption_02()
    {
        var src = @"
class C
{
    void Test(int[] a, int[] b)
    {
        switch (a, b)
        {
            case ([.., 42], [.., 43]):
            case ([42], [43]):
                break;
        }
    }
}";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics(
            // (9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case ([42], [43]):
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "([42], [43])").WithLocation(9, 18));

        VerifyDecisionDagDump<SwitchStatementSyntax>(comp,
@"[0]: t1 = t0.a; [1]
[1]: t1 != null ? [2] : [22]
[2]: t2 = t1.Length; [3]
[3]: t2 >= 1 ? [4] : [22]
[4]: t3 = t1[-1]; [5]
[5]: t3 == 42 ? [6] : [19]
[6]: t4 = t0.b; [7]
[7]: t4 != null ? [8] : [22]
[8]: t5 = t4.Length; [9]
[9]: t5 >= 1 ? [10] : [22]
[10]: t6 = t4[-1]; [11]
[11]: t6 == 43 ? [12] : [13]
[12]: leaf `case ([.., 42], [.., 43]):`
[13]: t2 == 1 ? [14] : [22]
[14]: t7 = t1[0]; [15]
[15]: t7 <-- t3; [16]
[16]: t5 == 1 ? [17] : [22]
[17]: t9 = t4[0]; [18]
[18]: t9 <-- t6; [22]
[19]: t2 == 1 ? [20] : [22]
[20]: t7 = t1[0]; [21]
[21]: t7 <-- t3; [22]
[22]: leaf <break> `switch (a, b)
        {
            case ([.., 42], [.., 43]):
            case ([42], [43]):
                break;
        }`
");
    }

    [Fact]
    public void Subsumption_03()
    {
        var src = @"
class C
{
    void Test(int[] a)
    {
        switch (a)
        {
            case { Length: 1 } and [.., 1]:
            case { Length: 1 } and [1, ..]:
                break;
        }
        switch (a)
        {
            case { Length: 1 } and [1, ..]:
            case { Length: 1 } and [.., 1]:
                break;
        }
    }
}";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics(
                // (9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case { Length: 1 } and [1, ..]:
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "{ Length: 1 } and [1, ..]").WithLocation(9, 18),
                // (15,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case { Length: 1 } and [.., 1]:
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "{ Length: 1 } and [.., 1]").WithLocation(15, 18)
                );
    }

    [Fact]
    public void Subsumption_04()
    {
        var src = @"
class C
{
    void Test(int[] a)
    {
        switch (a)
        {
            case [1, .., 3]:
            case [1, 2, 3]:
                break;
        }
    }
}";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics(
            // (9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case [1, 2, 3]:
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[1, 2, 3]").WithLocation(9, 18)
            );

        VerifyDecisionDagDump<SwitchStatementSyntax>(comp,
@"[0]: t0 != null ? [1] : [13]
[1]: t1 = t0.Length; [2]
[2]: t1 >= 2 ? [3] : [13]
[3]: t2 = t0[0]; [4]
[4]: t2 == 1 ? [5] : [13]
[5]: t3 = t0[-1]; [6]
[6]: t3 == 3 ? [7] : [8]
[7]: leaf `case [1, .., 3]:`
[8]: t1 == 3 ? [9] : [13]
[9]: t4 = t0[1]; [10]
[10]: t4 == 2 ? [11] : [13]
[11]: t5 = t0[2]; [12]
[12]: t5 <-- t3; [13]
[13]: leaf <break> `switch (a)
        {
            case [1, .., 3]:
            case [1, 2, 3]:
                break;
        }`
");
    }

    [Fact]
    public void Subsumption_05()
    {
        var src = @"
using System;
class C
{
    static int Test(int[] a)
    {
        switch (a)
        {
            case [1, 2, 3]: return 1;
            case [1, .., 3]: return 2;
            default: return 3;
        }
    }
    static void Main()
    {
        Console.WriteLine(Test(new[]{1,2,3}));
        Console.WriteLine(Test(new[]{1,0,3}));
        Console.WriteLine(Test(new[]{1,2,0}));
    }
}";
        var expectedOutput = @"
1
2
3
";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: expectedOutput);

        VerifyDecisionDagDump<SwitchStatementSyntax>(comp,
@"[0]: t0 != null ? [1] : [18]
[1]: t1 = t0.Length; [2]
[2]: t1 == 3 ? [3] : [12]
[3]: t2 = t0[0]; [4]
[4]: t2 == 1 ? [5] : [18]
[5]: t3 = t0[1]; [6]
[6]: t3 == 2 ? [7] : [15]
[7]: t4 = t0[2]; [8]
[8]: t4 == 3 ? [9] : [10]
[9]: leaf `case [1, 2, 3]:`
[10]: t5 = t0[-1]; [11]
[11]: t5 <-- t4; [18]
[12]: t1 >= 2 ? [13] : [18]
[13]: t2 = t0[0]; [14]
[14]: t2 == 1 ? [15] : [18]
[15]: t5 = t0[-1]; [16]
[16]: t5 == 3 ? [17] : [18]
[17]: leaf `case [1, .., 3]:`
[18]: leaf `default`
");
    }

    [Fact]
    public void Subsumption_06()
    {
        var src = @"
using System;
class C
{
    static int Test(int[] a)
    {
        switch (a)
        {
            case [42]: return 1;
            case [..,42]: return 2;
            default: return 3;
        }
    }
    static void Main()
    {
        Console.WriteLine(Test(new[]{42}));
        Console.WriteLine(Test(new[]{42, 42}));
        Console.WriteLine(Test(new[]{42, 43}));
    }
}";
        var expectedOutput = @"
1
2
3
";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: expectedOutput);

        VerifyDecisionDagDump<SwitchStatementSyntax>(comp,
@"[0]: t0 != null ? [1] : [12]
[1]: t1 = t0.Length; [2]
[2]: t1 == 1 ? [3] : [8]
[3]: t2 = t0[0]; [4]
[4]: t2 == 42 ? [5] : [6]
[5]: leaf `case [42]:`
[6]: t3 = t0[-1]; [7]
[7]: t3 <-- t2; [12]
[8]: t1 >= 1 ? [9] : [12]
[9]: t3 = t0[-1]; [10]
[10]: t3 == 42 ? [11] : [12]
[11]: leaf `case [..,42]:`
[12]: leaf `default`
");
    }

    [Fact]
    public void Subsumption_07()
    {
        var src = @"
class C
{
    void Test(int[] a)
    {
        switch (a)
        {
            case [>0, ..]:
            case [.., <=0]:
            case [var unreachable]:
                    break;
        }
    }
}";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics(
            // (10,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case [var unreachable]:
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[var unreachable]").WithLocation(10, 18));
    }

    [Fact]
    public void Subsumption_08()
    {
        var src = @"
class C
{
    void Test(object[] a)
    {
        switch (a)
        {
            case [null, ..]:
            case [.., not null]:
            case [var unreachable]:
                    break;
        }
        switch (a)
        {
            case [string, ..]:
            case [.., not string]:
            case [var unreachable]:
                    break;
        }
    }
}";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics(
            // (10,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case [var unreachable]:
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[var unreachable]").WithLocation(10, 18),
            // (17,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case [var unreachable]:
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[var unreachable]").WithLocation(17, 18));

        VerifyDecisionDagDump<SwitchStatementSyntax>(comp,
@"[0]: t0 != null ? [1] : [10]
[1]: t1 = t0.Length; [2]
[2]: t1 >= 1 ? [3] : [10]
[3]: t2 = t0[0]; [4]
[4]: t2 == null ? [5] : [6]
[5]: leaf `case [null, ..]:`
[6]: t3 = t0[-1]; [7]
[7]: t1 == 1 ? [8] : [9]
[8]: t3 <-- t2; [11]
[9]: t3 == null ? [10] : [11]
[10]: leaf <break> `switch (a)
        {
            case [null, ..]:
            case [.., not null]:
            case [var unreachable]:
                    break;
        }`
[11]: leaf `case [.., not null]:`
");
    }

    [Fact]
    public void Subsumption_09()
    {
        var src = @"
C.Test(new[] { 42, -1, 0, 42 });

class C
{
    public static void Test(int[] a)
    {
        switch (a)
        {
            case [_, > 0, ..]: 
                System.Console.Write(1);
                break;
            case [.., <= 0, _]: 
                System.Console.Write(2);
                break;
            default:
                System.Console.Write(3);
                break;
        }
    }
}";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "2");

        VerifyDecisionDagDump<SwitchStatementSyntax>(comp,
@"[0]: t0 != null ? [1] : [11]
[1]: t1 = t0.Length; [2]
[2]: t1 >= 2 ? [3] : [11]
[3]: t2 = t0[1]; [4]
[4]: t2 > 0 ? [5] : [6]
[5]: leaf `case [_, > 0, ..]:`
[6]: t3 = t0[-2]; [7]
[7]: t1 == 3 ? [8] : [9]
[8]: t3 <-- t2; [10]
[9]: t3 <= 0 ? [10] : [11]
[10]: leaf `case [.., <= 0, _]:`
[11]: leaf `default`
");
    }

    [Fact]
    public void Subsumption_10()
    {
        var src = @"
class C
{
    public int X { get; }
    public int Y { get; }
    public C F { get; }
    static void Test(C[] a) 
    {
        switch (a)
        {
            case [.., {X:> 0, Y:0}]:
            case [{Y:0, X:> 0}]:
                break;
        }
        switch (a)
        {
            case [.., {X:> 0, F.Y: 0}]:
            case [..[{F.Y: 0, X:> 0}]]:
                break;
        }
    }
}
";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics(
            // (12,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case [{Y:0, X:> 0}]:
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[{Y:0, X:> 0}]").WithLocation(12, 18),
            // (18,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case [..[{F.Y: 0, X:> 0}]]:
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[..[{F.Y: 0, X:> 0}]]").WithLocation(18, 18)
            );
    }

    [Fact]
    public void Subsumption_11()
    {
        var src = @"
class C
{
    public int X { get; }
    public int Y { get; }

    public void Deconstruct(out C c1, out C c2) => throw null;

    static void Test(C[] a) 
    {
        switch (a)
        {
            case [.., (_, { X: 0 })]:
            case [({ X: 0 }, _)]: // ok
                break;
        }
        switch (a)
        {
            case [.., (_, { X: 0 })]:
            case [(_, { X: 0 })]: // err
                break;
        }
    }
}
";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics(
            // (20,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case [(_, { X: 0 })]: // err
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[(_, { X: 0 })]").WithLocation(20, 18));
    }

    [Fact]
    public void Subsumption_12()
    {
        var src = @"
class C
{
    void Test(int[] a)
    {
        _ = a switch
        {
            { Length: not 1 }  => 0,
            [<0, ..] => 0,
            [..[>= 0]] or [..null] => 1,
            [_] => 2, // unreachable
        };
        _ = a switch // exhaustive
        {
            { Length: not 1 }  => 0,
            [<0, ..] => 0,
            [..[>= 0]] => 1,
            [_] => 2,
        };
    }
}" + TestSources.GetSubArray;
        var comp = CreateCompilationWithIndexAndRange(src);
        comp.VerifyEmitDiagnostics(
            // (11,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //             [_] => 2, // unreachable
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "[_]").WithLocation(11, 13));

        VerifyDecisionDagDump<SwitchExpressionSyntax>(comp,

@"[0]: t0 != null ? [1] : [13]
[1]: t1 = t0.Length; [2]
[2]: t1 == 1 ? [3] : [12]
[3]: t2 = t0[0]; [4]
[4]: t2 < 0 ? [5] : [6]
[5]: leaf <arm> `[<0, ..] => 0`
[6]: t3 = DagSliceEvaluation(t0); [7]
[7]: t3 != null ? [8] : [11]
[8]: t4 = t3.Length; [9]
[9]: t5 = t3[0]; [10]
[10]: t5 <-- t2; [11]
[11]: leaf <arm> `[..[>= 0]] or [..null] => 1`
[12]: leaf <arm> `{ Length: not 1 }  => 0`
[13]: leaf <default> `a switch
        {
            { Length: not 1 }  => 0,
            [<0, ..] => 0,
            [..[>= 0]] or [..null] => 1,
            [_] => 2, // unreachable
        }`
");
    }

    [Fact]
    public void Subsumption_13()
    {
        var src = @"
class C
{
    void Test(int[] a)
    {
        _ = a switch
        {
            [.., >0] => 1,
            [<0, ..] => 2,
            [0, ..] => 3,
            { Length: not 1 } => 4,
            [var unreachable] => 5,
        };
    }
}" + TestSources.GetSubArray;
        var comp = CreateCompilationWithIndexAndRange(src);
        comp.VerifyEmitDiagnostics(
            // (12,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //             [var unreachable] => 5,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "[var unreachable]").WithLocation(12, 13));

        VerifyDecisionDagDump<SwitchExpressionSyntax>(comp,
@"[0]: t0 != null ? [1] : [15]
[1]: t1 = t0.Length; [2]
[2]: t1 >= 1 ? [3] : [14]
[3]: t2 = t0[-1]; [4]
[4]: t2 > 0 ? [5] : [6]
[5]: leaf <arm> `[.., >0] => 1`
[6]: t3 = t0[0]; [7]
[7]: t1 == 1 ? [8] : [10]
[8]: t3 <-- t2; [9]
[9]: t3 < 0 ? [11] : [13]
[10]: t3 < 0 ? [11] : [12]
[11]: leaf <arm> `[<0, ..] => 2`
[12]: t3 == 0 ? [13] : [14]
[13]: leaf <arm> `[0, ..] => 3`
[14]: leaf <arm> `{ Length: not 1 } => 4`
[15]: leaf <default> `a switch
        {
            [.., >0] => 1,
            [<0, ..] => 2,
            [0, ..] => 3,
            { Length: not 1 } => 4,
            [var unreachable] => 5,
        }`
");
    }

    [Fact]
    public void Subsumption_14()
    {
        var src = @"
class C
{
    void Test(int[] a)
    {
        _ = a switch
        {
            [.., >=0, >0] => 0,
            [.., <0, >=0] => 0,
            [<=0, <0, ..] => 1,
            [>0, <=0, ..] => 1,
            [0, 0] => 1,
            { Length: not 2 }  => 2,
            [var unreachable, var unreachable2] => 3,
        };
    }
}";
        var comp = CreateCompilationWithIndexAndRange(src);
        comp.VerifyEmitDiagnostics(
            // (14,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //             [var unreachable, var unreachable2] => 3,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "[var unreachable, var unreachable2]").WithLocation(14, 13));
    }

    [Fact]
    public void Subsumption_15()
    {
        // testing the scenario where we have multiple preconditions for indexers to relate.
        var src = @"
class C
{
    void Test(int[][] a)
    {
        switch (a)
        {
            case [.., [.., 42]]:
            case [[42]]:
                break;
        };
    }
}";
        var comp = CreateCompilationWithIndexAndRange(src);
        comp.VerifyEmitDiagnostics(
            // (9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case [[42]]:
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[[42]]").WithLocation(9, 18));

        VerifyDecisionDagDump<SwitchStatementSyntax>(comp,
@"[0]: t0 != null ? [1] : [27]
[1]: t1 = t0.Length; [2]
[2]: t1 >= 1 ? [3] : [27]
[3]: t2 = t0[-1]; [4]
[4]: t2 != null ? [5] : [24]
[5]: t3 = t2.Length; [6]
[6]: t3 >= 1 ? [7] : [19]
[7]: t4 = t2[-1]; [8]
[8]: t4 == 42 ? [9] : [10]
[9]: leaf `case [.., [.., 42]]:`
[10]: t1 == 1 ? [11] : [27]
[11]: t5 = t0[0]; [12]
[12]: t5 <-- t2; [13]
[13]: t7 = t5.Length; [14]
[14]: t7 <-- t3; [15]
[15]: t7 == 1 ? [16] : [27]
[16]: t9 = t5[0]; [17]
[17]: t3 <-- t7; [18]
[18]: t9 <-- t4; [27]
[19]: t1 == 1 ? [20] : [27]
[20]: t5 = t0[0]; [21]
[21]: t5 <-- t2; [22]
[22]: t7 = t5.Length; [23]
[23]: t7 <-- t3; [27]
[24]: t1 == 1 ? [25] : [27]
[25]: t5 = t0[0]; [26]
[26]: t5 <-- t2; [27]
[27]: leaf <break> `switch (a)
        {
            case [.., [.., 42]]:
            case [[42]]:
                break;
        }`
");
    }

    [Fact]
    public void Subsumption_16()
    {
        var src = @"
using System;
class C
{
    static int Test(int[][] a)
    {
        switch (a)
        {
            case [.., [.., <0]]:
                return 1;
            case [[>=0]]:
                return 2;
        }
        return -1;
    }
    static void Main()
    {
        Console.WriteLine(Test(new[] { new[] { 0, -1 }}));
        Console.WriteLine(Test(new[] { new[] { 0 }, new[] { -1 }}));
        Console.WriteLine(Test(new[] { new[] { 0 }}));
    }
}";
        var expectedOutput = @"
1
1
2
";
        var comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory]
    [CombinatorialData]
    public void Subsumption_Slice_00(
        [CombinatorialValues(
                "[1,2,3]",
                "[1,2,3,..[]]",
                "[1,2,..[],3]",
                "[1,..[],2,3]",
                "[..[],1,2,3]",
                "[1,..[2,3]]",
                "[..[1,2],3]",
                "[..[1,2,3]]",
                "[..[..[1,2,3]]]",
                "[..[1,2,3,..[]]]",
                "[..[1,2,..[],3]]",
                "[..[1,..[],2,3]]",
                "[..[..[],1,2,3]]",
                "[..[1,..[2,3]]]",
                "[..[..[1,2],3]]",
                "[1, ..[2], 3]",
                "[1, ..[2, ..[3]]]",
                "[1, ..[2, ..[], 3]]")]
            string case1,
        [CombinatorialValues(
                "[1,2,3]",
                "[1,2,3,..[]]",
                "[1,2,..[],3]",
                "[1,..[],2,3]",
                "[..[],1,2,3]",
                "[1,..[2,3]]",
                "[..[1,2],3]",
                "[..[1,2,3]]",
                "[..[..[1,2,3]]]",
                "[..[1,2,3,..[]]]",
                "[..[1,2,..[],3]]",
                "[..[1,..[],2,3]]",
                "[..[..[],1,2,3]]",
                "[..[1,..[2,3]]]",
                "[..[..[1,2],3]]",
                "[1, ..[2], 3]",
                "[1, ..[2, ..[3]]]",
                "[1, ..[2, ..[], 3]]")]
            string case2)
    {
        var src = @"
using System;
class C
{
    void Test(Span<int> a)
    {
        switch (a)
        {
            case " + case1 + @":
            case " + case2 + @":
                break;
        }
    }
}";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics(
            // (10,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, case2).WithLocation(10, 18)
            );
    }

    [Fact]
    public void Subsumption_Slice_01()
    {
        var src = @"
class C
{
    public static void Test(System.Span<int> a)
    {
        switch (a)
        {
            case [var v]: break;
            case [..[var v]]: break;
        }
    }
}";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics(
            // (9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case [..[var v]]: break;
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[..[var v]]").WithLocation(9, 18));
    }

    [Fact]
    public void Exhaustiveness_01()
    {
        var src = @"
using System;
class C
{
    public int X = 0, Y = 0;
    public static void Test(Span<int> a, Span<C> b)
    {
        _ = a switch
        {
            [.., >=0] or [<0] or { Length: 0 or >1 } => 0
        };

        _ = a switch
        {
            [.., >=0] or [..[.., <0]] or [] => 0
        };

        _ = a switch
        {
            [..[>=0]] or [<0] or
            { Length: 0 or >1 } => 0
        };

        _ = a switch
        {
            [..[.., <0]] or [..] => 0
        };

        _ = a switch
        {
            [_, ..{ Length: < int.MaxValue - 1 }] or [] or { Length: int.MaxValue } => 0
        };

        _ = a switch
        {
            [..{ Length: <= int.MaxValue - 1 }, _] or []  => 0
        };

        _ = a switch
        {
            [_, ..{ Length: <= int.MaxValue - 2 }, _] or { Length: 0 or 1 } => 0
        };

        _ = b switch
        { 
            [.., { X: >=0, Y: >0 }] => 0,
            [.., { X: <0, Y: >=0 }] => 0,
            [{ X: <=0, Y: <0 }, ..] => 1,
            [{ X: >0, Y: <=0 }, ..] => 1,
            [{ X:0, Y:0 }] => 1,
            { Length: not 1 } => 0
        };
    }
}";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void Exhaustiveness_02()
    {
        var src = @"
using System;
class C
{
    public static void Test1(Span<int> a)
    {
        _ = a switch
        {
            [] => 1,
            [_] => 2,
            [_,..] => 3,
        };
    }
    public static void Test2(Span<int> a)
    {
        _ = a switch
        {
            { Length: 0 } => 1,
            { Length: 1 } => 2,
            { Length: >=1 } => 3,
        };
    }
}";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(src, parseOptions: TestOptions.RegularWithListPatterns);
        comp.VerifyEmitDiagnostics();
        var verifier = CompileAndVerify(comp);
        string expectedIl = @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""int System.Span<int>.Length.get""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  brfalse.s  IL_0011
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.1
  IL_000d:  beq.s      IL_0014
  IL_000f:  br.s       IL_0017
  IL_0011:  ldc.i4.1
  IL_0012:  pop
  IL_0013:  ret
  IL_0014:  ldc.i4.2
  IL_0015:  pop
  IL_0016:  ret
  IL_0017:  ldc.i4.3
  IL_0018:  pop
  IL_0019:  ret
}
";
        verifier.VerifyIL("C.Test1", expectedIl);
        verifier.VerifyIL("C.Test2", expectedIl);
    }

    [Fact]
    public void LengthPattern_NegativeLengthTest()
    {
        var src = @"
int[] a = null;
_ = a is { Length: -1 }; // 1
_ = a is { Length: -1 or 1 };
_ = a is { Length: -1 } or { Length: 1 };

_ = a switch // 2
{
    { Length: -1 } => 0, // 3
};

_ = a switch // 4
{
    { Length: -1 or 1 } => 0,
};

_ = a switch // 5
{
    { Length: -1 } or { Length: 1 } => 0,
};

_ = a switch // 6
{
    { Length: -1 } => 0, // 7
    { Length: 1 } => 0,
};
";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,5): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            // _ = a is { Length: -1 }; // 1
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is { Length: -1 }").WithArguments("int[]").WithLocation(3, 5),
            // (7,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = a switch // 2
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(7, 7),
            // (9,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     { Length: -1 } => 0, // 3
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ Length: -1 }").WithLocation(9, 5),
            // (12,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Length: 0 }' is not covered.
            // _ = a switch // 4
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Length: 0 }").WithLocation(12, 7),
            // (17,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Length: 0 }' is not covered.
            // _ = a switch // 5
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Length: 0 }").WithLocation(17, 7),
            // (22,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Length: 0 }' is not covered.
            // _ = a switch // 6
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Length: 0 }").WithLocation(22, 7),
            // (24,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     { Length: -1 } => 0, // 7
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ Length: -1 }").WithLocation(24, 5)
            );
    }

    [Fact]
    public void LengthPattern_NegativeNullHandling_WithNullHandling()
    {
        var src = @"
int[] a = null;
_ = a is null or { Length: -1 };

_ = a switch // 1
{
    null => 0,
    { Length: -1 } => 0, // 2
};
";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (5,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'not null' is not covered.
            // _ = a switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("not null").WithLocation(5, 7),
            // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     { Length: -1 } => 0, // 2
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ Length: -1 }").WithLocation(8, 5)
            );
    }

    [Fact]
    public void LengthPattern_NegativeNullHandling_DuplicateTest()
    {
        var src = @"
int[] a = null;
_ = a is { Length: -1 } or { Length: -1 };

_ = a switch
{
    { Length: -1 } => 1,
    { Length: -1 } => 2,
    _ => 3,
};
";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,5): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            // _ = a is { Length: -1 } or { Length: -1 };
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is { Length: -1 } or { Length: -1 }").WithArguments("int[]").WithLocation(3, 5),
            // (7,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     { Length: -1 } => 1,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ Length: -1 }").WithLocation(7, 5),
            // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     { Length: -1 } => 2,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ Length: -1 }").WithLocation(8, 5)
            );
    }

    [Fact]
    public void LengthPattern_NegativeRangeTest()
    {
        var src = @"
int[] a = null;
_ = a is { Length: < 0 }; // 1

_ = a switch // 2
{
    { Length: < 0 } => 0, // 3
};
";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (3,5): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            // _ = a is { Length: < 0 }; // 1
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is { Length: < 0 }").WithArguments("int[]").WithLocation(3, 5),
            // (5,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = a switch // 2
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(5, 7),
            // (7,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     { Length: < 0 } => 0, // 3
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ Length: < 0 }").WithLocation(7, 5)
            );
    }

    [Fact]
    public void LengthPattern_Switch_NegativeRangeTestByElimination()
    {
        var src = @"
int[] a = null;
_ = a switch
{
    { Length: 0 } => 1,
    { Length: <= 0 } => 2,
    _ => 3,
};
";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (6,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     { Length: <= 0 } => 2,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ Length: <= 0 }").WithLocation(6, 5)
            );
    }

    [Fact, WorkItem(51801, "https://github.com/dotnet/roslyn/issues/51801")]
    public void IndexerOverrideLacksAccessor()
    {
        var source = @"
#nullable enable
using System.Runtime.CompilerServices;

class Base
{
    public virtual object this[int i] { get { return 1; } set { } }
}

class C : Base
{
    public override object this[int i] { set { } }
    public int Length => 2;

    public string? Value { get; }

    public string M()
    {
        switch (this)
        {
            case [1, 1]:
                return Value;
            default:
                return Value;
        }
    }
}
";
        var verifier = CompileAndVerify(source, options: TestOptions.DebugDll);
        verifier.VerifyIL("C.M", @"
{
  // Code size      105 (0x69)
  .maxstack  2
  .locals init (C V_0,
                int V_1,
                object V_2,
                int V_3,
                object V_4,
                int V_5,
                C V_6,
                string V_7)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.s    V_6
  IL_0004:  ldloc.s    V_6
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_005c
  IL_000a:  ldloc.0
  IL_000b:  callvirt   ""int C.Length.get""
  IL_0010:  stloc.1
  IL_0011:  ldloc.1
  IL_0012:  ldc.i4.2
  IL_0013:  bne.un.s   IL_005c
  IL_0015:  ldloc.0
  IL_0016:  ldc.i4.0
  IL_0017:  callvirt   ""object Base.this[int].get""
  IL_001c:  stloc.2
  IL_001d:  ldloc.2
  IL_001e:  isinst     ""int""
  IL_0023:  brfalse.s  IL_005c
  IL_0025:  ldloc.2
  IL_0026:  unbox.any  ""int""
  IL_002b:  stloc.3
  IL_002c:  ldloc.3
  IL_002d:  ldc.i4.1
  IL_002e:  bne.un.s   IL_005c
  IL_0030:  ldloc.0
  IL_0031:  ldc.i4.1
  IL_0032:  callvirt   ""object Base.this[int].get""
  IL_0037:  stloc.s    V_4
  IL_0039:  ldloc.s    V_4
  IL_003b:  isinst     ""int""
  IL_0040:  brfalse.s  IL_005c
  IL_0042:  ldloc.s    V_4
  IL_0044:  unbox.any  ""int""
  IL_0049:  stloc.s    V_5
  IL_004b:  ldloc.s    V_5
  IL_004d:  ldc.i4.1
  IL_004e:  beq.s      IL_0052
  IL_0050:  br.s       IL_005c
  IL_0052:  ldarg.0
  IL_0053:  call       ""string C.Value.get""
  IL_0058:  stloc.s    V_7
  IL_005a:  br.s       IL_0066
  IL_005c:  ldarg.0
  IL_005d:  call       ""string C.Value.get""
  IL_0062:  stloc.s    V_7
  IL_0064:  br.s       IL_0066
  IL_0066:  ldloc.s    V_7
  IL_0068:  ret
}");
    }
}
