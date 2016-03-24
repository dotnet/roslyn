// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenTupleTests : CSharpTestBase
    {
        private static readonly string trivial2uple =
                    @"

// PROTOTYPE: put in correct namespace
namespace System
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
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }
    }
}

            ";

        private static readonly string trivial3uple =
                @"

    // PROTOTYPE: put in correct namespace
    namespace System
    {
        // struct with two values
        public struct ValueTuple<T1, T2, T3>
        {
            public T1 Item1;
            public T2 Item2;
            public T3 Item3;

            public ValueTuple(T1 item1, T2 item2, T3 item3)
            {
                this.Item1 = item1;
                this.Item2 = item2;
                this.Item3 = item3;
            }

            public override string ToString()
            {
                return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + "", "" + Item3?.ToString() + '}';
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
  .locals init (System.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  constrained. ""System.ValueTuple<int, int>""
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
  .locals init (System.ValueTuple<int, string> V_0, //x
                System.ValueTuple<int, <tuple: int Item1, int Item2>> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  ldc.i4.4
  IL_0006:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000b:  newobj     ""System.ValueTuple<int, <tuple: int Item1, int Item2>>..ctor(int, <tuple: int Item1, int Item2>)""
  IL_0010:  stloc.1
  IL_0011:  ldloca.s   V_1
  IL_0013:  constrained. ""System.ValueTuple<int, <tuple: int Item1, int Item2>>""
  IL_0019:  callvirt   ""string object.ToString()""
  IL_001e:  call       ""System.ValueTuple<int, string>..ctor(int, string)""
  IL_0023:  ldloca.s   V_0
  IL_0025:  constrained. ""System.ValueTuple<int, string>""
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
  .locals init (System.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     ""int System.ValueTuple<int, int>.Item2""
  IL_0010:  call       ""string int.ToString()""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0023:  ldloc.0
  IL_0024:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0029:  ldloc.0
  IL_002a:  ldfld      ""int System.ValueTuple<int, int>.Item2""
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
  .locals init (System.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     ""int System.ValueTuple<int, int>.Item2""
  IL_0010:  call       ""string int.ToString()""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0023:  ldloc.0
  IL_0024:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0029:  ldloc.0
  IL_002a:  ldfld      ""int System.ValueTuple<int, int>.Item2""
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
  .locals init (System.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     ""int System.ValueTuple<int, int>.Item2""
  IL_0010:  call       ""string int.ToString()""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0023:  ldloc.0
  IL_0024:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0029:  ldloc.0
  IL_002a:  ldfld      ""int System.ValueTuple<int, int>.Item2""
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
  .locals init (System.ValueTuple<int, <tuple: int c, int d>> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000a:  call       ""System.ValueTuple<int, <tuple: int c, int d>>..ctor(int, <tuple: int c, int d>)""
  IL_000f:  ldloca.s   V_0
  IL_0011:  ldflda     ""<tuple: int c, int d> System.ValueTuple<int, <tuple: int c, int d>>.Item2""
  IL_0016:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_001b:  call       ""string int.ToString()""
  IL_0020:  call       ""void System.Console.WriteLine(string)""
  IL_0025:  ldloca.s   V_0
  IL_0027:  ldflda     ""<tuple: int c, int d> System.ValueTuple<int, <tuple: int c, int d>>.Item2""
  IL_002c:  ldc.i4.s   39
  IL_002e:  stfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0033:  ldloc.0
  IL_0034:  ldfld      ""int System.ValueTuple<int, <tuple: int c, int d>>.Item1""
  IL_0039:  ldloc.0
  IL_003a:  ldfld      ""<tuple: int c, int d> System.ValueTuple<int, <tuple: int c, int d>>.Item2""
  IL_003f:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0044:  add
  IL_0045:  ldloc.0
  IL_0046:  ldfld      ""<tuple: int c, int d> System.ValueTuple<int, <tuple: int c, int d>>.Item2""
  IL_004b:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0050:  add
  IL_0051:  call       ""void System.Console.WriteLine(int)""
  IL_0056:  ret
}
");
        }
        [Fact]
        public void TupleTypeDeclaration()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string, int) x = (1, ""hello"", 2);
        System.Console.WriteLine(x.ToString());
    }
}

" + trivial3uple;

            var comp = CompileAndVerify(source, expectedOutput: @"{1, hello, 2}");
        }

        [Fact]
        public void TupleTypeMismatch()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string) x = (1, ""hello"", 2);
    }
}
" + trivial2uple + trivial3uple;

            // PROTOTYPE there should be no mention of Item1, Item2 or Item3 in there error for this test
            // (int a, string b) should print as < tuple: int a, string b> and(int, string) should print out as < tuple: int, string> (not < tuple:int Item1, string Item2>).
            // Only user-provided names should appear in the TupleTypeSymbol. The underlying type(ValueTuple) is the one that provides Item1, etc.
            // At the moment, because our error checking is incomplete, and also for purpose of error recovery, we assign "Item1" and such into the tuple symbol when the user missed some names.But I think this is a prototype bug.

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,27): error CS0029: Cannot implicitly convert type '<tuple: int Item1, string Item2, int Item3>' to '<tuple: int Item1, string Item2>'
                //         (int, string) x = (1, "hello", 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"(1, ""hello"", 2)").WithArguments("<tuple: int Item1, string Item2, int Item3>", "<tuple: int Item1, string Item2>").WithLocation(6, 27));
        }

        [Fact]
        public void TupleTypeWithLateDiscoveredName()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string a) x = (1, ""hello"", c: 2);
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics(
                // (6,29): error CS8203: Tuple member names must all be provided, if any one is provided.
                //         (int, string a) x = (1, "hello", c: 2);
                Diagnostic(ErrorCode.ERR_TupleExplicitNamesOnAllMembersOrNone, @"(1, ""hello"", c: 2)").WithLocation(6, 29));

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(1, ""hello"", c: 2)", node.ToString());
            Assert.Equal("<tuple: System.Int32 Item1, System.String Item2, System.Int32 c>", model.GetTypeInfo(node).Type.ToTestDisplayString());

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("<tuple: System.Int32 Item1, System.String a> x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleTypeDeclarationWithNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b) x = (1, ""hello"");
        System.Console.WriteLine(x.a.ToString());
        System.Console.WriteLine(x.b.ToString());
    }
}
" + trivial2uple;
            var comp = CompileAndVerify(source, expectedOutput: @"1
hello");
        }

        [Fact]
        public void TupleWithOnlySomeNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string a) x = (b: 1, ""hello"", 2);
    }
}
" + trivial2uple + trivial3uple;

            // PROTOTYPE we also expect an error on the tuple type declaration
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,29): error CS8203: Tuple member names must all be provided, if any one is provided.
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_TupleExplicitNamesOnAllMembersOrNone, @"(b: 1, ""hello"", 2)").WithLocation(6, 29));
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
  .locals init (System.ValueTuple<int, <tuple: int c, <tuple: int e, int f> d>> V_0) //v
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.1
  IL_000a:  ldc.i4.2
  IL_000b:  ldc.i4.3
  IL_000c:  ldc.i4.4
  IL_000d:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0012:  newobj     ""System.ValueTuple<int, <tuple: int e, int f>>..ctor(int, <tuple: int e, int f>)""
  IL_0017:  call       ""System.ValueTuple<int, <tuple: int c, <tuple: int e, int f> d>>..ctor(int, <tuple: int c, <tuple: int e, int f> d>)""
  IL_001c:  ldloc.0
  IL_001d:  call       ""System.Collections.Generic.Dictionary<<tuple: int Item1, int Item2>, <tuple: int a, <tuple: int c, <tuple: int e, int f> d> b>> C.Test<<tuple: int Item1, int Item2>, <tuple: int a, <tuple: int c, <tuple: int e, int f> d> b>>(<tuple: int Item1, int Item2>, <tuple: int a, <tuple: int c, <tuple: int e, int f> d> b>)""
  IL_0022:  ldc.i4.1
  IL_0023:  ldc.i4.2
  IL_0024:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0029:  callvirt   ""<tuple: int a, <tuple: int c, <tuple: int e, int f> d> b> System.Collections.Generic.Dictionary<<tuple: int Item1, int Item2>, <tuple: int a, <tuple: int c, <tuple: int e, int f> d> b>>.this[<tuple: int Item1, int Item2>].get""
  IL_002e:  ldfld      ""<tuple: int c, <tuple: int e, int f> d> System.ValueTuple<int, <tuple: int c, <tuple: int e, int f> d>>.Item2""
  IL_0033:  ldfld      ""<tuple: int e, int f> System.ValueTuple<int, <tuple: int e, int f>>.Item2""
  IL_0038:  ldfld      ""int System.ValueTuple<int, int>.Item2""
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
  IL_0006:  ldfld      ""T System.ValueTuple<T, T>.Item2""
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
    IL_0017:  newobj     ""System.ValueTuple<T, T>..ctor(T, T)""
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
    IL_0089:  ldfld      ""T System.ValueTuple<T, T>.Item1""
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

        [Fact]
        public void TupleUsageWithoutTupleLibrary()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string a) x = (b: 1, ""hello"", 2);
    }
}
";
            // PROTOTYPE those are not the final diagnostics
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(int, string a)").WithArguments("System.ValueTuple`2").WithLocation(6, 9),
                // (6,9): error CS0656: Missing compiler required member 'System.ValueTuple`2.Item1'
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(int, string a)").WithArguments("System.ValueTuple`2", "Item1").WithLocation(6, 9),
                // (6,9): error CS0656: Missing compiler required member 'System.ValueTuple`2.Item2'
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(int, string a)").WithArguments("System.ValueTuple`2", "Item2").WithLocation(6, 9),
                // (6,29): error CS8203: Tuple member names must all be provided, if any one is provided.
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_TupleExplicitNamesOnAllMembersOrNone, @"(b: 1, ""hello"", 2)").WithLocation(6, 29),
                // (6,29): error CS0518: Predefined type 'System.ValueTuple`3' is not defined or imported
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"(b: 1, ""hello"", 2)").WithArguments("System.ValueTuple`3").WithLocation(6, 29),
                // (6,29): error CS0656: Missing compiler required member 'System.ValueTuple`3.Item1'
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"(b: 1, ""hello"", 2)").WithArguments("System.ValueTuple`3", "Item1").WithLocation(6, 29),
                // (6,29): error CS0656: Missing compiler required member 'System.ValueTuple`3.Item2'
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"(b: 1, ""hello"", 2)").WithArguments("System.ValueTuple`3", "Item2").WithLocation(6, 29),
                // (6,29): error CS0656: Missing compiler required member 'System.ValueTuple`3.Item3'
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"(b: 1, ""hello"", 2)").WithArguments("System.ValueTuple`3", "Item3").WithLocation(6, 29),
                // (6,29): error CS8204: PROTOTYPE This is not supported yet.
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_PrototypeNotYetImplemented, @"(b: 1, ""hello"", 2)").WithLocation(6, 29));
        }

        [Fact]
        public void TupleWithDuplicateNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string a) x = (b: 1, b: ""hello"", b: 2);
    }
}
" + trivial2uple + trivial3uple;

            // PROTOTYPE we expect similar errors on "a"
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,38): error CS8202: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, b: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "b").WithLocation(6, 38),
                // (6,50): error CS8202: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, b: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "b").WithLocation(6, 50));
        }

        [Fact]
        public void TupleWithDuplicateReservedNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int Item1, string Item1) x = (Item1: 1, Item1: ""hello"");
        (int Item2, string Item2) y = (Item2: 1, Item2: ""hello"");
    }
}
" + trivial2uple;

            // PROTOTYPE we expect similar errors on the type declarations
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,50): error CS8201: Tuple member name 'Item1' is disallowed at position 2.
                //         (int Item1, string Item1) x = (Item1: 1, Item1: "hello");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item1").WithArguments("Item1", "2").WithLocation(6, 50),
                // (7,40): error CS8201: Tuple member name 'Item2' is disallowed at position 1.
                //         (int Item2, string Item2) y = (Item2: 1, Item2: "hello");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "1").WithLocation(7, 40));
        }

        [Fact]
        public void DefaultValueForTuple()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b) x = (1, ""hello"");
        x = default((int, string));
        System.Console.WriteLine(x.a);
        System.Console.WriteLine(x.b ?? ""null"");
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"0
null");
        }

        [Fact]
        public void TupleWithDuplicateMemberNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string a) x = (b: 1, c: ""hello"", b: 2);
    }
}
" + trivial2uple + trivial3uple;

            // PROTOTYPE we also expect duplicate member name error on the tuple type declaration
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,50): error CS8202: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, c: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "b").WithLocation(6, 50));
        }

        [Fact]
        public void TupleWithReservedMemberNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int Item1, string Item3) x = (Item2: 1, Item4: ""hello"", Item3: 2);
    }
}
" + trivial2uple + trivial3uple;

            // PROTOTYPE we also expect reserved member name error on the tuple type declaration
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,40): error CS8201: Tuple member name 'Item2' is disallowed at position 1.
                //         (int Item1, string Item3) x = (Item2: 1, Item4: "hello", Item3: 2);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "1").WithLocation(6, 40),
                // (6,50): error CS8201: Tuple member name 'Item4' is disallowed at position 2.
                //         (int Item1, string Item3) x = (Item2: 1, Item4: "hello", Item3: 2);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item4").WithArguments("Item4", "2").WithLocation(6, 50));
        }

        // PROTOTYPE this test can be removed once tuple-8 and above are implemented
        [Fact]
        public void LongTupleDeclarationDoesntCrash()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, int, int, int, int, int, int, int, int, int, int, int) x;
    }
}
" + trivial2uple;

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,9): error CS8204: PROTOTYPE This is not supported yet.
                //         (int, int, int, int, int, int, int, int, int, int, int, int) x;
                Diagnostic(ErrorCode.ERR_PrototypeNotYetImplemented, "(int, int, int, int, int, int, int, int, int, int, int, int)").WithLocation(6, 9),
                // (6,70): warning CS0168: The variable 'x' is declared but never used
                //         (int, int, int, int, int, int, int, int, int, int, int, int) x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(6, 70));
        }


        // PROTOTYPE this test can be removed once tuple-8 and above are implemented
        [Fact]
        public void LongTupleCreationDoesntCrash()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
    }
}
" + trivial2uple;

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,17): error CS8204: PROTOTYPE This is not supported yet.
                //         var x = (1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
                Diagnostic(ErrorCode.ERR_PrototypeNotYetImplemented, "(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1)").WithLocation(6, 17));
        }

        [Fact]
        public void MethodReturnsValueTuple()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(M().ToString());
    }

    static (int, string) M()
    {
        return (1, ""hello"");
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"{1, hello}");
        }

        [Fact]
        public void Tuple2To7Members()
        {

            var source = trivial2uple + trivial3uple + @"
namespace System
{
    public struct ValueTuple<T1, T2, T3, T4>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
        }
    }

    public struct ValueTuple<T1, T2, T3, T4, T5>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
        }
    }

    public struct ValueTuple<T1, T2, T3, T4, T5, T6>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
        }
    }

    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
        }
    }
}

class C
{
    static void Main()
    {
        System.Console.Write((1, 2).Item1);
        System.Console.Write((1, 2).Item2);
        System.Console.Write((3, 4, 5).Item1);
        System.Console.Write((3, 4, 5).Item2);
        System.Console.Write((3, 4, 5).Item3);
        System.Console.Write((6, 7, 8, 9).Item1);
        System.Console.Write((6, 7, 8, 9).Item2);
        System.Console.Write((6, 7, 8, 9).Item3);
        System.Console.Write((6, 7, 8, 9).Item4);
        System.Console.Write((0, 1, 2, 3, 4).Item1);
        System.Console.Write((0, 1, 2, 3, 4).Item2);
        System.Console.Write((0, 1, 2, 3, 4).Item3);
        System.Console.Write((0, 1, 2, 3, 4).Item4);
        System.Console.Write((0, 1, 2, 3, 4).Item5);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item1);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item2);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item3);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item4);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item5);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item6);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item1);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item2);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item3);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item4);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item5);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item6);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item7);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "123456789012345678901234567");
        }
    }
}