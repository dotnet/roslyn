// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class PatternMatchingTests : PatternMatchingTestBase
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
                // (12,13): error CS8059: Feature 'local functions' is not available in C# 6.  Please use language version 7 or greater.
                //         int f() => 2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "f").WithArguments("local functions", "7").WithLocation(12, 13),
                // (13,9): error CS8059: Feature 'byref locals and returns' is not available in C# 6.  Please use language version 7 or greater.
                //         ref int i3 = ref i1; // ref locals
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref int").WithArguments("byref locals and returns", "7").WithLocation(13, 9),
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
        Console.WriteLine(""2. {0}"", s is string w ? w : nameof(X));
        int? x = 12;
        {if (x is var y) Console.WriteLine(""3. {0}"", y);}
        {if (x is int y) Console.WriteLine(""4. {0}"", y);}
        x = null;
        {if (x is var y) Console.WriteLine(""5. {0}"", y);}
        {if (x is int y) Console.WriteLine(""6. {0}"", y);}
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
            using (new EnsureInvariantCulture())
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
        if (s is string t) { } else Console.WriteLine(t); 
        if (null is dynamic t) { } // null not allowed
        if (s is NullableInt x) { } // error: cannot use nullable type
        if (s is long l) { } // error: cannot convert string to long
        if (b is 1000) { } // error: cannot convert 1000 to byte
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (10,13): error CS8117: Invalid operand for pattern match; value required, but found '<null>'.
                //         if (null is dynamic t) { } // null not allowed
                Diagnostic(ErrorCode.ERR_BadIsPatternExpression, "null").WithArguments("<null>").WithLocation(10, 13),
                // (10,29): error CS0128: A local variable named 't' is already defined in this scope
                //         if (null is dynamic t) { } // null not allowed
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "t").WithArguments("t").WithLocation(10, 29),
                // (11,18): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
                //         if (s is NullableInt x) { } // error: cannot use nullable type
                Diagnostic(ErrorCode.ERR_PatternNullableType, "NullableInt").WithArguments("int?", "int").WithLocation(11, 18),
                // (12,18): error CS8121: An expression of type string cannot be handled by a pattern of type long.
                //         if (s is long l) { } // error: cannot convert string to long
                Diagnostic(ErrorCode.ERR_PatternWrongType, "long").WithArguments("string", "long").WithLocation(12, 18),
                // (13,18): error CS0031: Constant value '1000' cannot be converted to a 'byte'
                //         if (b is 1000) { } // error: cannot convert 1000 to byte
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "1000").WithArguments("1000", "byte").WithLocation(13, 18),
                // (9,55): error CS0165: Use of unassigned local variable 't'
                //         if (s is string t) { } else Console.WriteLine(t); 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "t").WithArguments("t").WithLocation(9, 55)
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
#if ALLOW_IN_CONSTRUCTOR_INITIALIZER
            compilation.VerifyDiagnostics(
                );
            var expectedOutput =
@"False
True
False";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
#else
            compilation.VerifyDiagnostics(
                // (13,36): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //     public D(object o) : this(o is int x && x >= 5) {}
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x").WithLocation(13, 36)
                );
#endif
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
            using (new EnsureInvariantCulture())
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
#if ALLOW_IN_FIELD_INITIALIZER
            compilation.VerifyDiagnostics();
            using (new EnsureInvariantCulture())
            {
                var expectedOutput =
@"False for 1
True for 10
False for 1.2";
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
#else
            compilation.VerifyDiagnostics(
                // (7,35): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //     static bool b1 = M(o1, (o1 is int x && x >= 5)),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x").WithLocation(7, 35),
                // (8,35): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //                 b2 = M(o2, (o2 is int x && x >= 5)),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x").WithLocation(8, 35),
                // (9,35): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //                 b3 = M(o3, (o3 is int x && x >= 5));
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x").WithLocation(9, 35)
                );
#endif
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
            using (new EnsureInvariantCulture())
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
            using (new EnsureInvariantCulture())
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
            using (new EnsureInvariantCulture())
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
            using (new EnsureInvariantCulture())
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
            using (new EnsureInvariantCulture())
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
        public void If_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        Test(1);
        Test(2);
    }

    public static void Test(int val)
    {
        if (Dummy(val == 1, val is var x1, x1))
        {
            System.Console.WriteLine(""true"");
            System.Console.WriteLine(x1);
        }
        else
        {
            System.Console.WriteLine(""false"");
            System.Console.WriteLine(x1);
        }

        System.Console.WriteLine(x1);
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
true
1
1
2
false
2
2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(4, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void If_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f = true;

        if (f)
            if (Dummy(f, (f ? 1 : 2) is var x1, x1))
                ;

        if (f)
        {
            if (Dummy(f, (f ? 3 : 4) is var x1, x1))
                ;
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
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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
                var yDecl = GetPatternDeclarations(tree, id).Single();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).Single();
                VerifyModelForDeclarationPattern(model, yDecl, yRef);
            }
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
#if ALLOW_IN_FIELD_INITIALIZER
            CompileAndVerify(compilation, expectedOutput: @"1
True");
#else
            compilation.VerifyDiagnostics(
                // (9,30): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //     static bool Test1 = 1 is int x1 && Dummy(x1); 
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x1").WithLocation(9, 30)
                );
#endif
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
#if ALLOW_IN_FIELD_INITIALIZER
            CompileAndVerify(compilation, expectedOutput: @"1
True
2
True");
#else
            compilation.VerifyDiagnostics(
                // (14,30): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //     static bool Test1 = 1 is int x1 && Dummy(() => x1);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x1").WithLocation(14, 30),
                // (15,23): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //     bool Test2 = 2 is int x1 && Dummy(() => x1);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x1").WithLocation(15, 23)
                );
#endif
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
#if ALLOW_IN_FIELD_INITIALIZER
            CompileAndVerify(compilation, expectedOutput: @"1
True");
#else
            compilation.VerifyDiagnostics(
                // (9,37): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //     static bool Test1 {get;} = 1 is int x1 && Dummy(x1); 
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x1").WithLocation(9, 37)
                );
#endif
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
#if ALLOW_IN_CONSTRUCTOR_INITIALIZER
            CompileAndVerify(compilation, expectedOutput:
@"1
2
True
True");
#else
            compilation.VerifyDiagnostics(
                // (12,36): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //     public D(object o) : base(2 is int x1 && Dummy(x1)) 
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x1").WithLocation(12, 36),
                // (17,28): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //     public D() : this(1 is int x1 && Dummy(x1)) 
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x1").WithLocation(17, 28)
                );
#endif
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

        System.Console.WriteLine(x1);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"Test1 case 0
Test1 {0}
Test1 1
Test1 {0}");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void Switch_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f = true;

        if (f)
            switch (Dummy(f, (f ? 1 : 2) is var x1, x1))
            {}

        if (f)
        {
            switch (Dummy(f, (f ? 3 : 4) is var x1, x1))
            {}
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
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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
        System.Console.WriteLine(x1);
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
c
b");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void LocalDeclarationStmt_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        if (true)
        {
            object d1 = Dummy(new C(""a""), new C(""b"") is var x1, x1);
        }
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
@"b");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void DeconstructionDeclarationStmt_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        (object d1, object d2) = (Dummy(new C(""a""), (new C(""b"") is var x1), x1),
                                 Dummy(new C(""c""), (new C(""d"") is var x2), x2));
        System.Console.WriteLine(d1);
        System.Console.WriteLine(d2);
        System.Console.WriteLine(x1);
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
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                              options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"b
d
a
c
b");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void DeconstructionDeclarationStmt_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        if (true)
        {
            (object d1, object d2) = (Dummy(new C(""a""), (new C(""b"") is var x1), x1), x1);
        }
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
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                              options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"b");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void DeconstructionDeclarationStmt_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        var (d1, (d2, d3)) = (Dummy(new C(""a""), (new C(""b"") is var x1), x1),
                              (Dummy(new C(""c""), (new C(""d"") is var x2), x2),
                               Dummy(new C(""e""), (new C(""f"") is var x3), x3)));
        System.Console.WriteLine(d1);
        System.Console.WriteLine(d2);
        System.Console.WriteLine(d3);
        System.Console.WriteLine(x1);
        System.Console.WriteLine(x2);
        System.Console.WriteLine(x3);
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
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                              options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"b
d
f
a
c
e
b
d
f");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").ToArray();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(1, x2Decl.Length);
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl[0], x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").ToArray();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").ToArray();
            Assert.Equal(1, x3Decl.Length);
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl[0], x3Ref);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void DeconstructionDeclarationStmt_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        (var d1, (var d2, var d3)) = (Dummy(new C(""a""), (new C(""b"") is var x1), x1),
                              (Dummy(new C(""c""), (new C(""d"") is var x2), x2),
                               Dummy(new C(""e""), (new C(""f"") is var x3), x3)));
        System.Console.WriteLine(d1);
        System.Console.WriteLine(d2);
        System.Console.WriteLine(d3);
        System.Console.WriteLine(x1);
        System.Console.WriteLine(x2);
        System.Console.WriteLine(x3);
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
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                              options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"b
d
f
a
c
e
b
d
f");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").ToArray();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(1, x2Decl.Length);
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl[0], x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").ToArray();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").ToArray();
            Assert.Equal(1, x3Decl.Length);
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl[0], x3Ref);
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

        System.Console.WriteLine(x1);
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
2
2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void While_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f = true;

        if (f)
            while (Dummy(f, (f ? 1 : 2) is var x1, x1))
                break;

        if (f)
        {
            while (Dummy(f, (f ? 3 : 4) is var x1, x1))
                break;
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
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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

        System.Console.WriteLine(x1);
    }

    static bool Dummy(bool x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"2
2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void Do_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f = true;

        if (f)
            do
                ;
            while (Dummy(f, (f ? 1 : 2) is var x1, x1) && false);

        if (f)
        {
            do
                ;
            while (Dummy(f, (f ? 3 : 4) is var x1, x1) && false);
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
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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

        System.Console.WriteLine(x1);
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
lock
lock");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void Lock_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f = true;

        if (f)
            lock (Dummy(f, (f ? 1 : 2) is var x1, x1))
                {}

        if (f)
        {
            lock (Dummy(f, (f ? 3 : 4) is var x1, x1))
                {}
        }
    }

    static object Dummy(bool x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"1
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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
        yield return Dummy(""yield2"" is var x2, x2);
        System.Console.WriteLine(x1);
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
yield2
yield1");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void Yield_02()
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
        bool f = true;

        if (f)
            yield return Dummy(""yield1"" is var x1, x1);

        if (f)
        {
            yield return Dummy(""yield2"" is var x1, x1);
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
@"yield1
yield2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void Return_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test0(true);
        Test0(false);
    }

    static object Test0(bool val)
    {
        if (val)
            return Test2(1 is var x1, x1);

        if (!val)
        {
            return Test2(2 is var x1, x1);
        }

        return null;
    }

    static object Test2(object x, object y)
    {
        System.Console.Write(y);
        return x;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: "12").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void Throw_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        Test(true);
        Test(false);
    }

    static void Test(bool val)
    {
        try
        {
            if (val)
                throw Dummy(""throw 1"" is var x1, x1);

            if (!val)
            {
                throw Dummy(""throw 2"" is var x1, x1);
            }
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
            CompileAndVerify(compilation, expectedOutput:
@"throw 1
throw 2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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

        [Fact]
        public void Labeled_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
a:      Test1(2 is var x1);
        System.Console.WriteLine(x1);
    }

    static object Test1(bool x)
    {
        return null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: "2").VerifyDiagnostics(
                // (6,1): warning CS0164: This label has not been referenced
                // a:      Test1(2 is var x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(6, 1)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void Labeled_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test0();
    }

    static object Test0()
    {
        bool test = true;

        if (test)
        {
a:          Test2(2 is var x1, x1);
        }

        return null;
    }

    static object Test2(object x, object y)
    {
        System.Console.Write(y);
        return x;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: "2").VerifyDiagnostics(
                // (15,1): warning CS0164: This label has not been referenced
                // a:          Test2(2 is var x1, x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(15, 1)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
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
            case int[] b:
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

        [Fact, WorkItem(12996, "https://github.com/dotnet/roslyn/issues/12996")]
        public void TypeOfAVarPatternVariable()
        {
            var source =
@"
class Program
{
    public static void Main(string[] args)
    {
    }

    public static void Test(int val)
    {
        if (val is var o1) 
        {
            System.Console.WriteLine(o1);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var tree = compilation.SyntaxTrees[0];

            var model1 = compilation.GetSemanticModel(tree);

            var declaration = tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();
            var o1 = GetReferences(tree, "o1").Single();

            var typeInfo1 = model1.GetTypeInfo(declaration);
            Assert.Equal(SymbolKind.NamedType, typeInfo1.Type.Kind);
            Assert.Equal("System.Boolean", typeInfo1.Type.ToTestDisplayString());

            typeInfo1 = model1.GetTypeInfo(o1);
            Assert.Equal(SymbolKind.NamedType, typeInfo1.Type.Kind);
            Assert.Equal("System.Int32", typeInfo1.Type.ToTestDisplayString());

            var model2 = compilation.GetSemanticModel(tree);

            var typeInfo2 = model2.GetTypeInfo(o1);
            Assert.Equal(SymbolKind.NamedType, typeInfo2.Type.Kind);
            Assert.Equal("System.Int32", typeInfo2.Type.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(13417, "https://github.com/dotnet/roslyn/issues/13417")]
        public void FixedFieldSize()
        {
            var text = @"
unsafe struct S
{
    fixed int F1[3 is var x1 ? x1 : 3];
    fixed int F2[3 is var x2 ? 3 : 3, x2];
}
";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseDebugDll.WithAllowUnsafe(true),
                                                            parseOptions: TestOptions.Regular);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            Assert.True(((TypeSymbol)compilation.GetSemanticModel(tree).GetTypeInfo(x1Ref).Type).IsErrorType());
            VerifyModelNotSupported(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelNotSupported(model, x2Decl, x2Ref);
            Assert.True(((TypeSymbol)compilation.GetSemanticModel(tree).GetTypeInfo(x2Ref).Type).IsErrorType());

            compilation.VerifyDiagnostics(
                // (5,17): error CS7092: A fixed buffer may only have one dimension.
                //     fixed int F2[3 is var x2 ? 3 : 3, x2];
                Diagnostic(ErrorCode.ERR_FixedBufferTooManyDimensions, "[3 is var x2 ? 3 : 3, x2]").WithLocation(5, 17),
                // (5,18): error CS0133: The expression being assigned to 'S.F2' must be constant
                //     fixed int F2[3 is var x2 ? 3 : 3, x2];
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "3 is var x2 ? 3 : 3").WithArguments("S.F2").WithLocation(5, 18),
                // (4,18): error CS0133: The expression being assigned to 'S.F1' must be constant
                //     fixed int F1[3 is var x1 ? x1 : 3];
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "3 is var x1 ? x1 : 3").WithArguments("S.F1").WithLocation(4, 18)
                );
        }

        [Fact, WorkItem(13316, "https://github.com/dotnet/roslyn/issues/13316")]
        public void TypeAsExpressionInIsPattern()
        {
            var source =
@"namespace CS7
{
    class T1 { public int a = 2; }
    class Program
    {
        static void Main(string[] args)
        {
            if (T1 is object i)
            {
            }
        }
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (8,17): error CS0119: 'T1' is a type, which is not valid in the given context
                //             if (T1 is object i)
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T1").WithArguments("CS7.T1", "type").WithLocation(8, 17)
                );
        }

        [Fact, WorkItem(13316, "https://github.com/dotnet/roslyn/issues/13316")]
        public void MethodGroupAsExpressionInIsPattern()
        {
            var source =
@"namespace CS7
{
    class Program
    {
        const int T = 2;
        static void M(object o)
        {
            if (M is T)
            {
            }
        }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (8,17): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //             if (M is T)
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "M is T").WithLocation(8, 17)
                );
        }

        [Fact, WorkItem(13383, "https://github.com/dotnet/roslyn/issues/13383")]
        public void MethodGroupAsExpressionInIsPatternBrokenCode()
        {
            var source =
@"namespace CS7
{
    class Program
    {
        static void M(object o)
        {
            if (o.Equals is()) {}
            if (object.Equals is()) {}
        }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (7,29): error CS1525: Invalid expression term ')'
                //             if (o.Equals is()) {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(7, 29),
                // (8,34): error CS1525: Invalid expression term ')'
                //             if (object.Equals is()) {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(8, 34),
                // (7,17): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //             if (o.Equals is()) {}
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "o.Equals is()").WithLocation(7, 17),
                // (8,17): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //             if (object.Equals is()) {}
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "object.Equals is()").WithLocation(8, 17)
                );
        }

        [Fact, WorkItem(13383, "https://github.com/dotnet/roslyn/issues/13383")]
        public void MethodGroupAsExpressionInIsPatternBrokenCode2()
        {
            var source =
@"namespace CS7
{
    class Program
    {
        static void M(object o)
        {
            if (null is()) {}
            if ((1, object.Equals) is()) {}
        }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (7,25): error CS1525: Invalid expression term ')'
                //             if (null is()) {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(7, 25),
                // (8,39): error CS1525: Invalid expression term ')'
                //             if ((1, object.Equals) is()) {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(8, 39),
                // (7,17): error CS8117: Invalid operand for pattern match; value required, but found '<null>'.
                //             if (null is()) {}
                Diagnostic(ErrorCode.ERR_BadIsPatternExpression, "null").WithArguments("<null>").WithLocation(7, 17),
                // (8,17): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //             if ((1, object.Equals) is()) {}
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(1, object.Equals)").WithArguments("System.ValueTuple`2").WithLocation(8, 17),
                // (8,17): error CS0023: Operator 'is' cannot be applied to operand of type '(int, method group)'
                //             if ((1, object.Equals) is()) {}
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "(1, object.Equals) is()").WithArguments("is", "(int, method group)").WithLocation(8, 17)
                );
        }

        [Fact, WorkItem(13723, "https://github.com/dotnet/roslyn/issues/13723")]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void ExpressionWithoutAType()
        {
            var source =
@"
public class Vec
{
    public static void Main()
    {
        if (null is 1) {}
        if (Main is 2) {}
        if (delegate {} is 3) {}
        if ((1, null) is 4) {}
        if (null is var x1) {}
        if (Main is var x2) {}
        if (delegate {} is var x3) {}
        if ((1, null) is var x4) {}
    }
}
";
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (6,13): error CS8117: Invalid operand for pattern match; value required, but found '<null>'.
                //         if (null is 1) {}
                Diagnostic(ErrorCode.ERR_BadIsPatternExpression, "null").WithArguments("<null>").WithLocation(6, 13),
                // (7,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (Main is 2) {}
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "Main is 2").WithLocation(7, 13),
                // (8,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (delegate {} is 3) {}
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "delegate {} is 3").WithLocation(8, 13),
                // (9,13): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         if ((1, null) is 4) {}
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(1, null)").WithArguments("System.ValueTuple`2").WithLocation(9, 13),
                // (9,13): error CS0023: Operator 'is' cannot be applied to operand of type '(int, <null>)'
                //         if ((1, null) is 4) {}
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "(1, null) is 4").WithArguments("is", "(int, <null>)").WithLocation(9, 13),
                // (10,13): error CS8117: Invalid operand for pattern match; value required, but found '<null>'.
                //         if (null is var x1) {}
                Diagnostic(ErrorCode.ERR_BadIsPatternExpression, "null").WithArguments("<null>").WithLocation(10, 13),
                // (11,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (Main is var x2) {}
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "Main is var x2").WithLocation(11, 13),
                // (12,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (delegate {} is var x3) {}
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "delegate {} is var x3").WithLocation(12, 13),
                // (13,13): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         if ((1, null) is var x4) {}
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(1, null)").WithArguments("System.ValueTuple`2").WithLocation(13, 13),
                // (13,13): error CS0023: Operator 'is' cannot be applied to operand of type '(int, <null>)'
                //         if ((1, null) is var x4) {}
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "(1, null) is var x4").WithArguments("is", "(int, <null>)").WithLocation(13, 13)
                );
        }

        [Fact, WorkItem(13746, "https://github.com/dotnet/roslyn/issues/13746")]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void ExpressionWithoutAType02()
        {
            var source =
@"
public class Program
{
    public static void Main()
    {
        if ((1, null) is Program) {}
    }
}
";
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (6,13): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         if ((1, null) is Program) {}
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(1, null)").WithArguments("System.ValueTuple`2").WithLocation(6, 13),
                // (6,13): error CS0023: Operator 'is' cannot be applied to operand of type '(int, <null>)'
                //         if ((1, null) is Program) {}
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "(1, null) is Program").WithArguments("is", "(int, <null>)").WithLocation(6, 13)
                );
        }

        [Fact]
        public void ThrowExpressionForParameterValidation()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        foreach (var s in new[] { ""0123"", ""foo"" })
        {
            Console.Write(s + "" "");
            try
            {
                Console.WriteLine(Ver(s));
            }
            catch (ArgumentException)
            {
                Console.WriteLine(""throws"");
            }
        }
    }
    static int Ver(string s)
    {
        var result = int.TryParse(s, out int k) ? k : throw new ArgumentException(nameof(s));
        return k; // definitely assigned!
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput:
@"0123 123
foo throws");
        }

        [Fact]
        public void ThrowExpressionWithNullable01()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(M(1));
        try
        {
            Console.WriteLine(M(null));
        }
        catch (Exception)
        {
            Console.WriteLine(""thrown"");
        }
    }
    static int M(int? data)
    {
        return data ?? throw null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput:
@"1
thrown");
        }

        [Fact]
        public void ThrowExpressionWithNullable02()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(M(1));
        try
        {
            Console.WriteLine(M(null));
        }
        catch (Exception)
        {
            Console.WriteLine(""thrown"");
        }
    }
    static string M(object data)
    {
        return data?.ToString() ?? throw null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput:
@"1
thrown");
        }

        [Fact]
        public void ThrowExpressionWithNullable03()
        {
            var source =
@"using System;
using System.Threading.Tasks;

class Program
{
    public static void Main(string[] args)
    {
        MainAsync().Wait();
    }
    static async Task MainAsync()
    {
        foreach (var i in new[] { 1, 2 })
        {
            try
            {
                var used = (await Foo(i))?.ToString() ?? throw await Bar(i);
            }
            catch (Exception ex)
            {
                Console.WriteLine(""thrown "" + ex.Message);
            }
        }
    }
    static async Task<object> Foo(int i)
    {
        await Task.Yield();
        return (i == 1) ? i : (object)null;
    }
    static async Task<Exception> Bar(int i)
    {
        await Task.Yield();
        Console.WriteLine(""making exception "" + i);
        return new Exception(i.ToString());
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe,
                references: new[] { MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929 });
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput:
@"making exception 2
thrown 2");
        }

        [Fact]
        public void ThrowExpressionPrecedence01()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        Exception ex = null;
        try
        {
            // The ?? operator is right-associative, even under 'throw'
            ex = ex ?? throw ex ?? throw new ArgumentException(""blue"");
        }
        catch (ArgumentException x)
        {
            Console.WriteLine(x.Message);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput:
@"blue");
        }

        [Fact]
        public void ThrowExpressionPrecedence02()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        MyException ex = null;
        try
        {
            // Throw expression binds looser than +
            ex = ex ?? throw ex + 1;
        }
        catch (MyException x)
        {
            Console.WriteLine(x.Message);
        }
    }
}
class MyException : Exception
{
    public MyException(string message) : base(message) {}
    public static MyException operator +(MyException left, int right)
    {
        return new MyException(""green"");
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput:
@"green");
        }

        [Fact, WorkItem(10492, "https://github.com/dotnet/roslyn/issues/10492")]
        public void IsPatternPrecedence()
        {
            var source =
@"using System;

class Program
{
    const bool B = true;
    const int One = 1;

    public static void Main(string[] args)
    {
        object a = null;
        B c = null;
        Console.WriteLine(a is B & c); // prints 5 (correct)
        Console.WriteLine(a is B > c); // prints 6 (correct)
        Console.WriteLine(a is B < c); // was syntax error but should print 7
        Console.WriteLine(3 is One + 2); // should print True
        Console.WriteLine(One + 2 is 3); // should print True
    }
}

class B
{
    public static int operator &(bool left, B right) => 5;
    public static int operator >(bool left, B right) => 6;
    public static int operator <(bool left, B right) => 7;
    public static int operator +(bool left, B right) => 8;
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe,
                parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (15,27): error CS8059: Feature 'pattern matching' is not available in C# 6.  Please use language version 7 or greater.
                //         Console.WriteLine(3 is One + 2); // should print True
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "3 is One + 2").WithArguments("pattern matching", "7").WithLocation(15, 27),
                // (16,27): error CS8059: Feature 'pattern matching' is not available in C# 6.  Please use language version 7 or greater.
                //         Console.WriteLine(One + 2 is 3); // should print True
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "One + 2 is 3").WithArguments("pattern matching", "7").WithLocation(16, 27)
                );
            var expectedOutput =
@"5
6
7
True
True";
            compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(10492, "https://github.com/dotnet/roslyn/issues/10492")]
        public void IsPatternPrecedence02()
        {
            var source =
@"using System;

class Program
{
    public static void Main(string[] args)
    {
        foreach (object A in new[] { null, new B<C,D>() })
        {
            // pass one argument, a pattern-matching operation
            M(A is B < C, D > E);
            switch (A)
            {
                case B < C, D > F:
                    Console.WriteLine(""yes"");
                    break;
                default:
                    Console.WriteLine(""no"");
                    break;
            }
        }
    }
    static void M(object o)
    {
        Console.WriteLine(o);
    }
}

class B<C,D>
{
}
class C {}
class D {}
";
            var expectedOutput =
@"False
no
True
yes";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(10492, "https://github.com/dotnet/roslyn/issues/10492")]
        public void IsPatternPrecedence03()
        {
            var source =
@"using System;

class Program
{
    public static void Main(string[] args)
    {
        object A = new B<C, D>();
        Console.WriteLine(A is B < C, D > E);
        Console.WriteLine(A as B < C, D > ?? string.Empty);
    }
}

class B<C,D>
{
    public static implicit operator string(B<C,D> b) => nameof(B<C,D>);
}
class C {}
class D {}
";
            var expectedOutput =
@"True
B";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);

            SyntaxFactory.ParseExpression("A is B < C, D > E").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("A as B < C, D > E").GetDiagnostics().Verify(
                // (1,1): error CS1073: Unexpected token 'E'
                // A as B < C, D > E
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "A as B < C, D >").WithArguments("E").WithLocation(1, 1)
                );

            SyntaxFactory.ParseExpression("A as B < C, D > ?? string.Empty").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("A is B < C, D > ?? string.Empty").GetDiagnostics().Verify(
                // (1,1): error CS1073: Unexpected token ','
                // A is B < C, D > ?? string.Empty
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "A is B < C").WithArguments(",").WithLocation(1, 1)
                );
        }
    }
}
