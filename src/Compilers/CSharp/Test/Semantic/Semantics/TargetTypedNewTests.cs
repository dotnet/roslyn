// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.TargetTypedNew)]
    public class TargetTypedNewTests : CSharpTestBase
    {
        [Fact]
        public void TestExpressionTree()
        {
            var comp = CreateCompilationWithMscorlib40AndSystemCore(@"
using System;
using System.Linq.Expressions;
class C {
    public static void Main()
    {
        Expression<Func<C>> a = () => new();
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (7,39): error CS9368: An expression tree lambda may not contain a target-typed new
                //         Expression<Func<C>> a = () => new();
                Diagnostic(ErrorCode.ERR_TargetTypedNewInExpressionTree, "new()").WithLocation(7, 39));
        }

        [Fact]
        public void TestLocal()
        {
            var comp = CreateCompilation(@"
using System;
class X {
    public static void Main()
    {
        object x = new();
        Console.Write(x);
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "System.Object");
        }

        [Fact]
        public void TestParameterDefaultValue()
        {
            var comp = CreateCompilation(@"
struct S {}
class C {
    void M(C c = new(), S s = new())
    {
    }
}
", options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (4,18): error CS1736: Default parameter value for 'c' must be a compile-time constant
                //     void M(C c = new(), S s = new())
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("c").WithLocation(4, 18),
                // (4,31): error CS9367: The default constructor of the value type 'S' may not be used with target-typed 'new'. Consider using 'default' instead.
                //     void M(C c = new(), S s = new())
                Diagnostic(ErrorCode.ERR_DefaultValueTypeCtorInTargetTypedNew, "new()").WithArguments("S").WithLocation(4, 31),
                // (4,31): error CS1736: Default parameter value for 's' must be a compile-time constant
                //     void M(C c = new(), S s = new())
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new()").WithArguments("s").WithLocation(4, 31)
                );
        }

        [Fact]
        public void TestParmas()
        {
            var comp = CreateCompilation(@"
using System;
struct S {}
class C {
    C(params int[] p) {
        foreach (var item in p)
            Console.Write(item);
    }

    public static void Main()
    {
        C c = new(1, 2, 3);
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics(
                 );
            CompileAndVerify(comp, expectedOutput: "123");
        }

        [Fact]
        public void TestNamedArgs()
        {
            var comp = CreateCompilation(@"
using System;
class C {
    class A {}
    class B {}
    static void M(A a) => Console.Write(""1"");
    static void M(B b) => Console.Write(""2"");

    public static void Main()
    {
        M(a: new());
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics(
                 );
            CompileAndVerify(comp, expectedOutput: "1");
        }

        [Fact]
        public void TestTrailingNamedArgs()
        {
            var comp = CreateCompilation(@"
using System;
class C {
    class A {}
    class B {}
    static void M(A a, int i) => Console.Write(""1"");
    static void M(B b, int i) => Console.Write(""2"");

    public static void Main()
    {
        M(a: new(), 1);
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics(
                 );
            CompileAndVerify(comp, expectedOutput: "1");
        }

        [Fact]
        public void TestArgs()
        {
            var comp = CreateCompilation(@"
using System;
class X {
    public int i;
    public X(int i) => this.i = i;
    public static void Main()
    {
        X x = new(42);
        Console.Write(x.i);
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void TestSafeCast()
        {
            var comp = CreateCompilation(@"
using System;
class X 
{
    public static void Main()
    {
        Console.Write(new() as X);
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (7,23): error CS9365: The first operand of an 'as' operator may not be a target-typed 'new'.
                //         Console.Write(new() as X);
                Diagnostic(ErrorCode.ERR_TypelessNewInAs, "new() as X").WithLocation(7, 23)
                );
        }

        [Fact]
        public void TestTupleElement()
        {
            var comp = CreateCompilation(@"
using System;
namespace System {
    public struct ValueTuple<T1,T2> {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 a, T2 b)
            => (Item1, Item2) = (a, b);
    }
    namespace Runtime.CompilerServices {
        public sealed class TupleElementNamesAttribute : Attribute {
            public TupleElementNamesAttribute(string[] names) {}
        }
    }
}

class X {
    static void M((X a, X b) t) => Console.Write($""{t.a}{t.b}"");

    public static void Main()
    {
        M((new(), new()));
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "XX");
        }

        [Fact]
        public void TestConst()
        {
            var comp = CreateCompilation(@"
using System;
class X {
    public static void Main()
    {
        const int x = new();
        Console.Write(x);
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,23): error CS9366: The default constructor of the value type 'int' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         const int x = new();
                Diagnostic(ErrorCode.ERR_DefaultValueTypeCtorInTargetTypedNew, "new()").WithArguments("int").WithLocation(6, 23)
                );
        }

        [Fact]
        public void TestBadTargetType_Assignment()
        {
            var comp = CreateCompilation(@"
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
    }
}
namespace System
{
    public struct ValueTuple<T1,T2> {}
}
", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (12,13): error CS0815: Cannot assign new() to an implicitly-typed variable
                //         var v0 = new();
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "v0 = new()").WithArguments("new()").WithLocation(12, 13),
                // (13,21): error CS9367: The default constructor of the value type 'Struct' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         Struct v1 = new();
                Diagnostic(ErrorCode.ERR_DefaultValueTypeCtorInTargetTypedNew, "new()").WithArguments("Struct").WithLocation(13, 21),
                // (14,21): error CS9366: The type 'Action' may not be used as the target-type of 'new'.
                //         Action v2 = new();
                Diagnostic(ErrorCode.ERR_BadTargetTypeForNew, "new()").WithArguments("System.Action").WithLocation(14, 21),
                // (15,9): error CS0723: Cannot declare a variable of static type 'Static'
                //         Static v3 = new();
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "Static").WithArguments("Static").WithLocation(15, 9),
                // (15,21): error CS1729: 'Static' does not contain a constructor that takes 0 arguments
                //         Static v3 = new();
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("Static", "0").WithLocation(15, 21),
                // (16,23): error CS0144: Cannot create an instance of the abstract class or interface 'Abstract'
                //         Abstract v4 = new();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new()").WithArguments("Abstract").WithLocation(16, 23),
                // (17,24): error CS9366: The type 'Interface' may not be used as the target-type of 'new'.
                //         Interface v5 = new();
                Diagnostic(ErrorCode.ERR_BadTargetTypeForNew, "new()").WithArguments("Interface").WithLocation(17, 24),
                // (18,26): error CS9366: The type 'Enumeration' may not be used as the target-type of 'new'.
                //         Enumeration v6 = new();
                Diagnostic(ErrorCode.ERR_BadTargetTypeForNew, "new()").WithArguments("Enumeration").WithLocation(18, 26),
                // (19,18): error CS9367: The default constructor of the value type 'int' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         int v7 = new();
                Diagnostic(ErrorCode.ERR_DefaultValueTypeCtorInTargetTypedNew, "new()").WithArguments("int").WithLocation(19, 18),
                // (20,19): error CS1919: Unsafe type 'int*' cannot be used in object creation
                //         int* v8 = new();
                Diagnostic(ErrorCode.ERR_UnsafeTypeInObjectCreation, "new()").WithArguments("int*").WithLocation(20, 19),
                // (21,19): error CS9367: The default constructor of the value type 'int?' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         int? v9 = new();
                Diagnostic(ErrorCode.ERR_DefaultValueTypeCtorInTargetTypedNew, "new()").WithArguments("int").WithLocation(21, 19),
                // (22,26): error CS8181: 'new' cannot be used with tuple type. Use a tuple literal expression instead.
                //         (int, int) v10 = new();
                Diagnostic(ErrorCode.ERR_NewWithTupleTypeSyntax, "new()").WithArguments("(int, int)").WithLocation(22, 26),
                // (23,23): error CS0143: The type 'dynamic' has no constructors defined
                //         dynamic v11 = new();
                Diagnostic(ErrorCode.ERR_NoConstructors, "new()").WithArguments("dynamic").WithLocation(23, 23),
                // (24,21): error CS9366: The type 'int[]' may not be used as the target-type of 'new'.
                //         int[] v12 = new();
                Diagnostic(ErrorCode.ERR_BadTargetTypeForNew, "new()").WithArguments("int[]").WithLocation(24, 21),
                // (25,9): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                //         Error v13 = new();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(25, 9),
                // (26,17): error CS9366: The type 'T' may not be used as the target-type of 'new'.
                //         T v14 = new();
                Diagnostic(ErrorCode.ERR_BadTargetTypeForNew, "new()").WithArguments("T").WithLocation(26, 17)
                );
        }

        [Fact]
        public void TestBadTargetType_Cast()
        {
            var comp = CreateCompilation(@"
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
    }
}
namespace System
{
    public struct ValueTuple<T1,T2> {}
}
", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (12,26): error CS9367: The default constructor of the value type 'Struct' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         var v1 = (Struct)new();
                Diagnostic(ErrorCode.ERR_DefaultValueTypeCtorInTargetTypedNew, "new()").WithArguments("Struct").WithLocation(12, 26),
                // (13,18): error CS9366: The type 'Action' may not be used as the target-type of 'new'.
                //         var v2 = (Action)new();
                Diagnostic(ErrorCode.ERR_BadTargetTypeForNew, "(Action)new()").WithArguments("System.Action").WithLocation(13, 18),
                // (14,18): error CS0716: Cannot convert to static type 'Static'
                //         var v3 = (Static)new();
                Diagnostic(ErrorCode.ERR_ConvertToStaticClass, "(Static)new()").WithArguments("Static").WithLocation(14, 18),
                // (14,9): error CS0723: Cannot declare a variable of static type 'Static'
                //         var v3 = (Static)new();
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "var").WithArguments("Static").WithLocation(14, 9),
                // (15,28): error CS0144: Cannot create an instance of the abstract class or interface 'Abstract'
                //         var v4 = (Abstract)new();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new()").WithArguments("Abstract").WithLocation(15, 28),
                // (16,18): error CS9366: The type 'Interface' may not be used as the target-type of 'new'.
                //         var v5 = (Interface)new();
                Diagnostic(ErrorCode.ERR_BadTargetTypeForNew, "(Interface)new()").WithArguments("Interface").WithLocation(16, 18),
                // (17,18): error CS9366: The type 'Enumeration' may not be used as the target-type of 'new'.
                //         var v6 = (Enumeration)new();
                Diagnostic(ErrorCode.ERR_BadTargetTypeForNew, "(Enumeration)new()").WithArguments("Enumeration").WithLocation(17, 18),
                // (18,23): error CS9367: The default constructor of the value type 'int' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         var v7 = (int)new();
                Diagnostic(ErrorCode.ERR_DefaultValueTypeCtorInTargetTypedNew, "new()").WithArguments("int").WithLocation(18, 23),
                // (19,18): error CS1919: Unsafe type 'int*' cannot be used in object creation
                //         var v8 = (int*)new();
                Diagnostic(ErrorCode.ERR_UnsafeTypeInObjectCreation, "(int*)new()").WithArguments("int*").WithLocation(19, 18),
                // (20,24): error CS9367: The default constructor of the value type 'int?' may not be used with target-typed 'new'. Consider using 'default' instead.
                //         var v9 = (int?)new();
                Diagnostic(ErrorCode.ERR_DefaultValueTypeCtorInTargetTypedNew, "new()").WithArguments("int").WithLocation(20, 24),
                // (21,19): error CS8181: 'new' cannot be used with tuple type. Use a tuple literal expression instead.
                //         var v10 = ((int,int))new();
                Diagnostic(ErrorCode.ERR_NewWithTupleTypeSyntax, "((int,int))new()").WithArguments("(int, int)").WithLocation(21, 19),
                // (22,19): error CS0143: The type 'dynamic' has no constructors defined
                //         var v11 = (dynamic)new();
                Diagnostic(ErrorCode.ERR_NoConstructors, "(dynamic)new()").WithArguments("dynamic").WithLocation(22, 19),
                // (23,19): error CS9366: The type 'int[]' may not be used as the target-type of 'new'.
                //         var v12 = (int[])new();
                Diagnostic(ErrorCode.ERR_BadTargetTypeForNew, "(int[])new()").WithArguments("int[]").WithLocation(23, 19),
                // (24,20): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                //         var v13 = (Error)new();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(24, 20),
                // (25,19): error CS9366: The type 'T' may not be used as the target-type of 'new'.
                //         var v14 = (T)new();
                Diagnostic(ErrorCode.ERR_BadTargetTypeForNew, "(T)new()").WithArguments("T").WithLocation(25, 19)
                );
        }

        [Fact]
        public void TestMethodInvocation()
        {
            var comp = CreateCompilation(@"
using System;
class X {
    
    static void M(object a, X b) => Console.Write($""{a} {b}"");

    public static void Main()
    {
        M(new(), new());
    }
}
", options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "System.Object X");
        }

        [Fact]
        public void TestAmbigCall()
        {
            var comp = CreateCompilation(@"
using System;
class X {
    
    static void M(object a, X b) => Console.Write($""{a} {b}"");
    static void M(X a, object b) => Console.Write($""{a} {b}"");

    public static void Main()
    {
        M(new(), new());
    }
}
").VerifyDiagnostics(
                // (10,9): error CS0121: The call is ambiguous between the following methods or properties: 'X.M(object, X)' and 'X.M(X, object)'
                //         M(new(), new());
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("X.M(object, X)", "X.M(X, object)").WithLocation(10, 9)
                );
        }

        [Fact]
        public void TestObjectInitializer()
        {
            var comp = CreateCompilation(@"
using System;
class X {
    
    public int field;

    public static void Main()
    {
        X x = new() { field = 42 };
        Console.Write(x.field);
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void TestObjectInitializer2()
        {
            var comp = CreateCompilation(@"
using System;
class C {
    
    class A { public int field; } 
    class B { }

    static void M(int a, A b) => Console.Write(b.field);
    static void M(double a, B b) {}

    public static void Main()
    {
        M(1.0, new() { field = 42 });
    }
}
").VerifyDiagnostics(
                // (13,24): error CS0117: 'C.B' does not contain a definition for 'field'
                //         M(1.0, new() { field = 42 });
                Diagnostic(ErrorCode.ERR_NoSuchMember, "field").WithArguments("C.B", "field").WithLocation(13, 24),
                // (5,26): warning CS0649: Field 'C.A.field' is never assigned to, and will always have its default value 0
                //     class A { public int field; } 
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C.A.field", "0").WithLocation(5, 26)
                );
        }

        [Fact]
        public void TestObjectInitializer3()
        {
            var comp = CreateCompilation(@"
using System;
class C {
    
    class A { public int field; } 
    class B { }

    public C(int i) {}

    static void M(int a, A b) => Console.Write(b.field);
    static void M(double a, B b) {}

    public static void Main()
    {
        M(1, new() { field = 42 });
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void TestFlowpass()
        {
            var comp = CreateCompilation(@"
using System;
class X {
    
    public int field;

    public override string ToString() => field.ToString();

    public static void Main()
    {
        int i;
        X x = new() { field = (i = 42) };
        Console.Write(i);
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void TestField()
        {
            var comp = CreateCompilation(@"
using System;
class X {
    object field = new();
    public static void Main() {
        Console.Write(new X().field);
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "System.Object");
        }

        [Fact]
        public void TestDotOff()
        {
            var comp = CreateCompilation(@"
using System;
class X {
    public static void Main() {
        Console.Write(new().field);
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,29): error CS0117: 'new()' does not contain a definition for 'field'
                //         Console.Write(new().field);
                Diagnostic(ErrorCode.ERR_NoSuchMember, "field").WithArguments("new()", "field").WithLocation(5, 29)
                );
        }

        [Fact]
        public void TestInaccessibleConstructor()
        {
            var comp = CreateCompilation(@"
class C { private C() {} }

class X {
    public static void Main() {
        C x = new();
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,15): error CS0122: 'C.C()' is inaccessible due to its protection level
                //         C x = new();
                Diagnostic(ErrorCode.ERR_BadAccess, "new()").WithArguments("C.C()").WithLocation(6, 15)
                );
        }

        [Fact]
        public void TestBadArgs()
        {
            var comp = CreateCompilation(@"
class C { private C() {} }

class X {
    public static void Main() {
        C x = new(1);
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,15): error CS1729: 'C' does not contain a constructor that takes 1 arguments
                //         C x = new(1);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new(1)").WithArguments("C", "1").WithLocation(6, 15)
                );
        }

        [Fact]
        public void TestNested()
        {
            var comp = CreateCompilation(@"
using System;
class C { 
    public C(C a, C b) => Console.Write(3); 
    public C(int i) => Console.Write(i);
}

class X {
    public static void Main() {
        C x = new(new(1), new(2));
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "123");
        }

        [Fact]
        public void TestBestType_TypeArg()
        {
            var comp = CreateCompilation(@"
using System;

class X {
    public static void M<T>(T t1, T t2) {
        Console.Write($""{t1}{t2}"");
    }

    public static void Main() {
        M(new X(), new());
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "XX");
        }

        [Fact]
        public void TestBestType_ArrayInit()
        {
            var comp = CreateCompilation(@"
using System;

class X {
    public static void Main() {
        var arr = new[] {new X(), new()};
        Console.Write($""{arr[0]}{arr[1]}"");
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "XX");
        }

        [Fact]
        public void TestBestType_NullCoalescing()
        {
            var comp = CreateCompilation(@"
using System;

class X {
    public static void Main() {
        X x = null;
        Console.Write((x ?? new()));
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "X");
        }

        [Fact]
        public void TestBestType_Lambda()
        {
            var comp = CreateCompilation(@"
using System;

class X {
    public static void M<T>(Func<bool, T> f) {
        Console.Write(f(true));
        Console.Write(f(false));
    }
    public static void Main() {
        M(b => { if (b) return new X(); else return new(); });
        M(b => { if (b) return new(); else return new X(); });
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "XXXX");
        }

        [Fact]
        public void TestBadTypeParameter()
        {
            var comp = CreateCompilation(@"
using System;

struct S {
    static void M1<T>() where T : struct
    {
        Console.Write((T)new(0));
    }
    static void M2<T>() where T : new()
    {
        Console.Write((T)new(0));
    }
}
", options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (7,26): error CS0417: 'T': cannot provide arguments when creating an instance of a variable type
                //         Console.Write((T)new(0));
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "new(0)").WithArguments("T").WithLocation(7, 26),
                // (11,26): error CS0417: 'T': cannot provide arguments when creating an instance of a variable type
                //         Console.Write((T)new(0));
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "new(0)").WithArguments("T").WithLocation(11, 26)
                );
        }

        [Fact]
        public void TestTypeParameter()
        {
            var comp = CreateCompilation(@"
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
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "SS");
        }

        [Fact]
        public void TestTypeParameterInitializer()
        {
            var comp = CreateCompilation(@"
using System;

class C {
    public int field;
    static void M1<T>() where T : C, new()
    {
        Console.Write(((T)new(){field = 42}).field);
    }

    public static void Main() {
        M1<C>();
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void TestInitializers()
        {
            var comp = CreateCompilation(@"
using System;
using System.Collections.Generic;
class X
{
    static Dictionary<Dictionary<int, int>, List<int>> v1 = new()
    {
        { new() { [1] = 2 }, new() { 3, 4 } }
    };

    static void Print(Dictionary<int, int> v)
    {
        foreach (var pair in v)
            Console.Write($""{pair.Key}{pair.Value}"");
    }

    static void Print(List<int> v)
    {
        foreach (var item in v)
            Console.Write(item);
    }

    public static void Main()
    {
        foreach (var pair in v1)
        {
            Print(pair.Key);
            Print(pair.Value);
        }
    }
}
", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "1234");
        }

        [Fact]
        public void TestImplicitConversion()
        {
            var comp = CreateCompilation(@"
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
", options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (17,11): error CS0122: 'Animal.Animal()' is inaccessible due to its protection level
                //         M(new());
                Diagnostic(ErrorCode.ERR_BadAccess, "new()").WithArguments("Animal.Animal()").WithLocation(17, 11)
                );
        }

        [Fact]
        public void TestOverloadResolution()
        {
            var comp = CreateCompilation(@"
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
", options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (18,11): error CS1729: 'D' does not contain a constructor that takes 1 arguments
                //         M(new(1), 1);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new(1)").WithArguments("D", "1").WithLocation(18, 11)
                );
        }

        [Fact]
        public void TestNullableType1()
        {
            var comp = CreateCompilation(@"
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
", options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "43");
        }

        [Fact]
        public void TestNullableType2()
        {
            var comp = CreateCompilation(@"
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
", options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "S");
        }
    }
}
