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
            var comp = CreateStandardCompilation(source);
            comp.VerifyDiagnostics(
                // (6,17): error CS8107: Feature 'default literal' is not available in C# 7. Please use language version 7.1 or greater.
                //         int x = default;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "default").WithArguments("default literal", "7.1").WithLocation(6, 17)
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().Single();
            Assert.Equal("System.Int32", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.Equal("0", model.GetConstantValue(def).Value.ToString());
            Assert.True(model.GetConversion(def).IsNullLiteral);
        }

        [Fact]
        public void AssignmentToThisOnRefType()
        {
            string source = @"
public class C
{
    public int field;
    public C() => this = default;
    public static void Main()
    {
        new C();
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (5,19): error CS1604: Cannot assign to 'this' because it is read-only
                //     public C() => this = default;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(5, 19)
                );
        }

        [Fact]
        public void AssignmentToThisOnStructType()
        {
            string source = @"
public struct S
{
    public int field;
    public S(int x) => this = default;
    public static void Main()
    {
        new S(1);
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Equal("S", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("S", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void InAttributeParameter()
        {
            string source = @"
[Custom(z: default, y: default, x: default)]
class C
{
    [Custom(default, default)]
    void M()
    {
    }
}
public class CustomAttribute : System.Attribute
{
    public CustomAttribute(int x, string y, byte z = 0) { }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void InStringInterpolation()
        {
            string source = @"
class C
{
    static void Main()
    {
        System.Console.Write($""({default}) ({null})"");
    }
}
";

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "() ()");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Null(model.GetTypeInfo(def).Type); // Should be given a type. Follow-up issue: https://github.com/dotnet/roslyn/issues/18609
            Assert.Null(model.GetTypeInfo(def).ConvertedType);
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.False(model.GetConversion(def).IsNullLiteral);

            var nullSyntax = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("null", nullSyntax.ToString());
            Assert.Null(model.GetTypeInfo(nullSyntax).Type);
            Assert.Null(model.GetTypeInfo(nullSyntax).ConvertedType); // Should be given a type. Follow-up issue: https://github.com/dotnet/roslyn/issues/18609
            Assert.Null(model.GetSymbolInfo(nullSyntax).Symbol);
        }

        [Fact]
        public void InUsing()
        {
            string source = @"
class C
{
    static void Main()
    {
        using (default)
        {
            System.Console.Write(""ok"");
        }
        using (null) { }
    }
}
";

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ok");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Null(model.GetTypeInfo(def).Type);
            Assert.Null(model.GetTypeInfo(def).ConvertedType); // Should get a type. Follow-up issue: https://github.com/dotnet/roslyn/issues/18609
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.False(model.GetConversion(def).IsNullLiteral);

            var nullSyntax = nodes.OfType<LiteralExpressionSyntax>().ElementAt(2);
            Assert.Equal("null", nullSyntax.ToString());
            Assert.Null(model.GetTypeInfo(nullSyntax).Type);
            Assert.Null(model.GetTypeInfo(nullSyntax).ConvertedType); // Should get a type. Follow-up issue: https://github.com/dotnet/roslyn/issues/18609
        }

        [Fact]
        public void CannotAwaitDefault()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M()
    {
        await default;
    }
}
";

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,9): error CS4001: Cannot await 'default'
                //         await default;
                Diagnostic(ErrorCode.ERR_BadAwaitArgIntrinsic, "await default").WithArguments("default").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ReturningDefaultFromAsyncMethod()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    async Task<T> M2<T>()
    {
        await Task.Delay(0);
        return default;
    }
}
";

            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("default", def.ToString());
            Assert.Equal("T", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("T", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.True(model.GetConversion(def).IsNullLiteral);
        }

        [Fact]
        public void AsyncLambda()
        {
            string source = @"
class C
{
    static void F<T>(System.Threading.Tasks.Task<T> t) { }

    static void M()
    {
        F(async () => await default);
    }
}
";

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (8,9): error CS0411: The type arguments for method 'C.F<T>(Task<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(async () => await default);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("C.F<T>(System.Threading.Tasks.Task<T>)").WithLocation(8, 9)
                );
        }

        [Fact]
        public void RefReturnValue()
        {
            string source = @"
class C
{
    ref int M()
    {
        return default;
    }
}
";

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,9): error CS8150: By-value returns may only be used in methods that return by value
                //         return default;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(6, 9)
                );
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().Single();
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().Single();
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("default", def.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.Equal("0", model.GetConstantValue(def).Value.ToString());
        }

        [Fact]
        public void CollectionInitializer()
        {
            string source = @"
class C
{
    static void Main()
    {
        var t = new System.Collections.Generic.List<int> { 1, default };
        System.Console.Write($""{t[0]} {t[1]}"");
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 0");
        }

        [Fact]
        public void MiscDefaultErrors()
        {
            string source = @"
class C
{
    static void Main()
    {
        switch (default)
        {
            default:
                break;
        }
        lock (default)
        {
        }
        default();

        int i = ++default;
        var anon = new { Name = default };
        System.TypedReference tr = __makeref(default);
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (14,17): error CS1031: Type expected
                //         default();
                Diagnostic(ErrorCode.ERR_TypeExpected, ")").WithLocation(14, 17),
                // (6,17): error CS8119: The switch expression must be a value; found 'default'.
                //         switch (default)
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "default").WithArguments("default").WithLocation(6, 17),
                // (11,15): error CS0185: 'default' is not a reference type as required by the lock statement
                //         lock (default)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "default").WithArguments("default").WithLocation(11, 15),
                // (16,19): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         int i = ++default;
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "default").WithLocation(16, 19),
                // (17,26): error CS0828: Cannot assign 'default' to anonymous type property
                //         var anon = new { Name = default };
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "Name = default").WithArguments("default").WithLocation(17, 26),
                // (18,46): error CS1510: A ref or out value must be an assignable variable
                //         System.TypedReference tr = __makeref(default);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default").WithLocation(18, 46)
                );
        }

        [Fact]
        public void InChecked()
        {
            string source = @"
class C
{
    static void Main()
    {
        int i = checked(default);
        int j = checked(default + 4);
        System.Console.Write($""{i} {j}"");
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 4");
        }

        [Fact]
        public void WithUserDefinedPlusOperator()
        {
            string source = @"
struct S
{
    int field;
    static void Main()
    {
        S s = new S(40) + default;
        s += new S(2);
        s += default;
        System.Console.Write(s);
    }
    S(int i) { field = i; }
    public static S operator +(S left, S right) => new S(left.field + right.field);
    public override string ToString() => field.ToString();
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var first = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("new S(40) + default", first.Parent.ToString());
            Assert.Equal("S", model.GetTypeInfo(first).Type.ToTestDisplayString());

            var second = nodes.OfType<LiteralExpressionSyntax>().ElementAt(3);
            Assert.Equal("s += default", second.Parent.ToString());
            Assert.Equal("S", model.GetTypeInfo(second).Type.ToTestDisplayString());
        }

        [Fact]
        public void WithUserDefinedEqualityOperator()
        {
            string source = @"
struct S
{
    static void Main()
    {
        if (new S() == default)
        {
            System.Console.Write(""branch reached."");
        }
    }
    public static bool operator ==(S left, S right) { System.Console.Write(""operator reached. ""); return true; }
    public static bool operator !=(S left, S right) => false;
    public override bool Equals(object o) => throw null;
    public override int GetHashCode() => throw null;
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "operator reached. branch reached.");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var first = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", first.ToString());
            Assert.Equal("S", model.GetTypeInfo(first).Type.ToTestDisplayString());
        }

        [Fact]
        public void RefTypeAndValue()
        {
            string source = @"
class C
{
    static void Main()
    {
        System.Console.Write(1);
        var t = __reftype(default);
        System.Console.Write(2);
        try
        {
            int rv = __refvalue(default, int);
        }
        catch (System.InvalidCastException)
        {
            System.Console.Write($""3: {t == null}"");
        }
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123: True");
        }

        [Fact]
        public void InCompoundAssignmentAndExceptionFilter()
        {
            string source = @"
class C
{
    static void Main()
    {
        try
        {
            int i = 2;
            i += default;
            bool b = true;
            b &= default;
            System.Console.Write($""{true | default} {i} {b}"");
            throw new System.Exception();
        }
        catch (System.Exception) when (default)
        {
            System.Console.Write(""catch"");
        }
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (15,40): warning CS7095: Filter expression is a constant, consider removing the filter
                //         catch (System.Exception) when (default)
                Diagnostic(ErrorCode.WRN_FilterIsConstant, "default").WithLocation(15, 40),
                // (17,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("catch");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(17, 13)
                );
            //CompileAndVerify(comp, expectedOutput: "True 2 False"); // PEVerify failed with Branch out of the method. Follow-up issue: https://github.com/dotnet/roslyn/issues/18678
        }

        [Fact]
        public void PEVerifyErrorWithFalse()
        {
            string source = @"
class C
{
    static void Main()
    {
        try
        {
            throw new System.Exception();
        }
        catch (System.Exception) when (false)
        {
            System.Console.Write(""catch"");
        }
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (10,40): warning CS7095: Filter expression is a constant, consider removing the filter
                //         catch (System.Exception) when (false)
                Diagnostic(ErrorCode.WRN_FilterIsConstant, "false").WithLocation(10, 40),
                // (12,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("catch");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(12, 13)
                );
            //CompileAndVerify(comp); // PEVerify failed with Branch out of the method. Follow-up issue: https://github.com/dotnet/roslyn/issues/18678
        }

        [Fact]
        public void NegationUnaryOperatorOnDefault()
        {
            string source = @"
class C
{
    static void Main()
    {
        if (!default)
        {
            System.Console.WriteLine(""reached"");
        }
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "reached");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var def = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Equal("System.Boolean", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Boolean", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void NegationUnaryOperatorOnTypelessExpressions()
        {
            string source = @"
class C
{
    static void Main()
    {
        if (!Main || !null)
        {
        }
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,13): error CS0023: Operator '!' cannot be applied to operand of type 'method group'
                //         if (!Main || !null)
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "!Main").WithArguments("!", "method group"),
                // (6,22): error CS0023: Operator '!' cannot be applied to operand of type '<null>'
                //         if (!Main || !null)
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "!null").WithArguments("!", "<null>").WithLocation(6, 22)
                );
        }

        [Fact]
        public void ConditionalOnDefault()
        {
            string source = @"
class C
{
    static void Main()
    {
        if (default)
        {
            System.Console.Write(""if"");
        }

        while (default)
        {
            System.Console.Write(""while"");
        }

        for (int i = 0; default; i++)
        {
            System.Console.Write(""for"");
        }
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (8,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("if");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(8, 13),
                // (13,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("while");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(13, 13),
                // (18,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("for");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(18, 13)
                );
        }

        [Fact]
        public void ConditionalOnDefaultIsFalse()
        {
            string source = @"
class C
{
    static void Main()
    {
        if (default == false)
        {
            System.Console.Write(""reached"");
        }
        if (default == true)
        {
            System.Console.Write(""NEVER"");
        }
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (12,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("NEVER");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(12, 13)
                );
            CompileAndVerify(comp, expectedOutput: "reached");
        }

        [Fact]
        public void InFixed()
        {
            string source = @"
class C
{
    static unsafe void Main()
    {
        fixed (byte* p = default)
        {
        }
        fixed (byte* p = &default)
        {
        }
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (6,26): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                //         fixed (byte* p = default)
                Diagnostic(ErrorCode.ERR_FixedNotNeeded, "default"),
                // (9,27): error CS0211: Cannot take the address of the given expression
                //         fixed (byte* p = &default)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "default").WithLocation(9, 27)
                );
        }

        [Fact]
        public void Dereference()
        {
            string source = @"
class C
{
    static void M()
    {
        var p = *default;
        var q = default->F;
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,17): error CS0193: The * or -> operator must be applied to a pointer
                //         var p = *default;
                Diagnostic(ErrorCode.ERR_PtrExpected, "*default").WithLocation(6, 17),
                // (7,17): error CS0193: The * or -> operator must be applied to a pointer
                //         var q = default->F;
                Diagnostic(ErrorCode.ERR_PtrExpected, "default->F").WithLocation(7, 17)
                );
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe,
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void ArrayTypeInferredFromParams()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
        M(null);
    }
    static void M(params object[] x) { }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Equal("System.Object[]", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Object[]", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.Null(model.GetDeclaredSymbol(def));

            var nullSyntax = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("null", nullSyntax.ToString());
            Assert.Equal("System.Object[]", model.GetTypeInfo(nullSyntax).ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ParamsAmbiguity()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
    }
    static void M(params object[] x) { }
    static void M(params int[] x) { }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(params object[])' and 'C.M(params int[])'
                //         M(default);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(params object[])", "C.M(params int[])").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ParamsAmbiguity2()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(default);
    }
    static void M(params object[] x) { }
    static void M(int x) { }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(params object[])' and 'C.M(int)'
                //         M(default);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(params object[])", "C.M(int)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ParamsAmbiguity3()
        {
            string source = @"
struct S
{
    static void Main()
    {
        object o = null;
        S s = default;
        M(o, default);
        M(default, o);
        M(s, default);
        M(default, s);
    }
    static void M<T>(T x, params T[] y) { }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var first = nodes.OfType<LiteralExpressionSyntax>().ElementAt(2);
            Assert.Equal("(o, default)", first.Parent.Parent.ToString());
            Assert.Equal("System.Object[]", model.GetTypeInfo(first).Type.ToTestDisplayString());

            var second = nodes.OfType<LiteralExpressionSyntax>().ElementAt(3);
            Assert.Equal("(default, o)", second.Parent.Parent.ToString());
            Assert.Equal("System.Object", model.GetTypeInfo(second).Type.ToTestDisplayString());

            var third = nodes.OfType<LiteralExpressionSyntax>().ElementAt(4);
            Assert.Equal("(s, default)", third.Parent.Parent.ToString());
            Assert.Equal("S[]", model.GetTypeInfo(third).Type.ToTestDisplayString());

            var fourth = nodes.OfType<LiteralExpressionSyntax>().ElementAt(5);
            Assert.Equal("(default, s)", fourth.Parent.Parent.ToString());
            Assert.Equal("S", model.GetTypeInfo(fourth).Type.ToTestDisplayString());
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("default", def.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.Null(model.GetDeclaredSymbol(def));
        }

        [Fact]
        public void TestSpeculativeModel()
        {
            string source = @"
class C
{
    static void Main()
    {
        int i = 2;
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var digit = tree.GetCompilationUnitRoot().FindToken(source.IndexOf('2'));
            var expressionSyntax = SyntaxFactory.ParseExpression("default");
            var typeInfo = model.GetSpeculativeTypeInfo(digit.SpanStart, expressionSyntax, SpeculativeBindingOption.BindAsExpression);
            Assert.Null(typeInfo.Type);
            var symbol = model.GetSpeculativeSymbolInfo(digit.SpanStart, expressionSyntax, SpeculativeBindingOption.BindAsExpression);
            Assert.True(symbol.IsEmpty);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DefaultInEnum()
        {
            string source = @"
enum E
{
    DefaultEntry = default,
    OneEntry = default + 1
}
class C
{
    static void Main()
    {
        System.Console.Write($""{(int)E.DefaultEntry} {(int)E.OneEntry}"");
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1");
        }

        [Fact]
        public void DefaultInTypedEnum()
        {
            string source = @"
enum E : byte
{
    DefaultEntry = default,
    OneEntry = default + 1
}
class C
{
    static void Main()
    {
        System.Console.Write($""{(byte)E.DefaultEntry} {(byte)E.OneEntry}"");
    }
}
";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 1");
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0-0");
        }

        [Fact]
        public void InvocationOnDynamic()
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (7,14): error CS9000: Cannot use a default literal as an argument to a dynamically dispatched operation.
                //         d.M2(default);
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgDefaultLiteral, "default").WithLocation(7, 14)
                );
        }

        [Fact]
        public void DynamicInvocation()
        {
            string source = @"
class C
{
    static void Main()
    {
        F(default);
    }
    static void F(dynamic x)
    {
        System.Console.Write(x == null);
    }
}
";

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, references: new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            CreateStandardCompilation(text, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_1)
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

            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,27): error CS9001: Use of default literal is not valid in this context
                //         foreach (int x in default) { }
                Diagnostic(ErrorCode.ERR_DefaultLiteralNotValid, "default").WithLocation(6, 27),
                // (7,27): error CS0186: Use of null is not valid in this context
                //         foreach (int x in null) { }
                Diagnostic(ErrorCode.ERR_NullNotValid, "null").WithLocation(7, 27)
                );
        }

        [Fact]
        public void QueryOnDefault()
        {
            string source =
@"using System.Linq;
static class C
{
    static void Main()
    {
        var q = from x in default select x;
        var p = from x in new int[] { 1 } select default;
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.Regular7_1, references: new[] { SystemCoreRef });
            compilation.VerifyDiagnostics(
                // (6,35): error CS9001: Use of default literal is not valid in this context
                //         var q = from x in default select x;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNotValid, "select x").WithLocation(6, 35),
                // (7,43): error CS1942: The type of the expression in the select clause is incorrect.  Type inference failed in the call to 'Select'.
                //         var p = from x in new int[] { 1 } select default;
                Diagnostic(ErrorCode.ERR_QueryTypeInferenceFailed, "select").WithArguments("select", "Select").WithLocation(7, 43)
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
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.Regular7_1);
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

            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
    static void M<T, TClass>() where TClass : class
    {
        System.Console.Write(default as long);
        System.Console.Write(default as T);
        System.Console.Write(default as TClass);
    }
}";

            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (6,30): error CS0077: The as operator must be used with a reference type or nullable type ('long' is a non-nullable value type)
                //         System.Console.Write(default as long);
                Diagnostic(ErrorCode.ERR_AsMustHaveReferenceType, "default as long").WithArguments("long").WithLocation(6, 30),
                // (7,30): error CS0413: The type parameter 'T' cannot be used with the 'as' operator because it does not have a class type constraint nor a 'class' constraint
                //         System.Console.Write(default as T);
                Diagnostic(ErrorCode.ERR_AsWithTypeVar, "default as T").WithArguments("T").WithLocation(7, 30),
                // (8,30): warning CS0458: The result of the expression is always 'null' of type 'TClass'
                //         System.Console.Write(default as TClass);
                Diagnostic(ErrorCode.WRN_AlwaysNull, "default as TClass").WithArguments("TClass").WithLocation(8, 30)
                );
        }

        [Fact]
        public void DefaultInAsOperatorWithReferenceType()
        {
            var text = @"
class C
{
    static void Main()
    {
        System.Console.Write($""{default as C == null} {default as string == null}"");
    }
}";
            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,33): warning CS0458: The result of the expression is always 'null' of type 'C'
                //         System.Console.Write($"{default as C == null} {default as string == null}");
                Diagnostic(ErrorCode.WRN_AlwaysNull, "default as C").WithArguments("C").WithLocation(6, 33),
                // (6,56): warning CS0458: The result of the expression is always 'null' of type 'string'
                //         System.Console.Write($"{default as C == null} {default as string == null}");
                Diagnostic(ErrorCode.WRN_AlwaysNull, "default as string").WithArguments("string").WithLocation(6, 56)
                );
            CompileAndVerify(comp, expectedOutput: "True True");
        }

        [Fact]
        public void DefaultInputToTypeTest()
        {
            var text = @"
static class C
{
    static void M()
    {
        System.Console.Write(default is C);
    }
}";

            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (6,30): error CS0023: Operator 'is' cannot be applied to operand of type 'default'
                //         System.Console.Write(default is C);
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "default is C").WithArguments("is", "default").WithLocation(6, 30)
                );
        }

        [Fact]
        public void DefaultInputToConstantPattern()
        {
            var text = @"
class C
{
    static void M<T>()
    {
        System.Console.Write(default is long);
        System.Console.Write(default is string);
        System.Console.Write(default is default);
        System.Console.Write(default is T);
    }
}";

            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (6,30): error CS0023: Operator 'is' cannot be applied to operand of type 'default'
                //         System.Console.Write(default is long);
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "default is long").WithArguments("is", "default").WithLocation(6, 30),
                // (7,30): error CS0023: Operator 'is' cannot be applied to operand of type 'default'
                //         System.Console.Write(default is string);
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "default is string").WithArguments("is", "default").WithLocation(7, 30),
                // (8,30): error CS0023: Operator 'is' cannot be applied to operand of type 'default'
                //         System.Console.Write(default is default);
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "default is default").WithArguments("is", "default").WithLocation(8, 30),
                // (8,41): error CS0150: A constant value is expected
                //         System.Console.Write(default is default);
                Diagnostic(ErrorCode.ERR_ConstantExpected, "default").WithLocation(8, 41),
                // (9,30): error CS0023: Operator 'is' cannot be applied to operand of type 'default'
                //         System.Console.Write(default is T);
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "default is T").WithArguments("is", "default").WithLocation(9, 30)
                );
        }

        [Fact]
        public void DefaultInConstantPattern()
        {
            var text = @"
class C
{
    static void Main()
    {
        string hello = ""hello"";
        string nullString = null;
        int two = 2;
        int zero = 0;
        System.Console.Write($""{hello is default} {nullString is default} {two is default} {zero is default}"");
    }
}";

            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "False True False True");
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1);
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

            var comp = CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: TestOptions.Regular7_1);
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

            var comp = CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "False");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var def = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void V6SwitchWarns()
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (12,18): warning CS9002: Did you mean to use the default switch label (`default:`) rather than `case default:`? If you really mean to use the default literal, consider `case (default):` or another literal (`case 0:` or `case null:`) as appropriate.
                //             case default:
                Diagnostic(ErrorCode.WRN_DefaultInSwitch, "default").WithLocation(12, 18)
                );
            CompileAndVerify(comp, expectedOutput: "default");
        }

        [Fact]
        public void V7SwitchWarns()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(null);
    }
    static void M(object x)
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (12,18): warning CS9002: Did you mean to use the default switch label (`default:`) rather than `case default:`? If you really mean to use the default literal, consider `case (default):` or another literal (`case 0:` or `case null:`) as appropriate.
                //             case default:
                Diagnostic(ErrorCode.WRN_DefaultInSwitch, "default").WithLocation(12, 18)
                );
            CompileAndVerify(comp, expectedOutput: "default");
        }

        [Fact]
        public void V6SwitchWarningWorkaround()
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
            case (default):
                System.Console.Write(""default"");
                break;
            default:
                break;
        }
    }
}
";

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "default");
        }

        [Fact]
        public void V7SwitchWarningWorkaround()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(null);
    }
    static void M(object x)
    {
        switch (x)
        {
            case (default):
                System.Console.Write(""default"");
                break;
            default:
                break;
        }
    }
}
";

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void OptionalCancellationTokenParameter()
        {
            string source = @"
class C
{
    static void Main()
    {
        M();
    }
    static void M(System.Threading.CancellationToken x = default)
    {
        System.Console.Write(""ran"");
    }
}
";

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ran");
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().Single();
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,17): error CS0118: 'System' is a namespace but is used like a type
                //         default(System).ToString();
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "type").WithLocation(6, 17)
                );
        }
    }
}
