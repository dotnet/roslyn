// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.DefaultLiteral)]
    public class DefaultLiteralTests : CompilingTestBase
    {
        [Fact]
        public void TestCSharp7()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,17): error CS8058: Feature 'target-typed default operator' is experimental and unsupported; use '/features:defaultLiteral' to enable.
                //         int x = default;
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "default").WithArguments("target-typed default operator", "defaultLiteral").WithLocation(6, 17)
                );
        }

        [Fact]
        public void AssignmentToInt()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = default;
        System.Console.Write(x);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<DefaultLiteralSyntax>().Single();
            Assert.Equal("System.Int32", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.Equal("0", model.GetConstantValue(def).Value.ToString());
            Assert.True(model.GetConversion(def).IsNullLiteral);
        }

        [Fact]
        public void BadAssignment()
        {
            string source = @"
class C<T>
{
    static void M()
    {
        var x4 = default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics(
                // (6,13): error CS0815: Cannot assign default to an implicitly-typed variable
                //         var x4 = default;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x4 = default").WithArguments("default").WithLocation(6, 13)
                );
        }

        [Fact]
        public void AssignmentToRefType()
        {
            string source = @"
class C<T> where T : class
{
    static void M()
    {
        C<string> x1 = default;
        int? x2 = default;
        dynamic x3 = default;
        ITest x5 = default;
        T x6 = default;
        System.Console.Write($""{x1} {x2} {x3} {x5} {x6}"");
    }
}
interface ITest { }
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssignmentToStructType()
        {
            string source = @"
struct S
{
    static void M()
    {
        S x1 = default;
        System.Console.Write(x1);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<DefaultLiteralSyntax>().Single();
            Assert.Equal("S", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("S", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.True(model.GetConversion(def).IsNullLiteral);
        }

        [Fact]
        public void AssignmentToGenericType()
        {
            string source = @"
class C
{
    static void M<T>()
    {
        T x1 = default;
        System.Console.Write(x1);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<DefaultLiteralSyntax>().Single();
            Assert.Equal("T", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("T", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.True(model.GetConversion(def).IsNullLiteral);
        }

        [Fact]
        public void AmbiguousMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
    }
    static void M(int x) { }
    static void M(string x) { }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(int)' and 'C.M(string)'
                //         M(default);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(int)", "C.M(string)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void MethodWithRefParameters()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
    }
    static void M(string x) { System.Console.Write(x == null ? ""null"" : ""bad""); }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "null");
        }

        [Fact]
        public void MethodWithNullableParameters()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
    }
    static void M(int? x) { System.Console.Write(x.HasValue ? ""bad"" : ""null""); }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "null");
        }

        [Fact]
        public void CannotInferTypeArg()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
    }
    static void M<T>(T x) { }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,9): error CS0411: The type arguments for method 'C.M<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<T>(T)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void CannotInferTypeArg2()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default, null);
    }
    static void M<T>(T x, T y) where T : class { }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,9): error CS0411: The type arguments for method 'C.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(default, null);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<T>(T, T)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void InvocationOnDefault()
        {
            string source = @"
class C
{
    static void Main()
    {
        default.ToString();
        default[0].ToString();
        System.Console.Write(nameof(default));
        throw default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,16): error CS0023: Operator '.' cannot be applied to operand of type 'default'
                //         default.ToString();
                Diagnostic(ErrorCode.ERR_BadUnaryOp, ".").WithArguments(".", "default").WithLocation(6, 16),
                // (7,9): error CS0021: Cannot apply indexing with [] to an expression of type 'default'
                //         default[0].ToString();
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "default[0]").WithArguments("default").WithLocation(7, 9),
                // (8,37): error CS8081: Expression does not have a name.
                //         System.Console.Write(nameof(default));
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "default").WithLocation(8, 37),
                // (9,15): error CS0155: The type caught or thrown must be derived from System.Exception
                //         throw default;
                Diagnostic(ErrorCode.ERR_BadExceptionType, "default").WithLocation(9, 15)
                );
        }

        [Fact]
        public void Cast()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = (int)default;
        System.Console.Write(x);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void GenericCast()
        {
            string source = @"
class C
{
    static void M<T>()
    {
        const T x = default(T);
        const T y = (T)default;
        const object z = (T)default;
        System.Console.Write($""{x} {y} {z}"");
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics(
                // (6,15): error CS0283: The type 'T' cannot be declared const
                //         const T x = default(T);
                Diagnostic(ErrorCode.ERR_BadConstType, "T").WithArguments("T").WithLocation(6, 15),
                // (7,15): error CS0283: The type 'T' cannot be declared const
                //         const T y = (T)default;
                Diagnostic(ErrorCode.ERR_BadConstType, "T").WithArguments("T").WithLocation(7, 15),
                // (8,26): error CS0133: The expression being assigned to 'z' must be constant
                //         const object z = (T)default;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "(T)default").WithArguments("z").WithLocation(8, 26)
                );
        }

        [Fact]
        public void UserDefinedStruct()
        {
            string source = @"
struct S { }
class C
{
    static void M()
    {
        const S x = default(S);
        const S y = (S)default;
        const object z = (S)default;
        System.Console.Write($""{x} {y} {z}"");
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics(
                // (7,15): error CS0283: The type 'S' cannot be declared const
                //         const S x = default(S);
                Diagnostic(ErrorCode.ERR_BadConstType, "S").WithArguments("S").WithLocation(7, 15),
                // (8,15): error CS0283: The type 'S' cannot be declared const
                //         const S y = (S)default;
                Diagnostic(ErrorCode.ERR_BadConstType, "S").WithArguments("S").WithLocation(8, 15),
                // (9,26): error CS0133: The expression being assigned to 'z' must be constant
                //         const object z = (S)default;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "(S)default").WithArguments("z").WithLocation(9, 26)
                );
        }

        [Fact]
        public void ImplicitlyTypedArray()
        {
            string source = @"
class C
{
    static void Main()
    {
        var t = new[] { 1, default };
        System.Console.Write(t[1]);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<DefaultLiteralSyntax>().Single();
            Assert.Equal("System.Int32", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.Equal("0", model.GetConstantValue(def).Value.ToString());
        }

        [Fact]
        public void FailedImplicitlyTypedArray()
        {
            string source = @"
class C
{
    static void Main()
    {
        var t = new[] { default, default };
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,17): error CS0826: No best type found for implicitly-typed array
                //         var t = new[] { default, default };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { default, default }").WithLocation(6, 17)
                );
        }

        [Fact]
        public void ArrayConstruction()
        {
            string source = @"
class C
{
    static void Main()
    {
        var t = new object[default];
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Tuple()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int, int) t = (1, default);
        System.Console.Write(t.Item2);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe,
                        references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void TypeInferenceSucceeds()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default, 1);
    }
    static void M<T>(T x, T y) { System.Console.Write(x); }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void DefaultIdentifier()
        {
            string source = @"
class C
{
    static void Main()
    {
        int @default = 2;
        int x = default;
        System.Console.Write($""{x} {@default}"");
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<DefaultLiteralSyntax>().Single();
            Assert.Equal("System.Int32", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.Null(model.GetDeclaredSymbol(def));
        }

        [Fact]
        public void Return()
        {
            string source = @"
class C
{
    static int M()
    {
        return default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void YieldReturn()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;
class C
{
    static IEnumerable<int> M()
    {
        yield return default;
    }
    static IEnumerable M2()
    {
        yield return default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReturnNullableType()
        {
            string source = @"
class C
{
    static int? M()
    {
        return default;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ConstAndProperty()
        {
            string source = @"
class C
{
    const int x = default;
    static int P { get { return default; } }
    static void Main()
    {
        System.Console.Write($""{x}-{P}"");
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0-0");
        }

        [Fact]
        public void DynamicInvocation()
        {
            string source = @"
class C
{
    static void M1()
    {
        dynamic d = null;
        d.M2(default);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics(
                // (7,14): error CS9000: Cannot use a type-inferred default operator as an argument to a dynamically dispatched operation.
                //         d.M2(default);
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgDefault, "default").WithLocation(7, 14)
                );
        }

        [Fact]
        public void DefaultEqualsDefault()
        {
            string source = @"
class C
{
    static void Main()
    {
        System.Console.Write($""{default == default} {default != default}"");
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,33): error CS0034: Operator '==' is ambiguous on operands of type 'default' and 'default'
                //         System.Console.Write($"{default == default} {default != default}");
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "default == default").WithArguments("==", "default", "default").WithLocation(6, 33),
                // (6,54): error CS0034: Operator '!=' is ambiguous on operands of type 'default' and 'default'
                //         System.Console.Write($"{default == default} {default != default}");
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "default != default").WithArguments("!=", "default", "default").WithLocation(6, 54)
                );
        }

        [Fact]
        public void NormalInitializerType_Default()
        {
            var text = @"
class Program
{
    unsafe static void Main()
    {
        fixed (int* p = default)
        {
        }
    }
}
";
            // Confusing, but matches Dev10.
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.ExperimentalParseOptions)
                .VerifyDiagnostics(
                // (6,25): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                //         fixed (int* p = default)
                Diagnostic(ErrorCode.ERR_FixedNotNeeded, "default").WithLocation(6, 25)
                );
        }

        [Fact]
        public void TestErrorDefaultLiteralCollection()
        {
            var text = @"
class C
{
    static void Main()
    {
        foreach (int x in default) { }
        foreach (int x in null) { }
    }
}";

            var comp = CreateCompilationWithMscorlib(text, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,27): error CS9001: Use of default is not valid in this context
                //         foreach (int x in default) { }
                Diagnostic(ErrorCode.ERR_DefaultNotValid, "default").WithLocation(6, 27),
                // (7,27): error CS0186: Use of null is not valid in this context
                //         foreach (int x in null) { }
                Diagnostic(ErrorCode.ERR_NullNotValid, "null").WithLocation(7, 27)
                );
        }

        [Fact]
        public void QueryOnDefault()
        {
            string source =
@"static class C
{
    static void Main()
    {
        var q = from x in default select x;
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.ExperimentalParseOptions);
            compilation.VerifyDiagnostics(
                // (5,35): error CS9001: Use of default is not valid in this context
                //         var q = from x in default select x;
                Diagnostic(ErrorCode.ERR_DefaultNotValid, "select x").WithLocation(5, 35)
                );
        }

        [Fact]
        public void DefaultInConditionalExpression()
        {
            string source =
@"static class C
{
    static void Main()
    {
        var x = default ? 4 : 5;
        System.Console.Write(x);
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "5");
        }

        [Fact]
        public void AlwaysNonNull()
        {
            string source =
@"static class C
{
    static void Main()
    {
        System.Console.Write((int?)1 == default);
        System.Console.Write(default == (int?)1);
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.ExperimentalParseOptions);
            compilation.VerifyDiagnostics(
                // (5,30): warning CS0472: The result of the expression is always 'false' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         System.Console.Write((int?)1 == default);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "(int?)1 == default").WithArguments("false", "int", "int?").WithLocation(5, 30),
                // (6,30): warning CS0472: The result of the expression is always 'false' since a value of type 'int' is never equal to 'null' of type 'int?'
                //         System.Console.Write(default == (int?)1);
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool, "default == (int?)1").WithArguments("false", "int", "int?").WithLocation(6, 30)
                );
        }

        [Fact]
        public void ThrowDefault()
        {
            var text = @"
class C
{
    static void Main()
    {
        throw default;
    }
}";

            var comp = CreateCompilationWithMscorlib(text, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,15): error CS0155: The type caught or thrown must be derived from System.Exception
                //         throw default;
                Diagnostic(ErrorCode.ERR_BadExceptionType, "default").WithLocation(6, 15)
                );
        }

        [Fact]
        public void DefaultInAsOperator()
        {
            var text = @"
class C
{
    static void Main()
    {
        System.Console.Write(default as long);
    }
}";

            var comp = CreateCompilationWithMscorlib(text, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,30): error CS0077: The as operator must be used with a reference type or nullable type ('long' is a non-nullable value type)
                //         System.Console.Write(default as long);
                Diagnostic(ErrorCode.ERR_AsMustHaveReferenceType, "default as long").WithArguments("long").WithLocation(6, 30)
                );
        }

        [Fact]
        public void DefaultInIsPattern()
        {
            var text = @"
class C
{
    static void Main()
    {
        System.Console.Write(default is long);
    }
}";

            var comp = CreateCompilationWithMscorlib(text, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,30): error CS9001: Use of default is not valid in this context
                //         System.Console.Write(default is long);
                Diagnostic(ErrorCode.ERR_DefaultNotValid, "default is long").WithArguments("long").WithLocation(6, 30)
                );
        }

        [Fact]
        public void TypeVarCanBeDefault()
        {
            var source =
@"interface I { }
class A { }
class B<T1, T2, T3, T4, T5, T6, T7>
    where T2 : class
    where T3 : struct
    where T4 : new()
    where T5 : I
    where T6 : A
    where T7 : T1
{
    static void M()
    {
        T1 t1 = default;
        T2 t2 = default;
        T3 t3 = default;
        T4 t4 = default;
        T5 t5 = default;
        T6 t6 = default;
        T7 t7 = default;
        System.Console.Write($""{t1} {t2} {t3} {t4} {t5} {t6} {t7}"");
    }
    static T1 F1() { return default; }
    static T2 F2() { return default; }
    static T3 F3() { return default; }
    static T4 F4() { return default; }
    static T5 F5() { return default; }
    static T6 F6() { return default; }
    static T7 F7() { return default; }
}";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ExprTreeConvertedNullOnLHS()
        {
            var text =
@"using System;
using System.Linq.Expressions;

class Program
{
    Expression<Func<object>> testExpr = () => default ?? ""hello"";
}";

            var comp = CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics(
                // (6,47): error CS0845: An expression tree lambda may not contain a coalescing operator with a null or default literal left-hand side
                //     Expression<Func<object>> testExpr = () => default ?? "hello";
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsBadCoalesce, "default").WithLocation(6, 47)
                );
        }

        [Fact]
        public void NullableAndDefault()
        {
            var text =
@"class Program
{
    static void Main()
    {
        int? x = default;
        System.Console.Write(x.HasValue);
    }
}";

            var comp = CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "False");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var def = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DefaultLiteralSyntax>().Single();
            Assert.Equal("System.Int32?", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Int32?", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.True(model.GetConversion(def).IsNullLiteral);
        }

        [Fact]
        public void IndexingIntoArray()
        {
            string source = @"
class C
{
    static void Main()
    {
        int[] x = { 1, 2 };
        System.Console.Write(x[default]);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1");
        }

        [Fact]
        public void Lambda()
        {
            string source = @"
class C
{
    static void Main()
    {
        System.Console.Write(M()());
    }
    static System.Func<int> M()
    {
        return () => default;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void Switch()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(0);
    }
    static void M(int x)
    {
        switch (x)
        {
            case default:
                System.Console.Write(""default"");
                break;
            default:
                break;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "default");
        }

        [Fact]
        public void BinaryOperator()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = 0;
        if (x == default)
        {
            System.Console.Write(""0"");
        }
        if (default == x)
        {
            System.Console.Write(""1"");
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "01");
        }

        [Fact]
        public void OptionalParameter()
        {
            string source = @"
class C
{
    static void Main()
    {
        M();
    }
    static void M(int x = default)
    {
        System.Console.Write(x);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void ArraySize()
        {
            string source = @"
class C
{
    static void Main()
    {
        var a = new int[default];
        System.Console.Write(a.Length);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void TernaryOperator()
        {
            string source = @"
class C
{
    static void Main()
    {
        bool flag = true;
        var x = flag ? default : 1;
        System.Console.Write($""{x} {x.GetType().ToString()}"");
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 System.Int32");
        }

        [Fact]
        public void RefTernaryOperator()
        {
            string source = @"
class C
{
    static void Main()
    {
        bool flag = true;
        var x = flag ? default : ""hello"";
        System.Console.Write(x == null ? ""null"" : ""bad"");
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "null");
        }

        [Fact]
        public void ExplicitCast()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = (short)default;
        System.Console.Write(x);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<DefaultLiteralSyntax>().Single();
            Assert.Equal("System.Int16", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.Null(model.GetDeclaredSymbol(def));
            Assert.Equal("System.Int16", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.Equal((short)0, model.GetConstantValue(def).Value);
            Assert.True(model.GetConversion(def).IsIdentity);

            var conversionSyntax = nodes.OfType<CastExpressionSyntax>().Single();
            var conversionTypeInfo = model.GetTypeInfo(conversionSyntax);
            Assert.Equal("System.Int16", conversionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", conversionTypeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal((short)0, model.GetConstantValue(conversionSyntax).Value);
            Conversion conversion = model.GetConversion(conversionSyntax);
            Assert.True(conversion.IsNumeric);
            Assert.True(conversion.IsImplicit);
        }

        [Fact]
        public void NotAType()
        {
            string source = @"
class C
{
    static void Main()
    {
        default(System).ToString();
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,17): error CS0118: 'System' is a namespace but is used like a type
                //         default(System).ToString();
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "type").WithLocation(6, 17)
                );
        }
    }
}
