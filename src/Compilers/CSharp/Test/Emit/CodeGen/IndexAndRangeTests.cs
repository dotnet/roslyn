// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class IndexAndRangeTests : CSharpTestBase
    {
        private CompilationVerifier CompileAndVerifyWithIndexAndRange(string s, string expectedOutput = null)
        {
            var comp = CreateCompilationWithIndexAndRange(s, TestOptions.ReleaseExe);
            return CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [Fact]
        public void RefToArrayIndexIndexer()
        {
            var verifier = CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        int[] x = { 0, 1, 2, 3 };
        ref int r1 = ref x[2];
        Console.WriteLine(r1);
        ref int r2 = ref x[^2];
        Console.WriteLine(r2);
        r2 = 7;
        Console.WriteLine(r1);
        Console.WriteLine(r2);
        r1 = 5;
        Console.WriteLine(r1);
        Console.WriteLine(r2);
    }
}", expectedOutput: @"2
2
7
7
5
5");
            verifier.VerifyIL("C.Main", @"
{
  // Code size      120 (0x78)
  .maxstack  4
  .locals init (int& V_0, //r1
                int& V_1, //r2
                System.Index V_2,
                int[] V_3)
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.02E4414E7DFA0F3AA2387EE8EA7AB31431CB406A""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  dup
  IL_0012:  ldc.i4.2
  IL_0013:  ldelema    ""int""
  IL_0018:  stloc.0
  IL_0019:  ldloc.0
  IL_001a:  ldind.i4
  IL_001b:  call       ""void System.Console.WriteLine(int)""
  IL_0020:  ldloca.s   V_2
  IL_0022:  ldc.i4.2
  IL_0023:  ldc.i4.1
  IL_0024:  call       ""System.Index..ctor(int, bool)""
  IL_0029:  stloc.3
  IL_002a:  ldloc.3
  IL_002b:  ldloca.s   V_2
  IL_002d:  call       ""bool System.Index.IsFromEnd.get""
  IL_0032:  brtrue.s   IL_003d
  IL_0034:  ldloca.s   V_2
  IL_0036:  call       ""int System.Index.Value.get""
  IL_003b:  br.s       IL_0048
  IL_003d:  ldloc.3
  IL_003e:  ldlen
  IL_003f:  conv.i4
  IL_0040:  ldloca.s   V_2
  IL_0042:  call       ""int System.Index.Value.get""
  IL_0047:  sub
  IL_0048:  ldelema    ""int""
  IL_004d:  stloc.1
  IL_004e:  ldloc.1
  IL_004f:  ldind.i4
  IL_0050:  call       ""void System.Console.WriteLine(int)""
  IL_0055:  ldloc.1
  IL_0056:  ldc.i4.7
  IL_0057:  stind.i4
  IL_0058:  ldloc.0
  IL_0059:  ldind.i4
  IL_005a:  call       ""void System.Console.WriteLine(int)""
  IL_005f:  ldloc.1
  IL_0060:  ldind.i4
  IL_0061:  call       ""void System.Console.WriteLine(int)""
  IL_0066:  ldloc.0
  IL_0067:  ldc.i4.5
  IL_0068:  stind.i4
  IL_0069:  ldloc.0
  IL_006a:  ldind.i4
  IL_006b:  call       ""void System.Console.WriteLine(int)""
  IL_0070:  ldloc.1
  IL_0071:  ldind.i4
  IL_0072:  call       ""void System.Console.WriteLine(int)""
  IL_0077:  ret
}");
        }

        [Fact]
        public void RangeIndexerStringIsFromEndStart()
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
        public void FakeRangeIndexerStringBothIsFromEnd()
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
  IL_000d:  call       ""bool System.Index.IsFromEnd.get""
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
  IL_0049:  call       ""bool System.Index.IsFromEnd.get""
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
  // Code size      152 (0x98)
  .maxstack  4
  .locals init (System.Range V_0,
                string V_1,
                int V_2,
                int V_3,
                System.Index V_4)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0008:  ldc.i4.3
  IL_0009:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_000e:  call       ""System.Range..ctor(System.Index, System.Index)""
  IL_0013:  ldarg.0
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""System.Index System.Range.Start.get""
  IL_001c:  stloc.s    V_4
  IL_001e:  ldloca.s   V_4
  IL_0020:  call       ""bool System.Index.IsFromEnd.get""
  IL_0025:  brtrue.s   IL_0039
  IL_0027:  ldloca.s   V_0
  IL_0029:  call       ""System.Index System.Range.Start.get""
  IL_002e:  stloc.s    V_4
  IL_0030:  ldloca.s   V_4
  IL_0032:  call       ""int System.Index.Value.get""
  IL_0037:  br.s       IL_0050
  IL_0039:  ldloc.1
  IL_003a:  callvirt   ""int string.Length.get""
  IL_003f:  ldloca.s   V_0
  IL_0041:  call       ""System.Index System.Range.Start.get""
  IL_0046:  stloc.s    V_4
  IL_0048:  ldloca.s   V_4
  IL_004a:  call       ""int System.Index.Value.get""
  IL_004f:  sub
  IL_0050:  stloc.2
  IL_0051:  ldloca.s   V_0
  IL_0053:  call       ""System.Index System.Range.End.get""
  IL_0058:  stloc.s    V_4
  IL_005a:  ldloca.s   V_4
  IL_005c:  call       ""bool System.Index.IsFromEnd.get""
  IL_0061:  brtrue.s   IL_0075
  IL_0063:  ldloca.s   V_0
  IL_0065:  call       ""System.Index System.Range.End.get""
  IL_006a:  stloc.s    V_4
  IL_006c:  ldloca.s   V_4
  IL_006e:  call       ""int System.Index.Value.get""
  IL_0073:  br.s       IL_008c
  IL_0075:  ldloc.1
  IL_0076:  callvirt   ""int string.Length.get""
  IL_007b:  ldloca.s   V_0
  IL_007d:  call       ""System.Index System.Range.End.get""
  IL_0082:  stloc.s    V_4
  IL_0084:  ldloca.s   V_4
  IL_0086:  call       ""int System.Index.Value.get""
  IL_008b:  sub
  IL_008c:  stloc.3
  IL_008d:  ldloc.1
  IL_008e:  ldloc.2
  IL_008f:  ldloc.3
  IL_0090:  ldloc.2
  IL_0091:  sub
  IL_0092:  callvirt   ""string string.Substring(int, int)""
  IL_0097:  ret
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
  // Code size       95 (0x5f)
  .maxstack  3
  .locals init (System.Index V_0,
                int[] V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.0
  IL_0004:  call       ""System.Index..ctor(int, bool)""
  IL_0009:  ldarg.0
  IL_000a:  stloc.1
  IL_000b:  ldloc.1
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       ""bool System.Index.IsFromEnd.get""
  IL_0013:  brtrue.s   IL_001e
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""int System.Index.Value.get""
  IL_001c:  br.s       IL_0029
  IL_001e:  ldloc.1
  IL_001f:  ldlen
  IL_0020:  conv.i4
  IL_0021:  ldloca.s   V_0
  IL_0023:  call       ""int System.Index.Value.get""
  IL_0028:  sub
  IL_0029:  ldelem.i4
  IL_002a:  call       ""void System.Console.WriteLine(int)""
  IL_002f:  ldloca.s   V_0
  IL_0031:  ldc.i4.1
  IL_0032:  ldc.i4.1
  IL_0033:  call       ""System.Index..ctor(int, bool)""
  IL_0038:  ldarg.0
  IL_0039:  stloc.1
  IL_003a:  ldloc.1
  IL_003b:  ldloca.s   V_0
  IL_003d:  call       ""bool System.Index.IsFromEnd.get""
  IL_0042:  brtrue.s   IL_004d
  IL_0044:  ldloca.s   V_0
  IL_0046:  call       ""int System.Index.Value.get""
  IL_004b:  br.s       IL_0058
  IL_004d:  ldloc.1
  IL_004e:  ldlen
  IL_004f:  conv.i4
  IL_0050:  ldloca.s   V_0
  IL_0052:  call       ""int System.Index.Value.get""
  IL_0057:  sub
  IL_0058:  ldelem.i4
  IL_0059:  call       ""void System.Console.WriteLine(int)""
  IL_005e:  ret
}");
        }

        [Fact]
        public void SuppressNullableWarning_FakeIndexIndexerArray()
        {
            string source = @"
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
        Console.Write(array[new Index(1, false)!]);
        Console.Write(array[(^1)!]);
    }
}";
            // cover case in ConvertToArrayIndex
            var comp = CreateCompilationWithIndex(source, WithNonNullTypesTrue(TestOptions.DebugExe));
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "211");
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
        public bool IsFromEnd => _value < 0;

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
  // Code size      159 (0x9f)
  .maxstack  5
  .locals init (System.Range V_0,
                int[] V_1,
                int V_2,
                int V_3,
                int[] V_4,
                System.Index V_5)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0008:  ldc.i4.3
  IL_0009:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_000e:  call       ""System.Range..ctor(System.Index, System.Index)""
  IL_0013:  ldarg.0
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""System.Index System.Range.Start.get""
  IL_001c:  stloc.s    V_5
  IL_001e:  ldloca.s   V_5
  IL_0020:  call       ""bool System.Index.IsFromEnd.get""
  IL_0025:  brtrue.s   IL_0039
  IL_0027:  ldloca.s   V_0
  IL_0029:  call       ""System.Index System.Range.Start.get""
  IL_002e:  stloc.s    V_5
  IL_0030:  ldloca.s   V_5
  IL_0032:  call       ""int System.Index.Value.get""
  IL_0037:  br.s       IL_004d
  IL_0039:  ldloc.1
  IL_003a:  ldlen
  IL_003b:  conv.i4
  IL_003c:  ldloca.s   V_0
  IL_003e:  call       ""System.Index System.Range.Start.get""
  IL_0043:  stloc.s    V_5
  IL_0045:  ldloca.s   V_5
  IL_0047:  call       ""int System.Index.Value.get""
  IL_004c:  sub
  IL_004d:  stloc.2
  IL_004e:  ldloca.s   V_0
  IL_0050:  call       ""System.Index System.Range.End.get""
  IL_0055:  stloc.s    V_5
  IL_0057:  ldloca.s   V_5
  IL_0059:  call       ""bool System.Index.IsFromEnd.get""
  IL_005e:  brtrue.s   IL_0072
  IL_0060:  ldloca.s   V_0
  IL_0062:  call       ""System.Index System.Range.End.get""
  IL_0067:  stloc.s    V_5
  IL_0069:  ldloca.s   V_5
  IL_006b:  call       ""int System.Index.Value.get""
  IL_0070:  br.s       IL_0086
  IL_0072:  ldloc.1
  IL_0073:  ldlen
  IL_0074:  conv.i4
  IL_0075:  ldloca.s   V_0
  IL_0077:  call       ""System.Index System.Range.End.get""
  IL_007c:  stloc.s    V_5
  IL_007e:  ldloca.s   V_5
  IL_0080:  call       ""int System.Index.Value.get""
  IL_0085:  sub
  IL_0086:  ldloc.2
  IL_0087:  sub
  IL_0088:  stloc.3
  IL_0089:  ldloc.3
  IL_008a:  newarr     ""int""
  IL_008f:  stloc.s    V_4
  IL_0091:  ldloc.1
  IL_0092:  ldloc.2
  IL_0093:  ldloc.s    V_4
  IL_0095:  ldc.i4.0
  IL_0096:  ldloc.3
  IL_0097:  call       ""void System.Array.Copy(System.Array, int, System.Array, int, int)""
  IL_009c:  ldloc.s    V_4
  IL_009e:  ret
}
");
        }

        [Fact]
        public void FakeRangeStartIsFromEndIndexerArray()
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
        public void FakeRangeBothIsFromEndIndexerArray()
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
  // Code size      160 (0xa0)
  .maxstack  5
  .locals init (System.Range V_0,
                int[] V_1,
                int V_2,
                int V_3,
                int[] V_4,
                System.Index V_5)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  newobj     ""System.Index..ctor(int, bool)""
  IL_000f:  call       ""System.Range..ctor(System.Index, System.Index)""
  IL_0014:  ldarg.0
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""System.Index System.Range.Start.get""
  IL_001d:  stloc.s    V_5
  IL_001f:  ldloca.s   V_5
  IL_0021:  call       ""bool System.Index.IsFromEnd.get""
  IL_0026:  brtrue.s   IL_003a
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""System.Index System.Range.Start.get""
  IL_002f:  stloc.s    V_5
  IL_0031:  ldloca.s   V_5
  IL_0033:  call       ""int System.Index.Value.get""
  IL_0038:  br.s       IL_004e
  IL_003a:  ldloc.1
  IL_003b:  ldlen
  IL_003c:  conv.i4
  IL_003d:  ldloca.s   V_0
  IL_003f:  call       ""System.Index System.Range.Start.get""
  IL_0044:  stloc.s    V_5
  IL_0046:  ldloca.s   V_5
  IL_0048:  call       ""int System.Index.Value.get""
  IL_004d:  sub
  IL_004e:  stloc.2
  IL_004f:  ldloca.s   V_0
  IL_0051:  call       ""System.Index System.Range.End.get""
  IL_0056:  stloc.s    V_5
  IL_0058:  ldloca.s   V_5
  IL_005a:  call       ""bool System.Index.IsFromEnd.get""
  IL_005f:  brtrue.s   IL_0073
  IL_0061:  ldloca.s   V_0
  IL_0063:  call       ""System.Index System.Range.End.get""
  IL_0068:  stloc.s    V_5
  IL_006a:  ldloca.s   V_5
  IL_006c:  call       ""int System.Index.Value.get""
  IL_0071:  br.s       IL_0087
  IL_0073:  ldloc.1
  IL_0074:  ldlen
  IL_0075:  conv.i4
  IL_0076:  ldloca.s   V_0
  IL_0078:  call       ""System.Index System.Range.End.get""
  IL_007d:  stloc.s    V_5
  IL_007f:  ldloca.s   V_5
  IL_0081:  call       ""int System.Index.Value.get""
  IL_0086:  sub
  IL_0087:  ldloc.2
  IL_0088:  sub
  IL_0089:  stloc.3
  IL_008a:  ldloc.3
  IL_008b:  newarr     ""int""
  IL_0090:  stloc.s    V_4
  IL_0092:  ldloc.1
  IL_0093:  ldloc.2
  IL_0094:  ldloc.s    V_4
  IL_0096:  ldc.i4.0
  IL_0097:  ldloc.3
  IL_0098:  call       ""void System.Array.Copy(System.Array, int, System.Array, int, int)""
  IL_009d:  ldloc.s    V_4
  IL_009f:  ret
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
  // Code size      160 (0xa0)
  .maxstack  5
  .locals init (System.Range V_0,
                int[] V_1,
                int V_2,
                int V_3,
                int[] V_4,
                System.Index V_5)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.0
  IL_0004:  newobj     ""System.Index..ctor(int, bool)""
  IL_0009:  ldc.i4.3
  IL_000a:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_000f:  call       ""System.Range..ctor(System.Index, System.Index)""
  IL_0014:  ldarg.0
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""System.Index System.Range.Start.get""
  IL_001d:  stloc.s    V_5
  IL_001f:  ldloca.s   V_5
  IL_0021:  call       ""bool System.Index.IsFromEnd.get""
  IL_0026:  brtrue.s   IL_003a
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""System.Index System.Range.Start.get""
  IL_002f:  stloc.s    V_5
  IL_0031:  ldloca.s   V_5
  IL_0033:  call       ""int System.Index.Value.get""
  IL_0038:  br.s       IL_004e
  IL_003a:  ldloc.1
  IL_003b:  ldlen
  IL_003c:  conv.i4
  IL_003d:  ldloca.s   V_0
  IL_003f:  call       ""System.Index System.Range.Start.get""
  IL_0044:  stloc.s    V_5
  IL_0046:  ldloca.s   V_5
  IL_0048:  call       ""int System.Index.Value.get""
  IL_004d:  sub
  IL_004e:  stloc.2
  IL_004f:  ldloca.s   V_0
  IL_0051:  call       ""System.Index System.Range.End.get""
  IL_0056:  stloc.s    V_5
  IL_0058:  ldloca.s   V_5
  IL_005a:  call       ""bool System.Index.IsFromEnd.get""
  IL_005f:  brtrue.s   IL_0073
  IL_0061:  ldloca.s   V_0
  IL_0063:  call       ""System.Index System.Range.End.get""
  IL_0068:  stloc.s    V_5
  IL_006a:  ldloca.s   V_5
  IL_006c:  call       ""int System.Index.Value.get""
  IL_0071:  br.s       IL_0087
  IL_0073:  ldloc.1
  IL_0074:  ldlen
  IL_0075:  conv.i4
  IL_0076:  ldloca.s   V_0
  IL_0078:  call       ""System.Index System.Range.End.get""
  IL_007d:  stloc.s    V_5
  IL_007f:  ldloca.s   V_5
  IL_0081:  call       ""int System.Index.Value.get""
  IL_0086:  sub
  IL_0087:  ldloc.2
  IL_0088:  sub
  IL_0089:  stloc.3
  IL_008a:  ldloc.3
  IL_008b:  newarr     ""int""
  IL_0090:  stloc.s    V_4
  IL_0092:  ldloc.1
  IL_0093:  ldloc.2
  IL_0094:  ldloc.s    V_4
  IL_0096:  ldc.i4.0
  IL_0097:  ldloc.3
  IL_0098:  call       ""void System.Array.Copy(System.Array, int, System.Array, int, int)""
  IL_009d:  ldloc.s    V_4
  IL_009f:  ret
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
  IL_0002:  newobj     ""System.Range..ctor(System.Index, System.Index)""
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
  IL_001f:  newobj     ""System.Range..ctor(System.Index, System.Index)""
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
  IL_001f:  newobj     ""System.Range..ctor(System.Index, System.Index)""
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
  IL_002d:  newobj     ""System.Range..ctor(System.Index, System.Index)""
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
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.0
  IL_0002:  newobj     ""System.Index..ctor(int, bool)""
  IL_0007:  ldarg.0
  IL_0008:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_000d:  ret
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
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (System.Index V_0,
                System.Index? V_1,
                System.Range? V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.0
  IL_0004:  call       ""System.Index..ctor(int, bool)""
  IL_0009:  ldarg.0
  IL_000a:  stloc.1
  IL_000b:  ldloca.s   V_1
  IL_000d:  call       ""bool System.Index?.HasValue.get""
  IL_0012:  brtrue.s   IL_001e
  IL_0014:  ldloca.s   V_2
  IL_0016:  initobj    ""System.Range?""
  IL_001c:  ldloc.2
  IL_001d:  ret
  IL_001e:  ldloc.0
  IL_001f:  ldloca.s   V_1
  IL_0021:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_0026:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_002b:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0030:  ret
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
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldc.i4.1
  IL_0003:  newobj     ""System.Index..ctor(int, bool)""
  IL_0008:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_000d:  ret
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
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (System.Index? V_0,
                System.Index V_1,
                System.Range? V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  ldc.i4.0
  IL_0005:  ldc.i4.1
  IL_0006:  call       ""System.Index..ctor(int, bool)""
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""bool System.Index?.HasValue.get""
  IL_0012:  brtrue.s   IL_001e
  IL_0014:  ldloca.s   V_2
  IL_0016:  initobj    ""System.Range?""
  IL_001c:  ldloc.2
  IL_001d:  ret
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_0025:  ldloc.1
  IL_0026:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_002b:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0030:  ret
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
  // Code size       20 (0x14)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.0
  IL_0002:  newobj     ""System.Index..ctor(int, bool)""
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  newobj     ""System.Index..ctor(int, bool)""
  IL_000e:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0013:  ret
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
  IL_0029:  newobj     ""System.Range..ctor(System.Index, System.Index)""
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
  IL_0029:  newobj     ""System.Range..ctor(System.Index, System.Index)""
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
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldc.i4.1
  IL_0003:  newobj     ""System.Index..ctor(int, bool)""
  IL_0008:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_000d:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0012:  ret
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
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.0
  IL_0002:  newobj     ""System.Index..ctor(int, bool)""
  IL_0007:  ldarg.0
  IL_0008:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_000d:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0012:  ret
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
  IL_0002:  newobj     ""System.Range..ctor(System.Index, System.Index)""
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
  // Code size       27 (0x1b)
  .maxstack  3
  .locals init (System.Index V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.Index""
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.1
  IL_000b:  newobj     ""System.Index..ctor(int, bool)""
  IL_0010:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0015:  newobj     ""System.Range?..ctor(System.Range)""
  IL_001a:  ret
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
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (System.Index V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.0
  IL_0002:  newobj     ""System.Index..ctor(int, bool)""
  IL_0007:  ldloca.s   V_0
  IL_0009:  initobj    ""System.Index""
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0015:  newobj     ""System.Range?..ctor(System.Range)""
  IL_001a:  ret
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
  IL_0012:  newobj     ""System.Range..ctor(System.Index, System.Index)""
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
        if ((Create(1, out int a)..Create(2, out int b)).Start.IsFromEnd && a < b)
        {
            Console.WriteLine(""YES"");
        }
        if ((Create(4, out int c)..Create(3, out int d)).Start.IsFromEnd && c < d)
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
        return $""value: '{arg.Value}', fromEnd: '{arg.IsFromEnd}'"";
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
