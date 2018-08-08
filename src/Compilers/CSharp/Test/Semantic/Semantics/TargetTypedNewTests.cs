// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

using static TestResources.NetFX.ValueTuple;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.TargetTypedNew)]
    public class TargetTypedNewTests : CSharpTestBase
    {
        private static readonly string s_trivial2uple =
@"namespace System
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
            return $""({this.Item1}, {this.Item2})"";
        }
    }
}";

        [Fact]
        public void TestExpressionTree()
        {
            var source = @"
using System;
using System.Linq.Expressions;

struct S
{
    public S(int i) {}
}

class C
{
    public static void Main()
    {
        Expression<Func<C>> expr1 = () => new();
        Expression<Func<S>> expr2 = () => new(1);
        Expression<Func<S?>> expr3 = () => new(2);
    }
}
";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            validate(0, "new()", type: "C", convertedType: "C", symbol: "C..ctor()", ConversionKind.Identity);
            validate(1, "new(1)", type: "S", convertedType: "S", symbol: "S..ctor(System.Int32 i)", ConversionKind.Identity);
            validate(2, "new(2)", type: "S", convertedType: "S?", symbol: "S..ctor(System.Int32 i)", ConversionKind.ImplicitNullable);

            void validate(int index, string expression, string type, string convertedType, string symbol, ConversionKind conversionKind)
            {
                var @new = nodes.OfType<ObjectCreationExpressionSyntax>().ElementAt(index);
                Assert.Equal(expression, @new.ToString());
                Assert.Equal(type, model.GetTypeInfo(@new).Type.ToTestDisplayString());
                Assert.Equal(convertedType, model.GetTypeInfo(@new).ConvertedType.ToTestDisplayString());
                Assert.Equal(symbol, model.GetSymbolInfo(@new).Symbol.ToTestDisplayString());
                Assert.Equal(model.GetConversion(@new).Kind, conversionKind);
            }
        }

        [Fact]
        public void TestLocal()
        {
            var source = @"
using System;

struct S
{
    public S(int i) {}
}

class C
{
    public static void Main()
    {
        C v1 = new();
        S v2 = new(1);
        S? v3 = new(2);
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
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            validate(0, "new()", type: "C", convertedType: "C", symbol: "C..ctor()", ConversionKind.Identity);
            validate(1, "new(1)", type: "S", convertedType: "S", symbol: "S..ctor(System.Int32 i)", ConversionKind.Identity);
            validate(2, "new(2)", type: "S", convertedType: "S?", symbol: "S..ctor(System.Int32 i)", ConversionKind.ImplicitNullable);

            void validate(int index, string expression, string type, string convertedType, string symbol, ConversionKind conversionKind)
            {
                var @new = nodes.OfType<ObjectCreationExpressionSyntax>().ElementAt(index);
                Assert.Equal(expression, @new.ToString());
                Assert.Equal(type, model.GetTypeInfo(@new).Type.ToTestDisplayString());
                Assert.Equal(convertedType, model.GetTypeInfo(@new).ConvertedType.ToTestDisplayString());
                Assert.Equal(symbol, model.GetSymbolInfo(@new).Symbol.ToTestDisplayString());
                Assert.Equal(model.GetConversion(@new).Kind, conversionKind);
            }
        }

        [Fact]
        public void TestParameterDefaultValue()
        {
            var source = @"
struct S
{
}

class C
{
    void M(
        C p1 = new(),
        S p2 = new(),
        S? p3 = new()
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
                // (10,16): error CS9367: The default constructor of the value type 'S' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         S p2 = new(),
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("S").WithLocation(10, 16),
                // (10,16): error CS1736: Default parameter value for 'p2' must be a compile-time constant
                //         S p2 = new(),
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("p2").WithLocation(10, 16),
                // (11,17): error CS9367: The default constructor of the value type 'S' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         S? p3 = new()
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("S").WithLocation(11, 17),
                // (11,17): error CS1736: Default parameter value for 'p3' must be a compile-time constant
                //         S? p3 = new()
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("p3").WithLocation(11, 17)
                );
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
            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "5");
        }

        [Fact]
        public void TestInAsOperator()
        {
            var source = @"
using System;

class C 
{
    public static void Main()
    {
        Console.Write(new() as C);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (8,23): error CS9365: The first operand of an 'as' operator may not be a target-typed 'new'
                //         Console.Write(new() as C);
                Diagnostic(ErrorCode.ERR_TypelessNewInAs, "new() as C").WithLocation(8, 23)
                );
        }

        [Fact]
        public void TestTupleElement()
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
" + s_trivial2uple + tupleattributes_cs;

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "(C, C)");
        }

        [Fact]
        public void TestBadTargetType_Assignment()
        {
            var source = @"
using System;

struct Struct {}
abstract class Abstract {}
static class Static {}
interface Interface {}
enum Enumeration {}

unsafe class C
{
    public void Test<T>()
    {
        var v0 = new();
        Struct v1 = new();
        Action v2 = new();
        Static v3 = new();
        Abstract v4 = new();
        Interface v5 = new();
        Enumeration v6 = new();
        int v7 = new();
        int* v8 = new();
        int? v9 = new();
        (int, int) v10 = new();
        dynamic v11 = new();
        int[] v12 = new();
        Error v13 = new();
        T v14 = new();
        ValueTuple<int, int> v15 = new();
        ValueTuple<int, int> v16 = new(2, 3);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (14,13): error CS0815: Cannot assign new(...) to an implicitly-typed variable
                //         var v0 = new();
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "v0 = new()").WithArguments("new(...)").WithLocation(14, 13),
                // (15,21): error CS9367: The default constructor of the value type 'Struct' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         Struct v1 = new();
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("Struct").WithLocation(15, 21),
                // (16,21): error CS9366: The type 'Action' may not be used as the target-type of 'new(...)'
                //         Action v2 = new();
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "new()").WithArguments("System.Action").WithLocation(16, 21),
                // (17,9): error CS0723: Cannot declare a variable of static type 'Static'
                //         Static v3 = new();
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "Static").WithArguments("Static").WithLocation(17, 9),
                // (17,21): error CS1729: 'Static' does not contain a constructor that takes 0 arguments
                //         Static v3 = new();
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("Static", "0").WithLocation(17, 21),
                // (18,23): error CS0144: Cannot create an instance of the abstract class or interface 'Abstract'
                //         Abstract v4 = new();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new()").WithArguments("Abstract").WithLocation(18, 23),
                // (19,24): error CS9366: The type 'Interface' may not be used as the target-type of 'new(...)'
                //         Interface v5 = new();
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "new()").WithArguments("Interface").WithLocation(19, 24),
                // (20,26): error CS9366: The type 'Enumeration' may not be used as the target-type of 'new(...)'
                //         Enumeration v6 = new();
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "new()").WithArguments("Enumeration").WithLocation(20, 26),
                // (21,18): error CS9367: The default constructor of the value type 'int' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         int v7 = new();
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(21, 18),
                // (22,19): error CS1919: Unsafe type 'int*' cannot be used in object creation
                //         int* v8 = new();
                Diagnostic(ErrorCode.ERR_UnsafeTypeInObjectCreation, "new()").WithArguments("int*").WithLocation(22, 19),
                // (23,19): error CS9367: The default constructor of the value type 'int' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         int? v9 = new();
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(23, 19),
                // (24,26): error CS8181: 'new' cannot be used with tuple type. Use a tuple literal expression instead.
                //         (int, int) v10 = new();
                Diagnostic(ErrorCode.ERR_NewWithTupleTypeSyntax, "new()").WithArguments("(int, int)").WithLocation(24, 26),
                // (25,23): error CS0143: The type 'dynamic' has no constructors defined
                //         dynamic v11 = new();
                Diagnostic(ErrorCode.ERR_NoConstructors, "new()").WithArguments("dynamic").WithLocation(25, 23),
                // (26,21): error CS9366: The type 'int[]' may not be used as the target-type of 'new(...)'
                //         int[] v12 = new();
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "new()").WithArguments("int[]").WithLocation(26, 21),
                // (27,9): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                //         Error v13 = new();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(27, 9),
                // (28,17): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //         T v14 = new();
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new()").WithArguments("T").WithLocation(28, 17),
                // (29,36): error CS8181: 'new' cannot be used with tuple type. Use a tuple literal expression instead.
                //         ValueTuple<int, int> v15 = new();
                Diagnostic(ErrorCode.ERR_NewWithTupleTypeSyntax, "new()").WithArguments("(int, int)").WithLocation(29, 36),
                // (30,36): error CS8181: 'new' cannot be used with tuple type. Use a tuple literal expression instead.
                //         ValueTuple<int, int> v16 = new(2, 3);
                Diagnostic(ErrorCode.ERR_NewWithTupleTypeSyntax, "new(2, 3)").WithArguments("(int, int)").WithLocation(30, 36)
                );
        }

        [Fact]
        public void TestBadTargetType_Cast()
        {
            string source = @"
using System;

struct Struct {}
abstract class Abstract {}
static class Static {}
interface Interface {}
enum Enumeration {}

unsafe class C
{
    public void Test<T>()
    {
        var v1 = (Struct)new();
        var v2 = (Action)new();
        var v3 = (Static)new();
        var v4 = (Abstract)new();
        var v5 = (Interface)new();
        var v6 = (Enumeration)new();
        var v7 = (int)new();
        var v8 = (int*)new();
        var v9 = (int?)new();
        var v10 = ((int,int))new();
        var v11 = (dynamic)new();
        var v12 = (int[])new();
        var v13 = (Error)new();
        var v14 = (T)new();
        var v15 = (ValueTuple<int, int>)new();
        var v16 = (ValueTuple<int, int>)new(2, 3);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (14,26): error CS9367: The default constructor of the value type 'Struct' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         var v1 = (Struct)new();
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("Struct").WithLocation(14, 26),
                // (15,18): error CS9366: The type 'Action' may not be used as the target-type of 'new(...)'
                //         var v2 = (Action)new();
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "(Action)new()").WithArguments("System.Action").WithLocation(15, 18),
                // (16,18): error CS0716: Cannot convert to static type 'Static'
                //         var v3 = (Static)new();
                Diagnostic(ErrorCode.ERR_ConvertToStaticClass, "(Static)new()").WithArguments("Static").WithLocation(16, 18),
                // (16,9): error CS0723: Cannot declare a variable of static type 'Static'
                //         var v3 = (Static)new();
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "var").WithArguments("Static").WithLocation(16, 9),
                // (17,28): error CS0144: Cannot create an instance of the abstract class or interface 'Abstract'
                //         var v4 = (Abstract)new();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new()").WithArguments("Abstract").WithLocation(17, 28),
                // (18,18): error CS9366: The type 'Interface' may not be used as the target-type of 'new(...)'
                //         var v5 = (Interface)new();
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "(Interface)new()").WithArguments("Interface").WithLocation(18, 18),
                // (19,18): error CS9366: The type 'Enumeration' may not be used as the target-type of 'new(...)'
                //         var v6 = (Enumeration)new();
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "(Enumeration)new()").WithArguments("Enumeration").WithLocation(19, 18),
                // (20,23): error CS9367: The default constructor of the value type 'int' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         var v7 = (int)new();
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(20, 23),
                // (21,18): error CS1919: Unsafe type 'int*' cannot be used in object creation
                //         var v8 = (int*)new();
                Diagnostic(ErrorCode.ERR_UnsafeTypeInObjectCreation, "(int*)new()").WithArguments("int*").WithLocation(21, 18),
                // (22,24): error CS9367: The default constructor of the value type 'int' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         var v9 = (int?)new();
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(22, 24),
                // (23,19): error CS8181: 'new' cannot be used with tuple type. Use a tuple literal expression instead.
                //         var v10 = ((int,int))new();
                Diagnostic(ErrorCode.ERR_NewWithTupleTypeSyntax, "((int,int))new()").WithArguments("(int, int)").WithLocation(23, 19),
                // (24,19): error CS0143: The type 'dynamic' has no constructors defined
                //         var v11 = (dynamic)new();
                Diagnostic(ErrorCode.ERR_NoConstructors, "(dynamic)new()").WithArguments("dynamic").WithLocation(24, 19),
                // (25,19): error CS9366: The type 'int[]' may not be used as the target-type of 'new(...)'
                //         var v12 = (int[])new();
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "(int[])new()").WithArguments("int[]").WithLocation(25, 19),
                // (26,20): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                //         var v13 = (Error)new();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(26, 20),
                // (27,22): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //         var v14 = (T)new();
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new()").WithArguments("T").WithLocation(27, 22),
                // (28,19): error CS8181: 'new' cannot be used with tuple type. Use a tuple literal expression instead.
                //         var v15 = (ValueTuple<int, int>)new();
                Diagnostic(ErrorCode.ERR_NewWithTupleTypeSyntax, "(ValueTuple<int, int>)new()").WithArguments("(int, int)").WithLocation(28, 19),
                // (29,19): error CS8181: 'new' cannot be used with tuple type. Use a tuple literal expression instead.
                //         var v16 = (ValueTuple<int, int>)new(2, 3);
                Diagnostic(ErrorCode.ERR_NewWithTupleTypeSyntax, "(ValueTuple<int, int>)new(2, 3)").WithArguments("(int, int)").WithLocation(29, 19)
                );
        }

        [Fact]
        public void TestAmbigCall()
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
using System.Collections.Generic;

class C
{
    public C field; 

    public static void Main()
    {
        Dictionary<C, List<int>> dict1 = new() { { new() { field = new() }, new() { 1, 2, 3 } } };
        Dictionary<C, List<int>> dict2 = new() { [new() { field = new() }] = new() { 1, 2, 3 } };
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void TestClassInitializer()
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
        public void TestFlowpass()
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
       C c = new().field;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,20): error CS0117: 'new(...)' does not contain a definition for 'field'
                //        C c = new().field;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "field").WithArguments("new(...)", "field").WithLocation(6, 20)
                );
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
            var soruce = @"
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
            var comp = CreateCompilation(soruce, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
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
        M(b => { if (b) return new(); else return new C(); });
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "CCCC");
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
        public void TestTypeParameter()
        {
            var source = @"
using System;

struct S {
    static void M1<T>() where T : struct
    {
        Console.Write(new T());
    }
    static void M2<T>() where T : new()
    {
        Console.Write(new T());
    }

    public static void Main() {
        M1<S>();
        M2<S>();
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "SS");
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
        public void TestInitializerErrors()
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
class Dog
{
    public Dog() {}
}
class Animal
{
    private Animal() {}
    public static implicit operator Animal(Dog dog) => throw null;
}

class Program
{
    public static void M(Animal a) => throw null;
    public static void Main()
    {
        M(new());
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (17,11): error CS0122: 'Animal.Animal()' is inaccessible due to its protection level
                //         M(new());
                Diagnostic(ErrorCode.ERR_BadAccess, "new()").WithArguments("Animal.Animal()").WithLocation(17, 11)
                );
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
        public void TestOverloadResolution1()
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
        public void TestOverloadResolution2()
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
        public void TestOverloadResolution3()
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
        public void TestOverloadResolution4()
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
        public void TestOverloadResolution5()
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
        public void TestOverloadResolution6()
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
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("C", "0").WithLocation(9, 19),
                // (9,26): error CS9367: The default constructor of the value type 'int' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         C c = new(new(), new());
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(9, 26)
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
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            validate(1, "N(1)", type: "C", convertedType: "C", symbol: "C C.N(System.Int32 i)", ConversionKind.Identity);
            validate(3, "N(2)", type: "C", convertedType: "C", symbol: "C C.N(System.Int32 i)", ConversionKind.Identity);

            void validate(int index, string expression, string type, string convertedType, string symbol, ConversionKind conversionKind)
            {
                var invocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(index);
                Assert.Equal(expression, invocation.ToString());
                Assert.Equal(type, model.GetTypeInfo(invocation).Type.ToTestDisplayString());
                Assert.Equal(convertedType, model.GetTypeInfo(invocation).ConvertedType.ToTestDisplayString());
                Assert.Equal(symbol, model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
                Assert.Equal(model.GetConversion(invocation).Kind, conversionKind);
            }
        }

        [Fact]
        public void TestAssignmnet()
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
        public void TestNullableType1()
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
        public void TestNullableType2()
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
        public void AsStatement()
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

            var comp = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (6,13): error CS0103: The name 'a' does not exist in the current context
                //         new(a) { x };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 13),
                // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         new() { x };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "new() { x }").WithLocation(7, 9)
                );
        }

        [Fact]
        public void TestCSharp7()
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
                // (6,15): error CS8107: Feature 'target-typed new' is not available in C# 7.0. Please use language version 8.0 or greater.
                //         C x = new();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "new").WithArguments("target-typed new", "8.0").WithLocation(6, 15)
                );
        }

        [Fact]
        public void AssignmentToClass()
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

            var def = nodes.OfType<ObjectCreationExpressionSyntax>().Single();
            Assert.Equal("C", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("C", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Equal("C..ctor()", model.GetSymbolInfo(def).Symbol.ToTestDisplayString());
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.True(model.GetConversion(def).IsIdentity);
        }

        [Fact]
        public void AssignmentToStruct()
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

            var def = nodes.OfType<ObjectCreationExpressionSyntax>().Single();
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

            var def = nodes.OfType<ObjectCreationExpressionSyntax>().Single();
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

            var def = nodes.OfType<ObjectCreationExpressionSyntax>().ElementAt(0);
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
                // (2,12): error CS9367: The default constructor of the value type 'byte' may not be used with target-typed 'new'. Consider using 'default' instead.
                // [Custom(z: new(), y: new(), x: new())]
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("byte").WithLocation(2, 12),
                // (2,22): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                // [Custom(z: new(), y: new(), x: new())]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("string", "0").WithLocation(2, 22),
                // (2,32): error CS9367: The default constructor of the value type 'int' may not be used with target-typed 'new'. Consider using 'default' instead.
                // [Custom(z: new(), y: new(), x: new())]
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(2, 32),
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

            var @new = nodes.OfType<ObjectCreationExpressionSyntax>().ElementAt(0);
            Assert.Equal("new()", @new.ToString());
            Assert.Null(model.GetTypeInfo(@new).Type);
            Assert.Null(model.GetTypeInfo(@new).ConvertedType); // Should get a type. Relates to https://github.com/dotnet/roslyn/issues/18609
            Assert.Null(model.GetSymbolInfo(@new).Symbol);
            Assert.False(model.GetConstantValue(@new).HasValue);

            var newObject = nodes.OfType<ObjectCreationExpressionSyntax>().ElementAt(1);
            Assert.Equal("new object()", newObject.ToString());
            Assert.Equal("System.Object", model.GetTypeInfo(newObject).Type?.ToTestDisplayString());
            Assert.Equal("System.Object", model.GetTypeInfo(newObject).ConvertedType?.ToTestDisplayString());
            Assert.Equal("System.Object..ctor()", model.GetSymbolInfo(newObject).Symbol?.ToTestDisplayString());
        }

        [Fact]
        public void InUsing()
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
                // (6,16): error CS1674: 'new(...)': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (new())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "new()").WithArguments("new(...)").WithLocation(6, 16),
                // (10,20): error CS0815: Cannot assign new(...) to an implicitly-typed variable
                //         using (var x = new())
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x = new()").WithArguments("new(...)").WithLocation(10, 20),
                // (14,39): error CS9366: The type 'IDisposable' may not be used as the target-type of 'new'.
                //         using (System.IDisposable x = new())
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "new()").WithArguments("System.IDisposable").WithLocation(14, 39)
                );
        }

        [Fact]
        public void CannotAwaitDefault()
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
                // (6,9): error CS4001: Cannot await 'new(...)'
                //         await new();
                Diagnostic(ErrorCode.ERR_BadAwaitArgIntrinsic, "await new()").WithArguments("new(...)").WithLocation(6, 9),
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

            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var def = nodes.OfType<ObjectCreationExpressionSyntax>().ElementAt(0);
            Assert.Equal("new()", def.ToString());
            Assert.Equal("T", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("T", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(def).Symbol);
            Assert.False(model.GetConstantValue(def).HasValue);
            Assert.True(model.GetConversion(def).IsIdentity);
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
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(6, 9),
                // (6,16): error CS8151: The return expression must be of type 'int' because this method returns by reference
                //         return new();
                Diagnostic(ErrorCode.ERR_RefReturnMustHaveIdentityConversion, "new()").WithArguments("int").WithLocation(6, 16),
                // (6,16): error CS9367: The default constructor of the value type 'int' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         return new();
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(6, 16)
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
                // (6,23): error CS0828: Cannot assign 'new(...)' to anonymous type property
                //         var x = new { Prop = new() };
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "Prop = new()").WithArguments("new(...)").WithLocation(6, 23)
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
                // (6,16): error CS8310: Operator '+' cannot be applied to operand 'new(...)'
                //         C v1 = +new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefault, "+new()").WithArguments("+", "new(...)").WithLocation(6, 16),
                // (7,16): error CS8310: Operator '-' cannot be applied to operand 'new(...)'
                //         C v2 = -new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefault, "-new()").WithArguments("-", "new(...)").WithLocation(7, 16),
                // (8,16): error CS8310: Operator '~' cannot be applied to operand 'new(...)'
                //         C v3 = ~new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefault, "~new()").WithArguments("~", "new(...)").WithLocation(8, 16),
                // (9,16): error CS8310: Operator '!' cannot be applied to operand 'new(...)'
                //         C v4 = !new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefault, "!new()").WithArguments("!", "new(...)").WithLocation(9, 16),
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
                // (6,15): error CS0117: 'new(...)' does not contain a definition for 'ToString'
                //         new().ToString();
                Diagnostic(ErrorCode.ERR_NoSuchMember, "ToString").WithArguments("new(...)", "ToString").WithLocation(6, 15),
                // (7,9): error CS0021: Cannot apply indexing with [] to an expression of type 'new(...)'
                //         new()[0].ToString();
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "new()[0]").WithArguments("new(...)").WithLocation(7, 9)
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
        throw new();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,15): error CS0155: The type caught or thrown must be derived from System.Exception
                //         throw new();
                Diagnostic(ErrorCode.ERR_BadExceptionType, "new()").WithLocation(6, 15)
                );
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
                // (7,23): error CS9367: The default constructor of the value type 'int' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         const int y = new();
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(7, 23)
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

            var def = nodes.OfType<ObjectCreationExpressionSyntax>().ElementAt(1);
            Assert.Equal("new()", def.ToString());
            Assert.Equal("C", model.GetTypeInfo(def).Type.ToTestDisplayString());
            Assert.Equal("C", model.GetTypeInfo(def).ConvertedType.ToTestDisplayString());
            Assert.Equal("C..ctor()", model.GetSymbolInfo(def).Symbol.ToTestDisplayString());
            Assert.False(model.GetConstantValue(def).HasValue);
        }

        [Fact]
        public void InSwitch()
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
                // (6,17): error CS8119: The switch expression must be a value; found 'new(...)'.
                //         switch (new())
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "new()").WithArguments("new(...)").WithLocation(6, 17),
                // (8,29): error CS9367: The default constructor of the value type 'bool' may not be used with target-typed 'new'. Consider using 'default' instead.
                //             case new() when new():
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("bool").WithLocation(8, 29),
                // (9,32): error CS9367: The default constructor of the value type 'bool' may not be used with target-typed 'new'. Consider using 'default' instead.
                //             case (new()) when (new()):
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("bool").WithLocation(9, 32),
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
                // (9,18): error CS0150: A constant value is expected
                //             case (new()):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(new())").WithLocation(9, 18),
                // (10,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 17)
                );
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
                // (9,21): error CS9367: The default constructor of the value type 'bool' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         catch when (new())
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("bool").WithLocation(9, 21)
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
                // (6,15): error CS0185: 'new(...)' is not a reference type as required by the lock statement
                //         lock (new())
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "new()").WithArguments("new(...)").WithLocation(6, 15)
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
                // (6,13): error CS0233: '?' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
                //         _ = sizeof(new());
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(").WithArguments("?").WithLocation(6, 13)
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
        int j = checked(new());
        C k = unchecked(new()); // ok
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,29): error CS0103: The name 'a' does not exist in the current context
                //         int i = checked(new(a));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 29),
                // (7,25): error CS9367: The default constructor of the value type 'int' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         int j = checked(new());
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(7, 25)
                );
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
            comp.VerifyDiagnostics(
                // (6,27): error CS9367: The default constructor of the value type 'TypedReference' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         var t = __reftype(new());
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("System.TypedReference").WithLocation(6, 27),
                // (7,29): error CS9367: The default constructor of the value type 'TypedReference' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         int rv = __refvalue(new(), int);
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("System.TypedReference").WithLocation(7, 29)
                );
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
                // (6,13): error CS9367: The default constructor of the value type 'bool' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         if (new())
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("bool").WithLocation(6, 13),
                // (11,16): error CS9367: The default constructor of the value type 'bool' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         while (new())
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("bool").WithLocation(11, 16),
                // (16,25): error CS9367: The default constructor of the value type 'bool' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         for (int i = 0; new(); i++)
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("bool").WithLocation(16, 25)
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
        if (new() == false)
        {
        }
        if (new() == true)
        {
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,13): error CS9367: The default constructor of the value type 'bool' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         if (new() == false)
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("bool").WithLocation(6, 13),
                // (9,13): error CS9367: The default constructor of the value type 'bool' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         if (new() == true)
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("bool").WithLocation(9, 13)
                );
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
                // (6,17): error CS0193: The * or -> operator must be applied to a pointer
                //         var p = *new();
                Diagnostic(ErrorCode.ERR_PtrExpected, "*new()").WithLocation(6, 17),
                // (7,17): error CS0193: The * or -> operator must be applied to a pointer
                //         var q = new()->F;
                Diagnostic(ErrorCode.ERR_PtrExpected, "new()->F").WithLocation(7, 17)
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
            comp.VerifyDiagnostics(
                // (6,28): error CS9367: The default constructor of the value type 'int' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         var t = new object[new()];
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(6, 28)
                );
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
            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe,
                        references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            comp.VerifyEmitDiagnostics();
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
            comp.VerifyEmitDiagnostics();
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
            comp.VerifyEmitDiagnostics(
                // (6,11): error CS9366: The type 'object[]' may not be used as the target-type of 'new(...)'
                //         M(new());
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "new()").WithArguments("object[]").WithLocation(6, 11)
                );
        }

        [Fact]
        public void ParamsAmbiguity1()
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
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(params object[])' and 'C.M(params int[])'
                //         M(new());
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
        M(new());
    }
    static void M(params object[] x) { }
    static void M(C x) { }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(params object[])' and 'C.M(C)'
                //         M(new());
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(params object[])", "C.M(C)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ParamsAmbiguity3()
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
            comp.VerifyEmitDiagnostics(
                // (8,14): error CS9366: The type 'object[]' may not be used as the target-type of 'new'.
                //         M(o, new());
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "new()").WithArguments("object[]").WithLocation(8, 14),
                // (10,14): error CS9366: The type 'C[]' may not be used as the target-type of 'new'.
                //         M(c, new());
                Diagnostic(ErrorCode.ERR_IllegalTargetType, "new()").WithArguments("C[]").WithLocation(10, 14)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var first = nodes.OfType<ObjectCreationExpressionSyntax>().ElementAt(1);
            Assert.Equal("(o, new())", first.Parent.Parent.ToString());
            Assert.Equal("System.Object[]", model.GetTypeInfo(first).Type.ToTestDisplayString());

            var second = nodes.OfType<ObjectCreationExpressionSyntax>().ElementAt(2);
            Assert.Equal("(new(), o)", second.Parent.Parent.ToString());
            Assert.Equal("System.Object", model.GetTypeInfo(second).Type.ToTestDisplayString());

            var third = nodes.OfType<ObjectCreationExpressionSyntax>().ElementAt(3);
            Assert.Equal("(c, new())", third.Parent.Parent.ToString());
            Assert.Equal("C[]", model.GetTypeInfo(third).Type.ToTestDisplayString());

            var fourth = nodes.OfType<ObjectCreationExpressionSyntax>().ElementAt(4);
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
            comp.VerifyEmitDiagnostics();
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
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void NewInEnum()
        {
            string source = @"
enum E : byte
{
    A = new(),
    B = new() + 1
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,9): error CS9367: The default constructor of the value type 'int' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //     B = new() + 1
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(5, 9),
                // (4,9): error CS9367: The default constructor of the value type 'byte' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //     A = new(),
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("byte").WithLocation(4, 9)
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
            comp.VerifyEmitDiagnostics();
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
                // (7,14): error CS9368: Cannot use a target-typed new as an argument to a dynamically dispatched operation.
                //         d.M2(new());
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgTypelessNew, "new()").WithLocation(7, 14)
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
                // (6,11): error CS0143: The type 'dynamic' has no constructors defined
                //         F(new());
                Diagnostic(ErrorCode.ERR_NoConstructors, "new()").WithArguments("dynamic").WithLocation(6, 11)
                );
        }

        [Fact]
        public void AmigBinaryOperator()
        {
            string source = @"
class C
{
    static void Main()
    {
        var x = new() == new();
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,17): error CS0034: Operator '==' is ambiguous on operands of type 'new(...)' and 'new(...)'
                //         var x = new() == new();
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "new() == new()").WithArguments("==", "new(...)", "new(...)").WithLocation(6, 17)
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
        foreach (int x in new()) { }
    }
}";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (6,27): error CS9369: Use of 'new(...)' is not valid in this context
                //         foreach (int x in new()) { }
                Diagnostic(ErrorCode.ERR_TypelessNewNotValid, "new()").WithLocation(6, 27)
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
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (6,33): error CS9369: Use of new(...) is not valid in this context
                //         var q = from x in new() select x;
                Diagnostic(ErrorCode.ERR_TypelessNewNotValid, "select x").WithLocation(6, 33),
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
                // (6,19): error CS0023: Operator 'is' cannot be applied to operand of type 'new(...)'
                //         bool v1 = new() is long;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "new() is long").WithArguments("is", "new(...)").WithLocation(6, 19),
                // (7,19): error CS0023: Operator 'is' cannot be applied to operand of type 'new(...)'
                //         bool v2 = new() is string;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "new() is string").WithArguments("is", "new(...)").WithLocation(7, 19),
                // (8,19): error CS0023: Operator 'is' cannot be applied to operand of type 'new(...)'
                //         bool v3 = new() is new();
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "new() is new()").WithArguments("is", "new(...)").WithLocation(8, 19),
                // (9,25): error CS9367: The default constructor of the value type 'bool' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         bool v4 = v1 is new();
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("bool").WithLocation(9, 25),
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

            var comp = CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (7,32): error CS8310: Operator '??' cannot be applied to operand 'new(...)'
                //         Func<object> f = () => new() ?? "hello";
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefault, @"new() ?? ""hello""").WithArguments("??", "new(...)").WithLocation(7, 32)
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
        public void BinaryOperator()
        {
            string source = @"
class C
{
    static void Main()
    {
        C x = new C();
        if (x == new())
        {
            System.Console.Write(""0"");
        }
        if (new() == x)
        {
            System.Console.Write(""1"");
        }
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (7,13): error CS0019: Operator '==' cannot be applied to operands of type 'C' and 'new(...)'
                //         if (x == new())
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == new()").WithArguments("==", "C", "new(...)").WithLocation(7, 13),
                // (11,13): error CS0019: Operator '==' cannot be applied to operands of type 'new(...)' and 'C'
                //         if (new() == x)
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "new() == x").WithArguments("==", "new(...)", "C").WithLocation(11, 13)
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
            comp.VerifyDiagnostics(
                // (6,25): error CS9367: The default constructor of the value type 'int' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         var a = new int[new()];
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(6, 25)
                );
        }

        [Fact]
        public void TernaryOperator1()
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
            comp.VerifyDiagnostics(
                // (7,24): error CS9367: The default constructor of the value type 'int' may not be used with 'new(...)'; Use 'default' or a literal expression instead
                //         var x = flag ? new() : 1;
                Diagnostic(ErrorCode.ERR_IllegalDefaultValueTypeCtor, "new()").WithArguments("int").WithLocation(7, 24)
                );
        }

        [Fact]
        public void TernaryOperator2()
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
        public void TernaryOperator3()
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
                // (7,30): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'new(...)' and 'new(...)'
                //         System.Console.Write(flag ? new() : new());
                Diagnostic(ErrorCode.ERR_InvalidQM, "flag ? new() : new()").WithArguments("new(...)", "new(...)").WithLocation(7, 30)
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
    }
}
