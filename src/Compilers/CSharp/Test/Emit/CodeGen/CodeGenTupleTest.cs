// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using System;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenTupleTests : CSharpTestBase
    {
        private static readonly string trivial2uple =
                    @"

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

        private static readonly string trivalRemainingTuples = @"
namespace System
{
    public struct ValueTuple<T1>
    {
        public T1 Item1;

        public ValueTuple(T1 item1)
        {
            this.Item1 = item1;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + '}';
        }
    }

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

    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public TRest Rest;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
            Rest = rest;
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

            var comp = CompileAndVerify(source, expectedOutput: "{1, 2}", parseOptions: TestOptions.Regular.WithTuplesFeature());
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

        [Fact(Skip = "PROTOTYPE(tuples): this should work, just found a bug while working on other stuff.")]
        public void SimpleTupleNew()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = new (int, int)(1, 2);
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
        public void SimpleTuple2()
        {
            var source = @"
class C
{
    static void Main()
    {
        var s = Single((a:1, b:2));
        System.Console.WriteLine(s[0].b.ToString());
    }

    static T[] Single<T>(T x)
    {
        return new T[]{x};
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: "2", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  call       ""(int a, int b)[] C.Single<(int a, int b)>((int a, int b))""
  IL_000c:  ldc.i4.0
  IL_000d:  ldelema    ""System.ValueTuple<int, int>""
  IL_0012:  ldflda     ""int System.ValueTuple<int, int>.Item2""
  IL_0017:  call       ""string int.ToString()""
  IL_001c:  call       ""void System.Console.WriteLine(string)""
  IL_0021:  ret
}");
        }

        [Fact]
        public void SimpleTupleTargetTyped()
        {
            var source = @"
class C
{
    static void Main()
    {
        (object, object) x = (null, null);
        System.Console.WriteLine(x.ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: "{, }", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (System.ValueTuple<object, object> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldnull
  IL_0003:  ldnull
  IL_0004:  call       ""System.ValueTuple<object, object>..ctor(object, object)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  constrained. ""System.ValueTuple<object, object>""
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

            var comp = CompileAndVerify(source, expectedOutput: "{1, {2, {3, 4}}}", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  5
  .locals init (System.ValueTuple<int, string> V_0, //x
                System.ValueTuple<int, (int, int)> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  ldc.i4.4
  IL_0006:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000b:  newobj     ""System.ValueTuple<int, (int, int)>..ctor(int, (int, int))""
  IL_0010:  stloc.1
  IL_0011:  ldloca.s   V_1
  IL_0013:  constrained. ""System.ValueTuple<int, (int, int)>""
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

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"2
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

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"2
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

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"2
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

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"2
42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       87 (0x57)
  .maxstack  4
  .locals init (System.ValueTuple<int, (int c, int d)> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000a:  call       ""System.ValueTuple<int, (int c, int d)>..ctor(int, (int c, int d))""
  IL_000f:  ldloca.s   V_0
  IL_0011:  ldflda     ""(int c, int d) System.ValueTuple<int, (int c, int d)>.Item2""
  IL_0016:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_001b:  call       ""string int.ToString()""
  IL_0020:  call       ""void System.Console.WriteLine(string)""
  IL_0025:  ldloca.s   V_0
  IL_0027:  ldflda     ""(int c, int d) System.ValueTuple<int, (int c, int d)>.Item2""
  IL_002c:  ldc.i4.s   39
  IL_002e:  stfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0033:  ldloc.0
  IL_0034:  ldfld      ""int System.ValueTuple<int, (int c, int d)>.Item1""
  IL_0039:  ldloc.0
  IL_003a:  ldfld      ""(int c, int d) System.ValueTuple<int, (int c, int d)>.Item2""
  IL_003f:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0044:  add
  IL_0045:  ldloc.0
  IL_0046:  ldfld      ""(int c, int d) System.ValueTuple<int, (int c, int d)>.Item2""
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

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"{1, hello, 2}");
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

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature()).VerifyDiagnostics(
                // (6,27): error CS0029: Cannot implicitly convert type '(int, string, int)' to '(int, string)'
                //         (int, string) x = (1, "hello", 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"(1, ""hello"", 2)").WithArguments("(int, string, int)", "(int, string)").WithLocation(6, 27));
        }

        [Fact]
        public void LongTupleTypeMismatch()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, int, int, int, int, int, int, int) x = (""Alice"", 2, 3, 4, 5, 6, 7, 8);
        (int, int, int, int, int, int, int, int) y = (1, 2, 3, 4, 5, 6, 7, 8, 9);
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature()).VerifyDiagnostics(
                // (6,54): error CS0029: Cannot implicitly convert type '(string, int, int, int, int, int, int, int)' to '(int, int, int, int, int, int, int, int)'
                //         (int, int, int, int, int, int, int, int) x = ("Alice", 2, 3, 4, 5, 6, 7, 8);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"(""Alice"", 2, 3, 4, 5, 6, 7, 8)").WithArguments("(string, int, int, int, int, int, int, int)", "(int, int, int, int, int, int, int, int)").WithLocation(6, 54),
                // (7,54): error CS0029: Cannot implicitly convert type '(int, int, int, int, int, int, int, int, int)' to '(int, int, int, int, int, int, int, int)'
                //         (int, int, int, int, int, int, int, int) y = (1, 2, 3, 4, 5, 6, 7, 8, 9);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(1, 2, 3, 4, 5, 6, 7, 8, 9)").WithArguments("(int, int, int, int, int, int, int, int, int)", "(int, int, int, int, int, int, int, int)").WithLocation(7, 54)
                );
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

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics(
                // (6,9): error CS8204: Tuple member names must all be provided, if any one is provided.
                //         (int, string a) x = (1, "hello", c: 2);
                Diagnostic(ErrorCode.ERR_TupleExplicitNamesOnAllMembersOrNone, "(int, string a)").WithLocation(6, 9),
                // (6,29): error CS8204: Tuple member names must all be provided, if any one is provided.
                //         (int, string a) x = (1, "hello", c: 2);
                Diagnostic(ErrorCode.ERR_TupleExplicitNamesOnAllMembersOrNone, @"(1, ""hello"", c: 2)").WithLocation(6, 29)
                );

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(1, ""hello"", c: 2)", node.ToString());
            Assert.Equal("(System.Int32 Item1, System.String Item2, System.Int32 c)", model.GetTypeInfo(node).Type.ToTestDisplayString());

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int32 Item1, System.String a) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
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
            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"1
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

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature()).VerifyDiagnostics(
                // (6,9): error CS8204: Tuple member names must all be provided, if any one is provided.
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_TupleExplicitNamesOnAllMembersOrNone, "(int, string a)").WithLocation(6, 9),
                // (6,29): error CS8204: Tuple member names must all be provided, if any one is provided.
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_TupleExplicitNamesOnAllMembersOrNone, @"(b: 1, ""hello"", 2)").WithLocation(6, 29)
                );
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

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"4");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       67 (0x43)
  .maxstack  6
  .locals init (System.ValueTuple<int, (int c, (int e, int f) d)> V_0) //v
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.1
  IL_000a:  ldc.i4.2
  IL_000b:  ldc.i4.3
  IL_000c:  ldc.i4.4
  IL_000d:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0012:  newobj     ""System.ValueTuple<int, (int e, int f)>..ctor(int, (int e, int f))""
  IL_0017:  call       ""System.ValueTuple<int, (int c, (int e, int f) d)>..ctor(int, (int c, (int e, int f) d))""
  IL_001c:  ldloc.0
  IL_001d:  call       ""System.Collections.Generic.Dictionary<(int, int), (int a, (int c, (int e, int f) d) b)> C.Test<(int, int), (int a, (int c, (int e, int f) d) b)>((int, int), (int a, (int c, (int e, int f) d) b))""
  IL_0022:  ldc.i4.1
  IL_0023:  ldc.i4.2
  IL_0024:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0029:  callvirt   ""(int a, (int c, (int e, int f) d) b) System.Collections.Generic.Dictionary<(int, int), (int a, (int c, (int e, int f) d) b)>.this[(int, int)].get""
  IL_002e:  ldfld      ""(int c, (int e, int f) d) System.ValueTuple<int, (int c, (int e, int f) d)>.Item2""
  IL_0033:  ldfld      ""(int e, int f) System.ValueTuple<int, (int e, int f)>.Item2""
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

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.<>c__DisplayClass1_0<T>.<Test>b__0()", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""(T f1, T f2) C.<>c__DisplayClass1_0<T>.x""
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
            var verifier = CompileAndVerify(source, additionalRefs: new[] { MscorlibRef_v46 }, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"42", options: TestOptions.ReleaseExe);
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
    IL_001c:  stfld      ""(T f1, T f2) C.<Test>d__1<T>.<x>5__1""
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
    IL_0084:  ldflda     ""(T f1, T f2) C.<Test>d__1<T>.<x>5__1""
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
        public void LongTupleWithSubstitution()
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
        var x = (f1: 1, f2: 2, f3: 3, f4: 4, f5: 5, f6: 6, f7: 7, f8: a);

        await Task.Yield();

        return x.f8;
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            CompileAndVerify(source, expectedOutput: @"42", additionalRefs: new[] { MscorlibRef_v46 }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void TupleUsageWithoutTupleLibrary()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string) x = (1, ""hello"");
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (6,9): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (int, string) x = (1, "hello");
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(int, string)").WithArguments("System.ValueTuple`2").WithLocation(6, 9),
                // (6,27): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (int, string) x = (1, "hello");
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"(1, ""hello"")").WithArguments("System.ValueTuple`2").WithLocation(6, 27)
                );
        }

        [Fact]
        public void TupleUsageWithMissingTupleMembers()
        {
            var source = @"
namespace System
{
    public struct ValueTuple<T1, T2> { }
}

class C
{
    static void Main()
    {
        (int, int) x = (1, 2);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, assemblyName: "comp", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyEmitDiagnostics(
                // (11,24): error CS8205: Member '.ctor' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         (int, int) x = (1, 2);
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "(1, 2)").WithArguments(".ctor", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(11, 24)
                               );
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

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (6,24): error CS8203: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, b: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "a").WithLocation(6, 24),
                // (6,38): error CS8203: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, b: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "b").WithLocation(6, 38),
                // (6,50): error CS8203: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, b: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "b").WithLocation(6, 50)
               );
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

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (6,28): error CS8201: Tuple member name 'Item1' is only allowed at position 1.
                //         (int Item1, string Item1) x = (Item1: 1, Item1: "hello");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item1").WithArguments("Item1", "1").WithLocation(6, 28),
                // (6,50): error CS8201: Tuple member name 'Item1' is only allowed at position 1.
                //         (int Item1, string Item1) x = (Item1: 1, Item1: "hello");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item1").WithArguments("Item1", "1").WithLocation(6, 50),
                // (7,14): error CS8201: Tuple member name 'Item2' is only allowed at position 2.
                //         (int Item2, string Item2) y = (Item2: 1, Item2: "hello");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "2").WithLocation(7, 14),
                // (7,40): error CS8201: Tuple member name 'Item2' is only allowed at position 2.
                //         (int Item2, string Item2) y = (Item2: 1, Item2: "hello");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "2").WithLocation(7, 40)
                );
        }

        [Fact]
        public void TupleWithNonReservedNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int Item1, int Item01, int Item10) x = (Item01: 1, Item1: 2, Item10: 3);
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (6,37): error CS8201: Tuple member name 'Item10' is only allowed at position 10.
                //         (int Item1, int Item01, int Item10) x = (Item01: 1, Item1: 2, Item10: 3);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item10").WithArguments("Item10", "10").WithLocation(6, 37),
                // (6,61): error CS8201: Tuple member name 'Item1' is only allowed at position 1.
                //         (int Item1, int Item01, int Item10) x = (Item01: 1, Item1: 2, Item10: 3);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item1").WithArguments("Item1", "1").WithLocation(6, 61),
                // (6,71): error CS8201: Tuple member name 'Item10' is only allowed at position 10.
                //         (int Item1, int Item01, int Item10) x = (Item01: 1, Item1: 2, Item10: 3);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item10").WithArguments("Item10", "10").WithLocation(6, 71)
                );
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

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"0
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

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (6,24): error CS8203: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, c: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "a").WithLocation(6, 24),
                // (6,50): error CS8203: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, c: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "b").WithLocation(6, 50)
                );
        }

        [Fact]
        public void TupleWithReservedMemberNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: ""bad"", Item4: ""bad"", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: ""bad"");
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (6,28): error CS8201: Tuple member name 'Item3' is only allowed at position 3.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item3").WithArguments("Item3", "3").WithLocation(6, 28),
                // (6,42): error CS8201: Tuple member name 'Item2' is only allowed at position 2.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "2").WithLocation(6, 42),
                // (6,100): error CS8202: Tuple membername 'Rest' is disallowed at any position.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberNameAnyPosition, "Rest").WithArguments("Rest").WithLocation(6, 100),
                // (6,111): error CS8201: Tuple member name 'Item2' is only allowed at position 2.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "2").WithLocation(6, 111),
                // (6,125): error CS8201: Tuple member name 'Item4' is only allowed at position 4.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item4").WithArguments("Item4", "4").WithLocation(6, 125),
                // (6,189): error CS8202: Tuple membername 'Rest' is disallowed at any position.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberNameAnyPosition, "Rest").WithArguments("Rest").WithLocation(6, 189)
               );
        }

        [Fact]
        public void LongTupleDeclaration()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, int, int, int, int, int, int, string, int, int, int, int) x = (1, 2, 3, 4, 5, 6, 7, ""Alice"", 2, 3, 4, 5);
        System.Console.WriteLine($""{x.Item1} {x.Item2} {x.Item3} {x.Item4} {x.Item5} {x.Item6} {x.Item7} {x.Item8} {x.Item9} {x.Item10} {x.Item11} {x.Item12}"");
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var x = nodes.OfType<VariableDeclaratorSyntax>().First();

                Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, "
                    + "System.String, System.Int32, System.Int32, System.Int32, System.Int32) x",
                    model.GetDeclaredSymbol(x).ToTestDisplayString());
            };

            var verifier = CompileAndVerify(source, expectedOutput: @"1 2 3 4 5 6 7 Alice 2 3 4 5", additionalRefs: new[] { MscorlibRef }, sourceSymbolValidator: validator, parseOptions: TestOptions.Regular.WithTuplesFeature());
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void LongTupleDeclarationWithNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, int b, int c, int d, int e, int f, int g, string h, int i, int j, int k, int l) x = (1, 2, 3, 4, 5, 6, 7, ""Alice"", 2, 3, 4, 5);
        System.Console.WriteLine($""{x.a} {x.b} {x.c} {x.d} {x.e} {x.f} {x.g} {x.h} {x.i} {x.j} {x.k} {x.l}"");
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var x = nodes.OfType<VariableDeclaratorSyntax>().First();

                Assert.Equal("(System.Int32 a, System.Int32 b, System.Int32 c, System.Int32 d, System.Int32 e, System.Int32 f, System.Int32 g, "
                    + "System.String h, System.Int32 i, System.Int32 j, System.Int32 k, System.Int32 l) x",
                    model.GetDeclaredSymbol(x).ToTestDisplayString());
            };

            var verifier = CompileAndVerify(source, expectedOutput: @"1 2 3 4 5 6 7 Alice 2 3 4 5", additionalRefs: new[] { MscorlibRef }, sourceSymbolValidator: validator, parseOptions: TestOptions.Regular.WithTuplesFeature());
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void HugeTupleCreationParses()
        {
            StringBuilder b = new StringBuilder();
            b.Append("(");
            for (int i = 0; i < 3000; i++)
            {
                b.Append("1, ");
            }
            b.Append("1)");

            var source = @"
class C
{
    static void Main()
    {
        var x = " + b.ToString() + @";
    }
}
";
            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void HugeTupleDeclarationParses()
        {
            StringBuilder b = new StringBuilder();
            b.Append("(");
            for (int i = 0; i < 3000; i++)
            {
                b.Append("int, ");
            }
            b.Append("int)");

            var source = @"
class C
{
    static void Main()
    {
        " + b.ToString() + @" x;
    }
}
";
            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void GenericTupleWithoutTupleLibrary()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = M<int, bool>();
        System.Console.WriteLine($""{x.first} {x.second}"");
    }

    static (T1 first, T2 second) M<T1, T2>()
    {
        return (default(T1), default(T2));
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (10,12): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //     static (T1 first, T2 second) M<T1, T2>()
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(T1 first, T2 second)").WithArguments("System.ValueTuple`2").WithLocation(10, 12),
                // (12,16): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         return (default(T1), default(T2));
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(default(T1), default(T2))").WithArguments("System.ValueTuple`2").WithLocation(12, 16)
                );
        }

        [Fact]
        public void GenericTuple()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = M<int, bool>();
        System.Console.WriteLine($""{x.first} {x.second}"");
    }

    static (T1 first, T2 second) M<T1, T2>()
    {
        return (default(T1), default(T2));
    }
}
" + trivial2uple;
            var comp = CompileAndVerify(source, expectedOutput: @"0 False", parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void LongTupleCreation()
        {
            var source = trivial2uple + trivial3uple + trivalRemainingTuples + @"
class C
{
    static void Main()
    {
        var x = (1, 2, 3, 4, 5, 6, 7, ""Alice"", 2, 3, 4, 5, 6, 7, ""Bob"", 2, 3);
        System.Console.WriteLine($""{x.Item1} {x.Item2} {x.Item3} {x.Item4} {x.Item5} {x.Item6} {x.Item7} {x.Item8} {x.Item9} {x.Item10} {x.Item11} {x.Item12} {x.Item13} {x.Item14} {x.Item15} {x.Item16} {x.Item17}"");
    }
}
";

            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var node = nodes.OfType<TupleExpressionSyntax>().Single();

                Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, "
                     + "System.String, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, "
                     + "System.String, System.Int32, System.Int32)",
                     model.GetTypeInfo(node).Type.ToTestDisplayString());
            };

            var verifier = CompileAndVerify(source, expectedOutput: @"1 2 3 4 5 6 7 Alice 2 3 4 5 6 7 Bob 2 3", additionalRefs: new[] { MscorlibRef }, sourceSymbolValidator: validator, parseOptions: TestOptions.Regular.WithTuplesFeature());
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TupleInLambda()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Action<(int, string)> f = ((int, string) x) => System.Console.WriteLine($""{x.Item1} {x.Item2}"");
        f((42, ""Alice""));
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"42 Alice", parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void TupleWithNamesInLambda()
        {
            var source = @"
class C
{
    static void Main()
    {
        int a, b = 0;
        System.Action<(int, string)> f = ((int a, string b) x) => System.Console.WriteLine($""{x.a} {x.b}"");
        f((c: 42, d: ""Alice""));
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"42 Alice", parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void TupleInProperty()
        {
            var source = @"
class C
{
    static (int a, string b) P { get; set; }

    static void Main()
    {
        P = (42, ""Alice"");
        System.Console.WriteLine($""{P.a} {P.b}"");
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"42 Alice", parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void ExtensionMethodOnTuple()
        {
            var source = @"
static class C
{
    static void Extension(this (int a, string b) x)
    {
        System.Console.WriteLine($""{x.a} {x.b}"");
    }
    static void Main()
    {
        (42, ""Alice"").Extension();
    }
}
" + trivial2uple;

            // PROTOTYPE(tuples): this should probably fail with diagnostics. No extension methods on tuple types (but you can on ValueTuple)
            CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef }, expectedOutput: @"42 Alice", parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void TupleInOptionalParam()
        {
            var source = @"
class C
{
    void M(int x, (int a, string b) y = (42, ""Alice"")) { }
}
" + trivial2uple;

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature()).VerifyDiagnostics(
                // (4,41): error CS1736: Default parameter value for 'y' must be a compile-time constant
                //     void M(int x, (int a, string b) y = (42, "Alice"))
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @"(42, ""Alice"")").WithArguments("y").WithLocation(4, 41));
        }

        [Fact]
        public void TupleDefaultInOptionalParam()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M();
    }

    static void M((int a, string b) x = default((int, string)))
    {
        System.Console.WriteLine($""{x.a} {x.b}"");
    }
}
" + trivial2uple;
            CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef }, expectedOutput: @"0 ", parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void TupleAsNamedParam()
        {
            var source = @"
class C
{
    static void Main()
    {
        M(y : (42, ""Alice""), x : 1);
    }
    static void M(int x, (int a, string b) y)
    {
        System.Console.WriteLine($""{y.a} {y.Item2}"");
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"42 Alice", parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void LongTupleCreationWithNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: ""Alice"", i: 2, j: 3, k: 4, l: 5, m: 6, n: 7, o: ""Bob"", p: 2, q: 3);
        System.Console.WriteLine($""{x.a} {x.b} {x.c} {x.d} {x.e} {x.f} {x.g} {x.h} {x.i} {x.j} {x.k} {x.l} {x.m} {x.n} {x.o} {x.p} {x.q}"");
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var node = nodes.OfType<TupleExpressionSyntax>().Single();

                Assert.Equal("(System.Int32 a, System.Int32 b, System.Int32 c, System.Int32 d, System.Int32 e, System.Int32 f, System.Int32 g, "
                     + "System.String h, System.Int32 i, System.Int32 j, System.Int32 k, System.Int32 l, System.Int32 m, System.Int32 n, "
                     + "System.String o, System.Int32 p, System.Int32 q)",
                     model.GetTypeInfo(node).Type.ToTestDisplayString());
            };

            var verifier = CompileAndVerify(source, expectedOutput: @"1 2 3 4 5 6 7 Alice 2 3 4 5 6 7 Bob 2 3", additionalRefs: new[] { MscorlibRef }, sourceSymbolValidator: validator, parseOptions: TestOptions.Regular.WithTuplesFeature());
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void LongTupleWithArgumentEvaluation()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: PrintAndReturn(1), b: 2, c: 3, d: PrintAndReturn(4), e: 5, f: 6, g: PrintAndReturn(7), h: PrintAndReturn(""Alice""), i: 2, j: 3, k: 4, l: 5, m: 6, n: PrintAndReturn(7), o: PrintAndReturn(""Bob""), p: 2, q: PrintAndReturn(3));
    }

    static T PrintAndReturn<T>(T i)
    {
        System.Console.Write(i + "" "");
        return i;
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            var verifier = CompileAndVerify(source, expectedOutput: @"1 4 7 Alice 7 Bob 3", additionalRefs: new[] { MscorlibRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void LongTupleGettingRest()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: ""Alice"", i: 1);
        System.Console.WriteLine($""{x.Rest.Item1} {x.Rest.Item2}"");
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var node = nodes.OfType<MemberAccessExpressionSyntax>().Where(n => n.ToString() == "x.Rest").First();
                Assert.Equal("System.ValueTuple<System.String, System.Int32>", model.GetTypeInfo(node).Type.ToTestDisplayString());
            };

            var verifier = CompileAndVerify(source, expectedOutput: @"Alice 1", additionalRefs: new[] { MscorlibRef }, sourceSymbolValidator: validator, parseOptions: TestOptions.Regular.WithTuplesFeature());
            verifier.VerifyDiagnostics();
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

            var comp = CompileAndVerify(source, expectedOutput: @"{1, hello}", parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void DistinctTupleTypesInCompilation()
        {
            var source1 = @"
public class C1
{
    public static (int a, int b) M()
    {
        return (1, 2);
    }
}
" + trivial2uple;

            var source2 = @"
public class C2
{
    public static (int c, int d) M()
    {
        return (3, 4);
    }
}
" + trivial2uple;

            var source = @"
class C3
{
    public static void Main()
    {
        System.Console.Write(C1.M().Item1 + "" "");
        System.Console.Write(C1.M().a + "" "");
        System.Console.Write(C1.M().Item2 + "" "");
        System.Console.Write(C1.M().b + "" "");
        System.Console.Write(C2.M().Item1 + "" "");
        System.Console.Write(C2.M().c + "" "");
        System.Console.Write(C2.M().Item2 + "" "");
        System.Console.Write(C2.M().d);
    }
}
";
            var comp1 = CreateCompilationWithMscorlib(source1, parseOptions: TestOptions.Regular.WithTuplesFeature());
            var comp2 = CreateCompilationWithMscorlib(source2, parseOptions: TestOptions.Regular.WithTuplesFeature());
            var comp = CompileAndVerify(source, expectedOutput: @"1 1 2 2 3 3 4 4", additionalRefs: new[] { new CSharpCompilationReference(comp1), new CSharpCompilationReference(comp2) }, parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void DistinctTupleTypesInCompilationCannotAssign()
        {
            var source1 = @"
public class C1
{
    public static (int a, int b) M()
    {
        return (1, 2);
    }
}
" + trivial2uple;

            var source2 = @"
public class C2
{
    public static (int c, int d) M()
    {
        return (3, 4);
    }
}
" + trivial2uple;

            var source = @"
class C3
{
    public static void Main()
    {
        var x = C1.M();
        x = C2.M();
    }
}
";
            var comp1 = CreateCompilationWithMscorlib(source1, parseOptions: TestOptions.Regular.WithTuplesFeature());
            var comp2 = CreateCompilationWithMscorlib(source2, parseOptions: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(source, references: new[] { new CSharpCompilationReference(comp1), new CSharpCompilationReference(comp2) }, parseOptions: TestOptions.Regular.WithTuplesFeature());

            // PROTOTYPE(tuples) this error is misleading or worse.
            comp.VerifyDiagnostics(
                // (7,13): error CS0029: Cannot implicitly convert type '(int c, int d)' to '(int a, int b)'
                //         x = C2.M();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "C2.M()").WithArguments("(int c, int d)", "(int a, int b)").WithLocation(7, 13)
                );
        }

        [Fact]
        public void AmbiguousTupleTypesForCreation()
        {
            var source = @"
class C3
{
    public static void Main()
    {
        var x = (1, 1);
    }
}
";
            var comp1 = CreateCompilationWithMscorlib(trivial2uple, assemblyName: "comp1", parseOptions: TestOptions.Regular.WithTuplesFeature());
            var comp2 = CreateCompilationWithMscorlib(trivial2uple, parseOptions: TestOptions.Regular.WithTuplesFeature());

            var comp = CompileAndVerify(source, additionalRefs: new[] { new CSharpCompilationReference(comp1), new CSharpCompilationReference(comp2) }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // warning CS1685: The predefined type 'ValueTuple<T1, T2>' is defined in multiple assemblies in the global alias; using definition from 'comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
                Diagnostic(ErrorCode.WRN_MultiplePredefTypes).WithArguments("System.ValueTuple<T1, T2>", "comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1)
                                );
        }

        [Fact]
        public void AmbiguousTupleTypesForDeclaration()
        {
            var source = @"
class C3
{
    public void M((int, int) x) { }
}
";
            var comp1 = CreateCompilationWithMscorlib(trivial2uple, assemblyName: "comp1", parseOptions: TestOptions.Regular.WithTuplesFeature());
            var comp2 = CreateCompilationWithMscorlib(trivial2uple, parseOptions: TestOptions.Regular.WithTuplesFeature());

            var comp = CompileAndVerify(source, additionalRefs: new[] { new CSharpCompilationReference(comp1), new CSharpCompilationReference(comp2) }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // warning CS1685: The predefined type 'ValueTuple<T1, T2>' is defined in multiple assemblies in the global alias; using definition from 'comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
                Diagnostic(ErrorCode.WRN_MultiplePredefTypes).WithArguments("System.ValueTuple<T1, T2>", "comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1)
                                );
        }

        [Fact]
        public void LocalTupleTypeWinsWhenTupleTypesInCompilation()
        {
            var source1 = @"
public class C1
{
    public static (int a, int b) M()
    {
        return (1, 2);
    }
}
" + trivial2uple;

            var source2 = @"
public class C2
{
    public static (int c, int d) M()
    {
        return (3, 4);
    }
}
" + trivial2uple;

            var source = @"
class C3
{
    public static void Main()
    {
        System.Console.Write(C1.M().Item1 + "" "");
        System.Console.Write(C1.M().a + "" "");
        System.Console.Write(C1.M().Item2 + "" "");
        System.Console.Write(C1.M().b + "" "");
        System.Console.Write(C2.M().Item1 + "" "");
        System.Console.Write(C2.M().c + "" "");
        System.Console.Write(C2.M().Item2 + "" "");
        System.Console.Write(C2.M().d + "" "");

        var x = (e: 5, f: 6);
        System.Console.Write(x.Item1 + "" "");
        System.Console.Write(x.e + "" "");
        System.Console.Write(x.Item2 + "" "");
        System.Console.Write(x.f + "" "");
        System.Console.Write(x.GetType().Assembly == typeof(C3).Assembly);
    }
}
" + trivial2uple;

            var comp1 = CreateCompilationWithMscorlib(source1, parseOptions: TestOptions.Regular.WithTuplesFeature());
            var comp2 = CreateCompilationWithMscorlib(source2, parseOptions: TestOptions.Regular.WithTuplesFeature());
            var comp = CompileAndVerify(source, expectedOutput: @"1 1 2 2 3 3 4 4 5 5 6 6 True", additionalRefs: new[] { new CSharpCompilationReference(comp1), new CSharpCompilationReference(comp2) }, parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void UnderlyingTypeMemberWithWrongSignature()
        {
            string source = @"
class C
{
    static void M()
    {
        var x = (""Alice"", ""Bob"");
        System.Console.WriteLine($""{x.Item1}"");
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public int Item1; // Not T1
        public int Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = 1;
            this.Item2 = 2;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, assemblyName: "comp", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (7,37): error CS8205: Member 'Item1' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine($"{x.Item1}");
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "x.Item1").WithArguments("Item1", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 37)
                );
        }

        [Fact]
        public void ImplementTupleInterface()
        {
            string source = @"
public interface I
{
    (int, int) M((string, string) a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M((""Alice"", ""Bob""));
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    public (int, int) M((string, string) a)
    {
        return (a.Item1.Length, a.Item2.Length);
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            var comp = CompileAndVerify(source, expectedOutput: @"5 3", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplementTupleInterfaceWithDifferentNames()
        {
            string source = @"
public interface I
{
    (int i1, int i2) M((string s1, string s2) a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M((""Alice"", ""Bob""));
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    public (int i3, int i4) M((string s3, string s4) a)
    {
        return (a.Item1.Length, a.Item2.Length);
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            var comp = CompileAndVerify(source, expectedOutput: @"5 3", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplementLongTupleInterface()
        {
            string source = @"
public interface I
{
    (int, int, int, int, int, int, int, int) M((int, int, int, int, int, int, int, int) a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M((1, 2, 3, 4, 5, 6, 7, 8));
        System.Console.WriteLine($""{r.Item1} {r.Item7} {r.Item8}"");
    }

    public (int, int, int, int, int, int, int, int) M((int, int, int, int, int, int, int, int) a)
    {
        return a;
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            var comp = CompileAndVerify(source, expectedOutput: @"1 7 8", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplementTupleInterfaceWithValueTuple()
        {
            string source = @"
public interface I
{
    (int i1, int i2) M((string, string) a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M((""Alice"", ""Bob""));
        System.Console.WriteLine($""{r.i1} {r.i2}"");
    }

    public System.ValueTuple<int, int> M(System.ValueTuple<string, string> a)
    {
        return (a.Item1.Length, a.Item2.Length);
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            var comp = CompileAndVerify(source, expectedOutput: @"5 3", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplementValueTupleInterfaceWithTuple()
        {
            string source = @"
public interface I
{
    System.ValueTuple<int, int> M(System.ValueTuple<string, string> a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M((""Alice"", ""Bob""));
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    public (int, int) M((string, string) a)
    {
        return new System.ValueTuple<int, int>(a.Item1.Length, a.Item2.Length);
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            var comp = CompileAndVerify(source, expectedOutput: @"5 3", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void OverrideTupleMethodWithDifferentNames()
        {
            string source = @"
class C
{
    public virtual (int a, int b) M((int c, int d) x)
    {
        throw new System.Exception();
    }
}
class D : C
{
    static void Main()
    {
        C c = new D();
        var r = c.M((1, 2));
        System.Console.WriteLine($""{r.a} {r.b}"");
    }

    public override (int e, int f) M((int g, int h) y)
    {
        return y;
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"1 2", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NewTupleMethodWithDifferentNames()
        {
            string source = @"
class C
{
    public virtual (int a, int b) M((int c, int d) x)
    {
        System.Console.WriteLine(""base"");
        return x;
    }
}
class D : C
{
    static void Main()
    {
        D d = new D();
        d.M((1, 2));
        C c = d;
        c.M((1, 2));
    }

    public new (int e, int f) M((int g, int h) y)
    {
        System.Console.Write(""new "");
        return y;
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"new base", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DuplicateTupleMethodsNotAllowed()
        {
            string source = @"
class C
{
    public (int, int) M((string, string) a)
    {
        return new System.ValueTuple<int, int>(a.Item1.Length, a.Item2.Length);
    }

    public System.ValueTuple<int, int> M(System.ValueTuple<string, string> a)
    {
        return (a.Item1.Length, a.Item2.Length);
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (9,40): error CS0111: Type 'C' already defines a member called 'M' with the same parameter types
                //     public System.ValueTuple<int, int> M(System.ValueTuple<string, string> a)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "C").WithLocation(9, 40)
                );
        }

        [Fact]
        public void TupleArrays()
        {
            string source = @"
public interface I
{
    System.ValueTuple<int, int>[] M((int, int)[] a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M(new [] { new System.ValueTuple<int, int>(1, 2) });
        System.Console.WriteLine($""{r[0].Item1} {r[0].Item2}"");
    }

    public (int, int)[] M(System.ValueTuple<int, int>[] a)
    {
        return new [] { (a[0].Item1, a[0].Item2) };
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"1 2", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TupleRef()
        {
            string source = @"
class C
{
    static void Main()
    {
        var r = (1, 2);
        M(ref r);
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    static void M(ref (int, int) a)
    {
        System.Console.WriteLine($""{a.Item1} {a.Item2}"");
        a = (3, 4);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput:
@"1 2
3 4");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TupleOut()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int, int) r;
        M(out r);
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    static void M(out (int, int) a)
    {
        a = (1, 2);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"1 2", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TupleTypeArgs()
        {
            string source = @"
class C
{
    static void Main()
    {
        var a = (1, ""Alice"");
        var r = M<int, string>(a);
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    static (T1, T2) M<T1, T2>((T1, T2) a)
    {
        return a;
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"1 Alice", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullableTuple()
        {
            string source = @"
class C
{
    static void Main()
    {
        M((1, ""Alice""));
    }

    static void M((int, string)? a)
    {
        System.Console.WriteLine($""{a.HasValue} {a.Value.Item1} {a.Value.Item2}"");
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"True 1 Alice", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Tuple2To8Members()
        {
            var source = trivial2uple + trivial3uple + trivalRemainingTuples + @"
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
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item1);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item2);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item3);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item4);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item5);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item6);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item7);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item8);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "12345678901234567890123456789012345", parseOptions: TestOptions.Regular.WithTuplesFeature());
        }

        [Fact]
        public void TupleTargetTypeTwice()
        {
            var source = @"
class C
{
    static void Main()
    {
        // this works
        // (short, string) x1 = (1, ""hello"");
        // this does not
        (short, string) x2 = ((byte, string))(1, ""hello"");
    }
}
" + trivial2uple;

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature()).VerifyDiagnostics(
                // (9,30): error CS0029: Cannot implicitly convert type '(byte, string)' to '(short, string)'
                //         (short, string) x2 = ((byte, string))(1, "hello");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"((byte, string))(1, ""hello"")").WithArguments("(byte, string)", "(short, string)").WithLocation(9, 30)
            );
        }

        [Fact]
        public void TupleTargetTypeLambda()
        {
            var source = @"

using System;

class C
{
    static void Test(Func<Func<(short, short)>> d)
    {
        Console.WriteLine(""short"");
    }

    static void Test(Func<Func<(byte, byte)>> d)
    {
        Console.WriteLine(""byte"");
    }

    static void Main()
    {
        // this works
        Test( ()=>()=>((byte, byte))(1,1)) ;

        // this does not
        Test(()=>()=>(1,1));
    }
}
" + trivial2uple;

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature()).VerifyDiagnostics(
                // (23,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.Test(Func<Func<(short, short)>>)' and 'C.Test(Func<Func<(byte, byte)>>)'
                //         Test(()=>()=>(1,1));
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("C.Test(System.Func<System.Func<(short, short)>>)", "C.Test(System.Func<System.Func<(byte, byte)>>)").WithLocation(23, 9)
            );
        }

        [Fact]
        public void TupleTargetTypeLambda1()
        {
            var source = @"

using System;

class C
{
    static void Test(Func<(Func<short>, int)> d)
    {
        Console.WriteLine(""short"");
    }

    static void Test(Func<(Func<byte>, int)> d)
    {
        Console.WriteLine(""byte"");
    }

    static void Main()
    {
        // this works
        Test(()=>(()=>(byte)1, 1));

        // this does not
        Test(()=>(()=>1, 1));
    }
}
" + trivial2uple;

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature()).VerifyDiagnostics(
                // (23,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.Test(Func<(Func<short>, int)>)' and 'C.Test(Func<(Func<byte>, int)>)'
                //         Test(()=>(()=>1, 1));
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("C.Test(System.Func<(System.Func<short>, int)>)", "C.Test(System.Func<(System.Func<byte>, int)>)").WithLocation(23, 9)
            );
        }

        [Fact]
        public void TargetTypingOverload01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((null, null));
        Test((1, 1));
        Test((()=>7, ()=>8), 2);
    }

    static void Test<T>((T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<T>, Func<T>) x, T y)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1().ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"
second
first
third
7
");
        }

        [Fact]
        public void TargetTypingOverload02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((()=>7, ()=>8));
    }

    static void Test<T>((T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<T>, Func<T>) x)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1().ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"
third
7
");
        }

        [Fact]
        public void TargetTypingNullable01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = M1();
        Test(x);
    }

    static (int a, double b)? M1()
    {
        return (1, 2);
    }

    static void Test<T>(T arg)
    {
        System.Console.WriteLine(typeof(T));
        System.Console.WriteLine(arg);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"
System.Nullable`1[System.ValueTuple`2[System.Int32,System.Double]]
{1, 2}
");
        }

        [Fact]
        public void TargetTypingOverload01Long()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((null, null, null, null, null, null, null, null, null, null));
        Test((1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
        Test((()=>7, ()=>8, ()=>8, ()=>8, ()=>8, ()=>8, ()=>8, ()=>8, ()=>8, ()=>8), 2);
    }

    static void Test<T>((T, T, T, T, T, T, T, T, T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object, object, object, object, object, object, object, object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<T>, Func<T>, Func<T>, Func<T>, Func<T>, Func<T>, Func<T>, Func<T>, Func<T>, Func<T>) x, T y)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1().ToString());
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"
second
first
third
7
");
        }

        [Fact]
        public void TargetTypingNullable02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = M1();
        Test(x);
    }

    static (int a, string b)? M1()
    {
        return (1, null);
    }

    static void Test<T>(T arg)
    {
        System.Console.WriteLine(typeof(T));
        System.Console.WriteLine(arg);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"
System.Nullable`1[System.ValueTuple`2[System.Int32,System.String]]
{1, }
");
        }

        [Fact]
        public void TargetTypingNullable02Long()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = M1();
        System.Console.WriteLine(x?.a);
        System.Console.WriteLine(x?.a8);
        Test(x);
    }

    static (int a, string b, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8)? M1()
    {
        return (1, null, 1, 2, 3, 4, 5, 6, 7, 8);
    }

    static void Test<T>(T arg)
    {
        System.Console.WriteLine(arg);
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"
1
8
System.ValueTuple`8[System.Int32,System.String,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.ValueTuple`3[System.Int32,System.Int32,System.Int32]]
");
        }

        [Fact]
        public void TargetTypingNullableOverload()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((null, null, null, null, null, null, null, null, null, null));
        Test((""a"", ""a"", ""a"", ""a"", ""a"", ""a"", ""a"", ""a"", ""a"", ""a""));
        Test((1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
    }

    static void Test((string, string, string, string, string, string, string, string, string, string) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((string, string, string, string, string, string, string, string, string, string)? x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test((int, int, int, int, int, int, int, int, int, int)? x)
    {
        System.Console.WriteLine(""third"");
    }

    static void Test((int, int, int, int, int, int, int, int, int, int) x)
    {
        System.Console.WriteLine(""fourth"");
    }
}
" + trivial2uple + trivial3uple + trivalRemainingTuples;

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithTuplesFeature(), expectedOutput: @"
first
first
fourth
");
        }

        [Fact]
        public void TupleConversion01()
        {
            var source = @"

class C
{
    static void Main()
    {
        // error must mention   (long c, long d)
        (int a, int b) x1 = ((long c, long d))(e: 1, f:2);
        // error must mention   (int c, long d)
        (short a, short b) x2 = ((int c, int d))(e: 1, f:2);

        // error must mention   (int e, string f)
        (int a, int b) x3 = ((long c, long d))(e: 1, f:""qq"");
    }
}
" + trivial2uple;

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature()).VerifyDiagnostics(
                // (8,29): error CS0029: Cannot implicitly convert type '(long c, long d)' to '(int a, int b)'
                //         (int a, int b) x1 = ((long c, long d))(e: 1, f:2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "((long c, long d))(e: 1, f:2)").WithArguments("(long c, long d)", "(int a, int b)").WithLocation(8, 29),
                // (10,33): error CS0029: Cannot implicitly convert type '(int c, int d)' to '(short a, short b)'
                //         (short a, short b) x2 = ((int c, int d))(e: 1, f:2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "((int c, int d))(e: 1, f:2)").WithArguments("(int c, int d)", "(short a, short b)").WithLocation(10, 33),
                // (13,29): error CS0030: Cannot convert type '(int e, string f)' to '(long c, long d)'
                //         (int a, int b) x3 = ((long c, long d))(e: 1, f:"qq");
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"((long c, long d))(e: 1, f:""qq"")").WithArguments("(int e, string f)", "(long c, long d)").WithLocation(13, 29)
            );
        }

        // PROTOTYPE(tuples): this test is for a precedent reference 
        //                    it does not test tuples and should be removed or moved to appropriate location
        [Fact]
        public void InterpolatedConvertedType()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.IFormattable x = (System.IFormattable)$""qq {1} qq"";
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<InterpolatedStringExpressionSyntax>().Single();

            Assert.Equal(@"$""qq {1} qq""", node.ToString());
            Assert.Equal("System.String", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("System.String", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));
            Assert.Equal(Conversion.Identity, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("System.IFormattable x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        // PROTOTYPE(tuples): this test is for a precedent reference 
        //                    it does not test tuples and should be removed or moved to appropriate location
        [Fact]
        public void InterpolatedConvertedTypeInSource()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.IFormattable x = $""qq {1} qq"";
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<InterpolatedStringExpressionSyntax>().Single();

            Assert.Equal(@"$""qq {1} qq""", node.ToString());
            Assert.Equal("System.String", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("System.IFormattable", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.InterpolatedString, model.GetConversion(node));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("System.IFormattable x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType01()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b)? x = (e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)?", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNullable, model.GetConversion(node));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType01insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b)? x = ((short c, string d)?)(e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitTuple, model.GetConversion(node));

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((short c, string d)?)(e: 1, f: ""hello"")
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)?", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNullable, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType02()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b)? x = (e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)?", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNullable, model.GetConversion(node));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType02insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b)? x = ((short c, string d))(e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitTuple, model.GetConversion(node));

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((short c, string d))(e: 1, f: ""hello"")
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)?", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNullable, model.GetConversion(node.Parent));


            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }


        [Fact]
        public void TupleConvertedType03()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b)? x = (e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 a, System.String b)?", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNullable, model.GetConversion(node));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int32 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType03insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b)? x = ((int c, string d)?)(e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((int c, string d)?)(e: 1, f: ""hello"")
            Assert.Equal("(System.Int32 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 a, System.String b)?", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNullable, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int32 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType04()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b)? x = ((int c, string d))(e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((int c, string d))(e: 1, f: ""hello"")
            Assert.Equal("(System.Int32 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 a, System.String b)?", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitNullable, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int32 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType05()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b) x = (e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 a, System.String b)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int32 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType05insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b) x = ((int c, string d))(e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int32 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType06()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = (e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitTuple, model.GetConversion(node));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType06insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = ((short c, string d))(e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitTuple, model.GetConversion(node));

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((short c, string d))(e: 1, f: ""hello"")
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedTypeNull01()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = (e: 1, f: null);
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: null)", node.ToString());
            Assert.Null(model.GetTypeInfo(node).Type);
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitTuple, model.GetConversion(node));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedTypeNull01insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = ((short c, string d))(e: 1, f: null);
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: null)", node.ToString());
            Assert.Null(model.GetTypeInfo(node).Type);
            Assert.Equal("(System.Int16 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitTuple, model.GetConversion(node));

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((short c, string d))(e: 1, f: null)
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedTypeUDC01()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = (e: 1, f: new C1(""qq""));
        System.Console.WriteLine(x.ToString());
    }

    class C1
    {
        private string s;

        public C1(string arg)
        {
            s = arg + 1;
        }

        public static implicit operator string (C1 arg)
        {
            return arg.s;
        }
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree, options: TestOptions.ReleaseExe);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: new C1(""qq""))", node.ToString());
            Assert.Equal("(System.Int32 e, C.C1 f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitTuple, model.GetConversion(node));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "{1, qq1}");
        }

        [Fact]
        public void TupleConvertedTypeUDC01insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = ((short c, string d))(e: 1, f: new C1(""qq""));
        System.Console.WriteLine(x.ToString());
    }

    class C1
    {
        private string s;

        public C1(string arg)
        {
            s = arg + 1;
        }

        public static implicit operator string (C1 arg)
        {
            return arg.s;
        }
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular.WithTuplesFeature());
            var comp = CreateCompilationWithMscorlib(tree, options: TestOptions.ReleaseExe);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: new C1(""qq""))", node.ToString());
            Assert.Equal("(System.Int32 e, C.C1 f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitTuple, model.GetConversion(node));

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((short c, string d))(e: 1, f: null)
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "{1, qq1}");
        }

        [Fact]
        public void Inference01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((null, null));
        Test((1, 1));
        Test((()=>7, ()=>8), 2);
    }

    static void Test<T>((T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<T>, Func<T>) x, T y)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1().ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
second
first
third
7
");
        }

        [Fact]
        public void Inference02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((()=>7, ()=>8));
    }

    static void Test<T>((T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<T>, Func<T>) x)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1().ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
third
7
");
        }

        [Fact]
        public void Inference03()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test(((x)=>x, (x)=>x));
    }

    static void Test<T>((T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<int, T>, Func<T, T>) x)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1(5).ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
third
5
");
        }

        [Fact]
        public void Inference04()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test( (x)=>x.y );
        Test( (x)=>x.bob );
    }

    static void Test<T>( Func<(byte x, byte y), T> x)
    {
        System.Console.WriteLine(""first"");
        System.Console.WriteLine(x((2,3)).ToString());
    }

    static void Test<T>( Func<(int alice, int bob), T> x)
    {
        System.Console.WriteLine(""second"");
        System.Console.WriteLine(x((4,5)).ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
first
3
second
5
");
        }

        [Fact]
        public void Inference05()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test( ((x)=>x.x, (x)=>x.Item2) );
        Test( ((x)=>x.bob, (x)=>x.Item1) );
    }

    static void Test<T>( (Func<(byte x, byte y), T> f1, Func<(int, int), T> f2) x)
    {
        System.Console.WriteLine(""first"");
        System.Console.WriteLine(x.f1((2,3)).ToString());
        System.Console.WriteLine(x.f2((2,3)).ToString());
    }

    static void Test<T>( (Func<(int alice, int bob), T> f1, Func<(int, int), T> f2) x)
    {
        System.Console.WriteLine(""second"");
        System.Console.WriteLine(x.f1((4,5)).ToString());
        System.Console.WriteLine(x.f2((4,5)).ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
first
2
3
second
5
4
");
        }

        [Fact(Skip = "PROTOTYPE(tuples): this should work, fix overload resolution]")]
        public void Inference06()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        M1((() => ""qq"", null));
    }

    static void M1((Func<object> f, object o) a)
    {
        System.Console.WriteLine(1);
    }

    static void M1((Func<string> f, object o) a)
    {
        System.Console.WriteLine(2);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
2
");
        }

        [Fact]
        public void Inference07()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Test((x) => (x, x), (t) => 1);
        Test1((x) => (x, x), (t) => 1);
        Test2((a: 1, b: 2), (t) => (t.a, t.b));
    }

    static void Test<U>(Func<int, ValueTuple<U, U>> f1, Func<ValueTuple<U, U>, int> f2)
    {
        System.Console.WriteLine(f2(f1(1)));
    }
    static void Test1<U>(Func<int, (U, U)> f1, Func<(U, U), int> f2)
    {
        System.Console.WriteLine(f2(f1(1)));
    }
    static void Test2<U, T>(U f1, Func<U, (T x, T y)> f2)
    {
        System.Console.WriteLine(f2(f1).y);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
1
1
2
");
        }
    }
}