﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        [Fact]
        public void TupleDictionary01()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    static void Main()
    {
        var k = (1, 2);
        var v = (a: 1, b: (c: 2, d: (e: 3, f: 4)));

        var d = Test(k, v);

        System.Console.WriteLine(d[(1, 2)].b.d.Item2);
    }

    static Dictionary<K, V> Test<K, V>(K key, V value)
    {
        var d = new Dictionary<K, V>();

        d[key] = value;

        return d;
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"4");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       67 (0x43)
  .maxstack  6
  .locals init (System.Runtime.CompilerServices.ValueTuple<int, <tuple: int c, <tuple: int e, int f> d>> V_0) //v
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""System.Runtime.CompilerServices.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.1
  IL_000a:  ldc.i4.2
  IL_000b:  ldc.i4.3
  IL_000c:  ldc.i4.4
  IL_000d:  newobj     ""System.Runtime.CompilerServices.ValueTuple<int, int>..ctor(int, int)""
  IL_0012:  newobj     ""System.Runtime.CompilerServices.ValueTuple<int, <tuple: int e, int f>>..ctor(int, <tuple: int e, int f>)""
  IL_0017:  call       ""System.Runtime.CompilerServices.ValueTuple<int, <tuple: int c, <tuple: int e, int f> d>>..ctor(int, <tuple: int c, <tuple: int e, int f> d>)""
  IL_001c:  ldloc.0
  IL_001d:  call       ""System.Collections.Generic.Dictionary<<tuple: int Item1, int Item2>, <tuple: int a, <tuple: int c, <tuple: int e, int f> d> b>> C.Test<<tuple: int Item1, int Item2>, <tuple: int a, <tuple: int c, <tuple: int e, int f> d> b>>(<tuple: int Item1, int Item2>, <tuple: int a, <tuple: int c, <tuple: int e, int f> d> b>)""
  IL_0022:  ldc.i4.1
  IL_0023:  ldc.i4.2
  IL_0024:  newobj     ""System.Runtime.CompilerServices.ValueTuple<int, int>..ctor(int, int)""
  IL_0029:  callvirt   ""<tuple: int a, <tuple: int c, <tuple: int e, int f> d> b> System.Collections.Generic.Dictionary<<tuple: int Item1, int Item2>, <tuple: int a, <tuple: int c, <tuple: int e, int f> d> b>>.this[<tuple: int Item1, int Item2>].get""
  IL_002e:  ldfld      ""<tuple: int c, <tuple: int e, int f> d> System.Runtime.CompilerServices.ValueTuple<int, <tuple: int c, <tuple: int e, int f> d>>.Item2""
  IL_0033:  ldfld      ""<tuple: int e, int f> System.Runtime.CompilerServices.ValueTuple<int, <tuple: int e, int f>>.Item2""
  IL_0038:  ldfld      ""int System.Runtime.CompilerServices.ValueTuple<int, int>.Item2""
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  ret
}
");
        }

        [Fact]
        public void TupleLambdaCapture01()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42));
    }

    public static T Test<T>(T a)
    {
        var x = (f1: a, f2: a);

        Func<T> f = () => x.f2;

        return f();
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.<>c__DisplayClass1_0<T>.<Test>b__0()", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""<tuple: T f1, T f2> C.<>c__DisplayClass1_0<T>.x""
  IL_0006:  ldfld      ""T System.Runtime.CompilerServices.ValueTuple<T, T>.Item2""
  IL_000b:  ret
}
");
        }

        [Fact]
        public void TupleAsyncCapture01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42).Result);
    }

    public static async Task<T> Test<T>(T a)
    {
        var x = (f1: a, f2: a);

        await Task.Yield();

        return x.f1;
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"42");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.<Test>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      191 (0xbf)
  .maxstack  3
  .locals init (int V_0,
                T V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0058
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""T C.<Test>d__1<T>.a""
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""T C.<Test>d__1<T>.a""
    IL_0017:  newobj     ""System.Runtime.CompilerServices.ValueTuple<T, T>..ctor(T, T)""
    IL_001c:  stfld      ""<tuple: T f1, T f2> C.<Test>d__1<T>.<x>5__1""
    IL_0021:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0026:  stloc.3
    IL_0027:  ldloca.s   V_3
    IL_0029:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_002e:  stloc.2
    IL_002f:  ldloca.s   V_2
    IL_0031:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0036:  brtrue.s   IL_0074
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.0
    IL_003c:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_0041:  ldarg.0
    IL_0042:  ldloc.2
    IL_0043:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
    IL_004e:  ldloca.s   V_2
    IL_0050:  ldarg.0
    IL_0051:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<Test>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<Test>d__1<T>)""
    IL_0056:  leave.s    IL_00be
    IL_0058:  ldarg.0
    IL_0059:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_005e:  stloc.2
    IL_005f:  ldarg.0
    IL_0060:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_0065:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_006b:  ldarg.0
    IL_006c:  ldc.i4.m1
    IL_006d:  dup
    IL_006e:  stloc.0
    IL_006f:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_0074:  ldloca.s   V_2
    IL_0076:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_007b:  ldloca.s   V_2
    IL_007d:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0083:  ldarg.0
    IL_0084:  ldflda     ""<tuple: T f1, T f2> C.<Test>d__1<T>.<x>5__1""
    IL_0089:  ldfld      ""T System.Runtime.CompilerServices.ValueTuple<T, T>.Item1""
    IL_008e:  stloc.1
    IL_008f:  leave.s    IL_00aa
  }
  catch System.Exception
  {
    IL_0091:  stloc.s    V_4
    IL_0093:  ldarg.0
    IL_0094:  ldc.i4.s   -2
    IL_0096:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_009b:  ldarg.0
    IL_009c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
    IL_00a1:  ldloc.s    V_4
    IL_00a3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.SetException(System.Exception)""
    IL_00a8:  leave.s    IL_00be
  }
  IL_00aa:  ldarg.0
  IL_00ab:  ldc.i4.s   -2
  IL_00ad:  stfld      ""int C.<Test>d__1<T>.<>1__state""
  IL_00b2:  ldarg.0
  IL_00b3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
  IL_00b8:  ldloc.1
  IL_00b9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.SetResult(T)""
  IL_00be:  ret
}
");
        }

    }
}
