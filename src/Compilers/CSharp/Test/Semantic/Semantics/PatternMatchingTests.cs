// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Threading;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTests : CSharpTestBase
    {
        [Fact]
        public void DemoModes()
        {
            var source =
@"
public class Vec
{
    public static void Main()
    {
        object o = ""Pass"";
        int i1 = 0b001010; // binary literals
        int i2 = 23_554; // digit separators
        // local functions
        // Note: due to complexity and cost of parsing local functions we
        // don't try to parse if the feature isn't enabled
        int f() => 2;
        ref int i3 = ref i1; // ref locals
        string s = o is string k ? k : null; // pattern matching
        //let var i4 = 3; // let
        //int i5 = o match (case * : 7); // match
        //object q = (o is null) ? o : throw null; // throw expressions
        //if (q is Vec(3)) {} // recursive pattern
    }
    public int X => 4;
    public Vec(int x) {}
}
";
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (7,18): error CS8059: Feature 'binary literals' is not available in C# 6.  Please use language version 7 or greater.
                //         int i1 = 0b001010; // binary literals
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "").WithArguments("binary literals", "7").WithLocation(7, 18),
                // (8,18): error CS8059: Feature 'digit separators' is not available in C# 6.  Please use language version 7 or greater.
                //         int i2 = 23_554; // digit separators
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "").WithArguments("digit separators", "7").WithLocation(8, 18),
                // (12,9): error CS8059: Feature 'local functions' is not available in C# 6.  Please use language version 7 or greater.
                //         int f() => 2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "int f() => 2;").WithArguments("local functions", "7").WithLocation(12, 9),
                // (13,9): error CS8059: Feature 'byref locals and returns' is not available in C# 6.  Please use language version 7 or greater.
                //         ref int i3 = ref i1; // ref locals
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7").WithLocation(13, 9),
                // (13,22): error CS8059: Feature 'byref locals and returns' is not available in C# 6.  Please use language version 7 or greater.
                //         ref int i3 = ref i1; // ref locals
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7").WithLocation(13, 22),
                // (14,20): error CS8059: Feature 'pattern matching' is not available in C# 6.  Please use language version 7 or greater.
                //         string s = o is string k ? k : null; // pattern matching
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "o is string k").WithArguments("pattern matching", "7").WithLocation(14, 20),
                // (12,13): warning CS0168: The variable 'f' is declared but never used
                //         int f() => 2;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "f").WithArguments("f").WithLocation(12, 13)
                );

            // enables binary literals, digit separators, local functions, ref locals, pattern matching
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'i2' is assigned but its value is never used
                //         int i2 = 23_554; // digit separators
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i2").WithArguments("i2").WithLocation(8, 13),
                // (12,13): warning CS0168: The variable 'f' is declared but never used
                //         int f() => 2;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "f").WithArguments("f").WithLocation(12, 13)
                );
        }

        [Fact]
        public void SimplePatternTest()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        var s = nameof(Main);
        if (s is string t) Console.WriteLine(""1. {0}"", t);
        s = null;
        Console.WriteLine(""2. {0}"", s is string t ? t : nameof(X));
        int? x = 12;
        if (x is var y) Console.WriteLine(""3. {0}"", y);
        if (x is int y) Console.WriteLine(""4. {0}"", y);
        x = null;
        if (x is var y) Console.WriteLine(""5. {0}"", y);
        if (x is int y) Console.WriteLine(""6. {0}"", y);
        Console.WriteLine(""7. {0}"", (x is bool is bool));
    }
}";
            var expectedOutput =
@"1. Main
2. X
3. 12
4. 12
5. 
7. True";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // warning CS0184: The given expression is never of the provided ('bool') type
                //         Console.WriteLine("7. {0}", (x is bool is bool));
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "x is bool").WithArguments("bool"),
                // warning CS0183: The given expression is always of the provided ('bool') type
                //         Console.WriteLine("7. {0}", (x is bool is bool));
                Diagnostic(ErrorCode.WRN_IsAlwaysTrue, "x is bool is bool").WithArguments("bool")
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void NullablePatternTest()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        T(null);
        T(1);
    }
    public static void T(object x)
    {
        if (x is Nullable<int> y) Console.WriteLine($""expression {x} is Nullable<int> y"");
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (11,18): error CS8105: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
    //         if (x is Nullable<int> y) Console.WriteLine($"expression {x} is Nullable<int> y");
    Diagnostic(ErrorCode.ERR_PatternNullableType, "Nullable<int>").WithArguments("int?", "int").WithLocation(11, 18)
                );
        }

        [Fact]
        public void UnconstrainedPatternTest()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        Test<string>(1);
        Test<int>(""foo"");
        Test<int>(1);
        Test<int>(1.2);
        Test<double>(1.2);
        Test<int?>(1);
        Test<int?>(null);
        Test<string>(null);
    }
    public static void Test<T>(object x)
    {
        if (x is T y)
            Console.WriteLine($""expression {x} is {typeof(T).Name} {y}"");
        else
            Console.WriteLine($""expression {x} is not {typeof(T).Name}"");
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            using (new EnsureEnglishCulture())
            {
                var expectedOutput =
@"expression 1 is not String
expression foo is not Int32
expression 1 is Int32 1
expression 1.2 is not Int32
expression 1.2 is Double 1.2
expression 1 is Nullable`1 1
expression  is not Nullable`1
expression  is not String";
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact, WorkItem(10932, "https://github.com/dotnet/roslyn/issues/10932")]
        public void PatternErrors()
        {
            var source =
@"using System;
using NullableInt = System.Nullable<int>;
public class X
{
    public static void Main()
    {
        var s = nameof(Main);
        byte b = 1;
        if (s is string t) { } else Console.WriteLine(t); // t not in scope
        if (null is dynamic t) { } // null not allowed
        if (s is NullableInt x) { } // error: cannot use nullable type
        if (s is long l) { } // error: cannot convert string to long
        if (b is 1000) { } // error: cannot convert 1000 to byte
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (9,55): error CS0103: The name 't' does not exist in the current context
                //         if (s is string t) { } else Console.WriteLine(t); // t not in scope
                Diagnostic(ErrorCode.ERR_NameNotInContext, "t").WithArguments("t").WithLocation(9, 55),
                // (10,13): error CS8117: Invalid operand for pattern match.
                //         if (null is dynamic t) { } // null not allowed
                Diagnostic(ErrorCode.ERR_BadIsPatternExpression, "null").WithLocation(10, 13),
                // (11,18): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
                //         if (s is NullableInt x) { } // error: cannot use nullable type
                Diagnostic(ErrorCode.ERR_PatternNullableType, "NullableInt").WithArguments("int?", "int").WithLocation(11, 18),
                // (12,18): error CS8121: An expression of type string cannot be handled by a pattern of type long.
                //         if (s is long l) { } // error: cannot convert string to long
                Diagnostic(ErrorCode.ERR_PatternWrongType, "long").WithArguments("string", "long").WithLocation(12, 18),
                // (13,18): error CS0031: Constant value '1000' cannot be converted to a 'byte'
                //         if (b is 1000) { } // error: cannot convert 1000 to byte
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "1000").WithArguments("1000", "byte").WithLocation(13, 18)
                );
        }

        [Fact]
        public void PatternInCtorInitializer()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        new D(1);
        new D(10);
        new D(1.2);
    }
}
class D
{
    public D(object o) : this(o is int x && x >= 5) {}
    public D(bool b) { Console.WriteLine(b); }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var expectedOutput =
@"False
True
False";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void PatternInCatchFilter()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        M(1);
        M(10);
        M(1.2);
    }
    private static void M(object o)
    {
        try
        {
            throw new Exception();
        }
        catch (Exception) when (o is int x && x >= 5)
        {
            Console.WriteLine($""Yes for {o}"");
        }
        catch (Exception)
        {
            Console.WriteLine($""No for {o}"");
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            using (new EnsureEnglishCulture())
            {
                var expectedOutput =
@"No for 1
Yes for 10
No for 1.2";
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact]
        public void PatternInFieldInitializer()
        {
            var source =
@"using System;
public class X
{
    static object o1 = 1;
    static object o2 = 10;
    static object o3 = 1.2;
    static bool b1 = M(o1, (o1 is int x && x >= 5)),
                b2 = M(o2, (o2 is int x && x >= 5)),
                b3 = M(o3, (o3 is int x && x >= 5));
    public static void Main()
    {
    }
    private static bool M(object o, bool result)
    {
        Console.WriteLine($""{result} for {o}"");
        return result;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            using (new EnsureEnglishCulture())
            {
                var expectedOutput =
@"False for 1
True for 10
False for 1.2";
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact]
        public void PatternInExpressionBodiedMethod()
        {
            var source =
@"using System;
public class X
{
    static object o1 = 1;
    static object o2 = 10;
    static object o3 = 1.2;
    static bool B1() => M(o1, (o1 is int x && x >= 5));
    static bool B2 => M(o2, (o2 is int x && x >= 5));
    static bool B3 => M(o3, (o3 is int x && x >= 5));
    public static void Main()
    {
        var r = B1() | B2 | B3;
    }
    private static bool M(object o, bool result)
    {
        Console.WriteLine($""{result} for {o}"");
        return result;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            using (new EnsureEnglishCulture())
            {
                var expectedOutput =
@"False for 1
True for 10
False for 1.2";
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact, WorkItem(8778, "https://github.com/dotnet/roslyn/issues/8778")]
        public void PatternInExpressionBodiedLocalFunction()
        {
            var source =
@"using System;
public class X
{
    static object o1 = 1;
    static object o2 = 10;
    static object o3 = 1.2;
    public static void Main()
    {
        bool B1() => M(o1, (o1 is int x && x >= 5));
        bool B2() => M(o2, (o2 is int x && x >= 5));
        bool B3() => M(o3, (o3 is int x && x >= 5));
        var r = B1() | B2() | B3();
    }
    private static bool M(object o, bool result)
    {
        Console.WriteLine($""{result} for {o}"");
        return result;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            using (new EnsureEnglishCulture())
            {
                var expectedOutput =
@"False for 1
True for 10
False for 1.2";
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact, WorkItem(8778, "https://github.com/dotnet/roslyn/issues/8778")]
        public void PatternInExpressionBodiedLambda()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        object o1 = 1;
        object o2 = 10;
        object o3 = 1.2;
        Func<object, bool> B1 = o => M(o, (o is int x && x >= 5));
        B1(o1);
        Func<bool> B2 = () => M(o2, (o2 is int x && x >= 5));
        B2();
        Func<bool> B3 = () => M(o3, (o3 is int x && x >= 5));
        B3();
    }
    private static bool M(object o, bool result)
    {
        Console.WriteLine($""{result} for {o}"");
        return result;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            using (new EnsureEnglishCulture())
            {
                var expectedOutput =
@"False for 1
True for 10
False for 1.2";
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact]
        public void PatternInBadPlaces()
        {
            var source =
@"using System;
[Obsolete("""" is string s ? s : """")]
public class X
{
    public static void Main()
    {
    }
    private static void M(string p = """" is object o ? o.ToString() : """")
    {
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (2,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    // [Obsolete("" is string s ? s : "")]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, @""""" is string s ? s : """"").WithLocation(2, 11),
    // (8,38): error CS1736: Default parameter value for 'p' must be a compile-time constant
    //     private static void M(string p = "" is object o ? o.ToString() : "")
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @""""" is object o ? o.ToString() : """"").WithArguments("p").WithLocation(8, 38)
                );
        }

        [Fact]
        public void PatternInSwitchAndForeach()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        object o1 = 1;
        object o2 = 10;
        object o3 = 1.2;
        object oa = new object[] { 1, 10, 1.2 };
        foreach (var o in oa is object[] z ? z : new object[0])
        {
            switch (o is int x && x >= 5)
            {
                case true:
                    M(o, true);
                    break;
                case false:
                    M(o, false);
                    break;
                default:
                    throw null;
            }
        }
    }
    private static bool M(object o, bool result)
    {
        Console.WriteLine($""{result} for {o}"");
        return result;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            using (new EnsureEnglishCulture())
            {
                var expectedOutput =
@"False for 1
True for 10
False for 1.2";
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact]
        public void GeneralizedSwitchStatement()
        {
            Uri u = new Uri("http://www.microsoft.com");
            var source =
@"using System;
public struct X
{
    public static void Main()
    {
        var oa = new object[] { 1, 10, 20L, 1.2, ""foo"", true, null, new X(), new Exception(""boo"") };
        foreach (var o in oa)
        {
            switch (o)
            {
                default:
                    Console.WriteLine($""class {o.GetType().Name} {o}"");
                    break;
                case 1:
                    Console.WriteLine(""one"");
                    break;
                case int i:
                    Console.WriteLine($""int {i}"");
                    break;
                case long i:
                    Console.WriteLine($""long {i}"");
                    break;
                case double d:
                    Console.WriteLine($""double {d}"");
                    break;
                case null:
                    Console.WriteLine($""null"");
                    break;
                case ValueType z:
                    Console.WriteLine($""struct {z.GetType().Name} {z}"");
                    break;
            }
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            using (new EnsureEnglishCulture())
            {
                var expectedOutput =
@"one
int 10
long 20
double 1.2
class String foo
struct Boolean True
null
struct X X
class Exception System.Exception: boo
";
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact]
        public void PatternVariableDefiniteAssignment()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        object o = new X();
        if (o is X x1) Console.WriteLine(x1); // OK
        if (!(o is X x2)) Console.WriteLine(x2); // error
        if (o is X x3 || true) Console.WriteLine(x3); // error
        switch (o)
        {
            case X x4:
            default:
                Console.WriteLine(x4); // error
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (8,45): error CS0165: Use of unassigned local variable 'x2'
                //         if (!(o is X x2)) Console.WriteLine(x2);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(8, 45),
                // (9,50): error CS0165: Use of unassigned local variable 'x3'
                //         if (o is X x3 || true) Console.WriteLine(x3);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x3").WithArguments("x3").WithLocation(9, 50),
                // (14,35): error CS0165: Use of unassigned local variable 'x4'
                //                 Console.WriteLine(x4); // error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x4").WithArguments("x4").WithLocation(14, 35)
                );
        }

        [Fact]
        public void PatternVariablesAreMutable()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        if (12 is var x) {
            x = x + 1;
            x++;
            M1(ref x);
            M2(out x);
        }
    }
    public static void M1(ref int x) {}
    public static void M2(out int x) { x = 1; }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void ScopeOfPatternVariables_ExpressionStatement_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        Dummy(true is var x1, x1);
        {
            Dummy(true is var x1, x1);
        }
        Dummy(true is var x1, x1);
    }

    void Test2()
    {
        Dummy(x2, true is var x2);
    }

    void Test3(int x3)
    {
        Dummy(true is var x3, x3);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);
        Dummy(true is var x4, x4);
    }

    void Test5()
    {
        Dummy(true is var x5, x5);
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    Dummy(true is var x6, x6);
    //}

    //void Test7()
    //{
    //    Dummy(true is var x7, x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8()
    {
        Dummy(true is var x8, x8, false is var x8, x8);
    }

    void Test9(bool y9)
    {
        if (y9)
            Dummy(true is var x9, x9);
    }

    System.Action Test10(bool y10)
    {
        return () =>
                {
                    if (y10)
                        Dummy(true is var x10, x10);
                };
    }

    void Test11()
    {
        Dummy(x11);
        Dummy(true is var x11, x11);
    }

    void Test12()
    {
        Dummy(true is var x12, x12);
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (21,15): error CS0841: Cannot use local variable 'x2' before it is declared
    //         Dummy(x2, true is var x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 15),
    // (26,27): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy(true is var x3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 27),
    // (33,27): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy(true is var x4);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(33, 27),
    // (38,27): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy(true is var x5);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(38, 27),
    // (59,48): error CS0128: A local variable named 'x8' is already defined in this scope
    //         Dummy(true is var x8, x8, false is var x8, x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 48),
    // (79,15): error CS0103: The name 'x11' does not exist in the current context
    //         Dummy(x11);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(79, 15),
    // (86,15): error CS0103: The name 'x12' does not exist in the current context
    //         Dummy(x12);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(86, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x1Decl[i], x1Ref[i]);
            }

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1]);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref[0]);
            VerifyNotAPatternLocal(model, x5Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").ToArray();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            for (int i = 0; i < x8Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").Single();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").Single();
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref);

            var x10Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x10").Single();
            var x10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[0]);
            VerifyNotInScope(model, x12Ref[1]);
        }

        [Fact, WorkItem(9258, "https://github.com/dotnet/roslyn/issues/9258")]
        public void PatternVariableOrder()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    static void Dummy(params object[] x) {}

    void Test1(object o1, object o2)
    {
        Dummy(o1 is int i && i < 10,
              o2 is int @i && @i > 10);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
                // (13,25): error CS0128: A local variable named 'i' is already defined in this scope
                //               o2 is int @i && @i > 10);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "@i").WithArguments("i").WithLocation(13, 25),
                // (13,31): error CS0165: Use of unassigned local variable 'i'
                //               o2 is int @i && @i > 10);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "@i").WithArguments("i").WithLocation(13, 31)
                );
        }

        private static void VerifyModelForDeclarationPattern(SemanticModel model, DeclarationPatternSyntax decl, params IdentifierNameSyntax[] references)
        {
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)decl));
            Assert.Same(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));

            var type = ((LocalSymbol)symbol).Type;
            if (!decl.Type.IsVar || !type.IsErrorType())
            {
                Assert.Equal(type, model.GetSymbolInfo(decl.Type).Symbol);
            }

            foreach (var reference in references)
            {
                Assert.Same(symbol, model.GetSymbolInfo(reference).Symbol);
                Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: decl.Identifier.ValueText).Single());
                Assert.True(model.LookupNames(reference.SpanStart).Contains(decl.Identifier.ValueText));
            }
        }

        private static void VerifyModelForDeclarationPatternDuplicateInSameScope(SemanticModel model, DeclarationPatternSyntax decl)
        {
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)decl));
            Assert.NotEqual(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));

            var type = ((LocalSymbol)symbol).Type;
            if (!decl.Type.IsVar || !type.IsErrorType())
            {
                Assert.Equal(type, model.GetSymbolInfo(decl.Type).Symbol);
            }
        }

        private static void VerifyNotAPatternLocal(SemanticModel model, IdentifierNameSyntax reference)
        {
            var symbol = model.GetSymbolInfo(reference).Symbol;

            if (symbol.Kind == SymbolKind.Local)
            {
                Assert.NotEqual(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            }

            Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: reference.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(reference.SpanStart).Contains(reference.Identifier.ValueText));
        }

        private static void VerifyNotInScope(SemanticModel model, IdentifierNameSyntax reference)
        {
            Assert.Null(model.GetSymbolInfo(reference).Symbol);
            Assert.False(model.LookupSymbols(reference.SpanStart, name: reference.Identifier.ValueText).Any());
            Assert.False(model.LookupNames(reference.SpanStart).Contains(reference.Identifier.ValueText));
        }

        [Fact]
        public void ScopeOfPatternVariables_ReturnStatement_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) { return null; }

    object Test1()
    {
        return Dummy(true is var x1, x1);
        {
            return Dummy(true is var x1, x1);
        }
        return Dummy(true is var x1, x1);
    }

    object Test2()
    {
        return Dummy(x2, true is var x2);
    }

    object Test3(int x3)
    {
        return Dummy(true is var x3, x3);
    }

    object Test4()
    {
        var x4 = 11;
        Dummy(x4);
        return Dummy(true is var x4, x4);
    }

    object Test5()
    {
        return Dummy(true is var x5, x5);
        var x5 = 11;
        Dummy(x5);
    }

    //object Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    return Dummy(true is var x6, x6);
    //}

    //object Test7()
    //{
    //    return Dummy(true is var x7, x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    object Test8()
    {
        return Dummy(true is var x8, x8, false is var x8, x8);
    }

    object Test9(bool y9)
    {
        if (y9)
            return Dummy(true is var x9, x9);
        return null;
    }
    System.Func<object> Test10(bool y10)
    {
        return () =>
                {
                    if (y10)
                        return Dummy(true is var x10, x10);
                    return null;};
    }

    object Test11()
    {
        Dummy(x11);
        return Dummy(true is var x11, x11);
    }

    object Test12()
    {
        return Dummy(true is var x12, x12);
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (14,13): warning CS0162: Unreachable code detected
    //             return Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(14, 13),
    // (21,22): error CS0841: Cannot use local variable 'x2' before it is declared
    //         return Dummy(x2, true is var x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 22),
    // (26,34): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         return Dummy(true is var x3, x3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 34),
    // (33,34): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         return Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(33, 34),
    // (38,34): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         return Dummy(true is var x5, x5);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(38, 34),
    // (39,9): warning CS0162: Unreachable code detected
    //         var x5 = 11;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "var").WithLocation(39, 9),
    // (59,55): error CS0128: A local variable named 'x8' is already defined in this scope
    //         return Dummy(true is var x8, x8, false is var x8, x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 55),
    // (79,15): error CS0103: The name 'x11' does not exist in the current context
    //         Dummy(x11);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(79, 15),
    // (86,15): error CS0103: The name 'x12' does not exist in the current context
    //         Dummy(x12);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(86, 15),
    // (86,9): warning CS0162: Unreachable code detected
    //         Dummy(x12);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "Dummy").WithLocation(86, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x1Decl[i], x1Ref[i]);
            }

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1]);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref[0]);
            VerifyNotAPatternLocal(model, x5Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").ToArray();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").Single();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").Single();
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref);

            var x10Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x10").Single();
            var x10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[0]);
            VerifyNotInScope(model, x12Ref[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_ThrowStatement_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.Exception Dummy(params object[] x) { return null;}

    void Test1()
    {
        throw Dummy(true is var x1, x1);
        {
            throw Dummy(true is var x1, x1);
        }
        throw Dummy(true is var x1, x1);
    }

    void Test2()
    {
        throw Dummy(x2, true is var x2);
    }

    void Test3(int x3)
    {
        throw Dummy(true is var x3, x3);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);
        throw Dummy(true is var x4, x4);
    }

    void Test5()
    {
        throw Dummy(true is var x5, x5);
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    throw Dummy(true is var x6, x6);
    //}

    //void Test7()
    //{
    //    throw Dummy(true is var x7, x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8()
    {
        throw Dummy(true is var x8, x8, false is var x8, x8);
    }

    void Test9(bool y9)
    {
        if (y9)
            throw Dummy(true is var x9, x9);
    }

    System.Action Test10(bool y10)
    {
        return () =>
                {
                    if (y10)
                        throw Dummy(true is var x10, x10);
                };
    }

    void Test11()
    {
        Dummy(x11);
        throw Dummy(true is var x11, x11);
    }

    void Test12()
    {
        throw Dummy(true is var x12, x12);
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (21,21): error CS0841: Cannot use local variable 'x2' before it is declared
    //         throw Dummy(x2, true is var x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 21),
    // (26,33): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         throw Dummy(true is var x3, x3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 33),
    // (33,33): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         throw Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(33, 33),
    // (38,33): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         throw Dummy(true is var x5, x5);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(38, 33),
    // (39,9): warning CS0162: Unreachable code detected
    //         var x5 = 11;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "var").WithLocation(39, 9),
    // (59,54): error CS0128: A local variable named 'x8' is already defined in this scope
    //         throw Dummy(true is var x8, x8, false is var x8, x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 54),
    // (79,15): error CS0103: The name 'x11' does not exist in the current context
    //         Dummy(x11);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(79, 15),
    // (86,15): error CS0103: The name 'x12' does not exist in the current context
    //         Dummy(x12);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(86, 15),
    // (86,9): warning CS0162: Unreachable code detected
    //         Dummy(x12);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "Dummy").WithLocation(86, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x1Decl[i], x1Ref[i]);
            }

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1]);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref[0]);
            VerifyNotAPatternLocal(model, x5Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").ToArray();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").Single();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").Single();
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref);

            var x10Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x10").Single();
            var x10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[0]);
            VerifyNotInScope(model, x12Ref[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_If_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        if (true is var x1)
        {
            Dummy(x1);
        }
        else
        {
            System.Console.WriteLine(x1);
        }
    }

    void Test2()
    {
        if (true is var x2)
            Dummy(x2);
        else
            System.Console.WriteLine(x2);
    }

    void Test3()
    {
        if (true is var x3)
            Dummy(x3);
        else
        {
            var x3 = 12;
            System.Console.WriteLine(x3);
        }
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        if (true is var x4)
            Dummy(x4);
    }

    void Test5(int x5)
    {
        if (true is var x5)
            Dummy(x5);
    }

    void Test6()
    {
        if (x6 && true is var x6)
            Dummy(x6);
    }

    void Test7()
    {
        if (true is var x7 && x7)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        if (true is var x8)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        if (true is var x9)
        {   
            Dummy(x9);
            if (true is var x9) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        if (y10 is var x10)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }










    void Test12()
    {
        if (y12 is var x12)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    if (y13 is var x13)
    //        let y13 = 12;
    //}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (110,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(110, 13),
    // (18,38): error CS0103: The name 'x1' does not exist in the current context
    //             System.Console.WriteLine(x1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(18, 38),
    // (27,38): error CS0103: The name 'x2' does not exist in the current context
    //             System.Console.WriteLine(x2);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(27, 38),
    // (46,25): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         if (true is var x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(46, 25),
    // (52,25): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         if (true is var x5)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(52, 25),
    // (58,13): error CS0841: Cannot use local variable 'x6' before it is declared
    //         if (x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(58, 13),
    // (59,19): error CS0165: Use of unassigned local variable 'x6'
    //             Dummy(x6);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x6").WithArguments("x6").WithLocation(59, 19),
    // (66,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(66, 17),
    // (76,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(76, 34),
    // (84,29): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             if (true is var x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(84, 29),
    // (91,13): error CS0103: The name 'y10' does not exist in the current context
    //         if (y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(91, 13),
    // (109,13): error CS0103: The name 'y12' does not exist in the current context
    //         if (y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(109, 13)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref[0]);
            VerifyNotInScope(model, x1Ref[1]);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref[0]);
            VerifyNotInScope(model, x2Ref[1]);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref[0]);
            VerifyNotAPatternLocal(model, x3Ref[1]);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1]);
            VerifyNotAPatternLocal(model, x4Ref[0]);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0]);
            VerifyNotInScope(model, x8Ref[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(2, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[1]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_Lambda_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    System.Action<object> Test1()
    {
        return (o) => let x1 = o;
    }

    System.Action<object> Test2()
    {
        return (o) => let var x2 = o;
    }

    void Test3()
    {
        Dummy((System.Func<object, bool>) (o => o is int x3 && x3 > 0));
    }

    void Test4()
    {
        Dummy((System.Func<object, bool>) (o => x4 && o is int x4));
    }

    void Test5()
    {
        Dummy((System.Func<object, object, bool>) ((o1, o2) => o1 is int x5 && 
                                                               o2 is int x5 && 
                                                               x5 > 0));
    }

    void Test6()
    {
        Dummy((System.Func<object, bool>) (o => o is int x6 && x6 > 0), (System.Func<object, bool>) (o => o is int x6 && x6 > 0));
    }

    void Test7()
    {
        Dummy(x7, 1);
        Dummy(x7, 
             (System.Func<object, bool>) (o => o is int x7 && x7 > 0), 
              x7);
        Dummy(x7, 2); 
    }

    void Test8()
    {
        Dummy(true is var x8 && x8, (System.Func<object, bool>) (o => o is int y8 && x8));
    }

    void Test9()
    {
        Dummy(true is var x9, 
              (System.Func<object, bool>) (o => o is int x9 && 
                                                x9 > 0), x9);
    }

    void Test10()
    {
        Dummy((System.Func<object, bool>) (o => o is int x10 && 
                                                x10 > 0),
              true is var x10, x10);
    }

    void Test11()
    {
        var x11 = 11;
        Dummy(x11);
        Dummy((System.Func<object, bool>) (o => o is int x11 && 
                                                x11 > 0), x11);
    }

    void Test12()
    {
        Dummy((System.Func<object, bool>) (o => o is int x12 && 
                                                x12 > 0), 
              x12);
        var x12 = 11;
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (12,27): error CS1002: ; expected
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x1").WithLocation(12, 27),
    // (17,27): error CS1002: ; expected
    //         return (o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(17, 27),
    // (12,23): error CS0103: The name 'let' does not exist in the current context
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(12, 23),
    // (12,23): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(12, 23),
    // (12,27): error CS0103: The name 'x1' does not exist in the current context
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(12, 27),
    // (12,32): error CS0103: The name 'o' does not exist in the current context
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(12, 32),
    // (12,27): warning CS0162: Unreachable code detected
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "x1").WithLocation(12, 27),
    // (17,23): error CS0103: The name 'let' does not exist in the current context
    //         return (o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(17, 23),
    // (17,23): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         return (o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(17, 23),
    // (17,36): error CS0103: The name 'o' does not exist in the current context
    //         return (o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(17, 36),
    // (17,27): warning CS0162: Unreachable code detected
    //         return (o) => let var x2 = o;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "var").WithLocation(17, 27),
    // (27,49): error CS0841: Cannot use local variable 'x4' before it is declared
    //         Dummy((System.Func<object, bool>) (o => x4 && o is int x4));
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(27, 49),
    // (33,74): error CS0128: A local variable named 'x5' is already defined in this scope
    //                                                                o2 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(33, 74),
    // (34,64): error CS0165: Use of unassigned local variable 'x5'
    //                                                                x5 > 0));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x5").WithArguments("x5").WithLocation(34, 64),
    // (44,15): error CS0103: The name 'x7' does not exist in the current context
    //         Dummy(x7, 1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(44, 15),
    // (45,15): error CS0103: The name 'x7' does not exist in the current context
    //         Dummy(x7, 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(45, 15),
    // (47,15): error CS0103: The name 'x7' does not exist in the current context
    //               x7);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(47, 15),
    // (48,15): error CS0103: The name 'x7' does not exist in the current context
    //         Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(48, 15),
    // (59,58): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //               (System.Func<object, bool>) (o => o is int x9 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(59, 58),
    // (65,58): error CS0136: A local or parameter named 'x10' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy((System.Func<object, bool>) (o => o is int x10 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x10").WithArguments("x10").WithLocation(65, 58),
    // (74,58): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy((System.Func<object, bool>) (o => o is int x11 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(74, 58),
    // (80,58): error CS0136: A local or parameter named 'x12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy((System.Func<object, bool>) (o => o is int x12 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x12").WithArguments("x12").WithLocation(80, 58),
    // (82,15): error CS0841: Cannot use local variable 'x12' before it is declared
    //               x12);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(82, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(5, x7Ref.Length);
            VerifyNotInScope(model, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[2]);
            VerifyNotInScope(model, x7Ref[3]);
            VerifyNotInScope(model, x7Ref[4]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(2, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[0]);

            var x10Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x10").ToArray();
            var x10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x10").ToArray();
            Assert.Equal(2, x10Decl.Length);
            Assert.Equal(2, x10Ref.Length);
            VerifyModelForDeclarationPattern(model, x10Decl[0], x10Ref[0]);
            VerifyModelForDeclarationPattern(model, x10Decl[1], x10Ref[1]);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(3, x11Ref.Length);
            VerifyNotAPatternLocal(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);
            VerifyNotAPatternLocal(model, x11Ref[2]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(3, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[0]);
            VerifyNotAPatternLocal(model, x12Ref[1]);
            VerifyNotAPatternLocal(model, x12Ref[2]);
        }

        [Fact]
        public void Lambda_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1());
    }

    static bool Test1()
    {
        System.Func<bool> l = () => 1 is int x1 && Dummy(x1); 
        return l();
    }

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void ScopeOfPatternVariables_Query_01()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from x in new[] { 1 is var y1 ? y1 : 0, y1}
                  select x + y1;

        Dummy(y1); 
    }

    void Test2()
    {
        var res = from x1 in new[] { 1 is var y2 ? y2 : 0}
                  from x2 in new[] { x1 is var z2 ? z2 : 0, z2, y2}
                  select x1 + x2 + y2 + 
                         z2;

        Dummy(z2); 
    }

    void Test3()
    {
        var res = from x1 in new[] { 1 is var y3 ? y3 : 0}
                  let x2 = x1 is var z3 && z3 > 0 && y3 < 0 
                  select new { x1, x2, y3,
                               z3};

        Dummy(z3); 
    }

    void Test4()
    {
        var res = from x1 in new[] { 1 is var y4 ? y4 : 0}
                  join x2 in new[] { 2 is var z4 ? z4 : 0, z4, y4}
                            on x1 + y4 + z4 + 3 is var u4 ? u4 : 0 + 
                                  v4 
                               equals x2 + y4 + z4 + 4 is var v4 ? v4 : 0 +
                                  u4 
                  select new { x1, x2, y4, z4, 
                               u4, v4 };

        Dummy(z4); 
        Dummy(u4); 
        Dummy(v4); 
    }

    void Test5()
    {
        var res = from x1 in new[] { 1 is var y5 ? y5 : 0}
                  join x2 in new[] { 2 is var z5 ? z5 : 0, z5, y5}
                            on x1 + y5 + z5 + 3 is var u5 ? u5 : 0 + 
                                  v5 
                               equals x2 + y5 + z5 + 4 is var v5 ? v5 : 0 +
                                  u5 
                  into g
                  select new { x1, y5, z5, g,
                               u5, v5 };

        Dummy(z5); 
        Dummy(u5); 
        Dummy(v5); 
    }

    void Test6()
    {
        var res = from x in new[] { 1 is var y6 ? y6 : 0}
                  where x > y6 && 1 is var z6 && z6 == 1
                  select x + y6 +
                         z6;

        Dummy(z6); 
    }

    void Test7()
    {
        var res = from x in new[] { 1 is var y7 ? y7 : 0}
                  orderby x > y7 && 1 is var z7 && z7 == 
                          u7,
                          x > y7 && 1 is var u7 && u7 == 
                          z7   
                  select x + y7 +
                         z7 + u7;

        Dummy(z7); 
        Dummy(u7); 
    }

    void Test8()
    {
        var res = from x in new[] { 1 is var y8 ? y8 : 0}
                  select x > y8 && 1 is var z8 && z8 == 1;

        Dummy(z8); 
    }

    void Test9()
    {
        var res = from x in new[] { 1 is var y9 ? y9 : 0}
                  group x > y9 && 1 is var z9 && z9 == 
                        u9
                  by
                        x > y9 && 1 is var u9 && u9 == 
                        z9;   

        Dummy(z9); 
        Dummy(u9); 
    }

    void Test10()
    {
        var res = from x1 in new[] { 1 is var y10 ? y10 : 0}
                  from y10 in new[] { 1 }
                  select x1 + y10;
    }

    void Test11()
    {
        var res = from x1 in new[] { 1 is var y11 ? y11 : 0}
                  let y11 = x1 + 1
                  select x1 + y11;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (17,15): error CS0103: The name 'y1' does not exist in the current context
    //         Dummy(y1); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y1").WithArguments("y1").WithLocation(17, 15),
    // (25,26): error CS0103: The name 'z2' does not exist in the current context
    //                          z2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(25, 26),
    // (27,15): error CS0103: The name 'z2' does not exist in the current context
    //         Dummy(z2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(27, 15),
    // (35,32): error CS0103: The name 'z3' does not exist in the current context
    //                                z3};
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(35, 32),
    // (37,15): error CS0103: The name 'z3' does not exist in the current context
    //         Dummy(z3); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(37, 15),
    // (45,35): error CS0103: The name 'v4' does not exist in the current context
    //                                   v4 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(45, 35),
    // (47,35): error CS1938: The name 'u4' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
    //                                   u4 
    Diagnostic(ErrorCode.ERR_QueryInnerKey, "u4").WithArguments("u4").WithLocation(47, 35),
    // (49,32): error CS0103: The name 'u4' does not exist in the current context
    //                                u4, v4 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(49, 32),
    // (49,36): error CS0103: The name 'v4' does not exist in the current context
    //                                u4, v4 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(49, 36),
    // (51,15): error CS0103: The name 'z4' does not exist in the current context
    //         Dummy(z4); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z4").WithArguments("z4").WithLocation(51, 15),
    // (52,15): error CS0103: The name 'u4' does not exist in the current context
    //         Dummy(u4); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(52, 15),
    // (53,15): error CS0103: The name 'v4' does not exist in the current context
    //         Dummy(v4); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(53, 15),
    // (61,35): error CS0103: The name 'v5' does not exist in the current context
    //                                   v5 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(61, 35),
    // (63,35): error CS1938: The name 'u5' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
    //                                   u5 
    Diagnostic(ErrorCode.ERR_QueryInnerKey, "u5").WithArguments("u5").WithLocation(63, 35),
    // (66,32): error CS0103: The name 'u5' does not exist in the current context
    //                                u5, v5 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(66, 32),
    // (66,36): error CS0103: The name 'v5' does not exist in the current context
    //                                u5, v5 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(66, 36),
    // (68,15): error CS0103: The name 'z5' does not exist in the current context
    //         Dummy(z5); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z5").WithArguments("z5").WithLocation(68, 15),
    // (69,15): error CS0103: The name 'u5' does not exist in the current context
    //         Dummy(u5); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(69, 15),
    // (70,15): error CS0103: The name 'v5' does not exist in the current context
    //         Dummy(v5); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(70, 15),
    // (78,26): error CS0103: The name 'z6' does not exist in the current context
    //                          z6;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(78, 26),
    // (80,15): error CS0103: The name 'z6' does not exist in the current context
    //         Dummy(z6); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(80, 15),
    // (87,27): error CS0103: The name 'u7' does not exist in the current context
    //                           u7,
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(87, 27),
    // (89,27): error CS0103: The name 'z7' does not exist in the current context
    //                           z7   
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(89, 27),
    // (91,31): error CS0103: The name 'u7' does not exist in the current context
    //                          z7 + u7;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(91, 31),
    // (91,26): error CS0103: The name 'z7' does not exist in the current context
    //                          z7 + u7;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(91, 26),
    // (93,15): error CS0103: The name 'z7' does not exist in the current context
    //         Dummy(z7); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(93, 15),
    // (94,15): error CS0103: The name 'u7' does not exist in the current context
    //         Dummy(u7); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(94, 15),
    // (88,52): error CS0165: Use of unassigned local variable 'u7'
    //                           x > y7 && 1 is var u7 && u7 == 
    Diagnostic(ErrorCode.ERR_UseDefViolation, "u7").WithArguments("u7").WithLocation(88, 52),
    // (102,15): error CS0103: The name 'z8' does not exist in the current context
    //         Dummy(z8); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z8").WithArguments("z8").WithLocation(102, 15),
    // (112,25): error CS0103: The name 'z9' does not exist in the current context
    //                         z9;   
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z9").WithArguments("z9").WithLocation(112, 25),
    // (109,25): error CS0103: The name 'u9' does not exist in the current context
    //                         u9
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u9").WithArguments("u9").WithLocation(109, 25),
    // (114,15): error CS0103: The name 'z9' does not exist in the current context
    //         Dummy(z9); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z9").WithArguments("z9").WithLocation(114, 15),
    // (115,15): error CS0103: The name 'u9' does not exist in the current context
    //         Dummy(u9); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u9").WithArguments("u9").WithLocation(115, 15),
    // (108,50): error CS0165: Use of unassigned local variable 'z9'
    //                   group x > y9 && 1 is var z9 && z9 == 
    Diagnostic(ErrorCode.ERR_UseDefViolation, "z9").WithArguments("z9").WithLocation(108, 50),
    // (121,24): error CS1931: The range variable 'y10' conflicts with a previous declaration of 'y10'
    //                   from y10 in new[] { 1 }
    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y10").WithArguments("y10").WithLocation(121, 24),
    // (128,23): error CS1931: The range variable 'y11' conflicts with a previous declaration of 'y11'
    //                   let y11 = x1 + 1
    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y11").WithArguments("y11").WithLocation(128, 23)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var y1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y1").Single();
            var y1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y1").ToArray();
            Assert.Equal(4, y1Ref.Length);
            VerifyModelForDeclarationPattern(model, y1Decl, y1Ref[0], y1Ref[1], y1Ref[2]);
            VerifyNotInScope(model, y1Ref[3]);

            var y2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y2").Single();
            var y2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y2").ToArray();
            Assert.Equal(3, y2Ref.Length);
            VerifyModelForDeclarationPattern(model, y2Decl, y2Ref);

            var z2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z2").Single();
            var z2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z2").ToArray();
            Assert.Equal(4, z2Ref.Length);
            VerifyModelForDeclarationPattern(model, z2Decl, z2Ref[0], z2Ref[1]);
            VerifyNotInScope(model, z2Ref[2]);
            VerifyNotInScope(model, z2Ref[3]);

            var y3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y3").Single();
            var y3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y3").ToArray();
            Assert.Equal(3, y3Ref.Length);
            VerifyModelForDeclarationPattern(model, y3Decl, y3Ref);

            var z3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z3").Single();
            var z3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z3").ToArray();
            Assert.Equal(3, z3Ref.Length);
            VerifyModelForDeclarationPattern(model, z3Decl, z3Ref[0]);
            VerifyNotInScope(model, z3Ref[1]);
            VerifyNotInScope(model, z3Ref[2]);

            var y4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y4").Single();
            var y4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y4").ToArray();
            Assert.Equal(5, y4Ref.Length);
            VerifyModelForDeclarationPattern(model, y4Decl, y4Ref);

            var z4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z4").Single();
            var z4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z4").ToArray();
            Assert.Equal(6, z4Ref.Length);
            VerifyModelForDeclarationPattern(model, z4Decl, z4Ref[0], z4Ref[1], z4Ref[2], z4Ref[3], z4Ref[4]);
            VerifyNotInScope(model, z4Ref[5]);

            var u4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "u4").Single();
            var u4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "u4").ToArray();
            Assert.Equal(4, u4Ref.Length);
            VerifyModelForDeclarationPattern(model, u4Decl, u4Ref[0]);
            VerifyNotInScope(model, u4Ref[1]);
            VerifyNotInScope(model, u4Ref[2]);
            VerifyNotInScope(model, u4Ref[3]);

            var v4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "v4").Single();
            var v4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "v4").ToArray();
            Assert.Equal(4, v4Ref.Length);
            VerifyNotInScope(model, v4Ref[0]);
            VerifyModelForDeclarationPattern(model, v4Decl, v4Ref[1]);
            VerifyNotInScope(model, v4Ref[2]);
            VerifyNotInScope(model, v4Ref[3]);

            var y5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y5").Single();
            var y5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y5").ToArray();
            Assert.Equal(5, y5Ref.Length);
            VerifyModelForDeclarationPattern(model, y5Decl, y5Ref);

            var z5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z5").Single();
            var z5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z5").ToArray();
            Assert.Equal(6, z5Ref.Length);
            VerifyModelForDeclarationPattern(model, z5Decl, z5Ref[0], z5Ref[1], z5Ref[2], z5Ref[3], z5Ref[4]);
            VerifyNotInScope(model, z5Ref[5]);

            var u5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "u5").Single();
            var u5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "u5").ToArray();
            Assert.Equal(4, u5Ref.Length);
            VerifyModelForDeclarationPattern(model, u5Decl, u5Ref[0]);
            VerifyNotInScope(model, u5Ref[1]);
            VerifyNotInScope(model, u5Ref[2]);
            VerifyNotInScope(model, u5Ref[3]);

            var v5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "v5").Single();
            var v5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "v5").ToArray();
            Assert.Equal(4, v5Ref.Length);
            VerifyNotInScope(model, v5Ref[0]);
            VerifyModelForDeclarationPattern(model, v5Decl, v5Ref[1]);
            VerifyNotInScope(model, v5Ref[2]);
            VerifyNotInScope(model, v5Ref[3]);

            var y6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y6").Single();
            var y6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y6").ToArray();
            Assert.Equal(3, y6Ref.Length);
            VerifyModelForDeclarationPattern(model, y6Decl, y6Ref);

            var z6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z6").Single();
            var z6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z6").ToArray();
            Assert.Equal(3, z6Ref.Length);
            VerifyModelForDeclarationPattern(model, z6Decl, z6Ref[0]);
            VerifyNotInScope(model, z6Ref[1]);
            VerifyNotInScope(model, z6Ref[2]);

            var y7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y7").Single();
            var y7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y7").ToArray();
            Assert.Equal(4, y7Ref.Length);
            VerifyModelForDeclarationPattern(model, y7Decl, y7Ref);

            var z7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z7").Single();
            var z7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z7").ToArray();
            Assert.Equal(4, z7Ref.Length);
            VerifyModelForDeclarationPattern(model, z7Decl, z7Ref[0]);
            VerifyNotInScope(model, z7Ref[1]);
            VerifyNotInScope(model, z7Ref[2]);
            VerifyNotInScope(model, z7Ref[3]);

            var u7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "u7").Single();
            var u7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "u7").ToArray();
            Assert.Equal(4, u7Ref.Length);
            VerifyNotInScope(model, u7Ref[0]);
            VerifyModelForDeclarationPattern(model, u7Decl, u7Ref[1]);
            VerifyNotInScope(model, u7Ref[2]);
            VerifyNotInScope(model, u7Ref[3]);

            var y8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y8").Single();
            var y8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y8").ToArray();
            Assert.Equal(2, y8Ref.Length);
            VerifyModelForDeclarationPattern(model, y8Decl, y8Ref);

            var z8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z8").Single();
            var z8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z8").ToArray();
            Assert.Equal(2, z8Ref.Length);
            VerifyModelForDeclarationPattern(model, z8Decl, z8Ref[0]);
            VerifyNotInScope(model, z8Ref[1]);

            var y9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y9").Single();
            var y9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y9").ToArray();
            Assert.Equal(3, y9Ref.Length);
            VerifyModelForDeclarationPattern(model, y9Decl, y9Ref);

            var z9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z9").Single();
            var z9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z9").ToArray();
            Assert.Equal(3, z9Ref.Length);
            VerifyModelForDeclarationPattern(model, z9Decl, z9Ref[0]);
            VerifyNotInScope(model, z9Ref[1]);
            VerifyNotInScope(model, z9Ref[2]);

            var u9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "u9").Single();
            var u9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "u9").ToArray();
            Assert.Equal(3, u9Ref.Length);
            VerifyNotInScope(model, u9Ref[0]);
            VerifyModelForDeclarationPattern(model, u9Decl, u9Ref[1]);
            VerifyNotInScope(model, u9Ref[2]);

            var y10Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y10").Single();
            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyModelForDeclarationPattern(model, y10Decl, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y11").Single();
            var y11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y11").ToArray();
            Assert.Equal(2, y11Ref.Length);
            VerifyModelForDeclarationPattern(model, y11Decl, y11Ref[0]);
            VerifyNotAPatternLocal(model, y11Ref[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Query_03()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test4()
    {
        var res = from x1 in new[] { 1 is var y4 ? y4 : 0}
                  select x1 into x1
                  join x2 in new[] { 2 is var z4 ? z4 : 0, z4, y4}
                            on x1 + y4 + z4 + 3 is var u4 ? u4 : 0 + 
                                  v4 
                               equals x2 + y4 + z4 + 4 is var v4 ? v4 : 0 +
                                  u4 
                  select new { x1, x2, y4, z4, 
                               u4, v4 };

        Dummy(z4); 
        Dummy(u4); 
        Dummy(v4); 
    }

    void Test5()
    {
        var res = from x1 in new[] { 1 is var y5 ? y5 : 0}
                  select x1 into x1
                  join x2 in new[] { 2 is var z5 ? z5 : 0, z5, y5}
                            on x1 + y5 + z5 + 3 is var u5 ? u5 : 0 + 
                                  v5 
                               equals x2 + y5 + z5 + 4 is var v5 ? v5 : 0 +
                                  u5 
                  into g
                  select new { x1, y5, z5, g,
                               u5, v5 };

        Dummy(z5); 
        Dummy(u5); 
        Dummy(v5); 
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (18,35): error CS0103: The name 'v4' does not exist in the current context
    //                                   v4 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(18, 35),
    // (20,35): error CS1938: The name 'u4' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
    //                                   u4 
    Diagnostic(ErrorCode.ERR_QueryInnerKey, "u4").WithArguments("u4").WithLocation(20, 35),
    // (22,32): error CS0103: The name 'u4' does not exist in the current context
    //                                u4, v4 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(22, 32),
    // (22,36): error CS0103: The name 'v4' does not exist in the current context
    //                                u4, v4 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(22, 36),
    // (24,15): error CS0103: The name 'z4' does not exist in the current context
    //         Dummy(z4); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z4").WithArguments("z4").WithLocation(24, 15),
    // (25,15): error CS0103: The name 'u4' does not exist in the current context
    //         Dummy(u4); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(25, 15),
    // (26,15): error CS0103: The name 'v4' does not exist in the current context
    //         Dummy(v4); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(26, 15),
    // (35,35): error CS0103: The name 'v5' does not exist in the current context
    //                                   v5 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(35, 35),
    // (37,35): error CS1938: The name 'u5' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
    //                                   u5 
    Diagnostic(ErrorCode.ERR_QueryInnerKey, "u5").WithArguments("u5").WithLocation(37, 35),
    // (40,32): error CS0103: The name 'u5' does not exist in the current context
    //                                u5, v5 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(40, 32),
    // (40,36): error CS0103: The name 'v5' does not exist in the current context
    //                                u5, v5 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(40, 36),
    // (42,15): error CS0103: The name 'z5' does not exist in the current context
    //         Dummy(z5); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z5").WithArguments("z5").WithLocation(42, 15),
    // (43,15): error CS0103: The name 'u5' does not exist in the current context
    //         Dummy(u5); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(43, 15),
    // (44,15): error CS0103: The name 'v5' does not exist in the current context
    //         Dummy(v5); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(44, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var y4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y4").Single();
            var y4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y4").ToArray();
            Assert.Equal(5, y4Ref.Length);
            VerifyModelForDeclarationPattern(model, y4Decl, y4Ref);

            var z4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z4").Single();
            var z4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z4").ToArray();
            Assert.Equal(6, z4Ref.Length);
            VerifyModelForDeclarationPattern(model, z4Decl, z4Ref[0], z4Ref[1], z4Ref[2], z4Ref[3], z4Ref[4]);
            VerifyNotInScope(model, z4Ref[5]);

            var u4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "u4").Single();
            var u4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "u4").ToArray();
            Assert.Equal(4, u4Ref.Length);
            VerifyModelForDeclarationPattern(model, u4Decl, u4Ref[0]);
            VerifyNotInScope(model, u4Ref[1]);
            VerifyNotInScope(model, u4Ref[2]);
            VerifyNotInScope(model, u4Ref[3]);

            var v4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "v4").Single();
            var v4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "v4").ToArray();
            Assert.Equal(4, v4Ref.Length);
            VerifyNotInScope(model, v4Ref[0]);
            VerifyModelForDeclarationPattern(model, v4Decl, v4Ref[1]);
            VerifyNotInScope(model, v4Ref[2]);
            VerifyNotInScope(model, v4Ref[3]);

            var y5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y5").Single();
            var y5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y5").ToArray();
            Assert.Equal(5, y5Ref.Length);
            VerifyModelForDeclarationPattern(model, y5Decl, y5Ref);

            var z5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z5").Single();
            var z5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z5").ToArray();
            Assert.Equal(6, z5Ref.Length);
            VerifyModelForDeclarationPattern(model, z5Decl, z5Ref[0], z5Ref[1], z5Ref[2], z5Ref[3], z5Ref[4]);
            VerifyNotInScope(model, z5Ref[5]);

            var u5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "u5").Single();
            var u5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "u5").ToArray();
            Assert.Equal(4, u5Ref.Length);
            VerifyModelForDeclarationPattern(model, u5Decl, u5Ref[0]);
            VerifyNotInScope(model, u5Ref[1]);
            VerifyNotInScope(model, u5Ref[2]);
            VerifyNotInScope(model, u5Ref[3]);

            var v5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "v5").Single();
            var v5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "v5").ToArray();
            Assert.Equal(4, v5Ref.Length);
            VerifyNotInScope(model, v5Ref[0]);
            VerifyModelForDeclarationPattern(model, v5Decl, v5Ref[1]);
            VerifyNotInScope(model, v5Ref[2]);
            VerifyNotInScope(model, v5Ref[3]);
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void ScopeOfPatternVariables_Query_05()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        int y1 = 0, y2 = 0, y3 = 0, y4 = 0, y5 = 0, y6 = 0, y7 = 0, y8 = 0, y9 = 0, y10 = 0, y11 = 0, y12 = 0;

        var res = from x1 in new[] { 1 is var y1 ? y1 : 0}
                  from x2 in new[] { 2 is var y2 ? y2 : 0}
                  join x3 in new[] { 3 is var y3 ? y3 : 0}
                       on 4 is var y4 ? y4 : 0
                          equals 5 is var y5 ? y5 : 0
                  where 6 is var y6 && y6 == 1
                  orderby 7 is var y7 && y7 > 0, 
                          8 is var y8 && y8 > 0 
                  group 9 is var y9 && y9 > 0 
                  by 10 is var y10 && y10 > 0
                  into g
                  let x11 = 11 is var y11 && y11 > 0
                  select 12 is var y12 && y12 > 0
                  into s
                  select y1 + y2 + y3 + y4 + y5 + y6 + y7 + y8 + y9 + y10 + y11 + y12;

        Dummy(y1, y2, y3, y4, y5, y6, y7, y8, y9, y10, y11, y12); 
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (16,47): error CS0136: A local or parameter named 'y1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         var res = from x1 in new[] { 1 is var y1 ? y1 : 0}
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y1").WithArguments("y1").WithLocation(16, 47),
                // (17,47): error CS0136: A local or parameter named 'y2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   from x2 in new[] { 2 is var y2 ? y2 : 0}
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y2").WithArguments("y2").WithLocation(17, 47),
                // (18,47): error CS0136: A local or parameter named 'y3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   join x3 in new[] { 3 is var y3 ? y3 : 0}
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y3").WithArguments("y3").WithLocation(18, 47),
                // (19,36): error CS0136: A local or parameter named 'y4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                        on 4 is var y4 ? y4 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y4").WithArguments("y4").WithLocation(19, 36),
                // (20,43): error CS0136: A local or parameter named 'y5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           equals 5 is var y5 ? y5 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y5").WithArguments("y5").WithLocation(20, 43),
                // (21,34): error CS0136: A local or parameter named 'y6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   where 6 is var y6 && y6 == 1
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y6").WithArguments("y6").WithLocation(21, 34),
                // (22,36): error CS0136: A local or parameter named 'y7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   orderby 7 is var y7 && y7 > 0, 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y7").WithArguments("y7").WithLocation(22, 36),
                // (23,36): error CS0136: A local or parameter named 'y8' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           8 is var y8 && y8 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y8").WithArguments("y8").WithLocation(23, 36),
                // (25,32): error CS0136: A local or parameter named 'y10' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   by 10 is var y10 && y10 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y10").WithArguments("y10").WithLocation(25, 32),
                // (24,34): error CS0136: A local or parameter named 'y9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   group 9 is var y9 && y9 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y9").WithArguments("y9").WithLocation(24, 34),
                // (27,39): error CS0136: A local or parameter named 'y11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   let x11 = 11 is var y11 && y11 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y11").WithArguments("y11").WithLocation(27, 39),
                // (28,36): error CS0136: A local or parameter named 'y12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   select 12 is var y12 && y12 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y12").WithArguments("y12").WithLocation(28, 36)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 13; i++)
            {
                var id = "y" + i;
                var yDecl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == id).Single();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).ToArray();
                Assert.Equal(3, yRef.Length);
                VerifyModelForDeclarationPattern(model, yDecl, yRef[0]);
                VerifyNotAPatternLocal(model, yRef[2]);

                switch (i)
                {
                    case 1:
                    case 3:
                    case 12:
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAPatternLocal(model, yRef[1]);
                        break;
                    default:
                        VerifyNotAPatternLocal(model, yRef[1]);
                        break;
                }

            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void ScopeOfPatternVariables_Query_06()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        Dummy(1 is int y1, 
              2 is int y2, 
              3 is int y3, 
              4 is int y4, 
              5 is int y5, 
              6 is int y6, 
              7 is int y7, 
              8 is int y8, 
              9 is int y9, 
              10 is int y10, 
              11 is int y11, 
              12 is int y12,
                  from x1 in new[] { 1 is var y1 ? y1 : 0}
                  from x2 in new[] { 2 is var y2 ? y2 : 0}
                  join x3 in new[] { 3 is var y3 ? y3 : 0}
                       on 4 is var y4 ? y4 : 0
                          equals 5 is var y5 ? y5 : 0
                  where 6 is var y6 && y6 == 1
                  orderby 7 is var y7 && y7 > 0, 
                          8 is var y8 && y8 > 0 
                  group 9 is var y9 && y9 > 0 
                  by 10 is var y10 && y10 > 0
                  into g
                  let x11 = 11 is var y11 && y11 > 0
                  select 12 is var y12 && y12 > 0
                  into s
                  select y1 + y2 + y3 + y4 + y5 + y6 + y7 + y8 + y9 + y10 + y11 + y12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (26,47): error CS0128: A local variable named 'y1' is already defined in this scope
                //                   from x1 in new[] { 1 is var y1 ? y1 : 0}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y1").WithArguments("y1").WithLocation(26, 47),
                // (27,47): error CS0136: A local or parameter named 'y2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   from x2 in new[] { 2 is var y2 ? y2 : 0}
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y2").WithArguments("y2").WithLocation(27, 47),
                // (28,47): error CS0128: A local variable named 'y3' is already defined in this scope
                //                   join x3 in new[] { 3 is var y3 ? y3 : 0}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y3").WithArguments("y3").WithLocation(28, 47),
                // (29,36): error CS0136: A local or parameter named 'y4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                        on 4 is var y4 ? y4 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y4").WithArguments("y4").WithLocation(29, 36),
                // (30,43): error CS0136: A local or parameter named 'y5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           equals 5 is var y5 ? y5 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y5").WithArguments("y5").WithLocation(30, 43),
                // (31,34): error CS0136: A local or parameter named 'y6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   where 6 is var y6 && y6 == 1
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y6").WithArguments("y6").WithLocation(31, 34),
                // (32,36): error CS0136: A local or parameter named 'y7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   orderby 7 is var y7 && y7 > 0, 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y7").WithArguments("y7").WithLocation(32, 36),
                // (33,36): error CS0136: A local or parameter named 'y8' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           8 is var y8 && y8 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y8").WithArguments("y8").WithLocation(33, 36),
                // (35,32): error CS0136: A local or parameter named 'y10' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   by 10 is var y10 && y10 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y10").WithArguments("y10").WithLocation(35, 32),
                // (34,34): error CS0136: A local or parameter named 'y9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   group 9 is var y9 && y9 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y9").WithArguments("y9").WithLocation(34, 34),
                // (37,39): error CS0136: A local or parameter named 'y11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   let x11 = 11 is var y11 && y11 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y11").WithArguments("y11").WithLocation(37, 39),
                // (38,36): error CS0136: A local or parameter named 'y12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   select 12 is var y12 && y12 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y12").WithArguments("y12").WithLocation(38, 36)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 13; i++)
            {
                var id = "y" + i;
                var yDecl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == id).ToArray();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).ToArray();
                Assert.Equal(2, yDecl.Length);
                Assert.Equal(2, yRef.Length);

                switch (i)
                {
                    case 1:
                    case 3:
                        VerifyModelForDeclarationPattern(model, yDecl[0], yRef);
                        VerifyModelForDeclarationPatternDuplicateInSameScope(model, yDecl[1]);
                        break;
                    case 12:
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyModelForDeclarationPattern(model, yDecl[0], yRef[1]);
                        VerifyModelForDeclarationPattern(model, yDecl[1], yRef[0]);
                        break;

                    default:
                        VerifyModelForDeclarationPattern(model, yDecl[0], yRef[1]);
                        VerifyModelForDeclarationPattern(model, yDecl[1], yRef[0]);
                        break;
                }
            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void ScopeOfPatternVariables_Query_07()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        Dummy(7 is int y3, 
                  from x1 in new[] { 0 }
                  select x1
                  into x1
                  join x3 in new[] { 3 is var y3 ? y3 : 0}
                       on x1 equals x3
                  select y3);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (18,47): error CS0128: A local variable named 'y3' is already defined in this scope
                //                   join x3 in new[] { 3 is var y3 ? y3 : 0}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y3").WithArguments("y3").WithLocation(18, 47)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            const string id = "y3";
            var yDecl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == id).ToArray();
            var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).ToArray();
            Assert.Equal(2, yDecl.Length);
            Assert.Equal(2, yRef.Length);
            VerifyModelForDeclarationPattern(model, yDecl[0], yRef[1]);
            // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
            //VerifyModelForDeclarationPattern(model, yDecl[1], yRef[0]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Query_08()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from x1 in new[] { Dummy(1 is var y1, 
                                           2 is var y2,
                                           3 is var y3,
                                           4 is var y4
                                          ) ? 1 : 0}
                  from y1 in new[] { 1 }
                  join y2 in new[] { 0 }
                       on y1 equals y2
                  let y3 = 0
                  group y3 
                  by x1
                  into y4
                  select y4;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (19,24): error CS1931: The range variable 'y1' conflicts with a previous declaration of 'y1'
                //                   from y1 in new[] { 1 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y1").WithArguments("y1").WithLocation(19, 24),
                // (20,24): error CS1931: The range variable 'y2' conflicts with a previous declaration of 'y2'
                //                   join y2 in new[] { 0 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y2").WithArguments("y2").WithLocation(20, 24),
                // (22,23): error CS1931: The range variable 'y3' conflicts with a previous declaration of 'y3'
                //                   let y3 = 0
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y3").WithArguments("y3").WithLocation(22, 23),
                // (25,24): error CS1931: The range variable 'y4' conflicts with a previous declaration of 'y4'
                //                   into y4
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y4").WithArguments("y4").WithLocation(25, 24)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 5; i++)
            {
                var id = "y" + i;
                var yDecl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == id).Single();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).Single();
                VerifyModelForDeclarationPattern(model, yDecl);
                VerifyNotAPatternLocal(model, yRef);
            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void ScopeOfPatternVariables_Query_09()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from y1 in new[] { 0 }
                  join y2 in new[] { 0 }
                       on y1 equals y2
                  let y3 = 0
                  group y3 
                  by 1
                  into y4
                  select y4 == null ? 1 : 0
                  into x2
                  join y5 in new[] { Dummy(1 is var y1, 
                                           2 is var y2,
                                           3 is var y3,
                                           4 is var y4,
                                           5 is var y5
                                          ) ? 1 : 0 }
                       on x2 equals y5
                  select x2;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (14,24): error CS1931: The range variable 'y1' conflicts with a previous declaration of 'y1'
                //         var res = from y1 in new[] { 0 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y1").WithArguments("y1").WithLocation(14, 24),
                // (15,24): error CS1931: The range variable 'y2' conflicts with a previous declaration of 'y2'
                //                   join y2 in new[] { 0 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y2").WithArguments("y2").WithLocation(15, 24),
                // (17,23): error CS1931: The range variable 'y3' conflicts with a previous declaration of 'y3'
                //                   let y3 = 0
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y3").WithArguments("y3").WithLocation(17, 23),
                // (20,24): error CS1931: The range variable 'y4' conflicts with a previous declaration of 'y4'
                //                   into y4
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y4").WithArguments("y4").WithLocation(20, 24),
                // (23,24): error CS1931: The range variable 'y5' conflicts with a previous declaration of 'y5'
                //                   join y5 in new[] { Dummy(1 is var y1, 
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y5").WithArguments("y5").WithLocation(23, 24)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 6; i++)
            {
                var id = "y" + i;
                var yDecl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == id).Single();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).Single();

                switch (i)
                {
                    case 4:
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyModelForDeclarationPattern(model, yDecl);
                        VerifyNotAPatternLocal(model, yRef);
                        break;
                    case 5:
                        VerifyModelForDeclarationPattern(model, yDecl);
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAPatternLocal(model, yRef);
                        break;
                    default:
                        VerifyModelForDeclarationPattern(model, yDecl);
                        VerifyNotAPatternLocal(model, yRef);
                        break;
                }
            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        [WorkItem(12052, "https://github.com/dotnet/roslyn/issues/12052")]
        public void ScopeOfPatternVariables_Query_10()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from y1 in new[] { 0 }
                  from x2 in new[] { 1 is var y1 ? y1 : 1 }
                  select y1;
    }

    void Test2()
    {
        var res = from y2 in new[] { 0 }
                  join x3 in new[] { 1 }
                       on 2 is var y2 ? y2 : 0 
                       equals x3
                  select y2;
    }

    void Test3()
    {
        var res = from x3 in new[] { 0 }
                  join y3 in new[] { 1 }
                       on x3 
                       equals 3 is var y3 ? y3 : 0
                  select y3;
    }

    void Test4()
    {
        var res = from y4 in new[] { 0 }
                  where 4 is var y4 && y4 == 1
                  select y4;
    }

    void Test5()
    {
        var res = from y5 in new[] { 0 }
                  orderby 5 is var y5 && y5 > 1, 
                          1 
                  select y5;
    }

    void Test6()
    {
        var res = from y6 in new[] { 0 }
                  orderby 1, 
                          6 is var y6 && y6 > 1 
                  select y6;
    }

    void Test7()
    {
        var res = from y7 in new[] { 0 }
                  group 7 is var y7 && y7 == 3 
                  by y7;
    }

    void Test8()
    {
        var res = from y8 in new[] { 0 }
                  group y8 
                  by 8 is var y8 && y8 == 3;
    }

    void Test9()
    {
        var res = from y9 in new[] { 0 }
                  let x4 = 9 is var y9 && y9 > 0
                  select y9;
    }

    void Test10()
    {
        var res = from y10 in new[] { 0 }
                  select 10 is var y10 && y10 > 0;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);

            // error CS0412 is misleading and reported due to preexisting bug https://github.com/dotnet/roslyn/issues/12052
            compilation.VerifyDiagnostics(
                // (15,47): error CS0412: 'y1': a parameter or local variable cannot have the same name as a method type parameter
                //                   from x2 in new[] { 1 is var y1 ? y1 : 1 }
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y1").WithArguments("y1").WithLocation(15, 47),
                // (23,36): error CS0412: 'y2': a parameter or local variable cannot have the same name as a method type parameter
                //                        on 2 is var y2 ? y2 : 0 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y2").WithArguments("y2").WithLocation(23, 36),
                // (33,40): error CS0412: 'y3': a parameter or local variable cannot have the same name as a method type parameter
                //                        equals 3 is var y3 ? y3 : 0
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y3").WithArguments("y3").WithLocation(33, 40),
                // (40,34): error CS0412: 'y4': a parameter or local variable cannot have the same name as a method type parameter
                //                   where 4 is var y4 && y4 == 1
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y4").WithArguments("y4").WithLocation(40, 34),
                // (47,36): error CS0412: 'y5': a parameter or local variable cannot have the same name as a method type parameter
                //                   orderby 5 is var y5 && y5 > 1, 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y5").WithArguments("y5").WithLocation(47, 36),
                // (56,36): error CS0412: 'y6': a parameter or local variable cannot have the same name as a method type parameter
                //                           6 is var y6 && y6 > 1 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y6").WithArguments("y6").WithLocation(56, 36),
                // (63,34): error CS0412: 'y7': a parameter or local variable cannot have the same name as a method type parameter
                //                   group 7 is var y7 && y7 == 3 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y7").WithArguments("y7").WithLocation(63, 34),
                // (71,31): error CS0412: 'y8': a parameter or local variable cannot have the same name as a method type parameter
                //                   by 8 is var y8 && y8 == 3;
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y8").WithArguments("y8").WithLocation(71, 31),
                // (77,37): error CS0412: 'y9': a parameter or local variable cannot have the same name as a method type parameter
                //                   let x4 = 9 is var y9 && y9 > 0
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y9").WithArguments("y9").WithLocation(77, 37),
                // (84,36): error CS0412: 'y10': a parameter or local variable cannot have the same name as a method type parameter
                //                   select 10 is var y10 && y10 > 0;
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y10").WithArguments("y10").WithLocation(84, 36)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 11; i++)
            {
                var id = "y" + i;
                var yDecl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == id).Single();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).ToArray();
                Assert.Equal(i == 10 ? 1 : 2, yRef.Length);

                switch (i)
                {
                    case 4:
                    case 6:
                        VerifyModelForDeclarationPattern(model, yDecl, yRef[0]);
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAPatternLocal(model, yRef[1]);
                        break;
                    case 8:
                        VerifyModelForDeclarationPattern(model, yDecl, yRef[1]);
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAPatternLocal(model, yRef[0]);
                        break;
                    case 10:
                        VerifyModelForDeclarationPattern(model, yDecl, yRef[0]);
                        break;
                    default:
                        VerifyModelForDeclarationPattern(model, yDecl, yRef[0]);
                        VerifyNotAPatternLocal(model, yRef[1]);
                        break;
                }
            }
        }

        [Fact]
        public void Query_01()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
        Test1();
    }

    static void Test1()
    {
        var res = from x1 in new[] { 1 is var y1 && Print(y1) ? 1 : 0}
                  from x2 in new[] { 2 is var y2 && Print(y2) ? 1 : 0}
                  join x3 in new[] { 3 is var y3 && Print(y3) ? 1 : 0}
                       on 4 is var y4 && Print(y4) ? 1 : 0
                          equals 5 is var y5 && Print(y5) ? 1 : 0
                  where 6 is var y6 && Print(y6)
                  orderby 7 is var y7 && Print(y7), 
                          8 is var y8 && Print(y8) 
                  group 9 is var y9 && Print(y9) 
                  by 10 is var y10 && Print(y10)
                  into g
                  let x11 = 11 is var y11 && Print(y11)
                  select 12 is var y12 && Print(y12);

        res.ToArray(); 
    }

    static bool Print(object x) 
    {
        System.Console.WriteLine(x);
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"1
3
5
2
4
6
7
8
10
9
11
12
");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 13; i++)
            {
                var id = "y" + i;
                var yDecl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == id).Single();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).Single();
                VerifyModelForDeclarationPattern(model, yDecl, yRef);
            }
        }

        [Fact]
        public void ScopeOfPatternVariables_ExpressionBodiedLocalFunctions_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        void f(object o) => let x1 = o;
        f(null);
    }

    void Test2()
    {
        void f(object o) => let var x2 = o;
        f(null);
    }

    void Test3()
    {
        bool f (object o) => o is int x3 && x3 > 0;
        f(null);
    }

    void Test4()
    {
        bool f (object o) => x4 && o is int x4;
        f(null);
    }

    void Test5()
    {
        bool f (object o1, object o2) => o1 is int x5 && 
                                         o2 is int x5 && 
                                         x5 > 0;
        f(null, null);
    }

    void Test6()
    {
        bool f1 (object o) => o is int x6 && x6 > 0; bool f2 (object o) => o is int x6 && x6 > 0;
        f1(null);
        f2(null);
    }

    void Test7()
    {
        Dummy(x7, 1);
         
        bool f (object o) => o is int x7 && x7 > 0; 

        Dummy(x7, 2); 
        f(null);
    }

    void Test11()
    {
        var x11 = 11;
        Dummy(x11);
        bool f (object o) => o is int x11 && 
                             x11 > 0;
        f(null);
    }

    void Test12()
    {
        bool f (object o) => o is int x12 && 
                             x12 > 0;
        var x12 = 11;
        Dummy(x12);
        f(null);
    }

    System.Action Test13()
    {
        return () =>
                    {
                        bool f (object o) => o is int x13 && x13 > 0;
                        f(null);
                    };
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (12,33): error CS1002: ; expected
    //         void f(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x1").WithLocation(12, 33),
    // (18,33): error CS1002: ; expected
    //         void f(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(18, 33),
    // (12,29): error CS0103: The name 'let' does not exist in the current context
    //         void f(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(12, 29),
    // (12,29): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         void f(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(12, 29),
    // (12,33): error CS0103: The name 'x1' does not exist in the current context
    //         void f(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(12, 33),
    // (12,38): error CS0103: The name 'o' does not exist in the current context
    //         void f(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(12, 38),
    // (18,29): error CS0103: The name 'let' does not exist in the current context
    //         void f(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(18, 29),
    // (18,29): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         void f(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(18, 29),
    // (18,42): error CS0103: The name 'o' does not exist in the current context
    //         void f(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(18, 42),
    // (30,30): error CS0841: Cannot use local variable 'x4' before it is declared
    //         bool f (object o) => x4 && o is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(30, 30),
    // (37,52): error CS0128: A local variable named 'x5' is already defined in this scope
    //                                          o2 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(37, 52),
    // (38,42): error CS0165: Use of unassigned local variable 'x5'
    //                                          x5 > 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x5").WithArguments("x5").WithLocation(38, 42),
    // (51,15): error CS0103: The name 'x7' does not exist in the current context
    //         Dummy(x7, 1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(51, 15),
    // (55,15): error CS0103: The name 'x7' does not exist in the current context
    //         Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(55, 15),
    // (63,39): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         bool f (object o) => o is int x11 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(63, 39),
    // (70,39): error CS0136: A local or parameter named 'x12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         bool f (object o) => o is int x12 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x12").WithArguments("x12").WithLocation(70, 39)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyNotInScope(model, x7Ref[0]);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotAPatternLocal(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[0]);
            VerifyNotAPatternLocal(model, x12Ref[1]);

            var x13Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x13").Single();
            var x13Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x13").Single();
            VerifyModelForDeclarationPattern(model, x13Decl, x13Ref);
        }

        [Fact]
        public void ExpressionBodiedLocalFunctions_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1());
    }

    static bool Test1()
    {
        bool f() => 1 is int x1 && Dummy(x1); 
        return f();
    }

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void ScopeOfPatternVariables_ExpressionBodiedFunctions_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }


    void Test1(object o) => let x1 = o;

    void Test2(object o) => let var x2 = o;

    bool Test3(object o) => o is int x3 && x3 > 0;

    bool Test4(object o) => x4 && o is int x4;

    bool Test5(object o1, object o2) => o1 is int x5 && 
                                         o2 is int x5 && 
                                         x5 > 0;

    bool Test61 (object o) => o is int x6 && x6 > 0; bool Test62 (object o) => o is int x6 && x6 > 0;

    bool Test71(object o) => o is int x7 && x7 > 0; 
    void Test72() => Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool Test11(object x11) => 1 is int x11 && 
                             x11 > 0;

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (9,33): error CS1002: ; expected
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x1").WithLocation(9, 33),
    // (9,36): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(9, 36),
    // (9,36): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(9, 36),
    // (9,39): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(9, 39),
    // (9,39): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(9, 39),
    // (11,33): error CS1002: ; expected
    //     void Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(11, 33),
    // (11,33): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
    //     void Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(11, 33),
    // (11,42): error CS0103: The name 'o' does not exist in the current context
    //     void Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(11, 42),
    // (9,29): error CS0103: The name 'let' does not exist in the current context
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(9, 29),
    // (9,29): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(9, 29),
    // (11,29): error CS0103: The name 'let' does not exist in the current context
    //     void Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(11, 29),
    // (11,29): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //     void Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(11, 29),
    // (15,29): error CS0841: Cannot use local variable 'x4' before it is declared
    //     bool Test4(object o) => x4 && o is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(15, 29),
    // (18,52): error CS0128: A local variable named 'x5' is already defined in this scope
    //                                          o2 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(18, 52),
    // (19,42): error CS0165: Use of unassigned local variable 'x5'
    //                                          x5 > 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x5").WithArguments("x5").WithLocation(19, 42),
    // (24,28): error CS0103: The name 'x7' does not exist in the current context
    //     void Test72() => Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(24, 28),
    // (25,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(25, 27),
    // (27,41): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //     bool Test11(object x11) => 1 is int x11 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(27, 41)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").Single();
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref);
        }

        [Fact]
        public void ExpressionBodiedFunctions_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1());
    }

    static bool Test1() => 1 is int x1 && Dummy(x1); 

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void ScopeOfPatternVariables_ExpressionBodiedProperties_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }


    bool Test1 => let x1 = 11;

    bool this[int o] => let var x2 = o;

    bool Test3 => 3 is int x3 && x3 > 0;

    bool Test4 => x4 && 4 is int x4;

    bool Test5 => 51 is int x5 && 
                  52 is int x5 && 
                  x5 > 0;

    bool Test61 => 6 is int x6 && x6 > 0; bool Test62 => 6 is int x6 && x6 > 0;

    bool Test71 => 7 is int x7 && x7 > 0; 
    bool Test72 => Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool this[object x11] => 1 is int x11 && 
                             x11 > 0;

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (9,23): error CS1002: ; expected
    //     bool Test1 => let x1 = 11;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x1").WithLocation(9, 23),
    // (9,26): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     bool Test1 => let x1 = 11;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(9, 26),
    // (9,26): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     bool Test1 => let x1 = 11;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(9, 26),
    // (11,29): error CS1002: ; expected
    //     bool this[int o] => let var x2 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(11, 29),
    // (11,29): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
    //     bool this[int o] => let var x2 = o;
    Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(11, 29),
    // (11,38): error CS0103: The name 'o' does not exist in the current context
    //     bool this[int o] => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(11, 38),
    // (9,19): error CS0103: The name 'let' does not exist in the current context
    //     bool Test1 => let x1 = 11;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(9, 19),
    // (11,25): error CS0103: The name 'let' does not exist in the current context
    //     bool this[int o] => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(11, 25),
    // (15,19): error CS0841: Cannot use local variable 'x4' before it is declared
    //     bool Test4 => x4 && 4 is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(15, 19),
    // (18,29): error CS0128: A local variable named 'x5' is already defined in this scope
    //                   52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(18, 29),
    // (24,26): error CS0103: The name 'x7' does not exist in the current context
    //     bool Test72 => Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(24, 26),
    // (25,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(25, 27),
    // (27,39): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //     bool this[object x11] => 1 is int x11 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(27, 39)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").Single();
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref);
        }

        [Fact]
        public void ExpressionBodiedProperties_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1);
        System.Console.WriteLine(new X()[0]);
    }

    static bool Test1 => 2 is int x1 && Dummy(x1); 

    bool this[object x] => 1 is int x1 && Dummy(x1); 

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"2
True
1
True");
        }

        [Fact]
        public void FieldInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1);
    }

    static bool Test1 = 1 is int x1 && Dummy(x1); 

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact, WorkItem(10487, "https://github.com/dotnet/roslyn/issues/10487")]
        public void FieldInitializers_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1);
        new X().M();
    }
    void M()
    {
        System.Console.WriteLine(Test2);
    }

    static bool Test1 = 1 is int x1 && Dummy(() => x1);
    bool Test2 = 2 is int x1 && Dummy(() => x1);

    static bool Dummy(System.Func<int> x)
    {
        System.Console.WriteLine(x());
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
True
2
True");
        }

        [Fact]
        public void ScopeOfPatternVariables_FieldInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Test3 = 3 is int x3 && x3 > 0;

    bool Test4 = x4 && 4 is int x4;

    bool Test5 = 51 is int x5 && 
                 52 is int x5 && 
                 x5 > 0;

    bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;

    bool Test71 = 7 is int x7 && x7 > 0; 
    bool Test72 = Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (10,18): error CS0841: Cannot use local variable 'x4' before it is declared
    //     bool Test4 = x4 && 4 is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(10, 18),
    // (13,28): error CS0128: A local variable named 'x5' is already defined in this scope
    //                  52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(13, 28),
    // (19,25): error CS0103: The name 'x7' does not exist in the current context
    //     bool Test72 = Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(19, 25),
    // (20,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(20, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_FieldInitializers_02()
        {
            var source =
@"
public enum X
{
    Test3 = 3 is int x3 ? x3 : 0,

    Test4 = x4 && 4 is int x4 ? 1 : 0,

    Test5 = 51 is int x5 && 
            52 is int x5 && 
            x5 > 0 ? 1 : 0,

    Test61 = 6 is int x6 && x6 > 0 ? 1 : 0, Test62 = 6 is int x6 && x6 > 0 ? 1 : 0,

    Test71 = 7 is int x7 && x7 > 0 ? 1 : 0, 
    Test72 = x7, 
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
    // (6,13): error CS0841: Cannot use local variable 'x4' before it is declared
    //     Test4 = x4 && 4 is int x4 ? 1 : 0,
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(6, 13),
    // (9,23): error CS0128: A local variable named 'x5' is already defined in this scope
    //             52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(9, 23),
    // (12,14): error CS0133: The expression being assigned to 'X.Test61' must be constant
    //     Test61 = 6 is int x6 && x6 > 0 ? 1 : 0, Test62 = 6 is int x6 && x6 > 0 ? 1 : 0,
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "6 is int x6 && x6 > 0 ? 1 : 0").WithArguments("X.Test61").WithLocation(12, 14),
    // (12,54): error CS0133: The expression being assigned to 'X.Test62' must be constant
    //     Test61 = 6 is int x6 && x6 > 0 ? 1 : 0, Test62 = 6 is int x6 && x6 > 0 ? 1 : 0,
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "6 is int x6 && x6 > 0 ? 1 : 0").WithArguments("X.Test62").WithLocation(12, 54),
    // (14,14): error CS0133: The expression being assigned to 'X.Test71' must be constant
    //     Test71 = 7 is int x7 && x7 > 0 ? 1 : 0, 
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "7 is int x7 && x7 > 0 ? 1 : 0").WithArguments("X.Test71").WithLocation(14, 14),
    // (15,14): error CS0103: The name 'x7' does not exist in the current context
    //     Test72 = x7, 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(15, 14),
    // (4,13): error CS0133: The expression being assigned to 'X.Test3' must be constant
    //     Test3 = 3 is int x3 ? x3 : 0,
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "3 is int x3 ? x3 : 0").WithArguments("X.Test3").WithLocation(4, 13)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
        }
        
        [Fact]
        public void ScopeOfPatternVariables_FieldInitializers_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    const bool Test3 = 3 is int x3 && x3 > 0;

    const bool Test4 = x4 && 4 is int x4;

    const bool Test5 = 51 is int x5 && 
                       52 is int x5 && 
                       x5 > 0;

    const bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;

    const bool Test71 = 7 is int x7 && x7 > 0; 
    const bool Test72 = x7 > 2; 
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (8,24): error CS0133: The expression being assigned to 'X.Test3' must be constant
    //     const bool Test3 = 3 is int x3 && x3 > 0;
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "3 is int x3 && x3 > 0").WithArguments("X.Test3").WithLocation(8, 24),
    // (10,24): error CS0841: Cannot use local variable 'x4' before it is declared
    //     const bool Test4 = x4 && 4 is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(10, 24),
    // (13,34): error CS0128: A local variable named 'x5' is already defined in this scope
    //                        52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(13, 34),
    // (16,25): error CS0133: The expression being assigned to 'X.Test61' must be constant
    //     const bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "6 is int x6 && x6 > 0").WithArguments("X.Test61").WithLocation(16, 25),
    // (16,57): error CS0133: The expression being assigned to 'X.Test62' must be constant
    //     const bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "6 is int x6 && x6 > 0").WithArguments("X.Test62").WithLocation(16, 57),
    // (18,25): error CS0133: The expression being assigned to 'X.Test71' must be constant
    //     const bool Test71 = 7 is int x7 && x7 > 0; 
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "7 is int x7 && x7 > 0").WithArguments("X.Test71").WithLocation(18, 25),
    // (19,25): error CS0103: The name 'x7' does not exist in the current context
    //     const bool Test72 = x7 > 2; 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(19, 25),
    // (20,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(20, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void PropertyInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1);
    }

    static bool Test1 {get;} = 1 is int x1 && Dummy(x1); 

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void ScopeOfPatternVariables_PropertyInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Test3 {get;} = 3 is int x3 && x3 > 0;

    bool Test4 {get;} = x4 && 4 is int x4;

    bool Test5 {get;} = 51 is int x5 && 
                 52 is int x5 && 
                 x5 > 0;

    bool Test61 {get;} = 6 is int x6 && x6 > 0; bool Test62 {get;} = 6 is int x6 && x6 > 0;

    bool Test71 {get;} = 7 is int x7 && x7 > 0; 
    bool Test72 {get;} = Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (10,25): error CS0841: Cannot use local variable 'x4' before it is declared
    //     bool Test4 {get;} = x4 && 4 is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(10, 25),
    // (13,28): error CS0128: A local variable named 'x5' is already defined in this scope
    //                  52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(13, 28),
    // (19,32): error CS0103: The name 'x7' does not exist in the current context
    //     bool Test72 {get;} = Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(19, 32),
    // (20,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(20, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_ParameterDefault_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Test3(bool p = 3 is int x3 && x3 > 0)
    {}

    void Test4(bool p = x4 && 4 is int x4)
    {}

    void Test5(bool p = 51 is int x5 && 
                        52 is int x5 && 
                        x5 > 0)
    {}

    void Test61(bool p1 = 6 is int x6 && x6 > 0, bool p2 = 6 is int x6 && x6 > 0)
    {}

    void Test71(bool p = 7 is int x7 && x7 > 0)
    {
    }

    void Test72(bool p = x7 > 2)
    {}

    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (8,25): error CS1736: Default parameter value for 'p' must be a compile-time constant
    //     void Test3(bool p = 3 is int x3 && x3 > 0)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "3 is int x3 && x3 > 0").WithArguments("p").WithLocation(8, 25),
    // (11,25): error CS0841: Cannot use local variable 'x4' before it is declared
    //     void Test4(bool p = x4 && 4 is int x4)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(11, 25),
    // (11,21): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'bool'
    //     void Test4(bool p = x4 && 4 is int x4)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "p").WithArguments("?", "bool").WithLocation(11, 21),
    // (15,35): error CS0128: A local variable named 'x5' is already defined in this scope
    //                         52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(15, 35),
    // (14,21): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'bool'
    //     void Test5(bool p = 51 is int x5 && 
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "p").WithArguments("?", "bool").WithLocation(14, 21),
    // (19,27): error CS1736: Default parameter value for 'p1' must be a compile-time constant
    //     void Test61(bool p1 = 6 is int x6 && x6 > 0, bool p2 = 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "6 is int x6 && x6 > 0").WithArguments("p1").WithLocation(19, 27),
    // (19,60): error CS1736: Default parameter value for 'p2' must be a compile-time constant
    //     void Test61(bool p1 = 6 is int x6 && x6 > 0, bool p2 = 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "6 is int x6 && x6 > 0").WithArguments("p2").WithLocation(19, 60),
    // (22,26): error CS1736: Default parameter value for 'p' must be a compile-time constant
    //     void Test71(bool p = 7 is int x7 && x7 > 0)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "7 is int x7 && x7 > 0").WithArguments("p").WithLocation(22, 26),
    // (26,26): error CS0103: The name 'x7' does not exist in the current context
    //     void Test72(bool p = x7 > 2)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(26, 26),
    // (29,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(29, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Attribute_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    [Test(p = 3 is int x3 && x3 > 0)]
    [Test(p = x4 && 4 is int x4)]
    [Test(p = 51 is int x5 && 
              52 is int x5 && 
              x5 > 0)]
    [Test(p1 = 6 is int x6 && x6 > 0, p2 = 6 is int x6 && x6 > 0)]
    [Test(p = 7 is int x7 && x7 > 0)]
    [Test(p = x7 > 2)]
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}

class Test : System.Attribute
{
    public bool p {get; set;}
    public bool p1 {get; set;}
    public bool p2 {get; set;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (8,15): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [Test(p = 3 is int x3 && x3 > 0)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "3 is int x3 && x3 > 0").WithLocation(8, 15),
    // (9,15): error CS0841: Cannot use local variable 'x4' before it is declared
    //     [Test(p = x4 && 4 is int x4)]
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(9, 15),
    // (11,25): error CS0128: A local variable named 'x5' is already defined in this scope
    //               52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(11, 25),
    // (13,53): error CS0128: A local variable named 'x6' is already defined in this scope
    //     [Test(p1 = 6 is int x6 && x6 > 0, p2 = 6 is int x6 && x6 > 0)]
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(13, 53),
    // (13,16): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [Test(p1 = 6 is int x6 && x6 > 0, p2 = 6 is int x6 && x6 > 0)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "6 is int x6 && x6 > 0").WithLocation(13, 16),
    // (14,15): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [Test(p = 7 is int x7 && x7 > 0)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "7 is int x7 && x7 > 0").WithLocation(14, 15),
    // (15,15): error CS0103: The name 'x7' does not exist in the current context
    //     [Test(p = x7 > 2)]
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(15, 15),
    // (16,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(16, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Attribute_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    [Test(3 is int x3 && x3 > 0)]
    [Test(x4 && 4 is int x4)]
    [Test(51 is int x5 && 
          52 is int x5 && 
          x5 > 0)]
    [Test(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)]
    [Test(7 is int x7 && x7 > 0)]
    [Test(x7 > 2)]
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}

class Test : System.Attribute
{
    public Test(bool p) {}
    public Test(bool p1, bool p2) {}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (8,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [Test(3 is int x3 && x3 > 0)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "3 is int x3 && x3 > 0").WithLocation(8, 11),
    // (9,11): error CS0841: Cannot use local variable 'x4' before it is declared
    //     [Test(x4 && 4 is int x4)]
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(9, 11),
    // (11,21): error CS0128: A local variable named 'x5' is already defined in this scope
    //           52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(11, 21),
    // (13,43): error CS0128: A local variable named 'x6' is already defined in this scope
    //     [Test(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)]
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(13, 43),
    // (14,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [Test(7 is int x7 && x7 > 0)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "7 is int x7 && x7 > 0").WithLocation(14, 11),
    // (15,11): error CS0103: The name 'x7' does not exist in the current context
    //     [Test(x7 > 2)]
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(15, 11),
    // (16,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(16, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_ConstructorInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    X(byte x)
        : this(3 is int x3 && x3 > 0)
    {}

    X(sbyte x)
        : this(x4 && 4 is int x4)
    {}

    X(short x)
        : this(51 is int x5 && 
               52 is int x5 && 
               x5 > 0)
    {}

    X(ushort x)
        : this(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    {}

    X(int x)
        : this(7 is int x7 && x7 > 0)
    {}
    X(uint x)
        : this(x7, 2)
    {}
    void Test73() { Dummy(x7, 3); } 

    X(params object[] x) {}
    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (13,16): error CS0841: Cannot use local variable 'x4' before it is declared
    //         : this(x4 && 4 is int x4)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(13, 16),
    // (18,26): error CS0128: A local variable named 'x5' is already defined in this scope
    //                52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(18, 26),
    // (23,48): error CS0128: A local variable named 'x6' is already defined in this scope
    //         : this(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(23, 48),
    // (30,16): error CS0103: The name 'x7' does not exist in the current context
    //         : this(x7, 2)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(30, 16),
    // (32,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(32, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_ConstructorInitializers_02()
        {
            var source =
@"
public class X : Y
{
    public static void Main()
    {
    }

    X(byte x)
        : base(3 is int x3 && x3 > 0)
    {}

    X(sbyte x)
        : base(x4 && 4 is int x4)
    {}

    X(short x)
        : base(51 is int x5 && 
               52 is int x5 && 
               x5 > 0)
    {}

    X(ushort x)
        : base(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    {}

    X(int x)
        : base(7 is int x7 && x7 > 0)
    {}
    X(uint x)
        : base(x7, 2)
    {}
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}

public class Y
{
    public Y(params object[] x) {}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (13,16): error CS0841: Cannot use local variable 'x4' before it is declared
    //         : base(x4 && 4 is int x4)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(13, 16),
    // (18,26): error CS0128: A local variable named 'x5' is already defined in this scope
    //                52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(18, 26),
    // (23,48): error CS0128: A local variable named 'x6' is already defined in this scope
    //         : base(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(23, 48),
    // (30,16): error CS0103: The name 'x7' does not exist in the current context
    //         : base(x7, 2)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(30, 16),
    // (32,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(32, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_ConstructorInitializers_03()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        new D(1);
        new D(10);
        new D(1.2);
    }
}
class D
{
    public D(object o) : this(o is int x && x >= 5) 
    {
        Console.WriteLine(x);
    }

    public D(bool b) { Console.WriteLine(b); }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (15,27): error CS0103: The name 'x' does not exist in the current context
    //         Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(15, 27)
                );
        }

        [Fact]
        public void ScopeOfPatternVariables_ConstructorInitializers_04()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        new D(1);
        new D(10);
        new D(1.2);
    }
}
class D : C
{
    public D(object o) : base(o is int x && x >= 5) 
    {
        Console.WriteLine(x);
    }
}

class C
{
    public C(bool b) { Console.WriteLine(b); }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (15,27): error CS0103: The name 'x' does not exist in the current context
    //         Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(15, 27)
                );
        }
        [Fact]
        public void ConstructorInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        var x = new D();
    }
}

class D : C
{
    public D(object o) : base(2 is int x1 && Dummy(x1)) 
    {
        System.Console.WriteLine(o);
    }

    public D() : this(1 is int x1 && Dummy(x1)) 
    {
    }

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }
}

class C
{
    public C(object b) 
    { 
        System.Console.WriteLine(b);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: 
@"1
2
True
True");
        }

        [Fact]
        public void ScopeOfPatternVariables_SwitchLabelGuard_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) { return true; }

    void Test1(int val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x1, x1):
                Dummy(x1);
                break;
            case 1 when Dummy(true is var x1, x1):
                Dummy(x1);
                break;
            case 2 when Dummy(true is var x1, x1):
                Dummy(x1);
                break;
        }
    }

    void Test2(int val)
    {
        switch (val)
        {
            case 0 when Dummy(x2, true is var x2):
                Dummy(x2);
                break;
        }
    }

    void Test3(int x3, int val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x3, x3):
                Dummy(x3);
                break;
        }
    }

    void Test4(int val)
    {
        var x4 = 11;
        switch (val)
        {
            case 0 when Dummy(true is var x4, x4):
                Dummy(x4);
                break;
            case 1 when Dummy(x4): Dummy(x4); break;
        }
    }

    void Test5(int val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x5, x5):
                Dummy(x5);
                break;
        }
        
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6(int val)
    //{
    //    let x6 = 11;
    //    switch (val)
    //    {
    //        case 0 when Dummy(x6):
    //            Dummy(x6);
    //            break;
    //        case 1 when Dummy(true is var x6, x6):
    //            Dummy(x6);
    //            break;
    //    }
    //}

    //void Test7(int val)
    //{
    //    switch (val)
    //    {
    //        case 0 when Dummy(true is var x7, x7):
    //            Dummy(x7);
    //            break;
    //    }
        
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8(int val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x8, x8, false is var x8, x8):
                Dummy(x8);
                break;
        }
    }

    void Test9(int val)
    {
        switch (val)
        {
            case 0 when Dummy(x9):
                int x9 = 9;
                Dummy(x9);
                break;
            case 2 when Dummy(x9 = 9):
                Dummy(x9);
                break;
            case 1 when Dummy(true is var x9, x9):
                Dummy(x9);
                break;
        }
    }

    //void Test10(int val)
    //{
    //    switch (val)
    //    {
    //        case 1 when Dummy(true is var x10, x10):
    //            Dummy(x10);
    //            break;
    //        case 0 when Dummy(x10):
    //            let x10 = 10;
    //            Dummy(x10);
    //            break;
    //        case 2 when Dummy(x10 = 10, x10):
    //            Dummy(x10);
    //            break;
    //    }
    //}

    void Test11(int val)
    {
        switch (x11 ? val : 0)
        {
            case 0 when Dummy(x11):
                Dummy(x11, 0);
                break;
            case 1 when Dummy(true is var x11, x11):
                Dummy(x11, 1);
                break;
        }
    }

    void Test12(int val)
    {
        switch (x12 ? val : 0)
        {
            case 0 when Dummy(true is var x12, x12):
                Dummy(x12, 0);
                break;
            case 1 when Dummy(x12):
                Dummy(x12, 1);
                break;
        }
    }

    void Test13()
    {
        switch (1 is var x13 ? x13 : 0)
        {
            case 0 when Dummy(x13):
                Dummy(x13);
                break;
            case 1 when Dummy(true is var x13, x13):
                Dummy(x13);
                break;
        }
    }

    void Test14(int val)
    {
        switch (val)
        {
            case 1 when Dummy(true is var x14, x14):
                Dummy(x14);
                Dummy(true is var x14, x14);
                Dummy(x14);
                break;
        }
    }

    void Test15(int val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x15, x15):
            case 1 when Dummy(true is var x15, x15):
                Dummy(x15);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (30,31): error CS0841: Cannot use local variable 'x2' before it is declared
    //             case 0 when Dummy(x2, true is var x2):
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(30, 31),
    // (40,43): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             case 0 when Dummy(true is var x3, x3):
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(40, 43),
    // (51,43): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             case 0 when Dummy(true is var x4, x4):
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(51, 43),
    // (62,43): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             case 0 when Dummy(true is var x5, x5):
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(62, 43),
    // (102,64): error CS0128: A local variable named 'x8' is already defined in this scope
    //             case 0 when Dummy(true is var x8, x8, false is var x8, x8):
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(102, 64),
    // (112,31): error CS0841: Cannot use local variable 'x9' before it is declared
    //             case 0 when Dummy(x9):
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(112, 31),
    // (119,43): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             case 1 when Dummy(true is var x9, x9):
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(119, 43),
    // (144,17): error CS0103: The name 'x11' does not exist in the current context
    //         switch (x11 ? val : 0)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(144, 17),
    // (146,31): error CS0103: The name 'x11' does not exist in the current context
    //             case 0 when Dummy(x11):
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(146, 31),
    // (147,23): error CS0103: The name 'x11' does not exist in the current context
    //                 Dummy(x11, 0);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(147, 23),
    // (157,17): error CS0103: The name 'x12' does not exist in the current context
    //         switch (x12 ? val : 0)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(157, 17),
    // (162,31): error CS0103: The name 'x12' does not exist in the current context
    //             case 1 when Dummy(x12):
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(162, 31),
    // (163,23): error CS0103: The name 'x12' does not exist in the current context
    //                 Dummy(x12, 1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(163, 23),
    // (175,43): error CS0136: A local or parameter named 'x13' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             case 1 when Dummy(true is var x13, x13):
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x13").WithArguments("x13").WithLocation(175, 43),
    // (187,35): error CS0136: A local or parameter named 'x14' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 Dummy(true is var x14, x14);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x14").WithArguments("x14").WithLocation(187, 35),
    // (198,43): error CS0128: A local variable named 'x15' is already defined in this scope
    //             case 1 when Dummy(true is var x15, x15):
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(198, 43),
    // (198,48): error CS0165: Use of unassigned local variable 'x15'
    //             case 1 when Dummy(true is var x15, x15):
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x15").WithArguments("x15").WithLocation(198, 48)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(6, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x1Decl[i], x1Ref[i*2], x1Ref[i * 2 + 1]);
            }

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(4, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[0], x4Ref[1]);
            VerifyNotAPatternLocal(model, x4Ref[2]);
            VerifyNotAPatternLocal(model, x4Ref[3]);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").ToArray();
            Assert.Equal(3, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref[0], x5Ref[1]);
            VerifyNotAPatternLocal(model, x5Ref[2]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").ToArray();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(3, x8Ref.Length);
            for (int i = 0; i < x8Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").Single();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(6, x9Ref.Length);
            VerifyNotAPatternLocal(model, x9Ref[0]);
            VerifyNotAPatternLocal(model, x9Ref[1]);
            VerifyNotAPatternLocal(model, x9Ref[2]);
            VerifyNotAPatternLocal(model, x9Ref[3]);
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref[4], x9Ref[5]);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(5, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyNotInScope(model, x11Ref[1]);
            VerifyNotInScope(model, x11Ref[2]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[3], x11Ref[4]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(5, x12Ref.Length);
            VerifyNotInScope(model, x12Ref[0]);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[1], x12Ref[2]);
            VerifyNotInScope(model, x12Ref[3]);
            VerifyNotInScope(model, x12Ref[4]);

            var x13Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x13").ToArray();
            var x13Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(5, x13Ref.Length);
            VerifyModelForDeclarationPattern(model, x13Decl[0], x13Ref[0], x13Ref[1], x13Ref[2]);
            VerifyModelForDeclarationPattern(model, x13Decl[1], x13Ref[3], x13Ref[4]);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(4, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref[0], x14Ref[1], x14Ref[3]);
            VerifyModelForDeclarationPattern(model, x14Decl[1], x14Ref[2]);

            var x15Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x15").ToArray();
            var x15Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x15").ToArray();
            Assert.Equal(2, x15Decl.Length);
            Assert.Equal(3, x15Ref.Length);
            for (int i = 0; i < x15Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x15Decl[0], x15Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x15Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_SwitchLabelPattern_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) { return true; }

    void Test1(object val)
    {
        switch (val)
        {
            case byte x1 when Dummy(x1):
                Dummy(x1);
                break;
            case int x1 when Dummy(x1):
                Dummy(x1);
                break;
            case long x1 when Dummy(x1):
                Dummy(x1);
                break;
        }
    }

    void Test2(object val)
    {
        switch (val)
        {
            case 0 when Dummy(x2):
            case int x2:
                Dummy(x2);
                break;
        }
    }

    void Test3(int x3, object val)
    {
        switch (val)
        {
            case int x3 when Dummy(x3):
                Dummy(x3);
                break;
        }
    }

    void Test4(object val)
    {
        var x4 = 11;
        switch (val)
        {
            case int x4 when Dummy(x4):
                Dummy(x4);
                break;
            case 1 when Dummy(x4):
                Dummy(x4);
                break;
        }
    }

    void Test5(object val)
    {
        switch (val)
        {
            case int x5 when Dummy(x5):
                Dummy(x5);
                break;
        }
        
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6(object val)
    //{
    //    let x6 = 11;
    //    switch (val)
    //    {
    //        case 0 when Dummy(x6):
    //            Dummy(x6);
    //            break;
    //        case int x6 when Dummy(x6):
    //            Dummy(x6);
    //            break;
    //    }
    //}

    //void Test7(object val)
    //{
    //    switch (val)
    //    {
    //        case int x7 when Dummy(x7):
    //            Dummy(x7);
    //            break;
    //    }
        
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8(object val)
    {
        switch (val)
        {
            case int x8 
                    when Dummy(x8, false is var x8, x8):
                Dummy(x8);
                break;
        }
    }

    void Test9(object val)
    {
        switch (val)
        {
            case 0 when Dummy(x9):
                int x9 = 9;
                Dummy(x9);
                break;
            case 2 when Dummy(x9 = 9):
                Dummy(x9);
                break;
            case int x9 when Dummy(x9):
                Dummy(x9);
                break;
        }
    }

    //void Test10(object val)
    //{
    //    switch (val)
    //    {
    //        case int x10 when Dummy(x10):
    //            Dummy(x10);
    //            break;
    //        case 0 when Dummy(x10):
    //            let x10 = 10;
    //            Dummy(x10);
    //            break;
    //        case 2 when Dummy(x10 = 10, x10):
    //            Dummy(x10);
    //            break;
    //    }
    //}

    void Test11(object val)
    {
        switch (x11 ? val : 0)
        {
            case 0 when Dummy(x11):
                Dummy(x11, 0);
                break;
            case int x11 when Dummy(x11):
                Dummy(x11, 1);
                break;
        }
    }

    void Test12(object val)
    {
        switch (x12 ? val : 0)
        {
            case int x12 when Dummy(x12):
                Dummy(x12, 0);
                break;
            case 1 when Dummy(x12):
                Dummy(x12, 1);
                break;
        }
    }

    void Test13()
    {
        switch (1 is var x13 ? x13 : 0)
        {
            case 0 when Dummy(x13):
                Dummy(x13);
                break;
            case int x13 when Dummy(x13):
                Dummy(x13);
                break;
        }
    }

    void Test14(object val)
    {
        switch (val)
        {
            case int x14 when Dummy(x14):
                Dummy(x14);
                Dummy(true is var x14, x14);
                Dummy(x14);
                break;
        }
    }

    void Test15(object val)
    {
        switch (val)
        {
            case int x15 when Dummy(x15):
            case long x15 when Dummy(x15):
                Dummy(x15);
                break;
        }
    }

    void Test16(object val)
    {
        switch (val)
        {
            case int x16 when Dummy(x16):
            case 1 when Dummy(true is var x16, x16):
                Dummy(x16);
                break;
        }
    }

    void Test17(object val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x17, x17):
            case int x17 when Dummy(x17):
                Dummy(x17);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
                // (30,31): error CS0841: Cannot use local variable 'x2' before it is declared
                //             case 0 when Dummy(x2):
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(30, 31),
                // (32,23): error CS0165: Use of unassigned local variable 'x2'
                //                 Dummy(x2);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(32, 23),
                // (41,22): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x3 when Dummy(x3):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(41, 22),
                // (52,22): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x4 when Dummy(x4):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(52, 22),
                // (65,22): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x5 when Dummy(x5):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(65, 22),
                // (106,49): error CS0128: A local variable named 'x8' is already defined in this scope
                //                     when Dummy(x8, false is var x8, x8):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(106, 49),
                // (116,31): error CS0841: Cannot use local variable 'x9' before it is declared
                //             case 0 when Dummy(x9):
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(116, 31),
                // (123,22): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x9 when Dummy(x9):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(123, 22),
                // (148,17): error CS0103: The name 'x11' does not exist in the current context
                //         switch (x11 ? val : 0)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(148, 17),
                // (150,31): error CS0103: The name 'x11' does not exist in the current context
                //             case 0 when Dummy(x11):
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(150, 31),
                // (151,23): error CS0103: The name 'x11' does not exist in the current context
                //                 Dummy(x11, 0);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(151, 23),
                // (161,17): error CS0103: The name 'x12' does not exist in the current context
                //         switch (x12 ? val : 0)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(161, 17),
                // (166,31): error CS0103: The name 'x12' does not exist in the current context
                //             case 1 when Dummy(x12):
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(166, 31),
                // (167,23): error CS0103: The name 'x12' does not exist in the current context
                //                 Dummy(x12, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(167, 23),
                // (179,22): error CS0136: A local or parameter named 'x13' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x13 when Dummy(x13):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x13").WithArguments("x13").WithLocation(179, 22),
                // (191,35): error CS0136: A local or parameter named 'x14' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                 Dummy(true is var x14, x14);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x14").WithArguments("x14").WithLocation(191, 35),
                // (202,23): error CS0128: A local variable named 'x15' is already defined in this scope
                //             case long x15 when Dummy(x15):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(202, 23),
                // (202,38): error CS0165: Use of unassigned local variable 'x15'
                //             case long x15 when Dummy(x15):
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x15").WithArguments("x15").WithLocation(202, 38),
                // (213,43): error CS0128: A local variable named 'x16' is already defined in this scope
                //             case 1 when Dummy(true is var x16, x16):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x16").WithArguments("x16").WithLocation(213, 43),
                // (213,48): error CS0165: Use of unassigned local variable 'x16'
                //             case 1 when Dummy(true is var x16, x16):
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x16").WithArguments("x16").WithLocation(213, 48),
                // (224,22): error CS0128: A local variable named 'x17' is already defined in this scope
                //             case int x17 when Dummy(x17):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x17").WithArguments("x17").WithLocation(224, 22),
                // (224,37): error CS0165: Use of unassigned local variable 'x17'
                //             case int x17 when Dummy(x17):
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x17").WithArguments("x17").WithLocation(224, 37)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(6, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x1Decl[i], x1Ref[i * 2], x1Ref[i * 2 + 1]);
            }

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(4, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[0], x4Ref[1]);
            VerifyNotAPatternLocal(model, x4Ref[2]);
            VerifyNotAPatternLocal(model, x4Ref[3]);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").ToArray();
            Assert.Equal(3, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref[0], x5Ref[1]);
            VerifyNotAPatternLocal(model, x5Ref[2]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").ToArray();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(3, x8Ref.Length);
            for (int i = 0; i < x8Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").Single();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(6, x9Ref.Length);
            VerifyNotAPatternLocal(model, x9Ref[0]);
            VerifyNotAPatternLocal(model, x9Ref[1]);
            VerifyNotAPatternLocal(model, x9Ref[2]);
            VerifyNotAPatternLocal(model, x9Ref[3]);
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref[4], x9Ref[5]);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(5, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyNotInScope(model, x11Ref[1]);
            VerifyNotInScope(model, x11Ref[2]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[3], x11Ref[4]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(5, x12Ref.Length);
            VerifyNotInScope(model, x12Ref[0]);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[1], x12Ref[2]);
            VerifyNotInScope(model, x12Ref[3]);
            VerifyNotInScope(model, x12Ref[4]);

            var x13Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x13").ToArray();
            var x13Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(5, x13Ref.Length);
            VerifyModelForDeclarationPattern(model, x13Decl[0], x13Ref[0], x13Ref[1], x13Ref[2]);
            VerifyModelForDeclarationPattern(model, x13Decl[1], x13Ref[3], x13Ref[4]);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(4, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref[0], x14Ref[1], x14Ref[3]);
            VerifyModelForDeclarationPattern(model, x14Decl[1], x14Ref[2]);

            var x15Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x15").ToArray();
            var x15Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x15").ToArray();
            Assert.Equal(2, x15Decl.Length);
            Assert.Equal(3, x15Ref.Length);
            for (int i = 0; i < x15Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x15Decl[0], x15Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x15Decl[1]);

            var x16Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x16").ToArray();
            var x16Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x16").ToArray();
            Assert.Equal(2, x16Decl.Length);
            Assert.Equal(3, x16Ref.Length);
            for (int i = 0; i < x16Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x16Decl[0], x16Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x16Decl[1]);

            var x17Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x17").ToArray();
            var x17Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x17").ToArray();
            Assert.Equal(2, x17Decl.Length);
            Assert.Equal(3, x17Ref.Length);
            for (int i = 0; i < x17Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x17Decl[0], x17Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x17Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Switch_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        switch (1 is var x1 ? x1 : 0)
        {
            case 0:
                Dummy(x1, 0);
                break;
        }

        Dummy(x1, 1);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        switch (4 is var x4 ? x4 : 0)
        {
            case 4:
                Dummy(x4);
                break;
        }
    }

    void Test5(int x5)
    {
        switch (5 is var x5 ? x5 : 0)
        {
            case 5:
                Dummy(x5);
                break;
        }
    }

    void Test6()
    {
        switch (x6 + 6 is var x6 ? x6 : 0)
        {
            case 6:
                Dummy(x6);
                break;
        }
    }

    void Test7()
    {
        switch (7 is var x7 ? x7 : 0)
        {
            case 7:
                var x7 = 12;
                Dummy(x7);
                break;
        }
    }

    void Test9()
    {
        switch (9 is var x9 ? x9 : 0)
        {
            case 9:
                Dummy(x9, 0);
                switch (9 is var x9 ? x9 : 0)
                {
                    case 9:
                        Dummy(x9, 1);
                        break;
                }
                break;
        }

    }

    void Test10()
    {
        switch (y10 + 10 is var x10 ? x10 : 0)
        {
            case 0 when y10:
                break;
            case y10:
                var y10 = 12;
                Dummy(y10);
                break;
        }
    }

    //void Test11()
    //{
    //    switch (y11 + 11 is var x11 ? x11 : 0)
    //    {
    //        case 0 when y11 > 0:
    //            break;
    //        case y11:
    //            let y11 = 12;
    //            Dummy(y11);
    //            break;
    //    }
    //}

    void Test14()
    {
        switch (Dummy(1 is var x14, 
                  2 is var x14, 
                  x14) ? 1 : 0)
        {
            case 0:
                Dummy(x14);
                break;
        }
    }

    void Test15(int val)
    {
        switch (val)
        {
            case 0 when y15 > 0:
                break;
            case y15: 
                var y15 = 15;
                Dummy(y15);
                break;
        }
    }

    //void Test16(int val)
    //{
    //    switch (val)
    //    {
    //        case 0 when y16 > 0:
    //            break;
    //        case y16: 
    //            let y16 = 16;
    //            Dummy(y16);
    //            break;
    //    }
    //}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (19,15): error CS0103: The name 'x1' does not exist in the current context
                //         Dummy(x1, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(19, 15),
                // (27,26): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         switch (4 is var x4 ? x4 : 0)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(27, 26),
                // (37,26): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         switch (5 is var x5 ? x5 : 0)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(37, 26),
                // (47,17): error CS0841: Cannot use local variable 'x6' before it is declared
                //         switch (x6 + 6 is var x6 ? x6 : 0)
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(47, 17),
                // (60,21): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                 var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(60, 21),
                // (72,34): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                 switch (9 is var x9 ? x9 : 0)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(72, 34),
                // (85,17): error CS0103: The name 'y10' does not exist in the current context
                //         switch (y10 + 10 is var x10 ? x10 : 0)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 17),
                // (87,25): error CS0841: Cannot use local variable 'y10' before it is declared
                //             case 0 when y10:
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y10").WithArguments("y10").WithLocation(87, 25),
                // (89,18): error CS0841: Cannot use local variable 'y10' before it is declared
                //             case y10:
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y10").WithArguments("y10").WithLocation(89, 18),
                // (112,28): error CS0128: A local variable named 'x14' is already defined in this scope
                //                   2 is var x14, 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(112, 28),
                // (125,25): error CS0841: Cannot use local variable 'y15' before it is declared
                //             case 0 when y15 > 0:
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y15").WithArguments("y15").WithLocation(125, 25),
                // (127,18): error CS0841: Cannot use local variable 'y15' before it is declared
                //             case y15: 
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y15").WithArguments("y15").WithLocation(127, 18)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref[0], x1Ref[1]);
            VerifyNotInScope(model, x1Ref[2]);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);
            VerifyNotAPatternLocal(model, x4Ref[0]);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(3, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(4, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);
            VerifyNotAPatternLocal(model, y10Ref[2]);
            VerifyNotAPatternLocal(model, y10Ref[3]);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);

            var y15Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y15").ToArray();
            Assert.Equal(3, y15Ref.Length);
            VerifyNotAPatternLocal(model, y15Ref[0]);
            VerifyNotAPatternLocal(model, y15Ref[1]);
            VerifyNotAPatternLocal(model, y15Ref[2]);
        }

        [Fact]
        public void Switch_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        Test1(0);
        Test1(1);
    }

    static bool Dummy1(bool val, params object[] x) {return val;}
    static T Dummy2<T>(T val, params object[] x) {return val;}

    static void Test1(int val)
    {
        switch (Dummy2(val, ""Test1 {0}"" is var x1))
        {
            case 0 when Dummy1(true, ""case 0"" is var y1):
                System.Console.WriteLine(x1, y1);
                break;
            case int z1:
                System.Console.WriteLine(x1, z1);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"Test1 case 0
Test1 1");
        }

        [Fact]
        public void ScopeOfPatternVariables_Using_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (Dummy(true is var x1, x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (Dummy(true is var x2, x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        using (Dummy(true is var x4, x4))
            Dummy(x4);
    }

    void Test6()
    {
        using (Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        using (Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        using (Dummy(true is var x8, x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        using (Dummy(true is var x9, x9))
        {   
            Dummy(x9);
            using (Dummy(true is var x9, x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        using (Dummy(y10 is var x10, x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    using (Dummy(y11 is var x11, x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        using (Dummy(y12 is var x12, x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    using (Dummy(y13 is var x13, x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        using (Dummy(1 is var x14, 
                     2 is var x14, 
                     x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,34): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         using (Dummy(true is var x4, x4))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 34),
    // (35,22): error CS0841: Cannot use local variable 'x6' before it is declared
    //         using (Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 22),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,38): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             using (Dummy(true is var x9, x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 38),
    // (68,22): error CS0103: The name 'y10' does not exist in the current context
    //         using (Dummy(y10 is var x10, x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 22),
    // (86,22): error CS0103: The name 'y12' does not exist in the current context
    //         using (Dummy(y12 is var x12, x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 22),
    // (99,31): error CS0128: A local variable named 'x14' is already defined in this scope
    //                      2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 31)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var x10Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x10").Single();
            var x10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Using_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (var d = Dummy(true is var x1, x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (var d = Dummy(true is var x2, x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        using (var d = Dummy(true is var x4, x4))
            Dummy(x4);
    }

    void Test6()
    {
        using (var d = Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        using (var d = Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        using (var d = Dummy(true is var x8, x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        using (var d = Dummy(true is var x9, x9))
        {   
            Dummy(x9);
            using (var e = Dummy(true is var x9, x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        using (var d = Dummy(y10 is var x10, x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    using (var d = Dummy(y11 is var x11, x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        using (var d = Dummy(y12 is var x12, x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    using (var d = Dummy(y13 is var x13, x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        using (var d = Dummy(1 is var x14, 
                             2 is var x14, 
                             x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,42): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         using (var d = Dummy(true is var x4, x4))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 42),
    // (35,30): error CS0841: Cannot use local variable 'x6' before it is declared
    //         using (var d = Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 30),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,46): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             using (var e = Dummy(true is var x9, x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 46),
    // (68,30): error CS0103: The name 'y10' does not exist in the current context
    //         using (var d = Dummy(y10 is var x10, x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 30),
    // (86,30): error CS0103: The name 'y12' does not exist in the current context
    //         using (var d = Dummy(y12 is var x12, x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 30),
    // (99,39): error CS0128: A local variable named 'x14' is already defined in this scope
    //                              2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 39)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var x10Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x10").Single();
            var x10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Using_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (System.IDisposable d = Dummy(true is var x1, x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (System.IDisposable d = Dummy(true is var x2, x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        using (System.IDisposable d = Dummy(true is var x4, x4))
            Dummy(x4);
    }

    void Test6()
    {
        using (System.IDisposable d = Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        using (System.IDisposable d = Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        using (System.IDisposable d = Dummy(true is var x8, x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        using (System.IDisposable d = Dummy(true is var x9, x9))
        {   
            Dummy(x9);
            using (System.IDisposable c = Dummy(true is var x9, x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        using (System.IDisposable d = Dummy(y10 is var x10, x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    using (System.IDisposable d = Dummy(y11 is var x11, x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        using (System.IDisposable d = Dummy(y12 is var x12, x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    using (System.IDisposable d = Dummy(y13 is var x13, x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        using (System.IDisposable d = Dummy(1 is var x14, 
                                            2 is var x14, 
                                            x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,57): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         using (System.IDisposable d = Dummy(true is var x4, x4))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 57),
    // (35,45): error CS0841: Cannot use local variable 'x6' before it is declared
    //         using (System.IDisposable d = Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 45),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,61): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             using (System.IDisposable c = Dummy(true is var x9, x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 61),
    // (68,45): error CS0103: The name 'y10' does not exist in the current context
    //         using (System.IDisposable d = Dummy(y10 is var x10, x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 45),
    // (86,45): error CS0103: The name 'y12' does not exist in the current context
    //         using (System.IDisposable d = Dummy(y12 is var x12, x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 45),
    // (99,54): error CS0128: A local variable named 'x14' is already defined in this scope
    //                                             2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 54)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var x10Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x10").Single();
            var x10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Using_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (var x1 = Dummy(true is var x1, x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (System.IDisposable x2 = Dummy(true is var x2, x2))
        {
            Dummy(x2);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (12,43): error CS0128: A local variable named 'x1' is already defined in this scope
    //         using (var x1 = Dummy(true is var x1, x1))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(12, 43),
    // (12,47): error CS0841: Cannot use local variable 'x1' before it is declared
    //         using (var x1 = Dummy(true is var x1, x1))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(12, 47),
    // (20,58): error CS0128: A local variable named 'x2' is already defined in this scope
    //         using (System.IDisposable x2 = Dummy(true is var x2, x2))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(20, 58)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl);
            VerifyNotAPatternLocal(model, x1Ref[0]);
            VerifyNotAPatternLocal(model, x1Ref[1]);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl);
            VerifyNotAPatternLocal(model, x2Ref[0]);
            VerifyNotAPatternLocal(model, x2Ref[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Using_05()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (System.IDisposable d = Dummy(true is var x1, x1), 
                                  x1 = Dummy(x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (System.IDisposable d1 = Dummy(true is var x2, x2), 
                                  d2 = Dummy(true is var x2, x2))
        {
            Dummy(x2);
        }
    }

    void Test3()
    {
        using (System.IDisposable d1 = Dummy(true is var x3, x3), 
                                  d2 = Dummy(x3))
        {
            Dummy(x3);
        }
    }

    void Test4()
    {
        using (System.IDisposable d1 = Dummy(x4), 
                                  d2 = Dummy(true is var x4, x4))
        {
            Dummy(x4);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (13,35): error CS0128: A local variable named 'x1' is already defined in this scope
    //                                   x1 = Dummy(x1))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(13, 35),
    // (22,58): error CS0128: A local variable named 'x2' is already defined in this scope
    //                                   d2 = Dummy(true is var x2, x2))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(22, 58),
    // (39,46): error CS0841: Cannot use local variable 'x4' before it is declared
    //         using (System.IDisposable d1 = Dummy(x4), 
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(39, 46)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").ToArray();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Decl.Length);
            Assert.Equal(3, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl[0], x2Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl[1]);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").ToArray();
            Assert.Equal(3, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);
        }

        [Fact]
        public void Using_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        using (System.IDisposable d1 = Dummy(new C(""a""), new C(""b"") is var x1),
                                  d2 = Dummy(new C(""c""), new C(""d"") is var x2))
        {
            System.Console.WriteLine(d1);
            System.Console.WriteLine(x1);
            System.Console.WriteLine(d2);
            System.Console.WriteLine(x2);
        }

        using (Dummy(new C(""e""), new C(""f"") is var x1))
        {
            System.Console.WriteLine(x1);
        }
    }

    static System.IDisposable Dummy(System.IDisposable x, params object[] y) {return x;}
}

class C : System.IDisposable
{
    private readonly string _val;

    public C(string val)
    {
        _val = val;
    }

    public void Dispose()
    {
        System.Console.WriteLine(""Disposing {0}"", _val);
    }

    public override string ToString()
    {
        return _val;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"a
b
c
d
Disposing c
Disposing a
f
Disposing e");
        }

        [Fact]
        public void ScopeOfPatternVariables_LocalDeclarationStmt_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        var d = Dummy(true is var x1, x1);
    }
    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        var d = Dummy(true is var x4, x4);
    }

    void Test6()
    {
        var d = Dummy(x6 && true is var x6);
    }

    void Test8()
    {
        var d = Dummy(true is var x8, x8);
        System.Console.WriteLine(x8);
    }

    void Test14()
    {
        var d = Dummy(1 is var x14, 
                      2 is var x14, 
                      x14);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (19,35): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         var d = Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(19, 35),
    // (24,23): error CS0841: Cannot use local variable 'x6' before it is declared
    //         var d = Dummy(x6 && true is var x6);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(24, 23),
    // (30,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(30, 34),
    // (36,32): error CS0128: A local variable named 'x14' is already defined in this scope
    //                       2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(36, 32)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").Single();
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0]);
            VerifyNotInScope(model, x8Ref[1]);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").Single();
            Assert.Equal(2, x14Decl.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }
        
        [Fact]
        public void ScopeOfPatternVariables_LocalDeclarationStmt_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        object d = Dummy(true is var x1, x1);
    }
    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        object d = Dummy(true is var x4, x4);
    }

    void Test6()
    {
        object d = Dummy(x6 && true is var x6);
    }

    void Test8()
    {
        object d = Dummy(true is var x8, x8);
        System.Console.WriteLine(x8);
    }

    void Test14()
    {
        object d = Dummy(1 is var x14, 
                         2 is var x14, 
                         x14);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (19,38): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         object d = Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(19, 38),
    // (24,26): error CS0841: Cannot use local variable 'x6' before it is declared
    //         object d = Dummy(x6 && true is var x6);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(24, 26),
    // (30,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(30, 34),
    // (36,35): error CS0128: A local variable named 'x14' is already defined in this scope
    //                          2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(36, 35)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").Single();
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0]);
            VerifyNotInScope(model, x8Ref[1]);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").Single();
            Assert.Equal(2, x14Decl.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_LocalDeclarationStmt_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        var x1 = 
                 Dummy(true is var x1, x1);
        Dummy(x1);
    }

    void Test2()
    {
        object x2 = 
                    Dummy(true is var x2, x2);
        Dummy(x2);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (13,36): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                  Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(13, 36),
    // (20,39): error CS0136: A local or parameter named 'x2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                     Dummy(true is var x2, x2);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x2").WithArguments("x2").WithLocation(20, 39)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref[0]);
            VerifyNotAPatternLocal(model, x1Ref[1]);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref[0]);
            VerifyNotAPatternLocal(model, x2Ref[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_LocalDeclarationStmt_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

   object Dummy(params object[] x) {return null;}

    void Test1()
    {
        object d = Dummy(true is var x1, x1), 
               x1 = Dummy(x1);
        Dummy(x1);
    }

    void Test2()
    {
        object d1 = Dummy(true is var x2, x2), 
               d2 = Dummy(true is var x2, x2);
    }

    void Test3()
    {
        object d1 = Dummy(true is var x3, x3), 
               d2 = Dummy(x3);
    }

    void Test4()
    {
        object d1 = Dummy(x4), 
               d2 = Dummy(true is var x4, x4);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (12,38): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         object d = Dummy(true is var x1, x1), 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(12, 38),
    // (20,39): error CS0128: A local variable named 'x2' is already defined in this scope
    //                d2 = Dummy(true is var x2, x2);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(20, 39),
    // (31,27): error CS0841: Cannot use local variable 'x4' before it is declared
    //         object d1 = Dummy(x4), 
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(31, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref[0], x1Ref[1]);
            VerifyNotAPatternLocal(model, x1Ref[2]);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").ToArray();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Decl.Length);
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl[0], x2Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl[1]);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);
        }

        [Fact]
        public void LocalDeclarationStmt_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        object d1 = Dummy(new C(""a""), new C(""b"") is var x1, x1),
               d2 = Dummy(new C(""c""), new C(""d"") is var x2, x2);
        System.Console.WriteLine(d1);
        System.Console.WriteLine(d2);
    }

    static object Dummy(object x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }
}

class C
{
    private readonly string _val;

    public C(string val)
    {
        _val = val;
    }

    public override string ToString()
    {
        return _val;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"b
d
a
c");
        }

        [Fact]
        public void ScopeOfPatternVariables_While_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        while (true is var x1 && x1)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        while (true is var x2 && x2)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        while (true is var x4 && x4)
            Dummy(x4);
    }

    void Test6()
    {
        while (x6 && true is var x6)
            Dummy(x6);
    }

    void Test7()
    {
        while (true is var x7 && x7)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        while (true is var x8 && x8)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        while (true is var x9 && x9)
        {   
            Dummy(x9);
            while (true is var x9 && x9) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        while (y10 is var x10)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    while (y11 is var x11)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        while (y12 is var x12)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    while (y13 is var x13)
    //        let y13 = 12;
    //}

    void Test14()
    {
        while (Dummy(1 is var x14, 
                     2 is var x14, 
                     x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,28): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         while (true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 28),
    // (35,16): error CS0841: Cannot use local variable 'x6' before it is declared
    //         while (x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 16),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,32): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             while (true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 32),
    // (68,16): error CS0103: The name 'y10' does not exist in the current context
    //         while (y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 16),
    // (86,16): error CS0103: The name 'y12' does not exist in the current context
    //         while (y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 16),
    // (99,31): error CS0128: A local variable named 'x14' is already defined in this scope
    //                      2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 31)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void While_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f = true;

        while (Dummy(f, (f ? 1 : 2) is var x1, x1))
        {
            System.Console.WriteLine(x1);
            f = false;
        }
    }

    static bool Dummy(bool x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"1
1
2");
        }

        [Fact]
        public void ScopeOfPatternVariables_Do_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        do
        {
            Dummy(x1);
        }
        while (true is var x1 && x1);
    }

    void Test2()
    {
        do
            Dummy(x2);
        while (true is var x2 && x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        do
            Dummy(x4);
        while (true is var x4 && x4);
    }

    void Test6()
    {
        do
            Dummy(x6);
        while (x6 && true is var x6);
    }

    void Test7()
    {
        do
        {
            var x7 = 12;
            Dummy(x7);
        }
        while (true is var x7 && x7);
    }

    void Test8()
    {
        do
            Dummy(x8);
        while (true is var x8 && x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        do
        {   
            Dummy(x9);
            do
                Dummy(x9);
            while (true is var x9 && x9); // 2
        }
        while (true is var x9 && x9);
    }

    void Test10()
    {
        do
        {   
            var y10 = 12;
            Dummy(y10);
        }
        while (y10 is var x10);
    }

    //void Test11()
    //{
    //    do
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //    while (y11 is var x11);
    //}

    void Test12()
    {
        do
            var y12 = 12;
        while (y12 is var x12);
    }

    //void Test13()
    //{
    //    do
    //        let y13 = 12;
    //    while (y13 is var x13);
    //}

    void Test14()
    {
        do
        {
            Dummy(x14);
        }
        while (Dummy(1 is var x14, 
                     2 is var x14, 
                     x14));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (97,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(97, 13),
    // (14,19): error CS0841: Cannot use local variable 'x1' before it is declared
    //             Dummy(x1);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(14, 19),
    // (22,19): error CS0841: Cannot use local variable 'x2' before it is declared
    //             Dummy(x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(22, 19),
    // (33,28): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         while (true is var x4 && x4);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(33, 28),
    // (32,19): error CS0841: Cannot use local variable 'x4' before it is declared
    //             Dummy(x4);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(32, 19),
    // (40,16): error CS0841: Cannot use local variable 'x6' before it is declared
    //         while (x6 && true is var x6);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(40, 16),
    // (39,19): error CS0841: Cannot use local variable 'x6' before it is declared
    //             Dummy(x6);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(39, 19),
    // (47,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(47, 17),
    // (56,19): error CS0841: Cannot use local variable 'x8' before it is declared
    //             Dummy(x8);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x8").WithArguments("x8").WithLocation(56, 19),
    // (59,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(59, 34),
    // (66,19): error CS0841: Cannot use local variable 'x9' before it is declared
    //             Dummy(x9);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(66, 19),
    // (69,32): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             while (true is var x9 && x9); // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(69, 32),
    // (68,23): error CS0841: Cannot use local variable 'x9' before it is declared
    //                 Dummy(x9);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(68, 23),
    // (81,16): error CS0103: The name 'y10' does not exist in the current context
    //         while (y10 is var x10);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(81, 16),
    // (98,16): error CS0103: The name 'y12' does not exist in the current context
    //         while (y12 is var x12);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(98, 16),
    // (115,31): error CS0128: A local variable named 'x14' is already defined in this scope
    //                      2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(115, 31),
    // (112,19): error CS0841: Cannot use local variable 'x14' before it is declared
    //             Dummy(x14);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x14").WithArguments("x14").WithLocation(112, 19)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[1]);
            VerifyNotAPatternLocal(model, x7Ref[0]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[0], x9Ref[3]);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[1], x9Ref[2]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[1]);
            VerifyNotAPatternLocal(model, y10Ref[0]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void Do_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f;

        do
        {
            f = false;
        }
        while (Dummy(f, (f ? 1 : 2) is var x1, x1));
    }

    static bool Dummy(bool x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:@"2");
        }

        [Fact]
        public void ScopeOfPatternVariables_For_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (
             Dummy(true is var x1 && x1)
             ;;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (
             Dummy(true is var x2 && x2)
             ;;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (
             Dummy(true is var x4 && x4)
             ;;)
            Dummy(x4);
    }

    void Test6()
    {
        for (
             Dummy(x6 && true is var x6)
             ;;)
            Dummy(x6);
    }

    void Test7()
    {
        for (
             Dummy(true is var x7 && x7)
             ;;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (
             Dummy(true is var x8 && x8)
             ;;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (
             Dummy(true is var x9 && x9)
             ;;)
        {   
            Dummy(x9);
            for (
                 Dummy(true is var x9 && x9) // 2
                 ;;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (
             Dummy(y10 is var x10)
             ;;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (
    //         Dummy(y11 is var x11)
    //         ;;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (
             Dummy(y12 is var x12)
             ;;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (
    //         Dummy(y13 is var x13)
    //         ;;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (
             Dummy(1 is var x14, 
                   2 is var x14, 
                   x14)
             ;;)
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
    // (34,32): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 32),
    // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
    //              Dummy(x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
    // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
    // (65,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
    // (65,9): warning CS0162: Unreachable code detected
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
    // (76,36): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                  Dummy(true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 36),
    // (85,20): error CS0103: The name 'y10' does not exist in the current context
    //              Dummy(y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 20),
    // (107,20): error CS0103: The name 'y12' does not exist in the current context
    //              Dummy(y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 20),
    // (124,29): error CS0128: A local variable named 'x14' is already defined in this scope
    //                    2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 29)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_For_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (;
             Dummy(true is var x1 && x1)
             ;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (;
             Dummy(true is var x2 && x2)
             ;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (;
             Dummy(true is var x4 && x4)
             ;)
            Dummy(x4);
    }

    void Test6()
    {
        for (;
             Dummy(x6 && true is var x6)
             ;)
            Dummy(x6);
    }

    void Test7()
    {
        for (;
             Dummy(true is var x7 && x7)
             ;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (;
             Dummy(true is var x8 && x8)
             ;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (;
             Dummy(true is var x9 && x9)
             ;)
        {   
            Dummy(x9);
            for (;
                 Dummy(true is var x9 && x9) // 2
                 ;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (;
             Dummy(y10 is var x10)
             ;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (;
    //         Dummy(y11 is var x11)
    //         ;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (;
             Dummy(y12 is var x12)
             ;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (;
    //         Dummy(y13 is var x13)
    //         ;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (;
             Dummy(1 is var x14, 
                   2 is var x14, 
                   x14)
             ;)
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
    // (34,32): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 32),
    // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
    //              Dummy(x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
    // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
    // (65,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
    // (76,36): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                  Dummy(true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 36),
    // (85,20): error CS0103: The name 'y10' does not exist in the current context
    //              Dummy(y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 20),
    // (107,20): error CS0103: The name 'y12' does not exist in the current context
    //              Dummy(y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 20),
    // (124,29): error CS0128: A local variable named 'x14' is already defined in this scope
    //                    2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 29)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_For_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (;;
             Dummy(true is var x1 && x1)
             )
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (;;
             Dummy(true is var x2 && x2)
             )
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (;;
             Dummy(true is var x4 && x4)
             )
            Dummy(x4);
    }

    void Test6()
    {
        for (;;
             Dummy(x6 && true is var x6)
             )
            Dummy(x6);
    }

    void Test7()
    {
        for (;;
             Dummy(true is var x7 && x7)
             )
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (;;
             Dummy(true is var x8 && x8)
             )
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (;;
             Dummy(true is var x9 && x9)
             )
        {   
            Dummy(x9);
            for (;;
                 Dummy(true is var x9 && x9) // 2
                 )
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (;;
             Dummy(y10 is var x10)
             )
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (;;
    //         Dummy(y11 is var x11)
    //         )
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (;;
             Dummy(y12 is var x12)
             )
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (;;
    //         Dummy(y13 is var x13)
    //         )
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (;;
             Dummy(1 is var x14, 
                   2 is var x14, 
                   x14)
             )
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
    // (34,32): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 32),
    // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
    //              Dummy(x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
    // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
    // (65,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
    // (65,9): warning CS0162: Unreachable code detected
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
    // (76,36): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                  Dummy(true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 36),
    // (85,20): error CS0103: The name 'y10' does not exist in the current context
    //              Dummy(y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 20),
    // (107,20): error CS0103: The name 'y12' does not exist in the current context
    //              Dummy(y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 20),
    // (124,29): error CS0128: A local variable named 'x14' is already defined in this scope
    //                    2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 29),
    // (16,19): error CS0165: Use of unassigned local variable 'x1'
    //             Dummy(x1);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(16, 19),
    // (25,19): error CS0165: Use of unassigned local variable 'x2'
    //             Dummy(x2);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(25, 19),
    // (36,19): error CS0165: Use of unassigned local variable 'x4'
    //             Dummy(x4);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x4").WithArguments("x4").WithLocation(36, 19),
    // (44,19): error CS0165: Use of unassigned local variable 'x6'
    //             Dummy(x6);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x6").WithArguments("x6").WithLocation(44, 19),
    // (63,19): error CS0165: Use of unassigned local variable 'x8'
    //             Dummy(x8);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x8").WithArguments("x8").WithLocation(63, 19),
    // (71,14): warning CS0162: Unreachable code detected
    //              Dummy(true is var x9 && x9)
    Diagnostic(ErrorCode.WRN_UnreachableCode, "Dummy").WithLocation(71, 14),
    // (74,19): error CS0165: Use of unassigned local variable 'x9'
    //             Dummy(x9);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x9").WithArguments("x9").WithLocation(74, 19),
    // (78,23): error CS0165: Use of unassigned local variable 'x9'
    //                 Dummy(x9);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x9").WithArguments("x9").WithLocation(78, 23),
    // (128,19): error CS0165: Use of unassigned local variable 'x14'
    //             Dummy(x14);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x14").WithArguments("x14").WithLocation(128, 19)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_For_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (var b =
             Dummy(true is var x1 && x1)
             ;;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (var b =
             Dummy(true is var x2 && x2)
             ;;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (var b =
             Dummy(true is var x4 && x4)
             ;;)
            Dummy(x4);
    }

    void Test6()
    {
        for (var b =
             Dummy(x6 && true is var x6)
             ;;)
            Dummy(x6);
    }

    void Test7()
    {
        for (var b =
             Dummy(true is var x7 && x7)
             ;;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (var b =
             Dummy(true is var x8 && x8)
             ;;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (var b1 =
             Dummy(true is var x9 && x9)
             ;;)
        {   
            Dummy(x9);
            for (var b2 =
                 Dummy(true is var x9 && x9) // 2
                 ;;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (var b =
             Dummy(y10 is var x10)
             ;;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (var b =
    //         Dummy(y11 is var x11)
    //         ;;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (var b =
             Dummy(y12 is var x12)
             ;;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (var b =
    //         Dummy(y13 is var x13)
    //         ;;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (var b =
             Dummy(1 is var x14, 
                   2 is var x14, 
                   x14)
             ;;)
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
    // (34,32): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 32),
    // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
    //              Dummy(x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
    // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
    // (65,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
    // (65,9): warning CS0162: Unreachable code detected
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
    // (76,36): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                  Dummy(true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 36),
    // (85,20): error CS0103: The name 'y10' does not exist in the current context
    //              Dummy(y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 20),
    // (107,20): error CS0103: The name 'y12' does not exist in the current context
    //              Dummy(y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 20),
    // (124,29): error CS0128: A local variable named 'x14' is already defined in this scope
    //                    2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 29)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_For_05()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (bool b =
             Dummy(true is var x1 && x1)
             ;;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (bool b =
             Dummy(true is var x2 && x2)
             ;;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (bool b =
             Dummy(true is var x4 && x4)
             ;;)
            Dummy(x4);
    }

    void Test6()
    {
        for (bool b =
             Dummy(x6 && true is var x6)
             ;;)
            Dummy(x6);
    }

    void Test7()
    {
        for (bool b =
             Dummy(true is var x7 && x7)
             ;;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (bool b =
             Dummy(true is var x8 && x8)
             ;;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (bool b1 =
             Dummy(true is var x9 && x9)
             ;;)
        {   
            Dummy(x9);
            for (bool b2 =
                 Dummy(true is var x9 && x9) // 2
                 ;;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (bool b =
             Dummy(y10 is var x10)
             ;;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (bool b =
    //         Dummy(y11 is var x11)
    //         ;;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (bool b =
             Dummy(y12 is var x12)
             ;;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (bool b =
    //         Dummy(y13 is var x13)
    //         ;;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (bool b =
             Dummy(1 is var x14, 
                   2 is var x14, 
                   x14)
             ;;)
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
    // (34,32): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 32),
    // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
    //              Dummy(x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
    // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
    // (65,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
    // (65,9): warning CS0162: Unreachable code detected
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
    // (76,36): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                  Dummy(true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 36),
    // (85,20): error CS0103: The name 'y10' does not exist in the current context
    //              Dummy(y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 20),
    // (107,20): error CS0103: The name 'y12' does not exist in the current context
    //              Dummy(y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 20),
    // (124,29): error CS0128: A local variable named 'x14' is already defined in this scope
    //                    2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 29)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_For_06()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (var x1 =
             Dummy(true is var x1 && x1)
             ;;)
        {}
    }

    void Test2()
    {
        for (var x2 = true;
             Dummy(true is var x2 && x2)
             ;)
        {}
    }

    void Test3()
    {
        for (var x3 = true;;
             Dummy(true is var x3 && x3)
             )
        {}
    }

    void Test4()
    {
        for (bool x4 =
             Dummy(true is var x4 && x4)
             ;;)
        {}
    }

    void Test5()
    {
        for (bool x5 = true;
             Dummy(true is var x5 && x5)
             ;)
        {}
    }

    void Test6()
    {
        for (bool x6 = true;;
             Dummy(true is var x6 && x6)
             )
        {}
    }

    void Test7()
    {
        for (bool x7 = true, b =
             Dummy(true is var x7 && x7)
             ;;)
        {}
    }

    void Test8()
    {
        for (bool b1 = Dummy(true is var x8 && x8), 
             b2 = Dummy(true is var x8 && x8);
             Dummy(true is var x8 && x8);
             Dummy(true is var x8 && x8))
        {}
    }

    void Test9()
    {
        for (bool b = x9, 
             b2 = Dummy(true is var x9 && x9);
             Dummy(true is var x9 && x9);
             Dummy(true is var x9 && x9))
        {}
    }

    void Test10()
    {
        for (var b = x10;
             Dummy(true is var x10 && x10) &&
             Dummy(true is var x10 && x10);
             Dummy(true is var x10 && x10))
        {}
    }

    void Test11()
    {
        for (bool b = x11;
             Dummy(true is var x11 && x11) &&
             Dummy(true is var x11 && x11);
             Dummy(true is var x11 && x11))
        {}
    }

    void Test12()
    {
        for (Dummy(x12);
             Dummy(x12) &&
             Dummy(true is var x12 && x12);
             Dummy(true is var x12 && x12))
        {}
    }

    void Test13()
    {
        for (var b = x13;
             Dummy(x13);
             Dummy(true is var x13 && x13),
             Dummy(true is var x13 && x13))
        {}
    }

    void Test14()
    {
        for (bool b = x14;
             Dummy(x14);
             Dummy(true is var x14 && x14),
             Dummy(true is var x14 && x14))
        {}
    }

    void Test15()
    {
        for (Dummy(x15);
             Dummy(x15);
             Dummy(x15),
             Dummy(true is var x15 && x15))
        {}
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (13,32): error CS0128: A local variable named 'x1' is already defined in this scope
    //              Dummy(true is var x1 && x1)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(13, 32),
    // (13,38): error CS0841: Cannot use local variable 'x1' before it is declared
    //              Dummy(true is var x1 && x1)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(13, 38),
    // (13,38): error CS0165: Use of unassigned local variable 'x1'
    //              Dummy(true is var x1 && x1)
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(13, 38),
    // (21,32): error CS0128: A local variable named 'x2' is already defined in this scope
    //              Dummy(true is var x2 && x2)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(21, 32),
    // (29,32): error CS0128: A local variable named 'x3' is already defined in this scope
    //              Dummy(true is var x3 && x3)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(29, 32),
    // (37,32): error CS0128: A local variable named 'x4' is already defined in this scope
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(37, 32),
    // (37,38): error CS0165: Use of unassigned local variable 'x4'
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x4").WithArguments("x4").WithLocation(37, 38),
    // (45,32): error CS0128: A local variable named 'x5' is already defined in this scope
    //              Dummy(true is var x5 && x5)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(45, 32),
    // (53,32): error CS0128: A local variable named 'x6' is already defined in this scope
    //              Dummy(true is var x6 && x6)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(53, 32),
    // (61,32): error CS0128: A local variable named 'x7' is already defined in this scope
    //              Dummy(true is var x7 && x7)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x7").WithArguments("x7").WithLocation(61, 32),
    // (69,37): error CS0128: A local variable named 'x8' is already defined in this scope
    //              b2 = Dummy(true is var x8 && x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(69, 37),
    // (70,32): error CS0128: A local variable named 'x8' is already defined in this scope
    //              Dummy(true is var x8 && x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(70, 32),
    // (71,32): error CS0128: A local variable named 'x8' is already defined in this scope
    //              Dummy(true is var x8 && x8))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(71, 32),
    // (77,23): error CS0841: Cannot use local variable 'x9' before it is declared
    //         for (bool b = x9, 
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(77, 23),
    // (79,32): error CS0128: A local variable named 'x9' is already defined in this scope
    //              Dummy(true is var x9 && x9);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x9").WithArguments("x9").WithLocation(79, 32),
    // (80,32): error CS0128: A local variable named 'x9' is already defined in this scope
    //              Dummy(true is var x9 && x9))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x9").WithArguments("x9").WithLocation(80, 32),
    // (86,22): error CS0841: Cannot use local variable 'x10' before it is declared
    //         for (var b = x10;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x10").WithArguments("x10").WithLocation(86, 22),
    // (88,32): error CS0128: A local variable named 'x10' is already defined in this scope
    //              Dummy(true is var x10 && x10);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x10").WithArguments("x10").WithLocation(88, 32),
    // (89,32): error CS0128: A local variable named 'x10' is already defined in this scope
    //              Dummy(true is var x10 && x10))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x10").WithArguments("x10").WithLocation(89, 32),
    // (95,23): error CS0841: Cannot use local variable 'x11' before it is declared
    //         for (bool b = x11;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x11").WithArguments("x11").WithLocation(95, 23),
    // (97,32): error CS0128: A local variable named 'x11' is already defined in this scope
    //              Dummy(true is var x11 && x11);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x11").WithArguments("x11").WithLocation(97, 32),
    // (98,32): error CS0128: A local variable named 'x11' is already defined in this scope
    //              Dummy(true is var x11 && x11))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x11").WithArguments("x11").WithLocation(98, 32),
    // (104,20): error CS0841: Cannot use local variable 'x12' before it is declared
    //         for (Dummy(x12);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(104, 20),
    // (105,20): error CS0841: Cannot use local variable 'x12' before it is declared
    //              Dummy(x12) &&
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(105, 20),
    // (107,32): error CS0128: A local variable named 'x12' is already defined in this scope
    //              Dummy(true is var x12 && x12))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x12").WithArguments("x12").WithLocation(107, 32),
    // (113,22): error CS0841: Cannot use local variable 'x13' before it is declared
    //         for (var b = x13;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x13").WithArguments("x13").WithLocation(113, 22),
    // (114,20): error CS0841: Cannot use local variable 'x13' before it is declared
    //              Dummy(x13);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x13").WithArguments("x13").WithLocation(114, 20),
    // (116,32): error CS0128: A local variable named 'x13' is already defined in this scope
    //              Dummy(true is var x13 && x13))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x13").WithArguments("x13").WithLocation(116, 32),
    // (122,23): error CS0841: Cannot use local variable 'x14' before it is declared
    //         for (bool b = x14;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x14").WithArguments("x14").WithLocation(122, 23),
    // (123,20): error CS0841: Cannot use local variable 'x14' before it is declared
    //              Dummy(x14);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x14").WithArguments("x14").WithLocation(123, 20),
    // (125,32): error CS0128: A local variable named 'x14' is already defined in this scope
    //              Dummy(true is var x14 && x14))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(125, 32),
    // (131,20): error CS0841: Cannot use local variable 'x15' before it is declared
    //         for (Dummy(x15);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(131, 20),
    // (132,20): error CS0841: Cannot use local variable 'x15' before it is declared
    //              Dummy(x15);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(132, 20),
    // (133,20): error CS0841: Cannot use local variable 'x15' before it is declared
    //              Dummy(x15),
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(133, 20)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl);
            VerifyNotAPatternLocal(model, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl);
            VerifyNotAPatternLocal(model, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x3Decl);
            VerifyNotAPatternLocal(model, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);
            VerifyNotAPatternLocal(model, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl);
            VerifyNotAPatternLocal(model, x5Ref);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x6Decl);
            VerifyNotAPatternLocal(model, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x7Decl);
            VerifyNotAPatternLocal(model, x7Ref);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").ToArray();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(4, x8Decl.Length);
            Assert.Equal(4, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[2]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[3]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(3, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x9Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x9Decl[2]);

            var x10Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x10").ToArray();
            var x10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x10").ToArray();
            Assert.Equal(3, x10Decl.Length);
            Assert.Equal(4, x10Ref.Length);
            VerifyModelForDeclarationPattern(model, x10Decl[0], x10Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x10Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x10Decl[2]);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").ToArray();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(3, x11Decl.Length);
            Assert.Equal(4, x11Ref.Length);
            VerifyModelForDeclarationPattern(model, x11Decl[0], x11Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x11Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x11Decl[2]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").ToArray();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(2, x12Decl.Length);
            Assert.Equal(4, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl[0], x12Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x12Decl[1]);

            var x13Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x13").ToArray();
            var x13Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(4, x13Ref.Length);
            VerifyModelForDeclarationPattern(model, x13Decl[0], x13Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x13Decl[1]);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(4, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);

            var x15Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x15").Single();
            var x15Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x15").ToArray();
            Assert.Equal(4, x15Ref.Length);
            VerifyModelForDeclarationPattern(model, x15Decl, x15Ref);
        }

        [Fact]
        public void For_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f = true;

        for (Dummy(f, (f ? 10 : 20) is var x0, x0); 
             Dummy(f, (f ? 1 : 2) is var x1, x1); 
             Dummy(f, (f ? 100 : 200) is var x2, x2))
        {
            System.Console.WriteLine(x0);
            System.Console.WriteLine(x1);
            f = false;
        }
    }

    static bool Dummy(bool x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"10
1
10
1
200
2");
        }

        [Fact]
        public void ScopeOfPatternVariables_Foreach_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.Collections.IEnumerable Dummy(params object[] x) {return null;}

    void Test1()
    {
        foreach (var i in Dummy(true is var x1 && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        foreach (var i in Dummy(true is var x2 && x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        foreach (var i in Dummy(true is var x4 && x4))
            Dummy(x4);
    }

    void Test6()
    {
        foreach (var i in Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        foreach (var i in Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        foreach (var i in Dummy(true is var x8 && x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        foreach (var i1 in Dummy(true is var x9 && x9))
        {   
            Dummy(x9);
            foreach (var i2 in Dummy(true is var x9 && x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        foreach (var i in Dummy(y10 is var x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    foreach (var i in Dummy(y11 is var x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        foreach (var i in Dummy(y12 is var x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    foreach (var i in Dummy(y13 is var x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        foreach (var i in Dummy(1 is var x14, 
                                2 is var x14, 
                                x14))
        {
            Dummy(x14);
        }
    }

    void Test15()
    {
        foreach (var x15 in 
                            Dummy(1 is var x15, x15))
        {
            Dummy(x15);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,45): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         foreach (var i in Dummy(true is var x4 && x4))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 45),
    // (35,33): error CS0841: Cannot use local variable 'x6' before it is declared
    //         foreach (var i in Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 33),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,50): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             foreach (var i2 in Dummy(true is var x9 && x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 50),
    // (68,33): error CS0103: The name 'y10' does not exist in the current context
    //         foreach (var i in Dummy(y10 is var x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 33),
    // (86,33): error CS0103: The name 'y12' does not exist in the current context
    //         foreach (var i in Dummy(y12 is var x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 33),
    // (99,42): error CS0128: A local variable named 'x14' is already defined in this scope
    //                                 2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 42),
    // (108,22): error CS0136: A local or parameter named 'x15' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         foreach (var x15 in 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x15").WithArguments("x15").WithLocation(108, 22)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);

            var x15Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x15").Single();
            var x15Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x15").ToArray();
            Assert.Equal(2, x15Ref.Length);
            VerifyModelForDeclarationPattern(model, x15Decl, x15Ref[0]);
            VerifyNotAPatternLocal(model, x15Ref[1]);
        }

        [Fact]
        public void Foreach_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f = true;

        foreach (var i in Dummy(3 is var x1, x1))
        {
            System.Console.WriteLine(x1);
        }
    }

    static System.Collections.IEnumerable Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return ""a"";
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"3
3");
        }

        [Fact]
        public void ScopeOfPatternVariables_Lock_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        lock (Dummy(true is var x1 && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        lock (Dummy(true is var x2 && x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        lock (Dummy(true is var x4 && x4))
            Dummy(x4);
    }

    void Test6()
    {
        lock (Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        lock (Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        lock (Dummy(true is var x8 && x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        lock (Dummy(true is var x9 && x9))
        {   
            Dummy(x9);
            lock (Dummy(true is var x9 && x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        lock (Dummy(y10 is var x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    lock (Dummy(y11 is var x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        lock (Dummy(y12 is var x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    lock (Dummy(y13 is var x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        lock (Dummy(1 is var x14, 
                    2 is var x14, 
                    x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,33): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         lock (Dummy(true is var x4 && x4))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 33),
    // (35,21): error CS0841: Cannot use local variable 'x6' before it is declared
    //         lock (Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 21),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,37): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             lock (Dummy(true is var x9 && x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 37),
    // (68,21): error CS0103: The name 'y10' does not exist in the current context
    //         lock (Dummy(y10 is var x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 21),
    // (86,21): error CS0103: The name 'y12' does not exist in the current context
    //         lock (Dummy(y12 is var x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 21),
    // (99,30): error CS0128: A local variable named 'x14' is already defined in this scope
    //                     2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 30)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void Lock_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        lock (Dummy(""lock"" is var x1, x1))
        {
            System.Console.WriteLine(x1);
        }
    }

    static object Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return new object();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"lock
lock");
        }

        [Fact]
        public void ScopeOfPatternVariables_Fixed_01()
        {
            var source =
@"
public unsafe class X
{
    public static void Main()
    {
    }

    int[] Dummy(params object[] x) {return null;}

    void Test1()
    {
        fixed (int* p = Dummy(true is var x1 && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        fixed (int* p = Dummy(true is var x2 && x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        fixed (int* p = Dummy(true is var x4 && x4))
            Dummy(x4);
    }

    void Test6()
    {
        fixed (int* p = Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        fixed (int* p = Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        fixed (int* p = Dummy(true is var x8 && x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        fixed (int* p1 = Dummy(true is var x9 && x9))
        {   
            Dummy(x9);
            fixed (int* p2 = Dummy(true is var x9 && x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        fixed (int* p = Dummy(y10 is var x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    fixed (int* p = Dummy(y11 is var x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        fixed (int* p = Dummy(y12 is var x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    fixed (int* p = Dummy(y13 is var x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        fixed (int* p = Dummy(1 is var x14, 
                              2 is var x14, 
                              x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,43): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         fixed (int* p = Dummy(true is var x4 && x4))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 43),
    // (35,31): error CS0841: Cannot use local variable 'x6' before it is declared
    //         fixed (int* p = Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 31),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,48): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             fixed (int* p2 = Dummy(true is var x9 && x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 48),
    // (68,31): error CS0103: The name 'y10' does not exist in the current context
    //         fixed (int* p = Dummy(y10 is var x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 31),
    // (86,31): error CS0103: The name 'y12' does not exist in the current context
    //         fixed (int* p = Dummy(y12 is var x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 31),
    // (99,40): error CS0128: A local variable named 'x14' is already defined in this scope
    //                               2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 40)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Fixed_02()
        {
            var source =
@"
public unsafe class X
{
    public static void Main()
    {
    }

    int[] Dummy(params object[] x) {return null;}
    int[] Dummy(int* x) {return null;}

    void Test1()
    {
        fixed (int* x1 = 
                         Dummy(true is var x1 && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        fixed (int* p = Dummy(true is var x2 && x2),
                    x2 = Dummy())
        {
            Dummy(x2);
        }
    }

    void Test3()
    {
        fixed (int* x3 = Dummy(),
                    p = Dummy(true is var x3 && x3))
        {
            Dummy(x3);
        }
    }

    void Test4()
    {
        fixed (int* p1 = Dummy(true is var x4 && x4),
                    p2 = Dummy(true is var x4 && x4))
        {
            Dummy(x4);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
    // (14,44): error CS0128: A local variable named 'x1' is already defined in this scope
    //                          Dummy(true is var x1 && x1))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(14, 44),
    // (14,50): error CS0165: Use of unassigned local variable 'x1'
    //                          Dummy(true is var x1 && x1))
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(14, 50),
    // (23,21): error CS0128: A local variable named 'x2' is already defined in this scope
    //                     x2 = Dummy())
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(23, 21),
    // (32,43): error CS0128: A local variable named 'x3' is already defined in this scope
    //                     p = Dummy(true is var x3 && x3))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(32, 43),
    // (41,44): error CS0128: A local variable named 'x4' is already defined in this scope
    //                     p2 = Dummy(true is var x4 && x4))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(41, 44)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl);
            VerifyNotAPatternLocal(model, x1Ref[0]);
            VerifyNotAPatternLocal(model, x1Ref[1]);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x3Decl);
            VerifyNotAPatternLocal(model, x3Ref[0]);
            VerifyNotAPatternLocal(model, x3Ref[1]);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").ToArray();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Decl.Length);
            Assert.Equal(3, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl[0], x4Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl[1]);
        }

        [Fact]
        public void Fixed_01()
        {
            var source =
@"
public unsafe class X
{
    public static void Main()
    {
        fixed (int* p = Dummy(""fixed"" is var x1, x1))
        {
            System.Console.WriteLine(x1);
        }
    }

    static int[] Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return new int[1];
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(compilation, expectedOutput:
@"fixed
fixed");
        }

        [Fact]
        public void ScopeOfPatternVariables_Yield_01()
        {
            var source =
@"
using System.Collections;

public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) { return null;}

    IEnumerable Test1()
    {
        yield return Dummy(true is var x1, x1);
        {
            yield return Dummy(true is var x1, x1);
        }
        yield return Dummy(true is var x1, x1);
    }

    IEnumerable Test2()
    {
        yield return Dummy(x2, true is var x2);
    }

    IEnumerable Test3(int x3)
    {
        yield return Dummy(true is var x3, x3);
    }

    IEnumerable Test4()
    {
        var x4 = 11;
        Dummy(x4);
        yield return Dummy(true is var x4, x4);
    }

    IEnumerable Test5()
    {
        yield return Dummy(true is var x5, x5);
        var x5 = 11;
        Dummy(x5);
    }

    //IEnumerable Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    yield return Dummy(true is var x6, x6);
    //}

    //IEnumerable Test7()
    //{
    //    yield return Dummy(true is var x7, x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    IEnumerable Test8()
    {
        yield return Dummy(true is var x8, x8, false is var x8, x8);
    }

    IEnumerable Test9(bool y9)
    {
        if (y9)
            yield return Dummy(true is var x9, x9);
    }

    IEnumerable Test11()
    {
        Dummy(x11);
        yield return Dummy(true is var x11, x11);
    }

    IEnumerable Test12()
    {
        yield return Dummy(true is var x12, x12);
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (23,28): error CS0841: Cannot use local variable 'x2' before it is declared
    //         yield return Dummy(x2, true is var x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(23, 28),
    // (28,40): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         yield return Dummy(true is var x3, x3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(28, 40),
    // (35,40): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         yield return Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(35, 40),
    // (40,40): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         yield return Dummy(true is var x5, x5);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(40, 40),
    // (61,61): error CS0128: A local variable named 'x8' is already defined in this scope
    //         yield return Dummy(true is var x8, x8, false is var x8, x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(61, 61),
    // (72,15): error CS0103: The name 'x11' does not exist in the current context
    //         Dummy(x11);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(72, 15),
    // (79,15): error CS0103: The name 'x12' does not exist in the current context
    //         Dummy(x12);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(79, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x1Decl[i], x1Ref[i]);
            }

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1]);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref[0]);
            VerifyNotAPatternLocal(model, x5Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").ToArray();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            for (int i = 0; i < x8Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").Single();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").Single();
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[0]);
            VerifyNotInScope(model, x12Ref[1]);
        }

        [Fact]
        public void Yield_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        foreach (var o in Test())
        {}
    }

    static System.Collections.IEnumerable Test()
    {
        yield return Dummy(""yield1"" is var x1, x1);
        yield return Dummy(""yield2"" is var x1, x1);
    }

    static object Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return new object();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"yield1
yield2");
        }

        [Fact]
        public void ScopeOfPatternVariables_Return_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) { return null;}

    object Test1()
    {
        return Dummy(true is var x1, x1);
        {
            return Dummy(true is var x1, x1);
        }
        return Dummy(true is var x1, x1);
    }

    object Test2()
    {
        return Dummy(x2, true is var x2);
    }

    object Test3(int x3)
    {
        return Dummy(true is var x3, x3);
    }

    object Test4()
    {
        var x4 = 11;
        Dummy(x4);
        return Dummy(true is var x4, x4);
    }

    object Test5()
    {
        return Dummy(true is var x5, x5);
        var x5 = 11;
        Dummy(x5);
    }

    //object Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    return Dummy(true is var x6, x6);
    //}

    //object Test7()
    //{
    //    return Dummy(true is var x7, x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    object Test8()
    {
        return Dummy(true is var x8, x8, false is var x8, x8);
    }

    object Test9(bool y9)
    {
        if (y9)
            return Dummy(true is var x9, x9);

        return null;
    }

    object Test11()
    {
        Dummy(x11);
        return Dummy(true is var x11, x11);
    }

    object Test12()
    {
        return Dummy(true is var x12, x12);
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (14,13): warning CS0162: Unreachable code detected
    //             return Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(14, 13),
    // (21,22): error CS0841: Cannot use local variable 'x2' before it is declared
    //         return Dummy(x2, true is var x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 22),
    // (26,34): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         return Dummy(true is var x3, x3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 34),
    // (33,34): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         return Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(33, 34),
    // (38,34): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         return Dummy(true is var x5, x5);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(38, 34),
    // (39,9): warning CS0162: Unreachable code detected
    //         var x5 = 11;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "var").WithLocation(39, 9),
    // (59,55): error CS0128: A local variable named 'x8' is already defined in this scope
    //         return Dummy(true is var x8, x8, false is var x8, x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 55),
    // (72,15): error CS0103: The name 'x11' does not exist in the current context
    //         Dummy(x11);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(72, 15),
    // (79,15): error CS0103: The name 'x12' does not exist in the current context
    //         Dummy(x12);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(79, 15),
    // (79,9): warning CS0162: Unreachable code detected
    //         Dummy(x12);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "Dummy").WithLocation(79, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x1Decl[i], x1Ref[i]);
            }

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1]);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref[0]);
            VerifyNotAPatternLocal(model, x5Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").ToArray();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            for (int i = 0; i < x8Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").Single();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").Single();
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[0]);
            VerifyNotInScope(model, x12Ref[1]);
        }

        [Fact]
        public void Return_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        Test();
    }

    static object Test()
    {
        return Dummy(""return"" is var x1, x1);
    }

    static object Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return new object();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:@"return");
        }

        [Fact]
        public void ScopeOfPatternVariables_Throw_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.Exception Dummy(params object[] x) { return null;}

    void Test1()
    {
        throw Dummy(true is var x1, x1);
        {
            throw Dummy(true is var x1, x1);
        }
        throw Dummy(true is var x1, x1);
    }

    void Test2()
    {
        throw Dummy(x2, true is var x2);
    }

    void Test3(int x3)
    {
        throw Dummy(true is var x3, x3);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);
        throw Dummy(true is var x4, x4);
    }

    void Test5()
    {
        throw Dummy(true is var x5, x5);
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    throw Dummy(true is var x6, x6);
    //}

    //void Test7()
    //{
    //    throw Dummy(true is var x7, x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8()
    {
        throw Dummy(true is var x8, x8, false is var x8, x8);
    }

    void Test9(bool y9)
    {
        if (y9)
            throw Dummy(true is var x9, x9);
    }

    void Test11()
    {
        Dummy(x11);
        throw Dummy(true is var x11, x11);
    }

    void Test12()
    {
        throw Dummy(true is var x12, x12);
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (21,21): error CS0841: Cannot use local variable 'x2' before it is declared
    //         throw Dummy(x2, true is var x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 21),
    // (26,33): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         throw Dummy(true is var x3, x3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 33),
    // (33,33): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         throw Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(33, 33),
    // (38,33): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         throw Dummy(true is var x5, x5);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(38, 33),
    // (39,9): warning CS0162: Unreachable code detected
    //         var x5 = 11;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "var").WithLocation(39, 9),
    // (59,54): error CS0128: A local variable named 'x8' is already defined in this scope
    //         throw Dummy(true is var x8, x8, false is var x8, x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 54),
    // (70,15): error CS0103: The name 'x11' does not exist in the current context
    //         Dummy(x11);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(70, 15),
    // (77,15): error CS0103: The name 'x12' does not exist in the current context
    //         Dummy(x12);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(77, 15),
    // (77,9): warning CS0162: Unreachable code detected
    //         Dummy(x12);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "Dummy").WithLocation(77, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x1Decl[i], x1Ref[i]);
            }

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1]);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref[0]);
            VerifyNotAPatternLocal(model, x5Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").ToArray();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            for (int i = 0; i < x8Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").Single();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").Single();
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[0]);
            VerifyNotInScope(model, x12Ref[1]);
        }

        [Fact]
        public void Throw_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        Test();
    }

    static void Test()
    {
        try
        {
            throw Dummy(""throw"" is var x1, x1);
        }
        catch
        {
        }
    }

    static System.Exception Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return new System.ArgumentException();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"throw");
        }

        [Fact]
        public void ScopeOfPatternVariables_Catch_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        try {}
        catch when (true is var x1 && x1)
        {
            Dummy(x1);
        }
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        try {}
        catch when (true is var x4 && x4)
        {
            Dummy(x4);
        }
    }

    void Test6()
    {
        try {}
        catch when (x6 && true is var x6)
        {
            Dummy(x6);
        }
    }

    void Test7()
    {
        try {}
        catch when (true is var x7 && x7)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        try {}
        catch when (true is var x8 && x8)
        {
            Dummy(x8);
        }

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        try {}
        catch when (true is var x9 && x9)
        {   
            Dummy(x9);
            try {}
            catch when (true is var x9 && x9) // 2
            {
                Dummy(x9);
            }
        }
    }

    void Test10()
    {
        try {}
        catch when (y10 is var x10)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    try {}
    //    catch when (y11 is var x11)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test14()
    {
        try {}
        catch when (Dummy(1 is var x14, 
                          2 is var x14, 
                          x14))
        {
            Dummy(x14);
        }
    }

    void Test15()
    {
        try {}
        catch (System.Exception x15)
              when (Dummy(1 is var x15, x15))
        {
            Dummy(x15);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (25,33): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         catch when (true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(25, 33),
    // (34,21): error CS0841: Cannot use local variable 'x6' before it is declared
    //         catch when (x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(34, 21),
    // (45,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(45, 17),
    // (58,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(58, 34),
    // (68,37): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             catch when (true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(68, 37),
    // (78,21): error CS0103: The name 'y10' does not exist in the current context
    //         catch when (y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(78, 21),
    // (99,36): error CS0128: A local variable named 'x14' is already defined in this scope
    //                           2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 36),
    // (110,36): error CS0128: A local variable named 'x15' is already defined in this scope
    //               when (Dummy(1 is var x15, x15))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(110, 36)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").ToArray();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);

            var x15Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x15").Single();
            var x15Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x15").ToArray();
            Assert.Equal(2, x15Ref.Length);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x15Decl);
            VerifyNotAPatternLocal(model, x15Ref[0]);
            VerifyNotAPatternLocal(model, x15Ref[1]);
        }

        [Fact]
        public void Catch_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch (System.Exception e) when (Dummy(e is var x1, x1))
        {
            System.Console.WriteLine(x1.GetType());
        }
    }

    static bool Dummy(object y, object z) 
    {
        System.Console.WriteLine(z.GetType());
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"System.InvalidOperationException
System.InvalidOperationException");
        }

        [Fact]
        public void Catch_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch (System.Exception e) when (Dummy(e is var x1, x1))
        {
            System.Action d = () =>
                                {
                                    System.Console.WriteLine(x1.GetType());
                                };

            System.Console.WriteLine(x1.GetType());
            d();
        }
    }

    static bool Dummy(object y, object z) 
    {
        System.Console.WriteLine(z.GetType());
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"System.InvalidOperationException
System.InvalidOperationException
System.InvalidOperationException");
        }

        [Fact]
        public void Catch_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch (System.Exception e) when (Dummy(e is var x1, x1))
        {
            System.Action d = () =>
                                {
                                    e = new System.NullReferenceException();
                                    System.Console.WriteLine(x1.GetType());
                                };

            System.Console.WriteLine(x1.GetType());
            d();
            System.Console.WriteLine(e.GetType());
        }
    }

    static bool Dummy(object y, object z) 
    {
        System.Console.WriteLine(z.GetType());
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"System.InvalidOperationException
System.InvalidOperationException
System.InvalidOperationException
System.NullReferenceException");
        }

        [Fact]
        public void Catch_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch (System.Exception e) when (Dummy(e is var x1, x1))
        {
            System.Action d = () =>
                                {
                                    e = new System.NullReferenceException();
                                };

            System.Console.WriteLine(x1.GetType());
            d();
            System.Console.WriteLine(e.GetType());
        }
    }

    static bool Dummy(object y, object z) 
    {
        System.Console.WriteLine(z.GetType());
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"System.InvalidOperationException
System.InvalidOperationException
System.NullReferenceException");
        }

        [Fact, WorkItem(10465, "https://github.com/dotnet/roslyn/issues/10465")]
        public void Constants_Fail()
        {
            var source =
@"
using System;
public class X
{
    public static void Main()
    {
        Console.WriteLine(1L is string); // warning: type mismatch
        Console.WriteLine(1 is int[]); // warning: expression is never of the provided type

        Console.WriteLine(1L is string s); // error: type mismatch
        Console.WriteLine(1 is int[] a); // error: expression is never of the provided type
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (7,27): warning CS0184: The given expression is never of the provided ('string') type
                //         Console.WriteLine(1L is string); // warning: type mismatch
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "1L is string").WithArguments("string").WithLocation(7, 27),
                // (8,27): warning CS0184: The given expression is never of the provided ('int[]') type
                //         Console.WriteLine(1 is int[]); // warning: expression is never of the provided type
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "1 is int[]").WithArguments("int[]").WithLocation(8, 27),
                // (10,33): error CS8121: An expression of type long cannot be handled by a pattern of type string.
                //         Console.WriteLine(1L is string s); // error: type mismatch
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("long", "string").WithLocation(10, 33),
                // (11,32): error CS8121: An expression of type int cannot be handled by a pattern of type int[].
                //         Console.WriteLine(1 is int[] a); // error: expression is never of the provided type
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int[]").WithArguments("int", "int[]").WithLocation(11, 32)
                );
        }

        [Fact, WorkItem(10465, "https://github.com/dotnet/roslyn/issues/10465")]
        public void Types_Pass()
        {
            var source =
@"
using System;
public class X
{
    public static void Main()
    {
        Console.WriteLine(1 is 1); // true
        Console.WriteLine(1L is int.MaxValue); // OK, but false
        Console.WriteLine(1 is int.MaxValue); // false
        Console.WriteLine(int.MaxValue is int.MaxValue); // true
        Console.WriteLine(""foo"" is System.String); // true
        Console.WriteLine(Int32.MaxValue is Int32.MaxValue); // true
        Console.WriteLine(new int[] {1, 2} is int[] a); // true
        object o = null;
        switch (o)
        {
            case int[] a:
                break;
            case int.MaxValue: // constant, not a type
                break;
            case int i:
                break;
            case null:
                Console.WriteLine(""null"");
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput:
@"True
False
False
True
True
True
True
null");
        }

        [Fact, WorkItem(10459, "https://github.com/dotnet/roslyn/issues/10459")]
        public void Typeswitch_01()
        {
            var source =
@"
using System;
public class X
{
    public static void Main(string[] args)
    {
        switch (args.GetType())
        {
            case typeof(string):
                Console.WriteLine(""string"");
                break;
            case typeof(string[]):
                Console.WriteLine(""string[]"");
                break;
            case null:
                Console.WriteLine(""null"");
                break;
            default:
                Console.WriteLine(""default"");
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (9,18): error CS0150: A constant value is expected
                //             case typeof(string):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "typeof(string)").WithLocation(9, 18),
                // (12,18): error CS0150: A constant value is expected
                //             case typeof(string[]):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "typeof(string[])").WithLocation(12, 18)
                );
            // If we support switching on System.Type as proposed, the expectation would be
            // something like CompileAndVerify(compilation, expectedOutput: @"string[]");
        }

        [Fact, WorkItem(10529, "https://github.com/dotnet/roslyn/issues/10529")]
        public void MissingTypeAndProperty()
        {
            var source =
@"
class Program
{
    public static void Main(string[] args)
    {
        {
            if (obj.Property is var o) { } // `obj` doesn't exist.
        }
        {
            var obj = new object();
            if (obj. is var o) { }
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (11,22): error CS1001: Identifier expected
                //             if (obj. is var o) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "is").WithLocation(11, 22),
                // (7,17): error CS0103: The name 'obj' does not exist in the current context
                //             if (obj.Property is var o) { } // `obj` doesn't exist.
                Diagnostic(ErrorCode.ERR_NameNotInContext, "obj").WithArguments("obj").WithLocation(7, 17)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            foreach (var isExpression in tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>())
            {
                var symbolInfo = model.GetSymbolInfo(isExpression.Expression);
                Assert.Null(symbolInfo.Symbol);
                Assert.True(symbolInfo.CandidateSymbols.IsDefaultOrEmpty);
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            }
        }

        [Fact]
        public void MixedDecisionTree()
        {
            var source =
@"
using System;
public class X
{
    public static void Main()
    {
        M(null);
        M(1);
        M((byte)1);
        M((short)1);
        M(2);
        M((byte)2);
        M((short)2);
        M(""hmm"");
        M(""bar"");
        M(""baz"");
        M(6);
    }

    public static void M(object o)
    {
        switch (o)
        {
            case ""hmm"":
                Console.WriteLine(""hmm""); break;
            case null:
                Console.WriteLine(""null""); break;
            case 1:
                Console.WriteLine(""int 1""); break;
            case ((byte)1):
                Console.WriteLine(""byte 1""); break;
            case ((short)1):
                Console.WriteLine(""short 1""); break;
            case ""bar"":
                Console.WriteLine(""bar""); break;
            case object t when t != o:
                Console.WriteLine(""impossible""); break;
            case 2:
                Console.WriteLine(""int 2""); break;
            case ((byte)2):
                Console.WriteLine(""byte 2""); break;
            case ((short)2):
                Console.WriteLine(""short 2""); break;
            case ""baz"":
                Console.WriteLine(""baz""); break;
            default:
                Console.WriteLine(""other "" + o); break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput:
@"null
int 1
byte 1
short 1
int 2
byte 2
short 2
hmm
bar
baz
other 6");
        }

        [Fact]
        public void SemanticAnalysisWithPatternInCsharp6()
        {
            var source =
@"class Program
{
    public static void Main(string[] args)
    {
        switch (args.Length)
        {
            case 1 when true:
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular6);
            compilation.VerifyDiagnostics(
                // (7,13): error CS8059: Feature 'pattern matching' is not available in C# 6.  Please use language version 7 or greater.
                //             case 1 when true:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case 1 when true:").WithArguments("pattern matching", "7").WithLocation(7, 13)
                );
        }

        [Fact, WorkItem(11379, "https://github.com/dotnet/roslyn/issues/11379")]
        public void DeclarationPatternWithStaticClass()
        {
            var source =
@"class Program
{
    public static void Main(string[] args)
    {
        object o = args;
        switch (o)
        {
            case StaticType t:
                break;
        }
    }
}
public static class StaticType
{
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (8,18): error CS0723: Cannot declare a variable of static type 'StaticType'
                //             case StaticType t:
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "StaticType").WithArguments("StaticType").WithLocation(8, 18)
                );
        }

        [Fact]
        public void PatternVariablesAreMutable02()
        {
            var source =
@"class Program
{
    public static void Main(string[] args)
    {
        object o = ""  whatever  "";
        if (o is string s)
        {
            s = s.Trim();
            System.Console.WriteLine(s);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: "whatever");
        }
    }
}
