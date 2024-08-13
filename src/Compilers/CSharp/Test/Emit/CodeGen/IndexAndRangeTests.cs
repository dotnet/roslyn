// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class IndexAndRangeTests : CSharpTestBase
    {
        private CompilationVerifier CompileAndVerifyWithIndexAndRange(string s, string expectedOutput = null)
        {
            var comp = CreateCompilationWithIndexAndRange(
                new[] { s, TestSources.GetSubArray, },
                expectedOutput is null ? TestOptions.ReleaseDll : TestOptions.ReleaseExe);
            return CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        private static (SemanticModel model, List<ElementAccessExpressionSyntax> accesses) GetModelAndAccesses(CSharpCompilation comp)
        {
            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            return (model, root.DescendantNodes().OfType<ElementAccessExpressionSyntax>().ToList());
        }

        private static void VerifyIndexCall(IMethodSymbol symbol, string methodName, string containingTypeName)
        {
            Assert.NotNull(symbol);
            Assert.Equal(methodName, symbol.Name);
            Assert.Equal(2, symbol.Parameters.Length);
            Assert.Equal(containingTypeName, symbol.ContainingType.Name);
        }

        [Fact]
        public void ExpressionTreePatternIndexAndRange()
        {
            var src = @"
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

struct S
{
    public int Length => 0;
    public S Slice(int start, int length) => default;
}

class Program
{
    static void Main()
    {
        Expression<Func<int[], int>> e = (int[] a) => a[new Index(0, true)]; // 1
        Expression<Func<List<int>, int>> e2 = (List<int> a) => a[new Index(0, true)]; // 2
        
        Expression<Func<int[], int[]>> e3 = (int[] a) => a[new Range(0, 1)]; // 3
        Expression<Func<S, S>> e4 = (S s) => s[new Range(0, 1)]; // 4
    }
}";
            var comp = CreateCompilationWithIndexAndRange(
                new[] { src, TestSources.GetSubArray, });
            comp.VerifyEmitDiagnostics(
                // (16,55): error CS8790: An expression tree may not contain a pattern System.Index or System.Range indexer access
                //         Expression<Func<int[], int>> e = (int[] a) => a[new Index(0, true)]; // 1
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsPatternImplicitIndexer, "a[new Index(0, true)]").WithLocation(16, 55),
                // (17,64): error CS8790: An expression tree may not contain a pattern System.Index or System.Range indexer access
                //         Expression<Func<List<int>, int>> e2 = (List<int> a) => a[new Index(0, true)]; // 2
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsPatternImplicitIndexer, "a[new Index(0, true)]").WithLocation(17, 64),
                // (19,58): error CS8790: An expression tree may not contain a pattern System.Index or System.Range indexer access
                //         Expression<Func<int[], int[]>> e3 = (int[] a) => a[new Range(0, 1)]; // 3
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsPatternImplicitIndexer, "a[new Range(0, 1)]").WithLocation(19, 58),
                // (20,46): error CS8790: An expression tree may not contain a pattern System.Index or System.Range indexer access
                //         Expression<Func<S, S>> e4 = (S s) => s[new Range(0, 1)]; // 4
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsPatternImplicitIndexer, "s[new Range(0, 1)]").WithLocation(20, 46)
            );
        }

        [Fact]
        public void ExpressionTreeFromEndIndexAndRange()
        {
            var src = @"
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

class Program
{
    static void Main()
    {
        Expression<Func<Index>> e = () => ^1;
        Expression<Func<Range>> e2 = () => 1..2;
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyEmitDiagnostics(
                // (10,43): error CS8791: An expression tree may not contain a from-end index ('^') expression.
                //         Expression<Func<Index>> e = () => ^1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsFromEndIndexExpression, "^1").WithLocation(10, 43),
                // (11,44): error CS8792: An expression tree may not contain a range ('..') expression.
                //         Expression<Func<Range>> e2 = () => 1..2;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsRangeExpression, "1..2").WithLocation(11, 44)
            );
        }

        [Fact]
        public void PatternIndexArray()
        {
            var src = @"
class C
{
    static int M1(int[] arr) => arr[^1];
    static char M2(string s) => s[^1];
    static int M3(int[] arr, int i) => arr[^i];
}
";
            var verifier = CompileAndVerifyWithIndexAndRange(src);
            // Code gen for the following two should look basically
            // the same, except that string will use Length/indexer
            // and array will use ldlen/ldelem, and string may have
            // more temporaries
            verifier.VerifyIL("C.M1", @"
{
  // Code size        8 (0x8)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldlen
  IL_0003:  conv.i4
  IL_0004:  ldc.i4.1
  IL_0005:  sub
  IL_0006:  ldelem.i4
  IL_0007:  ret
}");
            verifier.VerifyIL("C.M2", @"
{
  // Code size       15 (0xf)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  callvirt   ""int string.Length.get""
  IL_0007:  ldc.i4.1
  IL_0008:  sub
  IL_0009:  callvirt   ""char string.this[int].get""
  IL_000e:  ret
}
");
            verifier.VerifyIL("C.M3", @"
{
  // Code size        8 (0x8)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldlen
  IL_0003:  conv.i4
  IL_0004:  ldarg.1
  IL_0005:  sub
  IL_0006:  ldelem.i4
  IL_0007:  ret
}
");
        }

        [Fact]
        [WorkItem(37789, "https://github.com/dotnet/roslyn/issues/37789")]
        public void PatternIndexAndRangeCompoundOperatorRefIndexer()
        {
            var src = @"
using System;
class C
{
    static void Main(string[] args)
    {
        var span = new Span<byte>(new byte[2]);
        Console.WriteLine(span[1]);
        span[^1] += 1;
        Console.WriteLine(span[1]);
    }
}
";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(src, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"0
1");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       63 (0x3f)
  .maxstack  3
  .locals init (System.Span<byte> V_0) //span
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.2
  IL_0003:  newarr     ""byte""
  IL_0008:  call       ""System.Span<byte>..ctor(byte[])""
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldc.i4.1
  IL_0010:  call       ""ref byte System.Span<byte>.this[int].get""
  IL_0015:  ldind.u1
  IL_0016:  call       ""void System.Console.WriteLine(int)""
  IL_001b:  ldloca.s   V_0
  IL_001d:  dup
  IL_001e:  call       ""int System.Span<byte>.Length.get""
  IL_0023:  ldc.i4.1
  IL_0024:  sub
  IL_0025:  call       ""ref byte System.Span<byte>.this[int].get""
  IL_002a:  dup
  IL_002b:  ldind.u1
  IL_002c:  ldc.i4.1
  IL_002d:  add
  IL_002e:  conv.u1
  IL_002f:  stind.i1
  IL_0030:  ldloca.s   V_0
  IL_0032:  ldc.i4.1
  IL_0033:  call       ""ref byte System.Span<byte>.this[int].get""
  IL_0038:  ldind.u1
  IL_0039:  call       ""void System.Console.WriteLine(int)""
  IL_003e:  ret
}
");
        }

        [Fact]
        [WorkItem(37789, "https://github.com/dotnet/roslyn/issues/37789")]
        public void PatternIndexCompoundOperator()
        {
            var src = @"
using System;
struct S
{
    private readonly int[] _array;

    private int _counter;

    public S(int[] a)
    {
        _array = a;
        _counter = 0;
    }
    public int Length
    {
        get
        {
            Console.WriteLine(""Length "" + _counter++);
            return _array.Length;
        }
    }
    public int this[int index] 
    {
        get
        {
            Console.WriteLine(""Get "" + _counter++);
            return _array[index];
        }
        set
        {
            Console.WriteLine(""Set "" + _counter++);
            _array[index] = value;
        }
    }
}
class C
{
    static void Main(string[] args)
    {
        var array = new int[2];
        Console.WriteLine(array[1]);
        var s = new S(array);
        s[^1] += 5;
        Console.WriteLine(array[1]);
    }
}
";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(src, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"0
Length 0
Get 1
Set 2
5");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       60 (0x3c)
  .maxstack  4
  .locals init (int[] V_0, //array
                S V_1, //s
                S& V_2,
                int V_3)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  ldelem.i4
  IL_000a:  call       ""void System.Console.WriteLine(int)""
  IL_000f:  ldloca.s   V_1
  IL_0011:  ldloc.0
  IL_0012:  call       ""S..ctor(int[])""
  IL_0017:  ldloca.s   V_1
  IL_0019:  stloc.2
  IL_001a:  ldloc.2
  IL_001b:  call       ""int S.Length.get""
  IL_0020:  ldc.i4.1
  IL_0021:  sub
  IL_0022:  stloc.3
  IL_0023:  ldloc.2
  IL_0024:  ldloc.3
  IL_0025:  ldloc.2
  IL_0026:  ldloc.3
  IL_0027:  call       ""int S.this[int].get""
  IL_002c:  ldc.i4.5
  IL_002d:  add
  IL_002e:  call       ""void S.this[int].set""
  IL_0033:  ldloc.0
  IL_0034:  ldc.i4.1
  IL_0035:  ldelem.i4
  IL_0036:  call       ""void System.Console.WriteLine(int)""
  IL_003b:  ret
}
");
        }

        [Fact]
        public void PatternIndexCompoundOperator_InReadonlyMethod()
        {
            var src = @"
public struct S
{
    public readonly int[] _array;
    public int _counter;

    public S(int[] a)
    {
        _array = a;
        _counter = 0;
    }
    public int Length
    {
        get => throw null;
    }
    public int this[int index]
    {
        get => throw null;
        set => throw null;
    }

    readonly void M()
    {
        this[^1] += 5;
    }
}
";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(src);
            comp.VerifyDiagnostics(
                // (24,9): warning CS8656: Call to non-readonly member 'S.Length.get' from a 'readonly' member results in an implicit copy of 'this'.
                //         this[^1] += 5;
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Length.get", "this").WithLocation(24, 9),
                // (24,9): error CS1604: Cannot assign to 'this[^1]' because it is read-only
                //         this[^1] += 5;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this[^1]").WithArguments("this[^1]").WithLocation(24, 9)
                );
        }

        [Fact]
        [WorkItem(37789, "https://github.com/dotnet/roslyn/issues/37789")]
        public void PatternRangeCompoundOperator()
        {
            var src = @"
using System;
struct S
{
    private readonly int[] _array;

    private int _counter;

    public S(int[] a)
    {
        _array = a;
        _counter = 0;
    }
    public int Length
    {
        get
        {
            Console.WriteLine(""Length "" + _counter++);
            return _array.Length;
        }
    }
    public ref int Slice(int start, int length)
    {
        Console.WriteLine(""Slice "" + _counter++);
        return ref _array[start];
    }
}
class C
{
    static void Main(string[] args)
    {
        var array = new int[2];
        Console.WriteLine(array[1]);
        var s = new S(array);
        s[1..] += 5;
        Console.WriteLine(array[1]);
    }
}
";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(src, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"0
Length 0
Slice 1
5");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       55 (0x37)
      .maxstack  4
      .locals init (int[] V_0, //array
                    S V_1, //s
                    S& V_2)
      IL_0000:  ldc.i4.2
      IL_0001:  newarr     ""int""
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.1
      IL_0009:  ldelem.i4
      IL_000a:  call       ""void System.Console.WriteLine(int)""
      IL_000f:  ldloca.s   V_1
      IL_0011:  ldloc.0
      IL_0012:  call       ""S..ctor(int[])""
      IL_0017:  ldloca.s   V_1
      IL_0019:  stloc.2
      IL_001a:  ldloc.2
      IL_001b:  ldc.i4.1
      IL_001c:  ldloc.2
      IL_001d:  call       ""int S.Length.get""
      IL_0022:  ldc.i4.1
      IL_0023:  sub
      IL_0024:  call       ""ref int S.Slice(int, int)""
      IL_0029:  dup
      IL_002a:  ldind.i4
      IL_002b:  ldc.i4.5
      IL_002c:  add
      IL_002d:  stind.i4
      IL_002e:  ldloc.0
      IL_002f:  ldc.i4.1
      IL_0030:  ldelem.i4
      IL_0031:  call       ""void System.Console.WriteLine(int)""
      IL_0036:  ret
}");
        }

        [Fact]
        [WorkItem(37789, "https://github.com/dotnet/roslyn/issues/37789")]
        public void PatternindexNullableCoalescingAssignmentClass()
        {
            var src = @"
using System;
struct S
{
    private readonly string[] _array;

    private int _counter;

    public S(string[] a)
    {
        _array = a;
        _counter = 0;
    }
    public int Length
    {
        get
        {
            Console.WriteLine(""Length "" + _counter++);
            return _array.Length;
        }
    }
    public string this[int index] 
    {
        get
        {
            Console.WriteLine(""Get "" + _counter++);
            return _array[index];
        }
        set
        {
            Console.WriteLine(""Set "" + _counter++);
            _array[index] = value;
        }
    }
}
class C
{
    static void Main(string[] args)
    {
        var array = new string[2];
        array[0] = ""abc"";
        Console.WriteLine(array[1] is null);
        var s = new S(array);
        s[^1] ??= s[^2];
        s[^1] ??= s[^2];
        Console.WriteLine(s[^1] ??= ""def"");
        Console.WriteLine(array[1]);
    }
}";
            var comp = CreateCompilationWithIndex(src, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
True
Length 0
Get 1
Length 2
Get 3
Set 4
Length 5
Get 6
Length 7
Get 8
abc
abc");
            verifier.VerifyIL("C.Main", @"
{
  // Code size      180 (0xb4)
  .maxstack  5
  .locals init (string[] V_0, //array
                S V_1, //s
                S& V_2,
                int V_3,
                string V_4)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""string""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  ldstr      ""abc""
  IL_000e:  stelem.ref
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  ldelem.ref
  IL_0012:  ldnull
  IL_0013:  ceq
  IL_0015:  call       ""void System.Console.WriteLine(bool)""
  IL_001a:  ldloca.s   V_1
  IL_001c:  ldloc.0
  IL_001d:  call       ""S..ctor(string[])""
  IL_0022:  ldloca.s   V_1
  IL_0024:  stloc.2
  IL_0025:  ldloc.2
  IL_0026:  call       ""int S.Length.get""
  IL_002b:  ldc.i4.1
  IL_002c:  sub
  IL_002d:  stloc.3
  IL_002e:  ldloc.2
  IL_002f:  ldloc.3
  IL_0030:  call       ""string S.this[int].get""
  IL_0035:  brtrue.s   IL_0050
  IL_0037:  ldloc.2
  IL_0038:  ldloc.3
  IL_0039:  ldloca.s   V_1
  IL_003b:  dup
  IL_003c:  call       ""int S.Length.get""
  IL_0041:  ldc.i4.2
  IL_0042:  sub
  IL_0043:  call       ""string S.this[int].get""
  IL_0048:  dup
  IL_0049:  stloc.s    V_4
  IL_004b:  call       ""void S.this[int].set""
  IL_0050:  ldloca.s   V_1
  IL_0052:  stloc.2
  IL_0053:  ldloc.2
  IL_0054:  call       ""int S.Length.get""
  IL_0059:  ldc.i4.1
  IL_005a:  sub
  IL_005b:  stloc.3
  IL_005c:  ldloc.2
  IL_005d:  ldloc.3
  IL_005e:  call       ""string S.this[int].get""
  IL_0063:  brtrue.s   IL_007e
  IL_0065:  ldloc.2
  IL_0066:  ldloc.3
  IL_0067:  ldloca.s   V_1
  IL_0069:  dup
  IL_006a:  call       ""int S.Length.get""
  IL_006f:  ldc.i4.2
  IL_0070:  sub
  IL_0071:  call       ""string S.this[int].get""
  IL_0076:  dup
  IL_0077:  stloc.s    V_4
  IL_0079:  call       ""void S.this[int].set""
  IL_007e:  ldloca.s   V_1
  IL_0080:  stloc.2
  IL_0081:  ldloc.2
  IL_0082:  call       ""int S.Length.get""
  IL_0087:  ldc.i4.1
  IL_0088:  sub
  IL_0089:  stloc.3
  IL_008a:  ldloc.2
  IL_008b:  ldloc.3
  IL_008c:  call       ""string S.this[int].get""
  IL_0091:  dup
  IL_0092:  brtrue.s   IL_00a6
  IL_0094:  pop
  IL_0095:  ldloc.2
  IL_0096:  ldloc.3
  IL_0097:  ldstr      ""def""
  IL_009c:  dup
  IL_009d:  stloc.s    V_4
  IL_009f:  call       ""void S.this[int].set""
  IL_00a4:  ldloc.s    V_4
  IL_00a6:  call       ""void System.Console.WriteLine(string)""
  IL_00ab:  ldloc.0
  IL_00ac:  ldc.i4.1
  IL_00ad:  ldelem.ref
  IL_00ae:  call       ""void System.Console.WriteLine(string)""
  IL_00b3:  ret
}
");
        }

        [Fact]
        [WorkItem(37789, "https://github.com/dotnet/roslyn/issues/37789")]
        public void PatternindexNullableCoalescingAssignmentStruct()
        {
            var src = @"
using System;
struct S
{
    private readonly int?[] _array;

    private int _counter;

    public S(int?[] a)
    {
        _array = a;
        _counter = 0;
    }
    public int Length
    {
        get
        {
            Console.WriteLine(""Length "" + _counter++);
            return _array.Length;
        }
    }
    public int? this[int index] 
    {
        get
        {
            Console.WriteLine(""Get "" + _counter++);
            return _array[index];
        }
        set
        {
            Console.WriteLine(""Set "" + _counter++);
            _array[index] = value;
        }
    }

}
class C
{
    static void Main(string[] args)
    {
        var array = new int?[2];
        array[0] = 1;
        Console.WriteLine(array[1] is null);
        var s = new S(array);
        s[^1] ??= s[^2];
        s[^1] ??= s[^2];
        Console.WriteLine(s[^1] ??= 0);
        Console.WriteLine(array[1]);
    }
}";
            var comp = CreateCompilationWithIndex(src, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
True
Length 0
Get 1
Length 2
Get 3
Set 4
Length 5
Get 6
Length 7
Get 8
1
1");
            verifier.VerifyIL("C.Main", @"
{
  // Code size      256 (0x100)
  .maxstack  5
  .locals init (int?[] V_0, //array
                S V_1, //s
                int? V_2,
                S& V_3,
                int V_4,
                int? V_5,
                int V_6)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""int?""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  newobj     ""int?..ctor(int)""
  IL_000f:  stelem     ""int?""
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.1
  IL_0016:  ldelem     ""int?""
  IL_001b:  stloc.2
  IL_001c:  ldloca.s   V_2
  IL_001e:  call       ""bool int?.HasValue.get""
  IL_0023:  ldc.i4.0
  IL_0024:  ceq
  IL_0026:  call       ""void System.Console.WriteLine(bool)""
  IL_002b:  ldloca.s   V_1
  IL_002d:  ldloc.0
  IL_002e:  call       ""S..ctor(int?[])""
  IL_0033:  ldloca.s   V_1
  IL_0035:  stloc.3
  IL_0036:  ldloc.3
  IL_0037:  call       ""int S.Length.get""
  IL_003c:  ldc.i4.1
  IL_003d:  sub
  IL_003e:  stloc.s    V_4
  IL_0040:  ldloc.3
  IL_0041:  ldloc.s    V_4
  IL_0043:  call       ""int? S.this[int].get""
  IL_0048:  stloc.2
  IL_0049:  ldloca.s   V_2
  IL_004b:  call       ""bool int?.HasValue.get""
  IL_0050:  brtrue.s   IL_006c
  IL_0052:  ldloc.3
  IL_0053:  ldloc.s    V_4
  IL_0055:  ldloca.s   V_1
  IL_0057:  dup
  IL_0058:  call       ""int S.Length.get""
  IL_005d:  ldc.i4.2
  IL_005e:  sub
  IL_005f:  call       ""int? S.this[int].get""
  IL_0064:  dup
  IL_0065:  stloc.s    V_5
  IL_0067:  call       ""void S.this[int].set""
  IL_006c:  ldloca.s   V_1
  IL_006e:  stloc.3
  IL_006f:  ldloc.3
  IL_0070:  call       ""int S.Length.get""
  IL_0075:  ldc.i4.1
  IL_0076:  sub
  IL_0077:  stloc.s    V_4
  IL_0079:  ldloc.3
  IL_007a:  ldloc.s    V_4
  IL_007c:  call       ""int? S.this[int].get""
  IL_0081:  stloc.2
  IL_0082:  ldloca.s   V_2
  IL_0084:  call       ""bool int?.HasValue.get""
  IL_0089:  brtrue.s   IL_00a5
  IL_008b:  ldloc.3
  IL_008c:  ldloc.s    V_4
  IL_008e:  ldloca.s   V_1
  IL_0090:  dup
  IL_0091:  call       ""int S.Length.get""
  IL_0096:  ldc.i4.2
  IL_0097:  sub
  IL_0098:  call       ""int? S.this[int].get""
  IL_009d:  dup
  IL_009e:  stloc.s    V_5
  IL_00a0:  call       ""void S.this[int].set""
  IL_00a5:  ldloca.s   V_1
  IL_00a7:  stloc.3
  IL_00a8:  ldloc.3
  IL_00a9:  call       ""int S.Length.get""
  IL_00ae:  ldc.i4.1
  IL_00af:  sub
  IL_00b0:  stloc.s    V_4
  IL_00b2:  ldloc.3
  IL_00b3:  ldloc.s    V_4
  IL_00b5:  call       ""int? S.this[int].get""
  IL_00ba:  stloc.2
  IL_00bb:  ldloca.s   V_2
  IL_00bd:  call       ""int int?.GetValueOrDefault()""
  IL_00c2:  stloc.s    V_6
  IL_00c4:  ldloca.s   V_2
  IL_00c6:  call       ""bool int?.HasValue.get""
  IL_00cb:  brtrue.s   IL_00e7
  IL_00cd:  ldc.i4.0
  IL_00ce:  stloc.s    V_6
  IL_00d0:  ldloc.3
  IL_00d1:  ldloc.s    V_4
  IL_00d3:  ldloca.s   V_5
  IL_00d5:  ldloc.s    V_6
  IL_00d7:  call       ""int?..ctor(int)""
  IL_00dc:  ldloc.s    V_5
  IL_00de:  call       ""void S.this[int].set""
  IL_00e3:  ldloc.s    V_6
  IL_00e5:  br.s       IL_00e9
  IL_00e7:  ldloc.s    V_6
  IL_00e9:  call       ""void System.Console.WriteLine(int)""
  IL_00ee:  ldloc.0
  IL_00ef:  ldc.i4.1
  IL_00f0:  ldelem     ""int?""
  IL_00f5:  box        ""int?""
  IL_00fa:  call       ""void System.Console.WriteLine(object)""
  IL_00ff:  ret
}
");
        }

        [Fact]
        public void StringAndSpanPatternRangeOpenEnd()
        {
            var src = @"
using System;
class C
{
    public static void Main()
    {
        string s = ""abcd"";
        Console.WriteLine(s[..]);
        ReadOnlySpan<char> span = s;
        foreach (var c in span[..])
        {
            Console.Write(c);
        }
    }
}";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(src, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
abcd
abcd");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       73 (0x49)
  .maxstack  3
  .locals init (System.ReadOnlySpan<char> V_0, //span
                System.ReadOnlySpan<char> V_1,
                int V_2,
                System.ReadOnlySpan<char>& V_3)
  IL_0000:  ldstr      ""abcd""
  IL_0005:  dup
  IL_0006:  ldc.i4.0
  IL_0007:  callvirt   ""string string.Substring(int)""
  IL_000c:  call       ""void System.Console.WriteLine(string)""
  IL_0011:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.op_Implicit(string)""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  stloc.3
  IL_001a:  ldloc.3
  IL_001b:  ldc.i4.0
  IL_001c:  ldloc.3
  IL_001d:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0022:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.Slice(int, int)""
  IL_0027:  stloc.1
  IL_0028:  ldc.i4.0
  IL_0029:  stloc.2
  IL_002a:  br.s       IL_003e
  IL_002c:  ldloca.s   V_1
  IL_002e:  ldloc.2
  IL_002f:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_0034:  ldind.u2
  IL_0035:  call       ""void System.Console.Write(char)""
  IL_003a:  ldloc.2
  IL_003b:  ldc.i4.1
  IL_003c:  add
  IL_003d:  stloc.2
  IL_003e:  ldloc.2
  IL_003f:  ldloca.s   V_1
  IL_0041:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0046:  blt.s      IL_002c
  IL_0048:  ret
}");

            var (model, elementAccesses) = GetModelAndAccesses(comp);

            var info = model.GetSymbolInfo(elementAccesses[0]);
            var substringCall = (IMethodSymbol)info.Symbol;
            info = model.GetSymbolInfo(elementAccesses[1]);
            var sliceCall = (IMethodSymbol)info.Symbol;

            VerifyIndexCall(substringCall, "Substring", "String");
            VerifyIndexCall(sliceCall, "Slice", "ReadOnlySpan");
        }

        [Fact]
        public void SpanTaskReturn()
        {
            var src = @"
using System;
using System.Threading.Tasks;
class C
{
    static void Throws(Action a)
    {
        try
        {
            a();
        }
        catch
        {
            Console.WriteLine(""throws"");
        }
    }

    public static void Main()
    {
        string s = ""abcd"";
        Throws(() => { var span = new Span<char>(s.ToCharArray())[0..10]; });
    }
}";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(src, TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "throws");

            var (model, accesses) = GetModelAndAccesses(comp);
            VerifyIndexCall((IMethodSymbol)model.GetSymbolInfo(accesses[0]).Symbol, "Slice", "Span");
        }

        [Fact]
        public void PatternIndexSetter()
        {
            var src = @"
using System;
struct S
{
    public int F;
    public int Length => 1;
    public int this[int i]
    {
        get => F;
        set { F = value; }
    }
}
class C
{
    static void Main()
    {
        S s = new S();
        s.F = 0;
        Console.WriteLine(s[^1]);
        s[^1] = 2;
        Console.WriteLine(s[^1]);
        Console.WriteLine(s.F);
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"0
2
2");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       84 (0x54)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.0
  IL_000b:  stfld      ""int S.F""
  IL_0010:  ldloca.s   V_0
  IL_0012:  dup
  IL_0013:  call       ""int S.Length.get""
  IL_0018:  ldc.i4.1
  IL_0019:  sub
  IL_001a:  call       ""int S.this[int].get""
  IL_001f:  call       ""void System.Console.WriteLine(int)""
  IL_0024:  ldloca.s   V_0
  IL_0026:  dup
  IL_0027:  call       ""int S.Length.get""
  IL_002c:  ldc.i4.1
  IL_002d:  sub
  IL_002e:  ldc.i4.2
  IL_002f:  call       ""void S.this[int].set""
  IL_0034:  ldloca.s   V_0
  IL_0036:  dup
  IL_0037:  call       ""int S.Length.get""
  IL_003c:  ldc.i4.1
  IL_003d:  sub
  IL_003e:  call       ""int S.this[int].get""
  IL_0043:  call       ""void System.Console.WriteLine(int)""
  IL_0048:  ldloc.0
  IL_0049:  ldfld      ""int S.F""
  IL_004e:  call       ""void System.Console.WriteLine(int)""
  IL_0053:  ret
}
");

            var (model, accesses) = GetModelAndAccesses(comp);

            foreach (var access in accesses)
            {
                var info = model.GetSymbolInfo(access);
                var property = (IPropertySymbol)info.Symbol;

                Assert.NotNull(property);
                Assert.True(property.IsIndexer);
                Assert.Equal(SpecialType.System_Int32, property.Parameters[0].Type.SpecialType);
                Assert.Equal("S", property.ContainingType.Name);
            }
        }

        [Fact]
        public void PatternIndexerRefReturn()
        {
            var comp = CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
class C
{
    static void Main()
    {
        Span<int> s = new int[] { 2, 4, 5, 6 };
        Console.WriteLine(s[^2]);
        ref int x = ref s[^2];
        Console.WriteLine(x);
        s[^2] = 9;
        Console.WriteLine(s[^2]);
        Console.WriteLine(x);
    }
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"5
5
9
9");
            verifier.VerifyIL("C.Main", @"
{
  // Code size      112 (0x70)
  .maxstack  4
  .locals init (System.Span<int> V_0) //s
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.B35A10C764778866E34111165FC69660C6171DF0CB0141E39FA0217EF7A97646""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  call       ""System.Span<int> System.Span<int>.op_Implicit(int[])""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  dup
  IL_001a:  call       ""int System.Span<int>.Length.get""
  IL_001f:  ldc.i4.2
  IL_0020:  sub
  IL_0021:  call       ""ref int System.Span<int>.this[int].get""
  IL_0026:  ldind.i4
  IL_0027:  call       ""void System.Console.WriteLine(int)""
  IL_002c:  ldloca.s   V_0
  IL_002e:  dup
  IL_002f:  call       ""int System.Span<int>.Length.get""
  IL_0034:  ldc.i4.2
  IL_0035:  sub
  IL_0036:  call       ""ref int System.Span<int>.this[int].get""
  IL_003b:  dup
  IL_003c:  ldind.i4
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  ldloca.s   V_0
  IL_0044:  dup
  IL_0045:  call       ""int System.Span<int>.Length.get""
  IL_004a:  ldc.i4.2
  IL_004b:  sub
  IL_004c:  call       ""ref int System.Span<int>.this[int].get""
  IL_0051:  ldc.i4.s   9
  IL_0053:  stind.i4
  IL_0054:  ldloca.s   V_0
  IL_0056:  dup
  IL_0057:  call       ""int System.Span<int>.Length.get""
  IL_005c:  ldc.i4.2
  IL_005d:  sub
  IL_005e:  call       ""ref int System.Span<int>.this[int].get""
  IL_0063:  ldind.i4
  IL_0064:  call       ""void System.Console.WriteLine(int)""
  IL_0069:  ldind.i4
  IL_006a:  call       ""void System.Console.WriteLine(int)""
  IL_006f:  ret
}
");
        }

        [Fact]
        public void PatternIndexAndRangeSpanChar()
        {
            var comp = CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
class C
{
    static void Main()
    {
        ReadOnlySpan<char> s = ""abcdefg"";
        Console.WriteLine(s[^2]);
        var index = ^1;
        Console.WriteLine(s[index]);
        s = s[^2..];
        Console.WriteLine(s[0]);
        Console.WriteLine(s[1]);
    }
}", TestOptions.ReleaseExe); ;
            var verifier = CompileAndVerify(comp, expectedOutput: @"f
g
f
g");
            verifier.VerifyIL(@"C.Main", @"
{
  // Code size      124 (0x7c)
  .maxstack  4
  .locals init (System.ReadOnlySpan<char> V_0, //s
                System.Index V_1, //index
                System.ReadOnlySpan<char>& V_2,
                int V_3,
                int V_4)
  IL_0000:  ldstr      ""abcdefg""
  IL_0005:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.op_Implicit(string)""
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_0
  IL_000d:  dup
  IL_000e:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0013:  ldc.i4.2
  IL_0014:  sub
  IL_0015:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_001a:  ldind.u2
  IL_001b:  call       ""void System.Console.WriteLine(char)""
  IL_0020:  ldloca.s   V_1
  IL_0022:  ldc.i4.1
  IL_0023:  ldc.i4.1
  IL_0024:  call       ""System.Index..ctor(int, bool)""
  IL_0029:  ldloca.s   V_0
  IL_002b:  stloc.2
  IL_002c:  ldloc.2
  IL_002d:  ldloca.s   V_1
  IL_002f:  ldloc.2
  IL_0030:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0035:  call       ""int System.Index.GetOffset(int)""
  IL_003a:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_003f:  ldind.u2
  IL_0040:  call       ""void System.Console.WriteLine(char)""
  IL_0045:  ldloca.s   V_0
  IL_0047:  dup
  IL_0048:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_004d:  stloc.3
  IL_004e:  ldloc.3
  IL_004f:  ldc.i4.2
  IL_0050:  sub
  IL_0051:  stloc.s    V_4
  IL_0053:  ldloc.s    V_4
  IL_0055:  ldloc.3
  IL_0056:  ldloc.s    V_4
  IL_0058:  sub
  IL_0059:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.Slice(int, int)""
  IL_005e:  stloc.0
  IL_005f:  ldloca.s   V_0
  IL_0061:  ldc.i4.0
  IL_0062:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_0067:  ldind.u2
  IL_0068:  call       ""void System.Console.WriteLine(char)""
  IL_006d:  ldloca.s   V_0
  IL_006f:  ldc.i4.1
  IL_0070:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_0075:  ldind.u2
  IL_0076:  call       ""void System.Console.WriteLine(char)""
  IL_007b:  ret
}
");
        }

        [Fact]
        public void PatternIndexAndRangeSpanInt()
        {
            var comp = CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
class C
{
    static void Main()
    {
        Span<int> s = new int[] { 2, 4, 5, 6 };
        Console.WriteLine(s[^2]);
        var index = ^1;
        Console.WriteLine(s[index]);
        s = s[^2..];
        Console.WriteLine(s[0]);
        Console.WriteLine(s[1]);
    }
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"5
6
5
6");
            verifier.VerifyIL("C.Main", @"
{
  // Code size      136 (0x88)
      .maxstack  4
      .locals init (System.Span<int> V_0, //s
                    System.Index V_1, //index
                    System.Span<int>& V_2,
                    int V_3,
                    int V_4)
      IL_0000:  ldc.i4.4
      IL_0001:  newarr     ""int""
      IL_0006:  dup
      IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.B35A10C764778866E34111165FC69660C6171DF0CB0141E39FA0217EF7A97646""
      IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
      IL_0011:  call       ""System.Span<int> System.Span<int>.op_Implicit(int[])""
      IL_0016:  stloc.0
      IL_0017:  ldloca.s   V_0
      IL_0019:  dup
      IL_001a:  call       ""int System.Span<int>.Length.get""
      IL_001f:  ldc.i4.2
      IL_0020:  sub
      IL_0021:  call       ""ref int System.Span<int>.this[int].get""
      IL_0026:  ldind.i4
      IL_0027:  call       ""void System.Console.WriteLine(int)""
      IL_002c:  ldloca.s   V_1
      IL_002e:  ldc.i4.1
      IL_002f:  ldc.i4.1
      IL_0030:  call       ""System.Index..ctor(int, bool)""
      IL_0035:  ldloca.s   V_0
      IL_0037:  stloc.2
      IL_0038:  ldloc.2
      IL_0039:  ldloca.s   V_1
      IL_003b:  ldloc.2
      IL_003c:  call       ""int System.Span<int>.Length.get""
      IL_0041:  call       ""int System.Index.GetOffset(int)""
      IL_0046:  call       ""ref int System.Span<int>.this[int].get""
      IL_004b:  ldind.i4
      IL_004c:  call       ""void System.Console.WriteLine(int)""
      IL_0051:  ldloca.s   V_0
      IL_0053:  dup
      IL_0054:  call       ""int System.Span<int>.Length.get""
      IL_0059:  stloc.3
      IL_005a:  ldloc.3
      IL_005b:  ldc.i4.2
      IL_005c:  sub
      IL_005d:  stloc.s    V_4
      IL_005f:  ldloc.s    V_4
      IL_0061:  ldloc.3
      IL_0062:  ldloc.s    V_4
      IL_0064:  sub
      IL_0065:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
      IL_006a:  stloc.0
      IL_006b:  ldloca.s   V_0
      IL_006d:  ldc.i4.0
      IL_006e:  call       ""ref int System.Span<int>.this[int].get""
      IL_0073:  ldind.i4
      IL_0074:  call       ""void System.Console.WriteLine(int)""
      IL_0079:  ldloca.s   V_0
      IL_007b:  ldc.i4.1
      IL_007c:  call       ""ref int System.Span<int>.this[int].get""
      IL_0081:  ldind.i4
      IL_0082:  call       ""void System.Console.WriteLine(int)""
      IL_0087:  ret
}
");
        }

        [Fact]
        public void RealIndexersPreferredToPattern()
        {
            var src = @"
using System;
class C
{
    public int Length => throw null;
    public int this[Index i, int j = 0] { get { Console.WriteLine(""Index""); return 0; } }
    public int this[int i] { get { Console.WriteLine(""int""); return 0; } }    
    public int Slice(int i, int j) => throw null;
    public int this[Range r, int j = 0] { get { Console.WriteLine(""Range""); return 0; } }

    static void Main()
    {
        var c = new C();
        _ = c[0];
        _ = c[^0];
        _ = c[0..];
    }
}";
            var verifier = CompileAndVerifyWithIndexAndRange(src, expectedOutput: @"
int
Index
Range");
        }

        [Fact]
        public void PatternIndexList()
        {
            var src = @"
using System;
using System.Collections.Generic;
class C
{
    private static List<int> list = new List<int>() { 2, 4, 5, 6 };
    static void Main()
    {
        Console.WriteLine(list[^2]);
        var index = ^1;
        Console.WriteLine(list[index]);
    }
}";
            var verifier = CompileAndVerifyWithIndexAndRange(src, expectedOutput: @"5
6");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       63 (0x3f)
  .maxstack  3
  .locals init (System.Index V_0, //index
                System.Collections.Generic.List<int> V_1)
  IL_0000:  ldsfld     ""System.Collections.Generic.List<int> C.list""
  IL_0005:  dup
  IL_0006:  callvirt   ""int System.Collections.Generic.List<int>.Count.get""
  IL_000b:  ldc.i4.2
  IL_000c:  sub
  IL_000d:  callvirt   ""int System.Collections.Generic.List<int>.this[int].get""
  IL_0012:  call       ""void System.Console.WriteLine(int)""
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldc.i4.1
  IL_001a:  ldc.i4.1
  IL_001b:  call       ""System.Index..ctor(int, bool)""
  IL_0020:  ldsfld     ""System.Collections.Generic.List<int> C.list""
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  ldloca.s   V_0
  IL_0029:  ldloc.1
  IL_002a:  callvirt   ""int System.Collections.Generic.List<int>.Count.get""
  IL_002f:  call       ""int System.Index.GetOffset(int)""
  IL_0034:  callvirt   ""int System.Collections.Generic.List<int>.this[int].get""
  IL_0039:  call       ""void System.Console.WriteLine(int)""
  IL_003e:  ret
}
");
        }

        [Theory]
        [InlineData("Length")]
        [InlineData("Count")]
        public void PatternRangeIndexers(string propertyName)
        {
            var src = @"
using System;
class C
{
    private int[] _f = { 2, 4, 5, 6 };
    public int " + propertyName + @" => _f.Length;
    public int[] Slice(int start, int length) => _f[start..length];
    static void Main()
    {
        var c = new C();
        foreach (var x in c[1..])
        {
            Console.WriteLine(x);
        }
        foreach (var x in c[..^2])
        {
            Console.WriteLine(x);
        }
    }
}";
            var verifier = CompileAndVerifyWithIndexAndRange(src, @"
4
5
2
4");
            verifier.VerifyIL("C.Main", @"
{
    // Code size       87 (0x57)
    .maxstack  4
    .locals init (C V_0, //c
                  int[] V_1,
                  int V_2,
                  C V_3)
    IL_0000:  newobj     ""C..ctor()""
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  stloc.3
    IL_0008:  ldloc.3
    IL_0009:  ldc.i4.1
    IL_000a:  ldloc.3
    IL_000b:  callvirt   ""int C." + propertyName + @".get""
    IL_0010:  ldc.i4.1
    IL_0011:  sub
    IL_0012:  callvirt   ""int[] C.Slice(int, int)""
    IL_0017:  stloc.1
    IL_0018:  ldc.i4.0
    IL_0019:  stloc.2
    IL_001a:  br.s       IL_0028
    IL_001c:  ldloc.1
    IL_001d:  ldloc.2
    IL_001e:  ldelem.i4
    IL_001f:  call       ""void System.Console.WriteLine(int)""
    IL_0024:  ldloc.2
    IL_0025:  ldc.i4.1
    IL_0026:  add
    IL_0027:  stloc.2
    IL_0028:  ldloc.2
    IL_0029:  ldloc.1
    IL_002a:  ldlen
    IL_002b:  conv.i4
    IL_002c:  blt.s      IL_001c
    IL_002e:  ldloc.0
    IL_002f:  stloc.3
    IL_0030:  ldloc.3
    IL_0031:  ldc.i4.0
    IL_0032:  ldloc.3
    IL_0033:  callvirt   ""int C." + propertyName + @".get""
    IL_0038:  ldc.i4.2
    IL_0039:  sub
    IL_003a:  callvirt   ""int[] C.Slice(int, int)""
    IL_003f:  stloc.1
    IL_0040:  ldc.i4.0
    IL_0041:  stloc.2
    IL_0042:  br.s       IL_0050
    IL_0044:  ldloc.1
    IL_0045:  ldloc.2
    IL_0046:  ldelem.i4
    IL_0047:  call       ""void System.Console.WriteLine(int)""
    IL_004c:  ldloc.2
    IL_004d:  ldc.i4.1
    IL_004e:  add
    IL_004f:  stloc.2
    IL_0050:  ldloc.2
    IL_0051:  ldloc.1
    IL_0052:  ldlen
    IL_0053:  conv.i4
    IL_0054:  blt.s      IL_0044
    IL_0056:  ret
}
");
        }

        [Theory]
        [InlineData("Length")]
        [InlineData("Count")]
        public void PatternIndexIndexers(string propertyName)
        {
            var src = @"
using System;
class C
{
    private int[] _f = { 2, 4, 5, 6 };
    public int " + propertyName + @" => _f.Length;
    public int this[int x] => _f[x];
    static void Main()
    {
        var c = new C();
        Console.WriteLine(c[0]);
        Console.WriteLine(c[^1]);
    }
}";
            var verifier = CompileAndVerifyWithIndexAndRange(src, @"
2
6");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       36 (0x24)
  .maxstack  3
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.0
  IL_0007:  callvirt   ""int C.this[int].get""
  IL_000c:  call       ""void System.Console.WriteLine(int)""
  IL_0011:  dup
  IL_0012:  callvirt   ""int C." + propertyName + @".get""
  IL_0017:  ldc.i4.1
  IL_0018:  sub
  IL_0019:  callvirt   ""int C.this[int].get""
  IL_001e:  call       ""void System.Console.WriteLine(int)""
  IL_0023:  ret
}
");
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
        M(x);
    }

    static void M(int[] x)
    {
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
            verifier.VerifyIL("C.M", @"
{
  // Code size       67 (0x43)
  .maxstack  4
  .locals init (int& V_0) //r2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  ldelema    ""int""
  IL_0007:  dup
  IL_0008:  ldind.i4
  IL_0009:  call       ""void System.Console.WriteLine(int)""
  IL_000e:  ldarg.0
  IL_000f:  dup
  IL_0010:  ldlen
  IL_0011:  conv.i4
  IL_0012:  ldc.i4.2
  IL_0013:  sub
  IL_0014:  ldelema    ""int""
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  ldind.i4
  IL_001c:  call       ""void System.Console.WriteLine(int)""
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.7
  IL_0023:  stind.i4
  IL_0024:  dup
  IL_0025:  ldind.i4
  IL_0026:  call       ""void System.Console.WriteLine(int)""
  IL_002b:  ldloc.0
  IL_002c:  ldind.i4
  IL_002d:  call       ""void System.Console.WriteLine(int)""
  IL_0032:  dup
  IL_0033:  ldc.i4.5
  IL_0034:  stind.i4
  IL_0035:  ldind.i4
  IL_0036:  call       ""void System.Console.WriteLine(int)""
  IL_003b:  ldloc.0
  IL_003c:  ldind.i4
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  ret
}");
        }

        [Fact]
        public void RangeIndexerStringIsFromEndStart()
        {
            CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        string s = ""abcdef"";
        Console.WriteLine(s[^2..]);
    }
}", expectedOutput: "ef");
        }

        [Fact]
        public void FakeRangeIndexerStringBothIsFromEnd()
        {
            CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        string s = ""abcdef"";
        Console.WriteLine(s[^4..^1]);
    }
}", expectedOutput: "cde");
        }

        [Fact]
        public void IndexIndexerStringTwoArgs()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
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
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new Index(1, false)").WithArguments("System.Index", "int").WithLocation(12, 29),
                // (12,27): error CS0029: Cannot implicitly convert type 'System.Index' to 'int'
                //         Console.WriteLine(s[new Index(1, false), ^1]);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "^1").WithArguments("System.Index", "int").WithLocation(12, 50));
        }

        [Fact]
        public void FakeIndexIndexerString()
        {
            var verifier = CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        int i = 1;
        Console.WriteLine(s[new Index(i, false)]);
        Console.WriteLine(s[(Index)2]);
        Console.WriteLine(s[^1]);
        Console.WriteLine(s[new Index(3, false)]);
        Console.WriteLine(s[new Index(2, true)]);
        i = 6;
        Console.WriteLine(s[new Index(i, true)]);
        Console.WriteLine(s[new Index(value: 2, fromEnd: false)]);
        Console.WriteLine(s[new Index(fromEnd: true, value: 3)]);
        Console.WriteLine(s[new Index(3, false) {}]);
    }
}", expectedOutput: @"b
c
f
d
e
a
c
d
d");
            verifier.VerifyIL("C.Main", @"
{
  // Code size      219 (0xdb)
  .maxstack  4
  .locals init (int V_0, //i
                string V_1,
                System.Index V_2)
  IL_0000:  ldstr      ""abcdef""
  IL_0005:  ldc.i4.1
  IL_0006:  stloc.0
  IL_0007:  dup
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  newobj     ""System.Index..ctor(int, bool)""
  IL_0011:  stloc.2
  IL_0012:  ldloca.s   V_2
  IL_0014:  ldloc.1
  IL_0015:  callvirt   ""int string.Length.get""
  IL_001a:  call       ""int System.Index.GetOffset(int)""
  IL_001f:  callvirt   ""char string.this[int].get""
  IL_0024:  call       ""void System.Console.WriteLine(char)""
  IL_0029:  dup
  IL_002a:  ldc.i4.2
  IL_002b:  callvirt   ""char string.this[int].get""
  IL_0030:  call       ""void System.Console.WriteLine(char)""
  IL_0035:  dup
  IL_0036:  dup
  IL_0037:  callvirt   ""int string.Length.get""
  IL_003c:  ldc.i4.1
  IL_003d:  sub
  IL_003e:  callvirt   ""char string.this[int].get""
  IL_0043:  call       ""void System.Console.WriteLine(char)""
  IL_0048:  dup
  IL_0049:  ldc.i4.3
  IL_004a:  callvirt   ""char string.this[int].get""
  IL_004f:  call       ""void System.Console.WriteLine(char)""
  IL_0054:  dup
  IL_0055:  dup
  IL_0056:  callvirt   ""int string.Length.get""
  IL_005b:  ldc.i4.2
  IL_005c:  sub
  IL_005d:  callvirt   ""char string.this[int].get""
  IL_0062:  call       ""void System.Console.WriteLine(char)""
  IL_0067:  ldc.i4.6
  IL_0068:  stloc.0
  IL_0069:  dup
  IL_006a:  stloc.1
  IL_006b:  ldloc.1
  IL_006c:  ldloc.0
  IL_006d:  ldc.i4.1
  IL_006e:  newobj     ""System.Index..ctor(int, bool)""
  IL_0073:  stloc.2
  IL_0074:  ldloca.s   V_2
  IL_0076:  ldloc.1
  IL_0077:  callvirt   ""int string.Length.get""
  IL_007c:  call       ""int System.Index.GetOffset(int)""
  IL_0081:  callvirt   ""char string.this[int].get""
  IL_0086:  call       ""void System.Console.WriteLine(char)""
  IL_008b:  dup
  IL_008c:  ldc.i4.2
  IL_008d:  callvirt   ""char string.this[int].get""
  IL_0092:  call       ""void System.Console.WriteLine(char)""
  IL_0097:  dup
  IL_0098:  stloc.1
  IL_0099:  ldloc.1
  IL_009a:  ldc.i4.3
  IL_009b:  ldc.i4.1
  IL_009c:  newobj     ""System.Index..ctor(int, bool)""
  IL_00a1:  stloc.2
  IL_00a2:  ldloca.s   V_2
  IL_00a4:  ldloc.1
  IL_00a5:  callvirt   ""int string.Length.get""
  IL_00aa:  call       ""int System.Index.GetOffset(int)""
  IL_00af:  callvirt   ""char string.this[int].get""
  IL_00b4:  call       ""void System.Console.WriteLine(char)""
  IL_00b9:  stloc.1
  IL_00ba:  ldloc.1
  IL_00bb:  ldc.i4.3
  IL_00bc:  ldc.i4.0
  IL_00bd:  newobj     ""System.Index..ctor(int, bool)""
  IL_00c2:  stloc.2
  IL_00c3:  ldloca.s   V_2
  IL_00c5:  ldloc.1
  IL_00c6:  callvirt   ""int string.Length.get""
  IL_00cb:  call       ""int System.Index.GetOffset(int)""
  IL_00d0:  callvirt   ""char string.this[int].get""
  IL_00d5:  call       ""void System.Console.WriteLine(char)""
  IL_00da:  ret
}
");
        }

        [Fact]
        public void FakeRangeIndexerString()
        {
            var verifier = CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        Console.WriteLine(s[1..3]);
    }
}", expectedOutput: "bc");
            verifier.VerifyIL("C.Main", @"
{
    // Code size       18 (0x12)
    .maxstack  3
    IL_0000:  ldstr      ""abcdef""
    IL_0005:  ldc.i4.1
    IL_0006:  ldc.i4.2
    IL_0007:  callvirt   ""string string.Substring(int, int)""
    IL_000c:  call       ""void System.Console.WriteLine(string)""
    IL_0011:  ret
}");
        }

        [Fact, WorkItem(40776, "https://github.com/dotnet/roslyn/issues/40776")]
        public void FakeIndexIndexerOnDefaultStruct()
        {
            var verifier = CompileAndVerifyWithIndexAndRange(@"
using System;

struct NotASpan
{
    public int Length => 1;

    public int this[int index] => 0;
}

class C
{
    static int Repro() => default(NotASpan)[^0];

    static void Main() => Repro();
}");

            verifier.VerifyIL("C.Repro", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (NotASpan V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  dup
  IL_0003:  initobj    ""NotASpan""
  IL_0009:  dup
  IL_000a:  call       ""int NotASpan.Length.get""
  IL_000f:  call       ""int NotASpan.this[int].get""
  IL_0014:  ret
}
");
        }

        [Fact, WorkItem(40776, "https://github.com/dotnet/roslyn/issues/40776")]
        public void FakeIndexIndexerOnStructConstructor()
        {
            var comp = CreateCompilationWithIndexAndRangeAndSpan(@"
using System;

class C
{
    static byte Repro() => new Span<byte>(new byte[] { })[^1];
}");

            var verifier = CompileAndVerify(comp);

            verifier.VerifyIL("C.Repro", @"
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (System.Span<byte> V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""byte""
  IL_0006:  newobj     ""System.Span<byte>..ctor(byte[])""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  dup
  IL_000f:  call       ""int System.Span<byte>.Length.get""
  IL_0014:  ldc.i4.1
  IL_0015:  sub
  IL_0016:  call       ""ref byte System.Span<byte>.this[int].get""
  IL_001b:  ldind.u1
  IL_001c:  ret
}
");
        }

        [Fact]
        public void FakeRangeIndexerStringOpenEnd()
        {
            CompileAndVerifyWithIndexAndRange(@"
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
}", expectedOutput: "bcdef");
        }

        [Fact]
        public void FakeRangeIndexerStringOpenStart()
        {
            CompileAndVerifyWithIndexAndRange(@"
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
}", expectedOutput: "abcd");
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
        bool fromEnd = false;
        Console.WriteLine(array[new Index(1, fromEnd)]);
        Console.WriteLine(array[^1]);
    }
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"2
11");
            verifier.VerifyIL("C.M", @"
{
  // Code size       42 (0x2a)
  .maxstack  3
  .locals init (bool V_0, //fromEnd
                int[] V_1,
                System.Index V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  stloc.1
  IL_0004:  ldloc.1
  IL_0005:  ldc.i4.1
  IL_0006:  ldloc.0
  IL_0007:  newobj     ""System.Index..ctor(int, bool)""
  IL_000c:  stloc.2
  IL_000d:  ldloca.s   V_2
  IL_000f:  ldloc.1
  IL_0010:  ldlen
  IL_0011:  conv.i4
  IL_0012:  call       ""int System.Index.GetOffset(int)""
  IL_0017:  ldelem.i4
  IL_0018:  call       ""void System.Console.WriteLine(int)""
  IL_001d:  ldarg.0
  IL_001e:  dup
  IL_001f:  ldlen
  IL_0020:  conv.i4
  IL_0021:  ldc.i4.1
  IL_0022:  sub
  IL_0023:  ldelem.i4
  IL_0024:  call       ""void System.Console.WriteLine(int)""
  IL_0029:  ret
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
            var comp = CreateCompilationWithIndex(source, WithNullableEnable(TestOptions.DebugExe));
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "211");
        }

        [Fact]
        public void FakeRangeIndexerArray()
        {
            var verifier = CompileAndVerifyWithIndexAndRange(@"
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
}", expectedOutput: @"2
2
3");
            verifier.VerifyIL("C.M", @"
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0007:  ldc.i4.3
  IL_0008:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_000d:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0012:  call       ""int[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<int>(int[], System.Range)""
  IL_0017:  ret
}
");
        }

        [Fact]
        public void FakeRangeStartIsFromEndIndexerArray()
        {
            CompileAndVerifyWithIndexAndRange(@"
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
}", expectedOutput: @"2
3
11");
        }

        [Fact]
        public void FakeRangeBothIsFromEndIndexerArray()
        {
            CompileAndVerifyWithIndexAndRange(@"
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
}", expectedOutput: @"2
2
3");
        }

        [Fact]
        public void FakeRangeToEndIndexerArray()
        {
            var verifier = CompileAndVerifyWithIndexAndRange(@"
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
}", expectedOutput: @"3
2
3
11");
            verifier.VerifyIL("C.M", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0007:  call       ""System.Range System.Range.StartAt(System.Index)""
  IL_000c:  call       ""int[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<int>(int[], System.Range)""
  IL_0011:  ret
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
}" + TestSources.GetSubArray, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, verify: Verification.Passes, expectedOutput: @"3
1
2
3");
            verifier.VerifyIL("C.M", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.3
  IL_0002:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0007:  call       ""System.Range System.Range.EndAt(System.Index)""
  IL_000c:  call       ""int[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<int>(int[], System.Range)""
  IL_0011:  ret
}");
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
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Range System.Range.EndAt(System.Index)""
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
  IL_001c:  call       ""System.Range System.Range.EndAt(System.Index)""
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
  IL_0001:  call       ""System.Range System.Range.StartAt(System.Index)""
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
  IL_001c:  call       ""System.Range System.Range.StartAt(System.Index)""
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
  IL_0000:  call       ""System.Range System.Range.All.get""
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
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Range System.Range.StartAt(System.Index)""
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
  IL_0001:  call       ""System.Range System.Range.EndAt(System.Index)""
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
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (System.Index V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.Index""
  IL_0008:  ldloc.0
  IL_0009:  call       ""System.Range System.Range.StartAt(System.Index)""
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
  IL_0009:  call       ""System.Range System.Range.EndAt(System.Index)""
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

        [Fact]
        [WorkItem(54085, "https://github.com/dotnet/roslyn/issues/54085")]
        public void OrderOfEvaluation_01()
        {
            var comp = CreateCompilationWithIndexAndRangeAndSpan(@"
using System;

class CollectionX
{
    private int[] _array = new[] { 1, 2, 3 };

    public int Length
    {
        get
        {
            Console.WriteLine(""Length"");
            return _array.Length;
        }
    }

    public ref int this[int index]
    {
        get
        {
            Console.WriteLine(""Indexer get"");
            return ref _array[index];
        }
    }
}

static class SideEffect
{
    static CollectionX Get()
    {
        Console.WriteLine(""Get"");
        return new CollectionX();
    }
    static int GetIdx()
    {
        Console.WriteLine(""GetIdx"");
        return 1;
    }

    public static void Main()
    {
        int i2 =  Get()[Index.FromEnd(GetIdx())];
        Console.WriteLine(i2);
    }
}
", TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"Get
GetIdx
Length
Indexer get
3
");
        }

        [Fact]
        [WorkItem(54085, "https://github.com/dotnet/roslyn/issues/54085")]
        public void OrderOfEvaluation_02()
        {
            var comp = CreateCompilationWithIndexAndRangeAndSpan(@"
using System;

class CollectionX
{
    private int[] _array = new[] { 1, 2, 3 };

    public int Length
    {
        get
        {
            Console.WriteLine(""Length"");
            return _array.Length;
        }
    }

    public ref int this[int index]
    {
        get
        {
            Console.WriteLine(""Indexer get"");
            return ref _array[index];
        }
    }
}

static class SideEffect
{
    static CollectionX Get()
    {
        Console.WriteLine(""Get"");
        return new CollectionX();
    }
    static int GetIdx()
    {
        Console.WriteLine(""GetIdx"");
        return 1;
    }

    public static void Main()
    {
        int i2 =  Get()[^GetIdx()];
        Console.WriteLine(i2);
        Console.WriteLine();
        i2 =  Get()[GetIdx()];
        Console.WriteLine(i2);
        Console.WriteLine();
        i2 =  Get()[(Index)GetIdx()];
        Console.WriteLine(i2);
    }
}
", TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"Get
GetIdx
Length
Indexer get
3

Get
GetIdx
Indexer get
2

Get
GetIdx
Indexer get
2
");
        }

        [Fact]
        [WorkItem(57349, "https://github.com/dotnet/roslyn/issues/57349")]
        public void OrderOfEvaluation_03()
        {
            var comp = CreateCompilationWithIndexAndRangeAndSpan(@"
using System;

class CollectionX
{

    public int Length
    {
        get
        {
            Console.Write("" Length"");
            return 4;
        }
    }

    public int Slice(int x, int y)
    {
        Console.Write("" Slice {0}, {1}"", x, y);
        return 111;
    }
}

static class SideEffect
{
    static CollectionX Get()
    {
        Console.WriteLine();
        Console.Write(""Get"");
        return new CollectionX();
    }
    static Range GetIdx()
    {
        Console.Write("" GetIdx"");
        return 1..3;
    }
    static int GetIdx1(int x)
    {
        Console.Write("" GetIdx1"");
        return x;
    }
    static int GetIdx2(int x)
    {
        Console.Write("" GetIdx2"");
        return x;
    }
    static Index GetIdx3(Index x)
    {
        Console.Write("" GetIdx3"");
        return x;
    }
    static Index GetIdx4(Index x)
    {
        Console.Write("" GetIdx4"");
        return x;
    }

    public static void Main()
    {
        _ = Get()[GetIdx()];
        _ = Get()[GetIdx1(1)..GetIdx2(3)];
        _ = Get()[^GetIdx1(3)..GetIdx2(3)];
        _ = Get()[GetIdx1(1)..^GetIdx2(1)];
        _ = Get()[^GetIdx1(3)..^GetIdx2(1)];
        _ = Get()[GetIdx3(1)..GetIdx4(3)];
        _ = Get()[^GetIdx1(3)..];
        _ = Get()[GetIdx1(1)..];
        _ = Get()[..GetIdx2(3)];
        _ = Get()[..^GetIdx2(1)];
        _ = Get()[..];
        _ = Get()[GetIdx3(1)..];
        _ = Get()[..GetIdx4(3)];
        _ = Get()[^GetIdx1(3)..GetIdx4(3)];
        _ = Get()[GetIdx1(1)..GetIdx4(3)];
        _ = Get()[GetIdx3(1)..GetIdx2(3)];
        _ = Get()[GetIdx3(1)..^GetIdx2(1)];
    }
}
", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"
Get GetIdx Length Slice 1, 2
Get GetIdx1 GetIdx2 Slice 1, 2
Get GetIdx1 GetIdx2 Length Slice 1, 2
Get GetIdx1 GetIdx2 Length Slice 1, 2
Get GetIdx1 GetIdx2 Length Slice 1, 2
Get GetIdx3 GetIdx4 Length Slice 1, 2
Get GetIdx1 Length Slice 1, 3
Get GetIdx1 Length Slice 1, 3
Get GetIdx2 Slice 0, 3
Get GetIdx2 Length Slice 0, 3
Get Length Slice 0, 4
Get GetIdx3 Length Slice 1, 3
Get GetIdx4 Length Slice 0, 3
Get GetIdx1 GetIdx4 Length Slice 1, 2
Get GetIdx1 GetIdx4 Length Slice 1, 2
Get GetIdx3 GetIdx2 Length Slice 1, 2
Get GetIdx3 GetIdx2 Length Slice 1, 2
");
            verifier.VerifyMethodBody("SideEffect.Main", @"
{
  // Code size      731 (0x2db)
  .maxstack  4
  .locals init (System.Range V_0,
                int V_1,
                int V_2,
                int V_3,
                System.Index V_4,
                CollectionX V_5,
                int V_6,
                System.Index V_7)
  // sequence point: _ = Get()[GetIdx()];
  IL_0000:  call       ""CollectionX SideEffect.Get()""
  IL_0005:  call       ""System.Range SideEffect.GetIdx()""
  IL_000a:  stloc.0
  IL_000b:  dup
  IL_000c:  callvirt   ""int CollectionX.Length.get""
  IL_0011:  stloc.1
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""System.Index System.Range.Start.get""
  IL_0019:  stloc.s    V_4
  IL_001b:  ldloca.s   V_4
  IL_001d:  ldloc.1
  IL_001e:  call       ""int System.Index.GetOffset(int)""
  IL_0023:  stloc.2
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       ""System.Index System.Range.End.get""
  IL_002b:  stloc.s    V_4
  IL_002d:  ldloca.s   V_4
  IL_002f:  ldloc.1
  IL_0030:  call       ""int System.Index.GetOffset(int)""
  IL_0035:  ldloc.2
  IL_0036:  sub
  IL_0037:  stloc.3
  IL_0038:  ldloc.2
  IL_0039:  ldloc.3
  IL_003a:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_003f:  pop
  // sequence point: _ = Get()[GetIdx1(1)..GetIdx2(3)];
  IL_0040:  call       ""CollectionX SideEffect.Get()""
  IL_0045:  ldc.i4.1
  IL_0046:  call       ""int SideEffect.GetIdx1(int)""
  IL_004b:  stloc.3
  IL_004c:  ldloc.3
  IL_004d:  ldc.i4.3
  IL_004e:  call       ""int SideEffect.GetIdx2(int)""
  IL_0053:  ldloc.3
  IL_0054:  sub
  IL_0055:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_005a:  pop
  // sequence point: _ = Get()[^GetIdx1(3)..GetIdx2(3)];
  IL_005b:  call       ""CollectionX SideEffect.Get()""
  IL_0060:  ldc.i4.3
  IL_0061:  call       ""int SideEffect.GetIdx1(int)""
  IL_0066:  stloc.3
  IL_0067:  ldc.i4.3
  IL_0068:  call       ""int SideEffect.GetIdx2(int)""
  IL_006d:  stloc.2
  IL_006e:  dup
  IL_006f:  callvirt   ""int CollectionX.Length.get""
  IL_0074:  ldloc.3
  IL_0075:  sub
  IL_0076:  stloc.1
  IL_0077:  ldloc.1
  IL_0078:  ldloc.2
  IL_0079:  ldloc.1
  IL_007a:  sub
  IL_007b:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_0080:  pop
  // sequence point: _ = Get()[GetIdx1(1)..^GetIdx2(1)];
  IL_0081:  call       ""CollectionX SideEffect.Get()""
  IL_0086:  stloc.s    V_5
  IL_0088:  ldc.i4.1
  IL_0089:  call       ""int SideEffect.GetIdx1(int)""
  IL_008e:  stloc.1
  IL_008f:  ldc.i4.1
  IL_0090:  call       ""int SideEffect.GetIdx2(int)""
  IL_0095:  stloc.2
  IL_0096:  ldloc.s    V_5
  IL_0098:  ldloc.1
  IL_0099:  ldloc.s    V_5
  IL_009b:  callvirt   ""int CollectionX.Length.get""
  IL_00a0:  ldloc.2
  IL_00a1:  sub
  IL_00a2:  ldloc.1
  IL_00a3:  sub
  IL_00a4:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_00a9:  pop
  // sequence point: _ = Get()[^GetIdx1(3)..^GetIdx2(1)];
  IL_00aa:  call       ""CollectionX SideEffect.Get()""
  IL_00af:  ldc.i4.3
  IL_00b0:  call       ""int SideEffect.GetIdx1(int)""
  IL_00b5:  stloc.2
  IL_00b6:  ldc.i4.1
  IL_00b7:  call       ""int SideEffect.GetIdx2(int)""
  IL_00bc:  stloc.1
  IL_00bd:  dup
  IL_00be:  callvirt   ""int CollectionX.Length.get""
  IL_00c3:  stloc.3
  IL_00c4:  ldloc.3
  IL_00c5:  ldloc.2
  IL_00c6:  sub
  IL_00c7:  stloc.s    V_6
  IL_00c9:  ldloc.s    V_6
  IL_00cb:  ldloc.3
  IL_00cc:  ldloc.1
  IL_00cd:  sub
  IL_00ce:  ldloc.s    V_6
  IL_00d0:  sub
  IL_00d1:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_00d6:  pop
  // sequence point: _ = Get()[GetIdx3(1)..GetIdx4(3)];
  IL_00d7:  call       ""CollectionX SideEffect.Get()""
  IL_00dc:  ldc.i4.1
  IL_00dd:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_00e2:  call       ""System.Index SideEffect.GetIdx3(System.Index)""
  IL_00e7:  stloc.s    V_4
  IL_00e9:  ldc.i4.3
  IL_00ea:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_00ef:  call       ""System.Index SideEffect.GetIdx4(System.Index)""
  IL_00f4:  stloc.s    V_7
  IL_00f6:  dup
  IL_00f7:  callvirt   ""int CollectionX.Length.get""
  IL_00fc:  stloc.s    V_6
  IL_00fe:  ldloca.s   V_4
  IL_0100:  ldloc.s    V_6
  IL_0102:  call       ""int System.Index.GetOffset(int)""
  IL_0107:  stloc.3
  IL_0108:  ldloc.3
  IL_0109:  ldloca.s   V_7
  IL_010b:  ldloc.s    V_6
  IL_010d:  call       ""int System.Index.GetOffset(int)""
  IL_0112:  ldloc.3
  IL_0113:  sub
  IL_0114:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_0119:  pop
  // sequence point: _ = Get()[^GetIdx1(3)..];
  IL_011a:  call       ""CollectionX SideEffect.Get()""
  IL_011f:  ldc.i4.3
  IL_0120:  call       ""int SideEffect.GetIdx1(int)""
  IL_0125:  stloc.3
  IL_0126:  dup
  IL_0127:  callvirt   ""int CollectionX.Length.get""
  IL_012c:  stloc.s    V_6
  IL_012e:  ldloc.s    V_6
  IL_0130:  ldloc.3
  IL_0131:  sub
  IL_0132:  stloc.1
  IL_0133:  ldloc.1
  IL_0134:  ldloc.s    V_6
  IL_0136:  ldloc.1
  IL_0137:  sub
  IL_0138:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_013d:  pop
  // sequence point: _ = Get()[GetIdx1(1)..];
  IL_013e:  call       ""CollectionX SideEffect.Get()""
  IL_0143:  stloc.s    V_5
  IL_0145:  ldc.i4.1
  IL_0146:  call       ""int SideEffect.GetIdx1(int)""
  IL_014b:  stloc.1
  IL_014c:  ldloc.s    V_5
  IL_014e:  ldloc.1
  IL_014f:  ldloc.s    V_5
  IL_0151:  callvirt   ""int CollectionX.Length.get""
  IL_0156:  ldloc.1
  IL_0157:  sub
  IL_0158:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_015d:  pop
  // sequence point: _ = Get()[..GetIdx2(3)];
  IL_015e:  call       ""CollectionX SideEffect.Get()""
  IL_0163:  ldc.i4.0
  IL_0164:  ldc.i4.3
  IL_0165:  call       ""int SideEffect.GetIdx2(int)""
  IL_016a:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_016f:  pop
  // sequence point: _ = Get()[..^GetIdx2(1)];
  IL_0170:  call       ""CollectionX SideEffect.Get()""
  IL_0175:  stloc.s    V_5
  IL_0177:  ldc.i4.1
  IL_0178:  call       ""int SideEffect.GetIdx2(int)""
  IL_017d:  stloc.1
  IL_017e:  ldloc.s    V_5
  IL_0180:  ldc.i4.0
  IL_0181:  ldloc.s    V_5
  IL_0183:  callvirt   ""int CollectionX.Length.get""
  IL_0188:  ldloc.1
  IL_0189:  sub
  IL_018a:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_018f:  pop
  // sequence point: _ = Get()[..];
  IL_0190:  call       ""CollectionX SideEffect.Get()""
  IL_0195:  stloc.s    V_5
  IL_0197:  ldloc.s    V_5
  IL_0199:  ldc.i4.0
  IL_019a:  ldloc.s    V_5
  IL_019c:  callvirt   ""int CollectionX.Length.get""
  IL_01a1:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_01a6:  pop
  // sequence point: _ = Get()[GetIdx3(1)..];
  IL_01a7:  call       ""CollectionX SideEffect.Get()""
  IL_01ac:  ldc.i4.1
  IL_01ad:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_01b2:  call       ""System.Index SideEffect.GetIdx3(System.Index)""
  IL_01b7:  stloc.s    V_7
  IL_01b9:  dup
  IL_01ba:  callvirt   ""int CollectionX.Length.get""
  IL_01bf:  stloc.1
  IL_01c0:  ldloca.s   V_7
  IL_01c2:  ldloc.1
  IL_01c3:  call       ""int System.Index.GetOffset(int)""
  IL_01c8:  stloc.s    V_6
  IL_01ca:  ldloc.s    V_6
  IL_01cc:  ldloc.1
  IL_01cd:  ldloc.s    V_6
  IL_01cf:  sub
  IL_01d0:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_01d5:  pop
  // sequence point: _ = Get()[..GetIdx4(3)];
  IL_01d6:  call       ""CollectionX SideEffect.Get()""
  IL_01db:  stloc.s    V_5
  IL_01dd:  ldloc.s    V_5
  IL_01df:  ldc.i4.0
  IL_01e0:  ldc.i4.3
  IL_01e1:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_01e6:  call       ""System.Index SideEffect.GetIdx4(System.Index)""
  IL_01eb:  stloc.s    V_7
  IL_01ed:  ldloca.s   V_7
  IL_01ef:  ldloc.s    V_5
  IL_01f1:  callvirt   ""int CollectionX.Length.get""
  IL_01f6:  call       ""int System.Index.GetOffset(int)""
  IL_01fb:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_0200:  pop
  // sequence point: _ = Get()[^GetIdx1(3)..GetIdx4(3)];
  IL_0201:  call       ""CollectionX SideEffect.Get()""
  IL_0206:  ldc.i4.3
  IL_0207:  call       ""int SideEffect.GetIdx1(int)""
  IL_020c:  stloc.s    V_6
  IL_020e:  ldc.i4.3
  IL_020f:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0214:  call       ""System.Index SideEffect.GetIdx4(System.Index)""
  IL_0219:  stloc.s    V_7
  IL_021b:  dup
  IL_021c:  callvirt   ""int CollectionX.Length.get""
  IL_0221:  stloc.1
  IL_0222:  ldloc.1
  IL_0223:  ldloc.s    V_6
  IL_0225:  sub
  IL_0226:  stloc.3
  IL_0227:  ldloc.3
  IL_0228:  ldloca.s   V_7
  IL_022a:  ldloc.1
  IL_022b:  call       ""int System.Index.GetOffset(int)""
  IL_0230:  ldloc.3
  IL_0231:  sub
  IL_0232:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_0237:  pop
  // sequence point: _ = Get()[GetIdx1(1)..GetIdx4(3)];
  IL_0238:  call       ""CollectionX SideEffect.Get()""
  IL_023d:  stloc.s    V_5
  IL_023f:  ldc.i4.1
  IL_0240:  call       ""int SideEffect.GetIdx1(int)""
  IL_0245:  stloc.3
  IL_0246:  ldloc.s    V_5
  IL_0248:  ldloc.3
  IL_0249:  ldc.i4.3
  IL_024a:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_024f:  call       ""System.Index SideEffect.GetIdx4(System.Index)""
  IL_0254:  stloc.s    V_7
  IL_0256:  ldloca.s   V_7
  IL_0258:  ldloc.s    V_5
  IL_025a:  callvirt   ""int CollectionX.Length.get""
  IL_025f:  call       ""int System.Index.GetOffset(int)""
  IL_0264:  ldloc.3
  IL_0265:  sub
  IL_0266:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_026b:  pop
  // sequence point: _ = Get()[GetIdx3(1)..GetIdx2(3)];
  IL_026c:  call       ""CollectionX SideEffect.Get()""
  IL_0271:  stloc.s    V_5
  IL_0273:  ldc.i4.1
  IL_0274:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0279:  call       ""System.Index SideEffect.GetIdx3(System.Index)""
  IL_027e:  stloc.s    V_7
  IL_0280:  ldc.i4.3
  IL_0281:  call       ""int SideEffect.GetIdx2(int)""
  IL_0286:  stloc.3
  IL_0287:  ldloca.s   V_7
  IL_0289:  ldloc.s    V_5
  IL_028b:  callvirt   ""int CollectionX.Length.get""
  IL_0290:  call       ""int System.Index.GetOffset(int)""
  IL_0295:  stloc.1
  IL_0296:  ldloc.s    V_5
  IL_0298:  ldloc.1
  IL_0299:  ldloc.3
  IL_029a:  ldloc.1
  IL_029b:  sub
  IL_029c:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_02a1:  pop
  // sequence point: _ = Get()[GetIdx3(1)..^GetIdx2(1)];
  IL_02a2:  call       ""CollectionX SideEffect.Get()""
  IL_02a7:  ldc.i4.1
  IL_02a8:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_02ad:  call       ""System.Index SideEffect.GetIdx3(System.Index)""
  IL_02b2:  stloc.s    V_7
  IL_02b4:  ldc.i4.1
  IL_02b5:  call       ""int SideEffect.GetIdx2(int)""
  IL_02ba:  stloc.1
  IL_02bb:  dup
  IL_02bc:  callvirt   ""int CollectionX.Length.get""
  IL_02c1:  stloc.3
  IL_02c2:  ldloca.s   V_7
  IL_02c4:  ldloc.3
  IL_02c5:  call       ""int System.Index.GetOffset(int)""
  IL_02ca:  stloc.s    V_6
  IL_02cc:  ldloc.s    V_6
  IL_02ce:  ldloc.3
  IL_02cf:  ldloc.1
  IL_02d0:  sub
  IL_02d1:  ldloc.s    V_6
  IL_02d3:  sub
  IL_02d4:  callvirt   ""int CollectionX.Slice(int, int)""
  IL_02d9:  pop
  // sequence point: }
  IL_02da:  ret
}
");
        }

        [Fact, WorkItem(57745, "https://github.com/dotnet/roslyn/issues/57745")]
        public void ObsoleteRangeType()
        {
            var source = @"
_ = new C()[..];

class C
{
    public int Length => 0;
    public int this[int i] => 0;
    public int Slice(int i, int j) => 0;
}

namespace System
{
    [Obsolete]
    public readonly struct Range
    {
        public Index Start { get; }

        public Index End { get; }

        public Range(Index start, Index end) => throw null;

        public static Range StartAt(Index start) => throw null;

        public static Range EndAt(Index end) => throw null;

        public static Range All => throw null;
    }
}
";
            // Note: we currently don't report Obsolete diagnostic on either Index or Range
            // Tracked by https://github.com/dotnet/roslyn/issues/57745
            var comp = CreateCompilation(new[] { source, TestSources.Index });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitIndexerAccessAsLValue()
        {
            var source = @"
#nullable enable

object? o1 = new object();
M(o1)[^1] = null; // 1
M(o1)[..] = null; // 2

_ = M(o1)[^1].ToString();
_ = M(o1)[..].ToString();

object o2 = null; // 3
M(o2)[^1] = null;
M(o2)[..] = null;

_ = M(o2)[^1].ToString(); // 4
_ = M(o2)[..].ToString(); // 5

static C<T> M<T>(T t) => throw null!;

class C<T>
{
    public int Length => 0;
    public ref T this[int i] => throw null!;
    public ref T Slice(int i, int j) => throw null!;
}
";
            var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
            comp.VerifyDiagnostics(
                // (5,13): warning CS8625: Cannot convert null literal to non-nullable reference type.
                // M(o1)[^1] = null; // 1
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 13),
                // (6,13): warning CS8625: Cannot convert null literal to non-nullable reference type.
                // M(o1)[..] = null; // 2
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(6, 13),
                // (11,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                // object o2 = null; // 3
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(11, 13),
                // (15,5): warning CS8602: Dereference of a possibly null reference.
                // _ = M(o2)[^1].ToString(); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M(o2)[^1]").WithLocation(15, 5),
                // (16,5): warning CS8602: Dereference of a possibly null reference.
                // _ = M(o2)[..].ToString(); // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M(o2)[..]").WithLocation(16, 5)
                );
        }

        [Fact]
        public void NullableIndexerArgument()
        {
            var source = @"
#nullable enable
var c = new C();

object o1 = null;
_ = c[M1(o1.ToString())];

object o2 = null;
_ = c[M2(o2.ToString())];

static System.Index M1(object? o) => throw null!;
static System.Range M2(object? o) => throw null!;

class C
{
    public int Length => 0;
    public int this[int i] => throw null!;
    public int Slice(int i, int j) => throw null!;
}
";
            var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
            comp.VerifyDiagnostics(
                // (5,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                // object o1 = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(5, 13),
                // (6,10): warning CS8602: Dereference of a possibly null reference.
                // _ = c[M1(o1.ToString())];
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o1").WithLocation(6, 10),
                // (8,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                // object o2 = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(8, 13),
                // (9,10): warning CS8602: Dereference of a possibly null reference.
                // _ = c[M2(o2.ToString())];
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o2").WithLocation(9, 10)
                );
        }

        [Fact]
        public void SemanticModelOnReceiver()
        {
            var source = @"
var c = new C();

_ = c[^1];
_ = c[..];

class C
{
    public int Length => 0;
    public int this[int i] => throw null!;
    public int Slice(int i, int j) => throw null!;
}
";
            var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range });
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var receivers = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Select(e => e.Expression).ToArray();
            Assert.Equal(2, receivers.Length);
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);

            Assert.Equal("c", receivers[0].ToString());
            Assert.Equal("C", model.GetTypeInfo(receivers[0]).Type.ToTestDisplayString());

            Assert.Equal("c", receivers[1].ToString());
            Assert.Equal("C", model.GetTypeInfo(receivers[1]).Type.ToTestDisplayString());
        }

        [Fact]
        public void Nullable_OrderOfEvaluation()
        {
            var source = @"
#nullable enable
C? c = null;
_ = (c = new C())[M1(c.ToString())];

c = null;
_ = (c = new C())[M2(c.ToString())];

c = null;
_ = c[M1((c = new C()).ToString())]; // 1

string? s = null;
_ = (s = string.Empty)[M1(s.ToString())];

s = null;
_ = (s = string.Empty)[M2(s.ToString())];

int[]? a = null;
_ = (a = new[] { 1 })[M1(a.ToString())];

a = null;
_ = (a = new[] { 1 })[M2(a.ToString())];

static System.Index M1(object? o) => throw null!;
static System.Range M2(object? o) => throw null!;

class C
{
    public int Length => 0;
    public int this[int i] => throw null!;
    public int Slice(int i, int j) => throw null!;
}
";
            var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
            comp.VerifyDiagnostics(
                // (10,5): warning CS8602: Dereference of a possibly null reference.
                // _ = c[M1((c = new C()).ToString())]; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(10, 5)
                );
        }

        [Fact]
        public void PatternIndexArrayAndAwait_01()
        {
            var src = @"
class C
{
    static async System.Threading.Tasks.Task M1(int[] arr)
    {
        arr[^1] = await System.Threading.Tasks.Task.FromResult(0);
    }

    static async System.Threading.Tasks.Task Main()
    {
        var arr = new[] { 123 };
        System.Console.WriteLine(arr[0]);
        await M1(arr);
        System.Console.WriteLine(arr[0]);
    }
}
";
            CompileAndVerifyWithIndexAndRange(src, expectedOutput:
@"
123
0
").VerifyDiagnostics();
        }

        [Fact]
        public void PatternIndexArrayAndAwait_02()
        {
            var src = @"
class C
{
    static async System.Threading.Tasks.Task M1(int[] arr)
    {
        (arr[^1], arr[0]) = (123, await System.Threading.Tasks.Task.FromResult(124));
    }

    static async System.Threading.Tasks.Task Main()
    {
        var arr = new int[2];
        await M1(arr);
        System.Console.WriteLine(arr[0]);
        System.Console.WriteLine(arr[1]);
    }
}
";
            CompileAndVerifyWithIndexAndRange(src, expectedOutput:
@"
124
123
").VerifyDiagnostics();
        }

        [Fact]
        public void PatternIndexArrayAndAwait_03()
        {
            var src = @"
class C
{
    static async System.Threading.Tasks.Task M1((int x, int y)[] arr)
    {
        arr[^1].x = await System.Threading.Tasks.Task.FromResult(124);
    }

    static async System.Threading.Tasks.Task Main()
    {
        var arr = new (int x, int y)[1];
        await M1(arr);
        System.Console.WriteLine(arr[0].x);
    }
}
";
            CompileAndVerifyWithIndexAndRange(src, expectedOutput:
@"
124
").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(58569, "https://github.com/dotnet/roslyn/issues/58569")]
        public void PatternIndexArrayAndAwait_04()
        {
            var src = @"
class C
{
    static async System.Threading.Tasks.Task M1((int x, int y)[] arr)
    {
        (arr[^1].x, arr[0].y) = (123, await System.Threading.Tasks.Task.FromResult(124));
    }

    static async System.Threading.Tasks.Task Main()
    {
        var arr = new (int x, int y)[2];
        await M1(arr);
        System.Console.WriteLine(arr[0].y);
        System.Console.WriteLine(arr[1].x);
    }
}
";
            CompileAndVerifyWithIndexAndRange(src, expectedOutput:
@"
124
123
").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(58569, "https://github.com/dotnet/roslyn/issues/58569")]
        public void PatternIndexArrayAndAwait_05()
        {
            var src = @"
class C
{
    static async System.Threading.Tasks.Task M1((int x, int y)[] arr)
    {
        arr[^1].x += await System.Threading.Tasks.Task.FromResult(124);
    }

    static async System.Threading.Tasks.Task Main()
    {
        var arr = new (int x, int y)[] { (1, 2) };
        await M1(arr);
        System.Console.WriteLine(arr[0].x);
    }
}
";
            CompileAndVerifyWithIndexAndRange(src, expectedOutput:
@"
125
").VerifyDiagnostics();
        }

        [Fact]
        public void PatternIndexArrayAndAwait_06()
        {
            var src = @"
class C
{
    static async System.Threading.Tasks.Task M1(int[] arr)
    {
        arr[^1] += await System.Threading.Tasks.Task.FromResult(124);
    }

    static async System.Threading.Tasks.Task Main()
    {
        var arr = new int[] { 1 };
        await M1(arr);
        System.Console.WriteLine(arr[0]);
    }
}
";
            CompileAndVerifyWithIndexAndRange(src, expectedOutput:
@"
125
").VerifyDiagnostics();
        }

        [Fact]
        public void PatternIndexArrayAndAwait_07()
        {
            var src = @"
class C
{
    static async System.Threading.Tasks.Task M1(int[][] arr)
    {
        arr[^1][0] += await System.Threading.Tasks.Task.FromResult(124);
    }

    static async System.Threading.Tasks.Task Main()
    {
        var arr = new[] { new[] { 1 } };
        await M1(arr);
        System.Console.WriteLine(arr[0][0]);
    }
}
";
            CompileAndVerifyWithIndexAndRange(src, expectedOutput:
@"
125
").VerifyDiagnostics();
        }

        [Fact]
        public void PatternIndexArrayAndAwait_08()
        {
            var src = @"
class C
{
    static async System.Threading.Tasks.Task M1((int x, int y)[] arr)
    {
        (arr[1..][^1].x, arr[1..][0].y) = (123, await System.Threading.Tasks.Task.FromResult(124));
    }

    static async System.Threading.Tasks.Task Main()
    {
        var arr = new (int x, int y)[5];
        await M1(arr);
        System.Console.WriteLine(""Done"");
    }
}
";
            CompileAndVerifyWithIndexAndRange(src, expectedOutput: "Done").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(65586, "https://github.com/dotnet/roslyn/issues/65586")]
        public void SideeffectsOfSlicing_01()
        {
            var src = @"
using System;

class Program
{
    static S s = new S();
    
    static void Main()
    {
        Test();
        Console.WriteLine(s.count); 
    }

    static void Test()
    {
        _ = s[1..2];
    }
}

struct S
{
    public int count;
    
    public int Slice(int start, int length)
    {
        count++;
        return 0;
    }
    
    public int Length => 10;
}";
            var verifier = CompileAndVerifyWithIndexAndRange(src, expectedOutput: "1").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test",
@"
{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldsflda    ""S Program.s""
  IL_0005:  ldc.i4.1
  IL_0006:  ldc.i4.1
  IL_0007:  call       ""int S.Slice(int, int)""
  IL_000c:  pop
  IL_000d:  ret
}
");
        }

        [Fact]
        [WorkItem(65586, "https://github.com/dotnet/roslyn/issues/65586")]
        public void SideeffectsOfSlicing_02()
        {
            var src = @"
using System;

class Program
{
    static S s = new S();
    
    static void Main()
    {
        Test();
        Console.WriteLine(s.count); 
    }

    static void Test()
    {
        _ = s[1..2];
    }
}

class S
{
    public int count;
    
    public int Slice(int start, int length)
    {
        count++;
        return 0;
    }
    
    public int Length => 10;
}";
            var verifier = CompileAndVerifyWithIndexAndRange(src, expectedOutput: "1").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test",
@"
{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldsfld     ""S Program.s""
  IL_0005:  ldc.i4.1
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   ""int S.Slice(int, int)""
  IL_000c:  pop
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SpanSlice()
        {
            string source = """
                using System;
                class Program
                {
                    static void M(Span<int> s)
                    {
                        var x = s[1..^1];
                        var y = s[^1];
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SingleOverloadReadOnlySpan(bool isMissing)
        {

            string source = """
                using System;
                
                Console.Write(Util.SecondToLast("0123").ToString());

                static class Util
                {
                    public static ReadOnlySpan<char> SecondToLast(ReadOnlySpan<char> s) => s[1..];
                }
                """;
            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray, TestSources.Span, TestSources.MemoryExtensions, TestSources.ITuple },
                                                   TestOptions.UnsafeReleaseExe);
            if (isMissing)
                comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int);
            // If Verification.Skipped is not passed, the IL Verify will fail with:
            //     System.Exception : IL Verify failed unexpectedly:
            //     [SecondToLast]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x8 }
            var verify = CompileAndVerify(comp, expectedOutput: "123", verify: Verification.Skipped);
            verify.VerifyDiagnostics();
            verify.VerifyIL("Util.SecondToLast",
                isMissing
                ? """
            {
              // Code size       19 (0x13)
              .maxstack  4
              .locals init (System.ReadOnlySpan<char>& V_0)
              IL_0000:  ldarga.s   V_0
              IL_0002:  stloc.0
              IL_0003:  ldloc.0
              IL_0004:  ldc.i4.1
              IL_0005:  ldloc.0
              IL_0006:  call       "int System.ReadOnlySpan<char>.Length.get"
              IL_000b:  ldc.i4.1
              IL_000c:  sub
              IL_000d:  call       "System.ReadOnlySpan<char> System.ReadOnlySpan<char>.Slice(int, int)"
              IL_0012:  ret
            }
            """
                : """
            {
              // Code size        9 (0x9)
              .maxstack  2
              IL_0000:  ldarga.s   V_0
              IL_0002:  ldc.i4.1
              IL_0003:  call       "System.ReadOnlySpan<char> System.ReadOnlySpan<char>.Slice(int)"
              IL_0008:  ret
            }
            """);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SingleOverloadSpan(bool isMissing)
        {

            string source = """
                using System;
                
                Console.Write(Util.SecondToLast("0123".ToCharArray()).ToString());

                static class Util
                {
                    public static Span<char> SecondToLast(Span<char> s) => s[1..];
                }
                """;
            var comp = CreateCompilationWithIndexAndRange(
                new[] { source, TestSources.GetSubArray, TestSources.Span, TestSources.MemoryExtensions },
                TestOptions.UnsafeReleaseExe);
            if (isMissing)
                comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int);

            // If Verification.Skipped is not passed, the IL Verify will fail with:
            //     System.Exception : IL Verify failed unexpectedly:
            //     [SecondToLast]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x8 }
            var verify = CompileAndVerify(comp, expectedOutput: "123", verify: Verification.Skipped);
            verify.VerifyDiagnostics();
            verify.VerifyIL("Util.SecondToLast",
                isMissing
                ? """
            {
              // Code size       19 (0x13)
              .maxstack  4
              .locals init (System.Span<char>& V_0)
              IL_0000:  ldarga.s   V_0
              IL_0002:  stloc.0
              IL_0003:  ldloc.0
              IL_0004:  ldc.i4.1
              IL_0005:  ldloc.0
              IL_0006:  call       "int System.Span<char>.Length.get"
              IL_000b:  ldc.i4.1
              IL_000c:  sub
              IL_000d:  call       "System.Span<char> System.Span<char>.Slice(int, int)"
              IL_0012:  ret
            }
            """
                : """
            {
              // Code size        9 (0x9)
              .maxstack  2
              IL_0000:  ldarga.s   V_0
              IL_0002:  ldc.i4.1
              IL_0003:  call       "System.Span<char> System.Span<char>.Slice(int)"
              IL_0008:  ret
            }
            """);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SingleOverloadString(bool isMissing)
        {
            string source = """
                using System;
                
                Console.Write("0123"[1..]);
                """;
            var comp = CreateCompilationWithIndexAndRange(
                new[] { source, TestSources.GetSubArray, },
                TestOptions.ReleaseExe);
            if (isMissing)
                comp.MakeMemberMissing(SpecialMember.System_String__SubstringInt);
            var verify = CompileAndVerify(comp, expectedOutput: "123");
            verify.VerifyDiagnostics();
            verify.VerifyIL("<top-level-statements-entry-point>",
                isMissing
                ? """
            {
              // Code size       27 (0x1b)
              .maxstack  4
              .locals init (string V_0)
              IL_0000:  ldstr      "0123"
              IL_0005:  stloc.0
              IL_0006:  ldloc.0
              IL_0007:  ldc.i4.1
              IL_0008:  ldloc.0
              IL_0009:  callvirt   "int string.Length.get"
              IL_000e:  ldc.i4.1
              IL_000f:  sub
              IL_0010:  callvirt   "string string.Substring(int, int)"
              IL_0015:  call       "void System.Console.Write(string)"
              IL_001a:  ret
            }
            """
                : """
            {
              // Code size       17 (0x11)
              .maxstack  2

              IL_0000:  ldstr      "0123"
              IL_0005:  ldc.i4.1
              IL_0006:  callvirt   "string string.Substring(int)"
              IL_000b:  call       "void System.Console.Write(string)"
              IL_0010:  ret
            }
            """);
        }
    }
}
