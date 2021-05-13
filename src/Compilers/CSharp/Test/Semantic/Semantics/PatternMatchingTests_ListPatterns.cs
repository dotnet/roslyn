// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTests_ListPatterns : PatternMatchingTestBase
    {
        [Fact]
        public void ListPattern_Span()
        {
            string createMethod(string type) =>
@"static bool Test("+ type + @" input)
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
    " + createMethod("Span<char>") + @"
    " + createMethod("char[]") + @"
    " + createMethod("string") + @"
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
            compilation.VerifyDiagnostics();
            var expectedOutput = @"
111
111
000
000
111
111
000";
            var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            verifier.VerifyIL("X.Test(System.Span<char>)", @"
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
");
            verifier.VerifyIL("X.Test(char[])", @"
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
");
            verifier.VerifyIL("X.Test(string)", @"
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
");
        }

        [Theory]
        // Index
        [InlineData("[Index i]")]
        [InlineData("[Index i, int ignored = 0]")]
        [InlineData("[Index i, params int[] ignored]")]
        [InlineData("[params Index[] i]")]
        // int
        [InlineData("[int i]")]
        [InlineData("[int i, int ignored = 0]")]
        [InlineData("[int i, params int[] ignored]")]
        [InlineData("[params int[] i]")]
        // long
        [InlineData("[long i]")]
        [InlineData("[long i, int ignored = 0]")]
        [InlineData("[long i, params int[] ignored]")]
        [InlineData("[params long[] i]")]
        public void ListPattern_IndexIndexerPattern(string indexer)
        {
            var source = @"
using System;
class X
{
    public int this" + indexer + @"
    {
        get
        {
            i.ToString(); // verify argument is usable
            Console.Write(""this[] "");
            return 1;
        }
    }
    public int Length => 1;
    public static void Main()
    {
        Console.Write(new X() is { 1 });
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "this[] True");
        }

        [Theory]
        // Range
        [InlineData("[Range i]")]
        [InlineData("[Range i, int ignored = 0]")]
        [InlineData("[Range i, params int[] ignored]")]
        [InlineData("[params Range[] i]")]
        public void ListPattern_ExplicitRangeIndexerPattern(string indexer)
        {
            var source = @"
using System;
class X
{
    public int this" + indexer + @"
    {
        get
        {
            i.ToString(); // verify argument is usable
            Console.Write(""this[] "");
            return 1;
        }
    }
    public int this[int i] => throw new();
    public int Count => 1;
    public static void Main()
    {
        Console.Write(new X() is { .. 1 });
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "this[] True");
        }
    }
}
