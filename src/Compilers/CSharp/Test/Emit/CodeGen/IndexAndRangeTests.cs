﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    // PROTOTYPE: add tests for when either operands of range is optimized using NullableAlwaysHasValue
    // PROTOTYPE: add tests for when either operands of range is optimized using NullableNeverHasValue (not implemented yet)

    public class IndexAndRangeTests : CSharpTestBase
    {
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
            var compilation = CreateCompilationWithIndex(@"
using System;
class Program
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
    static string Print(Index? arg)
    {
        if (arg.HasValue)
        {
            return $""value: '{arg.Value.Value}', fromEnd: '{arg.Value.FromEnd}'"";
        }
        else
        {
            return ""default"";
        }
    }
}", options: TestOptions.ReleaseExe);

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
class Program
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
    static string Print(Index? arg)
    {
        if (arg.HasValue)
        {
            return $""value: '{arg.Value.Value}', fromEnd: '{arg.Value.FromEnd}'"";
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
            return $""value: '{Print(arg.Value.Start)}', fromEnd: '{Print(arg.Value.End)}'"";
        }
        else
        {
            return ""default"";
        }
    }
}", options: TestOptions.ReleaseExe);

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
class Program
{
    static void Main()
    {
        Console.WriteLine(Print(^1));
        Console.WriteLine(Print(..));
        Console.WriteLine(Print(2..));
        Console.WriteLine(Print(..3));
        Console.WriteLine(Print(4..5));
    }
    static string Print(Index arg)
    {
        return $""value: '{arg.Value}', fromEnd: '{arg.FromEnd}'"";
    }
    static string Print(Range arg)
    {
        return $""value: '{Print(arg.Start)}', fromEnd: '{Print(arg.End)}'"";
    }
}", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: @"
value: '1', fromEnd: 'True'
value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''
value: 'value: '2', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''
value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '3', fromEnd: 'False''
value: 'value: '4', fromEnd: 'False'', fromEnd: 'value: '5', fromEnd: 'False''");
        }

        [Fact]
        public void ExtensionIndexer_Error()
        {
            CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
public static class Program
{
    public static char get_IndexerExtension(this string foo, Index index)
    {
        return index.FromEnd ? foo[foo.Length - index.Value] : foo[index.Value];
    }
    public static void Main()
    {
        var foo = ""abcdefg"";
        foo[^3] = ""invalid"";
    }
}").VerifyDiagnostics(
                // (12,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         foo[^3] = "invalid";
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "foo[^3]").WithLocation(12, 9));
        }

        [Fact]
        public void ExtensionIndexer_StringIndex()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
public static class Program
{
    public static char get_IndexerExtension(this string foo, Index index)
    {
        return index.FromEnd ? foo[foo.Length - index.Value] : foo[index.Value];
    }
    public static void Main()
    {
        var foo = ""abcdefg"";
        Console.WriteLine(foo[3] + "" -- "" + foo[^1]);
    }
}", options: TestOptions.ReleaseExe), expectedOutput: "d -- g");
        }

        [Fact]
        public void ExtensionIndexer_StringRange()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
public static class Program
{
    public static string get_IndexerExtension(this string foo, Range range)
    {
        int start = range.Start.FromEnd ? (foo.Length - Math.Max(range.Start.Value, 1)) : range.Start.Value;
        int end = range.End.FromEnd ? (foo.Length - Math.Max(range.End.Value, 1)) : range.End.Value;
        return foo.Substring(start, end - start + 1);
    }
    public static void Main()
    {
        var foo = ""abcdefg"";
        Console.WriteLine(foo[1..2] + "" -- "" + foo[^2..^1]);
        Console.WriteLine(foo[..0] + "" -- "" + foo[..^2]);
        Console.WriteLine(foo[1..] + "" -- "" + foo[^3..]);
        Console.WriteLine(foo[..]);
    }
}", options: TestOptions.ReleaseExe), expectedOutput: @"
bc -- fg
a -- abcdef
bcdefg -- efg
abcdefg");
        }

        [Fact]
        public void ExtensionIndexer_SpanIndex()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
public static class Program
{
    public static int get_IndexerExtension(this Span<int> foo, Index index)
    {
        return foo[index.FromEnd ? (foo.Length - index.Value) : index.Value];
    }
    public static void Main()
    {
        Span<int> foo = new Span<int>(new [] { 0, 1, 2, 3, 4, 5, 6 });
        Console.WriteLine(foo[3] + "" -- "" + foo[^1]);
    }
}", options: TestOptions.ReleaseExe), expectedOutput: "3 -- 6");
        }

        [Fact]
        public void ExtensionIndexer_SpanRange()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
using System.Linq;
public static class Program
{
    public static string get_IndexerExtension(this Span<int> foo, Range range)
    {
        int start = range.Start.FromEnd ? (foo.Length - Math.Max(range.Start.Value, 1)) : range.Start.Value;
        int end = range.End.FromEnd ? (foo.Length - Math.Max(range.End.Value, 1)) : range.End.Value;

        int[] ar = new int[end - start + 1];
        for(var i = start; i <= end; i++)
        {
            ar[i - start] = foo[i];
        }

        return ""["" + string.Join("", "", ar) + ""]"";
    }
    public static void Main()
    {
        var foo = new Span<int>(new [] { 0, 1, 2, 3, 4, 5, 6 });
        Console.WriteLine(foo[1..2] + "" -- "" + foo[^2..^1]);
        Console.WriteLine(foo[..0] + "" -- "" + foo[..^2]);
        Console.WriteLine(foo[1..] + "" -- "" + foo[^3..]);
        Console.WriteLine(foo[..]);
    }
}", options: TestOptions.UnsafeReleaseExe), expectedOutput: @"
[1, 2] -- [5, 6]
[0] -- [0, 1, 2, 3, 4, 5]
[1, 2, 3, 4, 5, 6] -- [4, 5, 6]
[0, 1, 2, 3, 4, 5, 6]");
        }

        [Fact]
        public void ExtensionIndexer_ArrayIndex()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
public static class Program
{
    public static int get_IndexerExtension(this int[] foo, Index index)
    {
        return foo[index.FromEnd ? (foo.Length - index.Value) : index.Value];
    }
    public static void Main()
    {
        int[] foo = new [] { 0, 1, 2, 3, 4, 5, 6 };
        Console.WriteLine(foo[3] + "" -- "" + foo[^1]);
    }
}", options: TestOptions.ReleaseExe), expectedOutput: "3 -- 6");
        }

        [Fact]
        public void ExtensionIndexer_ArrayRange()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
using System.Linq;
public static class Program
{
    public static string get_IndexerExtension(this int[] foo, Range range)
    {
        int start = range.Start.FromEnd ? (foo.Length - Math.Max(range.Start.Value, 1)) : range.Start.Value;
        int end = range.End.FromEnd ? (foo.Length - Math.Max(range.End.Value, 1)) : range.End.Value;

        int[] ar = new int[end - start + 1];
        for(var i = start; i <= end; i++)
        {
            ar[i - start] = foo[i];
        }

        return ""["" + string.Join("", "", ar) + ""]"";
    }
    public static void Main()
    {
        int[] foo = new [] { 0, 1, 2, 3, 4, 5, 6 };
        Console.WriteLine(foo[1..2] + "" -- "" + foo[^2..^1]);
        Console.WriteLine(foo[..0] + "" -- "" + foo[..^2]);
        Console.WriteLine(foo[1..] + "" -- "" + foo[^3..]);
        Console.WriteLine(foo[..]);
    }
}", options: TestOptions.UnsafeReleaseExe), expectedOutput: @"
[1, 2] -- [5, 6]
[0] -- [0, 1, 2, 3, 4, 5]
[1, 2, 3, 4, 5, 6] -- [4, 5, 6]
[0, 1, 2, 3, 4, 5, 6]");
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
            CompileAndVerify(CreateCompilationWithIndex(@"
using System;
class Test
{
    static void Main()
    {
        byte a = 3;
        Index b = ^a;
        Console.WriteLine($""Value: {b.Value}, FromEnd: {b.FromEnd}"");
    }
}", options: TestOptions.ReleaseExe), expectedOutput: "Value: 3, FromEnd: True");
        }
    }
}
