// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class IndexAndRangeTests : CSharpTestBase
    {
        [Fact]
        public void RangeIndexerStringFromEndStart()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        Console.WriteLine(s[^2..]);
    }
}", options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "ef");
        }

        [Fact]
        public void FakeRangeIndexerStringBothFromEnd()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        Console.WriteLine(s[^4..^1]);
    }
}", TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "cde");
        }

        [Fact]
        public void IndexIndexerStringTwoArgs()
        {
            var comp = CreateCompilationWithIndex(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        M(s);
    }
    public static void M(string s)
    {
        Console.WriteLine(s[new Index(1, false)]);
        Console.WriteLine(s[new Index(1, false), ^1]);
    }
}");
            comp.VerifyDiagnostics(
                // (13,27): error CS1501: No overload for method 'this' takes 2 arguments
                //         Console.WriteLine(s[new Index(1, false), ^1]);
                Diagnostic(ErrorCode.ERR_BadArgCount, "s[new Index(1, false), ^1]").WithArguments("this", "2").WithLocation(13, 27));
        }

        [Fact]
        public void IndexIndexerArrayTwoArgs()
        {
            var comp = CreateCompilationWithIndex(@"
using System;
class C
{
    public static void Main()
    {
        var x = new int[1,1];
        M(x);
    }
    public static void M(int[,] s)
    {
        Console.WriteLine(s[new Index(1, false), ^1]);
    }
}");
            comp.VerifyDiagnostics(
                // (12,27): error CS0029: Cannot implicitly convert type 'System.Index' to 'int'
                //         Console.WriteLine(s[new Index(1, false), ^1]);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s[new Index(1, false), ^1]").WithArguments("System.Index", "int").WithLocation(12, 27),
                // (12,27): error CS0029: Cannot implicitly convert type 'System.Index' to 'int'
                //         Console.WriteLine(s[new Index(1, false), ^1]);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s[new Index(1, false), ^1]").WithArguments("System.Index", "int").WithLocation(12, 27));
        }

        [Fact]
        public void FakeIndexIndexerString()
        {
            var comp = CreateCompilationWithIndex(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        M(s);
    }
    public static void M(string s)
    {
        Console.WriteLine(s[new Index(1, false)]);
        Console.WriteLine(s[^1]);
    }
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"b
f");
            verifier.VerifyIL("C.M", @"
{
  // Code size      121 (0x79)
  .maxstack  3
  .locals init (System.Index V_0,
                string V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.0
  IL_0004:  call       ""System.Index..ctor(int, bool)""
  IL_0009:  ldarg.0
  IL_000a:  stloc.1
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""bool System.Index.FromEnd.get""
  IL_0012:  brtrue.s   IL_0023
  IL_0014:  ldloc.1
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""int System.Index.Value.get""
  IL_001c:  callvirt   ""char string.this[int].get""
  IL_0021:  br.s       IL_0037
  IL_0023:  ldloc.1
  IL_0024:  ldloc.1
  IL_0025:  callvirt   ""int string.Length.get""
  IL_002a:  ldloca.s   V_0
  IL_002c:  call       ""int System.Index.Value.get""
  IL_0031:  sub
  IL_0032:  callvirt   ""char string.this[int].get""
  IL_0037:  call       ""void System.Console.WriteLine(char)""
  IL_003c:  ldloca.s   V_0
  IL_003e:  ldc.i4.1
  IL_003f:  ldc.i4.1
  IL_0040:  call       ""System.Index..ctor(int, bool)""
  IL_0045:  ldarg.0
  IL_0046:  stloc.1
  IL_0047:  ldloca.s   V_0
  IL_0049:  call       ""bool System.Index.FromEnd.get""
  IL_004e:  brtrue.s   IL_005f
  IL_0050:  ldloc.1
  IL_0051:  ldloca.s   V_0
  IL_0053:  call       ""int System.Index.Value.get""
  IL_0058:  callvirt   ""char string.this[int].get""
  IL_005d:  br.s       IL_0073
  IL_005f:  ldloc.1
  IL_0060:  ldloc.1
  IL_0061:  callvirt   ""int string.Length.get""
  IL_0066:  ldloca.s   V_0
  IL_0068:  call       ""int System.Index.Value.get""
  IL_006d:  sub
  IL_006e:  callvirt   ""char string.this[int].get""
  IL_0073:  call       ""void System.Console.WriteLine(char)""
  IL_0078:  ret
}");
        }

        [Fact]
        public void FakeRangeIndexerString()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        var result = M(s);
        Console.WriteLine(result);
    }
    public static string M(string s) => s[1..3];
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "bc");
            verifier.VerifyIL("C.M", @"
{
  // Code size      151 (0x97)
  .maxstack  4
  .locals init (System.Range V_0,
                string V_1,
                int V_2,
                int V_3,
                System.Index V_4)
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0006:  ldc.i4.3
  IL_0007:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_000c:  call       ""System.Range System.Range.Create(System.Index, System.Index)""
  IL_0011:  stloc.0
  IL_0012:  ldarg.0
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_0
  IL_0016:  call       ""System.Index System.Range.Start.get""
  IL_001b:  stloc.s    V_4
  IL_001d:  ldloca.s   V_4
  IL_001f:  call       ""bool System.Index.FromEnd.get""
  IL_0024:  brtrue.s   IL_0038
  IL_0026:  ldloca.s   V_0
  IL_0028:  call       ""System.Index System.Range.Start.get""
  IL_002d:  stloc.s    V_4
  IL_002f:  ldloca.s   V_4
  IL_0031:  call       ""int System.Index.Value.get""
  IL_0036:  br.s       IL_004f
  IL_0038:  ldloc.1
  IL_0039:  callvirt   ""int string.Length.get""
  IL_003e:  ldloca.s   V_0
  IL_0040:  call       ""System.Index System.Range.Start.get""
  IL_0045:  stloc.s    V_4
  IL_0047:  ldloca.s   V_4
  IL_0049:  call       ""int System.Index.Value.get""
  IL_004e:  sub
  IL_004f:  stloc.2
  IL_0050:  ldloca.s   V_0
  IL_0052:  call       ""System.Index System.Range.End.get""
  IL_0057:  stloc.s    V_4
  IL_0059:  ldloca.s   V_4
  IL_005b:  call       ""bool System.Index.FromEnd.get""
  IL_0060:  brtrue.s   IL_0074
  IL_0062:  ldloca.s   V_0
  IL_0064:  call       ""System.Index System.Range.End.get""
  IL_0069:  stloc.s    V_4
  IL_006b:  ldloca.s   V_4
  IL_006d:  call       ""int System.Index.Value.get""
  IL_0072:  br.s       IL_008b
  IL_0074:  ldloc.1
  IL_0075:  callvirt   ""int string.Length.get""
  IL_007a:  ldloca.s   V_0
  IL_007c:  call       ""System.Index System.Range.End.get""
  IL_0081:  stloc.s    V_4
  IL_0083:  ldloca.s   V_4
  IL_0085:  call       ""int System.Index.Value.get""
  IL_008a:  sub
  IL_008b:  stloc.3
  IL_008c:  ldloc.1
  IL_008d:  ldloc.2
  IL_008e:  ldloc.3
  IL_008f:  ldloc.2
  IL_0090:  sub
  IL_0091:  callvirt   ""string string.Substring(int, int)""
  IL_0096:  ret
}");
        }

        [Fact]
        public void FakeRangeIndexerStringOpenEnd()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        var result = M(s);
        Console.WriteLine(result);
    }
    public static string M(string s) => s[1..];
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "bcdef");
        }

        [Fact]
        public void FakeRangeIndexerStringOpenStart()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        var result = M(s);
        Console.WriteLine(result);
    }
    public static string M(string s) => s[..^2];
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "abcd");
        }

        [Fact]
        public void FakeIndexIndexerArray()
        {
            var comp = CreateCompilationWithIndex(@"
using System;
class C
{
    public static void Main()
    {
        var x = new[] { 1, 2, 3, 11 };
        M(x);
    }

    public static void M(int[] array)
    {
        Console.WriteLine(array[new Index(1, false)]);
        Console.WriteLine(array[^1]);
    }
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"2
11");
            verifier.VerifyIL("C.M", @"
{
  // Code size       99 (0x63)
  .maxstack  3
  .locals init (System.Index V_0,
                int[] V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.0
  IL_0004:  call       ""System.Index..ctor(int, bool)""
  IL_0009:  ldarg.0
  IL_000a:  stloc.1
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""bool System.Index.FromEnd.get""
  IL_0012:  brtrue.s   IL_001f
  IL_0014:  ldloc.1
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""int System.Index.Value.get""
  IL_001c:  ldelem.i4
  IL_001d:  br.s       IL_002c
  IL_001f:  ldloc.1
  IL_0020:  ldloc.1
  IL_0021:  ldlen
  IL_0022:  conv.i4
  IL_0023:  ldloca.s   V_0
  IL_0025:  call       ""int System.Index.Value.get""
  IL_002a:  sub
  IL_002b:  ldelem.i4
  IL_002c:  call       ""void System.Console.WriteLine(int)""
  IL_0031:  ldloca.s   V_0
  IL_0033:  ldc.i4.1
  IL_0034:  ldc.i4.1
  IL_0035:  call       ""System.Index..ctor(int, bool)""
  IL_003a:  ldarg.0
  IL_003b:  stloc.1
  IL_003c:  ldloca.s   V_0
  IL_003e:  call       ""bool System.Index.FromEnd.get""
  IL_0043:  brtrue.s   IL_0050
  IL_0045:  ldloc.1
  IL_0046:  ldloca.s   V_0
  IL_0048:  call       ""int System.Index.Value.get""
  IL_004d:  ldelem.i4
  IL_004e:  br.s       IL_005d
  IL_0050:  ldloc.1
  IL_0051:  ldloc.1
  IL_0052:  ldlen
  IL_0053:  conv.i4
  IL_0054:  ldloca.s   V_0
  IL_0056:  call       ""int System.Index.Value.get""
  IL_005b:  sub
  IL_005c:  ldelem.i4
  IL_005d:  call       ""void System.Console.WriteLine(int)""
  IL_0062:  ret
}");
        }

        [Fact]
        public void FakeIndexIndexerArrayNoValue()
        {
            var comp = CreateCompilation(@"
using System;
namespace System
{
    public readonly struct Index
    {
        private readonly int _value;

        //public int Value => _value < 0 ? ~_value : _value;
        public bool FromEnd => _value < 0;

        public Index(int value, bool fromEnd)
        {
            if (value < 0)
            {
                throw new ArgumentException(""Index must not be negative."", nameof(value));
            }

            _value = fromEnd? ~value : value;
        }

        public static implicit operator Index(int value) => new Index(value, fromEnd: false);
    }
}
class C
{
    public static void Main()
    {
        var x = new[] { 11 };
        M();
        Console.WriteLine(x[^1]);
    }

    static void M()
    {
        Console.WriteLine(""test""[^1]);
    }
}", options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            // https://github.com/dotnet/roslyn/issues/30620
            // We check for the well-known member in lowering, so you won't normally see this
            // error during binding. This is fine for a preview-only feature.
            comp.VerifyEmitDiagnostics(
                // (28,5): error CS0656: Missing compiler required member 'System.Index.Value'
                //     {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{
        var x = new[] { 11 };
        M();
        Console.WriteLine(x[^1]);
    }").WithArguments("System.Index", "Value").WithLocation(28, 5),
                // (35,5): error CS0656: Missing compiler required member 'System.Index.Value'
                //     {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{
        Console.WriteLine(""test""[^1]);
    }").WithArguments("System.Index", "Value").WithLocation(35, 5));
        }

        [Fact]
        public void FakeRangeIndexerArray()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var arr = new[] { 1, 2, 3, 11 };
        var result = M(arr);
        Console.WriteLine(result.Length);
        foreach (var x in result)
        {
            Console.WriteLine(x);
        }
    }
    public static int[] M(int[] array) => array[1..3];
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, verify: Verification.Passes, expectedOutput: @"2
2
3");
            verifier.VerifyIL("C.M", @"
{
  // Code size      158 (0x9e)
  .maxstack  5
  .locals init (System.Range V_0,
                int[] V_1,
                int V_2,
                int V_3,
                int[] V_4,
                System.Index V_5)
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0006:  ldc.i4.3
  IL_0007:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_000c:  call       ""System.Range System.Range.Create(System.Index, System.Index)""
  IL_0011:  stloc.0
  IL_0012:  ldarg.0
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_0
  IL_0016:  call       ""System.Index System.Range.Start.get""
  IL_001b:  stloc.s    V_5
  IL_001d:  ldloca.s   V_5
  IL_001f:  call       ""bool System.Index.FromEnd.get""
  IL_0024:  brtrue.s   IL_0038
  IL_0026:  ldloca.s   V_0
  IL_0028:  call       ""System.Index System.Range.Start.get""
  IL_002d:  stloc.s    V_5
  IL_002f:  ldloca.s   V_5
  IL_0031:  call       ""int System.Index.Value.get""
  IL_0036:  br.s       IL_004c
  IL_0038:  ldloc.1
  IL_0039:  ldlen
  IL_003a:  conv.i4
  IL_003b:  ldloca.s   V_0
  IL_003d:  call       ""System.Index System.Range.Start.get""
  IL_0042:  stloc.s    V_5
  IL_0044:  ldloca.s   V_5
  IL_0046:  call       ""int System.Index.Value.get""
  IL_004b:  sub
  IL_004c:  stloc.2
  IL_004d:  ldloca.s   V_0
  IL_004f:  call       ""System.Index System.Range.End.get""
  IL_0054:  stloc.s    V_5
  IL_0056:  ldloca.s   V_5
  IL_0058:  call       ""bool System.Index.FromEnd.get""
  IL_005d:  brtrue.s   IL_0071
  IL_005f:  ldloca.s   V_0
  IL_0061:  call       ""System.Index System.Range.End.get""
  IL_0066:  stloc.s    V_5
  IL_0068:  ldloca.s   V_5
  IL_006a:  call       ""int System.Index.Value.get""
  IL_006f:  br.s       IL_0085
  IL_0071:  ldloc.1
  IL_0072:  ldlen
  IL_0073:  conv.i4
  IL_0074:  ldloca.s   V_0
  IL_0076:  call       ""System.Index System.Range.End.get""
  IL_007b:  stloc.s    V_5
  IL_007d:  ldloca.s   V_5
  IL_007f:  call       ""int System.Index.Value.get""
  IL_0084:  sub
  IL_0085:  ldloc.2
  IL_0086:  sub
  IL_0087:  stloc.3
  IL_0088:  ldloc.3
  IL_0089:  newarr     ""int""
  IL_008e:  stloc.s    V_4
  IL_0090:  ldloc.1
  IL_0091:  ldloc.2
  IL_0092:  ldloc.s    V_4
  IL_0094:  ldc.i4.0
  IL_0095:  ldloc.3
  IL_0096:  call       ""void System.Array.Copy(System.Array, int, System.Array, int, int)""
  IL_009b:  ldloc.s    V_4
  IL_009d:  ret
}
");
        }

        [Fact]
        public void FakeRangeStartFromEndIndexerArray()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var arr = new[] { 1, 2, 3, 11 };
        var result = M(arr);
        Console.WriteLine(result.Length);
        foreach (var x in result)
        {
            Console.WriteLine(x);
        }
    }
    public static int[] M(int[] array) => array[^2..];
}", TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"2
3
11");
        }

        [Fact]
        public void FakeRangeBothFromEndIndexerArray()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var arr = new[] { 1, 2, 3, 11 };
        var result = M(arr);
        Console.WriteLine(result.Length);
        foreach (var x in result)
        {
            Console.WriteLine(x);
        }
    }
    public static int[] M(int[] array) => array[^3..^1];
}", TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"2
2
3");
        }

        [Fact]
        public void FakeRangeToEndIndexerArray()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var arr = new[] { 1, 2, 3, 11 };
        var result = M(arr);
        Console.WriteLine(result.Length);
        foreach (var x in result)
        {
            Console.WriteLine(x);
        }
    }
    public static int[] M(int[] array) => array[1..];
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, verify: Verification.Passes, expectedOutput: @"3
2
3
11");
            verifier.VerifyIL("C.M", @"
{
  // Code size      152 (0x98)
  .maxstack  5
  .locals init (System.Range V_0,
                int[] V_1,
                int V_2,
                int V_3,
                int[] V_4,
                System.Index V_5)
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0006:  call       ""System.Range System.Range.FromStart(System.Index)""
  IL_000b:  stloc.0
  IL_000c:  ldarg.0
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       ""System.Index System.Range.Start.get""
  IL_0015:  stloc.s    V_5
  IL_0017:  ldloca.s   V_5
  IL_0019:  call       ""bool System.Index.FromEnd.get""
  IL_001e:  brtrue.s   IL_0032
  IL_0020:  ldloca.s   V_0
  IL_0022:  call       ""System.Index System.Range.Start.get""
  IL_0027:  stloc.s    V_5
  IL_0029:  ldloca.s   V_5
  IL_002b:  call       ""int System.Index.Value.get""
  IL_0030:  br.s       IL_0046
  IL_0032:  ldloc.1
  IL_0033:  ldlen
  IL_0034:  conv.i4
  IL_0035:  ldloca.s   V_0
  IL_0037:  call       ""System.Index System.Range.Start.get""
  IL_003c:  stloc.s    V_5
  IL_003e:  ldloca.s   V_5
  IL_0040:  call       ""int System.Index.Value.get""
  IL_0045:  sub
  IL_0046:  stloc.2
  IL_0047:  ldloca.s   V_0
  IL_0049:  call       ""System.Index System.Range.End.get""
  IL_004e:  stloc.s    V_5
  IL_0050:  ldloca.s   V_5
  IL_0052:  call       ""bool System.Index.FromEnd.get""
  IL_0057:  brtrue.s   IL_006b
  IL_0059:  ldloca.s   V_0
  IL_005b:  call       ""System.Index System.Range.End.get""
  IL_0060:  stloc.s    V_5
  IL_0062:  ldloca.s   V_5
  IL_0064:  call       ""int System.Index.Value.get""
  IL_0069:  br.s       IL_007f
  IL_006b:  ldloc.1
  IL_006c:  ldlen
  IL_006d:  conv.i4
  IL_006e:  ldloca.s   V_0
  IL_0070:  call       ""System.Index System.Range.End.get""
  IL_0075:  stloc.s    V_5
  IL_0077:  ldloca.s   V_5
  IL_0079:  call       ""int System.Index.Value.get""
  IL_007e:  sub
  IL_007f:  ldloc.2
  IL_0080:  sub
  IL_0081:  stloc.3
  IL_0082:  ldloc.3
  IL_0083:  newarr     ""int""
  IL_0088:  stloc.s    V_4
  IL_008a:  ldloc.1
  IL_008b:  ldloc.2
  IL_008c:  ldloc.s    V_4
  IL_008e:  ldc.i4.0
  IL_008f:  ldloc.3
  IL_0090:  call       ""void System.Array.Copy(System.Array, int, System.Array, int, int)""
  IL_0095:  ldloc.s    V_4
  IL_0097:  ret
}
");
        }

        [Fact]
        public void FakeRangeFromStartIndexerArray()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var arr = new[] { 1, 2, 3, 11 };
        var result = M(arr);
        Console.WriteLine(result.Length);
        foreach (var x in result)
        {
            Console.WriteLine(x);
        }
    }
    public static int[] M(int[] array) => array[..3];
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, verify: Verification.Passes, expectedOutput: @"3
1
2
3");
            verifier.VerifyIL("C.M", @"
{
  // Code size      152 (0x98)
  .maxstack  5
  .locals init (System.Range V_0,
                int[] V_1,
                int V_2,
                int V_3,
                int[] V_4,
                System.Index V_5)
  IL_0000:  ldc.i4.3
  IL_0001:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0006:  call       ""System.Range System.Range.ToEnd(System.Index)""
  IL_000b:  stloc.0
  IL_000c:  ldarg.0
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       ""System.Index System.Range.Start.get""
  IL_0015:  stloc.s    V_5
  IL_0017:  ldloca.s   V_5
  IL_0019:  call       ""bool System.Index.FromEnd.get""
  IL_001e:  brtrue.s   IL_0032
  IL_0020:  ldloca.s   V_0
  IL_0022:  call       ""System.Index System.Range.Start.get""
  IL_0027:  stloc.s    V_5
  IL_0029:  ldloca.s   V_5
  IL_002b:  call       ""int System.Index.Value.get""
  IL_0030:  br.s       IL_0046
  IL_0032:  ldloc.1
  IL_0033:  ldlen
  IL_0034:  conv.i4
  IL_0035:  ldloca.s   V_0
  IL_0037:  call       ""System.Index System.Range.Start.get""
  IL_003c:  stloc.s    V_5
  IL_003e:  ldloca.s   V_5
  IL_0040:  call       ""int System.Index.Value.get""
  IL_0045:  sub
  IL_0046:  stloc.2
  IL_0047:  ldloca.s   V_0
  IL_0049:  call       ""System.Index System.Range.End.get""
  IL_004e:  stloc.s    V_5
  IL_0050:  ldloca.s   V_5
  IL_0052:  call       ""bool System.Index.FromEnd.get""
  IL_0057:  brtrue.s   IL_006b
  IL_0059:  ldloca.s   V_0
  IL_005b:  call       ""System.Index System.Range.End.get""
  IL_0060:  stloc.s    V_5
  IL_0062:  ldloca.s   V_5
  IL_0064:  call       ""int System.Index.Value.get""
  IL_0069:  br.s       IL_007f
  IL_006b:  ldloc.1
  IL_006c:  ldlen
  IL_006d:  conv.i4
  IL_006e:  ldloca.s   V_0
  IL_0070:  call       ""System.Index System.Range.End.get""
  IL_0075:  stloc.s    V_5
  IL_0077:  ldloca.s   V_5
  IL_0079:  call       ""int System.Index.Value.get""
  IL_007e:  sub
  IL_007f:  ldloc.2
  IL_0080:  sub
  IL_0081:  stloc.3
  IL_0082:  ldloc.3
  IL_0083:  newarr     ""int""
  IL_0088:  stloc.s    V_4
  IL_008a:  ldloc.1
  IL_008b:  ldloc.2
  IL_008c:  ldloc.s    V_4
  IL_008e:  ldc.i4.0
  IL_008f:  ldloc.3
  IL_0090:  call       ""void System.Array.Copy(System.Array, int, System.Array, int, int)""
  IL_0095:  ldloc.s    V_4
  IL_0097:  ret
}
");
        }

        [Fact]
        public void LowerIndex_Int()
        {
            var compilation = CreateCompilationWithIndex(@"
using System;
public static class Util
{
    public static Index Convert(int a) => ^a;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Convert", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""System.Index..ctor(int, bool)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void LowerIndex_NullableInt()
        {
            var compilation = CreateCompilationWithIndex(@"
using System;
public static class Util
{
    public static Index? Convert(int? a) => ^a;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Convert", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int? V_0,
                System.Index? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool int?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""System.Index?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""int int?.GetValueOrDefault()""
  IL_001c:  ldc.i4.1
  IL_001d:  newobj     ""System.Index..ctor(int, bool)""
  IL_0022:  newobj     ""System.Index?..ctor(System.Index)""
  IL_0027:  ret
}");
        }

        [Fact]
        public void PrintIndexExpressions()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        int nonNullable = 1;
        int? nullableValue = 2;
        int? nullableDefault = default;

        Index a = nonNullable;
        Console.WriteLine(""a: "" + Print(a));

        Index b = ^nonNullable;
        Console.WriteLine(""b: "" + Print(b));

        // --------------------------------------------------------
        
        Index? c = nullableValue;
        Console.WriteLine(""c: "" + Print(c));

        Index? d = ^nullableValue;
        Console.WriteLine(""d: "" + Print(d));

        // --------------------------------------------------------
        
        Index? e = nullableDefault;
        Console.WriteLine(""e: "" + Print(e));

        Index? f = ^nullableDefault;
        Console.WriteLine(""f: "" + Print(f));
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"
a: value: '1', fromEnd: 'False'
b: value: '1', fromEnd: 'True'
c: value: '2', fromEnd: 'False'
d: value: '2', fromEnd: 'True'
e: default
f: default");
        }

        [Fact]
        public void LowerRange_Create_Index_Index()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range Create(Index start, Index end) => start..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Create", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""System.Range System.Range.Create(System.Index, System.Index)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void LowerRange_Create_Index_NullableIndex()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range? Create(Index start, Index? end) => start..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Create", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (System.Index V_0,
                System.Index? V_1,
                System.Range? V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_1
  IL_0006:  call       ""bool System.Index?.HasValue.get""
  IL_000b:  brtrue.s   IL_0017
  IL_000d:  ldloca.s   V_2
  IL_000f:  initobj    ""System.Range?""
  IL_0015:  ldloc.2
  IL_0016:  ret
  IL_0017:  ldloc.0
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_001f:  call       ""System.Range System.Range.Create(System.Index, System.Index)""
  IL_0024:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0029:  ret
}");
        }

        [Fact]
        public void LowerRange_Create_NullableIndex_Index()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range? Create(Index? start, Index end) => start..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Create", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (System.Index? V_0,
                System.Index V_1,
                System.Range? V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_0
  IL_0006:  call       ""bool System.Index?.HasValue.get""
  IL_000b:  brtrue.s   IL_0017
  IL_000d:  ldloca.s   V_2
  IL_000f:  initobj    ""System.Range?""
  IL_0015:  ldloc.2
  IL_0016:  ret
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_001e:  ldloc.1
  IL_001f:  call       ""System.Range System.Range.Create(System.Index, System.Index)""
  IL_0024:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0029:  ret
}");
        }

        [Fact]
        public void LowerRange_Create_NullableIndex_NullableIndex()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range? Create(Index? start, Index? end) => start..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Create", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (System.Index? V_0,
                System.Index? V_1,
                System.Range? V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_0
  IL_0006:  call       ""bool System.Index?.HasValue.get""
  IL_000b:  ldloca.s   V_1
  IL_000d:  call       ""bool System.Index?.HasValue.get""
  IL_0012:  and
  IL_0013:  brtrue.s   IL_001f
  IL_0015:  ldloca.s   V_2
  IL_0017:  initobj    ""System.Range?""
  IL_001d:  ldloc.2
  IL_001e:  ret
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_0026:  ldloca.s   V_1
  IL_0028:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_002d:  call       ""System.Range System.Range.Create(System.Index, System.Index)""
  IL_0032:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0037:  ret
}");
        }

        [Fact]
        public void LowerRange_ToEnd_Index()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range ToEnd(Index end) => ..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.ToEnd", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Range System.Range.ToEnd(System.Index)""
  IL_0006:  ret
}");
        }

        [Fact]
        public void LowerRange_ToEnd_NullableIndex()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range? ToEnd(Index? end) => ..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.ToEnd", @"
{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (System.Index? V_0,
                System.Range? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool System.Index?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""System.Range?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_001c:  call       ""System.Range System.Range.ToEnd(System.Index)""
  IL_0021:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0026:  ret
}");
        }

        [Fact]
        public void LowerRange_FromStart_Index()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range FromStart(Index start) => start..;
}");

            CompileAndVerify(compilation).VerifyIL("Util.FromStart", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Range System.Range.FromStart(System.Index)""
  IL_0006:  ret
}");
        }

        [Fact]
        public void LowerRange_FromStart_NullableIndex()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range? FromStart(Index? start) => start..;
}");

            CompileAndVerify(compilation).VerifyIL("Util.FromStart", @"
{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (System.Index? V_0,
                System.Range? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool System.Index?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""System.Range?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_001c:  call       ""System.Range System.Range.FromStart(System.Index)""
  IL_0021:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0026:  ret
}");
        }

        [Fact]
        public void LowerRange_All()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range All() => ..;
}");

            CompileAndVerify(compilation).VerifyIL("Util.All", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""System.Range System.Range.All()""
  IL_0005:  ret
}");
        }

        [Fact]
        public void PrintRangeExpressions()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Index nonNullable = 1;
        Index? nullableValue = 2;
        Index? nullableDefault = default;

        Range a = nonNullable..nonNullable;
        Console.WriteLine(""a: "" + Print(a));

        Range? b = nonNullable..nullableValue;
        Console.WriteLine(""b: "" + Print(b));

        Range? c = nonNullable..nullableDefault;
        Console.WriteLine(""c: "" + Print(c));

        // --------------------------------------------------------

        Range? d = nullableValue..nonNullable;
        Console.WriteLine(""d: "" + Print(d));

        Range? e = nullableValue..nullableValue;
        Console.WriteLine(""e: "" + Print(e));

        Range? f = nullableValue..nullableDefault;
        Console.WriteLine(""f: "" + Print(f));

        // --------------------------------------------------------

        Range? g = nullableDefault..nonNullable;
        Console.WriteLine(""g: "" + Print(g));

        Range? h = nullableDefault..nullableValue;
        Console.WriteLine(""h: "" + Print(h));

        Range? i = nullableDefault..nullableDefault;
        Console.WriteLine(""i: "" + Print(i));

        // --------------------------------------------------------

        Range? j = ..nonNullable;
        Console.WriteLine(""j: "" + Print(j));

        Range? k = ..nullableValue;
        Console.WriteLine(""k: "" + Print(k));

        Range? l = ..nullableDefault;
        Console.WriteLine(""l: "" + Print(l));

        // --------------------------------------------------------

        Range? m = nonNullable..;
        Console.WriteLine(""m: "" + Print(m));

        Range? n = nullableValue..;
        Console.WriteLine(""n: "" + Print(n));

        Range? o = nullableDefault..;
        Console.WriteLine(""o: "" + Print(o));

        // --------------------------------------------------------

        Range? p = ..;
        Console.WriteLine(""p: "" + Print(p));

    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"
a: value: 'value: '1', fromEnd: 'False'', fromEnd: 'value: '1', fromEnd: 'False''
b: value: 'value: '1', fromEnd: 'False'', fromEnd: 'value: '2', fromEnd: 'False''
c: default
d: value: 'value: '2', fromEnd: 'False'', fromEnd: 'value: '1', fromEnd: 'False''
e: value: 'value: '2', fromEnd: 'False'', fromEnd: 'value: '2', fromEnd: 'False''
f: default
g: default
h: default
i: default
j: value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '1', fromEnd: 'False''
k: value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '2', fromEnd: 'False''
l: default
m: value: 'value: '1', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''
n: value: 'value: '2', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''
o: default
p: value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''");
        }

        [Fact]
        public void PassingAsArguments()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(^1));
        Console.WriteLine(Print(..));
        Console.WriteLine(Print(2..));
        Console.WriteLine(Print(..3));
        Console.WriteLine(Print(4..5));
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: @"
value: '1', fromEnd: 'True'
value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''
value: 'value: '2', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''
value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '3', fromEnd: 'False''
value: 'value: '4', fromEnd: 'False'', fromEnd: 'value: '5', fromEnd: 'False''");
        }

        [Fact]
        public void LowerRange_OrderOfEvaluation_Index_NullableIndex()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    static void Main()
    {
        var x = Create();
    }

    public static Range? Create()
    {
        return GetIndex1() .. GetIndex2();
    }

    static Index GetIndex1()
    {
        System.Console.WriteLine(""1"");
        return default;
    }

    static Index? GetIndex2()
    {
        System.Console.WriteLine(""2"");
        return new Index(1, true);
    }
}", options: TestOptions.DebugExe);

            CompileAndVerify(compilation, expectedOutput: @"
1
2").VerifyIL("Util.Create", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (System.Index V_0,
                System.Index? V_1,
                System.Range? V_2,
                System.Range? V_3)
  IL_0000:  nop
  IL_0001:  call       ""System.Index Util.GetIndex1()""
  IL_0006:  stloc.0
  IL_0007:  call       ""System.Index? Util.GetIndex2()""
  IL_000c:  stloc.1
  IL_000d:  ldloca.s   V_1
  IL_000f:  call       ""bool System.Index?.HasValue.get""
  IL_0014:  brtrue.s   IL_0021
  IL_0016:  ldloca.s   V_2
  IL_0018:  initobj    ""System.Range?""
  IL_001e:  ldloc.2
  IL_001f:  br.s       IL_0033
  IL_0021:  ldloc.0
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_0029:  call       ""System.Range System.Range.Create(System.Index, System.Index)""
  IL_002e:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0033:  stloc.3
  IL_0034:  br.s       IL_0036
  IL_0036:  ldloc.3
  IL_0037:  ret
}");
        }

        [Fact]
        public void LowerRange_OrderOfEvaluation_Index_Null()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    static void Main()
    {
        var x = Create();
    }

    public static Range? Create()
    {
        return GetIndex1() .. GetIndex2();
    }

    static Index GetIndex1()
    {
        System.Console.WriteLine(""1"");
        return default;
    }

    static Index? GetIndex2()
    {
        System.Console.WriteLine(""2"");
        return null;
    }
}", options: TestOptions.DebugExe);

            CompileAndVerify(compilation, expectedOutput: @"
1
2").VerifyIL("Util.Create", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (System.Index V_0,
                System.Index? V_1,
                System.Range? V_2,
                System.Range? V_3)
  IL_0000:  nop
  IL_0001:  call       ""System.Index Util.GetIndex1()""
  IL_0006:  stloc.0
  IL_0007:  call       ""System.Index? Util.GetIndex2()""
  IL_000c:  stloc.1
  IL_000d:  ldloca.s   V_1
  IL_000f:  call       ""bool System.Index?.HasValue.get""
  IL_0014:  brtrue.s   IL_0021
  IL_0016:  ldloca.s   V_2
  IL_0018:  initobj    ""System.Range?""
  IL_001e:  ldloc.2
  IL_001f:  br.s       IL_0033
  IL_0021:  ldloc.0
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_0029:  call       ""System.Range System.Range.Create(System.Index, System.Index)""
  IL_002e:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0033:  stloc.3
  IL_0034:  br.s       IL_0036
  IL_0036:  ldloc.3
  IL_0037:  ret
}");
        }

        [Fact]
        public void Index_OperandConvertibleToInt()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        byte a = 3;
        Index b = ^a;
        Console.WriteLine(Print(b));
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe), expectedOutput: "value: '3', fromEnd: 'True'");
        }

        [Fact]
        public void Index_NullableAlwaysHasValue()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create()));
    }
    static Index? Create()
    {
        // should be lowered into: new Nullable<Index>(new Index(5, fromEnd: true))
        return ^new Nullable<int>(5);
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: '5', fromEnd: 'True'")
                .VerifyIL("Program.Create", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""System.Index..ctor(int, bool)""
  IL_0007:  newobj     ""System.Index?..ctor(System.Index)""
  IL_000c:  ret
}");
        }

        [Fact]
        public void Range_NullableAlwaysHasValue_Left()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create(^1)));
    }
    static Range? Create(Index arg)
    {
        // should be lowered into: new Nullable<Range>(Range.FromStart(arg))
        return new Nullable<Index>(arg)..;
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '1', fromEnd: 'True'', fromEnd: 'value: '0', fromEnd: 'True''")
                .VerifyIL("Program.Create", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Range System.Range.FromStart(System.Index)""
  IL_0006:  newobj     ""System.Range?..ctor(System.Range)""
  IL_000b:  ret
}");
        }

        [Fact]
        public void Range_NullableAlwaysHasValue_Right()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create(^1)));
    }
    static Range? Create(Index arg)
    {
        // should be lowered into: new Nullable<Range>(Range.ToEnd(arg))
        return ..new Nullable<Index>(arg);
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '1', fromEnd: 'True''")
                .VerifyIL("Program.Create", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Range System.Range.ToEnd(System.Index)""
  IL_0006:  newobj     ""System.Range?..ctor(System.Range)""
  IL_000b:  ret
}");
        }

        [Fact]
        public void Range_NullableAlwaysHasValue_Both()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create(^2, ^1)));
    }
    static Range? Create(Index arg1, Index arg2)
    {
        // should be lowered into: new Nullable<Range>(Range.Create(arg1, arg2))
        return new Nullable<Index>(arg1)..new Nullable<Index>(arg2);
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '2', fromEnd: 'True'', fromEnd: 'value: '1', fromEnd: 'True''")
                .VerifyIL("Program.Create", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""System.Range System.Range.Create(System.Index, System.Index)""
  IL_0007:  newobj     ""System.Range?..ctor(System.Range)""
  IL_000c:  ret
}");
        }

        [Fact]
        public void Index_NullableNeverHasValue()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create()));
    }
    static Index? Create()
    {
        // should be lowered into: new Nullable<Index>(new Index(default, fromEnd: true))
        return ^new Nullable<int>();
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe), expectedOutput: "value: '0', fromEnd: 'True'")
                .VerifyIL("Program.Create", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""System.Index..ctor(int, bool)""
  IL_0007:  newobj     ""System.Index?..ctor(System.Index)""
  IL_000c:  ret
}");
        }

        [Fact]
        public void Range_NullableNeverhasValue_Left()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create()));
    }
    static Range? Create()
    {
        // should be lowered into: new Nullable<Range>(Range.FromStart(default))
        return new Nullable<Index>()..;
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''")
                .VerifyIL("Program.Create", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (System.Index V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.Index""
  IL_0008:  ldloc.0
  IL_0009:  call       ""System.Range System.Range.FromStart(System.Index)""
  IL_000e:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0013:  ret
}");
        }

        [Fact]
        public void Range_NullableNeverHasValue_Right()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create()));
    }
    static Range? Create()
    {
        // should be lowered into: new Nullable<Range>(Range.ToEnd(default))
        return ..new Nullable<Index>();
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'False''")
                .VerifyIL("Program.Create", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (System.Index V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.Index""
  IL_0008:  ldloc.0
  IL_0009:  call       ""System.Range System.Range.ToEnd(System.Index)""
  IL_000e:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0013:  ret
}");
        }

        [Fact]
        public void Range_NullableNeverHasValue_Both()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create()));
    }
    static Range? Create()
    {
        // should be lowered into: new Nullable<Range>(Range.Create(default, default))
        return new Nullable<Index>()..new Nullable<Index>();
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'False''")
                .VerifyIL("Program.Create", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (System.Index V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.Index""
  IL_0008:  ldloc.0
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    ""System.Index""
  IL_0011:  ldloc.0
  IL_0012:  call       ""System.Range System.Range.Create(System.Index, System.Index)""
  IL_0017:  newobj     ""System.Range?..ctor(System.Range)""
  IL_001c:  ret
}");
        }

        [Fact]
        public void Index_OnFunctionCall()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(^Create(5)));
    }
    static int Create(int x) => x;
}" + PrintIndexesAndRangesCode,
                options: TestOptions.ReleaseExe),
                expectedOutput: "value: '5', fromEnd: 'True'");
        }

        [Fact]
        public void Range_OnFunctionCall()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create(1)..Create(2)));
    }
    static Index Create(int x) => ^x;
}" + PrintIndexesAndRangesCode,
                options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '1', fromEnd: 'True'', fromEnd: 'value: '2', fromEnd: 'True''");
        }

        [Fact]
        public void Index_OnAssignment()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        int x = default;
        Console.WriteLine(Print(^(x = Create(5))));
        Console.WriteLine(x);
    }
    static int Create(int x) => x;
}" + PrintIndexesAndRangesCode,
                options: TestOptions.ReleaseExe),
                expectedOutput: @"
value: '5', fromEnd: 'True'
5");
        }

        [Fact]
        public void Range_OnAssignment()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Index x = default, y = default;
        Console.WriteLine(Print((x = Create(1))..(y = Create(2))));
        Console.WriteLine(Print(x));
        Console.WriteLine(Print(y));
    }
    static Index Create(int x) => ^x;
}" + PrintIndexesAndRangesCode,
                options: TestOptions.ReleaseExe),
                expectedOutput: @"
value: 'value: '1', fromEnd: 'True'', fromEnd: 'value: '2', fromEnd: 'True''
value: '1', fromEnd: 'True'
value: '2', fromEnd: 'True'");
        }

        [Fact]
        public void Range_OnVarOut()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create(1, out Index y)..y));
    }
    static Index Create(int x, out Index y)
    {
        y = ^2;
        return ^x;
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '1', fromEnd: 'True'', fromEnd: 'value: '2', fromEnd: 'True''");
        }

        [Fact]
        public void Range_EvaluationInCondition()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        if ((Create(1, out int a)..Create(2, out int b)).Start.FromEnd && a < b)
        {
            Console.WriteLine(""YES"");
        }
        if ((Create(4, out int c)..Create(3, out int d)).Start.FromEnd && c < d)
        {
            Console.WriteLine(""NO"");
        }
    }
    static Index Create(int x, out int y)
    {
        y = x;
        return ^x;
    }
}", options: TestOptions.ReleaseExe), expectedOutput: "YES");
        }

        private const string PrintIndexesAndRangesCode = @"
partial class Program
{
    static string Print(Index arg)
    {
        return $""value: '{arg.Value}', fromEnd: '{arg.FromEnd}'"";
    }
    static string Print(Range arg)
    {
        return $""value: '{Print(arg.Start)}', fromEnd: '{Print(arg.End)}'"";
    }
    static string Print(Index? arg)
    {
        if (arg.HasValue)
        {
            return Print(arg.Value);
        }
        else
        {
            return ""default"";
        }
    }
    static string Print(Range? arg)
    {
        if (arg.HasValue)
        {
            return Print(arg.Value);
        }
        else
        {
            return ""default"";
        }
    }
}";
    }
}
