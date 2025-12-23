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
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[CompilerTrait(CompilerFeature.Patterns)]
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
  // Code size       71 (0x47)
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
  IL_000e:  ble.un.s   IL_0038
  IL_0010:  ldloca.s   V_3
  IL_0012:  ldc.i4.0
  IL_0013:  call       ""ref char System.Span<char>.this[int].get""
  IL_0018:  ldind.u2
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_3
  IL_001c:  ldc.i4.1
  IL_001d:  ldloc.s    V_4
  IL_001f:  ldc.i4.1
  IL_0020:  sub
  IL_0021:  ldc.i4.1
  IL_0022:  sub
  IL_0023:  call       ""System.Span<char> System.Span<char>.Slice(int, int)""
  IL_0028:  stloc.1
  IL_0029:  ldloca.s   V_3
  IL_002b:  ldloc.s    V_4
  IL_002d:  ldc.i4.1
  IL_002e:  sub
  IL_002f:  call       ""ref char System.Span<char>.this[int].get""
  IL_0034:  ldind.u2
  IL_0035:  stloc.2
  IL_0036:  br.s       IL_003a
  IL_0038:  ldc.i4.1
  IL_0039:  ret
  IL_003a:  ldloc.0
  IL_003b:  ldloc.2
  IL_003c:  bne.un.s   IL_0045
  IL_003e:  ldloc.1
  IL_003f:  call       ""bool X.Test(System.Span<char>)""
  IL_0044:  ret
  IL_0045:  ldc.i4.0
  IL_0046:  ret
}
"),
            () => verifier.VerifyIL("X.Test(char[])", @"
{
  // Code size       69 (0x45)
  .maxstack  4
  .locals init (char V_0, //first
                char[] V_1, //others
                char V_2, //last
                char[] V_3,
                int V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.3
  IL_0002:  ldloc.3
  IL_0003:  brfalse.s  IL_0043
  IL_0005:  ldloc.3
  IL_0006:  ldlen
  IL_0007:  conv.i4
  IL_0008:  stloc.s    V_4
  IL_000a:  ldloc.s    V_4
  IL_000c:  ldc.i4.1
  IL_000d:  ble.un.s   IL_0036
  IL_000f:  ldloc.3
  IL_0010:  ldc.i4.0
  IL_0011:  ldelem.u2
  IL_0012:  stloc.0
  IL_0013:  ldloc.3
  IL_0014:  ldc.i4.1
  IL_0015:  ldc.i4.0
  IL_0016:  newobj     ""System.Index..ctor(int, bool)""
  IL_001b:  ldc.i4.1
  IL_001c:  ldc.i4.1
  IL_001d:  newobj     ""System.Index..ctor(int, bool)""
  IL_0022:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0027:  call       ""char[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<char>(char[], System.Range)""
  IL_002c:  stloc.1
  IL_002d:  ldloc.3
  IL_002e:  ldloc.s    V_4
  IL_0030:  ldc.i4.1
  IL_0031:  sub
  IL_0032:  ldelem.u2
  IL_0033:  stloc.2
  IL_0034:  br.s       IL_0038
  IL_0036:  ldc.i4.1
  IL_0037:  ret
  IL_0038:  ldloc.0
  IL_0039:  ldloc.2
  IL_003a:  bne.un.s   IL_0043
  IL_003c:  ldloc.1
  IL_003d:  call       ""bool X.Test(char[])""
  IL_0042:  ret
  IL_0043:  ldc.i4.0
  IL_0044:  ret
}
"),
            () => verifier.VerifyIL("X.Test(string)", @"
{
  // Code size       68 (0x44)
  .maxstack  4
  .locals init (char V_0, //first
                string V_1, //others
                char V_2, //last
                string V_3,
                int V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.3
  IL_0002:  ldloc.3
  IL_0003:  brfalse.s  IL_0042
  IL_0005:  ldloc.3
  IL_0006:  callvirt   ""int string.Length.get""
  IL_000b:  stloc.s    V_4
  IL_000d:  ldloc.s    V_4
  IL_000f:  ldc.i4.1
  IL_0010:  ble.un.s   IL_0035
  IL_0012:  ldloc.3
  IL_0013:  ldc.i4.0
  IL_0014:  callvirt   ""char string.this[int].get""
  IL_0019:  stloc.0
  IL_001a:  ldloc.3
  IL_001b:  ldc.i4.1
  IL_001c:  ldloc.s    V_4
  IL_001e:  ldc.i4.1
  IL_001f:  sub
  IL_0020:  ldc.i4.1
  IL_0021:  sub
  IL_0022:  callvirt   ""string string.Substring(int, int)""
  IL_0027:  stloc.1
  IL_0028:  ldloc.3
  IL_0029:  ldloc.s    V_4
  IL_002b:  ldc.i4.1
  IL_002c:  sub
  IL_002d:  callvirt   ""char string.this[int].get""
  IL_0032:  stloc.2
  IL_0033:  br.s       IL_0037
  IL_0035:  ldc.i4.1
  IL_0036:  ret
  IL_0037:  ldloc.0
  IL_0038:  ldloc.2
  IL_0039:  bne.un.s   IL_0042
  IL_003b:  ldloc.1
  IL_003c:  call       ""bool X.Test(string)""
  IL_0041:  ret
  IL_0042:  ldc.i4.0
  IL_0043:  ret
}
")
            );
    }

    [Fact]
    [WorkItem(57731, "https://github.com/dotnet/roslyn/issues/57731")]
    public void ListPattern_Codegen()
    {
        var source = @"
public class X
{
    static int Test1(int[] x)
    {
        switch (x)
        {
            case [.., 1] and [1, ..]: return 0;
        }

        return 1;
    }
    static int Test2(int[] x)
    {
        switch (x)
        {
            case [2, ..] and [.., 1]: return 0;
        }

        return 3;
    }
    static int Test3(int[] x)
    {
        switch (x)
        {
            case [2, ..]: return 4;
            case [.., 1]: return 5;
        }

        return 3;
    }
    static int Test4(int[] x)
    {
        switch (x)
        {
            case [2, ..]: return 4;
            case [.., 1]: return 5;
            case [6, .., 7]: return 8;
        }

        return 3;
    }
}
";
        var verifier = CompileAndVerify(new[] { source, TestSources.Index, TestSources.Range }, parseOptions: TestOptions.RegularWithListPatterns,
            options: TestOptions.ReleaseDll);
        verifier.VerifyDiagnostics();
        AssertEx.Multiple(
            () => verifier.VerifyIL("X.Test1", @"
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_001b
  IL_0003:  ldarg.0
  IL_0004:  ldlen
  IL_0005:  conv.i4
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  blt.s      IL_001b
  IL_000b:  ldarg.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  sub
  IL_000f:  ldelem.i4
  IL_0010:  ldc.i4.1
  IL_0011:  bne.un.s   IL_001b
  IL_0013:  ldarg.0
  IL_0014:  ldc.i4.0
  IL_0015:  ldelem.i4
  IL_0016:  ldc.i4.1
  IL_0017:  bne.un.s   IL_001b
  IL_0019:  ldc.i4.0
  IL_001a:  ret
  IL_001b:  ldc.i4.1
  IL_001c:  ret
}"),
            () => verifier.VerifyIL("X.Test2", @"
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_001b
  IL_0003:  ldarg.0
  IL_0004:  ldlen
  IL_0005:  conv.i4
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  blt.s      IL_001b
  IL_000b:  ldarg.0
  IL_000c:  ldc.i4.0
  IL_000d:  ldelem.i4
  IL_000e:  ldc.i4.2
  IL_000f:  bne.un.s   IL_001b
  IL_0011:  ldarg.0
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.1
  IL_0014:  sub
  IL_0015:  ldelem.i4
  IL_0016:  ldc.i4.1
  IL_0017:  bne.un.s   IL_001b
  IL_0019:  ldc.i4.0
  IL_001a:  ret
  IL_001b:  ldc.i4.3
  IL_001c:  ret
}"),
            () => verifier.VerifyIL("X.Test3", @"
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_001f
  IL_0003:  ldarg.0
  IL_0004:  ldlen
  IL_0005:  conv.i4
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  blt.s      IL_001f
  IL_000b:  ldarg.0
  IL_000c:  ldc.i4.0
  IL_000d:  ldelem.i4
  IL_000e:  ldc.i4.2
  IL_000f:  beq.s      IL_001b
  IL_0011:  ldarg.0
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.1
  IL_0014:  sub
  IL_0015:  ldelem.i4
  IL_0016:  ldc.i4.1
  IL_0017:  beq.s      IL_001d
  IL_0019:  br.s       IL_001f
  IL_001b:  ldc.i4.4
  IL_001c:  ret
  IL_001d:  ldc.i4.5
  IL_001e:  ret
  IL_001f:  ldc.i4.3
  IL_0020:  ret
}"),
            () => verifier.VerifyIL("X.Test4", @"
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (int V_0,
              int V_1,
              int V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0031
  IL_0003:  ldarg.0
  IL_0004:  ldlen
  IL_0005:  conv.i4
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  blt.s      IL_0031
  IL_000b:  ldarg.0
  IL_000c:  ldc.i4.0
  IL_000d:  ldelem.i4
  IL_000e:  stloc.1
  IL_000f:  ldloc.1
  IL_0010:  ldc.i4.2
  IL_0011:  beq.s      IL_002b
  IL_0013:  ldarg.0
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.1
  IL_0016:  sub
  IL_0017:  ldelem.i4
  IL_0018:  stloc.2
  IL_0019:  ldloc.2
  IL_001a:  ldc.i4.1
  IL_001b:  beq.s      IL_002d
  IL_001d:  ldloc.0
  IL_001e:  ldc.i4.2
  IL_001f:  blt.s      IL_0031
  IL_0021:  ldloc.1
  IL_0022:  ldc.i4.6
  IL_0023:  bne.un.s   IL_0031
  IL_0025:  ldloc.2
  IL_0026:  ldc.i4.7
  IL_0027:  beq.s      IL_002f
  IL_0029:  br.s       IL_0031
  IL_002b:  ldc.i4.4
  IL_002c:  ret
  IL_002d:  ldc.i4.5
  IL_002e:  ret
  IL_002f:  ldc.i4.8
  IL_0030:  ret
  IL_0031:  ldc.i4.3
  IL_0032:  ret
}
")
        );
    }

    [Fact]
    public void ListPattern_LangVer()
    {
        var source = @"
_ = new C() is [];
_ = new C() is [.. var x];
_ = new C() is .. var y;

class C
{
    public int this[int i] => 1;
    public int Length => 0;
    public int Slice(int i, int j) => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.Regular10);
        compilation.VerifyDiagnostics(
            // (2,16): error CS8936: Feature 'list pattern' is not available in C# 10.0. Please use language version 11.0 or greater.
            // _ = new C() is [];
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "[]").WithArguments("list pattern", "11.0").WithLocation(2, 16),
            // (3,16): error CS8936: Feature 'list pattern' is not available in C# 10.0. Please use language version 11.0 or greater.
            // _ = new C() is [.. var x];
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "[.. var x]").WithArguments("list pattern", "11.0").WithLocation(3, 16),
            // (4,16): error CS8980: Slice patterns may only be used once and directly inside a list pattern.
            // _ = new C() is .. var y;
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, ".. var y").WithLocation(4, 16)
            );

        compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.Regular11);
        compilation.VerifyDiagnostics(
            // (4,16): error CS9002: Slice patterns may only be used once and directly inside a list pattern.
            // _ = new C() is .. var y;
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, ".. var y").WithLocation(4, 16)
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
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0016
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int Test5.Count.get""
  IL_0009:  stloc.0
  IL_000a:  ldarg.0
  IL_000b:  ldc.i4.0
  IL_000c:  ldloc.0
  IL_000d:  callvirt   ""int Test5.Slice(int, int)""
  IL_0012:  ldc.i4.1
  IL_0013:  ceq
  IL_0015:  ret
  IL_0016:  ldc.i4.0
  IL_0017:  ret
}
")
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
            // error CS8985: List patterns may not be used for a value of type 'object'. No suitable 'Length' or 'Count' property was found.
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, listPattern).WithArguments("object"),

            // error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            Diagnostic(ErrorCode.ERR_BadIndexLHS, listPattern).WithArguments("object"),

            // error CS0021: Cannot apply indexing with [] to an expression of type 'object'
            subpattern == ".._" ? Diagnostic(ErrorCode.ERR_BadIndexLHS, subpattern).WithArguments("object") : null
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
        _ = new X()[^1];
    } 
}
";
        var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (20,24): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = new X() is [1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[1]").WithArguments("System.Index", ".ctor").WithLocation(20, 24),
            // (21,24): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = new X() is [.. 1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[.. 1]").WithArguments("System.Index", ".ctor").WithLocation(21, 24),
            // (21,25): error CS0656: Missing compiler required member 'System.Range..ctor'
            //         _ = new X() is [.. 1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".. 1").WithArguments("System.Range", ".ctor").WithLocation(21, 25),
            // (22,21): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = new X()[^1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^1").WithArguments("System.Index", ".ctor").WithLocation(22, 21)
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
    public void ListPattern_MissingMembers_IndexCtor()
    {
        var source = @"
class X
{
    public int Length => throw null;
    public int this[System.Index i] => throw null;
    public int this[System.Range r] => throw null;

    public void M()
    {
        _ = this is [0];
        _ = this is [.., 0];
        _ = this[^1];
        _ = this[..];
    }
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.MakeMemberMissing(WellKnownMember.System_Index__ctor);
        compilation.VerifyEmitDiagnostics(
            // (10,21): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = this is [0];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[0]").WithArguments("System.Index", ".ctor").WithLocation(10, 21),
            // (11,21): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = this is [.., 0];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[.., 0]").WithArguments("System.Index", ".ctor").WithLocation(11, 21),
            // (12,18): error CS0656: Missing compiler required member 'System.Index..ctor'
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^1").WithArguments("System.Index", ".ctor").WithLocation(12, 18)
            );
    }

    [Fact]
    public void ListPattern_MissingMembers_RangeCtor()
    {
        var source = @"
class X
{
    public int Length => throw null;
    public int this[System.Index i] => throw null;
    public int this[System.Range r] => throw null;

    public void M()
    {
        _ = this is [0];

        _ = this is [.._];
        _ = this is [0, .._];
        _ = this is [.._, 0];
        _ = this is [0, .._, 0];

        _ = this[^1];

        _ = this[..];
        _ = this[1..];
        _ = this[..^1];
        _ = this[1..^1];
    }
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.MakeMemberMissing(WellKnownMember.System_Range__ctor);
        compilation.MakeMemberMissing(WellKnownMember.System_Range__get_All);
        compilation.MakeMemberMissing(WellKnownMember.System_Range__StartAt);
        compilation.MakeMemberMissing(WellKnownMember.System_Range__EndAt);
        // Note: slice patterns always use range expressions with start and end.
        // But range syntax binds differently depending whether start/end are there and depending on what members are available.

        compilation.VerifyEmitDiagnostics(
            // (12,22): error CS0656: Missing compiler required member 'System.Range..ctor'
            //         _ = this is [.._];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".._").WithArguments("System.Range", ".ctor").WithLocation(12, 22),
            // (13,25): error CS0656: Missing compiler required member 'System.Range..ctor'
            //         _ = this is [0, .._];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".._").WithArguments("System.Range", ".ctor").WithLocation(13, 25),
            // (14,22): error CS0656: Missing compiler required member 'System.Range..ctor'
            //         _ = this is [.._, 0];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".._").WithArguments("System.Range", ".ctor").WithLocation(14, 22),
            // (15,25): error CS0656: Missing compiler required member 'System.Range..ctor'
            //         _ = this is [0, .._, 0];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".._").WithArguments("System.Range", ".ctor").WithLocation(15, 25),
            // (19,18): error CS0656: Missing compiler required member 'System.Range..ctor'
            //         _ = this[..];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "..").WithArguments("System.Range", ".ctor").WithLocation(19, 18),
            // (20,18): error CS0656: Missing compiler required member 'System.Range..ctor'
            //         _ = this[1..];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1..").WithArguments("System.Range", ".ctor").WithLocation(20, 18),
            // (21,18): error CS0656: Missing compiler required member 'System.Range..ctor'
            //         _ = this[..^1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "..^1").WithArguments("System.Range", ".ctor").WithLocation(21, 18),
            // (22,18): error CS0656: Missing compiler required member 'System.Range..ctor'
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1..^1").WithArguments("System.Range", ".ctor").WithLocation(22, 18)
            );
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
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.MakeMemberMissing(SpecialMember.System_String__SubstringIntInt);
        compilation.VerifyEmitDiagnostics(
            // (6,19): error CS0656: Missing compiler required member 'System.String.Substring'
            //         _ = s is [.. var slice];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".. var slice").WithArguments("System.String", "Substring").WithLocation(6, 19),
            // (6,19): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            //         _ = s is [.. var slice];
            Diagnostic(ErrorCode.ERR_BadArgType, ".. var slice").WithArguments("1", "System.Range", "int").WithLocation(6, 19),
            // (7,13): error CS0656: Missing compiler required member 'System.String.Substring'
            //         _ = s[..];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "s[..]").WithArguments("System.String", "Substring").WithLocation(7, 13),
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
        _ = a[..];
    } 
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (6,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray'
            //         _ = a is [.. var slice];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".. var slice").WithArguments("System.Runtime.CompilerServices.RuntimeHelpers", "GetSubArray").WithLocation(6, 19),
            // (7,15): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray'
            //         _ = a[..];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "..").WithArguments("System.Runtime.CompilerServices.RuntimeHelpers", "GetSubArray").WithLocation(7, 15)
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
        var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns);
        compilation.VerifyEmitDiagnostics(
            // (6,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray'
            //         _ = a is [.. var slice];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".. var slice").WithArguments("System.Runtime.CompilerServices.RuntimeHelpers", "GetSubArray").WithLocation(6, 19),
            // (7,15): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray'
            //         _ = a[..];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "..").WithArguments("System.Runtime.CompilerServices.RuntimeHelpers", "GetSubArray").WithLocation(7, 15)
            );
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
            // (25,28): error CS0619: 'Test1.Count' is obsolete: 'error3'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test1.Count", "error3").WithLocation(25, 28),
            // (25,28): error CS0619: 'Test1.Count' is obsolete: 'error3'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test1.Count", "error3").WithLocation(25, 28),
            // (25,28): error CS0619: 'Test1.this[int]' is obsolete: 'error2'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test1.this[int]", "error2").WithLocation(25, 28),
            // (26,28): error CS0619: 'Test1.Count' is obsolete: 'error3'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test1.Count", "error3").WithLocation(26, 28),
            // (26,28): error CS0619: 'Test1.Count' is obsolete: 'error3'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test1.Count", "error3").WithLocation(26, 28),
            // (26,28): error CS0619: 'Test1.this[int]' is obsolete: 'error2'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test1.this[int]", "error2").WithLocation(26, 28),
            // (26,29): error CS0619: 'Test1.Count' is obsolete: 'error3'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Test1.Count", "error3").WithLocation(26, 29),
            // (26,29): error CS0619: 'Test1.Slice(int, int)' is obsolete: 'error1'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Test1.Slice(int, int)", "error1").WithLocation(26, 29),
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
            // (28,29): error CS0619: 'Test2.this[Range]' is obsolete: 'error5'
            //         _ = new Test2() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Test2.this[System.Range]", "error5").WithLocation(28, 29)
            );
    }

    [Fact]
    public void ListPattern_ObsoleteAccessors()
    {
        var source = @"
using System;
class Test1
{
    [Obsolete(""error1"", error: true)]
    public int Slice(int i, int j) => 0;
    public int this[int i]
    {
        [Obsolete(""error1"", error: true)]
        get => 0;
    }
    public int Count
    {
        [Obsolete(""error2"", error: true)]
        get => 0;
    }
}
class Test2
{
    public int this[Index i]
    {
        [Obsolete(""error3"", error: true)]
        get => 0;
    }
    public int this[Range i]
    {
        [Obsolete(""error4"", error: true)]
        get => 0;
    }
    public int Length
    {
        [Obsolete(""error5"", error: true)]
        get => 0;
    }
}
class X
{
    public void M()
    {
        _ = new Test1() is [0];
        _ = new Test1()[^1];

        _ = new Test1() is [..0];
        _ = new Test1()[..0];

        _ = new Test2() is [0];
        _ = new Test2()[^1];

        _ = new Test2() is [..0];
        _ = new Test2()[..0];
    }
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (40,28): error CS0619: 'Test1.Count.get' is obsolete: 'error2'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test1.Count.get", "error2").WithLocation(40, 28),
            // (40,28): error CS0619: 'Test1.Count.get' is obsolete: 'error2'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test1.Count.get", "error2").WithLocation(40, 28),
            // (40,28): error CS0619: 'Test1.this[int].get' is obsolete: 'error1'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test1.this[int].get", "error1").WithLocation(40, 28),

            // (41,13): error CS0619: 'Test1.Count.get' is obsolete: 'error2'
            //         _ = new Test1()[^1];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test1()[^1]").WithArguments("Test1.Count.get", "error2").WithLocation(41, 13),
            // (41,13): error CS0619: 'Test1.this[int].get' is obsolete: 'error1'
            //         _ = new Test1()[^1];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test1()[^1]").WithArguments("Test1.this[int].get", "error1").WithLocation(41, 13),

            // (43,28): error CS0619: 'Test1.Count.get' is obsolete: 'error2'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test1.Count.get", "error2").WithLocation(43, 28),
            // (43,28): error CS0619: 'Test1.Count.get' is obsolete: 'error2'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test1.Count.get", "error2").WithLocation(43, 28),
            // (43,28): error CS0619: 'Test1.this[int].get' is obsolete: 'error1'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test1.this[int].get", "error1").WithLocation(43, 28),
            // (43,29): error CS0619: 'Test1.Slice(int, int)' is obsolete: 'error1'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Test1.Slice(int, int)", "error1").WithLocation(43, 29),
            // (43,29): error CS0619: 'Test1.Count.get' is obsolete: 'error2'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Test1.Count.get", "error2").WithLocation(43, 29),

            // (44,13): error CS0619: 'Test1.Slice(int, int)' is obsolete: 'error1'
            //         _ = new Test1()[..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test1()[..0]").WithArguments("Test1.Slice(int, int)", "error1").WithLocation(44, 13),
            // (44,13): error CS0619: 'Test1.Count.get' is obsolete: 'error2'
            //         _ = new Test1()[..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test1()[..0]").WithArguments("Test1.Count.get", "error2").WithLocation(44, 13),

            // (46,28): error CS0619: 'Test2.Length.get' is obsolete: 'error5'
            //         _ = new Test2() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test2.Length.get", "error5").WithLocation(46, 28),
            // (46,28): error CS0619: 'Test2.this[Index].get' is obsolete: 'error3'
            //         _ = new Test2() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Test2.this[System.Index].get", "error3").WithLocation(46, 28),

            // (47,13): error CS0619: 'Test2.this[Index].get' is obsolete: 'error3'
            //         _ = new Test2()[^1];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test2()[^1]").WithArguments("Test2.this[System.Index].get", "error3").WithLocation(47, 13),

            // (49,28): error CS0619: 'Test2.Length.get' is obsolete: 'error5'
            //         _ = new Test2() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test2.Length.get", "error5").WithLocation(49, 28),
            // (49,28): error CS0619: 'Test2.this[Index].get' is obsolete: 'error3'
            //         _ = new Test2() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Test2.this[System.Index].get", "error3").WithLocation(49, 28),
            // (49,29): error CS0619: 'Test2.this[Range].get' is obsolete: 'error4'
            //         _ = new Test2() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Test2.this[System.Range].get", "error4").WithLocation(49, 29),

            // (50,13): error CS0619: 'Test2.this[Range].get' is obsolete: 'error4'
            //         _ = new Test2()[..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test2()[..0]").WithArguments("Test2.this[System.Range].get", "error4").WithLocation(50, 13)
            );
    }

    [Fact]
    public void ListPattern_ObsoleteAccessors_OnBase()
    {
        var source = @"
using System;
class Base1
{
    [Obsolete(""error1"", error: true)]
    public int Slice(int i, int j) => 0;

    public virtual int this[int i]
    {
        [Obsolete(""error2"", error: true)]
        get => 0;
        set { }
    }
    public virtual int Count
    {
        [Obsolete(""error3"", error: true)]
        get => 0;
        set { }
    }
}
class Test1 : Base1
{
    public override int this[int i]
    {
        set { }
    }
    public override int Count
    {
        set { }
    }
}
class Base2
{
    public virtual int this[Index i]
    {
        [Obsolete(""error4"", error: true)]
        get => 0;
        set { }
    }
    public virtual int this[Range i]
    {
        [Obsolete(""error5"", error: true)]
        get => 0;
        set { }
    }
    public virtual int Length
    {
        [Obsolete(""error6"", error: true)]
        get => 0;
        set { }
    }
}

class Test2 : Base2
{
    public override int this[Index i]
    {
        set { }
    }
    public override int this[Range i]
    {
        set { }
    }
    public override int Length
    {
        set { }
    }
}
class X
{
    public void M()
    {
        _ = new Test1() is [0];
        _ = new Test1() is [..0];
        _ = new Test2() is [0];
        _ = new Test2() is [..0];

        _ = new Test1()[^1];
        _ = new Test1()[..0];
        _ = new Test2()[^1];
        _ = new Test2()[..0];
    }
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (73,28): error CS0619: 'Base1.Count.get' is obsolete: 'error3'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Base1.Count.get", "error3").WithLocation(73, 28),
            // (73,28): error CS0619: 'Base1.Count.get' is obsolete: 'error3'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Base1.Count.get", "error3").WithLocation(73, 28),
            // (73,28): error CS0619: 'Base1.this[int].get' is obsolete: 'error2'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Base1.this[int].get", "error2").WithLocation(73, 28),

            // (74,28): error CS0619: 'Base1.Count.get' is obsolete: 'error3'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Base1.Count.get", "error3").WithLocation(74, 28),
            // (74,28): error CS0619: 'Base1.Count.get' is obsolete: 'error3'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Base1.Count.get", "error3").WithLocation(74, 28),
            // (74,28): error CS0619: 'Base1.this[int].get' is obsolete: 'error2'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Base1.this[int].get", "error2").WithLocation(74, 28),
            // (74,29): error CS0619: 'Base1.Count.get' is obsolete: 'error3'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Base1.Count.get", "error3").WithLocation(74, 29),
            // (74,29): error CS0619: 'Base1.Slice(int, int)' is obsolete: 'error1'
            //         _ = new Test1() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Base1.Slice(int, int)", "error1").WithLocation(74, 29),

            // (75,28): error CS0619: 'Base2.Length.get' is obsolete: 'error6'
            //         _ = new Test2() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Base2.Length.get", "error6").WithLocation(75, 28),
            // (75,28): error CS0619: 'Base2.this[Index].get' is obsolete: 'error4'
            //         _ = new Test2() is [0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[0]").WithArguments("Base2.this[System.Index].get", "error4").WithLocation(75, 28),

            // (76,28): error CS0619: 'Base2.Length.get' is obsolete: 'error6'
            //         _ = new Test2() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Base2.Length.get", "error6").WithLocation(76, 28),
            // (76,28): error CS0619: 'Base2.this[Index].get' is obsolete: 'error4'
            //         _ = new Test2() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[..0]").WithArguments("Base2.this[System.Index].get", "error4").WithLocation(76, 28),
            // (76,29): error CS0619: 'Base2.this[Range].get' is obsolete: 'error5'
            //         _ = new Test2() is [..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "..0").WithArguments("Base2.this[System.Range].get", "error5").WithLocation(76, 29),

            // (78,13): error CS0619: 'Base1.Count.get' is obsolete: 'error3'
            //         _ = new Test1()[^1];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test1()[^1]").WithArguments("Base1.Count.get", "error3").WithLocation(78, 13),
            // (78,13): error CS0619: 'Base1.this[int].get' is obsolete: 'error2'
            //         _ = new Test1()[^1];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test1()[^1]").WithArguments("Base1.this[int].get", "error2").WithLocation(78, 13),

            // (79,13): error CS0619: 'Base1.Count.get' is obsolete: 'error3'
            //         _ = new Test1()[..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test1()[..0]").WithArguments("Base1.Count.get", "error3").WithLocation(79, 13),
            // (79,13): error CS0619: 'Base1.Slice(int, int)' is obsolete: 'error1'
            //         _ = new Test1()[..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test1()[..0]").WithArguments("Base1.Slice(int, int)", "error1").WithLocation(79, 13),

            // (80,13): error CS0619: 'Base2.this[Index].get' is obsolete: 'error4'
            //         _ = new Test2()[^1];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test2()[^1]").WithArguments("Base2.this[System.Index].get", "error4").WithLocation(80, 13),

            // (81,13): error CS0619: 'Base2.this[Range].get' is obsolete: 'error5'
            //         _ = new Test2()[..0];
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new Test2()[..0]").WithArguments("Base2.this[System.Range].get", "error5").WithLocation(81, 13)
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
            // (6,23): error CS9002: Slice patterns may only be used once and directly inside a list pattern.
            //         _ = a is [.., ..];
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(6, 23),
            // (7,29): error CS9002: Slice patterns may only be used once and directly inside a list pattern.
            //         _ = a is [1, .., 2, .., 3];
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(7, 29),
            // (8,20): error CS9002: Slice patterns may only be used once and directly inside a list pattern.
            //         _ = a is [(..)];
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(8, 20),
            // (9,18): error CS9002: Slice patterns may only be used once and directly inside a list pattern.
            //         _ = a is ..;
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(9, 18),
            // (11,24): error CS9002: Slice patterns may only be used once and directly inside a list pattern.
            //         _ = a switch { .. => 0, _ => 0 };
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(11, 24),
            // (12,27): error CS9002: Slice patterns may only be used once and directly inside a list pattern.
            //         switch (a) { case ..: break; }
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(12, 27)
            );
    }

    [Fact]
    public void SlicePattern_NullValue()
    {
        var source = @"
#nullable enable
class C
{
    public int Length => 3;
    public int this[int i] => 0;
    public int[]? Slice(int i, int j) => null;

    public static void Main()
    {
        if (new C() is [.. var s0] && s0 == null)
            System.Console.Write(1);
        if (new C() is [.. null])
            System.Console.Write(2);
        if (new C() is not [.. {}])
            System.Console.Write(3);
    }
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, options: TestOptions.ReleaseExe);
        compilation.VerifyEmitDiagnostics();
        CompileAndVerify(compilation, expectedOutput: "12");
    }

    [Fact]
    public void ListPattern_MemberLookup_StaticIndexer()
    {
        var vbSource = @"
Namespace System
    Public Structure Index
        Public Sub New(ByVal value As Integer, ByVal Optional fromEnd As Boolean = False)
        End Sub
        Public Shared Widening Operator CType(ByVal i As Integer) As Index
            Return Nothing
        End Operator
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
        _ = new Test1()[0];
    } 
}
";
        var vbCompilation = CreateVisualBasicCompilation(vbSource);
        var csCompilation = CreateCompilation(csSource, references: new[] { vbCompilation.EmitToImageReference() });
        // Note: the VB indexer's name is "Item" but C# looks for "this[]", but we can't put Default on Shared indexer declaration
        csCompilation.VerifyEmitDiagnostics(
            // (6,28): error CS0021: Cannot apply indexing with [] to an expression of type 'Test1'
            //         _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[0]").WithArguments("Test1").WithLocation(6, 28),
            // (7,13): error CS0021: Cannot apply indexing with [] to an expression of type 'Test1'
            //         _ = new Test1()[0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "new Test1()[0]").WithArguments("Test1").WithLocation(7, 13));
    }

    [Fact]
    public void ListPattern_MemberLookup_StaticDefaultIndexer()
    {
        var ilSource = @"
.class public sequential ansi sealed System.Index
    extends [mscorlib]System.ValueType
{
    .method public specialname rtspecialname instance void .ctor ( int32 'value', [opt] bool fromEnd ) cil managed
    {
        .param [2] = bool(false)
        IL_0000: nop
        IL_0001: ldarg.0
        IL_0002: initobj System.Index
        IL_0008: ret
    }

    .method public specialname static valuetype System.Index op_Implicit ( int32 i ) cil managed
    {
        .locals init (
            [0] valuetype System.Index
        )

        IL_0000: nop
        IL_0001: ldloca.s 0
        IL_0003: initobj System.Index
        IL_0009: br.s IL_000b

        IL_000b: ldloc.0
        IL_000c: ret
    }
}

.class public auto ansi Test1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    .field private int32 _Length

    .method public specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ldarg.0
        IL_0008: ldc.i4.0
        IL_0009: call instance void Test1::set_Length(int32)
        IL_000e: nop
        IL_000f: ret
    }

    .method public specialname static int32 get_Item ( valuetype System.Index i ) cil managed
    {
        .locals init (
            [0] int32 Item
        )

        IL_0000: nop
        IL_0001: ldc.i4.0
        IL_0002: stloc.0
        IL_0003: br.s IL_0005

        IL_0005: ldloc.0
        IL_0006: ret
    }

    .method public specialname instance int32 get_Length () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Test1::_Length
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname instance void set_Length ( int32 AutoPropertyValue ) cil managed
    {
        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: stfld int32 Test1::_Length
        IL_0007: ret
    }

    .property int32 Item( valuetype System.Index i )
    {
        .get int32 Test1::get_Item(valuetype System.Index)
    }
    .property instance int32 Length()
    {
        .get instance int32 Test1::get_Length()
        .set instance void Test1::set_Length(int32)
    }
}
";
        var csSource = @"
System.Console.Write((new Test1() is [42], new Test1()[0]));
";
        var csCompilation = CreateCompilationWithIL(csSource, ilSource);
        csCompilation.VerifyEmitDiagnostics(
            // (2,38): error CS0176: Member 'Test1.this[Index]' cannot be accessed with an instance reference; qualify it with a type name instead
            // System.Console.Write((new Test1() is [42], new Test1()[0]));
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "[42]").WithArguments("Test1.this[System.Index]").WithLocation(2, 38),
            // (2,44): error CS0176: Member 'Test1.this[Index]' cannot be accessed with an instance reference; qualify it with a type name instead
            // System.Console.Write((new Test1() is [42], new Test1()[0]));
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "new Test1()[0]").WithArguments("Test1.this[System.Index]").WithLocation(2, 44)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_DefaultIndexerWithDifferentName()
    {
        var vbSource = @"
Namespace System
    Public Structure Index
        Public Sub New(ByVal value As Integer, ByVal Optional fromEnd As Boolean = False)
        End Sub
        Public Shared Widening Operator CType(ByVal i As Integer) As Index
            Return Nothing
        End Operator
    End Structure
End Namespace
Public Class Test1
    Public Default ReadOnly Property Name(i As System.Index) As Integer
        Get
            Return 42
        End Get
    End Property
    Public Property Length As Integer = 1
End Class
";
        var csSource = @"
System.Console.Write((new Test1() is [42], new Test1()[0]));
";
        var vbCompilation = CreateVisualBasicCompilation(vbSource);
        var csCompilation = CreateCompilation(csSource, references: new[] { vbCompilation.EmitToImageReference() });
        CompileAndVerify(csCompilation, expectedOutput: "(True, 42)").VerifyDiagnostics();
    }

    [Fact]
    public void ListPattern_MemberLookup_NonDefaultIndexerNamedItem()
    {
        var vbSource = @"
Namespace System
    Public Structure Index
        Public Sub New(ByVal value As Integer, ByVal Optional fromEnd As Boolean = False)
        End Sub
        Public Shared Widening Operator CType(ByVal i As Integer) As Index
            Return Nothing
        End Operator
    End Structure
End Namespace
Public Class Test1
    Public ReadOnly Property Item(i As System.Index) As Integer
        Get
            Return 0
        End Get
    End Property
    Public Property Length As Integer = 0
End Class
";
        var csSource = @"
System.Console.Write((new Test1() is [42], new Test1()[0]));
";
        var vbCompilation = CreateVisualBasicCompilation(vbSource);
        var csCompilation = CreateCompilation(csSource, references: new[] { vbCompilation.EmitToImageReference() });
        csCompilation.VerifyEmitDiagnostics(
            // (2,38): error CS0021: Cannot apply indexing with [] to an expression of type 'Test1'
            // System.Console.Write((new Test1() is [42], new Test1()[0]));
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[42]").WithArguments("Test1").WithLocation(2, 38),
            // (2,44): error CS0021: Cannot apply indexing with [] to an expression of type 'Test1'
            // System.Console.Write((new Test1() is [42], new Test1()[0]));
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "new Test1()[0]").WithArguments("Test1").WithLocation(2, 44)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Index_ErrorCases_1()
    {
        var source = @"
using System;

_ = new Test1() is [0];
_ = new Test1()[^1];

class Test1
{
    public int this[Index i] { set {} }
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,20): error CS0154: The property or indexer 'Test1.this[Index]' cannot be used in this context because it lacks the get accessor
            // _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "[0]").WithArguments("Test1.this[System.Index]").WithLocation(4, 20),
            // (5,5): error CS0154: The property or indexer 'Test1.this[Index]' cannot be used in this context because it lacks the get accessor
            // _ = new Test1()[^1];
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "new Test1()[^1]").WithArguments("Test1.this[System.Index]").WithLocation(5, 5)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Index_ErrorCases_2()
    {
        var source = @"
using System;

_ = new Test1() is [0];
_ = new Test1()[^1];

class Test1
{
    public int this[Index i] { private get => 0; set {} }
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,20): error CS0271: The property or indexer 'Test1.this[Index]' cannot be used in this context because the get accessor is inaccessible
            // _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_InaccessibleGetter, "[0]").WithArguments("Test1.this[System.Index]").WithLocation(4, 20),
            // (5,5): error CS0271: The property or indexer 'Test1.this[Index]' cannot be used in this context because the get accessor is inaccessible
            // _ = new Test1()[^1];
            Diagnostic(ErrorCode.ERR_InaccessibleGetter, "new Test1()[^1]").WithArguments("Test1.this[System.Index]").WithLocation(5, 5)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Index_ErrorCases_3()
    {
        var source = @"
using System;

_ = new Test1() is [0];
_ = new Test1()[^1];

class Test1
{
    public int this[int i, int ignored = 0] => 0;
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,20): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_BadArgType, "[0]").WithArguments("1", "System.Index", "int").WithLocation(4, 20),
            // (5,17): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = new Test1()[^1];
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(5, 17)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Index_ErrorCases_4()
    {
        var source = @"
using System;

_ = new Test1() is [0];
_ = new Test1()[^1];

class Test1
{
    public int this[long i, int ignored = 0] => 0;
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,20): error CS1503: Argument 1: cannot convert from 'System.Index' to 'long'
            // _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_BadArgType, "[0]").WithArguments("1", "System.Index", "long").WithLocation(4, 20),
            // (5,17): error CS1503: Argument 1: cannot convert from 'System.Index' to 'long'
            // _ = new Test1()[^1];
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "long").WithLocation(5, 17)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Index_ErrorCases_5()
    {
        var source = @"
using System;

_ = new Test1() is [0];
_ = new Test1()[^1];

class Test1
{
    public int this[long i] => 0;
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,20): error CS1503: Argument 1: cannot convert from 'System.Index' to 'long'
            // _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_BadArgType, "[0]").WithArguments("1", "System.Index", "long").WithLocation(4, 20),
            // (5,17): error CS1503: Argument 1: cannot convert from 'System.Index' to 'long'
            // _ = new Test1()[^1];
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "long").WithLocation(5, 17)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Index_ErrorCases_6()
    {
        var source = @"
using System;

_ = new Test1() is [0];
_ = new Test1()[^1];

class Test1
{
    public int this[params int[] i] => 0;
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,20): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_BadArgType, "[0]").WithArguments("1", "System.Index", "int").WithLocation(4, 20),
            // (5,17): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = new Test1()[^1];
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(5, 17)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Index_ErrorCases_7()
    {
        var source = @"
using System;

_ = new Test1() is [0];
_ = new Test1()[^1];

class Test1
{
    private int this[Index i] => 0;
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,20): error CS0122: 'Test1.this[Index]' is inaccessible due to its protection level
            // _ = new Test1() is [0];
            Diagnostic(ErrorCode.ERR_BadAccess, "[0]").WithArguments("Test1.this[System.Index]").WithLocation(4, 20),
            // (5,5): error CS0122: 'Test1.this[Index]' is inaccessible due to its protection level
            // _ = new Test1()[^1];
            Diagnostic(ErrorCode.ERR_BadAccess, "new Test1()[^1]").WithArguments("Test1.this[System.Index]").WithLocation(5, 5)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Range_ErrorCases_1()
    {
        var source = @"
using System;

_ = new Test1() is [..var p];
_ = new Test1() is [..];
_ = new Test1()[..];

class Test1
{
    public int this[Range i] { set {} }
    public int this[int i] => throw new();
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,21): error CS0154: The property or indexer 'Test1.this[Range]' cannot be used in this context because it lacks the get accessor
            // _ = new Test1() is [..var p];
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "..var p").WithArguments("Test1.this[System.Range]").WithLocation(4, 21),
            // (6,5): error CS0154: The property or indexer 'Test1.this[Range]' cannot be used in this context because it lacks the get accessor
            // _ = new Test1()[..];
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "new Test1()[..]").WithArguments("Test1.this[System.Range]").WithLocation(6, 5)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Range_ErrorCases_2()
    {
        var source = @"
using System;

_ = new Test1() is [..var p];
_ = new Test1() is [..];
_ = new Test1()[..];

class Test1
{
    public int this[Range i] { private get => 0; set {} }
    public int this[int i] => throw new();
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,21): error CS0271: The property or indexer 'Test1.this[Range]' cannot be used in this context because the get accessor is inaccessible
            // _ = new Test1() is [..var p];
            Diagnostic(ErrorCode.ERR_InaccessibleGetter, "..var p").WithArguments("Test1.this[System.Range]").WithLocation(4, 21),
            // (6,5): error CS0271: The property or indexer 'Test1.this[Range]' cannot be used in this context because the get accessor is inaccessible
            // _ = new Test1()[..];
            Diagnostic(ErrorCode.ERR_InaccessibleGetter, "new Test1()[..]").WithArguments("Test1.this[System.Range]").WithLocation(6, 5)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Range_ErrorCases_3()
    {
        var source = @"
using System;

_ = new Test1() is [..var p];
_ = new Test1() is [..];
_ = new Test1()[..];

class Test1
{
    public int Slice(int i, int j, int ignored = 0) => 0;
    public int this[int i] => throw new();
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,21): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1() is [..var p];
            Diagnostic(ErrorCode.ERR_BadArgType, "..var p").WithArguments("1", "System.Range", "int").WithLocation(4, 21),
            // (6,17): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1()[..];
            Diagnostic(ErrorCode.ERR_BadArgType, "..").WithArguments("1", "System.Range", "int").WithLocation(6, 17)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Range_ErrorCases_4()
    {
        var source = @"
using System;

_ = new Test1() is [..var p];
_ = new Test1() is [..];
_ = new Test1()[..];

class Test1
{
    public int Slice(int i, int j, params int[] ignored) => 0;
    public int this[int i] => throw new();
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,21): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1() is [..var p];
            Diagnostic(ErrorCode.ERR_BadArgType, "..var p").WithArguments("1", "System.Range", "int").WithLocation(4, 21),
            // (6,17): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1()[..];
            Diagnostic(ErrorCode.ERR_BadArgType, "..").WithArguments("1", "System.Range", "int").WithLocation(6, 17)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Range_ErrorCases_5()
    {
        var source = @"
using System;

_ = new Test1() is [..var p];
_ = new Test1() is [..];
_ = new Test1()[..];

class Test1
{
    public int Slice(long i, long j) => 0;
    public int this[int i] => throw new();
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,21): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1() is [..var p];
            Diagnostic(ErrorCode.ERR_BadArgType, "..var p").WithArguments("1", "System.Range", "int").WithLocation(4, 21),
            // (6,17): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1()[..];
            Diagnostic(ErrorCode.ERR_BadArgType, "..").WithArguments("1", "System.Range", "int").WithLocation(6, 17)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Range_ErrorCases_6()
    {
        var source = @"
using System;

_ = new Test1() is [..var p];
_ = new Test1() is [..];
_ = new Test1()[..];

class Test1
{
    public int Slice(params int[] i) => 0;
    public int this[int i] => throw new();
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,21): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1() is [..var p];
            Diagnostic(ErrorCode.ERR_BadArgType, "..var p").WithArguments("1", "System.Range", "int").WithLocation(4, 21),
            // (6,17): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1()[..];
            Diagnostic(ErrorCode.ERR_BadArgType, "..").WithArguments("1", "System.Range", "int").WithLocation(6, 17)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Range_ErrorCases_7()
    {
        var source = @"
using System;

_ = new Test1() is [..var p];
_ = new Test1() is [..];
_ = new Test1()[..];

class Test1
{
    private int Slice(int i, int j) => 0;
    public int this[int i] => throw new();
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,21): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1() is [..var p];
            Diagnostic(ErrorCode.ERR_BadArgType, "..var p").WithArguments("1", "System.Range", "int").WithLocation(4, 21),
            // (6,17): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1()[..];
            Diagnostic(ErrorCode.ERR_BadArgType, "..").WithArguments("1", "System.Range", "int").WithLocation(6, 17)
            );
    }

    [Fact]
    public void ListPattern_MemberLookup_Range_ErrorCases_8()
    {
        var source = @"
using System;

_ = new Test1() is [..var p];
_ = new Test1() is [..];
_ = new Test1()[..];

class Test1
{
    public void Slice(int i, int j) {}
    public int this[int i] => throw new();
    public int Length => 0;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (4,21): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1() is [..var p];
            Diagnostic(ErrorCode.ERR_BadArgType, "..var p").WithArguments("1", "System.Range", "int").WithLocation(4, 21),
            // (6,17): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new Test1()[..];
            Diagnostic(ErrorCode.ERR_BadArgType, "..").WithArguments("1", "System.Range", "int").WithLocation(6, 17)
            );
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index }, options: TestOptions.ReleaseExe);
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
        _ = a is [1] and [1];         // 2
        _ = a is {Length:0} and [1];  // 3
        _ = a is {Length:1} and [1];
        _ = a is [1,2,3] and [1,2,4]; // 4
        _ = a is [1,2,3] and [1,2,3]; // 5, 6, 7
        _ = a is ([>0]) and ([<0]);   // 8
        _ = a is ([>0]) and ([>=0]);  // 9
        _ = a is [>0] and [<0];       // 10
        _ = a is [>0] and [>=0];      // 11
        _ = a is {Length:-1};         // 12
        _ = a is [.., >0] and [<0];   // 13
        _ = a is [.., >0, _] and [_, <=0, ..];
        _ = new { a } is { a.Length:-1 }; // 14
        _ = a is [..{ Length: -1 }];  // 15
        _ = a is [..{ Length: < -1 }];  // 16
        _ = a is [..{ Length: <= -1 }]; // 17
        _ = a is [..{ Length: >= -1 }]; // 18
        _ = a is [..{ Length: > -1 }];  // 19
        _ = a is [_, _, ..{ Length: int.MaxValue - 1 }]; // 20
        _ = a is [_, _, ..{ Length: <= int.MaxValue - 1 }]; // 21
        _ = a is [_, _, ..{ Length: >= int.MaxValue - 1 }]; // 22
        _ = a is [_, _, ..{ Length: < int.MaxValue - 1 }]; // 23
        _ = a is [_, _, ..{ Length: > int.MaxValue - 1 }]; // 24
        _ = a is { LongLength: -1 };
        _ = (Array)a is { Length: -1 };
        _ = mdarray is { Length: -1 };
    } 
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
        compilation.VerifyEmitDiagnostics(
            // 0.cs(7,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [] and [1];          // 1
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [] and [1]").WithArguments("int[]").WithLocation(7, 13),
            // 0.cs(8,27): hidden CS9335: The pattern is redundant.
            //         _ = a is [1] and [1];         // 2
            Diagnostic(ErrorCode.HDN_RedundantPattern, "1").WithLocation(8, 27),
            // 0.cs(9,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is {Length:0} and [1];  // 3
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is {Length:0} and [1]").WithArguments("int[]").WithLocation(9, 13),
            // 0.cs(11,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [1,2,3] and [1,2,4]; // 4
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [1,2,3] and [1,2,4]").WithArguments("int[]").WithLocation(11, 13),
            // 0.cs(12,31): hidden CS9335: The pattern is redundant.
            //         _ = a is [1,2,3] and [1,2,3]; // 5, 6, 7
            Diagnostic(ErrorCode.HDN_RedundantPattern, "1").WithLocation(12, 31),
            // 0.cs(12,33): hidden CS9335: The pattern is redundant.
            //         _ = a is [1,2,3] and [1,2,3]; // 5, 6, 7
            Diagnostic(ErrorCode.HDN_RedundantPattern, "2").WithLocation(12, 33),
            // 0.cs(12,35): hidden CS9335: The pattern is redundant.
            //         _ = a is [1,2,3] and [1,2,3]; // 5, 6, 7
            Diagnostic(ErrorCode.HDN_RedundantPattern, "3").WithLocation(12, 35),
            // 0.cs(13,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is ([>0]) and ([<0]);   // 8
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is ([>0]) and ([<0])").WithArguments("int[]").WithLocation(13, 13),
            // 0.cs(14,31): hidden CS9335: The pattern is redundant.
            //         _ = a is ([>0]) and ([>=0]);  // 9
            Diagnostic(ErrorCode.HDN_RedundantPattern, ">=0").WithLocation(14, 31),
            // 0.cs(15,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [>0] and [<0];       // 10
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [>0] and [<0]").WithArguments("int[]").WithLocation(15, 13),
            // 0.cs(16,28): hidden CS9335: The pattern is redundant.
            //         _ = a is [>0] and [>=0];      // 11
            Diagnostic(ErrorCode.HDN_RedundantPattern, ">=0").WithLocation(16, 28),
            // 0.cs(17,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is {Length:-1};         // 12
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is {Length:-1}").WithArguments("int[]").WithLocation(17, 13),
            // 0.cs(18,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [.., >0] and [<0];   // 13
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [.., >0] and [<0]").WithArguments("int[]").WithLocation(18, 13),
            // 0.cs(20,13): error CS8518: An expression of type '<anonymous type: int[] a>' can never match the provided pattern.
            //         _ = new { a } is { a.Length:-1 }; // 14
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new { a } is { a.Length:-1 }").WithArguments("<anonymous type: int[] a>").WithLocation(20, 13),
            // 0.cs(21,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [..{ Length: -1 }];  // 15
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [..{ Length: -1 }]").WithArguments("int[]").WithLocation(21, 13),
            // 0.cs(22,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [..{ Length: < -1 }];  // 16
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [..{ Length: < -1 }]").WithArguments("int[]").WithLocation(22, 13),
            // 0.cs(23,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [..{ Length: <= -1 }]; // 17
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [..{ Length: <= -1 }]").WithArguments("int[]").WithLocation(23, 13),
            // 0.cs(24,31): hidden CS9335: The pattern is redundant.
            //         _ = a is [..{ Length: >= -1 }]; // 18
            Diagnostic(ErrorCode.HDN_RedundantPattern, ">= -1").WithLocation(24, 31),
            // 0.cs(25,31): hidden CS9335: The pattern is redundant.
            //         _ = a is [..{ Length: > -1 }];  // 19
            Diagnostic(ErrorCode.HDN_RedundantPattern, "> -1").WithLocation(25, 31),
            // 0.cs(26,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [_, _, ..{ Length: int.MaxValue - 1 }]; // 20
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [_, _, ..{ Length: int.MaxValue - 1 }]").WithArguments("int[]").WithLocation(26, 13),
            // 0.cs(27,37): hidden CS9335: The pattern is redundant.
            //         _ = a is [_, _, ..{ Length: <= int.MaxValue - 1 }]; // 21
            Diagnostic(ErrorCode.HDN_RedundantPattern, "<= int.MaxValue - 1").WithLocation(27, 37),
            // 0.cs(28,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [_, _, ..{ Length: >= int.MaxValue - 1 }]; // 22
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is [_, _, ..{ Length: >= int.MaxValue - 1 }]").WithArguments("int[]").WithLocation(28, 13),
            // 0.cs(29,37): hidden CS9335: The pattern is redundant.
            //         _ = a is [_, _, ..{ Length: < int.MaxValue - 1 }]; // 23
            Diagnostic(ErrorCode.HDN_RedundantPattern, "< int.MaxValue - 1").WithLocation(29, 37),
            // 0.cs(30,13): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            //         _ = a is [_, _, ..{ Length: > int.MaxValue - 1 }]; // 24
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
        _ = t[^1]; // 1
    }
    public static void Test2<T>(T t) where T : ICountableViaLength
    {
        _ = t is { Length: -1 };
        _ = new { t } is { t.Length: -1 };
        _ = t[^1]; // 2
    }
    public static void Test3<T>(T t) where T : IIndexable, ICountableViaCount
    {
        _ = t is { Count: -1 }; // 3
        _ = new { t } is { t.Count: -1 }; // 4
        _ = t[^1];
    }
    public static void Test4<T>(T t) where T : IIndexable, ICountableViaLength
    {
        _ = t is { Length: -1 }; // 5
        _ = new { t } is { t.Length: -1 }; // 6
        _ = t[^1];
    }
    public static void Test5<T>(T t) where T : IIndexable, ICountableViaLength, ICountableViaCount
    {
        _ = t is { Length: -1 }; // 7
        _ = t is { Count: -1 };
        _ = new { t } is { t.Length: -1 }; // 8
        _ = new { t } is { t.Count: -1 };
        _ = t[^1];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
        compilation.VerifyEmitDiagnostics(
            // (20,13): error CS0021: Cannot apply indexing with [] to an expression of type 'T'
            //         _ = t[^1]; // 1
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "t[^1]").WithArguments("T").WithLocation(20, 13),
            // (26,13): error CS0021: Cannot apply indexing with [] to an expression of type 'T'
            //         _ = t[^1]; // 2
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "t[^1]").WithArguments("T").WithLocation(26, 13),
            // (30,13): error CS8518: An expression of type 'T' can never match the provided pattern.
            //         _ = t is { Count: -1 }; // 3
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "t is { Count: -1 }").WithArguments("T").WithLocation(30, 13),
            // (31,13): error CS8518: An expression of type '<anonymous type: T t>' can never match the provided pattern.
            //         _ = new { t } is { t.Count: -1 }; // 4
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new { t } is { t.Count: -1 }").WithArguments("<anonymous type: T t>").WithLocation(31, 13),
            // (36,13): error CS8518: An expression of type 'T' can never match the provided pattern.
            //         _ = t is { Length: -1 }; // 5
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "t is { Length: -1 }").WithArguments("T").WithLocation(36, 13),
            // (37,13): error CS8518: An expression of type '<anonymous type: T t>' can never match the provided pattern.
            //         _ = new { t } is { t.Length: -1 }; // 6
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new { t } is { t.Length: -1 }").WithArguments("<anonymous type: T t>").WithLocation(37, 13),
            // (42,13): error CS8518: An expression of type 'T' can never match the provided pattern.
            //         _ = t is { Length: -1 }; // 7
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "t is { Length: -1 }").WithArguments("T").WithLocation(42, 13),
            // (44,13): error CS8518: An expression of type '<anonymous type: T t>' can never match the provided pattern.
            //         _ = new { t } is { t.Length: -1 }; // 8
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new { t } is { t.Length: -1 }").WithArguments("<anonymous type: T t>").WithLocation(44, 13)
            );
    }

    [Fact, WorkItem(59466, "https://github.com/dotnet/roslyn/issues/59466")]
    public void AlwaysTruePattern()
    {
        var source = @"
_ = new S() is [..var y];
y.ToString();

_ = new S() is [..];
_ = new S() is [..[..]];
_ = new S() is not [..];

struct S
{
    public int Length => 1;
    public int this[int i] => 42;
    public S this[System.Range r] => default;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,5): warning CS8794: An expression of type 'S' always matches the provided pattern.
            // _ = new S() is [..var y];
            Diagnostic(ErrorCode.WRN_IsPatternAlways, "new S() is [..var y]").WithArguments("S").WithLocation(2, 5),
            // (5,5): warning CS8794: An expression of type 'S' always matches the provided pattern.
            // _ = new S() is [..];
            Diagnostic(ErrorCode.WRN_IsPatternAlways, "new S() is [..]").WithArguments("S").WithLocation(5, 5),
            // (6,5): warning CS8794: An expression of type 'S' always matches the provided pattern.
            // _ = new S() is [..[..]];
            Diagnostic(ErrorCode.WRN_IsPatternAlways, "new S() is [..[..]]").WithArguments("S").WithLocation(6, 5),
            // (7,5): error CS8518: An expression of type 'S' can never match the provided pattern.
            // _ = new S() is not [..];
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new S() is not [..]").WithArguments("S").WithLocation(7, 5)
            );
    }

    [Fact]
    public void ListPattern_ValEscape()
    {
        CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
public ref struct R
{
    public int Length => throw null;
    public R this[int i] => throw null;
    public R Slice(int i, int j) => throw null;
    public static implicit operator R(Span<int> span) => throw null;
}
public class C
{
    public void M1(ref Span<int> s)
    {
        Span<int> outer = stackalloc int[100];
        if (outer is [] list) s = list; // error 1
    }
    public void M2(ref R r)
    {
        R outer = stackalloc int[100];
        if (outer is [var element, .. var slice] list)
        {
            r = element; // error 2
            r = slice; // error 3
            r = list; // error 4       
        } 
    }
    public void M1b(ref Span<int> s)
    {
        Span<int> outer = default;
        if (outer is [] list) s = list; // OK
    }
    public void M2b(ref R r)
    {
        R outer = default;
        if (outer is [var element, .. var slice] list)
        {
            r = element; // OK
            r = slice; // OK
            r = list; // OK     
        } 
    }
}").VerifyDiagnostics(
            // (15,35): error CS8352: Cannot use variable 'list' in this context because it may expose referenced variables outside of their declaration scope
            //         if (outer is [] list) s = list; // error 1
            Diagnostic(ErrorCode.ERR_EscapeVariable, "list").WithArguments("list").WithLocation(15, 35),
            // (22,17): error CS8352: Cannot use variable 'element' in this context because it may expose referenced variables outside of their declaration scope
            //             r = element; // error 2
            Diagnostic(ErrorCode.ERR_EscapeVariable, "element").WithArguments("element").WithLocation(22, 17),
            // (23,17): error CS8352: Cannot use variable 'slice' in this context because it may expose referenced variables outside of their declaration scope
            //             r = slice; // error 3
            Diagnostic(ErrorCode.ERR_EscapeVariable, "slice").WithArguments("slice").WithLocation(23, 17),
            // (24,17): error CS8352: Cannot use variable 'list' in this context because it may expose referenced variables outside of their declaration scope
            //             r = list; // error 4
            Diagnostic(ErrorCode.ERR_EscapeVariable, "list").WithArguments("list").WithLocation(24, 17));
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

        _ = a switch
        {
            null => 0,
            [..null]  => 1, 
            [.. { Length: not bad}]  => 2, 
            _ => 3,
        };

         _ = a switch
        {
            null => 0,
            [..null]  => 1, 
            [.. { Length: not < bad}]  => 2,
            _ => 3,
        };
    } 
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
        compilation.VerifyEmitDiagnostics(
            // 0.cs(7,25): error CS0029: Cannot implicitly convert type 'int[]' to 'int'
            //         const int bad = a;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "a").WithArguments("int[]", "int").WithLocation(7, 25),
            // 0.cs(11,31): hidden CS9335: The pattern is redundant.
            //         _ = a is [..{ Length: < bad }];
            Diagnostic(ErrorCode.HDN_RedundantPattern, "< bad").WithLocation(11, 31),
            // 0.cs(12,31): hidden CS9335: The pattern is redundant.
            //         _ = a is [..{ Length: <= bad }];
            Diagnostic(ErrorCode.HDN_RedundantPattern, "<= bad").WithLocation(12, 31),
            // 0.cs(13,31): hidden CS9335: The pattern is redundant.
            //         _ = a is [..{ Length: >= bad }];
            Diagnostic(ErrorCode.HDN_RedundantPattern, ">= bad").WithLocation(13, 31),
            // 0.cs(14,31): hidden CS9335: The pattern is redundant.
            //         _ = a is [..{ Length: > bad }];
            Diagnostic(ErrorCode.HDN_RedundantPattern, "> bad").WithLocation(14, 31),
            // 0.cs(28,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //             [.. { Length: not < bad}]  => 2,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "[.. { Length: not < bad}]").WithLocation(28, 13));
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
    public void ListPattern_NullableValueType()
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
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

    [Fact, WorkItem(65876, "https://github.com/dotnet/roslyn/issues/65876")]
    public void ListPattern_Negated_03()
    {
        var source = """
using System;
public class C
{
    static void Main() 
    {
        Console.WriteLine(M1(new[]{1,2}));
        Console.WriteLine(M1(new[]{2,1}));
        Console.WriteLine(M1(new[]{1}));
        Console.WriteLine(M1(new[]{0}));
        
        Console.WriteLine(M2(new[]{1,2}));
        Console.WriteLine(M2(new[]{2,1}));
        Console.WriteLine(M2(new[]{1}));
        Console.WriteLine(M2(new[]{0}));
    }
    
    public static bool M1(int[] a) {
        return a is not ([1,2,..] or [..,2,1] or [1]);
    }
    public static bool M2(int[] a) {
        return !(a is ([1,2,..] or [..,2,1] or [1]));
    }
}
""";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: @"
False
False
False
True
False
False
False
True
");
    }

    [Fact]
    public void ListPattern_Negated_04()
    {
        var source = """
using System;
public class C
{
    static void Main() 
    {
        Console.WriteLine(M1(new[]{1,2}));
        Console.WriteLine(M1(new[]{2,1}));
        Console.WriteLine(M1(new[]{1}));
        Console.WriteLine(M1(new[]{0}));
        
        Console.WriteLine(M2(new[]{1,2}));
        Console.WriteLine(M2(new[]{2,1}));
        Console.WriteLine(M2(new[]{1}));
        Console.WriteLine(M2(new[]{0}));
    }
    
    public static int M1(int[] a) {
        return a switch 
        {
            not ([1,2,..] or [..,2,1] or [1]) => 1, 
            [1,2,..] => 2,
            [..,2,1] => 3,
            [1] => 4,
        };
    }
    public static int M2(int[] a) {
        switch (a) 
        {
            case not ([1,2,..] or [..,2,1] or [1]):
                return 1; 
            case [1,2,..]:
                return 2;
            case [..,2,1]:
                return 3;
            case [1]:
                return 4;
        }
    }
}
""";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: @"
2
3
4
1
2
3
4
1
");
    }

    [Fact]
    public void ListPattern_Negated_05()
    {
        var source = """
using System;
public class C
{
    static void Main() 
    {
        Console.WriteLine(M1(new[]{1,2}, new[]{1}));
        Console.WriteLine(M1(new[]{1}, new[]{2,1}));
        Console.WriteLine(M1(new[]{2,1}, new[]{1,2}));
        Console.WriteLine(M1(new[]{0}, new[]{0}));

        Console.WriteLine(M2(new[]{1,2}, new[]{1}));
        Console.WriteLine(M2(new[]{1}, new[]{2,1}));
        Console.WriteLine(M2(new[]{2,1}, new[]{1,2}));
        Console.WriteLine(M2(new[]{0}, new[]{0}));
    }
    
    public static int M1(int[] a, int[] b) {
        return (a, b) switch 
        {
            (not ([1,2,..] or [..,2,1] or [1]),
             not ([1,2,..] or [..,2,1] or [1])) => 1, 
            ([1,2,..] or [1], [..,2,1] or [1]) => 2,
            ([..,2,1], [1,2,..]) => 3,
            _ => 0
        };
    }
    public static int M2(int[] a, int[] b) {
        switch (a, b) 
        {
            case (not ([1,2,..] or [..,2,1] or [1]),
                  not ([1,2,..] or [..,2,1] or [1])):
                return 1; 
            case ([1,2,..] or [1], [..,2,1] or [1]):
                return 2;
            case ([..,2,1], [1,2,..]):
                return 3;
            default:
                return 0;
        }
    }
}
""";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: @"
2
2
3
1
2
2
3
1
");
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
            // (6,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         _ = c is [var item];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[var item]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 18),
            // (7,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         _ = c is [..var rest];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[..var rest]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18),
            // (7,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         _ = c is [..var rest];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[..var rest]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18),
            // (7,19): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         _ = c is [..var rest];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "..var rest").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 19),
            // (7,19): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         _ = c is [..var rest];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "..var rest").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 19),
            // (8,21): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         var index = c[^1];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 21),
            // (8,21): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         var index = c[^1];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 21),
            // (9,21): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         var range = c[1..^1];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[1..^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(9, 21),
            // (9,21): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //         var range = c[1..^1];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[1..^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(9, 21)
            );

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
        var declarations = tree.GetRoot().DescendantNodes().OfType<VarPatternSyntax>().ToArray();

        verify(declarations[0], "item", "Missing?");
        verify(declarations[1], "rest", "Missing?");

        var localDeclarations = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
        verify2(localDeclarations[0], "index", "Missing");
        verify2(localDeclarations[1], "range", "Missing");

        void verify(VarPatternSyntax declaration, string name, string expectedType)
        {
            var local = (ILocalSymbol)model.GetDeclaredSymbol(declaration.Designation)!;
            Assert.Equal(name, local.Name);
            Assert.Equal(expectedType, local.Type.ToTestDisplayString(includeNonNullable: true));
        }

        void verify2(VariableDeclaratorSyntax declaration, string name, string expectedType)
        {
            var local = (ILocalSymbol)model.GetDeclaredSymbol(declaration)!;
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
    public int Length => 1;
    public Missing this[Index i] => throw null;
    public Missing this[Range r] => throw null;
    public int this[int i] => 42;
    public int Slice(int i, int j) => 43;
}
";
        var lib2Ref = CreateCompilation(new[] { lib2_cs, TestSources.Index, TestSources.Range }, references: new[] { missingRef })
            .EmitToImageReference();

        var source = @"
var c = new C();

if (c is [var item] && c is [..var slice])
{
    var item2 = c[^1];
    var slice2 = c[..];
    System.Console.Write((item, slice, item2, slice2));
}
";
        var compilation = CreateCompilation(source, references: new[] { lib2Ref });
        compilation.VerifyEmitDiagnostics(
            // (4,10): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // if (c is [var item] && c is [..var slice])
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[var item]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 10),
            // (4,29): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // if (c is [var item] && c is [..var slice])
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[..var slice]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 29),
            // (4,30): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // if (c is [var item] && c is [..var slice])
            Diagnostic(ErrorCode.ERR_NoTypeDef, "..var slice").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 30),
            // (6,17): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //     var item2 = c[^1];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 17),
            // (7,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //     var slice2 = c[..];
            Diagnostic(ErrorCode.ERR_NoTypeDef, "c[..]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18)
            );
    }

    [Fact]
    public void ListPattern_RangeTypeWithMissingInterface()
    {
        var missing_cs = @"
public interface IMissing { }
";

        var range_cs = @"
namespace System;

public struct Range : IMissing
{
    public Index Start { get; }
    public Index End { get; }
    public Range(Index start, Index end) => throw null;
    public static Range StartAt(Index start) => throw null;
    public static Range EndAt(Index end) => throw null;
    public static Range All => throw null;
}
";

        var lib_cs = @"
public class C
{
    public int Length => 0;
    public int this[System.Index i] => 0;
    public int this[System.Range r] => 0;
}
";

        var missingComp = CreateCompilation(missing_cs, assemblyName: "missing");
        missingComp.VerifyDiagnostics();

        var rangeComp = CreateCompilation(new[] { range_cs, TestSources.Index }, references: new[] { missingComp.EmitToImageReference() }, assemblyName: "range");
        rangeComp.VerifyDiagnostics();
        var rangeRef = rangeComp.EmitToImageReference();

        var libComp = CreateCompilation(lib_cs, references: new[] { rangeRef }, assemblyName: "lib");
        libComp.VerifyDiagnostics();

        var sources = new[]
        {
            "_ = ^1;",
            "_ = ..;",
            "_ = new C() is [var x];",
            "_ = new C() is [..var y];",
            "_ = new C()[^1];",
            "_ = new C()[..];"
        };

        foreach (var source in sources)
        {
            var comp = CreateCompilation(source, references: new[] { libComp.EmitToImageReference(), rangeRef });
            comp.VerifyDiagnostics();
            var used = comp.GetUsedAssemblyReferences();
            Assert.True(used.Any(r => r.Display == "range"));
        }
    }

    [Fact]
    public void ListPattern_ObsoleteLengthAndIndexerAndSlice()
    {
        var source = @"
_ = new C() is [var x]; // 1, 2, 3
_ = new C() is [.. var y]; // 4, 5, 6, 7, 8
new C().Slice(0, 0); // 9
_ = new C()[^1]; // 10, 11
_ = new C()[..]; // 12, 13

class C
{
    [System.Obsolete]
    public int Length => 0;

    [System.Obsolete]
    public int this[int i] => 0;

    [System.Obsolete]
    public int Slice(int i, int j) => 0;
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        // Note: duplicate diagnostics are reported on Length because both the list pattern
        //   and the implicit indexer need it.
        comp.VerifyDiagnostics(
            // (2,16): warning CS0612: 'C.Length' is obsolete
            // _ = new C() is [var x]; // 1, 2, 3
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[var x]").WithArguments("C.Length").WithLocation(2, 16),
            // (2,16): warning CS0612: 'C.Length' is obsolete
            // _ = new C() is [var x]; // 1, 2, 3
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[var x]").WithArguments("C.Length").WithLocation(2, 16),
            // (2,16): warning CS0612: 'C.this[int]' is obsolete
            // _ = new C() is [var x]; // 1, 2, 3
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[var x]").WithArguments("C.this[int]").WithLocation(2, 16),
            // (3,16): warning CS0612: 'C.Length' is obsolete
            // _ = new C() is [.. var y]; // 4, 5, 6, 7, 8
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[.. var y]").WithArguments("C.Length").WithLocation(3, 16),
            // (3,16): warning CS0612: 'C.Length' is obsolete
            // _ = new C() is [.. var y]; // 4, 5, 6, 7, 8
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[.. var y]").WithArguments("C.Length").WithLocation(3, 16),
            // (3,16): warning CS0612: 'C.this[int]' is obsolete
            // _ = new C() is [.. var y]; // 4, 5, 6, 7, 8
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[.. var y]").WithArguments("C.this[int]").WithLocation(3, 16),
            // (3,17): warning CS0612: 'C.Length' is obsolete
            // _ = new C() is [.. var y]; // 4, 5, 6, 7, 8
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, ".. var y").WithArguments("C.Length").WithLocation(3, 17),
            // (3,17): warning CS0612: 'C.Slice(int, int)' is obsolete
            // _ = new C() is [.. var y]; // 4, 5, 6, 7, 8
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, ".. var y").WithArguments("C.Slice(int, int)").WithLocation(3, 17),
            // (4,1): warning CS0612: 'C.Slice(int, int)' is obsolete
            // new C().Slice(0, 0); // 9
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C().Slice(0, 0)").WithArguments("C.Slice(int, int)").WithLocation(4, 1),
            // (5,5): warning CS0612: 'C.Length' is obsolete
            // _ = new C()[^1]; // 10, 11
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C()[^1]").WithArguments("C.Length").WithLocation(5, 5),
            // (5,5): warning CS0612: 'C.this[int]' is obsolete
            // _ = new C()[^1]; // 10, 11
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C()[^1]").WithArguments("C.this[int]").WithLocation(5, 5),
            // (6,5): warning CS0612: 'C.Length' is obsolete
            // _ = new C()[..]; // 12, 13
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C()[..]").WithArguments("C.Length").WithLocation(6, 5),
            // (6,5): warning CS0612: 'C.Slice(int, int)' is obsolete
            // _ = new C()[..]; // 12, 13
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C()[..]").WithArguments("C.Slice(int, int)").WithLocation(6, 5)
            );
    }

    [Fact]
    public void ListPattern_IndexAndSliceReturnMissingTypes()
    {
        var missing_cs = @"
public class Missing { }
public class Missing2 { }
";

        var lib_cs = @"
public class C
{
    public int Length => throw null;
    public Missing this[int i] => throw null;
    public Missing2 Slice(int i, int j) => throw null;
}
";

        var source = @"
_ = new C() is [var x]; // 1, 2
_ = new C() is [.. var y]; // 3, 4, 5, 6
new C().Slice(0, 0); // 7
_ = new C()[^1]; // 8, 9
_ = new C()[..]; // 10, 11
";
        var missingComp = CreateCompilation(missing_cs, assemblyName: "missing");
        missingComp.VerifyDiagnostics();

        var libComp = CreateCompilation(lib_cs, references: new[] { missingComp.EmitToImageReference() }, assemblyName: "lib");
        libComp.VerifyDiagnostics();

        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range }, references: new[] { libComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (2,16): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // _ = new C() is [var x]; // 1, 2
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[var x]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 16),
            // (2,16): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // _ = new C() is [var x]; // 1, 2
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[var x]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 16),
            // (3,16): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // _ = new C() is [.. var y]; // 3, 4, 5, 6
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[.. var y]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(3, 16),
            // (3,16): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // _ = new C() is [.. var y]; // 3, 4, 5, 6
            Diagnostic(ErrorCode.ERR_NoTypeDef, "[.. var y]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(3, 16),
            // (3,17): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // _ = new C() is [.. var y]; // 3, 4, 5, 6
            Diagnostic(ErrorCode.ERR_NoTypeDef, ".. var y").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(3, 17),
            // (3,17): error CS0012: The type 'Missing2' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // _ = new C() is [.. var y]; // 3, 4, 5, 6
            Diagnostic(ErrorCode.ERR_NoTypeDef, ".. var y").WithArguments("Missing2", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(3, 17),
            // (4,1): error CS0012: The type 'Missing2' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // new C().Slice(0, 0); // 7
            Diagnostic(ErrorCode.ERR_NoTypeDef, "new C().Slice").WithArguments("Missing2", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 1),
            // (5,5): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // _ = new C()[^1]; // 8, 9
            Diagnostic(ErrorCode.ERR_NoTypeDef, "new C()[^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(5, 5),
            // (5,5): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // _ = new C()[^1]; // 8, 9
            Diagnostic(ErrorCode.ERR_NoTypeDef, "new C()[^1]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(5, 5),
            // (6,5): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // _ = new C()[..]; // 10, 11
            Diagnostic(ErrorCode.ERR_NoTypeDef, "new C()[..]").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 5),
            // (6,5): error CS0012: The type 'Missing2' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // _ = new C()[..]; // 10, 11
            Diagnostic(ErrorCode.ERR_NoTypeDef, "new C()[..]").WithArguments("Missing2", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 5)
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
        compilation.VerifyDiagnostics(
            // 0.cs(8,26): hidden CS9335: The pattern is redundant.
            //         _ = integers is [{}];
            Diagnostic(ErrorCode.HDN_RedundantPattern, "{}").WithLocation(8, 26));

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
    public void ListPattern_Symbols_WithoutIndexOrRangeOrGetSubArray()
    {
        var source = @"
#nullable enable
class X
{
    public void Test(int[] integers)
    {
        _ = integers is [var item, ..var slice];
    }
}";
        var compilation = CreateCompilation(source);
        compilation.VerifyDiagnostics(
            // (7,25): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         _ = integers is [var item, ..var slice];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[var item, ..var slice]").WithArguments("System.Index").WithLocation(7, 25),
            // (7,36): error CS0518: Predefined type 'System.Range' is not defined or imported
            //         _ = integers is [var item, ..var slice];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "..var slice").WithArguments("System.Range").WithLocation(7, 36)
            );

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var designations = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();

        var itemDesignation = designations[0];
        Assert.Equal("item", itemDesignation.ToString());

        var symbol = model.GetDeclaredSymbol(itemDesignation);
        Assert.Equal(SymbolKind.Local, symbol.Kind);
        Assert.Equal("System.Int32", ((ILocalSymbol)symbol).Type.ToTestDisplayString());

        var typeInfo = model.GetTypeInfo(itemDesignation);
        Assert.Null(typeInfo.Type);
        Assert.Null(typeInfo.ConvertedType);

        typeInfo = model.GetTypeInfo(itemDesignation.Parent);
        Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
        Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

        var sliceDesignation = designations[1];
        Assert.Equal("slice", sliceDesignation.ToString());

        symbol = model.GetDeclaredSymbol(sliceDesignation);
        Assert.Equal(SymbolKind.Local, symbol.Kind);
        Assert.Equal("System.Int32", ((ILocalSymbol)symbol).Type.ToTestDisplayString());

        typeInfo = model.GetTypeInfo(sliceDesignation);
        Assert.Null(typeInfo.Type);
        Assert.Null(typeInfo.ConvertedType);

        typeInfo = model.GetTypeInfo(sliceDesignation.Parent);
        Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
        Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
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
        var comp = CreateCompilationWithIndexAndRange(src);
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
            Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Slice(int, int)", "this").WithLocation(12, 13)
            );
    }

    [Fact]
    public void PatternIndexRangeReadOnly_02()
    {
        var src = @"
using System;
struct S
{
    public readonly int this[int i] => 0;
    public readonly int Length => 0;
    public readonly int Slice(int x, int y) => 0;

    readonly void M(Index i, Range r)
    {
        _ = this[i];
        _ = this[r];

        _ = this is [1];
        _ = this is [2, ..var rest];
    }
}";
        var comp = CreateCompilationWithIndexAndRange(src);
        comp.VerifyDiagnostics();
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
        _ = this[^1];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index });
        compilation.VerifyEmitDiagnostics(
            // (4,17): error CS0547: 'C.Length': property or indexer cannot have void type
            //     public void Length => throw null;
            Diagnostic(ErrorCode.ERR_PropertyCantHaveVoidType, "Length").WithArguments("C.Length").WithLocation(4, 17),
            // (8,21): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            //         _ = this is [1];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[1]").WithArguments("C").WithLocation(8, 21),
            // (8,21): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            //         _ = this is [1];
            Diagnostic(ErrorCode.ERR_BadArgType, "[1]").WithArguments("1", "System.Index", "int").WithLocation(8, 21),
            // (9,18): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(9, 18)
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index });
        compilation.VerifyEmitDiagnostics(
            // (9,21): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            //         _ = this is [1];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[1]").WithArguments("C").WithLocation(9, 21),
            // (9,21): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            //         _ = this is [1];
            Diagnostic(ErrorCode.ERR_BadArgType, "[1]").WithArguments("1", "System.Index", "int").WithLocation(9, 21),
            // (10,18): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(10, 18)
            );
    }

    [Fact]
    public void ListPattern_StringLength_SystemIndexIndexer()
    {
        var source = @"
class C
{
    public string Length => throw null;
    public int this[System.Index i] => throw null;

    public void M()
    {
        _ = this is [1];
        _ = this[^1];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index });
        compilation.VerifyEmitDiagnostics(
            // (9,21): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            //         _ = this is [1];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[1]").WithArguments("C").WithLocation(9, 21)
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
        _ = this[..];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // (11,22): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            //         _ = this is [.._];
            Diagnostic(ErrorCode.ERR_BadArgType, ".._").WithArguments("1", "System.Range", "int").WithLocation(11, 22),
            // (12,22): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            //         _ = this is [..var unused];
            Diagnostic(ErrorCode.ERR_BadArgType, "..var unused").WithArguments("1", "System.Range", "int").WithLocation(12, 22),
            // (13,22): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            //         if (this is [..var used])
            Diagnostic(ErrorCode.ERR_BadArgType, "..var used").WithArguments("1", "System.Range", "int").WithLocation(13, 22),
            // (17,18): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            //         _ = this[..];
            Diagnostic(ErrorCode.ERR_BadArgType, "..").WithArguments("1", "System.Range", "int").WithLocation(17, 18)
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
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

        if (new C<int?>() is [var item2]) // 2
            item2.Value.ToString(); // 3
        var item22 = new C<int?>()[^1]; // 4
        item22.Value.ToString(); // 5

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
            // (10,29): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            //         if (new C<int>() is [var item]) // 1
            Diagnostic(ErrorCode.ERR_BadArgType, "[var item]").WithArguments("1", "System.Index", "int").WithLocation(10, 29),
            // (13,30): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int?'
            //         if (new C<int?>() is [var item2]) // 2
            Diagnostic(ErrorCode.ERR_BadArgType, "[var item2]").WithArguments("1", "System.Index", "int?").WithLocation(13, 30),
            // (14,19): error CS1061: 'int' does not contain a definition for 'Value' and no accessible extension method 'Value' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
            //             item2.Value.ToString(); // 3
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Value").WithArguments("int", "Value").WithLocation(14, 19),
            // (15,36): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int?'
            //         var item22 = new C<int?>()[^1]; // 4
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int?").WithLocation(15, 36),
            // (16,16): error CS1061: 'int' does not contain a definition for 'Value' and no accessible extension method 'Value' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
            //         item22.Value.ToString(); // 5
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Value").WithArguments("int", "Value").WithLocation(16, 16)
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index });
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index });
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
    public void ListPattern_Nullability_MaybeNullReceiver()
    {
        var source = @"
#nullable enable
class C<T>
{
    public int Length => throw null!;
    public T this[int i] => throw null!;

    public void M(C<int>? c)
    {
        if (c is [var item])
            item.ToString();

        _ = c[^1];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index });
        compilation.VerifyEmitDiagnostics(
            // (13,13): warning CS8602: Dereference of a possibly null reference.
            //         _ = c[^1];
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(13, 13)
            );
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
            rest2.Value.ToString(); // (assumed not-null)
        else
            rest2.Value.ToString(); // 2, 3

        if (new C<string?>() is [1, ..var rest3])
            rest3.ToString(); // (assumed not-null)
        else
            rest3.ToString(); // 4, 5

        if (new C<string>() is [1, ..var rest4])
        {
            rest4.ToString();
            rest4 = null;
        }
        else
            rest4.ToString(); // 6, 7

        if (new C<T>() is [1, ..var rest5])
        {
            rest5.ToString(); // (assumed not-null)
            rest5 = default;
        }
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // (14,13): error CS0165: Use of unassigned local variable 'rest'
            //             rest.ToString(); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest").WithArguments("rest").WithLocation(14, 13),
            // (19,13): warning CS8629: Nullable value type may be null.
            //             rest2.Value.ToString(); // 2, 3
            Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "rest2").WithLocation(19, 13),
            // (19,13): error CS0165: Use of unassigned local variable 'rest2'
            //             rest2.Value.ToString(); // 2, 3
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest2").WithArguments("rest2").WithLocation(19, 13),
            // (24,13): warning CS8602: Dereference of a possibly null reference.
            //             rest3.ToString(); // 4, 5
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest3").WithLocation(24, 13),
            // (24,13): error CS0165: Use of unassigned local variable 'rest3'
            //             rest3.ToString(); // 4, 5
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest3").WithArguments("rest3").WithLocation(24, 13),
            // (32,13): warning CS8602: Dereference of a possibly null reference.
            //             rest4.ToString(); // 6, 7
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest4").WithLocation(32, 13),
            // (32,13): error CS0165: Use of unassigned local variable 'rest4'
            //             rest4.ToString(); // 6, 7
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest4").WithArguments("rest4").WithLocation(32, 13)
            );

        var tree = compilation.SyntaxTrees.First();
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
    public void SlicePattern_Nullability_Annotation()
    {
        var source = @"
#nullable enable
class C
{
    public int Length => throw null!;
    public int this[int i] => throw null!;
    public int[]? Slice(int i, int j) => throw null!;

    public void M()
    {
        if (this is [1, ..var slice])
            slice.ToString();
        if (this is [1, ..[] list])
            list.ToString();
    }
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics();

        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
        var nodes = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>();
        Assert.Collection(nodes,
            d => verify(d, "slice", "int[]?", "int[]"),
            d => verify(d, "list", "int[]?", "int[]")
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
            rest2.Value.ToString(); // (assumed not-null)
        else
            rest2.Value.ToString(); // 2, 3

        if (new C<string?>() is [1, ..var rest3])
            rest3.ToString(); // (assumed not-null)
        else
            rest3.ToString(); // 4, 5

        if (new C<string>() is [1, ..var rest4])
        {
            rest4.ToString();
            rest4 = null;
        }
        else
            rest4.ToString(); // 6, 7
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // (15,13): error CS0165: Use of unassigned local variable 'rest'
            //             rest.ToString(); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest").WithArguments("rest").WithLocation(15, 13),
            // (20,13): warning CS8629: Nullable value type may be null.
            //             rest2.Value.ToString(); // 2, 3
            Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "rest2").WithLocation(20, 13),
            // (20,13): error CS0165: Use of unassigned local variable 'rest2'
            //             rest2.Value.ToString(); // 2, 3
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest2").WithArguments("rest2").WithLocation(20, 13),
            // (25,13): warning CS8602: Dereference of a possibly null reference.
            //             rest3.ToString(); // 4, 5
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest3").WithLocation(25, 13),
            // (25,13): error CS0165: Use of unassigned local variable 'rest3'
            //             rest3.ToString(); // 4, 5
            Diagnostic(ErrorCode.ERR_UseDefViolation, "rest3").WithArguments("rest3").WithLocation(25, 13),
            // (33,13): warning CS8602: Dereference of a possibly null reference.
            //             rest4.ToString(); // 6, 7
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "rest4").WithLocation(33, 13),
            // (33,13): error CS0165: Use of unassigned local variable 'rest4'
            //             rest4.ToString(); // 6, 7
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
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

        var tree = compilation.SyntaxTrees.First();
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
    public void SlicePattern_Nullability_MaybeNullReceiver()
    {
        var source = @"
#nullable enable
class C<T>
{
    public int Length => throw null!;
    public T this[int i] => throw null!;
    public T Slice(int i, int j) => throw null!;

    public void M(C<int>? c)
    {
        if (c is [.. var item])
            item.ToString();

        _ = c[..];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // (14,13): warning CS8602: Dereference of a possibly null reference.
            //         _ = c[..];
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(14, 13)
            );
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
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
        _ = new C()[^1];
        _ = new C()[..];
    }
}
";
        var compilation = CreateCompilationWithIL(new[] { source, TestSources.Index, TestSources.Range }, il);
        compilation.VerifyEmitDiagnostics(
            // (6,24): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            //         _ = new C() is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[var item, ..var rest]").WithArguments("C").WithLocation(6, 24),
            // (6,24): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            //         _ = new C() is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[var item, ..var rest]").WithArguments("C").WithLocation(6, 24),
            // (6,35): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            //         _ = new C() is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "..var rest").WithArguments("C").WithLocation(6, 35),
            // (7,13): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            //         _ = new C()[^1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "new C()[^1]").WithArguments("C").WithLocation(7, 13),
            // (8,13): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            //         _ = new C()[..];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "new C()[..]").WithArguments("C").WithLocation(8, 13)
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
            // (6,24): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            //         _ = new C() is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[var item, ..var rest]").WithArguments("C").WithLocation(6, 24),
            // (6,24): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            //         _ = new C() is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[var item, ..var rest]").WithArguments("C").WithLocation(6, 24),
            // (6,35): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            //         _ = new C() is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "..var rest").WithArguments("C").WithLocation(6, 35)
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
    [not null] => 0, // 1
    { Length: 0 or > 1 } => 0, // 2
};

_ = o switch // didn't test for null // 3
{
    [null] => 0,
    [not null] => 0, // 4
    { Length: 0 or > 1 } => 0, // 5
};

_ = o switch // didn't test for [null] // 6
{
    null => 0,
    [not null] => 0,
    { Length: 0 or > 1 } => 0,
};

_ = o switch // didn't test for [not null] // 7
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
    [.., not null] => 0, // 8
};

_ = o switch // didn't test for [null] // 9
{
    null => 0,
    [] => 0,
    [.., not null] => 0,
};

_ = o switch // didn't test for [not null] // 10
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
    { Length: 0 or > 1 } => 0, // 11
};

_ = o switch // didn't test for [_, null] // 12
{
    null => 0,
    [] => 0,
    [_] => 0,
    [.., not null] => 0,
};

_ = o switch // didn't test for [null, _] // 13
{
    null => 0,
    [] => 0,
    [_] => 0,
    [not null, ..] => 0,
};

_ = o switch // didn't test for { Length: 0 } // 14
{
    null => 0,
    [_, ..] => 0,
};

_ = o switch // didn't test for [null] // 15
{
    null => 0,
    [] => 0,
    [..var x, not null] => 0,
};
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
        compilation.VerifyEmitDiagnostics(
            // 0.cs(9,10): hidden CS9335: The pattern is redundant.
            //     [not null] => 0, // 1
            Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(9, 10),
            // 0.cs(10,15): hidden CS9335: The pattern is redundant.
            //     { Length: 0 or > 1 } => 0, // 2
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0 or > 1").WithLocation(10, 15),
            // 0.cs(13,7): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
            // _ = o switch // didn't test for null // 3
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(13, 7),
            // 0.cs(16,10): hidden CS9335: The pattern is redundant.
            //     [not null] => 0, // 4
            Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(16, 10),
            // 0.cs(17,15): hidden CS9335: The pattern is redundant.
            //     { Length: 0 or > 1 } => 0, // 5
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0 or > 1").WithLocation(17, 15),
            // 0.cs(20,7): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '[null]' is not covered.
            // _ = o switch // didn't test for [null] // 6
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("[null]").WithLocation(20, 7),
            // 0.cs(27,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '[not null]' is not covered.
            // _ = o switch // didn't test for [not null] // 7
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("[not null]").WithLocation(27, 7),
            // 0.cs(39,14): hidden CS9335: The pattern is redundant.
            //     [.., not null] => 0, // 8
            Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(39, 14),
            // 0.cs(42,7): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '[null]' is not covered.
            // _ = o switch // didn't test for [null] // 9
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("[null]").WithLocation(42, 7),
            // 0.cs(49,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '[not null]' is not covered.
            // _ = o switch // didn't test for [not null] // 10
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("[not null]").WithLocation(49, 7),
            // 0.cs(61,15): hidden CS9335: The pattern is redundant.
            //     { Length: 0 or > 1 } => 0, // 11
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0 or > 1").WithLocation(61, 15),
            // 0.cs(64,7): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '[_, null]' is not covered.
            // _ = o switch // didn't test for [_, null] // 12
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("[_, null]").WithLocation(64, 7),
            // 0.cs(72,7): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '[null, _]' is not covered.
            // _ = o switch // didn't test for [null, _] // 13
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("[null, _]").WithLocation(72, 7),
            // 0.cs(80,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Length: 0 }' is not covered.
            // _ = o switch // didn't test for { Length: 0 } // 14
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Length: 0 }").WithLocation(80, 7),
            // 0.cs(86,7): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '[null]' is not covered.
            // _ = o switch // didn't test for [null] // 15
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("[null]").WithLocation(86, 7)
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
            [.. not null] => 0, // 1
        };

        _ = this switch // no tests for [.. null, _]
        {
            null => 0,
            [] => 0,
            [.. not null] => 0,
        };

        _ = this switch // no test for [.. not null, _]
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // 0.cs(17,21): hidden CS9335: The pattern is redundant.
            //             [.. not null] => 0, // 1
            Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(17, 21),
            // 0.cs(20,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '[.. null, _]' is not covered.
            //         _ = this switch // no tests for [.. null, _]
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("[.. null, _]").WithLocation(20, 18),
            // 0.cs(27,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '[.. not null, _]' is not covered.
            //         _ = this switch // no test for [.. not null, _]
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("[.. not null, _]").WithLocation(27, 18)
            );
    }

    [Fact]
    public void SlicePattern_Nullability_Exhaustiveness_WithNestedPropertyTest()
    {
        var source = @"
#nullable enable
using System;
class C
{
    public int Length => throw null!;
    public D? this[Index i] => throw null!;
    public D? this[Range r] => throw null!;

    public void M()
    {
        _ = this switch
        {
            null => 0,
            [] => 0,
            [_, _, ..] => 0,
            [.. null] => 0,
            [.. { Property: < 0 }] => 0,
        };
    }
}

class D
{
    public int Property { get; set; }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // (12,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(12, 18)
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
            null or { Length: not 1 } => 0,
            [.. null] => 0,
            [.. [null]] => 0,
            [not null] => 0, // 1
        };

        _ = this switch // didn't test for [.. null] but the slice is assumed not-null
        {
            null or { Length: not 1 } => 0,
            [.. [null]] => 0,
            [not null] => 0, // 2
        };

        _ = this switch // didn't test for [.. [not null]] // 3
        {
            null or { Length: not 1 } => 0,
            [.. [null]] => 0,
        };

        _ = this switch // didn't test for [.. [not null]] // 4
        {
            null or { Length: not 1 } => 0,
            [.. null] => 0,
            [.. [null]] => 0,
        };

        _ = this switch // didn't test for [.. null, _] // we're trying to construct an example with Length=1, the slice may not be null // 5
        {
            null or { Length: not 1 } => 0,
            [.. [not null]] => 0,
        };

        _ = this switch // didn't test for [_, .. null, _, _, _] // we're trying to construct an example with Length=4, the slice may not be null // 6
        {
            null or { Length: not 4 } => 0,
            [_, .. [_, not null], _] => 0,
        };

        _ = this switch // exhaustive
        {
            null or { Length: not 4 } => 0,
            [_, .. [_, _], _] => 0, // 7
        };

        _ = this switch // didn't test for [_, .. [_, null], _] // 8
        {
            null or { Length: not 4 } => 0,
            [_, .. null or [_, not null], _] => 0,
        };

        _ = this switch // didn't test for [_, .. [_, null], _, _] // 9
        {
            null or { Length: not 5 } => 0,
            [_, .. null or [_, not null], _, _] => 0,
        };

        _ = this switch // didn't test for [_, .. [_, null, _], _] // 10
        {
            null or { Length: not 5 } => 0,
            [_, .. null or [_, not null, _], _] => 0,
        };

        _ = this switch // didn't test for [.. null, _] but the slice is assumed not-null
        {
            null or { Length: not 1 } => 0,
            [.. { Length: 1 }] => 0, // 11
        };
    }
}
";
        // Note: we don't try to explain nested slice patterns right now so all these just produce a fallback example
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // 0.cs(17,18): hidden CS9335: The pattern is redundant.
            //             [not null] => 0, // 1
            Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(17, 18),
            // 0.cs(24,18): hidden CS9335: The pattern is redundant.
            //             [not null] => 0, // 2
            Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(24, 18),
            // 0.cs(27,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch // didn't test for [.. [not null]] // 3
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(27, 18),
            // 0.cs(33,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch // didn't test for [.. [not null]] // 4
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(33, 18),
            // 0.cs(40,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch // didn't test for [.. null, _] // we're trying to construct an example with Length=1, the slice may not be null // 5
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("_").WithLocation(40, 18),
            // 0.cs(46,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch // didn't test for [_, .. null, _, _, _] // we're trying to construct an example with Length=4, the slice may not be null // 6
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("_").WithLocation(46, 18),
            // 0.cs(55,20): hidden CS9335: The pattern is redundant.
            //             [_, .. [_, _], _] => 0, // 7
            Diagnostic(ErrorCode.HDN_RedundantPattern, "[_, _]").WithLocation(55, 20),
            // 0.cs(58,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch // didn't test for [_, .. [_, null], _] // 8
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("_").WithLocation(58, 18),
            // 0.cs(64,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch // didn't test for [_, .. [_, null], _, _] // 9
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("_").WithLocation(64, 18),
            // 0.cs(70,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '_' is not covered.
            //         _ = this switch // didn't test for [_, .. [_, null, _], _] // 10
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("_").WithLocation(70, 18),
            // 0.cs(79,27): hidden CS9335: The pattern is redundant.
            //             [.. { Length: 1 }] => 0, // 11
            Diagnostic(ErrorCode.HDN_RedundantPattern, "1").WithLocation(79, 27)
            );
    }

    [Fact]
    public void SlicePattern_Nullability_Exhaustiveness_Multiple()
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
            [1, .., 2, .., 3] => 0,
            { Length: > 1 } => 0,
        };
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // (16,24): error CS9202: Slice patterns may only be used once and directly inside a list pattern.
            //             [1, .., 2, .., 3] => 0,
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(16, 24)
            );
    }

    [Fact]
    public void ListPattern_Dynamic()
    {
        var source = @"
#nullable enable
class C
{
    void M(dynamic d)
    {
        _ = d is [_, .._];
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index });
        compilation.VerifyEmitDiagnostics(
            // (7,18): error CS8979: List patterns may not be used for a value of type 'dynamic'.
            //         _ = d is [_, .._];
            Diagnostic(ErrorCode.ERR_UnsupportedTypeForListPattern, "[_, .._]").WithArguments("dynamic").WithLocation(7, 18),
            // (7,22): error CS0518: Predefined type 'System.Range' is not defined or imported
            //         _ = d is [_, .._];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, ".._").WithArguments("System.Range").WithLocation(7, 22)
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        compilation.VerifyEmitDiagnostics(
            // (5,21): error CS0631: ref and out are not valid in this context
            //     public int this[ref int i] => 0;
            Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(5, 21),
            // (10,21): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_BadArgRef, "[var item, ..var rest]").WithArguments("1", "ref").WithLocation(10, 21),
            // (10,32): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_BadArgRef, "..var rest").WithArguments("1", "ref").WithLocation(10, 32),
            // (11,18): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_BadArgRef, "^1").WithArguments("1", "ref").WithLocation(11, 18),
            // (12,18): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_BadArgRef, "1..^1").WithArguments("1", "ref").WithLocation(12, 18)
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
            // (11,21): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_BadArgRef, "[var item, ..var rest]").WithArguments("1", "ref").WithLocation(11, 21),
            // (11,32): error CS1620: Argument 1 must be passed with the 'ref' keyword
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_BadArgRef, "..var rest").WithArguments("1", "ref").WithLocation(11, 32),
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index });
        compilation.VerifyEmitDiagnostics(
            // (10,21): error CS1503: Argument 1: cannot convert from 'System.Index' to 'in int'
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_BadArgType, "[var item, ..var rest]").WithArguments("1", "System.Index", "in int").WithLocation(10, 21),
            // (10,32): error CS0518: Predefined type 'System.Range' is not defined or imported
            //         _ = this is [var item, ..var rest];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "..var rest").WithArguments("System.Range").WithLocation(10, 32),
            // (11,18): error CS1503: Argument 1: cannot convert from 'System.Index' to 'in int'
            //         _ = this[^1];
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "in int").WithLocation(11, 18),
            // (12,18): error CS0518: Predefined type 'System.Range' is not defined or imported
            //         _ = this[1..^1];
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1..^1").WithArguments("System.Range").WithLocation(12, 18)
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
        var compilation = CreateCompilation(new[] { source, TestSources.Index });
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
_ = new C()[..];

static class Extensions
{
    public static int Slice(this C c, int i, int j) => throw null;
}
class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
}";
        var comp = CreateCompilation(new[] { src, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics(
            // (2,17): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new C() is [..var y];
            Diagnostic(ErrorCode.ERR_BadArgType, "..var y").WithArguments("1", "System.Range", "int").WithLocation(2, 17),
            // (3,13): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = new C()[..];
            Diagnostic(ErrorCode.ERR_BadArgType, "..").WithArguments("1", "System.Range", "int").WithLocation(3, 13)
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
        CompileAndVerify(new[] { src, TestSources.Index, TestSources.Range }, expectedOutput: "(a, bc)");
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
    [ _, _, .. ] => 2,
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
    { Count: > 2 } => 3, // 4
};

class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
}";
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
            // 0.cs(2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 2 }' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 2 }").WithLocation(2, 13),
            // 0.cs(9,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            // _ = new C() switch // 2
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(9, 13),
            // 0.cs(16,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            // _ = new C() switch // 3
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(16, 13),
            // 0.cs(29,14): hidden CS9335: The pattern is redundant.
            //     { Count: > 2 } => 3, // 4
            Diagnostic(ErrorCode.HDN_RedundantPattern, "> 2").WithLocation(29, 14)
            );

        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 2 }' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 2 }").WithLocation(2, 13),
            // (5,5): error CS0518: Predefined type 'System.Index' is not defined or imported
            //     [_] => 1,
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[_]").WithArguments("System.Index").WithLocation(5, 5),
            // (5,5): error CS0656: Missing compiler required member 'System.Index.GetOffset'
            //     [_] => 1,
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[_]").WithArguments("System.Index", "GetOffset").WithLocation(5, 5),
            // (9,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            // _ = new C() switch // 2
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(9, 13),
            // (13,5): error CS0518: Predefined type 'System.Index' is not defined or imported
            //     [ _, _, .. ] => 2,
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[ _, _, .. ]").WithArguments("System.Index").WithLocation(13, 5),
            // (13,5): error CS0656: Missing compiler required member 'System.Index.GetOffset'
            //     [ _, _, .. ] => 2,
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[ _, _, .. ]").WithArguments("System.Index", "GetOffset").WithLocation(13, 5),
            // (16,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            // _ = new C() switch // 3
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(16, 13),
            // (20,5): error CS0518: Predefined type 'System.Index' is not defined or imported
            //     [_, _] => 2,
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[_, _]").WithArguments("System.Index").WithLocation(20, 5),
            // (20,5): error CS0656: Missing compiler required member 'System.Index.GetOffset'
            //     [_, _] => 2,
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[_, _]").WithArguments("System.Index", "GetOffset").WithLocation(20, 5),
            // (21,5): error CS0518: Predefined type 'System.Index' is not defined or imported
            //     [_, _, ..] => 3,
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[_, _, ..]").WithArguments("System.Index").WithLocation(21, 5),
            // (21,5): error CS0656: Missing compiler required member 'System.Index.GetOffset'
            //     [_, _, ..] => 3,
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[_, _, ..]").WithArguments("System.Index", "GetOffset").WithLocation(21, 5),
            // (28,5): error CS0518: Predefined type 'System.Index' is not defined or imported
            //     [_, _] => 2,
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[_, _]").WithArguments("System.Index").WithLocation(28, 5),
            // (28,5): error CS0656: Missing compiler required member 'System.Index.GetOffset'
            //     [_, _] => 2,
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[_, _]").WithArguments("System.Index", "GetOffset").WithLocation(28, 5),
            // (29,14): hidden CS9335: The pattern is redundant.
            //     { Count: > 2 } => 3, // 4
            Diagnostic(ErrorCode.HDN_RedundantPattern, "> 2").WithLocation(29, 14)
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
    [0] => 4, // 3
};

class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
}";
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
            // 0.cs(2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 0 }' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 0 }").WithLocation(2, 13),
            // 0.cs(8,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '[0]' is not covered.
            // _ = new C() switch // 2
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("[0]").WithLocation(8, 13),
            // 0.cs(20,6): hidden CS9335: The pattern is redundant.
            //     [0] => 4, // 3
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0").WithLocation(20, 6)
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
    { Count: 0 or > 1 } => 2, // 2
};

_ = new C() switch
{
    null => 0,
    [not null] => 1,
    [null] => 2, // 3
    { Count: 0 or > 1 } => 3, // 4, 5
};

_ = new C() switch // 6
{
    [not null] => 1,
    { Count: 0 or > 1 } => 2,
};

_ = new C() switch
{
    [not null] => 1,
    [null] => 2, // 7
    { Count: 0 or > 1 } => 3, // 8
};

class C
{
    public int Count => throw null!;
    public string? this[int i] => throw null!;
}";
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
            // 0.cs(3,13): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '[null]' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("[null]").WithLocation(3, 13),
            // 0.cs(14,6): hidden CS9335: The pattern is redundant.
            //     [null] => 2, // 2
            Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(14, 6),
            // 0.cs(15,14): hidden CS9335: The pattern is redundant.
            //     { Count: 0 or > 1 } => 3, // 3
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0 or > 1").WithLocation(15, 14),
            // 0.cs(18,13): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
            // _ = new C() switch // 4
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(18, 13),
            // 0.cs(27,6): hidden CS9335: The pattern is redundant.
            //     [null] => 2, // 5
            Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(27, 6),
            // 0.cs(28,14): hidden CS9335: The pattern is redundant.
            //     { Count: 0 or > 1 } => 3, // 6
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0 or > 1").WithLocation(28, 14)
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
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '[_, 0]' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("[_, 0]").WithLocation(2, 13)
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
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '[0, _]' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("[0, _]").WithLocation(2, 13)
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

_ = new C() switch // 2
{
    { Count: <= 2 } => 1,
    [.., > 0] => 2,
    [.., < 0] => 3,
};

_ = new C() switch // 3
{
    { Count: <= 2 } => 1,
    [0, ..] => 2,
    [.., > 0] => 3,
    [.., < 0] => 4,
};

class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
}";
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '[0]' is not covered.
            // _ = new C() switch // 1
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("[0]").WithLocation(2, 13),
            // (9,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '[_, _, 0]' is not covered.
            // _ = new C() switch // 2
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("[_, _, 0]").WithLocation(9, 13),
            // (16,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '[1, _, 0]' is not covered.
            // _ = new C() switch // 3
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("[1, _, 0]").WithLocation(16, 13)
            );
    }

    [Fact]
    public void ListPattern_Exhaustiveness_Slice()
    {
        var src = @"
_ = new C() switch
{
    null or { Length: 4 } => 0,
    [_, .., _] => 0
};

class C
{
    public int Length => throw null;
    public int this[int i] => throw null;
}
";
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Length: 0 }' is not covered.
            // _ = new C() switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Length: 0 }").WithLocation(2, 13)
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
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
            // 0.cs(5,6): hidden CS9335: The pattern is redundant.
            //     [< 0] => 2,
            Diagnostic(ErrorCode.HDN_RedundantPattern, "< 0").WithLocation(5, 6),
            // 0.cs(6,14): hidden CS9335: The pattern is redundant.
            //     { Count: 0 or > 1 } => 3,
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0 or > 1").WithLocation(6, 14));
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
        var comp = CreateCompilation(new[] { src, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics(
            // 0.cs(5,9): hidden CS9335: The pattern is redundant.
            //     [..[< 0]] => 2,
            Diagnostic(ErrorCode.HDN_RedundantPattern, "< 0").WithLocation(5, 9),
            // 0.cs(6,14): hidden CS9335: The pattern is redundant.
            //     { Count: 0 or > 1 } => 3,
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0 or > 1").WithLocation(6, 14));
    }

    [Fact]
    public void ListPattern_Exhaustiveness_Conjunction()
    {
        var src = @"
_ = new C() switch
{
    { Count: not 1 } => 0,
    [0] or not Derived => 0,
};

class C
{
    public int Count => throw null;
    public int this[int i] => throw null;
    public C Slice(int i, int j) => throw null;
}
class Derived : C { }
";
        // Note: we don't know how to explain `Derived and [1]`
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = new C() switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(2, 13)
            );
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
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
            // (4,5): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            //     [..] => 1,
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[..]").WithArguments("C").WithLocation(4, 5),
            // (4,5): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            //     [..] => 1,
            Diagnostic(ErrorCode.ERR_BadArgType, "[..]").WithArguments("1", "System.Index", "int").WithLocation(4, 5),
            // (7,13): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = new C()[^1]; // 2
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(7, 13)
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
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
            // (4,5): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            //     [..] => 1,
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[..]").WithArguments("C").WithLocation(4, 5),
            // (4,5): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            //     [..] => 1,
            Diagnostic(ErrorCode.ERR_BadArgType, "[..]").WithArguments("1", "System.Index", "int").WithLocation(4, 5),
            // (7,13): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = new C()[^1]; // 2, 3
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(7, 13)
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
@"[0]: t0 != null ? [1] : [16]
[1]: t1 = t0.Length; [2]
[2]: t1 == 3 ? [3] : [10]
[3]: t2 = t0[0]; [4]
[4]: t2 == 1 ? [5] : [16]
[5]: t3 = t0[1]; [6]
[6]: t3 == 2 ? [7] : [13]
[7]: t4 = t0[2]; [8]
[8]: t4 == 3 ? [9] : [16]
[9]: leaf `case [1, 2, 3]:`
[10]: t1 >= 2 ? [11] : [16]
[11]: t2 = t0[0]; [12]
[12]: t2 == 1 ? [13] : [16]
[13]: t5 = t0[-1]; [14]
[14]: t5 == 3 ? [15] : [16]
[15]: leaf `case [1, .., 3]:`
[16]: leaf `default`
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
@"[0]: t0 != null ? [1] : [10]
[1]: t1 = t0.Length; [2]
[2]: t1 == 1 ? [3] : [6]
[3]: t2 = t0[0]; [4]
[4]: t2 == 42 ? [5] : [10]
[5]: leaf `case [42]:`
[6]: t1 >= 1 ? [7] : [10]
[7]: t3 = t0[-1]; [8]
[8]: t3 == 42 ? [9] : [10]
[9]: leaf `case [..,42]:`
[10]: leaf `default`
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
        var comp = CreateCompilation(new[] { src, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
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
            [_] => 2, // unreachable 1
        };
        _ = a switch 
        {
            { Length: not 1 }  => 0,
            [<0, ..] => 0,
            [..[>= 0]] => 1,
            [_] => 2, // unreachable 2
        };
    }
}" + TestSources.GetSubArray;
        var comp = CreateCompilationWithIndexAndRange(src);
        comp.VerifyEmitDiagnostics(
                // (11,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             [_] => 2, // unreachable 1
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "[_]").WithLocation(11, 13),
                // (18,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             [_] => 2, // unreachable 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "[_]").WithLocation(18, 13)
                );

        AssertEx.Multiple(
            () => VerifyDecisionDagDump<SwitchExpressionSyntax>(comp,
@"[0]: t0 != null ? [1] : [11]
[1]: t1 = t0.Length; [2]
[2]: t1 == 1 ? [3] : [10]
[3]: t2 = t0[0]; [4]
[4]: t2 < 0 ? [5] : [6]
[5]: leaf <arm> `[<0, ..] => 0`
[6]: t3 = DagSliceEvaluation(t0); [7]
[7]: t4 = t3.Length; [8]
[8]: t5 = t3[0]; [9]
[9]: leaf <arm> `[..[>= 0]] or [..null] => 1`
[10]: leaf <arm> `{ Length: not 1 }  => 0`
[11]: leaf <default> `a switch
        {
            { Length: not 1 }  => 0,
            [<0, ..] => 0,
            [..[>= 0]] or [..null] => 1,
            [_] => 2, // unreachable 1
        }`
", index: 0),

            () => VerifyDecisionDagDump<SwitchExpressionSyntax>(comp,
@"[0]: t0 != null ? [1] : [11]
[1]: t1 = t0.Length; [2]
[2]: t1 == 1 ? [3] : [10]
[3]: t2 = t0[0]; [4]
[4]: t2 < 0 ? [5] : [6]
[5]: leaf <arm> `[<0, ..] => 0`
[6]: t3 = DagSliceEvaluation(t0); [7]
[7]: t4 = t3.Length; [8]
[8]: t5 = t3[0]; [9]
[9]: leaf <arm> `[..[>= 0]] => 1`
[10]: leaf <arm> `{ Length: not 1 }  => 0`
[11]: leaf <default> `a switch 
        {
            { Length: not 1 }  => 0,
            [<0, ..] => 0,
            [..[>= 0]] => 1,
            [_] => 2, // unreachable 2
        }`
", index: 1)
        );
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
@"[0]: t0 != null ? [1] : [26]
[1]: t1 = t0.Length; [2]
[2]: t1 >= 1 ? [3] : [26]
[3]: t2 = t0[-1]; [4]
[4]: t2 != null ? [5] : [23]
[5]: t3 = t2.Length; [6]
[6]: t3 >= 1 ? [7] : [18]
[7]: t4 = t2[-1]; [8]
[8]: t4 == 42 ? [9] : [10]
[9]: leaf `case [.., [.., 42]]:`
[10]: t1 == 1 ? [11] : [26]
[11]: t5 = t0[0]; [12]
[12]: t5 <-- t2; [13]
[13]: t7 = t5.Length; [14]
[14]: t7 <-- t3; [15]
[15]: t7 == 1 ? [16] : [26]
[16]: t9 = t5[0]; [17]
[17]: t9 <-- t4; [26]
[18]: t1 == 1 ? [19] : [26]
[19]: t5 = t0[0]; [20]
[20]: t5 <-- t2; [21]
[21]: t7 = t5.Length; [22]
[22]: t7 <-- t3; [26]
[23]: t1 == 1 ? [24] : [26]
[24]: t5 = t0[0]; [25]
[25]: t5 <-- t2; [26]
[26]: leaf <break> `switch (a)
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
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(
            // (11,20): hidden CS9335: The pattern is redundant.
            //             case [[>=0]]:
            Diagnostic(ErrorCode.HDN_RedundantPattern, ">=0").WithLocation(11, 20));
    }

    [Fact]
    [WorkItem(51192, "https://github.com/dotnet/roslyn/issues/51192")]
    public void Subsumption_17()
    {
        var source =
@"using System;
public class X
{
    public static void Main()
    {
        object[] o = null;
        switch (o)
        {
            case [.., I1 and Base]:
                break;
            case [Derived s]: // 1
                break;
        }

        switch (o)
        {
            case [.., Base and I1]:
                break;
            case [Derived s]: // 2
                break;
        }

        switch (o)
        {
            case [.., Base and not null]:
                break;
            case [Derived s]: // 3
                break;
        }

        switch (o) {
            case [.., ValueType and int and 1]:
                break;
            case [int and 1]: // 4
                break;
        }

        switch (o) {
            case [.., int and 1]:
                break;
            case [ValueType and int and 1]: // 5
                break;
        }

        switch (o)
        {
            case [.., I2 and Base]:
                break;
            case [Derived s]: // 6
                break;
        }

        switch (o)
        {
            case [.., I2 and Base { F1: 1 }]:
                break;
            case [Derived { F2: 1 }]:
                break;
            case [Derived { P1: 1 }]:
                break;
            case [Derived { F1: 1 } s]: // 7
                break;
        }

        switch (o)
        {
            case [.., I2 and Base { P1: 1 }]:
                break;
            case [Derived { F1: 1 }]:
                break;
            case [Derived { P2: 1 }]:
                break;
            case [Derived { P1: 1 } s]: // 8
                break;
        }

        switch (o)
        {
            case [.., I2 and Base(1, _)]:
                break;
            case [Derived(2, _)]:
                break;
            case [Derived(1, _, _)]:
                break;
            case [Derived(1, _) s]: // 9
                break;
        }

        switch (o)
        {
            case [.., I2 and Base { F3: (1, _) }]:
                break;
            case [Derived { F3: (_, 1) }]:
                break;
            case [Derived { F3: (1, _) } s]: // 10
                break;
        }

        switch (o)
        {
            case [.., I2 and Base]:
                break;
            case [Base and I2]: // 11
                break;
        }

        switch (o)
        {
            case [.., Base and I2]:
                break;
            case [I2 and Base]: // 12
                break;
        }

        object obj = null;
        switch (obj)
        {
            case I1 and Base and [.., I1 and Base]:
                break;
            case Derived and [Derived s]: // 13
                break;
        }

        switch (obj)
        {
            case Base and I1 and [.., Base and I1]: // 14, 15
                break;
            case Derived and [Derived s]: // OK, we're calling into different indexers
                break;
        }

        switch (obj)
        {
            case Base and not null and [.., Base and not null]:
                break;
            case Derived and [Derived s]: // 16
                break;
        }

        object[][] a = null;
        switch (a)
        {
            case [.., [.., I1 and Base]]:
                break;
            case [[Derived]]: // 17
                break;
        }
    }
}

interface I1 : System.Collections.IList {}
interface I2 {}

class Base : System.Collections.ArrayList, I1
{
    public int F1 = 0;
    public int F2 = 0;
    public object F3 = null;
    public int P1 {get; set;}
    public int P2 {get; set;}
    public void Deconstruct(out int x, out int y) => throw null;
    public void Deconstruct(out int x, out int y, out int z) => throw null;
}

class Derived : Base, I2
{
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, _iTupleSource }, options: TestOptions.DebugExe);
        compilation.VerifyDiagnostics(
                // 0.cs(11,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [Derived s]: // 1
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[Derived s]").WithLocation(11, 18),
                // 0.cs(19,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [Derived s]: // 2
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[Derived s]").WithLocation(19, 18),
                // 0.cs(27,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [Derived s]: // 3
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[Derived s]").WithLocation(27, 18),
                // 0.cs(34,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [int and 1]: // 4
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[int and 1]").WithLocation(34, 18),
                // 0.cs(41,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [ValueType and int and 1]: // 5
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[ValueType and int and 1]").WithLocation(41, 18),
                // 0.cs(49,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [Derived s]: // 6
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[Derived s]").WithLocation(49, 18),
                // 0.cs(61,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [Derived { F1: 1 } s]: // 7
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[Derived { F1: 1 } s]").WithLocation(61, 18),
                // 0.cs(73,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [Derived { P1: 1 } s]: // 8
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[Derived { P1: 1 } s]").WithLocation(73, 18),
                // 0.cs(85,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [Derived(1, _) s]: // 9
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[Derived(1, _) s]").WithLocation(85, 18),
                // 0.cs(95,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [Derived { F3: (1, _) } s]: // 10
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[Derived { F3: (1, _) } s]").WithLocation(95, 18),
                // 0.cs(103,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [Base and I2]: // 11
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[Base and I2]").WithLocation(103, 18),
                // 0.cs(111,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [I2 and Base]: // 12
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[I2 and Base]").WithLocation(111, 18),
                // 0.cs(120,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case Derived and [Derived s]: // 13
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "Derived and [Derived s]").WithLocation(120, 18),
                // 0.cs(126,27): hidden CS9335: The pattern is redundant.
                //             case Base and I1 and [.., Base and I1]: // 14, 15
                Diagnostic(ErrorCode.HDN_RedundantPattern, "I1").WithLocation(126, 27),
                // 0.cs(126,48): hidden CS9335: The pattern is redundant.
                //             case Base and I1 and [.., Base and I1]: // 14, 15
                Diagnostic(ErrorCode.HDN_RedundantPattern, "I1").WithLocation(126, 48),
                // 0.cs(136,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case Derived and [Derived s]: // 16
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "Derived and [Derived s]").WithLocation(136, 18),
                // 0.cs(145,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [[Derived]]: // 17
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[[Derived]]").WithLocation(145, 18)
                );
    }

    [Fact]
    public void Subsumption_18()
    {
        var src = @"
class C
{
    void Test(int[] a)
    {
        switch (a)
        {
            case [.., 0]:
            case [<0, ..]:
            case [.., >0]:
            case [_]:         
                break;
        };
    }
}";
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyEmitDiagnostics(
                // (11,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case [_]:         
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[_]").WithLocation(11, 18));
        VerifyDecisionDagDump<SwitchStatementSyntax>(comp,
@"[0]: t0 != null ? [1] : [14]
[1]: t1 = t0.Length; [2]
[2]: t1 >= 1 ? [3] : [14]
[3]: t2 = t0[-1]; [4]
[4]: t2 == 0 ? [5] : [6]
[5]: leaf `case [.., 0]:`
[6]: t3 = t0[0]; [7]
[7]: t1 == 1 ? [8] : [10]
[8]: t3 <-- t2; [9]
[9]: t3 < 0 ? [11] : [13]
[10]: t3 < 0 ? [11] : [12]
[11]: leaf `case [<0, ..]:`
[12]: t2 > 0 ? [13] : [14]
[13]: leaf `case [.., >0]:`
[14]: leaf <break> `switch (a)
        {
            case [.., 0]:
            case [<0, ..]:
            case [.., >0]:
            case [_]:         
                break;
        }`
");
    }

    [Fact]
    public void Subsumption_Slice_00()
    {
        const int Count = 18;
        var cases = new string[Count]
        {
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
           "[1, ..[2, ..[], 3]]"
        };

        // testing every possible combination takes too long,
        // covering a random subset instead.
        var r = new Random();
        for (int i = 0; i < 50; i++)
        {
            var case1 = cases[r.Next(Count)];
            var case2 = cases[r.Next(Count)];
            var type = r.Next(2) == 0 ? "System.Span<int>" : "int[]";

            var src = @"
class C
{
    void Test(" + type + @" a)
    {
        switch (a)
        {
            case " + case1 + @":
            case " + case2 + @":
                break;
        }
    }
}";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(new[] { src, TestSources.GetSubArray }, parseOptions: TestOptions.RegularWithListPatterns);
            comp.VerifyEmitDiagnostics(
                // (9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, case2).WithLocation(9, 18)
                );
        }
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
    public void Subsumption_Slice_02()
    {
        var source = @"
using System;

IOuter outer = null;
switch (outer)
{
    case [..[..[10],20]]:
        break;
    case [..[10],20]: // 1
        break;
}

interface IOuter
{
    int Length { get; }
    IInner Slice(int a, int b);
    object this[int i] { get; }
}
interface IInner
{
   int Count { get; }
   IOuter this[Range r] { get; }
   object this[Index i] { get; }
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(new[] { source, TestSources.GetSubArray }, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics(
                // (9,10): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //     case [..[10],20]: // 1
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[..[10],20]").WithLocation(9, 10)
                );
    }

    [Fact]
    public void Subsumption_Slice_03()
    {
        var source = @"
#nullable enable
class C
{
    public int Length => 3;
    public int this[int i] => 0;
    public int[]? Slice(int i, int j) => null;

    public static void Main()
    {
        switch (new C())
        {
            case [.. {}]:
                break;
            case [.. null]:
                break;
        }
    }
}
";
        var compilation = CreateCompilationWithIndexAndRange(source, options: TestOptions.ReleaseExe);
        compilation.VerifyEmitDiagnostics(
            // (15,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //             case [.. null]:
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[.. null]").WithLocation(15, 18)
            );
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
        comp.VerifyEmitDiagnostics(
            // (10,27): hidden CS9335: The pattern is redundant.
            //             [.., >=0] or [<0] or { Length: 0 or >1 } => 0
            Diagnostic(ErrorCode.HDN_RedundantPattern, "<0").WithLocation(10, 27),
            // (10,44): hidden CS9335: The pattern is redundant.
            //             [.., >=0] or [<0] or { Length: 0 or >1 } => 0
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0 or >1").WithLocation(10, 44),
            // (15,34): hidden CS9335: The pattern is redundant.
            //             [.., >=0] or [..[.., <0]] or [] => 0
            Diagnostic(ErrorCode.HDN_RedundantPattern, "<0").WithLocation(15, 34),
            // (20,27): hidden CS9335: The pattern is redundant.
            //             [..[>=0]] or [<0] or
            Diagnostic(ErrorCode.HDN_RedundantPattern, "<0").WithLocation(20, 27),
            // (21,23): hidden CS9335: The pattern is redundant.
            //             { Length: 0 or >1 } => 0
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0 or >1").WithLocation(21, 23),
            // (31,70): hidden CS9335: The pattern is redundant.
            //             [_, ..{ Length: < int.MaxValue - 1 }] or [] or { Length: int.MaxValue } => 0
            Diagnostic(ErrorCode.HDN_RedundantPattern, "int.MaxValue").WithLocation(31, 70),
            // (36,26): hidden CS9335: The pattern is redundant.
            //             [..{ Length: <= int.MaxValue - 1 }, _] or []  => 0
            Diagnostic(ErrorCode.HDN_RedundantPattern, "<= int.MaxValue - 1").WithLocation(36, 26),
            // (41,29): hidden CS9335: The pattern is redundant.
            //             [_, ..{ Length: <= int.MaxValue - 2 }, _] or { Length: 0 or 1 } => 0
            Diagnostic(ErrorCode.HDN_RedundantPattern, "<= int.MaxValue - 2").WithLocation(41, 29),
            // (41,68): hidden CS9335: The pattern is redundant.
            //             [_, ..{ Length: <= int.MaxValue - 2 }, _] or { Length: 0 or 1 } => 0
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0 or 1").WithLocation(41, 68),
            // (50,18): hidden CS9335: The pattern is redundant.
            //             [{ X:0, Y:0 }] => 1,
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0").WithLocation(50, 18),
            // (50,23): hidden CS9335: The pattern is redundant.
            //             [{ X:0, Y:0 }] => 1,
            Diagnostic(ErrorCode.HDN_RedundantPattern, "0").WithLocation(50, 23));
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
        comp.VerifyEmitDiagnostics(
            // (20,23): hidden CS9335: The pattern is redundant.
            //             { Length: >=1 } => 3,
            Diagnostic(ErrorCode.HDN_RedundantPattern, ">=1").WithLocation(20, 23));

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
    public void LengthPattern_NegativeLengthTest_MissingIndex()
    {
        var src = @"
int[] a = null;
_ = a is { Length: -1 };
";
        var comp = CreateCompilation(src);
        comp.MakeTypeMissing(WellKnownType.System_Index);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void LengthPattern_NegativeLengthTest()
    {
        var src = @"
int[] a = null;
_ = a is { Length: -1 }; // 1
_ = a is { Length: -1 or 1 }; // 2
_ = a is { Length: -1 } or { Length: 1 }; // 3

_ = a switch // 4
{
    { Length: -1 } => 0, // 5
};

_ = a switch // 6
{
    { Length: -1 or 1 } => 0,
};

_ = a switch // 7
{
    { Length: -1 } or { Length: 1 } => 0,
};

_ = a switch // 8
{
    { Length: -1 } => 0, // 9
    { Length: 1 } => 0,
};
";
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyDiagnostics(
            // 0.cs(3,5): error CS8518: An expression of type 'int[]' can never match the provided pattern.
            // _ = a is { Length: -1 }; // 1
            Diagnostic(ErrorCode.ERR_IsPatternImpossible, "a is { Length: -1 }").WithArguments("int[]").WithLocation(3, 5),
            // 0.cs(4,20): hidden CS9335: The pattern is redundant.
            // _ = a is { Length: -1 or 1 }; // 2
            Diagnostic(ErrorCode.HDN_RedundantPattern, "-1").WithLocation(4, 20),
            // 0.cs(5,10): hidden CS9335: The pattern is redundant.
            // _ = a is { Length: -1 } or { Length: 1 }; // 3
            Diagnostic(ErrorCode.HDN_RedundantPattern, "{ Length: -1 }").WithLocation(5, 10),
            // 0.cs(7,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            // _ = a switch // 4
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(7, 7),
            // 0.cs(9,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     { Length: -1 } => 0, // 5
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ Length: -1 }").WithLocation(9, 5),
            // 0.cs(12,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Length: 0 }' is not covered.
            // _ = a switch // 6
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Length: 0 }").WithLocation(12, 7),
            // 0.cs(17,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Length: 0 }' is not covered.
            // _ = a switch // 7
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Length: 0 }").WithLocation(17, 7),
            // 0.cs(22,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Length: 0 }' is not covered.
            // _ = a switch // 8
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Length: 0 }").WithLocation(22, 7),
            // 0.cs(24,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     { Length: -1 } => 0, // 9
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ Length: -1 }").WithLocation(24, 5)
            );
    }

    [Fact]
    public void LengthPattern_NegativeNullHandling_WithNullHandling()
    {
        var src = @"
int[] a = null;
_ = a is null or { Length: -1 }; // 1

_ = a switch // 2
{
    null => 0,
    { Length: -1 } => 0, // 3
};
";
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyDiagnostics(
            // 0.cs(3,18): hidden CS9335: The pattern is redundant.
            // _ = a is null or { Length: -1 }; // 1
            Diagnostic(ErrorCode.HDN_RedundantPattern, "{ Length: -1 }").WithLocation(3, 18),
            // 0.cs(5,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'not null' is not covered.
            // _ = a switch // 2
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("not null").WithLocation(5, 7),
            // 0.cs(8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     { Length: -1 } => 0, // 3
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
        var comp = CreateCompilation(new[] { src, TestSources.Index });
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
        var comp = CreateCompilation(new[] { src, TestSources.Index });
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
        var comp = CreateCompilation(new[] { src, TestSources.Index });
        comp.VerifyDiagnostics(
            // (6,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     { Length: <= 0 } => 2,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ Length: <= 0 }").WithLocation(6, 5)
            );

        comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
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
        var verifier = CompileAndVerify(new[] { source, TestSources.Index }, options: TestOptions.DebugDll);
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

    [Fact, WorkItem(51801, "https://github.com/dotnet/roslyn/issues/51801")]
    public void LengthOverrideLacksAccessor()
    {
        var source = @"
#nullable enable
using System.Runtime.CompilerServices;

class Base
{
    public virtual int Length { get { return 2; } set { } }
}

class C : Base
{
    public override int Length { set { } }
    public object this[int i] { get { return 1; } set { } }

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
        var verifier = CompileAndVerify(new[] { source, TestSources.Index });
        verifier.VerifyIL("C.M", @"
{
  // Code size       78 (0x4e)
  .maxstack  2
  .locals init (C V_0,
                object V_1,
                object V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brfalse.s  IL_0047
  IL_0005:  ldloc.0
  IL_0006:  callvirt   ""int Base.Length.get""
  IL_000b:  ldc.i4.2
  IL_000c:  bne.un.s   IL_0047
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.0
  IL_0010:  callvirt   ""object C.this[int].get""
  IL_0015:  stloc.1
  IL_0016:  ldloc.1
  IL_0017:  isinst     ""int""
  IL_001c:  brfalse.s  IL_0047
  IL_001e:  ldloc.1
  IL_001f:  unbox.any  ""int""
  IL_0024:  ldc.i4.1
  IL_0025:  bne.un.s   IL_0047
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4.1
  IL_0029:  callvirt   ""object C.this[int].get""
  IL_002e:  stloc.2
  IL_002f:  ldloc.2
  IL_0030:  isinst     ""int""
  IL_0035:  brfalse.s  IL_0047
  IL_0037:  ldloc.2
  IL_0038:  unbox.any  ""int""
  IL_003d:  ldc.i4.1
  IL_003e:  pop
  IL_003f:  pop
  IL_0040:  ldarg.0
  IL_0041:  call       ""string C.Value.get""
  IL_0046:  ret
  IL_0047:  ldarg.0
  IL_0048:  call       ""string C.Value.get""
  IL_004d:  ret
}");
    }

    [Fact]
    public void ListPattern_LengthAndCountAreOrthogonal()
    {
        var source = @"
_ = new C() switch
{
    [] => 0,
    { Length: 1 } => 0,
    { Count: > 1 } => 0
};

class C
{
    public int this[System.Index i] => 1;
    public int Length => 1;
    public int Count => 1;
}
";
        var compilation = CreateCompilationWithIndexAndRange(source);
        compilation.VerifyEmitDiagnostics(
            // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Length: 2,  Count: 0 }' is not covered.
            // _ = new C() switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Length: 2,  Count: 0 }").WithLocation(2, 13)
            );
    }

    [Fact]
    public void ListPattern_LengthFieldNotApplicable()
    {
        var source = @"
_ = new C() is [];
_ = new C()[^1];

class C
{
    public int this[int i] => 1;
    public int Length = 0;
}
";
        var compilation = CreateCompilationWithIndex(source);
        compilation.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            // _ = new C() is [];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("C").WithLocation(2, 16),
            // (2,16): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = new C() is [];
            Diagnostic(ErrorCode.ERR_BadArgType, "[]").WithArguments("1", "System.Index", "int").WithLocation(2, 16),
            // (3,13): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = new C()[^1];
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(3, 13)
            );
    }

    [Fact]
    public void ListPattern_IndexAndRangeAreNecessaryButOptimizedAway()
    {
        var source = @"
new C().M();

public class C
{
    public int this[int i] => 2;
    public int Length => 1;
    public int Slice(int i, int j) => 3;

    public void M()
    {
        if (this is [var x] && this is [.. var y])
            System.Console.Write((x, y));
    }
}
";

        var compilation = CreateCompilation(source);
        compilation.MakeTypeMissing(WellKnownType.System_Index);
        compilation.MakeTypeMissing(WellKnownType.System_Range);
        compilation.VerifyDiagnostics(
            // (12,21): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         if (this is [var x] && this is [.. var y])
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[var x]").WithArguments("System.Index").WithLocation(12, 21),
            // (12,21): error CS0656: Missing compiler required member 'System.Index.GetOffset'
            //         if (this is [var x] && this is [.. var y])
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[var x]").WithArguments("System.Index", "GetOffset").WithLocation(12, 21),
            // (12,40): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         if (this is [var x] && this is [.. var y])
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[.. var y]").WithArguments("System.Index").WithLocation(12, 40),
            // (12,40): error CS0656: Missing compiler required member 'System.Index.GetOffset'
            //         if (this is [var x] && this is [.. var y])
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[.. var y]").WithArguments("System.Index", "GetOffset").WithLocation(12, 40),
            // (12,41): error CS0518: Predefined type 'System.Range' is not defined or imported
            //         if (this is [var x] && this is [.. var y])
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, ".. var y").WithArguments("System.Range").WithLocation(12, 41),
            // (12,41): error CS0656: Missing compiler required member 'System.Range.get_Start'
            //         if (this is [var x] && this is [.. var y])
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".. var y").WithArguments("System.Range", "get_Start").WithLocation(12, 41),
            // (12,41): error CS0656: Missing compiler required member 'System.Range.get_End'
            //         if (this is [var x] && this is [.. var y])
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".. var y").WithArguments("System.Range", "get_End").WithLocation(12, 41),
            // (12,41): error CS0656: Missing compiler required member 'System.Index.GetOffset'
            //         if (this is [var x] && this is [.. var y])
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, ".. var y").WithArguments("System.Index", "GetOffset").WithLocation(12, 41)
            );

        compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        var verifier = CompileAndVerify(compilation, expectedOutput: "(2, 3)");
        verifier.VerifyDiagnostics();
        // Note: no Index or Range involved
        verifier.VerifyIL("C.M", @"
{
  // Code size       61 (0x3d)
  .maxstack  3
  .locals init (int V_0, //x
                int V_1, //y
                C V_2,
                int V_3)
  IL_0000:  ldarg.0
  IL_0001:  stloc.2
  IL_0002:  ldloc.2
  IL_0003:  brfalse.s  IL_003c
  IL_0005:  ldloc.2
  IL_0006:  callvirt   ""int C.Length.get""
  IL_000b:  ldc.i4.1
  IL_000c:  bne.un.s   IL_003c
  IL_000e:  ldloc.2
  IL_000f:  ldc.i4.0
  IL_0010:  callvirt   ""int C.this[int].get""
  IL_0015:  stloc.0
  IL_0016:  ldarg.0
  IL_0017:  stloc.2
  IL_0018:  ldloc.2
  IL_0019:  brfalse.s  IL_003c
  IL_001b:  ldloc.2
  IL_001c:  callvirt   ""int C.Length.get""
  IL_0021:  stloc.3
  IL_0022:  ldloc.2
  IL_0023:  ldc.i4.0
  IL_0024:  ldloc.3
  IL_0025:  callvirt   ""int C.Slice(int, int)""
  IL_002a:  stloc.1
  IL_002b:  ldloc.0
  IL_002c:  ldloc.1
  IL_002d:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0032:  box        ""System.ValueTuple<int, int>""
  IL_0037:  call       ""void System.Console.Write(object)""
  IL_003c:  ret
}
");
    }

    [Fact]
    public void ListPattern_StaticIndexers()
    {
        var source = @"
_ = new C() is [var x, .. var y];

class C
{
    public int Length => 0;
    public static int this[System.Index i] => 0;
    public static int this[System.Range r] => 0;
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyDiagnostics(
            // (7,23): error CS0106: The modifier 'static' is not valid for this item
            //     public static int this[System.Index i] => 0;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(7, 23),
            // (8,23): error CS0106: The modifier 'static' is not valid for this item
            //     public static int this[System.Range r] => 0;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(8, 23)
            );
    }

    [Fact]
    public void ListPattern_SetOnlyIndexers()
    {
        var source = @"
_ = new C() is [var x, .. var y]; // 1, 2, 3
_ = new C()[^1]; // 4
_ = new C()[..]; // 5

class C
{
    public int Length { set { } }
    public int this[System.Index i] { set { } }
    public int this[System.Range r] { set { } }
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            // _ = new C() is [var x, .. var y]; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[var x, .. var y]").WithArguments("C").WithLocation(2, 16),
            // (2,16): error CS0154: The property or indexer 'C.this[Index]' cannot be used in this context because it lacks the get accessor
            // _ = new C() is [var x, .. var y]; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "[var x, .. var y]").WithArguments("C.this[System.Index]").WithLocation(2, 16),
            // (2,24): error CS0154: The property or indexer 'C.this[Range]' cannot be used in this context because it lacks the get accessor
            // _ = new C() is [var x, .. var y]; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, ".. var y").WithArguments("C.this[System.Range]").WithLocation(2, 24),
            // (3,5): error CS0154: The property or indexer 'C.this[Index]' cannot be used in this context because it lacks the get accessor
            // _ = new C()[^1]; // 4
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "new C()[^1]").WithArguments("C.this[System.Index]").WithLocation(3, 5),
            // (4,5): error CS0154: The property or indexer 'C.this[Range]' cannot be used in this context because it lacks the get accessor
            // _ = new C()[..]; // 5
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "new C()[..]").WithArguments("C.this[System.Range]").WithLocation(4, 5)
            );
    }

    [Fact]
    public void ListPattern_SetOnlyIndexers_LengthWithGetter()
    {
        var source = @"
_ = new C() is [var x, .. var y]; // 1, 2
_ = new C()[^1]; // 3
_ = new C()[..]; // 4

class C
{
    public int Length => 0;
    public int this[System.Index i] { set { } }
    public int this[System.Range r] { set { } }
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS0154: The property or indexer 'C.this[Index]' cannot be used in this context because it lacks the get accessor
            // _ = new C() is [var x, .. var y]; // 1, 2
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "[var x, .. var y]").WithArguments("C.this[System.Index]").WithLocation(2, 16),
            // (2,24): error CS0154: The property or indexer 'C.this[Range]' cannot be used in this context because it lacks the get accessor
            // _ = new C() is [var x, .. var y]; // 1, 2
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, ".. var y").WithArguments("C.this[System.Range]").WithLocation(2, 24),
            // (3,5): error CS0154: The property or indexer 'C.this[Index]' cannot be used in this context because it lacks the get accessor
            // _ = new C()[^1]; // 3
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "new C()[^1]").WithArguments("C.this[System.Index]").WithLocation(3, 5),
            // (4,5): error CS0154: The property or indexer 'C.this[Range]' cannot be used in this context because it lacks the get accessor
            // _ = new C()[..]; // 4
            Diagnostic(ErrorCode.ERR_PropertyLacksGet, "new C()[..]").WithArguments("C.this[System.Range]").WithLocation(4, 5)
            );
    }

    [Fact]
    public void SlicePattern_OnConsList()
    {
        var source = @"
#nullable enable
using System;

record ConsList(object Head, ConsList? Tail)
{
    public int Length => throw null!;
    public object this[Index i] => throw null!;
    public ConsList? this[Range r] => throw null!;

    public static void Print(ConsList? list)
    {
        switch (list)
        {
            case null:
                return;
            case [var head, .. var tail]:
                System.Console.Write(head.ToString() + "" "");
                Print(tail);
                return;
        }
    }
}
";
        // Note: this pattern doesn't work well because list-patterns needs a functional Length
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, IsExternalInitTypeDefinition });
        compilation.VerifyDiagnostics();
        var verifier = CompileAndVerify(compilation, verify: Verification.FailsPEVerify);
        verifier.VerifyIL("ConsList.Print", @"
{
  // Code size       84 (0x54)
  .maxstack  4
  .locals init (object V_0, //head
                ConsList V_1) //tail
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0036
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""int ConsList.Length.get""
  IL_0009:  ldc.i4.1
  IL_000a:  blt.s      IL_0053
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  newobj     ""System.Index..ctor(int, bool)""
  IL_0014:  callvirt   ""object ConsList.this[System.Index].get""
  IL_0019:  stloc.0
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.1
  IL_001c:  ldc.i4.0
  IL_001d:  newobj     ""System.Index..ctor(int, bool)""
  IL_0022:  ldc.i4.0
  IL_0023:  ldc.i4.1
  IL_0024:  newobj     ""System.Index..ctor(int, bool)""
  IL_0029:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_002e:  callvirt   ""ConsList ConsList.this[System.Range].get""
  IL_0033:  stloc.1
  IL_0034:  br.s       IL_0037
  IL_0036:  ret
  IL_0037:  ldloc.0
  IL_0038:  callvirt   ""string object.ToString()""
  IL_003d:  ldstr      "" ""
  IL_0042:  call       ""string string.Concat(string, string)""
  IL_0047:  call       ""void System.Console.Write(string)""
  IL_004c:  ldloc.1
  IL_004d:  call       ""void ConsList.Print(ConsList)""
  IL_0052:  ret
  IL_0053:  ret
}
");
    }

    [Fact]
    public void PositionalPattern_OnConsList()
    {
        var source = @"
#nullable enable

var list = new ConsList(1, new ConsList(2, new ConsList(3, null)));
ConsList.Print(list);

record ConsList(object Head, ConsList? Tail)
{
    public static void Print(ConsList? list)
    {
        switch (list)
        {
            case null:
                return;
            case (var head, var tail):
                System.Console.Write(head.ToString() + "" "");
                Print(tail);
                return;
        }
    }
}
";
        var compilation = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, IsExternalInitTypeDefinition });
        compilation.VerifyDiagnostics();
        var verifier = CompileAndVerify(compilation, expectedOutput: "1 2 3", verify: Verification.FailsPEVerify);
        verifier.VerifyIL("ConsList.Print", @"
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (object V_0, //head
                ConsList V_1) //tail
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_000f
  IL_0003:  ldarg.0
  IL_0004:  ldloca.s   V_0
  IL_0006:  ldloca.s   V_1
  IL_0008:  callvirt   ""void ConsList.Deconstruct(out object, out ConsList)""
  IL_000d:  br.s       IL_0010
  IL_000f:  ret
  IL_0010:  ldloc.0
  IL_0011:  callvirt   ""string object.ToString()""
  IL_0016:  ldstr      "" ""
  IL_001b:  call       ""string string.Concat(string, string)""
  IL_0020:  call       ""void System.Console.Write(string)""
  IL_0025:  ldloc.1
  IL_0026:  call       ""void ConsList.Print(ConsList)""
  IL_002b:  ret
}
");
    }

    [Fact]
    public void Simple_IndexIndexer()
    {
        var source = @"
if (new C() is [var x])
{
    System.Console.Write((x, new C()[^1]));
}

class C
{
    public int Length => 1;
    public int this[System.Index i] => 42;
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "(42, 42)");
    }

    [Fact]
    public void Simple_IntIndexer_MissingIndex()
    {
        var source = @"
if (new C() is [var x])
{
    System.Console.Write(x);
}

class C
{
    public int Count => 1;
    public int this[int i] => 42;
}
";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS0518: Predefined type 'System.Index' is not defined or imported
            // if (new C() is [var x])
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[var x]").WithArguments("System.Index").WithLocation(2, 16),
            // (2,16): error CS0656: Missing compiler required member 'System.Index.GetOffset'
            // if (new C() is [var x])
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[var x]").WithArguments("System.Index", "GetOffset").WithLocation(2, 16)
            );
    }

    [Fact]
    public void Simple_IntIndexer()
    {
        var source = @"
if (new C() is [var x])
{
    System.Console.Write((x, new C()[^1]));
}

class C
{
    public int Count => 1;
    public int this[int i] => 42;
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "(42, 42)");
    }

    [Fact]
    public void Simple_String()
    {
        var source = @"
if (""42"" is [var x, var y])
{
    System.Console.Write((x, y));
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "(4, 2)");
    }

    [Fact]
    public void Simple_Array()
    {
        var source = @"
if (new[] { 4, 2 } is [var x, _])
{
    var y = new[] { 4, 2 }[^1];
    System.Console.Write((x, y));
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "(4, 2)");
    }

    [Theory]
    [InlineData("{ 4, 0, 0 }", "[var x, _, _]", "[0]")]
    [InlineData("{ 0, 4, 0 }", "[_, var x, _]", "[1]")]
    [InlineData("{ 0, 0, 4 }", "[_, _, var x]", "[2]")]
    [InlineData("{ 4, 0, 0 }", "[.., var x, _, _]", "[^3]")]
    [InlineData("{ 0, 4, 0 }", "[.., _, var x, _]", "[^2]")]
    [InlineData("{ 0, 0, 4 }", "[.., _, _, var x]", "[^1]")]
    public void Simple_Array_VerifyIndexMaths(string data, string pattern, string elementAccess)
    {
        var source = $@"
if (new[] {data} is {pattern})
{{
    var y = new[] {data}{elementAccess};
    System.Console.Write((x, y));
}}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "(4, 4)");
    }

    [Fact]
    public void Simple_IndexIndexer_Slice()
    {
        var source = @"
if (new C() is [.. var x])
{
    System.Console.Write((x, new C()[..]));
}

class C
{
    public int Length => 0;
    public int this[System.Index i] => throw null;
    public int this[System.Range r] => 42; 
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "(42, 42)");
    }

    [Fact]
    public void Simple_IntIndexer_Slice()
    {
        var source = @"
if (new C() is [.. var x])
{
    System.Console.Write((x, new C()[..]));
}

class C
{
    public int Length => 1;
    public int this[System.Index i] => throw null;
    public int Slice(int i, int j) => 42; 
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "(42, 42)");
    }

    [Fact]
    public void Simple_String_Slice()
    {
        var source = @"
if (""0420"" is [_, .. var x, _])
{
    System.Console.Write(x);
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "42");
    }

    [Fact, WorkItem(57728, "https://github.com/dotnet/roslyn/issues/57728")]
    public void Simple_Array_Slice()
    {
        var source = @"
class C
{
    public static void Main()
    {
        if (new[] { 0, 4, 2, 0 } is [_, .. var x, _])
        {
            var y = new[] { 0, 4, 2, 0 }[1..^1];
            System.Console.Write((x[0], x[1], y[0], y[1]));
        }
    }
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray }, options: TestOptions.ReleaseExe);
        comp.VerifyEmitDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "(4, 2, 4, 2)");
        // we use Array.Length to get the length, but should be using ldlen
        // Tracked by https://github.com/dotnet/roslyn/issues/57728
        verifier.VerifyIL("C.Main", @"
{
  // Code size      116 (0x74)
  .maxstack  5
  .locals init (int[] V_0, //x
                int[] V_1,
                int[] V_2) //y
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldc.i4.1
  IL_0008:  ldc.i4.4
  IL_0009:  stelem.i4
  IL_000a:  dup
  IL_000b:  ldc.i4.2
  IL_000c:  ldc.i4.2
  IL_000d:  stelem.i4
  IL_000e:  stloc.1
  IL_000f:  ldloc.1
  IL_0010:  brfalse.s  IL_0073
  IL_0012:  ldloc.1
  IL_0013:  ldlen
  IL_0014:  conv.i4
  IL_0015:  ldc.i4.2
  IL_0016:  blt.s      IL_0073
  IL_0018:  ldloc.1
  IL_0019:  ldc.i4.1
  IL_001a:  ldc.i4.0
  IL_001b:  newobj     ""System.Index..ctor(int, bool)""
  IL_0020:  ldc.i4.1
  IL_0021:  ldc.i4.1
  IL_0022:  newobj     ""System.Index..ctor(int, bool)""
  IL_0027:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_002c:  call       ""int[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<int>(int[], System.Range)""
  IL_0031:  stloc.0
  IL_0032:  ldc.i4.4
  IL_0033:  newarr     ""int""
  IL_0038:  dup
  IL_0039:  ldc.i4.1
  IL_003a:  ldc.i4.4
  IL_003b:  stelem.i4
  IL_003c:  dup
  IL_003d:  ldc.i4.2
  IL_003e:  ldc.i4.2
  IL_003f:  stelem.i4
  IL_0040:  ldc.i4.1
  IL_0041:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0046:  ldc.i4.1
  IL_0047:  ldc.i4.1
  IL_0048:  newobj     ""System.Index..ctor(int, bool)""
  IL_004d:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0052:  call       ""int[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<int>(int[], System.Range)""
  IL_0057:  stloc.2
  IL_0058:  ldloc.0
  IL_0059:  ldc.i4.0
  IL_005a:  ldelem.i4
  IL_005b:  ldloc.0
  IL_005c:  ldc.i4.1
  IL_005d:  ldelem.i4
  IL_005e:  ldloc.2
  IL_005f:  ldc.i4.0
  IL_0060:  ldelem.i4
  IL_0061:  ldloc.2
  IL_0062:  ldc.i4.1
  IL_0063:  ldelem.i4
  IL_0064:  newobj     ""System.ValueTuple<int, int, int, int>..ctor(int, int, int, int)""
  IL_0069:  box        ""System.ValueTuple<int, int, int, int>""
  IL_006e:  call       ""void System.Console.Write(object)""
  IL_0073:  ret
}
");
    }

    [Theory]
    [InlineData("{ 4, 2, 0, 0, 0 }", "[.. var x, _, _, _]", "[0..^3]")]
    [InlineData("{ 0, 4, 2, 0, 0 }", "[_, .. var x, _, _]", "[1..^2]")]
    [InlineData("{ 0, 0, 4, 2, 0 }", "[_, _, .. var x, _]", "[2..^1]")]
    [InlineData("{ 0, 0, 0, 4, 2 }", "[_, _, _, .. var x]", "[3..^0]")]
    public void Simple_Array_Slice_VerifyRangeMaths(string data, string pattern, string elementAccess)
    {
        var source = $@"
if (new[] {data} is {pattern})
{{
    var y = new[] {data}{elementAccess};
    System.Console.Write((x[0], x[1], x.Length, y[0], y[1], y.Length));
}}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "(4, 2, 2, 4, 2, 2)");
    }

    [Fact]
    public void ArrayLengthAccess()
    {
        var source = @"
class C
{
    public int M(int[] a)
    {
        switch (a)
        {
            case [.., var x]: return x;
        }

        return 3;
    }
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray }, options: TestOptions.ReleaseDll);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();

        verifier.VerifyIL("C.M", @"
{
  // Code size       19 (0x13)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0011
  IL_0003:  ldarg.1
  IL_0004:  ldlen
  IL_0005:  conv.i4
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  blt.s      IL_0011
  IL_000b:  ldarg.1
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  sub
  IL_000f:  ldelem.i4
  IL_0010:  ret
  IL_0011:  ldc.i4.3
  IL_0012:  ret
}
");
    }

    [Fact]
    public void ListPattern_NotCountableInterface()
    {
        var source = @"
_ = new C() is INotCountable and [var x, .. var y];

interface INotCountable { }
class C : INotCountable
{
    public int Length { get => 0; }
    public int this[System.Index i] { get => 0; }
    public int this[System.Range r] { get => 0; }
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        comp.VerifyEmitDiagnostics(
            // (2,34): error CS8985: List patterns may not be used for a value of type 'INotCountable'. No suitable 'Length' or 'Count' property was found.
            // _ = new C() is INotCountable and [var x, .. var y];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[var x, .. var y]").WithArguments("INotCountable").WithLocation(2, 34),
            // (2,34): error CS0021: Cannot apply indexing with [] to an expression of type 'INotCountable'
            // _ = new C() is INotCountable and [var x, .. var y];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[var x, .. var y]").WithArguments("INotCountable").WithLocation(2, 34),
            // (2,42): error CS0021: Cannot apply indexing with [] to an expression of type 'INotCountable'
            // _ = new C() is INotCountable and [var x, .. var y];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, ".. var y").WithArguments("INotCountable").WithLocation(2, 42)
            );
    }

    [Fact]
    public void ListPattern_ExplicitInterfaceImplementation()
    {
        var source = @"
if (new C() is Interface and [var x, .. var y])
{
    System.Console.Write((x, y));
}

interface Interface
{
    int Length { get; }
    int this[System.Index i] { get; }
    int this[System.Range r] { get; }
}
class C : Interface
{
    int Interface.Length { get => 2; }
    int Interface.this[System.Index i] { get => 42; }
    int Interface.this[System.Range r] { get => 43; }
}
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
        var verifier = CompileAndVerify(comp, expectedOutput: "(42, 43)");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void ListPattern_Tuples()
    {
        var source = @"
_ = (1, 2) is [var x, .. var y];

System.Runtime.CompilerServices.ITuple ituple = default;
_ = ituple is [var x2];
_ = ituple is [..var y2];

_ = ituple switch
{
    [1, 2] => 0,
    (1, 2) => 0,
    _ => 0,
};
";
        var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.ITuple });
        comp.VerifyEmitDiagnostics(
            // (2,15): error CS8985: List patterns may not be used for a value of type '(int, int)'. No suitable 'Length' or 'Count' property was found.
            // _ = (1, 2) is [var x, .. var y];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[var x, .. var y]").WithArguments("(int, int)").WithLocation(2, 15),
            // (2,15): error CS0021: Cannot apply indexing with [] to an expression of type '(int, int)'
            // _ = (1, 2) is [var x, .. var y];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[var x, .. var y]").WithArguments("(int, int)").WithLocation(2, 15),
            // (2,23): error CS0021: Cannot apply indexing with [] to an expression of type '(int, int)'
            // _ = (1, 2) is [var x, .. var y];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, ".. var y").WithArguments("(int, int)").WithLocation(2, 23),
            // (6,16): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
            // _ = ituple is [..var y2];
            Diagnostic(ErrorCode.ERR_BadArgType, "..var y2").WithArguments("1", "System.Range", "int").WithLocation(6, 16)
            );
    }

    [Fact]
    public void ListPattern_NullTestOnSlice()
    {
        var source = @"
using System;
Span<int> s = default;
switch (s)
{
    case [..[1],2,3]:
    case [1,2,3]: // error
        break;
}

int[] a = default;
switch (a)
{
    case [..[1],2,3]:
    case [1,2,3]: // error
        break;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(new[] { source, TestSources.GetSubArray });
        comp.VerifyEmitDiagnostics(
            // (7,10): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //     case [1,2,3]: // error
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[1,2,3]").WithLocation(7, 10),
            // (15,10): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
            //     case [1,2,3]: // error
            Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "[1,2,3]").WithLocation(15, 10)
            );
    }

    [Fact, WorkItem(59465, "https://github.com/dotnet/roslyn/issues/59465")]
    public void MissingLength()
    {
        var source = @"
_ = new S() is [];
_ = new S() is [..];
_ = new S() is [0, .. var x, 1];

struct S
{
    public int this[System.Index i] => 0;
    public int this[System.Range i] => 0;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("S").WithLocation(2, 16),
            // (3,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [..];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[..]").WithArguments("S").WithLocation(3, 16),
            // (4,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [0, .. var x, 1];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[0, .. var x, 1]").WithArguments("S").WithLocation(4, 16)
            );
    }

    [Fact, WorkItem(59465, "https://github.com/dotnet/roslyn/issues/59465")]
    public void MissingLength_WithIntIndexer()
    {
        var source = @"
_ = new S() is [];

struct S
{
    public int this[int i] => i;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("S").WithLocation(2, 16),
            // (2,16): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_BadArgType, "[]").WithArguments("1", "System.Index", "int").WithLocation(2, 16)
            );
    }

    [Fact, WorkItem(59465, "https://github.com/dotnet/roslyn/issues/59465")]
    public void MissingLength_PrivateLength()
    {
        var source = @"
_ = new S() is [];

struct S
{
    public int this[System.Index i] => 0;
    private int Length => 0;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("S").WithLocation(2, 16)
            );
    }

    [Fact, WorkItem(59465, "https://github.com/dotnet/roslyn/issues/59465")]
    public void MissingLength_ProtectedLength()
    {
        var source = @"
_ = new S() is [];

class S
{
    public int this[System.Index i] => 0;
    protected int Length => 0;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("S").WithLocation(2, 16)
            );
    }

    [Fact, WorkItem(59465, "https://github.com/dotnet/roslyn/issues/59465")]
    public void MissingLength_PrivateCount()
    {
        var source = @"
_ = new S() is [];

struct S
{
    public int this[System.Index i] => 0;
    private int Count => 0;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("S").WithLocation(2, 16)
            );
    }

    [Fact, WorkItem(59465, "https://github.com/dotnet/roslyn/issues/59465")]
    public void MissingLength_LengthMethod()
    {
        var source = @"
_ = new S() is [];

struct S
{
    public int this[System.Index i] => 0;
    public int Length() => 0;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("S").WithLocation(2, 16)
            );
    }

    [Fact, WorkItem(59465, "https://github.com/dotnet/roslyn/issues/59465")]
    public void MissingLength_WriteOnlyLength()
    {
        var source = @"
_ = new S() is [];

struct S
{
    public int this[System.Index i] => 0;
    public int Length { set { throw null; } }
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("S").WithLocation(2, 16)
            );
    }

    [Fact, WorkItem(59465, "https://github.com/dotnet/roslyn/issues/59465")]
    public void MissingLength_ObjectLength()
    {
        var source = @"
_ = new S() is [];

struct S
{
    public int this[System.Index i] => 0;
    public object Length => null;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("S").WithLocation(2, 16)
            );
    }

    [Fact, WorkItem(59465, "https://github.com/dotnet/roslyn/issues/59465")]
    public void MissingLength_StaticLength()
    {
        var source = @"
_ = new S() is [];

struct S
{
    public int this[System.Index i] => 0;
    public static int Length => 0;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("S").WithLocation(2, 16)
            );
    }

    [Fact, WorkItem(59465, "https://github.com/dotnet/roslyn/issues/59465")]
    public void MissingLength_StaticLength_IntIndexer()
    {
        var source = @"
_ = new S() is [];

struct S
{
    public int this[int i] => 0;
    public static int Length => 0;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,16): error CS8985: List patterns may not be used for a value of type 'S'. No suitable 'Length' or 'Count' property was found.
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("S").WithLocation(2, 16),
            // (2,16): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = new S() is [];
            Diagnostic(ErrorCode.ERR_BadArgType, "[]").WithArguments("1", "System.Index", "int").WithLocation(2, 16)
            );
    }

    [Fact, WorkItem(58738, "https://github.com/dotnet/roslyn/issues/58738")]
    public void ListPattern_AbstractFlowPass_isBoolTest()
    {
        var source = @"
var a = new[] { 1 };

if (a is [var x] and x is [1])
{
}

if ((a is [var y] and y) is .. 1)
{
}

var b = new[] { true };
if ((b is [var z] and z) is [true])
{
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(new[] { source, TestSources.GetSubArray });
        comp.VerifyEmitDiagnostics(
            // (4,22): error CS0029: Cannot implicitly convert type 'int' to 'int[]'
            // if (a is [var x] and x is [1])
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("int", "int[]").WithLocation(4, 22),
            // (4,27): error CS8985: List patterns may not be used for a value of type 'bool'. No suitable 'Length' or 'Count' property was found.
            // if (a is [var x] and x is [1])
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[1]").WithArguments("bool").WithLocation(4, 27),
            // (4,27): error CS0021: Cannot apply indexing with [] to an expression of type 'bool'
            // if (a is [var x] and x is [1])
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[1]").WithArguments("bool").WithLocation(4, 27),
            // (8,23): error CS0029: Cannot implicitly convert type 'int' to 'int[]'
            // if ((a is [var y] and y) is .. 1)
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("int", "int[]").WithLocation(8, 23),
            // (8,29): error CS0021: Cannot apply indexing with [] to an expression of type 'bool'
            // if ((a is [var y] and y) is .. 1)
            Diagnostic(ErrorCode.ERR_BadIndexLHS, ".. 1").WithArguments("bool").WithLocation(8, 29),
            // (13,23): error CS0029: Cannot implicitly convert type 'bool' to 'bool[]'
            // if ((b is [var z] and z) is [true])
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "z").WithArguments("bool", "bool[]").WithLocation(13, 23),
            // (13,29): error CS8985: List patterns may not be used for a value of type 'bool'. No suitable 'Length' or 'Count' property was found.
            // if ((b is [var z] and z) is [true])
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[true]").WithArguments("bool").WithLocation(13, 29),
            // (13,29): error CS0021: Cannot apply indexing with [] to an expression of type 'bool'
            // if ((b is [var z] and z) is [true])
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[true]").WithArguments("bool").WithLocation(13, 29)
            );
    }

    [Fact, WorkItem(58738, "https://github.com/dotnet/roslyn/issues/58738")]
    public void ListPattern_AbstractFlowPass_isBoolTest_Multiple()
    {
        var source = @"
var b = new[] { true };

if ((b is [var x] and x or x) is [true])
{
}

if ((b is [var z] and z and z) is [true])
{
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(new[] { source, TestSources.GetSubArray });
        comp.VerifyEmitDiagnostics(
            // 0.cs(4,16): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            // if ((b is [var x] and x or x) is [true])
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "x").WithLocation(4, 16),
            // 0.cs(4,23): error CS0029: Cannot implicitly convert type 'bool' to 'bool[]'
            // if ((b is [var x] and x or x) is [true])
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("bool", "bool[]").WithLocation(4, 23),
            // 0.cs(4,23): error CS0165: Use of unassigned local variable 'x'
            // if ((b is [var x] and x or x) is [true])
            Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(4, 23),
            // 0.cs(4,28): error CS0029: Cannot implicitly convert type 'bool' to 'bool[]'
            // if ((b is [var x] and x or x) is [true])
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("bool", "bool[]").WithLocation(4, 28),
            // 0.cs(4,34): error CS8985: List patterns may not be used for a value of type 'bool'. No suitable 'Length' or 'Count' property was found.
            // if ((b is [var x] and x or x) is [true])
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[true]").WithArguments("bool").WithLocation(4, 34),
            // 0.cs(4,34): error CS0021: Cannot apply indexing with [] to an expression of type 'bool'
            // if ((b is [var x] and x or x) is [true])
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[true]").WithArguments("bool").WithLocation(4, 34),
            // 0.cs(8,23): error CS0029: Cannot implicitly convert type 'bool' to 'bool[]'
            // if ((b is [var z] and z and z) is [true])
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "z").WithArguments("bool", "bool[]").WithLocation(8, 23),
            // 0.cs(8,29): error CS0029: Cannot implicitly convert type 'bool' to 'bool[]'
            // if ((b is [var z] and z and z) is [true])
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "z").WithArguments("bool", "bool[]").WithLocation(8, 29),
            // 0.cs(8,35): error CS8985: List patterns may not be used for a value of type 'bool'. No suitable 'Length' or 'Count' property was found.
            // if ((b is [var z] and z and z) is [true])
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[true]").WithArguments("bool").WithLocation(8, 35),
            // 0.cs(8,35): error CS0021: Cannot apply indexing with [] to an expression of type 'bool'
            // if ((b is [var z] and z and z) is [true])
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[true]").WithArguments("bool").WithLocation(8, 35)
            );
    }

    [Fact, WorkItem(58738, "https://github.com/dotnet/roslyn/issues/58738")]
    public void ListPattern_AbstractFlowPass_patternMatchesNull()
    {
        var source = @"
var a = new[] { 1 };

if (a?.M(out var i) is [1])
    i.ToString();
else
    i.ToString(); // 1

if (a?.M(out var j) is .. 1) // 2, 3
    j.ToString();
else
    j.ToString();

public static class Extension
{
    public static T M<T>(this T t, out int i) => throw null;
}
";
        var comp = CreateCompilationWithIndexAndRangeAndSpan(new[] { source, TestSources.GetSubArray });
        comp.VerifyEmitDiagnostics(
            // (7,5): error CS0165: Use of unassigned local variable 'i'
            //     i.ToString(); // 1
            Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i").WithLocation(7, 5),
            // (9,24): error CS8980: Slice patterns may only be used once and directly inside a list pattern.
            // if (a?.M(out var j) is .. 1) // 2, 3
            Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, ".. 1").WithLocation(9, 24),
            // (9,27): error CS0029: Cannot implicitly convert type 'int' to 'int[]'
            // if (a?.M(out var j) is .. 1) // 2, 3
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "int[]").WithLocation(9, 27)
            );
    }

    [Fact]
    public void NotExhaustive_LongList()
    {
        string source = """
            var a = new[] { 1, 2, 3 };
            _ = a switch { { Length: < 1000 } => 0 };
            """;
        var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
        comp.VerifyEmitDiagnostics(
            // (2,7): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Length: 1000 }' is not covered.
            // _ = a switch { { Length: < 1000 } => 0 };
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Length: 1000 }").WithLocation(2, 7));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71660")]
    public void MixedCountPatterns()
    {
        // One of the property patterns is treated as a non-negative Count pattern,
        // while the other is a regular property pattern.
        var source = """
using System;
using System.Collections;
using System.Collections.Generic;

System.Console.Write(select(new List<int>()));
System.Console.Write(select(new List<int>() { 42 }));
System.Console.Write(select(new C()));

int select(ICollection c)
{
    try
    {
        return c switch
        {
            { Count: 0 } => 0,
            IList { Count: > 0 } => 1,
        };
    }
    catch
    {
        return 2;
    }
}

class C : System.Collections.ICollection
{
    public int Count => 1;
    public object SyncRoot => throw new NotImplementedException();
    public bool IsSynchronized => throw new NotImplementedException();
    public void CopyTo(Array array, int index) => throw new NotImplementedException();
    public IEnumerator GetEnumerator() => throw new NotImplementedException();
}
""";
        CompileAndVerify(source, expectedOutput: "012").VerifyDiagnostics(
            // (13,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(13, 18)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71660")]
    public void MixedCountPatterns_ReverseOrder()
    {
        var source = """
using System;
using System.Collections;
using System.Collections.Generic;

System.Console.Write(select(new List<int>()));
System.Console.Write(select(new List<int>() { 42 }));
System.Console.Write(select(new C()));

int select(ICollection c)
{
    try
    {
        return c switch
        {
            IList { Count: > 0 } => 1,
            { Count: 0 } => 0,
        };
    }
    catch
    {
        return 2;
    }
}

class C : System.Collections.ICollection
{
    public int Count => 1;
    public object SyncRoot => throw new NotImplementedException();
    public bool IsSynchronized => throw new NotImplementedException();
    public void CopyTo(Array array, int index) => throw new NotImplementedException();
    public IEnumerator GetEnumerator() => throw new NotImplementedException();
}
""";
        CompileAndVerify(source, expectedOutput: "012").VerifyDiagnostics(
            // (13,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(13, 18)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71660")]
    public void MixedCountPatterns_NegativeTest()
    {
        var source = """
using System.Collections;
using System.Collections.Generic;

var result = (ICollection)new List<int>() switch
{
    IList { Count: > 0 } => throw null,
    IList { Count: < 0 } => throw null,
    { Count: 0 } => "ran",
};

System.Console.Write(result);
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (4,43): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            // var result = (ICollection)new List<int>() switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(4, 43),
            // (7,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     IList { Count: < 0 } => throw null,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "IList { Count: < 0 }").WithLocation(7, 5)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71660")]
    public void MixedCountPatterns_NegativeTestAfterRegularPropertyPattern()
    {
        var source = """
using System.Collections;
using System.Collections.Generic;

_ = (ICollection)new List<int>() switch
{
    IList { Count: > 0 } => 0,
    { Count: 0 } => 0,
    IList { Count: < 0 } => 0,
};
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (4,34): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            // _ = (ICollection)new List<int>() switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(4, 34),
            // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     IList { Count: < 0 } => 0,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "IList { Count: < 0 }").WithLocation(8, 5)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71660")]
    public void MixedCountPatterns_ExplicitICollectionType()
    {
        var source = """
using System;
using System.Collections;
using System.Collections.Generic;

System.Console.Write(select(new List<int>()));
System.Console.Write(select(new List<int>() { 42 }));
System.Console.Write(select(new C()));

int select(ICollection c)
{
    try
    {
        return c switch
        {
            ICollection { Count: 0 } => 0,
            IList { Count: 1 } => 1,
        };
    }
    catch
    {
        return 2;
    }
}

class C : System.Collections.ICollection
{
    public int Count => 1;
    public object SyncRoot => throw new NotImplementedException();
    public bool IsSynchronized => throw new NotImplementedException();
    public void CopyTo(Array array, int index) => throw new NotImplementedException();
    public IEnumerator GetEnumerator() => throw new NotImplementedException();
}
""";
        CompileAndVerify(source, expectedOutput: "012").VerifyDiagnostics(
            // (13,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(13, 18)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71660")]
    public void MixedCountPatterns_Subsumed()
    {
        var source = """
using System.Collections;

_ = (ICollection)null switch
{
    { Count: <0 or >0 } => 0,
    IList { Count: >0 } => 1,
    { Count: 0 } => 2,
};
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (6,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
            //     IList { Count: >0 } => 1,
            Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "IList { Count: >0 }").WithLocation(6, 5)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71660")]
    public void MixedCountPatterns_Or()
    {
        var source = """
using System;
using System.Collections;
using System.Collections.Generic;

System.Console.Write(select(new List<int>()));
System.Console.Write(select(new List<int>() { 42 }));
System.Console.Write(select(new C()));

int select(ICollection c)
{
    try
    {
        return c switch
        {
            { Count: 0 } or (IList { Count: > 0 }) => 1,
        };
    }
    catch
    {
        return 2;
    }
}

class C : System.Collections.ICollection
{
    public int Count => 1;
    public object SyncRoot => throw new NotImplementedException();
    public bool IsSynchronized => throw new NotImplementedException();
    public void CopyTo(Array array, int index) => throw new NotImplementedException();
    public IEnumerator GetEnumerator() => throw new NotImplementedException();
}
""";
        CompileAndVerify(source, expectedOutput: "112").VerifyDiagnostics(
            // (13,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(13, 18)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71660")]
    public void MixedCountPatterns_Or_ReverseOrder()
    {
        var source = """
using System;
using System.Collections;
using System.Collections.Generic;

System.Console.Write(select(new List<int>()));
System.Console.Write(select(new List<int>() { 42 }));
System.Console.Write(select(new C()));

int select(ICollection c)
{
    try
    {
        return c switch
        {
            (IList { Count: > 0 }) or { Count: 0 } => 1,
        };
    }
    catch
    {
        return 2;
    }
}

class C : System.Collections.ICollection
{
    public int Count => 1;
    public object SyncRoot => throw new NotImplementedException();
    public bool IsSynchronized => throw new NotImplementedException();
    public void CopyTo(Array array, int index) => throw new NotImplementedException();
    public IEnumerator GetEnumerator() => throw new NotImplementedException();
}
""";
        CompileAndVerify(source, expectedOutput: "112").VerifyDiagnostics(
            // (13,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: 1 }' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: 1 }").WithLocation(13, 18)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71660")]
    public void MixedCountPatterns_And()
    {
        var source = """
using System;
using System.Collections;
using System.Collections.Generic;

System.Console.Write(select(new List<int>()));
System.Console.Write(select(new List<int>() { 42 }));
System.Console.Write(select(new C()));

int select(ICollection c)
{
    try
    {
        return c switch
        {
            { Count: >= 0 } and (IList { Count: > 0 }) => 1,
        };
    }
    catch
    {
        return 2;
    }
}

class C : System.Collections.ICollection
{
    public int Count => 1;
    public object SyncRoot => throw new NotImplementedException();
    public bool IsSynchronized => throw new NotImplementedException();
    public void CopyTo(Array array, int index) => throw new NotImplementedException();
    public IEnumerator GetEnumerator() => throw new NotImplementedException();
}
""";
        CompileAndVerify(source, expectedOutput: "212").VerifyDiagnostics(
            // (13,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Count: -1 }' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Count: -1 }").WithLocation(13, 18)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71660")]
    public void MixedCountPatterns_And_ReverseOrder()
    {
        var source = """
using System;
using System.Collections;
using System.Collections.Generic;

System.Console.Write(select(new List<int>()));
System.Console.Write(select(new List<int>() { 42 }));
System.Console.Write(select(new C()));

int select(ICollection c)
{
    try
    {
        return c switch
        {
            (IList { Count: > 0 }) and { Count: >= 0 } => 1,
        };
    }
    catch
    {
        return 2;
    }
}

class C : System.Collections.ICollection
{
    public int Count => 1;
    public object SyncRoot => throw new NotImplementedException();
    public bool IsSynchronized => throw new NotImplementedException();
    public void CopyTo(Array array, int index) => throw new NotImplementedException();
    public IEnumerator GetEnumerator() => throw new NotImplementedException();
}
""";
        CompileAndVerify(source, expectedOutput: "212").VerifyDiagnostics(
            // (13,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
            //         return c switch
            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(13, 18)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70046")]
    public void PointerType_ListPattern_NoIndex()
    {
        var source = """
            unsafe class C
            {
                void M()
                {
                    void* v = null;
                    if (v is [])
                    {
                    }
                }
            }
            """;
        var compilation = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
        compilation.VerifyDiagnostics(
            // (6,18): error CS8985: List patterns may not be used for a value of type 'void*'. No suitable 'Length' or 'Count' property was found.
            //         if (v is [])
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("void*").WithLocation(6, 18),
            // (6,18): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         if (v is [])
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(6, 18),
            // (6,18): error CS0242: The operation in question is undefined on void pointers
            //         if (v is [])
            Diagnostic(ErrorCode.ERR_VoidError, "[]").WithLocation(6, 18));

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        var pattern = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();
        compilation.VerifyOperationTree(pattern, """
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'v is []')
                Value:
                  ILocalReferenceOperation: v (OperationKind.LocalReference, Type: System.Void*) (Syntax: 'v')
                Pattern:
                  IListPatternOperation (OperationKind.ListPattern, Type: null, IsInvalid) (Syntax: '[]') (InputType: System.Void*, NarrowedType: System.Void*, DeclaredSymbol: null, LengthSymbol: null, IndexerSymbol: null)
                    Patterns (0)
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70046")]
    public void PointerType_ListPattern_WithIndex()
    {
        var source = """
            unsafe class C
            {
                void M()
                {
                    void* v = null;
                    if (v is [])
                    {
                    }
                }
            }
            """;
        var compilation = CreateCompilation([source, TestSources.Index], options: TestOptions.UnsafeReleaseDll);
        compilation.VerifyDiagnostics(
            // (6,18): error CS8985: List patterns may not be used for a value of type 'void*'. No suitable 'Length' or 'Count' property was found.
            //         if (v is [])
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("void*").WithLocation(6, 18),
            // (6,18): error CS0242: The operation in question is undefined on void pointers
            //         if (v is [])
            Diagnostic(ErrorCode.ERR_VoidError, "[]").WithLocation(6, 18),
            // (6,18): error CS0029: Cannot implicitly convert type 'System.Index' to 'int'
            //         if (v is [])
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "[]").WithArguments("System.Index", "int").WithLocation(6, 18));

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
        var pattern = compilation.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();
        compilation.VerifyOperationTree(pattern, """
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'v is []')
                Value:
                  ILocalReferenceOperation: v (OperationKind.LocalReference, Type: System.Void*) (Syntax: 'v')
                Pattern:
                  IListPatternOperation (OperationKind.ListPattern, Type: null, IsInvalid) (Syntax: '[]') (InputType: System.Void*, NarrowedType: System.Void*, DeclaredSymbol: null, LengthSymbol: null, IndexerSymbol: null)
                    Patterns (0)
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70046")]
    public void FunctionPointerType_ListPattern_NoIndex()
    {
        var source = """
            unsafe class C
            {
                void M()
                {
                    delegate*<void> f = null;
                    if (f is [])
                    {
                    }
                }
            }
            """;
        var compilation = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
        compilation.VerifyDiagnostics(
            // (6,18): error CS8985: List patterns may not be used for a value of type 'delegate*<void>'. No suitable 'Length' or 'Count' property was found.
            //         if (f is [])
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("delegate*<void>").WithLocation(6, 18),
            // (6,18): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         if (f is [])
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(6, 18),
            // (6,18): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            //         if (f is [])
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("delegate*<void>").WithLocation(6, 18));

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        var pattern = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();
        compilation.VerifyOperationTree(pattern, """
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'f is []')
                Value:
                  ILocalReferenceOperation: f (OperationKind.LocalReference, Type: delegate*<System.Void>) (Syntax: 'f')
                Pattern:
                  IListPatternOperation (OperationKind.ListPattern, Type: null, IsInvalid) (Syntax: '[]') (InputType: delegate*<System.Void>, NarrowedType: delegate*<System.Void>, DeclaredSymbol: null, LengthSymbol: null, IndexerSymbol: null)
                    Patterns (0)
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70046")]
    public void FunctionPointerType_ListPattern_WithIndex()
    {
        var source = """
            unsafe class C
            {
                void M()
                {
                    delegate*<void> f = null;
                    if (f is [])
                    {
                    }
                }
            }
            """;
        var compilation = CreateCompilation([source, TestSources.Index], options: TestOptions.UnsafeReleaseDll);
        compilation.VerifyDiagnostics(
            // (6,18): error CS8985: List patterns may not be used for a value of type 'delegate*<void>'. No suitable 'Length' or 'Count' property was found.
            //         if (f is [])
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("delegate*<void>").WithLocation(6, 18),
            // (6,18): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            //         if (f is [])
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("delegate*<void>").WithLocation(6, 18));

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
        var pattern = compilation.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();
        compilation.VerifyOperationTree(pattern, """
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'f is []')
                Value:
                  ILocalReferenceOperation: f (OperationKind.LocalReference, Type: delegate*<System.Void>) (Syntax: 'f')
                Pattern:
                  IListPatternOperation (OperationKind.ListPattern, Type: null, IsInvalid) (Syntax: '[]') (InputType: delegate*<System.Void>, NarrowedType: delegate*<System.Void>, DeclaredSymbol: null, LengthSymbol: null, IndexerSymbol: null)
                    Patterns (0)
            """);
    }
}
