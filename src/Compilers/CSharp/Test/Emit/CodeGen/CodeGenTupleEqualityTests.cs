// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.TupleEquality)]
    public class CodeGenTupleEqualityTests : CSharpTestBase
    {
        [Fact]
        public void TestCSharp7_2()
        {
            var source = @"
class C
{
    static void Main()
    {
        var t = (1, 2);
        System.Console.Write(t == (1, 2));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_2);
            comp.VerifyDiagnostics(
                // (7,30): error CS8320: Feature 'tuple equality' is not available in C# 7.2. Please use language version 7.3 or greater.
                //         System.Console.Write(t == (1, 2));
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "t == (1, 2)").WithArguments("tuple equality", "7.3").WithLocation(7, 30)
                );
        }

        [Theory]
        [InlineData("(1, 2)", "(1L, 2L)", true)]
        [InlineData("(1, 2)", "(1, 0)", false)]
        [InlineData("(1, 2)", "(0, 2)", false)]
        [InlineData("(1, 2)", "((long, long))(1, 2)", true)]
        [InlineData("((1, 2L), (3, 4))", "((1L, 2), (3L, 4))", true)]
        [InlineData("((1, 2L), (3, 4))", "((0L, 2), (3L, 4))", false)]
        [InlineData("((1, 2L), (3, 4))", "((1L, 0), (3L, 4))", false)]
        [InlineData("((1, 2L), (3, 4))", "((1L, 0), (0L, 4))", false)]
        [InlineData("((1, 2L), (3, 4))", "((1L, 0), (3L, 0))", false)]
        void TestSimple(string change1, string change2, bool expectedMatch)
        {
            var sourceTemplate = @"
class C
{
    static void Main()
    {
        var t1 = CHANGE1;
        var t2 = CHANGE2;
        System.Console.Write($""{(t1 == t2) == EXPECTED} {(t1 != t2) != EXPECTED}"");
    }
}";
            string source = sourceTemplate
                .Replace("CHANGE1", change1)
                .Replace("CHANGE2", change2)
                .Replace("EXPECTED", expectedMatch ? "true" : "false");
            string name = GetUniqueName();
            var comp = CreateCompilation(source, options: TestOptions.DebugExe, assemblyName: name);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True True");
        }

        [Fact]
        public void TestTupleLiteralsWithDifferentCardinalities()
        {
            var source = @"
class C
{
    static bool M()
    {
        return (1, 1) == (2, 2, 2);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,16): error CS8373: Tuple types used as operands of an == or != operator must have matching cardinalities. But this operator has tuple types of cardinality 2 on the left and 3 on the right.
                //         return (1, 1) == (2, 2, 2);
                Diagnostic(ErrorCode.ERR_TupleSizesMismatchForBinOps, "(1, 1) == (2, 2, 2)").WithArguments("2", "3").WithLocation(6, 16)
                );
        }

        [Fact]
        public void TestTuplesWithDifferentCardinalities()
        {
            var source = @"
class C
{
    static bool M()
    {
        var t1 = (1, 1);
        var t2 = (2, 2, 2);
        return t1 == t2;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,16): error CS8355: Tuple types used as operands of a binary operator must have matching cardinalities. But this operator has tuple types of cardinality 2 on the left and 3 on the right.
                //         return t1 == t2;
                Diagnostic(ErrorCode.ERR_TupleSizesMismatchForBinOps, "t1 == t2").WithArguments("2", "3").WithLocation(8, 16)
                );
        }

        [Fact]
        public void TestNestedTuplesWithDifferentCardinalities()
        {
            var source = @"
class C
{
    static bool M()
    {
        var t1 = (1, (1, 1));
        var t2 = (2, (2, 2, 2));
        return t1 == t2;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,16): error CS8355: Tuple types used as operands of a binary operator must have matching cardinalities. But this operator has tuple types of cardinality 2 on the left and 3 on the right.
                //         return t1 == t2;
                Diagnostic(ErrorCode.ERR_TupleSizesMismatchForBinOps, "t1 == t2").WithArguments("2", "3").WithLocation(8, 16)
                );
        }

        [Fact, WorkItem(25295, "https://github.com/dotnet/roslyn/issues/25295")]
        public void TestWithoutValueTuple()
        {
            var source = @"
class C
{
    static bool M()
    {
        return (1, 2) == (3, 4);
    }
}";
            var comp = CreateCompilationWithMscorlib40(source);

            // https://github.com/dotnet/roslyn/issues/25295
            // Can we relax the requirement on ValueTuple types being found?

            comp.VerifyDiagnostics(
                // (6,16): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         return (1, 2) == (3, 4);
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(1, 2)").WithArguments("System.ValueTuple`2").WithLocation(6, 16),
                // (6,26): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         return (1, 2) == (3, 4);
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(3, 4)").WithArguments("System.ValueTuple`2").WithLocation(6, 26),
                // (6,16): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         return (1, 2) == (3, 4);
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(1, 2)").WithArguments("System.ValueTuple`2").WithLocation(6, 16),
                // (6,26): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         return (1, 2) == (3, 4);
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(3, 4)").WithArguments("System.ValueTuple`2").WithLocation(6, 26)
                );
        }

        [Fact]
        public void TestNestedNullableTuplesWithDifferentCardinalities()
        {
            var source = @"
class C
{
    static bool M()
    {
        (int, int)? nt = (1, 1);
        var t1 = (1, nt);
        var t2 = (2, (2, 2, 2));
        return t1 == t2;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,16): error CS8373: Tuple types used as operands of a binary operator must have matching cardinalities. But this operator has tuple types of cardinality 2 on the left and 3 on the right.
                //         return t1 == t2;
                Diagnostic(ErrorCode.ERR_TupleSizesMismatchForBinOps, "t1 == t2").WithArguments("2", "3").WithLocation(9, 16)
                );
        }

        [Fact]
        public void TestILForSimpleEqual()
        {
            var source = @"
class C
{
    static bool M()
    {
        var t1 = (1, 1);
        var t2 = (2, 2);
        return t1 == t2;
    }
}";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.M", @"{
  // Code size       50 (0x32)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0, //t1
                System.ValueTuple<int, int> V_1,
                System.ValueTuple<int, int> V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldc.i4.2
  IL_000a:  ldc.i4.2
  IL_000b:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0010:  ldloc.0
  IL_0011:  stloc.1
  IL_0012:  stloc.2
  IL_0013:  ldloc.1
  IL_0014:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0019:  ldloc.2
  IL_001a:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_001f:  bne.un.s   IL_0030
  IL_0021:  ldloc.1
  IL_0022:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0027:  ldloc.2
  IL_0028:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_002d:  ceq
  IL_002f:  ret
  IL_0030:  ldc.i4.0
  IL_0031:  ret
}");
        }

        [Fact]
        public void TestILForSimpleNotEqual()
        {
            var source = @"
class C
{
    static bool M((int, int) t1, (int, int) t2)
    {
        return t1 != t2;
    }
}";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.M", @"{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0,
                System.ValueTuple<int, int> V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_000a:  ldloc.1
  IL_000b:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0010:  bne.un.s   IL_0024
  IL_0012:  ldloc.0
  IL_0013:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0018:  ldloc.1
  IL_0019:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_001e:  ceq
  IL_0020:  ldc.i4.0
  IL_0021:  ceq
  IL_0023:  ret
  IL_0024:  ldc.i4.1
  IL_0025:  ret
}");
        }

        [Fact]
        public void TestILForSimpleEqualOnInTuple()
        {
            var source = @"
class C
{
    static bool M(in (int, int) t1, in (int, int) t2)
    {
        return t1 == t2;
    }
}";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();

            // note: the logic to save variables and side-effects results in copying the inputs
            comp.VerifyIL("C.M", @"{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0,
                System.ValueTuple<int, int> V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""System.ValueTuple<int, int>""
  IL_0006:  stloc.0
  IL_0007:  ldarg.1
  IL_0008:  ldobj      ""System.ValueTuple<int, int>""
  IL_000d:  stloc.1
  IL_000e:  ldloc.0
  IL_000f:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0014:  ldloc.1
  IL_0015:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_001a:  bne.un.s   IL_002b
  IL_001c:  ldloc.0
  IL_001d:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0022:  ldloc.1
  IL_0023:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0028:  ceq
  IL_002a:  ret
  IL_002b:  ldc.i4.0
  IL_002c:  ret
}");
        }

        [Fact]
        public void TestILForSimpleEqualOnTupleLiterals()
        {
            var source = @"
class C
{
    static void Main()
    {
        M(1, 1);
        M(1, 2);
        M(2, 1);
    }
    static void M(int x, byte y)
    {
        System.Console.Write($""{(x, x) == (y, y)} "");
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "True False False");
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.M", @"{
// Code size       38 (0x26)
  .maxstack  3
  .locals init (int V_0,
                byte V_1,
                byte V_2)
  IL_0000:  ldstr      ""{0} ""
  IL_0005:  ldarg.0
  IL_0006:  ldarg.0
  IL_0007:  stloc.0
  IL_0008:  ldarg.1
  IL_0009:  stloc.1
  IL_000a:  ldarg.1
  IL_000b:  stloc.2
  IL_000c:  ldloc.1
  IL_000d:  bne.un.s   IL_0015
  IL_000f:  ldloc.0
  IL_0010:  ldloc.2
  IL_0011:  ceq
  IL_0013:  br.s       IL_0016
  IL_0015:  ldc.i4.0
  IL_0016:  box        ""bool""
  IL_001b:  call       ""string string.Format(string, object)""
  IL_0020:  call       ""void System.Console.Write(string)""
  IL_0025:  ret
}");

            var tree = comp.Compilation.SyntaxTrees.First();
            var model = comp.Compilation.GetSemanticModel(tree);

            // check x
            var tupleX = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().First();
            Assert.Equal("(x, x)", tupleX.ToString());
            Assert.Equal(Conversion.Identity, model.GetConversion(tupleX));
            Assert.Null(model.GetSymbolInfo(tupleX).Symbol);

            var lastX = tupleX.Arguments[1].Expression;
            Assert.Equal("x", lastX.ToString());
            Assert.Equal(Conversion.Identity, model.GetConversion(lastX));
            Assert.Equal("System.Int32 x", model.GetSymbolInfo(lastX).Symbol.ToTestDisplayString());

            var xSymbol = model.GetTypeInfo(lastX);
            Assert.Equal("System.Int32", xSymbol.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", xSymbol.ConvertedType.ToTestDisplayString());

            var tupleXSymbol = model.GetTypeInfo(tupleX);
            Assert.Equal("(System.Int32, System.Int32)", tupleXSymbol.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)", tupleXSymbol.ConvertedType.ToTestDisplayString());

            // check y
            var tupleY = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Last();
            Assert.Equal("(y, y)", tupleY.ToString());
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(tupleY).Kind);

            var lastY = tupleY.Arguments[1].Expression;
            Assert.Equal("y", lastY.ToString());
            Assert.Equal(Conversion.ImplicitNumeric, model.GetConversion(lastY));

            var ySymbol = model.GetTypeInfo(lastY);
            Assert.Equal("System.Byte", ySymbol.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", ySymbol.ConvertedType.ToTestDisplayString());

            var tupleYSymbol = model.GetTypeInfo(tupleY);
            Assert.Equal("(System.Byte, System.Byte)", tupleYSymbol.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)", tupleYSymbol.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void TestILForAlwaysValuedNullable()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write($""{(new int?(Identity(42)), (int?)2) == (new int?(42), new int?(2))} "");
    }
    static int Identity(int x) => x;
}";
            var comp = CompileAndVerify(source, expectedOutput: "True");
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main", @"{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldstr      ""{0} ""
  IL_0005:  ldc.i4.s   42
  IL_0007:  call       ""int C.Identity(int)""
  IL_000c:  ldc.i4.s   42
  IL_000e:  bne.un.s   IL_0016
  IL_0010:  ldc.i4.2
  IL_0011:  ldc.i4.2
  IL_0012:  ceq
  IL_0014:  br.s       IL_0017
  IL_0016:  ldc.i4.0
  IL_0017:  box        ""bool""
  IL_001c:  call       ""string string.Format(string, object)""
  IL_0021:  call       ""void System.Console.Write(string)""
  IL_0026:  ret
}");
        }

        [Fact]
        public void TestILForNullableElementsEqualsToNull()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write(M(null, null));
        System.Console.Write(M(1, true));
    }
    static bool M(int? i1, bool? b1)
    {
        var t1 = (i1, b1);
        return t1 == (null, null);
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "TrueFalse");
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.M", @"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (System.ValueTuple<int?, bool?> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  newobj     ""System.ValueTuple<int?, bool?>..ctor(int?, bool?)""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldflda     ""int? System.ValueTuple<int?, bool?>.Item1""
  IL_000f:  call       ""bool int?.HasValue.get""
  IL_0014:  brtrue.s   IL_0026
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldflda     ""bool? System.ValueTuple<int?, bool?>.Item2""
  IL_001d:  call       ""bool bool?.HasValue.get""
  IL_0022:  ldc.i4.0
  IL_0023:  ceq
  IL_0025:  ret
  IL_0026:  ldc.i4.0
  IL_0027:  ret
}");
        }

        [Fact]
        public void TestILForNullableElementsNotEqualsToNull()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write(M(null, null));
        System.Console.Write(M(1, true));
    }
    static bool M(int? i1, bool? b1)
    {
        var t1 = (i1, b1);
        return t1 != (null, null);
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "FalseTrue");
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.M", @"{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (System.ValueTuple<int?, bool?> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  newobj     ""System.ValueTuple<int?, bool?>..ctor(int?, bool?)""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldflda     ""int? System.ValueTuple<int?, bool?>.Item1""
  IL_000f:  call       ""bool int?.HasValue.get""
  IL_0014:  brtrue.s   IL_0023
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldflda     ""bool? System.ValueTuple<int?, bool?>.Item2""
  IL_001d:  call       ""bool bool?.HasValue.get""
  IL_0022:  ret
  IL_0023:  ldc.i4.1
  IL_0024:  ret
}");
        }

        [Fact]
        public void TestILForNullableElementsComparedToNonNullValues()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write(M((null, null)));
        System.Console.Write(M((2, true)));
    }
    static bool M((int?, bool?) t1)
    {
        return t1 == (2, true);
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "FalseTrue");
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.M", @"{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (System.ValueTuple<int?, bool?> V_0,
                int? V_1,
                int V_2,
                bool? V_3,
                bool V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldfld      ""int? System.ValueTuple<int?, bool?>.Item1""
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.2
  IL_000a:  stloc.2
  IL_000b:  ldloca.s   V_1
  IL_000d:  call       ""int int?.GetValueOrDefault()""
  IL_0012:  ldloc.2
  IL_0013:  ceq
  IL_0015:  ldloca.s   V_1
  IL_0017:  call       ""bool int?.HasValue.get""
  IL_001c:  and
  IL_001d:  brfalse.s  IL_003d
  IL_001f:  ldloc.0
  IL_0020:  ldfld      ""bool? System.ValueTuple<int?, bool?>.Item2""
  IL_0025:  stloc.3
  IL_0026:  ldc.i4.1
  IL_0027:  stloc.s    V_4
  IL_0029:  ldloca.s   V_3
  IL_002b:  call       ""bool bool?.GetValueOrDefault()""
  IL_0030:  ldloc.s    V_4
  IL_0032:  ceq
  IL_0034:  ldloca.s   V_3
  IL_0036:  call       ""bool bool?.HasValue.get""
  IL_003b:  and
  IL_003c:  ret
  IL_003d:  ldc.i4.0
  IL_003e:  ret
}");
        }

        [Fact]
        public void TestILForNullableStructEqualsToNull()
        {
            var source = @"
struct S
{
    static void Main()
    {
        S? s = null;
        _ = s == null;
        System.Console.Write((s, null) == (null, s));
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "True");
            comp.VerifyDiagnostics();

            comp.VerifyIL("S.Main", @"{
  // Code size       48 (0x30)
  .maxstack  2
  .locals init (S? V_0, //s
                S? V_1,
                S? V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S?""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool S?.HasValue.get""
  IL_000f:  pop
  IL_0010:  ldloc.0
  IL_0011:  stloc.1
  IL_0012:  ldloc.0
  IL_0013:  stloc.2
  IL_0014:  ldloca.s   V_1
  IL_0016:  call       ""bool S?.HasValue.get""
  IL_001b:  brtrue.s   IL_0029
  IL_001d:  ldloca.s   V_2
  IL_001f:  call       ""bool S?.HasValue.get""
  IL_0024:  ldc.i4.0
  IL_0025:  ceq
  IL_0027:  br.s       IL_002a
  IL_0029:  ldc.i4.0
  IL_002a:  call       ""void System.Console.Write(bool)""
  IL_002f:  ret
}");
        }

        [Fact, WorkItem(25488, "https://github.com/dotnet/roslyn/issues/25488")]
        public void TestThisStruct()
        {
            var source = @"
public struct S
{
    public int I;
    public static void Main()
    {
        S s = new S() { I = 1 };
        s.M();
    }
    void M()
    {
        System.Console.Write((this, 2) == (1, this.Mutate()));
    }

    S Mutate()
    {
        I++;
        return this;
    }
    public static implicit operator S(int value) { return new S() { I = value }; }
    public static bool operator==(S s1, S s2) { System.Console.Write($""{s1.I} == {s2.I}, ""); return s1.I == s2.I; }
    public static bool operator!=(S s1, S s2) { throw null; }
    public override bool Equals(object o) { throw null; }
    public override int GetHashCode() { throw null; }
}";
            var comp = CompileAndVerify(source, expectedOutput: "1 == 1, 2 == 2, True");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestThisClass()
        {
            var source = @"
public class C
{
    public int I;
    public static void Main()
    {
        C c = new C() { I = 1 };
        c.M();
    }
    void M()
    {
        System.Console.Write((this, 2) == (2, this.Mutate()));
    }

    C Mutate()
    {
        I++;
        return this;
    }
    public static implicit operator C(int value) { return new C() { I = value }; }
    public static bool operator==(C c1, C c2) { System.Console.Write($""{c1.I} == {c2.I}, ""); return c1.I == c2.I; }
    public static bool operator!=(C c1, C c2) { throw null; }
    public override bool Equals(object o) { throw null; }
    public override int GetHashCode() { throw null; }
}";
            var comp = CompileAndVerify(source, expectedOutput: "2 == 2, 2 == 2, True");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestSimpleEqualOnTypelessTupleLiteral()
        {
            var source = @"
class C
{
    static bool M((string, long) t)
    {
        return t == (null, 1) && t == (""hello"", 1);
    }
}";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();

            var tree = comp.Compilation.SyntaxTrees.First();
            var model = comp.Compilation.GetSemanticModel(tree);

            var tuple1 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(0);
            var symbol1 = model.GetTypeInfo(tuple1);
            Assert.Null(symbol1.Type);
            Assert.Equal("(System.String, System.Int64)", symbol1.ConvertedType.ToTestDisplayString());
            Assert.Equal("(System.String, System.Int64)", model.GetDeclaredSymbol(tuple1).ToTestDisplayString());

            var tuple2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            var symbol2 = model.GetTypeInfo(tuple2);
            Assert.Equal("(System.String, System.Int32)", symbol2.Type.ToTestDisplayString());
            Assert.Equal("(System.String, System.Int64)", symbol2.ConvertedType.ToTestDisplayString());
            Assert.False(model.GetConstantValue(tuple2).HasValue);
            Assert.Equal(1, model.GetConstantValue(tuple2.Arguments[1].Expression).Value);
        }

        [Fact]
        public void TestConversionOnTupleExpression()
        {
            var source = @"
class C
{
    static bool M((int, byte) t)
    {
        return t == (1L, 2);
    }
}";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();

            var tree = comp.Compilation.SyntaxTrees.First();
            var model = comp.Compilation.GetSemanticModel(tree);

            var equals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            // check t
            var t = equals.Left;
            Assert.Equal("t", t.ToString());
            Assert.Equal("(System.Int32, System.Byte) t", model.GetSymbolInfo(t).Symbol.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTuple, model.GetConversion(t).Kind);

            var tTypeInfo = model.GetTypeInfo(t);
            Assert.Equal("(System.Int32, System.Byte)", tTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.Int64, System.Int32)", tTypeInfo.ConvertedType.ToTestDisplayString());

            // check tuple
            var tuple = equals.Right;
            Assert.Equal("(1L, 2)", tuple.ToString());
            Assert.Null(model.GetSymbolInfo(tuple).Symbol);
            Assert.Equal(Conversion.Identity, model.GetConversion(tuple));

            var tupleTypeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("(System.Int64, System.Int32)", tupleTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.Int64, System.Int32)", tupleTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void TestOtherOperatorsOnTuples()
        {
            var source = @"
class C
{
    void M()
    {
        var t1 = (1, 2);
        _ = t1 + t1; // error 1
        _ = t1 > t1; // error 2
        _ = t1 >= t1; // error 3
        _ = !t1; // error 4
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,13): error CS0019: Operator '+' cannot be applied to operands of type '(int, int)' and '(int, int)'
                //         _ = t1 + t1; // error 1
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t1 + t1").WithArguments("+", "(int, int)", "(int, int)").WithLocation(7, 13),
                // (8,13): error CS0019: Operator '>' cannot be applied to operands of type '(int, int)' and '(int, int)'
                //         _ = t1 > t1; // error 2
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t1 > t1").WithArguments(">", "(int, int)", "(int, int)").WithLocation(8, 13),
                // (9,13): error CS0019: Operator '>=' cannot be applied to operands of type '(int, int)' and '(int, int)'
                //         _ = t1 >= t1; // error 3
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t1 >= t1").WithArguments(">=", "(int, int)", "(int, int)").WithLocation(9, 13),
                // (10,13): error CS0023: Operator '!' cannot be applied to operand of type '(int, int)'
                //         _ = !t1; // error 4
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "!t1").WithArguments("!", "(int, int)").WithLocation(10, 13)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            Assert.Equal("(System.Int32, System.Int32)", model.GetDeclaredSymbol(tuple).ToTestDisplayString());
        }

        [Fact]
        public void TestTypelessTuples()
        {
            var source = @"
class C
{
    static void Main()
    {
        string s = null;
        System.Console.Write((s, null) == (null, s));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            // check first tuple and its null
            var tuple1 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(0);
            Assert.Equal("(s, null)", tuple1.ToString());
            var tupleType1 = model.GetTypeInfo(tuple1);
            Assert.Null(tupleType1.Type);
            Assert.Equal("(System.String s, System.String)", tupleType1.ConvertedType.ToTestDisplayString());

            var tuple1Null = tuple1.Arguments[1].Expression;
            var tuple1NullTypeInfo = model.GetTypeInfo(tuple1Null);
            Assert.Null(tuple1NullTypeInfo.Type);
            Assert.Equal("System.String", tuple1NullTypeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitReference, model.GetConversion(tuple1Null).Kind);

            // check second tuple and its null
            var tuple2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            Assert.Equal("(null, s)", tuple2.ToString());
            var tupleType2 = model.GetTypeInfo(tuple2);
            Assert.Null(tupleType2.Type);
            Assert.Equal("(System.String, System.String s)", tupleType2.ConvertedType.ToTestDisplayString());

            var tuple2Null = tuple2.Arguments[0].Expression;
            var tuple2NullTypeInfo = model.GetTypeInfo(tuple2Null);
            Assert.Null(tuple2NullTypeInfo.Type);
            Assert.Equal("System.String", tuple2NullTypeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitReference, model.GetConversion(tuple2Null).Kind);
        }

        [Fact]
        public void TestWithNoSideEffectsOrTemps()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write((1, 2) == (1, 3));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "False");
        }

        [Fact]
        public void TestSimpleTupleAndTupleType_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        var t1 = (1, 2L);
        System.Console.Write(t1 == (1L, 2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var equals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            // check t1
            var t1 = equals.Left;
            Assert.Equal("t1", t1.ToString());

            var t1TypeInfo = model.GetTypeInfo(t1);
            Assert.Equal("(System.Int32, System.Int64)", t1TypeInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.Int64, System.Int64)", t1TypeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTuple, model.GetConversion(t1).Kind);

            // check tuple and its literal 2
            var tuple = (TupleExpressionSyntax)equals.Right;
            Assert.Equal("(1L, 2)", tuple.ToString());

            var tupleType = model.GetTypeInfo(tuple);
            Assert.Equal("(System.Int64, System.Int32)", tupleType.Type.ToTestDisplayString());
            Assert.Equal("(System.Int64, System.Int64)", tupleType.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(tuple).Kind);

            var two = tuple.Arguments[1].Expression;
            Assert.Equal("2", two.ToString());

            var twoType = model.GetTypeInfo(two);
            Assert.Equal("System.Int32", twoType.Type.ToTestDisplayString());
            Assert.Equal("System.Int64", twoType.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitNumeric, model.GetConversion(two).Kind);
        }

        [Fact]
        public void TestSimpleTupleAndTupleType_02()
        {
            var source = @"
class C
{
    static void Main()
    {
        var t1 = (1, 2UL);
        System.Console.Write(t1 == (1L, 2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var equals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            // check t1
            var t1 = equals.Left;
            Assert.Equal("t1", t1.ToString());

            var t1TypeInfo = model.GetTypeInfo(t1);
            Assert.Equal("(System.Int32, System.UInt64)", t1TypeInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.Int64, System.UInt64)", t1TypeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTuple, model.GetConversion(t1).Kind);

            // check tuple and its literal 2
            var tuple = (TupleExpressionSyntax)equals.Right;
            Assert.Equal("(1L, 2)", tuple.ToString());

            var tupleType = model.GetTypeInfo(tuple);
            Assert.Equal("(System.Int64, System.Int32)", tupleType.Type.ToTestDisplayString());
            Assert.Equal("(System.Int64, System.UInt64)", tupleType.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(tuple).Kind);

            var two = tuple.Arguments[1].Expression;
            Assert.Equal("2", two.ToString());

            var twoType = model.GetTypeInfo(two);
            Assert.Equal("System.Int32", twoType.Type.ToTestDisplayString());
            Assert.Equal("System.UInt64", twoType.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitConstant, model.GetConversion(two).Kind);
        }

        [Fact]
        public void TestNestedTupleAndTupleType()
        {
            var source = @"
class C
{
    static void Main()
    {
        var t1 = (1, (2L, ""hello""));
        var t2 = (2, ""hello"");
        System.Console.Write(t1 == (1L, t2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var equals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            // check t1
            var t1 = equals.Left;
            Assert.Equal("t1", t1.ToString());

            var t1TypeInfo = model.GetTypeInfo(t1);
            Assert.Equal("(System.Int32, (System.Int64, System.String))", t1TypeInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.Int64, (System.Int64, System.String))", t1TypeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTuple, model.GetConversion(t1).Kind);
            Assert.Equal("(System.Int32, (System.Int64, System.String)) t1", model.GetSymbolInfo(t1).Symbol.ToTestDisplayString());

            // check tuple and its t2
            var tuple = (TupleExpressionSyntax)equals.Right;
            Assert.Equal("(1L, t2)", tuple.ToString());
            var tupleType = model.GetTypeInfo(tuple);
            Assert.Equal("(System.Int64, (System.Int32, System.String) t2)", tupleType.Type.ToTestDisplayString());
            Assert.Equal("(System.Int64, (System.Int64, System.String) t2)", tupleType.ConvertedType.ToTestDisplayString());

            var t2 = tuple.Arguments[1].Expression;
            Assert.Equal("t2", t2.ToString());

            var t2TypeInfo = model.GetTypeInfo(t2);
            Assert.Equal("(System.Int32, System.String)", t2TypeInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.Int64, System.String)", t2TypeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTuple, model.GetConversion(t2).Kind);
            Assert.Equal("(System.Int32, System.String) t2", model.GetSymbolInfo(t2).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestTypelessTupleAndTupleType()
        {
            var source = @"
class C
{
    static void Main()
    {
        (string, string) t = (null, null);
        System.Console.Write(t == (null, null));
        System.Console.Write(t != (null, null));
        System.Console.Write((1, t) == (1, (null, null)));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "TrueFalseTrue");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            Assert.Equal("(null, null)", tuple.ToString());
            var tupleType = model.GetTypeInfo(tuple);
            Assert.Null(tupleType.Type);
            Assert.Equal("(System.String, System.String)", tupleType.ConvertedType.ToTestDisplayString());

            // check last tuple ...
            var lastEquals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Last();
            var lastTuple = (TupleExpressionSyntax)lastEquals.Right;
            Assert.Equal("(1, (null, null))", lastTuple.ToString());
            TypeInfo lastTupleTypeInfo = model.GetTypeInfo(lastTuple);
            Assert.Null(lastTupleTypeInfo.Type);
            Assert.Equal("(System.Int32, (System.String, System.String))", lastTupleTypeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(lastTuple).Kind);

            // ... and its nested (null, null) tuple ...
            var nullNull = (TupleExpressionSyntax)lastTuple.Arguments[1].Expression;
            TypeInfo nullNullTypeInfo = model.GetTypeInfo(nullNull);
            Assert.Null(nullNullTypeInfo.Type);
            Assert.Equal("(System.String, System.String)", nullNullTypeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(nullNull).Kind);

            // ... and its last null.
            var lastNull = nullNull.Arguments[1].Expression;
            TypeInfo lastNullTypeInfo = model.GetTypeInfo(lastNull);
            Assert.Null(lastNullTypeInfo.Type);
            Assert.Equal("System.String", lastNullTypeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitReference, model.GetConversion(lastNull).Kind);
        }

        [Fact]
        public void TestTypedTupleAndDefault()
        {
            var source = @"
class C
{
    static void Main()
    {
        (string, string) t = (null, null);
        System.Console.Write(t == default);
        System.Console.Write(t != default);

        (string, string) t2 = (null, ""hello"");
        System.Console.Write(t2 == default);
        System.Console.Write(t2 != default);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "TrueFalseFalseTrue");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var defaultLiterals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Where(e => e.Kind() == SyntaxKind.DefaultLiteralExpression);

            foreach (var literal in defaultLiterals)
            {
                Assert.Equal("default", literal.ToString());
                var info = model.GetTypeInfo(literal);
                Assert.Equal("(System.String, System.String)", info.Type.ToTestDisplayString());
                Assert.Equal("(System.String, System.String)", info.ConvertedType.ToTestDisplayString());
            }
        }

        [Fact]
        public void TestNullableTupleAndDefault()
        {
            var source = @"
class C
{
    static void Main()
    {
        (string, string)? t = (null, null);
        System.Console.Write(t == default);
        System.Console.Write(t != default);
        System.Console.Write(default == t);
        System.Console.Write(default != t);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "FalseTrueFalseTrue");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var defaultLiterals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Where(e => e.Kind() == SyntaxKind.DefaultLiteralExpression);

            foreach (var literal in defaultLiterals)
            {
                Assert.Equal("default", literal.ToString());
                var info = model.GetTypeInfo(literal);
                Assert.Equal("(System.String, System.String)?", info.Type.ToTestDisplayString());
                Assert.Equal("(System.String, System.String)?", info.ConvertedType.ToTestDisplayString());
            }
        }

        [Fact]
        public void TestNullableTupleAndDefault_Nested()
        {
            var source = @"
class C
{
    static void Main()
    {
        (string, string)? t = (null, null);
        System.Console.Write((null, t) == (null, default));
        System.Console.Write((t, null) != (default, null));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "FalseTrue");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var defaultLiterals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Where(e => e.Kind() == SyntaxKind.DefaultLiteralExpression);

            foreach (var literal in defaultLiterals)
            {
                Assert.Equal("default", literal.ToString());
                var info = model.GetTypeInfo(literal);
                Assert.Equal("(System.String, System.String)?", info.Type.ToTestDisplayString());
                Assert.Equal("(System.String, System.String)?", info.ConvertedType.ToTestDisplayString());
            }
        }

        [Fact]
        public void TestNestedDefaultWithNonTupleType()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write((null, 1) == (null, default));
        System.Console.Write((0, null) != (default, null));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "FalseFalse");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var defaultLiterals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Where(e => e.Kind() == SyntaxKind.DefaultLiteralExpression);

            foreach (var literal in defaultLiterals)
            {
                Assert.Equal("default", literal.ToString());
                var info = model.GetTypeInfo(literal);
                Assert.Equal("System.Int32", info.Type.ToTestDisplayString());
                Assert.Equal("System.Int32", info.ConvertedType.ToTestDisplayString());
            }
        }

        [Fact]
        public void TestNestedDefaultWithNullableNonTupleType()
        {
            var source = @"
struct S
{
    static void Main()
    {
        S? ns = null;
        _ = (null, ns) == (null, default);
        _ = (ns, null) != (default, null);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,13): error CS0019: Operator '==' cannot be applied to operands of type 'S?' and 'default'
                //         _ = (null, ns) == (null, default);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(null, ns) == (null, default)").WithArguments("==", "S?", "default").WithLocation(7, 13),
                // (8,13): error CS0019: Operator '!=' cannot be applied to operands of type 'S?' and 'default'
                //         _ = (ns, null) != (default, null);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(ns, null) != (default, null)").WithArguments("!=", "S?", "default").WithLocation(8, 13)
                );
        }

        [Fact]
        public void TestNestedDefaultWithNullableNonTupleType_WithComparisonOperator()
        {
            var source = @"
public struct S
{
    public static void Main()
    {
        S? ns = new S();
        System.Console.Write((null, ns) == (null, default));
        System.Console.Write((ns, null) != (default, null));
    }
    public static bool operator==(S s1, S s2) => throw null;
    public static bool operator!=(S s1, S s2) => throw null;
    public override int GetHashCode() => throw null;
    public override bool Equals(object o) => throw null;
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "FalseTrue");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var defaults = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Where(e => e.Kind() == SyntaxKind.DefaultLiteralExpression);

            foreach (var literal in defaults)
            {
                var type = model.GetTypeInfo(literal);
                Assert.Equal("S?", type.Type.ToTestDisplayString());
                Assert.Equal("S?", type.ConvertedType.ToTestDisplayString());
            }
        }

        [Fact]
        public void TestAllDefaults()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write((default, default) == (default, default));
        System.Console.Write(default == (default, default));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,30): error CS8315: Operator '==' is ambiguous on operands 'default' and 'default'
                //         System.Console.Write((default, default) == (default, default));
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnDefaultOrNew, "(default, default) == (default, default)").WithArguments("==").WithLocation(6, 30),
                // (6,30): error CS8315: Operator '==' is ambiguous on operands 'default' and 'default'
                //         System.Console.Write((default, default) == (default, default));
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnDefaultOrNew, "(default, default) == (default, default)").WithArguments("==").WithLocation(6, 30),
                // (7,30): error CS0034: Operator '==' is ambiguous on operands of type 'default' and '(default, default)'
                //         System.Console.Write(default == (default, default));
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "default == (default, default)").WithArguments("==", "default", "(default, default)").WithLocation(7, 30)
                );
        }

        [Fact]
        public void TestNullsAndDefaults()
        {
            var source = @"
class C
{
    static void Main()
    {
        _ = (null, default) != (default, null);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,13): error CS0034: Operator '!=' is ambiguous on operands of type '<null>' and 'default'
                //         _ = (null, default) != (default, null);
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "(null, default) != (default, null)").WithArguments("!=", "<null>", "default").WithLocation(6, 13),
                // (6,13): error CS0034: Operator '!=' is ambiguous on operands of type 'default' and '<null>'
                //         _ = (null, default) != (default, null);
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "(null, default) != (default, null)").WithArguments("!=", "default", "<null>").WithLocation(6, 13)
                );
        }

        [Fact]
        public void TestAllDefaults_Nested()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write((null, (default, default)) == (null, (default, default)));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,30): error CS8315: Operator '==' is ambiguous on operands 'default' and 'default'
                //         System.Console.Write((null, (default, default)) == (null, (default, default)));
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnDefaultOrNew, "(null, (default, default)) == (null, (default, default))").WithArguments("==").WithLocation(6, 30),
                // (6,30): error CS8315: Operator '==' is ambiguous on operands 'default' and 'default'
                //         System.Console.Write((null, (default, default)) == (null, (default, default)));
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnDefaultOrNew, "(null, (default, default)) == (null, (default, default))").WithArguments("==").WithLocation(6, 30)
                );
        }

        [Fact]
        public void TestTypedTupleAndTupleOfDefaults()
        {
            var source = @"
class C
{
    static void Main()
    {
        (string, string)? t = (null, null);
        System.Console.Write(t == (default, default));
        System.Console.Write(t != (default, default));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "TrueFalse");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var lastTuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Last();

            Assert.Equal("(default, default)", lastTuple.ToString());
            Assert.Null(model.GetTypeInfo(lastTuple).Type);
            Assert.Equal("(System.String, System.String)?", model.GetTypeInfo(lastTuple).ConvertedType.ToTestDisplayString());

            var lastDefault = lastTuple.Arguments[1].Expression;
            Assert.Equal("default", lastDefault.ToString());
            Assert.Equal("System.String", model.GetTypeInfo(lastDefault).Type.ToTestDisplayString());
            Assert.Equal("System.String", model.GetTypeInfo(lastDefault).ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void TestTypelessTupleAndTupleOfDefaults()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write((null, () => 1) == (default, default));
        System.Console.Write((null, () => 2) == default);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,30): error CS0034: Operator '==' is ambiguous on operands of type '<null>' and 'default'
                //         System.Console.Write((null, () => 1) == (default, default));
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "(null, () => 1) == (default, default)").WithArguments("==", "<null>", "default").WithLocation(6, 30),
                // (6,30): error CS0019: Operator '==' cannot be applied to operands of type 'lambda expression' and 'default'
                //         System.Console.Write((null, () => 1) == (default, default));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(null, () => 1) == (default, default)").WithArguments("==", "lambda expression", "default").WithLocation(6, 30),
                // (7,30): error CS0034: Operator '==' is ambiguous on operands of type '(<null>, lambda expression)' and 'default'
                //         System.Console.Write((null, () => 2) == default);
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "(null, () => 2) == default").WithArguments("==", "(<null>, lambda expression)", "default").WithLocation(7, 30)
                );
        }

        [Fact]
        public void TestNullableStructAndDefault()
        {
            var source = @"
struct S
{
    static void M(string s)
    {
        S? ns = new S();
        _ = ns == null;
        _ = s == null;
        _ = ns == default; // error 1
        _ = (ns, ns) == (null, null);
        _ = (ns, ns) == (default, default); // errors 2 and 3
        _ = (ns, ns) == default; // error 4
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,13): error CS0019: Operator '==' cannot be applied to operands of type 'S?' and 'default'
                //         _ = ns == default; // error 1
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "ns == default").WithArguments("==", "S?", "default").WithLocation(9, 13),
                // (11,13): error CS0019: Operator '==' cannot be applied to operands of type 'S?' and 'default'
                //         _ = (ns, ns) == (default, default); // errors 2 and 3
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(ns, ns) == (default, default)").WithArguments("==", "S?", "default").WithLocation(11, 13),
                // (11,13): error CS0019: Operator '==' cannot be applied to operands of type 'S?' and 'default'
                //         _ = (ns, ns) == (default, default); // errors 2 and 3
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(ns, ns) == (default, default)").WithArguments("==", "S?", "default").WithLocation(11, 13),
                // (12,13): error CS0019: Operator '==' cannot be applied to operands of type 'S?' and 'S?'
                //         _ = (ns, ns) == default; // error 4
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(ns, ns) == default").WithArguments("==", "S?", "S?").WithLocation(12, 13),
                // (12,13): error CS0019: Operator '==' cannot be applied to operands of type 'S?' and 'S?'
                //         _ = (ns, ns) == default; // error 4
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(ns, ns) == default").WithArguments("==", "S?", "S?").WithLocation(12, 13)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var literals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>();

            var nullLiteral = literals.ElementAt(0);
            Assert.Equal("null", nullLiteral.ToString());
            Assert.Null(model.GetTypeInfo(nullLiteral).ConvertedType);

            var nullLiteral2 = literals.ElementAt(1);
            Assert.Equal("null", nullLiteral2.ToString());
            Assert.Null(model.GetTypeInfo(nullLiteral2).Type);
            Assert.Equal("System.String", model.GetTypeInfo(nullLiteral2).ConvertedType.ToTestDisplayString());

            var defaultLiterals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Where(e => e.Kind() == SyntaxKind.DelegateDeclaration);

            foreach (var defaultLiteral in defaultLiterals)
            {
                Assert.Equal("default", defaultLiteral.ToString());
                Assert.Null(model.GetTypeInfo(defaultLiteral).Type);
                Assert.Null(model.GetTypeInfo(defaultLiteral).ConvertedType);
            }
        }

        [Fact, WorkItem(25318, "https://github.com/dotnet/roslyn/issues/25318")]
        public void TestNullableStructAndDefault_WithComparisonOperator()
        {
            var source = @"
public struct S
{
    static void M(string s)
    {
        S? ns = new S();
        _ = ns == 1;
        _ = (ns, ns) == (default, default);
        _ = (ns, ns) == default;
    }
    public static bool operator==(S s, byte b) => throw null;
    public static bool operator!=(S s, byte b) => throw null;
    public override bool Equals(object other) => throw null;
    public override int GetHashCode() => throw null;
}";
            var comp = CreateCompilation(source);

            // https://github.com/dotnet/roslyn/issues/25318
            // This should be allowed

            comp.VerifyDiagnostics(
                // (7,13): error CS0019: Operator '==' cannot be applied to operands of type 'S?' and 'int'
                //         _ = ns == 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "ns == 1").WithArguments("==", "S?", "int").WithLocation(7, 13),
                // (8,13): error CS0019: Operator '==' cannot be applied to operands of type 'S?' and 'default'
                //         _ = (ns, ns) == (default, default);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(ns, ns) == (default, default)").WithArguments("==", "S?", "default").WithLocation(8, 13),
                // (8,13): error CS0019: Operator '==' cannot be applied to operands of type 'S?' and 'default'
                //         _ = (ns, ns) == (default, default);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(ns, ns) == (default, default)").WithArguments("==", "S?", "default").WithLocation(8, 13),
                // (9,13): error CS0019: Operator '==' cannot be applied to operands of type 'S?' and 'S?'
                //         _ = (ns, ns) == default;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(ns, ns) == default").WithArguments("==", "S?", "S?").WithLocation(9, 13),
                // (9,13): error CS0019: Operator '==' cannot be applied to operands of type 'S?' and 'S?'
                //         _ = (ns, ns) == default;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(ns, ns) == default").WithArguments("==", "S?", "S?").WithLocation(9, 13)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var defaultLiterals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Where(e => e.Kind() == SyntaxKind.DelegateDeclaration);

            // Should have types
            foreach (var defaultLiteral in defaultLiterals)
            {
                Assert.Equal("default", defaultLiteral.ToString());
                Assert.Null(model.GetTypeInfo(defaultLiteral).Type);
                Assert.Null(model.GetTypeInfo(defaultLiteral).ConvertedType);
                // https://github.com/dotnet/roslyn/issues/25318
                // default should become int
            }
        }

        [Fact]
        public void TestMixedTupleLiteralsAndTypes()
        {
            var source = @"
class C
{
    static void Main()
    {
        (string, string) t = (null, null);
        System.Console.Write((t, (null, null)) == ((null, null), t));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            // check last tuple ...
            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(3);
            Assert.Equal("((null, null), t)", tuple.ToString());

            var tupleType = model.GetTypeInfo(tuple);
            Assert.Null(tupleType.Type);
            Assert.Equal("((System.String, System.String), (System.String, System.String) t)",
                tupleType.ConvertedType.ToTestDisplayString());

            // ... its t ...
            var t = tuple.Arguments[1].Expression;
            Assert.Equal("t", t.ToString());

            var tType = model.GetTypeInfo(t);
            Assert.Equal("(System.String, System.String)", tType.Type.ToTestDisplayString());
            Assert.Equal("(System.String, System.String)", tType.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, model.GetConversion(t).Kind);
            Assert.Equal("(System.String, System.String) t", model.GetSymbolInfo(t).Symbol.ToTestDisplayString());
            Assert.Null(model.GetDeclaredSymbol(t));

            // ... its nested tuple ...
            var nestedTuple = (TupleExpressionSyntax)tuple.Arguments[0].Expression;
            Assert.Equal("(null, null)", nestedTuple.ToString());

            var nestedTupleType = model.GetTypeInfo(nestedTuple);
            Assert.Null(nestedTupleType.Type);
            Assert.Equal("(System.String, System.String)", nestedTupleType.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(nestedTuple).Kind);
            Assert.Null(model.GetSymbolInfo(nestedTuple).Symbol);
            Assert.Equal("(System.String, System.String)", model.GetDeclaredSymbol(nestedTuple).ToTestDisplayString());

            // ... a nested null.
            var nestedNull = nestedTuple.Arguments[0].Expression;
            Assert.Equal("null", nestedNull.ToString());

            var nestedNullType = model.GetTypeInfo(nestedNull);
            Assert.Null(nestedNullType.Type);
            Assert.Equal("System.String", nestedNullType.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitReference, model.GetConversion(nestedNull).Kind);
        }

        [Fact]
        public void TestAllNulls()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write(null == null);
        System.Console.Write((null, null) == (null, null));
        System.Console.Write((null, null) != (null, null));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "TrueTrueFalse");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var nulls = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>();
            foreach (var literal in nulls)
            {
                Assert.Equal("null", literal.ToString());
                var symbol = model.GetTypeInfo(literal);
                Assert.Null(symbol.Type);
                Assert.Null(symbol.ConvertedType);
            }

            var tuples = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>();
            foreach (var tuple in tuples)
            {
                Assert.Equal("(null, null)", tuple.ToString());
                var symbol = model.GetTypeInfo(tuple);
                Assert.Null(symbol.Type);
                Assert.Null(symbol.ConvertedType);
            }
        }

        [Fact]
        public void TestConvertedElementInTypelessTuple()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write((null, 1L) == (null, 2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "False");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var lastLiteral = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Last();
            Assert.Equal("2", lastLiteral.ToString());
            var literalInfo = model.GetTypeInfo(lastLiteral);
            Assert.Equal("System.Int32", literalInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int64", literalInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void TestConvertedElementInTypelessTuple_Nested()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write(((null, 1L), null) == ((null, 2), null));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "False");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var rightTuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(2);
            Assert.Equal("((null, 2), null)", rightTuple.ToString());
            var literalInfo = model.GetTypeInfo(rightTuple);
            Assert.Null(literalInfo.Type);
            Assert.Null(literalInfo.ConvertedType);

            var nestedTuple = (TupleExpressionSyntax)rightTuple.Arguments[0].Expression;
            Assert.Equal("(null, 2)", nestedTuple.ToString());
            var nestedLiteralInfo = model.GetTypeInfo(rightTuple);
            Assert.Null(nestedLiteralInfo.Type);
            Assert.Null(nestedLiteralInfo.ConvertedType);

            var two = nestedTuple.Arguments[1].Expression;
            Assert.Equal("2", two.ToString());
            var twoInfo = model.GetTypeInfo(two);
            Assert.Equal("System.Int32", twoInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int64", twoInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void TestFailedInference()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write((null, null, null, null) == (null, () => { }, Main, (int i) => { int j = 0; return i + j; }));
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,30): error CS0019: Operator '==' cannot be applied to operands of type '<null>' and 'lambda expression'
                //         System.Console.Write((null, null, null, null) == (null, () => { }, Main, (int i) => { int j = 0; return i + j; }));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(null, null, null, null) == (null, () => { }, Main, (int i) => { int j = 0; return i + j; })").WithArguments("==", "<null>", "lambda expression").WithLocation(6, 30),
                // (6,30): error CS0019: Operator '==' cannot be applied to operands of type '<null>' and 'method group'
                //         System.Console.Write((null, null, null, null) == (null, () => { }, Main, (int i) => { int j = 0; return i + j; }));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(null, null, null, null) == (null, () => { }, Main, (int i) => { int j = 0; return i + j; })").WithArguments("==", "<null>", "method group").WithLocation(6, 30),
                // (6,30): error CS0019: Operator '==' cannot be applied to operands of type '<null>' and 'lambda expression'
                //         System.Console.Write((null, null, null, null) == (null, () => { }, Main, (int i) => { int j = 0; return i + j; }));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(null, null, null, null) == (null, () => { }, Main, (int i) => { int j = 0; return i + j; })").WithArguments("==", "<null>", "lambda expression").WithLocation(6, 30)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            // check tuple on the left
            var tuple1 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(0);
            Assert.Equal("(null, null, null, null)", tuple1.ToString());

            var tupleType1 = model.GetTypeInfo(tuple1);
            Assert.Null(tupleType1.Type);
            Assert.Null(tupleType1.ConvertedType);

            // check tuple on the right ...
            var tuple2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            Assert.Equal("(null, () => { }, Main, (int i) => { int j = 0; return i + j; })", tuple2.ToString());

            var tupleType2 = model.GetTypeInfo(tuple2);
            Assert.Null(tupleType2.Type);
            Assert.Null(tupleType2.ConvertedType);

            // ... its first lambda ...
            var firstLambda = tuple2.Arguments[1].Expression;
            Assert.Null(model.GetTypeInfo(firstLambda).Type);
            Assert.Null(model.GetTypeInfo(firstLambda).ConvertedType);

            // ... its method group ...
            var methodGroup = tuple2.Arguments[2].Expression;
            Assert.Null(model.GetTypeInfo(methodGroup).Type);
            Assert.Null(model.GetTypeInfo(methodGroup).ConvertedType);
            Assert.Null(model.GetSymbolInfo(methodGroup).Symbol);
            Assert.Equal(new[] { "void C.Main()" }, model.GetSymbolInfo(methodGroup).CandidateSymbols.Select(s => s.ToTestDisplayString()));

            // ... its second lambda and the symbols it uses
            var secondLambda = tuple2.Arguments[3].Expression;
            Assert.Null(model.GetTypeInfo(secondLambda).Type);
            Assert.Null(model.GetTypeInfo(secondLambda).ConvertedType);

            var addition = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Last();
            Assert.Equal("i + j", addition.ToString());

            var i = addition.Left;
            Assert.Equal("System.Int32 i", model.GetSymbolInfo(i).Symbol.ToTestDisplayString());

            var j = addition.Right;
            Assert.Equal("System.Int32 j", model.GetSymbolInfo(j).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestVoidTypeElement()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write((Main(), null) != (null, Main()));
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,31): error CS8210: A tuple may not contain a value of type 'void'.
                //         System.Console.Write((Main(), null) != (null, Main()));
                Diagnostic(ErrorCode.ERR_VoidInTuple, "Main()").WithLocation(6, 31),
                // (6,55): error CS8210: A tuple may not contain a value of type 'void'.
                //         System.Console.Write((Main(), null) != (null, Main()));
                Diagnostic(ErrorCode.ERR_VoidInTuple, "Main()").WithLocation(6, 55)
                );
        }

        [Fact]
        public void TestFailedConversion()
        {
            var source = @"
class C
{
    static void M(string s)
    {
        System.Console.Write((s, s) == (1, () => { }));
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,30): error CS0019: Operator '==' cannot be applied to operands of type 'string' and 'int'
                //         System.Console.Write((s, s) == (1, () => { }));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(s, s) == (1, () => { })").WithArguments("==", "string", "int").WithLocation(6, 30),
                // (6,30): error CS0019: Operator '==' cannot be applied to operands of type 'string' and 'lambda expression'
                //         System.Console.Write((s, s) == (1, () => { }));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(s, s) == (1, () => { })").WithArguments("==", "string", "lambda expression").WithLocation(6, 30)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var tuple1 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(0);
            Assert.Equal("(s, s)", tuple1.ToString());
            var tupleType1 = model.GetTypeInfo(tuple1);
            Assert.Equal("(System.String, System.String)", tupleType1.Type.ToTestDisplayString());
            Assert.Equal("(System.String, System.String)", tupleType1.ConvertedType.ToTestDisplayString());

            var tuple2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            Assert.Equal("(1, () => { })", tuple2.ToString());
            var tupleType2 = model.GetTypeInfo(tuple2);
            Assert.Null(tupleType2.Type);
            Assert.Null(tupleType2.ConvertedType);
        }

        [Fact]
        public void TestDynamic()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        dynamic d1 = 1;
        dynamic d2 = 2;
        System.Console.Write($""{(d1, 2) == (1, d2)} "");
        System.Console.Write($""{(d1, 2) != (1, d2)} "");

        System.Console.Write($""{(d1, 20) == (10, d2)} "");
        System.Console.Write($""{(d1, 20) != (10, d2)} "");
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True False False True");
        }

        [Fact]
        public void TestDynamicWithConstants()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        System.Console.Write($""{((dynamic)true, (dynamic)false) == ((dynamic)true, (dynamic)false)} "");
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");
        }

        [Fact]
        public void TestDynamic_WithTypelessExpression()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        dynamic d1 = 1;
        dynamic d2 = 2;
        System.Console.Write((d1, 2) == (() => 1, d2));
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (8,30): error CS0019: Operator '==' cannot be applied to operands of type 'dynamic' and 'lambda expression'
                //         System.Console.Write((d1, 2) == (() => 1, d2));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(d1, 2) == (() => 1, d2)").WithArguments("==", "dynamic", "lambda expression").WithLocation(8, 30)
                );
        }

        [Fact]
        public void TestDynamic_WithBooleanConstants()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        System.Console.Write(((dynamic)true, (dynamic)false) == (true, false));
        System.Console.Write(((dynamic)true, (dynamic)false) != (true, false));
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "TrueFalse");
        }

        [Fact]
        public void TestDynamic_WithBadType()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        dynamic d1 = 1;
        dynamic d2 = 2;

        try
        {
            bool b = ((d1, 2) == (""hello"", d2));
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
        {
            System.Console.Write(e.Message);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Operator '==' cannot be applied to operands of type 'int' and 'string'");
        }

        [Fact]
        public void TestDynamic_WithNull()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        dynamic d1 = null;
        dynamic d2 = null;
        System.Console.Write((d1, null) == (null, d2));
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var tuple1 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(0);
            Assert.Equal("(d1, null)", tuple1.ToString());
            var tupleType1 = model.GetTypeInfo(tuple1);
            Assert.Null(tupleType1.Type);
            Assert.Equal("(dynamic d1, dynamic)", tupleType1.ConvertedType.ToTestDisplayString());

            var tuple2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            Assert.Equal("(null, d2)", tuple2.ToString());
            var tupleType2 = model.GetTypeInfo(tuple2);
            Assert.Null(tupleType2.Type);
            Assert.Equal("(dynamic, dynamic d2)", tupleType2.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void TestBadConstraintOnTuple()
        {
            // https://github.com/dotnet/roslyn/issues/37121 : This test appears to produce a duplicate diagnostic at (6, 35)
            var source = @"
ref struct S
{
    void M(S s1, S s2)
    {
        System.Console.Write(("""", s1) == (null, s2));
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,35): error CS0306: The type 'S' may not be used as a type argument
                //         System.Console.Write(("", s1) == (null, s2));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "s1").WithArguments("S").WithLocation(6, 35),
                // (6,30): error CS0019: Operator '==' cannot be applied to operands of type 'S' and 'S'
                //         System.Console.Write(("", s1) == (null, s2));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, @"("""", s1) == (null, s2)").WithArguments("==", "S", "S").WithLocation(6, 30),
                // (6,35): error CS0306: The type 'S' may not be used as a type argument
                //         System.Console.Write(("", s1) == (null, s2));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "s1").WithArguments("S").WithLocation(6, 35),
                // (6,49): error CS0306: The type 'S' may not be used as a type argument
                //         System.Console.Write(("", s1) == (null, s2));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "s2").WithArguments("S").WithLocation(6, 49)
                );
        }

        [Fact]
        public void TestErrorInTuple()
        {
            var source = @"
public class C
{
    public void M()
    {
        if (error1 == (error2, 3)) { }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,13): error CS0103: The name 'error1' does not exist in the current context
                //         if (error1 == (error2, 3)) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "error1").WithArguments("error1").WithLocation(6, 13),
                // (6,24): error CS0103: The name 'error2' does not exist in the current context
                //         if (error1 == (error2, 3)) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "error2").WithArguments("error2").WithLocation(6, 24)
                );
        }

        [Fact]
        public void TestWithTypelessTuple()
        {
            var source = @"
public class C
{
    public void M()
    {
        var t = (null, null);
        if (null == (() => {}) ) {}
        if ("""" == 1) {}
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,13): error CS0815: Cannot assign (<null>, <null>) to an implicitly-typed variable
                //         var t = (null, null);
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "t = (null, null)").WithArguments("(<null>, <null>)").WithLocation(6, 13),
                // (7,13): error CS0019: Operator '==' cannot be applied to operands of type '<null>' and 'lambda expression'
                //         if (null == (() => {}) ) {}
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "null == (() => {})").WithArguments("==", "<null>", "lambda expression").WithLocation(7, 13),
                // (8,13): error CS0019: Operator '==' cannot be applied to operands of type 'string' and 'int'
                //         if ("" == 1) {}
                Diagnostic(ErrorCode.ERR_BadBinaryOps, @""""" == 1").WithArguments("==", "string", "int").WithLocation(8, 13)
                );
        }

        [Fact]
        public void TestTupleEqualityPreferredOverCustomOperator()
        {
            var source = @"
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public static bool operator ==(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
            => throw null;
        public static bool operator !=(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
            => throw null;

        public override bool Equals(object o)
            => throw null;
        public override int GetHashCode()
            => throw null;
    }
}
public class C
{
    public static void Main()
    {
        var t1 = (1, 1);
        var t2 = (2, 2);
        System.Console.Write(t1 == t2);
        System.Console.Write(t1 != t2);
    }
}
";
            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            // Note: tuple equality picked ahead of custom operator== (small compat break)
            CompileAndVerify(comp, expectedOutput: "FalseTrue");
        }

        [Fact]
        public void TestCustomOperatorPlusAllowed()
        {
            var source = @"
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public static ValueTuple<T1, T2> operator +(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
            => (default(T1), default(T2));

        public override string ToString()
            => $""({Item1}, {Item2})"";
    }
}
public class C
{
    public static void Main()
    {
        var t1 = (0, 1);
        var t2 = (2, 3);
        System.Console.Write(t1 + t2);
    }
}
";
            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(0, 0)");
        }

        [Fact]
        void TestTupleEqualityPreferredOverCustomOperator_Nested()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        System.Console.Write( (1, 2, (3, 4)) == (1, 2, (3, 4)) );
    }
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public static bool operator ==(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
            => throw null;
        public static bool operator !=(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
            => throw null;

        public override bool Equals(object o)
            => throw null;

        public override int GetHashCode()
            => throw null;
    }
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
    }
}
";

            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            // Note: tuple equality picked ahead of custom operator==
            CompileAndVerify(comp, expectedOutput: "True");
        }

        [Fact]
        public void TestNaN()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        var t1 = (System.Double.NaN, 1);
        var t2 = (System.Double.NaN, 1);
        System.Console.Write($""{t1 == t2} {t1.Equals(t2)} {t1 != t2} {t1 == (System.Double.NaN, 1)}"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "False True True False");
        }

        [Fact]
        public void TestTopLevelDynamic()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        dynamic d1 = (1, 1);

        try
        {
            try
            {
                _ = d1 == (1, 1);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                System.Console.WriteLine(e.Message);
            }

            try
            {
                _ = d1 != (1, 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                System.Console.WriteLine(e.Message);
            }
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"Operator '==' cannot be applied to operands of type 'System.ValueTuple<int,int>' and 'System.ValueTuple<int,int>'
Operator '!=' cannot be applied to operands of type 'System.ValueTuple<int,int>' and 'System.ValueTuple<int,int>'");
        }

        [Fact]
        public void TestNestedDynamic()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        dynamic d1 = (1, 1, 1);

        try
        {
            try
            {
                _ = (2, d1) == (2, (1, 1, 1));
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                System.Console.WriteLine(e.Message);
            }

            try
            {
                _ = (3, d1) != (3, (1, 2, 3));
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                System.Console.WriteLine(e.Message);
            }
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"Operator '==' cannot be applied to operands of type 'System.ValueTuple<int,int,int>' and 'System.ValueTuple<int,int,int>'
Operator '!=' cannot be applied to operands of type 'System.ValueTuple<int,int,int>' and 'System.ValueTuple<int,int,int>'");
        }

        [Fact]
        public void TestComparisonWithDeconstructionResult()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        var b1 = (1, 2) == ((_, _) = new C());
        var b2 = (1, 42) != ((_, _) = new C());
        var b3 = (1, 42) == ((_, _) = new C()); // false
        var b4 = ((_, _) = new C()) == (1, 2);
        var b5 = ((_, _) = new C()) != (1, 42);
        var b6 = ((_, _) = new C()) == (1, 42); // false
        System.Console.Write($""{b1} {b2} {b3} {b4} {b5} {b6}"");
    }
    public void Deconstruct(out int x, out int y)
    {
        x = 1;
        y = 2;
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: @"True True False True True False");
        }

        [Fact]
        public void TestComparisonWithDeconstruction()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        var b1 = (1, 2) == new C();
    }
    public void Deconstruct(out int x, out int y)
    {
        x = 1;
        y = 2;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,18): error CS0019: Operator '==' cannot be applied to operands of type '(int, int)' and 'C'
                //         var b1 = (1, 2) == new C();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(1, 2) == new C()").WithArguments("==", "(int, int)", "C").WithLocation(6, 18)
                );
        }

        [Fact]
        public void TestEvaluationOrderOnTupleLiteral()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        System.Console.Write($""{EXPRESSION}"");
    }
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            throw null;
        }
    }
}
public class Base
{
    public int I;
    public Base(int i) { I = i; }
}
public class A : Base
{
    public A(int i) : base(i)
    {
        System.Console.Write($""A:{i}, "");
    }
    public static bool operator ==(A a, Y y)
    {
        System.Console.Write($""A({a.I}) == Y({y.I}), "");
        return a.I == y.I;
    }
    public static bool operator !=(A a, Y y)
    {
        System.Console.Write($""A({a.I}) != Y({y.I}), "");
        return a.I != y.I;
    }
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
public class X : Base
{
    public X(int i) : base(i)
    {
        System.Console.Write($""X:{i}, "");
    }
}
public class Y : Base
{
    public Y(int i) : base(i)
    {
        System.Console.Write($""Y:{i}, "");
    }
    public static implicit operator Y(X x)
    {
        System.Console.Write(""X -> "");
        return new Y(x.I);
    }
}
";

            validate("(new A(1), new A(2)) == (new X(1), new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A(1) == Y(1), A(2) == Y(2), True");
            validate("(new A(1), new A(2)) == (new X(30), new Y(40))", "A:1, A:2, X:30, Y:40, X -> Y:30, A(1) == Y(30), False");
            validate("(new A(1), new A(2)) == (new X(1), new Y(50))", "A:1, A:2, X:1, Y:50, X -> Y:1, A(1) == Y(1), A(2) == Y(50), False");

            validate("(new A(1), new A(2)) != (new X(1), new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A(1) != Y(1), A(2) != Y(2), False");
            validate("(new A(1), new A(2)) != (new Y(1), new X(2))", "A:1, A:2, Y:1, X:2, A(1) != Y(1), X -> Y:2, A(2) != Y(2), False");

            validate("(new A(1), new A(2)) != (new X(30), new Y(40))", "A:1, A:2, X:30, Y:40, X -> Y:30, A(1) != Y(30), True");
            validate("(new A(1), new A(2)) != (new X(50), new Y(2))", "A:1, A:2, X:50, Y:2, X -> Y:50, A(1) != Y(50), True");
            validate("(new A(1), new A(2)) != (new X(1), new Y(60))", "A:1, A:2, X:1, Y:60, X -> Y:1, A(1) != Y(1), A(2) != Y(60), True");

            void validate(string expression, string expected)
            {
                var comp = CreateCompilation(source.Replace("EXPRESSION", expression), options: TestOptions.DebugExe);
                comp.VerifyDiagnostics();
                CompileAndVerify(comp, expectedOutput: expected);
            }
        }

        [Fact]
        public void TestEvaluationOrderOnTupleType()
        {
            var source = @"
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            System.Console.Write(""ValueTuple2, "");
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
    public struct ValueTuple<T1, T2, T3>
    {
        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            // ValueTuple'3 required (constructed in bound tree), but not emitted
            throw null;
        }
    }
}
public class Base
{
    public int I;
    public Base(int i) { I = i; }
}
public class A : Base
{
    public A(int i) : base(i)
    {
        System.Console.Write($""A:{i}, "");
    }
    public static bool operator ==(A a, Y y)
    {
        System.Console.Write($""A({a.I}) == Y({y.I}), "");
        return true;
    }
    public static bool operator !=(A a, Y y)
        => throw null;
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
public class X : Base
{
    public X(int i) : base(i)
    {
        System.Console.Write($""X:{i}, "");
    }
}
public class Y : Base
{
    public Y(int i) : base(i)
    {
        System.Console.Write($""Y:{i}, "");
    }
    public static implicit operator Y(X x)
    {
        System.Console.Write(""X -> "");
        return new Y(x.I);
    }
}
public class C
{
    public static void Main()
    {
        System.Console.Write($""{(new A(1), GetTuple(), new A(4)) == (new X(5), (new X(6), new Y(7)), new Y(8))}"");
    }
    public static (A, A) GetTuple()
    {
        System.Console.Write($""GetTuple, "");
        return (new A(30), new A(40));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "A:1, GetTuple, A:30, A:40, ValueTuple2, A:4, X:5, X:6, Y:7, Y:8, X -> Y:5, A(1) == Y(5), X -> Y:6, A(30) == Y(6), A(40) == Y(7), A(4) == Y(8), True");
        }

        [Fact]
        public void TestConstrainedValueTuple()
        {
            var source = @"
class C
{
    void M()
    {
        _ = (this, this) == (0, 1); // constraint violated by tuple in source
        _ = (this, this) == (this, this); // constraint violated by converted tuple
    }
    public static bool operator ==(C c, int i)
        => throw null;
    public static bool operator !=(C c, int i)
        => throw null;
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
    public static implicit operator int(C c)
        => throw null;
}
namespace System
{
    public struct ValueTuple<T1, T2>
        where T1 : class
        where T2 : class
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            throw null;
        }
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,30): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T1' in the generic type or method 'ValueTuple<T1, T2>'
                //         _ = (this, this) == (0, 1); // constraint violated by tuple in source
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "0").WithArguments("System.ValueTuple<T1, T2>", "T1", "int").WithLocation(6, 30),
                // (6,33): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T2' in the generic type or method 'ValueTuple<T1, T2>'
                //         _ = (this, this) == (0, 1); // constraint violated by tuple in source
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "1").WithArguments("System.ValueTuple<T1, T2>", "T2", "int").WithLocation(6, 33),
                // (6,30): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T1' in the generic type or method 'ValueTuple<T1, T2>'
                //         _ = (this, this) == (0, 1); // constraint violated by tuple in source
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "0").WithArguments("System.ValueTuple<T1, T2>", "T1", "int").WithLocation(6, 30),
                // (6,33): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T2' in the generic type or method 'ValueTuple<T1, T2>'
                //         _ = (this, this) == (0, 1); // constraint violated by tuple in source
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "1").WithArguments("System.ValueTuple<T1, T2>", "T2", "int").WithLocation(6, 33),
                // (7,30): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T1' in the generic type or method 'ValueTuple<T1, T2>'
                //         _ = (this, this) == (this, this); // constraint violated by converted tuple
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "this").WithArguments("System.ValueTuple<T1, T2>", "T1", "int").WithLocation(7, 30),
                // (7,36): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T2' in the generic type or method 'ValueTuple<T1, T2>'
                //         _ = (this, this) == (this, this); // constraint violated by converted tuple
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "this").WithArguments("System.ValueTuple<T1, T2>", "T2", "int").WithLocation(7, 36)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            // check the int tuple
            var firstEquals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().First();
            var intTuple = firstEquals.Right;
            Assert.Equal("(0, 1)", intTuple.ToString());
            var intTupleType = model.GetTypeInfo(intTuple);
            Assert.Equal("(System.Int32, System.Int32)", intTupleType.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)", intTupleType.ConvertedType.ToTestDisplayString());

            // check the last tuple
            var secondEquals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Last();
            var lastTuple = secondEquals.Right;
            Assert.Equal("(this, this)", lastTuple.ToString());
            var lastTupleType = model.GetTypeInfo(lastTuple);
            Assert.Equal("(C, C)", lastTupleType.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)", lastTupleType.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void TestConstrainedNullable()
        {
            var source = @"
class C
{
    void M((int, int)? t1, (long, long)? t2)
    {
        _ = t1 == t2;
    }
}
public interface IInterface { }
namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Int64 { }
    public struct Nullable<T> where T : struct, IInterface { public T GetValueOrDefault() => default(T); }

    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            throw null;
        }
    }
}
";

            var comp = CreateEmptyCompilation(source);
            comp.VerifyDiagnostics(
                // (4,24): error CS0315: The type '(int, int)' cannot be used as type parameter 'T' in the generic type or method 'Nullable<T>'. There is no boxing conversion from '(int, int)' to 'IInterface'.
                //     void M((int, int)? t1, (long, long)? t2)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "t1").WithArguments("System.Nullable<T>", "IInterface", "T", "(int, int)").WithLocation(4, 24),
                // (4,42): error CS0315: The type '(long, long)' cannot be used as type parameter 'T' in the generic type or method 'Nullable<T>'. There is no boxing conversion from '(long, long)' to 'IInterface'.
                //     void M((int, int)? t1, (long, long)? t2)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "t2").WithArguments("System.Nullable<T>", "IInterface", "T", "(long, long)").WithLocation(4, 42)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            // check t1
            var equals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Last();
            var t1 = equals.Left;
            Assert.Equal("t1", t1.ToString());
            var t1Type = model.GetTypeInfo(t1);
            Assert.Equal("(System.Int32, System.Int32)?", t1Type.Type.ToTestDisplayString());
            Assert.Equal("(System.Int64, System.Int64)?", t1Type.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void TestEvaluationOrderOnTupleType2()
        {
            var source = @"
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            System.Console.Write(""ValueTuple2, "");
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
    public struct ValueTuple<T1, T2, T3>
    {
        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            // ValueTuple'3 required (constructed in bound tree), but not emitted
            throw null;
        }
    }
}
public class Base
{
    public int I;
    public Base(int i) { I = i; }
}
public class A : Base
{
    public A(int i) : base(i)
    {
        System.Console.Write($""A:{i}, "");
    }
    public static bool operator ==(A a, Y y)
    {
        System.Console.Write($""A({a.I}) == Y({y.I}), "");
        return true;
    }
    public static bool operator !=(A a, Y y)
        => throw null;
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
public class X : Base
{
    public X(int i) : base(i)
    {
        System.Console.Write($""X:{i}, "");
    }
}
public class Y : Base
{
    public Y(int i) : base(i)
    {
        System.Console.Write($""Y:{i}, "");
    }
    public static implicit operator Y(X x)
    {
        System.Console.Write($""X:{x.I} -> "");
        return new Y(x.I);
    }
}
public class C
{
    public static void Main()
    {
        System.Console.Write($""{(new A(1), (new A(2), new A(3)), new A(4)) == (new X(5), GetTuple(), new Y(8))}"");
    }
    public static (X, Y) GetTuple()
    {
        System.Console.Write($""GetTuple, "");
        return (new X(6), new Y(7));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: @"A:1, A:2, A:3, A:4, X:5, GetTuple, X:6, Y:7, ValueTuple2, Y:8, X:5 -> Y:5, A(1) == Y(5), X:6 -> Y:6, A(2) == Y(6), A(3) == Y(7), A(4) == Y(8), True");
        }

        [Fact]
        public void TestEvaluationOrderOnTupleType3()
        {
            var source = @"
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            System.Console.Write(""ValueTuple2, "");
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
    public struct ValueTuple<T1, T2, T3>
    {
        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            // ValueTuple'3 required (constructed in bound tree), but not emitted
            throw null;
        }
    }
}
public class Base
{
    public int I;
    public Base(int i) { I = i; }
}
public class A : Base
{
    public A(int i) : base(i)
    {
        System.Console.Write($""A:{i}, "");
    }
    public static bool operator ==(A a, Y y)
    {
        System.Console.Write($""A({a.I}) == Y({y.I}), "");
        return true;
    }
    public static bool operator !=(A a, Y y)
        => throw null;
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
public class X : Base
{
    public X(int i) : base(i)
    {
        System.Console.Write($""X:{i}, "");
    }
}
public class Y : Base
{
    public Y(int i) : base(i)
    {
        System.Console.Write($""Y:{i}, "");
    }
    public static implicit operator Y(X x)
    {
        System.Console.Write(""X -> "");
        return new Y(x.I);
    }
}
public class C
{
    public static void Main()
    {
        System.Console.Write($""{GetTuple() == (new X(6), new Y(7))}"");
    }
    public static (A, A) GetTuple()
    {
        System.Console.Write($""GetTuple, "");
        return (new A(30), new A(40));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "GetTuple, A:30, A:40, ValueTuple2, X:6, Y:7, X -> Y:6, A(30) == Y(6), A(40) == Y(7), True");
        }

        [Fact]
        public void TestObsoleteEqualityOperator()
        {
            var source = @"
class C
{
    void M()
    {
        System.Console.WriteLine($""{(new A(), new A()) == (new X(), new Y())}"");
        System.Console.WriteLine($""{(new A(), new A()) != (new X(), new Y())}"");
    }
}
public class A
{
    [System.Obsolete(""obsolete"", true)]
    public static bool operator ==(A a, Y y)
        => throw null;
    [System.Obsolete(""obsolete too"", true)]
    public static bool operator !=(A a, Y y)
        => throw null;
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
public class X
{
}
public class Y
{
    public static implicit operator Y(X x)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,37): error CS0619: 'A.operator ==(A, Y)' is obsolete: 'obsolete'
                //         System.Console.WriteLine($"{(new A(), new A()) == (new X(), new Y())}");
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "(new A(), new A()) == (new X(), new Y())").WithArguments("A.operator ==(A, Y)", "obsolete").WithLocation(6, 37),
                // (6,37): error CS0619: 'A.operator ==(A, Y)' is obsolete: 'obsolete'
                //         System.Console.WriteLine($"{(new A(), new A()) == (new X(), new Y())}");
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "(new A(), new A()) == (new X(), new Y())").WithArguments("A.operator ==(A, Y)", "obsolete").WithLocation(6, 37),
                // (7,37): error CS0619: 'A.operator !=(A, Y)' is obsolete: 'obsolete too'
                //         System.Console.WriteLine($"{(new A(), new A()) != (new X(), new Y())}");
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "(new A(), new A()) != (new X(), new Y())").WithArguments("A.operator !=(A, Y)", "obsolete too").WithLocation(7, 37),
                // (7,37): error CS0619: 'A.operator !=(A, Y)' is obsolete: 'obsolete too'
                //         System.Console.WriteLine($"{(new A(), new A()) != (new X(), new Y())}");
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "(new A(), new A()) != (new X(), new Y())").WithArguments("A.operator !=(A, Y)", "obsolete too").WithLocation(7, 37)
                );
        }

        [Fact]
        public void TestDefiniteAssignment()
        {
            var source = @"
class C
{
    void M()
    {
        int error1;
        System.Console.Write((1, 2) == (error1, 2));

        int error2;
        System.Console.Write((1, (error2, 3)) == (1, (2, 3)));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,41): error CS0165: Use of unassigned local variable 'error1'
                //         System.Console.Write((1, 2) == (error1, 2));
                Diagnostic(ErrorCode.ERR_UseDefViolation, "error1").WithArguments("error1").WithLocation(7, 41),
                // (10,35): error CS0165: Use of unassigned local variable 'error2'
                //         System.Console.Write((1, (error2, 3)) == (1, (2, 3)));
                Diagnostic(ErrorCode.ERR_UseDefViolation, "error2").WithArguments("error2").WithLocation(10, 35)
                );
        }

        [Fact]
        public void TestDefiniteAssignment2()
        {
            var source = @"
class C
{
    int M(out int x)
    {
        _ = (M(out int y), y) == (1, 2); // ok
        _ = (z, M(out int z)) == (1, 2); // error
        x = 1;
        return 2;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                    // (7,14): error CS0841: Cannot use local variable 'z' before it is declared
                    //         _ = (z, M(out int z)) == (1, 2); // error
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "z").WithArguments("z").WithLocation(7, 14)
                );
        }

        [Fact]
        public void TestEqualityOfTypeConvertingToTuple()
        {
            var source = @"
class C
{
    private int i;
    void M()
    {
        System.Console.Write(this == (1, 1));
        System.Console.Write((1, 1) == this);
    }
    public static implicit operator (int, int)(C c)
    {
        return (c.i, c.i);
    }
    C(int i) { this.i = i; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,30): error CS0019: Operator '==' cannot be applied to operands of type 'C' and '(int, int)'
                //         System.Console.Write(this == (1, 1));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "this == (1, 1)").WithArguments("==", "C", "(int, int)").WithLocation(7, 30),
                // (8,30): error CS0019: Operator '==' cannot be applied to operands of type '(int, int)' and 'C'
                //         System.Console.Write((1, 1) == this);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(1, 1) == this").WithArguments("==", "(int, int)", "C").WithLocation(8, 30)
                );
        }

        [Fact]
        public void TestEqualityOfTypeConvertingFromTuple()
        {
            var source = @"
class C
{
    private int i;
    public static void Main()
    {
        var c = new C(2);
        System.Console.Write(c == (1, 1));
        System.Console.Write((1, 1) == c);
    }
    public static implicit operator C((int, int) x)
    {
        return new C(x.Item1 + x.Item2);
    }
    public static bool operator ==(C c1, C c2)
        => c1.i == c2.i;
    public static bool operator !=(C c1, C c2)
        => throw null;
    public override int GetHashCode()
        => throw null;
    public override bool Equals(object other)
        => throw null;
    C(int i) { this.i = i; }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "TrueTrue");
        }

        [Fact]
        public void TestEqualityOfTypeComparableWithTuple()
        {
            var source = @"
class C
{
    private static void Main()
    {
        System.Console.Write(new C() == (1, 1));
        System.Console.Write(new C() != (1, 1));
    }
    public static bool operator ==(C c, (int, int) t)
    {
        return t.Item1 + t.Item2 == 2;
    }
    public static bool operator !=(C c, (int, int) t)
    {
        return t.Item1 + t.Item2 != 2;
    }
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "TrueFalse");
        }

        [Fact]
        public void TestOfTwoUnrelatedTypes()
        {
            var source = @"
class A { }
class C
{
    static void M()
    {
        System.Console.Write(new C() == new A());
        System.Console.Write((1, new C()) == (1, new A()));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,30): error CS0019: Operator '==' cannot be applied to operands of type 'C' and 'A'
                //         System.Console.Write(new C() == new A());
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "new C() == new A()").WithArguments("==", "C", "A").WithLocation(7, 30),
                // (8,30): error CS0019: Operator '==' cannot be applied to operands of type 'C' and 'A'
                //         System.Console.Write((1, new C()) == (1, new A()));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(1, new C()) == (1, new A())").WithArguments("==", "C", "A").WithLocation(8, 30)
                );
        }

        [Fact]
        public void TestOfTwoUnrelatedTypes2()
        {
            var source = @"
class A { }
class C
{
    static void M(string s, System.Exception e)
    {
        System.Console.Write(s == 3);
        System.Console.Write((1, s) == (1, e));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,30): error CS0019: Operator '==' cannot be applied to operands of type 'string' and 'int'
                //         System.Console.Write(s == 3);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s == 3").WithArguments("==", "string", "int").WithLocation(7, 30),
                // (8,30): error CS0019: Operator '==' cannot be applied to operands of type 'string' and 'Exception'
                //         System.Console.Write((1, s) == (1, e));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(1, s) == (1, e)").WithArguments("==", "string", "System.Exception").WithLocation(8, 30)
                );
        }

        [Fact]
        public void TestBadRefCompare()
        {
            var source = @"
class C
{
    static void M()
    {
        string s = ""11"";
        object o = s + s;
        (object, object) t = default;

        bool b = o == s;
        bool b2 = t == (s, s); // no warning
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,18): warning CS0252: Possible unintended reference comparison; to get a value comparison, cast the left hand side to type 'string'
                //         bool b = o == s;
                Diagnostic(ErrorCode.WRN_BadRefCompareLeft, "o == s").WithArguments("string").WithLocation(10, 18)
                );
        }

        [Fact, WorkItem(27047, "https://github.com/dotnet/roslyn/issues/27047")]
        public void TestWithObsoleteImplicitConversion()
        {
            var source = @"
class C
{
    private static bool TupleEquals((C, int)? nt1, (int, C) nt2)
        => nt1 == nt2; // warn 1 and 2

    private static bool TupleNotEquals((C, int)? nt1, (int, C) nt2)
        => nt1 != nt2; // warn 3 and 4

    [System.Obsolete(""obsolete"", error: true)]
    public static implicit operator int(C c)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,12): error CS0619: 'C.implicit operator int(C)' is obsolete: 'obsolete'
                //         => nt1 == nt2; // warn 1 and 2
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "nt1").WithArguments("C.implicit operator int(C)", "obsolete").WithLocation(5, 12),
                // (5,19): error CS0619: 'C.implicit operator int(C)' is obsolete: 'obsolete'
                //         => nt1 == nt2; // warn 1 and 2
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "nt2").WithArguments("C.implicit operator int(C)", "obsolete").WithLocation(5, 19),
                // (8,12): error CS0619: 'C.implicit operator int(C)' is obsolete: 'obsolete'
                //         => nt1 != nt2; // warn 3 and 4
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "nt1").WithArguments("C.implicit operator int(C)", "obsolete").WithLocation(8, 12),
                // (8,19): error CS0619: 'C.implicit operator int(C)' is obsolete: 'obsolete'
                //         => nt1 != nt2; // warn 3 and 4
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "nt2").WithArguments("C.implicit operator int(C)", "obsolete").WithLocation(8, 19)
                );
        }

        [Fact, WorkItem(27047, "https://github.com/dotnet/roslyn/issues/27047")]
        public void TestWithObsoleteBoolConversion()
        {
            var source = @"
public class A
{
    public static bool TupleEquals((A, A) t) => t == t; // warn 1 and 2
    public static bool TupleNotEquals((A, A) t) => t != t; // warn 3 and 4

    public static NotBool operator ==(A a1, A a2) => throw null;
    public static NotBool operator !=(A a1, A a2) => throw null;
    public override bool Equals(object o) => throw null;
    public override int GetHashCode() => throw null;
}
public class NotBool
{
    [System.Obsolete(""obsolete"", error: true)]
    public static implicit operator bool(NotBool b) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,49): error CS0619: 'NotBool.implicit operator bool(NotBool)' is obsolete: 'obsolete'
                //     public static bool TupleEquals((A, A) t) => t == t; // warn 1 and 2
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t == t").WithArguments("NotBool.implicit operator bool(NotBool)", "obsolete").WithLocation(4, 49),
                // (4,49): error CS0619: 'NotBool.implicit operator bool(NotBool)' is obsolete: 'obsolete'
                //     public static bool TupleEquals((A, A) t) => t == t; // warn 1 and 2
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t == t").WithArguments("NotBool.implicit operator bool(NotBool)", "obsolete").WithLocation(4, 49),
                // (5,52): error CS0619: 'NotBool.implicit operator bool(NotBool)' is obsolete: 'obsolete'
                //     public static bool TupleNotEquals((A, A) t) => t != t; // warn 3 and 4
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t != t").WithArguments("NotBool.implicit operator bool(NotBool)", "obsolete").WithLocation(5, 52),
                // (5,52): error CS0619: 'NotBool.implicit operator bool(NotBool)' is obsolete: 'obsolete'
                //     public static bool TupleNotEquals((A, A) t) => t != t; // warn 3 and 4
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t != t").WithArguments("NotBool.implicit operator bool(NotBool)", "obsolete").WithLocation(5, 52)
                );
        }

        [Fact, WorkItem(27047, "https://github.com/dotnet/roslyn/issues/27047")]
        public void TestWithObsoleteComparisonOperators()
        {
            var source = @"
public class A
{
    public static bool TupleEquals((A, A) t) => t == t; // warn 1 and 2
    public static bool TupleNotEquals((A, A) t) => t != t; // warn 3 and 4

    [System.Obsolete("""", error: true)]
    public static bool operator ==(A a1, A a2) => throw null;
    [System.Obsolete("""", error: true)]
    public static bool operator !=(A a1, A a2) => throw null;

    public override bool Equals(object o) => throw null;
    public override int GetHashCode() => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,49): error CS0619: 'A.operator ==(A, A)' is obsolete: ''
                //     public static bool TupleEquals((A, A) t) => t == t; // warn 1 and 2
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t == t").WithArguments("A.operator ==(A, A)", "").WithLocation(4, 49),
                // (4,49): error CS0619: 'A.operator ==(A, A)' is obsolete: ''
                //     public static bool TupleEquals((A, A) t) => t == t; // warn 1 and 2
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t == t").WithArguments("A.operator ==(A, A)", "").WithLocation(4, 49),
                // (5,52): error CS0619: 'A.operator !=(A, A)' is obsolete: ''
                //     public static bool TupleNotEquals((A, A) t) => t != t; // warn 3 and 4
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t != t").WithArguments("A.operator !=(A, A)", "").WithLocation(5, 52),
                // (5,52): error CS0619: 'A.operator !=(A, A)' is obsolete: ''
                //     public static bool TupleNotEquals((A, A) t) => t != t; // warn 3 and 4
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t != t").WithArguments("A.operator !=(A, A)", "").WithLocation(5, 52)
                );
        }

        [Fact, WorkItem(27047, "https://github.com/dotnet/roslyn/issues/27047")]
        public void TestWithObsoleteTruthOperators()
        {
            var source = @"
public class A
{
    public static bool TupleEquals((A, A) t) => t == t; // warn 1 and 2
    public static bool TupleNotEquals((A, A) t) => t != t; // warn 3 and 4

    public static NotBool operator ==(A a1, A a2) => throw null;
    public static NotBool operator !=(A a1, A a2) => throw null;
    public override bool Equals(object o) => throw null;
    public override int GetHashCode() => throw null;
}
public class NotBool
{
    [System.Obsolete(""obsolete"", error: true)]
    public static bool operator true(NotBool b) => throw null;

    [System.Obsolete(""obsolete"", error: true)]
    public static bool operator false(NotBool b) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,49): error CS0619: 'NotBool.operator false(NotBool)' is obsolete: 'obsolete'
                //     public static bool TupleEquals((A, A) t) => t == t; // warn 1 and 2
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t == t").WithArguments("NotBool.operator false(NotBool)", "obsolete").WithLocation(4, 49),
                // (4,49): error CS0619: 'NotBool.operator false(NotBool)' is obsolete: 'obsolete'
                //     public static bool TupleEquals((A, A) t) => t == t; // warn 1 and 2
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t == t").WithArguments("NotBool.operator false(NotBool)", "obsolete").WithLocation(4, 49),
                // (5,52): error CS0619: 'NotBool.operator true(NotBool)' is obsolete: 'obsolete'
                //     public static bool TupleNotEquals((A, A) t) => t != t; // warn 3 and 4
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t != t").WithArguments("NotBool.operator true(NotBool)", "obsolete").WithLocation(5, 52),
                // (5,52): error CS0619: 'NotBool.operator true(NotBool)' is obsolete: 'obsolete'
                //     public static bool TupleNotEquals((A, A) t) => t != t; // warn 3 and 4
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "t != t").WithArguments("NotBool.operator true(NotBool)", "obsolete").WithLocation(5, 52)
                );
        }

        [Fact]
        public void TestEqualOnNullableVsNullableTuples()
        {
            var source = @"
class C
{
    public static void Main()
    {
        Compare(null, null);
        Compare(null, (1, 2));
        Compare((2, 3), null);
        Compare((4, 4), (4, 4));
        Compare((5, 5), (10, 10));
    }
    private static void Compare((int, int)? nt1, (int, int)? nt2)
    {
        System.Console.Write($""{nt1 == nt2} "");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "True False False True False");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var comparison = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
            var nt1 = comparison.Left;
            var nt1Type = model.GetTypeInfo(nt1);
            Assert.Equal("nt1", nt1.ToString());
            Assert.Equal("(System.Int32, System.Int32)?", nt1Type.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)?", nt1Type.ConvertedType.ToTestDisplayString());

            var nt2 = comparison.Right;
            var nt2Type = model.GetTypeInfo(nt2);
            Assert.Equal("nt2", nt2.ToString());
            Assert.Equal("(System.Int32, System.Int32)?", nt2Type.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)?", nt2Type.ConvertedType.ToTestDisplayString());

            verifier.VerifyIL("C.Compare", @"{
  // Code size      104 (0x68)
  .maxstack  3
  .locals init ((int, int)? V_0,
                (int, int)? V_1,
                bool V_2,
                System.ValueTuple<int, int> V_3,
                System.ValueTuple<int, int> V_4)
  IL_0000:  nop
  IL_0001:  ldstr      ""{0} ""
  IL_0006:  ldarg.0
  IL_0007:  stloc.0
  IL_0008:  ldarg.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""bool (int, int)?.HasValue.get""
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  ldloca.s   V_1
  IL_0015:  call       ""bool (int, int)?.HasValue.get""
  IL_001a:  beq.s      IL_001f
  IL_001c:  ldc.i4.0
  IL_001d:  br.s       IL_0057
  IL_001f:  ldloc.2
  IL_0020:  brtrue.s   IL_0025
  IL_0022:  ldc.i4.1
  IL_0023:  br.s       IL_0057
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""(int, int) (int, int)?.GetValueOrDefault()""
  IL_002c:  stloc.3
  IL_002d:  ldloca.s   V_1
  IL_002f:  call       ""(int, int) (int, int)?.GetValueOrDefault()""
  IL_0034:  stloc.s    V_4
  IL_0036:  ldloc.3
  IL_0037:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_003c:  ldloc.s    V_4
  IL_003e:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0043:  bne.un.s   IL_0056
  IL_0045:  ldloc.3
  IL_0046:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_004b:  ldloc.s    V_4
  IL_004d:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0052:  ceq
  IL_0054:  br.s       IL_0057
  IL_0056:  ldc.i4.0
  IL_0057:  box        ""bool""
  IL_005c:  call       ""string string.Format(string, object)""
  IL_0061:  call       ""void System.Console.Write(string)""
  IL_0066:  nop
  IL_0067:  ret
}");
        }

        [Fact]
        public void TestEqualOnNullableVsNullableTuples_NeverNull()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.Write(((int, int)?) (1, 2) == ((int, int)?) (1, 2));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "True");
            verifier.VerifyIL("C.Main", @"
{
    // Code size       19 (0x13)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  ldc.i4.1
    IL_0003:  bne.un.s   IL_000b
    IL_0005:  ldc.i4.2
    IL_0006:  ldc.i4.2
    IL_0007:  ceq
    IL_0009:  br.s       IL_000c
    IL_000b:  ldc.i4.0
    IL_000c:  call       ""void System.Console.Write(bool)""
    IL_0011:  nop
    IL_0012:  ret
}
");
        }

        [Fact]
        public void TestEqualOnNullableVsNullableTuples_OneSideNeverNull()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.Write(((int, int)?) (1, 2) == (1, 2));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "True");
            verifier.VerifyIL("C.Main", @"
{
    // Code size       19 (0x13)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  ldc.i4.1
    IL_0003:  bne.un.s   IL_000b
    IL_0005:  ldc.i4.2
    IL_0006:  ldc.i4.2
    IL_0007:  ceq
    IL_0009:  br.s       IL_000c
    IL_000b:  ldc.i4.0
    IL_000c:  call       ""void System.Console.Write(bool)""
    IL_0011:  nop
    IL_0012:  ret
}
");
        }

        [Fact]
        public void TestEqualOnNullableVsNullableTuples_Tuple_AlwaysNull_AlwaysNull()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.Write(((int, int)?)null == ((int, int)?)null);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "True");
            verifier.VerifyIL("C.Main", @"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""void System.Console.Write(bool)""
  IL_0007:  nop
  IL_0008:  ret
}
");

            CompileAndVerify(source, options: TestOptions.ReleaseExe).VerifyIL("C.Main", @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""void System.Console.Write(bool)""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void TestEqualOnNullableVsNullableTuples_Tuple_MaybeNull_AlwaysNull()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M(null);
        M((1, 2));
    }
    public static void M((int, int)? t)
    {
        System.Console.Write(t == ((int, int)?)null);
    }
}
";

            CompileAndVerify(source, expectedOutput: "TrueFalse", options: TestOptions.ReleaseExe).VerifyIL("C.M", @"{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init ((int, int)? V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool (int, int)?.HasValue.get""
  IL_0009:  ldc.i4.0
  IL_000a:  ceq
  IL_000c:  call       ""void System.Console.Write(bool)""
  IL_0011:  ret
}
");
        }

        [Fact]
        public void TestNotEqualOnNullableVsNullableTuples_Tuple_MaybeNull_AlwaysNull()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M(null);
        M((1, 2));
    }
    public static void M((int, int)? t)
    {
        System.Console.Write(t != ((int, int)?)null);
    }
}
";

            CompileAndVerify(source, expectedOutput: "FalseTrue", options: TestOptions.ReleaseExe).VerifyIL("C.M", @"{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init ((int, int)? V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool (int, int)?.HasValue.get""
  IL_0009:  call       ""void System.Console.Write(bool)""
  IL_000e:  ret
}
");
        }

        [Fact]
        public void TestNotEqualOnNullableVsNullableTuples_Tuple_AlwaysNull_MaybeNull()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M(null);
        M((1, 2));
    }
    public static void M((int, int)? t)
    {
        System.Console.Write(((int, int)?)null == t);
    }
}
";

            CompileAndVerify(source, expectedOutput: "TrueFalse", options: TestOptions.ReleaseExe).VerifyIL("C.M", @"{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init ((int, int)? V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool (int, int)?.HasValue.get""
  IL_0009:  ldc.i4.0
  IL_000a:  ceq
  IL_000c:  call       ""void System.Console.Write(bool)""
  IL_0011:  ret
}
");
        }

        [Fact]
        public void TestEqualOnNullableVsNullableTuples_Tuple_NeverNull_AlwaysNull()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.Write((1, 2) == ((int, int)?)null);
    }
}
";

            CompileAndVerify(source, expectedOutput: "False", options: TestOptions.ReleaseExe).VerifyIL("C.Main", @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""void System.Console.Write(bool)""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void TestEqualOnNullableVsNullableTuples_Tuple_AlwaysNull_MaybeNull()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M(null);
        M((1, 2));
    }
    public static void M((int, int)? t)
    {
        System.Console.Write(((int, int)?)null == t);
    }
}
";

            CompileAndVerify(source, expectedOutput: "TrueFalse", options: TestOptions.ReleaseExe).VerifyIL("C.M", @"{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init ((int, int)? V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool (int, int)?.HasValue.get""
  IL_0009:  ldc.i4.0
  IL_000a:  ceq
  IL_000c:  call       ""void System.Console.Write(bool)""
  IL_0011:  ret
}
");
        }

        [Fact]
        public void TestEqualOnNullableVsNullableTuples_Tuple_AlwaysNull_NeverNull()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.Write(((int, int)?)null == (1, 2));
    }
}
";

            CompileAndVerify(source, expectedOutput: "False", options: TestOptions.ReleaseExe).VerifyIL("C.Main", @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""void System.Console.Write(bool)""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void TestEqualOnNullableVsNullableTuples_ElementAlwaysNull()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.Write((null, null) == (new int?(), new int?()));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "True");
            verifier.VerifyIL("C.Main", @"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""void System.Console.Write(bool)""
  IL_0007:  nop
  IL_0008:  ret
}
");
        }

        [Fact]
        public void TestNotEqualOnNullableVsNullableTuples()
        {
            var source = @"
class C
{
    public static void Main()
    {
        Compare(null, null);
        Compare(null, (1, 2));
        Compare((2, 3), null);
        Compare((4, 4), (4, 4));
        Compare((5, 5), (10, 10));
    }
    private static void Compare((int, int)? nt1, (int, int)? nt2)
    {
        System.Console.Write($""{nt1 != nt2} "");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "False True True False True");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var comparison = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
            var nt1 = comparison.Left;
            var nt1Type = model.GetTypeInfo(nt1);
            Assert.Equal("nt1", nt1.ToString());
            Assert.Equal("(System.Int32, System.Int32)?", nt1Type.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)?", nt1Type.ConvertedType.ToTestDisplayString());

            var nt2 = comparison.Right;
            var nt2Type = model.GetTypeInfo(nt2);
            Assert.Equal("nt2", nt2.ToString());
            Assert.Equal("(System.Int32, System.Int32)?", nt2Type.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)?", nt2Type.ConvertedType.ToTestDisplayString());

            verifier.VerifyIL("C.Compare", @"{
  // Code size      107 (0x6b)
  .maxstack  3
  .locals init ((int, int)? V_0,
                (int, int)? V_1,
                bool V_2,
                System.ValueTuple<int, int> V_3,
                System.ValueTuple<int, int> V_4)
  IL_0000:  nop
  IL_0001:  ldstr      ""{0} ""
  IL_0006:  ldarg.0
  IL_0007:  stloc.0
  IL_0008:  ldarg.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""bool (int, int)?.HasValue.get""
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  ldloca.s   V_1
  IL_0015:  call       ""bool (int, int)?.HasValue.get""
  IL_001a:  beq.s      IL_001f
  IL_001c:  ldc.i4.1
  IL_001d:  br.s       IL_005a
  IL_001f:  ldloc.2
  IL_0020:  brtrue.s   IL_0025
  IL_0022:  ldc.i4.0
  IL_0023:  br.s       IL_005a
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""(int, int) (int, int)?.GetValueOrDefault()""
  IL_002c:  stloc.3
  IL_002d:  ldloca.s   V_1
  IL_002f:  call       ""(int, int) (int, int)?.GetValueOrDefault()""
  IL_0034:  stloc.s    V_4
  IL_0036:  ldloc.3
  IL_0037:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_003c:  ldloc.s    V_4
  IL_003e:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0043:  bne.un.s   IL_0059
  IL_0045:  ldloc.3
  IL_0046:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_004b:  ldloc.s    V_4
  IL_004d:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0052:  ceq
  IL_0054:  ldc.i4.0
  IL_0055:  ceq
  IL_0057:  br.s       IL_005a
  IL_0059:  ldc.i4.1
  IL_005a:  box        ""bool""
  IL_005f:  call       ""string string.Format(string, object)""
  IL_0064:  call       ""void System.Console.Write(string)""
  IL_0069:  nop
  IL_006a:  ret
}");
        }

        [Fact]
        public void TestNotEqualOnNullableVsNullableNestedTuples()
        {
            var source = @"
class C
{
    public static void Main()
    {
        Compare((1, null), (1, null), true);
        Compare(null, (1, (2, 3)), false);
        Compare((1, (2, 3)), (1, null), false);
        Compare((1, (4, 4)), (1, (4, 4)), true);
        Compare((1, (5, 5)), (1, (10, 10)), false);
        System.Console.Write(""Success"");
    }
    private static void Compare((int, (int, int)?)? nt1, (int, (int, int)?)? nt2, bool expectMatch)
    {
        if (expectMatch != (nt1 == nt2) || expectMatch == (nt1 != nt2))
        {
            throw null;
        }
     }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Success");
        }

        [Fact]
        public void TestEqualOnNullableVsNullableTuples_WithImplicitConversion()
        {
            var source = @"
class C
{
    public static void Main()
    {
        Compare(null, null);
        Compare(null, (1, 2));
        Compare((2, 3), null);
        Compare((4, 4), (4, 4));
        Compare((5, 5), (10, 10));
    }
    private static void Compare((int, int)? nt1, (byte, long)? nt2)
    {
        System.Console.Write($""{nt1 == nt2} "");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "True False False True False");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var comparison = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
            var nt1 = comparison.Left;
            var nt1Type = model.GetTypeInfo(nt1);
            Assert.Equal("nt1", nt1.ToString());
            Assert.Equal("(System.Int32, System.Int32)?", nt1Type.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int64)?", nt1Type.ConvertedType.ToTestDisplayString());

            var nt2 = comparison.Right;
            var nt2Type = model.GetTypeInfo(nt2);
            Assert.Equal("nt2", nt2.ToString());
            Assert.Equal("(System.Byte, System.Int64)?", nt2Type.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int64)?", nt2Type.ConvertedType.ToTestDisplayString());

            verifier.VerifyIL("C.Compare", @"{
  // Code size      105 (0x69)
  .maxstack  3
  .locals init ((int, int)? V_0,
                (byte, long)? V_1,
                bool V_2,
                System.ValueTuple<int, int> V_3,
                System.ValueTuple<byte, long> V_4)
  IL_0000:  nop
  IL_0001:  ldstr      ""{0} ""
  IL_0006:  ldarg.0
  IL_0007:  stloc.0
  IL_0008:  ldarg.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""bool (int, int)?.HasValue.get""
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  ldloca.s   V_1
  IL_0015:  call       ""bool (byte, long)?.HasValue.get""
  IL_001a:  beq.s      IL_001f
  IL_001c:  ldc.i4.0
  IL_001d:  br.s       IL_0058
  IL_001f:  ldloc.2
  IL_0020:  brtrue.s   IL_0025
  IL_0022:  ldc.i4.1
  IL_0023:  br.s       IL_0058
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""(int, int) (int, int)?.GetValueOrDefault()""
  IL_002c:  stloc.3
  IL_002d:  ldloca.s   V_1
  IL_002f:  call       ""(byte, long) (byte, long)?.GetValueOrDefault()""
  IL_0034:  stloc.s    V_4
  IL_0036:  ldloc.3
  IL_0037:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_003c:  ldloc.s    V_4
  IL_003e:  ldfld      ""byte System.ValueTuple<byte, long>.Item1""
  IL_0043:  bne.un.s   IL_0057
  IL_0045:  ldloc.3
  IL_0046:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_004b:  conv.i8
  IL_004c:  ldloc.s    V_4
  IL_004e:  ldfld      ""long System.ValueTuple<byte, long>.Item2""
  IL_0053:  ceq
  IL_0055:  br.s       IL_0058
  IL_0057:  ldc.i4.0
  IL_0058:  box        ""bool""
  IL_005d:  call       ""string string.Format(string, object)""
  IL_0062:  call       ""void System.Console.Write(string)""
  IL_0067:  nop
  IL_0068:  ret
}");
        }

        [Fact]
        public void TestOnNullableVsNullableTuples_WithImplicitCustomConversion()
        {
            var source = @"
class C
{
    int _value;
    public C(int v) { _value = v; }

    public static void Main()
    {
        Compare(null, null);
        Compare(null, (1, new C(20)));
        Compare((new C(30), 3), null);
        Compare((new C(4), 4), (4, new C(4)));
        Compare((new C(5), 5), (10, new C(10)));
        Compare((new C(6), 6), (6, new C(20)));
    }
    private static void Compare((C, int)? nt1, (int, C)? nt2)
    {
        System.Console.Write($""{nt1 == nt2} "");
    }
    public static implicit operator int(C c)
    {
        System.Console.Write($""Convert{c._value} "");
        return c._value;
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "True False False Convert4 Convert4 True Convert5 False Convert6 Convert20 False ");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var comparison = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
            var nt1 = comparison.Left;
            var nt1Type = model.GetTypeInfo(nt1);
            Assert.Equal("nt1", nt1.ToString());
            Assert.Equal("(C, System.Int32)?", nt1Type.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)?", nt1Type.ConvertedType.ToTestDisplayString());

            var nt2 = comparison.Right;
            var nt2Type = model.GetTypeInfo(nt2);
            Assert.Equal("nt2", nt2.ToString());
            Assert.Equal("(System.Int32, C)?", nt2Type.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)?", nt2Type.ConvertedType.ToTestDisplayString());

            verifier.VerifyIL("C.Compare", @"{
  // Code size      114 (0x72)
  .maxstack  3
  .locals init ((C, int)? V_0,
                (int, C)? V_1,
                bool V_2,
                System.ValueTuple<C, int> V_3,
                System.ValueTuple<int, C> V_4)
  IL_0000:  nop
  IL_0001:  ldstr      ""{0} ""
  IL_0006:  ldarg.0
  IL_0007:  stloc.0
  IL_0008:  ldarg.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""bool (C, int)?.HasValue.get""
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  ldloca.s   V_1
  IL_0015:  call       ""bool (int, C)?.HasValue.get""
  IL_001a:  beq.s      IL_001f
  IL_001c:  ldc.i4.0
  IL_001d:  br.s       IL_0061
  IL_001f:  ldloc.2
  IL_0020:  brtrue.s   IL_0025
  IL_0022:  ldc.i4.1
  IL_0023:  br.s       IL_0061
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""(C, int) (C, int)?.GetValueOrDefault()""
  IL_002c:  stloc.3
  IL_002d:  ldloca.s   V_1
  IL_002f:  call       ""(int, C) (int, C)?.GetValueOrDefault()""
  IL_0034:  stloc.s    V_4
  IL_0036:  ldloc.3
  IL_0037:  ldfld      ""C System.ValueTuple<C, int>.Item1""
  IL_003c:  call       ""int C.op_Implicit(C)""
  IL_0041:  ldloc.s    V_4
  IL_0043:  ldfld      ""int System.ValueTuple<int, C>.Item1""
  IL_0048:  bne.un.s   IL_0060
  IL_004a:  ldloc.3
  IL_004b:  ldfld      ""int System.ValueTuple<C, int>.Item2""
  IL_0050:  ldloc.s    V_4
  IL_0052:  ldfld      ""C System.ValueTuple<int, C>.Item2""
  IL_0057:  call       ""int C.op_Implicit(C)""
  IL_005c:  ceq
  IL_005e:  br.s       IL_0061
  IL_0060:  ldc.i4.0
  IL_0061:  box        ""bool""
  IL_0066:  call       ""string string.Format(string, object)""
  IL_006b:  call       ""void System.Console.Write(string)""
  IL_0070:  nop
  IL_0071:  ret
}");
        }

        [Fact]
        public void TestOnNullableVsNonNullableTuples()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M(null);
        M((1, 2));
        M((10, 20));
    }
    private static void M((byte, int)? nt)
    {
        System.Console.Write((1, 2) == nt);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput: "FalseTrueFalse");
            verifier.VerifyIL("C.M", @"{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init ((byte, int)? V_0,
                System.ValueTuple<byte, int> V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""bool (byte, int)?.HasValue.get""
  IL_000a:  brtrue.s   IL_000f
  IL_000c:  ldc.i4.0
  IL_000d:  br.s       IL_002e
  IL_000f:  br.s       IL_0011
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       ""(byte, int) (byte, int)?.GetValueOrDefault()""
  IL_0018:  stloc.1
  IL_0019:  ldc.i4.1
  IL_001a:  ldloc.1
  IL_001b:  ldfld      ""byte System.ValueTuple<byte, int>.Item1""
  IL_0020:  bne.un.s   IL_002d
  IL_0022:  ldc.i4.2
  IL_0023:  ldloc.1
  IL_0024:  ldfld      ""int System.ValueTuple<byte, int>.Item2""
  IL_0029:  ceq
  IL_002b:  br.s       IL_002e
  IL_002d:  ldc.i4.0
  IL_002e:  call       ""void System.Console.Write(bool)""
  IL_0033:  nop
  IL_0034:  ret
}");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var comparison = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
            var tuple = comparison.Left;
            var tupleType = model.GetTypeInfo(tuple);
            Assert.Equal("(1, 2)", tuple.ToString());
            Assert.Equal("(System.Int32, System.Int32)", tupleType.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)?", tupleType.ConvertedType.ToTestDisplayString());

            var nt = comparison.Right;
            var ntType = model.GetTypeInfo(nt);
            Assert.Equal("nt", nt.ToString());
            Assert.Equal("(System.Byte, System.Int32)?", ntType.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)?", ntType.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void TestOnNullableVsNonNullableTuples_WithCustomConversion()
        {
            var source = @"
class C
{
    int _value;
    public C(int v) { _value = v; }
    public static void Main()
    {
        M(null);
        M((new C(1), 2));
        M((new C(10), 20));
    }
    private static void M((C, int)? nt)
    {
        System.Console.Write($""{(1, 2) == nt} "");
        System.Console.Write($""{nt == (1, 2)} "");
    }
    public static implicit operator int(C c)
    {
        System.Console.Write($""Convert{c._value} "");
        return c._value;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput: "False False Convert1 True Convert1 True Convert10 False Convert10 False");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var comparison = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().First();
            Assert.Equal("(1, 2) == nt", comparison.ToString());
            var tuple = comparison.Left;
            var tupleType = model.GetTypeInfo(tuple);
            Assert.Equal("(1, 2)", tuple.ToString());
            Assert.Equal("(System.Int32, System.Int32)", tupleType.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)?", tupleType.ConvertedType.ToTestDisplayString());

            var nt = comparison.Right;
            var ntType = model.GetTypeInfo(nt);
            Assert.Equal("nt", nt.ToString());
            Assert.Equal("(C, System.Int32)?", ntType.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)?", ntType.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void TestOnNullableVsNonNullableTuples3()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M(null);
        M((1, 2));
        M((10, 20));
    }
    private static void M((int, int)? nt)
    {
        System.Console.Write(nt == (1, 2));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput: "FalseTrueFalse");
            verifier.VerifyIL("C.M", @"{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init ((int, int)? V_0,
                bool V_1,
                System.ValueTuple<int, int> V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""bool (int, int)?.HasValue.get""
  IL_000a:  stloc.1
  IL_000b:  ldloc.1
  IL_000c:  brtrue.s   IL_0011
  IL_000e:  ldc.i4.0
  IL_000f:  br.s       IL_0034
  IL_0011:  ldloc.1
  IL_0012:  brtrue.s   IL_0017
  IL_0014:  ldc.i4.1
  IL_0015:  br.s       IL_0034
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""(int, int) (int, int)?.GetValueOrDefault()""
  IL_001e:  stloc.2
  IL_001f:  ldloc.2
  IL_0020:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0025:  ldc.i4.1
  IL_0026:  bne.un.s   IL_0033
  IL_0028:  ldloc.2
  IL_0029:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_002e:  ldc.i4.2
  IL_002f:  ceq
  IL_0031:  br.s       IL_0034
  IL_0033:  ldc.i4.0
  IL_0034:  call       ""void System.Console.Write(bool)""
  IL_0039:  nop
  IL_003a:  ret
}");
        }

        [Fact]
        public void TestOnNullableVsLiteralTuples()
        {
            var source = @"
class C
{
    public static void Main()
    {
        CheckNull(null);
        CheckNull((1, 2));
    }
    private static void CheckNull((int, int)? nt)
    {
        System.Console.Write($""{nt == null} "");
        System.Console.Write($""{nt != null} "");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True False False True");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var lastNull = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Last();
            Assert.Equal("null", lastNull.ToString());
            var nullType = model.GetTypeInfo(lastNull);
            Assert.Null(nullType.Type);
            Assert.Null(nullType.ConvertedType); // In nullable-null comparison, the null literal remains typeless
        }

        [Fact]
        public void TestOnLongTuple()
        {
            var source = @"
class C
{
    public static void Main()
    {
        Assert(MakeLongTuple(1) == MakeLongTuple(1));
        Assert(!(MakeLongTuple(1) != MakeLongTuple(1)));

        Assert(MakeLongTuple(1) == (1, 1, 1, 1, 1, 1, 1, 1, 1));
        Assert(!(MakeLongTuple(1) != (1, 1, 1, 1, 1, 1, 1, 1, 1)));

        Assert(MakeLongTuple(1) == MakeLongTuple(1, 1));
        Assert(!(MakeLongTuple(1) != MakeLongTuple(1, 1)));

        Assert(!(MakeLongTuple(1) == MakeLongTuple(1, 2)));
        Assert(!(MakeLongTuple(1) == MakeLongTuple(2, 1)));
        Assert(MakeLongTuple(1) != MakeLongTuple(1, 2));
        Assert(MakeLongTuple(1) != MakeLongTuple(2, 1));

        System.Console.Write(""Success"");
    }
    private static (int, int, int, int, int, int, int, int, int) MakeLongTuple(int x)
        => (x, x, x, x, x, x, x, x, x);

    private static (int?, int, int?, int, int?, int, int?, int, int?)? MakeLongTuple(int? x, int y)
        => (x, y, x, y, x, y, x, y, x);

    private static void Assert(bool test)
    {
        if (!test)
        {
            throw null;
        }
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Success");
        }

        [Fact]
        public void TestOn1Tuple_FromRest()
        {
            var source = @"
class C
{
    public static bool M()
    {
        var x1 = MakeLongTuple(1).Rest;

        bool b1 = x1 == x1;
        bool b2 = x1 != x1;

        return b1 && b2;
    }
    private static (int, int, int, int, int, int, int, int?) MakeLongTuple(int? x)
        => throw null;

    public bool Unused((int, int, int, int, int, int, int, int?) t)
    {
        return t.Rest == t.Rest;
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,19): error CS0019: Operator '==' cannot be applied to operands of type 'ValueTuple<int?>' and 'ValueTuple<int?>'
                //         bool b1 = x1 == x1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x1 == x1").WithArguments("==", "ValueTuple<int?>", "ValueTuple<int?>").WithLocation(8, 19),
                // (9,19): error CS0019: Operator '!=' cannot be applied to operands of type 'ValueTuple<int?>' and 'ValueTuple<int?>'
                //         bool b2 = x1 != x1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x1 != x1").WithArguments("!=", "ValueTuple<int?>", "ValueTuple<int?>").WithLocation(9, 19),
                // (18,16): error CS0019: Operator '==' cannot be applied to operands of type 'ValueTuple<int?>' and 'ValueTuple<int?>'
                //         return t.Rest == t.Rest;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t.Rest == t.Rest").WithArguments("==", "ValueTuple<int?>", "ValueTuple<int?>").WithLocation(18, 16)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var comparison = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Last();
            Assert.Equal("t.Rest == t.Rest", comparison.ToString());

            var left = model.GetTypeInfo(comparison.Left);
            Assert.Equal("ValueTuple<System.Int32?>", left.Type.ToTestDisplayString());
            Assert.Equal("ValueTuple<System.Int32?>", left.ConvertedType.ToTestDisplayString());
            Assert.True(left.Type.IsTupleType);
        }

        [Fact]
        public void TestOn1Tuple_FromValueTuple()
        {
            var source = @"
using System;
class C
{
    public static bool M()
    {
        var x1 = ValueTuple.Create((int?)1);

        bool b1 = x1 == x1;
        bool b2 = x1 != x1;

        return b1 && b2;
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,19): error CS0019: Operator '==' cannot be applied to operands of type 'ValueTuple<int?>' and 'ValueTuple<int?>'
                //         bool b1 = x1 == x1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x1 == x1").WithArguments("==", "ValueTuple<int?>", "ValueTuple<int?>").WithLocation(9, 19),
                // (10,19): error CS0019: Operator '!=' cannot be applied to operands of type 'ValueTuple<int?>' and 'ValueTuple<int?>'
                //         bool b2 = x1 != x1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x1 != x1").WithArguments("!=", "ValueTuple<int?>", "ValueTuple<int?>").WithLocation(10, 19)
                );
        }

        [Fact]
        public void TestOnTupleOfDecimals()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.Write(Compare((1, 2), (1, 2)));
        System.Console.Write(Compare((1, 2), (10, 20)));
    }
    public static bool Compare((decimal, decimal) t1, (decimal, decimal) t2)
    {
        return t1 == t2;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "TrueFalse");
            verifier.VerifyIL("C.Compare", @"{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (System.ValueTuple<decimal, decimal> V_0,
                System.ValueTuple<decimal, decimal> V_1,
                bool V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldarg.1
  IL_0004:  stloc.1
  IL_0005:  ldloc.0
  IL_0006:  ldfld      ""decimal System.ValueTuple<decimal, decimal>.Item1""
  IL_000b:  ldloc.1
  IL_000c:  ldfld      ""decimal System.ValueTuple<decimal, decimal>.Item1""
  IL_0011:  call       ""bool decimal.op_Equality(decimal, decimal)""
  IL_0016:  brfalse.s  IL_002b
  IL_0018:  ldloc.0
  IL_0019:  ldfld      ""decimal System.ValueTuple<decimal, decimal>.Item2""
  IL_001e:  ldloc.1
  IL_001f:  ldfld      ""decimal System.ValueTuple<decimal, decimal>.Item2""
  IL_0024:  call       ""bool decimal.op_Equality(decimal, decimal)""
  IL_0029:  br.s       IL_002c
  IL_002b:  ldc.i4.0
  IL_002c:  stloc.2
  IL_002d:  br.s       IL_002f
  IL_002f:  ldloc.2
  IL_0030:  ret
}");
        }

        [Fact]
        public void TestSideEffectsAreSavedToTemps()
        {
            var source = @"
class C
{
    public static void Main()
    {
        int i = 0;
        System.Console.Write((i++, i++, i++) == (0, 1, 2));
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "True");
        }

        [Fact]
        public void TestNonBoolComparisonResult_WithTrueFalseOperators()
        {
            var source = @"
using static System.Console;
public class C
{
    public static void Main()
    {
        Write($""{REPLACE}"");
    }
}
public class Base
{
    public int I;
    public Base(int i) { I = i; }
}
public class A : Base
{
    public A(int i) : base(i)
    {
        Write($""A:{i}, "");
    }
    public static NotBool operator ==(A a, Y y)
    {
        Write(""A == Y, "");
        return new NotBool(a.I == y.I);
    }
    public static NotBool operator !=(A a, Y y)
    {
        Write(""A != Y, "");
        return new NotBool(a.I != y.I);
    }
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
public class X : Base
{
    public X(int i) : base(i)
    {
        Write($""X:{i}, "");
    }
}
public class Y : Base
{
    public Y(int i) : base(i)
    {
        Write($""Y:{i}, "");
    }
    public static implicit operator Y(X x)
    {
        Write(""X -> "");
        return new Y(x.I);
    }
}
public class NotBool
{
    public bool B;
    public NotBool(bool value)
    {
        B = value;
    }
    public static bool operator true(NotBool b)
    {
        Write($""NotBool.true -> {b.B}, "");
        return b.B;
    }
    public static bool operator false(NotBool b)
    {
        Write($""NotBool.false -> {!b.B}, "");
        return !b.B;
    }
}
";

            validate("(new A(1), new A(2)) == (new X(1), new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A == Y, NotBool.false -> False, A == Y, NotBool.false -> False, True");
            validate("(new A(1), new A(2)) == (new X(1), new Y(20))", "A:1, A:2, X:1, Y:20, X -> Y:1, A == Y, NotBool.false -> False, A == Y, NotBool.false -> True, False");

            validate("(new A(1), new A(2)) != (new X(1), new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A != Y, NotBool.true -> False, A != Y, NotBool.true -> False, False");
            validate("(new A(1), new A(2)) != (new X(1), new Y(20))", "A:1, A:2, X:1, Y:20, X -> Y:1, A != Y, NotBool.true -> False, A != Y, NotBool.true -> True, True");

            validate("((dynamic)new A(1), new A(2)) == (new X(1), (dynamic)new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A == Y, NotBool.false -> False, A == Y, NotBool.false -> False, True");
            validate("((dynamic)new A(1), new A(2)) == (new X(1), (dynamic)new Y(20))", "A:1, A:2, X:1, Y:20, X -> Y:1, A == Y, NotBool.false -> False, A == Y, NotBool.false -> True, False");

            validate("((dynamic)new A(1), new A(2)) != (new X(1), (dynamic)new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A != Y, NotBool.true -> False, A != Y, NotBool.true -> False, False");
            validate("((dynamic)new A(1), new A(2)) != (new X(1), (dynamic)new Y(20))", "A:1, A:2, X:1, Y:20, X -> Y:1, A != Y, NotBool.true -> False, A != Y, NotBool.true -> True, True");

            void validate(string expression, string expected)
            {
                var comp = CreateCompilation(source.Replace("REPLACE", expression),
                    targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);

                comp.VerifyDiagnostics();
                CompileAndVerify(comp, expectedOutput: expected);
            }
        }

        [Fact]
        public void TestNonBoolComparisonResult_WithTrueFalseOperatorsOnBase()
        {
            var source = @"
using static System.Console;
public class C
{
    public static void Main()
    {
        Write($""{REPLACE}"");
    }
}
public class Base
{
    public int I;
    public Base(int i) { I = i; }
}
public class A : Base
{
    public A(int i) : base(i)
    {
        Write($""A:{i}, "");
    }
    public static NotBool operator ==(A a, Y y)
    {
        Write(""A == Y, "");
        return new NotBool(a.I == y.I);
    }
    public static NotBool operator !=(A a, Y y)
    {
        Write(""A != Y, "");
        return new NotBool(a.I != y.I);
    }
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
public class X : Base
{
    public X(int i) : base(i)
    {
        Write($""X:{i}, "");
    }
}
public class Y : Base
{
    public Y(int i) : base(i)
    {
        Write($""Y:{i}, "");
    }
    public static implicit operator Y(X x)
    {
        Write(""X -> "");
        return new Y(x.I);
    }
}
public class NotBoolBase
{
    public bool B;
    public NotBoolBase(bool value)
    {
        B = value;
    }
    public static bool operator true(NotBoolBase b)
    {
        Write($""NotBoolBase.true -> {b.B}, "");
        return b.B;
    }
    public static bool operator false(NotBoolBase b)
    {
        Write($""NotBoolBase.false -> {!b.B}, "");
        return !b.B;
    }
}
public class NotBool : NotBoolBase
{
    public NotBool(bool value) : base(value) { }
}
";

            // This tests the case where the custom operators false/true need an input conversion that's not just an identity conversion (in this case, it's an implicit reference conversion)
            validate("(new A(1), new A(2)) == (new X(1), new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A == Y, NotBoolBase.false -> False, A == Y, NotBoolBase.false -> False, True");
            validate("(new A(1), new A(2)) == (new X(1), new Y(20))", "A:1, A:2, X:1, Y:20, X -> Y:1, A == Y, NotBoolBase.false -> False, A == Y, NotBoolBase.false -> True, False");

            validate("(new A(1), new A(2)) != (new X(1), new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A != Y, NotBoolBase.true -> False, A != Y, NotBoolBase.true -> False, False");
            validate("(new A(1), new A(2)) != (new X(1), new Y(20))", "A:1, A:2, X:1, Y:20, X -> Y:1, A != Y, NotBoolBase.true -> False, A != Y, NotBoolBase.true -> True, True");

            validate("((dynamic)new A(1), new A(2)) == (new X(1), (dynamic)new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A == Y, NotBoolBase.false -> False, A == Y, NotBoolBase.false -> False, True");
            validate("((dynamic)new A(1), new A(2)) == (new X(1), (dynamic)new Y(20))", "A:1, A:2, X:1, Y:20, X -> Y:1, A == Y, NotBoolBase.false -> False, A == Y, NotBoolBase.false -> True, False");

            validate("((dynamic)new A(1), new A(2)) != (new X(1), (dynamic)new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A != Y, NotBoolBase.true -> False, A != Y, NotBoolBase.true -> False, False");
            validate("((dynamic)new A(1), new A(2)) != (new X(1), (dynamic)new Y(20))", "A:1, A:2, X:1, Y:20, X -> Y:1, A != Y, NotBoolBase.true -> False, A != Y, NotBoolBase.true -> True, True");

            void validate(string expression, string expected)
            {
                var comp = CreateCompilation(source.Replace("REPLACE", expression),
                    targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);

                comp.VerifyDiagnostics();
                CompileAndVerify(comp, expectedOutput: expected);
            }
        }

        [Fact]
        public void TestNonBoolComparisonResult_WithImplicitBoolConversion()
        {
            var source = @"
using static System.Console;
public class C
{
    public static void Main()
    {
        Write($""{REPLACE}"");
    }
}
public class Base
{
    public int I;
    public Base(int i) { I = i; }
}
public class A : Base
{
    public A(int i) : base(i)
    {
        Write($""A:{i}, "");
    }
    public static NotBool operator ==(A a, Y y)
    {
        Write(""A == Y, "");
        return new NotBool(a.I == y.I);
    }
    public static NotBool operator !=(A a, Y y)
    {
        Write(""A != Y, "");
        return new NotBool(a.I != y.I);
    }
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
public class X : Base
{
    public X(int i) : base(i)
    {
        Write($""X:{i}, "");
    }
}
public class Y : Base
{
    public Y(int i) : base(i)
    {
        Write($""Y:{i}, "");
    }
    public static implicit operator Y(X x)
    {
        Write(""X -> "");
        return new Y(x.I);
    }
}
public class NotBool
{
    public bool B;
    public NotBool(bool value)
    {
        B = value;
    }
    public static implicit operator bool(NotBool b)
    {
        Write($""NotBool -> bool:{b.B}, "");
        return b.B;
    }
}
";

            validate("(new A(1), new A(2)) == (new X(1), new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A == Y, NotBool -> bool:True, A == Y, NotBool -> bool:True, True");
            validate("(new A(1), new A(2)) == (new X(10), new Y(2))", "A:1, A:2, X:10, Y:2, X -> Y:10, A == Y, NotBool -> bool:False, False");
            validate("(new A(1), new A(2)) == (new X(1), new Y(20))", "A:1, A:2, X:1, Y:20, X -> Y:1, A == Y, NotBool -> bool:True, A == Y, NotBool -> bool:False, False");

            validate("(new A(1), new A(2)) != (new X(1), new Y(2))", "A:1, A:2, X:1, Y:2, X -> Y:1, A != Y, NotBool -> bool:False, A != Y, NotBool -> bool:False, False");
            validate("(new A(1), new A(2)) != (new X(10), new Y(2))", "A:1, A:2, X:10, Y:2, X -> Y:10, A != Y, NotBool -> bool:True, True");
            validate("(new A(1), new A(2)) != (new X(1), new Y(20))", "A:1, A:2, X:1, Y:20, X -> Y:1, A != Y, NotBool -> bool:False, A != Y, NotBool -> bool:True, True");

            void validate(string expression, string expected)
            {
                var comp = CreateCompilation(source.Replace("REPLACE", expression), options: TestOptions.DebugExe);

                comp.VerifyDiagnostics();
                CompileAndVerify(comp, expectedOutput: expected);
            }
        }

        [Fact]
        public void TestNonBoolComparisonResult_WithoutImplicitBoolConversion()
        {
            var source = @"
using static System.Console;
public class C
{
    public static void Main()
    {
        Write($""{REPLACE}"");
    }
}
public class Base
{
    public Base(int i) { }
}
public class A : Base
{
    public A(int i) : base(i)
        => throw null;
    public static NotBool operator ==(A a, Y y)
        => throw null;
    public static NotBool operator !=(A a, Y y)
        => throw null;
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
public class X : Base
{
    public X(int i) : base(i)
        => throw null;
}
public class Y : Base
{
    public Y(int i) : base(i)
        => throw null;
    public static implicit operator Y(X x)
        => throw null;
}
public class NotBool
{
}
";

            validate("(new A(1), new A(2)) == (new X(1), new Y(2))",
                // (7,18): error CS0029: Cannot implicitly convert type 'NotBool' to 'bool'
                //         Write($"{(new A(1), new A(2)) == (new X(1), new Y(2))}");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(new A(1), new A(2)) == (new X(1), new Y(2))").WithArguments("NotBool", "bool").WithLocation(7, 18),
                // (7,18): error CS0029: Cannot implicitly convert type 'NotBool' to 'bool'
                //         Write($"{(new A(1), new A(2)) == (new X(1), new Y(2))}");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(new A(1), new A(2)) == (new X(1), new Y(2))").WithArguments("NotBool", "bool").WithLocation(7, 18)
                );

            validate("(new A(1), 2) != (new X(1), 2)",
                // (7,18): error CS0029: Cannot implicitly convert type 'NotBool' to 'bool'
                //         Write($"{(new A(1), 2) != (new X(1), 2)}");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(new A(1), 2) != (new X(1), 2)").WithArguments("NotBool", "bool").WithLocation(7, 18)
                );

            void validate(string expression, params DiagnosticDescription[] expected)
            {
                var comp = CreateCompilation(source.Replace("REPLACE", expression));
                comp.VerifyDiagnostics(expected);
            }
        }

        [Fact]
        public void TestNonBoolComparisonResult_WithExplicitBoolConversion()
        {
            var source = @"
using static System.Console;
public class C
{
    public static void Main()
    {
        Write($""{(new A(1), new A(2)) == (new X(1), new Y(2))}"");
    }
}
public class Base
{
    public int I;
    public Base(int i) { I = i; }
}
public class A : Base
{
    public A(int i) : base(i)
        => throw null;
    public static NotBool operator ==(A a, Y y)
        => throw null;
    public static NotBool operator !=(A a, Y y)
        => throw null;
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
public class X : Base
{
    public X(int i) : base(i)
        => throw null;
}
public class Y : Base
{
    public Y(int i) : base(i)
        => throw null;
    public static implicit operator Y(X x)
        => throw null;
}
public class NotBool
{
    public NotBool(bool value)
        => throw null;
    public static explicit operator bool(NotBool b)
        => throw null;
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,18): error CS0029: Cannot implicitly convert type 'NotBool' to 'bool'
                //         Write($"{(new A(1), new A(2)) == (new X(1), new Y(2))}");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(new A(1), new A(2)) == (new X(1), new Y(2))").WithArguments("NotBool", "bool").WithLocation(7, 18),
                // (7,18): error CS0029: Cannot implicitly convert type 'NotBool' to 'bool'
                //         Write($"{(new A(1), new A(2)) == (new X(1), new Y(2))}");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(new A(1), new A(2)) == (new X(1), new Y(2))").WithArguments("NotBool", "bool").WithLocation(7, 18)
                );
        }

        [Fact]
        public void TestNullableBoolComparisonResult_WithTrueFalseOperators()
        {
            var source = @"
public class C
{
    public static void M(A a)
    {
        _ = (a, a) == (a, a);
        if (a == a) { }
    }
}
public class A
{
    public A(int i)
        => throw null;
    public static bool? operator ==(A a1, A a2)
        => throw null;
    public static bool? operator !=(A a1, A a2)
        => throw null;
    public override bool Equals(object o)
        => throw null;
    public override int GetHashCode()
        => throw null;
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,13): error CS0029: Cannot implicitly convert type 'bool?' to 'bool'
                //         _ = (a, a) == (a, a);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(a, a) == (a, a)").WithArguments("bool?", "bool").WithLocation(6, 13),
                // (6,13): error CS0029: Cannot implicitly convert type 'bool?' to 'bool'
                //         _ = (a, a) == (a, a);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(a, a) == (a, a)").WithArguments("bool?", "bool").WithLocation(6, 13),
                // (7,13): error CS0266: Cannot implicitly convert type 'bool?' to 'bool'. An explicit conversion exists (are you missing a cast?)
                //         if (a == a) { }
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a == a").WithArguments("bool?", "bool").WithLocation(7, 13),
                // (7,13): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                //         if (a == a) { }
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "a == a").WithLocation(7, 13)
                );
        }

        [Fact]
        public void TestElementNames()
        {
            var source = @"
#pragma warning disable CS0219
using static System.Console;
public class C
{
    public static void Main()
    {
        int a = 1;
        int b = 2;
        int c = 3;
        int d = 4;
        int x = 5;
        int y = 6;
        (int x, int y) t1 = (1, 2);
        (int, int) t2 = (1, 2);
        Write($""{REPLACE}"");
    }
}
";

            // tuple expression vs tuple expression
            validate("t1 == t2");

            // tuple expression vs tuple literal
            validate("t1 == (x: 1, y: 2)");
            validate("t1 == (1, 2)");
            validate("(1, 2) == t1");
            validate("(x: 1, d) == t1");

            validate("t2 == (x: 1, y: 2)",
                // (16,25): warning CS8375: The tuple element name 'x' is ignored because a different name or no name is specified on the other side of the tuple == or != operator.
                //         Write($"{t2 == (x: 1, y: 2)}");
                Diagnostic(ErrorCode.WRN_TupleBinopLiteralNameMismatch, "x: 1").WithArguments("x").WithLocation(16, 25),
                // (16,31): warning CS8375: The tuple element name 'y' is ignored because a different name or no name is specified on the other side of the tuple == or != operator.
                //         Write($"{t2 == (x: 1, y: 2)}");
                Diagnostic(ErrorCode.WRN_TupleBinopLiteralNameMismatch, "y: 2").WithArguments("y").WithLocation(16, 31)
                );

            // tuple literal vs tuple literal
            // - warnings reported on the right when both sides could complain
            // - no warnings on inferred names

            validate("((a, b), c: 3) == ((1, x: 2), 3)",
                // (16,27): warning CS8375: The tuple element name 'c' is ignored because a different name or no name is specified on the other side of the tuple == or != operator.
                //         Write($"{((a, b), c: 3) == ((1, x: 2), 3)}");
                Diagnostic(ErrorCode.WRN_TupleBinopLiteralNameMismatch, "c: 3").WithArguments("c").WithLocation(16, 27),
                // (16,41): warning CS8375: The tuple element name 'x' is ignored because a different name or no name is specified on the other side of the tuple == or != operator.
                //         Write($"{((a, b), c: 3) == ((1, x: 2), 3)}");
                Diagnostic(ErrorCode.WRN_TupleBinopLiteralNameMismatch, "x: 2").WithArguments("x").WithLocation(16, 41)
                );

            validate("(a, b) == (a: 1, b: 2)");
            validate("(a, b) == (c: 1, d)",
                // (16,29): warning CS8375: The tuple element name 'c' is ignored because a different name or no name is specified on the other side of the tuple == or != operator.
                //         Write($"{(a, b) == (c: 1, d)}");
                Diagnostic(ErrorCode.WRN_TupleBinopLiteralNameMismatch, "c: 1").WithArguments("c").WithLocation(16, 29)
                );

            validate("(a: 1, b: 2) == (c: 1, d)",
                // (16,35): warning CS8375: The tuple element name 'c' is ignored because a different name or no name is specified on the other side of the tuple == or != operator.
                //         Write($"{(a: 1, b: 2) == (c: 1, d)}");
                Diagnostic(ErrorCode.WRN_TupleBinopLiteralNameMismatch, "c: 1").WithArguments("c").WithLocation(16, 35),
                // (16,25): warning CS8375: The tuple element name 'b' is ignored because a different name or no name is specified on the other side of the tuple == or != operator.
                //         Write($"{(a: 1, b: 2) == (c: 1, d)}");
                Diagnostic(ErrorCode.WRN_TupleBinopLiteralNameMismatch, "b: 2").WithArguments("b").WithLocation(16, 25)
                );

            validate("(null, b) == (c: null, d: 2)",
                // (16,32): warning CS8375: The tuple element name 'c' is ignored because a different name or no name is specified on the other side of the tuple == or != operator.
                //         Write($"{(null, b) == (c: null, d: 2)}");
                Diagnostic(ErrorCode.WRN_TupleBinopLiteralNameMismatch, "c: null").WithArguments("c").WithLocation(16, 32),
                // (16,41): warning CS8375: The tuple element name 'd' is ignored because a different name or no name is specified on the other side of the tuple == or != operator.
                //         Write($"{(null, b) == (c: null, d: 2)}");
                Diagnostic(ErrorCode.WRN_TupleBinopLiteralNameMismatch, "d: 2").WithArguments("d").WithLocation(16, 41)
                );

            void validate(string expression, params DiagnosticDescription[] diagnostics)
            {
                var comp = CreateCompilation(source.Replace("REPLACE", expression));
                comp.VerifyDiagnostics(diagnostics);
            }
        }

        [Fact]
        void TestValueTupleWithObsoleteEqualityOperator()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        System.Console.Write((1, 2) == (3, 4));
    }
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        [System.Obsolete]
        public static bool operator==(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
            => throw null;

        public static bool operator!=(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
            => throw null;

        public override bool Equals(object other)
            => throw null;

        public override int GetHashCode()
            => throw null;
    }
}
";

            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            // Note: tuple equality picked ahead of custom operator==
            CompileAndVerify(comp, expectedOutput: "False");
        }

        [Fact]
        public void TestInExpressionTree()
        {
            var source = @"
using System;
using System.Linq.Expressions;
public class C
{
    public static void Main()
    {
        Expression<Func<int, bool>> expr = i => (i, i) == (i, i);
        Expression<Func<(int, int), bool>> expr2 = t => t != t;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,49): error CS8374: An expression tree may not contain a tuple == or != operator
                //         Expression<Func<int, bool>> expr = i => (i, i) == (i, i);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsTupleBinOp, "(i, i) == (i, i)").WithLocation(8, 49),
                // (8,49): error CS8143: An expression tree may not contain a tuple literal.
                //         Expression<Func<int, bool>> expr = i => (i, i) == (i, i);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsTupleLiteral, "(i, i)").WithLocation(8, 49),
                // (8,59): error CS8143: An expression tree may not contain a tuple literal.
                //         Expression<Func<int, bool>> expr = i => (i, i) == (i, i);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsTupleLiteral, "(i, i)").WithLocation(8, 59),
                // (9,57): error CS8374: An expression tree may not contain a tuple == or != operator
                //         Expression<Func<(int, int), bool>> expr2 = t => t != t;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsTupleBinOp, "t != t").WithLocation(9, 57)
                );
        }

        [Fact]
        public void TestComparisonOfDynamicAndTuple()
        {
            var source = @"
public class C
{
    (long, string) _t;
    public C(long l, string s) { _t = (l, s); }

    public static void Main()
    {
        dynamic d = new C(1, ""hello"");
        (long, string) tuple1 = (1, ""hello"");
        (long, string) tuple2 = (2, ""world"");
        System.Console.Write($""{d == tuple1} {d != tuple1} {d == tuple2} {d != tuple2}"");
    }
    public static bool operator==(C c, (long, string) t)
    {
        return c._t.Item1 == t.Item1 && c._t.Item2 == t.Item2;
    }
    public static bool operator!=(C c, (long, string) t)
    {
        return !(c == t);
    }
    public override bool Equals(object o) => throw null;
    public override int GetHashCode() => throw null;
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True False False True");
        }

        [Fact]
        public void TestComparisonOfDynamicTuple()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        dynamic d1 = (1, ""hello"");
        dynamic d2 = (1, ""hello"");
        dynamic d3 = ((byte)1, 2);
        PrintException(() => d1 == d2);
        PrintException(() => d1 == d3);
    }
    public static void PrintException(System.Func<bool> action)
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            action();
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
        {
            System.Console.WriteLine(e.Message);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"Operator '==' cannot be applied to operands of type 'System.ValueTuple<int,string>' and 'System.ValueTuple<int,string>'
Operator '==' cannot be applied to operands of type 'System.ValueTuple<int,string>' and 'System.ValueTuple<byte,int>'");
        }

        [Fact]
        public void TestComparisonWithTupleElementNames()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        int Bob = 1;
        System.Console.Write((Alice: 0, (Bob, 2)) == (Bob: 0, (1, Other: 2)));
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,55): warning CS8375: The tuple element name 'Bob' is ignored because a different name or no name is specified on the other side of the tuple == or != operator.
                //         System.Console.Write((Alice: 0, (Bob, 2)) == (Bob: 0, (1, Other: 2)));
                Diagnostic(ErrorCode.WRN_TupleBinopLiteralNameMismatch, "Bob: 0").WithArguments("Bob").WithLocation(7, 55),
                // (7,67): warning CS8375: The tuple element name 'Other' is ignored because a different name or no name is specified on the other side of the tuple == or != operator.
                //         System.Console.Write((Alice: 0, (Bob, 2)) == (Bob: 0, (1, Other: 2)));
                Diagnostic(ErrorCode.WRN_TupleBinopLiteralNameMismatch, "Other: 2").WithArguments("Other").WithLocation(7, 67)
                );

            CompileAndVerify(comp, expectedOutput: "True");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var equals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().First();

            // check left tuple
            var leftTuple = equals.Left;
            var leftInfo = model.GetTypeInfo(leftTuple);
            Assert.Equal("(System.Int32 Alice, (System.Int32 Bob, System.Int32))", leftInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 Alice, (System.Int32 Bob, System.Int32))", leftInfo.ConvertedType.ToTestDisplayString());

            // check right tuple
            var rightTuple = equals.Right;
            var rightInfo = model.GetTypeInfo(rightTuple);
            Assert.Equal("(System.Int32 Bob, (System.Int32, System.Int32 Other))", rightInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 Bob, (System.Int32, System.Int32 Other))", rightInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void TestComparisonWithCastedTuples()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        System.Console.Write( ((string, (byte, long))) (null, (1, 2L)) == ((string, (long, byte))) (null, (1L, 2)) );
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "True");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var equals = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().First();

            // check left cast ...
            var leftCast = (CastExpressionSyntax)equals.Left;
            Assert.Equal("((string, (byte, long))) (null, (1, 2L))", leftCast.ToString());
            var leftCastInfo = model.GetTypeInfo(leftCast);
            Assert.Equal("(System.String, (System.Byte, System.Int64))", leftCastInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.String, (System.Int64, System.Int64))", leftCastInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTuple, model.GetConversion(leftCast).Kind);

            // ... its tuple ...
            var leftTuple = (TupleExpressionSyntax)leftCast.Expression;
            Assert.Equal("(null, (1, 2L))", leftTuple.ToString());
            var leftTupleInfo = model.GetTypeInfo(leftTuple);
            Assert.Null(leftTupleInfo.Type);
            Assert.Equal("(System.String, (System.Byte, System.Int64))", leftTupleInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ExplicitTupleLiteral, model.GetConversion(leftTuple).Kind);

            // ... its null ...
            var leftNull = leftTuple.Arguments[0].Expression;
            Assert.Equal("null", leftNull.ToString());
            var leftNullInfo = model.GetTypeInfo(leftNull);
            Assert.Null(leftNullInfo.Type);
            Assert.Equal("System.String", leftNullInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitReference, model.GetConversion(leftNull).Kind);

            // ... its nested tuple
            var leftNestedTuple = leftTuple.Arguments[1].Expression;
            Assert.Equal("(1, 2L)", leftNestedTuple.ToString());
            var leftNestedTupleInfo = model.GetTypeInfo(leftNestedTuple);
            Assert.Equal("(System.Int32, System.Int64)", leftNestedTupleInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.Byte, System.Int64)", leftNestedTupleInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, model.GetConversion(leftNestedTuple).Kind);

            // check right cast ...
            var rightCast = (CastExpressionSyntax)equals.Right;
            var rightCastInfo = model.GetTypeInfo(rightCast);
            Assert.Equal("(System.String, (System.Int64, System.Byte))", rightCastInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.String, (System.Int64, System.Int64))", rightCastInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTuple, model.GetConversion(rightCast).Kind);

            // ... its tuple
            var rightTuple = rightCast.Expression;
            var rightTupleInfo = model.GetTypeInfo(rightTuple);
            Assert.Null(rightTupleInfo.Type);
            Assert.Equal("(System.String, (System.Int64, System.Byte))", rightTupleInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ExplicitTupleLiteral, model.GetConversion(rightTuple).Kind);
        }

        [Fact]
        public void TestGenericElement()
        {
            var source = @"
public class C
{
    public void M<T>(T t)
    {
        _ = (t, t) == (t, t);
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,13): error CS0019: Operator '==' cannot be applied to operands of type 'T' and 'T'
                //         _ = (t, t) == (t, t);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(t, t) == (t, t)").WithArguments("==", "T", "T").WithLocation(6, 13),
                // (6,13): error CS0019: Operator '==' cannot be applied to operands of type 'T' and 'T'
                //         _ = (t, t) == (t, t);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(t, t) == (t, t)").WithArguments("==", "T", "T").WithLocation(6, 13)
                );
        }

        [Fact]
        public void TestNameofEquality()
        {
            var source = @"
public class C
{
    public void M()
    {
        _ = nameof((1, 2) == (3, 4));
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,20): error CS8081: Expression does not have a name.
                //         _ = nameof((1, 2) == (3, 4));
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "(1, 2) == (3, 4)").WithLocation(6, 20)
                );
        }

        [Fact]
        public void TestAsRefOrOutArgument()
        {
            var source = @"
public class C
{
    public void M(ref bool x, out bool y)
    {
        x = true;
        y = true;
        M(ref (1, 2) == (3, 4), out (1, 2) == (3, 4));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,15): error CS1510: A ref or out value must be an assignable variable
                //         M(ref (1, 2) == (3, 4), out (1, 2) == (3, 4));
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(1, 2) == (3, 4)").WithLocation(8, 15),
                // (8,37): error CS1510: A ref or out value must be an assignable variable
                //         M(ref (1, 2) == (3, 4), out (1, 2) == (3, 4));
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(1, 2) == (3, 4)").WithLocation(8, 37)
                );
        }

        [Fact]
        public void TestWithAnonymousTypes()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        var a = new { A = 1 };
        var b = new { B = 2 };
        var c = new { A = 1 };
        var d = new { B = 2 };

        System.Console.Write((a, b) == (a, b));
        System.Console.Write((a, b) == (c, d));
        System.Console.Write((a, b) != (c, d));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "TrueFalseTrue");
        }

        [Fact]
        public void TestWithAnonymousTypes2()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        var a = new { A = 1 };
        var b = new { B = 2 };

        System.Console.Write((a, b) == (b, a));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,30): error CS0019: Operator '==' cannot be applied to operands of type '<anonymous type: int A>' and '<anonymous type: int B>'
                //         System.Console.Write((a, b) == (b, a));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(a, b) == (b, a)").WithArguments("==", "<anonymous type: int A>", "<anonymous type: int B>").WithLocation(9, 30),
                // (9,30): error CS0019: Operator '==' cannot be applied to operands of type '<anonymous type: int B>' and '<anonymous type: int A>'
                //         System.Console.Write((a, b) == (b, a));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(a, b) == (b, a)").WithArguments("==", "<anonymous type: int B>", "<anonymous type: int A>").WithLocation(9, 30)
                );
        }

        [Fact]
        public void TestRefReturningElements()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        System.Console.Write((P, S()) == (1, ""hello""));
    }
    public static int p = 1;
    public static ref int P => ref p;
    public static string s = ""hello"";
    public static ref string S() => ref s;
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");
        }

        [Fact]
        public void TestChecked()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        try
        {
            checked
            {
                int ten = 10;
                System.Console.Write((2147483647 + ten, 1) == (0, 1));
            }
        }
        catch (System.OverflowException)
        {
            System.Console.Write(""overflow"");
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "overflow");
        }

        [Fact]
        public void TestUnchecked()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        unchecked
        {
            int ten = 10;
            System.Console.Write((2147483647 + ten, 1) == (-2147483639, 1));
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");
        }

        [Fact]
        public void TestInQuery()
        {
            var source = @"
using System.Linq;
public class C
{
    public static void Main()
    {
        var query =
            from a in new int[] { 1, 2, 2}
            where (a, 2) == (2, a)
            select a;

        foreach (var i in query)
        {
            System.Console.Write(i);
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "22");
        }

        [Fact]
        public void TestWithPointer()
        {
            var source = @"
public class C
{
    public unsafe static void M()
    {
        int x = 234;
        int y = 236;
        int* p1 = &x;
        int* p2 = &y;
        _ = (p1, p2) == (p1, p2);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (10,14): error CS0306: The type 'int*' may not be used as a type argument
                //         _ = (p1, p2) == (p1, p2);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "p1").WithArguments("int*").WithLocation(10, 14),
                // (10,18): error CS0306: The type 'int*' may not be used as a type argument
                //         _ = (p1, p2) == (p1, p2);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "p2").WithArguments("int*").WithLocation(10, 18),
                // (10,26): error CS0306: The type 'int*' may not be used as a type argument
                //         _ = (p1, p2) == (p1, p2);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "p1").WithArguments("int*").WithLocation(10, 26),
                // (10,30): error CS0306: The type 'int*' may not be used as a type argument
                //         _ = (p1, p2) == (p1, p2);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "p2").WithArguments("int*").WithLocation(10, 30),
                // (10,14): error CS0306: The type 'void*' may not be used as a type argument
                //         _ = (p1, p2) == (p1, p2);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "p1").WithArguments("void*").WithLocation(10, 14),
                // (10,18): error CS0306: The type 'void*' may not be used as a type argument
                //         _ = (p1, p2) == (p1, p2);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "p2").WithArguments("void*").WithLocation(10, 18),
                // (10,26): error CS0306: The type 'void*' may not be used as a type argument
                //         _ = (p1, p2) == (p1, p2);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "p1").WithArguments("void*").WithLocation(10, 26),
                // (10,30): error CS0306: The type 'void*' may not be used as a type argument
                //         _ = (p1, p2) == (p1, p2);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "p2").WithArguments("void*").WithLocation(10, 30)
                );
        }

        [Fact]
        public void TestOrder01()
        {
            var source = @"
using System;

public class C
{
    public static void Main()
    {
        X x = new X();
        Y y = new Y();
        Console.WriteLine((1, ((int, int))x) == (y, (1, 1)));
     }
}

class X
{
    public static implicit operator (short, short)(X x)
    {
        Console.WriteLine(""X-> (short, short)"");
        return (1, 1);
    }
}

class Y
{
    public static implicit operator int(Y x)
    {
        Console.WriteLine(""Y -> int"");
        return 1;
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: @"X-> (short, short)
Y -> int
True");
        }

        [Fact]
        public void TestOrder02()
        {
            var source = @"
using System;

public class C {
    public static void Main() {
        var result = (new B(1), new Nullable<B>(new A(2))) == (new A(3), new B(4));
        Console.WriteLine();
        Console.WriteLine(result);
    }
}

struct A
{
    public readonly int N;
    public A(int n)
    {
        this.N = n;
        Console.Write($""new A({ n }); "");
    }
}

struct B
{
    public readonly int N;
    public B(int n)
    {
        this.N = n;
        Console.Write($""new B({n}); "");
    }
    public static implicit operator B(A a)
    {
        Console.Write($""A({a.N})->"");
        return new B(a.N);
    }
    public static bool operator ==(B b1, B b2)
    {
        Console.Write($""B({b1.N})==B({b2.N}); "");
        return b1.N == b2.N;
    }
    public static bool operator !=(B b1, B b2)
    {
        Console.Write($""B({b1.N})!=B({b2.N}); "");
        return b1.N != b2.N;
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                    // (22,8): warning CS0660: 'B' defines operator == or operator != but does not override Object.Equals(object o)
                    // struct B
                    Diagnostic(ErrorCode.WRN_EqualityOpWithoutEquals, "B").WithArguments("B").WithLocation(22, 8),
                    // (22,8): warning CS0661: 'B' defines operator == or operator != but does not override Object.GetHashCode()
                    // struct B
                    Diagnostic(ErrorCode.WRN_EqualityOpWithoutGetHashCode, "B").WithArguments("B").WithLocation(22, 8)
                );
            CompileAndVerify(comp, expectedOutput: @"new B(1); new A(2); A(2)->new B(2); new A(3); new B(4); A(3)->new B(3); B(1)==B(3); 
False
");
        }

        [Fact, WorkItem(35958, "https://github.com/dotnet/roslyn/issues/35958")]
        public void TestMethodGroupConversionInTupleEquality_01()
        {
            var source = @"
using System;

public class C {
    public static void Main()
    {
        Action a = M;
        Console.WriteLine((a, M) == (M, a));
    }
    
    static void M() {}
}";
            var comp = CompileAndVerify(source, expectedOutput: "True");
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(35958, "https://github.com/dotnet/roslyn/issues/35958")]
        public void TestMethodGroupConversionInTupleEquality_02()
        {
            var source = @"
using System;

public class C {
    public static void Main()
    {
        Action a = () => {};
        Console.WriteLine((a, M) == (M, a));
    }
    
    static void M() {}
}";
            var comp = CompileAndVerify(source, expectedOutput: "False");
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(35958, "https://github.com/dotnet/roslyn/issues/35958")]
        public void TestMethodGroupConversionInTupleEquality_03()
        {
            var source = @"
using System;

public class C {
    public static void Main()
    {
        K k = null;
        Console.WriteLine((k, 1) == (M, 1));
    }
    
    static void M() {}
}

class K
{
    public static bool operator ==(K k, System.Action a) => true;
    public static bool operator !=(K k, System.Action a) => false;
    public override bool Equals(object other) => false;
    public override int GetHashCode() => 1;
}
";
            var comp = CompileAndVerify(source, expectedOutput: "True");
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(35958, "https://github.com/dotnet/roslyn/issues/35958")]
        public void TestInterpolatedStringConversionInTupleEquality_01()
        {
            var source = @"
using System;

public class C {
    public static void Main()
    {
        K k = null;
        Console.WriteLine((k, 1) == ($""frog"", 1));
    }
    
    static void M() {}
}

class K
{
    public static bool operator ==(K k, IFormattable a) => a.ToString() == ""frog"";
    public static bool operator !=(K k, IFormattable a) => false;
    public override bool Equals(object other) => false;
    public override int GetHashCode() => 1;
}
";
            var comp = CompileAndVerify(source, expectedOutput: "True");
            comp.VerifyDiagnostics();
        }
    }
}
