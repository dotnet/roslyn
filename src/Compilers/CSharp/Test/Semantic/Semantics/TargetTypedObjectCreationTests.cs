// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class TargetTypedObjectCreationTests : CSharpTestBase
    {
        private static readonly CSharpParseOptions TargetTypedObjectCreationTestOptions = TestOptions.RegularPreview;

        private static CSharpCompilation CreateCompilation(string source, CSharpCompilationOptions options = null, IEnumerable<MetadataReference> references = null)
        {
            return CSharpTestBase.CreateCompilation(source, options: options, parseOptions: TargetTypedObjectCreationTestOptions, references: references);
        }

        [Fact]
        public void TestInLocal()
        {
            var source = @"
using System;

struct S
{
}

class C
{
    public static void Main()
    {
        C v1 = new();
        S v2 = new();
        S? v3 = new();
        Console.Write(v1);
        Console.Write(v2);
        Console.Write(v3);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "CSS");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().ToArray();

            assert(0, type: "C", convertedType: "C", symbol: "C..ctor()", ConversionKind.Identity);
            assert(1, type: "S", convertedType: "S", symbol: "S..ctor()", ConversionKind.Identity);
            assert(2, type: "S", convertedType: "S?", symbol: "S..ctor()", ConversionKind.ImplicitNullable);

            void assert(int index, string type, string convertedType, string symbol, ConversionKind conversionKind)
            {
                var @new = nodes[index];
                Assert.Equal(type, model.GetTypeInfo(@new).Type.ToTestDisplayString());
                Assert.Equal(convertedType, model.GetTypeInfo(@new).ConvertedType.ToTestDisplayString());
                Assert.Equal(symbol, model.GetSymbolInfo(@new).Symbol.ToTestDisplayString());
                Assert.Equal(conversionKind, model.GetConversion(@new).Kind);
            }
        }

        [Fact]
        public void TestInLocal_LangVersion8()
        {
            var source = @"
struct S
{
}

class C
{
    public static void Main()
    {
        C v1 = new();
        S v2 = new();
        S? v3 = new();
        C v4 = new(missing);
        S v5 = new(missing);
        S? v6 = new(missing);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,16): error CS8652: The feature 'target-typed object creation' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         C v1 = new();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "new").WithArguments("target-typed object creation").WithLocation(10, 16),
                // (11,16): error CS8652: The feature 'target-typed object creation' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         S v2 = new();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "new").WithArguments("target-typed object creation").WithLocation(11, 16),
                // (12,17): error CS8652: The feature 'target-typed object creation' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         S? v3 = new();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "new").WithArguments("target-typed object creation").WithLocation(12, 17),
                // (13,16): error CS8652: The feature 'target-typed object creation' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         C v4 = new(missing);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "new").WithArguments("target-typed object creation").WithLocation(13, 16),
                // (13,20): error CS0103: The name 'missing' does not exist in the current context
                //         C v4 = new(missing);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "missing").WithArguments("missing").WithLocation(13, 20),
                // (14,16): error CS8652: The feature 'target-typed object creation' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         S v5 = new(missing);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "new").WithArguments("target-typed object creation").WithLocation(14, 16),
                // (14,20): error CS0103: The name 'missing' does not exist in the current context
                //         S v5 = new(missing);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "missing").WithArguments("missing").WithLocation(14, 20),
                // (15,17): error CS8652: The feature 'target-typed object creation' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         S? v6 = new(missing);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "new").WithArguments("target-typed object creation").WithLocation(15, 17),
                // (15,21): error CS0103: The name 'missing' does not exist in the current context
                //         S? v6 = new(missing);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "missing").WithArguments("missing").WithLocation(15, 21)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().ToArray();

            assert(0, type: "C", convertedType: "C", symbol: "C..ctor()", ConversionKind.Identity);
            assert(1, type: "S", convertedType: "S", symbol: "S..ctor()", ConversionKind.Identity);
            assert(2, type: "S", convertedType: "S?", symbol: "S..ctor()", ConversionKind.ImplicitNullable);

            void assert(int index, string type, string convertedType, string symbol, ConversionKind conversionKind)
            {
                var @new = nodes[index];
                Assert.Equal(type, model.GetTypeInfo(@new).Type.ToTestDisplayString());
                Assert.Equal(convertedType, model.GetTypeInfo(@new).ConvertedType.ToTestDisplayString());
                Assert.Equal(symbol, model.GetSymbolInfo(@new).Symbol.ToTestDisplayString());
                Assert.Equal(conversionKind, model.GetConversion(@new).Kind);
            }
        }

        [Fact]
        public void TestInExpressionTree()
        {
            var source = @"
using System;
using System.Linq.Expressions;

struct S
{
}

class C
{
    public static void Main()
    {
        Expression<Func<C>> expr1 = () => new();
        Expression<Func<S>> expr2 = () => new();
        Expression<Func<S?>> expr3 = () => new();
        Console.Write(expr1.Compile()());
        Console.Write(expr2.Compile()());
        Console.Write(expr3.Compile()());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "CSS");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().ToArray();

            assert(0, type: "C", convertedType: "C", symbol: "C..ctor()", ConversionKind.Identity);
            assert(1, type: "S", convertedType: "S", symbol: "S..ctor()", ConversionKind.Identity);
            assert(2, type: "S", convertedType: "S?", symbol: "S..ctor()", ConversionKind.ImplicitNullable);

            void assert(int index, string type, string convertedType, string symbol, ConversionKind conversionKind)
            {
                var @new = nodes[index];
                Assert.Equal(type, model.GetTypeInfo(@new).Type.ToTestDisplayString());
                Assert.Equal(convertedType, model.GetTypeInfo(@new).ConvertedType.ToTestDisplayString());
                Assert.Equal(symbol, model.GetSymbolInfo(@new).Symbol.ToTestDisplayString());
                Assert.Equal(conversionKind, model.GetConversion(@new).Kind);
            }
        }

        [Fact]
        public void TestInParameterDefaultValue()
        {
            var source = @"
struct S
{
}

class C
{
    void M(
        C p1 = new(),
        S p2 = new(), // ok
        S? p3 = new(),
        int p4 = new(), // ok
        bool? p5 = new() // ok
        )
    {
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,16): error CS1736: Default parameter value for 'p1' must be a compile-time constant
                //         C p1 = new(),
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("p1").WithLocation(9, 16),
                // (11,17): error CS1736: Default parameter value for 'p3' must be a compile-time constant
                //         S? p3 = new()
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("p3").WithLocation(11, 17)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().ToArray();

            assert(0, type: "C", convertedType: "C", symbol: "C..ctor()", constant: null, ConversionKind.Identity);
            assert(1, type: "S", convertedType: "S", symbol: "S..ctor()", constant: null, ConversionKind.Identity);
            assert(2, type: "S", convertedType: "S?", symbol: "S..ctor()", constant: null, ConversionKind.ImplicitNullable);
            assert(3, type: "System.Int32", convertedType: "System.Int32", symbol: "System.Int32..ctor()", constant: "0", ConversionKind.Identity);
            assert(4, type: "System.Boolean", convertedType: "System.Boolean?", symbol: "System.Boolean..ctor()", constant: "False", ConversionKind.ImplicitNullable);

            void assert(int index, string type, string convertedType, string symbol, string constant, ConversionKind conversionKind)
            {
                var @new = nodes[index];
                Assert.Equal(type, model.GetTypeInfo(@new).Type.ToTestDisplayString());
                Assert.Equal(convertedType, model.GetTypeInfo(@new).ConvertedType.ToTestDisplayString());
                Assert.Equal(symbol, model.GetSymbolInfo(@new).Symbol.ToTestDisplayString());
                Assert.Equal(conversionKind, model.GetConversion(@new).Kind);
                Assert.Equal(constant, model.GetConstantValue(@new).Value?.ToString());
            }
        }

        [Fact]
        public void TestArguments_Out()
        {
            var source = @"
using System;

class C
{
    public int j;
    public C(out int i)
    {
        i = 2;
    }

    public static void Main()
    {
        C c = new(out var i) { j = i };
        Console.Write(c.j);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "2");
        }

        [Fact]
        public void TestArguments_Params()
        {
            var source = @"
using System;

class C
{
    public C(params int[] p)
    {
        foreach (var item in p)
        {
            Console.Write(item);
        }
    }

    public static void Main()
    {
        C c = new(1, 2, 3);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "123");
        }

        [Fact]
        public void TestArguments_NonTrailingNamedArgs()
        {
            var source = @"
using System;

class C
{
    public C(object c, object o) => Console.Write(1);
    public C(int i, object o) => Console.Write(2);

    public static void Main()
    {
        C c = new(c: new(), 2);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "1");
        }

        [Fact]
        public void TestArguments_DynamicArgs()
        {
            var source = @"
using System;

class C
{
    readonly int field;

    public C(int field)
    {
        this.field = field;
    }

    public C(dynamic c)
    {
        Console.Write(c.field);
    }

    public static void Main()
    {
        dynamic d = 5;
        C c = new(new C(d));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe, references: new[] { CSharpRef });
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "5");
        }

        [Fact]
        public void TestInDynamicInvocation()
        {
            var source = @"
class C
{
    public void M(int i) {}

    public static void Main()
    {
        dynamic d = new C();
        d.M(new());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe, references: new[] { CSharpRef });
            comp.VerifyDiagnostics(
                // (9,13): error CS8754: There is no target type for 'new()'
                //         d.M(new());
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(9, 13)
                );
        }

        [Fact]
        public void TestInAsOperator()
        {
            var source = @"
using System;

struct S
{
}

class C 
{
    public void M<TClass, TNew>()
        where TClass : class
        where TNew : new()
    {
        Console.Write(new() as C);
        Console.Write(new() as S?);
        Console.Write(new() as TClass);
        Console.Write(new() as TNew);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (14,23): error CS8754: There is no target type for 'new()'
                //         Console.Write(new() as C);
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(14, 23),
                // (15,23): error CS8754: There is no target type for 'new()'
                //         Console.Write(new() as S?);
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(15, 23),
                // (16,23): error CS8754: There is no target type for 'new()'
                //         Console.Write(new() as TClass);
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(16, 23),
                // (17,23): error CS8754: There is no target type for 'new()'
                //         Console.Write(new() as TNew);
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(17, 23)
                );
        }

        [Fact]
        public void TestInTupleElement()
        {
            var source = @"
using System;

class C
{
    static void M<T>((T a, T b) t, T c) => Console.Write(t);

    public static void Main()
    {
        M((new(), new()), new C());
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "(C, C)");
        }

        [Fact]
        public void TestTargetType_Var()
        {
            var source = @"
class C
{
    void M()
    {
        var x = new(2, 3);
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,17): error CS8754: There is no target type for 'new(int, int)'
                //         var x = new(5);
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new(2, 3)").WithArguments("new(int, int)").WithLocation(6, 17)
                );
        }

        [Fact]
        public void TestTargetType_Discard()
        {
            var source = @"
class C
{
    void M()
    {
        _ = new();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,13): error CS8754: There is no target type for 'new()'
                //         _ = new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 13)
                );
        }

        [Fact]
        public void TestTargetType_Delegate()
        {
            var source = @"
delegate void D();
class C
{
    void M()
    {
        D x0 = new();
        D x1 = new(M); // ok
        var x2 = (D)new();
        var x3 = (D)new(M); // ok
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,16): error CS1729: 'D' does not contain a constructor that takes 0 arguments
                //         D x0 = new();
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("D", "0").WithLocation(7, 16),
                // (9,21): error CS1729: 'D' does not contain a constructor that takes 0 arguments
                //         var x2 = (D)new();
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("D", "0").WithLocation(9, 21)
                );
        }

        [Fact]
        public void TestTargetType_Static()
        {
            var source = @"
public static class C {
    static void M(object c) {
        _ = (C)(new());
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                    // (4,13): error CS0716: Cannot convert to static type 'C'
                    //         _ = (C)(new());
                    Diagnostic(ErrorCode.ERR_ConvertToStaticClass, "(C)(new())").WithArguments("C").WithLocation(4, 13),
                    // (4,17): error CS1729: 'C' does not contain a constructor that takes 0 arguments
                    //         _ = (C)(new());
                    Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("C", "0").WithLocation(4, 17)
                    );
        }

        [Fact]
        public void TestTargetType_Abstract()
        {
            var source = @"
abstract class C
{
    void M()
    {
        C x0 = new();
        var x1 = (C)new();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,16): error CS0144: Cannot create an instance of the abstract class or interface 'C'
                //         C x0 = new();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new()").WithArguments("C").WithLocation(6, 16),
                // (7,21): error CS0144: Cannot create an instance of the abstract class or interface 'C'
                //         var x1 = (C)new();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new()").WithArguments("C").WithLocation(7, 21)
                );
        }

        [Fact]
        public void TestTargetType_Interface()
        {
            var source = @"
interface I {}
class C
{
    void M()
    {
        I x0 = new();
        var x1 = (I)new();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,16): error CS0144: Cannot create an instance of the abstract class or interface 'I'
                //         I x0 = new();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new()").WithArguments("I").WithLocation(7, 16),
                // (8,21): error CS0144: Cannot create an instance of the abstract class or interface 'I'
                //         var x1 = (I)new();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new()").WithArguments("I").WithLocation(8, 21)
                );
        }

        [Fact]
        public void TestTargetType_Enum()
        {
            var source = @"
using System;
enum E {}
class C
{
    static void Main()
    {
        E x0 = new();
        var x1 = (E)new();
        Console.Write(x0);
        Console.Write(x1);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "00");
        }

        [Fact]
        public void TestTargetType_Primitive()
        {
            var source = @"
class C
{
    void M()
    {
        int x0 = new();
        var x1 = (int)new();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,13): warning CS0219: The variable 'x0' is assigned but its value is never used
                //         int x0 = new();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x0").WithArguments("x0").WithLocation(6, 13),
                // (7,13): warning CS0219: The variable 'x1' is assigned but its value is never used
                //         var x1 = (int)new();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x1").WithArguments("x1").WithLocation(7, 13)
                );
        }

        [Fact]
        public void TestTargetType_TupleType()
        {
            var source = @"
#pragma warning disable 0219
class C
{
    void M()
    {
        (int, int) x0 = new();
        var x1 = ((int, int))new();
        (int, C) x2 = new();
        var x3 = ((int, C))new();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestTargetType_ValueTuple()
        {
            var source = @"
using System;
class C
{
    void M()
    {
        ValueTuple<int, int> x0 = new();
        ValueTuple<int, int> x1 = new(2, 3);
        var x2 = (ValueTuple<int, int>)new();
        var x3 = (ValueTuple<int, int>)new(2, 3);
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,30): warning CS0219: The variable 'x0' is assigned but its value is never used
                //         ValueTuple<int, int> x0 = new();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x0").WithArguments("x0").WithLocation(7, 30),
                // (9,13): warning CS0219: The variable 'x2' is assigned but its value is never used
                //         var x2 = (ValueTuple<int, int>)new();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x2").WithArguments("x2").WithLocation(9, 13)
                );
        }

        [Fact]
        public void TestTypeParameter()
        {
            var source = @"
using System;

struct S
{
    static void M1<T>() where T : struct
    {
        Console.Write((T)new());
    }
    static void M2<T>() where T : new()
    {
        Console.Write((T)new());
    }

    public static void Main()
    {
        M1<S>();
        M2<S>();
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "SS");
        }

        [Fact]
        public void TestTypeParameter_ErrorCases()
        {
            var source = @"
class C
{
    void M<T, TClass, TStruct, TNew>()
        where TClass : class
        where TStruct : struct
        where TNew : new()
    {
        {
            T x0 = new();
            var x1 = (T)new();
        }
        {
            TClass x0 = new();
            var x1 = (TClass)new();
        }
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,20): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //             T x0 = new();
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new()").WithArguments("T").WithLocation(10, 20),
                // (11,25): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //             var x1 = (T)new();
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new()").WithArguments("T").WithLocation(11, 25),
                // (14,25): error CS0304: Cannot create an instance of the variable type 'TClass' because it does not have the new() constraint
                //             TClass x0 = new();
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new()").WithArguments("TClass").WithLocation(14, 25),
                // (15,30): error CS0304: Cannot create an instance of the variable type 'TClass' because it does not have the new() constraint
                //             var x1 = (TClass)new();
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new()").WithArguments("TClass").WithLocation(15, 30)
                );
        }

        [Fact]
        public void TestTargetType_ErrorType()
        {
            var source = @"
class C
{
    void M()
    {
        Missing x0 = new();
        var x1 = (Missing)new();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0246: The type or namespace name 'Missing' could not be found (are you missing a using directive or an assembly reference?)
                //         Missing x0 = new();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Missing").WithArguments("Missing").WithLocation(6, 9),
                // (7,19): error CS0246: The type or namespace name 'Missing' could not be found (are you missing a using directive or an assembly reference?)
                //         var x1 = (Missing)new();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Missing").WithArguments("Missing").WithLocation(7, 19)
                );
        }

        [Fact]
        public void TestTargetType_Pointer()
        {
            var source = @"
class C
{
    unsafe void M()
    {
        int* x0 = new();
        var x1 = (int*)new();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (6,19): error CS1919: Unsafe type 'int*' cannot be used in object creation
                //         int* x0 = new();
                Diagnostic(ErrorCode.ERR_UnsafeTypeInObjectCreation, "new()").WithArguments("int*").WithLocation(6, 19),
                // (7,24): error CS1919: Unsafe type 'int*' cannot be used in object creation
                //         var x1 = (int*)new();
                Diagnostic(ErrorCode.ERR_UnsafeTypeInObjectCreation, "new()").WithArguments("int*").WithLocation(7, 24)
                );
        }

        [Fact]
        public void TestTargetType_AnonymousType()
        {
            var source = @"
class C
{
    void M()
    {
        var x0 = new { };
        x0 = new();
        var x1 = new { X = 1 };
        x1 = new(2);
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,14): error CS8752: The type '<empty anonymous type>' may not be used as the target-type of 'new()'
                //         x0 = new();
                Diagnostic(ErrorCode.ERR_TypelessNewIllegalTargetType, "new()").WithArguments("<empty anonymous type>").WithLocation(7, 14),
                // (9,14): error CS8752: The type '<anonymous type: int X>' may not be used as the target-type of 'new()'
                //         x1 = new(2);
                Diagnostic(ErrorCode.ERR_TypelessNewIllegalTargetType, "new(2)").WithArguments("<anonymous type: int X>").WithLocation(9, 14));
        }

        [Fact]
        public void TestTargetType_CoClass_01()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

class CoClassType : InterfaceType { }

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(CoClassType))]
interface InterfaceType { }

public class Program
{
    public static void Main()
    {
        InterfaceType a = new() { };
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().ToArray();

            var @new = nodes[0];
            Assert.Equal("InterfaceType", model.GetTypeInfo(@new).Type.ToTestDisplayString());
            Assert.Equal("InterfaceType", model.GetTypeInfo(@new).ConvertedType.ToTestDisplayString());
            Assert.Equal("CoClassType..ctor()", model.GetSymbolInfo(@new).Symbol.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, model.GetConversion(@new).Kind);
        }

        [Fact]
        public void TestTargetType_CoClass_02()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class GenericCoClassType<T, U> : NonGenericInterfaceType
{
    public GenericCoClassType(U x) { }
}

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(GenericCoClassType<int, string>))]
public interface NonGenericInterfaceType
{
}

public class MainClass
{
    public static int Main()
    {
        NonGenericInterfaceType a = new(""string"");
        return 0;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().ToArray();

            var @new = nodes[0];
            Assert.Equal("NonGenericInterfaceType", model.GetTypeInfo(@new).Type.ToTestDisplayString());
            Assert.Equal("NonGenericInterfaceType", model.GetTypeInfo(@new).ConvertedType.ToTestDisplayString());
            Assert.Equal("GenericCoClassType<System.Int32, System.String>..ctor(System.String x)", model.GetSymbolInfo(@new).Symbol.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, model.GetConversion(@new).Kind);
        }

        [Fact]
        public void TestAmbiguousCall()
        {
            var source = @"
class C {
    
    public C(object a, C b) {}
    public C(C a, object b) {}

    public static void Main()
    {
        C c = new(new(), new());
    }
}
";

            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,15): error CS0121: The call is ambiguous between the following methods or properties: 'C.C(object, C)' and 'C.C(C, object)'
                //         C c = new(new(), new());
                Diagnostic(ErrorCode.ERR_AmbigCall, "new(new(), new())").WithArguments("C.C(object, C)", "C.C(C, object)").WithLocation(9, 15)
                );
        }

        [Fact]
        public void TestObjectAndCollectionInitializer()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C
{
    public C field; 
    public int i;

    public C(int i) => this.i = i;
    public C() {}

    public static void Main()
    {
        Dictionary<C, List<int>> dict1 = new() { { new() { field = new(1) }, new() { 1, 2, 3 } } };
        Dictionary<C, List<int>> dict2 = new() { [new() { field = new(2) }] = new() { 4, 5, 6 } };
        Dump(dict1);
        Dump(dict2);
    }

    static void Dump(Dictionary<C, List<int>> dict)
    {
        foreach (C key in dict.Keys)
        {
            Console.Write($""C({key.field.i}): "");
        }

        foreach (List<int> value in dict.Values)
        {
            Console.WriteLine(string.Join("", "", value));
        }
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput:
@"C(1): 1, 2, 3
C(2): 4, 5, 6");
        }

        [Fact]
        public void TestInClassInitializer()
        {
            var source = @"
using System;

class D
{
}

class C
{
    public D field = new();
    public D Property1 { get; } = new();
    public D Property2 { get; set; } = new();

    public static void Main()
    {
        C c = new();
        Console.Write(c.field);
        Console.Write(c.Property1);
        Console.Write(c.Property2);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "DDD");
        }

        [Fact]
        public void TestDataFlow()
        {
            var source = @"
using System;

class C
{
    public int field;

    public static void Main()
    {
        int i;
        C c = new() { field = (i = 42) };
        Console.Write(i);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void TestDotOff()
        {
            var source = @"
class C
{
    public static void Main()
    {
       _ = (new()).field;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,13): error CS8754: There is no target type for 'new()'
                //        _ = (new()).field;
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 13)
                );
        }

        [Fact]
        public void TestConditionalAccess()
        {
            var source = @"
using System;
class C
{
    public static void Main()
    {
       Console.Write(((int?)new())?.ToString());
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void TestInaccessibleConstructor()
        {
            var source = @"
class D
{
    private D() {}
}

class C
{
    public static void Main()
    {
        D d = new();
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (11,15): error CS0122: 'D.D()' is inaccessible due to its protection level
                //         D d = new();
                Diagnostic(ErrorCode.ERR_BadAccess, "new()").WithArguments("D.D()").WithLocation(11, 15)
                );
        }

        [Fact]
        public void TestBadArgs()
        {
            var source = @"
class C
{
    public static void Main()
    {
        C c = new(1);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,15): error CS1729: 'C' does not contain a constructor that takes 1 arguments
                //         C c = new(1);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new(1)").WithArguments("C", "1").WithLocation(6, 15)
                );
        }

        [Fact]
        public void TestNested()
        {
            var source = @"
using System;

class C
{
    public C(C a, C b) => Console.Write(3); 
    public C(int i) => Console.Write(i);

    public static void Main()
    {
        C x = new(new(1), new(2));
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "123");
        }

        [Fact]
        public void TestDeconstruction()
        {
            var source = @"
class C
{
    public static void Main()
    {
        var (_, _) = new();
        (var _, var _) = new();
        (C _, C _) = new();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,22): error CS8754: There is no target type for 'new()'
                //         var (_, _) = new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 22),
                // (6,22): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         var (_, _) = new();
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "new()").WithLocation(6, 22),
                // (6,14): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable '_'.
                //         var (_, _) = new();
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "_").WithArguments("_").WithLocation(6, 14),
                // (6,14): error CS8183: Cannot infer the type of implicitly-typed discard.
                //         var (_, _) = new();
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "_").WithLocation(6, 14),
                // (6,17): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable '_'.
                //         var (_, _) = new();
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "_").WithArguments("_").WithLocation(6, 17),
                // (6,17): error CS8183: Cannot infer the type of implicitly-typed discard.
                //         var (_, _) = new();
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "_").WithLocation(6, 17),
                // (7,26): error CS8754: There is no target type for 'new()'
                //         (var _, var _) = new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(7, 26),
                // (7,26): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         (var _, var _) = new();
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "new()").WithLocation(7, 26),
                // (7,10): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable '_'.
                //         (var _, var _) = new();
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "var _").WithArguments("_").WithLocation(7, 10),
                // (7,10): error CS8183: Cannot infer the type of implicitly-typed discard.
                //         (var _, var _) = new();
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "var _").WithLocation(7, 10),
                // (7,17): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable '_'.
                //         (var _, var _) = new();
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "var _").WithArguments("_").WithLocation(7, 17),
                // (7,17): error CS8183: Cannot infer the type of implicitly-typed discard.
                //         (var _, var _) = new();
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "var _").WithLocation(7, 17),
                // (8,22): error CS8754: There is no target type for 'new()'
                //         (C _, C _) = new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(8, 22),
                // (8,22): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         (C _, C _) = new();
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "new()").WithLocation(8, 22)
                );
        }

        [Fact]
        public void TestBestType_NullCoalescing()
        {
            var source = @"
using System;

class C
{
    public static void Main()
    {
        C c = null;
        Console.Write(c ?? new());
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "C");
        }

        [Fact]
        public void TestBestType_Lambda()
        {
            var source = @"
using System;

class C
{
    public static void M<T>(Func<bool, T> f)
    {
        Console.Write(f(true));
        Console.Write(f(false));
    }
    public static void Main()
    {
        M(b => { if (b) return new C(); else return new(); });
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "CC");
        }

        [Fact]
        public void TestBestType_SwitchExpression()
        {
            var source = @"
using System;

class C
{
    public static void Main()
    {
        var b = false; 
        Console.Write((int)(b switch { true => 1, false => new() }));
        b = true;
        Console.Write((int)(b switch { true => 1, false => new() }));
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "01");
        }

        [Fact]
        public void TestInSwitchExpression()
        {
            var source = @"
using System;

class C
{
    public static void Main()
    {
        C x = 0 switch { _ => new() };
        Console.Write(x);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "C");
        }

        [Fact]
        public void TestInNullCoalescingAssignment()
        {
            var source = @"
using System;

class C
{
    public static void Main()
    {
        C x = null;
        x ??= new();
        Console.Write(x);
        int? i = null;
        i ??= new();
        Console.Write(i);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "C0");
        }

        [Fact]
        public void TestInNullCoalescingAssignment_ErrorCase()
        {
            var source = @"
class C
{
    public static void Main()
    {
        new() ??= new C();
        new() ??= new();
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (6,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         new() ??= new C();
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new()").WithLocation(6, 9),
                // (7,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         new() ??= new();
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new()").WithLocation(7, 9)
                );
        }

        [Fact]
        public void TestBestType_Lambda_ErrorCase()
        {
            var source = @"
using System;

class C
{
    public static void M<T>(Func<bool, T> f)
    {
    }

    public static void Main()
    {
        M(b => { if (b) return new(); else return new(); });
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (12,9): error CS0411: The type arguments for method 'C.M<T>(Func<bool, T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(b => { if (b) return new(); else return new(); });
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<T>(System.Func<bool, T>)").WithLocation(12, 9)
                );
        }

        [Fact]
        public void TestBadTypeParameter()
        {
            var source = @"
class C
{
    static void M<A, B, C>()
        where A : struct
        where B : new()
    {
        A v1 = new(1);
        B v2 = new(2);
        C v3 = new();
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (8,16): error CS0417: 'A': cannot provide arguments when creating an instance of a variable type
                //         A v1 = new(1);
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "new(1)").WithArguments("A").WithLocation(8, 16),
                // (9,16): error CS0417: 'B': cannot provide arguments when creating an instance of a variable type
                //         B v2 = new(2);
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "new(2)").WithArguments("B").WithLocation(9, 16),
                // (10,16): error CS0304: Cannot create an instance of the variable type 'C' because it does not have the new() constraint
                //         C v3 = new();
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new()").WithArguments("C").WithLocation(10, 16)
                );
        }

        [Fact]
        public void TestTypeParameterInitializer()
        {
            var source = @"
using System;

class C
{
    public int field;

    static void M1<T>() where T : C, new()
    {
        Console.Write(((T)new(){ field = 42 }).field);
    }

    public static void Main()
    {
        M1<C>();
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void TestInitializer_ErrorCase()
        {
            var source = @"
class C
{
    public static void Main()
    {
        string x = new() { Length = 5 };
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (8,28): error CS0200: Property or indexer 'string.Length' cannot be assigned to -- it is read only
                //         string x = new() { Length = 5 };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Length").WithArguments("string.Length").WithLocation(6, 28),
                // (8,20): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         string x = new() { Length = 5 };
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new() { Length = 5 }").WithArguments("string", "0").WithLocation(6, 20)
                );
        }

        [Fact]
        public void TestImplicitConversion()
        {
            var source = @"
public class Dog
{
    public Dog() {}
}
public class Animal
{
    public Animal() {}
    public static implicit operator Animal(Dog dog) => throw null;
}

public class Program
{
    public static void M(Animal a) => System.Console.Write(a);
    public static void Main()
    {
        M(new());
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                );
            CompileAndVerify(comp, expectedOutput: "Animal");
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        public void ArgList()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        C x = new(__arglist(2, 3, true));
    }
    
    public C(__arglist)
    {
        DumpArgs(new(__arglist));
    }

    static void DumpArgs(ArgIterator args)
    {
        while(args.GetRemainingCount() > 0)
        {
            TypedReference tr = args.GetNextArg();
            object arg = TypedReference.ToObject(tr);
            Console.Write(arg);
        }
    }
}";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "23True");
        }

        [Fact]
        public void TestOverloadResolution01()
        {
            var source = @"
class C
{
    public C(int i) {}
}

class D
{
}

class Program
{
    static void M(C c, object o) {}
    static void M(D d, int i) {}

    public static void Main()
    {
        M(new(1), 1);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (18,11): error CS1729: 'D' does not contain a constructor that takes 1 arguments
                //         M(new(1), 1);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new(1)").WithArguments("D", "1").WithLocation(18, 11)
                );
        }

        [Fact]
        public void TestOverloadResolution02()
        {
            var source = @"
using System;
class A
{
    public A(int i) {}
}
class B : A
{
    public B(int i) : base(i) {}
}

class Program
{
    static void M(A a) => Console.Write(""A"");
    static void M(B a) => Console.Write(""B"");

    public static void Main()
    {
        M(new(43));
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: "B");
        }

        [Fact]
        public void TestOverloadResolution03()
        {
            var source = @"
class A
{
    public A(int i) {}
}
class B : A
{
    public B(int i) : base(i) {}
}

class Program
{
    static void M(A a) {}
    static void M(B a) {}

    public static void Main()
    {
        M(new(Missing()));
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (18,15): error CS0103: The name 'Missing' does not exist in the current context
                //         M(new(Missing()));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Missing").WithArguments("Missing").WithLocation(18, 15)
                );
        }

        [Fact]
        public void TestOverloadResolution04()
        {
            var source = @"
class A
{
    public A(int i) {}
}
class B : A
{
    public B(int i) : base(i) {}
}

class Program
{
    public static void Main()
    {
        Missing(new(1));
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (15,9): error CS0103: The name 'Missing' does not exist in the current context
                //         Missing(new(1));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Missing").WithArguments("Missing").WithLocation(15, 9)
                );
        }

        [Fact]
        public void TestOverloadResolution05()
        {
            var source = @"
class A
{
    public A(int i) {}
}
class B : A
{
    public B(int i) : base(i) {}
}

class Program
{
    static void M(A a, int i) {}
    static void M(B a, object i) {}

    public static void Main()
    {
        M(new(), 1);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (18,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A, int)' and 'Program.M(B, object)'
                //         M(new(), 1);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A, int)", "Program.M(B, object)").WithLocation(18, 9)
                );
        }

        [Fact]
        public void TestOverloadResolution06()
        {
            var source = @"
class C
{
    public C(object a, C b) {}
    public C(C a, int b) {}

    public static void Main()
    {
        C c = new(new(), new());
    }
}
";

            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,19): error CS1729: 'C' does not contain a constructor that takes 0 arguments
                //         C c = new(new(), new());
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("C", "0").WithLocation(9, 19)
                );
        }

        [Fact]
        public void TestSymbols()
        {
            var source = @"
class C
{
    static C N(int i) => null;

    static void M(C c) {}

    public static void Main()
    {
        Missing(new() { X = N(1) });
        M(new() { X = N(2) });

    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (10,9): error CS0103: The name 'Missing' does not exist in the current context
                //         Missing(new() { X = N(1) });
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Missing").WithArguments("Missing").WithLocation(10, 9),
                // (11,19): error CS0117: 'C' does not contain a definition for 'X'
                //         M(new() { X = N(2) });
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("C", "X").WithLocation(11, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();

            assert(1, "N(1)", type: "C", convertedType: "C", symbol: "C C.N(System.Int32 i)", ConversionKind.Identity);
            assert(3, "N(2)", type: "C", convertedType: "C", symbol: "C C.N(System.Int32 i)", ConversionKind.Identity);

            void assert(int index, string expression, string type, string convertedType, string symbol, ConversionKind conversionKind)
            {
                var invocation = nodes[index];
                Assert.Equal(expression, invocation.ToString());
                Assert.Equal(type, model.GetTypeInfo(invocation).Type.ToTestDisplayString());
                Assert.Equal(convertedType, model.GetTypeInfo(invocation).ConvertedType.ToTestDisplayString());
                Assert.Equal(symbol, model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
                Assert.Equal(conversionKind, model.GetConversion(invocation).Kind);
            }
        }

        [Fact]
        public void TestAssignment()
        {
            var source = @"
class Program
{
    public static void Main()
    {
        new() = 5;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (6,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         new() = 5;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new()").WithLocation(6, 9)
                );
        }

        [Fact]
        public void TestNullableType01()
        {
            var source = @"
using System;

struct S
{
    public S(int i)
    {
        Console.Write(i);
    }

    public static void Main()
    {
        S? s = new(43);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "43");
        }

        [Fact]
        public void TestNullableType02()
        {
            var source = @"
using System;
struct S
{
    public static T? M<T>() where T : struct
    {
        return new();
    }

    public static void Main()
    {
        Console.Write(M<S>());
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: "S");
        }

        [Fact]
        public void TestInStatement()
        {
            var source = @"
struct S
{
    public static void Main()
    {
        new(a) { x };
        new() { x };
    }
}
";
            _ = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         new(a) { x };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "new(a) { x }").WithLocation(6, 9),
                // (6,13): error CS0103: The name 'a' does not exist in the current context
                //         new(a) { x };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 13),
                // (6,18): error CS0103: The name 'x' does not exist in the current context
                //         new(a) { x };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 18),
                // (7,9): error CS8754: There is no target type for 'new()'
                //         new() { x };
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new() { x }").WithArguments("new()").WithLocation(7, 9),
                // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         new() { x };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "new() { x }").WithLocation(7, 9),
                // (7,17): error CS0103: The name 'x' does not exist in the current context
                //         new() { x };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(7, 17)
                );
        }

        [Fact]
        public void TestLangVersion_CSharp7()
        {
            string source = @"
class C
{
    static void Main()
    {
        C x = new();
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (6,15): error CS8652: The feature 'target-typed object creation' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         C x = new();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "new").WithArguments("target-typed object creation").WithLocation(6, 15)
                );
        }

        [Fact]
        public void TestAssignmentToClass()
        {
            string source = @"
class C
{
    static void Main()
    {
        C x = new();
        System.Console.Write(x);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            Assert.Equal("C", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("C", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Equal("C..ctor()", model.GetSymbolInfo(def).Symbol.ToTestDisplayString());
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.True(model.GetConversion(def).IsIdentity);
        }

        [Fact]
        public void TestAssignmentToStruct()
        {
            string source = @"
struct S
{
    public S(int i) {}
    static void Main()
    {
        S x = new(43);
        System.Console.Write(x);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "S");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            Assert.Equal("S", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("S", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Equal("S..ctor(System.Int32 i)", model.GetSymbolInfo(def).Symbol.ToTestDisplayString());
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.True(model.GetConversion(def).IsIdentity);
        }

        [Fact]
        public void AssignmentToNullableStruct()
        {
            string source = @"
struct S
{
    public S(int i) {}
    static void Main()
    {
        S? x = new(43);
        System.Console.Write(x);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "S");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            Assert.Equal("S", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("S?", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Equal("S..ctor(System.Int32 i)", model.GetSymbolInfo(def).Symbol.ToTestDisplayString());
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.True(model.GetConversion(def).IsNullable);
            Assert.True(model.GetConversion(def).IsImplicit);
        }

        [Fact]
        public void AssignmentToThisOnRefType()
        {
            string source = @"
public class C
{
    public int field;
    public C() => this = new();
    public static void Main()
    {
        new C();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (5,19): error CS1604: Cannot assign to 'this' because it is read-only
                //     public C() => this = new();
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
    public S(int x) => this = new(x);
    public static void Main()
    {
        new S(1);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().ElementAt(0);
            Assert.Equal("new(x)", def.ToString());
            Assert.Equal("S", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("S", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void InAttributeParameter()
        {
            string source = @"
[Custom(z: new(), y: new(), x: new())]
class C
{
    [Custom(new(1), new('s', 2))]
    void M()
    {
    }
}
public class CustomAttribute : System.Attribute
{
    public CustomAttribute(int x, string y, byte z = 0) { }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,22): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                // [Custom(z: new(), y: new(), x: new())]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("string", "0").WithLocation(2, 22),
                // (5,13): error CS1729: 'int' does not contain a constructor that takes 1 arguments
                //     [Custom(new(1), new('s', 2))]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new(1)").WithArguments("int", "1").WithLocation(5, 13),
                // (5,21): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Custom(new(1), new('s', 2))]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new('s', 2)").WithLocation(5, 21)
                );
        }

        [Fact]
        public void InStringInterpolation()
        {
            string source = @"
class C
{
    static void Main()
    {
        System.Console.Write($""({new()}) ({new object()})"");
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(System.Object) (System.Object)");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var @new = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            Assert.Equal("new()", @new.ToString());
            Assert.Equal("System.Object", model.GetTypeInfo(@new).Type.ToTestDisplayString());
            Assert.Equal("System.Object", model.GetTypeInfo(@new).ConvertedType.ToTestDisplayString());
            Assert.Equal("System.Object..ctor()", model.GetSymbolInfo(@new).Symbol?.ToTestDisplayString());
            Assert.False(model.GetConstantValue(@new).HasValue);

            var newObject = nodes.OfType<ObjectCreationExpressionSyntax>().Single();
            Assert.Equal("new object()", newObject.ToString());
            Assert.Equal("System.Object", model.GetTypeInfo(newObject).Type.ToTestDisplayString());
            Assert.Equal("System.Object", model.GetTypeInfo(newObject).ConvertedType.ToTestDisplayString());
            Assert.Equal("System.Object..ctor()", model.GetSymbolInfo(newObject).Symbol?.ToTestDisplayString());
        }

        [Fact]
        public void InUsing01()
        {
            string source = @"
class C
{
    static void Main()
    {
        using (new())
        {
        }

        using (var x = new())
        {
        }

        using (System.IDisposable x = new())
        {
        }
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,16): error CS8754: There is no target type for 'new()'
                //         using (new())
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 16),
                // (10,24): error CS8754: There is no target type for 'new()'
                //         using (var x = new())
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(10, 24),
                // (14,39): error CS0144: Cannot create an instance of the abstract class or interface 'IDisposable'
                //         using (System.IDisposable x = new())
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new()").WithArguments("System.IDisposable").WithLocation(14, 39)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().ToArray();

            assert(0, type: "?", convertedType: "?", ConversionKind.Identity);
            assert(1, type: "?", convertedType: "?", ConversionKind.Identity);
            assert(2, type: "System.IDisposable", convertedType: "System.IDisposable", ConversionKind.Identity);

            void assert(int index, string type, string convertedType, ConversionKind conversionKind)
            {
                var @new = nodes[index];
                Assert.Equal(type, model.GetTypeInfo(@new).Type.ToTestDisplayString());
                Assert.Equal(convertedType, model.GetTypeInfo(@new).ConvertedType.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(@new).Symbol);
                Assert.Equal(conversionKind, model.GetConversion(@new).Kind);
            }
        }

        [Fact]
        public void InUsing02()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
        Console.Write(""C.Dispose"");
    }

    static void Main()
    {
        using (C c = new())
        {
        }
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C.Dispose");
        }

        [Fact]
        public void TestInAwait()
        {
            string source = @"
class C
{
    async System.Threading.Tasks.Task M1()
    {
        await new();
    }

    async System.Threading.Tasks.Task M2()
    {
        await new(a);
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,15): error CS8754: There is no target type for 'new()'
                //         await new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 15),
                // (11,19): error CS0103: The name 'a' does not exist in the current context
                //         await new(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(11, 19)
                );
        }

        [Fact]
        public void ReturningFromAsyncMethod()
        {
            string source = @"
using System.Threading.Tasks;
class C
{
    async Task<T> M2<T>() where T : new()
    {
        await Task.Delay(0);
        return new();
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().ElementAt(0);
            Assert.Equal("new()", def.ToString());
            Assert.Equal("T", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("T", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.True(model.GetConversion(def).IsIdentity);
        }

        [Fact]
        public void TestInAsyncLambda_01()
        {
            string source = @"
class C
{
    static void F<T>(System.Threading.Tasks.Task<T> t) { }

    static void M()
    {
        F(async () => await new());
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,9): error CS0411: The type arguments for method 'C.F<T>(Task<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(async () => await new());
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("C.F<T>(System.Threading.Tasks.Task<T>)").WithLocation(8, 9)
                );
        }

        [Fact]
        public void TestInAsyncLambda_02()
        {
            string source = @"
class C
{
    static void F<T>(System.Threading.Tasks.Task<T> t) { }

    static void M()
    {
        F(async () => new());
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,9): error CS0411: The type arguments for method 'C.F<T>(Task<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(async () => new());
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("C.F<T>(System.Threading.Tasks.Task<T>)").WithLocation(8, 9),
                // (8,20): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         F(async () => new());
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(8, 20)
                );
        }

        [Fact]
        public void RefReturnValue1()
        {
            string source = @"
class C
{
    ref int M()
    {
        return new();
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS8150: By-value returns may only be used in methods that return by value
                //         return new();
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(6, 9)
                );
        }

        [Fact]
        public void RefReturnValue2()
        {
            string source = @"
class C
{
    ref C M()
    {
        return ref new();
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,20): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref new();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "new()").WithLocation(6, 20)
                );
        }

        [Fact]
        public void InAnonType()
        {
            string source = @"
class C
{
    static void M()
    {
        var x = new { Prop = new() };
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,30): error CS8754: There is no target type for 'new()'
                //         var x = new { Prop = new() };
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 30)
                );
        }

        [Fact]
        public void BadUnaryOperator()
        {
            string source = @"
class C
{
    static void M()
    {
        C v1 = +new();
        C v2 = -new();
        C v3 = ~new();
        C v4 = !new();
        C v5 = ++new();
        C v6 = --new();
        C v7 = new()++;
        C v8 = new()--;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,17): error CS8754: There is no target type for 'new()'
                //         C v1 = +new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 17),
                // (7,17): error CS8754: There is no target type for 'new()'
                //         C v2 = -new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(7, 17),
                // (8,17): error CS8754: There is no target type for 'new()'
                //         C v3 = ~new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(8, 17),
                // (9,17): error CS8754: There is no target type for 'new()'
                //         C v4 = !new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(9, 17),
                // (10,18): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         C v5 = ++new();
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "new()").WithLocation(10, 18),
                // (11,18): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         C v6 = --new();
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "new()").WithLocation(11, 18),
                // (12,16): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         C v7 = new()++;
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "new()").WithLocation(12, 16),
                // (13,16): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         C v8 = new()--;
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "new()").WithLocation(13, 16)
                );
        }

        [Fact]
        public void AmbiguousMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(new());
    }
    static void M(int x) { }
    static void M(string x) { }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(int)' and 'C.M(string)'
                //         M(new());
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(int)", "C.M(string)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void MethodWithNullableParameters()
        {
            string source = @"
struct S
{
    public S(int i) {}

    static void Main()
    {
        M(new(43));
    }

    static void M(S? x) => System.Console.Write(x);
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "S");
        }

        [Fact]
        public void CannotInferTypeArg()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(new());
    }
    static void M<T>(T x) { }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,9): error CS0411: The type arguments for method 'C.M<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(new());
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
        M(new(), null);
    }
    static void M<T>(T x, T y) where T : class { }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,9): error CS0411: The type arguments for method 'C.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(new(), null);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<T>(T, T)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void Invocation()
        {
            string source = @"
class C
{
    static void Main()
    {
        new().ToString();
        new()[0].ToString();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,9): error CS8754: There is no target type for 'new()'
                //         new().ToString();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 9),
                // (7,9): error CS8754: There is no target type for 'new()'
                //         new()[0].ToString();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(7, 9)
                );
        }

        [Fact]
        public void InThrow()
        {
            string source = @"
class C
{
    static void Main()
    {
        throw new(""message"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().First();
            Assert.Equal("System.Exception", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("System.Exception", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Equal("System.Exception..ctor(System.String message)", model.GetSymbolInfo(def).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestConst()
        {
            string source = @"
class C
{
    static void M()
    {
        const object x = new();
        const int y = new();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,26): error CS0133: The expression being assigned to 'x' must be constant
                //         const object x = new();
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "new()").WithArguments("x").WithLocation(6, 26),
                // (7,19): warning CS0219: The variable 'y' is assigned but its value is never used
                //         const int y = new();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(7, 19)
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
        var t = new[] { new C(), new() };
        System.Console.Write(t[1]);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().First();
            Assert.Equal("new()", def.ToString());
            Assert.Equal("C", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("C", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Equal("C..ctor()", model.GetSymbolInfo(def).Symbol.ToTestDisplayString());
            Assert.False(model.GetConstantValue(def).HasValue);
        }

        [Fact]
        public void InSwitch1()
        {
            string source = @"
class C
{
    static void Main()
    {
        switch (new())
        {
            case new() when new():
            case (new()) when (new()):
                break;
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,17): error CS8754: There is no target type for 'new()'
                //         switch (new())
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 17),
                // (10,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 17)
                );
        }

        [Fact]
        public void InSwitch2()
        {
            string source = @"
class C
{
    static void Main()
    {
        switch (new C())
        {
            case new():
            case (new()):
                break;
        }
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,18): error CS0150: A constant value is expected
                //             case new():
                Diagnostic(ErrorCode.ERR_ConstantExpected, "new()").WithLocation(8, 18),
                // (9,19): error CS0150: A constant value is expected
                //             case (new()):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "new()").WithLocation(9, 19)
                );
        }

        [Fact]
        public void InSwitch3()
        {
            string source = @"
class C
{
    static void Main()
    {
        int i = 0;
        bool b = true;
        switch (i)
        {
            case new() when b:
                System.Console.Write(0);
                break;
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void InGoToCase()
        {
            string source = @"
using System;
class C
{
    static int Get(int i)
    {
        switch (i)
        {
            case new():
                return 1;
            case 1:
                goto case new();
            default:
                return 2;
        }
    }
    static void Main()
    {
        Console.Write(Get(0));
        Console.Write(Get(1));
        Console.Write(Get(2));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "112");
        }

        [Fact]
        public void InCatchFilter()
        {
            string source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        catch when (new())
        {
        }
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,21): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //         catch when (new())
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "new()").WithLocation(9, 21)
                );
        }

        [Fact]
        public void InLock()
        {
            string source = @"
class C
{
    static void Main()
    {
        lock (new())
        {
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,15): error CS8754: There is no target type for 'new()'
                //         lock (new())
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 15)
                );
        }

        [Fact]
        public void InMakeRef()
        {
            string source = @"
class C
{
    static void Main()
    {
        System.TypedReference tr = __makeref(new());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,46): error CS1510: A ref or out value must be an assignable variable
                //         System.TypedReference tr = __makeref(new());
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "new()").WithLocation(6, 46)
                );
        }

        [Fact]
        public void InNameOf()
        {
            string source = @"
class C
{
    static void Main()
    {
        _ = nameof(new());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,20): error CS8081: Expression does not have a name.
                //         _ = nameof(new());
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "new()").WithLocation(6, 20)
                );
        }

        [Fact]
        public void InOutArgument()
        {
            string source = @"
class C
{
    static void M(out int i)
    {
        i = 0;
        M(out new());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                    // (7,15): error CS1510: A ref or out value must be an assignable variable
                    //         M(out new());
                    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "new()").WithLocation(7, 15)
                    );
        }

        [Fact]
        public void InSizeOf()
        {
            string source = @"
class C
{
    static void Main()
    {
        _ = sizeof(new());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,20): error CS1031: Type expected
                //         _ = sizeof(new());
                Diagnostic(ErrorCode.ERR_TypeExpected, "new").WithLocation(6, 20),
                // (6,20): error CS1026: ) expected
                //         _ = sizeof(new());
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "new").WithLocation(6, 20),
                // (6,20): error CS1002: ; expected
                //         _ = sizeof(new());
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "new").WithLocation(6, 20),
                // (6,25): error CS1002: ; expected
                //         _ = sizeof(new());
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 25),
                // (6,25): error CS1513: } expected
                //         _ = sizeof(new());
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 25),
                // (6,13): error CS0233: '?' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(new());
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(").WithArguments("?").WithLocation(6, 13),
                // (6,20): error CS8754: There is no target type for 'new()'
                //         _ = sizeof(new());
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 20)
                );
        }

        [Fact]
        public void InTypeOf()
        {
            string source = @"
class C
{
    static void Main()
    {
        _ = typeof(new());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,20): error CS1031: Type expected
                //         _ = typeof(new());
                Diagnostic(ErrorCode.ERR_TypeExpected, "new").WithLocation(6, 20),
                // (6,20): error CS1026: ) expected
                //         _ = typeof(new());
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "new").WithLocation(6, 20),
                // (6,20): error CS1002: ; expected
                //         _ = typeof(new());
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "new").WithLocation(6, 20),
                // (6,20): error CS8754: There is no target type for 'new()'
                //         _ = typeof(new());
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 20),
                // (6,25): error CS1002: ; expected
                //         _ = typeof(new());
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 25),
                // (6,25): error CS1513: } expected
                //         _ = typeof(new());
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 25)
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
        int i = checked(new(a));
        int j = checked(new()); // ok
        C k = unchecked(new()); // ok
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,29): error CS0103: The name 'a' does not exist in the current context
                //         int i = checked(new(a));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 29),
                // (7,13): warning CS0219: The variable 'j' is assigned but its value is never used
                //         int j = checked(new());
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j").WithLocation(7, 13)
                );
        }

        [Fact]
        public void InRange()
        {
            string source = @"
using System;
class C
{
    static void Main()
    {
        Range x0 = new()..new();
        Range x1 = 1..new();
        Range x2 = new()..1;
        Console.WriteLine($""{x0.Start.Value}..{x0.End.Value}"");
        Console.WriteLine($""{x1.Start.Value}..{x1.End.Value}"");
        Console.WriteLine($""{x2.Start.Value}..{x2.End.Value}"");
    }
}
";
            var comp = CreateCompilationWithIndexAndRange(source, options: TestOptions.DebugExe, parseOptions: TargetTypedObjectCreationTestOptions);
            comp.VerifyDiagnostics();

            var expectedOutput =
@"0..0
1..0
0..1";
            CompileAndVerify(comp, expectedOutput: expectedOutput);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().ToArray();

            assert(0, type: "System.Index", convertedType: "System.Index", symbol: "System.Index..ctor()", ConversionKind.Identity);
            assert(1, type: "System.Index", convertedType: "System.Index", symbol: "System.Index..ctor()", ConversionKind.Identity);
            assert(2, type: "System.Index", convertedType: "System.Index", symbol: "System.Index..ctor()", ConversionKind.Identity);
            assert(3, type: "System.Index", convertedType: "System.Index", symbol: "System.Index..ctor()", ConversionKind.Identity);

            void assert(int index, string type, string convertedType, string symbol, ConversionKind conversionKind)
            {
                var @new = nodes[index];
                Assert.Equal(type, model.GetTypeInfo(@new).Type.ToTestDisplayString());
                Assert.Equal(convertedType, model.GetTypeInfo(@new).ConvertedType.ToTestDisplayString());
                Assert.Equal(symbol, model.GetSymbolInfo(@new).Symbol.ToTestDisplayString());
                Assert.Equal(conversionKind, model.GetConversion(@new).Kind);
            }
        }

        [Fact]
        public void RefTypeAndValue()
        {
            string source = @"
class C
{
    static void Main()
    {
        var t = __reftype(new());
        int rv = __refvalue(new(), int);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ConditionalOnNew()
        {
            string source = @"
class C
{
    static void Main()
    {
        if (new())
        {
            System.Console.Write(""if"");
        }

        while (new())
        {
            System.Console.Write(""while"");
        }

        for (int i = 0; new(); i++)
        {
            System.Console.Write(""for"");
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (8,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("if");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(8, 13),
                // (13,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("while");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(13, 13),
                // (18,13): warning CS0162: Unreachable code detected
                //             System.Console.Write("for");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(18, 13));
        }

        [Fact]
        public void InFixed()
        {
            string source = @"
class C
{
    static unsafe void Main()
    {
        fixed (byte* p = new())
        {
        }
        fixed (byte* p = &new())
        {
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (6,26): error CS9385: The given expression cannot be used in a fixed statement
                //         fixed (byte* p = new())
                Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new()").WithLocation(6, 26),
                // (9,27): error CS0211: Cannot take the address of the given expression
                //         fixed (byte* p = &new())
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "new()").WithLocation(9, 27)
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
        var p = *new();
        var q = new()->F;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,18): error CS8754: There is no target type for 'new()'
                //         var p = *new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 18),
                // (7,17): error CS8754: There is no target type for 'new()'
                //         var q = new()->F;
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(7, 17)
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
        var t = new[] { new(), new() };
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,17): error CS0826: No best type found for implicitly-typed array
                //         var t = new[] { new(), new() };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { new(), new() }").WithLocation(6, 17)
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
        var t = new object[new()];
    }
}
";
            var comp = CreateCompilation(source);
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
        (int, C) t = (1, new());
        System.Console.Write(t.Item2);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C");
        }

        [Fact]
        public void TypeInferenceSucceeds()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(new(), new C());
    }
    static void M<T>(T x, T y) { System.Console.Write(x); }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C");
        }

        [Fact]
        public void ArrayTypeInferredFromParams()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(new());
    }

    static void M(params object[] x) { }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,11): error CS9366: The type 'object[]' may not be used as the target-type of 'new()'
                //         M(new());
                Diagnostic(ErrorCode.ERR_TypelessNewIllegalTargetType, "new()").WithArguments("object[]").WithLocation(6, 11)
                );
        }

        [Fact]
        public void ParamsAmbiguity01()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(new());
    }
    static void M(params object[] x) { }
    static void M(params int[] x) { }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(params object[])' and 'C.M(params int[])'
                //         M(new());
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(params object[])", "C.M(params int[])").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ParamsAmbiguity02()
        {
            string source = @"
class C
{
    static void Main()
    {
        M(new());
    }
    static void M(params object[] x) { }
    static void M(C x) { }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(params object[])' and 'C.M(C)'
                //         M(new());
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(params object[])", "C.M(C)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ParamsAmbiguity03()
        {
            string source = @"
class C
{
    static void Main()
    {
        object o = null;
        C c = new();
        M(o, new());
        M(new(), o);
        M(c, new());
        M(new(), c);
    }
    static void M<T>(T x, params T[] y) { }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (8,14): error CS9366: The type 'object[]' may not be used as the target-type of 'new'.
                //         M(o, new());
                Diagnostic(ErrorCode.ERR_TypelessNewIllegalTargetType, "new()").WithArguments("object[]").WithLocation(8, 14),
                // (10,14): error CS9366: The type 'C[]' may not be used as the target-type of 'new'.
                //         M(c, new());
                Diagnostic(ErrorCode.ERR_TypelessNewIllegalTargetType, "new()").WithArguments("C[]").WithLocation(10, 14)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var first = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().ElementAt(1);
            Assert.Equal("(o, new())", first.Parent.Parent.ToString());
            Assert.Equal("System.Object[]", model.GetTypeInfo(first).Type.ToTestDisplayString());

            var second = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().ElementAt(2);
            Assert.Equal("(new(), o)", second.Parent.Parent.ToString());
            Assert.Equal("System.Object", model.GetTypeInfo(second).Type.ToTestDisplayString());

            var third = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().ElementAt(3);
            Assert.Equal("(c, new())", third.Parent.Parent.ToString());
            Assert.Equal("C[]", model.GetTypeInfo(third).Type.ToTestDisplayString());

            var fourth = nodes.OfType<ImplicitObjectCreationExpressionSyntax>().ElementAt(4);
            Assert.Equal("(new(), c)", fourth.Parent.Parent.ToString());
            Assert.Equal("C", model.GetTypeInfo(fourth).Type.ToTestDisplayString());
        }

        [Fact]
        public void NewIdentifier()
        {
            string source = @"
class C
{
    static void Main()
    {
        int @new = 2;
        C x = new();
        System.Console.Write($""{x} {@new}"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C 2");
        }

        [Fact]
        public void Return()
        {
            string source = @"
class C
{
    static C M()
    {
        return new();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NewInEnum()
        {
            string source = @"
enum E : byte
{
    A = new(),
}
";
            var comp = CreateCompilation(source);
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
    static IEnumerable<C> M()
    {
        yield return new();
    }
    static IEnumerable M2()
    {
        yield return new();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
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
        d.M2(new());
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,14): error CS8754: There is no target type for 'new()'
                //         d.M2(new());
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(7, 14)
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
        F(new());
    }
    static void F(dynamic x)
    {
        System.Console.Write(x == null);
    }
}
";

            var comp = CreateCompilation(source, references: new[] { CSharpRef }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,11): error CS8752: The type 'dynamic' may not be used as the target type of new()
                //         F(new());
                Diagnostic(ErrorCode.ERR_TypelessNewIllegalTargetType, "new()").WithArguments("dynamic").WithLocation(6, 11)
                );
        }

        [Fact]
        public void TestBinaryOperators01()
        {
            string source = @"
class C
{
    static void Main()
    {
        var a = new() + new();
        var b = new() - new();
        var c = new() & new();
        var d = new() | new();
        var e = new() ^ new();
        var f = new() * new();
        var g = new() / new();
        var h = new() % new();
        var i = new() >> new();
        var j = new() << new();
        var k = new() > new();
        var l = new() < new();
        var m = new() >= new();
        var n = new() <= new();
        var o = new() == new();
        var p = new() != new();
        var q = new() && new();
        var r = new() || new();
        var s = new() ?? new();
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,17): error CS8310: Operator '+' cannot be applied to operand 'new()'
                //         var a = new() + new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() + new()").WithArguments("+", "new()").WithLocation(6, 17),
                // (7,17): error CS8310: Operator '-' cannot be applied to operand 'new()'
                //         var b = new() - new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() - new()").WithArguments("-", "new()").WithLocation(7, 17),
                // (8,17): error CS8310: Operator '&' cannot be applied to operand 'new()'
                //         var c = new() & new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() & new()").WithArguments("&", "new()").WithLocation(8, 17),
                // (9,17): error CS8310: Operator '|' cannot be applied to operand 'new()'
                //         var d = new() | new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() | new()").WithArguments("|", "new()").WithLocation(9, 17),
                // (10,17): error CS8310: Operator '^' cannot be applied to operand 'new()'
                //         var e = new() ^ new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() ^ new()").WithArguments("^", "new()").WithLocation(10, 17),
                // (11,17): error CS8310: Operator '*' cannot be applied to operand 'new()'
                //         var f = new() * new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() * new()").WithArguments("*", "new()").WithLocation(11, 17),
                // (12,17): error CS8310: Operator '/' cannot be applied to operand 'new()'
                //         var g = new() / new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() / new()").WithArguments("/", "new()").WithLocation(12, 17),
                // (13,17): error CS8310: Operator '%' cannot be applied to operand 'new()'
                //         var h = new() % new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() % new()").WithArguments("%", "new()").WithLocation(13, 17),
                // (14,17): error CS8310: Operator '>>' cannot be applied to operand 'new()'
                //         var i = new() >> new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() >> new()").WithArguments(">>", "new()").WithLocation(14, 17),
                // (15,17): error CS8310: Operator '<<' cannot be applied to operand 'new()'
                //         var j = new() << new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() << new()").WithArguments("<<", "new()").WithLocation(15, 17),
                // (16,17): error CS8310: Operator '>' cannot be applied to operand 'new()'
                //         var k = new() > new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() > new()").WithArguments(">", "new()").WithLocation(16, 17),
                // (17,17): error CS8310: Operator '<' cannot be applied to operand 'new()'
                //         var l = new() < new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() < new()").WithArguments("<", "new()").WithLocation(17, 17),
                // (18,17): error CS8310: Operator '>=' cannot be applied to operand 'new()'
                //         var m = new() >= new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() >= new()").WithArguments(">=", "new()").WithLocation(18, 17),
                // (19,17): error CS8310: Operator '<=' cannot be applied to operand 'new()'
                //         var n = new() <= new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() <= new()").WithArguments("<=", "new()").WithLocation(19, 17),
                // (20,17): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         var o = new() == new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() == new()").WithArguments("==", "new()").WithLocation(20, 17),
                // (21,17): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         var p = new() != new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() != new()").WithArguments("!=", "new()").WithLocation(21, 17),
                // (22,17): error CS8754: There is no target type for 'new()'
                //         var q = new() && new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(22, 17),
                // (22,26): error CS8754: There is no target type for 'new()'
                //         var q = new() && new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(22, 26),
                // (23,17): error CS8754: There is no target type for 'new()'
                //         var r = new() || new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(23, 17),
                // (23,26): error CS8754: There is no target type for 'new()'
                //         var r = new() || new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(23, 26),
                // (24,17): error CS8754: There is no target type for 'new()'
                //         var s = new() ?? new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(24, 17)
                );
        }

        [Fact]
        public void TestBinaryOperators02()
        {
            string source = @"
class C
{
    static void Main()
    {
        _ = new() + 1;
        _ = new() - 1;
        _ = new() & 1;
        _ = new() | 1;
        _ = new() ^ 1;
        _ = new() * 1;
        _ = new() / 1;
        _ = new() % 1;
        _ = new() >> 1;
        _ = new() << 1;
        _ = new() > 1;
        _ = new() < 1;
        _ = new() >= 1;
        _ = new() <= 1;
        _ = new() == 1;
        _ = new() != 1;
        _ = new() && 1;
        _ = new() || 1;
        _ = new() ?? 1;
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,13): error CS8310: Operator '+' cannot be applied to operand 'new()'
                //         _ = new() + 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() + 1").WithArguments("+", "new()").WithLocation(6, 13),
                // (7,13): error CS8310: Operator '-' cannot be applied to operand 'new()'
                //         _ = new() - 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() - 1").WithArguments("-", "new()").WithLocation(7, 13),
                // (8,13): error CS8310: Operator '&' cannot be applied to operand 'new()'
                //         _ = new() & 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() & 1").WithArguments("&", "new()").WithLocation(8, 13),
                // (9,13): error CS8310: Operator '|' cannot be applied to operand 'new()'
                //         _ = new() | 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() | 1").WithArguments("|", "new()").WithLocation(9, 13),
                // (10,13): error CS8310: Operator '^' cannot be applied to operand 'new()'
                //         _ = new() ^ 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() ^ 1").WithArguments("^", "new()").WithLocation(10, 13),
                // (11,13): error CS8310: Operator '*' cannot be applied to operand 'new()'
                //         _ = new() * 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() * 1").WithArguments("*", "new()").WithLocation(11, 13),
                // (12,13): error CS8310: Operator '/' cannot be applied to operand 'new()'
                //         _ = new() / 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() / 1").WithArguments("/", "new()").WithLocation(12, 13),
                // (13,13): error CS8310: Operator '%' cannot be applied to operand 'new()'
                //         _ = new() % 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() % 1").WithArguments("%", "new()").WithLocation(13, 13),
                // (14,13): error CS8310: Operator '>>' cannot be applied to operand 'new()'
                //         _ = new() >> 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() >> 1").WithArguments(">>", "new()").WithLocation(14, 13),
                // (15,13): error CS8310: Operator '<<' cannot be applied to operand 'new()'
                //         _ = new() << 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() << 1").WithArguments("<<", "new()").WithLocation(15, 13),
                // (16,13): error CS8310: Operator '>' cannot be applied to operand 'new()'
                //         _ = new() > 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() > 1").WithArguments(">", "new()").WithLocation(16, 13),
                // (17,13): error CS8310: Operator '<' cannot be applied to operand 'new()'
                //         _ = new() < 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() < 1").WithArguments("<", "new()").WithLocation(17, 13),
                // (18,13): error CS8310: Operator '>=' cannot be applied to operand 'new()'
                //         _ = new() >= 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() >= 1").WithArguments(">=", "new()").WithLocation(18, 13),
                // (19,13): error CS8310: Operator '<=' cannot be applied to operand 'new()'
                //         _ = new() <= 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() <= 1").WithArguments("<=", "new()").WithLocation(19, 13),
                // (20,13): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         _ = new() == 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() == 1").WithArguments("==", "new()").WithLocation(20, 13),
                // (21,13): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         _ = new() != 1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() != 1").WithArguments("!=", "new()").WithLocation(21, 13),
                // (22,13): error CS8754: There is no target type for 'new()'
                //         _ = new() && 1;
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(22, 13),
                // (23,13): error CS8754: There is no target type for 'new()'
                //         _ = new() || 1;
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(23, 13),
                // (24,13): error CS8754: There is no target type for 'new()'
                //         _ = new() ?? 1;
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(24, 13)
                );
        }

        [Fact]
        public void TestBinaryOperators03()
        {
            string source = @"
class C
{
    static void Main()
    {
        _ = 1 + new();
        _ = 1 - new();
        _ = 1 & new();
        _ = 1 | new();
        _ = 1 ^ new();
        _ = 1 * new();
        _ = 1 / new();
        _ = 1 % new();
        _ = 1 >> new();
        _ = 1 << new();
        _ = 1 > new();
        _ = 1 < new();
        _ = 1 >= new();
        _ = 1 <= new();
        _ = 1 == new();
        _ = 1 != new();
        _ = 1 && new();
        _ = 1 || new();
        _ = 1 ?? new();
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,13): error CS8310: Operator '+' cannot be applied to operand 'new()'
                //         _ = 1 + new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 + new()").WithArguments("+", "new()").WithLocation(6, 13),
                // (7,13): error CS8310: Operator '-' cannot be applied to operand 'new()'
                //         _ = 1 - new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 - new()").WithArguments("-", "new()").WithLocation(7, 13),
                // (8,13): error CS8310: Operator '&' cannot be applied to operand 'new()'
                //         _ = 1 & new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 & new()").WithArguments("&", "new()").WithLocation(8, 13),
                // (9,13): error CS8310: Operator '|' cannot be applied to operand 'new()'
                //         _ = 1 | new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 | new()").WithArguments("|", "new()").WithLocation(9, 13),
                // (10,13): error CS8310: Operator '^' cannot be applied to operand 'new()'
                //         _ = 1 ^ new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 ^ new()").WithArguments("^", "new()").WithLocation(10, 13),
                // (11,13): error CS8310: Operator '*' cannot be applied to operand 'new()'
                //         _ = 1 * new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 * new()").WithArguments("*", "new()").WithLocation(11, 13),
                // (12,13): error CS8310: Operator '/' cannot be applied to operand 'new()'
                //         _ = 1 / new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 / new()").WithArguments("/", "new()").WithLocation(12, 13),
                // (13,13): error CS8310: Operator '%' cannot be applied to operand 'new()'
                //         _ = 1 % new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 % new()").WithArguments("%", "new()").WithLocation(13, 13),
                // (14,13): error CS8310: Operator '>>' cannot be applied to operand 'new()'
                //         _ = 1 >> new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 >> new()").WithArguments(">>", "new()").WithLocation(14, 13),
                // (15,13): error CS8310: Operator '<<' cannot be applied to operand 'new()'
                //         _ = 1 << new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 << new()").WithArguments("<<", "new()").WithLocation(15, 13),
                // (16,13): error CS8310: Operator '>' cannot be applied to operand 'new()'
                //         _ = 1 > new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 > new()").WithArguments(">", "new()").WithLocation(16, 13),
                // (17,13): error CS8310: Operator '<' cannot be applied to operand 'new()'
                //         _ = 1 < new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 < new()").WithArguments("<", "new()").WithLocation(17, 13),
                // (18,13): error CS8310: Operator '>=' cannot be applied to operand 'new()'
                //         _ = 1 >= new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 >= new()").WithArguments(">=", "new()").WithLocation(18, 13),
                // (19,13): error CS8310: Operator '<=' cannot be applied to operand 'new()'
                //         _ = 1 <= new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 <= new()").WithArguments("<=", "new()").WithLocation(19, 13),
                // (20,13): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         _ = 1 == new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 == new()").WithArguments("==", "new()").WithLocation(20, 13),
                // (21,13): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         _ = 1 != new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "1 != new()").WithArguments("!=", "new()").WithLocation(21, 13),
                // (22,18): error CS8754: There is no target type for 'new()'
                //         _ = 1 && new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(22, 18),
                // (23,18): error CS8754: There is no target type for 'new()'
                //         _ = 1 || new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(23, 18),
                // (24,13): error CS0019: Operator '??' cannot be applied to operands of type 'int' and 'new()'
                //         _ = 1 ?? new();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "1 ?? new()").WithArguments("??", "int", "new()").WithLocation(24, 13)
                );
        }

        [Fact]
        public void InForeach()
        {
            var text = @"
class C
{
    static void Main()
    {
        foreach (int x in new()) { }
    }
}";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,27): error CS8754: There is no target type for 'new()'
                //         foreach (int x in new()) { }
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 27)
                );
        }

        [Fact]
        public void Query()
        {
            string source =
@"using System.Linq;
static class C
{
    static void Main()
    {
        var q = from x in new() select x;
        var p = from x in new int[] { 1 } select new();
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (6,27): error CS8754: There is no target type for 'new()'
                //         var q = from x in new() select x;
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 27),
                // (7,43): error CS1942: The type of the expression in the select clause is incorrect.  Type inference failed in the call to 'Select'.
                //         var p = from x in new int[] { 1 } select new();
                Diagnostic(ErrorCode.ERR_QueryTypeInferenceFailed, "select").WithArguments("select", "Select").WithLocation(7, 43)
                );
        }

        [Fact]
        public void InIsOperator()
        {
            var text = @"
class C
{
    void M()
    {
        bool v1 = new() is long;
        bool v2 = new() is string;
        bool v3 = new() is new();
        bool v4 = v1 is new();
        bool v5 = this is new();
    }
}";

            var comp = CreateCompilation(text, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (6,19): error CS8754: There is no target type for 'new()'
                //         bool v1 = new() is long;
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(6, 19),
                // (7,19): error CS8754: There is no target type for 'new()'
                //         bool v2 = new() is string;
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(7, 19),
                // (8,19): error CS8754: There is no target type for 'new()'
                //         bool v3 = new() is new();
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(8, 19),
                // (10,27): error CS0150: A constant value is expected
                //         bool v5 = this is new();
                Diagnostic(ErrorCode.ERR_ConstantExpected, "new()").WithLocation(10, 27)
                );
        }

        [Fact]
        public void InNullCoalescing()
        {
            var text =
@"using System;

class Program
{
    static void Main()
    {
        Func<object> f = () => new() ?? ""hello"";
    }
}";

            var comp = CreateCompilation(text).VerifyDiagnostics(
                // (7,32): error CS8754: There is no target type for 'new()'
                //         Func<object> f = () => new() ?? "hello";
                Diagnostic(ErrorCode.ERR_TypelessNewNoTargetType, "new()").WithArguments("new()").WithLocation(7, 32)
                );
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
    static System.Func<C> M()
    {
        return () => new();
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C");
        }

        [Fact]
        public void TestTupleEquality01()
        {
            string source = @"
using System;
class C
{
    public static void Main()
    {
        Console.Write(new() == (1, 2L) ? 1 : 0);
        Console.Write(new() != (1, 2L) ? 1 : 0);
        Console.Write((1, 2L) == new() ? 1 : 0);
        Console.Write((1, 2L) != new() ? 1 : 0);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,23): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.Write(new() == (1, 2L) ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() == (1, 2L)").WithArguments("==", "new()").WithLocation(7, 23),
                // (8,23): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.Write(new() != (1, 2L) ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() != (1, 2L)").WithArguments("!=", "new()").WithLocation(8, 23),
                // (9,23): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.Write((1, 2L) == new() ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "(1, 2L) == new()").WithArguments("==", "new()").WithLocation(9, 23),
                // (10,23): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.Write((1, 2L) != new() ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "(1, 2L) != new()").WithArguments("!=", "new()").WithLocation(10, 23)
                );
        }

        [Fact]
        public void TestTupleEquality02()
        {
            string source = @"
using System;

class C
{
    public static void Main()
    {
        Console.Write((new(), new()) == (1, 2L) ? 1 : 0);
        Console.Write((new(), new()) != (1, 2L) ? 1 : 0);
        Console.Write((1, 2L) == (new(), new()) ? 1 : 0);
        Console.Write((1, 2L) != (new(), new()) ? 1 : 0);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (8,23): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.Write((new(), new()) == (1, 2L) ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "(new(), new()) == (1, 2L)").WithArguments("==", "new()").WithLocation(8, 23),
                // (8,23): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.Write((new(), new()) == (1, 2L) ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "(new(), new()) == (1, 2L)").WithArguments("==", "new()").WithLocation(8, 23),
                // (9,23): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.Write((new(), new()) != (1, 2L) ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "(new(), new()) != (1, 2L)").WithArguments("!=", "new()").WithLocation(9, 23),
                // (9,23): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.Write((new(), new()) != (1, 2L) ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "(new(), new()) != (1, 2L)").WithArguments("!=", "new()").WithLocation(9, 23),
                // (10,23): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.Write((1, 2L) == (new(), new()) ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "(1, 2L) == (new(), new())").WithArguments("==", "new()").WithLocation(10, 23),
                // (10,23): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.Write((1, 2L) == (new(), new()) ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "(1, 2L) == (new(), new())").WithArguments("==", "new()").WithLocation(10, 23),
                // (11,23): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.Write((1, 2L) != (new(), new()) ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "(1, 2L) != (new(), new())").WithArguments("!=", "new()").WithLocation(11, 23),
                // (11,23): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.Write((1, 2L) != (new(), new()) ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "(1, 2L) != (new(), new())").WithArguments("!=", "new()").WithLocation(11, 23)
                );
        }

        [Fact]
        public void TestEquality_Class()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        Console.Write(new C() == new() ? 1 : 0);
        Console.Write(new C() != new() ? 1 : 0);
        Console.Write(new() == new C() ? 1 : 0);
        Console.Write(new() != new C() ? 1 : 0);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (8,23): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.Write(new C() == new() ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new C() == new()").WithArguments("==", "new()").WithLocation(8, 23),
                // (9,23): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.Write(new C() != new() ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new C() != new()").WithArguments("!=", "new()").WithLocation(9, 23),
                // (10,23): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.Write(new() == new C() ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() == new C()").WithArguments("==", "new()").WithLocation(10, 23),
                // (11,23): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.Write(new() != new C() ? 1 : 0);
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() != new C()").WithArguments("!=", "new()").WithLocation(11, 23)
                );
        }

        [Fact]
        public void TestEquality_Class_UserDefinedOperator()
        {
            string source = @"
#pragma warning disable CS0660, CS0661
using System;

class D
{
}

class C
{
    public static bool operator ==(C o1, C o2) => default;
    public static bool operator !=(C o1, C o2) => default;
    public static bool operator ==(C o1, D o2) => default;
    public static bool operator !=(C o1, D o2) => default;

    static void Main()
    {
        Console.WriteLine(new C() == new());
        Console.WriteLine(new() == new C());
        Console.WriteLine(new C() != new());
        Console.WriteLine(new() != new C());
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (18,27): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.WriteLine(new C() == new());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new C() == new()").WithArguments("==", "new()").WithLocation(18, 27),
                // (19,27): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.WriteLine(new() == new C());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() == new C()").WithArguments("==", "new()").WithLocation(19, 27),
                // (20,27): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.WriteLine(new C() != new());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new C() != new()").WithArguments("!=", "new()").WithLocation(20, 27),
                // (21,27): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.WriteLine(new() != new C());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() != new C()").WithArguments("!=", "new()").WithLocation(21, 27)
                );
        }

        [Fact]
        public void TestEquality_Struct()
        {
            string source = @"
using System;

struct S
{
    static void Main()
    {
        Console.WriteLine(new S() == new());
        Console.WriteLine(new() == new S());
        Console.WriteLine(new S() != new());
        Console.WriteLine(new() != new S());

        Console.WriteLine(new S?() == new());
        Console.WriteLine(new() == new S?());
        Console.WriteLine(new S?() != new());
        Console.WriteLine(new() != new S?());
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (8,27): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.WriteLine(new S() == new());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new S() == new()").WithArguments("==", "new()").WithLocation(8, 27),
                // (9,27): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.WriteLine(new() == new S());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() == new S()").WithArguments("==", "new()").WithLocation(9, 27),
                // (10,27): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.WriteLine(new S() != new());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new S() != new()").WithArguments("!=", "new()").WithLocation(10, 27),
                // (11,27): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.WriteLine(new() != new S());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() != new S()").WithArguments("!=", "new()").WithLocation(11, 27),
                // (13,27): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.WriteLine(new S?() == new());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new S?() == new()").WithArguments("==", "new()").WithLocation(13, 27),
                // (14,27): error CS8310: Operator '==' cannot be applied to operand 'new()'
                //         Console.WriteLine(new() == new S?());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() == new S?()").WithArguments("==", "new()").WithLocation(14, 27),
                // (15,27): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.WriteLine(new S?() != new());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new S?() != new()").WithArguments("!=", "new()").WithLocation(15, 27),
                // (16,27): error CS8310: Operator '!=' cannot be applied to operand 'new()'
                //         Console.WriteLine(new() != new S?());
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() != new S?()").WithArguments("!=", "new()").WithLocation(16, 27)
                );
        }

        [Fact]
        public void TestEquality_Struct_UserDefinedOperator()
        {
            string source = @"
#pragma warning disable CS0660, CS0661
using System;

struct S
{
    public S(int i)
    {
    }

    public static bool operator ==(S o1, S o2) => default;
    public static bool operator !=(S o1, S o2) => default;

    static void Main()
    {
        Console.WriteLine(new S(42) == new(42));
        Console.WriteLine(new(42) == new S(42));
        Console.WriteLine(new S(42) != new(42));
        Console.WriteLine(new(42) != new S(42));

        Console.WriteLine(new S?(new(42)) == new(42));
        Console.WriteLine(new(42) == new S?(new(42)));
        Console.WriteLine(new S?(new(42)) != new(42));
        Console.WriteLine(new(42) != new S?(new(42)));
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (16,27): error CS8310: Operator '==' cannot be applied to operand 'new(int)'
                //         Console.WriteLine(new S(42) == new(42));
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new S(42) == new(42)").WithArguments("==", "new(int)").WithLocation(16, 27),
                // (17,27): error CS8310: Operator '==' cannot be applied to operand 'new(int)'
                //         Console.WriteLine(new(42) == new S(42));
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new(42) == new S(42)").WithArguments("==", "new(int)").WithLocation(17, 27),
                // (18,27): error CS8310: Operator '!=' cannot be applied to operand 'new(int)'
                //         Console.WriteLine(new S(42) != new(42));
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new S(42) != new(42)").WithArguments("!=", "new(int)").WithLocation(18, 27),
                // (19,27): error CS8310: Operator '!=' cannot be applied to operand 'new(int)'
                //         Console.WriteLine(new(42) != new S(42));
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new(42) != new S(42)").WithArguments("!=", "new(int)").WithLocation(19, 27),
                // (21,27): error CS8310: Operator '==' cannot be applied to operand 'new(int)'
                //         Console.WriteLine(new S?(new(42)) == new(42));
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new S?(new(42)) == new(42)").WithArguments("==", "new(int)").WithLocation(21, 27),
                // (22,27): error CS8310: Operator '==' cannot be applied to operand 'new(int)'
                //         Console.WriteLine(new(42) == new S?(new(42)));
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new(42) == new S?(new(42))").WithArguments("==", "new(int)").WithLocation(22, 27),
                // (23,27): error CS8310: Operator '!=' cannot be applied to operand 'new(int)'
                //         Console.WriteLine(new S?(new(42)) != new(42));
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new S?(new(42)) != new(42)").WithArguments("!=", "new(int)").WithLocation(23, 27),
                // (24,27): error CS8310: Operator '!=' cannot be applied to operand 'new(int)'
                //         Console.WriteLine(new(42) != new S?(new(42)));
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new(42) != new S?(new(42))").WithArguments("!=", "new(int)").WithLocation(24, 27)
                );
        }

        [Fact]
        public void ArraySize()
        {
            string source = @"
class C
{
    static void Main()
    {
        var a = new int[new()];
        System.Console.Write(a.Length);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void TernaryOperator01()
        {
            string source = @"
class C
{
    static void Main()
    {
        bool flag = true;
        var x = flag ? new() : 1;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TernaryOperator02()
        {
            string source = @"
class C
{
    static void Main()
    {
        bool flag = true;
        System.Console.Write(flag ? new() : new C());
        System.Console.Write(flag ? new C() : new());
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "CC");
        }

        [Fact]
        public void TernaryOperator03()
        {
            string source = @"
class C
{
    static void Main()
    {
        bool flag = true;
        System.Console.Write(flag ? new() : new());
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,24): error CS0121: The call is ambiguous between the following methods or properties: 'Console.Write(bool)' and 'Console.Write(char)'
                //         System.Console.Write(flag ? new() : new());
                Diagnostic(ErrorCode.ERR_AmbigCall, "Write").WithArguments("System.Console.Write(bool)", "System.Console.Write(char)").WithLocation(7, 24)
                );
        }

        [Fact]
        public void NotAType()
        {
            string source = @"
class C
{
    static void Main()
    {
        ((System)new()).ToString();
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,11): error CS0118: 'System' is a namespace but is used like a type
                //         ((System)new()).ToString();
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "type").WithLocation(6, 11)
                );
        }

        [Fact]
        public void TestSpeculativeModel01()
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
            var comp = CreateCompilation(source, parseOptions: TargetTypedObjectCreationTestOptions);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();
            int nodeLocation = node.Location.SourceSpan.Start;

            var newExpression = SyntaxFactory.ParseExpression("new()");
            var typeInfo = model.GetSpeculativeTypeInfo(nodeLocation, newExpression, SpeculativeBindingOption.BindAsExpression);
            Assert.Null(typeInfo.Type);
            var symbolInfo = model.GetSpeculativeSymbolInfo(nodeLocation, newExpression, SpeculativeBindingOption.BindAsExpression);
            Assert.True(symbolInfo.IsEmpty);
        }

        [Fact]
        public void TestSpeculativeModel02()
        {
            string source = @"
class C
{
    static void M(int i) {}
    static void Main()
    {
        M(42);
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TargetTypedObjectCreationTestOptions);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExpressionStatementSyntax>().Single();
            int nodeLocation = node.Location.SourceSpan.Start;

            var modifiedNode = (ExpressionStatementSyntax)SyntaxFactory.ParseStatement("M(new());", options: TargetTypedObjectCreationTestOptions);
            Assert.False(modifiedNode.HasErrors);

            bool success = model.TryGetSpeculativeSemanticModel(nodeLocation, modifiedNode, out var speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var newExpression = ((InvocationExpressionSyntax)modifiedNode.Expression).ArgumentList.Arguments[0].Expression;
            var symbolInfo = speculativeModel.GetSymbolInfo(newExpression);
            Assert.Equal("System.Int32..ctor()", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = speculativeModel.GetTypeInfo(newExpression);
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
        }

        [Fact]
        public void TestInOverloadWithIllegalConversion()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M(new());
        M(array: new());
    }
    static void M(int[] array) { }
    static void M(int i) { }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(int[])' and 'C.M(int)'
                //         M(new());
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(int[])", "C.M(int)").WithLocation(6, 9),
                // (7,18): error CS8752: The type 'int[]' may not be used as the target type of new()
                //         M(array: new());
                Diagnostic(ErrorCode.ERR_TypelessNewIllegalTargetType, "new()").WithArguments("int[]").WithLocation(7, 18)
                );
        }

        [Fact]
        public void TestInOverloadWithUseSiteError()
        {
            var missing = @"public class Missing { }";
            var missingComp = CreateCompilation(missing, assemblyName: "missing");

            var lib = @"
public class C
{
    public void M(Missing m) { }
    public void M(C c) { }
}";
            var libComp = CreateCompilation(lib, references: new[] { missingComp.EmitToImageReference() });

            var source = @"
class D
{
    public void M2(C c)
    {
        c.M(new());
        c.M(default);
        c.M(null);
    }
}
";
            var comp = CreateCompilation(source, references: new[] { libComp.EmitToImageReference() });
            comp.VerifyDiagnostics(
                // (6,9): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c.M(new());
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c.M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 9),
                // (7,9): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c.M(default);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c.M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 9),
                // (8,9): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c.M(null);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c.M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 9)
                );
        }

        [Fact]
        public void TestInConstructorOverloadWithUseSiteError()
        {
            var missing = @"public class Missing { }";
            var missingComp = CreateCompilation(missing, assemblyName: "missing");

            var lib = @"
public class C
{
    public C(Missing m) => throw null;
    public C(D d) => throw null;
}
public class D { }
";
            var libComp = CreateCompilation(lib, references: new[] { missingComp.EmitToImageReference() });

            var source = @"
class D
{
    public void M()
    {
        new C(new());
        new C(default);
        new C(null);
        C c = new(null);
    }
}
";
            var comp = CreateCompilation(source, references: new[] { libComp.EmitToImageReference() });
            comp.VerifyDiagnostics(
                // (6,13): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         new C(new());
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 13),
                // (7,13): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         new C(default);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 13),
                // (8,13): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         new C(null);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 13),
                // (9,15): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         C c = new(null);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new(null)").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(9, 15)
                );
        }

        [Fact]
        public void TargetTypedNewHasUseSiteError()
        {
            var missing = @"public class Missing { }";
            var missingComp = CreateCompilation(missing, assemblyName: "missing");

            var lib = @"
public class C
{
    public static void M(Missing m) => throw null;
}
";
            var libComp = CreateCompilation(lib, references: new[] { missingComp.EmitToImageReference() });
            libComp.VerifyDiagnostics();

            var source = @"
class D
{
    public void M2()
    {
        C.M(new());
    }
}
";
            var comp = CreateCompilation(source, references: new[] { libComp.EmitToImageReference() });
            comp.VerifyDiagnostics(
                // (6,9): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         C.M(new());
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C.M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ArgumentOfTargetTypedNewHasUseSiteError()
        {
            var missing = @"public class Missing { }";
            var missingComp = CreateCompilation(missing, assemblyName: "missing");

            var lib = @"
public class C
{
    public C(Missing m) => throw null;
}
";
            var libComp = CreateCompilation(lib, references: new[] { missingComp.EmitToImageReference() });
            libComp.VerifyDiagnostics();

            var source = @"
class D
{
    public void M(C c) { }
    public void M2()
    {
        M(new(null));
    }
}
";
            var comp = CreateCompilation(source, references: new[] { libComp.EmitToImageReference() });
            comp.VerifyDiagnostics(
                // (7,11): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         M(new(null));
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new(null)").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 11)
                );
        }

        [Fact]
        public void UseSiteWarning()
        {
            var signedDll = TestOptions.ReleaseDll.WithCryptoPublicKey(TestResources.TestKeys.PublicKey_ce65828c82a341f2);

            var libBTemplate = @"
[assembly: System.Reflection.AssemblyVersion(""{0}.0.0.0"")]
public class B {{ }}
";

            var libBv1 = CreateCompilation(string.Format(libBTemplate, "1"), assemblyName: "B", options: signedDll);
            var libBv2 = CreateCompilation(string.Format(libBTemplate, "2"), assemblyName: "B", options: signedDll);

            var libASource = @"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]

public class A
{
    public void M(B b) { }
    public void M(string s) { }
}
";

            var libAv1 = CreateCompilation(
                libASource,
                new[] { new CSharpCompilationReference(libBv1) },
                assemblyName: "A",
                options: signedDll);

            var source = @"
public class Source
{
    public void Test(A a)
    {
        a.M(new());
        a.M(default);
        a.M(null);
    }
}
";

            var comp = CreateCompilation(source, new[] { new CSharpCompilationReference(libAv1), new CSharpCompilationReference(libBv2) },
                parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics(
                // (6,9): warning CS1701: Assuming assembly reference 'B, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' used by 'A' matches identity 'B, Version=2.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' of 'B', you may need to supply runtime policy
                //         a.M(new());
                Diagnostic(ErrorCode.WRN_UnifyReferenceMajMin, "a.M").WithArguments("B, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "A", "B, Version=2.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "B").WithLocation(6, 9),
                // (6,11): error CS0121: The call is ambiguous between the following methods or properties: 'A.M(B)' and 'A.M(string)'
                //         a.M(new());
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("A.M(B)", "A.M(string)").WithLocation(6, 11),
                // (7,9): warning CS1701: Assuming assembly reference 'B, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' used by 'A' matches identity 'B, Version=2.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' of 'B', you may need to supply runtime policy
                //         a.M(default);
                Diagnostic(ErrorCode.WRN_UnifyReferenceMajMin, "a.M").WithArguments("B, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "A", "B, Version=2.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "B").WithLocation(7, 9),
                // (7,11): error CS0121: The call is ambiguous between the following methods or properties: 'A.M(B)' and 'A.M(string)'
                //         a.M(default);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("A.M(B)", "A.M(string)").WithLocation(7, 11),
                // (8,9): warning CS1701: Assuming assembly reference 'B, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' used by 'A' matches identity 'B, Version=2.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' of 'B', you may need to supply runtime policy
                //         a.M(null);
                Diagnostic(ErrorCode.WRN_UnifyReferenceMajMin, "a.M").WithArguments("B, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "A", "B, Version=2.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "B").WithLocation(8, 9),
                // (8,11): error CS0121: The call is ambiguous between the following methods or properties: 'A.M(B)' and 'A.M(string)'
                //         a.M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("A.M(B)", "A.M(string)").WithLocation(8, 11)
                );
        }
    }
}
