// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
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

        [Fact]
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

            // PROTOTYPE(tuple-equality) See if we can relax the restriction on requiring ValueTuple types

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

            var tupleY = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Last();
            Assert.Equal("(y, y)", tupleY.ToString());

            // PROTOTYPE(tuple-equality)
            return;

            //var tupleYSymbol = model.GetTypeInfo(tupleY);
            //Assert.Equal("(System.Byte, System.Byte)", tupleYSymbol.Type.ToTestDisplayString());
            //Assert.Equal("(System.Int32, System.Int32)", tupleYSymbol.ConvertedType.ToTestDisplayString());

            //var y = tupleY.Arguments[0].Expression;
            //var ySymbol = model.GetTypeInfo(y);
            //Assert.Equal("System.Byte", ySymbol.Type.ToTestDisplayString());
            //Assert.Equal("System.Int32", ySymbol.ConvertedType.ToTestDisplayString());
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
  // Code size       66 (0x42)
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
  IL_0013:  beq.s      IL_0018
  IL_0015:  ldc.i4.0
  IL_0016:  br.s       IL_001f
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       ""bool int?.HasValue.get""
  IL_001f:  brfalse.s  IL_0040
  IL_0021:  ldloc.0
  IL_0022:  ldfld      ""bool? System.ValueTuple<int?, bool?>.Item2""
  IL_0027:  stloc.3
  IL_0028:  ldc.i4.1
  IL_0029:  stloc.s    V_4
  IL_002b:  ldloca.s   V_3
  IL_002d:  call       ""bool bool?.GetValueOrDefault()""
  IL_0032:  ldloc.s    V_4
  IL_0034:  beq.s      IL_0038
  IL_0036:  ldc.i4.0
  IL_0037:  ret
  IL_0038:  ldloca.s   V_3
  IL_003a:  call       ""bool bool?.HasValue.get""
  IL_003f:  ret
  IL_0040:  ldc.i4.0
  IL_0041:  ret
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

        [Fact]
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
}";
            // PROTOTYPE(tuple-equality) We need to create a temp for `this`, otherwise it gets mutated
            var comp = CompileAndVerify(source, expectedOutput: "2 == 1, False");
            //comp.VerifyDiagnostics();
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

            // PROTOTYPE(tuple-equality) Semantic model
            return;

            //var tuple1 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(0);
            //var symbol1 = model.GetTypeInfo(tuple1);
            //Assert.Null(symbol1.Type);
            //Assert.Equal("(System.String, System.Int64)", symbol1.ConvertedType.ToTestDisplayString());

            //var tuple2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            //var symbol2 = model.GetTypeInfo(tuple2);
            //Assert.Equal("(System.String, System.Int32)", symbol2.Type.ToTestDisplayString());
            //Assert.Equal("(System.String, System.Int64)", symbol2.ConvertedType.ToTestDisplayString());
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

            // PROTOTYPE(tuple-equality) Semantic model
            return;

            //var tree = comp.SyntaxTrees[0];
            //var model = comp.GetSemanticModel(tree);

            //var tuple1 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(0);
            //Assert.Equal("(s, null)", tuple1.ToString());
            //var tupleType1 = model.GetTypeInfo(tuple1);
            //Assert.Null(tupleType1.Type);
            //Assert.Equal("(System.String, System.String)", tupleType1.ConvertedType.ToTestDisplayString());

            //var tuple2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            //Assert.Equal("(null, s)", tuple2.ToString());
            //var tupleType2 = model.GetTypeInfo(tuple2);
            //Assert.Null(tupleType2.Type);
            //Assert.Equal("(System.String, System.String)", tupleType2.ConvertedType.ToTestDisplayString());
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
        public void TestSimpleTupleAndTupleType()
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

            // PROTOTYPE(tuple-equality) Semantic model
            return;

            //var tree = comp.SyntaxTrees[0];
            //var model = comp.GetSemanticModel(tree);

            //var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            //Assert.Equal("(1L, 2)", tuple.ToString());
            //var tupleType = model.GetTypeInfo(tuple);
            //Assert.Equal("(System.Int64, System.Int32)", tupleType.Type.ToTestDisplayString());
            //Assert.Equal("(System.Int64, System.Int64)", tupleType.ConvertedType.ToTestDisplayString());
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

            // PROTOTYPE(tuple-equality) Semantic model
            return;

            //var tree = comp.SyntaxTrees[0];
            //var model = comp.GetSemanticModel(tree);

            //var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(3);
            //Assert.Equal("(1L, t2)", tuple.ToString());
            //var tupleType = model.GetTypeInfo(tuple);
            //Assert.Equal("(System.Int64, (System.Int32, System.String) t2)", tupleType.Type.ToTestDisplayString());
            //Assert.Equal("(System.Int64, (System.Int64, System.String))", tupleType.ConvertedType.ToTestDisplayString());
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

            // PROTOTYPE(tuple-equality) Semantic model: check type on last tuple and its elements (should be typeless)
            return;

            //var tree = comp.SyntaxTrees[0];
            //var model = comp.GetSemanticModel(tree);

            //var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            //Assert.Equal("(null, null)", tuple.ToString());
            //var tupleType = model.GetTypeInfo(tuple);
            //Assert.Null(tupleType.Type);
            //Assert.Equal("(System.String, System.String)", tupleType.ConvertedType.ToTestDisplayString());
        }

        [Fact(Skip = "PROTOTYPE(tuple-equality) Default")]
        public void TestTypedTupleAndDefault()
        {
            var source = @"
class C
{
    static void Main()
    {
        (string, string) t = (null, null);
        System.Console.Write(t == default);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,30): error CS8310: Operator '==' cannot be applied to operand 'default'
                //         System.Console.Write(t == default);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefault, "t == default").WithArguments("==", "default").WithLocation(7, 30)
                );
        }

        [Fact(Skip = "PROTOTYPE(tuple-equality) Default")]
        public void TestNullableTupleAndDefault()
        {
            var source = @"
class C
{
    static void Main()
    {
        (string, string)? t = (null, null);
        System.Console.Write(t == default);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,30): error CS8310: Operator '==' cannot be applied to operand 'default'
                //         System.Console.Write(t == default);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefault, "t == default").WithArguments("==", "default").WithLocation(7, 30)
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
            // PROTOTYPE(tuple-equality) Expand this test
        }

        [Fact(Skip = "PROTOTYPE(tuple-equality) Default")]
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
        _ = ns == default;
        _ = (ns, ns) == (default, default);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,13): error CS8310: Operator '==' cannot be applied to operand 'default'
                //         _ = ns == default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefault, "ns == default").WithArguments("==", "default").WithLocation(9, 13),
                // (10,13): error CS8310: Operator '==' cannot be applied to operand 'default'
                //         _ = (ns, ns) == (default, default);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefault, "(ns, ns) == (default, default)").WithArguments("==", "default").WithLocation(10, 13),
                // (10,13): error CS8310: Operator '==' cannot be applied to operand 'default'
                //         _ = (ns, ns) == (default, default);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefault, "(ns, ns) == (default, default)").WithArguments("==", "default").WithLocation(10, 13)
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

            // PROTOTYPE(tuple-equality) Semantic model
            return;

            //var defaultLiteral = literals.ElementAt(2);
            //Assert.Equal("default", defaultLiteral.ToString());
            //Assert.Equal("System.Object", model.GetTypeInfo(defaultLiteral).ConvertedType.ToTestDisplayString());

            //var defaultLiteral2 = literals.ElementAt(3);
            //Assert.Equal("default", defaultLiteral2.ToString());
            //Assert.Equal("System.Object", model.GetTypeInfo(defaultLiteral2).ConvertedType.ToTestDisplayString());
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

            // PROTOTYPE(tuple-equality) Semantic model: expect nulls to have type string
            return;

            //var tree = comp.SyntaxTrees[0];
            //var model = comp.GetSemanticModel(tree);

            //var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(3);
            //Assert.Equal("((null, null), t)", tuple.ToString());
            //var tupleType = model.GetTypeInfo(tuple);
            //Assert.Null(tupleType.Type);
            //Assert.Equal("((System.String, System.String), (System.String, System.String))",
            //    tupleType.ConvertedType.ToTestDisplayString());
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
            // PROTOTYPE(tuple-equality) Semantic model: check that null and tuples are typeless
        }

        [Fact]
        public void TestFailedInference()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write((null, null, null) == (null, () => { }, Main));
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,30): error CS0019: Operator '==' cannot be applied to operands of type '<null>' and 'lambda expression'
                //         System.Console.Write((null, null, null) == (null, () => { }, Main));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(null, null, null) == (null, () => { }, Main)").WithArguments("==", "<null>", "lambda expression").WithLocation(6, 30),
                // (6,30): error CS0019: Operator '==' cannot be applied to operands of type '<null>' and 'method group'
                //         System.Console.Write((null, null, null) == (null, () => { }, Main));
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(null, null, null) == (null, () => { }, Main)").WithArguments("==", "<null>", "method group").WithLocation(6, 30)
                );

            // PROTOTYPE(tuple-equality) Semantic model: check that null and tuples are typeless
            return;

            //var tree = comp.SyntaxTrees[0];
            //var model = comp.GetSemanticModel(tree);

            //var tuple1 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(0);
            //Assert.Equal("(null, null)", tuple1.ToString());
            //var tupleType1 = model.GetTypeInfo(tuple1);
            //Assert.Null(tupleType1.Type);
            //Assert.Equal("(System.Object, ?)", tupleType1.ConvertedType.ToTestDisplayString());

            //var tuple2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            //Assert.Equal("(null, () => { })", tuple2.ToString());
            //var tupleType2 = model.GetTypeInfo(tuple2);
            //Assert.Null(tupleType2.Type);
            //Assert.Equal("(System.Object, ?)", tupleType2.ConvertedType.ToTestDisplayString());
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

            // PROTOTYPE(tuple-equality) Semantic model
            return;

            //var tuple1 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(0);
            //Assert.Equal("(s, s)", tuple1.ToString());
            //var tupleType1 = model.GetTypeInfo(tuple1);
            //Assert.Equal("(System.String, System.String)", tupleType1.Type.ToTestDisplayString());
            //Assert.Equal("(System.Object, System.String)", tupleType1.ConvertedType.ToTestDisplayString());

            //var tuple2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(1);
            //Assert.Equal("(1, () => { })", tuple2.ToString());
            //var tupleType2 = model.GetTypeInfo(tuple2);
            //Assert.Null(tupleType2.Type);
            //Assert.Equal("(System.Object, ?)", tupleType2.ConvertedType.ToTestDisplayString());
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
            var comp = CreateCompilation(source, references: new[] { CSharpRef, SystemCoreRef }, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, references: new[] { CSharpRef, SystemCoreRef }, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, references: new[] { CSharpRef, SystemCoreRef }, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, references: new[] { CSharpRef, SystemCoreRef }, options: TestOptions.DebugExe);
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
    }
}";
            var comp = CreateCompilation(source, references: new[] { CSharpRef, SystemCoreRef }, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, references: new[] { CSharpRef, SystemCoreRef }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");
            // PROTOTYPE(tuple-equality) verify converted type on null
        }

        [Fact]
        public void TestBadConstraintOnTuple()
        {
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
                Diagnostic(ErrorCode.ERR_BadBinaryOps, @"("""", s1) == (null, s2)").WithArguments("==", "S", "S").WithLocation(6, 30)
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
        public void TestCustomOperatorPreferred()
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
        dynamic d1 = (1, 1);

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
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe,
                references: new[] { CSharpRef, SystemCoreRef });
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
        dynamic d1 = (1, 1, 1);

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
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe,
                references: new[] { CSharpRef, SystemCoreRef });
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
            // PROTOTYPE(tuple-equality) test case where conversion is on last tuple element on the left side

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
            CompileAndVerify(comp, expectedOutput: "True");
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

            // PROTOTYPE(tuple-equality) Semantic model
            return;

//            var comparison = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
//            var nt1 = comparison.Left;
//            var nt1Type = model.GetTypeInfo(nt1);
//            Assert.Equal("nt1", nt1.ToString());
//            Assert.Equal("(System.Int32, System.Int32)?", nt1Type.Type.ToTestDisplayString());
//            Assert.Equal("(System.Int32, System.Int64)?", nt1Type.ConvertedType.ToTestDisplayString());

//            var nt2 = comparison.Right;
//            var nt2Type = model.GetTypeInfo(nt2);
//            Assert.Equal("nt2", nt2.ToString());
//            Assert.Equal("(System.Byte, System.Int64)?", nt2Type.Type.ToTestDisplayString());
//            Assert.Equal("(System.Int32, System.Int64)?", nt2Type.ConvertedType.ToTestDisplayString());

//            verifier.VerifyIL("C.Compare", @"{
//  // Code size      217 (0xd9)
//  .maxstack  3
//  .locals init ((int, long)? V_0,
//                bool V_1,
//                System.ValueTuple<int, long> V_2,
//                (int, long)? V_3,
//                System.ValueTuple<int, long> V_4,
//                (int, int)? V_5,
//                (int, long)? V_6,
//                System.ValueTuple<int, int> V_7,
//                (byte, long)? V_8,
//                System.ValueTuple<byte, long> V_9)
//  IL_0000:  nop
//  IL_0001:  ldstr      ""{0} ""
//  IL_0006:  ldarg.0
//  IL_0007:  stloc.s    V_5
//  IL_0009:  ldloca.s   V_5
//  IL_000b:  call       ""bool (int, int)?.HasValue.get""
//  IL_0010:  brtrue.s   IL_001e
//  IL_0012:  ldloca.s   V_6
//  IL_0014:  initobj    ""(int, long)?""
//  IL_001a:  ldloc.s    V_6
//  IL_001c:  br.s       IL_0040
//  IL_001e:  ldloca.s   V_5
//  IL_0020:  call       ""(int, int) (int, int)?.GetValueOrDefault()""
//  IL_0025:  stloc.s    V_7
//  IL_0027:  ldloc.s    V_7
//  IL_0029:  ldfld      ""int System.ValueTuple<int, int>.Item1""
//  IL_002e:  ldloc.s    V_7
//  IL_0030:  ldfld      ""int System.ValueTuple<int, int>.Item2""
//  IL_0035:  conv.i8
//  IL_0036:  newobj     ""System.ValueTuple<int, long>..ctor(int, long)""
//  IL_003b:  newobj     ""(int, long)?..ctor((int, long))""
//  IL_0040:  stloc.0
//  IL_0041:  ldloca.s   V_0
//  IL_0043:  call       ""bool (int, long)?.HasValue.get""
//  IL_0048:  stloc.1
//  IL_0049:  ldarg.1
//  IL_004a:  stloc.s    V_8
//  IL_004c:  ldloca.s   V_8
//  IL_004e:  call       ""bool (byte, long)?.HasValue.get""
//  IL_0053:  brtrue.s   IL_0061
//  IL_0055:  ldloca.s   V_6
//  IL_0057:  initobj    ""(int, long)?""
//  IL_005d:  ldloc.s    V_6
//  IL_005f:  br.s       IL_0082
//  IL_0061:  ldloca.s   V_8
//  IL_0063:  call       ""(byte, long) (byte, long)?.GetValueOrDefault()""
//  IL_0068:  stloc.s    V_9
//  IL_006a:  ldloc.s    V_9
//  IL_006c:  ldfld      ""byte System.ValueTuple<byte, long>.Item1""
//  IL_0071:  ldloc.s    V_9
//  IL_0073:  ldfld      ""long System.ValueTuple<byte, long>.Item2""
//  IL_0078:  newobj     ""System.ValueTuple<int, long>..ctor(int, long)""
//  IL_007d:  newobj     ""(int, long)?..ctor((int, long))""
//  IL_0082:  stloc.3
//  IL_0083:  ldloc.1
//  IL_0084:  ldloca.s   V_3
//  IL_0086:  call       ""bool (int, long)?.HasValue.get""
//  IL_008b:  beq.s      IL_0090
//  IL_008d:  ldc.i4.0
//  IL_008e:  br.s       IL_00c8
//  IL_0090:  ldloc.1
//  IL_0091:  brtrue.s   IL_0096
//  IL_0093:  ldc.i4.1
//  IL_0094:  br.s       IL_00c8
//  IL_0096:  ldloca.s   V_0
//  IL_0098:  call       ""(int, long) (int, long)?.GetValueOrDefault()""
//  IL_009d:  stloc.2
//  IL_009e:  ldloca.s   V_3
//  IL_00a0:  call       ""(int, long) (int, long)?.GetValueOrDefault()""
//  IL_00a5:  stloc.s    V_4
//  IL_00a7:  ldloc.2
//  IL_00a8:  ldfld      ""int System.ValueTuple<int, long>.Item1""
//  IL_00ad:  ldloc.s    V_4
//  IL_00af:  ldfld      ""int System.ValueTuple<int, long>.Item1""
//  IL_00b4:  bne.un.s   IL_00c7
//  IL_00b6:  ldloc.2
//  IL_00b7:  ldfld      ""long System.ValueTuple<int, long>.Item2""
//  IL_00bc:  ldloc.s    V_4
//  IL_00be:  ldfld      ""long System.ValueTuple<int, long>.Item2""
//  IL_00c3:  ceq
//  IL_00c5:  br.s       IL_00c8
//  IL_00c7:  ldc.i4.0
//  IL_00c8:  box        ""bool""
//  IL_00cd:  call       ""string string.Format(string, object)""
//  IL_00d2:  call       ""void System.Console.Write(string)""
//  IL_00d7:  nop
//  IL_00d8:  ret
//}");
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

            // PROTOTYPE(tuple-equality) Semantic model
            return;

//            var tree = comp.SyntaxTrees.First();
//            var model = comp.GetSemanticModel(tree);

//            var comparison = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
//            var nt1 = comparison.Left;
//            var nt1Type = model.GetTypeInfo(nt1);
//            Assert.Equal("nt1", nt1.ToString());
//            Assert.Equal("(C, System.Int32)?", nt1Type.Type.ToTestDisplayString());
//            Assert.Equal("(System.Int32, System.Int32)?", nt1Type.ConvertedType.ToTestDisplayString());

//            var nt2 = comparison.Right;
//            var nt2Type = model.GetTypeInfo(nt2);
//            Assert.Equal("nt2", nt2.ToString());
//            Assert.Equal("(System.Int32, C)?", nt2Type.Type.ToTestDisplayString());
//            Assert.Equal("(System.Int32, System.Int32)?", nt2Type.ConvertedType.ToTestDisplayString());
//            verifier.VerifyIL("C.Compare", @"{
//  // Code size      226 (0xe2)
//  .maxstack  3
//  .locals init ((int, int)? V_0,
//                bool V_1,
//                System.ValueTuple<int, int> V_2,
//                (int, int)? V_3,
//                System.ValueTuple<int, int> V_4,
//                (C, int)? V_5,
//                (int, int)? V_6,
//                System.ValueTuple<C, int> V_7,
//                (int, C)? V_8,
//                System.ValueTuple<int, C> V_9)
//  IL_0000:  nop
//  IL_0001:  ldstr      ""{0} ""
//  IL_0006:  ldarg.0
//  IL_0007:  stloc.s    V_5
//  IL_0009:  ldloca.s   V_5
//  IL_000b:  call       ""bool (C, int)?.HasValue.get""
//  IL_0010:  brtrue.s   IL_001e
//  IL_0012:  ldloca.s   V_6
//  IL_0014:  initobj    ""(int, int)?""
//  IL_001a:  ldloc.s    V_6
//  IL_001c:  br.s       IL_0044
//  IL_001e:  ldloca.s   V_5
//  IL_0020:  call       ""(C, int) (C, int)?.GetValueOrDefault()""
//  IL_0025:  stloc.s    V_7
//  IL_0027:  ldloc.s    V_7
//  IL_0029:  ldfld      ""C System.ValueTuple<C, int>.Item1""
//  IL_002e:  call       ""int C.op_Implicit(C)""
//  IL_0033:  ldloc.s    V_7
//  IL_0035:  ldfld      ""int System.ValueTuple<C, int>.Item2""
//  IL_003a:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
//  IL_003f:  newobj     ""(int, int)?..ctor((int, int))""
//  IL_0044:  stloc.0
//  IL_0045:  ldloca.s   V_0
//  IL_0047:  call       ""bool (int, int)?.HasValue.get""
//  IL_004c:  stloc.1
//  IL_004d:  ldarg.1
//  IL_004e:  stloc.s    V_8
//  IL_0050:  ldloca.s   V_8
//  IL_0052:  call       ""bool (int, C)?.HasValue.get""
//  IL_0057:  brtrue.s   IL_0065
//  IL_0059:  ldloca.s   V_6
//  IL_005b:  initobj    ""(int, int)?""
//  IL_0061:  ldloc.s    V_6
//  IL_0063:  br.s       IL_008b
//  IL_0065:  ldloca.s   V_8
//  IL_0067:  call       ""(int, C) (int, C)?.GetValueOrDefault()""
//  IL_006c:  stloc.s    V_9
//  IL_006e:  ldloc.s    V_9
//  IL_0070:  ldfld      ""int System.ValueTuple<int, C>.Item1""
//  IL_0075:  ldloc.s    V_9
//  IL_0077:  ldfld      ""C System.ValueTuple<int, C>.Item2""
//  IL_007c:  call       ""int C.op_Implicit(C)""
//  IL_0081:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
//  IL_0086:  newobj     ""(int, int)?..ctor((int, int))""
//  IL_008b:  stloc.3
//  IL_008c:  ldloc.1
//  IL_008d:  ldloca.s   V_3
//  IL_008f:  call       ""bool (int, int)?.HasValue.get""
//  IL_0094:  beq.s      IL_0099
//  IL_0096:  ldc.i4.0
//  IL_0097:  br.s       IL_00d1
//  IL_0099:  ldloc.1
//  IL_009a:  brtrue.s   IL_009f
//  IL_009c:  ldc.i4.1
//  IL_009d:  br.s       IL_00d1
//  IL_009f:  ldloca.s   V_0
//  IL_00a1:  call       ""(int, int) (int, int)?.GetValueOrDefault()""
//  IL_00a6:  stloc.2
//  IL_00a7:  ldloca.s   V_3
//  IL_00a9:  call       ""(int, int) (int, int)?.GetValueOrDefault()""
//  IL_00ae:  stloc.s    V_4
//  IL_00b0:  ldloc.2
//  IL_00b1:  ldfld      ""int System.ValueTuple<int, int>.Item1""
//  IL_00b6:  ldloc.s    V_4
//  IL_00b8:  ldfld      ""int System.ValueTuple<int, int>.Item1""
//  IL_00bd:  bne.un.s   IL_00d0
//  IL_00bf:  ldloc.2
//  IL_00c0:  ldfld      ""int System.ValueTuple<int, int>.Item2""
//  IL_00c5:  ldloc.s    V_4
//  IL_00c7:  ldfld      ""int System.ValueTuple<int, int>.Item2""
//  IL_00cc:  ceq
//  IL_00ce:  br.s       IL_00d1
//  IL_00d0:  ldc.i4.0
//  IL_00d1:  box        ""bool""
//  IL_00d6:  call       ""string string.Format(string, object)""
//  IL_00db:  call       ""void System.Console.Write(string)""
//  IL_00e0:  nop
//  IL_00e1:  ret
//}");
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

            // PROTOTYPE(tuple-equality) Semantic model
            return;

            //var comparison = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
            //var tuple = comparison.Left;
            //var tupleType = model.GetTypeInfo(tuple);
            //Assert.Equal("(1, 2)", tuple.ToString());
            //Assert.Equal("(System.Int32, System.Int32)", tupleType.Type.ToTestDisplayString());
            //Assert.Equal("(System.Int32, System.Int32)?", tupleType.ConvertedType.ToTestDisplayString());

            //var nt = comparison.Right;
            //var ntType = model.GetTypeInfo(nt);
            //Assert.Equal("nt", nt.ToString());
            //Assert.Equal("(System.Byte, System.Int32)?", ntType.Type.ToTestDisplayString());
            //Assert.Equal("(System.Int32, System.Int32)?", ntType.ConvertedType.ToTestDisplayString());
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

            // PROTOTYPE(tuple-equality) Semantic model
            return;

            //var tree = comp.SyntaxTrees.First();
            //var model = comp.GetSemanticModel(tree);

            //var comparison = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().First();
            //Assert.Equal("(1, 2) == nt", comparison.ToString());
            //var tuple = comparison.Left;
            //var tupleType = model.GetTypeInfo(tuple);
            //Assert.Equal("(1, 2)", tuple.ToString());
            //Assert.Equal("(System.Int32, System.Int32)", tupleType.Type.ToTestDisplayString());
            //Assert.Equal("(System.Int32, System.Int32)?", tupleType.ConvertedType.ToTestDisplayString());

            //var nt = comparison.Right;
            //var ntType = model.GetTypeInfo(nt);
            //Assert.Equal("nt", nt.ToString());
            //Assert.Equal("(C, System.Int32)?", ntType.Type.ToTestDisplayString());
            //Assert.Equal("(System.Int32, System.Int32)?", ntType.ConvertedType.ToTestDisplayString());
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
            // PROTOTYPE(tuple-equality) Semantic model: check type of null
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

            // PROTOTYPE(tuple-equality) Semantic model
            return;

            //var tree = comp.SyntaxTrees.First();
            //var model = comp.GetSemanticModel(tree);
            //var comparison = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Last();
            //Assert.Equal("t.Rest == t.Rest", comparison.ToString());

            //var left = model.GetTypeInfo(comparison.Left);
            //Assert.Equal("ValueTuple<System.Int32?>", left.Type.ToTestDisplayString());
            //Assert.Equal("System.Object", left.ConvertedType.ToTestDisplayString());
            //Assert.True(left.Type.IsTupleType); // PROTOTYPE(tuple-equality) Need to investigate this
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
                    references: new[] { CSharpRef, SystemCoreRef }, options: TestOptions.DebugExe);

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
                    references: new[] { CSharpRef, SystemCoreRef }, options: TestOptions.DebugExe);

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
    }
}

// PROTOTYPE(tuple-equality)
// Test with tuple element names (semantic model)
// Test tuples with casts or nested casts
