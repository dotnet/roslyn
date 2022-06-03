// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Linq;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.DefaultLiteral)]
    public class DefaultLiteralTests : CompilingTestBase
    {
        [Fact, WorkItem(30384, "https://github.com/dotnet/roslyn/issues/30384")]
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (6,17): error CS8107: Feature 'default literal' is not available in C# 7.0. Please use language version 7.1 or greater.
                //         int x = default;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "default").WithArguments("default literal", "7.1").WithLocation(6, 17)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().Single();
            Assert.Equal("System.Int32", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.Equal("0", model.GetConstantValue(def).Value.ToString());
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.True(model.GetConversion(def).IsDefaultLiteral);
        }

        [Fact]
        [WorkItem(19013, "https://github.com/dotnet/roslyn/issues/19013")]
        public void TestCSharp7Cascade()
        {
            string source = @"
using System.Threading;
using System.Threading.Tasks;

class C
{
    async Task M(CancellationToken t = default) { await Task.Delay(0); }
}
";
            var comp = CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (7,40): error CS8107: Feature 'default literal' is not available in C# 7.0. Please use language version 7.1 or greater.
                //     async Task M(CancellationToken t = default) { await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "default").WithArguments("default literal", "7.1").WithLocation(7, 40)
                );
        }

        [Fact]
        public void LambdaWithInference()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        new C<string, string>().M();
    }
}
class C<T1, T2>
{
    public void M(bool b = true)
    {
        var map = new C<string, string>();
        map.GetOrAdd("""", _ => default);
        map.GetOrAdd("""", _ => null);

        var map2 = new C<string, int>();
        map2.GetOrAdd("""", _ => default);

        var map3 = new C<string, (string, string)>();
        map3.GetOrAdd("""", _ => default);
        map3.GetOrAdd("""", _ => (null, null));

        var map4 = new C<string, string>();
        map4.GetOrAdd("""", _ => b switch { _ => null });
        map4.GetOrAdd("""", _ => b switch { _ => default });
    }
}
internal static class Extensions
{
    public static V GetOrAdd<K, V>(this C<K, V> dictionary, K key, System.Func<K, V> function)
    {
        var value = function(key);
        System.Console.Write(value is null ? ""null"" : value.ToString());
        System.Console.Write($""({typeof(V).ToString()})"");
        System.Console.Write("" "");
        return value;
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput:
                "null(System.String) null(System.String) 0(System.Int32) (, )(System.ValueTuple`2[System.String,System.String]) (, )(System.ValueTuple`2[System.String,System.String]) null(System.String) null(System.String) ");
        }

        [Fact, WorkItem(18609, "https://github.com/dotnet/roslyn/issues/18609")]
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.True(model.GetConversion(def).IsDefaultLiteral);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(18609, "https://github.com/dotnet/roslyn/issues/18609")]
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "() ()");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Equal("System.Object", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Object", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.True(model.GetConstantValue(def).HasValue);
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.True(model.GetConversion(def).IsDefaultLiteral);

            var nullSyntax = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("null", nullSyntax.ToString());
            Assert.Null(model.GetTypeInfo(nullSyntax).Type);
            Assert.Equal("System.Object", model.GetTypeInfo(nullSyntax).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(nullSyntax).Symbol);
        }

        [Fact, WorkItem(18609, "https://github.com/dotnet/roslyn/issues/18609")]
        public void InRawStringInterpolation()
        {
            string source = @"
class C
{
    static void Main()
    {
        System.Console.Write($""""""({default}) ({null})"""""");
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "() ()");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Equal("System.Object", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Object", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.True(model.GetConstantValue(def).HasValue);
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.True(model.GetConversion(def).IsDefaultLiteral);

            var nullSyntax = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("null", nullSyntax.ToString());
            Assert.Null(model.GetTypeInfo(nullSyntax).Type);
            Assert.Equal("System.Object", model.GetTypeInfo(nullSyntax).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(nullSyntax).Symbol);
        }

        [Fact, WorkItem(35684, "https://github.com/dotnet/roslyn/issues/35684")]
        [WorkItem(40791, "https://github.com/dotnet/roslyn/issues/40791")]
        public void ComparisonWithGenericType_Unconstrained()
        {
            string source = @"
class C
{
    static bool M<T>(T x = default)
    {
        return x == default // 1
            && x == default(T); // 2
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,16): error CS8761: Operator '==' cannot be applied to 'default' and operand of type 'T' because it is a type parameter that is not known to be a reference type
                //         return x == default // 1
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnUnconstrainedDefault, "x == default").WithArguments("==", "T").WithLocation(6, 16),
                // (7,16): error CS0019: Operator '==' cannot be applied to operands of type 'T' and 'T'
                //             && x == default(T); // 2
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == default(T)").WithArguments("==", "T", "T").WithLocation(7, 16)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var default1 = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", default1.ToString());
            Assert.Equal("T", model.GetTypeInfo(default1).Type.ToTestDisplayString());
            Assert.Equal("T", model.GetTypeInfo(default1).ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(default1).IsDefaultLiteral);

            var default2 = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("default", default2.ToString());
            Assert.Equal("?", model.GetTypeInfo(default2).Type.ToTestDisplayString());
            Assert.Equal("?", model.GetTypeInfo(default2).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(default2));
        }

        [Fact, WorkItem(40791, "https://github.com/dotnet/roslyn/issues/40791")]
        public void ComparisonWithGenericType_Unconstrained_Inequality()
        {
            string source = @"
class C
{
    static bool M<T>(T x = default)
    {
        return default != x // 1
            && default(T) != x; // 2
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,16): error CS8761: Operator '!=' cannot be applied to 'default' and operand of type 'T' because it is a type parameter that is not known to be a reference type
                //         return default != x // 1
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnUnconstrainedDefault, "default != x").WithArguments("!=", "T").WithLocation(6, 16),
                // (7,16): error CS0019: Operator '!=' cannot be applied to operands of type 'T' and 'T'
                //             && default(T) != x; // 2
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "default(T) != x").WithArguments("!=", "T", "T").WithLocation(7, 16)
                );
        }

        /// <summary>
        /// <seealso cref="BuiltInOperators.IsValidObjectEquality"/>
        /// </summary>
        [Fact, WorkItem(40791, "https://github.com/dotnet/roslyn/issues/40791")]
        public void ComparisonWithGenericType_VariousConstraints()
        {
            string source = @"
public class C { }
public interface I { }
public class C2<U>
{
    bool M1<T>(T x = default) where T : class
    { // equality is okay because T is a reference type
        return default != x
            && default(T) != x;
    }
    bool M2<T>(T x = default) where T : struct
    {
        return default != x // 1
            && default(T) != x; // 2
    }
    bool M3<T>(T x = default) where T : U
    {
        return default != x // 3
            && default(T) != x; // 4
    }
    bool M4<T>(T x = default) where T : C
    { // equality is okay because T is a reference type
        return default != x
            && default(T) != x;
    }
    bool M5<T>(T x = default) where T : I
    {
        return default != x // 5
            && default(T) != x; // 6
    }
    public virtual bool M6<T>(T x = default) where T : U
        => true;
}
public class Derived : C2<int?>
{
    public override bool M6<T>(T x = default)
    {
        return default != x // 7
            && default(T) != x; // 8
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (13,16): error CS8761: Operator '!=' cannot be applied to 'default' and operand of type 'T' because it is a type parameter that is not known to be a reference type
                //         return default != x // 1
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnUnconstrainedDefault, "default != x").WithArguments("!=", "T").WithLocation(13, 16),
                // (14,16): error CS0019: Operator '!=' cannot be applied to operands of type 'T' and 'T'
                //             && default(T) != x; // 2
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "default(T) != x").WithArguments("!=", "T", "T").WithLocation(14, 16),
                // (18,16): error CS8761: Operator '!=' cannot be applied to 'default' and operand of type 'T' because it is a type parameter that is not known to be a reference type
                //         return default != x // 3
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnUnconstrainedDefault, "default != x").WithArguments("!=", "T").WithLocation(18, 16),
                // (19,16): error CS0019: Operator '!=' cannot be applied to operands of type 'T' and 'T'
                //             && default(T) != x; // 4
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "default(T) != x").WithArguments("!=", "T", "T").WithLocation(19, 16),
                // (28,16): error CS8761: Operator '!=' cannot be applied to 'default' and operand of type 'T' because it is a type parameter that is not known to be a reference type
                //         return default != x // 5
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnUnconstrainedDefault, "default != x").WithArguments("!=", "T").WithLocation(28, 16),
                // (29,16): error CS0019: Operator '!=' cannot be applied to operands of type 'T' and 'T'
                //             && default(T) != x; // 6
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "default(T) != x").WithArguments("!=", "T", "T").WithLocation(29, 16),
                // (38,16): error CS8761: Operator '!=' cannot be applied to 'default' and operand of type 'T' because it is a type parameter that is not known to be a reference type
                //         return default != x // 7
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnUnconstrainedDefault, "default != x").WithArguments("!=", "T").WithLocation(38, 16),
                // (39,16): error CS0019: Operator '!=' cannot be applied to operands of type 'T' and 'T'
                //             && default(T) != x; // 8
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "default(T) != x").WithArguments("!=", "T", "T").WithLocation(39, 16)
                );
        }

        [Fact, WorkItem(38643, "https://github.com/dotnet/roslyn/issues/38643")]
        [WorkItem(40791, "https://github.com/dotnet/roslyn/issues/40791")]
        public void ComparisonWithGenericType_ValueType()
        {
            string source = @"
class C
{
    static bool M<T>(T x = default) where T : struct
    {
        return x == default // 1
            && x == default(T); // 2
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,16): error CS8761: Operator '==' cannot be applied to 'default' and operand of type 'T' because it is a type parameter that is not known to be a reference type
                //         return x == default // 1
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnUnconstrainedDefault, "x == default").WithArguments("==", "T").WithLocation(6, 16),
                // (7,16): error CS0019: Operator '==' cannot be applied to operands of type 'T' and 'T'
                //             && x == default(T); // 2
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == default(T)").WithArguments("==", "T", "T").WithLocation(7, 16)
                );
        }

        [Fact, WorkItem(38643, "https://github.com/dotnet/roslyn/issues/38643")]
        public void ComparisonWithGenericType_ReferenceType()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        M<C>();
    }
    static void M<T>() where T : class, new()
    {
        T t = new T();
        T nullT = null;

        System.Console.Write($""{t == default}{default == t} {nullT == default}{default == nullT} {t == default(T)}{default(T) == t}"");
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "FalseFalse TrueTrue FalseFalse");
        }

        [Fact, WorkItem(18609, "https://github.com/dotnet/roslyn/issues/18609")]
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,16): error CS8716: There is no target type for the default literal.
                //         using (default)
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 16)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Equal("?", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("?", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.False(model.GetConversion(def).IsDefaultLiteral);

            var nullSyntax = nodes.OfType<LiteralExpressionSyntax>().ElementAt(2);
            Assert.Equal("null", nullSyntax.ToString());
            Assert.Null(model.GetTypeInfo(nullSyntax).Type);
            Assert.Null(model.GetTypeInfo(nullSyntax).ConvertedType); // Should get a converted type https://github.com/dotnet/roslyn/issues/37798
        }

        [Fact]
        public void InUsing_WithVar()
        {
            string source = @"
class C
{
    static void Main()
    {
        using (var x = default)
        {
            System.Console.Write(""ok"");
        }
        using (var x = null) { }
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                    // (6,24): error CS8716: There is no target type for the default literal.
                    //         using (var x = default)
                    Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 24),
                    // (10,20): error CS0815: Cannot assign <null> to an implicitly-typed variable
                    //         using (var x = null) { }
                    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x = null").WithArguments("<null>").WithLocation(10, 20)
                    );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Equal("?", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("?", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.False(model.GetConversion(def).IsDefaultLiteral);

            var nullSyntax = nodes.OfType<LiteralExpressionSyntax>().ElementAt(2);
            Assert.Equal("null", nullSyntax.ToString());
            Assert.Null(model.GetTypeInfo(nullSyntax).Type);
            Assert.Null(model.GetTypeInfo(nullSyntax).ConvertedType); // Should get a converted type https://github.com/dotnet/roslyn/issues/37798
        }

        [Fact]
        public void InUsingDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        using var x = default;
        using var y = null;
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                    // (6,23): error CS8716: There is no target type for the default literal.
                    //         using var x = default;
                    Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 23),
                    // (7,19): error CS0815: Cannot assign <null> to an implicitly-typed variable
                    //         using var y = null;
                    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "y = null").WithArguments("<null>").WithLocation(7, 19)
                    );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Equal("?", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("?", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.False(model.GetConversion(def).IsDefaultLiteral);

            var nullSyntax = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("null", nullSyntax.ToString());
            Assert.Null(model.GetTypeInfo(nullSyntax).Type);
            Assert.Null(model.GetTypeInfo(nullSyntax).ConvertedType); // Should get a converted type https://github.com/dotnet/roslyn/issues/37798
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,15): error CS8716: There is no target type for the default literal.
                //         await default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 15)
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
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.True(model.GetConversion(def).IsDefaultLiteral);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,9): error CS8150: By-value returns may only be used in methods that return by value
                //         return default;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(6, 9)
                );
        }

        [Fact, WorkItem(18609, "https://github.com/dotnet/roslyn/issues/18609")]
        public void BadAssignment()
        {
            string source = @"
class C<T>
{
    static void M()
    {
        var x = default;
        var y = null;
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,17): error CS8716: There is no target type for the default literal.
                //         var x = default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 17),
                // (7,13): error CS0815: Cannot assign <null> to an implicitly-typed variable
                //         var y = null;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "y = null").WithArguments("<null>").WithLocation(7, 13)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Equal("?", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("?", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.False(model.GetConversion(def).IsDefaultLiteral);

            var nullSyntax = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("null", nullSyntax.ToString());
            Assert.Null(model.GetTypeInfo(nullSyntax).Type);
            Assert.Null(model.GetTypeInfo(nullSyntax).ConvertedType); // Should get a converted type https://github.com/dotnet/roslyn/issues/37798
        }

        [Fact]
        public void BadUnaryOperator()
        {
            string source = @"
class C<T>
{
    static void M()
    {
        var a = +default;
        var b = -default;
        var c = ~default;
        var d = !default;
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,18): error CS8716: There is no target type for the default literal.
                //         var a = +default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 18),
                // (7,18): error CS8716: There is no target type for the default literal.
                //         var b = -default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 18),
                // (8,18): error CS8716: There is no target type for the default literal.
                //         var c = ~default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(8, 18),
                // (9,18): error CS8716: There is no target type for the default literal.
                //         var d = !default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(9, 18)
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().Single();
            Assert.Equal("S", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("S", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.True(model.GetConversion(def).IsDefaultLiteral);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().Single();
            Assert.Equal("T", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("T", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.True(model.GetConversion(def).IsDefaultLiteral);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
    void M2()
    {
        default(C).ToString();
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,9): error CS8716: There is no target type for the default literal.
                //         default.ToString();
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 9),
                // (7,9): error CS8716: There is no target type for the default literal.
                //         default[0].ToString();
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 9),
                // (8,37): error CS8081: Expression does not have a name.
                //         System.Console.Write(nameof(default));
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "default").WithLocation(8, 37),
                // (9,15): error CS8716: There is no target type for the default literal.
                //         throw default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(9, 15),
                // (13,9): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'C' is null
                //         default(C).ToString();
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(C).ToString").WithArguments("C").WithLocation(13, 9)
                );

            var comp2 = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp2.VerifyDiagnostics(
                // (6,9): error CS8107: Feature 'default literal' is not available in C# 7.0. Please use language version 7.1 or greater.
                //         default.ToString();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "default").WithArguments("default literal", "7.1").WithLocation(6, 9),
                // (6,9): error CS8716: There is no target type for the default literal.
                //         default.ToString();
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 9),
                // (7,9): error CS8107: Feature 'default literal' is not available in C# 7.0. Please use language version 7.1 or greater.
                //         default[0].ToString();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "default").WithArguments("default literal", "7.1").WithLocation(7, 9),
                // (7,9): error CS8716: There is no target type for the default literal.
                //         default[0].ToString();
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 9),
                // (8,37): error CS8107: Feature 'default literal' is not available in C# 7.0. Please use language version 7.1 or greater.
                //         System.Console.Write(nameof(default));
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "default").WithArguments("default literal", "7.1").WithLocation(8, 37),
                // (9,15): error CS8107: Feature 'default literal' is not available in C# 7.0. Please use language version 7.1 or greater.
                //         throw default;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "default").WithArguments("default literal", "7.1").WithLocation(9, 15),
                // (9,15): error CS8716: There is no target type for the default literal.
                //         throw default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(9, 15),
                // (13,9): warning CS1720: Expression will always cause a System.NullReferenceException because the default value of 'C' is null
                //         default(C).ToString();
                Diagnostic(ErrorCode.WRN_DotOnDefault, "default(C).ToString").WithArguments("C").WithLocation(13, 9)
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (14,17): error CS1031: Type expected
                //         default();
                Diagnostic(ErrorCode.ERR_TypeExpected, ")").WithLocation(14, 17),
                // (6,17): error CS8716: There is no target type for the default literal.
                //         switch (default)
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 17),
                // (11,15): error CS8716: There is no target type for the default literal.
                //         lock (default)
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(11, 15),
                // (16,19): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         int i = ++default;
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "default").WithLocation(16, 19),
                // (17,33): error CS8716: There is no target type for the default literal.
                //         var anon = new { Name = default };
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(17, 33),
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
        System.Console.Write($""{i}"");
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void InChecked2()
        {
            string source = @"
class C
{
    static void Main()
    {
        int j = checked(default + 4);
        System.Console.Write($""{j}"");
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,25): error CS8310: Operator '+' cannot be applied to operand 'default'
                //         int j = checked(default + 4);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default + 4").WithArguments("+", "default").WithLocation(6, 25)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var addition = nodes.OfType<BinaryExpressionSyntax>().Single();
            Assert.Null(model.GetSymbolInfo(addition).Symbol);
        }

        [Fact]
        public void TestBinaryOperators()
        {
            string source = @"
class C
{
    static void Main()
    {
        var a = default + default;
        var b = default - default;
        var c = default & default;
        var d = default | default;
        var e = default ^ default;
        var f = default * default;
        var g = default / default;
        var h = default % default;
        var i = default >> default;
        var j = default << default;
        var k = default > default;
        var l = default < default;
        var m = default >= default;
        var n = default <= default;
        var o = default == default; // ambiguous
        var p = default != default; // ambiguous
        var q = default && default;
        var r = default || default;
        var s = default ?? default;
        var t = default >>> default;
    }
}
";
            var expected = new[]
            {
                // (6,17): error CS8310: Operator '+' cannot be applied to operand 'default'
                //         var a = default + default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default + default").WithArguments("+", "default").WithLocation(6, 17),
                // (7,17): error CS8310: Operator '-' cannot be applied to operand 'default'
                //         var b = default - default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default - default").WithArguments("-", "default").WithLocation(7, 17),
                // (8,17): error CS8310: Operator '&' cannot be applied to operand 'default'
                //         var c = default & default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default & default").WithArguments("&", "default").WithLocation(8, 17),
                // (9,17): error CS8310: Operator '|' cannot be applied to operand 'default'
                //         var d = default | default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default | default").WithArguments("|", "default").WithLocation(9, 17),
                // (10,17): error CS8310: Operator '^' cannot be applied to operand 'default'
                //         var e = default ^ default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default ^ default").WithArguments("^", "default").WithLocation(10, 17),
                // (11,17): error CS8310: Operator '*' cannot be applied to operand 'default'
                //         var f = default * default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default * default").WithArguments("*", "default").WithLocation(11, 17),
                // (12,17): error CS8310: Operator '/' cannot be applied to operand 'default'
                //         var g = default / default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default / default").WithArguments("/", "default").WithLocation(12, 17),
                // (13,17): error CS8310: Operator '%' cannot be applied to operand 'default'
                //         var h = default % default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default % default").WithArguments("%", "default").WithLocation(13, 17),
                // (14,17): error CS8310: Operator '>>' cannot be applied to operand 'default'
                //         var i = default >> default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default >> default").WithArguments(">>", "default").WithLocation(14, 17),
                // (15,17): error CS8310: Operator '<<' cannot be applied to operand 'default'
                //         var j = default << default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default << default").WithArguments("<<", "default").WithLocation(15, 17),
                // (16,17): error CS8310: Operator '>' cannot be applied to operand 'default'
                //         var k = default > default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default > default").WithArguments(">", "default").WithLocation(16, 17),
                // (17,17): error CS8310: Operator '<' cannot be applied to operand 'default'
                //         var l = default < default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default < default").WithArguments("<", "default").WithLocation(17, 17),
                // (18,17): error CS8310: Operator '>=' cannot be applied to operand 'default'
                //         var m = default >= default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default >= default").WithArguments(">=", "default").WithLocation(18, 17),
                // (19,17): error CS8310: Operator '<=' cannot be applied to operand 'default'
                //         var n = default <= default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default <= default").WithArguments("<=", "default").WithLocation(19, 17),
                // (20,17): error CS8315: Operator '==' is ambiguous on operands 'default' and 'default'
                //         var o = default == default; // ambiguous
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnDefault, "default == default").WithArguments("==", "default", "default").WithLocation(20, 17),
                // (21,17): error CS8315: Operator '!=' is ambiguous on operands 'default' and 'default'
                //         var p = default != default; // ambiguous
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnDefault, "default != default").WithArguments("!=", "default", "default").WithLocation(21, 17),
                // (22,17): error CS8716: There is no target type for the default literal.
                //         var q = default && default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(22, 17),
                // (22,28): error CS8716: There is no target type for the default literal.
                //         var q = default && default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(22, 28),
                // (23,17): error CS8716: There is no target type for the default literal.
                //         var r = default || default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(23, 17),
                // (23,28): error CS8716: There is no target type for the default literal.
                //         var r = default || default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(23, 28),
                // (24,17): error CS8716: There is no target type for the default literal.
                //         var s = default ?? default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(24, 17),
                // (25,17): error CS8310: Operator '>>>' cannot be applied to operand 'default'
                //         var t = default >>> default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default >>> default").WithArguments(">>>", "default").WithLocation(25, 17)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(expected);

            var comp2 = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp2.VerifyDiagnostics(expected);
        }

        [Fact]
        public void TestBinaryOperators2()
        {
            string source = @"
class C
{
    static void Main()
    {
        var a = default + 1;
        var b = default - 1;
        var c = default & 1;
        var d = default | 1;
        var e = default ^ 1;
        var f = default * 1;
        var g = default / 1;
        var h = default % 1;
        var i = default >> 1;
        var j = default << 1;
        var k = default > 1;
        var l = default < 1;
        var m = default >= 1;
        var n = default <= 1;
        var o = default == 1; // ok
        var p = default != 1; // ok
        var q = default && 1;
        var r = default || 1;
        var s = default ?? 1;
        var t = default ?? default(int?);
        var u = default >>> 1;
    }
}
";
            var expected = new[]
            {
                // (6,17): error CS8310: Operator '+' cannot be applied to operand 'default'
                //         var a = default + 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default + 1").WithArguments("+", "default").WithLocation(6, 17),
                // (7,17): error CS8310: Operator '-' cannot be applied to operand 'default'
                //         var b = default - 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default - 1").WithArguments("-", "default").WithLocation(7, 17),
                // (8,17): error CS8310: Operator '&' cannot be applied to operand 'default'
                //         var c = default & 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default & 1").WithArguments("&", "default").WithLocation(8, 17),
                // (9,17): error CS8310: Operator '|' cannot be applied to operand 'default'
                //         var d = default | 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default | 1").WithArguments("|", "default").WithLocation(9, 17),
                // (10,17): error CS8310: Operator '^' cannot be applied to operand 'default'
                //         var e = default ^ 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default ^ 1").WithArguments("^", "default").WithLocation(10, 17),
                // (11,17): error CS8310: Operator '*' cannot be applied to operand 'default'
                //         var f = default * 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default * 1").WithArguments("*", "default").WithLocation(11, 17),
                // (12,17): error CS8310: Operator '/' cannot be applied to operand 'default'
                //         var g = default / 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default / 1").WithArguments("/", "default").WithLocation(12, 17),
                // (13,17): error CS8310: Operator '%' cannot be applied to operand 'default'
                //         var h = default % 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default % 1").WithArguments("%", "default").WithLocation(13, 17),
                // (14,17): error CS8310: Operator '>>' cannot be applied to operand 'default'
                //         var i = default >> 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default >> 1").WithArguments(">>", "default").WithLocation(14, 17),
                // (15,17): error CS8310: Operator '<<' cannot be applied to operand 'default'
                //         var j = default << 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default << 1").WithArguments("<<", "default").WithLocation(15, 17),
                // (16,17): error CS8310: Operator '>' cannot be applied to operand 'default'
                //         var k = default > 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default > 1").WithArguments(">", "default").WithLocation(16, 17),
                // (17,17): error CS8310: Operator '<' cannot be applied to operand 'default'
                //         var l = default < 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default < 1").WithArguments("<", "default").WithLocation(17, 17),
                // (18,17): error CS8310: Operator '>=' cannot be applied to operand 'default'
                //         var m = default >= 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default >= 1").WithArguments(">=", "default").WithLocation(18, 17),
                // (19,17): error CS8310: Operator '<=' cannot be applied to operand 'default'
                //         var n = default <= 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default <= 1").WithArguments("<=", "default").WithLocation(19, 17),
                // (22,17): error CS8716: There is no target type for the default literal.
                //         var q = default && 1;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(22, 17),
                // (23,17): error CS8716: There is no target type for the default literal.
                //         var r = default || 1;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(23, 17),
                // (24,17): error CS8716: There is no target type for the default literal.
                //         var s = default ?? 1;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(24, 17),
                // (25,17): error CS8716: There is no target type for the default literal.
                //         var t = default ?? default(int?);
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(25, 17),
                // (20,13): warning CS0219: The variable 'o' is assigned but its value is never used
                //         var o = default == 1; // ok
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "o").WithArguments("o").WithLocation(20, 13),
                // (21,13): warning CS0219: The variable 'p' is assigned but its value is never used
                //         var p = default != 1; // ok
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "p").WithArguments("p").WithLocation(21, 13),
                // (26,17): error CS8310: Operator '>>>' cannot be applied to operand 'default'
                //         var u = default >>> 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default >>> 1").WithArguments(">>>", "default").WithLocation(26, 17)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(expected);

            var comp2 = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp2.VerifyDiagnostics(expected);
        }

        [Fact]
        public void TestBinaryOperators3()
        {
            string source = @"
class C
{
    static void Main()
    {
        var a = 1 + default;
        var b = 1 - default;
        var c = 1 & default;
        var d = 1 | default;
        var e = 1 ^ default;
        var f = 1 * default;
        var g = 1 / default;
        var h = 1 % default;
        var i = 1 >> default;
        var j = 1 << default;
        var k = 1 > default;
        var l = 1 < default;
        var m = 1 >= default;
        var n = 1 <= default;
        var o = 1 == default; // ok
        var p = 1 != default; // ok
        var q = 1 && default;
        var r = 1 || default;
        var s = new object() ?? default; // ok
        var t = 1 ?? default;
        var u = 1 >>> default;
    }
}
";
            var expected = new[]
            {
                // (6,17): error CS8310: Operator '+' cannot be applied to operand 'default'
                //         var a = 1 + default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 + default").WithArguments("+", "default").WithLocation(6, 17),
                // (7,17): error CS8310: Operator '-' cannot be applied to operand 'default'
                //         var b = 1 - default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 - default").WithArguments("-", "default").WithLocation(7, 17),
                // (8,17): error CS8310: Operator '&' cannot be applied to operand 'default'
                //         var c = 1 & default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 & default").WithArguments("&", "default").WithLocation(8, 17),
                // (9,17): error CS8310: Operator '|' cannot be applied to operand 'default'
                //         var d = 1 | default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 | default").WithArguments("|", "default").WithLocation(9, 17),
                // (10,17): error CS8310: Operator '^' cannot be applied to operand 'default'
                //         var e = 1 ^ default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 ^ default").WithArguments("^", "default").WithLocation(10, 17),
                // (11,17): error CS8310: Operator '*' cannot be applied to operand 'default'
                //         var f = 1 * default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 * default").WithArguments("*", "default").WithLocation(11, 17),
                // (12,17): error CS8310: Operator '/' cannot be applied to operand 'default'
                //         var g = 1 / default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 / default").WithArguments("/", "default").WithLocation(12, 17),
                // (13,17): error CS8310: Operator '%' cannot be applied to operand 'default'
                //         var h = 1 % default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 % default").WithArguments("%", "default").WithLocation(13, 17),
                // (14,17): error CS8310: Operator '>>' cannot be applied to operand 'default'
                //         var i = 1 >> default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 >> default").WithArguments(">>", "default").WithLocation(14, 17),
                // (15,17): error CS8310: Operator '<<' cannot be applied to operand 'default'
                //         var j = 1 << default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 << default").WithArguments("<<", "default").WithLocation(15, 17),
                // (16,17): error CS8310: Operator '>' cannot be applied to operand 'default'
                //         var k = 1 > default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 > default").WithArguments(">", "default").WithLocation(16, 17),
                // (17,17): error CS8310: Operator '<' cannot be applied to operand 'default'
                //         var l = 1 < default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 < default").WithArguments("<", "default").WithLocation(17, 17),
                // (18,17): error CS8310: Operator '>=' cannot be applied to operand 'default'
                //         var m = 1 >= default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 >= default").WithArguments(">=", "default").WithLocation(18, 17),
                // (19,17): error CS8310: Operator '<=' cannot be applied to operand 'default'
                //         var n = 1 <= default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 <= default").WithArguments("<=", "default").WithLocation(19, 17),
                // (22,22): error CS8716: There is no target type for the default literal.
                //         var q = 1 && default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(22, 22),
                // (23,22): error CS8716: There is no target type for the default literal.
                //         var r = 1 || default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(23, 22),
                // (25,17): error CS0019: Operator '??' cannot be applied to operands of type 'int' and 'default'
                //         var t = 1 ?? default;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "1 ?? default").WithArguments("??", "int", "default").WithLocation(25, 17),
                // (20,13): warning CS0219: The variable 'o' is assigned but its value is never used
                //         var o = 1 == default; // ok
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "o").WithArguments("o").WithLocation(20, 13),
                // (21,13): warning CS0219: The variable 'p' is assigned but its value is never used
                //         var p = 1 != default; // ok
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "p").WithArguments("p").WithLocation(21, 13),
                // (26,17): error CS8310: Operator '>>>' cannot be applied to operand 'default'
                //         var u = 1 >>> default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 >>> default").WithArguments(">>>", "default").WithLocation(26, 17)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(expected);

            var comp2 = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp2.VerifyDiagnostics(expected);
        }

        [Fact]
        public void TestBinaryOperators4()
        {
            string source = @"
class C
{
    static void Main()
    {
        var a = default(string) ?? """";
        var b = default(int?) ?? default;
        var c = null ?? default(int?);
        System.Console.Write($""{a == """"} {b == 0} {c == null}"");
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True True True");
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
        S s = new S(40);
        s += default;
    }
    S(int i) { field = i; }
    public static S operator +(S left, S right) => new S(left.field + right.field);
}
";
            var expected = new[]
            {
                // (8,9): error CS8310: Operator '+=' cannot be applied to operand 'default'
                //         s += default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "s += default").WithArguments("+=", "default").WithLocation(8, 9)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(expected);

            var comp2 = CreateCompilation(source, parseOptions: TestOptions.Regular7_3, options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(expected);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var defaultLiteral = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("s += default", defaultLiteral.Parent.ToString());
            Assert.Equal("?", model.GetTypeInfo(defaultLiteral).Type.ToTestDisplayString());
        }

        [Fact]
        public void EqualityComparison()
        {
            string template = @"
MODIFIER MyType
{
    static void Main()
    {
        TYPE x = VALUE;

        if ((x == default) != EQUAL) throw null;
        if ((default == x) != EQUAL) throw null;

        if ((x != default) == EQUAL) throw null;
        if ((default != x) == EQUAL) throw null;

        if ((x == default(TYPE)) != EQUAL) throw null;
        if ((x != default(TYPE)) == EQUAL) throw null;

        System.Console.Write(""Done"");
    }
}
";
            validate(modifier: "class", type: "int", value: "0", equal: "true", semanticType: "System.Int32");
            validate("class", "int", "1", "false", "System.Int32");
            validate("class", "int?", "null", "true", "System.Int32?");

            validate("class", "string", "null", "true", "System.String");
            validate("class", "string", "\"\"", "false", "System.String");

            validate("class", "MyType", "null", "true", "System.Object");

            // struct MyType doesn't have an == operator
            validate("struct", "MyType", "new MyType()", "false", semanticType: "?",
                // (8,14): error CS0019: Operator '==' cannot be applied to operands of type 'MyType' and 'default'
                //         if ((x == default) != false) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == default").WithArguments("==", "MyType", "default").WithLocation(8, 14),
                // (9,14): error CS0019: Operator '==' cannot be applied to operands of type 'default' and 'MyType'
                //         if ((default == x) != false) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "default == x").WithArguments("==", "default", "MyType").WithLocation(9, 14),
                // (11,14): error CS0019: Operator '!=' cannot be applied to operands of type 'MyType' and 'default'
                //         if ((x != default) == false) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x != default").WithArguments("!=", "MyType", "default").WithLocation(11, 14),
                // (12,14): error CS0019: Operator '!=' cannot be applied to operands of type 'default' and 'MyType'
                //         if ((default != x) == false) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "default != x").WithArguments("!=", "default", "MyType").WithLocation(12, 14),
                // (14,14): error CS0019: Operator '==' cannot be applied to operands of type 'MyType' and 'MyType'
                //         if ((x == default(MyType)) != false) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == default(MyType)").WithArguments("==", "MyType", "MyType").WithLocation(14, 14),
                // (15,14): error CS0019: Operator '!=' cannot be applied to operands of type 'MyType' and 'MyType'
                //         if ((x != default(MyType)) == false) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x != default(MyType)").WithArguments("!=", "MyType", "MyType").WithLocation(15, 14)
                );

            // struct MyType doesn't have an == operator, so no lifted == operator on MyType?
            validate("struct", "MyType?", "null", "true", semanticType: "?",
                // (8,14): error CS0019: Operator '==' cannot be applied to operands of type 'MyType?' and 'default'
                //         if ((x == default) != true) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == default").WithArguments("==", "MyType?", "default").WithLocation(8, 14),
                // (9,14): error CS0019: Operator '==' cannot be applied to operands of type 'default' and 'MyType?'
                //         if ((default == x) != true) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "default == x").WithArguments("==", "default", "MyType?").WithLocation(9, 14),
                // (11,14): error CS0019: Operator '!=' cannot be applied to operands of type 'MyType?' and 'default'
                //         if ((x != default) == true) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x != default").WithArguments("!=", "MyType?", "default").WithLocation(11, 14),
                // (12,14): error CS0019: Operator '!=' cannot be applied to operands of type 'default' and 'MyType?'
                //         if ((default != x) == true) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "default != x").WithArguments("!=", "default", "MyType?").WithLocation(12, 14),
                // (14,14): error CS0019: Operator '==' cannot be applied to operands of type 'MyType?' and 'MyType?'
                //         if ((x == default(MyType?)) != true) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == default(MyType?)").WithArguments("==", "MyType?", "MyType?").WithLocation(14, 14),
                // (15,14): error CS0019: Operator '!=' cannot be applied to operands of type 'MyType?' and 'MyType?'
                //         if ((x != default(MyType?)) == true) throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x != default(MyType?)").WithArguments("!=", "MyType?", "MyType?").WithLocation(15, 14)
                );

            void validate(string modifier, string type, string value, string equal, string semanticType, params DiagnosticDescription[] diagnostics)
            {
                validateLangVer(modifier, type, value, equal, semanticType, TestOptions.Regular7_2, diagnostics);
                validateLangVer(modifier, type, value, equal, semanticType, TestOptions.Regular, diagnostics);
            }

            void validateLangVer(string modifier, string type, string value, string equal, string semanticType, CSharpParseOptions parseOptions, params DiagnosticDescription[] diagnostics)
            {
                var source = template.Replace("MODIFIER", modifier).Replace("TYPE", type).Replace("VALUE", value).Replace("EQUAL", equal);
                var comp = CreateCompilation(source, parseOptions: parseOptions, options: TestOptions.DebugExe);
                if (diagnostics.Length == 0)
                {
                    comp.VerifyDiagnostics();
                    CompileAndVerify(comp, expectedOutput: "Done");
                }
                else
                {
                    comp.VerifyDiagnostics(diagnostics);
                }

                var tree = comp.SyntaxTrees.First();
                var model = comp.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var defaults = nodes.OfType<LiteralExpressionSyntax>().Where(l => l.ToString() == "default");
                Assert.True(defaults.Count() == 4);
                foreach (var @default in defaults)
                {
                    Assert.Equal("default", @default.ToString());
                    if (semanticType is null)
                    {
                        Assert.Null(model.GetTypeInfo(@default).Type);
                        Assert.Null(model.GetTypeInfo(@default).ConvertedType);
                    }
                    else
                    {
                        Assert.Equal(semanticType, model.GetTypeInfo(@default).Type.ToTestDisplayString());
                        Assert.Equal(semanticType, model.GetTypeInfo(@default).ConvertedType.ToTestDisplayString());
                    }
                }
            }
        }

        [Fact]
        public void EqualityComparison_Tuples()
        {
            string template = @"
MODIFIER MyType
{
    static void Main()
    {
        TYPE x = VALUE;

        if ((x == default) != EQUAL) throw null;
        if ((default == x) != EQUAL) throw null;

        if ((x != default) == EQUAL) throw null;
        if ((default != x) == EQUAL) throw null;

        if ((x == default(TYPE)) != EQUAL) throw null;
        if ((x != default(TYPE)) == EQUAL) throw null;

        System.Console.Write(""Done"");
    }
}
";

            validate("class", "(int, int)", "(1, 2)", "false", "(System.Int32, System.Int32)");
            validate("class", "(int, int)", "(0, 0)", "true", "(System.Int32, System.Int32)");
            validate("class", "(int, int)?", "null", "true", "(System.Int32, System.Int32)?");
            validate("class", "(int, int)?", "(0, 0)", "false", "(System.Int32, System.Int32)?");

            void validate(string modifier, string type, string value, string equal, string semanticType, params DiagnosticDescription[] diagnostics)
            {
                var source = template.Replace("MODIFIER", modifier).Replace("TYPE", type).Replace("VALUE", value).Replace("EQUAL", equal);
                var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3, options: TestOptions.DebugExe);
                if (diagnostics.Length == 0)
                {
                    comp.VerifyDiagnostics();
                    CompileAndVerify(comp, expectedOutput: "Done");
                }
                else
                {
                    comp.VerifyDiagnostics(diagnostics);
                }

                var tree = comp.SyntaxTrees.First();
                var model = comp.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var defaults = nodes.OfType<LiteralExpressionSyntax>().Where(l => l.ToString() == "default");
                Assert.True(defaults.Count() == 4);
                foreach (var @default in defaults)
                {
                    Assert.Equal("default", @default.ToString());
                    Assert.Equal(semanticType, model.GetTypeInfo(@default).Type.ToTestDisplayString());
                    Assert.Equal(semanticType, model.GetTypeInfo(@default).ConvertedType.ToTestDisplayString());
                }
            }
        }

        [Fact]
        public void EqualityComparison_StructWithComparison()
        {
            string template = @"
struct MyType
{
    int i;
    public MyType(int value)
    {
        i = value;
    }
    static void Main()
    {
        TYPE x = VALUE;

        if ((x == default) != EQUAL) throw null;
        if ((default == x) != EQUAL) throw null;

        if ((x != default) == EQUAL) throw null;
        if ((default != x) == EQUAL) throw null;

        if ((x == default(TYPE)) != EQUAL) throw null;
        if ((x != default(TYPE)) == EQUAL) throw null;

        System.Console.Write(""Done"");
    }
    public static bool operator==(MyType x, MyType y)
        => x.i == y.i;
    public static bool operator!=(MyType x, MyType y)
        => !(x == y);
    public override bool Equals(object o) => throw null;
    public override int GetHashCode() => throw null;
}
";

            validate("MyType", "new MyType(0)", "true", "MyType");
            validate("MyType", "new MyType(1)", "false", "MyType");

            validate("MyType?", "new MyType(0)", "false", "MyType?");
            validate("MyType?", "new MyType(1)", "false", "MyType?");
            validate("MyType?", "null", "true", "MyType?");

            void validate(string type, string value, string equal, string semanticType, params DiagnosticDescription[] diagnostics)
            {
                var source = template.Replace("TYPE", type).Replace("VALUE", value).Replace("EQUAL", equal);
                var comp = CreateCompilation(source, parseOptions: TestOptions.Regular, options: TestOptions.DebugExe);
                if (diagnostics.Length == 0)
                {
                    comp.VerifyDiagnostics();
                    CompileAndVerify(comp, expectedOutput: "Done");
                }
                else
                {
                    comp.VerifyDiagnostics(diagnostics);
                }

                var tree = comp.SyntaxTrees.First();
                var model = comp.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var defaults = nodes.OfType<LiteralExpressionSyntax>().Where(l => l.ToString() == "default");
                Assert.True(defaults.Count() == 4);
                foreach (var @default in defaults)
                {
                    Assert.Equal("default", @default.ToString());
                    Assert.Equal(semanticType, model.GetTypeInfo(@default).Type.ToTestDisplayString());
                    Assert.Equal(semanticType, model.GetTypeInfo(@default).ConvertedType.ToTestDisplayString());
                }
            }
        }

        [Fact]
        public void EqualityComparisonWithUserDefinedEqualityOperator()
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "operator reached. branch reached.");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var first = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", first.ToString());
            Assert.Equal("S", model.GetTypeInfo(first).Type.ToTestDisplayString());
            Assert.Equal("S", model.GetTypeInfo(first).ConvertedType.ToTestDisplayString());
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "123: True", verify: Verification.FailsILVerify);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (9,13): error CS8310: Operator '+=' cannot be applied to operand 'default'
                //             i += default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "i += default").WithArguments("+=", "default").WithLocation(9, 13),
                // (11,13): error CS8310: Operator '&=' cannot be applied to operand 'default'
                //             b &= default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "b &= default").WithArguments("&=", "default").WithLocation(11, 13),
                // (12,37): error CS8310: Operator '|' cannot be applied to operand 'default'
                //             System.Console.Write($"{true | default} {i} {b}");
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "true | default").WithArguments("|", "default").WithLocation(12, 37),
                // (15,40): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //         catch (System.Exception) when (default)
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "default").WithLocation(15, 40),
                // (17,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("catch");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(17, 13)
                );
        }

        [Fact]
        public void PEVerifyWithUnreachableCatch1()
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
        catch (System.Exception) when (default)
        {
            System.Console.Write(""catch"");
        }
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (10,40): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //         catch (System.Exception) when (false)
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "default").WithLocation(10, 40),
                // (12,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("catch");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(12, 13)
                );
            CompileAndVerify(comp);
        }

        [Fact]
        public void PEVerifyWithUnreachableCatch2()
        {
            string source = @"
class C
{
    static void Main()
    {
        try
        {
            SomeAction();
        }
        catch (System.NullReferenceException)
        {
            System.Console.Write(""NullReferenceException"");
        }
        catch
        {
            System.Console.Write(""OtherExceptions"");
        }
    }

    private static void SomeAction()
    {
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.Exception) when (default)
        {
            System.Console.Write(""Unreachable"");
        }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (26,40): warning CS8360: Filter expression is a constant, consider removing the filter
                //         catch (System.Exception) when (default)
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "default").WithLocation(26, 40),
                // (28,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("catch");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(28, 13)
                );
            CompileAndVerify(comp, expectedOutput: "NullReferenceException");
        }

        [Fact, WorkItem(18609, "https://github.com/dotnet/roslyn/issues/18609")]
        public void NegationUnaryOperatorOnDefault()
        {
            string source = @"
class C
{
    static void Main()
    {
        if (!default)
        {
            throw null;
        }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,14): error CS8716: There is no target type for the default literal.
                //         if (!default)
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 14)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var def = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ElementAt(0);
            Assert.Equal("default", def.ToString());
            Assert.Equal("?", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("?", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,13): error CS0023: Operator '!' cannot be applied to operand of type 'method group'
                //         if (!Main || !null)
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "!Main").WithArguments("!", "method group").WithLocation(6, 13),
                // (6,22): error CS8310: Operator '!' cannot be applied to operand '<null>'
                //         if (!Main || !null)
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "!null").WithArguments("!", "<null>").WithLocation(6, 22)
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (6,26): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (byte* p = default)
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "default").WithLocation(6, 26),
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,18): error CS8716: There is no target type for the default literal.
                //         var p = *default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 18),
                // (7,17): error CS8716: There is no target type for the default literal.
                //         var q = default->F;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 17)
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateCompilationWithMscorlib40(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe,
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var first = nodes.OfType<LiteralExpressionSyntax>().ElementAt(2);
            Assert.Equal("(o, default)", first.Parent.Parent.ToString());
            Assert.Equal("System.Object[]", model.GetTypeInfo(first).Type.ToTestDisplayString());
            Assert.Equal("System.Object[]", model.GetTypeInfo(first).ConvertedType.ToTestDisplayString());

            var second = nodes.OfType<LiteralExpressionSyntax>().ElementAt(3);
            Assert.Equal("(default, o)", second.Parent.Parent.ToString());
            Assert.Equal("System.Object", model.GetTypeInfo(second).Type.ToTestDisplayString());
            Assert.Equal("System.Object", model.GetTypeInfo(second).ConvertedType.ToTestDisplayString());

            var third = nodes.OfType<LiteralExpressionSyntax>().ElementAt(4);
            Assert.Equal("(s, default)", third.Parent.Parent.ToString());
            Assert.Equal("S[]", model.GetTypeInfo(third).Type.ToTestDisplayString());
            Assert.Equal("S[]", model.GetTypeInfo(third).ConvertedType.ToTestDisplayString());

            var fourth = nodes.OfType<LiteralExpressionSyntax>().ElementAt(5);
            Assert.Equal("(default, s)", fourth.Parent.Parent.ToString());
            Assert.Equal("S", model.GetTypeInfo(fourth).Type.ToTestDisplayString());
            Assert.Equal("S", model.GetTypeInfo(fourth).ConvertedType.ToTestDisplayString());
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0 2");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().ElementAt(1);
            Assert.Equal("default", def.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);

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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (5,16): error CS8310: Operator '+' cannot be applied to operand 'default'
                //     OneEntry = default + 1
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default + 1").WithArguments("+", "default").WithLocation(5, 16)
                );
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (5,16): error CS8310: Operator '+' cannot be applied to operand 'default'
                //     OneEntry = default + 1
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default + 1").WithArguments("+", "default").WithLocation(5, 16)
                );
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (7,14): error CS8716: There is no target type for the default literal.
                //         d.M2(default);
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 14)
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, references: new[] { CSharpRef }, options: TestOptions.DebugExe);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,33): error CS8315: Operator '==' is ambiguous on operands 'default' and 'default'
                //         System.Console.Write($"{default == default} {default != default}");
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnDefault, "default == default").WithArguments("==", "default", "default").WithLocation(6, 33),
                // (6,54): error CS8315: Operator '!=' is ambiguous on operands 'default' and 'default'
                //         System.Console.Write($"{default == default} {default != default}");
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnDefault, "default != default").WithArguments("!=", "default", "default").WithLocation(6, 54)
                );
        }

        [Fact]
        public void DefaultEqualsDefault_InCSharp7_3()
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

            // default == default is still disallowed in 7.3
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,33): error CS8315: Operator '==' is ambiguous on operands 'default' and 'default'
                //         System.Console.Write($"{default == default} {default != default}");
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnDefault, "default == default").WithArguments("==", "default", "default").WithLocation(6, 33),
                // (6,54): error CS8315: Operator '!=' is ambiguous on operands 'default' and 'default'
                //         System.Console.Write($"{default == default} {default != default}");
                Diagnostic(ErrorCode.ERR_AmbigBinaryOpsOnDefault, "default != default").WithArguments("!=", "default", "default").WithLocation(6, 54)
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
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_1)
                .VerifyDiagnostics(
                // (6,25): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (int* p = default)
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "default").WithLocation(6, 25)
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

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,27): error CS8716: There is no target type for the default literal.
                //         foreach (int x in default) { }
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 27),
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
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular7_1);
            compilation.VerifyDiagnostics(
                // (6,27): error CS8716: There is no target type for the default literal.
                //         var q = from x in default select x;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 27),
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
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular7_1);
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

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,15): error CS8716: There is no target type for the default literal.
                //         throw default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 15)
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

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (6,30): error CS8716: There is no target type for the default literal.
                //         System.Console.Write(default as long);
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 30),
                // (7,30): error CS8716: There is no target type for the default literal.
                //         System.Console.Write(default as T);
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 30),
                // (8,30): error CS8716: There is no target type for the default literal.
                //         System.Console.Write(default as TClass);
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(8, 30)
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
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,33): error CS8716: There is no target type for the default literal.
                //         System.Console.Write($"{default as C == null} {default as string == null}");
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 33),
                // (6,56): error CS8716: There is no target type for the default literal.
                //         System.Console.Write($"{default as C == null} {default as string == null}");
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 56)
                );
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

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (6,30): error CS8716: There is no target type for the default literal.
                //         System.Console.Write(default is C);
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 30)
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

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (6,30): error CS8716: There is no target type for the default literal.
                //         System.Console.Write(default is long);
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 30),
                // (7,30): error CS8716: There is no target type for the default literal.
                //         System.Console.Write(default is string);
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 30),
                // (8,30): error CS8716: There is no target type for the default literal.
                //         System.Console.Write(default is default);
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(8, 30),
                // (8,41): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         System.Console.Write(default is default);
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(8, 41),
                // (9,30): error CS8716: There is no target type for the default literal.
                //         System.Console.Write(default is T);
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(9, 30)
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

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (10,42): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         System.Console.Write($"{hello is default} {nullString is default} {two is default} {zero is default}");
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(10, 42),
                // (10,66): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         System.Console.Write($"{hello is default} {nullString is default} {two is default} {zero is default}");
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(10, 66),
                // (10,83): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         System.Console.Write($"{hello is default} {nullString is default} {two is default} {zero is default}");
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(10, 83),
                // (10,101): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         System.Console.Write($"{hello is default} {nullString is default} {two is default} {zero is default}");
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(10, 101)
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
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

            var comp = CreateCompilationWithMscorlib40AndSystemCore(text, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,47): error CS8716: There is no target type for the default literal.
                //     Expression<Func<object>> testExpr = () => default ?? "hello";
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 47)
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

            var comp = CreateCompilationWithMscorlib40AndSystemCore(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "False");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var def = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();
            Assert.Equal("System.Int32?", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Int32?", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.False(model.GetConversion(def).IsNullLiteral);
            Assert.True(model.GetConversion(def).IsDefaultLiteral);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (12,18): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //             case default:
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(12, 18)
                );
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (12,18): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //             case default:
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(12, 18)
                );
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (12,19): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //             case (default):
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(12, 19)
                );
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (12,19): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //             case (default):
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(12, 19)
                );
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "01");
        }

        [Fact]
        public void BinaryOperator_ValidObjectEquality()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        C c = new C();
        C nullC = null;

        System.Console.Write($""{c == default}{default == c} {nullC == default}{default == nullC} {c != default}{default != c} {nullC != default}{default != nullC}"");
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "FalseFalse TrueTrue TrueTrue FalseFalse");
        }

        [Fact]
        public void BinaryOperator_NullVersusDefault()
        {
            string source = @"
class C
{
    static void M(C x)
    {
        _ = null == default
            || default == null;
        _ = null != default
            || default != null;
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (6,13): error CS0034: Operator '==' is ambiguous on operands of type '<null>' and 'default'
                //         _ = null == default
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "null == default").WithArguments("==", "<null>", "default").WithLocation(6, 13),
                // (7,16): error CS0034: Operator '==' is ambiguous on operands of type 'default' and '<null>'
                //             || default == null;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "default == null").WithArguments("==", "default", "<null>").WithLocation(7, 16),
                // (8,13): error CS0034: Operator '!=' is ambiguous on operands of type '<null>' and 'default'
                //         _ = null != default
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "null != default").WithArguments("!=", "<null>", "default").WithLocation(8, 13),
                // (9,16): error CS0034: Operator '!=' is ambiguous on operands of type 'default' and '<null>'
                //             || default != null;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "default != null").WithArguments("!=", "default", "<null>").WithLocation(9, 16)
                );
        }

        [Fact]
        public void BinaryOperator_WithCustomComparisonOperator()
        {
            string source = @"
class C
{
    static void M(C x)
    {
        _ = x == default
            || default == x;
        _ = x != default
            || default != x;
    }
    public static bool operator ==(C one, C other) => throw null;
    public static bool operator !=(C one, C other) => throw null;
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (2,7): warning CS0660: 'C' defines operator == or operator != but does not override Object.Equals(object o)
                // class C
                Diagnostic(ErrorCode.WRN_EqualityOpWithoutEquals, "C").WithArguments("C").WithLocation(2, 7),
                // (2,7): warning CS0661: 'C' defines operator == or operator != but does not override Object.GetHashCode()
                // class C
                Diagnostic(ErrorCode.WRN_EqualityOpWithoutGetHashCode, "C").WithArguments("C").WithLocation(2, 7)
                );

            var tree = comp.SyntaxTrees.Last();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().First();
            Assert.Equal("default", def.ToString());
            Assert.Equal("C", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("C", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void BinaryOperator_WithCustomComparisonOperator_DifferentTypes()
        {
            string source = @"
class C
{
    static void M(C x)
    {
        _ = x == default;
        _ = x != default;
    }
    public static bool operator ==(C one, D other) => throw null;
    public static bool operator !=(C one, D other) => throw null;
}
class D
{
}
";
            // Note: default gets its type from overload resolution, not from target-typing to the other side
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (2,7): warning CS0660: 'C' defines operator == or operator != but does not override Object.Equals(object o)
                // class C
                Diagnostic(ErrorCode.WRN_EqualityOpWithoutEquals, "C").WithArguments("C").WithLocation(2, 7),
                // (2,7): warning CS0661: 'C' defines operator == or operator != but does not override Object.GetHashCode()
                // class C
                Diagnostic(ErrorCode.WRN_EqualityOpWithoutGetHashCode, "C").WithArguments("C").WithLocation(2, 7)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().First();
            Assert.Equal("default", def.ToString());
            Assert.Equal("D", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("D", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "null");
        }

        [Fact, WorkItem(18609, "https://github.com/dotnet/roslyn/issues/18609")]
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<LiteralExpressionSyntax>().Single();
            Assert.Equal("default", def.ToString());
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,17): error CS0118: 'System' is a namespace but is used like a type
                //         default(System).ToString();
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "type").WithLocation(6, 17)
                );
        }

        [Fact, WorkItem(18609, "https://github.com/dotnet/roslyn/issues/18609")]
        public void DefaultNullableParameter()
        {
            var text = @"
class C
{
    static void Main() { A(); B(); D(); E(); }

    static void A(int? x = default) => System.Console.Write($""{x.HasValue} "");
    static void B(int? x = default(int?)) => System.Console.Write($""{x.HasValue} "");
    static void D(int? x = default(byte?)) => System.Console.Write($""{x.HasValue} "");
    static void E(int? x = default(byte)) => System.Console.Write($""{x.HasValue}:{x.Value}"");
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "False False False True:0");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var default1 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();
            Assert.Equal("System.Int32?", model.GetTypeInfo(default1).Type.ToTestDisplayString());
            Assert.Equal("System.Int32?", model.GetTypeInfo(default1).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(default1).Symbol);
            Assert.False(model.GetConstantValue(default1).HasValue);
            Assert.False(model.GetConversion(default1).IsNullLiteral);
            Assert.True(model.GetConversion(default1).IsDefaultLiteral);

            var default2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DefaultExpressionSyntax>().ElementAt(0);
            Assert.Equal("System.Int32?", model.GetTypeInfo(default2).Type.ToTestDisplayString());
            Assert.Equal("System.Int32?", model.GetTypeInfo(default2).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(default2).Symbol);
            Assert.False(model.GetConstantValue(default2).HasValue);
            Assert.Equal(ConversionKind.Identity, model.GetConversion(default2).Kind);

            var default3 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DefaultExpressionSyntax>().ElementAt(1);
            Assert.Equal("System.Byte?", model.GetTypeInfo(default3).Type.ToTestDisplayString());
            Assert.Equal("System.Int32?", model.GetTypeInfo(default3).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(default3).Symbol);
            Assert.False(model.GetConstantValue(default3).HasValue);
            Assert.Equal(ConversionKind.ImplicitNullable, model.GetConversion(default3).Kind);

            var default4 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DefaultExpressionSyntax>().ElementAt(2);
            Assert.Equal("System.Byte", model.GetTypeInfo(default4).Type.ToTestDisplayString());
            Assert.Equal("System.Int32?", model.GetTypeInfo(default4).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(default4).Symbol);
            Assert.True(model.GetConstantValue(default4).HasValue);
            Conversion conversion = model.GetConversion(default4);
            Assert.Equal(ConversionKind.ImplicitNullable, conversion.Kind);
            Assert.Equal(ConversionKind.ImplicitNumeric, conversion.UnderlyingConversions.Single().Kind);
        }

        [Fact]
        public void TestDefaultInConstWithNullable()
        {
            string source = @"
struct S { }
class C<T> where T : struct
{
    const int? x1 = default;
    const int? x2 = default(int?);
    const int? x3 = (default);
    const S? y1 = default;
    const S? y2 = default(S?);
    const T? z1 = default;
    const T? z2 = default(T?);
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (5,5): error CS0283: The type 'int?' cannot be declared const
                //     const int? x1 = default;
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("int?").WithLocation(5, 5),
                // (6,5): error CS0283: The type 'int?' cannot be declared const
                //     const int? x2 = default(int?);
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("int?").WithLocation(6, 5),
                // (7,5): error CS0283: The type 'int?' cannot be declared const
                //     const int? x3 = (default);
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("int?").WithLocation(7, 5),
                // (8,5): error CS0283: The type 'S?' cannot be declared const
                //     const S? y1 = default;
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("S?").WithLocation(8, 5),
                // (9,5): error CS0283: The type 'S?' cannot be declared const
                //     const S? y2 = default(S?);
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("S?").WithLocation(9, 5),
                // (10,5): error CS0283: The type 'T?' cannot be declared const
                //     const T? z1 = default;
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("T?").WithLocation(10, 5),
                // (11,5): error CS0283: The type 'T?' cannot be declared const
                //     const T? z2 = default(T?);
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("T?").WithLocation(11, 5),
                // (6,21): error CS0133: The expression being assigned to 'C<T>.x2' must be constant
                //     const int? x2 = default(int?);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default(int?)").WithArguments("C<T>.x2").WithLocation(6, 21),
                // (7,21): error CS0133: The expression being assigned to 'C<T>.x3' must be constant
                //     const int? x3 = (default);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "(default)").WithArguments("C<T>.x3").WithLocation(7, 21),
                // (8,19): error CS0133: The expression being assigned to 'C<T>.y1' must be constant
                //     const S? y1 = default;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default").WithArguments("C<T>.y1").WithLocation(8, 19),
                // (9,19): error CS0133: The expression being assigned to 'C<T>.y2' must be constant
                //     const S? y2 = default(S?);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default(S?)").WithArguments("C<T>.y2").WithLocation(9, 19),
                // (10,19): error CS0133: The expression being assigned to 'C<T>.z1' must be constant
                //     const T? z1 = default;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default").WithArguments("C<T>.z1").WithLocation(10, 19),
                // (11,19): error CS0133: The expression being assigned to 'C<T>.z2' must be constant
                //     const T? z2 = default(T?);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default(T?)").WithArguments("C<T>.z2").WithLocation(11, 19),
                // (5,21): error CS0133: The expression being assigned to 'C<T>.x1' must be constant
                //     const int? x1 = default;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default").WithArguments("C<T>.x1").WithLocation(5, 21)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var defaultLiterals = nodes.OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(4, defaultLiterals.Length);
            foreach (var value in defaultLiterals)
            {
                Assert.False(model.GetConstantValue(value).HasValue);
            }
        }

        [Fact]
        public void TestDefaultInOptionalParameterWithNullable()
        {
            string source = @"
struct S { }
class C
{
    public static void Main()
    {
        M<long>();
    }
    static void M<T>(
        int? x1 = default,
        int? x2 = default(int?),
        int? x3 = (default),
        S? y1 = default,
        S? y2 = default(S?),
        T? z1 = default,
        T? z2 = default(T?)) where T : struct
    {
        System.Console.WriteLine($""{x1.HasValue} {x2.HasValue} {x3.HasValue} {y1.HasValue} {y2.HasValue} {z1.HasValue} {z2.HasValue}"");
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "False False False False False False False");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var parameters = nodes.OfType<ParameterSyntax>().ToArray();
            Assert.Equal(7, parameters.Length);
            foreach (var parameter in parameters)
            {
                var defaultValue = parameter.Default.Value;
                Assert.False(model.GetConstantValue(defaultValue).HasValue);
            }
        }

        [Fact]
        public void TestDefaultInAttributeOptionalParameterWithNullable()
        {
            string source = @"
public struct S { }
public class A : System.Attribute
{
    public A(
        int? x1 = default,
        int? x2 = default(int?),
        int? x3 = (default),
        S? y1 = default,
        S? y2 = default(S?))
    {
    }
}
[A]
class C
{
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (14,2): error CS0181: Attribute constructor parameter 'x1' has type 'int?', which is not a valid attribute parameter type
                // [A]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A").WithArguments("x1", "int?").WithLocation(14, 2),
                // (14,2): error CS0181: Attribute constructor parameter 'x2' has type 'int?', which is not a valid attribute parameter type
                // [A]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A").WithArguments("x2", "int?").WithLocation(14, 2),
                // (14,2): error CS0181: Attribute constructor parameter 'x3' has type 'int?', which is not a valid attribute parameter type
                // [A]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A").WithArguments("x3", "int?").WithLocation(14, 2),
                // (14,2): error CS0181: Attribute constructor parameter 'y1' has type 'S?', which is not a valid attribute parameter type
                // [A]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A").WithArguments("y1", "S?").WithLocation(14, 2),
                // (14,2): error CS0181: Attribute constructor parameter 'y2' has type 'S?', which is not a valid attribute parameter type
                // [A]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A").WithArguments("y2", "S?").WithLocation(14, 2)
                );
        }
    }
}
