// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.Tuples)]
    public class CodeGenDeconstructTests : CSharpTestBase
    {
        private static readonly MetadataReference[] s_valueTupleRefs = new[] { SystemRuntimeFacadeRef, ValueTupleRef };

        const string commonSource =
@"public class Pair<T1, T2>
{
    T1 item1;
    T2 item2;

    public Pair(T1 item1, T2 item2)
    {
        this.item1 = item1;
        this.item2 = item2;
    }

    public void Deconstruct(out T1 item1, out T2 item2)
    {
        System.Console.WriteLine($""Deconstructing {ToString()}"");
        item1 = this.item1;
        item2 = this.item2;
    }

    public override string ToString() { return $""({item1.ToString()}, {item2.ToString()})""; }
}

public static class Pair
{
    public static Pair<T1, T2> Create<T1, T2>(T1 item1, T2 item2) { return new Pair<T1, T2>(item1, item2); }
}

public class Integer
{
    public int state;
    public override string ToString() { return state.ToString(); }
    public Integer(int i) { state = i; }
    public static implicit operator LongInteger(Integer i) { System.Console.WriteLine($""Converting {i}""); return new LongInteger(i.state); }
}

public class LongInteger
{
    long state;
    public LongInteger(long l) { state = l; }
    public override string ToString() { return state.ToString(); }
}";

        [Fact]
        public void SimpleAssign()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C();
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var lhs = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().First();
                Assert.Equal(@"(x, y)", lhs.ToString());
                Assert.Equal("(System.Int64, System.String)", model.GetTypeInfo(lhs).Type.ToTestDisplayString());
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (long V_0, //x
                string V_1, //y
                int V_2,
                string V_3,
                string V_4)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_2
  IL_0007:  ldloca.s   V_3
  IL_0009:  call       ""void C.Deconstruct(out int, out string)""
  IL_000e:  ldloc.2
  IL_000f:  conv.i8
  IL_0010:  ldloc.3
  IL_0011:  stloc.s    V_4
  IL_0013:  dup
  IL_0014:  stloc.0
  IL_0015:  ldloc.s    V_4
  IL_0017:  stloc.1
  IL_0018:  pop
  IL_0019:  ldloc.0
  IL_001a:  box        ""long""
  IL_001f:  ldstr      "" ""
  IL_0024:  ldloc.1
  IL_0025:  call       ""string string.Concat(object, object, object)""
  IL_002a:  call       ""void System.Console.WriteLine(string)""
  IL_002f:  ret
}");
        }

        [Fact]
        [WorkItem(13632, "https://github.com/dotnet/roslyn/issues/13632")]
        public void SimpleAssignWithoutConversion()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y;

        (x, y) = new C();
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (int V_0, //x
                string V_1, //y
                int V_2,
                string V_3,
                string V_4)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_2
  IL_0007:  ldloca.s   V_3
  IL_0009:  call       ""void C.Deconstruct(out int, out string)""
  IL_000e:  ldloc.2
  IL_000f:  ldloc.3
  IL_0010:  stloc.s    V_4
  IL_0012:  dup
  IL_0013:  stloc.0
  IL_0014:  ldloc.s    V_4
  IL_0016:  stloc.1
  IL_0017:  pop
  IL_0018:  ldloc.0
  IL_0019:  box        ""int""
  IL_001e:  ldstr      "" ""
  IL_0023:  ldloc.1
  IL_0024:  call       ""string string.Concat(object, object, object)""
  IL_0029:  call       ""void System.Console.WriteLine(string)""
  IL_002e:  ret
}");
        }

        [Fact]
        public void DeconstructMethodAmbiguous()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;
        (x, y) = new C();
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }

    public void Deconstruct(out int a)
    {
        a = 2;
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructCanHaveReturnType()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C();
        System.Console.WriteLine(x + "" "" + y);
    }

    public int Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
        return 42;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyExecutionOrder_Deconstruct()
        {
            string source = @"
using System;
class C
{
    int x { set { Console.WriteLine($""setX""); } }
    int y { set { Console.WriteLine($""setY""); } }

    C getHolderForX() { Console.WriteLine(""getHolderforX""); return this; }
    C getHolderForY() { Console.WriteLine(""getHolderforY""); return this; }
    C getDeconstructReceiver() { Console.WriteLine(""getDeconstructReceiver""); return this; }

    static void Main()
    {
        C c = new C();
        (c.getHolderForX().x, c.getHolderForY().y) = c.getDeconstructReceiver();
    }
    public void Deconstruct(out D1 x, out D2 y) { x = new D1(); y = new D2(); Console.WriteLine(""Deconstruct""); }
}
class D1
{
    public static implicit operator int(D1 d) { Console.WriteLine(""Conversion1""); return 1; }
}
class D2
{
    public static implicit operator int(D2 d) { Console.WriteLine(""Conversion2""); return 2; }
}
";

            string expected =
@"getHolderforX
getHolderforY
getDeconstructReceiver
Deconstruct
Conversion1
Conversion2
setX
setY
";
            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyExecutionOrder_TupleLiteral()
        {
            string source = @"
using System;
class C
{
    int x { set { Console.WriteLine($""setX""); } }
    int y { set { Console.WriteLine($""setY""); } }

    C getHolderForX() { Console.WriteLine(""getHolderforX""); return this; }
    C getHolderForY() { Console.WriteLine(""getHolderforY""); return this; }

    static void Main()
    {
        C c = new C();
        (c.getHolderForX().x, c.getHolderForY().y) = (new D1(), new D2());
    }
}
class D1
{
    public D1() { Console.WriteLine(""Constructor1""); }
    public static implicit operator int(D1 d) { Console.WriteLine(""Conversion1""); return 1; }
}
class D2
{
    public D2() { Console.WriteLine(""Constructor2""); }
    public static implicit operator int(D2 d) { Console.WriteLine(""Conversion2""); return 2; }
}
";

            string expected =
@"getHolderforX
getHolderforY
Constructor1
Conversion1
Constructor2
Conversion2
setX
setY
";
            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DifferentVariableKinds()
        {
            string source = @"
class C
{
    int[] ArrayIndexer = new int[1];

    string property;
    string Property { set { property = value; } }

    string AutoProperty { get; set; }

    static void Main()
    {
        C c = new C();
        (c.ArrayIndexer[0], c.Property, c.AutoProperty) = new C();
        System.Console.WriteLine(c.ArrayIndexer[0] + "" "" + c.property + "" "" + c.AutoProperty);
    }

    public void Deconstruct(out int a, out string b, out string c)
    {
        a = 1;
        b = ""hello"";
        c = ""world"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello world", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Dynamic()
        {
            string source = @"
class C
{
    dynamic Dynamic1;
    dynamic Dynamic2;

    static void Main()
    {
        C c = new C();
        (c.Dynamic1, c.Dynamic2) = c;
        System.Console.WriteLine(c.Dynamic1 + "" "" + c.Dynamic2);
    }

    public void Deconstruct(out int a, out dynamic b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef, CSharpRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructInterfaceOnStruct()
        {
            string source = @"
interface IDeconstructable
{
    void Deconstruct(out int a, out string b);
}

struct C : IDeconstructable
{
    string state;

    static void Main()
    {
        int x;
        string y;
        IDeconstructable c = new C() { state = ""initial"" };
        System.Console.Write(c);

        (x, y) = c;
        System.Console.WriteLine("" "" + c + "" "" + x + "" "" + y);
    }

    void IDeconstructable.Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
        state = ""modified"";
    }

    public override string ToString() { return state; }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "initial modified 1 hello", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef, CSharpRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructMethodHasParams2()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C();
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b, params int[] c) // not a Deconstruct operator
    {
        a = 1;
        b = ""ignored"";
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 2;
        b = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "2 hello", additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void OutParamsDisallowed()
        {
            string source = @"
class C
{
    public void Deconstruct(out int a, out string b, out params int[] c)
    {
        a = 1;
        b = ""ignored"";
        c = new[] { 2, 2 };
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (4,58): error CS1108: A parameter cannot have all the specified modifiers; there are too many modifiers on the parameter
                //     public void Deconstruct(out int a, out string b, out params int[] c)
                Diagnostic(ErrorCode.ERR_MultiParamMod, "params").WithLocation(4, 58)
                );
        }

        [Fact]
        public void DeconstructMethodHasArglist2()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C();
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }

    public void Deconstruct(out int a, out string b, __arglist) // not a Deconstruct operator
    {
        a = 2;
        b = ""ignored"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef, CSharpRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DifferentStaticVariableKinds()
        {
            string source = @"
class C
{
    static int[] ArrayIndexer = new int[1];

    static string property;
    static string Property { set { property = value; } }

    static string AutoProperty { get; set; }

    static void Main()
    {
        (C.ArrayIndexer[0], C.Property, C.AutoProperty) = new C();
        System.Console.WriteLine(C.ArrayIndexer[0] + "" "" + C.property + "" "" + C.AutoProperty);
    }

    public void Deconstruct(out int a, out string b, out string c)
    {
        a = 1;
        b = ""hello"";
        c = ""world"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello world", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DifferentVariableRefKinds()
        {
            string source = @"
class C
{
    static void Main()
    {
        long a = 1;
        int b;
        C.M(ref a, out b);
        System.Console.WriteLine(a + "" "" + b);
    }

    static void M(ref long a, out int b)
    {
        (a, b) = new C();
    }

    public void Deconstruct(out int x, out byte y)
    {
        x = 2;
        y = (byte)3;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "2 3", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void RefReturningMethod()
        {
            string source = @"
class C
{
    static int i = 0;

    static void Main()
    {
        (M(), M()) = new C();
        System.Console.WriteLine($""Final i is {i}"");
    }

    static ref int M()
    {
        System.Console.WriteLine($""M (previous i is {i})"");
        return ref i;
    }

    void Deconstruct(out int x, out int y)
    {
        System.Console.WriteLine(""Deconstruct"");
        x = 42;
        y = 43;
    }
}
";
            var expected =
@"M (previous i is 0)
M (previous i is 0)
Deconstruct
Final i is 43
";

            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact, CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void RefReturningProperty()
        {
            string source = @"
class C
{
    static int i = 0;

    static void Main()
    {
        (P, P) = new C();
        System.Console.WriteLine($""Final i is {i}"");
    }

    static ref int P
    {
        get
        {
            System.Console.WriteLine($""P (previous i is {i})"");
            return ref i;
        }
    }

    void Deconstruct(out int x, out int y)
    {
        System.Console.WriteLine(""Deconstruct"");
        x = 42;
        y = 43;
    }
}
";
            var expected =
@"P (previous i is 0)
P (previous i is 0)
Deconstruct
Final i is 43
";

            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void RefReturningMethodFlow()
        {
            string source = @"
struct C
{
    static C i;
    static C P { get { System.Console.WriteLine(""getP""); return i; } set { System.Console.WriteLine(""setP""); i = value; } }

    static void Main()
    {
        (M(), M()) = P;
    }

    static ref C M()
    {
        System.Console.WriteLine($""M (previous i is {i})"");
        return ref i;
    }

    void Deconstruct(out int x, out int y)
    {
        System.Console.WriteLine(""Deconstruct"");
        x = 42;
        y = 43;
    }

    public static implicit operator C(int x)
    {
        System.Console.WriteLine(""conversion"");
        return new C();
    }
}
";

            var expected =
@"M (previous i is C)
M (previous i is C)
getP
Deconstruct
conversion
conversion";

            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Indexers()
        {
            string source = @"
class C
{
    static SomeArray array;

    static void Main()
    {
        int y;
        (Foo()[Bar()], y) = new C();
        System.Console.WriteLine($""Final array values[2] {array.values[2]}"");
    }

    static SomeArray Foo()
    {
        System.Console.WriteLine($""Foo"");
        array = new SomeArray();
        return array;
    }

    static int Bar()
    {
        System.Console.WriteLine($""Bar"");
        return 2;
    }

    void Deconstruct(out int x, out int y)
    {
        System.Console.WriteLine(""Deconstruct"");
        x = 101;
        y = 102;
    }
}
class SomeArray
{
    public int[] values;
    public SomeArray() { values = new [] { 42, 43, 44 }; }
    public int this[int index] {
        get { System.Console.WriteLine($""indexGet (with value {values[index]})""); return values[index]; }
        set { System.Console.WriteLine($""indexSet (with value {value})""); values[index] = value; }
    }
}
";
            var expected =
@"Foo
Bar
Deconstruct
indexSet (with value 101)
Final array values[2] 101
";
            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningTuple()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = (1, ""hello"");
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningTupleWithConversion()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = M();
        System.Console.WriteLine(x + "" "" + y);
    }

    static System.ValueTuple<int, string> M()
    {
        return (1, ""hello"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningLongTuple()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        int y;

        (x, x, x, x, x, x, x, x, x, y) = (1, 1, 1, 1, 1, 1, 1, 1, 4, 2);
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "4 2", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size      152 (0x98)
  .maxstack  10
  .locals init (int V_0, //y
                long V_1,
                long V_2,
                long V_3,
                long V_4,
                long V_5,
                long V_6,
                long V_7,
                long V_8,
                long V_9,
                int V_10)
  IL_0000:  ldc.i4.1
  IL_0001:  conv.i8
  IL_0002:  ldc.i4.1
  IL_0003:  conv.i8
  IL_0004:  ldc.i4.1
  IL_0005:  conv.i8
  IL_0006:  ldc.i4.1
  IL_0007:  conv.i8
  IL_0008:  ldc.i4.1
  IL_0009:  conv.i8
  IL_000a:  ldc.i4.1
  IL_000b:  conv.i8
  IL_000c:  ldc.i4.1
  IL_000d:  conv.i8
  IL_000e:  ldc.i4.1
  IL_000f:  conv.i8
  IL_0010:  ldc.i4.4
  IL_0011:  conv.i8
  IL_0012:  ldc.i4.2
  IL_0013:  newobj     ""System.ValueTuple<long, long, int>..ctor(long, long, int)""
  IL_0018:  newobj     ""System.ValueTuple<long, long, long, long, long, long, long, (long, long, int)>..ctor(long, long, long, long, long, long, long, (long, long, int))""
  IL_001d:  dup
  IL_001e:  ldfld      ""long System.ValueTuple<long, long, long, long, long, long, long, (long, long, int)>.Item1""
  IL_0023:  stloc.1
  IL_0024:  dup
  IL_0025:  ldfld      ""long System.ValueTuple<long, long, long, long, long, long, long, (long, long, int)>.Item2""
  IL_002a:  stloc.2
  IL_002b:  dup
  IL_002c:  ldfld      ""long System.ValueTuple<long, long, long, long, long, long, long, (long, long, int)>.Item3""
  IL_0031:  stloc.3
  IL_0032:  dup
  IL_0033:  ldfld      ""long System.ValueTuple<long, long, long, long, long, long, long, (long, long, int)>.Item4""
  IL_0038:  stloc.s    V_4
  IL_003a:  dup
  IL_003b:  ldfld      ""long System.ValueTuple<long, long, long, long, long, long, long, (long, long, int)>.Item5""
  IL_0040:  stloc.s    V_5
  IL_0042:  dup
  IL_0043:  ldfld      ""long System.ValueTuple<long, long, long, long, long, long, long, (long, long, int)>.Item6""
  IL_0048:  stloc.s    V_6
  IL_004a:  dup
  IL_004b:  ldfld      ""long System.ValueTuple<long, long, long, long, long, long, long, (long, long, int)>.Item7""
  IL_0050:  stloc.s    V_7
  IL_0052:  dup
  IL_0053:  ldfld      ""(long, long, int) System.ValueTuple<long, long, long, long, long, long, long, (long, long, int)>.Rest""
  IL_0058:  ldfld      ""long System.ValueTuple<long, long, int>.Item1""
  IL_005d:  stloc.s    V_8
  IL_005f:  dup
  IL_0060:  ldfld      ""(long, long, int) System.ValueTuple<long, long, long, long, long, long, long, (long, long, int)>.Rest""
  IL_0065:  ldfld      ""long System.ValueTuple<long, long, int>.Item2""
  IL_006a:  stloc.s    V_9
  IL_006c:  ldfld      ""(long, long, int) System.ValueTuple<long, long, long, long, long, long, long, (long, long, int)>.Rest""
  IL_0071:  ldfld      ""int System.ValueTuple<long, long, int>.Item3""
  IL_0076:  stloc.s    V_10
  IL_0078:  ldloc.s    V_9
  IL_007a:  ldloc.s    V_10
  IL_007c:  stloc.0
  IL_007d:  box        ""long""
  IL_0082:  ldstr      "" ""
  IL_0087:  ldloc.0
  IL_0088:  box        ""int""
  IL_008d:  call       ""string string.Concat(object, object, object)""
  IL_0092:  call       ""void System.Console.WriteLine(string)""
  IL_0097:  ret
}
");
        }

        [Fact]
        public void AssigningLongTupleWithNames()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        int y;

        (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "9 10", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (9,43): warning CS8123: The tuple element name 'a' is ignored because a different name is specified by the target type '(long, long, long, long, long, long, long, long, long, int)'.
                //         (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "a: 1").WithArguments("a", "(long, long, long, long, long, long, long, long, long, int)").WithLocation(9, 43),
                // (9,49): warning CS8123: The tuple element name 'b' is ignored because a different name is specified by the target type '(long, long, long, long, long, long, long, long, long, int)'.
                //         (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "b: 2").WithArguments("b", "(long, long, long, long, long, long, long, long, long, int)").WithLocation(9, 49),
                // (9,55): warning CS8123: The tuple element name 'c' is ignored because a different name is specified by the target type '(long, long, long, long, long, long, long, long, long, int)'.
                //         (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "c: 3").WithArguments("c", "(long, long, long, long, long, long, long, long, long, int)").WithLocation(9, 55),
                // (9,61): warning CS8123: The tuple element name 'd' is ignored because a different name is specified by the target type '(long, long, long, long, long, long, long, long, long, int)'.
                //         (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "d: 4").WithArguments("d", "(long, long, long, long, long, long, long, long, long, int)").WithLocation(9, 61),
                // (9,67): warning CS8123: The tuple element name 'e' is ignored because a different name is specified by the target type '(long, long, long, long, long, long, long, long, long, int)'.
                //         (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "e: 5").WithArguments("e", "(long, long, long, long, long, long, long, long, long, int)").WithLocation(9, 67),
                // (9,73): warning CS8123: The tuple element name 'f' is ignored because a different name is specified by the target type '(long, long, long, long, long, long, long, long, long, int)'.
                //         (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "f: 6").WithArguments("f", "(long, long, long, long, long, long, long, long, long, int)").WithLocation(9, 73),
                // (9,79): warning CS8123: The tuple element name 'g' is ignored because a different name is specified by the target type '(long, long, long, long, long, long, long, long, long, int)'.
                //         (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "g: 7").WithArguments("g", "(long, long, long, long, long, long, long, long, long, int)").WithLocation(9, 79),
                // (9,85): warning CS8123: The tuple element name 'h' is ignored because a different name is specified by the target type '(long, long, long, long, long, long, long, long, long, int)'.
                //         (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "h: 8").WithArguments("h", "(long, long, long, long, long, long, long, long, long, int)").WithLocation(9, 85),
                // (9,91): warning CS8123: The tuple element name 'i' is ignored because a different name is specified by the target type '(long, long, long, long, long, long, long, long, long, int)'.
                //         (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "i: 9").WithArguments("i", "(long, long, long, long, long, long, long, long, long, int)").WithLocation(9, 91),
                // (9,97): warning CS8123: The tuple element name 'j' is ignored because a different name is specified by the target type '(long, long, long, long, long, long, long, long, long, int)'.
                //         (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "j: 10").WithArguments("j", "(long, long, long, long, long, long, long, long, long, int)").WithLocation(9, 97)
                );
        }

        [Fact]
        public void AssigningLongTuple2()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        int y;

        (x, x, x, x, x, x, x, x, x, y) = (1, 1, 1, 1, 1, 1, 1, 1, 4, (byte)2);
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "4 2", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningTypelessTuple()
        {
            string source = @"
class C
{
    static void Main()
    {
        string x = ""goodbye"";
        string y;

        (x, y) = (null, ""hello"");
        System.Console.WriteLine($""{x}{y}"");
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "hello", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       52 (0x34)
  .maxstack  3
  .locals init (string V_0, //x
                string V_1, //y
                string V_2,
                string V_3)
  IL_0000:  ldstr      ""goodbye""
  IL_0005:  stloc.0
  IL_0006:  ldnull
  IL_0007:  ldstr      ""hello""
  IL_000c:  newobj     ""System.ValueTuple<string, string>..ctor(string, string)""
  IL_0011:  dup
  IL_0012:  ldfld      ""string System.ValueTuple<string, string>.Item1""
  IL_0017:  stloc.2
  IL_0018:  ldfld      ""string System.ValueTuple<string, string>.Item2""
  IL_001d:  stloc.3
  IL_001e:  ldloc.2
  IL_001f:  stloc.0
  IL_0020:  ldloc.3
  IL_0021:  stloc.1
  IL_0022:  ldstr      ""{0}{1}""
  IL_0027:  ldloc.0
  IL_0028:  ldloc.1
  IL_0029:  call       ""string string.Format(string, object, object)""
  IL_002e:  call       ""void System.Console.WriteLine(string)""
  IL_0033:  ret
}
");
        }

        [Fact]
        public void ValueTupleReturnIsNotEmittedIfUnused()
        {
            string source = @"
class C
{
    public static void Main()
    {
        int x, y;
        (x, y) = new C();
    }

    public void Deconstruct(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
";
            var comp = CompileAndVerify(source, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main",
@"{
  // Code size       19 (0x13)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_0
  IL_0007:  ldloca.s   V_1
  IL_0009:  call       ""void C.Deconstruct(out int, out int)""
  IL_000e:  ldloc.0
  IL_000f:  ldloc.1
  IL_0010:  stloc.2
  IL_0011:  pop
  IL_0012:  ret
}");
        }

        [Fact]
        public void ValueTupleReturnIsEmittedIfUsed()
        {
            string source = @"
class C
{
    public static void Main()
    {
        int x, y;
        var z = ((x, y) = new C());
        z.ToString();
    }

    public void Deconstruct(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
";
            var comp = CompileAndVerify(source, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main",
@"{
  // Code size       39 (0x27)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0, //z
                int V_1,
                int V_2,
                int V_3)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_1
  IL_0007:  ldloca.s   V_2
  IL_0009:  call       ""void C.Deconstruct(out int, out int)""
  IL_000e:  ldloc.1
  IL_000f:  ldloc.2
  IL_0010:  stloc.3
  IL_0011:  ldloc.3
  IL_0012:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0017:  stloc.0
  IL_0018:  ldloca.s   V_0
  IL_001a:  constrained. ""System.ValueTuple<int, int>""
  IL_0020:  callvirt   ""string object.ToString()""
  IL_0025:  pop
  IL_0026:  ret
}");
        }

        [Fact]
        public void DeconstructionDeclarationCanOnlyBeParsedAsStatement()
        {
            string source = @"
class C
{
    public static void Main()
    {
        var z = ((var x, int y) = new C());
    }

    public void Deconstruct(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,33): error CS1003: Syntax error, '=>' expected
                //         var z = ((var x, int y) = new C());
                Diagnostic(ErrorCode.ERR_SyntaxError, "=").WithArguments("=>", "=").WithLocation(6, 33),
                // (6,33): error CS1525: Invalid expression term '='
                //         var z = ((var x, int y) = new C());
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(6, 33),
                // (6,19): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         var z = ((var x, int y) = new C());
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(6, 19)
                );
        }

        [Fact]
        public void NoArgIteratorTypeInAsync()
        {
            string source = @"
using System;
class C
{
    public async void M()
    {
        (int x, var (err1, y)) = (0, new C());
        (ArgIterator err2, var err3) = M2();
        foreach ((ArgIterator err4, var err5) in new[] { M2() })
        {
        }
    }

    public static (ArgIterator, ArgIterator) M2()
    {
        return (default(ArgIterator), default(ArgIterator));
    }
    public void Deconstruct(out ArgIterator a, out int b)
    {
        a = default(ArgIterator);
        b = 2;
    }

    public void M3()
    {
        (int x, var (err1, y)) = (0, new C());
        (ArgIterator err2, var err3) = M2();
        foreach ((ArgIterator err4, var err5) in new[] { M2() })
        {
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (14,20): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     public static (ArgIterator, ArgIterator) M2()
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "ArgIterator").WithArguments("System.ArgIterator").WithLocation(14, 20),
                // (14,33): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     public static (ArgIterator, ArgIterator) M2()
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "ArgIterator").WithArguments("System.ArgIterator").WithLocation(14, 33),
                // (18,29): error CS1601: Cannot make reference to variable of type 'ArgIterator'
                //     public void Deconstruct(out ArgIterator a, out int b)
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "out ArgIterator a").WithArguments("System.ArgIterator").WithLocation(18, 29),
                // (7,22): error CS4012: Parameters or locals of type 'ArgIterator' cannot be declared in async methods or lambda expressions.
                //         (int x, var (err1, y)) = (0, new C());
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "err1").WithArguments("System.ArgIterator").WithLocation(7, 22),
                // (8,10): error CS4012: Parameters or locals of type 'ArgIterator' cannot be declared in async methods or lambda expressions.
                //         (ArgIterator err2, var err3) = M2();
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "ArgIterator").WithArguments("System.ArgIterator").WithLocation(8, 10),
                // (8,32): error CS4012: Parameters or locals of type 'ArgIterator' cannot be declared in async methods or lambda expressions.
                //         (ArgIterator err2, var err3) = M2();
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "err3").WithArguments("System.ArgIterator").WithLocation(8, 32),
                // (9,19): error CS4012: Parameters or locals of type 'ArgIterator' cannot be declared in async methods or lambda expressions.
                //         foreach ((ArgIterator err4, var err5) in new[] { M2() })
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "ArgIterator").WithArguments("System.ArgIterator").WithLocation(9, 19),
                // (9,41): error CS4012: Parameters or locals of type 'ArgIterator' cannot be declared in async methods or lambda expressions.
                //         foreach ((ArgIterator err4, var err5) in new[] { M2() })
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "err5").WithArguments("System.ArgIterator").WithLocation(9, 41),
                // (5,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public async void M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(5, 23),
                // (16,17): error CS0610: Field or property cannot be of type 'ArgIterator'
                //         return (default(ArgIterator), default(ArgIterator));
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "default(ArgIterator)").WithArguments("System.ArgIterator").WithLocation(16, 17),
                // (16,39): error CS0610: Field or property cannot be of type 'ArgIterator'
                //         return (default(ArgIterator), default(ArgIterator));
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "default(ArgIterator)").WithArguments("System.ArgIterator").WithLocation(16, 39)
                );
        }

        [Fact]
        public void MixedDeconstructionCannotBeParsed()
        {
            string source = @"
class C
{
    public static void Main()
    {
        int x;
        (x, int y) = new C();
    }

    public void Deconstruct(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (7,10): error CS1031: Type expected
                //         (x, int y) = new C();
                Diagnostic(ErrorCode.ERR_TypeExpected, "x").WithLocation(7, 10),
                // (7,10): error CS0128: A local variable or function named 'x' is already defined in this scope
                //         (x, int y) = new C();
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(7, 10),
                // (6,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(6, 13)
                );
        }

        [Fact]
        public void DeconstructionWithTupleNamesCannotBeParsed()
        {
            string source = @"
class C
{
    public static void Main()
    {
        (Alice: var x, Bob: int y) = (1, 2);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8124: Tuple must contain at least two elements.
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, "(Alice: var ").WithLocation(6, 9),
                // (6,21): error CS1026: ) expected
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "x").WithLocation(6, 21),
                // (6,21): error CS1002: ; expected
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "x").WithLocation(6, 21),
                // (6,22): error CS1002: ; expected
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 22),
                // (6,22): error CS1513: } expected
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(6, 22),
                // (6,34): error CS1002: ; expected
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 34),
                // (6,34): error CS1513: } expected
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 34),
                // (6,36): error CS1525: Invalid expression term '='
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(6, 36),
                // (6,17): error CS0103: The name 'var' does not exist in the current context
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 17),
                // (6,21): error CS0103: The name 'x' does not exist in the current context
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 21),
                // (6,24): warning CS0164: This label has not been referenced
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "Bob").WithLocation(6, 24),
                // (6,33): warning CS0168: The variable 'y' is declared but never used
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "y").WithArguments("y").WithLocation(6, 33)
                );
        }

        [Fact]
        public void ValueTupleReturnIsEmittedIfUsedInLambda()
        {
            string source = @"
class C
{
    static void F(System.Action a) { }
    static void F<T>(System.Func<T> f) { System.Console.Write(f().ToString()); }
    static void Main()
    {
        int x, y;
        F(() => (x, y) = (1, 2));
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "(1, 2)", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningIntoProperties()
        {
            string source = @"
class C
{
    static long x { set { System.Console.WriteLine($""setX {value}""); } }
    static string y { get; set; }

    static void Main()
    {
        (x, y) = new C();
        System.Console.WriteLine(y);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            string expected =
@"setX 1
hello";
            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningTupleIntoProperties()
        {
            string source = @"
class C
{
    static long x { set { System.Console.WriteLine($""setX {value}""); } }
    static string y { get; set; }

    static void Main()
    {
        (x, y) = (1, ""hello"");
        System.Console.WriteLine(y);
    }
}
";
            string expected =
@"setX 1
hello";
            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Swap()
        {
            string source = @"
class C
{
    static int x = 2;
    static int y = 4;

    static void Main()
    {
        Swap();
        System.Console.WriteLine(x + "" "" + y);
    }

    static void Swap()
    {
        (x, y) = (y, x);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "4 2", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Swap", @"
{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldsfld     ""int C.y""
  IL_0005:  ldsfld     ""int C.x""
  IL_000a:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000f:  dup
  IL_0010:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0015:  stloc.0
  IL_0016:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  stsfld     ""int C.x""
  IL_0022:  ldloc.1
  IL_0023:  stsfld     ""int C.y""
  IL_0028:  ret
}

");
        }

        [Fact]
        public void CircularFlow()
        {
            string source = @"
class C
{
    static void Main()
    {
        (object i, object ii) x = (1,2);
        object y;

        (x.ii, y) = x;
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "(1, 1) 2", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void CircularFlow2()
        {
            string source = @"
class C
{
    static void Main()
    {
        (object i, object ii) x = (1,2);
        object y;

        ref var a = ref x;

        (a.ii, y) = x;
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "(1, 1) 2", additionalRefs: s_valueTupleRefs, parseOptions: TestOptions.Regular.WithRefsFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructUsingBaseDeconstructMethod()
        {
            string source = @"
class Base
{
    public void Deconstruct(out int a, out int b) { a = 1; b = 2; }
}
class C : Base
{
    static void Main()
    {
        int x, y;
        (x, y) = new C();

        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int c) { c = 42; }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 2", additionalRefs: s_valueTupleRefs, parseOptions: TestOptions.Regular.WithRefsFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructUsingExtensionMethod()
        {
            string source = @"
using System;
class C
{
    static void Main()
    {
        int x;
        string y, z;
        (x, (y, z)) = Tuple.Create(1, Tuple.Create(""hello"", ""world""));

        System.Console.WriteLine(x + "" "" + y + "" "" + z);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello world", additionalRefs: s_valueTupleRefs, parseOptions: TestOptions.Regular.WithRefsFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedDeconstructUsingExtensionMethod()
        {
            string source = @"
using System;
class C
{
    static void Main()
    {
        int x;
        string y, z;
        (x, (y, z)) = Tuple.Create(1, Tuple.Create(""hello"", ""world""));

        System.Console.WriteLine(x + "" "" + y + "" "" + z);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello world", additionalRefs: s_valueTupleRefs, parseOptions: TestOptions.Regular.WithRefsFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void OverrideDeconstruct()
        {
            string source = @"
class Base
{
    public virtual void Deconstruct(out int a, out string b) { a = 1; b = ""hello""; }
}
class C : Base
{
    static void Main()
    {
        int x;
        string y;
        (x, y) = new C();
    }
    public override void Deconstruct(out int a, out string b) { a = 1; b = ""hello""; System.Console.WriteLine(""override""); }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "override", additionalRefs: s_valueTupleRefs, parseOptions: TestOptions.Regular.WithRefsFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructRefTuple()
        {
            string template = @"
using System;
class C
{
    static void Main()
    {
        int VARIABLES; // int x1, x2, ...
        (VARIABLES) = (TUPLE).ToTuple(); // (x1, x2, ...) = (1, 2, ...).ToTuple();

        System.Console.WriteLine(OUTPUT);
    }
}
";
            for (int i = 2; i <= 21; i++)
            {
                var tuple = String.Join(", ", Enumerable.Range(1, i).Select(n => n.ToString()));
                var variables = String.Join(", ", Enumerable.Range(1, i).Select(n => $"x{n}"));
                var output = String.Join(@" + "" "" + ", Enumerable.Range(1, i).Select(n => $"x{n}"));
                var expected = String.Join(" ", Enumerable.Range(1, i).Select(n => n));

                var source = template.Replace("VARIABLES", variables).Replace("TUPLE", tuple).Replace("OUTPUT", output);
                var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: s_valueTupleRefs, parseOptions: TestOptions.Regular.WithRefsFeature());
                comp.VerifyDiagnostics();
            }
        }

        [Fact]
        public void DeconstructExtensionMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C();
        System.Console.WriteLine(x + "" "" + y);
    }
}
static class D
{
    public static void Deconstruct(this C value, out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UnderspecifiedDeconstructGenericExtensionMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;
        (x, y) = new C();
    }
}
static class Extension
{
    public static void Deconstruct<T>(this C value, out int a, out T b)
    {
        a = 2;
        b = default(T);
    }
}";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef });
            comp.VerifyDiagnostics(
                // (8,18): error CS0411: The type arguments for method 'Extension.Deconstruct<T>(C, out int, out T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "new C()").WithArguments("Extension.Deconstruct<T>(C, out int, out T)").WithLocation(8, 18),
                // (8,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(8, 18)
                );
        }

        [Fact]
        public void UnspecifiedGenericMethodIsNotCandidate()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;
        (x, y) = new C();
    }
}
static class Extension
{
    public static void Deconstruct<T>(this C value, out int a, out T b)
    {
        a = 2;
        b = default(T);
    }
    public static void Deconstruct(this C value, out int a, out string b)
    {
        a = 2;
        b = ""hello"";
        System.Console.Write(""Deconstructed"");
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "Deconstructed", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructGenericExtensionMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C1<string>();
    }
}

public class C1<T> { }

static class Extension
{
    public static void Deconstruct<T>(this C1<T> value, out int a, out T b)
    {
        a = 2;
        b = default(T);
        System.Console.WriteLine(""Deconstructed"");
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "Deconstructed", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructGenericMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C1();
    }
}
class C1
{
    public void Deconstruct<T>(out int a, out T b)
    {
        a = 2;
        b = default(T);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (9,18): error CS0411: The type arguments for method 'C1.Deconstruct<T>(out int, out T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         (x, y) = new C1();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "new C1()").WithArguments("C1.Deconstruct<T>(out int, out T)").WithLocation(9, 18),
                // (9,18): error CS8129: No Deconstruct instance or extension method was found for type 'C1', with 2 out parameters.
                //         (x, y) = new C1();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C1()").WithArguments("C1", "2").WithLocation(9, 18)
                );
        }

        [Fact]
        public void AmbiguousDeconstructGenericMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C1();
        System.Console.Write($""{x} {y}"");
    }
}
class C1
{
    public void Deconstruct<T>(out int a, out T b)
    {
        a = 2;
        b = default(T);
    }
    public void Deconstruct(out int a, out string b)
    {
        a = 2;
        b = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, additionalRefs: s_valueTupleRefs, expectedOutput: "2 hello");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedTupleAssignment()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y, z;

        (x, (y, z)) = (1, (""a"", ""b""));
        System.Console.WriteLine(x + "" "" + y + "" "" + z);
    }
}
";
            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var lhs = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().First();
                Assert.Equal(@"(x, (y, z))", lhs.ToString());
                Assert.Equal("(System.Int32, (System.String, System.String))", model.GetTypeInfo(lhs).Type.ToTestDisplayString());
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 a b", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedTypelessTupleAssignment()
        {
            string source = @"
class C
{
    static void Main()
    {
        string x, y, z;

        (x, (y, z)) = (null, (null, null));
        System.Console.WriteLine(""nothing"" + x + y + z);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "nothing", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedDeconstructAssignment()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y, z;

        (x, (y, z)) = new D1();
        System.Console.WriteLine(x + "" "" + y + "" "" + z);
    }
}
class D1
{
    public void Deconstruct(out int item1, out D2 item2)
    {
        item1 = 1;
        item2 = new D2();
    }
}
class D2
{
    public void Deconstruct(out string item1, out string item2)
    {
        item1 = ""a"";
        item2 = ""b"";
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1 a b", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedMixedAssignment1()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y, z;

        (x, (y, z)) = (1, new D1());
        System.Console.WriteLine(x + "" "" + y + "" "" + z);
    }
}
class D1
{
    public void Deconstruct(out int item1, out int item2)
    {
        item1 = 2;
        item2 = 3;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1 2 3", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedMixedAssignment2()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y, z;

        (x, (y, z)) = new D1();
        System.Console.WriteLine(x + "" "" + y + "" "" + z);
    }
}
class D1
{
    public void Deconstruct(out int item1, out (string, string) item2)
    {
        item1 = 1;
        item2 = (""a"", ""b"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1 a b", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyNestedExecutionOrder()
        {
            string source = @"
using System;
class C
{
    int x { set { Console.WriteLine($""setX""); } }
    int y { set { Console.WriteLine($""setY""); } }
    int z { set { Console.WriteLine($""setZ""); } }

    C getHolderForX() { Console.WriteLine(""getHolderforX""); return this; }
    C getHolderForY() { Console.WriteLine(""getHolderforY""); return this; }
    C getHolderForZ() { Console.WriteLine(""getHolderforZ""); return this; }
    C getDeconstructReceiver() { Console.WriteLine(""getDeconstructReceiver""); return this; }

    static void Main()
    {
        C c = new C();
        (c.getHolderForX().x, (c.getHolderForY().y, c.getHolderForZ().z)) = c.getDeconstructReceiver();
    }
    public void Deconstruct(out D1 x, out C1 t) { x = new D1(); t = new C1(); Console.WriteLine(""Deconstruct1""); }
}
class C1
{
    public void Deconstruct(out D2 y, out D3 z) { y = new D2(); z = new D3(); Console.WriteLine(""Deconstruct2""); }
}
class D1
{
    public static implicit operator int(D1 d) { Console.WriteLine(""Conversion1""); return 1; }
}
class D2
{
    public static implicit operator int(D2 d) { Console.WriteLine(""Conversion2""); return 2; }
}
class D3
{
    public static implicit operator int(D3 d) { Console.WriteLine(""Conversion3""); return 3; }
}
";

            string expected =
@"getHolderforX
getHolderforY
getHolderforZ
getDeconstructReceiver
Deconstruct1
Deconstruct2
Conversion1
Conversion2
Conversion3
setX
setY
setZ
";
            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyNestedExecutionOrder2()
        {
            string source = @"
using System;
class C
{
    static LongInteger x1 { set { Console.WriteLine($""setX1 {value}""); } }
    static LongInteger x2 { set { Console.WriteLine($""setX2 {value}""); } }
    static LongInteger x3 { set { Console.WriteLine($""setX3 {value}""); } }
    static LongInteger x4 { set { Console.WriteLine($""setX4 {value}""); } }
    static LongInteger x5 { set { Console.WriteLine($""setX5 {value}""); } }
    static LongInteger x6 { set { Console.WriteLine($""setX6 {value}""); } }
    static LongInteger x7 { set { Console.WriteLine($""setX7 {value}""); } }

    static void Main()
    {
        ((x1, (x2, x3)), ((x4, x5), (x6, x7))) = Pair.Create(Pair.Create(new Integer(1), Pair.Create(new Integer(2), new Integer(3))),
                                                      Pair.Create(Pair.Create(new Integer(4), new Integer(5)), Pair.Create(new Integer(6), new Integer(7))));
    }
}
" + commonSource;

            string expected =
@"Deconstructing ((1, (2, 3)), ((4, 5), (6, 7)))
Deconstructing (1, (2, 3))
Deconstructing (2, 3)
Deconstructing ((4, 5), (6, 7))
Deconstructing (4, 5)
Deconstructing (6, 7)
Converting 1
Converting 2
Converting 3
Converting 4
Converting 5
Converting 6
Converting 7
setX1 1
setX2 2
setX3 3
setX4 4
setX5 5
setX6 6
setX7 7";
            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MixOfAssignments()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        C a, b, c;
        c = new C();
        (x, y) = a = b = c;
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/12400")]
        [WorkItem(12400, "https://github.com/dotnet/roslyn/issues/12400")]
        public void AssignWithPostfixOperator()
        {
            string source = @"
class C
{
    int state = 1;

    static void Main()
    {
        long x;
        string y;
        C c = new C();
        (x, y) = c++;
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = state;
        b = ""hello"";
    }

    public static C operator ++(C c1)
    {
        return new C() { state = 2 };
    }
}
";
            // https://github.com/dotnet/roslyn/issues/12400
            // we expect "2 hello" instead, which means the evaluation order is wrong
            var comp = CompileAndVerify(source, expectedOutput: "1 hello");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(13631, "https://github.com/dotnet/roslyn/issues/13631")]
        public void DeconstructionDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, x2) = (1, ""hello"");
        System.Console.WriteLine(x1 + "" "" + x2);
    }
}
";

            var comp = CompileAndVerify(source, additionalRefs: s_valueTupleRefs, expectedOutput: "1 hello");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (int V_0, //x1
                string V_1, //x2
                int V_2,
                string V_3)
  IL_0000:  ldc.i4.1
  IL_0001:  ldstr      ""hello""
  IL_0006:  newobj     ""System.ValueTuple<int, string>..ctor(int, string)""
  IL_000b:  dup
  IL_000c:  ldfld      ""int System.ValueTuple<int, string>.Item1""
  IL_0011:  stloc.2
  IL_0012:  ldfld      ""string System.ValueTuple<int, string>.Item2""
  IL_0017:  stloc.3
  IL_0018:  ldloc.2
  IL_0019:  stloc.0
  IL_001a:  ldloc.3
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  box        ""int""
  IL_0022:  ldstr      "" ""
  IL_0027:  ldloc.1
  IL_0028:  call       ""string string.Concat(object, object, object)""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ret
}
");
        }

        [Fact]
        public void NestedVarDeconstructionDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, (x2, x3)) = (1, (2, ""hello""));
        System.Console.WriteLine(x1 + "" "" + x2 + "" "" + x3);
    }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var lhs = tree.GetRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().First();
                Assert.Equal(@"var (x1, (x2, x3))", lhs.ToString());
                Assert.Null(model.GetTypeInfo(lhs).Type);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionLocal(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionLocal(model, x2, x2Ref);

                var x3 = GetDeconstructionVariable(tree, "x3");
                var x3Ref = GetReference(tree, "x3");
                VerifyModelForDeconstructionLocal(model, x3, x3Ref);
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2 hello", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedDeconstructionDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int x1, (int x2, string x3)) = (1, (2, ""hello""));
        System.Console.WriteLine(x1 + "" "" + x2 + "" "" + x3);
    }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionLocal(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionLocal(model, x2, x2Ref);

                var x3 = GetDeconstructionVariable(tree, "x3");
                var x3Ref = GetReference(tree, "x3");
                VerifyModelForDeconstructionLocal(model, x3, x3Ref);
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2 hello", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VarMethodExists()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1 = 1;
        int x2 = 1;
        var (x1, x2);
    }
    static void var(int a, int b) { System.Console.WriteLine(""var""); }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "var", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeMergingSuccess1()
        {
            string source = @"
class C
{
    static void Main()
    {
        (var (x1, x2), string x3) = ((1, 2), null);
        System.Console.WriteLine(x1 + "" "" + x2 + "" "" + x3);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: " 1 2", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeMergingSuccess2()
        {
            string source = @"
class C
{
    static void Main()
    {
        (string x1, byte x2, var x3) = (null, 2, 3);
        System.Console.WriteLine(x1 + "" "" + x2 + "" "" + x3);
    }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var lhs = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().First();
                Assert.Equal(@"(string x1, byte x2, var x3)", lhs.ToString());
                Assert.Equal("(System.String, System.Byte, System.Int32)", model.GetTypeInfo(lhs).Type.ToTestDisplayString());

                var literal = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
                Assert.Equal(@"(null, 2, 3)", literal.ToString());
                Assert.Null(model.GetTypeInfo(literal).Type);
                Assert.Equal("(System.String, System.Byte, System.Int32)", model.GetTypeInfo(literal).ConvertedType.ToTestDisplayString());
                Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(literal).Kind);
            };

            var comp = CompileAndVerify(source, expectedOutput: " 2 3", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeMergingSuccess3()
        {
            string source = @"
class C
{
    static void Main()
    {
        (string x1, var x2) = (null, (1, 2));
        System.Console.WriteLine(x1 + "" "" + x2);
    }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var lhs = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().First();
                Assert.Equal(@"(string x1, var x2)", lhs.ToString());
                Assert.Equal("(System.String, (System.Int32, System.Int32))", model.GetTypeInfo(lhs).Type.ToTestDisplayString());

                var literal = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
                Assert.Equal(@"(null, (1, 2))", literal.ToString());
                Assert.Null(model.GetTypeInfo(literal).Type);
                Assert.Equal("(System.String, (System.Int32, System.Int32))", model.GetTypeInfo(literal).ConvertedType.ToTestDisplayString());
                Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(literal).Kind);

                var nestedLiteral = literal.Arguments[1].Expression;
                Assert.Equal(@"(1, 2)", nestedLiteral.ToString());
                Assert.Equal("(System.Int32, System.Int32)", model.GetTypeInfo(nestedLiteral).Type.ToTestDisplayString());
                Assert.Equal("(System.Int32, System.Int32)", model.GetTypeInfo(nestedLiteral).ConvertedType.ToTestDisplayString());
            };

            var comp = CompileAndVerify(source, expectedOutput: " (1, 2)", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeMergingSuccess4()
        {
            string source = @"
class C
{
    static void Main()
    {
        ((string x1, byte x2, var x3), int x4) = (M(), 4);
        System.Console.WriteLine(x1 + "" "" + x2 + "" "" + x3 + "" "" + x4);
    }
    static (string, byte, int) M() { return (null, 2, 3); }
}
";
            var comp = CompileAndVerify(source, expectedOutput: " 2 3 4", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VarVarDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        (var (x1, x2), var x3) = Pair.Create(Pair.Create(1, ""hello""), 2);
        System.Console.WriteLine(x1 + "" "" + x2 + "" "" + x3);
    }
}
" + commonSource;

            string expected =
@"Deconstructing ((1, hello), 2)
Deconstructing (1, hello)
1 hello 2";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionLocal(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionLocal(model, x2, x2Ref);

                var x3 = GetDeconstructionVariable(tree, "x3");
                var x3Ref = GetReference(tree, "x3");
                VerifyModelForDeconstructionLocal(model, x3, x3Ref);
            };

            var comp = CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular,
                            sourceSymbolValidator: validator, additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        private static void VerifyModelForDeconstructionLocal(SemanticModel model, SingleVariableDesignationSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeconstruction(model, decl, LocalDeclarationKind.RegularVariable, references);
        }

        private static void VerifyModelForDeconstructionForeach(SemanticModel model, SingleVariableDesignationSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeconstruction(model, decl, LocalDeclarationKind.ForEachIterationVariable, references);
        }

        private static void VerifyModelForDeconstruction(SemanticModel model, SingleVariableDesignationSyntax decl, LocalDeclarationKind kind, params IdentifierNameSyntax[] references)
        {
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(kind, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)decl));
            Assert.Same(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));

            var local = (SourceLocalSymbol)symbol;
            var typeSyntax = GetTypeSyntax(decl);
            if (local.IsVar && local.Type.IsErrorType())
            {
                Assert.Null(model.GetSymbolInfo(typeSyntax).Symbol);
            }
            else
            {
                if (typeSyntax != null)
                {
                    Assert.Equal(local.Type, model.GetSymbolInfo(typeSyntax).Symbol);
                }
            }

            foreach (var reference in references)
            {
                Assert.Same(symbol, model.GetSymbolInfo(reference).Symbol);
                Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: decl.Identifier.ValueText).Single());
                Assert.True(model.LookupNames(reference.SpanStart).Contains(decl.Identifier.ValueText));
                Assert.Equal(local.Type, model.GetTypeInfo(reference).Type);
            }
        }

        private static void VerifyModelForDeconstructionField(SemanticModel model, SingleVariableDesignationSyntax decl, params IdentifierNameSyntax[] references)
        {
            var field = (FieldSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, field.Name);
            Assert.Equal(SymbolKind.Field, ((FieldSymbol)field).Kind);
            Assert.Same(field, model.GetDeclaredSymbol((SyntaxNode)decl));
            Assert.Same(field, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            Assert.Equal(Accessibility.Private, field.DeclaredAccessibility);
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));

            foreach (var reference in references)
            {
                Assert.Same(field, model.GetSymbolInfo(reference).Symbol);
                Assert.Same(field, model.LookupSymbols(reference.SpanStart, name: decl.Identifier.ValueText).Single());
                Assert.True(model.LookupNames(reference.SpanStart).Contains(decl.Identifier.ValueText));
                Assert.Equal(field.Type, model.GetTypeInfo(reference).Type);
            }
        }

        private static TypeSyntax GetTypeSyntax(SingleVariableDesignationSyntax decl)
        {
            return (decl.Parent as DeclarationExpressionSyntax)?.Type;
        }

        private static SingleVariableDesignationSyntax GetDeconstructionVariable(SyntaxTree tree, string name)
        {
            return tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(d => d.Identifier.ValueText == name).Single();
        }

        private static IEnumerable<DiscardedDesignationSyntax> GetDiscardDesignations(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes().OfType<DiscardedDesignationSyntax>();
        }

        private static IEnumerable<IdentifierNameSyntax> GetDiscardIdentifiers(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(i => i.Identifier.ContextualKind() == SyntaxKind.UnderscoreToken);
        }

        private static IdentifierNameSyntax GetReference(SyntaxTree tree, string name)
        {
            return GetReferences(tree, name).Single();
        }

        private static IdentifierNameSyntax[] GetReferences(SyntaxTree tree, string name, int count)
        {
            var nameRef = GetReferences(tree, name).ToArray();
            Assert.Equal(count, nameRef.Length);
            return nameRef;
        }

        private static IEnumerable<IdentifierNameSyntax> GetReferences(SyntaxTree tree, string name)
        {
            return tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == name);
        }

        [Fact]
        public void DeclarationWithActualVarType()
        {
            string source = @"
class C
{
    static void Main()
    {
        (var x1, int x2) = (new var(), 2);
        System.Console.WriteLine(x1 + "" "" + x2);
    }
}
class var
{
    public override string ToString() { return ""var""; }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionLocal(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionLocal(model, x2, x2Ref);

                // extra checks on x1
                var x1Type = GetTypeSyntax(x1);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x1Type).Symbol.Kind);
                Assert.Equal("var", model.GetSymbolInfo(x1Type).Symbol.ToDisplayString());

                // extra checks on x2
                var x2Type = GetTypeSyntax(x2);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x2Type).Symbol.Kind);
                Assert.Equal("int", model.GetSymbolInfo(x2Type).Symbol.ToDisplayString());
            };

            var comp = CompileAndVerify(source, expectedOutput: "var 2", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeclarationWithImplicitVarType()
        {
            string source = @"
class C
{
    static void Main()
    {
        (var x1, var x2) = (1, 2);
        var (x3, x4) = (3, 4);
        System.Console.WriteLine(x1 + "" "" + x2 + "" "" + x3 + "" "" + x4);
    }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionLocal(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionLocal(model, x2, x2Ref);

                var x3 = GetDeconstructionVariable(tree, "x3");
                var x3Ref = GetReference(tree, "x3");
                VerifyModelForDeconstructionLocal(model, x3, x3Ref);

                var x4 = GetDeconstructionVariable(tree, "x4");
                var x4Ref = GetReference(tree, "x4");
                VerifyModelForDeconstructionLocal(model, x4, x4Ref);

                // extra checks on x1
                var x1Type = GetTypeSyntax(x1);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x1Type).Symbol.Kind);
                Assert.Equal("int", model.GetSymbolInfo(x1Type).Symbol.ToDisplayString());
                Assert.Null(model.GetAliasInfo(x1Type));

                var x34Var = (DeclarationExpressionSyntax)x3.Parent.Parent;
                Assert.Equal("var", x34Var.Type.ToString());
                Assert.Null(model.GetSymbolInfo(x34Var.Type).Symbol); // The var in `var (x3, x4)` has no symbol
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2 3 4", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeclarationWithAliasedVarType()
        {
            string source = @"
using var = D;
class C
{
    static void Main()
    {
        (var x1, int x2) = (new var(), 2);
        System.Console.WriteLine(x1 + "" "" + x2);
    }
}
class D
{
    public override string ToString() { return ""var""; }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionLocal(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionLocal(model, x2, x2Ref);

                // extra checks on x1
                var x1Type = GetTypeSyntax(x1);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x1Type).Symbol.Kind);
                Assert.Equal("D", model.GetSymbolInfo(x1Type).Symbol.ToDisplayString());
                var x1Alias = model.GetAliasInfo(x1Type);
                Assert.Equal(SymbolKind.NamedType, x1Alias.Target.Kind);
                Assert.Equal("D", x1Alias.Target.ToDisplayString());

                // extra checks on x2
                var x2Type = GetTypeSyntax(x2);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x2Type).Symbol.Kind);
                Assert.Equal("int", model.GetSymbolInfo(x2Type).Symbol.ToDisplayString());
                Assert.Null(model.GetAliasInfo(x2Type));
            };

            var comp = CompileAndVerify(source, expectedOutput: "var 2", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForWithImplicitVarType()
        {
            string source = @"
class C
{
    static void Main()
    {
        for (var (x1, x2) = (1, 2); x1 < 2; (x1, x2) = (x1 + 1, x2 + 1))
        {
            System.Console.WriteLine(x1 + "" "" + x2);
        }
    }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReferences(tree, "x1", 4);
                VerifyModelForDeconstructionLocal(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReferences(tree, "x2", 3);
                VerifyModelForDeconstructionLocal(model, x2, x2Ref);

                // extra check on var
                var x12Var = (DeclarationExpressionSyntax)x1.Parent.Parent;
                Assert.Equal("var", x12Var.Type.ToString());
                Assert.Null(model.GetSymbolInfo(x12Var.Type).Symbol); // The var in `var (x1, x2)` has no symbol
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForWithBadInitializersCannotParse()
        {
            string source = @"
class C
{
    static void Main()
    {
        for (var (x1, x2) = (1, 2), x1 = 0; ; )
        {
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (6,35): error CS1002: ; expected
                //         for (var (x1, x2) = (1, 2), x1 = 0; ; )
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 35),
                // (6,35): error CS1525: Invalid expression term ','
                //         for (var (x1, x2) = (1, 2), x1 = 0; ; )
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 35),
                // (6,35): error CS1002: ; expected
                //         for (var (x1, x2) = (1, 2), x1 = 0; ; )
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 35),
                // (6,35): error CS1525: Invalid expression term ','
                //         for (var (x1, x2) = (1, 2), x1 = 0; ; )
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 35),
                // (6,43): error CS1026: ) expected
                //         for (var (x1, x2) = (1, 2), x1 = 0; ; )
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(6, 43),
                // (6,47): error CS1513: } expected
                //         for (var (x1, x2) = (1, 2), x1 = 0; ; )
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 47)
                );
        }

        [Fact]
        public void ForWithActualVarType()
        {
            string source = @"
class C
{
    static void Main()
    {
        for ((int x1, var x2) = (1, new var()); x1 < 2; x1++)
        {
            System.Console.WriteLine(x1 + "" "" + x2);
        }
    }
}
class var
{
    public override string ToString() { return ""var""; }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReferences(tree, "x1", 3);
                VerifyModelForDeconstructionLocal(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionLocal(model, x2, x2Ref);

                // extra checks on x1
                var x1Type = GetTypeSyntax(x1);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x1Type).Symbol.Kind);
                Assert.Equal("int", model.GetSymbolInfo(x1Type).Symbol.ToDisplayString());

                // extra checks on x2
                var x2Type = GetTypeSyntax(x2);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x2Type).Symbol.Kind);
                Assert.Equal("var", model.GetSymbolInfo(x2Type).Symbol.ToDisplayString());
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 var", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForWithTypes()
        {
            string source = @"
class C
{
    static void Main()
    {
        for ((int x1, var x2) = (1, 2); x1 < 2; x1++)
        {
            System.Console.WriteLine(x1 + "" "" + x2);
        }
    }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReferences(tree, "x1", 3);
                VerifyModelForDeconstructionLocal(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionLocal(model, x2, x2Ref);

                // extra checks on x1
                var x1Type = GetTypeSyntax(x1);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x1Type).Symbol.Kind);
                Assert.Equal("int", model.GetSymbolInfo(x1Type).Symbol.ToDisplayString());

                // extra checks on x2
                var x2Type = GetTypeSyntax(x2);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x2Type).Symbol.Kind);
                Assert.Equal("int", model.GetSymbolInfo(x2Type).Symbol.ToDisplayString());
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForEachIEnumerableDeclarationWithImplicitVarType()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    static void Main()
    {
        foreach (var (x1, x2) in M())
        {
            Print(x1, x2);
        }
    }
    static IEnumerable<(int, int)> M() { yield return (1, 2); }
    static void Print(object a, object b) { System.Console.WriteLine(a + "" "" + b); }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionForeach(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionForeach(model, x2, x2Ref);

                // extra check on var
                var x12Var = (DeclarationExpressionSyntax)x1.Parent.Parent;
                Assert.Equal("var", x12Var.Type.ToString());
                Assert.Null(model.GetSymbolInfo(x12Var.Type).Symbol); // The var in `var (x1, x2)` has no symbol
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main",
@"{
  // Code size       76 (0x4c)
  .maxstack  2
  .locals init (System.Collections.Generic.IEnumerator<(int, int)> V_0,
                int V_1, //x1
                int V_2, //x2
                int V_3,
                int V_4)
  IL_0000:  call       ""System.Collections.Generic.IEnumerable<(int, int)> C.M()""
  IL_0005:  callvirt   ""System.Collections.Generic.IEnumerator<(int, int)> System.Collections.Generic.IEnumerable<(int, int)>.GetEnumerator()""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  br.s       IL_0037
    IL_000d:  ldloc.0
    IL_000e:  callvirt   ""(int, int) System.Collections.Generic.IEnumerator<(int, int)>.Current.get""
    IL_0013:  dup
    IL_0014:  ldfld      ""int System.ValueTuple<int, int>.Item1""
    IL_0019:  stloc.3
    IL_001a:  ldfld      ""int System.ValueTuple<int, int>.Item2""
    IL_001f:  stloc.s    V_4
    IL_0021:  ldloc.3
    IL_0022:  stloc.1
    IL_0023:  ldloc.s    V_4
    IL_0025:  stloc.2
    IL_0026:  ldloc.1
    IL_0027:  box        ""int""
    IL_002c:  ldloc.2
    IL_002d:  box        ""int""
    IL_0032:  call       ""void C.Print(object, object)""
    IL_0037:  ldloc.0
    IL_0038:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_003d:  brtrue.s   IL_000d
    IL_003f:  leave.s    IL_004b
  }
  finally
  {
    IL_0041:  ldloc.0
    IL_0042:  brfalse.s  IL_004a
    IL_0044:  ldloc.0
    IL_0045:  callvirt   ""void System.IDisposable.Dispose()""
    IL_004a:  endfinally
  }
  IL_004b:  ret
}
");
        }

        [Fact]
        public void ForEachSZArrayDeclarationWithImplicitVarType()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach (var (x1, x2) in M())
        {
            System.Console.Write(x1 + "" "" + x2 + "" - "");
        }
    }
    static (int, int)[] M() { return new[] { (1, 2), (3, 4) }; }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                var symbol = model.GetDeclaredSymbol(x1);

                VerifyModelForDeconstructionForeach(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionForeach(model, x2, x2Ref);

                // extra check on var
                var x12Var = (DeclarationExpressionSyntax)x1.Parent.Parent;
                Assert.Equal("var", x12Var.Type.ToString());
                Assert.Null(model.GetSymbolInfo(x12Var.Type).Symbol); // The var in `var (x1, x2)` has no symbol
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2 - 3 4 -", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main",
@"{
  // Code size       99 (0x63)
  .maxstack  4
  .locals init ((int, int)[] V_0,
                int V_1,
                int V_2, //x1
                int V_3, //x2
                int V_4,
                int V_5)
  IL_0000:  call       ""(int, int)[] C.M()""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  IL_0008:  br.s       IL_005c
  IL_000a:  ldloc.0
  IL_000b:  ldloc.1
  IL_000c:  ldelem     ""System.ValueTuple<int, int>""
  IL_0011:  dup
  IL_0012:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0017:  stloc.s    V_4
  IL_0019:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_001e:  stloc.s    V_5
  IL_0020:  ldloc.s    V_4
  IL_0022:  stloc.2
  IL_0023:  ldloc.s    V_5
  IL_0025:  stloc.3
  IL_0026:  ldc.i4.4
  IL_0027:  newarr     ""object""
  IL_002c:  dup
  IL_002d:  ldc.i4.0
  IL_002e:  ldloc.2
  IL_002f:  box        ""int""
  IL_0034:  stelem.ref
  IL_0035:  dup
  IL_0036:  ldc.i4.1
  IL_0037:  ldstr      "" ""
  IL_003c:  stelem.ref
  IL_003d:  dup
  IL_003e:  ldc.i4.2
  IL_003f:  ldloc.3
  IL_0040:  box        ""int""
  IL_0045:  stelem.ref
  IL_0046:  dup
  IL_0047:  ldc.i4.3
  IL_0048:  ldstr      "" - ""
  IL_004d:  stelem.ref
  IL_004e:  call       ""string string.Concat(params object[])""
  IL_0053:  call       ""void System.Console.Write(string)""
  IL_0058:  ldloc.1
  IL_0059:  ldc.i4.1
  IL_005a:  add
  IL_005b:  stloc.1
  IL_005c:  ldloc.1
  IL_005d:  ldloc.0
  IL_005e:  ldlen
  IL_005f:  conv.i4
  IL_0060:  blt.s      IL_000a
  IL_0062:  ret
}");
        }

        [Fact]
        public void ForEachMDArrayDeclarationWithImplicitVarType()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach (var (x1, x2) in M())
        {
            Print(x1, x2);
        }
    }
    static (int, int)[,] M() { return new (int, int)[2, 2] { { (1, 2), (3, 4) }, { (5, 6), (7, 8) } }; }
    static void Print(object a, object b) { System.Console.Write(a + "" "" + b + "" - ""); }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionForeach(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionForeach(model, x2, x2Ref);

                // extra check on var
                var x12Var = (DeclarationExpressionSyntax)x1.Parent.Parent;
                Assert.Equal("var", x12Var.Type.ToString());
                Assert.Null(model.GetSymbolInfo(x12Var.Type).Symbol); // The var in `var (x1, x2)` has no symbol
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2 - 3 4 - 5 6 - 7 8 -", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main",
@"{
  // Code size      114 (0x72)
  .maxstack  3
  .locals init ((int, int)[,] V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4,
                int V_5, //x1
                int V_6, //x2
                int V_7,
                int V_8)
  IL_0000:  call       ""(int, int)[,] C.M()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_000d:  stloc.1
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0015:  stloc.2
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.0
  IL_0018:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_001d:  stloc.3
  IL_001e:  br.s       IL_006d
  IL_0020:  ldloc.0
  IL_0021:  ldc.i4.1
  IL_0022:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0027:  stloc.s    V_4
  IL_0029:  br.s       IL_0064
  IL_002b:  ldloc.0
  IL_002c:  ldloc.3
  IL_002d:  ldloc.s    V_4
  IL_002f:  call       ""(int, int)[*,*].Get""
  IL_0034:  dup
  IL_0035:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_003a:  stloc.s    V_7
  IL_003c:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0041:  stloc.s    V_8
  IL_0043:  ldloc.s    V_7
  IL_0045:  stloc.s    V_5
  IL_0047:  ldloc.s    V_8
  IL_0049:  stloc.s    V_6
  IL_004b:  ldloc.s    V_5
  IL_004d:  box        ""int""
  IL_0052:  ldloc.s    V_6
  IL_0054:  box        ""int""
  IL_0059:  call       ""void C.Print(object, object)""
  IL_005e:  ldloc.s    V_4
  IL_0060:  ldc.i4.1
  IL_0061:  add
  IL_0062:  stloc.s    V_4
  IL_0064:  ldloc.s    V_4
  IL_0066:  ldloc.2
  IL_0067:  ble.s      IL_002b
  IL_0069:  ldloc.3
  IL_006a:  ldc.i4.1
  IL_006b:  add
  IL_006c:  stloc.3
  IL_006d:  ldloc.3
  IL_006e:  ldloc.1
  IL_006f:  ble.s      IL_0020
  IL_0071:  ret
}");
        }

        [Fact]
        public void ForEachStringDeclarationWithImplicitVarType()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach (var (x1, x2) in M())
        {
            Print(x1, x2);
        }
    }
    static string M() { return ""123""; }
    static void Print(object a, object b) { System.Console.Write(a + "" "" + b + "" - ""); }
}
static class Extension
{
    public static void Deconstruct(this char value, out int item1, out int item2)
    {
        item1 = item2 = System.Int32.Parse(value.ToString());
    }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionForeach(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionForeach(model, x2, x2Ref);

                // extra check on var
                var x12Var = (DeclarationExpressionSyntax)x1.Parent.Parent;
                Assert.Equal("var", x12Var.Type.ToString());
                Assert.Null(model.GetSymbolInfo(x12Var.Type).Symbol); // The var in `var (x1, x2)` has no symbol
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 1 - 2 2 - 3 3 - ", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef }, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main",
@"{
  // Code size       69 (0x45)
  .maxstack  3
  .locals init (string V_0,
                int V_1,
                int V_2, //x1
                int V_3, //x2
                int V_4,
                int V_5,
                int V_6)
  IL_0000:  call       ""string C.M()""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  IL_0008:  br.s       IL_003b
  IL_000a:  ldloc.0
  IL_000b:  ldloc.1
  IL_000c:  callvirt   ""char string.this[int].get""
  IL_0011:  ldloca.s   V_4
  IL_0013:  ldloca.s   V_5
  IL_0015:  call       ""void Extension.Deconstruct(char, out int, out int)""
  IL_001a:  ldloc.s    V_4
  IL_001c:  ldloc.s    V_5
  IL_001e:  stloc.s    V_6
  IL_0020:  dup
  IL_0021:  stloc.2
  IL_0022:  ldloc.s    V_6
  IL_0024:  stloc.3
  IL_0025:  pop
  IL_0026:  ldloc.2
  IL_0027:  box        ""int""
  IL_002c:  ldloc.3
  IL_002d:  box        ""int""
  IL_0032:  call       ""void C.Print(object, object)""
  IL_0037:  ldloc.1
  IL_0038:  ldc.i4.1
  IL_0039:  add
  IL_003a:  stloc.1
  IL_003b:  ldloc.1
  IL_003c:  ldloc.0
  IL_003d:  callvirt   ""int string.Length.get""
  IL_0042:  blt.s      IL_000a
  IL_0044:  ret
}");
        }

        [Fact]
        public void ForEachIEnumerableDeclarationWithNesting()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    static void Main()
    {
        foreach ((int x1, var (x2, x3), (int x4, int x5)) in M())
        {
            System.Console.Write(x1 + "" "" + x2 + "" "" + x3 + "" "" + x4 + "" "" + x5 + "" - "");
        }
    }
    static IEnumerable<(int, (int, int), (int, int))> M() { yield return (1, (2, 3), (4, 5)); yield return (6, (7, 8), (9, 10)); }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionForeach(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionForeach(model, x2, x2Ref);

                var x3 = GetDeconstructionVariable(tree, "x3");
                var x3Ref = GetReference(tree, "x3");
                VerifyModelForDeconstructionForeach(model, x3, x3Ref);

                var x4 = GetDeconstructionVariable(tree, "x4");
                var x4Ref = GetReference(tree, "x4");
                VerifyModelForDeconstructionForeach(model, x4, x4Ref);

                var x5 = GetDeconstructionVariable(tree, "x5");
                var x5Ref = GetReference(tree, "x5");
                VerifyModelForDeconstructionForeach(model, x5, x5Ref);

                // extra check on var
                var x23Var = (DeclarationExpressionSyntax)x2.Parent.Parent;
                Assert.Equal("var", x23Var.Type.ToString());
                Assert.Null(model.GetSymbolInfo(x23Var.Type).Symbol); // The var in `var (x2, x3)` has no symbol
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2 3 4 5 - 6 7 8 9 10 -", additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForEachSZArrayDeclarationWithNesting()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, var (x2, x3), (int x4, int x5)) in M())
        {
            System.Console.Write(x1 + "" "" + x2 + "" "" + x3 + "" "" + x4 + "" "" + x5 + "" - "");
        }
    }
    static (int, (int, int), (int, int))[] M() { return new[] { (1, (2, 3), (4, 5)), (6, (7, 8), (9, 10)) }; }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 2 3 4 5 - 6 7 8 9 10 -", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForEachMDArrayDeclarationWithNesting()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, var (x2, x3), (int x4, int x5)) in M())
        {
            System.Console.Write(x1 + "" "" + x2 + "" "" + x3 + "" "" + x4 + "" "" + x5 + "" - "");
        }
    }
    static (int, (int, int), (int, int))[,] M() { return new(int, (int, int), (int, int))[1, 2] { { (1, (2, 3), (4, 5)), (6, (7, 8), (9, 10)) } }; }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 2 3 4 5 - 6 7 8 9 10 -", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForEachStringDeclarationWithNesting()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, var (x2, x3)) in M())
        {
            System.Console.Write(x1 + "" "" + x2 + "" "" + x3 + "" - "");
        }
    }
    static string M() { return ""12""; }
}
static class Extension
{
    public static void Deconstruct(this char value, out int item1, out (int, int) item2)
    {
        item1 = System.Int32.Parse(value.ToString());
        item2 = (item1, item1);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 1 1 - 2 2 2 - ", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForEachIEnumerableDeclarationWithDeconstruct()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    static void Main()
    {
        foreach ((long x1, var (x2, x3)) in M())
        {
            Print(x1, x2, x3);
        }
    }
    static IEnumerable<Pair<int, Pair<int, int>>> M() { yield return Pair.Create(1, Pair.Create(2, 3)); yield return Pair.Create(4, Pair.Create(5, 6)); }
    static void Print(object a, object b, object c) { System.Console.WriteLine(a + "" "" + b + "" "" + c); }
}
" + commonSource;

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionForeach(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionForeach(model, x2, x2Ref);

                var x3 = GetDeconstructionVariable(tree, "x3");
                var x3Ref = GetReference(tree, "x3");
                VerifyModelForDeconstructionForeach(model, x3, x3Ref);

                // extra check on var
                var x23Var = (DeclarationExpressionSyntax)x2.Parent.Parent;
                Assert.Equal("var", x23Var.Type.ToString());
                Assert.Null(model.GetSymbolInfo(x23Var.Type).Symbol); // The var in `var (x2, x3)` has no symbol
            };

            string expected =
@"Deconstructing (1, (2, 3))
Deconstructing (2, 3)
1 2 3
Deconstructing (4, (5, 6))
Deconstructing (5, 6)
4 5 6";

            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main",
@"{
  // Code size      103 (0x67)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerator<Pair<int, Pair<int, int>>> V_0,
                long V_1, //x1
                int V_2, //x2
                int V_3, //x3
                int V_4,
                Pair<int, int> V_5,
                int V_6,
                int V_7,
                int V_8,
                int V_9)
  IL_0000:  call       ""System.Collections.Generic.IEnumerable<Pair<int, Pair<int, int>>> C.M()""
  IL_0005:  callvirt   ""System.Collections.Generic.IEnumerator<Pair<int, Pair<int, int>>> System.Collections.Generic.IEnumerable<Pair<int, Pair<int, int>>>.GetEnumerator()""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  br.s       IL_0052
    IL_000d:  ldloc.0
    IL_000e:  callvirt   ""Pair<int, Pair<int, int>> System.Collections.Generic.IEnumerator<Pair<int, Pair<int, int>>>.Current.get""
    IL_0013:  ldloca.s   V_4
    IL_0015:  ldloca.s   V_5
    IL_0017:  callvirt   ""void Pair<int, Pair<int, int>>.Deconstruct(out int, out Pair<int, int>)""
    IL_001c:  ldloc.s    V_5
    IL_001e:  ldloca.s   V_6
    IL_0020:  ldloca.s   V_7
    IL_0022:  callvirt   ""void Pair<int, int>.Deconstruct(out int, out int)""
    IL_0027:  ldloc.s    V_4
    IL_0029:  conv.i8
    IL_002a:  ldloc.s    V_6
    IL_002c:  stloc.s    V_8
    IL_002e:  ldloc.s    V_7
    IL_0030:  stloc.s    V_9
    IL_0032:  dup
    IL_0033:  stloc.1
    IL_0034:  ldloc.s    V_8
    IL_0036:  stloc.2
    IL_0037:  ldloc.s    V_9
    IL_0039:  stloc.3
    IL_003a:  pop
    IL_003b:  ldloc.1
    IL_003c:  box        ""long""
    IL_0041:  ldloc.2
    IL_0042:  box        ""int""
    IL_0047:  ldloc.3
    IL_0048:  box        ""int""
    IL_004d:  call       ""void C.Print(object, object, object)""
    IL_0052:  ldloc.0
    IL_0053:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0058:  brtrue.s   IL_000d
    IL_005a:  leave.s    IL_0066
  }
  finally
  {
    IL_005c:  ldloc.0
    IL_005d:  brfalse.s  IL_0065
    IL_005f:  ldloc.0
    IL_0060:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0065:  endfinally
  }
  IL_0066:  ret
}
");
        }

        [Fact]
        public void ForEachSZArrayDeclarationWithDeconstruct()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, var (x2, x3)) in M())
        {
            System.Console.WriteLine(x1 + "" "" + x2 + "" "" + x3);
        }
    }
    static Pair<int, Pair<int, int>>[] M() { return new[] { Pair.Create(1, Pair.Create(2, 3)), Pair.Create(4, Pair.Create(5, 6)) }; }
}
" + commonSource;

            string expected =
@"Deconstructing (1, (2, 3))
Deconstructing (2, 3)
1 2 3
Deconstructing (4, (5, 6))
Deconstructing (5, 6)
4 5 6";

            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForEachMDArrayDeclarationWithDeconstruct()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, var (x2, x3)) in M())
        {
            System.Console.WriteLine(x1 + "" "" + x2 + "" "" + x3);
        }
    }
    static Pair<int, Pair<int, int>>[,] M() { return new Pair<int, Pair<int, int>> [1, 2] { { Pair.Create(1, Pair.Create(2, 3)), Pair.Create(4, Pair.Create(5, 6)) } }; }
}
" + commonSource;

            string expected =
@"Deconstructing (1, (2, 3))
Deconstructing (2, 3)
1 2 3
Deconstructing (4, (5, 6))
Deconstructing (5, 6)
4 5 6";

            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForEachWithExpressionBody()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach (var (x1, x2) in new[] { (1, 2), (3, 4) })
            System.Console.Write(x1 + "" "" + x2 + "" - "");
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 2 - 3 4 -", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForEachCreatesNewVariables()
        {
            string source = @"
class C
{
    static void Main()
    {
        var lambdas = new System.Action[2];
        int index = 0;
        foreach (var (x1, x2) in M())
        {
            lambdas[index] = () => { System.Console.Write(x1 + "" ""); };
            index++;
        }
        lambdas[0]();
        lambdas[1]();
    }
    static (int, int)[] M() { return new[] { (0, 0), (10, 10) }; }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "0 10 ", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TupleDeconstructionIntoDynamicArrayIndexer()
        {
            string source = @"
class C
{
    static void Main()
    {
        dynamic x = new string[] { """", """" };
        M(x);
        System.Console.WriteLine($""{x[0]} {x[1]}"");
    }

    static void M(dynamic x)
    {
        (x[0], x[1]) = (""hello"", ""world"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "hello world", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, CSharpRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void IntTupleDeconstructionIntoDynamicArrayIndexer()
        {
            string source = @"
class C
{
    static void Main()
    {
        dynamic x = new int[] { 1, 2 };
        M(x);
        System.Console.WriteLine($""{x[0]} {x[1]}"");
    }

    static void M(dynamic x)
    {
        (x[0], x[1]) = (3, 4);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "3 4", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, CSharpRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructionIntoDynamicArrayIndexer()
        {
            string source = @"
class C
{
    static void Main()
    {
        dynamic x = new string[] { """", """" };
        M(x);
        System.Console.WriteLine($""{x[0]} {x[1]}"");
    }

    static void M(dynamic x)
    {
        (x[0], x[1]) = new C();
    }

    public void Deconstruct(out string a, out string b)
    {
        a = ""hello"";
        b = ""world"";
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "hello world", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, CSharpRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TupleDeconstructionIntoDynamicArray()
        {
            string source = @"
class C
{
    static void Main()
    {
        dynamic[] x = new string[] { """", """" };
        M(x);
        System.Console.WriteLine($""{x[0]} {x[1]}"");
    }

    static void M(dynamic[] x)
    {
        (x[0], x[1]) = (""hello"", ""world"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "hello world", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, CSharpRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructionIntoDynamicArray()
        {
            string source = @"
class C
{
    static void Main()
    {
        dynamic[] x = new string[] { """", """" };
        M(x);
        System.Console.WriteLine($""{x[0]} {x[1]}"");
    }

    static void M(dynamic[] x)
    {
        (x[0], x[1]) = new C();
    }

    public void Deconstruct(out string a, out string b)
    {
        a = ""hello"";
        b = ""world"";
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "hello world", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, CSharpRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructionIntoDynamicMember()
        {
            string source = @"
class C
{
    static void Main()
    {
        dynamic x = System.ValueTuple.Create(1, 2);
        (x.Item1, x.Item2) = new C();
        System.Console.WriteLine($""{x.Item1} {x.Item2}"");
    }

    public void Deconstruct(out int a, out int b)
    {
        a = 3;
        b = 4;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "3 4", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, CSharpRef, SystemCoreRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void FieldAndLocalWithSameName()
        {
            string source = @"
class C
{
    public int x = 3;
    static void Main()
    {
        new C().M();
    }
    void M()
    {
        var (x, y) = (1, 2);
        System.Console.Write($""{x} {y} {this.x}"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1 2 3", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NoGlobalDeconstructionUnlessScript()
        {
            string source = @"
class C
{
    var (x, y) = (1, 2);
}
";
            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (3,2): error CS1520: Method must have a return type
                // {
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "").WithLocation(3, 2),
                // (4,11): error CS1001: Identifier expected
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(4, 11),
                // (4,14): error CS1001: Identifier expected
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(4, 14),
                // (4,16): error CS1002: ; expected
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=").WithLocation(4, 16),
                // (4,16): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(4, 16),
                // (4,18): error CS8124: Tuple must contain at least two elements.
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, "(").WithLocation(4, 18),
                // (4,19): error CS1031: Type expected
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_TypeExpected, "1").WithLocation(4, 19),
                // (4,19): error CS1026: ) expected
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "1").WithLocation(4, 19),
                // (4,19): error CS1519: Invalid token '1' in class, struct, or interface member declaration
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "1").WithArguments("1").WithLocation(4, 19),
                // (4,10): error CS0246: The type or namespace name 'x' could not be found (are you missing a using directive or an assembly reference?)
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "x").WithArguments("x").WithLocation(4, 10),
                // (4,13): error CS0246: The type or namespace name 'y' could not be found (are you missing a using directive or an assembly reference?)
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "y").WithArguments("y").WithLocation(4, 13),
                // (4,5): error CS0501: 'C.var(x, y)' must declare a body because it is not marked abstract, extern, or partial
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "var").WithArguments("C.var(x, y)").WithLocation(4, 5)
                );

            var nodes = comp.SyntaxTrees[0].GetCompilationUnitRoot().DescendantNodesAndSelf();
            Assert.False(nodes.Any(n => n.Kind() == SyntaxKind.SimpleAssignmentExpression));
        }

        [Fact]
        public void SimpleDeconstructionInScript()
        {
            var source =
@"
using alias = System.Int32;
(string x, alias y) = (""hello"", 42);
System.Console.Write($""{x} {y}"");
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x = GetDeconstructionVariable(tree, "x");
                var xSymbol = model.GetDeclaredSymbol(x);
                var xRef = GetReference(tree, "x");
                Assert.Equal("System.String Script.x", xSymbol.ToTestDisplayString());
                VerifyModelForDeconstructionField(model, x, xRef);

                var y = GetDeconstructionVariable(tree, "y");
                var ySymbol = model.GetDeclaredSymbol(y);
                var yRef = GetReference(tree, "y");
                Assert.Equal("System.Int32 Script.y", ySymbol.ToTestDisplayString());
                VerifyModelForDeconstructionField(model, y, yRef);

                // extra checks on x
                var xType = GetTypeSyntax(x);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(xType).Symbol.Kind);
                Assert.Equal("string", model.GetSymbolInfo(xType).Symbol.ToDisplayString());
                Assert.Null(model.GetAliasInfo(xType));

                // extra checks on y
                var yType = GetTypeSyntax(y);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(yType).Symbol.Kind);
                Assert.Equal("int", model.GetSymbolInfo(yType).Symbol.ToDisplayString());
                Assert.Equal("alias=System.Int32", model.GetAliasInfo(yType).ToTestDisplayString());
            };

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "hello 42", sourceSymbolValidator: validator);
            verifier.VerifyIL("<<Initialize>>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      151 (0x97)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                string V_2,
                int V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int <<Initialize>>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldstr      ""hello""
    IL_000c:  ldc.i4.s   42
    IL_000e:  newobj     ""System.ValueTuple<string, int>..ctor(string, int)""
    IL_0013:  dup
    IL_0014:  ldfld      ""string System.ValueTuple<string, int>.Item1""
    IL_0019:  stloc.2
    IL_001a:  ldfld      ""int System.ValueTuple<string, int>.Item2""
    IL_001f:  stloc.3
    IL_0020:  ldarg.0
    IL_0021:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_0026:  ldloc.2
    IL_0027:  stfld      ""string x""
    IL_002c:  ldarg.0
    IL_002d:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_0032:  ldloc.3
    IL_0033:  stfld      ""int y""
    IL_0038:  ldstr      ""{0} {1}""
    IL_003d:  ldarg.0
    IL_003e:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_0043:  ldfld      ""string x""
    IL_0048:  ldarg.0
    IL_0049:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_004e:  ldfld      ""int y""
    IL_0053:  box        ""int""
    IL_0058:  call       ""string string.Format(string, object, object)""
    IL_005d:  call       ""void System.Console.Write(string)""
    IL_0062:  nop
    IL_0063:  ldnull
    IL_0064:  stloc.1
    IL_0065:  leave.s    IL_0081
  }
  catch System.Exception
  {
    IL_0067:  stloc.s    V_4
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.s   -2
    IL_006c:  stfld      ""int <<Initialize>>d__0.<>1__state""
    IL_0071:  ldarg.0
    IL_0072:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
    IL_0077:  ldloc.s    V_4
    IL_0079:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetException(System.Exception)""
    IL_007e:  nop
    IL_007f:  leave.s    IL_0096
  }
  IL_0081:  ldarg.0
  IL_0082:  ldc.i4.s   -2
  IL_0084:  stfld      ""int <<Initialize>>d__0.<>1__state""
  IL_0089:  ldarg.0
  IL_008a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
  IL_008f:  ldloc.1
  IL_0090:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetResult(object)""
  IL_0095:  nop
  IL_0096:  ret
}");
        }

        [Fact]
        public void NoGlobalDeconstructionOutsideScript()
        {
            var source =
@"
(string x, int y) = (""hello"", 42);
";
            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular, options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics(
                // (2,19): error CS1022: Type or namespace definition, or end-of-file expected
                // (string x, int y) = ("hello", 42);
                Diagnostic(ErrorCode.ERR_EOFExpected, "=").WithLocation(2, 19),
                // (2,22): error CS1022: Type or namespace definition, or end-of-file expected
                // (string x, int y) = ("hello", 42);
                Diagnostic(ErrorCode.ERR_EOFExpected, @"""hello""").WithLocation(2, 22),
                // (2,22): error CS1026: ) expected
                // (string x, int y) = ("hello", 42);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, @"""hello""").WithLocation(2, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1)
                );

            var nodes = comp.SyntaxTrees[0].GetCompilationUnitRoot().DescendantNodesAndSelf();
            Assert.False(nodes.Any(n => n.Kind() == SyntaxKind.SimpleAssignmentExpression));
        }

        [Fact]
        public void NestedDeconstructionInScript()
        {
            var source =
@"
(string x, (int y, int z)) = (""hello"", (42, 43));
System.Console.Write($""{x} {y} {z}"");
";
            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "hello 42 43");
        }

        [Fact]
        public void VarDeconstructionInScript()
        {
            var source =
@"
(var x, var y) = (""hello"", 42);
System.Console.Write($""{x} {y}"");
";
            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "hello 42");
        }

        [Fact]
        public void NestedVarDeconstructionInScript()
        {
            var source =
@"
(var x1, var (x2, x3)) = (""hello"", (42, 43));
System.Console.Write($""{x1} {x2} {x3}"");
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Symbol = model.GetDeclaredSymbol(x1);
                var x1Ref = GetReference(tree, "x1");
                Assert.Equal("System.String Script.x1", x1Symbol.ToTestDisplayString());
                VerifyModelForDeconstructionField(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Symbol = model.GetDeclaredSymbol(x2);
                var x2Ref = GetReference(tree, "x2");
                Assert.Equal("System.Int32 Script.x2", x2Symbol.ToTestDisplayString());
                VerifyModelForDeconstructionField(model, x2, x2Ref);

                // extra checks on x1's var
                var x1Type = GetTypeSyntax(x1);
                Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x1Type).Symbol.Kind);
                Assert.Equal("string", model.GetSymbolInfo(x1Type).Symbol.ToDisplayString());
                Assert.Null(model.GetAliasInfo(x1Type));

                // extra check on x2 and x3's var
                var x23Var = (DeclarationExpressionSyntax)x2.Parent.Parent;
                Assert.Equal("var", x23Var.Type.ToString());
                Assert.Null(model.GetSymbolInfo(x23Var.Type).Symbol); // The var in `var (x2, x3)` has no symbol
            };

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "hello 42 43", sourceSymbolValidator: validator);
        }

        [Fact]
        public void EvaluationOrderForDeconstructionInScript()
        {
            var source =
    @"
(int, int) M(out int x) { x = 1; return (2, 3); }
var (x2, x3) = M(out var x1);
System.Console.Write($""{x1} {x2} {x3}"");
";
            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "1 2 3");
            verifier.VerifyIL("<<Initialize>>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      182 (0xb6)
  .maxstack  4
  .locals init (int V_0,
                object V_1,
                int V_2,
                int V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int <<Initialize>>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldarg.0
    IL_0008:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_000d:  ldarg.0
    IL_000e:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_0013:  ldflda     ""int x1""
    IL_0018:  call       ""(int, int) M(out int)""
    IL_001d:  dup
    IL_001e:  ldfld      ""int System.ValueTuple<int, int>.Item1""
    IL_0023:  stloc.2
    IL_0024:  ldfld      ""int System.ValueTuple<int, int>.Item2""
    IL_0029:  stloc.3
    IL_002a:  ldarg.0
    IL_002b:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_0030:  ldloc.2
    IL_0031:  stfld      ""int x2""
    IL_0036:  ldarg.0
    IL_0037:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_003c:  ldloc.3
    IL_003d:  stfld      ""int x3""
    IL_0042:  ldstr      ""{0} {1} {2}""
    IL_0047:  ldarg.0
    IL_0048:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_004d:  ldfld      ""int x1""
    IL_0052:  box        ""int""
    IL_0057:  ldarg.0
    IL_0058:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_005d:  ldfld      ""int x2""
    IL_0062:  box        ""int""
    IL_0067:  ldarg.0
    IL_0068:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_006d:  ldfld      ""int x3""
    IL_0072:  box        ""int""
    IL_0077:  call       ""string string.Format(string, object, object, object)""
    IL_007c:  call       ""void System.Console.Write(string)""
    IL_0081:  nop
    IL_0082:  ldnull
    IL_0083:  stloc.1
    IL_0084:  leave.s    IL_00a0
  }
  catch System.Exception
  {
    IL_0086:  stloc.s    V_4
    IL_0088:  ldarg.0
    IL_0089:  ldc.i4.s   -2
    IL_008b:  stfld      ""int <<Initialize>>d__0.<>1__state""
    IL_0090:  ldarg.0
    IL_0091:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
    IL_0096:  ldloc.s    V_4
    IL_0098:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetException(System.Exception)""
    IL_009d:  nop
    IL_009e:  leave.s    IL_00b5
  }
  IL_00a0:  ldarg.0
  IL_00a1:  ldc.i4.s   -2
  IL_00a3:  stfld      ""int <<Initialize>>d__0.<>1__state""
  IL_00a8:  ldarg.0
  IL_00a9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
  IL_00ae:  ldloc.1
  IL_00af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetResult(object)""
  IL_00b4:  nop
  IL_00b5:  ret
}
");
        }

        [Fact]
        public void DeconstructionForEachInScript()
        {
            var source =
@"
foreach ((string x1, var (x2, x3)) in new[] { (""hello"", (42, ""world"")) })
{
    System.Console.Write($""{x1} {x2} {x3}"");
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Symbol = model.GetDeclaredSymbol(x1);
                Assert.Equal("System.String x1", x1Symbol.ToTestDisplayString());
                Assert.Equal(SymbolKind.Local, x1Symbol.Kind);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Symbol = model.GetDeclaredSymbol(x2);
                Assert.Equal("System.Int32 x2", x2Symbol.ToTestDisplayString());
                Assert.Equal(SymbolKind.Local, x2Symbol.Kind);
            };

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "hello 42 world", sourceSymbolValidator: validator);
        }

        [Fact]
        public void DeconstructionInForLoopInScript()
        {
            var source =
@"
for ((string x1, var (x2, x3)) = (""hello"", (42, ""world"")); ; )
{
    System.Console.Write($""{x1} {x2} {x3}"");
    break;
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x1 = GetDeconstructionVariable(tree, "x1");
                var x1Symbol = model.GetDeclaredSymbol(x1);
                Assert.Equal("System.String x1", x1Symbol.ToTestDisplayString());
                Assert.Equal(SymbolKind.Local, x1Symbol.Kind);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Symbol = model.GetDeclaredSymbol(x2);
                Assert.Equal("System.Int32 x2", x2Symbol.ToTestDisplayString());
                Assert.Equal(SymbolKind.Local, x2Symbol.Kind);
            };

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "hello 42 world", sourceSymbolValidator: validator);
        }

        [Fact]
        public void DeconstructionInCSharp6Script()
        {
            var source =
@"
var (x, y) = (1, 2);
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp6), options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics(
                // (2,1): error CS8059: Feature 'tuples' is not available in C# 6.  Please use language version 7 or greater.
                // var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "var (x, y)").WithArguments("tuples", "7").WithLocation(2, 1),
                // (2,14): error CS8059: Feature 'tuples' is not available in C# 6.  Please use language version 7 or greater.
                // var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(1, 2)").WithArguments("tuples", "7").WithLocation(2, 14)
                );
        }

        [Fact]
        public void InvalidDeconstructionInScript()
        {
            var source =
@"
int (x, y) = (1, 2);
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (2,5): error CS8136: Deconstruction 'var (...)' form disallows a specific type for 'var'.
                // int (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "(x, y)").WithLocation(2, 5)
                );


            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = GetDeconstructionVariable(tree, "x");
            var xSymbol = model.GetDeclaredSymbol(x);
            Assert.Equal("System.Int32 Script.x", xSymbol.ToTestDisplayString());
            var xType = ((FieldSymbol)xSymbol).Type;
            Assert.False(xType.IsErrorType());
            Assert.Equal("System.Int32", xType.ToTestDisplayString());

            var y = GetDeconstructionVariable(tree, "y");
            var ySymbol = model.GetDeclaredSymbol(y);
            Assert.Equal("System.Int32 Script.y", ySymbol.ToTestDisplayString());
            var yType = ((FieldSymbol)ySymbol).Type;
            Assert.False(yType.IsErrorType());
            Assert.Equal("System.Int32", yType.ToTestDisplayString());
        }

        [Fact]
        public void InvalidDeconstructionInScript_2()
        {
            var source =
@"
(int (x, y), int z) = ((1, 2), 3);
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (2,6): error CS8136: Deconstruction 'var (...)' form disallows a specific type for 'var'.
                // (int (x, y), int z) = ((1, 2), 3);
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "(x, y)").WithLocation(2, 6)
                );


            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = GetDeconstructionVariable(tree, "x");
            var xSymbol = model.GetDeclaredSymbol(x);
            Assert.Equal("System.Int32 Script.x", xSymbol.ToTestDisplayString());
            var xType = ((FieldSymbol)xSymbol).Type;
            Assert.False(xType.IsErrorType());
            Assert.Equal("System.Int32", xType.ToTestDisplayString());

            var y = GetDeconstructionVariable(tree, "y");
            var ySymbol = model.GetDeclaredSymbol(y);
            Assert.Equal("System.Int32 Script.y", ySymbol.ToTestDisplayString());
            var yType = ((FieldSymbol)ySymbol).Type;
            Assert.False(yType.IsErrorType());
            Assert.Equal("System.Int32", yType.ToTestDisplayString());
        }

        [Fact]
        public void NameConflictInDeconstructionInScript()
        {
            var source =
@"
int x1;
var (x1, x2) = (1, 2);
System.Console.Write(x1);
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (3,6): error CS0102: The type 'Script' already contains a definition for 'x1'
                // var (x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x1").WithArguments("Script", "x1").WithLocation(3, 6),
                // (4,22): error CS0229: Ambiguity between 'x1' and 'x1'
                // System.Console.Write(x1);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x1").WithArguments("x1", "x1").WithLocation(4, 22)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var firstX1 = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(d => d.Identifier.ValueText == "x1").Single();
            var firstX1Symbol = model.GetDeclaredSymbol(firstX1);
            Assert.Equal("System.Int32 Script.x1", firstX1Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, firstX1Symbol.Kind);

            var secondX1 = GetDeconstructionVariable(tree, "x1");
            var secondX1Symbol = model.GetDeclaredSymbol(secondX1);
            Assert.Equal("System.Int32 Script.x1", secondX1Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, secondX1Symbol.Kind);

            Assert.NotEqual(firstX1Symbol, secondX1Symbol);
        }

        [Fact]
        public void NameConflictInDeconstructionInScript2()
        {
            var source =
@"
var (x, y) = (1, 2);
var (z, y) = (1, 2);
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (3,9): error CS0102: The type 'Script' already contains a definition for 'y'
                // var (z, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "y").WithArguments("Script", "y").WithLocation(3, 9)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var firstY = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(d => d.Identifier.ValueText == "y").First();
            var firstYSymbol = model.GetDeclaredSymbol(firstY);
            Assert.Equal("System.Int32 Script.y", firstYSymbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, firstYSymbol.Kind);

            var secondY = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(d => d.Identifier.ValueText == "y").ElementAt(1);
            var secondYSymbol = model.GetDeclaredSymbol(secondY);
            Assert.Equal("System.Int32 Script.y", secondYSymbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, secondYSymbol.Kind);

            Assert.NotEqual(firstYSymbol, secondYSymbol);
        }

        [Fact]
        public void NameConflictInDeconstructionInScript3()
        {
            var source =
@"
var (x, (y, x)) = (1, (2, ""hello""));
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (2,13): error CS0102: The type 'Script' already contains a definition for 'x'
                // var (x, (y, x)) = (1, (2, "hello"));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x").WithArguments("Script", "x").WithLocation(2, 13)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var firstX = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(d => d.Identifier.ValueText == "x").First();
            var firstXSymbol = model.GetDeclaredSymbol(firstX);
            Assert.Equal("System.Int32 Script.x", firstXSymbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, firstXSymbol.Kind);

            var secondX = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(d => d.Identifier.ValueText == "x").ElementAt(1);
            var secondXSymbol = model.GetDeclaredSymbol(secondX);
            Assert.Equal("System.String Script.x", secondXSymbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, secondXSymbol.Kind);

            Assert.NotEqual(firstXSymbol, secondXSymbol);
        }

        [Fact]
        public void UnassignedUsedInDeconstructionInScript()
        {
            var source =
@"
System.Console.Write(x);
var (x, y) = (1, 2);
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = GetDeconstructionVariable(tree, "x");
            var xSymbol = model.GetDeclaredSymbol(x);
            var xRef = GetReference(tree, "x");
            Assert.Equal("System.Int32 Script.x", xSymbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x, xRef);
            var xType = ((FieldSymbol)xSymbol).Type;
            Assert.False(xType.IsErrorType());
            Assert.Equal("System.Int32", xType.ToTestDisplayString());
        }

        [Fact]
        public void FailedInferenceInDeconstructionInScript()
        {
            var source =
@"
var (x, y) = (1, null);
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.GetDeclarationDiagnostics().Verify(
                // (2,6): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x'.
                // var (x, y) = (1, null);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x").WithArguments("x").WithLocation(2, 6),
                // (2,9): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y'.
                // var (x, y) = (1, null);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y").WithArguments("y").WithLocation(2, 9)
                );

            comp.VerifyDiagnostics(
                // (2,6): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x'.
                // var (x, y) = (1, null);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x").WithArguments("x").WithLocation(2, 6),
                // (2,9): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y'.
                // var (x, y) = (1, null);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y").WithArguments("y").WithLocation(2, 9)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = GetDeconstructionVariable(tree, "x");
            var xSymbol = model.GetDeclaredSymbol(x);
            Assert.Equal("var Script.x", xSymbol.ToTestDisplayString());
            var xType = ((FieldSymbol)xSymbol).Type;
            Assert.True(xType.IsErrorType());
            Assert.Equal("var", xType.ToTestDisplayString());

            var y = GetDeconstructionVariable(tree, "y");
            var ySymbol = model.GetDeclaredSymbol(y);
            Assert.Equal("var Script.y", ySymbol.ToTestDisplayString());
            var yType = ((FieldSymbol)ySymbol).Type;
            Assert.True(yType.IsErrorType());
            Assert.Equal("var", yType.ToTestDisplayString());
        }

        [Fact]
        public void FailedCircularInferenceInDeconstructionInScript()
        {
            var source =
@"
var (x1, x2) = (x2, x1);
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.GetDeclarationDiagnostics().Verify(
                // (2,10): error CS7019: Type of 'x2' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // var (x1, x2) = (x2, x1);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "x2").WithArguments("x2").WithLocation(2, 10),
                // (2,6): error CS7019: Type of 'x1' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // var (x1, x2) = (x2, x1);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "x1").WithArguments("x1").WithLocation(2, 6)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Symbol = model.GetDeclaredSymbol(x1);
            var x1Ref = GetReference(tree, "x1");
            Assert.Equal("var Script.x1", x1Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x1, x1Ref);
            var x1Type = ((FieldSymbol)x1Symbol).Type;
            Assert.True(x1Type.IsErrorType());
            Assert.Equal("var", x1Type.Name);

            var x2 = GetDeconstructionVariable(tree, "x2");
            var x2Symbol = model.GetDeclaredSymbol(x2);
            var x2Ref = GetReference(tree, "x2");
            Assert.Equal("var Script.x2", x2Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x2, x2Ref);
            var x2Type = ((FieldSymbol)x2Symbol).Type;
            Assert.True(x2Type.IsErrorType());
            Assert.Equal("var", x2Type.Name);
        }

        [Fact]
        public void FailedCircularInferenceInDeconstructionInScript2()
        {
            var source =
@"
var (x1, x2) = (y1, y2);
var (y1, y2) = (x1, x2);
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.GetDeclarationDiagnostics().Verify(
                // (3,6): error CS7019: Type of 'y1' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // var (y1, y2) = (x1, x2);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "y1").WithArguments("y1").WithLocation(3, 6),
                // (2,6): error CS7019: Type of 'x1' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // var (x1, x2) = (y1, y2);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "x1").WithArguments("x1").WithLocation(2, 6),
                // (2,10): error CS7019: Type of 'x2' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // var (x1, x2) = (y1, y2);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "x2").WithArguments("x2").WithLocation(2, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Symbol = model.GetDeclaredSymbol(x1);
            var x1Ref = GetReference(tree, "x1");
            Assert.Equal("var Script.x1", x1Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x1, x1Ref);
            var x1Type = ((FieldSymbol)x1Symbol).Type;
            Assert.True(x1Type.IsErrorType());
            Assert.Equal("var", x1Type.Name);

            var x2 = GetDeconstructionVariable(tree, "x2");
            var x2Symbol = model.GetDeclaredSymbol(x2);
            var x2Ref = GetReference(tree, "x2");
            Assert.Equal("var Script.x2", x2Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x2, x2Ref);
            var x2Type = ((FieldSymbol)x2Symbol).Type;
            Assert.True(x2Type.IsErrorType());
            Assert.Equal("var", x2Type.Name);
        }

        [Fact]
        public void VarAliasInVarDeconstructionInScript()
        {
            var source =
@"
using var = System.Byte;
var (x1, (x2, x3)) = (1, (2, 3));
System.Console.Write($""{x1} {x2} {x3}"");
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (3,5): error CS8136: Deconstruction 'var (...)' form disallows a specific type for 'var'.
                // var (x1, (x2, x3)) = (1, (2, 3));
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "(x1, (x2, x3))").WithLocation(3, 5)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Symbol = model.GetDeclaredSymbol(x1);
            var x1Ref = GetReference(tree, "x1");
            Assert.Equal("System.Byte Script.x1", x1Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x1, x1Ref);

            var x3 = GetDeconstructionVariable(tree, "x3");
            var x3Symbol = model.GetDeclaredSymbol(x3);
            var x3Ref = GetReference(tree, "x3");
            Assert.Equal("System.Byte Script.x3", x3Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x3, x3Ref);

            // extra check on var
            var x123Var = (DeclarationExpressionSyntax)x1.Parent.Parent;
            Assert.Equal("var", x123Var.Type.ToString());
            Assert.Null(model.GetSymbolInfo(x123Var.Type).Symbol); // The var in `var (x1, x2)` has no symbol
        }

        [Fact]
        public void VarTypeInVarDeconstructionInScript()
        {
            var source =
@"
class var
{
    public static implicit operator var(int i) { return null; }
}
var (x1, (x2, x3)) = (1, (2, 3));
System.Console.Write($""{x1} {x2} {x3}"");
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (6,5): error CS8136: Deconstruction 'var (...)' form disallows a specific type for 'var'.
                // var (x1, (x2, x3)) = (1, (2, 3));
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "(x1, (x2, x3))").WithLocation(6, 5)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Symbol = model.GetDeclaredSymbol(x1);
            var x1Ref = GetReference(tree, "x1");
            Assert.Equal("Script.var Script.x1", x1Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x1, x1Ref);

            var x3 = GetDeconstructionVariable(tree, "x3");
            var x3Symbol = model.GetDeclaredSymbol(x3);
            var x3Ref = GetReference(tree, "x3");
            Assert.Equal("Script.var Script.x3", x3Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x3, x3Ref);

            // extra check on var
            var x123Var = (DeclarationExpressionSyntax)x1.Parent.Parent;
            Assert.Equal("var", x123Var.Type.ToString());
            Assert.Null(model.GetSymbolInfo(x123Var.Type).Symbol); // The var in `var (x1, x2)` has no symbol
        }

        [Fact]
        public void VarAliasInTypedDeconstructionInScript()
        {
            var source =
@"
using var = System.Byte;
(var x1, (var x2, var x3)) = (1, (2, 3));
System.Console.Write($""{x1} {x2} {x3}"");
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2 3");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Symbol = model.GetDeclaredSymbol(x1);
            var x1Ref = GetReference(tree, "x1");
            Assert.Equal("System.Byte Script.x1", x1Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x1, x1Ref);

            var x3 = GetDeconstructionVariable(tree, "x3");
            var x3Symbol = model.GetDeclaredSymbol(x3);
            var x3Ref = GetReference(tree, "x3");
            Assert.Equal("System.Byte Script.x3", x3Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x3, x3Ref);

            // extra checks on x1's var
            var x1Type = GetTypeSyntax(x1);
            Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x1Type).Symbol.Kind);
            Assert.Equal("byte", model.GetSymbolInfo(x1Type).Symbol.ToDisplayString());
            var x1Alias = model.GetAliasInfo(x1Type);
            Assert.Equal(SymbolKind.NamedType, x1Alias.Target.Kind);
            Assert.Equal("byte", x1Alias.Target.ToDisplayString());

            // extra checks on x3's var
            var x3Type = GetTypeSyntax(x3);
            Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x3Type).Symbol.Kind);
            Assert.Equal("byte", model.GetSymbolInfo(x3Type).Symbol.ToDisplayString());
            var x3Alias = model.GetAliasInfo(x3Type);
            Assert.Equal(SymbolKind.NamedType, x3Alias.Target.Kind);
            Assert.Equal("byte", x3Alias.Target.ToDisplayString());
        }

        [Fact]
        public void VarTypeInTypedDeconstructionInScript()
        {
            var source =
@"
class var
{
    public static implicit operator var(int i) { return new var(); }
    public override string ToString() { return ""var""; }
}
(var x1, (var x2, var x3)) = (1, (2, 3));
System.Console.Write($""{x1} {x2} {x3}"");
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "var var var");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Symbol = model.GetDeclaredSymbol(x1);
            var x1Ref = GetReference(tree, "x1");
            Assert.Equal("Script.var Script.x1", x1Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x1, x1Ref);

            var x3 = GetDeconstructionVariable(tree, "x3");
            var x3Symbol = model.GetDeclaredSymbol(x3);
            var x3Ref = GetReference(tree, "x3");
            Assert.Equal("Script.var Script.x3", x3Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x3, x3Ref);

            // extra checks on x1's var
            var x1Type = GetTypeSyntax(x1);
            Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x1Type).Symbol.Kind);
            Assert.Equal("var", model.GetSymbolInfo(x1Type).Symbol.ToDisplayString());
            Assert.Null(model.GetAliasInfo(x1Type));

            // extra checks on x3's var
            var x3Type = GetTypeSyntax(x3);
            Assert.Equal(SymbolKind.NamedType, model.GetSymbolInfo(x3Type).Symbol.Kind);
            Assert.Equal("var", model.GetSymbolInfo(x3Type).Symbol.ToDisplayString());
            Assert.Null(model.GetAliasInfo(x3Type));
        }

        [Fact]
        public void SimpleDiscardWithConversion()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (int _, var x) = (new C(1), 1);
        (var _, var y) = (new C(2), 2);
        var (_, z) = (new C(3), 3);
        System.Console.Write($""Output {x} {y} {z}."");
    }
    int _i;
    public C(int i) { _i = i; }
    public static implicit operator int(C c) { System.Console.Write($""Converted {c._i}. ""); return 0; }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Converted 1. Output 1 2 3.");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard1 = GetDiscardDesignations(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            var declaration1 = (DeclarationExpressionSyntax)discard1.Parent;
            Assert.Equal("int _", declaration1.ToString());
            //Assert.Equal("", model.GetTypeInfo(declaration1).Type.ToTestDisplayString()); // https://github.com/dotnet/roslyn/issues/15450

            var discard2 = GetDiscardDesignations(tree).ElementAt(1);
            Assert.Null(model.GetDeclaredSymbol(discard2));
            var declaration2 = (DeclarationExpressionSyntax)discard2.Parent;
            Assert.Equal("var _", declaration2.ToString());
            //Assert.Equal("", model.GetTypeInfo(declaration2).Type.ToTestDisplayString()); //  https://github.com/dotnet/roslyn/issues/15450

            var discard3 = GetDiscardDesignations(tree).ElementAt(2);
            var declaration3 = (DeclarationExpressionSyntax)discard3.Parent.Parent;
            Assert.Equal("var (_, z)", declaration3.ToString());
            //Assert.Equal("", model.GetTypeInfo(var_2).Type.ToTestDisplayString()); //  https://github.com/dotnet/roslyn/issues/15450
        }

        [Fact]
        public void CannotDeconstructIntoDiscardOfWrongType()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (int _, string _) = (""hello"", 42);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (6,30): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         (int _, string _) = ("hello", 42);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "int").WithLocation(6, 30),
                // (6,39): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         (int _, string _) = ("hello", 42);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "42").WithArguments("int", "string").WithLocation(6, 39)
                );
        }

        [Fact]
        public void DiscardFromDeconstructMethod()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (var _, string y) = new C();
        System.Console.Write(y);
    }
    void Deconstruct(out int x, out string y) { x = 42; y = ""hello""; }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "hello");
        }

        [Fact]
        public void ShortDiscardInDeclaration()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (_, var x) = (1, 2);
        System.Console.Write(x);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard = GetDiscardIdentifiers(tree).First();
            var symbol = (IDiscardedSymbol)model.GetSymbolInfo(discard).Symbol; // returns null  https://github.com/dotnet/roslyn/issues/15450
            //Assert.Equal("System.Int32", symbol.Type.ToTestDisplayString());
        }

        [Fact]
        public void UnderscoreLocalInDeconstructDeclaration()
        {
            var source =
@"
class C
{
    static void Main()
    {
        int _;
        (_, var x) = (1, 2);
        System.Console.Write(_);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (7,10): error CS8184: Cannot reference _ in a deconstruction declaration
                //         (_, var x) = (1, 2);
                Diagnostic(ErrorCode.ERR_MixedDeconstructionDisallowed, "_").WithArguments("_").WithLocation(7, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard = GetDiscardIdentifiers(tree).First();
            Assert.Equal("(_, var x)", discard.Parent.Parent.ToString());
            var symbol = (LocalSymbol)model.GetSymbolInfo(discard).Symbol;
            Assert.Equal("System.Int32", symbol.Type.ToTestDisplayString());
        }

        [Fact]
        public void ShortDiscardInDeconstructAssignment()
        {
            var source =
@"
class C
{
    static void Main()
    {
        int x;
        (_, _, x) = (1, 2, 3);
        System.Console.Write(x);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "3");
        }

        [Fact]
        public void MixedDeconstructionIsBlocked()
        {
            var source =
@"
class C
{
    static void Main()
    {
        int i;
        (i, var x) = (1, 2);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (7,10): error CS1031: Type expected
                //         (i, var x) = (1, 2);
                Diagnostic(ErrorCode.ERR_TypeExpected, "i").WithLocation(7, 10),
                // (7,10): error CS0128: A local variable or function named 'i' is already defined in this scope
                //         (i, var x) = (1, 2);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "i").WithArguments("i").WithLocation(7, 10),
                // (6,13): warning CS0168: The variable 'i' is declared but never used
                //         int i;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i").WithArguments("i").WithLocation(6, 13)
                );
        }

        [Fact]
        public void DiscardInDeconstructAssignment()
        {
            var source =
@"
class C
{
    static void Main()
    {
        int x;
        (_, x) = (1, 2);
        System.Console.Write(x);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard1 = GetDiscardIdentifiers(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            var tuple1 = (TupleExpressionSyntax)discard1.Parent.Parent;
            Assert.Equal("(_, x)", tuple1.ToString());
            Assert.Equal("(System.Int32, System.Int32)", model.GetTypeInfo(tuple1).Type.ToTestDisplayString());
        }

        [Fact]
        public void DiscardInDeconstructDeclaration()
        {
            var source =
@"
class C
{
    static void Main()
    {
        var (_, x) = (1, 2);
        System.Console.Write(x);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard1 = GetDiscardDesignations(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            var tuple1 = (DeclarationExpressionSyntax)discard1.Parent.Parent;
            Assert.Equal("var (_, x)", tuple1.ToString());
            //Assert.Equal("(System.Int32, System.Int32)", model.GetTypeInfo(tuple1).Type.ToTestDisplayString()); // fix null type  https://github.com/dotnet/roslyn/issues/15450
        }

        [Fact]
        public void UnderscoreLocalInDeconstructAssignment()
        {
            var source =
@"
class C
{
    static void Main()
    {
        int x, _;
        (_, x) = (1, 2);
        System.Console.Write($""{_} {x}"");
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard = GetDiscardIdentifiers(tree).First();
            Assert.Equal("(_, x)", discard.Parent.Parent.ToString());
            var symbol = (LocalSymbol)model.GetSymbolInfo(discard).Symbol;
            Assert.Equal("System.Int32", symbol.Type.ToTestDisplayString());
        }

        [Fact]
        public void DiscardInForeach()
        {
            var source =
@"
class C
{
    static void Main()
    {
        foreach (var (_, x) in new[] { (1, ""hello"") }) { System.Console.Write(""1 ""); }
        foreach ((_, var x) in new[] { (1, ""hello"") }) { System.Console.Write(""2""); }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard1 = GetDiscardDesignations(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            var declaration1 = (DeclarationExpressionSyntax)discard1.Parent.Parent;
            Assert.Equal("var (_, x)", declaration1.ToString());
            //Assert.Equal("", model.GetTypeInfo(declaration1).Type.ToTestDisplayString()); //  https://github.com/dotnet/roslyn/issues/15450

            var discard3 = GetDiscardIdentifiers(tree).First();
            Assert.Equal("(_, var x)", discard3.Parent.Parent.ToString());
            var symbol3 = (IDiscardedSymbol)model.GetSymbolInfo(discard3).Symbol; // returns null  https://github.com/dotnet/roslyn/issues/15450
            //Assert.Equal("System.Int32", symbol3.Type.ToTestDisplayString());
        }

        [Fact]
        public void TwoDiscardsInForeach()
        {
            var source =
@"
class C
{
    static void Main()
    {
        foreach ((_, _) in new[] { (1, ""hello"") }) { System.Console.Write(""2""); }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (6,25): error CS1001: Identifier expected
                //         foreach ((_, _) in new[] { (1, "hello") }) { System.Console.Write("2"); }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "in").WithLocation(6, 25),
                // (6,25): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach ((_, _) in new[] { (1, "hello") }) { System.Console.Write("2"); }
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(6, 25),
                // (6,19): error CS0246: The type or namespace name '_' could not be found (are you missing a using directive or an assembly reference?)
                //         foreach ((_, _) in new[] { (1, "hello") }) { System.Console.Write("2"); }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "_").WithArguments("_").WithLocation(6, 19),
                // (6,22): error CS0246: The type or namespace name '_' could not be found (are you missing a using directive or an assembly reference?)
                //         foreach ((_, _) in new[] { (1, "hello") }) { System.Console.Write("2"); }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "_").WithArguments("_").WithLocation(6, 22),
                // (6,9): error CS0030: Cannot convert type '(int, string)' to '(_, _)'
                //         foreach ((_, _) in new[] { (1, "hello") }) { System.Console.Write("2"); }
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("(int, string)", "(_, _)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void UnderscoreLocalDisallowedInForEach()
        {
            var source =
@"
class C
{
    static void Main()
    {
        {
            foreach ((var x, _) in new[] { (1, ""hello"") }) { System.Console.Write(""2 ""); }
        }
        {
            int _;
            foreach ((var y, _) in new[] { (1, ""hello"") }) { System.Console.Write(""4""); } // error
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (11,30): error CS8184: Cannot reference _ in a deconstruction declaration
                //             foreach ((var y, _) in new[] { (1, "hello") }) { System.Console.Write("4"); } // error
                Diagnostic(ErrorCode.ERR_MixedDeconstructionDisallowed, "_").WithArguments("_").WithLocation(11, 30),
                // (11,30): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //             foreach ((var y, _) in new[] { (1, "hello") }) { System.Console.Write("4"); } // error
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "_").WithArguments("string", "int").WithLocation(11, 30),
                // (10,17): warning CS0168: The variable '_' is declared but never used
                //             int _;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "_").WithArguments("_").WithLocation(10, 17)
                );
        }

        [Fact]
        public void TwoDiscardsInDeconstructAssignment()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (_, _) = (new C(), new C());
    }
    public C() { System.Console.Write(""C""); }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "CC");
        }

        [Fact]
        public void SingleDiscardInAssignment()
        {
            var source =
@"
class C
{
    static void Main()
    {
        _ = M();
    }
    public static int M() { System.Console.Write(""M""); return 1; }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "M");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard1 = GetDiscardIdentifiers(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            Assert.Equal("System.Int32", model.GetTypeInfo(discard1).Type.ToTestDisplayString());
        }

        [Fact]
        public void SingleDiscardInAsyncAssignment()
        {
            var source =
@"
class C
{
    async void M()
    {
        System.Threading.Tasks.Task.Delay(new System.TimeSpan(0)); // warning
        _ = System.Threading.Tasks.Task.Delay(new System.TimeSpan(0)); // fire-and-forget
        await System.Threading.Tasks.Task.Delay(new System.TimeSpan(0));
    }
}
";

            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (6,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         System.Threading.Tasks.Task.Delay(new System.TimeSpan(0));
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "System.Threading.Tasks.Task.Delay(new System.TimeSpan(0))").WithLocation(6, 9)
                );
        }

        [Fact]
        public void SingleDiscardInUntypedAssignment()
        {
            var source =
@"
class C
{
    static void Main()
    {
        _ = null;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,9): error CS8183: Cannot infer the type of implicitly-typed discard.
                //         _ = null;
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "_").WithLocation(6, 9)
                );
        }

        [Fact]
        public void UnderscoreLocalInAssignment()
        {
            var source =
@"
class C
{
    static void Main()
    {
        int _;
        _ = M();
        System.Console.Write(_);
    }
    public static int M() { return 1; }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1");
        }

        [Fact]
        public void DeclareAndUseLocalInDeconstruction()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (var x, x) = (1, 2);
        (y, var y) = (1, 2);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            // mixing declaration and expressions isn't supported yet
            comp.VerifyDiagnostics(
                // (6,17): error CS1031: Type expected
                //         (var x, x) = (1, 2);
                Diagnostic(ErrorCode.ERR_TypeExpected, "x").WithLocation(6, 17),
                // (7,10): error CS1031: Type expected
                //         (y, var y) = (1, 2);
                Diagnostic(ErrorCode.ERR_TypeExpected, "y").WithLocation(7, 10),
                // (6,17): error CS0128: A local variable or function named 'x' is already defined in this scope
                //         (var x, x) = (1, 2);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(6, 17),
                // (7,17): error CS0128: A local variable or function named 'y' is already defined in this scope
                //         (y, var y) = (1, 2);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y").WithArguments("y").WithLocation(7, 17)
                );
        }

        [Fact]
        public void OutVarAndUsageInDeconstructAssignment()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (M(out var x).P, x) = (1, x);
        System.Console.Write(x);
    }
    static C M(out int i) { i = 42; return new C(); }
    int P { set { System.Console.Write($""Written {value}. ""); } }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Written 1. 42");
        }

        [Fact]
        public void OutDiscardInDeconstructAssignment()
        {
            var source =
@"
class C
{
    static void Main()
    {
        int _;
        (M(out var _).P, _) = (1, 2);
        System.Console.Write(_);
    }
    static C M(out int i) { i = 42; return new C(); }
    int P { set { System.Console.Write($""Written {value}. ""); } }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Written 1. 2");
        }

        [Fact]
        public void OutDiscardInDeconstructTarget()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (x, _) = (M(out var x), 2);
        System.Console.Write(x);
    }
    static int M(out int i) { i = 42; return 3; }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (6,10): error CS0841: Cannot use local variable 'x' before it is declared
                //         (x, _) = (M(out var x), 2);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(6, 10)
                );
        }


        [Fact]
        public void SimpleDiscardDeconstructInScript()
        {
            var source =
@"
using alias = System.Int32;
(string _, alias _) = (""hello"", 42);
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var discard1 = GetDiscardDesignations(tree).First();
                var declaration1 = (DeclarationExpressionSyntax)discard1.Parent;
                Assert.Equal("string _", declaration1.ToString());
                Assert.Null(model.GetDeclaredSymbol(declaration1));
                Assert.Null(model.GetDeclaredSymbol(discard1));

                var discard2 = GetDiscardDesignations(tree).ElementAt(1);
                var declaration2 = (DeclarationExpressionSyntax)discard2.Parent;
                Assert.Equal("alias _", declaration2.ToString());
                Assert.Null(model.GetDeclaredSymbol(declaration2));
                Assert.Null(model.GetDeclaredSymbol(discard2));

                var tuple = (TupleExpressionSyntax)declaration1.Parent.Parent;
                Assert.Equal("(string _, alias _)", tuple.ToString());
                Assert.Equal("(System.String, System.Int32)", model.GetTypeInfo(tuple).Type.ToTestDisplayString());
            };

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, sourceSymbolValidator: validator);
        }

        [Fact]
        public void SingleDiscardInAssignmentInScript()
        {
            var source =
@"
int M() { System.Console.Write(""M""); return 1; }
_ = M();
";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "M");
        }

        [Fact]
        public void NestedVarDiscardDeconstructionInScript()
        {
            var source =
@"
(var _, var (_, x3)) = (""hello"", (42, 43));
System.Console.Write($""{x3}"");
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var discard2 = GetDiscardDesignations(tree).ElementAt(1);
                var nestedDeclaration = (DeclarationExpressionSyntax)discard2.Parent.Parent;
                Assert.Equal("var (_, x3)", nestedDeclaration.ToString());
                Assert.Null(model.GetDeclaredSymbol(nestedDeclaration));
                Assert.Null(model.GetDeclaredSymbol(discard2));
                //Assert.Equal("", model.GetTypeInfo(nestedDeclaration).Type.ToString()); //  https://github.com/dotnet/roslyn/issues/15450

                var tuple = (TupleExpressionSyntax)discard2.Parent.Parent.Parent.Parent;
                Assert.Equal("(var _, var (_, x3))", tuple.ToString());
                Assert.Equal("(System.String, (System.Int32, System.Int32))", model.GetTypeInfo(tuple).Type.ToTestDisplayString());
            };

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "43", sourceSymbolValidator: validator);
        }

        [Fact]
        public void VariousDiscardsInForeach()
        {
            var source =
@"
class C
{
    static void Main()
    {
        foreach ((var _, int _, _, var (_, _), int x) in new[] { (1, 2, 3, (4, 5), 6) })
        {
            System.Console.Write(x);
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "6");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard1 = GetDiscardDesignations(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            var declaration1 = (DeclarationExpressionSyntax)discard1.Parent;
            Assert.Equal("var _", declaration1.ToString());
            //Assert.Equal("", model.GetTypeInfo(declaration1).Type.ToTestDisplayString()); //  https://github.com/dotnet/roslyn/issues/15450

            var discard2 = GetDiscardDesignations(tree).ElementAt(1);
            Assert.Null(model.GetDeclaredSymbol(discard2));
            var declaration2 = (DeclarationExpressionSyntax)discard2.Parent;
            Assert.Equal("int _", declaration2.ToString());
            //Assert.Equal("", model.GetTypeInfo(declaration2).Type.ToTestDisplayString()); //  https://github.com/dotnet/roslyn/issues/15450

            var discard3 = GetDiscardIdentifiers(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard3));
            //Assert.Equal("System.Int32", model.GetTypeInfo(discard3).Type.ToTestDisplayString()); //  https://github.com/dotnet/roslyn/issues/15450
        }

        [Fact]
        public void DiscardInCSharp6Foreach()
        {
            var source =
@"
class C
{
    static void Main()
    {
        foreach (var _ in M())
        {
            System.Console.Write(_);
        }
    }
    static System.Collections.Generic.IEnumerable<int> M()
    {
        System.Console.Write(""M "");
        yield return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "M 1");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            //var discard1 = GetDiscardDesignations(tree).First();
            //Assert.Null(model.GetDeclaredSymbol(discard1));
            //var declaration1 = (DeclarationExpressionSyntax)discard1.Parent;
            //Assert.Equal("var _", declaration1.ToString());
            ////Assert.Equal("", model.GetTypeInfo(declaration1).Type.ToTestDisplayString()); //  https://github.com/dotnet/roslyn/issues/15450
        }

        [Fact]
        public void ShortDiscardDisallowedInForeach()
        {
            var source =
@"
class C
{
    static void Main()
    {
        foreach (_ in M())
        {
        }
    }
    static System.Collections.Generic.IEnumerable<int> M()
    {
        yield return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (6,20): error CS1001: Identifier expected
                //         foreach (_ in M())
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "in").WithLocation(6, 20),
                // (6,20): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach (_ in M())
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(6, 20),
                // (6,18): error CS0246: The type or namespace name '_' could not be found (are you missing a using directive or an assembly reference?)
                //         foreach (_ in M())
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "_").WithArguments("_").WithLocation(6, 18)
                );
        }

        [Fact]
        public void ExistingUnderscoreLocalInLegacyForeach()
        {
            var source =
@"
class C
{
    static void Main()
    {
        int _;
        foreach (var _ in new[] { 1 })
        {
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (7,22): error CS0136: A local or parameter named '_' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         foreach (var _ in new[] { 1 })
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "_").WithArguments("_").WithLocation(7, 22),
                // (6,13): warning CS0168: The variable '_' is declared but never used
                //         int _;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "_").WithArguments("_").WithLocation(6, 13)
                );
        }
    }
}
