// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenTupleTests : CSharpTestBase
    {
        private static readonly string trivial2uple =
                    @"

// PROTOTYPE: put in correct namespace
namespace System.Runtime.CompilerServices
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2.ToString() + '}';
        }
    }
}

            ";

        [Fact]
        public void SimpleTuple()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (1, 2);
        System.Console.WriteLine(x.ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: "{1, 2}");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (System.Runtime.CompilerServices.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.Runtime.CompilerServices.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  constrained. ""System.Runtime.CompilerServices.ValueTuple<int, int>""
  IL_0011:  callvirt   ""string object.ToString()""
  IL_0016:  call       ""void System.Console.WriteLine(string)""
  IL_001b:  ret
}");
        }

        [Fact]
        public void SimpleTupleNested()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (1, (2, (3, 4)).ToString());
        System.Console.WriteLine(x.ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: "{1, {2, {3, 4}}}");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  5
  .locals init (System.Runtime.CompilerServices.ValueTuple<int, string> V_0, //x
                System.Runtime.CompilerServices.ValueTuple<int, <tuple: int Item1, int Item2>> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  ldc.i4.4
  IL_0006:  newobj     ""System.Runtime.CompilerServices.ValueTuple<int, int>..ctor(int, int)""
  IL_000b:  newobj     ""System.Runtime.CompilerServices.ValueTuple<int, <tuple: int Item1, int Item2>>..ctor(int, <tuple: int Item1, int Item2>)""
  IL_0010:  stloc.1
  IL_0011:  ldloca.s   V_1
  IL_0013:  constrained. ""System.Runtime.CompilerServices.ValueTuple<int, <tuple: int Item1, int Item2>>""
  IL_0019:  callvirt   ""string object.ToString()""
  IL_001e:  call       ""System.Runtime.CompilerServices.ValueTuple<int, string>..ctor(int, string)""
  IL_0023:  ldloca.s   V_0
  IL_0025:  constrained. ""System.Runtime.CompilerServices.ValueTuple<int, string>""
  IL_002b:  callvirt   ""string object.ToString()""
  IL_0030:  call       ""void System.Console.WriteLine(string)""
  IL_0035:  ret
}");
        }

        [Fact]
        public void TupleUnderlyingItemAccess()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (1, 2);
        System.Console.WriteLine(x.Item2.ToString());
        x.Item1 = 40;
        System.Console.WriteLine(x.Item1 + x.Item2);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"2
42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (System.Runtime.CompilerServices.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.Runtime.CompilerServices.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item2""
  IL_0010:  call       ""string int.ToString()""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item1""
  IL_0023:  ldloc.0
  IL_0024:  ldfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item1""
  IL_0029:  ldloc.0
  IL_002a:  ldfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item2""
  IL_002f:  add
  IL_0030:  call       ""void System.Console.WriteLine(int)""
  IL_0035:  ret
}
");
        }

        [Fact]
        public void TupleUnderlyingItemAccess01()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: 1, b: 2);
        System.Console.WriteLine(x.Item2.ToString());
        x.Item1 = 40;
        System.Console.WriteLine(x.Item1 + x.Item2);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"2
42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (System.Runtime.CompilerServices.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.Runtime.CompilerServices.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item2""
  IL_0010:  call       ""string int.ToString()""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item1""
  IL_0023:  ldloc.0
  IL_0024:  ldfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item1""
  IL_0029:  ldloc.0
  IL_002a:  ldfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item2""
  IL_002f:  add
  IL_0030:  call       ""void System.Console.WriteLine(int)""
  IL_0035:  ret
}
");
        }

        [Fact]
        public void TupleItemAccess()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: 1, b: 2);
        System.Console.WriteLine(x.b.ToString());
        x.a = 40;
        System.Console.WriteLine(x.a + x.b);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"2
42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (System.Runtime.CompilerServices.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.Runtime.CompilerServices.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item2""
  IL_0010:  call       ""string int.ToString()""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item1""
  IL_0023:  ldloc.0
  IL_0024:  ldfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item1""
  IL_0029:  ldloc.0
  IL_002a:  ldfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item2""
  IL_002f:  add
  IL_0030:  call       ""void System.Console.WriteLine(int)""
  IL_0035:  ret
}
");
        }

        [Fact]
        public void TupleItemAccess01()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: 1, b: (c: 2, d: 3));
        System.Console.WriteLine(x.b.c.ToString());
        x.b.d = 39;
        System.Console.WriteLine(x.a + x.b.c + x.b.d);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"2
42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       87 (0x57)
  .maxstack  4
  .locals init (System.Runtime.CompilerServices.ValueTuple<int, <tuple: int c, int d>> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  newobj     ""System.Runtime.CompilerServices.ValueTuple<int, int>..ctor(int, int)""
  IL_000a:  call       ""System.Runtime.CompilerServices.ValueTuple<int, <tuple: int c, int d>>..ctor(int, <tuple: int c, int d>)""
  IL_000f:  ldloca.s   V_0
  IL_0011:  ldflda     ""<tuple: int c, int d> System.Runtime.CompilerServices.ValueTuple<int, <tuple: int c, int d>>.Item2""
  IL_0016:  ldflda     ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item1""
  IL_001b:  call       ""string int.ToString()""
  IL_0020:  call       ""void System.Console.WriteLine(string)""
  IL_0025:  ldloca.s   V_0
  IL_0027:  ldflda     ""<tuple: int c, int d> System.Runtime.CompilerServices.ValueTuple<int, <tuple: int c, int d>>.Item2""
  IL_002c:  ldc.i4.s   39
  IL_002e:  stfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item2""
  IL_0033:  ldloc.0
  IL_0034:  ldfld      ""int System.Runtime.CompilerServices.ValueTuple<int, <tuple: int c, int d>>.Item1""
  IL_0039:  ldloc.0
  IL_003a:  ldfld      ""<tuple: int c, int d> System.Runtime.CompilerServices.ValueTuple<int, <tuple: int c, int d>>.Item2""
  IL_003f:  ldfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item1""
  IL_0044:  add
  IL_0045:  ldloc.0
  IL_0046:  ldfld      ""<tuple: int c, int d> System.Runtime.CompilerServices.ValueTuple<int, <tuple: int c, int d>>.Item2""
  IL_004b:  ldfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item2""
  IL_0050:  add
  IL_0051:  call       ""void System.Console.WriteLine(int)""
  IL_0056:  ret
}
");
        }

    }
}
