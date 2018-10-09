// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
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
