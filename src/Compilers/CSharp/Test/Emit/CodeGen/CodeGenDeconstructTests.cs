// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                Assert.Equal("(System.Int64 x, System.String y)", model.GetTypeInfo(lhs).Type.ToTestDisplayString());
                Assert.Equal("(System.Int64 x, System.String y)", model.GetTypeInfo(lhs).ConvertedType.ToTestDisplayString());

                var right = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
                Assert.Equal(@"new C()", right.ToString());
                Assert.Equal("C", model.GetTypeInfo(right).Type.ToTestDisplayString());
                Assert.Equal("C", model.GetTypeInfo(right).ConvertedType.ToTestDisplayString());
                Assert.Equal(ConversionKind.Identity, model.GetConversion(right).Kind);
            };

            var comp = CompileAndVerifyWithMscorlib40(source, expectedOutput: "1 hello", references: s_valueTupleRefs, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (long V_0, //x
                string V_1, //y
                int V_2,
                string V_3)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_2
  IL_0007:  ldloca.s   V_3
  IL_0009:  callvirt   ""void C.Deconstruct(out int, out string)""
  IL_000e:  ldloc.2
  IL_000f:  conv.i8
  IL_0010:  stloc.0
  IL_0011:  ldloc.3
  IL_0012:  stloc.1
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       ""string long.ToString()""
  IL_001a:  ldstr      "" ""
  IL_001f:  ldloc.1
  IL_0020:  call       ""string string.Concat(string, string, string)""
  IL_0025:  call       ""void System.Console.WriteLine(string)""
  IL_002a:  ret
}");
        }

        [Fact]
        public void ObsoleteDeconstructMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C();
        foreach (var (z1, z2) in new[] { new C() }) { }
    }

    [System.Obsolete]
    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,18): warning CS0612: 'C.Deconstruct(out int, out string)' is obsolete
                //         (x, y) = new C();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C()").WithArguments("C.Deconstruct(out int, out string)").WithLocation(9, 18),
                // (10,34): warning CS0612: 'C.Deconstruct(out int, out string)' is obsolete
                //         foreach (var (z1, z2) in new[] { new C() }) { }
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new[] { new C() }").WithArguments("C.Deconstruct(out int, out string)").WithLocation(10, 34)
                );
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

            var comp = CompileAndVerify(source, expectedOutput: "1 hello");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       42 (0x2a)
  .maxstack  3
  .locals init (int V_0, //x
                string V_1, //y
                int V_2,
                string V_3)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_2
  IL_0007:  ldloca.s   V_3
  IL_0009:  callvirt   ""void C.Deconstruct(out int, out string)""
  IL_000e:  ldloc.2
  IL_000f:  stloc.0
  IL_0010:  ldloc.3
  IL_0011:  stloc.1
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""string int.ToString()""
  IL_0019:  ldstr      "" ""
  IL_001e:  ldloc.1
  IL_001f:  call       ""string string.Concat(string, string, string)""
  IL_0024:  call       ""void System.Console.WriteLine(string)""
  IL_0029:  ret
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

            var comp = CompileAndVerify(source, expectedOutput: "1 hello");
            comp.VerifyDiagnostics();

            var tree = comp.Compilation.SyntaxTrees.First();
            var model = comp.Compilation.GetSemanticModel(tree);
            var deconstruction = (AssignmentExpressionSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.SimpleAssignmentExpression).AsNode();
            Assert.Equal("(x, y) = new C()", deconstruction.ToString());
            var deconstructionInfo = model.GetDeconstructionInfo(deconstruction);

            var firstDeconstructMethod = ((CSharpCompilation)comp.Compilation).GetTypeByMetadataName("C").GetMembers(WellKnownMemberNames.DeconstructMethodName)
                .OfType<SourceOrdinaryMethodSymbol>().Where(m => m.ParameterCount == 2).Single();
            Assert.Equal(firstDeconstructMethod.GetPublicSymbol(), deconstructionInfo.Method);

            Assert.Equal("void C.Deconstruct(out System.Int32 a, out System.String b)",
                deconstructionInfo.Method.ToTestDisplayString());
            Assert.Null(deconstructionInfo.Conversion);

            var nested = deconstructionInfo.Nested;
            Assert.Equal(2, nested.Length);

            Assert.Null(nested[0].Method);
            Assert.Equal(ConversionKind.ImplicitNumeric, nested[0].Conversion.Value.Kind);
            Assert.Empty(nested[0].Nested);

            Assert.Null(nested[1].Method);
            Assert.Equal(ConversionKind.Identity, nested[1].Conversion.Value.Kind);
            Assert.Empty(nested[1].Nested);

            var assignment = (AssignmentExpressionSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.SimpleAssignmentExpression, occurrence: 2).AsNode();
            Assert.Equal("a = 1", assignment.ToString());
            var defaultInfo = model.GetDeconstructionInfo(assignment);
            Assert.Null(defaultInfo.Method);
            Assert.Empty(defaultInfo.Nested);
            Assert.Equal(ConversionKind.UnsetConversionKind, defaultInfo.Conversion.Value.Kind);
        }

        [Fact]
        [WorkItem(27520, "https://github.com/dotnet/roslyn/issues/27520")]
        public void GetDeconstructionInfoOnIncompleteCode()
        {
            string source = @"
class C
{
    static void M(string s)
    {
        foreach (char in s) { }
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,18): error CS1525: Invalid expression term 'char'
                //         foreach (char in s) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "char").WithArguments("char").WithLocation(6, 18),
                // (6,23): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach (char in s) { }
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(6, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var foreachDeconstruction = (ForEachVariableStatementSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.ForEachVariableStatement).AsNode();
            Assert.Equal(@"foreach (char in s) { }", foreachDeconstruction.ToString());
            var deconstructionInfo = model.GetDeconstructionInfo(foreachDeconstruction);
            Assert.Equal(Conversion.UnsetConversion, deconstructionInfo.Conversion);
            Assert.Null(deconstructionInfo.Method);
            Assert.Empty(deconstructionInfo.Nested);
        }

        [Fact]
        [WorkItem(15634, "https://github.com/dotnet/roslyn/issues/15634")]
        public void DeconstructMustReturnVoid()
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

    public int Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
        return 42;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(8, 18)
                );
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
            var comp = CompileAndVerify(source, expectedOutput: expected);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyExecutionOrder_Deconstruct_Conditional()
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
        bool b = true;
        (c.getHolderForX().x, c.getHolderForY().y) = b ? c.getDeconstructReceiver() : default;
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
            var comp = CompileAndVerify(source, expectedOutput: expected);
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
            var comp = CompileAndVerify(source, expectedOutput: expected);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyExecutionOrder_TupleLiteral_Conditional()
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
        bool b = true;
        (c.getHolderForX().x, c.getHolderForY().y) =  b ? (new D1(), new D2()) : default;
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
Constructor2
Conversion1
Conversion2
setX
setY
";
            var comp = CompileAndVerify(source, expectedOutput: expected);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyExecutionOrder_TupleLiteralAndDeconstruction()
        {
            string source = @"
using System;
class C
{
    int w { set { Console.WriteLine($""setW""); } }
    int x { set { Console.WriteLine($""setX""); } }
    int y { set { Console.WriteLine($""setY""); } }
    int z { set { Console.WriteLine($""setZ""); } }

    C getHolderForW() { Console.WriteLine(""getHolderforW""); return this; }
    C getHolderForX() { Console.WriteLine(""getHolderforX""); return this; }
    C getHolderForY() { Console.WriteLine(""getHolderforY""); return this; }
    C getHolderForZ() { Console.WriteLine(""getHolderforZ""); return this; }

    static void Main()
    {
        C c = new C();
        (c.getHolderForW().w, (c.getHolderForY().y, c.getHolderForZ().z), c.getHolderForX().x) = (new D1(), new D2(), new D3());
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
    public void Deconstruct(out int x, out int y) { x = 2; y = 3; Console.WriteLine(""deconstruct""); }
}
class D3
{
    public D3() { Console.WriteLine(""Constructor3""); }
    public static implicit operator int(D3 d) { Console.WriteLine(""Conversion3""); return 3; }
}
";

            string expected =
@"getHolderforW
getHolderforY
getHolderforZ
getHolderforX
Constructor1
Conversion1
Constructor2
Constructor3
Conversion3
deconstruct
setW
setY
setZ
setX
";
            var comp = CompileAndVerify(source, expectedOutput: expected);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyExecutionOrder_TupleLiteralAndDeconstruction_Conditional()
        {
            string source = @"
using System;
class C
{
    int w { set { Console.WriteLine($""setW""); } }
    int x { set { Console.WriteLine($""setX""); } }
    int y { set { Console.WriteLine($""setY""); } }
    int z { set { Console.WriteLine($""setZ""); } }

    C getHolderForW() { Console.WriteLine(""getHolderforW""); return this; }
    C getHolderForX() { Console.WriteLine(""getHolderforX""); return this; }
    C getHolderForY() { Console.WriteLine(""getHolderforY""); return this; }
    C getHolderForZ() { Console.WriteLine(""getHolderforZ""); return this; }

    static void Main()
    {
        C c = new C();
        bool b = false;
        (c.getHolderForW().w, (c.getHolderForY().y, c.getHolderForZ().z), c.getHolderForX().x) = b ? default : (new D1(), new D2(), new D3());
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
    public void Deconstruct(out int x, out int y) { x = 2; y = 3; Console.WriteLine(""deconstruct""); }
}
class D3
{
    public D3() { Console.WriteLine(""Constructor3""); }
    public static implicit operator int(D3 d) { Console.WriteLine(""Conversion3""); return 3; }
}
";

            string expected =
@"getHolderforW
getHolderforY
getHolderforZ
getHolderforX
Constructor1
Constructor2
Constructor3
deconstruct
Conversion1
Conversion3
setW
setY
setZ
setX
";
            var comp = CompileAndVerify(source, expectedOutput: expected);
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

            var comp = CompileAndVerify(source, expectedOutput: "1 hello world");
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

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", references: new[] { CSharpRef });
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

            var comp = CompileAndVerify(source, expectedOutput: "initial modified 1 hello", references: new[] { CSharpRef });
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

            var comp = CompileAndVerify(source, expectedOutput: "2 hello");
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

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,58): error CS8328:  The parameter modifier 'params' cannot be used with 'out' 
                //     public void Deconstruct(out int a, out string b, out params int[] c)
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "out").WithLocation(4, 58));
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

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", references: new[] { CSharpRef });
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

            var comp = CompileAndVerify(source, expectedOutput: "1 hello world");
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

            var comp = CompileAndVerify(source, expectedOutput: "2 3");
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

            var comp = CompileAndVerify(source, expectedOutput: expected);
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

            var comp = CompileAndVerify(source, expectedOutput: expected);
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

            var comp = CompileAndVerify(source, expectedOutput: expected);
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
        (Goo()[Bar()], y) = new C();
        System.Console.WriteLine($""Final array values[2] {array.values[2]}"");
    }

    static SomeArray Goo()
    {
        System.Console.WriteLine($""Goo"");
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
@"Goo
Bar
Deconstruct
indexSet (with value 101)
Final array values[2] 101
";
            var comp = CompileAndVerify(source, expectedOutput: expected);
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
        int i = 1;
        (x, y) = (i, ""hello"");
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello");
            comp.VerifyDiagnostics();

            var tree = comp.Compilation.SyntaxTrees.First();
            var model = comp.Compilation.GetSemanticModel(tree);
            var deconstruction = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();
            var deconstructionInfo = model.GetDeconstructionInfo(deconstruction);

            Assert.Null(deconstructionInfo.Method);
            Assert.Null(deconstructionInfo.Conversion);

            var nested = deconstructionInfo.Nested;
            Assert.Equal(2, nested.Length);

            Assert.Null(nested[0].Method);
            Assert.Equal(ConversionKind.Identity, nested[0].Conversion.Value.Kind);
            Assert.Empty(nested[0].Nested);

            Assert.Null(nested[1].Method);
            Assert.Equal(ConversionKind.Identity, nested[1].Conversion.Value.Kind);
            Assert.Empty(nested[1].Nested);

            var tuple = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            Assert.Equal(@"(i, ""hello"")", tuple.ToString());
            var tupleConversion = model.GetConversion(tuple);
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, tupleConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitNumeric, tupleConversion.UnderlyingConversions[0].Kind);
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
            var comp = CompileAndVerify(source, expectedOutput: "1 hello");
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
        System.Console.WriteLine(string.Concat(x, "" "", y));
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "4 2");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (int V_0) //y
  IL_0000:  ldc.i4.4
  IL_0001:  conv.i8
  IL_0002:  ldc.i4.2
  IL_0003:  stloc.0
  IL_0004:  box        ""long""
  IL_0009:  ldstr      "" ""
  IL_000e:  ldloc.0
  IL_000f:  box        ""int""
  IL_0014:  call       ""string string.Concat(object, object, object)""
  IL_0019:  call       ""void System.Console.WriteLine(string)""
  IL_001e:  ret
}");
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

            var comp = CompileAndVerify(source, expectedOutput: "9 10");
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

            var comp = CompileAndVerify(source, expectedOutput: "4 2");
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

            var comp = CompileAndVerify(source, expectedOutput: "hello");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (string V_0) //y
  IL_0000:  ldnull
  IL_0001:  ldstr      ""hello""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""string string.Concat(string, string)""
  IL_000d:  call       ""void System.Console.WriteLine(string)""
  IL_0012:  ret
} ");
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
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main",
@"{
  // Code size       15 (0xf)
  .maxstack  3
  .locals init (int V_0,
                int V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_0
  IL_0007:  ldloca.s   V_1
  IL_0009:  callvirt   ""void C.Deconstruct(out int, out int)""
  IL_000e:  ret
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
            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var x = nodes.OfType<VariableDeclaratorSyntax>().ElementAt(2);

                Assert.Equal("(System.Int32 x, System.Int32 y) z", model.GetDeclaredSymbol(x).ToTestDisplayString());
            };
            var comp = CompileAndVerify(source, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main",
@"{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0, //z
                int V_1,
                int V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_1
  IL_0007:  ldloca.s   V_2
  IL_0009:  callvirt   ""void C.Deconstruct(out int, out int)""
  IL_000e:  ldloc.1
  IL_000f:  ldloc.2
  IL_0010:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0015:  stloc.0
  IL_0016:  ldloca.s   V_0
  IL_0018:  constrained. ""System.ValueTuple<int, int>""
  IL_001e:  callvirt   ""string object.ToString()""
  IL_0023:  pop
  IL_0024:  ret
}");
        }

        [Fact]
        public void ValueTupleReturnIsEmittedIfUsed_WithCSharp7_1()
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
            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var x = nodes.OfType<VariableDeclaratorSyntax>().ElementAt(2);

                Assert.Equal("(System.Int32 x, System.Int32 y) z", model.GetDeclaredSymbol(x).ToTestDisplayString());
            };
            var comp = CompileAndVerify(source,
                sourceSymbolValidator: validator, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(18629, "https://github.com/dotnet/roslyn/issues/18629")]
        public void ValueTupleNotRequiredIfReturnIsNotUsed()
        {
            string source = @"
class C
{
    public static void Main()
    {
        int x, y;
        (x, y) = new C();
        System.Console.Write($""assignment: {x} {y}. "");

        foreach (var (a, b) in new[] { new C() })
        {
            System.Console.Write($""foreach: {a} {b}."");
        }
    }

    public void Deconstruct(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
";
            var comp = CreateCompilationWithMscorlib40(source, parseOptions: TestOptions.Regular7, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();

            CompileAndVerify(comp, expectedOutput: "assignment: 1 2. foreach: 1 2.");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var xy = nodes.OfType<TupleExpressionSyntax>().Single();
            Assert.Equal("(x, y)", xy.ToString());
            var tuple1 = model.GetTypeInfo(xy).Type;
            Assert.Equal("(System.Int32 x, System.Int32 y)[missing]", tuple1.ToTestDisplayString());

            var ab = nodes.OfType<DeclarationExpressionSyntax>().Single();
            var tuple2 = model.GetTypeInfo(ab).Type;
            Assert.Equal("(System.Int32 a, System.Int32 b)[missing]", tuple2.ToTestDisplayString());
            Assert.Equal("(System.Int32 a, System.Int32 b)[missing]", model.GetTypeInfo(ab).ConvertedType.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(18629, "https://github.com/dotnet/roslyn/issues/18629")]
        public void ValueTupleNotRequiredIfReturnIsNotUsed2()
        {
            string source = @"
class C
{
    public static void Main()
    {
        int x, y;
        for((x, y) = new C(1); ; (x, y) = new C(2))
        {
        }
    }

    public C(int c) { }
    public void Deconstruct(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
";
            var comp = CreateCompilationWithMscorlib40(source, parseOptions: TestOptions.Regular7, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var tuple1 = nodes.OfType<TupleExpressionSyntax>().ElementAt(0);
            Assert.Equal("(x, y) = new C(1)", tuple1.Parent.ToString());
            var tupleType1 = model.GetTypeInfo(tuple1).Type;
            Assert.Equal("(System.Int32 x, System.Int32 y)[missing]", tupleType1.ToTestDisplayString());

            var tuple2 = nodes.OfType<TupleExpressionSyntax>().ElementAt(1);
            Assert.Equal("(x, y) = new C(2)", tuple2.Parent.ToString());
            var tupleType2 = model.GetTypeInfo(tuple1).Type;
            Assert.Equal("(System.Int32 x, System.Int32 y)[missing]", tupleType2.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(18629, "https://github.com/dotnet/roslyn/issues/18629")]
        public void ValueTupleNotRequiredIfReturnIsNotUsed3()
        {
            string source = @"
class C
{
    public static void Main()
    {
        int x, y;
        (x, y) = new C();
    }

    public C() { }
    public void Deconstruct(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
namespace System
{
    [Obsolete]
    public struct ValueTuple<T1, T2>
    {
        [Obsolete]
        public T1 Item1;

        [Obsolete]
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2) { Item1 = item1; Item2 = item2; }
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var tuple = nodes.OfType<TupleExpressionSyntax>().ElementAt(0);
            Assert.Equal("(x, y) = new C()", tuple.Parent.ToString());
            var tupleType = model.GetTypeInfo(tuple).Type;
            Assert.Equal("(System.Int32 x, System.Int32 y)", tupleType.ToTestDisplayString());
            var underlying = ((INamedTypeSymbol)tupleType).TupleUnderlyingType;
            Assert.Equal("(System.Int32, System.Int32)", underlying.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(18629, "https://github.com/dotnet/roslyn/issues/18629")]
        public void ValueTupleRequiredWhenRightHandSideIsTuple()
        {
            string source = @"
class C
{
    public static void Main()
    {
        int x, y;
        (x, y) = (1, 2);
    }
}
";
            var comp = CreateCompilationWithMscorlib40(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (7,18): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(1, 2)").WithArguments("System.ValueTuple`2").WithLocation(7, 18)
                );
        }

        [Fact]
        [WorkItem(18629, "https://github.com/dotnet/roslyn/issues/18629")]
        public void ValueTupleRequiredWhenRightHandSideIsTupleButNoReferenceEmitted()
        {
            string source = @"
class C
{
    public static void Main()
    {
        int x, y;
        (x, y) = (1, 2);
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics();

            Action<PEAssembly> assemblyValidator = assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var names = reader.GetAssemblyRefNames().Select(name => reader.GetString(name));
                Assert.Empty(names.Where(name => name.Contains("ValueTuple")));
            };

            CompileAndVerifyCommon(comp, assemblyValidator: assemblyValidator);
        }

        [Fact]
        public void ValueTupleReturnMissingMemberWithCSharp7()
        {
            string source = @"
class C
{
    public void M()
    {
        int x, y;
        var nested = ((x, y) = (1, 2));
        System.Console.Write(nested.x);
    }
}
";

            var comp = CreateCompilation(source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            comp.VerifyDiagnostics(
                // (8,37): error CS8305: Tuple element name 'x' is inferred. Please use language version 7.1 or greater to access an element by its inferred name.
                //         System.Console.Write(nested.x);
                Diagnostic(ErrorCode.ERR_TupleInferredNamesNotAvailable, "x").WithArguments("x", "7.1").WithLocation(8, 37)
                );
        }

        [Fact]
        public void ValueTupleReturnWithInferredNamesWithCSharp7_1()
        {
            string source = @"
class C
{
    public void M()
    {
        int x, y, Item1, Rest;
        var a = ((x, y) = (1, 2));
        var b = ((x, x) = (1, 2));
        var c = ((_, x) = (1, 2));
        var d = ((Item1, Rest) = (1, 2));
        var nested = ((x, Item1, y, (_, x, x), (x, y)) = (1, 2, 3, (4, 5, 6), (7, 8)));
        (int, int) f = ((x, y) = (1, 2));
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
                var declarations = nodes.OfType<VariableDeclaratorSyntax>();
                Assert.Equal("(System.Int32 x, System.Int32 y) a", model.GetDeclaredSymbol(declarations.ElementAt(4)).ToTestDisplayString());

                Assert.Equal("(System.Int32, System.Int32) b", model.GetDeclaredSymbol(declarations.ElementAt(5)).ToTestDisplayString());

                Assert.Equal("(System.Int32, System.Int32 x) c", model.GetDeclaredSymbol(declarations.ElementAt(6)).ToTestDisplayString());

                var x = (ILocalSymbol)model.GetDeclaredSymbol(declarations.ElementAt(7));
                Assert.Equal("(System.Int32, System.Int32) d", x.ToTestDisplayString());
                Assert.True(x.Type.GetSymbol().TupleElementNames.IsDefault);

                Assert.Equal("(System.Int32 x, System.Int32, System.Int32 y, (System.Int32, System.Int32, System.Int32), (System.Int32 x, System.Int32 y)) nested",
                    model.GetDeclaredSymbol(declarations.ElementAt(8)).ToTestDisplayString());
            };

            var comp = CompileAndVerify(source,
                sourceSymbolValidator: validator, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            comp.VerifyDiagnostics();
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

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,19): error CS8185: A declaration is not allowed in this context.
                //         var z = ((var x, int y) = new C());
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var x").WithLocation(6, 19)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void Constraints_01()
        {
            string source = @"
using System;
class C
{
    public void M()
    {
        (int x, var (err1, y)) = (0, new C()); // ok, no return value used
        (ArgIterator err2, var err3) = M2(); // ok, no return value
        foreach ((ArgIterator err4, var err5) in new[] { M2() }) // ok, no return value
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
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (19,29): error CS1601: Cannot make reference to variable of type 'ArgIterator'
                //     public void Deconstruct(out ArgIterator a, out int b)
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "out ArgIterator a").WithArguments("System.ArgIterator").WithLocation(19, 29),
                // (14,46): error CS0306: The type 'ArgIterator' may not be used as a type argument
                //     public static (ArgIterator, ArgIterator) M2()
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M2").WithArguments("System.ArgIterator").WithLocation(14, 46),
                // (14,46): error CS0306: The type 'ArgIterator' may not be used as a type argument
                //     public static (ArgIterator, ArgIterator) M2()
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M2").WithArguments("System.ArgIterator").WithLocation(14, 46),
                // (16,17): error CS0306: The type 'ArgIterator' may not be used as a type argument
                //         return (default(ArgIterator), default(ArgIterator));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "default(ArgIterator)").WithArguments("System.ArgIterator").WithLocation(16, 17),
                // (16,39): error CS0306: The type 'ArgIterator' may not be used as a type argument
                //         return (default(ArgIterator), default(ArgIterator));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "default(ArgIterator)").WithArguments("System.ArgIterator").WithLocation(16, 39)
                );
        }

        [Fact]
        public void Constraints_02()
        {
            string source = @"
unsafe class C
{
    public void M()
    {
        (int x, var (err1, y)) = (0, new C()); // ok, no return value
        (var err2, var err3) = M2(); // ok, no return value
        foreach ((var err4, var err5) in new[] { M2() }) // ok, no return value
        {
        }
    }

    public static (int*, int*) M2()
    {
        return (default(int*), default(int*));
    }

    public void Deconstruct(out int* a, out int b)
    {
        a = default(int*);
        b = 2;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (13,32): error CS0306: The type 'int*' may not be used as a type argument
                //     public static (int*, int*) M2()
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M2").WithArguments("int*").WithLocation(13, 32),
                // (13,32): error CS0306: The type 'int*' may not be used as a type argument
                //     public static (int*, int*) M2()
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M2").WithArguments("int*").WithLocation(13, 32),
                // (15,17): error CS0306: The type 'int*' may not be used as a type argument
                //         return (default(int*), default(int*));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "default(int*)").WithArguments("int*").WithLocation(15, 17),
                // (15,32): error CS0306: The type 'int*' may not be used as a type argument
                //         return (default(int*), default(int*));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "default(int*)").WithArguments("int*").WithLocation(15, 32)
                );
        }

        [Fact]
        public void Constraints_03()
        {
            string source = @"
unsafe class C
{
    public void M()
    {
        int ok;
        int* err1, err2;
        var t = ((ok, (err1, ok)) = (0, new C()));
        var t2 = ((err1, err2) = M2());
    }

    public static (int*, int*) M2()
    {
        throw null;
    }

    public void Deconstruct(out int* a, out int b)
    {
        a = default(int*);
        b = 2;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (12,32): error CS0306: The type 'int*' may not be used as a type argument
                //     public static (int*, int*) M2()
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M2").WithArguments("int*").WithLocation(12, 32),
                // (12,32): error CS0306: The type 'int*' may not be used as a type argument
                //     public static (int*, int*) M2()
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M2").WithArguments("int*").WithLocation(12, 32),
                // (8,24): error CS0306: The type 'int*' may not be used as a type argument
                //         var t = ((ok, (err1, ok)) = (0, new C()));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "err1").WithArguments("int*").WithLocation(8, 24),
                // (9,20): error CS0306: The type 'int*' may not be used as a type argument
                //         var t2 = ((err1, err2) = M2());
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "err1").WithArguments("int*").WithLocation(9, 20),
                // (9,26): error CS0306: The type 'int*' may not be used as a type argument
                //         var t2 = ((err1, err2) = M2());
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "err2").WithArguments("int*").WithLocation(9, 26)
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

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,10): error CS8187: Tuple element names are not permitted on the left of a deconstruction.
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_TupleElementNamesInDeconstruction, "Alice:").WithLocation(6, 10),
                // (6,24): error CS8187: Tuple element names are not permitted on the left of a deconstruction.
                //         (Alice: var x, Bob: int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_TupleElementNamesInDeconstruction, "Bob:").WithLocation(6, 24)
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
            var comp = CompileAndVerify(source, expectedOutput: "(1, 2)");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningIntoProperties()
        {
            string source = @"
class C
{
    static int field;
    static long x { set { System.Console.WriteLine($""setX {value}""); } }
    static string y { get; set; }
    static ref int z { get { return ref field; } }

    static void Main()
    {
        (x, y, z) = new C();
        System.Console.WriteLine(y);
        System.Console.WriteLine($""field: {field}"");
    }

    public void Deconstruct(out int a, out string b, out int c)
    {
        a = 1;
        b = ""hello"";
        c = 2;
    }
}
";
            string expected =
@"setX 1
hello
field: 2
";
            var comp = CompileAndVerify(source, expectedOutput: expected);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningTupleIntoProperties()
        {
            string source = @"
class C
{
    static int field;
    static long x { set { System.Console.WriteLine($""setX {value}""); } }
    static string y { get; set; }
    static ref int z { get { return ref field; } }

    static void Main()
    {
        (x, y, z) = (1, ""hello"", 2);
        System.Console.WriteLine(y);
        System.Console.WriteLine($""field: {field}"");
    }
}
";
            string expected =
@"setX 1
hello
field: 2
";
            var comp = CompileAndVerify(source, expectedOutput: expected);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(18554, "https://github.com/dotnet/roslyn/issues/18554")]
        public void AssigningIntoIndexers()
        {
            string source = @"
using System;
class C
{
    int field;
    ref int this[int x, int y, int z, int opt = 1]
    {
        get
        {
            Console.WriteLine($""this.get"");
            return ref field;
        }
    }

    int this[int x, long y, int z, int opt = 1]
    {
        set
        {
            Console.WriteLine($""this.set({value})"");
        }
    }

    int M(int i)
    {
        Console.WriteLine($""M({i})"");
        return 0;
    }

    void Test()
    {
        (this[z: M(1), x: M(2), y: 10], this[z: M(3), x: M(4), y: 10L]) = this;
        Console.WriteLine($""field: {field}"");
    }

    static void Main()
    {
        new C().Test();
    }

    void Deconstruct(out int a, out int b)
    {
        Console.WriteLine(nameof(Deconstruct));
        a = 1;
        b = 2;
    }
}
";

            var expectedOutput =
@"M(1)
M(2)
this.get
M(3)
M(4)
Deconstruct
this.set(2)
field: 1
";

            var comp = CompileAndVerify(source, expectedOutput: expectedOutput);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(18554, "https://github.com/dotnet/roslyn/issues/18554")]
        public void AssigningTupleIntoIndexers()
        {
            string source = @"
using System;
class C
{
    int field;
    ref int this[int x, int y, int z, int opt = 1]
    {
        get
        {
            Console.WriteLine($""this.get"");
            return ref field;
        }
    }

    int this[int x, long y, int z, int opt = 1]
    {
        set
        {
            Console.WriteLine($""this.set({value})"");
        }
    }

    int M(int i)
    {
        Console.WriteLine($""M({i})"");
        return 0;
    }

    void Test()
    {
        (this[z: M(1), x: M(2), y: 10], this[z: M(3), x: M(4), y: 10L]) = (1, 2);
        Console.WriteLine($""field: {field}"");
    }

    static void Main()
    {
        new C().Test();
    }
}
";

            var expectedOutput =
@"M(1)
M(2)
this.get
M(3)
M(4)
this.set(2)
field: 1
";
            var comp = CompileAndVerify(source, expectedOutput: expectedOutput);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningIntoIndexerWithOptionalValueParameter()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )
    .method public hidebysig specialname 
        instance void set_Item (
            int32 i,
            [opt] int32 'value'
        ) cil managed 
    {
        .param [2] = int32(1)
        .maxstack 8
        IL_0000: ldstr ""this.set({0})""
        IL_0005: ldarg.2            
        IL_0006: box[mscorlib]System.Int32 
        IL_000b: call string[mscorlib] System.String::Format(string, object) 
        IL_0010: call void [mscorlib]System.Console::WriteLine(string) 
        IL_0015: ret                
        } // end of method C::set_Item

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor() 
        IL_0006: ret
    } // end of method C::.ctor
    .property instance int32 Item(
        int32 i
    )
    {
        .set instance void C::set_Item(int32, int32)
    }

} // end of class C
";

            var source = @"
class Program
{

    static void Main()
    {
        var c = new C();
        (c[1], c[2]) = (1, 2);
    }
}
";

            string expectedOutput =
@"this.set(1)
this.set(2)
";

            var comp = CreateCompilationWithILAndMscorlib40(source, ilSource, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
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

            var comp = CompileAndVerify(source, expectedOutput: "4 2");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Swap", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldsfld     ""int C.y""
  IL_0005:  ldsfld     ""int C.x""
  IL_000a:  stloc.0
  IL_000b:  stsfld     ""int C.x""
  IL_0010:  ldloc.0
  IL_0011:  stsfld     ""int C.y""
  IL_0016:  ret
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
        (object i, object ii) x = (1, 2);
        object y;

        (x.ii, y) = x;
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "(1, 1) 2");
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

            var comp = CompileAndVerify(source, expectedOutput: "(1, 1) 2", parseOptions: TestOptions.Regular.WithRefsFeature());
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

            var comp = CompileAndVerify(source, expectedOutput: "1 2", parseOptions: TestOptions.Regular.WithRefsFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedDeconstructUsingSystemTupleExtensionMethod()
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

            var comp = CompileAndVerify(source, expectedOutput: "1 hello world", parseOptions: TestOptions.Regular.WithRefsFeature());
            comp.VerifyDiagnostics();

            var tree = comp.Compilation.SyntaxTrees.First();
            var model = comp.Compilation.GetSemanticModel(tree);
            var deconstruction = (AssignmentExpressionSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.SimpleAssignmentExpression).AsNode();
            Assert.Equal(@"(x, (y, z)) = Tuple.Create(1, Tuple.Create(""hello"", ""world""))", deconstruction.ToString());
            var deconstructionInfo = model.GetDeconstructionInfo(deconstruction);

            Assert.Equal("void System.TupleExtensions.Deconstruct<System.Int32, System.Tuple<System.String, System.String>>(" +
                "this System.Tuple<System.Int32, System.Tuple<System.String, System.String>> value, " +
                "out System.Int32 item1, out System.Tuple<System.String, System.String> item2)",
                deconstructionInfo.Method.ToTestDisplayString());
            Assert.Null(deconstructionInfo.Conversion);

            var nested = deconstructionInfo.Nested;
            Assert.Equal(2, nested.Length);

            Assert.Null(nested[0].Method);
            Assert.Equal(ConversionKind.Identity, nested[0].Conversion.Value.Kind);
            Assert.Empty(nested[0].Nested);

            Assert.Equal("void System.TupleExtensions.Deconstruct<System.String, System.String>(" +
                "this System.Tuple<System.String, System.String> value, " +
                "out System.String item1, out System.String item2)",
                nested[1].Method.ToTestDisplayString());
            Assert.Null(nested[1].Conversion);

            var nested2 = nested[1].Nested;
            Assert.Equal(2, nested.Length);

            Assert.Null(nested2[0].Method);
            Assert.Equal(ConversionKind.Identity, nested2[0].Conversion.Value.Kind);
            Assert.Empty(nested2[0].Nested);

            Assert.Null(nested2[1].Method);
            Assert.Equal(ConversionKind.Identity, nested2[1].Conversion.Value.Kind);
            Assert.Empty(nested2[1].Nested);
        }

        [Fact]
        public void DeconstructUsingValueTupleExtensionMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y, z;
        (x, y, z) = (1, 2);
    }
}
public static class Extensions
{
    public static void Deconstruct(this (int, int) self, out int x, out string y, out string z)
    {
        throw null;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,25): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         (x, y, z) = (1, 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "2").WithArguments("int", "string").WithLocation(8, 25),
                // (8,9): error CS8132: Cannot deconstruct a tuple of '2' elements into '3' variables.
                //         (x, y, z) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, "(x, y, z) = (1, 2)").WithArguments("2", "3").WithLocation(8, 9)
                );
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

            var comp = CompileAndVerify(source, expectedOutput: "override", parseOptions: TestOptions.Regular.WithRefsFeature());
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
                var comp = CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular.WithRefsFeature());
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

            var comp = CompileAndVerify(source, expectedOutput: "1 hello");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructRefExtensionMethod()
        {
            // https://github.com/dotnet/csharplang/blob/main/meetings/2018/LDM-2018-01-24.md
            string source = @"
struct C
{
    static void Main()
    {
        long x;
        string y;
        
        var c = new C();
        (x, y) = c;
        System.Console.WriteLine(x + "" "" + y);
    }
}
static class D
{
    public static void Deconstruct(this ref C value, out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (10,9): error CS1510: A ref or out value must be an assignable variable
                //         (x, y) = c;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(x, y) = c").WithLocation(10, 9)
                );
        }

        [Fact]
        public void DeconstructInExtensionMethod()
        {
            string source = @"
struct C
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
    public static void Deconstruct(this in C value, out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello");
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

            var comp = CreateCompilation(source);
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

            var comp = CompileAndVerify(source, expectedOutput: "Deconstructed");
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

            var comp = CompileAndVerify(source, expectedOutput: "Deconstructed");
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

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,18): error CS0411: The type arguments for method 'C1.Deconstruct<T>(out int, out T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         (x, y) = new C1();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "new C1()").WithArguments("C1.Deconstruct<T>(out int, out T)").WithLocation(9, 18),
                // (9,18): error CS8129: No Deconstruct instance or extension method was found for type 'C1', with 2 out parameters and a void return type.
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

            var comp = CompileAndVerify(source, expectedOutput: "2 hello");
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
        long x;
        string y, z;

        (x, (y, z)) = ((int)1, (""a"", ""b""));
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
                Assert.Equal("(System.Int64 x, (System.String y, System.String z))", model.GetTypeInfo(lhs).Type.ToTestDisplayString());
                Assert.Equal("(System.Int64 x, (System.String y, System.String z))", model.GetTypeInfo(lhs).ConvertedType.ToTestDisplayString());
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 a b", sourceSymbolValidator: validator);
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
            var comp = CompileAndVerify(source, expectedOutput: "nothing");
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
            var comp = CompileAndVerify(source, expectedOutput: "1 a b");
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
            var comp = CompileAndVerify(source, expectedOutput: "1 2 3");
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
            var comp = CompileAndVerify(source, expectedOutput: "1 a b");
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
            var comp = CompileAndVerify(source, expectedOutput: expected);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyNestedExecutionOrder_Conditional()
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
        bool b = false;
        (c.getHolderForX().x, (c.getHolderForY().y, c.getHolderForZ().z)) = b ? default : c.getDeconstructReceiver();
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
            var comp = CompileAndVerify(source, expectedOutput: expected);
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
            var comp = CompileAndVerify(source, expectedOutput: expected);
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

            var comp = CompileAndVerify(source, expectedOutput: "1 hello");
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

            var comp = CompileAndVerify(source, expectedOutput: "1 hello");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (int V_0, //x1
                string V_1) //x2
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldstr      ""hello""
  IL_0007:  stloc.1
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""string int.ToString()""
  IL_000f:  ldstr      "" ""
  IL_0014:  ldloc.1
  IL_0015:  call       ""string string.Concat(string, string, string)""
  IL_001a:  call       ""void System.Console.WriteLine(string)""
  IL_001f:  ret
}");
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
                Assert.Equal("(System.Int32 x1, (System.Int32 x2, System.String x3))", model.GetTypeInfo(lhs).Type.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(lhs).Symbol);

                var lhsNested = tree.GetRoot().DescendantNodes().OfType<ParenthesizedVariableDesignationSyntax>().ElementAt(1);
                Assert.Equal(@"(x2, x3)", lhsNested.ToString());
                Assert.Null(model.GetTypeInfo(lhsNested).Type);
                Assert.Null(model.GetSymbolInfo(lhsNested).Symbol);

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

            var comp = CompileAndVerify(source, expectedOutput: "1 2 hello", sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedVarDeconstructionDeclaration_WithCSharp7_1()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int x1, var (x2, (x3, x4)), var x5) = (1, (2, (3, ""hello"")), 5);
        System.Console.WriteLine($""{x1} {x2} {x3} {x4} {x5}"");
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
                Assert.Equal(@"(int x1, var (x2, (x3, x4)), var x5)", lhs.ToString());
                Assert.Equal("(System.Int32 x1, (System.Int32 x2, (System.Int32 x3, System.String x4)), System.Int32 x5)",
                    model.GetTypeInfo(lhs).Type.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(lhs).Symbol);

                var x234 = tree.GetRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ElementAt(1);
                Assert.Equal(@"var (x2, (x3, x4))", x234.ToString());
                Assert.Equal("(System.Int32 x2, (System.Int32 x3, System.String x4))", model.GetTypeInfo(x234).Type.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(x234).Symbol);

                var x34 = tree.GetRoot().DescendantNodes().OfType<ParenthesizedVariableDesignationSyntax>().ElementAt(1);
                Assert.Equal(@"(x3, x4)", x34.ToString());
                Assert.Null(model.GetTypeInfo(x34).Type);
                Assert.Null(model.GetSymbolInfo(x34).Symbol);

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

                var x5 = GetDeconstructionVariable(tree, "x5");
                var x5Ref = GetReference(tree, "x5");
                VerifyModelForDeconstructionLocal(model, x5, x5Ref);
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2 3 hello 5",
                sourceSymbolValidator: validator, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedVarDeconstructionAssignment_WithCSharp7_1()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1, x2, x3;
        (x1, (x2, x3)) = (1, (2, 3));
        System.Console.WriteLine($""{x1} {x2} {x3}"");
    }
}
";

            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var x123 = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().First();
                Assert.Equal(@"(x1, (x2, x3))", x123.ToString());
                Assert.Equal("(System.Int32 x1, (System.Int32 x2, System.Int32 x3))",
                    model.GetTypeInfo(x123).Type.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(x123).Symbol);

                var x23 = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
                Assert.Equal(@"(x2, x3)", x23.ToString());
                Assert.Equal("(System.Int32 x2, System.Int32 x3)", model.GetTypeInfo(x23).Type.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(x23).Symbol);
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2 3",
                sourceSymbolValidator: validator, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedVarDeconstructionDeclaration2()
        {
            string source = @"
class C
{
    static void Main()
    {
        (var x1, var (x2, x3)) = (1, (2, ""hello""));
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
                Assert.Equal("(var x1, var (x2, x3))", lhs.ToString());
                Assert.Equal("(System.Int32 x1, (System.Int32 x2, System.String x3))", model.GetTypeInfo(lhs).Type.ToTestDisplayString());
                Assert.Equal("(System.Int32 x1, (System.Int32 x2, System.String x3))", model.GetTypeInfo(lhs).ConvertedType.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(lhs).Symbol);

                var lhsNested = tree.GetRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ElementAt(1);
                Assert.Equal("var (x2, x3)", lhsNested.ToString());
                Assert.Equal("(System.Int32 x2, System.String x3)", model.GetTypeInfo(lhsNested).Type.ToTestDisplayString());
                Assert.Equal("(System.Int32 x2, System.String x3)", model.GetTypeInfo(lhsNested).ConvertedType.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(lhsNested).Symbol);

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

            var comp = CompileAndVerify(source, expectedOutput: "1 2 hello", sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedVarDeconstructionDeclarationWithCSharp7_1()
        {
            string source = @"
class C
{
    static void Main()
    {
        (var x1, byte _, var (x2, x3)) = (1, 2, (3, ""hello""));
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
                Assert.Equal("(var x1, byte _, var (x2, x3))", lhs.ToString());
                Assert.Equal("(System.Int32 x1, System.Byte, (System.Int32 x2, System.String x3))", model.GetTypeInfo(lhs).Type.ToTestDisplayString());
                Assert.Equal("(System.Int32 x1, System.Byte, (System.Int32 x2, System.String x3))", model.GetTypeInfo(lhs).ConvertedType.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(lhs).Symbol);

                var lhsNested = tree.GetRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ElementAt(2);
                Assert.Equal("var (x2, x3)", lhsNested.ToString());
                Assert.Equal("(System.Int32 x2, System.String x3)", model.GetTypeInfo(lhsNested).Type.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(lhsNested).Symbol);
            };

            var comp = CompileAndVerify(source, sourceSymbolValidator: validator,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
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

            var comp = CompileAndVerify(source, expectedOutput: "1 2 hello", sourceSymbolValidator: validator);
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

            var comp = CompileAndVerify(source, expectedOutput: "var");
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
            var comp = CompileAndVerify(source, expectedOutput: " 1 2");
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
                Assert.Equal("(System.String x1, System.Byte x2, System.Int32 x3)", model.GetTypeInfo(lhs).Type.ToTestDisplayString());

                var literal = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
                Assert.Equal(@"(null, 2, 3)", literal.ToString());
                Assert.Null(model.GetTypeInfo(literal).Type);
                Assert.Equal("(System.String, System.Byte, System.Int32)", model.GetTypeInfo(literal).ConvertedType.ToTestDisplayString());
                Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(literal).Kind);
            };

            var comp = CompileAndVerify(source, expectedOutput: " 2 3", sourceSymbolValidator: validator);
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
                Assert.Equal("(System.String x1, (System.Int32, System.Int32) x2)", model.GetTypeInfo(lhs).Type.ToTestDisplayString());

                var literal = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
                Assert.Equal(@"(null, (1, 2))", literal.ToString());
                Assert.Null(model.GetTypeInfo(literal).Type);
                Assert.Equal("(System.String, (System.Int32, System.Int32))", model.GetTypeInfo(literal).ConvertedType.ToTestDisplayString());
                Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(literal).Kind);

                var nestedLiteral = literal.Arguments[1].Expression;
                Assert.Equal(@"(1, 2)", nestedLiteral.ToString());
                Assert.Equal("(System.Int32, System.Int32)", model.GetTypeInfo(nestedLiteral).Type.ToTestDisplayString());
                Assert.Equal("(System.Int32, System.Int32)", model.GetTypeInfo(nestedLiteral).ConvertedType.ToTestDisplayString());
                Assert.Equal(ConversionKind.Identity, model.GetConversion(nestedLiteral).Kind);
            };

            var comp = CompileAndVerify(source, expectedOutput: " (1, 2)", sourceSymbolValidator: validator);
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
            var comp = CompileAndVerify(source, expectedOutput: " 2 3 4");
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
                            sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        private static void VerifyModelForDeconstructionLocal(SemanticModel model, SingleVariableDesignationSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeconstruction(model, decl, LocalDeclarationKind.DeconstructionVariable, references);
        }

        private static void VerifyModelForLocal(SemanticModel model, SingleVariableDesignationSyntax decl, LocalDeclarationKind kind, params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeconstruction(model, decl, kind, references);
        }

        private static void VerifyModelForDeconstructionForeach(SemanticModel model, SingleVariableDesignationSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeconstruction(model, decl, LocalDeclarationKind.ForEachIterationVariable, references);
        }

        private static void VerifyModelForDeconstruction(SemanticModel model, SingleVariableDesignationSyntax decl, LocalDeclarationKind kind, params IdentifierNameSyntax[] references)
        {
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(kind, symbol.GetSymbol<LocalSymbol>().DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)decl));
            Assert.Same(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));

            var local = symbol.GetSymbol<SourceLocalSymbol>();
            var typeSyntax = GetTypeSyntax(decl);
            if (local.IsVar && local.Type.IsErrorType())
            {
                Assert.Null(model.GetSymbolInfo(typeSyntax).Symbol);
            }
            else
            {
                if (typeSyntax != null)
                {
                    Assert.Equal(local.Type.GetPublicSymbol(), model.GetSymbolInfo(typeSyntax).Symbol);
                }
            }

            foreach (var reference in references)
            {
                Assert.Same(symbol, model.GetSymbolInfo(reference).Symbol);
                Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: decl.Identifier.ValueText).Single());
                Assert.True(model.LookupNames(reference.SpanStart).Contains(decl.Identifier.ValueText));
                Assert.Equal(local.Type.GetPublicSymbol(), model.GetTypeInfo(reference).Type);
            }
        }

        private static void VerifyModelForDeconstructionField(SemanticModel model, SingleVariableDesignationSyntax decl, params IdentifierNameSyntax[] references)
        {
            var field = (IFieldSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, field.Name);
            Assert.Equal(SymbolKind.Field, field.Kind);
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

        private static IEnumerable<DiscardDesignationSyntax> GetDiscardDesignations(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes().OfType<DiscardDesignationSyntax>();
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
class @var
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

            var comp = CompileAndVerify(source, expectedOutput: "var 2", sourceSymbolValidator: validator);
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

            var comp = CompileAndVerify(source, expectedOutput: "1 2 3 4", sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeclarationWithAliasedVarType()
        {
            string source = @"
using @var = D;
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

            var comp = CompileAndVerify(source, expectedOutput: "var 2", sourceSymbolValidator: validator);
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

            var comp = CompileAndVerify(source, expectedOutput: "1 2", sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ForWithVarDeconstructInitializersCanParse()
        {
            string source = @"
using System;
class C
{
    static void Main()
    {
        int x3;
        for (var (x1, x2) = (1, 2), x3 = 3; true; )
        {
            Console.WriteLine(x1);
            Console.WriteLine(x2);
            Console.WriteLine(x3);
            break;
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
                var x1Ref = GetReference(tree, "x1");
                VerifyModelForDeconstructionLocal(model, x1, x1Ref);

                var x2 = GetDeconstructionVariable(tree, "x2");
                var x2Ref = GetReference(tree, "x2");
                VerifyModelForDeconstructionLocal(model, x2, x2Ref);
            };

            var comp = CompileAndVerify(source, expectedOutput: @"1
2
3", sourceSymbolValidator: validator);
            comp.VerifyDiagnostics(
                // this is permitted now, as it is just an assignment expression
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
class @var
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

            var comp = CompileAndVerify(source, expectedOutput: "1 var", sourceSymbolValidator: validator);
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

            var comp = CompileAndVerify(source, expectedOutput: "1 2", sourceSymbolValidator: validator);
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
                Assert.Equal("(System.Int32 x1, System.Int32 x2)", model.GetTypeInfo(x12Var).Type.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(x12Var.Type).Symbol); // The var in `var (x1, x2)` has no symbol

                // verify deconstruction info
                var deconstructionForeach = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single();
                var deconstructionInfo = model.GetDeconstructionInfo(deconstructionForeach);

                Assert.Null(deconstructionInfo.Method);
                Assert.Null(deconstructionInfo.Conversion);

                var nested = deconstructionInfo.Nested;
                Assert.Equal(2, nested.Length);

                Assert.Null(nested[0].Method);
                Assert.Equal(ConversionKind.Identity, nested[0].Conversion.Value.Kind);
                Assert.Empty(nested[0].Nested);

                Assert.Null(nested[1].Method);
                Assert.Equal(ConversionKind.Identity, nested[1].Conversion.Value.Kind);
                Assert.Empty(nested[1].Nested);
            };

            var comp = CompileAndVerify(source, expectedOutput: "1 2", sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();

            var comp7_1 = CompileAndVerify(source, expectedOutput: "1 2",
                sourceSymbolValidator: validator, parseOptions: TestOptions.Regular7_1);
            comp7_1.VerifyDiagnostics();

            comp.VerifyIL("C.Main",
@"{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> V_0,
                int V_1, //x1
                int V_2) //x2
  IL_0000:  call       ""System.Collections.Generic.IEnumerable<System.ValueTuple<int, int>> C.M()""
  IL_0005:  callvirt   ""System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> System.Collections.Generic.IEnumerable<System.ValueTuple<int, int>>.GetEnumerator()""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  br.s       IL_0031
    IL_000d:  ldloc.0
    IL_000e:  callvirt   ""System.ValueTuple<int, int> System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>>.Current.get""
    IL_0013:  dup
    IL_0014:  ldfld      ""int System.ValueTuple<int, int>.Item1""
    IL_0019:  stloc.1
    IL_001a:  ldfld      ""int System.ValueTuple<int, int>.Item2""
    IL_001f:  stloc.2
    IL_0020:  ldloc.1
    IL_0021:  box        ""int""
    IL_0026:  ldloc.2
    IL_0027:  box        ""int""
    IL_002c:  call       ""void C.Print(object, object)""
    IL_0031:  ldloc.0
    IL_0032:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0037:  brtrue.s   IL_000d
    IL_0039:  leave.s    IL_0045
  }
  finally
  {
    IL_003b:  ldloc.0
    IL_003c:  brfalse.s  IL_0044
    IL_003e:  ldloc.0
    IL_003f:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0044:  endfinally
  }
  IL_0045:  ret
}");
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

            var comp = CompileAndVerify(source, expectedOutput: "1 2 - 3 4 -", sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main",
@"{
  // Code size       75 (0x4b)
  .maxstack  4
  .locals init (System.ValueTuple<int, int>[] V_0,
                int V_1,
                int V_2, //x1
                int V_3) //x2
  IL_0000:  call       ""System.ValueTuple<int, int>[] C.M()""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  IL_0008:  br.s       IL_0044
  IL_000a:  ldloc.0
  IL_000b:  ldloc.1
  IL_000c:  ldelem     ""System.ValueTuple<int, int>""
  IL_0011:  dup
  IL_0012:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0017:  stloc.2
  IL_0018:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_001d:  stloc.3
  IL_001e:  ldloca.s   V_2
  IL_0020:  call       ""string int.ToString()""
  IL_0025:  ldstr      "" ""
  IL_002a:  ldloca.s   V_3
  IL_002c:  call       ""string int.ToString()""
  IL_0031:  ldstr      "" - ""
  IL_0036:  call       ""string string.Concat(string, string, string, string)""
  IL_003b:  call       ""void System.Console.Write(string)""
  IL_0040:  ldloc.1
  IL_0041:  ldc.i4.1
  IL_0042:  add
  IL_0043:  stloc.1
  IL_0044:  ldloc.1
  IL_0045:  ldloc.0
  IL_0046:  ldlen
  IL_0047:  conv.i4
  IL_0048:  blt.s      IL_000a
  IL_004a:  ret
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

            var comp = CompileAndVerify(source, expectedOutput: "1 2 - 3 4 - 5 6 - 7 8 -", sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main",
@"{
  // Code size      106 (0x6a)
  .maxstack  3
  .locals init (System.ValueTuple<int, int>[,] V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4,
                int V_5, //x1
                int V_6) //x2
  IL_0000:  call       ""System.ValueTuple<int, int>[,] C.M()""
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
  IL_001e:  br.s       IL_0065
  IL_0020:  ldloc.0
  IL_0021:  ldc.i4.1
  IL_0022:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0027:  stloc.s    V_4
  IL_0029:  br.s       IL_005c
  IL_002b:  ldloc.0
  IL_002c:  ldloc.3
  IL_002d:  ldloc.s    V_4
  IL_002f:  call       ""(int, int)[*,*].Get""
  IL_0034:  dup
  IL_0035:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_003a:  stloc.s    V_5
  IL_003c:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0041:  stloc.s    V_6
  IL_0043:  ldloc.s    V_5
  IL_0045:  box        ""int""
  IL_004a:  ldloc.s    V_6
  IL_004c:  box        ""int""
  IL_0051:  call       ""void C.Print(object, object)""
  IL_0056:  ldloc.s    V_4
  IL_0058:  ldc.i4.1
  IL_0059:  add
  IL_005a:  stloc.s    V_4
  IL_005c:  ldloc.s    V_4
  IL_005e:  ldloc.2
  IL_005f:  ble.s      IL_002b
  IL_0061:  ldloc.3
  IL_0062:  ldc.i4.1
  IL_0063:  add
  IL_0064:  stloc.3
  IL_0065:  ldloc.3
  IL_0066:  ldloc.1
  IL_0067:  ble.s      IL_0020
  IL_0069:  ret
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

            var comp = CompileAndVerify(source, expectedOutput: "1 1 - 2 2 - 3 3 - ", sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main",
@"{
  // Code size       60 (0x3c)
  .maxstack  3
  .locals init (string V_0,
                int V_1,
                int V_2, //x2
                int V_3,
                int V_4)
  IL_0000:  call       ""string C.M()""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  IL_0008:  br.s       IL_0032
  IL_000a:  ldloc.0
  IL_000b:  ldloc.1
  IL_000c:  callvirt   ""char string.this[int].get""
  IL_0011:  ldloca.s   V_3
  IL_0013:  ldloca.s   V_4
  IL_0015:  call       ""void Extension.Deconstruct(char, out int, out int)""
  IL_001a:  ldloc.3
  IL_001b:  ldloc.s    V_4
  IL_001d:  stloc.2
  IL_001e:  box        ""int""
  IL_0023:  ldloc.2
  IL_0024:  box        ""int""
  IL_0029:  call       ""void C.Print(object, object)""
  IL_002e:  ldloc.1
  IL_002f:  ldc.i4.1
  IL_0030:  add
  IL_0031:  stloc.1
  IL_0032:  ldloc.1
  IL_0033:  ldloc.0
  IL_0034:  callvirt   ""int string.Length.get""
  IL_0039:  blt.s      IL_000a
  IL_003b:  ret
}");
        }

        [Fact]
        [WorkItem(22495, "https://github.com/dotnet/roslyn/issues/22495")]
        public void ForEachCollectionSymbol()
        {
            string source = @"
using System.Collections.Generic;
class Deconstructable
{
    void M(IEnumerable<Deconstructable> x)
    {
        foreach (var (y1, y2) in x)
        {
        }
    }
    void Deconstruct(out int i, out int j) { i = 0; j = 0; }
}
";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var collection = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single().Expression;
            Assert.Equal("x", collection.ToString());
            var symbol = model.GetSymbolInfo(collection).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal("x", symbol.Name);
            Assert.Equal("System.Collections.Generic.IEnumerable<Deconstructable> x", symbol.ToTestDisplayString());
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

            var comp = CompileAndVerify(source, expectedOutput: "1 2 3 4 5 - 6 7 8 9 10 -", sourceSymbolValidator: validator);
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

            var comp = CompileAndVerify(source, expectedOutput: "1 2 3 4 5 - 6 7 8 9 10 -");
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

            var comp = CompileAndVerify(source, expectedOutput: "1 2 3 4 5 - 6 7 8 9 10 -");
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

            var comp = CompileAndVerify(source, expectedOutput: "1 1 1 - 2 2 2 - ");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructExtensionOnInterface()
        {
            string source = @"
public interface Interface { }
class C : Interface
{
    static void Main()
    {
        var (x, y) = new C();
        System.Console.Write($""{x} {y}"");
    }
}
static class Extension
{
    public static void Deconstruct(this Interface value, out int item1, out string item2)
    {
        item1 = 42;
        item2 = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "42 hello");
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

            var comp = CompileAndVerify(source, expectedOutput: expected, sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main",
@"{
  // Code size       90 (0x5a)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerator<Pair<int, Pair<int, int>>> V_0,
                int V_1, //x2
                int V_2, //x3
                int V_3,
                Pair<int, int> V_4,
                int V_5,
                int V_6)
  IL_0000:  call       ""System.Collections.Generic.IEnumerable<Pair<int, Pair<int, int>>> C.M()""
  IL_0005:  callvirt   ""System.Collections.Generic.IEnumerator<Pair<int, Pair<int, int>>> System.Collections.Generic.IEnumerable<Pair<int, Pair<int, int>>>.GetEnumerator()""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  br.s       IL_0045
    IL_000d:  ldloc.0
    IL_000e:  callvirt   ""Pair<int, Pair<int, int>> System.Collections.Generic.IEnumerator<Pair<int, Pair<int, int>>>.Current.get""
    IL_0013:  ldloca.s   V_3
    IL_0015:  ldloca.s   V_4
    IL_0017:  callvirt   ""void Pair<int, Pair<int, int>>.Deconstruct(out int, out Pair<int, int>)""
    IL_001c:  ldloc.s    V_4
    IL_001e:  ldloca.s   V_5
    IL_0020:  ldloca.s   V_6
    IL_0022:  callvirt   ""void Pair<int, int>.Deconstruct(out int, out int)""
    IL_0027:  ldloc.3
    IL_0028:  conv.i8
    IL_0029:  ldloc.s    V_5
    IL_002b:  stloc.1
    IL_002c:  ldloc.s    V_6
    IL_002e:  stloc.2
    IL_002f:  box        ""long""
    IL_0034:  ldloc.1
    IL_0035:  box        ""int""
    IL_003a:  ldloc.2
    IL_003b:  box        ""int""
    IL_0040:  call       ""void C.Print(object, object, object)""
    IL_0045:  ldloc.0
    IL_0046:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_004b:  brtrue.s   IL_000d
    IL_004d:  leave.s    IL_0059
  }
  finally
  {
    IL_004f:  ldloc.0
    IL_0050:  brfalse.s  IL_0058
    IL_0052:  ldloc.0
    IL_0053:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0058:  endfinally
  }
  IL_0059:  ret
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

            var comp = CompileAndVerify(source, expectedOutput: expected);
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

            var comp = CompileAndVerify(source, expectedOutput: expected);
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

            var comp = CompileAndVerify(source, expectedOutput: "1 2 - 3 4 -");
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
            var comp = CompileAndVerify(source, expectedOutput: "0 10 ");
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
            var comp = CompileAndVerify(source, expectedOutput: "hello world", references: new[] { CSharpRef });
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
            var comp = CompileAndVerify(source, expectedOutput: "3 4", references: new[] { CSharpRef });
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
            var comp = CompileAndVerify(source, expectedOutput: "hello world", references: new[] { CSharpRef });
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
            var comp = CompileAndVerify(source, expectedOutput: "hello world", references: new[] { CSharpRef });
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
            var comp = CompileAndVerify(source, expectedOutput: "hello world", references: new[] { CSharpRef });
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
            var comp = CompileAndVerify(source, expectedOutput: "3 4", references: new[] { CSharpRef });
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
            var comp = CompileAndVerify(source, expectedOutput: "1 2 3");
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
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,11): error CS1001: Identifier expected
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(4, 11),
                // (4,14): error CS1001: Identifier expected
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(4, 14),
                // (4,16): error CS1002: ; expected
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=").WithLocation(4, 16),
                // (4,16): error CS1519: Invalid token '=' in class, record, struct, or interface member declaration
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(4, 16),
                // (4,19): error CS1031: Type expected
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_TypeExpected, "1").WithLocation(4, 19),
                // (4,19): error CS8124: Tuple must contain at least two elements.
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, "1").WithLocation(4, 19),
                // (4,19): error CS1026: ) expected
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "1").WithLocation(4, 19),
                // (4,19): error CS1519: Invalid token '1' in class, record, struct, or interface member declaration
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "1").WithArguments("1").WithLocation(4, 19),
                // (4,5): error CS1520: Method must have a return type
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "var").WithLocation(4, 5),
                // (4,5): error CS0501: 'C.C(x, y)' must declare a body because it is not marked abstract, extern, or partial
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "var").WithArguments("C.C(x, y)").WithLocation(4, 5),
                // (4,10): error CS0246: The type or namespace name 'x' could not be found (are you missing a using directive or an assembly reference?)
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "x").WithArguments("x").WithLocation(4, 10),
                // (4,13): error CS0246: The type or namespace name 'y' could not be found (are you missing a using directive or an assembly reference?)
                //     var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "y").WithArguments("y").WithLocation(4, 13)
                );

            var nodes = comp.SyntaxTrees[0].GetCompilationUnitRoot().DescendantNodesAndSelf();
            Assert.False(nodes.Any(n => n.Kind() == SyntaxKind.SimpleAssignmentExpression));
        }

        [Fact]
        public void SimpleDeconstructionInScript()
        {
            var source =
@"
using @alias = System.Int32;
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
  // Code size      129 (0x81)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int <<Initialize>>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldarg.0
    IL_0008:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_000d:  ldstr      ""hello""
    IL_0012:  stfld      ""string x""
    IL_0017:  ldarg.0
    IL_0018:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_001d:  ldc.i4.s   42
    IL_001f:  stfld      ""int y""
    IL_0024:  ldstr      ""{0} {1}""
    IL_0029:  ldarg.0
    IL_002a:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_002f:  ldfld      ""string x""
    IL_0034:  ldarg.0
    IL_0035:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_003a:  ldfld      ""int y""
    IL_003f:  box        ""int""
    IL_0044:  call       ""string string.Format(string, object, object)""
    IL_0049:  call       ""void System.Console.Write(string)""
    IL_004e:  nop
    IL_004f:  ldnull
    IL_0050:  stloc.1
    IL_0051:  leave.s    IL_006b
  }
  catch System.Exception
  {
    IL_0053:  stloc.2
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.s   -2
    IL_0057:  stfld      ""int <<Initialize>>d__0.<>1__state""
    IL_005c:  ldarg.0
    IL_005d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
    IL_0062:  ldloc.2
    IL_0063:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetException(System.Exception)""
    IL_0068:  nop
    IL_0069:  leave.s    IL_0080
  }
  IL_006b:  ldarg.0
  IL_006c:  ldc.i4.s   -2
  IL_006e:  stfld      ""int <<Initialize>>d__0.<>1__state""
  IL_0073:  ldarg.0
  IL_0074:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
  IL_0079:  ldloc.1
  IL_007a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetResult(object)""
  IL_007f:  nop
  IL_0080:  ret
}");
        }

        [Fact]
        public void GlobalDeconstructionOutsideScript()
        {
            var source =
@"
(string x, int y) = (""hello"", 42);
System.Console.Write(x);
System.Console.Write(y);
";
            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe, references: s_valueTupleRefs);

            comp.VerifyDiagnostics();

            var nodes = comp.SyntaxTrees[0].GetCompilationUnitRoot().DescendantNodesAndSelf();
            Assert.True(nodes.Any(n => n.Kind() == SyntaxKind.SimpleAssignmentExpression));

            CompileAndVerify(comp, expectedOutput: "hello42");
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
  // Code size      178 (0xb2)
  .maxstack  4
  .locals init (int V_0,
                object V_1,
                System.ValueTuple<int, int> V_2,
                System.Exception V_3)
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
    IL_0018:  call       ""System.ValueTuple<int, int> M(out int)""
    IL_001d:  stloc.2
    IL_001e:  ldarg.0
    IL_001f:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_0024:  ldloc.2
    IL_0025:  ldfld      ""int System.ValueTuple<int, int>.Item1""
    IL_002a:  stfld      ""int x2""
    IL_002f:  ldarg.0
    IL_0030:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_0035:  ldloc.2
    IL_0036:  ldfld      ""int System.ValueTuple<int, int>.Item2""
    IL_003b:  stfld      ""int x3""
    IL_0040:  ldstr      ""{0} {1} {2}""
    IL_0045:  ldarg.0
    IL_0046:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_004b:  ldfld      ""int x1""
    IL_0050:  box        ""int""
    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_005b:  ldfld      ""int x2""
    IL_0060:  box        ""int""
    IL_0065:  ldarg.0
    IL_0066:  ldfld      ""Script <<Initialize>>d__0.<>4__this""
    IL_006b:  ldfld      ""int x3""
    IL_0070:  box        ""int""
    IL_0075:  call       ""string string.Format(string, object, object, object)""
    IL_007a:  call       ""void System.Console.Write(string)""
    IL_007f:  nop
    IL_0080:  ldnull
    IL_0081:  stloc.1
    IL_0082:  leave.s    IL_009c
  }
  catch System.Exception
  {
    IL_0084:  stloc.3
    IL_0085:  ldarg.0
    IL_0086:  ldc.i4.s   -2
    IL_0088:  stfld      ""int <<Initialize>>d__0.<>1__state""
    IL_008d:  ldarg.0
    IL_008e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
    IL_0093:  ldloc.3
    IL_0094:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetException(System.Exception)""
    IL_0099:  nop
    IL_009a:  leave.s    IL_00b1
  }
  IL_009c:  ldarg.0
  IL_009d:  ldc.i4.s   -2
  IL_009f:  stfld      ""int <<Initialize>>d__0.<>1__state""
  IL_00a4:  ldarg.0
  IL_00a5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
  IL_00aa:  ldloc.1
  IL_00ab:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetResult(object)""
  IL_00b0:  nop
  IL_00b1:  ret
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
                // (2,5): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                // var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(x, y)").WithArguments("tuples", "7.0").WithLocation(2, 5),
                // (2,14): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7 or greater.
                // var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(1, 2)").WithArguments("tuples", "7.0").WithLocation(2, 14)
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
            var xType = ((IFieldSymbol)xSymbol).Type;
            Assert.False(xType.IsErrorType());
            Assert.Equal("System.Int32", xType.ToTestDisplayString());

            var y = GetDeconstructionVariable(tree, "y");
            var ySymbol = model.GetDeclaredSymbol(y);
            Assert.Equal("System.Int32 Script.y", ySymbol.ToTestDisplayString());
            var yType = ((IFieldSymbol)ySymbol).Type;
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
            var xType = ((IFieldSymbol)xSymbol).Type;
            Assert.False(xType.IsErrorType());
            Assert.Equal("System.Int32", xType.ToTestDisplayString());

            var y = GetDeconstructionVariable(tree, "y");
            var ySymbol = model.GetDeclaredSymbol(y);
            Assert.Equal("System.Int32 Script.y", ySymbol.ToTestDisplayString());
            var yType = ((IFieldSymbol)ySymbol).Type;
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
            var xType = ((IFieldSymbol)xSymbol).Type;
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
            var xType = xSymbol.GetSymbol<FieldSymbol>().TypeWithAnnotations;
            Assert.True(xType.Type.IsErrorType());
            Assert.Equal("var", xType.ToTestDisplayString());

            var xTypeISymbol = xType.Type.GetPublicSymbol();
            Assert.Equal(SymbolKind.ErrorType, xTypeISymbol.Kind);

            var y = GetDeconstructionVariable(tree, "y");
            var ySymbol = model.GetDeclaredSymbol(y);
            Assert.Equal("var Script.y", ySymbol.ToTestDisplayString());
            var yType = ((IFieldSymbol)ySymbol).Type;
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
            var x1Type = ((IFieldSymbol)x1Symbol).Type;
            Assert.True(x1Type.IsErrorType());
            Assert.Equal("var", x1Type.Name);

            var x2 = GetDeconstructionVariable(tree, "x2");
            var x2Symbol = model.GetDeclaredSymbol(x2);
            var x2Ref = GetReference(tree, "x2");
            Assert.Equal("var Script.x2", x2Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x2, x2Ref);
            var x2Type = ((IFieldSymbol)x2Symbol).Type;
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
            var x1Type = ((IFieldSymbol)x1Symbol).Type;
            Assert.True(x1Type.IsErrorType());
            Assert.Equal("var", x1Type.Name);

            var x2 = GetDeconstructionVariable(tree, "x2");
            var x2Symbol = model.GetDeclaredSymbol(x2);
            var x2Ref = GetReference(tree, "x2");
            Assert.Equal("var Script.x2", x2Symbol.ToTestDisplayString());
            VerifyModelForDeconstructionField(model, x2, x2Ref);
            var x2Type = ((IFieldSymbol)x2Symbol).Type;
            Assert.True(x2Type.IsErrorType());
            Assert.Equal("var", x2Type.Name);
        }

        [Fact]
        public void VarAliasInVarDeconstructionInScript()
        {
            var source =
@"
using @var = System.Byte;
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
            Assert.Null(model.GetTypeInfo(x123Var.Type).Type);
            Assert.Null(model.GetSymbolInfo(x123Var.Type).Symbol); // The var in `var (x1, x2)` has no symbol
        }

        [Fact]
        public void VarTypeInVarDeconstructionInScript()
        {
            var source =
@"
class @var
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
            Assert.Null(model.GetTypeInfo(x123Var.Type).Type);
            Assert.Null(model.GetSymbolInfo(x123Var.Type).Symbol); // The var in `var (x1, x2)` has no symbol
        }

        [Fact]
        public void VarAliasInTypedDeconstructionInScript()
        {
            var source =
@"
using @var = System.Byte;
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
class @var
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
            Assert.Null(model.GetSymbolInfo(x1).Symbol);
            VerifyModelForDeconstructionField(model, x1, x1Ref);

            var x3 = GetDeconstructionVariable(tree, "x3");
            var x3Symbol = model.GetDeclaredSymbol(x3);
            var x3Ref = GetReference(tree, "x3");
            Assert.Equal("Script.var Script.x3", x3Symbol.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(x3).Symbol);
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Converted 1. Output 1 2 3.");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard1 = GetDiscardDesignations(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            Assert.Null(model.GetSymbolInfo(discard1).Symbol);
            var declaration1 = (DeclarationExpressionSyntax)discard1.Parent;
            Assert.Equal("int _", declaration1.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(declaration1).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(declaration1).Symbol);

            var discard2 = GetDiscardDesignations(tree).ElementAt(1);
            Assert.Null(model.GetDeclaredSymbol(discard2));
            Assert.Null(model.GetSymbolInfo(discard2).Symbol);
            var declaration2 = (DeclarationExpressionSyntax)discard2.Parent;
            Assert.Equal("var _", declaration2.ToString());
            Assert.Equal("C", model.GetTypeInfo(declaration2).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(declaration2).Symbol);

            var discard3 = GetDiscardDesignations(tree).ElementAt(2);
            var declaration3 = (DeclarationExpressionSyntax)discard3.Parent.Parent;
            Assert.Equal("var (_, z)", declaration3.ToString());
            Assert.Equal("(C, System.Int32 z)", model.GetTypeInfo(declaration3).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(declaration3).Symbol);
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard = GetDiscardIdentifiers(tree).First();
            var symbol = (IDiscardSymbol)model.GetSymbolInfo(discard).Symbol;
            Assert.Equal("int _", symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            Assert.Equal("System.Int32", model.GetTypeInfo(discard).Type.ToTestDisplayString());

            var isymbol = (ISymbol)symbol;
            Assert.Equal(SymbolKind.Discard, isymbol.Kind);
        }

        [Fact, WorkItem(25829, "https://github.com/dotnet/roslyn/issues/25829")]
        public void SameTypeDiscardsAreEqual01()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (_, _) = (1, 2);
        _ = 3;
        M(out _);
    }
    static void M(out int x) => x = 1;
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discards = GetDiscardIdentifiers(tree).ToArray();
            Assert.Equal(4, discards.Length);
            var symbol0 = (IDiscardSymbol)model.GetSymbolInfo(discards[0]).Symbol;
            Assert.Equal(symbol0, symbol0);
            var set = new HashSet<ISymbol>();
            foreach (var discard in discards)
            {
                var symbol = (IDiscardSymbol)model.GetSymbolInfo(discard).Symbol;
                set.Add(symbol);
                Assert.Equal(SymbolKind.Discard, symbol.Kind);
                Assert.Equal("int _", symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                Assert.Equal(symbol0, symbol);
                Assert.Equal(symbol, symbol);
                Assert.Equal(symbol.GetHashCode(), symbol0.GetHashCode());

                // Test to show that reference-unequal discards are equal by type.
                IDiscardSymbol symbolClone = new DiscardSymbol(TypeWithAnnotations.Create(symbol.Type.GetSymbol())).GetPublicSymbol();
                Assert.NotSame(symbol, symbolClone);
                Assert.Equal(SymbolKind.Discard, symbolClone.Kind);
                Assert.Equal("int _", symbolClone.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                Assert.Equal(symbol.Type, symbolClone.Type);
                Assert.Equal(symbol0, symbolClone);
                Assert.Equal(symbol, symbolClone);
                Assert.Same(symbol.Type, symbolClone.Type); // original symbol for System.Int32 has identity.
                Assert.Equal(symbol.GetHashCode(), symbolClone.GetHashCode());
            }

            Assert.Equal(1, set.Count);
        }

        [Fact, WorkItem(25829, "https://github.com/dotnet/roslyn/issues/25829")]
        public void SameTypeDiscardsAreEqual02()
        {
            var source =
@"using System.Collections.Generic;
class C
{
    static void Main()
    {
        (_, _) = (new List<int>(), new List<int>());
        _ = new List<int>();
        M(out _);
    }
    static void M(out List<int> x) => x = new List<int>();
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discards = GetDiscardIdentifiers(tree).ToArray();
            Assert.Equal(4, discards.Length);
            var symbol0 = (IDiscardSymbol)model.GetSymbolInfo(discards[0]).Symbol;
            Assert.Equal(symbol0, symbol0);
            var set = new HashSet<ISymbol>();
            foreach (var discard in discards)
            {
                var symbol = (IDiscardSymbol)model.GetSymbolInfo(discard).Symbol;
                set.Add(symbol);
                Assert.Equal(SymbolKind.Discard, symbol.Kind);
                Assert.Equal("List<int> _", symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                Assert.Equal(symbol0, symbol);
                Assert.Equal(symbol0.Type, symbol.Type);
                Assert.Equal(symbol, symbol);
                Assert.Equal(symbol.GetHashCode(), symbol0.GetHashCode());
                if (discard != discards[0])
                {
                    // Although it is not part of the compiler's contract, at the moment distinct constructions are distinct
                    Assert.NotSame(symbol.Type, symbol0.Type);
                    Assert.NotSame(symbol, symbol0);
                }
            }

            Assert.Equal(1, set.Count);
        }

        [Fact, WorkItem(25829, "https://github.com/dotnet/roslyn/issues/25829")]
        public void DifferentTypeDiscardsAreNotEqual()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (_, _) = (1.0, 2);
        _ = 3;
        M(out _);
    }
    static void M(out int x) => x = 1;
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discards = GetDiscardIdentifiers(tree).ToArray();
            Assert.Equal(4, discards.Length);
            var symbol0 = (IDiscardSymbol)model.GetSymbolInfo(discards[0]).Symbol;
            var set = new HashSet<ISymbol>();
            foreach (var discard in discards)
            {
                var symbol = (IDiscardSymbol)model.GetSymbolInfo(discard).Symbol;
                Assert.Equal(SymbolKind.Discard, symbol.Kind);
                set.Add(symbol);
                if (discard == discards[0])
                {
                    Assert.Equal("double _", symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                    Assert.Equal(symbol0, symbol);
                    Assert.Equal(symbol0.GetHashCode(), symbol.GetHashCode());
                }
                else
                {
                    Assert.Equal("int _", symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                    Assert.NotEqual(symbol0, symbol);
                }
            }

            Assert.Equal(2, set.Count);
        }

        [Fact]
        public void EscapedUnderscoreInDeclaration()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (@_, var x) = (1, 2);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,10): error CS0103: The name '_' does not exist in the current context
                //         (@_, var x) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@_").WithArguments("_").WithLocation(6, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            Assert.Empty(GetDiscardIdentifiers(tree));
        }

        [Fact]
        public void EscapedUnderscoreInDeclarationCSharp9()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (@_, var x) = (1, 2);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,9): error CS8773: Feature 'Mixed declarations and expressions in deconstruction' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         (@_, var x) = (1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "(@_, var x) = (1, 2)").WithArguments("Mixed declarations and expressions in deconstruction", "10.0").WithLocation(6, 9),
                // (6,10): error CS0103: The name '_' does not exist in the current context
                //         (@_, var x) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@_").WithArguments("_").WithLocation(6, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            Assert.Empty(GetDiscardIdentifiers(tree));
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1")
                .VerifyIL("C.Main", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (int V_0, //_
                int V_1) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.2
  IL_0004:  stloc.1
  IL_0005:  ldloc.0
  IL_0006:  call       ""void System.Console.Write(int)""
  IL_000b:  nop
  IL_000c:  ret
}");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard = GetDiscardIdentifiers(tree).First();
            Assert.Equal("(_, var x)", discard.Parent.Parent.ToString());
            var symbol = (ILocalSymbol)model.GetSymbolInfo(discard).Symbol;
            Assert.Equal("int _", symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            Assert.Equal("System.Int32", model.GetTypeInfo(discard).Type.ToTestDisplayString());
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "3");
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
        (_, x) = (1L, 2);
        System.Console.Write(x);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard1 = GetDiscardIdentifiers(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            Assert.Equal("long _", model.GetSymbolInfo(discard1).Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

            var tuple1 = (TupleExpressionSyntax)discard1.Parent.Parent;
            Assert.Equal("(_, x)", tuple1.ToString());
            Assert.Equal("(System.Int64, System.Int32 x)", model.GetTypeInfo(tuple1).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(tuple1).Symbol);
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard1 = GetDiscardDesignations(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            var tuple1 = (DeclarationExpressionSyntax)discard1.Parent.Parent;
            Assert.Equal("var (_, x)", tuple1.ToString());
            Assert.Equal("(System.Int32, System.Int32 x)", model.GetTypeInfo(tuple1).Type.ToTestDisplayString());
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard = GetDiscardIdentifiers(tree).First();
            Assert.Equal("(_, x)", discard.Parent.Parent.ToString());
            var symbol = (ILocalSymbol)model.GetSymbolInfo(discard).Symbol;
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
        foreach ((_, (var y, int z)) in new[] { (1, (""hello"", 2)) }) { System.Console.Write(""2""); }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            DiscardDesignationSyntax discard1 = GetDiscardDesignations(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            Assert.Null(model.GetTypeInfo(discard1).Type);

            var declaration1 = (DeclarationExpressionSyntax)discard1.Parent.Parent;
            Assert.Equal("var (_, x)", declaration1.ToString());
            Assert.Null(model.GetTypeInfo(discard1).Type);
            Assert.Equal("(System.Int32, System.String x)", model.GetTypeInfo(declaration1).Type.ToTestDisplayString());

            IdentifierNameSyntax discard2 = GetDiscardIdentifiers(tree).First();
            Assert.Equal("(_, (var y, int z))", discard2.Parent.Parent.ToString());
            Assert.Equal("int _", model.GetSymbolInfo(discard2).Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            Assert.Equal("System.Int32", model.GetTypeInfo(discard2).Type.ToTestDisplayString());

            var yz = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(2);
            Assert.Equal("(var y, int z)", yz.ToString());
            Assert.Equal("(System.String y, System.Int32 z)", model.GetTypeInfo(yz).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(yz).Symbol);

            var y = tree.GetRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ElementAt(1);
            Assert.Equal("var y", y.ToString());
            Assert.Equal("System.String", model.GetTypeInfo(y).Type.ToTestDisplayString());
            Assert.Equal("System.String y", model.GetSymbolInfo(y).Symbol.ToTestDisplayString());
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
            Action<ModuleSymbol> validator = (ModuleSymbol module) =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var refs = GetReferences(tree, "_");
                Assert.Equal(2, refs.Count());
                model.GetTypeInfo(refs.ElementAt(0)); //  Assert.Equal("int", model.GetTypeInfo(refs.ElementAt(0)).Type.ToDisplayString());
                model.GetTypeInfo(refs.ElementAt(1)); //  Assert.Equal("string", model.GetTypeInfo(refs.ElementAt(1)).Type.ToDisplayString());

                var tuple = (TupleExpressionSyntax)refs.ElementAt(0).Parent.Parent;
                Assert.Equal("(_, _)", tuple.ToString());
                Assert.Equal("(System.Int32, System.String)", model.GetTypeInfo(tuple).Type.ToTestDisplayString());
            };

            var comp = CompileAndVerify(source, expectedOutput: @"2", sourceSymbolValidator: validator);
            comp.VerifyDiagnostics(
                // this is permitted now, as it is just an assignment expression
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
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (11,30): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //             foreach ((var y, _) in new[] { (1, "hello") }) { System.Console.Write("4"); } // error
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "_").WithArguments("string", "int").WithLocation(11, 30),
                // (11,22): error CS8186: A foreach loop must declare its iteration variables.
                //             foreach ((var y, _) in new[] { (1, "hello") }) { System.Console.Write("4"); } // error
                Diagnostic(ErrorCode.ERR_MustDeclareForeachIteration, "(var y, _)").WithLocation(11, 22),
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "CC");
        }

        [Fact]
        public void VerifyDiscardIL()
        {
            var source =
@"
class C
{
    C()
    {
        System.Console.Write(""ctor"");
    }
    static int Main()
    {
        var (x, _, _) = (1, new C(), 2);
        return x;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "ctor");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main()", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  pop
  IL_0006:  ldc.i4.1
  IL_0007:  ret
}");
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "M");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard1 = GetDiscardIdentifiers(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            Assert.Equal("System.Int32", model.GetTypeInfo(discard1).Type.ToTestDisplayString());
        }

        [Fact]
        public void SingleDiscardInAssignmentInCSharp6()
        {
            var source =
@"
class C
{
    static void Error()
    {
        _ = 1;
    }
    static void Ok()
    {
        int _;
        _ = 1;
        System.Console.Write(_);
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular6);
            comp.VerifyDiagnostics(
                // (6,9): error CS8059: Feature 'discards' is not available in C# 6. Please use language version 7.0 or greater.
                //         _ = 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "_").WithArguments("discards", "7.0").WithLocation(6, 9)
                );
        }

        [Fact]
        public void VariousDiscardsInCSharp6()
        {
            var source =
@"
class C
{
    static void M1(out int x)
    {
        (_, var _, int _) = (1, 2, 3);
        var (_, _) = (1, 2);
        bool b = 3 is int _;
        switch (3)
        {
            case int _:
                break;
        }
        M1(out var _);
        M1(out int _);
        M1(out _);
        x = 2;
    }
    static void M2()
    {
        const int _ = 3;
        switch (3)
        {
            case _: // not a discard
                break;
        }
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular6);
            comp.VerifyDiagnostics(
                // (6,9): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         (_, var _, int _) = (1, 2, 3);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(_, var _, int _)").WithArguments("tuples", "7.0").WithLocation(6, 9),
                // (6,10): error CS8059: Feature 'discards' is not available in C# 6. Please use language version 7.0 or greater.
                //         (_, var _, int _) = (1, 2, 3);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "_").WithArguments("discards", "7.0").WithLocation(6, 10),
                // (6,29): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         (_, var _, int _) = (1, 2, 3);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(1, 2, 3)").WithArguments("tuples", "7.0").WithLocation(6, 29),
                // (7,13): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         var (_, _) = (1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(_, _)").WithArguments("tuples", "7.0").WithLocation(7, 13),
                // (7,22): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         var (_, _) = (1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(1, 2)").WithArguments("tuples", "7.0").WithLocation(7, 22),
                // (8,18): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //         bool b = 3 is int _;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "3 is int _").WithArguments("pattern matching", "7.0").WithLocation(8, 18),
                // (11,13): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //             case int _:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case int _:").WithArguments("pattern matching", "7.0").WithLocation(11, 13),
                // (14,20): error CS8059: Feature 'out variable declaration' is not available in C# 6. Please use language version 7.0 or greater.
                //         M1(out var _);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "_").WithArguments("out variable declaration", "7.0").WithLocation(14, 20),
                // (15,20): error CS8059: Feature 'out variable declaration' is not available in C# 6. Please use language version 7.0 or greater.
                //         M1(out int _);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "_").WithArguments("out variable declaration", "7.0").WithLocation(15, 20),
                // (16,16): error CS8059: Feature 'discards' is not available in C# 6. Please use language version 7.0 or greater.
                //         M1(out _);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "_").WithArguments("discards", "7.0").WithLocation(16, 16),
                // (24,18): warning CS8512: The name '_' refers to the constant, not the discard pattern. Use 'var _' to discard the value, or '@_' to refer to a constant by that name.
                //             case _: // not a discard
                Diagnostic(ErrorCode.WRN_CaseConstantNamedUnderscore, "_").WithLocation(24, 18)
                );
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compCSharp9 = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            compCSharp9.VerifyDiagnostics(
                // (6,9): error CS8773: Feature 'Mixed declarations and expressions in deconstruction' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         (var x, x) = (1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "(var x, x) = (1, 2)").WithArguments("Mixed declarations and expressions in deconstruction", "10.0").WithLocation(6, 9),
                // (6,17): error CS0841: Cannot use local variable 'x' before it is declared
                //         (var x, x) = (1, 2);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(6, 17),
                // (7,9): error CS8773: Feature 'Mixed declarations and expressions in deconstruction' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         (y, var y) = (1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "(y, var y) = (1, 2)").WithArguments("Mixed declarations and expressions in deconstruction", "10.0").WithLocation(7, 9),
                // (7,10): error CS0841: Cannot use local variable 'y' before it is declared
                //         (y, var y) = (1, 2);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(7, 10)
                );

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (6,17): error CS0841: Cannot use local variable 'x' before it is declared
                //         (var x, x) = (1, 2);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(6, 17),
                // (7,10): error CS0841: Cannot use local variable 'y' before it is declared
                //         (y, var y) = (1, 2);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(7, 10)
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,26): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         (M(out var x).P, x) = (1, x);
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "x").WithLocation(6, 26)
                );
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
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
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
using @alias = System.Int32;
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
        public void SimpleDiscardDeconstructInScript2()
        {
            var source =
@"
public class C
{
    public C() { System.Console.Write(""ctor""); }
    public void Deconstruct(out string x, out string y) { x = y = null; }
}
(string _, string _) = new C();
";


            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ctor");
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
                Assert.Equal("(System.Int32, System.Int32 x3)", model.GetTypeInfo(nestedDeclaration).Type.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(nestedDeclaration).Symbol);

                var tuple = (TupleExpressionSyntax)discard2.Parent.Parent.Parent.Parent;
                Assert.Equal("(var _, var (_, x3))", tuple.ToString());
                Assert.Equal("(System.String, (System.Int32, System.Int32 x3))", model.GetTypeInfo(tuple).Type.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(tuple).Symbol);
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
        foreach ((var _, int _, _, var (_, _), int x) in new[] { (1L, 2, 3, (""hello"", 5), 6) })
        {
            System.Console.Write(x);
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "6");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var discard1 = GetDiscardDesignations(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            Assert.True(model.GetSymbolInfo(discard1).IsEmpty);
            Assert.Null(model.GetTypeInfo(discard1).Type);
            var declaration1 = (DeclarationExpressionSyntax)discard1.Parent;
            Assert.Equal("var _", declaration1.ToString());
            Assert.Equal("System.Int64", model.GetTypeInfo(declaration1).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(declaration1).Symbol);

            var discard2 = GetDiscardDesignations(tree).ElementAt(1);
            Assert.Null(model.GetDeclaredSymbol(discard2));
            Assert.True(model.GetSymbolInfo(discard2).IsEmpty);
            Assert.Null(model.GetTypeInfo(discard2).Type);
            var declaration2 = (DeclarationExpressionSyntax)discard2.Parent;
            Assert.Equal("int _", declaration2.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(declaration2).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(declaration2).Symbol);

            var discard3 = GetDiscardIdentifiers(tree).First();
            Assert.Equal("_", discard3.Parent.ToString());
            Assert.Null(model.GetDeclaredSymbol(discard3));
            Assert.Equal("System.Int32", model.GetTypeInfo(discard3).Type.ToTestDisplayString());
            Assert.Equal("int _", model.GetSymbolInfo(discard3).Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            var discard3Symbol = (IDiscardSymbol)model.GetSymbolInfo(discard3).Symbol;
            Assert.Equal("System.Int32", discard3Symbol.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(discard3).Type.ToTestDisplayString());

            var discard4 = GetDiscardDesignations(tree).ElementAt(2);
            Assert.Null(model.GetDeclaredSymbol(discard4));
            Assert.True(model.GetSymbolInfo(discard4).IsEmpty);
            Assert.Null(model.GetTypeInfo(discard4).Type);

            var nestedDeclaration = (DeclarationExpressionSyntax)discard4.Parent.Parent;
            Assert.Equal("var (_, _)", nestedDeclaration.ToString());
            Assert.Equal("(System.String, System.Int32)", model.GetTypeInfo(nestedDeclaration).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(nestedDeclaration).Symbol);
        }

        [Fact]
        public void UnderscoreInCSharp6Foreach()
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
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "M 1");
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
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,18): error CS8186: A foreach loop must declare its iteration variables.
                //         foreach (_ in M())
                Diagnostic(ErrorCode.ERR_MustDeclareForeachIteration, "_").WithLocation(6, 18)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var discard = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().First();

            var symbol = (DiscardSymbol)model.GetSymbolInfo(discard).Symbol.GetSymbol();
            Assert.True(symbol.TypeWithAnnotations.Type.IsErrorType());
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
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,22): error CS0136: A local or parameter named '_' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         foreach (var _ in new[] { 1 })
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "_").WithArguments("_").WithLocation(7, 22),
                // (6,13): warning CS0168: The variable '_' is declared but never used
                //         int _;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "_").WithArguments("_").WithLocation(6, 13)
                );
        }

        [Fact]
        public void MixedDeconstruction_01()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        var t = (1, 2);
        var x = (int x1, int x2) = t;
        System.Console.WriteLine(x1);
        System.Console.WriteLine(x2);
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (7,18): error CS8185: A declaration is not allowed in this context.
                //         var x = (int x1, int x2) = t;
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x1").WithLocation(7, 18)
            );
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForDeconstructionLocal(model, x1, x1Ref);

            var x2 = GetDeconstructionVariable(tree, "x2");
            var x2Ref = GetReference(tree, "x2");
            VerifyModelForDeconstructionLocal(model, x2, x2Ref);
        }

        [Fact]
        public void MixedDeconstruction_02()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        var t = (1, 2);
        int z;
        (int x1, z) = t;
        System.Console.WriteLine(x1);
    }
}";

            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "1")
                .VerifyIL("Program.Main", @"
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0, //t
                int V_1, //z
                int V_2) //x1
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.2
  IL_0005:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000a:  ldloc.0
  IL_000b:  dup
  IL_000c:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0011:  stloc.2
  IL_0012:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0017:  stloc.1
  IL_0018:  ldloc.2
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  nop
  IL_001f:  ret
}");
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForDeconstructionLocal(model, x1, x1Ref);
        }

        [Fact]
        public void MixedDeconstruction_03()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        var t = (1, 2);
        int z;
        for ((int x1, z) = t; ; )
        {
            System.Console.WriteLine(x1);
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "1")
                .VerifyIL("Program.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0, //t
                int V_1, //z
                int V_2) //x1
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.2
  IL_0005:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000a:  ldloc.0
  IL_000b:  dup
  IL_000c:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0011:  stloc.2
  IL_0012:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0017:  stloc.1
  IL_0018:  br.s       IL_0024
  IL_001a:  nop
  IL_001b:  ldloc.2
  IL_001c:  call       ""void System.Console.WriteLine(int)""
  IL_0021:  nop
  IL_0022:  br.s       IL_0026
  IL_0024:  br.s       IL_001a
  IL_0026:  ret
}");
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForDeconstructionLocal(model, x1, x1Ref);
            var symbolInfo = model.GetSymbolInfo(x1Ref);
            Assert.Equal(symbolInfo.Symbol, model.GetDeclaredSymbol(x1));
            Assert.Equal(SpecialType.System_Int32, symbolInfo.Symbol.GetTypeOrReturnType().SpecialType);

            var lhs = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            Assert.Equal(@"(int x1, z)", lhs.ToString());
            Assert.Equal("(System.Int32 x1, System.Int32 z)", model.GetTypeInfo(lhs).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 x1, System.Int32 z)", model.GetTypeInfo(lhs).ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void MixedDeconstruction_03CSharp9()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        var t = (1, 2);
        int z;
        for ((int x1, z) = t; ; )
        {
            System.Console.WriteLine(x1);
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            compilation.VerifyDiagnostics(
                // (8,14): error CS8773: Feature 'Mixed declarations and expressions in deconstruction' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         for ((int x1, z) = t; ; )
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "(int x1, z) = t").WithArguments("Mixed declarations and expressions in deconstruction", "10.0").WithLocation(8, 14));

            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForDeconstructionLocal(model, x1, x1Ref);
        }

        [Fact]
        public void MixedDeconstruction_04()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        var t = (1, 2);
        for (; ; (int x1, int x2) = t)
        {
            System.Console.WriteLine(x1);
            System.Console.WriteLine(x2);
        }
    }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,19): error CS8185: A declaration is not allowed in this context.
                //         for (; ; (int x1, int x2) = t)
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x1").WithLocation(7, 19),
                // (9,38): error CS0103: The name 'x1' does not exist in the current context
                //             System.Console.WriteLine(x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(9, 38),
                // (10,38): error CS0103: The name 'x2' does not exist in the current context
                //             System.Console.WriteLine(x2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(10, 38)
            );
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForDeconstructionLocal(model, x1);
            var symbolInfo = model.GetSymbolInfo(x1Ref);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);

            var x2 = GetDeconstructionVariable(tree, "x2");
            var x2Ref = GetReference(tree, "x2");
            VerifyModelForDeconstructionLocal(model, x2);
            symbolInfo = model.GetSymbolInfo(x2Ref);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [Fact]
        public void MixedDeconstruction_05()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        foreach ((M(out var x1), args is var x2, _) in new[] { (1, 2, 3) })
        {
            System.Console.WriteLine(x1);
            System.Console.WriteLine(x2);
        }
    }
    static int _M;
    static ref int M(out int x) { x = 2; return ref _M; }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,34): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         foreach ((M(out var x1), args is var x2, _) in new[] { (1, 2, 3) })
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "args is var x2").WithLocation(6, 34),
                // (6,34): error CS0029: Cannot implicitly convert type 'int' to 'bool'
                //         foreach ((M(out var x1), args is var x2, _) in new[] { (1, 2, 3) })
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "args is var x2").WithArguments("int", "bool").WithLocation(6, 34),
                // (6,18): error CS8186: A foreach loop must declare its iteration variables.
                //         foreach ((M(out var x1), args is var x2, _) in new[] { (1, 2, 3) })
                Diagnostic(ErrorCode.ERR_MustDeclareForeachIteration, "(M(out var x1), args is var x2, _)").WithLocation(6, 18)
                );
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            Assert.Equal("int", model.GetTypeInfo(x1Ref).Type.ToDisplayString());

            model = compilation.GetSemanticModel(tree);
            var x2 = GetDeconstructionVariable(tree, "x2");
            var x2Ref = GetReference(tree, "x2");
            Assert.Equal("string[]", model.GetTypeInfo(x2Ref).Type.ToDisplayString());

            VerifyModelForLocal(model, x1, LocalDeclarationKind.OutVariable, x1Ref);
            VerifyModelForLocal(model, x2, LocalDeclarationKind.PatternVariable, x2Ref);
        }

        [Fact]
        public void ForeachIntoExpression()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        foreach (M(out var x1) in new[] { 1, 2, 3 })
        {
            System.Console.WriteLine(x1);
        }
    }
    static int _M;
    static ref int M(out int x) { x = 2; return ref _M; }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,32): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach (M(out var x1) in new[] { 1, 2, 3 })
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(6, 32)
                );
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            Assert.Equal("int", model.GetTypeInfo(x1Ref).Type.ToDisplayString());

            VerifyModelForLocal(model, x1, LocalDeclarationKind.OutVariable, x1Ref);
        }

        [Fact]
        public void MixedDeconstruction_06()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        foreach (M1(M2(out var x1, args is var x2), x1, x2) in new[] {1, 2, 3})
        {
            System.Console.WriteLine(x1);
            System.Console.WriteLine(x2);
        }
    }

    static int _M;
    static ref int M1(int m2, int x, string[] y) { return ref _M; }
    static int M2(out int x, bool b) => x = 2;
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,61): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach (M1(M2(out var x1, args is var x2), x1, x2) in new[] {1, 2, 3})
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(6, 61)
                );
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Ref = GetReferences(tree, "x1");
            Assert.Equal("int", model.GetTypeInfo(x1Ref.First()).Type.ToDisplayString());

            model = compilation.GetSemanticModel(tree);
            var x2 = GetDeconstructionVariable(tree, "x2");
            var x2Ref = GetReferences(tree, "x2");
            Assert.Equal("string[]", model.GetTypeInfo(x2Ref.First()).Type.ToDisplayString());

            VerifyModelForLocal(model, x1, LocalDeclarationKind.OutVariable, x1Ref.ToArray());
            VerifyModelForLocal(model, x2, LocalDeclarationKind.PatternVariable, x2Ref.ToArray());
        }

        [Fact]
        public void MixedDeconstruction_07()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        var t = (1, ("""", true));
        string y;
        for ((int x, (y, var z)) = t; ; )
        {
            System.Console.Write(x);
            System.Console.Write(z);
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation, expectedOutput: "1True")
                .VerifyIL("Program.Main", @"
{
  // Code size       73 (0x49)
  .maxstack  4
  .locals init (System.ValueTuple<int, System.ValueTuple<string, bool>> V_0, //t
                string V_1, //y
                int V_2, //x
                bool V_3, //z
                System.ValueTuple<string, bool> V_4)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.1
  IL_0004:  ldstr      """"
  IL_0009:  ldc.i4.1
  IL_000a:  newobj     ""System.ValueTuple<string, bool>..ctor(string, bool)""
  IL_000f:  call       ""System.ValueTuple<int, System.ValueTuple<string, bool>>..ctor(int, System.ValueTuple<string, bool>)""
  IL_0014:  ldloc.0
  IL_0015:  dup
  IL_0016:  ldfld      ""System.ValueTuple<string, bool> System.ValueTuple<int, System.ValueTuple<string, bool>>.Item2""
  IL_001b:  stloc.s    V_4
  IL_001d:  ldfld      ""int System.ValueTuple<int, System.ValueTuple<string, bool>>.Item1""
  IL_0022:  stloc.2
  IL_0023:  ldloc.s    V_4
  IL_0025:  ldfld      ""string System.ValueTuple<string, bool>.Item1""
  IL_002a:  stloc.1
  IL_002b:  ldloc.s    V_4
  IL_002d:  ldfld      ""bool System.ValueTuple<string, bool>.Item2""
  IL_0032:  stloc.3
  IL_0033:  br.s       IL_0046
  IL_0035:  nop
  IL_0036:  ldloc.2
  IL_0037:  call       ""void System.Console.Write(int)""
  IL_003c:  nop
  IL_003d:  ldloc.3
  IL_003e:  call       ""void System.Console.Write(bool)""
  IL_0043:  nop
  IL_0044:  br.s       IL_0048
  IL_0046:  br.s       IL_0035
  IL_0048:  ret
}");
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var x = GetDeconstructionVariable(tree, "x");
            var xRef = GetReference(tree, "x");
            VerifyModelForDeconstructionLocal(model, x, xRef);
            var xSymbolInfo = model.GetSymbolInfo(xRef);
            Assert.Equal(xSymbolInfo.Symbol, model.GetDeclaredSymbol(x));
            Assert.Equal(SpecialType.System_Int32, xSymbolInfo.Symbol.GetTypeOrReturnType().SpecialType);

            var z = GetDeconstructionVariable(tree, "z");
            var zRef = GetReference(tree, "z");
            VerifyModelForDeconstructionLocal(model, z, zRef);
            var zSymbolInfo = model.GetSymbolInfo(zRef);
            Assert.Equal(zSymbolInfo.Symbol, model.GetDeclaredSymbol(z));
            Assert.Equal(SpecialType.System_Boolean, zSymbolInfo.Symbol.GetTypeOrReturnType().SpecialType);

            var lhs = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(2);
            Assert.Equal(@"(int x, (y, var z))", lhs.ToString());
            Assert.Equal("(System.Int32 x, (System.String y, System.Boolean z))", model.GetTypeInfo(lhs).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 x, (System.String y, System.Boolean z))", model.GetTypeInfo(lhs).ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void IncompleteDeclarationIsSeenAsTupleLiteral()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int x1, string x2);
        System.Console.WriteLine(x1);
        System.Console.WriteLine(x2);
    }
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,10): error CS8185: A declaration is not allowed in this context.
                //         (int x1, string x2);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x1").WithLocation(6, 10),
                // (6,18): error CS8185: A declaration is not allowed in this context.
                //         (int x1, string x2);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "string x2").WithLocation(6, 18),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         (int x1, string x2);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int x1, string x2)").WithLocation(6, 9),
                // (6,10): error CS0165: Use of unassigned local variable 'x1'
                //         (int x1, string x2);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int x1").WithArguments("x1").WithLocation(6, 10),
                // (6,18): error CS0165: Use of unassigned local variable 'x2'
                //         (int x1, string x2);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "string x2").WithArguments("x2").WithLocation(6, 18)
                );

            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var x1 = GetDeconstructionVariable(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            Assert.Equal("int", model.GetTypeInfo(x1Ref).Type.ToDisplayString());

            var x2 = GetDeconstructionVariable(tree, "x2");
            var x2Ref = GetReference(tree, "x2");
            Assert.Equal("string", model.GetTypeInfo(x2Ref).Type.ToDisplayString());

            VerifyModelForDeconstruction(model, x1, LocalDeclarationKind.DeclarationExpressionVariable, x1Ref);
            VerifyModelForDeconstruction(model, x2, LocalDeclarationKind.DeclarationExpressionVariable, x2Ref);
        }

        [Fact]
        [WorkItem(15893, "https://github.com/dotnet/roslyn/issues/15893")]
        public void DeconstructionOfOnlyOneElement()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (p2) = (1, 2);
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,16): error CS1003: Syntax error, ',' expected
                //         var (p2) = (1, 2);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",", ")").WithLocation(6, 16),
                // (6,16): error CS1001: Identifier expected
                //         var (p2) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(6, 16)
                );
        }

        [Fact]
        [WorkItem(14876, "https://github.com/dotnet/roslyn/issues/14876")]
        public void TupleTypeInDeconstruction()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int x, (string, long) y) = M();
        System.Console.Write($""{x} {y}"");
    }

    static (int, (string, long)) M()
    {
        return (5, (""Goo"", 34983490));
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "5 (Goo, 34983490)");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(12468, "https://github.com/dotnet/roslyn/issues/12468")]
        public void RefReturningVarInvocation()
        {
            string source = @"
class C
{
    static int i;

    static void Main()
    {
        int x = 0, y = 0;
        (var(x, y)) = 42; // parsed as invocation
        System.Console.Write(i);
    }
    static ref int var(int a, int b) { return ref i; }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "42", verify: Verification.Passes);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void InvokeVarForLvalueInParens()
        {
            var source = @"
class Program
{
    public static void Main()
    {
        (var(x, y)) = 10;
        System.Console.WriteLine(z);
    }
    static int x = 1, y = 2, z = 3;
    static ref int var(int x, int y)
    {
        return ref z;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();

            // PEVerify fails with ref return https://github.com/dotnet/roslyn/issues/12285
            CompileAndVerify(compilation, expectedOutput: "10", verify: Verification.Fails);
        }

        [Fact]
        [WorkItem(16106, "https://github.com/dotnet/roslyn/issues/16106")]
        public void DefAssignmentsStruct001()
        {
            string source = @"

using System.Collections.Generic;

public class MyClass
{
    public static void Main()
    {
        ((int, int), string)[] arr = new((int, int), string)[1];

        Test5(arr);
    }

    public static void Test4(IEnumerable<(KeyValuePair<int, int>, string)> en)
    {
        foreach ((KeyValuePair<int, int> kv, string s) in en)
        {
            var a = kv.Key; // false error CS0170: Use of possibly unassigned field
        }
    }

    public static void Test5(IEnumerable<((int, int), string)> en)
    {
        foreach (((int, int k) t, string s) in en)
        {
            var a = t.k; // false error CS0170: Use of possibly unassigned field
            System.Console.WriteLine(a);
        }
    }
}";

            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "0");
        }

        [Fact]
        [WorkItem(16106, "https://github.com/dotnet/roslyn/issues/16106")]
        public void DefAssignmentsStruct002()
        {
            string source = @"
public class MyClass
{
    public static void Main()
    {
        var data = new int[10];
        var arr  = new int[2];

        foreach (arr[out int size] in data) {}
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,36): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach (arr[out int size] in data) {}
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(9, 36)
                );
        }

        [Fact]
        [WorkItem(16106, "https://github.com/dotnet/roslyn/issues/16106")]
        public void DefAssignmentsStruct003()
        {
            string source = @"
public class MyClass
{
    public static void Main()
    {
        var data = new (int, int)[10];
        var arr  = new int[2];

        foreach ((arr[out int size], int b) in data) {}
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,27): error CS1615: Argument 1 may not be passed with the 'out' keyword
                //         foreach ((arr[out int size], int b) in data) {}
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "int size").WithArguments("1", "out").WithLocation(9, 27),
                // (9,18): error CS8186: A foreach loop must declare its iteration variables.
                //         foreach ((arr[out int size], int b) in data) {}
                Diagnostic(ErrorCode.ERR_MustDeclareForeachIteration, "(arr[out int size], int b)").WithLocation(9, 18)
                );
        }

        [Fact]
        [WorkItem(16962, "https://github.com/dotnet/roslyn/issues/16962")]
        public void Events_01()
        {
            string source = @"
class C
{
    static event System.Action E;

    static void Main()
    {
        (E, _) = (null, 1);
        System.Console.WriteLine(E == null);
        (E, _) = (Handler, 1);
        E();
    }

    static void Handler()
    {
        System.Console.WriteLine(""Handler"");
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput:
@"True
Handler");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(16962, "https://github.com/dotnet/roslyn/issues/16962")]
        public void Events_02()
        {
            string source = @"
struct S
{
    event System.Action E;

    class C
    {
        static void Main()
        {
            var s = new S();
            (s.E, _) = (null, 1);
            System.Console.WriteLine(s.E == null);
            (s.E, _) = (Handler, 1);
            s.E();
        }

        static void Handler()
        {
            System.Console.WriteLine(""Handler"");
        }
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput:
@"True
Handler");
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.WinRTNeedsWindowsDesktop)]
        [WorkItem(16962, "https://github.com/dotnet/roslyn/issues/16962")]
        public void Events_03()
        {
            string source1 = @"
public interface EventInterface
{
	event System.Action E;
}
";

            var comp1 = CreateEmptyCompilation(source1, WinRtRefs, TestOptions.ReleaseWinMD, TestOptions.Regular);

            string source2 = @"
class C : EventInterface
{
    public event System.Action E;

    static void Main()
    {
        var c = new C();
        c.Test();
    }

    void Test()
    {
        (E, _) = (null, 1);
        System.Console.WriteLine(E == null);
        (E, _) = (Handler, 1);
        E();
    }

    static void Handler()
    {
        System.Console.WriteLine(""Handler"");
    }
}
";

            var comp2 = CompileAndVerify(source2, targetFramework: TargetFramework.Empty, expectedOutput:
@"True
Handler", references: WinRtRefs.Concat(new[] { ValueTupleRef, comp1.ToMetadataReference() }));
            comp2.VerifyDiagnostics();

            Assert.True(comp2.Compilation.GetMember<IEventSymbol>("C.E").IsWindowsRuntimeEvent);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.WinRTNeedsWindowsDesktop)]
        [WorkItem(16962, "https://github.com/dotnet/roslyn/issues/16962")]
        public void Events_04()
        {
            string source1 = @"
public interface EventInterface
{
	event System.Action E;
}
";

            var comp1 = CreateEmptyCompilation(source1, WinRtRefs, TestOptions.ReleaseWinMD, TestOptions.Regular);

            string source2 = @"
struct S : EventInterface
{
    public event System.Action E;

    class C
    {
        S s = new S();

        static void Main()
        {
            var c = new C();
            (GetC(c).s.E, _) = (null, GetInt(1));
            System.Console.WriteLine(c.s.E == null);
            (GetC(c).s.E, _) = (Handler, GetInt(2));
            c.s.E();
        }

        static int GetInt(int i)
        {
            System.Console.WriteLine(i);
            return i;
        }

        static C GetC(C c)
        {
            System.Console.WriteLine(""GetC"");
            return c;
        }

        static void Handler()
        {
            System.Console.WriteLine(""Handler"");
        }
    }
}
";

            var comp2 = CompileAndVerify(source2, targetFramework: TargetFramework.Empty, expectedOutput:
@"GetC
1
True
GetC
2
Handler", references: WinRtRefs.Concat(new[] { ValueTupleRef, comp1.ToMetadataReference() }));
            comp2.VerifyDiagnostics();

            Assert.True(comp2.Compilation.GetMember<IEventSymbol>("S.E").IsWindowsRuntimeEvent);
        }

        [Fact]
        [WorkItem(16962, "https://github.com/dotnet/roslyn/issues/16962")]
        public void Events_05()
        {
            string source = @"
class C
{
    public static event System.Action E;
}

class Program
{
    static void Main()
    {
        (C.E, _) = (null, 1);
    }
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (11,12): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         (C.E, _) = (null, 1);
                Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C").WithLocation(11, 12)
                );
        }

        [Fact]
        [WorkItem(16962, "https://github.com/dotnet/roslyn/issues/16962")]
        public void Events_06()
        {
            string source = @"
class C
{
    static event System.Action E
    {
        add {}
        remove {}
    }

    static void Main()
    {
        (E, _) = (null, 1);
    }
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (12,10): error CS0079: The event 'C.E' can only appear on the left hand side of += or -=
                //         (E, _) = (null, 1);
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "E").WithArguments("C.E").WithLocation(12, 10)
                );
        }

        [Fact]
        public void SimpleAssignInConstructor()
        {
            string source = @"
public class C
{
    public long x;
    public string y;

    public C(int a, string b) => (x, y) = (a, b);

    public static void Main()
    {
        var c = new C(1, ""hello"");
        System.Console.WriteLine(c.x + "" "" + c.y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C..ctor(int, string)", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (long V_0,
                string V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.1
  IL_0007:  conv.i8
  IL_0008:  stloc.0
  IL_0009:  ldarg.2
  IL_000a:  stloc.1
  IL_000b:  ldarg.0
  IL_000c:  ldloc.0
  IL_000d:  stfld      ""long C.x""
  IL_0012:  ldarg.0
  IL_0013:  ldloc.1
  IL_0014:  stfld      ""string C.y""
  IL_0019:  ret
}");
        }

        [Fact]
        public void DeconstructAssignInConstructor()
        {
            string source = @"
public class C
{
    public long x;
    public string y;

    public C(C oldC) => (x, y) = oldC;
    public C() { }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }

    public static void Main()
    {
        var oldC = new C() { x = 1, y = ""hello"" };
        var newC = new C(oldC);
        System.Console.WriteLine(newC.x + "" "" + newC.y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C..ctor(C)", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  .locals init (int V_0,
                string V_1,
                long V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.1
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldloca.s   V_1
  IL_000b:  callvirt   ""void C.Deconstruct(out int, out string)""
  IL_0010:  ldloc.0
  IL_0011:  conv.i8
  IL_0012:  stloc.2
  IL_0013:  ldarg.0
  IL_0014:  ldloc.2
  IL_0015:  stfld      ""long C.x""
  IL_001a:  ldarg.0
  IL_001b:  ldloc.1
  IL_001c:  stfld      ""string C.y""
  IL_0021:  ret
}");
        }

        [Fact]
        public void AssignInConstructorWithProperties()
        {
            string source = @"
public class C
{
    public long X { get; set; }
    public string Y { get; }
    private int z;
    public ref int Z { get { return ref z; } }

    public C(int a, string b, ref int c) => (X, Y, Z) = (a, b, c);

    public static void Main()
    {
        int number = 2;
        var c = new C(1, ""hello"", ref number);
        System.Console.WriteLine($""{c.X} {c.Y} {c.Z}"");
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello 2");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C..ctor(int, string, ref int)", @"
{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (long V_0,
                string V_1,
                int V_2,
                long V_3)
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  call       ""ref int C.Z.get""
  IL_000c:  ldarg.1
  IL_000d:  conv.i8
  IL_000e:  stloc.0
  IL_000f:  ldarg.2
  IL_0010:  stloc.1
  IL_0011:  ldarg.3
  IL_0012:  ldind.i4
  IL_0013:  stloc.2
  IL_0014:  ldarg.0
  IL_0015:  ldloc.0
  IL_0016:  dup
  IL_0017:  stloc.3
  IL_0018:  call       ""void C.X.set""
  IL_001d:  ldarg.0
  IL_001e:  ldloc.1
  IL_001f:  stfld      ""string C.<Y>k__BackingField""
  IL_0024:  ldloc.2
  IL_0025:  stind.i4
  IL_0026:  ret
}");
        }

        [Fact]
        public void VerifyDeconstructionInAsync()
        {
            var source =
@"
using System.Threading.Tasks;
class C
{
    static void Main()
    {
        System.Console.Write(C.M().Result);
    }
    static async Task<int> M()
    {
        await Task.Delay(0);
        var (x, y) = (1, 2);
        return x + y;
    }
}
";

            var comp = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "3");
        }

        [Fact]
        public void DeconstructionWarnsForSelfAssignment()
        {
            var source =
@"
class C
{
    object x = 1;
    static object y = 2;
    void M()
    {
        ((x, x), this.x, C.y) = ((x, (1, 2)), x, y);
    }
}
";

            var comp = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (8,11): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         ((x, x), this.x, C.y) = ((x, (1, 2)), x, y);
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "x").WithLocation(8, 11),
                // (8,18): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         ((x, x), this.x, C.y) = ((x, (1, 2)), x, y);
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "this.x").WithLocation(8, 18),
                // (8,26): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         ((x, x), this.x, C.y) = ((x, (1, 2)), x, y);
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "C.y").WithLocation(8, 26)
                );
        }

        [Fact]
        public void DeconstructionWarnsForSelfAssignment2()
        {
            var source =
@"
class C
{
    object x = 1;
    static object y = 2;
    void M()
    {
        object z = 3;
        (x, (y, z)) = (x, (y, z));
    }
}
";

            var comp = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (9,10): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         (x, (y, z)) = (x, (y, z));
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "x"),
                // (9,14): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         (x, (y, z)) = (x, (y, z));
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "y").WithLocation(9, 14),
                // (9,17): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         (x, (y, z)) = (x, (y, z));
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "z").WithLocation(9, 17)
                );
        }

        [Fact]
        public void DeconstructionWarnsForSelfAssignment_WithUserDefinedConversionOnElement()
        {
            var source =
@"
class C
{
    object x = 1;
    static C y = null;
    void M()
    {
        (x, y) = (x, (C)(D)y);
    }
    public static implicit operator C(D d) => null;
}
class D
{
    public static implicit operator D(C c) => null;
}
";

            var comp = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (8,10): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         (x, y) = (x, (C)(D)y);
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "x").WithLocation(8, 10)
                );
        }

        [Fact]
        public void DeconstructionWarnsForSelfAssignment_WithNestedConversions()
        {
            var source =
@"
class C
{
    object x = 1;
    int y = 2;
    byte b = 3;
    void M()
    {
        // The conversions on the right-hand-side:
        // - a deconstruction conversion
        // - an implicit tuple literal conversion on the entire right-hand-side
        // - another implicit tuple literal conversion on the nested tuple
        // - a conversion on element `b`
        (_, (x, y)) = (1, (x, b));
    }
}
";

            var comp = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (14,14): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         (_, (x, y)) = (1, (x, b));
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "x").WithLocation(14, 14)
                );
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructionWarnsForSelfAssignment_WithExplicitTupleConversion()
        {
            var source =
@"
class C
{
    int y = 2;
    byte b = 3;
    void M()
    {
        // The conversions on the right-hand-side:
        // - a deconstruction conversion on the entire right-hand-side
        // - an identity conversion as its operand
        // - an explicit tuple literal conversion as its operand
        (y, _) = ((int, int))(y, b);
    }
}
";

            var comp = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                );

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().Single();

            Assert.Equal("((int, int))(y, b)", node.ToString());

            comp.VerifyOperationTree(node, expectedOperationTree:
@"
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, System.Int32)) (Syntax: '((int, int))(y, b)')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 y, System.Int32 b)) (Syntax: '(y, b)')
      NaturalType: (System.Int32 y, System.Byte b)
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'y')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IFieldReferenceOperation: System.Int32 C.y (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'y')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'y')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'b')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IFieldReferenceOperation: System.Byte C.b (OperationKind.FieldReference, Type: System.Byte) (Syntax: 'b')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'b')
");
        }

        [Fact]
        public void DeconstructionWarnsForSelfAssignment_WithDeconstruct()
        {
            var source =
@"
class C
{
    object x = 1;
    static object y = 2;
    void M()
    {
        object z = 3;
        (x, (y, z)) = (x, y);
    }
}
static class Extensions
{
    public static void Deconstruct(this object input, out object output1, out object output2)
    {
        output1 = input;
        output2 = input;
    }
}";

            var comp = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (9,10): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         (x, (y, z)) = (x, y);
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "x").WithLocation(9, 10)
                );
        }

        [Fact]
        public void TestDeconstructOnErrorType()
        {
            var source =
@"
class C
{
    Error M()
    {
        int x, y;
        (x, y) = M();
        throw null;
    }
}";

            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll); // no ValueTuple reference
            comp.VerifyDiagnostics(
                // (4,5): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                //     Error M()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(4, 5)
                );
        }

        [Fact]
        public void TestDeconstructOnErrorTypeFromImageReference()
        {
            var missing_cs = "public class Missing { }";
            var missing = CreateCompilationWithMscorlib45(missing_cs, options: TestOptions.DebugDll, assemblyName: "missing");

            var lib_cs = "public class C { public Missing M() { throw null; } }";
            var lib = CreateCompilationWithMscorlib45(lib_cs, references: new[] { missing.EmitToImageReference() }, options: TestOptions.DebugDll);

            var source =
@"
class D
{
    void M()
    {
        int x, y;
        (x, y) = new C().M();
        throw null;
    }
}";

            var comp = CreateCompilationWithMscorlib45(source, references: new[] { lib.EmitToImageReference() }, options: TestOptions.DebugDll); // no ValueTuple reference
            comp.VerifyDiagnostics(
                // (7,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         (x, y) = new C().M();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new C().M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18)
                );
        }

        [Fact]
        public void TestDeconstructOnErrorTypeFromCompilationReference()
        {
            var missing_cs = "public class Missing { }";
            var missing = CreateCompilationWithMscorlib45(missing_cs, options: TestOptions.DebugDll, assemblyName: "missing");

            var lib_cs = "public class C { public Missing M() { throw null; } }";
            var lib = CreateCompilationWithMscorlib45(lib_cs, references: new[] { missing.ToMetadataReference() }, options: TestOptions.DebugDll);

            var source =
@"
class D
{
    void M()
    {
        int x, y;
        (x, y) = new C().M();
        throw null;
    }
}";

            var comp = CreateCompilationWithMscorlib45(source, references: new[] { lib.ToMetadataReference() }, options: TestOptions.DebugDll); // no ValueTuple reference
            comp.VerifyDiagnostics(
                // (7,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         (x, y) = new C().M();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new C().M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18)
                );
        }

        [Fact, WorkItem(17756, "https://github.com/dotnet/roslyn/issues/17756")]
        public void TestDiscardedAssignmentNotLvalue()
        {
            var source = @"
class Program
{
    struct S1
    {
        public int field;
        public int Increment() => field++;
    }

    static void Main()
    {
        S1 v = default(S1);
        v.Increment(); 

        (_ = v).Increment();

        System.Console.WriteLine(v.field);
    }
}
";
            string expectedOutput = @"1";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(19398, "https://github.com/dotnet/roslyn/issues/19398")]
        public void TupleCastInDeconstruction()
        {
            var source = @"
class C
{
    static void Main()
    {
        var t = (1, 2);
        var (a, b) = ((byte, byte))t;
        System.Console.Write($""{a} {b}"");
    }
}";
            CompileAndVerify(source, expectedOutput: @"1 2");
        }

        [Fact, WorkItem(19398, "https://github.com/dotnet/roslyn/issues/19398")]
        public void TupleCastInDeconstruction2()
        {
            var source = @"
class C
{
    static void Main()
    {
        var t = (new C(), new D());
        var (a, _) = ((byte, byte))t;
        System.Console.Write($""{a}"");
    }
    public static explicit operator byte(C c) { System.Console.Write(""Convert ""); return 1; }
}
class D
{
    public static explicit operator byte(D c) { System.Console.Write(""Convert2 ""); return 2; }
}";
            CompileAndVerify(source, expectedOutput: @"Convert Convert2 1");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19398, "https://github.com/dotnet/roslyn/issues/19398")]
        public void TupleCastInDeconstruction3()
        {
            var source = @"
class C
{
    static int A { set { System.Console.Write(""A ""); } }
    static int B { set { System.Console.Write(""B""); } }
    static void Main()
    {
        (A, B) = ((byte, byte))(new C(), new D());
    }
    public static explicit operator byte(C c) { System.Console.Write(""Convert ""); return 1; }
    public C() { System.Console.Write(""C ""); }
}
class D
{
    public static explicit operator byte(D c) { System.Console.Write(""Convert2 ""); return 2; }
    public D() { System.Console.Write(""D ""); }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"C Convert D Convert2 A B").Compilation;
            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().Single();

            Assert.Equal("((byte, byte))(new C(), new D())", node.ToString());

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Byte, System.Byte)) (Syntax: '((byte, byt ... ), new D())')
  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Byte, System.Byte)) (Syntax: '(new C(), new D())')
      NaturalType: (C, D)
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Byte C.op_Explicit(C c)) (OperationKind.Conversion, Type: System.Byte, IsImplicit) (Syntax: 'new C()')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Byte C.op_Explicit(C c))
            Operand: 
              IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                Arguments(0)
                Initializer: 
                  null
          IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Byte D.op_Explicit(D c)) (OperationKind.Conversion, Type: System.Byte, IsImplicit) (Syntax: 'new D()')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Byte D.op_Explicit(D c))
            Operand: 
              IObjectCreationOperation (Constructor: D..ctor()) (OperationKind.ObjectCreation, Type: D) (Syntax: 'new D()')
                Arguments(0)
                Initializer: 
                  null
");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19398, "https://github.com/dotnet/roslyn/issues/19398")]
        public void TupleCastInDeconstruction4()
        {
            var source = @"
class C
{
    static void Main()
    {
        var (a, _) = ((short, short))((int, int))(1L, 2L);
        System.Console.Write($""{a}"");
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1").Compilation;
            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().ElementAt(1);

            Assert.Equal("((int, int))(1L, 2L)", node.ToString());

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, System.Int32)) (Syntax: '((int, int))(1L, 2L)')
  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1L, 2L)')
      NaturalType: (System.Int64, System.Int64)
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1L')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 1) (Syntax: '1L')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2L')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 2) (Syntax: '2L')
");

            Assert.Equal("((short, short))((int, int))(1L, 2L)", node.Parent.ToString());

            compilation.VerifyOperationTree(node.Parent, expectedOperationTree:
@"
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int16, System.Int16)) (Syntax: '((short, sh ... t))(1L, 2L)')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, System.Int32)) (Syntax: '((int, int))(1L, 2L)')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1L, 2L)')
          NaturalType: (System.Int64, System.Int64)
          Elements(2):
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1L')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 1) (Syntax: '1L')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2L')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 2) (Syntax: '2L')
");
        }

        [Fact, WorkItem(19398, "https://github.com/dotnet/roslyn/issues/19398")]
        public void UserDefinedCastInDeconstruction()
        {
            var source = @"
class C
{
    static void Main()
    {
        var c = new C();
        var (a, b) = ((byte, byte))c;
        System.Console.Write($""{a} {b}"");
    }
    public static explicit operator (byte, byte)(C c)
    {
        return (3, 4);
    }
}";
            CompileAndVerify(source, expectedOutput: @"3 4");
        }

        [Fact, WorkItem(19398, "https://github.com/dotnet/roslyn/issues/19398")]
        public void DeconstructionLoweredToNothing()
        {
            var source = @"
class C
{
    static void M()
    {
        for (var(_, _) = (1, 2); ; (_, _) = (3, 4))
        {
        }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size        2 (0x2)
  .maxstack  0
  IL_0000:  br.s       IL_0000
}");
        }

        [Fact, WorkItem(19398, "https://github.com/dotnet/roslyn/issues/19398")]
        public void DeconstructionLoweredToNothing2()
        {
            var source = @"
class C
{
    static void M()
    {
        (_, _) = (1, 2);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("C.M", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [Fact, WorkItem(19398, "https://github.com/dotnet/roslyn/issues/19398")]
        public void DeconstructionLoweredToNothing3()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var(_, _) in new[] { (1, 2) })
        {
            System.Console.Write(""once"");
        }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "once");
        }

        [WorkItem(21028, "https://github.com/dotnet/roslyn/issues/21028")]
        [Fact]
        public void InferredName()
        {
            var source =
@"class C
{
    static void Main()
    {
        int x = 0, y = 1;
        var t = (x, y);
        var (a, b) = t;
    }
}";
            // C# 7.0
            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            comp.VerifyEmitDiagnostics();
            // C# 7.1
            comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            comp.VerifyEmitDiagnostics();
        }

        [WorkItem(21028, "https://github.com/dotnet/roslyn/issues/21028")]
        [Fact]
        public void InferredName_ConditionalOperator()
        {
            var source =
@"class C
{
    static void M(int a, int b, bool c)
    {
        (var x, var y) = c ? (a, default(object)) : (b, null);
        (x, y) = c ? (a, default(string)) : (b, default(object));
    }
}";
            // C# 7.0
            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            comp.VerifyEmitDiagnostics();
            // C# 7.1
            comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            comp.VerifyEmitDiagnostics();
        }

        [WorkItem(21028, "https://github.com/dotnet/roslyn/issues/21028")]
        [Fact]
        public void InferredName_ImplicitArray()
        {
            var source =
@"class C
{
    static void M(int x)
    {
        int y;
        object z;
        (y, z) = (new [] { (x, default(object)), (2, 3) })[0];
    }
}";
            // C# 7.0
            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            comp.VerifyEmitDiagnostics();
            // C# 7.1
            comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            comp.VerifyEmitDiagnostics();
        }

        [WorkItem(21028, "https://github.com/dotnet/roslyn/issues/21028")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/32006")]
        public void InferredName_Lambda()
        {
            // See https://github.com/dotnet/roslyn/issues/32006
            // need to relax assertion in GetImplicitTupleLiteralConversion

            var source =
@"class C
{
    static T F<T>(System.Func<object, bool, T> f)
    {
        return f(null, false);
    }
    static void M()
    {
        var (x, y) = F((a, b) =>
        {
            if (b) return (default(object), a);
            return (null, null);
        });
    }
}";
            // C# 7.0
            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            comp.VerifyEmitDiagnostics();
            // C# 7.1
            comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            comp.VerifyEmitDiagnostics();
        }

        [WorkItem(21028, "https://github.com/dotnet/roslyn/issues/21028")]
        [Fact]
        public void InferredName_ConditionalOperator_LongTuple()
        {
            var source =
@"class C
{
    static void M(object a, object b, bool c)
    {
        var (_1, _2, _3, _4, _5, _6, _7, _8, _9, _10) = c ?
            (1, 2, 3, 4, 5, 6, 7, a, b, 10) :
            (1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
    }
}";
            // C# 7.0
            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            comp.VerifyEmitDiagnostics();
            // C# 7.1
            comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            comp.VerifyEmitDiagnostics();
        }

        [WorkItem(21028, "https://github.com/dotnet/roslyn/issues/21028")]
        [Fact]
        public void InferredName_ConditionalOperator_UseSite()
        {
            var source =
@"class C
{
    static void M(int a, int b, bool c)
    {
        var (x, y) = c ? ((object)1, a) : (b, 2);
    }
}
namespace System
{
    struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        private T2 Item2;
        public ValueTuple(T1 item1, T2 item2) => throw null;
    }
}";
            var expected = new[] {
                // (12,19): warning CS0649: Field '(T1, T2).Item1' is never assigned to, and will always have its default value 
                //         public T1 Item1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Item1").WithArguments("(T1, T2).Item1", "").WithLocation(12, 19),
                // (13,20): warning CS0169: The field '(T1, T2).Item2' is never used
                //         private T2 Item2;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Item2").WithArguments("(T1, T2).Item2").WithLocation(13, 20)
            };

            // C# 7.0
            var comp = CreateCompilation(
                source,
                assemblyName: "39f5d0e8-2935-4207-a74d-517a8e55af08",
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            comp.VerifyEmitDiagnostics(expected);

            // C# 7.1
            comp = CreateCompilation(
                source,
                assemblyName: "39f5d0e8-2935-4207-a74d-517a8e55af08",
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void InferredName_ConditionalOperator_UseSite_AccessingWithinConstructor()
        {
            var source =
@"class C
{
    static void M(int a, int b, bool c)
    {
        var (x, y) = c ? ((object)1, a) : (b, 2);
    }
}
namespace System
{
    struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        private T2 Item2;
        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }
}";
            var expected = new[]
            {
                    // (13,20): warning CS0169: The field '(T1, T2).Item2' is never used
                    //         private T2 Item2;
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "Item2").WithArguments("(T1, T2).Item2").WithLocation(13, 20),
                    // (14,16): error CS0171: Field '(T1, T2).Item2' must be fully assigned before control is returned to the caller
                    //         public ValueTuple(T1 item1, T2 item2)
                    Diagnostic(ErrorCode.ERR_UnassignedThis, "ValueTuple").WithArguments("(T1, T2).Item2").WithLocation(14, 16),
                    // (14,16): error CS0171: Field '(T1, T2).Item2' must be fully assigned before control is returned to the caller
                    //         public ValueTuple(T1 item1, T2 item2)
                    Diagnostic(ErrorCode.ERR_UnassignedThis, "ValueTuple").WithArguments("(T1, T2).Item2").WithLocation(14, 16),
                    // (17,13): error CS0229: Ambiguity between '(T1, T2).Item2' and '(T1, T2).Item2'
                    //             Item2 = item2;
                    Diagnostic(ErrorCode.ERR_AmbigMember, "Item2").WithArguments("(T1, T2).Item2", "(T1, T2).Item2").WithLocation(17, 13)
            };

            // C# 7.0
            var comp = CreateCompilation(
                source,
                assemblyName: "39f5d0e8-2935-4207-a74d-517a8e55af08",
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            comp.VerifyEmitDiagnostics(expected);
            // C# 7.1
            comp = CreateCompilation(
                source,
                assemblyName: "39f5d0e8-2935-4207-a74d-517a8e55af08",
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void TestGetDeconstructionInfoOnIncompleteCode()
        {
            string source = @"
class C
{
    void M() { var (y1, y2) =}
    void Deconstruct(out int x1, out int x2) { x1 = 1; x2 = 2; }
}
";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First();
            Assert.Equal("var (y1, y2) =", node.ToString());
            var info = model.GetDeconstructionInfo(node);
            Assert.Null(info.Method);
            Assert.Empty(info.Nested);
        }

        [Fact]
        public void TestDeconstructStructThis()
        {
            string source = @"
public struct S
{
    int I;
    public static void Main()
    {
        S s = new S();
        s.M();
    }
    public void M()
    {
        this.I = 42;
        var (x, (y, z)) = (this, this /* mutating deconstruction */);
        System.Console.Write($""{x.I} {y} {z}"");
    }
    void Deconstruct(out int x1, out int x2) { x1 = I++; x2 = I++; }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "42 42 43");
        }

        [Fact]
        public void TestDeconstructClassThis()
        {
            string source = @"
public class C
{
    int I;
    public static void Main()
    {
        C c = new C();
        c.M();
    }
    public void M()
    {
        this.I = 42;
        var (x, (y, z)) = (this, this /* mutating deconstruction */);
        System.Console.Write($""{x.I} {y} {z}"");
    }
    void Deconstruct(out int x1, out int x2) { x1 = I++; x2 = I++; }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "44 42 43");
        }

        [Fact]
        public void AssigningConditional_OutParams()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        Test(true, false);
        Test(false, true);
        Test(false, false);
    }

    static void Test(bool b1, bool b2)
    {
        M(out int x, out int y, b1, b2);
        Console.Write(x);
        Console.Write(y);
    }

    static void M(out int x, out int y, bool b1, bool b2)
    {
        (x, y) = b1 ? (10, 20) : b2 ? (30, 40) : (50, 60);
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: "102030405060");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.M", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldarg.2
  IL_0001:  brtrue.s   IL_0018
  IL_0003:  ldarg.3
  IL_0004:  brtrue.s   IL_000f
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.s   50
  IL_0009:  stind.i4
  IL_000a:  ldarg.1
  IL_000b:  ldc.i4.s   60
  IL_000d:  stind.i4
  IL_000e:  ret
  IL_000f:  ldarg.0
  IL_0010:  ldc.i4.s   30
  IL_0012:  stind.i4
  IL_0013:  ldarg.1
  IL_0014:  ldc.i4.s   40
  IL_0016:  stind.i4
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.s   10
  IL_001b:  stind.i4
  IL_001c:  ldarg.1
  IL_001d:  ldc.i4.s   20
  IL_001f:  stind.i4
  IL_0020:  ret
}");
        }

        [Fact]
        public void AssigningConditional_VarDeconstruction()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        M(true, false);
        M(false, true);
        M(false, false);
    }

    static void M(bool b1, bool b2)
    {
        var (x, y) = b1 ? (10, 20) : b2 ? (30, 40) : (50, 60);
        Console.Write(x);
        Console.Write(y);
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: "102030405060");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.M", @"
{
  // Code size       41 (0x29)
  .maxstack  1
  .locals init (int V_0, //x
                int V_1) //y
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0016
  IL_0003:  ldarg.1
  IL_0004:  brtrue.s   IL_000e
  IL_0006:  ldc.i4.s   50
  IL_0008:  stloc.0
  IL_0009:  ldc.i4.s   60
  IL_000b:  stloc.1
  IL_000c:  br.s       IL_001c
  IL_000e:  ldc.i4.s   30
  IL_0010:  stloc.0
  IL_0011:  ldc.i4.s   40
  IL_0013:  stloc.1
  IL_0014:  br.s       IL_001c
  IL_0016:  ldc.i4.s   10
  IL_0018:  stloc.0
  IL_0019:  ldc.i4.s   20
  IL_001b:  stloc.1
  IL_001c:  ldloc.0
  IL_001d:  call       ""void System.Console.Write(int)""
  IL_0022:  ldloc.1
  IL_0023:  call       ""void System.Console.Write(int)""
  IL_0028:  ret
}");
        }

        [Fact]
        public void AssigningConditional_MixedDeconstruction()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        M(true, false);
        M(false, true);
        M(false, false);
    }

    static void M(bool b1, bool b2)
    {
        (var x, long y) = b1 ? (10, 20) : b2 ? (30, 40) : (50, 60);
        Console.Write(x);
        Console.Write(y);
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: "102030405060");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.M", @"
{
  // Code size       50 (0x32)
  .maxstack  1
  .locals init (int V_0, //x
                long V_1, //y
                long V_2)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_001c
  IL_0003:  ldarg.1
  IL_0004:  brtrue.s   IL_0011
  IL_0006:  ldc.i4.s   60
  IL_0008:  conv.i8
  IL_0009:  stloc.2
  IL_000a:  ldc.i4.s   50
  IL_000c:  stloc.0
  IL_000d:  ldloc.2
  IL_000e:  stloc.1
  IL_000f:  br.s       IL_0025
  IL_0011:  ldc.i4.s   40
  IL_0013:  conv.i8
  IL_0014:  stloc.2
  IL_0015:  ldc.i4.s   30
  IL_0017:  stloc.0
  IL_0018:  ldloc.2
  IL_0019:  stloc.1
  IL_001a:  br.s       IL_0025
  IL_001c:  ldc.i4.s   20
  IL_001e:  conv.i8
  IL_001f:  stloc.2
  IL_0020:  ldc.i4.s   10
  IL_0022:  stloc.0
  IL_0023:  ldloc.2
  IL_0024:  stloc.1
  IL_0025:  ldloc.0
  IL_0026:  call       ""void System.Console.Write(int)""
  IL_002b:  ldloc.1
  IL_002c:  call       ""void System.Console.Write(long)""
  IL_0031:  ret
}");
        }

        [Fact]
        public void AssigningConditional_SideEffects()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        M(true, false);
        M(false, true);
        M(false, false);
        SideEffect(true);
        SideEffect(false);
    }

    static int left;
    static int right;

    static ref int SideEffect(bool isLeft)
    {
        Console.WriteLine($""{(isLeft ? ""left"" : ""right"")}: {(isLeft ? left : right)}"");
        return ref isLeft ? ref left : ref right;
    }

    static void M(bool b1, bool b2)
    {
        (SideEffect(isLeft: true), SideEffect(isLeft: false)) = b1 ? (10, 20) : b2 ? (30, 40) : (50, 60);
    }
}
";

            var expected =
@"left: 0
right: 0
left: 10
right: 20
left: 30
right: 40
left: 50
right: 60";
            var comp = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expected);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.M", @"
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (int& V_0,
                int& V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""ref int C.SideEffect(bool)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  call       ""ref int C.SideEffect(bool)""
  IL_000d:  stloc.1
  IL_000e:  ldarg.0
  IL_000f:  brtrue.s   IL_0026
  IL_0011:  ldarg.1
  IL_0012:  brtrue.s   IL_001d
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.s   50
  IL_0017:  stind.i4
  IL_0018:  ldloc.1
  IL_0019:  ldc.i4.s   60
  IL_001b:  stind.i4
  IL_001c:  ret
  IL_001d:  ldloc.0
  IL_001e:  ldc.i4.s   30
  IL_0020:  stind.i4
  IL_0021:  ldloc.1
  IL_0022:  ldc.i4.s   40
  IL_0024:  stind.i4
  IL_0025:  ret
  IL_0026:  ldloc.0
  IL_0027:  ldc.i4.s   10
  IL_0029:  stind.i4
  IL_002a:  ldloc.1
  IL_002b:  ldc.i4.s   20
  IL_002d:  stind.i4
  IL_002e:  ret
}");
        }

        [Fact]
        public void AssigningConditional_SideEffects_RHS()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        M(true, false);
        M(false, true);
        M(false, false);
    }

    static T Echo<T>(T v, int i)
    {
        Console.WriteLine(i + "": "" + v);
        return v;
    }

    static void M(bool b1, bool b2)
    {
        var (x, y) = Echo(b1, 1) ? Echo((10, 20), 2) : Echo(b2, 3) ? Echo((30, 40), 4) : Echo((50, 60), 5);
        Console.WriteLine(""x: "" + x);
        Console.WriteLine(""y: "" + y);
        Console.WriteLine();
    }
}
";

            var expectedOutput =
@"1: True
2: (10, 20)
x: 10
y: 20

1: False
3: True
4: (30, 40)
x: 30
y: 40

1: False
3: False
5: (50, 60)
x: 50
y: 60
";
            var comp = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningConditional_UnusedDeconstruction()
        {
            string source = @"
class C
{
    static void M(bool b1, bool b2)
    {
        (_, _) = b1 ? (10, 20) : b2 ? (30, 40) : (50, 60);
    }
}
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.M", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldarg.1
  IL_0004:  pop
  IL_0005:  ret
}");
        }

        [Fact, WorkItem(46562, "https://github.com/dotnet/roslyn/issues/46562")]
        public void CompoundAssignment()
        {
            string source = @"
class C
{
    void M()
    {
        decimal x = 0;
        (var y, _) += 0.00m;
        (int z, _) += z;
        (var t, _) += (1, 2);
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,17): warning CS0219: The variable 'x' is assigned but its value is never used
                //         decimal x = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(6, 17),
                // (7,10): error CS8185: A declaration is not allowed in this context.
                //         (var y, _) += 0.00m;
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var y").WithLocation(7, 10),
                // (7,17): error CS0103: The name '_' does not exist in the current context
                //         (var y, _) += 0.00m;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(7, 17),
                // (8,10): error CS8185: A declaration is not allowed in this context.
                //         (int z, _) += z;
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int z").WithLocation(8, 10),
                // (8,10): error CS0165: Use of unassigned local variable 'z'
                //         (int z, _) += z;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int z").WithArguments("z").WithLocation(8, 10),
                // (8,17): error CS0103: The name '_' does not exist in the current context
                //         (int z, _) += z;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(8, 17),
                // (9,10): error CS8185: A declaration is not allowed in this context.
                //         (var t, _) += (1, 2);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var t").WithLocation(9, 10),
                // (9,17): error CS0103: The name '_' does not exist in the current context
                //         (var t, _) += (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(9, 17)
                );
        }

        [Fact, WorkItem(50654, "https://github.com/dotnet/roslyn/issues/50654")]
        public void Repro50654()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int, (int, (int, int), (int, int)))[] vals = new[]
        {
            (1, (2, (3, 4), (5, 6))),
            (11, (12, (13, 14), (15, 16)))
        };

        foreach (var (a, (b, (c, d), (e, f))) in vals)
        {
            System.Console.Write($""{a + b + c + d + e + f} "");
        }

        foreach ((int a, (int b, (int c, int d), (int e, int f))) in vals)
        {
            System.Console.Write($""{a + b + c + d + e + f} "");
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "21 81 21 81");
        }

        [Fact]
        public void MixDeclarationAndAssignmentPermutationsOf2()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1 = 0;
        (x1, string y1) = new C();
        System.Console.WriteLine(x1 + "" "" + y1);
        int x2;
        (x2, var y2) = new C();
        System.Console.WriteLine(x2 + "" "" + y2);
        string y3 = """";
        (int x3, y3) = new C();
        System.Console.WriteLine(x3 + "" "" + y3);
        string y4;
        (var x4, y4) = new C();
        System.Console.WriteLine(x4 + "" "" + y4);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: @"1 hello
1 hello
1 hello
1 hello");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size      188 (0xbc)
  .maxstack  3
  .locals init (int V_0, //x1
                string V_1, //y1
                int V_2, //x2
                string V_3, //y2
                string V_4, //y3
                int V_5, //x3
                string V_6, //y4
                int V_7, //x4
                int V_8,
                string V_9)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  newobj     ""C..ctor()""
  IL_0007:  ldloca.s   V_8
  IL_0009:  ldloca.s   V_9
  IL_000b:  callvirt   ""void C.Deconstruct(out int, out string)""
  IL_0010:  ldloc.s    V_8
  IL_0012:  stloc.0
  IL_0013:  ldloc.s    V_9
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""string int.ToString()""
  IL_001d:  ldstr      "" ""
  IL_0022:  ldloc.1
  IL_0023:  call       ""string string.Concat(string, string, string)""
  IL_0028:  call       ""void System.Console.WriteLine(string)""
  IL_002d:  newobj     ""C..ctor()""
  IL_0032:  ldloca.s   V_8
  IL_0034:  ldloca.s   V_9
  IL_0036:  callvirt   ""void C.Deconstruct(out int, out string)""
  IL_003b:  ldloc.s    V_8
  IL_003d:  stloc.2
  IL_003e:  ldloc.s    V_9
  IL_0040:  stloc.3
  IL_0041:  ldloca.s   V_2
  IL_0043:  call       ""string int.ToString()""
  IL_0048:  ldstr      "" ""
  IL_004d:  ldloc.3
  IL_004e:  call       ""string string.Concat(string, string, string)""
  IL_0053:  call       ""void System.Console.WriteLine(string)""
  IL_0058:  ldstr      """"
  IL_005d:  stloc.s    V_4
  IL_005f:  newobj     ""C..ctor()""
  IL_0064:  ldloca.s   V_8
  IL_0066:  ldloca.s   V_9
  IL_0068:  callvirt   ""void C.Deconstruct(out int, out string)""
  IL_006d:  ldloc.s    V_8
  IL_006f:  stloc.s    V_5
  IL_0071:  ldloc.s    V_9
  IL_0073:  stloc.s    V_4
  IL_0075:  ldloca.s   V_5
  IL_0077:  call       ""string int.ToString()""
  IL_007c:  ldstr      "" ""
  IL_0081:  ldloc.s    V_4
  IL_0083:  call       ""string string.Concat(string, string, string)""
  IL_0088:  call       ""void System.Console.WriteLine(string)""
  IL_008d:  newobj     ""C..ctor()""
  IL_0092:  ldloca.s   V_8
  IL_0094:  ldloca.s   V_9
  IL_0096:  callvirt   ""void C.Deconstruct(out int, out string)""
  IL_009b:  ldloc.s    V_8
  IL_009d:  stloc.s    V_7
  IL_009f:  ldloc.s    V_9
  IL_00a1:  stloc.s    V_6
  IL_00a3:  ldloca.s   V_7
  IL_00a5:  call       ""string int.ToString()""
  IL_00aa:  ldstr      "" ""
  IL_00af:  ldloc.s    V_6
  IL_00b1:  call       ""string string.Concat(string, string, string)""
  IL_00b6:  call       ""void System.Console.WriteLine(string)""
  IL_00bb:  ret
}");
        }

        [Fact]
        public void MixDeclarationAndAssignmentPermutationsOf3()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1;
        string y1;
        (x1, y1, var z1) = new C();
        System.Console.WriteLine(x1 + "" "" + y1 + "" "" + z1);

        int x2;
        bool z2;
        (x2, var y2, z2) = new C();
        System.Console.WriteLine(x2 + "" "" + y2 + "" "" + z2);

        string y3;
        bool z3;
        (var x3, y3, z3) = new C();
        System.Console.WriteLine(x3 + "" "" + y3 + "" "" + z3);

        bool z4;
        (var x4, var y4, z4) = new C();
        System.Console.WriteLine(x4 + "" "" + y4 + "" "" + z4);

        string y5;
        (var x5, y5, var z5) = new C();
        System.Console.WriteLine(x5 + "" "" + y5 + "" "" + z5);

        int x6;
        (x6, var y6, var z6) = new C();
        System.Console.WriteLine(x6 + "" "" + y6 + "" "" + z6);
    }

    public void Deconstruct(out int a, out string b, out bool c)
    {
        a = 1;
        b = ""hello"";
        c = true;
    }
}
";

            var comp = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: @"1 hello True
1 hello True
1 hello True
1 hello True
1 hello True
1 hello True");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DontAllowMixedDeclarationAndAssignmentInExpressionContext()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1 = 0;
        var z1 = (x1, string y1) = new C();
        string y2 = """";
        var z2 = (int x2, y2) = new C();
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,23): error CS8185: A declaration is not allowed in this context.
                //         var z1 = (x1, string y1) = new C();
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "string y1").WithLocation(7, 23),
                // (9,19): error CS8185: A declaration is not allowed in this context.
                //         var z2 = (int x2, y2) = new C();
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x2").WithLocation(9, 19));
        }

        [Fact]
        public void DontAllowMixedDeclarationAndAssignmentInForeachDeclarationVariable()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1;
        foreach((x1, string y1) in new C[0]);
        string y2;
        foreach((int x2, y2) in new C[0]);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                    // (6,13): warning CS0168: The variable 'x1' is declared but never used
                    //         int x1;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x1").WithArguments("x1").WithLocation(6, 13),
                    // (7,17): error CS8186: A foreach loop must declare its iteration variables.
                    //         foreach((x1, string y1) in new C[0]);
                    Diagnostic(ErrorCode.ERR_MustDeclareForeachIteration, "(x1, string y1)").WithLocation(7, 17),
                    // (8,16): warning CS0168: The variable 'y2' is declared but never used
                    //         string y2;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "y2").WithArguments("y2").WithLocation(8, 16),
                    // (9,17): error CS8186: A foreach loop must declare its iteration variables.
                    //         foreach((int x2, y2) in new C[0]);
                    Diagnostic(ErrorCode.ERR_MustDeclareForeachIteration, "(int x2, y2)").WithLocation(9, 17));
        }

        [Fact]
        public void DuplicateDeclarationOfVariableDeclaredInMixedDeclarationAndAssignment()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1;
        string y1;
        (x1, string y1) = new C();
        string y2;
        (int x2, y2) = new C();
        int x2;
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (7,16): warning CS0168: The variable 'y1' is declared but never used
                //         string y1;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "y1").WithArguments("y1").WithLocation(7, 16),
                // (8,21): error CS0128: A local variable or function named 'y1' is already defined in this scope
                //         (x1, string y1) = new C();
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y1").WithArguments("y1").WithLocation(8, 21),
                // (11,13): error CS0128: A local variable or function named 'x2' is already defined in this scope
                //         int x2;
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(11, 13),
                // (11,13): warning CS0168: The variable 'x2' is declared but never used
                //         int x2;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x2").WithArguments("x2").WithLocation(11, 13));
        }

        [Fact]
        public void AssignmentToUndeclaredVariableInMixedDeclarationAndAssignment()
        {
            string source = @"
class C
{
    static void Main()
    {
        (x1, string y1) = new C();
        (int x2, y2) = new C();
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (6,10): error CS0103: The name 'x1' does not exist in the current context
                //         (x1, string y1) = new C();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(6, 10),
                // (7,18): error CS0103: The name 'y2' does not exist in the current context
                //         (int x2, y2) = new C();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y2").WithArguments("y2").WithLocation(7, 18));
        }

        [Fact]
        public void MixedDeclarationAndAssignmentInForInitialization()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1;
        for((x1, string y1) = new C(); x1 < 2; x1++)
            System.Console.WriteLine(x1 + "" "" + y1);
        string y2;
        for((int x2, y2) = new C(); x2 < 2; x2++)
            System.Console.WriteLine(x2 + "" "" + y2);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            var comp = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: @"1 hello
1 hello");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size      109 (0x6d)
  .maxstack  3
  .locals init (int V_0, //x1
                string V_1, //y2
                string V_2, //y1
                int V_3,
                string V_4,
                int V_5) //x2
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_3
  IL_0007:  ldloca.s   V_4
  IL_0009:  callvirt   ""void C.Deconstruct(out int, out string)""
  IL_000e:  ldloc.3
  IL_000f:  stloc.0
  IL_0010:  ldloc.s    V_4
  IL_0012:  stloc.2
  IL_0013:  br.s       IL_0030
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""string int.ToString()""
  IL_001c:  ldstr      "" ""
  IL_0021:  ldloc.2
  IL_0022:  call       ""string string.Concat(string, string, string)""
  IL_0027:  call       ""void System.Console.WriteLine(string)""
  IL_002c:  ldloc.0
  IL_002d:  ldc.i4.1
  IL_002e:  add
  IL_002f:  stloc.0
  IL_0030:  ldloc.0
  IL_0031:  ldc.i4.2
  IL_0032:  blt.s      IL_0015
  IL_0034:  newobj     ""C..ctor()""
  IL_0039:  ldloca.s   V_3
  IL_003b:  ldloca.s   V_4
  IL_003d:  callvirt   ""void C.Deconstruct(out int, out string)""
  IL_0042:  ldloc.3
  IL_0043:  stloc.s    V_5
  IL_0045:  ldloc.s    V_4
  IL_0047:  stloc.1
  IL_0048:  br.s       IL_0067
  IL_004a:  ldloca.s   V_5
  IL_004c:  call       ""string int.ToString()""
  IL_0051:  ldstr      "" ""
  IL_0056:  ldloc.1
  IL_0057:  call       ""string string.Concat(string, string, string)""
  IL_005c:  call       ""void System.Console.WriteLine(string)""
  IL_0061:  ldloc.s    V_5
  IL_0063:  ldc.i4.1
  IL_0064:  add
  IL_0065:  stloc.s    V_5
  IL_0067:  ldloc.s    V_5
  IL_0069:  ldc.i4.2
  IL_006a:  blt.s      IL_004a
  IL_006c:  ret
}");
        }

        [Fact]
        public void MixDeclarationAndAssignmentInTupleDeconstructPermutationsOf2()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1 = 0;
        (x1, string y1) = (1, ""hello"");
        System.Console.WriteLine(x1 + "" "" + y1);
        int x2;
        (x2, var y2) = (1, ""hello"");
        System.Console.WriteLine(x2 + "" "" + y2);
        string y3 = """";
        (int x3, y3) = (1, ""hello"");
        System.Console.WriteLine(x3 + "" "" + y3);
        string y4;
        (var x4, y4) = (1, ""hello"");
        System.Console.WriteLine(x4 + "" "" + y4);
    }
}
";

            var comp = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: @"1 hello
1 hello
1 hello
1 hello");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size      140 (0x8c)
  .maxstack  3
  .locals init (int V_0, //x1
                string V_1, //y1
                int V_2, //x2
                string V_3, //y2
                string V_4, //y3
                int V_5, //x3
                string V_6, //y4
                int V_7) //x4
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.0
  IL_0004:  ldstr      ""hello""
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""string int.ToString()""
  IL_0011:  ldstr      "" ""
  IL_0016:  ldloc.1
  IL_0017:  call       ""string string.Concat(string, string, string)""
  IL_001c:  call       ""void System.Console.WriteLine(string)""
  IL_0021:  ldc.i4.1
  IL_0022:  stloc.2
  IL_0023:  ldstr      ""hello""
  IL_0028:  stloc.3
  IL_0029:  ldloca.s   V_2
  IL_002b:  call       ""string int.ToString()""
  IL_0030:  ldstr      "" ""
  IL_0035:  ldloc.3
  IL_0036:  call       ""string string.Concat(string, string, string)""
  IL_003b:  call       ""void System.Console.WriteLine(string)""
  IL_0040:  ldstr      """"
  IL_0045:  stloc.s    V_4
  IL_0047:  ldc.i4.1
  IL_0048:  stloc.s    V_5
  IL_004a:  ldstr      ""hello""
  IL_004f:  stloc.s    V_4
  IL_0051:  ldloca.s   V_5
  IL_0053:  call       ""string int.ToString()""
  IL_0058:  ldstr      "" ""
  IL_005d:  ldloc.s    V_4
  IL_005f:  call       ""string string.Concat(string, string, string)""
  IL_0064:  call       ""void System.Console.WriteLine(string)""
  IL_0069:  ldc.i4.1
  IL_006a:  stloc.s    V_7
  IL_006c:  ldstr      ""hello""
  IL_0071:  stloc.s    V_6
  IL_0073:  ldloca.s   V_7
  IL_0075:  call       ""string int.ToString()""
  IL_007a:  ldstr      "" ""
  IL_007f:  ldloc.s    V_6
  IL_0081:  call       ""string string.Concat(string, string, string)""
  IL_0086:  call       ""void System.Console.WriteLine(string)""
  IL_008b:  ret
}");
        }

        [Fact]
        public void MixedDeclarationAndAssignmentCSharpNine()
        {
            string source = @"
class Program
{
    static void Main()
    {
        int x1;
        (x1, string y1) = new A();
        string y2;
        (int x2, y2) = new A();
        bool z3;
        (int x3, (string y3, z3)) = new B();
        int x4;
        (x4, var (y4, z4)) = new B();
    }
}

class A
{
    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}

class B
{
    public void Deconstruct(out int a, out (string b, bool c) tuple)
    {
        a = 1;
        tuple = (""hello"", true);
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (7,9): error CS8773: Feature 'Mixed declarations and expressions in deconstruction' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         (x1, string y1) = new A();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "(x1, string y1) = new A()").WithArguments("Mixed declarations and expressions in deconstruction", "10.0").WithLocation(7, 9),
                // (9,9): error CS8773: Feature 'Mixed declarations and expressions in deconstruction' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         (int x2, y2) = new A();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "(int x2, y2) = new A()").WithArguments("Mixed declarations and expressions in deconstruction", "10.0").WithLocation(9, 9),
                // (11,9): error CS8773: Feature 'Mixed declarations and expressions in deconstruction' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         (int x3, (string y3, z3)) = new B();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "(int x3, (string y3, z3)) = new B()").WithArguments("Mixed declarations and expressions in deconstruction", "10.0").WithLocation(11, 9),
                // (13,9): error CS8773: Feature 'Mixed declarations and expressions in deconstruction' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         (x4, var (y4, z4)) = new B();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "(x4, var (y4, z4)) = new B()").WithArguments("Mixed declarations and expressions in deconstruction", "10.0").WithLocation(13, 9));
        }

        [Fact]
        public void NestedMixedDeclarationAndAssignmentPermutations()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1;
        string y1;
        (x1, (y1, var z1)) = new C();
        System.Console.WriteLine(x1 + "" "" + y1 + "" "" + z1);

        int x2;
        bool z2;
        (x2, (var y2, z2)) = new C();
        System.Console.WriteLine(x2 + "" "" + y2 + "" "" + z2);

        string y3;
        bool z3;
        (var x3, (y3, z3)) = new C();
        System.Console.WriteLine(x3 + "" "" + y3 + "" "" + z3);

        bool z4;
        (var x4, (var y4, z4)) = new C();
        System.Console.WriteLine(x4 + "" "" + y4 + "" "" + z4);

        string y5;
        (var x5, (y5, var z5)) = new C();
        System.Console.WriteLine(x5 + "" "" + y5 + "" "" + z5);

        int x6;
        (x6, (var y6, var z6)) = new C();
        System.Console.WriteLine(x6 + "" "" + y6 + "" "" + z6);

        int x7;
        (x7, var (y7, z7)) = new C();
        System.Console.WriteLine(x7 + "" "" + y7 + "" "" + z7);
    }

    public void Deconstruct(out int a, out (string a, bool b) b)
    {
        a = 1;
        b = (""hello"", true);
    }
}
";

            var comp = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: @"1 hello True
1 hello True
1 hello True
1 hello True
1 hello True
1 hello True
1 hello True");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MixedDeclarationAndAssignmentUseBeforeDeclaration()
        {
            string source = @"
class Program
{
    static void Main()
    {
        (x1, string y1) = new A();
        int x1;
        (int x2, y2) = new A();
        string y2;
        (int x3, (string y3, z3)) = new B();
        bool z3;
        (x4, var (y4, z4)) = new B();
        int x4;
    }
}

class A
{
    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}

class B
{
    public void Deconstruct(out int a, out (string b, bool c) tuple)
    {
        a = 1;
        tuple = (""hello"", true);
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (6,10): error CS0841: Cannot use local variable 'x1' before it is declared
                    //         (x1, string y1) = new A();
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1", isSuppressed: false).WithArguments("x1").WithLocation(6, 10),
                    // (8,18): error CS0841: Cannot use local variable 'y2' before it is declared
                    //         (int x2, y2) = new A();
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y2", isSuppressed: false).WithArguments("y2").WithLocation(8, 18),
                    // (10,30): error CS0841: Cannot use local variable 'z3' before it is declared
                    //         (int x3, (string y3, z3)) = new B();
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "z3", isSuppressed: false).WithArguments("z3").WithLocation(10, 30),
                    // (12,10): error CS0841: Cannot use local variable 'x4' before it is declared
                    //         (x4, var (y4, z4)) = new B();
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4", isSuppressed: false).WithArguments("x4").WithLocation(12, 10));
        }

        [Fact]
        public void MixedDeclarationAndAssignmentUseDeclaredVariableInAssignment()
        {
            string source = @"
class Program
{
    static void Main()
    {
        (var x1, x1) = new A();
        (x2, var x2) = new A();
        (var x3, (var y3, x3)) = new B();
        (x4, (var y4, var x4)) = new B();
    }
}

class A
{
    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}

class B
{
    public void Deconstruct(out int a, out (string b, bool c) tuple)
    {
        a = 1;
        tuple = (""hello"", true);
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (6,18): error CS0841: Cannot use local variable 'x1' before it is declared
                    //         (var x1, x1) = new A();
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 18),
                    // (7,10): error CS0841: Cannot use local variable 'x2' before it is declared
                    //         (x2, var x2) = new A();
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(7, 10),
                    // (8,27): error CS0841: Cannot use local variable 'x3' before it is declared
                    //         (var x3, (var y3, x3)) = new B();
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x3").WithArguments("x3").WithLocation(8, 27),
                    // (9,10): error CS0841: Cannot use local variable 'x4' before it is declared
                    //         (x4, (var y4, var x4)) = new B();
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(9, 10));
        }
    }
}
